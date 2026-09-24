# P2-T04 — Job On Context Snapshot Invariant — OWNER CLARIFICATION RESPONSE

**Status:** OWNER CLARIFICATION APPLIED + VERIFIED — awaiting ONE focused independent review of this
snapshot invariant.
**P2-T04 remains CLOSED. P2-T07 remains CLOSED. P2-T08 and P2-T10 remain NOT AUTHORIZED.**

## 1. Authority record

| Item | Value |
|---|---|
| Clarification source | Direct OWNER clarification to the Job On / P2-T04 authority (focused; normal Job On creation + duplication only) |
| Contract | `plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md` |
| Clarification section | **§23 — OWNER CLARIFICATION — JOB ON CONTEXT SNAPSHOT INVARIANT (SUPERSEDES THE AFFECTED DUPLICATION RULES)** (§23.1 normative rule, §23.2 superseded wording list, §23.3 deliberately-not-introduced) |
| Superseded wording | former §10.3 step 6 (verbatim frozen-triple copy; live Tool not re-read), former §10.5 bullet, former §21 Q16 pinned default (a), former §20.4 row DUP7, former §20.3 row CTX14 duplication reading — all marked superseded in the contract |
| Implementation SHA | `d2b3b3c` (P2-T04 OWNER CLARIFICATION implementation: service correction + focused tests + contract §23) |
| DMO-MODULAR remote `main` | `a96814f` before this work; post-push HEAD = the governance commit carrying this file (see the implementation task report) |

## 2. The rule (normative, as recorded in contract §23.1)

> Every new Job On creates new component context snapshots from the current canonical Tool
> identities selected for that Job On. This applies both to normal creation and duplication.
> Duplication reuses `tool_id` identities, never `cm_id`/`mf_id`/`bq_id` context identities.

- New `jobon_id` ⇒ new `cm_id` / `mf_id` / `bq_id` snapshots; context identities from another Job
  On are never reused (normal creation and duplication alike).
- The snapshot source is the CURRENT canonical Tool row at the moment the new Job On is created;
  the source context contributes ONLY the canonical `tool_id` needed to locate the Tool.
- The new snapshot uses the existing context schema and the existing snapshot composition logic —
  the accepted frozen set `tool_type` / `tool_reference` / `tool_lot` (§7.2). No new snapshot
  fields, no new table, no new column, no new domain concept.
- Historical contexts and the source Job On remain immutable (version, `updated_at`, contexts,
  frozen triples).
- P2-T07: new Job On ⇒ new `bq_id` snapshot; movements stay keyed by the `bq_id` of their own
  production; no old movement is migrated to a new `bq_id`.

## 3. What the implementation changed (focused, service-level only)

| File | Change |
|---|---|
| `src/DMO.Application/JobOn/JobOnService.cs` | `DuplicateAsync` now re-snapshots every duplicated context through `ResolveToolAsync(context.ContextType, context.ToolId.Value, …)` — the SAME snapshot path normal creation uses. The source context contributes only `tool_id`; a NEW `ToolContextSnapshot` is composed from the CURRENT canonical Tool row; the context identity is a fresh `Guid.NewGuid()`. The former verbatim frozen-triple copy is gone. |
| `src/DMO.Application/Repositories/IJobOnRepository.cs` | XML documentation of `DuplicatedAsync` updated (supplied contexts carry the service-built current-Tool snapshots). |
| `src/DMO.Infrastructure/Persistence/JobOnRepository.cs` | Class remarks + in-transaction comment updated; the repository still persists exactly the supplied duplicated contexts (no persistence change). |
| `src/DMO.Web/Pages/JobOn/Duplicate.cshtml` | Page comment and the operator hint updated to the current-state snapshot wording. |
| `tests/DMO.IntegrationTests/JobOn/P2T04TestStore.cs` | Test-double comment updated (the double persists the service-supplied contexts; it never snapshots or clones). |

No schema, migration, route, authorization, endpoint, model or repository behavior change.
`CurrentBuildAvailable` stays `[]`; no availability/navigation registration happened.

## 4. Test coverage (focused)

