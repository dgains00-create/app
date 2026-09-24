# P2-T07 — Boquilhas — FINAL INDEPENDENT REVIEW (OWNER-CLARIFICATION CORRECTION)

**Workstream:** P2-T07 — Boquilhas (production movement register).
**Task class:** one independent verification of the OWNER-CORRECTED implementation against the
FINAL Owner model (contract §33). NOT a re-audit of the superseded active/closed design; the old
83/86 matrix was deliberately not reconstructed. **No implementation was modified.**
**Verdict:** **VERIFIED**
**P2-T07 FINAL STATUS RECOMMENDATION:** **CLOSED**

---

## 0. Verdict summary

| Item | Result |
|---|---|
| Owner-correction implementation SHA | `caad7927e933abc772d396fd85b7279b7376400a` (local `main` == `origin/main`; working tree clean at review start) |
| Governance/response SHA | `d00177dce68f41715fbf4127173a96228c0e2102` (direct child of `caad792`; records only) |
| Identity / production association | **PASS** |
| Three-movement vocabulary | **PASS** |
| Production-ended movement | **PASS** |
| Quantity replay | **PASS** |
| Entrada sem reparação semantics | **PASS** |
| Edit/audit | **PASS** |
| Repairer history | **PASS** |
| Schema simplification | **PASS** |
| Routes/access | **PASS** |
| Frontend/history | **PASS** |
| Negative scope | **PASS** |
| CurrentBuildAvailable | **PASS** — `[]` |
| Build | 0 errors (12 analyzer-style warnings, all in pre-existing/protected or test files) |
| Unit suite | **658 / 658 PASS** |
| Integration suite (disposable PostgreSQL 16.15) | **633 PASS / 0 FAILED / 2 SKIPPED** (the only skips: `DMO.IntegrationTests.Auth.LiveDevTestSupabase*` — pre-existing live-Supabase gated) |
| Focused Boquilhas (DB env) | **67 / 67** integration rows + 49 Boquilhas unit rows (inside the 658) |
| Migration apply / Down / re-apply | apply → 7 migration ids, 24 raw tables (23 product); real EF Down → 21 raw; re-apply ×2 → 24 raw (idempotent) |
| Migrations 001–006 + `DmoDbContext.cs` | byte-identical (`git diff` empty for every one of them) |
| Node adapter behavioral harness (shipped JS, 9 scenarios) | PASS (S1–S9) |

Under the simplified workflow, **no additional Architect implementation review is required** —
this review found no concrete architectural defect.

---

## 1. Reviewed objects / commit verification

| Item | Value |
|---|---|
| Final Owner model | Task brief (binding) + `plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md` §33 (OWNER CLARIFICATION §33.1 superseded list / §33.2 normative rule / §33.3 authorization) — read in full; implementation matches both |
| Implementation response | `dev/responses/P2_T07_IMPLEMENTATION_RESPONSE.md` §21 (correction record; §21.7 final corrected facts) — read; every reported fact independently reproduced below |
| Implementation diff surface | `git diff caad792~1 caad792` — exactly the Boquilhas domain/application/persistence/web/tests + the ONE corrected migration pair; `Documents/DmoDbContext.cs` NOT in the diff; migration `20260924031924_BoquilhasDomain` deleted, `20260924051151_BoquilhasDomain` added (pre-closure clean correction, no compensating legacy) |
| Protected files | migrations 001–006 + `src/DMO.Infrastructure/Persistence/DmoDbContext.cs` byte-identical vs `caad792~1` (empty `git diff` for each) and pinned by regression row `N7_ProtectedFilesRemainByteIdentical` |
| Source review | read completely: migration source, `MovementKind`/`BoquilhaRegister`/`BoquilhaMovement`/`OutstandingProjection` (domain), `IBoquilhasService`/`BoquilhasService`/`BoquilhasModels`/`BoquilhasValidator`/`BoquilhasReadModels` (application), `IBoquilhasRepository`/`BoquilhasRepository`/`DmoBoquilhasContextRead`/`BoquilhasDependencyProbe`/entities/configs (persistence), `BoquilhasEndpoints` + the three Razor pages + `dmo-boquilhas.js`/`.css` (web), `ModuleCatalog`/`ModuleRegistrations`/`Program.cs` (access/composition) |

