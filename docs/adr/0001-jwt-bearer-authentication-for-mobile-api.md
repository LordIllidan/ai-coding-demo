# 1. JWT bearer authentication for the mobile API

## Status

Accepted

## Context

`PolicyPlatform.Api` had no authentication middleware — every controller was open. The
mobile "last payout" endpoint (`GET /api/mobile/me/claims/last-payout`) must resolve the
calling customer strictly from the caller's identity: the request carries no `customerId`,
`policyId`, or `claimId` in path/query/body, so there is no way to authorize or scope the
read without a verified identity attached to the request.

## Decision

Add ASP.NET Core JWT bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`)
to `PolicyPlatform.Api`:

- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` validates
  issuer, audience, lifetime, and signing key (`Jwt:Issuer` / `Jwt:Audience` /
  `Jwt:SigningKey` configuration, populated via environment/user-secrets — no secret
  committed, same pattern as the DB connection string).
- A custom `OnChallenge` handler returns a JSON `AUTH_REQUIRED` problem body instead of the
  framework's empty 401 challenge, so mobile clients get a machine-readable error code.
- `MobileClaimsController` is annotated `[Authorize]` and reads the customer id exclusively
  from the token's `customerId` claim (falling back to `sub`/`NameIdentifier`) — never from
  request input.
- `app.UseAuthentication()` was added ahead of the pre-existing `app.UseAuthorization()`.
- No other controller was annotated `[Authorize]`; existing endpoints remain open. Only new
  mobile "me" endpoints are expected to require a token going forward.

## Consequences

- All future "me"-scoped mobile endpoints should reuse this scheme and the
  claim-derived-identity pattern rather than accepting a client-supplied customer/claim id.
- The API now depends on a configured `Jwt:Issuer`/`Jwt:Audience`/`Jwt:SigningKey` triple to
  issue 200s on `[Authorize]`-protected routes; local/dev/demo environments without that
  configuration will see `[Authorize]` routes reject all tokens (empty signing key never
  validates).
- Token issuance itself (login endpoint, refresh, key rotation) is out of scope for this
  change — only validation of bearer tokens presented to the API.
- Existing unauthenticated endpoints are unaffected, so this is additive, not a breaking
  change to the current API surface.
