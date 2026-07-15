# ai-coding-demo

**PolicyPlatform** — .NET DDD Clean Architecture app (insurance policy generation domain:
Domain/Application/Infrastructure/Api + an MCP server), plus 6 wyspecjalizowanych workerow
ktore odpalaja lokalne `claude` CLI (Claude Code) na self-hosted runnerze (label
`ai-demo-worker`) i implementuja/testuja/reviewuja/persystuja/dokumentuja zmiany. Wzorowane
na `AgentWorkflowPDLC2`.

**Jira jest jedynym trackerem zadan** — CodingWorker NIE tworzy GitHub issue, tylko branch+PR
bezposrednio z `repository_dispatch` (payload: jira_key, title, body). UnitTest/E2E/Review
workery dzialaja juz na PR (label na PR, nie na issue).

## Architektura appki (`src/`, `tests/`)

```
PolicyPlatform.Domain          <- zero zaleznosci, encje: Policy, Customer, Coverage, Money...
PolicyPlatform.Application     <- use-case'y (PolicyService/CustomerService), interfejsy repo
PolicyPlatform.Infrastructure  <- in-memory repo (latwo podmienic na EF Core), DI wiring
PolicyPlatform.Api             <- ASP.NET Core WebApi (kontrolery REST)
PolicyPlatform.McpServer       <- MCP server (stdio) - narzedzia generate_policy/get_policy/...
                                   dla agenta, cienka warstwa nad tym samym PolicyService
```

Zasada zaleznosci: Domain <- Application <- Infrastructure <- {Api, McpServer}. Api i McpServer
nigdy nie zawieraja logiki biznesowej — tylko translacje request/response <-> Application.

## Workery

| Worker | Trigger | Co robi | Skrypt |
|---|---|---|---|
| **CodingWorker** | `repository_dispatch` (`ai_coding_task`) z Jiry | implementuje task, otwiera branch+PR | `ai-coding-worker.ps1` |
| **UnitTestWorker** | label `ai-unittest-task` na PR | dopisuje testy jednostkowe (xUnit) na TYM SAMYM branchu, nie rusza kodu produkcyjnego | `ai-unittest-worker.ps1` |
| **E2ETestWorker** | label `ai-e2e-task` na PR | integracyjne testy HTTP przez `WebApplicationFactory<Program>` | `ai-e2e-worker.ps1` |
| **DatabaseWorker** | label `ai-database-task` na PR | EF Core: DbContext/entity configs/migracje w `PolicyPlatform.Infrastructure`, NIE rusza Domain/Application | `ai-database-worker.ps1` |
| **DocumentationWorker** | label `ai-docs-task` na PR | XML doc comments (standard Microsoft `///`) na publicznym API, ADR (format Michael Nygard) dla decyzji architektonicznych, README gdy trzeba | `ai-docs-worker.ps1` |
| **ReviewWorker** | label `ai-review-task` na PR | tylko czyta diff, **nie edytuje plikow**, `gh pr review --comment`/`--request-changes` — NIGDY `--approve` (AI nie zatwierdza wlasnego kodu) | `ai-review-worker.ps1` |

Wspolne helpery (branch/push/claude invoke) w `ai-worker-common.ps1`, dot-source w kazdym workerze.

## CI/CD (`.github/workflows/dotnet-ci.yml`)

`dotnet build` + `dotnet test` na kazdym push/PR. Osobny job **guard-agent-scope**: jesli
branch/PR pochodzi z `ai-coding/*` i dotyka `.github/workflows/` lub `.github/scripts/` —
build failuje. Agent nie moze modyfikowac wlasnych guardrails/CI, tylko czlowiek.

## Jak odpalic demo z Jiry

Patrz `docs/jira-github-integration.md` w glownym repo (Azure F) — opisuje jak
AISDLC (Jira) laczy sie z `repository_dispatch` tutaj.

Recznie (bez Jiry, do testow): `gh workflow run ai-coding-worker.yml --repo LordIllidan/ai-coding-demo -f jira_key=TEST-1 -f title="..." -f body="..."`,
potem `gh workflow run ai-unittest-worker.yml -f pr_number=<nr>` (analogicznie e2e/review).
