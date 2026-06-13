# Summary — Dog photo storage reorganization

## What changed

| File | Change |
|---|---|
| `src/Refugio.Web/Endpoints/DogEndpoints.cs` | All three photo upload endpoints updated |
| `features/feature-dogs.md` | UC-D5 and UC-D6 updated to reflect new rules |

### `DogEndpoints.cs` — three upload endpoints

**`POST /api/dogs/{id}/photo` (primary photo)**
- Directory: `wwwroot/dogs/` → `wwwroot/photos/dogs/{id}/`
- Deletes previous primary by glob `primary.*` in the dog's folder (was `{id}.*` in flat folder)
- File saved as `primary.{ext}` (was `{id}.{ext}`)
- URL stored: `/photos/dogs/{id}/primary.{ext}` (was `/dogs/{id}.{ext}`)
- Allowed extensions: `.jpg/.png` (was `.jpg/.jpeg/.png/.webp`)
- Max size: 2 MB (was 5 MB)

**`POST /api/dogs/{id}/photos` (gallery SSR form)**
- Directory: `wwwroot/photos/dogs/{id}/`
- File saved as `{guid}.{ext}` (was `{dogId}_{guid}.{ext}`)
- URL stored: `/photos/dogs/{id}/{guid}.{ext}`
- Same extension/size constraints as above

**`POST /api/dogs/{id}/photos/upload` (gallery JSON, React SPA)**
- Same changes as gallery SSR form

## Why

Requirements mandated per-dog subfolders (`photos/dogs/{dog_id}/`), PNG/JPG only, and 2 MB max. No database schema change was needed — `DogPhoto.Url` already stores a URL string; we store a different path value.

## Side effects

- **Existing photos are unaffected.** Files already in `wwwroot/dogs/` remain on disk and their URLs remain in the DB. `UseStaticFiles()` continues to serve them. No data loss.
- **No migration needed.** Schema unchanged.
- **Old `wwwroot/dogs/` directory not deleted.** Contains live photo files for existing records. Do not delete it.

## Code review checklist

- [ ] All three upload endpoints use the new path (`photos/dogs/{id}/`)
- [ ] Extension check is `.jpg` or `.png` only (no `.jpeg`, no `.webp`)
- [ ] Size check is `2 * 1024 * 1024`
- [ ] Primary photo cleanup targets `primary.*` inside the dog's subfolder, not a flat glob
- [ ] Gallery filenames are `{guid}.{ext}`, not `{dogId}_{guid}.{ext}`
- [ ] `UseStaticFiles()` in `Program.cs` unchanged — serves all of `wwwroot` including the new subfolder
- [ ] `features/feature-dogs.md` UC-D5 and UC-D6 match the new behavior
