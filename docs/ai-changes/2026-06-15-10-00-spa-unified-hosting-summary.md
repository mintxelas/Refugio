# Summary: Unified SPA + API hosting

## What changed

### `Refugio.Web.csproj`
- Added `Microsoft.AspNetCore.SpaProxy` 10.0.9 package.
- Added three MSBuild properties:
  - `SpaRoot`: `ClientApp\` — tells the package where the SPA lives.
  - `SpaProxyServerUrl`: `http://localhost:5173` — Vite dev server URL.
  - `SpaProxyLaunchCommand`: `npm run dev` — command to start Vite.

### `Program.cs`
- Wrapped the `ClientApp/dist` static file serving in `if (!app.Environment.IsDevelopment())`.

## Why

Single `dotnet run` now serves both the API and the React frontend. In dev, SpaProxy auto-starts Vite and proxies SPA requests through port 5110. In production, the existing `ClientApp/dist` static file serving handles it. No CORS config needed — everything is same-origin.

## Side effects

- Accessing `http://localhost:5110` in dev now shows the React app (after Vite boots, ~2–3 s).
- Port 5173 still works if you start Vite manually (Vite's own dev server).
- HMR still works fully — SpaProxy transparently forwards WebSocket upgrade requests to Vite.
- `npm run dev` no longer needs to be started separately.

## Code review checklist

- [ ] `SpaProxyServerUrl` matches `vite.config.ts` `server.port` (both 5173).
- [ ] `SpaProxyLaunchCommand` runs successfully from `ClientApp\` directory.
- [ ] Production publish: `BuildClientApp` target still fires `BeforeTargets="Publish"` and `dist/` lands in output.
- [ ] `!app.Environment.IsDevelopment()` guard prevents stale `dist/` from shadowing live Vite output in dev.
