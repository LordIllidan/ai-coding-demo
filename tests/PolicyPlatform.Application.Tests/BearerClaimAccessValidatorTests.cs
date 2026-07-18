using PolicyPlatform.Application.Claims;
using PolicyPlatform.Infrastructure.Security;
using Xunit;

namespace PolicyPlatform.Application.Tests;

public class BearerClaimAccessValidatorTests
{
    private readonly BearerClaimAccessValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bearer")]
    [InlineData("Bearer ")]
    [InlineData("Token abc123")]
    [InlineData("bearer abc123")]
    public async Task EnsureAccessAsync_MissingOrMalformedHeader_ThrowsInvalidToken(string? authorizationHeaderValue)
    {
        await Assert.ThrowsAsync<InvalidTokenException>(
            () => _validator.EnsureAccessAsync(authorizationHeaderValue, Guid.NewGuid()));
    }

    [Fact]
    public async Task EnsureAccessAsync_WellFormedBearerToken_CompletesWithoutThrowing()
    {
        await _validator.EnsureAccessAsync("Bearer abc123", Guid.NewGuid());
    }
}
