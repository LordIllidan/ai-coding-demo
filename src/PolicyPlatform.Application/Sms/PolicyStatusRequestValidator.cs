using System.Text.RegularExpressions;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Customers;

namespace PolicyPlatform.Application.Sms;

public enum PolicyStatusRequestValidationResult
{
    Valid,
    MissingFields,
    InvalidPolicyNumberFormat,
    InvalidPeselFormat,
}

/// <summary>Input-shape validation for POST /api/v1/sms/policy-status-requests, per the
/// AISDLC-78 contract. Runs before any business decision (see <see cref="IPolicyStatusRequestHandler"/>)
/// or rate limiting, and never has to consider whether the policy/customer actually exist.</summary>
public static partial class PolicyStatusRequestValidator
{
    public static PolicyStatusRequestValidationResult Validate(
        SmsPolicyStatusRequestDto request, out string normalizedPolicyNumber)
    {
        normalizedPolicyNumber = string.Empty;

        if (string.IsNullOrWhiteSpace(request.MessageId)
            || !Guid.TryParse(request.MessageId, out _)
            || string.IsNullOrWhiteSpace(request.SenderMsisdn)
            || !MsisdnPattern().IsMatch(request.SenderMsisdn)
            || string.IsNullOrWhiteSpace(request.PolicyNumber)
            || string.IsNullOrWhiteSpace(request.Pesel))
        {
            return PolicyStatusRequestValidationResult.MissingFields;
        }

        normalizedPolicyNumber = request.PolicyNumber.Trim().ToUpperInvariant();
        if (!PolicyNumberPattern().IsMatch(normalizedPolicyNumber))
        {
            return PolicyStatusRequestValidationResult.InvalidPolicyNumberFormat;
        }

        try
        {
            _ = new Pesel(request.Pesel);
        }
        catch (DomainException)
        {
            return PolicyStatusRequestValidationResult.InvalidPeselFormat;
        }

        return PolicyStatusRequestValidationResult.Valid;
    }

    [GeneratedRegex(@"^[A-Z0-9-]{6,30}$")]
    private static partial Regex PolicyNumberPattern();

    [GeneratedRegex(@"^\+[1-9]\d{6,14}$")]
    private static partial Regex MsisdnPattern();
}
