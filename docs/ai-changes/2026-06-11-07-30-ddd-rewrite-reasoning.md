# DDD Rewrite — Reasoning Process

## Goal
Rewrite the backend as Domain-Driven Design, expose it through the REST API, and make the Blazor SSR UI
consume that API — keeping all functionality and the exact visual appearance.

## Step-by-step logic

1. **Survey first.** Read every entity, all 6 actors, all message files, `ShelterActorService`, all 7
   endpoint files, `ShelterApiClient`, `Program.cs`, DbContext/interceptor/seed, both test bases.
   The codebase had grown past its own CLAUDE.md (Goals, Settings, photo galleries, purge endpoints),
   so the docs could not be trusted as the spec — the code and the 147 integration tests were the spec.

2. **Pick the safety rails.** The 147 integration tests pin routes, JSON shapes, RBAC and redirects.
   Decision: keep every route and wire shape identical so those tests remain the behavioral contract,
   and measure success as "integration suite green with only type-level edits".

3. **Layering with dependency inversion.** Old flow was `Domain → Infrastructure → Application → Web`
   (actors used the DbContext directly). New flow: Domain has zero deps; Application depends only on
   Domain; Infrastructure implements Domain repository interfaces and Application query/abstraction
   interfaces; Web is the composition root. This is the textbook DDD shape and makes the application
   layer unit-testable without EF-specific knowledge leaking upward.

4. **Rich aggregates without schema drift.** Entities got private setters, private EF constructors,
   static factories (`Dog.CheckIn`, `Adoption.Submit`, `Volunteer.Register`, …) and behavior methods.
   Two hard constraints discovered and honored:
   - **CLR namespace must stay `Refugio.Domain.Entities`** — the EF migration snapshot keys entities by
     full CLR name; a namespace move would diff as drop/create of every table.
   - **Property names/types unchanged** — verified with `dotnet ef migrations has-pending-model-changes`
     → "No changes have been made to the model since the last migration." Zero migrations needed;
     existing production `shelter.db` works untouched.

5. **Behavior placement.** Each actor handler's logic was either (a) a state transition → moved into an
   entity behavior; (b) orchestration → application service method; (c) cross-aggregate aggregation →
   CQRS-lite read-model query (`IDashboardQueries`, `IFinanceQueries`, `IAdoptionQueries`,
   `IVolunteerQueries`, `IMedicalQueries`) implemented in Infrastructure. The volunteer credential rules
   (no login → no hash/language) moved from the actor into `Volunteer` itself.

6. **Domain events for the one real side effect.** The adoption status-change email became:
   `Adoption.ChangeStatus` raises `AdoptionStatusChanged` → `UnitOfWork` dispatches after a successful
   save → `AdoptionStatusChangedHandler` emails. Crucially, `UpdateDetails` (full edit) changes status
   silently — the old `UpdateAdoption` never emailed, and that asymmetry is preserved and now unit-tested.

7. **Repositories never save.** Only `IUnitOfWork.SaveChangesAsync()` commits and dispatches events, so
   event dispatch cannot be bypassed. `RemovePermanently` arms the existing `SkipSoftDeleteInterceptor`
   flag that the interceptor consumes at save time — the soft-delete policy stayed exactly where it was.

8. **UI → API over real HTTP.** `ShelterApiClient` was rewritten from in-process actor asks to
   `HttpClient` JSON calls. Three subtleties:
   - The named client's primary handler sets `UseCookies = false` — otherwise the handler's cookie
     container silently discards the manually forwarded Cookie header.
   - `ForwardCookieHandler` copies the incoming request's cookies so auth/RBAC/culture flow through.
   - Base address derives from the current request; in tests the named client is rewired to
     `TestServer.CreateHandler()` because `WebApplicationFactory` has no real socket.

9. **Additive API surface for the UI.** Reads the UI used to get in-process needed real endpoints:
   paged lists (`/paged`), `/volunteers/counts`, deleted lists/by-id (`/deleted`, `/deleted/{id}`),
   photo GETs, `GET/PUT /api/medical/{id}`, `PUT /api/medications/{id}`, `PUT /api/adoptions/{id}`.
   All additive — existing routes untouched. `DELETE /api/medications/{id}` keeps its legacy
   "deactivate" semantics for external consumers.

10. **Pages: type swaps only.** DTO property names mirror the old entity JSON, so the ~21 Razor pages
    changed only in `@code` blocks (entity → DTO types, message records → request records). Markup
    untouched → visual aspect identical.

11. **Verification loop per layer.** Build after each layer; `has-pending-model-changes` after Domain;
    live smoke test (login + every page + API JSON) after Web; full suites at the end.

## Problems found and fixed (evidence first)

- **`database is locked` in integration tests.** The adoptions kanban issues 6 parallel API calls
  (`Task.WhenAll`); each opens a scoped DbContext. The old single shared `:memory:` SqliteConnection
  cannot initialize concurrent contexts (actors had serialized DB access via their mailbox; HTTP does
  not). Fix: shared-cache named in-memory DB (`Mode=Memory;Cache=Shared` + keeper connection) — each
  context gets its own connection to the same database. Production (file DB, per-context pooled
  connections) was verified unaffected by live smoke test.
- **Pre-existing `DivideByZeroException` in `Home.razor`.** The donation-goal percentage divides by the
  Goals-table sum; with zero goals (the current prod DB state) the dashboard crashed — on the old code
  too. Guarded to 0%.
- **Stale unit-test expectation.** `GetDashboardStats_ReturnsStatsWithCorrectGoal` asserted a hardcoded
  12000 goal that predates "goal = sum of Goals targets" (commit 079204a). Replaced with explicit
  empty-DB (0) and seeded-goals (sum) tests.
- **`PasswordHash` leak.** The old API serialized whole `Volunteer` entities, hash included.
  `VolunteerDto` has no such property; a test asserts the DTO type cannot leak it.

## Alternatives considered and rejected

- **Keep actors under the services** — two indirection layers, none of the DDD benefits.
- **MediatR** — replaces actor routing with handler routing; still no rich domain model; new dependency.
- **Move entity namespaces per aggregate folder** — EF snapshot keys on CLR names → destructive
  migrations. File organization was deemed not worth corrupting schema history.
- **Strict aggregate purism for Dog children** (load whole aggregate to edit one medication) — would
  multiply queries for zero behavioral gain; children are reachable through `IDogRepository` instead.
- **Separate `Refugio.Api` host** — breaks single-binary deployment, cookie domain, and the test setup.
- **Serializing the kanban's API calls** to dodge the SQLite lock — would change app behavior to
  accommodate a test-infra limitation; fixed the test infra instead.
- **AutoMapper** — hand-written `ToDto()` extensions are shorter than the config would be.
