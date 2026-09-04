# Prompt feedback — High-severity security fixes

## Original prompt

> Corrige los problemas de criticidad Alta

(Given right after a full security-audit report that listed 1 Critical, 2 High, 4 Medium, and 3
Low findings, each with a title, component, and fix recommendation.)

## Clarity assessment

The prompt was short but unambiguous **because it referred back to a structured, already-labeled
report** — "High" mapped directly to the two findings tagged `[ALTA]` in the prior message
(BFLA on DELETE routes, and no rate limiting on auth). No clarification was needed, and no
scope-creep questions were warranted: fixing exactly the two High findings, and no others, was
the correct interpretation.

This is a good example of how a short follow-up prompt works well: it leans entirely on shared
context from the immediately preceding audit output. It would **not** have been clear in
isolation (e.g., in a fresh conversation with no prior audit), where "the High severity problems"
would be undefined.

## Suggestions for even better future prompts

1. **If only fixing a subset of a numbered list, naming the numbers/titles removes any residual
   ambiguity** — e.g. "corrige los hallazgos #2 y #3 (BFLA en DELETE y rate limiting)" instead of
   the severity label alone. Not needed here since there were only two High items, but it
   matters once a severity bucket has 3+ items and you only want some of them.
2. **State whether tests should be added or just the fix** — this prompt didn't say, so the
   assumption made (and consistent with this repo's `CLAUDE.md` mandate: "ALWAYS write at least
   one test that proves the step taken is correct") was to add regression tests for both fixes.
   If a future request wants a fast patch-only pass without new tests, say so explicitly (e.g.
   "sin tests, solo el fix").
3. **If a fix has a known trade-off** (like the in-memory, per-instance rate limiter here), it's
   worth stating upfront whether the deployment target is single-instance or load-balanced — it
   changes the correct implementation (in-memory limiter vs. a distributed store like Redis).
   This project's deployment (Raspberry Pi, single instance, visible from `publish-pi/`) made
   in-memory the obviously correct choice without having to ask, but that won't always be true.

## Original prompt (verbatim, for reference)

```
Corrige los problemas de criticidad Alta
```
