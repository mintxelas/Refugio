# Remove Dead Code — Prompt Feedback

## Original prompt
> detect and remove dead code

## Assessment
Clear intent, but under-specified on scope and risk tolerance. Dead-code removal
spans several very different categories (unused private members, unreferenced
public API, legacy static assets, generated artifacts), each with a different
confidence level and blast radius.

## Suggestions for future prompts
1. **State the scope.** e.g. "C# only", "include `wwwroot`/static assets", or
   "whole repo including generated files".
2. **State the risk tolerance.** e.g. "only remove what's provably unreferenced and
   keep public API" vs "aggressive — remove anything not reachable from an
   endpoint/entry point".
3. **Say whether to commit.** "remove and commit on a branch" vs "show me the list
   first" changes the workflow.
4. **Call out known false-positive sources.** DI-resolved services, EF overrides,
   Akka message handlers, and extension methods are not seen by a naive call graph;
   noting "this app uses DI + Akka + EF" up front avoids over-deletion.

## A stronger version
> Detect and remove dead code in `src/` (C#) and `wwwroot`. Only remove code that is
> provably unreferenced — keep public API surface, EF overrides, DI-registered
> services, and Akka handlers. List anything ambiguous instead of deleting it.
> Validate with build + tests, then show the diff before committing.
