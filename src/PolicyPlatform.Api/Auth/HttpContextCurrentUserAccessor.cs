using System.Security.Claims;
using PolicyPlatform.Application.Abstractions;

namespace PolicyPlatform.Api.Auth;

public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Guid GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var subject = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(subject) || !Guid.TryParse(subject, out var userId))
        {
            throw new UnauthorizedAccessException("JWT is missing a valid user identifier claim.");
        }

        return userId;
    }
}
