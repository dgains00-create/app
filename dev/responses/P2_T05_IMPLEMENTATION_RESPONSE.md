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
| Owner correction SHA | `66e7f0d` — WATER_TEMPERATURE_TO_WATER_DENSITY_LOOKUP (see §14) |
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

**Correction re-run (§14, after `66e7f0d`):**

| Suite | Passed | Failed | Skipped | Notes |
|---|---|---|---|---|
| `dotnet build DMO.slnx -c Debug` | — | 0 errors | — | same single pre-existing pinned analyzer warning (byte-identical `P2T02RegressionTests.cs`); 0 warnings in all correction code |
| Full unit suite (`DMO.UnitTests`) | **577** | 0 | 0 | baseline 535 at the §1–§13 close; **+42 new WDL rows** |
| Full integration suite (with disposable PostgreSQL 16) | **479** | 0 | **2** | the 2 skips are the pre-existing live-Supabase Auth category-B tests (unchanged posture); DB-class rows ran for real (`DMO_TEST_POSTGRES_CONNECTION`) |
| Targeted P2-T05 (unit + integration, DB attached) | **162** | 0 | 0 | 68 unit (ControloCreate folder; 26 prior + 42 WDL) + 94 integration (prior 93 + 1 rendered-surface WDL1) |
| Focused water-density lookup tests | **43** | 0 | 0 | `WaterDensityLookupTests` 42 rows + rendered-surface `WDL1` |
| Migration/schema verification | UNCHANGED | — | — | correction touches NO migration file, NO model configuration; the one-migration/8-table contract is untouched (see §14.6) |

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
  (response record) with SHA-record follow-ups (see §1 table note: the P2-T04 discipline — the
  implementation response is recorded in its own governance commit after the implementation
  commit, so the response can carry the implementation SHA).
- Remote `origin/main` at the implementation push: `092743a97da2fe6b90dac04d69a9a3986f0997d7` —
  the implementation commit is reachable from the current remote main
  (`git merge-base --is-ancestor 092743a origin/main` exits 0; verified after the final push).
- Correction commit: `66e7f0d` (implementation + tests; 13 files, +314/−123) — see §14. This
  response is the follow-on governance commit carrying the correction record (P2-T04
  response-commit discipline).
- No force push; working tree at the end: CLEAN.

## 13. Governance updates applied

- `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md`: §4 B2 rows, §7 P2-T05 status block and
  Appendix A — B2 → `RESOLVED — PLAN ACCEPT`; P2-T05 → `IMPLEMENTED — AWAITING INDEPENDENT
  VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW`.
- `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` §5.1: same record.
- P2-T05 is NOT closed; P2-T06/T07/T08/T10 remain NOT AUTHORIZED.

## 14. Focused Owner correction — WATER_TEMPERATURE_TO_WATER_DENSITY_LOOKUP (applied)

This section records the Owner clarification received AFTER the §1–§13 implementation close and
BEFORE independent verification, and its fully applied correction. Commit:
`66e7f0d` (implementation + tests), governance follow-up recorded below. **P2-T05 is NOT
closed; independent verification / Architect implementation review still has NOT happened.**

### 14.1 The Owner clarification (authoritative, applied verbatim)

For Peso/Controlo the operator enters ONLY the **water temperature**. The operator does NOT
enter water density, a divisor, or any manually selected calculation factor. The application
**automatically resolves the WATER DENSITY** corresponding to the entered temperature from the
application's authoritative water-temperature table:

```text
water_temperature → built-in water temperature table → water_density
capacity_cm3 = water_weight_g ÷ water_density_for_entered_temperature
```

The value previously described generically in the contract as "valor da tabela de temperatura"
/ "temperature divisor" is specifically **the water density corresponding to the entered water
temperature**. This is a different fact from **glass density** (resolved from the Tool/processo
mapping; used later for glass weight). The two density lookups remain strictly separate.

### 14.2 Authoritative numeric table — FOUND (no blocker; not invented)

Every source the task named was searched before any data change:

