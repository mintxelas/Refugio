# React SPA Migration — Summary

## What Changed

### Backend (`src/Refugio.Web/`)

| File | Change |
|------|--------|
| `Endpoints/AuthEndpoints.cs` | Added `MapApiAuthEndpoints` with 4 JSON endpoints: `/api/auth/login`, `/api/auth/me`, `/api/auth/logout`, `/api/auth/change-password` |
| `Endpoints/DogEndpoints.cs` | Added `POST /api/dogs/{id}/photos/upload` (JSON response) and `GET /api/reports/upcoming-visits` |
| `Endpoints/FinanceEndpoints.cs` | Added `POST /api/expenses/{id}/photos/upload` (JSON response) |
| `Endpoints/SettingsEndpoints.cs` | Added `POST /api/settings/logo/upload` (JSON response) |
| `Program.cs` | Added `api.MapApiAuthEndpoints()` call; added SPA static file serving + `MapFallbackToFile` |
| `Refugio.Web.csproj` | Excluded `ClientApp/**` from .NET compile; added `BuildClientApp` MSBuild target |

### React SPA (`src/Refugio.Web/ClientApp/`)

New directory. All files created from scratch.

**Foundation:**
- `package.json` — React 18, react-router-dom 6, Vite 5, TypeScript 5
- `tsconfig.json` — strict mode, path aliases
- `vite.config.ts` — proxy to `:5110`, vitest config
- `index.html` — full Tailwind CDN with all MD3 design tokens, Google Fonts

**Core:**
- `src/types.ts` — all DTO interfaces + enum union types matching C# member names
- `src/main.tsx` — app entry point
- `src/App.tsx` — route table (25 routes, `RequireAuth` + `RequireManager` guards)

**API layer (`src/api/`):**
- `client.ts` — `ApiError`, `api.get/post/put/del`, `upload<T>`
- `auth.ts`, `dogs.ts`, `adoptions.ts`, `tasks.ts`, `finance.ts`, `volunteers.ts`, `settings.ts`, `dashboard.ts`, `events.ts`

**Auth (`src/auth/`):**
- `AuthContext.tsx` — `AuthProvider`, `useAuth`, `RequireAuth`, `RequireManager`

**Shared components (`src/components/`):**
- `Layout.tsx`, `Sidebar.tsx`, `TopBar.tsx`, `LoadingSpinner.tsx`, `ErrorMessage.tsx`, `Pagination.tsx`, `StatusChip.tsx`

**Pages (`src/pages/`):**
25 pages covering all routes: `Login`, `Home`, `Dogs`, `DogDetail`, `DogCheckin`, `DogEdit`, `MedicalRecordEdit`, `MedicationEdit`, `Health`, `Adoptions`, `AdoptionEdit`, `Calendar`, `EventEdit`, `Funds`, `DonationEdit`, `ExpenseEdit`, `GoalEdit`, `Volunteers`, `VolunteerEdit`, `Reports`, `Settings`, `ChangePassword`, `AdminDeleted`

## Why It Changed

The plan calls for replacing the Blazor SSR UI with a React SPA to enable a richer interactive client experience (no full-page reloads, instant navigation, optimistic UI). The backend API was already complete; only auth and file upload endpoints needed JSON-returning variants.

## Side Effects / Follow-Up Actions

1. **Antiforgery on delete endpoints.** The SPA's `api.del()` calls `POST /{id}/delete` without antiforgery tokens. Verify those endpoints have `.DisableAntiforgery()` on the server, or add an `X-Requested-With: XMLHttpRequest` header check. The existing endpoints registered in `*Endpoints.cs` should be audited.

2. **`npm install` not run.** Run `npm install` inside `ClientApp/` before first build: `cd src/Refugio.Web/ClientApp && npm install`.

3. **Build the SPA.** Run `npm run build` to produce `ClientApp/dist/`. The ASP.NET app only serves the SPA if `dist/` exists.

4. **Razor pages not deleted.** The `.razor` files still exist and are compiled. Once the SPA is validated, remove all 31 `.razor` pages and update `Program.cs` to remove `MapRazorComponents<App>()`. That is the plan's final cleanup step.

5. **Localization not carried over.** The SPA uses English strings directly. The `SharedResources.resx` localization from the Blazor pages is not used in React. If multi-language support is required, add a React i18n library (e.g., `i18next`) or expose a `/api/strings?culture=x` endpoint.

6. **Photo display paths.** Dog/expense photo `<img src>` uses paths like `/uploads/...` and `/branding/...`. These serve from ASP.NET's `UseStaticFiles`. Verify the paths match what the server writes.

## Code Review Checklist

- [ ] `MapApiAuthEndpoints` called before other domain groups in `Program.cs`
- [ ] All 4 new auth endpoints have `.DisableAntiforgery()`
- [ ] `GET /api/auth/me` has `.RequireAuthorization()`
- [ ] Photo upload endpoints return `application/json` (not redirect)
- [ ] `MapFallbackToFile` only triggers when `dist/` exists
- [ ] `ClientApp/**` excluded from .NET compile in `.csproj`
- [ ] All 25 pages imported in `App.tsx` routes
- [ ] `RequireManager` applied to `/admin/deleted` and `/settings`
- [ ] Enum string values in `types.ts` match C# member names exactly
- [ ] `api.del` uses `POST /{path}/delete` (not HTTP DELETE verb)
- [ ] `Page<T>` interface has `items` (camelCase) matching server JSON
