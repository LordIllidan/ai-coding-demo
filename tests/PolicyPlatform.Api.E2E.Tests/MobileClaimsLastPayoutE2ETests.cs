using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Claims;
using PolicyPlatform.Domain.Policies;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Api.E2E.Tests;

/// <summary>Covers AISDLC-139: GET /api/mobile/me/claims/last-payout. Signs JWTs locally with
/// the same issuer/audience/key as appsettings.Development.json (the environment
/// WebApplicationFactory hosts under by default) instead of mocking authentication, so the
/// real JwtBearer pipeline in Program.cs is exercised end-to-end.</summary>
public sealed class MobileClaimsLastPayoutE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SigningKey = "dev-only-local-signing-key-not-for-production-use-0123456789";
    private const string Issuer = "policy-platform";
    private const string Audience = "policy-platform-mobile";
    private const string Endpoint = "/api/mobile/me/claims/last-payout";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MobileClaimsLastPayoutE2ETests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string BuildToken(Guid? customerId, IEnumerable<Claim>? extraClaims = null)
    {
        var claims = new List<Claim>();
        if (customerId is not null)
        {
            claims.Add(new Claim("customerId", customerId.Value.ToString()));
        }
        if (extraClaims is not null)
        {
            claims.AddRange(extraClaims);
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void AuthorizeAs(Guid customerId, IEnumerable<Claim>? extraClaims = null)
        => _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BuildToken(customerId, extraClaims));

    private ClaimPayout SeedPaidPayout(
        Guid customerId, string claimNumber, decimal amount, string currency, DateTime paidAt,
        ClaimPayoutStatus status = ClaimPayoutStatus.Paid)
    {
        var payout = ClaimPayout.Create(
            Guid.NewGuid(), Guid.NewGuid(), claimNumber, customerId, new Money(amount, currency), paidAt, status);

        var repository = (InMemoryClaimPayoutRepository)_factory.Services.GetRequiredService<IClaimPayoutRepository>();
        repository.AddAsync(payout).GetAwaiter().GetResult();
        return payout;
    }

    [Fact]
    public async Task Get_NoBearerToken_Returns401AuthRequired()
    {
        var response = await _client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("AUTH_REQUIRED", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Get_TokenWithoutResolvableCustomerId_Returns401AuthRequired()
    {
        // Signed and unexpired, but carries no customerId/NameIdentifier/sub claim the
        // controller can resolve to a customer — must still be treated as unauthenticated,
        // never fall through to querying data for a default/empty customer id.
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BuildToken(customerId: null, extraClaims: [new Claim("scope", "mobile")]));

        var response = await _client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("AUTH_REQUIRED", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Get_ExpiredToken_Returns401AuthRequired()
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256);
        var expiredToken = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("customerId", Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: credentials);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(expiredToken));

        var response = await _client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidCustomerWithNoPaidPayout_Returns404LastPayoutNotFound()
    {
        AuthorizeAs(Guid.NewGuid());

        var response = await _client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("LAST_PAYOUT_NOT_FOUND", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Get_CustomerWithOnlyPendingPayout_Returns404LastPayoutNotFound()
    {
        var customerId = Guid.NewGuid();
        SeedPaidPayout(customerId, "CLM/1/2026", 100m, "PLN", DateTime.UtcNow, ClaimPayoutStatus.Pending);
        AuthorizeAs(customerId);

        var response = await _client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_PaidPayoutExists_Returns200WithContractShape()
    {
        var customerId = Guid.NewGuid();
        var paidAt = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc);
        SeedPaidPayout(customerId, "CLM/2026/00042", 1234.5m, "PLN", paidAt);
        AuthorizeAs(customerId);

        var response = await _client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CLM/2026/00042", body.GetProperty("claimNumber").GetString());
        Assert.Equal("1234.50", body.GetProperty("amount").GetProperty("value").GetString());
        Assert.Equal("PLN", body.GetProperty("amount").GetProperty("currency").GetString());
        Assert.Equal("2026-03-15", body.GetProperty("payoutDate").GetString());
        Assert.True(body.GetProperty("readOnly").GetBoolean());
    }

    [Fact]
    public async Task Get_MultiplePaidPayouts_ReturnsOnlyMostRecentByPaidAt()
    {
        var customerId = Guid.NewGuid();
        SeedPaidPayout(customerId, "CLM/OLD", 100m, "PLN", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SeedPaidPayout(customerId, "CLM/NEWEST", 999m, "PLN", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        SeedPaidPayout(customerId, "CLM/MID", 500m, "PLN", new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc));
        AuthorizeAs(customerId);

        var response = await _client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CLM/NEWEST", body.GetProperty("claimNumber").GetString());
    }

    [Fact]
    public async Task Get_PaidPayoutBelongsToDifferentCustomer_Returns404NotLeaked()
    {
        var ownerCustomerId = Guid.NewGuid();
        var requestingCustomerId = Guid.NewGuid();
        SeedPaidPayout(ownerCustomerId, "CLM/OTHER-CUSTOMER", 5000m, "PLN", DateTime.UtcNow);
        AuthorizeAs(requestingCustomerId);

        var response = await _client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("LAST_PAYOUT_NOT_FOUND", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Get_ReadOnlyEndpoint_DoesNotAcceptWriteVerbs()
    {
        AuthorizeAs(Guid.NewGuid());

        var putResponse = await _client.PutAsJsonAsync(Endpoint, new { });
        var postResponse = await _client.PostAsJsonAsync(Endpoint, new { });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, postResponse.StatusCode);
    }
}
