## ADDED Requirements

### Requirement: All UI text is in Spanish
The React SPA SHALL render all user-visible text in Spanish. No English strings SHALL appear in labels, buttons, headings, placeholders, error messages, confirmation dialogs, or navigation items.

#### Scenario: Navigation sidebar shows Spanish labels
- **WHEN** user opens the application
- **THEN** the sidebar navigation shows: "Inicio", "Perros", "Salud", "Adopciones", "Fondos", "Calendario", "Voluntarios", "Informes", "Admin / Eliminados", "Configuración"

#### Scenario: Form save button is in Spanish
- **WHEN** user opens any edit form
- **THEN** the submit button reads "Guardar"

#### Scenario: Cancel button is in Spanish
- **WHEN** user opens any edit form
- **THEN** the cancel link reads "Cancelar"

#### Scenario: Delete confirmation dialog is in Spanish
- **WHEN** user clicks a delete button
- **THEN** the confirmation dialog text is in Spanish

#### Scenario: Error messages are in Spanish
- **WHEN** a form validation error occurs
- **THEN** the error message is displayed in Spanish

### Requirement: Enum values display in Spanish
The UI SHALL display Spanish labels for all enum values. Raw C# member names (e.g. "Applied", "Available") SHALL NOT be shown to users.

#### Scenario: Dog status chip shows Spanish label
- **WHEN** a dog with status "Available" is displayed
- **THEN** the status chip shows "Disponible"

#### Scenario: Adoption status chip shows Spanish label
- **WHEN** an adoption with status "Applied" is displayed
- **THEN** the status chip shows "Solicitada"

#### Scenario: Volunteer status chip shows Spanish label
- **WHEN** a volunteer with status "Active" is displayed
- **THEN** the status chip shows "Activo"

#### Scenario: Donation category dropdown shows Spanish options
- **WHEN** user opens the donation category selector
- **THEN** options show: "Mensual", "Única vez", "En especie", "Empresa"

#### Scenario: Expense category dropdown shows Spanish options
- **WHEN** user opens the expense category selector
- **THEN** options show: "Médico", "Alimentación", "Instalaciones", "Suministros", "Transporte", "Otro"

#### Scenario: Dog status dropdown shows Spanish options
- **WHEN** user opens the dog status selector
- **THEN** options show: "Disponible", "Adoptado", "En acogida", "Médico", "Cuarentena"

#### Scenario: Fee payment method dropdown shows Spanish options
- **WHEN** user opens a fee payment method dropdown
- **THEN** options show: "Metálico", "Bizum", "Transferencia"

### Requirement: Enum label maps centralized in labels.ts
The SPA SHALL define all enum-to-Spanish label mappings in a single `src/labels.ts` file. Components SHALL import labels from this file rather than defining their own maps.

#### Scenario: StatusChip renders Spanish label
- **WHEN** a StatusChip is rendered for any enum value
- **THEN** it displays the Spanish label from the central label map, not the raw enum member name

#### Scenario: Single source of truth for enum labels
- **WHEN** an enum label is needed in any page or component
- **THEN** it is imported from `src/labels.ts`
