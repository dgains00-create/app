# P2-T06 — Controlo Approve (Aprovar + Histórico de Pesos) — BACKEND / INTERFACE CONTRACT

**Workstream:** P2-T06 — Controlo_Approve.
**Task class:** contract authoring only. **No implementation, no migration, no Supabase change.**
**Handoff:** `plans/beta-workstreams/P2-T06-CONTROLO-APPROVE.md` (authoritative handoff for this
workstream). **Settled delta:** `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` (§2).
**Status:** **AUTHORED — AWAITING ARCHITECT PLAN REVIEW.** Not accepted; implementation is
authorized only by the Architect `PLAN ACCEPT` per `dmo-beta-master/WORKFLOW.md`.

> **Scope note (operative):** this contract authors the **decision core of Controlo Approve** over
> the existing, authoritative Peso read model published by the **accepted, CLOSED** P2-T05:
> pending/review list, exact-record review (shared read model, no fork), approve/reject/reopen on
> the **same `peso_id`**, the decision/audit trail, the local **Histórico de Pesos**, the shared
> fixed-desktop surfaces, and the exact boundary records (per-CM `Manter`/`Colocar de parte`,
> Folha decision, Comparação read, `Enviar para produção`) whose carriers are **not yet
> persisted** in the current schema and are therefore contracted as seams with pinned defaults —
> **nothing is invented** (see §17, §18, §23, §24, Q-SCOPE-REMAINDER, Q-SEND, Q-FOLHA, Q-PERCM,
> Q-COMP).

---

## Required-section index

| # | Section |
|---|---|
| 1 | Authority |
| 2 | Scope / non-scope |
| 3 | Domain / state vocabulary |
| 4 | Existing identities consumed |
| 5 | New identities genuinely required |
| 6 | Physical persistence schema |
| 7 | Keys / constraints / indexes |
| 8 | Repository contracts |
| 9 | Application service contracts |
| 10 | Transaction boundaries |
| 11 | Concurrency / version behavior |
| 12 | Result / error vocabulary |
| 13 | Route matrix |
| 14 | Authorization policy per route |
| 15 | Review list / read / detail contracts |
| 16 | Shared Peso read-model reuse |
| 17 | Per-CM decision contract |
| 18 | Folha decision contract |
| 19 | Approve / reject / reopen semantics |
| 20 | Local Histórico behavior |
| 21 | Fixed-desktop UI regions |
| 22 | Negative-scope protections |
| 23 | Enviar para produção (authority trace + gap) |
| 24 | Downstream seams (P2-T07 / P2-T08 / P2-T10) |
| 25 | Migration contract |
| 26 | Test-to-acceptance matrix |
| 27 | Authority questions |
| 28 | Authority questions — summary |
| 29 | Explicit downstream seams |
| 30 | Implementation acceptance criteria |
| App. A | Protected boundaries |
| App. B | Implementation file ownership / expected paths |
| App. C | Fixed desktop obligations |
| App. D | Governance record |
| App. E | PLAN REVIEW gate |

---

## 1. Authority

### 1.1 Authority order applied

The repository authority order of `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` continues to apply,
extended by the settled delta:

1. `reports/BETA_MASTER_RECONCILIATION.md` — current-state and delta authority;
2. `diogo-o/dmo-beta-master` — Beta functional/scope/workflow/acceptance authority;
3. `diogo-o/dmo-master` — global architecture, identities, access model, module vocabulary;
4. `diogo-o/DMO-MODULAR` — implementation state and integration seams;
5. `diogo-o/workbench` / `diogo-o/dmo-work` — historical/planning evidence only; superseded where
   the current authority differs.

**Area-specific supersession (settled):** `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`
is repository-recorded settled functional authority for the settings/repairer/email/PDF area.
Its §2 fixes the Approve surface to exactly **Aprovar** and **Histórico de Pesos**; Controlo
Approve owns **no** Definições and **no** operational setting. The `Admin > Definições > Controlo`
recipient-location statement of `dmo-master/modules/CONTROLO.md` §17 is superseded in that area
(`…DELTA.md` §1/§2/§8/§9 + P2-T05 contract §1.1) — recipients are named lists owned by
`Controlo_Create → Definições`; no exact routing rule is fixed (`…DELTA.md` §9.4; P2-T05 Q-ROUTE).
The **existence** of the explicit confirmed `Enviar para produção` action is **not** superseded —
only its recipient configuration location and any routing rule (§23).

### 1.2 Authority read for this contract (completely)

**DMO-MODULAR:**
- `plans/beta-workstreams/P2-T06-CONTROLO-APPROVE.md` — the binding handoff (scope §4, settings
  excluded §4.1, non-scope §5, access §7, persistence §8, tests §9, acceptance §10);
- `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` — §0 terminology (HISTÓRICO local vs HISTÓRICO
  GLOBAL), §4 B2, §5 dependency graph, §7 P2-T06 (settled reduced scope), §7 P2-T05 (the read
  model + status vocabulary it publishes), §8 shared-data map (Peso row, Comparação row, Folha
  row), §9 route/availability plan, §10 access plan, §11 P2-T06 test strategy, §12 protected
  register, §13 integration sequence (step 10 = P2-T06; step 11 = P2-T10c Approve availability),
  Appendix A;
- `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` — §2 (Approve reduced to Aprovar +
  Histórico de Pesos; settings table), §9 (email routing intent; no invented rules), §12
  (superseded visuals V1/V3/V4/V5), §13–§16;
- `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` (accepted, CLOSED) — every seam this
  contract consumes: `peso_id` identity/anchoring (§3), status vocabulary and the reviewable
  predicate (§3.1: reviewable **iff** `submitted_at IS NOT NULL` and `status = 'pendente'`),
  measurement model and frozen facts (§5/§6), lifecycle (§5.7: "P2-T06 —► 'aprovado' |
  'nao_aprovado' on the same row"), create/edit/submit closed semantics (§7.3/§7.4/§7.5:
  `submitted_at` cleared only by P2-T06 reopen; `already-submitted`; no delete), physical schema
  (§16/§17), concurrency (§19: `SaveAsync` pattern), route/policy pattern (§21), failure
  vocabulary (§26.1/§26.2), published read-model shapes (§26.3 `PesoSheetReadModel`), the
  deferred pending-list index (§17.5: "no index on `pesos.status`/`submitted_at` … each owning
  workstream contracts its own query index"), downstream seams (§29), acceptance criteria (§30);
- `dev/responses/P2_T05_IMPLEMENTATION_RESPONSE.md` (final) — the shipped seams: the exact
  `status`-preserving submit (`with { SubmittedAt, SubmittedByUserId }` only), `IControloCreateService`
  (`GetAsync` → `Found(PesoSheetReadModel)`), the aggregate-embedded row set, the 17 shipped
  routes and their `controlo-create` gates, `ConcurrencyConflictExceptionMapping`,
  `PesoSheetReadModel.cs` location, the D1/D2 correction patterns (submitted-view adapter
  safety; `renderConflict` + explicit reload recovery), the 5-settings-store state,
  `ControloCreateDomain` migration 004 + `GlassDensitySettings` migration 005;
- `reports/P2_T05_INDEPENDENT_VERIFICATION.md`, `reports/P2_T05_REVERIFICATION_D1_D2.md`,
  `reports/P2_T05_GLASS_DENSITY_CORRECTION_VERIFICATION.md` — verification state (P2-T05 CLOSED;
  D3/D4 non-blocking carry-forward);
- `plans/contracts/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_CONTRACT.md` — the closed
  glass-density rule (frozen-on-Peso; settings changes affect new Pesos only) that P2-T06 must
  never re-read (§5.2 of this contract);
- `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` — §1A fixed desktop, §4 state vocabulary
  (`conflict` = clear message + supplied recovery choices), §9 `DenseDataTable` (single click
  selects, double click opens **only when the consumer supplies it**), §12 `AuditTrail` (never
  synthesizes actor/time), §14 `DecisionBar` (disabled reasons; no decision rule encoded), §16
  change protocol;
- `plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md` and
  `plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md` (accepted) — the exact
  component contracts consumed here (table = selection surface, actions outside the table;
  AuditTrail renders only supplied facts; DecisionBar is rule-free);
- `src/` (read-only inspection) — `ModuleCatalog` (`ControloApprove` = `controlo-approve`,
  destination `controlo`), `ModuleRegistrations` (`[]`), `PesoSheetReadModel.cs`,
  `ControloCreateService.cs` (submit leaves `status` untouched), `Pages/Controlo/Create.cshtml`
  (the Peso sheet renderer is page-owned; **no shared sheet partial exists** — §16),
  `ControloPolicyNames.cs` (the pinned-policy pattern to mirror), `DmoDbContext`/
  configurations (RESTRICT FKs, version conventions), migration list (001–005).

**dmo-beta-master (BETA authority):**
- `modules/CONTROLO_APPROVE.md` (full) — purpose, included scope, outside scope, identity and
  persistence (same `peso_id`; same `controlo_sheet_id` for Folha), human decision principle,
  review workflow, reopen, shared Peso presentation (consume, no duplicate renderer, no
  divergent field ordering, no hidden result set), Comparison review (exact persisted
  `current_peso_id -> previous_peso_id`, no heuristic), access, frontend responsibility, backend
  contracts required, acceptance criteria, required evidence;
- `modules/CONTROLO_CREATE.md` — the C-side contract (what Create publishes; per-CM decisions are
  explicit human facts; Comparação per-CM pairing is part of the comparison workflow);
- `architecture/RECORD_LIFECYCLES.md` §4–§8 — Peso status vocabulary (`Pendente`/`Aprovado`/
  `Não aprovado`), submission, approve/reject/reopen on the same `peso_id`, reopen preserves
  history, Comparação is a relation not a status, Folha (§7: `controlo_sheet_id` distinct;
  component decisions/observations are persisted facts; do not collapse Folha lifecycle into
  Peso status), §12 cross-record invariants;
- `architecture/ACCESS_AND_NAVIGATION.md` — independent Create/Approve enforcement; shared
  destination never merges grants; direct-route enforcement; no second access model;
- `architecture/CROSS_MODULE_FLOWS.md` — Create → Approve flow (same persisted Peso;
  approve/reject/reopen); anti-inference rules; backend/frontend crossing;
- `architecture/BACKEND_FRONTEND_MODEL.md` — backend owns IDs/persistence/relations/
  authorization/lifecycle/audit; frontend owns rendering/inputs/choices; no frontend-synthesized
  actor/time; no second source of truth;
- `implementation/BETA_INTEGRATION_SEAMS.md` — "C → D seam" (D consumes submitted `peso_id`,
  canonical read-only Peso sheet/read model, Comparison context, warnings/results, submission
  attribution/status, Folha review projection where applicable; D must not fork the renderer),
  "Workstream D" (backend operations/read contracts needed: pending list, exact submitted Peso
  retrieval, approve, reject, reopen, per-CM decisions where required, Folha decision where
  required, audit/decision history, confirmed send-to-production action **where the owning
  contract allows it**); concurrency/stale-data presentation; testing seam;
- `contracts/IDENTITIES_AND_RELATIONSHIPS.md` — identity map (`peso_id`, `controlo_sheet_id`),
  Comparison relation, no reverse-ID arrays, no fake identities, human selection rule;
- `contracts/SHARED_FRONTEND.md` — §9 `DenseDataTable`, §12 `AuditTrail`, §14 `DecisionBar`
  (§"Common state vocabulary": `stale`/`conflict` distinct);
- `ACCEPTANCE_MATRIX.md` — §2 global invariants, §6 Workstream D (acceptance/evidence), §9–§11
  gates;
- `WORKFLOW.md` — controlled chain, conflict protocol, anti-invention rules.

**dmo-work (accepted rulings/reviews closing P2-T05 identity/Peso authority — binding):**
- `dev/rulings/P2_T05_PESO_IDENTITY_RULE.md` — **binding obligations for the P2-T06 contract**
  (§4): consume the same persisted `peso_id` for pending list, decision, reopen and history;
  keep the same `peso_id` across approve/reject/reopen cycles; persist decision/audit metadata
  preserving **who approved, when, and the `pesos.version` approved**; reopen must preserve
  decision/attribution history, restore the draft-editable handoff (so the accepted P2-T05
  submit route works again) and enforce that **any material edit to an approved Peso leaves it
  `nao_aprovado` — never `aprovado` — before re-submission**; **define "material edit"
  precisely**; MUST NOT create a new `peso_id`, an approval-copy record, a second snapshot of
  operational facts, or recalculate/re-resolve density independently of the persisted facts;
- `dev/rulings/P2_T05_PESO_APPROVAL_PDF_AUTHORITATIVE_DATA_RULE.md` — P2-T06 MUST read the
  persisted Peso record and its historical snapshots; MUST NOT recreate the control data,
  maintain a separate approval copy, or recalculate a second independent version; approval data
  is a consumer of the same authoritative record (no separate dataset);
- `dev/rulings/P2_T05_GLASS_DENSITY_CONFIGURATION_OWNER_RULE.md` + the correction contract and
  its closure — glass density is frozen on each Peso; later settings changes affect new Pesos
  only; P2-T06 reads the frozen value through the read model and never re-reads the setting;
- `dev/reviews/P2-T05_CONTROLO_CREATE_CLOSURE.md` and
  `dev/reviews/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_CLOSURE.md` — P2-T05 and its
  correction slice are **CLOSED**; next eligible gate = **P2-T06 planning/contract gate**;
  P2-T06/T07/T08/T10 remain **NOT AUTHORIZED**; settled decisions MUST NOT be reopened.

**dmo-master (global authority; consumed where needed — identities/access/lifecycle):**
- `modules/CONTROLO.md` (full, current) — §2 access (Controlo Approve = Peso/Comparison review,
  approve/reject/reopen, review history), §4 (Peso owns approval/reopen attribution and
  history), §5/§6 (pending association never blocks approval), §10 (Comparação: per-CM human
  decisions `Manter`/`Colocar de parte` and justification where required; its own
  confirmation/approval semantics), §13 (Folha: five family items CM/MF/BQ/PU/CS with OK/NOK
  and observation; distinct final human decision and attribution; do not collapse into Peso
  approval), §15 (actors: Responsável Peso approval/rejection; Folha final decision belongs to
  Controlo Approve; **Folha states `Rascunho → Submetida → Aprovada / Rejeitada`**; submission
  belongs to Controlo Create; decision and reopen belong to Controlo Approve; reopen moves a
  submitted or decided Folha back to `Rascunho`; after rejection `reabrir → editar → resubmit`;
  appended events never mutate prior history; **the initial Peso approval is general for the
  controlled set; Comparação decisions are per-CM and not implied by it**), §16 (history is
  query traversal over preserved facts; no universal History table), §17 (`Enviar para
  produção` is an explicit confirmed action available from an approved Peso record; never
  automatic; missing/invalid recipient configuration blocks only the send; approval and the
  recorded document remain valid), §18 (invariants), §19 (acceptance);
- `global/ACCESS_MODEL.md` §9 — "Controlo Approve exposes the review/approval/decision surface:
  Peso review/approval/rejection, per-CM Comparação decisions, Folha decision, reopen where the
  workflow allows, and the explicit confirmed send-to-production action"; §1 (module identity
  4, destination `controlo`); the hidden-control-is-never-the-security-boundary rule;
- `global/INFORMATION_MODEL.md` — direct-relationship rules (`peso_id -> cm_id`; no
  `job_on_revision_id`), fact ownership, no repeated reachable IDs.

### 1.3 What is NOT reopened by this contract (closed authority, settled)

- The P2-T05 contract/closure and the glass-density correction: schema, status vocabulary,
  reviewable predicate, frozen facts, submit semantics, `already-submitted`, concurrency
  pattern, routes/policies, "no ninth table beyond `glass_density_settings`" posture (P2-T06
  adds **one** decision table — see §5.1/§25.1 and the supersession record §25.3).
- Water density: operator enters only water temperature; automatic 31-value lookup 5–35 °C,
  `AwayFromZero`, no interpolation (Owner clarification, closed). P2-T06 reads persisted
  results only — no water engine (§22, BND-B7).
- Glass density: current configuration lives in `Controlo → Definições` per processo
  (NNPB/PS); the reviewed Peso's **frozen** `glass_density_g_cm3` is the review truth; P2-T06
  has no glass-density settings surface and never re-reads the setting (§5.2, §22).
- Identity: `tool_id` = canonical Tool identity; `cm_id` = frozen Job On context identity;
  no `production_id`; no `job_on_revision_id`; pending `tool_id` anchor never blocks approval.
- Settings ownership: all Definições belong to `Controlo_Create`; Approve owns none.
- HISTÓRICO terminology: "Histórico de Pesos" here is **HISTÓRICO (local)**; HISTÓRICO GLOBAL
  (`historia`) is DEFERRED BY DESIGN and is never implied (§20.4).

