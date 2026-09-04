# Prompt feedback — Real CSRF (antiforgery) protection

## Original prompt

> Arregla el problema de CSRF

(Referring to the Medium-severity finding from the original audit: "CSRF defenses rely solely on
SameSite cookie attribute; `.DisableAntiforgery()` calls are no-ops because no antiforgery
middleware/token system is registered at all.")

## Clarity assessment

Unambiguous in *what* to fix (only one CSRF finding existed in the audit), but this is a case
where the short prompt genuinely couldn't specify *how* — there were real architectural choices
to make (double-submit header vs. hidden form field; whether to bind tokens to identity or not;
Minimal-API-specific mechanics that aren't obvious even from the ASP.NET Core docs' table of
contents) that the one-line request left entirely to judgment. That's fine for a request this
size — asking the user to pre-specify "use the double-submit header pattern with a
`/antiforgery/token` endpoint" would be asking them to already know the ASP.NET Core Minimal API
antiforgery internals, which is exactly the kind of decision this tool exists to make well by
default and explain afterward.

## What made this harder than the previous two fixes

This is worth naming because it's a pattern likely to recur: **the first implementation compiled,
looked correct by inspection, and was still wrong** (`.RequireAntiforgery()` doesn't exist for
Minimal APIs; the corrected filter-based version then had a second, subtler bug — cached tokens
going stale across the anonymous → authenticated identity transition). Neither bug was caught by
reading the code again; both were only caught by actually running requests through the real
HTTP pipeline (`curl` against a live `dotnet run` instance) and reading the raw response, before
trusting the automated test suite's green/red signal. For security-relevant plumbing
specifically — auth, CSRF, rate limiting, anything with framework "magic" behavior — a
"looks right, compiles, tests probably pass" pass is not sufficient; the extra step of a live
manual request cycle is what surfaced both issues here.

## Suggestions for future prompts like this one

1. **If a fix touches both backend and frontend request-plumbing (as any real CSRF fix must),
   it's worth confirming up front whether frontend changes are in scope** — this request didn't
   say, and the correct interpretation (a backend-only "fix" would leave the SPA broken, since
   the SPA would then fail every mutating call with no token) was to treat both sides as
   required. Worth flagging explicitly next time if only one side is wanted for some reason
   (e.g. "backend only, I'll wire the frontend myself").
2. **For "fix X" prompts on security/auth-adjacent plumbing, expect (and budget time for) a
   live-request verification pass**, not just a green test suite — as this session
   demonstrated, tests can pass with a subtly broken implementation until a live end-to-end
   check happens to exercise the exact anonymous→authenticated transition that broke.

## Original prompt (verbatim, for reference)

```
Arregla el problema de CSRF
```
