You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: LordIllidan/ai-coding-demo
- PR: #34 AI: [AISDLC-188] [DEV] Mobile: ekran historii ostatnich logowań
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/34
- Branch: ai-coding/aisdlc-188-dev-mobile-ekran-historii-ostatnich-logowa-29680531316

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-188-coding-prompt.md b/ai-coding-runs/aisdlc-188-coding-prompt.md
new file mode 100644
index 0000000..6dfd281
--- /dev/null
+++ b/ai-coding-runs/aisdlc-188-coding-prompt.md
@@ -0,0 +1,36 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-188 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: [DEV] Mobile: ekran historii ostatnich logowań
+
+Task description:
+~~~markdown
+Parent story: AISDLC-165 — Wyświetlanie historii ostatnich logowań w aplikacji mobilnej
+
+TODO: zaimplementować ekran historii ostatnich logowań w aplikacji mobilnej, pobranie danych z /api/mobile/me/login-history, obsługę loading/empty/error state oraz renderowanie daty, godziny i informacji o urządzeniu/sesji.
+KONTRAKT: KONTRAKT (TechLeadAgent):
+Zakres: ekran historii ostatnich logowań pobiera dane wyłącznie dla zalogowanego użytkownika; backend filtruje po tożsamości z JWT i zwraca wpisy posortowane od najnowszego do najstarszego.
+API: GET /api/mobile/me/login-history
+Auth: wymagany nagłówek Authorization: Bearer <JWT>. Backend bierze user_id wyłącznie z tokenu (sub/accountId). Frontend NIE wysyła userId/customerId/policyId ani żadnych identyfikatorów użytkownika w path/query/body. Brak albo nieważny token => 401 Unauthorized.
+Request: brak body. Brak query params w tej historyjce; nie ma filtrowania ani paginacji.
+Response 200: { "items": LoginHistoryEntry[] }
+LoginHistoryEntry: { "loginId": string (UUID), "occurredAt": string (ISO-8601 UTC), "deviceLabel": string | null, "deviceType": "PHONE" | "TABLET" | "WEB" | "UNKNOWN", "osName": string | null, "osVersion": string | null, "sessionId": string | null, "ipAddress": string | null }
+Zasady listy: items są sortowane malejąco po occurredAt; jeśli brak rekordów, backend zwraca 200 z "items": [] i frontend pokazuje stan pusty.
+Błędy/walidacje: 401 Unauthorized dla braku lub wygaśnięcia tokenu; 500 Internal Server Error dla problemów odczytu danych. 403 nie jest standardową ścieżką dla tego endpointu /me.
+Warstwa danych: tabela login_history_entries (id UUID PK, user_id UUID NOT NULL, occurred_at TIMESTAMPTZ NOT NULL, device_label TEXT NULL, device_type TEXT NOT NULL, os_name TEXT NULL, os_version TEXT NULL, session_id UUID NULL, ip_address INET NULL, created_at TIMESTAMPTZ NOT NULL DEFAULT now()); indeks obowiązkowy: (user_id, occurred_at DESC).
+UI: loading state jest tylko po stronie frontend — aktywny od wywołania GET do odpowiedzi API; po odpowiedzi znika i jest zastępowany listą, empty state albo komunikatem błędu.
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
diff --git a/src/PolicyPlatform.Api/wwwroot/mobile/login-history-list.js b/src/PolicyPlatform.Api/wwwroot/mobile/login-history-list.js
new file mode 100644
index 0000000..3c72dda
--- /dev/null
+++ b/src/PolicyPlatform.Api/wwwroot/mobile/login-history-list.js
@@ -0,0 +1,32 @@
+// Komponent listy historii logowań (AISDLC-188).
+// UMD-style export so this pure module is testable from Node without a build step.
+(function (root, factory) {
+  if (typeof module === 'object' && module.exports) {
+    module.exports = factory();
+  } else {
+    root.LoginHistoryList = factory();
+  }
+})(typeof self !== 'undefined' ? self : this, function () {
+  function renderLoginHistoryList(items) {
+    const list = document.createElement('ul');
+    list.className = 'login-history-list';
+    for (const item of items) {
+      const row = document.createElement('li');
+      row.className = 'login-history-row';
+      row.dataset.loginId = item.loginId;
+      row.innerHTML = `
+        <div class="login-history-row-primary">
+          <span class="login-history-device">${item.deviceLabel}</span>
+          <span class="login-history-time">${item.occurredAtLabel}</span>
+        </div>
+        <div class="login-history-row-secondary">
+          ${item.osLabel ? `<span>${item.osLabel}</span>` : ''}
+          ${item.ipAddress ? `<span>${item.ipAddress}</span>` : ''}
+        </div>`;
+      list.appendChild(row);
+    }
+    return list;
+  }
+
+  return { renderLoginHistoryList };
+});
diff --git a/src/PolicyPlatform.Api/wwwroot/mobile/login-history-screen.js b/src/PolicyPlatform.Api/wwwroot/mobile/login-history-screen.js
new file mode 100644
index 0000000..3473fc2
--- /dev/null
+++ b/src/PolicyPlatform.Api/wwwroot/mobile/login-history-screen.js
@@ -0,0 +1,57 @@
+// Kontroler ekranu historii logowań (AISDLC-188): spina serwis, viewmodel i komponent listy.
+(function () {
+  const { fetchLoginHistory } = window.LoginHistoryService;
+  const { STATUS, buildViewState } = window.LoginHistoryViewModel;
+  const { renderLoginHistoryList } = window.LoginHistoryList;
+
+  const AUTH_TOKEN_STORAGE_KEY = 'mobile_auth_token';
+
+  function render(root, viewState) {
+    root.innerHTML = '';
+
+    if (viewState.status === STATUS.LOADING) {
+      const loading = document.createElement('p');
+      loading.className = 'login-history-loading';
+      loading.textContent = 'Ładowanie historii logowań…';
+      root.appendChild(loading);
+      return;
+    }
+
+    if (viewState.status === STATUS.ERROR) {
+      const error = document.createElement('p');
+      error.className = 'login-history-error';
+      error.textContent = viewState.errorMessage;
+      root.appendChild(error);
+      return;
+    }
+
+    if (viewState.status === STATUS.EMPTY) {
+      const empty = document.createElement('p');
+      empty.className = 'login-history-empty';
+      empty.textContent = 'Brak zarejestrowanych logowań.';
+      root.appendChild(empty);
+      return;
+    }
+
+    root.appendChild(renderLoginHistoryList(viewState.items));
+  }
+
+  async function initLoginHistoryScreen(root) {
+    render(root, buildViewState({ loading: true }));
+
+    const token = window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
+    try {
+      const entries = await fetchLoginHistory(token);
+      render(root, buildViewState({ loading: false, entries }));
+    } catch (error) {
+      render(root, buildViewState({ loading: false, error }));
+    }
+  }
+
+  window.addEventListener('DOMContentLoaded', () => {
+    const root = document.getElementById('login-history-root');
+    if (root) initLoginHistoryScreen(root);
+  });
+
+  window.initLoginHistoryScreen = initLoginHistoryScreen;
+})();
diff --git a/src/PolicyPlatform.Api/wwwroot/mobile/login-history-service.js b/src/PolicyPlatform.Api/wwwroot/mobile/login-history-service.js
new file mode 100644
index 0000000..56427f0
--- /dev/null
+++ b/src/PolicyPlatform.Api/wwwroot/mobile/login-history-service.js
@@ -0,0 +1,45 @@
+// Serwis danych ekranu historii logowań (AISDLC-188).
+// UMD-style export so this pure module is testable from Node without a build step.
+(function (root, factory) {
+  if (typeof module === 'object' && module.exports) {
+    module.exports = factory();
+  } else {
+    root.LoginHistoryService = factory();
+  }
+})(typeof self !== 'undefined' ? self : this, function () {
+  const LOGIN_HISTORY_ENDPOINT = '/api/mobile/me/login-history';
+
+  class LoginHistoryError extends Error {
+    constructor(status, message) {
+      super(message);
+      this.name = 'LoginHistoryError';
+      this.status = status;
+    }
+  }
+
+  // Frontend nigdy nie wysyła userId/customerId/policyId ani innych identyfikatorów
+  // użytkownika — backend ustala tożsamość wyłącznie na podstawie tokenu JWT.
+  async function fetchLoginHistory(token, fetchImpl = fetch) {
+    let res;
+    try {
+      res = await fetchImpl(LOGIN_HISTORY_ENDPOINT, {
+        method: 'GET',
+        headers: token ? { Authorization: `Bearer ${token}` } : {},
+      });
+    } catch {
+      throw new LoginHistoryError(0, 'Brak połączenia z serwerem.');
+    }
+
+    if (res.status === 401) {
+      throw new LoginHistoryError(401, 'Sesja wygasła. Zaloguj się ponownie.');
+    }
+    if (!res.ok) {
+      throw new LoginHistoryError(res.status, 'Nie udało się pobrać historii logowań. Spróbuj ponownie później.');
+    }
+
+    const body = await res.json();
+    return Array.isArray(body?.items) ? body.items : [];
+  }
+
+  return { LOGIN_HISTORY_ENDPOINT, LoginHistoryError, fetchLoginHistory };
+});
diff --git a/src/PolicyPlatform.Api/wwwroot/mobile/login-history-viewmodel.js b/src/PolicyPlatform.Api/wwwroot/mobile/login-history-viewmodel.js
new file mode 100644
index 0000000..f7b2384
--- /dev/null
+++ b/src/PolicyPlatform.Api/wwwroot/mobile/login-history-viewmodel.js
@@ -0,0 +1,77 @@
+// Mapowanie stanu ekranu historii logowań (AISDLC-188).
+// UMD-style export so this pure module is testable from Node without a build step.
+(function (root, factory) {
+  if (typeof module === 'object' && module.exports) {
+    module.exports = factory();
+  } else {
+    root.LoginHistoryViewModel = factory();
+  }
+})(typeof self !== 'undefined' ? self : this, function () {
+  const STATUS = {
+    LOADING: 'loading',
+    LIST: 'list',
+    EMPTY: 'empty',
+    ERROR: 'error',
+  };
+
+  const DEVICE_TYPE_LABELS = {
+    PHONE: 'Telefon',
+    TABLET: 'Tablet',
+    WEB: 'Przeglądarka',
+    UNKNOWN: 'Nieznane urządzenie',
+  };
+
+  function deviceTypeLabel(deviceType) {
+    return DEVICE_TYPE_LABELS[deviceType] ?? DEVICE_TYPE_LABELS.UNKNOWN;
+  }
+
+  function formatOccurredAt(occurredAtIso) {
+    const date = new Date(occurredAtIso);
+    if (Number.isNaN(date.getTime())) return occurredAtIso;
+    return date.toLocaleString('pl-PL', {
+      year: 'numeric',
+      month: '2-digit',
+      day: '2-digit',
+      hour: '2-digit',
+      minute: '2-digit',
+    });
+  }
+
+  // Backend zwraca już malejąco po occurredAt, ale ekran nie polega na tym założeniu —
+  // "od najnowszego do najstarszego" sortujemy również na froncie.
+  function sortByOccurredAtDescending(entries) {
+    return [...entries].sort((a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime());
+  }
+
+  function toListItem(entry) {
+    const osLabel = entry.osName ? `${entry.osName}${entry.osVersion ? ' ' + entry.osVersion : ''}` : null;
+    return {
+      loginId: entry.loginId,
+      occurredAtLabel: formatOccurredAt(entry.occurredAt),
+      deviceLabel: entry.deviceLabel || deviceTypeLabel(entry.deviceType),
+      osLabel,
+      ipAddress: entry.ipAddress || null,
+    };
+  }
+
+  function mapLoginHistoryEntries(entries) {
+    return sortByOccurredAtDescending(entries).map(toListItem);
+  }
+
+  // Buduje jednoznaczny stan widoku: dokładnie jeden z loading/list/empty/error jest aktywny.
+  function buildViewState({ loading, error, entries }) {
+    if (loading) return { status: STATUS.LOADING, items: [], errorMessage: null };
+    if (error) return { status: STATUS.ERROR, items: [], errorMessage: error.message };
+    const items = mapLoginHistoryEntries(entries ?? []);
+    if (items.length === 0) return { status: STATUS.EMPTY, items: [], errorMessage: null };
+    return { status: STATUS.LIST, items, errorMessage: null };
+  }
+
+  return {
+    STATUS,
+    deviceTypeLabel,
+    formatOccurredAt,
+    mapLoginHistoryEntries,
+    buildViewState,
+  };
+});
diff --git a/src/PolicyPlatform.Api/wwwroot/mobile/login-history.html b/src/PolicyPlatform.Api/wwwroot/mobile/login-history.html
new file mode 100644
index 0000000..c8096cf
--- /dev/null
+++ b/src/PolicyPlatform.Api/wwwroot/mobile/login-history.html
@@ -0,0 +1,28 @@
+<!doctype html>
+<html lang="pl">
+<head>
+<meta charset="utf-8" />
+<meta name="viewport" content="width=device-width, initial-scale=1" />
+<title>Historia logowań</title>
+<style>
+  :root { color-scheme: light dark; }
+  body { font-family: system-ui, sans-serif; max-width: 480px; margin: 0 auto; padding: 1rem; }
+  h1 { font-size: 1.2rem; }
+  .login-history-loading, .login-history-empty { color: #888; text-align: center; margin-top: 2rem; }
+  .login-history-error { color: #a33; text-align: center; margin-top: 2rem; }
+  .login-history-list { list-style: none; margin: 0; padding: 0; }
+  .login-history-row { padding: 0.75rem 0; border-bottom: 1px solid #8884; }
+  .login-history-row-primary { display: flex; justify-content: space-between; font-weight: 600; }
+  .login-history-row-secondary { display: flex; gap: 0.75rem; font-size: 0.8rem; color: #888; margin-top: 0.2rem; }
+</style>
+</head>
+<body>
+<h1>Historia logowań</h1>
+<div id="login-history-root"></div>
+
+<script src="login-history-service.js"></script>
+<script src="login-history-viewmodel.js"></script>
+<script src="login-history-list.js"></script>
+<script src="login-history-screen.js"></script>
+</body>
+</html>

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