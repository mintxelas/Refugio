# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Commands

```bash
# Build entire solution
dotnet build

# Run the web app (http://localhost:5110)
cd src/Refugio.Web && dotnet run

# Run unit tests (108 tests, Akka.TestKit + in-memory EF)
dotnet test tests/Refugio.Tests/

# Run integration tests (75 tests, WebApplicationFactory + SQLite in-memory)
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

All entities have a `DeletedAt DateTime?` property. EF global query filters in `ShelterDbContext.OnModelCreating` exclude soft-deleted records from all queries automatically — no callers need to filter manually. Use `.IgnoreQueryFilters()` only in admin/recovery contexts.

`ShelterTask` uses only `AssignedVolunteerId int?` + `AssignedVolunteer Volunteer?` (FK nav prop). The legacy `AssignedTo string?` field was removed in migration `RemoveTaskAssignedTo`.

Both `Donation.Category` (`DonationCategory`) and `Expense.Category` (`ExpenseCategory`) are enums stored as strings via `HasConversion<string>()`. This makes existing data human-readable in the DB and allows adding members without a DDL migration.

### Actors

| Actor | Handles |
|---|---|
| `DogActor` | Dogs, medical records, medications, photo upload, dashboard stats, soft-deleted dog recovery |
| `AdoptionActor` | Adoption/foster applications, status pipeline, soft-deleted adoption recovery |
| `FinanceActor` | Donations and expenses, paginated retrieval, CSV export |
| `VolunteerActor` | Volunteers, volunteer status, shelter events, soft-deleted volunteer recovery |
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
- **Kanban "show more":** Adoptions kanban uses `GetAdoptionsPaged` per status column; a `?{status}Limit=N` query param drives column capacity; "Show more" links increment the limit without full pagination.
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

`MainLayout.razor` reads user claims via `IHttpContextAccessor` to show username + dropdown (Change Password, Sign Out). The "Deleted Records" nav link is wrapped in `<AuthorizeView Roles="Manager">` — Volunteers never see it.

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
| Adoptions | `/adoptions` | Kanban by status + per-column "show more" + CSV export |
| Adoption edit | `/adoptions/{id}` | Edit all fields incl. status; server-side validation |
| Calendar | `/calendar` | Weekly grid + upcoming list; `?week=yyyy-MM-dd` |
| Event edit | `/calendar/events/{id}` | Server-side validation |
| Funds | `/funds` | Tabs: donations / expenses / summary chart + pagination + CSV export |
| Donation edit | `/funds/donations/{id}` | Server-side validation |
| Expense edit | `/funds/expenses/{id}` | Server-side validation |
| Volunteers | `/volunteers` | Filter by status + pagination |
| Volunteer edit | `/volunteers/{id}` | Includes login credentials and role assignment; server-side validation |
| Admin — Deleted Records | `/admin/deleted` | Manager-only; tabs: dogs / adoptions / volunteers; restore buttons |
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
**Tradeoff:** The file now exceeds 300 keys. Key naming discipline (`Section_KeyName`) is critical to avoid collisions.
**Options discarded:** Per-page resource files (correct at large scale, overkill here — adds namespace juggling for marginal benefit).

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
**Why:** EF Core registers provider-specific singletons into the DI container when `AddDbContext` is called. When `WebApplicationFactory.ConfigureServices` adds a second provider (in-memory), EF's internal service provider receives both and throws `InvalidOperationException`. Using SQLite in-memory keeps a single provider. It also lets `db.Database.Migrate()` run correctly (in-memory SQLite supports migrations; EF in-memory does not). The open `SqliteConnection` is kept alive on the factory instance and disposed with it — SQLite in-memory databases are scoped to the connection.
**Tradeoff:** Tests depend on SQLite behavior; a subtle SQLite vs production-SQLite difference could cause a test to pass but prod to fail (both are SQLite here so the risk is minimal).
**Options discarded:** EF in-memory provider (dual-provider conflict); separate test SQLite file (cleanup complexity, parallel-test isolation risk).

### Adoptions kanban "show more" instead of traditional pagination
**Decision:** Each status column in the kanban calls `GetAdoptionsPaged` with a per-column limit driven by a query param (`?appliedLimit=N`). "Show more" links increment the limit.
**Why:** True pagination on a kanban is disruptive — volunteers need to see all cards in a column at once, not navigate pages. Per-column capping with show-more matches the UX expectation while preventing unbounded DB queries.
**Tradeoff:** The URL grows one param per column once any column is expanded. State is not preserved across sessions.
**Options discarded:** Full numeric pagination per column (bad UX for kanban); loading all records (unbounded query, O(n) memory); infinite scroll (requires JS).

### Admin deleted records view
**Decision:** `/admin/deleted` page (Manager-only) queries each actor with `GetDeletedDogs/Adoptions/Volunteers` messages that use `.IgnoreQueryFilters()`. Restore buttons POST to `/api/{entity}/{id}/restore`.
**Why:** Soft delete is only useful if recovery is possible. Routing recovery through the same actor/message pattern keeps it consistent with the rest of the codebase. Manager restriction prevents accidental mass-restore by regular volunteers.
**Tradeoff:** Only dogs, adoptions, and volunteers are recoverable via UI. Soft-deleted medical records, medications, events, donations, expenses, and tasks are not shown — they are rarely deleted accidentally and recovering them without their parent context would be confusing.
**Options discarded:** Hard delete with an archive table (more schema complexity); exposing `.IgnoreQueryFilters()` directly in existing list pages (leaks admin concern into user-facing views).

---

## What has been implemented

- **EF Core migrations** — replaced try/catch ALTER TABLE; migrations in `src/Refugio.Infrastructure/Migrations/`
- **Actor startup fix** — `Task.Delay(500).Wait()` replaced with `Supervisor.Ask<ActorIdentity>(new Identify("probe"), 10s)`
- **Photo upload for dogs** — `POST /api/dogs/{id}/photo`; wired in `DogEdit.razor`
- **Role-based access control** — `Manager` / `Volunteer` roles; destructive actions restricted to Manager
- **Task assignment to volunteers** — `ShelterTask.AssignedVolunteerId` FK to `Volunteer`; dropdown in Home task creation
- **`ShelterTask.AssignedTo` cleanup** — legacy free-text field removed; migration `RemoveTaskAssignedTo` applied
- **Pagination** — Dogs, Donations, Expenses, Volunteers lists; paged actor messages for all four
- **Adoptions kanban "show more"** — per-column capped queries via `GetAdoptionsPaged`; `?{status}Limit=N` query params
- **CSV export** — Adoptions, Donations, Expenses
- **Soft delete** — `DeletedAt` on all entities + EF global query filters
- **Admin deleted records view** — `/admin/deleted`; Manager-only; tabs for dogs/adoptions/volunteers; restore POST endpoints; `GetDeleted*` + `Restore*` messages in each actor using `.IgnoreQueryFilters()`
- **POST forms for delete** — replaced GET delete links across all pages
- **`DogHelpers` static class** — `DogStatusDisplay`, `AgeDisplay`, `StatusChipClass`, `StatusIcon` in `src/Refugio.Web/Helpers/DogHelpers.cs`
- **`FormReader` static helper** — typed form parsing (`GetString`, `GetInt`, `GetDecimal`, `GetDateTime`, `GetBool`, `GetEnum<T>`) in `src/Refugio.Web/Helpers/FormReader.cs`; replaces scattered `int.TryParse` / `.ToString()` calls
- **Server-side input validation** — all edit pages validate required fields and business rules; errors shown above the form
- **`ExpenseCategory` enum** — `Expense.Category` changed from free-text `string` to `ExpenseCategory` enum (`Medical`, `Food`, `Facilities`, `Supplies`, `Transport`, `Other`), mirroring `DonationCategory`; UI uses `<select>` with `Enum.GetValues`
- **Unit test suite** — `tests/Refugio.Tests`: 108 tests across `DogActor`, `AdoptionActor`, `FinanceActor`, `VolunteerActor`, `TaskActor`, `PasswordHelper`; uses `Akka.TestKit.Xunit2` + EF in-memory
- **Integration test suite** — `tests/Refugio.Tests.Integration`: 75 tests across `AuthEndpointTests`, `DogsApiTests`, `RbacTests`, `AdoptionApiTests`, `VolunteerApiTests`, `FinanceCsvTests`, `PaginationTests`; uses `WebApplicationFactory` + SQLite in-memory

---

## Recommended next steps

### Code quality / architecture

1. **Compile-time safety for `FormReader` field names.** Strings passed to `GetInt(form, "AgeMonths")` can't be checked at compile time. A typo fails silently at runtime. Options: `nameof`-compatible string constants declared per page; or a source generator that emits typed form accessors from a model class. Low risk at current scale but the silent failure mode grows more dangerous as forms accumulate.

2. **Extract shared validation.** Validation logic is duplicated per Blazor page. A typed `FormModel<T>` with a `Validate()` method per entity, or a shared `ValidationResult`-based actor response, would centralize rules and prevent pages diverging over time. The current per-page approach keeps things self-contained but will drift as business rules evolve.

3. **Remove the double volunteer fetch in `Volunteers.razor`.** The page calls `Api.GetVolunteers()` (all records, for the task-assignment dropdown) and `Api.GetVolunteersPaged(...)` separately. The paged result already contains all active volunteers visible on the page — the full-load call is redundant and doubles DB queries on every page load.

4. **Resolve `DogDetail.razor` / `DogEdit.razor` duplication.** Dog check-in reuses `DogDetail.razor` with an `IsNew` flag, causing the component to serve two distinct roles. Splitting into a dedicated intake form that defaults status to `Available` and hides rarely-needed fields would reduce both component complexity and shelter-staff friction.

### Testing

- **Unit tests (108 total)** cover all five actors. Priority gaps: `DogActor` restore handlers (`GetDeletedDogs`, `RestoreDog`) have no tests; `DogActor.UpdateDogPhoto` is untested; soft-delete filter verification (confirm `HasQueryFilter` excludes deleted rows in normal queries and `.IgnoreQueryFilters()` includes them) would catch a regression if a filter is accidentally removed.
- **Integration tests (75 total)** cover auth, dogs CRUD, RBAC, adoptions, volunteers, finance CSV, and pagination. Remaining gaps: soft-delete/restore endpoints (`POST /api/dogs/{id}/restore` etc.); admin page access control (verify a Volunteer role gets 403 on `/admin/deleted` endpoints).
- **No E2E browser tests needed** for SSR-only pages — integration tests cover the full request pipeline without Playwright overhead.

### Functionality

1. **Email notifications.** Adoption status changes and upcoming medical appointments are the two highest-value trigger points. Add `IEmailSender` (ASP.NET Core built-in interface) backed by SMTP or a transactional provider (Resend, SendGrid). Wire into `AdoptionActor` on status advance and a background `IHostedService` for appointment reminders. The actor pattern already gives a natural injection point — pass `IEmailSender` into the actor constructor alongside `IServiceScopeFactory`.

2. **Reporting dashboard.** The finance summary chart exists. Useful extensions: adoption conversion rate by month (applied → finalized ratio), average shelter stay in days by breed (requires `Dog.AdoptedAt` or reading adoption finalize dates), volunteer activity per month. All are computable from existing data with new actor messages — no schema changes needed.

3. **Dog intake form improvements.** `dogs/new` reuses `DogDetail.razor` with an `IsNew` flag — the form shows all fields including status and medical history. A simpler dedicated intake component that defaults status to `Available`, hides medical/medication sections, and focuses on name/breed/age/photo would match the actual shelter check-in workflow and reduce entry errors.

4. **Extend admin deleted records view.** Currently only dogs, adoptions, and volunteers are recoverable. Consider adding soft-deleted donations and expenses (accidental finance entry deletion is plausible). Medical records and medications are intentionally excluded — recovering them without their parent dog context would be confusing.

5. **Multi-language expansion.** The localization infrastructure is in place. Adding a third language (e.g. `ca-ES` Catalan, `pt-BR` Portuguese) requires only a new RESX file and a one-line addition to the `supportedCultures` array in `Program.cs` — no code changes beyond that.
