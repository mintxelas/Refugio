# Refugio UI Migration — Blazor SSR → Vite + React SPA

> **Architecture note.** This plan is a **frontend replacement**, not a domain change. The backend
> is the DDD stack (Application services + Infrastructure repos/UoW + minimal-API endpoints). The
> former Akka.NET actor layer was already removed in June 2026, so there is **no actor model to
> design here** — all business logic stays exactly where it is. The React SPA becomes a *second*
> consumer of the same `/api/*` REST surface that `ShelterApiClient` consumes today. The one-way
> dependency flow (Domain → Infrastructure → Application → Web) is untouched; every change in this
> plan lands in `Refugio.Web` (composition root) or the new `ClientApp/` subtree.

---

## 1. Feature Summary

Replace the 25 Blazor SSR pages with a Vite + React + TypeScript single-page application served by
the same ASP.NET host. The SPA lives in `src/Refugio.Web/ClientApp/`, builds to `ClientApp/dist/`,
and is served as static files with an `index.html` fallback for all non-`/api` routes. It talks to
the existing REST API over same-origin `fetch` with cookie auth (`credentials: 'include'`). A small
set of **new JSON auth endpoints** is added so the SPA can log in, identify the current user, log
out, and change password without HTML redirects. The audience is unchanged: shelter staff
(`Manager`) and volunteers (`Volunteer`), with Manager-only areas (admin/deleted, settings, all
destructive actions) gated in both the UI and the API. Localization (i18n) is **dropped for now**
(English-only) per the migration decision; the four RESX cultures and the `/set-language` switcher
are simply not reimplemented in React.

---

## 2. User Flow

All routes are **client-side** (React Router). The browser loads `index.html` once; navigation is
in-app. Auth is enforced two ways: (a) a client-side route guard that redirects to `/login` when
`/api/auth/me` returns 401, and (b) the API itself, which still returns 401/403 on every protected
call regardless of the client.

1. **Unauthenticated load** — user hits any URL (e.g. `/dogs`). `index.html` loads, `AuthProvider`
   calls `GET /api/auth/me`. It returns **401** → context user is `null` → the route guard renders a
   redirect to `/login`.
2. **Login** (`/login`, blank layout, no sidebar) — controlled form (email + password). Submit →
   `POST /api/auth/login` JSON. On **200** the server issues the auth **cookie** (same
   `CookieAuthenticationDefaults` scheme) and returns `{ id, name, email, role }`; the client stores
   it in context and navigates to `/`. On **401** the form shows an inline error. No redirect from the
   server — the SPA controls navigation.
3. **Authenticated app** — `Layout` (sidebar + topbar) wraps every page. Sidebar links mirror the
   current `MainLayout`: Dashboard, Dogs, Health, Adoptions, Funds, Calendar, Volunteers, Reports, and
   — only when `user.role === 'Manager'` — Admin/Deleted and Settings. The topbar shows the shelter
   name/logo (from `GET /api/settings`), the user's initials/name, a "Change password" link, and
   "Sign out".
4. **Reads** — each page calls its `GET /api/*` endpoint(s) on mount, shows `LoadingSpinner` while
   pending and `ErrorMessage` on failure. Filters / tabs / pagination are driven by **URL query
   params** via `useSearchParams` (e.g. `/dogs?search=rex&status=Available&page=2`), preserving
   deep-linkability and browser back/forward — the same UX the SSR query-param pages had, now without
   full round-trips.
5. **Mutations (create / edit)** — controlled forms POST/PUT JSON to the API, then `navigate(...)`
   back to the list on success (client-side, no server redirect). Validation is **client-side** for
   UX *and* still enforced server-side by the services (the SPA must surface 400 responses).
6. **Destructive actions (delete / purge / restore)** — a confirm dialog (`window.confirm` or a small
   modal) then `POST /api/{entity}/{id}/delete` (or `/restore`, `/purge`). These endpoints already
   `RequireAuthorization("Manager")`; the UI additionally hides the controls when
   `user.role !== 'Manager'`. A 403 from the API is the backstop.
