# Feature: Admin — Deleted Records (soft delete, restore, purge)

Every entity is soft-deletable; a Manager-only admin page lists deleted rows per entity type and offers restore or permanent purge.

## Mechanism

- All entities extend `Entity` (`ISoftDeletable`: `DeletedAt`, `Restore()`).
- `SoftDeleteInterceptor` (registered in `OnConfiguring`, so it reaches all test contexts) converts `Remove()` into `DeletedAt = UtcNow`.
- EF **global query filters** exclude soft-deleted rows from every query; admin/recovery queries opt out with `.IgnoreQueryFilters()` (which propagates to `.Include()`s in the same query).
- `RemovePermanently()` arms a `SkipSoftDeleteInterceptor` flag consumed at the next save → real DELETE (purge).
- Service base class `ShelterServiceBase` provides the delete/restore/purge shapes for all aggregates.

## Use cases

### UC-X1: Browse deleted records (Manager)
- **UI:** `/admin/deleted` — 8 tabs: dogs, medical, medications, adoptions, volunteers, donations, expenses, goals (`?tab=`).
- **API:** per area, `GET /api/{area}/deleted` and `GET /api/{area}/deleted/{id}` (Manager policy).

### UC-X2: Restore a record (Manager)
- **API:** `POST /api/{area}/{id}/restore` → redirect back to the relevant tab.
- **Parent-liveness rule:** dog children (medical records, medications) cannot be restored while the parent dog is deleted. Double-guarded — the UI disables the button with a warning, and the service re-checks parent liveness using the filtered query before restoring.

### UC-X3: Purge a record (Manager, irreversible)
- **API:** `POST /api/{area}/{id}/purge` → permanent DELETE via `RemovePermanently()`, redirect back to the tab.
- UI uses a JS `confirm(...)` before submitting.

### UC-X4: REST soft delete
- External consumers use `DELETE /api/{area}/{id}` → 204/404; same soft-delete semantics.

## Coverage matrix

| Entity | Delete | Restore | Purge | Admin tab |
|---|---|---|---|---|
| Dog | yes | yes | yes | dogs |
| MedicalRecord | yes | parent-guarded | yes | medical |
| Medication | yes (REST DELETE = deactivate instead) | parent-guarded | yes | medications |
| Adoption | yes | yes | yes | adoptions |
| Volunteer | yes | yes | yes | volunteers |
| Donation | yes | yes | yes | donations |
| Expense | yes | yes | yes | expenses |
| Goal | yes | yes | yes | goals |
| ShelterTask | yes (soft) | no UI | no | — |
| ShelterEvent | yes (soft) | no UI | no | — |
