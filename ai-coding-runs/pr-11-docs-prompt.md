You are the DOCUMENTATION agent in a specialized worker pipeline (separate agents handle
coding, unit tests, e2e tests, database, and review — stay scoped to documentation only,
never touch executable logic).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under documentation:
- Repository: LordIllidan/ai-coding-demo
- PR: #11 AI: [AISDLC-51] Implementacja endpointu POST /api/theft-claims i walidacji numeru zgłoszenia Policji
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/11
- Branch: ai-coding/aisdlc-51-implementacja-endpointu-post-api-theft-claims-i-29476273812

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-51-coding-prompt.md b/ai-coding-runs/aisdlc-51-coding-prompt.md
new file mode 100644
index 0000000..6b07763
--- /dev/null
+++ b/ai-coding-runs/aisdlc-51-coding-prompt.md
@@ -0,0 +1,33 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-51 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: Implementacja endpointu POST /api/theft-claims i walidacji numeru zgłoszenia Policji
+
+Task description:
+~~~markdown
+Parent story: AISDLC-31 — Walidacja braku lub niepoprawności numeru zgłoszenia Policji
+
+Implementacja backendowego endpointu POST /api/theft-claims wraz z walidacją policeReportNumber, normalizacją do UPPERCASE i zwracaniem 422 dla błędów walidacji.
+KONTRAKT:
+Zakres: jedno API do zapisu zgłoszenia kradzieży pojazdu z walidacją numeru zgłoszenia Policji.
+Endpoint: POST /api/theft-claims.
+Request JSON: policyId:string (UUID, required) — identyfikator polisy; NIE customerId. policeReportNumber:string (required, trim, 3-50 znaków, dozwolone litery/cyfry/spacja/"/"/"-"); przed zapisem normalizować do UPPERCASE.
+Sukces: 201 Created, body: { claimId:string(UUID), policyId:string(UUID), policeReportNumber:string, status:'ACCEPTED', nextStepAllowed:true }.
+Błąd walidacji: 422 Unprocessable Entity, body: { code:'VALIDATION_ERROR', fieldErrors:[{ field:'policeReportNumber', code:'POLICE_REPORT_NUMBER_REQUIRED' | 'POLICE_REPORT_NUMBER_INVALID_FORMAT', message:'Numer zgłoszenia Policji jest wymagany i musi być poprawny.' }] }.
+Reguła biznesowa: jeśli policeReportNumber jest pusty, po trim ma 0 znaków albo nie pasuje do regexu ^[A-Z0-9][A-Z0-9/ -]{2,49}$, backend odrzuca zapis i nie przechodzi do kolejnego kroku procesu.
+DB: tabela theft_claim; kolumny min.: id UUID PK, policy_id UUID NOT NULL, police_report_number VARCHAR(50) NOT NULL, status VARCHAR(20) NOT NULL, created_at TIMESTAMP, updated_at TIMESTAMP; zapisujemy wartość już po normalizacji.
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
index 7ccff39..3c2f491 100644
--- a/src/PolicyPlatform.Api/Controllers/ClaimsController.cs
+++ b/src/PolicyPlatform.Api/Controllers/ClaimsController.cs
@@ -1,5 +1,6 @@
 using Microsoft.AspNetCore.Mvc;
 using PolicyPlatform.Application.Claims;
+using PolicyPlatform.Domain.Claims;
 using PolicyPlatform.Domain.Common;
 
 namespace PolicyPlatform.Api.Controllers;
@@ -13,12 +14,19 @@ public sealed class ClaimsController : ControllerBase
     public ClaimsController(ClaimService claimService) => _claimService = claimService;
 
     [HttpPost]
