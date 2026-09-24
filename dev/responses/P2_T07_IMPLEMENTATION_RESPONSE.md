# P2-T07 — Boquilhas — IMPLEMENTATION RESPONSE

**Implementation status:** `IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT
IMPLEMENTATION REVIEW`.
**B3 status:** `RESOLVED — PLAN ACCEPT` (focused Architect B1 re-review
`7c2479ebae50f8fe18a770fd373cffaae65a48e1`, dmo-work).
**P2-T07 is NOT closed. P2-T08 / P2-T10 remain NOT AUTHORIZED.**

## 1. Authority record

| Item | Value |
|---|---|
| Contract | `plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md` |
| **Corrected contract SHA** | `b884dd8462b00010710d4f777fd721efc7dcc692` (the reviewed/authorized object) |
| Original contract SHA | `dcca794685c03969d97295887e57e0f60e0bd7d8` (authoring; SHA follow-up `d548c33…`) |
| Original Architect PLAN review | `dev/reviews/P2-T07_BOQUILHAS_CONTRACT_PLAN_REVIEW.md` @ dmo-work `542a08a1bcf1340306f8e337a6c597580a921f3d` — **PLAN REJECT, blocking finding B1 only** |
| **Focused Architect PLAN re-review** | `dev/reviews/P2-T07_BOQUILHAS_B1_FOCUSED_PLAN_REVIEW.md` @ dmo-work `7c2479ebae50f8fe18a770fd373cffaae65a48e1` — **PLAN ACCEPT**; blocking findings NONE |
| Implementation authorization | **AUTHORIZED — P2-T07 ONLY** (against `b884dd84…` only; per the focused re-review) |
| DMO-MODULAR remote `main` (start) | `b884dd8462b00010710d4f777fd721efc7dcc692` (clean tree at start) |
| Implementation SHA | see §16 (commits) |

## 2. Migration identity

- **Migration:** `20260924031924_BoquilhasDomain` — the ONE new EF migration pair
  `src/DMO.Infrastructure/Migrations/20260924031924_BoquilhasDomain.cs` (+ `.Designer.cs`),
  generated with `dotnet ef migrations add BoquilhasDomain` through the accepted
  `DesignTimeDmoDbContextFactory`; `DmoDbContextModelSnapshot.cs` extended by EF.
- Migrations **001–006 (incl. Designers) are byte-identical** (git-diff verified); `DmoDbContext.cs`
  is **deliberately NOT modified** (the repositories obtain their sets with `_context.Set<TEntity>()`).
- **One documented post-EF edit** (recorded in the migration file): EF's model consolidation folds
  the contracted single-column FK-supporting/traversal indexes `IX_boquilhas_bq_id` /
  `IX_boquilhas_tool_id` into the partial unique indexes on the same columns, while §7.5 requires
  both — the migration re-declares the two plain indexes explicitly (physical facts owned by the
  migration; the EF model snapshot deliberately carries only the unified model — a documented seam
  for any future migration).

## 3. Physical schema summary (verified against disposable PostgreSQL 16.15)

