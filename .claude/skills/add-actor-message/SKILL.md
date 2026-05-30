---
name: add-actor-message
description: Add a new Akka.NET request/response message to the Refugio actor system with correct marker-interface routing. Use when adding a new actor operation, message record, or actor query/command — e.g. "add a GetX message", "new actor command", "expose a new operation from DogActor/FinanceActor/etc.", or when a message throws "No actor registered for message".
---

# Add an actor message (with marker routing)

Every **request** message must carry a marker interface or `ShelterActorService.Route` throws
`No actor registered for message X` at runtime. Response/page/stat records are NOT markers.

## Marker → actor map

| Marker | Routes to | Owns |
|---|---|---|
| `IDogMessage` | `DogActor` | dogs, medical records, medications, photos, dashboard stats |
| `IFinanceMessage` | `FinanceActor` | donations, expenses, CSV export |
| `IAdoptionMessage` | `AdoptionActor` | adoptions, status pipeline, adoption reports |
| `IVolunteerMessage` | `VolunteerActor` | volunteers **and** shelter events |
| `ITaskMessage` | `TaskActor` | shelter tasks |

Markers defined in `src/Refugio.Application/Messages/ShelterMessage.cs` (all `: IShelterMessage`).

## Steps

1. **Define the message record** in `src/Refugio.Application/Messages/` (group with related messages).
   - Request → add the marker for its owning actor:
     ```csharp
     public record GetDogById(int Id) : IDogMessage;
     public record CreateDog(/* fields */) : IDogMessage;
     ```
   - Response/page/stat record → **no marker**:
     ```csharp
     public record DashboardStats(int Total, int Available);
     ```
   - `(Volunteer + Event)` messages both use `IVolunteerMessage`. Adoption reports use `IAdoptionMessage`.

2. **Register the handler** in the owning actor's constructor (`src/Refugio.Application/Actors/<X>Actor.cs`):
   ```csharp
   ReceiveAsync<GetDogById>(Handle);
   ```
   Soft-delete CRUD is a one-liner — no Handle method needed:
   ```csharp
   ReceiveAsync<DeleteDog>(msg => SoftDelete<Dog>(msg.Id));
   ReceiveAsync<RestoreDog>(msg => Restore<Dog>(msg.Id));
   ReceiveAsync<GetDeletedDogs>(_ => GetDeleted<Dog>());
   ```

3. **Write the handler** using `WithDb` (scope-per-handler — never open scopes by hand):
   ```csharp
   private Task Handle(GetDogById msg) => WithDb(async db =>
   {
       var dog = await db.Dogs.FindAsync(msg.Id);
       Sender.Tell(dog);            // always Sender.Tell a reply
   });
   ```
   Need a scoped service (e.g. email sender)? Use the overload:
   ```csharp
   WithDb(async (db, sp) => { var email = sp.GetRequiredService<IShelterEmailSender>(); ... });
   ```
   Paged result:
   ```csharp
   Sender.Tell(await q.OrderBy(x => x.Name).ToPageAsync(msg.Page, msg.PageSize));
   ```

4. **Call it** from `ShelterApiClient` / `Endpoints/*Endpoints.cs` — route on the marker, never name a ref:
   ```csharp
   var dog = await actors.Ask<Dog?>(new GetDogById(id));
   ```

## Verify
- Request message has exactly one marker interface.
- Handler registered in the **matching** actor's ctor (marker and actor must agree).
- Handler replies with `Sender.Tell(...)`.
- `dotnet test tests/Refugio.Tests/` for the actor.
