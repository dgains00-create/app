# P2-T06 — IMPLEMENTATION RESPONSE

**Workstream:** P2-T06 — Controlo Approve (Aprovar + Histórico de Pesos; the decision core over
the exact shared Peso).
**Task class:** implementation against the accepted contract. **No independent verification, no
Architect implementation review, no availability registration** (per the approved STOP rules).
**Status:** IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW.

---

## 1. Baseline / authority

| Item | Value |
|---|---|
| Accepted contract | `plans/contracts/P2-T06_CONTROLO_APPROVE_CONTRACT.md` @ `dd0e16390e46d49c811d1597de674dcc68023813` |
| Architect PLAN review | dmo-work `dev/reviews/P2-T06_CONTROLO_APPROVE_PLAN_REVIEW.md` @ `947c5f7cb18b9e78dc6d4bf3a6477314e1514492` — **PLAN ACCEPT**, blocking findings NONE, implementation AUTHORIZED (P2-T06 only) |
| DMO-MODULAR remote `main` before this task | `d458d0de55c90eca8806ef21c4b59b8c87080c4a` (working tree clean) |
| P2-T05 + correction slice | CLOSED (untouched beyond the disclosed per-phase test inventories) |
| P2-T07 / P2-T08 / P2-T10 | NOT AUTHORIZED (unchanged) |
| `ModuleRegistrations.CurrentBuildAvailable` | `[]` (unchanged — verified by BND1/test) |

## 2. Migration / schema delta (exact)

- **Migration:** `20260923171223_ControloApproveDomain` (the SIXTH overall; EF-generated with the
  accepted `DesignTimeDmoDbContextFactory`, `DmoDbContextModelSnapshot.cs` extended by EF).
- **New table (exactly ONE):** `peso_review_decisions` — columns exactly per §6.1
  (`peso_review_decision_id` PK uuid/gen_random_uuid, `peso_id` FK RESTRICT → `pesos`,
  `decision` CHECK `IN ('aprovado','nao_aprovado','reaberto')`, `decided_by_user_id` FK RESTRICT
  → `users`, `decided_at`, nullable `reason`, `prior_status` CHECK
  `IN ('pendente','aprovado','nao_aprovado')`, `pesos_version_at_decision` CHECK `>= 1`,
  `created_at` default now()); the four CHECKs + both FKs use the exact contracted names.
- **New index (exactly ONE on the existing `pesos`):** `IX_pesos_reviewable (status,
  submitted_at DESC)` — the deferred pending-list index P2-T05 §17.5 reserved for this slice.
- **New indexes on the decision table:** `IX_peso_review_decisions_peso_id`,
  `IX_peso_review_decisions_decided_at DESC` (§7.3), plus the EF-auto FK-supporting index on
  `decided_by_user_id` — the identical accepted precedent of P2-T05's auto FK indexes
  (`IX_pesos_created_by_user_id`/`IX_pesos_submitted_by_user_id` also shipped outside the
  contracted table) — recorded here as disclosed, additive scaffolding only.
- `pesos`/`peso_measurement_rows` keep EVERY column/CHECK/FK (byte-identity pinned); the
  `Down` drops the index then the table (exact inverse); re-apply is a no-op.
- Verified on a **disposable PostgreSQL** (Docker `postgres:16-alpine`, dedicated DB):
  MG1 (six migrations in history / 21 public tables), MG2 (columns/CHECKs/FKs
  `confdeltype='r'`/reason-23514), MG3 (append-only: no PUT/DELETE route, no update/delete
  repository member) all green; migrations 001–005 + `DmoDbContext.cs` byte-identical (hashes).

## 3. Routes / authorization (exact 9-route matrix)

