namespace PolicyPlatform.Domain.LoginHistory;

/// <summary>Kind of client device a login was recorded from.</summary>
public enum DeviceType
{
    /// <summary>Mobile phone client.</summary>
    PHONE,

    /// <summary>Tablet client.</summary>
    TABLET,

    /// <summary>Browser-based web client.</summary>
    WEB,

    /// <summary>Device type could not be determined.</summary>
    UNKNOWN
}
