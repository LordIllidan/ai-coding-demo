using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Sms;

/// <summary>Input to <see cref="PolicyStatusRequestService.HandleAsync"/>. Fields are the raw
/// strings as received from the SMS gateway, prior to domain-object parsing.</summary>
/// <param name="PolicyNumber">Policy number as submitted by the sender.</param>
/// <param name="Pesel">PESEL as submitted by the sender.</param>
public sealed record PolicyStatusRequest(string PolicyNumber, string Pesel);

/// <summary>Business-outcome result of a policy-status lookup, per the AISDLC-78 reply contract.
/// <paramref name="PolicyStatusCode"/> and <paramref name="PolicyStatusLabel"/> are populated only
/// when <paramref name="ReplyCode"/> is <see cref="SmsReplyCode.PolicyStatusFound"/>.</summary>
/// <param name="RequestId">Server-generated identifier for this request/reply pair.</param>
/// <param name="DecisionCode">Top-level outcome (replied/rejected/rate-limited/error).</param>
/// <param name="ReplyCode">Fine-grained reason behind <paramref name="DecisionCode"/>.</param>
/// <param name="ReplyText">Human-readable SMS reply text.</param>
/// <param name="PolicyStatusCode">Disclosable policy status, or <see langword="null"/> when not found/verified.</param>
/// <param name="PolicyStatusLabel">Localized label for <paramref name="PolicyStatusCode"/>, or <see langword="null"/>.</param>
public sealed record PolicyStatusReply(
    Guid RequestId,
    SmsDecisionCode DecisionCode,
    SmsReplyCode ReplyCode,
    string ReplyText,
    PolicyStatus? PolicyStatusCode,
    string? PolicyStatusLabel);
