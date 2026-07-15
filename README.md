# ai-coding-demo

Demo repo: issue z labelem `ai-coding-task` -> self-hosted runner odpala lokalne
`claude` CLI (Claude Code) -> branch + commit + PR. Wzorowane na
`AgentWorkflowPDLC2` (uproszczone: bez etapow risk/stage/review, bez zewnetrznego
AgentConfig repo, prosto issue -> coding -> PR).

## Jak odpalic demo

1. Upewnij sie ze self-hosted runner z labelem `ai-demo-worker` jest online:
   `gh api repos/LordIllidan/ai-coding-demo/actions/runners`
2. Utworz issue z labelem `ai-coding-task`:
   ```
   gh issue create --repo LordIllidan/ai-coding-demo \
     --title "Dodaj endpoint /health" \
     --body "Zwroc plik src/health.py z funkcja health() zwracajaca {'status':'ok'}." \
     --label ai-coding-task
   ```
3. Watch: `gh run watch --repo LordIllidan/ai-coding-demo`
4. Po zakonczeniu: `gh pr view --repo LordIllidan/ai-coding-demo --web`

## src/

- `health.py` — `health()` zwraca `{"status": "ok"}`. Testy: `tests/test_health.py` (unit),
  `tests/test_health_e2e.py` (proces-boundary check, brak HTTP frameworka w repo).
