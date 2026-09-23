# P2-T06 — Controlo Approve — INDEPENDENT VERIFICATION

**Workstream:** P2-T06 — Controlo_Approve (Aprovar + Histórico de Pesos).
**Task class:** independent verification only. **No implementation change, no Architect review, no
closure, no P2-T07/P2-T08/P2-T10 work, no availability registration, no Supabase touch.**
**Verdict:** **NOT VERIFIED** — one concrete contracted test row is missing (see §11).

---

## 0. Verdict summary

| Item | Result |
|---|---|
| Identity | PASS |
| Persistence | PASS |
| Approve | PASS |
| Reject | PASS |
| Reopen | PASS |
| Shared Peso read model | PASS |
| Historical freeze | PASS |
| Routes / access | PASS |
| Concurrency | PASS |
| Histórico | PASS |
| Q-SEND boundary | PASS |
| Deferred carriers | PASS |
| Negative scope | PASS |
| Availability | PASS |
| **Targeted test matrix (61 AC / 67 rows)** | **NOT FULLY PROVEN — row CP4 absent** (66/67 rows have test coverage) |

P2-T06 remains **NOT CLOSED** (no gate is closed by this report).

---

## 1. Reviewed objects / commit verification

| Item | Value |
|---|---|
| Contract | `plans/contracts/P2-T06_CONTROLO_APPROVE_CONTRACT.md` @ `dd0e16390e46d49c811d1597de674dcc68023813` (read completely, 1904 lines, all 30 sections + Appendices A–E) |
| Architect PLAN review | `diogo-o/dmo-work` `dev/reviews/P2-T06_CONTROLO_APPROVE_PLAN_REVIEW.md` @ `947c5f7cb18b9e78dc6d4bf3a6477314e1514492` (=== dmo-work remote `main` HEAD; read completely, 457 lines; PLAN ACCEPT, blocking findings NONE) |
| Implementation response | `dev/responses/P2_T06_IMPLEMENTATION_RESPONSE.md` (read completely) |
| **Implementation SHA** | `e527ade46900fc114f44b9bfec404fb3b382dd01` (working tree clean; local HEAD == `origin/main`; `git diff --check` clean) |
| Contract ancestry | `dd0e163` is an ancestor of `e527ade` (authoring → D.4 record → implementation) |
| P2-T05 + correction slice | read-only inspection of the closed pieces actually consumed (below); CLOSED state unchanged |
| Supabase | **untouched** — all DB work on fresh disposable PostgreSQL 16.15 (Docker `postgres:16-alpine`, dedicated databases) |

Closed P2-T05 pieces inspected to verify the reopened-draft handoff claim:
- `ControloCreateService.SubmitAsync` writes **only** `SubmittedAt`/`SubmittedByUserId` and never
  `status` (verified, lines 356–360); edit/associate refuse when `SubmittedAt != null`
  (lines 233, 326, 406) — proving a reopened record (handoff cleared) is editable/submittable
  again through the **unchanged** P2-T05 routes.
- `PesoRepository.SubmittedAsync` sets only `SubmittedAt` (line 198).
- `PesoSheetReadModel` is the shared type in `DMO.Application.ControloCreate`; the review sheet
  embeds it through `IControloCreateService.GetAsync` (RD1: reflective type assertion + `Assert.Same`
  on the shared instance in unit tests).
- D2 conflict pattern (`renderConflict` + "Recarregar estado atual") re-implemented in the
  P2-T06-owned adapter and proven behaviorally (K5, node v24.19.0).

---

## 2. Identity / same `peso_id` — PASS

- `ControloApproveService`/`PesoReviewRepository` operate on the single route-carrier `pesoId`;
  `DecisionAsync` transitions the tracked `pesos` row loaded by that id; the decision row FK
  `FK_peso_review_decisions_pesos_peso_id` (`RESTRICT`) makes same-record binding structural.
- DB evidence (fresh disposable PostgreSQL): I1/I2/I3 — approve/reject/reopen assert `peso_id`
  unchanged, **0 new `pesos` rows**, same frozen facts re-read after (own inspection + tests).
