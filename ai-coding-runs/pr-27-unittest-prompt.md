You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: LordIllidan/ai-coding-demo
- PR: #27 AI: [AISDLC-158] [Fix] Git push fails in ai-worker-common.ps1 due to DNS resolution error
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/27
- Branch: ai-coding/aisdlc-158-fix-git-push-fails-in-ai-worker-common-ps1-due-t-29679735941

Diff introduced by this PR:
~~~diff
diff --git a/.github/scripts/ai-worker-common.ps1 b/.github/scripts/ai-worker-common.ps1
index ee6ab3a..c110e15 100644
--- a/.github/scripts/ai-worker-common.ps1
+++ b/.github/scripts/ai-worker-common.ps1
@@ -28,6 +28,55 @@ function Invoke-Checked {
     }
 }
 
+function Test-TransientGitNetworkError {
+    <# Rozpoznaje przejsciowe bledy sieciowe (np. DNS runnera na chwile padl) - te warto powtorzyc. #>
+    param([Parameter(Mandatory = $true)][string]$ErrorText)
+    $patterns = @(
+        "Could not resolve host",
+        "Failed to connect",
+        "Connection timed out",
+        "Connection reset by peer",
+        "Recv failure",
+        "unable to access",
+        "The requested URL returned error: 5",
+        "Empty reply from server",
+        "Network is unreachable",
+        "Temporary failure in name resolution"
+    )
+    foreach ($pattern in $patterns) {
+        if ($ErrorText -match [regex]::Escape($pattern)) { return $true }
+    }
+    return $false
+}
+
+function Invoke-CheckedWithRetry {
+    <#
+    Jak Invoke-Checked, ale przy przejsciowym bledzie sieciowym (DNS, timeout, reset) probuje ponownie
+    z backoff zamiast od razu wywalac joba. Bledy nie-sieciowe (np. rejected/non-fast-forward) rzuca od razu.
+    #>
+    param(
+        [Parameter(Mandatory = $true)][string]$Command,
+        [Parameter(Mandatory = $true)][string[]]$Arguments,
+        [int]$MaxAttempts = 3,
+        [int[]]$DelaySeconds = @(5, 15)
+    )
+    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
+        $output = & $Command @Arguments 2>&1
+        $output | ForEach-Object { Write-Host $_ }
+        if ($LASTEXITCODE -eq 0) { return }
+
+        $outputText = ($output | ForEach-Object { $_.ToString() }) -join "`n"
+        $isLastAttempt = $attempt -ge $MaxAttempts
+        if ($isLastAttempt -or -not (Test-TransientGitNetworkError -ErrorText $outputText)) {
+            throw "Command failed with exit code ${LASTEXITCODE}: ${Command} $($Arguments -join ' ')`n$outputText"
+        }
+
+        $delay = $DelaySeconds[[Math]::Min($attempt - 1, $DelaySeconds.Length - 1)]
+        Write-Warning "Transient network error on attempt ${attempt}/${MaxAttempts} for '${Command} $($Arguments -join ' ')' - retrying in ${delay}s..."
+        Start-Sleep -Seconds $delay
+    }
+}
+
 function Write-Utf8File {
     param(
         [Parameter(Mandatory = $true)][string]$Path,
@@ -75,7 +124,7 @@ function Initialize-FreshBranch {
         [Parameter(Mandatory = $true)][string]$BranchName,
         [Parameter(Mandatory = $true)][string]$BaseBranch
     )
-    Invoke-Checked "git" "fetch" "origin" $BaseBranch | Out-Null
+    Invoke-CheckedWithRetry -Command "git" -Arguments @("fetch", "origin", $BaseBranch)
     Invoke-Checked "git" "switch" "-c" $BranchName "origin/$BaseBranch" | Out-Null
 }
 
@@ -83,7 +132,7 @@ function Checkout-PullRequestBranch {
     <# Do workerow ktore dopisuja do ISTNIEJACEGO PR-a (unittest/e2e) zamiast tworzyc nowy. #>
     param([Parameter(Mandatory = $true)][int]$PullRequestNumber, [Parameter(Mandatory = $true)][string]$Repository)
     $pr = gh pr view $PullRequestNumber --repo $Repository --json number,title,body,url,headRefName,baseRefName | ConvertFrom-Json
-    Invoke-Checked "git" "fetch" "origin" $pr.headRefName | Out-Null
+    Invoke-CheckedWithRetry -Command "git" -Arguments @("fetch", "origin", $pr.headRefName)
     Invoke-Checked "git" "switch" "-C" $pr.headRefName "origin/$($pr.headRefName)" | Out-Null
     return $pr
 }
@@ -109,10 +158,10 @@ function Push-WorkerChanges {
     Invoke-Checked "git" "commit" "-m" $CommitMessage
     Enable-LocalGitCredentialsForPush
     if ($SetUpstream) {
-        Invoke-Checked "git" "push" "-u" "origin" $BranchName
+        Invoke-CheckedWithRetry -Command "git" -Arguments @("push", "-u", "origin", $BranchName)
     }
     else {
-        Invoke-Checked "git" "push" "origin" $BranchName
+        Invoke-CheckedWithRetry -Command "git" -Arguments @("push", "origin", $BranchName)
     }
     return $true
 }
diff --git a/ai-coding-runs/aisdlc-158-coding-prompt.md b/ai-coding-runs/aisdlc-158-coding-prompt.md
new file mode 100644
index 0000000..3b0d6b1
--- /dev/null
+++ b/ai-coding-runs/aisdlc-158-coding-prompt.md
@@ -0,0 +1,28 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-158 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: [Fix] Git push fails in ai-worker-common.ps1 due to DNS resolution error
+
+Task description:
+~~~markdown
+Parent story: AISDLC-148 — Licznik nieprzeczytanych powiadomień w aplikacji mobilnej
+
+Pipeline failed during the GitHub Actions run https://github.com/LordIllidan/ai-coding-demo/actions/runs/29679179162.
+Log excerpt shows git push failing with: 'Could not resolve host: github.com' and then 'Command failed with exit code 128: git push -u origin ai-coding/aisdlc-157-fix-runner-script-fails-on-ai-coding-worker-ps1-29679179162'. This is a real failure in the worker automation, not an infra flake, because the script aborts on the push step.
+The fix should inspect .github/scripts/ai-coding-worker.ps1 and .github/scripts/ai-worker-common.ps1, especially the git push/retry/error handling path, and make the job fail gracefully or avoid hard failure when remote access is unavailable.
+PR link was not provided in the input.
+~~~
+
+Task:
+1. Implement the requested code change in this repository, scoped to the task above.
+2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
+   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
+3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
+4. Do not read or print secrets. Avoid destructive git commands.
+5. Before finishing, leave the workspace ready to commit (diff applied on disk).
+
+Output: short summary of changed files and what each change does.
\ No newline at end of file

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