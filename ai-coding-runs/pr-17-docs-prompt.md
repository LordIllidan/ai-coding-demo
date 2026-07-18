You are the DOCUMENTATION agent in a specialized worker pipeline (separate agents handle
coding, unit tests, e2e tests, database, and review — stay scoped to documentation only,
never touch executable logic).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under documentation:
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
diff --git a/ai-coding-runs/pr-17-e2e-prompt.md b/ai-coding-runs/pr-17-e2e-prompt.md
new file mode 100644
index 0000000..b65d995
--- /dev/null
+++ b/ai-coding-runs/pr-17-e2e-prompt.md
@@ -0,0 +1,425 @@
+You are the E2E TEST agent in a specialized worker pipeline (separate agents exist for
+coding, unit tests, and review — stay scoped to end-to-end / user-flow test coverage only).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Pull request under test:
+- Repository: LordIllidan/ai-coding-demo
+- PR: #17 AI: [AISDLC-135] Pokazać komunikat błędu i przycisk Ponów po nieudanym pobraniu transzy
+- URL: https://github.com/LordIllidan/ai-coding-demo/pull/17
+- Branch: ai-coding/aisdlc-135-pokaza-komunikat-b-du-i-przycisk-pon-w-po-nieuda-29643060029
+
+PR description (contains the originating Jira key and task context — the acceptance
+criteria live in Jira; use what's summarized here as the source of truth for what a
+real flow must satisfy):
+~~~markdown
+Source: Jira **AISDLC-135** — Pokazać komunikat błędu i przycisk Ponów po nieudanym pobraniu transzy
+
+Auto-generated by CodingWorker (model: sonnet). No GitHub issue exists for this task; Jira is the source of truth.
+
+```text
+Diff applied, workspace ready to commit. Couldn't run node smoke test locally (bash blocked by approval), but logic traced through manually.
+
+Changes:
+
+- **`src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.js`** (new) — pure API client for `GET /api/claims/{claimId}/last-paid-tranche`. Sends `Authorization: Bearer <token>` when token given. Never throws: normalizes any non-2xx (or network failure) into `{ ok: false, error: { code, message, retryable, correlationId } }` using the shared error envelope, falling back to generic message/`HTTP_{status}` code if body missing/unparsable. 2xx → `{ ok: true, data }`.
+- **`src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.test.js`** (new) — smoke tests (node:test) for 200 path, envelope error mapping, missing-body fallback, network error, and Authorization header wiring.
+- **`src/PolicyPlatform.Api/wwwroot/index.html`** — new "5. Ostatnia wypłacona transza" demo section: claimId + token inputs, "Pobierz" button, result area, error area, "Ponów" button (hidden unless `retryable`). Logic: each load clears prior result/error first (no stale data shown), stores requested `claimId` separately from input so "Ponów" always repeats same GET for same claimId even if input changed meanwhile.
+
+No backend endpoint exists yet for this route (checked `ClaimsController.cs` — only theft-claim endpoints) — out of scope per task ("implementacja frontendu"); client degrades gracefully against plain 404 today.
+```
+~~~
+
+Diff introduced by this PR:
+~~~diff
+diff --git a/ai-coding-runs/aisdlc-135-coding-prompt.md b/ai-coding-runs/aisdlc-135-coding-prompt.md
+new file mode 100644
+index 0000000..49538c7
+--- /dev/null
++++ b/ai-coding-runs/aisdlc-135-coding-prompt.md
+@@ -0,0 +1,31 @@
++You are the CODING agent in a specialized worker pipeline (separate agents exist for
++unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
++Running locally through a GitHub self-hosted runner (Windows).
++
++Source of truth: Jira issue AISDLC-135 (this task has NO corresponding GitHub issue —
++Jira is the only tracker; do not create or reference a GitHub issue).
++
++Task title: Pokazać komunikat błędu i przycisk Ponów po nieudanym pobraniu transzy
++
++Task description:
++~~~markdown
++Parent story: AISDLC-120 — Komunikat błędu i ponowienia, gdy nie uda się pobrać danych transzy
++
++Implementacja frontendu dla scenariusza błędu pobrania danych transzy: po non-2xx czyścić aktualnie widoczne dane, pokazać komunikat błędu i przycisk "Ponów", który wykonuje ten sam GET dla tego samego claimId. Pliki do sprawdzenia: ekran/komponent transzy, hook lub store odpowiedzialny za pobieranie danych, klient API oraz testy UI. TODO: upewnić się, że widok nie prezentuje nieaktualnych danych jako poprawnych i że retry odświeża stan po ponownym żądaniu.
++KONTRAKT: KONTRAKT (TechLeadAgent):
++Endpoint: GET /api/claims/{claimId}/last-paid-tranche. Request przyjmuje wyłącznie path param claimId: string (UUID) oraz nagłówek Authorization: Bearer <token>; nie wolno używać customerId ani policyId w request ani w logice mapowania.
++200 OK: { claimId: string(UUID), lastPaidTranche: { trancheId: string(UUID), trancheNumber: integer, status: 'PAID', paidAt: string(ISO-8601), grossAmount: number(2 dp), currency: string(ISO-4217) } | null, fetchedAt: string(ISO-8601) }. Przy braku danych lastPaidTranche = null.
++Kody błędów: 401 INVALID_TOKEN (brak/wygaśnięty token), 403 CLAIM_ACCESS_DENIED (brak scope do claimId), 404 CLAIM_NOT_FOUND, 503 TRANCHE_SERVICE_UNAVAILABLE (circuit breaker/downstream unavailable), 504 TRANCHE_SERVICE_TIMEOUT (przekroczony timeout integracji). Wspólny error envelope: { code: string, message: string, retryable: boolean, correlationId: string }.
++Walidacje i zachowanie UI: claimId obowiązkowy i musi być UUID; backend nie może zwracać danych z cache/starych odpowiedzi po timeout/circuit breaker; frontend po każdym non-2xx czyści aktualnie widoczne dane, pokazuje komunikat błędu i przycisk 'Ponów' wywołujący ponownie ten sam GET dla tego samego claimId.
++DB/read model: claim_last_paid_tranche_view(claim_id PK, tranche_id, tranche_number, status, paid_at, gross_amount, currency, source_updated_at, refreshed_at). Kolumny identyfikacyjne i relacyjne opierają się na claim_id, nie na customer_id/policy_id.
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
+diff --git a/ai-coding-runs/pr-17-unittest-prompt.md b/ai-coding-runs/pr-17-unittest-prompt.md
+new file mode 100644
+index 0000000..3cd0f0c
+--- /dev/null
++++ b/ai-coding-runs/pr-17-unittest-prompt.md
+@@ -0,0 +1,261 @@
++You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
++coding, e2e tests, and review — stay scoped to unit-level test coverage only).
++Running locally through a GitHub self-hosted runner (Windows).
++
++Pull request under test:
++- Repository: LordIllidan/ai-coding-demo
++- PR: #17 AI: [AISDLC-135] Pokazać komunikat błędu i przycisk Ponów po nieudanym pobraniu transzy
++- URL: https://github.com/LordIllidan/ai-coding-demo/pull/17
++- Branch: ai-coding/aisdlc-135-pokaza-komunikat-b-du-i-przycisk-pon-w-po-nieuda-29643060029
++
++Diff introduced by this PR:
++~~~diff
++diff --git a/ai-coding-runs/aisdlc-135-coding-prompt.md b/ai-coding-runs/aisdlc-135-coding-prompt.md
++new file mode 100644
++index 0000000..49538c7
++--- /dev/null
+++++ b/ai-coding-runs/aisdlc-135-coding-prompt.md
++@@ -0,0 +1,31 @@
+++You are the CODING agent in a specialized worker pipeline (separate agents exist for
+++unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+++Running locally through a GitHub self-hosted runner (Windows).
+++
+++Source of truth: Jira issue AISDLC-135 (this task has NO corresponding GitHub issue —
+++Jira is the only tracker; do not create or reference a GitHub issue).
+++
+++Task title: Pokazać komunikat błędu i przycisk Ponów po nieudanym pobraniu transzy
+++
+++Task description:
+++~~~markdown
+++Parent story: AISDLC-120 — Komunikat błędu i ponowienia, gdy nie uda się pobrać danych transzy
+++
+++Implementacja frontendu dla scenariusza błędu pobrania danych transzy: po non-2xx czyścić aktualnie widoczne dane, pokazać komunikat błędu i przycisk "Ponów", który wykonuje ten sam GET dla tego samego claimId. Pliki do sprawdzenia: ekran/komponent transzy, hook lub store odpowiedzialny za pobieranie danych, klient API oraz testy UI. TODO: upewnić się, że widok nie prezentuje nieaktualnych danych jako poprawnych i że retry odświeża stan po ponownym żądaniu.
+++KONTRAKT: KONTRAKT (TechLeadAgent):
+++Endpoint: GET /api/claims/{claimId}/last-paid-tranche. Request przyjmuje wyłącznie path param claimId: string (UUID) oraz nagłówek Authorization: Bearer <token>; nie wolno używać customerId ani policyId w request ani w logice mapowania.
+++200 OK: { claimId: string(UUID), lastPaidTranche: { trancheId: string(UUID), trancheNumber: integer, status: 'PAID', paidAt: string(ISO-8601), grossAmount: number(2 dp), currency: string(ISO-4217) } | null, fetchedAt: string(ISO-8601) }. Przy braku danych lastPaidTranche = null.
+++Kody błędów: 401 INVALID_TOKEN (brak/wygaśnięty token), 403 CLAIM_ACCESS_DENIED (brak scope do claimId), 404 CLAIM_NOT_FOUND, 503 TRANCHE_SERVICE_UNAVAILABLE (circuit breaker/downstream unavailable), 504 TRANCHE_SERVICE_TIMEOUT (przekroczony timeout integracji). Wspólny error envelope: { code: string, message: string, retryable: boolean, correlationId: string }.
+++Walidacje i zachowanie UI: claimId obowiązkowy i musi być UUID; backend nie może zwracać danych z cache/starych odpowiedzi po timeout/circuit breaker; frontend po każdym non-2xx czyści aktualnie widoczne dane, pokazuje komunikat błędu i przycisk 'Ponów' wywołujący ponownie ten sam GET dla tego samego claimId.
+++DB/read model: claim_last_paid_tranche_view(claim_id PK, tranche_id, tranche_number, status, paid_at, gross_amount, currency, source_updated_at, refreshed_at). Kolumny identyfikacyjne i relacyjne opierają się na claim_id, nie na customer_id/policy_id.
+++~~~
+++
+++Task:
+++1. Implement the requested code change in this repository, scoped to the task above.
+++2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
+++   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
+++3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
+++4. Do not read or print secrets. Avoid destructive git commands.
+++5. Before finishing, leave the workspace ready to commit (diff applied on disk).
+++
+++Output: short summary of changed files and what each change does.
++\ No newline at end of file
++diff --git a/src/PolicyPlatform.Api/wwwroot/index.html b/src/PolicyPlatform.Api/wwwroot/index.html
++index 25a5623..d6e687a 100644
++--- a/src/PolicyPlatform.Api/wwwroot/index.html
+++++ b/src/PolicyPlatform.Api/wwwroot/index.html
++@@ -72,9 +72,20 @@ <h2>4. Zgłoszenie kradzieży</h2>
++   <div id="theftResult"></div>
++ </section>
++ 
+++<section>
+++  <h2>5. Ostatnia wypłacona transza</h2>
+++  <label>Claim ID (UUID) <input id="trancheClaimId" placeholder="np. 3fa85f64-5717-4562-b3fc-2c963f66afa6" /></label>
+++  <label>Token (Bearer) <input id="trancheToken" placeholder="opcjonalny token dema" /></label>
+++  <button onclick="loadLastPaidTranche()">Pobierz</button>
+++  <div id="trancheResult"></div>
+++  <div id="trancheError" class="field-error"></div>
+++  <button id="trancheRetryButton" onclick="retryLastPaidTranche()" style="display:none;">Ponów</button>
+++</section>
+++
++ <div id="log"></div>
++ 
++ <script src="theft-claim-validation.js"></script>
+++<script src="last-paid-tranche-client.js"></script>
++ <script>
++ const log = (msg) => { document.getElementById('log').textContent += msg + "\n"; };
++ 
++@@ -169,6 +180,50 @@ <h2>4. Zgłoszenie kradzieży</h2>
++   resultEl.textContent = 'Formularz poprawny — zgłoszenie kradzieży gotowe do wysłania.';
++ }
++ 
+++let lastRequestedTrancheClaimId = null;
+++
+++function renderTrancheResult(data) {
+++  const t = data.lastPaidTranche;
+++  document.getElementById('trancheResult').textContent = t
+++    ? `Transza #${t.trancheNumber}: ${t.grossAmount.toFixed(2)} ${t.currency}, wypłacono ${t.paidAt} (${t.status})`
+++    : 'Brak wypłaconych transz dla tego zgłoszenia.';
+++}
+++
+++function renderTrancheError(error) {
+++  document.getElementById('trancheResult').textContent = '';
+++  document.getElementById('trancheError').textContent = `${error.message} (${error.code})`;
+++  document.getElementById('trancheRetryButton').style.display = error.retryable ? 'inline-block' : 'none';
+++}
+++
+++async function loadLastPaidTrancheFor(claimId) {
+++  lastRequestedTrancheClaimId = claimId;
+++  // Czyścimy poprzednie dane/błąd zanim pokażemy wynik nowego żądania, żeby nigdy
+++  // nie prezentować nieaktualnych danych jako aktualnych podczas ładowania.
+++  document.getElementById('trancheResult').textContent = '';
+++  document.getElementById('trancheError').textContent = '';
+++  document.getElementById('trancheRetryButton').style.display = 'none';
+++
+++  const token = document.getElementById('trancheToken').value.trim() || undefined;
+++  const result = await LastPaidTrancheClient.fetchLastPaidTranche(claimId, { token });
+++
+++  if (!result.ok) {
+++    renderTrancheError(result.error);
+++    return;
+++  }
+++  renderTrancheResult(result.data);
+++}
+++
+++function loadLastPaidTranche() {
+++  const claimId = document.getElementById('trancheClaimId').value.trim();
+++  loadLastPaidTrancheFor(claimId);
+++}
+++
+++function retryLastPaidTranche() {
+++  // Ponów wykonuje dokładnie to samo żądanie (ten sam claimId), niezależnie od
+++  // tego, co użytkownik mógł od tej pory wpisać w polu Claim ID.
+++  loadLastPaidTrancheFor(lastRequestedTrancheClaimId);
+++}
+++
++ document.getElementById('effectiveDate').valueAsDate = new Date();
++ document.getElementById('expiryDate').valueAsDate = new Date(Date.now() + 365 * 24 * 3600 * 1000);
++ loadPolicies().catch(() => {});
++diff --git a/src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.js b/src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.js
++new file mode 100644
++index 0000000..399f852
++--- /dev/null
+++++ b/src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.js
++@@ -0,0 +1,56 @@
+++// Klient API dla GET /api/claims/{claimId}/last-paid-tranche (AISDLC-135 / AISDLC-120).
+++// UMD-style export so this pure module is testable from Node without a build step.
+++(function (root, factory) {
+++  if (typeof module === 'object' && module.exports) {
+++    module.exports = factory();
+++  } else {
+++    root.LastPaidTrancheClient = factory();
+++  }
+++})(typeof self !== 'undefined' ? self : this, function () {
+++  const GENERIC_ERROR_MESSAGE = 'Nie udało się pobrać danych ostatniej wypłaconej transzy. Spróbuj ponownie.';
+++
+++  function safeParseJson(text) {
+++    if (!text) return null;
+++    try {
+++      return JSON.parse(text);
+++    } catch {
+++      return null;
+++    }
+++  }
+++
+++  // Zwraca { ok: true, data } albo { ok: false, error: { code, message, retryable, correlationId } }.
+++  // Nigdy nie rzuca wyjątku — wywołujący ma zawsze dostać jednoznaczny wynik do wyrenderowania.
+++  async function fetchLastPaidTranche(claimId, options) {
+++    const { fetchImpl = fetch, token } = options || {};
+++    const headers = {};
+++    if (token) headers.Authorization = `Bearer ${token}`;
+++
+++    let res;
+++    try {
+++      res = await fetchImpl(`/api/claims/${encodeURIComponent(claimId)}/last-paid-tranche`, { headers });
+++    } catch {
+++      return {
+++        ok: false,
+++        error: { code: 'NETWORK_ERROR', message: GENERIC_ERROR_MESSAGE, retryable: true, correlationId: null },
+++      };
+++    }
+++
+++    const body = safeParseJson(await res.text());
+++
+++    if (!res.ok) {
+++      return {
+++        ok: false,
+++        error: {
+++          code: body?.code || `HTTP_${res.status}`,
+++          message: body?.message || GENERIC_ERROR_MESSAGE,
+++          retryable: body?.retryable ?? true,
+++          correlationId: body?.correlationId || null,
+++        },
+++      };
+++    }
+++
+++    return { ok: true, data: body };
+++  }
+++
+++  return { fetchLastPaidTranche, GENERIC_ERROR_MESSAGE };
+++});
++diff --git a/src/PolicyPlatform.Api/wwwroot/last-paid-tranche-client.test.js b/src
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