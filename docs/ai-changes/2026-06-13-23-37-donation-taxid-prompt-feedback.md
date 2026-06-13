# Prompt feedback: Add TaxId to Donation

## Original prompt

> Update the Donation with a TaxId field (string, optional).

## What was clear

Type (`string`) and nullability (`optional`) were explicit — no ambiguity on domain model.

## What was inferred (could be made explicit)

- **Validation**: no max-length or format constraint assumed. Prompt could say "no format validation" or specify one (e.g. "max 50 chars").
- **UI placement**: inferred the edit page; prompt could confirm or add "also show on the list/detail view".
- **CSV export**: assumed TaxId belongs in the export; prompt could say "include in CSV export" or "omit".
- **Localization labels**: guessed per-culture labels (Tax ID / N° de identificación fiscal / CNPJ/CPF / NIF/CIF). Prompt could provide preferred labels.

## Improved prompt example

> Add an optional `TaxId` string field (max 50 chars) to the `Donation` entity.
> Show it on the edit page with label "Tax ID".
> Include it in the CSV donation export.
> No format validation needed.
