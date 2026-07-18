using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Api.E2E.Tests;

// AISDLC-138: GET /api/mobile/me/claims/last-payout. The customer identity comes exclusively
// from the Bearer JWT (customerId claim, falling back to sub/NameIdentifier) — the request
// never carries customerId/policyId/claimId. These tests drive the real HTTP pipeline
// (WebApplicationFactory<Program>) with hand-signed HS256 tokens, so no framework internals
// are mocked; only the outermost persistence dependency is swapped for the 503 timeout case,
// since nothing in the current in-memory repository can otherwise reach that branch.
public sealed class MobileClaimsLastPayoutEndpointTests
    : IClassFixture<MobileClaimsLastPayoutEndpointTests.JwtTestWebApplicationFactory>
{
    private const string Route = "/api/mobile/me/claims/last-payout";

    private readonly JwtTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MobileClaimsLastPayoutEndpointTests(JwtTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private sealed record MoneyDto(string Value, string Currency);

    private sealed record LastPayoutDto(string ClaimNumber, MoneyDto Amount, string PayoutDate, bool ReadOnly);

    private sealed record ProblemDto(string? Title, int? Status);

    private void SeedPayout(
        Guid customerId, string claimNumber, decimal amountGross, string currencyCode, DateTime paidAt,
        ClaimPayoutStatus status = ClaimPayoutStatus.Paid)
    {
        var repository = (InMemoryClaimPayoutRepository)_factory.Services.GetRequiredService<IClaimPayoutRepository>();
        var payout = ClaimPayout.Register(
            Guid.NewGuid(), Guid.NewGuid(), claimNumber, customerId, amountGross, currencyCode, paidAt, status);
        repository.AddAsync(payout).GetAwaiter().GetResult();
    }

    private HttpClient AuthorizedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Get_ValidTokenNoPayouts_ReturnsNotFound()
    {
        var customerId = Guid.NewGuid();
        var token = JwtTestWebApplicationFactory.CreateToken(customerId: customerId);

        var response = await AuthorizedClient(token).GetAsync(Route);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
        Assert.Equal("LAST_PAYOUT_NOT_FOUND", problem!.Title);
    }

    [Fact]
    public async Task Get_ValidTokenWithPayout_ReturnsMappedDto()
    {
        var customerId = Guid.NewGuid();
        SeedPayout(customerId, "CLM-2026-0001", 1234.5m, "pln", new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc));
        var token = JwtTestWebApplicationFactory.CreateToken(customerId: customerId);

        var response = await AuthorizedClient(token).GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LastPayoutDto>();
        Assert.NotNull(dto);
        Assert.Equal("CLM-2026-0001", dto!.ClaimNumber);
        Assert.Equal("1234.50", dto.Amount.Value);
        Assert.Equal("PLN", dto.Amount.Currency);
        Assert.Equal("2026-03-10", dto.PayoutDate);
        Assert.True(dto.ReadOnly);
    }

    [Fact]
    public async Task Get_MultiplePayouts_ReturnsMostRecentOnly()
    {
        var customerId = Guid.NewGuid();
        SeedPayout(customerId, "CLM-OLD", 100m, "PLN", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SeedPayout(customerId, "CLM-NEW", 200m, "PLN", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var token = JwtTestWebApplicationFactory.CreateToken(customerId: customerId);

        var response = await AuthorizedClient(token).GetAsync(Route);

        var dto = await response.Content.ReadFromJsonAsync<LastPayoutDto>();
        Assert.Equal("CLM-NEW", dto!.ClaimNumber);
    }

    [Fact]
    public async Task Get_RejectedPayoutOnly_ReturnsNotFound()
    {
        var customerId = Guid.NewGuid();
        SeedPayout(
            customerId, "CLM-REJ", 100m, "PLN", DateTime.UtcNow, ClaimPayoutStatus.Rejected);
        var token = JwtTestWebApplicationFactory.CreateToken(customerId: customerId);

        var response = await AuthorizedClient(token).GetAsync(Route);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_DoesNotLeakOtherCustomersPayout()
    {
        var ownerCustomerId = Guid.NewGuid();
        var requestingCustomerId = Guid.NewGuid();
        SeedPayout(ownerCustomerId, "CLM-OWNER", 500m, "PLN", DateTime.UtcNow);
        var token = JwtTestWebApplicationFactory.CreateToken(customerId: requestingCustomerId);

        var response = await AuthorizedClient(token).GetAsync(Route);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_TokenWithoutCustomerIdOrSubject_ReturnsForbiddenCrossCustomer()
    {
        var token = JwtTestWebApplicationFactory.CreateToken(customerId: null, includeSubject: false);

        var response = await AuthorizedClient(token).GetAsync(Route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
        Assert.Equal("FORBIDDEN_CROSS_CUSTOMER", problem!.Title);
    }

    [Fact]
    public async Task Get_TokenWithNonGuidCustomerId_ReturnsForbiddenCrossCustomer()
    {
        var token = JwtTestWebApplicationFactory.CreateToken(rawCustomerIdClaim: "not-a-guid");

        var response = await AuthorizedClient(token).GetAsync(Route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_SubjectClaimFallback_IsHonoredWhenCustomerIdClaimMissing()
    {
        var customerId = Guid.NewGuid();
        SeedPayout(customerId, "CLM-SUB", 42m, "PLN", DateTime.UtcNow);
        var token = JwtTestWebApplicationFactory.CreateToken(customerId: null, subject: customerId);

        var response = await AuthorizedClient(token).GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_NoAuthorizationHeader_ReturnsAuthRequired()
    {
        var response = await _client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("AUTH_REQUIRED", body);
    }

    [Fact]
    public async Task Get_ExpiredToken_ReturnsUnauthorized()
    {
        var token = JwtTestWebApplicationFactory.CreateToken(
            customerId: Guid.NewGuid(), lifetime: TimeSpan.FromMinutes(-10));

        var response = await AuthorizedClient(token).GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_TokenSignedWithWrongKey_ReturnsUnauthorized()
    {
        var token = JwtTestWebApplicationFactory.CreateToken(
            customerId: Guid.NewGuid(), signingKeyOverride: "a-completely-different-signing-key-not-configured!!");

        var response = await AuthorizedClient(token).GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_TokenWithWrongAudience_ReturnsUnauthorized()
    {
        var token = JwtTestWebApplicationFactory.CreateToken(
            customerId: Guid.NewGuid(), audienceOverride: "some-other-audience");

        var response = await AuthorizedClient(token).GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_RepositoryTimesOut_ReturnsDataSourceTimeout()
    {
        await using var timeoutFactory = new TimeoutInjectingWebApplicationFactory();
        var token = JwtTestWebApplicationFactory.CreateToken(customerId: Guid.NewGuid());
        var client = timeoutFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDto>();
        Assert.Equal("DATA_SOURCE_TIMEOUT", problem!.Title);
    }

    // Documents the shipped default configuration: appsettings.json commits blank
    // Jwt:Issuer/Audience/SigningKey (real values are meant to come from env/user-secrets).
    // If nothing supplies them — e.g. a fresh environment before secrets are provisioned —
    // the contract still promises 401 AUTH_REQUIRED for an unauthenticated request. This runs
    // against the *unmodified* factory (no test-only Jwt override) to verify that promise holds.
    [Fact]
    public async Task Get_NoAuthHeader_DefaultUnconfiguredJwtSettings_StillReturnsAuthRequired()
    {
        await using var defaultFactory = new WebApplicationFactory<Program>();
        var client = defaultFactory.CreateClient();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public sealed class JwtTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        public const string Issuer = "policyplatform-e2e-tests";
        public const string Audience = "policyplatform-e2e-tests-audience";
        public const string SigningKey = "e2e-tests-only-signing-key-never-use-in-production-32b";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = Issuer,
                    ["Jwt:Audience"] = Audience,
                    ["Jwt:SigningKey"] = SigningKey,
                });
            });
        }

        public static string CreateToken(
            Guid? customerId = null,
            Guid? subject = null,
            bool includeSubject = true,
            string? rawCustomerIdClaim = null,
            TimeSpan? lifetime = null,
            string? signingKeyOverride = null,
            string? audienceOverride = null)
        {
            var header = new Dictionary<string, object> { ["alg"] = "HS256", ["typ"] = "JWT" };
            var now = DateTimeOffset.UtcNow;
            var exp = now.Add(lifetime ?? TimeSpan.FromMinutes(5));

            var payload = new Dictionary<string, object>
            {
                ["iss"] = Issuer,
                ["aud"] = audienceOverride ?? Audience,
                ["iat"] = now.ToUnixTimeSeconds(),
                ["exp"] = exp.ToUnixTimeSeconds(),
            };

            if (rawCustomerIdClaim is not null)
            {
                payload["customerId"] = rawCustomerIdClaim;
            }
            else if (customerId is { } id)
            {
                payload["customerId"] = id.ToString();
            }

            if (includeSubject)
            {
                payload["sub"] = (subject ?? customerId ?? Guid.NewGuid()).ToString();
            }

            var headerSegment = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
            var payloadSegment = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
            var unsigned = $"{headerSegment}.{payloadSegment}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKeyOverride ?? SigningKey));
            var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsigned));

            return $"{unsigned}.{Base64UrlEncode(signature)}";
        }

        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private sealed class TimeoutInjectingWebApplicationFactory : JwtTestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IClaimPayoutRepository>();
                services.AddSingleton<IClaimPayoutRepository, TimeoutClaimPayoutRepository>();
            });
        }

        private sealed class TimeoutClaimPayoutRepository : IClaimPayoutRepository
        {
            public Task<ClaimPayout?> GetLastPaidForCustomerAsync(Guid customerId, CancellationToken ct = default) =>
                throw new TimeoutException("Simulated data source timeout for e2e test.");
        }
    }
}
