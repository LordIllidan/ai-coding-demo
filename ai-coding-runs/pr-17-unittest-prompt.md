You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: LordIllidan/ai-coding-demo
- PR: #17 AI: [AISDLC-135] Pokazać komunikat błędu i przycisk Ponów po nieudanym pobraniu transzy
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/17
- Branch: ai-coding/aisdlc-135-pokaza-komunikat-b-du-i-przycisk-pon-w-po-nieuda-29643060029

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-135-coding-prompt.md b/ai-coding-runs/aisdlc-135-coding-prompt.md
new file mode 100644
index 0000000..49538c7
--- /dev/null
+++ b/ai-coding-runs/aisdlc-135-coding-prompt.md
@@ -0,0 +1,31 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-135 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: Pokazać komunikat błędu i przycisk Ponów po nieudanym pobraniu transzy
+
+Task description:
+~~~markdown
+Parent story: AISDLC-120 — Komunikat błędu i ponowienia, gdy nie uda się pobrać danych transzy
+
+Implementacja frontendu dla scenariusza błędu pobrania danych transzy: po non-2xx czyścić aktualnie widoczne dane, pokazać komunikat błędu i przycisk "Ponów", który wykonuje ten sam GET dla tego samego claimId. Pliki do sprawdzenia: ekran/komponent transzy, hook lub store odpowiedzialny za pobieranie danych, klient API oraz testy UI. TODO: upewnić się, że widok nie prezentuje nieaktualnych danych jako poprawnych i że retry odświeża stan po ponownym żądaniu.
+KONTRAKT: KONTRAKT (TechLeadAgent):
+Endpoint: GET /api/claims/{claimId}/last-paid-tranche. Request przyjmuje wyłącznie path param claimId: string (UUID) oraz nagłówek Authorization: Bearer <token>; nie wolno używać customerId ani policyId w request ani w logice mapowania.
+200 OK: { claimId: string(UUID), lastPaidTranche: { trancheId: string(UUID), trancheNumber: integer, status: 'PAID', paidAt: string(ISO-8601), grossAmount: number(2 dp), currency: string(ISO-4217) } | null, fetchedAt: string(ISO-8601) }. Przy braku danych lastPaidTranche = null.
+Kody błędów: 401 INVALID_TOKEN (brak/wygaśnięty token), 403 CLAIM_ACCESS_DENIED (brak scope do claimId), 404 CLAIM_NOT_FOUND, 503 TRANCHE_SERVICE_UNAVAILABLE (circuit breaker/downstream unavailable), 504 TRANCHE_SERVICE_TIMEOUT (przekroczony timeout integracji). Wspólny error envelope: { code: string, message: string, retryable: boolean, correlationId: string }.
+Walidacje i zachowanie UI: claimId obowiązkowy i musi być UUID; backend nie może zwracać danych z cache/starych odpowiedzi po timeout/circuit breaker; frontend po każdym non-2xx czyści aktualnie widoczne dane, pokazuje komunikat błędu i przycisk 'Ponów' wywołujący ponownie ten sam GET dla tego samego claimId.
+DB/read model: claim_last_paid_tranche_view(claim_id PK, tranche_id, tranche_number, status, paid_at, gross_amount, currency, source_updated_at, refreshed_at). Kolumny identyfikacyjne i relacyjne opierają się na claim_id, nie na customer_id/policy_id.
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
diff --git a/src/PolicyPlatform.Api/wwwroot/index.html b/src/PolicyPlatform.Api/wwwroot/index.html
index 25a5623..d6e687a 100644
--- a/src/PolicyPlatform.Api/wwwroot/index.html
+++ b/src/PolicyPlatform.Api/wwwroot/index.html
@@ -72,9 +72,20 @@ <h2>4. Zgłoszenie kradzieży</h2>
   <div id="theftResult"></div>
 </section>
 
