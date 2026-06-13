## ADDED Requirements

### Requirement: FeePaymentMethod enum defines allowed payment methods
The system SHALL define a `FeePaymentMethod` enum with exactly three members: `Cash`, `Bizum`, `Transfer`. This enum SHALL be stored as a string in the database. Member names are permanent API identifiers.

#### Scenario: Enum members serialize as strings
- **WHEN** an adoption with `preAdoptionFeePaymentMethod = Cash` is returned from the API
- **THEN** the JSON field value is the string `"Cash"`

### Requirement: Adoption tracks payment method for each fee stage
The `Adoption` aggregate SHALL store nullable `PreAdoptionFeePaymentMethod` and `AdoptionFeePaymentMethod` fields of type `FeePaymentMethod?`. Both fields SHALL default to `null` and be independently settable.

#### Scenario: Default payment methods are null
- **WHEN** a new adoption is submitted without specifying payment methods
- **THEN** both `preAdoptionFeePaymentMethod` and `adoptionFeePaymentMethod` are `null`

#### Scenario: Submit adoption with payment methods
- **WHEN** a new adoption is created with `preAdoptionFeePaymentMethod = Bizum` and `adoptionFeePaymentMethod = Transfer`
- **THEN** both values are persisted and returned in `AdoptionDto`

#### Scenario: Update adoption to set payment method
- **WHEN** `UpdateDetails` is called with a new payment method value
- **THEN** the entity persists the updated method and it appears in subsequent GET responses

#### Scenario: Clear payment method on update
- **WHEN** `UpdateDetails` is called with `null` for a previously set payment method
- **THEN** the method is cleared (stored as `null`)

#### Scenario: Payment method is independent of fee-charged flag
- **WHEN** an adoption has `preAdoptionFeeCharged = false` but `preAdoptionFeePaymentMethod = Cash`
- **THEN** both values are persisted as-is with no validation error

### Requirement: Payment method fields are exposed through the API
`AdoptionDto`, `CreateAdoptionRequest`, and `UpdateAdoptionRequest` SHALL include `preAdoptionFeePaymentMethod` and `adoptionFeePaymentMethod` (nullable `FeePaymentMethod`).

#### Scenario: GET adoption returns payment method fields
- **WHEN** `GET /api/adoptions/{id}` is called for an adoption with payment methods set
- **THEN** the JSON response includes `preAdoptionFeePaymentMethod` and `adoptionFeePaymentMethod` with correct string values

#### Scenario: POST creates adoption with payment methods
- **WHEN** `POST /api/adoptions` body includes `PreAdoptionFeePaymentMethod = "Transfer"` and `AdoptionFeePaymentMethod = "Cash"`
- **THEN** the created adoption persists and returns both values

#### Scenario: PUT updates adoption payment methods
- **WHEN** `PUT /api/adoptions/{id}` body includes updated payment method values
- **THEN** the adoption reflects the new values in subsequent reads

#### Scenario: Payment method fields are null in GET when not set
- **WHEN** `GET /api/adoptions/{id}` is called for an adoption with no payment methods set
- **THEN** `preAdoptionFeePaymentMethod` and `adoptionFeePaymentMethod` are JSON `null`

### Requirement: Adoption edit form shows payment method dropdowns in Spanish
The `AdoptionEdit` React page SHALL render a `<select>` dropdown next to each fee-charged checkbox. The dropdown options SHALL be labeled in Spanish: "Metálico" (Cash), "Bizum" (Bizum), "Transferencia" (Transfer), plus an empty/unset option. The dropdowns SHALL be enabled regardless of the fee-charged checkbox state.

#### Scenario: Dropdowns show Spanish labels
- **WHEN** user opens the adoption edit form
- **THEN** the pre-adoption payment method dropdown shows options: (empty), "Metálico", "Bizum", "Transferencia"
- **THEN** the adoption payment method dropdown shows the same options

#### Scenario: Form populates existing payment method on edit
- **WHEN** user opens the adoption edit form for an adoption with `preAdoptionFeePaymentMethod = Bizum`
- **THEN** the pre-adoption payment method dropdown shows "Bizum" as selected

#### Scenario: User selects payment method and saves
- **WHEN** user selects "Transferencia" from the adoption payment method dropdown and submits
- **THEN** the API receives `AdoptionFeePaymentMethod = "Transfer"` and the adoption is updated

#### Scenario: User clears payment method and saves
- **WHEN** user selects the empty option from a payment method dropdown and submits
- **THEN** the API receives `null` for that payment method field