---

## 2. Identity / production association — **PASS**

- Every register anchors a REAL `bq_contexts` row: `boquilhas.bq_id` NOT NULL, FK
  `FK_boquilhas_bq_contexts_bq_id` (RESTRICT) to `bq_contexts` → `jobon_id + tool_id`
  (`DmoBoquilhasContextRead` resolves the chain read-only). Confirmed in migration, entity,
  configuration, and in-transaction (`BoquilhasRepository.CreatedAsync` re-asserts
  `BqContextNotFound` inside the create transaction).
- No fake Job On, no fake `bq_id`, no `production_id`: false-identity tokens
  (`production_id`, `job_on_revision_id`, …) fail the negative-scope scan and the `I4` regression
  row; `AssociateBqAsync` composes ONLY `IJobOnService.UpdateAsync` (Keep + Set BQ slot) — the
  `bq_contexts` row is created by Job On's own code, never by a Boquilhas repository.
- One register per BQ context — final identity rule, DB-enforced by the **plain unique key
  `boquilhas_bq_id_key`** (no lifecycle state): rows `T1…T4` (real anchor, fake refused,
  no manufactured movement, duplicate refused) and persistence rows `P1`/`P2`, direct-SQL 23505
  evidence, all green. The superseded active-anchor B1 machinery is absent (see §9/§12).

## 3. Three-movement vocabulary — **PASS**

- `MovementKind` is exactly `Saida | Entrada | EntradaSemReparacao` with stored tokens
  `saida|entrada|entrada_sem_reparacao`; the DB CHECK `boquilha_movements_type_check` fixes the
  same closed set; appends parse via the closed token map and refuse anything else in-transaction
  with `MOVEMENT_TYPE_INVALID` (never a 500).
- No `Início`, no `Irreparável` anywhere in production code (unit rows
  `Tokens_AreExactAndClosed`/`NoLifecycleOrSupersededKinds_Exist`, endpoint row `T7` — superseded
  types refused, direct-SQL 23514 evidence). `Editar` is an action, never a type.
- Register creation manufactures NO quantity event: `CreateAsync` writes only the identity row
  (no movement), proven by `T3_RegisterCreation_DoesNotManufactureAMovement` and
  `P1_…CreationManufacturesNoMovement`.

## 4. Production-ended movement — **PASS**

- No code path rejects a movement because the production end date has passed: the append path
  checks only register existence + repairer existence + Saída-required facts; the production
  remains the historical context. `T5_AProductionEndedOn09027_StillAcceptsAMovementOn09029` and
  `P3_MovementsAfterTheProductionEndDateAreValid` (real disposable PostgreSQL) green.

## 5. Quantity replay — **PASS**

- Outstanding is **derived by replay at read time only**:
  `outstanding = Σ(Saída) − Σ(Entrada) − Σ(Entrada sem reparação)`
  (`OutstandingProjection.Replay`, applied to the ledger in physical receipt order
  `recorded_at ASC, movement_id ASC`; running per-movement `saldo` in the ficha). Never stored,
  no balance column/table (schema §9; `B1_NoSecondBalanceAuthorityExists` green).
- Owner example `Saída 10 / Entrada 4 / Entrada sem reparação 6 → 0` reproduced in three
  independent layers (unit `OwnerExample_ReplaysToZero`, endpoint `T9`, repository `R1`).
- Negative outstanding is a valid, visible, non-blocking projection: unit
  `NegativeOutstanding_IsAValidProjection`, endpoint `T10`, and the UI renders it with a
  negative-tone class (no validation, no CHECK, no presentation block).

## 6. Entrada sem reparação semantics — **PASS**

- A normal movement of the closed set: subtracts quantity from outstanding exactly like an
  Entrada (unit `EntradaSemReparacao_ReturnsQuantity`), remains **explicitly visible** in the
  ledger and Histórico rows (`IsEntradaSemReparacao` + distinct label/status), and **does not
  mutate the Tool row** (`E1_EntradaSemReparacaoDoesNotMutateTheTool` on real PG,
  `T11_…DoesNotMutateTheTool`).
- No permanent irreparable bucket/entity: no irreparable state on `tools` (the correction commit
  touches zero Tool files; migration 003 tools schema untouched), no bucket tables/columns
  (`BalanceTokens` scan + `B1` rows green).