- No `approval_peso_id`, no `production_id`, no `job_on_revision_id`, no review-copy type/table/
  column anywhere in P2-T06 sources (`grep` across `src/DMO.Application/ControloApprove`,
  `src/DMO.Web/Pages/Controlo/Approve`, `src/DMO.Infrastructure/Persistence/*Review*`; tests
  I4/I5/I6 + BND9). The only new identity is `peso_review_decision_id` (I6).
- `cm_id`/`tool_id` consumed exclusively through the shared read/`PesoReviewRow` traversal; nothing
  mints them (I5). Pending `tool_id` anchor never blocks review (I7).

## 3. Persistence — PASS

- **Migration `20260923171223_ControloApproveDomain` is the SIXTH overall** (verified in
  `__EFMigrationsHistory` after a fresh apply: 001 AccountAndTemplateFoundation, 002
  TemplateModuleComposition, 003 ToolJobOnDomainCore, 004 ControloCreateDomain, 005
  GlassDensitySettings, **006 ControloApproveDomain**).
- **Exactly ONE new table** `peso_review_decisions` (verified: 21 public tables = closed 20 + 1;
  migration source contains exactly one `CreateTable`, no `AddColumn`/`DropColumn`/`RenameTable`).
- Columns/CHECKs/FKs exact per §6.1/§7: 9 columns with §6 nullability; 4 CHECKs with the exact
  contracted names (`decision`/`prior_status`/`reason_required`/`version` vocabularies); 2 FKs
  `confdeltype='r'` (MG2 + own `pg_constraint` inspection).
- Indexes on the real database:
  - `IX_pesos_reviewable` = `(status, submitted_at DESC)` — exact per §7.3;
  - `IX_peso_review_decisions_decided_at` = `(decided_at DESC)` — exact per §7.3;
  - `IX_peso_review_decisions_peso_id`, `IX_peso_review_decisions_decided_by_user_id` — the
    disclosed FK-support scaffolding (P2-T05 precedent, disclosed in response §2).
- **Prior migrations 001–005 (incl. Designers) + `DmoDbContext.cs`: byte-identical** — verified by
  `git diff d458d0d e527ade` on those exact paths (empty diff) and by the MG1 pinned-hash test.
- **Down / re-apply independently verified by this verifier** on a scratch disposable database
  (`dmo_verify_cycle`): fresh apply → 6 migrations/21 tables → simulated Down of migration 006
  exactly as its source declares (history row removed, `DROP TABLE peso_review_decisions`,
  `DROP INDEX IX_pesos_reviewable`) → closed 19-product-table state restored → re-applied through
  the real `migrate` runner → exactly migration 006 re-applied, table + index restored → third run
  = no-op (0 pending). No drift.
- No unrelated schema drift: table register matches the contracted 21-table set exactly.

## 4. Decision history — PASS

- Append-only after COMMIT: no `MapPut`/`MapDelete` on any P2-T06 route; no `UpdatedAsync`/
  `DeletedAsync`/`RemoveRange` repository member (MG3 structural scan); no UPDATE/DELETE SQL.
- Decision facts exactly the contracted set: `peso_id`, `decision`, `decided_by_user_id`,
  `decided_at`, `pesos_version_at_decision`, `prior_status`, `reason` (NULL for `aprovado`,
  required non-blank for `nao_aprovado`/`reaberto` — validator + DB CHECK backstop).
- Prior decisions remain after reopen (O2 DB test + `Reopen_ReturnsTheSamePesoToPendente...` unit
  test: prior approval event intact, reopen event appended); a new approval creates a NEW decision
  fact (O4: approve→reopen→resubmit→approve = two distinct `aprovado` events, nothing overwritten,
  DB-verified). No historical erase anywhere.

## 5. Approve — PASS

- Only a reviewable submitted Peso can be approved: draft → 409 `not-reviewable` (D5 unit + HTTP),
  decided → 409 `already-decided` (K3/K4), stale → 409 `stale-version` with zero writes (K1 unit +
  DB + HTTP).
