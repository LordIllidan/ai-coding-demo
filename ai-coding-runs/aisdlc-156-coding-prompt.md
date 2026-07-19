You are the CODING agent in a specialized worker pipeline (separate agents exist for
unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
Running locally through a GitHub self-hosted runner (Windows).

Source of truth: Jira issue AISDLC-156 (this task has NO corresponding GitHub issue —
Jira is the only tracker; do not create or reference a GitHub issue).

Task title: Testy kontraktowe i regresyjne dla licznika powiadomień

Task description:
~~~markdown
Parent story: AISDLC-148 — Licznik nieprzeczytanych powiadomień w aplikacji mobilnej

Dodanie testów kontraktowych, integracyjnych i regresyjnych dla licznika nieprzeczytanych powiadomień. Pliki/TODO: testy API dla counter/list/read, scenariusze 401/403/404/400, testy UI dla widocznego badge przy 0 oraz odświeżania po nowych i przeczytanych powiadomieniach.
KONTRAKT: KONTRAKT (TechLeadAgent):
Zakres: mobilny licznik nieprzeczytanych powiadomień dla aktualnego użytkownika zalogowanego tokenem Bearer. Frontend nigdy nie wysyła userId w requestach tego flow; backend zawsze bierze tożsamość z JWT (claim sub) i zwraca dane wyłącznie dla bieżącego konta.
1) GET /api/mobile/v1/notifications/counter — pobranie licznika. Response 200: { unreadCount: integer >= 0, calculatedAt: ISO-8601 UTC string }. unreadCount jest obowiązkowe, nigdy null, a wartość 0 ma być zwracana jawnie (UI nie może ukrywać licznika jako pustego stringa).
2) GET /api/mobile/v1/notifications?read=false&limit=50&cursor=<string> — lista nieprzeczytanych powiadomień. Response 200: { items: [{ id: uuid, title: string, body: string, type: string, createdAt: ISO-8601 UTC string, isRead: false, readAt: null }], nextCursor: string|null }. Parametr read akceptuje tylko false w tym widoku.
3) PATCH /api/mobile/v1/notifications/{notificationId}/read — oznaczenie jednego powiadomienia jako przeczytanego. Path param notificationId: uuid. Request body: brak. Response 200: { notificationId: uuid, isRead: true, readAt: ISO-8601 UTC string, unreadCount: integer >= 0 }. Endpoint jest idempotentny: ponowne wywołanie dla już przeczytanego powiadomienia zwraca 200 z aktualnym unreadCount.
Walidacje i błędy wspólne: 401 UNAUTHENTICATED — brak/nieprawidłowy Bearer JWT; 400 VALIDATION_ERROR — niepoprawny UUID lub query parametry spoza kontraktu; 403 FORBIDDEN — próba operacji na cudzym notificationId lub dostępu poza bieżącym użytkownikiem; 404 NOTIFICATION_NOT_FOUND — notificationId nie istnieje. Dla security nie zwracamy userId w odpowiedziach API.
Warstwa danych: tabela notifications(id uuid PK, user_id uuid NOT NULL, title varchar(200) NOT NULL, body text NOT NULL, type varchar(50) NOT NULL, is_read boolean NOT NULL DEFAULT false, read_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL). Indeks wymagany: (user_id, is_read, created_at DESC). Licznik jest liczony jako COUNT(*) WHERE user_id = :currentUserId AND is_read = false; nie tworzymy osobnej kolumny counter.
Zdarzenia wewnętrzne: notification.created oraz notification.read (payload: notificationId, userId, occurredAt) służą do invalidacji cache/push; frontend po pushu lub powrocie na ekran odświeża GET /counter.
UI kontrakt: badge licznika ma być widoczny przy każdej wartości liczbowej, w tym 0; po otrzymaniu nowego powiadomienia rośnie po odświeżeniu GET /counter, a po oznaczeniu jako przeczytane maleje po odpowiedzi PATCH /read.
~~~

Task:
1. Implement the requested code change in this repository, scoped to the task above.
2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
4. Do not read or print secrets. Avoid destructive git commands.
5. Before finishing, leave the workspace ready to commit (diff applied on disk).

Output: short summary of changed files and what each change does.