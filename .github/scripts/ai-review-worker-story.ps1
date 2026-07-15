param(
    [Parameter(Mandatory = $true)]
    [string]$StoryKey,

    [Parameter(Mandatory = $true)]
    [string]$PrNumbers,  # comma-separated, e.g. "12,13,14"

    [Parameter(Mandatory = $false)]
    [string]$Contract = "",  # kontrakt z TechLeadAgent (Jira comment), moze byc puste

    [Parameter(Mandatory = $true)]
    [string]$Repository,

    [Parameter(Mandatory = $true)]
    [string]$RunId
)

# Story-level review: WSZYSTKIE DevSubtaski jednej Story naraz, nie PR po PR-ze — zeby
# lapac rozjazdy MIEDZY subtaskami (np. frontend woła customerId, backend oczekuje
# policyId), ktorych review pojedynczego PR-a nigdy nie zobaczy, bo kazda strona
# rozjazdu jest "poprawna" we wlasnym izolowanym diffie.

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot "ai-worker-common.ps1")

Test-RequiredCommand "git"
Test-RequiredCommand "gh"
Test-RequiredCommand "claude"

$model = if ($env:AI_CLAUDE_MODEL) { $env:AI_CLAUDE_MODEL } else { "sonnet" }
$budget = if ($env:AI_CLAUDE_REVIEW_BUDGET_USD) { $env:AI_CLAUDE_REVIEW_BUDGET_USD } else { "2" }

$prNumberList = $PrNumbers -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
if ($prNumberList.Count -eq 0) { throw "PrNumbers pusty — brak PR-ow do recenzji." }

$sections = @()
$prMeta = @()
foreach ($n in $prNumberList) {
    $pr = gh pr view $n --repo $Repository --json number,title,url,headRefName | ConvertFrom-Json
    $diff = Get-PullRequestDiff -PullRequestNumber ([int]$n) -Repository $Repository -MaxChars 12000
    $prMeta += $pr
    $sections += @"
### PR #$($pr.number) — $($pr.title)
URL: $($pr.url)  Branch: $($pr.headRefName)

~~~diff
$diff
~~~
"@
}

$contractSection = if ($Contract.Trim()) {
    "Kontrakt techniczny ustalony przez TechLeadAgent PRZED codingiem (wszystkie diffy ponizej musza go respektowac):`n`n$Contract"
} else {
    "(Brak zapisanego kontraktu TechLeadAgent dla tej historyjki — oceniaj spojnosc miedzy diffami na podstawie samego kodu.)"
}

$prompt = @"
You are the STORY-LEVEL REVIEW agent (separate agents already did coding, unit tests, and
e2e tests per-subtask — your job is to review the WHOLE story's PRs TOGETHER, specifically
to catch integration mismatches between subtasks that a per-PR review would miss, e.g. a
frontend PR calling a field name the backend PR never implemented).
Running locally through a GitHub self-hosted runner (Windows), read-only tool access.

Jira story: $StoryKey
Repository: $Repository
Number of PRs in this story: $($prMeta.Count)

$contractSection

All diffs for this story's PRs:

$($sections -join "`n`n")

Task: review ALL diffs above TOGETHER for:
1. Correctness/security within each diff (same as a normal review).
2. CROSS-PR CONSISTENCY: do the PRs actually agree with each other and with the contract
   (same field names, same endpoint paths/methods, same types)? Call out explicitly any
   place where one PR assumes something another PR doesn't provide.
3. Test coverage adequacy across the whole story, not just per file.

Output format (exact, first line matters — a script parses it):
Line 1: exactly one of ``Verdict: LOOKS_GOOD`` / ``Verdict: REQUEST_CHANGES``
Then: a structured review — cross-PR consistency findings first (if any), then per-PR notes.

Use REQUEST_CHANGES for real bugs/security issues/integration mismatches/missing critical
coverage, not style nitpicks. You are NOT authorized to approve any pull request — final
approval is always a human decision. Your review is advisory input only.
"@

$promptPath = "ai-coding-runs/story-$StoryKey-review-prompt.md"
Write-Utf8File -Path $promptPath -Content $prompt

$allowedTools = "Read,Glob,Grep,LS,Bash(git status:*),Bash(git diff:*),Bash(git log:*)"
$result = Invoke-ClaudeCode -Prompt $prompt -Model $model -Budget $budget -AllowedTools $allowedTools -PermissionMode "default"

if ($result.ExitCode -ne 0) {
    $excerpt = if ($result.Text.Length -gt 3000) { $result.Text.Substring(0, 3000) + "`n... truncated ..." } else { $result.Text }
    foreach ($pr in $prMeta) {
        gh pr comment $pr.number --repo $Repository --body "Story-level ReviewWorker failed (exit $($result.ExitCode)) for $StoryKey. Run: $env:GITHUB_SERVER_URL/$Repository/actions/runs/$RunId`n`n``````text`n$excerpt`n``````"
    }
    throw "Claude Code exited with code $($result.ExitCode)."
}

# Te same zasady co ReviewWorker per-PR: nigdy --approve (ludzka decyzja), nigdy
# --request-changes (GitHub odrzuca na wlasnym PR-ze tej samej tozsamosci) — zawsze
# --comment, werdykt sygnalizowany labelem.
$verdict = "LOOKS_GOOD"
if ($result.Text -match "(?im)^\s*Verdict\s*:\s*REQUEST_CHANGES\s*$") { $verdict = "REQUEST_CHANGES" }

$bodyPath = ".ai-review-body-story.md"
$prList = ($prMeta | ForEach-Object { "#$($_.number)" }) -join ", "
$header = "Story-level review dla $StoryKey (razem z: $prList)`n`n"
Write-Utf8File -Path $bodyPath -Content ($header + $result.Text)

gh label create "ai-review-done" --repo $Repository --color "0e8a16" --description "AI review worker completed" 2>$null | Out-Null
gh label create "ai-review-flagged" --repo $Repository --color "d73a4a" --description "AI review found issues requiring changes" 2>$null | Out-Null

foreach ($pr in $prMeta) {
    Invoke-Checked "gh" "pr" "review" "$($pr.number)" "--repo" $Repository "--comment" "--body-file" $bodyPath
    Invoke-Checked "gh" "pr" "edit" "$($pr.number)" "--repo" $Repository "--add-label" "ai-review-done"
    if ($verdict -eq "REQUEST_CHANGES") {
        Invoke-Checked "gh" "pr" "edit" "$($pr.number)" "--repo" $Repository "--add-label" "ai-review-flagged"
    }
}

Write-Output "Story-level review ($verdict) dla $StoryKey na PR-ach: $prList — human approval still required."
