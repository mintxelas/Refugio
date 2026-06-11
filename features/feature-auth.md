# Feature: Authentication & Authorization

Cookie-based authentication over the `Volunteer` aggregate's credentials, with two roles and a Manager policy for destructive actions. Because pages call the API over real HTTP with forwarded cookies, RBAC applies identically to in-app and external calls.

## Model

- **Scheme:** `CookieAuthenticationDefaults`; sessions last 7 days with sliding expiry.
- **Claims on login:** `NameIdentifier` (volunteer id), `Name`, `Email`, `Role`.
- **Roles:** `Roles.Manager`, `Roles.Volunteer` (constants — never inline strings; normalized at startup).
- **Policy `"Manager"`:** required by all destructive endpoints (delete/restore/purge, settings update, deleted listings).
- **Hashing:** PBKDF2-SHA256 (`PasswordHelper`); rules on the aggregate (see [Volunteers](feature-volunteers.md)).
- **Pages:** every Blazor page carries `@attribute [Authorize]`; unauthenticated users are redirected to `/login` (`<AuthorizeRouteView>` + `<RedirectTo>`); `/login` uses `BlankLayout` and allows anonymous.
- **Seed login:** `elena@havensanctuary.org` / `shelter123` (Manager).

## Use cases

### UC-AU1: Log in
- **UI:** `/login` — POST form (fields `email`, `password`).
- **API:** `POST /auth/login` (antiforgery disabled).
- **Flow:** `IVolunteerService.LoginAsync` verifies via `Volunteer.VerifyPassword`; failure → redirect `/login?error=1`; success → sign in cookie, then if the volunteer has a supported `PreferredLanguage`, set the culture cookie (1-year expiry), redirect `/`.

### UC-AU2: Log out
- **API:** `GET /auth/logout` — signs out, redirect `/login`.

### UC-AU3: Change own password
- **UI:** `/change-password`.
- **API:** `POST /auth/change-password` (auth, antiforgery disabled). Fields `currentPassword`, `newPassword`, `confirmPassword`.
- **Rules:** all fields required; new password ≥ 6 chars and must match confirmation; current password must verify (`Volunteer.ChangePassword`). Outcomes redirect with `?success=1` / `?error=1`.

### UC-AU4: Role-gated UI and endpoints
- Destructive UI controls are hidden/disabled for non-managers via `<AuthorizeView Roles="Manager">`; the endpoints enforce the policy server-side regardless.

### UC-AU5: Switch UI language
- **API:** `GET /set-language?culture=&returnUrl=` — only honors supported cultures; sets the culture cookie (1 year), redirects back. See [Localization](feature-localization.md).
