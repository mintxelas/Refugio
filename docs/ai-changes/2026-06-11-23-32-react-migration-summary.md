# React Migration — Summary

## What changed

### New: `src/Refugio.Web/ClientApp/` (Vite + React SPA)

| File | Purpose |
|---|---|
| `index.html` | Shell — Tailwind CDN (same config as App.razor), Material Symbols, Google Fonts |
| `vite.config.ts` | Dev proxy `/api` + `/auth` → `localhost:5110` |
| `src/types.ts` | TypeScript types mirroring all DTOs |
| `src/api/client.ts` | Base fetch wrapper; `api.{get,post,put,del}` + `postAction` + `upload` |
| `src/api/{auth,dogs,adoptions,tasks,finance,volunteers,settings,dashboard,events}.ts` | Per-aggregate API modules |
| `src/auth/AuthContext.tsx` | `AuthProvider`, `useAuth`, `RequireAuth`, `RequireManager` |
| `src/components/{Layout,Sidebar,TopBar,Pagination,LoadingSpinner,ErrorMessage,StatusChip}.tsx` | Shared UI |
| `src/pages/*.tsx` | 25 pages — all routes from the Blazor app |

### Modified: `src/Refugio.Web/Endpoints/AuthEndpoints.cs`

- Added `MapApiAuthEndpoints()` extension with 4 JSON auth endpoints under `/api/auth/*`
- Existing form-based auth endpoints unchanged (Blazor back-compat)

### Modified: `src/Refugio.Web/Endpoints/DogEndpoints.cs`

- Added `POST /api/dogs/{id}/photos/upload` — JSON response variant for SPA photo uploads

### Modified: `src/Refugio.Web/Program.cs`

- Added `api.MapApiAuthEndpoints()` call
- Added SPA static files serving from `ClientApp/dist/` (guarded by directory existence check)
- Added `MapFallbackToFile("index.html")` for SPA routing

## Why

Replace Blazor SSR (pure server-side, no interactivity) with a standard React SPA that can use the existing REST API. The API was already the single contract; this makes the React UI a first-class consumer alongside any future external integrations.

## Side effects / follow-up

1. **`npm install` required**: Run `cd src/Refugio.Web/ClientApp && npm install` before building
2. **Dev workflow**: Run `dotnet run` (port 5110) + `npm run dev` (port 5173 with proxy). Or build once with `npm run build` and run only dotnet
3. **Blazor removal (deferred)**: Delete `Components/Pages/*.razor`, `Components/Layout/*.razor`, remove `MapRazorComponents<App>()` and `AddRazorComponents()` from `Program.cs` once the SPA is validated
4. **Integration tests**: Tests that assert SSR HTML will need rewriting to assert JSON API responses
5. **i18n**: Dropped; English only. Can add `react-i18next` later using the existing RESX data
6. **Volunteer photo upload**: `POST /volunteers/{id}/photo` returns a redirect — the SPA currently cannot upload volunteer photos; needs a JSON upload endpoint (same pattern as dogs/expenses)

## Code review checklist

- [ ] `postAction` correctly handles opaque redirects in all browsers
- [ ] All 25 pages render and navigate correctly after `npm run build`
- [ ] Auth flow: login → redirect to `/`, `/login` blocks unauthenticated, `/admin/deleted` blocks non-Manager
- [ ] Cookie is forwarded on all `fetch` calls (`credentials: 'include'` set on every request)
- [ ] REST DELETE endpoints called for entity deletes (not POST redirect endpoints)
- [ ] Dog photo upload uses `/dogs/{id}/photos/upload` (new JSON endpoint)
- [ ] SPA fallback does not intercept `/api/*` or `/auth/*` routes
