# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Skills — use these for recurring procedures

Step-by-step recipes for common tasks live as skills in `.claude/skills/`. They load only when
triggered, so this file stays reference-only. **When doing one of these tasks, invoke the skill** —
it carries the exact files, snippets, and verify steps.

| Skill | Use when |
|---|---|
| `add-service-operation` | Adding an operation to an existing aggregate (domain behavior + service method + endpoint + ApiClient) |
| `add-aggregate` | Introducing a new domain area (entity, repository, service, queries, DI) |
| `blazor-ssr-form-page` | Building/editing a `.razor` page with a form, validation, delete, filters, tabs, or pagination |
| `add-localization` | Adding any visible UI string, validation message, or new enum display value (all 4 RESX files) |
| `ef-migration` | Any schema/entity/enum-mapping change that needs a migration |
| `add-soft-delete-entity` | Wiring soft delete + restore + `/admin/deleted` tab for an entity |

The sections below remain the authoritative **reference** (architecture, design decisions, conventions);
the skills are the **procedures** distilled from them.

## Commands

```bash
# Build entire solution
dotnet build

# Run the web app (http://localhost:5110)
cd src/Refugio.Web && dotnet run

# Run unit tests (service + domain + read-model tests, in-memory EF)
dotnet test tests/Refugio.Tests.Unit/

# Run integration tests (WebApplicationFactory + SQLite shared-cache in-memory)
dotnet test tests/Refugio.Tests.Integration/

# Run all tests
dotnet test

# Add EF Core migration
dotnet ef migrations add <Name> --project src/Refugio.Infrastructure --startup-project src/Refugio.Web

# Verify the domain model still matches the migration snapshot (run after touching entities)
dotnet ef migrations has-pending-model-changes --project src/Refugio.Infrastructure --startup-project src/Refugio.Web
```

The SQLite database (`shelter.db`) is auto-created on first run inside `src/Refugio.Web/`. Migrations run automatically at startup via `db.Database.Migrate()` (or `EnsureCreated()` when the EF provider is in-memory). → Schema change: **skill `ef-migration`**.

---

## Architecture — DDD layering + Akka.NET actor layer

Five projects with **dependency inversion** (Application does NOT depend on Infrastructure):

```
Refugio.Domain        ← no dependencies. Aggregates, domain events, repository interfaces, IUnitOfWork.
Refugio.Application   ← depends on Domain. Use-case services, DTO contracts, read-model query interfaces,
                        IShelterEmailSender, domain-event handlers.
Refugio.Infrastructure← depends on Domain + Application. EF Core + SQLite, repository/query implementations,
                        UnitOfWork (saves + dispatches domain events), SoftDeleteInterceptor, SeedData, email adapter.
Refugio.Actors        ← depends on Application. Akka.NET (Akka.Hosting) layer: one actor per aggregate area,
                        actor messages, scope-per-message delegation to the application services.
Refugio.Web           ← composition root. REST API (Endpoints/*Endpoints.cs) + Blazor SSR pages.
                        Endpoints Ask the area actors; pages consume the API over real HTTP via ShelterApiClient.
```

A request flows: **Blazor page → `ShelterApiClient` (HTTP, cookie-forwarded) → minimal-API endpoint →
area actor (`Ask`, fresh DI scope per message) → application service → domain behavior + repository →
`IUnitOfWork.SaveChangesAsync()` (dispatches domain events) → DTO reply → HTTP response**.

