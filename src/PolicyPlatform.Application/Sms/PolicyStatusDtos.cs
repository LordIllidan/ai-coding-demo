using PolicyPlatform.Domain.Policies;

namespace PolicyPlatform.Application.Sms;

public sealed record PolicyStatusRequest(string PolicyNumber, string Pesel);

public sealed record PolicyStatusReply(
    Guid RequestId,
    SmsDecisionCode DecisionCode,
    SmsReplyCode ReplyCode,
    string ReplyText,
    PolicyStatus? PolicyStatusCode,
    string? PolicyStatusLabel);
