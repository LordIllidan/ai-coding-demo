You are a coding agent running locally through a GitHub self-hosted runner (Windows).

Source GitHub issue:
- Repository: LordIllidan/ai-coding-demo
- Issue: #1
- URL: https://github.com/LordIllidan/ai-coding-demo/issues/1
- Title: Dodaj endpoint /health

Issue body:
~~~markdown
Dodaj plik src/health.py z funkcja health() ktora zwraca slownik {'status': 'ok'}. Dodaj tez prosty test w tests/test_health.py sprawdzajacy ze health() zwraca dokladnie {'status': 'ok'}.
~~~

Task:
1. Implement the requested code change in this repository, scoped to the issue.
2. Add or update focused tests when the change affects behavior.
3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
4. Do not read or print secrets. Avoid destructive git commands.
5. Before finishing, leave the workspace ready to commit (diff applied on disk).

Output: short summary of changed files and what each change does.