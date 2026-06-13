# Reasoning — Dog photo storage reorganization

## Goal

Move dog photos from a flat `wwwroot/dogs/` directory to per-dog subfolders `wwwroot/photos/dogs/{dogId}/`, tighten file validation to PNG/JPG only and 2 MB max, and persist the URL path in the `DogPhoto.Url` column so images load from there.

## Step-by-step logic

1. **Understand current state.** Photos stored flat in `wwwroot/dogs/`. Primary photo named `{id}.{ext}`, gallery photos `{dogId}_{guid}.{ext}`. URL stored in `DogPhoto.Url` (already in DB). Allowed: `.jpg/.jpeg/.png/.webp`, max 5 MB. `app.UseStaticFiles()` serves all of `wwwroot` — any subfolder works automatically.

2. **Identify all upload paths.** Three endpoints write files:
   - `POST /api/dogs/{id}/photo` — primary photo replacement
   - `POST /api/dogs/{id}/photos` — gallery multi-upload (SSR form)
   - `POST /api/dogs/{id}/photos/upload` — gallery multi-upload (JSON, React SPA)

3. **Choose new path structure.** `wwwroot/photos/dogs/{dogId}/{filename}` groups all dog photos together and isolates each dog. No migration needed — `DogPhoto.Url` already stores the URL; we just store a different URL value going forward. Existing photos keep their old URLs (still served by `UseStaticFiles`).

4. **Primary photo naming.** Kept as `primary.{ext}` so old-style `{id}.*` glob deletion can be replaced with a deterministic `primary.*` pattern within the dog's subfolder.

5. **Gallery photo naming.** Simplified from `{dogId}_{guid}.{ext}` to `{guid}.{ext}` — the dog subfolder makes the `dogId` prefix redundant.

6. **Extension filter.** Reduced from `.jpg/.jpeg/.png/.webp` to `.jpg/.png` per requirements. `.jpeg` is an alias for `.jpg` not uploaded by most tools (browser file pickers use `.jpg`), `.webp` excluded.

7. **Size limit.** Reduced from 5 MB to 2 MB per requirements.

8. **No domain/service/infrastructure changes.** `DogPhoto.Url` is just a string; storing a different path value requires no entity, migration, or service change.

9. **Feature doc updated.** `features/feature-dogs.md` UC-D5 and UC-D6 updated to reflect new rules, path, and the JSON upload endpoint documentation added.

## Alternatives considered

- **Migrate existing photos to new paths** — rejected: existing `DogPhoto.Url` values in the DB would become stale. Old files remain served; migration of physical files is a separate operational concern outside scope.
- **Store only the filename in DB, resolve path at serve time** — rejected: `Url` is the wire contract (exposed in `DogPhotoDto`); changing its semantics breaks clients and tests.
- **Subdirectory by date** — rejected: per-dog is simpler and aligns with how the rest of the app is structured (one dog = one unit).
