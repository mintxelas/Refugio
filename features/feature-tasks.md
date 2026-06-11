# Feature: Tasks

Day-to-day shelter to-dos with a due time, optional location, and optional assignment to a volunteer. Surfaced on the home dashboard ("upcoming tasks").

## Entity

### ShelterTask (aggregate root)

| Property | Type | Notes |
|---|---|---|
| `Title` | string | required |
| `Notes` | string? | |
| `DueDateTime` | DateTime | |
| `IsCompleted` | bool | default false |
| `Location` | string? | |
| `AssignedVolunteerId` / `AssignedVolunteer` | int? / nav | optional assignee |

**Behaviors:** `ShelterTask.Create(title, dueDateTime, notes?, location?, assignedVolunteerId?)`, `Complete()`.

## Use cases

### UC-T1: List tasks
- **UI:** home dashboard `/` (upcoming tasks widget).
- **API:** `GET /api/tasks?includeCompleted=` (default excludes completed).

### UC-T2: Create a task
- **API:** `POST /api/tasks` → 201 with `TaskDto`.

### UC-T3: Complete a task
- **API:** `PUT /api/tasks/{id}/complete` (REST, 204/404) or `POST /api/tasks/{id}/complete` (UI form, auth, redirect `/`).
- **Domain:** `Complete()` — one-way flag.

### UC-T4: Delete a task
- **API:** `DELETE /api/tasks/{id}` (204/404) or `POST /api/tasks/{id}/delete` (Manager, redirect `/`). Soft delete via interceptor.