-    public async Task<ActionResult<TheftClaimDto>> Create(CreateTheftClaimRequest request, CancellationToken ct)
+    public async Task<ActionResult<TheftClaimCreatedResponse>> Create(CreateTheftClaimRequest request, CancellationToken ct)
     {
         try
         {
             var claim = await _claimService.RegisterTheftClaimAsync(request, ct);
-            return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
+            return CreatedAtAction(nameof(GetById), new { id = claim.ClaimId }, claim);
+        }
+        catch (PoliceReportNumberValidationException ex)
+        {
+            var error = new ValidationErrorResponse(
+                "VALIDATION_ERROR",
+                [new FieldError("policeReportNumber", ex.Code, PoliceReportNumberValidationException.ValidationMessage)]);
+            return UnprocessableEntity(error);
         }
         catch (DomainException ex)
         {
diff --git a/src/PolicyPlatform.Application/Claims/ClaimDtos.cs b/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
index 6f14802..5d9bcf7 100644
--- a/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
+++ b/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
@@ -2,25 +2,42 @@
 
 namespace PolicyPlatform.Application.Claims;
 
-public sealed record CreateTheftClaimRequest(
+public sealed record CreateTheftClaimRequest(Guid PolicyId, string? PoliceReportNumber);
+
+/// <summary>201 response for POST /api/theft-claims (AISDLC-51 contract).</summary>
+public sealed record TheftClaimCreatedResponse(
+    Guid ClaimId,
     Guid PolicyId,
-    DateOnly IncidentDate,
-    string Description,
-    string? PoliceReportNumber);
+    string PoliceReportNumber,
+    string Status,
+    bool NextStepAllowed)
+{
+    public static TheftClaimCreatedResponse FromDomain(TheftClaim claim) => new(
+        claim.Id,
+        claim.PolicyId,
+        claim.PoliceReportNumber.Value,
+        claim.Status,
+        NextStepAllowed: true);
+}
 
 public sealed record TheftClaimDto(
     Guid Id,
     Guid PolicyId,
-    DateOnly IncidentDate,
-    string Description,
     string PoliceReportNumber,
-    DateTime ReportedAt)
+    string Status,
+    DateTime CreatedAt,
+    DateTime UpdatedAt)
 {
     public static TheftClaimDto FromDomain(TheftClaim claim) => new(
         claim.Id,
         claim.PolicyId,
-        claim.IncidentDate,
-        claim.Description,
         claim.PoliceReportNumber.Value,
-        claim.ReportedAt);
+        claim.Status,
+        claim.CreatedAt,
+        claim.UpdatedAt);
 }
+
+/// <summary>422 response body for POST /api/theft-claims validation failures (AISDLC-51 contract).</summary>
+public sealed record FieldError(string Field, string Code, string Message);
+
+public sealed record ValidationErrorResponse(string Code, IReadOnlyList<FieldError> FieldErrors);
diff --git a/src/PolicyPlatform.Application/Claims/ClaimService.cs b/src/PolicyPlatform.Application/Claims/ClaimService.cs
index 1193860..22d57db 100644
--- a/src/PolicyPlatform.Application/Claims/ClaimService.cs
+++ b/src/PolicyPlatform.Application/Claims/ClaimService.cs
@@ -17,22 +17,20 @@ public ClaimService(IClaimRepository claims, IPolicyRepository policies)
         _policies = policies;
     }
 
