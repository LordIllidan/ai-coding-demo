You are the CODING agent in a specialized worker pipeline (separate agents exist for
unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
Running locally through a GitHub self-hosted runner (Windows).

Source of truth: Jira issue AISDLC-137 (this task has NO corresponding GitHub issue —
Jira is the only tracker; do not create or reference a GitHub issue).

Task title: Mobile: ekran transzy i stan pusty bez edycji

Task description:
~~~markdown
Parent story: AISDLC-119 — Komunikat, gdy klient nie ma wypłaconej transzy odszkodowania

Podpiąć ekran mobilny do GET /api/v1/claims/{claimId}/payouts/last-paid-installment i wyrenderować stan PAID lub jeden pusty stan bez akcji edycji. Pliki: ekran/komponent szczegółów transzy, mapowanie odpowiedzi API, stan błędów/empty-state, testy UI. TODO: ukryć kwotę/datę/numer szkody przy NO_PAYOUT i INCOMPLETE_DATA, nie pokazywać placeholderów dla brakujących pól, zachować przekierowanie logowania i komunikaty dla 403/404/500.
KONTRAKT: KONTRAKT (TechLeadAgent):
Endpoint: GET /api/v1/claims/{claimId}/payouts/last-paid-installment. Path param claimId = UUID szkody; nie używamy customerId ani policyId do pobrania tego ekranu. Autoryzacja: Bearer token w nagłówku Authorization.
Sukces 200 (tylko gdy da się pokazać pełne dane): { claimId: string(UUID), claimNumber: string, screenState: 'PAID', lastPaidInstallment: { installmentId: string(UUID), installmentNo: integer >= 1, paidAt: string(YYYY-MM-DD), amount: number(2 miejsca po przecinku), currency: string(ISO-4217) }, canEdit: false }. Frontend renderuje wartości tylko dla screenState='PAID' i tylko jeśli wszystkie pola lastPaidInstallment są niepuste.
Brak wypłaty 200: { claimId: string(UUID), screenState: 'NO_PAYOUT', lastPaidInstallment: null, canEdit: false }. Frontend pokazuje jeden pusty stan z tekstem: 'Nie mamy jeszcze wypłaconej transzy odszkodowania.' i nie pokazuje kwoty, daty, numeru szkody ani akcji edycji. Jeśli backend wykryje rekord częściowy/brakujące pola, ma zwrócić screenState='INCOMPLETE_DATA' z lastPaidInstallment=null i tym samym zachowaniem UI (bez placeholderów i bez częściowych wartości).
Błędy wspólne: 401 UNAUTHORIZED -> przekierowanie do logowania; 403 CLAIM_ACCESS_DENIED -> komunikat 'Brak uprawnień do danych szkody.'; 404 CLAIM_NOT_FOUND -> komunikat o braku danych; 500 CLAIM_PAYOUT_LOOKUP_FAILED -> komunikat techniczny/obserwowalność, bez danych na ekranie. Warstwa DB: filtrowanie po claims.id = claimId, wybór ostatniej wypłaty po paid_at DESC, installment_no DESC; nie wolno filtrować po policy_id.
~~~

Task:
1. Implement the requested code change in this repository, scoped to the task above.
2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
4. Do not read or print secrets. Avoid destructive git commands.
5. Before finishing, leave the workspace ready to commit (diff applied on disk).

Output: short summary of changed files and what each change does.