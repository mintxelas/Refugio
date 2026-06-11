# Feature: Finance

Donations, expenses (with receipt photo gallery), and fundraising goals, plus a yearly summary and CSV exports. All handled by one application service (`IFinanceService`) across three aggregates.

## Entities

### Donation (aggregate root)

| Property | Type | Notes |
|---|---|---|
| `DonorName` | string | required |
| `Amount` | decimal | |
| `Date` | DateTime | defaults to UTC now |
| `Category` | `DonationCategory` | default `OneTime` |
| `Notes` | string? | |

**Enum `DonationCategory`:** `Monthly`, `OneTime`, `InKind`, `Corporate`.
**Behaviors:** `Donation.Record(...)`, `Update(...)`.

### Expense (aggregate root)

| Property | Type | Notes |
|---|---|---|
| `Description` | string | required |
| `Amount` | decimal | |
| `Date` | DateTime | defaults to UTC now |
| `Category` | `ExpenseCategory` | default `Other` |
| `Notes` | string? | |
| `Photos` | collection | receipt images |

**Enum `ExpenseCategory`:** `Medical`, `Food`, `Facilities`, `Supplies`, `Transport`, `Other`.
**Behaviors:** `Expense.Record(...)`, `Update(...)`.

### ExpensePhoto (child of Expense)

| Property | Type | Notes |
|---|---|---|
| `ExpenseId` | int | parent |
| `Url` | string | `/expenses/{file}` under wwwroot |
| `UploadedAt` | DateTime | UTC now |

No default-photo concept (unlike `DogPhoto`).

### Goal (aggregate root)

| Property | Type | Notes |
|---|---|---|
| `Title` | string | required |
| `Description` | string? | |
| `TargetAmount` | decimal | |
| `CurrentAmount` | decimal | manually maintained |
| `Deadline` | DateTime? | |
| `CreatedAt` | DateTime | UTC now |

**Behaviors:** `Goal.Create(...)`, `Update(...)`.

## Use cases

### UC-F1: Browse finances
- **UI:** `/funds` — tabs: donations / expenses / goals / summary; pagination; CSV export links.
- **API:** `GET /api/donations[/paged]`, `GET /api/expenses[/paged]` (default 25/page), `GET /api/goals`, `GET /api/donations/{id}`, `GET /api/expenses/{id}`, `GET /api/goals/{id}`.

### UC-F2: Record a donation / expense
- **API:** `POST /api/donations`, `POST /api/expenses` → 201.

### UC-F3: Edit donation / expense / goal
- **UI:** `/funds/donations/{id}`, `/funds/expenses/{id}` (with receipt gallery), `/funds/goals/{id}`.
- **API:** `PUT /api/donations/{id}`, `PUT /api/expenses/{id}`, `PUT /api/goals/{id}` → 200/404.

### UC-F4: Create a fundraising goal
- **API:** `POST /api/goals` → 201.
- **Note:** dashboard donation-goal percentage guards division by zero when no goals exist.

### UC-F5: Receipt photo gallery
- **API:** `GET /api/expenses/{id}/photos`; `POST /api/expenses/{id}/photos` (multi-file `Photos`, `.jpg/.jpeg/.png/.webp`, max 5 MB each, names `{expenseId}_{guid}.{ext}`, redirect to expense edit); `POST /api/expenses/photos/{photoId}/delete?expenseId=` (removes record + file).

### UC-F6: Delete / restore / purge (Manager)
- Same shape for all three aggregates: REST `DELETE /api/{donations|expenses|goals}/{id}` (204/404); UI `POST .../{id}/delete` (redirect `/funds` or `/funds?tab=goals`); `/restore`, `/purge` redirect to `/admin/deleted?tab={donations|expenses|goals}`. Deleted listings: `GET /api/{area}/deleted[/{id}]`.

### UC-F7: Finance summary
- **API:** `GET /api/finances/summary?year=` → `FinanceSummary` read model (`IFinanceQueries`, defaults to current year). Shown in the `/funds` summary tab.

### UC-F8: CSV exports (auth)
- `GET /api/export/donations` — Date, Donor Name, Category, Amount, Notes.
- `GET /api/export/expenses` — Date, Description, Category, Amount, Notes.
- Quoting: fields containing commas/quotes/newlines are double-quoted with `""` escaping; file name `{type}-{yyyy-MM-dd}.csv`.
