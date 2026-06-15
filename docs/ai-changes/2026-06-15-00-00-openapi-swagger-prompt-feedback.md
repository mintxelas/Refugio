# Prompt Feedback: OpenAPI/Swagger

## Original prompt

> Add openapi/swagger specification of the API and a UI to browse it.

## What was clear

- Goal unambiguous: spec + browser UI.

## What was missing / could improve

- **Environment scope**: should the UI be dev-only or also in production/staging? Assumed dev-only.
- **Auth in UI**: should Scalar allow users to authenticate (e.g. set a cookie or bearer token) via the UI? Not addressed.
- **Endpoint annotations**: should operation summaries/tags be added to all endpoints immediately, or just the wiring? Adding them is significant effort — a good follow-up prompt would be: "Add `.WithSummary()` and `.WithTags()` to all endpoint groups."

## Suggested prompt

> Add OpenAPI spec (dev-only) and a Scalar browser UI. Include cookie-auth security scheme so the UI can authenticate. After wiring, add `.WithSummary()` and `.WithTags()` to all endpoint groups so the spec is human-readable.
