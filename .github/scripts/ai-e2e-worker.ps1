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
$budget = if ($env:AI_CLAUDE_E2E_BUDGET_USD) { $env:AI_CLAUDE_E2E_BUDGET_USD } else { "1.5" }

$pr = Checkout-PullRequestBranch -PullRequestNumber $PullRequestNumber -Repository $Repository
$diff = Get-PullRequestDiff -PullRequestNumber $PullRequestNumber -Repository $Repository

# Jira is the source of truth for acceptance criteria (no GitHub issue exists — see
# CodingWorker), so the PR body itself already carries the Jira key + task context.
$linkedIssueBody = $pr.body

$prompt = @"
You are the E2E TEST agent in a specialized worker pipeline (separate agents exist for
coding, unit tests, and review — stay scoped to end-to-end / user-flow test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: $Repository
- PR: #$($pr.number) $($pr.title)
- URL: $($pr.url)
- Branch: $($pr.headRefName)

PR description (contains the originating Jira key and task context — the acceptance
criteria live in Jira; use what's summarized here as the source of truth for what a
real flow must satisfy):
~~~markdown
$linkedIssueBody
~~~

Diff introduced by this PR:
~~~diff
$diff
~~~

Task:
1. Identify the user-facing (API) behavior this PR introduces or changes.
2. Write end-to-end test(s) exercising the real HTTP flow against the running Api project —
   use ASP.NET Core's WebApplicationFactory<Program> for in-process integration testing
   (real HTTP pipeline, DI container, no mocks of the API surface itself). If a tests project
   for this already exists, extend it; otherwise create one under tests/PolicyPlatform.Api.E2E.Tests
   and add it to PolicyPlatform.slnx.
3. Cover the acceptance criteria implied by the Jira task if present.
4. Do NOT modify production/source code — only add or extend e2e test files.
5. Do not merge, push, or create/edit pull requests — the wrapper script handles that.
6. Do not read or print secrets. Avoid destructive git commands.

Output: short summary of which user flows got e2e coverage and any gaps you could not cover.
"@

$promptPath = "ai-coding-runs/pr-$PullRequestNumber-e2e-prompt.md"
Write-Utf8File -Path $promptPath -Content $prompt

$allowedTools = "Read,Write,Glob,Grep,LS,Bash(git status:*),Bash(git diff:*),Bash(dotnet build:*),Bash(dotnet test:*)"
$result = Invoke-ClaudeCode -Prompt $prompt -Model $model -Budget $budget -AllowedTools $allowedTools

if ($result.ExitCode -ne 0) {
    $excerpt = if ($result.Text.Length -gt 3000) { $result.Text.Substring(0, 3000) + "`n... truncated ..." } else { $result.Text }
    gh pr comment $PullRequestNumber --repo $Repository --body "E2ETestWorker failed (exit $($result.ExitCode)). Run: $env:GITHUB_SERVER_URL/$Repository/actions/runs/$RunId`n`n``````text`n$excerpt`n``````"
    throw "Claude Code exited with code $($result.ExitCode)."
}

$pushed = Push-WorkerChanges -CommitMessage "AI(e2e): add e2e tests for PR #$PullRequestNumber" -BranchName $pr.headRefName
if (-not $pushed) {
    gh pr comment $PullRequestNumber --repo $Repository --body "E2ETestWorker ran but found nothing to add.`n`n``````text`n$($result.Text)`n``````"
    Write-Output "No changes produced."
    exit 0
}

gh label create "ai-e2e-done" --repo $Repository --color "0e8a16" --description "AI e2e-test worker completed" 2>$null | Out-Null
Invoke-Checked "gh" "pr" "edit" "$PullRequestNumber" "--repo" $Repository "--remove-label" "ai-e2e-task" "--add-label" "ai-e2e-done"
Invoke-Checked "gh" "pr" "comment" "$PullRequestNumber" "--repo" $Repository "--body" "E2ETestWorker added e2e tests.`n`n``````text`n$($result.Text)`n``````"

Write-Output "E2E tests pushed to $($pr.headRefName)"
