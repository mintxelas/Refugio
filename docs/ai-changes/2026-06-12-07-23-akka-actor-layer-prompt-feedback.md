# Akka.NET actor layer conversion — prompt feedback

## Original prompt

> You are a software architect expert in distributed systems. Convert the current backend
> to an Akka.net application keeping the clean architecture separation and the http API
> without changes.

## What worked well

- **Two hard constraints stated up front** ("clean architecture separation", "http API
  without changes") — both are verifiable, and the second is mechanically provable by the
  existing integration suite. Constraints that map to tests are the best kind.
- Short and goal-oriented; no over-specification of mechanism, which left room to pick the
  actors-in-front-of-services shape that satisfies both constraints.

## What was ambiguous (and how it was resolved)

1. **Depth of conversion** — actors *replacing* the application services vs actors
   *fronting* them. Resolved to fronting: replacing would have violated the
   clean-architecture constraint in spirit (use-case logic migrating into actor classes)
   and rewritten code the repo's history deliberately settled. If you wanted logic moved
   INTO actors, say so explicitly.
2. **Read path** — should stateless CQRS read models (`IDashboardQueries` etc.) go through
   actors? Resolved: no (documented in CLAUDE.md). State the expectation if you want 100%
   of traffic through the mailbox.
3. **Background jobs** — the prompt says "backend", which was interpreted to include the
   daily reminder BackgroundService → `ReminderActor`. Confirm scope when jobs exist.
4. **Distribution** — "expert in distributed systems" hints at remoting/clustering, but a
   single-box app got a single-node actor system (messages kept remoting-safe regardless).
   If clustering/sharding is the actual end goal, name it — it changes message and
   supervision design significantly.

## Suggestions for future prompts of this kind

- Name the target topology: `single-node actor system` vs `cluster-ready (Akka.Cluster /
  sharding)` — the biggest fork in any Akka design.
- State the consistency goal explicitly if you have one, e.g. "writes to the same
  aggregate area must be serialized" — that one sentence drives the Command/Query split
  and lets the implementer prove it with a test.
- Reference the acceptance gate: "the existing integration tests must pass unmodified" —
  it was assumed here, but saying it makes the definition of done unambiguous.
- For conversions, say what may be deleted (e.g. "the hosted reminder service may be
  replaced by an actor timer") so behavior swaps don't need inference.

## Suggested improved prompt

> Convert the backend to a single-node Akka.NET application using Akka.Hosting.
> Keep the DDD layering: actors form a new layer in front of the existing application
> services (do not move use-case logic into actors). The HTTP API must not change — the
> existing integration tests must pass unmodified. Writes to an aggregate area must be
> serialized through the area actor's mailbox; reads must stay concurrent. Replace the
> AppointmentReminderService with an actor timer. CQRS read-model endpoints may stay
> direct. Work incrementally per area with tests after each step.
