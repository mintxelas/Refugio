# Reasoning: Expense IVA Tax Triplets

## Problem
Spanish accounting requires VAT (IVA) breakdown lines on expenses. The `Expense` aggregate had no such structure.

## Step-by-step logic

### 1. Domain entity design
- Added `ExpenseTaxLine` as a child entity of `Expense` (not a value object) because EF Core requires a backing table for collections, and the project uses the same pattern for `ExpensePhoto`.
- Math invariant `round(IvaPercent/100 × Base, 2) == Importe` enforced in `ExpenseTaxLine.Create` factory — fail fast at the domain boundary.
- `Expense.SetTaxLines` mutates the existing `TaxLines` collection in-place (`Clear()` + `Add`) rather than replacing the list reference. This is critical: EF Core tracks collection item additions/removals; replacing the list reference loses tracking of removed items.

### 2. EF cascade
- `HasMany/WithOne` with `OnDelete(Cascade)` ensures tax lines are deleted when the expense is deleted (soft or purge).
- No soft delete global filter on `ExpenseTaxLine` — lines are only accessed through their parent expense; the parent's filter is sufficient.

### 3. Repository override
- `EfRepository<T>.GetAsync` uses `FindAsync` which doesn't load navigation properties.
- `ExpenseRepository.GetAsync` overridden to use `Include(e => e.TaxLines)` + `FirstOrDefaultAsync` so update operations can see the existing lines before calling `SetTaxLines`.

### 4. Service update strategy
- Replace-all: existing tax lines are cleared via `SetTaxLines` (which calls `TaxLines.Clear()`), EF detects orphans and deletes them on save.
- Single commit point — no extra repo method needed.

### 5. React UI
- Controlled row array `TaxRow[]` in state; "Importe is user-editable" matches Spanish invoice workflow where amounts must match source documents exactly.
- Validation on submit (not on blur) to avoid annoyingly early errors while typing.
- `rowErrors` parallel array tracks per-row error strings; displayed under the table.

## Alternatives considered

- **Auto-compute Importe** — rejected; fiscal amounts must match source docs exactly.
- **JSON column for tax lines** — rejected; breaks EF migration pattern, harder to query.
- **Diff/patch sync on update** — rejected; overkill for 1–5 rows with no external references.
- **Value object** — rejected; EF Core can't own a mutable collection of value objects without a backing table.
