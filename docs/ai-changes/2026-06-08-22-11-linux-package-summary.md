# Summary: Linux Packaging

## What Changed

- Created `publish/refugio-linux-x64.zip` (53 MB) — self-contained `linux-x64` binary of the full app.
- Created `publish/refugio/refugio.service` — systemd unit file included in the zip.

## Why

User needs to deploy the app on a Linux server with no .NET runtime dependency.

## Side Effects / Follow-up

- **SQLite db path:** `shelter.db` is created relative to working directory (`/opt/refugio/`). Ensure `www-data` (or your service user) has write permission there.
- **HTTPS:** App runs HTTP on port 5110. Put nginx or Caddy in front for TLS termination.
- **ARM servers:** Re-run publish with `-r linux-arm64` for AWS Graviton, Raspberry Pi, etc.
- **Connection string:** Hardcoded in `Program.cs`. Override via `ConnectionStrings__ShelterDb` env var (ASP.NET Core config binding) if path needs to change — but this requires adding `appsettings.json` config key first.

## Review Checklist

- [ ] Verify `Refugio.Web` binary is executable after unzip
- [ ] Confirm `www-data` can write to `/opt/refugio/`
- [ ] Reverse proxy configured for HTTPS before exposing publicly
- [ ] `shelter.db` backup strategy in place (SQLite is a single file)
