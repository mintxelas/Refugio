## 1. Domain Entity

- [x] 1.1 Add `PreAdoptionDate` (`DateTime?`), `AdoptionDate` (`DateTime?`), `PreAdoptionFeeCharged` (`bool`), `AdoptionFeeCharged` (`bool`) properties to `Adoption` entity with private setters
- [x] 1.2 Update `Adoption.Submit` factory to accept and assign the four new parameters (all optional, defaults `null`/`false`)
- [x] 1.3 Update `Adoption.UpdateDetails` to accept and assign the four new parameters

## 2. Application Layer

- [x] 2.1 Add the four fields to `AdoptionDto` in `AdoptionContracts.cs`
- [x] 2.2 Add the four fields to `CreateAdoptionRequest` and `UpdateAdoptionRequest`
- [x] 2.3 Update `DtoMapping` to map the four entity fields to `AdoptionDto`
- [x] 2.4 Update `AdoptionService.CreateAdoption` to pass the four new fields from the request to `Adoption.Submit`
- [x] 2.5 Update `AdoptionService.UpdateAdoption` to pass the four new fields from the request to `UpdateDetails`

## 3. EF Core Migration

- [x] 3.1 Run `dotnet ef migrations add AdoptionDatesAndFees --project src/Refugio.Infrastructure --startup-project src/Refugio.Web` and verify the generated migration adds four columns correctly (two nullable datetime, two bit/bool with default 0)

## 4. React SPA

- [x] 4.1 Update `AdoptionDto` interface in `types.ts` with `preAdoptionDate`, `adoptionDate` (`string | null`), `preAdoptionFeeCharged`, `adoptionFeeCharged` (`boolean`)
- [x] 4.2 Update the adoption API client (`api/adoptions.ts`) create/update request payload types and calls to include the four fields
- [x] 4.3 Add date picker inputs (`<input type="date">`) for `preAdoptionDate` and `adoptionDate` in `AdoptionEdit.tsx`
- [x] 4.4 Add checkbox inputs for `preAdoptionFeeCharged` and `adoptionFeeCharged` in `AdoptionEdit.tsx`
- [x] 4.5 Populate form state from the existing adoption on edit (all four fields)

## 5. Tests

- [x] 5.1 Add unit test in `AdoptionServiceTests` (or equivalent) covering create and update with all four new fields set
- [x] 5.2 Add/update integration test in `AdoptionApiTests` to assert the four fields round-trip through POST and PUT endpoints and are returned in GET
