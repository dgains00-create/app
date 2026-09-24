# P2-T07 — Boquilhas — BACKEND / INTERFACE CONTRACT

> **OWNER CLARIFICATION (final functional rule):** **§33 below is a NEW OWNER CLARIFICATION that
> SUPERSEDES every AFFECTED rule of this contract** — standalone Boquilhas, the Início movement
> type, the Irreparável movement semantics, the open/closed lifecycle (status, close/reopen,
> close snapshots, reopening history), the B1 active-aggregate race machinery (the two ACTIVE
> partial unique indexes and their 23505 mapping) and the four-bucket balance model are replaced
> by the production movement register model of §33. Rules not listed in §33.1 remain in force.

**Workstream:** P2-T07 — Boquilhas (BQ external-repair quantity workflow; aggregate + movement
ledger + edit-audit + close/reopen + local Histórico).
**Task class:** contract authoring only. **No implementation, no migration, no Supabase change.**
**Handoff:** `plans/beta-workstreams/P2-T07-BOQUILHAS.md` (authoritative handoff for this
workstream). **Settled delta:** `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` (§3–§6,
§11).
**Status:** **CORRECTED (B1) — AWAITING FOCUSED ARCHITECT PLAN RE-REVIEW.** The Architect PLAN
review (`dev/reviews/P2-T07_BOQUILHAS_CONTRACT_PLAN_REVIEW.md` @ dmo-work `542a08a1…`) returned
**PLAN REJECT — blocking finding B1 only** (one-active-aggregate-per-anchor concurrency
enforcement). This revision applies exactly that correction: two partial unique indexes
(`IX_boquilhas_active_bq_id`, `IX_boquilhas_active_tool_id`), the exact 23505 →
`Refused(ActiveAggregateExists)` mapping, the create/reopen race-safe backstop semantics and the
corresponding test rows (§7, §8, §10, §11, §22, §23, §28, §29, App. D.5). **Not accepted**;
implementation is authorized only by the Architect `PLAN ACCEPT` per
`dmo-beta-master/WORKFLOW.md`.

> **Scope note (operative):** this contract authors the **operational Boquilhas aggregate and its
> movement history** over the accepted, **CLOSED** P2-T04 domain core (canonical `tool_id`,
> `bq_id` frozen Job On BQ context, the shared Tool search/select/create orchestration) and the
> accepted, **CLOSED** P2-T05 Definições state that Boquilhas **consumes** (repairer register,
> per-machine repairer assignments — read-only; never administered here): the two aggregate flows
> (production-linked `boquilhas_id → bq_id → jobon_id + tool_id`; standalone
> `boquilhas_id → tool_id`, **no fake Job On / no fake `bq_id`**), the **exactly four** write
> movement types (`Início`, `Saída`, `Entrada`, `Irreparável` — `Editar` is an action, **never** a
> fifth type), the derived balance buckets (Disponível / Em reparação / Irreparável / Entrada
> excecional) from movement facts **only** (no second mutable balance authority), edit-with-audit
> on the same `movement_id` (no second quantity event, no double balance effect),
> `business_date` (editable) ⊥ `recorded_at` (immutable), close/reopen on the **same**
> `boquilhas_id` (immutable close snapshot; recorded reopen actor/time/reason; failed close is
> atomic), historical repairer preservation (snapshotted on the movement — a later assignment
> change never rewrites an earlier record), the local Histórico (HISTÓRICO (local), **not**
> HISTÓRICO GLOBAL), the fixed-desktop surfaces, and the exact boundary records (no Boquilhas
> settings, no PDF/file/email mechanics, no availability registration) — **nothing is invented**
> (see §2, §15–§27 and the authority questions of §30).

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
| 15 | Identity model — binding |
| 16 | Tool selection / creation contract |
| 17 | Movement contract |
| 18 | Balance derivation contract |
| 19 | Edit / audit contract |
| 20 | Business date vs recorded time |
| 21 | Repairer resolution contract |
| 22 | Production-linked vs standalone contract |
| 23 | Active aggregate, utilisation, close / reopen |
| 24 | Local Histórico behavior |
| 25 | Fixed-desktop UI regions |
| 26 | Negative-scope protections |
| 27 | Downstream seams (P2-T08 / P2-T10 / Job On) |
| 28 | Migration contract |
| 29 | Test-to-acceptance matrix |
| 30 | Authority questions |
| 31 | Authority questions — summary |
| 32 | Implementation acceptance criteria |
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

**Area-specific supersession (settled).** `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`
is repository-recorded settled functional authority for the settings/repairer/email/PDF area, and
the master plan §7 (P2-T07) + the handoff incorporate it. In the Boquilhas area it supersedes the
following statements of `dmo-master/dmo-modular/modules/BOQUILHAS.md` (consumed here only where
needed for global identity/access/module rules):

| Superseded global statement | Current settled authority |
|---|---|
| §10: "the canonical repairer directory … is created and managed under `Admin > Definições > Geral > Reparadores` (ADMIN contract)" | the **repairer register is owned by `Controlo_Create → Definições`** (`…DELTA.md` §3; P2-T05 contract/closures); Boquilhas **selects/consumes** only; **no ADMIN-managed register surface in this Beta** |
| §10/§15: per-line (B1–C3) suggested/allowed repairer association configured in "`Admin > Definições > Boquilhas`"; "Boquilhas has exactly one configurable settings family" | the **per-machine repairer assignments are owned by `Controlo_Create → Definições`** (`…DELTA.md` §4; P2-T05 contract §11/§16.4); Boquilhas has **no settings surface and no internal Definições tab** (master plan §7 P2-T07 non-scope; handoff §6) |
| §10: "repairers are deactivated (never deleted)" | P2-T05 settled **current-state register with no delete/deactivation path** (P2-T05 AC-D3); Boquilhas consumes the register as-is and invents no lifecycle |
| §12: the "Production-lines side panel (B1–C3)" as a Job-On-dependent contextual surface | the **Boquilhas machine/sidebar concept that depends on Job On operational context is REMOVED from current visual authority** (`…DELTA.md` §11; master plan §7 P2-T07): no simulated Job On machine/reference state inside Boquilhas; the settled **production-line contextual panel reading (not owning) real production context** remains (§22) |

The global §6/§7/§8/§9/§13/§14/§16/§17/§18 rules (identity model, movement vocabulary, trace
lifecycle, balance, excess return, repairer attribution, history traversal, close/reopen on the
same identity) are **consumed** below and are not contradicted by this contract.

### 1.2 Authority read for this contract (completely)

**DMO-MODULAR:**

- `plans/beta-workstreams/P2-T07-BOQUILHAS.md` — the binding handoff (purpose §1, authority §2,
  starting point §3, scope §4, blocker B3 §5, non-scope §6, expected files §7, access §8,
  persistence §9, tests §10, acceptance §11, completion evidence §12);
- `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` — §0 terminology (HISTÓRICO local vs HISTÓRICO
  GLOBAL), §4 B3, §5 dependency graph (P2-T04 before P2-T07, hard rule 3), §7 P2-T07 (scope,
  settled repairer resolution, removed sidebar, non-scope), §8 shared-data map (Tool, Job On,
  CM/MF/BQ context, Boquilhas aggregate, Movement, Repairer, Machine repairer assignment rows),
  §9 route/availability plan (Boquilhas → `/boquilhas`, available at P2-T07 complete, route
  registered in P2-T10), §10 access plan, §11 P2-T07 test strategy, §12 protected register, §13
  integration sequence (step 12 = P2-T07; step 13 = P2-T10d Boquilhas availability),
  Appendix A (9.13/9.14 rows);
- `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` — §3 (repairer register owned by
  Controlo_Create → Definições; name-only; Boquilhas consumes §3.6), §4 (six independent
  machines, no grouping, no cascade), §5 (automatic Boquilhas repairer resolution
  machine → current assignment → repairer; operator should not normally re-select; boundaries
  §5.4 — the movement still records the chosen value; override remains an implementation-level
  decision for this contract), §6 (historical preservation — binding; implementation-neutral
  pattern family), §11 (machine sidebar removed from current authority; production-line
  contextual panel reading real context is a different thing, unchanged), §12 (superseded
  visuals V2/V6/V7/V8), §13–§16;
- `plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md` (accepted, CLOSED) — every seam
  this contract consumes: canonical Tool identity (§3.1 `tools` + `tool_type` CHECK
  `CM|MF|BQ`), `tool_machines` machine compatibility (§3.2, codes `B1..C3`, one-or-more),
  `bq_contexts` (§3.4: frozen `tool_type='BQ'`/`tool_reference`/`tool_lot` triple, direct
  `tool_id` relation, `jobon_id` owner, no machine column, no version — parent-protected),
  context rules (§7: idempotent create/reuse for the same `jobon_id + tool_id`; contexts created
  only where actually needed), query contracts (§8: reference → productions, Tool search with
  `Type` filter, context resolution, Job On ficha), the **shared Tool search/select/create
  orchestration** (§9: no auto-selection, ambiguity explicit, canonical `tool_id` returned,
  origin-state preservation, opaque keys, create returns real id, origin never becomes Tool
  owner), create/edit transactions (§11, `ToolAssociationChange` Set on the BQ slot), repository
  contracts (§12.2 `IToolRepository`/`IJobOnRepository`), service contracts (§12.3
  `IToolService`/`IJobOnService`, validation codes incl. `TOOL_NOT_FOUND`, `TOOL_TYPE_MISMATCH`,
  `MACHINE_UNKNOWN`, `MACHINE_REQUIRED`, `REFERENCE_REQUIRED`), dependency-probe seam (§12.4
  `IJobOnDependencyProbe` — future modules register one additive line each), route matrix (§13:
  14 routes incl. `GET /ferramentas/tools` 12 and `POST /ferramentas/tools` 13, both
  `dmo.module.ferramentas`), failure/result vocabulary (§14, `Refused` union, 409 single refusal
  status, mapping table), concurrency (§15), migration contract (§16: 003, six tables),
  Appendix A/B;