- SAME `peso_id` → `'aprovado'`; version increments exactly once; the decision row is appended in
  the SAME transaction (all-or-nothing, J3-style forced mid-transaction failure proof + own code
  inspection of `DecisionAsync`: assert-version → transition → insert → SaveAsync → Commit).
- Actor/time backend-authored: `ICurrentAccountContext` + backend clock; command carriers contain
  only identity/version (D1, D4; DB rows equal the authenticated backend user/clock).
- No automatic approval path: no route/worker/threshold derives a decision (D2 static scan); the
  adapter issues exactly one confirmed request per action (K5 node harness).

## 6. Reject — PASS

- SAME `peso_id` → `'nao_aprovado'` (I2/J1); exactly the closed three-value status vocabulary.
- Non-blank reason required: `REJECT_REASON_REQUIRED` (400) before any write; DB CHECK backstop
  maps 23514 on the reason CHECK onto the same validator token, never a 500 (J2).
- Decision row records outcome/actor/time/version-at-decision/prior status/reason (J1); version
  incremented once; append-only; atomic failure (J3: status unchanged, no event, no bump).

## 7. Reopen — CRITICAL — PASS

Proven through the REAL closed P2-T05 services/routes (not wholesale quoted):
- Same `peso_id`, `status='pendente'`, `submitted_at`/`submitted_by_user_id` cleared (O1 endpoint
  test over the real shared composition + repository test I3; DB-verified).
- The CLOSED Create edit/submit routes work again on the reopened record: O1 route test uses the
  real shared Peso service; R5 DB test submits through the real `ControloCreateService.SubmitAsync`
  and the re-submitted record is reviewable (`submitted_at` set, `status='pendente'`).
- Never automatically `aprovado`: O4 — resubmit → reviewable `pendente` → only a NEW explicit
  approval returns `aprovado` (two distinct events; no path restores the old approval).
- Prior review history preserved intact (O2; append-only trail).
- Reopen does not edit measurement facts: P2-T06 write paths touch only `status`/`version`/
  `updated_at` (+ `submitted_at`/`submitted_by_user_id` cleared on reopen) and the decision table
  (R2/O5 static audit + code inspection).

## 8. Shared Peso read model / historical freeze — PASS

- The review sheet embeds the EXACT `PesoSheetReadModel` (same C# record, `Assert.Same` on the
  shared instance in unit tests + reflection type-identity assertion); obtained through the SAME
  application read (`IControloCreateService.GetAsync`) under the `controlo-approve`-gated route.
  No second/forked read model, no copied shape, no second calculation renderer (RD1/RD3;
  `ReviewSheetReadModel` composes only Decisions + Availability around the shared type).
- Renderer parity (RD2): the same fixture rendered by Create's read-only submitted view and
  Approve's review view is mechanically compared 1:1 over the shared `data-dmo-*` hooks (strip
  facts, temperature, volumes, SAP references, per-row capacity/glass, frozen density) — exact
  values, labels and ordering; normalization matches.
- No second calculation engine (BND7 static scan; code inspection: no density/water resolution,
  no formula, no config re-read in any P2-T06 source).
- Historical freeze: R1 DB test — frozen facts byte-equal across approve and reopen; R3 DB test —
  after the NNPB `glass_density_settings` value is changed to 3.00, the reviewed/reviewable Peso
  keeps its frozen 2.4027 density and per-row results, and ONLY a new Peso resolves the new value;
  R5 — reopen → resubmit keeps the ORIGINAL frozen density and results through the real Create
  submit route. Approval reads facts; it never recreates calculation authority.

## 9. Routes / access — PASS

- Exactly 9 routes (own source scan + access tests probing all nine): 2 pages
  (`GET /controlo/approve`, `GET /controlo/approve/historico`) + 7 minimal-API endpoints
  (`/pending`, `/history`, `/pesos/{pesoId}`, `/pesos/{pesoId}/decisions`,
  `/pesos/{pesoId}/approve`, `/pesos/{pesoId}/reject`, `/pesos/{pesoId}/reopen`). No aliases, no
  extra mutation endpoint, no `MapPut`/`MapDelete` anywhere in the surface.
