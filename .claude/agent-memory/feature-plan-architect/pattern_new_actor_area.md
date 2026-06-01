---
name: pattern-new-actor-area
description: Canonical files/precedents for adding a new actor area (entity + actor + messages + marker + endpoints) in Refugio
metadata:
  type: project
---

Adding a new domain area follows a fixed, layer-ordered recipe. Precedents to mirror:

- **Entity:** Domain/Entities/*.cs implementing ISoftDeletable (int Id + DateTime? DeletedAt). Newest precedent: Goal.cs.
- **DbContext:** add DbSet in ShelterDbContext + a HasQueryFilter(e => e.DeletedAt == null) line in OnModelCreating. Enums map via HasConversion<string>(). Decimals via HasColumnType.
- **Seed:** SeedData.Seed adds rows inside the `if (db.Dogs.Any()) return;` guard.
- **Marker:** add interface IXMessage : IShelterMessage in Messages/ShelterMessage.cs. Existing: IDog/IFinance/IAdoption/IVolunteer/ITask.
- **Messages:** records in Messages/XMessages.cs, each implementing the marker. Response/page/stat records are NOT markers.
- **Actor:** Actors/XActor.cs extends ShelterActorBase(IServiceScopeFactory); ctor registers ReceiveAsync handlers; handlers use WithDb(async db => ...) and Sender.Tell(...). Generic CRUD one-liners: SoftDelete<T>(id), Restore<T>(id, guard?), GetDeleted<T>(), GetDeletedById<T>, PermanentDelete<T>.
- **Wiring:** ShelterSupervisorActor.cs spawns the child (Context.ActorOf(resolver.Props<XActor>(), "name")); ShelterActorService.cs adds an IActorRef property, resolves /user/shelter/name in ctor, and adds the marker case to Route().
- **Facade:** ShelterApiClient.cs (scoped) adds methods calling actors.Ask<T>(new Msg(...)). No call site names an actor ref.
- **Endpoints:** Endpoints/XEndpoints.cs MapXEndpoints extension; registered in Program.cs under the /api group (or app root for non-api). Form POST actions return Results.Redirect; destructive use .RequireAuthorization("Manager"). File uploads use .DisableAntiforgery().

Skills cover each: add-actor, add-actor-message, ef-migration, add-soft-delete-entity, blazor-ssr-form-page, add-localization.

NOTE: Goal lives inside FinanceActor (no separate GoalActor) — so a new entity does NOT always get its own actor; judge by domain area. See [[pattern-mainlayout-data]].