| Project | Key pieces |
|---|---|
| `Refugio.Domain` | `Common/Entity` (Id, DeletedAt, domain events, `Restore()`), `IAggregateRoot`, `Page<T>`, `IUnitOfWork`, `Repositories/I*Repository`, `Events/AdoptionStatusChanged`, rich entities in `Entities/`, `PasswordHelper`, `Roles` |
| `Refugio.Application` | `Contracts/` (DTOs + request records — the wire contract), `Services/` (interface + impl per aggregate, `ShelterServiceBase` for delete/restore/purge shapes), `Queries/IReadQueries` (stats read models), `Abstractions/` (`IShelterEmailSender`, `IDomainEventHandler<T>`, `IDomainEventDispatcher`), `Events/AdoptionStatusChangedHandler`, `Mapping/DtoMapping`, `AddApplicationServices()` |
| `Refugio.Infrastructure` | `ShelterDbContext`, `SoftDeleteInterceptor`, `UnitOfWork`, `DomainEventDispatcher`, `Repositories/EfRepository<T>` + per-aggregate repos, `Queries/ReadQueries`, `Email/NoOpEmailSender`, `SeedData`, `DatabaseInitializer`, EF migrations, `AddInfrastructureServices()` |
| `Refugio.Actors` | `ShelterActorBase<TService>` (Command/Query registration, scope-per-message, `Status.Failure` on faults), area actors (`Dog,Adoption,Volunteer,Event,Task,Finance,Settings`Actor), `ReminderActor` (daily vet digest on an actor timer), `Messages/` (query/id records per area), `NullReply`, `ActorAsk` (`AskFor`/`AskRequired`), `ActorSystemRegistration.AddShelterActors()` |
| `Refugio.Web` | `Program.cs` (wiring only), `Endpoints/{Dog,Adoption,Task,Finance,Volunteer,Settings,Auth}Endpoints.cs` (Ask area actors via `IActorRegistry`), `Services/ShelterApiClient` (typed HTTP client), `Services/ForwardCookieHandler`, `SettingsCacheService`, Blazor pages, `Helpers/` (`DogHelpers`, `FormReader`, `Validator`, `PhotoFiles`, `ImageResizer`) |

Two test projects:

| Project | Role |
|---|---|
| `tests/Refugio.Tests.Unit` | Service tests (real services + repos + UoW over in-memory EF via `ServiceTestBase`; each call in a fresh scope), domain behavior tests, read-model query tests, `PasswordHelper` tests. |
| `tests/Refugio.Tests.Integration` | `WebApplicationFactory` + SQLite **shared-cache named in-memory DB** (`Mode=Memory;Cache=Shared` + keeper connection). The named `ShelterApi` HttpClient is rewired to the TestServer handler so SSR pages' API calls work in-memory. |

### Domain model

Aggregate roots: `Dog` (children: `MedicalRecord`, `Medication`, `DogPhoto`), `Adoption`, `Volunteer`,
`Donation`, `Expense` (child: `ExpensePhoto`), `Goal`, `ShelterTask`, `ShelterEvent`, `ShelterSettings`.

- Entities have **private setters + behavior methods + static factories** (`Dog.CheckIn`, `Adoption.Submit`,
  `Volunteer.Register`, `Donation.Record`, `ShelterEvent.Schedule`, …). EF rehydrates via private ctors.
- **Entity CLR namespace must stay `Refugio.Domain.Entities`** — the EF migration snapshot keys entities by
  full CLR name; moving the namespace would make the next migration drop/recreate every table.
- Child entities of the Dog aggregate are reached only through `IDogRepository` (no own repository).
- Domain events: behaviors call `Raise(...)`; `UnitOfWork` dequeues and dispatches **after** save.
  Currently: `Adoption.ChangeStatus(...)` → `AdoptionStatusChanged` → handler emails the applicant.
  `Adoption.UpdateDetails(...)` deliberately changes status **silently** (no email) — preserve that.
- All entities extend `Entity` (`ISoftDeletable`); EF global query filters exclude soft-deleted rows.
  `SoftDeleteInterceptor` (in `OnConfiguring`, reaches all test contexts) converts `Remove()` into
  `DeletedAt = UtcNow`; `RemovePermanently()` arms `SkipSoftDeleteInterceptor` for a real delete.
- Enums (`DogStatus`, `AdoptionStatus`, `AdoptionType`, `VolunteerStatus`, `DonationCategory`,
  `ExpenseCategory`) are stored as strings (`HasConversion<string>()`); member names are permanent identifiers.

### Application services and contracts

- One service per aggregate area: `IDogService`, `IAdoptionService`, `IVolunteerService`, `IEventService`,
  `ITaskService`, `IFinanceService` (donations + expenses + goals), `ISettingsService` — interface + impl
  in one file under `Services/`. All extend `ShelterServiceBase` for the soft-delete/restore/purge shapes.
- **DTOs in `Contracts/` are the wire contract** — property names match what the old API serialized, so
  external consumers and tests keep working. `VolunteerDto` deliberately has **no PasswordHash**.
- Request records carry `Id` so endpoints can `request with { Id = id }`.
- Cross-aggregate aggregations (dashboard, finance summary, adoption conversion, shelter stay, volunteer
  counts, upcoming vet visits) are **CQRS-lite read models**: interfaces in `Application/Queries`,
  EF implementations in `Infrastructure/Queries`, injected straight into endpoints.