| Source | Finding |
|---|---|
| `dmo-master` (`modules/CONTROLO.md` §8.2/§8.5) | formula + "configured divisor" seam only; **no numeric table** |
| `dmo-beta-master` (`modules/CONTROLO_CREATE.md`) | formula + 5–35 °C range only; **no numeric table** |
| Legacy BA-DMO corpus (the contract's referenced historical evidence) | **THE TABLE**: `src/BA.Dmo.Domain/Modules/Peso/WeightCalculator.cs` — `WaterDensityByCelsius` (31 entries 5–35 °C, "TD-25", the single authoritative weight/volume engine) with `LookupDensity`; identical blob in every BA-DMO copy (hash-checked across BA-DMO, BA-DMO-CLEAN, -SOURCE-TMP, -wt-peso, -bb3065c6, -jobon-fix, -SMOKE, -wt, dmo-clean-v2; a second copy differs only in line endings, content-identical) |
| Shipped legacy web application (`DENSITY_TABLE` in `app-core.js`) | identical 31-entry table, including the **PORTAL_DMO 5.5.0 production distribution** and its packed bundles (`v5-operator.bundle.js`/`v5-manager.bundle.js`) |
| Legacy tests/spec | `WeightCalculatorTests.cs` (31-row density theory, rounding boundaries D1–D4) and `02_PESO_COMPLETE_SPEC.md` §4 ("devolve o valor da tabela `DENSITY_TABLE` para a temperatura arredondada (5–35 °C). Se fora do intervalo, erro.") |
| `CONTROLO_TECHNICAL_MODEL.md` §6/G5 | "Capacidade = PesoEmAgua ÷ densidade(água, temp)"; the water-density table is deterministic code (`LookupDensity`, divisor 5–35 °C); `TemperaturaC` persisted, **divisor deliberately not snapshotted** (G5: the stored record is the truth) |
| Manuals/reference files, migrations, archived material | `N06_peso.sql` and manuals repeat the formula/range; the values live in code (above) |

All independent copies agree on the exact 31 values (g/cm³, per whole degree):
`5→0.99888, 6→0.99885, 7→0.99882, 8→0.99877, 9→0.99871, 10→0.99863, 11→0.99854,
12→0.99844, 13→0.99832, 14→0.99819, 15→0.99805, 16→0.99789, 17→0.99773, 18→0.99765,
19→0.99737, 20→0.99717, 21→0.99696, 22→0.99674, 23→0.99652, 24→0.99628, 25→0.99603,
26→0.99577, 27→0.99551, 28→0.99523, 29→0.99494, 30→0.99485, 31→0.99435, 32→0.99403,
33→0.99371, 34→0.99339, 35→0.99305`.

### 14.3 Exact lookup behavior (as built)

- **Table granularity:** per WHOLE degree, 31 entries, 5–35 °C. The source defines **no
  interpolation** — none is implemented.
- **Rule (authoritative, from the legacy engine):** the entered temperature is rounded to the
  nearest whole degree with `MidpointRounding.AwayFromZero` (identical to the legacy
  `Math.Round`/`Math.round` for positive temperatures) and the exact 5–35 °C entry is used.
  Examples proven in tests: 20.4 → 20 → 0.99717; 20.5 → 21 → 0.99696 (never the midpoint);
  4.50 → 5 → 0.99888; 35.49 → 35 → 0.99305.
- **Shipment (smallest deterministic representation in the established architecture):**
  `ConfigurationCalculationConfiguration.AuthoritativeWaterDensityByCelsius` — the built-in
  authoritative table used automatically by the application. Deployment calibration is still
  possible through the backend calculation configuration section
  `Controlo:Calculation:WaterDensities` (entries `{Temperature, Density}`); a supplied section
  **replaces** the built-in table (exact fail-closed semantics — an entry it omits resolves no
  density; the built-in is never silently substituted under an override). The table is NOT:
  Peso-owned editable data, an operator field, a `Definições` area, a business identity, or a
  user-CRUD table.
- **Validation range preserved:** the accepted 5–35 °C request-validation range is unchanged
  (`TEMPERATURE_OUT_OF_RANGE` untouched; the `numeric(4,1)` CHECK untouched).
- **Fail-closed preserved:** an entered valid-range temperature for which no density can be
  resolved (e.g. a whole degree absent from a supplied override table) produces the accepted
  `calculation-configuration-missing` refusal — nothing written, nothing invented. Rounded
  degrees outside 5–35 fail closed at the lookup as defense in depth.
- **Vocabulary:** the resolution member is renamed `TryGetWaterDivisor` →
  `TryGetWaterDensity`; the API display fact `waterDivisorGCm3` → `waterDensityGCm3`. Transport
  tokens are unchanged (`calculation-configuration-missing`, `RESULT_NON_POSITIVE`, …).

### 14.4 Water density vs glass density — kept separate

1. **WATER DENSITY** — resolved from the entered water temperature via the authoritative table;
   used for capacity; never entered manually. 2. **GLASS DENSITY** — resolved from the Tool/
   processo mapping (`cm_id → tool_id → processo`; pending: `tool_id → processo`); used later for
   glass weight; frozen onto the Peso as `glass_density_g_cm3`. The two lookups remain separate
   facts in the calculation configuration and are proven independent in both directions
   (same temperature + different processo → same capacity, different glass weight; a missing
   glass mapping refuses the calculation without affecting the water lookup, and a missing water
   entry refuses it without touching the glass mapping).

### 14.5 Tests added (focused coverage)

- **Unit `WaterDensityLookupTests` (WDL1–WDL9, 42 rows)** against the REAL shipped
  `ConfigurationCalculationConfiguration`: (1) no density/divisor/factor member exists on any
  create/edit/calculate/row carrier; (2) the 31-entry authoritative theory (exact values);
  (3) rounding to the nearest whole degree with NO interpolation (20.5 → 21, midpoint never
  used); (4) capacity = water weight ÷ resolved density end-to-end (997.17 g ÷ 0.99717 →
  1000.0000 cm³ at 20 °C); (5) temperature change changes density and capacity as expected;
  (6) glass-density lookup independent from the water-density lookup; (7) a valid-range
  temperature absent from an override table fails closed
  (`calculation-configuration-missing`, nothing written); (8) unsupported degrees fail closed
  at the lookup + override replace semantics without silent fallback; (9)
  `RESULT_NON_POSITIVE` stays intact with the shipped configuration.
- **Integration rendered-surface `WDL1`** (`ControloSurfaceRenderingTests`): the operator
  surface renders exactly ONE water input — `Temperatura da água (°C)` (5–35) — and no
  water-density/divisor field: the workflow is exactly
  `[ Temperatura da água: ____ °C ]`.
- Existing P2-T05 rows updated only for the resolution-fact naming (MES3/MES9/MES11 comments
  and the echoed display fact) and all remain green.

### 14.6 Persistence / schema — UNCHANGED (explicit decision)

**NO schema change.** Rationale, per the task's persistence-review instruction:

1. The Peso already persists `water_temperature` plus the backend-derived frozen per-row
   results (`capacity_cm3`, `glass_weight_g`) and the frozen `glass_density_g_cm3`.
2. The accepted historical model (§6.3: stored results are never recomputed from current
   state; a later configuration change affects NEW Pesos only) makes the **frozen per-row
   results** the reproducible historical fact — exactly the legacy authority's conclusion
   (`CONTROLO_TECHNICAL_MODEL.md` G5: "o registo guardado é a verdade; recalculo é
   pré-visualização"; the legacy engine deliberately did **not** snapshot the water-density
   divisor, and §6.3.3 already equivalent-frosts the glass side).
3. The task's rule "DO NOT add a column merely because it seems convenient" applies: no current
   authority requires water-density snapshotting, and a table change in the future would still
   never rewrite stored Pesos. **Reported explicitly:** if full re-derivation of an historical
   Peso under a CHANGED future table were ever required (an audit proving the exact old
   density), that would need an explicitly justified additive migration — none is made here.
4. Consequence: the accepted **one-migration / 8-table contract is untouched** — no ninth
   table, no column, no new identity; `20260923045054_ControloCreateDomain` byte-unchanged.

### 14.7 Routes / availability — unchanged

No route, policy, availability entry or destination registration changed. `CurrentBuildAvailable`
stays `[]`. P2-T10 still owns availability registration. Negative-scope pins (BND1–BND9) are
unaffected (verified by the untouched-file set and the full suites).

---

## 15. Focused correction — D1 (submitted-view adapter crash) + D2 (stale-version recovery)

Applied after the independent verification (`reports/P2_T05_INDEPENDENT_VERIFICATION.md`, verdict
**NOT VERIFIED** with D1/D2 BLOCKING and D3/D4 NON-BLOCKING). This correction is **limited to the
page-owned JS adapter** (`src/DMO.Web/wwwroot/js/dmo-controlo.js`) and its regression tests; the
verification's other findings (D3 — dead `CALCULATION_CONFIGURATION_MISSING` region/typo; D4 —
blank Desvio cells in the calculate preview) are recorded as NON-BLOCKING and are **explicitly left
untouched by this task**.

