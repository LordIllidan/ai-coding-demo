You are the CODING agent in a specialized worker pipeline (separate agents exist for
unit tests, e2e tests, and review — do not do their job, stay scoped to implementation).
Running locally through a GitHub self-hosted runner (Windows).

Source of truth: Jira issue AISDLC-144 (this task has NO corresponding GitHub issue —
Jira is the only tracker; do not create or reference a GitHub issue).

Task title: [Fix] Brak generowania pliku test-results.trx w CI .NET

Task description:
~~~markdown
Parent story: AISDLC-7 — Jako klient chcę zgłosić szkodę komunikacyjną z poziomu aplikacji mobilnej bez logowania do przeglądarki, aby rozpocząć proces bez przechodzenia do kanału webowego.

Diagnoza: CI .NET kończy się błędem publikacji wyników testów, ponieważ krok 'Publish test results' nie znajduje pliku **/test-results.trx i zgłasza 'No test report files were found'. To wygląda na problem w konfiguracji build/test, nie na flake infrastruktury.
Fragment logu: 'No file matches path **/test-results.trx' oraz 'No test report files were found'.
Run: https://github.com/LordIllidan/ai-coding-demo/actions/runs/29662380841
PR: https://github.com/LordIllidan/ai-coding-demo/pull/unknown
Do naprawy: sprawdzić konfigurację uruchamiania testów, nazwę/wzorzec pliku TRX oraz krok publikacji wyników, aby test report był generowany w oczekiwanej lokalizacji.
~~~

Task:
1. Implement the requested code change in this repository, scoped to the task above.
2. You MAY add minimal smoke-level tests if a function is otherwise untestable, but
   comprehensive unit/e2e test coverage is a SEPARATE worker's job — do not over-invest there.
3. Do not merge, do not push, and do not create a pull request — the wrapper script handles that.
4. Do not read or print secrets. Avoid destructive git commands.
5. Before finishing, leave the workspace ready to commit (diff applied on disk).

Output: short summary of changed files and what each change does.