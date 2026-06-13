## Why

Adoptions need to track key milestone dates (pre-adoption handoff and formal adoption) and whether fees were charged at each stage. Currently the `Adoption` entity captures no date or fee information beyond the application timestamp, leaving staff unable to report on timelines or fee collection.

## What Changes

- Add `PreAdoptionDate` (`DateTime?`) to `Adoption` entity
- Add `AdoptionDate` (`DateTime?`) to `Adoption` entity
- Add `PreAdoptionFeeCharged` (`bool`) to `Adoption` entity
- Add `AdoptionFeeCharged` (`bool`) to `Adoption` entity
- Expose all four fields through `AdoptionDto`, `CreateAdoptionRequest`, `UpdateAdoptionRequest`
- Update `Adoption.Submit` and `Adoption.UpdateDetails` to accept and persist the four new fields
- Render all four fields in the `AdoptionEdit` React form (date pickers + checkboxes)
- Display all four fields in the `AdoptionEdit` view / detail view
- EF Core migration to add the four columns

## Capabilities

### New Capabilities

- `adoption-dates-and-fees`: Track pre-adoption date, adoption date, pre-adoption fee charged flag, and adoption fee charged flag on the Adoption aggregate — surfaced in all forms and views.

### Modified Capabilities

## Impact

- `Refugio.Domain/Entities/Adoption.cs` — entity fields + `Submit` factory + `UpdateDetails` behavior
- `Refugio.Application/Contracts/AdoptionContracts.cs` — request records + `AdoptionDto`
- `Refugio.Application/Mapping/DtoMapping.cs` — mapping new fields
- `Refugio.Application/Services/AdoptionService.cs` — pass new fields through
- `Refugio.Infrastructure/Migrations/` — new EF Core migration
- `Refugio.Web/ClientApp/src/types.ts` — `AdoptionDto` interface
- `Refugio.Web/ClientApp/src/api/adoptions.ts` — request payloads
- `Refugio.Web/ClientApp/src/pages/AdoptionEdit.tsx` — form inputs
- Tests: `DogsApiTests.cs` / `AdoptionApiTests.cs` + unit service tests updated