-    public async Task<TheftClaimDto> RegisterTheftClaimAsync(
+    public async Task<TheftClaimCreatedResponse> RegisterTheftClaimAsync(
         CreateTheftClaimRequest request, CancellationToken ct = default)
     {
+        // Validate the police report number before touching the DB — an invalid
+        // format is a client error (422) regardless of whether the policy exists.
+        var policeReportNumber = new PoliceReportNumber(request.PoliceReportNumber);
+
         _ = await _policies.GetByIdAsync(request.PolicyId, ct)
             ?? throw new DomainException($"Policy {request.PolicyId} was not found.");
 
-        var claim = TheftClaim.Register(
-            Guid.NewGuid(),
-            request.PolicyId,
-            request.IncidentDate,
-            request.Description,
-            new PoliceReportNumber(request.PoliceReportNumber),
-            DateTime.UtcNow);
+        var claim = TheftClaim.Register(Guid.NewGuid(), request.PolicyId, policeReportNumber, DateTime.UtcNow);
 
         await _claims.AddAsync(claim, ct);
-        return TheftClaimDto.FromDomain(claim);
+        return TheftClaimCreatedResponse.FromDomain(claim);
     }
 
     public async Task<TheftClaimDto?> GetTheftClaimAsync(Guid claimId, CancellationToken ct = default)
diff --git a/src/PolicyPlatform.Domain/Claims/PoliceReportNumber.cs b/src/PolicyPlatform.Domain/Claims/PoliceReportNumber.cs
index 867f1f9..de46c87 100644
--- a/src/PolicyPlatform.Domain/Claims/PoliceReportNumber.cs
+++ b/src/PolicyPlatform.Domain/Claims/PoliceReportNumber.cs
@@ -1,21 +1,34 @@
-using PolicyPlatform.Domain.Common;
+using System.Text.RegularExpressions;
 
 namespace PolicyPlatform.Domain.Claims;
 
-public readonly record struct PoliceReportNumber
+public readonly partial record struct PoliceReportNumber
 {
+    public const string RequiredCode = "POLICE_REPORT_NUMBER_REQUIRED";
+    public const string InvalidFormatCode = "POLICE_REPORT_NUMBER_INVALID_FORMAT";
+
     public string Value { get; }
 
     public PoliceReportNumber(string? value)
     {
-        if (string.IsNullOrWhiteSpace(value))
+        var trimmed = (value ?? string.Empty).Trim();
+        if (trimmed.Length == 0)
         {
-            throw new DomainException(
-                "A theft claim cannot be registered without a police report number (numer zgłoszenia Policji).");
+            throw new PoliceReportNumberValidationException(RequiredCode);
         }
 
-        Value = value.Trim();
+        var normalized = trimmed.ToUpperInvariant();
+        if (!FormatPattern().IsMatch(normalized))
+        {
+            throw new PoliceReportNumberValidationException(InvalidFormatCode);
+        }
+
+        Value = normalized;
     }
 
     public override string ToString() => Value;
+
+    // 3-50 chars, letters/digits/space/"/"/"-" only, normalized to UPPERCASE before this runs.
+    [GeneratedRegex("^[A-Z0-9][A-Z0-9/ -]{2,49}$")]
+    private static partial Regex FormatPattern();
 }
diff --git a/src/PolicyPlatform.Domain/Claims/PoliceReportNumberValidationException.cs b/src/PolicyPlatform.Domain/Claims/PoliceReportNumberValidationException.cs
new file mode 100644
index 0000000..f75ec74
--- /dev/null
+++ b/src/PolicyPlatform.Domain/Claims/PoliceReportNumberValidationException.cs
@@ -0,0 +1,15 @@
+using PolicyPlatform.Domain.Common;
+
+namespace PolicyPlatform.Domain.Claims;
+
+/// <summary>Thrown by <see cref="PoliceReportNumber"/> when the raw input fails the
+/// required/format check. Carries the machine-readable code the API maps to a
+/// VALIDATION_ERROR field error (see AISDLC-51 contract).</summary>
+public sealed class PoliceReportNumberValidationException : DomainException
+{
+    public const string ValidationMessage = "Numer zgłoszenia Policji jest wymagany i musi być poprawny.";
+
+    public string Code { get; }
+
+    public PoliceReportNumberValidationException(string code) : base(ValidationMessage) => Code = code;
+}
diff --git a/src/PolicyPlatform.Domain/Claims/TheftClaim.cs b/src/PolicyPlatform.Domain/Claims/TheftClaim.cs
index e5323f6..9559f05 100644
--- a/src/PolicyPlatform.Domain/Claims/TheftClaim.cs
+++ b/src/PolicyPlatform.Domain/Claims/TheftClaim.cs
@@ -4,33 +4,34 @@ namespace PolicyPlatform.Domain.Claims;
 
 public sealed class TheftClaim : Entity
 {
+    public const string StatusAccepted = "ACCEPTED";
+
     public Guid PolicyId { get; }
-    public DateOnly IncidentDate { get; }
-    public string Description { get; }
     public PoliceReportNumber PoliceReportNumber { get; }
-    public DateTime ReportedAt { get; }
+    public string Status { get; }
+    public DateTime CreatedAt { get; }
+    public DateTime UpdatedAt { get; }
 
     private TheftClaim(
-        Guid id, Guid policyId, DateOnly incidentDate, string description,
-        PoliceReportNumber policeReportNumber, DateTime reportedAt)
+        Guid id, Guid policyId, PoliceReportNumber policeReportNumber, string status,
+        DateTime createdAt, DateTime updatedAt)
         : base(id)
     {
         PolicyId = policyId;
-        IncidentDate = incidentDate;
-        Description = description;
         PoliceReportNumber = policeReportNumber;
-        ReportedAt = reportedAt;
+        Status = status;
+        CreatedAt = createdAt;
+        UpdatedAt = updatedAt;
     }
 
     public static TheftClaim Register(
-        Guid id, Guid policyId, DateOnly incidentDate, string description,
-        PoliceReportNumber policeReportNumber, DateTime reportedAt)
+        Guid id, Guid policyId, PoliceReportNumber policeReportNumber, DateTime registeredAt)
     {
         if (policyId == Guid.Empty)
         {
             throw new DomainException("Theft claim must reference a valid policy.");
         }
 
-        return new TheftClaim(id, policyId, incidentDate, description, policeReportNumber, reportedAt);
+        return new TheftClaim(id, policyId, policeReportNumber, StatusAccepted, registeredAt, registeredAt);
     }
 }