## 7. Edit/audit — **PASS**

- Edit mutates the **SAME `movement_id`**, version-guarded: `EditMovementAsync` requires
  `ExpectedMovementVersion == Version` (service pre-check + repository in-transaction check +
  EF `IsConcurrencyToken` row guard), writes nothing on mismatch and surfaces 409
  `stale-version` (`T13` / `E3`; node S3/S4: no auto-retry, explicit reload recovery).
- Exactly **one audit row per edit** (`boquilha_movement_audit`) written in the SAME transaction
  with the exact before/after values of every editable field + backend editor/time — a separate
  append-only table, never a second quantity movement and never rendered as a movement
  (`T12_Edit_KeepsTheSameMovementId_NoDoubleCount_AuditPreserved`, `E2`, audit route 3 + UI trail).
- `recorded_at` (backend receipt timestamp) and `movement_type` are immutable — never carried by
  the edit command, never written by the repository UPDATE (`recorded_at` read back unchanged).
- `business_date` remains editable (carried in `EditMovementCommand`, audited before/after).

## 8. Repairer history — **PASS**

- New Saída: frontend resolves `machine → current assignment → suggested repairer` from the
  authoritative consumed read (`GetMachineAssignmentsAsync`/`GetRepairersAsync` — read-only
  composition over the closed P2-T05 contract); `assignmentUnavailable` stays blank (explicit,
  never an error, never a default); the operator keeps the final choice and the selected
  `repairer_id` is stored on the movement row.
- Historical movements store the repairer used at that time; a later assignment change does not
  rewrite old movements (`T14_LaterAssignmentChange_DoesNotRewriteTheMovementRepairer`, `R2` on
  real PG). Repairer existence re-validated in-transaction (`RepairerNotFound` →
  `T20_AppendWithAnUnknownRepairer_IsRefused`).
- Boquilhas administers no repairers/settings: no settings/admin tokens in the owned surface
  (`N1R4_NoSettingsOrRepairerAdministrationSurfaceExists` green; settings scan clean).

## 9. Schema simplification — **PASS**

- Final migration `20260924051151_BoquilhasDomain` creates **exactly three tables** —
  `boquilhas`, `boquilha_movements`, `boquilha_movement_audit` — with no other statement
  (source regex proof `MG1`, `3` CreateTable / `3` DropTable, zero AddColumn/DropColumn/
  InsertData/RenameTable/`WHERE`).
- Absent (verified in migration source, model snapshot, live `information_schema`/`pg_indexes`
  on the disposable DB, and `MG1/MG2/MG3`, `S1`): `boquilha_close_snapshots`,
  `boquilha_reopenings`, `boquilha_machines`, any `status` column (or any lifecycle column:
  no `tool_id`/`opening_date`/`utilisation_percent`/register `version`), and **no active-anchor
  partial unique indexes** (`IX_boquilhas_active_*` absent; only the plain `boquilhas_bq_id_key`
  UNIQUE on `(bq_id)`).
- Constraints: closed three-type CHECK, `quantity > 0`, machine CHECK `B1..C3`, Saída-required
  CHECK (machine + repairer), observations CHECK, `version >= 1`; 7 RESTRICT FKs (confdeltype
  `'r'`, no cascade). Direct violations map 23514→validator token / 23505→`register-exists`
  (the only 23505 mapping), 23503→anchor/repairer tokens — never a 500.
- Migrations 001–006 + `DmoDbContext.cs` byte-identical; `Down` removes ONLY P2-T07 state
  (real EF migrator: 24 raw → 21 raw, migration 007 removed from `__EFMigrationsHistory`,
  migration 006 retained); re-apply `×2` → 24 raw, idempotent. Final physical count: 23 product
  tables / 24 raw. All executed against a **fresh disposable PostgreSQL 16.15** (Docker
  container) via `DMO_TEST_POSTGRES_CONNECTION`.

## 10. Routes/access — **PASS**

