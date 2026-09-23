# P2-T05 — Controlo Create + Controlo_Create → Definições — IMPLEMENTATION RESPONSE

**Implementation status:** `IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT
IMPLEMENTATION REVIEW`.
**B2 status:** `RESOLVED — PLAN ACCEPT` (correction re-review ACCEPT
`f54ac15a96797a0dd0c51cf85b9b179e16be4da3` / `ceb9ee9…`, dmo-work).
**P2-T05 is NOT closed. P2-T06 / P2-T07 / P2-T08 / P2-T10 remain NOT AUTHORIZED.**

## 1. Authority record

| Item | Value |
|---|---|
| Contract | `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` (accepted; 66 AC ↔ 84 matrix rows) |
| Architect plan review | `256081fae43d4192b879b65fca0bb43efe8cdbca` (dmo-work) — PLAN REJECT — C1–C4 only; Q-PDF resolved: ACCEPT DEFAULT (server-host filesystem configuration with server-side accessibility check) |
| Architect correction re-review | `f54ac15a96797a0dd0c51cf85b9b179e16be4da3` / `ceb9ee9…` (dmo-work) — **PLAN ACCEPT** (authorizes P2-T05 implementation) |
| Authoritative data rule | `dev/rulings/P2_T05_PESO_APPROVAL_PDF_AUTHORITATIVE_DATA_RULE.md` (dmo-work `c2b68a8…`) — Peso record = single source of truth; PDF is a derived artifact |
| Peso identity rule | `dev/rulings/P2_T05_PESO_IDENTITY_RULE.md` (dmo-work) — one `peso_id` per occurrence; same id across edits/resubmits |
| Identity verification | `reports/P2_T05_COMPONENT_CONTEXT_IDENTITY_VERIFICATION.md` — PASS; P2-T05 mints no `cm_id`/`mf_id`/`bq_id` |
| Implementation SHA | `092743a97da2fe6b90dac04d69a9a3986f0997d7` (see §12) |
| DMO-MODULAR remote `main` (start) | `b57eb1e…` (clean tree at start) |

## 2. Identity discipline (the hard rules, as built)

- **`peso_id` is created only by the backend** inside the create transaction (`PesoId.New()`,
  PID4); it appears in no request with creation meaning.
- **Anchoring is exclusive and DB-enforced**: `pesos_anchor_check
  ((cm_id IS NULL)::int + (tool_id IS NULL)::int) = 1`. Production Pesos anchor to the REAL
  P2-T04 `cm_id` (`FK_pesos_cm_contexts_cm_id RESTRICT`); pending Pesos anchor truthfully to
  `tool_id` (`FK_pesos_tools_tool_id RESTRICT`) and are displayed `Job On por associar` (a
  context condition, never an error).
- **No new `cm_id`/`mf_id`/`bq_id` is ever minted by P2-T05**: the only context-adjacent write
  (route 11, missing CM) composes `IJobOnService.UpdateAsync` (Keep all facts + Set the CM slot),
  so the `cm_contexts` row is created/updated by Job On's own application/repository code
  (§21.4). The sole cross-stream additive member on the Job On application contract is the
  accepted Q-CAND `IJobOnService.ListPesoAssociationCandidatesAsync(toolId)` (§20.4.2).
- **`pesos` carries NO `jobon_id`/`reference`/`production_number`/`machine` columns**:
  production labels flow through documented traversal `peso_id → cm_id → jobon_id`
  (§6.2). No `production_id`, no `job_on_revision_id`, no approval copy, no second snapshot,
  no jsonb.
- **Rows are an aggregate part of the Peso** (the domain `Peso` embeds
  `IReadOnlyList<PesoMeasurementRow> Rows`, the accepted P2-T04 `JobOn(Contexts)` pattern —
  reader: ***the contract is silent on the domain record shape; the user (Architect) confirmed
  the aggregate-embedded representation on 2026-09-23 during implementation***), while the
  repository write methods keep the contracted separated signatures
  `CreatedAsync(Peso, IReadOnlyList<PesoMeasurementRow> rows, …)`.

## 3. Authoritative-data and snapshot semantics, as built

- The persisted `pesos`/`peso_measurement_rows` rows are the single source of truth for the
  P2-T06 handoff and the future P2-T08 PDF generation (authoritative data rule): the same
  `peso_id` is submitted with backend attribution (`submitted_at`/`submitted_by_user_id`,
  status stays `pendente`); no approval copy, no PDF-facts duplicate model, no document table.
- Frozen facts: per-row `capacity_cm3`/`glass_weight_g` are computed and persisted by the
  backend at save time (§5.3 formulas) and **never recomputed from current Tool/Job On state**;
  `glass_density_g_cm3` is written once at the first successful calculate/save and **never
  refreshed by later configuration changes** (§6.3.3 — the draft-edit path recomputes rows with
  the frozen density; a later density-mapping change affects NEW Pesos only — MES10).
