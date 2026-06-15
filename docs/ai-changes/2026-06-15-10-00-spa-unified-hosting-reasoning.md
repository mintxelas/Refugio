# Reasoning: Unified SPA + API hosting

## Problem

Dev workflow required two servers:
1. `dotnet run` on port 5110 (API)
2. `npm run dev` on port 5173 (Vite SPA)

Production publish already worked — Program.cs served `ClientApp/dist` from the same .NET host. But dev always needed the Vite server running separately.

## Root cause

No mechanism to auto-start Vite from `dotnet run` or proxy SPA requests through the .NET server in dev.

## Solution chosen: `Microsoft.AspNetCore.SpaProxy`

ASP.NET Core provides `Microsoft.AspNetCore.SpaProxy` (10.0.9) for exactly this. It:
1. Reads `SpaProxyLaunchCommand` and `SpaProxyServerUrl` from the csproj `<PropertyGroup>`.
2. When `ASPNETCORE_ENVIRONMENT=Development`, automatically starts Vite as a child process and waits until it's ready.
3. Adds middleware that forwards non-API requests to Vite's port (5173), passing through HMR WebSocket traffic.
4. Is a no-op in non-Development environments, so production behaviour is unchanged.

## Alternatives rejected

- **Manual `UseProxyToSpaDevelopmentServer`** (`Microsoft.AspNetCore.SpaServices.Extensions`): older, deprecated, requires more wiring.
- **Run two servers manually**: the status quo. Inconvenient and requires CORS if the React app makes direct API calls without a proxy.
- **Always build dist/ before running**: kills HMR, slows the dev loop.

## Files changed

| File | Change |
|---|---|
| `Refugio.Web.csproj` | Added `SpaRoot`, `SpaProxyServerUrl`, `SpaProxyLaunchCommand` properties; `Microsoft.AspNetCore.SpaProxy` 10.0.9 package |
| `Program.cs` | Wrapped SPA static-file serving in `!IsDevelopment()` guard — SpaProxy handles dev, existing dist-serving handles prod |

## Why the Program.cs guard matters

Without the `!IsDevelopment()` guard, if a developer had previously built `ClientApp/dist/`, the static files from that stale build would shadow the live Vite output. Guarding on environment ensures the right behaviour in both modes.
