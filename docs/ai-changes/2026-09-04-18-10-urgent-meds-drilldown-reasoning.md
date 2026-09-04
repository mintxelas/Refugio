# Reasoning — Urgent meds drill-down

## Problem
The dashboard's "Meds urgentes" KPI card icon linked to `/health`, a generic
page requiring the user to manually pick a dog from a dropdown. It gave no
direct path to the specific dogs whose medication is about to run out.

## Investigation
- Found the KPI card in `Home.tsx`, wired via `KpiCard` with `href="/health"`.
- The urgent-meds count itself comes from `DashboardQueries.GetStatsAsync()`
  in `ReadQueries.cs`: `IsActive && EndDate <= UtcNow.AddDays(3)`.
- No existing query or endpoint exposed the *list* behind that count — only
  the aggregate number (`DashboardStats.UrgentMeds`).
- `IMedicalQueries.GetUpcomingVisitsAsync` was the closest existing pattern
  (a read-model list, injected directly into an endpoint, `RequireAuthorization()`,
  GET so no CSRF token needed under the app's double-submit-cookie CSRF filter,
  which only validates non-safe HTTP methods).

## Decisions
1. **Reused the exact same filter predicate** used for the `UrgentMeds` count
   (extracted into `UrgentMedicationsQuery`) so the list total always matches
   the KPI number — no drift between the two.
2. **Added `GetUrgentMedicationsAsync()` to `IDashboardQueries`** rather than
   `IMedicalQueries`, since the concept ("urgent meds") is already owned by
   `DashboardQueries`/`DashboardStats`.
3. **New endpoint `GET /api/reports/urgent-medications`**, `RequireAuthorization()`
   only (matches `/api/reports/upcoming-visits`). No explicit CSRF attribute
   needed: the app's antiforgery filter in `Program.cs` only validates
   non-GET/HEAD/OPTIONS/TRACE requests, and the endpoint sits inside the
   already-CSRF-filtered `/api` group.
4. **New page `UrgentMedications.tsx`** at route `/health/urgent-medications`,
   placed inside the existing `<RequireAuth>` wrapper in `App.tsx` alongside
   every other authenticated page — no new auth mechanism invented.
5. **Repointed the KPI card's icon link** from `/health` to
   `/health/urgent-medications`.

## Alternatives considered
- *Filter the existing `/health` page by a query param.* Rejected: `Health`
  already uses a `dogId` query param for a different purpose (single-dog
  medical-record editor), and overloading it would make both flows unclear.
- *Add the list to `DashboardStats` itself.* Rejected: would bloat the
  lightweight dashboard payload every time it's polled, when the list is only
  needed on drill-down.

## Testing
- Unit test `DashboardQueriesTests.GetUrgentMedications_ReturnsActiveMedsEndingWithinThreeDays`
  proves the query only returns medications ending within 3 days and that the
  count matches `DashboardStats.UrgentMeds`.
- Integration test `SecurityTests.AnonGet_UrgentMedications_Returns401OrRedirect`
  proves the new endpoint is not reachable anonymously.
- Verified manually end-to-end with Playwright: logged in, clicked the KPI
  icon, landed on the new page, saw the seeded urgent medication, and its
  link navigated to the medication detail page.