- The CM context's frozen triple comes from the P2-T04 `cm_contexts` row and is presented
  separately from the live Tool projection (the page renders `data-dmo-frozen-triple` and the
  live `_ToolSummaryRow` as distinct facts).
- **Literal-shape reading (disclosed, the single place where the accepted composition rule
  could not be satisfied by shipped seams):** the contracted traversal
  `cm_id → tool_id → jobon_id` (§6.1, §7.2: "cmId → tool_id → processo", §26.3: "composed from
  cm_contexts + tools") cannot be composed from the shipped Job On application contracts (the
  ficha read is `jobon_id`-keyed and the accepted additive candidates read is `tool_id`-keyed;
  neither can resolve a bare `cm_id`, which the contracted calculate/create carriers require).
  P2-T05 therefore exposes one internal, read-only projection — `IPesoContextRead` /
  `DmoPesoContextRead` (`cm_id` → `jobon_id` + `tool_id` + frozen triple, single-table SELECT,
  no write, no creation, no identity minting). Every Job On production fact
  (reference/production number/machine) still comes from `IJobOnService` (AC-R2: one query
  chain; no second production lookup model). Tests: PID1/PID6/JRC7/Probe rows.

## 4. Migration identity

- **Migration:** `20260923045054_ControloCreateDomain` — the ONE new EF migration pair
  `src/DMO.Infrastructure/Migrations/20260923045054_ControloCreateDomain.cs` (+ `.Designer.cs`),
  generated with `dotnet ef migrations add ControloCreateDomain` through the accepted
  `DesignTimeDmoDbContextFactory`; `DmoDbContextModelSnapshot.cs` extended by EF.
- Migrations 001/002/003 (incl. Designers) are byte-identical (regression-asserted, BND8);
  `DmoDbContext.cs` is byte-identical (the repositories obtain their sets with
  `_context.Set<TEntity>()`).
- `Down` drops exactly the eight tables in referentially safe order (EF-generated).

## 5. Physical schema summary (verified against disposable PostgreSQL 16)

| Fact | Contract | Actual |
|---|---|---|
| Tables | 8 (`pesos`, `peso_measurement_rows`, `repairers`, `machine_repairer_assignments`, `pdf_directory_settings`, `email_lists`, `email_list_recipients`, `email_templates`) | 8 (verified via information_schema after applying all 4 migrations to the disposable DB) |
| CHECK constraints | 22 named | 22 (pg_constraint, exact names) |
| FKs | 7, ALL `ON DELETE RESTRICT` | 7, all `confdeltype='r'` (16/16 product FKs RESTRICT overall; the only cascade in the schema is the pre-existing foundation `FK_template_modules_templates_template_id`) |
| Unique keys | 6 (position, machine, singleton, list name, list+address, template name) | present |
| Indexes | `IX_pesos_cm_id`, `IX_pesos_tool_id`, `machine_repairer_assignments_repairer_idx` (+ the unique keys) | present (+ two EF auto FK-supporting indexes on `pesos` user FKs — same EF behavior as the accepted P2-T04 baseline) |
| Concurrency | `version` tokens on `pesos`, `repairers`, `machine_repairer_assignments`, `pdf_directory_settings`, `email_lists`, `email_templates` (`.IsConcurrencyToken()`) | present |

Not created: any other table, seed/reference row, trigger, function, view, extension, sequence,
enum type, RLS policy or history statement (verified: no `InsertData`/`Sql(`/`CreateSequence`/
`AlterTable` in the migration; all eight tables empty after applying).

## 6. Interfaces / routes / policies

- `IControloCreateService` (calculate/create/get/update/submit/associate/candidates) and
  `IControloDefinicoesService` (the 18 settings members) over the exact §20.2 repository
  contracts (6 new flat interfaces) and the §26.1 closed result vocabularies.
- **Exactly the accepted 17 routes** (§21.3): 2 Razor pages (`/controlo/create`,
  `/controlo/create/definicoes`) + 15 minimal-API endpoints (`ControloCreateEndpoints`,
  `ControloDefinicoesEndpoints`). Route 8 is the stateless
  `POST /controlo/create/calculate` with **request-carrier identity only** (no path identity, no
  record resolution, never 404, no write, no version bump, no id allocation — C3/AC-R7).
- Every route/action — Create and Definições alike — declares exactly one policy:
  `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloCreate)`; the Razor
  `[Authorize]` constant is pinned in `ControloPolicyNames` and asserted equal to the canonical
  projection (AUT5). No route carries `controlo-approve`; ADMIN gains nothing (AUT3).
