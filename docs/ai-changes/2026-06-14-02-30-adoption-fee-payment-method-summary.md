# Summary: Adoption Fee Payment Method

## What Changed

| Area | Change |
|---|---|
| `Adoption.cs` | Added `FeePaymentMethod` enum (`Cash`, `Bizum`, `Transfer`); added `PreAdoptionFeePaymentMethod?` and `AdoptionFeePaymentMethod?` props; updated `Submit` + `UpdateDetails` |
| `AdoptionContracts.cs` | Added both fields to `AdoptionDto`, `CreateAdoptionRequest`, `UpdateAdoptionRequest` |
| `ShelterDbContext.cs` | Two `HasConversion<string>()` registrations for the new properties |
| `DtoMapping.cs` | Maps both fields in `AdoptionDto ToDto` |
| `AdoptionService.cs` | Threads both fields through `SubmitAsync` and `UpdateAsync` |
| Migration | `20260613232521_AdoptionFeePaymentMethod` — two nullable TEXT columns on `Adoptions` |
| `types.ts` | `FeePaymentMethod` type; two nullable fields on `AdoptionDto` |
| `api/adoptions.ts` | `create` and `update` payloads include both optional fields |
| `AdoptionEdit.tsx` | `PAYMENT_METHOD_LABELS` + `PAYMENT_METHODS` constants; form state; populate on load; `<select>` dropdowns with Spanish labels; submit includes both fields |
| Unit tests | 4 new tests: submit with methods, default null, update persists, clear to null |
| Integration tests | 3 new tests: POST round-trip, PUT update, GET null when unset |

## Why
Staff needed to record how each fee was paid (cash / Bizum / bank transfer) alongside the existing "fee charged" checkboxes. Independent, nullable — no enforcement between the two fields.

## Side Effects / Follow-up
- Existing adoptions in the DB will have NULL for both columns — expected.
- `FeePaymentMethod` member names (`Cash`, `Bizum`, `Transfer`) are now permanent API/DB identifiers.
- The `Adoptions` list page (`Adoptions.tsx`) and any detail view do not display the payment method — they were not in scope. Add if needed.

## Code Review Checklist
- [ ] `FeePaymentMethod` enum in correct namespace (`Refugio.Domain.Entities`)
- [ ] Both `HasConversion<string>()` lines present in `ShelterDbContext.OnModelCreating`
- [ ] Migration file: two nullable TEXT columns, no default value
- [ ] `AdoptionDto` positional parameters order: payment methods come after fee-charged booleans, before dates
- [ ] `AdoptionEdit.tsx`: empty option value is `""` → converts to `null` on submit
- [ ] Unit tests cover: set, default null, update, clear to null
- [ ] Integration tests cover: POST, PUT, GET null
