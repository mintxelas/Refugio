# Prompt Feedback: MED/LOW Security Fixes

## Original prompt

> "yes"

(Single-word approval to proceed with implementing the medium and low severity security findings from the preceding audit.)

## What worked

The implicit context was clear: a security audit had just been completed and the "yes" was approving the fix plan already laid out in the conversation. No ambiguity about scope.

## Suggestions for improvement

The original prompt could have been improved as follows:

### 1. Be explicit about which findings to fix

**Current**: "yes"

**Better**:
> Fix the MED-1 (open redirect), MED-2 (file upload), MED-3 (CSV injection), MED-4 (PBKDF2 iterations), LOW-1 (timing oracle), and LOW-3 (path traversal) findings from the audit. Prioritize in that order.

Being explicit avoids relying on conversation context that may be lost in a long session.

### 2. Specify production migration concern upfront

If the password iteration change must not break existing production hashes, say so:

> Fix MED-4 PBKDF2 iterations. Note: production DB has existing hashes. Provide a migration path, not just the hash change.

### 3. Mention test expectations

> All fixes must keep the full test suite green. If any test assertions need updating to match the new secure behavior, update them and explain why.

This prevents the agent from silently accepting test failures as "expected" or leaving pending work undocumented.

### 4. Split into two prompts: plan then implement

**First prompt**: "Describe the code changes needed for each MED/LOW finding and the files involved."
**Second prompt**: "Implement the plan above and run all tests."

This pattern gives you a review checkpoint before code is written, reducing the risk of misunderstood scope.
