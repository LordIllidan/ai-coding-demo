# E2E tests for ai-worker-common.ps1's real-git wiring (Push-WorkerChanges / Initialize-FreshBranch).
# Unlike ai-worker-common.Tests.ps1 (which drives Invoke-CheckedWithRetry against a scripted fake
# command), these tests run the real `git` binary against real local repositories, so the DNS-failure
# and rejected-push scenarios produce genuine git stderr text instead of a simulated fixture.
# Run: Invoke-Pester -Script .github/scripts/ai-worker-common.E2E.Tests.ps1

$commonScript = Join-Path $PSScriptRoot "ai-worker-common.ps1"
. $commonScript

function New-BareRepo {
    param([Parameter(Mandatory = $true)][string]$Path)
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
    Push-Location $Path
    git init --bare -b main | Out-Null
    Pop-Location
}

function Set-GitIdentity {
    git config user.email "e2e@ai-worker-common.local" | Out-Null
    git config user.name "AI Worker E2E" | Out-Null
}

Describe "Push-WorkerChanges (real git, local origin)" {

    BeforeAll {
        $script:root = Join-Path ([System.IO.Path]::GetTempPath()) ("ai-worker-e2e-{0}" -f ([Guid]::NewGuid()))
        $script:originPath = Join-Path $script:root "origin.git"
        $script:workPath = Join-Path $script:root "work"

        New-BareRepo -Path $script:originPath

        New-Item -ItemType Directory -Force -Path $script:workPath | Out-Null
        Push-Location $script:workPath
        git init -b main | Out-Null
        Set-GitIdentity
        git remote add origin $script:originPath | Out-Null
        "seed" | Set-Content -Path "seed.txt"
        git add -A | Out-Null
        git commit -m "seed" | Out-Null
        git push -u origin main 2>&1 | Out-Null
        Pop-Location
    }

    AfterAll {
        if ($script:root -and (Test-Path $script:root)) {
            Remove-Item -Recurse -Force $script:root -ErrorAction SilentlyContinue
        }
    }

    BeforeEach {
        Push-Location $script:workPath
        Mock Start-Sleep { }
    }

    AfterEach {
        Pop-Location
    }

    It "commits and pushes a real change to the local origin" {
        "change-$([Guid]::NewGuid())" | Set-Content -Path "changed.txt"

        $result = Push-WorkerChanges -CommitMessage "e2e: real commit" -BranchName "main"

        $result | Should Be $true
        $originLog = git --git-dir $script:originPath log -1 --format=%s
        $originLog | Should Be "e2e: real commit"
    }

    It "returns false and pushes nothing when the working tree is clean" {
        $result = Push-WorkerChanges -CommitMessage "e2e: noop" -BranchName "main"
        $result | Should Be $false
    }

    It "retries a real DNS resolution failure and then throws with the actual git error text" {
        git remote set-url origin "https://ai-coding-demo-e2e-unresolvable-host.invalid/repo.git"
        try {
            "dns-fail-$([Guid]::NewGuid())" | Set-Content -Path "dnsfail.txt"

            $caught = $null
            try {
                Push-WorkerChanges -CommitMessage "e2e: dns fail" -BranchName "main" | Out-Null
            }
            catch {
                $caught = $_
            }

            $caught | Should Not Be $null
            $caught.Exception.Message | Should Match "Could not resolve host"
            Assert-MockCalled Start-Sleep -Times 2 -Exactly
        }
        finally {
            git remote set-url origin $script:originPath
        }
    }

    It "throws immediately on a real non-fast-forward rejection, without retrying" {
        $otherClone = Join-Path $script:root "other-clone"
        git clone $script:originPath $otherClone 2>&1 | Out-Null
        Push-Location $otherClone
        Set-GitIdentity
        "diverge" | Set-Content -Path "diverge.txt"
        git add -A | Out-Null
        git commit -m "diverging commit" | Out-Null
        git push origin main 2>&1 | Out-Null
        Pop-Location

        "local-only-$([Guid]::NewGuid())" | Set-Content -Path "local.txt"

        $caught = $null
        try {
            Push-WorkerChanges -CommitMessage "e2e: should be rejected" -BranchName "main" | Out-Null
        }
        catch {
            $caught = $_
        }

        $caught | Should Not Be $null
        $caught.Exception.Message | Should Match "rejected"
        Assert-MockCalled Start-Sleep -Times 0 -Exactly
    }
}

Describe "Initialize-FreshBranch (real git, local origin)" {

    BeforeAll {
        $script:root2 = Join-Path ([System.IO.Path]::GetTempPath()) ("ai-worker-e2e-init-{0}" -f ([Guid]::NewGuid()))
        $script:originPath2 = Join-Path $script:root2 "origin.git"
        $script:workPath2 = Join-Path $script:root2 "work"

        New-BareRepo -Path $script:originPath2

        New-Item -ItemType Directory -Force -Path $script:workPath2 | Out-Null
        Push-Location $script:workPath2
        git init -b main | Out-Null
        Set-GitIdentity
        git remote add origin $script:originPath2 | Out-Null
        "seed" | Set-Content -Path "seed.txt"
        git add -A | Out-Null
        git commit -m "seed" | Out-Null
        git push -u origin main 2>&1 | Out-Null
    }

    AfterAll {
        Pop-Location
        if ($script:root2 -and (Test-Path $script:root2)) {
            Remove-Item -Recurse -Force $script:root2 -ErrorAction SilentlyContinue
        }
    }

    It "fetches origin/main and creates a fresh local branch from it" {
        Mock Start-Sleep { }

        Initialize-FreshBranch -BranchName "ai-coding/e2e-fresh-branch" -BaseBranch "main"

        $currentBranch = git rev-parse --abbrev-ref HEAD
        $currentBranch | Should Be "ai-coding/e2e-fresh-branch"
    }
}