- `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` (accepted, **CLOSED**) — the consumed
  Definições state: `repairers` (§16.3: `repairer_id`, name-only, RESTRICT FKs), 
  `machine_repairer_assignments` (§16.4: one independent current assignment per machine,
  `machine IN ('B1'..'C3')` UNIQUE, current-state only — **no assignment history** (§11.4,
  Q-HIST), the resolution read contract §11.4 ("the resolution query itself (machine → repairer)
  is a read contract for the future consumer (P2-T07)"), the no-delete/no-deactivation register
  rule (§10.3/AC-D3), the identity-anchoring pattern this contract mirrors (§3.2 DB-enforced
  exclusive anchor `CHECK ((cm_id IS NULL)::int + (tool_id IS NULL)::int = 1)`), the "missing
  context created through the Job On application contract" composition (§21.4 route 11), the
  "Tool search/create access remains `ferramentas`-gated; no second Tool access path" rule
  (§22/§23.1/AC-R6), transactions (§18), concurrency (§19 `SaveAsync` +
  `ConcurrencyConflictExceptionMapping`), routes (§21), failure vocabulary (§26.1/§26.2),
  downstream seams (§29 incl. the **P2-T07 repairer-resolution seam**: "current-state assignment
  rows + read contract → snapshot `repairer_id` on its records; never rewrites history; never
  administers the register"), acceptance (§30);
- `plans/contracts/P2-T06_CONTROLO_APPROVE_CONTRACT.md` (accepted, **CLOSED**) + the P2-T06
  closure record — the most recent accepted contract conventions this contract mirrors
  (section index shape, vocabulary tables, result unions, route matrix, test-to-acceptance
  matrix audit, BND negative-scope table, append-only decision-trail pattern, page constants
  `…PolicyNames` pattern, interim-runtime-state pattern); P2-T06's closure records "NEXT ELIGIBLE
  GATE: P2-T07 planning/contract gate (recorded only — NOT AUTHORIZED)";
- `dev/responses/P2_T04_IMPLEMENTATION_RESPONSE.md`, `dev/responses/P2_T05_IMPLEMENTATION_RESPONSE.md`,
  `dev/responses/P2_T06_IMPLEMENTATION_RESPONSE.md` — shipped seams (exact route paths, page
  policy constants, `SaveAsync` mapping, `PesoJobOnDependencyProbe` additive registration,
  `dmo-{module}.css`/`.js` page-owned asset pattern, D1/D2 `renderConflict` reload-recovery
  pattern);
- `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` — §1A fixed desktop, §4 state vocabulary
  (`conflict` = clear message + supplied recovery choices; empty ≠ lookup-failed ≠
  permission-denied), §9 `DenseDataTable` (single click selects, double click opens **only when
  the consumer supplies it**, actions outside the table, no per-row action grids), §12
  `AuditTrail` (renders only supplied actor/timestamp/action and optional before/after detail;
  never synthesizes attribution), §14 `DecisionBar` (disabled reasons; no decision rule
  encoded);
- `plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md` and
  `plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md` (accepted) — the exact
  component contracts consumed;
- `src/` (read-only inspection, remote-main state) — `ModuleCatalog` (`Boquilhas` =
  `boquilhas`, destination `boquilhas`), `ModuleRegistrations` (`[]`), the shipped migrations
  001–006 (6 migrations; 21 public tables), `DmoDbContext`, `JobOnPolicyNames`/
  `ControloPolicyNames` (the pinned-policy pattern to mirror), `Endpoints/*` (the minimal-API
  group pattern), `Pages/*` (page + `[Authorize(Policy = …)]` pattern).

**dmo-beta-master (BETA authority):**

- `modules/BOQUILHAS.md` (full) — purpose, included/outside scope, canonical identities
  (`tool_id`/`bq_id`/`boquilhas_id`/`movement_id`; `bq_id` is not `boquilhas_id`, neither
  replaces `tool_id`), production-linked flow, standalone flow (no fake Job On/`bq_id`),
  search/select/create, opening facts, movement vocabulary (**exactly four** write types;
  `Editar` is an action, not a type; append-only movement facts
  `movement_id ├─ movement_type ├─ quantity ├─ business_date ├─ recorded_at ├─ actor ├─
  repairer_id? (required on external Saída) └─ observations/context`), business date vs audit
  timestamp, edit/audit (preserves before/after, authenticated user, system timestamp; the edit
  record is audit history, **not** another quantity event; editing must not change balance
  twice; any annulment/removal supported by the backend is a registered confirmed fact — never
  silent physical deletion), balance (movement facts are the authority; **no second
  independently mutable balance**; buckets Disponível / Em reparação / Irreparável / Entrada
  excecional; Saída cannot exceed available; Irreparável cannot exceed in-repair; excess Entrada
  recorded, not silently clamped/rejected; zero balance does not delete the aggregate; negative
  saldo visible and not automatically blocking), excess Entrada (stores the real returned
  quantity; excess remains visible as a movement fact/projection; the original Entrada is never
  rewritten; later discrepancy-resolution is a new recorded fact with its own note/attribution,
  required **only at resolution time**, never to save the Entrada), repairer (external Saída
  stores the final selected canonical `repairer_id`; suggested/default may be shown but the
  human can select the final value where allowed; historical movements retain their repairer
  relation even if the directory/default changes later), close/reopen (same `boquilhas_id`;
  active → close + immutable close snapshot → archived projection → optional reopen under
  allowed conditions; movements remain linked to the same aggregate; close never creates a
  replacement aggregate; failed close leaves active state unchanged; reopening recorded with
  actor/time/reason; history remains replayable; the exact allowed-reopen eligibility is
  enforced by the backend/domain contract), Job On boundary, History (read/query projection over
  the same facts; filters reference/lot/line/business date-period/movement type/repairer/
  aggregate-file state/pagination; single click selects, double click may open — UI behaviors,
  not domain authority), access (one assignable module; navigation availability is projection
  only; backend route/action enforcement required), frontend responsibility, backend contracts
  required, acceptance criteria, required evidence;
- `modules/JOB_ON_LIGHT.md` — production-context surface (reference → productions query,
  explicit selection, no auto-selection, CM/MF/BQ association with canonical `tool_id`, shared
  Tool orchestration, no private Tool registry) — the context P2-T07 reads;
- `modules/FERRAMENTAS_LIGHT.md` — search/select/create contract, ownership (Ferramentas owns
  Tool master facts; the origin module never becomes owner), no top-level destination;
- `architecture/RECORD_LIFECYCLES.md` — §9 Boquilhas (aggregate + movements; movements are
  append-only facts; editing creates preserved edit/audit history, **not** a second quantity
  movement; close keeps the same `boquilhas_id`, writes an immutable final close
  snapshot/metadata, preserves all movements, moves the aggregate out of active lists into the
  archived/history projection; a failed close does not produce a valid partial closed state;
  reopen keeps the same `boquilhas_id`, recorded actor/time/reason, allowed only for the last
  closed trace and only where the settled aggregate rules permit it, never rewrites prior
  movements or close history; close and reopen are **aggregate lifecycle facts, not movement
  types**), §12 cross-record invariants;
- `architecture/CROSS_MODULE_FLOWS.md` — Job On → Boquilhas (production-linked
  `jobon_id → bq_id → tool_id → boquilhas_id → movement_id`; standalone `tool_id →
  boquilhas_id → movement_id`; **no fake Job On** in the standalone case), shared Tool flow (one
  shared orchestration, origin state restored, owning module persists its own relation), read
  projections, anti-inference rules, backend/frontend crossing;
- `architecture/BACKEND_FRONTEND_MODEL.md` — canonical identity chain,
  `boquilhas_id → bq_id → tool_id + jobon_id` / `boquilhas_id → tool_id`, movement persistence
  ("Movement types are append-only quantity facts. Editing an existing movement preserves
  before/after audit and does not create a second quantity event unless an explicitly authorised
  annulment/correction model says otherwise"), cross-module reads, backend owns IDs/persistence/
  transactions/relations/gates/audit/concurrency;
- `architecture/ACCESS_AND_NAVIGATION.md` — Boquilhas is one assignable module; navigation is a
  projection, never a grant; direct-route enforcement is server-side; current-build availability
  rules; shared-destination rule (Boquilhas has **no** shared destination);
- `implementation/BETA_INTEGRATION_SEAMS.md` — Workstream E (owns aggregate
  registration/opening, movement entry, derived balance presentation, movement edit + audit,
  History, close/reopen, production-linked and standalone repair-flow orchestration; depends on
  B only for Tool/Job On context orchestration, **not** for its movement ledger), "B → E seam"
  (canonical BQ Tool selection/create, optional selected `jobon_id`, production-linked `bq_id`
  resolution/reuse, return to the same Boquilhas origin state after Tool creation; standalone
  remains valid without Job On), "E → B reverse status seam" (Job On may read Boquilhas state;
  Job On does not own movement facts), access/action and navigation/route-registration seams,
  concurrency/stale-data rule (shared `stale`/`conflict` vocabulary; no silent retry/overwrite),
  testing seam (Boquilhas edit creates audit history without a second quantity movement);
- `contracts/IDENTITIES_AND_RELATIONSHIPS.md` — identity map (`boquilhas_id` = one BQ
  external-repair aggregate; `movement_id` = one quantity movement/event; production-linked
  `boquilhas_id → bq_id → tool_id + jobon_id`, standalone `boquilhas_id → tool_id`), Boquilhas
  identity rule (`boquilhas_id` is a quantity/repair aggregate, not `bq_id`, not `tool_id`, not
  a physical BQ piece; no per-piece UUID), no reverse-ID arrays, no fake identities (no
  `production_id`, no `job_on_revision_id`, no fake `jobon_id` for standalone Boquilhas), human
  selection rule;
- `contracts/SHARED_FRONTEND.md` — §9 `DenseDataTable`, §12 `AuditTrail`, §14 `DecisionBar`, the
  common state vocabulary (`stale`/`conflict` distinct);
- `ACCEPTANCE_MATRIX.md` — §7 (Boquilhas acceptance/evidence: four-movement vocabulary,
  production-linked and standalone creation, Tool-create return-state, movement validation,
  excess Entrada and negative-saldo tests, edit/audit one-movement test, business-date vs
  recorded-at tests, close/reopen history tests, History filter/select/open tests, access and
  responsive rendered tests), §9–§11 gates;
- `WORKFLOW.md` — controlled chain, conflict protocol, anti-invention rules.

**dmo-work (accepted rulings/reviews closing P2-T05 identity/context decisions — binding, not
reopened):**

- `dev/reviews/P2-T05_CONTROLO_CREATE_CLOSURE.md` and
  `dev/reviews/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_CLOSURE.md` — P2-T05 and its
  correction slice are **CLOSED**; the register/assignment state this contract consumes is
  closed authority (name-only register, current-state six-machine assignments, no
  delete/deactivation);
- `dev/reviews/P2-T06_CONTROLO_APPROVE_CLOSURE.md` — P2-T06 **CLOSED**; "NEXT ELIGIBLE GATE:
  P2-T07 planning/contract gate (recorded only — NOT AUTHORIZED)"; P2-T07/T08/T10 NOT
  AUTHORIZED;
- `dev/reviews/P2-T04_DOMAIN_CORE_TOOL_JOBON_IMPLEMENTATION_REVIEW.md` (ACCEPT) — the closed
  Tool/Job On/context seams this contract consumes (incl. `bq_contexts`, the shared Tool
  orchestration, the probe seam);
- `dev/rulings/P2_T05_PESO_IDENTITY_RULE.md`, `dev/rulings/P2_T05_PESO_APPROVAL_PDF_AUTHORITATIVE_DATA_RULE.md`,
  `dev/rulings/P2_T05_GLASS_DENSITY_CONFIGURATION_OWNER_RULE.md` — Peso-area rulings read for
  their general identity discipline (same-record transitions, no second authoritative dataset,
  settings changes affect new records only); they bind Peso/Controlo, and their principles
  (same identity across lifecycle transitions; the record's own persisted facts stay
  authoritative; configuration changes never rewrite records) are consumed in the Boquilhas
  area exactly as the Boquilhas-specific authority states them (§19, §21, §23).

**dmo-master (global authority; consumed where needed — identities/access/lifecycle):**

- `modules/BOQUILHAS.md` (full, current) — identity model (§2), production-linked vs standalone
  (§3), what `boquilhas_id` represents (§4; two registered BQ Tools are never merged merely
  because visible reference/lot text matches), selection and creation (§5: EXISTE → SELECIONA /
  NÃO EXISTE → CRIA → CONTINUA; creation is not blocked because the full Ferramentas master is
  not filled in; creation does not transfer master ownership; one canonical `tool_id`, no
  duplicate registry, no manual copy; opening facts: Boquilha/reference required, Lote required,
  Máquina(s)/Linha(s) multi-select B1–C3 at least one, Total do lote = the Início movement (not
  Armazém stock), Utilização inicial manual, Data de abertura editable default today,
  Observações compactas; never a separate "associated line" field and "Fabricar/Reparar" choice;
  machine compatibility never inferred from the reference code), movement vocabulary (§6: the
  four types; `Editar` action semantics; line-context adjustments are movement/aggregate
  context, **not** new movement types; closing is aggregate lifecycle, not a quantity movement;
  movement facts incl. business_date/recorded_at/actor/repairer_id (required on external
  Saída)/motive-detail-observations and line context where recorded; business date vs audit
  timestamp — owner-settled; deletion/removal where available is a registered annulment with
  confirmation, never silent physical removal), trace lifecycle (§7: active → close → immutable
  close snapshot → archived (derived file state) → optional reopen **only the last closed
  trace**; failed close leaves the trace active and never presents a partial snapshot as valid;
  closure moves the trace out of the active lists into the archived projection (History "file
  state" filter); future repairer/line/config changes never modify a closed snapshot; reopening
  allowed only for the last closed trace **and only when no other active trace exists for the
  same BQ Tool context**; reopen recorded who/when/why; close/reopen are aggregate facts/stills
  on the same `boquilhas_id` — closure never creates a second identity, never rewrites
  movements, balance remains replayable), quantity and balance (§8: whole-unit BQ count, not
  Tool identity and not `% utilização`; **do not create a mutable stored balance as a second
  authority**; the four rules; four buckets; `Linha atual` is a context value, not a balance
  component; all buckets are derived projections — never hand-entered, never a stored second
  authority), excess return (§9: real entered quantity; expected_return_quantity and
  excess_received_quantity as movement facts; accumulated excess derived by summing movement
  facts; negative repair-flow saldo is a valid visible projection, not automatically a blocking
  error; return never blocked; discrepancy resolution is a new recorded fact with a mandatory
  note **at resolution time only**; original movement never rewritten), repairer (§10: every
  external-repair Saída persists the final selected canonical `repairer_id`; a suggested/default
  repairer may help but the final stored relationship is the chosen `repairer_id`; later
  directory/default changes never rewrite previous movement history; repairers are shared
  canonical vocabulary — no local free-text identities), utilisation/dates (§11: `% utilização`
  is manual-only where used; never calculated/incremented automatically from quantity movements;
  the live utilisation reading belongs to Ferramentas; a repair trace may capture opening/
  closing utilisation values as **manual stills**; never auto-calculated, never synced, never
  the live usage authority; `Data de abertura` is an editable DATE business date; movement
  `business_date` and `recorded_at` are distinct facts), Job On crossing rules (§12: selection
  in Job On does not create a movement; Boquilhas does not choose the production BQ;
  production-linked anchors to that Job On's `bq_id`; standalone valid; Controlo results do not
  mutate Boquilhas balance; Boquilhas consumes production context through `bq_id` stills and
  does not depend on live mutable Job On state to decide balances; the production-lines side
  panel display rules — read/query/navigation only, Job On mutations stay Job On Create
  behavior, navigation gated by the user's own Job On module access, conflicting display
  informational only), History/query (§13: Registo covers the selected aggregate with all
  movements; transversal History covers the whole system, same data source, only the query
  scope differs; filters reference/lot/line/date-period (business_date)/movement type/repairer/
  aggregate-file state/pagination; movement-table projection Referência · Lote · Movimento ·
  Quantidade · Saldo · Reparador · Linha · Data e hora · Operador; per-movement Saldo
  (saídas − entradas) and negative saldo are valid visible projections; red presentation is a UI
  rule and never blocks; single click selects a row and enables actions; double click opens the
  aggregate; edit history is separately inspectable audit history, never rendered as an extra
  quantity movement; traversals), calendar and documents (§14: operator-facing calendar/date
  picker is a query projection over movement `business_date`; no mandatory official Boquilhas
  document identity), settings (§15: no internal Definições tab; no empty settings for
  symmetry), access and module surface (§16: one assignable module; application surface =
  Registo + the Boquilhas lot grid + Histórico (+ the contextual panel as read/query/navigation);
  views of one module, not additional assignable modules; no Fabrico view), Information Web
  (§17), invariants (§18), modular implementation note (early Boquilhas may select/create the
  real canonical BQ `tool_id`, find/reuse the real `jobon_id`, create a minimal real
  `jobon_id`/`bq_id` when needed and the required production facts are known, persist against
  that `bq_id`; reuse, never another identity for the same production);
- `global/ACCESS_MODEL.md` — §1 (Boquilhas is one assignable module, identity 6), §11
  (Boquilhas grants the pages/actions defined by the canonical module mapping), the
  hidden-control-is-never-the-security-boundary rule;
- `global/INFORMATION_MODEL.md` — Boquilhas direct-relationship rules
  (`movement_id → boquilhas_id → bq_id → tool_id + jobon_id`; standalone variant), fact
  ownership, no repeated reachable IDs.

### 1.3 What is NOT reopened by this contract (closed authority, settled)

- **Identity**: `tool_id` is the canonical Tool identity; `bq_id` is the frozen Job On BQ
  context; `boquilhas_id` is the aggregate; `movement_id` is one movement. No `production_id`,
  no `job_on_revision_id`, no reverse-ID arrays, no per-piece BQ identity, no second Tool
  registry, no `machine_id` scheme, no line/group model.
- **Standalone validity**: standalone Boquilhas (`boquilhas_id → tool_id`) is a first-class
  valid flow; no fake Job On / no fake `bq_id` is ever created.
- **Movement vocabulary**: exactly `Início`, `Saída`, `Entrada`, `Irreparável`; `Editar` is an
  action on an existing movement, never a fifth movement type; no obsolete legacy types.
- **Movement facts as balance authority**: derived buckets; **no second mutable balance authority
  of any kind**; zero balance never deletes the aggregate; negative saldo is visible and
  non-blocking; excess Entrada is recorded, never clamped, never rejected for exceeding the
  expected amount.
- **Repairer ownership**: register and machine assignments are owned by
  `Controlo_Create → Definições` (closed P2-T05); Boquilhas consumes; Boquilhas has **no**
  settings surface.
- **Machine independence**: `B1 B2 B3 C1 C2 C3` are independent; no shared B/C repairer, no
  "Linha B"/"Linha C" grouping, no cascade; changing one assignment changes no other.
- **Historical repairer preservation**: later assignment changes never rewrite earlier movement
  repairer facts.
- **Business date ⊥ recorded time**: `business_date` operator-editable; `recorded_at` immutable;
  changing one never rewrites the other.
- **Close/reopen on the same `boquilhas_id`**: no replacement aggregate, no second identity;
  failed close leaves the active state unchanged.
- **HISTÓRICO terminology**: the P2-T07 History is **HISTÓRICO (local)** inside the module;
  HISTÓRICO GLOBAL (`historia`) is DEFERRED BY DESIGN and is never implied (§24).
- **% utilização is manual** where used; never derived from movements; never auto-incremented;
  never rendered as a progress bar.
- **Removed visual authority**: no machine/reference sidebar simulating Job On state
  (`…DELTA.md` §11); only the settled read-only production-line contextual panel (§22).

### 1.4 Recorded authority silences for P2-T07 (no behavior invented; each pinned below)

| # | Silence | Consequence fixed here |
|---|---|---|
| S1 | **Aggregate anchor representation** — authority fixes `boquilhas_id → bq_id` (production-linked) and `boquilhas_id → tool_id` (standalone) but no physical exclusivity rule | §15: **DB-enforced exclusive anchor** (`CHECK ((bq_id IS NULL)::int + (tool_id IS NULL)::int = 1)`), mirroring the accepted P2-T05 `pesos` anchor pattern (§3.2) |
| S2 | **Opening mechanics** — "Total do lote — the initial quantity … (the Início movement)" without fixing when/how the Início row is created | §17: the **Início movement is created in the same transaction as the aggregate** (the opening quantity is the Início); later `inicio` appends are refused (`only-one-inicio`) (Q-INICIO) |
| S3 | **External Saída machine context** — automatic resolution requires "a registration/movement … associated with a machine", but authority fixes no mandatory movement machine field | §21: **external Saída movements carry a `machine`** (from the aggregate's registered machine set) so automatic resolution operates; other movement types carry machine as optional line context where recorded (Q-MACHINE) |
| S4 | **Edit field set** — "Editar changes an existing movement" without enumerating editable fields | §19: editable = quantity, business_date, machine, repairer_id, observations; **movement_type and recorded_at immutable**; validations re-run (Q-EDIT-FIELDS) |
| S5 | **Replay order** — balance derives from movement facts; authority keeps `business_date` editable and `recorded_at` immutable without fixing the derivation order | §18: deterministic replay order `recorded_at ASC, movement_id ASC` (physical receipt order); **editing `business_date` never reorders the ledger and never changes the balance** (Q-ORDER) |
| S6 | **Entrada expected/excess persistence** — global §9 lists `expected_return_quantity`/`excess_received_quantity` as movement facts; the Beta requires "the excess remains visible as a movement fact/projection" | §17/§18: these two facts are **persisted per Entrada movement row**, computed inside the append transaction by replay (`expected = GREATEST(0, in-repair before)`; `excess = GREATEST(0, quantity − expected)`), DB CHECK-enforced; the **Entrada excecional bucket is the sum of `excess_received_quantity`** (derived projection, never stored) (Q-EXCESS) |
| S7 | **Duplicate/open guard** — reopen is "only … when no other active trace exists for the same BQ Tool context"; authority is silent on creating a second active aggregate for the same context | §23: **creation is refused when an active aggregate already exists for the same anchor** (`active-aggregate-exists`); after close, a new trace may be opened or the last closed trace reopened (Q-CREATE) |
| S8 | **Reopen eligibility algorithm** — "only for the last closed trace and only when no other active trace exists for the same BQ Tool context" | §23: exact three-condition algorithm (closed; its close is the most recent close among all aggregates sharing its anchor; no other active aggregate shares its anchor) (Q-REOPEN-ELIG) |
| S9 | **Reference/lot on the aggregate** — global §5 lists them as opening fields; the same §5 forbids a manual copy and master plan §8 forbids duplicate master data; contexts freeze only the production-linked triple | §15/§24: the aggregate **stores no reference/lot copy**; they are read through the anchor — standalone via live `tools`, production-linked via the frozen `bq_contexts` triple; History reference/lot filters are traversal predicates (Q-REFLOT) |
| S10 | **Line/current-line representation** — global §8 "`Linha atual` is a context value"; Beta opening facts are machine(s)/line(s) multi-select | §17/§23: **no "Linha atual" column is invented**; the line context is (a) the aggregate's registered machine set (`boquilha_machines`, editable via the opening-facts route) and (b) per-movement `machine` facts where recorded (Q-LINE) |
| S11 | **Close metadata date** — "closing date/time and closing user" could be read as operator-entered | §23: close date/time and user are **backend facts** (`closed_at` backend clock, `closed_by_user_id` current account) inside the immutable snapshot; no operator close date is invented (Q-CLOSE-DATE) |
| S12 | **Annulment/removal** — Beta: "Any annulment/removal supported by the backend is a registered, confirmed fact — never silent physical deletion" (conditional) | §19: **no annulment/removal carrier in P2-T07**; `Editar` (audited replace) is the correction mechanism; a future annulment needs its own authority and must be a registered, confirmed fact — never a physical delete (Q-ANUL) |
| S13 | **Utilisation still shape** — "initial utilisation as a manual still where applicable"; "opening/closing utilisation values as manual stills"; `% utilização` never derived | §23: **one manual `utilisation_percent` fact on the aggregate** (operator-entered at opening, editable, 0–100 or NULL), captured as-is in the close snapshot; never derived from movements, never synced, never rendered as a progress bar (Q-UTIL) |

---

## 2. Scope / non-scope

### 2.1 In scope (this contract authors all of it)

1. **Identity core** — the `boquilhas_id` aggregate with the DB-enforced exclusive anchor
   (`bq_id` production-linked | `tool_id` standalone) and the `movement_id` ledger (§15).
2. **Opening flow** — production-linked and standalone aggregate creation through the shared
   Tool orchestration, the Job On reads, BQ-context reuse/creation, opening facts, and the
   Início movement (§16, §17, §22).
3. **Movement entry** — exactly the four write types with exact per-type forms, validations and
   the balance-relative refusals (§17).
4. **Derived balance** — the four buckets derived by exact replay from movement facts; no
   second mutable balance authority (§18).
5. **Edit + audit** — same-`movement_id` edit with before/after audit, backend actor/time,
   concurrency, no second quantity event, no double balance effect (§19).
6. **Dates** — `business_date` (editable) versus `recorded_at` (immutable); opening date
   (editable business date) (§20, §23).
7. **Repairer resolution** — machine → current assignment → resolved/suggested repairer, final
   human-confirmed `repairer_id` stored on the movement, historical preservation (§21).
8. **Close/reopen** — same-`boquilhas_id` lifecycle with immutable close snapshot, atomic failed
   close, recorded reopen, exact eligibility (§23).
9. **Utilisation** — manual `utilisation_percent` still, edited by the operator, captured in the
   close snapshot; never derived (§23).
10. **Opening-facts update** — opening business date, machine set, manual utilisation,
    observations (aggregate context only; never a movement type) (§23).
11. **Local Histórico** — the module's History list with backend-applied filters, table
    selection/open semantics, paging (§24).
12. **Schema/migration** — exactly SIX new tables in exactly ONE new additive migration (007)
    (§6, §7, §28).
13. **Contracts** — repository/application interfaces, routes (all `boquilhas`), authorization,
    failure vocabulary, concurrency, test-to-acceptance matrix (§8–§14, §29, §32).

### 2.2 Explicit non-scope (never implemented by P2-T07)

| Non-scope | Owner/authority |
|---|---|
| Repairer administration (register add/edit/select, machine assignments, PDF directory, email lists, email templates, glass/water density) | `Controlo_Create → Definições` (closed P2-T05); **no P2-T07 settings surface, no internal Admin/Definições tab** (`…DELTA.md` §1/§3/§4; global §15) |
| Mandatory Boquilhas PDF, document generation, file storage/writing, document identity, directory handling, email sending/routing/templates | **P2-T08** (NOT AUTHORIZED) (§26, BND-B2/BND-B7) |
| Module availability registration, destination route registration, navigation exposure | **P2-T10** — `ModuleRegistrations.CurrentBuildAvailable` stays `[]` (§13.3, §26 BND-B4) |
| HISTÓRICO GLOBAL (`historia`) | DEFERRED BY DESIGN (master plan §0/§3.1/§14.1) — never merged with the local Histórico (§24) |
| Any machine/reference **sidebar simulating Job On state** | REMOVED from current visual authority (`…DELTA.md` §11); the read-only production-line contextual panel (§22) is a different, settled surface |
| Job On planning ownership; choosing the production BQ; Armazém stock/location truth; per-piece BQ identity | global §5/§12/§18; `IDENTITIES_AND_RELATIONSHIPS.md` |
| A fifth movement type, obsolete legacy movement types (`Contagem`, "Fabricar/Reparar", …), annulment/removal actions | §17, §19 (S12) |
| The full Ferramentas lifecycle (change requests, technical condition, usage history), the full Job On lifecycle | outside Beta (`FERRAMENTAS_LIGHT.md`, `JOB_ON_LIGHT.md`) |
| Generic lifecycle/state engine, revision infrastructure, generic audit engine, reverse-ID arrays, `production_id`/`job_on_revision_id` | forbidden (`RECORD_LIFECYCLES.md` §1; `IDENTITIES_AND_RELATIONSHIPS.md`) |
| Mobile/tablet layouts, breakpoint reflow, card conversion, table-row action grids, navigation redesign | fixed desktop policy (§25; master plan) |

### 2.3 Dependency statement (exact)

- **Blocker closed by this contract:** **B3** (the Boquilhas backend/interface contract — the
  aggregate/movement schema, the balance derivation, the edit/audit representation, the
  close/reopen representation, the History query shapes, the transactional boundaries, the
  route/endpoint matrix, and the `repairer_id` consumption shape). Authoring **does not close
  B3**: only the Architect `PLAN ACCEPT` does.
- **Depends on:** **P2-T04** (CLOSED) — canonical BQ Tool selection/create (shared
  orchestration), optional `bq_id` resolution/reuse/creation through `IJobOnService`, `job_ons`/
  `bq_contexts` reads, the probe seam.
- **Consumes (read-only) the CLOSED P2-T05 Definições state:** `repairers` and
  `machine_repairer_assignments` application repositories/services (the P2-T05 §11.4 read
  contract and §29 seam explicitly reserve this consumption for P2-T07). **No P2-T05 write
  surface is used and none is added.**
- **Not a dependency:** P2-T06 (closed, unrelated decision core); P2-T08/P2-T10 remain NOT
  AUTHORIZED.

---

## 3. Domain / state vocabulary

### 3.1 Movement type vocabulary (exact — closed set, four values)

```text
movement_type ∈ { 'inicio' | 'saida' | 'entrada' | 'irreparavel' }
labels:            Início | Saída | Entrada | Irreparável
```

- ASCII domain tokens (accepted convention, P2-T06 §3.2 precedent), canonical Portuguese labels
  for presentation.
- **`Editar` is an action on an existing movement — it is NOT a movement type and no token,
  enum value or CHECK value exists for it** (AC-V2; proven by scan + rendered test).
- Only these four values are writable; any other value is `MOVEMENT_TYPE_INVALID` (§12).
- No obsolete/legacy type is carried forward (`Contagem`, "Fabricar/Reparar" choice, …).

### 3.2 Aggregate status vocabulary (exact — two values)

```text
boquilhas.status ∈ { 'active' | 'closed' }
labels:                Ativo | Fechado
```

- `active` = the trace accepts movements/edits/opening-fact updates; it appears in the Registo
  lot grid.
- `closed` = the trace has a committed immutable close snapshot; it appears only in the
  archived projection / Histórico ("aggregate/file state" filter); no movement/opening-fact
  mutation is accepted without a reopen (§23).
- Status is aggregate **lifecycle**, not a movement type and not a balance component
  (`RECORD_LIFECYCLES.md` §9).
- No third status exists (no `rascunho`, no `arquivado`, no `cancelado` — "archived" is the
  derived file-state projection of a closed trace, not a column).

### 3.3 Balance buckets (derived projections — exact)

```text
Disponível           = Σ(início) − Σ(saída) + Σ(entrada)
Em reparação         = Σ(saída) − Σ(entrada) − Σ(irreparável)
Irreparável          = Σ(irreparável)
Entrada excecional   = Σ over entrada rows of excess_received_quantity
```

- Buckets are **derived at read time from movement facts only**; they are never hand-entered
  and never stored as a mutable balance (§18; AC-B1).
- Invariant: `Disponível + Em reparação + Irreparável = Σ(início)`.
- Negative `Em reparação` (and any negative saldo projection) is a **valid visible projection**,
  never an automatic block (§18).
- `% utilização` is **not** in this set and is never derived from it (§23).

### 3.4 Repairer resolution read vocabulary

```text
resolution outcome ∈ { assignment-found | assignment-unavailable }
```

- `assignment-found` — the machine's current assignment (a real `repairer_id`) exists; it is the
  **suggested** repairer for the movement.
- `assignment-unavailable` — no current assignment row for the machine (explicit state; **never
  an error, never a default repairer, never conflated with a missing repairer**); the operator
  selects the final repairer manually (§21).

### 3.5 Result/refusal vocabulary (P2-T07 transport tokens — full set in §12)

```text
validation-failed (400)   stale-version (409)              saida-exceeds-available (409)
irreparavel-exceeds-in-repair (409)   active-aggregate-exists (409)   already-closed (409)
not-closed (409)          not-last-closed (409)            aggregate-closed (409)
only-one-inicio (409)
not-found (404)           permission-denied (403)
```

---

## 4. Existing identities consumed

| Identity | Consumed for | Source / authority | Created by P2-T07? |
|---|---|---|---|
| `tool_id` | the **canonical registered BQ Tool** — standalone aggregate anchor; Tool facts (reference/lot/processo/machines) for search, display and History traversal | P2-T04 `tools` (CLOSED); `tool_type` CHECK `CM|MF|BQ` | **NO** — never minted, never duplicated (shared orchestration returns it; the aggregate only references it) |
| `bq_id` | the **frozen Job On BQ context** — production-linked aggregate anchor; frozen `tool_type='BQ'`/`tool_reference`/`tool_lot` triple + `jobon_id` for traversal and the contextual panel | P2-T04 `bq_contexts` (CLOSED); created **only by Job On's own application contract** (`IJobOnService`, BQ-slot Set — §22.3) | **NO** (P2-T07 may trigger its creation through `IJobOnService`, exactly like P2-T05 route 11 creates CM contexts; it never inserts `bq_contexts` rows itself) |
| `jobon_id` | production occurrence behind a production-linked aggregate (traversal: reference, production number, machine); read for the opening flow and the contextual panel | P2-T04 `job_ons` (CLOSED) | **NO** — no `jobon_id` column on any P2-T07 table (reached through `bq_id`) |
| `user_id` | backend-authored actor facts: aggregate `created_by_user_id`, movement `recorded_by_user_id`, audit `edited_by_user_id`, snapshot `closed_by_user_id`, reopen `reopened_by_user_id` | foundation `users`; `ICurrentAccountContext` | **NO** (FK references) |
| `repairer_id` | the canonical repairer register entry stored on **every external Saída** (final selected value, historically preserved) | P2-T05 `repairers` (CLOSED); FK `RESTRICT` | **NO** — consumed; the register is never administered here (§21) |
| machine | closed code value `B1|B2|B3|C1|C2|C3` — aggregate machine set and per-movement line context | P2-T04 `MachineCode` convention; P2-T05 §16.4 CHECK | **NO identity** — no machine registry, no `machine_id` (`…DELTA.md` §4.4) |

**No duplicate Tool/Job On/BQ context identities; no `production_id`; no `job_on_revision_id`;
no `machine_id`; no per-piece BQ identity; no reverse-ID arrays; no client-generated id of any
kind.**

---

## 5. New identities genuinely required

Exactly **three** persistence identities:

```text
boquilhas_id          — one collective BQ external-repair aggregate (one repair trace)
movement_id           — one quantity movement/event on one aggregate
boquilha_machine_id, movement_audit_id, close_snapshot_id, reopen_id
                      — child-row identities of the aggregate/movement/close/reopen structures
```

- `boquilhas_id`: backend-allocated uuid inside the create transaction (DB default
  `gen_random_uuid()` remains the foundation column convention only, never the application
  source); durable and visible at COMMIT (§10).
- `movement_id`: backend-allocated uuid inside the append transaction.
- `boquilhas_id` is **not** `bq_id`, **not** `tool_id`, **not** a physical BQ piece
  (`IDENTITIES_AND_RELATIONSHIPS.md`).
- No `production_id`, no `job_on_revision_id`, no aggregate revision id, no `movement_revision_id`,
  no edit-event id that re-identifies a quantity event (the audit id identifies **audit** rows,
  never quantity events — §19), no document id, no send id.

---

## 6. Physical persistence schema

Engine: **PostgreSQL** (Supabase TEST runtime target). Conventions are exactly the accepted
foundation conventions (P2-T04 §3; P2-T05 §16; P2-T06 §6): explicit snake_case; PK `uuid` +
`gen_random_uuid()` default, application-allocated before the transaction; `timestamp with time
zone` + `now()`; **every FK `ON DELETE RESTRICT`**; CHECK constraints for closed value sets and
non-blank text (`btrim(x) <> ''`); one `EntityTypeConfiguration` per entity discovered by
`ApplyConfigurationsFromAssembly`; `_context.Set<TEntity>()` repositories; `DmoDbContext.cs`
byte-identical.

**Six tables are created. No seventh table is created** — in particular no machine registry, no
balance table, no aggregate-summary cache, no assignment history, no document table, no generic
audit table, no reverse-array column.

### 6.1 `boquilhas` — the aggregate

| Column | Type | Null | Contract |
|---|---|---|---|
| `boquilhas_id` | `uuid` | NO | PK; default `gen_random_uuid()`; application-allocated |
| `bq_id` | `uuid` | YES | FK → `bq_contexts(bq_id)` `RESTRICT`; production-linked anchor (real frozen Job On BQ context) |
| `tool_id` | `uuid` | YES | FK → `tools(tool_id)` `RESTRICT`; standalone anchor (canonical BQ Tool) |
| `status` | `text` | NO | CHECK `status IN ('active','closed')` (§3.2) |
| `opening_date` | `date` | NO | editable business date of the repair-flow register; default today at creation (§20/§23) |
| `utilisation_percent` | `numeric(5,2)` | YES | CHECK `NULL OR (>= 0 AND <= 100)`; **manual still only** — never derived (§23, S13) |
| `observations` | `text` | YES | compact opening observations; CHECK `NULL OR btrim(observations) <> ''` |
| `created_by_user_id` | `uuid` | NO | FK → `users(user_id)` `RESTRICT`; backend-authored opening actor |
| `version` | `integer` | NO | default `1`; `.IsConcurrencyToken()` (§11) |
| `created_at` | `timestamptz` | NO | default `now()` |
| `updated_at` | `timestamptz` | NO | default `now()` |

- Anchor exclusivity: `CHECK ((bq_id IS NULL)::int + (tool_id IS NULL)::int = 1)` — a
  Boquilhas aggregate always anchors to **exactly one** of the two real relations; never two,
  never none (mirrors the accepted P2-T05 `pesos` anchor CHECK; AC-I7).
- Deliberately absent: `reference`/`lot` copy (S9 — reached through the anchor), `jobon_id`
  column, `machine`/`linha` column (machine set is a child table of §6.2), `linha_atual`
  (S10), balance columns (never stored — §18), close/reopen columns (close snapshot + reopen
  records are child rows of §6.4/§6.6), repairer (repairers attach to movements), `% utilização`
  derivations, any `production_id`/`job_on_revision_id`/`machine_id`.

### 6.2 `boquilha_machines` — registered machine/line context of the trace

| Column | Type | Null | Contract |
|---|---|---|---|
| `boquilha_machine_id` | `uuid` | NO | PK; default `gen_random_uuid()` |
| `boquilhas_id` | `uuid` | NO | FK → `boquilhas(boquilhas_id)` `RESTRICT` |
| `machine` | `text` | NO | CHECK `machine IN ('B1','B2','B3','C1','C2','C3')` |

- `UNIQUE (boquilhas_id, machine)`; **one or more rows are required for every aggregate**
  (validator `MACHINES_REQUIRED`; "Máquina(s)/Linha(s) … at least one required", global §5).
- Registered machine/line is **search/filter context, never identity** (global §5); no
  grouping column, no `is_primary`, no cascade to movements (movement `machine` facts are
  independent frozen facts, §17).
- No `version` (the aggregate's version protects the set — parent-protects-child precedent).

### 6.3 `boquilha_movements` — the movement/quantity event ledger

| Column | Type | Null | Contract |
|---|---|---|---|
| `movement_id` | `uuid` | NO | PK; default `gen_random_uuid()`; application-allocated |
| `boquilhas_id` | `uuid` | NO | FK → `boquilhas(boquilhas_id)` `RESTRICT` |
| `movement_type` | `text` | NO | CHECK `movement_type IN ('inicio','saida','entrada','irreparavel')` — the closed §3.1 set |
| `quantity` | `integer` | NO | CHECK `quantity > 0`; whole-unit BQ count; **no non-positive quantity is ever stored** (validator `QUANTITY_NOT_POSITIVE` first; DB backstop) |
| `business_date` | `date` | NO | the date the physical/operational movement happened; **operator-editable** (§20) |
| `recorded_at` | `timestamptz` | NO | default `now()`; **immutable system receipt timestamp — never rewritten, never edited** (§20) |
| `recorded_by_user_id` | `uuid` | NO | FK → `users(user_id)` `RESTRICT`; backend-authored recording actor (unchanged by later edits) |
| `machine` | `text` | YES | CHECK `machine IS NULL OR machine IN ('B1','B2','B3','C1','C2','C3')`; line context "where recorded" (global §6); **required on external Saída** (§21, S3) |
| `repairer_id` | `uuid` | YES | FK → `repairers(repairer_id)` `RESTRICT`; the **final selected** canonical repairer; **required on external Saída** (§21); historically preserved — a later assignment change never rewrites this column |
| `expected_return_quantity` | `integer` | YES | Entrada fact (S6, Q-EXCESS): expected return = `GREATEST(0, Em reparação before this Entrada)` computed by replay inside the append transaction; NOT NULL iff `movement_type='entrada'` |
| `excess_received_quantity` | `integer` | YES | Entrada fact: `GREATEST(0, quantity − expected_return_quantity)`; NOT NULL iff `movement_type='entrada'`; CHECK-consistent with the expected/quantity values |
| `observations` | `text` | YES | motive/detail/observations "where recorded" (global §6); CHECK `NULL OR btrim(observations) <> ''` |
| `version` | `integer` | NO | default `1`; `.IsConcurrencyToken()` — the per-movement edit guard (§11/§19) |
| `created_at` | `timestamptz` | NO | default `now()` |
| `updated_at` | `timestamptz` | NO | default `now()` (edited rows only) |

Exact CHECKs (§7.4):

```text
boquilha_movements_saida_required_check:
   NOT (movement_type = 'saida' AND (machine IS NULL OR repairer_id IS NULL))
boquilha_movements_entrada_facts_check:
   (movement_type = 'entrada' AND expected_return_quantity IS NOT NULL
      AND excess_received_quantity IS NOT NULL AND expected_return_quantity >= 0
      AND excess_received_quantity = GREATEST(0, quantity - expected_return_quantity))
   OR (movement_type <> 'entrada' AND expected_return_quantity IS NULL
      AND excess_received_quantity IS NULL)
```

- **No `annulled`/`deleted` column and no delete path** (S12): the ledger is append-only as a
  fact set; edits replace the current values of the **same row** (§19).
- The movement row is **the** single quantity event: editing never inserts a second row (E3).

### 6.4 `boquilha_movement_audit` — the edit/audit history of movements

| Column | Type | Null | Contract |
|---|---|---|---|
| `movement_audit_id` | `uuid` | NO | PK; default `gen_random_uuid()` |
| `movement_id` | `uuid` | NO | FK → `boquilha_movements(movement_id)` `RESTRICT` |
| `edited_by_user_id` | `uuid` | NO | FK → `users(user_id)` `RESTRICT`; **backend-authored** editor (`ICurrentAccountContext`) — never client-supplied |
| `edited_at` | `timestamptz` | NO | backend clock; **backend-authored** — the authoritative edit timestamp |
| `before_quantity` / `after_quantity` | `integer` | NO | CHECK `> 0` both (every recorded movement value is positive) |
| `before_business_date` / `after_business_date` | `date` | NO | before/after of the editable business date |
| `before_machine` / `after_machine` | `text` | YES | before/after line context; NULL = absent |
| `before_repairer_id` / `after_repairer_id` | `uuid` | YES | before/after final repairer |
| `before_observations` / `after_observations` | `text` | YES | before/after observations (NULL = absent) |
| `created_at` | `timestamptz` | NO | default `now()` |

- **One row per edit of a movement**, written **in the same transaction** as the movement
  UPDATE (§10). It is **audit history, not a quantity event** (`RECORD_LIFECYCLES.md` §9;
  global §6): it has no quantity-balance effect, no CHECK membership in the balance repertoire,
  and is never rendered as a movement row (AC-E3/E4).
- **Append-only by construction**: no UPDATE/DELETE route, member or SQL exists for this table
  (AC-MG3 discipline, P2-T06 precedent).
- No `movement_type` before/after columns: movement_type is immutable (§19, S4) — nothing to
  audit.

### 6.5 `boquilha_close_snapshots` — the immutable close snapshot

| Column | Type | Null | Contract |
|---|---|---|---|
| `close_snapshot_id` | `uuid` | NO | PK; default `gen_random_uuid()` |
| `boquilhas_id` | `uuid` | NO | FK → `boquilhas(boquilhas_id)` `RESTRICT` — the **same** aggregate (no replacement identity) |
| `closed_by_user_id` | `uuid` | NO | FK → `users(user_id)` `RESTRICT`; backend-authored closing user |
| `closed_at` | `timestamptz` | NO | backend clock; the closing date/time (S11, Q-CLOSE-DATE) |
| `initial_quantity` | `integer` | NO | the aggregate's Início quantity at close (`Σ(início)`) |
| `opening_date` | `date` | NO | the aggregate's opening business date at close |
| `disponivel` | `integer` | NO | derived bucket at close (replay; may be any integer) |
| `em_reparacao` | `integer` | NO | derived bucket at close (may be negative — valid projection) |
| `irreparavel` | `integer` | NO | derived bucket at close |
| `entrada_excecional` | `integer` | NO | accumulated excess at close; CHECK `>= 0` |
| `utilisation_percent` | `numeric(5,2)` | YES | the **manual still** as of close (copy of the current value at close time); CHECK range |
| `created_at` | `timestamptz` | NO | default `now()` |

- **Immutable after COMMIT**: no UPDATE/DELETE path (AC-C1); future repairer/line/opening-fact/
  movement changes never modify a closed snapshot (global §7).
- One snapshot per close event; a reopen → close cycle writes a **new** snapshot row (history of
  closes per aggregate; AC-C8).
- The snapshot is a **frozen factual summary at close**, not a balance authority: the live
  balance continues to derive from movement facts (and, after reopen, continues to change);
  the snapshot never feeds any derivation (AC-B1).

### 6.6 `boquilha_reopenings` — reopen records (append-only history)

| Column | Type | Null | Contract |
|---|---|---|---|
| `reopen_id` | `uuid` | NO | PK; default `gen_random_uuid()` |
| `boquilhas_id` | `uuid` | NO | FK → `boquilhas(boquilhas_id)` `RESTRICT` — the **same** aggregate |
| `close_snapshot_id` | `uuid` | NO | FK → `boquilha_close_snapshots(close_snapshot_id)` `RESTRICT`; the exact close being reopened (the last close — §23 eligibility) |
| `reopened_by_user_id` | `uuid` | NO | FK → `users(user_id)` `RESTRICT`; backend-authored actor |
| `reopened_at` | `timestamptz` | NO | backend clock |
| `reason` | `text` | NO | CHECK `btrim(reason) <> ''` — **required** (validator `REOPEN_REASON_REQUIRED` first; DB backstop) |
| `created_at` | `timestamptz` | NO | default `now()` |

- Append-only (no UPDATE/DELETE path); reopen records are history, never a movement type
  (`RECORD_LIFECYCLES.md` §9: "Close and reopen are aggregate lifecycle facts, not movement
  types").

---

## 7. Keys / constraints / indexes

### 7.1 Primary keys

```text
PK_boquilhas                     (boquilhas_id)
PK_boquilha_machines             (boquilha_machine_id)
PK_boquilha_movements            (movement_id)
PK_boquilha_movement_audit       (movement_audit_id)
PK_boquilha_close_snapshots      (close_snapshot_id)
PK_boquilha_reopenings           (reopen_id)
```

### 7.2 Unique constraints (unique indexes, accepted foundation style)

| Name | Table | Tuple / predicate | Meaning |
|---|---|---|---|
| `boquilha_machines_boquilhas_machine_key` | `boquilha_machines` | (`boquilhas_id`, `machine`) | one row per machine per aggregate |
| `IX_boquilhas_active_bq_id` | `boquilhas` | (`bq_id`) **PARTIAL**: `WHERE status = 'active' AND bq_id IS NOT NULL` | **at most one ACTIVE aggregate per production-linked anchor** (`bq_id`) — the race-safe database backstop of the one-active rule (Q-CREATE/Q-REOPEN-ELIG; §11.2, §22.2, §23.3) |
| `IX_boquilhas_active_tool_id` | `boquilhas` | (`tool_id`) **PARTIAL**: `WHERE status = 'active' AND tool_id IS NOT NULL` | **at most one ACTIVE aggregate per standalone anchor** (`tool_id`) — the same backstop |

These are the **only** business unique tuples. The rationale for the two partial tuples versus
the rejected unconditional one:

- No **unconditional** "one aggregate per anchor" tuple exists: multiple traces over time are
  legitimate (a closed trace never blocks a later one — §23), so `bq_id`/`tool_id` alone are
  deliberately **not** unique.
- The **active-only** partial tuples above are the exact Q-CREATE/Q-REOPEN-ELIG invariant
  ("one active aggregate at a time per BQ Tool context", global §7; §1.3) made structural:
  closed rows are unconstrained, active rows collide on the same anchor.
- The anchor-exclusivity CHECK (`boquilhas_anchor_exclusive_check`, §7.4) guarantees a row can
  satisfy **at most one** of the two partial predicates (production-linked = `bq_id` non-NULL /
  `tool_id` NULL; standalone = the reverse), so the two indexes serialize active-aggregate
  creation **independently per truthful anchor type** — never cross-talk between the flows.

**Concurrency authority (B1 correction; Architect PLAN review `542a08a1…`).** The application
pre-check (`HasActiveAggregateForAnchorAsync` inside the create/reopen transactions, §22.2/
§23.3) remains the **normal-path refusal for UX, but is NOT the concurrency authority**. The
database partial unique indexes are the race-safe invariant backstop: if a competing create (or
reopen) commits an active aggregate on the same anchor between the pre-check and this
transaction's COMMIT, the database raises `23505 unique_violation` on the relevant index, the
**entire transaction rolls back**, and the repository maps it to the **same**
`Refused(ActiveAggregateExists)` — no partial aggregate, no orphan Início, no 500.

**23505 mapping (exact — B1 correction).** PostgreSQL `23505 unique_violation`, when caused by
`IX_boquilhas_active_bq_id` **or** `IX_boquilhas_active_tool_id`, maps in the
repository/infrastructure layer to `Refused(ActiveAggregateExists)` (409), distinguished by the
reported constraint/index identity (`PostgresException.SqlState` + constraint name, the
accepted `TryMapConstraintViolation` pattern — §8.2 binding rules). **No other 23505 source is
mapped to this domain result**: the mapping is scoped to exactly these two index identities,
and no generic catch-all 23505 mapping is contracted.

### 7.3 Foreign keys (all `ON DELETE RESTRICT`)

| FK | From | To |
|---|---|---|
| `FK_boquilhas_bq_contexts_bq_id` | `boquilhas.bq_id` | `bq_contexts(bq_id)` |
| `FK_boquilhas_tools_tool_id` | `boquilhas.tool_id` | `tools(tool_id)` |
| `FK_boquilhas_users_created_by_user_id` | `boquilhas.created_by_user_id` | `users(user_id)` |
| `FK_boquilha_machines_boquilhas_boquilhas_id` | `boquilha_machines.boquilhas_id` | `boquilhas(boquilhas_id)` |
| `FK_boquilha_movements_boquilhas_boquilhas_id` | `boquilha_movements.boquilhas_id` | `boquilhas(boquilhas_id)` |
| `FK_boquilha_movements_repairers_repairer_id` | `boquilha_movements.repairer_id` | `repairers(repairer_id)` |
| `FK_boquilha_movements_users_recorded_by_user_id` | `boquilha_movements.recorded_by_user_id` | `users(user_id)` |
| `FK_boquilha_movement_audit_boquilha_movements_movement_id` | `boquilha_movement_audit.movement_id` | `boquilha_movements(movement_id)` |
| `FK_boquilha_movement_audit_users_edited_by_user_id` | `boquilha_movement_audit.edited_by_user_id` | `users(user_id)` |
| `FK_boquilha_close_snapshots_boquilhas_boquilhas_id` | `boquilha_close_snapshots.boquilhas_id` | `boquilhas(boquilhas_id)` |
| `FK_boquilha_close_snapshots_users_closed_by_user_id` | `boquilha_close_snapshots.closed_by_user_id` | `users(user_id)` |
| `FK_boquilha_reopenings_boquilhas_boquilhas_id` | `boquilha_reopenings.boquilhas_id` | `boquilhas(boquilhas_id)` |
| `FK_boquilha_reopenings_boquilha_close_snapshots_close_snapshot_id` | `boquilha_reopenings.close_snapshot_id` | `boquilha_close_snapshots(close_snapshot_id)` |
| `FK_boquilha_reopenings_users_reopened_by_user_id` | `boquilha_reopenings.reopened_by_user_id` | `users(user_id)` |

- **No cascade anywhere.** Deleting a `bq_contexts` row (a Job On context removal), a `tools`
  row or a `repairers` row referenced by Boquilhas fails closed at the database; the application
  refuses first through registered probes and the `dependency-exists` vocabulary of P2-T04
  (§27).

### 7.4 CHECK constraints (exact)

| Name | Table | Expression |
|---|---|---|
| `boquilhas_anchor_exclusive_check` | `boquilhas` | `((bq_id IS NULL)::int + (tool_id IS NULL)::int) = 1` |
| `boquilhas_status_check` | `boquilhas` | `status IN ('active','closed')` |
| `boquilhas_utilisation_range_check` | `boquilhas` | `utilisation_percent IS NULL OR (utilisation_percent >= 0 AND utilisation_percent <= 100)` |
| `boquilhas_observations_check` | `boquilhas` | `observations IS NULL OR btrim(observations) <> ''` |
| `boquilha_machines_machine_check` | `boquilha_machines` | `machine IN ('B1','B2','B3','C1','C2','C3')` |
| `boquilha_movements_type_check` | `boquilha_movements` | `movement_type IN ('inicio','saida','entrada','irreparavel')` |
| `boquilha_movements_quantity_check` | `boquilha_movements` | `quantity > 0` |
| `boquilha_movements_machine_check` | `boquilha_movements` | `machine IS NULL OR machine IN ('B1','B2','B3','C1','C2','C3')` |
| `boquilha_movements_saida_required_check` | `boquilha_movements` | `NOT (movement_type = 'saida' AND (machine IS NULL OR repairer_id IS NULL))` |
| `boquilha_movements_entrada_facts_check` | `boquilha_movements` | (exact two-branch expression of §6.3) |
| `boquilha_movements_observations_check` | `boquilha_movements` | `observations IS NULL OR btrim(observations) <> ''` |
| `boquilha_movements_version_check` | `boquilha_movements` | `version >= 1` |
| `boquilha_movement_audit_quantities_check` | `boquilha_movement_audit` | `before_quantity > 0 AND after_quantity > 0` |
| `boquilha_close_snapshots_entrada_excecional_check` | `boquilha_close_snapshots` | `entrada_excecional >= 0` |
| `boquilha_close_snapshots_utilisation_check` | `boquilha_close_snapshots` | `utilisation_percent IS NULL OR (utilisation_percent >= 0 AND utilisation_percent <= 100)` |
| `boquilha_reopenings_reason_required_check` | `boquilha_reopenings` | `btrim(reason) <> ''` |

(Validators raise the typed tokens first; the CHECKs are the accepted database backstop mapped via
`PostgresException.SqlState` 23514 — never a 500.)

### 7.5 Indexes (each justified by a contracted query)

| Index | Table | Columns | Unique | Justified by |
|---|---|---|---|---|
| `IX_boquilhas_status` | `boquilhas` | `status` | NO | the Registo lot-grid predicate (`status = 'active'`, default) and the Histórico "aggregate/file state" filter (§24) |
| `IX_boquilhas_bq_id` | `boquilhas` | `bq_id` | NO | FK-supporting + production-linked History traversal and the reopen eligibility anchor query (§23) |
| `IX_boquilhas_tool_id` | `boquilhas` | `tool_id` | NO | FK-supporting + standalone History traversal and the reopen eligibility anchor query (§23) |
| `IX_boquilha_movements_boquilhas_id` | `boquilha_movements` | `boquilhas_id`, `recorded_at`, `movement_id` | NO | FK-supporting + the ledger replay order (`recorded_at ASC, movement_id ASC` — §18) and per-aggregate movement reads |
| `IX_boquilha_movement_audit_movement_id` | `boquilha_movement_audit` | `movement_id`, `edited_at` | NO | FK-supporting + the per-movement trail read (§19) |
| `IX_boquilha_close_snapshots_boquilhas_id` | `boquilha_close_snapshots` | `boquilhas_id`, `closed_at` | NO | FK-supporting + "last close" determination (§23) |
| `IX_boquilha_reopenings_boquilhas_id` | `boquilha_reopenings` | `boquilhas_id`, `reopened_at` | NO | FK-supporting + reopen history read (§23) |
| `IX_boquilhas_active_bq_id` | `boquilhas` | `bq_id` | YES (partial) | **invariant backstop, not a query index** — at most one ACTIVE aggregate per `bq_id` (§7.2; race-free create/reopen enforcement, §11.2/§22.2/§23.3) |
| `IX_boquilhas_active_tool_id` | `boquilhas` | `tool_id` | YES (partial) | **invariant backstop, not a query index** — at most one ACTIVE aggregate per `tool_id` (§7.2; race-free create/reopen enforcement, §11.2/§22.2/§23.3) |

The two partial unique indexes are declared in §7.2 (semantic register) and listed here only for
the physical index register; they exist to enforce the one-active invariant, not to serve a read
predicate. No other index is contracted. In particular no speculative index is added for Histórico
filter combinations (reference/lot traversal, movement type, repairer, business-date range): those are
predicate-bound registry queries over small aggregates and no authority justifies speculative
indexes (P2-T04 Q12 / P2-T05 §17.5 / P2-T06 §7.3 stance). `boquilha_machines`
(`UNIQUE (boquilhas_id, machine)`) serves its own FK support.

---

## 8. Repository contracts

### 8.1 Layer placement (accepted conventions only)

| Layer | Location |
|---|---|
| domain types | `src/DMO.Domain/Boquilhas/` — `BoquilhasId`, `MovementId`, `MovementKind` (the four tokens), `BoquilhaAggregate`, `BoquilhaMovement`, `MovementAuditEntry`, `CloseSnapshot`, `ReopeningRecord`, `BalanceProjection`; no new dependency |
| repository contract | `src/DMO.Application/Repositories/IBoquilhasRepository.cs` — **new**; consumed P2-T04/P2-T05 repositories (`IJobOnRepository`/`IJobOnService`, `IToolRepository`/`IToolService` reads, `IRepairerRepository` read, `IMachineRepairerAssignmentRepository` read) gain **no** member |
| application area | `src/DMO.Application/Boquilhas/` — `BoquilhasModels.cs`, `BoquilhasValidator.cs`, `IBoquilhasService.cs`, `BoquilhasService.cs`, `BoquilhasReadModels.cs` |
| persistence | `src/DMO.Infrastructure/Persistence/` — `Entities/BoquilhaEntity.cs` (+ Machine/Movement/Audit/CloseSnapshot/Reopening entities), `EntityConfigurations/*`, `BoquilhasRepository.cs`, `Migrations/<timestamp>_BoquilhasDomain.cs` (+ Designer + EF snapshot extension) |
| Web | `src/DMO.Web/Endpoints/BoquilhasEndpoints.cs`, `src/DMO.Web/Pages/Boquilhas/` (`Index.cshtml(.cs)` Registo, `Novo.cshtml(.cs)`, `Historico.cshtml(.cs)`), `BoquilhasPolicyNames.cs`, `wwwroot/css/dmo-boquilhas.css`, `wwwroot/js/dmo-boquilhas.js` |

No new .NET project; no generic repository abstraction, no unit-of-work, no mediator, no CQRS,
no second `DbContext` (accepted discipline, `docs/ARCHITECTURE.md`).

### 8.2 `IBoquilhasRepository` (exact)

```csharp
namespace DMO.Application.Repositories;

public interface IBoquilhasRepository
{
    // Aggregate + machines + movements (ledger order) + close snapshots + reopenings, tracked.
    Task<BoquilhaAggregate?> GetByIdAsync(Guid boquilhasId, CancellationToken cancellationToken);

    // Registo lot grid: predicate + filters applied in SQL, deterministic ordering, one page (§23/§24).
    Task<IReadOnlyList<BoquilhaListItem>> ListAsync(BoquilhasListQuery query,
        CancellationToken cancellationToken);

    // Local Histórico: predicates + filters applied in SQL, one page (§24).
    Task<IReadOnlyList<HistoryItem>> GetHistoryAsync(BoquilhasHistoryQuery query,
        CancellationToken cancellationToken);

    // Reopen eligibility anchor scan: does another ACTIVE aggregate share this anchor?
    Task<bool> HasActiveAggregateForAnchorAsync(Guid? bqId, Guid? toolId, Guid excludeBoquilhasId,
        CancellationToken cancellationToken);

    // The most recent close snapshot across all aggregates sharing this anchor (reopen eligibility).
    Task<Guid?> GetLastCloseSnapshotIdForAnchorAsync(Guid? bqId, Guid? toolId,
        CancellationToken cancellationToken);

    // The per-movement edit/audit trail, edited_at ASC.
    Task<IReadOnlyList<MovementAuditEntry>> GetMovementAuditAsync(Guid movementId,
        CancellationToken cancellationToken);

    // Create: ONE transaction — aggregate + machine rows + the Início movement (§10).
    Task<BoquilhaAggregate> CreatedAsync(BoqCreateUnit unit, CancellationToken cancellationToken);

    // Append: ONE transaction — balance-relative validation + the single movement row (§10/§17).
    Task<BoquilhaMovement> AppendMovementAsync(BoqAppendUnit unit, CancellationToken cancellationToken);

    // Edit: ONE transaction — guarded UPDATE of the same row + the audit row (§10/§19).
    Task<BoqEditResult> EditMovementAsync(BoqEditUnit unit, CancellationToken cancellationToken);

    // Close: ONE transaction — status + version + immutable snapshot row (§10/§23).
    Task<BoqCloseResult> CloseAsync(BoqCloseUnit unit, CancellationToken cancellationToken);

    // Reopen: ONE transaction — status + version + reopen row (§10/§23).
    Task<BoqReopenResult> ReopenAsync(BoqReopenUnit unit, CancellationToken cancellationToken);

    // Opening-facts: ONE transaction — aggregate row + machine-set replace (§10/§23).
    Task<BoquilhaAggregate> UpdateOpeningFactsAsync(BoqOpeningFactsUnit unit,
        CancellationToken cancellationToken);
}
```

Binding rules (same as P2-T04 §12.2 / P2-T05 §20.2 / P2-T06 §8.2): `CancellationToken` mandatory
last; `Task<T?>` single reads / `Task<IReadOnlyList<T>>` lists / `Task<T>` writes; the write
opens its own transaction; private static `Project(...)` mapping; constraint violations mapped
via `PostgresException.SqlState` + constraint name (23514 → the same validator token, never a
500; **23505 on `IX_boquilhas_active_bq_id`/`IX_boquilhas_active_tool_id` →
`Refused(ActiveAggregateExists)` — the exact scoped mapping of §7.2, with no other 23505 source
mapped to this domain result**); repositories own no domain rule beyond the accepted unit shapes;
`BoquilhasService` owns the balance-relative validation by composing repository reads (single
replay helper, §18).

**Where the consumed reads come from:** machine assignment reads use the **closed P2-T05**
`IMachineRepairerAssignmentRepository` (`GetByMachineAsync`/`ListAsync` — read-only, no member
added); the register read uses the closed `IRepairerRepository` list read; Tool/Job On reads use
the **closed P2-T04** `IToolRepository`/`IJobOnRepository` application reads through
`IToolService`/`IJobOnService`; the BQ-context creation composes `IJobOnService.UpdateAsync`
(BQ-slot Set) exactly like P2-T05's route 11 composes the CM-slot Set (§22.3). **No module reads
another module's tables** (accepted blocker rule): P2-T07's repository touches only P2-T07's six
tables + consumed application contracts.

---

## 9. Application service contracts

### 9.1 `IBoquilhasService` (exact)

```csharp
namespace DMO.Application.Boquilhas;

public interface IBoquilhasService
{
    Task<BoquilhasResult> GetListAsync(BoquilhasListQuery query, CancellationToken cancellationToken);
    Task<BoquilhasResult> GetHistoryAsync(BoquilhasHistoryQuery query, CancellationToken cancellationToken);
    Task<BoquilhasResult> GetAsync(Guid boquilhasId, CancellationToken cancellationToken);
    Task<BoquilhasResult> GetMovementAuditAsync(Guid boquilhasId, Guid movementId, CancellationToken cancellationToken);
    Task<BoquilhasResult> CreateAsync(CreateBoquilhasCommand command, CancellationToken cancellationToken);
    Task<BoquilhasResult> AppendMovementAsync(AppendMovementCommand command, CancellationToken cancellationToken);
    Task<BoquilhasResult> EditMovementAsync(EditMovementCommand command, CancellationToken cancellationToken);
    Task<BoquilhasResult> CloseAsync(CloseBoquilhasCommand command, CancellationToken cancellationToken);
    Task<BoquilhasResult> ReopenAsync(ReopenBoquilhasCommand command, CancellationToken cancellationToken);
    Task<BoquilhasResult> UpdateOpeningFactsAsync(UpdateOpeningFactsCommand command, CancellationToken cancellationToken);
    Task<BoquilhasResult> GetMachineAssignmentsAsync(CancellationToken cancellationToken);   // consumed read
    Task<BoquilhasResult> GetRepairersAsync(CancellationToken cancellationToken);            // consumed read
    Task<BoquilhasResult> FindProductionsAsync(FindProductionsQuery query, CancellationToken cancellationToken); // IJobOnService
    Task<BoquilhasResult> GetJobOnAsync(Guid jobOnId, CancellationToken cancellationToken);  // IJobOnService
    Task<BoquilhasResult> AssociateBqAsync(AssociateBqCommand command, CancellationToken cancellationToken);   // IJobOnService Set
}
```

`BoquilhasService` composes:

- `IBoquilhasRepository` — all P2-T07 persistence;
- `IJobOnService` — productions, ficha, `UpdateAsync` (BQ-slot Set) — the **only** paths to Job
  On/`bq_contexts` (AC-I3/I5);
- `IToolService` (read) — Tool existence/type for the standalone anchor and the shared
  orchestration backend validation (`TOOL_NOT_FOUND`/`TOOL_TYPE_MISMATCH`); Tool **search and
  create** stay on the **`ferramentas`-gated** P2-T04 routes via the shared orchestration (§16 —
  AC-T4);
- `IMachineRepairerAssignmentRepository` (read) + `IRepairerRepository` (read) — resolution +
  register consumption (§21);
- `ICurrentAccountContext` — every backend actor (created/recorded/edited/closed/reopened);
- `IClock`/`DateTimeOffset.UtcNow` — every backend timestamp.

Decision rules of the service (exact, in-transaction where they read persisted state):

1. **Balance-relative validation is computed by replay over the ledger inside the same
   transaction** (a single pure `BalanceProjection Replay(IReadOnlyList<BoquilhaMovement>)`
   helper; §18) — never from a stored total.
2. **Edit validation replays with the edited movement's current values excluded, then validates
   the new values as an append** (§19) — guaranteeing a single net event.
3. The Web layer never queries the database directly; the service never calls a P2-T05/P2-T04/
   P2-T06-gated HTTP route (no cross-module HTTP calls — P2-T06 §14 discipline).

### 9.2 Validators

`BoquilhasValidator` — pure static, runs before any write, returns the closed §12.2 code set:
`QUANTITY_NOT_POSITIVE`, `INITIAL_QUANTITY_REQUIRED`, `MOVEMENT_TYPE_INVALID`,
`MACHINES_REQUIRED`, `MACHINE_REQUIRED`, `MACHINE_UNKNOWN`, `MACHINE_NOT_IN_AGGREGATE`,
`REPAIRER_REQUIRED`, `REPAIRER_NOT_FOUND`, `ANCHOR_REQUIRED`, `ANCHOR_CONFLICT`,
`TOOL_NOT_FOUND`, `TOOL_TYPE_MISMATCH`, `BQ_CONTEXT_NOT_FOUND`, `UTILISATION_INVALID`,
`BUSINESS_DATE_INVALID`, `OBSERVATIONS_INVALID`, `REOPEN_REASON_REQUIRED`,
`REFERENCE_REQUIRED`, `FILTER_INVALID` (incl. page/pageSize out of bounds).

---

## 10. Transaction boundaries

All multi-row writes open their own transaction at the repository (`BeginTransactionAsync`,
accepted `TemplateRepository`/P2-T05 pattern). No ambient/unit-of-work, no second `DbContext`,
no isolation override, no advisory locks, no outbox.

| Operation | Transaction boundary | Atomic unit |
|---|---|---|
| Aggregate create (§22.4) | ONE transaction | `boquilhas` row + **all** `boquilha_machines` rows + the **Início** `boquilha_movements` row — all-or-nothing; application pre-check + the **partial unique index backstop** (`IX_boquilhas_active_bq_id`/`IX_boquilhas_active_tool_id`, §7.2): a competing create/reopen that commits between pre-check and COMMIT raises 23505 → whole transaction rolls back → `Refused(ActiveAggregateExists)` (§11, §22.2) |
| Movement append (§17) | ONE transaction | replay validation + the single `boquilha_movements` row (incl. Entrada expected/excess facts) — all-or-nothing |
| Movement edit (§19) | ONE transaction | guarded UPDATE of the **same** `movement_id` row (new current values + `version += 1` + `updated_at`) + the `boquilha_movement_audit` row — all-or-nothing; **no second movement row is ever inserted** |
| Close (§23) | ONE transaction | `boquilhas.status := 'closed'` + `version += 1` + the immutable `boquilha_close_snapshots` row (replay-computed buckets) — all-or-nothing |
| Reopen (§23) | ONE transaction | eligibility checks + `boquilhas.status := 'active'` + `version += 1` + the `boquilha_reopenings` row — all-or-nothing; the **partial unique index backstop** protects the one-active invariant at the database: if a concurrent create/reopen commits an active aggregate on the same anchor before this transaction's status UPDATE, the UPDATE raises 23505 → complete rollback → `Refused(ActiveAggregateExists)` — the aggregate remains `closed`, its close snapshot and all history intact, no partial reopening record (§11, §23.3) |
| Opening-facts update (§23) | ONE transaction | `boquilhas` row (opening_date/utilisation/observations) + machine-set replace (delete + insert child rows) — all-or-nothing |
| Reads (grid/ficha/history/audit/assignments/repairers/productions/jobon) | none — read-only | — |

Rules:

1. A forced mid-transaction failure leaves **zero** rows of the operation (no aggregate without
   its Início; no status flip without a snapshot; no movement update without its audit row) —
   test rows prove both orders (K3, C2). A 23505 on an active-anchor index in create/reopen is
   such a failure: zero rows of the operation survive (K6–K8).
2. After a successful COMMIT, a retry is a **new** attempt with the fresh observed version;
   after a rolled-back failure, a retry is a clean attempt. No client idempotency key is
   invented (P2-T04 Q17 stance).
3. **Immutable-by-construction rows:** `boquilha_movement_audit`, `boquilha_close_snapshots`
   and `boquilha_reopenings` have **no** UPDATE/DELETE route, repository member or SQL (AC-C1,
   AC-E2, AC-MG3).
4. `recorded_at` is written exactly once at movement insertion and is never touched by any
   later statement (§20).
5. **The race-safe invariant backstop uses no advisory lock, no `SERIALIZABLE` isolation, no
   explicit table lock, no lock table and no application mutex** (B1 correction, Architect
   review `542a08a1…`): the partial unique indexes of §7.2 plus the exact 23505 mapping are the
   entire mechanism; everything else in this section is unchanged.

---

## 11. Concurrency / version behavior

### 11.1 Mechanism (accepted, reused exactly)

- `boquilhas.version` and `boquilha_movements.version`: `integer NOT NULL DEFAULT 1`,
  `.IsConcurrencyToken()` (the accepted P2-T04 `job_ons.version` / P2-T05 `pesos.version`
  mechanism).
- Every guarded operation observes the version(s) in its carrier and: (1) in-transaction
  compare → `ConcurrencyConflictException` (domain type, `DMO.Application.Persistence`); (2) the
  EF concurrency token stays active through the **`SaveAsync` helper** mapping
  `DbUpdateConcurrencyException` via the accepted `ConcurrencyConflictExceptionMapping` — a
  save-time race surfaces as **409 `stale-version`**, never 500, never a silent overwrite.
- Services translate `ConcurrencyConflictException` → `Refused(StaleVersion)` → 409.
- Child rows (`boquilha_machines`, `boquilha_movement_audit`, `boquilha_close_snapshots`,
  `boquilha_reopenings`) carry no token: the parent's version protects their writes
  (accepted `template_modules`/P2-T04 §15.1 reasoning); audit/snapshot/reopen rows are
  immutable after COMMIT anyway.

### 11.2 Per-operation matrix

| Operation | Observed version(s) | On staleness | Rows written on staleness |
|---|---|---|---|
| Aggregate create | n/a (nothing pre-exists) — the anchor race is closed by the **application pre-check AND the partial unique index backstop** (§7.2): either the pre-check refuses or the database raises 23505 on `IX_boquilhas_active_bq_id`/`IX_boquilhas_active_tool_id`, both → `Refused(ActiveAggregateExists)` (409) | `Refused(ActiveAggregateExists)` (409) | 0 |
| Movement append | `expectedAggregateVersion` (protects against concurrent close/reopen) | `Refused(StaleVersion)` (409) | 0 |
| Movement edit | `expectedAggregateVersion` + `expectedMovementVersion` | `Refused(StaleVersion)` (409) | 0 |
| Close | `expectedAggregateVersion` | `Refused(StaleVersion)` (409) | 0 |
| Reopen | `expectedAggregateVersion` + the **active-anchor index backstop** (a concurrent create/reopen that commits another active aggregate on the same anchor makes the status UPDATE raise 23505 → `Refused(ActiveAggregateExists)`, complete rollback — §23.3) | `Refused(StaleVersion)` (409) or `Refused(ActiveAggregateExists)` (409) | 0 |
| Opening-facts update | `expectedAggregateVersion` | `Refused(StaleVersion)` (409) | 0 |

### 11.3 Rules

**No silent overwrite, no automatic merge, no automatic retry.** A refused operation reports the
typed reason; the surface enters the accepted **`conflict` presentation with an explicit reload
recovery** (`renderConflict`, the D2 pattern shipped by P2-T05: clear heading — "Conflito — os
dados foram alterados por outra ação; nada foi guardado." — typed server message, one explicit
"Recarregar estado atual" action; the observed version refreshes **only** on success). Reads
never bump. Versions increment exactly once per committed mutation (create starts at 1; each
guarded mutation adds 1).

**One-active-anchor invariant (B1 correction; Architect review `542a08a1…`).** The partial
unique indexes of §7.2 are the race-safe database backstop of the one-active-aggregate-per-anchor
rule: a `23505 unique_violation` on either index — produced by a create or a reopen racing a
concurrent create/reopen that committed first — rolls back the **entire** transaction and maps to
`Refused(ActiveAggregateExists)` via the exact §7.2 mapping (never a 500, never a partial
aggregate, never an orphan Início; a failed reopen preserves the `closed` state, the close
snapshot and all history). The application pre-check remains the normal-path refusal and is not
the concurrency authority. No advisory lock, no `SERIALIZABLE` isolation, no explicit table lock
and no lock table is introduced (§10 rule 5).

### 11.4 Interaction of the two version scopes

- Movement append/edit asserts the **aggregate version** because close/reopen mutate the
  aggregate row; a close that commits first bumps the aggregate version, so a concurrent
  append/edit fails `stale-version` (no movement lands on a closed trace). An append/edit that
  commits first is simply included by the later close (the close replays the committed ledger
  inside its transaction — `aggregate-closed` guards are re-asserted in-transaction).
- Movement edit additionally asserts the **movement version**: two concurrent editors of the
  same movement — the second fails `stale-version`.
- Reopen/close guard against each other and against opening-fact updates through the aggregate
  version; the eligibility anchor scans (§23) run inside the transaction.
- **Create-vs-create and create-vs-reopen on the same anchor** have no pre-existing version to
  observe (create) or a version that cannot see the other's uncommitted row — they are closed by
  the §7.2 partial unique indexes: whoever commits the active row first wins (create: INSERT
  succeeds, or the reopen's status UPDATE succeeds); the loser's write raises 23505 → the exact
  mapping → `Refused(ActiveAggregateExists)` (K6–K8).

---

## 12. Result / error vocabulary

### 12.1 Result unions (closed sets)

```csharp
namespace DMO.Application.Boquilhas;

public abstract record BoquilhasResult
{
    public sealed record ListFound(IReadOnlyList<BoquilhaListItemReadModel> Rows, int Total) : BoquilhasResult;
    public sealed record HistoryFound(IReadOnlyList<HistoryItemReadModel> Rows, int Total) : BoquilhasResult;
    public sealed record Ficha(BoquilhasFichaReadModel Ficha) : BoquilhasResult;
    public sealed record MovementAuditFound(Guid MovementId, IReadOnlyList<MovementAuditItemReadModel> Entries) : BoquilhasResult;
    public sealed record Created(Guid BoquilhasId, int Version) : BoquilhasResult;
    public sealed record OpeningFactsUpdated(Guid BoquilhasId, int Version) : BoquilhasResult;
    public sealed record MovementAppended(Guid MovementId, int Version, int AggregateVersion) : BoquilhasResult;
    public sealed record MovementEdited(Guid MovementId, int Version, int AggregateVersion) : BoquilhasResult;
    public sealed record Closed(Guid BoquilhasId, int Version, DateTimeOffset ClosedAt) : BoquilhasResult;
    public sealed record Reopened(Guid BoquilhasId, int Version, DateTimeOffset ReopenedAt) : BoquilhasResult;
    public sealed record ProductionsFound(IReadOnlyList<JobOnProductionListItem> Productions) : BoquilhasResult;
    public sealed record JobOnFichaFound(JobOnFicha Ficha) : BoquilhasResult;
    public sealed record BqAssociated(Guid JobOnId, Guid BqId, int JobOnVersion) : BoquilhasResult;
    public sealed record AssignmentsFound(IReadOnlyList<MachineRepairerAssignmentReadModel> Assignments) : BoquilhasResult;
    public sealed record RepairersFound(IReadOnlyList<RepairerReadModel> Repairers) : BoquilhasResult;
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : BoquilhasResult;
    public sealed record NotFound(Guid Id) : BoquilhasResult;
    public sealed record Refused(BoquilhasRefusalReason Reason, string Message) : BoquilhasResult;
}

public enum BoquilhasRefusalReason
{
    StaleVersion,                  // 409 stale-version
    SaidaExceedsAvailable,         // 409 saida-exceeds-available
    IrreparavelExceedsInRepair,    // 409 irreparavel-exceeds-in-repair
    ActiveAggregateExists,         // 409 active-aggregate-exists
    AlreadyClosed,                 // 409 already-closed
    NotClosed,                     // 409 not-closed
    NotLastClosed,                 // 409 not-last-closed
    AggregateClosed,               // 409 aggregate-closed
    OnlyOneInicio,                 // 409 only-one-inicio
}
```

No result type carries a lifecycle status beyond the aggregate status token, a document state,
a PDF/file fact, a permission decision or a navigation target.

### 12.2 Validation codes (exact transport strings inside `validation-failed`)

```text
QUANTITY_NOT_POSITIVE        INITIAL_QUANTITY_REQUIRED    MOVEMENT_TYPE_INVALID
MACHINES_REQUIRED            MACHINE_REQUIRED             MACHINE_UNKNOWN
MACHINE_NOT_IN_AGGREGATE     REPAIRER_REQUIRED            REPAIRER_NOT_FOUND
ANCHOR_REQUIRED              ANCHOR_CONFLICT              TOOL_NOT_FOUND
TOOL_TYPE_MISMATCH           BQ_CONTEXT_NOT_FOUND         UTILISATION_INVALID
BUSINESS_DATE_INVALID        OBSERVATIONS_INVALID         REOPEN_REASON_REQUIRED
REFERENCE_REQUIRED           FILTER_INVALID
```

(`TOOL_NOT_FOUND`/`TOOL_TYPE_MISMATCH`/`MACHINE_UNKNOWN`/`MACHINE_REQUIRED`/
`REFERENCE_REQUIRED` reuse the exact P2-T04 tokens; `REOPEN_REASON_REQUIRED` mirrors the P2-T06
pattern.)

### 12.3 Transport mapping (exact, mirroring the accepted endpoint pattern)

| Result | HTTP | Body |
|---|---|---|
| `ListFound` / `HistoryFound` | 200 | `{ "rows": […], "total": n }` |
| `Ficha` | 200 | `{ "ficha": { … } }` (§15.5 shapes) |
| `MovementAuditFound` | 200 | `{ "movementId": "…", "entries": […] }` |
| `Created` | 201 | `{ "boquilhasId": "…", "version": 1 }` |
| `OpeningFactsUpdated` | 200 | `{ "boquilhasId": "…", "version": n }` |
| `MovementAppended` | 201 | `{ "movementId": "…", "version": 1, "aggregateVersion": n }` |
| `MovementEdited` | 200 | `{ "movementId": "…", "version": n, "aggregateVersion": n }` |
| `Closed` | 200 | `{ "boquilhasId": "…", "version": n, "closedAt": "…" }` |
| `Reopened` | 200 | `{ "boquilhasId": "…", "version": n, "reopenedAt": "…" }` |
| `ProductionsFound` | 200 | `{ "productions": […] }` (accepted P2-T04 shape) |
| `JobOnFichaFound` | 200 | `{ "ficha": { … } }` (accepted P2-T04 shape) |
| `BqAssociated` | 201 | `{ "jobonId": "…", "bqId": "…", "version": n }` |
| `AssignmentsFound` | 200 | `{ "assignments": [ { "machine", "repairerId", "repairerName" } ] }` — absent machine = **explicit** `"assignmentUnavailable": true`, never an error |
| `RepairersFound` | 200 | `{ "repairers": [ { "repairerId", "name" } ] }` |
| `ValidationFailed` | 400 | `{ "reason": "validation-failed", "errors": [ … ] }` |
| `NotFound` | 404 | `{ "reason": "not-found" }` |
| `Refused(StaleVersion)` | 409 | `{ "reason": "stale-version", "message": "…" }` |
| `Refused(SaidaExceedsAvailable)` | 409 | `{ "reason": "saida-exceeds-available", "message": "…" }` |
| `Refused(IrreparavelExceedsInRepair)` | 409 | `{ "reason": "irreparavel-exceeds-in-repair", "message": "…" }` |
| `Refused(ActiveAggregateExists)` | 409 | `{ "reason": "active-aggregate-exists", "message": "…" }` |
| `Refused(AlreadyClosed)` | 409 | `{ "reason": "already-closed", "message": "…" }` |
| `Refused(NotClosed)` | 409 | `{ "reason": "not-closed", "message": "…" }` |
| `Refused(NotLastClosed)` | 409 | `{ "reason": "not-last-closed", "message": "…" }` |
| `Refused(AggregateClosed)` | 409 | `{ "reason": "aggregate-closed", "message": "…" }` |
| `Refused(OnlyOneInicio)` | 409 | `{ "reason": "only-one-inicio", "message": "…" }` |
| anonymous caller | 401 | (policy challenge) |
| authenticated but not granted, or module not available | 403 | (policy forbid) |
| unexpected infrastructure failure | 500 | generic; logged at status level only, never a payload |

Rules (accepted, unchanged): a denial is **never** rendered as an empty list/page/success; a
lookup failure is never mapped to `empty`; **409 is the single status for every balance/lifecycle
state refusal** — distinguished by the reason token — and never partially applied; no error body
contains a connection string, a filesystem path, a secret or another user's data; the Web layer
performs no domain decision.

### 12.4 Presentation state mapping (consumer side)

| Outcome | Presented shared state (P2-T01/P2-T03 vocabulary) |
|---|---|
| request in flight | `loading` |
| rows present | `ready` |
| zero rows | `empty` (explicit no-results) |
| infrastructure failure | `lookup-failed` (+ supplied retry) |
| module/service unavailable | `unavailable` |
| 403 | `permission-denied` (never a blank surface) |
| mutation in flight | `saving` / `submitting` |
| 409 `stale-version` | `conflict` with explicit reload recovery (D2 pattern; no auto-retry) |
| 409 `saida-exceeds-available` / `irreparavel-exceeds-in-repair` | actionable refusal on the form (never a block of other work; negative saldo presentation stays non-blocking) |
| 409 `active-aggregate-exists` / `already-closed` / `not-last-closed` / `aggregate-closed` / `only-one-inicio` | actionable refusal with the stated reason |
| negative saldo in the balance projection | visible value; red presentation is a UI rule and never blocks (global §13) |
| `assignment-unavailable` | explicit state on the movement form; manual selection enabled (never an error) |

---

## 13. Route matrix

### 13.1 Base path

```text
Boquilhas   /boquilhas   (pages + minimal-API JSON endpoints)
```

`boquilhas` is the accepted `DestinationId` of `ModuleCatalog.Boquilhas` (single assignable
module — **no shared destination, no sibling module**); **no destination/route registration
happens in P2-T07** (interim state §13.3; P2-T10 owns registration).

### 13.2 Complete P2-T07 route table (exactly eighteen rows)

| # | Route | Kind | Purpose | Policy | Request carrier | Result carrier | Failure states |
|---|---|---|---|---|---|---|---|
| 1 | `GET /boquilhas` | page `Pages/Boquilhas/Index` | Registo: lot grid (active aggregates) + opened aggregate register (summary, balance, movements, close/reopen/utilisation regions) (§25) | `boquilhas` | `?boquilhasId=` (open exact aggregate) | rendered page | 403 |
| 2 | `GET /boquilhas/novo` | page `Pages/Boquilhas/Novo` | aggregate opening surface: production-linked / standalone flows, Tool orchestration, opening facts (§16, §22) | `boquilhas` | `?jobonId=` (prefill only) | rendered page | 403 |
| 3 | `GET /boquilhas/historico` | page `Pages/Boquilhas/Historico` | local Histórico surface: filters + table + open (§24) | `boquilhas` | `?boquilhasId=` (open exact aggregate) | rendered page | 403 |
| 4 | `GET /boquilhas/aggregates` | minimal API | Registo lot-grid query (active default; state/reference/lot/machine filters; paging) | `boquilhas` | `BoquilhasListQuery` query string | 200 `ListFound` | 400 `FILTER_INVALID`, 403 |
| 5 | `GET /boquilhas/aggregates/{boquilhasId:guid}` | minimal API | aggregate ficha: anchor projection, machines, opening facts, status/version, derived balance buckets, movement ledger, close-snapshot/reopen presence (§15.5) | `boquilhas` | route `boquilhasId` | 200 `Ficha` | 404, 403 |
| 6 | `GET /boquilhas/aggregates/{boquilhasId:guid}/movements/{movementId:guid}/audit` | minimal API | the edit/audit trail of one movement (§19) | `boquilhas` | route ids | 200 `MovementAuditFound` | 404, 403 |
| 7 | `POST /boquilhas/aggregates` | minimal API | create the aggregate (+ machine set + Início) transactionally | `boquilhas` | `CreateBoquilhasRequest` (§22.4) | 201 `Created` | 400 `validation-failed`, 404, 409 `stale-version` \| `active-aggregate-exists`, 403 |
| 8 | `POST /boquilhas/aggregates/{boquilhasId:guid}/movements` | minimal API | append one movement (four types; balance-relative validation) | `boquilhas` | `AppendMovementRequest` (§17) | 201 `MovementAppended` | 400 `validation-failed`, 404, 409 `stale-version` \| `saida-exceeds-available` \| `irreparavel-exceeds-in-repair` \| `aggregate-closed` \| `only-one-inicio`, 403 |
| 9 | `PUT /boquilhas/aggregates/{boquilhasId:guid}/movements/{movementId:guid}` | minimal API | edit the existing movement + audit (same row; no second quantity event) | `boquilhas` | `EditMovementRequest` (§19) | 200 `MovementEdited` | 400 `validation-failed`, 404, 409 `stale-version` \| `saida-exceeds-available` \| `irreparavel-exceeds-in-repair` \| `aggregate-closed`, 403 |
| 10 | `POST /boquilhas/aggregates/{boquilhasId:guid}/close` | minimal API | close (status + immutable snapshot) | `boquilhas` | `{ expectedVersion }` | 200 `Closed` | 400, 404, 409 `stale-version` \| `already-closed`, 403 |
| 11 | `POST /boquilhas/aggregates/{boquilhasId:guid}/reopen` | minimal API | reopen (reason required; eligibility) | `boquilhas` | `{ expectedVersion, reason }` | 200 `Reopened` | 400 `REOPEN_REASON_REQUIRED`, 404, 409 `stale-version` \| `not-closed` \| `not-last-closed` \| `active-aggregate-exists`, 403 |
| 12 | `PUT /boquilhas/aggregates/{boquilhasId:guid}/opening-facts` | minimal API | update opening business date / manual utilisation / observations / machine set | `boquilhas` | `UpdateOpeningFactsRequest` (§23) | 200 `OpeningFactsUpdated` | 400 `validation-failed`, 404, 409 `stale-version` \| `aggregate-closed`, 403 |
| 13 | `GET /boquilhas/productions` | minimal API | reference → productions for the **opening** flow (production-linked) | `boquilhas` | `?reference=` | `ProductionsFound` | 400 `REFERENCE_REQUIRED`, 403, 200 empty |
| 14 | `GET /boquilhas/jobons/{jobonId:guid}` | minimal API | Job On ficha read (BQ context presence + production facts) for the opening flow and the contextual panel | `boquilhas` | route `jobonId` | `JobOnFichaFound` | 404, 403 |
| 15 | `POST /boquilhas/jobons/{jobonId:guid}/bq-association` | minimal API | create the missing BQ context through `IJobOnService` (BQ-slot Set; explicit human confirmation) | `boquilhas` | `{ toolId, expectedJobOnVersion }` | 201 `BqAssociated` | 400 `TOOL_NOT_FOUND` \| `TOOL_TYPE_MISMATCH`, 404, 409 `stale-version`, 403 |
| 16 | `GET /boquilhas/machine-assignments` | minimal API | consumed read: the six current machine → repairer assignments (resolution suggestions) | `boquilhas` | — | `AssignmentsFound` | 403 |
| 17 | `GET /boquilhas/repairers` | minimal API | consumed read: repairer register (manual selection list) | `boquilhas` | — | `RepairersFound` | 403 |
| 18 | `GET /boquilhas/history` | minimal API | local Histórico query (full filter set §24, backend-applied) | `boquilhas` | `BoquilhasHistoryQuery` query string | 200 `HistoryFound` | 400 `FILTER_INVALID`, 403 |

**Route-count statement:** exactly the eighteen rows above (3 pages + 15 endpoints). No settings
route, no repairer-administration route, no machine-assignment write route, no PDF/file/email
route, no document route, no availability route, no second Tool search/create route (the shared
orchestration stays on the P2-T04 `ferramentas` routes 12/13 — §16), no Job On mutation beyond
the BQ-slot association, no route carrying a second policy, no route registering anything. The
three pages may server-render their initial data from the same query contracts (page load =
routes 4/18/5 response embedded) keeping exactly one query shape (P2-T05 §4.2 precedent).

### 13.3 Interim runtime state (mandatory — P2-T05 §21.6/P2-T06 §13.3 pattern)

```text
ModuleRegistrations.CurrentBuildAvailable = []            (unchanged by P2-T07)
EmptyDestinationRouteRegistry                             (unchanged)
DestinationRouteRegistrations                             (unchanged — still empty)
```

P2-T07 pages/endpoints exist and are server-gated, but `AccessResolver` denies every P2-T07
route to every caller **until P2-T10 registers `boquilhas` availability** (step P2-T10d of the
master plan §13). Integration tests use test-only module registries.

---

## 14. Authorization policy per route

| Concern | Contract |
|---|---|
| Module | `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas)` on **every** P2-T07 route/action — pages and endpoints alike; the Razor `[Authorize]` constants are pinned in `BoquilhasPolicyNames` and asserted equal to the canonical projection of `ModuleCatalog.Boquilhas` (the accepted `JobOnPolicyNames`/`ControloPolicyNames` pattern) |
| Single module | Boquilhas is **one** assignable module with **no** shared destination and **no** sibling module; there is nothing to merge (`ACCESS_MODEL.md` §1/§11; §16) |
| Unrelated grants | a grant to any other module (`job-on-view`, `job-on-create`, `controlo-create`, `controlo-approve`, `ferramentas`, …) **never** satisfies a P2-T07 route; the shared Tool flows' `ferramentas` routes remain `ferramentas`-gated and grant **no** Boquilhas capability (AC-A2) |
| ADMIN | ADMIN gains **no** operational access (`ModuleAuthorizationHandler` fails closed for non-USER accounts); no `dmo.administration` policy on any P2-T07 route (AC-A4) |
| Direct-route enforcement | hiding a control is never authorization: every route is denied server-side for non-granted callers, including direct URL entry (AC-A3) |
| Presentation | navigation/button visibility never substitutes for the gate; denial is never an empty list/blank surface (AC-A5) |
| Cross-module reads | Boquilhas reads Job On/Tool/repairer facts through **accepted application contracts under Boquilhas-gated routes** (the "reads needed to operate the surface" pattern, P2-T04 §13.3); **no P2-T07 code calls a route gated by another module's policy**; Tool search/create stays on the `ferramentas` routes (P2-T05 §22/AC-R6 precedent) |

---

## 15. Identity model — binding

### 15.1 The four facts (exact)

```text
tool_id       = canonical registered BQ Tool                 (P2-T04 tools; CHECK tool_type='BQ' consumers)
bq_id         = frozen Job On BQ context                     (P2-T04 bq_contexts; CHECK tool_type='BQ';
                frozen triple tool_type/tool_reference/tool_lot; direct tool_id relation; jobon_id owner)
boquilhas_id  = one collective BQ external-repair aggregate   (P2-T07 boquilhas)
movement_id   = one quantity movement/event on one aggregate (P2-T07 boquilha_movements)
```

- `bq_id` is **not** `boquilhas_id`; neither replaces `tool_id` (`modules/BOQUILHAS.md`;
  `IDENTITIES_AND_RELATIONSHIPS.md`).
- `tool_id` = canonical Tool; `bq_id` = **frozen Job On BQ context**. They are **not competing
  identities** — they are the two legal anchors of an aggregate, mutually exclusive per
  aggregate (S1).
- No `production_id`, no `job_on_revision_id`, no reverse-ID arrays, no per-piece BQ identity,
  no merge of two BQ Tools because visible reference/lot text matches (global §4).

### 15.2 The two flows (exact, both first-class)

```text
PRODUCTION-LINKED:   boquilhas_id → bq_id → jobon_id + tool_id        (bq_id is a REAL bq_contexts row)
STANDALONE:          boquilhas_id → tool_id                            (tool_id is a REAL tools row, type 'BQ')
```

- The DB-enforced exclusive anchor makes the combination **exactly one of the two**; a
  production-linked aggregate never carries a second direct `tool_id` (reachable through
  `bq_id`), and a standalone aggregate never carries a synthetic `bq_id` or Job On (AC-I1/I2,
  AC-I7).
- "Invalid linked/standalone identity combination" is therefore a closed set: both anchors
  supplied (`ANCHOR_CONFLICT`), no anchor (`ANCHOR_REQUIRED`), a `bq_id` that is not a real
  `bq_contexts` row (`BQ_CONTEXT_NOT_FOUND` — FK + validator), a `tool_id` that is not a real BQ
  Tool (`TOOL_NOT_FOUND`/`TOOL_TYPE_MISMATCH`).
- `movement_id → boquilhas_id` in both flows; movement rows never carry `tool_id`/`bq_id`/
  `jobon_id` (reachable through the aggregate).

### 15.3 What the aggregate does NOT duplicate

- No reference/lot copy (S9): labels are read **through the anchor** — standalone via live
  `tools`, production-linked via the frozen `bq_contexts` triple; History reference/lot filters
  are traversal predicates (§24). The frozen triple is presented as historical/production fact,
  never as the Tool's current value (P2-T04 §8.4 discipline).
- No Job On state copy: the contextual panel reads real production context and owns none of it
  (§22.5).
- No machine registry, no `machine_id`, no line-grouping (CHECK codes `B1..C3` on the machine
  set and movement rows only).
- No mirror of the P2-T05 repairer register (FK to `repairers` only).

---

## 16. Tool selection / creation contract

### 16.1 Ownership (exact)

The **shared Tool search/select/create orchestration** of P2-T04 §9 is consumed unchanged: no
second Tool picker, no second Tool registry, no module-specific Tool identity, no local Tool
storage (`FERRAMENTAS_LIGHT.md`; `CROSS_MODULE_FLOWS.md` "Shared Tool flow"; P2-T05 §23.1). The
Tool search/create **HTTP surface remains the P2-T04 `ferramentas` routes** (`GET/POST
/ferramentas/tools`, routes 12/13, `dmo.module.ferramentas`) — "Tool search/create access
remains `ferramentas`-gated; no second Tool access path" (P2-T05 §22/AC-R6). P2-T07 pages
orchestrate the picker subflow against those routes exactly as the P2-T05 Novo controlo surface
does.

### 16.2 The Boquilhas opening use (exact)

```text
[Boquilhas opening surface]
  1. operator supplies the known context (reference/lot, expected BQ slot)
  2. adapter calls GET /ferramentas/tools with type=BQ (+ reference/lot/machine search facts)
       → candidates are BQ Tools only (the authoritative BQ tool type filter; AC-T1)
  3. ToolPickerPresentation renders candidates; a single candidate is presented exactly like
     many — NEVER auto-selected (AC-T2)
  4a. explicit selection  → adapter resolves its opaque key → canonical tool_id (never built
      from display text; backend re-validates existence + type)
  4b. "Criar ferramenta"  → the contextual create subflow (enabled by the consumer — Boquilhas
      enables it per the EXISTE → SELECIONA / NÃO EXISTE → CRIA → CONTINUA rule) posts
      POST /ferramentas/tools with tool_type=BQ, reference, lot, machines (≥1), optional
      processo/quantity → the response carries the canonical tool_id
      → the subflow returns to the SAME Boquilhas origin state (no navigation away; entered
      opening facts preserved; P2-T03 opaque OriginToken never parsed as identity)
  5. the opening command carries the canonical tool_id; the backend validates it
```

Rules (binding — P2-T04 §9.3 consumed verbatim): no auto-selection including a single
candidate; ambiguity stays explicit; create returns the real persisted `tool_id`; cancel
restores origin with no Tool selected and no origin value changed; pre-fill is assistance only
(the machine set may pre-fill from the selected Tool's registered compatible machines —
registered Tool facts, never inferred from the reference code; operator confirms/changes); the
origin module never becomes Tool owner (no P2-T07 write to `tools`/`tool_machines`).

---

## 17. Movement contract

### 17.1 The ledger and the movement selector

- The movement **selector exposes exactly** `Início`, `Saída`, `Entrada`, `Irreparável`
  (AC-V1); `Editar` is **not** rendered as a movement type — it is an external/context action on
  a selected existing movement (§19; AC-V2).
- Each movement is an append-only operational fact on the aggregate:
  `movement_id ├─ movement_type ├─ quantity ├─ business_date ├─ recorded_at ├─ actor ├─
  machine? (required on external Saída) ├─ repairer_id? (required on external Saída) └─
  observations/context` (global §6; beta module doc).
- `Início` is created **once, with the aggregate** at opening (S2): the opening quantity IS the
  Início movement. The append route refuses any later `inicio` (`only-one-inicio`, 409) —
  AC-V4.

### 17.2 Append validation (exact, computed inside the append transaction)

```text
AppendMovementCommand(
    Guid BoquilhasId, int ExpectedAggregateVersion,
    string MovementType,            // 'inicio' | 'saida' | 'entrada' | 'irreparavel'
    int Quantity,
    DateOnly BusinessDate,
    string? Machine, Guid? RepairerId,
    string? Observations)
```

1. validators (§9.2) — type in the closed set (`MOVEMENT_TYPE_INVALID` otherwise), `Quantity >
   0` (`QUANTITY_NOT_POSITIVE`), `business_date` well-formed (`BUSINESS_DATE_INVALID`),
   `Machine`/`RepairerId` shape (`MACHINE_UNKNOWN`, `REPAIRER_NOT_FOUND`), observations
   (`OBSERVATIONS_INVALID`);
2. in-transaction: aggregate exists (`NotFound`), version matches
   (`Refused(StaleVersion)`), status `active` (`Refused(AggregateClosed)`), type `inicio` ⇒
   refused (`Refused(OnlyOneInicio)`);
3. `machine`, when supplied, must belong to the aggregate's registered machine set
   (`MACHINE_NOT_IN_AGGREGATE`); external Saída requires `machine` + `repairer_id`
   (`MACHINE_REQUIRED`, `REPAIRER_REQUIRED`) — and the CHECK backstop (§7.4);
4. balance-relative rules over the replay **before** this movement (AC-B3/B4):
   - `saida`: `Quantity <= Disponível` else `Refused(SaidaExceedsAvailable)` — nothing written;
   - `irreparavel`: `Quantity <= Em reparação` else `Refused(IrreparavelExceedsInRepair)` —
     nothing written;
   - `entrada`: **never refused for exceeding the expected amount** — recorded in full; the row
     carries `expected_return_quantity = GREATEST(0, Em reparação before)` and
     `excess_received_quantity = GREATEST(0, Quantity − expected_return_quantity)` (S6;
     AC-B5/B6);
5. INSERT the single movement row (`recorded_at` = backend clock, `recorded_by_user_id` =
   current account) and COMMIT.

**Negative saldo is never a refusal.** Any derived negative bucket (typically `Em reparação`
after an excess Entrada) remains visible and non-blocking; no validation, no CHECK and no
presentation blocks on it (AC-B7).

### 17.3 Line context (S10)

- Each movement may carry `machine` as its line context "where recorded" (global §6); external
  Saída requires it (§21). It is a **frozen fact of that movement** (never rewritten by later
  aggregate machine-set changes).
- Changing the aggregate's machine set (opening-facts route, §23) is aggregate context — never
  a movement type and never a cascade to movement rows.

---

## 18. Balance derivation contract

### 18.1 The only authority

Movement facts (`boquilha_movements`) are the **sole mutable operational quantity authority**.
No balance table, no balance columns on `boquilhas`, no sum cache, no read-model table exists or
is created (AC-B1; scan + schema proof). Derived projections are computed at read time by exact
replay.

### 18.2 Replay (exact)

```text
Replay(aggregate):
  1. load every movement of the aggregate
  2. order deterministically by recorded_at ASC, movement_id ASC        (S5; AC-D3)
  3. seed: disponivel = 0, em_reparacao = 0, irreparavel = 0, entrada_excecional = 0
  4. for each movement m:
       inicio        -> disponivel += m.quantity
       saida         -> disponivel -= m.quantity; em_reparacao += m.quantity
       entrada       -> em_reparacao -= m.quantity; disponivel += m.quantity
                        entrada_excecional += m.excess_received_quantity
       irreparavel   -> em_reparacao -= m.quantity; irreparavel += m.quantity
```

Buckets (labels: Disponível / Em reparação / Irreparável / Entrada excecional):

```text
Disponível           = Σ(início) − Σ(saída) + Σ(entrada)
Em reparação         = Σ(saída) − Σ(entrada) − Σ(irreparável)
Irreparável          = Σ(irreparável)
Entrada excecional   = Σ(excess_received_quantity over entrada rows)
Invariant:           Disponível + Em reparação + Irreparável = Σ(início)
```

- The per-movement `Saldo` projection (`saídas − entradas`, global §13) is display-only
  presentation over the same facts.
- **Replay order is physical receipt order** (`recorded_at`, tie-break `movement_id`): editing
  `business_date` never reorders the ledger and never changes the balance (AC-D3); operational
  calendars/filters use `business_date`, audit/security may use `recorded_at` (global §6/§14).
- Edits replay with the edited row's current values replaced by its new values (§19) — exactly
  one net quantity event per movement.

### 18.3 Derived read support

If the implementation proves a query-support need, it may add **derived/query support only** —
an index (post-hoc, justified by a real predicate, own decision) — never a materialized balance.
No such index is contracted today (§7.5 stance).

---

## 19. Edit / audit contract

### 19.1 Editar — the action, exact

```text
EditMovementCommand(
    Guid BoquilhasId, int ExpectedAggregateVersion,
    Guid MovementId, int ExpectedMovementVersion,
    int Quantity, DateOnly BusinessDate,
    string? Machine, Guid? RepairerId, string? Observations)
```

- `Editar` **modifies an EXISTING movement** — the SAME `movement_id` row (AC-E1). It is an
  external/context action on a selected movement; it never appears in the movement selector
  (§17.1); it creates **no** second movement row and **no** second quantity event (AC-E3).
- **Editable fields:** `quantity`, `business_date`, `machine`, `repairer_id`, `observations`
  (S4). **Immutable fields:** `movement_type`, `recorded_at`, `boquilhas_id` — none of them is
  in the carrier; no code path writes them (AC-E6).
- **Before/after audit:** one `boquilha_movement_audit` row per edit, in the same transaction,
  carrying the exact before/after values of every editable field, the **backend** editor
  (`edited_by_user_id` from `ICurrentAccountContext`) and the **backend** timestamp
  (`edited_at`, backend clock) — never client-supplied (AC-E2/E5).
- **Concurrency:** `expectedAggregateVersion` + `expectedMovementVersion`; staleness → 409
  `stale-version`, nothing written (§11).
- **Atomicity:** the UPDATE and the audit INSERT are one transaction; a mid-transaction failure
  leaves the movement untouched and no audit row (AC-E1/E3, K3).

### 19.2 Re-validation — no double balance effect (exact)

1. In-transaction: aggregate exists (`NotFound`) and is `active` (`Refused(AggregateClosed)`);
   versions match; the movement exists and belongs to the aggregate (`NotFound`);
2. compute the replay **excluding the edited movement's current values** (its current effects
   removed — the "before state" of the ledger);
3. validate the **new values exactly like an append** (§17.2 rules 1/3/4, with `Quantity`/
   `Machine`/`RepairerId`/`BusinessDate` from the carrier) — Saída still ≤ Disponível,
   Irreparável still ≤ Em reparação, machine membership, repairer required on Saída;
4. UPDATE the same row to the new values, `version += 1`, `updated_at = now()`;
5. INSERT the audit row (before = the row's pre-update values; after = the new values).

Consequence (AC-E4): the post-edit balance equals the replay with the row's current values
replaced by the new ones — a **single net event**; the aggregate's total quantity effect is the
same as before the edit for the unedited rows, and the edited row contributes exactly one event.
The audit trail renders via the accepted **`AuditTrail` primitive** (supplied entries only;
never synthesizes actor/time; never rendered as movement rows — AC-E3/E4).

### 19.3 No annulment/removal carrier

No delete/annulment action, route or column exists in P2-T07 (S12). `Editar` (audited replace)
is the correction mechanism; a wrongly recorded movement is corrected, never silently removed.
A future annulment requires its own authority and, per the Beta module doc, must then be a
**registered, confirmed fact — never silent physical deletion** (AC-N7 boundaries).

---

## 20. Business date vs recorded time

Two distinct facts, never conflated (global §6; beta module doc; AC-D1/D2/D3):

| Fact | Meaning | Editable? | Written by |
|---|---|---|---|
| `business_date` (movement) | when the physical/operational movement happened; the operator-facing calendar/date filters use it | **YES** — operator-editable at append and via `Editar` (correcting a legitimate earlier/later movement without falsifying audit) | the operator (through the command), stored on the row |
| `recorded_at` (movement) | immutable system receipt timestamp | **NO — immutable**, never manually changed, never rewritten | backend clock at first insertion only |
| `opening_date` (aggregate) | operator-editable business date of the repair-flow register (default today) | **YES** — opening-facts route (§23) | the operator, stored on the row |
| `edited_at`/`closed_at`/`reopened_at`/`created_at` | backend technical timestamps | **NO** | backend clock |

- **Changing `business_date` never rewrites `recorded_at`** (AC-D3): the edit UPDATE statement
  lists only the editable columns; a test asserts `recorded_at` byte-equal before/after.
- Balance and replay order never depend on `business_date` (§18.2); historical truth is not
  judged by insertion timestamps alone (operational calendar = `business_date`; audit/security
  may use `recorded_at` — global §6/§14).

---

## 21. Repairer resolution contract

### 21.1 The closed rule (consumed, binding)

```text
machine
→ current repairer assignment        (Controlo_Create → Definições; machine_repairer_assignments)
→ repairer automatically resolved    (suggested for the Boquilhas movement)
```

- The **register and the assignments belong to `Controlo_Create → Definições`** (closed P2-T05;
  `…DELTA.md` §3/§4). Boquilhas **consumes** both; it administers neither (AC-R4; no write
  route/type exists).
- The six machines `B1 B2 B3 C1 C2 C3` are **independent**: each has its own assignment; there
  is **no** shared B/C repairer, **no** "Linha B"/"Linha C" grouping, **no** cascade — changing
  one machine's assignment changes no other machine's resolution (AC-R2; the Definições write
  surface is P2-T05's; P2-T07 only reads whatever current state exists).
- `assignment-unavailable` (no assignment row for the machine) is an **explicit state** — never
  a default repairer, never an error, never conflated with a missing repairer (AC-R6); the
  operator then selects the final repairer manually from the register read (route 17).

### 21.2 The stored fact

- **Every external Saída stores the final selected canonical `repairer_id`** (validator
  `REPAIRER_REQUIRED` + DB CHECK backstop; AC-R5) and its `machine`. The suggestion may be
  resolved automatically and pre-filled, but the **final stored relationship is the chosen
  `repairer_id`** (global §10: "the human can select the final value where allowed"): the append
  command always carries the final value; the backend validates it against the register
  (`REPAIRER_NOT_FOUND`). The operator is **not** forced to re-type it when the machine already
  determines it (`…DELTA.md` §5.3) — the UI pre-fill IS the automatic resolution (AC-R1).
- Where the operator genuinely used a repairer other than the machine's current assignment, the
  movement stores the selected override (the delta §5.4 leaves the override to this contract;
  pinned here: the operator may replace the suggested value before saving — the stored fact is
  final-selected; no separate override flag is invented).
- `repairer_id` is **historically preserved on the movement row** (the minimum-relation,
  minimum-snapshot pattern the architecture establishes — same family as the frozen context
  triple and P2-T05 §11.4's explicit assignment: "Boquilhas satisfies that by storing the
  canonical `repairer_id` **on the external Saída movement itself** with historical retention").
  A later assignment change **never rewrites earlier movements** (AC-R3): no join-time
  re-resolution, no write path touching `repairer_id` except `Editar` (an explicit human
  correction, itself audited).

### 21.3 Resolution read (exact)

`GET /boquilhas/machine-assignments` returns the six current assignments composed from the
closed `IMachineRepairerAssignmentRepository` (read-only, under the `boquilhas` gate — the P2-T05
§29 seam: "current-state assignment rows + read contract"). Per machine: `{ machine,
repairerId, repairerName }` or `{ machine, assignmentUnavailable: true }`. The movement form
resolves the suggestion from this read; **no P2-T07 command contains a resolved repairer derived
server-side from a machine alone** — the saved value is always the human-confirmed
`repairer_id` of the command.

---

## 22. Production-linked vs standalone contract

### 22.1 The two opening flows (exact)

**Production-linked (AC-I1):**

```text
[Novo — produção]
  1. operator searches a reference → GET /boquilhas/productions (reference required;
     productions listed; explicit selection; never auto-selected)
  2. GET /boquilhas/jobons/{jobonId} — production facts + BQ context presence
  3. BQ Tool via the shared orchestration (§16) — type filter BQ, explicit selection or
     contextual creation
  4. bq context:
       - exists for (jobon_id, tool_id)  → REUSE it (idempotent resolution; P2-T04 §7.3)
       - absent                          → explicit "Associar BQ" action →
             POST /boquilhas/jobons/{jobonId}/bq-association
             (composes IJobOnService.UpdateAsync with Keep-all + Set(BQ slot,
             toolId validated TOOL_NOT_FOUND/TOOL_TYPE_MISMATCH); the bq_contexts row is
             created BY Job On's own application/repository code, never by a Boquilhas
             repository — the accepted P2-T05 §21.4 composition, BQ slot)
             → real bq_id returned (AC-I3)
  5. opening command anchors bq_id → the aggregate is production-linked
```

**Standalone (AC-I2):**

```text
[Novo — independente]
  1. BQ Tool via the shared orchestration (§16)
  2. opening command anchors tool_id directly
  3. NO Job On read, NO bq context, NO fake bq_id, NO synthetic Job On (AC-I2/I4)
```

- Standalone is a **first-class valid flow**; nothing forces Boquilhas records through Job On
  (`RECORD_LIFECYCLES.md` §9; `CROSS_MODULE_FLOWS.md`; `IDENTITIES_AND_RELATIONSHIPS.md`).
- **No fake identities anywhere:** the anchor exclusivity CHECK, the `BQ_CONTEXT_NOT_FOUND`
  validation (a supplied `bq_id` must be a real row) and the scans of §29 prove the absence of
  fake context IDs (AC-I4).

### 22.2 Opening command (exact)

```text
CreateBoquilhasCommand(
    Guid? BqId, Guid? ToolId,            // exactly one (ANCHOR_REQUIRED/ANCHOR_CONFLICT)
    IReadOnlyList<string> Machines,      // ≥1 (MACHINES_REQUIRED), codes B1..C3 (MACHINE_UNKNOWN)
    int InitialQuantity,                 // > 0 (INITIAL_QUANTITY_REQUIRED / QUANTITY_NOT_POSITIVE)
    DateOnly OpeningDate,                // default today at the surface; well-formed (BUSINESS_DATE_INVALID)
    decimal? UtilisationPercent,         // manual still; 0–100 or NULL (UTILISATION_INVALID)
    string? Observations)
```

Transaction (§10): validate anchors (Tool exists + type `BQ`; `bq_id` exists as a real
`bq_contexts` row); refuse when an **active** aggregate already exists for the same anchor
(`Refused(ActiveAggregateExists)` — S7, Q-CREATE; the **application pre-check** = normal-path
refusal for UX only, NOT the concurrency authority — §7.2); INSERT `boquilhas` (created_by =
current account) + machine rows + the **Início** movement (`quantity = InitialQuantity`,
`business_date = OpeningDate`, recorded_by = current account); COMMIT. The Início movement is
therefore born with aggregate `version = 1` and movement `version = 1`.

**Race-free anchor enforcement (B1 correction; Architect review `542a08a1…`):** if a competing
transaction commits an active aggregate on the same anchor between the pre-check and this
transaction's COMMIT, the INSERT hits the §7.2 partial unique index
(`IX_boquilhas_active_bq_id` for the production-linked anchor, `IX_boquilhas_active_tool_id` for
the standalone anchor) → `23505 unique_violation` → the **entire transaction rolls back** (no
`boquilhas` row, no machine rows, no Início — no partial aggregate, no orphan Início) → the
exact §7.2 mapping returns `Refused(ActiveAggregateExists)` (409). The application detects the
losing race through the same refusal token as the normal path; nothing internal leaks to the
caller.

### 22.3 BQ-context creation — never a Boquilhas-owned write

`POST /boquilhas/jobons/{jobonId}/bq-association` composes **only** `IJobOnService`: it loads
the Job On ficha, builds an `UpdateJobOnCommand` with **Keep** for every fact and **Set** for
the BQ slot (supplied canonical `tool_id`, validated existence + `TOOL_TYPE_MISMATCH`), and
invokes `IJobOnService.UpdateAsync` — the `bq_contexts` row is created by Job On's own
application/repository code with the frozen triple read from `tools` at that moment (P2-T04
§7.4/§11.2), exactly mirroring P2-T05 route 11's CM composition. `expectedJobOnVersion` comes
from the ficha read. It performs **no other Job On change**. If Controlo or another early module
already created the same `jobon_id`, Boquilhas reuses it — never another identity for the same
production (global modular note).

### 22.4 Job On read projections (the settled production-line contextual panel)

- **Removed from current visual authority** (`…DELTA.md` §11): no machine/reference **sidebar
  simulating Job On state** — no Boquilhas-owned fake production panel (AC-N5).
- **The settled production-line contextual panel** (§25.1 region R6) is a **read-only reading of
  REAL production context** when the aggregate is production-linked: composed from
  `GET /boquilhas/jobons/{jobonId}` (reference, production number, machine via `bq_id →
  job_ons`; frozen BQ triple; processo via the Tool projection). It is **consumed, never
  owned** — it displays nothing that Job On owns as if Boquilhas owned it; mutation of Job On
  planning remains Job On Create behavior with its own gate; navigation to the Job On view is
  gated by the user's **own** `job-on-view` grant (global §12). For a standalone aggregate the
  panel renders only the aggregate's registered machine set — no simulated Job On state (AC-N5).
- Controlo results never mutate Boquilhas balance/state; Boquilhas never decides balances from
  live mutable Job On state (global §12) — balance derives from Boquilhas movements only (§18).

---

## 23. Active aggregate, utilisation, close / reopen

### 23.1 The active aggregate (Registo)

- **What opens/creates an aggregate:** the Novo surface (§22) — Tool/Job On context +
  opening facts + Início; backend-allocated `boquilhas_id` visible at COMMIT.
- **Relation to `tool_id`/`bq_id`:** exactly one exclusive anchor (§15).
- **Status/lifecycle:** `active` ↔ `closed` (§3.2); "archived" is the derived file-state
  projection of `closed` (History "aggregate/file state" filter), not a column.
- **Current summary (exact facts):** anchor context (frozen triple or live Tool projection),
  registered machines, opening date, the derived balance buckets (§18), the manual
  `utilisation_percent` still (number, **never a progress bar** — global §18 "warning ≠ block;
  `%` is never shown as a progress bar"), initial quantity (the Início), status, version.
- **Utilisation:** `utilisation_percent` is **manual only** — operator-entered at opening and
  editable via the opening-facts route; **never derived from movements, never auto-incremented,
  never synced to/from the Ferramentas live reading** (which stays Tool-owned; global §11;
  AC-U1…AC-U3)

### 23.2 Close (exact)

```text
CloseBoquilhasCommand(Guid BoquilhasId, int ExpectedVersion)

BEGIN
  1. load aggregate + ledger (tracked)
  2. assert exists                                  else NotFound
  3. assert version == ExpectedVersion              else ROLLBACK -> Refused(StaleVersion)
  4. assert status == 'active'                      else ROLLBACK -> Refused(AlreadyClosed)
  5. compute the balance by replay (§18)
  6. INSERT boquilha_close_snapshots (closed_by = current account, closed_at = backend clock,
     initial_quantity, opening_date, disponivel, em_reparacao, irreparavel,
     entrada_excecional, utilisation_percent = current manual still)
  7. UPDATE boquilhas: status := 'closed', version += 1, updated_at := now()
  8. COMMIT
```

- **Same `boquilhas_id`**, no replacement aggregate, no new identity (AC-C4/I5).
- **Immutable snapshot** — no UPDATE/DELETE path; future repairer/line/config/movement changes
  never modify it (AC-C1).
- **Failed close is atomic**: any failure (including a forced mid-transaction error) leaves the
  aggregate `active` with no snapshot row (AC-C2).
- After close, the trace leaves the active lists (Registo grid) and appears in the archived
  projection / Histórico (AC-C3).

### 23.3 Reopen (exact)

```text
ReopenBoquilhasCommand(Guid BoquilhasId, int ExpectedVersion, string Reason)

BEGIN
  1. load aggregate (tracked)
  2. assert exists                                  else NotFound
  3. assert version == ExpectedVersion              else ROLLBACK -> Refused(StaleVersion)
  4. validate: btrim(Reason) <> ''                  else ValidationFailed(REOPEN_REASON_REQUIRED)
  5. assert status == 'closed'                      else ROLLBACK -> Refused(NotClosed)
  6. anchor := this aggregate's bq_id | tool_id
  7. assert THIS aggregate has the most recent close among all aggregates sharing the anchor
       (max(closed_at) over boquilha_close_snapshots joined through boquilhas by anchor,
        tie-break close_snapshot_id)               else ROLLBACK -> Refused(NotLastClosed)
  8. assert no OTHER active aggregate shares the anchor (HasActiveAggregateForAnchor)
                                                    else ROLLBACK -> Refused(ActiveAggregateExists)
                                                    (application pre-check = normal-path refusal
                                                     for UX only, NOT the concurrency authority —
                                                     §7.2; the partial unique index of step 10 is
                                                     the race-safe backstop)
  9. INSERT boquilha_reopenings (boquilhas_id, close_snapshot_id = the last close,
     reopened_by = current account, reopened_at = backend clock, reason)
  10. UPDATE boquilhas: status := 'active', version += 1, updated_at := now()
  11. COMMIT
```

- **Same `boquilhas_id`**: no replacement aggregate; prior movements, audits, snapshots and
  reopen records are never rewritten (AC-C7).
- Reopen records actor/time/reason as backend facts (AC-C5).
- Eligibility (S8, Q-REOPEN-ELIG): closed; **last** closed trace for the same BQ Tool context;
  **no other active trace** for the same context. After reopen the trace is an ordinary active
  aggregate; a later close writes a **new** snapshot (AC-C8).
- Reopen is a lifecycle fact, not a movement type (`RECORD_LIFECYCLES.md` §9).
- Failed reopen (any precondition) writes nothing.
- **Race-safe backstop (B1 correction; Architect review `542a08a1…`):** the same
  one-active-per-anchor invariant protects reopen at the database. If a concurrent create (or
  another reopen of a different aggregate on the same anchor) commits an **active** row between
  step 8's scan and step 10's UPDATE, the UPDATE raises `23505 unique_violation` on the §7.2
  partial unique index → the **entire transaction rolls back** → exact §7.2 mapping →
  `Refused(ActiveAggregateExists)`. The aggregate remains `closed`; its close snapshot, history
  and reopen records are untouched; **no partial reopening record is written** (K8).

### 23.4 Opening-facts update (exact)

```text
UpdateOpeningFactsCommand(Guid BoquilhasId, int ExpectedVersion,
    DateOnly OpeningDate, decimal? UtilisationPercent, string? Observations,
    IReadOnlyList<string> Machines)

BEGIN
  1. load aggregate + machines (tracked)
  2. assert exists / version match / status 'active' (else NotFound / StaleVersion / AggregateClosed)
  3. validate: machines ≥ 1 and codes B1..C3 (MACHINES_REQUIRED/MACHINE_UNKNOWN);
     utilisation 0–100 or NULL (UTILISATION_INVALID); dates well-formed; observations shape
  4. UPDATE aggregate row: opening_date, utilisation_percent, observations, version += 1
  5. REPLACE the machine set (delete + insert child rows)
  6. COMMIT
```

- This is **aggregate context only**: it never touches movements, never changes balance, never
  rewrites movement `machine` facts, never reorders the ledger (S10; AC-U2).
- `% utilização` remains a manual still (AC-U1); the close snapshot captures it as of close
  (AC-C5).

---

## 24. Local Histórico behavior

### 24.1 What it is

**HISTÓRICO (local)** inside the Boquilhas module (master plan §0.1; global §13 "transversal
History view covers the whole system … same data source — only the query scope differs"). It is
**not** HISTÓRICO GLOBAL (`historia`, DEFERRED BY DESIGN); neither is authority for the other
(AC-H2).

### 24.2 Query contract (exact)

```csharp
public sealed record BoquilhasHistoryQuery(
    string? State,              // 'active' | 'closed'  (aggregate/file state; both when null)
    string? Reference,          // traversal: tools.reference (standalone) | bq_contexts.tool_reference (linked)
    string? Lot,                // traversal: tools.lot | bq_contexts.tool_lot
    string? Machine,            // aggregates whose machine set contains the value
    DateOnly? BusinessDateFrom, DateOnly? BusinessDateTo,   // movement business_date range (EXISTS)
    string? MovementType,       // aggregates having a movement of this type (EXISTS)
    Guid? RepairerId,           // aggregates having a movement with this repairer (EXISTS)
    int Page = 1, int PageSize = 50);
```

- **Every filter is a backend SQL predicate** (never client-side filtering of a fetched set);
  unknown/ill-formed values → 400 `FILTER_INVALID` (never a silent full list; AC-H1).
- Visibility: **all** aggregates of the module surface (active and closed) — the query scope is
  the whole system's Boquilhas facts; the state filter narrows it (global §13).
- Ordering: deterministic technical order `created_at DESC, boquilhas_id DESC` (authority-silent
  ordering; no industrial meaning, never pre-selects); paging 1-based, `1 <= PageSize <= 100`.
- `Total` = backend-counted rows for the same predicate + filters.

**Row shape:**

```csharp
public sealed record HistoryItemReadModel(
    Guid BoquilhasId, int Version, string State,            // 'active' | 'closed'
    Guid? BqId, Guid? ToolId,                               // exactly one non-null (anchor facts)
    string? Reference, string? Lot,                         // traversal (frozen triple | live Tool)
    IReadOnlyList<string> Machines,
    DateOnly OpeningDate, int InitialQuantity,
    int Disponivel, int EmReparacao, int Irreparavel, int EntradaExcecional,  // replay at read time
    int MovementCount,
    DateTimeOffset? ClosedAt, Guid? ClosedByUserId,         // last close, when closed
    DateTimeOffset? LastMovementAt);
```

### 24.3 Table interaction rule (accepted, binding)

- **Single click selects the row; double click opens the exact aggregate** (Registo/Histórico
  detail via `GET /boquilhas/aggregates/{id}`); actions (Abrir, Reabrir, Ver histórico, Editar
  movimento…) live **outside the table against the selected row** — DenseDataTable selection
  surface + DecisionBar pattern (P2-T02/P2-T03 contracts); **no repeated per-row action button
  grids** (AC-H3/H4).
- The movement table in Registo follows the same rule (single click selects a movement and
  enables Editar/audit; double click opens the movement's edit/detail; actions outside the
  table).
- Rendering discipline: `DenseDataTable`, `RecordStatus` (status text), `AuditTrail` (movement
  edit history and close/reopen trail), `DecisionBar`, `CommonState` (loading/empty/
  lookup-failed/unavailable/permission-denied — all four distinct).
- Empty ≠ lookup-failed ≠ permission-denied (AC-H6).

---

## 25. Fixed-desktop UI regions

Binding **DMO FIXED DESKTOP LAYOUT POLICY** (master plan; freeze §1A; P2-T02 §4; P2-T03 §6;
P2-T05 §24; P2-T06 §21). Canonical 1366 × 768; compact density; region-stable composition; no
breakpoint-driven structural variant (`@media`/`@container`/`@supports` structural rules, width
listeners, control relocation, table→card conversion, required-column hiding, action
relocation); smaller windows use page/local scrolling (keyboard-reachable overflow for wide
regions); mobile/tablet out of scope; shared header/navigation untouched.

### 25.1 Registo — `Pages/Boquilhas/Index` (regions, exact)

| Region | Contract |
|---|---|
| **R1 — Lot grid** | `DenseDataTable` of aggregates (route 4 query; active default, state/reference/lot/machine filters, paging). Columns: Referência · Lote · Máquinas · Estado · Data de abertura · Início · Disponível · Em reparação · Irreparável. **Single click selects; double click opens the exact aggregate in R2**; no per-row action grid; actions outside the table |
| **R2 — Aggregate register** | the opened aggregate (route 5 ficha): anchor context (live Tool projection or frozen BQ triple), machines, opening date, status (`RecordStatus`), manual `% utilização` (**number, never a progress bar**), derived balance buckets (Disponível / Em reparação / Irreparável / Entrada excecional — negative values visible, non-blocking), movement ledger table (Referência · Lote · Movimento · Quantidade · Saldo · Reparador · Linha · Data e hora · Operador — global §13 projection; single click selects a movement, double click opens its edit/detail in R5) |
| **R3 — Movement entry** | the movement **selector exposes exactly** Início/Saída/Entrada/Irreparável; per-type form (Saída: quantity, machine (required), repairer (auto-resolved suggestion, human-confirmed final), business date, observations; Entrada: quantity, business date, observations; Irreparável: quantity, machine?, business date, observations) |
| **R4 — Action region** | `DecisionBar` outside the tables: Registar movimento / Editar (enabled with a selected movement) / Fechar registo / Reabrir / Guardar alterações de abertura; the availability/disabled reasons per the aggregate state (closed → no movement entry; open → no reopen); movement type **never** contains Editar |
| **R5 — Movement edit/detail** | the selected movement: current facts (read-only type/recorded_at/actor), editable fields (quantity/business date/machine/repairer/observations), the `AuditTrail` of before/after edits (route 6) — audit entries are never rendered as movements |
| **R6 — Production-line contextual panel** | **read-only** (§22.4): production-linked — real Job On facts via `bq_id` (reference, production number, machine, frozen BQ triple; navigation to the Job On gated by the user's own `job-on-view` grant); standalone — the aggregate's registered machine set only; **no simulated Job On machine/reference sidebar** |

### 25.2 Novo — `Pages/Boquilhas/Novo` (regions, exact)

| Region | Contract |
|---|---|
| **N1 — Flow choice** | explicit production-linked (`Ligado a produção`) or standalone (`Independente`) — both first-class; no forced Job On |
| **N2 — Production context (linked only)** | reference search → productions list (`DenseDataTable`, explicit selection, never auto-selected) → Job On read; BQ context presence with explicit `Associar BQ` when absent (route 15, confirmation) |
| **N3 — BQ Tool** | the shared Tool orchestration (§16): type-filtered BQ candidates, explicit select, contextual `Criar ferramenta` (enabled), origin state preserved on return |
| **N4 — Opening facts** | machines multi-select (pre-filled from the Tool's registered compatible machines as assistance; ≥1 required), initial quantity (Início), opening date (default today), manual `% utilização` (optional), compact observations; Criar registo posts route 7 |

### 25.3 Histórico — `Pages/Boquilhas/Historico` (regions, exact)

| Region | Contract |
|---|---|
| **H1 — Filters** | the §24.2 set (state, reference, lot, machine, business-date period, movement type, repairer; pagination) — backend-applied |
| **H2 — History table** | `DenseDataTable` of §24.2 rows; single click selects, double click opens the exact aggregate (Registo); no per-row action grid |
| **H3 — Detail** | the exact aggregate (Ficha content) + outside-table actions per state (Abrir/Reabrir) |

### 25.4 Page-owned assets

New `dmo-boquilhas.css` (no structural `@media`/`@container`/`@supports` rules, no token
re-declaration, no colour-literal system) and `dmo-boquilhas.js` reusing `window.dmoFocus` with
the accepted null-safe binding and `renderConflict` reload-recovery patterns (stale-version →
conflict presentation; observed versions refreshed only on success; no auto-retry/auto-merge).
No shared component or shared CSS/JS file is modified.

---

## 26. Negative-scope protections

| # | Protected fact | Proof discipline |
|---|---|---|
| BND-B1 | **No Boquilhas settings/Admin tab** — no internal Definições; no repairer register administration (add/edit/select), no machine-assignment writes, no PDF-directory, no email lists/templates, no glass/water density surface | static scan + route scan + access tests (N1) |
| BND-B2 | **No P2-T08 leakage** — no mandatory Boquilhas PDF, no PDF generation, no file storage/writing, no directory handling, no document identity, no email send/routing/transport; no `Infrastructure/Files`/`Pdf` | static + route scan (N2) |
| BND-B3 | **No HISTÓRICO GLOBAL** (`historia`) route/entry/registration/label | static scan (N3) |
| BND-B4 | **No availability registration** — `ModuleRegistrations.CurrentBuildAvailable` stays `[]`; `DestinationRouteRegistrations` untouched; no navigation entry | static + tests (N4) |
| BND-B5 | **No fake machine/reference sidebar** simulating Job On state; the contextual panel reads real context only | rendered + static scan (N5) |
| BND-B6 | **No fifth movement type / no legacy types / no annulment** — tokens exactly `inicio|saida|entrada|irreparavel`; no `contagem`/`editar`/annulled token, no delete path | static + DB CHECK + rendered (N6) |
| BND-B7 | **No second balance authority** — no balance table/columns/cache anywhere; buckets derived by replay only | schema scan + static (B1) |
| BND-B8 | **No identity duplication** — no `production_id`/`job_on_revision_id`/fake `bq_id`/fake `jobon_id`/reverse arrays/per-piece UUID; no second Tool registry | static + DB (I4/I6) |
| BND-B9 | **No P2-T06/P2-T05 leakage** — no decision trail, no Peso/approval code, no Definições write surface, no `controlo` code | static + route scan |
| BND-B10 | **Protected files** — migrations 001–006 (incl. Designers), `DmoDbContext.cs`, all P2-T04/P2-T05/P2-T06 sources and shared/frozen artifacts byte-identical | regression assertions (MG1) |

---

## 27. Downstream seams (P2-T08 / P2-T10 / Job On)

| Seam | Who consumes | What P2-T07 leaves | What the consumer must do |
|---|---|---|---|
| Aggregate/movement facts + close state | P2-T08 (documents/file state — where a file state applies), P2-T10 | `boquilhas`/`boquilha_movements`/`boquilha_close_snapshots` facts + derived balance; **no document identity/state invented** | P2-T08: any availability state it contracts for Boquilhas reads these facts; P2-T10: register `boquilhas` availability (step P2-T10d) when the surface is real |
| Repairer snapshot on movements | (none — closed) | `repairer_id` + `machine` on external Saída rows; historical retention | nothing — the register/assignments remain P2-T05-owned |
| Job On delete/context-removal protection | P2-T04 delete/context flow | production-linked aggregates reference `bq_contexts` (FK RESTRICT) | P2-T07 registers **`BoquilhasDependencyProbe : IJobOnDependencyProbe`** (one additive line in `Program.cs`, accepted seam): reports `boquilhas-aggregate` dependencies when aggregates reference the target `bq_id`/Job On — app-level `dependency-exists` before the DB backstop |
| Standalone anchor protection | P2-T04/Tool future | standalone aggregates reference `tools` (FK RESTRICT) | nothing now (no Tool delete path in P2-T04); future Tool-mutating workstreams inherit the RESTRICT backstop |
| Read projections | Job On read projections | balance/state read contracts (routes 4/5/18) | Job On exposes its own read projections through its own gated routes if it chooses — never reads P2-T07 tables directly |
| Availability/navigation | P2-T10 | pages/routes exist, `CurrentBuildAvailable` `[]` | register `boquilhas` availability + `/boquilhas` route through the single seam when real |

No seam authorizes implementation: every consumer remains NOT AUTHORIZED until its own gate.

---

## 28. Migration contract

### 28.1 One migration, one owner

P2-T07 owns **exactly one new migration** (the seventh overall):

```text
src/DMO.Infrastructure/Migrations/<UTC timestamp>_BoquilhasDomain.cs   (+ .Designer.cs)
```

- EF-generated (`dotnet ef migrations add BoquilhasDomain` through the accepted
  `DesignTimeDmoDbContextFactory`); `DmoDbContextModelSnapshot.cs` extended by EF (the
  documented, EF-owned extension — P2-T04 §16.1 precedent);
- migrations 001–006 (incl. Designers) **never edited** (regression-asserted byte identity);
- `DmoDbContext.cs` **deliberately NOT modified** (`ApplyConfigurationsFromAssembly` + `Set<T>`).

### 28.2 Expected schema delta (exact)

Created: **6 tables** — `boquilhas`, `boquilha_machines`, `boquilha_movements`,
`boquilha_movement_audit`, `boquilha_close_snapshots`, `boquilha_reopenings` — with exactly the
columns, nullability, defaults, CHECKs, unique constraints (incl. the two **partial unique
indexes** `IX_boquilhas_active_bq_id` / `IX_boquilhas_active_tool_id`, `WHERE status = 'active'
AND <anchor> IS NOT NULL` — §7.2, B1 correction), foreign keys (all `RESTRICT`) and indexes of
§6/§7.

Post-migration public table count: **27** (21 current + 6 new).

Not created: any other table, any seed/reference row, any trigger, any function, any view, any
extension, any sequence, any PostgreSQL enum type, any RLS policy, any
`__EFMigrationsHistory` manipulation, and **no column on any existing table** (no `boquilhas`
columns on `tools`/`job_ons`/`bq_contexts`/`repairers`; no balance column anywhere). The B1
correction changes **indexes only**: the table set (6), the migration count (1 — migration 007
remains the single Boquilhas migration) and every column/CHECK/FK are unchanged.

### 28.3 Supersession record (exact extent)

Nothing in any prior migration is superseded: 001–006 and every existing table keep their exact
shape. The new tables are additive and standalone; the only "supersession" is the expected one
of the sequencing authority: B3's physical contract is now authored (pending PLAN ACCEPT) — no
schema statement of P2-T04/P2-T05/P2-T06 is reopened.

### 28.4 Safety rules / rollback

1. Purely additive (`CREATE TABLE` + `CREATE INDEX`); no `DROP`/`TRUNCATE`/type change/rename/
   `EnsureDeleted`; applying twice is a no-op.
2. `Down` drops the six tables in exact reverse dependency order (`boquilha_reopenings`,
   `boquilha_close_snapshots`, `boquilha_movement_audit`, `boquilha_movements`,
   `boquilha_machines`, `boquilhas`); re-apply idempotent.
3. PostgreSQL/Supabase-compatible by construction (portable features only; verified against the
   **disposable** PostgreSQL test database in implementation, never a live Supabase change — an
   operator's deployment step).

---

## 29. Test-to-acceptance matrix

Test classes follow the accepted naming (`<ID>_<PascalCaseClauses>`, XML summary naming the AC).
Classes: U = unit, I = integration/host (HTTP + gates + composition), DB = disposable PostgreSQL,
S = static/architecture scan, R = rendered. The matrix is bidirectional-complete against §32
(CRITERION ↔ PROOF); row ids are unique; every row maps to its same-named criterion (§32) — the
audit is mechanical.

#### IDENTITY

| # | Class | Test | Proves |
|---|---|---|---|
| I1 | DB | open production-linked: aggregate anchors the REAL `bq_id` (FK row in `bq_contexts`); `jobon_id` + canonical `tool_id` reachable through `bq_id`; no second `tool_id` on the aggregate; the frozen triple is presented from the context row | AC-I1 |
| I2 | DB | open standalone: aggregate anchors `tool_id` only; `bq_id` NULL; zero fake Job On/bq rows anywhere; movements attach `movement_id → boquilhas_id` | AC-I2 |
| I3 | DB/S | standalone and production-linked flows never create a fake `jobon_id`/`bq_id`: no P2-T07 write path inserts into `job_ons`/`bq_contexts` (BQ context creation composes `IJobOnService` only); a supplied non-existent `bq_id` → 400 `BQ_CONTEXT_NOT_FOUND` | AC-I3 |
| I4 | S | static scan of P2-T07 code/schema/contracts: no `production_id`, no `job_on_revision_id`, no reverse-ID arrays, no per-piece BQ UUID, no module-specific Tool id, no client-minted id | AC-I4 |
| I5 | DB | close then reopen → the SAME `boquilhas_id`; `boquilhas` row count unchanged; movements/snapshots/reopen records all point at that id; no replacement aggregate exists | AC-I5 |
| I6 | DB | `boquilhas` row count before/after a full close/reopen/close cycle is constant (no replacement); every close writes a new snapshot row on the same id | AC-I5, AC-C8 |
| I7 | DB | direct insert with both anchors or no anchor fails the `boquilhas_anchor_exclusive_check` (23514 → typed tokens, never 500); valid inserts with exactly one anchor succeed | AC-I7 |
| I8 | DB/S | `tool_id` semantics = canonical Tool (live `tools` facts via FK + traversal) and `bq_id` semantics = frozen Job On BQ context (frozen triple immutable after creation) — proven by schema shape + insert/read assertions | AC-I8 |

#### TOOL (shared orchestration)

| # | Class | Test | Proves |
|---|---|---|---|
| T1 | I | the Boquilhas picker query uses `type=BQ`: candidates returned are BQ Tools only (non-BQ Tools never appear); reference/lot/machine search facts filter server-side | AC-T1 |
| T2 | U/R | the picker never auto-selects — including exactly one candidate; Enter on search never selects; a single candidate is presented like many | AC-T2 |
| T3 | I/R | contextual `Criar ferramenta` (consumer-enabled) posts `POST /ferramentas/tools` (BQ type), returns the canonical `tool_id`, and the subflow returns to the SAME Boquilhas origin state (opening facts preserved; no navigation away; no origin value changed); cancel restores origin with no Tool | AC-T3 |
| T4 | S | no second Tool registry/picker: the only Tool search/create routes in the solution remain P2-T04's `/ferramentas/tools` (12/13); no P2-T07 Tool table/type/route exists | AC-T4 |
| T5 | I | Tool create command payload (type/reference/lot/machines ≥1) validated through the shared orchestration; `duplicate-identity` refusal honored on the origin surface | AC-T5 |
| T6 | U/R | ambiguous candidates stay explicit: N candidates are N items, never merged/ranked/hidden; the adapter never builds a selection from display text | AC-T6 |

#### MOVEMENT VOCABULARY

| # | Class | Test | Proves |
|---|---|---|---|
| V1 | DB/S | exactly the four type tokens are writable: `boquilha_movements_type_check` and the appendix type set contain only `inicio|saida|entrada|irreparavel` | AC-V1 |
| V2 | R/S | the movement selector exposes only Início/Saída/Entrada/Irreparável; no `Editar` entry/token/enum value exists anywhere (rendered test + scan) | AC-V2 |
| V3 | I | append with any other type string → 400 `validation-failed` `MOVEMENT_TYPE_INVALID`; nothing written | AC-V3 |
| V4 | DB | a second `inicio` append → 409 `only-one-inicio`; exactly one Início per aggregate (created at opening); the ledger shows one Início | AC-V4 |

#### BALANCE (derived only)

| # | Class | Test | Proves |
|---|---|---|---|
| B1 | S/DB | no second mutable balance authority: schema scan finds no balance table/column (`disponivel`/`em_reparacao`/`irreparavel`/`entrada_excecional` exist only in read models and the close snapshot); no P2-T07 code stores a derived total | AC-B1 |
| B2 | DB | multi-scenario replay: the invariant `Disponível + Em reparação + Irreparável = Σ(início)` holds and each bucket equals the §18 formulas for Início→Saída→Entrada→Irreparável sequences incl. excess returns | AC-B2 |
| B3 | DB | Saída with `Quantity > Disponível` → 409 `saida-exceeds-available`; zero rows written (ledger + version unchanged); a Saída exactly equal to Disponível succeeds | AC-B3 |
| B4 | DB | Irreparável with `Quantity > Em reparação` → 409 `irreparavel-exceeds-in-repair`; nothing written; an in-range value succeeds | AC-B4 |
| B5 | DB | excess Entrada is recorded: the row persists `quantity`, `expected_return_quantity` and `excess_received_quantity` exactly per §17.2; the bucket shows the accumulated excess | AC-B5 |
| B6 | I | excess Entrada is neither clamped nor rejected: a return above the expected amount saves normally (no refusal, no rewrite, no hidden discrepancy) | AC-B6 |
| B7 | DB/R | negative saldo (e.g. `Em reparação < 0` after excess Entrada) is derived and visible; no validation/CHECK/presentation blocks on it; the register renders the negative value non-blocking | AC-B7 |
| B8 | DB | zero balance never deletes/invalidates the aggregate: after full return the aggregate still exists, history is queryable, further valid movements remain possible | AC-B8 |
| B9 | DB | editing only `business_date` leaves every bucket unchanged (replay order is physical); balance equals the replay of the same ledger | AC-B9 (with AC-D3) |

#### EDIT / AUDIT

| # | Class | Test | Proves |
|---|---|---|---|
| E1 | DB | `Editar` updates the SAME `movement_id` row: movement row count unchanged (1), the row carries the new values, and exactly one audit row exists for the edit | AC-E1 |
| E2 | DB | the audit row carries the exact before/after values of every editable field (quantity/business_date/machine/repairer/observations) | AC-E2 |
| E3 | DB/S | no second quantity event: movement row count and the ledger replay prove a single event; audit rows are never rendered as movements and carry no balance effect | AC-E3 |
| E4 | DB | no double balance effect: the post-edit buckets equal the replay with the row's current values replaced by the new values (compare against a fresh replay of an equivalent ledger) | AC-E4 |
| E5 | DB | `edited_by_user_id`/`edited_at` are backend-authored (equal the authenticated backend user/clock); the edit carrier accepts no actor/time; a client-supplied actor/time is impossible | AC-E5 |
| E6 | DB/S | `movement_type` and `recorded_at` are immutable: no edit path, member or SQL writes them; `recorded_at` byte-equal across edits; invalid type edits impossible by carrier shape | AC-E6 |

#### DATES

| # | Class | Test | Proves |
|---|---|---|---|
| D1 | I | `business_date` is operator-editable at append and via `Editar` (a legitimate earlier/later date is accepted and stored); the operator-facing date filters use it | AC-D1 |
| D2 | DB | `recorded_at` is immutable: it is set once by the backend at insertion and is byte-identical across every later edit | AC-D2 |
| D3 | DB | changing `business_date` never rewrites `recorded_at` (asserted byte-equal) and never changes the balance/replay order | AC-D3 |

#### REPAIRER

| # | Class | Test | Proves |
|---|---|---|---|
| R1 | I/DB | machine → current assignment → repairer resolved automatically: the assignment read returns the machine's current assignment; the movement form pre-fills it and the saved external Saída stores it (final selected value; no forced re-typing) | AC-R1 |
| R2 | I/DB | B1/B2/B3/C1/C2/C3 resolve independently: assignments set per machine (P2-T05 test fixtures through the closed assignment state) produce per-machine resolutions; changing one machine's assignment changes no other machine's resolution | AC-R2 |
| R3 | DB | historical preservation: after a machine's assignment changes, an earlier movement's `repairer_id`/machine facts are unchanged; no P2-T07 write path re-resolves or rewrites stored movement repairer facts | AC-R3 |
| R4 | S | Boquilhas does not administer the register/assignments: no repairer/assignment write route, member, type or table in P2-T07; only consumed reads exist | AC-R4 |
| R5 | DB | external Saída requires `repairer_id` (+ machine): 400 `REPAIRER_REQUIRED`/`MACHINE_REQUIRED` with nothing written; the DB CHECK backstop rejects a direct insert (23514 → typed token) | AC-R5 |
| R6 | I | `assignment-unavailable` is an explicit state (no assignment row): never an error, never a default repairer, never conflated with a missing repairer; manual selection from the register read saves normally | AC-R6 |

#### UTILISATION (manual)

| # | Class | Test | Proves |
|---|---|---|---|
| U1 | S/DB | `% utilização` is manual only: no code derives it from movements/balance; no auto-increment; the live Ferramentas reading is never read for it; the aggregate carries the operator-entered still | AC-U1 |
| U2 | DB | the opening-facts update (opening date / utilisation / observations / machine set) changes no movement and no balance bucket; movement `machine` facts untouched | AC-U2 |
| U3 | R | the summary renders `% utilização` as a number — never a progress bar; a closed aggregate shows the snapshot still | AC-U3 |

#### CLOSE / REOPEN

| # | Class | Test | Proves |
|---|---|---|---|
| C1 | DB | the close snapshot is immutable: its exact columns (buckets at close, utilisation still, opening date, initial quantity, closed_by/closed_at) are frozen; no UPDATE/DELETE path exists; later edits/movements never modify it | AC-C1 |
| C2 | DB | failed close is atomic: a forced mid-transaction failure leaves `status='active'`, no snapshot row, no version bump, no partial state | AC-C2 |
| C3 | DB | after close the aggregate leaves the active predicate (Registo grid list) and appears with state `closed` in the archived/Histórico projection | AC-C3 |
| C4 | DB | reopen returns the SAME `boquilhas_id` to `active`; row count unchanged; no replacement aggregate; movements and all history remain attached | AC-C4 |
| C5 | DB | reopen records actor/time/reason as backend facts; a blank reason → 400 `REOPEN_REASON_REQUIRED` with nothing written; the reopen row references the exact close being reopened | AC-C5 |
| C6 | DB | reopen eligibility: `not-closed` (already active), `not-last-closed` (a later close exists on another aggregate sharing the anchor), `active-aggregate-exists` (another active aggregate shares the anchor) — all refused with nothing written; the eligible case succeeds | AC-C6 |
| C7 | DB | history is preserved across close/reopen: movements, movement audit, close snapshots and reopen records are all intact and ordered; nothing is rewritten or deleted | AC-C7 |
| C8 | DB | close/reopen cycles on the same id: each close writes a NEW immutable snapshot; reopenings reference their exact close; the aggregate id never changes | AC-C8 |

#### HISTORY (local)

| # | Class | Test | Proves |
|---|---|---|---|
| H1 | I | every §24.2 filter (state, reference, lot, machine, business-date range, movement type, repairer, paging) is backend-applied; unknown/ill-formed values → 400 `FILTER_INVALID`; empty = explicit empty | AC-H1 |
| H2 | S | no `historia` (HISTÓRICO GLOBAL) route/entry/registration exists; the Boquilhas History lives only inside the module (route 3/18) | AC-H2 |
| H3 | R | History/lot/movement tables follow the accepted interaction: single click selects, double click opens the exact item, actions live outside the table, no per-row action button grid | AC-H3 |
| H4 | I | rows carry the exact `boquilhas_id` + anchor/state/balance facts; opening a row loads the exact aggregate (same id, same facts); movement audit opens the exact movement trail | AC-H4 |
| H5 | I | paging is 1-based with deterministic ordering (`created_at DESC, boquilhas_id DESC`); `pageSize` 1–100 (400 otherwise); `total` is the backend count for the same predicate | AC-H5 |
| H6 | R/I | empty ≠ lookup-failed ≠ permission-denied across grid/History/movement reads; a denied caller never receives a blank/empty presentation | AC-H6 |

#### ACCESS

| # | Class | Test | Proves |
|---|---|---|---|
| A1 | S | every P2-T07 route/action carries exactly `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas)`; page constants pinned in `BoquilhasPolicyNames` and asserted equal to the canonical projection; no second policy anywhere | AC-A1 |
| A2 | I | grants to unrelated modules (`job-on-view`, `job-on-create`, `controlo-create`, `controlo-approve`, `ferramentas`, `ferramentas-approve`) never satisfy any P2-T07 route (direct-route server-side denial) | AC-A2 |
| A3 | I | a non-granted caller is denied every P2-T07 route by direct URL (server-side, never by hiding); an unknown/absent module availability still denies (test-only registries) | AC-A3 |
| A4 | I | ADMIN is denied every P2-T07 route (no super-user) | AC-A4 |
| A5 | R/I | denial is never an empty list/blank surface: a denied caller receives the documented 403 presentation on every P2-T07 route | AC-A5 |
| A6 | S | no P2-T07 route carries a second policy; Boquilhas reads Job On/Tool/repairer facts only through accepted application contracts under Boquilhas-gated routes; no P2-T07 code calls another module's gated HTTP route | AC-A6 |

#### CONCURRENCY

| # | Class | Test | Proves |
|---|---|---|---|
| K1 | DB | every guarded operation (append/edit/close/reopen/opening-facts) with a stale aggregate version → 409 `stale-version`; nothing written (rows, versions, snapshots unchanged) | AC-K1 |
| K2 | DB | movement edit with a stale movement version → 409 `stale-version`; the movement row and audit table unchanged | AC-K2 |
| K3 | DB | forced mid-transaction failure in append, edit, close and reopen → zero partial state (no movement, no audit, no snapshot, no reopen record, no version bump, status unchanged) | AC-K3 |
| K4 | DB | save-time race (second connection bumps the aggregate version mid-save) → 409 `stale-version` via the `SaveAsync` mapping; no silent overwrite of the newer state | AC-K4 |
| K5 | R | on `stale-version` the surface enters the accepted conflict presentation with the explicit "Recarregar estado atual" recovery; no auto-retry, no auto-merge; observed versions refresh only on success (D2 pattern) | AC-K5 |
| K6 | DB | **B1 correction — production-linked create race:** two concurrent creates for the SAME `bq_id` against a real PostgreSQL database → exactly ONE succeeds (201); exactly ONE returns 409 `active-aggregate-exists` (either via the application pre-check or via the DB partial unique index `IX_boquilhas_active_bq_id` mapped from 23505 — the test asserts the outcome, not which mechanism fired, and asserts the loser's refusal is the typed token, never a 500); end state: exactly ONE active aggregate, exactly ONE committed Início event, machine rows only for the winner, ZERO partial/orphan state | AC-C6, AC-K3, AC-K4 |
| K7 | DB | **B1 correction — standalone create race:** same concurrent race for the SAME `tool_id` via `IX_boquilhas_active_tool_id`; identical expected outcome (one 201, one 409 `active-aggregate-exists`, one active aggregate, one Início, no orphan state) | AC-C6, AC-K3, AC-K4 |
| K8 | DB | **B1 correction — create-vs-reopen race:** a reopen of the last-closed aggregate for an anchor races a create for the SAME anchor → at most one active aggregate results: if the reopen commits first the create refuses (pre-check or 23505); if the create commits first, the reopen's status UPDATE raises 23505 on the relevant active-anchor index → 409 `active-aggregate-exists` and the reopen transaction rolls back COMPLETELY — the aggregate remains `closed`, its close snapshot, reopen records and all history intact, and NO partial reopening record exists | AC-C6, AC-C7, AC-K3 |

#### MIGRATION / SCHEMA

| # | Class | Test | Proves |
|---|---|---|---|
| MG1 | DB | migration 007 applies over 001–006; exactly six new tables; migrations 001–006 and `DmoDbContext.cs` byte-identical; `Down` drops the six tables in inverse order; re-apply idempotent; post-migration table count 27 | AC-MG1 |
| MG2 | DB | the six tables' columns/CHECKs/FKs/indexes match §6/§7 exactly (constraint names, `confdeltype='r'`, the anchor-exclusive CHECK, the Saída-required CHECK, the Entrada-facts CHECK, no cascade); direct-violation inserts map 23514 → typed tokens, never 500 | AC-MG2 |
| MG3 | DB/S | immutability by construction: no UPDATE/DELETE primitive exists for `boquilha_movement_audit`, `boquilha_close_snapshots`, `boquilha_reopenings` (route + repository reflection); `recorded_at` never rewritten; no balance table/column in the migration | AC-MG3 |

#### FIXED DESKTOP

| # | Class | Test | Proves |
|---|---|---|---|
| L1 | S | static scan of P2-T07-owned assets (`dmo-boquilhas.css`, `.js`, the three pages): no `@media`/`@container`/`@supports` structural rule, no width listener, no table→card conversion, no required-column hiding, no action relocation | AC-L1 |
| L2 | R | Registo/Novo/Histórico render at the canonical 1366 × 768 validation viewport with region-stable composition (R1–R6, N1–N4, H1–H3); a larger desktop preserves the composition; `% utilização` renders as a number, never a progress bar; over-wide regions scroll locally, keyboard-reachable | AC-L2 |
| L3 | S/R | no shared shell/header/navigation file is modified; the pages render inside the existing shell unchanged | AC-L3 |

#### BOUNDARIES

| # | Class | Test | Proves |
|---|---|---|---|
| N1 | S | no Boquilhas settings/Admin tab: no repairer register administration, no machine-assignment writes, no PDF-directory, no email list/template, no glass/water density code, route or type under Boquilhas | AC-N1 |
| N2 | S | no PDF/file/email code: no `Infrastructure/Files`/`Pdf` adapter, no SMTP/email, no directory write, no document identity, no mandatory Boquilhas PDF | AC-N2 |
| N3 | S | no `historia` (HISTÓRICO GLOBAL) route/entry/registration | AC-N3 |
| N4 | S | `ModuleRegistrations.CurrentBuildAvailable` stays `[]`; `DestinationRouteRegistrations` empty; no navigation/availability registration | AC-N4 |
| N5 | R/S | no machine/reference sidebar simulating Job On state is rendered; the contextual panel renders real production context (linked) / the registered machine set (standalone) only | AC-N5 |
| N6 | S/DB | no fifth movement type, no legacy types (`contagem`, "Fabricar/Reparar", …) and no annulment/delete path exist in code, schema or routes | AC-N6 |
| N7 | S | protected files regression: migrations 001–006, `DmoDbContext.cs`, P2-T04/P2-T05/P2-T06 sources and shared/frozen artifacts byte-identical; existing tests keep passing (never weakened) | AC-N7 |

**Completeness (mechanical audit):** the matrix is bidirectional:

- **Acceptance criteria: 83** — AC-I1…AC-I8 (8), AC-T1…AC-T6 (6), AC-V1…AC-V4 (4),
  AC-B1…AC-B9 (9), AC-E1…AC-E6 (6), AC-D1…AC-D3 (3), AC-R1…AC-R6 (6), AC-U1…AC-U3 (3),
  AC-C1…AC-C8 (8), AC-H1…AC-H6 (6), AC-A1…AC-A6 (6), AC-K1…AC-K5 (5), AC-MG1…AC-MG3 (3),
  AC-L1…AC-L3 (3), AC-N1…AC-N7 (7) = 8+6+4+9+6+3+6+3+8+6+6+5+3+3+7 = **83**; each criterion
  exists exactly once in §32 (no aliases; **no new criterion was added by the B1 correction** —
  the added test rows map to the existing AC-C6/AC-C7/AC-K3/AC-K4 criteria).
- **Test rows: 86** — IDENTITY 8, TOOL 6, MOVEMENT VOCABULARY 4, BALANCE 9, EDIT 6, DATES 3,
  REPAIRER 6, UTILISATION 3, CLOSE/REOPEN 8, HISTORY 6, ACCESS 6, **CONCURRENCY 8 (K1–K8)**,
  MIGRATION 3, FIXED DESKTOP 3, BOUNDARIES 7 = 8+6+4+9+6+3+6+3+8+6+6+8+3+3+7 = **86**; all
  row ids unique; each row maps to ≥ 1 criterion that exists in §32 (the added K6–K8 rows map
  to the existing active-aggregate/concurrency criteria).
- **missing = 0** (every criterion maps to ≥ 1 row — exactly its own row), **dangling = 0**
  (every row maps only to keys that exist in §32), **orphan = 0** (no row without a criterion;
  the boundary/negative rows are scans of real absence, not tests of other workstreams'
  behavior).
- The row↔AC key is re-verified mechanically in the eventual implementation response (P2-T04
  §20.12 discipline).

---

## 30. Authority questions

### 30.1 BLOCKING — NONE

There are **no BLOCKING authority questions**. Every open point has a pinned default consistent
with the closed P2-T04/P2-T05/P2-T06 state and the settled delta (see §30.2; summary §31).

### 30.2 NON-BLOCKING / OWNER-LEVEL (genuine open points only — closed rules are NOT re-asked)

| # | Question | Authority gap | Pinned default | Impact | Class |
|---|---|---|---|---|---|
| Q-EDIT-FIELDS | Which movement fields does `Editar` change? | "Editar changes an existing movement" (global §6; beta) without enumerating fields; audit must capture before/after | **quantity, business_date, machine, repairer_id, observations editable; movement_type and recorded_at immutable** (§19) — errors of type are corrected by a registered future annulment if authority ever adds one; nothing is silently deletable | command carrier + audit columns | ACCEPT DEFAULT |
| Q-MACHINE | Is `machine` mandatory on external Saída? | automatic resolution requires "a movement associated with a machine" (`…DELTA.md` §5); no mandatory movement-machine rule | **external Saída requires `machine` (from the aggregate's registered set)** so resolution operates; other types optional line context (§21) | movement CHECK + validator | ACCEPT DEFAULT |
| Q-INICIO | When is the Início movement created, and may more Inícios be appended? | "Total do lote — the initial quantity … (the Início movement)" (global §5); no append rule | **created with the aggregate in the create transaction; later `inicio` appends refused (`only-one-inicio`)** (§17, §22.2) | create transaction + append guard | ACCEPT DEFAULT |
| Q-EXCESS | How is the excess-Entrada fact persisted? | global §9 lists `expected_return_quantity`/`excess_received_quantity` as movement facts; beta says "movement fact/projection"; no physical rule | **persisted on the Entrada row, computed in the append transaction by replay; the Entrada excecional bucket = the sum** (§17.2, §18) | Entrada CHECK + row | ACCEPT DEFAULT |
| Q-ORDER | What replay order derives the balance? | movement facts are the authority; `business_date` editable; `recorded_at` immutable; no order rule | **`recorded_at ASC, movement_id ASC` (physical receipt order)**; editing `business_date` never reorders or changes balance (§18) | derivation contract | ACCEPT DEFAULT |
| Q-CREATE | May a second ACTIVE aggregate be opened for the same BQ Tool context? | reopen is "only when no other active trace exists for the same BQ Tool context" (global §7); creation rule silent | **creation refused while an active aggregate exists for the same anchor (`active-aggregate-exists`)**; after close a new trace or a reopen is possible (§23) — race-safe enforcement via the §7.2 partial unique index backstop (B1 correction; §§11, 22.2) | create refusal + partial unique indexes | ACCEPT DEFAULT |
| Q-REOPEN-ELIG | Exact reopen eligibility algorithm | "only for the last closed trace and only when no other active trace exists for the same BQ Tool context" (global §7); "where the settled aggregate rules permit" (beta §9) | **closed ∧ this aggregate holds the most recent close among aggregates sharing its anchor ∧ no other active aggregate shares its anchor** (§23.3) | reopen preconditions | ACCEPT DEFAULT |
| Q-REFLOT | Are reference/lot duplicated on the aggregate? | global §5 lists them as opening fields; §5 also forbids a manual copy; master plan §8 forbids duplicate master data | **no copy — reached through the anchor** (live `tools` standalone / frozen `bq_contexts` triple linked); filters are traversal (§15.3, §24) | schema + filters | ACCEPT DEFAULT |
| Q-UTIL | Shape of the manual utilisation still | "initial utilisation as a manual still where applicable"; "opening/closing … manual stills"; never derived/synced | **one manual `utilisation_percent` (0–100 or NULL) on the aggregate, editable, captured as-is in the close snapshot; never a progress bar** (§23) | aggregate column + snapshot | ACCEPT DEFAULT |
| Q-LINE | Is a "Linha atual" context value modelled? | global §8 names it a context value; Beta opening facts are machine(s)/line(s) multi-select; no field fixed | **no dedicated column — line context = the aggregate's machine set + per-movement `machine` facts** (§17.3, §23.4) | schema (absent) | ACCEPT DEFAULT |
| Q-CLOSE-DATE | Close date/time — operator or backend? | "closing date/time and closing user" (global §7) | **backend facts** (`closed_at` backend clock, `closed_by_user_id` current account), inside the immutable snapshot; no operator close date invented (§23.2) | snapshot columns | ACCEPT DEFAULT |
| Q-ANUL | Does P2-T07 contract an annulment/removal action? | beta: "Any annulment/removal supported by the backend is a registered, confirmed fact" (conditional); no requirement | **no annulment carrier in P2-T07** — `Editar` (audited replace) is the correction mechanism; a future annulment needs its own authority and must be registered + confirmed, never a delete (§19.3) | absent routes/columns | ACCEPT DEFAULT |

---

## 31. Authority questions — summary

**BLOCKING: 0.** **REQUIRES OWNER DECISION: 0.** **NON-BLOCKING: 12** (Q-EDIT-FIELDS,
Q-MACHINE, Q-INICIO, Q-EXCESS, Q-ORDER, Q-CREATE, Q-REOPEN-ELIG, Q-REFLOT, Q-UTIL, Q-LINE,
Q-CLOSE-DATE, Q-ANUL), each with a pinned default already reflected in the schema/interfaces/
routes above; none blocks the PLAN REVIEW gate. If the Architect rejects a pinned default, the
contract returns `CORRECTION REQUIRED` for that item. Closed rules (standalone validity,
repairer ownership, machine independence, historical repairer preservation, movement
vocabulary, no fake Job On, Editar not a type, close/reopen same identity, movement facts as
balance authority) are **not** re-asked — they are consumed as settled.

**B1 is not an authority question.** The Architect's blocking finding (`542a08a1…`) was a
concurrency implementation-contract defect (race-safe enforcement of the already-pinned
Q-CREATE/Q-REOPEN-ELIG rule), corrected here by the §7.2 partial unique indexes + the exact
23505 mapping (§7, §8, §10, §11, §22, §23, §28, §29, App. D.5). The 12 dispositions above are
**unchanged: 12 ACCEPT DEFAULT / 0 REQUIRES OWNER DECISION / 0 BLOCKING.**

---

## 32. Implementation acceptance criteria

P2-T07 is acceptable only when every criterion below is satisfied and proven by §29.

### Identity (AC-I1 … AC-I8)

| # | Criterion |
|---|---|
| AC-I1 | A production-linked aggregate anchors the REAL `bq_id` (a `bq_contexts` row); `jobon_id` and the canonical `tool_id` are reachable through it; nothing is duplicated for navigation. |
| AC-I2 | A standalone aggregate anchors `tool_id` only, with no fake Job On and no fake `bq_id`; both flows are first-class. |
| AC-I3 | BQ context creation is composed exclusively through `IJobOnService` (no P2-T07 write to `job_ons`/`bq_contexts`); a supplied non-existent `bq_id` is refused (`BQ_CONTEXT_NOT_FOUND`). |
| AC-I4 | No `production_id`, `job_on_revision_id`, reverse-ID array, per-piece BQ UUID, module-specific Tool id or client-minted id exists anywhere in P2-T07. |
| AC-I5 | Close/reopen operate on the SAME `boquilhas_id`; no replacement aggregate is ever created. |
| AC-I6 | Multiple close/reopen cycles keep the same aggregate identity and produce one immutable snapshot per close. |
| AC-I7 | The aggregate anchors to exactly one of `bq_id`/`tool_id`, DB-enforced (exclusive-anchor CHECK). |
| AC-I8 | `tool_id` = canonical Tool and `bq_id` = frozen Job On BQ context — distinct semantics, both consumed, never competing identities. |

### Tool orchestration (AC-T1 … AC-T6)

| # | Criterion |
|---|---|
| AC-T1 | Boquilhas searches/selects BQ Tools only — candidates filtered by the authoritative BQ tool type. |
| AC-T2 | The picker never auto-selects, including exactly one candidate. |
| AC-T3 | Contextual `Criar ferramenta` returns the canonical `tool_id` and restores the same Boquilhas origin state; cancel restores origin with no Tool. |
| AC-T4 | No second Tool picker/registry exists; the shared orchestration's `ferramentas` routes are the only Tool search/create surface. |
| AC-T5 | Tool creation through the shared orchestration is validated (type/reference/lot/machines) and duplicates are refused. |
| AC-T6 | Ambiguous candidates remain explicit; a selection is never built from display text. |

### Movement vocabulary (AC-V1 … AC-V4)

| # | Criterion |
|---|---|
| AC-V1 | Exactly the four movement types are writable (`inicio|saida|entrada|irreparavel`), DB-CHECK-enforced. |
| AC-V2 | The movement selector exposes only Início/Saída/Entrada/Irreparável; `Editar` is never a movement type. |
| AC-V3 | Any other type value is refused (`MOVEMENT_TYPE_INVALID`) with nothing written. |
| AC-V4 | Exactly one Início per aggregate, created at opening; later Início appends are refused. |

### Balance (AC-B1 … AC-B9)

| # | Criterion |
|---|---|
| AC-B1 | Balance is derived from movement facts only; no second mutable balance table/column exists anywhere. |
| AC-B2 | The buckets match the exact §18 replay with the invariant `Disponível + Em reparação + Irreparável = Σ(início)`. |
| AC-B3 | Saída cannot exceed the available quantity (`saida-exceeds-available`; nothing written). |
| AC-B4 | Irreparável cannot exceed the quantity in repair (`irreparavel-exceeds-in-repair`; nothing written). |
| AC-B5 | Excess Entrada is recorded (expected/excess facts on the movement; bucket accumulated by sum). |
| AC-B6 | Excess Entrada is never clamped and never rejected for exceeding the expected repair quantity. |
| AC-B7 | Negative saldo remains visible and non-blocking. |
| AC-B8 | Zero balance never deletes or invalidates the aggregate; history stays queryable. |
| AC-B9 | Editing `business_date` never changes the balance (replay order is physical). |

### Edit / audit (AC-E1 … AC-E6)

| # | Criterion |
|---|---|
| AC-E1 | `Editar` modifies the SAME `movement_id` row; no second movement row is created. |
| AC-E2 | Every edit persists an audit row with the exact before/after values. |
| AC-E3 | An edit is audit history, never a second quantity event. |
| AC-E4 | An edit never double-counts: the post-edit balance equals a replay with the row's values replaced; validations are re-run (Saída ≤ Disponível, Irreparável ≤ Em reparação under the new values). |
| AC-E5 | Edit actor/time are backend-authored facts; the carrier accepts neither. |
| AC-E6 | `movement_type` and `recorded_at` are immutable; no path writes them. |

### Dates (AC-D1 … AC-D3)

| # | Criterion |
|---|---|
| AC-D1 | `business_date` is operator-editable (append and edit); operational calendars/filters use it. |
| AC-D2 | `recorded_at` is an immutable backend timestamp. |
| AC-D3 | Changing `business_date` never rewrites `recorded_at` and never changes the balance. |

### Repairer (AC-R1 … AC-R6)

| # | Criterion |
|---|---|
| AC-R1 | Machine → current assignment → repairer resolves automatically (suggested value); the operator is not forced to re-select when the machine already determines it; the saved external Saída stores the final selected `repairer_id`. |
| AC-R2 | B1/B2/B3/C1/C2/C3 resolve independently; changing one assignment changes no other machine's resolution. |
| AC-R3 | A later assignment change never rewrites an earlier movement's repairer (historical preservation on the movement row). |
| AC-R4 | Boquilhas administers neither the register nor the assignments (consume-only). |
| AC-R5 | Every external Saída stores a canonical `repairer_id` (+ machine); missing → typed refusal + DB backstop. |
| AC-R6 | `assignment-unavailable` is an explicit non-error state; manual selection from the register works. |

### Utilisation (AC-U1 … AC-U3)

| # | Criterion |
|---|---|
| AC-U1 | `% utilização` is manual only — never derived from movements, never auto-incremented, never synced. |
| AC-U2 | Opening-facts updates (opening date / utilisation / observations / machine set) never change movements or balance. |
| AC-U3 | `% utilização` renders as a number, never a progress bar; the close snapshot captures the manual still. |

### Close / reopen (AC-C1 … AC-C8)

| # | Criterion |
|---|---|
| AC-C1 | Close writes an immutable snapshot (summary, current state, close metadata, closing date/time and closing user); no UPDATE/DELETE path exists. |
| AC-C2 | A failed close is atomic: the aggregate stays active with no partial snapshot. |
| AC-C3 | Close moves the aggregate out of the active lists into the archived/Histórico projection. |
| AC-C4 | Reopen returns the SAME `boquilhas_id` to active; no replacement aggregate. |
| AC-C5 | Reopen records actor/time/reason as backend facts; reason required. |
| AC-C6 | Reopen eligibility is exact: closed; last-closed for the same BQ Tool context; no other active aggregate for the same context — refusals write nothing. |
| AC-C7 | Movements, audit, snapshots and reopen records are preserved across close/reopen; nothing rewritten. |
| AC-C8 | Close/reopen cycles keep the same identity; each close produces a new immutable snapshot. |

### History (AC-H1 … AC-H6)

| # | Criterion |
|---|---|
| AC-H1 | The local Histórico filters are the §24.2 set, backend-applied; invalid filters refused. |
| AC-H2 | The History is HISTÓRICO (local) inside Boquilhas — never conflated with HISTÓRICO GLOBAL (`historia`, deferred); no such route/entry exists. |
| AC-H3 | Tables are selection surfaces: single click selects, double click opens the exact item, actions live outside the table; no per-row action button grid. |
| AC-H4 | Rows carry the exact `boquilhas_id` and facts; opening loads the exact record/trail. |
| AC-H5 | Paging is 1-based, bounded, deterministic; `total` is backend-counted. |
| AC-H6 | Empty ≠ lookup-failed ≠ permission-denied across all P2-T07 reads. |

### Access (AC-A1 … AC-A6)

| # | Criterion |
|---|---|
| AC-A1 | Every P2-T07 route/action carries exactly `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas)`. |
| AC-A2 | A grant to any unrelated module never satisfies a P2-T07 route. |
| AC-A3 | Direct-route denial is server-side (direct URL denied for non-granted callers). |
| AC-A4 | ADMIN gains no operational Boquilhas access. |
| AC-A5 | Denial is never an empty list/blank surface. |
| AC-A6 | No route carries a second policy; cross-module reads use accepted application contracts under Boquilhas-gated routes; no cross-module HTTP calls. |

### Concurrency (AC-K1 … AC-K5)

| # | Criterion |
|---|---|
| AC-K1 | Append/edit/close/reopen/opening-facts are aggregate-version-guarded: stale → 409 `stale-version`, nothing written. |
| AC-K2 | Movement edit is additionally movement-version-guarded; a second concurrent editor fails closed. |
| AC-K3 | No partial mutation on any failure (atomicity across ledger/audit/snapshot/reopen). |
| AC-K4 | Save-time races surface as 409 via the accepted mapping — no silent overwrite, no auto-retry, no merge. |
| AC-K5 | Conflict/reload recovery matches the accepted D2 behavior (conflict presentation + explicit reload; versions refresh only on success). |

### Migration / schema (AC-MG1 … AC-MG3)

| # | Criterion |
|---|---|
| AC-MG1 | Exactly ONE new additive migration (007) owns exactly the six contracted tables; migrations 001–006 and `DmoDbContext.cs` byte-identical; `Down` exact inverse; re-apply idempotent. |
| AC-MG2 | The six tables enforce the exact columns/CHECKs/FKs (all RESTRICT)/indexes of §6/§7; constraint violations map to typed tokens. |
| AC-MG3 | Audit/snapshot/reopen rows are immutable/append-only; `recorded_at` never rewritten; no balance table exists. |

### Fixed desktop (AC-L1 … AC-L3)

| # | Criterion |
|---|---|
| AC-L1 | Registo/Novo/Histórico are designed/validated at 1366 × 768 with region-stable composition; no breakpoint variant, no relocation, no card conversion, no column hiding. |
| AC-L2 | Over-wide regions use keyboard-reachable local overflow; `% utilização` is a number, never a progress bar; no required column hidden, no action moved between regions at any desktop width. |
| AC-L3 | The shared header/navigation system is untouched (existing shell consumed as-is). |

### Boundaries (AC-N1 … AC-N7)

| # | Criterion |
|---|---|
| AC-N1 | No Boquilhas settings/Admin tab and no administration of repairers, machine assignments, PDF directory, email lists/templates or densities. |
| AC-N2 | No PDF generation, file storage/writing, directory handling, document identity or email sending exists in P2-T07. |
| AC-N3 | No HISTÓRICO GLOBAL (`historia`) route/entry/registration. |
| AC-N4 | `ModuleRegistrations.CurrentBuildAvailable` stays `[]`; no route/destination/navigation registration. |
| AC-N5 | No machine/reference sidebar simulating Job On state is rendered; the production-line contextual panel reads real context only. |
| AC-N6 | No fifth movement type, no legacy type and no annulment/delete path exists. |
| AC-N7 | All protected/closed files and existing tests remain byte-identical/green (no regression, no weakening). |

**Count: 83 criteria** (AC-I1…I8, AC-T1…T6, AC-V1…V4, AC-B1…B9, AC-E1…E6, AC-D1…D3,
AC-R1…R6, AC-U1…U3, AC-C1…C8, AC-H1…H6, AC-A1…A6, AC-K1…K5, AC-MG1…MG3, AC-L1…L3,
AC-N1…N7) ↔ the §29 rows (**86 rows** — 83 same-named + the B1-correction concurrency rows
K6–K8 mapping to AC-C6/AC-C7/AC-K3/AC-K4; audit in §29: **missing 0, dangling 0, orphan 0**).

---

## Appendix A — Protected boundaries

### A.1 Must not be touched by P2-T07

```text
src/DMO.Application/Access/**                          (catalog, registry, resolver, availability)
src/DMO.Web/Authorization/**                           (module/administration gates)
src/DMO.Web/Auth/*, src/DMO.Web/Startup/*, src/DMO.Application/Accounts/*, Session/*
src/DMO.Infrastructure/Persistence/DmoDbContext.cs
src/DMO.Infrastructure/Migrations/20260922001736_*, 20260922001757_*,
   20260922232349_ToolJobOnDomainCore.*, 20260923045054_ControloCreateDomain.*,
   20260923122429_GlassDensitySettings.*, 20260923171223_ControloApproveDomain.*
   (and their Designers)                              (migrations 001–006 byte-identical)
src/DMO.Application/Tools/**, src/DMO.Application/JobOn/**, src/DMO.Application/ControloCreate/**,
   src/DMO.Application/ControloApprove/**            (closed application surfaces)
src/DMO.Application/Repositories/IToolRepository.cs, IJobOnRepository.cs, IPesoRepository.cs,
   IRepairerRepository.cs, IMachineRepairerAssignmentRepository.cs (+ siblings; read-only consumables)
src/DMO.Infrastructure/Persistence/Entities/* , EntityConfigurations/* (P2-T04/P2-T05/P2-T06)
src/DMO.Infrastructure/Persistence/{Tool,JobOn,Peso,Repairer,MachineRepairerAssignment,…}Repository.cs
src/DMO.Web/Endpoints/{JobOnEndpoints,FerramentasEndpoints,ControloCreateEndpoints,
   ControloDefinicoesEndpoints,ControloApproveEndpoints,AuthEndpoints,…}.cs
src/DMO.Web/Pages/JobOn/**, src/DMO.Web/Pages/Ferramentas/**, src/DMO.Web/Pages/Controlo/**,
   {JobOnPolicyNames,ControloPolicyNames,ControloApprovePolicyNames}.cs
src/DMO.Web/wwwroot/css/{dmo-tokens,dmo-shell,dmo-user-shell,dmo-components,dmo-jobon,
   dmo-controlo,dmo-controlo-approve}.css   src/DMO.Web/wwwroot/js/{dmo-focus,dmo-dense-table,
   dmo-tool-picker,dmo-measurement-rows,dmo-jobon,dmo-controlo,dmo-controlo-approve}.js
src/DMO.Web/Frontend/Shell/**, src/DMO.Web/Frontend/Shared/**, src/DMO.Web/Pages/Shared/**
src/DMO.Web/Navigation/DestinationRouteRegistrations.cs
src/DMO.Web/Pages/{Index,Login,AccessDenied}.*, src/DMO.Web/Pages/Administration/**
docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md
plans/contracts/P2-T02_*.md, P2-T03_*.md, P2-T04_*.md, P2-T05_*.md, P2-T06_*.md
tests/**                                       (existing tests keep passing, never weakened)
```

The only accepted non-new files P2-T07 changes: `src/DMO.Web/Program.cs` (additive
`MapBoquilhasEndpoints()` + service registrations + the one additive probe registration line),
`PersistenceServiceCollectionExtensions.cs` (additive repository registration),
`DmoDbContextModelSnapshot.cs` (EF-generated snapshot extension), and the governance documents
of Appendix D.

### A.2 Accepted input, consume only

`ModuleCatalog` (`Boquilhas`), `ModuleRegistrations` (`[]`), the module gates,
authentication/session/`ICurrentAccountContext`, the closed P2-T04 application contracts
(`IToolService`, `IJobOnService`, `MachineCode`), the closed P2-T05 application contracts
(`IRepairerRepository` read, `IMachineRepairerAssignmentRepository` read), the accepted
`ConcurrencyConflictExceptionMapping`/`SaveAsync` pattern, migrations 001–006, the shared
P2-T01/T02/T03 primitives (`DenseDataTable`, `AuditTrail`, `DecisionBar`, `ToolPicker`,
`RecordStatus`, `CommonState`), the A1 freeze, and the `ferramentas`-gated shared Tool routes
(browser-side orchestration only).

### A.3 Downstream boundaries (must be preserved)

No P2-T07 artifact names, imports, renders, fixtures or tests: a settings/Definições surface, a
repairer/assignment write surface, a PDF/file/email/send surface, a document identity, a
balance table/column, a machine/reference sidebar simulating Job On, a fifth movement type, an
annulment/delete path, availability/navigation registration, `historia` (HISTÓRICO GLOBAL),
`production_id`/`job_on_revision_id`/fake `bq_id`/fake `jobon_id`, reverse-ID arrays, or a
second Tool registry. The B3 contract status is CORRECTED (B1) — AWAITING FOCUSED ARCHITECT
PLAN RE-REVIEW (PLAN REJECT `542a08a1…`, B1 applied) until the Architect decides; P2-T08/P2-T10
receive only the seams of §27.

---

## Appendix B — Implementation file ownership / expected paths

```text
src/DMO.Domain/Boquilhas/            BoquilhasId.cs, MovementId.cs, MovementKind.cs,
                                     BoquilhaAggregate.cs, BoquilhaMovement.cs,
                                     MovementAuditEntry.cs, CloseSnapshot.cs,
                                     ReopeningRecord.cs, BalanceProjection.cs
src/DMO.Application/Repositories/    IBoquilhasRepository.cs
src/DMO.Application/Boquilhas/       BoquilhasModels.cs (commands/queries),
                                     BoquilhasValidator.cs,
                                     IBoquilhasService.cs, BoquilhasService.cs,
                                     BoquilhasReadModels.cs (ficha/list/history/audit/resolution)
src/DMO.Infrastructure/Persistence/  Entities/{Boquilha,BoquilhaMachine,BoquilhaMovement,
                                     BoquilhaMovementAudit,BoquilhaCloseSnapshot,
                                     BoquilhaReopening}Entity.cs,
                                     EntityConfigurations/{…}Configuration.cs,
                                     BoquilhasRepository.cs,
                                     BoquilhasDependencyProbe.cs,
                                     Migrations/<timestamp>_BoquilhasDomain.cs (+Designer),
                                     Migrations/DmoDbContextModelSnapshot.cs (EF extension),
                                     PersistenceServiceCollectionExtensions.cs (additive)
src/DMO.Web/Endpoints/               BoquilhasEndpoints.cs
src/DMO.Web/Pages/Boquilhas/         Index.cshtml(.cs), Novo.cshtml(.cs), Historico.cshtml(.cs),
                                     BoquilhasPolicyNames.cs
src/DMO.Web/wwwroot/css/dmo-boquilhas.css (new)
src/DMO.Web/wwwroot/js/dmo-boquilhas.js   (only what is genuinely required; reuses window.dmoFocus)
src/DMO.Web/Program.cs               (additive registrations + MapBoquilhasEndpoints +
                                     one additive IJobOnDependencyProbe registration line)
tests/DMO.UnitTests/Boquilhas/**     tests/DMO.IntegrationTests/Boquilhas/**,
tests/DMO.IntegrationTests/Persistence/ (movement repository + migration tests),
tests/DMO.IntegrationTests/Boquilhas/P2T07RegressionTests.cs, P2T07ProductionScan.cs
```

---

## Appendix C — Fixed desktop obligations

Restated for implementers: §25 rules are binding; canonical validation viewport 1366 × 768;
compact density; region-stable composition (R1–R6, N1–N4, H1–H3); no
`@media`/`@container`/`@supports` structural rule in `dmo-boquilhas.css`; local
keyboard-reachable overflow for wide regions; no mobile/tablet variants; shared
header/navigation untouched; `% utilização` never rendered as a progress bar; selection/open
table semantics (single click selects, double click opens, actions outside the table) on every
grid including the movement ledger.

---

## Appendix D — Governance record

### D.1 Status recorded by this task

| Item | Status |
|---|---|
| P2-T07 | **CONTRACT CORRECTED (B1) — AWAITING FOCUSED ARCHITECT PLAN RE-REVIEW** (PLAN REJECT `542a08a1…`; B1-only correction applied; NOT accepted, NOT implemented) |
| P2-T05 (Controlo_Create) | **CLOSED** (closure `3491097…`, dmo-work) |
| P2-T05 glass-density correction slice | **CLOSED** (closure `02bd53e…`, dmo-work) |
| P2-T06 (Controlo Approve) | **CLOSED** (closure `fcfa81f…`, dmo-work; "NEXT ELIGIBLE GATE: P2-T07 planning/contract gate — recorded only, NOT AUTHORIZED") |
| P2-T07 | **NOT STARTED — NOT AUTHORIZED** (unchanged; this task corrects the contract only) |
| P2-T08 / P2-T10 | **NOT AUTHORIZED** (unchanged) |
| Application code modified | **NO** |
| Migration created | **NO** |
| Supabase modified | **NO** |
| `ModuleRegistrations.CurrentBuildAvailable` | `[]` (unchanged) |
| `DestinationRouteRegistrations` / route registry | unchanged (still empty) |

### D.2 Governance files updated by this task

| File | Update |
|---|---|
| `plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md` | **new** — this contract |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §4 B3 row (authored status); §7 P2-T07 CONTRACT STATUS record; Appendix A 9.13/9.14 rows (status records only) |
| `plans/beta-workstreams/P2-T07-BOQUILHAS.md` | contract-authored record (pointer + status) |
| `dev/responses/P2_T07_CONTRACT_AUTHORING_RESPONSE.md` | **new** — the authoring response |

`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`, `reports/BETA_MASTER_RECONCILIATION.md`,
every closed contract/closure record and every ruling are **not** edited.

### D.3 Baseline verification (recorded by this task)

```text
DMO-MODULAR remote main (origin/main, fetched)   : 8f9e4f85af774453d6b99f66fc3dbaa7d5196a11
dmo-beta-master remote main                      : 78da49248f6cf7a8cbe4ddd946f3c38abbaf322f
dmo-work remote main                             : fcfa81f3aabd5457d73e50c86089b4de18e79225
dmo-master remote main                           : 8f1ca3e27e0eaf58c3dce285b544066565fa3dc1
   (dmo-master @ dmo-modular: ae2a9b9d12132ee4b41dc0696f34c6439b5cca52 — unchanged)
P2-T05 CLOSED (implementation 9fbfcf4, review ACCEPT ae99d1f) + correction slice CLOSED
   (implementation ce516d0, verification 7afcb00, review ACCEPT 300f011; closure 02bd53e)
P2-T06 CLOSED (contract dd0e163, PLAN ACCEPT 947c5f7, implementation e527ade + CP4 e784d0d,
   verification 8f9e4f8, review ACCEPT 4d88dbe; closure fcfa81f)
P2-T07 / P2-T08 / P2-T10            : NOT AUTHORIZED (unchanged; next eligible gate = P2-T07
                                       planning/contract gate)
CurrentBuildAvailable               : [] (verified in src/DMO.Application/Access/ModuleRegistrations.cs)
migrations                          : 001–006 (21 public tables; unchanged)
working tree (application code)     : clean before and after authoring
build/tests                         : not modified (authoring only; no build claimed)
```

### D.4 Planning-gate record (authoring commit)

| Item | Value |
|---|---|
| Contract file | `plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md` (this file) |
| Authoring commit | `dcca794685c03969d97295887e57e0f60e0bd7d8` (DMO-MODULAR remote `main`) |
| Authoring response | `dev/responses/P2_T07_CONTRACT_AUTHORING_RESPONSE.md` (same commit) |
| DMO-MODULAR remote `main` before this task | `8f9e4f85af774453d6b99f66fc3dbaa7d5196a11` |
| Implementation performed | **NONE** (docs-only planning commit: contract + response + master-plan/workstream status records) |
| Status | **AUTHORED — AWAITING ARCHITECT PLAN REVIEW** (PLAN ACCEPT / corrections / reject) |

### D.5 Focused B1 correction record (this task)

| Item | Value |
|---|---|
| Architect PLAN review | `dev/reviews/P2-T07_BOQUILHAS_CONTRACT_PLAN_REVIEW.md` @ dmo-work `542a08a1bcf1340306f8e337a6c597580a921f3d` |
| Original decision | **PLAN REJECT** — blocking findings **B1 only** (12 authority questions ACCEPT DEFAULT / 0 OWNER / 0 BLOCKING) |
| Defect | the `active-aggregate-exists` refusal was not race-safe as contracted: create has no version guard and the in-transaction application scan cannot serialize concurrent creates for the same anchor under READ COMMITTED with no unique anchor tuple, no partial unique index, no isolation override and no advisory locks |
| Correction applied | **B1 only** — `IX_boquilhas_active_bq_id` and `IX_boquilhas_active_tool_id` partial unique indexes (`WHERE status = 'active' AND <anchor> IS NOT NULL`, §7.2/§7.5), the exact scoped 23505 → `Refused(ActiveAggregateExists)` mapping (§7.2/§8.2), race-safe create (§22.2) and reopen (§23.3) backstop semantics + transaction rules (§10/§11), migration-007 schema delta (§28.2), and the concurrent create/reopen test rows K6–K8 (§29; **83 AC / 86 rows; no new criterion; missing 0, dangling 0, orphan 0**) |
| Not changed | all Architect PASS domains (identity, orchestration, vocabulary, replay balance, edit, dates, repairer, close/reopen, utilisation, Histórico, routes, access, boundaries, fixed desktop); tables (6), migration count (1), movement/repairer/lifecycle semantics, the 12 ACCEPT DEFAULT questions |
| Status after correction | **CORRECTED — AWAITING FOCUSED ARCHITECT PLAN RE-REVIEW** (NOT accepted; implementation NOT AUTHORIZED) |
| Correction commit | recorded in the DMO-MODULAR remote `main` report of the correction task |
| Implementation performed | **NONE** (docs/governance only) |

---

## Appendix E — PLAN REVIEW gate

Implementation may begin only when the Architect has:

1. reviewed **this** file at a recorded SHA;
2. dispositioned the authority questions of §30/§31 — in particular Q-EDIT-FIELDS, Q-MACHINE,
   Q-INICIO, Q-EXCESS, Q-ORDER, Q-CREATE, Q-REOPEN-ELIG, Q-REFLOT, Q-UTIL, Q-LINE,
   Q-CLOSE-DATE, Q-ANUL;
3. confirmed the identity/anchor model of §15, the movement/balance/edit contracts of
   §17–§19, the repairer consumption of §21, the close/reopen contract of §23, the
   route/policy matrix of §13/§14 and the migration contract of §28;
4. returned an explicit `PLAN ACCEPT` (or `CORRECTION REQUIRED` / `REJECT`) per
   `dmo-beta-master/WORKFLOW.md` step 6.

Until then:

```text
P2-T07 CONTRACT CORRECTED (B1) — AWAITING FOCUSED ARCHITECT PLAN RE-REVIEW
NOT IMPLEMENTED — NOT AUTHORIZED
CurrentBuildAvailable = []
```

---

## 33. OWNER CLARIFICATION — FINAL FUNCTIONAL RULE (SUPERSEDES THE AFFECTED CONTRACT RULES)

**Authority:** direct OWNER clarification after the P2-T07 implementation was pushed but while the
workstream is NOT closed and migration 007 was NEVER accepted by independent verification. This
section is a NEW OWNER CLARIFICATION and **supersedes every affected rule of this contract**: where
any earlier section (including the §7.2 B1 correction) conflicts with this section, THIS section
wins. The affected rules are **marked superseded** below; everything else (edit/audit single-event
semantics, business_date ⊥ recorded_at, repairer historical preservation, shared Tool
orchestration, fixed desktop, Histórico, authorization) is preserved as-is.

### 33.1 Superseded rules (explicit list)

The following rules and machinery are **REPLACED** as of this clarification; no implementation
after this clarification may use them, and the existing unreviewed implementation is corrected to
remove them (P2-T07 is not closed, so migration 007 is corrected CLEANLY — no compensating legacy
tables, no second migration):

1. **Standalone Boquilhas flow** — there is NO standalone (`tool_id`) anchor: every Boquilhas
   register belongs to a REAL Job On / production (the existing real identity chain
   `jobon_id → bq_id → tool_id`; no fake Job On, no fake bq_id, no `production_id`, no duplicate
   Tool identity). The production is the context: movements may be recorded while the production
   runs AND AFTER it has ended — a movement is never rejected because the production end date has
   passed; the movement's own `business_date` records when the movement happened and historical
   association remains with the original production/jobon_id.
2. **Início as a movement type** — removed. Creating/accessing the register MUST NOT generate a
   quantity movement merely to establish existence. If a register row must exist before the first
   movement, it is created as identity only (no manufactured stock).
3. **Irreparável as a movement type/name/meaning** — removed and REPLACED by
   **Entrada sem reparação**: X boquilhas returned from the repairer but NOT repaired. It is a
   normal historical movement record with a distinct meaning; it does NOT mark the Tool
   irreparable, does NOT create a permanent Tool state, does NOT separate/destroy the Tool
   identity and does NOT create an irreparable bucket/entity. It exists so the history clearly
   records, e.g., "Entraram 6 T173 que não foram reparadas (não cobráveis)". No
   billing/accounting mechanics are implemented in P2-T07.
4. **The open/closed aggregate lifecycle** — removed in full: no `active`/`closed` status, no
   Close action, no Reopen action, no close/reopen eligibility, no close snapshot, no reopening
   history, no `boquilha_close_snapshots` table, no `boquilha_reopenings` table, no
   active-anchor uniqueness and no active-aggregate pre-check.
5. **The B1 active-aggregate race machinery** — SUPERSEDED because its underlying invariant no
   longer exists: the partial unique indexes
   `IX_boquilhas_active_bq_id`/`IX_boquilhas_active_tool_id`, the scoped
   23505 → `Refused(ActiveAggregateExists)` mapping, `HasActiveAggregateForAnchorAsync` and the
   K6/K7/K8 create/create + create-vs-reopen race tests are REMOVED. They are NOT replaced by
   another locking mechanism — the problem they solved no longer exists.
6. **The four-bucket balance model** — removed. The amounts are reconciled to the new owner model:
   the ONLY derived value is the **outstanding repair quantity**,
   `outstanding = Σ(Saída) − Σ(Entrada) − Σ(Entrada sem reparação)` (the old Disponível /
   Em reparação / Irreparável / Entrada excecional buckets and the expected/excess facts are
   gone; Entrada sem reparação RETURNS quantity from repair like an Entrada). The outstanding is
   DERIVED BY REPLAY at read time and remains never-stored: movement facts remain the sole
   authority and no second mutable balance source is created. A negative outstanding is a valid
   visible projection (non-blocking).
7. **The register machine set (`boquilha_machines`) and the opening facts** — removed with the
   aggregate model: no register-level machine set, no opening date/utilisation/observations
   fields on the register, no opening-facts route. The movement row still carries its own
   `machine` (one of B1..C3, required on Saída) and the consumed repairer-resolved fact.

### 33.2 The final functional rule (normative)

Boquilhas is a **historical movement register associated with a REAL production**:

- registration: production/BQ context → Boquilhas register identity (one register per BQ context,
  plain unique key; no lifecycle state) → movements;
- movements: exactly THREE operational movement types — **Saída** (moves quantity out to repair;
  machine + repairer recorded), **Entrada** (returns repaired quantity), **Entrada sem
  reparação** (returns quantity from repair, records the returned boquilhas were NOT repaired);
  `Editar` remains an action, never a type;
- quantity effect: `outstanding = Σ(Saída) − Σ(Entrada) − Σ(Entrada sem reparação)`, derived by
  replay, never stored, never validated against a stock (no Saída ≤ Disponível rule, no
  Irreparável ≤ Em reparação rule);
- dates: `business_date` operator-editable (may be later than the production end date);
  `recorded_at` immutable backend timestamp;
- edit/audit, repairer resolution + historical preservation, Tool/Job On identities, Histórico
  (production context + chronological movement history, vocabulary Saída/Entrada/Entrada sem
  reparação only), routes all gated `dmo.module.boquilhas`, fixed 1366×768 desktop and the
  negative scope (no settings, no PDF/email/files, no availability) — all PRESERVED per the
  earlier sections of this contract;
- schema: migration 007 corrected (pre-closure) to the final THREE tables
  (boquilhas / boquilha_movements / boquilha_movement_audit) with no lifecycle-only structure;
  final physical table count to be reported by the implementation: 23 product tables
  (20 closed + 3), 24 raw incl. `__EFMigrationsHistory`.
- routes: the final route matrix is recalculated: 12 endpoints + 3 pages = 15 (close/reopen/
  opening-facts and standalone routes removed), NOT preserved at the old eighteen.

### 33.3 Authorization record

This clarification supersedes the affected contract rules pre-verification; it does NOT reopen any
closed P2-T04/P2-T05/P2-T06 authority. The implementation record
(`dev/responses/P2_T07_IMPLEMENTATION_RESPONSE.md`) is updated with this clarification as a
correction section.
