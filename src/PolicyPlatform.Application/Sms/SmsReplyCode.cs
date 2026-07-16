namespace PolicyPlatform.Application.Sms;

/// <summary>Fine-grained reply reason for an SMS policy-status request, per the AISDLC-78 contract.</summary>
public enum SmsReplyCode
{
    /// <summary>Policy was found and its status is disclosed in the reply.</summary>
    PolicyStatusFound,

    /// <summary>Policy could not be verified — covers both "no such policy" and "PESEL mismatch"
    /// indistinguishably, per the anti-enumeration security rule.</summary>
    PolicyNotVerified,

    /// <summary>policyNumber or pesel was missing from the request.</summary>
    InvalidInputMissingFields,

    /// <summary>policyNumber failed format validation.</summary>
    InvalidPolicyNumberFormat,

    /// <summary>pesel failed format/checksum validation.</summary>
    InvalidPeselFormat,

    /// <summary>Sender exceeded the SMS rate limit.</summary>
    SmsRateLimited,

    /// <summary>A downstream dependency was unavailable.</summary>
    ServiceUnavailable,
}
