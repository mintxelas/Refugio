# Summary — Photo storage reorganization: finance (expenses) and volunteer

## What changed

| File | Change |
|---|---|
| `src/Refugio.Web/Endpoints/FinanceEndpoints.cs` | Both expense photo upload endpoints updated |
| `src/Refugio.Web/Endpoints/VolunteerEndpoints.cs` | Photo upload and photo delete endpoints updated |
| `features/feature-finance.md` | `ExpensePhoto.Url` and UC-F5 updated |
| `features/feature-volunteers.md` | UC-V5 updated |

### `FinanceEndpoints.cs` — expense photo uploads

Both `POST /api/expenses/{id}/photos` (SSR form) and `POST /api/expenses/{id}/photos/upload` (JSON):
- Directory: `wwwroot/expenses/` → `wwwroot/photos/finance/expenses/{id}/`
- File name: `{expenseId}_{guid}.{ext}` → `{guid}.{ext}`
- URL stored: `/expenses/{file}` → `/photos/finance/expenses/{id}/{file}`
- Extensions: `.jpg/.png` only (was `.jpg/.jpeg/.png/.webp`)
- Max size: 2 MB (was 5 MB)

### `VolunteerEndpoints.cs` — profile photo

`POST /api/volunteers/{id}/photo`:
- Directory: `wwwroot/volunteers/` → `wwwroot/photos/volunteer/{id}/`
- File name: `{id}.{ext}` → `primary.{ext}`
- Cleanup glob: `{id}.*` → `primary.*` scoped to the volunteer's subfolder
- URL stored: `/volunteers/{file}` → `/photos/volunteer/{id}/primary.{ext}`
- Extensions: `.jpg/.png` only (was `.jpg/.jpeg/.png/.webp`)
- Max size: 2 MB (was 5 MB)

`POST /api/volunteers/{id}/photo/delete`:
- Delete scoped to `wwwroot/photos/volunteer/{id}/primary.*` (was `{id}.*` in flat dir)

## Adoption — out of scope

Adoption has no photo entity, no `PhotoUrl` property on the `Adoption` aggregate, and no upload endpoint. There is nothing to migrate. If applicant document photos are required in the future, that is a new feature (entity + migration + service + endpoint + UI).

## Side effects

- Existing files in `wwwroot/expenses/` and `wwwroot/volunteers/` remain; their URLs in the DB still resolve via `UseStaticFiles()`. No data loss.
- No schema migration required.

## Code review checklist

- [ ] Both expense upload endpoints use `photos/finance/expenses/{id}/`
- [ ] Expense file names are `{guid}.{ext}`, not `{expenseId}_{guid}.{ext}`
- [ ] Volunteer upload uses `photos/volunteer/{id}/primary.{ext}`
- [ ] Volunteer delete cleans `primary.*` inside the volunteer subfolder
- [ ] All extensions: `.jpg` or `.png` only
- [ ] All size limits: `2 * 1024 * 1024`
- [ ] Feature docs match the new behavior
