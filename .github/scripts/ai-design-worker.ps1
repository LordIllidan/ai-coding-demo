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
$budget = if ($env:AI_CLAUDE_DESIGN_BUDGET_USD) { $env:AI_CLAUDE_DESIGN_BUDGET_USD } else { "1" }

$pr = Checkout-PullRequestBranch -PullRequestNumber $PullRequestNumber -Repository $Repository
$diff = Get-PullRequestDiff -PullRequestNumber $PullRequestNumber -Repository $Repository

$prompt = @"
You are the UI/UX DESIGN agent in a specialized worker pipeline (separate agents handle
coding, unit tests, e2e tests, database, documentation, and review — stay scoped to
design/UX review only, never touch executable logic).
Running locally through a GitHub self-hosted runner (Windows), billed against the
maintainer's own Claude subscription (not the Foundry SDLC agents).

IMPORTANT — this repository is backend-only (ASP.NET Core: Api/Application/Domain/
Infrastructure/McpServer). There is no mobile app, no web frontend, no rendered UI code
here. Jira stories titled "Mobile: ekran ..." describe screens that live in a DIFFERENT,
not-yet-built client — this PR only contains the API contract that client will bind to.
You cannot review pixels, layout, or visual design because none exist in this diff.

Your actual job: review whether the API/DTO surface introduced or changed in this diff
properly SUPPORTS a good client UX, given the target screen described in the Jira story
title/description if present in the PR title. Concretely check:

1. Response shape completeness for the target screen — does the DTO carry every field a
   reasonable screen would need to render without a second round-trip (e.g. display names
   alongside ids, formatted/derived fields the client would otherwise have to compute)?
2. Empty/zero-result state — does the endpoint return a clean empty collection/null rather
   than an error when there is legitimately nothing to show, so the client can render an
   empty state instead of an error state?
3. Error state modeling — are error responses distinguishable by type (validation vs
   not-found vs forbidden vs server error) with enough detail (problem details / error
   code) for the client to show a specific, useful message instead of a generic failure?
4. Loading/pagination signaling — for list endpoints, is there a clear total-count / has-
   more / cursor mechanism so the client can render pagination or infinite-scroll state
   correctly?
5. Field naming consistency — do field names match the conventions already used elsewhere
   in this API (casing, terminology), so a mobile client binding to multiple endpoints
   doesn't have to special-case this one?
6. Sensitive data exposure — does the response leak more than the described screen needs
   (e.g. full entity when the screen shows 3 fields)?

Pull request under design/UX review:
- Repository: $Repository
- PR: #$($pr.number) $($pr.title)
- URL: $($pr.url)
- Branch: $($pr.headRefName)

Diff introduced by this PR:
~~~diff
$diff
~~~

Rules:
1. Do NOT modify any code, tests, or docs — this agent is READ-ONLY / comment-only. Do not
   use Edit/Write/git commit under any circumstance.
2. If the API/DTO shape looks adequate for the target screen, say so briefly — do not
   invent problems to justify the review.
3. If gaps are found, list them concretely: which field/endpoint, what's missing, what
   client-side UX problem it causes.
4. Do not speculate about visual design (colors, spacing, components) — there is nothing
   in this repo to review at that level.
5. Do not read or print secrets. Avoid destructive git commands (this agent should not run
   any git write commands at all).

Output: a short review (bullet points) of API-shape-for-UX findings, or "brak zastrzezen
do ksztaltu API pod katem UX" if nothing is missing.
"@

$promptPath = "ai-coding-runs/pr-$PullRequestNumber-design-prompt.md"
Write-Utf8File -Path $promptPath -Content $prompt

$allowedTools = "Read,Glob,Grep,LS,Bash(git status:*),Bash(git diff:*)"
$result = Invoke-ClaudeCode -Prompt $prompt -Model $model -Budget $budget -AllowedTools $allowedTools

if ($result.ExitCode -ne 0) {
    $excerpt = if ($result.Text.Length -gt 3000) { $result.Text.Substring(0, 3000) + "`n... truncated ..." } else { $result.Text }
    gh pr comment $PullRequestNumber --repo $Repository --body "DesignWorker failed (exit $($result.ExitCode)). Run: $env:GITHUB_SERVER_URL/$Repository/actions/runs/$RunId`n`n``````text`n$excerpt`n``````"
    throw "Claude Code exited with code $($result.ExitCode)."
}

gh label create "ai-design-done" --repo $Repository --color "0e8a16" --description "AI design/UX worker completed" 2>$null | Out-Null
Invoke-Checked "gh" "pr" "edit" "$PullRequestNumber" "--repo" $Repository "--remove-label" "ai-design-task" "--add-label" "ai-design-done"
Invoke-Checked "gh" "pr" "comment" "$PullRequestNumber" "--repo" $Repository "--body" "DesignWorker (API-shape-for-UX review) finished.`n`n``````text`n$($result.Text)`n``````"

Write-Output "Design review posted on PR #$PullRequestNumber"
