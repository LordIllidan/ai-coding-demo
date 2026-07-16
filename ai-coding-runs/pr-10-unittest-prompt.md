You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: LordIllidan/ai-coding-demo
- PR: #10 AI: [AISDLC-51] Implementacja endpointu POST /api/theft-claims i walidacji numeru zgłoszenia Policji
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/10
- Branch: ai-coding/aisdlc-51-implementacja-endpointu-post-api-theft-claims-i-29476273792

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
index 7ccff39..132f1b0 100644
--- a/src/PolicyPlatform.Api/Controllers/ClaimsController.cs
+++ b/src/PolicyPlatform.Api/Controllers/ClaimsController.cs
@@ -18,7 +18,11 @@ public async Task<ActionResult<TheftClaimDto>> Create(CreateTheftClaimRequest re
         try
         {
             var claim = await _claimService.RegisterTheftClaimAsync(request, ct);
-            return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
+            return CreatedAtAction(nameof(GetById), new { id = claim.ClaimId }, claim);
+        }
+        catch (TheftClaimValidationException ex)
+        {
+            return UnprocessableEntity(new ValidationErrorResponse("VALIDATION_ERROR", ex.FieldErrors));
         }
         catch (DomainException ex)
         {
diff --git a/src/PolicyPlatform.Application/Claims/ClaimDtos.cs b/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
index 6f14802..0e30ae3 100644
--- a/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
+++ b/src/PolicyPlatform.Application/Claims/ClaimDtos.cs
@@ -2,25 +2,35 @@
 
 namespace PolicyPlatform.Application.Claims;
 
-public sealed record CreateTheftClaimRequest(
-    Guid PolicyId,
-    DateOnly IncidentDate,
-    string Description,
-    string? PoliceReportNumber);
+public sealed record CreateTheftClaimRequest(Guid PolicyId, string? PoliceReportNumber);
 
 public sealed record TheftClaimDto(
-    Guid Id,
+    Guid ClaimId,
     Guid PolicyId,
-    DateOnly IncidentDate,
-    string Description,
     string PoliceReportNumber,
-    DateTime ReportedAt)
+    string Status,
+    bool NextStepAllowed)
 {
     public static TheftClaimDto FromDomain(TheftClaim claim) => new(
         claim.Id,
         claim.PolicyId,
-        claim.IncidentDate,
-        claim.Description,
         claim.PoliceReportNumber.Value,
-        claim.ReportedAt);
+        "ACCEPTED",
+        NextStepAllowed: true);
+}
+
+public sealed record FieldError(string Field, string Code, string Message);
+
+public sealed record ValidationErrorResponse(string Code, IReadOnlyList<FieldError> FieldErrors);
+
+/// <summary>Thrown when a theft claim request fails field-level validation (AISDLC-51
+/// contract) — mapped to a 422 response by the controller, distinct from
+/// <see cref="PolicyPlatform.Domain.Common.DomainException"/> which maps to 400.</summary>
+public sealed class TheftClaimValidationException : Exception
+{
+    public IReadOnlyList<FieldError> FieldErrors { get; }
+
+    public TheftClaimValidationException(IReadOnlyList<FieldError> fieldErrors)
+        : base("Theft claim validation failed.")
+        => FieldErrors = fieldErrors;
 }
