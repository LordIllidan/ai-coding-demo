using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Infrastructure.Persistence;
using Xunit;

namespace PolicyPlatform.Api.E2E.Tests;

// AISDLC-140: GET /api/mobile/me/claims/last-payout — read-only mobile screen backed only by
// the caller's JWT subject (no client-supplied customerId/policyId/claimId anywhere).
public sealed class LastPayoutEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Route = "/api/mobile/me/claims/last-payout";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LastPayoutEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private sealed record ErrorEnvelopeDto(string Code, string Message, bool Retryable, string CorrelationId);

    private sealed record MoneyDto(string Value, string Currency);

    private sealed record LastPayoutResponseDto(string ClaimNumber, MoneyDto Amount, string PayoutDate, bool ReadOnly);

    private void SeedPayout(Guid customerId, CustomerPayout payout)
    {
        var repository = (InMemoryLastPayoutRepository)_factory.Services.GetRequiredService<ILastPayoutRepository>();
        repository.Seed(customerId, payout);
    }

    private static string BuildToken(Guid? sub, string? aud = "mobile-client", DateTimeOffset? exp = null)
    {
        var header = Base64UrlEncode("""{"alg":"none","typ":"JWT"}"""u8.ToArray());

        var payload = new Dictionary<string, object?>();
        if (sub is not null) payload["sub"] = sub.Value.ToString();
        if (aud is not null) payload["aud"] = aud;
        payload["exp"] = (exp ?? DateTimeOffset.UtcNow.AddHours(1)).ToUnixTimeSeconds();

        var payloadJson = JsonSerializer.Serialize(payload);
        var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        return $"{header}.{encodedPayload}.unsigned";
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private HttpRequestMessage AuthorizedRequest(HttpMethod method, string? bearerToken)
    {
        var request = new HttpRequestMessage(method, Route);
        if (bearerToken is not null)
        {
            request.Headers.Add("Authorization", $"Bearer {bearerToken}");
        }
        return request;
    }

    [Fact]
    public async Task Get_NoAuthorizationHeader_ReturnsAuthRequired()
    {
        var response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, bearerToken: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>();
        Assert.Equal("AUTH_REQUIRED", error!.Code);
        Assert.False(error.Retryable);
        Assert.False(string.IsNullOrWhiteSpace(error.CorrelationId));
    }

    [Fact]
    public async Task Get_MalformedToken_ReturnsAuthRequired()
    {
        var response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, "not-a-jwt"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>();
        Assert.Equal("AUTH_REQUIRED", error!.Code);
    }

    [Fact]
    public async Task Get_ExpiredToken_ReturnsAuthRequired()
    {
        var token = BuildToken(Guid.NewGuid(), exp: DateTimeOffset.UtcNow.AddMinutes(-5));

        var response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, token));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>();
        Assert.Equal("AUTH_REQUIRED", error!.Code);
    }

    [Fact]
    public async Task Get_TokenMissingSubject_ReturnsAuthRequired()
    {
        var token = BuildToken(sub: null);

        var response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, token));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>();
        Assert.Equal("AUTH_REQUIRED", error!.Code);
    }

    [Fact]
    public async Task Get_TokenWithWrongAudience_ReturnsForbiddenCrossCustomer()
    {
        var token = BuildToken(Guid.NewGuid(), aud: "some-other-client");

        var response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, token));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>();
        Assert.Equal("FORBIDDEN_CROSS_CUSTOMER", error!.Code);
    }

    [Fact]
    public async Task Get_AuthenticatedCustomerWithNoPaidPayout_ReturnsNotFound()
    {
        var token = BuildToken(Guid.NewGuid());

        var response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, token));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorEnvelopeDto>();
        Assert.Equal("LAST_PAYOUT_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task Get_CustomerWithOnlyNonPaidPayout_ReturnsNotFound()
    {
        var customerId = Guid.NewGuid();
        SeedPayout(customerId, new CustomerPayout("PL-1001", 1500.00m, "PLN", DateTimeOffset.UtcNow.AddDays(-1), "PENDING"));
        var token = BuildToken(customerId);

        var response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, token));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidTokenWithPaidPayout_ReturnsContractShapedBody()
    {
        var customerId = Guid.NewGuid();
        SeedPayout(customerId, new CustomerPayout("PL-2002", 3250.5m, "PLN", DateTimeOffset.Parse("2026-03-10T12:00:00Z"), "PAID"));
        var token = BuildToken(customerId);

        var response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LastPayoutResponseDto>();
        Assert.Equal("PL-2002", body!.ClaimNumber);
        Assert.Equal("3250.50", body.Amount.Value);
        Assert.Equal("PLN", body.Amount.Currency);
        Assert.Equal("2026-03-10", body.PayoutDate);
        Assert.True(body.ReadOnly);
    }

    [Fact]
    public async Task Get_MultiplePaidPayouts_ReturnsOnlyTheLatestByPaidAt()
    {
        var customerId = Guid.NewGuid();
        SeedPayout(customerId, new CustomerPayout("PL-OLD", 100.00m, "PLN", DateTimeOffset.Parse("2025-01-01T00:00:00Z"), "PAID"));
        SeedPayout(customerId, new CustomerPayout("PL-NEW", 200.00m, "PLN", DateTimeOffset.Parse("2026-05-01T00:00:00Z"), "PAID"));
        var token = BuildToken(customerId);

        var response = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, token));

        var body = await response.Content.ReadFromJsonAsync<LastPayoutResponseDto>();
        Assert.Equal("PL-NEW", body!.ClaimNumber);
    }

    [Fact]
    public async Task Get_TwoCustomers_EachOnlySeesOwnPayout()
    {
        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();
        SeedPayout(customerA, new CustomerPayout("PL-A", 111.11m, "PLN", DateTimeOffset.UtcNow.AddDays(-2), "PAID"));
        SeedPayout(customerB, new CustomerPayout("PL-B", 222.22m, "PLN", DateTimeOffset.UtcNow.AddDays(-1), "PAID"));

        var responseA = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, BuildToken(customerA)));
        var responseB = await _client.SendAsync(AuthorizedRequest(HttpMethod.Get, BuildToken(customerB)));

        var bodyA = await responseA.Content.ReadFromJsonAsync<LastPayoutResponseDto>();
        var bodyB = await responseB.Content.ReadFromJsonAsync<LastPayoutResponseDto>();
        Assert.Equal("PL-A", bodyA!.ClaimNumber);
        Assert.Equal("PL-B", bodyB!.ClaimNumber);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task Write_Methods_AreNotHandled_ScreenIsReadOnly(string method)
    {
        var customerId = Guid.NewGuid();
        SeedPayout(customerId, new CustomerPayout("PL-3003", 500.00m, "PLN", DateTimeOffset.UtcNow, "PAID"));
        var token = BuildToken(customerId);

        var response = await _client.SendAsync(AuthorizedRequest(new HttpMethod(method), token));

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            $"expected write method {method} to be rejected by routing, got {response.StatusCode}");
    }
}