7. **File uploads (dog photos, expense receipts, shelter logo)** — `multipart/form-data` POST via
   `FormData` (no JSON). These endpoints currently respond with `Results.Redirect(...)`; see §3/§7 for
   the JSON-returning variants the SPA needs.
8. **CSV export** (`/funds`, `/adoptions`) — a plain `<a href="/api/export/donations">` style link
   (or `window.location`) hitting the existing `GET /api/export/*` endpoints; the browser downloads
   the file. Cookies ride along automatically (same origin).
9. **Logout** — `POST /api/auth/logout` clears the cookie and returns 200; client clears context and
   navigates to `/login`.
10. **Change password** (`/change-password`) — form → `POST /api/auth/change-password` JSON; shows
    inline success/error from the JSON response (no `?success=1` redirect).

---

## 3. Architecture & Layer Placement

Every change is in `Refugio.Web` or the new `ClientApp/` subtree. **No** change to Domain,
Application, or Infrastructure projects — the SPA reuses the existing services through the existing
endpoints. The DDD dependency flow is therefore inherently preserved.

| New / changed item | Layer / location | Rationale |
|---|---|---|
| `POST /api/auth/login` (JSON) | `Refugio.Web/Endpoints/AuthEndpoints.cs` | New JSON consumer of `IVolunteerService.LoginAsync`; signs the same cookie. Mirrors existing form login. |
| `GET /api/auth/me` (JSON) | same | Reads `ClaimsPrincipal`; no service needed. New surface for SPA bootstrap. |
| `POST /api/auth/logout` (JSON) | same | JSON variant of existing `GET /auth/logout`. |
| `POST /api/auth/change-password` (JSON) | same | JSON variant of existing form endpoint; reuses `IVolunteerService.ChangePasswordAsync`. |
| JSON variants of upload endpoints (dog photo, expense receipt, logo) | `DogEndpoints.cs`, `FinanceEndpoints.cs`, `SettingsEndpoints.cs` | Existing ones `Redirect`; SPA needs `200 + JSON` (new URL) or `400`. Add new routes; keep old for back-compat. |
| `MapFallbackToFile` + `UseStaticFiles` for `ClientApp/dist` | `Refugio.Web/Program.cs` | Serve the SPA and fall back to `index.html` for client routes. |
| `ClientApp/**` (Vite React app) | `src/Refugio.Web/ClientApp/` | The new UI. Not a .NET project; excluded from `dotnet build` via `.csproj` glob exclusion. |
| `.csproj` MSBuild target to `npm run build` on Publish | `Refugio.Web.csproj` | Bundle the SPA into the published output. |
| Delete (eventually) `Components/Pages/*.razor`, `Layout/*.razor`, `App.razor`, `Routes.razor`, RESX | `Refugio.Web/Components`, `Resources` | Removed once parity is verified. **Do last**, behind a parity checklist. |

**Dependency-flow confirmation:** the SPA depends only on the HTTP contract (DTO JSON). It does not
reference any .NET project. The backend gains four small endpoints that call **existing** application
services. No layer imports anything it didn't already import.

---

## 4. "Actor Model" Design — N/A (justified)

