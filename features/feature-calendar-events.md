# Feature: Calendar & Events

Scheduled shelter events (walks, adoption days, vet checkups…) shown on a weekly calendar grid.

## Entity

### ShelterEvent (aggregate root)

| Property | Type | Notes |
|---|---|---|
| `Title` | string | required |
| `StartDateTime` | DateTime | |
| `EndDateTime` | DateTime | |
| `Location` | string? | |
| `Description` | string? | |
| `EventType` | string | free text, default `"General"` |
| `AssignedVolunteers` | int? | headcount, not a relation |

**Behaviors:** `ShelterEvent.Schedule(...)` (static factory), `Update(...)`.

## Use cases

### UC-E1: View weekly calendar
- **UI:** `/calendar` — weekly grid, `?week=yyyy-MM-dd` to navigate.
- **API:** `GET /api/events?from=&to=` — date-range filter.

### UC-E2: Schedule an event
- **API:** `POST /api/events` → 201 with `EventDto`.

### UC-E3: Edit an event
- **UI:** `/calendar/events/{id}`.
- **API:** `GET /api/events/{id}`, `PUT /api/events/{id}` → 200/404.

### UC-E4: Delete an event
- **API:** `DELETE /api/events/{id}` (204/404) or `POST /api/events/{id}/delete` (Manager, redirect `/calendar`). Soft delete.
