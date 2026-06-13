## 1. Domain Entity

- [x] 1.1 Add `FeePaymentMethod` enum (`Cash`, `Bizum`, `Transfer`) to `Adoption.cs` (same file, same namespace as `AdoptionType`/`AdoptionStatus`)
- [x] 1.2 Add `PreAdoptionFeePaymentMethod` (`FeePaymentMethod?`) and `AdoptionFeePaymentMethod` (`FeePaymentMethod?`) properties to `Adoption` with private setters
- [x] 1.3 Update `Adoption.Submit` factory to accept and assign both nullable payment method parameters (optional, default `null`)
- [x] 1.4 Update `Adoption.UpdateDetails` to accept and assign both nullable payment method parameters (optional, default `null`)

## 2. Application Layer

- [x] 2.1 Add `PreAdoptionFeePaymentMethod` and `AdoptionFeePaymentMethod` (`FeePaymentMethod?`) to `AdoptionDto`
- [x] 2.2 Add both fields (optional, default `null`) to `CreateAdoptionRequest` and `UpdateAdoptionRequest`
- [x] 2.3 Register `FeePaymentMethod` enum with `HasConversion<string>()` in `ShelterDbContext.OnModelCreating` for both properties
- [x] 2.4 Update `DtoMapping.ToDto` to map both payment method fields from entity to `AdoptionDto`
- [x] 2.5 Update `AdoptionService.SubmitAsync` to pass both payment method fields from request to `Adoption.Submit`
- [x] 2.6 Update `AdoptionService.UpdateAsync` to pass both payment method fields from request to `UpdateDetails`

## 3. EF Core Migration

- [x] 3.1 Run `dotnet ef migrations add AdoptionFeePaymentMethod --project src/Refugio.Infrastructure --startup-project src/Refugio.Web` and verify the migration adds two nullable TEXT columns (`PreAdoptionFeePaymentMethod`, `AdoptionFeePaymentMethod`) to the `Adoptions` table

## 4. React SPA

- [x] 4.1 Add `FeePaymentMethod` type (`'Cash' | 'Bizum' | 'Transfer'`) and update `AdoptionDto` in `types.ts` to include `preAdoptionFeePaymentMethod: FeePaymentMethod | null` and `adoptionFeePaymentMethod: FeePaymentMethod | null`
- [x] 4.2 Update `api/adoptions.ts` `create` and `update` payload types to include `preAdoptionFeePaymentMethod?: FeePaymentMethod | null` and `adoptionFeePaymentMethod?: FeePaymentMethod | null`
- [x] 4.3 Add a `PAYMENT_METHOD_LABELS` map in `AdoptionEdit.tsx`: `{ Cash: 'Metálico', Bizum: 'Bizum', Transfer: 'Transferencia' }`
- [x] 4.4 Add `preAdoptionFeePaymentMethod` and `adoptionFeePaymentMethod` to form state (initial value `null`)
- [x] 4.5 Populate both payment method fields from the existing adoption on edit load
- [x] 4.6 Add `<select>` dropdown for `preAdoptionFeePaymentMethod` next to the Pre-Adoption Fee Charged checkbox, with empty option + three Spanish-labeled options
- [x] 4.7 Add `<select>` dropdown for `adoptionFeePaymentMethod` next to the Adoption Fee Charged checkbox, with same options
- [x] 4.8 Include both payment method fields in the `adoptionsApi.update` call in `handleSubmit` (send `null` when empty option is selected)

## 5. Tests

- [x] 5.1 Add unit tests in `AdoptionServiceTests`: submit and update with payment methods set; update clearing a method to null
- [x] 5.2 Add integration tests in `AdoptionApiTests`: POST round-trip with payment methods; PUT update; GET returning null when not set
