# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Commands

```bash
# Build entire solution
dotnet build

# Run the web app (http://localhost:5110)
cd src/Refugio.Web && dotnet run

# Run unit tests (102 tests, Akka.TestKit + in-memory EF)
dotnet test tests/Refugio.Tests/

# Run integration tests (16 tests, WebApplicationFactory + SQLite in-memory)
dotnet test tests/Refugio.Tests.Integration/

# Run all tests
dotnet test

# Build a specific project
dotnet build src/Refugio.Web/Refugio.Web.csproj

# Restore packages
dotnet restore

# Add EF Core migration
dotnet ef migrations add <Name> --project src/Refugio.Infrastructure --startup-project src/Refugio.Web

# Apply migrations manually
dotnet ef database update --project src/Refugio.Infrastructure --startup-project src/Refugio.Web
```

The SQLite database (`shelter.db`) is auto-created on first run inside `src/Refugio.Web/`. Migrations run automatically at startup via `db.Database.Migrate()` in `Program.cs` (or `EnsureCreated()` when the EF provider is in-memory, detected via `db.Database.ProviderName`).

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
| `Refugio.Web` | ASP.NET 9. Hosts both REST API (`/api/*` minimal API) and Blazor SSR pages. `DogHelpers` and `FormReader` static helpers in `Helpers/`. |

Two test projects:

| Project | Role |
|---|---|
| `tests/Refugio.Tests` | Unit tests — Akka.TestKit actors with in-memory EF. `ActorTestBase` wires up `IServiceScopeFactory` with a unique in-memory DB per test. |
| `tests/Refugio.Tests.Integration` | Integration tests — `WebApplicationFactory` + SQLite in-memory connection (not EF in-memory, to avoid dual-provider conflict). `ShelterWebFactory` replaces the `DbContextOptions` registration and keeps a `SqliteConnection` open for the factory lifetime. |

### Domain entities

`Dog`, `MedicalRecord`, `Medication`, `Adoption`, `ShelterTask`, `Donation`, `Expense`, `Volunteer`, `ShelterEvent`.

All entities have a `DeletedAt DateTime?` property. EF global query filters in `ShelterDbContext.OnModelCreating` exclude soft-deleted records from all queries automatically — no callers need to filter manually.

`ShelterTask` uses only `AssignedVolunteerId int?` + `AssignedVolunteer Volunteer?` (FK nav prop). The legacy `AssignedTo string?` field was removed in migration `RemoveTaskAssignedTo`.

