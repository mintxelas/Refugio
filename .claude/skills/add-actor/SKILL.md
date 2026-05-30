---
name: add-actor
description: Add a new Akka.NET domain actor to the Refugio shelter app. Use when introducing a new domain area that needs its own actor (e.g. a new InventoryActor, ReportActor), wiring it into the supervisor and ShelterActorService routing. Not for adding a message to an existing actor — use add-actor-message for that.
---

# Add a new domain actor

All domain actors extend `ShelterActorBase` (`src/Refugio.Application/Actors/ShelterActorBase.cs`),
which owns the `WithDb` scope-per-handler pattern and generic soft-delete CRUD
(`SoftDelete<T>`, `Restore<T>`, `GetDeleted<T>`).

## Steps

1. **Create the actor** `src/Refugio.Application/Actors/<Area>Actor.cs`:
   ```csharp
   public class InventoryActor : ShelterActorBase
   {
       public InventoryActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
       {
           ReceiveAsync<GetAllItems>(Handle);
           ReceiveAsync<DeleteItem>(msg => SoftDelete<Item>(msg.Id));
       }

       private Task Handle(GetAllItems msg) => WithDb(async db =>
           Sender.Tell(await db.Items.OrderBy(i => i.Name).ToListAsync()));
   }
   ```

2. **Add a marker interface** in `Messages/ShelterMessage.cs`:
   ```csharp
   public interface IInventoryMessage : IShelterMessage { }
   ```
   Give every request message for this actor that marker.

3. **Spawn it** in `ShelterSupervisorActor` constructor — follow the existing child-spawn pattern
   (resolver `Props<InventoryActor>()`, name e.g. `"inventory"`).

4. **Expose + route** in `src/Refugio.Application/Services/ShelterActorService.cs`:
   - Add property: `public IActorRef Inventory { get; }`
   - Resolve in ctor: `Inventory = system.ActorSelection("/user/shelter/inventory").ResolveOne(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();`
   - Add to `Route` switch: `IInventoryMessage => Inventory,`

5. **Add endpoints** (optional) — `Endpoints/InventoryEndpoints.cs` extension method, called from `Program.cs`.
   Add methods to `ShelterApiClient`.

6. **Tests** — add an actor test class under `tests/Refugio.Tests/` using `ActorTestBase`.

## Verify
- New marker added to `Route` switch (else messages throw at runtime).
- Actor name in `ActorSelection` path matches the name used in `ShelterSupervisorActor`.
- `dotnet build` + `dotnet test tests/Refugio.Tests/`.
