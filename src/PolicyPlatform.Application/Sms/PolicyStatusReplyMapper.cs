using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Sms;

/// <summary>Maps use-case outcomes to the decisionCode/replyCode/replyText contract for the
/// SMS policy-status endpoint (AISDLC-78). Kept separate from PolicyStatusRequestService so the
/// wording/labels can change without touching the decision logic.</summary>
public static class PolicyStatusReplyMapper
{
    public static PolicyStatusReply Found(Guid requestId, PolicyStatus status) => new(
        requestId,
        SmsDecisionCode.Replied,
        SmsReplyCode.PolicyStatusFound,
        ReplyText(SmsReplyCode.PolicyStatusFound),
        status,
        StatusLabel(status));

    /// <summary>Covers both "no such policy" and "policy exists but PESEL does not match" —
    /// the two must be indistinguishable to the caller, so this is the only non-found outcome.</summary>
    public static PolicyStatusReply NotVerified(Guid requestId) => new(
        requestId,
        SmsDecisionCode.Replied,
        SmsReplyCode.PolicyNotVerified,
        ReplyText(SmsReplyCode.PolicyNotVerified),
        null,
        null);

    public static PolicyStatusReply ServiceUnavailable(Guid requestId) => new(
        requestId,
        SmsDecisionCode.Error,
        SmsReplyCode.ServiceUnavailable,
        ReplyText(SmsReplyCode.ServiceUnavailable),
        null,
        null);

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

    public static string StatusLabel(PolicyStatus status) => status switch
    {
        PolicyStatus.Active => "Aktywna",
        PolicyStatus.Expired => "Wygasla",
        PolicyStatus.Cancelled => "Anulowana",
        // Draft policies are not yet issued and must never surface as a "found" result —
        // IPolicyStatusLookupRepository implementations must treat them as no-match.
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status is not a disclosable policy status."),
    };
}
