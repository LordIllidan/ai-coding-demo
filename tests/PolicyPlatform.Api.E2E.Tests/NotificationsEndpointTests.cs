using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Notifications;
using Xunit;

namespace PolicyPlatform.Api.E2E.Tests;

// AISDLC-156: contract + regression coverage for the mobile unread-notifications counter
// (counter/list/read) against the real HTTP pipeline via WebApplicationFactory<Program>.
// The API exposes no notification-creation endpoint yet, so test data is seeded directly
// through the DI-resolved INotificationRepository singleton (the same seam the in-memory
// store itself uses) rather than mocking any part of the HTTP surface under test.
public sealed class NotificationsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CounterUrl = "/api/mobile/v1/notifications/counter";
    private const string ListUrl = "/api/mobile/v1/notifications";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public NotificationsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private sealed record ApiError(string Code, string Message);

    private sealed record CounterResponse(int UnreadCount, DateTime CalculatedAt);

    private sealed record ListItemResponse(Guid Id, string Title, string Body, string Type, DateTime CreatedAt, bool IsRead, DateTime? ReadAt);

    private sealed record ListResponse(List<ListItemResponse> Items, string? NextCursor);

    private sealed record ReadResultResponse(Guid NotificationId, bool IsRead, DateTime ReadAt, int UnreadCount);

    private static string PatchReadUrl(Guid notificationId) => $"{ListUrl}/{notificationId}/read";

    private async Task<Notification> SeedNotificationAsync(Guid userId, DateTime? createdAt = null)
    {
        var repository = _factory.Services.GetRequiredService<INotificationRepository>();
        var notification = Notification.Create(
            Guid.NewGuid(), userId, "Nowa polisa gotowa", "Szczegóły w aplikacji.", "policy",
            createdAt ?? DateTime.UtcNow);
        await repository.SaveAsync(notification);
        return notification;
    }

    private string CreateToken(
        Guid? subject = null,
        string? rawSubClaim = null,
        TimeSpan? expiresIn = null,
        string? signingKey = null,
        string? issuer = null,
        string? audience = null)
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var key = signingKey ?? config["Jwt:SigningKey"]!;
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, rawSubClaim ?? (subject ?? Guid.NewGuid()).ToString()) };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer ?? config["Jwt:Issuer"],
            audience: audience ?? config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromMinutes(30)),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string? token)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    // --- Authentication contract (401 UNAUTHENTICATED) ---

    [Fact]
    public async Task Counter_NoAuthorizationHeader_ReturnsUnauthenticated()
    {
        var response = await _client.SendAsync(Authed(HttpMethod.Get, CounterUrl, token: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("UNAUTHENTICATED", error!.Code);
    }

    [Fact]
    public async Task Counter_ExpiredToken_ReturnsUnauthenticated()
    {
        var token = CreateToken(expiresIn: TimeSpan.FromMinutes(-5));

        var response = await _client.SendAsync(Authed(HttpMethod.Get, CounterUrl, token));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Counter_WrongSigningKey_ReturnsUnauthenticated()
    {
        var token = CreateToken(signingKey: "wrong-signing-key-totally-different-from-the-real-dev-key-000");

        var response = await _client.SendAsync(Authed(HttpMethod.Get, CounterUrl, token));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Counter_TokenWithNonGuidSubjectClaim_ReturnsUnauthenticated()
    {
        var token = CreateToken(rawSubClaim: "not-a-guid");

        var response = await _client.SendAsync(Authed(HttpMethod.Get, CounterUrl, token));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal("UNAUTHENTICATED", error!.Code);
    }

    // --- GET /counter ---

    [Fact]
    public async Task Counter_NoNotifications_ReturnsExplicitZeroNotOmitted()
    {
        var token = CreateToken(Guid.NewGuid());

        var response = await _client.SendAsync(Authed(HttpMethod.Get, CounterUrl, token));
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"unreadCount\":0", raw);
        var body = await response.Content.ReadFromJsonAsync<CounterResponse>();
        Assert.Equal(0, body!.UnreadCount);
        Assert.DoesNotContain("userId", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Counter_WithUnreadNotifications_ReturnsMatchingCount()
    {
        var userId = Guid.NewGuid();
        await SeedNotificationAsync(userId);
        await SeedNotificationAsync(userId);
        var token = CreateToken(userId);

        var response = await _client.SendAsync(Authed(HttpMethod.Get, CounterUrl, token));
        var body = await response.Content.ReadFromJsonAsync<CounterResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, body!.UnreadCount);
    }

    [Fact]
    public async Task Counter_IsolatedPerUser_DoesNotLeakOtherUsersNotifications()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        await SeedNotificationAsync(owner);
        var token = CreateToken(other);

        var response = await _client.SendAsync(Authed(HttpMethod.Get, CounterUrl, token));
        var body = await response.Content.ReadFromJsonAsync<CounterResponse>();

        Assert.Equal(0, body!.UnreadCount);
    }

    // --- GET /notifications (unread list) ---

    [Fact]
    public async Task List_DefaultsToUnreadOnly_WithContractShape()
    {
        var userId = Guid.NewGuid();
        var notification = await SeedNotificationAsync(userId);
        var token = CreateToken(userId);

        var response = await _client.SendAsync(Authed(HttpMethod.Get, ListUrl, token));
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(body!.Items);
        Assert.Equal(notification.Id, item.Id);
        Assert.False(item.IsRead);
        Assert.Null(item.ReadAt);
    }

    [Fact]
    public async Task List_ExplicitReadFalse_IsAccepted()
    {
        var userId = Guid.NewGuid();
        await SeedNotificationAsync(userId);
        var token = CreateToken(userId);

        var response = await _client.SendAsync(Authed(HttpMethod.Get, $"{ListUrl}?read=false", token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReadTrue_ReturnsValidationError()
    {
        var token = CreateToken(Guid.NewGuid());

        var response = await _client.SendAsync(Authed(HttpMethod.Get, $"{ListUrl}?read=true", token));
        var error = await response.Content.ReadFromJsonAsync<ApiError>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", error!.Code);
    }

    [Fact]
    public async Task List_UnsupportedQueryParameter_ReturnsValidationError()
    {
        var token = CreateToken(Guid.NewGuid());

        var response = await _client.SendAsync(Authed(HttpMethod.Get, $"{ListUrl}?includeRead=true", token));
        var error = await response.Content.ReadFromJsonAsync<ApiError>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", error!.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task List_NonPositiveLimit_ReturnsValidationError(int limit)
    {
        var token = CreateToken(Guid.NewGuid());

        var response = await _client.SendAsync(Authed(HttpMethod.Get, $"{ListUrl}?limit={limit}", token));
        var error = await response.Content.ReadFromJsonAsync<ApiError>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", error!.Code);
    }

    [Fact]
    public async Task List_PaginatesWithCursor_UntilExhausted()
    {
        var userId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;
        // Distinct, strictly increasing timestamps so cursor ordering (createdAt desc, id desc) is deterministic.
        await SeedNotificationAsync(userId, baseTime.AddSeconds(1));
        await SeedNotificationAsync(userId, baseTime.AddSeconds(2));
        await SeedNotificationAsync(userId, baseTime.AddSeconds(3));
        var token = CreateToken(userId);

        var firstPage = await _client.SendAsync(Authed(HttpMethod.Get, $"{ListUrl}?limit=2", token));
        var firstBody = await firstPage.Content.ReadFromJsonAsync<ListResponse>();

        Assert.Equal(HttpStatusCode.OK, firstPage.StatusCode);
        Assert.Equal(2, firstBody!.Items.Count);
        Assert.NotNull(firstBody.NextCursor);

        var secondPage = await _client.SendAsync(
            Authed(HttpMethod.Get, $"{ListUrl}?limit=2&cursor={Uri.EscapeDataString(firstBody.NextCursor!)}", token));
        var secondBody = await secondPage.Content.ReadFromJsonAsync<ListResponse>();

        Assert.Equal(HttpStatusCode.OK, secondPage.StatusCode);
        Assert.Single(secondBody!.Items);
        Assert.Null(secondBody.NextCursor);

        var allIds = firstBody.Items.Concat(secondBody.Items).Select(i => i.Id).ToHashSet();
        Assert.Equal(3, allIds.Count);
    }

    // --- PATCH /notifications/{id}/read ---

    [Fact]
    public async Task MarkAsRead_OwnNotification_MarksReadAndDecreasesCounter()
    {
        var userId = Guid.NewGuid();
        var notification = await SeedNotificationAsync(userId);
        await SeedNotificationAsync(userId);
        var token = CreateToken(userId);

        var readResponse = await _client.SendAsync(Authed(HttpMethod.Patch, PatchReadUrl(notification.Id), token));
        var readBody = await readResponse.Content.ReadFromJsonAsync<ReadResultResponse>();

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(notification.Id, readBody!.NotificationId);
        Assert.True(readBody.IsRead);
        Assert.Equal(1, readBody.UnreadCount);

        var counterResponse = await _client.SendAsync(Authed(HttpMethod.Get, CounterUrl, token));
        var counterBody = await counterResponse.Content.ReadFromJsonAsync<CounterResponse>();
        Assert.Equal(1, counterBody!.UnreadCount);

        var listResponse = await _client.SendAsync(Authed(HttpMethod.Get, ListUrl, token));
        var listBody = await listResponse.Content.ReadFromJsonAsync<ListResponse>();
        Assert.DoesNotContain(listBody!.Items, i => i.Id == notification.Id);
    }

    [Fact]
    public async Task MarkAsRead_CalledTwice_IsIdempotentAndReturnsStableUnreadCount()
    {
        var userId = Guid.NewGuid();
        var notification = await SeedNotificationAsync(userId);
        var token = CreateToken(userId);

        var first = await _client.SendAsync(Authed(HttpMethod.Patch, PatchReadUrl(notification.Id), token));
        var firstBody = await first.Content.ReadFromJsonAsync<ReadResultResponse>();

        var second = await _client.SendAsync(Authed(HttpMethod.Patch, PatchReadUrl(notification.Id), token));
        var secondBody = await second.Content.ReadFromJsonAsync<ReadResultResponse>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.True(secondBody!.IsRead);
        Assert.Equal(firstBody!.UnreadCount, secondBody.UnreadCount);
    }

    [Fact]
    public async Task MarkAsRead_InvalidUuid_ReturnsValidationError()
    {
        var token = CreateToken(Guid.NewGuid());

        var response = await _client.SendAsync(Authed(HttpMethod.Patch, $"{ListUrl}/not-a-uuid/read", token));
        var error = await response.Content.ReadFromJsonAsync<ApiError>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", error!.Code);
    }

    [Fact]
    public async Task MarkAsRead_UnknownId_ReturnsNotFound()
    {
        var token = CreateToken(Guid.NewGuid());

        var response = await _client.SendAsync(Authed(HttpMethod.Patch, PatchReadUrl(Guid.NewGuid()), token));
        var error = await response.Content.ReadFromJsonAsync<ApiError>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NOTIFICATION_NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task MarkAsRead_OtherUsersNotification_ReturnsForbiddenAndDoesNotMutateIt()
    {
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var notification = await SeedNotificationAsync(owner);
        var attackerToken = CreateToken(attacker);

        var response = await _client.SendAsync(Authed(HttpMethod.Patch, PatchReadUrl(notification.Id), attackerToken));
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("userId", raw, StringComparison.OrdinalIgnoreCase);

        var ownerToken = CreateToken(owner);
        var counterResponse = await _client.SendAsync(Authed(HttpMethod.Get, CounterUrl, ownerToken));
        var counterBody = await counterResponse.Content.ReadFromJsonAsync<CounterResponse>();
        Assert.Equal(1, counterBody!.UnreadCount);
    }
}
