# P2-T05 — Controlo Create (Peso create/measurement core + `Controlo_Create → Definições`) — BACKEND / INTERFACE CONTRACT

**Workstream:** P2-T05 — Controlo_Create.
**Task class:** contract authoring only. **No implementation, no migration, no Supabase change.**
**Handoff:** `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` (authoritative handoff for this
workstream). **Settled delta:** `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`.

> **Scope note (operative):** this contract authorizes the **Peso create/measurement/calculation/
> submission core** and the **`Controlo_Create → Definições` operational settings** (repairer
> register, per-machine repairer assignments, PDF/document base directory, email recipient lists,
> email templates), **plus the seams** required by the workstream handoff. Comparação, Pegamentos,
> Folha (`controlo_sheet_id`), Resumo (`resumo_id`) and the shared read-model *rendering surface*
> remain part of the P2-T05 handoff but are **explicitly not authored here** — no table, route or
> behavior for them is created or reserved (§2.3, §29, Q-SCOPE). No approval behavior is authored
> (P2-T06).

---

## Required-section index

| # | Section |
|---|---|
| 1 | Authority |
| 2 | Scope / non-scope |
| 3 | Peso identity |
| 4 | CM / Job On relationship |
| 5 | Measurement model |
| 6 | Historical snapshot model |
| 7 | Create / edit / delete / submit behavior |
| 8 | Controlo_Create UI contract |
| 9 | Definições ownership |
| 10 | Repairer registry |
| 11 | Machine assignment |
| 12 | PDF directory settings |
| 13 | Email lists |
| 14 | Email templates |
| 15 | Email / document seam |
| 16 | Physical schema |
| 17 | Keys / constraints / indexes |
| 18 | Transactions |
| 19 | Concurrency |
| 20 | Repository / application interfaces |
| 21 | Routes / endpoints |
| 22 | Authorization |
| 23 | P2-T03 / P2-T04 composition |
| 24 | Fixed desktop |
| 25 | Migration contract |
| 26 | Failure / result vocabulary |
| 27 | Test-to-acceptance matrix |
| 28 | Authority questions |
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
5. `diogo-o/workbench` / `diogo-o/dmo-work` / the historical BA-DMO material inspected locally —
   historical evidence only; superseded where the current authority differs.

**Area-specific supersession (settled):** `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`
is repository-recorded settled functional authority for the settings/repairer/email/PDF area. Where
it and an existing authority file conflict in that area, the delta's settled decisions win. Two
documented conflicts are consumed as superseded for the Beta:

| Master/Beta statement | Superseded by (settled) |
|---|---|
| `dmo-master/modules/CONTROLO.md` §17: send recipients resolved by Machine/Line groups "configured in `Admin > Definições > Controlo`" | `…DELTA.md` §1/§2/§8/§9: Definições lives **inside Controlo_Create** (never a global Admin surface); recipient lists are named lists configured there; no exact routing rule is fixed (`…DELTA.md` §9.4). The Line-group **routing** concept is not merged with the (forbidden) Line-group **repairer assignment** — see §13.4/§28 Q-ROUTE |
| `dmo-master/global/INFORMATION_MODEL.md` "Fact ownership": "Admin -> … canonical shared repairer directory" | `…DELTA.md` §3/§3.6: the canonical repairer register is owned by **Controlo_Create → Definições**; Boquilhas consumes it; Admin owns none of it in the Beta |

No reconciliation question is reopened by this contract.

### 1.2 Authority read for this contract (completely)

**DMO-MODULAR:**
- `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` — the binding handoff (scope §4, non-scope §6,
  B2 §5, access §8, persistence §9, tests §10, acceptance §11);
