# Reasoning: Linux Packaging

## Steps

1. Read `Refugio.Web.csproj` — confirmed `net9.0` target, SQLite dependency, no existing publish config.
2. Read `Program.cs` — confirmed `Data Source=shelter.db` (relative path), port `5110`, no env-specific config.
3. Asked user: Docker vs self-contained vs framework-dependent. User chose self-contained.
4. Ran `dotnet publish -r linux-x64 --self-contained true -c Release` to produce a standalone binary.
5. Created `refugio.service` systemd unit file targeting `/opt/refugio/` with `www-data` user.
6. Zipped publish output to `publish/refugio-linux-x64.zip`.

## Decisions

**Self-contained over framework-dependent:** No .NET 9 runtime installation needed on server. Slightly larger artifact (~53 MB) but zero server-side dependency management.

**`linux-x64` RID:** Most common Linux server architecture. For ARM (e.g. Raspberry Pi, AWS Graviton), use `linux-arm64`.

**Port 5110:** Matches dev port from CLAUDE.md. Change `ASPNETCORE_URLS` env var to override.

**`www-data` user:** Standard low-privilege Linux web service user. Adjust to match server setup.

## Alternatives Rejected

- Docker: More portable but requires Docker daemon on server. User did not want this.
- Framework-dependent: Requires .NET 9 runtime pre-installed on server.
- Publish profile in csproj: Overkill for a one-off packaging step.
