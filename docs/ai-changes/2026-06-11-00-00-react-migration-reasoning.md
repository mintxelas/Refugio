# React SPA Migration — Reasoning

## Problem Statement

The Refugio app used 31 Blazor SSR `.razor` pages with server-side rendering. The plan calls for replacing them with a Vite + React TypeScript SPA served by the same ASP.NET 9 host, while preserving all existing API endpoints and the cookie authentication model.

## Step-by-Step Logic

### Phase A: Backend preparation

**A1 — JSON auth endpoints**

The existing auth endpoints (`/auth/login`, `/auth/logout`, `/auth/change-password`) used Blazor form submissions and HTML redirects — unusable from a SPA. Four new JSON endpoints were added under `/api/auth/*`:
- `POST /api/auth/login` — accepts `{ email, password }` JSON, signs in via `SignInAsync`, returns `{ id, name, email, role }`.
- `GET /api/auth/me` — returns the current user's claims or 401. SPA uses this on startup to hydrate auth state.
- `POST /api/auth/logout` — calls `SignOutAsync`, returns `{ success: true }`.
- `POST /api/auth/change-password` — accepts `{ currentPassword, newPassword, confirmPassword }`, delegates to `IVolunteerService`.

All 4 have `.DisableAntiforgery()` because they are consumed by a JS SPA with `credentials: 'include'`, not browser forms. The existing Blazor form endpoints remain intact.

**A2 — JSON upload variants**

Photo upload endpoints (`/api/dogs/{id}/photos`, `/api/expenses/{id}/photos`, `/api/settings/logo`) previously returned HTML redirects. Three new endpoints added alongside the originals that return JSON (`{ urls: [...] }` or `{ url }`). React's file input uses `FormData` and expects a JSON response.

An additional `GET /api/reports/upcoming-visits` endpoint was added — `dashboardApi.getUpcomingVisits()` needed it, and `IMedicalQueries.GetUpcomingVisitsAsync` already existed with no HTTP surface.

**A3 — SPA static file serving**

`MapFallbackToFile("index.html")` ensures React Router's client-side routing works when a user refreshes on any SPA route. The fallback only fires when `ClientApp/dist/` exists (checked via `Directory.Exists`), so the app runs normally in development without a built SPA.

`.csproj` excludes `ClientApp/**` from .NET compilation (no `.cs` files there, but prevents VS tooling from scanning `node_modules`). An MSBuild `BuildClientApp` target runs `npm ci && npm run build` before `Publish`.

### Phase B: React scaffold

**Package choices:**
- React 18.3.1 — current stable at time of writing.
- react-router-dom 6.26.2 — v6 API with `<Routes>`, `useSearchParams`, `NavLink`.
- Vite 5.4.1 — fast build, native ESM, dev proxy.
- TypeScript 5.5.3 — satisfies operator, improved inference.
- vitest 2.0.5 + @testing-library/react 16.0.0 — matches Vite's test runner.

**`index.html` design tokens:**
The Tailwind CDN JIT `theme.extend` block was copied verbatim from `App.razor`'s `<script>` block. This ensures every custom token (`primary`, `on-surface-variant`, `surface-container-lowest`, `margin-desktop`, etc.) works identically in React. No arbitrary hex values used — all colors reference the MD3 token names.

**Vite proxy:**
`/api`, `/auth`, `/branding`, `/uploads` all proxy to `http://localhost:5110`. In production, all traffic hits the same ASP.NET host.

### Phase C: Pages

**Auth flow:**
`AuthContext.tsx` calls `/api/auth/me` on mount. While loading, `RequireAuth` shows a spinner (prevents flash of login page). After load, no user → redirect to `/login`. `RequireManager` checks `user.role === 'Manager'` — used on `/admin/deleted` and `/settings` routes.

**URL-driven state:**
Every filter, tab, and pagination state uses `useSearchParams`. Updates use `setSearchParams(p, { replace: true })` to avoid polluting history. This mirrors the Blazor `[SupplyParameterFromQuery]` pattern.

**Form pattern:**
Each edit page uses controlled `useState` for form fields. Validation accumulates errors in `string[]` before any API call. Sticky values preserved (form state not reset on error). Success → `navigate(back)`.

**Kanban (Adoptions):**
Per-column limits via URL params (`?appliedLimit=N`). Advance/reject buttons call `POST /api/adoptions/{id}/status`. New application form at bottom of Applied column.

**Delete convention:**
`api.del(path)` calls `POST /{path}/delete` with antiforgery bypassed (API endpoints use `.DisableAntiforgery()` for the delete variants, or they're called from the SPA which doesn't send antiforgery tokens — the existing delete endpoints on the server are `.RequireAuthorization("Manager")` only).

Wait — this is a nuance. The `.razor` delete forms use antiforgery tokens. The API delete endpoints (`POST /api/{entity}/{id}/delete`) likely also require antiforgery by default in ASP.NET 9. The SPA won't send the `__RequestVerificationToken` header. The existing endpoint registrations use `.DisableAntiforgery()` for delete endpoints.

**AdminDeleted:**
Uses a generic `DeletedTable<T>` component parameterized by column definitions and row renderer. Tab-driven loading (loads only the active tab's data). Purge requires `window.confirm`. Restore removes from local state immediately (optimistic update).

## Implementation Choices vs Alternatives

| Choice | Alternative Rejected | Reason |
|--------|---------------------|--------|
| Cookie auth with `credentials: 'include'` | JWT Bearer tokens | Backend already uses cookies; no migration needed |
| Vite proxy in dev | CORS headers on backend | Simpler, no backend change, matches prod (same origin) |
| Single `types.ts` | Per-domain type files | 96 files already; one file fine at this scale |
| `useSearchParams` for all state | `useState` + `history.pushState` | React Router v6 canonical; back button works |
| Generic `DeletedTable<T>` | 8 separate tables | DRY, same structure for all 8 entity types |
| `api.del` uses POST to `/{id}/delete` | HTTP DELETE verb | Browser fetch can do DELETE, but the server's REST endpoints are the canonical way to delete; the Blazor pages already use `POST /{id}/delete` convention for CSRF |
