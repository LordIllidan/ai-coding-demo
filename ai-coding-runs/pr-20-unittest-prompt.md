You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: LordIllidan/ai-coding-demo
- PR: #20 AI: [AISDLC-139] [Mobile] Read-only ekran ostatniej wypłaty
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/20
- Branch: ai-coding/aisdlc-139-mobile-read-only-ekran-ostatniej-wyp-aty-29643129939

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-139-coding-prompt.md b/ai-coding-runs/aisdlc-139-coding-prompt.md
new file mode 100644
index 0000000..3946eed
--- /dev/null
+++ b/ai-coding-runs/aisdlc-139-coding-prompt.md
@@ -0,0 +1,30 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-139 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: [Mobile] Read-only ekran ostatniej wypłaty
+
+Task description:
+~~~markdown
+Parent story: AISDLC-118 — Wyświetlenie ostatniej wypłaconej transzy odszkodowania w aplikacji mobilnej
+
+Widok tylko do odczytu pokazujący kwotę, datę i numer szkody z GET /api/mobile/me/claims/last-payout; brak edycji i brak akcji zapisu. Pliki TODO: ekran/kontroler mobilny, model widoku, mapowanie błędów, stan ładowania/pusty.
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
diff --git a/src/PolicyPlatform.Api/Controllers/MobileClaimsController.cs b/src/PolicyPlatform.Api/Controllers/MobileClaimsController.cs
new file mode 100644
index 0000000..9870516
--- /dev/null
+++ b/src/PolicyPlatform.Api/Controllers/MobileClaimsController.cs
@@ -0,0 +1,68 @@
+using System.Security.Claims;
+using Microsoft.AspNetCore.Authorization;
+using Microsoft.AspNetCore.Mvc;
+using PolicyPlatform.Application.Mobile;
+
+namespace PolicyPlatform.Api.Controllers;
+
+/// <summary>Read-only endpoints for the mobile app. The authenticated customer is always
+/// resolved from the JWT subject — these endpoints never accept a client-supplied
+/// customerId/policyId/claimId, so there is no identifier for a client to tamper with.</summary>
+[ApiController]
+[Authorize]
+[Route("api/mobile/me/claims")]
+public sealed class MobileClaimsController : ControllerBase
+{
+    private readonly MobileClaimPayoutService _payouts;
+    private readonly ILogger<MobileClaimsController> _logger;
+
+    public MobileClaimsController(MobileClaimPayoutService payouts, ILogger<MobileClaimsController> logger)
+    {
+        _payouts = payouts;
+        _logger = logger;
+    }
+
+    [HttpGet("last-payout")]
+    public async Task<ActionResult<LastPayoutResponse>> GetLastPayout(CancellationToken ct)
+    {
+        if (!TryGetCustomerId(out var customerId))
+        {
+            return Error(StatusCodes.Status401Unauthorized, MobileErrorCodes.AuthRequired,
+                "The token does not contain a resolvable customer identity.");
+        }
+
+        try
+        {
+            var response = await _payouts.GetLastPayoutAsync(customerId, ct);
+            return Ok(response);
+        }
+        catch (LastPayoutNotFoundException)
+        {
+            return Error(StatusCodes.Status404NotFound, MobileErrorCodes.LastPayoutNotFound,
+                "No paid claim payout was found for this customer.");
+        }
+        catch (DataSourceUnavailableException)
+        {
+            return Error(StatusCodes.Status503ServiceUnavailable, MobileErrorCodes.DataSourceTimeout,
+                "The data source timed out. Please try again shortly.");
+        }
+        catch (Exception ex)
+        {
+            _logger.LogError(ex, "Unexpected failure while fetching the last claim payout.");
+            return Error(StatusCodes.Status500InternalServerError, MobileErrorCodes.InternalError,
+                "An unexpected error occurred.");
+        }
+    }
+
+    private ObjectResult Error(int statusCode, string code, string message)
+        => StatusCode(statusCode, new { error = code, message });
+
+    private bool TryGetCustomerId(out Guid customerId)
+    {
+        var value = User.FindFirstValue("customerId")
+            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
+            ?? User.FindFirstValue("sub");
+
+        return Guid.TryParse(value, out customerId);
+    }
+}
diff --git a/src/PolicyPlatform.Api/PolicyPlatform.Api.csproj b/src/PolicyPlatform.Api/PolicyPlatform.Api.csproj
index f78b87a..6f7b1cc 100644
--- a/src/PolicyPlatform.Api/PolicyPlatform.Api.csproj
+++ b/src/PolicyPlatform.Api/PolicyPlatform.Api.csproj
@@ -7,6 +7,7 @@
   </PropertyGroup>
 
   <ItemGroup>
