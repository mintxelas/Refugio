# Prompt Feedback: Update CLAUDE.md to reflect React UI

## Original prompt

> "did you update the docs to reflect the UI is REACT?"

## What was good

- Direct and unambiguous — left no doubt about what needed updating.
- Correctly implied all Blazor/Razor references were stale.

## What could be improved

The prompt came after a session where Blazor was removed. A stronger prompt would give scope
and intent upfront so the task could be done in the same step rather than after a question:

### Suggested prompt

```
Update CLAUDE.md to reflect that the UI is now a React SPA (Vite + TypeScript in ClientApp/).
Specifically:
- Remove the Blazor SSR, localization, and Blazor ↔ API sections entirely.
- Remove the blazor-ssr-form-page and add-localization skills from the table.
- Update the request flow, Refugio.Web architecture description, auth endpoints, and pages table.
- Add a React SPA section describing ClientApp/ folder layout and key conventions.
- The docs should be accurate enough that a future Claude session generates React code,
  not Razor pages.
```

### Why this is better

1. **Explicit scope** — lists every section to remove/add, not just "update to React".
2. **Success criterion** — "accurate enough that a future session generates React code" tells Claude what "done" looks like.
3. **Timing** — ideally given when the Blazor removal is requested, not after, so the docs update is part of the same atomic task.

## General pattern

For documentation updates: state (a) what is stale, (b) what the correct state is, (c) the
acceptance bar. "Update docs to X" without these three produces ambiguity about how deep to go.
