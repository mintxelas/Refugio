# DDD Rewrite — Summary

## What changed

The Akka.NET actor backend was replaced with a Domain-Driven Design stack, and the Blazor SSR UI now
consumes the REST API over real HTTP. All routes, JSON shapes, pages and visuals are unchanged.

| Layer | Before | After |
|---|---|---|
| Domain | Anemic POCOs, public setters | Rich aggregates: private setters, factories, behavior methods, domain events, `Entity` base, repository interfaces, `IUnitOfWork` |
| Application | 6 Akka actors + ~70 message records + marker-interface routing | 7 application services (interface+impl), DTO/request contracts, read-model query interfaces, `AdoptionStatusChanged` email handler, `VetAppointmentNotifier` |
| Infrastructure | DbContext + seed only | + EF repositories (`EfRepository<T>` base), read-query impls, `UnitOfWork` (save → dispatch domain events), `DomainEventDispatcher`, `NoOpEmailSender` |
| Web | Endpoints/pages call actors in-process | Endpoints call services; `ShelterApiClient` makes real HTTP calls with cookie forwarding; pages consume DTOs only |
| Tests | 142 Akka.TestKit actor tests / 147 integration | 218 unit (service + domain + query) / 147 integration — **365 green** |

Packages: Akka, Akka.DependencyInjection, Akka.TestKit.Xunit2 removed.

## Why

- Actors added boilerplate (messages, routing, scope-per-handler) without concurrency value at this scale.
- DDD puts each rule in one place (credential rules on `Volunteer`, pipeline on `Adoption`, soft-delete
  shapes in `ShelterServiceBase`/`EfRepository`), unit-testable without an actor framework.
- UI-through-API makes the REST surface the single contract — auth, RBAC and serialization are exercised
  on every page render, and external consumers get the same behavior the UI does.

## Behavior deltas (deliberate, small)

1. `POST /api/dogs/{id}/medical` and `/medications` now return **404** when the dog doesn't exist
   (previously a 500 FK violation).
2. `VolunteerDto` no longer exposes `PasswordHash` (the old API leaked the hash).
3. `Home.razor` no longer crashes when the Goals table is empty (pre-existing divide-by-zero, now 0%).
4. New additive endpoints for the UI: `/api/*/paged`, `/api/*/deleted`, `/api/*/deleted/{id}`,
   `/api/volunteers/counts`, `GET /api/dogs/{id}/photos`, `GET /api/expenses/{id}/photos`,
   `GET|PUT /api/medical/{id}`, `DELETE /api/medical/{id}`, `PUT /api/medications/{id}`,
   `PUT /api/adoptions/{id}`.

## Schema

**None.** `dotnet ef migrations has-pending-model-changes` reports no model changes — existing
`shelter.db` files keep working without a migration. Entity CLR namespace was intentionally kept at
`Refugio.Domain.Entities` for this reason.

## Side effects / follow-ups to be aware of

- `SettingsCacheService` is now scoped (needs the request to call the API); cache storage stays the
  process-wide `IMemoryCache`.
- Integration factory uses a shared-cache named in-memory SQLite DB (parallel API calls from SSR pages);
  a keeper connection holds it alive.
- The hosted `AppointmentReminderService` delegates to `VetAppointmentNotifier` (Application) via a scope.
- No outbox for domain-event emails (same reliability as before: post-commit, best-effort).
- CLAUDE.md and `.claude/skills/` were rewritten (`add-actor*` → `add-aggregate`, `add-service-operation`).

## Code-review checklist

1. **Contract stability:** diff an old vs new JSON payload for `/api/dogs/{id}`, `/api/adoptions` —
   property names, enum-as-string, nested `dog`/`assignedVolunteer` objects.
2. **Email asymmetry preserved:** `Adoption.ChangeStatus` raises the event; `Adoption.UpdateDetails`
   does not. See `AdoptionStatusEmailTests.Update_DoesNotSendEmail_EvenWhenStatusChanges`.
3. **Restore guards:** medical/medication restore requires a live parent dog (service re-checks with the
   filtered query); purge only accepts soft-deleted rows.
4. **Cookie forwarding:** `ForwardCookieHandler` + `UseCookies=false` on the primary handler; RBAC tests
   (manager vs volunteer vs anonymous) all green.
5. **No schema drift:** run `dotnet ef migrations has-pending-model-changes`.
6. **Scope correctness:** repositories never call `SaveChanges`; services always end mutations with
   `UnitOfWork.SaveChangesAsync()`.
7. Run `dotnet test` — expect 218 + 147 green; run the app and click through dashboard, dogs, adoptions
   kanban, funds tabs, admin deleted, settings.

## How verified here

- Full solution build clean; `has-pending-model-changes` → none.
- Live smoke test against the existing prod `shelter.db`: login, dashboard, dogs (+detail/edit), adoptions,
  funds, volunteers, calendar, health, reports, admin/deleted, settings — all 200 with real data via the
  HTTP API path.
- 218 unit + 147 integration tests green.
