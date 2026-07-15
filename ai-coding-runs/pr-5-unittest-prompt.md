You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: LordIllidan/ai-coding-demo
- PR: #5 AI: [AISDLC-12] [Mobile] Push-notyfikacje o zmianie statusu zgłoszenia
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/5
- Branch: ai-coding/aisdlc-12-mobile-push-notyfikacje-o-zmianie-statusu-zg-osz-29391273884

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-12-coding-prompt.md b/ai-coding-runs/aisdlc-12-coding-prompt.md
new file mode 100644
index 0000000..5e151d8
--- /dev/null
+++ b/ai-coding-runs/aisdlc-12-coding-prompt.md
@@ -0,0 +1,26 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-12 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: [Mobile] Push-notyfikacje o zmianie statusu zgłoszenia
+
+Task description:
+~~~markdown
+Parent story: AISDLC-7 — Jako klient chcę zgłosić szkodę komunikacyjną z poziomu aplikacji mobilnej bez logowania do przeglądarki, aby rozpocząć proces bez przechodzenia do kanału webowego.
+
+Co robi: implementuje odbiór i prezentację push-notyfikacji o zmianie statusu zgłoszenia szkody w aplikacji mobilnej. Pliki: konfiguracja push, handler powiadomień, widoki/akcje po kliknięciu. TODO: powiązać identyfikator zgłoszenia, obsłużyć deep link i scenariusze offline.
+Co robi / które pliki / TODO: zweryfikować rejestrację tokenów i uprawnienia systemowe.
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
diff --git a/src/PolicyPlatform.Api/Controllers/DeviceRegistrationsController.cs b/src/PolicyPlatform.Api/Controllers/DeviceRegistrationsController.cs
new file mode 100644
index 0000000..3835ae6
--- /dev/null
+++ b/src/PolicyPlatform.Api/Controllers/DeviceRegistrationsController.cs
@@ -0,0 +1,42 @@
+using Microsoft.AspNetCore.Mvc;
+using PolicyPlatform.Application.Notifications;
+using PolicyPlatform.Domain.Common;
+
+namespace PolicyPlatform.Api.Controllers;
+
+[ApiController]
+[Route("api/device-registrations")]
+public sealed class DeviceRegistrationsController : ControllerBase
+{
+    private readonly DeviceRegistrationService _deviceRegistrations;
+
+    public DeviceRegistrationsController(DeviceRegistrationService deviceRegistrations)
+        => _deviceRegistrations = deviceRegistrations;
+
+    [HttpPost]
+    public async Task<ActionResult<DeviceRegistrationDto>> Register(RegisterDeviceRequest request, CancellationToken ct)
+    {
+        try
+        {
+            var device = await _deviceRegistrations.RegisterDeviceAsync(request, ct);
+            return Ok(device);
+        }
+        catch (DomainException ex)
+        {
+            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
+        }
+    }
+
+    [HttpPost("{id:guid}/unregister")]
+    public async Task<ActionResult<DeviceRegistrationDto>> Unregister(Guid id, CancellationToken ct)
+    {
+        try
+        {
+            return Ok(await _deviceRegistrations.UnregisterDeviceAsync(id, ct));
+        }
+        catch (DomainException ex)
+        {
+            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
+        }
+    }
+}
diff --git a/src/PolicyPlatform.Api/Controllers/PoliciesController.cs b/src/PolicyPlatform.Api/Controllers/PoliciesController.cs
index 9adbc93..098c758 100644
--- a/src/PolicyPlatform.Api/Controllers/PoliciesController.cs
+++ b/src/PolicyPlatform.Api/Controllers/PoliciesController.cs
@@ -1,4 +1,5 @@
 using Microsoft.AspNetCore.Mvc;
+using PolicyPlatform.Application.Notifications;
 using PolicyPlatform.Application.Policies;
 using PolicyPlatform.Domain.Common;
 