diff --git a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
index b5fa109..478dc2c 100644
--- a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
+++ b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
@@ -25,21 +25,19 @@ public static IServiceCollection AddPolicyPlatformInfrastructure(
         {
             services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
             services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
+            services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
         }
         else
         {
             services.AddDbContext<PolicyPlatformDbContext>(options => options.UseSqlServer(connectionString));
             services.AddScoped<IPolicyRepository, EfPolicyRepository>();
             services.AddScoped<ICustomerRepository, EfCustomerRepository>();
+            services.AddScoped<IClaimRepository, EfClaimRepository>();
         }
 
         services.AddSingleton<IPolicyNumberGenerator, SequentialPolicyNumberGenerator>();
         services.AddScoped<PolicyService>();
         services.AddScoped<CustomerService>();
-
-        // Claims have no durable store yet (EF Core provider is a separate, unscoped
-        // piece of work) — in-memory keeps the theft-claim validation flow runnable now.
-        services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
         services.AddScoped<ClaimService>();
         return services;
     }
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/Configurations/TheftClaimConfiguration.cs b/src/PolicyPlatform.Infrastructure/Persistence/Configurations/TheftClaimConfiguration.cs
new file mode 100644
index 0000000..f6384ce
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/Configurations/TheftClaimConfiguration.cs
@@ -0,0 +1,28 @@
+using Microsoft.EntityFrameworkCore;
+using Microsoft.EntityFrameworkCore.Metadata.Builders;
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Infrastructure.Persistence.Configurations;
+
+public sealed class TheftClaimConfiguration : IEntityTypeConfiguration<TheftClaim>
+{
+    public void Configure(EntityTypeBuilder<TheftClaim> builder)
+    {
+        builder.ToTable("theft_claim");
+
+        builder.HasKey(c => c.Id);
+        builder.Property(c => c.Id).HasColumnName("id");
+
+        builder.Property(c => c.PolicyId).HasColumnName("policy_id").IsRequired();
+
+        builder.Property(c => c.PoliceReportNumber)
+            .HasConversion(number => number.Value, value => new PoliceReportNumber(value))
+            .HasColumnName("police_report_number")
+            .HasMaxLength(50)
+            .IsRequired();
+
+        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
+        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
+        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").IsRequired();
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/EfClaimRepository.cs b/src/PolicyPlatform.Infrastructure/Persistence/EfClaimRepository.cs
new file mode 100644
index 0000000..4d91a55
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/EfClaimRepository.cs
@@ -0,0 +1,21 @@
+using Microsoft.EntityFrameworkCore;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Infrastructure.Persistence;
+
+public sealed class EfClaimRepository : IClaimRepository
+{
+    private readonly PolicyPlatformDbContext _db;
+
+    public EfClaimRepository(PolicyPlatformDbContext db) => _db = db;
+
+    public async Task<TheftClaim?> GetByIdAsync(Guid id, CancellationToken ct = default)
+        => await _db.TheftClaims.FirstOrDefaultAsync(c => c.Id == id, ct);
+
+    public async Task AddAsync(TheftClaim claim, CancellationToken ct = default)
+    {
+        await _db.TheftClaims.AddAsync(claim, ct);
+        await _db.SaveChangesAsync(ct);
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260716120000_AddTheftClaims.Designer.cs b/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260716120000_AddTheftClaims.Designer.cs
new file mode 100644
index 0000000..f791971
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260716120000_AddTheftClaims.Designer.cs
@@ -0,0 +1,208 @@
+// <auto-generated />
+using System;
+using Microsoft.EntityFrameworkCore;
+using Microsoft.EntityFrameworkCore.Infrastructure;
+using Microsoft.EntityFrameworkCore.Metadata;
+using Microsoft.EntityFrameworkCore.Migrations;
+using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
+using PolicyPlatform.Infrastructure.Persistence;
+
+#nullable disable
+
+namespace PolicyPlatform.Infrastructure.Persistence.Migrations
+{
+    [DbContext(typeof(PolicyPlatformDbContext))]
+    [Migration("20260716120000_AddTheftClaims")]
+    partial class AddTheftClaims
+    {
+        /// <inheritdoc />
+        protected override void BuildTargetModel(ModelBuilder modelBuilder)
+        {
+#pragma warning disable 612, 618
+            modelBuilder
+                .HasAnnotation("ProductVersion", "9.0.0")
+                .HasAnnotation("Relational:MaxIdentifierLength", 128);
+
+            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);
+
+            modelBuilder.Entity("PolicyPlatform.Domain.Claims.TheftClaim", b =>
+                {
+                    b.Property<Guid>("Id")
+                        .ValueGeneratedOnAdd()
+                        .HasColumnType("uniqueidentifier")
+                        .HasColumnName("id");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("datetime2")
+                        .HasColumnName("created_at");
+
+                    b.Property<string>("PoliceReportNumber")
+                        .IsRequired()
+                        .HasMaxLength(50)
+                        .HasColumnType("nvarchar(50)")
+                        .HasColumnName("police_report_number");
+
+                    b.Property<Guid>("PolicyId")
+                        .HasColumnType("uniqueidentifier")
+                        .HasColumnName("policy_id");
+
+                    b.Property<string>("Status")
+                        .IsRequired()
+                        .HasMaxLength(20)
+                        .HasColumnType("nvarchar(20)")
+                        .HasColumnName("status");
+
+                    b.Property<DateTime>("UpdatedAt")
+                        .HasColumnType("datetime2")
+                        .HasColumnName("updated_at");
+
+                    b.HasKey("Id");
+
+                    b.ToTable("theft_claim", (string)null);
+                });
+
+            mo
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