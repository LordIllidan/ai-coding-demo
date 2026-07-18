using Microsoft.AspNetCore.Http;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Api.ErrorHandling;

/// <summary>Maps last-paid-tranche fetch failures to the shared error envelope contract:
/// { code, message, retryable, correlationId }.</summary>
public static class TrancheFetchErrorMapper
{
    /// <summary>Maps a tranche-fetch exception to its HTTP status code and error envelope.</summary>
    /// <param name="exception">One of the tranche-fetch exceptions declared in
    /// <c>PolicyPlatform.Application.Claims</c>.</param>
    /// <param name="correlationId">Correlation id to embed in the envelope.</param>
    /// <returns>The HTTP status code and the envelope to serialize as the response body.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="exception"/> is not one of the mapped tranche-fetch exception types.</exception>
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
