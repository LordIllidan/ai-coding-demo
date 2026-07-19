You are the CODING agent in a specialized worker pipeline (separate agents exist for
unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
Running locally through a GitHub self-hosted runner (Windows).

Source of truth: Jira issue AISDLC-191 (this task has NO corresponding GitHub issue —
Jira is the only tracker; do not create or reference a GitHub issue).

Task title: [DEV] Backend: endpoint /api/mobile/me/login-history i sortowanie wpisów

Task description:
~~~markdown
Parent story: AISDLC-165 — Wyświetlanie historii ostatnich logowań w aplikacji mobilnej

Implementacja backendowego endpointu GET /api/mobile/me/login-history: kontroler/serwis/repozytorium, pobieranie danych wyłącznie z JWT, mapowanie DTO LoginHistoryEntry, sortowanie malejące po occurredAt i obsługa 200/401/500. Pliki: backend API, auth middleware, repository, testy integracyjne. TODO: nie przyjmować żadnych identyfikatorów użytkownika w path/query/body.
KONTRAKT: KONTRAKT (TechLeadAgent):
Zakres: ekran historii ostatnich logowań pobiera dane wyłącznie dla zalogowanego użytkownika; backend filtruje po tożsamości z JWT i zwraca wpisy posortowane od najnowszego do najstarszego.
API: GET /api/mobile/me/login-history
Auth: wymagany nagłówek Authorization: Bearer <JWT>. Backend bierze user_id wyłącznie z tokenu (sub/accountId). Frontend NIE wysyła userId/customerId/policyId ani żadnych identyfikatorów użytkownika w path/query/body. Brak albo nieważny token => 401 Unauthorized.
Request: brak body. Brak query params w tej historyjce; nie ma filtrowania ani paginacji.
Response 200: { "items": LoginHistoryEntry[] }
LoginHistoryEntry: { "loginId": string (UUID), "occurredAt": string (ISO-8601 UTC), "deviceLabel": string | null, "deviceType": "PHONE" | "TABLET" | "WEB" | "UNKNOWN", "osName": string | null, "osVersion": string | null, "sessionId": string | null, "ipAddress": string | null }
Zasady listy: items są sortowane malejąco po occurredAt; jeśli brak rekordów, backend zwraca 200 z "items": [] i frontend pokazuje stan pusty.
Błędy/walidacje: 401 Unauthorized dla braku lub wygaśnięcia tokenu; 500 Internal Server Error dla problemów odczytu danych. 403 nie jest standardową ścieżką dla tego endpointu /me.
Warstwa danych: tabela login_history_entries (id UUID PK, user_id UUID NOT NULL, occurred_at TIMESTAMPTZ NOT NULL, device_label TEXT NULL, device_type TEXT NOT NULL, os_name TEXT NULL, os_version TEXT NULL, session_id UUID NULL, ip_address INET NULL, created_at TIMESTAMPTZ NOT NULL DEFAULT now()); indeks obowiązkowy: (user_id, occurred_at DESC).
UI: loading state jest tylko po stronie frontend — aktywny od wywołania GET do odpowiedzi API; po odpowiedzi znika i jest zastępowany listą, empty state albo komunikatem błędu.
~~~

Task:
1. Implement the requested code change in this repository, scoped to the task above.
2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
4. Do not read or print secrets. Avoid destructive git commands.
5. Before finishing, leave the workspace ready to commit (diff applied on disk).

Output: short summary of changed files and what each change does.