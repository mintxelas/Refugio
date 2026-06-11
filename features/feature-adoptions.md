# Feature: Adoptions

Adoption and foster applications moving through a fixed pipeline, displayed as a kanban board. Status changes notify the applicant by email via a domain event; full edits deliberately do not.

## Entity

### Adoption (aggregate root)

| Property | Type | Notes |
|---|---|---|
| `DogId` / `Dog` | int / nav | the dog applied for |
| `ApplicantName` | string | required |
| `ApplicantEmail` | string? | notification target |
| `ApplicantPhone` | string? | |
| `Type` | `AdoptionType` | `Adoption` or `Foster` |
| `Status` | `AdoptionStatus` | default `Applied` |
| `Notes` | string? | |
| `CreatedAt` | DateTime | UTC now at submit |
| `UpdatedAt` | DateTime? | set on edit / status change |

**Enum `AdoptionStatus` (pipeline order):** `Applied` → `Interview` → `HomeCheck` → `Approved` → `Finalized`; `Rejected` is terminal at any point.

**Behaviors:**
- `Adoption.Submit(...)` — static factory.
- `ChangeStatus(newStatus, notes?)` — sets status, stamps `UpdatedAt`, **raises `AdoptionStatusChanged`** (dispatched after save → `AdoptionStatusChangedHandler` emails the applicant).
- `UpdateDetails(...)` — full edit including status, **silently** (no event/email). This asymmetry is intentional and must be preserved.
- `NextStatus()` — the next pipeline step, or current status when terminal.

## Use cases

### UC-A1: Browse adoption pipeline (kanban)
- **UI:** `/adoptions` — one column per status, per-column "show more" (`?{col}Limit=N`), CSV export link. The page fans out 6 parallel API calls (one per column).
- **API:** `GET /api/adoptions?status=`, `GET /api/adoptions/paged?status=&page=&pageSize=` (default 25).

### UC-A2: Submit an application
- **API:** `POST /api/adoptions` → 201 with `AdoptionDto`.
- **Domain:** `Adoption.Submit(...)`, status starts at `Applied`.

### UC-A3: Advance an application one step
- **UI:** kanban card action.
- **API:** `POST /api/adoptions/{id}/advance` (auth) — moves to `NextStatus()` and redirects `/adoptions`.
- **Side effect:** raises `AdoptionStatusChanged` → applicant email.

### UC-A4: Reject an application
- **API:** `POST /api/adoptions/{id}/reject` (auth) — `ChangeStatus(Rejected)`, redirect `/adoptions`. Applicant email sent.

### UC-A5: Set status directly (REST)
- **API:** `PUT /api/adoptions/{id}/status` body `UpdateAdoptionStatusRequest(Id, Status, Notes)` → 200/404. Raises the event/email.

### UC-A6: Edit application details
- **UI:** `/adoptions/{id}`.
- **API:** `PUT /api/adoptions/{id}` → 200/404.
- **Rule:** `UpdateDetails` may change status but sends **no** email.

### UC-A7: Delete / restore / purge (Manager)
- **API:** `DELETE /api/adoptions/{id}` (204/404), `POST /api/adoptions/{id}/delete` (redirect `/adoptions`), `/restore` and `/purge` (redirect `/admin/deleted?tab=adoptions`). Deleted listings: `GET /api/adoptions/deleted[/{id}]`.

### UC-A8: Export adoptions CSV
- **API:** `GET /api/export/adoptions` (auth) — columns: ID, Applicant Name, Email, Phone, Type, Status, Dog Name, Created, Updated, Notes. RFC-style quoting for commas/quotes/newlines.

### UC-A9: Adoption reports
- **API:** `GET /api/reports/adoption-conversion?year=` (`AdoptionConversionStats`, defaults to current year) and `GET /api/reports/shelter-stay` (`ShelterStayStats` — average stay by breed). Both auth-required; surfaced on `/reports` (see [Dashboard & Reports](feature-dashboard-reports.md)).