Both `Donation.Category` (`DonationCategory`) and `Expense.Category` (`ExpenseCategory`) are enums stored as strings via `HasConversion<string>()`. This makes existing data human-readable in the DB and allows adding members without a DDL migration.

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
    var val = FormReader.GetString(form, "FieldName");
    var num = FormReader.GetInt(form, "Count");
    var cat = FormReader.GetEnum(form, "Category", ExpenseCategory.Other);
}
```

Use `FormReader` (`src/Refugio.Web/Helpers/FormReader.cs`) for all form field parsing. It provides typed helpers (`GetString`, `GetStringOrNull`, `GetInt`, `GetNullableInt`, `GetDecimal`, `GetDateTime`, `GetBool`, `GetEnum<T>`) that handle empty-string and parse-failure cases consistently. Imported globally via `@using static Refugio.Web.Helpers.FormReader` in `_Imports.razor`.

### Server-side validation

After reading form fields, validate before calling the actor. Accumulate errors in `List<string> _errors`, display above the form, bail if any exist:

```csharp
_errors.Clear();
if (string.IsNullOrWhiteSpace(name)) _errors.Add(L["Validation_NameRequired"].Value);
if (amount <= 0) _errors.Add(L["Validation_AmountPositive"].Value);
if (_errors.Count > 0) return;
```

Add new localization keys to both RESX files when adding new validation messages.

### Delete actions

Delete buttons are POST forms with antiforgery tokens, **not GET links**:

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
- **Pagination:** query param `?page=N`; actors expose paged messages (`GetDogsPaged`, `GetDonationsPaged`, `GetExpensesPaged`, `GetVolunteersPaged`, `GetAdoptionsPaged`).
- **Enum selects:** use `Enum.GetValues<TEnum>()` in a foreach to render `<option>` elements; parse with `FormReader.GetEnum<TEnum>`.

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
| Dog edit | `/dogs/{id}/edit` | Edit all fields incl. status; server-side validation |
| Dog check-in | `/dogs/new` | Same component as Dog detail (`IsNew` flag) |
| Medical record edit | `/dogs/{dogId}/medical/{id}` | Server-side validation |
| Medication edit | `/dogs/{dogId}/medications/{id}` | Includes IsActive toggle; server-side validation |
| Health dashboard | `/health` | Add records/medications; dog selected via `?dogId=` |
| Adoptions | `/adoptions` | Kanban board (Applied→Interview→HomeCheck→Approved→Finalized) + CSV export; loads all records |
| Adoption edit | `/adoptions/{id}` | Edit all fields incl. status; server-side validation |
| Calendar | `/calendar` | Weekly grid + upcoming list; `?week=yyyy-MM-dd` |
| Event edit | `/calendar/events/{id}` | Server-side validation |
| Funds | `/funds` | Tabs: donations / expenses / summary chart + pagination + CSV export |
| Donation edit | `/funds/donations/{id}` | Server-side validation |
| Expense edit | `/funds/expenses/{id}` | Server-side validation |
| Volunteers | `/volunteers` | Filter by status + pagination |
| Volunteer edit | `/volunteers/{id}` | Includes login credentials and role assignment; server-side validation |
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

### `IHttpContextAccessor` + `FormReader` for form data
**Decision:** Read POST form fields via `ctx.Request.ReadFormAsync()` then parse with the static `FormReader` helper rather than `[SupplyParameterFromForm]`.
**Why:** `[SupplyParameterFromForm]` silently fails (no exception, just null/default) when a field value can't be parsed to its bound type (e.g. empty string to `int`). This caused data-loss bugs during development. `FormReader` centralizes parse-failure handling and eliminates repeated `int.TryParse` boilerplate.
**Tradeoff:** Field names are plain strings — no compile-time safety. A typo in `GetInt(form, "Ammount")` fails silently at runtime.

### POST forms for destructive actions
**Decision:** Delete buttons are POST forms with antiforgery tokens across all pages.
**Why:** GET requests with side effects violate HTTP semantics and are vulnerable to CSRF via `<img src>` / prefetch attacks. POST + antiforgery is correct HTTP usage.
**Options discarded:** GET delete links with `.RequireAuthorization()` (earlier approach — mitigated CSRF risk but semantically wrong); JavaScript fetch (requires JS, breaks pure SSR story).

### Role-based access control
**Decision:** `Volunteer.Role` is `"Manager"` or `"Volunteer"`. Destructive actions are restricted via `[Authorize(Policy = "Manager")]` on API endpoints and `<AuthorizeView Roles="Manager">` in Razor pages.
**Why:** Multiple volunteers access the system; not all should be able to delete records or deactivate colleagues.
**Tradeoff:** Role is a plain string on `Volunteer`, not a separate `Role` entity. Adding fine-grained permissions would require a role/permission table.
**Options discarded:** Per-resource ownership checks (too complex for this use case); Claims-based permissions without roles (overkill for two access levels).

### Single `SharedResources` for all localization keys
**Decision:** One RESX file pair for the whole app rather than per-page or per-feature resource files.
**Why:** Simpler — one place to add keys, no namespace confusion with `IStringLocalizer<T>` generics.
**Tradeoff:** The file grows large (300+ keys). Key naming discipline (`Section_KeyName`) is critical to avoid collisions.

### Server-side validation in Blazor pages
**Decision:** Each edit page accumulates errors in `List<string> _errors`, validates after reading the form, and returns early if any errors exist.
**Why:** HTML `required` / `type="number"` attributes are bypassed by direct HTTP requests. Server-side validation is the only reliable guard.
**Tradeoff:** Validation logic is duplicated per page — no shared validator or actor-level `ValidationResult`. A future refactor could extract a shared validation layer, but the current approach keeps each page self-contained and avoids a new abstraction.
**Options discarded:** FluentValidation (adds a package for limited gain at this scale); actor-level `ValidationResult` (cleaner but requires a new response type and error-display protocol per actor message).

### Enum categories stored as strings in SQLite
**Decision:** `DonationCategory` and `ExpenseCategory` enums use `HasConversion<string>()` in `ShelterDbContext` — stored as TEXT, not integer ordinal.
**Why:** SQLite has no enum type; storing as integer would make the DB unreadable without the code. String storage also means adding a new enum member requires no DDL migration — EF generates an empty migration file, and the snapshot updates without any `ALTER TABLE`.
**Tradeoff:** Renaming an enum member is a breaking change — existing string values in the DB won't match the new name. Treat enum member names as permanent identifiers.
**Options discarded:** Integer storage (EF default — unreadable DB, no gain for this use case); separate lookup table (overkill; these categories are stable).

### Integration tests use SQLite in-memory, not EF in-memory provider
**Decision:** `ShelterWebFactory` replaces `DbContextOptions<ShelterDbContext>` with a `SqliteConnection("Data Source=:memory:")`, not `UseInMemoryDatabase`.
**Why:** EF Core registers provider-specific singletons into the DI container when `AddDbContext` is called. When `WebApplicationFactory.ConfigureServices` adds a second provider (in-memory), EF's internal service provider receives both and throws `InvalidOperationException`. Using SQLite in-memory keeps a single provider. It also lets `db.Database.Migrate()` run correctly (in-memory SQLite supports migrations; EF in-memory does not). The open `SqliteConnection` is kept alive on the factory instance and disposed with it, which is required — SQLite in-memory databases are scoped to the connection.
**Tradeoff:** Tests depend on SQLite behavior; a subtle SQLite vs production-SQLite difference could cause a test to pass but code to fail in ways that would never happen in practice (both are SQLite here so risk is minimal).
**Options discarded:** EF in-memory provider (dual-provider conflict); separate test SQLite file (cleanup complexity, parallel-test isolation risk).

---

## What has been implemented

These items were originally listed as recommended next steps and have since been completed:

- **EF Core migrations** — replaced try/catch ALTER TABLE; migrations in `src/Refugio.Infrastructure/Migrations/`
- **Actor startup fix** — `Task.Delay(500).Wait()` replaced with `Supervisor.Ask<ActorIdentity>(new Identify("probe"), 10s)`
- **Photo upload for dogs** — `POST /api/dogs/{id}/photo`; wired in `DogEdit.razor`
- **Role-based access control** — `Manager` / `Volunteer` roles; destructive actions restricted to Manager
- **Task assignment to volunteers** — `ShelterTask.AssignedVolunteerId` FK to `Volunteer`; dropdown in Home task creation
- **`ShelterTask.AssignedTo` cleanup** — legacy free-text field removed; migration `RemoveTaskAssignedTo` applied
- **Pagination** — Dogs, Donations, Expenses, and Volunteers lists; paged actor messages for all four; `GetAdoptionsPaged` exists in actor/messages but Adoptions kanban still uses `GetAllAdoptions` (see next steps)
- **CSV export** — Adoptions, Donations, Expenses
- **Soft delete** — `DeletedAt` on all entities + EF global query filters
- **POST forms for delete** — replaced GET delete links across all pages
- **`DogHelpers` static class** — `DogStatusDisplay`, `AgeDisplay`, `StatusChipClass`, `StatusIcon` in `src/Refugio.Web/Helpers/DogHelpers.cs`
- **`FormReader` static helper** — typed form parsing (`GetString`, `GetInt`, `GetDecimal`, `GetDateTime`, `GetBool`, `GetEnum<T>`) in `src/Refugio.Web/Helpers/FormReader.cs`; replaces scattered `int.TryParse` / `.ToString()` calls
- **Server-side input validation** — all edit pages validate required fields and business rules; errors shown above the form
- **`ExpenseCategory` enum** — `Expense.Category` changed from free-text `string` to `ExpenseCategory` enum (`Medical`, `Food`, `Facilities`, `Supplies`, `Transport`, `Other`), mirroring `DonationCategory`; UI uses `<select>` with `Enum.GetValues`
- **Unit test suite** — `tests/Refugio.Tests`: 102 tests across `DogActor`, `AdoptionActor`, `FinanceActor`, `VolunteerActor`, `TaskActor`, `PasswordHelper`; uses `Akka.TestKit.Xunit2` + EF in-memory
- **Integration test suite** — `tests/Refugio.Tests.Integration`: 16 tests covering auth endpoints, dogs API, and RBAC; uses `WebApplicationFactory` + SQLite in-memory; `ShelterWebFactory` fixed to avoid dual-provider conflict

---

## Recommended next steps

### Code quality / architecture

1. **Paginate the Adoptions kanban board.** `GetAdoptionsPaged` exists in `AdoptionActor` and `ShelterApiClient` but `Adoptions.razor` still calls `GetAllAdoptions`. The kanban groups cards by status client-side, so pagination needs a design choice: paginate per-column (one paged request per status), or cap with a "show more" link per column. Both approaches are supported by the existing actor message.

2. **Compile-time safety for `FormReader` field names.** Strings passed to `GetInt(form, "AgeMonths")` can't be checked at compile time. Options: `nameof`-compatible constants per page; or a source generator that emits typed accessors. Low risk at current scale but a silent failure mode as forms grow.

3. **Expand integration test coverage.** Current integration tests cover: auth, dog CRUD via API, and RBAC delete. Missing: adoption status advance, volunteer create/update, finance CSV export responses, pagination query params. Follow the pattern in `DogsApiTests` / `RbacTests`.

4. **Extract shared validation.** Validation logic is duplicated per Blazor page. A typed `FormModel<T>` with a `Validate()` method per entity, or a shared `ValidationResult`-based actor response, would centralize rules and prevent pages diverging over time.

5. **Remove the double volunteer fetch in `Volunteers.razor`.** The page calls `Api.GetVolunteers()` (all records, for the task-assignment dropdown) then also `Api.GetVolunteersPaged(...)`. If the dropdown only needs active volunteers, the paged result already includes them and the full-load call is redundant.

### Testing strategy

- **Unit tests** cover all actors. Priority additions: `DogActor.UpdateDogPhoto` (currently untested), soft-delete filter verification (confirm global filter excludes deleted records in queries).
- **Integration tests** use `ShelterWebFactory` with `SqliteConnection("Data Source=:memory:")`. Add new test classes as `IClassFixture<ShelterWebFactory>` — the factory is shared across all tests in a class. For tests that mutate state, create a fresh `ShelterWebFactory` per test or seed/clean carefully.
- **No E2E browser tests needed** for SSR-only pages — integration tests cover the full request pipeline without Playwright overhead.

### Functionality

1. **Email notifications.** Adoption status changes and upcoming medical appointments are obvious trigger points. Add `IEmailSender` (ASP.NET Core built-in interface) backed by SMTP or Resend. Wire into `AdoptionActor` on status advance and a background job for appointment reminders.

2. **Admin view for soft-deleted records.** No UI currently shows deleted dogs, adoptions, or volunteers. A `/admin/deleted` page using `.IgnoreQueryFilters()` would allow recovery. Restrict to Manager role.

3. **Dog intake form improvements.** `dogs/new` reuses `DogDetail.razor` with an `IsNew` flag — the form shows all fields including status. A simpler intake form that defaults status to `Available` and hides rarely-used fields on first entry would reduce friction for the most common shelter workflow.

4. **Reporting dashboard.** The finance summary chart exists. Extend with: adoption conversion rate by month, average shelter stay by breed, volunteer activity per month (requires a `VolunteerHours` entity or `ShelterEvent` attendance tracking).

5. **Multi-language expansion.** The localization infrastructure is in place. Adding a third language (e.g. `ca-ES` Catalan) requires only a new RESX file — no code changes.
