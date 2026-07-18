namespace PolicyPlatform.Application.Mobile;

/// <summary>Error codes returned in the mobile last-payout error envelope. Shared between the
/// JWT authentication pipeline (AUTH_REQUIRED/FORBIDDEN_CROSS_CUSTOMER) and the application/API
/// layers (the rest), so both sides agree on the same literal strings.</summary>
public static class MobileErrorCodes
{
    /// <summary>No/invalid bearer token. Maps to HTTP 401.</summary>
    public const string AuthRequired = "AUTH_REQUIRED";

    /// <summary>Token is valid but not authorized for the requested data. Maps to HTTP 403.</summary>
    public const string ForbiddenCrossCustomer = "FORBIDDEN_CROSS_CUSTOMER";

    /// <summary>Authenticated customer has no paid claim payout. Maps to HTTP 404.</summary>
    public const string LastPayoutNotFound = "LAST_PAYOUT_NOT_FOUND";

    /// <summary>Data source timed out or is unavailable. Maps to HTTP 503.</summary>
    public const string DataSourceTimeout = "DATA_SOURCE_TIMEOUT";

    /// <summary>Unexpected failure. Maps to HTTP 500.</summary>
    public const string InternalError = "INTERNAL_ERROR";
}

/// <summary>Thrown when the authenticated customer has no paid claim payout. Maps to
/// <see cref="MobileErrorCodes.LastPayoutNotFound"/> / HTTP 404.</summary>
public sealed class LastPayoutNotFoundException() : Exception("No paid claim payout was found for the current customer.");

/// <summary>Thrown when the payout data source times out or is otherwise unavailable. Maps to
/// <see cref="MobileErrorCodes.DataSourceTimeout"/> / HTTP 503.</summary>
public sealed class DataSourceUnavailableException(Exception inner)
    : Exception("The data source timed out or is unavailable.", inner);
