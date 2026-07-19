# Wspolne helpery dla wszystkich ai-*-worker.ps1. Dot-source na poczatku kazdego workera:
#   . (Join-Path $PSScriptRoot "ai-worker-common.ps1")

function Test-RequiredCommand {
    param([Parameter(Mandatory = $true)][string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found on PATH."
    }
}

function ConvertTo-Slug {
    param([Parameter(Mandatory = $true)][string]$Value)
    $slug = $Value.ToLowerInvariant() -replace "[^a-z0-9]+", "-"
    $slug = $slug.Trim("-")
    if ($slug.Length -gt 48) { $slug = $slug.Substring(0, 48).Trim("-") }
    if ([string]::IsNullOrWhiteSpace($slug)) { return "ai-task" }
    return $slug
}

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments
    )
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: ${Command} $($Arguments -join ' ')"
    }
}

function Test-TransientGitNetworkError {
    <# Rozpoznaje przejsciowe bledy sieciowe (np. DNS runnera na chwile padl) - te warto powtorzyc. #>
    param([Parameter(Mandatory = $true)][string]$ErrorText)
    $patterns = @(
        "Could not resolve host",
        "Failed to connect",
        "Connection timed out",
        "Connection reset by peer",
        "Recv failure",
        "unable to access",
        "The requested URL returned error: 5",
        "Empty reply from server",
        "Network is unreachable",
        "Temporary failure in name resolution"
    )
    foreach ($pattern in $patterns) {
        if ($ErrorText -match [regex]::Escape($pattern)) { return $true }
    }
    return $false
}

function Invoke-CheckedWithRetry {
    <#
    Jak Invoke-Checked, ale przy przejsciowym bledzie sieciowym (DNS, timeout, reset) probuje ponownie
    z backoff zamiast od razu wywalac joba. Bledy nie-sieciowe (np. rejected/non-fast-forward) rzuca od razu.
    #>
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [int]$MaxAttempts = 3,
        [int[]]$DelaySeconds = @(5, 15)
    )
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        $output = & $Command @Arguments 2>&1
        $output | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -eq 0) { return }

        $outputText = ($output | ForEach-Object { $_.ToString() }) -join "`n"
        $isLastAttempt = $attempt -ge $MaxAttempts
        if ($isLastAttempt -or -not (Test-TransientGitNetworkError -ErrorText $outputText)) {
            throw "Command failed with exit code ${LASTEXITCODE}: ${Command} $($Arguments -join ' ')`n$outputText"
        }

        $delay = $DelaySeconds[[Math]::Min($attempt - 1, $DelaySeconds.Length - 1)]
        Write-Warning "Transient network error on attempt ${attempt}/${MaxAttempts} for '${Command} $($Arguments -join ' ')' - retrying in ${delay}s..."
        Start-Sleep -Seconds $delay
    }
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )
    $directory = Split-Path -Parent $Path
    if ($directory) { New-Item -ItemType Directory -Force -Path $directory | Out-Null }
    [System.IO.File]::WriteAllText((Join-Path (Get-Location).Path $Path), $Content, [System.Text.UTF8Encoding]::new($false))
}

function Enable-LocalGitCredentialsForPush {
    git config --local --unset-all "http.https://github.com/.extraheader" 2>$null
    $env:GIT_TERMINAL_PROMPT = "0"
}

function Invoke-ClaudeCode {
    <#
    Odpala claude --print z podanym promptem i allowedTools. Zwraca [pscustomobject]
    @{ ExitCode; Text }. NIE rzuca przy niezerowym exit code — wolant decyduje co dalej.
    #>
    param(
        [Parameter(Mandatory = $true)][string]$Prompt,
        [Parameter(Mandatory = $true)][string]$Model,
        [Parameter(Mandatory = $true)][string]$Budget,
        [Parameter(Mandatory = $true)][string]$AllowedTools,
        [string]$PermissionMode = "acceptEdits"
    )
    $claudeArgs = @(
        "--print",
        "--model", $Model,
        "--permission-mode", $PermissionMode,
        "--output-format", "text",
        "--max-budget-usd", $Budget,
        "--allowedTools", $AllowedTools
    )
    $output = $Prompt | & claude @claudeArgs 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | ForEach-Object { $_.ToString() }) -join "`n"
    return [pscustomobject]@{ ExitCode = $exitCode; Text = $text }
}

function Initialize-FreshBranch {
    <# Zawsze switch -c z origin/BaseBranch, unikalna nazwa (RunId w nazwie) - zero kolizji, zero force-push. #>
    param(
        [Parameter(Mandatory = $true)][string]$BranchName,
        [Parameter(Mandatory = $true)][string]$BaseBranch
    )
    Invoke-CheckedWithRetry -Command "git" -Arguments @("fetch", "origin", $BaseBranch)
    Invoke-Checked "git" "switch" "-c" $BranchName "origin/$BaseBranch" | Out-Null
}

function Checkout-PullRequestBranch {
    <# Do workerow ktore dopisuja do ISTNIEJACEGO PR-a (unittest/e2e) zamiast tworzyc nowy. #>
    param([Parameter(Mandatory = $true)][int]$PullRequestNumber, [Parameter(Mandatory = $true)][string]$Repository)
    $pr = gh pr view $PullRequestNumber --repo $Repository --json number,title,body,url,headRefName,baseRefName | ConvertFrom-Json
    Invoke-CheckedWithRetry -Command "git" -Arguments @("fetch", "origin", $pr.headRefName)
    Invoke-Checked "git" "switch" "-C" $pr.headRefName "origin/$($pr.headRefName)" | Out-Null
    return $pr
}

function Get-PullRequestDiff {
    param([Parameter(Mandatory = $true)][int]$PullRequestNumber, [Parameter(Mandatory = $true)][string]$Repository, [int]$MaxChars = 20000)
    $diff = gh pr diff $PullRequestNumber --repo $Repository
    $text = ($diff | Out-String)
    if ($text.Length -gt $MaxChars) { return $text.Substring(0, $MaxChars) + "`n... diff truncated ..." }
    return $text
}

function Push-WorkerChanges {
    <# git add -A + commit + push. Zwraca $true jesli byly zmiany, $false jesli nic do commitowania. #>
    param(
        [Parameter(Mandatory = $true)][string]$CommitMessage,
        [Parameter(Mandatory = $true)][string]$BranchName,
        [switch]$SetUpstream
    )
    $changes = git status --porcelain
    if (-not $changes) { return $false }
    Invoke-Checked "git" "add" "."
    Invoke-Checked "git" "commit" "-m" $CommitMessage
    Enable-LocalGitCredentialsForPush
    if ($SetUpstream) {
        Invoke-CheckedWithRetry -Command "git" -Arguments @("push", "-u", "origin", $BranchName)
    }
    else {
        Invoke-CheckedWithRetry -Command "git" -Arguments @("push", "origin", $BranchName)
    }
    return $true
}
