# Pester tests for Test-TransientGitNetworkError and Invoke-CheckedWithRetry (ai-worker-common.ps1).
# Run: Invoke-Pester -Script .github/scripts/ai-worker-common.Tests.ps1

$commonScript = Join-Path $PSScriptRoot "ai-worker-common.ps1"
. $commonScript

Describe "Test-TransientGitNetworkError" {

    $transientSamples = @(
        "fatal: unable to access 'https://github.com/foo/bar.git/': Could not resolve host: github.com",
        "ssh: connect to host github.com port 22: Failed to connect",
        "fatal: unable to access 'https://github.com/': Connection timed out",
        "error: RPC failed; curl 56 Connection reset by peer",
        "error: RPC failed; curl 18 transfer closed with outstanding read data remaining (Recv failure)",
        "fatal: unable to access 'https://github.com/': The requested URL returned error: 502",
        "fatal: unable to access 'https://github.com/': Empty reply from server",
        "fatal: unable to access 'https://github.com/': Network is unreachable",
        "fatal: unable to access 'https://github.com/': Temporary failure in name resolution"
    )

    foreach ($sample in $transientSamples) {
        It "returns true for transient error text: '$sample'" {
            Test-TransientGitNetworkError -ErrorText $sample | Should Be $true
        }
    }

    It "returns false for a non-transient git error (rejected push)" {
        $text = "! [rejected]        main -> main (non-fast-forward)`nerror: failed to push some refs"
        Test-TransientGitNetworkError -ErrorText $text | Should Be $false
    }

    It "returns false for unrelated arbitrary text" {
        Test-TransientGitNetworkError -ErrorText "everything is fine" | Should Be $false
    }
}

Describe "Invoke-CheckedWithRetry" {

    $fakeCommand = Join-Path $PSScriptRoot "fixtures\fake-git-command.ps1"
    $counterFile = Join-Path ([System.IO.Path]::GetTempPath()) ("fake-git-counter-{0}.txt" -f ([Guid]::NewGuid()))

    BeforeEach {
        if (Test-Path $counterFile) { Remove-Item $counterFile -Force }
        Mock Start-Sleep { }
    }

    AfterEach {
        if (Test-Path $counterFile) { Remove-Item $counterFile -Force }
        Remove-Item Env:\FAKE_GIT_COUNTER_FILE -ErrorAction SilentlyContinue
        Remove-Item Env:\FAKE_GIT_FAIL_COUNT -ErrorAction SilentlyContinue
        Remove-Item Env:\FAKE_GIT_FAIL_MODE -ErrorAction SilentlyContinue
    }

    It "succeeds without retrying when the command works on the first attempt" {
        $env:FAKE_GIT_COUNTER_FILE = $counterFile
        $env:FAKE_GIT_FAIL_COUNT = "0"
        $env:FAKE_GIT_FAIL_MODE = "transient"

        { Invoke-CheckedWithRetry -Command "powershell" -Arguments @("-NoProfile", "-File", $fakeCommand) -MaxAttempts 3 -DelaySeconds @(0, 0) } | Should Not Throw

        [int](Get-Content $counterFile) | Should Be 1
    }

    It "retries once after a transient network error and then succeeds" {
        $env:FAKE_GIT_COUNTER_FILE = $counterFile
        $env:FAKE_GIT_FAIL_COUNT = "1"
        $env:FAKE_GIT_FAIL_MODE = "transient"

        { Invoke-CheckedWithRetry -Command "powershell" -Arguments @("-NoProfile", "-File", $fakeCommand) -MaxAttempts 3 -DelaySeconds @(0, 0) } | Should Not Throw

        [int](Get-Content $counterFile) | Should Be 2
    }

    It "throws immediately on a non-transient error without retrying" {
        $env:FAKE_GIT_COUNTER_FILE = $counterFile
        $env:FAKE_GIT_FAIL_COUNT = "3"
        $env:FAKE_GIT_FAIL_MODE = "nontransient"

        { Invoke-CheckedWithRetry -Command "powershell" -Arguments @("-NoProfile", "-File", $fakeCommand) -MaxAttempts 3 -DelaySeconds @(0, 0) } | Should Throw

        [int](Get-Content $counterFile) | Should Be 1
    }

    It "throws after exhausting all attempts when the transient error never clears" {
        $env:FAKE_GIT_COUNTER_FILE = $counterFile
        $env:FAKE_GIT_FAIL_COUNT = "99"
        $env:FAKE_GIT_FAIL_MODE = "transient"

        { Invoke-CheckedWithRetry -Command "powershell" -Arguments @("-NoProfile", "-File", $fakeCommand) -MaxAttempts 3 -DelaySeconds @(0, 0) } | Should Throw

        [int](Get-Content $counterFile) | Should Be 3
    }
}