### 4.1 Always-run service-level rows (`tests/DMO.IntegrationTests/JobOn/JobOnEndpointsTests.cs`)

| Row | Invariant | Proves |
|---|---|---|
| SNAP1 | create from scratch | new cm/mf/bq context ids, each pointing at the selected canonical `tool_id`, frozen from the CURRENT Tool triple |
| SNAP2 | duplicate with a changed Tool | NEW context ids (A ≠ B per type), SAME canonical `tool_id`s, duplicate carries the CURRENT post-change values, source Job On + source contexts unchanged |
| SNAP3 | multiple duplications | same source duplicated twice ⇒ occurrences B and C with all-distinct context ids; same canonical `tool_id` |
| SNAP4 | P2-T07 identity regression | source `bq_id` ≠ duplicated `bq_id` while both reference the same canonical BQ `tool_id` |

### 4.2 DB-class rows over the disposable PostgreSQL (`tests/DMO.IntegrationTests/Persistence/JobOnDuplicationIntegrationTests.cs`, `ToolRepositoryIntegrationTests.cs`)

| Row | Replacement / addition |
|---|---|
| DUP7 | **Semantics flipped** (AC-61): after the live Tool changes reference/lot, duplication snapshots the CURRENT Tool state; the source context keeps its historical triple. |
| DUP17 | new: duplicating the same source twice ⇒ per-table context ids all distinct across source/B/C; same canonical `tool_id`s; source untouched. |
| DUP18 | new: source `bq_id` and duplicated `bq_id` distinct, same canonical BQ `tool_id`; exactly one BQ context per production. |
| CTX14 (ToolRepositoryIntegrationTests) | updated to the current-state reading: duplication snapshots the post-change triple; the source context is never rewritten; a later create freezes current values. |

## 5. Build / test results (exact, final runs)

| Suite | Passed | Failed | Skipped | Notes |
|---|---|---|---|---|
| `dotnet build DMO.slnx -c Debug` | — | 0 errors | — | 12 disclosed pre-existing analyzer warnings (P2T02/P2T06 protected files + PesoReview rows); 0 warnings in all changed/new code |
| Full unit suite (`DMO.UnitTests`) | 658 | 0 | 0 | unchanged baseline |
| Full integration suite (`DMO.IntegrationTests`, disposable PostgreSQL 16 container `dmo-disposable-pg`, never shared Supabase TEST) | 639 | 0 | 2 | the skips are the 2 pre-existing live-Supabase Auth tests (category B), unchanged |
| Focused Job On snapshot / duplication rows (SNAP1–SNAP4 + DUP7 + DUP17 + DUP18 + CTX14) | 18 | 0 | 0 | always-run + DB-class rows executed for real |
| P2-T04 regression rows (BND/RTE/DUP/CTX/JOB/DEP/ORC surface, `P2T04RegressionTests`) | 18 | 0 | 0 | subset of the full integration run above |
| P2-T07 identity regression (`Boquilhas` block incl. `P2T07RegressionTests`) | 67 | 0 | 0 | subset of the full integration run above |

## 6. Scope discipline

- P2-T04 is NOT reopened as a whole; only the duplication snapshot path + documentation + focused
  tests changed.
- P2-T07 is NOT modified (no Boquilhas file, no migration 007, no route change).
- P2-T08 / P2-T10 NOT started.
- No availability registration; `CurrentBuildAvailable` stays `[]`.
- No new domain concept: no `production_id`, no `job_on_revision_id`, no rollover entity, no Tool
  history table, no snapshot version entity, no context reuse, no reverse arrays on Tool, no
  synchronization framework.
- The old source contexts, the old Boquilhas movements and every historical production remain
  exactly as they were.

## 7. Records

- Contract: `plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md` — §23 added; §10.3/§10.4/
  §10.5/§7.2/§20.3/§20.4/§21(Q16) updated and the superseded wording marked.
- Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` — §4 B1 row + §7 P2-T04 status block
  record the clarification and its verification.
- Workstream: `plans/beta-workstreams/P2-T04-DOMAIN-CORE-TOOL-JOBON.md` — §5.2 record.
- This response is the focused clarification record.

**NEXT GATE:** one focused independent review of this snapshot invariant.