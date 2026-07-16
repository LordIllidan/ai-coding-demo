# E2E coverage report — PR #15 (AISDLC-86)

## Verdict: no e2e coverage added, and none possible on this diff.

PR #15 scope is service layer only (`PolicyStatusRequestService`, `PolicyStatusReplyMapper`,
`Pesel`, enums, repo port). Confirmed by reading current repo state:

- `src/PolicyPlatform.Api/Controllers/` has `ClaimsController`, `CustomersController`,
  `PoliciesController` — no SMS/policy-status controller.
- `src/PolicyPlatform.Api/Program.cs` — no reference to `PolicyPlatform.Application.Sms`,
  no DI registration of `PolicyStatusRequestService` or `IPolicyStatusLookupRepository`.
- No route `POST /api/v1/sms/policy-status-requests` exists anywhere in `src/`.

So there is no HTTP surface to drive with `WebApplicationFactory<Program>`. The endpoint
is explicitly owned by a sibling task (branch
`ai-coding/aisdlc-85-endpoint-post-api-v1-sms-policy-status-requests-...` exists locally,
unmerged) per the PR description's own "deliberately left out of scope" list.

Per task rules, this worker must not modify production/source code — adding a controller
just to make an e2e test possible would be implementing AISDLC-85's job, not testing
AISDLC-86's.

## What already covers this PR's logic

`tests/PolicyPlatform.Application.Tests/PolicyStatusRequestServiceTests.cs` and
`PolicyStatusReplyMapperTests.cs` (added by the unit-test worker, already in this PR)
cover: found/matching-pesel, no-match, bad policy-number format, bad PESEL format,
downstream-unavailable, and requestId freshness — all via direct service calls with a
fake repo. That is the correct altitude for this diff; no controller means no HTTP
concern (status codes, routing, request parsing) exists yet to add e2e assertions on.

## Gap / follow-up

Once AISDLC-85 (endpoint) merges, add e2e coverage there for:
- `POST /api/v1/sms/policy-status-requests` happy path → 200 + `POLICY_STATUS_FOUND`.
- Missing fields → 400 `INVALID_INPUT_MISSING_FIELDS`.
- Bad policy number / PESEL format → 422.
- Unknown policy or PESEL mismatch → same `POLICY_NOT_VERIFIED` body, asserting no
  observable difference (status code, timing-insensitive body) between the two cases —
  this is the actual security property (AISDLC-86's core deliverable) and can only be
  verified end-to-end once the HTTP layer exists.
- Rate limiting (AISDLC-84) → 429 `SMS_RATE_LIMITED` after 5 attempts/15min.
- Downstream failure → 503 `SERVICE_UNAVAILABLE`.

No files changed in `tests/` for this run — nothing to add without touching code outside
this PR's declared scope.
