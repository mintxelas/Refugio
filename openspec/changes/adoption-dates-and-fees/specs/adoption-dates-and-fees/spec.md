## ADDED Requirements

### Requirement: Adoption tracks pre-adoption and adoption dates
The `Adoption` aggregate SHALL store a nullable `PreAdoptionDate` and a nullable `AdoptionDate` representing the dates of each milestone. Both fields SHALL be optional; absence means the milestone has not occurred yet.

#### Scenario: Submit adoption with no dates
- **WHEN** a new adoption is created without providing dates
- **THEN** `PreAdoptionDate` and `AdoptionDate` are `null`

#### Scenario: Submit adoption with both dates
- **WHEN** a new adoption is created with `preAdoptionDate` and `adoptionDate` values
- **THEN** both dates are persisted and returned in `AdoptionDto`

#### Scenario: Update adoption to set dates
- **WHEN** `UpdateDetails` is called with new date values
- **THEN** the entity persists the updated dates and they appear in subsequent GET responses

#### Scenario: Clear a date on update
- **WHEN** `UpdateDetails` is called with `null` for a previously set date
- **THEN** the date is cleared (stored as `null`) in the entity

### Requirement: Adoption tracks fee-charged flags for each stage
The `Adoption` aggregate SHALL store a non-nullable `PreAdoptionFeeCharged` (`bool`, default `false`) and `AdoptionFeeCharged` (`bool`, default `false`) indicating whether a fee was collected at each stage.

#### Scenario: Default fee flags are false
- **WHEN** a new adoption is submitted without specifying fee flags
- **THEN** both `PreAdoptionFeeCharged` and `AdoptionFeeCharged` are `false`

#### Scenario: Submit adoption with fees charged
- **WHEN** a new adoption is created with both fee flags set to `true`
- **THEN** both flags are persisted and returned as `true` in `AdoptionDto`

#### Scenario: Update fee flags
- **WHEN** `UpdateDetails` is called with new fee flag values
- **THEN** the entity stores the updated flags

### Requirement: All four fields are exposed through the API
The `AdoptionDto`, `CreateAdoptionRequest`, and `UpdateAdoptionRequest` SHALL include `preAdoptionDate`, `adoptionDate`, `preAdoptionFeeCharged`, and `adoptionFeeCharged`.

#### Scenario: GET adoption returns all four fields
- **WHEN** `GET /api/adoptions/{id}` is called for an adoption with dates and fee flags set
- **THEN** the JSON response includes `preAdoptionDate`, `adoptionDate`, `preAdoptionFeeCharged`, `adoptionFeeCharged` with correct values

#### Scenario: POST creates adoption with fields
- **WHEN** `POST /api/adoptions` body includes all four new fields
- **THEN** the created adoption persists and returns them

#### Scenario: PUT updates adoption with fields
- **WHEN** `PUT /api/adoptions/{id}` body includes updated values for the four fields
- **THEN** the adoption reflects the new values in subsequent reads

### Requirement: Adoption form shows all four fields
The `AdoptionEdit` React page SHALL render date picker inputs for `preAdoptionDate` and `adoptionDate`, and checkbox inputs for `preAdoptionFeeCharged` and `adoptionFeeCharged`.

#### Scenario: Form shows empty dates by default
- **WHEN** user opens the adoption form for a new adoption
- **THEN** date fields are empty and fee checkboxes are unchecked

#### Scenario: Form populates existing values on edit
- **WHEN** user opens the adoption form for an existing adoption with dates and fees set
- **THEN** date fields show the stored dates and fee checkboxes reflect the stored flag values

#### Scenario: User fills in dates and fees and saves
- **WHEN** user enters dates and checks fee boxes then submits the form
- **THEN** the API receives the new values and the adoption is updated
