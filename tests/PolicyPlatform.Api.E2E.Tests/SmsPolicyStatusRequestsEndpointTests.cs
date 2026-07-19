using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PolicyPlatform.Api.E2E.Tests;

public sealed class SmsPolicyStatusRequestsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Endpoint = "/api/v1/sms/policy-status-requests";

    // Algorithmically valid test PESEL (fictional person, passes the official checksum).
    private const string ValidPesel = "44051401359";
    private const string InvalidChecksumPesel = "44051401350";

    private readonly HttpClient _client;

    public SmsPolicyStatusRequestsEndpointTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    private sealed record SmsPolicyStatusRequest(
        string? MessageId, string? SenderMsisdn, string? PolicyNumber, string? Pesel, string? ReceivedAt = null);

    private sealed record SmsPolicyStatusResponse(
        string RequestId,
        string DecisionCode,
        string ReplyCode,
        string ReplyText,
        string? PolicyStatusCode,
        string? PolicyStatusLabel);

    private static SmsPolicyStatusRequest ValidRequest(
        string? messageId = null, string? senderMsisdn = "+48600100200",
        string? policyNumber = "POL-123456", string? pesel = ValidPesel)
        => new(messageId ?? Guid.NewGuid().ToString(), senderMsisdn, policyNumber, pesel);

    [Fact]
    public async Task Post_MissingPolicyNumber_ReturnsBadRequestWithMissingFieldsCode()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidRequest(policyNumber: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SmsPolicyStatusResponse>();
        Assert.Equal("REJECTED", body!.DecisionCode);
        Assert.Equal("INVALID_INPUT_MISSING_FIELDS", body.ReplyCode);
        Assert.Null(body.PolicyStatusCode);
    }

    [Fact]
    public async Task Post_MissingPesel_ReturnsBadRequestWithMissingFieldsCode()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidRequest(pesel: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SmsPolicyStatusResponse>();
        Assert.Equal("INVALID_INPUT_MISSING_FIELDS", body!.ReplyCode);
    }

    [Fact]
    public async Task Post_MissingOrInvalidMessageId_ReturnsBadRequestWithMissingFieldsCode()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidRequest(messageId: "not-a-uuid"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SmsPolicyStatusResponse>();
        Assert.Equal("INVALID_INPUT_MISSING_FIELDS", body!.ReplyCode);
    }

    [Fact]
    public async Task Post_InvalidSenderMsisdn_ReturnsBadRequestWithMissingFieldsCode()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidRequest(senderMsisdn: "0600100200"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SmsPolicyStatusResponse>();
        Assert.Equal("INVALID_INPUT_MISSING_FIELDS", body!.ReplyCode);
    }

    [Theory]
    [InlineData("SHORT")]
    [InlineData("THIS-POLICY-NUMBER-IS-WAY-TOO-LONG-1234567890")]
    [InlineData("POL 123456")]
    public async Task Post_InvalidPolicyNumberFormat_ReturnsUnprocessableEntity(string policyNumber)
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidRequest(policyNumber: policyNumber));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SmsPolicyStatusResponse>();
        Assert.Equal("REJECTED", body!.DecisionCode);
        Assert.Equal("INVALID_POLICY_NUMBER_FORMAT", body.ReplyCode);
        Assert.Null(body.PolicyStatusCode);
    }

    [Fact]
    public async Task Post_PolicyNumberLowerCaseAndPadded_IsNormalizedAndPassesFormatCheck()
    {
        // Format validation must run on the trimmed+uppercased value, not the raw input —
        // if this were rejected as INVALID_POLICY_NUMBER_FORMAT, normalization wouldn't be wired in.
        var response = await _client.PostAsJsonAsync(Endpoint, ValidRequest(policyNumber: "  pol-123456  "));

        Assert.NotEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData("1234567890")] // 10 digits, too short
    [InlineData("123456789012")] // 12 digits, too long
    [InlineData("4405140135A")] // non-digit
    [InlineData(InvalidChecksumPesel)] // 11 digits, correct shape, wrong checksum
    public async Task Post_InvalidPeselFormat_ReturnsUnprocessableEntity(string pesel)
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidRequest(pesel: pesel));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SmsPolicyStatusResponse>();
        Assert.Equal("REJECTED", body!.DecisionCode);
        Assert.Equal("INVALID_PESEL_FORMAT", body.ReplyCode);
        Assert.Null(body.PolicyStatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_ReturnsServiceUnavailableUntilDecisionLogicIsWired()
    {
        // AISDLC-86 (actual policy/PESEL lookup) isn't implemented yet — the handler is a
        // safe placeholder that always reports SERVICE_UNAVAILABLE for well-formed input.
        var response = await _client.PostAsJsonAsync(Endpoint, ValidRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SmsPolicyStatusResponse>();
        Assert.Equal("ERROR", body!.DecisionCode);
        Assert.Equal("SERVICE_UNAVAILABLE", body.ReplyCode);
        Assert.True(Guid.TryParse(body.RequestId, out _));
        Assert.Null(body.PolicyStatusCode);
        Assert.Null(body.PolicyStatusLabel);
    }

    [Fact]
    public async Task Post_ValidRequest_NeverReturnsNotFoundRegardlessOfPolicyExistence()
    {
        // Security rule: no 404, and no distinguishing "doesn't exist" from "PESEL mismatch" —
        // every well-formed request must resolve to one of the documented decision outcomes.
        var response = await _client.PostAsJsonAsync(Endpoint, ValidRequest());

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
