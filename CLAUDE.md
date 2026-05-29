# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Commands

```bash
# Build entire solution
dotnet build

# Run the web app (http://localhost:5110)
cd src/Refugio.Web && dotnet run

# Build a specific project
dotnet build src/Refugio.Web/Refugio.Web.csproj

# Restore packages
dotnet restore

# Add EF Core migration
dotnet ef migrations add <Name> --project src/Refugio.Infrastructure --startup-project src/Refugio.Web

# Apply migrations manually
dotnet ef database update --project src/Refugio.Infrastructure --startup-project src/Refugio.Web
```

No test suite yet. The SQLite database (`shelter.db`) is auto-created on first run inside `src/Refugio.Web/`. Migrations run automatically at startup via `db.Database.Migrate()` in `Program.cs`.

---

## Architecture

Four projects, strict one-way dependency flow:

```
Domain → Infrastructure → Application → Web
```

| Project | Role |
|---|---|
| `Refugio.Domain` | Pure entity classes, enums, `PasswordHelper`. Zero dependencies. |
| `Refugio.Infrastructure` | EF Core + SQLite (`ShelterDbContext`). EF migrations in `Migrations/`. `SeedData.Seed()` runs on first boot. |
| `Refugio.Application` | Akka.NET actor system. One actor per domain area. `ShelterActorService` is the singleton bridge. |
| `Refugio.Web` | ASP.NET 9. Hosts both REST API (`/api/*` minimal API) and Blazor SSR pages. `DogHelpers` static class for shared display logic. |

### Domain entities

`Dog`, `MedicalRecord`, `Medication`, `Adoption`, `ShelterTask`, `Donation`, `Expense`, `Volunteer`, `ShelterEvent`.

All entities have a `DeletedAt DateTime?` property. EF global query filters in `ShelterDbContext.OnModelCreating` exclude soft-deleted records from all queries automatically — no callers need to filter manually.

`ShelterTask` has both `AssignedTo string?` (legacy free-text) and `AssignedVolunteerId int?` + `AssignedVolunteer Volunteer?` (FK nav prop). New code should use the FK.

### Actors

| Actor | Handles |
|---|---|
| `DogActor` | Dogs, medical records, medications, photo upload, dashboard stats |
| `AdoptionActor` | Adoption/foster applications and status pipeline |
| `FinanceActor` | Donations and expenses, paginated retrieval, CSV export |
| `VolunteerActor` | Volunteers, volunteer status, shelter events |
| `TaskActor` | Shelter tasks, volunteer assignment |

`ShelterSupervisorActor` spawns all child actors. `ShelterActorService` (singleton) blocks on startup using `Supervisor.Ask<ActorIdentity>(new Identify("probe"), 10s)` — waits for the supervisor's constructor to complete before resolving child actor refs. No arbitrary `Task.Delay`.

### Blazor ↔ API integration

`ShelterApiClient` (scoped) calls `ShelterActorService` (singleton) in-process via Akka `Ask<T>` (10 s timeout). **No HttpClient, no HTTP round-trip** between Blazor pages and the API. The REST API (`/api/*`) exists for future external consumers.

---

## Akka.NET actor pattern — critical

Actors are singleton-lifetime; `ShelterDbContext` is scoped. Every actor receives `IServiceScopeFactory` and creates a scope per message handler:

```csharp
private async Task Handle(SomeMessage msg)
{
    using var scope = _scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
    // use db — scope disposes after handler returns
}
```

**Adding a new actor:** implement `ReceiveActor`, register in `ShelterSupervisorActor` constructor, expose ref in `ShelterActorService`, add methods to `ShelterApiClient`.

---

## Blazor SSR — critical constraints

Pure static SSR — no interactive render mode. `@onclick`, `@bind`, and all interactive Razor directives are **silently ignored**. Use only:

### Forms
```razor
<form method="post" @formname="unique-name">
    <AntiforgeryToken />
    ...
</form>
```
Read in `OnInitializedAsync` via `IHttpContextAccessor`. **Do NOT use `[SupplyParameterFromForm]`** — it silently drops fields that can't bind (e.g. empty string → `int`):
```csharp
var ctx = HttpContextAccessor.HttpContext;
if (ctx?.Request.Method == "POST")
{
    var form = await ctx.Request.ReadFormAsync();
    var val = form["FieldName"].ToString();
}
```

### Delete actions
Delete buttons are POST forms with antiforgery tokens, **not GET links**. Pattern used across all pages:
```razor
<form method="post" action="/api/resource/@item.Id/delete">
    <AntiforgeryToken />
    <button type="submit" onclick="return confirm('...')">Delete</button>
</form>
```
All action endpoints require `.RequireAuthorization()` and destructive ones additionally require `.RequireAuthorization("Manager")`.

