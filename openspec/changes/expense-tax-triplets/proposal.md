## Why

Spanish accounting requires VAT (IVA) breakdowns on expenses — each expense line must declare the tax base, tax rate, and computed amount. Without this, generated records can't be used for fiscal reporting.

## What Changes

- Add `ExpenseTaxLine` child entity to `Expense` aggregate (IvaPercent, Base, Importe)
- Extend `Expense.Record` / `Expense.Update` to accept a list of tax lines (1–5 required)
- Add EF migration for the new `ExpenseTaxLines` table
- Extend `ExpenseDto` / `CreateExpenseRequest` / `UpdateExpenseRequest` contracts with `taxLines`
- Update finance endpoints to pass tax lines through the actor to the service
- Render in-place triplet editor in `ExpenseEdit.tsx` (add/remove rows, inline validation)

## Capabilities

### New Capabilities

- `expense-tax-lines`: In-place CRUD of 1–5 IVA triplets on the expense form, with validation that `round(IvaPercent/100 * Base, 2) == Importe`

### Modified Capabilities

- (none)

## Impact

- **Domain**: `Expense`, new `ExpenseTaxLine` entity
- **Application**: `IFinanceService` + `FinanceService` (create/update), `Contracts/` (request records + DTO)
- **Infrastructure**: EF migration, `DtoMapping`
- **Actors**: `FinanceActor` messages that carry the request records (inherit new fields automatically)
- **Web**: `FinanceEndpoints` (expense create/update), `ClientApp/src/pages/ExpenseEdit.tsx`, `ClientApp/src/types.ts`, `ClientApp/src/api/finance.ts`
