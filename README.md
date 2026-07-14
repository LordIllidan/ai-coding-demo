# ai-coding-demo

Demo repo: 4 wyspecjalizowane workery, kazdy osobny label + osobny base prompt,
kazdy odpala lokalne `claude` CLI (Claude Code) na self-hosted runnerze (label
`ai-demo-worker`). Wzorowane na `AgentWorkflowPDLC2`, uproszczone (bez zewnetrznego
AgentConfig repo, bez etapow risk/research/plan — prosto issue -> coding -> testy -> review).

## Workery

| Worker | Trigger | Co robi | Skrypt |
|---|---|---|---|
| **CodingWorker** | label `ai-coding-task` na issue | implementuje feature, otwiera branch+PR | `ai-coding-worker.ps1` |
| **UnitTestWorker** | label `ai-unittest-task` na PR | dopisuje testy jednostkowe na TYM SAMYM branchu PR-a (bez zmian w kodzie produkcyjnym) | `ai-unittest-worker.ps1` |
| **E2ETestWorker** | label `ai-e2e-task` na PR | dopisuje testy e2e wg acceptance criteria z issue | `ai-e2e-worker.ps1` |
| **ReviewWorker** | label `ai-review-task` na PR | tylko czyta diff, **nie edytuje plikow**, wystawia `gh pr review --comment`/`--request-changes` — NIGDY `--approve` (zeby AI nie zatwierdzalo wlasnego kodu) | `ai-review-worker.ps1` |

Wspolne helpery (branch/push/claude invoke) w `ai-worker-common.ps1`, dot-source w kazdym workerze.

## Jak odpalic demo (pelny lancuch)

1. Upewnij sie ze self-hosted runner z labelem `ai-demo-worker` jest online:
   `gh api repos/LordIllidan/ai-coding-demo/actions/runners`
2. Coding: `gh issue create --repo LordIllidan/ai-coding-demo --title "..." --body "..." --label ai-coding-task`
3. Po PR: `gh pr edit <nr> --repo LordIllidan/ai-coding-demo --add-label ai-unittest-task`
4. Po testach: `gh pr edit <nr> --repo LordIllidan/ai-coding-demo --add-label ai-e2e-task`
5. Na koniec: `gh pr edit <nr> --repo LordIllidan/ai-coding-demo --add-label ai-review-task`
6. Watch: `gh run watch --repo LordIllidan/ai-coding-demo`

Kazdy krok mozna tez odpalic recznie: `gh workflow run ai-unittest-worker.yml --repo LordIllidan/ai-coding-demo -f pr_number=<nr>` (analogicznie dla e2e/review).

## src/

Pusty punkt startowy zeby agent mial gdzie dopisac kod.
