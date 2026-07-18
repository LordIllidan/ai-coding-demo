using Microsoft.AspNetCore.Http;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Api.ErrorHandling;

/// <summary>Maps GET /api/mobile/me/claims/last-payout failures to the shared error envelope
/// contract: { code, message, retryable, correlationId }.</summary>
public static class LastPayoutErrorMapper
{
    public static (int StatusCode, ErrorEnvelope Envelope) Map(Exception exception, string correlationId) =>
        exception switch
        {
            AuthRequiredException => (
                StatusCodes.Status401Unauthorized,
                new ErrorEnvelope("AUTH_REQUIRED", "Access token is missing or invalid.", false, correlationId)),
            ForbiddenCrossCustomerException => (
                StatusCodes.Status403Forbidden,
                new ErrorEnvelope("FORBIDDEN_CROSS_CUSTOMER", "Token is not authorized for this customer's data.", false, correlationId)),
            LastPayoutNotFoundException => (
                StatusCodes.Status404NotFound,
                new ErrorEnvelope("LAST_PAYOUT_NOT_FOUND", "No paid payout was found for this customer.", false, correlationId)),
            DataSourceTimeoutException => (
                StatusCodes.Status503ServiceUnavailable,
                new ErrorEnvelope("DATA_SOURCE_TIMEOUT", "The payout data source timed out. Please try again.", true, correlationId)),
            _ => throw new ArgumentOutOfRangeException(nameof(exception), exception, "Unmapped last-payout fetch exception."),
        };
}
