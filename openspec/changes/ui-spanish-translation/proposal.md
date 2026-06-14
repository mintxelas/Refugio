## Why

The shelter operates entirely in Spanish and all staff are Spanish speakers. Every UI string is currently hardcoded in English, creating friction for daily use.

## What Changes

- Translate all hardcoded UI strings in React pages, components, and navigation to Spanish
- Replace raw enum member names rendered in the UI (e.g. "Applied", "Available", "Monthly") with Spanish display labels via label maps
- C# enum member names stay unchanged — they are permanent API/DB identifiers stored as strings; only their UI display changes
- No localization framework is introduced — Spanish strings are hardcoded directly (project convention)
- No English fallback — the frontend targets Spanish only

## Capabilities

### New Capabilities

- `ui-spanish-translation`: All user-visible text in the React SPA is Spanish. This covers: navigation labels (Sidebar), page headings, form labels, button text, placeholder text, error and confirmation messages, table/list column headers, status chips (DogStatus, AdoptionStatus, VolunteerStatus, DonationCategory, ExpenseCategory, FeePaymentMethod display labels), and any other hardcoded English strings in JSX.

### Modified Capabilities

## Impact

- `src/Refugio.Web/ClientApp/src/components/Sidebar.tsx` — nav labels
- `src/Refugio.Web/ClientApp/src/components/StatusChip.tsx` — Spanish label maps + renders label instead of raw enum name
- `src/Refugio.Web/ClientApp/src/components/TopBar.tsx` — any English text
- `src/Refugio.Web/ClientApp/src/components/Pagination.tsx` — any English text
- `src/Refugio.Web/ClientApp/src/pages/*.tsx` — all 23 page files (headings, labels, buttons, placeholders, confirmations, error messages)
- No backend changes — C# enum names and API contracts unchanged
- No database migration required