- `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` — §0 terminology, §4 B2, §5 dependency graph, §7 P2-T05
  (with the settled delta's Definições scope), §7 P2-T06/P2-T08 (boundaries), §8 cross-cutting rules
  6–7, §9 route/availability plan, §10 access plan, §11 P2-T05 test strategy, §12 protected
  register, §13 final-integration sequence, Appendix A;
- `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` — **the settled settings authority**
  (§1 ownership, §2 Approve reduction, §3 repairer register, §4 machine autonomy, §6 historical
  preservation, §7 PDF directory, §8 email lists, §9 email routing intent, §10 email templates,
  §11 Boquilhas sidebar, §12 superseded visuals, §14 blocker effects, §15 non-changes, §16 open
  points);
- `reports/BETA_MASTER_RECONCILIATION.md` — §5.5 Workstream C reconciliation, §5.8 documents,
  §7 partials;
- `plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md` (accepted, `PLAN ACCEPT`
  `7d7a7c564027945a5c9a73cb798e9c226ee7f013`) — every real seam this contract consumes: identities
  (§2), schema (`tools`, `job_ons`, `cm_contexts`/`mf_contexts`/`bq_contexts`) (§3), frozen triple
  (§7), query contracts (§8), shared Tool orchestration (§9), transactions (§10/§11), dependency
  probe seam (§11.5), repository/service pattern (§12), route/policy pattern (§13), result
  vocabulary (§14), concurrency (§15), migration contract (§16), non-scope/protected files (§17),
  P2-T03 composition (§18), Supabase compatibility (§19);
- `dev/responses/P2_T04_IMPLEMENTATION_RESPONSE.md` — the actual shipped seams (repository
  `SaveAsync` helper, `ConcurrencyConflictExceptionMapping`, `JobOnPolicyNames`, 14 routes,
  availability boundary);
- `docs/CREATION_AND_ASSOCIATION_LOGIC.md` — the identity chain `peso_id -> cm_id -> tool_id +
  jobon_id`, pending association, no duplicate identities, PDF directory logic;
- `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` — §1A fixed desktop, §3 rules, §4 state
  vocabulary, §5 access/navigation seam (Definições is a surface, never a destination), §6
  `ProductionContextStrip`, §7 `ToolPicker`, §8 `ToolSummaryRow`, §10 `RecordStatus`, §13
  `MeasurementRows`, §14 `DecisionBar`, §16 change protocol;
- `src/` (read-only inspection) — `ModuleCatalog` (`ControloCreate`, `ControloApprove`, `Ferramentas`),
  `ModuleRegistrations` (`[]`), `DmoDbContext`/configurations (frozen triple, RESTRICT FKs, version
  conventions), `JobOnRepository`/`ToolRepository`/`JobOnService`/`ToolService` (transaction and
  result patterns), `CmContextEntityConfiguration`, `UserEntity` (`user_id`), `JobOnEndpoints`/
  `FerramentasEndpoints` (route/policy/transport pattern), shared frontend contracts.

**dmo-beta-master (BETA authority):**
- `modules/CONTROLO_CREATE.md` (full) — Peso workflow, pending association, calculation ownership,
  measurement rows, Comparação, Pegamentos, Folha/Resumo, access, acceptance;
- `modules/CONTROLO_APPROVE.md` — the D-side consumer contract (what Create must publish and
  must not pre-empt);
- `architecture/RECORD_LIFECYCLES.md` §4–§8 — Peso status vocabulary (`Pendente`/`Aprovado`/
  `Não aprovado`), submission, comparison staleness, Folha/Resumo distinct records;
- `architecture/CROSS_MODULE_FLOWS.md` — Job On → Controlo Create → Approve; pending association;
  shared Tool flow; anti-inference rules;
- `architecture/BACKEND_FRONTEND_MODEL.md` — backend/frontend ownership; selection vs inference;
- `contracts/IDENTITIES_AND_RELATIONSHIPS.md` — identity map; `peso_id -> cm_id`; no reverse
  arrays; no fake identities; human selection rule;
- `implementation/BETA_INTEGRATION_SEAMS.md` — Workstream B→C seam, C→D seam, access/action seam,
  navigation seam, backend/interface blocker rule, concurrency presentation;
- `contracts/DOCUMENTS_AND_FILES.md` — document identity/naming only (generation is P2-T08),
  availability states, historical rendering rule;
- `ACCEPTANCE_MATRIX.md` — §2 global invariants, §5 Workstream C, §6 Workstream D, §8 documents,
  §9 access, §10 persistence, §11 test evidence;
- `WORKFLOW.md` — controlled development chain, conflict protocol, cross-stream change protocol,
  anti-invention rules.

**dmo-master (current master authority; committed on `main` and the working branch at the same
blobs for the files cited):**
- `modules/CONTROLO.md` (full) — Peso identity/anchoring (§4–§6), historical context (§7), inputs
  and calculations (§8: drawing inputs, SAP optional references §8.1A, water temperature §8.2,
  glass density/processo §8.3, formulas §8.5, pairing §8.6, rows/status §8.7), Peso duplication §9,
  Comparação §10, Pegamentos §11, Folha §13, Resumo §14, actors §15, history §16, documents §17,
  invariants §18, acceptance §19;
- `global/INFORMATION_MODEL.md` — identity/direct-relationship rules; `peso_id -> cm_id`; no
  `job_on_revision_id`; AUTOPOPULATE vs SNAPSHOT (§"Baffle", §"Direct relationships/Peso",
  §"Fact ownership", §"Design rule");
- `global/ACCESS_MODEL.md` §9 — Controlo Create scope (creation/measurement/submission surface),
  Controlo Approve scope, shared destination, distinct modules;
- `dmo-modular/BETA_VERSION.md` §2/§4 — Tool creation from Controlo context, simplified Job On
  identity, "Controlo does not begin from a Tool and later look for a production; it begins from
  the concrete Job On".

**Historical evidence consulted (evidence only — never authority against the current documents):**
legacy BA-DMO material (`BA-DMO/.tmp-dmo-clean/database/migrations/N06_peso.sql`,
`.tmp-jobon-integration/AI-CONTEXT/docs/old-design/22_PESO_OPERADOR_01_VISUAL_AUTHORITY_peso-operador.html`
and the `Manual/20_CONTROLO_FUNCTIONAL.md` family) — used to *understand* the historical Peso sheet
row structure and the legacy "browser/computer-local" PDF directory concept. Every legacy feature
that conflicts with current authority is superseded (§1.3). In particular:

- the legacy `peso_controlos` status `rascunho` and the legacy uniqueness tuple
  `(mold, neckring, production, line, lote, date)` are **superseded** by the current vocabulary
  §3.3/§5.7 (three statuses only; no Peso uniqueness tuple);
- the legacy `job_on_revision_id` and `cm_snapshot`/`measurements_snapshot` jsonb columns are
  **forbidden** by current authority (no `job_on_revision_id`; preserved facts are normalized
  columns, no jsonb snapshot blob);
- the legacy "Diretório principal dos relatórios … guardados neste computador / autorização
  pertence a este browser" concept conflicts with the current server-rendered web runtime and is
  the basis of the **BLOCKING** deployment question §28 Q-PDF;
- the legacy `Peso nominal`/`Estado do molde`/`Notas`/autosave elements are **not** current
  authority and are not contracted (§1.4, §28 Q-NOMINAL).

### 1.3 Authority boundary for this workstream

P2-T05 owns (per the operative task and the handoff §4):

1. creation of Peso/Controlo records; 2. weight registration; 3. capacity registration;
4. association with the correct CM contextual occurrence (and the truthful pending case); 5. the
historical frozen measurement record; 6. the Controlo_Create operational UI; 7.
`Controlo_Create → Definições`; 8. repairer registry; 9. independent machine → repairer
assignment; 10. PDF/base-directory configuration; 11. email recipient lists; 12. email
templates/configuration; 13. the publish seam of the canonical read-only Peso sheet/read model for
P2-T06 (carrier shapes + rendering contract; the D-owned routes are authored by P2-T06).

P2-T05 does **not** own approval workflow (P2-T06); does **not** own Boquilhas (P2-T07); does
**not** own document generation/sending (P2-T08); does **not** own availability/navigation
registration (P2-T10).

### 1.4 Accepted input register (consume only — never modified)

```text
src/DMO.Application/Access/ModuleCatalog.cs   (ControloCreate, ControloApprove identities —
                                               one policy per module; never modified)
src/DMO.Application/Access/ModuleRegistrations.cs  (CurrentBuildAvailable stays [])
src/DMO.Application/Access/{ModuleRegistry,AccessResolver,AccessOutcome,ModuleResolve,
                             ModuleAccessService,IModuleAccessService,ModuleDefinition,
                             ModuleId,ModuleSurfaceDescriptor}.cs
src/DMO.Web/Authorization/{ModuleAuthorizationPolicies,ModuleAuthorizationHandler,
                           ModuleAuthorizationRequirement}.cs
src/DMO.Infrastructure/Persistence/DmoDbContext.cs            (byte-identical; Set<T> only)
src/DMO.Infrastructure/Migrations/20260922001736_*, 20260922001757_*,
   20260922232349_ToolJobOnDomainCore.*                       (never edited)
src/DMO.Application/{JobOn,Tools}/**                          (B's accepted contracts — consumed)
src/DMO.Infrastructure/Persistence/{JobOnRepository,ToolRepository,JobOnLineageDependencyProbe,
    ConcurrencyConflictExceptionMapping, EntityConfigurations/{Cm,Mf,Bq}Context*, JobOn*, Tool*}.cs
src/DMO.Web/Endpoints/{JobOnEndpoints,FerramentasEndpoints}.cs (consumed routes 1–14 of P2-T04)
src/DMO.Web/Frontend/Shared/Contracts/** and Pages/Shared/Components/**  (P2-T01/T02/T03, frozen)
docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md               (frozen; §16 change protocol)
plans/contracts/P2-T02_*.md, P2-T03_*.md, P2-T04_*.md          (accepted contracts)
tests/**                                                       (existing tests keep passing)
```

The only cross-stream additive change this contract proposes is one **read-only** query method on
the Job On application contract for association-candidate discovery (§20.4, §28 Q-CAND) — additive,
no existing member modified, per Beta `WORKFLOW.md` cross-stream change protocol. Everything else
consumes accepted seams as they are.

### 1.5 Recorded authority silences (no behavior invented — the implementer must not fill these)

| # | Silence | Consequence fixed here |
|---|---|---|
| S1 | Peso **uniqueness** — no current authority fixes a uniqueness tuple for `peso_id` | no database unique tuple beyond the PK; multiple Peso records per CM/date are legitimate; the previous-Peso/Comparação candidate surface (future) relies on explicit human selection §5.7 / Q-UNIQ |
| S2 | Peso **nominal weight**, mold state (`Estado do molde`), `Notas`, autosave | not current authority (legacy visuals only) → **not contracted**; no column, no field; Q-NOMINAL |
| S3 | **Water-temperature divisor table data** (temperature → divisor values) and the **processo → glass-density mapping values** | formula mechanics and the resolution seam are contracted (§5.3); the concrete table values exist in **no** repository authority → Q-CALC |
| S4 | **Units** | masses in grams (g), volumes in cubic centimetres (cm³), density in g/cm³, temperature in °C — legacy-consistent, authority-silent → Q-UNIT |
| S5 | **Decimal precision** | storage `numeric(18,4)`; presentation rounded to at most 2 decimal places (presentation rule only, `CONTROLO.md` §8.5) → Q-PREC |
| S6 | **Peso delete** | no delete path in P2-T05 (authority silent; smallest default) → §7.6 / Q-DELETE |
| S7 | **Peso duplication** | not in this contract's scope (authority: `CONTROLO.md` §9 exists globally; not in the operative task and not in the Beta handoff "Included in Beta" list) → seam §29 / Q-DUP |
| S8 | **Repairer name uniqueness / active-inactive lifecycle** | no unique name constraint; no status column; no user-facing deactivation flow → §10 / Q-REP |
| S9 | **Machine-assignment history** | current-state only; downstream (P2-T07) snapshots `repairer_id` on its records → §11.4 / Q-HIST |
| S10 | **Email routing rules** (context → list mapping; fallbacks; preview mandatory?) | intent only (`…DELTA.md` §9) — no routing rule is authored by P2-T05; P2-T08's contract owns it → §13.4 / Q-ROUTE |
| S11 | **Email-template placeholder syntax** | none exists in authority (`…DELTA.md` §10.4); templates store plain text; no parsing in P2-T05 → §14 / Q-PLACE |
| S12 | Whether settings are **site-wide or user-specific** | site-wide (single-company operational configuration; no per-user settings model exists) → Q-SITE |
| S13 | **Settings audit trail / actor columns** | none in P2-T05 (default); version tokens protect writes → Q-SETAUDIT |
| S14 | **Document availability read** on the Create side | P2-T05 reserves the UI region only; the read contract is P2-T08 → §8.7 / Q-DOCREAD |
| S15 | **"What accessible means" for the PDF base directory** | BLOCKING deployment question §28 Q-PDF |
| S16 | **Peso measurement row label** (legacy `cm_number`/`CM`) | rows are identified by dense 1-based `row_position` (positional pairing, `CONTROLO.md` §8.6); no label column in P2-T05 → Q-ROWLBL |
| S17 | **SAP reference fields** | contracted per `CONTROLO.md` §8.1A (optional, manual, snapshotted by being Peso columns; never a Comparação relation) — not a silence, listed for completeness |
| S18 | **Peso anchor re-establishment** (production-bound Peso moving to another `cm_id`) | not offered by P2-T05 (associate action applies only to the pending case) → Q-REANCHOR |
| S19 | **`water_weight` upper bound** | no authority fixes a maximum; validation only requires a finite positive value → §5.4 |
| S20 | **Email-list/member ordering** | deterministic technical order only (`name`, `address`); no ordering column → §13.5 / Q-ORDER |

---

## 2. Scope / non-scope

### 2.1 In scope (this contract authors all of it)

1. **Peso domain core** — `peso_id` identity and anchoring (production `cm_id` / pending
   `tool_id`), Peso-owned fields, Peso measurement rows (weight + capacity), backend calculations
   (capacity and glass weight), historical snapshot semantics, save/edit/submit transactions,
   explicit Peso association, published read-model shapes.
2. **Controlo_Create operational UI** — the Novo controlo surface (production/Job On selection,
   CM context, measurement entry, results, actions) and the `Definições` surface.
3. **`Controlo_Create → Definições`** — repairer register; per-machine repairer assignments
   (`B1`,`B2`,`B3`,`C1`,`C2`,`C3`, independent); PDF/document base-directory setting (configure /
   change / check); email recipient lists; email templates.
4. **Integration** — P2-T04 reuse (no duplicate identities), the Peso dependency probe on the
   accepted `IJobOnDependencyProbe` seam, composition of P2-T03 primitives, the cross-stream
   additive candidate-query proposal, and the P2-T06 handoff carrier (submitted `peso_id` +
   read-model shapes).
5. **Contractual artifacts** — physical schema (8 tables), routes (15), authorization, migration
   contract (ONE new migration), failure vocabulary, concurrency, test-to-acceptance matrix.

### 2.2 Explicit non-scope (never implemented by P2-T05)

| Non-scope | Owner |
|---|---|
| approve / reject / reopen decisions; pending-review list; per-CM `Manter`/`Colocar de parte` decisions; approval/decision history presentation; approval-copy Peso | P2-T06 |
| Comparação building/rebuilding (explicit `previous_peso_id` pairing persistence, stale condition, comparison table creation, per-CM pairing) | P2-T05 handoff remainder — **not this contract** (§2.3, §29, Q-SCOPE) |
| Pegamentos record/schema/evaluation; Folha (`controlo_sheet_id`); Resumo (`resumo_id`) | P2-T05 handoff remainder — **not this contract** (§2.3, §29) |
| Boquilhas movements/balance/close-reopen/History; repairer resolution **inside** Boquilhas; the Boquilhas machine sidebar | P2-T07 |
| Document generation, PDF bytes, file writing, naming/directory resolution at generation time, availability facts, email sending/transport/preview, exact context→list routing | P2-T08 |
| Module availability registration; real destination route registration; navigation exposure; cross-module links | P2-T10 |
| A new global Admin module or ADMIN surfaces for these settings; moving settings into the ADMIN-only Administration surfaces; registering `Definições` as a destination | forbidden by `…DELTA.md` §1.4 |
| A machine registry, `machine_id` scheme, machine table, line grouping, inheritance/cascade | forbidden (`…DELTA.md` §4.4; P2-T04 Q4) |
| Repairer fields beyond name; a grouped/Line repairer assignment model | forbidden (`…DELTA.md` §3.3, §4.2) |
| A second Tool registry; a second Job On authority; `production_id`; `job_on_revision_id`; reverse-ID arrays; fake `cm_id`/`jobon_id`; client-minted canonical identities | forbidden by authority |
| Generic lifecycle/state engine; a generic snapshot history engine; a document metadata table | forbidden |
| Mobile/tablet layouts, breakpoint reflow, card conversion, required-column hiding, action relocation | fixed desktop policy |
| HISTÓRICO GLOBAL (`historia`) | DEFERRED BY DESIGN |

### 2.3 The scope-boundary record (Comparação / Pegamentos / Folha / Resumo / read-model rendering)

The P2-T05 handoff and master plan §7 list Comparação, Pegamentos, Folha, Resumo and the shared
Peso read model inside the P2-T05 workstream. The **operative authoring task for this contract**
limits it to the Peso create core + Definições + seams. This contract therefore:

- authors the **schema and behavior that Comparação/read-model need as carriers only where those
  carriers are Peso facts** (the persisted `peso_id`, rows, results, status/attribution — all
  contracted here);
- does **not** create the `previous_peso_id` relation, Pegamentos/Folha/Resumo tables, comparison
  tables, or the read-model *rendering* contract — those require a follow-on authored contract
  (same workstream, separate `PLAN ACCEPT`) — see Q-SCOPE (NON-BLOCKING for this contract because
  no schema-critical uncertainty is left: every later addition is an additive migration on top of
  the §16 schema, and the Peso carriers they need are already fixed here);
- publishes the **read-model carrier shapes** (§26.3) so P2-T06 planning can proceed, while the
  D-owned read routes remain P2-T06's contract.

---

## 3. Peso identity

### 3.1 What `peso_id` is (exact)

```text
peso_id
= one Peso control/result fact (one specific Peso/Controlo record)
```

- **Never** a production identity, a Tool identity, a Job On identity or a CM identity.
- **Owner:** Controlo (`dmo-master` fact ownership: "Controlo -> Peso/Pegamentos/Folha/Resumo").
- **Allocation:** backend application layer allocates a `uuid` inside the create transaction; the
  DB default `gen_random_uuid()` exists only as the foundation column convention and is never the
  source of a canonical identity used by the application (`PesoId.New()`, mirroring `ToolId`/
  `JobOnId`).
- **Creation semantics:** durable and visible at COMMIT of the create transaction (§18.1); nothing
  observes it before that; no client ever supplies or guesses it.
- **Uniqueness:** PK only. No additional business unique tuple is fixed (S1, §28 Q-UNIQ). Duplicate
  prevention is therefore not a domain rule: the surface enforces explicit human selection of
  previous Peso/comparison candidates, never a uniqueness refusal.
- **Status:** the canonical three-value vocabulary of `RECORD_LIFECYCLES.md` §4:
  `pendente` (Pendente) / `aprovado` (Aprovado) / `nao_aprovado` (Não aprovado) — stored as ASCII
  domain tokens, displayed with the canonical Portuguese labels. **P2-T05 writes only `pendente`.**
  `aprovado`/`nao_aprovado` are written by P2-T06's approved/rejected transitions on the same row.
  Status grants no action and infers no lifecycle (`RecordStatus` rule).
- **Submitted carrier:** `submitted_at` + `submitted_by_user_id` (nullable) are the reviewable
  handoff carrier required by P2-T06 (create-side truth for state/attribution; §12 of the task
  allows the minimal persisted carrier for later approval). A Peso is reviewable **iff**
  `submitted_at IS NOT NULL` and `status = 'pendente'`.
- **Optimistic concurrency:** `version integer NOT NULL DEFAULT 1`,
  `.IsConcurrencyToken()` — exactly the accepted `job_ons.version` mechanism (§19).
- **Delete/edit rules:** §7.6 — no delete path in P2-T05; edit while not submitted; no Create-side
  mutation after submit.
- **Historical preservation:** Peso rows are never rewritten by later Tool/Job On changes §6.

### 3.2 Anchoring (the direct canonical relation)

A Peso always anchors to **exactly one** of two direct relations — this is the smallest valid
relationship over the real P2-T04 model:

```text
normal production Peso:        peso_id -> cm_id                    (cm_contexts row; real PK)
pending association Peso:      peso_id -> tool_id                  (tools row; real PK)
```

- The `cm_id` is the direct fact because it identifies the exact historical CM occurrence
  controlled in that production (`CONTROLO.md` §4). Tool and Job On are reachable through it —
  **no** direct `tool_id` and **no** direct `jobon_id` are persisted in the normal case.
- The pending anchor `tool_id` is the **truthful** anchor for (a) a Peso created before its Tool
  has a production CM context (pre-production technical verification) and (b) a Peso whose real
  controlled CM differs from the CM currently configured in the related Job On
  (`CONTROLO.md` §5/§6; `CONTROLO_CREATE.md` "Pending association case").
- **DB-enforced mutual exclusion:** `CHECK ((cm_id IS NULL)::int + (tool_id IS NULL)::int = 1)`
  — a Peso always has one anchor, never two, never none.
- Both FKs are `ON DELETE RESTRICT`: deleting a `cm_contexts` row or `tools` row referenced by a
  Peso fails closed at the database (and the application refuses first through the Probe of §20.4
  and the `dependency-exists` vocabulary).
- **No `jobon_id` column on `pesos`.** The Job On is reached through `cm_id` (normal case) or not
  at all (pending case). This is the task's "no redundant production_id" rule: nothing that is
  already reachable through `cm_id` is duplicated.

### 3.3 Identity semantics table

| Identity | Meaning | Owner | Never means |
|---|---|---|---|
| `peso_id` | one Peso control/result fact | Controlo | a production, a Tool, a Job On, a CM, an approval copy, a revision |
| `peso_measurement_row_id` | one measurement row of one Peso (stable persistence identity) | Controlo | a CM identity, a tool number, a production fact |
| `cm_id` (consumed) | the CM context of the Job On occurrence — the Peso's production anchor | Job On (P2-T04) | a Tool, a CM Tool identity, the Peso |
| `tool_id` (consumed) | canonical Tool — the Peso's pending/truthful anchor | Ferramentas (P2-T04) | a machine, a label, a surrogate |
| `jobon_id` (consumed, reachable) | the production occurrence behind the anchor | Job On (P2-T04) | a Peso fact |
| `repairer_id` | one external repairer (register entry) | Controlo_Create → Definições | a supplier code, a person identity, a machine |
| `email_list_id`, `email_list_recipient_id`, `email_template_id` | configuration identities | Controlo_Create → Definições | document identities, send identities |

### 3.4 Forbidden in this workstream (by authority)

`production_id`, `job_on_revision_id`, any `revision_id`, any Peso status beyond the three-value
vocabulary, `rascunho` as a status, module-private/legacy identity columns (`mold_number`,
`neckring_number`, `peso_lote_id`, `peso_reference_id`), fake `cm_id`/`jobon_id`, client-generated
Peso ids, jsonb measurement blobs, reverse-ID arrays (`tool.pesos[]`, `jobon.pesos[]`, …), and any
`peso_tool_snapshot` identity (`CONTROLO.md` §7: no generic snapshot engine).

---

## 4. CM / Job On relationship

### 4.1 The real chain (consumed, never recreated)

```text
Job On production occurrence (jobon_id)
  └─ cm_contexts (cm_id)  ──► tool_id  (canonical Tool; frozen triple tool_type/reference/lot)
        │
        └─ Peso (peso_id → cm_id)          normal production case
Peso (peso_id → tool_id)                   truthful pending case ("Job On por associar")
```

- P2-T05 creates **no Tool master, no Job On, no second CM identity**. It reuses the P2-T04
  tables/entities/services exactly (App. A), and only ever **anchors** the Peso to the real
  `cm_id`/`tool_id` and — for the missing-CM recovery of §4.4 — creates one `cm_contexts` row
  **through the Job On application contract** (never a direct table write from a Controlo
  repository).

### 4.2 Job On / production selection (Part 8 — exact rules)

1. **Explicit human selection only.** The Novo controlo workflow starts from the concrete Job On
   (`BETA_VERSION.md` §4: "Controlo does not begin from a Tool and later look for a production").
   The reference → productions query (`IJobOnService.FindProductionsAsync`) lists **every** matching
   occurrence (no latest-only, no recency filter); the operator explicitly selects one; a single
   result is **never** auto-selected (P2-T03/T04 selection contract).
2. **No auto-resolution and no second production lookup model.** The P2-T05 surface consumes the
   P2-T04 application contract (route 2 of the P2-T04 matrix when the caller holds `job-on-view`;
   for Controlo-Create-only callers the P2-T05 read routes of §21.3 delegate to the **same**
   service — one query shape, no duplicated model).
3. **Independent Peso path.** The operator may also create an independent Peso (no Job On) by
   explicitly selecting a canonical CM Tool (shared Tool orchestration, §4.3) — the truthful
   pending case. This is a first-class path, not an error state.
4. A blank reference is a validation failure (`REFERENCE_REQUIRED`); a valid reference with no
   occurrences is an explicit empty; a failed lookup is `lookup-failed`; a denied caller is
   `permission-denied`. The four are never conflated.

### 4.3 CM context resolution and selection (Part 9 — exact rules)

| Situation | Behavior |
|---|---|
| The selected Job On has a CM context (0 or 1 is guaranteed by `cm_contexts_jobon_key`) | The CM is **determined** by the Job On; the surface shows the context's frozen triple + the live Tool summary projection (P2-T04 `ToolContextFicha`/`ToolSummaryProjection`) + `processo` (read through `tool_id`, consumed — never re-entered). The operator's explicit action is "create Peso for this production" — no further CM choice exists (at most one CM context per Job On). |
| The selected Job On has **no** CM context | Two explicit recovery paths, both human-confirmed: (a) **create the missing CM context**: operator selects/creates a canonical CM Tool through the shared Tool orchestration (`ferramentas`-gated search/create routes of P2-T04), then confirms the association; the `cm_contexts` row is created **through `IJobOnService`'s association (Set) operation** under the Controlo_Create route (§21.3 route 12, §20.4) — the accepted "consuming workflow creates the missing context later" rule (`P2-T04` contract §6.7, `BETA_VERSION.md` §2.1); (b) **pending anchor**: operator explicitly selects a canonical CM Tool (shared orchestration) and the Peso is created with `tool_id` anchor, displayed `Job On por associar`. |
| Multiple CM candidates when associating a pending Peso | Candidate list = real `cm_id`s whose context resolves to the **same `tool_id`** (the anchor must match; `CONTROLO.md` §5: "candidate `cm_id` values resolving to the same `tool_id` may be shown; association is always human-confirmed"). Candidate discovery uses the additive read of §20.4 (Q-CAND). |
| Tool type mismatch (a non-CM Tool selected for the CM slot) | `TOOL_TYPE_MISMATCH` validation failure; nothing is written (P2-T04 rule reused). |
| Missing/denied reads | `permission-denied`/`lookup-failed` states; never an invented context. |
| Later Job On edits | A later CM re-selection (P2-T04 `Set`) keeps the `cm_id` **id** (in-place update); the Peso keeps pointing at the same `cm_id` whose frozen triple refreshes only through that explicit human Tool re-selection — never by P2-T05. A later `Remove` of the CM context is refused while pesos reference it (RESTRICT + probe). |

**What information identifies the CM:** frozen type/reference/lot of the used Tool
(`tool_reference` shown as the human CM identity, e.g. `5447T173`), the live Tool summary
(quantity, `processo`, compatible machines) and the production context strip (reference,
production_number, machine from the Job On). No internal id is displayed as identity
(`ProductionContextStrip`/human-selection rules).

### 4.4 Peso association action (pending → production)

```text
POST /controlo/create/pesos/{pesoId:guid}/associate     (route 10, §21.3)
carrier: { cmId, expectedVersion }
```

Rules (exact):

1. allowed **only** while the Peso is pending (`tool_id` anchor non-null) and not submitted;
2. the target `cm_id` must exist (`CM_CONTEXT_NOT_FOUND`);
3. `cm_contexts.tool_id` of the target **must equal** the Peso's anchor `tool_id`
   (`ASSOCIATION_MISMATCH` refusal otherwise — the anchor is the truthful Tool; the application
   never guesses);
4. on success: `cm_id` set, `tool_id` cleared, `version += 1`, one transaction — the temporary
   direct Tool anchor is **cleared** (`CONTROLO.md` §5);
5. never automatic, never inferred from date/reference/machine/history — always the explicit
   human-confirmed `cm_id` (`CROSS_MODULE_FLOWS.md` anti-inference rules);
6. a Peso already production-bound is refused (`already-associated`).

### 4.5 Failure behavior

Any resolution failure is a typed outcome (§26): `REFERENCE_REQUIRED`, `CM_CONTEXT_NOT_FOUND`,
`TOOL_NOT_FOUND`, `TOOL_TYPE_MISMATCH`, `ASSOCIATION_MISMATCH`, `not-found`, `stale-version`,
`permission-denied`. There is no silent fallback to another Tool, no auto-association, no fake
identity, and no reconstruction of relations from display text.

---

## 5. Measurement model

### 5.1 Principle

The backend is the **only** calculation authority (`BACKEND_FRONTEND_MODEL.md`, `CONTROLO_CREATE.md`
"Calculation ownership"). The frontend renders inputs and results; it never redefines the
formulas. The Peso fact model registers **both** the entered **weight** and the **derived
capacity** (task Part 1) — per measurement row, persisted as normalized columns.

### 5.2 Peso-owned input facts (columns on `pesos`)

| Fact | Type | Rules |
|---|---|---|
| `water_temperature` | `numeric(4,1)` | **required**; canonical supported range **5–35 °C**; CHECK `water_temperature >= 5 AND water_temperature <= 35`; validator token `TEMPERATURE_OUT_OF_RANGE` |
| `volume_marisa_bq` | `numeric(18,4)` | Peso-owned drawing input (`CONTROLO.md` §8.1); nullable (a Peso may be measured without drawing values finalized); unit cm³; historically associated with the Peso (§6) |
| `volume_puncao_pu` | `numeric(18,4)` | same rules; unit cm³; excluded volume |
| `previous_production_end_reference` | `text` | optional manual SAP reference ("Fim da produção anterior (SAP)"), `CONTROLO.md` §8.1A; not an identity; no Comparação meaning |
| `previous_average_weight_reference` | `text` | optional manual SAP reference ("Peso médio anterior (SAP)"), same rules |
| `glass_density_g_cm3` | `numeric(18,4)` | the **process-derived glass density actually used**: resolved by the backend at calculate time from the processo→density calibration mapping via `cm_id → tool_id → processo` (pending case: `tool_id → processo`), **frozen onto the Peso** (`CONTROLO.md` §8.3: "must be historically preserved with that Peso result"); NOT NULL once calculated; never re-entered by the operator; never persisted per measurement row; see Q-CALC for the mapping values |

**Not contracted:** `Peso nominal`/`média nominal` (S2), mold state, notes (S2).

### 5.3 Calculations (exact, authority-fixed)

```text
Capacidade / Volume do CM (per row, cm³)
= Peso de água (per row, g) ÷ valor da tabela de temperatura (g/cm³)

Peso do vidro (per row, g)
= (Capacidade do CM + Volume Marisa/BQ − Volume Punção/PU) × Densidade do vidro
```

- `valor da tabela de temperatura` = the **configured divisor value** for the entered
  `water_temperature` (a temperature→divisor mapping; `CONTROLO.md` §8.2 "the table value
  corresponding to the water temperature is used as the divisor"). The divisor resolution is a
  backend calculation-configuration concern **outside** the five Definições areas (S3, Q-CALC).
  The formula is contracted exactly; the table **data** is recorded as an authority silence —
  no divisor values are invented anywhere in this contract.
- Individual results are first-class: the persisted per-row capacity and per-row `Peso do vidro`
  are **never hidden behind an average**. Averages (`Média em água`, `Capacidade média`, `Peso
  médio do vidro`) and deviations from the average (`Desvio cm³`, `Desvio %`) are **derived
  presentation** — recomputed at read time from the persisted rows, never stored, and never able
  to mask an individual result (`CONTROLO.md` §8.5; `ACCEPTANCE_MATRIX.md` §5).
- Decimals are normalized to at most two decimal places **for presentation only**; calculation
  precision is the stored `numeric(18,4)` (S5).
- A row whose capacity cannot be computed (divisor unavailable) is refused with
  `CALCULATION_CONFIGURATION_MISSING` (§26.2, Q-CALC) — a typed failure, never a fabricated
  result and never a silent zero.
- No other calculation exists in this workstream: tolerance corridors, `NotEvaluable`, ovalização
  and nominal belong to Pegamentos (not this contract, §29); Comparação differences belong to the
  Comparação slice (not this contract).

### 5.4 Peso measurement rows (Part 6 — exact)

| Element | Contract |
|---|---|
| Row identity | `peso_measurement_row_id uuid` PK; the persistence row id is the stable identity. Frontend row keys stay opaque P2-T03 mechanics (§23) — never canonical ids. |
| Row count | **variable**; a valid Peso always keeps **at least one** valid row; the old fixed UI row count is not a cardinality rule (`CONTROLO.md` §8.7). Enforced by validator (`ROW_REQUIRED`) and by the create/save transaction. |
| Row ordering | `row_position integer` — dense 1-based ordinal within the Peso; `UNIQUE (peso_id, row_position)`; ordering is the positional pairing key of `CONTROLO.md` §8.6 (S16, Q-ROWLBL). |
| Row input | `water_weight_g numeric(18,4)` — "Peso de água" in grams; **required per row**; validation: finite, `> 0` (`ROW_WEIGHT_INVALID`); no authority-fixed upper bound (S19). |
| Row derived facts (backend-computed, persisted) | `capacity_cm3 numeric(18,4)` (per-row capacity) and `glass_weight_g numeric(18,4)` (per-row `Peso do vidro`), computed by the §5.3 formulas at calculate/save time and stored with the row — **registered** capacity and weight per row, never recomputed from current Tool state (§6). |
| Units | g / cm³ (§28 Q-UNIT). |
| What is NOT in a row | no `tool_id`/`cm_id`/processo per row (S16); no nominal; no label column in P2-T05 (Q-ROWLBL); no jsonb. |

### 5.5 Validation (closed, exact tokens — §26.2)

`WATER_TEMPERATURE_REQUIRED`, `TEMPERATURE_OUT_OF_RANGE`, `ROW_REQUIRED`, `ROW_WEIGHT_INVALID`,
`ROW_POSITION_INVALID`, `DUPLICATE_ROW_POSITION`, `VOLUME_NEGATIVE`, `DENSITY_MISSING`
(calculation-configuration), `PESO_ANCHOR_REQUIRED`, `PESO_ANCHOR_CONFLICT`, `CM_CONTEXT_NOT_FOUND`,
`TOOL_NOT_FOUND`, `TOOL_TYPE_MISMATCH`, `ASSOCIATION_MISMATCH`, `REFERENCE_REQUIRED`,
`PRODUCTION_NUMBER_REQUIRED`, `MACHINE_UNKNOWN`, plus the settings tokens of §26.2.

### 5.6 Precision and rounding

- Storage: `numeric(18,4)` for weights/volumes/capacity/density; `numeric(4,1)` for temperature.
- Presentation: at most two decimal places, Portuguese decimal comma; this is presentation-only
  normalization and never reduces calculation precision (test-matrix rows prove parity).

### 5.7 Status and lifecycle owned by Create (exact)

```text
create ──► status = 'pendente', submitted_at = NULL      (editable draft)
save/edit ─► same row, version += 1, status stays 'pendente'
submit ──► submitted_at := now(), submitted_by := current user
           status stays 'pendente'                        (reviewable by P2-T06)
P2-T06 ──► 'aprovado' | 'nao_aprovado' on the same row    (NOT authored here)
```

- `Job On por associar` is a **context/association condition** (pending anchor), **not** a status
  and **not** an error (`RECORD_LIFECYCLES.md` §4); it does not block measurement or submission;
  later explicit association clears it (§4.4).
- "Stale" (Comparação condition) is **not** a Peso status (`RECORD_LIFECYCLES.md` §5) and does
  not exist in this contract's scope (Q-SCOPE).

---

## 6. Historical snapshot model (Part 4 — exact)

### 6.1 The three kinds of historical data and their owners

| Kind | Owned/stored where | Frozen? | Never |
|---|---|---|---|
| **Live references** | `pesos.cm_id` (or `pesos.tool_id` pending) — direct FKs to real P2-T04 rows | no — they are relations | a duplicated `jobon_id`/second identity chain |
| **CM context snapshot** | `cm_contexts.tool_type/tool_reference/tool_lot` — the **P2-T04 frozen triple** captured at the explicit Tool selection for that occurrence | yes — frozen by P2-T04 §7.2; later live Tool changes never rewrite it (already proven/implemented) | re-read of the live Tool into history; a second snapshot table |
| **Peso-owned measurement/calculation facts** | `pesos` + `peso_measurement_rows` columns: temperatures, weights, volumes, SAP references, **frozen `glass_density_g_cm3`**, **frozen per-row `capacity_cm3` and `glass_weight_g`** | yes — written once by the backend at save/calculate; never recomputed from current Tool/Job On state | recomputation "for freshness" |
| **Derived presentation** | nothing — recomputed at read time | no | persisted averages that could hide individual results |
| **Production display context** (reference, production_number, machine, processo) | **not stored on Peso** — reached by traversal `peso_id → cm_id → jobon_id` (+ `tool_id` for processo) per `INFORMATION_MODEL.md`/`CONTROLO.md` §16 | no — see §6.2 | duplicated "merely for navigation" |

### 6.2 Why reference/production_number/machine are NOT frozen on the Peso (the task's
relational-identity vs historical-snapshot distinction, applied)

- The task forbids duplicating reference/production number/machine/Job On identity "unless a
  historical snapshot specifically requires freezing a value". No current authority **specifically
  requires** printing the Peso's history from frozen Job On labels:
  - `INFORMATION_MODEL.md`: "Do not repeat every reachable ID downstream"; history is query
    traversal over preserved facts;
  - `CONTROLO.md` §7: "Peso does not need a generic `peso_tool_snapshot` identity for values
    already preserved by the production CM context" (the context preserves the **Tool** triple);
  - the only Peso-side freezing that authority does require is the **calculation configuration
    actually used** (density §8.3, formulas/inputs §8.1/§8.1A) — all frozen columns here.
- **Consequence, stated exactly:** a later Job On edit changes what the Peso's read model shows
  for reference/production/machine through traversal — this is the accepted information-web
  behavior, and the Peso's *own* frozen facts (context triple via `cm_id`, inputs, density, per-row
  results, attribution) are never altered by it. The test matrix proves the frozen facts stay
  stable (§26.4, SNAPSHOT rows); it does not claim Job-On-label freezing, which does not exist.
