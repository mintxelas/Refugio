# Summary — High-severity security fixes

## What changed

1. **Closed a Broken Function-Level Authorization gap.** Seven REST `DELETE` endpoints now
   require the `Manager` role, matching the `Manager`-only `POST .../delete` endpoint that
   performs the same action:
   - `DELETE /api/dogs/{id}`
   - `DELETE /api/medical/{id}`
   - `DELETE /api/donations/{id}`
   - `DELETE /api/expenses/{id}`
   - `DELETE /api/goals/{id}`
   - `DELETE /api/adoptions/{id}`
   - `DELETE /api/tasks/{id}`

   Before this fix, any authenticated Volunteer (non-Manager) account could hard/soft-delete
   dogs, donations, expenses, goals, adoptions, and medical records by calling the `DELETE`
   verb directly, bypassing the Manager-only restriction the app already enforces on the
   equivalent `POST` route.

2. **Added rate limiting to the login and change-password endpoints.** `POST /api/auth/login`
   and `POST /api/auth/change-password` are now throttled to 5 requests/minute per client IP
   (`Microsoft.AspNetCore.RateLimiting`, configurable via `Auth:RateLimitPermitLimit` /
   `Auth:RateLimitWindowSeconds`), returning `429 Too Many Requests` once exceeded. This closes
   an unlimited credential-guessing window against the app's authentication.

## Why

Both were flagged High severity in the security audit: the DELETE-verb gap is a real
authorization bypass reachable by any low-privilege logged-in user, and the missing rate limit
made online password guessing free.

## Side effects / follow-ups

- **None expected for normal usage.** Every code path that calls these `DELETE` endpoints in the
  app itself (React SPA) or in the existing test suite already authenticates as a Manager, so
  no legitimate caller loses access.
- If any **external REST consumer** was relying on calling `DELETE /api/{resource}/{id}` as a
  non-Manager user, that call will now fail with a redirect/403 — this is the intended fix, but
  worth a heads-up if such a consumer exists outside this repo.
- The rate limiter is **per-server-instance, in-memory** (not distributed). If the app is ever
  scaled to multiple instances behind a load balancer without sticky sessions, the effective
  limit becomes `5 × instance count`. Not a concern for the current single-instance deployment
  (Raspberry Pi), but worth revisiting if that changes.
- Not addressed in this change (still open from the audit, lower priority):
  - **Critical**: hardcoded seed Manager credentials (`elena@havensanctuary.org` / `shelter123`)
    are created unconditionally on first run in any environment, including production.
  - Medium: no antiforgery/CSRF tokens (relies solely on `SameSite` cookie attribute), weak
    password policy (length ≥ 6 only, no complexity check), `wwwroot/photos/` not in
    `.gitignore`, broad CORS policy for the "Mobile" origin.
  - Two NuGet packages flagged by `dotnet build` with known high-severity advisories:
    `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (GHSA-2m69-gcr7-jv3q) and `Microsoft.OpenApi` 2.0.0
    (GHSA-v5pm-xwqc-g5wc) — worth a dependency bump in a follow-up.

## What to verify in code review

- Confirm the seven `.RequireAuthorization("Manager")` additions land on the correct `MapDelete`
  call in each file (not accidentally on a neighboring route) — diff each endpoint file directly.
- Confirm `DELETE /api/medications/{id}` was **intentionally left unchanged** (it calls
  `DeactivateMedication`, a different and less destructive command than
  `DeleteMedication` used by `POST /medications/{id}/delete`) — this is documented in the
  existing code comment, not an oversight.
- Confirm `DELETE /api/volunteers/{id}` and `DELETE /api/events/{id}` were already correctly
  gated before this change (no fix needed there) — verify by reading `VolunteerEndpoints.cs`.
- Run the full integration suite (`dotnet test tests/Refugio.Tests.Integration/`) — 171/171
  passing after this change, including the two new test files
  (`DeleteVerbAuthorizationTests.cs`, `AuthRateLimitTests.cs`).
- Sanity-check the rate limiter's test-environment override in `ShelterWebFactory.cs`
  (`Auth:RateLimitPermitLimit = 1000`) is not accidentally shipped as a production default —
  it's only set inside the test factory, `Program.cs` keeps the real default at 5.