| Fact | Contract | Actual |
|---|---|---|
| Tables | 6 new (`boquilhas`, `boquilha_machines`, `boquilha_movements`, `boquilha_movement_audit`, `boquilha_close_snapshots`, `boquilha_reopenings`) | 6 — post-migration raw count 27 (**26 product tables + `__EFMigrationsHistory`**; the contract's "21 current" narrative counts product tables only — the repo held **20** product tables before P2-T07, a descriptive miscount of the same class as the N-1 observation of migration 005; the physical counts are recorded by MigrationRunnerTests/DatabaseConnectivityTests/migration rows) |
| CHECKs | 16 named (§7.4) | 16 exact names/expressions (incl. `boquilhas_anchor_exclusive_check`, `boquilha_movements_saida_required_check`, `boquilha_movements_entrada_facts_check`) |
| FKs | 14, ALL `ON DELETE RESTRICT` | 14, all `confdeltype='r'` |
| **Partial unique indexes (B1)** | `IX_boquilhas_active_bq_id` (bq_id, `WHERE status='active' AND bq_id IS NOT NULL`) + `IX_boquilhas_active_tool_id` (tool_id, same shape) | both present with the exact predicates (pg_indexes verified) |
| Query indexes (§7.5) | 8 declared + the 2 backstops | present (incl. the two re-declared traversal indexes; the EF auto FK-supporting indexes on the user/repairer FKs are the accepted EF baseline behavior) |
| Concurrency | `version` tokens on `boquilhas` + `boquilha_movements` (`.IsConcurrencyToken()`, default 1) | present |
| Down | removes ONLY the six P2-T07 tables; re-apply idempotent | verified with the REAL EF migrator (`MigrateAsync` to migration 006 and back) |

## 4. Routes / policies

- **Exactly 18 routes = 3 pages + 15 endpoints** (§13.2): `Pages/Boquilhas/Index` (Registo),
  `Novo`, `Historico` + the 15 minimal-API endpoints of `BoquilhasEndpoints` (`/boquilhas/aggregates`,
  `/aggregates/{id}`, `/aggregates/{id}/movements/{movementId}/audit`, `POST /aggregates`,
  `POST /aggregates/{id}/movements`, `PUT /aggregates/{id}/movements/{movementId}`,
  `POST /aggregates/{id}/close`, `POST /aggregates/{id}/reopen`, `PUT /aggregates/{id}/opening-facts`,
  `GET /productions`, `GET /jobons/{jobonId}`, `POST /jobons/{jobonId}/bq-association`,
  `GET /machine-assignments`, `GET /repairers`, `GET /history`). Route count asserted by test
  (15 handler registrations + 3 pages).
- **Authorization:** every route/action carries exactly
  `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas)` = `dmo.module.boquilhas`;
  the Razor `[Authorize]` constants are pinned in `BoquilhasPolicyNames` and asserted equal to the
  canonical projection (A1). No sibling grant satisfies a P2-T07 route (test A2/A3); ADMIN is
  denied (A4); direct-route denial is server-side (A3); denial is never an empty surface (A5).
- **Interim runtime state:** `ModuleRegistrations.CurrentBuildAvailable` stays `[]`;
  `DestinationRouteRegistrations` untouched; `AccessResolver` denies every P2-T07 route until
  P2-T10 registers availability; integration tests use test-only registries (N4).

## 5. Identity model

- **Production-linked:** `boquilhas_id → bq_id → jobon_id + tool_id` — `bq_id` is a REAL
  `bq_contexts` row (`BQ_CONTEXT_NOT_FOUND` refused otherwise; FK RESTRICT backstop;
  `BoquilhasDependencyProbe` reports `boquilhas-aggregate` dependencies on Job On deletion).
- **Standalone:** `boquilhas_id → tool_id` — a REAL canonical BQ Tool (`TOOL_NOT_FOUND` /
  `TOOL_TYPE_MISMATCH` refused otherwise); zero fake Job On/bq rows (tested).
- **DB-enforced exclusive anchor:** `CHECK ((bq_id IS NULL)::int + (tool_id IS NULL)::int) = 1`.
- No `production_id`, no `job_on_revision_id`, no reverse arrays, no per-piece UUID, no client-minted
  id (static scans + carrier-shape tests). `boquilhas_id`/`movement_id` are backend-allocated inside
  their write transactions.

## 6. Shared Tool orchestration

- The P2-T04 shared search/select/create orchestration is consumed unchanged: candidates are BQ
  Tools only (type filter test T1), a single candidate is NEVER auto-selected (rendered T2 + node
  scenarios S7), ambiguity stays explicit (S9/T6), the contextual `Criar ferramenta` posts the
  `ferramentas`-gated P2-T04 route and returns the canonical `tool_id` restoring the SAME origin
  state (T3, node S8/S9), with no second Tool picker/registry (T4/R4 scan + route scan).

## 7. Movement / balance / edit model

- **Exactly four write types** `inicio|saida|entrada|irreparavel` (CHECK + enum + tokens; `Editar`
  is an action, never a type — rendered + scan + carrier-shape proofs).
- **Início is created WITH the aggregate** in the create transaction; later `inicio` appends are
  refused `only-one-inicio` (V4).
- **Balance is derived by replay only** (`recorded_at ASC, movement_id ASC` — physical receipt
  order): the four buckets; Saída ≤ Disponível and Irreparável ≤ Em reparação as in-transaction
  typed refusals (nothing written); excess Entrada recorded in full with the per-row
  `expected_return_quantity`/`excess_received_quantity` facts (CHECK-consistent); negative saldo
  visible and non-blocking; zero balance never deletes the aggregate. No balance table/column
  exists anywhere (schema + static scans).
- **Edit** replaces the SAME `movement_id` (row count unchanged), writes ONE audit row in the same
  transaction (exact before/after of quantity/business_date/machine/repairer_id/observations +
  backend actor/time), re-validates by replay of the before-state (N2: an Entrada edit recomputes
  expected/excess from the replay before-state with the new quantity), never inserts a second
  quantity event and never double-counts (E1–E6 tests).
- **Dates:** `business_date` operator-editable; `recorded_at` written once and byte-identical
  across edits; `edited_at`/`closed_at`/`reopened_at` are separate backend timestamps.

## 8. Concurrency (incl. the B1 correction)

- Aggregate `version` + movement `version` tokens, in-transaction compares, and the accepted
  `SaveAsync` → `ConcurrencyConflictExceptionMapping` for save-time races (K4 real-PG
  interceptor test): staleness is always 409 `stale-version` with zero rows written.
- **Race-safe one-active-per-anchor backstop (B1 correction):** the application pre-check
  (`HasActiveAggregateForAnchorAsync` inside the create/reopen transactions) is UX only; the DB
  partial unique indexes `IX_boquilhas_active_bq_id` / `IX_boquilhas_active_tool_id` are the
  concurrency authority. A 23505 on EXACTLY those two index identities maps in the repository to
  `Refused(ActiveAggregateExists)` (409) via `PostgresException.SqlState`/`ConstraintName` —
  **no other 23505 source maps to this domain result** (unrelated unique violations, e.g. the
  machines key, map to their own validator token).
- **K6/K7/K8 executed against the REAL disposable PostgreSQL** (two genuine concurrent
  transactions): exactly one winner, exactly one `ActiveAggregateExists` loser, one active
  aggregate, one committed Início, machine rows only for the winner, zero orphan/partial state;
  the create-vs-reopen loser keeps the aggregate `closed` with its close snapshot and history
  intact and NO partial reopening record.
- Every guarded mutation bumps the aggregate version exactly once (append/edit/opening-facts/
  close/reopen) so concurrent writers always lose with `stale-version` or the typed refusal —
  no movement can land on a closed trace, and a close always replays the committed ledger.
- Conflict presentation: the page-owned adapter enters the accepted `conflict` state with the
  explicit "Recarregar estado atual" recovery (D2), exactly ONE request per refusal, observed
  versions refreshed only on success (node scenarios S3–S6).

## 9. Repairer resolution & historical preservation

- Consumed reads only: `IMachineRepairerAssignmentRepository.ListAsync` +
  `IRepairerRepository.ListAsync` under the `boquilhas` gate (routes 16/17); **no member added to
  the closed contracts**; the six machines resolve independently (R1/R2).
- `assignment-unavailable` (absent row) is an explicit non-error state rendering manual selection
  (R6); the saved value is always the human-confirmed `repairer_id` of the command.
- The movement row stores the final selected `repairer_id` + `machine` (required on external
  Saída, CHECK + typed tokens); a later assignment change never rewrites earlier movements (R3
  real-PG test). No repairer/assignment administration exists anywhere (N1/R4 scans).

## 10. Close / reopen

- Close and reopen operate on the SAME `boquilhas_id` (row count constant across cycles; I5/I6/C8);
  the immutable close snapshot (buckets at close, utilisation still, initial quantity, opening date,
  backend `closed_by`/`closed_at`) is written atomically with the status flip — a failed close (K3
  injection) leaves `active` with no snapshot and no version bump; later movements/edits never
  modify a snapshot; each close writes a NEW snapshot row.
- Reopen eligibility is exact (closed ∧ last close among aggregates sharing the anchor ∧ no other
  active aggregate shares the anchor) with typed refusals writing nothing (C6); actor/time/reason
  are backend facts with the required non-blank reason (C5); the reopen row references the exact
  close being reopened.

## 11. Utilisation

- `% utilização` is the single manual `utilisation_percent` fact (0–100 or NULL; CHECK), entered
  at opening, edited via the opening-facts route (aggregate context only — never touches
  movements/balance), captured as-is in the close snapshot, rendered as a NUMBER never a progress
  bar (U1–U3), with the standard aggregate-version concurrency and the refused-when-closed rule.

## 12. Local Histórico

- HISTÓRICO (local) inside the module — no `historia` route/entry/registration (H2/N3 scans).
- Every §24.2 filter is a backend SQL predicate (reference/lot traversal through the frozen
  `bq_contexts` triple or the live `tools` row; machine-set membership; business-date period /
  movement type / repairer EXISTS predicates); unknown/ill-formed values → 400 `FILTER_INVALID`;
  paging 1-based with the deterministic technical order and a backened-counted `total` (H1/H5).
- **Traversal implementation note (review observation N1, resolved):** the History/ficha
  traversal reads follow the accepted read-only entity-set composition pattern (the P2-T06
  `PesoReviewRepository` precedent, review ACCEPT `4d88dbe`): `bq_contexts`/`tools`/`job_ons` are
  composed read-only inside the Boquilhas repository and the narrow
  `IBoquilhasContextRead`/`DmoBoquilhasContextRead` seam (the P2-T05 `IPesoContextRead` precedent,
  disclosed here) resolves the one `bq_id → jobon_id + tool_id + frozen triple` chain the closed
  P2-T04 application contracts cannot (the ficha read is `jobon_id`-keyed). No foreign writes, no
  cross-module HTTP. Both are additive disclosures recorded in the touched pin files.
- Table interaction is the accepted arbitration everywhere (single click selects, double click
  opens, actions OUTSIDE the tables): the lot grid, the Histórico table and the movement ledger
  (H3 rendered + node scenarios).

## 13. Fixed desktop / frontend

- Registo/Novo/Histórico are composed at the canonical 1366 × 768 fixed-desktop policy: region
  order stable, no `@media`/`@container`/`@supports` structural rule, no width listener, no card
  conversion, no required-column hiding, no action relocation (L1/L2 comment-stripped scans + the
  rendered rows); the pages render inside the existing shared shell (L3); the two new
  page-owned assets (`dmo-boquilhas.css`/`.js`) reuse the shared tokens and components, with no
  token re-declaration and no colour-literal system.

## 14. Consumed seams / additive disclosures (exact list)

1. `IBoquilhasRepository` + the **two additive read-only count members**
   (`CountListAsync`/`CountHistoryAsync`) required by §24.2/AC-H5 (`Total` = backend count for the
   same predicate; the P2-T05 Q-CAND additive-member precedent).
2. `IBoquilhasContextRead`/`DmoBoquilhasContextRead` — the narrow read-only `bq_contexts`
   traversal seam (the P2-T05 `IPesoContextRead` precedent, §12 above).
3. `BoquilhasDependencyProbe : IJobOnDependencyProbe` — the one additive registration line
   (contract §27).
4. **Pin-growth updates in the closed regression files** (each disclosed in-file and registered in
   the accepted allow-lists): `MigrationRunnerTests`, `DatabaseConnectivityTests`,
   `Migration003/004/005/006` (migration/table inventories grow to 7/27), `ToolRestrictionTests`
   (documentation only — the DbSet inventory is UNCHANGED because the Boquilhas repository uses the
   accepted `IQueryable` accessor + `_context.Set<T>()`-inline convention), `JobOnSourceModelTests`
   (the JOB23 disclosed EF-holder list gains the three Boquilhas persistence holders),
   `P2T04ProductionScan` (new Boquilhas test folders + persistence test files registered),
   `P2T04RegressionTests` (BND9/BND11/BND12/BND14 extended with the P2-T07 owned surface and the
   probe line), `P2T05ProductionScan` (the P2-T05 owned-surface extension with the P2-T07 paths).
5. The migration carries the two re-declared traversal indexes (§2) and the documented
   "21 current product tables" descriptive miscount is recorded (§3).

## 15. Test matrix result

- **83 acceptance criteria / 86 test rows** (the §29 audit: missing 0, dangling 0, orphan 0;
  K6–K8 map to the existing AC-C6/AC-C7/AC-K3/AC-K4). The row↔AC key is re-verified mechanically
  in this response (see the matrix inventory in the commit's test files: every method name starts
  with its row id and its XML summary names the AC).
- Targeted P2-T07: **35 unit + 80 integration** = 115 tests, 0 failed, 0 skipped (integration with
  the disposable PostgreSQL connection set; the 2 pre-existing live-Supabase category-B skips are
  the unchanged baseline posture).
- DB-class rows ran for real against a **fresh disposable local PostgreSQL 16.15** container
  (`postgres:16`) — never the shared Supabase TEST — incl. the migration cycle (reset → 7
  migrations → 27 raw tables → real Down → 21 raw tables → re-apply idempotent), the repository
  matrix (I/V/B/E/D/R/U/C/K1–K5) and the **K6/K7/K8 concurrent races**.

## 16. Build / test results (exact, final runs)

| Suite | Passed | Failed | Skipped | Notes |
|---|---|---|---|---|
| `dotnet build DMO.slnx -c Debug -t:Rebuild` | — | 0 errors | — | 10 disclosed analyzer warnings, ALL pre-existing in the protected P2-T02/P2-T06 test files (unchanged baseline); 0 warnings in all P2-T07 code |
| Full unit suite (`DMO.UnitTests`) | **644** | 0 | 0 | 609 P2-T06 close-baseline + 35 P2-T07 unit rows |
| Full integration suite (disposable PostgreSQL 16.15) | **646** | 0 | **2** | the 2 skips are the pre-existing live-Supabase Auth category-B tests (unchanged posture); with `DMO_TEST_POSTGRES_CONNECTION` set, every DB-class row ran for real |
| Targeted P2-T07 (unit + integration, DB attached) | **115** | 0 | 0 | 35 unit + 80 integration |
| K6 (production-linked create race) | 1/1 | 0 | 0 | REAL concurrent PostgreSQL transactions |
| K7 (standalone create race) | 1/1 | 0 | 0 | same |
| K8 (create-vs-reopen race) | 1/1 | 0 | 0 | loser closed with snapshot/history intact |
| Migration/schema cycle | PASS | — | — | apply clean → 26 product tables; Down (real EF migrator) removes ONLY the six; re-apply idempotent; migrations 001–006 + `DmoDbContext.cs` byte-identical |
| P2-T04 identity regression | green | — | — | full P2-T04 suites incl. the BND allow-list rows (extended with the P2-T07 owned surface) |
| P2-T05 settings/repairer regression | green | — | — | full P2-T05 suites incl. the BND9 owned-surface extension |
| P2-T06 regression | green | — | — | full P2-T06 suites (shared infrastructure touched only through the disclosed pins) |
| Frontend interaction tests | green | — | — | `dmo-boquilhas-adapter.behavior.mjs` (9 scenarios, real node engine): K5 conflict/recovery, T2/T6 never auto-select, T3 create/cancel origin preservation |
| Auth/access negative tests | green | — | — | A1–A6 rows (sibling grants, non-granted, ADMIN, direct-URL denial) |
| Negative-scope scans | green | — | — | N1–N7 + BND rows (no settings, no PDF/file/email, no `historia`, no availability, no fifth type, no second balance authority, protected files) |
| `CurrentBuildAvailable` | `[]` | — | — | `ModuleRegistrations.cs` untouched |

## 17. Negative scope (summary of the built-in evidence)

- No Boquilhas settings/Admin surface and no repairer/assignment administration (N1/R4).
- No P2-T08 mechanics: no PDF generation/filesystem/email/document identity (N2).
- No HISTÓRICO GLOBAL (`historia`) route/entry (N3/H2).
- No availability registration; `CurrentBuildAvailable` stays `[]`; no destination/route/navigation
  registration (N4).
- No fake machine/reference sidebar — the contextual panel reads REAL production context
  (production-linked) or the registered machine set only (N5).
- No fifth movement type, no legacy type, no annulment/delete path (N6/V1).
- Protected files byte-identical (N7/BND-B10); `DmoDbContext.cs` untouched.

## 18. Commit / repository state

- Implementation commit: see §16 of the report record (this response is carried by its own
  governance commit after the implementation commit, the accepted P2-T04 discipline).
- The implementation commit is reachable from remote `main`
  (`git merge-base --is-ancestor <impl-sha> origin/main` verified after the push); no force push;
  working tree CLEAN at the close.

## 19. Governance updates applied

- `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md`: §4 B3 rows → `RESOLVED — PLAN ACCEPT` /
  `B3 CLOSED as a blocker; P2-T07 IMPLEMENTED`; §7 P2-T07 status block → `IMPLEMENTED — AWAITING
  INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW`; Appendix A 9.13/9.14 rows → the same
  status record.
- `plans/beta-workstreams/P2-T07-BOQUILHAS.md`: §14 status + new §15 implementation record.
- This response: `dev/responses/P2_T07_IMPLEMENTATION_RESPONSE.md`.

## 20. STOP

P2-T07 implementation stops here: **no independent verification, no Architect implementation
review, no closure** were performed by this task. NEXT GATE: independent verification of P2-T07.
P2-T08 / P2-T10 remain NOT AUTHORIZED; `CurrentBuildAvailable` stays `[]`.