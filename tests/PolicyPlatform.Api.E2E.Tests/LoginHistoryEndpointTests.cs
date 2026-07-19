using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PolicyPlatform.Api.E2E.Tests;

public sealed class LoginHistoryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LoginHistoryEndpointTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task Get_WithoutBearerToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/mobile/me/login-history");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
