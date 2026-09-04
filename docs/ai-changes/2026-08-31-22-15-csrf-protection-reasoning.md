# Reasoning — Real CSRF (antiforgery) protection

## Context

The security audit flagged (Medium): no real CSRF protection existed anywhere in the app. Every
`.DisableAntiforgery()` call scattered across the endpoint files was a no-op, because
`AddAntiforgery()`/antiforgery middleware was never registered — the app relied solely on the
`SameSite` cookie attribute (`Strict` in production, `Lax` in development) as its only CSRF
defense, with no fallback if that attribute were ever relaxed (e.g. to support the cross-origin
"Mobile" CORS policy, which needs `SameSite=None` to work at all).

## Step-by-step

### 1. Picking the CSRF pattern for a JSON SPA
ASP.NET Core's antiforgery system was designed around server-rendered HTML forms (hidden
`__RequestVerificationToken` field). For a React SPA talking JSON over `fetch`, the standard
adaptation is the **double-submit header pattern**: the antiforgery cookie stays `HttpOnly` (JS
never touches it), and a separate endpoint hands the SPA the matching plaintext request token,
which the SPA echoes back in a custom header (`X-XSRF-TOKEN`) on every mutating call. This is
Microsoft's own documented pattern for "Antiforgery with Minimal APIs and SPAs".

### 2. First attempt: `.RequireAntiforgery()` — doesn't exist
Tried adding `.RequireAntiforgery()` at the `/api` route-group level, mirroring how
`.RequireAuthorization()` is already used group-wide in this codebase. This doesn't compile —
Minimal APIs have no such method. Checked Microsoft's own docs directly (not just IntelliSense
guesswork): **antiforgery validation for Minimal APIs is automatic only for endpoints binding a
typed `[FromForm]` parameter** (including `IFormFile`). `.DisableAntiforgery()` is the *only*
lever the framework ships — an opt-out for that one automatic case. There is no built-in opt-in
for JSON endpoints.

This also explains why the codebase's existing `.DisableAntiforgery()` calls were harmless
no-ops in two independent ways: no antiforgery services were registered at all, *and* none of
those endpoints (which read `HttpContext.Request.Form.Files` manually rather than through typed
`[FromForm]` binding) would have triggered the automatic requirement even if antiforgery had
been registered.

### 3. Real fix: a custom endpoint filter
Since none of this app's mutating endpoints use typed form binding, protection has to be
explicit. Added a small `IEndpointFilter`-shaped local function (`ValidateAntiforgery`) that:
- Skips validation for safe methods (GET/HEAD/OPTIONS/TRACE) — matches how the built-in
  antiforgery middleware behaves, so GETs need no token.
- For every other method, resolves `IAntiforgery` from `HttpContext.RequestServices` and calls
  `ValidateRequestAsync`, returning `400` on `AntiforgeryValidationException`.

Applied via `.AddEndpointFilter(ValidateAntiforgery)` on the `/api` route group — one line,
covers every endpoint mapped through it, including the multipart photo/logo upload endpoints
(the filter runs before the handler regardless of how the handler reads the request body, so it
protects them too, unlike the framework's typed-form-binding-only auto-check).

Given this, the earlier `UseAntiforgery()` middleware registration became dead weight (it only
acts on `IAntiforgeryMetadata`, which nothing in this app sets) and was removed — the custom
filter is now the single source of truth for CSRF enforcement.

### 4. Removing the no-op `.DisableAntiforgery()` calls
All 12 occurrences (`AuthEndpoints` ×3, `DogEndpoints` ×3, `FinanceEndpoints` ×2,
`AdoptionEndpoints` ×2, `SettingsEndpoints` ×1, `VolunteerEndpoints` ×1) were deleted. They did
nothing before (no antiforgery registered) and would do nothing now (the app's custom filter,
not the framework's per-endpoint metadata, is what enforces validation) — keeping them would
misleadingly suggest those specific endpoints are exempt from CSRF protection, when in fact all
of them (including login, logout, and every file upload) are now protected.

### 5. The bug the manual verification caught: identity-bound tokens
First implementation fetched the CSRF token once per client/session and cached it. Verified end
to end with `curl` before touching the frontend or trusting the integration tests, and found:
login succeeded (still anonymous when validated), but the *next* mutating call
(`POST /api/dogs`, now authenticated) failed with `400` — despite sending the exact same
cookie+header pair that had just worked for login.

Root cause: ASP.NET Core's default antiforgery token generator embeds the caller's identity
(`HttpContext.User.Identity.Name`) into the token at issue time, and `ValidateRequestAsync`
rejects a token whose embedded identity doesn't match the current request's identity. A token
fetched while anonymous carries an empty identity; once the user logs in, every later request is
authenticated, so the cached anonymous token no longer matches and validation fails — a stale
token bug, not a broken implementation.

Fix: treat login and logout as CSRF-token-invalidating events. `client.ts` exposes
`resetCsrfToken()`; `auth.ts`'s `login`/`logout` call it right after their API call succeeds, so
the next mutating request re-fetches a token bound to the new identity. The integration test
helpers (`ShelterWebFactory.CreateAuthenticatedClientAsync`, and the three ad-hoc
`VolunteerRoleClientAsync` helpers in `DeleteEndpointTests`, `DeleteVerbAuthorizationTests`,
`RestoreApiTests`) got the equivalent fix: fetch the token once for the (anonymous) login call,
then fetch it again immediately after login succeeds.

### 6. Regression coverage
Added `CsrfProtectionTests.cs`:
- The token endpoint returns a non-empty token anonymously.
- A fully authenticated client with the header stripped gets `400` on a mutating call.
- The same call with the header attached succeeds (`201`).
- A GET without any token still succeeds (safe methods stay exempt).
- **The exact bug found during manual verification**: a token fetched before login, reused
  without refresh after login, gets `400` — this test would have caught the identity-binding
  issue immediately instead of needing a `curl` investigation, and guards against it being
  silently reintroduced.

## Alternatives considered and rejected

- **Disable identity-binding via a custom `IAntiforgeryAdditionalDataProvider`** — would have
  sidestepped the "refetch after login" requirement entirely, since the token would validate
  identically before and after authentication. Rejected: identity-binding is a real (if
  secondary) defense-in-depth property of ASP.NET Core's antiforgery system, protecting against
  a stale anonymous token being replayed after a different user authenticates in the same
  browser session. Refetching after login/logout keeps that property intact for two extra lines
  of frontend code (and the equivalent in test helpers) — not worth trading away.
- **Rely only on `SameSite=Strict`, skip real CSRF tokens entirely** — this was the audit
  finding being fixed; `SameSite` alone has no fallback if the cookie policy is ever relaxed
  (e.g. for the cross-origin "Mobile" policy, which structurally cannot work with
  `SameSite=Strict`/`Lax` cookies and may eventually need `SameSite=None`).
- **Traditional hidden-form-field token instead of a header** — doesn't fit a JSON `fetch`-based
  SPA; would require serializing the token into every request body instead of a single header
  attached once per mutating call.
