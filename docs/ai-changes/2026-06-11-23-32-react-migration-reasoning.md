# React Migration — Reasoning

## Problem

Blazor SSR UI (31 .razor files) needed full conversion to Vite + React SPA while keeping the backend REST API and visual design intact.

## Step-by-step decisions

### 1. Architecture: SPA inside Web project

React SPA placed in `src/Refugio.Web/ClientApp/`. ASP.NET serves `ClientApp/dist/` as static files and falls back to `index.html` for all non-API routes. Avoids CORS, keeps cookie auth on same origin.

### 2. Auth: JSON endpoints alongside form endpoints

Existing form-POST auth endpoints (`/auth/login` → redirect) kept for Blazor back-compat. New JSON endpoints added under `/api/auth/*`:
- `POST /api/auth/login` → `{id, name, email, role}` or 401
- `GET /api/auth/me` → current user or 401
- `POST /api/auth/logout` → 200
- `POST /api/auth/change-password` → `{success}` or 400

React `AuthContext` calls `/api/auth/me` on startup to rehydrate session from existing cookie.

### 3. delete/restore/purge API calls — critical fix

The Blazor `POST /{id}/delete` and `POST /{id}/restore` endpoints return `Results.Redirect(...)`. The initial implementation called these from the SPA via `api.del(url)` which used `POST` and `redirect: 'follow'`. This would:
1. Follow the redirect to a React route URL
2. Get back `index.html` (200 OK)
3. Try to parse HTML as JSON → runtime error

**Fix applied:**
- `api.del()` changed to use `DELETE` HTTP method, pointing to proper REST endpoints (`DELETE /api/dogs/{id}` etc.) that return 204
- `postAction(url)` added to `client.ts` — uses `redirect: 'manual'` so fetch returns an opaque redirect response (type='opaqueredirect', status=0) instead of following to HTML. Treats opaque redirect or 2xx as success
- restore/purge operations use `postAction` since no REST equivalents exist
- Dog photo set-default and delete also use `postAction` (endpoints return redirect)

### 4. Dog photo upload JSON endpoint

Existing `POST /dogs/{id}/photos` returns `Results.Redirect(...)`. Added parallel `POST /dogs/{id}/photos/upload` endpoint that returns `{urls:[...]}` — same pattern already used for expense photos.

### 5. i18n dropped

Localization (4 RESX files, culture cookie) dropped for the React SPA — English only. The `Volunteer.PreferredLanguage` field is preserved in the domain and can be leveraged later. Simplifies the SPA significantly.

### 6. SPA fallback guard

`MapFallbackToFile` only registered when `ClientApp/dist/` directory exists. App boots normally without a built SPA (Blazor SSR still serves). This allows `dotnet run` to work before `npm run build`.

## Alternatives rejected

- **Next.js**: SSR complexity without benefit (API already handles data, no SEO requirement)
- **Separate project**: CORS + separate deploy complexity; same-origin cookie auth works cleanly with co-located SPA
- **In-process shortcut**: Already rejected at the DDD rewrite; API is the contract