### 1.4 Recorded authority silences for P2-T06 (no behavior invented; each pinned below)

| # | Silence | Consequence fixed here |
|---|---|---|
| S1 | **Decision-history schema** — no authority fixes the physical shape of approval/deletion/reopen history; the Peso Identity Rule fixes only the required facts (who/when/`pesos.version`) | §6: **one** append-only decision table owned by this slice |
| S2 | **Reject/reopen reason** — authority says "reject with note where required" and "reason where required" but fixes no mandatory rule for Peso | §19.3: pinned default — non-blank reason **required** for reject and for reopen; none for approve (Q-NOTE) |
| S3 | **Reopen target status** — P2-T05's lifecycle leaves reopen to P2-T06 and the Identity Rule requires "draft-editable handoff" and "never `aprovado` before re-submission" without naming the interim status | §19.2: pinned default — reopen sets `status = 'pendente'`, clears `submitted_at`/`submitted_by_user_id`; the only mechanism consistent with the **closed** Create submit route and the reviewable predicate (Q-REOPEN) |
| S4 | **`Enviar para produção` executable semantics** — master fixes existence/confirmation/location-from-approved-Peso; Beta conditions inclusion on a published backend contract; the send workflow is P2-T08 and no send-state vocabulary exists anywhere | §23: pinned default — initiation affordance on approved Pesos only, never automatic, **no send mechanics and no send persistence in P2-T06**; currently unavailable-with-reason; the executable send contract is P2-T08's (Q-SEND) |
| S5 | **Per-CM decision carrier** — `Manter`/`Colocar de parte` are Comparação decisions (master §10/§15); Comparação (incl. `previous_peso_id`) is a **P2-T05 handoff remainder, NOT persisted** | §17: vocabulary and semantics fixed; **no carrier, no table, no route is invented** — persistence is enabled by the Comparação remainder contract (Q-PERCM) |
| S6 | **Folha decision carrier** — master §15 fixes Folha states and decision ownership; `controlo_sheet_id` is **not persisted** (P2-T05 handoff remainder) | §18: vocabulary/rules fixed as a seam; **no Folha routes/persistence now** (Q-FOLHA) |
| S7 | **Warnings** — the shipped P2-T05 read model carries **no warning facts** for plain Pesos (tolerances/nominals belong to Pegamentos/Comparação, not persisted) | §15.3: review renders warnings **only when the shared read model supplies them**; the "warnings never decide" invariant is binding regardless (Q-WARN) |
| S8 | **Pending-list/history query index** — P2-T05 §17.5 explicitly defers the `pesos.status`/`submitted_at` index to this slice | §7.3: exactly one additive index, justified by the pending-list query |
| S9 | **History `page`/`pageSize`** — DenseDataTable pages consumer-side; no authority fixes paging bounds | §15.2: 1-based paging, `pageSize` 1–100, deterministic ordering (Q-PAGE) |
| S10 | **Decision-event token for reopen** — the only vocabulary words in authority are the statuses and the verb `reabrir` (master §15) | §3.2: event tokens `aprovado`/`nao_aprovado`/`reaberto` (past participles consistent with the status tokens; label "Reaberto") |

---

## 2. Scope / non-scope

### 2.1 In scope (this contract authors all of it)

1. **Decision core** — approve / reject / reopen on the **same** `peso_id`, with exact
   preconditions, backend attribution, version semantics and atomicity (§19).
2. **Pending/review list (Aprovar)** — backend-reported reviewable facts, filters, exact
   `peso_id` selection, open/read (§15.1, §15.2).
3. **Exact-record review** — read-only review surface composed from the **shared** P2-T05
   `PesoSheetReadModel` (no fork, no copy, no second renderer) plus approval-only presentation
   facts (§15.3, §16).
4. **Decision/audit trail** — append-only decision history (who/when/`pesos.version`/reason/
   prior status) read and rendered through the accepted `AuditTrail` primitive (§6, §15.4).
5. **Histórico de Pesos (local)** — history list/detail inside Controlo Approve: filters,
   rows, selection/open, decision trail (§20).
6. **Schema/migration** — exactly ONE new decision table + ONE additive query index, in exactly
   ONE new additive migration (the sixth overall) (§6, §7, §25).
7. **Contracts** — repository/application interfaces, routes (all `controlo-approve`),
   authorization, failure vocabulary, concurrency, test-to-acceptance matrix (§8–§14, §26, §30).

### 2.2 Explicit non-scope (never implemented by P2-T06)

| Non-scope | Owner/authority |
|---|---|
| Editing submitted measurement facts without a reopen; any Peso-input mutation | Create (P2-T05 closed routes) — P2-T06 has zero write paths on Peso inputs/results (§19.5) |
| Approval-copy Peso; second Peso aggregate; second calculation model; second renderer/read model | forbidden (Identity Rule; authoritative data rule; module doc) |
| Definições / any operational setting (repairers, machine assignments, PDF directory, email lists, email templates, glass-density, water-density) | `Controlo_Create → Definições` (`…DELTA.md` §1/§2) — no settings surface of any kind here |
| Comparação building/rebuilding, `previous_peso_id` persistence, comparison-table creation | P2-T05 handoff remainder — **NOT AUTHORIZED** (§17, Q-COMP) |
| Per-CM decision **persistence** before its carrier exists | seam §17 (vocabulary fixed; carrier deferred) |
| Folha record/submission/decision **routes** before `controlo_sheet_id` exists | seam §18 (vocabulary fixed; carrier deferred) |
| PDF generation, file creation, directory writing, document regeneration, document identity, email sending/routing/transport | P2-T08 (NOT AUTHORIZED) — P2-T06 exposes only decision state/facts P2-T08 later consumes (§23, §24) |
| Boquilhas behavior: movement records, BQ aggregate, repairer resolution, Boquilhas history | P2-T07 (NOT AUTHORIZED) |
| Module availability registration, route registration, navigation exposure | P2-T10 — `CurrentBuildAvailable` stays `[]` (§22, BND-B4) |
| HISTÓRICO GLOBAL (`historia`) | DEFERRED BY DESIGN (master plan §0/§3.1) — never merged with the local Histórico |
| Generic lifecycle/state engine, revision infrastructure, generic audit system, Admin audit | forbidden (`RECORD_LIFECYCLES.md` §1; §22) |
| Mobile/tablet layouts, breakpoint reflow, card conversion, table-row action grids, header/navigation redesign | fixed desktop policy (§21; master plan) |

### 2.3 The carrier-boundary record (what exists vs what must wait)

Current authoritative schema (CLOSED P2-T05 + correction) contains **no** `previous_peso_id`
column/relation, **no** Comparação/Pegamentos/Folha/Resumo table, **no** warning facts on the
Peso read model, and **no** send-state vocabulary. This is not an omission to repair: the P2-T05
contract explicitly recorded Comparação/Pegamentos/Folha/Resumo and read-model rendering as
**P2-T05 handoff remainder — NOT AUTHORIZED** (P2-T05 Q-SCOPE; BND7). P2-T06 therefore:

- authors the **D-side contracts** that CAN operate today (approve/reject/reopen/decision trail/
  pending/history) over the real persisted `peso_id` + shared read model;
- fixes — as **closed vocabulary and semantics** — the parts that authority defines but whose
  carriers are absent (per-CM decisions §17, Folha decisions §18, Comparação read §17.2,
  warnings §15.3), **without inventing carrier tables, routes or state**;
- records the seams so the future remainder contracts (own gates) enable them additively.

---

## 3. Domain / state vocabulary

### 3.1 Peso status vocabulary (existing, CLOSED — consumed, never extended)

```text
pesos.status ∈ { 'pendente' | 'aprovado' | 'nao_aprovado' }
```

- P2-T05 writes only `pendente` (create/save/submit); P2-T06 writes `aprovado` (approve) and
  `nao_aprovado` (reject) **on the same row**, and restores `pendente` (reopen).
- **Reviewable (pending) predicate — fixed, consumed:** a Peso is in the review circuit **iff**
  `submitted_at IS NOT NULL` AND `status = 'pendente'` (P2-T05 §3.1). P2-T06 does not redefine
  it.
- **`Job On por associar`** is a context/association condition (pending `tool_id` anchor), not a
  status and not an error; it **does not block measurement, submission or approval**
  (`RECORD_LIFECYCLES.md` §4; master §5/§6).
- No fourth status exists; no queue status; no "sent"/"enviado" status (no authority).

### 3.2 P2-T06-owned decision vocabulary (new — the decision/audit trail)

```text
decision (event) ∈ { 'aprovado' | 'nao_aprovado' | 'reaberto' }
labels:            Aprovado | Não aprovado | Reaberto
```