@@ -9,8 +10,13 @@ namespace PolicyPlatform.Api.Controllers;
 public sealed class PoliciesController : ControllerBase
 {
     private readonly PolicyService _policyService;
+    private readonly PolicyStatusNotificationService _statusNotifications;
 
-    public PoliciesController(PolicyService policyService) => _policyService = policyService;
+    public PoliciesController(PolicyService policyService, PolicyStatusNotificationService statusNotifications)
+    {
+        _policyService = policyService;
+        _statusNotifications = statusNotifications;
+    }
 
     [HttpPost]
     public async Task<ActionResult<PolicyDto>> Create(CreatePolicyRequest request, CancellationToken ct)
@@ -42,7 +48,7 @@ public async Task<ActionResult<PolicyDto>> Activate(Guid id, CancellationToken c
     {
         try
         {
-            return Ok(await _policyService.ActivatePolicyAsync(id, ct));
+            return Ok(await _statusNotifications.ActivatePolicyAsync(id, ct));
         }
         catch (DomainException ex)
         {
@@ -55,7 +61,7 @@ public async Task<ActionResult<PolicyDto>> Cancel(Guid id, CancellationToken ct)
     {
         try
         {
-            return Ok(await _policyService.CancelPolicyAsync(id, ct));
+            return Ok(await _statusNotifications.CancelPolicyAsync(id, ct));
         }
         catch (DomainException ex)
         {
diff --git a/src/PolicyPlatform.Application/Abstractions/IDeviceRegistrationRepository.cs b/src/PolicyPlatform.Application/Abstractions/IDeviceRegistrationRepository.cs
new file mode 100644
index 0000000..947f008
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/IDeviceRegistrationRepository.cs
@@ -0,0 +1,10 @@
+using PolicyPlatform.Domain.Notifications;
+
+namespace PolicyPlatform.Application.Abstractions;
+
+public interface IDeviceRegistrationRepository
+{
+    Task<DeviceRegistration?> GetByIdAsync(Guid id, CancellationToken ct = default);
+    Task<IReadOnlyList<DeviceRegistration>> ListActiveByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
+    Task AddAsync(DeviceRegistration device, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Application/Abstractions/IPushNotificationSender.cs b/src/PolicyPlatform.Application/Abstractions/IPushNotificationSender.cs
new file mode 100644
index 0000000..15f254f
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/IPushNotificationSender.cs
@@ -0,0 +1,8 @@
+using PolicyPlatform.Domain.Notifications;
+
+namespace PolicyPlatform.Application.Abstractions;
+
+public interface IPushNotificationSender
+{
+    Task SendAsync(DeviceRegistration device, string title, string body, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Application/Notifications/DeviceRegistrationDtos.cs b/src/PolicyPlatform.Application/Notifications/DeviceRegistrationDtos.cs
new file mode 100644
index 0000000..80128a4
--- /dev/null
+++ b/src/PolicyPlatform.Application/Notifications/DeviceRegistrationDtos.cs
@@ -0,0 +1,24 @@
+using PolicyPlatform.Domain.Notifications;
+
+namespace PolicyPlatform.Application.Notifications;
+
+public sealed record RegisterDeviceRequest(
+    Guid CustomerId,
+    string PushToken,
+    DevicePlatform Platform,
+    bool NotificationsPermissionGranted);
+
+public sealed record DeviceRegistrationDto(
+    Guid Id,
+    Guid CustomerId,
+    string Platform,
+    bool NotificationsPermissionGranted,
+    bool IsActive)
+{
+    public static DeviceRegistrationDto FromDomain(DeviceRegistration device) => new(
+        device.Id,
+        device.CustomerId,
+        device.Platform.ToString(),
+        device.NotificationsPermissionGranted,
+        device.IsActive);
+}
diff --git a/src/PolicyPlatform.Application/Notifications/DeviceRegistrationService.cs b/src/PolicyPlatform.Application/Notifications/DeviceRegistrationService.cs
new file mode 100644
index 0000000..b6dd727
--- /dev/null
+++ b/src/PolicyPlatform.Application/Notifications/DeviceRegistrationService.cs
@@ -0,0 +1,41 @@
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.Common;
+using PolicyPlatform.Domain.Notifications;
+
+namespace PolicyPlatform.Application.Notifications;
+
+/// <summary>Application service (use-case layer). Orchestrates domain objects and
+/// repositories; contains no business rules itself — those live in the Domain.</summary>
+public sealed class DeviceRegistrationService
+{
+    private readonly IDeviceRegistrationRepository _devices;
+    private readonly ICustomerRepository _customers;
+
+    public DeviceRegistrationService(IDeviceRegistrationRepository devices, ICustomerRepository customers)
+    {
+        _devices = devices;
+        _customers = customers;
+    }
+
+    public async Task<DeviceRegistrationDto> RegisterDeviceAsync(
+        RegisterDeviceRequest request, CancellationToken ct = default)
+    {
+        var customer = await _customers.GetByIdAsync(request.CustomerId, ct)
+            ?? throw new DomainException($"Customer {request.CustomerId} was not found.");
+
+        var device = DeviceRegistration.Register(
+            Guid.NewGuid(), customer.Id, request.PushToken, request.Platform, request.NotificationsPermissionGranted);
+
+        await _devices.AddAsync(device, ct);
+        return DeviceRegistrationDto.FromDomain(device);
+    }
+
+    public async Task<DeviceRegistrationDto> UnregisterDeviceAsync(Guid deviceId, CancellationToken ct = default)
+    {
+        var device = await _devices.GetByIdAsync(deviceId, ct)
+            ?? throw new DomainException($"Device registration {deviceId} was not found.");
+
+        device.Revoke();
+        return DeviceRegistrationDto.FromDomain(device);
+    }
+}
diff --git a/src/PolicyPlatform.Application/Notifications/PolicyStatusNotificationService.cs b/src/PolicyPlatform.Application/Notifications/PolicyStatusNotificationService.cs
new file mode 100644
index 0000000..373ea4f
--- /dev/null
+++ b/src/PolicyPlatform.Application/Notifications/PolicyStatusNotificationService.cs
@@ -0,0 +1,49 @@
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Application.Policies;
+
+namespace PolicyPlatform.Application.Notifications;
+
+/// <summary>Wraps <see cref="PolicyService"/> status transitions with a push notification
+/// to the customer's registered devices. Kept separate from PolicyService so the core
+/// policy use-cases stay free of notification concerns.</summary>
+public sealed class PolicyStatusNotificationService
+{
+    private readonly PolicyService _policies;
+    private readonly IDeviceRegistrationRepository _devices;
+    private readonly IPushNotificationSender _sender;
+
+    public PolicyStatusNotificationService(
+        PolicyService policies, IDeviceRegistrationRepository devices, IPushNotificationSender sender)
+    {
+        _policies = policies;
+        _devices = devices;
+        _sender = sender;
+    }
+
+    public async Task<PolicyDto> ActivatePolicyAsync(Guid policyId, CancellationToken ct = default)
+    {
+        var policy = await _policies.ActivatePolicyAsync(policyId, ct);
+        await NotifyStatusChangeAsync(policy, ct);
+        return policy;
+    }
+
+    public async Task<PolicyDto> CancelPolicyAsync(Guid policyId, CancellationToken ct = default)
+    {
+        var policy = await _policies.CancelPolicyAsync(policyId, ct);
+        await NotifyStatusChangeAsync(policy, ct);
+        return policy;
+    }
+
+    private async Task NotifyStatusChangeAsync(PolicyDto policy, CancellationToken ct)
+    {
+        var devices = await _devices.ListActiveByCustomerIdAsync(policy.CustomerId, ct);
+        foreach (var device in devices)
+        {
+            await _sender.SendAsync(
+                device,
+                title: "Status zgłoszenia zaktualizowany",
+                body: $"Polisa {policy.Number} ma nowy status: {policy.Status}.",
+                ct);
+        }
+    }
+}
diff --git a/src/PolicyPlatform.Domain/Notifications/DevicePlatform.cs b/src/PolicyPlatform.Domain/Notifications/DevicePlatform.cs
new file mode 100644
index 0000000..084d46b
--- /dev/null
+++ b/src/PolicyPlatform.Domain/Notifications/DevicePlatform.cs
@@ -0,0 +1,7 @@
+namespace PolicyPlatform.Domain.Notifications;
+
+public enum DevicePlatform
+{
+    Ios,
+    Android,
+}
diff --git a/src/PolicyPlatform.Domain/Notifications/DeviceRegistration.cs b/src/PolicyPlatform.Domain/Notifications/DeviceRegistration.cs
new file mode 100644
index 0000000..5ff78aa
--- /dev/null
+++ b/src/PolicyPlatform.Domain/Notifications/DeviceRegistration.cs
@@ -0,0 +1,50 @@
+using PolicyPlatform.Domain.Common;
+
+namespace PolicyPlatform.Domain.Notifications;
+
+public sealed class DeviceRegistration : Entity
+{
+    public Guid CustomerId { get; }
+    public string PushToken { get; }
+    public DevicePlatform Platform { get; }
+    public bool NotificationsPermissionGranted { get; }
+    public bool IsActive { get; private set; }
+
+    private DeviceRegistration(
+        Guid id, Guid customerId, string pushToken, DevicePlatform platform, bool notificationsPermissionGranted)
+        : base(id)
+    {
+        CustomerId = customerId;
+        PushToken = pushToken;
+        Platform = platform;
+        NotificationsPermissionGranted = notificationsPermissionGranted;
+        IsActive = true;
+    }
+
+    public static DeviceRegistration Register(
+        Guid id, Guid customerId, string pushToken, DevicePlatform platform, bool notificationsPermissionGranted)
+    {
+        if (customerId == Guid.Empty)
+        {
+            throw new DomainException("Device registration must belong to a valid customer.");
+        }
+
+        if (string.IsNullOrWhiteSpace(pushToken))
+        {
+            throw new DomainException("Push token is required to register a device.");
+        }
+
+        if (!notificationsPermissionGranted)
+        {
+            throw new DomainException(
+                "Device cannot be registered for push notifications without OS-level notification permission.");
+        }
+
+        return new DeviceRegistration(id, customerId, pushToken.Trim(), platform, notificationsPermissionGranted);
+    }
+
+    public void Revoke()
+    {
+        IsActive = false;
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
index e1ebcd1..8fa1e3e 100644
--- a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
+++ b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
@@ -1,7 +1,9 @@
 using Microsoft.Extensions.DependencyInjection;
 using PolicyPlatform.Application.Abstractions;
 using PolicyPlatform.Application.Customers;
+using PolicyPlatform.Application.Notifications;
 using PolicyPlatform.Application.Policies;
+using PolicyPlatform.Infrastructure.Notifications;
 using PolicyPlatform.Infrastructure.Numbering;
 using PolicyPlatform.Infrastructure.Persistence;
 
@@ -14,8 +16,12 @@ public static IServiceCollection AddPolicyPlatformInfrastructure(this IServiceCo
         services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
         services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
         services.AddSingleton<IPolicyNumberGenerator, SequentialPolicyNumberGenerator>();
+        services.AddSingleton<IDeviceRegistrationRepository, InMemoryDeviceRegistrationRepository>();
+        services.AddSingleton<IPushNotificationSender, LoggingPushNotificationSender>();
         services.AddScoped<PolicyService>();
         services.AddScoped<CustomerService>();
+        services.AddScoped<DeviceRegistrationService>();
+        services.AddScoped<PolicyStatusNotificationService>();
         return services;
     }
 }
diff --git a/src/PolicyPlatform.Infrastructure/Notifications/LoggingPushNotificationSender.cs b/src/PolicyPlatform.Infrastructure/Notifications/LoggingPushNotificationSender.cs
new file mode 100644
index 0000000..2eddfc2
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Notifications/LoggingPushNotificationSender.cs
@@ -0,0 +1,26 @@
+using Microsoft.Extensions.Logging;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.Notifications;
+
+namespace PolicyPlatform.Infrastructure.Notifications;
+
+/// <summary>Placeholder IPushNotificationSender: logs instead of calling FCM/APNs.
+/// Swap for a real gateway (FCM HTTP v1 for Android, APNs for iOS) once push
+/// credentials are provisioned — the Application layer only depends on IPushNotificationSender.</summary>
+public sealed class LoggingPushNotificationSender : IPushNotificationSender
+{
+    private readonly ILogger<LoggingPushNotificationSender> _logger;
+
+    public LoggingPushNotificationSender(ILogger<LoggingPushNotificationSender> logger)
+    {
+        _logger = logger;
+    }
+
+    public Task SendAsync(DeviceRegistration device, string title, string body, CancellationToken ct = default)
+    {
+        _logger.LogInformation(
+            "Push notification to device {DeviceId} ({Platform}) for customer {CustomerId}: {Title} — {Body}",
+            device.Id, device.Platform, device.CustomerId, title, body);
+        return Task.CompletedTask;
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/InMemoryDeviceRegistrationRepository.cs b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryDeviceRegistrationRepository.cs
new file mode 100644
index 0000000..1a6e97e
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/InMemoryDeviceRegistrationRepository.cs
@@ -0,0 +1,26 @@
+using System.Collections.Concurrent;
+using PolicyPlatform.Application.Abstractions;
+using PolicyPlatform.Domain.Notifications;
+
+namespace PolicyPlatform.Infrastructure.Persistence;
+
+/// <summary>Process-lifetime in-memory store. Swap for an EF Core provider once a real
+/// database is provisioned — the Application layer only depends on IDeviceRegistrationRepository.</summary>
+public sealed class InMemoryDeviceRegistrationRepository : IDeviceRegistrationRepository
+{
+    private readonly ConcurrentDictionary<Guid, DeviceRegistration> _devices = new();
+
+    public Task<DeviceRegistration?> GetByIdAsync(Guid id, CancellationToken ct = default)
+        => Task.FromResult(_devices.GetValueOrDefault(id));
+
+    public Task<IReadOnlyList<DeviceRegistration>> ListActiveByCustomerIdAsync(
+        Guid customerId, CancellationToken ct = default)
+        => Task.FromResult<IReadOnlyList<DeviceRegistration>>(
+            _devices.Values.Where(d => d.CustomerId == customerId && d.IsActive).ToList());
+
+    public Task AddAsync(DeviceRegistration device, CancellationToken ct = default)
+    {
+        _devices[device.Id] = device;
+        return Task.CompletedTask;
+    }
+}
diff --git a/src/PolicyPlatform.Infrastructure/PolicyPlatform.Infrastructure.csproj b/src/PolicyPlatform.Infrastructure/PolicyPlatform.Infrastructure.csproj
index a625513..66d4216 100644
--- a/src/PolicyPlatform.Infrastructure/PolicyPlatform.Infrastructure.csproj
+++ b/src/PolicyPlatform.Infrastructure/PolicyPlatform.Infrastructure.csproj
@@ -7,6 +7,7 @@
 
   <ItemGroup>
     <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.10" />
+    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.10" />
   </ItemGroup>
 
   <PropertyGroup>

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