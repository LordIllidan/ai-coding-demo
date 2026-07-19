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

function Invoke-CheckedWithRetry {
    <#
    Jak Invoke-Checked, ale dla operacji sieciowych git (fetch/push): przy przejsciowym
    bledzie sieci (DNS, polaczenie, 5xx) ponawia z backoffem zamiast od razu rzucac wyjatek.
    Bezwarunkowo rzuca po ostatniej probie lub gdy blad nie jest rozpoznany jako przejsciowy.
    #>
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments,
        [int]$MaxAttempts = 4,
        [int]$InitialDelaySeconds = 5
    )
    $transientPattern = "Could not resolve host|Failed to connect|Connection timed out|Connection reset|Empty reply from server|Recv failure|TLS connection|unable to access|early EOF|Temporary failure in name resolution|The requested URL returned error: 5\d\d"
    $delaySeconds = $InitialDelaySeconds
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        $output = & $Command @Arguments 2>&1
        if ($LASTEXITCODE -eq 0) {
            if ($output) { $output | ForEach-Object { Write-Output $_ } }
            return
        }
        $outputText = ($output | Out-String)
        $isTransient = $outputText -match $transientPattern
        if ($outputText) { Write-Host $outputText }
        if (-not $isTransient -or $attempt -eq $MaxAttempts) {
            throw "Command failed with exit code ${LASTEXITCODE}: ${Command} $($Arguments -join ' ')"
        }
        Write-Warning "Transient network error (attempt $attempt/$MaxAttempts) for '${Command} $($Arguments -join ' ')' - retrying in ${delaySeconds}s..."
        Start-Sleep -Seconds $delaySeconds
        $delaySeconds = $delaySeconds * 2
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
    Invoke-CheckedWithRetry "git" "fetch" "origin" $BaseBranch | Out-Null
    Invoke-Checked "git" "switch" "-c" $BranchName "origin/$BaseBranch" | Out-Null
}

function Checkout-PullRequestBranch {
    <# Do workerow ktore dopisuja do ISTNIEJACEGO PR-a (unittest/e2e) zamiast tworzyc nowy. #>
    param([Parameter(Mandatory = $true)][int]$PullRequestNumber, [Parameter(Mandatory = $true)][string]$Repository)
    $pr = gh pr view $PullRequestNumber --repo $Repository --json number,title,body,url,headRefName,baseRefName | ConvertFrom-Json
    Invoke-CheckedWithRetry "git" "fetch" "origin" $pr.headRefName | Out-Null
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
        Invoke-CheckedWithRetry "git" "push" "-u" "origin" $BranchName
    }
    else {
        Invoke-CheckedWithRetry "git" "push" "origin" $BranchName
    }
    return $true
}
