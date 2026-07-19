You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: LordIllidan/ai-coding-demo
- PR: #25 AI: [AISDLC-156] Testy kontraktowe i regresyjne dla licznika powiadomień
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/25
- Branch: ai-coding/aisdlc-156-testy-kontraktowe-i-regresyjne-dla-licznika-powi-29678108877

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-156-coding-prompt.md b/ai-coding-runs/aisdlc-156-coding-prompt.md
new file mode 100644
index 0000000..61962d7
--- /dev/null
+++ b/ai-coding-runs/aisdlc-156-coding-prompt.md
@@ -0,0 +1,34 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-156 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: Testy kontraktowe i regresyjne dla licznika powiadomień
+
+Task description:
+~~~markdown
+Parent story: AISDLC-148 — Licznik nieprzeczytanych powiadomień w aplikacji mobilnej
+
+Dodanie testów kontraktowych, integracyjnych i regresyjnych dla licznika nieprzeczytanych powiadomień. Pliki/TODO: testy API dla counter/list/read, scenariusze 401/403/404/400, testy UI dla widocznego badge przy 0 oraz odświeżania po nowych i przeczytanych powiadomieniach.
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
diff --git a/ai-coding-runs/pr-25-e2e-prompt.md b/ai-coding-runs/pr-25-e2e-prompt.md
new file mode 100644
index 0000000..3f1b75f
--- /dev/null
+++ b/ai-coding-runs/pr-25-e2e-prompt.md
@@ -0,0 +1,489 @@
+You are the E2E TEST agent in a specialized worker pipeline (separate agents exist for
+coding, unit tests, and review — stay scoped to end-to-end / user-flow test coverage only).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Pull request under test:
+- Repository: LordIllidan/ai-coding-demo
+- PR: #25 AI: [AISDLC-156] Testy kontraktowe i regresyjne dla licznika powiadomień
+- URL: https://github.com/LordIllidan/ai-coding-demo/pull/25
+- Branch: ai-coding/aisdlc-156-testy-kontraktowe-i-regresyjne-dla-licznika-powi-29678108877
+
+PR description (contains the originating Jira key and task context — the acceptance
+criteria live in Jira; use what's summarized here as the source of truth for what a
+real flow must satisfy):
+~~~markdown
+Source: Jira **AISDLC-156** — Testy kontraktowe i regresyjne dla licznika powiadomień
+
+Auto-generated by CodingWorker (model: sonnet). No GitHub issue exists for this task; Jira is the source of truth.
+
+```text
+Diff on disk, ready to commit. Summary below.
+
+**AISDLC-156 — backend for mobile unread-notifications counter (parent story AISDLC-148 wasn't built yet, so implemented the feature the contract needs before any test worker can exercise it):**
+
+- `src/PolicyPlatform.Domain/Notifications/Notification.cs` — entity: `UserId`, `Title`, `Body`, `Type`, `IsRead`/`ReadAt`, idempotent `MarkAsRead`.
+- `src/PolicyPlatform.Application/Abstractions/INotificationRepository.cs` — repo contract: get-by-id, count-unread, cursor-paginated unread list, save.
+- `src/PolicyPlatform.Application/Notifications/` — `NotificationDtos.cs` (counter/list/read response shapes), `NotificationService.cs` (counter/list/mark-read use cases, all keyed off current-user id, never a caller-supplied id), `NotificationAccessException.cs` (NotFound/Forbidden signal for controller).
+- `src/PolicyPlatform.Infrastructure/Persistence/InMemoryNotificationRepository.cs` — in-memory store (same pattern as `InMemoryClaimRepository`), base64 cursor over `(createdAt, id)`.
+- `src/PolicyPlatform.Infrastructure/DependencyInjection.cs` — wired new repo + service.
+- `src/PolicyPlatform.Api/Controllers/NotificationsController.cs` — `GET /api/mobile/v1/notifications/counter`, `GET /api/mobile/v1/notifications?read=false&limit&cursor`, `PATCH /api/mobile/v1/notifications/{id}/read`. Enforces contract: 400 `VALIDATION_ERROR` (bad UUID, unsupported query param, bad `read`/`limit`), 403 `FORBIDDEN` (cross-user), 404 `NOTIFICATION_NOT_FOUND`, 401 `UNAUTHENTICATED`.
+- `Program.cs` + `appsettings*.json` + `.csproj` — added JWT bearer auth (`Microsoft.AspNetCore.Authentication.JwtBearer`), identity resolved from `sub` claim only, `MapInboundClaims=false` to keep raw claim name, JSON 401 body via `OnChallenge`. Applied only to `NotificationsController` (`[Authorize]`) — other controllers untouched/still anonymous.
+
+Build: solution compiles clean, 0 errors/warnings.
+
+Left for other workers: comprehensive contract/regression/UI tests (unit, e2e) per ticket TODO — out of scope for coding agent.
+```
+~~~
+
+Diff introduced by this PR:
+~~~diff
+diff --git a/ai-coding-runs/aisdlc-156-coding-prompt.md b/ai-coding-runs/aisdlc-156-coding-prompt.md
+new file mode 100644
+index 0000000..61962d7
+--- /dev/null
++++ b/ai-coding-runs/aisdlc-156-coding-prompt.md
+@@ -0,0 +1,34 @@
++You are the CODING agent in a specialized worker pipeline (separate agents exist for
++unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
++Running locally through a GitHub self-hosted runner (Windows).
++
++Source of truth: Jira issue AISDLC-156 (this task has NO corresponding GitHub issue —
++Jira is the only tracker; do not create or reference a GitHub issue).
++
++Task title: Testy kontraktowe i regresyjne dla licznika powiadomień
++
++Task description:
++~~~markdown
++Parent story: AISDLC-148 — Licznik nieprzeczytanych powiadomień w aplikacji mobilnej
++
++Dodanie testów kontraktowych, integracyjnych i regresyjnych dla licznika nieprzeczytanych powiadomień. Pliki/TODO: testy API dla counter/list/read, scenariusze 401/403/404/400, testy UI dla widocznego badge przy 0 oraz odświeżania po nowych i przeczytanych powiadomieniach.
++KONTRAKT: KONTRAKT (TechLeadAgent):
++Zakres: mobilny licznik nieprzeczytanych powiadomień dla aktualnego użytkownika zalogowanego tokenem Bearer. Frontend nigdy nie wysyła userId w requestach tego flow; backend zawsze bierze tożsamość z JWT (claim sub) i zwraca dane wyłącznie dla bieżącego konta.
++1) GET /api/mobile/v1/notifications/counter — pobranie licznika. Response 200: { unreadCount: integer >= 0, calculatedAt: ISO-8601 UTC string }. unreadCount jest obowiązkowe, nigdy null, a wartość 0 ma być zwracana jawnie (UI nie może ukrywać licznika jako pustego stringa).
++2) GET /api/mobile/v1/notifications?read=false&limit=50&cursor=<string> — lista nieprzeczytanych powiadomień. Response 200: { items: [{ id: uuid, title: string, body: string, type: string, createdAt: ISO-8601 UTC string, isRead: false, readAt: null }], nextCursor: string|null }. Parametr read akceptuje tylko false w tym widoku.
++3) PATCH /api/mobile/v1/notifications/{notificationId}/read — oznaczenie jednego powiadomienia jako przeczytanego. Path param notificationId: uuid. Request body: brak. Response 200: { notificationId: uuid, isRead: true, readAt: ISO-8601 UTC string, unreadCount: integer >= 0 }. Endpoint jest idempotentny: ponowne wywołanie dla już przeczytanego powiadomienia zwraca 200 z aktualnym unreadCount.
++Walidacje i błędy wspólne: 401 UNAUTHENTICATED — brak/nieprawidłowy Bearer JWT; 400 VALIDATION_ERROR — niepoprawny UUID lub query parametry spoza kontraktu; 403 FORBIDDEN — próba operacji na cudzym notificationId lub dostępu poza bieżącym użytkownikiem; 404 NOTIFICATION_NOT_FOUND — notificationId nie istnieje. Dla security nie zwracamy userId w odpowiedziach API.
++Warstwa danych: tabela notifications(id uuid PK, user_id uuid NOT NULL, title varchar(200) NOT NULL, body text NOT NULL, type varchar(50) NOT NULL, is_read boolean NOT NULL DEFAULT false, read_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL). Indeks wymagany: (user_id, is_read, created_at DESC). Licznik jest liczony jako COUNT(*) WHERE user_id = :currentUserId AND is_read = false; nie tworzymy osobnej kolumny counter.
++Zdarzenia wewnętrzne: notification.created oraz notification.read (payload: notificationId, userId, occurredAt) służą do invalidacji cache/push; frontend po pushu lub powrocie na ekran odświeża GET /counter.
++UI kontrakt: badge licznika ma być widoczny przy każdej wartości liczbowej, w tym 0; po otrzymaniu nowego powiadomienia rośnie po odświeżeniu GET /counter, a po oznaczeniu jako przeczytane maleje po odpowiedzi PATCH /read.
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
+diff --git a/src/PolicyPlatform.Api/Controllers/NotificationsController.cs b/src/PolicyPlatform.Api/Controllers/NotificationsController.cs
+new file mode 100644
+index 0000000..b636714
+--- /dev/null
++++ b/src/PolicyPlatform.Api/Controllers/NotificationsController.cs
+@@ -0,0 +1,110 @@
++using System.IdentityModel.Tokens.Jwt;
++using Microsoft.AspNetCore.Authorization;
++using Microsoft.AspNetCore.Mvc;
++using PolicyPlatform.Application.Notifications;
++
++namespace PolicyPlatform.Api.Controllers;
++
++[ApiController]
++[Authorize]
++[Route("api/mobile/v1/notifications")]
++public sealed class NotificationsController : ControllerBase
++{
++    private static readonly HashSet<string> AllowedListQueryParams = new(StringComparer.OrdinalIgnoreCase)
++    {
++        "read", "limit", "cursor",
++    };
++
++    private readonly NotificationService _notifications;
++
++    public NotificationsController(NotificationService notifications) => _notifications = notifications;
++
++    [HttpGet("counter")]
++    public async Task<ActionResult<NotificationCounterDto>> GetCounter(CancellationToken ct)
++    {
++        if (!TryGetCurrentUserId(out var userId))
++        {
++            return UnauthenticatedError();
++        }
++
++        return Ok(await _notifications.GetCounterAsync(userId, ct));
++    }
++
++    [HttpGet]
++    public async Task<ActionResult<NotificationListDto>> GetUnread(
++        [FromQuery] string? read, [FromQuery] int limit = 50, [FromQuery] string? cursor = null, CancellationToken ct = default)
++    {
++        foreach (var key in Request.Query.Keys)
++        {
++            if (!AllowedListQueryParams.Contains(key))
++            {
++                return ValidationError($"Unsupported query parameter '{key}'.");
++            }
++        }
++
++        if (read is not null && read != "false")
++        {
++            return ValidationError("Query parameter 'read' only supports 'false'.");
++        }
++
++        if (limit <= 0)
++        {
++            return ValidationError("Query parameter 'limit' must be a positive integer.");
++        }
++
++        if (!TryGetCurrentUserId(out var userId))
++        {
++            return UnauthenticatedError();
++        }
++
++        return Ok(await _notifications.GetUnreadAsync(userId, limit, cursor, ct));
++    }
++
++    [HttpPatch("{notificationId}/read")]
++    public async Task<ActionResult<NotificationReadResultDto>> MarkAsRead(string notificationId, CancellationToken ct)
++    {
++        if (!Guid.TryParse(notificationId, out var parsedId))
++        {
++            return ValidationError("Path parameter 'notificationId' must be a valid UUID.");
++        }
++
++        if (!TryGetCurrentUserId(out var userId))
++        {
++            return UnauthenticatedError();
++        }
++
++        try
++        {
++            return Ok(await _notifications.MarkAsReadAsync(userId, parsedId, ct));
++        }
++        catch (NotificationAccessException ex) when (ex.Error == NotificationAccessError.NotFound)
++        {
++            return NotFoundError("Notification was not found.");
++        }
++        catch (NotificationAccessException ex) when (ex.Error == NotificationAccessError.Forbidden)
++        {
++            return ForbiddenError("Notification does not belong to the current user.");
++        }
++    }
++
++    private bool TryGetCurrentUserId(out Guid userId)
++    {
++        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
++        return Guid.TryParse(sub, out userId);
++    }
++
++    private ObjectResult UnauthenticatedError()
++        => ErrorResult(StatusCodes.Status401Unauthorized, "UNAUTHENTICATED", "Bearer token is missing a valid subject claim.");
++
++    private ObjectResult ValidationError(string message)
++        => ErrorResult(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", message);
++
++    private ObjectResult NotFoundError(string message)
++        => ErrorResult(StatusCodes.Status404NotFound, "NOTIFICATION_NOT_FOUND", message);
++
++    private ObjectResult ForbiddenError(string message)
++        => ErrorResult(StatusCodes.Status403Forbidden, "FORBIDDEN", message);
++
++    private ObjectResult ErrorResult(int statusCode, string code, string message)
++        => new(new { code, message }) { StatusCode = statusCode };
++}
+diff --git a/src/PolicyPlatform.Api/PolicyPlatform.Api.csproj b/src/PolicyPlatform.Api/PolicyPlatform.Api.csproj
+index f78b87a..6f7b1cc 100644
+--- a/src/PolicyPlatform.Api/PolicyPlatform.Api.csproj
++++ b/src/PolicyPlatform.Api/PolicyPlatform.Api.csproj
+@@ -7,6 +7,7 @@
+   </PropertyGroup>
+ 
+   <ItemGroup>
++    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
+     <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.17" />
+     <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0">
+       <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
+diff --git a/src/PolicyPlatform.Api/Program.cs b/src/PolicyPlatform.Api/Program.cs
+index fe20a14..a8bef06 100644
+--- a/src/PolicyPlatform.Api/Program.cs
++++ b/src/PolicyPlatform.Api/Program.cs
+@@ -1,4 +1,7 @@
++using System.Text;
+ using System.Text.Json.Serialization;
++using Microsoft.AspNetCore.Authentication.JwtBearer;
++using Microsoft.IdentityModel.Tokens;
+ using PolicyPlatform.Infrastructure;
+ 
+ var builder = WebApplication.CreateBuilder(args);
+@@ -8,6 +11,37 @@
+ builder.Services.AddOpenApi();
+ builder.Services.AddPolicyPlatformInfrastructure(builder.Configuration);
+ 
++// Bearer JWT auth for the mobile notifications flow (AISDLC-156): identity always comes
++// from the token's "sub" claim, never from a request parameter.
++var jwtSection = builder.Configuration.GetSection("Jwt");
++builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
++    .AddJwtBearer(options =>
++    {
++        // Keep the raw "sub" claim name instead of ASP.NET's default ClaimTypes remap.
++        options.MapInboundClaims = false;
++        options.TokenValidationParameters = new TokenValidationParameters
++        {
++            ValidateIssuer = true,
++            ValidIssuer = jwtSection["Issuer"],
++            ValidateAudience = true,
++            ValidAudience = jwtSection["Audience"],
++            ValidateIssuerSigningKey = true,
++            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!)),
++            ValidateLifetime = true,
++        };
++        options.Events = new JwtBearerEvents
++        {
++            OnChallenge = async context =>
++            {
++                context.HandleResponse();
++                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
++                context.Response.ContentType = "application/json";
++                await context.Response.WriteAsJsonAsync(new { code = "UNAUTHENTICATED", message = "Missing or invalid Bearer token." });
++            },
++        };
++    });
++builder.Services.AddAuthorization();
++
+ var app = builder.Build();
+ 
+ if (app.Environment.IsDevelopment())
+@@ -16,6 +50,7 @@
+ }
+ 
+ app.UseHttpsRedirection();
++app.UseAuthentication();
+ app.UseAuthorization();
+ app.MapControllers();
+ app.UseDefaultFiles();
+diff --git a/src/PolicyPlatform.Api/appsettings.Development.json b/src/PolicyPlatform.Api/appsettings.Development.json
+index 0c208ae..f051f94 100644
+--- a/src/PolicyPlatform.Api/appsettings.Development.json
++++ b/src/PolicyPlatform.Api/appsettings.Development.json
+@@ -4,5 +4,8 @@
+       "Default": "Information",
+       "Microsoft.AspNetCore": "Warning"
+     }
++  },
++  "Jwt": {
++    "SigningKey": "dev-only-signing-key-not-for-
... diff truncated ...
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