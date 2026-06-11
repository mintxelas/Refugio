# Feature: Medical (records, medications, reminders)

Veterinary history per dog: visit records and medication courses, both child entities of the `Dog` aggregate (no own repository — reached through `IDogRepository`). A hosted service emails managers about upcoming vet visits.

## Entities

### MedicalRecord (child of Dog)

| Property | Type | Notes |
|---|---|---|
| `DogId` | int | parent |
| `VisitDate` | DateTime | defaults to UTC now |
| `VetName` | string | required |
| `Diagnosis` | string | required |
| `Treatment` | string | required |
| `Notes` | string? | |
| `NextVisitDate` | DateTime? | drives the upcoming-visit reminder |

**Behaviors:** `MedicalRecord.Create(...)`, `Update(...)`.

### Medication (child of Dog)

| Property | Type | Notes |
|---|---|---|
| `DogId` | int | parent |
| `Name` | string | required |
| `Dosage` | string | required |
| `Frequency` | string | required |
| `StartDate` | DateTime | |
| `EndDate` | DateTime? | |
| `IsActive` | bool | default true |

**Behaviors:** `Medication.Create(...)`, `Update(...)` (includes `IsActive` toggle), `Deactivate()`.

## Use cases

### UC-M1: View a dog's medical history
- **UI:** `/dogs/{id}` (history section) and `/health` dashboard (`?dogId=` filter).
- **API:** `GET /api/dogs/{id}/medical`, `GET /api/dogs/{id}/medications`, `GET /api/medical/{id}`, `GET /api/medications/{id}`.

### UC-M2: Record a vet visit
- **UI:** `/health` (add form) or dog detail.
- **API:** `POST /api/dogs/{id}/medical` → 201, or 404 when the dog doesn't exist.
- **Domain:** `MedicalRecord.Create(...)`; optional `NextVisitDate` schedules a follow-up.

### UC-M3: Edit a vet visit
- **UI:** `/dogs/{dogId}/medical/{id}`.
- **API:** `PUT /api/medical/{id}` → 200/404.

### UC-M4: Start a medication course
- **UI:** `/health` (add form).
- **API:** `POST /api/dogs/{id}/medications` → 201, or 404 when the dog doesn't exist.

### UC-M5: Edit / deactivate a medication
- **UI:** `/dogs/{dogId}/medications/{id}` — includes `IsActive` toggle.
- **API:** `PUT /api/medications/{id}`; `DELETE /api/medications/{id}` **deactivates** the course (kept REST verb for external consumers — not a soft delete).

### UC-M6: Delete / restore / purge (Manager)
- **API:** `POST /api/medical/{id}/delete?dogId=&returnUrl=` and `POST /api/medications/{id}/delete?dogId=&returnUrl=` (redirect back to caller); `/restore` and `/purge` variants redirect to `/admin/deleted?tab=medical|medications`. `DELETE /api/medical/{id}` is the REST soft delete.
- **Deleted listings:** `GET /api/medical/deleted[/{id}]`, `GET /api/medications/deleted[/{id}]` (Manager).
- **Rule:** restore is blocked while the parent dog is deleted (double-guarded: UI disables, service re-checks).

### UC-M7: Upcoming vet visit reminders
- **Read model:** `IMedicalQueries.GetUpcomingVisitsAsync(daysAhead)` — visits with `NextVisitDate` from today up to N days out.
- **Process:** `AppointmentReminderService` (hosted service in Web) wraps `VetAppointmentNotifier`, which emails managers (`IVolunteerQueries.GetManagerEmailsAsync`) via `IShelterEmailSender` (No-Op adapter in dev).
