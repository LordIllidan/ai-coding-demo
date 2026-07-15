You are the DATABASE agent in a specialized worker pipeline (separate agents exist for
coding, unit tests, e2e tests, and review — stay scoped to persistence/schema only, do
not touch Domain business rules or Application use-case logic).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under review:
- Repository: LordIllidan/ai-coding-demo
- PR: #4 AI: [AISDLC-15] [Mobile] Inicjacja zgłoszenia szkody bez przejścia do przeglądarki
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/4
- Branch: ai-coding/aisdlc-15-mobile-inicjacja-zg-oszenia-szkody-bez-przej-cia-29391270738

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-15-coding-prompt.md b/ai-coding-runs/aisdlc-15-coding-prompt.md
new file mode 100644
index 0000000..53acafa
--- /dev/null
+++ b/ai-coding-runs/aisdlc-15-coding-prompt.md
@@ -0,0 +1,25 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-15 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: [Mobile] Inicjacja zgłoszenia szkody bez przejścia do przeglądarki
+
+Task description:
+~~~markdown
+Parent story: AISDLC-7 — Jako klient chcę zgłosić szkodę komunikacyjną z poziomu aplikacji mobilnej bez logowania do przeglądarki, aby rozpocząć proces bez przechodzenia do kanału webowego.
+
+co robi: implementacja startu zgłoszenia szkody w aplikacji mobilnej bez przekierowania do przeglądarki; które pliki: warstwa UI/flow rozpoczęcia zgłoszenia, obsługa nawigacji i integracja z backendem; TODO: sprawdzić istniejący mechanizm otwierania webview, dodać ścieżkę natywną i testy podstawowe.
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
diff --git a/src/PolicyPlatform.Api/Controllers/ClaimsController.cs b/src/PolicyPlatform.Api/Controllers/ClaimsController.cs
new file mode 100644
index 0000000..539753c
--- /dev/null
+++ b/src/PolicyPlatform.Api/Controllers/ClaimsController.cs
@@ -0,0 +1,42 @@
+using Microsoft.AspNetCore.Mvc;
+using PolicyPlatform.Application.Claims;
+using PolicyPlatform.Domain.Common;
+
+namespace PolicyPlatform.Api.Controllers;
+
+/// <summary>Lets a client (notably the mobile app) initiate a damage claim
+/// (zgłoszenie szkody) directly against the backend, so it can start the process
+/// without redirecting to a browser-hosted webview flow.</summary>
+[ApiController]
+[Route("api/claims")]
+public sealed class ClaimsController : ControllerBase
+{
+    private readonly ClaimService _claimService;
+
+    public ClaimsController(ClaimService claimService) => _claimService = claimService;
+
+    [HttpPost]
+    public async Task<ActionResult<ClaimDto>> Initiate(InitiateClaimRequest request, CancellationToken ct)
+    {
+        try
+        {
+            var claim = await _claimService.InitiateClaimAsync(request, ct);
+            return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
+        }
+        catch (DomainException ex)
+        {
+            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
+        }
+    }
+
+    [HttpGet("{id:guid}")]
+    public async Task<ActionResult<ClaimDto>> GetById(Guid id, CancellationToken ct)
+    {
+        var claim = await _claimService.GetClaimAsync(id, ct);
+        return claim is null ? NotFound() : Ok(claim);
+    }
+
+    [HttpGet]
+    public async Task<ActionResult<IReadOnlyList<ClaimDto>>> List(CancellationToken ct)
+        => Ok(await _claimService.ListClaimsAsync(ct));
+}
diff --git a/src/PolicyPlatform.Application/Abstractions/IClaimRepository.cs b/src/PolicyPlatform.Application/Abstractions/IClaimRepository.cs
new file mode 100644
index 0000000..538170e
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/IClaimRepository.cs
@@ -0,0 +1,10 @@
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Application.Abstractions;
+
+public interface IClaimRepository
+{
+    Task<Claim?> GetByIdAsync(Guid id, CancellationToken ct = default);
+    Task<IReadOnlyList<Claim>> ListAsync(CancellationToken ct = default);
+    Task AddAsync(Claim claim, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Application/Claims/ClaimDtos.cs b/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
new file mode 100644
index 0000000..18dd23c
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
@@ -0,0 +1,29 @@
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Application.Claims;
+
+public sealed record InitiateClaimRequest(
+    Guid PolicyId,
+    Guid CustomerId,
+    string Channel,
+    DateOnly IncidentDate,
+    string? Description);
+
+public sealed record ClaimDto(
+    Guid Id,
+    Guid PolicyId,
+    Guid CustomerId,
+    string Channel,
+    DateOnly IncidentDate,
+    string? Description,
+    DateTime CreatedAtUtc)
+{
+    public static ClaimDto FromDomain(Claim claim) => new(
+        claim.Id,
+        claim.PolicyId,
+        claim.CustomerId,
+        claim.Channel.ToString(),
+        claim.IncidentDate,
+        claim.Description,
+        claim.CreatedAtUtc);
+}
diff --git a/src/PolicyPlatform.Application/Claims/ClaimService.cs b/src/PolicyPlatform.Application/Claims/ClaimService.cs
new file mode 100644
index 0000000..a6f611f
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/ClaimService.cs
@@ -0,0 +1,67 @@
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.Claims;
+using PolicyPlatform.Domain.Common;
+using PolicyPlatform.Domain.Policies;
+
+namespace PolicyPlatform.Application.Claims;
+
+/// <summary>Application service (use-case layer) for initiating a damage claim
+/// (zgłoszenie szkody) directly from a channel such as the mobile app, without
+/// requiring a browser/webview handoff. Contains no business rules itself —
+/// those live in the Domain.</summary>
+public sealed class ClaimService
+{
+    private readonly IClaimRepository _claims;
+    private readonly IPolicyRepository _policies;
+    private readonly ICustomerRepository _customers;
+
+    public ClaimService(IClaimRepository claims, IPolicyRepository policies, ICustomerRepository customers)
+    {
+        _claims = claims;
+        _policies = policies;
+        _customers = customers;
+    }
+
+    public async Task<ClaimDto> InitiateClaimAsync(InitiateClaimRequest request, CancellationToken ct = default)
+    {
+        var customer = await _customers.GetByIdAsync(request.CustomerId, ct)
+            ?? throw new DomainException($"Customer {request.CustomerId} was not found.");
+
+        var policy = await _policies.GetByIdAsync(request.PolicyId, ct)
+            ?? throw new DomainException($"Policy {request.PolicyId} was not found.");
+
+        if (policy.CustomerId != customer.Id)
+        {
+            throw new DomainException("Policy does not belong to the given customer.");
+        }
+
+        if (policy.Status != PolicyStatus.Active)
+        {
+            throw new DomainException($"A claim can only be reported against an active policy (current status: {policy.Status}).");
+        }
+
+        if (!Enum.TryParse<ClaimChannel>(request.Channel, ignoreCase: true, out var channel))
+        {
+            throw new DomainException($"'{request.Channel}' is not a recognized claim channel.");
+        }
+
+        var today = DateOnly.FromDateTime(DateTime.UtcNow);
+        var claim = Claim.Initiate(
+            Guid.NewGuid(), policy.Id, customer.Id, channel, request.IncidentDate, request.Description, today, DateTime.UtcNow);
+
+        await _claims.AddAsync(claim, ct);
+        return ClaimDto.FromDomain(claim);
+    }
+
+    public async Task<ClaimDto?> GetClaimAsync(Guid claimId, CancellationToken ct = default)
+    {
+        var claim = await _claims.GetByIdAsync(claimId, ct);
+        return claim is null ? null : ClaimDto.FromDomain(claim);
+    }
+
+    public async Task<IReadOnlyList<ClaimDto>> ListClaimsAsync(CancellationToken ct = default)
+    {
+        var claims = await _claims.ListAsync(ct);
+        return claims.Select(ClaimDto.FromDomain).ToList();
+    }
+}
diff --git a/src/PolicyPlatform.Domain/Claims/Claim.cs b/src/PolicyPlatform.Domain/Claims/Claim.cs
new file mode 100644
index 0000000..98d4916
--- /dev/null
+++ b/src/PolicyPlatform.Domain/Claims/Claim.cs
@@ -0,0 +1,48 @@
+using PolicyPlatform.Domain.Common;
+
+namespace PolicyPlatform.Domain.Claims;
+
+public sealed class Claim : Entity
+{
+    public Guid PolicyId { get; }
+    public Guid CustomerId { get; }
+    public ClaimChannel Channel { get; }
+    public DateOnly IncidentDate { get; }
+    public string? Description { get; }
+    public DateTime CreatedAtUtc { get; }
+
+    private Claim(
+        Guid id, Guid policyId, Guid customerId, ClaimChannel channel,
+        DateOnly incidentDate, string? description, DateTime createdAtUtc)
+        : base(id)
+    {
+        PolicyId = policyId;
+        CustomerId = customerId;
+        Channel = channel;
+        IncidentDate = incidentDate;
+        Description = description;
+        CreatedAtUtc = createdAtUtc;
+    }
+
+    public static Claim Initiate(
+        Guid id, Guid policyId, Guid customerId, ClaimChannel channel,
+        DateOnly incidentDate, string? description, DateOnly today, DateTime createdAtUtc)
+    {
+        if (policyId == Guid.Empty)
+        {
+            throw new DomainException("Claim must reference a valid policy.");
+        }
+
+        if (customerId == Guid.Empty)
+        {
+            throw new DomainException("Claim must reference a valid customer.");
+        }
+
+        if (incidentDate > today)
+        {
+            throw new DomainException("Incident date cannot be in the future.");
+        }
+
+        return new Claim(id, policyId, customerId, channel, incidentDate, description?.Trim(), createdAtUtc);
+    }
+}
diff --git a/src/PolicyPlatform.Domain/Claims/ClaimChannel.cs b/src/PolicyPlatform.Domain/Claims/ClaimChannel.cs
new file mode 100644
index 0000000..5b551e1
--- /dev/null
+++ b/src/PolicyPlatform.Domain/Claims/ClaimChannel.cs
@@ -0,0 +1,9 @@
+namespace PolicyPlatform.Domain.Claims;
+
+/// <summary>Channel a claim was initiated through. MobileNative marks claims started
+/// directly in the mobile app's own UI, without redirecting to a browser/webview.</summary>
+public enum ClaimChannel
+{
+    MobileNative,
+    Web,
+}
diff --git a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
index e1ebcd1..a790284 100644
--- a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
+++ b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
@@ -1,5 +1,6 @@
 using Microsoft.Extensions.DependencyInjection;
 using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Application.Claims;
 using PolicyPlatform.Application.Customers;
 using PolicyPlatform.Application.Policies;
 using PolicyPlatform.Infrastructure.Numbering;
@@ -13,9 +14,11 @@ public static IServiceCollection AddPolicyPlatformInfrastructure(this IServiceCo
     {
         services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
         services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
+        services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
         services.AddSingleton<IPolicyNumberGenerator, SequentialPolicyNumberGenerator>();
         services.AddScoped<PolicyService>();
         services.AddScoped<CustomerService>();
+        services.AddScoped<ClaimService>();
         return services;
     }
 }
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimRepository.cs b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimRepository.cs
new file mode 100644
index 0000000..5c3413c
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimRepository.cs
@@ -0,0 +1,24 @@
+using System.Collections.Concurrent;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Infrastructure.Persistence;
+
+/// <summary>Process-lifetime in-memory store. Swap for an EF Core provider once a real
+/// database is provisioned — the Application layer only depends on IClaimRepository.</summary>
+public sealed class InMemoryClaimRepository : IClaimRepository
+{
+    private readonly ConcurrentDictionary<Guid, Claim> _claims = new();
+
+    public Task<Claim?> GetByIdAsync(Guid id, CancellationToken ct = default)
+        => Task.FromResult(_claims.GetValueOrDefault(id));
+
+    public Task<IReadOnlyList<Claim>> ListAsync(CancellationToken ct = default)
+        => Task.FromResult<IReadOnlyList<Claim>>(_claims.Values.ToList());
+
+    public Task AddAsync(Claim claim, CancellationToken ct = default)
+    {
+        _claims[claim.Id] = claim;
+        return Task.CompletedTask;
+    }
+}

~~~

Context: PolicyPlatform.Infrastructure currently uses in-memory repositories
(InMemoryPolicyRepository, InMemoryCustomerRepository — see src/PolicyPlatform.Infrastructure/Persistence/)
as explicit placeholders for a real database. Domain/Application must stay completely
unaware of the storage technology — only PolicyPlatform.Infrastructure may reference EF Core.

Task:
1. If this diff adds new Domain entities/value objects that need persisting and no EF Core
   DbContext exists yet in PolicyPlatform.Infrastructure, set it up: add the
   Microsoft.EntityFrameworkCore.Sqlite package reference, a PolicyPlatformDbContext,
   entity configurations (IEntityTypeConfiguration<T> per aggregate — do not put mapping
   attributes on Domain classes), and EF Core repository implementations that satisfy the
   SAME interfaces the in-memory repositories already implement (IPolicyRepository,
   ICustomerRepository in PolicyPlatform.Application/Abstractions) — do not change those
   interfaces.
2. If the DbContext already exists, add/update entity configurations and a migration
   (dotnet ef migrations add) for whatever new persisted state this PR's diff requires.
3. Keep the in-memory repositories in place (do not delete them) unless explicitly asked —
   wire the EF Core ones in behind a feature flag or leave both registered with EF Core as
   the new default in DependencyInjection.cs, whichever keeps the diff smallest and reversible.
4. Do NOT modify Domain or Application layer business logic — only Infrastructure (and
   DependencyInjection.cs wiring).
5. Do not merge, push, or create/edit pull requests — the wrapper script handles that.
6. Do not read or print secrets. Avoid destructive git commands.

Output: short summary of schema/migration changes made, or "no persistence changes needed"
if this PR's diff doesn't require any.