### Other SSR patterns
- **Query params / filters:** `[SupplyParameterFromQuery]` + `<a href="/page?param=x">` links.
- **Collapsible sections:** `<details>/<summary>` — no JS needed.
- **Clickable rows:** absolute `<a class="absolute inset-0">` inside `relative` container; content uses `pointer-events-none`; action buttons use `relative z-10`.
- **POST-then-redirect:** call `Nav.NavigateTo(...)` after processing to issue a 302 and prevent re-submit on refresh.
- **Multiple forms on one page:** check `form["_handler"].ToString()` (injected by `@formname`) to distinguish which form was submitted.
- **Tabs:** `[SupplyParameterFromQuery]` + query param links; active tab by string comparison.
- **Pagination:** query param `?page=N`; actors expose paged messages (`GetDogsPaged`, `GetDonationsPaged`).

---

## Authentication and authorization

Cookie-based (`CookieAuthenticationDefaults`). Sessions last 7 days (sliding expiry).

| Endpoint | Method | Notes |
|---|---|---|
| `/auth/login` | POST | `.DisableAntiforgery()` |
| `/auth/logout` | GET | `.DisableAntiforgery()` |
| `/auth/change-password` | POST | `.DisableAntiforgery()` |

All Blazor pages: `@attribute [Authorize]`. Unauthenticated → redirect to `/login` via `<AuthorizeRouteView>` + `<RedirectTo>` in `Routes.razor`.

Password hashing: PBKDF2-SHA256 via `PasswordHelper` in `Refugio.Domain.Helpers` (no external packages).

### Roles

`Volunteer.Role` is either `"Manager"` or `"Volunteer"` (normalized at startup in `Program.cs`). `Volunteer.CanLogin` enables login; `Volunteer.PasswordHash` stores the hash.

Seed login: `elena@havensanctuary.org` / `shelter123` (Manager role).

Authorization policy `"Manager"` (`opts.AddPolicy("Manager", p => p.RequireRole("Manager"))`) restricts destructive actions. Non-manager users see delete buttons hidden or disabled via `AuthorizeView`. API endpoints use `.RequireAuthorization("Manager")`.

`MainLayout.razor` reads user claims via `IHttpContextAccessor` to show username + dropdown (Change Password, Sign Out).

---

## Localization

Two supported cultures: `en-US` (default) and `es-ES`. Culture is persisted in a cookie via `CookieRequestCultureProvider`.

- Switch language: `GET /set-language?culture=en-US&returnUrl=/current-page`
- Resource files: `src/Refugio.Web/Resources/SharedResources.resx` (English) and `SharedResources.es-ES.resx` (Spanish)
- Marker class: `src/Refugio.Web/SharedResources.cs`
- Global injection in `_Imports.razor`: `@inject IStringLocalizer<SharedResources> L`
- Use `@L["Key"]` in markup, `L["Key"].Value` in C# code, `string.Format(L["Key"].Value, arg)` for parameterized strings

**Adding a new string:** add `<data name="Key">` to both RESX files. Never hardcode UI text in Razor files.

Language switcher is in `MainLayout.razor` — CSS `group-hover` dropdown, no JS.

---

## Styling

Tailwind CSS via CDN (`App.razor`). Design tokens (colors, spacing, fonts) defined inline in the `<script>` block of `App.razor` following Material Design 3 naming: `primary`, `secondary`, `tertiary`, `surface-*`, `on-*`, `*-container`, `*-fixed`. **Never use arbitrary hex values** — always use the named tokens.

---

## Pages and routes

| Page | Route | Notes |
|---|---|---|
| Home / Dashboard | `/` | KPI cards, upcoming tasks, recent dogs |
| Dog catalog | `/dogs` | Search + status filter + pagination |
| Dog detail | `/dogs/{id}` | Medical history, medications, adoption/health links, photo upload |
| Dog edit | `/dogs/{id}/edit` | Edit all fields incl. status |
| Dog check-in | `/dogs/new` | Same component as Dog detail (`IsNew` flag) |
| Medical record edit | `/dogs/{dogId}/medical/{id}` | — |
| Medication edit | `/dogs/{dogId}/medications/{id}` | Includes IsActive toggle |
| Health dashboard | `/health` | Add records/medications; dog selected via `?dogId=` |
| Adoptions | `/adoptions` | Kanban board (Applied→Interview→HomeCheck→Approved→Finalized) + CSV export |
| Adoption edit | `/adoptions/{id}` | Edit all fields incl. status |
| Calendar | `/calendar` | Weekly grid + upcoming list; `?week=yyyy-MM-dd` |
| Event edit | `/calendar/events/{id}` | — |
| Funds | `/funds` | Tabs: donations / expenses / summary chart + pagination + CSV export |
| Donation edit | `/funds/donations/{id}` | — |
| Expense edit | `/funds/expenses/{id}` | — |
| Volunteers | `/volunteers` | Filter by status |
| Volunteer edit | `/volunteers/{id}` | Includes login credentials and role assignment |
| Change password | `/change-password` | Authenticated users only |
| Login | `/login` | BlankLayout, no auth required |