- **Interim runtime state (mandatory, §21.6)**: `ModuleRegistrations.CurrentBuildAvailable` is
  still `[]`; `DestinationRouteRegistrations` is untouched and empty; no availability entry, no
  route registration, no navigation entry was added. P2-T10 owns availability registration.
  Integration tests use test-only module registries.

## 7. Behaviors worth recording (exact readings)

- **Submit recompute-and-verify (§7.4 step 5):** the stored per-row results are re-derived from
  the persisted inputs with the Peso's FROZEN density and the current divisor configuration; any
  non-positive re-derived result is refused with the typed `RESULT_NON_POSITIVE` (C2), and a
  stored result that cannot be re-derived is refused with `calculation-configuration-missing` —
  nothing is silently rewritten and nothing is invented.
- **The draft-edit path** recomputes rows with the frozen density (§6.3.3); the anchor is never
  editable through the edit command (association is the dedicated pending-only action with
  `ASSOCIATION_MISMATCH` when the candidate `cm_id` does not resolve to the anchor `tool_id`).
- **`RESULT_NON_POSITIVE`** is refused before any write (validator-level for
  capacity/glass ≤ 0 and invalid divisor/density configuration) AND is the mapped backstop of
  the `peso_measurement_rows_capacity_check`/`peso_measurement_rows_glass_check` CHECKs
  (SQLSTATE 23514, §20.2) — the CHECKs are never weakened (MES11).
- **Calculation configuration (Q-CALC):** `IControloCalculationConfiguration`
  (`Controlo:Calculation` config sections) with **no** value shipped in the default
  configuration — until a deployment supplies divisor/density values, calculation-dependent
  operations return the typed `calculation-configuration-missing` (MES9). No value is invented.
- **PDF directory (Q-PDF):** server-host absolute path, single-row setting; the check executes
  server-side (`ServerHostPdfDirectoryProbe`) and returns exactly the typed §12.2 vocabulary
  (`not-configured | ok | directory-not-found | not-a-directory | access-denied | invalid-path |
  check-failed`), creating no documents (probe file created and removed inside the probe).
- **Email lists/templates** persist complete recipient sets with replace-all semantics in one
  transaction; no recipient address is hardcoded anywhere (SET8/AC-F6); template text is stored
  verbatim (no placeholder syntax — Q-PLACE).

## 8. Test matrix result

- 66 acceptance criteria / **84 test rows**: all 84 rows implemented (unit + integration +
  DB + static/rendered); the row↔AC key cross-check is reported in the targeted totals below
  (every row maps to the exact AC cited in the test's XML doc, per §27.4 discipline — the
  mechanical row↔AC audit is re-verified by the `DMO.UnitTests/ControloCreate` and
  `DMO.IntegrationTests/ControloCreate|Persistence` test blocks, which name the row id in every
  method).
- Targeted P2-T05: **119 tests — 119 passed / 0 failed / 0 skipped** (26 unit + 93 integration;
  integration with the disposable PostgreSQL connection set: P2T05RegressionTests 14,
  ControloCreateEndpoints 14, ControloCreateAccess 4, ControloDefinicoesEndpoints 5,
  ControloSurfaceRendering 4, Migration004 10, PesoRepository 22, ControloSettingsRepository 15,
  PesoJobOnDependencyProbe 5).
- DB-class rows ran for real against a **disposable local PostgreSQL 16** container
  (host-local `postgres:16`, `DMO_TEST_POSTGRES_CONNECTION`) — never the shared Supabase TEST;
  they are `[SkippableFact]` + `PersistenceTestDatabase.SkipIfNotConfigured()` and report as
  environment-gated skips when the variable is unset (no test weakened).
- Existing tests: the full P2-T04 suite and the protected regression suites keep passing
  (see §10).

## 9. Disclosed changes outside the P2-T05 owned paths (all sanctioned)

1. **The Q-CAND additive member** (P2-T05 contract §20.4.2, Architect ACCEPT): one read-only
   member on the Job On application contract —
   `src/DMO.Application/JobOn/IJobOnService.cs`, `JobOnService.cs`, `JobOnModels.cs`
   (`ListPesoAssociationCandidatesAsync`; `PesoAssociationCandidate` record;
   `JobOnResult.AssociationCandidates`). Consequences on P2-T04-accepted test pins, disclosed:
   - `tests/DMO.UnitTests/JobOn/JobOnContractTests.cs` — `DUP13`'s closed-surface count
     `7 → 8` (documented in the test).
   - `tests/DMO.IntegrationTests/JobOn/P2T04RegressionTests.cs` — `BND7` excludes exactly
     those three files (asserted non-vacuous: the files exist and carry the additive member).