### 15.1 D1 — root cause and fix

**Root cause (as verified):** `wireCreate` bound the `calculate`/`save`/`cancel` handlers with
unconditional `root.querySelector(...).addEventListener(...)` calls. The valid submitted Peso view
deliberately renders NO editable actions — only the disabled `submit` (decision bar
`Create.cshtml.cs` `BuildDraftRegions` submitted branch) — so loading/reloading a submitted Peso
threw `TypeError: Cannot read properties of null` on the missing elements and killed **every**
subsequent binding on that view (submit re-wire, cancel, associate). No test executed the adapter
JS, so the green suites could not see it.

**Fix:** a page-owned null-safe binding helper
`on(root, selector, event, handler)` — binds only when the target element exists — now used by
**every** single-element binding in the adapter (`wireCreate` calculate/save/submit/cancel/
associate and the Definições binders `wireRepairers` add, `wirePdfDirectory` save/check,
`wireEmailLists` create/update/edit-cancel, `wireEmailTemplates` create/update/edit-cancel). The
`forEach`-based bindings (rename/delete/cell rows) were already collection-safe. Absence of
editable controls in the submitted state stays VALID (nothing was restored to satisfy JS); the
`forEach` typed refusals and the fixed-desktop/no-breakpoint posture are untouched.

### 15.2 D1 — regression proof

