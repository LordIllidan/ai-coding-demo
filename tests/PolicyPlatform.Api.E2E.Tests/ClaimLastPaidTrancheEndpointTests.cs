using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Claims;
using PolicyPlatform.Infrastructure.Integration;
using Xunit;

namespace PolicyPlatform.Api.E2E.Tests;

/// <summary>End-to-end HTTP coverage for GET /api/claims/{claimId}/last-paid-tranche
/// (AISDLC-134): claimId-only lookup, the shared error envelope for 401/403/404/503/504,
/// and the "no stale data served on integration failure" contract.</summary>
public sealed class ClaimLastPaidTrancheEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ValidBearerToken = "Bearer test-token";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ClaimLastPaidTrancheEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private sealed record CoverageRequest(string Type, decimal SumInsured, decimal Premium, string Currency = "PLN");

    private sealed record CreatePolicyRequest(
        Guid CustomerId, DateOnly EffectiveDate, DateOnly ExpiryDate, IReadOnlyList<CoverageRequest> Coverages);

    private sealed record CreateCustomerRequest(string FullName, string Email);

    private sealed record CustomerDto(Guid Id, string FullName, string Email);

    private sealed record PolicyDto(Guid Id, string Number, Guid CustomerId);

    private sealed record CreateTheftClaimRequest(
        Guid PolicyId, DateOnly IncidentDate, string Description, string? PoliceReportNumber);

    private sealed record TheftClaimDto(Guid Id, Guid PolicyId);

    private sealed record TrancheDto(
        Guid TrancheId, int TrancheNumber, string Status, DateTimeOffset PaidAt, decimal GrossAmount, string Currency);

    private sealed record LastPaidTrancheResponse(Guid ClaimId, TrancheDto? LastPaidTranche, DateTimeOffset FetchedAt);

    private sealed record ErrorEnvelopeResponse(string Code, string Message, bool Retryable, string CorrelationId);

    private sealed class ThrowingTrancheClient(Exception exception) : ITrancheIntegrationClient
    {
        public Task<LastPaidTrancheDto?> GetLastPaidTrancheAsync(Guid claimId, CancellationToken ct = default)
            => throw exception;
    }

    private static async Task<Guid> CreateClaimAsync(HttpClient client)
    {
        var customerResponse = await client.PostAsJsonAsync(
            "/api/customers", new CreateCustomerRequest("Jan Kowalski", $"jan.{Guid.NewGuid():N}@example.com"));
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var policyResponse = await client.PostAsJsonAsync("/api/policies", new CreatePolicyRequest(
            customer!.Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            [new CoverageRequest("AC", 50000m, 1200m)]));
        policyResponse.EnsureSuccessStatusCode();
        var policy = await policyResponse.Content.ReadFromJsonAsync<PolicyDto>();

        var claimResponse = await client.PostAsJsonAsync("/api/theft-claims", new CreateTheftClaimRequest(
            policy!.Id, new DateOnly(2026, 2, 1), "Kradziez pojazdu.", "KMP/1/2026"));
        claimResponse.EnsureSuccessStatusCode();
        var claim = await claimResponse.Content.ReadFromJsonAsync<TheftClaimDto>();

        return claim!.Id;
    }

    private HttpRequestMessage BuildRequest(Guid claimId, string? bearerToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/claims/{claimId}/last-paid-tranche");
        if (bearerToken is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", bearerToken);
        }

        return request;
    }

    [Fact]
    public async Task Get_MissingAuthorizationHeader_ReturnsInvalidTokenEnvelope()
    {
        var claimId = await CreateClaimAsync(_client);

        var response = await _client.SendAsync(BuildRequest(claimId, bearerToken: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelopeResponse>();
        Assert.Equal("INVALID_TOKEN", body!.Code);
        Assert.False(body.Retryable);
        Assert.False(string.IsNullOrWhiteSpace(body.CorrelationId));
    }

    [Fact]
    public async Task Get_MalformedAuthorizationHeader_ReturnsInvalidTokenEnvelope()
    {
        var claimId = await CreateClaimAsync(_client);

        var response = await _client.SendAsync(BuildRequest(claimId, "Bearer "));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_NonGuidClaimId_ReturnsNotFound()
    {
        var response = await _client.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "/api/claims/not-a-guid/last-paid-tranche")
            {
                Headers = { { "Authorization", ValidBearerToken } },
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownClaimId_ReturnsClaimNotFoundEnvelope()
    {
        var response = await _client.SendAsync(BuildRequest(Guid.NewGuid(), ValidBearerToken));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelopeResponse>();
        Assert.Equal("CLAIM_NOT_FOUND", body!.Code);
        Assert.False(body.Retryable);
    }

    [Fact]
    public async Task Get_KnownClaimWithNoTrancheYet_ReturnsOkWithNullTranche()
    {
        var claimId = await CreateClaimAsync(_client);

        var response = await _client.SendAsync(BuildRequest(claimId, ValidBearerToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LastPaidTrancheResponse>();
        Assert.Equal(claimId, body!.ClaimId);
        Assert.Null(body.LastPaidTranche);
    }

    [Fact]
    public async Task Get_KnownClaimWithSeededTranche_ReturnsOkWithTrancheData()
    {
        var claimId = await CreateClaimAsync(_client);
        var seededTranche = new LastPaidTrancheDto(
            Guid.NewGuid(), 3, "PAID", new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero), 1234.56m, "PLN");
        var trancheClient = (InMemoryTrancheIntegrationClient)_factory.Services.GetRequiredService<ITrancheIntegrationClient>();
        trancheClient.Seed(claimId, seededTranche);

        var response = await _client.SendAsync(BuildRequest(claimId, ValidBearerToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LastPaidTrancheResponse>();
        Assert.NotNull(body!.LastPaidTranche);
        Assert.Equal(seededTranche.TrancheId, body.LastPaidTranche!.TrancheId);
        Assert.Equal(seededTranche.GrossAmount, body.LastPaidTranche.GrossAmount);
        Assert.Equal(seededTranche.Currency, body.LastPaidTranche.Currency);
    }

    [Theory]
    [InlineData(typeof(TrancheServiceUnavailableException), HttpStatusCode.ServiceUnavailable, "TRANCHE_SERVICE_UNAVAILABLE")]
    [InlineData(typeof(TrancheServiceTimeoutException), HttpStatusCode.GatewayTimeout, "TRANCHE_SERVICE_TIMEOUT")]
    public async Task Get_DownstreamFailure_ReturnsRetryableEnvelopeWithoutServingStaleView(
        Type exceptionType, HttpStatusCode expectedStatus, string expectedCode)
    {
        await using var failingFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITrancheIntegrationClient>();
                services.AddSingleton<ITrancheIntegrationClient>(
                    new ThrowingTrancheClient((Exception)Activator.CreateInstance(exceptionType)!));
            }));
        using var failingClient = failingFactory.CreateClient();

        var claimId = await CreateClaimAsync(failingClient);

        // Seed a stale read-model row before the failing call, to prove the endpoint
        // never falls back to it once the downstream fetch throws.
        var staleRow = new ClaimLastPaidTrancheViewRecord(
            claimId, Guid.NewGuid(), 1, "PAID", DateTimeOffset.UtcNow.AddDays(-30), 999m, "PLN",
            DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-30));
        await failingFactory.Services.GetRequiredService<IClaimLastPaidTrancheViewRepository>()
            .UpsertAsync(staleRow);

        var response = await failingClient.SendAsync(BuildRequest(claimId, ValidBearerToken));

        Assert.Equal(expectedStatus, response.StatusCode);
        var rawBody = await response.Content.ReadAsStringAsync();
        var body = System.Text.Json.JsonSerializer.Deserialize<ErrorEnvelopeResponse>(
            rawBody, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Equal(expectedCode, body!.Code);
        Assert.True(body.Retryable);
        Assert.DoesNotContain("999", rawBody);
        Assert.DoesNotContain("lastPaidTranche", rawBody, StringComparison.OrdinalIgnoreCase);
    }
}
