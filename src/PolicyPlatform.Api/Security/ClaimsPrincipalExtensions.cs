using System.Security.Claims;

namespace PolicyPlatform.Api.Security;

public static class ClaimsPrincipalExtensions
{
    private static readonly string[] UserIdClaimTypes = ["sub", ClaimTypes.NameIdentifier, "accountId"];

    /// <summary>User identity for /me endpoints comes exclusively from the JWT (sub/accountId
    /// claim) — callers must never accept a user id from path/query/body.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        foreach (var claimType in UserIdClaimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (Guid.TryParse(value, out var userId))
            {
                return userId;
            }
        }

        return null;
    }
}
