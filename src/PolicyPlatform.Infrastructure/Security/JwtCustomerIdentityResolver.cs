using System.Text;
using System.Text.Json;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Application.Claims;

namespace PolicyPlatform.Infrastructure.Security;

/// <summary>Resolves the customer id from the unverified payload of a bearer JWT. No
/// signature/issuer verification is performed yet — that requires a real IdP integration and
/// is tracked as follow-up work; this resolver enforces structure, expiry, subject presence,
/// and audience only. Never trusts a customerId supplied anywhere but the token's "sub".</summary>
public sealed class JwtCustomerIdentityResolver : ICustomerIdentityResolver
{
    private const string BearerPrefix = "Bearer ";
    private const string ExpectedAudience = "mobile-client";

    public Guid ResolveCustomerId(string? authorizationHeaderValue)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeaderValue) ||
            !authorizationHeaderValue.StartsWith(BearerPrefix, StringComparison.Ordinal))
        {
            throw new AuthRequiredException();
        }

        var token = authorizationHeaderValue[BearerPrefix.Length..].Trim();
        var segments = token.Split('.');
        if (segments.Length != 3 || segments[0].Length == 0 || segments[1].Length == 0)
        {
            throw new AuthRequiredException();
        }

        JsonElement payload;
        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(segments[1]));
            payload = JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            throw new AuthRequiredException();
        }

        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new AuthRequiredException();
        }

        if (payload.TryGetProperty("exp", out var expElement) &&
            expElement.TryGetInt64(out var exp) &&
            DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow)
        {
            throw new AuthRequiredException();
        }

        if (!payload.TryGetProperty("sub", out var subElement) ||
            subElement.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(subElement.GetString(), out var customerId))
        {
            throw new AuthRequiredException();
        }

        if (payload.TryGetProperty("aud", out var audElement) &&
            audElement.ValueKind == JsonValueKind.String &&
            !string.Equals(audElement.GetString(), ExpectedAudience, StringComparison.Ordinal))
        {
            throw new ForbiddenCrossCustomerException();
        }

        return customerId;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
