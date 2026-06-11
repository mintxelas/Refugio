# Prompt feedback — Feature specifications generation

## Original prompt

> based on this project, generate the specs of each feature, entity and use case supported and save them as md files in the "features" folder.

## What worked well

- Clear deliverable type (specs), clear scope (features + entities + use cases), clear destination (`features/` folder, md files).
- "Based on this project" correctly signaled code-derived specs rather than aspirational ones.

## Ambiguities resolved by assumption

1. **File granularity** — one file per feature area (entities + use cases together) was chosen. If you wanted one file per entity or per use case, say so: *"one md file per entity and one per use case"*.
2. **Spec depth** — endpoint-level detail (routes, verbs, auth, redirects, upload constraints) was included. Specify the audience if you want different altitude: *"specs for product/QA"* (less API detail) vs *"specs for developers"* (current output).
3. **Spec format** — no template given; a table-for-entities + UC-id format was invented. Supplying a template or one example file would guarantee the structure you want.
4. **Cross-cutting concerns** — placed in a README index. Alternative: repeat per file for standalone readability.

## Suggestions for future prompts

- Name the audience and purpose: *"…to be used by the feature-plan-architect agent as input"* — this changes what is worth including (e.g. invariants and quirks vs marketing-level descriptions).
- State whether existing docs should be trusted or re-verified: *"derive from source code, not CLAUDE.md"* — here both were used, code as authority.
- If specs should stay current, ask for the maintenance hook: *"add a CLAUDE.md note that specs in features/ must be updated when endpoints change"*.
- Mention exclusions explicitly if any (e.g. *"skip test infrastructure and seeding"*) — seeding/test conventions were deliberately left out as non-features.
