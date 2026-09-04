# Reasoning — High-severity security fixes

## Context

A full security audit of the repo (auth, authorization, file upload, CORS, seeding, CSV export,
client) surfaced two High-severity findings, requested for a fix in this change:

1. **Broken Function-Level Authorization (BFLA)**: several REST `DELETE` verb endpoints executed
   the exact same domain command as a parallel `POST .../delete` endpoint, but only the `POST`
   route required the `Manager` role. The `DELETE` route inherited just the API group's baseline
   `RequireAuthorization()` (any authenticated user), so any logged-in Volunteer could bypass the
   intended Manager-only gate simply by calling the other HTTP verb.
2. **No rate limiting on `/api/auth/login` and `/api/auth/change-password`**: nothing prevented
   unlimited password-guessing attempts against either endpoint.

## Step-by-step

### 1. Locating every mismatched DELETE route
Grepped every `MapDelete(` call across `src/Refugio.Web/Endpoints/*.cs` and compared each one
against its sibling `POST .../delete` route to check whether both invoke the same actor command
and carry the same authorization policy. Found seven mismatches, all missing
`.RequireAuthorization("Manager")` on the bare REST verb:

- `DogEndpoints`: `DELETE /dogs/{id}`, `DELETE /medical/{id}`
- `FinanceEndpoints`: `DELETE /donations/{id}`, `DELETE /expenses/{id}`, `DELETE /goals/{id}`
- `AdoptionEndpoints`: `DELETE /adoptions/{id}`
- `TaskEndpoints`: `DELETE /tasks/{id}`

One route was deliberately **not** touched: `DELETE /medications/{id}` calls
`DeactivateMedication`, a different (less destructive, already-authenticated-only) command than
`POST /medications/{id}/delete` (`DeleteMedication`) — the code comment ("Kept verb: external
consumers 'delete' a medication by deactivating the course") confirms this asymmetry is
intentional, not a bug, so no change was made there.

`DELETE /volunteers/{id}` already had `.RequireAuthorization("Manager")` explicitly — no fix
needed.

### 2. Fix
Added `.RequireAuthorization("Manager")` to each of the seven identified routes — the minimal
change that makes the DELETE verb match the authorization level already enforced on the
equivalent POST route. No new abstraction was introduced; this mirrors the existing convention
used everywhere else in the codebase.

### 3. Verifying no regression
Checked every existing integration test that calls `DeleteAsync(...)` on the affected paths
(`DogsApiTests`, `AdoptionApiTests`, `VolunteerApiTests`, `TasksApiTests`, `PurgeApiTests`,
`RestoreApiTests`). All of them already authenticate as the seeded Manager account before
calling `DELETE`, so tightening the policy could not break them — confirmed by running the full
integration suite after the change (171/171 passing).

### 4. New regression test for the fix
Added `DeleteVerbAuthorizationTests.cs`, mirroring the existing `DeleteEndpointTests.cs` pattern
(anonymous / Volunteer-role / Manager clients), covering the representative case (`/donations`)
plus one aggregate outside Finance (`/tasks`) to prove the fix generalizes across actors, not
just one endpoint file.

### 5. Rate limiting design
Considered options:
- **Hardcoded 5 requests/minute, no configuration** — simplest, but would immediately break the
  existing integration suite, since `ShelterWebFactory.CreateAuthenticatedClientAsync()` calls
  `POST /api/auth/login` once per helper call, and several test classes (`DeleteEndpointTests`,
  `RbacTests`, etc.) call that helper many times within a single test run against one shared
  in-memory host. All of that traffic shares one rate-limiter partition because
  `HttpContext.Connection.RemoteIpAddress` is `null` under `TestServer`, so it collapses into a
  single "unknown" IP bucket. Rejected: would make CI flaky/red without adding real coverage.
- **Skip the limiter entirely when `Environment.IsDevelopment()`** — rejected: doesn't test the
  real behavior at all, and conflates "testing" with "development", when they're already
  separate ASP.NET Core environments here (`Testing` vs `Development` vs `Production`).
- **Configurable permit limit/window via `IConfiguration`, defaulting to 5/60s** — chosen. Uses
  ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting` (no new package), partitions by
  client IP via `RateLimitPartition.GetFixedWindowLimiter`, and reads `PermitLimit`/`Window` from
  config (`Auth:RateLimitPermitLimit`, `Auth:RateLimitWindowSeconds`). `ShelterWebFactory` raises
  the limit to 1000 for the whole suite (effectively off), and a small dedicated test
  (`AuthRateLimitTests`) uses `WebApplicationFactory.WithWebHostBuilder` to push the limit back
  down to 3 and prove a 4th request in the same window returns `429 Too Many Requests`.

Applied `.RequireRateLimiting("auth")` to both `/auth/login` and `/auth/change-password` — the
two endpoints that let an attacker test a password guess.

## Alternatives considered and rejected

- **Account lockout after N failed attempts** (stored on the `Volunteer` entity) — more
  thorough, but a bigger, stateful change (schema + domain behavior) than the scope of "fix the
  High findings" calls for; a per-IP rate limit is the standard, lower-risk first line of
  defense and was flagged as the concrete recommendation in the audit. Left as a possible
  follow-up.
- **Removing the duplicate DELETE routes entirely** instead of gating them — would be a larger,
  more disruptive change (breaking any external REST consumer relying on that verb per the
  code's own comments) for no extra security benefit over just fixing the policy.