+    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
     <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.17" />
     <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0">
       <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
diff --git a/src/PolicyPlatform.Api/Program.cs b/src/PolicyPlatform.Api/Program.cs
index fe20a14..fce3e5f 100644
--- a/src/PolicyPlatform.Api/Program.cs
+++ b/src/PolicyPlatform.Api/Program.cs
@@ -1,4 +1,8 @@
+using System.Text;
 using System.Text.Json.Serialization;
+using Microsoft.AspNetCore.Authentication.JwtBearer;
+using Microsoft.IdentityModel.Tokens;
+using PolicyPlatform.Application.Mobile;
 using PolicyPlatform.Infrastructure;
 
 var builder = WebApplication.CreateBuilder(args);
@@ -8,6 +12,52 @@
 builder.Services.AddOpenApi();
 builder.Services.AddPolicyPlatformInfrastructure(builder.Configuration);
 
+var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
+    ?? throw new InvalidOperationException("Configuration key 'Jwt:SigningKey' is required.");
+
+builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
+    .AddJwtBearer(options =>
+    {
+        options.TokenValidationParameters = new TokenValidationParameters
+        {
+            ValidIssuer = builder.Configuration["Jwt:Issuer"],
+            ValidAudience = builder.Configuration["Jwt:Audience"],
+            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
+            ValidateIssuer = true,
+            ValidateAudience = true,
+            ValidateIssuerSigningKey = true,
+            ValidateLifetime = true,
+        };
+
+        // Unauthenticated/forbidden requests must return the mobile error envelope
+        // ({ error, message }) instead of ASP.NET Core's default empty 401/403 body.
+        options.Events = new JwtBearerEvents
+        {
+            OnChallenge = async context =>
+            {
+                context.HandleResponse();
+                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
+                context.Response.ContentType = "application/json";
+                await context.Response.WriteAsJsonAsync(new
+                {
+                    error = MobileErrorCodes.AuthRequired,
+                    message = "A valid bearer token is required.",
+                });
+            },
+            OnForbidden = async context =>
+            {
+                context.Response.StatusCode = StatusCodes.Status403Forbidden;
+                context.Response.ContentType = "application/json";
+                await context.Response.WriteAsJsonAsync(new
+                {
+                    error = MobileErrorCodes.ForbiddenCrossCustomer,
+                    message = "The token is not authorized to access this data.",
+                });
+            },
+        };
+    });
+builder.Services.AddAuthorization();
+
 var app = builder.Build();
 
 if (app.Environment.IsDevelopment())
@@ -16,6 +66,7 @@
 }
 
 app.UseHttpsRedirection();
+app.UseAuthentication();
 app.UseAuthorization();
 app.MapControllers();
 app.UseDefaultFiles();
diff --git a/src/PolicyPlatform.Api/appsettings.Development.json b/src/PolicyPlatform.Api/appsettings.Development.json
index 0c208ae..7130321 100644
--- a/src/PolicyPlatform.Api/appsettings.Development.json
+++ b/src/PolicyPlatform.Api/appsettings.Development.json
@@ -4,5 +4,8 @@
       "Default": "Information",
       "Microsoft.AspNetCore": "Warning"
     }
+  },
+  "Jwt": {
+    "SigningKey": "dev-only-local-signing-key-not-for-production-use-0123456789"
   }
 }
diff --git a/src/PolicyPlatform.Api/appsettings.json b/src/PolicyPlatform.Api/appsettings.json
index 10f68b8..fbe9821 100644
--- a/src/PolicyPlatform.Api/appsettings.json
+++ b/src/PolicyPlatform.Api/appsettings.json
@@ -5,5 +5,9 @@
       "Microsoft.AspNetCore": "Warning"
     }
   },
-  "AllowedHosts": "*"
+  "AllowedHosts": "*",
+  "Jwt": {
+    "Issuer": "policy-platform",
+    "Audience": "policy-platform-mobile"
+  }
 }