- `GET /controlo/approve`, `GET /controlo/approve/historico` (pages) + 7 minimal-API endpoints
  (`/pending`, `/history`, `/pesos/{pesoId}`, `/pesos/{pesoId}/decisions`,
  `/pesos/{pesoId}/approve`, `/pesos/{pesoId}/reject`, `/pesos/{pesoId}/reopen`) — exactly the
  §13.2 rows, no aliases, no extra mutation endpoint (proven by the T06 regression scan and the
  A1/A6 sweeps).
- **Authorization:** every page/endpoint carries exactly
  `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloApprove)` =
  `dmo.module.controlo-approve` (pinned in `ControloApprovePolicyNames`, asserted equal to the
  canonical projection — A5). Create ⇎ Approve grants (A1–A4/A6): a create-only caller is denied
  every approve route (403) and vice versa; the shared `controlo` destination never merges the
  grants; ADMIN fails closed. No availability/destination/navigation registration exists
  (`CurrentBuildAvailable` stays `[]` — P2-T10c owns registration).

## 4. Status / lifecycle / decisions (identity rule)

- Approve → `'aprovado'`, reject → `'nao_aprovado'` on the SAME `pesos` row; the decision table
  records who/when/`pesos.version` (backend actor from `ICurrentAccountContext`, backend clock —
  never client-supplied; D4), prior status and reason. Version increments exactly once per
  committed decision; every decision is ONE transaction (transition + event all-or-nothing;
  J3/K2 atomicity proven with forced mid-transaction failures).
- Human-only commands (identity + version [+ reason]) — no warning/result input, no
  auto-approve/reject (D1/D2); double decisions → 409 `already-decided` (K3/K4); drafts →
  409 `not-reviewable` (D5).
- **Reopen (Q-REOPEN, exact):** same `peso_id` → `status='pendente'`,
  `submitted_at`/`submitted_by_user_id` cleared → the CLOSED P2-T05 edit/submit routes work
  again (proven O1/R5 over the real services: resubmit → reviewable); the prior approval is
  superseded by the draft and `aprovado` returns only via a NEW approval (O4: three distinct
  events approve→reaberto→approve); prior decision/actor/time/reason preserved intact in the
  append-only trail (O2; no historical erase); no fourth persisted status (the event token
  `reaberto` is a trail fact, never a status).
- **Decision-history model:** `peso_review_decisions` is the append-only trail; decision rows
  are immutable after COMMIT (no update/delete path — structural, MG3); the Peso remains the
  business identity (FK RESTRICT backstop).

## 5. Shared Peso read model (no fork — proof)

- The review sheet embeds the EXACT `PesoSheetReadModel` C# type from
  `DMO.Application.ControloCreate`, obtained through the SAME application read
  (`IControloCreateService.GetAsync`) under the `controlo-approve`-gated route — type-identity
  asserted (RD1: reflective assertion + `Assert.Same` on the shared instance in the unit tests).
- Approval-only presentation composes AROUND the shared model (`ReviewSheetReadModel`,
  availability, disabled reasons — RD3); no member is altered/hidden/redefined.
- **Renderer parity (RD2):** the same fixture rendered by Create's read-only submitted view and
  Approve's review view is mechanically compared over the shared `data-dmo-*` hooks —
  strip facts, temperature, volumes, SAP references, per-row capacity/glass and the frozen
  density are equal with identical labels/order/≤ 2-dp normalization.
- No second calculation engine exists (BND7 code scan: no formula/density/water resolution in
  any P2-T06 code source; the review renders the frozen facts only); settings changes never
  rewrite a reviewed Peso (R3: the NNPB setting changed to 3.00 → the reviewed Peso keeps
  2.4027 and only a NEW Peso resolves the new value); reopen/re-submit keep the original frozen
  facts (R5).

## 6. Q-SEND implementation (exact)

