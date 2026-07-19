# Test fixture standing in for `git` in Invoke-CheckedWithRetry tests (ai-worker-common.Tests.ps1).
# Fails FAKE_GIT_FAIL_COUNT times (with FAKE_GIT_FAIL_MODE-shaped output) before succeeding,
# tracking attempt count via FAKE_GIT_COUNTER_FILE since each attempt is a separate process.

$counterFile = $env:FAKE_GIT_COUNTER_FILE
$failCount = [int]$env:FAKE_GIT_FAIL_COUNT
$mode = $env:FAKE_GIT_FAIL_MODE

$attempt = 0
if (Test-Path $counterFile) { $attempt = [int](Get-Content $counterFile) }
$attempt++
Set-Content -Path $counterFile -Value $attempt

if ($attempt -le $failCount) {
    if ($mode -eq "transient") {
        Write-Output "fatal: unable to access 'https://github.com/foo/bar.git/': Could not resolve host: github.com"
    }
    else {
        Write-Output "! [rejected]        main -> main (non-fast-forward)"
    }
    exit 1
}
else {
    Write-Output "Everything up-to-date"
    exit 0
}
