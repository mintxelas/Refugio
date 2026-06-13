## ADDED Requirements

### Requirement: Date of Entry is mandatory on check-in
When checking in a new dog, the system SHALL require a Date of Entry value. If omitted or invalid, the request SHALL be rejected with a validation error.

#### Scenario: Valid date provided on check-in
- **WHEN** a user submits the check-in form with a valid Date of Entry
- **THEN** the dog is created with `ArrivalDate` equal to the submitted date

#### Scenario: Date of Entry omitted on check-in
- **WHEN** a user submits the check-in form without a Date of Entry
- **THEN** the form SHALL display a validation error and not submit

#### Scenario: Default date pre-filled
- **WHEN** the check-in form is opened
- **THEN** the Date of Entry field SHALL be pre-filled with today's date

### Requirement: Date of Entry is editable on update
When editing an existing dog, the system SHALL allow updating the Date of Entry. The field SHALL be mandatory.

#### Scenario: Date of Entry editable in edit form
- **WHEN** a user opens the edit form for an existing dog
- **THEN** the Date of Entry field SHALL display the dog's current `ArrivalDate`

#### Scenario: Updated date persisted
- **WHEN** a user changes the Date of Entry and saves
- **THEN** the dog's `ArrivalDate` SHALL be updated to the new value

#### Scenario: Date of Entry cleared in edit form
- **WHEN** a user clears the Date of Entry field and saves
- **THEN** the form SHALL display a validation error and not submit

### Requirement: Date of Entry displayed in dog detail view
The dog detail view SHALL display the Date of Entry in a human-readable format.

#### Scenario: Date of Entry visible on detail page
- **WHEN** a user views a dog's detail page
- **THEN** the Date of Entry SHALL be shown formatted as a locale date string
