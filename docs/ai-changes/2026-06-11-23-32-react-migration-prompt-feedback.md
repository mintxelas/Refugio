# React Migration — Prompt Feedback

## Original prompt

> Convert the project's UI to React. Maintain visual aspect and navigation.

## What worked

Short and clear on intent. The codebase context (CLAUDE.md, existing pages) provided enough detail to infer the full scope.

## What was missing / could be improved

**1. Scope signal**
"Convert the UI to React" is ambiguous about completeness. A better prompt would state:
> "Convert all pages to React (full replacement). Keep the .razor files until the SPA is validated."

**2. Backend changes allowed?**
The migration required 4 new auth endpoints + 1 new dog-photo upload endpoint. Clarify:
> "You may add JSON-returning equivalents of existing endpoints where needed to support the SPA."

**3. Auth approach**
The prompt didn't specify how React should handle auth (cookie vs. token, how to detect session). Specify:
> "Use cookie auth — same cookie the API already sets. React should call GET /api/auth/me on startup to read the session."

**4. i18n scope**
The Blazor app has 4 locales. Should React support them? The prompt didn't say. Specify:
> "English-only for now — drop the language switcher."

**5. Dev workflow**
Didn't mention whether to scaffold via `npm create vite` or write files manually. Since this runs in an AI agent:
> "Write all ClientApp files manually (do not run npm create vite). Include a vite.config.ts dev proxy for /api and /auth."

**6. Blazor teardown**
Should the .razor files be deleted? Not stated. Recommend:
> "Keep Blazor pages in place until the React SPA is confirmed working; note what to delete as a follow-up step."

## Improved prompt example

```
Convert the Refugio shelter app UI from Blazor SSR to a Vite + React SPA:

- Place the SPA in src/Refugio.Web/ClientApp/ (write files manually, do not run npm)
- ASP.NET serves ClientApp/dist/ in production; Vite proxies /api + /auth to port 5110 in dev
- Cookie auth, same-origin. Add GET /api/auth/me and POST /api/auth/login (JSON) alongside the existing form endpoints
- Convert all 25 pages to React with the same routes, Tailwind design tokens, and Material Symbols icons
- English-only (drop i18n)
- Keep .razor files in place; note the teardown steps in a summary doc
- Write docs/ai-changes/ reasoning, summary, and prompt-feedback docs
```