+<section>
+  <h2>5. Ostatnia wypłacona transza</h2>
+  <label>Claim ID (UUID) <input id="trancheClaimId" placeholder="np. 3fa85f64-5717-4562-b3fc-2c963f66afa6" /></label>
+  <label>Token (Bearer) <input id="trancheToken" placeholder="opcjonalny token dema" /></label>
+  <button onclick="loadLastPaidTranche()">Pobierz</button>
+  <div id="trancheResult"></div>
+  <div id="trancheError" class="field-error"></div>
+  <button id="trancheRetryButton" onclick="retryLastPaidTranche()" style="display:none;">Ponów</button>
+</section>
+
 <div id="log"></div>
 
 <script src="theft-claim-validation.js"></script>
+<script src="last-paid-tranche-client.js"></script>
 <script>
 const log = (msg) => { document.getElementById('log').textContent += msg + "\n"; };
 
@@ -169,6 +180,50 @@ <h2>4. Zgłoszenie kradzieży</h2>
   resultEl.textContent = 'Formularz poprawny — zgłoszenie kradzieży gotowe do wysłania.';
 }
 
+let lastRequestedTrancheClaimId = null;
+
+function renderTrancheResult(data) {
+  const t = data.lastPaidTranche;
+  document.getElementById('trancheResult').textContent = t
+    ? `Transza #${t.trancheNumber}: ${t.grossAmount.toFixed(2)} ${t.currency}, wypłacono ${t.paidAt} (${t.status})`
+    : 'Brak wypłaconych transz dla tego zgłoszenia.';
+}
+
+function renderTrancheError(error) {
+  document.getElementById('trancheResult').textContent = '';
+  document.getElementById('trancheError').textContent = `${error.message} (${error.code})`;
+  document.getElementById('trancheRetryButton').style.display = error.retryable ? 'inline-block' : 'none';
+}
+
+async function loadLastPaidTrancheFor(claimId) {
+  lastRequestedTrancheClaimId = claimId;
+  // Czyścimy poprzednie dane/błąd zanim pokażemy wynik nowego żądania, żeby nigdy
+  // nie prezentować nieaktualnych danych jako aktualnych podczas ładowania.
+  document.getElementById('trancheResult').textContent = '';
+  document.getElementById('trancheError').textContent = '';
+  document.getElementById('trancheRetryButton').style.display = 'none';
+
+  const token = document.getElementById('trancheToken').value.trim() || undefined;
+  const result = await LastPaidTrancheClient.fetchLastPaidTranche(claimId, { token });
+
+  if (!result.ok) {
+    renderTrancheError(result.error);
+    return;
+  }
+  renderTrancheResult(result.data);
+}
+
+function loadLastPaidTranche() {
+  const claimId = document.getElementById('trancheClaimId').value.trim();
+  loadLastPaidTrancheFor(claimId);
+}
+
+function retryLastPaidTranche() {
+  // Ponów wykonuje dokładnie to samo żądanie (ten sam claimId), niezależnie od
+  // tego, co użytkownik mógł od tej pory wpisać w polu Claim ID.
+  loadLastPaidTrancheFor(lastRequestedTrancheClaimId);
+}
+
 document.getElementById('effectiveDate').valueAsDate = new Date();
 document.getElementById('expiryDate').valueAsDate = new Date(Date.now() + 365 * 24 * 3600 * 1000);
 loadPolicies().catch(() => {});