- `Page<T>(Items, TotalCount, PageNumber, PageSize)` lives in `Domain.Common`; repos build it with
  `IQueryable.ToPageAsync(page, size)` (`Infrastructure/Repositories/EfRepository.cs`).

### Blazor ↔ API integration — critical

`ShelterApiClient` (scoped) makes **real HTTP calls** to this same host's `/api/*` endpoints:

- Named client `ShelterApiClient.ClientName` with `UseCookies = false` and `ForwardCookieHandler`, which
  copies the incoming request's Cookie header → the user's auth session and role flow through the API.
- Base address comes from the current request (`scheme://host`); integration tests rewire the named
  client's primary handler to `TestServer.CreateHandler()`.
- Pages consume **DTOs only** — never domain entities, never DbContext, never services directly.
- Helper shapes in the client: `GetRequired<T>` (throws), `GetOrNull<T>`/`PostOrNull<T>`/`PutOrNull<T>`
  (404 → null), `Delete` (404 → false).
- SSR pages that fan out (the adoptions kanban does `Task.WhenAll` over 6 calls) hit the API
  **concurrently** — anything on that path must tolerate parallel scoped DbContexts (the prod file DB and
  the shared-cache test DB both do; a single-connection `:memory:` DB does not).

---

## Akka.NET actor layer — how it works

Reintroduced June 2026 on `Akka.Hosting` 1.5.x as a thin routing/concurrency shell **in front of** the
application services (the services and domain were not changed). System name `refugio`; actors registered
in `ActorSystemRegistration.AddShelterActors()` and resolved in endpoints via `IActorRegistry`.

- **One actor per aggregate area**: `DogActor`, `AdoptionActor`, `VolunteerActor`, `EventActor`,
  `TaskActor`, `FinanceActor`, `SettingsActor` — all extend `ShelterActorBase<TService>`.
- **Messages**: mutations reuse the request records from `Application/Contracts` (already immutable);
  query/id-style operations have records in `Refugio.Actors/Messages/{Area}Messages.cs`.