- The `Enviar para produção` affordance is rendered ONLY on approved Pesos, in the decision
  region, as an explicit-confirmed (non-automatic) action that is **unavailable-with-reason**
  until the P2-T08 contract exists (reason text names the documents contract); it performs ZERO
  send mechanics — no PDF, no file/directory write, no email/recipient/routing, no document
  identity and **zero send persistence** (no `sent_at`, no `enviado` status, no send table;
  BND2 scan + no-send-route check). P2-T08 later consumes exactly what P2-T06 produces:
  `status='aprovado'` + the decision trail + the frozen facts.

## 7. Deferred carriers (confirmed — nothing invented)

- **Per-CM (Q-PERCM):** vocabulary pinned as documented constants
  (`PerCmDecisionVocabulary`: exactly `Manter` / `Colocar de parte`, D3) — NO table, NO route,
  NO type beyond the constants; persistence is enabled only by the Comparação remainder
  contract (BND6/D6 scans prove absence).
- **Folha (Q-FOLHA):** no `controlo_sheet_id` column/table/route anywhere (BND6).
- **Comparação (Q-COMP):** no `previous_peso_id`, no heuristic/latest/date/machine pairing, no
  comparison persistence (CP1/CP2/CP3 scans; the review sheet carries no comparison region).

## 8. Historico / concurrency / fixed desktop

- **Histórico de Pesos** is HISTÓRICO (local): backend-applied filters (review state,
  production/tool facts via traversal, submitted/decided ranges, decision actor/outcome), rows
  carry the exact `peso_id`/status/last-decision/decision-count, reopened records keep their
  trail visible (H1/PL1/H2/H5); single click selects, double click opens the exact record,
  actions live OUTSIDE the table (DenseDataTable arbitration; PL4/H3 rendering proofs); no
  `historia`/HISTÓRICO GLOBAL route or entry (H4/B5).
- **Concurrency:** version-guarded decisions → 409 `stale-version`, zero rows written
  (K1), save-time race mapped via the accepted `SaveAsync`/`ConcurrencyConflictExceptionMapping`
  (K3 over a real second connection), no auto-retry/merge/overwrite; the D2 conflict
  presentation with the explicit "Recarregar estado atual" recovery is implemented in the
  page-owned adapter and proven BEHAVIORALLY with a real JS engine (K5 node harness: one
  request on stale-version, conflict marker + reload recovery, non-stale typed failures keep the
  errors presentation, reason-gated reject/reopen, approve one-confirmed-request, open
  arbitration through the route map).
- **Fixed desktop:** both pages designed/validated at 1366 × 768, region-stable R1–R5/H1–H3,
  breakpoint-free stylesheet (L1 scan: no `@media`/`@container`/`@supports`/width listeners),
  local keyboard-reachable overflow for the results table (L2 rendered region-order proofs),
  shared shell/header/navigation untouched (L3).

## 9. Test totals and verification

- Full solution build: **succeeds** (`dotnet build DMO.slnx`, no errors; the only warnings are
  two pre-existing xUnit analyzer warnings in existing suites).
- **Unit suite: 605 / 605 green** (includes the new ControloApprove validator/service/type
  tests; pre-existing suites unchanged in behavior).
- **Integration suite: 568 / 570 green, 2 skipped** — the 2 skips are the pre-existing
  LIVE-Dev/TEST Supabase auth tests (skipped by default; `DMO_SUPABASE_LIVE_TEST` not set).
  All DB-class rows ran against the **disposable PostgreSQL** (`DMO_TEST_POSTGRES_CONNECTION`):
  migration 006 / schema / repository rows (I1–I3, I7, R1, R3, R5, J1–J3, O1–O4, D4, PL1, H1,
  K1–K4, MG1–MG3) green; the full pre-existing regression matrix (P2-T04/P2-T05/water-density/
  glass-density/D1-D2 incl. the node behavioral harness, access/negative tests, frozen-file
  hashes) green.
- `git diff --check`: clean. Working tree: contains only this task's files (see §11).

## 10. Negative-scope proof / availability

