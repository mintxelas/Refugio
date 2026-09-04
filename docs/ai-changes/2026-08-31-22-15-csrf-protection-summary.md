# Summary — Real CSRF (antiforgery) protection

## What changed

**Backend** (`src/Refugio.Web/`):
- `Program.cs`: registered `AddAntiforgery(opts => opts.HeaderName = "X-XSRF-TOKEN")`; added
  `GET /api/antiforgery/token` (anonymous) which issues the antiforgery cookie and returns the
  matching request token as plain text; added a custom endpoint filter
  (`ValidateAntiforgery`) applied to the whole `/api` route group that validates the
  `X-XSRF-TOKEN` header against the antiforgery cookie for every non-GET/HEAD/OPTIONS/TRACE
  request, returning `400` on failure.
- Removed all 12 `.DisableAntiforgery()` calls across `AuthEndpoints.cs`, `DogEndpoints.cs`,
  `FinanceEndpoints.cs`, `AdoptionEndpoints.cs`, `SettingsEndpoints.cs`, `VolunteerEndpoints.cs`
  — they were no-ops before (no antiforgery existed) and would still be no-ops now (the new
  protection is a custom filter, not the framework's per-endpoint metadata they toggled).

**Frontend** (`ClientApp/src/api/`):
- `client.ts`: `request`, `postAction`, and `upload` now fetch (and cache in memory) the CSRF
  token and attach it as `X-XSRF-TOKEN` on every non-GET call; exports `resetCsrfToken()`.
- `auth.ts`: `login` and `logout` call `resetCsrfToken()` right after their request succeeds.

**Tests**: added `CsrfProtectionTests.cs` (5 tests). Updated `ShelterWebFactory.cs` and the three
test files with their own login helper (`DeleteEndpointTests.cs`,
`DeleteVerbAuthorizationTests.cs`, `RestoreApiTests.cs`) to fetch the CSRF token, log in, then
fetch the token *again* post-login.

## Why

Every mutating endpoint was reachable with only a valid session cookie — nothing checked that
the request actually originated from the app's own frontend. `SameSite=Strict/Lax` was the only
defense, with no fallback if that cookie attribute is ever relaxed.

## Side effects / follow-ups

- **Every mutating `/api/*` call site anywhere (browser, script, curl) now needs a matching
  `X-XSRF-TOKEN` header**, fetched from `GET /api/antiforgery/token` first. The React SPA
  handles this transparently (all API calls funnel through `client.ts`). Any *external* REST
  consumer of this API (mobile app, script, Postman collection) will need to fetch that token
  and attach the header on every POST/PUT/DELETE, including login itself.
- **The token is bound to the caller's identity at issue time.** A token fetched anonymously
  stops validating the instant the user logs in (and the reverse on logout) — this is an
  ASP.NET Core antiforgery behavior, not something this app added. `client.ts`/`auth.ts` handle
  it by refreshing the token after login/logout; any other consumer of the API must do the same
  or it will start getting `400`s on the request right after authenticating.
- The antiforgery cookie is `HttpOnly` and unrelated to the auth cookie — it doesn't affect
  session length, `SameSite` policy, or anything already configured for `CookieAuthenticationDefaults`.
- Not addressed here (separate finding): the "Mobile" CORS policy and how a genuinely
  cross-origin consumer would authenticate at all given `SameSite=Strict` in production — that's
  a pre-existing, unrelated gap.

## What to verify in code review

- Confirm the `ValidateAntiforgery` filter is applied to the `/api` group (`Program.cs`) and not
  accidentally scoped to only some endpoints.
- Confirm `GET /api/antiforgery/token` stays `.AllowAnonymous()` — if auth is ever required on
  it, login itself becomes unbootstrappable (chicken-and-egg: can't get a token to log in with).
- Run the full test suite: **421/421 passing** (245 unit + 176 integration, including the 5 new
  `CsrfProtectionTests`). Also ran `npx tsc -b` in `ClientApp/` — clean, no type errors.
- Manually verified the real HTTP flow end-to-end with `curl` before trusting the automated
  tests (fetch token → login → refetch token → authenticated POST) — this is what caught the
  identity-binding bug in the first pass; worth repeating after any future change to the login
  flow or to `client.ts`'s token caching.
- `CsrfProtectionTests.CsrfToken_FetchedBeforeLogin_NoLongerValidatesAfterLogin` documents and
  guards the identity-binding gotcha specifically — if this test ever needs to be deleted or
  changed to pass, that's a signal the underlying behavior (and the frontend's
  `resetCsrfToken()` calls) need matching review.
