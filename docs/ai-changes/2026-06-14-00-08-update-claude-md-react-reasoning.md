# Reasoning: Update CLAUDE.md to reflect React UI

## Problem

`CLAUDE.md` was written when the UI was Blazor SSR. The UI was replaced with a React SPA
(`ClientApp/`), but `CLAUDE.md` still described Blazor pages, RESX localization, `ShelterApiClient`,
`ForwardCookieHandler`, Antiforgery, and `FormReader` — all deleted. Future Claude sessions would
generate Blazor code and contradict the actual codebase.

## Steps taken

1. **Read the file** — confirmed 20+ stale references across every section.
2. **Identified sections to remove entirely**:
   - "Blazor ↔ API integration — critical" (ShelterApiClient, cookie forwarding, parallel scoped DbContexts)
   - "Blazor SSR — critical constraints" (form rendering, @onsubmit, AntiforgeryToken, sticky values)
   - Localization section (RESX, SharedResources, IStringLocalizer — all deleted)
3. **Identified sections to update**:
   - Skills table: removed `blazor-ssr-form-page` and `add-localization`
   - Architecture `Refugio.Web` row: now mentions React SPA in `ClientApp/`
   - Request flow diagram: React SPA → fetch /api/* → actor → service → DB
   - Key pieces table: removed ShelterApiClient, ForwardCookieHandler, SettingsCacheService, Helpers/FormReader, Helpers/Validator, Helpers/DogHelpers; added `ClientApp/`
   - Integration tests description: removed ShelterApi named-client rewiring note
   - Auth endpoints table: updated to JSON `/api/auth/*`
   - Auth section: removed `@attribute [Authorize]` Blazor note
   - Styling section: no longer references `App.razor`
   - Pages table: now lists React file names, not `.razor` files
   - Design decisions: replaced "UI consumes its own REST API" (ShelterApiClient pattern) with "React SPA replaced Blazor SSR" decision
   - Testing conventions: removed SSR-page integration test note, updated auth helper description
4. **Added new "React SPA — ClientApp/" section** describing folder structure, conventions, TypeScript types, and invariant culture note for CSV.
5. **Fixed DeleteEndpointTests.cs doc comment** — still said "Blazor SSR forms".

## Why these choices

- Kept the `ef-migration`, `add-service-operation`, `add-aggregate`, `add-soft-delete-entity` skills — still fully applicable.
- Kept all backend architecture sections unchanged — actors, domain, application services are untouched.
- Added explicit "Do NOT create or edit `.razor` files" warning to prevent future regressions.
- InvariantCulture CSV note added — this was a real bug discovered during Blazor removal; worth documenting so it's not re-introduced.

## Alternatives rejected

- Partial update (leave Blazor sections but mark them deprecated): creates ambiguity and Claude would still try to use them.
- Separate React-specific CLAUDE.md in ClientApp/: splits guidance, harder to maintain.
