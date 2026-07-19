You are the CODING agent in a specialized worker pipeline (separate agents exist for
unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
Running locally through a GitHub self-hosted runner (Windows).

Source of truth: Jira issue AISDLC-51 (this task has NO corresponding GitHub issue —
Jira is the only tracker; do not create or reference a GitHub issue).

Task title: Implementacja endpointu POST /api/theft-claims i walidacji numeru zgłoszenia Policji

Task description:
~~~markdown
Parent story: AISDLC-31 — Walidacja braku lub niepoprawności numeru zgłoszenia Policji

Implementacja backendowego endpointu POST /api/theft-claims wraz z walidacją policeReportNumber, normalizacją do UPPERCASE i zwracaniem 422 dla błędów walidacji.
KONTRAKT:
Zakres: jedno API do zapisu zgłoszenia kradzieży pojazdu z walidacją numeru zgłoszenia Policji.
Endpoint: POST /api/theft-claims.
Request JSON: policyId:string (UUID, required) — identyfikator polisy; NIE customerId. policeReportNumber:string (required, trim, 3-50 znaków, dozwolone litery/cyfry/spacja/"/"/"-"); przed zapisem normalizować do UPPERCASE.
Sukces: 201 Created, body: { claimId:string(UUID), policyId:string(UUID), policeReportNumber:string, status:'ACCEPTED', nextStepAllowed:true }.
Błąd walidacji: 422 Unprocessable Entity, body: { code:'VALIDATION_ERROR', fieldErrors:[{ field:'policeReportNumber', code:'POLICE_REPORT_NUMBER_REQUIRED' | 'POLICE_REPORT_NUMBER_INVALID_FORMAT', message:'Numer zgłoszenia Policji jest wymagany i musi być poprawny.' }] }.
Reguła biznesowa: jeśli policeReportNumber jest pusty, po trim ma 0 znaków albo nie pasuje do regexu ^[A-Z0-9][A-Z0-9/ -]{2,49}$, backend odrzuca zapis i nie przechodzi do kolejnego kroku procesu.
DB: tabela theft_claim; kolumny min.: id UUID PK, policy_id UUID NOT NULL, police_report_number VARCHAR(50) NOT NULL, status VARCHAR(20) NOT NULL, created_at TIMESTAMP, updated_at TIMESTAMP; zapisujemy wartość już po normalizacji.
~~~

Task:
1. Implement the requested code change in this repository, scoped to the task above.
2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
4. Do not read or print secrets. Avoid destructive git commands.
5. Before finishing, leave the workspace ready to commit (diff applied on disk).

Output: short summary of changed files and what each change does.