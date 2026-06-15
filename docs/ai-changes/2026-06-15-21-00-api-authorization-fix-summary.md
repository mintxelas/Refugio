# Summary: API Authorization Fix

## What changed

| File | Change |
|---|---|
| `src/Refugio.Domain/Entities/Volunteer.cs` | Added `ValidateRole()` called in `Register` and `Update`; throws `ArgumentException` for any value outside `Manager`/`Volunteer` |
| `src/Refugio.Web/Program.cs` | `app.MapGroup("/api")` → `app.MapGroup("/api").RequireAuthorization()` — default-deny |
| `src/Refugio.Web/Endpoints/AuthEndpoints.cs` | Added `.AllowAnonymous()` to login and logout endpoints |
| `src/Refugio.Web/Endpoints/DogEndpoints.cs` | Added `.AllowAnonymous()` to dashboard, dogs list/paged/by-id, dog photos |
| `src/Refugio.Web/Endpoints/VolunteerEndpoints.cs` | Added `.RequireAuthorization("Manager")` to POST/PUT/DELETE volunteer endpoints |
| `tests/Refugio.Tests.Integration/SecurityTests.cs` | New file — 9 tests proving CRIT-1 and CRIT-2 are closed |
| 8 existing test files | Updated data-setup helpers from `AnonClient()` to Manager authenticated client; added `IAsyncLifetime` where needed |

## Why

- **CRIT-1 closed**: Anonymous callers can no longer create Manager accounts. `POST /api/volunteers` requires Manager auth. Domain-level role validation adds defense in depth.
- **CRIT-2 closed**: All financial data (donations, expenses), PII (adoptions, volunteers), and management operations (tasks, events, medical records) now require authentication. Public dog catalog and dashboard remain accessible.
- **HIGH-3 closed**: `PUT /api/volunteers/{id}` and `PUT /api/volunteers/{id}/status` require Manager.

## Side effects / follow-up

- The React SPA's authenticated pages already use cookie auth, so no SPA changes needed.
- Any future endpoint added to `/api` is auth-gated by default. Developers must explicitly call `.AllowAnonymous()` to expose public reads — the safe direction.
- Password hashing (PBKDF2, 10k iterations), brute-force protection, cookie hardening, CSV injection, and open redirect (MED-1 to MED-5) remain open. See the full security audit for prioritization.

## Code review checklist

- [ ] Verify `Volunteer.Register` throws on `Role = "Admin"` or any non-canonical value
- [ ] Confirm `GET /api/dogs` and `GET /api/dashboard` return 200 without a session cookie
- [ ] Confirm `GET /api/donations` returns 302→login without a session cookie
- [ ] Confirm `POST /api/volunteers` returns 302→login without a session cookie
- [ ] Confirm a Volunteer-role user cannot call `POST /api/volunteers` (gets 403/redirect)
- [ ] All 389 tests pass (`dotnet test`)
