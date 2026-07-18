namespace PolicyPlatform.Application.Mobile;

/// <summary>Error codes returned in the mobile last-payout error envelope. Shared between the
/// JWT authentication pipeline (AUTH_REQUIRED/FORBIDDEN_CROSS_CUSTOMER) and the application/API
/// layers (the rest), so both sides agree on the same literal strings.</summary>
public static class MobileErrorCodes
{
    public const string AuthRequired = "AUTH_REQUIRED";
    public const string ForbiddenCrossCustomer = "FORBIDDEN_CROSS_CUSTOMER";
    public const string LastPayoutNotFound = "LAST_PAYOUT_NOT_FOUND";
    public const string DataSourceTimeout = "DATA_SOURCE_TIMEOUT";
    public const string InternalError = "INTERNAL_ERROR";
}

public sealed class LastPayoutNotFoundException() : Exception("No paid claim payout was found for the current customer.");

public sealed class DataSourceUnavailableException(Exception inner)
    : Exception("The data source timed out or is unavailable.", inner);
