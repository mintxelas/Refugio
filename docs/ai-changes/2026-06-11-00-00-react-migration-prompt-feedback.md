# React SPA Migration — Prompt Feedback

## Original Prompt

> "Implement the React SPA migration described in `docs/react-migration/PLAN.md`."

With additional inline context:
- Scope: backend JSON endpoints, SPA scaffold, 25 pages, visual parity
- Key constraint: do not run `npm install` or `dotnet build`
- Do not delete `.razor` files yet

## What Worked Well

The prompt correctly pointed to the plan file rather than restating it inline. The constraint to not run npm/dotnet was explicit and prevented side effects. The instruction to preserve `.razor` files gave a clear scope boundary.

## What Could Be Improved

### 1. Antiforgery expectations on delete/mutating endpoints

The prompt did not specify whether the SPA should send antiforgery tokens. The Blazor pages use `<AntiforgeryToken/>` in forms. The SPA uses `credentials: 'include'` but no token header. The plan and prompt are silent on this.

**Better phrasing:** "The SPA does not send antiforgery tokens. All mutating endpoints called by the SPA must have `.DisableAntiforgery()` applied, or the backend must be configured to exempt authenticated JSON requests."

### 2. Specify which existing endpoints to reuse vs create new

The prompt implied "add JSON upload variants" but did not specify whether the existing redirect-based endpoints should be repurposed or new ones added in parallel. This led to adding parallel endpoints (safer, but doubles the handler count).

**Better phrasing:** "Add new endpoints at distinct paths (e.g., `.../upload` suffix) that return JSON, leaving the existing redirect-based endpoints intact for Blazor compatibility."

### 3. Clarify the delete verb convention

The existing Blazor pages use `POST /{id}/delete`. The plan mentioned this but did not confirm whether the SPA should also use POST (not HTTP DELETE). The existing server endpoints use the `POST /{id}/delete` shape.

**Better phrasing:** "All delete actions must use `POST /api/{entity}/{id}/delete` (not HTTP DELETE). The server registers these as POST endpoints for browser compatibility."

### 4. State management depth for `AuthContext`

The prompt did not specify how auth state should be initialized (on mount? on every page load?). The implemented approach calls `/api/auth/me` once on mount — correct but not stated.

**Better phrasing:** "On SPA load, call `GET /api/auth/me`. If 401, redirect to `/login`. Cache the result in React context for the session lifetime."

### 5. Pagination shape

The prompt did not repeat the `Page<T>` JSON shape (`items`, `totalCount`, `pageNumber`, `pageSize`). This is in CLAUDE.md but easy to mis-case.

**Better phrasing:** "The backend `Page<T>` serializes to `{ items: T[], totalCount: number, pageNumber: number, pageSize: number }` (camelCase). Use this exact shape in `types.ts`."

### 6. Task completion HTTP verb

The plan omitted the HTTP verb for `PUT /api/tasks/{id}/complete`. Using POST would have caused a 405. Including the verb in the plan prevents silent mismatches.

**Better phrasing:** "Task completion: `PUT /api/tasks/{id}/complete` (not POST)."

## Suggested Improved Prompt Structure

```
Implement the React SPA migration described in `docs/react-migration/PLAN.md`.

Key clarifications:
1. SPA uses `credentials: 'include'` (no antiforgery tokens). All mutating SPA endpoints need `.DisableAntiforgery()`.
2. Add JSON upload variants at new paths (`/upload` suffix). Keep existing redirect endpoints intact.
3. Delete operations use `POST /api/{entity}/{id}/delete` (not HTTP DELETE).
4. Auth init: call `GET /api/auth/me` on SPA load; 401 → redirect to /login.
5. `Page<T>` shape: `{ items, totalCount, pageNumber, pageSize }` (camelCase).
6. HTTP verbs for non-obvious endpoints: task completion = `PUT /api/tasks/{id}/complete`.
7. Do not run `npm install` or `dotnet build`. Do not delete `.razor` files.
```