- **Command vs Query registration** (`ShelterActorBase`):
  `Command<TMsg>` runs through `ReceiveAsync` — awaited in the mailbox, so **writes to an area are
  serialized**. `Query<TMsg>` dispatches the task and `PipeTo`s the reply — **reads stay concurrent**
  (the adoptions kanban's parallel fan-out is unaffected).
- **Scope-per-message**: each message creates a DI scope and resolves the area's service — one scope =
  one unit of work, exactly like an HTTP request did before.
- **Replies**: value or `NullReply.Instance` (actors cannot `Tell(null)`); faults become
  `Status.Failure`, which faults the `Ask`. Endpoints use `ActorAsk.AskFor<T>` (null-unwrapping) and
  `AskRequired<T>` (non-null guarantee); 30 s Ask timeout + request `CancellationToken`.
- **`SettingsActor` registers `GetSettings` as a Command on purpose** — the service creates the default
  row when missing; serializing through the mailbox removes a duplicate-default race.
- **`ReminderActor`** replaces the old `AppointmentReminderService` hosted service: periodic actor timer
  (immediate first tick, then every 24 h) → `VetAppointmentNotifier` in a fresh scope; failures are
  logged and retried next tick instead of stopping the host.
- **CQRS read models (`I*Queries`) stay injected directly into endpoints** — stateless reads gain
  nothing from a mailbox.
- No remoting/persistence: messages stay in-process; closures are avoided anyway so remoting stays open.

---

## Blazor SSR — critical constraints

Pure static SSR — no interactive render mode. `@onclick`, `@bind`, and all interactive Razor directives are **silently ignored**; every action is a full HTTP round-trip. Reference constraints:

- **Forms:** `<form method="post" @formname="…" @onsubmit="Handler">` + `<AntiforgeryToken/>`; read fields in the handler via `IHttpContextAccessor` + `FormReader` (`Helpers/FormReader.cs`, typed `GetString/GetInt/GetDecimal/GetDateTime/GetBool/GetEnum<T>`). **Never `[SupplyParameterFromForm]`** — silently drops unbindable fields (empty string → `int`).
- **Validation:** server-side only (HTML `required` bypassed by raw HTTP); accumulate `List<string> _errors` via the `Validator` helper, display above form, bail before calling the ApiClient. Keep sticky field values.
- **Delete:** POST form + antiforgery to `POST /api/{entity}/{id}/delete` (not a GET link); REST keeps the real `DELETE` verb for external consumers. All action endpoints `.RequireAuthorization()`; destructive → `"Manager"`. Hide/disable for non-managers via `<AuthorizeView Roles="Manager">`.
- **Enum selects:** `value` = C# member name (for `FormReader.GetEnum`); label = `@L[$"EnumType_{value}"]`. Never render raw `.ToString()`.
- **Query params** drive filters / tabs / pagination (`[SupplyParameterFromQuery]` + `<a href="?param=x">`). **POST-then-redirect** (`Nav.NavigateTo`) stops re-submit. **Multiple forms:** disambiguate via `form["_handler"]`. **Collapsible:** `<details>/<summary>`. **JS confirm:** `onclick="return confirm(...)"` — no other JS. **Clickable rows:** absolute `<a class="absolute inset-0">` overlay. **Kanban "show more":** per-column paged calls, `?{col}Limit=N`.

→ Building/editing such a page: **skill `blazor-ssr-form-page`**.

---

## Authentication and authorization

Cookie-based (`CookieAuthenticationDefaults`). Sessions last 7 days (sliding expiry).

| Endpoint | Method | Notes |
|---|---|---|
| `/auth/login` | POST | `.DisableAntiforgery()`; calls `IVolunteerService.LoginAsync` |
| `/auth/logout` | GET | `.DisableAntiforgery()` |
| `/auth/change-password` | POST | `.DisableAntiforgery()` |

All Blazor pages: `@attribute [Authorize]`. Unauthenticated → redirect to `/login` via `<AuthorizeRouteView>` + `<RedirectTo>` in `Routes.razor`.

Password hashing: PBKDF2-SHA256 via `PasswordHelper`; credential rules live **on the `Volunteer` aggregate**
(`Register`/`Update`/`EnableLogin`/`VerifyPassword`/`ChangePassword` — no login → no hash, no language).

Roles: `Volunteer.Role` is `Roles.Manager` or `Roles.Volunteer` (constants — never inline the strings; normalized at startup). Policy `"Manager"` restricts destructive actions; the cookie forwarded by `ShelterApiClient` makes RBAC apply to in-app API calls too. Seed login: `elena@havensanctuary.org` / `shelter123` (Manager).

---

## Localization

Four supported cultures: `en-US` (default), `es-ES`, `pt-BR`, `ca-ES`. Culture persisted in a cookie via `CookieRequestCultureProvider`.

- Switch language: `GET /set-language?culture=…&returnUrl=…` — honors any culture in `supportedCultures`.
- Resource files: `src/Refugio.Web/Resources/SharedResources*.resx` (4 files); marker class `SharedResources.cs`; global `@inject IStringLocalizer<SharedResources> L` in `_Imports.razor`.
- `@L["Key"]` in markup, `L["Key"].Value` in C#, `string.Format(L["Key"].Value, arg)` for parameterized.
- Enum display: `EnumType_MemberName` keys (e.g. `L[$"DonationCategory_{d.Category}"]`; `DogStatus` via `DogHelpers.DogStatusDisplay`). `<select>` option values stay C# member names — only labels are localized.

Never hardcode UI text. → **skill `add-localization`**.

---

## Styling

Tailwind CSS via CDN (`App.razor`). Design tokens defined inline in the `<script>` block of `App.razor` following Material Design 3 naming: `primary`, `secondary`, `tertiary`, `surface-*`, `on-*`, `*-container`, `*-fixed`. **Never use arbitrary hex values** — always the named tokens.

---

## Pages and routes

| Page | Route | Notes |
|---|---|---|
| Home / Dashboard | `/` | KPI cards, upcoming tasks, recent dogs |
| Dog catalog | `/dogs` | Search + status filter + pagination |
| Dog detail | `/dogs/{id}` | Medical history, medications, photo gallery |
| Dog edit | `/dogs/{id}/edit` | All fields incl. status; photo gallery management |
| Dog check-in | `/dogs/new` | `DogCheckin.razor`; server-side validation, sticky values |
| Medical record edit | `/dogs/{dogId}/medical/{id}` | |
| Medication edit | `/dogs/{dogId}/medications/{id}` | Includes IsActive toggle |
| Health dashboard | `/health` | Add records/medications; `?dogId=` |
| Adoptions | `/adoptions` | Kanban + per-column "show more" + CSV export |
| Adoption edit | `/adoptions/{id}` | |
| Calendar | `/calendar` | Weekly grid; `?week=yyyy-MM-dd` |
| Event edit | `/calendar/events/{id}` | |
| Funds | `/funds` | Tabs: donations / expenses / goals / summary + pagination + CSV export |
| Donation edit | `/funds/donations/{id}` | |
| Expense edit | `/funds/expenses/{id}` | Receipt photo gallery |
| Goal edit | `/funds/goals/{id}` | |
| Volunteers | `/volunteers` | Status filter + pagination + counts cards |
| Volunteer edit | `/volunteers/{id}` | Credentials, role, photo, preferred language |
| Reports | `/reports` | Adoption conversion by month; avg stay by breed; `?year=N` |
| Admin — Deleted Records | `/admin/deleted` | Manager-only; 8 tabs incl. goals; restore/purge; parent-dog liveness warnings |
| Settings | `/settings` | Shelter name/phrase/logo (Manager) |
| Change password | `/change-password` | |
| Login | `/login` | BlankLayout, anonymous |

---

## Key design decisions and tradeoffs

### DDD layering with application services (replaced Akka.NET actors)
**Decision:** Business logic lives in rich domain aggregates orchestrated by application services; the former actor system was removed.
**Why:** The actor model added message records, marker-interface routing, and scope-per-handler boilerplate without delivering concurrency value at this traffic level. DDD gives the same single-place-per-rule property with plain C# — behaviors on aggregates, one service per area, repositories behind interfaces — and makes the domain unit-testable without an actor test kit.
**Tradeoff:** Lost the mailbox serialization actors provided (irrelevant for a CRUD app; EF optimistic behavior + scoped contexts cover it) and the future event-sourcing story actors hinted at. Domain events restore the "react to changes" seam.
**Options discarded:** Keeping actors behind the services (two layers of indirection); MediatR (request routing without the domain model benefits).

### Akka.NET reintroduced as a shell in front of the services (June 2026)
**Decision:** The backend runs as an Akka.NET application again, but the actors are a thin layer **in front of** the unchanged application services: endpoints Ask area actors; actors run the service call in a fresh DI scope and reply.
**Why:** Restores the actor model's mailbox guarantees (writes per area serialized; the `SettingsActor` mailbox even fixes a latent duplicate-default-row race) without giving up the DDD wins — domain stays unit-testable, services stay the single place per rule, the HTTP contract is untouched (the pre-existing 147 integration tests passed unmodified).
**Tradeoff:** One extra hop per API call (~Ask overhead, μs-ms); message records duplicate query parameter lists; reads must be registered as `Query<>` or they would serialize behind writes.
**Options discarded:** Moving use-case logic into actors (loses service-level unit tests, recreates the old boilerplate problem); closure-envelope messages (kills any future remoting and hides intent); entity-level actors/sharding (overkill at this traffic).

### UI consumes its own REST API over real HTTP
**Decision:** `ShelterApiClient` calls `/api/*` with `HttpClient`, forwarding the caller's cookies. No in-process shortcut.
**Why:** Makes the API the single contract (UI = first consumer, external integrations = same surface), exercises auth/RBAC/serialization on every page render, and decouples the UI from domain types (DTOs only).
**Tradeoff:** A page render costs extra in-process HTTP hops (TestServer-style overhead, ~ms each; the kanban fans out 6 parallel calls). The cookie-forwarding handler and base-address-from-request are subtle pieces; tests must rewire the named client to the TestServer handler.
**Options discarded:** In-process service injection into pages (no API contract guarantee — the old actor approach's weakness); a separate API host (deployment complexity for a single-box app).

### DTO contracts mirror the old entity JSON
**Decision:** DTO property names/shapes reproduce what the API serialized when it returned entities.
**Why:** Zero breaking change for external consumers and the 147 integration tests; `VolunteerDto` additionally stops leaking `PasswordHash` (the old API exposed it).
**Tradeoff:** Some DTOs carry nullable collections that are empty rather than null depending on includes — mirrors the old behavior.

### Domain events for side effects
**Decision:** `Adoption.ChangeStatus` raises `AdoptionStatusChanged`; `UnitOfWork` dispatches after save; an Application handler sends the email.
**Why:** The side effect is declared where the state change happens, fires only after a successful commit, and is testable end-to-end with a capturing sender.
**Tradeoff:** Hand-rolled dispatcher (reflection over `IDomainEventHandler<>`); no outbox — an email can still be lost if the process dies between save and dispatch (same as before).
**Critical:** `UpdateAdoption` (full edit) changes status **without** an event/email — that asymmetry is intentional, preserved from the original behavior.

### Repositories + UnitOfWork, repositories never save
**Decision:** Repos mutate the change tracker; only `IUnitOfWork.SaveChangesAsync()` commits (and dispatches events). `RemovePermanently` arms the interceptor-skip flag consumed at the next save.
**Why:** One commit point per use case; event dispatch can't be bypassed.
**Tradeoff:** A service forgetting `SaveChangesAsync` silently does nothing — the service tests catch this.

### Entity namespace pinned to `Refugio.Domain.Entities`
**Decision:** Files may organize by aggregate, but the CLR namespace of mapped entities does not change.
**Why:** The EF model snapshot keys on CLR full names; renaming would generate drop/create migrations for every table. Verified with `dotnet ef migrations has-pending-model-changes` (returns "no changes" after the DDD rewrite — schema untouched).

### Soft delete via `Entity`/`ISoftDeletable` + EF global filters + interceptor
Unchanged from before: interceptor converts `Remove()` to a timestamp; global filters hide deleted rows; `.IgnoreQueryFilters()` only in admin/recovery contexts (it propagates to `.Include()`s in the same query). Restore of dog-children is double-guarded (UI disables, service re-checks parent liveness with the filtered query).

### Integration tests: SQLite shared-cache named in-memory DB
**Decision:** `Data Source=RefugioTests-{guid};Mode=Memory;Cache=Shared` + a keeper connection per factory, instead of a single shared `:memory:` connection.
**Why:** SSR pages now fan out parallel API calls; multiple scoped DbContexts over one physical SQLite connection throw `SQLite Error 5: database is locked`. Shared-cache mode gives every context its own connection to the same in-memory database. The unique name isolates class fixtures.
**Tradeoff:** Tests within a class still share one DB — use unique entity names per test.

### Pre-existing fix worth knowing
`Home.razor` donation-goal percentage divides by `DonationGoal`; with an empty Goals table this crashed (`DivideByZeroException`) — now guarded to 0%.

Other decisions retained from the original design (POST forms for browser mutations, EF migrations at startup, enum-as-string storage, single `SharedResources`, server-side validation per page, kanban show-more) are unchanged — see the relevant sections above.

---

## Testing conventions

### Unit tests (`tests/Refugio.Tests.Unit`)
- `ServiceTestBase` builds the real DI graph (services + repos + UoW + dispatcher + queries) over a
  unique in-memory EF DB. `WithServiceAsync<TService,T>` runs each call in a **fresh scope** — mirrors
  one HTTP request and keeps the change tracker honest (a shared scope would let `FindAsync` return
  soft-deleted tracked entities).
- Seed via domain factories (`Dog.CheckIn(...)`), set `DeletedAt` directly when seeding deleted rows.
- Override `ConfigureServices` to swap adapters (e.g. `CapturingEmailSender` for the email tests).
- Domain behavior tests (`Domain/EntityBehaviorTests.cs`) cover event raising, credential rules,
  pipeline transitions — no DB needed.
- Actor tests (`Actors/`, `Akka.TestKit.Xunit2`): `ShelterActorBaseTests` proves the layer contract
  (scope-per-message, NullReply unwrap, `Status.Failure` → faulted Ask, command serialization, query
  concurrency) against a probe service; `ReminderActorTests` proves the digest timer. Stub the area
  service in a tiny `ServiceCollection` and hand the actor its `IServiceScopeFactory`.

### Enum values in integration test JSON bodies
`ConfigureHttpJsonOptions` registers `JsonStringEnumConverter`: always use C# member names in JSON
(`Category = "OneTime"`, assert `"Applied"`). For `GetFromJsonAsync<T>` deserialize into **DTO types**
(`DogDto`, `AdoptionDto`, …) with a `JsonSerializerOptions` carrying `JsonStringEnumConverter` —
domain entities have private setters and won't deserialize.

### Integration test isolation
Each test class uses `IClassFixture<ShelterWebFactory>` — one factory + one shared-cache in-memory DB per
class. Tests share state; mitigate with unique entity names per test. The factory rewires the named
`ShelterApi` client to the TestServer handler — keep that when adding factories.

**No E2E browser tests needed** for SSR-only pages — integration tests cover the full pipeline (page →
ApiClient → API → service → DB) in-memory.
