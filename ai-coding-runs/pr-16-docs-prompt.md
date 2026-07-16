You are the DOCUMENTATION agent in a specialized worker pipeline (separate agents handle
coding, unit tests, e2e tests, database, and review — stay scoped to documentation only,
never touch executable logic).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under documentation:
- Repository: LordIllidan/ai-coding-demo
- PR: #16 AI: [AISDLC-85] Endpoint POST /api/v1/sms/policy-status-requests i walidacja wejścia
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/16
- Branch: ai-coding/aisdlc-85-endpoint-post-api-v1-sms-policy-status-requests-29505354299

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-85-coding-prompt.md b/ai-coding-runs/aisdlc-85-coding-prompt.md
new file mode 100644
index 0000000..2296280
--- /dev/null
+++ b/ai-coding-runs/aisdlc-85-coding-prompt.md
@@ -0,0 +1,36 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-85 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: Endpoint POST /api/v1/sms/policy-status-requests i walidacja wejścia
+
+Task description:
+~~~markdown
+Parent story: AISDLC-78 — SMS z zapytaniem o status polisy bez logowania
+
+Dodanie kontrolera i DTO dla POST /api/v1/sms/policy-status-requests, wpięcie walidacji wejścia oraz mapowania odpowiedzi HTTP. Do sprawdzenia: pliki API/controller, request/response DTO, walidatory i routing.
+KONTRAKT: KONTRAKT (TechLeadAgent):
+Zakres: obsługa SMS bez logowania do sprawdzenia statusu polisy. Jedynymi danymi biznesowymi są policyNumber i pesel; nie używamy customerId ani policyId.
+Endpoint wejściowy: POST /api/v1/sms/policy-status-requests.
+Request JSON: messageId:string(UUID, idempotency key), senderMsisdn:string(E.164), policyNumber:string, pesel:string(11 cyfr), receivedAt:string(ISO-8601) opcjonalnie. policyNumber: trim + uppercase, regex ^[A-Z0-9-]{6,30}$; pesel: dokładnie 11 cyfr i poprawny checksum.
+Response 200: requestId:string(UUID), decisionCode:'REPLIED'|'REJECTED'|'RATE_LIMITED'|'ERROR', replyCode:'POLICY_STATUS_FOUND'|'POLICY_NOT_VERIFIED'|'INVALID_INPUT_MISSING_FIELDS'|'INVALID_POLICY_NUMBER_FORMAT'|'INVALID_PESEL_FORMAT'|'SMS_RATE_LIMITED'|'SERVICE_UNAVAILABLE', replyText:string, policyStatusCode:'ACTIVE'|'EXPIRED'|'SUSPENDED'|'CANCELLED'|null, policyStatusLabel:string|null.
+Zasada bezpieczeństwa: dla błędnego numeru polisy, błędnego PESEL albo braku polisy system zawsze zwraca ten sam wynik biznesowy POLICY_NOT_VERIFIED bez ujawniania, czy polisa istnieje; nie ma 404 i nie ma różnicy między 'nie istnieje' a 'niezgodny PESEL'.
+Rate limiting: 5 prób na 15 minut per senderMsisdn; po przekroczeniu blokada 60 minut, response 429 + SMS_RATE_LIMITED, bez wykonania lookupu.
+Walidacje: brak policyNumber lub pesel -> 400 INVALID_INPUT_MISSING_FIELDS; zły format policyNumber -> 422 INVALID_POLICY_NUMBER_FORMAT; zły pesel -> 422 INVALID_PESEL_FORMAT; błąd downstream -> 503 SERVICE_UNAVAILABLE.
+Baza: tabela sms_policy_status_request(id UUID PK, message_id UUID UNIQUE, sender_msisdn VARCHAR(20), policy_number VARCHAR(30), pesel_encrypted TEXT, request_status VARCHAR(20), response_code VARCHAR(40), response_text VARCHAR(160), created_at TIMESTAMPTZ, processed_at TIMESTAMPTZ, attempt_number INT, rate_limit_blocked_until TIMESTAMPTZ); pesel przechowujemy wyłącznie szyfrowany.
+Mapowanie scenariuszy: AISDLC-81 -> POLICY_STATUS_FOUND; AISDLC-82 -> INVALID_INPUT_MISSING_FIELDS; AISDLC-83 -> POLICY_NOT_VERIFIED; AISDLC-84 -> SMS_RATE_LIMITED.
+Eventy opcjonalne: sms.policy-status.requested.v1 i sms.policy-status.replied.v1; payload zawiera requestId, senderMsisdn, policyNumber, requestStatus i replyCode.
+~~~
+
+Task:
+1. Implement the requested code change in this repository, scoped to the task above.
+2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
+   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
+3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
+4. Do not read or print secrets. Avoid destructive git commands.
+5. Before finishing, leave the workspace ready to commit (diff applied on disk).
+
+Output: short summary of changed files and what each change does.
\ No newline at end of file
diff --git a/ai-coding-runs/pr-16-e2e-prompt.md b/ai-coding-runs/pr-16-e2e-prompt.md
new file mode 100644
index 0000000..3bcce71
--- /dev/null
+++ b/ai-coding-runs/pr-16-e2e-prompt.md
@@ -0,0 +1,473 @@
+You are the E2E TEST agent in a specialized worker pipeline (separate agents exist for
+coding, unit tests, and review — stay scoped to end-to-end / user-flow test coverage only).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Pull request under test:
+- Repository: LordIllidan/ai-coding-demo
+- PR: #16 AI: [AISDLC-85] Endpoint POST /api/v1/sms/policy-status-requests i walidacja wejścia
+- URL: https://github.com/LordIllidan/ai-coding-demo/pull/16
+- Branch: ai-coding/aisdlc-85-endpoint-post-api-v1-sms-policy-status-requests-29505354299
+
+PR description (contains the originating Jira key and task context — the acceptance
+criteria live in Jira; use what's summarized here as the source of truth for what a
+real flow must satisfy):
+~~~markdown
+Source: Jira **AISDLC-85** — Endpoint POST /api/v1/sms/policy-status-requests i walidacja wejścia
+
+Auto-generated by CodingWorker (model: sonnet). No GitHub issue exists for this task; Jira is the source of truth.
+
+```text
+Diff applied on disk, build green, ready to commit (commit itself left to wrapper script).
+
+## Summary — AISDLC-85: POST /api/v1/sms/policy-status-requests + input validation
+
+New endpoint added end-to-end with input validation and HTTP response mapping, per the parent story's contract. Decision logic (actual policy/PESEL lookup, AISDLC-86) and persistence/rate-limiting (AISDLC-87) are separate sibling tasks — deliberately not duplicated here; wired via a port + placeholder instead.
+
+**New files:**
+- `src/PolicyPlatform.Domain/Customers/Pesel.cs` — PESEL value object (11-digit + checksum validation).
+- `src/PolicyPlatform.Application/Sms/SmsDecisionCode.cs`, `SmsReplyCode.cs` — internal decision enums per contract.
+- `src/PolicyPlatform.Application/Sms/PolicyStatusDtos.cs` — business-level `PolicyStatusRequest`/`PolicyStatusReply`.
+- `src/PolicyPlatform.Application/Sms/PolicyStatusReplyMapper.cs` — maps outcomes to reply text; includes `MissingFields`/`InvalidPolicyNumberFormat`/`InvalidPeselFormat`/`RateLimited` factories for this task's validation errors.
+- `src/PolicyPlatform.Application/Sms/IPolicyStatusRequestHandler.cs` — seam for AISDLC-86's decision logic.
+- `src/PolicyPlatform.Application/Sms/SmsPolicyStatusHttpDtos.cs` — wire request/response DTOs; maps enums to exact contract strings (`REPLIED`, `POLICY_STATUS_FOUND`, etc.) independent of C# enum naming.
+- `src/PolicyPlatform.Application/Sms/PolicyStatusRequestValidator.cs` — validates messageId (UUID), senderMsisdn (E.164), policyNumber (trim+uppercase, `^[A-Z0-9-]{6,30}$`), pesel (checksum via `Pesel`).
+- `src/PolicyPlatform.Api/Controllers/SmsPolicyStatusRequestsController.cs` — routes `POST api/v1/sms/policy-status-requests`, runs validation → 400/422, else delegates to handler → 200/503.
+- `src/PolicyPlatform.Infrastructure/Sms/PendingPolicyStatusRequestHandler.cs` — placeholder handler (always `SERVICE_UNAVAILABLE`, safe/no data leak) until AISDLC-86 lands.
+
+**Modified:**
+- `src/PolicyPlatform.Infrastructure/DependencyInjection.cs` — registers the placeholder handler.
+
+Build verified green (`dotnet build`). Live smoke test skipped (session budget). 429 rate-limiting path not implemented — out of scope (AISDLC-84/87).
+```
+~~~
+
+Diff introduced by this PR:
+~~~diff
+diff --git a/ai-coding-runs/aisdlc-85-coding-prompt.md b/ai-coding-runs/aisdlc-85-coding-prompt.md
+new file mode 100644
+index 0000000..2296280
+--- /dev/null
++++ b/ai-coding-runs/aisdlc-85-coding-prompt.md
+@@ -0,0 +1,36 @@
++You are the CODING agent in a specialized worker pipeline (separate agents exist for
++unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
++Running locally through a GitHub self-hosted runner (Windows).
++
++Source of truth: Jira issue AISDLC-85 (this task has NO corresponding GitHub issue —
++Jira is the only tracker; do not create or reference a GitHub issue).
++
++Task title: Endpoint POST /api/v1/sms/policy-status-requests i walidacja wejścia
++
++Task description:
++~~~markdown
++Parent story: AISDLC-78 — SMS z zapytaniem o status polisy bez logowania
++
++Dodanie kontrolera i DTO dla POST /api/v1/sms/policy-status-requests, wpięcie walidacji wejścia oraz mapowania odpowiedzi HTTP. Do sprawdzenia: pliki API/controller, request/response DTO, walidatory i routing.
++KONTRAKT: KONTRAKT (TechLeadAgent):
++Zakres: obsługa SMS bez logowania do sprawdzenia statusu polisy. Jedynymi danymi biznesowymi są policyNumber i pesel; nie używamy customerId ani policyId.
++Endpoint wejściowy: POST /api/v1/sms/policy-status-requests.
++Request JSON: messageId:string(UUID, idempotency key), senderMsisdn:string(E.164), policyNumber:string, pesel:string(11 cyfr), receivedAt:string(ISO-8601) opcjonalnie. policyNumber: trim + uppercase, regex ^[A-Z0-9-]{6,30}$; pesel: dokładnie 11 cyfr i poprawny checksum.
++Response 200: requestId:string(UUID), decisionCode:'REPLIED'|'REJECTED'|'RATE_LIMITED'|'ERROR', replyCode:'POLICY_STATUS_FOUND'|'POLICY_NOT_VERIFIED'|'INVALID_INPUT_MISSING_FIELDS'|'INVALID_POLICY_NUMBER_FORMAT'|'INVALID_PESEL_FORMAT'|'SMS_RATE_LIMITED'|'SERVICE_UNAVAILABLE', replyText:string, policyStatusCode:'ACTIVE'|'EXPIRED'|'SUSPENDED'|'CANCELLED'|null, policyStatusLabel:string|null.
++Zasada bezpieczeństwa: dla błędnego numeru polisy, błędnego PESEL albo braku polisy system zawsze zwraca ten sam wynik biznesowy POLICY_NOT_VERIFIED bez ujawniania, czy polisa istnieje; nie ma 404 i nie ma różnicy między 'nie istnieje' a 'niezgodny PESEL'.
++Rate limiting: 5 prób na 15 minut per senderMsisdn; po przekroczeniu blokada 60 minut, response 429 + SMS_RATE_LIMITED, bez wykonania lookupu.
++Walidacje: brak policyNumber lub pesel -> 400 INVALID_INPUT_MISSING_FIELDS; zły format policyNumber -> 422 INVALID_POLICY_NUMBER_FORMAT; zły pesel -> 422 INVALID_PESEL_FORMAT; błąd downstream -> 503 SERVICE_UNAVAILABLE.
++Baza: tabela sms_policy_status_request(id UUID PK, message_id UUID UNIQUE, sender_msisdn VARCHAR(20), policy_number VARCHAR(30), pesel_encrypted TEXT, request_status VARCHAR(20), response_code VARCHAR(40), response_text VARCHAR(160), created_at TIMESTAMPTZ, processed_at TIMESTAMPTZ, attempt_number INT, rate_limit_blocked_until TIMESTAMPTZ); pesel przechowujemy wyłącznie szyfrowany.
++Mapowanie scenariuszy: AISDLC-81 -> POLICY_STATUS_FOUND; AISDLC-82 -> INVALID_INPUT_MISSING_FIELDS; AISDLC-83 -> POLICY_NOT_VERIFIED; AISDLC-84 -> SMS_RATE_LIMITED.
++Eventy opcjonalne: sms.policy-status.requested.v1 i sms.policy-status.replied.v1; payload zawiera requestId, senderMsisdn, policyNumber, requestStatus i replyCode.
++~~~
++
++Task:
++1. Implement the requested code change in this repository, scoped to the task above.
++2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
++   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
++3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
++4. Do not read or print secrets. Avoid destructive git commands.
++5. Before finishing, leave the workspace ready to commit (diff applied on disk).
++
++Output: short summary of changed files and what each change does.
+\ No newline at end of file
+diff --git a/src/PolicyPlatform.Api/Controllers/SmsPolicyStatusRequestsController.cs b/src/PolicyPlatform.Api/Controllers/SmsPolicyStatusRequestsController.cs
+new file mode 100644
+index 0000000..15342ba
+--- /dev/null
++++ b/src/PolicyPlatform.Api/Controllers/SmsPolicyStatusRequestsController.cs
+@@ -0,0 +1,48 @@
++using Microsoft.AspNetCore.Mvc;
++using PolicyPlatform.Application.Sms;
++
++namespace PolicyPlatform.Api.Controllers;
++
++[ApiController]
++[Route("api/v1/sms/policy-status-requests")]
++public sealed class SmsPolicyStatusRequestsController : ControllerBase
++{
++    private readonly IPolicyStatusRequestHandler _handler;
++
++    public SmsPolicyStatusRequestsController(IPolicyStatusRequestHandler handler) => _handler = handler;
++
++    [HttpPost]
++    public async Task<ActionResult<SmsPolicyStatusResponseDto>> Create(
++        SmsPolicyStatusRequestDto request, CancellationToken ct)
++    {
++        var requestId = Guid.NewGuid();
++        var validation = PolicyStatusRequestValidator.Validate(request, out var normalizedPolicyNumber);
++
++        var reply = validation switch
++        {
++            PolicyStatusRequestValidationResult.MissingFields =>
++                PolicyStatusReplyMapper.MissingFields(requestId),
++            PolicyStatusRequestValidationResult.InvalidPolicyNumberFormat =>
++                PolicyStatusReplyMapper.InvalidPolicyNumberFormat(requestId),
++            PolicyStatusRequestValidationResult.InvalidPeselFormat =>
++                PolicyStatusReplyMapper.InvalidPeselFormat(requestId),
++            _ => null,
++        };
++
++        // Rate limiting (5/15min per senderMsisdn, AISDLC-84/87) is not wired in yet — only
++        // input-shape validation and the resulting HTTP mapping are in scope here.
++        reply ??= await _handler.HandleAsync(new PolicyStatusRequest(normalizedPolicyNumber, request.Pesel!), ct);
++
++        return StatusCode(HttpStatusFor(reply), SmsPolicyStatusResponseDto.FromReply(reply));
++    }
++
++    private static int HttpStatusFor(PolicyStatusReply reply) => reply.ReplyCode switch
++    {
++        SmsReplyCode.InvalidInputMissingFields => StatusCodes.Status400BadRequest,
++        SmsReplyCode.InvalidPolicyNumberFormat => StatusCodes.Status422UnprocessableEntity,
++        SmsReplyCode.InvalidPeselFormat => StatusCodes.Status422UnprocessableEntity,
++        SmsReplyCode.SmsRateLimited => StatusCodes.Status429TooManyRequests,
++        SmsReplyCode.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
++        _ => StatusCodes.Status200OK,
++    };
++}
+diff --git a/src/PolicyPlatform.Application/Sms/IPolicyStatusRequestHandler.cs b/src/PolicyPlatform.Application/Sms/IPolicyStatusRequestHandler.cs
+new file mode 100644
+index 0000000..18f860e
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Sms/IPolicyStatusRequestHandler.cs
+@@ -0,0 +1,11 @@
++namespace PolicyPlatform.Application.Sms;
++
++/// <summary>Use-case port behind POST /api/v1/sms/policy-status-requests (AISDLC-78). Given an
++/// already input-validated request (well-formed policyNumber/pesel — see
++/// <see cref="PolicyStatusRequestValidator"/>), decides the business outcome. Does not perform
++/// input-shape validation or rate limiting — those are handled upstream by the controller — and
++/// does not know about HTTP status codes.</summary>
++public interface IPolicyStatusRequestHandler
++{
++    Task<PolicyStatusReply> HandleAsync(PolicyStatusRequest request, CancellationToken ct = default);
++}
+diff --git a/src/PolicyPlatform.Application/Sms/PolicyStatusDtos.cs b/src/PolicyPlatform.Application/Sms/PolicyStatusDtos.cs
+new file mode 100644
+index 0000000..e63868e
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Sms/PolicyStatusDtos.cs
+@@ -0,0 +1,13 @@
++using PolicyPlatform.Domain.Policies;
++
++namespace PolicyPlatform.Application.Sms;
++
++public sealed record PolicyStatusRequest(string PolicyNumber, string Pesel);
++
++public sealed record PolicyStatusReply(
++    Guid RequestId,
++    SmsDecisionCode DecisionCode,
++    SmsReplyCode ReplyCode,
++    string ReplyText,
++    PolicyStatus? PolicyStatusCode,
++    string? PolicyStatusLabel);
+diff --git a/src/PolicyPlatform.Application/Sms/PolicyStatusReplyMapper.cs b/src/PolicyPlatform.Application/Sms/PolicyStatusReplyMapper.cs
+new file mode 100644
+index 0000000..6cf6e7a
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Sms/PolicyStatusReplyMapper.cs
+@@ -0,0 +1,90 @@
++using PolicyPlatform.Domain.Policies;
++
++namespace PolicyPlatform.Application.Sms;
++
++/// <summary>Maps use-case outcomes to the decisionCode/replyCode/replyText contract for the
++/// SMS policy-status endpoint (AISDLC-78). Kept separate from the decision logic so the
++/// wording/labels can change without touching business rules.</summary>
++public static class PolicyStatusReplyMapper
++{
++    public static PolicyStatusReply Found(Guid requestId, PolicyStatus status) => new(
++        requestId,
++        SmsDecisionCode.Replied,
++        SmsReplyCode.PolicyStatusFound,
++        ReplyText(SmsReplyCode.PolicyStatusFound),
++        status,
++        StatusLabel(status));
++
++    /// <summary>Covers both "no such policy" and "policy exists but PESEL does not match" —
++    /// the two must be indistinguishable to the caller, so this is the only non-found outcome.</summary>
++    public static PolicyStatusReply NotVerified(Guid requestId) => new(
++        requestId,
++        SmsDecisionCode.Replied,
++        SmsReplyCode.PolicyNotVerified,
++        ReplyText(SmsReplyCode.PolicyNotVerified),
++        null,
++        null);
++
++    public static PolicyStatusReply ServiceUnavailable(Guid requestId) => new(
++        requestId,
++        SmsDecisionCode.Error,
++        SmsReplyCode.ServiceUnavailable,
++        ReplyText(SmsReplyCode.ServiceUnavailable),
++        null,
++        null);
++
++    /// <summary>Required policyNumber/pesel field(s) were absent from the request body.</summary>
++    public static PolicyStatusReply MissingFields(Guid requestId) => new(
++        requestId,
++        SmsDecisionCode.Rejected,
++        SmsReplyCode.InvalidInputMissingFields,
++        ReplyText(SmsReplyCode.InvalidInputMissingFields),
++        null,
++        null);
++
++    public static PolicyStatusReply InvalidPolicyNumberFormat(Guid requestId) => new(
++        requestId,
++        SmsDecisionCode.Rejected,
++        SmsReplyCode.InvalidPolicyNumberFormat,
++        ReplyText(SmsReplyCode.InvalidPolicyNumberFormat),
++        null,
++        null);
++
++    public static PolicyStatusReply InvalidPeselFormat(Guid requestId) => new(
++        requestId,
++        SmsDecisionCode.Rejected,
++        SmsReplyCode.InvalidPeselFormat,
++        ReplyText(SmsReplyCode.InvalidPeselFormat),
++        null,
++        null);
++
++    public static PolicyStatusReply RateLimited(Guid requestId) => new(
++        requestId,
++        SmsDecisionCode.RateLimited,
++        SmsReplyCode.SmsRateLimited,
++        ReplyText(SmsReplyCode.SmsRateLimited),
++        null,
++        null);
++
++    public static string ReplyText(SmsReplyCode replyCode) => replyCode switch
++    {
++        SmsReplyCode.PolicyStatusFound => "Status Twojej polisy zostal znaleziony.",
++        SmsReplyCode.PolicyNotVerified => "Nie udalo sie zweryfikowac polisy. Sprawdz numer polisy i PESEL.",
++        SmsReplyCode.InvalidInputMissingFields => "Brak wymaganych danych: numer polisy i PESEL sa wymagane.",
++        SmsReplyCode.InvalidPolicyNumberFormat => "Nieprawidlowy format numeru polisy.",
++        SmsReplyCode.InvalidPeselFormat => "Nieprawidlowy numer PESEL.",
++        SmsReplyCode.SmsRateLimited => "Zbyt wiele prob. Sprobuj ponownie pozniej.",
++        SmsReplyCode.ServiceUnavailable => "Usluga jest chwilowo niedostepna. Sprobuj ponownie pozniej.",
++        _ => throw new ArgumentOutOfRangeException(nameof(replyCode), replyCode, null),
++    };
++
++    public static string StatusLabel(PolicyStatus status) => status switch
++    {
++        PolicyStatus.Active => "Aktywna",
++        PolicyStatus.Expired => "Wygasla",
++        PolicyStatus.Cancelled => "Anulowana",
++        // Draft policies are not yet issued and must never surface as a "found" result —
++        // lookup implem
... diff truncated ...
~~~

Standards to follow (do not invent your own format):

1. XML documentation comments (Microsoft C# standard, ///) on every public type and public
   member introduced or changed in this diff — Domain entities/value objects, Application
   service methods, Api controller actions, McpServer tools. <summary> is mandatory;
   <param>/<returns>/<exception> where applicable. Follow the tone already used in this repo
   if any XML doc comments already exist — otherwise establish it consistently.
2. If this diff introduces a genuinely new architectural decision (new persistence
   technology, new external dependency, a pattern that constrains future work), add an
   Architecture Decision Record under docs/adr/NNNN-title-in-kebab-case.md using the
   Michael Nygard ADR format: Title, Status (Proposed/Accepted), Context, Decision,
   Consequences. Number sequentially from the highest existing ADR in docs/adr/ (start at
   0001 if the directory does not exist yet). Do NOT write an ADR for routine feature work
   that doesn't change architecture — most PRs will not need one.
3. Update README.md ONLY if this diff changes something a reader of the README would need
   to know (new project, new setup step, new public API surface worth mentioning) — do not
   pad README with routine changes.
4. Do NOT modify Domain/Application/Infrastructure business logic, tests, or CI — only
   doc comments (which live inside source files but change no behavior), docs/, and README.md.
5. Do not merge, push, or create/edit pull requests — the wrapper script handles that.
6. Do not read or print secrets. Avoid destructive git commands.

Output: short summary of what got documented (XML comments added to which types, any ADR
written, any README change) or "no documentation gap found" if the diff is already
adequately documented.