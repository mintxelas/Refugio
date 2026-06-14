# Remove Dead Code — Summary

## What changed
| Item | Action |
|---|---|
| `SettingsApiTests.VolunteerClientAsync` | Removed unused private test helper (IDE0051). |
| `coverage/` (135 files, 3.4 MB) | Untracked from git; generated HTML coverage report. |
| `coverage/` in `.gitignore` | Added so the report is never re-committed. |
| `wwwroot/lib/bootstrap/**` (44 files) | Removed Blazor-era Bootstrap; SPA uses Tailwind CDN. |
| `wwwroot/app.css` | Removed unreferenced Blazor-era stylesheet. |

## Why
- The C# codebase has **no dead symbols** — proven via codegraph zero-edge query
  cross-checked with textual reference counts, plus a Roslyn IDE0051/IDE0052 pass.
  Only one unused private member existed (a test helper).
- The real dead weight was **committed generated/legacy static assets**: a 3.4 MB
  HTML coverage report and the leftover Bootstrap CSS/JS from the deleted Blazor UI.

## Side effects / follow-ups
- None functional. `dotnet build` succeeds; Settings integration tests 7/7 pass.
- **Pre-existing, unrelated failures remain** (NOT caused by this change): every
  Expense-creating test fails with `Tax lines are required` /
  `An expense must have between 1 and 5 tax lines`, introduced by the recent
  `ExpenseTaxLines` feature (commit `e15ad83`). Verified these fail on clean HEAD
  with the changes stashed. Recommend a separate fix for the expense test fixtures.

## Code-review checklist
1. Confirm nothing references `app.css` or `/lib/bootstrap/` — check
   `ClientApp/index.html` and any static-file links (grep returned zero).
2. Confirm `coverage/` is generated output, not hand-authored docs (it is a
   ReportGenerator HTML dump).
3. Confirm `wwwroot/dogs/1.png` was correctly **kept** (runtime upload/seed path).
4. Re-run `dotnet build` and `dotnet test tests/Refugio.Tests.Integration/
   --filter SettingsApiTests` — both green.
5. The temporary `.editorconfig` used to surface analyzers was deleted — verify it
   is not present in the diff.
