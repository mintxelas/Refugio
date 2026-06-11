---
name: add-aggregate
description: Add a new DDD aggregate (domain area) to the Refugio shelter app — rich entity, repository, application service, queries, DI wiring. Use when introducing a whole new domain concept (e.g. Inventory, Sponsorships). Not for adding one operation to an existing aggregate — use add-service-operation for that.
---

# Add a new aggregate / domain area

Layering (one-way): `Domain ← Application ← Infrastructure ← Web` (Web is the composition root).

## Steps

1. **Entity** — `src/Refugio.Domain/Entities/Item.cs`, namespace **must stay** `Refugio.Domain.Entities`
   (EF migration snapshot keys on CLR full names; a namespace move makes EF regenerate tables):
   ```csharp
   public class Item : Entity, IAggregateRoot   // Entity gives Id, DeletedAt, domain events, Restore()
   {
       public string Name { get; private set; } = "";
       private Item() { }                        // EF rehydration
       public static Item Create(string name) => new() { Name = name };
       public void Update(string name) => Name = name;
   }
   ```
   Behavior methods + factories only — no public setters. Raise domain events inside behaviors
   with `Raise(new SomethingHappened(...))` (event record in `Refugio.Domain.Events`).

2. **Repository interface** — `src/Refugio.Domain/Repositories/IItemRepository.cs`:
   ```csharp
   public interface IItemRepository : IRepository<Item>   // Get/Add/Remove/RemovePermanently/GetDeleted*
   {
       Task<List<Item>> GetAllAsync();
   }
   ```

3. **DbSet + filter** in `ShelterDbContext` + EF migration (see ef-migration skill).

4. **DTO + requests** — `src/Refugio.Application/Contracts/ItemContracts.cs`. DTO property names
   define the wire contract; enums stay enum-typed (serialized as strings).

5. **Application service** — `src/Refugio.Application/Services/ItemService.cs`: interface + impl in one
   file; extend `ShelterServiceBase` for SoftDelete/Restore/Purge shapes; ctor takes repo + `IUnitOfWork`;
   map with `entity.ToDto()` extensions in `Mapping/DtoMapping.cs`. Register in
   `Application/DependencyInjection.cs`.

6. **EF repository** — `src/Refugio.Infrastructure/Repositories/ItemRepository.cs` extending
   `EfRepository<Item>`. Register in `Infrastructure/DependencyInjection.cs`.

7. **Endpoints** — `src/Refugio.Web/Endpoints/ItemEndpoints.cs` extension method on the `/api` group,
   wired in `Program.cs`. Same-route conventions: GET list/byId anonymous, mutations via POST forms →
   `.RequireAuthorization()` (destructive → `"Manager"`), REST verbs kept for external consumers.

8. **ApiClient methods** — `src/Refugio.Web/Services/ShelterApiClient.cs` (the UI consumes the API over
   HTTP; never inject services into pages).

9. **Domain events (optional)** — handler implements `IDomainEventHandler<TEvent>` in
   `Application/Events/`, registered in Application DI; `UnitOfWork` dispatches after save.

## Verify
- `dotnet build` then `dotnet test`.
- Unit: service tests over `ServiceTestBase` (in-memory EF + real repos).
- Integration: endpoint round-trip in `tests/Refugio.Tests.Integration`.
