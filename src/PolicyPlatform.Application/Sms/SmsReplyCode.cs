namespace PolicyPlatform.Application.Sms;

/// <summary>Fine-grained reply reason for an SMS policy-status request, per the AISDLC-78 contract.</summary>
public enum SmsReplyCode
{
    PolicyStatusFound,
    PolicyNotVerified,
    InvalidInputMissingFields,
    InvalidPolicyNumberFormat,
    InvalidPeselFormat,
    SmsRateLimited,
    ServiceUnavailable,
}