diff --git a/src/PolicyPlatform.Application/Abstractions/IClaimPayoutRepository.cs b/src/PolicyPlatform.Application/Abstractions/IClaimPayoutRepository.cs
new file mode 100644
index 0000000..c0a7f0a
--- /dev/null
+++ b/src/PolicyPlatform.Application/Abstractions/IClaimPayoutRepository.cs
@@ -0,0 +1,11 @@
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Application.Abstractions;
+
+public interface IClaimPayoutRepository
+{
+    /// <summary>Returns the most recently paid payout for the customer, or null if none exists.
+    /// Callers must pass only a customer id derived from the authenticated request (JWT subject) —
+    /// never one supplied by the client in a path/query/body parameter.</summary>
+    Task<ClaimPayout?> GetLastPaidPayoutAsync(Guid customerId, CancellationToken ct = default);
+}
diff --git a/src/PolicyPlatform.Application/Mobile/MobileClaimPayoutDtos.cs b/src/PolicyPlatform.Application/Mobile/MobileClaimPayoutDtos.cs
new file mode 100644
index 0000000..7ceab5e
--- /dev/null
+++ b/src/PolicyPlatform.Application/Mobile/MobileClaimPayoutDtos.cs
@@ -0,0 +1,20 @@
+using System.Globalization;
+using PolicyPlatform.Domain.Claims;
+
+namespace PolicyPlatform.Application.Mobile;
+
+public sealed record LastPayoutAmountDto(string Value, string Currency);
+
+public sealed record LastPayoutResponse(
+    string ClaimNumber,
+    LastPayoutAmountDto Amount,
+    string PayoutDate,
+    bool ReadOnly = true)
+{
+    public static LastPayoutResponse FromDomain(ClaimPayout payout) => new(
+        payout.ClaimNumber,
+        new LastPayoutAmountDto(
+            payout.Amount.Amount.ToString("F2", CultureInfo.InvariantCulture),
+            payout.Amount.Currency),
+        DateOnly.FromDateTime(payout.PaidAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
+}
diff --git a/src/PolicyPlatform.Application/Mobile/MobileClaimPayoutExceptions.cs b/src/PolicyPlatform.Application/Mobile/MobileClaimPayoutExceptions.cs
new file mode 100644
index 0000000..c2695a5
--- /dev/null
+++ b/src/PolicyPlatform.Application/Mobile/MobileClaimPayoutExceptions.cs
@@ -0,0 +1,18 @@
+namespace PolicyPlatform.Application.Mobile;
+
+/// <summary>Error codes returned in the mobile last-payout error envelope. Shared between the
+/// JWT authentication pipeline (AUTH_REQUIRED/FORBIDDEN_CROSS_CUSTOMER) and the application/API
+/// layers (the rest), so both sides agree on the same literal strings.</summary>
+public static class MobileErrorCodes
+{
+    public const string AuthRequired = "AUTH_REQUIRED";
+    public const string ForbiddenCrossCustomer = "FORBIDDEN_CROSS_CUSTOMER";
+    public const string LastPayoutNotFound = "LAST_PAYOUT_NOT_FOUND";
+    public const string DataSourceTimeout = "DATA_SOURCE_TIMEOUT";
+    public const string InternalError = "INTERNAL_ERROR";
+}
+
+public sealed class LastPayoutNotFoundException() : Exception("No paid claim payout was found for the current customer.");
+
+public sealed class DataSourceUnavailableException(Exception inner)
+    : Exception("The data source timed out or is unavailable.", inner);
diff --git a/src/PolicyPlatform.Application/Mobile/MobileClaimPayoutService.cs b/src/PolicyPlatform.Application/Mobile/MobileClaimPayoutService.cs
new file mode 100644
index 0000000..1cc8177
--- /dev/null
+++ b/src/PolicyPlatform.Application/Mobile/MobileClaimPayoutService.cs
@@ -0,0 +1,29 @@
+using System.Data.Common;
+using PolicyPlatform.Application.Abstractions;
+
+namespace PolicyPlatform.Application.Mobile;
+
+/// <summary>Application service (use-case layer) for the mobile "last payout" read-only screen.
+/// The customer id must come from the authenticated request (JWT subject) — this service never
+/// accepts one supplied by the client.</summary>
+public sealed class MobileClaimPayoutService
+{
+    private readonly IClaimPayoutRepository _payouts;
+
+    public MobileClaimPayoutService(IClaimPayoutRepository payouts) => _payouts = payouts;
+
+    public async Task<LastPayoutResponse> GetLastPayoutAsync(Guid customerId, CancellationToken ct = default)
+    {
+        try
+        {
+            var payout = await _payouts.GetLastPaidPayoutAsync(customerId, ct);
+            return payout is null
+                ? throw new LastPayoutNotFoundException()
+                : LastPayoutResponse.FromDomain(payout);
+        }
+        catch (Exception ex) when (ex is TimeoutException or DbException)
+        {
+            throw new DataSourceUnavailableException(ex);
+        }
+    }
+}
diff --git a/src/PolicyPlatform.Domain/Claims/ClaimPayout.cs b/src/PolicyPlatform.Domain/Claims/ClaimPayout.cs
new file mode 100644
index 0000000..5d090ce
--- /dev/null
+++ b/src/PolicyPlatform.Domain/Claims/ClaimPayout.cs
@@ -0,0 +1,49 @@
+using PolicyPlatform.Domain.Common;
+using PolicyPlatform.Domain.Policies;
+
+namespace PolicyPlatform.Domain.Claims;
+
+public sealed class ClaimPayout : Entity
+{
+    public Guid ClaimId { get; }
+    public string ClaimNumber { get; }
+    public Guid CustomerId { get; }
+    public Money Amount { get; }
+    public DateTime PaidAt { get; }
+    public ClaimPayoutStatus Status { get; }
+
+    private ClaimPayout(
+        Guid id, Guid claimId, string claimNumber, Guid customerId,
+        Money amount, DateTime paidAt, ClaimPayoutStatus status)
+        : base(id)
+    {
+        ClaimId = claimId;
+        ClaimNumber = claimNumber;
+        CustomerId = customerId;
+        Amount = amount;
+        PaidAt = paidAt;
+        Status = status;
+    }
+
+    public static ClaimPayout Create(
+        Guid id, Guid claimId, string claimNumber, Guid customerId,
+        Money amount, DateTime paidAt, ClaimPayoutStatus status)
+    {
+        if (claimId == Guid.Empty)
+        {
+            throw new DomainException("Claim payout must reference a valid claim.");
+        }
+
+        if (string.IsNullOrWhiteSpace(claimNumber))
+        {
+            throw new DomainException("Claim payout must reference a claim number.");
+        }
+
+        if (customerId == Guid.Empty)
+        {
+            throw new DomainException("Claim payout must reference a valid customer.");
+        }
+
+        return new ClaimPayout(id, claimId, claimNumber, customerId, amount, paidAt, status);
+    }
+}
diff --git a/src/PolicyPlatform.Domain/Claims/ClaimPayoutStatus.cs b/src/PolicyPlatform.Domain/Claims/ClaimPayoutStatus.cs
new file mode 100644
index 0000000..7b1c202
--- /dev/null
+++ b/src/PolicyPlatform.Domain/Claims/ClaimPayoutStatus.cs
@@ -0,0 +1,8 @@
+namespace PolicyPlatform.Domain.Claims;
+
+public enum ClaimPayoutStatus
+{
+    Pending,
+    Paid,
+    Rejected
+}
diff --git a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
index b5fa109..d5b987d 100644
--- a/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
+++ b/src/PolicyPlatform.Infrastructure/DependencyInjection.cs
@@ -4,6 +4,7 @@
 using PolicyPlatform.Application.Abstractions;
 using PolicyPlatform.Application.Claims;
 using PolicyPlatform.Application.Customers;
+using PolicyPlatform.Application.Mobile;
 using PolicyPlatform.Application.Policies;
 using PolicyPlatform.Infrastructure.Numbering;
 using PolicyPlatform.Infrastructure.Persistence;
@@ -25,12 +26,14 @@ public static IServiceCollection AddPolicyPlatformInfrastructure(
         {
             services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
             services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
+            services.AddSingleton<IClaimPayoutRepository, InMemoryClaimPayoutRepository>();
         }
         else
         {
             services.AddDbContext<PolicyPlatformDbContext>(options => options.UseSqlServer(connectionString));
             services.AddScoped<IPolicyRepository, EfPolicyRepository>();
             services.AddScoped<ICustomerRepository, EfCustomerRepository>();
+            services.AddScoped<IClaimPayoutRepository, EfClaimPayoutRepository>();
         }
 
         services.AddSingleton<IPolicyNumberGenerator, SequentialPolicyNumberGenerator>();
@@ -41,6 +44,7 @@ public static IServiceCollection AddPolicyPlatformInfrastructure(
         // piece of work) — in-memory keeps the theft-claim validation flow runnable now.
         services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
         services.AddScoped<ClaimService>();
+        services.AddScoped<MobileClaimPayoutService>();
         return services;
     }
 }
diff --git a/src/PolicyPlatform.Infrastructure/Persistence/Configurations/ClaimPayoutConfiguration.cs b/src/PolicyPlatform.Infrastructure/Persistence/Configurations/ClaimPayoutConfiguration.cs
new file mode 100644
index 0000000..04ecf87
--- /dev/null
+++ b/src/PolicyPlatform.Infrastructure/Persistence/Configurations/ClaimPayoutConfiguration.cs
@@ -0,0 +1,28 @@
+using Microsoft.EntityFrameworkCore;
+using Microsoft.EntityFrameworkCore.Metadata.Builders;
+using PolicyPlatform.Domain.
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