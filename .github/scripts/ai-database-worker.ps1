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
$budget = if ($env:AI_CLAUDE_DATABASE_BUDGET_USD) { $env:AI_CLAUDE_DATABASE_BUDGET_USD } else { "1.5" }

$pr = Checkout-PullRequestBranch -PullRequestNumber $PullRequestNumber -Repository $Repository
$diff = Get-PullRequestDiff -PullRequestNumber $PullRequestNumber -Repository $Repository

$prompt = @"
You are the DATABASE agent in a specialized worker pipeline (separate agents exist for
coding, unit tests, e2e tests, and review — stay scoped to persistence/schema only, do
not touch Domain business rules or Application use-case logic).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under review:
- Repository: $Repository
- PR: #$($pr.number) $($pr.title)
- URL: $($pr.url)
- Branch: $($pr.headRefName)

Diff introduced by this PR:
~~~diff
$diff
~~~

Context: PolicyPlatform.Infrastructure currently uses in-memory repositories
(InMemoryPolicyRepository, InMemoryCustomerRepository — see src/PolicyPlatform.Infrastructure/Persistence/)
as explicit placeholders for a real database. Domain/Application must stay completely
unaware of the storage technology — only PolicyPlatform.Infrastructure may reference EF Core.

Task:
1. If this diff adds new Domain entities/value objects that need persisting and no EF Core
   DbContext exists yet in PolicyPlatform.Infrastructure, set it up: add the
   Microsoft.EntityFrameworkCore.Sqlite package reference, a PolicyPlatformDbContext,
   entity configurations (IEntityTypeConfiguration<T> per aggregate — do not put mapping
   attributes on Domain classes), and EF Core repository implementations that satisfy the
   SAME interfaces the in-memory repositories already implement (IPolicyRepository,
   ICustomerRepository in PolicyPlatform.Application/Abstractions) — do not change those
   interfaces.
2. If the DbContext already exists, add/update entity configurations and a migration
   (dotnet ef migrations add) for whatever new persisted state this PR's diff requires.
3. Keep the in-memory repositories in place (do not delete them) unless explicitly asked —
   wire the EF Core ones in behind a feature flag or leave both registered with EF Core as
   the new default in DependencyInjection.cs, whichever keeps the diff smallest and reversible.
4. Do NOT modify Domain or Application layer business logic — only Infrastructure (and
   DependencyInjection.cs wiring).
5. Do not merge, push, or create/edit pull requests — the wrapper script handles that.
6. Do not read or print secrets. Avoid destructive git commands.

Output: short summary of schema/migration changes made, or "no persistence changes needed"
if this PR's diff doesn't require any.
"@

$promptPath = "ai-coding-runs/pr-$PullRequestNumber-database-prompt.md"
Write-Utf8File -Path $promptPath -Content $prompt

$allowedTools = "Read,Write,Edit,Glob,Grep,LS,Bash(git status:*),Bash(git diff:*),Bash(dotnet build:*),Bash(dotnet test:*),Bash(dotnet ef:*),Bash(dotnet add package:*)"
$result = Invoke-ClaudeCode -Prompt $prompt -Model $model -Budget $budget -AllowedTools $allowedTools

if ($result.ExitCode -ne 0) {
    $excerpt = if ($result.Text.Length -gt 3000) { $result.Text.Substring(0, 3000) + "`n... truncated ..." } else { $result.Text }
    gh pr comment $PullRequestNumber --repo $Repository --body "DatabaseWorker failed (exit $($result.ExitCode)). Run: $env:GITHUB_SERVER_URL/$Repository/actions/runs/$RunId`n`n``````text`n$excerpt`n``````"
    throw "Claude Code exited with code $($result.ExitCode)."
}

$pushed = Push-WorkerChanges -CommitMessage "AI(database): persistence/schema changes for PR #$PullRequestNumber" -BranchName $pr.headRefName
if (-not $pushed) {
    gh pr comment $PullRequestNumber --repo $Repository --body "DatabaseWorker ran, no persistence changes needed.`n`n``````text`n$($result.Text)`n``````"
    Write-Output "No changes produced."
    exit 0
}

gh label create "ai-database-done" --repo $Repository --color "0e8a16" --description "AI database worker completed" 2>$null | Out-Null
Invoke-Checked "gh" "pr" "edit" "$PullRequestNumber" "--repo" $Repository "--remove-label" "ai-database-task" "--add-label" "ai-database-done"
Invoke-Checked "gh" "pr" "comment" "$PullRequestNumber" "--repo" $Repository "--body" "DatabaseWorker updated persistence layer.`n`n``````text`n$($result.Text)`n``````"

Write-Output "Database changes pushed to $($pr.headRefName)"