---

## Key design decisions and tradeoffs

### Akka.NET as the application layer
**Decision:** Route all business logic through Akka.NET actors rather than using plain service classes.
**Why:** The shelter had a stated desire for an eventually-concurrent model (multiple simultaneous users, possible future background jobs). Actors give natural single-writer-per-entity concurrency and a clear place for future event sourcing or reactive messaging.
**Tradeoff:** Significant boilerplate (message records, actor registration, scope-per-handler pattern). For a CRUD app at this scale, plain scoped services (`IRepository<T>`) would have been simpler. The actor overhead is mostly invisible at this traffic level.
**Options discarded:** Plain `IRepository<T>` services (simpler but no future concurrency story); MediatR (still synchronous, adds a package for little gain at this scale).

### Pure Blazor SSR (no interactivity)
**Decision:** No `@rendermode InteractiveServer` or `@rendermode InteractiveWebAssembly` anywhere.
**Why:** Avoids SignalR dependency and WebSocket state management; works correctly behind reverse proxies and CDNs; zero JavaScript runtime complexity.
**Tradeoff:** Every user action is a full HTTP round-trip. Filtering, sorting, and multi-step flows require query params and redirects. UI is noticeably less fluid than a SPA or interactive Blazor app. Workarounds (`<details>` for toggles, `onclick="return confirm()"` for JS confirms) are scattered across pages.
**Options discarded:** Full SPA (React/Vue) — too far from the Blazor skillset; Interactive Blazor globally — SignalR state management complexity at scale; HTMX — would require more JS tooling and breaks the pure .NET story.
**If revisiting:** Adding `@rendermode InteractiveServer` per-component (not globally) to forms and kanban boards would dramatically improve UX with minimal structural change.

