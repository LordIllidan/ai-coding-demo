namespace PolicyPlatform.Application.Abstractions;

/// <summary>Resolves the authenticated customer's id from a raw Authorization header value.
/// Never accepts a client-supplied customerId — the identity comes exclusively from the
/// bearer token. Throws PolicyPlatform.Application.Claims.AuthRequiredException (401) when the
/// token is missing/malformed/expired, or ForbiddenCrossCustomerException (403) when the token
/// is structurally valid but not scoped to the caller's own customer data.</summary>
public interface ICustomerIdentityResolver
{
    /// <summary>Resolves the authenticated customer's id from the raw Authorization header value.</summary>
    /// <param name="authorizationHeaderValue">Raw value of the incoming Authorization header, e.g. "Bearer &lt;token&gt;".</param>
    /// <returns>The customer id extracted from the token's subject claim.</returns>
    /// <exception cref="PolicyPlatform.Application.Claims.AuthRequiredException">The header is missing or the token is malformed/expired.</exception>
    /// <exception cref="PolicyPlatform.Application.Claims.ForbiddenCrossCustomerException">The token is structurally valid but not scoped to the caller's own customer data.</exception>
    Guid ResolveCustomerId(string? authorizationHeaderValue);
}
