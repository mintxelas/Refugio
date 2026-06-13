# Prompt Feedback: Expense IVA Tax Triplets

## Original prompt
> Enrich the expenses form with a list of triplets: IVA (percentage) Importe (money amount) Base (money amount) with a validation that (IVA * Base)==Importe. There can be 1 to 5 triplets. The CRUD of the triplets must be in-place, with no other forms.

## What was clear
- The three fields and their names (IVA, Base, Importe)
- The math invariant
- The 1–5 count constraint
- In-place editing requirement (no modal/separate form)

## What required inference
- **Is Importe auto-computed or user-entered?** Decided user-entered (match source documents). Prompt says "validation that (IVA * Base)==Importe" which implies both sides are data — user enters all three and we validate.
- **Are triplets persisted server-side or only UI state?** Inferred server-side persistence (they must survive page reload).
- **Does changing tax lines affect the parent `Amount` field?** Inferred no (amount stays independent).
- **Should triplets be required for create?** Inferred yes (1–5 min).

## How to improve the prompt
1. **Clarify Importe entry mode**: "User enters all three values and we validate the math" vs "IVA% and Base are entered; Importe is auto-computed and read-only."
2. **Clarify persistence scope**: "Persist tax lines to the database" vs "UI-only, not stored."
3. **State whether existing expenses need migration**: "Existing expenses should be migrated with default tax lines" or "constraint applies only to new/updated expenses."
4. **Specify rounding rule**: "Round to 2 decimal places" (implied by money, but explicit is better).

## Suggested improved prompt
> Add 1–5 IVA tax breakdown lines to the expense create/edit form. Each line has three fields: IvaPercent (decimal ≥ 0), Base (taxable amount, > 0), and Importe (tax amount). The user enters all three values. On save, validate that round(IvaPercent / 100 × Base, 2) == Importe for each line. Persist the lines to the database — they must survive page reload. The CRUD of lines must be inline in the form (no modal/sub-form). Lines are required: at least 1, at most 5. Existing expenses in the DB do not need migration.
