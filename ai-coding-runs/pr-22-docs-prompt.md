You are the DOCUMENTATION agent in a specialized worker pipeline (separate agents handle
coding, unit tests, e2e tests, database, and review — stay scoped to documentation only,
never touch executable logic).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under documentation:
- Repository: LordIllidan/ai-coding-demo
- PR: #22 AI: [AISDLC-137] Mobile: ekran transzy i stan pusty bez edycji
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/22
- Branch: ai-coding/aisdlc-137-mobile-ekran-transzy-i-stan-pusty-bez-edycji-29643132752

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-137-coding-prompt.md b/ai-coding-runs/aisdlc-137-coding-prompt.md
new file mode 100644
index 0000000..ad6ec2f
--- /dev/null
+++ b/ai-coding-runs/aisdlc-137-coding-prompt.md
@@ -0,0 +1,30 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-137 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: Mobile: ekran transzy i stan pusty bez edycji
+
+Task description:
+~~~markdown
+Parent story: AISDLC-119 — Komunikat, gdy klient nie ma wypłaconej transzy odszkodowania
+
+Podpiąć ekran mobilny do GET /api/v1/claims/{claimId}/payouts/last-paid-installment i wyrenderować stan PAID lub jeden pusty stan bez akcji edycji. Pliki: ekran/komponent szczegółów transzy, mapowanie odpowiedzi API, stan błędów/empty-state, testy UI. TODO: ukryć kwotę/datę/numer szkody przy NO_PAYOUT i INCOMPLETE_DATA, nie pokazywać placeholderów dla brakujących pól, zachować przekierowanie logowania i komunikaty dla 403/404/500.
+KONTRAKT: KONTRAKT (TechLeadAgent):
+Endpoint: GET /api/v1/claims/{claimId}/payouts/last-paid-installment. Path param claimId = UUID szkody; nie używamy customerId ani policyId do pobrania tego ekranu. Autoryzacja: Bearer token w nagłówku Authorization.
+Sukces 200 (tylko gdy da się pokazać pełne dane): { claimId: string(UUID), claimNumber: string, screenState: 'PAID', lastPaidInstallment: { installmentId: string(UUID), installmentNo: integer >= 1, paidAt: string(YYYY-MM-DD), amount: number(2 miejsca po przecinku), currency: string(ISO-4217) }, canEdit: false }. Frontend renderuje wartości tylko dla screenState='PAID' i tylko jeśli wszystkie pola lastPaidInstallment są niepuste.
+Brak wypłaty 200: { claimId: string(UUID), screenState: 'NO_PAYOUT', lastPaidInstallment: null, canEdit: false }. Frontend pokazuje jeden pusty stan z tekstem: 'Nie mamy jeszcze wypłaconej transzy odszkodowania.' i nie pokazuje kwoty, daty, numeru szkody ani akcji edycji. Jeśli backend wykryje rekord częściowy/brakujące pola, ma zwrócić screenState='INCOMPLETE_DATA' z lastPaidInstallment=null i tym samym zachowaniem UI (bez placeholderów i bez częściowych wartości).
+Błędy wspólne: 401 UNAUTHORIZED -> przekierowanie do logowania; 403 CLAIM_ACCESS_DENIED -> komunikat 'Brak uprawnień do danych szkody.'; 404 CLAIM_NOT_FOUND -> komunikat o braku danych; 500 CLAIM_PAYOUT_LOOKUP_FAILED -> komunikat techniczny/obserwowalność, bez danych na ekranie. Warstwa DB: filtrowanie po claims.id = claimId, wybór ostatniej wypłaty po paid_at DESC, installment_no DESC; nie wolno filtrować po policy_id.
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
diff --git a/ai-coding-runs/pr-22-unittest-prompt.md b/ai-coding-runs/pr-22-unittest-prompt.md
new file mode 100644
index 0000000..492a9af
--- /dev/null
+++ b/ai-coding-runs/pr-22-unittest-prompt.md
@@ -0,0 +1,311 @@
+You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
+coding, e2e tests, and review — stay scoped to unit-level test coverage only).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Pull request under test:
+- Repository: LordIllidan/ai-coding-demo
+- PR: #22 AI: [AISDLC-137] Mobile: ekran transzy i stan pusty bez edycji
+- URL: https://github.com/LordIllidan/ai-coding-demo/pull/22
+- Branch: ai-coding/aisdlc-137-mobile-ekran-transzy-i-stan-pusty-bez-edycji-29643132752
+
+Diff introduced by this PR:
+~~~diff
+diff --git a/ai-coding-runs/aisdlc-137-coding-prompt.md b/ai-coding-runs/aisdlc-137-coding-prompt.md
+new file mode 100644
+index 0000000..ad6ec2f
+--- /dev/null
++++ b/ai-coding-runs/aisdlc-137-coding-prompt.md
+@@ -0,0 +1,30 @@
++You are the CODING agent in a specialized worker pipeline (separate agents exist for
++unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
++Running locally through a GitHub self-hosted runner (Windows).
++
++Source of truth: Jira issue AISDLC-137 (this task has NO corresponding GitHub issue —
++Jira is the only tracker; do not create or reference a GitHub issue).
++
++Task title: Mobile: ekran transzy i stan pusty bez edycji
++
++Task description:
++~~~markdown
++Parent story: AISDLC-119 — Komunikat, gdy klient nie ma wypłaconej transzy odszkodowania
++
++Podpiąć ekran mobilny do GET /api/v1/claims/{claimId}/payouts/last-paid-installment i wyrenderować stan PAID lub jeden pusty stan bez akcji edycji. Pliki: ekran/komponent szczegółów transzy, mapowanie odpowiedzi API, stan błędów/empty-state, testy UI. TODO: ukryć kwotę/datę/numer szkody przy NO_PAYOUT i INCOMPLETE_DATA, nie pokazywać placeholderów dla brakujących pól, zachować przekierowanie logowania i komunikaty dla 403/404/500.
++KONTRAKT: KONTRAKT (TechLeadAgent):
++Endpoint: GET /api/v1/claims/{claimId}/payouts/last-paid-installment. Path param claimId = UUID szkody; nie używamy customerId ani policyId do pobrania tego ekranu. Autoryzacja: Bearer token w nagłówku Authorization.
++Sukces 200 (tylko gdy da się pokazać pełne dane): { claimId: string(UUID), claimNumber: string, screenState: 'PAID', lastPaidInstallment: { installmentId: string(UUID), installmentNo: integer >= 1, paidAt: string(YYYY-MM-DD), amount: number(2 miejsca po przecinku), currency: string(ISO-4217) }, canEdit: false }. Frontend renderuje wartości tylko dla screenState='PAID' i tylko jeśli wszystkie pola lastPaidInstallment są niepuste.
++Brak wypłaty 200: { claimId: string(UUID), screenState: 'NO_PAYOUT', lastPaidInstallment: null, canEdit: false }. Frontend pokazuje jeden pusty stan z tekstem: 'Nie mamy jeszcze wypłaconej transzy odszkodowania.' i nie pokazuje kwoty, daty, numeru szkody ani akcji edycji. Jeśli backend wykryje rekord częściowy/brakujące pola, ma zwrócić screenState='INCOMPLETE_DATA' z lastPaidInstallment=null i tym samym zachowaniem UI (bez placeholderów i bez częściowych wartości).
++Błędy wspólne: 401 UNAUTHORIZED -> przekierowanie do logowania; 403 CLAIM_ACCESS_DENIED -> komunikat 'Brak uprawnień do danych szkody.'; 404 CLAIM_NOT_FOUND -> komunikat o braku danych; 500 CLAIM_PAYOUT_LOOKUP_FAILED -> komunikat techniczny/obserwowalność, bez danych na ekranie. Warstwa DB: filtrowanie po claims.id = claimId, wybór ostatniej wypłaty po paid_at DESC, installment_no DESC; nie wolno filtrować po policy_id.
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
+index 25a5623..85a50f5 100644
+--- a/src/PolicyPlatform.Api/wwwroot/index.html
++++ b/src/PolicyPlatform.Api/wwwroot/index.html
+@@ -72,9 +72,17 @@ <h2>4. Zgłoszenie kradzieży</h2>
+   <div id="theftResult"></div>
+ </section>
+ 
++<section>
++  <h2>5. Transza odszkodowania (ekran mobilny)</h2>
++  <label>ID szkody (claimId) <input id="payoutClaimId" placeholder="UUID szkody" /></label>
++  <button onclick="loadLastPaidInstallment()">Pokaż transzę</button>
++  <div id="payoutResult"></div>
++</section>
++
+ <div id="log"></div>
+ 
+ <script src="theft-claim-validation.js"></script>
++<script src="last-paid-installment.js"></script>
+ <script>
+ const log = (msg) => { document.getElementById('log').textContent += msg + "\n"; };
+ 
+@@ -169,6 +177,54 @@ <h2>4. Zgłoszenie kradzieży</h2>
+   resultEl.textContent = 'Formularz poprawny — zgłoszenie kradzieży gotowe do wysłania.';
+ }
+ 
++async function loadLastPaidInstallment() {
++  const claimId = document.getElementById('payoutClaimId').value.trim();
++  const resultEl = document.getElementById('payoutResult');
++  resultEl.textContent = '';
++  if (!claimId) { resultEl.textContent = 'Podaj identyfikator szkody.'; return; }
++
++  let res;
++  try {
++    const token = localStorage.getItem('authToken');
++    res = await fetch(`/api/v1/claims/${encodeURIComponent(claimId)}/payouts/last-paid-installment`, {
++      headers: token ? { Authorization: `Bearer ${token}` } : {},
++    });
++  } catch (e) {
++    resultEl.textContent = LastPaidInstallment.ERROR_MESSAGES.CLAIM_PAYOUT_LOOKUP_FAILED;
++    log(`Payout lookup failed: ${e.message}`);
++    return;
++  }
++
++  const text = await res.text();
++  const body = text ? JSON.parse(text) : null;
++
++  if (res.status === 401) {
++    window.location.href = '/login.html';
++    return;
++  }
++  if (res.status !== 200) {
++    const err = LastPaidInstallment.mapErrorResponse(res.status, body);
++    resultEl.textContent = err.text;
++    log(`Payout lookup ${res.status}: ${JSON.stringify(body)}`);
++    return;
++  }
++
++  const view = LastPaidInstallment.mapPayoutResponse(body);
++  if (view.screenState === 'PAID') {
++    resultEl.innerHTML = `
++      <table>
++        <tbody>
++          <tr><th>Numer szkody</th><td>${view.claimNumber}</td></tr>
++          <tr><th>Nr transzy</th><td>${view.installment.installmentNo}</td></tr>
++          <tr><th>Data wypłaty</th><td>${view.installment.paidAt}</td></tr>
++          <tr><th>Kwota</th><td>${LastPaidInstallment.formatAmount(view.installment)}</td></tr>
++        </tbody>
++      </table>`;
++  } else {
++    resultEl.textContent = view.message;
++  }
++}
++
+ document.getElementById('effectiveDate').valueAsDate = new Date();
+ document.getElementById('expiryDate').valueAsDate = new Date(Date.now() + 365 * 24 * 3600 * 1000);
+ loadPolicies().catch(() => {});
+diff --git a/src/PolicyPlatform.Api/wwwroot/last-paid-installment.js b/src/PolicyPlatform.Api/wwwroot/last-paid-installment.js
+new file mode 100644
+index 0000000..c2283ce
+--- /dev/null
++++ b/src/PolicyPlatform.Api/wwwroot/last-paid-installment.js
+@@ -0,0 +1,73 @@
++// Ekran mobilny "Transza odszkodowania" (AISDLC-137, parent story AISDLC-119).
++// UMD-style export so this pure module is testable from Node without a build step.
++(function (root, factory) {
++  if (typeof module === 'object' && module.exports) {
++    module.exports = factory();
++  } else {
++    root.LastPaidInstallment = factory();
++  }
++})(typeof self !== 'undefined' ? self : this, function () {
++  const EMPTY_STATE_MESSAGE = 'Nie mamy jeszcze wypłaconej transzy odszkodowania.';
++
++  const ERROR_MESSAGES = {
++    CLAIM_ACCESS_DENIED: 'Brak uprawnień do danych szkody.',
++    CLAIM_NOT_FOUND: 'Nie znaleziono danych szkody.',
++    CLAIM_PAYOUT_LOOKUP_FAILED: 'Wystąpił błąd techniczny. Spróbuj ponownie później.',
++    UNKNOWN: 'Wystąpił nieoczekiwany błąd.',
++  };
++
++  const REQUIRED_INSTALLMENT_FIELDS = ['installmentId', 'installmentNo', 'paidAt', 'amount', 'currency'];
++
++  // Backend contract: values are only trustworthy for screenState='PAID' and only
++  // when every lastPaidInstallment field is present — never render placeholders
++  // for a partial record (NO_PAYOUT and INCOMPLETE_DATA share the same empty UI).
++  function isCompleteInstallment(installment) {
++    if (!installment || typeof installment !== 'object') return false;
++    return REQUIRED_INSTALLMENT_FIELDS.every((field) => {
++      const value = installment[field];
++      return value !== null && value !== undefined && value !== '';
++    });
++  }
++
++  function mapPayoutResponse(body) {
++    if (body && body.screenState === 'PAID' && isCompleteInstallment(body.lastPaidInstallment)) {
++      const i = body.lastPaidInstallment;
++      return {
++        screenState: 'PAID',
++        claimNumber: body.claimNumber,
++        installment: {
++          installmentId: i.installmentId,
++          installmentNo: i.installmentNo,
++          paidAt: i.paidAt,
++          amount: i.amount,
++          currency: i.currency,
++        },
++        canEdit: false,
++        message: null,
++      };
++    }
++    return { screenState: 'EMPTY', claimNumber: null, installment: null, canEdit: false, message: EMPTY_STATE_MESSAGE };
++  }
++
++  function mapErrorResponse(status, body) {
++    if (status === 401) return { kind: 'redirect-login' };
++    const code = body && body.code;
++    if (status === 403) return { kind: 'message', code: 'CLAIM_ACCESS_DENIED', text: ERROR_MESSAGES.CLAIM_ACCESS_DENIED };
++    if (status === 404) return { kind: 'message', code: 'CLAIM_NOT_FOUND', text: ERROR_MESSAGES.CLAIM_NOT_FOUND };
++    if (status === 500) return { kind: 'message', code: 'CLAIM_PAYOUT_LOOKUP_FAILED', text: ERROR_MESSAGES.CLAIM_PAYOUT_LOOKUP_FAILED };
++    return { kind: 'message', code: code || 'UNKNOWN', text: ERROR_MESSAGES.UNKNOWN };
++  }
++
++  function formatAmount(installment) {
++    return `${installment.amount.toFixed(2)} ${installment.currency}`;
++  }
++
++  return {
++    EMPTY_STATE_MESSAGE,
++    ERROR_MESSAGES,
++    isCompleteInstallment,
++    mapPayoutResponse,
++    mapErrorResponse,
++    formatAmount,
++  };
++});
+diff --git a/src/PolicyPlatform.Api/wwwroot/last-paid-installment.test.js b/src/PolicyPlatform.Api/wwwroot/last-paid-installment.test.js
+new file mode 100644
+index 0000000..1dd62fa
+--- /dev/null
++++ b/src/PolicyPlatform.Api/wwwroot/last-paid-installment.test.js
+@@ -0,0 +1,87 @@
++const { test } = require('node:test');
++const assert = require('node:assert/strict');
++const {
++  isCompleteInstallment,
++  mapPayoutResponse,
++  mapErrorResponse,
++  EMPTY_STATE_MESSAGE,
++} = require('./last-paid-installment.js');
++
++const FULL_INSTALLMENT = {
++  installmentId: '9c6b1a1e-1111-4a1a-9a1a-000000000001',
++  installmentNo: 1,
++  paidAt: '2026-06-01',
++  amount: 1234.56,
++  currency: 'PLN',
++};
++
++test('mapPayoutResponse: renders PAID when screenState is PAID and all fields present', () => {
++  const result = mapPayoutResponse({
++    claimId: 'c1',
++    claimNumber: 'SZK/2026/001',
++    screenState: 'PAID',
++    lastPaidInstallment: FULL_INSTALLMENT,
++    canEdit: false,
++  });
++  assert.equal(result.screenState, 'PAID');
++  assert.equal(result.claimNumber, 'SZK/2026/001');
++  assert.deepEqual(result.installment, FULL_INSTALLMENT);
++  assert.equal(result.canEdit, false);
++});
++
++test('mapPayoutResponse: NO_PAYOUT yields empty state with fixed message, no data', () => {
++  const result = mapPayoutResponse({ claimId: 'c1', screenState: 'NO_PAYOUT', lastPaidInstallment: null, canEdit: false });
++  assert.equal(result.screenState, 'EMPTY');
++  assert.equal(result.installment, null);
++  assert.equal(result.claimNumber, null);
++  assert.equal(result.message, EMPTY_STATE_MESSAGE);
++});
++
++test('mapPayoutResponse: INCOMPLETE_DATA yields the same empty state as NO_PAYOUT', () => {
++  const result = mapPayoutResponse({ claimId: 'c1', screenState: 'INCOMPLETE_DATA', lastPaidInstallment: null, canEdit: false });
++  assert.equal(result.screenState, 'EMPTY');
++  assert.equal(result.message, EMPTY_STATE_MESSAGE);
++});
++
++test('mapPayoutResponse: PAID with a missing field falls back to empty state (no placeholders)', () => {
++  for (const field of Object.keys(FULL_INSTALLMENT)) {
++    const partial = { ...FULL_INSTALLMENT, [field]: '' };
++    const result = mapPayoutResponse({ claimId: 'c1', screenState: 'PAID', lastPaidInstallment: partial, canEdit: false });
++    assert.equal(result.screenState, 'EMPTY', `expected empty state when ${field} is blank`);
++    assert.equal(result.installment, null);
++  }
++});
++
++test('mapPayoutResponse: PAID with null lastPaidInstallment falls back to empty state', () => {
++  const result = mapPayoutResponse({ claimId: 'c1', screenState: 'PAID', lastPaidInstallment: null, canEdit: false });
++  assert.equal(result.screenState, 'EMPTY');
++});
++
++test('isCompleteInstallment: rejects missing/null/undefined/empty-string fields', () => {
++  assert.equal(isCompleteInstallment(null), false);
++  assert.equal(isCompleteInstallment({}), false);
++  assert.equal(isCompleteInstallment({ ...FULL_INSTALLMENT, amount: undefined }), false);
++  assert.equal(isCompleteInstallment(FULL_INSTALLMENT), true);
++});
++
++test('mapErrorResponse: 401 signals a login redirect', () => {
++  assert.deepEqual(mapErrorResponse(401, null), { kind: 'redirect-login' });
++});
++
++test('mapErrorResponse: 403 maps to CLAIM_ACCESS_DENIED message', () => {
++  const result = mapErrorResponse(403, { code: 'CLAIM_ACCESS_DENIED' });
++  assert.equal(result.kind, 'message');
++  assert.equal(result.text, 'Brak uprawnień do danych szkody.');
++});
++
++test('mapErrorResponse: 404 maps to a not-found message', () => {
++  const result = mapErrorResponse(404, { code: 'CLAIM_NOT_FOUND' });
++  assert.equal(result.kind, 'message');
++  assert.equal(result.text, 'Nie znaleziono danych szkody.');
++});
++
++test('mapErrorResponse: 500 maps to a technical message without exposing details', () => {
++  const result = mapErrorResponse(500, { code: 'CLAIM_PAYOUT_LOOKUP_FAILED' });
++  assert.equal(result.kind, 'message');
++  assert.equal(result.text, 'Wystąpił błąd techniczny. Spróbuj ponownie później.');
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
index 25a5623..85a50f5 100644
--- a/src/PolicyPlatform.Api/wwwroot/index.html
+++ b/src/PolicyPlatform.Api/wwwroot/index.html
@@ -72,9 +72,17 @@ <h2>4. Zgłoszenie kradzieży</h2>
   <div id="theftResult"></div>
 </section>
 
+<section>
+  <h2>5. Transza odszkodowania (ekran mobilny)</h2>
+  <label>ID szkody (claimId) <input id="payoutClaimId" placeholder="UUID szkody" /></label>
+  <button onclick="loadLastPaidInstallment()">Pokaż transzę</button>
+  <div id="payoutResult"></div>
+</section>
+
 <div id="log"></div>
 
 <script src="theft-claim-validation.js"></script>
+<script src="last-paid-installment.js"></
... diff truncated ...
~~~

Standards to follow (do not invent your own format):

1. XML documentation comments (Microsoft C# standard, ///) on every public type and public
   member introduced or changed in this diff — Domain entities/value objects, Application
   service methods, Api controller actions, McpServer tools. <summary> is mandatory;
   <param>/<returns>/<exception> where applicable. Follow the tone already used in this repo
   if any XML doc comments already exist — otherwise establish it consistently.
2. If this diff introduces a genuinely new architectural decision (new persistence
   technology, new external dependency, a pattern that constrains future work), add an
   Architecture Decision Record under docs/adr/NNNN-title-in-kebab-case.md using the
   Michael Nygard ADR format: Title, Status (Proposed/Accepted), Context, Decision,
   Consequences. Number sequentially from the highest existing ADR in docs/adr/ (start at
   0001 if the directory does not exist yet). Do NOT write an ADR for routine feature work
   that doesn't change architecture — most PRs will not need one.
3. Update README.md ONLY if this diff changes something a reader of the README would need
   to know (new project, new setup step, new public API surface worth mentioning) — do not
   pad README with routine changes.
4. Do NOT modify Domain/Application/Infrastructure business logic, tests, or CI — only
   doc comments (which live inside source files but change no behavior), docs/, and README.md.
5. Do not merge, push, or create/edit pull requests — the wrapper script handles that.
6. Do not read or print secrets. Avoid destructive git commands.

Output: short summary of what got documented (XML comments added to which types, any ADR
written, any README change) or "no documentation gap found" if the diff is already
adequately documented.