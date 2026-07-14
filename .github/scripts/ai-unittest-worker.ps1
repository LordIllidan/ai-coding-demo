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
$budget = if ($env:AI_CLAUDE_UNITTEST_BUDGET_USD) { $env:AI_CLAUDE_UNITTEST_BUDGET_USD } else { "1.5" }

$pr = Checkout-PullRequestBranch -PullRequestNumber $PullRequestNumber -Repository $Repository
$diff = Get-PullRequestDiff -PullRequestNumber $PullRequestNumber -Repository $Repository

$prompt = @"
You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: $Repository
- PR: #$($pr.number) $($pr.title)
- URL: $($pr.url)
- Branch: $($pr.headRefName)

Diff introduced by this PR:
~~~diff
$diff
~~~

Task:
1. Identify new or changed functions/methods/classes in the diff that lack unit test coverage.
2. Write focused unit tests for them, following this repository's existing test conventions
   (framework, file layout, naming) — inspect existing tests/ before writing new ones.
3. Do NOT modify production/source code — only add or extend test files. If a change is
   untestable without a source fix, say so in your output instead of touching source.
4. Do not merge, push, or create/edit pull requests — the wrapper script handles that.
5. Do not read or print secrets. Avoid destructive git commands.

Output: short summary of which functions got new test coverage and any gaps you could not cover.
"@

$promptPath = "ai-coding-runs/pr-$PullRequestNumber-unittest-prompt.md"
Write-Utf8File -Path $promptPath -Content $prompt

$allowedTools = "Read,Write,Glob,Grep,LS,Bash(git status:*),Bash(git diff:*),Bash(python:*),Bash(pytest:*)"
$result = Invoke-ClaudeCode -Prompt $prompt -Model $model -Budget $budget -AllowedTools $allowedTools

if ($result.ExitCode -ne 0) {
    $excerpt = if ($result.Text.Length -gt 3000) { $result.Text.Substring(0, 3000) + "`n... truncated ..." } else { $result.Text }
    gh pr comment $PullRequestNumber --repo $Repository --body "UnitTestWorker failed (exit $($result.ExitCode)). Run: $env:GITHUB_SERVER_URL/$Repository/actions/runs/$RunId`n`n``````text`n$excerpt`n``````"
    throw "Claude Code exited with code $($result.ExitCode)."
}

$pushed = Push-WorkerChanges -CommitMessage "AI(unittest): add unit tests for PR #$PullRequestNumber" -BranchName $pr.headRefName
if (-not $pushed) {
    gh pr comment $PullRequestNumber --repo $Repository --body "UnitTestWorker ran but found nothing to add.`n`n``````text`n$($result.Text)`n``````"
    Write-Output "No changes produced."
    exit 0
}

gh label create "ai-unittest-done" --repo $Repository --color "0e8a16" --description "AI unit-test worker completed" 2>$null | Out-Null
Invoke-Checked "gh" "pr" "edit" "$PullRequestNumber" "--repo" $Repository "--remove-label" "ai-unittest-task" "--add-label" "ai-unittest-done"
Invoke-Checked "gh" "pr" "comment" "$PullRequestNumber" "--repo" $Repository "--body" "UnitTestWorker added unit tests.`n`n``````text`n$($result.Text)`n``````"

Write-Output "Unit tests pushed to $($pr.headRefName)"
