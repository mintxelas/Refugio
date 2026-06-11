---
name: add-soft-delete-entity
description: Wire up soft delete, restore, and the admin Deleted-Records tab for a Refugio domain entity. Use when adding a new entity that needs delete/recovery, or exposing an existing entity's delete/restore through services, endpoints, and the /admin/deleted UI. Covers ISoftDeletable, the global query filter, SoftDeleteInterceptor, and the parent-liveness guard.
---

# Add soft delete + restore for an entity

Deletes are **soft by default at the persistence layer**. `SoftDeleteInterceptor` (registered in
`ShelterDbContext.OnConfiguring`) turns any `Remove()` of an `ISoftDeletable` into setting
`DeletedAt = UtcNow`. EF global query filters exclude soft-deleted rows from all queries. Callers just
call `Remove()` and never hand-set the timestamp.

## Steps

1. **Entity extends `Entity`** (gives `Id`, `DeletedAt`, `Restore()`, domain events). Aggregate roots
   also implement `IAggregateRoot`.

2. **Global query filter** in `ShelterDbContext.OnModelCreating`:
   ```csharp
   modelBuilder.Entity<Item>().HasQueryFilter(e => e.DeletedAt == null);
   ```

3. **Repository** — `IRepository<T>` already provides `Remove` (soft), `RemovePermanently` (arms the
   skip flag), `GetDeletedAsync`, `GetDeletedByIdAsync`. Override the GetDeleted* impls when the admin
   UI needs an `Include` (e.g. the parent dog).

4. **Service methods** — `ShelterServiceBase` provides the shapes:
   ```csharp
   public Task<bool> DeleteAsync(int id) => SoftDeleteAsync(() => items.GetAsync(id), items.Remove);
   public Task<bool> RestoreAsync(int id) => RestoreAsync(() => items.GetDeletedByIdAsync(id));
   public Task<bool> PurgeAsync(int id) => PurgeAsync(() => items.GetDeletedByIdAsync(id), items.RemovePermanently);
   ```
   Restore/Purge only accept currently-deleted rows (the GetDeleted* loaders enforce that — purge of a
   live record must return false).

5. **Parent-liveness guard** (child entities like medical records / medications). A child may only be
   restored while its parent is alive — check with the **normal filtered** query so a soft-deleted
   parent blocks it:
   ```csharp
   public async Task<bool> RestoreMedicalRecordAsync(int id)
   {
       var record = await dogs.GetDeletedMedicalRecordByIdAsync(id);   // IgnoreQueryFilters + Include(Dog)
       if (record is null || !await dogs.ExistsAsync(record.DogId)) return false;
       record.Restore();
       await UnitOfWork.SaveChangesAsync();
       return true;
   }
   ```

6. **Endpoints** (`Endpoints/*Endpoints.cs`):
   - `POST /api/{entity}/{id}/delete` (Blazor form) + `DELETE /api/{entity}/{id}` (REST).
   - `POST /api/{entity}/{id}/restore`, `POST /api/{entity}/{id}/purge`.
   - `GET /api/{entity}/deleted` + `GET /api/{entity}/deleted/{id}` for the admin UI.
   - All `.RequireAuthorization("Manager")`.

7. **ApiClient + Admin UI** — `GetDeletedXs()`/`GetDeletedXById()` methods on `ShelterApiClient`; add a
   tab to `/admin/deleted` (Manager-only): list + restore/purge POST forms. For child entities, check
   `rec.Dog?.DeletedAt != null` and show a disabled non-form label + warning instead of a restore button
   when the parent is also deleted (the service enforces the same rule server-side — two layers, deliberate).

## Notes
- `.IgnoreQueryFilters()` only in admin/recovery contexts; on a root query it also bypasses filters on
  `.Include()`d relations in the same query.
- Events, tasks, shelter-events are intentionally NOT in the admin restore UI (no recovery value).
- New entity also needs an EF migration (see ef-migration skill).

## Verify
- Filtered query hides deleted rows; `IgnoreQueryFilters()` shows them.
- Restore blocked when parent dead (test both service + UI).
- `dotnet test` (unit covers GetDeleted*/Restore*/Purge* + parent-liveness).
