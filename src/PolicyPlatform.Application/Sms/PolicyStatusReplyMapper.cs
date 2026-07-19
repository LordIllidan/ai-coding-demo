using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Sms;

/// <summary>Maps use-case outcomes to the decisionCode/replyCode/replyText contract for the
/// SMS policy-status endpoint (AISDLC-78). Kept separate from the decision logic so the
/// wording/labels can change without touching business rules.</summary>
public static class PolicyStatusReplyMapper
{
    /// <summary>Policy was found and its status is disclosable to the caller.</summary>
    /// <param name="requestId">Identifier assigned to this request/reply pair.</param>
    /// <param name="status">The policy's current status.</param>
    public static PolicyStatusReply Found(Guid requestId, PolicyStatus status) => new(
        requestId,
        SmsDecisionCode.Replied,
        SmsReplyCode.PolicyStatusFound,
        ReplyText(SmsReplyCode.PolicyStatusFound),
        status,
        StatusLabel(status));

    /// <summary>Covers both "no such policy" and "policy exists but PESEL does not match" —
    /// the two must be indistinguishable to the caller, so this is the only non-found outcome.</summary>
    /// <param name="requestId">Identifier assigned to this request/reply pair.</param>
    public static PolicyStatusReply NotVerified(Guid requestId) => new(
        requestId,
        SmsDecisionCode.Replied,
        SmsReplyCode.PolicyNotVerified,
        ReplyText(SmsReplyCode.PolicyNotVerified),
        null,
        null);

    /// <summary>A downstream dependency was unavailable; no decision could be made.</summary>
    /// <param name="requestId">Identifier assigned to this request/reply pair.</param>
    public static PolicyStatusReply ServiceUnavailable(Guid requestId) => new(
        requestId,
        SmsDecisionCode.Error,
        SmsReplyCode.ServiceUnavailable,
        ReplyText(SmsReplyCode.ServiceUnavailable),
        null,
        null);

    /// <summary>Required policyNumber/pesel field(s) were absent from the request body.</summary>
    /// <param name="requestId">Identifier assigned to this request/reply pair.</param>
    public static PolicyStatusReply MissingFields(Guid requestId) => new(
        requestId,
        SmsDecisionCode.Rejected,
        SmsReplyCode.InvalidInputMissingFields,
        ReplyText(SmsReplyCode.InvalidInputMissingFields),
        null,
        null);

    /// <summary>policyNumber failed format validation.</summary>
    /// <param name="requestId">Identifier assigned to this request/reply pair.</param>
    public static PolicyStatusReply InvalidPolicyNumberFormat(Guid requestId) => new(
        requestId,
        SmsDecisionCode.Rejected,
        SmsReplyCode.InvalidPolicyNumberFormat,
        ReplyText(SmsReplyCode.InvalidPolicyNumberFormat),
        null,
        null);

    /// <summary>pesel failed format/checksum validation.</summary>
    /// <param name="requestId">Identifier assigned to this request/reply pair.</param>
    public static PolicyStatusReply InvalidPeselFormat(Guid requestId) => new(
        requestId,
        SmsDecisionCode.Rejected,
        SmsReplyCode.InvalidPeselFormat,
        ReplyText(SmsReplyCode.InvalidPeselFormat),
        null,
        null);

    /// <summary>Sender exceeded the SMS rate limit; no lookup was performed.</summary>
    /// <param name="requestId">Identifier assigned to this request/reply pair.</param>
    public static PolicyStatusReply RateLimited(Guid requestId) => new(
        requestId,
        SmsDecisionCode.RateLimited,
        SmsReplyCode.SmsRateLimited,
        ReplyText(SmsReplyCode.SmsRateLimited),
        null,
        null);

    /// <summary>Polish-language SMS reply text for a given reply code.</summary>
    /// <param name="replyCode">The reply reason to render as text.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for an unrecognized <paramref name="replyCode"/>.</exception>
    public static string ReplyText(SmsReplyCode replyCode) => replyCode switch
    {
        SmsReplyCode.PolicyStatusFound => "Status Twojej polisy zostal znaleziony.",
        SmsReplyCode.PolicyNotVerified => "Nie udalo sie zweryfikowac polisy. Sprawdz numer polisy i PESEL.",
        SmsReplyCode.InvalidInputMissingFields => "Brak wymaganych danych: numer polisy i PESEL sa wymagane.",
        SmsReplyCode.InvalidPolicyNumberFormat => "Nieprawidlowy format numeru polisy.",
        SmsReplyCode.InvalidPeselFormat => "Nieprawidlowy numer PESEL.",
        SmsReplyCode.SmsRateLimited => "Zbyt wiele prob. Sprobuj ponownie pozniej.",
        SmsReplyCode.ServiceUnavailable => "Usluga jest chwilowo niedostepna. Sprobuj ponownie pozniej.",
        _ => throw new ArgumentOutOfRangeException(nameof(replyCode), replyCode, null),
    };

    /// <summary>Human-readable Polish label for a disclosable policy status.</summary>
    /// <param name="status">Policy status to label.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for a non-disclosable status (e.g. Draft).</exception>
    public static string StatusLabel(PolicyStatus status) => status switch
    {
        PolicyStatus.Active => "Aktywna",
        PolicyStatus.Expired => "Wygasla",
        PolicyStatus.Cancelled => "Anulowana",
        // Draft policies are not yet issued and must never surface as a "found" result —
        // lookup implementations must treat them as no-match.
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status is not a disclosable policy status."),
    };
}