- Final route matrix **15 = 3 pages + 12 endpoints**, recalculated (not preserved at the old 18):
  - Pages: `/boquilhas`, `/boquilhas/novo`, `/boquilhas/historico`
    (`[Authorize(Policy = BoquilhasPolicyNames.Boquilhas)]`).
  - Endpoints, all under one group → `RequireAuthorization(dmo.module.boquilhas)`:
    1. `GET /boquilhas/registers`
    2. `GET /boquilhas/registers/{boquilhasId}`
    3. `GET /boquilhas/registers/{boquilhasId}/movements/{movementId}/audit`
    4. `POST /boquilhas/registers`
    5. `POST /boquilhas/registers/{boquilhasId}/movements`
    6. `PUT /boquilhas/registers/{boquilhasId}/movements/{movementId}`
    7. `GET /boquilhas/productions`
    8. `GET /boquilhas/jobons/{jobonId}`
    9. `POST /boquilhas/jobons/{jobonId}/bq-association`
    10. `GET /boquilhas/machine-assignments`
    11. `GET /boquilhas/repairers`
    12. `GET /boquilhas/history`
  - Removed lifecycle routes are **gone**: no close/reopen/opening-facts route exists in
    `BoquilhasEndpoints.cs` or anywhere else (`A6R4_ExactlyTwelveEndpoints_NoLifecycleNoSettingsNoDocumentRoutes`
    green; source inspection).
- Every route/action uses exactly `dmo.module.boquilhas` — one canonical policy, no second
  policy, no sibling satisfaction, server-side enforcement: `A1` pin,
  `A2A3` (unrelated `job-on-view` grant → 403 on every one of the 15 routes),
  `A3` (no grant → 403 by direct URL), `A4` (ADMIN with all grants → 403),
  `A5` (denial is never an empty list/blank surface), `A6` (granted user exercises the surface).
  `A1A6_ExactlyFifteenRoutesAllCarryingTheCanonicalBoquilhasPolicy` green.

## 11. Frontend/history — **PASS**

- UI reflects the final model exactly: no Close, no Reopen, no active/closed state, no Início,
  no Irreparável; the movement selector and Histórico filter expose only
  `saida | entrada | entrada_sem_reparacao` with the Portuguese labels
  (rendered `V2`/`N5` rows; source inspection of `Index.cshtml`, `Novo.cshtml`,
  `Historico.cshtml`, `dmo-boquilhas.js`).
- Table arbitration everywhere: single click selects, double click opens (register list and
  Histórico via the opaque route map; movement ledger ↦ edit/detail), actions live outside the
  tables, **no per-row button grid** (`H3_TablesAreSelectionSurfaces_WithOpenRoutes_AndNoPerRowButtons`,
  `L1L2` fixed 1366×768 no-breakpoint composition, `L3` shell integration — all green).
- Local Histórico shows the production-linked movement facts: production context
  (reference/lot/production), movement type, quantity, `business_date`, `recorded_at`,
  repairer, machine, observations — backend-applied SQL filters, `FILTER_INVALID` on
  ill-formed values, 1-based paging with backend total (`T15`, `H2A…` rows green). No
  HISTÓRICO GLOBAL (`N3H2_NoHistoricoGlobalRouteOrEntryExists` green; `histor*ia*` token scan
  clean).

## 12. Negative scope — **PASS**

Confirmed absent (each with a live source/scan or test pin, all executed in this review):
- standalone Boquilhas flow — no `tool_id` anchor, Novo is production-only (section 2);
- lifecycle state / close / reopen — no status, no actions, no routes, no UI (sections 9–11);
- `boquilha_close_snapshots` / `boquilha_reopenings` / `boquilha_machines` — absent from
  migration, snapshot and live schema (section 9);
- active-anchor partial indexes + `ActiveAggregateExists` mapping — absent
  (`MG1_NoActivePartialUniqueIndexes_OnlyThePlainBqIdUniqueKey`, `B1Superseded` row,
  token scan: only documentation mentions of the superseded items);
- Início / Irreparável movements — refused at every layer (section 3);
- Tool irreparable state — zero Tool files changed by the correction; `tools` schema untouched;
  rows `E1`/`T11` prove the Tool row is never mutated by returns;
- second mutable balance authority — no balance column/table anywhere (`B1` row, schema);
- Boquilhas settings — `N1R4`, settings-token scan clean;
- PDF / email / file behavior — `N2_NoP2T08DocumentEmailOrFileExecutionExists`, token scan clean;
- availability registration — `CurrentBuildAvailable` stays `[]` (section 13).

