# Prompt feedback — Urgent meds drill-down

## Original prompt
> When I click on "urgent meds" red-cross icon it should take me to the list
> of specific dogs that need the urgent medication. Remember to protect the
> endpoint and page with authentication requirement and CSRF protection.

## What worked well
- Clear trigger ("urgent meds red-cross icon") made it easy to locate the
  exact UI element (`Home.tsx` KPI card).
- Explicitly calling out auth + CSRF meant no ambiguity about whether this
  was a security-sensitive change — it was treated as a hard requirement,
  not a nice-to-have.

## What was ambiguous / assumed
- "List of specific dogs" didn't say whether to show one row per dog or one
  row per urgent medication (a dog could have more than one). Assumed
  one row per medication, since that's what needs individual attention and
  it's what the underlying `UrgentMeds` count actually counts.
- No route/URL was specified. Chose `/health/urgent-medications` since it's
  a drill-down of the existing health/medical area, but `/reports/...` or a
  modal on the dashboard itself were also plausible.
- "CSRF protection" was mentioned for what turned out to be a GET-only
  endpoint — the app's CSRF filter only applies to non-safe HTTP methods, so
  there was nothing extra to add here beyond `RequireAuthorization()`. Worth
  confirming in future prompts whether the intended action is read-only or
  will also need a mutation (e.g., "mark as administered"), since that would
  actually exercise the CSRF path.

## Suggestions for future prompts
- If a future request implies a mutating action (e.g., "let me dismiss an
  urgent medication from this list"), say so explicitly — that changes the
  endpoint from GET to POST/PATCH and brings the CSRF token flow into play
  for real.
- Naming the desired destination screen (e.g., "a new page under Health" vs.
  "a filtered view of the existing Health page") would remove one judgment
  call.