- Every route/action carries exactly `dmo.module.controlo-approve`:
  `MapGroup("/controlo/approve").RequireAuthorization(Policy)` for the endpoints
  (`Policy = ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloApprove)`) and
  `[Authorize(Policy = ControloApprovePolicyNames.ControloApprove)]` on both pages, with the
  pinned constant asserted equal to the canonical projection (A5).
- Create ⇎ Approve: a `controlo-create`-only caller is denied every one of the nine routes
  (server-side, direct URL, A1); an approve-only caller is denied every P2-T05 route incl. every
  Definições route (A2); both grants together keep the gates fully separate on the shared
  `controlo` destination (A3); ADMIN gains no operational access (A4); denial is never an empty
  list/blank surface (A6).

## 10. Histórico / Q-SEND / deferred carriers / concurrency / fixed desktop — PASS

- **Histórico is LOCAL** (Histórico de Pesos) inside Controlo Approve: no `historia` route, entry,
  registration, link or label (H4/B5 scans + tests); filters all backend-applied over
  backend-reported facts (§15.2 set: review state, traversal facts, submitted/decided ranges,
  decision actor/outcome) with unknown/ill-formed values refused `FILTER_INVALID`, never a silent
  full list (H2/PL2); rows carry the exact `peso_id`/status/last-decision/actor/time/decision
  count (H5); the visibility predicate (submitted OR has a decision trail) keeps reopened drafts'
  trails readable (H1/H2); single click selects, double click opens the exact record, actions live
  outside the table, no per-row action grid (PL4/H3 rendering proofs); trail rendered via
  `AuditTrail` from supplied facts only (never session-synthesized actor/time).
- **Q-SEND**: "Enviar para produção" renders only on approved Pesos, in the decision region, as an
  explicit-confirmed action that is **unavailable-with-reason** (reason names the P2-T08 documents
  contract); zero send mechanics — no PDF/file/directory/email/recipient/routing code, no
  `sent_at`/`enviado`/send table/document identity (BND2 scan + no send route + own grep);
  no record-state mutation performed or persisted.
- **Deferred carriers**: per-CM vocabulary pinned only as documented constants
  (`PerCmDecisionVocabulary`: exactly `Manter`/`Colocar de parte` — D3); no per-CM table/route/
  type (D6/BND6); no `controlo_sheet_id` column/table/route (BND6); no `previous_peso_id` column/
  table and no comparison relation anywhere in the migration or P2-T06 code (CP1/CP2/BND6/MG1);
  the review sheet carries no comparison region and fabricates none (CP3).
- **Concurrency**: approve/reject/reopen are version-guarded — in-transaction compare +
  `SaveAsync`/`ConcurrencyConflictExceptionMapping` → 409 `stale-version`, **zero rows written**
  (K1 unit/DB/HTTP); save-time race with a real second connection surfaces as the same 409 (K3 DB
  race test); no auto-retry/merge/overwrite; double decisions → 409 `already-decided` (K4);
  the D2 conflict presentation with the explicit "Recarregar estado atual" reload recovery is in
  the page-owned adapter and proven behaviorally (K5 node harness: exactly ONE request on
  stale-version, conflict marker + recovery, no retry, non-stale failures keep the errors
  presentation, reason-gated reject/reopen, approve one-confirmed-request, open arbitration
  through the route map). Observed version refreshes only on success.
- **Fixed desktop**: canonical 1366 × 768 validated rendering (L2); the page-owned CSS contains no
  `@media`/`@container`/`@supports` structural rule, no width listener, no table→card conversion,
  no action relocation (L1 scan); shared shell/header/navigation untouched (L3; Program.cs adds
  only service registration + `MapControloApproveEndpoints()`; PersistenceServiceCollectionExtensions
  adds one additive registration).

## 11. Negative scope / targeted matrix — ONE GAP

**Negative scope: PASS.** Static + behavioral scans confirm absence of: Definições/settings surface
(BND1), P2-T08 PDF/file/email mechanics (BND2), Boquilhas (BND3), availability/navigation
registration (BND4), HISTÓRICO GLOBAL (B5/H4), comparison/Folha/per-CM/send carriers (BND6), second
calculation engine (BND7), generic lifecycle/revision/queue infrastructure (BND8), identity
duplication (I4/I5/I6/BND9). `ModuleRegistrations.CurrentBuildAvailable` is `[]` in source and
asserted by BND1; `DestinationRouteRegistrations` unchanged (empty).