### EF Core migrations
**Decision:** Use `db.Database.Migrate()` at startup with proper EF Core migrations in `src/Refugio.Infrastructure/Migrations/`.
**Why:** Replaces the earlier try/catch `ALTER TABLE` approach. Migrations are idempotent, support column renames, and produce a complete schema history.
**Tradeoff:** Migration files must be generated and committed for every schema change (`dotnet ef migrations add`). No automatic schema inference.
**Options discarded:** try/catch ALTER TABLE (kept silently failing on renames/type changes); Fluent Migrator (adds a dependency without meaningful benefit over EF's built-in tooling).

### Soft delete via `DeletedAt` + EF global filters
**Decision:** All entities have `DeletedAt DateTime?`; deleted records are excluded by EF global query filters in `ShelterDbContext`, not hard-deleted.
**Why:** Audit trail, accidental-deletion recovery, referential integrity (cascade hard-delete would lose adoption/medical history when a dog is "removed").
**Tradeoff:** Global filters are invisible — a future developer may be confused why a queried record "doesn't exist." Filters must be explicitly ignored with `.IgnoreQueryFilters()` for admin views. Foreign key constraints still apply to soft-deleted rows.
**Options discarded:** Hard delete with archive table — more complex schema; no delete at all — UI becomes cluttered with inactive records.

### `IHttpContextAccessor` for form data
**Decision:** Read POST form fields manually via `ctx.Request.ReadFormAsync()` rather than `[SupplyParameterFromForm]`.
**Why:** `[SupplyParameterFromForm]` silently fails (no exception, just null/default) when a form field value cannot be parsed to its bound property type (e.g. empty string to `int`, or empty string to `decimal`). This caused data-loss bugs during development.
**Tradeoff:** Manual `form["FieldName"].ToString()` is verbose and loses compile-time safety on field names.

### POST forms for destructive actions
**Decision:** Delete buttons are POST forms with antiforgery tokens across all pages.
**Why:** GET requests with side effects violate HTTP semantics and are vulnerable to CSRF via `<img src>` / prefetch attacks. POST + antiforgery is correct HTTP usage.
**Options discarded:** GET delete links with `.RequireAuthorization()` (earlier approach — mitigated CSRF risk but semantically wrong); JavaScript fetch (requires JS, breaks pure SSR story).

### Role-based access control
**Decision:** `Volunteer.Role` is `"Manager"` or `"Volunteer"`. Destructive actions (delete, deactivate) are restricted via `[Authorize(Policy = "Manager")]` on API endpoints and `<AuthorizeView Roles="Manager">` in Razor pages.
**Why:** Multiple volunteers access the system; not all should be able to delete records or deactivate colleagues.
**Tradeoff:** Role is a plain string on `Volunteer`, not a separate `Role` entity. Adding fine-grained permissions would require a role/permission table.
**Options discarded:** Per-resource ownership checks (too complex for this use case); Claims-based permissions without roles (overkill for two access levels).

### Single `SharedResources` for all localization keys
**Decision:** One RESX file pair for the whole app rather than per-page or per-feature resource files.
**Why:** Simpler — one place to add keys, no namespace confusion with `IStringLocalizer<T>` generics.
**Tradeoff:** The file grows large (250+ keys). Key naming discipline (`Section_KeyName`) is critical to avoid collisions.

---

## What has been implemented

These items were originally listed as "recommended next steps" and have since been completed:

- **EF Core migrations** — replaced try/catch ALTER TABLE; migrations in `src/Refugio.Infrastructure/Migrations/`
- **Actor startup fix** — `Task.Delay(500).Wait()` replaced with `Supervisor.Ask<ActorIdentity>(new Identify("probe"), 10s)`
- **Photo upload for dogs** — `POST /api/dogs/{id}/photo`; wired in `DogEdit.razor`
- **Role-based access control** — `Manager` / `Volunteer` roles; destructive actions restricted to Manager
- **Task assignment to volunteers** — `ShelterTask.AssignedVolunteerId` FK to `Volunteer`; dropdown in Home task creation
- **Pagination** — Dogs catalog and Donations tab; paged actor messages
- **CSV export** — Adoptions, Donations, Expenses
- **Soft delete** — `DeletedAt` on all entities + EF global query filters
- **POST forms for delete** — replaced GET delete links across all pages
- **`DogHelpers` static class** — `DogStatusDisplay`, `AgeDisplay`, `StatusChipClass`, `StatusIcon` extracted to `src/Refugio.Web/Helpers/DogHelpers.cs`; injected via `_Imports.razor`

---

## Recommended next steps

### Code quality / architecture

1. **Add server-side input validation.** The app relies on HTML `required` / `type="number"` attributes, which are bypassed by direct HTTP requests. Add validation in each form handler in Blazor pages and in API endpoints. Consider a shared `ValidationResult` pattern passed back through actor messages.

2. **Clean up `ShelterTask.AssignedTo`.** The entity has both the legacy `AssignedTo string?` and the new `AssignedVolunteerId int?`. Remove `AssignedTo`, add a migration, and update any display code still reading the string field.

3. **Paginate remaining lists.** Adoptions, Volunteers, Medical Records, and Events all load all records. Add `GetAdoptionsPaged` / `GetVolunteersPaged` messages following the same pattern as `GetDogsPaged`.

4. **Add a test project.** The actor/message pattern is well-suited to unit testing actors in isolation with `Akka.TestKit`. Priority order: `DogActor` (most message types), `FinanceActor` (CSV export logic), `TaskActor` (volunteer assignment).

5. **Replace `IHttpContextAccessor` form reading with a thin model binder.** The current manual `form["Field"].ToString()` approach is correct but verbose. A typed form-reading helper (e.g. `FormReader.GetInt(form, "Field", defaultValue)`) would reduce repetition without breaking the SSR contract.

6. ~~**Investigate `ShelterTask.ShelterTaskId` duplicate.**~~ Verified: entity only has `Id`, which is the EF PK. No duplicate. Closed.

### Testing strategy

- **Unit tests:** `Akka.TestKit` for actors — test message handling in isolation with a `TestProbe` for the DB scope factory. No Blazor/HTTP involved.
- **Integration tests:** `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) with an in-memory SQLite database. Test the full request pipeline for critical paths (login, dog create, adoption status advance).
- **No E2E browser tests needed** for SSR-only pages — integration tests cover the same surface area without Playwright overhead.

### Functionality

1. **Email notifications.** Adoption status changes and upcoming medical appointments are obvious trigger points. Add `IEmailSender` (ASP.NET Core built-in interface) backed by SMTP or Resend. Wire into `AdoptionActor` on status advance.

2. **Expense pagination and CSV.** Expenses tab in `/funds` currently loads all records. Follows the same `GetExpensesPaged` pattern as donations.

3. **Multi-language expansion.** The localization infrastructure is in place. Adding a third language (e.g. `ca-ES` Catalan) requires only a new RESX file — no code changes.

4. **Admin view for soft-deleted records.** No UI currently shows deleted dogs, adoptions, or volunteers. A `/admin/deleted` page using `.IgnoreQueryFilters()` would allow recovery.

5. **Dog intake form improvements.** Currently `dogs/new` reuses `DogDetail.razor` with `IsNew` flag — the form shows all fields including status. Consider a simpler intake wizard that defaults status to `Available` and hides rarely-used fields on first entry.

6. **Reporting dashboard.** The finance summary chart exists. Extend with: adoption conversion rate by month, average shelter stay by breed, volunteer hours per month (requires a `VolunteerHours` entity or `ShelterEvent` attendance tracking).
