# Summary — Feature specifications generation

## What changed

Created `features/` folder with 13 new Markdown files (documentation only — no code changed):

- `README.md` — index + cross-cutting rules (soft delete, UoW, API-as-contract, SSR constraints, enum-as-string, authorization).
- `feature-dogs.md` — Dog + DogPhoto entities; catalog, check-in, edit, photo gallery, delete/restore/purge (UC-D1…D7).
- `feature-medical.md` — MedicalRecord + Medication; history, visits, medication courses, deactivate-on-DELETE quirk, vet appointment reminders (UC-M1…M7).
- `feature-adoptions.md` — Adoption entity, pipeline order, event/email rule, kanban, advance/reject, CSV export, reports (UC-A1…A9).
- `feature-volunteers.md` — Volunteer entity, credential invariants, status, photo, login linkage (UC-V1…V7).
- `feature-auth.md` — cookie auth, claims, roles, Manager policy, login/logout/change-password, language at login (UC-AU1…AU5).
- `feature-tasks.md` — ShelterTask (UC-T1…T4).
- `feature-calendar-events.md` — ShelterEvent (UC-E1…E4).
- `feature-finance.md` — Donation, Expense, ExpensePhoto, Goal; tabs, receipt gallery, summary, CSV exports (UC-F1…F8).
- `feature-settings.md` — ShelterSettings; name/phrase update, logo pipeline rules (UC-S1…S3).
- `feature-dashboard-reports.md` — all 7 read-model methods mapped to endpoints/pages (UC-R1…R4).
- `feature-admin-deleted.md` — soft-delete mechanism, restore parent-guard, purge, coverage matrix (UC-X1…X4).
- `feature-localization.md` — 4 cultures, RESX conventions, enum display keys (UC-L1…L3).

Also created the three meta files in `docs/ai-changes/` (this one included).

## Why

Requested: durable specs of every feature, entity, and use case as a reference for future planning, testing, and onboarding.

## Side effects / follow-ups

- None at runtime — documentation only.
- Specs snapshot the code as of 2026-06-11; future endpoint or entity changes need spec updates (no automation wired for that).
- Use-case IDs (UC-xx) are now available to reference in PLAN.md files and test names.

## Code review checklist

1. **Accuracy against code (highest value):** spot-check endpoint tables against `src/Refugio.Web/Endpoints/*.cs` — verbs, routes, auth policies (`RequireAuthorization("Manager")` vs plain), redirect targets.
2. **Entity tables** vs `src/Refugio.Domain/Entities/*.cs` — property names, defaults, enum members.
3. **Behavioral rules:** verify the documented adoption email asymmetry (`ChangeStatus` raises event; `UpdateDetails` silent), the medication `DELETE` = deactivate, the Volunteer no-login → no-hash/no-language invariant, and the parent-liveness restore guard match current code.
4. **Coverage:** confirm no feature missing — compare against the page table in CLAUDE.md (all 20+ routes accounted for across specs).
5. **Upload rules:** extensions/size limits/file-naming documented for dog photos, volunteer photos, expense receipts, and logo match the endpoint code.
