## ADDED Requirements

### Requirement: Expense carries 1–5 IVA tax lines
An expense SHALL have between 1 and 5 tax lines. Each tax line consists of three fields: `IvaPercent` (rate, decimal ≥ 0), `Base` (taxable amount, decimal > 0), and `Importe` (tax amount, decimal ≥ 0). Attempting to create or update an expense with 0 or more than 5 tax lines SHALL be rejected with a validation error.

#### Scenario: Create expense with valid single tax line
- **WHEN** user submits a new expense with one tax line where IvaPercent=21, Base=100.00, Importe=21.00
- **THEN** expense is persisted with one tax line and the API returns 201 with `taxLines` containing that triplet

#### Scenario: Create expense with no tax lines rejected
- **WHEN** user submits a new expense with an empty tax lines list
- **THEN** the API returns 400 and the expense is not created

#### Scenario: Create expense with more than 5 tax lines rejected
- **WHEN** user submits a new expense with 6 tax lines
- **THEN** the API returns 400 and the expense is not created

### Requirement: IVA math invariant enforced
Each tax line SHALL satisfy `round(IvaPercent / 100 × Base, 2) == Importe`. The domain SHALL reject any tax line where this invariant does not hold.

#### Scenario: Tax line with correct math accepted
- **WHEN** IvaPercent=10, Base=200.00, Importe=20.00
- **THEN** tax line is valid and accepted

#### Scenario: Tax line with wrong Importe rejected
- **WHEN** IvaPercent=21, Base=100.00, Importe=22.00
- **THEN** domain throws a validation error and the expense is not saved

#### Scenario: Tax line with IvaPercent=0 and Importe=0 accepted
- **WHEN** IvaPercent=0, Base=50.00, Importe=0.00
- **THEN** tax line is valid (zero-rate VAT)

### Requirement: Expense form shows inline tax line editor
The `ExpenseEdit` page SHALL render a table of tax line rows inline in the expense form. The user SHALL be able to add a row (up to 5), remove any row (down to 1), and edit IvaPercent, Base, and Importe fields per row. The form SHALL validate the IVA math per row before submission and display an error if the invariant fails. No separate modal or sub-form SHALL be used.

#### Scenario: User adds a tax line row
- **WHEN** user clicks "Add line" and the current row count is below 5
- **THEN** a new empty row appears in the tax lines table

#### Scenario: Add button disabled at 5 rows
- **WHEN** 5 tax lines already exist
- **THEN** the "Add line" button is disabled

#### Scenario: User removes a tax line row
- **WHEN** user clicks the remove button on a row and more than 1 row exists
- **THEN** that row is removed from the table

#### Scenario: Remove button disabled at 1 row
- **WHEN** only 1 tax line row exists
- **THEN** the remove button for that row is disabled

#### Scenario: Math validation error shown inline
- **WHEN** a row has IvaPercent=21, Base=100.00, Importe=22.00 and user submits
- **THEN** the form shows a per-row validation error "Importe must equal IvaPercent% × Base" and does not submit

### Requirement: Expense read returns tax lines
`GET /api/expenses/{id}` SHALL include a `taxLines` array in the response. Each element SHALL contain `id`, `ivaPercent`, `base`, and `importe`.

#### Scenario: Fetch expense includes tax lines
- **WHEN** client calls GET /api/expenses/{id} for an expense with tax lines
- **THEN** response body contains `taxLines` array with the correct triplets

### Requirement: Update expense replaces tax lines
`PUT /api/expenses/{id}` SHALL accept a `taxLines` array and replace all existing tax lines for that expense atomically.

#### Scenario: Update replaces all tax lines
- **WHEN** expense has two tax lines and PUT sends one new tax line
- **THEN** after the call, the expense has exactly one tax line matching the new data
