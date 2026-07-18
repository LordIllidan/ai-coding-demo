# 0001. Unverified JWT payload parsing for mobile customer identity

## Status

Accepted

## Context

`GET /api/mobile/me/claims/last-payout` must identify the calling customer solely from
their bearer token — the request carries no `customerId`/`policyId`/`claimId`. There is no
IdP integration in this codebase yet (no signing-key source, issuer, or JWKS endpoint), so a
real signature/issuer-verified JWT resolver cannot be built now.

`JwtCustomerIdentityResolver` (`src/PolicyPlatform.Infrastructure/Security/JwtCustomerIdentityResolver.cs`)
decodes the base64url JWT payload and reads `sub`, `exp`, and `aud` directly, **without
verifying the token's signature**. It enforces structure (3 segments), expiry, presence of a
GUID `sub`, and a matching `aud` when present, but a token forged with an arbitrary `sub`
would be accepted as-is.

## Decision

Ship the unverified-payload resolver behind the `ICustomerIdentityResolver` abstraction so the
mobile last-payout endpoint can be built and tested end-to-end now, and swap in a real
signature/issuer-verified implementation once IdP integration lands — no other layer depends
on how identity is resolved, only on the interface.

This is acceptable only because the endpoint sits behind whatever perimeter auth/gateway is in
front of this API in current deployments; it is not acceptable as a permanent state for an
endpoint reachable without additional protection.

## Consequences

- Any deployment that exposes this API directly to untrusted callers is vulnerable to
  cross-customer data access via a forged `sub` claim — this must not go to an environment
  without a verifying perimeter (or without a real IdP-backed resolver) in front of it.
- Replacing `JwtCustomerIdentityResolver` with a signature/issuer-verifying implementation is
  a drop-in swap of the `ICustomerIdentityResolver` DI registration in
  `PolicyPlatform.Infrastructure/DependencyInjection.cs` — no Application or Api layer changes
  needed.
- Follow-up work is required to wire a real IdP (signing keys, issuer, JWKS) before this
  endpoint is considered production-ready for untrusted network exposure.
