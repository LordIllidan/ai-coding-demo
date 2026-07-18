You are the CODING agent in a specialized worker pipeline (separate agents exist for
unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
Running locally through a GitHub self-hosted runner (Windows).

Source of truth: Jira issue AISDLC-134 (this task has NO corresponding GitHub issue —
Jira is the only tracker; do not create or reference a GitHub issue).

Task title: Obsłużyć błąd pobrania ostatniej wypłaconej transzy bez serwowania starych danych

Task description:
~~~markdown
Parent story: AISDLC-120 — Komunikat błędu i ponowienia, gdy nie uda się pobrać danych transzy

Implementacja backendu dla GET /api/claims/{claimId}/last-paid-tranche: walidacja claimId jako UUID, mapowanie błędów 401/403/404/503/504 do wspólnego envelope oraz brak zwracania cache/starych danych po timeout/circuit breaker. Pliki do sprawdzenia: kontroler/handler endpointu claims, serwis pobierania transzy, mapper błędów, read model/repocitory claim_last_paid_tranche_view. TODO: potwierdzić, że logika używa wyłącznie claimId i że odpowiedź 200 zwraca lastPaidTranche=null, gdy brak danych.
KONTRAKT: KONTRAKT (TechLeadAgent):
Endpoint: GET /api/claims/{claimId}/last-paid-tranche. Request przyjmuje wyłącznie path param claimId: string (UUID) oraz nagłówek Authorization: Bearer <token>; nie wolno używać customerId ani policyId w request ani w logice mapowania.
200 OK: { claimId: string(UUID), lastPaidTranche: { trancheId: string(UUID), trancheNumber: integer, status: 'PAID', paidAt: string(ISO-8601), grossAmount: number(2 dp), currency: string(ISO-4217) } | null, fetchedAt: string(ISO-8601) }. Przy braku danych lastPaidTranche = null.
Kody błędów: 401 INVALID_TOKEN (brak/wygaśnięty token), 403 CLAIM_ACCESS_DENIED (brak scope do claimId), 404 CLAIM_NOT_FOUND, 503 TRANCHE_SERVICE_UNAVAILABLE (circuit breaker/downstream unavailable), 504 TRANCHE_SERVICE_TIMEOUT (przekroczony timeout integracji). Wspólny error envelope: { code: string, message: string, retryable: boolean, correlationId: string }.
Walidacje i zachowanie UI: claimId obowiązkowy i musi być UUID; backend nie może zwracać danych z cache/starych odpowiedzi po timeout/circuit breaker; frontend po każdym non-2xx czyści aktualnie widoczne dane, pokazuje komunikat błędu i przycisk 'Ponów' wywołujący ponownie ten sam GET dla tego samego claimId.
DB/read model: claim_last_paid_tranche_view(claim_id PK, tranche_id, tranche_number, status, paid_at, gross_amount, currency, source_updated_at, refreshed_at). Kolumny identyfikacyjne i relacyjne opierają się na claim_id, nie na customer_id/policy_id.
~~~

Task:
1. Implement the requested code change in this repository, scoped to the task above.
2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
4. Do not read or print secrets. Avoid destructive git commands.
5. Before finishing, leave the workspace ready to commit (diff applied on disk).

Output: short summary of changed files and what each change does.