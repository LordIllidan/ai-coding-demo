You are the UNIT TEST agent in a specialized worker pipeline (separate agents exist for
coding, e2e tests, and review — stay scoped to unit-level test coverage only).
Running locally through a GitHub self-hosted runner (Windows).

Pull request under test:
- Repository: LordIllidan/ai-coding-demo
- PR: #15 AI: [AISDLC-86] Logika statusu polisy: decyzje, mapowanie replyCode i ochrona przed ujawnieniem danych
- URL: https://github.com/LordIllidan/ai-coding-demo/pull/15
- Branch: ai-coding/aisdlc-86-logika-statusu-polisy-decyzje-mapowanie-replycod-29505352007

Diff introduced by this PR:
~~~diff
diff --git a/ai-coding-runs/aisdlc-86-coding-prompt.md b/ai-coding-runs/aisdlc-86-coding-prompt.md
new file mode 100644
index 0000000..e7a6428
--- /dev/null
+++ b/ai-coding-runs/aisdlc-86-coding-prompt.md
@@ -0,0 +1,36 @@
+You are the CODING agent in a specialized worker pipeline (separate agents exist for
+unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Source of truth: Jira issue AISDLC-86 (this task has NO corresponding GitHub issue —
+Jira is the only tracker; do not create or reference a GitHub issue).
+
+Task title: Logika statusu polisy: decyzje, mapowanie replyCode i ochrona przed ujawnieniem danych
+
+Task description:
+~~~markdown
+Parent story: AISDLC-78 — SMS z zapytaniem o status polisy bez logowania
+
+Implementacja logiki decyzyjnej dla statusu polisy: service/use case, mapowanie decisionCode/replyCode oraz jednolity wynik POLICY_NOT_VERIFIED bez ujawniania, czy polisa istnieje. Do sprawdzenia: warstwa serwisowa, mappery odpowiedzi i obsługa błędów domenowych.
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
diff --git a/ai-coding-runs/pr-15-docs-prompt.md b/ai-coding-runs/pr-15-docs-prompt.md
new file mode 100644
index 0000000..18b6653
--- /dev/null
+++ b/ai-coding-runs/pr-15-docs-prompt.md
@@ -0,0 +1,346 @@
+You are the DOCUMENTATION agent in a specialized worker pipeline (separate agents handle
+coding, unit tests, e2e tests, database, and review — stay scoped to documentation only,
+never touch executable logic).
+Running locally through a GitHub self-hosted runner (Windows).
+
+Pull request under documentation:
+- Repository: LordIllidan/ai-coding-demo
+- PR: #15 AI: [AISDLC-86] Logika statusu polisy: decyzje, mapowanie replyCode i ochrona przed ujawnieniem danych
+- URL: https://github.com/LordIllidan/ai-coding-demo/pull/15
+- Branch: ai-coding/aisdlc-86-logika-statusu-polisy-decyzje-mapowanie-replycod-29505352007
+
+Diff introduced by this PR:
+~~~diff
+diff --git a/ai-coding-runs/aisdlc-86-coding-prompt.md b/ai-coding-runs/aisdlc-86-coding-prompt.md
+new file mode 100644
+index 0000000..e7a6428
+--- /dev/null
++++ b/ai-coding-runs/aisdlc-86-coding-prompt.md
+@@ -0,0 +1,36 @@
++You are the CODING agent in a specialized worker pipeline (separate agents exist for
++unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
++Running locally through a GitHub self-hosted runner (Windows).
++
++Source of truth: Jira issue AISDLC-86 (this task has NO corresponding GitHub issue —
++Jira is the only tracker; do not create or reference a GitHub issue).
++
++Task title: Logika statusu polisy: decyzje, mapowanie replyCode i ochrona przed ujawnieniem danych
++
++Task description:
++~~~markdown
++Parent story: AISDLC-78 — SMS z zapytaniem o status polisy bez logowania
++
++Implementacja logiki decyzyjnej dla statusu polisy: service/use case, mapowanie decisionCode/replyCode oraz jednolity wynik POLICY_NOT_VERIFIED bez ujawniania, czy polisa istnieje. Do sprawdzenia: warstwa serwisowa, mappery odpowiedzi i obsługa błędów domenowych.
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
+diff --git a/src/PolicyPlatform.Application/Abstractions/IPolicyStatusLookupRepository.cs b/src/PolicyPlatform.Application/Abstractions/IPolicyStatusLookupRepository.cs
+new file mode 100644
+index 0000000..86497b3
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Abstractions/IPolicyStatusLookupRepository.cs
+@@ -0,0 +1,13 @@
++using PolicyPlatform.Domain.Customers;
++using PolicyPlatform.Domain.Policies;
++
++namespace PolicyPlatform.Application.Abstractions;
++
++/// <summary>Port used by the SMS policy-status use case. The single lookup method must return
++/// null both when the policy number does not exist and when it exists but the PESEL does not
++/// match its holder — the two cases are indistinguishable by design so implementations cannot
++/// accidentally leak whether a policy exists via a richer return type.</summary>
++public interface IPolicyStatusLookupRepository
++{
++    Task<PolicyStatus?> FindPolicyStatusAsync(PolicyNumber policyNumber, Pesel pesel, CancellationToken ct = default);
++}
+diff --git a/src/PolicyPlatform.Application/Abstractions/PolicyStatusLookupUnavailableException.cs b/src/PolicyPlatform.Application/Abstractions/PolicyStatusLookupUnavailableException.cs
+new file mode 100644
+index 0000000..76c75e5
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Abstractions/PolicyStatusLookupUnavailableException.cs
+@@ -0,0 +1,13 @@
++namespace PolicyPlatform.Application.Abstractions;
++
++/// <summary>Thrown by <see cref="IPolicyStatusLookupRepository"/> implementations when the
++/// lookup could not be completed (e.g. a downstream dependency failure). Distinguished from
++/// domain validation errors so the use case can map it to SERVICE_UNAVAILABLE instead of the
++/// uniform POLICY_NOT_VERIFIED result.</summary>
++public sealed class PolicyStatusLookupUnavailableException : Exception
++{
++    public PolicyStatusLookupUnavailableException(string message, Exception? innerException = null)
++        : base(message, innerException)
++    {
++    }
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
+index 0000000..09d86ea
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Sms/PolicyStatusReplyMapper.cs
+@@ -0,0 +1,57 @@
++using PolicyPlatform.Domain.Policies;
++
++namespace PolicyPlatform.Application.Sms;
++
++/// <summary>Maps use-case outcomes to the decisionCode/replyCode/replyText contract for the
++/// SMS policy-status endpoint (AISDLC-78). Kept separate from PolicyStatusRequestService so the
++/// wording/labels can change without touching the decision logic.</summary>
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
++        // IPolicyStatusLookupRepository implementations must treat them as no-match.
++        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status is not a disclosable policy status."),
++    };
++}
+diff --git a/src/PolicyPlatform.Application/Sms/PolicyStatusRequestService.cs b/src/PolicyPlatform.Application/Sms/PolicyStatusRequestService.cs
+new file mode 100644
+index 0000000..8be0bf9
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Sms/PolicyStatusRequestService.cs
+@@ -0,0 +1,49 @@
++using PolicyPlatform.Application.Abstractions;
++using PolicyPlatform.Domain.Common;
++using PolicyPlatform.Domain.Customers;
++using PolicyPlatform.Domain.Policies;
++
++namespace PolicyPlatform.Application.Sms;
++
++/// <summary>Use case behind POST /api/v1/sms/policy-status-requests (AISDLC-78). Decides the
++/// business outcome of a policy-status lookup; does not perform input-shape validation or rate
++/// limiting (handled upstream) and does not know about HTTP status codes.</summary>
++public sealed class PolicyStatusRequestService
++{
++    private readonly IPolicyStatusLookupRepository _lookup;
++
++    public PolicyStatusRequestService(IPolicyStatusLookupRepository lookup) => _lookup = lookup;
++
++    public async Task<PolicyStatusReply> HandleAsync(PolicyStatusRequest request, CancellationToken ct = default)
++    {
++        var requestId = Guid.NewGuid();
++
++        PolicyNumber policyNumber;
++        Pesel pesel;
++        try
++        {
++            policyNumber = new PolicyNumber(request.PolicyNumber);
++            pesel = new Pesel(request.Pesel);
++        }
++        catch (DomainException)
++        {
++            // Well-formed-per-upstream-validation but not a real policy number/PESEL shape:
++            // still falls under "no disclosure whether a policy exists".
++            return PolicyStatusReplyMapper.NotVerified(requestId);
++        }
++
++        PolicyStatus? status;
++        try
++        {
++            status = await _lookup.FindPolicyStatusAsync(policyNumber, pesel, ct);
++        }
++        catch (PolicyStatusLookupUnavailableException)
++        {
++            return PolicyStatusReplyMapper.ServiceUnavailable(requestId);
++        }
++
++        return status is null
++            ? PolicyStatusReplyMapper.NotVerified(requestId)
++            : PolicyStatusReplyMapper.Found(requestId, status.Value);
++    }
++}
+diff --git a/src/PolicyPlatform.Application/Sms/SmsDecisionCode.cs b/src/PolicyPlatform.Application/Sms/SmsDecisionCode.cs
+new file mode 100644
+index 0000000..20969fb
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Sms/SmsDecisionCode.cs
+@@ -0,0 +1,10 @@
++namespace PolicyPlatform.Application.Sms;
++
++/// <summary>Top-level outcome of an SMS policy-status request, per the AISDLC-78 contract.</summary>
++public enum SmsDecisionCode
++{
++    Replied,
++    Rejected,
++    RateLimited,
++    Error,
++}
+diff --git a/src/PolicyPlatform.Application/Sms/SmsReplyCode.cs b/src/PolicyPlatform.Application/Sms/SmsReplyCode.cs
+new file mode 100644
+index 0000000..7f56a47
+--- /dev/null
++++ b/src/PolicyPlatform.Application/Sms/SmsReplyCode.cs
+@@ -0,0 +1,13 @@
++namespace PolicyPlatform.Application.Sms;
++
++/// <summary>Fine-grained reply reason for an SMS policy-status request, per the AISDLC-78 contract.</summary>
++public enum SmsReplyCode
++{
++    PolicyStatusFound,
++    PolicyNotVerified,
++    InvalidInputMissingFields,
++    InvalidPolicyNumberFormat,
++    InvalidPeselFormat,
++    SmsRateLimited,
++    ServiceUnavailable,
++}
+diff --git a/src/PolicyPlatform.Domain/Customers/Pesel.cs b/src/PolicyPlatform.Domain/Customers/Pesel.cs
+new file mode 100644
+index 0000000..24a0ceb
+--- /dev/null
++++ b/src/PolicyPlatform.Domain/Customers/Pesel.cs
+@@ -0,0 +1,46 @@
++using System.Text.RegularExpressions;
++using PolicyPlatform.Domain.Common;
++
++namespace PolicyPlatform.Domain.Customers;
++
++/// <summary>Polish national identification number. Validates format and the official
++/// checksum so a merely well-formed-looking but bogus number is rejected at the domain
++/// boundary rather than being silently compared/stored.</summary>
++public readonly partial record struct Pesel
++{
++    private static readonly int[] Weights = [1, 3, 7, 9, 1, 3, 7, 9, 1, 3];
++
++    public string Value { get; }
++
++    public Pesel(string value)
++    {
++        if (!PeselPattern().IsMatch(value))
++        {
++            throw new DomainException("PESEL must be exactly 11 digits.");
++        }
++
++        if (!HasValidChecksum(value))
++        {
++            throw new DomainException("PESEL checksum is invalid.");
++        }
++
++        Value = value;
++    }
++
++    public override string ToString() => Value;
++
++    private static bool HasValidChecksum(string pesel)
++    {
++        var sum = 0;
++        for (var i = 0; i < Weights.Length; i++)
++        {
++            sum += (pesel[i] - '0') * Weights[i];
++        }
++
++        var checkDigit = (10 - (sum % 10)) % 10;
++        return checkDigit == pesel[10] - '0';
++    }
++
++    [GeneratedRegex(@"^\d{11}$")]
++    private static partial 
... diff truncated ...
~~~

Task:
1. Identify new or changed functions/methods/classes in the diff that lack unit test coverage.
2. Write focused unit tests for them, following this repository's existing test conventions
   (framework, file layout, naming) — inspect existing tests/ before writing new ones.
3. Do NOT modify production/source code — only add or extend test files. If a change is
   untestable without a source fix, say so in your output instead of touching source.
4. Do not merge, push, or create/edit pull requests — the wrapper script handles that.
5. Do not read or print secrets. Avoid destructive git commands.

Output: short summary of which functions got new test coverage and any gaps you could not cover.