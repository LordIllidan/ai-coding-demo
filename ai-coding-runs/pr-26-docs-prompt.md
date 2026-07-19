You are the DOCUMENTATION agent in a specialized worker pipeline (separate agents handle
coding, unit tests, e2e tests, database, and review — stay scoped to documentation only,
never touch executable logic).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under documentation:
- Repository: LordIllidan/ai-coding-demo
- PR: #26 AI: [AISDLC-155] Mobile: prezentacja badge licznika i odświeżanie
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/26
- Branch: ai-coding/aisdlc-155-mobile-prezentacja-badge-licznika-i-od-wie-anie-29678110418

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-155-coding-prompt.md b/ai-coding-runs/aisdlc-155-coding-prompt.md
new file mode 100644
index 0000000..a994ffc
--- /dev/null
+++ b/ai-coding-runs/aisdlc-155-coding-prompt.md
@@ -0,0 +1,34 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-155 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: Mobile: prezentacja badge licznika i odświeżanie
+
+Task description:
+~~~markdown
+Parent story: AISDLC-148 — Licznik nieprzeczytanych powiadomień w aplikacji mobilnej
+
+Implementacja warstwy mobilnej dla badge licznika i odświeżania danych. Pliki/TODO: komponent/licznik w UI, integracja z API clientem, odświeżanie na wejściu na ekran i po odebraniu pusha, aktualizacja stanu po oznaczeniu powiadomienia jako przeczytane, obsługa wartości 0.
+KONTRAKT: KONTRAKT (TechLeadAgent):
+Zakres: mobilny licznik nieprzeczytanych powiadomień dla aktualnego użytkownika zalogowanego tokenem Bearer. Frontend nigdy nie wysyła userId w requestach tego flow; backend zawsze bierze tożsamość z JWT (claim sub) i zwraca dane wyłącznie dla bieżącego konta.
+1) GET /api/mobile/v1/notifications/counter — pobranie licznika. Response 200: { unreadCount: integer >= 0, calculatedAt: ISO-8601 UTC string }. unreadCount jest obowiązkowe, nigdy null, a wartość 0 ma być zwracana jawnie (UI nie może ukrywać licznika jako pustego stringa).
+2) GET /api/mobile/v1/notifications?read=false&limit=50&cursor=<string> — lista nieprzeczytanych powiadomień. Response 200: { items: [{ id: uuid, title: string, body: string, type: string, createdAt: ISO-8601 UTC string, isRead: false, readAt: null }], nextCursor: string|null }. Parametr read akceptuje tylko false w tym widoku.
+3) PATCH /api/mobile/v1/notifications/{notificationId}/read — oznaczenie jednego powiadomienia jako przeczytanego. Path param notificationId: uuid. Request body: brak. Response 200: { notificationId: uuid, isRead: true, readAt: ISO-8601 UTC string, unreadCount: integer >= 0 }. Endpoint jest idempotentny: ponowne wywołanie dla już przeczytanego powiadomienia zwraca 200 z aktualnym unreadCount.
+Walidacje i błędy wspólne: 401 UNAUTHENTICATED — brak/nieprawidłowy Bearer JWT; 400 VALIDATION_ERROR — niepoprawny UUID lub query parametry spoza kontraktu; 403 FORBIDDEN — próba operacji na cudzym notificationId lub dostępu poza bieżącym użytkownikiem; 404 NOTIFICATION_NOT_FOUND — notificationId nie istnieje. Dla security nie zwracamy userId w odpowiedziach API.
+Warstwa danych: tabela notifications(id uuid PK, user_id uuid NOT NULL, title varchar(200) NOT NULL, body text NOT NULL, type varchar(50) NOT NULL, is_read boolean NOT NULL DEFAULT false, read_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL). Indeks wymagany: (user_id, is_read, created_at DESC). Licznik jest liczony jako COUNT(*) WHERE user_id = :currentUserId AND is_read = false; nie tworzymy osobnej kolumny counter.
+Zdarzenia wewnętrzne: notification.created oraz notification.read (payload: notificationId, userId, occurredAt) służą do invalidacji cache/push; frontend po pushu lub powrocie na ekran odświeża GET /counter.
+UI kontrakt: badge licznika ma być widoczny przy każdej wartości liczbowej, w tym 0; po otrzymaniu nowego powiadomienia rośnie po odświeżeniu GET /counter, a po oznaczeniu jako przeczytane maleje po odpowiedzi PATCH /read.
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
index 25a5623..4790dd8 100644
--- a/src/PolicyPlatform.Api/wwwroot/index.html
+++ b/src/PolicyPlatform.Api/wwwroot/index.html
@@ -20,6 +20,10 @@
   #log { white-space: pre-wrap; font-family: monospace; font-size: 0.8rem; color: #888; }
   .field-error { color: #a33; font-size: 0.8rem; margin-top: 0.2rem; min-height: 1em; }
   input.invalid { border-color: #a33; outline-color: #a33; }
+  .badge { display: inline-flex; align-items: center; justify-content: center; min-width: 1.4rem; height: 1.4rem;
+    padding: 0 0.35rem; border-radius: 999px; background: #a33; color: #fff; font-size: 0.85rem; font-weight: 600; }
+  .notif-item { display: flex; justify-content: space-between; align-items: center; gap: 0.5rem;
+    padding: 0.4rem 0; border-bottom: 1px solid #8884; }
 </style>
 </head>
 <body>
@@ -72,9 +76,19 @@ <h2>4. Zgłoszenie kradzieży</h2>
   <div id="theftResult"></div>
 </section>
 
+<section>
+  <h2>5. Powiadomienia (ekran mobilny)</h2>
+  <p>Nieprzeczytane: <span id="notifBadge" class="badge">0</span></p>
+  <button onclick="onScreenEnter()">Wejście na ekran (odśwież)</button>
+  <button onclick="onPushReceived()">Symuluj push</button>
+  <div id="notifCounterResult"></div>
+  <div id="notifList"></div>
+</section>
+
 <div id="log"></div>
 
 <script src="theft-claim-validation.js"></script>
+<script src="notification-counter.js"></script>
 <script>
 const log = (msg) => { document.getElementById('log').textContent += msg + "\n"; };
 
@@ -169,9 +183,82 @@ <h2>4. Zgłoszenie kradzieży</h2>
   resultEl.textContent = 'Formularz poprawny — zgłoszenie kradzieży gotowe do wysłania.';
 }
 
+function authHeaders() {
+  const token = localStorage.getItem('authToken');
+  return token ? { Authorization: `Bearer ${token}` } : {};
+}
+
+function renderBadge(unreadCount) {
+  document.getElementById('notifBadge').textContent = NotificationCounter.formatBadgeValue(unreadCount);
+}
+
+async function handleNotificationsError(res) {
+  const text = await res.text();
+  const body = text ? JSON.parse(text) : null;
+  if (res.status === 401) {
+    window.location.href = '/login.html';
+    return null;
+  }
+  const err = NotificationCounter.mapErrorResponse(res.status, body);
+  document.getElementById('notifCounterResult').textContent = err.text;
+  log(`Notifications ${res.status}: ${JSON.stringify(body)}`);
+  return err;
+}
+
+async function refreshNotificationCounter() {
+  const res = await fetch('/api/mobile/v1/notifications/counter', { headers: authHeaders() });
+  if (res.status !== 200) { await handleNotificationsError(res); return; }
+  const body = await res.json();
+  const counter = NotificationCounter.mapCounterResponse(body);
+  renderBadge(counter.unreadCount);
+  document.getElementById('notifCounterResult').textContent = `Ostatnie odświeżenie: ${counter.calculatedAt}`;
+}
+
+async function loadUnreadNotifications() {
+  const res = await fetch('/api/mobile/v1/notifications?read=false&limit=50', { headers: authHeaders() });
+  if (res.status !== 200) { await handleNotificationsError(res); return; }
+  const body = await res.json();
+  const list = NotificationCounter.mapListResponse(body);
+  const container = document.getElementById('notifList');
+  container.innerHTML = '';
+  for (const item of list.items) {
+    const row = document.createElement('div');
+    row.className = 'notif-item';
+    row.innerHTML = `
+      <span><strong>${item.title}</strong> — ${item.body}</span>
+      <button onclick="markNotificationAsRead('${item.id}')">Oznacz jako przeczytane</button>`;
+    container.appendChild(row);
+  }
+}
+
+async function markNotificationAsRead(notificationId) {
+  const res = await fetch(`/api/mobile/v1/notifications/${encodeURIComponent(notificationId)}/read`, {
+    method: 'PATCH',
+    headers: authHeaders(),
+  });
+  if (res.status !== 200) { await handleNotificationsError(res); return; }
+  const body = await res.json();
+  const readResult = NotificationCounter.mapReadResponse(body);
+  renderBadge(NotificationCounter.applyReadResult(readResult).unreadCount);
+  loadUnreadNotifications().catch(() => {});
+}
+
+// "Wejście na ekran" — refresh on screen entry per contract.
+async function onScreenEnter() {
+  await refreshNotificationCounter();
+  await loadUnreadNotifications();
+}
+
+// Push delivery has no payload the client trusts; per contract it just
+// triggers a GET /counter refresh (never bumps the badge locally).
+async function onPushReceived() {
+  await refreshNotificationCounter();
+}
+
 document.getElementById('effectiveDate').valueAsDate = new Date();
 document.getElementById('expiryDate').valueAsDate = new Date(Date.now() + 365 * 24 * 3600 * 1000);
 loadPolicies().catch(() => {});
+onScreenEnter().catch(() => {});
 </script>
 </body>
 </html>
diff --git a/src/PolicyPlatform.Api/wwwroot/notification-counter.js b/src/PolicyPlatform.Api/wwwroot/notification-counter.js
new file mode 100644
index 0000000..4f97894
--- /dev/null
+++ b/src/PolicyPlatform.Api/wwwroot/notification-counter.js
@@ -0,0 +1,81 @@
+// Ekran mobilny "Badge licznika powiadomień" (AISDLC-155, parent story AISDLC-148).
+// UMD-style export so this pure module is testable from Node without a build step.
+(function (root, factory) {
+  if (typeof module === 'object' && module.exports) {
+    module.exports = factory();
+  } else {
+    root.NotificationCounter = factory();
+  }
+})(typeof self !== 'undefined' ? self : this, function () {
+  const ERROR_MESSAGES = {
+    UNAUTHENTICATED: 'Sesja wygasła. Zaloguj się ponownie.',
+    VALIDATION_ERROR: 'Nieprawidłowe żądanie.',
+    FORBIDDEN: 'Brak dostępu do tego powiadomienia.',
+    NOTIFICATION_NOT_FOUND: 'Powiadomienie nie zostało znalezione.',
+    UNKNOWN: 'Wystąpił nieoczekiwany błąd.',
+  };
+
+  // unreadCount is mandatory and never null per contract — 0 must render as the
+  // visible digit "0", never as an empty/hidden badge.
+  function formatBadgeValue(unreadCount) {
+    if (typeof unreadCount !== 'number' || !Number.isFinite(unreadCount) || unreadCount < 0) {
+      throw new TypeError('unreadCount must be a non-negative finite number');
+    }
+    return String(unreadCount);
+  }
+
+  function mapCounterResponse(body) {
+    return { unreadCount: body.unreadCount, calculatedAt: body.calculatedAt };
+  }
+
+  function mapListResponse(body) {
+    return {
+      items: (body.items || []).map((item) => ({
+        id: item.id,
+        title: item.title,
+        body: item.body,
+        type: item.type,
+        createdAt: item.createdAt,
+        isRead: item.isRead,
+        readAt: item.readAt,
+      })),
+      nextCursor: body.nextCursor,
+    };
+  }
+
+  function mapReadResponse(body) {
+    return {
+      notificationId: body.notificationId,
+      isRead: body.isRead,
+      readAt: body.readAt,
+      unreadCount: body.unreadCount,
+    };
+  }
+
+  // Source of truth for the badge after marking a notification read is the
+  // server-returned unreadCount from the PATCH response, not a local decrement —
+  // the endpoint is idempotent and re-reading an already-read notification must
+  // not double-decrement the badge.
+  function applyReadResult(readResult) {
+    return { unreadCount: readResult.unreadCount, calculatedAt: null };
+  }
+
+  function mapErrorResponse(status, body) {
+    if (status === 401) return { kind: 'redirect-login', code: 'UNAUTHENTICATED', text: ERROR_MESSAGES.UNAUTHENTICATED };
+    const code = (body && body.code) || null;
+    if (status === 400) return { kind: 'message', code: 'VALIDATION_ERROR', text: ERROR_MESSAGES.VALIDATION_ERROR };
+    if (status === 403) return { kind: 'message', code: 'FORBIDDEN', text: ERROR_MESSAGES.FORBIDDEN };
+    if (status === 404) return { kind: 'message', code: 'NOTIFICATION_NOT_FOUND', text: ERROR_MESSAGES.NOTIFICATION_NOT_FOUND };
+    return { kind: 'message', code: code || 'UNKNOWN', text: ERROR_MESSAGES.UNKNOWN };
+  }
+
+  return {
+    ERROR_MESSAGES,
+    formatBadgeValue,
+    mapCounterResponse,
+    mapListResponse,
+    mapReadResponse,
+    applyReadResult,
+    mapErrorResponse,
+  };
+});

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