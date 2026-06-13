# Summary: Update CLAUDE.md to reflect React UI

## What changed

`CLAUDE.md` rewritten to match actual codebase — React SPA replaced Blazor SSR.

### Removed
- Skills: `blazor-ssr-form-page`, `add-localization`
- Entire "Blazor ↔ API integration — critical" section
- Entire "Blazor SSR — critical constraints" section
- Entire "Localization" section
- References to `ShelterApiClient`, `ForwardCookieHandler`, `SettingsCacheService`, `FormReader`, `Validator`, `DogHelpers`, `SharedResources`, RESX files, `@attribute [Authorize]`, Antiforgery, `App.razor`
- ShelterApi named-client rewiring in integration test section
- Home.razor DivideByZeroException fix note

### Added
- "React SPA — ClientApp/" section: folder layout, conventions, types.ts/api/*.ts pattern, invariant culture CSV warning
- Explicit "Do NOT create or edit `.razor` files" warning
- React file names in Pages table
- "React SPA replaced Blazor SSR" design decision entry
- npm commands (dev server, build)

### Updated
- Request flow diagram: React browser → /api/* → actor → service
- Auth section: JSON `/api/auth/*` endpoints table
- Integration test auth: `POST /api/auth/login` JSON
- `Refugio.Web` key pieces: now mentions `ClientApp/`

### Also fixed
- `DeleteEndpointTests.cs` doc comment still said "Blazor SSR forms" — updated.

## Why

`CLAUDE.md` is the primary guidance file for Claude Code sessions. Stale Blazor docs
would cause Claude to generate Razor pages, edit `.razor` files, and reference deleted
helpers — all of which would fail to compile. Accurate docs prevent entire classes of
wrong-direction work.

## Side effects / follow-up

- `.claude/skills/blazor-ssr-form-page` and `.claude/skills/add-localization` still exist on disk but are no longer referenced in the skills table. Can be deleted if desired.
- Any future feature that needs client-side UI must go in `ClientApp/src/pages/` (React), not `.razor`.

## Code review checklist

- [ ] No `.razor` references remain in CLAUDE.md
- [ ] React SPA section accurately describes `ClientApp/` layout
- [ ] Auth endpoints table matches `AuthEndpoints.cs` actual routes
- [ ] Integration test auth description matches `ShelterWebFactory.CreateAuthenticatedClientAsync`
- [ ] `DeleteEndpointTests.cs` comment is accurate