1. **Rendered proof** (new, `ControloSurfaceRenderingTests.D1_SubmittedViewOmitsEditableActionsAndKeepsTheDisabledSubmit`):
   a real Peso is created and submitted through the REAL routes, then the page is opened in the
   submitted state: `data-dmo-action="calculate"`/`"save"`/`"cancel"` are ABSENT, exactly the
   disabled `submit` remains ("Submetido" + associated reason span), and the adapter's state
   region (`data-dmo-controlo-state`, `data-dmo-peso-id`,
   `data-dmo-submitted="true"`) is still rendered — the exact pre-fix crash condition, proven at
   the markup level.
2. **Behavioral proof** (new, node harness `dmo-controlo-adapter.behavior.mjs` executed by
   `DmoControloAdapterBehaviorTests` with a real JS engine over the REAL shipped adapter and a
   minimal DOM/fetch stub): scenario S1 loads the submitted view (editable actions absent,
   disabled submit, pending associate present) — the adapter **initializes without throwing**,
   the disabled submit never fires, and the still-present submitted-state controls (associate)
   keep working and surface the typed `already-submitted` refusal. Scenario S0 guards the normal
   draft surface (calculate + create-save success) against the refactor.
3. **Regression sensitivity (proven):** the same harness run against the PRE-fix adapter fails S1
   with exactly the reported TypeError ("Cannot read properties of null (reading
   'addEventListener')") — the test is a genuine regression, not a tautology.

### 15.3 D2 — root cause, recovery behavior and fix

