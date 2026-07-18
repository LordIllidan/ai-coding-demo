using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PolicyPlatform.Api.E2E.Tests;

public sealed class PayoutsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PayoutsEndpointTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    private sealed record CoverageRequest(string Type, decimal SumInsured, decimal Premium, string Currency = "PLN");

    private sealed record CreatePolicyRequest(
        Guid CustomerId, DateOnly EffectiveDate, DateOnly ExpiryDate, IReadOnlyList<CoverageRequest> Coverages);

    private sealed record CreateCustomerRequest(string FullName, string Email);

    private sealed record CustomerDto(Guid Id, string FullName, string Email);

    private sealed record PolicyDto(Guid Id, string Number, Guid CustomerId);

    private sealed record CreateTheftClaimRequest(
        Guid PolicyId, DateOnly IncidentDate, string Description, string? PoliceReportNumber);

    private sealed record TheftClaimDto(
        Guid Id, Guid PolicyId, DateOnly IncidentDate, string Description, string PoliceReportNumber, DateTime ReportedAt);

    private async Task<Guid> CreateTheftClaimAsync()
    {
        var customerResponse = await _client.PostAsJsonAsync(
            "/api/customers", new CreateCustomerRequest("Jan Kowalski", $"jan.{Guid.NewGuid():N}@example.com"));
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var policyResponse = await _client.PostAsJsonAsync("/api/policies", new CreatePolicyRequest(
            customer!.Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            [new CoverageRequest("AC", 50000m, 1200m)]));
        policyResponse.EnsureSuccessStatusCode();
        var policy = await policyResponse.Content.ReadFromJsonAsync<PolicyDto>();

        var claimResponse = await _client.PostAsJsonAsync("/api/theft-claims", new CreateTheftClaimRequest(
            policy!.Id, new DateOnly(2026, 2, 1), "Kradziez pojazdu.", "KMP/123/2026"));
        claimResponse.EnsureSuccessStatusCode();
        var claim = await claimResponse.Content.ReadFromJsonAsync<TheftClaimDto>();

        return claim!.Id;
    }

    private static HttpRequestMessage AuthorizedGet(Guid claimId, string token = "test-token")
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get, $"/api/v1/claims/{claimId}/payouts/last-paid-installment");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task Get_NoAuthorizationHeader_ReturnsUnauthorizedWithCode()
    {
        var claimId = await CreateTheftClaimAsync();

        var response = await _client.GetAsync($"/api/v1/claims/{claimId}/payouts/last-paid-installment");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("UNAUTHORIZED", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_MalformedAuthorizationHeader_ReturnsUnauthorized()
    {
        var claimId = await CreateTheftClaimAsync();
        var request = new HttpRequestMessage(
            HttpMethod.Get, $"/api/v1/claims/{claimId}/payouts/last-paid-installment");
        request.Headers.TryAddWithoutValidation("Authorization", "Basic dXNlcjpwYXNz");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_BlankBearerToken_ReturnsUnauthorized()
    {
        var claimId = await CreateTheftClaimAsync();
        var request = new HttpRequestMessage(
            HttpMethod.Get, $"/api/v1/claims/{claimId}/payouts/last-paid-installment");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer    ");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownClaimId_ReturnsNotFoundWithCode()
    {
        var response = await _client.SendAsync(AuthorizedGet(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CLAIM_NOT_FOUND", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_KnownClaimWithNoPayouts_ReturnsNoPayoutScreenState()
    {
        var claimId = await CreateTheftClaimAsync();

        var response = await _client.SendAsync(AuthorizedGet(claimId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(claimId, body.GetProperty("claimId").GetGuid());
        Assert.Equal("NO_PAYOUT", body.GetProperty("screenState").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("lastPaidInstallment").ValueKind);
        Assert.False(body.GetProperty("canEdit").GetBoolean());
    }

    [Fact]
    public async Task Get_NoPayoutResponse_OmitsClaimNumberField()
    {
        var claimId = await CreateTheftClaimAsync();

        var response = await _client.SendAsync(AuthorizedGet(claimId));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(
            body.TryGetProperty("claimNumber", out _),
            "claimNumber must be absent (not null) from the payload unless screenState is PAID.");
    }

    [Fact]
    public async Task Get_InvalidClaimIdFormat_ReturnsNotFoundRoute()
    {
        var response = await _client.GetAsync("/api/v1/claims/not-a-guid/payouts/last-paid-installment");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
