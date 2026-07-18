using Microsoft.AspNetCore.Http;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Api.ErrorHandling;

/// <summary>Maps last-paid-tranche fetch failures to the shared error envelope contract:
/// { code, message, retryable, correlationId }.</summary>
public static class TrancheFetchErrorMapper
{
    public static (int StatusCode, ErrorEnvelope Envelope) Map(Exception exception, string correlationId) =>
        exception switch
        {
            InvalidTokenException => (
                StatusCodes.Status401Unauthorized,
                new ErrorEnvelope("INVALID_TOKEN", "Access token is missing or expired.", false, correlationId)),
            ClaimAccessDeniedException => (
                StatusCodes.Status403Forbidden,
                new ErrorEnvelope("CLAIM_ACCESS_DENIED", "You do not have access to this claim.", false, correlationId)),
            ClaimNotFoundException => (
                StatusCodes.Status404NotFound,
                new ErrorEnvelope("CLAIM_NOT_FOUND", "Claim was not found.", false, correlationId)),
            TrancheServiceUnavailableException => (
                StatusCodes.Status503ServiceUnavailable,
                new ErrorEnvelope("TRANCHE_SERVICE_UNAVAILABLE", "Tranche service is temporarily unavailable.", true, correlationId)),
            TrancheServiceTimeoutException => (
                StatusCodes.Status504GatewayTimeout,
                new ErrorEnvelope("TRANCHE_SERVICE_TIMEOUT", "Tranche service did not respond in time.", true, correlationId)),
            _ => throw new ArgumentOutOfRangeException(nameof(exception), exception, "Unmapped tranche fetch exception."),
        };
}