**Targeted P2-T06 matrix: NOT FULLY PROVEN — DEFECT.**

- **Contract §26.4, row CP4 (class U): MISSING TEST.** The contracted proof obligation —
  "the exact-relation composition contract is pinned: given a future read model carrying
  `previous_peso_id`, the composition exposes exactly that id and enough current/previous context —
  no fallback, no substitution, no mutation (**unit test over the composition function with a
  supplied carrier**)" — has NO implementation: no test method exists anywhere in
  `tests/DMO.UnitTests/ControloApprove/**` (nor any other test folder; exhaustive search for
  CP4/supplied-carrier/composition symbols), and no comparison-composition function exists in
  `src/DMO.Application/ControloApprove/**` for such a test to target. Row CP4 is the §26.4 proof
  carrier for AC-CP1 ("when the carrier exists" branch) and AC-CP2 (§30). The implementation
  response's claim **"61 AC / 67 rows proven across the suites"** (§9/§12) is therefore not
  accurate: **66 of 67 rows are proven**; the future-carrier composition contract is unpinned.
  (The seam itself — AC-CP4: no replacement pairing, no synthetic comparison — IS honored in
  behavior via CP3/BND6/CP1 and the contract text; the gap is the contracted unit test and the
  pinned composition function.)
- Non-blocking observations (not contract deviations):
  1. Row K2's reopen leg ("forced mid-transaction failure in ... reopen") is not exercised
     directly; the J3 DB test forces failures in the approve and reject legs of the SAME single
     write path (`DecisionAsync`), so the atomicity mechanism is proven uniformly. Row K2 was not
     counted in the defect above.
  2. Build warning disclosure: the response states "the only warnings are two pre-existing xUnit
     analyzer warnings"; a full fresh build emits 10 xUnit analyzer warnings (1 in the pre-existing
     `P2T02RegressionTests`; 9 in the NEW P2-T06-owned test files). Build itself: 0 errors.
  3. The migration source renders `IX_peso_review_decisions_decided_at` with
     `descending: new bool[0]`, while the applied DDL on PostgreSQL 16 is
     `(decided_at DESC)` — the database result matches §7.3 exactly; no observable deviation.
  4. Some touched/new test files and new sources contain comment-text encoding artifacts
     (mojibake of em-dashes/arrows in XML doc comments). Cosmetic only; compilation, behavior and
     assertions unaffected.

## 12. Independent test runs (fresh disposable PostgreSQL 16.15 only)

| Suite | Result |
|---|---|
| `dotnet build DMO.slnx` | **PASS** — 0 errors; 10 xUnit-analyzer warnings (see §11 obs. 2) |
| Unit suite | **605 / 605 PASS** (0 skipped) |
| Integration suite (fresh disposable PostgreSQL) | **568 PASS / 2 SKIPPED** — the 2 skips are the pre-existing live-Supabase auth tests (`LiveDevTestSupabaseUserAuthTests`, `LiveDevTestSupabaseAdminAuthTests`; `DMO_SUPABASE_LIVE_TEST` not set; re-confirmed by filtered re-run) |
| Targeted P2-T06 suite | 66/67 contracted rows have test coverage (see §11) — **row CP4 absent** |
| P2-T05 regression suite | PASS (included in the 568; ControloCreate/Definições/glass-density/access/negative rows unchanged in behavior) |
| Water-density focused tests | PASS (unchanged; verify-only posture of Approve re-confirmed by R1/R3/R5) |
| Glass-density correction tests | PASS (R3: settings change never rewrites a reviewed Peso; new Peso resolves new value) |
| D1/D2 JS behavior harness | PASS (ran inside the integration suite with node v24.19.0 present) |
| K5 approve-adapter JS harness | PASS (`.mjs` executed against the REAL shipped `dmo-controlo-approve.js`; all step assertions green) |
| Migration/schema tests | PASS (MG1/MG2/MG3 + DatabaseConnectivity/MIG numbering) — plus this verifier's own Down/re-apply cycle on a scratch disposable database (fresh apply → Down → re-apply → no-op) |
| Access/auth negative tests | PASS (A1–A6 incl. ADMIN fail-closed and direct-URL denial) |
| `CurrentBuildAvailable` | `[]` — confirmed in source and asserted by BND1 |