diff --git a/src/PolicyPlatform.Application/Claims/ClaimService.cs b/src/PolicyPlatform.Application/Claims/ClaimService.cs
index 1193860..1ec28bd 100644
--- a/src/PolicyPlatform.Application/Claims/ClaimService.cs
+++ b/src/PolicyPlatform.Application/Claims/ClaimService.cs
@@ -20,16 +20,20 @@ public ClaimService(IClaimRepository claims, IPolicyRepository policies)
     public async Task<TheftClaimDto> RegisterTheftClaimAsync(
         CreateTheftClaimRequest request, CancellationToken ct = default)
     {
+        if (!PoliceReportNumber.TryCreate(request.PoliceReportNumber, out var policeReportNumber, out var error))
+        {
+            var code = error == PoliceReportNumberError.Required
+                ? "POLICE_REPORT_NUMBER_REQUIRED"
+                : "POLICE_REPORT_NUMBER_INVALID_FORMAT";
+            throw new TheftClaimValidationException([
+                new FieldError("policeReportNumber", code, "Numer zgłoszenia Policji jest wymagany i musi być poprawny.")
+            ]);
+        }
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
         return TheftClaimDto.FromDomain(claim);
diff --git a/src/PolicyPlatform.Domain/Claims/PoliceReportNumber.cs b/src/PolicyPlatform.Domain/Claims/PoliceReportNumber.cs
index 867f1f9..846a204 100644
--- a/src/PolicyPlatform.Domain/Claims/PoliceReportNumber.cs
+++ b/src/PolicyPlatform.Domain/Claims/PoliceReportNumber.cs
@@ -1,20 +1,56 @@
-using PolicyPlatform.Domain.Common;
+using System.Text.RegularExpressions;
 
 namespace PolicyPlatform.Domain.Claims;
 
+public enum PoliceReportNumberError
+{
+    Required,
+    InvalidFormat,
+}
+
 public readonly record struct PoliceReportNumber
 {
+    /// <summary>Trimmed, upper-cased value: 3-50 chars, letters/digits/space/"/"/"-",
+    /// must start with a letter or digit (AISDLC-51 contract).</summary>
+    private static readonly Regex Pattern = new("^[A-Z0-9][A-Z0-9/ -]{2,49}$", RegexOptions.Compiled);
+
     public string Value { get; }
 
-    public PoliceReportNumber(string? value)
+    private PoliceReportNumber(string value) => Value = value;
+
+    public static bool TryCreate(string? raw, out PoliceReportNumber number, out PoliceReportNumberError? error)
+    {
+        var trimmed = (raw ?? string.Empty).Trim();
+        if (trimmed.Length == 0)
+        {
+            number = default;
+            error = PoliceReportNumberError.Required;
+            return false;
+        }
+
+        var normalized = trimmed.ToUpperInvariant();
+        if (!Pattern.IsMatch(normalized))
+        {
+            number = default;
+            error = PoliceReportNumberError.InvalidFormat;
+            return false;
+        }
+
+        number = new PoliceReportNumber(normalized);
+        error = null;
+        return true;
+    }
+
+    /// <summary>Throws for values already known to be valid (e.g. round-tripping from the
+    /// database). Use <see cref="TryCreate"/> at API/validation boundaries instead.</summary>
+    public static PoliceReportNumber Create(string? raw)
     {
-        if (string.IsNullOrWhiteSpace(value))
+        if (!TryCreate(raw, out var number, out var error))
         {
-            throw new DomainException(
-                "A theft claim cannot be registered without a police report number (numer zgłoszenia Policji).");
+            throw new ArgumentException($"Invalid police report number ({error}).", nameof(raw));
         }
 
-        Value = value.Trim();
+        return number;
     }
 
     public override string ToString() => Value;
diff --git a/src/PolicyPlatform.Domain/Claims/TheftClaim.cs b/src/PolicyPlatform.Domain/Claims/TheftClaim.cs
index e5323f6..f8252ad 100644
--- a/src/PolicyPlatform.Domain/Claims/TheftClaim.cs
+++ b/src/PolicyPlatform.Domain/Claims/TheftClaim.cs
@@ -2,35 +2,38 @@
 
 namespace PolicyPlatform.Domain.Claims;
 
+public enum TheftClaimStatus
+{
+    Accepted,
+}
+
 public sealed class TheftClaim : Entity
 {
     public Guid PolicyId { get; }
-    public DateOnly IncidentDate { get; }
-    public string Description { get; }
     public PoliceReportNumber PoliceReportNumber { get; }
-    public DateTime ReportedAt { get; }
+    public TheftClaimStatus Status { get; }
+    public DateTime CreatedAt { get; }
+    public DateTime UpdatedAt { get; }
 
     private TheftClaim(
-        Guid id, Guid policyId, DateOnly incidentDate, string description,
-        PoliceReportNumber policeReportNumber, DateTime reportedAt)
+        Guid id, Guid policyId, PoliceReportNumber policeReportNumber, TheftClaimStatus status,
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
 
-    public static TheftClaim Register(
-        Guid id, Guid policyId, DateOnly incidentDate, string description,
-        PoliceReportNumber policeReportNumber, DateTime reportedAt)
+    public static TheftClaim Register(Guid id, Guid policyId, PoliceReportNumber policeReportNumber, DateTime now)
     {
         if (policyId == Guid.Empty)
         {
             throw new DomainException("Theft claim must reference a valid policy.");
         }
 
-        return new TheftClaim(id, policyId, incidentDate, description, policeReportNumber, reportedAt);
+        return new TheftClaim(id, policyId, policeReportNumber, TheftClaimStatus.Accepted, now, now);
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
index 0000000..80ed17d
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/Configurations/TheftClaimConfiguration.cs
@@ -0,0 +1,32 @@
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
+        builder.HasKey(c => c.Id);
+        builder.Property(c => c.Id).HasColumnName("id");
+
+        builder.Property(c => c.PolicyId).HasColumnName("policy_id").IsRequired();
+
+        builder.Property(c => c.PoliceReportNumber)
+            .HasConversion(number => number.Value, value => PoliceReportNumber.Create(value))
+            .HasColumnName("police_report_number")
+            .HasMaxLength(50)
+            .IsRequired();
+
+        builder.Property(c => c.Status)
+            .HasConversion<string>()
+            .HasColumnName("status")
+            .HasMaxLength(20)
+            .IsRequired();
+
+        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
+        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
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
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260716000000_AddTheftClaim.Designer.cs b/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260716000000_AddTheftClaim.Designer.cs
new file mode 100644
index 0000000..782a890
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260716000000_AddTheftClaim.Designer.cs
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
+    [Migration("20260716000000_AddTheftClaim")]
+    partial class AddTheftClaim
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
+                    b.Property<Guid>("PolicyId")
+                        .HasColumnType("uniqueidentifier")
+                        .HasColumnName("policy_id");
+
+                    b.Property<string>("PoliceReportNumber")
+                        .IsRequired()
+                        .HasMaxLength(50)
+                        .HasColumnType("nvarchar(50)")
+                        .HasColumnName("police_report_number");
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
+            modelBuilder.Entity("PolicyPlatform.Domain.Customers.Customer", b =>
+                {
+                    b.Property<Guid>("Id")
+                        .ValueGeneratedOnAdd()
+                        .HasColumnType("uniqueidentifier");
+
+                    b.Property<string>("Email")
+                        .IsRequired()
+                        .HasMaxLength(320)
+                        .HasColumnType("nvarchar(320)");
+
+                    b.Property<string>("FullName")
+                        .IsRequired()
+                        .HasMaxLength(200)
+                        .HasColumnType("nvarchar(200)");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("Email")
+                        .IsUnique();
+
+                    b.ToTable("Customers");
+                });
+
+            modelBuilder.Entity("PolicyPlatform.Domain.Policies.Policy", b =>
+                {
+                    b.Pro
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