- P2-T08's **official frozen output** freezes bytes at generation from then-current traversal —
  exactly the "frozen output corresponds to a frozen record state" rule of
  `DOCUMENTS_AND_FILES.md` §4; regeneration semantics remain P2-T08's contract. Q-PRODLABEL
  records the alternative (frontier: freeze labels on Peso) if the Architect prefers it — additive
  migration.

### 6.3 Historical immutability guarantees (exact, by construction)

1. no code path copies live Tool values into an existing Peso row or row rows (only the §5.3
   calculation writes derived columns, from Peso-entered inputs);
2. no code path recomputes the stored capacity/glass weight from current Tool/Job On state —
   there is no such method, event, trigger or worker in the contract;
3. the frozen `glass_density_g_cm3` is written once by the backend at the first successful
   calculate/save and is not refreshed by later config changes (a later density-mapping change
   affects **new** Peso calculations only);
4. `cm_contexts` values change only through P2-T04's explicit human `Set` (never through P2-T05);
5. no Peso row or row row is ever rewritten by a Tool/Job On/machine/repairer/PDF/email
   configuration change;
6. the Peso's own edit path (§7.3) rewrites only the Peso's own input facts and derived results,
   while not submitted, with the version token protecting the whole row set.

---

## 7. Create / edit / delete / submit behavior

### 7.1 Create transaction (exact steps)

```text
DMO.Application.ControloCreate.CreatePesoCommand(
    CmContextId? cmId,           // production-bound anchor (from an explicitly selected Job On
                                 // whose CM context exists)
    ToolId? pendingToolId,       // truthful pending anchor (explicitly selected canonical CM Tool)
    decimal WaterTemperature,
    decimal? VolumeMarisaBq, decimal? VolumePuncaoPu,
    string? PreviousProductionEndReference, string? PreviousAverageWeightReference,
    IReadOnlyList<decimal> RowWaterWeightsG)   // at least one element (validator)
```

```text
BEGIN
  1. validate everything (§5.5) — including exactly one anchor (cmId xor pendingToolId),
     temperature range, >= 1 row, valid water weights, dense 1-based positions
  2. resolve the anchor:
       cmId        -> load cm_contexts row (must exist; its jobon_id and tool_id exist by FK)
       pendingToolId -> load tools row (must exist; tool_type must be 'CM' — TOOL_TYPE_MISMATCH)
  3. resolve calculation configuration (divisor via water_temperature; glass density via
     tool_id.processo -> density mapping). Missing configuration -> ROLLBACK ->
     Refused(CalculationConfigurationMissing) — nothing written; no invented value
  4. allocate peso_id (+ one peso_measurement_row_id per row)
  5. INSERT pesos (status = 'pendente', submitted_at = NULL, submitted_by = NULL,
     created_by_user_id = current USER, created_at = now(), version = 1)
  6. INSERT every peso_measurement_row with row_position 1..n, water_weight_g,
     capacity_cm3 and glass_weight_g computed by §5.3 (backend, in-transaction)
  7. COMMIT
```

Guarantees: the Peso with its full row set exists or nothing exists (atomicity; forced-failure
rollback test); no partial Peso record; the returned carrier carries the real `peso_id` and
`version = 1`; retry after commit creates a **new** legitimate Peso (no uniqueness refusal — S1);
no client idempotency key is invented (accepted Q17 stance of P2-T04, same reasoning).

### 7.2 Calculate (stateless, read-only)

```text
POST /controlo/create/pesos/{pesoId:guid}/calculate    (route 8, §21.3)
```

- Inputs: current input facts (temperature, volumes, rows, anchor) as supplied by the operator
  (draft can be calculated before the first save);
- outputs: per-row `capacity_cm3`, `glass_weight_g` (full `numeric(18,4)` + 2-dp presentation) and
  the resolved divisor/density facts (display only);
- **no write, no version bump, no row creation** (exactly the P2-T04 read-transaction rule);
- the save transaction **recomputes** from the inputs so stored results always equal the
  authoritative formula (no preview/persist drift);
- `CALCULATION_CONFIGURATION_MISSING` when the divisor/density mapping is unavailable.

### 7.3 Edit (draft only; exact rules)

```text
DMO.Application.ControloCreate.UpdatePesoCommand(
    Guid PesoId, int ExpectedVersion,
    decimal WaterTemperature, decimal? VolumeMarisaBq, decimal? VolumePuncaoPu,
    string? PreviousProductionEndReference, string? PreviousAverageWeightReference,
    IReadOnlyList<decimal> RowWaterWeightsG)
```

```text
BEGIN
  1. load pesos + rows (tracked)
  2. assert row.version == ExpectedVersion        else ROLLBACK -> Refused(StaleVersion)
  3. assert submitted_at IS NULL                  else ROLLBACK -> Refused(AlreadySubmitted)
  4. validate (§5.5; same closed set as create)
  5. replace the row set: delete existing rows, insert the new set at dense positions
     (whole-set semantics — the measurement row set is an aggregate part of the Peso)
  6. recompute derived columns (§5.3) from the new inputs
  7. version += 1; updated_at = now()
  8. COMMIT
```

Rules: the anchor is **not** editable through this command (association is the dedicated action
§4.4; re-anchoring a production-bound Peso is not offered — Q-REANCHOR); `submitted_at` is never
cleared by Create (reopen is P2-T06); reads never mutate; a stale edit writes nothing.

### 7.4 Submit (the P2-T06 handoff, exact)

```text
POST /controlo/create/pesos/{pesoId:guid}/submit     (route 9, §21.3)
carrier: { expectedVersion }
```

```text
BEGIN
  1. load pesos + rows
  2. assert version == ExpectedVersion              else -> Refused(StaleVersion)
  3. assert submitted_at IS NULL                    else -> Refused(AlreadySubmitted)
  4. validate the full current state (§5.5: >= 1 valid row, temperature range, volumes, anchor)
  5. recompute derived columns (guarantees stored == authoritative)
  6. submitted_at := now(); submitted_by_user_id := current USER
  7. version += 1; COMMIT
```

- Create **never approves its own record**; submission is a review-state transition only; the
  same `peso_id` remains the single record (no approval-copy); state/attribution are backend
  truth (`submitted_at`/`submitted_by_user_id`).
- After submit, every Create-side mutation (edit, associate, calculate has no write anyway)
  is refused with `already-submitted`; correction then belongs to P2-T06's reopen workflow
  (seam §29).

### 7.5 Duplicate attempts / double submission

`submit` on a submitted Peso → 409 `already-submitted` (no silent success, no second record).

### 7.6 Delete (Part 11 — exact)

**There is no Peso delete path in P2-T05** (S6, Q-DELETE): no route, no repository primitive, no
soft-delete/archive/supersede column. Rationale: authority is silent on Peso deletion; the settled
status vocabulary has no "cancelled"; correction of a not-yet-submitted Peso is the draft edit;
correction of a submitted Peso is P2-T06 reopen; P2-T04's delete-protection philosophy
(dependency probes + RESTRICT) would apply to any future decision. A later authority decision
(option b in Q-DELETE) is an additive migration.

---

## 8. Controlo_Create UI contract

### 8.1 Principle

The operational page is contracted **without redesign**: the settled functional authority and the
existing legacy visual baseline (used as *visual reference only where compatible with authority*)
define the regions below. The page is designed at 1366 × 768 (binding §24). No mobile/tablet
variant, no breakpoint reflow, no action relocation, no required-control hiding.

### 8.2 Surfaces and routes

```text
/controlo/create            Novo controlo (Peso create/measurement/submission)   page
/controlo/create/definicoes Definições (operational settings)                   page (§9)
```

Both are **pages inside the Controlo Create working area**, gated `controlo-create`; `Definições`
is **not** a destination, not a Module, not registered anywhere (`…DELTA.md` §1.4; freeze §5;
master plan §9). The two pages share the Controlo_Create secondary-navigation pattern
(`_SecondaryNavigation`, P2-T09 wiring is additive shell only — not P2-T05's work; the pages may
render their own local surface links without touching shell files).

### 8.3 Novo controlo — regions (exact, in structural order)

| Region | Contract |
|---|---|
| **R1 — Production/Job On context strip** | read-only header region: reference, production number, machine, processo (consumed through `cm_id → tool_id`; unavailable when no CM context), plus the canonical Tool summary (type/reference/lot) when a CM context exists. Data comes **only** from the real backend reads (§21.3); never reconstructed from form text; the strip is not editable. `ProductionContextStrip` is **not implemented** by P2-T01…T03, so P2-T05 renders this region page-owned (Q-PCS; no look-alike of an implemented shared component exists). |
| **R2 — Production selection** | explicit selection: reference input → productions list (`DenseDataTable`), one row per occurrence (reference, production number, machine, production date), explicit select — never auto-select/latest-only; "independent Peso" affordance (explicit canonical CM Tool selection) opens the shared Tool orchestration; states: loading/empty/lookup-failed/unavailable/permission-denied (§4.2). |
| **R3 — CM context** | shows: (a) determined CM context (frozen triple + live tool summary); or (b) `Job On por associar` pending condition (neutral, non-error presentation with the truthful pending semantics — a Peso is fully usable in this state); or (c) missing-context recovery: "Corrigir ferramentas no Job On"-style actionable message + shared Tool orchestration to select/create the CM Tool and confirm the association (§4.3); candidate list when associating (§4.4). |
| **R4 — Peso inputs** | water temperature (°C, 5–35 with immediate input-shape feedback; backend validates); drawing inputs Volume Marisa/BQ and Volume Punção/PU (cm³, optional); optional SAP references (two text fields); measurement rows via `MeasurementRows` (variable rows, at-least-one enforced with visible reason, focus rules per P2-T03) — row fields: Peso de água (g). |
| **R5 — Results** | per-row results table (individual results first-class): Peso em água / Capacidade (cm³) / Desvio cm³ / Desvio % / Peso do vidro (g) — the last four derived; plus read-derived summaries Média em água, Capacidade média, Peso médio do vidro, Densidade usada; decimals at ≤ 2 dp; averages never hide an individual result; `CALCULATION_CONFIGURATION_MISSING` state when configuration is absent. |
| **R6 — Actions (DecisionBar)** | `Calcular` (stateless, §7.2), `Guardar` (draft save), `Submeter para aprovação` (submit — only while not submitted; after submit the bar shows the submitted state and no Create mutation), `Cancelar`. Duplicate invocation prevented; disabled reasons visible; `submitting` state distinct from `saving`. |
| **R7 — Status/state region** | `RecordStatus` with the canonical Peso status text (Pendente during Create) + `Job On por associar` condition text when pending; `CommonState` regions for every non-ready state. |
| **R8 — Document seam region** | reserved read-only region for future document availability presentation; P2-T05 renders **no** generation action and performs **no** availability lookup (Q-DOCREAD; P2-T08 owns generation/availability). |

### 8.4 Save behavior

Explicit `Guardar` posts the full draft (inputs + whole row set) with the observed `version`;
success keeps the operator on the same surface (no navigation loss), reports saved state, and
returns the fresh `version`. `stale-version` → `conflict` presentation with reload/recovery;
`already-submitted` → read-only presentation. No autosave (S2).

### 8.5 Validation / errors

Backend validation is authoritative; the form shows immediate input-shape feedback without
duplicating domain validation. Errors are typed (§26) and mapped to the shared state vocabulary
(freeze §4): `validation-failed` (400) with the exact token list; `conflict` for 409s; denied →
`permission-denied` (never a blank surface, never an empty). Entered values are retained on
refusal.

### 8.6 Success state

After create/save: the Peso sheet is shown with its real `peso_id`, frozen inputs/results and the
fresh version; after submit: the reviewable state with backend attribution (submitted at/by) and
the submitted-only presentation — Create cannot approve.

### 8.7 Definições access

`Definições` is reached inside the Controlo Create workflow (secondary link from Novo controlo).
Every `Definições` route/action is gated `controlo-create`; a `controlo-approve`-only caller
receives the documented server-side denial on every one of them (§21.5, §22).

---

## 9. Definições ownership

### 9.1 Ownership (settled)

`Controlo_Create → Definições` owns **exactly** the operational configuration consumed by the
Controlo/Peso/Boquilhas workflows:

```text
Controlo_Create
└── Definições
    ├── Reparadores                  (repairer register — §10)
    ├── Reparador por máquina        (B1 B2 B3 C1 C2 C3 — §11)
    ├── Diretório de PDF/documentos  (base directory — §12)
    ├── Listas de email              (named recipient lists — §13)
    └── Templates de email           (subject/body/context — §14)
```

- Controlo_Approve owns **none** of it (`…DELTA.md` §2); no Admin module/surface owns it
  (`…DELTA.md` §1.4, §15.1); `Definições` is not a destination (§15.7, §30).
- Surface organization: **one page with five sections** (delta O2 pinned default; §28 Q-SURF).
- Persistence: all five areas are **site-wide** database configuration (S12, Q-SITE) — no
  per-user, per-browser or per-template settings.
- Settings are configuration data, not domain records: no setting value ever becomes a canonical
  identity, a join key, a document identity or production truth.

---

## 10. Repairer registry

### 10.1 Identity and minimal data (settled)

`repairer_id uuid` PK, backend-allocated. Required business data: **Name only** (`…DELTA.md`
§3.2). **Forbidden to invent** as required or as functional fields: address, email, phone,
supplier code, tax data (NIF/VAT/fiscal), contact person (§3.3).

### 10.2 User-facing capabilities (exact)

1. **add** a repairer (name);
2. **edit** a repairer name;
3. **select** an existing repairer for a machine assignment (§11).

Nothing else is user-facing: no status vocabulary, no mandatory deactivation workflow, no
administrative lifecycle UI («DELTA» §3.5).

### 10.3 Uniqueness / lifecycle

- No unique constraint on `name` (two repairers may share a name; selection is by row, explicit)
  (S8, Q-REP).
- **No active/inactive column** in P2-T05 (S8). Historical safety is achieved structurally:
  every FK to `repairers` is `RESTRICT`, so a repairer referenced by a machine assignment (or, in
  the future, by a Boquilhas movement) **cannot be hard-deleted**; the register therefore needs
  no lifecycle column to protect history in this contract. A future deactivation need is an
  additive migration (Q-REP option b).
- Delete: **no delete route in P2-T05** (matches delta §3.5: the user-facing model is add/edit/
  select; name-only register entries that are no longer used simply stay in the register).

### 10.4 Validation

`NAME_REQUIRED` (`btrim(name) <> ''` DB CHECK + validator); trimmed on input.

---

## 11. Machine assignment

### 11.1 The settled rule (critical, exact)

- Machines: `B1 B2 B3 C1 C2 C3` (the six settled codes).
- Each machine holds its **own independent** repairer assignment.
- There is **NO** grouping rule: no shared "Linha B"/"Linha C" repairer, no inheritance, no
  cascade. Changing `B1` affects **only** `B1` (`…DELTA.md` §4).
- Several machines currently pointing at the same repairer is **configuration, not domain law**.

### 11.2 Representation

One row per machine in `machine_repairer_assignments`; `machine` is a **closed code value**
(`text` + CHECK, exactly the P2-T04 `MachineCode` convention) — **no machine registry, no
`machine_id` scheme** (`…DELTA.md` §4.4; P2-T04 Q4). An absent row = "no repairer assigned"
(explicit empty — never a default repairer, never an error).

### 11.3 Operations

```text
GET    /controlo/create/definicoes/machine-assignments
PUT    /controlo/create/definicoes/machine-assignments/{machine}    (set/change/clear)
```

- Set/change: upsert the row for that machine (`repairerId` must exist → `REPAIRER_NOT_FOUND`).
- Clear: the row for that machine is removed (assignment becomes none) — explicit operator action.
- One machine's operation never touches another machine's row (unit test rows B1–C3 §26.4).
- `MACHINE_UNKNOWN` for any code outside the six.

### 11.4 History — current-state only (the smallest model, proven sufficient)

**Contract decision:** the assignment table holds **current assignment only**; **no assignment
history table** is created (S9, Q-HIST). Proof of sufficiency from authority:

- `…DELTA.md` §6 requires that a later assignment change never rewrites earlier **Boquilhas**
  records;
