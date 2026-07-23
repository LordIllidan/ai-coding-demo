param(
    [Parameter(Mandatory = $true)]
    [string]$StoryKey,

    [Parameter(Mandatory = $true)]
    [string]$StoryStatus,

    [Parameter(Mandatory = $true)]
    [string]$RunId
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot "ai-worker-common.ps1")

Test-RequiredCommand "claude"

# Kontrakt PRZEZ env var, NIE jako CLI arg -- tekst kontraktu jest dowolnym tekstem z Jiry
# (moze zawierac cudzyslowy, backticki, nowe linie) i interpolowany bezposrednio w YAML
# "run:" jako argument psuje parsowanie PowerShella (realny bug zlapany live: kontrakt z
# cudzyslowem urwal argument w polowie, "A positional parameter cannot be found that
# accepts argument 'W'"). Env var nie przechodzi przez ten sam shell-quoting krok.
if (-not (Get-Item "env:CONTRACT_TEXT" -ErrorAction SilentlyContinue)) {
    throw "Missing required env: CONTRACT_TEXT"
}
$Contract = $env:CONTRACT_TEXT

foreach ($name in "JIRA_BASE_URL", "JIRA_EMAIL", "JIRA_API_TOKEN") {
    # GitHub Actions still SETS an env var for an unconfigured secret, just as an empty
    # string -- Get-Item alone doesn't catch that (item exists, value is just ""), which
    # produced a confusing downstream "Invalid URI: hostname could not be parsed" instead
    # of a clear error. Check for actual content, not just presence.
    if ([string]::IsNullOrWhiteSpace((Get-Item "env:$name" -ErrorAction SilentlyContinue).Value)) {
        throw "Missing required secret/env: $name (repo secret not configured)"
    }
}

$model = if ($env:AI_CLAUDE_MODEL) { $env:AI_CLAUDE_MODEL } else { "sonnet" }
$budget = if ($env:AI_CLAUDE_DESIGN_BUDGET_USD) { $env:AI_CLAUDE_DESIGN_BUDGET_USD } else { "1" }

$jiraAuthPair = "$($env:JIRA_EMAIL):$($env:JIRA_API_TOKEN)"
$jiraAuthBytes = [System.Text.Encoding]::UTF8.GetBytes($jiraAuthPair)
$jiraAuthHeader = @{ Authorization = "Basic $([Convert]::ToBase64String($jiraAuthBytes))"; "Content-Type" = "application/json" }
$jiraBase = $env:JIRA_BASE_URL.TrimEnd('/')

$prompt = @"
You are the UI/UX DESIGN agent in a specialized worker pipeline — running BEFORE any code
exists, right after the Tech Lead published a technical contract for this story and BEFORE
the Coding agent creates any dev subtasks. Your review gates whether implementation starts
at all: if you find real gaps, developers should fix the contract first instead of building
against a bad shape.

Running locally through a GitHub self-hosted runner (Windows), billed against the
maintainer's own Claude subscription (not the Foundry SDLC agents).

IMPORTANT — this repository is backend-only (ASP.NET Core: Api/Application/Domain/
Infrastructure/McpServer). There is no mobile app, no web frontend, no rendered UI code
here, and at this pipeline stage there is not even a diff yet — only the text contract
below. You cannot review pixels, layout, or visual design. Review the CONTRACT for whether
it gives a client (mobile/web, built elsewhere) what it needs for good UX:

1. Response shape completeness — does the contract's response carry every field a
   reasonable screen would need without a second round-trip (display names alongside ids,
   derived/formatted fields the client would otherwise compute itself)?
2. Empty/zero-result state — is there a clear "nothing to show" shape distinct from error?
3. Error state modeling — are error cases distinguishable by type (validation / not-found /
   forbidden / server error) with enough detail for a specific client-side message?
4. Pagination/loading signaling — for list endpoints, is there a total-count/has-more/
   cursor mechanism described?
5. Field naming consistency — does the contract's naming look consistent with a typical
   REST API in this domain (casing, terminology)?
6. Sensitive data exposure — does the contract expose more than the described screen needs?

Story: $StoryKey (status: $StoryStatus)

Technical contract from Tech Lead:
~~~text
$Contract
~~~

Rules:
1. This agent is READ-ONLY — do not modify any files, do not run git commands, do not
   create/edit pull requests. You may Read/Glob/Grep the repo for context (existing API
   conventions) but write nothing.
2. If the contract looks adequate, say so briefly — do not invent problems.
3. If gaps are found, list them concretely: which field/endpoint, what's missing, what
   client-side UX problem it would cause if built as-is.
4. Do not speculate about visual design — there is nothing to review at that level.
5. Do not read or print secrets.

Output: a short review (bullet points) of contract-shape-for-UX findings, or "brak
zastrzezen do ksztaltu kontraktu pod katem UX" if nothing is missing.
"@

$allowedTools = "Read,Glob,Grep,LS"
$result = Invoke-ClaudeCode -Prompt $prompt -Model $model -Budget $budget -AllowedTools $allowedTools

