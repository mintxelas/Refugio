# net10.0 solution upgrade — summary

## What changed

| File | Change |
|---|---|
| All 7 `.csproj` files | `TargetFramework` `net9.0` → `net10.0` |
| `Refugio.Application.csproj` | `Microsoft.Extensions.DependencyInjection.Abstractions` `9.*` → `10.*` |
| `Refugio.Infrastructure.csproj` | EF packages `9.*` → `10.*` |
| `Refugio.Web.csproj` | EF packages `9.*` → `10.*` |
| `Refugio.Tests.Unit.csproj` | EF + Extensions packages `9.*` → `10.*` |
| `Refugio.Tests.Integration.csproj` | `AspNetCore.Mvc.Testing`, EF packages `9.*` → `10.*` |
| `SettingsApiTests.cs` | `UpdateSettings_AsAnon_RedirectsToLogin` → `UpdateSettings_AsAnon_Returns401`; asserts `401` not `302` |

Akka.Hosting 1.5.68 and Akka.TestKit.Xunit2 1.5.68 unchanged (netstandard2.0-compatible).

## Result

`dotnet test`: **225 unit + 149 integration, 0 failures** on `net10.0`.

## Why the test changed

ASP.NET Core 10 changed cookie authentication: unauthenticated API requests (PUT/JSON) now
return `401 Unauthorized` rather than `302 Found → /login`. The old behavior was a
browser-oriented redirect that made no semantic sense for an API call. The fix updates the
assertion to match the new, correct behavior — the endpoint is still protected.

## Side effects / follow-ups

- **`OpenTelemetry.Api 1.10.0` vulnerability warnings (NU1902):** 16 warnings, pre-existing,
  pulled in transitively by Akka.Hosting 1.5.68. Upgrade Akka.Hosting when a newer version
  ships that bumps its OpenTelemetry dependency (GHSA-8785-wc3w-h8q6, GHSA-g94r-2vxg-569j,
  moderate severity).
- No `global.json` — SDK version is whatever is installed. Consider pinning with
  `global.json` if reproducible builds across machines matter.
- EF migrations snapshot is TFM-agnostic; no new migration needed.

## Code-review checklist

1. All 7 `.csproj` show `net10.0`; all `Microsoft.*` packages show `10.*`.
2. Akka packages unchanged at `1.5.68`.
3. `UpdateSettings_AsAnon_Returns401` asserts `401`; no production auth logic changed.
4. `dotnet ef migrations has-pending-model-changes` still reports no changes (entity namespace untouched).
