You are the CODING agent in a specialized worker pipeline (separate agents exist for
unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
Running locally through a GitHub self-hosted runner (Windows).

Source of truth: Jira issue AISDLC-139 (this task has NO corresponding GitHub issue —
Jira is the only tracker; do not create or reference a GitHub issue).

Task title: [Mobile] Read-only ekran ostatniej wypłaty

Task description:
~~~markdown
Parent story: AISDLC-118 — Wyświetlenie ostatniej wypłaconej transzy odszkodowania w aplikacji mobilnej

Widok tylko do odczytu pokazujący kwotę, datę i numer szkody z GET /api/mobile/me/claims/last-payout; brak edycji i brak akcji zapisu. Pliki TODO: ekran/kontroler mobilny, model widoku, mapowanie błędów, stan ładowania/pusty.
KONTRAKT: Endpoint: GET /api/mobile/me/claims/last-payout. Autoryzacja: wymagany Bearer JWT (klient zalogowany); request NIE zawiera customerId, policyId ani claimId w path/query/body — backend identyfikuje dane wyłącznie po subject/customerId z tokena.
Request: brak body, brak parametrów. Odpowiedź 200: { claimNumber: string, amount: { value: string(decimal, 2 miejsca), currency: string(3, domyślnie PLN) }, payoutDate: string(YYYY-MM-DD), readOnly: true }. Ekran jest wyłącznie do odczytu, UI nie może wysyłać żadnego PUT/PATCH/POST dla tej historii.
Mapowanie danych: claimNumber <- claim.claim_number; amount.value <- claim_payout.amount_gross; amount.currency <- claim_payout.currency_code; payoutDate <- claim_payout.paid_date (lub data businessowa wyliczona z claim_payout.paid_at). Backend pobiera TYLKO ostatni rekord spełniający: customer_id = JWT.sub/customerId, status = 'PAID', ORDER BY paid_at DESC LIMIT 1.
Błędy/walidacje: 401 AUTH_REQUIRED (brak/nieprawidłowy token), 403 FORBIDDEN_CROSS_CUSTOMER (token nieuprawniony do danych), 404 LAST_PAYOUT_NOT_FOUND (brak wypłaconej transzy dla zalogowanego klienta), 503 DATA_SOURCE_TIMEOUT (timeout/awaria źródła danych — kontrolowany komunikat, bez nieaktualnych danych), 500 INTERNAL_ERROR (awaria nieoczekiwana).
Baza: odczyt z tabeli claim_payout; wspólne kolumny: id, claim_id, customer_id, amount_gross, currency_code, paid_at, status. Żadna warstwa nie może opierać się na podmianie identyfikatora przez klienta — identyfikator kontekstu pochodzi tylko z JWT.
~~~

Task:
1. Implement the requested code change in this repository, scoped to the task above.
2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
4. Do not read or print secrets. Avoid destructive git commands.
5. Before finishing, leave the workspace ready to commit (diff applied on disk).

Output: short summary of changed files and what each change does.