- No Definições/settings surface under Approve (B1 scan + type sweep), no P2-T08 execution
  (B2), no Boquilhas (B3), no availability/navigation registration (B4/BND1: `CurrentBuildAvailable`
  still `[]`, registry empty), no `historia` (B5/H4), no comparison/Folha/per-CM/send carrier
  (B6), no second calculation engine (B7), no generic lifecycle/revision/queue infrastructure
  (B8), no identity duplication/approval copy (I4/I5/I6/BND-B9), protected files byte-identical
  (MG1 hashes incl. migrations 001–005 + `DmoDbContext.cs`).
- `ModuleRegistrations.CurrentBuildAvailable` remains `[]`; `DestinationRouteRegistrations`
  empty; P2-T10 owns availability registration (step P2-T10c) — NOT performed here.

## 11. Changed-file inventory (exact)

**New product files** (contract App. B): `src/DMO.Domain/Controlo/PesoReviewDecisionId.cs`,
`PesoReviewDecisionKind.cs`, `PesoReviewDecision.cs`; `src/DMO.Application/Repositories/IPesoReviewRepository.cs`,
`src/DMO.Application/Persistence/PesoReviewPersistenceException.cs`,
`src/DMO.Application/ControloApprove/` (ControloApproveModels.cs, ControloApproveValidator.cs,
IControloApproveService.cs, ControloApproveService.cs, ReviewSheetReadModel.cs,
PerCmDecisionVocabulary.cs); `src/DMO.Infrastructure/Persistence/PesoReviewRepository.cs`,
`Entities/PesoReviewDecisionEntity.cs`,
`EntityConfigurations/PesoReviewDecisionEntityConfiguration.cs` (incl. the additive
`PesoReviewableIndexConfiguration`), `Migrations/20260923171223_ControloApproveDomain.cs(.Designer.cs)`;
`src/DMO.Web/Endpoints/ControloApproveEndpoints.cs`,
`src/DMO.Web/Pages/Controlo/Approve/` (ControloApprovePolicyNames.cs, Index.cshtml(.cs),
Historico.cshtml(.cs)), `wwwroot/css/dmo-controlo-approve.css`, `wwwroot/js/dmo-controlo-approve.js`.

**Additive edits to the only accepted non-new files** (App. A): `src/DMO.Web/Program.cs`
(service registration + `MapControloApproveEndpoints()`), `PersistenceServiceCollectionExtensions.cs`
(review repository registration), `DmoDbContextModelSnapshot.cs` (EF-generated extension).

**New test files:** `tests/DMO.UnitTests/ControloApprove/**` (validator/service tests + fakes),
`tests/DMO.IntegrationTests/ControloApprove/**` (P2T06TestHost, P2T06TestComposition,
P2T06ReviewStore, access/endpoints/rendering/regression tests, P2T06ProductionScan,
dmo-controlo-approve-adapter.behavior.mjs + harness test), `tests/DMO.IntegrationTests/Persistence/`
(Migration006ControloApproveDomainTests, PesoReviewRepositoryIntegrationTests).

**Disclosed per-phase test-inventory extensions (the accepted additive pattern, each documented
in-file):** `ToolRestrictionTests` (one review entity joins the DbSet inventory), the migration
inventories (Migration003/004/005 + DatabaseConnectivity + MigrationRunner: sixth migration and
the one decision table join the pinned registers; MIG11 latest-migration set; MIG_X2's single
authorized decision-table exception), `P2T04ProductionScan` (P2-T06 test folders + disclosed
additive test paths + persistence-test basenames), `P2T05ProductionScan` (P2-T06 owned surface
added to the allowlist), `ControloCreateAccessTests.AUT4` (the approve surface now exists as a
separate sibling — assertion updated from 404 to 403-denial, preserving its AC-G4 intent),
`P2T05TestStore` (additive P2-T06 arrangement surface). The P2-T05 `BND2`-style closure posture
("no approve surface") is superseded by the P2-T06 contract, exactly as the P2-T05 closure
recorded ("P2-T06 adds Approve actions on the same destination").

