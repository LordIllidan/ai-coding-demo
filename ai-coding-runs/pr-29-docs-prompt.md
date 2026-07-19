You are the DOCUMENTATION agent in a specialized worker pipeline (separate agents handle
coding, unit tests, e2e tests, database, and review — stay scoped to documentation only,
never touch executable logic).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under documentation:
- Repository: LordIllidan/ai-coding-demo
- PR: #29 AI: [AISDLC-193] [DEV] Data: model, migracja i indeks dla historii logowań
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/29
- Branch: ai-coding/aisdlc-193-dev-data-model-migracja-i-indeks-dla-historii-lo-29680526614

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-193-coding-prompt.md b/ai-coding-runs/aisdlc-193-coding-prompt.md
new file mode 100644
index 0000000..5f0589d
--- /dev/null
+++ b/ai-coding-runs/aisdlc-193-coding-prompt.md
@@ -0,0 +1,36 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-193 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: [DEV] Data: model, migracja i indeks dla historii logowań
+
+Task description:
+~~~markdown
+Parent story: AISDLC-165 — Wyświetlanie historii ostatnich logowań w aplikacji mobilnej
+
+Dodanie modelu danych i migracji dla login_history_entries, utworzenie indeksu (user_id, occurred_at DESC) oraz przygotowanie repozytorium do odczytu wpisów bieżącego użytkownika. Pliki: migracje DB, encja/model, repository, ewentualne fixture/seedy. TODO: zapewnić, że do historii trafiają wyłącznie udane logowania użytkownika z JWT.
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
diff --git a/src/PolicyPlatform.Application/Abstractions/ILoginHistoryRepository.cs b/src/PolicyPlatform.Application/Abstractions/ILoginHistoryRepository.cs
new file mode 100644
index 0000000..2a4856d
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/ILoginHistoryRepository.cs
@@ -0,0 +1,11 @@
+using PolicyPlatform.Domain.LoginHistory;
+
+namespace PolicyPlatform.Application.Abstractions;
+
+public interface ILoginHistoryRepository
+{
+    /// <summary>Returns the given user's login history, newest entry first.</summary>
+    Task<IReadOnlyList<LoginHistoryEntry>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
+
+    Task AddAsync(LoginHistoryEntry entry, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Domain/LoginHistory/DeviceType.cs b/src/PolicyPlatform.Domain/LoginHistory/DeviceType.cs
new file mode 100644
index 0000000..51bed5a
--- /dev/null
+++ b/src/PolicyPlatform.Domain/LoginHistory/DeviceType.cs
@@ -0,0 +1,9 @@
+namespace PolicyPlatform.Domain.LoginHistory;
+
+public enum DeviceType
+{
+    PHONE,
+    TABLET,
+    WEB,
+    UNKNOWN
+}
diff --git a/src/PolicyPlatform.Domain/LoginHistory/LoginHistoryEntry.cs b/src/PolicyPlatform.Domain/LoginHistory/LoginHistoryEntry.cs
new file mode 100644
index 0000000..206f27e
--- /dev/null
+++ b/src/PolicyPlatform.Domain/LoginHistory/LoginHistoryEntry.cs
@@ -0,0 +1,69 @@
+using PolicyPlatform.Domain.Common;
+
+namespace PolicyPlatform.Domain.LoginHistory;
+
+public sealed class LoginHistoryEntry : Entity
+{
+    public Guid UserId { get; }
+    public DateTimeOffset OccurredAt { get; }
+    public string? DeviceLabel { get; }
+    public DeviceType DeviceType { get; }
+    public string? OsName { get; }
+    public string? OsVersion { get; }
+    public Guid? SessionId { get; }
+    public string? IpAddress { get; }
+    public DateTimeOffset CreatedAt { get; }
+
+    private LoginHistoryEntry(
+        Guid id,
+        Guid userId,
+        DateTimeOffset occurredAt,
+        DeviceType deviceType,
+        string? deviceLabel,
+        string? osName,
+        string? osVersion,
+        Guid? sessionId,
+        string? ipAddress,
+        DateTimeOffset createdAt) : base(id)
+    {
+        UserId = userId;
+        OccurredAt = occurredAt;
+        DeviceType = deviceType;
+        DeviceLabel = deviceLabel;
+        OsName = osName;
+        OsVersion = osVersion;
+        SessionId = sessionId;
+        IpAddress = ipAddress;
+        CreatedAt = createdAt;
+    }
+
+    public static LoginHistoryEntry Create(
+        Guid id,
+        Guid userId,
+        DateTimeOffset occurredAt,
+        DeviceType deviceType,
+        string? deviceLabel = null,
+        string? osName = null,
+        string? osVersion = null,
+        Guid? sessionId = null,
+        string? ipAddress = null,
+        DateTimeOffset? createdAt = null)
+    {
+        if (userId == Guid.Empty)
+        {
+            throw new DomainException("Login history entry requires a user id.");
+        }
+
+        return new LoginHistoryEntry(
+            id,
+            userId,
+            occurredAt,
+            deviceType,
+            deviceLabel,
+            osName,
+            osVersion,
+            sessionId,
+            ipAddress,
+            createdAt ?? DateTimeOffset.UtcNow);
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
index b5fa109..9ff697c 100644
--- a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
+++ b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
@@ -25,12 +25,14 @@ public static IServiceCollection AddPolicyPlatformInfrastructure(
         {
             services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
             services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
+            services.AddSingleton<ILoginHistoryRepository, InMemoryLoginHistoryRepository>();
         }
         else
         {
             services.AddDbContext<PolicyPlatformDbContext>(options => options.UseSqlServer(connectionString));
             services.AddScoped<IPolicyRepository, EfPolicyRepository>();
             services.AddScoped<ICustomerRepository, EfCustomerRepository>();
+            services.AddScoped<ILoginHistoryRepository, EfLoginHistoryRepository>();
         }
 
         services.AddSingleton<IPolicyNumberGenerator, SequentialPolicyNumberGenerator>();
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/Configurations/LoginHistoryEntryConfiguration.cs b/src/PolicyPlatform.Infrastructure/Persistence/Configurations/LoginHistoryEntryConfiguration.cs
new file mode 100644
index 0000000..9ca24b5
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/Configurations/LoginHistoryEntryConfiguration.cs
@@ -0,0 +1,30 @@
+using Microsoft.EntityFrameworkCore;
+using Microsoft.EntityFrameworkCore.Metadata.Builders;
+using PolicyPlatform.Domain.LoginHistory;
+
+namespace PolicyPlatform.Infrastructure.Persistence.Configurations;
+
+public sealed class LoginHistoryEntryConfiguration : IEntityTypeConfiguration<LoginHistoryEntry>
+{
+    public void Configure(EntityTypeBuilder<LoginHistoryEntry> builder)
+    {
+        builder.ToTable("LoginHistoryEntries");
+
+        builder.HasKey(e => e.Id);
+
+        builder.Property(e => e.UserId).IsRequired();
+        builder.Property(e => e.OccurredAt).IsRequired();
+
+        builder.Property(e => e.DeviceLabel).HasMaxLength(200);
+        builder.Property(e => e.DeviceType).HasConversion<string>().HasMaxLength(20).IsRequired();
+        builder.Property(e => e.OsName).HasMaxLength(100);
+        builder.Property(e => e.OsVersion).HasMaxLength(50);
+        builder.Property(e => e.IpAddress).HasMaxLength(45);
+
+        builder.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
+
+        builder.HasIndex(e => new { e.UserId, e.OccurredAt })
+            .IsDescending(false, true)
+            .HasDatabaseName("IX_LoginHistoryEntries_UserId_OccurredAt");
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/EfLoginHistoryRepository.cs b/src/PolicyPlatform.Infrastructure/Persistence/EfLoginHistoryRepository.cs
new file mode 100644
index 0000000..42da189
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/EfLoginHistoryRepository.cs
@@ -0,0 +1,24 @@
+using Microsoft.EntityFrameworkCore;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.LoginHistory;
+
+namespace PolicyPlatform.Infrastructure.Persistence;
+
+public sealed class EfLoginHistoryRepository : ILoginHistoryRepository
+{
+    private readonly PolicyPlatformDbContext _db;
+
+    public EfLoginHistoryRepository(PolicyPlatformDbContext db) => _db = db;
+
+    public async Task<IReadOnlyList<LoginHistoryEntry>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
+        => await _db.LoginHistoryEntries
+            .Where(e => e.UserId == userId)
+            .OrderByDescending(e => e.OccurredAt)
+            .ToListAsync(ct);
+
+    public async Task AddAsync(LoginHistoryEntry entry, CancellationToken ct = default)
+    {
+        await _db.LoginHistoryEntries.AddAsync(entry, ct);
+        await _db.SaveChangesAsync(ct);
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/InMemoryLoginHistoryRepository.cs b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryLoginHistoryRepository.cs
new file mode 100644
index 0000000..53c9fc0
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryLoginHistoryRepository.cs
@@ -0,0 +1,26 @@
+using System.Collections.Concurrent;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.LoginHistory;
+
+namespace PolicyPlatform.Infrastructure.Persistence;
+
+public sealed class InMemoryLoginHistoryRepository : ILoginHistoryRepository
+{
+    private readonly ConcurrentDictionary<Guid, LoginHistoryEntry> _entries = new();
+
+    public Task<IReadOnlyList<LoginHistoryEntry>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
+    {
+        IReadOnlyList<LoginHistoryEntry> result = _entries.Values
+            .Where(e => e.UserId == userId)
+            .OrderByDescending(e => e.OccurredAt)
+            .ToList();
+
+        return Task.FromResult(result);
+    }
+
+    public Task AddAsync(LoginHistoryEntry entry, CancellationToken ct = default)
+    {
+        _entries[entry.Id] = entry;
+        return Task.CompletedTask;
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260719090000_AddLoginHistoryEntries.Designer.cs b/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260719090000_AddLoginHistoryEntries.Designer.cs
new file mode 100644
index 0000000..c5db2e5
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260719090000_AddLoginHistoryEntries.Designer.cs
@@ -0,0 +1,222 @@
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
+    [Migration("20260719090000_AddLoginHistoryEntries")]
+    partial class AddLoginHistoryEntries
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
+            modelBuilder.Entity("PolicyPlatform.Domain.LoginHistory.LoginHistoryEntry", b =>
+                {
+                    b.Property<Guid>("Id")
+                        .ValueGeneratedOnAdd()
+                        .HasColumnType("uniqueidentifier");
+
+                    b.Property<DateTimeOffset>("CreatedAt")
+                        .ValueGeneratedOnAdd()
+                        .HasColumnType("datetimeoffset")
+                        .HasDefaultValueSql("SYSUTCDATETIME()");
+
+                    b.Property<string>("DeviceLabel")
+                        .HasMaxLength(200)
+                        .HasColumnType("nvarchar(200)");
+
+                    b.Property<string>("DeviceType")
+                        .IsRequired()
+                        .HasMaxLength(20)
+                        .HasColumnType("nvarchar(20)");
+
+                    b.Property<string>("IpAddress")
+                        .HasMaxLength(45)
+                        .HasColumnType("nvarchar(45)");
+
+                    b.Property<DateTimeOffset>("OccurredAt")
+                        .HasColumnType("datetimeoffset");
+
+                    b.Property<string>("OsName")
+                        .HasMaxLength(100)
+                        .HasColumnType("nvarchar(100)");
+
+                    b.Property<string>("OsVersion")
+                        .HasMaxLength(50)
+                        .HasColumnType("nvarchar(50)");
+
+                    b.Property<Guid?>("SessionId")
+                        .HasColumnType("uniqueidentifier");
+
+                    b.Property<Guid>("UserId")
+                        .HasColumnType("uniqueidentifier");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex(new[] { "UserId", "OccurredAt" })
+                        .IsDescending(false, true)
+                        .HasDatabaseName("IX_LoginHistoryEntries_UserId_OccurredAt");
+
+                    b.ToTable("LoginHistoryEntries");
+                });
+
+            modelBuilder.Entity("PolicyPlatform.Domain.Policies.Policy", b =>
+                {
+                    b.Property<Guid>("Id")
+                        .ValueGeneratedOnAdd()
+                        .HasColumnType("uniqueidentifier");
+
+                    b.Property<Guid>("CustomerId")
+                        .HasColumnType("uniqueidentifier");
+
+                    b.Property<DateOnly>("EffectiveDate")
+                        .HasColumnType("date");
+
+                    b.Property<DateOnly>("ExpiryDate")
+                        .HasColumnType("date");
+
+                    b.Property<string>("Number")
+                        .IsRequired()
+                        .HasMaxLength(20)
+                        .HasColumnType("nvarchar(20)");
+
+                    b.Property<string>("Status")
+                        .IsRequired()
+                        .HasMaxLength(20)
+                        .HasColumnType("nvarchar(20)");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("Number")
+                        .IsUnique();
+
+                    b.ToTable("Policies");
+                });
+
+            modelBuilder.Entity("PolicyPlatform.Domain.Policies.Policy", b =>
+                {
+                    b.OwnsMany("PolicyPlatform.Domain.Policies.Coverage", "Coverages", b1 =>
+                        {
+                            b1.Property<int>("Id")
+                                .ValueGeneratedOnAdd()
+                                .HasColumnType("int");
+
+                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b1.Property<int>("Id"));
+
+                            b1.Property<Guid>("PolicyId")
+                                .HasColumnType("uniqueidentifier");
+
+                            b1.Property<string>("Type")
+                                .IsRequired()
+                                .HasMaxLength(10)
+                                .HasColumnType("nvarchar(10)");
+
+                            b1.HasKey("Id");
+
+                            b1.HasIndex("PolicyId");
+
+                            b1.ToTable("Coverage");
+
+                            b1.WithOwner()
+                                .HasForeignKey("PolicyId");
+
+                            b1.OwnsOne("PolicyPlatform.Domain.Policies.Money", "Premium", b2 =>
+                                {
+                                    b2.Property<int>("CoverageId")
+                                        .HasColumnType("int");
+
+                                    b2.Property<decimal>("Amount")
+                                        .HasPrecision(18, 2)
+                                        .HasColumnType("decimal(18,2)")
+                                        .HasColumnName("PremiumAmount");
+
+                                    b2.Property<string>("Currency")
+                                        .IsRequired()
+                                        .HasMaxLength(3)
+                                        .HasColumnType("nvarchar(3)")
+                                        .HasColumnName("PremiumCurrency");
+
+                                    b2.HasKey("CoverageId");
+
+              
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