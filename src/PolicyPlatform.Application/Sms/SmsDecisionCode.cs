namespace PolicyPlatform.Application.Sms;

/// <summary>Top-level outcome of an SMS policy-status request, per the AISDLC-78 contract.</summary>
public enum SmsDecisionCode
{
    /// <summary>Business decision was made and a reply was produced (found or not-verified).</summary>
    Replied,

    /// <summary>Request failed input-shape validation.</summary>
    Rejected,

    /// <summary>Sender exceeded the 5-per-15-minutes rate limit; no lookup was performed.</summary>
    RateLimited,

    /// <summary>A downstream error prevented a decision (e.g. lookup unavailable).</summary>
    Error,
}
