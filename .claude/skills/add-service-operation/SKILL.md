---
name: add-service-operation
description: Add a new operation to an existing Refugio aggregate — domain behavior, service method, REST endpoint, ApiClient method. Use when exposing a new query/command on dogs, adoptions, volunteers, finance, tasks, events or settings (e.g. "add a GetX endpoint", "new update operation").
---

# Add an operation to an existing aggregate

Flow of a request: Blazor page → `ShelterApiClient` (HTTP) → endpoint → application service →
domain behavior + repository → `IUnitOfWork.SaveChangesAsync()` (dispatches domain events) → DTO back.

## Steps

1. **Domain behavior** (commands only) — method on the entity in `Refugio.Domain/Entities/`:
   ```csharp
   public void Reschedule(DateTime due) => DueDateTime = due;
   ```
   State changes live here, not in services. Raise a domain event if other parts must react.

2. **Repository method** (if a new query shape is needed) — add to the interface in
   `Refugio.Domain/Repositories/` and implement in `Refugio.Infrastructure/Repositories/`.
   Paged lists: `query.ToPageAsync(page, pageSize)` returning `Page<T>`.
   Cross-aggregate read models (stats/aggregations) go on a query interface in
   `Refugio.Application/Queries/IReadQueries.cs` + impl in `Refugio.Infrastructure/Queries/ReadQueries.cs`
   instead of a repository.

3. **Request/response contracts** — records in `Refugio.Application/Contracts/`. Request DTOs carry the
   `Id` so endpoints can do `request with { Id = id }`.

4. **Service method** — interface + impl in `Refugio.Application/Services/XService.cs`:
   ```csharp
   public async Task<ItemDto?> RescheduleAsync(int id, DateTime due)
   {
       var item = await items.GetAsync(id);
       if (item is null) return null;
       item.Reschedule(due);
       await UnitOfWork.SaveChangesAsync();
       return item.ToDto();
   }
   ```

5. **Endpoint** — in the area's `Refugio.Web/Endpoints/*Endpoints.cs`:
   - JSON reads/writes for the ApiClient (`MapGet`/`MapPut`/`MapPost` returning DTOs, 404 on null).
   - Browser-triggered mutations stay POST forms with redirects (`/delete`, `/restore`, …) and
     `.RequireAuthorization(...)`.
   - Route literals like `/deleted`, `/paged`, `/counts` must be mapped — they coexist fine with
     `{id:int}` routes (the int constraint won't match them).

6. **ApiClient method** — `ShelterApiClient` helper shapes: `GetRequired<T>` (throws on failure),
   `GetOrNull<T>` / `PutOrNull<T>` / `PostOrNull<T>` (404 → null), `Delete` (404 → false).

7. **Page code** — pages call the ApiClient and consume DTOs only; never entities, never DbContext.

## Verify
- Unit test in `tests/Refugio.Tests.Unit/Services/` via `ServiceTestBase` (`WithServiceAsync` runs each
  call in a fresh scope, like one HTTP request).
- Integration test if the endpoint shape matters (RBAC, redirects, JSON contract).
- `dotnet test`.
