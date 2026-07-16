using System.Text.RegularExpressions;
using PolicyPlatform.Domain.Common;
using PolicyPlatform.Domain.Customers;

namespace PolicyPlatform.Application.Sms;

/// <summary>Outcome of <see cref="PolicyStatusRequestValidator.Validate"/>.</summary>
public enum PolicyStatusRequestValidationResult
{
    /// <summary>Request is well-formed; safe to pass to <see cref="IPolicyStatusRequestHandler"/>.</summary>
    Valid,

    /// <summary>messageId, senderMsisdn, policyNumber, or pesel is missing/blank/malformed as a UUID/MSISDN.</summary>
    MissingFields,

    /// <summary>policyNumber does not match <c>^[A-Z0-9-]{6,30}$</c> after trim + uppercase.</summary>
    InvalidPolicyNumberFormat,

    /// <summary>pesel is not 11 digits or fails the checksum.</summary>
    InvalidPeselFormat,
}

/// <summary>Input-shape validation for POST /api/v1/sms/policy-status-requests, per the
/// AISDLC-78 contract. Runs before any business decision (see <see cref="IPolicyStatusRequestHandler"/>)
/// or rate limiting, and never has to consider whether the policy/customer actually exist.</summary>
public static partial class PolicyStatusRequestValidator
{
    /// <summary>Validates a wire-level request's shape and normalizes its policy number.</summary>
    /// <param name="request">Raw wire-level request body.</param>
    /// <param name="normalizedPolicyNumber">Trimmed/uppercased policy number on success; empty string otherwise.</param>
    /// <returns>The validation outcome.</returns>
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
