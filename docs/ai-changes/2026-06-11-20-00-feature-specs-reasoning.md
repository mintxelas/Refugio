# Reasoning — Feature specifications generation

## Goal
Generate specification documents for every feature, entity, and use case the Refugio app supports, saved as Markdown under `features/`.

## Step-by-step process

1. **Source-of-truth selection.** CLAUDE.md gives the architecture and page map, but specs must reflect actual code. Read all 13 domain entity files (`src/Refugio.Domain/Entities/*.cs`) for properties, behaviors, enums, and invariants, and all 7 endpoint files (`src/Refugio.Web/Endpoints/*.cs`) for the complete API surface (routes, verbs, auth policies, redirects, upload rules). Read `IReadQueries.cs` for the read models.
2. **Grouping decision.** Specs are organized by **feature area** (12 files + index), each containing its entities and use cases together, rather than separate entity/use-case trees. A reader of "Adoptions" wants the entity, the pipeline, the endpoints, and the email rule in one place.
3. **Use-case format.** Each use case lists actor/UI route/API endpoints/domain behavior/business rules — compact tables for entity properties, prose for rules. IDs (UC-D1, UC-A3…) make them referenceable from future plans/tests.
4. **Cross-cutting rules** (soft delete, UoW, API-as-contract, SSR constraints, enum-as-string) were lifted into `features/README.md` once instead of repeating in every file.
5. **Verification details captured from code, not docs:** photo upload constraints (extensions, 5 MB, file naming), CSV column orders and quoting, logo PNG-only 100×100 normalization with cache-bust, adoption pipeline order from `NextStatus()`, credential invariants from `Volunteer.SetCredentials`, the `DELETE /api/medications/{id}` = deactivate quirk, parent-liveness restore guard.

## Alternatives considered

- **One big SPEC.md** — rejected: ~12 areas × entities × use cases would be unnavigable; per-feature files match how work is scoped (skills, services, endpoint files all split the same way).
- **Separate `entities/` and `use-cases/` folders** — rejected: splits each feature's story across files and duplicates entity context inside use cases.
- **Generating from CLAUDE.md alone** — rejected: CLAUDE.md omits endpoint-level details (query params, redirects, upload rules) that make specs actionable.
- **Using codegraph explore for everything** — partially used (file discovery); direct Reads chosen for entities/endpoints because complete file content was needed, not symbol excerpts.

## Key decisions

- Documented the intentional `UpdateDetails` (silent) vs `ChangeStatus` (email) asymmetry prominently — it is the most likely behavior to be accidentally "fixed".
- Included a soft-delete coverage matrix (which entities have restore/purge UI vs only REST delete) since it varies by entity.
- Tasks and Events get no deleted-records tab — recorded explicitly to prevent assuming the 8-tab admin page covers them.