diff --git a/src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.js b/src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.js
new file mode 100644
index 0000000..399f852
--- /dev/null
+++ b/src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.js
@@ -0,0 +1,56 @@
+// Klient API dla GET /api/claims/{claimId}/last-paid-tranche (AISDLC-135 / AISDLC-120).
+// UMD-style export so this pure module is testable from Node without a build step.
+(function (root, factory) {
+  if (typeof module === 'object' && module.exports) {
+    module.exports = factory();
+  } else {
+    root.LastPaidTrancheClient = factory();
+  }
+})(typeof self !== 'undefined' ? self : this, function () {
+  const GENERIC_ERROR_MESSAGE = 'Nie udało się pobrać danych ostatniej wypłaconej transzy. Spróbuj ponownie.';
+
+  function safeParseJson(text) {
+    if (!text) return null;
+    try {
+      return JSON.parse(text);
+    } catch {
+      return null;
+    }
+  }
+
+  // Zwraca { ok: true, data } albo { ok: false, error: { code, message, retryable, correlationId } }.
+  // Nigdy nie rzuca wyjątku — wywołujący ma zawsze dostać jednoznaczny wynik do wyrenderowania.
+  async function fetchLastPaidTranche(claimId, options) {
+    const { fetchImpl = fetch, token } = options || {};
+    const headers = {};
+    if (token) headers.Authorization = `Bearer ${token}`;
+
+    let res;
+    try {
+      res = await fetchImpl(`/api/claims/${encodeURIComponent(claimId)}/last-paid-tranche`, { headers });
+    } catch {
+      return {
+        ok: false,
+        error: { code: 'NETWORK_ERROR', message: GENERIC_ERROR_MESSAGE, retryable: true, correlationId: null },
+      };
+    }
+
+    const body = safeParseJson(await res.text());
+
+    if (!res.ok) {
+      return {
+        ok: false,
+        error: {
+          code: body?.code || `HTTP_${res.status}`,
+          message: body?.message || GENERIC_ERROR_MESSAGE,
+          retryable: body?.retryable ?? true,
+          correlationId: body?.correlationId || null,
+        },
+      };
+    }
+
+    return { ok: true, data: body };
+  }
+
+  return { fetchLastPaidTranche, GENERIC_ERROR_MESSAGE };
+});
diff --git a/src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.test.js b/src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.test.js
new file mode 100644
index 0000000..c67d6f5
--- /dev/null
+++ b/src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.test.js
@@ -0,0 +1,54 @@
+const { test } = require('node:test');
+const assert = require('node:assert/strict');
+const { fetchLastPaidTranche, GENERIC_ERROR_MESSAGE } = require('./last-paid-tranche-client.js');
+
+function fakeFetch(response) {
+  return async () => response;
+}
+
+test('fetchLastPaidTranche: returns data on 200 OK', async () => {
+  const data = { claimId: 'c1', lastPaidTranche: null, fetchedAt: '2026-07-18T00:00:00Z' };
+  const result = await fetchLastPaidTranche('c1', {
+    fetchImpl: fakeFetch({ ok: true, status: 200, text: async () => JSON.stringify(data) }),
+  });
+  assert.deepEqual(result, { ok: true, data });
+});
+
+test('fetchLastPaidTranche: maps non-2xx envelope to a normalized error', async () => {
+  const envelope = { code: 'CLAIM_NOT_FOUND', message: 'Brak zgłoszenia', retryable: false, correlationId: 'corr-1' };
+  const result = await fetchLastPaidTranche('missing', {
+    fetchImpl: fakeFetch({ ok: false, status: 404, text: async () => JSON.stringify(envelope) }),
+  });
+  assert.deepEqual(result, { ok: false, error: envelope });
+});
+
+test('fetchLastPaidTranche: falls back to a generic message when the error body is missing/unparsable', async () => {
+  const result = await fetchLastPaidTranche('c1', {
+    fetchImpl: fakeFetch({ ok: false, status: 503, text: async () => '' }),
+  });
+  assert.equal(result.ok, false);
+  assert.equal(result.error.code, 'HTTP_503');
+  assert.equal(result.error.message, GENERIC_ERROR_MESSAGE);
+  assert.equal(result.error.retryable, true);
+});
+
+test('fetchLastPaidTranche: network failure is treated as a retryable error, not a thrown exception', async () => {
+  const result = await fetchLastPaidTranche('c1', {
+    fetchImpl: async () => { throw new TypeError('failed to fetch'); },
+  });
+  assert.equal(result.ok, false);
+  assert.equal(result.error.code, 'NETWORK_ERROR');
+  assert.equal(result.error.retryable, true);
+});
+
+test('fetchLastPaidTranche: sends Authorization header when a token is provided', async () => {
+  let capturedHeaders;
+  await fetchLastPaidTranche('c1', {
+    token: 'abc',
+    fetchImpl: async (_url, opts) => {
+      capturedHeaders = opts.headers;
+      return { ok: true, status: 200, text: async () => JSON.stringify({ claimId: 'c1', lastPaidTranche: null, fetchedAt: 'x' }) };
+    },
+  });
+  assert.equal(capturedHeaders.Authorization, 'Bearer abc');
+});

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