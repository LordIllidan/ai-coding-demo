using PolicyPlatform.Application.Sms;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class PolicyStatusRequestValidatorTests
{
    private const string ValidMessageId = "8f14e45f-ceea-467e-9b8b-8f5a3f2b1a1a";
    private const string ValidMsisdn = "+48501234567";
    private const string ValidPesel = "44051401359";

    private static SmsPolicyStatusRequestDto ValidRequest(
        string? messageId = ValidMessageId,
        string? senderMsisdn = ValidMsisdn,
        string? policyNumber = "POL-2026-01",
        string? pesel = ValidPesel,
        string? receivedAt = null)
        => new(messageId, senderMsisdn, policyNumber, pesel, receivedAt);

    [Fact]
    public void Validate_WellFormedRequest_ReturnsValidAndNormalizesPolicyNumber()
    {
        var request = ValidRequest(policyNumber: " pol-2026-01 ");

        var result = PolicyStatusRequestValidator.Validate(request, out var normalized);

        Assert.Equal(PolicyStatusRequestValidationResult.Valid, result);
        Assert.Equal("POL-2026-01", normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingPolicyNumber_ReturnsMissingFields(string? policyNumber)
    {
        var request = ValidRequest(policyNumber: policyNumber);

        var result = PolicyStatusRequestValidator.Validate(request, out var normalized);

        Assert.Equal(PolicyStatusRequestValidationResult.MissingFields, result);
        Assert.Equal(string.Empty, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingPesel_ReturnsMissingFields(string? pesel)
    {
        var request = ValidRequest(pesel: pesel);

        var result = PolicyStatusRequestValidator.Validate(request, out _);

        Assert.Equal(PolicyStatusRequestValidationResult.MissingFields, result);
    }

    [Fact]
    public void Validate_MissingMessageId_ReturnsMissingFields()
    {
        var request = ValidRequest(messageId: null);

        var result = PolicyStatusRequestValidator.Validate(request, out _);

        Assert.Equal(PolicyStatusRequestValidationResult.MissingFields, result);
    }

    [Fact]
    public void Validate_NonGuidMessageId_ReturnsMissingFields()
    {
        var request = ValidRequest(messageId: "not-a-guid");

        var result = PolicyStatusRequestValidator.Validate(request, out _);

        Assert.Equal(PolicyStatusRequestValidationResult.MissingFields, result);
    }

    [Theory]
    [InlineData("501234567")]
    [InlineData("0048501234567")]
    [InlineData("+0501234567")]
    public void Validate_InvalidMsisdn_ReturnsMissingFields(string senderMsisdn)
    {
        var request = ValidRequest(senderMsisdn: senderMsisdn);

        var result = PolicyStatusRequestValidator.Validate(request, out _);

        Assert.Equal(PolicyStatusRequestValidationResult.MissingFields, result);
    }

    [Theory]
    [InlineData("SHORT")]
    [InlineData("this-policy-number-is-definitely-too-long")]
    [InlineData("POL 2026 01")]
    public void Validate_InvalidPolicyNumberFormat_ReturnsInvalidPolicyNumberFormat(string policyNumber)
    {
        var request = ValidRequest(policyNumber: policyNumber);

        var result = PolicyStatusRequestValidator.Validate(request, out _);

        Assert.Equal(PolicyStatusRequestValidationResult.InvalidPolicyNumberFormat, result);
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("44051401350")]
    public void Validate_InvalidPesel_ReturnsInvalidPeselFormat(string pesel)
    {
        var request = ValidRequest(pesel: pesel);

        var result = PolicyStatusRequestValidator.Validate(request, out _);

        Assert.Equal(PolicyStatusRequestValidationResult.InvalidPeselFormat, result);
    }
}
