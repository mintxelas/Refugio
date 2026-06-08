# Prompt Feedback: Linux Packaging

## Original Prompt

> package the application so I can deploy it in a linux server.

## What Worked

Short and clear intent. Enough to act on.

## What Was Missing / Ambiguous

1. **Target architecture** — assumed `linux-x64`. Should specify `linux-x64` vs `linux-arm64` (AWS Graviton, Raspberry Pi).
2. **Deployment method** — Docker, self-contained binary, or framework-dependent. Required clarifying question.
3. **Service user** — used `www-data` as default. Server may use a different user.
4. **Port / reverse proxy** — unclear if app runs behind nginx/Caddy or directly on 80/443.
5. **Data directory** — SQLite path `shelter.db` is relative. A production deploy often wants it in `/var/lib/refugio/` or similar.

## Improved Prompt Example

> Package the app for deployment on a linux-x64 VPS. Use a self-contained binary (no Docker, no .NET runtime on server). The service should run as user `deploy`, listen on port 8080, and store shelter.db in /var/lib/refugio/. Include a systemd service file.
