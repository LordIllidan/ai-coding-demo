using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PolicyPlatform.Api.E2E.Tests;

public sealed class TheftClaimsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TheftClaimsEndpointTests(WebApplicationFactory<Program> factory)
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
        Guid ClaimId, Guid PolicyId, string PoliceReportNumber, string Status, bool NextStepAllowed);

    private sealed record FieldErrorDto(string Field, string Code, string Message);

    private sealed record ValidationErrorDto(string Code, IReadOnlyList<FieldErrorDto> FieldErrors);

    private async Task<Guid> CreateActivePolicyAsync()
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

        return policy!.Id;
    }

    [Fact]
    public async Task Post_MissingPoliceReportNumber_Returns422WithRequiredFieldError()
    {
        var policyId = await CreateActivePolicyAsync();
        var request = new CreateTheftClaimRequest(policyId, new DateOnly(2026, 2, 1), "Kradziez pojazdu.", null);

        var response = await _client.PostAsJsonAsync("/api/theft-claims", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ValidationErrorDto>();
        Assert.Equal("VALIDATION_ERROR", error!.Code);
        var fieldError = Assert.Single(error.FieldErrors);
        Assert.Equal("policeReportNumber", fieldError.Field);
        Assert.Equal("POLICE_REPORT_NUMBER_REQUIRED", fieldError.Code);
    }

    [Fact]
    public async Task Post_BlankPoliceReportNumber_Returns422WithRequiredFieldError()
    {
        var policyId = await CreateActivePolicyAsync();
        var request = new CreateTheftClaimRequest(policyId, new DateOnly(2026, 2, 1), "Kradziez pojazdu.", "   ");

        var response = await _client.PostAsJsonAsync("/api/theft-claims", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ValidationErrorDto>();
        Assert.Equal("VALIDATION_ERROR", error!.Code);
        var fieldError = Assert.Single(error.FieldErrors);
        Assert.Equal("policeReportNumber", fieldError.Field);
        Assert.Equal("POLICE_REPORT_NUMBER_REQUIRED", fieldError.Code);
    }

    [Theory]
    [InlineData("AB")] // below minimum length (3 chars)
    [InlineData("KM@123")] // disallowed character '@'
    [InlineData("!ABCDEF")] // disallowed leading character
    public async Task Post_InvalidFormatPoliceReportNumber_Returns422WithFormatFieldError(string policeReportNumber)
    {
        var policyId = await CreateActivePolicyAsync();
        var request = new CreateTheftClaimRequest(
            policyId, new DateOnly(2026, 2, 1), "Kradziez pojazdu.", policeReportNumber);

        var response = await _client.PostAsJsonAsync("/api/theft-claims", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ValidationErrorDto>();
        Assert.Equal("VALIDATION_ERROR", error!.Code);
        var fieldError = Assert.Single(error.FieldErrors);
        Assert.Equal("policeReportNumber", fieldError.Field);
        Assert.Equal("POLICE_REPORT_NUMBER_INVALID_FORMAT", fieldError.Code);
    }

    [Fact]
    public async Task Post_UnknownPolicy_ReturnsBadRequest()
    {
        var request = new CreateTheftClaimRequest(
            Guid.NewGuid(), new DateOnly(2026, 2, 1), "Kradziez pojazdu.", "KMP/123/2026");

        var response = await _client.PostAsJsonAsync("/api/theft-claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_ReturnsCreatedAndRetrievableClaim()
    {
        var policyId = await CreateActivePolicyAsync();
        var request = new CreateTheftClaimRequest(policyId, new DateOnly(2026, 2, 1), "Kradziez pojazdu.", "kmp/123/2026");

        var createResponse = await _client.PostAsJsonAsync("/api/theft-claims", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TheftClaimDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.ClaimId);
        Assert.Equal(policyId, created.PolicyId);
        Assert.Equal("KMP/123/2026", created.PoliceReportNumber);
        Assert.Equal("ACCEPTED", created.Status);
        Assert.True(created.NextStepAllowed);
        Assert.NotNull(createResponse.Headers.Location);

        var getResponse = await _client.GetAsync(createResponse.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<TheftClaimDto>();
        Assert.Equal(created.ClaimId, fetched!.ClaimId);
        Assert.Equal("KMP/123/2026", fetched.PoliceReportNumber);
        Assert.Equal("ACCEPTED", fetched.Status);
        Assert.True(fetched.NextStepAllowed);
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/theft-claims/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
