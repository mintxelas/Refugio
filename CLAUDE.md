# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Commands

```bash
# Build entire solution
dotnet build

# Run the web app (http://localhost:5110)
cd src/Refugio.Web && dotnet run

# Run unit tests (128 tests, Akka.TestKit + in-memory EF)
dotnet test tests/Refugio.Tests/

# Run integration tests (91 tests, WebApplicationFactory + SQLite in-memory)
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

All entities have a `DeletedAt DateTime?` property. EF global query filters in `ShelterDbContext.OnModelCreating` exclude soft-deleted records from all queries automatically — no callers need to filter manually. Use `.IgnoreQueryFilters()` only in admin/recovery contexts. When `.IgnoreQueryFilters()` is applied to a root query it also bypasses filters on all related entities loaded via `.Include()` in the same query.

`ShelterTask` uses only `AssignedVolunteerId int?` + `AssignedVolunteer Volunteer?` (FK nav prop). The legacy `AssignedTo string?` field was removed in migration `RemoveTaskAssignedTo`.

Both `Donation.Category` (`DonationCategory`) and `Expense.Category` (`ExpenseCategory`) are enums stored as strings via `HasConversion<string>()`. This makes existing data human-readable in the DB and allows adding members without a DDL migration.

### Actors

| Actor | Handles |
|---|---|
| `DogActor` | Dogs, medical records, medications, photo upload, dashboard stats; soft-deleted recovery for dogs, medical records, and medications (with parent-dog liveness check on restore) |
| `AdoptionActor` | Adoption/foster applications, status pipeline; soft-deleted adoption recovery |
| `FinanceActor` | Donations and expenses, paginated retrieval, CSV export; soft-deleted donation and expense recovery |
| `VolunteerActor` | Volunteers, volunteer status, shelter events; soft-deleted volunteer recovery |
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

The REST API (`/api/*`) uses the correct `DELETE` verb for external consumers. Blazor pages use parallel `POST /api/{entity}/{id}/delete` endpoints because HTML forms only support GET and POST — the `DELETE` verb requires JavaScript, which this app deliberately avoids.

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
- **Enum selects:** `value="@enumValue"` must always be the C# member name (needed for `FormReader.GetEnum` parsing). Display text uses the localization key — `@L[$"EnumType_{enumValue}"]`. Never render raw enum `.ToString()` as visible UI text.

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

### Enum display localization

Enums are stored and parsed using their C# member name (English). Every enum value rendered as visible UI text must go through a localization key. The convention is `EnumType_MemberName`:

| Enum | Key pattern | Example |
|---|---|---|
| `DogStatus` | `DogStatus_{value}` | via `DogHelpers.DogStatusDisplay(s, L)` |
| `AdoptionStatus` | `Adoptions_{value}` | `L[$"Adoptions_{adoption.Status}"]` |
| `AdoptionType` | `Adoptions_Adoption` / `Adoptions_Foster` | explicit switch in `Adoptions.razor` |
| `VolunteerStatus` | `VolunteerStatus_{value}` | `L[$"VolunteerStatus_{v.Status}"]` |
| `DonationCategory` | `DonationCategory_{value}` | `L[$"DonationCategory_{d.Category}"]` |
| `ExpenseCategory` | `ExpenseCategory_{value}` | `L[$"ExpenseCategory_{e.Category}"]` |

`<select>` option values stay as the enum name (`value="@cat"`) — only the visible label is localized (`@L[$"ExpenseCategory_{cat}"]`). `FormReader.GetEnum` parses the submitted English value back regardless of UI language.

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
| Dog check-in | `/dogs/new` | Dedicated `DogCheckin.razor`; defaults to `Available` status; server-side validation with `_errors` and sticky field values |
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
| Reports | `/reports` | Adoption conversion rate by month (year selector); avg shelter stay by breed; `?year=N` query param |
| Admin — Deleted Records | `/admin/deleted` | Manager-only; 7 tabs: dogs / adoptions / volunteers / donations / expenses / medical records / medications; restore buttons; medical+medication tabs show warning and disabled button when parent dog is also deleted |
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

### POST forms for all browser-triggered mutations
**Decision:** All browser-driven mutations use `<form method="post">` with antiforgery tokens. The REST API (`/api/*`) uses correct HTTP verbs (`DELETE`, `PUT`) for external consumers.
**Why:** HTML forms only support GET and POST. Using `DELETE` from the browser requires JavaScript, which this app avoids. GET requests with side effects violate HTTP semantics and are CSRF-vulnerable. POST + antiforgery is the correct SSR-only solution.
**Tradeoff:** Parallel endpoints exist — e.g. both `DELETE /api/dogs/{id}` (for external callers) and `POST /api/dogs/{id}/delete` (for Blazor forms). Minor duplication in `Program.cs`.
**Options discarded:** GET delete links with `.RequireAuthorization()` (semantically wrong, earlier approach); JavaScript `fetch()` for DELETE (requires JS, breaks SSR purity).

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

### Admin deleted records view — full coverage with parent-dog constraint
**Decision:** `/admin/deleted` (Manager-only) has 7 tabs: dogs, adoptions, volunteers, donations, expenses, medical records, medications. All use `GetDeleted*` actor messages with `.IgnoreQueryFilters()` and restore via `POST /api/{entity}/{id}/restore`.

Medical records and medications include their parent `Dog` via `.Include(r => r.Dog)` on an `.IgnoreQueryFilters()` query — this loads the dog even if it is also soft-deleted (EF propagates `IgnoreQueryFilters` to includes in the same query). The UI checks `rec.Dog?.DeletedAt != null` and shows a warning icon, red dog name, and a disabled non-form label instead of a restore button. The restore actor handlers enforce the same constraint server-side: `await db.Dogs.AnyAsync(d => d.Id == rec.DogId)` uses the normal (filtered) query — returns `false` if the dog is soft-deleted — and returns `false` to the endpoint without restoring.

Events, tasks, and shelter-event records are not included — these are rarely deleted accidentally and have no recovery UX value.
**Tradeoff:** Two enforcement layers (UI disables button, actor double-checks) add a small amount of duplication but prevent a crafted POST from restoring an orphaned medical record.
**Options discarded:** Single enforcement in the endpoint only (actor unaware of constraint — hard to test); UI-only enforcement (crafted POST bypasses it); separate archive table (more schema complexity for the same outcome).

### `IHttpContextAccessor` + `FormReader` for form data
**Decision:** Read POST form fields via `ctx.Request.ReadFormAsync()` then parse with the static `FormReader` helper rather than `[SupplyParameterFromForm]`.
**Why:** `[SupplyParameterFromForm]` silently fails (no exception, just null/default) when a field value can't be parsed to its bound type (e.g. empty string to `int`). This caused data-loss bugs during development. `FormReader` centralizes parse-failure handling and eliminates repeated `int.TryParse` boilerplate.
**Tradeoff:** Field names are plain strings — no compile-time safety. A typo in `GetInt(form, "Ammount")` fails silently at runtime.

### Role-based access control
**Decision:** `Volunteer.Role` is `"Manager"` or `"Volunteer"`. Destructive actions are restricted via `[Authorize(Policy = "Manager")]` on API endpoints and `<AuthorizeView Roles="Manager">` in Razor pages.
**Why:** Multiple volunteers access the system; not all should be able to delete records or deactivate colleagues.
**Tradeoff:** Role is a plain string on `Volunteer`, not a separate `Role` entity. Adding fine-grained permissions would require a role/permission table.
**Options discarded:** Per-resource ownership checks (too complex for this use case); Claims-based permissions without roles (overkill for two access levels).

### Single `SharedResources` for all localization keys
**Decision:** One RESX file pair for the whole app rather than per-page or per-feature resource files.
**Why:** Simpler — one place to add keys, no namespace confusion with `IStringLocalizer<T>` generics.
**Tradeoff:** The file now exceeds 345 keys. Key naming discipline (`Section_KeyName`) is critical to avoid collisions. Enum display keys follow their own convention (`EnumType_MemberName`) documented in the Localization section.
**Options discarded:** Per-page resource files (correct at large scale, overkill here — adds namespace juggling for marginal benefit).

### Enum display via localization keys, storage via C# member name
**Decision:** Enums are stored and POSTed as their C# member name (English), but every render of an enum value as visible UI text goes through a RESX key. `DogStatus` uses `DogHelpers.DogStatusDisplay(s, L)`. All others use inline `L[$"EnumType_{value}"]`.
**Why:** Enums stored as strings (via `HasConversion<string>()`) are stable identifiers — their value in the DB must not change with the UI language. Decoupling storage name from display string means adding a language never touches the DB layer.
**Tradeoff:** The `L[$"EnumType_{value}"]` pattern uses a string key constructed at runtime. A missing RESX key silently falls back to the key string itself (e.g. `"VolunteerStatus_Active"`) — visible to users but not a crash. Adding a new enum member requires adding RESX keys to both files before the member is used in the UI.
**Critical:** `<select>` option `value` attributes must always be the enum member name, not the localized string — `FormReader.GetEnum` parses the submitted value back to the enum and will fail if the localized string was submitted instead.
**Options discarded:** Storing localized strings in DB (breaks when language changes or DB is queried directly); switch statements per enum per page (doesn't scale); `[Display]` attributes on enum members (requires reflection helper, adds indirection with no benefit over RESX).

### Server-side validation in Blazor pages
**Decision:** Each edit page accumulates errors in `List<string> _errors`, validates after reading the form, and returns early if any errors exist.
**Why:** HTML `required` / `type="number"` attributes are bypassed by direct HTTP requests. Server-side validation is the only reliable guard.
**Tradeoff:** Validation logic is duplicated per page — no shared validator or actor-level `ValidationResult`. A future refactor could extract a shared validation layer, but the current approach keeps each page self-contained and avoids a new abstraction.
**Options discarded:** FluentValidation (adds a package for limited gain at this scale); actor-level `ValidationResult` (cleaner but requires a new response type and error-display protocol per actor message).

### Enum categories stored as strings in SQLite
**Decision:** All enum properties use `HasConversion<string>()` in `ShelterDbContext` — stored as TEXT, not integer ordinal.
**Why:** SQLite has no enum type; storing as integer would make the DB unreadable without the code. String storage also means adding a new enum member requires no DDL migration — EF generates an empty migration file, and the snapshot updates without any `ALTER TABLE`.
**Tradeoff:** Renaming an enum member is a breaking change — existing string values in the DB won't match the new name. Treat enum member names as permanent identifiers.
**Options discarded:** Integer storage (EF default — unreadable DB, no gain for this use case); separate lookup table (overkill; these categories are stable).

### Integration tests use SQLite in-memory, not EF in-memory provider
**Decision:** `ShelterWebFactory` replaces `DbContextOptions<ShelterDbContext>` with a `SqliteConnection("Data Source=:memory:")`, not `UseInMemoryDatabase`.
**Why:** EF Core registers provider-specific singletons into the DI container when `AddDbContext` is called. When `WebApplicationFactory.ConfigureServices` adds a second provider (in-memory), EF's internal service provider receives both and throws `InvalidOperationException`. Using SQLite in-memory keeps a single provider. It also lets `db.Database.Migrate()` run correctly (in-memory SQLite supports migrations; EF in-memory does not). The open `SqliteConnection` is kept alive on the factory instance and disposed with it — SQLite in-memory databases are scoped to the connection.
**Tradeoff:** Tests depend on SQLite behavior; a subtle SQLite vs production-SQLite difference could cause a test to pass but prod to fail (both are SQLite here so the risk is minimal). Tests within a class share a single DB — mutation from one test is visible to subsequent tests in the same class. Mitigated by using unique entity names per test, but not fully isolated.
**Options discarded:** EF in-memory provider (dual-provider conflict); separate test SQLite file (cleanup complexity, parallel-test isolation risk); one factory per test (startup cost of Akka actor system per test — prohibitive).

### Adoptions kanban "show more" instead of traditional pagination
**Decision:** Each status column in the kanban calls `GetAdoptionsPaged` with a per-column limit driven by a query param (`?appliedLimit=N`). "Show more" links increment the limit.
**Why:** True pagination on a kanban is disruptive — volunteers need to see all cards in a column at once, not navigate pages. Per-column capping with show-more matches the UX expectation while preventing unbounded DB queries.
**Tradeoff:** The URL grows one param per column once any column is expanded. State is not preserved across sessions.
**Options discarded:** Full numeric pagination per column (bad UX for kanban); loading all records (unbounded query, O(n) memory); infinite scroll (requires JS).

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
- **Admin deleted records view** — `/admin/deleted`; Manager-only; 7 tabs (dogs, adoptions, volunteers, donations, expenses, medical records, medications); `GetDeleted*` + `Restore*` actor messages using `.IgnoreQueryFilters()`; medical record and medication tabs load the parent dog via `.Include()` on an `IgnoreQueryFilters` query (dog shown even if also deleted); restore is blocked for records whose dog is also soft-deleted — UI disables the button, actor enforces server-side via `db.Dogs.AnyAsync()` with the normal filtered query
- **POST forms for delete** — replaced GET delete links across all pages; parallel `DELETE` verb endpoints kept for REST API consumers
- **`DogHelpers` static class** — `DogStatusDisplay`, `AgeDisplay`, `StatusChipClass`, `StatusIcon` in `src/Refugio.Web/Helpers/DogHelpers.cs`
- **`FormReader` static helper** — typed form parsing (`GetString`, `GetInt`, `GetDecimal`, `GetDateTime`, `GetBool`, `GetEnum<T>`) in `src/Refugio.Web/Helpers/FormReader.cs`; replaces scattered `int.TryParse` / `.ToString()` calls
- **`Validator` static helper** — shared validation predicates (`RequireNotEmpty`, `RequirePositive`, `RequireNonNegative`, `RequireValidEmail`, `RequireDate`, `RequireAfter`) in `src/Refugio.Web/Helpers/Validator.cs`; globally imported via `_Imports.razor`; used across all 8 edit pages
- **Server-side input validation** — all edit pages validate required fields and business rules via `Validator` helper; errors shown above the form
- **`GetVolunteerCounts` actor message** — lightweight alternative to loading all volunteers for stats; returns `VolunteerCounts(Total, Active, Pending)` via 3 COUNT queries; `Volunteers.razor` stats cards use this instead of full `GetAllVolunteers`
- **Email notifications** — `IShelterEmailSender` in `Refugio.Application.Services`; `NoOpEmailSender` (log-only default) registered as singleton; `AdoptionActor` sends status-change email to `ApplicantEmail`; `AppointmentReminderService : BackgroundService` emails managers daily for upcoming vet appointments (NextVisitDate within 3 days)
- **Reporting dashboard** — `/reports` page with adoption conversion stats by month and avg shelter stay by breed; actor messages `GetAdoptionConversionStats(year)` and `GetShelterStayStats()`; REST endpoints `GET /api/reports/adoption-conversion` and `GET /api/reports/shelter-stay` (auth required); Reports link in sidebar nav
- **Dog check-in as dedicated page** — `DogCheckin.razor` at `/dogs/new`; clean form with server-side validation, sticky field values, `_errors` display; `DogDetail.razor` simplified (no `IsNew` flag, no dual-role complexity)
- **Portuguese (pt-BR) + Catalan (ca-ES) localization** — full RESX translations for both; added to `supportedCultures`; `MainLayout.razor` language switcher shows EN/ES/PT/CA
- **`ExpenseCategory` enum** — `Expense.Category` changed from free-text `string` to `ExpenseCategory` enum (`Medical`, `Food`, `Facilities`, `Supplies`, `Transport`, `Other`), mirroring `DonationCategory`; UI uses `<select>` with `Enum.GetValues`
- **Full enum display localization** — every enum value rendered as visible UI text goes through a RESX key; covers `DogStatus`, `AdoptionStatus`, `AdoptionType`, `VolunteerStatus`, `DonationCategory`, `ExpenseCategory` across all pages and select dropdowns; `value` attributes stay as C# member names for correct `FormReader` parsing
- **Unit test suite** — `tests/Refugio.Tests`: 142 tests across `DogActor`, `AdoptionActor`, `FinanceActor`, `VolunteerActor`, `TaskActor`, `PasswordHelper`; uses `Akka.TestKit.Xunit2` + EF in-memory; covers CRUD, soft-delete filter verification, all `GetDeleted*`/`Restore*` handlers in all actors, parent-dog liveness constraint, email notification triggers, adoption conversion stats, shelter stay stats; `ActorTestBase` has `ConfigureServices` virtual hook for registering stub services
- **Integration test suite** — `tests/Refugio.Tests.Integration`: 103 tests across `AuthEndpointTests`, `DogsApiTests`, `RbacTests`, `AdoptionApiTests`, `VolunteerApiTests`, `FinanceCsvTests`, `PaginationTests`, `RestoreApiTests`, `ReportsApiTests`; `ReportsApiTests` covers reports API RBAC + page render for `/reports` and `/dogs/new`

---

## Testing conventions

### Enum values in integration test JSON bodies

`ConfigureHttpJsonOptions` registers `JsonStringEnumConverter`, so the REST API serializes and deserializes enums as **strings**. Always use the C# member name when sending enum values in JSON bodies or asserting enum values in responses:

```csharp
// Correct — string names
await client.PostAsJsonAsync("/api/donations", new { DonorName = "X", Amount = 50m, Category = "OneTime" });
await client.PostAsJsonAsync("/api/adoptions", new { ..., Type = "Adoption" });
Assert.Equal("Applied", doc.RootElement.GetProperty("status").GetString());
```

For `GetFromJsonAsync<T>` with entity types that have enum properties (`Dog`, `Adoption`, `Volunteer`), pass a `JsonSerializerOptions` with `JsonStringEnumConverter` — otherwise the default STJ client-side options will fail to deserialize the string enum values:

```csharp
private static readonly JsonSerializerOptions _jsonOpts = new()
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};
var dogs = await client.GetFromJsonAsync<List<Dog>>("/api/dogs", _jsonOpts);
```

Query parameters for enum-typed route values (e.g. `?status=Applied`) accept both the name and the integer ordinal — ASP.NET Core's model binding uses `Enum.TryParse`, not STJ. Use string names for consistency.

### Integration test isolation

Each test class uses `IClassFixture<ShelterWebFactory>` — one factory and one SQLite in-memory DB per class, shared across all tests in that class. Tests within a class run sequentially but share state. Mitigate interference by using unique entity names per test (e.g. `"E2ERestoreDog"`, `"ParentDogMedRecord"`). For tests that require completely clean state, create a fresh `ShelterWebFactory` directly (forgoing the shared fixture) — but note the Akka startup cost (~500ms).

---

## Recommended next steps

### Most important (ranked)

Prioritized by value-to-effort across all categories below. Each is verified outstanding as of the current `main` (restore feature is complete and fully documented above):

All original items complete. Remaining work is low-priority refactors or future features:

1. ~~Close the restore test gaps~~ **Done.**
2. ~~Remove the double volunteer fetch~~ **Done.**
3. ~~Email notifications~~ **Done.** `IShelterEmailSender` interface in `Refugio.Application.Services`. `NoOpEmailSender` is the default (logs, no SMTP). `AdoptionActor.Handle(UpdateAdoptionStatus)` sends email to `ApplicantEmail` on every status change. `AppointmentReminderService : BackgroundService` runs daily, finds `MedicalRecord.NextVisitDate` within 3 days, emails all Manager-role volunteers. Swap `NoOpEmailSender` for a real implementation (Resend, SendGrid, SMTP) by replacing the `IShelterEmailSender` singleton registration in `Program.cs`.
4. ~~Extract shared validation~~ **Done.**

**See "Functionality" section below for additional completed items.**

Details and lower-priority items below.

### Code quality / architecture

1. ~~Compile-time safety for `FormReader` field names~~ **Done.** Every page with form POST handling declares `private const string F{Field} = "{Field}";` constants at the top of its `@code` block. All `GetString(form, "...")`, `GetInt(form, "...")`, `form["..."]`, etc. calls use these constants. 14 pages updated: `DogCheckin`, `DogEdit`, `AdoptionEdit`, `MedicalRecordEdit`, `MedicationEdit`, `VolunteerEdit`, `Volunteers`, `DonationEdit`, `ExpenseEdit`, `EventEdit`, `Home`, `Funds`, `Health`, `Adoptions`. A typo in a constant is now a compile error, not a silent runtime bug.

2. ~~Extract shared validation~~ **Done.** `Validator` helper + all 8 edit pages updated.

3. ~~Remove the double volunteer fetch~~ **Done.** Replaced with `GetVolunteerCounts`.

4. ~~Resolve `DogDetail.razor` / `DogEdit.razor` duplication~~ **Done.** `DogCheckin.razor` (`/dogs/new`) is now a standalone component with proper validation and `_errors` display. `DogDetail.razor` removed the `IsNew` flag entirely — it only serves the view-dog route.

### Testing

**142 unit tests, 103 integration tests.** All originally-documented gaps closed.

New tests added:
- `AdoptionActorEmailTests`: `UpdateAdoptionStatus_SendsEmail_WhenApplicantEmailSet`, `..._DoesNotSendEmail_WhenNoApplicantEmail`
- `AdoptionActorTests`: `GetAdoptionConversionStats_*` (2 tests), `GetShelterStayStats_*` (2 tests)
- `ReportsApiTests`: reports API RBAC + page render (5 tests), DogCheckin page render (2 tests)

`ActorTestBase` now has a `protected virtual void ConfigureServices(IServiceCollection)` hook that test subclasses can override to register stub/spy services (e.g. `CapturingEmailSender`).

**No E2E browser tests needed** for SSR-only pages — integration tests cover the full request pipeline without Playwright overhead.

### Functionality

1. ~~Email notifications~~ **Done.** See "Most important" above.

2. ~~Reporting dashboard~~ **Done.** New `/reports` page (`Reports.razor`) with two sections:
   - **Adoption Conversion Rate** — `GetAdoptionConversionStats(year)` actor message returns `MonthlyConversionData(Month, Applied, Finalized)` × 12; table shows applied vs finalized per month + running conversion %.
   - **Average Shelter Stay by Breed** — `GetShelterStayStats()` actor message joins finalized adoptions to their dog's `ArrivalDate`, computes average days in shelter, groups by breed.
   - API endpoints: `GET /api/reports/adoption-conversion?year=N` and `GET /api/reports/shelter-stay`, both `RequireAuthorization()`.
   - Reports link added to sidebar nav (`bar_chart` icon).

3. ~~Dog intake form improvements~~ **Done.** `DogCheckin.razor` at `/dogs/new`. Clean dedicated form: name, breed, age, gender, weight, traits, notes. Status defaults to `Available` (no status selector). Uses `Validator` helper and shows `_errors` above form on validation failure. Sticky field values on validation error (repopulates form). `DogDetail.razor` simplified — `IsNew`, `_error`, and the check-in branch removed.

4. ~~Multi-language expansion~~ **Done.** Portuguese (pt-BR) and Catalan (ca-ES) added as third and fourth supported cultures. `SharedResources.pt-BR.resx` and `SharedResources.ca-ES.resx` have full translations for all 345+ keys. `MainLayout.razor` language switcher shows 4 options (EN/ES/PT/CA); `langLabel` derives the badge from the active culture. `Program.cs` `supportedCultures` includes `pt-BR` and `ca-ES`. Add more languages by creating a new RESX file and adding one entry to `supportedCultures`.
