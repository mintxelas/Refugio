# Feature: Dashboard & Reports

Cross-aggregate aggregations exposed as CQRS-lite read models: interfaces in `Refugio.Application.Queries` (`IReadQueries.cs`), EF LINQ implementations in `Refugio.Infrastructure.Queries.ReadQueries`, injected straight into endpoints (no service layer).

## Read models

| Interface | Method | Returns | Consumed by |
|---|---|---|---|
| `IDashboardQueries` | `GetStatsAsync()` | `DashboardStats` | `GET /api/dashboard` → home page KPIs |
| `IFinanceQueries` | `GetSummaryAsync(year)` | `FinanceSummary` | `GET /api/finances/summary?year=` → `/funds` summary tab |
| `IAdoptionQueries` | `GetConversionStatsAsync(year)` | `AdoptionConversionStats` | `GET /api/reports/adoption-conversion?year=` → `/reports` |
| `IAdoptionQueries` | `GetShelterStayStatsAsync()` | `ShelterStayStats` | `GET /api/reports/shelter-stay` → `/reports` |
| `IVolunteerQueries` | `GetCountsAsync()` | `VolunteerCounts` | `GET /api/volunteers/counts` → `/volunteers` cards |
| `IVolunteerQueries` | `GetManagerEmailsAsync()` | `List<string>` | vet appointment reminders (internal) |
| `IMedicalQueries` | `GetUpcomingVisitsAsync(daysAhead)` | `List<UpcomingVisit>` | reminder hosted service (internal) |

## Use cases

### UC-R1: Home dashboard
- **UI:** `/` — KPI cards, upcoming tasks, recent dogs.
- **API:** `GET /api/dashboard` (`DashboardStats`), plus task and dog list calls.
- **Rule:** donation-goal percentage is guarded against `DivideByZeroException` when the Goals table is empty (renders 0%).

### UC-R2: Adoption conversion report
- **UI:** `/reports` — conversion by month, `?year=N` selector.
- **API:** `GET /api/reports/adoption-conversion?year=` (auth, defaults to current year).

### UC-R3: Shelter stay report
- **UI:** `/reports` — average stay by breed.
- **API:** `GET /api/reports/shelter-stay` (auth).

### UC-R4: Volunteer counts
- **API:** `GET /api/volunteers/counts` — totals per status for the `/volunteers` cards.