- One row per human decision/transition event, append-only, attributed, versioned-at-decision
  (§6). `reaberto` token derives directly from the authority verb `reabrir` (master §15 "após
  rejeição o fluxo é reabrir → editar → resubmeter").
- The **Peso statuses** and the **decision events** are distinct facts: status is the record's
  current lifecycle; events are the immutable trail. No invented status names, no automatic
  events, no event for submission (submission is a Peso-row fact: `submitted_at`/
  `submitted_by_user_id`).

### 3.3 Per-CM decision vocabulary (closed words — carrier deferred, §17)

```text
per-CM decision ∈ { Manter | Colocar de parte }     (explicit human decisions only)
```

- Exactly these two values (master §10/§15; beta module doc "such as `Manter` / `Colocar de
  parte` where required" — the established pair is the only pair). No additional decision is
  invented.
- Never implied by the general Peso approval; never selected automatically; warnings/results
  never choose one (§17.1).

### 3.4 Folha decision vocabulary (closed words — carrier deferred, §18)

```text
controlo_sheet status ∈ { rascunho | submetida | aprovada | rejeitada }
```

- Master §15 is the only vocabulary authority: `Rascunho → Submetida → Aprovada / Rejeitada`;
  submission belongs to Controlo Create; decision (aprovar/rejeitar) and reopen belong to
  Controlo Approve; reopen moves a submitted or decided Folha back to `Rascunho`;
  reopened/corrected events are appended, never mutating prior history. Distinct from Peso
  status (`RECORD_LIFECYCLES.md` §7).
- Not persisted today → seam only (§18).

### 3.5 Result/refusal vocabulary (P2-T06 transport tokens)

| Token | HTTP | Meaning |
|---|---|---|
| `validation-failed` | 400 | `errors[]` carries the closed codes of §12.2 |
| `stale-version` | 409 | observed `pesos.version` no longer current; nothing written (accepted semantics) |
| `not-reviewable` | 409 | approve/reject/reopen on a Peso outside the allowed states (draft; never in the review circuit) |
| `already-decided` | 409 | approve/reject on a decided Peso; no second event, no status flip |
| `not-found` | 404 | the `peso_id` does not exist |
| `permission-denied` | 403 | module gate denial (never an empty list/blank surface) |

---

## 4. Existing identities consumed

| Identity | Consumed for | Source / authority | Created by P2-T06? |
|---|---|---|---|
| `peso_id` | the single record of decision: pending list, review, approve/reject/reopen, decision trail, history — **the same record from creation through review/decision** | P2-T05 §3.1 (backend-allocated; `pesos.peso_id`) | **NO** — never minted, never replaced |
| `cm_id` | production anchor — review context facts (frozen triple, processo) reached **through the shared read model only** | P2-T04 `cm_contexts`; P2-T05 §3.2 | **NO** |
| `tool_id` | pending/truthful anchor — review context reached **through the shared read model only**; approval never blocked by pending association | P2-T04 `tools`; P2-T05 §3.2 | **NO** |
| `jobon_id` | traversal-only production labels (reference/production number/machine) in list rows and the production projection | P2-T04; P2-T05 §6.2/§26.3 | **NO** (no `jobon_id` column on `pesos`; no direct reads of Job On tables) |
| `user_id` | backend-authored actor facts: `decided_by_user_id`; submission attribution display (`submitted_by_user_id`) | foundation `users`; `ICurrentAccountContext` | **NO** (referenced by FK) |
| `controlo_sheet_id` | Folha decision seam — **not persisted in the current schema** (§18) | master §13/§15 | **NO** |
| `peso_measurement_row_id` | decision-history presentation of "which record" only via the shared read model rows | P2-T05 §16.2 | **NO** |

**No duplicate Tool/Job On/CM identities; no `production_id`; no `job_on_revision_id`; no
`approval_peso_id`; no review-copy id; no client-generated id of any kind.**

---

## 5. New identities genuinely required

**Exactly one:** `peso_review_decision_id` — the persistence identity of one immutable decision
event (approve/reject/reopen) on one `peso_id`. Backend-allocated (`Guid.NewGuid()` /
`PesoReviewDecisionId.New()` in the application layer inside the decision transaction; DB default
`gen_random_uuid()` remains the foundation column convention only). It is:

- **not** a Peso identity (a Peso keeps its `peso_id` across every decision cycle);
- **not** a revision (`peso_review_decisions` is an event trail keyed by `peso_id`, not a record
  revision;
- not a document identity, not a send identity, not a production identity.

No other new identity exists in this contract: no `approval_peso_id`, no `review_id`, no
`job_on_revision_id`, no `controlo_sheet` placeholder id, no per-CM decision id (carrier
deferred, §17), no send-request id (§23).

---

## 6. Physical persistence schema

Engine: PostgreSQL (Supabase TEST runtime target), accepted foundation conventions (P2-T04 §3;
P2-T05 §16): explicit snake_case; PK `uuid` + `gen_random_uuid()` default, application-allocated;
`timestamp with time zone` + `now()`; closed sets via CHECK; **every FK `ON DELETE RESTRICT`**;
one `EntityTypeConfiguration` per entity discovered by `ApplyConfigurationsFromAssembly`;
`_context.Set<TEntity>()` repositories; `DmoDbContext.cs` byte-identical.

### 6.1 `peso_review_decisions` — the P2-T06 decision/audit trail (ONE new table)

| Column | Type | Null | Contract |
|---|---|---|---|
| `peso_review_decision_id` | `uuid` | NO | PK; default `gen_random_uuid()`; application-allocated within the decision transaction |
| `peso_id` | `uuid` | NO | FK → `pesos(peso_id)` `RESTRICT` — the decision binds the **same** record; no Peso delete path exists and this FK is the database-level proof that decisions never orphan a record id |
| `decision` | `text` | NO | CHECK `decision IN ('aprovado','nao_aprovado','reaberto')` — the closed §3.2 vocabulary |
| `decided_by_user_id` | `uuid` | NO | FK → `users(user_id)` `RESTRICT`; **backend-authored** actor from `ICurrentAccountContext` (never client-supplied) |
| `decided_at` | `timestamptz` | NO | backend clock; **backend-authored** (never client-supplied); the authoritative decision timestamp |
| `reason` | `text` | YES | CHECK `reason IS NULL OR btrim(reason) <> ''`; **required** for `nao_aprovado` and `reaberto` (validator; §19.3); always NULL for `aprovado` |
| `prior_status` | `text` | NO | CHECK `prior_status IN ('pendente','aprovado','nao_aprovado')` — the Peso status **before** this transition (trail readability: "which state was decided from") |
| `pesos_version_at_decision` | `integer` | NO | CHECK `pesos_version_at_decision >= 1` — the `pesos.version` **observed** by the actor at decision time (Identity Rule: "which record version was approved" is always recoverable) |
| `created_at` | `timestamptz` | NO | default `now()` |

No `updated_at`, no `version` token on decision rows: **decision rows are immutable after
COMMIT** — append-only by contract (§10.3, AC-MG3). No soft-delete/archive column. No column
duplicates any Peso operational fact (no water temperature, no density, no rows, no results —
the record remains the single source of truth; the *version* referenced is the pointer to "which
record version" was decided, as the Identity Rule requires).

### 6.2 No change to existing tables

`pesos` and `peso_measurement_rows` keep every column/CHECK/FK exactly as shipped (P2-T05 §16).
The **only** schema delta on `pesos` is the **one additive index** of §7.3 (an index is not a
column; `pesos` itself is not modified). No column is added to `pesos` — in particular **no**
`decided_at`/`decided_by_user_id` columns on `pesos` (those facts live in the decision trail;
the Peso row keeps its P2-T05 columns), no `previous_peso_id`, no `sent_at`, no
`controlo_sheet_id`.

Why not columns on `pesos` (recorded decision): the accepted model is record state on the row
(`status`) plus an attributed, versioned trail for the history that must survive cycles and
reopens; adding decision columns to `pesos` would (a) destroy the prior-decision facts on each
new decision, violating the Identity Rule's audit requirement, (b) force `pesos` to carry a
reopen/approve event log, and (c) change a CLOSED table's shape beyond the single deferred index.

### 6.3 Why exactly one table (recorded decision)

- The authoritative data rule forbids a second dataset for Peso facts; this table stores **no**
  Peso fact — only decision/audit metadata, which the Identity Rule explicitly assigns to
  P2-T06's own persistence ("approval decision/audit metadata … belongs to P2-T06's own
  decision/audit persistence … it never duplicates Peso operational facts").
- No approval-queue table (the pending list is the reviewable predicate over `pesos` itself —
  a **query**, not a queue table).
- No per-CM decision table today (carrier absent — §17.3).
- No Folha decision table today (carrier absent — §18.3).
- No send table (§23.4).
- No generic audit infrastructure (`RECORD_LIFECYCLES.md` §1).

---

## 7. Keys / constraints / indexes

### 7.1 Primary key

```text
PK_peso_review_decisions (peso_review_decision_id)
```

### 7.2 Foreign keys (all `ON DELETE RESTRICT`)

| FK | From | To |
|---|---|---|
| `FK_peso_review_decisions_pesos_peso_id` | `peso_review_decisions.peso_id` | `pesos(peso_id)` |
| `FK_peso_review_decisions_users_decided_by_user_id` | `peso_review_decisions.decided_by_user_id` | `users(user_id)` |

No cascading FK in this schema; deleting a Peso is impossible in the application (no path) and
fails closed at the database if ever attempted.

### 7.3 Indexes (each justified by a contracted query)

| Index | Table | Columns | Unique | Justified by |
|---|---|---|---|---|
| `IX_pesos_reviewable` | `pesos` | `status`, `submitted_at DESC` | NO | the **deferred pending-list query** (P2-T05 §17.5): `WHERE submitted_at IS NOT NULL AND status = 'pendente' ORDER BY submitted_at ASC` — the review queue; also serves the history query's `status`-filtered scans |
| `IX_peso_review_decisions_peso_id` | `peso_review_decisions` | `peso_id` | NO | FK-supporting + the per-Peso trail read (§15.4) — always loaded as an ordered set |
| `IX_peso_review_decisions_decided_at` | `peso_review_decisions` | `decided_at DESC` | NO | the Histórico list ordering when the queue is decision-time-ordered (§15.2) |

No other index is contracted. In particular no index is added for reference/machine/production
traversal filters: those predicates traverse `cm_id → job_ons` using P2-T04's existing keys, are
secondary in list queries, and no authority justifies speculative indexes (P2-T04 §12/Q12 and
P2-T05 §17.5 precedent).

### 7.4 CHECK constraints (exact)

| Name | Table | Expression |
|---|---|---|
| `peso_review_decisions_decision_check` | `peso_review_decisions` | `decision IN ('aprovado','nao_aprovado','reaberto')` |
| `peso_review_decisions_prior_status_check` | `peso_review_decisions` | `prior_status IN ('pendente','aprovado','nao_aprovado')` |
| `peso_review_decisions_reason_required_check` | `peso_review_decisions` | `(decision = 'aprovado' AND reason IS NULL) OR (decision <> 'aprovado' AND btrim(reason) <> '')` |
| `peso_review_decisions_version_check` | `peso_review_decisions` | `pesos_version_at_decision >= 1` |

(The reason CHECK encodes the §19.3 rule at the database backstop; the validator raises the typed
tokens first.)

### 7.5 Uniqueness

Only the PK is unique. Multiple decision events per `peso_id` are **required** (approve → reopen
→ approve …). No business unique tuple is invented (no "one decision per Peso").

---

## 8. Repository contracts

### 8.1 Layer placement (accepted conventions only)

| Layer | Location |
|---|---|
| domain types | `src/DMO.Domain/Controlo/` — `PesoReviewDecisionId`, decision kind (`PesoReviewDecisionKind` with the three tokens), `PesoReviewDecision` aggregate-free record; no new dependency |
| repository contract | `src/DMO.Application/Repositories/IPesoReviewRepository.cs` — **new** (P2-T05's `IPesoRepository` is a closed input, consumed read-only where needed; it gains no member) |
| application area | `src/DMO.Application/ControloApprove/` — `ControloApproveModels.cs`, `ControloApproveValidator.cs`, `IControloApproveService.cs`, `ControloApproveService.cs`, `ReviewSheetReadModel.cs` |
| persistence | `src/DMO.Infrastructure/Persistence/` — `Entities/PesoReviewDecisionEntity.cs`, `EntityConfigurations/PesoReviewDecisionEntityConfiguration.cs`, `PesoReviewRepository.cs`, `Migrations/<timestamp>_ControloApproveDomain.cs` (+ Designer + EF snapshot extension) |
| Web | `src/DMO.Web/Endpoints/ControloApproveEndpoints.cs`, `src/DMO.Web/Pages/Controlo/Approve/` (`Index.cshtml(.cs)` Aprovar, `Historico.cshtml(.cs)`), `ControloApprovePolicyNames.cs`, `wwwroot/css/dmo-controlo-approve.css`, `wwwroot/js/dmo-controlo-approve.js` |

No new .NET project; no generic repository abstraction, no mediator, no CQRS, no second
`DbContext` (accepted discipline).

### 8.2 `IPesoReviewRepository` (exact)

```csharp
namespace DMO.Application.Repositories;

public interface IPesoReviewRepository
{
    // Pending list — the reviewable predicate (submitted_at NOT NULL AND status = 'pendente'),
    // filters applied in SQL (§15.2), deterministic ordering, one page.
    Task<IReadOnlyList<PesoReviewRow>> GetPendingAsync(PendingListQuery query,
        CancellationToken cancellationToken);

    // Histórico de Pesos — records visible to Approve: submitted or with a decision trail
    // (§15.2), filters applied in SQL, one page.
    Task<IReadOnlyList<PesoReviewRow>> GetHistoryAsync(HistoryListQuery query,
        CancellationToken cancellationToken);

    // The decision trail of one peso_id, ordered by decided_at ASC.
    Task<IReadOnlyList<PesoReviewDecision>> GetDecisionsAsync(Guid pesoId,
        CancellationToken cancellationToken);

    // One decision event applied atomically: status transition on the existing pesos row
    // (version-guarded) + the append-only decision row, in ONE transaction (§10).
    Task<PesoReviewDecision> DecisionAsync(PesoReviewDecision decision, int expectedPesoVersion,
        CancellationToken cancellationToken);
}
```

Binding rules (same as P2-T04 §12.2 / P2-T05 §20.2): `CancellationToken` mandatory last;
`Task<T?>` single reads / `Task<IReadOnlyList<T>>` lists / `Task<T>` writes; the write opens its
own transaction; private static `Project(...)` mapping; constraint violations mapped via
`PostgresException.SqlState` + constraint name (e.g. `23514` on the reason CHECK → the same
validator token, never a 500); repositories own no domain rule; services contain no SQL.

**Where the Peso row read comes from:** the review-sheet read consumes P2-T05's **closed**
`IPesoRepository.GetByIdAsync(pesoId)` (row + rows) through the shared application service —
never a P2-T06 duplicate read of `pesos` internals. The decision **update** of the `pesos`
status/submitted columns is a P2-T06 write path (authorized by P2-T05 §5.7/§29: "P2-T06 writes
`'aprovado'`/`'nao_aprovado'` on the same row"; §7.3: "`submitted_at` is never cleared by Create
(reopen is P2-T06)"), implemented inside `PesoReviewRepository.DecisionAsync` as a guarded
single-row update + insert, exactly like the accepted sibling repositories.

---

## 9. Application service contracts

### 9.1 `IControloApproveService` (exact)

```csharp
namespace DMO.Application.ControloApprove;

public interface IControloApproveService
{
    Task<ReviewResult> GetPendingAsync(PendingListQuery query, CancellationToken cancellationToken);
    Task<ReviewResult> GetHistoryAsync(HistoryListQuery query, CancellationToken cancellationToken);
    Task<ReviewResult> GetReviewSheetAsync(Guid pesoId, CancellationToken cancellationToken);
    Task<ReviewResult> GetDecisionsAsync(Guid pesoId, CancellationToken cancellationToken);
    Task<ReviewResult> ApproveAsync(Guid pesoId, int expectedVersion, CancellationToken cancellationToken);
    Task<ReviewResult> RejectAsync(Guid pesoId, int expectedVersion, string reason, CancellationToken cancellationToken);
    Task<ReviewResult> ReopenAsync(Guid pesoId, int expectedVersion, string reason, CancellationToken cancellationToken);
}
```

`ControloApproveService` composes:
- `IControloCreateService.GetAsync` (shared application read → `Found(PesoSheetReadModel)`) — the
  **only** source of Peso facts (AC-RD1);
- `IPesoReviewRepository` — list queries and the decision write;
- `ICurrentAccountContext` — the backend actor for `decided_by_user_id` (never a command field);
- `IClock`/`DateTimeOffset.UtcNow` — backend `decided_at`.

The Web layer never queries the database directly; the service never touches P2-T04/P2-T05
repositories for writes; decision routes accept **no** Peso facts, no warnings, no calculation
output, no actor/time (AC-D2/D4).

### 9.2 Validators

`ControloApproveValidator` — pure static, runs before any write, returns the closed §12.2 code
set: `REJECT_REASON_REQUIRED` (reject without non-blank reason), `REOPEN_REASON_REQUIRED`
(reopen without non-blank reason), `FILTER_INVALID` (unknown/ill-formed filter values incl.
page/pageSize out of bounds), `APPROVAL_NOT_AVAILABLE` (defensive; a review sheet loaded for a
non-approvable state cannot be decided). Command carriers contain only identity/version/reason —
nothing else can be validated (by construction).

---

## 10. Transaction boundaries

All multi-row writes open their own transaction at the repository (`BeginTransactionAsync`,
accepted `TemplateRepository`/P2-T05 pattern). No ambient/unit-of-work, no second `DbContext`,
no isolation override, no advisory locks.

| Operation | Transaction boundary | Atomic unit |
|---|---|---|
| Approve (§19.1) | ONE transaction | `pesos` status `'aprovado'` + `version += 1` + decision row `('aprovado', actor, now, NULL, prior 'pendente', version-at-decision)` — all-or-nothing |
| Reject (§19.1) | ONE transaction | `pesos` status `'nao_aprovado'` + `version += 1` + decision row `('nao_aprovado', actor, now, reason, prior, version-at-decision)` — all-or-nothing |
| Reopen (§19.2) | ONE transaction | `pesos` status `'pendente'` + `submitted_at := NULL` + `submitted_by_user_id := NULL` + `version += 1` + decision row `('reaberto', actor, now, reason, prior, version-at-decision)` — all-or-nothing |
| Pending/history/review/trail reads | none — read-only | — |

Rules:
1. A forced mid-transaction failure leaves **zero** rows of the operation (no status flip without
   an event; no event without the transition) — test rows prove both orders (K2).
2. After a successful commit, a retry is a **new** decision attempt with the fresh observed
   version; after a rolled-back failure, a retry is a clean attempt. No idempotency key is
   invented (P2-T04 Q17/P2-T05 stance; double-submit analog = `already-decided`, §19.4).
3. **Decision rows are immutable after COMMIT** — there is no UPDATE/DELETE route, repository
   member or SQL for `peso_review_decisions`; the event trail is append-only by construction
   (AC-MG3). "Reopen preserves prior decision/actor/timestamp" is therefore structural, not
   conventional.

---

## 11. Concurrency / version behavior

### 11.1 Mechanism (accepted, reused exactly)

- Every decision operation observes `pesos.version` (`expectedVersion` in the carrier) and:
  (1) in-transaction compare → `ConcurrencyConflictException` (domain type); (2) the EF
  concurrency token stays active through the **`SaveAsync` helper** mapping
  `DbUpdateConcurrencyException` via the accepted `ConcurrencyConflictExceptionMapping` — the
  P2-T05 §19/P2-T04 §15.1 pattern, so the save-time race surfaces as **409 `stale-version`**,
  never 500 and never a silent overwrite.
- Services translate `ConcurrencyConflictException` → `Refused(StaleVersion)` → 409.
- Decision rows carry no token (immutable after COMMIT — §10.3); the Peso's version is the
  guard.

### 11.2 Per-operation matrix

| Operation | Observed version | On staleness | Rows written on staleness |
|---|---|---|---|
| Approve | `expectedVersion` | `Refused(StaleVersion)` (409) | 0 |
| Reject | `expectedVersion` | `Refused(StaleVersion)` (409) | 0 |
| Reopen | `expectedVersion` | `Refused(StaleVersion)` (409) | 0 |

### 11.3 Rules

**No silent overwrite, no automatic merge, no automatic retry.** A refused decision reports the
typed reason; the surface enters the accepted **`conflict` presentation with an explicit reload
recovery** (`renderConflict`, the D2 pattern shipped by P2-T05: clear heading — "Conflito — os
dados foram alterados por outra ação; nada foi guardado." — typed server message, one explicit
"Recarregar estado atual" action that reloads the authoritative state; the observed version is
refreshed **only** on success). Reads never bump; one version increment per committed decision;
re-submission after reopen bumps again via Create's accepted submit route.

### 11.4 Double-decision guard

- Approve on a decided Peso (`aprovado`/`nao_aprovado`) → 409 `already-decided`, no second
  event, no status flip.
- Reject on a decided Peso → 409 `already-decided`.
- Approve/reject/reopen on a **draft** (never submitted) → 409 `not-reviewable` (a draft never
  entered the review circuit; it is Create's object).
- Reopen on a draft → 409 `not-reviewable` (nothing to reopen; Create edits drafts directly).

---

## 12. Result / error vocabulary

### 12.1 Result unions (closed sets)

```csharp
namespace DMO.Application.ControloApprove;

public abstract record ReviewResult
{
    public sealed record PendingFound(IReadOnlyList<PendingItemReadModel> Rows, int Total) : ReviewResult;
    public sealed record HistoryFound(IReadOnlyList<HistoryItemReadModel> Rows, int Total) : ReviewResult;
    public sealed record ReviewSheet(ReviewSheetReadModel Sheet) : ReviewResult;
    public sealed record DecisionsFound(IReadOnlyList<DecisionItemReadModel> Decisions) : ReviewResult;
    public sealed record Approved(Guid PesoId, int Version, DateTimeOffset DecidedAt) : ReviewResult;
    public sealed record Rejected(Guid PesoId, int Version, DateTimeOffset DecidedAt) : ReviewResult;
    public sealed record Reopened(Guid PesoId, int Version, DateTimeOffset DecidedAt) : ReviewResult;
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : ReviewResult;
    public sealed record NotFound(Guid PesoId) : ReviewResult;
    public sealed record Refused(ReviewRefusalReason Reason, string Message) : ReviewResult;
}

public enum ReviewRefusalReason
{
    StaleVersion,        // 409 stale-version
    NotReviewable,       // 409 not-reviewable
    AlreadyDecided,      // 409 already-decided
}
```

### 12.2 Transport tokens (exact)

| Token | Where | Meaning |
|---|---|---|
| `validation-failed` (400) | every mutation | `errors[]` carries the closed codes: `REJECT_REASON_REQUIRED`, `REOPEN_REASON_REQUIRED`, `FILTER_INVALID`, `APPROVAL_NOT_AVAILABLE` |
| `stale-version` (409) | approve/reject/reopen | observed version no longer current; nothing written |
| `not-reviewable` (409) | approve/reject/reopen | the Peso is outside the allowed states (draft) |
| `already-decided` (409) | approve/reject | the Peso is already `aprovado`/`nao_aprovado`; no second event |
| `not-found` (404) | reads and decision targets | the `peso_id` does not exist |
| `permission-denied` (403) | every route | module gate denial — never an empty list, never a blank surface (freeze §4) |

Denial rules are the accepted ones: 409 is the single refusal status for persisted-state
refusals; no error body contains secrets/other users' data; `permission-denied` presentation is
never a blank surface.

---

## 13. Route matrix

### 13.1 Base path

```text
Controlo Approve   /controlo/approve     (Aprovar — page)      + /controlo/approve/historico (page)
```

`controlo` is the accepted shared `DestinationId` of `ControloCreate`/`ControloApprove`
(`ModuleCatalog.cs`); **no destination/route registration happens in P2-T06** (interim state
§14.3; P2-T10 owns registration).

### 13.2 Complete P2-T06 route table (exactly nine rows)

| # | Route | Kind | Purpose | Policy | Request carrier | Result carrier | Failure states |
|---|---|---|---|---|---|---|---|
| 1 | `GET /controlo/approve` | page `Pages/Controlo/Approve/Index` | Aprovar surface: pending list + filters + review + decision regions (§21) | `controlo-approve` | `?pesoId=` (open selected/exact record) | rendered page | 403 |
| 2 | `GET /controlo/approve/historico` | page `Pages/Controlo/Approve/Historico` | Histórico de Pesos surface: filters + list + detail (§20, §21) | `controlo-approve` | `?pesoId=` (open exact record) | rendered page | 403 |
| 3 | `GET /controlo/approve/pending` | minimal API | pending-list query (reviewable facts + filters) | `controlo-approve` | `PendingListQuery` query string | 200 `PendingFound` | 400 `FILTER_INVALID`, 403 |
| 4 | `GET /controlo/approve/history` | minimal API | Histórico list query (filters) | `controlo-approve` | `HistoryListQuery` query string | 200 `HistoryFound` | 400 `FILTER_INVALID`, 403 |
| 5 | `GET /controlo/approve/pesos/{pesoId:guid}` | minimal API | exact-submitted-record review sheet: shared `PesoSheetReadModel` + approval facts (§15.3) | `controlo-approve` | route `pesoId` | 200 `ReviewSheet` | 404, 403 |
| 6 | `GET /controlo/approve/pesos/{pesoId:guid}/decisions` | minimal API | the decision trail of the exact Peso (§15.4) | `controlo-approve` | route `pesoId` | 200 `DecisionsFound` | 404, 403 |
| 7 | `POST /controlo/approve/pesos/{pesoId:guid}/approve` | minimal API | human approve on the same `peso_id` | `controlo-approve` | `{ expectedVersion }` | 200 `Approved` | 400, 404, 409 `stale-version` \| `not-reviewable` \| `already-decided`, 403 |
| 8 | `POST /controlo/approve/pesos/{pesoId:guid}/reject` | minimal API | human reject with reason on the same `peso_id` | `controlo-approve` | `{ expectedVersion, reason }` | 200 `Rejected` | 400 `REJECT_REASON_REQUIRED`, 404, 409 `stale-version` \| `not-reviewable` \| `already-decided`, 403 |
| 9 | `POST /controlo/approve/pesos/{pesoId:guid}/reopen` | minimal API | human reopen with reason on the same `peso_id` | `controlo-approve` | `{ expectedVersion, reason }` | 200 `Reopened` | 400 `REOPEN_REASON_REQUIRED`, 404, 409 `stale-version` \| `not-reviewable`, 403 |

**Route-count statement:** exactly the nine rows above. No Definições route, no settings route,
no PDF/file/email route, no send route, no Boquilhas route, no Folha route (carrier absent —
§18.3), no per-CM decision route (carrier absent — §17.3), no route carrying a second policy,
no route accepting Peso facts, no route registering anything. The pending/history list pages may
server-render their initial page from the same query contracts (page load = route 3/4 response
embedded), keeping exactly one query shape (P2-T05 §4.2 "one query shape" precedent).

### 13.3 Interim runtime state (mandatory — P2-T05 §21.6 pattern)

```text
ModuleRegistrations.CurrentBuildAvailable = []            (unchanged by P2-T06)
EmptyDestinationRouteRegistry                             (unchanged)
DestinationRouteRegistrations                             (unchanged — still empty)
```

P2-T06 pages/endpoints exist and are server-gated, but `AccessResolver` denies every P2-T06
route to every caller **until P2-T10 registers `controlo-approve` availability** (step P2-T10c of
the master plan §13). Integration tests use test-only module registries.

---

## 14. Authorization policy per route

| Concern | Contract |
|---|---|
| Module | `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloApprove)` on **every** P2-T06 route/action — pages and endpoints alike; the Razor `[Authorize]` constants are pinned in `ControloApprovePolicyNames` and asserted equal to the canonical projection of `ModuleCatalog.ControloApprove` (the accepted `ControloPolicyNames`/`JobOnPolicyNames` pattern) |
| Create vs Approve | Create and Approve are **independent grants**. Having `controlo-create` does NOT grant any P2-T06 route; having `controlo-approve` does NOT grant any P2-T05 route (incl. every Definições route/action). The shared `controlo` destination never merges the grants (`NoProfilesRegressionTests` sibling rule; `ACCESS_AND_NAVIGATION.md`) |
| ADMIN | ADMIN gains **no** operational access (`ModuleAuthorizationHandler` fails closed for non-USER accounts); no `dmo.administration` policy on any P2-T06 route |
| Direct-route enforcement | hiding a control is never authorization: every route is denied server-side for non-granted callers, including direct URL entry |
| Presentation | navigation/button visibility never substitutes for the gate; denial is never an empty list/blank surface |
| Cross-module reads | the review sheet and list rows compose the **shared** application read under the P2-T06 route — the route's policy gate is P2-T06's; no P2-T06 code calls a P2-T05-gated HTTP route |

---

## 15. Review list / read / detail contracts

### 15.1 Pending list (Aprovar) — the review queue

**Query contract (exact):**

```csharp
public sealed record PendingListQuery(
    string? Reference,          // traversal cm_id -> job_on.reference (pending Peso: no match)
    string? ProductionNumber,   // traversal cm_id -> job_on.production_number
    string? Machine,            // traversal cm_id -> job_on.machine
    string? Processo,           // traversal cm_id -> tool.process (NNPB|PS)
    string? ToolReference,      // frozen cm triple tool_reference | pending tool reference
    DateTimeOffset? SubmittedFrom, DateTimeOffset? SubmittedTo,
    int Page = 1, int PageSize = 50);
```

- Predicate (fixed): `submitted_at IS NOT NULL AND status = 'pendente'`; **every filter is a
  backend SQL predicate** (never client-side filtering of a fetched set); ordering
  `submitted_at ASC` (oldest first — review queue), tie-break `peso_id ASC` (deterministic);
  paging 1-based, `1 <= PageSize <= 100` (`FILTER_INVALID` otherwise).
- Filters are **over backend-reported reviewable facts only**: the traversal facts above are the
  backend-reported production context of the reviewable record; a filter value that matches
  nothing returns an **explicit empty** (never a fallback list, never an error conflated with
  lookup failure).
- `Total` is the backend-counted row count for the same predicate + filters (paging metadata;
  DenseDataTable consumer-owned paging).

**Row shape:**

```csharp
public sealed record PendingItemReadModel(
    Guid PesoId, int Version, string Status /* 'pendente' */,
    Guid? CmId, Guid? ToolId,                       // exactly one non-null (anchor facts)
    string? Reference, string? ProductionNumber, string? Machine,   // traversal (NULL when pending)
    string? ToolReference, string? Processo,        // frozen triple reference | pending tool; processo traversal
    DateTimeOffset SubmittedAt, Guid SubmittedByUserId,
    int RowCount,                                    // backend-reported count of measurement rows
    bool HasDecisionTrail);                          // false for a first-time review
```

(Exact column set is the contract; all facts are backend-authored — no client-invented column.)

### 15.2 Histórico list — the local Histórico query

```csharp
public sealed record HistoryListQuery(
    string? ReviewState,        // filter vocabulary — see below (NOT a status)
    string? Reference,          // traversal cm_id -> job_on.reference
    string? ProductionNumber,   // traversal cm_id -> job_on.production_number
    string? Machine,            // traversal cm_id -> job_on.machine
    string? Processo,           // traversal cm_id -> tool.process (NNPB|PS)
    string? ToolReference,      // frozen cm triple tool_reference | pending tool reference
    DateTimeOffset? SubmittedFrom, DateTimeOffset? SubmittedTo,
    DateTimeOffset? DecidedFrom, DateTimeOffset? DecidedTo,
    Guid? DecidedByUserId, string? Decision,     // trail facts
    int Page = 1, int PageSize = 50);
```

**Decision (pinned):** the Histórico **filter vocabulary** uses the existing status values plus
the explicit review-scope value `submetido`:

- `ReviewState` ∈ `{ submetido | pendente | aprovado | nao_aprovado }`, where `submetido` is the
  documented alias for the reviewable predicate (`submitted_at NOT NULL AND status =
  'pendente'`). This is a **filter vocabulary, not a status**: no fourth status is invented; the
  filter selects records **by their current Peso status**, with `submetido` selecting the
  reviewable subset; `pendente` as a filter selects submitted, still-undecided records (same
  predicate, both spellings select the same set — both documented, no divergence). The complete
  filter set:
- `Reference`, `ProductionNumber`, `Machine`, `Processo`, `ToolReference` (as §15.1);
- `SubmittedFrom/To`, `DecidedFrom/To` (decision-time range over the trail);
- `DecidedByUserId`, `Decision` ∈ `{ aprovado | nao_aprovado | reaberto }` (trail facts);
- paging/ordering: `submitted_at DESC`, tie-break `peso_id DESC`.

**Visibility predicate (pinned):** a Peso appears in Histórico when it is **submitted**
(`submitted_at IS NOT NULL`) **or** it has at least one decision event (so a reopened draft keeps
its trail visible — "decision/reopen trail" remains readable; `RECORD_LIFECYCLES.md` §4
"preserves approval/rejection/reopen history and actors"). `ReviewState = 'pendente'` filters
submitted-undecided records; drafts never appear (they never entered the review circuit).

**Row shape:** `HistoryItemReadModel(Guid PesoId, int Version, string Status, Guid? CmId,
Guid? ToolId, string? Reference, string? ProductionNumber, string? Machine, string? ToolReference,
string? Processo, DateTimeOffset? SubmittedAt, Guid? SubmittedByUserId,
DecisionSummary? LastDecision, int DecisionCount)` where `DecisionSummary(string Decision, Guid
DecidedByUserId, DateTimeOffset DecidedAt, string? Reason)`.

### 15.3 Exact-record review — the review sheet (read-only)

```csharp
public sealed record ReviewSheetReadModel(
    PesoSheetReadModel Peso,                    // THE shared P2-T05 type — consumed, never duplicated (§16)
    IReadOnlyList<DecisionItemReadModel> Decisions,
    ReviewAvailability Availability);

public sealed record ReviewAvailability(
    bool CanApprove, string? ApproveDisabledReason,
    bool CanReject, string? RejectDisabledReason,
    bool CanReopen, string? ReopenDisabledReason);

public sealed record DecisionItemReadModel(
    Guid PesoReviewDecisionId,
    string Decision,                            // 'aprovado' | 'nao_aprovado' | 'reaberto'
    Guid DecidedByUserId, DateTimeOffset DecidedAt,
    string? Reason,                             // NULL for 'aprovado'
    string PriorStatus,
    int PesoVersionAtDecision);
```

- The sheet renders: the shared read-only Peso presentation (production context, CM context,
  Tool information, measurements, water temperature, frozen water result facts (density echo via
  calculation display facts where the read model supplies them), frozen glass density,
  calculated capacity/glass weight per row, submit attribution, and — **only when the shared
  read model supplies them** — warnings and the comparison relation) — **exactly** what the
  shared model carries today (S7): inputs, frozen density, per-row results, attribution,
  production projection. **No recalculation, no re-resolution, no reinterpretation.**
- Reviewability presentation: `CanApprove`/`CanReject` true only for the reviewable predicate;
  `CanReopen` true only for reviewable or decided records; disabled states carry the associated
  reason (DecisionBar contract). On decided records the action region shows the decision facts
  (who/when/version) plus reopen.
- **Warnings (Q-WARN, pinned):** the current authoritative read model carries no warning facts
  for Peso; the review surface therefore renders **no** warning region today. When (and only
  when) the shared read model is extended with warning carriers by a future authored contract,
  the review surface renders them **as evidence only** — never as a decision input, never
  auto-selecting, never blocking (the invariant is contracted and tested with the decision-route
  shape, AC-D2). No warning region, type or field is invented now.

### 15.4 Decision trail read

`GET /controlo/approve/pesos/{pesoId}/decisions` returns every event in `decided_at ASC` order
(backed by `IX_peso_review_decisions_peso_id`). Rendered with the accepted **`AuditTrail`
primitive** (supplied entries only; never synthesizes actor/time from the current session; entry
text = decision label + prior status + version-at-decision; timestamp/actor from the event,
never the session). Empty trail = explicit empty (a reviewable first-time record).

---

## 16. Shared Peso read-model reuse (NO FORK — the renderer contract)

**Contract decisions (exact):**

1. **One read model.** The review sheet embeds the **exact** `PesoSheetReadModel` type published
   by P2-T05 (`src/DMO.Application/ControloCreate/PesoSheetReadModel.cs`) — the same C# record,
   obtained through the **same** application read (`IControloCreateService.GetAsync`), under
   P2-T06's own `controlo-approve`-gated route (P2-T05 §29 seam: "D consumes this shape through
   its own `controlo-approve`-gated routes"). P2-T06 declares **no** second Peso read-model
   type; no copied shape; no P2-T06-local projection of Peso facts (RD1).
2. **No fork, no copy, no second renderer.** The shipped P2-T05 Peso-sheet renderer is
   page-owned inside the CLOSED `Pages/Controlo/Create.cshtml` (no shared sheet partial exists —
   verified against the shipped tree). Reuse is therefore contractually defined as:
   - the review page renders **from the same read model and the same presentation discipline**:
   identical field set, identical field order, identical labels, identical ≤ 2-dp decimal
   normalization, identical Portuguese separators, no field hidden from the review view that the
   Create read-only (submitted) view shows, no field added that changes domain meaning (module
   doc: "no divergent field ordering or hidden result set that changes meaning; review mode may
   configure visibility/actions but must not fork the domain representation");
   - **renderer-parity contract test** (RD2): one fixture → Create's submitted view and Approve's
   review view → the same operational facts appear with the same labels/order/normalization
   (rendered test, mechanically compared via the page's `data-dmo-*` hooks).
   - The alternative — extracting a shared `_PesoSheetReadOnly` partial and rewiring the CLOSED
   `Create.cshtml` to consume it — is a **cross-stream change** to P2-T05-owned code and is
   recorded as the Architect-selectable option (Q-RENDER); under this contract's default it is
   **not** done (nothing in P2-T05 is modified).
3. **Composition, not replacement.** Approval-only presentation (decisions, availability,
   disabled reasons) lives in the P2-T06 wrapper types (`ReviewSheetReadModel`), **around** the
   shared model — the Peso facts model is never altered, extended-with-semantics or replaced
   (RD3). P2-T06 adds **no** field to Peso facts presentation that pretends to be a Peso fact.
4. The review page is a **read-only** surface: no editable measurement control, no submit/associate
   control, no Create action (the submitted view omits editable actions — the D1 pattern is
   preserved in reverse: the review view never renders Create actions at all).

---

## 17. Per-CM decision contract

### 17.1 Closed vocabulary and semantics (binding — master §10/§15; `CONTROLO_APPROVE.md`
"per-CM human decisions such as `Manter` / `Colocar de parte` where required")

- The per-CM decision vocabulary is **exactly** `Manter` and `Colocar de parte` (labels
  "Manter", "Colocar de parte"). No additional decision is invented.
- Per-CM decisions apply to **Comparação** (the comparison Peso workflow) — the initial Peso
  approval is general for the controlled set and **never implies** them; they are explicit
  human facts; justification is required where the Comparação authority requires it ("and
  justification where required", master §10).
- **Warnings/calculation results never choose one** — the decision input is human-only, and no
  code path may write a per-CM decision except an explicit human action (AC-D2/D3).

### 17.2 Carrier truth (exact — do not infer missing behavior)

The explicit comparison relation `current_peso_id → previous_peso_id` and the comparison Peso
record **are not persisted in the current accepted schema** (P2-T05 Q-SCOPE/§29; BND7: "no
`previous_peso_id` column exists" — verified in the closed implementation). The D-side Comparação
**read** contract is therefore:

- **when a carrier exists** (future Comparação remainder contract, its own gate): the review
  surface composes the persisted relation through the shared read model exactly as published —
  the exact `previous_peso_id`, current/previous context for human understanding, **no
  auto-select, no heuristic reconstruction, no "latest Peso" substitution, no mutation of the
  pairing by any decision route**;
- **today**: no carrier exists ⇒ the review surface renders **no** comparison region (explicit
  absence; never a fabricated or reconstructed comparison; never a heuristic pair).

### 17.3 Persistence decision (pinned — Q-PERCM)

P2-T06 authors **no** per-CM decision table, route or type in this contract. The per-CM decision
facts belong with the Comparação carrier (the comparison record's per-CM rows), whose ownership
is the P2-T05 handoff remainder. When that remainder contract is authored (own gate), it —
together with this D-side contract's vocabulary — defines the per-CM decision persistence
(ownership: the comparison workflow's review; cardinality: one human decision per comparison CM
row; history: attached to the comparison record's decision trail; never automatic). The negative
tests of §26.4 prove that P2-T06 introduces **no** invented per-CM persistence (AC-CP4, AC-D3).

---

## 18. Folha decision contract

### 18.1 Closed vocabulary and ownership (binding — master §13/§15; `RECORD_LIFECYCLES.md` §7;
`CONTROLO_APPROVE.md` "Folha decision/review"; ACCESS_MODEL §9 "Folha decision")

- `controlo_sheet_id` = one persisted Folha de Controlo (master §3/§13): five family items
  CM/MF/BQ/PU/CS, each with its established OK/NOK state and observation; family context from
  the Job On (`controlo_sheet_id → jobon_id`); **distinct final human Controlo decision and
  attribution** — never collapsed into Peso approval.
- Folha lifecycle (master §15): `Rascunho → Submetida → Aprovada / Rejeitada`; only a submitted
  Folha enters the decision circuit; **submission belongs to Controlo Create; decision
  (approve/reject) and reopen belong to Controlo Approve**; reopen moves a submitted or decided
  Folha back to `Rascunho`; after rejection the flow is `reabrir → editar → resubmit`;
  reopened/corrected events are appended, never mutating prior history.
- Reviewable fields (master §13): the five items' OK/NOK states and observations, the sheet's
  submission facts, actor/time facts of the decision, the relation to the Job On (and, through
  the Job On, to Pesos — traversal only).

### 18.2 Carrier truth (exact)

`controlo_sheet_id` **is not persisted in the current accepted schema** (P2-T05 Q-SCOPE: "Folha
(`controlo_sheet_id`) … explicitly not authored here — no table, route or behavior"; BND7
verified). The P2-T06 Folha contract is therefore a **seam**:

- the D-side decision rules above are fixed authority (vocabulary, ownership of the decision and
  reopen, append-only correction);
- **no Folha route, page region with fabricated data, table or type is implemented now** —
  "Do not infer missing behavior"; the Aprovar surface shows **no** Folha decision surface until
  the Folha record + its Create-side submission exist (P2-T05 handoff remainder, own gate);
- when the Folha remainder contract is authored, this contract's D-side rules bind its decision
  surface: operate on the exact persisted `controlo_sheet_id` (no replacement sheet), decision
  state `aprovada`/`rejeitada`, actor/time backend facts, reopen to `rascunho` with appended
  events, Peso/Job On relationship via traversal, and the same stale-version/conflict semantics.

### 18.3 Persistence decision (pinned — Q-FOLHA)

P2-T06 authors **no** Folha table, no Folha decision table, no Folha routes. (Q-FOLHA, ACCEPT
DEFAULT; changing this requires the Folha record contract first.)

---

## 19. Approve / reject / reopen semantics

### 19.1 Approve and reject (exact)

```text
POST /controlo/approve/pesos/{pesoId}/approve   carrier { expectedVersion }
POST /controlo/approve/pesos/{pesoId}/reject    carrier { expectedVersion, reason }

BEGIN
 1. load pesos (+ rows, tracked)
 2. assert exists                                  else NotFound
 3. assert version == expectedVersion              else ROLLBACK -> Refused(StaleVersion)
 4. assert submitted_at IS NOT NULL                else ROLLBACK -> Refused(NotReviewable)
 5. assert status == 'pendente'                    else ROLLBACK -> Refused(AlreadyDecided)
 6. validate (reject: btrim(reason) <> '')         else ValidationFailed(REJECT_REASON_REQUIRED)
 7. insert decision row: (peso_id, 'aprovado'|'nao_aprovado', current USER, now(),
    reason|NULL, prior_status = 'pendente', pesos_version_at_decision = observed version)
 8. update pesos: status := 'aprovado'|'nao_aprovado', version += 1, updated_at := now()
 9. COMMIT
```

- Both are **human-only** actions: the routes accept no warning/calculation/reason-free
  bypass; no route, worker or event auto-approves/auto-rejects (AC-D1/D2).
- **Approve never rejects on warnings and reject never auto-triggers**: `status` changes only
  per an explicit human action on the route.
- Actor/time are **backend facts** (`ICurrentAccountContext`, backend clock) — never client
  fields (AC-D4).
- The decision row records `prior_status` and `pesos_version_at_decision` = the observed
  version (Identity Rule: who/when/which version approved).

### 19.2 Reopen (exact)

```text
POST /controlo/approve/pesos/{pesoId}/reopen    carrier { expectedVersion, reason }

BEGIN
 1. load pesos (+ rows, tracked)
 2. assert exists                                  else NotFound
 3. assert version == expectedVersion              else ROLLBACK -> Refused(StaleVersion)
 4. assert status IN ('pendente','aprovado','nao_aprovado') AND submitted_at IS NOT NULL
     OR status IN ('aprovado','nao_aprovado')      else ROLLBACK -> Refused(NotReviewable)
                                                  (draft: never submitted -> NotReviewable)
 5. validate: btrim(reason) <> ''                  else ValidationFailed(REOPEN_REASON_REQUIRED)
 6. insert decision row: (peso_id, 'reaberto', current USER, now(), reason,
    prior_status = current status, pesos_version_at_decision = observed version)
 7. update pesos: status := 'pendente', submitted_at := NULL, submitted_by_user_id := NULL,
    version += 1, updated_at := now()
 8. COMMIT
```

**Reopen semantics — the exact mapping of the closed-state contract (Q-REOPEN, pinned):**

- **Same identity:** the SAME `peso_id`. No copy, no replacement, no new record (I3, AC-O1).
- **Draft-editable handoff restored:** `submitted_at`/`submitted_by_user_id` are cleared so the
  **accepted, CLOSED** P2-T05 edit route (route 7) and submit route (route 9) work again —
  both assert `submitted_at IS NULL`; P2-T05 §7.3 explicitly records "`submitted_at` is never
  cleared by Create (reopen is P2-T06)". The reopened record is a normal `pendente` draft in
  Create's hands.
- **`status = 'pendente'` (not `nao_aprovado`) — why (recorded, Q-REOPEN):** the decided-status
  alternatives are mechanically impossible with the closed submit route: the shipped submit
  writes only `submitted_at`/`submitted_by_user_id` and never touches `status` (verified in the
  shipped `ControloCreateService.SubmitAsync`), and the reviewable predicate requires
  `status = 'pendente'`; a reopened record left `nao_aprovado` could therefore never become
  reviewable again (P2-T05 §3.1, §7.4). The Identity Rule's obligations are all satisfied:
  (a) same `peso_id` ✓; (b) prior decision/attribution history preserved in the append-only
  trail ✓; (c) draft-editable handoff restored so the accepted submit route works again ✓;
  (d) **"any material edit to an approved Peso leaves it `nao_aprovado` — never `aprovado` —
  before re-submission"**: after reopen the record is `pendente` and remains so through Create's
  edits (the closed edit path never changes status); it is **never** `aprovado` without a NEW
  human approval — and because the reopened record must be resubmitted before any reviewer can
  act on it again, the prior `aprovado` is superseded by the draft and re-approval is the only
  path back. **"Material edit" (defined precisely, per the Identity Rule):** any change to Peso
  inputs or computed results (water temperature, volumes, SAP references, the measurement row
  set) — i.e. any change the Create edit route can make; it is precisely the "post-reopen
  edit" that forces the re-approval cycle. Reopen itself touches no measurement fact (AC-O5).
- **Attribution and audit:** the reopen event carries actor/time/reason and the prior decision
  remains in the trail untouched — no historical erase (O2, AC-O2/O3).
- **Reopen is not an editor:** P2-T06 owns no measurement-edit path; editing after reopen is
  exclusively Create's accepted edit route (AC-O5).

### 19.3 Reason requirements (pinned — Q-NOTE)

- Reject: non-blank reason **required** (`REJECT_REASON_REQUIRED`; DB CHECK backstop).
- Reopen: non-blank reason **required** (`REOPEN_REASON_REQUIRED`; DB CHECK backstop).
- Approve: no reason (always NULL).

### 19.4 Double-decision / invalid-state guards

- approve/reject on a decided Peso → 409 `already-decided` (no second event, no flip);
- approve/reject/reopen on a draft → 409 `not-reviewable`;
- double submit remains Create's `already-submitted` (P2-T05 routes untouched).

### 19.5 What P2-T06 never writes (structural negative)

P2-T06 write paths touch **only** `pesos.status`, `pesos.version`, `pesos.updated_at`,
`pesos.submitted_at`, `pesos.submitted_by_user_id` (reopen) and the decision table. No P2-T06
method, SQL or route updates any other `pesos` column or any row of `peso_measurement_rows`
(R2, AC-R1/O5).

---

## 20. Local Histórico behavior

### 20.1 What it is

**HISTÓRICO (local)** inside Controlo Approve: "Histórico de Pesos" — history **within the
module** (master plan §0.1; `…DELTA.md` §2.4). It is **not** HISTÓRICO GLOBAL (`historia`,
DEFERRED BY DESIGN); neither is authority for the other (§20.4; AC-H4).

### 20.2 Contract

- **Filters:** the §15.2 set (review state, production/tool facts via traversal, submitted and
  decided time ranges, decision actor/outcome) — all **backend-applied** over backend-reported
  facts; invalid filters → 400 `FILTER_INVALID` (never a silent full list).
- **Rows:** exact `peso_id`, version, current status, anchor/tool reference, production context
  (traversal), submission facts, last decision summary, decision count (§15.2 row shape).
- **Single click selects the row; double click opens the exact Peso (review/detail).** Actions
  (Reabrir, Ver decisões, Reabrir-then-approve flows) live **outside the table** — the accepted
  DenseDataTable selection surface + DecisionBar pattern (P2-T02/P2-T03 contracts). **No
  repeated per-row action button grid** is rendered (AC-H3; P2-T02 §4 binding).
- **Detail:** opening a row loads the exact record via `GET /controlo/approve/pesos/{id}` —
  the review sheet (shared read model + decision trail + availability).
- **Decision/reopen trail:** visible on the detail; append-only; actor/time/version/reason per
  event; renders via `AuditTrail` (never synthesizing actor/time).
- **Deterministic ordering** (§15.2) and paging (1–100).

### 20.3 Rendering discipline

The Histórico page uses the shared `DenseDataTable` (selection/open semantics, keyboard
reachability, programmatic non-color-only selection), `RecordStatus` for status text, `AuditTrail`
for the trail, `DecisionBar` for outside-table actions, and `CommonState` for
loading/empty/lookup-failed/unavailable/permission-denied (all four distinct; lookup failure is
never an empty; denial is never a blank surface). Consumer-owned paging; the component fetches
nothing.

### 20.4 HISTÓRICO GLOBAL boundary

No `historia` route, entry, registration, link or label change; the page lives under the
`controlo` destination inside the Approve surface; the local Histórico never aggregates other
modules' histories.

---

## 21. Fixed-desktop UI regions

Binding **DMO FIXED DESKTOP LAYOUT POLICY** (master plan; freeze §1A; P2-T02 §4; P2-T03 §6;
P2-T05 §24). Canonical 1366 × 768; compact density; region-stable composition; no
breakpoint-driven structural variant (`@media`/`@container`/`@supports` structural rules, width
listeners, control relocation, table→card conversion, required-column hiding, action relocation);
smaller windows scroll (keyboard-reachable local overflow for wide regions); mobile/tablet out of
scope; the shared header/navigation system is **not** redesigned (P2-T06 pages consume the
existing shell; secondary navigation uses the existing local-link pattern — no shell file
changes).

### 21.1 Aprovar — `Pages/Controlo/Approve/Index` (regions, exact)

| Region | Contract |
|---|---|
| **R1 — Filters** | pending-list filters (reference, production number, machine, processo, tool reference, submitted range) above the table; filter submission triggers the **backend** query (never client-side filtering of a partial set); explicit empty/lookup-failed/denied states distinct |
| **R2 — Pending table** | `DenseDataTable` of reviewable Pesos (§15.1 rows): submitted at/by, reference/production/machine (traversal), tool reference, processo, row count. Single click selects; **double click opens the exact record** (R3); no per-row action grid |
| **R3 — Review sheet** | the shared read-only Peso presentation (§15.3/§16): production strip (traversal facts), CM/pending context, measurements + frozen results (≤ 2 dp), frozen density ("Densidade usada"), submit attribution, decision trail summary; comparison region **only when the shared read model supplies the persisted relation** (§17.2); warnings region **only when the shared read model supplies warnings** (§15.3) |
| **R4 — Decision region** | `DecisionBar`: `Aprovar` (primary) / `Rejeitar` (danger, opens the reason input) / `Reabrir` (reopen with reason) — **outside the table, against the opened/selected record**; disabled reasons visible; duplicate invocation prevented; `Enviar para produção` affordance on approved Pesos per §23 (explicit confirmation; currently unavailable-with-reason) |
| **R5 — Decision trail** | `AuditTrail` of the opened record (§15.4): labeled events with backend actor/timestamp/version/reason; empty trail = explicit "Sem decisões" |

### 21.2 Histórico — `Pages/Controlo/Approve/Historico` (regions, exact)

| Region | Contract |
|---|---|
| **H1 — Filters** | the §15.2 filter set (review state, production/tool facts, submitted/decided ranges, decision actor/outcome) |
| **H2 — History table** | `DenseDataTable` of §15.2 rows: single click selects; double click opens detail; status via `RecordStatus`; no per-row action grid |
| **H3 — Detail** | the review sheet of the exact record (R3 content) + full decision trail (R5) + outside-table actions per availability |

### 21.3 Page-owned assets

New `dmo-controlo-approve.css` (no structural media/container/support rules) and — only if
genuinely required — `dmo-controlo-approve.js` reusing `window.dmoFocus`, with the page-owned
null-safe binding and `renderConflict` reload-recovery patterns of the accepted D2 correction
(conflict presentation on `stale-version`; observed version refreshed only on success; no
auto-retry/auto-merge). No shared component or shared CSS/JS file is modified.

---

## 22. Negative-scope protections

| # | Protected fact | Proof discipline |
|---|---|---|
| BND-B1 | **No Definições** — no settings surface, route, type or menu entry under Approve (repairers, machine assignments, PDF directory, email lists, templates, glass-density, water-density) | static scan + route scan + access tests (A3/B1) |
| BND-B2 | **No P2-T08 leakage** — no PDF generation, file creation, directory writing, document regeneration, document identity, email sending/routing/transport; no `Infrastructure/Files`/`Pdf`; no SMTP; no placeholder parsing | static + route scan (B2) |
| BND-B3 | **No P2-T07 leakage** — no movement records, BQ aggregate, repairer resolution, Boquilhas history | static scan (B3) |
| BND-B4 | **No availability registration** — `ModuleRegistrations.CurrentBuildAvailable` stays `[]`; `DestinationRouteRegistrations` untouchted; no navigation entry | static + tests (B4) |
| BND-B5 | **No HISTÓRICO GLOBAL** (`historia`) route/entry/registration | static scan (B5) |
| BND-B6 | **No comparison/Folha invention** — no `previous_peso_id` column/table, no per-CM decision table/route, no `controlo_sheet_id` table/column/route; no replacement sheet | schema scan + route scan (B6) |
| BND-B7 | **No second calculation engine** — no water/glass density engine, no divisor/density constant copy, no formula; frozen results consumed as facts | static scan (B7) |
| BND-B8 | **No generic lifecycle/revision infrastructure** — no queue table, no revision id, no generic audit engine, no state machine framework | static scan (B8) |
| BND-B9 | **No identity duplication** — no new `peso_id`/`cm_id`/`tool_id`/`jobon_id` minting; no approval-copy Peso; no `production_id`/`job_on_revision_id`/`approval_peso_id` | static + DB (I4/I5) |
| BND-B10 | **Protected files** — migrations 001–005 (incl. Designers), `DmoDbContext.cs`, P2-T05 pages/endpoints/services/repositories/entities/configurations/`dmo-controlo.css`/`dmo-controlo.js`, all shared/frozen artifacts byte-identical | regression assertions (MG1) |

---

## 23. Enviar para produção (authority trace + gap)

### 23.1 Authority trace (exact)

| Authority | Statement | Consequence |
|---|---|---|
| `dmo-master/modules/CONTROLO.md` §17 | "Enviar para produção" is an **explicit, confirmed** action **available from an approved Peso record**; **never automatic and never implied by approval**; a missing/invalid recipient configuration **blocks only the send** — the approval and the recorded document remain valid and documented; recipients were resolved by Machine/Line groups configured in `Admin > Definições > Controlo` | the action **exists**; human-confirmed; precondition = approved Peso; the send can be blocked without invalidating the approval |
| `dmo-master/global/ACCESS_MODEL.md` §9 | "Controlo Approve exposes … the explicit confirmed send-to-production action" | the action surface is part of the Approve module scope |
| `…DELTA.md` §1/§2/§8/§9 (+ P2-T05 §1.1 Q-ROUTE) | recipient **configuration** location superseded: named lists in `Controlo_Create → Definições`; **no exact routing rule is fixed** (P2-T08's contract owns it) | the recipient side is not P2-T06's |
| `…DELTA.md` §14 (P2-T08 row) | P2-T08 affected pages include: document availability surfaces; open/regenerate actions; **sending flow (list + template + attachment + preview + send)** | the **send workflow** is P2-T08 |
| `dmo-beta-master/modules/CONTROLO_APPROVE.md` "Included in Beta" | "explicit confirmed `Enviar para produção` only where allowed by the published backend contract" | Beta conditions inclusion on a published backend contract |
| Handoff/master plan §7 P2-T06 | same conditional wording | same |
| `implementation/BETA_INTEGRATION_SEAMS.md` Workstream D | D needs a "confirmed send-to-production action **where the owning contract allows it**" | the owning (send) contract does not exist (P2-T08 NOT AUTHORIZED) |
| This task's P2-T08 boundary | "P2-T06 may expose only the decision state/facts that P2-T08 later consumes"; "ensure it does NOT implement P2-T08 PDF/email sending prematurely" | no mechanics, no send state |

### 23.2 Determination (pinned default — Q-SEND, ACCEPT DEFAULT)

1. **It is not genuinely executable in P2-T06.** The send workflow (PDF generation, recipient
   resolution, transport, preview) is P2-T08's contract (`…DELTA.md` §14; Q-ROUTE); P2-T08 is
   NOT AUTHORIZED; no send-state vocabulary exists in any repository authority; the Beta
   conditions the action on a published backend contract that does not exist.
2. **P2-T06's contribution is the initiation affordance only** — required by the master (the
   action is a confirmed action on an approved Peso, and ACCESS_MODEL §9 places the surface in
   Approve): on an **approved** Peso, the review's decision region offers **"Enviar para
   produção"** as an explicit, **confirmed** (non-automatic) affordance. Its **execution
   semantics are defined by P2-T08's contract** (the owning contract); P2-T06 performs **no**
   send mechanic and persists **no** send state/fact (§23.4).
3. **Preconditions (fixed):** the Peso is `aprovado`; the action is never implied by approval;
   confirmation is explicit (the accepted DecisionBar confirmation pattern; duplicate invocation
   prevented).
4. **Interim behavior (until the P2-T08 contract exists):** the affordance renders in the
   **unavailable-with-reason** state (associated reason text: the sending workflow is defined by
   the documents contract, P2-T08 — not yet available), exactly like every other
   not-yet-possible action; no fake success, no dead-click, no client-side send, no state.
5. **It does not implement P2-T08 prematurely** (no PDF/file/email/directory/routing code —
   BND-B2), and it does not block this contract: everything else is independent.

### 23.3 Recorded gap (authority question Q-SEND)

The master fixes presence/confirmation/precondition; the Beta conditions inclusion on a published
backend contract; the delta places the executable flow in P2-T08; **no authority fixes what
state (if any) the confirmed action changes on the record before P2-T08 mechanics exist, nor
whether the affordance must be live (vs unavailable-with-reason) in an interim build.** Pinned
default (above): initiation-only affordance, unavailable-with-reason until P2-T08, zero P2-T06
send persistence. The Architect may alternatively disposition Q-SEND as `REQUIRES OWNER
DECISION` — the default is contractually safe either way (it adds no invented behavior and
removes none that authority mandates).

### 23.4 No send persistence (exact)

No `sent_at`, no `enviado` status, no send-request table, no document reference on the Peso or in
P2-T06 tables. The decision state P2-T08 later consumes is exactly what this contract produces:
`pesos.status = 'aprovado'` + the decision trail (who/when/version) + the frozen Peso facts —
the authoritative-data rule's "approval data … consumers of the same authoritative record".

---

## 24. Downstream seams (P2-T07 / P2-T08 / P2-T10)

| Seam | Who consumes | What P2-T06 leaves | What the consumer must do |
|---|---|---|---|
| Approved/decided state + decision trail | P2-T08 (PDF/send), P2-T10 (integration), Job On read projections | `pesos.status` transitions + `peso_review_decisions` (who/when/version/reason) + frozen Peso facts | P2-T08: generate/send from the same `peso_id`; never a second dataset; P2-T10: register `controlo-approve` availability (step P2-T10c) |
| Send-to-production initiation | P2-T08 (owning contract) | the initiation affordance on approved Pesos (§23) | P2-T08: define executable send semantics, routing (Q-ROUTE), templates (Q-PLACE), and any immutable-output metadata it genuinely needs |
| Comparação per-CM decisions | P2-T05 handoff remainder (Comparação contract) | closed vocabulary `Manter`/`Colocar de parte`, semantics (§17.1), no carrier | the remainder contract persists the relation + comparison record; then the D-side review composes it via the shared read model |
| Folha decision | P2-T05 handoff remainder (Folha contract) | closed vocabulary/states/ownership (§18.1), no carrier | the remainder contract persists `controlo_sheet_id` + submission; then the D-side decision/reopen rules (§18.1) bind |
| Warnings | future read-model extension contract | no warning region today; never-decide invariant (§15.3) | extend the shared read model; the review surface renders facts only |
| Boquilhas | P2-T07 | nothing | P2-T07 consumes repairer register from Definições; unrelated to P2-T06 |
| Availability/navigation | P2-T10 | `CurrentBuildAvailable` stays `[]` | register `controlo-approve` on the shared `controlo` destination when real |

No seam authorizes implementation: every consumer remains NOT AUTHORIZED until its own gate.

---

## 25. Migration contract

### 25.1 One migration, one owner

P2-T06 owns **exactly one new migration** (the sixth overall):

```text
src/DMO.Infrastructure/Migrations/<UTC timestamp>_ControloApproveDomain.cs   (+ .Designer.cs)
```

- EF-generated (`dotnet ef migrations add ControloApproveDomain` through the accepted
  `DesignTimeDmoDbContextFactory`), accepted shape; `DmoDbContextModelSnapshot.cs` extended by EF
  (the documented, EF-owned extension — P2-T04 §16.1/P2-T05 §25.1 precedent);
- migrations 001–005 (incl. Designers) **never edited** (regression-asserted byte identity);
- `DmoDbContext.cs` **deliberately NOT modified** (`ApplyConfigurationsFromAssembly` + `Set<T>`).

### 25.2 Expected schema delta (exact)

Created: **1 table** — `peso_review_decisions` — with exactly the columns, defaults, CHECKs
(`decision`, `prior_status`, reason-required, version-at-decision) and the 2 FKs
(both `RESTRICT`) of §6/§7.

Indexes: **1 additive index on the existing `pesos` table** — `IX_pesos_reviewable (status,
submitted_at DESC)` (§7.3) — an index-only additive statement; **no column change** to `pesos`
or any other table.

Not created: any other table, seed/reference row, trigger, function, view, extension, sequence,
enum type, RLS policy, or any column not listed in §6.

### 25.3 Supersession record (exact extent)

The P2-T05-closed statement "8-table P2-T05 schema / no ninth table" was already superseded to
the exact extent of the correction's `glass_density_settings` (fifth → 9 tables). This contract
supersedes it **to the exact further extent of** `peso_review_decisions` (decision/audit trail,
explicitly assigned to P2-T06's own persistence by the Identity Rule) and the single deferred
query index P2-T05 §17.5 explicitly reserved for this slice. Nothing else changes:
`pesos`/`peso_measurement_rows`/the five Definições tables/`glass_density_settings` keep every
column; no Comparação/Pegamentos/Folha/Resumo/send/document table is created.

### 25.4 Safety rules / rollback

1. Purely additive (`CREATE TABLE` + `CREATE INDEX`); no `DROP`/`TRUNCATE`/type change/rename/
   `EnsureDeleted`; applying twice is a no-op.
2. `Down` drops the index and the table, in exact inverse order
   (`DROP INDEX IX_pesos_reviewable` → `DROP TABLE peso_review_decisions`); re-apply idempotent.
3. PostgreSQL/Supabase-compatible by construction (portable features only; verified against the
   **disposable** PostgreSQL test database in implementation, never a live Supabase change —
   an operator's deployment step).

---

## 26. Test-to-acceptance matrix

Test classes follow the accepted naming (`<ID>_<PascalCaseClauses>`, XML summary naming the AC).
Classes: U = unit, I = integration/host (HTTP + gates + composition), DB = disposable PostgreSQL,
S = static/architecture scan, R = rendered. The matrix is bidirectional-complete against §30
(CRITERION ↔ PROOF). Row ids are unique; every row maps only to §30 keys.

#### IDENTITY

| # | Class | Test | Proves |
|---|---|---|---|
| I1 | DB | approve on a reviewable Peso → status `'aprovado'` on the SAME `peso_id`; exactly 0 new `pesos` rows; the read model re-reads the same id with the same frozen facts | AC-I1, AC-R1 |
| I2 | DB | reject → `'nao_aprovado'` on the SAME `peso_id`; no second record | AC-I2 |
| I3 | DB | reopen → SAME `peso_id`; status `'pendente'`; `submitted_at`/`submitted_by_user_id` NULL; `pesos` row count unchanged | AC-I3, AC-O1 |
| I4 | S | static scan of P2-T06 code/schema/contracts: no `production_id`, no `job_on_revision_id`, no `approval_peso_id`, no review-copy type/table/column, no client-minted id | AC-I4, AC-B9 |
| I5 | S | no P2-T06 code creates `cm_id`/`tool_id`/`jobon_id`; anchors appear only through consumed read-model projections | AC-I5 |
| I6 | S | exactly one new identity type exists in the P2-T06 surface: the decision record id (`peso_review_decision_id`) — anything else fails the scan | AC-I6 |
| I7 | DB | a reviewable Peso with pending `tool_id` anchor (Job On por associar) can be approved/rejected normally — the pending condition never blocks review | AC-I5, AC-D1 |

#### ACCESS

| # | Class | Test | Proves |
|---|---|---|---|
| A1 | I | a `controlo-approve`-granted caller reaches pages 1–2 and endpoints 3–9; a `controlo-create`-only caller is denied every one of them (direct-route server-side denial) | AC-A2, AC-A4 |
| A2 | I | a `controlo-approve`-only caller is denied every P2-T05 route incl. the Definições pages/routes 12–17 of P2-T05 (server-side; never by hiding) | AC-A3 |
| A3 | I | both grants together: one visible `controlo` destination, route-level gates still fully separate (no grant merging on the shared destination) | AC-A2, AC-A4 |
| A4 | I | ADMIN is denied every P2-T06 route (no super-user) | AC-A4 |
| A5 | S | every P2-T06 route/action carries exactly `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloApprove)`; page `[Authorize]` constants pinned in `ControloApprovePolicyNames` and asserted equal to the canonical projection; no second policy anywhere | AC-A1 |
| A6 | I | denial is never an empty list/blank surface: a denied caller receives the documented 403 presentation on every P2-T06 route | AC-A4 |

#### READ-ONLY FACTS

| # | Class | Test | Proves |
|---|---|---|---|
| R1 | DB | water temperature, volumes, SAP references, frozen `glass_density_g_cm3` and every per-row frozen result are byte-equal before/after approve, reject and reopen | AC-R1, AC-R2 |
| R2 | S | P2-T06 write paths touch only `pesos.status`/`version`/`updated_at`/`submitted_at`/`submitted_by_user_id` and the decision table — scan proves no other `pesos` column and no `peso_measurement_rows` write | AC-R1, AC-O5 |
| R3 | DB | after changing a `glass_density_settings` value, a reviewed (and a reviewable) Peso's read model is unchanged (frozen density + rows); only a NEW Peso resolves the new value | AC-R3 |
| R4 | I | the approval review read returns exactly the persisted facts — identical values to the Create-side read (same shared service), no recalculation observable in the response | AC-R4, AC-RD1 |
| R5 | DB | reopen → re-submit (Create routes) → the re-submitted record still carries the ORIGINAL frozen density and frozen per-row results (no re-resolution, no recompute) | AC-R5 |

#### DECISIONS

| # | Class | Test | Proves |
|---|---|---|---|
| D1 | U | the approve/reject/reopen commands contain only identity/version/reason — no Peso fact, no warning, no calculation output can be supplied (closed command shape) | AC-D1, AC-D2 |
| D2 | S | static scan: no code path derives a decision from results/warnings/config (no auto-approve/auto-reject symbol, no threshold logic in P2-T06) | AC-D1, AC-D2 |
| D3 | S | static scan of P2-T06 vocabulary: the decision trail uses exactly `aprovado`/`nao_aprovado`/`reaberto`; the per-CM vocabulary is exactly `Manter`/`Colocar de parte` (documented constants); no invented decision word/status exists | AC-D3 |
| D4 | DB | `decided_by_user_id` and `decided_at` are backend-authored — DB rows equal the authenticated backend user/clock; the carriers accept no actor/time | AC-D4 |
| D5 | U | approve/reject/reopen on a never-submitted draft → `not-reviewable`; no event, no status change | AC-D1, AC-K3 |
| D6 | S | no per-CM decision persistence exists in P2-T06 (no table/route/type); the vocabulary is fixed by contract only (§17) — the carrier seam is proven absent, nothing invented | AC-D3, AC-CP4 |

#### REJECT

| # | Class | Test | Proves |
|---|---|---|---|
| J1 | DB | reject with reason → same `peso_id` `'nao_aprovado'`; decision row `('nao_aprovado', actor, now, reason, prior 'pendente', observed version)`; version incremented once | AC-J1 |
| J2 | U/DB | reject with blank/missing reason → 400 `validation-failed` `REJECT_REASON_REQUIRED`; nothing written (and the DB CHECK backstop rejects a direct insert without violation mapping to 500) | AC-J2 |
| J3 | DB | forced mid-transaction failure during reject → zero partial state (status unchanged, no decision row, no version bump) | AC-J3, AC-K2 |

#### REOPEN

| # | Class | Test | Proves |
|---|---|---|---|
| O1 | DB | reopen from `aprovado` (and separately from `nao_aprovado`, and from submitted-`pendente`) → SAME `peso_id`, `'pendente'`, `submitted_at` NULL; the accepted Create edit (route 7) and submit (route 9) routes then work again and the re-submitted record is reviewable | AC-O1 |
| O2 | DB | the trail after reopen contains the prior approve/reject event intact (actor/time/version/reason) plus the reopen event; no historical erase anywhere in the trail | AC-O2 |
| O3 | DB | reopen with blank/missing reason → 400 `REOPEN_REASON_REQUIRED`, nothing written; valid reopen stores actor/time/reason backend facts | AC-O3 |
| O4 | DB | a reopened previously-`aprovado` record is never `aprovado` again without a NEW approve: resubmit → reviewable `pendente` → approve → `aprovado` (two distinct decision events); no path returns the old approval | AC-O4 |
| O5 | S | no P2-T06 route/primitive edits measurement rows or any Peso input — editing after reopen exists only through Create's accepted edit route (route scan + reflection) | AC-O5 |

#### COMPARAÇÃO

| # | Class | Test | Proves |
|---|---|---|---|
| CP1 | S | no heuristic previous-Peso logic exists in P2-T06 (no latest/date/reference/machine selection, no reconstruction, no default pairing) | AC-CP1 |
| CP2 | DB/S | P2-T06's migration creates no `previous_peso_id` column/table and no comparison relation; decision routes write no pairing (schema scan + update audit) | AC-CP3, AC-CP4 |
| CP3 | I | the review sheet composes comparison context **only** from the shared read model when it supplies it; with the current schema the sheet carries no comparison region and fabricates none | AC-CP2, AC-CP4 |
| CP4 | U | the exact-relation composition contract is pinned: given a future read model carrying `previous_peso_id`, the composition exposes exactly that id and enough current/previous context — no fallback, no substitution, no mutation (unit test over the composition function with a supplied carrier) | AC-CP1, AC-CP2, AC-CP4 |

#### PENDING / REVIEW LIST

| # | Class | Test | Proves |
|---|---|---|---|
| PL1 | DB | the pending list returns exactly the reviewable set (`submitted_at IS NOT NULL AND status='pendente'`): drafts, decided and reopened records are excluded | AC-AP1 |
| PL2 | I | every pending filter (reference/production/machine/processo/tool reference/submitted range) is applied server-side; unknown/ill-formed filter values → 400 `FILTER_INVALID` (never a silent full list); empty result = explicit empty | AC-AP1, AC-AP3 |
| PL3 | I | opening a pending item (`GET /controlo/approve/pesos/{id}`) loads the EXACT submitted `peso_id` (same id, same facts as Create reported) | AC-AP2 |
| PL4 | R | the Aprovar page renders the pending table with selection/open semantics: single click selects, double click opens the exact record; no per-row action grid; actions live outside the table | AC-AP2, AC-H3 |

#### HISTORY

| # | Class | Test | Proves |
|---|---|---|---|
| H1 | DB | the decision trail returns every event (approve/reject/reopen cycles) ordered by time with actor/time/version/reason/prior status; nothing missing after multiple cycles on one `peso_id` | AC-H1 |
| H2 | I | Histórico filters (review state, production/tool facts, submitted/decided ranges, decision actor/outcome) are backend-applied; invalid input → typed refusal, not a silent full list | AC-H2 |
| H3 | R | the Histórico table: single click selects, double click opens the exact Peso detail; actions outside the table; no per-row action button grid | AC-H3 |
| H4 | S | no `historia` route/entry/registration exists; the local Histórico lives only inside the Approve surface (HISTÓRICO GLOBAL conflation absent) | AC-H4 |
| H5 | I | history rows carry the exact `peso_id` + status/decision/actor/time facts; opening a row loads the exact record including its trail | AC-H5 |

#### CONCURRENCY

| # | Class | Test | Proves |
|---|---|---|---|
| K1 | DB | approve/reject/reopen with a stale `expectedVersion` → 409 `stale-version`; nothing written (status, version, trail unchanged) | AC-K1 |
| K2 | DB | forced mid-transaction failure in approve and reopen → zero partial state (transition and event both absent) | AC-K2 |
| K3 | DB | save-time race (second connection bumps `pesos.version` mid-save) → 409 `stale-version` via the `SaveAsync` mapping; no silent overwrite of the newer state | AC-K1, AC-K3 |
| K4 | DB | double decision: approve after approve, approve after reject, reject after approve → 409 `already-decided`; exactly one decision event exists | AC-D1, AC-K3 |
| K5 | R | on `stale-version` the surface enters the accepted conflict presentation with the explicit "Recarregar estado atual" recovery; no auto-retry, no auto-merge; the observed version refreshes only on success (D2 pattern) | AC-K4 |

#### RENDERER / READ-MODEL REUSE

| # | Class | Test | Proves |
|---|---|---|---|
| RD1 | S | the review-sheet type embeds the EXACT `PesoSheetReadModel` type from `DMO.Application.ControloCreate` (type identity assertion); no duplicate/copied Peso facts model exists in `ControloApprove` | AC-RD1 |
| RD2 | R | renderer parity: the same fixture rendered by Create's read-only (submitted) view and Approve's review view shows the same operational facts with the same labels, order and ≤ 2-dp normalization (mechanical comparison over `data-dmo-*` hooks); no field hidden in review that Create shows | AC-RD2 |
| RD3 | S | the wrapper adds only approval-only facts (decisions/availability/disabled reasons); no member of `PesoSheetReadModel` is altered, hidden or redefined by P2-T06 types | AC-RD3 |

#### MIGRATION / SCHEMA

| # | Class | Test | Proves |
|---|---|---|---|
| MG1 | DB | migration 006 applies over 001–005; exactly one new table (`peso_review_decisions`) and exactly one additive index on `pesos`; migrations 001–005 and `DmoDbContext.cs` byte-identical; `Down` drops index + table; re-apply idempotent | AC-MG1 |
| MG2 | DB | the decision table's CHECKs/FKs/columns match §6/§7 exactly (constraint names, `confdeltype='r'`, no cascade); reason CHECK rejects an `aprovado` row with a reason and a `reaberto` row without one (23514 → typed token, never 500) | AC-MG2 |
| MG3 | DB | decision rows are append-only: no UPDATE/DELETE primitive exists (route + repository reflection); `pesos.version` increments exactly once per decision transition | AC-MG3 |

#### FIXED DESKTOP

| # | Class | Test | Proves |
|---|---|---|---|
| L1 | S | static scan of P2-T06-owned assets (`dmo-controlo-approve.css`, `.js`, Approve pages): no `@media`/`@container`/`@supports` structural rule, no width listener, no table→card conversion, no required-column hiding, no action relocation | AC-L1 |
| L2 | R | Aprovar and Histórico render at the canonical 1366 × 768 validation viewport with region-stable composition; a larger desktop preserves the composition (whitespace/limited non-structural expansion only) | AC-L1, AC-L2 |
| L3 | S/R | no shared shell/header/navigation file is modified; the pages render inside the existing shell unchanged | AC-L3 |

#### BOUNDARIES

| # | Class | Test | Proves |
|---|---|---|---|
| B1 | S | no Definições/settings surface: no repairer/machine-assignment/PDF-directory/email-list/email-template/glass-density/water-density code, route or type under Approve | AC-B1 |
| B2 | S | no PDF/file/email code: no `Infrastructure/Files`/`Pdf` adapter, no SMTP/email, no directory write, no document identity, no placeholder parsing, no send route | AC-B2 |
| B3 | S | no Boquilhas code: no movement/aggregate/repairer-resolution/Boquilhas-history type or route | AC-B3 |
| B4 | S | `ModuleRegistrations.CurrentBuildAvailable` stays `[]`; `DestinationRouteRegistrations` empty; no navigation/availability registration | AC-B4 |
| B5 | S | no `historia` (HISTÓRICO GLOBAL) route/entry/registration | AC-B5 |
| B6 | S | no `previous_peso_id`/`controlo_sheet_id`/per-CM/Folha/send table, column or route exists in P2-T06 code/schema | AC-B6 |
| B7 | S | no second calculation engine: no water/glass density constant copy, no divisor table, no formula, no config re-read in P2-T06 | AC-B7 |
| B8 | S | no generic lifecycle/revision/queue infrastructure (no `revision_id`, no queue table, no state-machine engine, no generic audit engine) | AC-B8 |

**Completeness (mechanical audit):** the matrix is bidirectional:

- **Acceptance criteria: 61** — AC-I1…AC-I6 (6), AC-A1…AC-A4 (4), AC-R1…AC-R5 (5),
  AC-D1…AC-D4 (4), AC-J1…AC-J3 (3), AC-O1…AC-O5 (5), AC-CP1…AC-CP4 (4), AC-AP1…AC-AP3 (3),
  AC-H1…AC-H5 (5), AC-K1…AC-K4 (4), AC-RD1…AC-RD3 (3), AC-MG1…AC-MG3 (3), AC-L1…AC-L3 (3),
  AC-B1…AC-B9 (9) = 6+4+5+4+3+5+4+3+5+4+3+3+3+9 = **61**; each criterion exists exactly once in
  §30 (no aliases).
- **Test rows: 67** — IDENTITY 7, ACCESS 6, READ-ONLY 5, DECISIONS 6, REJECT 3, REOPEN 5,
  COMPARAÇÃO 4, PENDING 4, HISTORY 5, CONCURRENCY 5, RENDERER 3, MIGRATION 3, DESKTOP 3,
  BOUNDARIES 8 = **67**; all row ids unique.
- **missing = 0** (every one of the 61 criteria maps to ≥ 1 row — verified in the Proves column
  above), **dangling = 0** (every row maps only to keys that exist in §30), **orphan = 0** (no
  row without a criterion; B1–B8/BND rows are negative-boundary scans of real absence, not tests
  of other workstreams' behavior).
- The row↔AC key is re-verified mechanically in the eventual implementation response (P2-T04
  §20.12 discipline).

---

## 27. Authority questions

### 27.1 BLOCKING — NONE

There are **no BLOCKING authority questions**. Every open point has a pinned default consistent
with the closed P2-T05 state and the Identity Rule (see §27.2; summary §28).

### 27.2 NON-BLOCKING / OWNER-LEVEL (each with a pinned default already reflected in this
contract)

| # | Question | Authority gap | Pinned default | Impact | Class |
|---|---|---|---|---|---|
| Q-SEND | `Enviar para produção` — executable semantics, interim presence, any state change | master fixes existence/confirmation/precondition (approved Peso) and ACCESS_MODEL §9 places the surface in Approve; the Beta conditions inclusion on a published backend contract that does not exist; the delta places the sending flow in P2-T08; no send-state vocabulary exists anywhere | **initiation-only affordance on approved Pesos** (explicit confirmation, never automatic), **unavailable-with-reason until the P2-T08 contract exists**, **zero send persistence in P2-T06**; the owning contract (P2-T08) defines execution (§23) | no schema/route beyond §13; the affordance region only | ACCEPT DEFAULT (Architect may re-route to OWNER) |
| Q-REOPEN | Reopen target status and the "material edit → `nao_aprovado`" letter of the Identity Rule | Identity Rule requires (c) draft-editable handoff via the accepted submit route and (d) never `aprovado` before re-submission, without naming the interim status; the closed P2-T05 submit route never writes `status` | **reopen → `status='pendente'`, clear `submitted_at`/`submitted_by`**; the prior approval is superseded by the draft; `aprovado` returns only via a NEW approval; "material edit" = any change to Peso inputs or computed results (§19.2) — the only mechanics consistent with the closed routes and the reviewable predicate | reopen route behavior | ACCEPT DEFAULT |
| Q-NOTE | Reject/reopen reason mandatory? | authority: "reject with note where required", "reason where required"; no Peso-specific rule | **non-blank reason required** for reject and for reopen; none for approve (validator + DB CHECK) | command carriers + CHECK | ACCEPT DEFAULT |
| Q-FOLHA | Folha decision surface before `controlo_sheet_id` exists | master §15 fixes Folha states/decision/reopen ownership; Folha record + submission are the P2-T05 handoff remainder (NOT AUTHORIZED; no table) | **vocabulary/rules fixed (§18.1); no Folha routes/persistence now**; enabled by the Folha remainder contract (own gate) | none today | ACCEPT DEFAULT |
| Q-PERCM | Per-CM `Manter`/`Colocar de parte` persistence before Comparação exists | master §10/§15 fix vocabulary and semantics; Comparação (incl. `previous_peso_id`) is NOT persisted | **vocabulary/semantics fixed (§17.1); no carrier/table/route invented**; persistence enabled by the Comparação remainder contract | none today | ACCEPT DEFAULT |
| Q-COMP | Comparação read when no relation is persisted | the authoring task assumed "P2-T05 persists `current_peso_id → previous_peso_id`"; the closed P2-T05 contract/closure records Q-SCOPE: NOT persisted (BND7 verified) | **no relation to read today; exact-relation read contract pinned for the future carrier (§17.2); no heuristic, no latest, no mutation** | review region absent today | ACCEPT DEFAULT |
| Q-RENDER | Renderer reuse mechanics | the shipped Peso-sheet renderer is page-owned inside the CLOSED `Create.cshtml`; no shared partial exists; extracting one modifies closed P2-T05 code | **same read-model type + same presentation discipline + renderer-parity contract test (§16)**; the shared-partial extraction is the optional cross-stream alternative (separate acceptance) | review page | ACCEPT DEFAULT |
| Q-WARN | Warnings region | the shipped read model carries no warning facts for Peso (P2-T05 scope); tolerances/nominals belong to Pegamentos/Comparação (not persisted) | **render warnings only when the shared read model supplies them; never decide** (§15.3) | review region | ACCEPT DEFAULT |
| Q-PAGE | History/pending paging bounds and ordering | DenseDataTable pages consumer-side; no authority fixes bounds | 1-based paging, `pageSize` 1–100, deterministic ordering (§15.1/§15.2) | queries | ACCEPT DEFAULT |
| Q-DECISIONS | Decision-event token for reopen | vocabulary words in authority are statuses + the verb `reabrir` | event tokens `aprovado`/`nao_aprovado`/`reaberto` (labels Aprovado/Não aprovado/Reaberto) | DECISION CHECK | ACCEPT DEFAULT |

---

## 28. Authority questions — summary

**BLOCKING: 0.** **REQUIRES OWNER DECISION: 0** (Q-SEND is recorded as ACCEPT DEFAULT with the
explicit note that the Architect may disposition it to `REQUIRES OWNER DECISION` — §23.3; the
default adds no invented behavior). **NON-BLOCKING: 10** (Q-SEND, Q-REOPEN, Q-NOTE, Q-FOLHA,
Q-PERCM, Q-COMP, Q-RENDER, Q-WARN, Q-PAGE, Q-DECISIONS), each with a pinned default already
reflected in the schema/interfaces/routes above; none blocks the PLAN REVIEW gate. If the
Architect rejects a pinned default, the contract returns `CORRECTION REQUIRED` for that item.

---

## 29. Explicit downstream seams

| Seam | Who consumes | What P2-T06 leaves | What the consumer must do |
|---|---|---|---|
| Same-`peso_id` reviewable/decided handoff | P2-T08 (PDF), P2-T10 | status transitions + decision trail + frozen facts | same `peso_id` generation/sending; never a second dataset (Identity Rule; authoritative data rule) |
| `Enviar para produção` initialization | P2-T08 | initiation affordance on approved Pesos (§23) | owning send contract defines execution/routing/templates/transport and any genuinely-needed output metadata |
| Comparação relation + per-CM decisions | P2-T05 handoff remainder | closed vocabulary + exact-relation read contract (§17) | persist the comparison record/relation; then the review composes |
| Folha decision | P2-T05 handoff remainder | closed vocabulary/states/ownership (§18) | persist `controlo_sheet_id` + submission; then D-side decision rules bind |
| Warnings carriers | future read-model extension | never-decide invariant; no region today | extend the shared read model additively |
| Availability/navigation | P2-T10 | `[]` unchanged | register `controlo-approve` (step P2-T10c) when the surface is real |

No seam authorizes implementation: every consumer remains NOT AUTHORIZED until its own gate.

---

## 30. Implementation acceptance criteria

P2-T06 is acceptable only when every criterion below is satisfied and proven by §26.4.

### Identity (AC-I1 … AC-I6)

| # | Criterion |
|---|---|
| AC-I1 | Approve mutates lifecycle of the SAME `peso_id`; no Peso record is created, copied or replaced. |
| AC-I2 | Reject mutates lifecycle of the SAME `peso_id`; no second record. |
| AC-I3 | Reopen preserves the SAME `peso_id`; no clone, no replacement identity. |
| AC-I4 | No approval-copy Peso, no `production_id`, no `job_on_revision_id`, no `approval_peso_id`, no revision id exists anywhere in P2-T06. |
| AC-I5 | P2-T06 creates no `cm_id`/`tool_id`/`jobon_id`; anchors are consumed read-only through the shared read model; the pending `tool_id` anchor never blocks review. |
| AC-I6 | The only new identity is the decision record id (`peso_review_decision_id`). |

### Access (AC-A1 … AC-A4)

| # | Criterion |
|---|---|
| AC-A1 | Every P2-T06 route/action carries exactly `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloApprove)`. |
| AC-A2 | `controlo-create` does not grant any Approve route; a Create-only caller is denied server-side. |
| AC-A3 | `controlo-approve` does not grant Create: an Approve-only caller cannot reach any P2-T05 route incl. `Controlo_Create → Definições` (server-side). |
| AC-A4 | The shared `controlo` destination never merges grants; direct-route denial is server-side; ADMIN gains no operational access; denial is never a blank/empty presentation. |

### Read-only facts (AC-R1 … AC-R5)

| # | Criterion |
|---|---|
| AC-R1 | Approval actions modify only lifecycle facts (status/version/trail); submitted measurement/calculation facts are never modified by an approval action. |
| AC-R2 | Frozen water/glass calculation facts (temperature inputs, density used, per-row capacity/glass weight) remain unchanged across approve/reject/reopen. |
| AC-R3 | Settings changes (e.g. glass density per processo) never rewrite a reviewed/reviewable Peso; only new Pesos resolve the new value. |
| AC-R4 | Review renders the exact submitted facts from the shared read model — no recalculation, no reinterpretation. |
| AC-R5 | Reopen does not recalculate or re-resolve density; frozen facts remain authoritative through reopen and re-submission. |

### Decisions (AC-D1 … AC-D4)

| # | Criterion |
|---|---|
| AC-D1 | A human decision is required: approve/reject are explicit actions by an authenticated human; no automatic transition exists on any route. |
| AC-D2 | Warnings/calculation results never decide: decision commands carry no warning/result input and no code path derives a decision from them. |
| AC-D3 | The per-CM decision vocabulary is exactly `Manter`/`Colocar de parte` (explicit human decisions; never implied by the general approval); no additional decision word exists. |
| AC-D4 | Actor/time are backend-authored facts (user/clock); no client-supplied actor/time exists. |

### Reject (AC-J1 … AC-J3)

| # | Criterion |
|---|---|
| AC-J1 | Reject transitions the same `peso_id` to `nao_aprovado` with backend attribution and a recorded decision event (outcome, actor, time, version-at-decision, prior status, reason). |
| AC-J2 | Reject requires a non-blank reason (`REJECT_REASON_REQUIRED`); refusal writes nothing. |
| AC-J3 | A failed rejection is atomic — no partial mutation (status/version/event all-or-nothing). |

### Reopen (AC-O1 … AC-O5)

| # | Criterion |
|---|---|
| AC-O1 | Reopen keeps the same `peso_id` and restores the draft-editable handoff (`pendente`, `submitted_at`/`submitted_by` cleared) so the accepted Create edit and submit routes work again. |
| AC-O2 | Reopen preserves the prior decision, prior actor, prior timestamp and reason in the immutable decision trail — no historical erase. |
| AC-O3 | Reopen records actor/time/reason as backend facts; reopen requires a non-blank reason (`REOPEN_REASON_REQUIRED`). |
| AC-O4 | A reopened previously-approved Peso is never `aprovado` before re-submission; it becomes `aprovado` again only through a NEW approval decision. |
| AC-O5 | P2-T06 is never an editor of measurement facts: no P2-T06 route/primitive modifies Peso inputs or rows; editing after reopen is exclusively Create's accepted edit route. |

### Comparação (AC-CP1 … AC-CP4)

| # | Criterion |
|---|---|
| AC-CP1 | Review uses the exact persisted `previous_peso_id` relation when the carrier exists; no "latest" heuristic, no reconstruction, no substitution. |
| AC-CP2 | Enough current/previous context is presented from the persisted relation for human understanding — never from inference. |
| AC-CP3 | Decision actions never mutate the pairing (no write to any comparison relation). |
| AC-CP4 | Carrier absence today (Comparação remainder NOT AUTHORIZED) is an explicit recorded seam; P2-T06 authors no replacement pairing and no synthetic comparison. |

### Pending / review list (AC-AP1 … AC-AP3)

| # | Criterion |
|---|---|
| AC-AP1 | The pending list returns only backend-reported reviewable facts (submitted `pendente` records); filters are backend-applied. |
| AC-AP2 | Opening a pending item loads the exact submitted `peso_id` through the shared read model. |
| AC-AP3 | Filters never invent facts: unknown/ill-formed filter values are refused; empty ≠ lookup-failed ≠ denied. |

### History (AC-H1 … AC-H5)

| # | Criterion |
|---|---|
| AC-H1 | The complete decision trail (approve/reject/reopen events with actor/time/version/reason/prior status) is readable per Peso. |
| AC-H2 | The local Histórico filters are the §15.2 set, backend-applied; invalid filters refused. |
| AC-H3 | Tables are selection surfaces: single click selects, double click opens the exact record, contextual actions live outside the table; no per-row action button grid. |
| AC-H4 | Histórico de Pesos is HISTÓRICO (local) inside Controlo Approve — never conflated with HISTÓRICO GLOBAL (`historia`, deferred); no such route/entry exists. |
| AC-H5 | History rows carry the exact `peso_id` and status/decision/actor/time facts; opening loads the exact record with its trail. |

### Concurrency (AC-K1 … AC-K4)

| # | Criterion |
|---|---|
| AC-K1 | Approve/reject/reopen are version-guarded: stale version fails closed with 409 `stale-version`; nothing written. |
| AC-K2 | No partial mutation on any decision failure (atomic). |
| AC-K3 | No silent overwrite, no automatic merge, no automatic retry; double decisions refused (`already-decided`). |
| AC-K4 | Conflict/reload recovery is compatible with the accepted D2 behavior (conflict presentation + explicit reload; observed version refreshed only on success). |

### Renderer / read-model reuse (AC-RD1 … AC-RD3)

| # | Criterion |
|---|---|
| AC-RD1 | The review sheet embeds the exact shared `PesoSheetReadModel` type; no copied/duplicated Peso facts model exists. |
| AC-RD2 | Review rendering matches Create's read-only presentation field-for-field (same labels, order, ≤ 2-dp normalization; no hidden result set) — proven by the renderer-parity test. |
| AC-RD3 | Approval-only presentation composes around the shared model; the Peso facts model is never altered or replaced. |

### Migration / schema (AC-MG1 … AC-MG3)

| # | Criterion |
|---|---|
| AC-MG1 | Exactly ONE new additive migration (006) owns exactly the one decision table and the one additive `pesos` index; migrations 001–005 and `DmoDbContext.cs` byte-identical; `Down` exact inverse; re-apply idempotent. |
| AC-MG2 | The decision table enforces the closed vocabularies (decision/prior-status CHECKs), the reason rule (validator + 23514-mapped CHECK), `RESTRICT` FKs and exact §6 columns. |
| AC-MG3 | Decision rows are immutable/append-only after COMMIT (no UPDATE/DELETE path); `pesos.version` increments exactly once per decision transition. |

### Fixed desktop (AC-L1 … AC-L3)

| # | Criterion |
|---|---|
| AC-L1 | Aprovar and Histórico are designed/validated at 1366 × 768 with region-stable composition; no breakpoint variant, no relocation, no card conversion, no column hiding. |
| AC-L2 | Over-wide regions use keyboard-reachable local overflow; no required column hidden, no action moved between regions at any desktop width. |
| AC-L3 | The shared header/navigation system is untouched (no redesign; existing shell consumed as-is). |

### Boundaries (AC-B1 … AC-B9)

| # | Criterion |
|---|---|
| AC-B1 | No Definições and no operational setting of any kind exists under Approve (repairers, machine assignments, PDF directory, email lists, templates, glass/water density). |
| AC-B2 | No PDF generation, file creation, directory writing, document regeneration, document identity, email sending/routing exists in P2-T06. |
| AC-B3 | No Boquilhas behavior exists in P2-T06 (movements, BQ aggregate, repairer resolution, Boquilhas history). |
| AC-B4 | `ModuleRegistrations.CurrentBuildAvailable` stays `[]`; no route/destination/navigation registration. |
| AC-B5 | No HISTÓRICO GLOBAL (`historia`) route/entry/registration. |
| AC-B6 | No `previous_peso_id`, no per-CM decision carrier, no `controlo_sheet_id`/Folha table/column/route, no replacement sheet. |
| AC-B7 | No second calculation engine (no water/glass density engine, no formula, no config re-read). |
| AC-B8 | No generic lifecycle/revision/queue/audit infrastructure. |
| AC-B9 | No identity duplication: no new `peso_id`/`cm_id`/`tool_id`/`jobon_id` minting; no approval-copy Peso; no `production_id`/`job_on_revision_id`/`approval_peso_id` anywhere in P2-T06 code or schema. |

**Count: 61 criteria** (AC-I1…AC-I6, AC-A1…AC-A4, AC-R1…AC-R5, AC-D1…AC-D4, AC-J1…AC-J3,
AC-O1…AC-O5, AC-CP1…AC-CP4, AC-AP1…AC-AP3, AC-H1…AC-H5, AC-K1…AC-K4, AC-RD1…AC-RD3,
AC-MG1…AC-MG3, AC-L1…AC-L3, AC-B1…AC-B9) ↔ the §26.4 rows (**67 rows**; audit in §26.4:
**missing 0, dangling 0, orphan 0**).

---

## Appendix A — Protected boundaries

### A.1 Must not be touched by P2-T06

```text
src/DMO.Application/Access/**                          (catalog, registry, resolver, availability)
src/DMO.Web/Authorization/**                           (module/administration gates)
src/DMO.Web/Auth/*, src/DMO.Web/Startup/*, src/DMO.Application/Accounts/*, Session/*
src/DMO.Infrastructure/Persistence/DmoDbContext.cs
src/DMO.Infrastructure/Migrations/20260922001736_*, 20260922001757_*,
   20260922232349_ToolJobOnDomainCore.*, 20260923045054_ControloCreateDomain.*,
   20260923122429_GlassDensitySettings.*             (and their Designers)
src/DMO.Application/ControloCreate/**                  (P2-T05 closed application surface)
src/DMO.Application/Repositories/IPesoRepository.cs (+ P2-T05/P2-T04 siblings; read-only consumables)
src/DMO.Infrastructure/Persistence/Entities/{Peso, PesoMeasurementRow, Repairer, …}Entity.cs
src/DMO.Infrastructure/Persistence/EntityConfigurations/* (P2-T04/P2-T05 configurations)
src/DMO.Infrastructure/Persistence/{Peso, …}Repository.cs (P2-T05 repositories; consumed, not edited)
src/DMO.Web/Endpoints/{ControloCreateEndpoints, ControloDefinicoesEndpoints, JobOnEndpoints,
   FerramentasEndpoints, AuthEndpoints, …}.cs
src/DMO.Web/Pages/Controlo/{Create,Definicoes}.cshtml(.cs), ControloPolicyNames.cs
src/DMO.Web/wwwroot/css/{dmo-tokens,dmo-shell,dmo-user-shell,dmo-components,dmo-jobon,
   dmo-controlo}.css   src/DMO.Web/wwwroot/js/{dmo-focus,dmo-dense-table,dmo-tool-picker,
   dmo-measurement-rows,dmo-jobon,dmo-controlo}.js
src/DMO.Web/Frontend/Shell/**, src/DMO.Web/Frontend/Shared/**, src/DMO.Web/Pages/Shared/**
src/DMO.Web/Navigation/DestinationRouteRegistrations.cs
src/DMO.Web/Pages/{Index,Login,AccessDenied}.*, src/DMO.Web/Pages/Administration/**
docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md
plans/contracts/P2-T02_*.md, P2-T03_*.md, P2-T04_*.md, P2-T05_*.md
tests/**                                       (existing tests keep passing, never weakened)
```

The only accepted non-new files P2-T06 changes: `src/DMO.Web/Program.cs` (additive
`MapControloApproveEndpoints()` + service registrations), `PersistenceServiceCollectionExtensions.cs`
(additive repository registration), `DmoDbContextModelSnapshot.cs` (EF-generated snapshot
extension), and the governance documents of Appendix D.

### A.2 Accepted input, consume only

`ModuleCatalog` (`ControloApprove`), `ModuleRegistrations` (`[]`), the module/administration
gates, authentication/session/`ICurrentAccountContext`, `IPesoRepository` (read),
`IControloCreateService.GetAsync` (the shared Peso read — the ONLY Peso facts source),
`PesoSheetReadModel`, `ConcurrencyConflictExceptionMapping`/`SaveAsync` pattern
(consumed, not modified), migrations 001–005, the shared P2-T01/T02/T03 primitives
(`DenseDataTable`, `AuditTrail`, `DecisionBar`, `RecordStatus`, `CommonState`), the A1 freeze,
and the P2-T05 pages' rendering discipline (parity target — inspected, not modified).

### A.3 Downstream boundaries (must be preserved)

No P2-T06 artifact names, imports, renders, fixtures or tests: a settings/Definições surface, an
approval-copy Peso, `previous_peso_id`/comparison persistence, per-CM decision persistence,
`controlo_sheet_id`/Folha persistence, warnings-as-decisions, Boquilhas code, PDF/file/email/send
code, availability/navigation registration, `historia` (HISTÓRICO GLOBAL), `production_id`,
`job_on_revision_id`, `approval_peso_id`, reverse-ID arrays, or a second Peso read model.

---

## Appendix B — Implementation file ownership / expected paths

```text
src/DMO.Domain/Controlo/                  PesoReviewDecisionId.cs, PesoReviewDecisionKind.cs,
                                          PesoReviewDecision.cs
src/DMO.Application/Repositories/         IPesoReviewRepository.cs
src/DMO.Application/ControloApprove/      ControloApproveModels.cs, ControloApproveValidator.cs,
                                          IControloApproveService.cs, ControloApproveService.cs,
                                          ReviewSheetReadModel.cs
src/DMO.Infrastructure/Persistence/       Entities/PesoReviewDecisionEntity.cs,
                                          EntityConfigurations/PesoReviewDecisionEntityConfiguration.cs,
                                          PesoReviewRepository.cs,
                                          Migrations/<timestamp>_ControloApproveDomain.cs (+Designer),
                                          Migrations/DmoDbContextModelSnapshot.cs (EF extension),
                                          PersistenceServiceCollectionExtensions.cs (additive)
src/DMO.Web/Endpoints/                    ControloApproveEndpoints.cs
src/DMO.Web/Pages/Controlo/Approve/       Index.cshtml(.cs), Historico.cshtml(.cs),
                                          ControloApprovePolicyNames.cs
src/DMO.Web/wwwroot/css/dmo-controlo-approve.css (new)
src/DMO.Web/wwwroot/js/dmo-controlo-approve.js   (only if genuinely required; reuses window.dmoFocus)
src/DMO.Web/Program.cs                    (additive registrations + MapControloApproveEndpoints only)
tests/DMO.UnitTests/ControloApprove/**    tests/DMO.IntegrationTests/ControloApprove/**,
tests/DMO.IntegrationTests/Persistence/   (decision repository + migration tests),
tests/DMO.IntegrationTests/ControloApprove/P2T06RegressionTests.cs, P2T06ProductionScan.cs
```

---

## Appendix C — Fixed desktop obligations

Restated for implementers: §21 rules are binding; canonical validation viewport 1366 × 768;
compact density; region-stable composition (R1–R5, H1–H3); no `@media`/`@container`/`@supports`
structural rule in `dmo-controlo-approve.css`; local keyboard-reachable overflow for wide
regions; no mobile/tablet variants; shared header/navigation untouched.

---

## Appendix D — Governance record

### D.1 Status recorded by this task

| Item | Status |
|---|---|
| P2-T06 | **CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW** (NOT accepted, NOT implemented) |
| P2-T05 (Controlo_Create) | **CLOSED** (closure `3491097…`; original closure unchanged) |
| P2-T05 glass-density correction slice | **CLOSED** (closure `02bd53e…`, dmo-work) |
| P2-T06 | **NOT STARTED — NOT AUTHORIZED** (unchanged; this task authors the contract only) |
| P2-T07 / P2-T08 / P2-T10 | **NOT AUTHORIZED** (unchanged) |
| Application code modified | **NO** |
| Migration created | **NO** |
| Supabase modified | **NO** |
| `ModuleRegistrations.CurrentBuildAvailable` | `[]` (unchanged) |
| `DestinationRouteRegistrations` / route registry | unchanged (still empty) |

### D.2 Governance files updated by this task

| File | Update |
|---|---|
| `plans/contracts/P2-T06_CONTROLO_APPROVE_CONTRACT.md` | **new** — this contract |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §7 P2-T06 CONTRACT STATUS record; Appendix A 9.12 row (status records only) |
| `plans/beta-workstreams/P2-T06-CONTROLO-APPROVE.md` | §13 contract-authored record (pointer + status) |
| `dev/responses/P2_T06_CONTRACT_AUTHORING_RESPONSE.md` | **new** — the authoring response |

`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`, `reports/BETA_MASTER_RECONCILIATION.md`,
all P2-T05 contract files and every closed decision record are **not** edited.

### D.3 Baseline verification (recorded by this task)

```text
DMO-MODULAR remote main (origin/main, fetched)   : 7afcb0079ab2ee84ad2a0356d1f3576e832646e7
dmo-beta-master remote main                      : 78da49248f6cf7a8cbe4ddd946f3c38abbaf322f
dmo-work remote main                             : 02bd53e03e61bcc7d627ea8e1545d79e6eff8a70
dmo-master remote main                           : 8f1ca3e27e0eaf58c3dce285b544066565fa3dc1
P2-T05 CLOSED (implementation 9fbfcf4, review ACCEPT ae99d1f) + correction slice CLOSED
   (implementation ce516d0, verification 7afcb00, review ACCEPT 300f011)
P2-T06 / P2-T07 / P2-T08 / P2-T10               : NOT AUTHORIZED (unchanged)
CurrentBuildAvailable                            : [] (verified in src/DMO.Application/Access/ModuleRegistrations.cs)
working tree (application code)                  : clean before and after authoring
build/tests                                      : not modified (authoring only; no build claimed)
```

### D.4 Planning-gate record (authoring commit)

| Item | Value |
|---|---|
| Contract file | `plans/contracts/P2-T06_CONTROLO_APPROVE_CONTRACT.md` (this file) |
| Authoring commit | `dd0e16390e46d49c811d1597de674dcc68023813` (DMO-MODULAR remote `main`) |
| Authoring response | `dev/responses/P2_T06_CONTRACT_AUTHORING_RESPONSE.md` (same commit) |
| DMO-MODULAR remote `main` before this task | `7afcb0079ab2ee84ad2a0356d1f3576e832646e7` |
| Implementation performed | **NONE** (docs-only planning commit; 4 files: contract + response + master-plan/workstream status records) |
| Status | **AUTHORED — AWAITING ARCHITECT PLAN REVIEW** (PLAN ACCEPT / corrections / reject) |

---

## Appendix E — PLAN REVIEW gate

Implementation may begin only when the Architect has:

1. reviewed **this** file at a recorded SHA;
2. dispositioned the authority questions of §28 — in particular Q-SEND, Q-REOPEN (reopen target
   status and the material-edit mapping), Q-FOLHA/Q-PERCM/Q-COMP (carrier deferrals) and
   Q-RENDER (parity-test reuse default);
3. confirmed the decision vocabulary of §3, the persistence shape of §6/§7, the
   approve/reject/reopen semantics of §19 (including the Identity-Rule mapping), the route/policy
   matrix of §13/§14 and the migration contract of §25;
4. returned an explicit `PLAN ACCEPT` (or `CORRECTION REQUIRED` / `REJECT`) per
   `dmo-beta-master/WORKFLOW.md` step 6.

Until then:

```text
P2-T06 CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW
NOT IMPLEMENTED — NOT AUTHORIZED
CurrentBuildAvailable = []
```