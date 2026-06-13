# Summary: Add TaxId to Donation

## What changed

| File | Change |
|---|---|
| `Refugio.Domain/Entities/Donation.cs` | Added `TaxId` property; updated `Record` and `Update` |
| `Refugio.Application/Contracts/FinanceContracts.cs` | Added `TaxId?` to `DonationDto`, `CreateDonationRequest`, `UpdateDonationRequest` |
| `Refugio.Application/Mapping/DtoMapping.cs` | `ToDto` passes `donation.TaxId` |
| `Refugio.Application/Services/FinanceService.cs` | `RecordDonationAsync` and `UpdateDonationAsync` forward `TaxId` |
| `Refugio.Web/Endpoints/FinanceEndpoints.cs` | CSV export includes `Tax ID` column |
| `Refugio.Web/Components/Pages/DonationEdit.razor` | Added TaxId input field and form read |
| `SharedResources*.resx` (×4) | Added `Funds_TaxId` key |
| `Migrations/…_AddDonationTaxId.cs` | Nullable `TaxId TEXT` column on `Donations` |

## Why

Tax ID is commonly needed for donation receipts and financial reporting (EIN, VAT, CNPJ, NIF/CIF depending on jurisdiction).

## Side effects / follow-up

- Existing donations have `TaxId = NULL` — no data migration needed.
- CSV export gains a new column; downstream consumers of the export file should be aware.
- No API contract break: `DonationDto.TaxId` is a trailing optional record param (null by default).

## Review checklist

- [ ] `Funds_TaxId` present in all 4 RESX files
- [ ] Migration adds nullable column (not NOT NULL without default)
- [ ] `Update` call passes TaxId (not silently dropped)
- [ ] CSV header and row both updated consistently
