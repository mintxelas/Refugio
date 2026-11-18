# Prompt feedback: Security hardening

## Original prompt

> harden this application so it can't be hacked.

## Assessment

The goal is clear, but the prompt is unbounded. "Can't be hacked" cannot be achieved or verified, so the scope had to be inferred: which threats matter, what level of breaking change is acceptable, and whether to report or to fix. Several fixes changed behavior (inactive volunteers can't log in, volunteers can't delete events, a one-time logout for everyone, Tailwind build change, major frontend toolchain bumps). Those choices were made without your input.

## Suggestions

- **State the threat model and deployment:** e.g. "Public internet on a Raspberry Pi behind Caddy; attackers are anonymous users and ex-volunteers."
- **Say whether to fix or only report:** "Audit and report, then fix Critical/High only", or "fix everything up to Medium".
- **Declare the acceptable breakage:** "Forcing a re-login is fine", "Don't change what volunteers can do", "No major dependency upgrades".
- **Name known business rules:** e.g. whether volunteers may see other volunteers' contact data, and whether donation totals are meant to be public. These decide whether a finding is a bug or intended.
- **Ask for prioritization if time is limited:** "Top 5 fixes by risk reduction."

## Better prompt example

> Security-audit Refugio as deployed (public internet, Pi behind a TLS proxy). Fix every Critical/High
> finding with a regression test; list Medium/Low with recommendations. Forcing users to re-login is OK;
> changing what the Volunteer role can do needs my sign-off first. Don't upgrade major frontend versions
> unless a vulnerability requires it.
