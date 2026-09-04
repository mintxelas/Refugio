# Site backup ZIP - summary

## What changed
- **`src/Refugio.Web/Endpoints/SettingsEndpoints.cs`** - new `GET /api/settings/backup` endpoint (`Manager`-only). Builds an in-memory ZIP containing:
  - `shelter.db` - consistent snapshot via SQLite online-backup API.
  - All files under `wwwroot/photos/**` and `wwwroot/branding/**`, keyed by their wwwroot-relative path.
  - Returns it as `refugio-backup-{yyyyMMdd-HHmmss}.zip`.
- **`src/Refugio.Web/ClientApp/src/pages/Settings.tsx`** - Manager-only "Copia de seguridad" card with a `<a href="/api/settings/backup" download>` button.
- **`tests/Refugio.Tests.Integration/BackupApiTests.cs`** - new tests (Manager 200 + zip contents; anon redirect).

## Why
Managers asked for a self-service full backup (database + uploaded pictures) launchable from the configuration area.

## Side effects / follow-ups
- **No restore.** This only produces a backup. Restoring means stopping the app and swapping `shelter.db` + `wwwroot` manually. A restore/upload flow is not implemented.
- **ZIP is buffered in memory.** Fine at current data volume. If photo storage grows very large, switch to streaming to a temp file (noted in a `ponytail:` code comment).
- **DB connection string comes from `ShelterDbContext`**, so the endpoint follows whatever provider is configured (file DB in prod, in-memory in tests).

## Code-review checklist
- [ ] Endpoint is `.RequireAuthorization("Manager")` - verify a Volunteer cannot download.
- [ ] `db.Database.GetConnectionString()` used (not hardcoded `shelter.db`) so the snapshot targets the real DB.
- [ ] Temp snapshot connection uses `Pooling=False` and the temp file is deleted in a `finally`.
- [ ] `SqliteConnection.BackupDatabase` used for the snapshot (consistent under concurrent writes).
- [ ] ZIP buffered then returned via `Results.File` (no sync IO on `Response.Body`).
- [ ] Picture paths stored relative to `wwwroot` (so `photos/...` / `branding/...` structure is preserved on unzip).
- [ ] Frontend link is Manager-gated and uses a native `download` anchor.