## 13. CurrentBuildAvailable — **PASS**

`src/DMO.Application/Access/ModuleRegistrations.cs` — `CurrentBuildAvailable { get; } = []`
(source read; the correction commit does not register Boquilhas) and regression row
`N4_CurrentBuildAvailableStaysEmptyAndNoP2T07RegistrationExists` green.

---

## 14. Full-suite results (independently reproduced)

| Claim | Expected | Independent result |
|---|---|---|
| Build | 0 errors | **0 errors** — `dotnet build DMO.slnx -c Release` (12 warnings, all analyzer-style, pre-existing/protected or test files) |
| Unit | 658 / 658 | **658 / 658 PASS** (0 failed, 0 skipped) |
| Integration (disposable PostgreSQL) | 633 passed / 2 skipped | **633 PASS / 0 FAILED / 2 SKIPPED** (skips = `LiveDevTestSupabaseAdminAuthTests` + `LiveDevTestSupabaseUserAuthTests`, pre-existing live-Supabase gated) |
| Focused Boquilhas | 67 / 67 | **67 / 67 PASS** (`--filter FullyQualifiedName~Boquilhas`, integration project, `DMO_TEST_POSTGRES_CONNECTION` → fresh disposable PostgreSQL 16.15) + 49/49 Boquilhas unit rows within the 658 |
| Migration apply / Down / re-apply | apply → 24 raw; Down → 21 raw; re-apply clean | **PASS** (MG1/MG2/MG3 on the disposable DB: 7 ids in history; 24 raw = 23 product + history; Down → 21 raw; re-apply ×2 → 24 raw idempotent) |
| Auth negatives | green | **PASS** (A1–A6: sibling grants 403, no grant 403, ADMIN 403, denial never an empty list, granted user OK) |
| Negative-scope scans | green | **PASS** (N1–N7 + BND rows in-suite; independent `git grep` token scans over `src` clean except documentation mentions of superseded items) |
| Node adapter harness | 9/9 | **PASS** — direct `node` run of `tests/DMO.IntegrationTests/Boquilhas/dmo-boquilhas-adapter.behavior.mjs` over the real shipped `dmo-boquilhas.js`: S1–S9 all green (stale-version conflict + explicit recovery on append/edit; register-exists/400 keep the errors presentation without recovery control; picker never auto-selects; contextual create/cancel preserve origin) |
| Final tables | 3 | `boquilhas`, `boquilha_movements`, `boquilha_movement_audit` (live schema + migration + snapshot) |
| Final routes | 15 | 3 pages + 12 endpoints, all gated exactly `dmo.module.boquilhas` |
| CurrentBuildAvailable | `[]` | `[]` (source + N4) |

---

## 15. Conclusion

Every binding element of the FINAL Owner model (contract §33) is implemented as specified and
independently reproduced on a fresh disposable PostgreSQL 16.15: the production-linked identity
with the one-register-per-BQ-context plain unique key, the closed three-movement vocabulary, the
replay-derived outstanding with the Owner example at zero and negative values visible/non-blocking,
Entrada sem reparação as an explicit non-mutating return, the same-row version-guarded edit with a
single before/after audit trail and immutable `recorded_at`, the frozen repairer history, the
three-table schema with the removed lifecycle/active machinery, the 15-route matrix gated exactly
`dmo.module.boquilhas`, the corrected frontend, the full negative scope, and
`CurrentBuildAvailable = []`.

**No concrete architectural defect was found.**

- Identity / production association: **PASS**
- Three-movement vocabulary: **PASS**
- Production-ended movement: **PASS**
- Quantity replay: **PASS**
- Entrada sem reparação semantics: **PASS**
- Edit/audit: **PASS**
- Repairer history: **PASS**
- Schema simplification: **PASS**
- Routes/access: **PASS**
- Frontend/history: **PASS**
- Negative scope: **PASS**
- CurrentBuildAvailable: **PASS**

**P2-T07 FINAL STATUS RECOMMENDATION: CLOSED**

Under the simplified workflow, no additional Architect implementation review is required unless
this review finds a concrete architectural defect — none was found.

---

**Discipline:** this review changed no implementation (working tree contained only this report
before the commit). No P2-T08 / P2-T10 work was started. No availability registration was made.
STOP.