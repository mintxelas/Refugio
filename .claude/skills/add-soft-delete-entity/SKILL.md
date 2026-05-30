---
name: add-soft-delete-entity
description: Wire up soft delete, restore, and the admin Deleted-Records tab for a Refugio domain entity. Use when adding a new entity that needs delete/recovery, or exposing an existing entity's delete/restore through actors, endpoints, and the /admin/deleted UI. Covers ISoftDeletable, the global query filter, SoftDeleteInterceptor, and the parent-liveness guard.
---

# Add soft delete + restore for an entity

Deletes are **soft by default at the persistence layer**. `SoftDeleteInterceptor` (registered in
`ShelterDbContext.OnConfiguring`) turns any `Remove()` of an `ISoftDeletable` into setting
`DeletedAt = UtcNow`. EF global query filters exclude soft-deleted rows from all queries. Callers just
call `Remove()` and never hand-set the timestamp.

## Steps

1. **Entity implements `ISoftDeletable`** (`int Id` + `DateTime? DeletedAt`). New entity → add it.

2. **Global query filter** in `ShelterDbContext.OnModelCreating`:
   ```csharp
   modelBuilder.Entity<Item>().HasQueryFilter(e => e.DeletedAt == null);
   ```

3. **Actor handlers** — one-liners in the owning actor ctor (`ShelterActorBase` provides them):
   ```csharp
   ReceiveAsync<DeleteItem>(msg => SoftDelete<Item>(msg.Id));
   ReceiveAsync<RestoreItem>(msg => Restore<Item>(msg.Id));
   ReceiveAsync<GetDeletedItems>(_ => GetDeleted<Item>());
   ```
   Messages need the owning actor's marker interface (see add-actor-message skill).

4. **Parent-liveness guard** (child entities like medical records / medications). A child may only be
   restored while its parent is alive. Pass a guard to `Restore` — use the **normal filtered** query so
   a soft-deleted parent returns false:
   ```csharp
   ReceiveAsync<RestoreMedicalRecord>(msg => Restore<MedicalRecord>(msg.Id, DogIsAlive));
   private static Task<bool> DogIsAlive(ShelterDbContext db, MedicalRecord rec)
       => db.Dogs.AnyAsync(d => d.Id == rec.DogId);
   // load parent for the admin UI with IgnoreQueryFilters + Include (shows parent even if deleted):
   ReceiveAsync<GetDeletedMedicalRecords>(_ => GetDeleted<MedicalRecord>(q => q.Include(r => r.Dog)));
   ```

5. **Endpoints** (`Endpoints/*Endpoints.cs`):
   - `POST /api/{entity}/{id}/delete` (Blazor form) + `DELETE /api/{entity}/{id}` (REST).
   - `POST /api/{entity}/{id}/restore`.
   - All `.RequireAuthorization("Manager")`.

6. **Admin UI** — add a tab to `/admin/deleted` (`Admin/Deleted.razor`, Manager-only): list via
   `GetDeleted*`, restore via POST form. For child entities, check `rec.Dog?.DeletedAt != null` and show
   a disabled non-form label + warning instead of a restore button when the parent is also deleted
   (the actor enforces the same rule server-side — two layers, deliberate).

## Notes
- `.IgnoreQueryFilters()` only in admin/recovery contexts; on a root query it also bypasses filters on
  `.Include()`d relations in the same query.
- Events, tasks, shelter-events are intentionally NOT in the admin restore UI (no recovery value).
- New entity also needs an EF migration (see ef-migration skill).

## Verify
- Filtered query hides deleted rows; `IgnoreQueryFilters()` shows them.
- Restore blocked when parent dead (test both actor + UI).
- `dotnet test` (unit covers `GetDeleted*`/`Restore*` + parent-liveness).