**Observations recorded (non-blocking, no contract deviation):**
1. The pinned reason CHECK of §7.4 treats a NULL reason as satisfied (PostgreSQL CHECK
   semantics); the service validator closes NULL/blank first and the CHECK backstop closes every
   non-NULL blank — the J2/J3 tests prove the backstop over the blank case.
2. The list `Total` metadata is the backend-counted row count of the returned page (the exact
   `IPesoReviewRepository` carrier of §8.2 carries no cross-page total; the DenseDataTable
   paging is consumer-owned and renders page labels only).
3. The pending/history list queries resolve the traversal facts and aggregate counts in batched
   reads (single-entity SQL sources + EXISTS predicates) — the composed left-join form with
   nested correlated aggregates is not translatable in this EF version; observable results are
   identical (backend-filtered, deterministic ordering, one page).

## 12. Output record

```text
P2-T06 IMPLEMENTATION STATUS:    IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION
                                 / ARCHITECT IMPLEMENTATION REVIEW
ACCEPTED CONTRACT SHA:           dd0e16390e46d49c811d1597de674dcc68023813
PLAN ACCEPT SHA:                 947c5f7cb18b9e78dc6d4bf3a6477314e1514492 (dmo-work)
MIGRATION:                       20260923171223_ControloApproveDomain (SIXTH, additive)
NEW TABLE:                       peso_review_decisions (append-only decision trail)
NEW INDEX:                       IX_pesos_reviewable on pesos (status, submitted_at DESC)
ROUTES:                          exactly 9 (2 pages + 7 endpoints), no aliases
AUTHORIZATION:                   dmo.module.controlo-approve on every route/action;
                                 Create ⇎ Approve; shared controlo destination never merges
REOPEN:                          same peso_id → pendente + cleared submitted handoff;
                                 prior history preserved; aprovado only via a NEW approval
DECISION HISTORY:                append-only; who/when/pesos.version; immutable after COMMIT
SHARED READ MODEL:               exact PesoSheetReadModel type + shared read + parity test
Q-SEND:                          initiation-only affordance on approved Pesos,
                                 unavailable-with-reason until P2-T08; zero send persistence
DEFERRED CARRIERS:               per-CM/Folha/Comparação vocabulary pinned, nothing invented
BUILD:                           PASS (dotnet build DMO.slnx)
UNIT:                            605 / 605 PASS
INTEGRATION:                     568 PASS / 2 pre-existing live-Supabase SKIPS
TARGETED P2-T06 MATRIX:          PASS (61 AC / 67 rows proven across the suites)
P2-T05 REGRESSION:               PASS (incl. disclosed inventory extensions)
WATER-DENSITY / GLASS-DENSITY:   PASS (unchanged + R3 freeze proof)
D1/D2 REGRESSION:                PASS (incl. the K5 approve-adapter node harness)
MIGRATION/SCHEMA:                PASS (disposable PostgreSQL; Down/re-apply verified)
NEGATIVE SCOPE:                  PASS (B1–B8/I4–I6 scans; no settings/PDF/email/send/Boquilhas)
CurrentBuildAvailable:           [] (UNCHANGED)
P2-T07 / P2-T08 / P2-T10:        NOT AUTHORIZED (no work performed)
```

## 13. Governance state after this task

| Item | Status |
|---|---|
| P2-T06 | **IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW** (not closed; not self-accepted) |
| P2-T05 (Controlo_Create) + correction slice | CLOSED (unchanged) |
| P2-T06 implementation commit | this task's commit (recorded below) |
| Supabase / live TEST | untouched (disposable PostgreSQL only) |
| `ModuleRegistrations.CurrentBuildAvailable` | `[]` (unchanged) |
| `DestinationRouteRegistrations` / route registry | unchanged (still empty) |