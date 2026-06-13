# Prompt feedback — Adoption photo gallery

## Original prompt

> Add the pictures feature for Adoption (photos/adoption/{id}/ folder) pictures.

## What worked well

- Path convention (`photos/adoption/{id}/`) is clear and consistent with the previous change.
- Single-sentence scope implies same constraints (ext, size) as the previous task.

## What could be improved

1. **No gallery semantics stated.** The prompt doesn't say whether adoption photos should have a "default" or "cover" photo concept (like dogs) or be a pure gallery (like expense receipts). This required a judgment call — the expense receipt model was chosen. Specify "no default photo" or "one photo is the cover" explicitly.

2. **No use-case description.** What do adoption photos represent? Applicant ID documents? Home inspection photos? Application attachments? The intended use shapes decisions like: should photos be manager-only upload, or any authorized user? Should they appear on the kanban card or only on the detail page?

3. **No constraint repetition.** Relying on "same constraints as before" is ambiguous across sessions. A complete prompt would restate: `.jpg/.png` only, 2 MB max.

## Suggested improved prompt

> Add a photo gallery to the Adoption aggregate (same as expense receipt photos — no default/cover photo). Files stored at `wwwroot/photos/adoption/{adoptionId}/{guid}.{ext}`. Constraints: `.jpg/.png` only, 2 MB max. Implement the full stack: domain entity `AdoptionPhoto`, repository methods on `IAdoptionRepository`, service methods on `IAdoptionService`, actor messages and `AdoptionActor` registration, endpoints (`GET`, SSR form upload, JSON upload, delete), `ShelterApiClient` method, and EF migration. Photos load on the single adoption detail (`GET /api/adoptions/{id}`) but not in the kanban list. Update `features/feature-adoptions.md` with the new entity and use case.
