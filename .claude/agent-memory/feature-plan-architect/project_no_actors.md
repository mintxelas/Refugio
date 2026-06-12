---
name: project-no-actors
description: Refugio backend is DDD services, NOT Akka.NET actors — the actor layer was removed June 2026
metadata:
  type: project
---

The Refugio backend is a DDD stack (rich domain aggregates + application services + repos/UoW +
minimal-API endpoints), NOT an Akka.NET actor system. The actor layer (ShelterActorService, marker
interfaces like IDogMessage, ShelterActorBase, Ask<T>) was removed in June 2026.

**Why:** Documented in CLAUDE.md "Akka.NET is gone" — actors added message/routing boilerplate without
concurrency value at this traffic level.

**How to apply:** When a plan template tells me to design an "actor model", recognize it as stale for
this repo and state N/A with justification. The correct backend pattern to mirror is
**minimal-API endpoint (Endpoints/*.cs) → application service (Application/Services/I*Service) →
domain aggregate + repository → IUnitOfWork.SaveChangesAsync()**. One service per aggregate area.
Business logic lives on aggregates (private setters + behavior methods + static factories).
RBAC is `.RequireAuthorization("Manager")` on endpoints; DTOs in Application/Contracts are the wire
contract (enums serialized as member-name strings via JsonStringEnumConverter).
