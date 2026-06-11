# Refugio — Feature Specifications

Refugio is a dog-shelter management application (Haven Sanctuary) built with ASP.NET Core 9, Blazor static SSR, and a DDD architecture (Domain / Application / Infrastructure / Web). The UI consumes the app's own REST API over real HTTP via `ShelterApiClient`. Persistence is SQLite via EF Core with soft delete on every entity.

## Feature index

| Spec | Scope |
|---|---|
| [Dogs](feature-dogs.md) | Dog catalog, check-in, profile editing, photo gallery |
| [Medical](feature-medical.md) | Medical records (vet visits), medications, health dashboard, appointment reminders |
| [Adoptions](feature-adoptions.md) | Adoption/foster pipeline (kanban), status notifications, CSV export |
| [Volunteers](feature-volunteers.md) | Volunteer registry, credentials, roles, profile photo |
| [Authentication & Authorization](feature-auth.md) | Cookie login, RBAC (Manager/Volunteer), password change |
| [Tasks](feature-tasks.md) | Day-to-day shelter tasks with assignment and completion |
| [Calendar & Events](feature-calendar-events.md) | Weekly calendar of shelter events |
| [Finance](feature-finance.md) | Donations, expenses (+ receipt photos), fundraising goals, summary, CSV exports |
| [Settings & Branding](feature-settings.md) | Shelter name, phrase, logo upload |
| [Dashboard & Reports](feature-dashboard-reports.md) | Home KPIs, adoption conversion, shelter-stay stats |
| [Admin — Deleted Records](feature-admin-deleted.md) | Soft delete, restore, permanent purge across all entities |
| [Localization](feature-localization.md) | 4 cultures, language switcher, per-user preferred language |

## Cross-cutting rules

- **Domain model:** rich entities with private setters, behavior methods, and static factories under `Refugio.Domain.Entities`. All extend `Entity` (`Id`, `DeletedAt`, domain-event queue, `Restore()`).
- **Soft delete:** `SoftDeleteInterceptor` converts `Remove()` into `DeletedAt = UtcNow`; EF global query filters hide deleted rows everywhere except admin/recovery queries (`IgnoreQueryFilters`).
- **Unit of Work:** repositories never save; `IUnitOfWork.SaveChangesAsync()` is the single commit point and dispatches domain events after a successful save.
- **API as contract:** every page renders by calling `/api/*` endpoints with forwarded cookies, so auth/RBAC/serialization apply to in-app calls exactly as to external consumers. DTOs in `Refugio.Application.Contracts` are the wire contract.
- **Enums stored as strings** (`HasConversion<string>()`); C# member names are permanent identifiers and the JSON representation.
- **Blazor pure static SSR:** no interactive directives; every mutation is a POST form with antiforgery, server-side validation, then redirect (POST-redirect-GET).
- **Authorization:** all pages require login; destructive operations (delete/restore/purge) require the `Manager` policy.
