# Summary: Security hardening pass (Nov 2026)

## What changed

Fixes, by severity. Each one has a regression test that failed before the fix.

| # | Severity | Finding | Fix | Test |
|---|---|---|---|---|
| 1 | **High** | Seed backdoor: on every startup, `EnsureSeedAccount` turned login back on for `elena@havensanctuary.org` with password `shelter123` if it had been disabled. A fresh Production DB also got that known password. | Removed `EnsureSeedAccount`. The seed manager password now comes from `Seed:ManagerPassword`. Outside Production it falls back to the demo password. A fresh Production DB with no password set fails at startup with a clear message. | `SeedAccountTests`, `SeedDataTests` |
| 2 | **High** | Sessions could not be revoked. Deactivating, deleting, demoting or resetting the password of a volunteer left their 7-day cookie fully working, Manager role included. Inactive and Pending volunteers could also log in. | `VolunteerSession` cookie events: a password stamp is added at sign-in and the volunteer's state is checked on every request. Login requires `Status == Active`. Changing your own password re-issues your cookie. | `HardeningTests.Session_*`, `Login_InactiveVolunteer_IsRejected` |
| 3 | **High** | `DELETE /api/events/{id}` had no Manager check, so any volunteer could delete events (`POST .../delete` already required Manager). | `.RequireAuthorization("Manager")` | `DeleteEvent_ViaDeleteVerb_AsVolunteerRole_IsForbidden` |
| 4 | **High** (supply chain) | The SPA loaded Tailwind's runtime script from `cdn.tailwindcss.com` inside every logged-in session. That gave a third party full JS access, could not be pinned with SRI, and ruled out a CSP. | Tailwind 3.4 is now compiled at build time (`tailwind.config.js`, `postcss.config.js`, `src/index.css`). `index.html` has no inline or CDN scripts or styles left. | `ClientApp/test/indexHtml.test.ts` |
| 5 | Medium | No security headers. | `X-Content-Type-Options: nosniff` and `Referrer-Policy: same-origin` on every response, plus a strict CSP outside Development (`script-src 'self'`, `frame-ancestors 'none'`, `object-src 'none'`, ...). | `Responses_CarrySecurityHeaders` |
| 6 | Medium | Anonymous `GET /api/dogs/{id}` returned the full medical history and medications. | Anonymous callers get the DTO with `MedicalRecords` and `Medications` set to null. | `GetDog_Anonymous_DoesNotExposeMedicalHistory` |
| 7 | Medium | IDOR: any volunteer could replace or delete any other volunteer's photo. | These endpoints now allow only the volunteer themself or a Manager. | `VolunteerPhoto_*` |
| 8 | Medium | Known advisories in `react-router-dom` 6.x (open redirect) and in dev tooling (vite/vitest/esbuild/tinypool). | react-router-dom 7.18, vite 8, vitest 5, plugin-react 6, jsdom latest. `npm audit`: 0 vulnerabilities. | build + vitest |
| 9 | Low | Credentialed CORS for `localhost:8081/19006` was active in Production. | CORS now applies only in Development. Native mobile fetch does not need CORS. | `Cors_OutsideDevelopment_GrantsNoCrossOriginCredentials` |
| 10 | Low | Logo PNG decompression bomb: a 10000x10000 image header plus an unbounded inflate could allocate more than 1 GB on the Pi (Manager-only path). | Images are capped at 2048x2048. Inflate reads only the number of bytes the dimensions call for. | `Resize_OversizedDimensions_RejectedBeforeDecoding` |

Final state: 262 unit + 201 integration + 5 vitest tests pass. `tsc -b` is clean. `dotnet list package --vulnerable` and `npm audit` are clean.
Manually checked: app run in Staging mode (CSP on, built SPA). Login, dashboard and a form page render the same as before, with zero console/CSP errors.

## Side effects / follow-ups (action required)

- **Production deploy:** existing DBs are fine; no seed runs. **Change the `elena@havensanctuary.org` password now** if it is still `shelter123`. The old `EnsureSeedAccount` would have reset it on every restart anyway. For a fresh Production DB, set `Seed:ManagerPassword` (env var `Seed__ManagerPassword`).
- **Everyone is logged out once** after deploy: old cookies have no stamp claim. Expected.
- **Inactive/Pending volunteers can no longer log in.** Their open sessions end on the next request.
- **Volunteers can no longer delete events** with the DELETE verb. This matches the POST route.
- **One extra DB lookup per authenticated request** (indexed by PK). Negligible at shelter scale. A `ponytail:` comment marks where to add caching.
- Frontend toolchain got major upgrades (vite 8, vitest 5). Run `npm run dev` once to confirm the dev proxy still behaves.

## Still open (not fixed; judgment calls)

- Password policy is 6 characters minimum, with no breach check. Rate limiting (5/min/IP) partly mitigates this. Recommend 12+ characters and possibly TOTP for Managers.
- Every authenticated volunteer can read all volunteers' PII (emails, phones), adoption applicants' data and finances. Decide whether that is intended. If not, add per-area role policies.
- The anonymous dashboard shows donation totals, and anonymous dog DTOs include free-text `Notes`. Confirm both are meant to be public.
- `GET /api/settings/backup` (Manager) zips the DB including password hashes. Store backups carefully.
- `SeedData` re-seeds whenever the `Dogs` table is empty. In Production with no config it now throws instead of creating a known account.
- Google Fonts are still loaded cross-origin (CSS only; scripts are not allowed). Self-host the fonts to drop the third-party request.

## Review checklist

- `Program.cs`: the headers middleware runs before `UseStaticFiles` (so uploads get `nosniff`). CSP is skipped only in Development. CORS only in Development.
- `VolunteerSession.ValidateAsync`: every reject path calls `RejectPrincipal` + `SignOutAsync`. Role mapping is shared with login (`RoleClaimFor`).
- `AuthEndpoints` change-password: re-sign-in happens only after a successful change.
- `DatabaseInitializer`/`SeedData`: no hardcoded password left in Infrastructure.
- `ImageResizer`: `ReadAtLeast(..., throwOnEndOfStream: false)` + underrun check keeps the earlier error semantics.
- `ClientApp`: compare `tailwind.config.js` with the old inline config (moved verbatim). Click through 2-3 pages in a non-Development run to confirm styling.
