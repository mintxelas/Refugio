# DDD Rewrite — Prompt Feedback

## Original prompt

> Rewrite the backend of this app to make it Domain Driven Design oriented. THEN Create an API for it and
> THEN rewrite the Blazor UI to use that API. Keep all the functionality and visual aspect.

## What worked well

- **Clear sequencing** ("THEN … THEN …") communicated layering priorities.
- **Hard invariant stated** ("Keep all the functionality and visual aspect") gave an unambiguous
  success criterion — it drove the decision to keep routes/JSON identical and lean on the existing
  integration suite as the contract.

## Where the prompt left big decisions open (worth specifying next time)

1. **Fate of Akka.NET.** "DDD oriented" can coexist with actors. I removed them (services replace
   routing); if you wanted actors kept underneath, say e.g. *"replace the actor layer with application
   services"* or *"keep Akka but make the domain rich"*.
2. **"Create an API"** — one already existed at `/api/*`. I interpreted it as *"make the API the single
   backend boundary and complete it"* (added paged/deleted/counts/photo endpoints). Alternatives (new
   `/api/v2`, separate API host, OpenAPI doc) were plausible readings. State the target shape:
   *"extend the existing /api routes; no versioning; same JSON"*.
3. **UI-to-API transport.** "Use that API" could mean typed client over HTTP (chosen — real loopback
   calls with cookie forwarding) or just "call services that back the API". If you care about the hop,
   say *"over real HTTP, including in tests"* or *"in-process is fine"*.
4. **Database compatibility.** Nothing said existing `shelter.db` files must survive. I treated
   zero-migration as mandatory (and verified it). Make it explicit: *"no schema changes / existing DBs
   must keep working"*.
5. **Test strategy.** The unit suite was actor-shaped and had to be rewritten. If you have expectations
   (port 1:1, minimum counts, coverage), state them: *"port all unit test cases to service tests"*.
6. **Tolerance for behavior fixes.** I fixed a pre-existing dashboard crash (divide-by-zero on empty
   Goals), stopped leaking `PasswordHash`, and turned an FK 500 into a 404. "Keep all functionality"
   strictly read could forbid those. A line like *"obvious bugs may be fixed; note them"* removes doubt.

## Suggested phrasing for a future prompt of this scale

> Rewrite the backend to DDD: rich aggregates with behavior + domain events, repositories + unit of work,
> application services, DTO contracts. Remove Akka entirely. Keep the existing /api routes and JSON shapes
> exactly (the integration tests are the contract) and add any endpoints the UI still needs. Then make the
> Blazor SSR pages consume the API over real HTTP with the user's cookies forwarded. No schema changes —
> existing shelter.db must run unmigrated. Port the unit tests to service/domain tests; everything green.
> Obvious pre-existing bugs may be fixed — list them in the summary. Update CLAUDE.md and the skills.

## General guidance

- For rewrites, name the **contract that must not move** (routes/JSON/DB/visuals) and the **layer that
  may change freely** — that pair of constraints does more than any amount of architectural adjectives.
- Mention docs/skills/memory upkeep explicitly if you want them in scope (done here by standing
  instruction, but the prompt itself didn't ask).
