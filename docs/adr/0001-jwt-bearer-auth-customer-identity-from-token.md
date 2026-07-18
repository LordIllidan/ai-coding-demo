# 0001. JWT bearer authentication, customer identity sourced only from the token

## Status
Accepted

## Context
AISDLC-139 adds the first authenticated endpoint in this API
(`GET /api/mobile/me/claims/last-payout`). Previously the app had no authentication
pipeline. The endpoint contract requires that the responding customer's identity come
only from the caller's credentials — the request must not (and cannot) carry a
customerId/policyId/claimId, so no layer can be tricked into returning another
customer's data via a client-supplied identifier.

## Decision
- Use ASP.NET Core JWT bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`),
  configured in `Program.cs` from `Jwt:Issuer` / `Jwt:Audience` / `Jwt:SigningKey`.
- Controllers resolve the acting customer id exclusively from JWT claims
  (`customerId`, falling back to `ClaimTypes.NameIdentifier`/`sub`) — never from a
  route, query string, or request body. See `MobileClaimsController.TryGetCustomerId`.
- Auth failures are translated to the mobile error envelope (`{ error, message }`)
  via custom `JwtBearerEvents.OnChallenge`/`OnForbidden` handlers, returning
  `AUTH_REQUIRED` (401) / `FORBIDDEN_CROSS_CUSTOMER` (403) instead of ASP.NET Core's
  default empty response bodies.

## Consequences
- All future authenticated endpoints in this API should follow the same pattern:
  identity from token claims only, no client-supplied identifiers for "my own data"
  endpoints, and error responses mapped to the shared `{ error, message }` envelope.
- `Jwt:SigningKey` must be configured (`appsettings.Development.json` holds a
  dev-only placeholder); the app fails fast at startup if it is missing.
- Introduces a new package dependency (`Microsoft.AspNetCore.Authentication.JwtBearer`)
  and couples endpoint authorization to claim names (`customerId`/`NameIdentifier`/`sub`)
  that any future token issuer must populate consistently.
