# Reasoning — Photo storage reorganization: finance (expenses) and volunteer

## Goal

Apply the same per-entity subfolder convention introduced for dog photos to expense receipt photos and volunteer profile photos. Path: `photos/finance/expenses/{id}/` and `photos/volunteer/{id}/`. Extensions: `.jpg/.png` only. Max: 2 MB.

## Scope finding

Reviewed all three candidate areas:

- **Volunteer** — one endpoint: `POST /api/volunteers/{id}/photo` (profile photo). One delete endpoint. Single-replacement pattern like dog primary photo.
- **Finance/Expenses** — two upload endpoints: `POST /api/expenses/{id}/photos` (SSR form) and `POST /api/expenses/{id}/photos/upload` (JSON, React SPA). Gallery pattern like dog gallery.
- **Adoption** — **no photo entity or endpoint exists**. `Adoption` has no `PhotoUrl`, `AdoptionPhoto` entity, or photo upload endpoint. Nothing to migrate. See "Adoption" section below.

## Changes per area

### Finance — Expenses
- Directory: `wwwroot/expenses/` → `wwwroot/photos/finance/expenses/{id}/`
- File name: `{expenseId}_{guid}.{ext}` → `{guid}.{ext}` (folder makes ID prefix redundant)
- URL stored: `/expenses/{file}` → `/photos/finance/expenses/{id}/{file}`
- Extensions: `.jpg/.jpeg/.png/.webp` → `.jpg/.png`
- Size: 5 MB → 2 MB
- Both upload endpoints (SSR form + JSON) updated identically.

### Volunteer
- Directory: `wwwroot/volunteers/` → `wwwroot/photos/volunteer/{id}/`
- File name: `{id}.{ext}` → `primary.{ext}`
- Delete cleanup glob: `{id}.*` in flat dir → `primary.*` in dog's subfolder
- URL stored: `/volunteers/{file}` → `/photos/volunteer/{id}/primary.{ext}`
- Same for the delete endpoint which also cleans the file from disk.

## Adoption — no action

`Adoption` is a pure-data aggregate (applicant name, email, phone, type, status, notes). No photo concept exists in the domain, service, or endpoint layer. If adoption photos are needed in the future (e.g., applicant ID documents), they would require a new `AdoptionDocument` child entity + migration + service methods + endpoints + UI.

## No migration needed

`ExpensePhoto.Url` and `Volunteer.PhotoUrl` are string columns; storing a different path requires no schema change. Existing files in `wwwroot/expenses/` and `wwwroot/volunteers/` remain on disk and continue to be served by `UseStaticFiles()`.
