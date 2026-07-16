You are the CODING agent in a specialized worker pipeline (separate agents exist for
unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
Running locally through a GitHub self-hosted runner (Windows).

Source of truth: Jira issue AISDLC-69 (this task has NO corresponding GitHub issue —
Jira is the only tracker; do not create or reference a GitHub issue).

Task title: Persistencja zgłoszenia assistance i asynchroniczna wysyłka do partnera

Task description:
~~~markdown
Parent story: AISDLC-57 — Zgłoszenie awarii w aplikacji mobilnej z lokalizacją GPS

Persistencja rejestracji oraz jobów integracyjnych dla zgłoszeń assistance: zapis reportu, eventów i zadań wysyłki do partnera po commicie. Pliki: encje, repozytoria, serwis transakcyjny, worker/retry scheduler, testy integracyjne. TODO: nie blokować rejestracji na błędzie partnera.
KONTRAKT (TechLeadAgent):
1) Endpoint: POST /api/v1/assistance/reports
2) Autoryzacja: Authorization: Bearer <JWT>. Użytkownik i pojazd są identyfikowane z tokenu; nie wysyłamy customerId, policyId ani userId w body.
3) Nagłówek idempotencji: Idempotency-Key: UUID v4, wymagany dla każdego wysłania. Ten sam klucz dla tego samego użytkownika w oknie 24h musi zwrócić istniejący reportId i nie tworzyć duplikatu.
4) Request JSON:
   - incidentType: string enum = DISABLED_VEHICLE | ACCIDENT_NO_INJURY | NO_FUEL | FLAT_BATTERY
   - gpsLatitude: number (decimal, zakres -90..90), wymagane
   - gpsLongitude: number (decimal, zakres -180..180), wymagane
   - gpsAccuracyMeters: number, opcjonalne, > 0
   - occurredAt: string ISO-8601, opcjonalne; jeśli podane, nie może być w przyszłości o więcej niż 5 minut
5) Response 201 JSON:
   { reportId: UUID, status: 'REGISTERED', incidentType: enum, partnerDispatchStatus: 'SENT' | 'FAILED_RETRY_SCHEDULED', warnings?: [{ code: 'PARTNER_INTEGRATION_FAILED', message: string }], createdAt: ISO-8601 }
6) Błędy/walidacje wspólne:
   - 400 ASSISTANCE_001 INVALID_INCIDENT_TYPE — typ spoza listy obsługiwanej
   - 400 ASSISTANCE_002 GPS_LOCATION_REQUIRED — brak GPS lub brak wymaganych współrzędnych
   - 400 ASSISTANCE_003 INVALID_COORDINATES — współrzędne poza zakresem lub niepoprawny format
   - 400 ASSISTANCE_004 MISSING_IDEMPOTENCY_KEY — brak Idempotency-Key
   - 409 ASSISTANCE_005 DUPLICATE_SUBMISSION — ponowne wysłanie tego samego Idempotency-Key
   - 401/403 AUTH_001 UNAUTHORIZED/ACCESS_DENIED — brak lub nieważny JWT
7) Baza danych:
   - assistance_report(id UUID PK, user_id UUID NOT NULL, idempotency_key UUID NOT NULL, incident_type VARCHAR(32) NOT NULL, gps_latitude DECIMAL(9,6) NOT NULL, gps_longitude DECIMAL(9,6) NOT NULL, gps_accuracy_m DECIMAL(8,2) NULL, status VARCHAR(32) NOT NULL, partner_dispatch_status VARCHAR(32) NOT NULL, partner_case_id VARCHAR(64) NULL, created_at TIMESTAMP NOT NULL, updated_at TIMESTAMP NOT NULL)
   - UNIQUE(user_id, idempotency_key)
   - assistance_report_event(id UUID PK, report_id UUID FK, event_type VARCHAR(48) NOT NULL, payload JSONB NOT NULL, created_at TIMESTAMP NOT NULL)
8) Eventy integracyjne: ASSISTANCE_REPORT_CREATED, PARTNER_DISPATCH_REQUESTED, PARTNER_DISPATCH_SUCCEEDED, PARTNER_DISPATCH_FAILED.
9) Reguły flow:
   - brak zgody na GPS / brak lokalizacji: frontend nie wysyła requestu i pokazuje lokalny błąd, backend dodatkowo waliduje i zwraca 400 ASSISTANCE_002
   - nieobsługiwany typ zdarzenia: request nie jest zapisywany
   - błąd partnera assistance nie blokuje rejestracji; rekord zostaje REGISTERED, partner_dispatch_status = FAILED_RETRY_SCHEDULED, a system zapisuje event PARTNER_DISPATCH_FAILED i planuje retry
~~~

Task:
1. Implement the requested code change in this repository, scoped to the task above.
2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
4. Do not read or print secrets. Avoid destructive git commands.
5. Before finishing, leave the workspace ready to commit (diff applied on disk).

Output: short summary of changed files and what each change does.