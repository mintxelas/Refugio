# Reasoning: Security hardening pass

## Process

1. **Read prior work first.** `docs/ai-changes` already held four earlier security passes: authz default-deny, PBKDF2 600k, magic bytes, CSV injection, DELETE-verb authz, rate limiting, CSRF. Re-reporting those would be noise, so the audit looked for what remained.
2. **Threat model.** Attacker profiles: anonymous internet user (public dog catalog, login), low-privilege volunteer, ex-volunteer with an old cookie, compromised third party (CDN), malicious Manager/insider (DoS only). Assets: manager session, volunteer and adopter PII, finances, medical data, the Pi's availability.
3. **Endpoint sweep.** Grepped every `Map*` call with its auth requirement and compared parallel routes (POST delete vs DELETE). Found `DELETE /events/{id}` was the only one missed in the earlier DELETE-verb fix.
4. **Auth lifecycle.** Followed login, cookie, and later state changes. The cookie was never re-validated, so deactivated or demoted users kept their rights for 7 days. Login ignored `Status`.
5. **Startup code.** `DatabaseInitializer.EnsureSeedAccount` looked harmless (a migration helper), but it re-enables a known password on every restart. This was the worst finding: anyone who had read the README could log in as Manager after the next reboot, even when an admin had disabled that account.
6. **Frontend supply chain.** `dist/index.html` loads `cdn.tailwindcss.com` (the Tailwind "play CDN", which its docs say not to use in production). A JS dependency you cannot pin, running in a Manager session, is a full compromise vector. It also blocks any useful CSP.
7. **Dependencies.** `dotnet list package --vulnerable` was clean. `npm audit` flagged react-router (prod) and the vite/vitest chain (dev).
8. **Fix loop.** For each finding: write a failing test, watch it fail for the right reason, apply the minimal fix, watch it pass. Ran the full suite after the riskiest step (session validation) and at the end.

## Key decisions

- **Session stamp = SHA-256 of the password hash, truncated.** No migration and no DTO change (`VolunteerDto` must never carry hash material). It is computed in `OnSigningIn` straight from the DB. Rejected: a `SecurityStamp` column (needs a migration, and every credential path has to remember to bump it); ASP.NET Identity (a rewrite).
- **Per-request DB check instead of a periodic check.** Revocation is instant. One PK lookup on SQLite is microseconds, and static files skip the auth middleware. A `ponytail:` comment marks the upgrade path.
- **Self password change re-issues the cookie.** Otherwise the user would be logged out by their own action. Other sessions still die, which is the point.
- **Seed password from config, demo fallback only outside Production.** This keeps tests and dev zero-config (`Testing` env still uses `shelter123`) and makes a fresh Production deploy fail loudly instead of silently creating a known credential.
- **Tailwind 3.4 compiled, not v4.** The CDN serves v3. Compiling v3 with the config copied verbatim gives pixel parity, confirmed by screenshots. v4 changes preflight defaults (border color, etc.) and would mix a visual migration into a security change. Noted as a follow-up.
- **CSP without `'unsafe-inline'`.** The inline `<style>` moved to `index.css`. React `style` props use CSSOM, which CSP does not block. `img-src https:` is allowed because dog photo URLs are free-form, and images are low risk.
- **CSP off in Development.** Scalar's API explorer needs inline scripts, and in dev the SPA is served by Vite, not Kestrel.
- **Medical data stripped in the endpoint, not the service.** It is a presentation/authorization concern. The actor and service stay identity-agnostic, as they are today.
- **Logo bomb: cap plus bounded inflate.** The dimension cap alone still lets a crafted IDAT stream inflate without limit. Reading exactly `h*(stride+1)` bytes closes both holes.

## Alternatives rejected

- A global API rate limit: the Pi sits behind a proxy and the data is not public. The cost/benefit was low.
- Raising the password minimum in the domain: this would break seed/test credentials and existing users. It is a product decision, so it is listed as open.
- Hashing the stamp with an HMAC key: the cookie is already encrypted and authenticated by Data Protection, so a plain hash is enough.
