# Reasoning: Adoption Fee Payment Method

## Problem
`PreAdoptionFeeCharged` and `AdoptionFeeCharged` booleans record that a fee was collected but not how. Staff need to track the payment channel (cash, Bizum, bank transfer) per fee stage.

## Steps and Decisions

### 1. Enum in the same file
`FeePaymentMethod { Cash, Bizum, Transfer }` added at the bottom of `Adoption.cs` alongside `AdoptionType` and `AdoptionStatus`. Keeps adoption-specific enums co-located. Namespace stays `Refugio.Domain.Entities` (EF snapshot key constraint).

### 2. Nullable, no cross-field validation
Both properties are `FeePaymentMethod?`. A fee may be charged without recording the method (or not charged at all). No domain validation enforcing "if charged then method required" — spec says optional, independent fields.

### 3. Enum stored as string — `HasConversion<string>()`
Project convention for all enums. Two `HasConversion<string>()` lines added to `ShelterDbContext.OnModelCreating`. Migration generates two nullable TEXT columns.

### 4. All layers threaded with optional parameters
`Submit` and `UpdateDetails` gain two trailing optional params (default `null`) so existing callers — including test seeds — don't break. Contracts and service pass them through.

### 5. React: hardcoded Spanish labels
No localization system. Label map `{ Cash: 'Metálico', Bizum: 'Bizum', Transfer: 'Transferencia' }` defined as a module-level constant in `AdoptionEdit.tsx`. Dropdowns placed inline next to their corresponding checkbox in a flex row. Empty option maps to `null` on submit.

### Alternatives Rejected
- **Free-text string field**: No constraint on values, no type safety in TypeScript, harder to enumerate options.
- **Non-nullable with a `None` member**: Adds noise to all existing and future records; null already expresses "not set."
- **Separate `PaymentMethod` entity**: Overkill for three fixed values.
