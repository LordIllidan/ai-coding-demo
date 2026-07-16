namespace PolicyPlatform.Application.Sms;

/// <summary>Wire-level request body for POST /api/v1/sms/policy-status-requests. Fields are kept
/// as raw strings (not value objects) so malformed input can be rejected with the contract's
/// specific error codes instead of a generic model-binding failure.</summary>
public sealed record SmsPolicyStatusRequestDto(
    string? MessageId,
    string? SenderMsisdn,
    string? PolicyNumber,
    string? Pesel,
    string? ReceivedAt);

/// <summary>Wire-level response body shared by every outcome (200/400/422/429/503) of the
/// SMS policy-status endpoint, per the AISDLC-78 contract. decisionCode/replyCode/policyStatusCode
/// are plain SCREAMING_SNAKE_CASE strings rather than serialized enums so the wire format stays
/// stable regardless of the C# enum member names used internally.</summary>
public sealed record SmsPolicyStatusResponseDto(
    string RequestId,
    string DecisionCode,
    string ReplyCode,
    string ReplyText,
    string? PolicyStatusCode,
    string? PolicyStatusLabel)
{
    public static SmsPolicyStatusResponseDto FromReply(PolicyStatusReply reply) => new(
        reply.RequestId.ToString(),
        ToWireDecisionCode(reply.DecisionCode),
        ToWireReplyCode(reply.ReplyCode),
        reply.ReplyText,
        reply.PolicyStatusCode is null ? null : ToWirePolicyStatusCode(reply.PolicyStatusCode.Value),
        reply.PolicyStatusLabel);

    private static string ToWireDecisionCode(SmsDecisionCode code) => code switch
    {
        SmsDecisionCode.Replied => "REPLIED",
        SmsDecisionCode.Rejected => "REJECTED",
        SmsDecisionCode.RateLimited => "RATE_LIMITED",
        SmsDecisionCode.Error => "ERROR",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    private static string ToWireReplyCode(SmsReplyCode code) => code switch
    {
        SmsReplyCode.PolicyStatusFound => "POLICY_STATUS_FOUND",
        SmsReplyCode.PolicyNotVerified => "POLICY_NOT_VERIFIED",
        SmsReplyCode.InvalidInputMissingFields => "INVALID_INPUT_MISSING_FIELDS",
        SmsReplyCode.InvalidPolicyNumberFormat => "INVALID_POLICY_NUMBER_FORMAT",
        SmsReplyCode.InvalidPeselFormat => "INVALID_PESEL_FORMAT",
        SmsReplyCode.SmsRateLimited => "SMS_RATE_LIMITED",
        SmsReplyCode.ServiceUnavailable => "SERVICE_UNAVAILABLE",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    private static string ToWirePolicyStatusCode(Domain.Policies.PolicyStatus status) => status switch
    {
        Domain.Policies.PolicyStatus.Active => "ACTIVE",
        Domain.Policies.PolicyStatus.Expired => "EXPIRED",
        Domain.Policies.PolicyStatus.Cancelled => "CANCELLED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status is not a disclosable policy status."),
    };
}
