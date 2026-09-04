# Site backup ZIP - reasoning

## Problem
Managers need a one-click backup from the Settings area: a downloadable ZIP containing the SQLite database and all uploaded pictures.

## What was backed up
- **Database:** `shelter.db` (single SQLite file, connection string owned by `ShelterDbContext`).
- **Pictures:** everything under `wwwroot/photos/**` (dogs, adoptions, finance, volunteers) and `wwwroot/branding/**` (logo).

## Steps
1. Located storage: `Program.cs` (`Data Source=shelter.db`), photo endpoints (`wwwroot/photos/{area}/...`), logo (`wwwroot/branding`).
2. Added `GET /api/settings/backup`, `Manager`-only, in `SettingsEndpoints.cs`.
3. Streamed a ZIP built with `System.IO.Compression.ZipArchive` (in-box, no new dependency).
4. Frontend: added a Manager-only section in `Settings.tsx` with a plain `<a download>` link. No API client, no blob handling.

## Key decisions
- **SQLite online-backup API, not `File.Copy`.** The live DB may be mid-write; `SqliteConnection.BackupDatabase` produces a consistent snapshot. Connection string is read from `ShelterDbContext` (`db.Database.GetConnectionString()`) so tests hit the in-memory shared-cache DB, not a hardcoded file.
- **Buffer the ZIP in memory, then `Results.File`.** `ZipArchive` writes synchronously; Kestrel forbids sync IO on the response body (`InvalidOperationException: Synchronous operations are disallowed`). Buffering to a `MemoryStream` then returning `Results.File` copies async. Acceptable for a shelter's data volume; the upgrade path (stream to a temp file) is noted in a `ponytail:` comment.
- **`Pooling=False` on the temp destination connection.** Microsoft.Data.Sqlite pools connections and keeps the file handle open after dispose, so `File.Delete` on the temp snapshot threw `IOException: being used by another process`. Disabling pooling releases the handle immediately.

## Alternatives rejected
- **`File.Copy` of `shelter.db`** - risks an inconsistent snapshot under concurrent writes.
- **Stream ZIP straight to `Response.Body`** - fails on Kestrel's sync-IO guard.
- **New zip library / archiver dependency** - `System.IO.Compression` already covers it.
- **Restore/upload endpoint** - out of scope; backup is not restore. Add when asked.

## Tests
`BackupApiTests`: Manager GET returns 200 + `application/zip` + a non-empty `shelter.db` ZIP entry; anonymous GET redirects to login. Both green.
