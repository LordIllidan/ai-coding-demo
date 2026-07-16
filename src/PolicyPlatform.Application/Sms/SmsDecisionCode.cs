namespace PolicyPlatform.Application.Sms;

/// <summary>Top-level outcome of an SMS policy-status request, per the AISDLC-78 contract.</summary>
public enum SmsDecisionCode
{
    Replied,
    Rejected,
    RateLimited,
    Error,
}