2. **Shared-model-growth pins** (same single `DmoDbContext`, accepted precedent):
   `tests/DMO.IntegrationTests/MigrationRunnerTests.cs` (modelled tables 10 → 18, forbidden
   tables list pruned by the two P2-T04 entries that P2-T05 legitimately creates) and
   `tests/DMO.IntegrationTests/DatabaseConnectivityTests.cs` (3 → 4 migrations; 11 → 19
   tables); both remain registered in the accepted P2-T04 disclosed-additive list.
3. **P2-T04 scan helper extension** (`P2T04ProductionScan.cs`): the new P2-T05 test folders and
   env-gated test files are registered as new-surface (same treatment P2-T04 gave its own), so
   the P2-T04 "existing tests carry no vocabulary" rows keep scanning only the pre-existing
   surface.

## 10. Build / test results (exact, final runs)

| Suite | Passed | Failed | Skipped | Notes |
|---|---|---|---|---|
| `dotnet build DMO.slnx -c Debug` | — | 0 errors | — | 1 pre-existing analyzer warning in the protected `P2T02RegressionTests.cs` (xUnit2029, byte-identical file); 0 warnings in all P2-T05 code |
| Full unit suite (`DMO.UnitTests`) | **535** | 0 | 0 | baseline was 509 at the P2-T04 close; +26 P2-T05 unit rows |
| Full integration suite (with disposable PostgreSQL) | **478** | 0 | **2** | the 2 skips are the pre-existing live-Supabase Auth category-B tests (unchanged posture); with the DB configured, the DB-class rows run for real |
| Targeted P2-T05 (unit + integration, DB attached) | **119** | 0 | 0 | 26 unit + 93 integration (→ all 84 matrix rows implemented; several rows have both a U/I and a DB facet) |
| Migration/schema verification | PASS | — | — | all 4 migrations applied to the disposable PostgreSQL 16 (`dotnet ef database update` + MIG rows); live catalog: 19 public tables (18 product + history), 22 named Controlo CHECKs, 7/7 Controlo FKs `ON DELETE RESTRICT` (`confdeltype='r'`; the only non-RESTRICT product FK is the pre-existing foundation cascade `FK_template_modules_templates_template_id`), 6 unique keys, declared indexes present, 0 seed rows, Down = exactly the 8 tables, re-run idempotent |

## 11. Negative-scope proofs (summary of the built-in evidence)

- No `production_id`, no `job_on_revision_id`, no `rascunho` — PID5 static scan + schema scan.
- No approval/decision/review table, route, type or presentation — BND2 (the three-value status
  vocabulary is the only sanctioned approval vocabulary).
- No Boquilhas aggregate/movement/resolution/sidebar/assignment-history — BND3 + MAC6.
- No PDF generation/email send/availability read/routing/placeholder code — BND4 (the directory
  probe's own probe-file IO is the contracted exception).
- No duplicate Tool/JobOn/CM registry, no reverse arrays, no fake identities — BND5.
- No machine registry / `machine_id` / line grouping — BND6 + MAC6 (six independent machines).
- No `previous_peso_id`, no Comparação/Pegamentos/Folha/Resumo identity — BND7 (Q-SCOPE).
- Migrations 001/002/003 + `DmoDbContext.cs` byte-identical; shared/frozen artifacts untouched —
  BND8.
- Exactly one new migration owns exactly the eight contracted tables — BND9 + MIG rows.
- `CurrentBuildAvailable` stays `[]`; Definições is not a destination; no route registration —
  BND1.
- Fixed desktop: no breakpoint rule, no width listener, no card conversion, no required-column
  hiding, no action relocation — LAY1/LAY2/LAY3 (AC-N1).

## 12. Commits / repository state

- Implementation commit: `092743a97da2fe6b90dac04d69a9a3986f0997d7` (implementation + tests +
  migration + governance status updates; 105 files, +21120/−43).
- This response is the follow-up governance commit `f4e81da1f714cb473cce6f336169cf28dabd8e88`
  (response record) with the SHA-record follow-ups `db797728028e762d373f87b80e072393e817dc5e`
  and `1bbbedf6a28ae61aec2b5cd05f566cf7e22854cb` (see §1 table note: the P2-T04 discipline —
  the implementation response is recorded in its own governance commit after the
  implementation commit, so the response can carry the implementation SHA).
- Remote `origin/main` after push: `1bbbedf6a28ae61aec2b5cd05f566cf7e22854cb` (all commits on
  top; reachability verified: `git merge-base --is-ancestor 092743a origin/main` exits 0).
- No force push; working tree at the end: CLEAN.

## 13. Governance updates applied

- `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md`: §4 B2 rows, §7 P2-T05 status block and
  Appendix A — B2 → `RESOLVED — PLAN ACCEPT`; P2-T05 → `IMPLEMENTED — AWAITING INDEPENDENT
  VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW`.
- `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` §5.1: same record.
- P2-T05 is NOT closed; P2-T06/T07/T08/T10 remain NOT AUTHORIZED.