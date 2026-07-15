You are the DOCUMENTATION agent in a specialized worker pipeline (separate agents handle
coding, unit tests, e2e tests, database, and review — stay scoped to documentation only,
never touch executable logic).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under documentation:
- Repository: LordIllidan/ai-coding-demo
- PR: #7 AI: [AISDLC-41] Backend: blokada zapisu zgłoszenia kradzieży bez numeru Policji
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/7
- Branch: ai-coding/aisdlc-41-backend-blokada-zapisu-zg-oszenia-kradzie-y-bez-29434646346

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-41-coding-prompt.md b/ai-coding-runs/aisdlc-41-coding-prompt.md
new file mode 100644
index 0000000..84269e3
--- /dev/null
+++ b/ai-coding-runs/aisdlc-41-coding-prompt.md
@@ -0,0 +1,27 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-41 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: Backend: blokada zapisu zgłoszenia kradzieży bez numeru Policji
+
+Task description:
+~~~markdown
+Parent story: AISDLC-30 — Rejestracja zgłoszenia kradzieży z obowiązkowym numerem zgłoszenia Policji
+
+Co robi: dopina walidację po stronie zapisu/API, aby zgłoszenie kradzieży nie mogło zostać zapisane bez numeru zgłoszenia Policji.
+Pliki: endpoint/API zapisu szkody, serwis walidacyjny, mapper błędów do frontu.
+TODO: sprawdzić gdzie trafia payload, dodać twardą walidację po stronie backendu, zwrócić czytelny błąd 4xx i pokryć testami integracyjnymi.
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
diff --git a/ai-coding-runs/pr-7-unittest-prompt.md b/ai-coding-runs/pr-7-unittest-prompt.md
new file mode 100644
index 0000000..4c2c61c
--- /dev/null
+++ b/ai-coding-runs/pr-7-unittest-prompt.md
@@ -0,0 +1,316 @@
+You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
+coding, e2e tests, and review — stay scoped to unit-level test coverage only).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Pull request under test:
+- Repository: LordIllidan/ai-coding-demo
+- PR: #7 AI: [AISDLC-41] Backend: blokada zapisu zgłoszenia kradzieży bez numeru Policji
+- URL: https://github.com/LordIllidan/ai-coding-demo/pull/7
+- Branch: ai-coding/aisdlc-41-backend-blokada-zapisu-zg-oszenia-kradzie-y-bez-29434646346
+
+Diff introduced by this PR:
+~~~diff
+diff --git a/ai-coding-runs/aisdlc-41-coding-prompt.md b/ai-coding-runs/aisdlc-41-coding-prompt.md
+new file mode 100644
+index 0000000..84269e3
+--- /dev/null
++++ b/ai-coding-runs/aisdlc-41-coding-prompt.md
+@@ -0,0 +1,27 @@
++You are the CODING agent in a specialized worker pipeline (separate agents exist for
++unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
++Running locally through a GitHub self-hosted runner (Windows).
++
++Source of truth: Jira issue AISDLC-41 (this task has NO corresponding GitHub issue —
++Jira is the only tracker; do not create or reference a GitHub issue).
++
++Task title: Backend: blokada zapisu zgłoszenia kradzieży bez numeru Policji
++
++Task description:
++~~~markdown
++Parent story: AISDLC-30 — Rejestracja zgłoszenia kradzieży z obowiązkowym numerem zgłoszenia Policji
++
++Co robi: dopina walidację po stronie zapisu/API, aby zgłoszenie kradzieży nie mogło zostać zapisane bez numeru zgłoszenia Policji.
++Pliki: endpoint/API zapisu szkody, serwis walidacyjny, mapper błędów do frontu.
++TODO: sprawdzić gdzie trafia payload, dodać twardą walidację po stronie backendu, zwrócić czytelny błąd 4xx i pokryć testami integracyjnymi.
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
+diff --git a/src/PolicyPlatform.Api/Controllers/ClaimsController.cs b/src/PolicyPlatform.Api/Controllers/ClaimsController.cs
+new file mode 100644
+index 0000000..7ccff39
+--- /dev/null
++++ b/src/PolicyPlatform.Api/Controllers/ClaimsController.cs
+@@ -0,0 +1,35 @@
++using Microsoft.AspNetCore.Mvc;
++using PolicyPlatform.Application.Claims;
++using PolicyPlatform.Domain.Common;
++
++namespace PolicyPlatform.Api.Controllers;
++
++[ApiController]
++[Route("api/theft-claims")]
++public sealed class ClaimsController : ControllerBase
++{
++    private readonly ClaimService _claimService;
++
++    public ClaimsController(ClaimService claimService) => _claimService = claimService;
++
++    [HttpPost]
++    public async Task<ActionResult<TheftClaimDto>> Create(CreateTheftClaimRequest request, CancellationToken ct)
++    {
++        try
++        {
++            var claim = await _claimService.RegisterTheftClaimAsync(request, ct);
++            return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
++        }
++        catch (DomainException ex)
++        {
++            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
++        }
++    }
++
++    [HttpGet("{id:guid}")]
++    public async Task<ActionResult<TheftClaimDto>> GetById(Guid id, CancellationToken ct)
++    {
++        var claim = await _claimService.GetTheftClaimAsync(id, ct);
++        return claim is null ? NotFound() : Ok(claim);
++    }
++}
+diff --git a/src/PolicyPlatform.Application/Abstractions/IClaimRepository.cs b/src/PolicyPlatform.Application/Abstractions/IClaimRepository.cs
+new file mode 100644
+index 0000000..033328a
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Abstractions/IClaimRepository.cs
+@@ -0,0 +1,9 @@
++using PolicyPlatform.Domain.Claims;
++
++namespace PolicyPlatform.Application.Abstractions;
++
++public interface IClaimRepository
++{
++    Task<TheftClaim?> GetByIdAsync(Guid id, CancellationToken ct = default);
++    Task AddAsync(TheftClaim claim, CancellationToken ct = default);
++}
+diff --git a/src/PolicyPlatform.Application/Claims/ClaimDtos.cs b/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
+new file mode 100644
+index 0000000..6f14802
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
+@@ -0,0 +1,26 @@
++using PolicyPlatform.Domain.Claims;
++
++namespace PolicyPlatform.Application.Claims;
++
++public sealed record CreateTheftClaimRequest(
++    Guid PolicyId,
++    DateOnly IncidentDate,
++    string Description,
++    string? PoliceReportNumber);
++
++public sealed record TheftClaimDto(
++    Guid Id,
++    Guid PolicyId,
++    DateOnly IncidentDate,
++    string Description,
++    string PoliceReportNumber,
++    DateTime ReportedAt)
++{
++    public static TheftClaimDto FromDomain(TheftClaim claim) => new(
++        claim.Id,
++        claim.PolicyId,
++        claim.IncidentDate,
++        claim.Description,
++        claim.PoliceReportNumber.Value,
++        claim.ReportedAt);
++}
+diff --git a/src/PolicyPlatform.Application/Claims/ClaimService.cs b/src/PolicyPlatform.Application/Claims/ClaimService.cs
+new file mode 100644
+index 0000000..1193860
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Claims/ClaimService.cs
+@@ -0,0 +1,43 @@
++using PolicyPlatform.Application.Abstractions;
++using PolicyPlatform.Domain.Claims;
++using PolicyPlatform.Domain.Common;
++
++namespace PolicyPlatform.Application.Claims;
++
++/// <summary>Application service (use-case layer). Orchestrates domain objects and
++/// repositories; contains no business rules itself — those live in the Domain.</summary>
++public sealed class ClaimService
++{
++    private readonly IClaimRepository _claims;
++    private readonly IPolicyRepository _policies;
++
++    public ClaimService(IClaimRepository claims, IPolicyRepository policies)
++    {
++        _claims = claims;
++        _policies = policies;
++    }
++
++    public async Task<TheftClaimDto> RegisterTheftClaimAsync(
++        CreateTheftClaimRequest request, CancellationToken ct = default)
++    {
++        _ = await _policies.GetByIdAsync(request.PolicyId, ct)
++            ?? throw new DomainException($"Policy {request.PolicyId} was not found.");
++
++        var claim = TheftClaim.Register(
++            Guid.NewGuid(),
++            request.PolicyId,
++            request.IncidentDate,
++            request.Description,
++            new PoliceReportNumber(request.PoliceReportNumber),
++            DateTime.UtcNow);
++
++        await _claims.AddAsync(claim, ct);
++        return TheftClaimDto.FromDomain(claim);
++    }
++
++    public async Task<TheftClaimDto?> GetTheftClaimAsync(Guid claimId, CancellationToken ct = default)
++    {
++        var claim = await _claims.GetByIdAsync(claimId, ct);
++        return claim is null ? null : TheftClaimDto.FromDomain(claim);
++    }
++}
+diff --git a/src/PolicyPlatform.Domain/Claims/PoliceReportNumber.cs b/src/PolicyPlatform.Domain/Claims/PoliceReportNumber.cs
+new file mode 100644
+index 0000000..867f1f9
+--- /dev/null
++++ b/src/PolicyPlatform.Domain/Claims/PoliceReportNumber.cs
+@@ -0,0 +1,21 @@
++using PolicyPlatform.Domain.Common;
++
++namespace PolicyPlatform.Domain.Claims;
++
++public readonly record struct PoliceReportNumber
++{
++    public string Value { get; }
++
++    public PoliceReportNumber(string? value)
++    {
++        if (string.IsNullOrWhiteSpace(value))
++        {
++            throw new DomainException(
++                "A theft claim cannot be registered without a police report number (numer zgłoszenia Policji).");
++        }
++
++        Value = value.Trim();
++    }
++
++    public override string ToString() => Value;
++}
+diff --git a/src/PolicyPlatform.Domain/Claims/TheftClaim.cs b/src/PolicyPlatform.Domain/Claims/TheftClaim.cs
+new file mode 100644
+index 0000000..e5323f6
+--- /dev/null
++++ b/src/PolicyPlatform.Domain/Claims/TheftClaim.cs
+@@ -0,0 +1,36 @@
++using PolicyPlatform.Domain.Common;
++
++namespace PolicyPlatform.Domain.Claims;
++
++public sealed class TheftClaim : Entity
++{
++    public Guid PolicyId { get; }
++    public DateOnly IncidentDate { get; }
++    public string Description { get; }
++    public PoliceReportNumber PoliceReportNumber { get; }
++    public DateTime ReportedAt { get; }
++
++    private TheftClaim(
++        Guid id, Guid policyId, DateOnly incidentDate, string description,
++        PoliceReportNumber policeReportNumber, DateTime reportedAt)
++        : base(id)
++    {
++        PolicyId = policyId;
++        IncidentDate = incidentDate;
++        Description = description;
++        PoliceReportNumber = policeReportNumber;
++        ReportedAt = reportedAt;
++    }
++
++    public static TheftClaim Register(
++        Guid id, Guid policyId, DateOnly incidentDate, string description,
++        PoliceReportNumber policeReportNumber, DateTime reportedAt)
++    {
++        if (policyId == Guid.Empty)
++        {
++            throw new DomainException("Theft claim must reference a valid policy.");
++        }
++
++        return new TheftClaim(id, policyId, incidentDate, description, policeReportNumber, reportedAt);
++    }
++}
+diff --git a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
+index 1f1c7f7..b5fa109 100644
+--- a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
++++ b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
+@@ -2,6 +2,7 @@
+ using Microsoft.Extensions.Configuration;
+ using Microsoft.Extensions.DependencyInjection;
+ using PolicyPlatform.Application.Abstractions;
++using PolicyPlatform.Application.Claims;
+ using PolicyPlatform.Application.Customers;
+ using PolicyPlatform.Application.Policies;
+ using PolicyPlatform.Infrastructure.Numbering;
+@@ -35,6 +36,11 @@ public static IServiceCollection AddPolicyPlatformInfrastructure(
+         services.AddSingleton<IPolicyNumberGenerator, SequentialPolicyNumberGenerator>();
+         services.AddScoped<PolicyService>();
+         services.AddScoped<CustomerService>();
++
++        // Claims have no durable store yet (EF Core provider is a separate, unscoped
++        // piece of work) — in-memory keeps the theft-claim validation flow runnable now.
++        services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
++        services.AddScoped<ClaimService>();
+         return services;
+     }
+ }
+diff --git a/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimRepository.cs b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimRepository.cs
+new file mode 100644
+index 0000000..a2362c1
+--- /dev/null
++++ b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimRepository.cs
+@@ -0,0 +1,21 @@
++using System.Collections.Concurrent;
++using PolicyPlatform.Application.Abstractions;
++using PolicyPlatform.Domain.Claims;
++
++namespace PolicyPlatform.Infrastructure.Persistence;
++
++/// <summary>Process-lifetime in-memory store. Swap for an EF Core provider once claims
++/// need durable persistence — the Application layer only depends on IClaimRepository.</summary>
++public sealed class InMemoryClaimRepository : IClaimRepository
++{
++    private readonly ConcurrentDictionary<Guid, TheftClaim> _claims = new();
++
++    public Task<TheftClaim?> GetByIdAsync(Guid id, CancellationToken ct = default)
++        => Task.FromResult(_claims.GetValueOrDefault(id));
++
++    public Task AddAsync(TheftClaim claim, CancellationToken ct = default)
++    {
++        _claims[claim.Id] = claim;
++        return Task.CompletedTask;
++    }
++}
+
+~~~
+
+Task:
+1. Identify new or changed functions/methods/classes in the diff that lack unit test coverage.
+2. Write focused unit tests for them, following this repository's existing test conventions
+   (framework, file layout, naming) — inspect existing tests/ before writing new ones.
+3. Do NOT modify production/source code — only add or extend test files. If a change is
+   untestable without a source fix, say so in your output instead of touching source.
+4. Do not merge, push, or create/edit pull requests — the wrapper script handles that.
+5. Do not read or print secrets. Avoid destructive git commands.
+
+Output: short summary of which functions got new test coverage and any gaps you could not cover.
\ No newline at end of file
diff --git a/src/PolicyPlatform.Api/Controllers/ClaimsController.cs b/src/PolicyPlatform.Api/Controllers/ClaimsController.cs
new file mode 100644
index 0000000..7ccff39
--- /dev/null
+++ b/src/PolicyPlatform.Api/Controllers/ClaimsController.cs
@@ -0,0 +1,35 @@
+using Microsoft.AspNetCore.Mvc;
+using PolicyPlatform.Application.Claims;
+using PolicyPlatform.Domain.Common;
+
+namespace PolicyPlatform.Api.Controllers;
+
+[ApiController]
+[Route("api/theft-claims")]
+public sealed class ClaimsController : ControllerBase
+{
+    private readonly ClaimService _claimService;
+
+    public ClaimsController(ClaimService claimService) => _claimService = claimService;
+
+    [HttpPost]
+    public async Task<ActionResult<TheftClaimDto>> Create(CreateTheftClaimRequest request, CancellationToken ct)
+    {
+        try
+        {
+            var claim = await _claimService.RegisterTheftClaimAsync(request, ct);
+            return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
+        }
+        catch (DomainException ex)
+        {
+            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
+        }
+    }
+
+    [HttpGet("{id:guid}")]
+    public async Task<ActionResult<TheftClaimDto>> GetById(Guid id, CancellationToken ct)
+    {
+        var claim = await _claimService.GetTheftClaimAsync(id, ct);
+        return claim is null ? NotFound() : Ok(claim);
+    }
+}
diff --git a/src/PolicyPlatform.Application/Abstractions/IClaimRepository.cs b/src/PolicyPlatform.Application/Abstractions/IClaimRepository.cs
new file mode 100644
index 0000000..033328a
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/IClaimRepository.cs
@@ -0,0 +1,9 @@
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Application.Abstractions;
+
+public interface IClaimRepository
+{
+    Task<TheftClaim?> GetByIdAsync(Guid id, CancellationToken ct = default);
+    Task AddAsync(TheftClaim claim, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Application/Claims/ClaimDtos.cs b/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
new file mode 100644
index 0000000..6f14802
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
@@ -0,0 +1,26 @@
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Application.Claims;
+
+public sealed record CreateTheftClaimRequest(
+    Guid PolicyId,
+    DateOnly IncidentDate,
+    string Description,
+    string? PoliceReportNumber);
+
+public sealed record TheftClaimDto(
+    Guid Id,
+    Guid PolicyId,
+    DateOnly IncidentDate,
+    string Description,
+    string PoliceReportNumber,
+    DateTime ReportedAt)
+{
+    public static TheftClaimDto FromDomain(TheftClaim claim) => new(
+        claim.Id,
+        claim.PolicyId,
+        claim.IncidentDate,
+        claim.Description,
+        claim.PoliceReportNumber.Value,
+        claim.ReportedAt);
+}
diff --git a/src/PolicyPlatform.Application/Claims/ClaimService.cs b/src/PolicyPlatform.Application/Claims/ClaimService.cs
new file mode 100644
index 0000000..1193860
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/ClaimService.cs
@@ -0,0 +1,43 @@
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.Claims;
+using PolicyPlatform.Domain.Common;
+
+namespace PolicyPlatform.Application.Claims;
+
+/// <summary>Application service (use-case layer). Orchestrates domain objects and
+/// repositories; contains no business rules itself — those live in the Domain.</summary>
+public sealed class ClaimService
+{
+    private readonly IClaimRepository _claims;
+    private readonly IPolicyRepository _policies;
+
+    public ClaimService(IClaimRepository claims, IPolicyRepository policies)
+    {
+        _claims = claims;
+        _policies = policies;
+    }
+
+    public async Task<TheftClaimDto> RegisterTheftClaimAsync(
+        CreateTheftClaimRequest request, CancellationToken ct = default)
+    {
+        _ = await _policies.GetByIdAsync(request.PolicyId, ct)
+            ?? throw new DomainException($"Policy {request.PolicyId} was not found.");
+
+        var claim = TheftClaim.Register(
+            Guid.NewGuid(),
+            request.PolicyId,
+            request.IncidentDate,
+            request.Description,
+            new PoliceReportNumber(request.PoliceReportNumber),
+            DateTime.UtcNow);
+
+        await _claims.AddAsync(claim, ct);
+        return TheftClaimDto.FromDomain(claim);
+    }
+
+    public async Task<TheftClaimDto?> GetTheftClaimAsync(Guid claimId, CancellationToken ct = default)
+    {
+        var claim = await _claims.GetByIdAsync(claimId, ct);
+        return claim is null ? null : TheftClaimDto.Fr
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