## 13. Output record

```text
verified implementation SHA:    e527ade46900fc114f44b9bfec404fb3b382dd01 (HEAD == origin/main)
remote main:                     e527ade46900fc114f44b9bfec404fb3b382dd01
verdict:                         NOT VERIFIED (one concrete defect — matrix row CP4)
build:                           PASS (0 errors; 10 analyzer warnings — see §11 obs. 2)
unit:                            605 / 605 PASS
integration:                     568 PASS / 2 pre-existing live-Supabase SKIPS
targeted P2-T06:                 66/67 rows proven; row CP4 (contract §26.4, unit-composition
                                 test for the future Comparação carrier) NOT implemented
P2-T05 regression:               PASS
water-density:                   PASS
glass-density:                   PASS
D1/D2/K5:                        PASS (node behavioral harnesses executed)
migration/schema:                PASS (sixth migration; one table; one additive pesos index;
                                 Down/re-apply independently re-verified; no drift)
identity:                        PASS (SAME peso_id throughout; FK RESTRICT backstop;
                                 no approval-copy/second aggregate/duplicate identity)
reopen:                          PASS (pendente + cleared handoff → real Create edit/submit
                                 routes work again; never auto-aprovado; history preserved)
routes/access:                   PASS (exactly 9 routes; dmo.module.controlo-approve everywhere;
                                 Create ⇎ Approve; ADMIN fail-closed; direct-URL gated)
negative scope:                  PASS (B1–B8/I4–I6/BND1–BND9 scans; no settings/PDF/email/
                                 Boquilhas/HISTÓRICO GLOBAL/send/availability artifacts)
CurrentBuildAvailable:           []
working tree:                    clean before report; only this report added by the verifier
```

## 14. Verification evidence trail

- Disposable PostgreSQL: Docker `postgres:16-alpine` (16.15), two dedicated databases
  (`dmo_verify` for the integration suite incl. migration tests; `dmo_verify_cycle` for the
  verifier's own Down/re-apply cycle). Never the shared Supabase TEST.
- Own DB inspections: `__EFMigrationsHistory` (6 rows, 006 last), 21 public tables,
  `pg_indexes` (`IX_pesos_reviewable (status, submitted_at DESC)`;
  `IX_peso_review_decisions_decided_at (decided_at DESC)`), `pg_constraint` (4 CHECKs, 2 RESTRICT
  FKs), Down/re-apply cycle via the real `migrate` runner (dummy Supabase env, disposable DB only).
- Git evidence: `git diff d458d0d e527ade` for protected paths = empty; `git diff --check` clean.
- Test-matrix audit: every one of the 67 §26.4 row ids mapped to ≥1 test method across
  `tests/DMO.UnitTests/ControloApprove/**`, `tests/DMO.IntegrationTests/ControloApprove/**`,
  `tests/DMO.IntegrationTests/Persistence/{PesoReviewRepository,Migration006ControloApproveDomain}*`
  — with the single CP4 exception (and the observation on K2's reopen leg).

## 15. Governance state after this verification

| Item | Status |
|---|---|
| P2-T06 | **IMPLEMENTED — AWAITING ARCHITECT IMPLEMENTATION REVIEW** (NOT CLOSED; NOT VERIFIED per this report) |
| P2-T05 + correction slice | CLOSED (unchanged) |
| P2-T07 / P2-T08 / P2-T10 | NOT AUTHORIZED (no work performed) |
| Supabase / live TEST | untouched (disposable PostgreSQL only) |
| `ModuleRegistrations.CurrentBuildAvailable` | `[]` (unchanged) |
| Implementation modified | **NO** (report only) |

NEXT GATE: Architect implementation review of P2-T06 — with this report's single concrete finding
(row CP4 of contract §26.4) dispositioned.