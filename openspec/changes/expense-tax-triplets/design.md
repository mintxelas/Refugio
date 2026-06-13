## Context

Expenses in Refugio need VAT (IVA) breakdown lines for Spanish fiscal compliance. Each expense can have 1–5 tax triplets: `IvaPercent` (rate), `Base` (taxable amount), `Importe` (tax amount = IvaPercent% × Base). Currently `Expense` has no such structure.

## Goals / Non-Goals

**Goals:**
- Add `ExpenseTaxLine` child entity to `Expense` aggregate
- Enforce 1–5 triplets and math invariant at domain and API level
- Surface inline triplet editor in `ExpenseEdit.tsx` (no modal, no separate form)
- Migrate DB schema and update all contract/mapping layers

**Non-Goals:**
- Summing tax lines into `Expense.Amount` automatically — amount stays user-controlled
- Supporting fractional IVA percentages beyond 2 decimal places
- CSV export changes

## Decisions

### 1. `ExpenseTaxLine` as a child entity (not a value object)
EF Core can't own a collection of value objects without a backing table. `ExpenseTaxLine` extends `Entity` for an `Id` PK — same pattern as `ExpensePhoto`. EF navigates via `Expense.TaxLines`.

**Alternative:** JSON column. Rejected — harder to query and breaks the project's EF migration pattern.

### 2. Replace-all sync on update
On `UpdateExpense`, the service removes all existing `ExpenseTaxLine` rows for the expense and inserts the new list. This avoids diffing and is safe because tax lines have no independent identity beyond their parent expense.

**Alternative:** Diff/patch. Rejected — over-engineered for 1–5 rows with no external references.

### 3. Validation: domain + API boundary
Math invariant (`round(ivaPercent / 100 × base, 2) == importe`) checked in `ExpenseTaxLine.Create` (throws `ArgumentException`) and in the React form before submit (inline feedback per row).  
Count constraint (1–5) enforced in `Expense.SetTaxLines`.

### 4. React: controlled-row table, no form element nesting
Triplet rows are part of the expense `<form>`. Each row has three `<input>` fields. Importe is user-editable (not auto-computed) so the user can enter the exact fiscal amount and the validation confirms the math, matching the UX of invoice software.

**Alternative:** Auto-compute Importe. Rejected — fiscal amounts must match source documents exactly; rounding must be explicit.

### 5. `ISoftDeletable` — not on `ExpenseTaxLine`
Tax lines live and die with their parent `Expense`. No soft delete, no global filter needed. `ExpenseTaxLine` does NOT extend `Entity` (which carries `DeletedAt`); it gets a minimal base class with just `Id`.

Actually, looking at the codebase: `Entity` in `Refugio.Domain.Common` carries `DeletedAt` and domain events. For `ExpenseTaxLine` we still extend `Entity` (same as `ExpensePhoto`) — the EF global filter only applies to types that `ISoftDeletable` is configured for in `ShelterDbContext`. Since `ExpenseTaxLine` won't be registered with a global filter, soft-delete behavior won't apply.

## Risks / Trade-offs

- **Migration on existing data** → Expenses with no tax lines violate the 1–5 min constraint. The constraint is enforced only at the API/domain layer on new create/update calls; existing rows without tax lines remain valid in DB. No backfill needed.
- **Replace-all sync** → If two concurrent requests update the same expense's tax lines, the last write wins. Acceptable given single-user shelter context.
- **Importe user-editable** → User could enter a wrong value and bypass validation. The validation check is the guard; trust the check.

## Migration Plan

1. Add `ExpenseTaxLine` entity class
2. Add `TaxLines` navigation + `SetTaxLines` to `Expense`
3. Add EF config in `ShelterDbContext`
4. `dotnet ef migrations add AddExpenseTaxLines`
5. Update contracts, mapping, service, endpoints, React types + API client + form
6. Run all tests
