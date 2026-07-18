You are the DOCUMENTATION agent in a specialized worker pipeline (separate agents handle
coding, unit tests, e2e tests, database, and review — stay scoped to documentation only,
never touch executable logic).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under documentation:
- Repository: LordIllidan/ai-coding-demo
- PR: #19 AI: [AISDLC-140] [QA] Testy integracyjne i kontraktowe dla last-payout
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/19
- Branch: ai-coding/aisdlc-140-qa-testy-integracyjne-i-kontraktowe-dla-last-pay-29643128953

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-140-coding-prompt.md b/ai-coding-runs/aisdlc-140-coding-prompt.md
new file mode 100644
index 0000000..dd36e26
--- /dev/null
+++ b/ai-coding-runs/aisdlc-140-coding-prompt.md
@@ -0,0 +1,30 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-140 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: [QA] Testy integracyjne i kontraktowe dla last-payout
+
+Task description:
+~~~markdown
+Parent story: AISDLC-118 — Wyświetlenie ostatniej wypłaconej transzy odszkodowania w aplikacji mobilnej
+
+Testy integracyjne i kontraktowe dla GET /api/mobile/me/claims/last-payout oraz scenariuszy read-only: 200/401/403/404/503 i brak możliwości edycji przez POST/PATCH/PUT. Pliki TODO: testy backend, e2e/mocked API, regresja bezpieczeństwa.
+KONTRAKT: Endpoint: GET /api/mobile/me/claims/last-payout. Autoryzacja: wymagany Bearer JWT (klient zalogowany); request NIE zawiera customerId, policyId ani claimId w path/query/body — backend identyfikuje dane wyłącznie po subject/customerId z tokena.
+Request: brak body, brak parametrów. Odpowiedź 200: { claimNumber: string, amount: { value: string(decimal, 2 miejsca), currency: string(3, domyślnie PLN) }, payoutDate: string(YYYY-MM-DD), readOnly: true }. Ekran jest wyłącznie do odczytu, UI nie może wysyłać żadnego PUT/PATCH/POST dla tej historii.
+Mapowanie danych: claimNumber <- claim.claim_number; amount.value <- claim_payout.amount_gross; amount.currency <- claim_payout.currency_code; payoutDate <- claim_payout.paid_date (lub data businessowa wyliczona z claim_payout.paid_at). Backend pobiera TYLKO ostatni rekord spełniający: customer_id = JWT.sub/customerId, status = 'PAID', ORDER BY paid_at DESC LIMIT 1.
+Błędy/walidacje: 401 AUTH_REQUIRED (brak/nieprawidłowy token), 403 FORBIDDEN_CROSS_CUSTOMER (token nieuprawniony do danych), 404 LAST_PAYOUT_NOT_FOUND (brak wypłaconej transzy dla zalogowanego klienta), 503 DATA_SOURCE_TIMEOUT (timeout/awaria źródła danych — kontrolowany komunikat, bez nieaktualnych danych), 500 INTERNAL_ERROR (awaria nieoczekiwana).
+Baza: odczyt z tabeli claim_payout; wspólne kolumny: id, claim_id, customer_id, amount_gross, currency_code, paid_at, status. Żadna warstwa nie może opierać się na podmianie identyfikatora przez klienta — identyfikator kontekstu pochodzi tylko z JWT.
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
diff --git a/src/PolicyPlatform.Api/Controllers/MobileLastPayoutController.cs b/src/PolicyPlatform.Api/Controllers/MobileLastPayoutController.cs
new file mode 100644
index 0000000..dd35425
--- /dev/null
+++ b/src/PolicyPlatform.Api/Controllers/MobileLastPayoutController.cs
@@ -0,0 +1,36 @@
+using Microsoft.AspNetCore.Mvc;
+using PolicyPlatform.Api.ErrorHandling;
+using PolicyPlatform.Application.Claims;
+
+namespace PolicyPlatform.Api.Controllers;
+
+/// <summary>Mobile "my last payout" screen — read-only. Only GET is mapped for this route: no
+/// POST/PUT/PATCH exists here by design, so any write attempt gets ASP.NET's default 405/404
+/// rather than reaching application logic.</summary>
+[ApiController]
+[Route("api/mobile/me/claims/last-payout")]
+public sealed class MobileLastPayoutController : ControllerBase
+{
+    private readonly LastPayoutService _service;
+
+    public MobileLastPayoutController(LastPayoutService service) => _service = service;
+
+    [HttpGet]
+    public async Task<IActionResult> GetLastPayout(CancellationToken ct)
+    {
+        var correlationId = HttpContext.TraceIdentifier;
+
+        try
+        {
+            var authorizationHeaderValue = Request.Headers.Authorization.ToString();
+            var result = await _service.GetLastPayoutAsync(authorizationHeaderValue, ct);
+            return Ok(result);
+        }
+        catch (Exception ex) when (ex is AuthRequiredException or ForbiddenCrossCustomerException
+            or LastPayoutNotFoundException or DataSourceTimeoutException)
+        {
+            var (statusCode, envelope) = LastPayoutErrorMapper.Map(ex, correlationId);
+            return StatusCode(statusCode, envelope);
+        }
+    }
+}
diff --git a/src/PolicyPlatform.Api/ErrorHandling/LastPayoutErrorMapper.cs b/src/PolicyPlatform.Api/ErrorHandling/LastPayoutErrorMapper.cs
new file mode 100644
index 0000000..d1a1f79
--- /dev/null
+++ b/src/PolicyPlatform.Api/ErrorHandling/LastPayoutErrorMapper.cs
@@ -0,0 +1,27 @@
+using Microsoft.AspNetCore.Http;
+using PolicyPlatform.Application.Claims;
+
+namespace PolicyPlatform.Api.ErrorHandling;
+
+/// <summary>Maps GET /api/mobile/me/claims/last-payout failures to the shared error envelope
+/// contract: { code, message, retryable, correlationId }.</summary>
+public static class LastPayoutErrorMapper
+{
+    public static (int StatusCode, ErrorEnvelope Envelope) Map(Exception exception, string correlationId) =>
+        exception switch
+        {
+            AuthRequiredException => (
+                StatusCodes.Status401Unauthorized,
+                new ErrorEnvelope("AUTH_REQUIRED", "Access token is missing or invalid.", false, correlationId)),
+            ForbiddenCrossCustomerException => (
+                StatusCodes.Status403Forbidden,
+                new ErrorEnvelope("FORBIDDEN_CROSS_CUSTOMER", "Token is not authorized for this customer's data.", false, correlationId)),
+            LastPayoutNotFoundException => (
+                StatusCodes.Status404NotFound,
+                new ErrorEnvelope("LAST_PAYOUT_NOT_FOUND", "No paid payout was found for this customer.", false, correlationId)),
+            DataSourceTimeoutException => (
+                StatusCodes.Status503ServiceUnavailable,
+                new ErrorEnvelope("DATA_SOURCE_TIMEOUT", "The payout data source timed out. Please try again.", true, correlationId)),
+            _ => throw new ArgumentOutOfRangeException(nameof(exception), exception, "Unmapped last-payout fetch exception."),
+        };
+}
diff --git a/src/PolicyPlatform.Application/Abstractions/ICustomerIdentityResolver.cs b/src/PolicyPlatform.Application/Abstractions/ICustomerIdentityResolver.cs
new file mode 100644
index 0000000..e2eeb85
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/ICustomerIdentityResolver.cs
@@ -0,0 +1,11 @@
+namespace PolicyPlatform.Application.Abstractions;
+
+/// <summary>Resolves the authenticated customer's id from a raw Authorization header value.
+/// Never accepts a client-supplied customerId — the identity comes exclusively from the
+/// bearer token. Throws PolicyPlatform.Application.Claims.AuthRequiredException (401) when the
+/// token is missing/malformed/expired, or ForbiddenCrossCustomerException (403) when the token
+/// is structurally valid but not scoped to the caller's own customer data.</summary>
+public interface ICustomerIdentityResolver
+{
+    Guid ResolveCustomerId(string? authorizationHeaderValue);
+}
diff --git a/src/PolicyPlatform.Application/Abstractions/ILastPayoutRepository.cs b/src/PolicyPlatform.Application/Abstractions/ILastPayoutRepository.cs
new file mode 100644
index 0000000..4274eb4
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/ILastPayoutRepository.cs
@@ -0,0 +1,12 @@
+using PolicyPlatform.Application.Claims;
+
+namespace PolicyPlatform.Application.Abstractions;
+
+/// <summary>Read-only access to the last PAID claim_payout row for a customer. Callers pass
+/// only the customerId resolved from the caller's JWT — never a client-supplied identifier.
+/// Implementations must throw DataSourceTimeoutException (rather than falling back to a cached
+/// or stale row) when the underlying data source is unreachable or times out.</summary>
+public interface ILastPayoutRepository
+{
+    Task<LastPayoutRecord?> GetLastPayoutAsync(Guid customerId, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Application/Claims/LastPayoutDtos.cs b/src/PolicyPlatform.Application/Claims/LastPayoutDtos.cs
new file mode 100644
index 0000000..8cc12cd
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/LastPayoutDtos.cs
@@ -0,0 +1,26 @@
+using System.Globalization;
+
+namespace PolicyPlatform.Application.Claims;
+
+/// <summary>Raw projection of the last PAID claim_payout row joined with its claim, as read
+/// from the data source. All values are already in the shape the mobile contract expects.</summary>
+public sealed record LastPayoutRecord(
+    string ClaimNumber,
+    decimal AmountGross,
+    string CurrencyCode,
+    DateOnly PayoutDate);
+
+public sealed record MoneyDto(string Value, string Currency);
+
+/// <summary>Response body for GET /api/mobile/me/claims/last-payout. ReadOnly is always true —
+/// this screen never accepts writes.</summary>
+public sealed record LastPayoutResponse(string ClaimNumber, MoneyDto Amount, string PayoutDate, bool ReadOnly)
+{
+    public static LastPayoutResponse FromRecord(LastPayoutRecord record) => new(
+        record.ClaimNumber,
+        new MoneyDto(record.AmountGross.ToString("F2", CultureInfo.InvariantCulture), record.CurrencyCode),
+        record.PayoutDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
+        ReadOnly: true);
+}
+
+public sealed record ErrorEnvelope(string Code, string Message, bool Retryable, string CorrelationId);
diff --git a/src/PolicyPlatform.Application/Claims/LastPayoutExceptions.cs b/src/PolicyPlatform.Application/Claims/LastPayoutExceptions.cs
new file mode 100644
index 0000000..b5f2b49
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/LastPayoutExceptions.cs
@@ -0,0 +1,17 @@
+namespace PolicyPlatform.Application.Claims;
+
+/// <summary>Authorization header missing, malformed, or the token is expired/unparseable.</summary>
+public sealed class AuthRequiredException() : Exception("Access token is missing or invalid.");
+
+/// <summary>Token is structurally valid but not scoped to the caller's own customer data.</summary>
+public sealed class ForbiddenCrossCustomerException() : Exception("Token is not authorized for this customer's data.");
+
+/// <summary>No PAID claim_payout row exists for the resolved customer.</summary>
+public sealed class LastPayoutNotFoundException(Guid customerId) : Exception($"No paid payout found for customer {customerId}.")
+{
+    public Guid CustomerId { get; } = customerId;
+}
+
+/// <summary>The payout data source timed out or is unreachable. Must never be swallowed in
+/// favor of returning stale data.</summary>
+public sealed class DataSourceTimeoutException() : Exception("The payout data source timed out.");
diff --git a/src/PolicyPlatform.Application/Claims/LastPayoutService.cs b/src/PolicyPlatform.Application/Claims/LastPayoutService.cs
new file mode 100644
index 0000000..0fcd210
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/LastPayoutService.cs
@@ -0,0 +1,28 @@
+using PolicyPlatform.Application.Abstractions;
+
+namespace PolicyPlatform.Application.Claims;
+
+/// <summary>Use-case for GET /api/mobile/me/claims/last-payout. The customer identity comes
+/// exclusively from the caller's JWT (via ICustomerIdentityResolver) — the request itself
+/// carries no customerId, policyId, or claimId.</summary>
+public sealed class LastPayoutService
+{
+    private readonly ICustomerIdentityResolver _identity;
+    private readonly ILastPayoutRepository _repository;
+
+    public LastPayoutService(ICustomerIdentityResolver identity, ILastPayoutRepository repository)
+    {
+        _identity = identity;
+        _repository = repository;
+    }
+
+    public async Task<LastPayoutResponse> GetLastPayoutAsync(string? authorizationHeaderValue, CancellationToken ct = default)
+    {
+        var customerId = _identity.ResolveCustomerId(authorizationHeaderValue);
+
+        var record = await _repository.GetLastPayoutAsync(customerId, ct)
+            ?? throw new LastPayoutNotFoundException(customerId);
+
+        return LastPayoutResponse.FromRecord(record);
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
index b5fa109..7308e3c 100644
--- a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
+++ b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
@@ -7,6 +7,7 @@
 using PolicyPlatform.Application.Policies;
 using PolicyPlatform.Infrastructure.Numbering;
 using PolicyPlatform.Infrastructure.Persistence;
+using PolicyPlatform.Infrastructure.Security;
 
 namespace PolicyPlatform.Infrastructure;
 
@@ -41,6 +42,12 @@ public static IServiceCollection AddPolicyPlatformInfrastructure(
         // piece of work) — in-memory keeps the theft-claim validation flow runnable now.
         services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
         services.AddScoped<ClaimService>();
+
+        // Same story for claim_payout: no durable store yet, so the last-payout read model
+        // is backed by an in-memory stand-in until an EF Core provider lands.
+        services.AddSingleton<ILastPayoutRepository, InMemoryLastPayoutRepository>();
+        services.AddSingleton<ICustomerIdentityResolver, JwtCustomerIdentityResolver>();
+        services.AddScoped<LastPayoutService>();
         return services;
     }
 }
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/InMemoryLastPayoutRepository.cs b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryLastPayoutRepository.cs
new file mode 100644
index 0000000..d24111b
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryLastPayoutRepository.cs
@@ -0,0 +1,42 @@
+using System.Collections.Concurrent;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Application.Claims;
+
+namespace PolicyPlatform.Infrastructure.Persistence;
+
+/// <summary>Process-lifetime in-memory stand-in for the claim_payout table. Swap for an EF
+/// Core provider once claim payouts need durable persistence — the Application layer only
+/// depends on ILastPayoutRepository.</summary>
+public sealed class InMemoryLastPayoutRepository : ILastPayoutRepository
+{
+    private readonly ConcurrentDictionary<Guid, List<CustomerPayout>> _payoutsByCustomer = new();
+
+    public Task<LastPayoutRecord?> GetLastPayoutAsync(Guid customerId, CancellationToken ct = default)
+    {
+        if (!_payoutsByCustomer.TryGetValue(customerId, out var payouts))
+        {
+            return Task.FromResult<LastPayoutRecord?>(null);
+        }
+
+        var last = payouts
+            .Where(p => p.Status == "PAID")
+            .OrderByDescending(p => p.PaidAt)
+            .FirstOrDefault();
+
+        return Task.FromResult(last is null
+            ? null
+            : new LastPayoutRecord(last.ClaimNumber, last.AmountGross, last.CurrencyCode, DateOnly.FromDateTime(last.PaidAt.UtcDateTime)));
+    }
+
+    public void Seed(Guid customerId, CustomerPayout payout)
+        => _payoutsByCustomer.GetOrAdd(customerId, static _ => []).Add(payout);
+}
+
+/// <summary>Row shape mirroring the shared claim_payout columns (amount_gross, currency_code,
+/// paid_at, status) plus the joined claim_number, for the in-memory stand-in only.</summary>
+public sealed record CustomerPayout(
+    string ClaimNumber,
+    decimal AmountGross,
+    string CurrencyCode,
+    DateTimeOffset PaidAt,
+    string Status);
diff --git a/src/PolicyPlatform.Infrastructure/Security/JwtCustomerIdentityResolver.cs b/src/PolicyPlatform.Infrastructure/Security/JwtCustomerIdentityResolver.cs
new file mode 100644
index 0000000..4a1d9f2
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Security/JwtCustomerIdentityResolver.cs
@@ -0,0 +1,78 @@
+using System.Text;
+using System.Text.Json;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Application.Claims;
+
+namespace PolicyPlatform.Infrastructure.Security;
+
+/// <summary>Resolves the customer id from the unverified payload of a bearer JWT. No
+/// signature/issuer verification is performed yet — that requires a real IdP integration and
+/// is tracked as follow-up work; this resolver enforces structure, expiry, subject presence,
+/// and audience only. Never trusts a customerId supplied anywhere but the token's "sub".</summary>
+public sealed class JwtCustomerIdentityResolver : ICustomerIdentityResolver
+{
+    private const string BearerPrefix = "Bearer ";
+    private const string ExpectedAudience = "mobile-client";
+
+    public Guid ResolveCustomerId(string? authorizationHeaderValue)
+    {
+        if (string.IsNullOrWhiteSpace(authorizationHeaderValue) ||
+            !authorizationHeaderValue.StartsWith(BearerPrefix, StringComparison.Ordinal))
+        {
+            throw new AuthRequiredException();
+        }
+
+        var token = authorizationHeaderValue[BearerPrefix.Length..].Trim();
+        var segments = token.Split('.');
+        if (segments.Length != 3 || segments[0].Length == 0 || segments[1].Length == 0)
+        {
+            throw new AuthRequiredException();
+        }
+
+        JsonElement payload;
+        try
+        {
+            var json = Encoding.UTF8.GetString(Base64UrlDecode(segments[1]));
+            payload = JsonSerializer.Deserialize<JsonElement>(json);
+        }
+        catch (Exception ex) when (ex is FormatException or JsonException)
+        {
+            throw new AuthRequiredException();
+        }
+
+        if (payload.ValueKind != JsonValueKind.Object)
+        {
+            throw new AuthRequiredException();
+        }
+
+        if (payload.TryGetProperty("exp", out var expElement) &&
+            expElement.TryGetInt64(out var exp) &&
+            DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow)
+        {
+            throw new AuthRequiredException();
+        }
+
+        if (!payload.TryGetProperty("sub", out var subElement) ||
+            subElement.ValueKind != JsonValueKind.String ||
+            !Guid.TryParse(subElement.GetString(), out var customerId))
+        {
+            throw new AuthRequiredException();
+        }
+
+        if (payload.TryGetProperty("aud", out var audElement) &&
+            audElement.ValueKind == JsonValueKind.String &&
+            !string.Equals(audElement.GetString(), ExpectedAudience, StringComparison.Ordinal))
+        {
+            throw new ForbiddenCrossCustomerException();
+        }
+
+        return customerId;
+    }
+
+    private static byte[] Base64UrlDecode(string input)
+    {
+        var padded = input.Replace('-', '+').Replace('_', '/');
+        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
+        return Convert.FromBase64String(padded);
+    }
+}

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