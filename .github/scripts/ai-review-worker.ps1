param(
    [Parameter(Mandatory = $true)]
    [int]$PullRequestNumber,

    [Parameter(Mandatory = $true)]
    [string]$Repository,

    [Parameter(Mandatory = $true)]
    [string]$RunId
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot "ai-worker-common.ps1")

Test-RequiredCommand "git"
Test-RequiredCommand "gh"
Test-RequiredCommand "claude"

$model = if ($env:AI_CLAUDE_MODEL) { $env:AI_CLAUDE_MODEL } else { "sonnet" }
$budget = if ($env:AI_CLAUDE_REVIEW_BUDGET_USD) { $env:AI_CLAUDE_REVIEW_BUDGET_USD } else { "1" }

# Read-only checkout: this worker never commits/pushes, only reads the branch for context.
$pr = Checkout-PullRequestBranch -PullRequestNumber $PullRequestNumber -Repository $Repository
$diff = Get-PullRequestDiff -PullRequestNumber $PullRequestNumber -Repository $Repository

$prompt = @"
You are the REVIEW agent in a specialized worker pipeline (separate agents already did
coding, unit tests, and e2e tests — your job is ONLY to review, never to edit files).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under review:
- Repository: $Repository
- PR: #$($pr.number) $($pr.title)
- URL: $($pr.url)
- Branch: $($pr.headRefName)

Diff:
~~~diff
$diff
~~~

Task: review this diff for correctness, security, and test coverage. You have read-only
tool access — you cannot and must not attempt to edit any file. Look at surrounding
repository files with Read/Grep/Glob for context as needed.

Output format (exact, first line matters — a script parses it):
Line 1: exactly one of ``Verdict: LOOKS_GOOD`` / ``Verdict: REQUEST_CHANGES``
Then: a short structured review — what's right, concrete issues found (file:line if possible),
and whether test coverage (unit/e2e) looks adequate for the change.

Use REQUEST_CHANGES only for real bugs/security issues/missing critical coverage, not style
nitpicks. Use LOOKS_GOOD otherwise. You are NOT authorized to approve this pull request —
final approval is always a human decision. Your review is advisory input only.
"@

$promptPath = "ai-coding-runs/pr-$PullRequestNumber-review-prompt.md"
Write-Utf8File -Path $promptPath -Content $prompt

# Read-only tools, no Edit/Write, no acceptEdits needed since nothing to accept.
$allowedTools = "Read,Glob,Grep,LS,Bash(git status:*),Bash(git diff:*),Bash(git log:*)"
$result = Invoke-ClaudeCode -Prompt $prompt -Model $model -Budget $budget -AllowedTools $allowedTools -PermissionMode "default"

if ($result.ExitCode -ne 0) {
    $excerpt = if ($result.Text.Length -gt 3000) { $result.Text.Substring(0, 3000) + "`n... truncated ..." } else { $result.Text }
    gh pr comment $PullRequestNumber --repo $Repository --body "ReviewWorker failed (exit $($result.ExitCode)). Run: $env:GITHUB_SERVER_URL/$Repository/actions/runs/$RunId`n`n``````text`n$excerpt`n``````"
    throw "Claude Code exited with code $($result.ExitCode)."
}

# NOTE: this worker never calls `gh pr review --approve`. Approval is always a human decision —
# an AI approving a PR authored by another AI worker defeats the point of review entirely.
$verdict = "LOOKS_GOOD"
if ($result.Text -match "(?im)^\s*Verdict\s*:\s*REQUEST_CHANGES\s*$") { $verdict = "REQUEST_CHANGES" }

$reviewFlag = if ($verdict -eq "REQUEST_CHANGES") { "--request-changes" } else { "--comment" }

$bodyPath = ".ai-review-body.md"
Write-Utf8File -Path $bodyPath -Content $result.Text
Invoke-Checked "gh" "pr" "review" "$PullRequestNumber" "--repo" $Repository $reviewFlag "--body-file" $bodyPath

gh label create "ai-review-done" --repo $Repository --color "0e8a16" --description "AI review worker completed" 2>$null | Out-Null
Invoke-Checked "gh" "pr" "edit" "$PullRequestNumber" "--repo" $Repository "--remove-label" "ai-review-task" "--add-label" "ai-review-done"

Write-Output "Posted review ($verdict) on PR #$PullRequestNumber — human approval still required."
