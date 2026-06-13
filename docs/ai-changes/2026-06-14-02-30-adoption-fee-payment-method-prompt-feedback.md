# Prompt Feedback: Adoption Fee Payment Method

## Original Prompt
> By the Pre-Adoption Fee form field, add a dropdown list of values [cash, bizum, transfer]. Same for Adoption Fee. These new fields say how the fee was paid. In spanish, use the values [metalico, bizum, transferencia] for the options. Update both the entity, forms and views related to adoptions.

## What Worked
- Clear placement ("by the Pre-Adoption Fee form field").
- Explicit option values and their Spanish display labels.
- "Update both the entity, forms and views" set the scope.

## What Could Be Improved

### 1. Distinguish values from labels
The prompt lists `[cash, bizum, transfer]` as "values" and `[metalico, bizum, transferencia]` as the Spanish labels. But it's ambiguous whether `cash/bizum/transfer` are the C# enum member names (API identifiers) or just descriptions. Clarifying "use `Cash`, `Bizum`, `Transfer` as the enum member names (the API/DB identifiers)" avoids guessing.

**Improved**: "Add a `FeePaymentMethod` enum with members `Cash`, `Bizum`, `Transfer` (these are the permanent API identifiers). Display them in Spanish in the form: Cash → 'Metálico', Bizum → 'Bizum', Transfer → 'Transferencia'."

### 2. Specify nullability
The prompt doesn't say whether the field is required or optional. A null/"not set" option in the dropdown was inferred. Stating "optional, nullable — staff can leave it blank" would confirm the intent.

### 3. "Views" is ambiguous
"Forms and views related to adoptions" could include the adoption list page and a detail view. The change only touched `AdoptionEdit.tsx` (the edit form). Clarify: "Add the dropdowns to the edit form only. Display the value read-only in the list or detail view if one exists."

### 4. Mention tests
No explicit test requirement. Adding "include unit and integration tests for the new fields" ensures coverage is part of the change.

**Improved version of the full prompt**:
> Add two nullable `FeePaymentMethod` fields to the `Adoption` entity: `PreAdoptionFeePaymentMethod` and `AdoptionFeePaymentMethod`. Define a `FeePaymentMethod` C# enum with members `Cash`, `Bizum`, `Transfer` (these are the permanent API/DB identifiers, stored as strings). In the `AdoptionEdit` React form, place a `<select>` dropdown next to each fee-charged checkbox. Options: an empty/unset option, then Metálico (Cash), Bizum (Bizum), Transferencia (Transfer). Fields are optional — selecting the empty option sends null. Wire through contracts, EF migration, and add unit + integration tests.
