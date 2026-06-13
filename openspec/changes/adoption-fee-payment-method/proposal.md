## Why

The new fee-charged boolean fields tell staff that a fee was collected, but not how it was paid. Adding a payment method field per fee stage lets the shelter track cash vs. bank transfer vs. Bizum without a separate notes field.

## What Changes

- Add `PreAdoptionFeePaymentMethod` (`FeePaymentMethod?`, nullable) to `Adoption` entity
- Add `AdoptionFeePaymentMethod` (`FeePaymentMethod?`, nullable) to `Adoption` entity
- Add enum `FeePaymentMethod` with members `Cash`, `Bizum`, `Transfer`
- Expose both nullable fields through `AdoptionDto`, `CreateAdoptionRequest`, `UpdateAdoptionRequest`
- Update `Adoption.Submit` and `Adoption.UpdateDetails` to accept and persist the two new fields
- Render both fields as dropdown selects in `AdoptionEdit.tsx`, positioned next to their corresponding fee checkbox (displayed in Spanish: "Metálico", "Bizum", "Transferencia")
- Display both values in any adoption detail/view context
- EF Core migration: two nullable string columns (enum stored as string)

## Capabilities

### New Capabilities

- `adoption-fee-payment-method`: Two nullable `FeePaymentMethod` fields on the `Adoption` aggregate — `PreAdoptionFeePaymentMethod` and `AdoptionFeePaymentMethod` — surfaced as Spanish-labeled dropdowns in the edit form, stored as strings in the DB.

### Modified Capabilities

## Impact

- `Refugio.Domain/Entities/Adoption.cs` — new enum `FeePaymentMethod`, two properties, updated `Submit` + `UpdateDetails`
- `Refugio.Application/Contracts/AdoptionContracts.cs` — `AdoptionDto`, `CreateAdoptionRequest`, `UpdateAdoptionRequest`
- `Refugio.Application/Mapping/DtoMapping.cs` — mapping new fields
- `Refugio.Application/Services/AdoptionService.cs` — pass new fields through
- `Refugio.Infrastructure/Migrations/` — new EF Core migration (enum-as-string, no DDL change for values)
- `Refugio.Web/ClientApp/src/types.ts` — `AdoptionDto` + `FeePaymentMethod` type
- `Refugio.Web/ClientApp/src/api/adoptions.ts` — request payload types
- `Refugio.Web/ClientApp/src/pages/AdoptionEdit.tsx` — two select dropdowns
- Tests: unit + integration round-trip
