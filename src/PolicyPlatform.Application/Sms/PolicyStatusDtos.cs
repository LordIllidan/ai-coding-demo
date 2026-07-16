using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Sms;

/// <summary>Business-level, already input-validated request passed to
/// <see cref="IPolicyStatusRequestHandler"/>: <paramref name="PolicyNumber"/> is trimmed/uppercased
/// and <paramref name="Pesel"/> has a verified checksum.</summary>
/// <param name="PolicyNumber">Normalized policy number (trim + uppercase).</param>
/// <param name="Pesel">Checksum-verified 11-digit PESEL.</param>
public sealed record PolicyStatusRequest(string PolicyNumber, string Pesel);

/// <summary>Business-level outcome of an SMS policy-status request, per the AISDLC-78 contract.</summary>
/// <param name="RequestId">Identifier assigned to this request/reply pair.</param>
/// <param name="DecisionCode">Top-level outcome (replied, rejected, rate-limited, error).</param>
/// <param name="ReplyCode">Fine-grained reply reason.</param>
/// <param name="ReplyText">Human-readable SMS reply text.</param>
/// <param name="PolicyStatusCode">Disclosable policy status when found; otherwise <c>null</c>.</param>
/// <param name="PolicyStatusLabel">Human-readable label for <paramref name="PolicyStatusCode"/>; otherwise <c>null</c>.</param>
public sealed record PolicyStatusReply(
    Guid RequestId,
    SmsDecisionCode DecisionCode,
    SmsReplyCode ReplyCode,
    string ReplyText,
    PolicyStatus? PolicyStatusCode,
    string? PolicyStatusLabel);
