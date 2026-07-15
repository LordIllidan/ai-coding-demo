You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: LordIllidan/ai-coding-demo
- PR: #6 AI: [AISDLC-10] [Mobile] Dodawanie zdjęć z aparatu i galerii do zgłoszenia
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/6
- Branch: ai-coding/aisdlc-10-mobile-dodawanie-zdj-z-aparatu-i-galerii-do-zg-o-29391277740

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-10-coding-prompt.md b/ai-coding-runs/aisdlc-10-coding-prompt.md
new file mode 100644
index 0000000..90ca44b
--- /dev/null
+++ b/ai-coding-runs/aisdlc-10-coding-prompt.md
@@ -0,0 +1,26 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-10 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: [Mobile] Dodawanie zdjęć z aparatu i galerii do zgłoszenia
+
+Task description:
+~~~markdown
+Parent story: AISDLC-7 — Jako klient chcę zgłosić szkodę komunikacyjną z poziomu aplikacji mobilnej bez logowania do przeglądarki, aby rozpocząć proces bez przechodzenia do kanału webowego.
+
+Co robi: dodaje możliwość dołączania zdjęć z aparatu lub galerii urządzenia do zgłoszenia szkody w aplikacji mobilnej. Pliki: ekran formularza zgłoszenia, picker zdjęć, upload/załączniki. TODO: obsłużyć uprawnienia, walidację typu/rozmiaru plików i prezentację miniatur.
+Co robi / które pliki / TODO: dopiąć integrację uploadu z API i stany błędów.
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
diff --git a/src/PolicyPlatform.Api/Controllers/ClaimAttachmentsController.cs b/src/PolicyPlatform.Api/Controllers/ClaimAttachmentsController.cs
new file mode 100644
index 0000000..5f40189
--- /dev/null
+++ b/src/PolicyPlatform.Api/Controllers/ClaimAttachmentsController.cs
@@ -0,0 +1,46 @@
+using Microsoft.AspNetCore.Mvc;
+using PolicyPlatform.Application.Claims;
+using PolicyPlatform.Domain.Common;
+
+namespace PolicyPlatform.Api.Controllers;
+
+/// <summary>Receives claim photos uploaded by the mobile app's camera/gallery picker
+/// (AISDLC-10). File type and size are validated by the domain layer; this controller only
+/// translates the multipart request into bytes and maps domain errors to 400 responses.</summary>
+[ApiController]
+[Route("api/claims/{claimId:guid}/attachments")]
+public sealed class ClaimAttachmentsController : ControllerBase
+{
+    private readonly ClaimAttachmentService _claimAttachments;
+
+    public ClaimAttachmentsController(ClaimAttachmentService claimAttachments) => _claimAttachments = claimAttachments;
+
+    [HttpPost]
+    [RequestSizeLimit(20 * 1024 * 1024)]
+    public async Task<ActionResult<ClaimAttachmentDto>> Upload(Guid claimId, IFormFile file, CancellationToken ct)
+    {
+        if (file is null || file.Length == 0)
+        {
+            return Problem("No file was uploaded.", statusCode: StatusCodes.Status400BadRequest);
+        }
+
+        try
+        {
+            using var stream = new MemoryStream();
+            await file.CopyToAsync(stream, ct);
+
+            var attachment = await _claimAttachments.UploadAsync(
+                claimId, file.FileName, file.ContentType, stream.ToArray(), ct);
+
+            return CreatedAtAction(nameof(List), new { claimId }, attachment);
+        }
+        catch (DomainException ex)
+        {
+            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
+        }
+    }
+
+    [HttpGet]
+    public async Task<ActionResult<IReadOnlyList<ClaimAttachmentDto>>> List(Guid claimId, CancellationToken ct)
+        => Ok(await _claimAttachments.ListByClaimAsync(claimId, ct));
+}
diff --git a/src/PolicyPlatform.Application/Abstractions/IClaimAttachmentRepository.cs b/src/PolicyPlatform.Application/Abstractions/IClaimAttachmentRepository.cs
new file mode 100644
index 0000000..432a7e5
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/IClaimAttachmentRepository.cs
@@ -0,0 +1,10 @@
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Application.Abstractions;
+
+public interface IClaimAttachmentRepository
+{
+    Task<ClaimAttachment?> GetByIdAsync(Guid id, CancellationToken ct = default);
+    Task<IReadOnlyList<ClaimAttachment>> ListByClaimAsync(Guid claimId, CancellationToken ct = default);
+    Task AddAsync(ClaimAttachment attachment, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Application/Claims/ClaimAttachmentDtos.cs b/src/PolicyPlatform.Application/Claims/ClaimAttachmentDtos.cs
new file mode 100644
index 0000000..2aee850
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/ClaimAttachmentDtos.cs
@@ -0,0 +1,15 @@
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Application.Claims;
+
+public sealed record ClaimAttachmentDto(
+    Guid Id, Guid ClaimId, string FileName, string ContentType, long SizeBytes, DateTimeOffset UploadedAtUtc)
+{
+    public static ClaimAttachmentDto FromDomain(ClaimAttachment attachment) => new(
+        attachment.Id,
+        attachment.ClaimId,
+        attachment.FileName,
+        attachment.ContentType,
+        attachment.SizeBytes,
+        attachment.UploadedAtUtc);
+}
diff --git a/src/PolicyPlatform.Application/Claims/ClaimAttachmentService.cs b/src/PolicyPlatform.Application/Claims/ClaimAttachmentService.cs
new file mode 100644
index 0000000..bce0342
--- /dev/null
+++ b/src/PolicyPlatform.Application/Claims/ClaimAttachmentService.cs
@@ -0,0 +1,30 @@
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Application.Claims;
+
+/// <summary>Use-case layer for uploading claim photos taken with the mobile camera or
+/// picked from the gallery (AISDLC-10). Validation rules (allowed type/size) live on the
+/// domain entity; this service only orchestrates persistence.</summary>
+public sealed class ClaimAttachmentService
+{
+    private readonly IClaimAttachmentRepository _attachments;
+
+    public ClaimAttachmentService(IClaimAttachmentRepository attachments) => _attachments = attachments;
+
+    public async Task<ClaimAttachmentDto> UploadAsync(
+        Guid claimId, string fileName, string contentType, byte[] content, CancellationToken ct = default)
+    {
+        var attachment = ClaimAttachment.Create(
+            Guid.NewGuid(), claimId, fileName, contentType, content, DateTimeOffset.UtcNow);
+
+        await _attachments.AddAsync(attachment, ct);
+        return ClaimAttachmentDto.FromDomain(attachment);
+    }
+
+    public async Task<IReadOnlyList<ClaimAttachmentDto>> ListByClaimAsync(Guid claimId, CancellationToken ct = default)
+    {
+        var attachments = await _attachments.ListByClaimAsync(claimId, ct);
+        return attachments.Select(ClaimAttachmentDto.FromDomain).ToList();
+    }
+}
diff --git a/src/PolicyPlatform.Domain/Claims/ClaimAttachment.cs b/src/PolicyPlatform.Domain/Claims/ClaimAttachment.cs
new file mode 100644
index 0000000..78c8370
--- /dev/null
+++ b/src/PolicyPlatform.Domain/Claims/ClaimAttachment.cs
@@ -0,0 +1,65 @@
+using PolicyPlatform.Domain.Common;
+
+namespace PolicyPlatform.Domain.Claims;
+
+/// <summary>A photo (camera or gallery) attached to a damage claim submitted from the
+/// mobile app. Backend counterpart of AISDLC-10 — validates what the mobile picker
+/// uploads; thumbnails and OS-level permission handling stay client-side.</summary>
+public sealed class ClaimAttachment : Entity
+{
+    private static readonly IReadOnlyCollection<string> AllowedContentTypes =
+        new[] { "image/jpeg", "image/png", "image/heic", "image/webp" };
+
+    private const long MaxSizeBytes = 15 * 1024 * 1024; // 15 MB
+
+    public Guid ClaimId { get; }
+    public string FileName { get; }
+    public string ContentType { get; }
+    public long SizeBytes { get; }
+    public byte[] Content { get; }
+    public DateTimeOffset UploadedAtUtc { get; }
+
+    private ClaimAttachment(
+        Guid id, Guid claimId, string fileName, string contentType, byte[] content, DateTimeOffset uploadedAtUtc)
+        : base(id)
+    {
+        ClaimId = claimId;
+        FileName = fileName;
+        ContentType = contentType;
+        SizeBytes = content.LongLength;
+        Content = content;
+        UploadedAtUtc = uploadedAtUtc;
+    }
+
+    public static ClaimAttachment Create(
+        Guid id, Guid claimId, string fileName, string contentType, byte[] content, DateTimeOffset uploadedAtUtc)
+    {
+        if (claimId == Guid.Empty)
+        {
+            throw new DomainException("Attachment must belong to a valid claim.");
+        }
+
+        if (string.IsNullOrWhiteSpace(fileName))
+        {
+            throw new DomainException("Attachment file name is required.");
+        }
+
+        if (!AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
+        {
+            throw new DomainException(
+                $"Content type '{contentType}' is not allowed. Allowed types: {string.Join(", ", AllowedContentTypes)}.");
+        }
+
+        if (content.LongLength <= 0)
+        {
+            throw new DomainException("Attachment file is empty.");
+        }
+
+        if (content.LongLength > MaxSizeBytes)
+        {
+            throw new DomainException($"Attachment exceeds the maximum allowed size of {MaxSizeBytes / (1024 * 1024)} MB.");
+        }
+
+        return new ClaimAttachment(id, claimId, fileName, contentType, content, uploadedAtUtc);
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
index e1ebcd1..8acb14f 100644
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
+        services.AddSingleton<IClaimAttachmentRepository, InMemoryClaimAttachmentRepository>();
         services.AddSingleton<IPolicyNumberGenerator, SequentialPolicyNumberGenerator>();
         services.AddScoped<PolicyService>();
         services.AddScoped<CustomerService>();
+        services.AddScoped<ClaimAttachmentService>();
         return services;
     }
 }
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimAttachmentRepository.cs b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimAttachmentRepository.cs
new file mode 100644
index 0000000..8275250
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryClaimAttachmentRepository.cs
@@ -0,0 +1,25 @@
+using System.Collections.Concurrent;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Infrastructure.Persistence;
+
+/// <summary>Process-lifetime in-memory store. Swap for blob storage once a real backend is
+/// provisioned — the Application layer only depends on IClaimAttachmentRepository.</summary>
+public sealed class InMemoryClaimAttachmentRepository : IClaimAttachmentRepository
+{
+    private readonly ConcurrentDictionary<Guid, ClaimAttachment> _attachments = new();
+
+    public Task<ClaimAttachment?> GetByIdAsync(Guid id, CancellationToken ct = default)
+        => Task.FromResult(_attachments.GetValueOrDefault(id));
+
+    public Task<IReadOnlyList<ClaimAttachment>> ListByClaimAsync(Guid claimId, CancellationToken ct = default)
+        => Task.FromResult<IReadOnlyList<ClaimAttachment>>(
+            _attachments.Values.Where(a => a.ClaimId == claimId).ToList());
+
+    public Task AddAsync(ClaimAttachment attachment, CancellationToken ct = default)
+    {
+        _attachments[attachment.Id] = attachment;
+        return Task.CompletedTask;
+    }
+}

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