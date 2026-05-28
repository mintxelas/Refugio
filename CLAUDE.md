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
```

No test suite yet. The SQLite database (`shelter.db`) is auto-created on first run inside `src/Refugio.Web/` with seed data. Schema migrations use try/catch `ALTER TABLE ... ADD COLUMN` at startup in `Program.cs` — no EF migrations framework.

---

## Architecture

Four projects, strict one-way dependency flow:

```
Domain → Infrastructure → Application → Web
```

| Project | Role |
|---|---|
| `Refugio.Domain` | Pure entity classes, enums, `PasswordHelper`. Zero dependencies. |
| `Refugio.Infrastructure` | EF Core + SQLite (`ShelterDbContext`). `SeedData.Seed()` runs on first boot. |
| `Refugio.Application` | Akka.NET actor system. One actor per domain area. `ShelterActorService` is the singleton bridge. |
| `Refugio.Web` | ASP.NET 9. Hosts both REST API (`/api/*` minimal API) and Blazor SSR pages. |

### Domain entities

`Dog`, `MedicalRecord`, `Medication`, `Adoption`, `ShelterTask`, `Donation`, `Expense`, `Volunteer`, `ShelterEvent`.

### Actors

| Actor | Handles |
|---|---|
| `DogActor` | Dogs, medical records, medications, dashboard stats |
| `AdoptionActor` | Adoption/foster applications and status pipeline |
| `FinanceActor` | Donations and expenses |
| `VolunteerActor` | Volunteers, volunteer status, shelter events |
| `TaskActor` | Shelter tasks |

`ShelterSupervisorActor` spawns all child actors. `ShelterActorService` (singleton) resolves actor refs by path with a 500 ms warmup delay + 5 s `ResolveOne` timeout to avoid a startup race condition.

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

### Other SSR patterns
- **Query params / filters:** `[SupplyParameterFromQuery]` + `<a href="/page?param=x">` links.
- **Collapsible sections:** `<details>/<summary>` — no JS needed.
- **Clickable rows:** absolute `<a class="absolute inset-0">` inside `relative` container; content uses `pointer-events-none`; action buttons use `relative z-10`.
- **POST-then-redirect:** call `Nav.NavigateTo(...)` after processing to issue a 302 and prevent re-submit on refresh.
- **Action endpoints (activate/delete/etc):** `GET /api/resource/{id}/action` that executes the action and redirects back. Always add `.RequireAuthorization()`.
- **Multiple forms on one page:** check `form["_handler"].ToString()` (injected by `@formname`) to distinguish which form was submitted.
- **Tabs:** `[SupplyParameterFromQuery]` + query param links; active tab by string comparison.

---

## Authentication

Cookie-based (`CookieAuthenticationDefaults`). Sessions last 7 days (sliding expiry).

| Endpoint | Method | Notes |
|---|---|---|
| `/auth/login` | POST | `.DisableAntiforgery()` |
| `/auth/logout` | GET | `.DisableAntiforgery()` |
| `/auth/change-password` | POST | `.DisableAntiforgery()` |

All Blazor pages: `@attribute [Authorize]`. Unauthenticated → redirect to `/login` via `<AuthorizeRouteView>` + `<RedirectTo>` in `Routes.razor`.

Password hashing: PBKDF2-SHA256 via `PasswordHelper` in `Refugio.Domain.Helpers` (no external packages).

`Volunteer` entities have `CanLogin` (bool) and `PasswordHash` (string?). Seed login: `elena@havensanctuary.org` / `shelter123`.

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
| Dog catalog | `/dogs` | Search + status filter |
| Dog detail | `/dogs/{id}` | Medical history, medications, adoption/health links |
| Dog edit | `/dogs/{id}/edit` | Edit all fields incl. status |
| Dog check-in | `/dogs/new` | Same component as Dog detail (`IsNew` flag) |
| Medical record edit | `/dogs/{dogId}/medical/{id}` | — |
| Medication edit | `/dogs/{dogId}/medications/{id}` | Includes IsActive toggle |
| Health dashboard | `/health` | Add records/medications; dog selected via `?dogId=` |
| Adoptions | `/adoptions` | Kanban board (Applied→Interview→HomeCheck→Approved→Finalized) |
| Adoption edit | `/adoptions/{id}` | Edit all fields incl. status |
| Calendar | `/calendar` | Weekly grid + upcoming list; `?week=yyyy-MM-dd` |
| Event edit | `/calendar/events/{id}` | — |
| Funds | `/funds` | Tabs: donations / expenses / summary chart |
| Donation edit | `/funds/donations/{id}` | — |
| Expense edit | `/funds/expenses/{id}` | — |
| Volunteers | `/volunteers` | Filter by status |
| Volunteer edit | `/volunteers/{id}` | Includes login credentials |
| Change password | `/change-password` | Authenticated users only |
| Login | `/login` | BlankLayout, no auth required |

---

## Key design decisions and tradeoffs

### Akka.NET as the application layer
**Decision:** Route all business logic through Akka.NET actors rather than using plain service classes.
**Why:** The shelter had a stated desire for an eventually-concurrent model (multiple simultaneous users, possible future background jobs). Actors give natural single-writer-per-entity concurrency and a clear place for future event sourcing or reactive messaging.
**Tradeoff:** Significant boilerplate (message records, actor registration, scope-per-handler pattern). For a CRUD app at this scale, plain scoped services (`IRepository<T>`) would have been simpler. The actor overhead is mostly invisible at this traffic level but adds ~500 ms startup latency due to the warmup delay.
**If revisiting:** The actor system is fine to keep but the 500 ms `Task.Delay(500).Wait()` is a code smell — replace with a proper `ActorSystem.WhenTerminated` / readiness probe or structured startup.

### Pure Blazor SSR (no interactivity)
**Decision:** No `@rendermode InteractiveServer` or `@rendermode InteractiveWebAssembly` anywhere.
**Why:** Avoids SignalR dependency and WebSocket state management; works correctly behind reverse proxies and CDNs; zero JavaScript runtime complexity.
**Tradeoff:** Every user action is a full HTTP round-trip. Filtering, sorting, and multi-step flows require query params and redirects. UI is noticeably less fluid than a SPA or interactive Blazor app. Workarounds (GET-action endpoints, `<details>` for toggles, `onclick="return confirm()"` for JS confirms) are scattered across pages.
**If revisiting:** Adding `@rendermode InteractiveServer` per-component (not globally) to forms and kanban boards would dramatically improve UX with minimal structural change.

### SQLite + try/catch migrations
**Decision:** Schema changes via `ALTER TABLE ... ADD COLUMN` in `Program.cs` startup, wrapped in try/catch to skip if column already exists.
**Why:** Zero-friction for a single-instance deployment. No migration toolchain to manage.
**Tradeoff:** Not idempotent in a meaningful way — columns are never removed or renamed. Will silently fail to add columns in edge cases (existing table with different schema). Not suitable for multi-instance deployments.
**If revisiting:** Switch to EF Core migrations (`dotnet ef migrations add`). The infrastructure is already in place — just run `Add-Migration Initial` and delete the try/catch blocks.

### `IHttpContextAccessor` for form data
**Decision:** Read POST form fields manually via `ctx.Request.ReadFormAsync()` rather than `[SupplyParameterFromForm]`.
**Why:** `[SupplyParameterFromForm]` silently fails (no exception, just null/default) when a form field value cannot be parsed to its bound property type (e.g. empty string to `int`, or empty string to `decimal`). This caused data-loss bugs during development.
**Tradeoff:** Manual `form["FieldName"].ToString()` is verbose and loses compile-time safety on field names.

### Single `SharedResources` for all localization keys
**Decision:** One RESX file pair for the whole app rather than per-page or per-feature resource files.
**Why:** Simpler — one place to add keys, no namespace confusion with `IStringLocalizer<T>` generics.
**Tradeoff:** The file grows large (~250+ keys). Key naming discipline (`Section_KeyName`) is critical to avoid collisions.

### GET endpoints for destructive actions
**Decision:** `GET /api/resource/{id}/delete` (and `/activate`, `/deactivate`, `/advance`, `/reject`) rather than using DELETE/POST from Blazor pages.
**Why:** Pure SSR means `<form method="delete">` doesn't exist in HTML. Options are: (a) POST form with hidden `_method` override, (b) JavaScript fetch, (c) plain GET link with server-side redirect. Option (c) requires no JS and no extra form.
**Tradeoff:** GET requests with side effects violate HTTP semantics and are vulnerable to CSRF via `<img src>` / prefetch attacks. Mitigated here by `.RequireAuthorization()` on all action endpoints (unauthenticated requests redirect to login). A POST form with `<AntiforgeryToken />` would be strictly more correct.

---

## Recommended next steps

### Code quality / architecture
1. **Replace try/catch migrations with EF Core migrations.** The schema will keep drifting — `try/catch ALTER TABLE` won't survive column renames or type changes.
2. **Remove `Task.Delay(500).Wait()` in `ShelterActorService`.** Replace with a proper supervisor readiness pattern (e.g. send a `Ready?` probe message with retry).
3. **Add input validation.** Currently the app relies on HTML `required` / `type="number"` attributes, which are bypassed by direct HTTP requests. Add server-side validation in each form handler.
4. **Move all delete action endpoints to POST forms.** Eliminates the GET-side-effect semantic problem. Can be a small `<form method="post">` with a single button and antiforgery token — reuse the existing confirm pattern.
5. **Add a test project.** The actor/message pattern is well-suited to unit testing actors in isolation with `Akka.TestKit`. Start with `DogActor` and `FinanceActor`.
6. **Extract `DogStatusDisplay` / `AgeDisplay` helpers** from individual pages into a shared Razor component or static helper class — currently duplicated across `Dogs.razor`, `DogDetail.razor`, `DogEdit.razor`, `Home.razor`.

### Functionality
1. **Photo upload for dogs.** The `Dog.PhotoUrl` field and `<img>` placeholder already exist. Add a file upload endpoint (`POST /api/dogs/{id}/photo`) and wire it to the dog edit form.
2. **Role-based access control.** Currently any authenticated user can do everything. Add roles (`Manager`, `Volunteer`) to the `Volunteer` entity and restrict destructive actions (`[Authorize(Roles = "Manager")]`).
3. **Task assignment to volunteers.** `ShelterTask.AssignedTo` is a free-text string. Make it a foreign key to `Volunteer` and add a dropdown in the task creation form.
4. **Pagination on the dog catalog and finance tables.** Currently loads all records; will degrade with hundreds of entries.
5. **Email notifications.** Adoption status changes and upcoming medical appointments are obvious trigger points. Add `IEmailSender` (ASP.NET Core built-in interface) backed by SMTP or Resend.
6. **Reporting / export.** The finance summary chart exists. Add CSV export for donations, expenses, and adoption history.
7. **Multi-language expansion.** The localization infrastructure is in place. Adding a third language (e.g. `ca-ES` Catalan) requires only a new RESX file — no code changes.
8. **Soft delete.** Currently hard-deleting dogs, adoptions, and events. A `DeletedAt` timestamp and a global query filter would give an audit trail and allow recovery.
