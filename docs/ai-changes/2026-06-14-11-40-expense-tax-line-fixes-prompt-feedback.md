# Expense Tax-Line Test Failures — Prompt Feedback

## Original prompt
> check and fix failing tests

## Assessment
Good, action-oriented prompt. It correctly delegated diagnosis (didn't pre-assume the
cause). The only ambiguity: "fix failing tests" can mean "make tests pass" (which can
tempt weakening assertions) vs "fix whatever the tests are catching" (fix the
product). Here the right reading was the latter — two real product bugs existed.

## Suggestions for future prompts
1. **State the intent behind green.** e.g. "fix the failing tests by fixing the root
   cause — do not weaken assertions or relax invariants." This pre-empts the lazy
   path of editing the test to match buggy behavior.
2. **Point at scope if known.** "the expense tax-line tests" would have narrowed the
   search instantly.
3. **Ask for a root-cause classification.** Requesting "say whether each failure is a
   test bug or a product bug" forces the distinction that mattered here.

## A stronger version
> Check the failing tests. For each, identify the root cause and say whether it's a
> test defect or a production defect. Fix the production defects properly (don't
> weaken assertions or relax domain invariants), update genuinely stale test data,
> and confirm the whole suite is green with no pending migration.
