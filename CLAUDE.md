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

# Run React dev server (proxies /api/* to the .NET backend)
cd src/Refugio.Web/ClientApp && npm run dev

# Build React SPA for production (output → ClientApp/dist/, served by .NET)
cd src/Refugio.Web/ClientApp && npm run build

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
Refugio.Web           ← composition root. REST API (Endpoints/*Endpoints.cs) + React SPA (ClientApp/).
                        Endpoints Ask the area actors; SPA calls the API over HTTP from the browser.
```

A request flows: **React SPA (browser) → `/api/*` endpoint → area actor (`Ask`, fresh DI scope per message) →
application service → domain behavior + repository → `IUnitOfWork.SaveChangesAsync()` (dispatches domain events) →
DTO reply → JSON response → React state update**.

| Project | Key pieces |
|---|---|
| `Refugio.Domain` | `Common/Entity` (Id, DeletedAt, domain events, `Restore()`), `IAggregateRoot`, `Page<T>`, `IUnitOfWork`, `Repositories/I*Repository`, `Events/AdoptionStatusChanged`, rich entities in `Entities/`, `PasswordHelper`, `Roles` |
| `Refugio.Application` | `Contracts/` (DTOs + request records — the wire contract), `Services/` (interface + impl per aggregate, `ShelterServiceBase` for delete/restore/purge shapes), `Queries/IReadQueries` (stats read models), `Abstractions/` (`IShelterEmailSender`, `IDomainEventHandler<T>`, `IDomainEventDispatcher`), `Events/AdoptionStatusChangedHandler`, `Mapping/DtoMapping`, `AddApplicationServices()` |
| `Refugio.Infrastructure` | `ShelterDbContext`, `SoftDeleteInterceptor`, `UnitOfWork`, `DomainEventDispatcher`, `Repositories/EfRepository<T>` + per-aggregate repos, `Queries/ReadQueries`, `Email/NoOpEmailSender`, `SeedData`, `DatabaseInitializer`, EF migrations, `AddInfrastructureServices()` |
| `Refugio.Actors` | `ShelterActorBase<TService>` (Command/Query registration, scope-per-message, `Status.Failure` on faults), area actors (`Dog,Adoption,Volunteer,Event,Task,Finance,Settings`Actor), `ReminderActor` (daily vet digest on an actor timer), `Messages/` (query/id records per area), `NullReply`, `ActorAsk` (`AskFor`/`AskRequired`), `ActorSystemRegistration.AddShelterActors()` |
| `Refugio.Web` | `Program.cs` (wiring only), `Endpoints/{Dog,Adoption,Task,Finance,Volunteer,Settings,Auth}Endpoints.cs` (Ask area actors via `IActorRegistry`), `Helpers/` (`PhotoFiles`, `ImageResizer`), `ClientApp/` (React SPA — see below) |

Two test projects:

| Project | Role |
|---|---|
| `tests/Refugio.Tests.Unit` | Service tests (real services + repos + UoW over in-memory EF via `ServiceTestBase`; each call in a fresh scope), domain behavior tests, read-model query tests, `PasswordHelper` tests. |
| `tests/Refugio.Tests.Integration` | `WebApplicationFactory` + SQLite **shared-cache named in-memory DB** (`Mode=Memory;Cache=Shared` + keeper connection). Tests call the REST API directly via `HttpClient`. Auth via `POST /api/auth/login` JSON. |

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
- **DTOs in `Contracts/` are the wire contract** — property names match what the API serializes.
  `VolunteerDto` deliberately has **no PasswordHash**.
- Request records carry `Id` so endpoints can `request with { Id = id }`.
- Cross-aggregate aggregations (dashboard, finance summary, adoption conversion, shelter stay, volunteer
  counts, upcoming vet visits) are **CQRS-lite read models**: interfaces in `Application/Queries`,
  EF implementations in `Infrastructure/Queries`, injected straight into endpoints.
- `Page<T>(Items, TotalCount, PageNumber, PageSize)` lives in `Domain.Common`; repos build it with
  `IQueryable.ToPageAsync(page, size)` (`Infrastructure/Repositories/EfRepository.cs`).

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
  serialized**. `Query<TMsg>` dispatches the task and `PipeTo`s the reply — **reads stay concurrent**.
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

## React SPA — `ClientApp/`

**The UI is a React SPA** (Vite + React 18 + TypeScript + React Router). There are no Blazor/Razor pages.
Do NOT create or edit `.razor` files for feature work.

```
ClientApp/
  src/
    api/          ← typed API clients (finance.ts, dogs.ts, …); call /api/* endpoints
    auth/         ← AuthContext (current user, login/logout)
    components/   ← shared UI components (Layout, Sidebar, Pagination, StatusChip, …)
    pages/        ← one file per route (Funds.tsx, DonationEdit.tsx, Dogs.tsx, …)
    types.ts      ← all DTO interfaces (DonationDto, DogDto, …) mirroring the C# Contracts
    main.tsx      ← React Router routes
```

Key conventions:
- **Types**: add/update interfaces in `types.ts` whenever a DTO changes in C# `Contracts/`.
- **API client**: add/update the typed call in the matching `api/*.ts` file.
- **Auth**: cookie-based; `AuthContext` calls `GET /api/auth/me` on load; login via `POST /api/auth/login` (JSON).
- **Routing**: React Router SPA routes; the .NET backend serves `ClientApp/dist/index.html` as the fallback for all non-`/api/*` paths (when `dist/` exists).
- **No localization**: UI strings are hardcoded in English in JSX. No RESX files.
- **Tailwind CSS**: configured in `ClientApp/` (`tailwind.config.js`). Use design tokens (same MD3 names as the old Blazor app: `primary`, `surface-*`, `on-*`, etc.).
- **Amount formatting in CSV export**: always use `CultureInfo.InvariantCulture` in backend CSV generation (`Amount.ToString("F2", CultureInfo.InvariantCulture)`) — the server has no locale middleware.

---

## Authentication and authorization

Cookie-based (`CookieAuthenticationDefaults`). Sessions last 7 days (sliding expiry).
All auth endpoints are under `/api/auth/` and return JSON.

| Endpoint | Method | Notes |
|---|---|---|
| `POST /api/auth/login` | JSON body `{email, password}` | Returns `{id, name, email, role}`; sets auth cookie |
| `GET /api/auth/me` | — | Returns current user or 401 |
| `POST /api/auth/logout` | — | Clears cookie, returns 200 |
| `POST /api/auth/change-password` | JSON body `{currentPassword, newPassword, confirmPassword}` | Returns 200 or 400 |

Password hashing: PBKDF2-SHA256 via `PasswordHelper`; credential rules live **on the `Volunteer` aggregate**
(`Register`/`Update`/`EnableLogin`/`VerifyPassword`/`ChangePassword` — no login → no hash, no language).

Roles: `Volunteer.Role` is `Roles.Manager` or `Roles.Volunteer` (constants — never inline the strings).
Policy `"Manager"` restricts destructive endpoints. Seed login: `elena@havensanctuary.org` / `shelter123` (Manager).

---

## Styling

Tailwind CSS (in `ClientApp/`). Material Design 3 token names: `primary`, `secondary`, `tertiary`,
`surface-*`, `on-*`, `*-container`, `*-fixed`. **Never use arbitrary hex values** — always the named tokens.

---

## Pages and routes

| Page | Route | React file |
|---|---|---|
| Home / Dashboard | `/` | `Home.tsx` |
| Dog catalog | `/dogs` | `Dogs.tsx` |
| Dog detail | `/dogs/:id` | `DogDetail.tsx` |
| Dog edit / check-in | `/dogs/:id/edit`, `/dogs/new` | `DogEdit.tsx` |
| Health dashboard | `/health` | `Health.tsx` |
| Adoptions | `/adoptions` | `Adoptions.tsx` |
| Adoption edit | `/adoptions/:id` | `AdoptionEdit.tsx` |
| Calendar | `/calendar` | `Calendar.tsx` |
| Event edit | `/calendar/events/:id` | `EventEdit.tsx` |
| Funds | `/funds` | `Funds.tsx` |
| Donation edit | `/funds/donations/:id` | `DonationEdit.tsx` |
| Expense edit | `/funds/expenses/:id` | `ExpenseEdit.tsx` |
| Goal edit | `/funds/goals/:id` | `GoalEdit.tsx` |
| Volunteers | `/volunteers` | `Volunteers.tsx` |
| Volunteer edit | `/volunteers/:id` | `VolunteerEdit.tsx` |
| Reports | `/reports` | `Reports.tsx` |
| Admin — Deleted Records | `/admin/deleted` | `AdminDeleted.tsx` |
| Settings | `/settings` | `Settings.tsx` |
| Change password | `/change-password` | `ChangePassword.tsx` |
| Login | `/login` | `Login.tsx` |

---

## Key design decisions and tradeoffs

### React SPA replaced Blazor SSR (June 2026)
**Decision:** The UI is a React SPA (Vite + TypeScript + React Router) serving from `ClientApp/dist/` as static files. Blazor/Razor pages, RESX localization, `ShelterApiClient`, `ForwardCookieHandler`, `SettingsCacheService`, `FormReader`, `DogHelpers`, and `Validator` were all deleted.
**Why:** React is the team's preferred frontend. The REST API contract was unchanged — React simply became the new first consumer.
**Tradeoff:** No server-side rendering; initial HTML is the SPA shell. Auth is cookie-based (same as before), but the cookie is now set by the JSON `/api/auth/login` endpoint rather than a form POST.

### Akka.NET reintroduced as a shell in front of the services (June 2026)
**Decision:** The backend runs as an Akka.NET application, but actors are a thin layer **in front of** the unchanged application services: endpoints Ask area actors; actors run the service call in a fresh DI scope and reply.
**Why:** Restores the actor model's mailbox guarantees (writes per area serialized; the `SettingsActor` mailbox fixes a latent duplicate-default-row race) without giving up the DDD wins.
**Tradeoff:** One extra hop per API call (~Ask overhead, μs-ms); message records duplicate query parameter lists; reads must be registered as `Query<>` or they would serialize behind writes.

### DTO contracts are the wire contract
**Decision:** DTO property names/shapes are the stable API surface. C# uses camelCase JSON serialization (`JsonStringEnumConverter` registered).
**Why:** React `types.ts` mirrors these exactly. Any C# DTO change requires a matching `types.ts` update.
**Tradeoff:** Two places to update per field addition (C# + TypeScript).

### Domain events for side effects
**Decision:** `Adoption.ChangeStatus` raises `AdoptionStatusChanged`; `UnitOfWork` dispatches after save; an Application handler sends the email.
**Why:** Side effect declared at the state change, fires only after a successful commit.
**Critical:** `UpdateAdoption` (full edit) changes status **without** an event/email — that asymmetry is intentional, preserved from the original behavior.

### Repositories + UnitOfWork, repositories never save
**Decision:** Repos mutate the change tracker; only `IUnitOfWork.SaveChangesAsync()` commits (and dispatches events).
**Why:** One commit point per use case; event dispatch can't be bypassed.

### Entity namespace pinned to `Refugio.Domain.Entities`
**Decision:** Files may organize by aggregate, but the CLR namespace of mapped entities does not change.
**Why:** The EF model snapshot keys on CLR full names; renaming would generate drop/create migrations for every table.

### Soft delete via `Entity`/`ISoftDeletable` + EF global filters + interceptor
Interceptor converts `Remove()` to a timestamp; global filters hide deleted rows; `.IgnoreQueryFilters()` only in admin/recovery contexts (it propagates to `.Include()`s in the same query).

### Integration tests: SQLite shared-cache named in-memory DB
**Decision:** `Data Source=RefugioTests-{guid};Mode=Memory;Cache=Shared` + a keeper connection per factory.
**Why:** Allows multiple concurrent `DbContext` connections to the same in-memory DB (needed for parallel API calls in tests). Unique name isolates class fixtures.
**Tradeoff:** Tests within a class still share one DB — use unique entity names per test.

---

## Testing conventions

### Unit tests (`tests/Refugio.Tests.Unit`)
- `ServiceTestBase` builds the real DI graph (services + repos + UoW + dispatcher + queries) over a
  unique in-memory EF DB. `WithServiceAsync<TService,T>` runs each call in a **fresh scope** — mirrors
  one HTTP request and keeps the change tracker honest.
- Seed via domain factories (`Dog.CheckIn(...)`), set `DeletedAt` directly when seeding deleted rows.
- Override `ConfigureServices` to swap adapters (e.g. `CapturingEmailSender` for the email tests).
- Domain behavior tests (`Domain/EntityBehaviorTests.cs`) cover event raising, credential rules,
  pipeline transitions — no DB needed.
- Actor tests (`Actors/`, `Akka.TestKit.Xunit2`): `ShelterActorBaseTests` proves the layer contract
  (scope-per-message, NullReply unwrap, `Status.Failure` → faulted Ask, command serialization, query
  concurrency) against a probe service; `ReminderActorTests` proves the digest timer.

### Enum values in integration test JSON bodies
`ConfigureHttpJsonOptions` registers `JsonStringEnumConverter`: always use C# member names in JSON
(`Category = "OneTime"`, assert `"Applied"`). For `GetFromJsonAsync<T>` deserialize into **DTO types**
with a `JsonSerializerOptions` carrying `JsonStringEnumConverter` — domain entities have private setters.

### Integration test isolation
Each test class uses `IClassFixture<ShelterWebFactory>` — one factory + one shared-cache in-memory DB per
class. Tests share state; mitigate with unique entity names per test.
Auth in tests: `CreateAuthenticatedClientAsync()` calls `POST /api/auth/login` with JSON
(`elena@havensanctuary.org` / `shelter123`). For volunteer-role clients, create the volunteer via the API then
call `POST /api/auth/login` with that volunteer's credentials.
