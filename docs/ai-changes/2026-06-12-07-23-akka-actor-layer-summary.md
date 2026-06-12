# Akka.NET actor layer conversion — summary

## What changed

| Area | Change |
|---|---|
| **New project** `src/Refugio.Actors` | Akka.Hosting 1.5.68. `ShelterActorBase<TService>` (Command/Query registration, DI scope per message, `NullReply`, `Status.Failure` faults), 7 area actors (`Dog`, `Adoption`, `Volunteer`, `Event`, `Task`, `Finance`, `Settings`), `ReminderActor` (timer), `Messages/` records per area, `ActorAsk` (`AskFor`/`AskRequired`, 30 s timeout + cancellation), `AddShelterActors()`. References **Application only** — dependency inversion preserved. |
| `Refugio.Web/Program.cs` | `builder.Services.AddShelterActors()`; hosted reminder registration removed. |
| All 7 endpoint files | Same routes/verbs/status codes/redirects; handlers now `Ask` area actors via `IActorRegistry` instead of injecting `I*Service`. CQRS read-model endpoints (`I*Queries`) unchanged. |
| `Services/AppointmentReminderService.cs` | **Deleted** — replaced by `ReminderActor` (immediate first tick, then daily; failures logged, host keeps running). |
| Tests | +`Actors/ShelterActorBaseTests` (6 tests: contract incl. write serialization + read concurrency), +`Actors/ReminderActorTests`, +`TasksApiTests` (2 — /api/tasks had no HTTP coverage). `Akka.TestKit.Xunit2` added to the unit test project. |
| Docs | CLAUDE.md: architecture diagram/table updated, "Akka.NET is gone" section replaced with the new actor-layer reference, new design-decision entry, testing conventions extended. |

## Why

Requested conversion to an Akka.NET backend with two constraints honored by construction:

- **Clean architecture intact** — actors are a routing/concurrency shell; Domain,
  Application, Infrastructure not touched (services remain the single place per rule).
- **HTTP API unchanged** — the pre-existing 147 integration tests ran **unmodified** and
  pass; they exercise routes, payloads, enums-as-strings, auth/RBAC, redirects.

What the mailbox buys: writes per area are serialized (e.g. the settings get-or-create
race is now impossible); reads are dispatched concurrently so SSR fan-out keeps its
parallelism.

## Result

`dotnet test`: **225 unit + 149 integration, 0 failures.**

## Side effects / follow-ups

- A pre-existing duplicate registration of `POST /api/dogs/{id}/photos/upload` was removed
  (it would have thrown `AmbiguousMatchException` on any request — it was never functional).
- Reminder digest failure no longer stops the host (logged + retried next tick). Intentional.
- `Ask` timeout is 30 s per actor call; long-running future operations should stream or
  use `Tell` patterns instead.
- No remoting/clustering configured — single-node actor system, messages in-process.

## Code-review checklist

1. **Contract:** diff endpoint files against git history — verify only the handler bodies
   changed (routes, verbs, `RequireAuthorization`/`DisableAntiforgery`, redirect targets identical).
2. **Command/Query placement:** every mutating service call registered via `Command<>`;
   every read via `Query<>` (exception: `SettingsActor.GetSettings`, comment explains).
   A read accidentally registered as Command silently serializes behind writes.
3. **Scope discipline:** no service instance cached on an actor field; scope created and
   disposed per message in `ShelterActorBase.RunInScopeAsync` only.
4. **Null semantics:** endpoints that returned 404 on service-null still do — check
   `AskFor` (nullable) vs `AskRequired` (non-null) choices per route.
5. **Reminder parity:** `ReminderActor` fires immediately on start then every 24 h —
   same cadence as the deleted BackgroundService loop.
6. **Tests:** run `dotnet test`; confirm the serialization and concurrency tests in
   `ShelterActorBaseTests` pass (they are the architecture's regression guard).
