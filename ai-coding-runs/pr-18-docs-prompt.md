You are the DOCUMENTATION agent in a specialized worker pipeline (separate agents handle
coding, unit tests, e2e tests, database, and review — stay scoped to documentation only,
never touch executable logic).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under documentation:
- Repository: LordIllidan/ai-coding-demo
- PR: #18 AI: [AISDLC-134] Obsłużyć błąd pobrania ostatniej wypłaconej transzy bez serwowania starych danych
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/18
- Branch: ai-coding/aisdlc-134-obs-u-y-b-d-pobrania-ostatniej-wyp-aconej-transz-29643061070

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-134-coding-prompt.md b/ai-coding-runs/aisdlc-134-coding-prompt.md
new file mode 100644
index 0000000..ebc7ae6
--- /dev/null
+++ b/ai-coding-runs/aisdlc-134-coding-prompt.md
@@ -0,0 +1,31 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-134 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: Obsłużyć błąd pobrania ostatniej wypłaconej transzy bez serwowania starych danych
+
+Task description:
+~~~markdown
+Parent story: AISDLC-120 — Komunikat błędu i ponowienia, gdy nie uda się pobrać danych transzy
+
+Implementacja backendu dla GET /api/claims/{claimId}/last-paid-tranche: walidacja claimId jako UUID, mapowanie błędów 401/403/404/503/504 do wspólnego envelope oraz brak zwracania cache/starych danych po timeout/circuit breaker. Pliki do sprawdzenia: kontroler/handler endpointu claims, serwis pobierania transzy, mapper błędów, read model/repocitory claim_last_paid_tranche_view. TODO: potwierdzić, że logika używa wyłącznie claimId i że odpowiedź 200 zwraca lastPaidTranche=null, gdy brak danych.
+KONTRAKT: KONTRAKT (TechLeadAgent):
+Endpoint: GET /api/claims/{claimId}/last-paid-tranche. Request przyjmuje wyłącznie path param claimId: string (UUID) oraz nagłówek Authorization: Bearer <token>; nie wolno używać customerId ani policyId w request ani w logice mapowania.
+200 OK: { claimId: string(UUID), lastPaidTranche: { trancheId: string(UUID), trancheNumber: integer, status: 'PAID', paidAt: string(ISO-8601), grossAmount: number(2 dp), currency: string(ISO-4217) } | null, fetchedAt: string(ISO-8601) }. Przy braku danych lastPaidTranche = null.
+Kody błędów: 401 INVALID_TOKEN (brak/wygaśnięty token), 403 CLAIM_ACCESS_DENIED (brak scope do claimId), 404 CLAIM_NOT_FOUND, 503 TRANCHE_SERVICE_UNAVAILABLE (circuit breaker/downstream unavailable), 504 TRANCHE_SERVICE_TIMEOUT (przekroczony timeout integracji). Wspólny error envelope: { code: string, message: string, retryable: boolean, correlationId: string }.
+Walidacje i zachowanie UI: claimId obowiązkowy i musi być UUID; backend nie może zwracać danych z cache/starych odpowiedzi po timeout/circuit breaker; frontend po każdym non-2xx czyści aktualnie widoczne dane, pokazuje komunikat błędu i przycisk 'Ponów' wywołujący ponownie ten sam GET dla tego samego claimId.
+DB/read model: claim_last_paid_tranche_view(claim_id PK, tranche_id, tranche_number, status, paid_at, gross_amount, currency, source_updated_at, refreshed_at). Kolumny identyfikacyjne i relacyjne opierają się na claim_id, nie na customer_id/policy_id.
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
diff --git a/src/PolicyPlatform.Api/Controllers/ClaimTranchesController.cs b/src/PolicyPlatform.Api/Controllers/ClaimTranchesController.cs
new file mode 100644
index 0000000..9ccc717
--- /dev/null
+++ b/src/PolicyPlatform.Api/Controllers/ClaimTranchesController.cs
@@ -0,0 +1,33 @@
+using Microsoft.AspNetCore.Mvc;
+using PolicyPlatform.Api.ErrorHandling;
+using PolicyPlatform.Application.Claims;
+
+namespace PolicyPlatform.Api.Controllers;
+
+[ApiController]
+[Route("api/claims")]
+public sealed class ClaimTranchesController : ControllerBase
+{
+    private readonly ClaimLastPaidTrancheService _service;
+
+    public ClaimTranchesController(ClaimLastPaidTrancheService service) => _service = service;
+
+    [HttpGet("{claimId:guid}/last-paid-tranche")]
+    public async Task<IActionResult> GetLastPaidTranche(Guid claimId, CancellationToken ct)
+    {
+        var correlationId = HttpContext.TraceIdentifier;
+
+        try
+        {
+            var authorizationHeaderValue = Request.Headers.Authorization.ToString();
+            var result = await _service.GetLastPaidTrancheAsync(claimId, authorizationHeaderValue, ct);
+            return Ok(result);
+        }
+        catch (Exception ex) when (ex is InvalidTokenException or ClaimAccessDeniedException or ClaimNotFoundException
+            or TrancheServiceUnavailableException or TrancheServiceTimeoutException)
+        {
+            var (statusCode, envelope) = TrancheFetchErrorMapper.Map(ex, correlationId);
+            return StatusCode(statusCode, envelope);
+        }
+    }
+}
diff --git a/src/PolicyPlatform.Api/ErrorHandling/TrancheFetchErrorMapper.cs b/src/PolicyPlatform.Api/ErrorHandling/TrancheFetchErrorMapper.cs
new file mode 100644
index 0000000..60a7902
--- /dev/null
+++ b/src/PolicyPlatform.Api/ErrorHandling/TrancheFetchErrorMapper.cs
@@ -0,0 +1,30 @@
+using Microsoft.AspNetCore.Http;
+using PolicyPlatform.Application.Claims;
+
+namespace PolicyPlatform.Api.ErrorHandling;
+
+/// <summary>Maps last-paid-tranche fetch failures to the shared error envelope contract:
+/// { code, message, retryable, correlationId }.</summary>
+public static class TrancheFetchErrorMapper
+{
+    public static (int StatusCode, ErrorEnvelope Envelope) Map(Exception exception, string correlationId) =>
+        exception switch
+        {
+            InvalidTokenException => (
+                StatusCodes.Status401Unauthorized,
+                new ErrorEnvelope("INVALID_TOKEN", "Access token is missing or expired.", false, correlationId)),
+            ClaimAccessDeniedException => (
+                StatusCodes.Status403Forbidden,
+                new ErrorEnvelope("CLAIM_ACCESS_DENIED", "You do not have access to this claim.", false, correlationId)),
+            ClaimNotFoundException => (
+                StatusCodes.Status404NotFound,
+                new ErrorEnvelope("CLAIM_NOT_FOUND", "Claim was not found.", false, correlationId)),
+            TrancheServiceUnavailableException => (
+                StatusCodes.Status503ServiceUnavailable,
+                new ErrorEnvelope("TRANCHE_SERVICE_UNAVAILABLE", "Tranche service is temporarily unavailable.", true, correlationId)),
+            TrancheServiceTimeoutException => (
+                StatusCodes.Status504GatewayTimeout,
+                new ErrorEnvelope("TRANCHE_SERVICE_TIMEOUT", "Tranche service did not respond in time.", true, correlationId)),
+            _ => throw new ArgumentOutOfRangeException(nameof(exception), exception, "Unmapped tranche fetch exception."),
+        };
+}
diff --git a/src/PolicyPlatform.Application/Abstractions/IClaimAccessValidator.cs b/src/PolicyPlatform.Application/Abstractions/IClaimAccessValidator.cs
new file mode 100644
index 0000000..9a76d38
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/IClaimAccessValidator.cs
@@ -0,0 +1,9 @@
+namespace PolicyPlatform.Application.Abstractions;
+
+/// <summary>Validates the caller's bearer token and its scope for a given claim.
+/// Throws PolicyPlatform.Application.Claims.InvalidTokenException (401) or
+/// ClaimAccessDeniedException (403) when access must be refused.</summary>
+public interface IClaimAccessValidator
+{
+    Task EnsureAccessAsync(string? authorizationHeaderValue, Guid claimId, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Application/Abstractions/IClaimLastPaidTrancheViewRepository.cs b/src/PolicyPlatform.Application/Abstractions/IClaimLastPaidTrancheViewRepository.cs
new file mode 100644
index 0000000..7e04476
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/IClaimLastPaidTrancheViewRepository.cs
@@ -0,0 +1,22 @@
+namespace PolicyPlatform.Application.Abstractions;
+
+/// <summary>Read model backing claim_last_paid_tranche_view. Keyed by claim_id only —
+/// never by customer_id/policy_id. Refreshed on a successful downstream fetch; must not
+/// be consulted to serve a response after a failed fetch (see ITrancheIntegrationClient).</summary>
+public sealed record ClaimLastPaidTrancheViewRecord(
+    Guid ClaimId,
+    Guid TrancheId,
+    int TrancheNumber,
+    string Status,
+    DateTimeOffset PaidAt,
+    decimal GrossAmount,
+    string Currency,
+    DateTimeOffset SourceUpdatedAt,
+    DateTimeOffset RefreshedAt);
+
+public interface IClaimLastPaidTrancheViewRepository
+{
+    Task<ClaimLastPaidTrancheViewRecord?> GetAsync(Guid claimId, CancellationToken ct = default);
+
+    Task UpsertAsync(ClaimLastPaidTrancheViewRecord record, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Application/Abstractions/ITrancheIntegrationClient.cs b/src/PolicyPlatform.Application/Abstractions/ITrancheIntegrationClient.cs
new file mode 100644
index 0000000..c47a3ab
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/ITrancheIntegrationClient.cs
@@ -0,0 +1,12 @@
+using PolicyPlatform.Application.Claims;
+
+namespace PolicyPlatform.Application.Abstractions;
+
+/// <summary>Live gateway to the downstream tranche system. Implementations own the
+/// timeout/circuit-breaker policy and must throw TrancheServiceUnavailableException or
+/// TrancheServiceTimeoutException on failure rather than returning a cached result —
+/// callers must never fall back to stale data when this call fails.</summary>
+public interface ITrancheIntegrationClient
+{
+    Task<LastPaidTrancheDto?> GetLastPaidTrancheAsync(Guid claimId, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Application/Claims/ClaimLastPaidTrancheService.cs b/src/PolicyPlatform.Application/Claims/ClaimLastPaidTrancheService.cs
new file mode 100644
index 0000000..dbdef20
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/ClaimLastPaidTrancheService.cs
@@ -0,0 +1,62 @@
+using PolicyPlatform.Application.Abstractions;
+
+namespace PolicyPlatform.Application.Claims;
+
+/// <summary>Use-case for GET /api/claims/{claimId}/last-paid-tranche. Uses claimId only —
+/// customerId/policyId never enter the lookup or authorization logic.</summary>
+public sealed class ClaimLastPaidTrancheService
+{
+    private readonly IClaimRepository _claims;
+    private readonly IClaimAccessValidator _accessValidator;
+    private readonly ITrancheIntegrationClient _trancheClient;
+    private readonly IClaimLastPaidTrancheViewRepository _view;
+    private readonly TimeProvider _clock;
+
+    public ClaimLastPaidTrancheService(
+        IClaimRepository claims,
+        IClaimAccessValidator accessValidator,
+        ITrancheIntegrationClient trancheClient,
+        IClaimLastPaidTrancheViewRepository view,
+        TimeProvider? clock = null)
+    {
+        _claims = claims;
+        _accessValidator = accessValidator;
+        _trancheClient = trancheClient;
+        _view = view;
+        _clock = clock ?? TimeProvider.System;
+    }
+
+    public async Task<LastPaidTrancheResult> GetLastPaidTrancheAsync(
+        Guid claimId, string? authorizationHeaderValue, CancellationToken ct = default)
+    {
+        await _accessValidator.EnsureAccessAsync(authorizationHeaderValue, claimId, ct);
+
+        var claim = await _claims.GetByIdAsync(claimId, ct)
+            ?? throw new ClaimNotFoundException(claimId);
+
+        // No try/catch around this call by design: on timeout or an open circuit breaker the
+        // client throws and that error must propagate as-is, never masked by a fallback read
+        // of the (possibly stale) claim_last_paid_tranche_view row.
+        var tranche = await _trancheClient.GetLastPaidTrancheAsync(claimId, ct);
+
+        var fetchedAt = _clock.GetUtcNow();
+
+        if (tranche is not null)
+        {
+            await _view.UpsertAsync(
+                new ClaimLastPaidTrancheViewRecord(
+                    claimId,
+                    tranche.TrancheId,
+                    tranche.TrancheNumber,
+                    tranche.Status,
+                    tranche.PaidAt,
+                    tranche.GrossAmount,
+                    tranche.Currency,
+                    tranche.PaidAt,
+                    fetchedAt),
+                ct);
+        }
+
+        return new LastPaidTrancheResult(claimId, tranche, fetchedAt);
+    }
+}
diff --git a/src/PolicyPlatform.Application/Claims/LastPaidTrancheDtos.cs b/src/PolicyPlatform.Application/Claims/LastPaidTrancheDtos.cs
new file mode 100644
index 0000000..4985b13
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/LastPaidTrancheDtos.cs
@@ -0,0 +1,13 @@
+namespace PolicyPlatform.Application.Claims;
+
+public sealed record LastPaidTrancheDto(
+    Guid TrancheId,
+    int TrancheNumber,
+    string Status,
+    DateTimeOffset PaidAt,
+    decimal GrossAmount,
+    string Currency);
+
+public sealed record LastPaidTrancheResult(Guid ClaimId, LastPaidTrancheDto? LastPaidTranche, DateTimeOffset FetchedAt);
+
+public sealed record ErrorEnvelope(string Code, string Message, bool Retryable, string CorrelationId);
diff --git a/src/PolicyPlatform.Application/Claims/TrancheFetchExceptions.cs b/src/PolicyPlatform.Application/Claims/TrancheFetchExceptions.cs
new file mode 100644
index 0000000..eea4f96
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/TrancheFetchExceptions.cs
@@ -0,0 +1,21 @@
+namespace PolicyPlatform.Application.Claims;
+
+/// <summary>Authorization header missing, malformed, or the token has expired.</summary>
+public sealed class InvalidTokenException() : Exception("Access token is missing or invalid.");
+
+/// <summary>Token is valid but does not carry a scope for the requested claim.</summary>
+public sealed class ClaimAccessDeniedException(Guid claimId) : Exception($"Access to claim {claimId} is denied.")
+{
+    public Guid ClaimId { get; } = claimId;
+}
+
+public sealed class ClaimNotFoundException(Guid claimId) : Exception($"Claim {claimId} was not found.")
+{
+    public Guid ClaimId { get; } = claimId;
+}
+
+/// <summary>Downstream tranche integration is unreachable or its circuit breaker is open.</summary>
+public sealed class TrancheServiceUnavailableException() : Exception("Tranche service is unavailable.");
+
+/// <summary>Downstream tranche integration did not respond within the configured timeout.</summary>
+public sealed class TrancheServiceTimeoutException() : Exception("Tranche service request timed out.");
diff --git a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
index b5fa109..3f19d5f 100644
--- a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
+++ b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
@@ -5,8 +5,10 @@
 using PolicyPlatform.Application.Claims;
 using PolicyPlatform.Application.Customers;
 using PolicyPlatform.Application.Policies;
+using PolicyPlatform.Infrastructure.Integration;
 using PolicyPlatform.Infrastructure.Numbering;
 using PolicyPlatform.Infrastructure.Persistence;
+using PolicyPlatform.Infrastructure.Security;
 
 namespace PolicyPlatform.Infrastructure;
 
@@ -41,6 +43,11 @@ public static IServiceCollection AddPolicyPlatformInfrastructure(
         // piece of work) — in-memory keeps the theft-claim validation flow runnable now.
         services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
         services.AddScoped<ClaimService>();
+
+        services.AddSingleton<IClaimLastPaidTrancheViewRepository, InMemoryClaimLastPaidTrancheViewRepository>();
+        services.AddSingleton<ITrancheIntegrationClient, InMemoryTrancheIntegrationClient>();
+        services.AddSingleton<IClaimAccessValidator, BearerClaimAccessValidator>();
+        services.AddScoped<ClaimLastPaidTrancheService>();
         return services;
     }
 }
diff --git a/src/PolicyPlatform.Infrastructure/Integration/InMemoryTrancheIntegrationClient.cs b/src/PolicyPlatform.Infrastructure/Integration/InMemoryTrancheIntegrationClient.cs
new file mode 100644
index 0000000..d311729
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Integration/InMemoryTrancheIntegrationClient.cs
@@ -0,0 +1,18 @@
+using System.Collections.Concurrent;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Application.Claims;
+
+namespace PolicyPlatform.Infrastructure.Integration;
+
+/// <summary>Local stand-in for the downstream tranche system. Swap for a real HTTP client
+/// wired with a timeout policy and circuit breaker (raising TrancheServiceTimeoutException /
+/// TrancheServiceUnavailableException on failure) once that integration exists.</summary>
+public sealed class InMemoryTrancheIntegrationClient : ITrancheIntegrationClient
+{
+    private readonly ConcurrentDictionary<Guid, LastPaidTrancheDto> _tranches = new();
+
+    public Task<LastPaidTrancheDto?> GetLastPaidTrancheAsync(Guid claimId, CancellationToken ct = default)
+        => Task.FromResult(_tranches.GetValueOrDefault(claimId));
+
+    public void Seed(Guid claimId, LastPaidTrancheDto tranche) => _tranches[claimId] = tranche;
+}
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimLastPaidTrancheViewRepository.cs b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimLastPaidTrancheViewRepository.cs
new file mode 100644
index 0000000..229c397
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimLastPaidTrancheViewRepository.cs
@@ -0,0 +1,20 @@
+using System.Collections.Concurrent;
+using PolicyPlatform.Application.Abstractions;
+
+namespace PolicyPlatform.Infrastructure.Persistence;
+
+/// <summary>Process-lifetime stand-in for the claim_last_paid_tranche_view read model.
+/// Swap for an EF Core-backed view once claims move to durable persistence.</summary>
+public sealed class InMemoryClaimLastPaidTrancheViewRepository : IClaimLastPaidTrancheViewRepository
+{
+    private readonly ConcurrentDictionary<Guid, ClaimLastPaidTrancheViewRecord> _rows = new();
+
+    public Task<ClaimLastPaidTrancheViewRecord?> GetAsync(Guid claimId, CancellationToken ct = default)
+        => Task.FromResult(_rows.GetValueOrDefault(claimId));
+
+    public Task UpsertAsync(ClaimLastPaidTrancheViewRecord record, CancellationToken ct = default)
+    {
+        _rows[record.ClaimId] = record;
+        return Task.CompletedTask;
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/Security/BearerClaimAccessValidator.cs b/src/PolicyPlatform.Infrastructure/Security/BearerClaimAccessValidator.cs
new file mode 100644
index 0000000..21c6342
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Security/BearerClaimAccessValidator.cs
@@ -0,0 +1,25 @@
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Application.Claims;
+
+namespace PolicyPlatform.Infrastructure.Security;
+
+/// <summary>Checks that a bearer token is present. Scope-to-claimId enforcement (403
+/// CLAIM_ACCESS_DENIED) requires a real IdP/token-introspection integration and is not yet
+/// wired up — this validator only ever throws InvalidTokenException, leaving the
+/// ClaimAccessDeniedException path ready for that follow-up work.</summary>
+public sealed class BearerClaimAccessValidator : IClaimAccessValidator
+{
+    private const string BearerPrefix = "Bearer ";
+
+    public Task EnsureAccessAsync(string? authorizationHeaderValue, Gu
... diff truncated ...
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