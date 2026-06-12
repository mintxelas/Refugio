# Akka.NET actor layer conversion — reasoning

## Goal

Convert the backend to an Akka.NET application while (1) keeping the clean-architecture
separation and (2) leaving the HTTP API contract byte-for-byte unchanged.

## Step-by-step logic

1. **Inventory before design.** Read all 7 endpoint files, all service interfaces, DI
   registrations, the hosted reminder service, and the test layout. This fixed the exact
   surface to convert: 7 area services, ~110 endpoint operations, 1 background job.

2. **Choose where the actors sit.** Three candidate shapes:
   - *Actors replace the services* (the project's original pre-DDD design): rejected —
     moves use-case logic into actors, loses the service-level unit tests, and recreates
     the boilerplate problem documented in CLAUDE.md as the reason actors were removed.
   - *Actors behind the services*: rejected — two layers of indirection, no mailbox value
     at the request boundary.
   - **Actors in front of the services (chosen):** endpoints Ask an area actor; the actor
     opens a DI scope, resolves the existing application service, executes, replies.
     Services, domain, repositories, UnitOfWork, domain events: all untouched. The
     dependency graph stays inverted: `Refugio.Actors` references **Application only**.

3. **Make the mailbox mean something.** A naive port serializes *everything* per area and
   would throttle the SSR pages' parallel fan-out (the kanban issues 6 concurrent calls).
   So `ShelterActorBase<TService>` distinguishes:
   - `Command<TMsg>` → `ReceiveAsync` (awaited in the mailbox) → **writes serialized per area**;
   - `Query<TMsg>` → `Receive` + task + `PipeTo` → **reads concurrent**, mailbox never blocked.
   This is the actual architectural payoff: write consistency without read throttling.

4. **Message design.** Mutations reuse the existing request records from
   `Application/Contracts` — they are already immutable records carrying `Id`, i.e. valid
   actor messages; zero duplication. Only query/id-style operations needed new records
   (`Refugio.Actors/Messages/*`). A closure-envelope design (`Invoke<TService,TReply>`)
   was considered and rejected: it kills any future remoting and hides intent.

5. **Null and failure semantics.** Actors cannot `Tell(null)`, so a `NullReply` sentinel
   plus `AskFor<T>` (unwraps to `default`) and `AskRequired<T>` (non-null guarantee)
   reproduce the services' `T?` semantics at the endpoint. Handler exceptions become
   `Status.Failure`, which natively faults the `Ask` — proven by test before relying on it.

6. **Prove the layer before wiring it.** `ShelterActorBaseTests` (Akka.TestKit.Xunit2)
   pin the five contract properties: result delivery, null unwrap, fault propagation,
   command serialization (gated stub, second command must not enter while first is
   in-flight), query concurrency (both queries must enter before either completes).

7. **Increment per area, gated by the existing integration suite.** Tasks first (smallest;
   had no HTTP tests, so `TasksApiTests` was added), then adoptions (heaviest coverage,
   includes the domain-event email path), dogs, volunteers+events+auth, finance, settings.
   After each rewire, the area's pre-existing integration tests ran unmodified — that is
   the no-API-change proof, not an assertion.

8. **Background job → actor.** `AppointmentReminderService` (BackgroundService) became
   `ReminderActor` with a periodic timer (immediate first tick, 24 h interval). Behavior
   improvement: a failed digest is logged and retried next tick instead of stopping the
   host (the BackgroundService default would have).

## Decisions worth flagging

- **CQRS read models stay direct.** `I*Queries` are stateless cross-aggregate reads;
  pushing them through a mailbox adds latency and zero guarantees. Documented in CLAUDE.md.
- **`SettingsActor.GetSettings` is a Command on purpose.** The service get-or-creates the
  default row; two parallel first reads could both insert. The mailbox removes that race —
  a latent bug fixed by the model, worth the serialized read on a single-row table.
- **Duplicate route dropped.** `DogEndpoints` registered `POST /dogs/{id}/photos/upload`
  twice (identical handlers, lines 116 and 139 of the old file). Any request would have
  thrown `AmbiguousMatchException`; one copy removed during the rewrite.
- **Akka.Hosting 1.5.68** (latest stable at conversion time) — actors start via the
  hosted-service integration, which also makes them boot inside `WebApplicationFactory`
  with no test-infrastructure changes.