- Boquilhas satisfies that by storing the canonical `repairer_id` **on the external Saída
  movement itself** with historical retention (master plan §8 Peso/Boquilhas rows; `…DELTA.md`
  §5.4/§6.4: "the movement still records it"; `ACCEPTANCE_MATRIX.md` §7: "external Saída stores
  canonical `repairer_id`");
- therefore the *resolution source* ("machine → current assignment → repairer", `…DELTA.md` §5)
  only ever feeds **new** records, and each new record snapshots its own `repairer_id` — no
  assignment-history rows are required for historical preservation;
- P2-T05's task §17 explicitly allows downstream snapshotting when sufficient and forbids
  overbuilding.

The resolution query itself (machine → repairer) is a read contract for the future consumer
(P2-T07); P2-T05 exposes the current assignment read under `controlo-create` (§21.3 route 14) and
records the consumption seam (§29).

---

## 12. PDF directory settings

### 12.1 Settled scope

`Controlo_Create → Definições` owns the **PDF/document base-directory configuration**
(`…DELTA.md` §7): configure the base directory; change it; verify/check whether it is
accessible. The `<reference>/<production-number>/` structure, the document file names
(`Peso_<reference>_<machine>.pdf`, `Pegamentos_<reference>_<machine>.pdf`,
`Resume_<reference>_<machine>.pdf`) and every availability state are **unchanged** and are
P2-T08's/generation's concern (§12.4, §15).

### 12.2 Configuration contract (FIXED by this contract)

| Element | Contract |
|---|---|
| Persistence | one row in `pdf_directory_settings` (single-row table; §16); site-wide (Q-SITE) |
| Path representation | absolute filesystem path, `text`, trimmed non-blank (`DIRECTORY_REQUIRED`); no trailing separator normalization is performed or claimed (stored verbatim, trimmed); paths are **never** used as identities, never join keys, never printed into documents (`DOCUMENTS_AND_FILES.md` §5) |
| Configure / change | `PUT /controlo/create/definicoes/pdf-directory` — single upsert; version-checked; change is a plain setting change (no migration of documents, no availability change — P2-T08 consumes the new value; `…DELTA.md` §7.4) |
| Check | `POST /controlo/create/definicoes/pdf-directory/check` returns a typed accessibility result (below) |
| Failure/result vocabulary | `not-configured` (no row yet — explicit state, not an error), `ok`, `directory-not-found`, `not-a-directory`, `access-denied` (permission/read-write), `invalid-path` (not an absolute path), `check-failed` (infrastructure lookup failure — never conflated with `directory-not-found`) |
| Security constraints | the setting surface returns **only** the typed result and the configured path; it never lists directory contents, never follows/exposes other filesystem areas, never renders the path into documents, never reads files; no arbitrary file-read endpoint exists |
| Behavior when unavailable | the check reports the typed state; no P2-T05 workflow depends on the directory (P2-T05 performs no file IO); P2-T08 consumes availability through the accepted `AvailabilityState` `workspace-unavailable` family |
| Directory creation | P2-T05 does **not** create directories (creation/use semantics belong to P2-T08's generator contract) |

### 12.3 The BLOCKING deployment question (Part 18 — exact)

The legacy authority's "main reports directory" was a **browser/computer-local** directory
("Os PDFs aprovados são guardados neste computador", "A autorização pertence a este
browser/computador" — legacy visual). The current runtime is a **server-side ASP.NET modular
monolith** (Razor pages + minimal APIs; PostgreSQL; no browser-file-system layer, no client-side
storage authority anywhere in the accepted foundation), and **no repository authority records
where the production runtime will be hosted** (no Dockerfile, no hosting/deployment contract in
DMO-MODULAR; Supabase is the database, the web process location is unrecorded). Therefore:

- the **persistence and change surface** of the setting is architecture-neutral and is fixed
  here (§12.2, schema §16);
- the **"accessible" check semantics** — who can observe the path (server process filesystem,
  network share reachable from the server, operator workstation, or a managed document store) —
  **cannot be fixed without inventing either browser filesystem access or an unrecorded
  deployment assumption**. This is recorded as **BLOCKING question Q-PDF** (§28). Until the
  Architect resolves it, the check *operation* may not be implemented to a fabricated semantic;
  the schema and the configuration/change surface are unaffected and remain fixed.

### 12.4 Ownership boundary with P2-T08

P2-T05 owns the **setting** (configure/change/check surface). P2-T08 owns generation, the
resolution of the base + `<reference>/<production-number>/` at generation time, file writing,
naming and availability facts (`…DELTA.md` §7.5; master plan §7 P2-T08). P2-T05 creates no file
capability (`Infrastructure/Files`/`Pdf` stay P2-T08's).

---

## 13. Email lists

### 13.1 Settled scope

`Controlo_Create → Definições` owns **named email recipient lists**
(`…DELTA.md` §8): create/edit a named list; associate recipients; select/use lists (selection for
sending rules is exposed as a read; the rules themselves are P2-T08, §13.4).

### 13.2 Configuration contract

| Element | Contract |
|---|---|
| List identity | `email_list_id uuid` PK, backend-allocated |
| List name | `text` non-blank (`LIST_NAME_REQUIRED`), trimmed, **unique** (`email_lists_name_key`) — a named list must be unambiguously addressable (Q-NAME) |
| Recipients | rows in `email_list_recipients`; each = one address (`address text`, trimmed, non-blank, `ADDRESS_REQUIRED`) |
| Validation | minimal unbroken address shape: exactly one `@`, non-blank local part and domain, no whitespace (`ADDRESS_INVALID`) — no full RFC validation is invented (Q-ADDR) |
| Edit semantics | `UpdateEmailListCommand` carries the **complete recipient set** (replace-all within one transaction) — no partial list is ever persisted (§18.6) |
| Duplicates | `UNIQUE (email_list_id, address)` — one address once per list; the **same address may appear in several lists** (`…DELTA.md` §8.5 default pinned here) |
| Ordering | deterministic technical order only (`address` ASC); no ordering column (S20, Q-ORDER) |
| Delete | list delete with explicit confirmation (`DELETE_NOT_CONFIRMED`); refused with `dependency-exists` when a future dependent (P2-T08 routing) references it (RESTRICT FK backstop) |
| Hardcoding | **recipient addresses are never hardcoded in application code** (`…DELTA.md` §8.4) — asserted by static scan |

### 13.3 Consumption seam

P2-T05 exposes the list read (name + recipients + version) under `controlo-create`. The
future sending flow (P2-T08) consumes the same application read contract; P2-T05 defines no
sending behavior.

### 13.4 Email routing — the Line-group question (Part 21 — exact)

- The master authority (`CONTROLO.md` §17) describes send recipients resolved by
  **Machine/Line groups** (B1–B3 → Linha B; C1–C3 → Linha C), configured in `Admin > Definições`
  (superseded location — §1.1).
- The settled delta forbids line grouping **for repairer assignment only** (§4.2) and fixes no
  email routing rule at all; its §9.4 explicitly forbids inventing exact automatic routing rules;
  the task confirms **email routing grouping and repairer assignment are different concepts and
  must not be merged**.
- **Contract decision:** P2-T05 stores **named lists only**. No machine/line routing table, no
  list-group table, no automatic routing rule is created; the context→list mapping (machine-based,
  line-based, explicit-selection, or configured-by-context) is recorded as **authority question
  Q-ROUTE** for the P2-T08 contract, which must be consistent with `…DELTA.md` §9.4 and §8.4.
  The named-list model of this contract is neutral to any future routing decision.

---

## 14. Email templates

### 14.1 Settled scope

`Controlo_Create → Definições` owns **email templates** (`…DELTA.md` §10): a template may contain
subject, body and the applicable document type/context where needed; templates should support
contextual values already known to the application (reference, production, machine, date).

### 14.2 Configuration contract

| Element | Contract |
|---|---|
| Identity | `email_template_id uuid` PK, backend-allocated |
| Name | `text` non-blank, trimmed, unique (`TEMPLATE_NAME_REQUIRED`, `email_templates_name_key`) |
| Subject | `text` non-blank (`SUBJECT_REQUIRED`) |
| Body | `text` non-blank (`BODY_REQUIRED`) |
| Document type/context | `document_type text NULL` with CHECK `document_type IN ('peso','pegamentos','resumo')` — the Beta's three document output families (`DOCUMENTS_AND_FILES.md` §2); `NULL` = template not bound to a specific output (generic); `DOCUMENT_TYPE_UNKNOWN` for other values (Q-DOCTYPE) |
| Placeholder syntax | **none is fixed** (`…DELTA.md` §10.4): text is stored verbatim; P2-T05 performs **no** placeholder parsing, substitution or validation. Any future syntax is an authored decision (Q-PLACE) |
| Recipient-list association | **none on the template** — routing (including any default list per document context) is P2-T08's contract (Q-ROUTE); templates and lists are independent configuration entities (§15) |
| Versioning/locale/permission granularity | none (`…DELTA.md` §10.5) |
| Delete | with explicit confirmation; `dependency-exists` refusal when referenced |

---

## 15. Email / document seam (Part 23 — exact)

**P2-T05 owns settings/configuration only. P2-T08 owns the send-PDF workflow:**

```text
[P2-T05, this contract]                     [P2-T08, future contract]
Definições settings ──────────────────────►  sending orchestration
  email lists (named + recipients)            context → list resolution (Q-ROUTE)
  email templates (subject/body/type)         template application (Q-PLACE)
  PDF base directory (config/change/check)    generation: base + <reference>/<prod>/…
                                              attachment + preview + send (transport O10)
```

- The sending boundary is: **configuration persists here; consumption and transport are
  P2-T08's** (master plan §7 P2-T08 "settings surfaces themselves … owned and implemented by
  P2-T05 — P2-T08 only consumes the configured directory and the configured email
  lists/templates").
- The known desired concept (`…DELTA.md` §9.2: `Peso PDF → known production/machine context →
  appropriate configured email list → email template → PDF attachment → preview → send`) is
  recorded as **intent** — nothing in it is implemented here.
- Pegamentos presence (Part 24): a **Pegamentos record may legitimately be absent**
  (`CONTROLO.md` §11; `DOCUMENTS_AND_FILES.md` §2); the UI never offers a live action implying a
  file exists when it does not. P2-T05 owns **no** Pegamentos document state — the seam is: the
  future Pegamentos surface (P2-T05 handoff remainder) and P2-T08's availability presentation
  must keep absent ≠ missing ≠ lookup-failed distinct (the accepted `AvailabilityState`
  vocabulary already does). No state or column for Pegamentos presence is created by this
  contract (Q-SCOPE).

---

## 16. Physical schema

Engine: **PostgreSQL** (Supabase TEST runtime target), accepted foundation conventions (P2-T04
contract §3): explicit snake_case; PK `uuid` + `gen_random_uuid()` default, application-allocated;
`timestamp with time zone` + `now()`; `text` with `btrim(x) <> ''` CHECKs for human codes; closed
sets via CHECK; **every FK `ON DELETE RESTRICT`**; one `EntityTypeConfiguration` per entity,
discovered by `ApplyConfigurationsFromAssembly`.

**Eight tables are created. No ninth table** — in particular no machine registry, no
assignment-history table, no Peso-association/snapshot table, no document metadata table, no
approval/decision table (P2-T06), no Comparação/Pegamentos/Folha/Resumo table (Q-SCOPE), no
water-table/calculation-config table (Q-CALC: calculation configuration is consumed, not owned,
by P2-T05), and no reverse-array column.

### 16.1 `pesos` — one Peso control/result fact

| Column | Type | Null | Contract |
|---|---|---|---|
| `peso_id` | `uuid` | NO | PK; default `gen_random_uuid()`; application-allocated |
| `cm_id` | `uuid` | YES | FK → `cm_contexts(cm_id)` `RESTRICT`; the production anchor (then `tool_id` NULL) |
| `tool_id` | `uuid` | YES | FK → `tools(tool_id)` `RESTRICT`; the truthful pending anchor (then `cm_id` NULL) |
| `status` | `text` | NO | CHECK `status IN ('pendente','aprovado','nao_aprovado')`; default `'pendente'`; P2-T05 writes only `pendente` |
| `submitted_at` | `timestamp with time zone` | YES | the reviewable handoff carrier; NULL until submit |
| `submitted_by_user_id` | `uuid` | YES | FK → `users(user_id)` `RESTRICT`; backend-set at submit |
| `water_temperature` | `numeric(4,1)` | NO | CHECK `water_temperature >= 5 AND water_temperature <= 35` |
| `volume_marisa_bq` | `numeric(18,4)` | YES | drawing input, cm³; NULL = not entered |
| `volume_puncao_pu` | `numeric(18,4)` | YES | drawing input, cm³; NULL = not entered |
| `glass_density_g_cm3` | `numeric(18,4)` | YES | frozen process-derived density used; NULL only before the first successful calculate/save |
| `previous_production_end_reference` | `text` | YES | optional manual SAP reference; nonblank when present (CHECK `btrim(...) <> ''`) |
| `previous_average_weight_reference` | `text` | YES | optional manual SAP reference; nonblank when present |
| `version` | `integer` | NO | default `1`; optimistic-concurrency token (`.IsConcurrencyToken()`) |
| `created_by_user_id` | `uuid` | NO | FK → `users(user_id)` `RESTRICT`; backend-set actor (`CONTROLO.md` §15 operator) |
| `created_at` | `timestamp with time zone` | NO | default `now()` |
| `updated_at` | `timestamp with time zone` | NO | default `now()` |

Exact CHECK: `pesos_anchor_check`: `((cm_id IS NULL)::int + (tool_id IS NULL)::int) = 1`.

Deliberately absent: `jobon_id`, `reference`, `production_number`, `machine` (reachable through
`cm_id`; §6.2), `previous_peso_id` (Q-SCOPE — Comparação is not this contract), approval columns
(P2-T06), any nominal/mold-state/notes column (S2), any jsonb.

### 16.2 `peso_measurement_rows` — Peso measurement rows

| Column | Type | Null | Contract |
|---|---|---|---|
| `peso_measurement_row_id` | `uuid` | NO | PK; default `gen_random_uuid()`; application-allocated |
| `peso_id` | `uuid` | NO | FK → `pesos(peso_id)` `RESTRICT` |
| `row_position` | `integer` | NO | dense 1-based ordinal; CHECK `row_position >= 1` |
| `water_weight_g` | `numeric(18,4)` | NO | "Peso de água", grams; CHECK `water_weight_g > 0` |
| `capacity_cm3` | `numeric(18,4)` | NO | derived per-row capacity (backend); CHECK `capacity_cm3 > 0` |
| `glass_weight_g` | `numeric(18,4)` | NO | derived per-row glass weight (backend); CHECK `glass_weight_g > 0` |
| `created_at` | `timestamp with time zone` | NO | default `now()` |

No `version` column: rows are written only inside a Peso mutation transaction; `pesos.version`
protects the aggregate (accepted `template_modules`/context-row reasoning). No label column
(Q-ROWLBL); no per-row identity facts (S16).

### 16.3 `repairers` — the canonical repairer register

| Column | Type | Null | Contract |
|---|---|---|---|
| `repairer_id` | `uuid` | NO | PK; default `gen_random_uuid()` |
| `name` | `text` | NO | trimmed non-blank (CHECK) — the **only** business field |
| `version` | `integer` | NO | default `1`; token (§19) |
| `created_at` | `timestamp with time zone` | NO | default `now()` |
| `updated_at` | `timestamp with time zone` | NO | default `now()` |

Deliberately absent: address/email/phone/supplier code/tax fields/contact person (§10.1), status/
active column (§10.3), unique name (Q-REP).

### 16.4 `machine_repairer_assignments` — one independent current assignment per machine

| Column | Type | Null | Contract |
|---|---|---|---|
| `machine_repairer_assignment_id` | `uuid` | NO | PK; default `gen_random_uuid()` |
| `machine` | `text` | NO | CHECK `machine IN ('B1','B2','B3','C1','C2','C3')`; **UNIQUE** — one row per machine, at most one |
| `repairer_id` | `uuid` | NO | FK → `repairers(repairer_id)` `RESTRICT` |
| `version` | `integer` | NO | default `1`; token |
| `created_at` | `timestamp with time zone` | NO | default `now()` |
| `updated_at` | `timestamp with time zone` | NO | default `now()` |

Deliberately absent: line/group column, history table (§11.4), machine registry (§11.2).

### 16.5 `pdf_directory_settings` — the single-row base-directory setting

| Column | Type | Null | Contract |
|---|---|---|---|
| `pdf_directory_setting_id` | `uuid` | NO | PK; default `gen_random_uuid()` |
| `singleton` | `boolean` | NO | `CHECK (singleton)`; **UNIQUE** — the physical single-row mechanism |
| `base_directory` | `text` | NO | absolute path, trimmed non-blank (CHECK); the operator-configured base |
| `version` | `integer` | NO | default `1`; token |
| `updated_at` | `timestamp with time zone` | NO | default `now()` |

Absent row = not configured (`not-configured` explicit state). No path-identity, no per-machine
directories, no audit columns (S13).

### 16.6 `email_lists` — named recipient lists

| Column | Type | Null | Contract |
|---|---|---|---|
| `email_list_id` | `uuid` | NO | PK; default `gen_random_uuid()` |
| `name` | `text` | NO | trimmed non-blank (CHECK); **UNIQUE** |
| `version` | `integer` | NO | default `1`; token |
| `created_at` | `timestamp with time zone` | NO | default `now()` |
| `updated_at` | `timestamp with time zone` | NO | default `now()` |

### 16.7 `email_list_recipients` — recipients of a list

| Column | Type | Null | Contract |
|---|---|---|---|
| `email_list_recipient_id` | `uuid` | NO | PK; default `gen_random_uuid()` |
| `email_list_id` | `uuid` | NO | FK → `email_lists(email_list_id)` `RESTRICT` |
| `address` | `text` | NO | trimmed non-blank (CHECK); minimal address shape validation `ADDRESS_INVALID` |
| `created_at` | `timestamp with time zone` | NO | default `now()` |

`UNIQUE (email_list_id, address)`. No `version` (the parent list's version protects the set —
parent-protects-child precedent). No display_name/position (S20).

### 16.8 `email_templates`

| Column | Type | Null | Contract |
|---|---|---|---|
| `email_template_id` | `uuid` | NO | PK; default `gen_random_uuid()` |
| `name` | `text` | NO | trimmed non-blank (CHECK); **UNIQUE** |
| `subject` | `text` | NO | trimmed non-blank (CHECK) |
| `body` | `text` | NO | trimmed non-blank (CHECK); stored verbatim — no placeholder processing (§14.2) |
| `document_type` | `text` | YES | CHECK `document_type IS NULL OR document_type IN ('peso','pegamentos','resumo')` |
| `version` | `integer` | NO | default `1`; token |
| `created_at` | `timestamp with time zone` | NO | default `now()` |
| `updated_at` | `timestamp with time zone` | NO | default `now()` |

---

## 17. Keys / constraints / indexes

### 17.1 Primary keys

```text
PK_pesos                       (peso_id)
PK_peso_measurement_rows       (peso_measurement_row_id)
PK_repairers                   (repairer_id)
PK_machine_repairer_assignments(machine_repairer_assignment_id)
PK_pdf_directory_settings      (pdf_directory_setting_id)
PK_email_lists                 (email_list_id)
PK_email_list_recipients       (email_list_recipient_id)
PK_email_templates             (email_template_id)
```

### 17.2 Unique constraints (unique indexes, accepted foundation style)

| Name | Table | Tuple | Meaning |
|---|---|---|---|
| `peso_measurement_rows_peso_position_key` | `peso_measurement_rows` | (`peso_id`, `row_position`) | dense positional ordering per Peso |
| `machine_repairer_assignments_machine_key` | `machine_repairer_assignments` | (`machine`) | at most one independent assignment per machine |
| `pdf_directory_settings_singleton_key` | `pdf_directory_settings` | (`singleton`) | the single-row mechanism |
| `email_lists_name_key` | `email_lists` | (`name`) | named list unambiguously addressable |
| `email_list_recipients_list_address_key` | `email_list_recipients` | (`email_list_id`, `address`) | one address once per list |
| `email_templates_name_key` | `email_templates` | (`name`) | named template |

**Deliberately absent:** any Peso business unique tuple (S1/Q-UNIQ); `repairers.name` unique
(Q-REP).

### 17.3 Foreign keys (all `ON DELETE RESTRICT`)

| FK | From | To |
|---|---|---|
| `FK_pesos_cm_contexts_cm_id` | `pesos.cm_id` | `cm_contexts(cm_id)` |
| `FK_pesos_tools_tool_id` | `pesos.tool_id` | `tools(tool_id)` |
| `FK_pesos_users_submitted_by_user_id` | `pesos.submitted_by_user_id` | `users(user_id)` |
| `FK_pesos_users_created_by_user_id` | `pesos.created_by_user_id` | `users(user_id)` |
| `FK_peso_measurement_rows_pesos_peso_id` | `peso_measurement_rows.peso_id` | `pesos(peso_id)` |
| `FK_machine_repairer_assignments_repairers_repairer_id` | `machine_repairer_assignments.repairer_id` | `repairers(repairer_id)` |
| `FK_email_list_recipients_email_lists_email_list_id` | `email_list_recipients.email_list_id` | `email_lists(email_list_id)` |

No FK in this schema cascades: deleting a repairer, a list, a CM context or a Tool referenced
here fails closed at the database — the backstop behind every `dependency-exists` refusal.

### 17.4 CHECK constraints (exact)

| Name | Table | Expression |
|---|---|---|
| `pesos_anchor_check` | `pesos` | `((cm_id IS NULL)::int + (tool_id IS NULL)::int) = 1` |
| `pesos_status_check` | `pesos` | `status IN ('pendente','aprovado','nao_aprovado')` |
| `pesos_temperature_check` | `pesos` | `water_temperature >= 5 AND water_temperature <= 35` |
| `pesos_marisa_volume_check` | `pesos` | `volume_marisa_bq IS NULL OR volume_marisa_bq >= 0` |
| `pesos_puncao_volume_check` | `pesos` | `volume_puncao_pu IS NULL OR volume_puncao_pu >= 0` |
| `pesos_density_check` | `pesos` | `glass_density_g_cm3 IS NULL OR glass_density_g_cm3 > 0` |
| `pesos_sap_reference_check` | `pesos` | `previous_production_end_reference IS NULL OR btrim(previous_production_end_reference) <> ''` |
| `pesos_sap_weight_check` | `pesos` | `previous_average_weight_reference IS NULL OR btrim(previous_average_weight_reference) <> ''` |
| `peso_measurement_rows_position_check` | `peso_measurement_rows` | `row_position >= 1` |
| `peso_measurement_rows_weight_check` | `peso_measurement_rows` | `water_weight_g > 0` |
| `peso_measurement_rows_capacity_check` | `peso_measurement_rows` | `capacity_cm3 > 0` |
| `peso_measurement_rows_glass_check` | `peso_measurement_rows` | `glass_weight_g > 0` |
| `repairers_name_required_check` | `repairers` | `btrim(name) <> ''` |
| `machine_repairer_assignments_machine_check` | `machine_repairer_assignments` | `machine IN ('B1','B2','B3','C1','C2','C3')` |
| `pdf_directory_settings_singleton_check` | `pdf_directory_settings` | `singleton` |
| `pdf_directory_settings_directory_required_check` | `pdf_directory_settings` | `btrim(base_directory) <> ''` |
| `email_lists_name_required_check` | `email_lists` | `btrim(name) <> ''` |
| `email_list_recipients_address_required_check` | `email_list_recipients` | `btrim(address) <> ''` |
| `email_templates_name_required_check` | `email_templates` | `btrim(name) <> ''` |
| `email_templates_subject_required_check` | `email_templates` | `btrim(subject) <> ''` |
| `email_templates_body_required_check` | `email_templates` | `btrim(body) <> ''` |
| `email_templates_document_type_check` | `email_templates` | `document_type IS NULL OR document_type IN ('peso','pegamentos','resumo')` |

### 17.5 Indexes (each justified by a contracted query)

| Index | Table | Columns | Unique | Justified by |
|---|---|---|---|---|
| `IX_pesos_cm_id` | `pesos` | `cm_id` | NO | FK-supporting; "pesos of this CM occurrence" (association candidates §4.4, read model §26.3, future Approve reads) |
| `IX_pesos_tool_id` | `pesos` | `tool_id` | NO | FK-supporting; "pending pesos of this Tool" (pending state queries, association candidates) |
| `peso_measurement_rows_peso_position_key` | `peso_measurement_rows` | `peso_id, row_position` | YES | ordering + "rows of this Peso" (always loaded as a set) |
| `machine_repairer_assignments_machine_key` | `machine_repairer_assignments` | `machine` | YES | single assignment per machine; machine-keyed read/upsert; P2-T07 resolution read |
| `machine_repairer_assignments_repairer_idx` | `machine_repairer_assignments` | `repairer_id` | NO | FK-supporting; "machines assigned to repairer X" (register surface) |
| `IX_email_list_recipients_email_list_id` (implied by the unique key) | `email_list_recipients` | `email_list_id, address` | YES | FK + "recipients of list" (always loaded as a set) |
| `IX_email_lists_*`/`IX_email_templates_*`/`IX_repairers_*` | — | — | — | **none** — registry-size reads, no contracted predicate query justifies an extra index (P2-T04 Q12 stance) |

No other index is contracted — in particular no index on `pesos.status`/`submitted_at` (the only
current consumer is the future P2-T06 pending-list query; each owning workstream contracts its own
query index, P2-T04 §12/Q12 precedent), no `production_date`-style speculative index.

### 17.6 Delete behavior summary

| Statement | Result |
|---|---|
| delete a `cm_contexts` row referenced by a Peso | refused — application probe (`peso` dependency, §20.4) + `RESTRICT` backstop |
| delete a `tools` row referenced by a pending Peso | refused — RESTRICT |
| delete a Peso / a row | **no path exists in P2-T05** (§7.6) |
| delete a repairer referenced by an assignment | refused — RESTRICT (`dependency-exists` naming `machine-assignment`); unreferenced repairers have no delete UI either (§10.3) |
| remove a machine assignment row | allowed — that is "clear the assignment" (§11.3) |
| delete an email list referenced downstream (P2-T08) | refused — RESTRICT backstop; P2-T05 deletes only with confirmation and no current dependency |
| delete a template | with confirmation; RESTRICT backstop for future dependencies |

---

## 18. Transactions

All multi-row writes open their own transaction at the repository
(`await using var transaction = await _context.Database.BeginTransactionAsync(...)` — accepted
`TemplateRepository` pattern). No ambient/unit-of-work, no second `DbContext`, no isolation-level
override, no advisory locks (P2-T04 §15.4 stance reused).

| Operation | Transaction boundary | Atomic unit |
|---|---|---|
| Create Peso (§7.1) | one transaction | `pesos` + every row row; failure ⇒ nothing |
| Calculate (§7.2) | none — read-only | — |
| Update draft (§7.3) | one transaction | rows replaced + derived recompute + version bump, all-or-nothing |
| Submit (§7.4) | one transaction | attribution + version, all-or-nothing |
| Associate (§4.4) | one transaction | anchor swap + version bump |
| Create/rename repairer | one transaction | single row |
| Set/change/clear machine assignment | one transaction | single row upsert/delete |
| Configure/change PDF directory | one transaction | single-row upsert |
| Create/update email list | one transaction | list row + **complete recipient set** (replace-all; no partial list state observable) |
| Delete email list / template | one transaction | single row (RESTRICT backstop for dependencies) |
| Create/update/delete email template | one transaction | single row |

Retry semantics: after a successful commit, retries are distinct new operations (no idempotency
key is invented — P2-T04 Q17 reasoning); after a rolled-back failure, a retry is a clean attempt.
A forced mid-transaction failure must leave zero rows of the operation (test rows §26.4).

---

## 19. Concurrency

### 19.1 Mechanism (accepted, reused)

- `version` tokens on every P2-T05 mutation target: `pesos`, `repairers`,
  `machine_repairer_assignments`, `pdf_directory_settings`, `email_lists`, `email_templates`
  (`integer NOT NULL DEFAULT 1`, `.IsConcurrencyToken()`).
- Child sets carry no token: `peso_measurement_rows` and `email_list_recipients` are protected by
  the parent row's version in the same transaction (accepted `template_modules`/context rows
  reasoning).
- Every guarded write: (1) explicit in-transaction version compare → `ConcurrencyConflictException`
  (domain type), and (2) the EF concurrency token stays active through the **`SaveAsync` helper**
  wrapping `SaveChangesAsync` and mapping `DbUpdateConcurrencyException` via the accepted
  `ConcurrencyConflictExceptionMapping.ToDomainConflict` — exactly the P2-T04 §15.1 correction
  pattern that is now the established repository convention (`JobOnRepository.SaveAsync`), so the
  save-time race surfaces as 409 `stale-version`, never 500 and never a silent overwrite.
- Services translate `ConcurrencyConflictException` → `Refused(StaleVersion)` → HTTP 409
  `stale-version`.

### 19.2 Per-operation matrix

| Operation | Observed version | On staleness | Rows written on staleness |
|---|---|---|---|
| Peso create | n/a | n/a (no uniqueness refusal; S1) | 0 on validation/configuration failure |
| Peso update | `ExpectedVersion` | `Refused(StaleVersion)` | 0 |
| Peso submit | `ExpectedVersion` | `Refused(StaleVersion)` | 0 |
| Peso associate | `ExpectedVersion` | `Refused(StaleVersion)` | 0 |
| repairer create/rename | n/a / `ExpectedVersion` | `Refused(StaleVersion)` | 0 |
| assignment set/clear | `ExpectedVersion` (or none on first set) | `Refused(StaleVersion)` | 0 |
| directory set/change | `ExpectedVersion` (or none on first set) | `Refused(StaleVersion)` | 0 |
| email list update/delete | `ExpectedVersion` | `Refused(StaleVersion)` | 0 |
| template create/update/delete | n/a / `ExpectedVersion` | `Refused(StaleVersion)` | 0 |

### 19.3 Rules

No silent retry, no last-write-wins, no automatic merge; one version increment per committed
mutation; reads never bump; a refused mutation reports the typed reason and the surface reloads
(`conflict` presentation, freeze §4). Settings writes follow the same discipline (Q-CONC default,
S13 — no extra settings audit).

---

## 20. Repository / application interfaces

### 20.1 Layer placement (accepted conventions only)

| Layer | Location |
|---|---|
| domain types | `src/DMO.Domain/Controlo/` (PesoId, PesoMeasurementRowId, Peso statuses, money-free value objects) — `DMO.Domain` depends on nothing |
| repository contracts | `src/DMO.Application/Repositories/` — flat `IPesoRepository`, `IRepairerRepository`, `IMachineRepairerAssignmentRepository`, `IPdfDirectorySettingsRepository`, `IEmailListRepository`, `IEmailTemplateRepository` |
| application area | `src/DMO.Application/ControloCreate/` — `{Area}Models.cs`, `{Area}Validator.cs`, `IControloCreateService.cs`, `ControloCreateService.cs`, `IControloDefinicoesService.cs`, `ControloDefinicoesService.cs` |
| persistence | `src/DMO.Infrastructure/Persistence/` (`Entities/`, `EntityConfigurations/`, `{Name}Repository.cs`, `PesoJobOnDependencyProbe.cs`) |
| Web | `src/DMO.Web/Endpoints/ControloCreateEndpoints.cs` (+ `ControloDefinicoesEndpoints.cs`), `src/DMO.Web/Pages/Controlo/Create.cshtml(.cs)`, `src/DMO.Web/Pages/Controlo/Definicoes.cshtml(.cs)` |

No new .NET project; no generic repository abstraction, no mediator, no CQRS, no second
`DbContext` (accepted architecture discipline).

### 20.2 Repository contracts (exact)

```csharp
namespace DMO.Application.Repositories;

public interface IPesoRepository
{
    Task<Peso?> GetByIdAsync(Guid pesoId, CancellationToken cancellationToken);          // + rows
    Task<Peso> CreatedAsync(Peso peso, IReadOnlyList<PesoMeasurementRow> rows,
        CancellationToken cancellationToken);
    Task<Peso> UpdatedAsync(Peso peso, IReadOnlyList<PesoMeasurementRow> rows,
        CancellationToken cancellationToken);
    Task<Peso> SubmittedAsync(Peso peso, CancellationToken cancellationToken);
    Task<Peso> AssociatedAsync(Peso peso, CancellationToken cancellationToken);
}

public interface IRepairerRepository
{
    Task<Repairer?> GetByIdAsync(Guid repairerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Repairer>> ListAsync(CancellationToken cancellationToken);
    Task<Repairer> CreatedAsync(Repairer repairer, CancellationToken cancellationToken);
    Task<Repairer> RenamedAsync(Repairer repairer, CancellationToken cancellationToken);
}

public interface IMachineRepairerAssignmentRepository
{
    Task<MachineRepairerAssignment?> GetByMachineAsync(string machine, CancellationToken cancellationToken);
    Task<IReadOnlyList<MachineRepairerAssignment>> ListAsync(CancellationToken cancellationToken);
    Task<MachineRepairerAssignment> SetAsync(MachineRepairerAssignment assignment, CancellationToken cancellationToken);
    Task ClearedAsync(string machine, int expectedVersion, CancellationToken cancellationToken);
}

public interface IPdfDirectorySettingsRepository
{
    Task<PdfDirectorySettings?> GetAsync(CancellationToken cancellationToken);
    Task<PdfDirectorySettings> SetAsync(PdfDirectorySettings settings, CancellationToken cancellationToken);
}

public interface IEmailListRepository
{
    Task<EmailList?> GetByIdAsync(Guid emailListId, CancellationToken cancellationToken); // + recipients
    Task<IReadOnlyList<EmailList>> ListAsync(CancellationToken cancellationToken);        // + counts
    Task<EmailList> CreatedAsync(EmailList list, IReadOnlyList<EmailRecipient> recipients, CancellationToken cancellationToken);
    Task<EmailList> UpdatedAsync(EmailList list, IReadOnlyList<EmailRecipient> recipients, CancellationToken cancellationToken);
    Task DeletedAsync(Guid emailListId, int expectedVersion, CancellationToken cancellationToken);
}

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByIdAsync(Guid emailTemplateId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmailTemplate>> ListAsync(CancellationToken cancellationToken);
    Task<EmailTemplate> CreatedAsync(EmailTemplate template, CancellationToken cancellationToken);
    Task<EmailTemplate> UpdatedAsync(EmailTemplate template, CancellationToken cancellationToken);
    Task DeletedAsync(Guid emailTemplateId, int expectedVersion, CancellationToken cancellationToken);
}
```

Binding rules (same as P2-T04 §12.2): `CancellationToken` mandatory last; `Task<T?>` single
reads / `Task<IReadOnlyList<T>>` lists / `Task` writes; multi-row writes open their own
transaction; private static `Project(...)` mapping; constraint violations mapped by
`PostgresException.SqlState` + constraint name to typed failures (`DuplicateIdentity`-style where
applicable, e.g. `email_lists_name_key` → `DuplicateName`); repositories own no domain rule;
services contain no SQL.

### 20.3 Service contracts (exact)

```csharp
namespace DMO.Application.ControloCreate;

public interface IControloCreateService
{
    Task<PesoResult> CalculateAsync(CalculatePesoCommand command, CancellationToken cancellationToken);   // read-only
    Task<PesoResult> CreateAsync(CreatePesoCommand command, CancellationToken cancellationToken);
    Task<PesoResult> GetAsync(Guid pesoId, CancellationToken cancellationToken);
    Task<PesoResult> UpdateAsync(UpdatePesoCommand command, CancellationToken cancellationToken);
    Task<PesoResult> SubmitAsync(SubmitPesoCommand command, CancellationToken cancellationToken);
    Task<PesoResult> AssociateAsync(AssociatePesoCommand command, CancellationToken cancellationToken);
    Task<PesoResult> ListAssociationCandidatesAsync(Guid toolId, CancellationToken cancellationToken);
}

public interface IControloDefinicoesService
{
    // repairers
    Task<SettingsResult> ListRepairersAsync(CancellationToken cancellationToken);
    Task<SettingsResult> CreateRepairerAsync(CreateRepairerCommand command, CancellationToken cancellationToken);
    Task<SettingsResult> RenameRepairerAsync(RenameRepairerCommand command, CancellationToken cancellationToken);
    // machine assignments
    Task<SettingsResult> ListMachineAssignmentsAsync(CancellationToken cancellationToken);
    Task<SettingsResult> SetMachineAssignmentAsync(SetMachineAssignmentCommand command, CancellationToken cancellationToken);
    Task<SettingsResult> ClearMachineAssignmentAsync(ClearMachineAssignmentCommand command, CancellationToken cancellationToken);
    // pdf directory
    Task<SettingsResult> GetPdfDirectoryAsync(CancellationToken cancellationToken);
    Task<SettingsResult> SetPdfDirectoryAsync(SetPdfDirectoryCommand command, CancellationToken cancellationToken);
    Task<SettingsResult> CheckPdfDirectoryAsync(CancellationToken cancellationToken);   // Q-PDF gated
    // email lists
    Task<SettingsResult> ListEmailListsAsync(CancellationToken cancellationToken);
    Task<SettingsResult> GetEmailListAsync(Guid emailListId, CancellationToken cancellationToken);
    Task<SettingsResult> CreateEmailListAsync(CreateEmailListCommand command, CancellationToken cancellationToken);
    Task<SettingsResult> UpdateEmailListAsync(UpdateEmailListCommand command, CancellationToken cancellationToken);
    Task<SettingsResult> DeleteEmailListAsync(DeleteEmailListCommand command, CancellationToken cancellationToken);
    // email templates
    Task<SettingsResult> ListEmailTemplatesAsync(CancellationToken cancellationToken);
    Task<SettingsResult> GetEmailTemplateAsync(Guid emailTemplateId, CancellationToken cancellationToken);
    Task<SettingsResult> CreateEmailTemplateAsync(CreateEmailTemplateCommand command, CancellationToken cancellationToken);
    Task<SettingsResult> UpdateEmailTemplateAsync(UpdateEmailTemplateCommand command, CancellationToken cancellationToken);
    Task<SettingsResult> DeleteEmailTemplateAsync(DeleteEmailTemplateCommand command, CancellationToken cancellationToken);
}
```

Validators are pure static, run before any write, and return the closed §26 code set.
`ControloCreateService` composes the P2-T04 application contracts (`IJobOnService`,
`IToolService`) for selection/context reads and for the missing-CM association (§4.3) — the Web
layer never queries the database directly and no repository reads another module's tables.

### 20.4 Integration seams (exact)

1. **`PesoJobOnDependencyProbe : IJobOnDependencyProbe`** — `src/DMO.Infrastructure/Persistence/`,
   registered with one additive line
   `services.AddScoped<IJobOnDependencyProbe, PesoJobOnDependencyProbe>();`. It reports
   `peso` (kind) with a human description for every Peso whose `cm_id` is one of the target's
   context ids **or** whose pending `tool_id` is the target's Tool when the probe target is a
   context-bearing Job On (scope: the `JobOnDependencyTarget` context ids). This makes a Job On
   delete (and a CM removal inside a Job On edit) refuse with `dependency-exists` naming `peso`
   before the `RESTRICT` FK backstop. No dependency table is introduced (P2-T04 §11.5).
2. **Association-candidate read (cross-stream additive proposal, Q-CAND):** one additive
   read-only method on the Job On application contract —
   `Task<JobOnResult> ListPesoAssociationCandidatesAsync(Guid toolId, CancellationToken)` returning
   records `(Guid CmContextId, Guid JobOnId, string Reference, string ProductionNumber, string
   Machine)` for every `cm_contexts` row resolving to that `tool_id`. It is the accepted §8.3
   reverse read of P2-T04 now exposed to the consuming workflow; it modifies **no** existing
   member and no Job On route (the P2-T05 route carries `controlo-create`). Fallback if the
   Architect declines: compose N `GetAsync` ficha reads under `controlo-create` from the
   `ferramentas` ficha's usage occurrences — recorded for the review; the default is the additive
   method.
3. **Shared Tool orchestration:** CM Tool search/select/create reuses the accepted
   `ferramentas`-gated routes 12/13 of P2-T04 (ToolPicker mechanics, opaque keys, inline create,
   origin-state preservation all inherited unchanged). P2-T05 never mints a Tool id and never
   writes `tools`.
4. **Reference → productions:** `IJobOnService.FindProductionsAsync` consumed through P2-T05's
   `controlo-create`-gated read routes (§21.3) — the established "the reads the workflow needs"
   pattern (P2-T04 §13.3/Q19 applied to Controlo Create per `ACCESS_MODEL.md` §9).
5. **Published Peso read model (C → D seam):** the carrier shapes `PesoSheetReadModel` of §26.3
   are published by this contract; the D-owned pending-list/review **routes** are P2-T06's
   contract; D must not fork the renderer (its obligation, recorded in seam §29).

---

## 21. Routes / endpoints

### 21.1 Base path

```text
Controlo Create   /controlo/create   (Razor page + minimal-API JSON endpoints)
Definições        /controlo/create/definicoes  (inside the Controlo Create working area —
                                                NOT a destination, NOT a Module)
```

`controlo` is the accepted `DestinationId` shared by `ControloCreate`/`ControloApprove`
(`ModuleCatalog.cs`); no destination/route registration happens in P2-T05 (§21.6).

### 21.2 Policy rule

Every route declares **exactly one** canonical Module policy:
`ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloCreate)` — on every Create
route/action **and on every Definições route/action** (`…DELTA.md` §1.5; handoff §8). No route
carries `controlo-approve`, no route is anonymous, no policy is created. Razor `[Authorize]`
arguments are compile-time constants pinned in `ControloPolicyNames` (asserted equal to the
canonical projection — the accepted `JobOnPolicyNames` pattern). The P2-T05 routes that compose
P2-T04 application reads never bypass a policy: the Tool search/create calls stay on the
`ferramentas` routes (existing P2-T04 routes 12/13) — a P2-T05 page cannot grant Tool access.

### 21.3 Complete P2-T05 route table

| # | Route | Kind | Purpose | Policy | Request carrier | Result carrier | Failure states |
|---|---|---|---|---|---|---|---|
| 1 | `GET /controlo/create` | page `Pages/Controlo/Create` | Novo controlo surface (R1–R8) | `controlo-create` | `?jobonId=` / `?pesoId=` (open existing draft) | rendered page | 403; unavailable-module 403 |
| 2 | `GET /controlo/create/productions` | minimal API | reference → productions (explicit selection source) | `controlo-create` | `?reference=` | `JobOnProductionListResponse` (accepted shape) | 400 `REFERENCE_REQUIRED`, 403, 200 empty |
| 3 | `GET /controlo/create/jobons/{jobonId:guid}` | minimal API | Job On ficha + CM context (frozen triple + live tool projection) for the strip/context regions | `controlo-create` | route `jobonId` | `JobOnFicha`-derived composition (accepted shape, §26.3) | 400, 404, 403 |
| 4 | `GET /controlo/create/tools/{toolId:guid}/association-candidates` | minimal API | pending-association candidates resolving to the anchor Tool | `controlo-create` | route `toolId` | `PesoAssociationCandidateResponse` (`cmId, jobonId, reference, productionNumber, machine`) | 404, 403 (additive Job On read, §20.4) |
| 5 | `POST /controlo/create/pesos` | minimal API | create Peso (draft) transactionally | `controlo-create` | `CreatePesoRequest` | 201 `PesoCreatedResponse` (`pesoId`, `version`) | 400 `validation-failed`, 409 `calculation-configuration-missing`, 403 |
| 6 | `GET /controlo/create/pesos/{pesoId:guid}` | minimal API | Peso read (ficha: anchor projection, inputs, rows+results, submitted attribution) | `controlo-create` | route `pesoId` | `PesoSheetResponse` (§26.3) | 404, 403 |
| 7 | `PUT /controlo/create/pesos/{pesoId:guid}` | minimal API | update draft (inputs + whole row set, recompute) | `controlo-create` | `UpdatePesoRequest` | 200 `PesoUpdatedResponse` (`pesoId`, `version`) | 400, 404, 409 `stale-version` \| `already-submitted`, 403 |
| 8 | `POST /controlo/create/pesos/{pesoId:guid}/calculate` | minimal API | stateless calculate (no write) | `controlo-create` | `CalculatePesoRequest` (current input facts) | 200 `PesoCalculationResponse` (per-row results, 2-dp presentation) | 400, 409 `calculation-configuration-missing`, 403 |
| 9 | `POST /controlo/create/pesos/{pesoId:guid}/submit` | minimal API | submit the same `peso_id` (reviewable handoff) | `controlo-create` | `{ expectedVersion }` | 200 `PesoSubmittedResponse` (`pesoId`, `version`, `submittedAt`) | 400, 404, 409 `stale-version` \| `already-submitted`, 403 |
| 10 | `POST /controlo/create/pesos/{pesoId:guid}/associate` | minimal API | explicit pending→cm association | `controlo-create` | `{ cmId, expectedVersion }` | 200 `PesoAssociatedResponse` (`pesoId`, `version`, `cmId`) | 400, 404, 409 `stale-version` \| `already-submitted` \| `association-mismatch` \| `already-associated`, 403 |
| 11 | `POST /controlo/create/jobons/{jobonId:guid}/cm-association` | minimal API | create the missing CM context (composes `IJobOnService` association Set; CM-slot only, explicit human confirmation) | `controlo-create` | `{ toolId, expectedJobOnVersion }` | 201 `CmAssociationCreatedResponse` (`jobonId`, `cmId`, `version`) | 400 `TOOL_TYPE_MISMATCH`/`TOOL_NOT_FOUND`, 404, 409 `stale-version`, 403 |
| 12 | `GET /controlo/create/definicoes` | page `Pages/Controlo/Definicoes` | Definições surface — five sections (§9) | `controlo-create` | — | rendered page | 403 |
| 13 | `GET /controlo/create/definicoes/repairers` · `POST …/repairers` · `PUT …/repairers/{repairerId:guid}` | minimal API | list / add / rename repairers | `controlo-create` | `CreateRepairerRequest` / `RenameRepairerRequest` | 200 list / 201 `{ repairerId, version }` / 200 `{ repairerId, version }` | 400, 404, 409 `stale-version`, 403 |
| 14 | `GET /controlo/create/definicoes/machine-assignments` · `PUT …/machine-assignments/{machine}` | minimal API | list all six assignments / set-change-clear one (independent) | `controlo-create` | `{ repairerId? }` | 200 / 200 `{ machine, version }` | 400 `MACHINE_UNKNOWN` \| `REPAIRER_NOT_FOUND`, 409 `stale-version`, 403 |
| 15 | `GET /controlo/create/definicoes/pdf-directory` · `PUT …/pdf-directory` · `POST …/pdf-directory/check` | minimal API | read / configure / change / check the base directory | `controlo-create` | `{ baseDirectory, expectedVersion? }` / — | 200 / 200 `{ version }` / 200 `PdfDirectoryCheckResponse` | 400 `DIRECTORY_REQUIRED` \| `DIRECTORY_INVALID`, 409 `stale-version`, 403; check returns typed `not-configured \| ok \| directory-not-found \| not-a-directory \| access-denied \| invalid-path \| check-failed` |
| 16 | `GET/POST/PUT/DELETE /controlo/create/definicoes/email-lists[/{emailListId:guid}]` | minimal API | list / create / update (name + full recipient set) / delete | `controlo-create` | `CreateEmailListRequest` / `UpdateEmailListRequest` (recipients[]) | 200 / 201 / 200 / 204 | 400, 404, 409 `stale-version` \| `duplicate-name` \| `dependency-exists`, 403 |
| 17 | `GET/POST/PUT/DELETE /controlo/create/definicoes/email-templates[/{emailTemplateId:guid}]` | minimal API | list / create / update / delete templates | `controlo-create` | `Create/UpdateEmailTemplateRequest` | 200 / 201 / 200 / 204 | 400, 404, 409 `stale-version` \| `duplicate-name` \| `dependency-exists`, 403 |

**Route-count statement:** exactly the 17 rows above. No approval route, no Boquilhas route, no
document/generation route, no availability route, no route carrying a second policy, no
standalone Tool-create page (shared orchestration only), no Definições destination registration.

### 21.4 The compose route (11) — exact semantics

The missing-CM association is the accepted "consuming workflow creates the missing context"
rule (P2-T04 contract §6.7; `BETA_VERSION.md` §2.1). Route 11 loads the current Job On ficha,
builds an `UpdateJobOnCommand` with **Keep** for every fact and **Set** for the CM slot (the
supplied canonical CM `tool_id`, validated existence + `TOOL_TYPE_MISMATCH`), and invokes
`IJobOnService.UpdateAsync` — so the `cm_contexts` row is created **by Job On's own application
and repository code**, never by a Controlo repository, with the frozen triple read from `tools`
at that moment (P2-T04 §7.4). It performs no other Job On change. The version observed in the
ficha read is the `expectedJobOnVersion`.

### 21.5 Definições gating proof (contractual)

A caller holding only `controlo-approve` is denied routes 12–17 server-side (and every other
P2-T05 route); the shared `controlo` destination never merges grants
(`NoProfilesRegressionTests` sibling rule). Direct-route denial is tested (§26.4 AUTH rows).

### 21.6 Interim runtime state (mandatory)

```text
ModuleRegistrations.CurrentBuildAvailable = []            (unchanged by P2-T05)
EmptyDestinationRouteRegistry                             (unchanged)
DestinationRouteRegistrations                             (unchanged — still empty)
```

P2-T05 pages/endpoints exist and are server-gated, but `AccessResolver` denies every P2-T05 route
to every caller **until P2-T10 registers `controlo-create` availability** (accepted interim rule,
P2-T04 §13.5). P2-T05 adds no availability entry, no route registration, no navigation entry;
integration tests use test-only module registries.

---

## 22. Authorization

| Concern | Contract |
|---|---|
| Module | `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloCreate)` on every P2-T05 route/action, Create and Definições alike |
| Admin | ADMIN gains **no** operational access (`ModuleAuthorizationHandler` fails closed for non-USER accounts) — no `dmo.administration` policy on any P2-T05 route |
| Controlo Approve | no P2-T05 route carries `controlo-approve`; Approve-only callers are denied everything here; Create-only callers are denied every P2-T06 route (P2-T06's obligation, same mechanism) |
| Settings authorization | the same `controlo-create` capability owns `Definições` (recorded: one capability owns settings — handoff §8/§29; `…DELTA.md` §1.5) |
| Reads | reference→productions, ficha, candidates are provided on `controlo-create`-gated routes composing accepted application contracts (the "reads needed to operate the surface" pattern, P2-T04 §13.3); **Tool search/create remains `ferramentas`-gated** (P2-T04 Q21) |
| Presentation | navigation/button visibility never substitutes for the server-side gate |
| Denial | denial is never an empty list, never a blank surface, never a success (freeze §4; `ACCEPTANCE_MATRIX.md` §9) |

---

## 23. P2-T03 / P2-T04 composition

### 23.1 Consumed P2-T03 primitives (unchanged)

| Primitive | Consumed for | Mapping rule |
|---|---|---|
| `MeasurementRowsPresentation` (+ `_MeasurementRows`) | the Peso water-weight rows (R4) | minimum row count 1 (mandatory `MinimumViolationReason`), opaque frontend row keys, stable identity, focus rules — **all** mechanics; the weight field is a supplied generic numeric field; no domain rule enters the component |
| `DecisionBarPresentation` (+ `_DecisionBar`) | Calcular / Guardar / Submeter / Cancelar (R6) | actions supplied with availability/disabled reasons and pending keys; no approve/reject semantics encoded |
| `ToolPickerPresentation` (+ `_ToolPicker`) | CM Tool search/select/create for the pending anchor and the missing-CM recovery | opaque adapter keys; origin-state preservation inherited; create subflow posts to the existing `POST /ferramentas/tools` (P2-T04 route 13); never auto-selects |
| `ToolSummaryRowPresentation` | the associated CM Tool summary (R3) | facts supplied from the real reads; frozen triple shown separately from the live projection |
| `CommonState`/`CommonStateRegionPresentation` (+ `_CommonStateRegion`) | every non-ready surface | the sole state vocabulary; the four mandated distinctions stay distinct |
| `DenseTablePresentation` (+ `_DenseDataTable`) | productions list (R2), repairers list, email lists/templates tables in Definições | rows/keys supplied by P2-T05; component fetches nothing |
| `RecordStatusPresentation` (+ `_RecordStatus`) | Peso status text (Pendente) | supplied text; status grants no action |
| `AvailabilityState`/`_AvailabilityState` | **not used by P2-T05** — document availability is P2-T08 (Q-DOCREAD) | — |
| `AuditTrailPresentation` | **not used** — P2-T05 records no audit facts (S13) | — |

### 23.2 Consumed P2-T04 contracts (unchanged)

`IJobOnService` (productions, ficha, association via §21.4), `IToolService`/Tool routes (shared
orchestration), the frozen context triple + live Tool projection reads, `IJobOnDependencyProbe`
(probe seam — P2-T05 supplies an implementation, P2-T04's interface is untouched), the result/
refusal vocabulary shapes, the concurrency mapping helper, `MachineCode`-style machine validation
(§16.4 CHECK + validator).

### 23.3 Composition rules (binding)

1. **No shared artifact is modified** — no P2-T01/T02/T03/T04 contract type, partial, CSS block,
   asset, interaction model or test changes; `dmo-components.css` is untouched; P2-T05 CSS lives
   in a **new** `dmo-controlo.css` (accepted per-surface stylesheet precedent) with no
   `@media`/`@container`/`@supports`/token-declaration/colour-literal in the appended-region
   sense; any JS adapter is a **new** `dmo-controlo.js` reusing `window.dmoFocus`.
2. **Opaque keys stay opaque** — no P2-T05 contract type/fixture/DOM hook declares a key to be
   `peso_id`, `tool_id`, `cm_id`, `jobon_id` or any canonical identity.
3. **No look-alike components** — P2-T05 creates no second picker/table/bar/state region. The
   production-context strip is rendered page-owned because `ProductionContextStrip` is **not
   implemented** in the accepted shared set (freeze §6 defines it; no workstream shipped it);
   when its owner implements it, P2-T05 adopts it (Q-PCS).
4. **Domain orchestration is P2-T05's** — the formulas, anchor rules, association flow, snapshot
   semantics, settings rules live in the domain/application layer, never in a shared component.
5. **Fixed desktop** applies to every P2-T05 surface (§24).

---

## 24. Fixed desktop

Every P2-T05 surface inherits the binding **DMO FIXED DESKTOP LAYOUT POLICY** (master plan;
freeze §1A; P2-T02 §4; P2-T03 §6; P2-T04 Appendix C):

1. canonical design/validation viewport **1366 × 768**; compact operational density mandatory;
2. regions R1–R8 and the five Definições sections keep their assigned structural region at any
   desktop width; larger resolutions preserve composition (extra space = whitespace/limited
   non-structural expansion);
3. **no breakpoint-driven structural or semantic variant**: no `@media`/`@container`/`@supports`
   structural rule, no width listener, no control moving between regions;
4. no table→card conversion, no required-column hiding, no action relocation, no side-panel
   stacking below the work area, no alternate mobile/tablet navigation;
5. smaller windows scroll (page-level or keyboard-reachable local overflow containers — e.g. the
   per-CM results table, the Definições tables); over-wide regions use local horizontal scroll;
6. mobile/tablet/touch-first layouts are out of scope;
7. design priority: workflow stability → predictable control placement → information density →
   readability → consistency → visual polish.

---

## 25. Migration contract

### 25.1 One migration, one owner

P2-T05 owns **exactly one new migration**:

```text
src/DMO.Infrastructure/Migrations/<UTC timestamp>_ControloCreateDomain.cs        (+ .Designer.cs)
```

- EF-generated (`dotnet ef migrations add ControloCreateDomain` through the existing
  `DesignTimeDmoDbContextFactory`), not hand-written; accepted shape (`#nullable disable`,
  block-scoped namespace, `public partial class ControloCreateDomain : Migration`);
- `DmoDbContextModelSnapshot.cs` is extended by EF as part of generation — the documented,
  EF-owned extension of an existing persistence file, exactly as P2-T04 §16.1;
- migrations 001/002/003 (incl. their Designers) are **never edited** (regression-asserted byte
  identity);
- `DmoDbContext.cs` is **deliberately NOT modified** — new entities are discovered through
  `ApplyConfigurationsFromAssembly`; repositories use `_context.Set<TEntity>()`.

### 25.2 Expected schema delta (exact)

Created: **8 tables** — `pesos`, `peso_measurement_rows`, `repairers`,
`machine_repairer_assignments`, `pdf_directory_settings`, `email_lists`,
`email_list_recipients`, `email_templates` — with exactly the columns, nullability, defaults,
CHECKs, unique constraints, FKs (all `RESTRICT`) and indexes of §16/§17.

Not created: any other table, any seed/reference row, any trigger, function, view, extension,
sequence, enum type, RLS policy, history-manipulation statement, or any column not listed in
§16. In particular no Comparação/Pegamentos/Folha/Resumo/approval/document/water-table table.

### 25.3 Safety rules

1. purely additive; no `DROP`/`TRUNCATE`/type change/rename/`EnsureDeleted`;
2. applying twice is a no-op (EF history); the migration runner is unchanged;
3. PostgreSQL/Supabase-compatible by construction (only portable features — same evidence family
   as P2-T04 §19; verified against the **disposable** PostgreSQL test database, never a live
   Supabase change, which is an operator's deployment step, not this authoring task's).

### 25.4 Rollback expectations

`Down` drops exactly the eight created tables in reverse-dependency order:

```text
email_templates, email_list_recipients, email_lists, pdf_directory_settings,
machine_repairer_assignments, repairers, peso_measurement_rows, pesos
```

(exact inverse of `Up`; EF-generated ordering acceptable where referentially safe, per P2-T04
Finding-3 adjudication).

---

## 26. Failure / result vocabulary

### 26.1 Result unions (closed sets)

```csharp
namespace DMO.Application.ControloCreate;

public abstract record PesoResult
{
    public sealed record Created(Guid PesoId, int Version) : PesoResult;
    public sealed record Updated(Guid PesoId, int Version) : PesoResult;
    public sealed record Submitted(Guid PesoId, int Version, DateTimeOffset SubmittedAt) : PesoResult;
    public sealed record Associated(Guid PesoId, int Version, Guid CmId) : PesoResult;
    public sealed record Calculation(PesoCalculation Calculation) : PesoResult;
    public sealed record Found(PesoSheetReadModel Sheet) : PesoResult;
    public sealed record Candidates(IReadOnlyList<PesoAssociationCandidate> Candidates) : PesoResult;
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : PesoResult;
    public sealed record NotFound(Guid PesoId) : PesoResult;
    public sealed record Refused(PesoRefusalReason Reason, string Message) : PesoResult;
}

public enum PesoRefusalReason
{
    StaleVersion, AlreadySubmitted, AlreadyAssociated,
    AssociationMismatch, CalculationConfigurationMissing, DependencyExists,
}

public abstract record SettingsResult
{
    // per-surface successes: RepairersFound / RepairerCreated / RepairerRenamed /
    // AssignmentsFound / AssignmentSet / AssignmentCleared / PdfDirectoryFound /
    // PdfDirectorySaved / PdfDirectoryCheck(PdfDirectoryCheckResult) /
    // EmailListsFound / EmailListFound / EmailListCreated / EmailListUpdated / EmailListDeleted /
    // EmailTemplatesFound / EmailTemplateFound / EmailTemplateCreated / EmailTemplateUpdated /
    // EmailTemplateDeleted
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : SettingsResult;
    public sealed record NotFound(Guid Id) : SettingsResult;
    public sealed record DuplicateName(string Name) : SettingsResult;
    public sealed record Refused(SettingsRefusalReason Reason, string Message,
        IReadOnlyList<SettingsDependency>? Dependencies = null) : SettingsResult;
}

public enum SettingsRefusalReason
{
    StaleVersion, DependencyExists,
}
```

### 26.2 Domain/token vocabulary (exact transport strings)

| Token | Raised by | Meaning |
|---|---|---|
| `validation-failed` (400) | every mutation/query | `errors[]` carries §5.5/§10.4/§12.2/§13.2/§14.2 codes: `REFERENCE_REQUIRED`, `PRODUCTION_NUMBER_REQUIRED`, `MACHINE_UNKNOWN`, `TEMPERATURE_OUT_OF_RANGE`, `WATER_TEMPERATURE_REQUIRED`, `ROW_REQUIRED`, `ROW_WEIGHT_INVALID`, `ROW_POSITION_INVALID`, `DUPLICATE_ROW_POSITION`, `VOLUME_NEGATIVE`, `PESO_ANCHOR_REQUIRED`, `PESO_ANCHOR_CONFLICT`, `CM_CONTEXT_NOT_FOUND`, `TOOL_NOT_FOUND`, `TOOL_TYPE_MISMATCH`, `NAME_REQUIRED`, `REPAIRER_NOT_FOUND`, `DIRECTORY_REQUIRED`, `DIRECTORY_INVALID`, `LIST_NAME_REQUIRED`, `TEMPLATE_NAME_REQUIRED`, `SUBJECT_REQUIRED`, `BODY_REQUIRED`, `DOCUMENT_TYPE_UNKNOWN`, `ADDRESS_REQUIRED`, `ADDRESS_INVALID`, `DELETE_NOT_CONFIRMED` |
| `stale-version` (409) | every guarded write | observed version no longer current; nothing written |
| `already-submitted` (409) | edit/associate/submit of a submitted Peso | Create-side mutation closed after submit |
| `already-associated` (409) | associate on a production-bound Peso | anchor swap only from pending |
| `association-mismatch` (409) | associate | candidate `cm_id` does not resolve to the anchor `tool_id` |
| `calculation-configuration-missing` (409) | create/calculate/update/submit | divisor or density mapping unavailable; no invented value; nothing written |
| `dependency-exists` (409) | Job On delete/CM removal (via probe), list/template delete | `dependencies[]` names kinds (`peso`, `machine-assignment`, `duplication-lineage`, …); nothing deleted |
| `duplicate-name` (409) | email list/template create/rename | `email_lists_name_key`/`email_templates_name_key` collision |
| `not-found` (404) | reads and mutation targets | the record does not exist |
| `not-configured` (200) | pdf-directory check | no row configured yet — explicit state, not an error |

Denial rules are the accepted ones: never an empty result/page on denial; lookup failure never
maps to `empty`; 409 is the single refusal status for persisted-state refusals; no error body
contains paths-to-secrets/connection strings/other users' data; `permission-denied` presentation
is never a blank surface.

### 26.3 Published read-model shapes (C → D seam, exact)

```csharp
public sealed record PesoSheetReadModel(
    Guid PesoId, int Version, string Status,            // 'pendente' | 'aprovado' | 'nao_aprovado'
    Guid? CmId, Guid? ToolId,                           // exactly one non-null (anchor)
    PesoContextProjection? Context,                     // production case:
    PesoPendingProjection? Pending,                     // pending case (tool facts)
    PesoProductionProjection? Production,               // traversal: reference, productionNumber,
                                                        // machine (NULL when pending)
    Guid? CreatedByUserId, DateTimeOffset CreatedAt,
    Guid? SubmittedByUserId, DateTimeOffset? SubmittedAt,
    decimal WaterTemperature,
    decimal? VolumeMarisaBq, decimal? VolumePuncaoPu, decimal? GlassDensityGCm3,
    string? PreviousProductionEndReference, string? PreviousAverageWeightReference,
    IReadOnlyList<PesoRowReadModel> Rows);

public sealed record PesoContextProjection(            // composed from cm_contexts + tools (live)
    Guid CmId, Guid ToolId,
    string FrozenToolType, string FrozenToolReference, string FrozenToolLot,
    ToolSummaryProjection Tool);                        // accepted P2-T04 live projection

public sealed record PesoPendingProjection(Guid ToolId, ToolSummaryProjection Tool);

public sealed record PesoProductionProjection(
    string Reference, string ProductionNumber, string Machine);   // traversal through cm_id

public sealed record PesoRowReadModel(
    Guid PesoMeasurementRowId, int RowPosition,
    decimal WaterWeightG, decimal CapacityCm3, decimal GlassWeightG);
```

- The statuses/values are backend facts; presentation rounds to ≤ 2 dp only at render time;
- the read is a **read-only projection**: composing it never writes, never bumps, never creates
  (P2-T04 read-transaction rule);
- D (P2-T06) consumes this shape through its **own** `controlo-approve`-gated routes (P2-T06's
  contract) and must not fork the renderer (seam §29).

### 26.4 Test-to-acceptance matrix (Part 34 — complete)

Test classes follow the accepted naming (`<ID>_<PascalCaseClauses>`, XML summary naming the AC).
Classes: U = unit, I = integration/host (HTTP + gates + composition), DB = disposable PostgreSQL,
S = static/architecture scan, R = rendered. The matrix is bidirectional-complete against §30
(CRITERION ↔ PROOF).

#### PESO IDENTITY

| # | Class | Test | Proves |
|---|---|---|---|
| PID1 | DB | create with a real `cm_id` persists the Peso with `cm_id` set and `tool_id` NULL; read model resolves `cm_id → tool_id → jobon_id` | AC-P1, AC-A1 |
| PID2 | DB | pending create persists `tool_id` with `cm_id` NULL; the row is `pendente` and measurable | AC-P2, AC-P3 |
| PID3 | DB | the anchor CHECK rejects both-NULL and both-set rows | AC-P1 |
| PID4 | U | `peso_id` appears in no command/request as client-supplied; only the backend allocates it | AC-P4 |
| PID5 | S | no `production_id`, `job_on_revision_id`, reverse array, jsonb measurement blob, or legacy identity column exists in the new schema/code | AC-A6 |
| PID6 | DB | pending→associate: candidate `cm_id` resolving to the same `tool_id` accepts; the anchor swaps and `tool_id` clears; version increments once | AC-P5, AC-A2 |
| PID7 | DB | associate with a `cm_id` resolving to a different Tool → 409 `association-mismatch`, nothing written | AC-P5 |
| PID8 | DB | associate on a production-bound or already-submitted Peso → 409 `already-associated`/`already-submitted` | AC-P5 |
| PID9 | U | association candidates return only real `cm_contexts` rows resolving to the anchor Tool; no inference/ranking | AC-P6 |
| PID10 | U/DB | no Peso uniqueness tuple: two Pesos for the same cm/date are both legal and distinct | AC-P7 (Q-UNIQ default) |

#### MEASUREMENTS

| # | Class | Test | Proves |
|---|---|---|---|
| MES1 | U | temperature < 5 or > 35 → `TEMPERATURE_OUT_OF_RANGE`; boundary 5 and 35 accepted | AC-M1 |
| MES2 | U/DB | per-row `water_weight_g` stored exactly (numeric(18,4)); a row with weight ≤ 0 → `ROW_WEIGHT_INVALID` | AC-M2 |
| MES3 | U/DB | capacity = `water_weight ÷ divisor` computed by the backend; result stored per row | AC-M3 |
| MES4 | U/DB | glass weight = `(capacity + Marisa − PU) × density`; result stored per row | AC-M4 |
| MES5 | U | variable rows: 1..n accepted; 0 rows → `ROW_REQUIRED`; dense positions; `DUPLICATE_ROW_POSITION` | AC-M5 |
| MES6 | U/DB | rows are persisted as rows with stable ids and dense positions; re-read after save returns the same facts | AC-M6 |
| MES7 | U | presentation rounds to ≤ 2 dp while the stored numeric(18,4) is untouched (parity proof with 3+ dp inputs) | AC-M7 |
| MES8 | U | averages/deviations are derived and never stored; individual results all present in the read model | AC-M8 |
| MES9 | U | missing calculation configuration → `calculation-configuration-missing` (nothing invented, no zero) | AC-M3, AC-M9 (Q-CALC) |
| MES10 | DB | `glass_density_g_cm3` frozen once on the Peso at first calculate; a later density-mapping change does not rewrite it | AC-H3, AC-M4 |

#### SNAPSHOT

| # | Class | Test | Proves |
|---|---|---|---|
| SNA1 | DB | after `UPDATE tools SET reference/lot`, the context triple behind the Peso is unchanged (P2-T04 frozen context reused) | AC-H1 |
| SNA2 | DB | after a Job On edit (facts changed), the Peso's stored inputs/results/attribution are unchanged; the production projection reflects traversal (documented behavior) | AC-H1, AC-H2 |
| SNA3 | DB | stored capacity/glass weight are never recomputed from current Tool state — no refresh path exists (static scan + DB proof) | AC-H2 |
| SNA4 | DB | a later CM re-selection (`Set`) keeps `cm_id` and id stable; the Peso read composes live projection + frozen triple as separate facts | AC-H1 |
| SNA5 | U/S | no code path copies live Tool values into Peso rows; no snapshot engine/table exists | AC-H3, AC-A6 |

#### CREATE

| # | Class | Test | Proves |
|---|---|---|---|
| CRE1 | DB | create inserts `pesos` + all rows in one transaction; forced failure → zero rows | AC-C1 |
| CRE2 | DB | create commits only at COMMIT; nothing observable before | AC-C1 |
| CRE3 | DB | `version = 1` after create; update increments exactly once; read never increments | AC-C7, AC-C8 |
| CRE4 | U | validator runs before any write (create/update/submit share the closed code set) | AC-C2 |
| CRE5 | DB | update replaces the whole row set atomically; no partial rows observable | AC-C3 |
| CRE6 | DB | save-time concurrency race (second connection bumps version mid-save) → 409 `stale-version`, losing facts not persisted (SaveAsync pattern) | AC-C7, AC-C9 |
| CRE7 | DB | submit: same `peso_id`, `submitted_at`/`submitted_by` set by backend, status stays `pendente`; edit/associate afterwards → `already-submitted` | AC-C4, AC-C5 |
| CRE8 | DB | double submit → 409 `already-submitted`; no second record | AC-C5 |
| CRE9 | DB | draft edit with stale version → 409 `stale-version`, nothing written | AC-C7 |
| CRE10 | DB | delete: no Peso delete route/primitive exists (route scan + reflection) | AC-C6 (Q-DELETE) |

#### JOB ON / CM RESOLUTION

| # | Class | Test | Proves |
|---|---|---|---|
| JRC1 | I | productions read returns every occurrence; a single occurrence is never auto-selected; selection explicit | AC-R1 |
| JRC2 | I | `controlo-create`-only caller reads productions/ficha/candidates through the P2-T05 routes (delegation, no second model) | AC-R2 |
| JRC3 | I | route 11 creates the missing CM context through `IJobOnService` Set (facts Keep); `TOOL_TYPE_MISMATCH` refused; no other Job On change | AC-R3 |
| JRC4 | I | pending case renders `Job On por associar` (non-error), measurement can save/submit while pending | AC-R4 |
| JRC5 | U/DB | ambiguity handling: N candidates → N items, explicit human selection; blank reference → 400 `REFERENCE_REQUIRED`; empty vs lookup-failed vs denied distinct | AC-R5 |
| JRC6 | I | Tool search/create stays `ferramentas`-gated; a `controlo-create`-only caller is denied it (no second Tool access path) | AC-R6 |

#### REPAIRERS

| # | Class | Test | Proves |
|---|---|---|---|
| REP1 | U/DB | add repairer with name only; `NAME_REQUIRED` for blank/whitespace name | AC-D1 |
| REP2 | DB | rename edits the name (version-guarded); the row id never changes | AC-D2 |
| REP3 | DB | no delete route exists for repairers | AC-D3 (Q-REP) |
| REP4 | S | no address/email/phone/supplier-code/tax/contact-person field or type exists anywhere in P2-T05 code/schema | AC-D1, AC-D4 |
| REP5 | DB | a repairer referenced by an assignment cannot be deleted at the database level (RESTRICT) | AC-D3 |

#### MACHINE ASSIGNMENT

| # | Class | Test | Proves |
|---|---|---|---|
| MAC1 | DB | `B1`..`C3` each can hold an assignment; `MACHINE_UNKNOWN` for `B4`/`Linha B`/`C`/`B` | AC-E1 |
| MAC2 | DB | setting `B1 → A` leaves `B2,B3,C1,C2,C3` unchanged (one row per machine; others untouched) | AC-E2 |
| MAC3 | DB | changing `B1` again changes only `B1` (B1 → A then B1 → B; others unchanged) | AC-E2 |
| MAC4 | DB | clearing `C2` removes only its row; the other five remain | AC-E2 |
| MAC5 | DB | each machine resolves `machine → assigned repairer` independently (the §11.4 resolution read) | AC-E3 |
| MAC6 | U/S | no Line B/C grouping concept: no group column, no `linha` member, no cascade anywhere | AC-E4, AC-A6 |
| MAC7 | I | assignment set/change/clear routes carry `controlo-create`; approve-only caller denied | AC-E5, AC-G2 |

#### SETTINGS

| # | Class | Test | Proves |
|---|---|---|---|
| SET1 | DB | PDF directory configure → persisted; change → version-guarded new value; `DIRECTORY_REQUIRED` blank | AC-F1 |
| SET2 | DB | check returns the typed result states; `not-configured` distinct from all others | AC-F2 (Q-PDF semantics gate) |
| SET3 | U | check vocabulary: `ok | directory-not-found | not-a-directory | access-denied | invalid-path | check-failed` never conflated with `not-configured`, with each other, or with `empty` | AC-F2 |
| SET4 | DB | email list create/update carries name + full recipient set atomically; `replace-all` leaves no partial set | AC-F3 |
| SET5 | DB | list update: recipients replaced exactly (old removed, new inserted) in one transaction | AC-F3 |
| SET6 | DB | `email_lists_name_key` collision → 409 `duplicate-name`; `ADDRESS_INVALID` / `ADDRESS_REQUIRED`; same address allowed in two lists, refused twice in one list | AC-F4 |
| SET7 | DB | template carries name/subject/body/document_type; `DOCUMENT_TYPE_UNKNOWN` for a fourth value; NULL type allowed | AC-F5 |
| SET8 | S | no hardcoded recipient address exists in application code (scan of `src/` for `@`-literals in non-test code) | AC-F6 |
| SET9 | DB | list/template delete with confirmation; missing confirmation → `DELETE_NOT_CONFIRMED`; dependency refusal via RESTRICT backstop when referenced | AC-F7 |
| SET10 | U | settings reads/writes are site-wide rows (no per-user dimension exists in the schema) | AC-F8 (Q-SITE) |
| SET11 | DB | settings writes are version-guarded; save-time race maps to `stale-version` | AC-F9 |

#### AUTH

| # | Class | Test | Proves |
|---|---|---|---|
| AUT1 | I | every P2-T05 route (Create + Definições) denies a caller without `controlo-create` (direct-route denial) | AC-G1 |
| AUT2 | I | a `controlo-approve`-only holder is denied Definições pages/routes 12–17 and every Peso route; the shared `controlo` destination never merges grants | AC-G2 |
| AUT3 | I | ADMIN is denied every P2-T05 route (no super-user) | AC-G3 |
| AUT4 | I | a Create-only holder is denied a P2-T06-proxy route (no leakage in either direction — P2-T06 routes do not exist yet, so the negative is the Approve policy being unexercisable) | AC-G4 |
| AUT5 | S | no P2-T05 route carries `controlo-approve`, `dmo.administration` or a second policy; page `[Authorize]` constants asserted equal to the canonical projection | AC-G1 |

#### BOUNDARIES

| # | Class | Test | Proves |
|---|---|---|---|
| BND1 | S/R | `CurrentBuildAvailable` stays `[]`; no route registration, no navigation entry, no destination for Definições | AC-Y1 |
| BND2 | S | no P2-T06 leakage: no approve/reject/reopen route, type, table or presentation; no approval queue; no per-CM decision | AC-Y2, AC-N2 |
| BND3 | S | no P2-T07 leakage: no Boquilhas type/movement/repairer-resolution/assignment-history table; nothing automated for future records | AC-Y3 |
| BND4 | S | no P2-T08 leakage: no document table, no PDF/email code, no availability read, no file capability, no routing rule, no placeholder parsing | AC-Y4 |
| BND5 | S | no duplicate Tool/JobOn/CM identity: no second registry, no fake ids, no reverse arrays | AC-A6, AC-A1 |
| BND6 | S | no unsupported machine registry, no `machine_id`, no line grouping | AC-E4 |
| BND7 | S | `pesos`/settings rows carry no `previous_peso_id`, no Comparação/Pegamentos/Folha/Resumo columns or tables (Q-SCOPE seam) | AC-Y5 |
| BND8 | R/S | migrations 001/002/003 byte-identical; `DmoDbContext.cs` byte-identical; frozen shared artifacts untouched | AC-Y6, AC-A7 |
| BND9 | S | no P2-T05 source change outside the contracted paths (production scan of the implementation commit) | AC-Y7 |

**Completeness:** every §30 criterion maps to at least one row above and every row maps to at
least one criterion (verified in the authoring run by the row↔AC key, mechanically in the
eventual implementation response — P2-T04 §20.12 discipline).

---

## 27. Authority questions

### 27.1 BLOCKING

| # | Question | Authority gap (exact) | Options | Smallest recommended default | Implementation impact | Class |
|---|---|---|---|---|---|---|
| **Q-PDF** | What do "local directory" and "accessible" mean for the PDF/document base-directory in the **deployed web architecture**? | The settled delta fixes configure/change/check (§7.2) but not the mechanism; the legacy authority's "main reports directory … guardados neste computador / autorização pertence a este browser" is a **browser/computer-local** directory; the current runtime is a **server-side** ASP.NET monolith and **no repository authority records where the production web process runs** (no Dockerfile/hosting contract; Supabase = DB only). Server-local, network-share and browser-local semantics differ materially and cannot be decided from authority. | (a) server-process-local absolute path checked by the server process (needs the deployment to run on/near the storage); (b) UNC/network share reachable from the server (needs credentials/account facts); (c) browser-local directory via the File System Access API (requires a new, unauthorised client-storage capability); (d) managed document store (new architecture) | **(a)** server-process-local absolute path — the only option implementable with the current server-side architecture and no new client capability; the check asserts existence + directory + read/write reachability **from the server process account** | the `check` operation (route 15) semantics, its testability (DB-class tests can only assert the typed vocabulary; the executable check environment is deployment-dependent), and P2-T08's consumption. **The schema, the configure/change surface and the `not-configured` state are unaffected and remain FIXED.** | **BLOCKING** |

### 27.2 NON-BLOCKING (each with a pinned default already reflected in this contract)

| # | Question | Authority gap | Pinned default | Impact | Class |
|---|---|---|---|---|---|
| Q-UNIQ | Exact Peso uniqueness | no current authority fixes a tuple (legacy tuple superseded) | **no tuple beyond PK**; duplicates legitimate; explicit selection everywhere | schema: no unique index | NON-BLOCKING |
| Q-CALC | Water-temperature divisor table data and processo→density mapping values | formulas fixed; the **values** exist in no repository authority | mechanism contracted; values arrive as backend calculation configuration (not a Definições area, not hardcoded); until they exist → `calculation-configuration-missing` | calculate endpoint; no schema impact | NON-BLOCKING |
| Q-UNIT | Units | authority silent; legacy shows g / cm³ / g·cm⁻³ / °C | g, cm³, g/cm³, °C | display + CHECKs | NON-BLOCKING |
| Q-PREC | Storage precision | silent | numeric(18,4); ≤ 2 dp presentation only | columns | NON-BLOCKING |
| Q-NOMINAL | Peso nominal weight / mold state / Notas / autosave | legacy visuals only; not in current authority | **not contracted** | no columns | NON-BLOCKING |
| Q-DELETE | Peso delete semantics | authority silent; statuses fixed three | **no delete path in P2-T05**; draft edit / P2-T06 reopen cover correction | no route/primitive | NON-BLOCKING |
| Q-DUP | Peso duplication (`CONTROLO.md` §9) | global authority exists; Beta handoff "Included in Beta" does not list it; operative task does not include it | **not in this contract**; a later P2-T05 slice or contract addendum | none here | NON-BLOCKING |
| Q-REP | Repairer name uniqueness; deactivation lifecycle | silent (§3.5 implementation-concern-only) | no unique name; **no status column**; no delete UI; RESTRICT-only protection | schema | NON-BLOCKING |
| Q-HIST | Machine-assignment history | delta §6 + task §17 | **current-state only**; P2-T07 snapshots `repairer_id` on movements | no history table | NON-BLOCKING |
| Q-ROUTE | Email routing grouping (Line B/C) vs named lists | master §17 (line groups, Admin location — superseded location) vs delta §9.4 (no invented rules) | P2-T05 stores named lists only; the context→list rule is P2-T08's decision, consistent with delta §9.4; repairer grouping stays forbidden | schema neutral | NON-BLOCKING |
| Q-PLACE | Template placeholder syntax | none exists (`…DELTA.md` §10.4) | none; verbatim text; P2-T08's contract authors any syntax | no parsing | NON-BLOCKING |
| Q-DOCTYPE | Template document-type closed set | "applicable document type/context where needed" | `peso/pegamentos/resumo` + NULL generic; extension = additive CHECK change | CHECK | NON-BLOCKING |
| Q-SITE | Settings site-wide vs user-specific | silent | site-wide | schema | NON-BLOCKING |
| Q-CONC | Settings concurrency/versioning | silent | version tokens identical to the accepted Peso/Job On pattern | columns | NON-BLOCKING |
| Q-SETAUDIT | Assignment-change audit trail | delta O9 open | none in P2-T05 (S13); version tokens + timestamps only | no audit table | NON-BLOCKING |
| Q-CAND | Association-candidate read access | P2-T04 §8.3 reverse read exists but is not exposed as a Job On route | **additive** read-only method on the Job On application contract (cross-stream protocol), exposed via route 4 under `controlo-create`; fallback: N ficha reads | one additive method | NON-BLOCKING |
| Q-REANCHOR | Production-bound Peso moving to another `cm_id` | silent | not offered by P2-T05 (associate is pending-only) | no operation | NON-BLOCKING |
| Q-ROWLBL | Row label (legacy CM number) | silent; pairing is positional (§8.6) | no label column; `row_position` is the pairing key; a future Comparação slice may add a label additively | schema | NON-BLOCKING |
| Q-ORDER | Email-list/member ordering | silent | no ordering column; deterministic `name`/`address` order | schema | NON-BLOCKING |
| Q-NAME | List/template name uniqueness | silent | unique names (unambiguous addressing) | unique keys | NON-BLOCKING |
| Q-ADDR | Recipient address validation strictness | silent | minimal shape (one `@`, non-blank parts, no whitespace) | validator | NON-BLOCKING |
| Q-SURF | Definições in-surface organization | delta O2 | one page, five sections | page | NON-BLOCKING |
| Q-DOCREAD | Create-side document availability read | CONTROLO_CREATE lists "Create-side history/document views"; availability facts are P2-T08 | P2-T05 reserves region R8 only; no read; P2-T08 supplies | no code | NON-BLOCKING |
| Q-PCS | `ProductionContextStrip` absence | freeze §6 defines it; no workstream implemented it | P2-T05 renders the strip page-owned (no look-alike of an implemented component); adopts the shared partial when its owner ships it | page markup | NON-BLOCKING |
| Q-PRODLABEL | Freeze Job-On display labels (reference/production/machine) on Peso | task §4 vs information-web traversal | **traversal only** (§6.2); P2-T08 freezes output bytes at generation | no columns | NON-BLOCKING |
| Q-SCOPE | Comparação / Pegamentos / Folha / Resumo / read-model rendering remain in the P2-T05 handoff but are not authored by this contract | master plan §7 lists them; the operative authoring task scopes this contract to Peso core + Definições + seams | **this contract covers §2.1 only**; every remainder requires its own authored contract (same workstream, separate PLAN ACCEPT) before implementation; the Peso carriers they need are fixed here | additive migrations later; no uncertainty blocks this schema | NON-BLOCKING |

---

## 28. Authority questions — summary

**BLOCKING: 1** (Q-PDF — exactly the task-mandated deployment question; it gates only the
directory **check** executable semantics; the setting's persistence/change surface stays FIXED).

**NON-BLOCKING: 26** (Q-UNIQ … Q-SCOPE) — each with a pinned default reflected in the schema,
interfaces and routes above; none blocks the PLAN REVIEW gate. If the Architect rejects a pinned
default, the contract returns `CORRECTION REQUIRED` for that item rather than proceeding.

---

## 29. Explicit downstream seams

| Seam | Who consumes | What P2-T05 leaves | What the consumer must do |
|---|---|---|---|
| Submitted `peso_id` handoff (`submitted_at`/`submitted_by`, status `pendente`) | P2-T06 | the same row, reviewable facts, backend attribution | approve/reject/reopen on the same `peso_id`; its pending-list query owns its own index; its routes own their policies |
| Published Peso read model (§26.3) | P2-T06 | carrier shapes + composition rules | D-owned `controlo-approve` read routes; **no forked renderer** (D's obligation, master plan §7 P2-T06) |
| Comparação (explicit `previous_peso_id`, stale rebuild, positional pairing) | P2-T05 handoff remainder / its own contract (Q-SCOPE) | rows carry `row_position` + per-row `glass_weight_g` (the "current value per row" of `CONTROLO.md` §10); **no `previous_peso_id` column exists** | author the pairing/persistence contract on top of this schema (additive migration) |
| Pegamentos / Folha / Resumo | P2-T05 handoff remainder / its own contract (Q-SCOPE) | real P2-T04 contexts (`cm_id`/`mf_id`/`bq_id`, `jobon_id`) available; no table | own contracts + migrations; register their own dependency probes |
| Repairer resolution `machine → current assignment → repairer` | P2-T07 | current-state assignment rows + read contract | snapshot `repairer_id` on its records; never rewrites history; never administers the register |
| Dependency probe seam | P2-T07, P2-T08 (their tables → Job On/context deletes) | `PesoJobOnDependencyProbe` + the additive-registration pattern | register their own probes (one additive line each) |
| PDF base directory + email lists/templates | P2-T08 | settings rows + read contracts | generation/availability; consumption of the base + `<reference>/<production-number>/`; sending via lists/templates with no hardcoded addresses; routing rule per Q-ROUTE; placeholder syntax per Q-PLACE |
| CM missing-context creation (route 11) | P2-T05 itself (reused by later surfaces) | composition through `IJobOnService` | no other module needs to re-implement it |
| Availability/navigation | P2-T10 | pages/routes exist, availability `[]` | register `controlo-create` when real; never register Definições as a destination |

No seam authorizes implementation: every consumer remains NOT AUTHORIZED until its own gate.

---

## 30. Implementation acceptance criteria

P2-T05 is acceptable only when every criterion below is satisfied and proven by §26.4.

### Peso identity and anchoring (AC-P1 … AC-P7)

| # | Criterion |
|---|---|
| AC-P1 | A Peso anchors to exactly one of `cm_id` (production) or `tool_id` (pending), DB-enforced; Tool/Job On are reachable through `cm_id` and never duplicated. |
| AC-P2 | The pending `tool_id` anchor is a valid, first-class, non-error state (`Job On por associar`), fully usable and submittable. |
| AC-P3 | `peso_id` is created by the backend and is one specific Peso/Controlo record — never a production, Tool, Job On, CM, revision or approval-copy identity. |
| AC-P4 | No client supplies or guesses `peso_id` or any canonical identity. |
| AC-P5 | Association to `cm_id` is always explicit, human-confirmed, candidate-resolving-to-the-same-Tool only; success clears the direct Tool anchor; no inference rule exists. |
| AC-P6 | Association candidates are real context rows, never synthesized. |
| AC-P7 | No Peso uniqueness tuple exists beyond the PK (Q-UNIQ default). |

### Measurement model (AC-M1 … AC-M9)

| # | Criterion |
|---|---|
| AC-M1 | Water temperature obeys 5–35 °C, backend-validated. |
| AC-M2 | Per-row water weight (g) is registered exactly and validated positive. |
| AC-M3 | Per-row capacity (cm³) is computed by the backend formula and **registered** (persisted). |
| AC-M4 | Per-row glass weight is computed by the backend formula and persisted; the density used is frozen on the Peso. |
| AC-M5 | Rows are variable, at least one valid row, dense positional ordering, stable ids. |
| AC-M6 | Measurement facts persist and re-read identically. |
| AC-M7 | Presentation normalization (≤ 2 dp) never reduces calculation precision. |
| AC-M8 | Individual results stay first-class; averages/deviations are derived and never hide them. |
| AC-M9 | Missing calculation configuration yields a typed refusal, never an invented value. |

### Historical snapshot (AC-H1 … AC-H3)

| # | Criterion |
|---|---|
| AC-H1 | Later Tool changes never rewrite the Peso's context/Snapshot or stored results; later Job On changes never alter the Peso's frozen facts (labels flow through documented traversal only — §6.2). |
| AC-H2 | Stored capacity/glass weight are never recomputed from current Tool/Job On state; no refresh path exists. |
| AC-H3 | Exactly the contracted facts are frozen (inputs, density used, per-row results, context triple via `cm_id`); no snapshot engine or extra identity is added. |

### Create/edit/delete (AC-C1 … AC-C9)

| # | Criterion |
|---|---|
| AC-C1 | Peso create is atomic (record + full row set) and returns the real `peso_id`; rollback leaves nothing. |
| AC-C2 | Every mutation validates before any write with the closed code set. |
| AC-C3 | Draft update replaces the row set atomically and recomputes stored results. |
| AC-C4 | Submit transitions the **same** `peso_id` into the reviewable handoff with backend state/attribution; Create never approves. |
| AC-C5 | Post-submit Create-side mutations are refused (`already-submitted`); double submit refused; no second record. |
| AC-C6 | No Peso delete path exists in P2-T05; no lifecycle state beyond the three-value vocabulary. |
| AC-C7 | Optimistic concurrency: stale version refuses every guarded write with 409 `stale-version` (compare + EF-token race), nothing written. |
| AC-C8 | One version increment per mutation; reads never bump. |
| AC-C9 | A save-time race never yields 500 and never overwrites silently. |

### Job On / CM resolution (AC-R1 … AC-R6)

| # | Criterion |
|---|---|
| AC-R1 | Production selection is explicit; no auto-selection, no latest-only; one real `jobon_id`. |
| AC-R2 | The P2-T05 read model is one query chain over the accepted P2-T04 application contracts; no second production lookup model. |
| AC-R3 | A missing CM context is created through the Job On application contract (Set, CM-slot only), with type validation; nothing else changes. |
| AC-R4 | The pending case blocks nothing and is presented truthfully. |
| AC-R5 | Empty / lookup-failed / permission-denied / validation are four distinct outcomes. |
| AC-R6 | Tool search/create access remains `ferramentas`-gated; no second Tool access path. |

### Definições (AC-D1 … AC-F9)

| # | Criterion |
|---|---|
| AC-D1 | Repairer register: name is the only required data; add / edit name / select work. |
| AC-D2 | Rename edits the same `repairer_id`, version-guarded. |
| AC-D3 | No repairer delete/deactivation path; referenced repairers are DB-protected. |
| AC-D4 | No invented repairer field exists (address/email/phone/supplier/tax/contact). |
| AC-E1 | `B1 B2 B3 C1 C2 C3` each hold one independent assignment; unknown codes refused. |
| AC-E2 | Changing/clearing one machine leaves the other five unchanged. |
| AC-E3 | The resolution read `machine → repairer` returns exactly the machine's current assignment; absent = none. |
| AC-E4 | No grouping rule, no line concept, no machine registry, no cascade exists anywhere. |
| AC-E5 | Every assignment operation is `controlo-create`-gated. |
| AC-F1 | The PDF base directory can be configured, changed (version-guarded) and read; `not-configured` is explicit. |
| AC-F2 | The check returns the typed vocabulary only, never conflated states. |
| AC-F3 | Email lists persist as named lists with an atomic replace-all recipient set. |
| AC-F4 | List validation: unique names, minimal addresses, one-per-list duplicates refused, cross-list repetition allowed. |
| AC-F5 | Templates carry name/subject/body and the three-value document type (or none), text verbatim. |
| AC-F6 | No recipient address is hardcoded in application code. |
| AC-F7 | Confirmed deletes only; dependency refusals typed. |
| AC-F8 | Settings are site-wide configuration, not user-specific. |
| AC-F9 | Settings writes are concurrency-safe per the accepted pattern. |

### Authorization and boundaries (AC-G1 … AC-G4, AC-Y1 … AC-Y7, AC-N1)

| # | Criterion |
|---|---|
| AC-G1 | Every P2-T05 route (Create + Definições) carries exactly the `controlo-create` module policy; direct-route denial is server-side. |
| AC-G2 | A `controlo-approve`-only holder is denied every P2-T05 surface; no grant merging on the shared destination. |
| AC-G3 | ADMIN gains no operational access. |
| AC-G4 | Create grants nothing beyond its own module (no Approve, no settings elsewhere). |
| AC-Y1 | `CurrentBuildAvailable` remains `[]`; no route/destination/navigation registration; Definições is not a destination. |
| AC-Y2 | No P2-T06 leak (approve/reject/reopen/queue/decision/presentation) exists. |
| AC-Y3 | No P2-T07 leak (Boquilhas/repairer resolution code, assignment history, sidebar) exists. |
| AC-Y4 | No P2-T08 leak (PDF/file/email-send/availability/routing/placeholder code) exists. |
| AC-Y5 | No Comparação/Pegamentos/Folha/Resumo table, column or route exists (Q-SCOPE seam only). |
| AC-Y6 | Migrations 001/002/003 and `DmoDbContext.cs` are byte-identical; shared/frozen artifacts untouched. |
| AC-Y7 | Exactly one new migration owns exactly the eight contracted tables; no ninth table or unrequested column. |
| AC-N1 | The fixed desktop policy holds for every P2-T05 surface (no breakpoint variant, no relocation, no hiding). |

**Count: 64 criteria** (AC-P1…AC-P7, AC-M1…AC-M9, AC-H1…AC-H3, AC-C1…AC-C9, AC-R1…AC-R6,
AC-D1…AC-D4, AC-E1…AC-E5, AC-F1…AC-F9, AC-G1…AC-G4, AC-Y1…AC-Y7, AC-N1) ↔ the §26.4 rows.

---

## Appendix A — Protected boundaries

### A.1 Must not be touched by P2-T05

```text
src/DMO.Application/Access/**                           (catalog, registry, resolver, availability)
src/DMO.Web/Authorization/**                            (module/administration gates)
src/DMO.Web/Auth/*, src/DMO.Web/Startup/*, src/DMO.Application/Accounts/*, Session/*
src/DMO.Infrastructure/Persistence/DmoDbContext.cs
src/DMO.Infrastructure/Migrations/20260922001736_*, 20260922001757_*,
   20260922232349_ToolJobOnDomainCore.*                 (and their Designers)
src/DMO.Infrastructure/Persistence/{UserRepository,TemplateRepository,TemplateModuleRepository,
   AdminAccountRepository,JobOnRepository,ToolRepository,JobOnLineageDependencyProbe}.cs
src/DMO.Infrastructure/Persistence/Entities/{Template,User,AdminAccount,TemplateModule,
   JobOn,Tool,ToolMachine,CmContext,MfContext,BqContext}Entity.cs
src/DMO.Infrastructure/Persistence/EntityConfigurations/{Template,User,AdminAccount,TemplateModule,
   JobOn,Tool,ToolMachine,CmContext,MfContext,BqContext}*Configuration.cs
src/DMO.Web/Frontend/Shell/**, src/DMO.Web/Frontend/Shared/**
src/DMO.Web/Navigation/DestinationRouteRegistrations.cs
src/DMO.Web/Pages/Shared/**, src/DMO.Web/Pages/{Index,Login,AccessDenied}.*,
   src/DMO.Web/Pages/Administration/**
src/DMO.Web/Endpoints/{AuthEndpoints,TechnicalEndpoints,TemplateAdministrationEndpoints,
   UserAdministrationEndpoints,JobOnEndpoints,FerramentasEndpoints}.cs
src/DMO.Web/wwwroot/css/{dmo-tokens,dmo-shell,dmo-user-shell,dmo-components,dmo-admin-users,
   dmo-admin-templates,dmo-jobon}.css
src/DMO.Web/wwwroot/js/{dmo-focus,dmo-dense-table,dmo-tool-picker,dmo-measurement-rows,dmo-jobon}.js
docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md
plans/contracts/P2-T02_*.md, P2-T03_*.md, P2-T04_*.md
tests/**                                              (existing tests keep passing, never weakened)
```

The only accepted non-new files P2-T05 changes: `src/DMO.Web/Program.cs` (additive service
registrations + `MapControloCreateEndpoints()`/`MapControloDefinicoesEndpoints()`),
`PersistenceServiceCollectionExtensions.cs` (additive repository + probe registrations),
`DmoDbContextModelSnapshot.cs` (EF-generated snapshot extension), and the governance documents of
Appendix D.

### A.2 Accepted input, consume only

`ModuleCatalog`, `ModuleRegistry`, `AccessResolver`/`AccessOutcome`/`ModuleResolve`,
`ModuleAccessService`/`IModuleAccessService`, the module/administration gates,
authentication/session/current account (`ICurrentAccountContext` is the actor source for
`created_by_user_id`/`submitted_by_user_id`), migrations 001–003, the Template model, USER
administration, root routing/landing, navigation projection, the destination-route seam, the
shared shell, `ModuleRegistrations`, the A1 freeze, every P2-T01/T02/T03 shared artifact, and the
P2-T04 application/repository contracts (consumed, with the one additive read proposal of §20.4).

### A.3 Downstream boundaries (must be preserved)

No P2-T05 artifact names, imports, renders, fixtures or tests: an approval queue/decision,
`previous_peso_id`, Comparação tables/rendering, Pegamentos/Folha/Resumo records or rendering,
Boquilhas aggregates/movements/repairer-resolution code, assignment-history rows, document/PDF/
email-send/availability code, a machine registry, a second Tool/Job On registry, fake
`cm_id`/`jobon_id`/`tool_id`/`peso_id`, `production_id`/`job_on_revision_id`, reverse arrays, or a
Definições destination/navigation entry.

---

## Appendix B — Implementation file ownership / expected paths

```text
src/DMO.Domain/Controlo/                 PesoId.cs, PesoMeasurementRowId.cs, PesoStatus.cs,
                                         Peso.cs, PesoMeasurementRow.cs,
                                         RepairerId.cs, EmailListId.cs, EmailTemplateId.cs, …
src/DMO.Application/Repositories/        IPesoRepository.cs, IRepairerRepository.cs,
                                         IMachineRepairerAssignmentRepository.cs,
                                         IPdfDirectorySettingsRepository.cs, IEmailListRepository.cs,
                                         IEmailTemplateRepository.cs
src/DMO.Application/ControloCreate/      ControloCreateModels.cs, ControloCreateValidator.cs,
                                         IControloCreateService.cs, ControloCreateService.cs,
                                         IControloDefinicoesService.cs, ControloDefinicoesService.cs,
                                         ControloDefinicoesValidator.cs,
                                         PesoSheetReadModel.cs
src/DMO.Infrastructure/Persistence/      Entities/{Peso, PesoMeasurementRow, Repairer,
                                         MachineRepairerAssignment, PdfDirectorySettings,
                                         EmailList, EmailListRecipient, EmailTemplate}Entity.cs,
                                         EntityConfigurations/… (8 configurations),
                                         {Peso,Repairer,MachineRepairerAssignment,
                                         PdfDirectorySettings,EmailList,EmailTemplate}Repository.cs,
                                         PesoJobOnDependencyProbe.cs,
                                         Migrations/<timestamp>_ControloCreateDomain.cs (+.Designer),
                                         Migrations/DmoDbContextModelSnapshot.cs (EF extension),
                                         PersistenceServiceCollectionExtensions.cs (additive)
src/DMO.Web/Endpoints/                   ControloCreateEndpoints.cs, ControloDefinicoesEndpoints.cs
src/DMO.Web/Pages/Controlo/              Create.cshtml(.cs), Definicoes.cshtml(.cs)
src/DMO.Web/Pages/Controlo/              ControloPolicyNames.cs
src/DMO.Web/wwwroot/css/dmo-controlo.css (new file)
src/DMO.Web/wwwroot/js/dmo-controlo.js   (only if genuinely required; reuses window.dmoFocus)
src/DMO.Web/Program.cs                   (additive registrations + Map…Endpoints only)
tests/DMO.UnitTests/ControloCreate/**    tests/DMO.IntegrationTests/ControloCreate/**,
tests/DMO.IntegrationTests/Persistence/  (Peso/Settings repository integration + migration tests),
tests/DMO.IntegrationTests/ControloCreate/P2T05RegressionTests.cs, P2T05ProductionScan.cs
```

---

## Appendix C — Fixed desktop obligations

Restated for implementers: §24 rules 1–7 are binding; canonical validation viewport 1366 × 768;
compact density; region-stable composition; no `@media`/`@container`/`@supports` structural rule
in `dmo-controlo.css`; local keyboard-reachable overflow for wide tables; no mobile/tablet
variants.

---

## Appendix D — Governance record

### D.1 Status recorded by this authoring task

| Item | Status |
|---|---|
| P2-T05 | **CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW** |
| B2 (`plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §4) | **AWAITING PLAN ACCEPT** (not resolved; resolution requires Architect `PLAN ACCEPT`) |
| Implementation | **NOT STARTED — NOT AUTHORIZED** |
| Application code modified | NO |
| Migration created | NO |
| Supabase modified | NO |
| `ModuleRegistrations.CurrentBuildAvailable` | `[]` (unchanged) |
| `DestinationRouteRegistrations` / route registry | unchanged (still empty) |
| P2-T04 | CLOSED (Architect re-review ACCEPT `b6f7a01c99fc8c517cca5cab335af5d47fb9e2f9`) — unchanged by this task |
| P2-T06 / P2-T07 / P2-T08 / P2-T10 | NOT AUTHORIZED — unchanged |

### D.2 Governance files updated by this authoring task

| File | Update |
|---|---|
| `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` | this contract (new) |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §4 B2 row + blocker-status table; §7 P2-T05 contract-status record; Appendix A B2 row |
| `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` | §5 contract-authored record |
| `dev/responses/P2_T05_CONTRACT_AUTHORING_RESPONSE.md` | the authoring response (new) |

`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` is **not** edited (settled authority
record; its §14.1 "B2 still open" statement remains correct). `reports/BETA_MASTER_RECONCILIATION.md`
is **not** edited (audited current-state report of a specific commit).

### D.3 Baseline verification recorded by the authoring run

```text
DMO-MODULAR baseline            : c1b457f312e63bd32a718888d06793e249eec051 (origin/main, clean)
P2-T04 FINAL ARCHITECT RE-REVIEW: b6f7a01c99fc8c517cca5cab335af5d47fb9e2f9 (dmo-work main, ACCEPT)
dmo-beta-master main            : 78da49248f6cf7a8cbe4ddd946f3c38abbaf322f
dmo-master main                 : 610c8b4d3084864a750f6aa00507b5273ef09567b
build/tests                     : not modified (authoring task runs no build; baseline evidence
                                  from P2-T04 response: build 0 errors; unit 509/0/0;
                                  integration 385/0/2 env-gated)
working tree (application code) : clean before and after authoring
```

---

## Appendix E — PLAN REVIEW gate

Implementation may begin only when the Architect has:

1. reviewed **this** file at a recorded SHA;
2. dispositioned the 27 authority questions of §28 — in particular the single **BLOCKING** item
   Q-PDF (deployment semantics of the directory-accessible check) and the default for Q-CALC;
3. confirmed the physical schema of §16/§17, the anchoring model of §3/§4, the frozen-facts model
   of §6, the transaction boundaries of §18 and the route/policy matrix of §21;
4. returned an explicit `PLAN ACCEPT` (or `CORRECTION REQUIRED` / `REJECT`) per
   `dmo-beta-master/WORKFLOW.md` step 6.

Until then:

```text
P2-T05 CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW
B2 AWAITING PLAN ACCEPT
NOT IMPLEMENTED
```