$reviewText = if ($result.ExitCode -ne 0) {
    $excerpt = if ($result.Text.Length -gt 2000) { $result.Text.Substring(0, 2000) + "`n... truncated ..." } else { $result.Text }
    "DesignWorker (story-level) failed (exit $($result.ExitCode)). Run: $env:GITHUB_SERVER_URL/$env:GITHUB_REPOSITORY/actions/runs/$RunId`n`n$excerpt"
}
else {
    $result.Text
}

$mockupLink = ""
# Kontrakt ma realna sekcje UI (nie "nie dotyczy") -> wygeneruj faktyczny wireframe HTML
# uzywajac DOKLADNIE nazw kontrolek z kontraktu (testID/id), zeby ten sam artefakt byl
# punktem odniesienia dla czlowieka (podglad w przegladarce) i dla E2ETestAgent (te same
# selektory). To odpowiedz na "brakuje nam agenta ktory faktycznie PROJEKTUJE, nie tylko
# recenzuje ksztalt API" -- osobny krok, osobny budzet, zeby review-only stories go nie placily.
if ($Contract -match '(?ms)^##\s*Nazewnictwo UI\s*$(.+?)(?=^##|\z)') {
    $uiSection = $Matches[1].Trim()
    if ($uiSection -and $uiSection -notmatch 'nie dotyczy') {
        $mockupPrompt = @"
You are the UI DESIGN MOCKUP generator — same pipeline as the review above, but this step
PRODUCES an artifact instead of reviewing one. The Tech Lead's contract below has a UI
naming section with exact control identifiers. Build a single self-contained HTML file
(inline CSS, no external assets/fonts/JS frameworks) that visually mockups the screen(s)
described, using those EXACT identifiers as both the visible element and its id/data-testid
attribute — this file becomes the shared reference: a human opens it in a browser to sign
off on layout, and E2ETestAgent's Playwright locators must match these same ids.

Story: $StoryKey
UI naming section from contract:
~~~text
$uiSection
~~~

Full contract for context (field names, states to represent — empty/error/loading if the
contract describes them):
~~~text
$Contract
~~~

Rules:
1. Write the mockup to EXACTLY this path: design-mockups/$StoryKey.html
2. Plain HTML+CSS wireframe (boxes/labels/placeholder text) — not production styling, this
   is a layout/naming reference, not a pixel-perfect design.
3. Use semantic HTML and put the contract's exact identifier on each control as
   id="<identifier>" and data-testid="<identifier>".
4. If the contract describes empty/error states, include them as separate labeled sections
   in the same file (not separate files) so all states are visible at once.
5. Do not touch any other file. Do not run git commands.

Output: one short sentence confirming what you wrote.
"@
        $branchName = "design/$($StoryKey.ToLowerInvariant())-mockup"
        Initialize-FreshBranch -BranchName $branchName -BaseBranch "main"
        $mockupResult = Invoke-ClaudeCode -Prompt $mockupPrompt -Model $model -Budget $budget -AllowedTools "Write"
        if ($mockupResult.ExitCode -eq 0 -and (Test-Path "design-mockups/$StoryKey.html")) {
            $pushed = Push-WorkerChanges -CommitMessage "AI(design): wireframe mockup for $StoryKey" -BranchName $branchName -SetUpstream
            if ($pushed) {
                $mockupLink = "$env:GITHUB_SERVER_URL/$env:GITHUB_REPOSITORY/blob/$branchName/design-mockups/$StoryKey.html"
                Write-Output "Mockup pushed: $mockupLink"
            }
        }
        else {
            Write-Output "Mockup generation failed or produced no file (exit $($mockupResult.ExitCode)) -- continuing without it, review comment still posts."
        }
    }
}

if ($mockupLink) {
    $reviewText = "$reviewText`n`nWireframe mockup (branch, not merged): $mockupLink"
}

$commentBody = "DESIGN REVIEW (UIDesignAgent):`n$reviewText"
$commentPayload = @{ body = @{ type = "doc"; version = 1; content = @(@{ type = "paragraph"; content = @(@{ type = "text"; text = $commentBody }) }) } } | ConvertTo-Json -Depth 10
Invoke-RestMethod -Uri "$jiraBase/rest/api/3/issue/$StoryKey/comment" -Method Post -Headers $jiraAuthHeader -Body $commentPayload | Out-Null
Write-Output "Posted design review comment on $StoryKey"

if ($result.ExitCode -ne 0) {
    throw "Claude Code exited with code $($result.ExitCode)."
}

$gateLabel = "ai-watch-done-$StoryStatus-UIDesignAgent"
$labelPayload = @{ update = @{ labels = @(@{ add = $gateLabel }) } } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Uri "$jiraBase/rest/api/3/issue/$StoryKey" -Method Put -Headers $jiraAuthHeader -Body $labelPayload | Out-Null
Write-Output "Added gate label '$gateLabel' on $StoryKey -- CodingAgent unblocked"
