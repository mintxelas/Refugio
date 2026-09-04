# Summary — Urgent meds drill-down

## What changed
- **Backend**: new `UrgentMedicationDto` contract, `IDashboardQueries.GetUrgentMedicationsAsync()`
  (reuses the same "active + ending within 3 days" filter as the dashboard's
  `UrgentMeds` count), and a new authenticated endpoint
  `GET /api/reports/urgent-medications`.
- **Frontend**: new `UrgentMedications` page at `/health/urgent-medications`,
  listing each dog + medication needing attention, each linking to that
  medication's detail page. The dashboard's "Meds urgentes" KPI icon now
  links here instead of the generic `/health` page.
- **Tests**: a unit test for the new query and an integration test proving
  the endpoint rejects unauthenticated requests.

## Why
The red-cross "Meds urgentes" icon showed a count but gave no way to see
*which* dogs needed attention without manually searching. Clicking it now
takes the user straight to that list.

## Security
- Endpoint requires authentication (`RequireAuthorization()`), same as every
  other data endpoint in the API group.
- CSRF: the endpoint is a GET, and the app's CSRF filter (`Program.cs`)
  only validates non-safe HTTP methods (POST/PUT/PATCH/DELETE) — GETs never
  carry state-changing risk, so no CSRF token is required or expected here,
  consistent with the existing `/api/reports/upcoming-visits` endpoint.
- Page sits inside the app's existing `<RequireAuth>` route wrapper, same as
  every other authenticated page — no bespoke guard was written.

## Side effects / follow-ups
- None to existing behavior — `DashboardStats.UrgentMeds` still computes the
  same way (its implementation now delegates to the shared query helper).
- The urgent-medication window (3 days) is hardcoded, matching the existing
  dashboard stat. If that threshold ever needs to become configurable,
  update `UrgentMedicationsQuery` in `ReadQueries.cs` in one place.

## Verify during code review
- `UrgentMedicationsQuery` predicate in `ReadQueries.cs` matches the one
  previously inlined in `GetStatsAsync` (no behavior drift).
- New endpoint has no `.AllowAnonymous()` and sits inside the `api` group
  (inherits `RequireAuthorization()` + the CSRF filter from `Program.cs:142`).
- New route `/health/urgent-medications` is declared inside the
  `<RequireAuth>` block in `App.tsx`, not outside it.
- `types.ts` `UrgentMedicationDto` field names match the C# DTO exactly
  (camelCase via the app's JSON settings).
