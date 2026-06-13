# Reasoning: Adoption Dates and Fees

## Problem
The `Adoption` entity captures no milestone dates or fee information beyond the application timestamp. Staff cannot track when a pre-adoption handoff or formal adoption occurred, nor whether a fee was collected at each stage.

## Implementation Steps

### Step 1 — Domain entity
Added four properties with private setters to `Adoption`:
- `PreAdoptionDate` (`DateTime?`) — nullable so it's absent until the event occurs
- `AdoptionDate` (`DateTime?`) — same
- `PreAdoptionFeeCharged` (`bool`, default `false`) — non-nullable; no three-state ambiguity needed
- `AdoptionFeeCharged` (`bool`, default `false`)

Updated `Adoption.Submit` factory with four optional parameters (defaults `null`/`false`) appended after existing optional parameters to preserve backward compatibility with existing call sites.

Updated `Adoption.UpdateDetails` with the same four parameters. Deliberately no domain event raised — these are administrative bookkeeping fields that don't drive status notifications, matching the existing precedent of `UpdateDetails` being silent.

### Step 2 — Application contracts
Added four fields to `AdoptionDto`, `CreateAdoptionRequest`, and `UpdateAdoptionRequest`. Parameters are optional with sensible defaults on the request records so existing test call sites compile without changes.

### Step 3 — DtoMapping
Added four new fields to `AdoptionDto ToDto()` mapping, placed alongside the existing fields.

### Step 4 — AdoptionService
Threaded all four fields through `SubmitAsync` → `Adoption.Submit` and `UpdateAsync` → `UpdateDetails` using named parameters for clarity.

### Step 5 — EF Core migration
`dotnet ef migrations add AdoptionDatesAndFees` produced:
- Two nullable TEXT columns (`PreAdoptionDate`, `AdoptionDate`) — nullable so existing rows are unaffected
- Two INTEGER columns (`PreAdoptionFeeCharged`, `AdoptionFeeCharged`) with `defaultValue: false`

### Step 6 — React SPA
- `types.ts`: Added `preAdoptionDate: string | null`, `adoptionDate: string | null`, `preAdoptionFeeCharged: boolean`, `adoptionFeeCharged: boolean` to `AdoptionDto`
- `api/adoptions.ts`: Added the four fields to `create` and `update` payload types
- `AdoptionEdit.tsx`: Extended `form` state with the four fields; populated them on load using `.substring(0, 10)` to normalize ISO 8601 dates to `yyyy-MM-dd` for `<input type="date">`; added date picker inputs in a 2-col grid and checkbox inputs for fees; included all four in the submit payload

### Step 7 — Tests
Added 4 unit tests covering: submit with all four fields set, default values (null/false), update persisting fields, and clearing a date to null.
Added 3 integration tests: POST round-trip with dates and fees, PUT update of dates and fees, GET confirming default values are null/false.

## Alternatives Considered
- **Separate fee entity with amount**: Rejected — the prompt specifies boolean flags only; no amount tracking needed.
- **Named parameters vs positional**: Named parameters chosen in service layer to make the call sites self-documenting.
- **DateTime vs DateOnly**: Used `DateTime?` since EF SQLite stores dates as TEXT and the existing entity uses `DateTime` throughout. DateOnly would require a value converter.

## Environment Note
The `Microsoft.NET.Test.Sdk 17.12.*` is incompatible with .NET 10 SDK's stricter MSB3492 enforcement in `WriteLinesToFile` (`WriteOnlyWhenDifferent="true"`). Updated to `17.13.*` and used `/p:DisableMsCoverageReferencedPathMaps=true` to unblock the test build. The 12 pre-existing integration test failures are all expense-related (expense-tax-triplets feature, unrelated to this change).
