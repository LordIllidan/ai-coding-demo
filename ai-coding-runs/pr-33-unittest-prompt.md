You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: LordIllidan/ai-coding-demo
- PR: #33 AI: [AISDLC-189] [DEV] Data: model, migracja i indeks dla historii logowań
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/33
- Branch: ai-coding/aisdlc-189-dev-data-model-migracja-i-indeks-dla-historii-lo-29680530450

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-189-coding-prompt.md b/ai-coding-runs/aisdlc-189-coding-prompt.md
new file mode 100644
index 0000000..1ee3f69
--- /dev/null
+++ b/ai-coding-runs/aisdlc-189-coding-prompt.md
@@ -0,0 +1,36 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-189 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: [DEV] Data: model, migracja i indeks dla historii logowań
+
+Task description:
+~~~markdown
+Parent story: AISDLC-165 — Wyświetlanie historii ostatnich logowań w aplikacji mobilnej
+
+TODO: dodać migrację i model dla tabeli login_history_entries, w tym indeks (user_id, occurred_at DESC), oraz spiąć encję/repozytorium z polami z kontraktu: occurredAt, deviceLabel, deviceType, osName, osVersion, sessionId, ipAddress.
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
index 0000000..a465fb3
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/ILoginHistoryRepository.cs
@@ -0,0 +1,12 @@
+using PolicyPlatform.Domain.Auth;
+
+namespace PolicyPlatform.Application.Abstractions;
+
+public interface ILoginHistoryRepository
+{
+    /// <summary>Returns entries for the given user, sorted by OccurredAt descending
+    /// (newest first) to match the mobile login-history contract.</summary>
+    Task<IReadOnlyList<LoginHistoryEntry>> ListForUserAsync(Guid userId, CancellationToken ct = default);
+
+    Task AddAsync(LoginHistoryEntry entry, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Domain/Auth/LoginDeviceType.cs b/src/PolicyPlatform.Domain/Auth/LoginDeviceType.cs
new file mode 100644
index 0000000..5e94976
--- /dev/null
+++ b/src/PolicyPlatform.Domain/Auth/LoginDeviceType.cs
@@ -0,0 +1,9 @@
+namespace PolicyPlatform.Domain.Auth;
+
+public enum LoginDeviceType
+{
+    Phone,
+    Tablet,
+    Web,
+    Unknown,
+}
diff --git a/src/PolicyPlatform.Domain/Auth/LoginHistoryEntry.cs b/src/PolicyPlatform.Domain/Auth/LoginHistoryEntry.cs
new file mode 100644
index 0000000..50d176b
--- /dev/null
+++ b/src/PolicyPlatform.Domain/Auth/LoginHistoryEntry.cs
@@ -0,0 +1,69 @@
+using PolicyPlatform.Domain.Common;
+
+namespace PolicyPlatform.Domain.Auth;
+
+public sealed class LoginHistoryEntry : Entity
+{
+    public Guid UserId { get; }
+    public DateTimeOffset OccurredAt { get; }
+    public string? DeviceLabel { get; }
+    public LoginDeviceType DeviceType { get; }
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
+        string? deviceLabel,
+        LoginDeviceType deviceType,
+        string? osName,
+        string? osVersion,
+        Guid? sessionId,
+        string? ipAddress,
+        DateTimeOffset createdAt) : base(id)
+    {
+        UserId = userId;
+        OccurredAt = occurredAt;
+        DeviceLabel = deviceLabel;
+        DeviceType = deviceType;
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
+        LoginDeviceType deviceType,
+        string? deviceLabel = null,
+        string? osName = null,
+        string? osVersion = null,
+        Guid? sessionId = null,
+        string? ipAddress = null,
+        DateTimeOffset? createdAt = null)
+    {
+        if (userId == Guid.Empty)
+        {
+            throw new DomainException("Login history entry user id cannot be empty.");
+        }
+
+        return new LoginHistoryEntry(
+            id,
+            userId,
+            occurredAt,
+            deviceLabel,
+            deviceType,
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
index 0000000..36738ee
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/Configurations/LoginHistoryEntryConfiguration.cs
@@ -0,0 +1,30 @@
+using Microsoft.EntityFrameworkCore;
+using Microsoft.EntityFrameworkCore.Metadata.Builders;
+using PolicyPlatform.Domain.Auth;
+
+namespace PolicyPlatform.Infrastructure.Persistence.Configurations;
+
+public sealed class LoginHistoryEntryConfiguration : IEntityTypeConfiguration<LoginHistoryEntry>
+{
+    public void Configure(EntityTypeBuilder<LoginHistoryEntry> builder)
+    {
+        builder.ToTable("login_history_entries");
+
+        builder.HasKey(e => e.Id);
+        builder.Property(e => e.Id).HasColumnName("id");
+
+        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
+        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();
+        builder.Property(e => e.DeviceLabel).HasColumnName("device_label").HasMaxLength(200);
+        builder.Property(e => e.DeviceType).HasColumnName("device_type").HasConversion<string>().HasMaxLength(20).IsRequired();
+        builder.Property(e => e.OsName).HasColumnName("os_name").HasMaxLength(100);
+        builder.Property(e => e.OsVersion).HasColumnName("os_version").HasMaxLength(50);
+        builder.Property(e => e.SessionId).HasColumnName("session_id");
+        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
+        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
+
+        builder.HasIndex(e => new { e.UserId, e.OccurredAt })
+            .HasDatabaseName("IX_login_history_entries_user_id_occurred_at")
+            .IsDescending(false, true);
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/EfLoginHistoryRepository.cs b/src/PolicyPlatform.Infrastructure/Persistence/EfLoginHistoryRepository.cs
new file mode 100644
index 0000000..d4b2553
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/EfLoginHistoryRepository.cs
@@ -0,0 +1,24 @@
+using Microsoft.EntityFrameworkCore;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.Auth;
+
+namespace PolicyPlatform.Infrastructure.Persistence;
+
+public sealed class EfLoginHistoryRepository : ILoginHistoryRepository
+{
+    private readonly PolicyPlatformDbContext _db;
+
+    public EfLoginHistoryRepository(PolicyPlatformDbContext db) => _db = db;
+
+    public async Task<IReadOnlyList<LoginHistoryEntry>> ListForUserAsync(Guid userId, CancellationToken ct = default)
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
index 0000000..0cae74b
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryLoginHistoryRepository.cs
@@ -0,0 +1,25 @@
+using System.Collections.Concurrent;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.Auth;
+
+namespace PolicyPlatform.Infrastructure.Persistence;
+
+public sealed class InMemoryLoginHistoryRepository : ILoginHistoryRepository
+{
+    private readonly ConcurrentDictionary<Guid, LoginHistoryEntry> _entries = new();
+
+    public Task<IReadOnlyList<LoginHistoryEntry>> ListForUserAsync(Guid userId, CancellationToken ct = default)
+    {
+        IReadOnlyList<LoginHistoryEntry> result = _entries.Values
+            .Where(e => e.UserId == userId)
+            .OrderByDescending(e => e.OccurredAt)
+            .ToList();
+        return Task.FromResult(result);
+    }
+
+    public Task AddAsync(LoginHistoryEntry entry, CancellationToken ct = default)
+    {
+        _entries[entry.Id] = entry;
+        return Task.CompletedTask;
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260719120000_AddLoginHistoryEntries.Designer.cs b/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260719120000_AddLoginHistoryEntries.Designer.cs
new file mode 100644
index 0000000..c0992c3
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/Migrations/20260719120000_AddLoginHistoryEntries.Designer.cs
@@ -0,0 +1,230 @@
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
+    [Migration("20260719120000_AddLoginHistoryEntries")]
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
+            modelBuilder.Entity("PolicyPlatform.Domain.Auth.LoginHistoryEntry", b =>
+                {
+                    b.Property<Guid>("Id")
+                        .ValueGeneratedOnAdd()
+                        .HasColumnType("uniqueidentifier")
+                        .HasColumnName("id");
+
+                    b.Property<DateTimeOffset>("CreatedAt")
+                        .HasColumnType("datetimeoffset")
+                        .HasColumnName("created_at");
+
+                    b.Property<string>("DeviceLabel")
+                        .HasMaxLength(200)
+                        .HasColumnType("nvarchar(200)")
+                        .HasColumnName("device_label");
+
+                    b.Property<string>("DeviceType")
+                        .IsRequired()
+                        .HasMaxLength(20)
+                        .HasColumnType("nvarchar(20)")
+                        .HasColumnName("device_type");
+
+                    b.Property<string>("IpAddress")
+                        .HasMaxLength(45)
+                        .HasColumnType("nvarchar(45)")
+                        .HasColumnName("ip_address");
+
+                    b.Property<DateTimeOffset>("OccurredAt")
+                        .HasColumnType("datetimeoffset")
+                        .HasColumnName("occurred_at");
+
+                    b.Property<string>("OsName")
+                        .HasMaxLength(100)
+                        .HasColumnType("nvarchar(100)")
+                        .HasColumnName("os_name");
+
+                    b.Property<string>("OsVersion")
+                        .HasMaxLength(50)
+                        .HasColumnType("nvarchar(50)")
+                        .HasColumnName("os_version");
+
+                    b.Property<Guid?>("SessionId")
+                        .HasColumnType("uniqueidentifier")
+                        .HasColumnName("session_id");
+
+                    b.Property<Guid>("UserId")
+                        .HasColumnType("uniqueidentifier")
+                        .HasColumnName("user_id");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex(new[] { "UserId", "OccurredAt" })
+                        .HasDatabaseName("IX_login_history_entries_user_id_occurred_at")
+                        .IsDescending(false, true);
+
+                    b.ToTable("login_history_entries");
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
+                                    b2.Prope
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