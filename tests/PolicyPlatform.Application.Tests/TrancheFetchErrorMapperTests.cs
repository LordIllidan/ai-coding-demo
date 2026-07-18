using Microsoft.AspNetCore.Http;
using PolicyPlatform.Api.ErrorHandling;
using PolicyPlatform.Application.Claims;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class TrancheFetchErrorMapperTests
{
    [Theory]
    [InlineData(typeof(InvalidTokenException), StatusCodes.Status401Unauthorized, "INVALID_TOKEN", false)]
    [InlineData(typeof(ClaimAccessDeniedException), StatusCodes.Status403Forbidden, "CLAIM_ACCESS_DENIED", false)]
    [InlineData(typeof(ClaimNotFoundException), StatusCodes.Status404NotFound, "CLAIM_NOT_FOUND", false)]
    [InlineData(typeof(TrancheServiceUnavailableException), StatusCodes.Status503ServiceUnavailable, "TRANCHE_SERVICE_UNAVAILABLE", true)]
    [InlineData(typeof(TrancheServiceTimeoutException), StatusCodes.Status504GatewayTimeout, "TRANCHE_SERVICE_TIMEOUT", true)]
    public void Map_KnownTrancheFetchException_ReturnsExpectedStatusAndEnvelope(
        Type exceptionType, int expectedStatusCode, string expectedCode, bool expectedRetryable)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, args: exceptionType switch
        {
            _ when exceptionType == typeof(ClaimAccessDeniedException) => [Guid.NewGuid()],
            _ when exceptionType == typeof(ClaimNotFoundException) => [Guid.NewGuid()],
            _ => Array.Empty<object>(),
        })!;

        var (statusCode, envelope) = TrancheFetchErrorMapper.Map(exception, "corr-1");

        Assert.Equal(expectedStatusCode, statusCode);
        Assert.Equal(expectedCode, envelope.Code);
        Assert.Equal(expectedRetryable, envelope.Retryable);
        Assert.Equal("corr-1", envelope.CorrelationId);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
    }

    [Fact]
    public void Map_UnmappedExceptionType_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TrancheFetchErrorMapper.Map(new InvalidOperationException("not a tranche-fetch exception"), "corr-2"));
    }
}
