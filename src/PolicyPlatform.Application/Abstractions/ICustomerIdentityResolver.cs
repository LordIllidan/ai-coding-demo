namespace PolicyPlatform.Application.Abstractions;

/// <summary>Resolves the authenticated customer's id from a raw Authorization header value.
/// Never accepts a client-supplied customerId — the identity comes exclusively from the
/// bearer token. Throws PolicyPlatform.Application.Claims.AuthRequiredException (401) when the
/// token is missing/malformed/expired, or ForbiddenCrossCustomerException (403) when the token
/// is structurally valid but not scoped to the caller's own customer data.</summary>
public interface ICustomerIdentityResolver
{
    Guid ResolveCustomerId(string? authorizationHeaderValue);
}