There is **no actor model in this codebase** and none is introduced. Per `CLAUDE.md` ("Akka.NET is
gone"), business logic lives in rich domain aggregates orchestrated by application services. This
migration touches only the presentation tier, so the correct backend pattern to mirror is the
**minimal-API endpoint → application service** shape already used by every endpoint in
`Endpoints/*.cs`. The four new auth endpoints follow it exactly (`IVolunteerService` for login /
change-password; `ClaimsPrincipal` for `me`/`logout`). No request is routed through any actor or
message because none exist.

---

## 5. Data Model & Migration

**No schema change. No EF migration.** This is a UI swap; entities, enums, soft-delete, and all
mappings are untouched. The SPA consumes the **existing DTO contracts** as the wire format
(verbatim, including the JSON enum-string convention via `JsonStringEnumConverter`, already
registered in `Program.cs`). The TypeScript types in §6 are hand-written mirrors of these DTOs:

- `DogDto`, `MedicalRecordDto`, `MedicationDto`, `DogPhotoDto`
- `AdoptionDto`, `CreateAdoptionRequest`, `UpdateAdoptionRequest`, `UpdateAdoptionStatusRequest`
- `DonationDto`, `ExpenseDto`, `ExpensePhotoDto`, `GoalDto`
- `VolunteerDto` (**no PasswordHash** — preserve that omission in the TS type)
- `ShelterTaskDto`, `ShelterEventDto`, `ShelterSettingsDto`
- Read models: `DashboardStats`, `FinanceSummary`/`MonthSummary`, `AdoptionConversionStats`/
  `MonthlyConversionData`, `ShelterStayStats`/`BreedStayData`, `VolunteerCounts`, `UpcomingVisit`,
  `Page<T>`.

**Enum string values to encode as TS union types** (member names are permanent identifiers — use them
verbatim as `<option value>`):

- `DogStatus`: `Available | Adopted | …` (read remaining members from `Dog.cs` when writing the type)
- `AdoptionType`: `Adoption | Foster`
- `AdoptionStatus`: `Applied | Interview | …`
- `VolunteerStatus`: `Active | Inactive | Pending`
- `DonationCategory`: `Monthly | OneTime | InKind | Corporate`
- `ExpenseCategory`: `Medical | Food | Facilities | Supplies | Transport | Other`

Skills `ef-migration` / `add-soft-delete-entity` are **not** invoked — no data work.

---

## 6. SOLID & Clean Architecture Notes

- **Single Responsibility.** Backend: each new auth endpoint does one thing; the JSON upload variants
  do exactly the upload (no view concern). Frontend: one module per concern — `api/client.ts` (transport
  only), one `api/*.ts` per aggregate (endpoint mapping only), `AuthContext` (session state only),
  page components (composition + local state only). No page reaches the network except through `api/*`.
- **Open/Closed.** The API surface is extended (new endpoints) without modifying existing endpoint
  behavior; old form endpoints remain for back-compat. On the client, `client.ts` exposes
  `get/post/put/del` primitives; new aggregates are added as new `api/*.ts` modules without touching the
  base client.
- **Interface Segregation.** Each `api/*.ts` exposes only the calls a feature area needs; pages import
  the narrow module, not a god-client. (The current `ShelterApiClient` is one fat class — the React side
  deliberately splits it per aggregate.)
- **Dependency Inversion.** Pages depend on the typed `api/*` abstraction and the `AuthContext`
  interface, not on `fetch` directly. `AuthProvider` is the single composition point for session state,
  mirroring how DI composes services server-side.
- **Clean Architecture.** The SPA is a pure delivery mechanism over the same contract the API already
  publishes. It holds **no business rules** — validation it does is UX sugar; the authoritative rules
  stay in the domain/services. Auth/RBAC remain server-enforced (cookie + `RequireAuthorization`),
  so a tampered client cannot bypass them.

---

## 7. Step-by-Step Implementation Plan

> Order: backend auth/upload endpoints first (so the SPA has something to call), then SPA foundation,
> then pages in dependency order (auth/layout → simple lists → detail/edit → complex boards →
> manager-only). Each step has a verify action.

### Phase A — Backend enablement (in `Refugio.Web`)

1. **Add JSON auth endpoints** in `Endpoints/AuthEndpoints.cs` (new `MapApiAuthEndpoints(this RouteGroupBuilder api)` called from the `/api` group in `Program.cs`). Keep the existing form endpoints untouched.
   ```csharp
   // POST /api/auth/login  — JSON in, JSON out, sets the same cookie
   api.MapPost("/auth/login", async (LoginRequest body, HttpContext ctx, IVolunteerService volunteers) =>
   {
       var v = await volunteers.LoginAsync(body.Email, body.Password);
       if (v is null) return Results.Unauthorized();
       var role = v.Role == Roles.Manager ? Roles.Manager : Roles.Volunteer;
       var claims = new List<Claim>
       {
           new(ClaimTypes.NameIdentifier, v.Id.ToString()),
           new(ClaimTypes.Name, v.Name),
           new(ClaimTypes.Email, v.Email),
           new(ClaimTypes.Role, role),
       };
       await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
           new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
       return Results.Ok(new { id = v.Id, name = v.Name, email = v.Email, role });
   }).DisableAntiforgery();

   // GET /api/auth/me  — current user or 401
   api.MapGet("/auth/me", (HttpContext ctx) =>
   {
       if (ctx.User.Identity?.IsAuthenticated != true) return Results.Unauthorized();
       return Results.Ok(new
       {
           id = int.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!),
           name = ctx.User.FindFirstValue(ClaimTypes.Name),
           email = ctx.User.FindFirstValue(ClaimTypes.Email),
           role = ctx.User.FindFirstValue(ClaimTypes.Role),
       });
   }).RequireAuthorization();

   // POST /api/auth/logout
   api.MapPost("/auth/logout", async (HttpContext ctx) =>
   {
       await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
       return Results.Ok();
   }).DisableAntiforgery();

   // POST /api/auth/change-password  — JSON, returns 200 / 400
   api.MapPost("/auth/change-password", async (ChangePasswordRequest body, HttpContext ctx, IVolunteerService volunteers) =>
   {
       var id = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
       if (id is null) return Results.Unauthorized();
       if (string.IsNullOrEmpty(body.NewPassword) || body.NewPassword.Length < 6 || body.NewPassword != body.ConfirmPassword)
           return Results.BadRequest(new { error = "invalid" });
       var ok = await volunteers.ChangePasswordAsync(int.Parse(id), body.CurrentPassword, body.NewPassword);
       return ok ? Results.Ok() : Results.BadRequest(new { error = "wrong_current" });
   }).RequireAuthorization().DisableAntiforgery();
   ```
   Add small request records (`LoginRequest(string Email, string Password)`, `ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword)`).
   **Verify:** `dotnet build`; `curl -i -X POST localhost:5110/api/auth/login -H "Content-Type: application/json" -d '{"email":"elena@havensanctuary.org","password":"shelter123"}'` returns 200 + `Set-Cookie`; reusing the cookie, `GET /api/auth/me` returns the user JSON.

2. **Add JSON upload variants** for dog photo (`POST /api/dogs/{id}/photos`), expense receipt, and logo (`POST /api/settings/logo`). The current handlers `Results.Redirect(...)`; add parallel routes (or a `?json=1` branch) that return `Results.Ok(new { url })` on success and `Results.BadRequest(...)` on validation failure, reusing the existing `ImageResizer`/`PhotoFiles` logic. Keep the redirect versions for SSR back-compat until pages are deleted.
   **Verify:** `dotnet build`; multipart `curl` upload returns JSON with the new photo URL.

3. **Wire static files + SPA fallback** in `Program.cs` (after `app.MapStaticAssets()` / API mapping, before `app.Run()`):
   ```csharp
   // Serve the built React SPA. dist/ exists only after `npm run build`.
   var spaDist = Path.Combine(builder.Environment.ContentRootPath, "ClientApp", "dist");
   if (Directory.Exists(spaDist))
   {
       app.UseStaticFiles(new StaticFileOptions
       {
           FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(spaDist),
           RequestPath = ""
       });
       app.MapFallbackToFile("index.html", new StaticFileOptions
       {
           FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(spaDist)
       });
   }
   ```
   Order matters: the `/api` group and `/auth` form endpoints are mapped **before** the fallback, so only non-API paths fall through to `index.html`. The `MapRazorComponents<App>()` line stays during transition; remove it in the final cleanup step.
   **Verify:** with no `dist/` yet, `dotnet run` still boots (guard skips). After building the SPA (later), navigating to `/dogs` directly serves `index.html`.

4. **Exclude `ClientApp` from the .NET compile** and add a publish-time build target in `Refugio.Web.csproj`:
   ```xml
   <ItemGroup>
     <Content Remove="ClientApp\**" />
     <None Remove="ClientApp\node_modules\**" />
   </ItemGroup>
   <Target Name="BuildClientApp" BeforeTargets="Publish">
     <Exec Command="npm ci" WorkingDirectory="ClientApp" />
     <Exec Command="npm run build" WorkingDirectory="ClientApp" />
   </Target>
   ```
   **Verify:** `dotnet build` does not try to compile `ClientApp`; `dotnet publish` runs the npm build and `dist/` lands in the publish output.

### Phase B — SPA foundation (in `ClientApp/`)

5. **Scaffold Vite** — `npm create vite@latest ClientApp -- --template react-ts` (run from `src/Refugio.Web/`), then `npm i react-router-dom`. Create `tsconfig.json` (from template). **Files:** `package.json`, `tsconfig.json`, `vite.config.ts`, `index.html`, `src/main.tsx`, `src/App.tsx`.
   **Verify:** `cd ClientApp && npm install && npm run dev` serves a blank app on `:5173`.

6. **`index.html`** — port the entire `<head>` from `App.razor`: the Tailwind CDN `<script src="https://cdn.tailwindcss.com?plugins=forms,container-queries">`, the **identical `tailwind.config` object** (all color tokens, `borderRadius`, `spacing`, `fontFamily`, `fontSize` — copy verbatim from §App.razor lines 11–96), the Google Fonts links (Montserrat + Inter + Material Symbols Outlined), and the `<style>` block (`.material-symbols-outlined`, `.shadow-soft`, scrollbar, body/h font families). `<body class="bg-background text-on-background"><div id="root"></div><script type="module" src="/src/main.tsx"></script></body>`.
   **Verify:** `npm run dev`, a test `<div class="bg-primary text-on-primary p-md">` renders in the shelter green with correct fonts.

7. **`vite.config.ts`** — base path and dev proxy so `/api`, `/auth`, `/branding`, `/uploads` hit ASP.NET on `:5110`:
   ```ts
   import { defineConfig } from 'vite';
   import react from '@vitejs/plugin-react';

   export default defineConfig({
     plugins: [react()],
     server: {
       port: 5173,
       proxy: {
         '/api':      { target: 'http://localhost:5110', changeOrigin: true, secure: false },
         '/auth':     { target: 'http://localhost:5110', changeOrigin: true, secure: false },
         '/branding': { target: 'http://localhost:5110', changeOrigin: true, secure: false },
         '/uploads':  { target: 'http://localhost:5110', changeOrigin: true, secure: false },
       },
     },
     build: { outDir: 'dist', emptyOutDir: true },
   });
   ```
   Cookies flow through the proxy on the same dev origin (`:5173`), so `credentials: 'include'` works in dev exactly as in prod.
   **Verify:** with `dotnet run` going, `fetch('/api/dashboard', {credentials:'include'})` from the dev app returns 401 (not connection-refused), proving the proxy reaches ASP.NET.

8. **`src/api/client.ts`** — the single transport primitive:
   ```ts
   const JSON_HEADERS = { 'Content-Type': 'application/json' };

   async function request<T>(method: string, url: string, body?: unknown): Promise<T> {
     const res = await fetch(url, {
       method,
       credentials: 'include',
       headers: body !== undefined ? JSON_HEADERS : undefined,
       body: body !== undefined ? JSON.stringify(body) : undefined,
     });
     if (res.status === 401) throw new ApiError(401, 'unauthorized');
     if (!res.ok) throw new ApiError(res.status, await res.text());
     return res.status === 204 ? (undefined as T) : res.json();
   }
   export const api = {
     get:  <T>(u: string) => request<T>('GET', u),
     post: <T>(u: string, b?: unknown) => request<T>('POST', u, b),
     put:  <T>(u: string, b?: unknown) => request<T>('PUT', u, b),
     del:  (u: string) => request<void>('POST', u), // delete endpoints are POST /{id}/delete
   };
   // multipart helper for uploads (no JSON headers)
   export async function upload<T>(url: string, form: FormData): Promise<T> {
     const res = await fetch(url, { method: 'POST', credentials: 'include', body: form });
     if (!res.ok) throw new ApiError(res.status, await res.text());
     return res.json();
   }
   export class ApiError extends Error { constructor(public status: number, msg: string){ super(msg);} }
   ```
   **Verify:** unit-callable from the browser console; `api.get('/api/dashboard')` after login returns stats.

9. **`src/types.ts`** — TypeScript mirrors of all DTOs + `Page<T>` + enum unions (from §5). Single source of truth imported by `api/*` and pages.
   **Verify:** `tsc --noEmit` passes.

10. **`src/api/*.ts`** — one module per aggregate, each a thin typed map of the endpoints already in
    `ShelterApiClient` (the authoritative endpoint catalog):
    - `auth.ts` — `login`, `me`, `logout`, `changePassword` (the new JSON endpoints).
    - `dogs.ts` — list/paged/get/create/update/delete + photos + medical + medications.
    - `adoptions.ts` — list/paged/get/create/update/updateStatus + deleted.
    - `tasks.ts` — list/create.
    - `finance.ts` — donations/expenses/goals (list/paged/get/create/update) + summary + photos + deleted.
    - `volunteers.ts` — list/paged/get/create/update/counts + deleted.
    - `settings.ts` — get/update/uploadLogo.
    - `dashboard.ts` — dashboard + reports (conversion, shelter-stay).
    Mirror exact paths and query-string params from `ShelterApiClient` (e.g. `/api/dogs/paged?search=&status=&page=&pageSize=`).
    **Verify:** `tsc --noEmit`; each module's calls compile against `types.ts`.

11. **`src/auth/AuthContext.tsx`** — session state + guard:
    ```ts
    interface User { id: number; name: string; email: string; role: 'Manager' | 'Volunteer'; }
    interface AuthValue {
      user: User | null;
      loading: boolean;
      login(email: string, pw: string): Promise<boolean>;
      logout(): Promise<void>;
      changePassword(cur: string, next: string, confirm: string): Promise<boolean>;
    }
    ```
    On mount, `authApi.me()` → set `user` or `null`, then `loading=false`. `login` calls the API, stores the returned user. Provide `useAuth()` hook and a `<RequireAuth>` wrapper that renders `<Navigate to="/login">` when `!user` (and a spinner while `loading`), plus `<RequireManager>` for manager-only routes.
    **Verify:** wrap a throwaway page; unauthenticated load redirects to `/login`; after login the user shows.

12. **`src/components/`** — shared UI:
    - `Layout.tsx` + `Sidebar.tsx` + `TopBar.tsx` — port `MainLayout.razor` markup verbatim (sidebar `w-64 fixed`, nav links with active state via `NavLink` `className` callback using the same active classes `text-primary font-bold border-r-4 border-primary bg-primary-container/10`; manager-only links gated on `user.role`). TopBar: shelter name/logo from `settings.get()`, user dropdown (CSS-hover groups port directly), "Change password" + "Sign out" (calls `logout()`). **Drop** the language switcher.
    - `Pagination.tsx` (drives `?page=` query param), `LoadingSpinner.tsx`, `ErrorMessage.tsx`, plus small `StatusChip`, `ConfirmButton` (window.confirm wrapper), `EnumSelect` helpers.
    **Verify:** Layout renders with sidebar + topbar matching the Blazor look side-by-side.

13. **`src/App.tsx`** — `<BrowserRouter>` → `<AuthProvider>` → `<Routes>`. `/login` and (optionally) `/change-password` outside `Layout`; everything else inside `<RequireAuth><Layout/></RequireAuth>`; `/admin/deleted` and `/settings` additionally inside `<RequireManager>`.
    **Verify:** all 25 routes resolve to a placeholder; manager routes redirect a volunteer to `/`.

### Phase C — Pages (dependency order)

> For each page: build the component, wire its `api/*` calls, manage state, port the Tailwind markup
> from the corresponding `.razor` (preserving card/button/headline/chip classes from the visual
> reference). Verify against the live Blazor page for parity. Group order:

14. **Login** (`Login.tsx`) — `auth.login`; inline 401 error; redirect to `/`. **Verify:** seed login works; bad creds show error.
15. **Dashboard** (`Home.tsx`) — `dashboard.get`, `tasks.list`, recent dogs. Guard the donation-goal `÷ DonationGoal` against 0 (known SSR bug — keep guarded). **Verify:** KPI cards match.
16. **Dogs list** (`Dogs.tsx`) — `dogs.paged` with `?search=&status=&page=` via `useSearchParams`; cards + `Pagination` + status filter. **Verify:** filters/pagination update URL and results.
17. **Dog detail** (`DogDetail.tsx`) — `dogs.get` (+ photos/medical/medications). **Verify:** gallery + history render.
18. **Dog check-in** (`DogCheckin.tsx`) — controlled create form → `dogs.create` → navigate to detail; client + server validation. **Verify:** new dog persists.
19. **Dog edit** (`DogEdit.tsx`) — load + `dogs.update`; photo gallery management via `upload()` to the JSON photo endpoint + set-default/delete. **Verify:** edits and photo upload persist.
20. **Medical / Medication edits** (`MedicalRecordEdit.tsx`, `MedicationEdit.tsx`) — get/update; medication `IsActive` toggle. **Verify:** round-trip.
21. **Health** (`Health.tsx`) — `?dogId=` add records/medications. **Verify:** adds attach to the dog.
22. **Volunteers list + edit** (`Volunteers.tsx`, `VolunteerEdit.tsx`) — `volunteers.paged` + `counts` cards + status filter; edit credentials/role/photo/(preferred language field can stay as a stored value even though app i18n is dropped). **Verify:** counts + edits correct; role change reflected.
23. **Funds** (`Funds.tsx`) — tabs donations/expenses/goals/summary via `?tab=`; paged lists; `FinanceSummary` chart; CSV export links to `/api/export/donations` and `/api/export/expenses`. Edits: `DonationEdit`, `ExpenseEdit` (receipt upload), `GoalEdit`. **Verify:** each tab + pagination + export download.
24. **Calendar + Event edit** (`Calendar.tsx`, `EventEdit.tsx`) — `?week=yyyy-MM-dd`; weekly grid from `events.list(from,to)`; create/edit events. **Verify:** week nav + event CRUD.
25. **Adoptions Kanban + edit** (`Adoptions.tsx`, `AdoptionEdit.tsx`) — 5 status columns, per-column "show more" via `?{col}Limit=N` (or paged calls), new-application form, status move via `adoptions.updateStatus`, CSV export to `/api/export/adoptions`. **Verify:** columns, show-more, status drag/move, export.
26. **Reports** (`Reports.tsx`) — `?year=`; `dashboard.adoptionConversion(year)` + `shelterStay()`. **Verify:** charts match.
27. **Settings** (`Settings.tsx`, manager-only) — `settings.get`/`update` + logo upload (`upload()` to JSON logo endpoint). **Verify:** name/phrase/logo persist; topbar updates.
28. **Change password** (`ChangePassword.tsx`) — `auth.changePassword`; inline success/error from JSON. **Verify:** correct/incorrect current password handled.
29. **Admin/Deleted** (`AdminDeleted.tsx`, manager-only) — 8 tabs (dogs/medical/medications/adoptions/donations/expenses/goals/volunteers) via `?tab=`, each listing `*/deleted` and offering restore/purge (`POST /{id}/restore`, `/{id}/purge`); parent-dog liveness warnings for dog-children restores. **Verify:** restore/purge per tab; blocked restore when parent dog is deleted.

### Phase D — Cleanup

30. **Remove Blazor UI** once all 25 pages reach parity: delete `Components/Pages/*.razor` (keep `Error.razor` only if still referenced), `Components/Layout/*.razor`, `App.razor`, `Routes.razor`, `_Imports.razor` Blazor bits, the `Resources/SharedResources*.resx` + marker, and the `MapRazorComponents`/`AddRazorComponents`/localization/`SettingsCacheService` (if now unused) wiring in `Program.cs`. Keep the form-based `/auth/*` endpoints only if an external consumer needs them; otherwise remove.
    **Verify:** `dotnet build`; `dotnet test` (integration tests that asserted SSR HTML must be updated or removed — see §8); SPA serves every route from `dist/`.

---

## 8. Testing Strategy

**Backend (xUnit — keep the existing two projects):**
- **Unit (`Refugio.Tests.Unit`).** No new domain/service tests needed (no logic changed). The auth
  endpoints reuse `IVolunteerService` already covered.
- **Integration (`Refugio.Tests.Integration`, `WebApplicationFactory` + SQLite shared-cache).** Add
  tests for the **new JSON auth + upload endpoints** — these are the only new server behavior:
  - `POST /api/auth/login` with seed creds → 200, body `{id,name,email,role}`, `Set-Cookie` present.
  - `POST /api/auth/login` with bad creds → 401.
  - `GET /api/auth/me` without cookie → 401; with the login cookie → user JSON.
  - `POST /api/auth/logout` → 200 and subsequent `me` → 401.
  - `POST /api/auth/change-password` happy path → 200; wrong current → 400; mismatch/short → 400.
  - JSON upload variant returns `{ url }` on a valid PNG and 400 on a bad/oversized file.
  - **Important:** the existing integration tests assert SSR-rendered HTML from Blazor pages. After
    Phase D those pages are gone — **update those tests to assert the JSON API directly** (page →
    API → service → DB collapses to API → service → DB, which the API tests already cover), or delete
    the now-obsolete HTML assertions. Do this as part of step 30, not before, so the suite stays green
    during the transition.

**Frontend (React):**
- Add **Vitest + React Testing Library** in `ClientApp`. Cover the non-trivial units:
  - `AuthContext`: `me` 401 → `user=null`; login success → user set; logout clears.
  - `RequireAuth` / `RequireManager` redirect logic.
  - `api/client.ts`: 401 throws `ApiError(401)`; 204 returns undefined; query-param builders.
  - A couple of page smoke tests with a mocked `api/*` (Dogs list renders cards, filter updates query).
  - **Verify:** `npm run test` green.
- **No E2E/browser tests required.** Same rationale as the SSR pages: the API integration tests cover
  the full server pipeline, and the React unit tests cover client logic. (If desired later, a thin
  Playwright smoke over login + one CRUD path is optional, not required by this plan.)

---

## 9. Risks, Tradeoffs & Open Questions

- **Tailwind CDN in production.** Keeping the CDN script (per decision #3) means a runtime fetch of a
  JIT compiler and no purge — fine for an internal single-box app, heavier than a built CSS bundle. If
  load time matters later, switch to the Tailwind Vite plugin with the same config. **Tradeoff
  accepted** for parity/speed now.
- **Upload endpoints return redirects today.** The SPA needs JSON variants (step 2). Confirm whether
  to add new routes vs. branch existing ones; this plan adds parallel JSON routes to avoid breaking
  the SSR pages during transition.
- **Existing integration tests assert SSR HTML.** They will fail once Blazor is removed. The plan
  defers their rewrite to step 30 so the suite stays green mid-migration. **Open question:** are any
  of those HTML assertions the *only* coverage of a behavior not also covered by an API test? Audit
  before deleting.
- **CSRF.** The JSON auth/upload endpoints use `DisableAntiforgery()` (matching the existing form
  endpoints) and rely on the SameSite cookie + same-origin policy. For an internal app this matches
  current posture; if stricter CSRF is wanted, add a token header scheme — **out of scope** here.
- **i18n dropped.** Decision #4 removes culture switching. The `Volunteer.PreferredLanguage` field
  still exists in the domain and the volunteer edit form can still store it; it simply has no UI
  effect until i18n is reintroduced. Confirm stakeholders accept English-only for the interim.
- **Dev workflow is two processes.** `dotnet run` (`:5110`) + `npm run dev` (`:5173`, proxying to
  5110). Document this in the repo README. Production is single-process (ASP.NET serves `dist/`).
- **Cookie auth vs SPA.** Chosen over tokens (decision #1/#2) — simplest, same-origin, no token
  storage/XSS surface. The cost is that the SPA cannot be hosted on a different origin without CORS +
  `SameSite=None`; acceptable given the single-host deployment.
