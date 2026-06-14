## 1. Shared Label Maps

- [x] 1.1 Create `src/Refugio.Web/ClientApp/src/labels.ts` with exhaustive `Record<EnumType, string>` maps for: `DogStatus`, `AdoptionStatus`, `AdoptionType`, `VolunteerStatus`, `DonationCategory`, `ExpenseCategory`, `FeePaymentMethod` — all values in Spanish
- [x] 1.2 Update `StatusChip.tsx` to import label maps from `labels.ts` and render the Spanish label instead of the raw enum name
- [x] 1.3 Remove `PAYMENT_METHOD_LABELS` and `PAYMENT_METHODS` constants from `AdoptionEdit.tsx` and import them from `labels.ts` instead

## 2. Shared Components

- [x] 2.1 Translate `Sidebar.tsx` — nav item labels: Dashboard→Inicio, Dogs→Perros, Health→Salud, Adoptions→Adopciones, Funds→Fondos, Calendar→Calendario, Volunteers→Voluntarios, Reports→Informes, Admin / Deleted→Admin / Eliminados, Settings→Configuración; button "Check In Dog"→"Registrar Perro"
- [x] 2.2 Translate `TopBar.tsx` — any English text (logout, user info labels)
- [x] 2.3 Translate `Pagination.tsx` — any English text (page indicators, next/prev labels)
- [x] 2.4 Translate `ErrorMessage.tsx` — any English text
- [x] 2.5 Translate `LoadingSpinner.tsx` — any English text

## 3. Auth Pages

- [x] 3.1 Translate `Login.tsx` — labels, button, error messages
- [x] 3.2 Translate `ChangePassword.tsx` — labels, button, error messages, success message

## 4. Dog Pages

- [x] 4.1 Translate `DogCheckin.tsx` — all labels, buttons, placeholders, confirmations, error messages; use `DogStatus` label map for status dropdown options
- [x] 4.2 Translate `DogEdit.tsx` — all labels, buttons, placeholders, confirmations, error messages; use `DogStatus` label map for status dropdown options
- [x] 4.3 Translate `DogDetail.tsx` — all headings, labels, field names, button text
- [x] 4.4 Translate `Dogs.tsx` — page heading, search/filter labels, table headers, empty state text; use `DogStatus` label map for filter options
- [x] 4.5 Translate `MedicalRecordEdit.tsx` — all labels, buttons, placeholders, error messages
- [x] 4.6 Translate `MedicationEdit.tsx` — all labels, buttons, placeholders, error messages
- [x] 4.7 Translate `Health.tsx` — headings, table headers, empty state, any filter labels

## 5. Adoption Pages

- [x] 5.1 Translate `Adoptions.tsx` — heading, filter labels, table headers, empty state; use `AdoptionStatus` and `AdoptionType` label maps for displayed values
- [x] 5.2 Translate `AdoptionEdit.tsx` — all labels, buttons, placeholders, confirmations, error messages; use `AdoptionStatus` and `AdoptionType` label maps for dropdowns; import `FeePaymentMethod` labels from `labels.ts`

## 6. Finance Pages

- [x] 6.1 Translate `Funds.tsx` — headings, tab labels, column headers, empty state, button text; use `DonationCategory` and `ExpenseCategory` label maps
- [x] 6.2 Translate `DonationEdit.tsx` — all labels, buttons, placeholders, error messages; use `DonationCategory` label map for category dropdown
- [x] 6.3 Translate `ExpenseEdit.tsx` — all labels, buttons, placeholders, error messages; use `ExpenseCategory` label map for category dropdown
- [x] 6.4 Translate `GoalEdit.tsx` — all labels, buttons, placeholders, error messages

## 7. Other Pages

- [x] 7.1 Translate `Calendar.tsx` — headings, labels, button text, empty state
- [x] 7.2 Translate `EventEdit.tsx` — all labels, buttons, placeholders, error messages
- [x] 7.3 Translate `Volunteers.tsx` — heading, column headers, empty state; use `VolunteerStatus` label map
- [x] 7.4 Translate `VolunteerEdit.tsx` — all labels, buttons, placeholders, error messages; use `VolunteerStatus` label map for status dropdown
- [x] 7.5 Translate `Reports.tsx` — headings, section labels, data field names
- [x] 7.6 Translate `Settings.tsx` — all labels, buttons, placeholders, error/success messages
- [x] 7.7 Translate `AdminDeleted.tsx` — headings, tab labels, column headers, restore/purge button text, confirmation dialogs, empty state
- [x] 7.8 Translate `Home.tsx` — dashboard section headings, stat labels, empty state messages

## 8. Verify

- [x] 8.1 Grep for common remaining English strings (`Save`, `Cancel`, `Delete`, `Error`, `Loading`, `Search`, `Filter`, `Edit`, `Add`, `New`, `Back`, `Confirm`) across `ClientApp/src/pages/` and `ClientApp/src/components/` and fix any found
- [x] 8.2 Build the React SPA (`npm run build`) with no TypeScript errors — exhaustive `Record<EnumType, string>` maps will catch any missing enum members at compile time
