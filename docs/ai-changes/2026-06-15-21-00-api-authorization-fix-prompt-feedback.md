# Prompt Feedback: API Authorization Fix

## Original prompt

> "Audit the security of this app."

## What worked

The prompt triggered the full `/security-check` skill which provided a structured audit framework. The open-ended instruction gave enough freedom to identify the critical severity issues without being narrowed to a specific area.

## What could be improved

### 1. Scope the fix request separately from the audit

"Audit the security" and "fix the security issues" are two different tasks. An audit produces a report; a fix modifies code. Combining both in one implicit request forces the assistant to guess which findings to fix and in what order. Consider two explicit steps:

> Step 1: "Audit the security of this app and produce a prioritized findings report."
> Step 2: "Fix CRIT-1 and CRIT-2 from the report."

### 2. Specify the fix scope

Security audits produce many findings. Without direction, the fixer must decide which severity level to address. A better prompt after seeing the report:

> "Fix the Critical and High findings. Skip the Medium/Low ones for now — I'll address those in a separate pass."

### 3. Clarify which endpoints should remain public

The fixer had to infer that `/api/dogs` and `/api/dashboard` should stay anonymous (reasonable for a shelter's public catalog). If the intent was different (fully internal app), specifying it would prevent guessing:

> "This is a fully internal management app — no endpoint should be public. All API routes require login."

OR:

> "The dog catalog and dashboard stats are public-facing. Keep those endpoints accessible without login."

### 4. State the testing expectation

"Write tests proving the vulnerability before fixing" vs "just fix and update tests" produce different outputs. Explicit:

> "Write integration tests that prove each critical vulnerability first (they should fail), then apply the fix so the tests pass."
