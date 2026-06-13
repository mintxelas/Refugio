# Reasoning: Add TaxId to Donation

## Steps

1. **Read domain entity** — `Donation.cs` had `Notes` as the last optional field; added `TaxId` after it with `private set`.
2. **Updated `Record` factory** — added optional `string? taxId = null` parameter (kept last, after `date`, to avoid breaking existing call sites with positional args).
3. **Updated `Update` method** — added `taxId` param; all callers pass it explicitly.
4. **Contracts** — `DonationDto` got `string? TaxId = null` as trailing optional (preserves positional deserialization for existing clients). `CreateDonationRequest` and `UpdateDonationRequest` each got `string? TaxId = null`.
5. **Mapping** — `ToDto` extended to pass `donation.TaxId`.
6. **Service** — `RecordDonationAsync` and `UpdateDonationAsync` now forward `request.TaxId`.
7. **CSV export** — added `Tax ID` column to header and row projection.
8. **Blazor page** — added `TaxId` input field in the 2-column grid; added `FTaxId` constant; `Save` handler reads and forwards it.
9. **Localization** — added `Funds_TaxId` to all 4 RESX files with culture-appropriate labels.
10. **EF migration** — `AddDonationTaxId` adds nullable `TEXT` column `TaxId` to `Donations`.

## Alternatives considered

- **Required TaxId**: rejected — not every donor has one; optional matches real-world usage.
- **Max-length constraint**: skipped — formats vary widely (EIN, VAT, CNPJ, etc.); free TEXT is correct.
- **Separate TaxInfo entity**: overkill for a single field.
