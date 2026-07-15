You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: LordIllidan/ai-coding-demo
- PR: #8 AI: [AISDLC-40] Walidacja: obowiązkowy numer zgłoszenia Policji w formularzu kradzieży
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/8
- Branch: ai-coding/aisdlc-40-walidacja-obowi-zkowy-numer-zg-oszenia-policji-w-29434652955

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-40-coding-prompt.md b/ai-coding-runs/aisdlc-40-coding-prompt.md
new file mode 100644
index 0000000..5418719
--- /dev/null
+++ b/ai-coding-runs/aisdlc-40-coding-prompt.md
@@ -0,0 +1,27 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-40 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: Walidacja: obowiązkowy numer zgłoszenia Policji w formularzu kradzieży
+
+Task description:
+~~~markdown
+Parent story: AISDLC-30 — Rejestracja zgłoszenia kradzieży z obowiązkowym numerem zgłoszenia Policji
+
+Co robi: dodaje wymagane pole numeru zgłoszenia Policji oraz walidację na poziomie formularza.
+Pliki: komponent formularza kradzieży, walidatory/model formularza, komunikaty błędów.
+TODO: oznaczyć pole jako wymagane, zablokować przejście dalej bez wartości, dopisać walidację formatową jeśli istnieje oraz testy jednostkowe.
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
diff --git a/ai-coding-runs/pr-8-unittest-prompt.md b/ai-coding-runs/pr-8-unittest-prompt.md
new file mode 100644
index 0000000..4cf8ed4
--- /dev/null
+++ b/ai-coding-runs/pr-8-unittest-prompt.md
@@ -0,0 +1,188 @@
+You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
+coding, e2e tests, and review — stay scoped to unit-level test coverage only).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Pull request under test:
+- Repository: LordIllidan/ai-coding-demo
+- PR: #8 AI: [AISDLC-40] Walidacja: obowiązkowy numer zgłoszenia Policji w formularzu kradzieży
+- URL: https://github.com/LordIllidan/ai-coding-demo/pull/8
+- Branch: ai-coding/aisdlc-40-walidacja-obowi-zkowy-numer-zg-oszenia-policji-w-29434652955
+
+Diff introduced by this PR:
+~~~diff
+diff --git a/ai-coding-runs/aisdlc-40-coding-prompt.md b/ai-coding-runs/aisdlc-40-coding-prompt.md
+new file mode 100644
+index 0000000..5418719
+--- /dev/null
++++ b/ai-coding-runs/aisdlc-40-coding-prompt.md
+@@ -0,0 +1,27 @@
++You are the CODING agent in a specialized worker pipeline (separate agents exist for
++unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
++Running locally through a GitHub self-hosted runner (Windows).
++
++Source of truth: Jira issue AISDLC-40 (this task has NO corresponding GitHub issue —
++Jira is the only tracker; do not create or reference a GitHub issue).
++
++Task title: Walidacja: obowiązkowy numer zgłoszenia Policji w formularzu kradzieży
++
++Task description:
++~~~markdown
++Parent story: AISDLC-30 — Rejestracja zgłoszenia kradzieży z obowiązkowym numerem zgłoszenia Policji
++
++Co robi: dodaje wymagane pole numeru zgłoszenia Policji oraz walidację na poziomie formularza.
++Pliki: komponent formularza kradzieży, walidatory/model formularza, komunikaty błędów.
++TODO: oznaczyć pole jako wymagane, zablokować przejście dalej bez wartości, dopisać walidację formatową jeśli istnieje oraz testy jednostkowe.
++~~~
++
++Task:
++1. Implement the requested code change in this repository, scoped to the task above.
++2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
++   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
++3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
++4. Do not read or print secrets. Avoid destructive git commands.
++5. Before finishing, leave the workspace ready to commit (diff applied on disk).
++
++Output: short summary of changed files and what each change does.
+\ No newline at end of file
+diff --git a/src/PolicyPlatform.Api/wwwroot/index.html b/src/PolicyPlatform.Api/wwwroot/index.html
+index 7c271d7..25a5623 100644
+--- a/src/PolicyPlatform.Api/wwwroot/index.html
++++ b/src/PolicyPlatform.Api/wwwroot/index.html
+@@ -18,6 +18,8 @@
+   .status-Active { color: #2e8b57; }
+   .status-Cancelled, .status-Expired { color: #a33; }
+   #log { white-space: pre-wrap; font-family: monospace; font-size: 0.8rem; color: #888; }
++  .field-error { color: #a33; font-size: 0.8rem; margin-top: 0.2rem; min-height: 1em; }
++  input.invalid { border-color: #a33; outline-color: #a33; }
+ </style>
+ </head>
+ <body>
+@@ -58,8 +60,21 @@ <h2>3. Polisy</h2>
+   </table>
+ </section>
+ 
++<section>
++  <h2>4. Zgłoszenie kradzieży</h2>
++  <label>Customer ID <input id="theftCustomerId" placeholder="wklej z kroku 1" /></label>
++  <label>Data kradzieży <input id="theftIncidentDate" type="date" /></label>
++  <div id="theftIncidentDateError" class="field-error"></div>
++  <label>Opis zdarzenia <input id="theftDescription" placeholder="okoliczności kradzieży" /></label>
++  <label>Numer zgłoszenia Policji * <input id="theftPoliceReportNumber" placeholder="np. L.dz. 123/26/RSD" required /></label>
++  <div id="theftPoliceReportNumberError" class="field-error"></div>
++  <button onclick="submitTheftClaim()">Zgłoś kradzież</button>
++  <div id="theftResult"></div>
++</section>
++
+ <div id="log"></div>
+ 
++<script src="theft-claim-validation.js"></script>
+ <script>
+ const log = (msg) => { document.getElementById('log').textContent += msg + "\n"; };
+ 
+@@ -130,6 +145,30 @@ <h2>3. Polisy</h2>
+   }
+ }
+ 
++function submitTheftClaim() {
++  const policeReportNumber = document.getElementById('theftPoliceReportNumber').value;
++  const incidentDate = document.getElementById('theftIncidentDate').value;
++  const policeInput = document.getElementById('theftPoliceReportNumber');
++  const dateInput = document.getElementById('theftIncidentDate');
++  const policeErrorEl = document.getElementById('theftPoliceReportNumberError');
++  const dateErrorEl = document.getElementById('theftIncidentDateError');
++  const resultEl = document.getElementById('theftResult');
++
++  const { valid, errors } = TheftClaimValidation.validateTheftClaimForm({ policeReportNumber, incidentDate });
++
++  policeErrorEl.textContent = errors.policeReportNumber || '';
++  policeInput.classList.toggle('invalid', Boolean(errors.policeReportNumber));
++  dateErrorEl.textContent = errors.incidentDate || '';
++  dateInput.classList.toggle('invalid', Boolean(errors.incidentDate));
++
++  if (!valid) {
++    resultEl.textContent = 'Popraw błędy w formularzu, aby kontynuować.';
++    return;
++  }
++
++  resultEl.textContent = 'Formularz poprawny — zgłoszenie kradzieży gotowe do wysłania.';
++}
++
+ document.getElementById('effectiveDate').valueAsDate = new Date();
+ document.getElementById('expiryDate').valueAsDate = new Date(Date.now() + 365 * 24 * 3600 * 1000);
+ loadPolicies().catch(() => {});
+diff --git a/src/PolicyPlatform.Api/wwwroot/theft-claim-validation.js b/src/PolicyPlatform.Api/wwwroot/theft-claim-validation.js
+new file mode 100644
+index 0000000..7f9e445
+--- /dev/null
++++ b/src/PolicyPlatform.Api/wwwroot/theft-claim-validation.js
+@@ -0,0 +1,57 @@
++// Walidacja formularza zgłoszenia kradzieży (AISDLC-40).
++// UMD-style export so this pure module is testable from Node without a build step.
++(function (root, factory) {
++  if (typeof module === 'object' && module.exports) {
++    module.exports = factory();
++  } else {
++    root.TheftClaimValidation = factory();
++  }
++})(typeof self !== 'undefined' ? self : this, function () {
++  // Numer zgłoszenia Policji: brak jednego oficjalnego standardu, więc akceptujemy
++  // typowe formaty ("L.dz. 123/26/RSD", "RSD-1234/26" itp.) — litery, cyfry,
++  // kropki, myślniki, ukośniki i spacje, min. 3 znaki, przynajmniej jedna cyfra.
++  const POLICE_REPORT_NUMBER_PATTERN = /^(?=.*\d)[A-Za-z0-9./ -]{3,40}$/;
++
++  const ERRORS = {
++    POLICE_REPORT_NUMBER_REQUIRED: 'Numer zgłoszenia Policji jest wymagany.',
++    POLICE_REPORT_NUMBER_INVALID_FORMAT:
++      'Nieprawidłowy format numeru zgłoszenia Policji (dozwolone litery, cyfry, kropki, myślniki, ukośniki, min. 3 znaki, w tym co najmniej jedna cyfra).',
++    INCIDENT_DATE_REQUIRED: 'Data kradzieży jest wymagana.',
++    INCIDENT_DATE_FUTURE: 'Data kradzieży nie może być w przyszłości.',
++  };
++
++  function validatePoliceReportNumber(value) {
++    const trimmed = (value ?? '').trim();
++    if (!trimmed) return ERRORS.POLICE_REPORT_NUMBER_REQUIRED;
++    if (!POLICE_REPORT_NUMBER_PATTERN.test(trimmed)) return ERRORS.POLICE_REPORT_NUMBER_INVALID_FORMAT;
++    return null;
++  }
++
++  function validateIncidentDate(value) {
++    const trimmed = (value ?? '').trim();
++    if (!trimmed) return ERRORS.INCIDENT_DATE_REQUIRED;
++    const date = new Date(trimmed);
++    if (Number.isNaN(date.getTime())) return ERRORS.INCIDENT_DATE_REQUIRED;
++    const endOfToday = new Date();
++    endOfToday.setHours(23, 59, 59, 999);
++    if (date.getTime() > endOfToday.getTime()) return ERRORS.INCIDENT_DATE_FUTURE;
++    return null;
++  }
++
++  function validateTheftClaimForm(form) {
++    const errors = {};
++    const policeReportNumberError = validatePoliceReportNumber(form.policeReportNumber);
++    if (policeReportNumberError) errors.policeReportNumber = policeReportNumberError;
++    const incidentDateError = validateIncidentDate(form.incidentDate);
++    if (incidentDateError) errors.incidentDate = incidentDateError;
++    return { valid: Object.keys(errors).length === 0, errors };
++  }
++
++  return {
++    POLICE_REPORT_NUMBER_PATTERN,
++    ERRORS,
++    validatePoliceReportNumber,
++    validateIncidentDate,
++    validateTheftClaimForm,
++  };
++});
+
+~~~
+
+Task:
+1. Identify new or changed functions/methods/classes in the diff that lack unit test coverage.
+2. Write focused unit tests for them, following this repository's existing test conventions
+   (framework, file layout, naming) — inspect existing tests/ before writing new ones.
+3. Do NOT modify production/source code — only add or extend test files. If a change is
+   untestable without a source fix, say so in your output instead of touching source.
+4. Do not merge, push, or create/edit pull requests — the wrapper script handles that.
+5. Do not read or print secrets. Avoid destructive git commands.
+
+Output: short summary of which functions got new test coverage and any gaps you could not cover.
\ No newline at end of file
diff --git a/src/PolicyPlatform.Api/wwwroot/index.html b/src/PolicyPlatform.Api/wwwroot/index.html
index 7c271d7..25a5623 100644
--- a/src/PolicyPlatform.Api/wwwroot/index.html
+++ b/src/PolicyPlatform.Api/wwwroot/index.html
@@ -18,6 +18,8 @@
   .status-Active { color: #2e8b57; }
   .status-Cancelled, .status-Expired { color: #a33; }
   #log { white-space: pre-wrap; font-family: monospace; font-size: 0.8rem; color: #888; }
+  .field-error { color: #a33; font-size: 0.8rem; margin-top: 0.2rem; min-height: 1em; }
+  input.invalid { border-color: #a33; outline-color: #a33; }
 </style>
 </head>
 <body>
@@ -58,8 +60,21 @@ <h2>3. Polisy</h2>
   </table>
 </section>
 
+<section>
+  <h2>4. Zgłoszenie kradzieży</h2>
+  <label>Customer ID <input id="theftCustomerId" placeholder="wklej z kroku 1" /></label>
+  <label>Data kradzieży <input id="theftIncidentDate" type="date" /></label>
+  <div id="theftIncidentDateError" class="field-error"></div>
+  <label>Opis zdarzenia <input id="theftDescription" placeholder="okoliczności kradzieży" /></label>
+  <label>Numer zgłoszenia Policji * <input id="theftPoliceReportNumber" placeholder="np. L.dz. 123/26/RSD" required /></label>
+  <div id="theftPoliceReportNumberError" class="field-error"></div>
+  <button onclick="submitTheftClaim()">Zgłoś kradzież</button>
+  <div id="theftResult"></div>
+</section>
+
 <div id="log"></div>
 
+<script src="theft-claim-validation.js"></script>
 <script>
 const log = (msg) => { document.getElementById('log').textContent += msg + "\n"; };
 
@@ -130,6 +145,30 @@ <h2>3. Polisy</h2>
   }
 }
 
+function submitTheftClaim() {
+  const policeReportNumber = document.getElementById('theftPoliceReportNumber').value;
+  const incidentDate = document.getElementById('theftIncidentDate').value;
+  const policeInput = document.getElementById('theftPoliceReportNumber');
+  const dateInput = document.getElementById('theftIncidentDate');
+  const policeErrorEl = document.getElementById('theftPoliceReportNumberError');
+  const dateErrorEl = document.getElementById('theftIncidentDateError');
+  const resultEl = document.getElementById('theftResult');
+
+  const { valid, errors } = TheftClaimValidation.validateTheftClaimForm({ policeReportNumber, incidentDate });
+
+  policeErrorEl.textContent = errors.policeReportNumber || '';
+  policeInput.classList.toggle('invalid', Boolean(errors.policeReportNumber));
+  dateErrorEl.textContent = errors.incidentDate || '';
+  dateInput.classList.toggle('invalid', Boolean(errors.incidentDate));
+
+  if (!valid) {
+    resultEl.textContent = 'Popraw błędy w formularzu, aby kontynuować.';
+    return;
+  }
+
+  resultEl.textContent = 'Formularz poprawny — zgłoszenie kradzieży gotowe do wysłania.';
+}
+
 document.getElementById('effectiveDate').valueAsDate = new Date();
 document.getElementById('expiryDate').valueAsDate = new Date(Date.now() + 365 * 24 * 3600 * 1000);
 loadPolicies().catch(() => {});
diff --git a/src/PolicyPlatform.Api/wwwroot/theft-claim-validation.js b/src/PolicyPlatform.Api/wwwroot/theft-claim-validation.js
new file mode 100644
index 0000000..7f9e445
--- /dev/null
+++ b/src/PolicyPlatform.Api/wwwroot/theft-claim-validation.js
@@ -0,0 +1,57 @@
+// Walidacja formularza zgłoszenia kradzieży (AISDLC-40).
+// UMD-style export so this pure module is testable from Node without a build step.
+(function (root, factory) {
+  if (typeof module === 'object' && module.exports) {
+    module.exports = factory();
+  } else {
+    root.TheftClaimValidation = factory();
+  }
+})(typeof self !== 'undefined' ? self : this, function () {
+  // Numer zgłoszenia Policji: brak jednego oficjalnego standardu, więc akceptujemy
+  // typowe formaty ("L.dz. 123/26/RSD", "RSD-1234/26" itp.) — litery, cyfry,
+  // kropki, myślniki, ukośniki i spacje, min. 3 znaki, przynajmniej jedna cyfra.
+  const POLICE_REPORT_NUMBER_PATTERN = /^(?=.*\d)[A-Za-z0-9./ -]{3,40}$/;
+
+  const ERRORS = {
+    POLICE_REPORT_NUMBER_REQUIRED: 'Numer zgłoszenia Policji jest wymagany.',
+    POLICE_REPORT_NUMBER_INVALID_FORMAT:
+      'Nieprawidłowy format numeru zgłoszenia Policji (dozwolone litery, cyfry, kropki, myślniki, ukośniki, min. 3 znaki, w tym co najmniej jedna cyfra).',
+    INCIDENT_DATE_REQUIRED: 'Data kradzieży jest wymagana.',
+    INCIDENT_DATE_FUTURE: 'Data kradzieży nie może być w przyszłości.',
+  };
+
+  function validatePoliceReportNumber(value) {
+    const trimmed = (value ?? '').trim();
+    if (!trimmed) return ERRORS.POLICE_REPORT_NUMBER_REQUIRED;
+    if (!POLICE_REPORT_NUMBER_PATTERN.test(trimmed)) return ERRORS.POLICE_REPORT_NUMBER_INVALID_FORMAT;
+    return null;
+  }
+
+  function validateIncidentDate(value) {
+    const trimmed = (value ?? '').trim();
+    if (!trimmed) return ERRORS.INCIDENT_DATE_REQUIRED;
+    const date = new Date(trimmed);
+    if (Number.isNaN(date.getTime())) return ERRORS.INCIDENT_DATE_REQUIRED;
+    const endOfToday = new Date();
+    endOfToday.setHours(23, 59, 59, 999);
+    if (date.getTime() > endOfToday.getTime()) return ERRORS.INCIDENT_DATE_FUTURE;
+    return null;
+  }
+
+  function validateTheftClaimForm(form) {
+    const errors = {};
+    const policeReportNumberError = validatePoliceReportNumber(form.policeReportNumber);
+    if (policeReportNumberError) errors.policeReportNumber = policeReportNumberError;
+    const incidentDateError = validateIncidentDate(form.incidentDate);
+    if (incidentDateError) errors.incidentDate = incidentDateError;
+    return { valid: Object.keys(errors).length === 0, errors };
+  }
+
+  return {
+    POLICE_REPORT_NUMBER_PATTERN,
+    ERRORS,
+    validatePoliceReportNumber,
+    validateIncidentDate,
+    validateTheftClaimForm,
+  };
+});
diff --git a/src/PolicyPlatform.Api/wwwroot/theft-claim-validation.test.js b/src/PolicyPlatform.Api/wwwroot/theft-claim-validation.test.js
new file mode 100644
index 0000000..195fbae
--- /dev/null
+++ b/src/PolicyPlatform.Api/wwwroot/theft-claim-validation.test.js
@@ -0,0 +1,93 @@
+const { test } = require('node:test');
+const assert = require('node:assert/strict');
+const {
+  validatePoliceReportNumber,
+  validateIncidentDate,
+  validateTheftClaimForm,
+  ERRORS,
+} = require('./theft-claim-validation.js');
+
+test('validatePoliceReportNumber: rejects empty/missing values', () => {
+  assert.equal(validatePoliceReportNumber(''), ERRORS.POLICE_REPORT_NUMBER_REQUIRED);
+  assert.equal(validatePoliceReportNumber('   '), ERRORS.POLICE_REPORT_NUMBER_REQUIRED);
+  assert.equal(validatePoliceReportNumber(undefined), ERRORS.POLICE_REPORT_NUMBER_REQUIRED);
+  assert.equal(validatePoliceReportNumber(null), ERRORS.POLICE_REPORT_NUMBER_REQUIRED);
+});
+
+test('validatePoliceReportNumber: rejects too-short values', () => {
+  assert.equal(validatePoliceReportNumber('A1'), ERRORS.POLICE_REPORT_NUMBER_INVALID_FORMAT);
+});
+
+test('validatePoliceReportNumber: rejects values without a digit', () => {
+  assert.equal(validatePoliceReportNumber('ABC/RSD'), ERRORS.POLICE_REPORT_NUMBER_INVALID_FORMAT);
+});
+
+test('validatePoliceReportNumber: rejects disallowed characters', () => {
+  assert.equal(validatePoliceReportNumber('RSD#123'), ERRORS.POLICE_REPORT_NUMBER_INVALID_FORMAT);
+});
+
+test('validatePoliceReportNumber: rejects values over 40 characters', () => {
+  const tooLong = 'A1'.repeat(21); // 42 chars, contains digits
+  assert.equal(validatePoliceReportNumber(tooLong), ERRORS.POLICE_REPORT_NUMBER_INVALID_FORMAT);
+});
+
+test('validatePoliceReportNumber: accepts typical formats', () => {
+  assert.equal(validatePoliceReportNumber('L.dz. 123/26/RSD'), null);
+  assert.equal(validatePoliceReportNumber('RSD-1234/26'), null);
+  assert.equal(validatePoliceReportNumber('AB1'), null);
+});
+
+test('validatePoliceReportNumber: trims surrounding whitespace before validating', () => {
+  assert.equal(validatePoliceReportNumber('  RSD-1234/26  '), null);
+});
+
+test('validateIncidentDate: rejects empty/missing values', () => {
+  assert.equal(validateIncidentDate(''), ERRORS.INCIDENT_DATE_REQUIRED);
+  assert.equal(validateIncidentDate('   '), ERRORS.INCIDENT_DATE_REQUIRED);
+  assert.equal(validateIncidentDate(undefined), ERRORS.INCIDENT_DATE_REQUIRED);
+  assert.equal(validateIncidentDate(null), ERRORS.INCIDENT_DATE_REQUIRED);
+});
+
+test('validateIncidentDate: rejects unparseable dates', () => {
+  assert.equal(validateIncidentDate('not-a-date'), ERRORS.INCIDENT_DATE_REQUIRED);
+});
+
+test('validateIncidentDate: rejects future dates', () => {
+  const future = new Date(Date.now() + 7 * 24 * 3600 * 1000).toISOString().slice(0, 10);
+  a
... diff truncated ...
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