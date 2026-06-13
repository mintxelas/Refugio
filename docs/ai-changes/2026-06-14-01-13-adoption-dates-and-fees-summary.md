# Summary: Adoption Dates and Fees

## What Changed

| Layer | File | Change |
|---|---|---|
| Domain | `Adoption.cs` | +4 properties, updated `Submit` + `UpdateDetails` |
| Application | `AdoptionContracts.cs` | +4 fields on `AdoptionDto`, `CreateAdoptionRequest`, `UpdateAdoptionRequest` |
| Application | `DtoMapping.cs` | `ToDto` includes 4 new fields |
| Application | `AdoptionService.cs` | Pass 4 fields through `SubmitAsync` / `UpdateAsync` |
| Infrastructure | `20260613230024_AdoptionDatesAndFees.cs` | EF migration: 2 nullable date cols + 2 bool cols (default false) |
| React | `types.ts` | `AdoptionDto` interface extended |
| React | `api/adoptions.ts` | `create` + `update` payloads extended |
| React | `AdoptionEdit.tsx` | Date pickers + checkboxes added; form state + load + submit updated |
| Tests | `AdoptionServiceTests.cs` | +4 unit tests |
| Tests | `AdoptionApiTests.cs` | +3 integration tests |
| Build | Both test `.csproj` | `Microsoft.NET.Test.Sdk` bumped `17.12.*` → `17.13.*` (net10 SDK compat) |

## Why
Staff needed to record when a pre-adoption handoff and formal adoption occurred, and whether a fee was charged at each stage. None of these were tracked — `ArrivalDate` is on `Dog`, not `Adoption`, and dates like pre-adoption handoff were entirely absent.

## Side Effects / Follow-up
- Existing adoptions will have `NULL` dates and `false` fee flags — no backfill needed.
- The `Microsoft.NET.Test.Sdk` upgrade to `17.13.*` fixes a pre-existing environment incompatibility with the .NET 10 SDK's stricter MSBuild file-write behavior.
- 12 pre-existing integration test failures remain (all expense-related, from the `expense-tax-triplets` feature in progress).

## Code Review Checklist
- [ ] `Adoption.Submit` optional parameters are in the correct position (after `createdAt`) — verify no existing callers pass positional arguments past `status`/`createdAt`
- [ ] `UpdateDetails` signature: confirm all callers in seeded tests compile without changes (they use named or fewer params, which are now defaults)
- [ ] Migration: verify two nullable date columns and two bit columns with `defaultValue: false` in the generated `.cs`
- [ ] React form: confirm date input uses `type="date"` and `.substring(0, 10)` normalization for existing ISO 8601 strings from the API
- [ ] No domain event raised in `UpdateDetails` for these new fields — intentional, matches existing behavior