**Root cause (as verified):** every guarded-mutation failure path called only
`renderErrors(root, response.body)`; the anchor `version` was refreshed only on success, so a 409
`stale-version` repeated forever on re-save with no reload/recovery affordance — violating §8.4
("`stale-version` → `conflict` presentation with reload/recovery"), §19.3 ("a refused mutation
reports the typed reason and **the surface reloads** (`conflict` presentation)") and freeze §4
(`conflict` = clear message + supplied recovery choices).

**Recovery behavior (as built, page-owned):**
- `renderMutationFailure(root, response)` — the single page-owned failure presentation of every
  mutation path: a typed `payload.reason === "stale-version"` enters `renderConflict`; every other
  typed failure (validation-failed, already-submitted, calculation-configuration-missing, …) keeps
  the existing `renderErrors` behavior **exactly** (verified: S6).
- `renderConflict(root, message)` — the conflict presentation on the page's own state region:
  clear heading ("Conflito — os dados foram alterados por outra ação; nada foi guardado."), the
  typed server message, and ONE explicit recovery action — a "Recarregar estado atual" button
  (`data-dmo-conflict-reload`) that reloads the authoritative current server state. **No
  automatic retry, no auto-merge, no overwrite of the newer server version, no silent replacement
  of local state** (the observed version is only refreshed on success, unchanged).
- Applied **consistently to every guarded mutation** the adapter handles that can receive
  `stale-version`: draft save (PUT), submit, associate, repairer rename, machine-assignment
  set/clear, PDF-directory save, email-list update **and** delete, email-template update **and**
  delete — all route through the same page-owned `renderMutationFailure`.
- `renderErrors` now removes any stale `data-dmo-conflict` marker so a later non-conflict failure
  never leaves a false conflict presentation behind.

### 15.4 D2 — regression proof (behavioral, node harness)

The harness drives the REAL adapter handlers with the REAL response shapes and proves, per guarded
mutation: (S2) save → 409 `stale-version` → conflict state visible (`data-dmo-conflict="true"`,
clear message, typed server message), exactly ONE request issued (no auto-retry), the observed
`data-dmo-version` is NOT overwritten, and the recovery control exists; clicking it calls
`location.reload()` (recovery reloads authoritative state). (S3) submit → same conflict/recovery.
(S4) machine-assignment set → same (settings surface). (S5) email-list update → same. (S6)
non-stale typed failures (400 `validation-failed` with token list; 409 `already-submitted`) keep
the existing `renderErrors` presentation — no conflict marker, no recovery control. S1 additionally
proves the D1 submitted-view behavior (§15.2).

### 15.5 Test totals after the correction (this run)

| Suite | Passed | Failed | Skipped | Notes |
|---|---|---|---|---|
| `dotnet build DMO.slnx -c Debug` | — | 0 errors | — | 1 pre-existing pinned xUnit2029 warning (byte-identical protected `P2T02RegressionTests.cs`); 0 warnings in correction code |
| Full unit suite (`DMO.UnitTests`) | **577** | 0 | 0 | unchanged (no unit changes needed) |
| Full integration suite (disposable PostgreSQL 16) | **481** | 0 | **2** | baseline 479 + the 2 new D1/D2 regression tests (rendered D1 + node harness wrapper); the 2 skips are the pre-existing live-Supabase Auth tests |
| Targeted P2-T05 (unit + integration, DB attached) | **164** | 0 | 0 | 68 unit + 96 integration (baseline 162 + 2 new) |
| D1 focused regression | **2/2** | 0 | 0 | rendered `D1_SubmittedView…` + behavioral S0/S1 scenarios (inside the wrapper) |
| D2 focused regression | **5/5 behavioral scenarios** | 0 | 0 | S2/S3/S4/S5 (conflict+recovery) + S6 (non-stale preservation) |
| Water-density focused | **43** | 0 | 0 | 42 `WaterDensityLookupTests` + rendered `WDL1` — unchanged, green |
| Schema | UNCHANGED | — | — | no migration/model/config file touched; one-migration/8-table contract intact |
| Negative scope / BND1–BND9 / LAY1 | green | — | — | inside the 481; the adapter gains no width listener/breakpoint token |
| `CurrentBuildAvailable` | `[]` | — | — | `ModuleRegistrations.cs` untouched |
| Working tree | CLEAN | — | — | at the close (only the correction commit pushed) |

### 15.6 Unchanged invariants (confirmed by the diff)

No domain code, no schema/migration, no route, no identity, no authorization, no water-density
behavior (31-value table, `AwayFromZero`, no interpolation, glass-density lookup), no fixed-desktop
structure, no availability registration was touched by this correction. The diff is exactly:
`src/DMO.Web/wwwroot/js/dmo-controlo.js` (adapter), the two new regression tests
(`ControloSurfaceRenderingTests` rendered D1 row; `DmoControloAdapterBehaviorTests` + the
`dmo-controlo-adapter.behavior.mjs` harness) and this response. P2-T05 remains
**NOT CLOSED**; D3/D4 remain recorded as NON-BLOCKING; the next gate is the quick independent
re-verification limited to D1/D2 + regression preservation, then the Architect implementation
review. P2-T06 / P2-T07 / P2-T08 / P2-T10 remain NOT AUTHORIZED.

## 16. Post-closure correction - GLASS-DENSITY CONFIGURATION (Owner rule) - IMPLEMENTATION RECORD

This section records the **focused implementation of the approved post-closure correction**:
the glass-density operational values now live in `Controlo -> Definições`
(`glass_density_settings`), per the Owner rule GLASS_DENSITY_CONFIGURATION; nothing else in
closed P2-T05 is reopened.

### 16.1 Authority chain

| Item | Value |
|---|---|
| Owner rule | GLASS_DENSITY_CONFIGURATION (dmo-work `dev/rulings/P2_T05_GLASS_DENSITY_CONFIGURATION_OWNER_RULE.md` @ `409ac24…`); `tool_id` carries ONLY the processo (NNPB/PS); `Controlo -> Definições` keeps the current operational density per processo; new Pesos resolve the current value and FREEZE it on `pesos.glass_density_g_cm3`; later changes affect only new Pesos; **no per-`tool_id` density exists** |
| Correction contract | `plans/contracts/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_CONTRACT.md` @ authoring commit `1c3f36e` (SHA-record follow-up `0436cdd`, DMO-MODULAR remote `main` before this task) |
| Architect PLAN review | dmo-work `dev/reviews/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_PLAN_REVIEW.md` — **PLAN ACCEPT** @ `4924982f…` (blocking findings NONE; observations N-1 migration ordinal, N-2 seed-version convention, N-3 evidence rows) |
| Closed P2-T05 baseline | accepted contract `b38993f`; CLOSED implementation `9fbfcf4`; Architect implementation review ACCEPT `ae99d1f…`; closure `3491097…` |
| Implementation authorization | **AUTHORIZED — focused correction only** (per the Architect PLAN ACCEPT; no P2-T06/07/08/10) |
| DMO-MODULAR remote `main` before this task | `0436cdd1097bd71f5956641c39fc2f46f0ada809` |

### 16.2 Migration number actually used and seed-version convention (N-1, N-2)

- **Migration number: the FIFTH migration overall** — `20260923122429_GlassDensitySettings`
  (+ `.Designer.cs`), generated with `dotnet ef migrations add GlassDensitySettings` through the
  accepted `DesignTimeDmoDbContextFactory`, applied AFTER
  `20260923045054_ControloCreateDomain`. Architect observation **N-1** confirmed the repo then
  held exactly four migrations (001–004); the contract's "sixth migration overall" wording was a
  descriptive miscount and was NOT preserved anywhere in code.
- **Seed version: 1 (the established sibling-settings convention)** — Architect observation
  **N-2**: contract §5.2 literally said "version 0", but EVERY sibling Definições settings table
  creates rows at `version DEFAULT 1` (migration 004: `email_lists`, `repairers`,
  `machine_repairer_assignments`, `pdf_directory_settings` all `defaultValue: 1`), and the
  repository convention is first-write-at-1 / version+1-per-committed-mutation. Seeding at 1
  makes the bootstrap rows indistinguishable from rows the settings surface itself would have
  created and produces the exact sibling stale-version behavior (a fresh surface sends
  `expectedVersion 1` -> writes version 2). No new concurrency model was invented: the seed rows
  carry the same `version integer NOT NULL DEFAULT 1` + in-transaction compare ->
  `ConcurrencyConflictException` -> 409 `stale-version` machinery as every other Definições
  settings row (the choice is documented here as N-2 required).

### 16.3 Schema delta (exactly one approved table)

`glass_density_settings` — a sibling of the five existing Definições settings tables:

| Column | Type / constraint |
|---|---|
| `processo` | `text NOT NULL`, **PRIMARY KEY**; CHECK `glass_density_settings_processo_check` = `processo IN ('NNPB','PS')` (named per contract; the Tool-owned two-value set, no FK, no process catalog) |
| `density_g_cm3` | `numeric(18,4) NOT NULL`; CHECK `glass_density_settings_density_check` = `density_g_cm3 > 0` — the SAME type/posture as the frozen `pesos.glass_density_g_cm3` (no rounding drift) |
| `version` | `integer NOT NULL DEFAULT 1`, concurrency token |
| `created_at` / `updated_at` | `timestamptz NOT NULL DEFAULT now()` |

No extra index (the PK covers the only query shapes), no `pesos`/`peso_measurement_rows` column
change, no other table, no availability/destination/navigation registration. The closed
"8-table P2-T05 schema / no ninth table" decision is superseded **to the exact extent of this
one table**; migration 004's eight `CreateTable` calls are byte-identical (BND2/BND8 still pin
004's eight tables and the protected files).

### 16.4 Seeded authoritative values and provenance

The migration inserts EXACTLY two rows (a deliberate, documented exception to the baseline's
"no seeds" posture, superseded to this exact extent by correction contract §5.2):

```
NNPB → 2.4027 (version 1)
PS   → 2.4231 (version 1)
```

Provenance (contract §4, verified in the PLAN review directly against the legacy tree):
`BA-DMO/src/BA.Dmo.Domain/Modules/Peso/PesoModuleCatalog.cs` (`public const decimal ConstantNnpb
= 2.4027m;` / `ConstantPs = 2.4231m;`), the legacy settings store `peso_settings`
(`constant_nnpb`/`constant_ps`, "fallback catálogo 2.4027/2.4231"), and the legacy UI
(`Responsavel.cshtml` placeholders `2,4027`/`2,4231`, step 0.0001). Recovered, **not invented**;
operator-editable afterwards; no generic catalog and no runtime fallback — a missing row
(defensive only, never reachable on the normal path) fails closed with
`calculation-configuration-missing` (409), nothing invented.

### 16.5 Route delta (exactly the two approved routes)

| # | Route | Behavior (as built) |
|---|---|---|
| 18 | `GET /controlo/create/definicoes/glass-densities` | both CURRENT operational values + versions — always exactly NNPB/PS in canonical order: `[{"processo":"NNPB","densityGcm3":…,"version":…},{"processo":"PS",…}]` (transport member exactly `densityGcm3`, correction §5.3 vocabulary) |
| 19 | `PUT /controlo/create/definicoes/glass-densities/{processo}` | body `{ densityGcm3, expectedVersion }`; updates ONLY that processo's row (per-processo independence); version-guarded -> 409 `stale-version`, nothing written, success returns the new row/version; refusals: 400 `validation-failed` with `PROCESSO_UNKNOWN` (path not NNPB/PS) and `DENSITY_NOT_POSITIVE` (value ≤ 0); defensive 404 `not-found` |

Both routes sit inside the existing Definições group and inherit the EXACT
`dmo.module.controlo-create` gate (`RequireAuthorization(ModuleAuthorizationPolicies.PolicyName(
ModuleCatalog.ControloCreate))`) — no new capability, no approval capability, no availability
registration (AUT1/AUT2/AUT4 + GD-I5 prove the gate incl. the approve-only denial). The
Definições page gained ONE section ("Densidade do vidro (g/cm³)") rendering the two editable
rows (step 0.0001, unit g/cm³ shown, save per processo, observed version carried on the input);
fixed-desktop posture, breakpoint-free stylesheet and the D2 conflict/reload presentation are
unchanged.

### 16.6 Calculation-source change (the only calculation delta)

- Glass density resolution switched source: at calculate/save/create/submit time the backend now
  reads the CURRENT OPERATIONAL value of the anchor's processo from `glass_density_settings`
  (through the new `IGlassDensitySettingsRepository`, the existing Definições repository
  pattern) via the `cm_id -> tool_id -> processo -> settings row -> density_g_cm3` chain.
- `IControloCalculationConfiguration.TryGetGlassDensity` was **removed** ("replaced by" the
  operational-store read, correction §5.4): the seam is now WATER-only and
  `ConfigurationCalculationConfiguration` no longer reads `Controlo:Calculation:GlassDensities`
  **anywhere** (GD-S2 scans the whole of `src` for the section literal in CODE and finds zero
  readers; the water override `Controlo:Calculation:WaterDensities` and the built-in 31-value
  table are untouched). No dual source of truth.
- **Freeze and immutability unchanged** (R4/R5/R6): the resolved value is written ONCE into
  `pesos.glass_density_g_cm3` at the first successful calculate/save; edit-recalculation and
  submit recompute-verify re-derive with the FROZEN value; no path rewrites a Peso's frozen
  density or stored per-row results; later settings changes affect ONLY new Pesos; Pesos
  created before the correction keep their frozen values untouched.

### 16.7 Frozen-history proof

- **Unit** (GD-U4/GD-U5): create freezes 2.4027 -> settings change (3.00) -> the SAME Peso's edit
  keeps the frozen density and row results, submit recompute-verify passes ONLY with the frozen
  value (a current-settings re-derive would mismatch), and a NEW Peso freezes the new 3.00;
  4-dp values round-trip verbatim (2.4099).
- **HTTP end-to-end** (GD-I6): real routes — save -> freeze 2.4027; PUT settings -> 3.00; the
  existing Peso's sheet keeps `glassDensityGCm3` 2.4027 and its frozen rows through edit and
  submit; a new Peso resolves 3.00. Water behavior unchanged in both generations (same
  temperature -> same capacity).
- **DB** (MES10 reworked): the real repositories over the disposable DB — the seeded row read
  2.4027/version 1, the Definições-style settings write bumps to 3.00/version 2, the existing
  Peso keeps the frozen 2.4027 and rows 2414.7135, a new Peso freezes 3.00 (rows 3015.0000).

### 16.8 Tests added (focused correction)

| Class | Rows |
|---|---|
| `GlassDensityCorrectionTests` (unit) | GD-U1 (resolution per processo, 2 cases) … GD-U6 = 7 cases: current-value resolution; per-processo independence; absent row -> fail-closed, nothing written; frozen-through-edit-and-submit; 4-dp exactness; water unchanged |
| `ControloDefinicoesValidatorTests` (unit, +2) | GD-V1 `PROCESSO_UNKNOWN` (no alias/other processo), GD-V2 `DENSITY_NOT_POSITIVE` (0/−), positive accepted |
| `GlassDensitySettingsEndpointsTests` (integration HTTP) | GD-I1 GET two rows/bootstrap; GD-I2 per-processo PUT independence both directions; GD-I3 stale -> 409 `stale-version`, nothing written; GD-I4 `PROCESSO_UNKNOWN`/`DENSITY_NOT_POSITIVE` exact tokens; GD-I5 approve-only 403 gate; GD-I6 frozen-history end-to-end |
| `GlassDensityCorrectionRegressionTests` (integration S) | GD-S1 migration = exactly one table, one `Down`, two seed rows, no later-workstream vocabulary; GD-S2 removed config section has no code reader; GD-S3 no per-`tool_id` density anywhere; GD-S4 `CurrentBuildAvailable` stays `[]` + single canonical gate |
| `Migration005GlassDensitySettingsTests` (DB) | GD-M1 fifth migration/20 tables (+1 only); GD-M2 source creates/drops exactly one table; GD-M3 physical shape/checks/PK/no extra index; GD-M4 exactly two seed rows NNPB 2.4027 / PS 2.4231 at version 1; GD-M5 DB CHECKs reject TOOL/0/−1; GD-M6 Down removes only the table (19 restored) + re-apply restores the exact two rows + no-op re-run |
| `GlassDensitySettingsRepositoryIntegrationTests` (DB) | GD-DB1 seeded state readable; GD-DB2 version-guarded write (+1, stale throws, PS untouched); GD-DB3 service-level refusals over real repositories |
| `dmo-controlo-adapter.behavior.mjs` (+2 scenarios) | S7 glass-density PUT stale-version -> conflict + recovery, exactly ONE request, observed version sent, comma decimal parsed; S8 validation-failed -> plain errors, no conflict marker/recovery |

Updated pins (intentionally superseded to the exact correction extent, each documented in-file):
`Migration003`/`Migration004` (four→five migrations, 19→20 public tables), `DatabaseConnectivity`
(5 applied), `MigrationRunner` (modelled set +1), `ToolRestrictionTests.TOL17` (DbSet set +1
settings entity), `BND5` (newest Designer declares the cm_contexts FK by type name),
rendering rows (SIX sections / five settings scroll containers, rendered NNPB/PS values), access
inventory (+routes 18/19), `P2T04ProductionScan` disclosed lists (+the two new persistence test
files), the seed-row restore helper `GlassDensityTestState` (canonical two-row state is restored
around every DB write test — the same discipline as the accepted settings-table clears).

### 16.9 Exact test totals (this run, disposable PostgreSQL 16)

| Suite | Passed | Failed | Skipped | Notes |
|---|---|---|---|---|
| `dotnet build` (src + both test projects) | — | 0 errors | — | 1 pre-existing pinned xUnit2029 warning (byte-identical protected `P2T02RegressionTests.cs`); 0 warnings in correction code |
| Full unit suite | **586** | 0 | 0 | 535 closed baseline + 42 WDL + correction additions; green |
| Full integration suite | **500** | 0 | **2** | green twice in a row (deterministic); the 2 skips are the pre-existing live-Supabase Auth rows |
| Targeted P2-T05 (unit + integration, DB attached) | **207** | 0 | 0 | 77 unit + 130 integration |
| Targeted correction (new rows only) | **28** | 0 | 0 | 9 unit (7 GD-U + 2 GD-V) + 19 integration (6 GD-I + 4 GD-S + 6 GD-M + 3 GD-DB) |
| Water-density focused | **43** | 0 | 0 | 42 `WaterDensityLookupTests` + rendered `WDL1` — **unchanged and green** (every WDL row kept, glass fixtures re-pointed to the store) |
| D1/D2 regression | **green** | 0 | 0 | S0–S8 behavioral scenarios (S7/S8 new for the glass-density PUT) + rendered D1 row |
| Schema/migration verification | **manual + DB rows** | — | — | apply clean on a fresh disposable DB; exactly one new table; exactly two seed rows; constraints/PK correct; `Down` removes ONLY the correction table (19-table state restored); re-apply recreates the exact two rows; re-run no-op; **no unrelated schema drift** (manual `dotnet ef` down/up cycle on a second disposable DB) |
| Negative scope / BND1–BND9 / LAY1 / AUT / TOL17 | green | — | — | inside the 500; the new sources carry no approval/PDF/email/Boquilhas/snapshot/legacy-identity vocabulary; adapter gains no width listener/breakpoint token |
| `CurrentBuildAvailable` | **`[]`** | — | — | BND1 + GD-S4; `ModuleRegistrations.cs` untouched |
| Working tree | CLEAN | — | — | at the close (only the correction commit pushed) |

### 16.10 Confirmations required by the task

- **Water-density behavior unchanged**: operator enters only the water temperature; the 31-value
  built-in table (5–35 °C, `AwayFromZero` nearest whole degree, no interpolation) and the
  `Controlo:Calculation:WaterDensities` override seam are byte-identical; glass and water
  density remain separate facts (WDL2/WDL3/WDL6 + the water suite green).
- **D3/D4 untouched**: no D3/D4 code or recorded decision was modified; both remain NON-BLOCKING
  carry-forward.
- **`CurrentBuildAvailable` remains `[]`**; no availability/destination/navigation registration.
- **No later workstream started**: P2-T06 (approval), P2-T07 (Boquilhas), P2-T08 (PDF/email),
  P2-T10 (availability) remain NOT AUTHORIZED and not implemented; GD-S1/BND2–BND4 scan the new
  sources for their vocabulary and find none.
- **Layer changes**: exactly one migration (005), one new table, two seed rows; `DmoDbContext.cs`
  and migrations 001/002/003 (incl. Designers) byte-identical (BND8 pins hold); DI gains one
  scoped registration; `Program.cs` unchanged.

**STOP — this record performs the focused implementation only. NEXT GATE: independent
verification of this focused correction, then the Architect implementation review.**