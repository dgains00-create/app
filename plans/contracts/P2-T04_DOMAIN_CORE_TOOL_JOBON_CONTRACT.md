# P2-T04 — Canonical Tool Identity + Job On Light + Ferramentas Light — BACKEND / INTERFACE CONTRACT

**Contract class:** authored backend/interface contract for authority blocker **B1**
(`plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §4).
**Status of this document:** `P2-T04 CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW`.
**B1 status:** `AWAITING PLAN ACCEPT`.
**Implementation status:** **NOT IMPLEMENTED. NOT AUTHORIZED.** This document creates no
application code, no entity, no `DbSet`, no repository, no service, no endpoint, no Razor page, no
migration, no test, no route, no availability registration and no Supabase change.

Per `diogo-o/dmo-beta-master` `WORKFLOW.md` step 6, implementation may begin only after the
Architect reviews **this** file and returns `PLAN ACCEPT`.

Baseline this contract was authored against:

```text
repository:                https://github.com/diogo-o/DMO-MODULAR.git
branch:                    main
HEAD at authoring start:   0f370e61f403987cc930b16ebd55afe8dd57784b
origin/main at start:      0f370e61f403987cc930b16ebd55afe8dd57784b
working tree at start:     CLEAN
application code changed:  NO
migration added:           NO
Supabase changed:          NO
```

---

## Required-section index

| # | Section | # | Section |
|---|---|---|---|
| 1 | Authority | 12 | Repository / application interfaces |
| 2 | Domain identities | 13 | Endpoint / route / policy matrix |
| 3 | Physical schema | 14 | Failure / result vocabulary |
| 4 | Keys and constraints | 15 | Concurrency |
| 5 | Canonical Tool rules | 16 | Migration contract |
| 6 | Job On occurrence rules | 17 | Explicit non-scope |
| 7 | Context / snapshot rules | 18 | P2-T03 composition |
| 8 | Query contracts | 19 | Supabase / PostgreSQL compatibility |
| 9 | Tool orchestration contract | 20 | Test-to-acceptance matrix |
| 10 | Duplication transaction | 21 | Authority questions |
| 11 | Create / edit / delete transactions | 22 | Implementation acceptance criteria |
| 23 | OWNER CLARIFICATION — Job On context snapshot invariant (supersedes the affected duplication rules) |  |  |

Appendices: **A** protected boundaries · **B** file ownership / expected paths · **C** fixed desktop
obligations · **D** governance record · **E** PLAN REVIEW gate.

---

## 1. Authority

### 1.1 Authority order applied

Per `WORKFLOW.md` ("Authority rule") and master plan §0.1:

```text
dmo-master global invariants
  -> dmo-beta-master Beta authority
  -> accepted task plan / authored contract
  -> DMO-MODULAR implementation
```

1. `reports/BETA_MASTER_RECONCILIATION.md` — current-state/delta authority (this repo).
2. `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` — repository-recorded **settled
   functional authority** for the settings/repairer/email/PDF area, position 4.1 (this repo).
3. `diogo-o/dmo-beta-master` @ `main` — Beta functional/scope/workflow/acceptance authority.
4. `diogo-o/dmo-master` @ `dmo-modular` — global architecture, canonical identities, access model,
   module vocabulary.
5. Current `diogo-o/DMO-MODULAR` @ `main` — implementation state and integration seams.
6. `diogo-o/workbench` / `diogo-o/dmo-work` — historical evidence only.

Existing code is **implementation state, not product authority**. Tests are evidence, not
authority.

### 1.2 Authority read for this contract

**DMO-MODULAR (this repository)**

| Artefact | Read for |
|---|---|
| `plans/beta-workstreams/P2-T04-DOMAIN-CORE-TOOL-JOBON.md` | workstream scope, §5 B1 requirements, §8 access, §9 persistence, §10 tests, non-scope |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §0, §2, §4, §5, §7 (P2-T04), §8, §9, §10, §11, §12, §13, §14 | B1 register, dependency order, shared-data map, route/access plan, test strategy, protected register |
| `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §4, §5, §6, §11, §14, §15, §16 | machine autonomy, no line grouping, repairers excluded, sidebar removal, B1 "unchanged", open points O1/O4 |
| `docs/CREATION_AND_ASSOCIATION_LOGIC.md` | real-then-enriched identities, minimal Job On identity, **Job On uniqueness rule**, context creation/reuse, MUST-NOT list |
| `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` §1A, §2, §3, §4, §5, §7, §8, §15 | fixed desktop policy, opaque carrier prohibition, ToolPicker/ToolSummaryRow boundaries, access seam, `Controlo_Create → Definições` is not a destination |
| `plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md` | accepted primitives consumed by §18; Appendix A.3 P2-T04 boundary |
| `docs/ARCHITECTURE.md` | project/namespace boundaries, forbidden abstractions, dependency direction |
| `src/DMO.Application/Access/*`, `src/DMO.Web/Authorization/*`, `src/DMO.Infrastructure/Persistence/*`, `src/DMO.Infrastructure/Migrations/20260922001736_*`, `…1757_*` | accepted implementation conventions contracted in §3, §12, §13, §15, §16, §19 |

**dmo-beta-master (Beta authority)**

| Artefact | Read for |
|---|---|
| `modules/JOB_ON_LIGHT.md` (full) | included/outside scope, simplified Beta fields, canonical identity, Tool selection, duplicate workflow, access, acceptance criteria |
| `modules/FERRAMENTAS_LIGHT.md` (full) | contextual-only Tool surface, Tool-owned facts, search/select/create contract, ownership, access, acceptance criteria |
| `architecture/CROSS_MODULE_FLOWS.md` | Tool → Job On → contexts; shared Tool flow; anti-inference rules |
| `architecture/BACKEND_FRONTEND_MODEL.md` | canonical identity chain, backend/frontend responsibility split, selection vs inference, authorization chain |
| `architecture/RECORD_LIFECYCLES.md` §1–§3, §11, §12 | no Job On lifecycle state machine, §2 protection rules, §3 context freeze, warnings stay warnings |
| `contracts/IDENTITIES_AND_RELATIONSHIPS.md` | canonical identities, no reverse-ID arrays, no fake identities, human-selection rule |
| `implementation/BETA_INTEGRATION_SEAMS.md` | Workstream B ownership, B→C / B→E seams, access/action seam, navigation/route-registration seam, blocker rule |
| `ACCEPTANCE_MATRIX.md` §2, §4, §9, §10, §11, §12 | Beta invariants, Job On Light gate, access/navigation gate, persistence/schema gate, test-evidence gate |
| `WORKFLOW.md` | task chain, conflict protocol, cross-stream change protocol, git discipline, testing evidence, anti-invention rules |

**dmo-master (global authority)**

| Artefact | Read for |
|---|---|
| `global/ACCESS_MODEL.md` §1, §2, §8, §11, §13, §14 | 13 modules, Job On View/Create separation, Ferramentas access, fail-closed, invariants |
| `BETA_VERSION.md` §2, §2.1, §3, §4, §4.1, §5, §8, §11 | minimum Beta Tool fields, canonical Tool identity in Beta, simplified Job On identity, duplication, history page, guardrails |
| `modules/JOB_ON.md` §3, §4, §5, §6, §7, §8, §9, §9A, §11, §12, §15 | context freeze, capture boundary, duplicate contract, historical stability, edit/delete protection, invariants |
| `modules/FERRAMENTAS.md` §2.2, §3, §3.1, §5.1, §5.5, §5.6, §6, §7, §11 | Tool is canonical identity owner, lot is identity, `processo` is Tool-owned, `tool_machines`, frozen context minimum, no Tool deletion authority |

Where this contract and an existing **repository** authority file disagree, this contract does not
silently win: the disagreement is recorded in §21 and, for this contract's own area, the settled
delta/meta-plan text governs.

### 1.3 Authority boundary for this workstream

P2-T04 fixes **only**: canonical Tool identity and its minimum Beta facts; the Job On production
occurrence; the CM/MF/BQ production contexts and their frozen values; the reference → productions
query; the shared Tool search/select/create orchestration return contract; Job On duplication; the
Job On edit warning and delete dependency rules; the contextual Ferramentas Light surfaces; the
routes/policies carrying them; the physical schema and migration contract for those facts.

It fixes **no** Controlo/Peso/Pegamentos/Folha/Resumo behaviour (P2-T05/P2-T06), **no** Boquilhas
behaviour or repairer resolution (P2-T07), **no** document/PDF generation, naming, availability or
sending (P2-T08), **no** secondary navigation (P2-T09), **no** module availability or top-level
destination registration (P2-T10), and **no** module catalogue change.

### 1.4 Accepted input register (consume only — never modified)

| Accepted artefact | Consumed as |
|---|---|
| `ModuleCatalog` (13 identities incl. `job-on-view`, `job-on-create`, `ferramentas`, `ferramentas-approve`) | policy identity source; **not modified** |
| `ModuleAuthorizationPolicies.PolicyName(ModuleId)` / `AddModuleAuthorization()` | the only authorization projection; **not modified** |
| `ModuleRegistry`, `AccessResolver`, `ModuleAccessService`, `IModuleAccessService` | effective-access resolution; **not modified** |
| `ModuleRegistrations.CurrentBuildAvailable` | stays `[]`; **not modified** |
| `DestinationRoutes.IDestinationRouteRegistry` / `EmptyDestinationRouteRegistry`, `Navigation/DestinationRouteRegistrations` | route seam owned by P2-T10; **not modified** |
| `DmoDbContext` + `ApplyConfigurationsFromAssembly` + `Database__ConnectionString` + `--migrate` | persistence host contract; `DmoDbContext.cs` is **not modified** (see §16.5) |
| migrations `20260922001736_AccountAndTemplateFoundation`, `20260922001757_TemplateModuleComposition` | foundation schema; **never edited** |
| `ConcurrencyConflictException` / `ConcurrencyConflictExceptionMapping` | optimistic-concurrency vocabulary; reused, not re-implemented |
| `CommonState`, `CommonStateRegionPresentation`, `SharedActionPresentation`, `RecordStatusPresentation`, `DenseTablePresentation`, `AuditTrailPresentation` (P2-T01/P2-T02) | shared presentation primitives; **not modified** |
| `ToolPickerPresentation`, `ToolPickerCandidatePresentation`, `ToolPickerFactPresentation`, `ToolPickerEvent*`, `ToolPickerInteraction`, `ToolSummaryRowPresentation`, `ToolSummaryFactPresentation`, `ToolSummaryRowEvent*`, `DecisionBarPresentation`, `DecisionBarAction*` (P2-T03) | shared presentation primitives consumed by §18; **not modified** |

---

## 2. Domain identities

### 2.1 Canonical identities of this contract

```text
tool_id
= one concrete operational Tool (canonical Ferramentas identity)

jobon_id
= one concrete production occurrence

cm_id / mf_id / bq_id
= the production-specific Tool context of that Job On, of type CM / MF / BQ
```

`cm_id`, `mf_id` and `bq_id` are **not** Tool identities. Each context row retains a direct
relation to the canonical `tool_id`.

**Forbidden in this workstream (and by authority):** `production_id`, `job_on_revision_id`,
`job_on_status`, any `revision_id`, module-specific/private Tool IDs, fake `cm_id`/`mf_id`/`bq_id`,
fake `jobon_id`, client-generated canonical identities, and every reverse-ID array
(`tool.jobons[]`, `jobon.pesos[]`, `tool.cm_ids[]`, …).

### 2.2 Identity semantics

| Identity | Meaning | Owner | Never means |
|---|---|---|---|
| `tool_id` | one concrete Tool: one type, one reference, one lot | Ferramentas (Tool master facts) | a lot layer, a per-piece unit, a display-key surrogate, a machine |
| `jobon_id` | one production occurrence, identified by reference + production number | Job On | a snapshot/revision, a lifecycle state, a whole-record freeze |
| `cm_id` | the CM context of that Job On occurrence (at most one) | Job On | a Tool, a CM Tool identity, a measurement record |
| `mf_id` | the MF context of that Job On occurrence (at most one) | Job On | a Tool, an MF Tool identity |
| `bq_id` | the BQ context of that Job On occurrence (at most one) | Job On | a Boquilhas aggregate, a BQ Tool identity |

### 2.3 Closed value sets (exact)

```text
ToolType   = CM | MF | BQ
Processo   = NNPB | PS          (Tool-owned; nullable "where applicable to that Tool")
MachineCode= B1 | B2 | B3 | C1 | C2 | C3
```

Machine semantics (settled — `…DELTA.md` §4):

- `B1`, `B2`, `B3`, `C1`, `C2`, `C3` are **independent operational machines**;
- there is **no** "Linha B"/"Linha C" concept, no line grouping, no inheritance, no cascade and no
  shared B/C assignment;
- the machine is consumed as **known production context**, never duplicated into a second machine
  authority;
- **no `machine_id` scheme and no machine registry** is introduced by this contract; the machine is
  a code value, not a new canonical identity (`…DELTA.md` §4.4, master plan §8 rule 7).

### 2.4 Domain types added to `src/DMO.Domain` (first real domain types)

| Type | Shape | Notes |
|---|---|---|
| `ToolId` | `readonly record struct ToolId(Guid Value)` | `From(Guid)`, `New()`; not implicit |
| `JobOnId` | `readonly record struct JobOnId(Guid Value)` | same |
| `ToolContextType` | `enum { Cm, Mf, Bq }` | one context per type per Job On |
| `ToolType` | `enum { Cm, Mf, Bq }` | Tool-owned identity fact |
| `Processo` | `enum { Nnpb, Ps }` | Tool-owned fact |
| `MachineCode` | `readonly record struct MachineCode(string Value)` | `From(string)` validates against the settled set; `All`; `IsKnown`; **no grouping member** |
| `ToolCompatibility` | `readonly record struct ToolCompatibility(IReadOnlyList<MachineCode>)` | create-time compatibility set, non-empty |
| `ToolContextSnapshot` | `sealed record (ToolType Type, string Reference, string Lot)` | the frozen context values |
| `Tool` | `sealed record (ToolId ToolId, ToolType Type, string Reference, string Lot, Processo? Processo, int? Quantity, IReadOnlyList<MachineCode> CompatibleMachines)` | no version (see §15.5) |
| `ToolContext` | `sealed record (ToolContextType ContextType, Guid ContextId, JobOnId JobOnId, ToolId ToolId, ToolContextSnapshot Frozen)` | ids per §3 |
| `JobOn` | `sealed record (JobOnId JobOnId, string Reference, string ProductionNumber, MachineCode Machine, DateOnly? ProductionDate, Guid? CopiedFromJobOnId, int Version, IReadOnlyList<ToolContext> Contexts)` | no lifecycle status field exists |

`Quantity` is `int?` because authority fixes the canonical Tool total "where canonical"; **`null`
means not established/not applicable and is never rendered as `0`** (P2-T03 `ToolSummaryRow`
"missing optional ≠ zero" rule).

**No** `DMO.Domain` type carries a Job On lifecycle state, a revision identity, a machine registry
entry, a repairer, a machine→repairer assignment, a measurement, a document state or an
availability state.

---

## 3. Physical schema

Engine: **PostgreSQL** (Supabase TEST runtime target). Conventions are exactly the accepted
foundation conventions of migrations `001`/`002` (§1.4):

- explicit snake_case table and column names (there is **no** snake_case naming-strategy package);
- PK `Guid` → `uuid`, generated in the application, with DB default `gen_random_uuid()`;
- timestamps `DateTimeOffset` → `timestamp with time zone`, DB default `now()`;
- `text` for human codes; PostgreSQL `CHECK` constraints for closed value sets and non-blank text
  (`btrim(x) <> ''`, mirroring the `users`/`admin_accounts` convention);
- **all foreign keys `ON DELETE RESTRICT`** (`DeleteBehavior.Restrict`): this schema never
  cascade-deletes operational history;
- separate `EntityTypeConfiguration` per entity, discovered by
  `ApplyConfigurationsFromAssembly(typeof(DmoDbContext).Assembly)`.

**Six tables are created. No seventh table is created**, in particular no machine registry, no
repairer, no audit table, no dependency table, no document table, no lifecycle/status table and no
reverse-array column.

### 3.1 `tools` — canonical Tool

| Column | Type | Null | Contract |
|---|---|---|---|
| `tool_id` | `uuid` | NO | PK; default `gen_random_uuid()`; application-allocated |
| `tool_type` | `text` | NO | one of `CM`,`MF`,`BQ` |
| `reference` | `text` | NO | trimmed non-blank Tool reference (identity fact) |
| `lot` | `text` | NO | trimmed non-blank lot (identity fact: different lot = different Tool) |
| `processo` | `text` | YES | `NNPB` or `PS` when applicable; `NULL` = not applicable/not established |
| `quantity` | `integer` | YES | canonical Tool total where canonical; `NULL` = not established; `0` is a real value |
| `created_at` | `timestamp with time zone` | NO | default `now()` |
| `updated_at` | `timestamp with time zone` | NO | default `now()` |

Deliberately **absent**: `tool_machine` (a Tool is compatible with **one or more** machines →
child table §3.2), `technical_condition`, `operational_note`, `utilisation`, `drawing`,
`revision`, `baffle_id`, change-request columns, `tool_reference_id`/`tool_reference_code`
classification tables, `version`, any reverse-ID array.

Justification for each absence:

- `technical_condition`, `operational_note`, utilisation and the change-request lifecycle are
  explicitly **outside Beta scope** (`FERRAMENTAS_LIGHT.md` "Explicitly outside Beta"; P2-T04
  handoff §6) and no P2-T04 acceptance criterion requires them;
- classification (`tool_references`) is global-implementation state, not a Beta requirement, and
  creating it now would be a table for symmetry — forbidden by the Beta schema gate
  (`ACCEPTANCE_MATRIX.md` §10);
- no `version` column because nothing updates a Tool in P2-T04 (§15.5). Precedent:
  `template_modules` deliberately carries no version because its parent row's version protects the
  same transaction (`TemplateModuleEntity` remark).

### 3.2 `tool_machines` — machine/line compatibility (Tool-owned, one row per compatible machine)

| Column | Type | Null | Contract |
|---|---|---|---|
| `tool_machine_id` | `uuid` | NO | PK; default `gen_random_uuid()` |
| `tool_id` | `uuid` | NO | FK → `tools(tool_id)` `ON DELETE RESTRICT` |
| `machine` | `text` | NO | one of `B1`,`B2`,`B3`,`C1`,`C2`,`C3` |

- one row per compatible machine; **one or more rows are required for every created Tool**
  (`BETA_VERSION.md` §2.1 "one or more machines/lines where the Tool works"). "One or more" is
  enforced by the create validator (§12.3) and proven by test; it is **not** expressed as a
  DB trigger because authority fixes no trigger and the accepted foundation has none.
- deliberately **absent**: a `machines` registry table, `machine_id`, `line_id`, `line`/`grupo`
  grouping column, `is_primary` flag, quantity-per-machine column (`FERRAMENTAS.md` §5.1: "Machine
  quantity is not a Tool fact in this contract").
- name rationale: the global target already names a per-instance machine association table
  (`FERRAMENTAS.md` §3 `tool_machines`). This contract reuses that name for the Beta minimum shape
  and **does not** invent a machine identity; the full Ferramentas/Armazém workstream may enrich it
  under its own authority (§21 Q4).

### 3.3 `job_ons` — one production occurrence

| Column | Type | Null | Contract |
|---|---|---|---|
| `jobon_id` | `uuid` | NO | PK; default `gen_random_uuid()`; application-allocated |
| `reference` | `text` | NO | production reference (Job On general data) |
| `production_number` | `text` | NO | production number as entered (must reproduce the P2-T08 directory token) |
| `machine` | `text` | NO | one of `B1`,`B2`,`B3`,`C1`,`C2`,`C3` |
| `production_date` | `date` | YES | the production date used as the §6.6 threshold; planning data, **never proof of production** |
| `copied_from_jobon_id` | `uuid` | YES | explicit duplication-source lineage (self-FK → `job_ons`); structural, never a revision |
| `version` | `integer` | NO | default `1`; optimistic-concurrency token (§15) |
| `created_at` | `timestamp with time zone` | NO | default `now()` |
| `updated_at` | `timestamp with time zone` | NO | default `now()` |

Deliberately **absent — each is a settled authority decision, not an omission**:

| Not present | Authority |
|---|---|
| `processo` | `FERRAMENTAS.md` §3/§647 ("reached through `tool_id` … never stored as an independent Job On fact"); `JOB_ON.md` §6/§15; `JOB_ON_LIGHT.md` ("Processo as a displayed/consumed Tool fact where available, not a second Job On authority"); master plan §8 rule 3 |
| any Job On status/state column (`rascunho`, `planeado`, `em fabrico`, `fechado`, `cancelado`, `active`) | `RECORD_LIFECYCLES.md` §1–§2, `JOB_ON.md` §9/§9A/§15 |
| `production_id`, `job_on_revision_id` | `IDENTITIES_AND_RELATIONSHIPS.md`, `JOB_ON.md` §15, `JOB_ON_LIGHT.md` |
| quantity snapshot columns (`quantity_needed`, `quantity_in_machine`) | `JOB_ON.md` §5/§15, `FERRAMENTAS.md` §6 ("No Job On quantity snapshot exists") |
| `requires_pegamentos` / `pegamentos_note` | `JOB_ON.md` §12 (full-Job-On Pegamentos requirement, outside Beta Job On Light scope) |
| sections/drop/stoppage/type/weight/notes/manual blocks, verification occurrences | `JOB_ON_LIGHT.md` "Simplified Beta fields" + "Explicitly outside Beta"; global `JOB_ON.md` §6 full inventory is **not** Beta capture scope |
| reverse arrays | `IDENTITIES_AND_RELATIONSHIPS.md` "No reverse-ID arrays" |

`production_number` is stored as `text`, not integer: the value is a production identifier that must
be reproduced verbatim in the P2-T08 `<reference>/<production-number>/` directory, and authority
fixes no numeric semantics. This is recorded in §21 Q11.

### 3.4 `cm_contexts`, `mf_contexts`, `bq_contexts` — production-specific Tool contexts

Three tables, identical shape, one per context identity kind. Authority fixes **three distinct
context identities with three distinct captured-value families** (`JOB_ON.md` §3: `cm_id` carries
"captured CM-facing values", `mf_id` MF-facing, `bq_id` BQ-facing; §5.6 confirms the per-type field
inventory). A single discriminated table would have to become a sparse shared table as soon as
P2-T05 adds the CM/MF/BQ-facing fields, so the three-table model is the one that matches authority;
this is recorded as §21 Q5.

**`cm_contexts`** (then `mf_contexts`, `bq_contexts` with `mf_id`/`bq_id` and `'MF'`/`'BQ'`):

| Column | Type | Null | Contract |
|---|---|---|---|
| `cm_id` | `uuid` | NO | PK; default `gen_random_uuid()`; application-allocated |
| `jobon_id` | `uuid` | NO | FK → `job_ons(jobon_id)` `ON DELETE RESTRICT`; the owning occurrence |
| `tool_id` | `uuid` | NO | FK → `tools(tool_id)` `ON DELETE RESTRICT`; the direct canonical Tool relation |
| `tool_type` | `text` | NO | frozen Tool type; DB `CHECK (tool_type = 'CM')` |
| `tool_reference` | `text` | NO | frozen Tool reference as used at this occurrence |
| `tool_lot` | `text` | NO | frozen Tool lot as used at this occurrence |
| `created_at` | `timestamp with time zone` | NO | default `now()` |
| `updated_at` | `timestamp with time zone` | NO | default `now()` |

No `version` column: a context is only ever written inside a Job On mutation transaction, and the
`job_ons.version` token protects the aggregate (same accepted pattern as `template_modules`).

**No new "context snapshot" table and no second historical entity is created.** The frozen values
are columns on the context row itself, which is the smallest model that satisfies "freeze the
selected Tool relation and required captured Tool-facing values" (`JOB_ON.md` §5) and "later live
Tool changes never rewrite that context" (§5, §9).

**Deliberately absent from every context table:** `processo` (Tool-owned — `JOB_ON.md` §6),
quantity/stock values (`JOB_ON.md` §5), operational note (`JOB_ON.md` §5: stored once on the Tool,
never duplicated per Job On), Baffle/calote context (`JOB_ON.md` §89 — outside Beta), measurement
or Controlo values (P2-T05), Boquilhas movement facts (P2-T07), any status/lifecycle column, and any
machine column (the machine is the Job On's production fact, not a second per-context copy — §7.5).

### 3.5 Snapshots versus live Tool state

```text
tools.tool_reference / tools.lot / tools.tool_type     = CURRENT canonical Tool facts
cm_contexts / mf_contexts / bq_contexts
  ├─ tool_id            -> direct relation to the canonical Tool   (not frozen)
  └─ tool_type / tool_reference / tool_lot  = FROZEN as used at that occurrence
```

Consequences fixed by this contract:

1. A later change to a canonical Tool's reference/lot metadata (`FERRAMENTAS.md` §536 allows
   reference metadata to change without changing `tool_id`) **must not** change any existing
   context row. No code path copies live Tool values into a context except at the moment of an
   explicit human Tool selection for that context (§7.4).
2. A context row is **not** a second canonical Tool: it carries no Tool-owned mutable fact beyond
   the frozen type/reference/lot triple, and it is never used to answer "what is this Tool like
   now".
3. `tool_id` remains the only persisted cross-module Tool identity. No context row stores a
   "Tool name", "Tool display key" or any other surrogate identity.

---

## 4. Keys and constraints

### 4.1 Primary keys

```text
PK_tools           (tool_id)
PK_tool_machines   (tool_machine_id)
PK_job_ons         (jobon_id)
PK_cm_contexts     (cm_id)
PK_mf_contexts     (mf_id)
PK_bq_contexts     (bq_id)
```

All are `uuid` and application-allocated before the transaction (§11.1); the DB default
`gen_random_uuid()` exists only as the foundation's column convention and is never the source of a
canonical identity used by the application.

### 4.2 Unique constraints (exact tuples)

Uniqueness is implemented exactly as the accepted foundation convention does it: a **unique index**
declared through `HasIndex(...).IsUnique().HasDatabaseName("<name>_key")`
(precedent: `users_company_number_key`, `template_modules_template_order_key`), not through a
separate `ALTER TABLE … ADD CONSTRAINT`. The table below is the semantic register; §4.5 is the
physical index register and both use the same names.

| Constraint name | Table | Tuple | Meaning |
|---|---|---|---|
| `tools_type_reference_lot_key` | `tools` | (`tool_type`, `reference`, `lot`) | **the canonical Tool identity tuple**; prevents a duplicate canonical Tool |
| `tool_machines_tool_machine_key` | `tool_machines` | (`tool_id`, `machine`) | one compatibility row per machine |
| `job_ons_reference_production_number_key` | `job_ons` | (`reference`, `production_number`) | **the Job On production uniqueness rule** |
| `cm_contexts_jobon_key` | `cm_contexts` | (`jobon_id`) | at most one CM context per Job On |
| `mf_contexts_jobon_key` | `mf_contexts` | (`jobon_id`) | at most one MF context per Job On |
| `bq_contexts_jobon_key` | `bq_contexts` | (`jobon_id`) | at most one BQ context per Job On |

Selection rationale for the two load-bearing tuples:

- **Tool = (type, reference, lot).** Authority fixes "a different lot is operationally a different
  Tool" (`FERRAMENTAS.md` §3.1, `FERRAMENTAS_LIGHT.md`, `BETA_VERSION.md` §11) and fixes reference
  and lot as Tool-owned identity facts. `CM`, `MF` and `BQ` are distinct Tool families
  (`BETA_VERSION.md` §2.1 "type: CM, MF or BQ"), so the type participates: collapsing a CM and a BQ
  that share reference+lot into one identity would be a wrong unification with no authority, and it
  is the irreversible direction of error — whereas the application still refuses to create a Tool
  whose (type, reference, lot) exists.
  `processo` is **not** in the tuple: it is a Tool-owned *field*, and authority settles that a wrong
  Tool-owned field is corrected **on the Tool**, not by creating another identity
  (`JOB_ON.md` §9: "correct that field on the canonical `tool_id`. That is a Tool data correction,
  not a new Tool identity"). `quantity` is not in the tuple for the same reason.
- **Job On = (reference, production_number).** `docs/CREATION_AND_ASSOCIATION_LOGIC.md`
  "Job On uniqueness rule" fixes the expected business key as `reference + production_number` and
  extends it only "if the real process allows more than one distinct Job On with the same
  combination". The P2-T04 handoff §5 repeats the same rule and forbids adding machine/date "for
  safety". No authority in any of the three repositories asserts the extending condition, so the
  pair is complete (§21 Q1 records the extension path and the machine evidence examined).

### 4.3 Foreign keys

| FK | From | To | Delete behavior |
|---|---|---|---|
| `FK_tool_machines_tools_tool_id` | `tool_machines.tool_id` | `tools.tool_id` | `RESTRICT` |
| `FK_job_ons_job_ons_copied_from_jobon_id` | `job_ons.copied_from_jobon_id` | `job_ons.jobon_id` | `RESTRICT` |
| `FK_cm_contexts_job_ons_jobon_id` | `cm_contexts.jobon_id` | `job_ons.jobon_id` | `RESTRICT` |
| `FK_cm_contexts_tools_tool_id` | `cm_contexts.tool_id` | `tools.tool_id` | `RESTRICT` |
| `FK_mf_contexts_job_ons_jobon_id` | `mf_contexts.jobon_id` | `job_ons.jobon_id` | `RESTRICT` |
| `FK_mf_contexts_tools_tool_id` | `mf_contexts.tool_id` | `tools.tool_id` | `RESTRICT` |
| `FK_bq_contexts_job_ons_jobon_id` | `bq_contexts.jobon_id` | `job_ons.jobon_id` | `RESTRICT` |
| `FK_bq_contexts_tools_tool_id` | `bq_contexts.tool_id` | `tools.tool_id` | `RESTRICT` |

**No foreign key in this schema cascades.** Consequence, relied on by §11.5: a Job On that has
duplicates, or that is referenced by any future dependent table, cannot be physically deleted by a
careless statement — the database fails closed and the application refuses first with a typed
reason.

The self-FK on `copied_from_jobon_id` means **a Job On used as the recorded duplication source is
itself protected** (its recorded lineage is part of persisted history and must not be silently
nulled). This is included in the dependency vocabulary of §11.5.

### 4.4 Check constraints

| Constraint name | Table | Expression |
|---|---|---|
| `tools_type_check` | `tools` | `tool_type IN ('CM','MF','BQ')` |
| `tools_reference_required_check` | `tools` | `btrim(reference) <> ''` |
| `tools_lot_required_check` | `tools` | `btrim(lot) <> ''` |
| `tools_processo_check` | `tools` | `processo IS NULL OR processo IN ('NNPB','PS')` |
| `tools_quantity_check` | `tools` | `quantity IS NULL OR quantity >= 0` |
| `tool_machines_machine_check` | `tool_machines` | `machine IN ('B1','B2','B3','C1','C2','C3')` |
| `job_ons_reference_required_check` | `job_ons` | `btrim(reference) <> ''` |
| `job_ons_production_number_required_check` | `job_ons` | `btrim(production_number) <> ''` |
| `job_ons_machine_check` | `job_ons` | `machine IN ('B1','B2','B3','C1','C2','C3')` |
| `cm_contexts_tool_type_check` | `cm_contexts` | `tool_type = 'CM'` |
| `cm_contexts_tool_reference_required_check` | `cm_contexts` | `btrim(tool_reference) <> ''` |
| `cm_contexts_tool_lot_required_check` | `cm_contexts` | `btrim(tool_lot) <> ''` |
| `mf_contexts_tool_type_check` | `mf_contexts` | `tool_type = 'MF'` |
| `mf_contexts_tool_reference_required_check` | `mf_contexts` | `btrim(tool_reference) <> ''` |
| `mf_contexts_tool_lot_required_check` | `mf_contexts` | `btrim(tool_lot) <> ''` |
| `bq_contexts_tool_type_check` | `bq_contexts` | `tool_type = 'BQ'` |
| `bq_contexts_tool_reference_required_check` | `bq_contexts` | `btrim(tool_reference) <> ''` |
| `bq_contexts_tool_lot_required_check` | `bq_contexts` | `btrim(tool_lot) <> ''` |

The per-context `tool_type` CHECK makes it **impossible at the database level** for an MF context to
record a CM/BQ frozen type: a context type and its frozen Tool type can never disagree in
persisted data.

### 4.5 Indexes (each justified by a contracted query)

| Index name | Table | Columns | Unique | Justified by |
|---|---|---|---|---|
| `tools_type_reference_lot_key` | `tools` | `tool_type, reference, lot` | YES | §5.5 duplicate prevention; §8.2 Tool search when type + reference are supplied (leftmost prefix) |
| `tools_reference_idx` | `tools` | `reference` | NO | §8.2 Tool search prefilled from the Job On reference (the dominant Beta search), and §8.3 contextual Tool listing by reference |
| `tool_machines_tool_machine_key` | `tool_machines` | `tool_id, machine` | YES | uniqueness + "compatible machines of this Tool" read |
| `tool_machines_machine_idx` | `tool_machines` | `machine` | NO | §8.2 Tool search filtered by machine compatibility |
| `job_ons_reference_production_number_key` | `job_ons` | `reference, production_number` | YES | uniqueness + §8.1 reference → productions (leftmost prefix `reference`) |
| `IX_job_ons_copied_from_jobon_id` | `job_ons` | `copied_from_jobon_id` | NO | FK-supporting index (EF default name convention); §11.5 lineage dependency probe and §11.4 duplication-source reads |
| `cm_contexts_jobon_key` / `mf_contexts_jobon_key` / `bq_contexts_jobon_key` | context tables | `jobon_id` | YES | one context per type per Job On + context resolution by Job On |
| `IX_cm_contexts_tool_id` / `IX_mf_contexts_tool_id` / `IX_bq_contexts_tool_id` | context tables | `tool_id` | NO | FK-supporting index; query-based reverse lookup "which occurrences used this Tool" (`IDENTITIES_AND_RELATIONSHIPS.md`: reverse navigation is a query, never an array) |

**No other index is contracted.** In particular there is no index supporting a lot-only or
machine-only Job On search (no such query is contracted) and no index on `production_date` (the date
is never a query predicate — the threshold is evaluated on the loaded row). A Tool search supplying
only `lot` (no type, no reference) is applied as a filter after the reference/type predicate; this
is accepted for the Beta registry size and recorded as §21 Q12.

### 4.6 Delete behavior summary

| Statement | Result |
|---|---|
| delete a `job_ons` row | allowed by the database only when no `job_ons.copied_from_jobon_id` and no context row references it; the application additionally refuses when any dependency probe reports a dependency (§11.5) |
| delete a `tools` row | refused while any `tool_machines` or any context row references it — **P2-T04 implements no Tool deletion path at all** (§5.6) |
| delete a context row | performed only inside a Job On edit transaction (§11.4); refused while a future dependent table references the context |

---

## 5. Canonical Tool rules

### 5.1 One canonical registry

`tools` is the only Tool registry in the system. P2-T04 creates no second Tool table, no
module-private Tool store, no Tool cache table and no "provisional Tool" entity. Job On, Controlo
and (later) Boquilhas reference the same `tool_id` (`CROSS_MODULE_FLOWS.md` "Shared Tool flow";
`FERRAMENTAS_LIGHT.md` "Ownership"; `BETA_VERSION.md` §2).

### 5.2 Tool-owned facts fixed by this contract

Exactly the six Beta minimum facts of `BETA_VERSION.md` §2.1 are contracted:

```text
tool_type            CM | MF | BQ                       (required)
reference            non-blank text                     (required, identity fact)
lot                  non-blank text                     (required, identity fact)
machine compatibility one or more of B1 B2 B3 C1 C2 C3  (required, >= 1)
quantity             integer >= 0, or NULL              (canonical total where canonical)
processo             NNPB | PS, or NULL                 (where applicable to that Tool)
```

No other Tool fact is created, stored, returned or displayed by P2-T04. The Tool-owned facts that
authority lists but Beta excludes (technical condition, technical condition history, persistent
operational note, utilisation, drawing/revision, Baffle geometry, change requests) are recorded as
**not implemented** in §17, not as empty columns.

### 5.3 Create semantics

1. The command carries the six facts above and nothing else.
2. Validation runs **before any write** (§12.3): required facts, closed value sets, at least one
   machine, `quantity >= 0` when supplied.
3. Duplicate prevention: the service queries the canonical registry for the exact
   (`tool_type`, `reference`, `lot`) tuple. If a Tool exists, the create is **refused** with
   `ToolResult.DuplicateIdentity(existingToolId, …)` and the operator must select the existing Tool
   (this is the "no duplicate canonical Tool" rule and it is also enforced by the unique index for
   the concurrent case).
4. The backend allocates `tool_id` (a real canonical identity) and inserts `tools` + one
   `tool_machines` row per compatible machine **in one transaction**. Either the Tool with its full
   compatibility set exists, or nothing exists.
5. The result carries the real canonical `tool_id`. No provisional, temporary, display-key or
   client-minted identity is ever returned or persisted.
6. **No Tool update and no Tool delete path exists in P2-T04.** The Tool is immutable in this
   workstream: there is no `PUT`/`PATCH`/`DELETE` Tool route, no Tool version column and no Tool
   change-request. Tool editing is the full Ferramentas lifecycle (change-request under Ferramentas,
   direct edit under Ferramentas Approve) and is explicitly outside Beta Light scope
   (`FERRAMENTAS_LIGHT.md` "Explicitly outside Beta"; `RECORD_LIFECYCLES.md` §10).
7. Because there is no Tool mutation, "wrong Tool-owned field" corrections
   (`JOB_ON.md` §9) are **out of this workstream**: they require the Tool-edit authority just
   described and are recorded as §21 Q13.

### 5.4 Lot rules

```text
same reference + same lot      -> the same canonical Tool (or a duplicate attempt that is refused)
same reference + different lot -> a DIFFERENT canonical Tool (a new tool_id)
```

- Lot is an identity fact of the concrete Tool, **never** a child layer beneath one Tool
  (`FERRAMENTAS.md` §3.1 explicitly forbids a `tool_lote_id`-style layer). P2-T04 creates no lot
  table, no lot id and no lot→Tool relationship table.
- Reference and lot are stored trimmed of leading/trailing whitespace and compared exactly
  (ordinal). No case-folding, normalisation or fuzzy matching is invented (§21 Q14).
- The lot is never derived, never inferred from the reference, and never auto-filled.

### 5.5 Duplicate-prevention semantics (two layers)

| Layer | Mechanism | Result |
|---|---|---|
| application | identity query on (`tool_type`, `reference`, `lot`) before the insert | `DuplicateIdentity` with the existing `tool_id`; nothing written |
| database | unique index `tools_type_reference_lot_key` | a losing concurrent insert raises `23505` with that constraint name, mapped to `DuplicateIdentity`; nothing written twice |

Both layers must exist: the application layer produces the useful operator message, the database
layer closes the race. This is the accepted `UserRepository` constraint-mapping pattern
(`TryMapConstraintViolation` on `PostgresException.SqlState`), reusing the same infrastructure
approach with a Tool-specific typed failure.

### 5.6 Search semantics (behavioural rules)

- **Inputs** are exactly the contracted search criteria (§8.2). No hidden filter is added, and in
  particular no "same machine only", "latest only", "in-stock only" or "compatible ranking" rule is
  invented (`CROSS_MODULE_FLOWS.md` anti-inference rules; `BACKEND_FRONTEND_MODEL.md` "Selection
  versus inference").
- **Output** is one item per matching canonical Tool, never merged, deduplicated or grouped.
- **Selection is always explicit.** Zero results is an explicit empty; one result is presented
  exactly like many and is **never** auto-selected; ambiguity remains visible as N separate items
  (`FERRAMENTAS_LIGHT.md` "Search/select/create contract"; `SHARED_FRONTEND_CONTRACT_FREEZE.md` §7).
- **Identity is never inferred** from reference, lot, machine or type text
  (`FERRAMENTAS_LIGHT.md`; `IDENTITIES_AND_RELATIONSHIPS.md` "Human selection rule").
- **Filtering by machine** means "the Tool is registered as compatible with that machine", read
  from `tool_machines`; it never means "the Tool is physically there" (Armazém owns location truth —
  `JOB_ON.md` §10).
- **Ordering** is the deterministic technical order of §8.2. Authority fixes no industrial ordering
  for Tool search; none is invented (§21 Q15).
- **Permissions**: the search endpoint carries the `ferramentas` policy (§13). A denied caller is
  denied, never shown an empty list (`permission-denied ≠ empty`).

---

## 6. Job On occurrence rules

### 6.1 `jobon_id` is one production occurrence

`jobon_id` identifies **one concrete production occurrence**. It is not a snapshot, not a revision,
not a lifecycle container and not a "current production" pointer. `Create New Job On` and
`Duplicate Selected Existing Job On` both create a **new** `jobon_id`
(`JOB_ON.md` §8; `JOB_ON_LIGHT.md` "Duplicate workflow"; `BETA_VERSION.md` §4.1).

### 6.2 Fields captured (exact, and nothing else)

```text
reference           required, non-blank
production_number   required, non-blank (stored verbatim as entered, trimmed)
machine             required, one of B1 B2 B3 C1 C2 C3
production_date     optional (date) — the §6.6 threshold fact
(+ system: jobon_id, copied_from_jobon_id, version, created_at, updated_at)
```

Plus the optional production contexts (§7), which are separate rows — never columns on the Job On.

The Beta capture set is exactly `JOB_ON_LIGHT.md` "Simplified Beta fields" (Referência, Número de
produção, Máquina/Linha, Processo-as-consumed, optional CM/MF/BQ) plus the single production date
required to implement the accepted date-threshold edit warning (`RECORD_LIFECYCLES.md` §2,
`JOB_ON.md` §9A, master plan §11 P2-T04 "the date-threshold edit is a warning"). `processo` is
**consumed, never captured** (§6.3). The frontend must not invent additional full-Job-On fields that
appear in the future complete Job On (`JOB_ON_LIGHT.md`; handoff §6).

`production_date` is a single nullable column. Its domain role in P2-T04 is exactly the
planned-production-date threshold of `JOB_ON.md` §9A; it is explicitly **planning data and never
proof that production occurred**. §21 Q10 records the alternative (a separate planned vs actual
date pair) and why the single column is the smaller model.

### 6.3 `processo` is consumed, never a Job On fact

```text
jobon_id -> cm_id -> tool_id -> processo = NNPB | PS
```

`processo` is a canonical Tool-owned fact (`FERRAMENTAS.md` §3) reached through the Job On's CM
context. There is **no** `job_ons.processo` column, no Job On processo DTO field that can be written
by a Job On command, and no second process authority. Where the Job On has no CM context yet,
`processo` is **not available** and the surfaces show it as unavailable — it is never inferred,
never defaulted and never copied from another Job On (§21 Q2).

### 6.4 Production uniqueness rule (exact)

```text
UNIQUE (reference, production_number)
```

- The rule is the repository-recorded creation logic (`docs/CREATION_AND_ASSOCIATION_LOGIC.md`
  "Job On uniqueness rule") and the B1 statement in the P2-T04 handoff §5.
- **Machine, date and every other fact are deliberately excluded.** The handoff forbids adding them
  "merely for safety" and requires citing the exact authority that extends the pair. No authority in
  `dmo-master`, `dmo-beta-master` or this repository asserts the extending condition ("the real
  process allows more than one distinct Job On with the same combination"). §21 Q1 records the
  machine evidence that was examined and the exact smallest extension
  (`+ machine`) that a future authority decision would require.
- The system must **not** decide two Job Ons are the same "merely because they look similar", and
  must not silently reuse an existing Job On: a create attempt on an existing pair is refused with
  `DuplicateProduction` naming the existing `jobon_id`, and the operator then explicitly selects
  that production or explicitly duplicates a source. There is no automatic reuse, no automatic
  resolution and no automatic duplication (`CROSS_MODULE_FLOWS.md` anti-inference rules).

### 6.5 No lifecycle state machine

No Job On-wide status, stage, phase or state exists in this contract, in the schema, in the domain
types, in the commands, in the results, in the routes or in the tests. Specifically absent:
`rascunho`, `planeado`, `em fabrico`, `fechado`, `cancelado`, `active`, `locked`, `approved`
(`RECORD_LIFECYCLES.md` §1–§2; `JOB_ON.md` §9, §9A, §15; master plan §14.2 "A generic
lifecycle/state engine — explicitly forbidden").

The only protections are the two fact-based ones that authority does fix: the planned-production-date
**warning** (§6.6) and the existence of **dependent operational facts** (§11.5).

### 6.6 Date-threshold edit is a warning, never hard immutability

```text
persisted production_date is NULL                        -> edit allowed normally
today <  persisted production_date                       -> edit allowed normally
today >= persisted production_date                       -> edit allowed ONLY with an explicit
                                                            acknowledgement; otherwise refused with
                                                            DateThresholdConfirmationRequired
```

- There is **no** `IF production_date <= now THEN UPDATE forbidden` rule (`JOB_ON.md` §9A).
- The acknowledgement is a request fact (`DateThresholdWarningAcknowledged`), not a stored state,
  not a status column and not an approval (`RECORD_LIFECYCLES.md` §12 "warnings remain warnings").
- The threshold is evaluated against the **currently persisted** `production_date` of the Job On
  being edited, which is exactly the authority wording ("a warning/confirmation that the Job On
  concerns a production date already reached/passed"). Changing the date in the same edit does not
  remove the warning, and §21 Q9 records whether a **create** with a past date should also warn
  (authority scopes the threshold to edit; the pinned default is no create-time warning).
- A refusal is transport-visible (reason token in §14) and never a silent no-op, and the operator's
  entered values are retained by the surface.

### 6.7 Contexts are created only where actually needed

Both Beta entry paths are supported and produce the same identities
(`BETA_VERSION.md` §4):

1. associate CM/MF/BQ directly on the Job On when the operator already knows the Tools;
2. leave them empty and let the consuming workflow (Controlo P2-T05 / Boquilhas P2-T07) create the
   missing context later.

There is no "Job-On-created" versus "Controlo-created" distinction, and **no empty context identity
is created for symmetry** (`BETA_VERSION.md` §4; master plan §8 rule 4).

### 6.8 Reference → productions behaviour (occurrence side)

- Every matching occurrence for the reference is returned, including old ones; there is no "latest
  production" notion and no hidden recency filter (`JOB_ON_LIGHT.md` "may use an older/non-latest
  source"; `BETA_VERSION.md` §4.1).
- An occurrence is selected only by an explicit human action, even when exactly one is returned.
- A blank/missing reference is a validation failure; a valid reference with no occurrences is an
  explicit empty; a failed lookup is a lookup failure. The three are never conflated
  (`SHARED_FRONTEND_CONTRACT_FREEZE.md` §4).

---

## 7. Context / snapshot rules

### 7.1 Context creation conditions

A context row is created **iff** the corresponding Tool slot is explicitly selected:

```text
CM slot tool selected  -> exactly one cm_contexts row for that jobon_id  (or an in-place update)
MF slot tool selected  -> exactly one mf_contexts row for that jobon_id  (or an in-place update)
BQ slot tool selected  -> exactly one bq_contexts row for that jobon_id  (or an in-place update)
slot not selected      -> NO row of that type exists for that jobon_id
```

Consequences fixed by this contract:

- `SELECT count(*) FROM cm_contexts WHERE jobon_id = :id` is 0 or 1 — never more;
- a Job On with no CM association has **no** `cm_id` (there is no placeholder, no empty context, no
  `NULL`-Tool context row);
- contexts are never created by a read, by a list query, by duplication of an empty slot, or by
  "completing the set".

### 7.2 Direct canonical relation and frozen values

Each context row carries `tool_id` (live canonical relation) **and** the frozen triple
`tool_type` / `tool_reference` / `tool_lot` captured at the moment of the explicit selection.

- The frozen triple is explicitly authorised: `FERRAMENTAS.md` §6 settles that the new CM/MF/BQ
  historical context "freezes the Tool-facing state required to explain that production later. At
  minimum where displayed/required this may include: `tool_id`; readable type/reference/lot context
  value…"; `JOB_ON.md` §5 fixes "freeze the selected Tool relation and required captured CM/MF/BQ
  historical values"; `RECORD_LIFECYCLES.md` §3 fixes the same.
- Nothing else is frozen. The exclusions (`quantity`, `processo`, operational note, Baffle/calote,
  measurements, Boquilhas facts, machine) are each an authority decision, cited in §3.4.
- The frozen triple is **not** a second Tool authority: it is never queried to answer "what is this
  Tool now", never used as a join key, and never rendered as if it were current Tool state. The
  live ficha always reads through `tool_id`.
- <b>Every new context is a new snapshot:</b> normal creation, an explicit re-selection (§7.4) and
  duplication each capture the CURRENT canonical Tool row at that moment through the selected
  `tool_id`. Duplication never clones an older context's frozen triple (§23 — the Owner
  clarification supersedes the former verbatim-copy wording of §10.3/§21 Q16).

### 7.3 Idempotent create/reuse for the same `jobon_id + tool_id`

Selecting the same Tool for a slot that already holds it is a **reuse**: no second row, no id change,
no version bump beyond the enclosing transaction's single bump, no duplicate. The repository resolves
by (`jobon_id`, context type); when the resolved row already references the requested `tool_id`, the
context is returned unchanged.

This is the accepted invocation semantics for P2-T05/P2-T07 consumers
(`BETA_INTEGRATION_SEAMS.md` "C → D/B seam": "CM/MF/BQ production-context resolution"; master plan
§11 P2-T04 "CM/MF/BQ context create/reuse is idempotent for the same `jobon_id + tool_id`").

### 7.4 Update rules

| Change | Effect |
|---|---|
| select a Tool for a slot with no context | insert one context row (new context id), freezing the Tool's current triple |
| select a **different** Tool for a slot that already has a context | **in-place update** of that same context row: `tool_id` replaced and the frozen triple refreshed from the newly selected Tool at that moment. The context id is retained and no second row appears (`JOB_ON.md` §9: "correct the context's `tool_id`… does not require a historical cascade/repoint mechanism"). |
| re-select the **same** Tool for a slot that already has it | no-op reuse (§7.3) |
| remove the Tool association from a slot | the context row is deleted inside the same Job On transaction (§11.4) |
| any other field of a context | **not editable** — a context has no other field |
| a change to the canonical Tool's own metadata | **no effect at all** on any context row |

If the selected Tool's `tool_type` does not match the slot's context type, the whole operation fails
with a validation failure (`TOOL_TYPE_MISMATCH`); no context is written (§21 Q3).

### 7.5 Why no machine column on a context

The machine is a **Job On production fact** (`job_ons.machine`). The Tool's *compatibility* set is a
Tool-owned fact (`tool_machines`). A per-context machine copy would duplicate the Job On fact without
authority, and the full-Job-On captured-value inventory allows a machine/line context value only
"where the real sheet uses them" — not fixed by Beta Light. Therefore no context table carries a
machine column, and "machine context" is read from the Job On (§21 Q6).

### 7.6 Historical immutability guarantees (exact)

The following are impossible by construction in P2-T04, not merely discouraged:

1. no code path copies live Tool values into an existing context row (only §7.4's explicit
   re-selection does, and it is a human action);
2. no code path propagates a Tool change to contexts — there is no such method, event, trigger or
   background worker in the contract;
3. no context row can change its context type (DB CHECK, §4.4);
4. no context row can exist without a `jobon_id` and a `tool_id` (NOT NULL FKs);
5. no context row can be created for a Job On that does not exist (FK);
6. a Job On's contexts can only be reached through that Job On or through an explicit Tool-reverse
   query (§4.5), never through a reverse array on the Tool.

---

## 8. Query contracts

Every query is an application-layer contract. Nothing in the Web layer queries the database
directly, and no module reads another module's tables (`BETA_INTEGRATION_SEAMS.md` blocker rule).

### 8.1 Reference → productions

| Element | Contract |
|---|---|
| **Input carrier** | `DMO.Application.JobOn.FindProductionsQuery(string Reference)` |
| **Input rules** | required; trimmed of leading/trailing whitespace; blank ⇒ validation failure (`REFERENCE_REQUIRED`), **not** an empty result |
| **Matching** | exact match on `job_ons.reference` (ordinal, case-sensitive, matching the storage rule of §6.2) |
| **Result carrier** | `IReadOnlyList<JobOnProductionListItem>` |
| **Item shape** | `JobOnProductionListItem(Guid JobOnId, string Reference, string ProductionNumber, string Machine, DateOnly? ProductionDate)` |
| **Deliberately not in the item** | context ids, context presence flags, Tool facts, Controlo state, Boquilhas state, document availability. Any of those is a separate contracted read; none is copied into this carrier to save a call (`BETA_INTEGRATION_SEAMS.md` "must not read C's private tables or copy Controlo state"; master plan §8 cross-cutting rules) |
| **Ordering** | `ORDER BY production_number ASC, production_date ASC NULLS LAST, jobon_id ASC` |
| **Ordering authority** | **authority-silent** — no industrial ordering (no "most recent first", no "latest production") is fixed anywhere; the contracted order is a deterministic technical order chosen only so paging/lists are stable. It carries no industrial meaning and must not be used to pre-select a production (§21 Q7) |
| **Ambiguity** | N matches ⇒ N items; nothing is merged, ranked, filtered or pre-selected |
| **Empty** | explicit empty result (no rows) distinguishable from a lookup failure |
| **Historical occurrences** | included; never excluded for age |
| **Permission** | performed under the calling route's policy (§13): `job-on-view` for the consult surface, `job-on-create` for the create surface. A denied caller receives the policy denial, never an empty list |
| **Unbounded scan** | none: the query is always predicate-bound by `reference` |

### 8.2 Tool search / list

| Element | Contract |
|---|---|
| **Input carrier** | `DMO.Application.Tools.ToolSearchQuery(string? Query, ToolType? Type, string? Reference, string? Lot, MachineCode? Machine, int Limit)` |
| **Input rules** | `Limit` required, `1..100` inclusive (a value outside the range is a validation failure; the limit is never ignored); at least one of `Query`/`Reference`/`Lot`/`Type`/`Machine` must be supplied, else validation failure (`SEARCH_CRITERIA_REQUIRED`) so that no unbounded registry dump is contracted; `Reference`/`Lot`/`Query` are trimmed, and a blank-after-trim value counts as not supplied |
| **Matching** | `Query` is a case-insensitive substring match over `reference` and `lot` **only**; `Type`, `Reference`, `Lot` are exact matches; `Machine` matches an existing `tool_machines` row |
| **Result carrier** | `IReadOnlyList<ToolSearchItem>` |
| **Item shape** | `ToolSearchItem(Guid ToolId, ToolType Type, string Reference, string Lot, Processo? Processo, int? Quantity, IReadOnlyList<MachineCode> CompatibleMachines)` — `CompatibleMachines` ordered by the settled machine order `B1,B2,B3,C1,C2,C3` |
| **Deliberately not in the item** | any fabricated status, availability, technical condition, stock/location state, utilisation, operational note or "compatible with your Job On" verdict (`FERRAMENTAS_LIGHT.md`; P2-T03 records candidate status as an accepted silence) |
| **Ordering** | `ORDER BY tool_type ASC, reference ASC, lot ASC, tool_id ASC` |
| **Ordering authority** | **authority-silent**; deterministic technical order only, no relevance/industrial ranking, no scoring (`FERRAMENTAS_LIGHT.md` "search/select/create contract"; P2-T03 forbids ranking in the picker) (§21 Q15) |
| **Ambiguity** | N matches ⇒ N items, never merged or de-duplicated |
| **Empty** | explicit empty, distinct from lookup failure |
| **Limit** | applied at the database level (`LIMIT`); the number of returned items never exceeds `Limit` |
| **Permission** | `ferramentas` (§13). Denied ⇒ denial, never an empty list |
| **No inference** | the query never resolves "the Tool" for a reference/lot/machine combination; reference + lot + machine alone is explicitly forbidden as an identity inference (`FERRAMENTAS_LIGHT.md`) |

The Tool **list** capability is this same query with only `Type` or `Reference` supplied; no separate
list contract, endpoint or method is created.

### 8.3 Context resolution (read)

| Element | Contract |
|---|---|
| **Input** | `(JobOnId jobOnId, ToolContextType contextType)` |
| **Result** | the resolved `ToolContext` (id + `tool_id` + frozen triple) or `null` when no context of that type exists |
| **Rules** | at most one row can match (unique constraint); no creation happens on a read; a missing context is a legitimate "not associated yet" state, never an error and never an empty placeholder row |
| **Reverse read (which occurrences used this Tool)** | `IReadOnlyList<JobOnProductionListItem>` by `tool_id` through the context tables — query-based, never an array on the Tool (`IDENTITIES_AND_RELATIONSHIPS.md` "No reverse-ID arrays"). This read exists for the contextual ficha and later modules; it is **not** exposed as a Job On route in P2-T04 |

### 8.4 Job On read (ficha)

| Element | Contract |
|---|---|
| **Input** | `JobOnId` |
| **Result carrier** | `JobOnFicha(Guid JobOnId, string Reference, string ProductionNumber, string Machine, DateOnly? ProductionDate, Guid? CopiedFromJobOnId, int Version, IReadOnlyList<ToolContextFicha> Contexts)` |
| **`ToolContextFicha`** | `(ToolContextType ContextType, Guid ContextId, Guid ToolId, ToolType ToolType, string ToolReference, string ToolLot, ToolSummaryProjection Tool)` — the frozen triple **plus** the live `tool_id` |
| **`ToolSummaryProjection`** | `(Guid ToolId, ToolType Type, string Reference, string Lot, Processo? Processo, int? Quantity, IReadOnlyList<MachineCode> CompatibleMachines)` — the **live** Tool facts of the referenced Tool, composed **at read time** from `tools`/`tool_machines`. It is a projection: it is never persisted into `job_ons` or a context row, and it is not Tool access. `JOB_ON.md` §5/§15 and `FERRAMENTAS.md` §6 require the Job On to display the referenced Tool's current facts (including the Tool count and `processo` "read live through `tool_id`"), so this projection belongs to the Job On read model and is gated by the Job On policy (see §21 Q21). A surface that wants the **contextual Tool ficha** (usage occurrences, future Tool-owned facts) uses §8.5, which carries the `ferramentas` policy |
| **Missing** | `NotFound` (never an empty ficha, never a fabricated record) |
| **Permission** | `job-on-view` on the consult surface, `job-on-create` on the edit/duplicate surfaces (§13) |
| **Historical integrity** | the projection is live Tool state and is never stored; the ficha therefore cannot be used to "refresh" the frozen triple, and the frozen triple is never presented as if it were the Tool's current value |

### 8.5 Tool detail (contextual ficha read)

| Element | Contract |
|---|---|
| **Input** | `ToolId` |
| **Result carrier** | `ToolFicha(Guid ToolId, ToolType Type, string Reference, string Lot, Processo? Processo, int? Quantity, IReadOnlyList<MachineCode> CompatibleMachines, IReadOnlyList<ToolUsageOccurrence> UsageOccurrences)` |
| **`ToolUsageOccurrence`** | `(Guid JobOnId, string Reference, string ProductionNumber, string Machine)` — the reverse read of §8.3, present because the contextual ficha is the canonical Tool surface and Job On occurrence listing is the only reverse relation P2-T04 owns |
| **Missing** | `NotFound` |
| **Permission** | `ferramentas` |
| **No mutation** | the ficha read carries no edit/create action contract; P2-T04 exposes no Tool mutation (§5.3.6) |

---

## 9. Tool search / select / create orchestration contract

### 9.1 Ownership

One shared orchestration is owned by the P2-T04 application layer and consumed by every P2-T04
surface; later workstreams (P2-T05, P2-T07) consume the same contract rather than forking it
(`CROSS_MODULE_FLOWS.md` "Shared Tool flow"; `BETA_INTEGRATION_SEAMS.md` Workstream B: "canonical
Tool search/select/create", "B is the shared Tool selection/create flow"). P2-T03 remains
presentation/mechanics only and is **not** modified (§18).

### 9.2 The flow, with the layer that owns each step

```text
[P2-T04 origin surface]
  1. operator supplies the known context (reference, machine, expected Tool type)
  2. adapter calls the Tool search query contract (§8.2)
  3. results are mapped to ToolPickerCandidatePresentation items
       - Key            = an OPAQUE candidate key owned by the adapter
       - Facts          = supplied Tool facts (type/reference/lot/machines/quantity/processo)
  4. picker raises `search requested` / `candidate selected` / `create requested` / `cancel requested`
       (P2-T03 vocabulary, unchanged)
  5a. explicit selection -> the adapter resolves its opaque key to the canonical tool_id
  5b. create requested   -> the inline create subflow posts the Tool create command (§5.3)
                           -> the response carries the canonical tool_id
  6. the adapter associates the canonical tool_id with the origin slot and the picker is closed
     through `return requested`
  7. the origin surface submits its own command carrying the canonical tool_id(s)
       (Job On create/edit, §11)
```

### 9.3 Mandatory orchestration rules

1. **No auto-selection.** No step selects, ranks or pre-fills a *selection*. A single candidate is
   presented exactly like many and requires explicit selection
   (`FERRAMENTAS_LIGHT.md`; P2-T03 §3.1.7 which is consumed unchanged).
2. **Ambiguity stays explicit.** N candidates are N items; none is merged, grouped or hidden.
3. **A selection returns exactly one canonical `tool_id`** for exactly one human action. The
   selection result type is `DMO.Application.Tools.ToolSelection(Guid ToolId, ToolType ExpectedType)`
   built by the caller from the resolved key; it is never built from display text.
4. **Create returns the real canonical `tool_id`.** `ToolResult.Created(Guid ToolId)` carries the
   persisted identity allocated by the backend. No provisional identity, no negative/temporary id,
   no display-key surrogate, no "create-on-submit" placeholder
   (`BACKEND_FRONTEND_MODEL.md` "Frontend must not: mint canonical IDs").
5. **The origin-state preservation boundary.**
   - The unsaved origin state belongs to the P2-T04 surface and is preserved on the client; the
     Tool create subflow posts to the Tool endpoint **without navigating away** from the origin
     page, so entered Job On values are never at risk (P2-T04 therefore contracts **no** standalone
     Tool-create page and **no** server-side draft store — §21 Q8).
   - The P2-T03 opaque `OriginToken` is the correlation handle for the origin subflow. It is
     **never** parsed as, declared to be, or persisted as a canonical identity
     (`SHARED_FRONTEND_CONTRACT_FREEZE.md` §2/§7; P2-T03 §2.3), and it never carries a canonical
     Tool/Job On id in a URL, route, filename or path.
   - Cancelling the picker or the create subflow returns to the origin with **no** Tool selected and
     **no** origin value changed.
6. **The adapter holds the opaque→canonical mapping.** The mapping from opaque candidate key to
   `tool_id` lives in the P2-T04 adapter's own state for the duration of the interaction; the
   submitted command carries the canonical `tool_id`, and the backend validates it (existence +
   type match) instead of trusting it.
7. **Pre-fill is assistance only.** Context values already known (reference, machine, expected type)
   may pre-populate the Tool create form or the search criteria; unknown Tool facts are never
   invented (`BETA_VERSION.md` §2.1: "Pre-population is assistance only").
8. **The origin module never becomes Tool owner.** Neither the Job On create/edit surface nor any
   later consumer writes `tools`/`tool_machines` outside the Tool create command; there is no second
   Tool registry and no Job-On-owned Tool copy (`FERRAMENTAS_LIGHT.md` "Ownership").

### 9.4 What P2-T04 does **not** decide

Search ranking/relevance, candidate compatibility verdicts beyond the contracted machine-compatibility
predicate, picker modality/placement, whether create is an overlay or an inline panel, retry timing,
and the visual composition of the picker. Those are either P2-T03 presentation/mechanics (already
frozen) or implementation concerns inside the contracted behaviour.

---

## 10. Duplication transaction

### 10.1 Command

```text
DMO.Application.JobOn.DuplicateJobOnCommand(
    Guid SourceJobOnId,
    int  ExpectedSourceVersion,
    string ProductionNumber,
    string Machine,
    DateOnly? ProductionDate)
```

- The **source** is always explicit (an id chosen by the human) and is never inferred, never "the
  latest", never "the previous" (`JOB_ON.md` §7/§8; `JOB_ON_LIGHT.md` "Duplicate workflow";
  `BETA_VERSION.md` §4.1).
- `reference` is **not** supplied: a duplicate is another production **of the same reference**
  (`JOB_ON.md` §7 "For a reference: create first Job On → duplicate a selected existing Job On for
  the next production"; the typical changes are production number, machine/line, dates, Tool
  selection and manual fields).
- `Machine` is required because the machine is a required Job On fact; the surface pre-fills it from
  the source and the operator explicitly confirms/changes it.
- `ProductionDate` is optional.
- `ExpectedSourceVersion` is the version observed in the preview (§10.2) — the concurrency contract.

### 10.2 Source loading and preview

1. The operator opens the duplication surface for a chosen source `jobon_id`.
2. `PreviewDuplicateAsync(sourceJobOnId)` returns `JobOnResult.DuplicationPreview(SourceJobOnId,
   SourceVersion, Source)` where `Source` is the read-only Job On ficha including its contexts and
   their frozen values. Any historical source may be previewed (no age/machine/reference filter is
   invented; older sources are explicitly legitimate).
3. The preview is **read-only**: opening it writes nothing, touches no version, and creates no
   draft Job On (`JOB_ON.md` §9 historical stability).

### 10.3 Transaction (exact steps)

```text
BEGIN
  1. load the source job_ons row + its context rows
  2. assert row.version == ExpectedSourceVersion      else ROLLBACK -> Refused(StaleVersion)
  3. validate the new Job On facts (reference from source, production_number, machine, date)
     and the production uniqueness rule (reference, production_number)
       -> collision: ROLLBACK -> Refused(DuplicateProduction, existingJobOnId)
  4. allocate a NEW jobon_id and a NEW id for every copied context
  5. INSERT job_ons (copied_from_jobon_id = source jobon_id, version = 1)
  6. for every source context: INSERT the corresponding context row on the NEW jobon_id with
       - tool_id        = the SOURCE CONTEXT's tool_id      (retained, unchanged)
       - tool_type/tool_reference/tool_lot = a FRESH snapshot of the CURRENT canonical Tool row
         read through that tool_id at duplication time (Owner clarification §23; the former
         wording "copied verbatim from the source context; the live Tool is NOT re-read" is
         SUPERSEDED)
  7. COMMIT
```

> **SUPERSEDED (Owner clarification §23):** the original step 6 of this section read "the SOURCE
> CONTEXT's frozen triple (copied verbatim; the live Tool is NOT re-read — JOB_ON.md §8 'copy
> captured/manual values as reviewable starting values')". That wording is superseded by §23: the
> source context contributes only its canonical `tool_id`; the new context's frozen triple is
> always a NEW snapshot of the CURRENT canonical Tool row at duplication time.

### 10.4 Guaranteed outcomes

| Requirement | Guarantee |
|---|---|
| new `jobon_id` | a new uuid, never the source id |
| new context ids | every copied context gets a new id; **no source context id is reused** |
| source unchanged | the source row is only read: its `version`, `updated_at` and every other column are unchanged, and its context rows are untouched. Proven by comparing the full source state before/after. |
| copied Tool choices retained | each copied context carries the same canonical `tool_id` as its source context, unchanged, until the operator explicitly changes it (§7.4) |
| current Tool state captured | each copied context's frozen triple is a NEW snapshot of the CURRENT canonical Tool row read through its `tool_id` at duplication time — never the source context's historical frozen triple (§23 supersedes the former verbatim-copy wording) |
| historical source allowed | any source may be used; older sources are never rejected |
| explicit source relation | `copied_from_jobon_id` records the source (structural lineage, never a revision) |
| transactional | the whole thing is one transaction; a failure at any step leaves neither the new Job On nor any new context row |
| concurrency | a stale `ExpectedSourceVersion` refuses the duplication and creates nothing |
| no ID copying | ids that must be new are allocated new; the `tool_id` is the one identity retained from the source context |

### 10.5 What duplication never does

- never edits, rewrites, re-points or re-versions the source or its contexts
  (`JOB_ON.md` §8 "never reuse the source `jobon_id`", "never reuse the source context IDs");
- never creates a revision identity or a "version of" relation (no `job_on_revision_id`, no
  `production_id`);
- never copies a frozen-only, verification, approval, Controlo, Boquilhas or document fact — P2-T04
  has none of those, and any future one is that module's own duplication concern;
- never auto-selects a source and never auto-fills a production number that was not supplied;
- never clones the SOURCE context's frozen triple into the new context — the new context
  re-snapshots the CURRENT canonical Tool row at duplication time (§23). The former bullet
  "never re-reads the live Tool to 'refresh' the copied triple (§21 Q16 records this pinned
  default)" is **SUPERSEDED** by the Owner clarification §23.

---

## 11. Create / edit / delete transactions

### 11.1 Job On create transaction

```text
DMO.Application.JobOn.CreateJobOnCommand(
    string Reference, string ProductionNumber, string Machine, DateOnly? ProductionDate,
    Guid? CmToolId, Guid? MfToolId, Guid? BqToolId)
```

```text
BEGIN
  1. validate everything (§12.3): facts + machine + the production uniqueness rule
  2. resolve and validate every supplied context Tool id:
       - must exist in tools                     else ValidationFailed(TOOL_NOT_FOUND)
       - tool_type must equal the slot's type     else ValidationFailed(TOOL_TYPE_MISMATCH)
  3. allocate jobon_id (uuid) and an id for every context that will exist
  4. INSERT job_ons (version = 1, copied_from_jobon_id = NULL)
  5. INSERT a context row ONLY for each supplied Tool slot (0..3 rows)
  6. COMMIT
```

Answers to the B1 questions:

| Question | Contract answer |
|---|---|
| When does `jobon_id` become real? | It is allocated by the backend application layer and becomes durable/visible at **COMMIT**. Nothing observes it before that. No client ever supplies or guesses it. |
| When are contexts created? | In the same transaction, only for slots the operator explicitly filled. |
| How are canonical Tools referenced? | By `tool_id`, validated against `tools` inside the transaction; the frozen triple is read from the Tool row at that moment. The Tool row is never modified. |
| What if context creation fails? | The whole transaction rolls back: **no Job On and no context survive**. There is no partial Job On and no half-created context set. |
| What is atomic? | The Job On row plus its required context rows — one unit. |
| What is retry-safe? | A retry after a successful commit is refused with `DuplicateProduction` naming the existing `jobon_id` (no duplicate row, no silent reuse). A retry after a rolled-back commit is a clean, idempotent-in-effect attempt. No client idempotency key is invented (§21 Q17). |
| Optimistic concurrency | Not applicable to create (nothing pre-exists); the created row starts at `version = 1`. Concurrency is resolved by the unique production index. |

### 11.2 Job On update transaction

```text
DMO.Application.JobOn.UpdateJobOnCommand(
    Guid JobOnId, int ExpectedVersion,
    string Reference, string ProductionNumber, string Machine, DateOnly? ProductionDate,
    Guid? CmToolId, Guid? MfToolId, Guid? BqToolId,
    bool DateThresholdWarningAcknowledged)
```

Semantics of a supplied Tool id: `null` means "no guidance for this slot"; the slot's association is
changed/created/removed through the explicit `ToolAssociationChange` list instead of magic nulls, so
that "leave as is" and "remove" are never conflated:

```text
DMO.Application.JobOn.ToolAssociationChange(ToolContextType ContextType, ToolAssociationAction Action, Guid? ToolId)
ToolAssociationAction = Keep | Set | Remove
```

```text
BEGIN
  1. load the Job On row + contexts (tracked)
  2. assert row.version == ExpectedVersion            else ROLLBACK -> Refused(StaleVersion)
  3. validate the new facts + the production uniqueness rule (excluding this jobon_id)
  4. date threshold (§6.6): if row.production_date is not null and <= today and
     !DateThresholdWarningAcknowledged
        -> ROLLBACK -> Refused(DateThresholdConfirmationRequired)
  5. apply the fact changes
  6. apply every ToolAssociationChange:
       Keep   -> no write
       Set    -> resolve Tool; validate type match; insert the context when absent (new id),
                 else update the same row's tool_id + refresh the frozen triple
       Remove -> delete the context row (refused -> Refused(DependencyExists) when a dependent
                 fact references it)
  7. row.version += 1 ; row.updated_at = now()
  8. COMMIT
```

| Rule | Contract |
|---|---|
| Editable facts | reference, production_number, machine, production_date, and the three Tool associations. Nothing else exists to edit. |
| Not editable | the frozen triple as a *field*; `jobon_id`; `copied_from_jobon_id` (lineage is recorded once at duplication and is never rewritten — §21 Q18); `created_at`; `version` (only the backend increments it) |
| Dependent-context effects | a context is created, updated in place, or removed as above, all inside the same transaction; no cascade reaches any other module's data |
| Concurrency | one version assertion, one increment; a stale edit writes nothing |
| Authorization | `job-on-create` (§13) |
| Grouping/reporting | every change is reported in the result so the surface can show exactly what changed; the refresh of a frozen triple happens **only** as the direct consequence of an explicit `Set` |
| Warning behaviour | the threshold is a gate that a human acknowledgement opens; it is never a hard prohibition and never a stored state |
| No lifecycle | no edit changes any status, because no status exists |

### 11.3 Job On read transaction

Reads are read-only: no write, no version bump, no context creation, no implicit completion of the
context set (`BETA_FRONTEND_MODEL.md`: reads never mutate). A read may legitimately show a Job On
with zero contexts.

### 11.4 Context removal inside an edit or a create rollback

- Context removal is possible **only** through an `UpdateJobOnCommand` with `Remove`
  (`JOB_ON.md` §305 correction authority) or as part of a permitted Job On deletion.
- A context removal that a future dependent record references is refused. In P2-T04 the enforcement
  is (a) the dependency probes of §11.5 and (b) the `ON DELETE RESTRICT` foreign keys that the
  dependent tables must declare (`…DELTA.md` §6.4 pattern; `ACCEPTANCE_MATRIX.md` §10).
- No context is ever removed by a read, a list, a duplicate, a Tool change or a Tool deletion.

### 11.5 Delete and the dependency rule

```text
DMO.Application.JobOn.DeleteJobOnCommand(
    Guid JobOnId, int ExpectedVersion,
    bool DeleteConfirmed, bool DateThresholdWarningAcknowledged)
```

```text
BEGIN
  1. load the Job On row + contexts
  2. assert row.version == ExpectedVersion                else -> Refused(StaleVersion)
  3. DeleteConfirmed must be true                          else -> ValidationFailed(DELETE_NOT_CONFIRMED)
  4. date threshold (§6.6): passed date and !acknowledged  else -> Refused(DateThresholdConfirmationRequired)
                                                             (the stronger warning of JOB_ON.md §9A)
  5. run EVERY registered IJobOnDependencyProbe over the Job On + its context ids
       any reported dependency -> ROLLBACK -> Refused(DependencyExists, dependencies)
  6. DELETE the context rows of this Job On              (explicit, ordered, no cascade)
  7. DELETE the job_ons row
  8. COMMIT
```

**The dependency set is fixed as follows.**

| Dependency | Owner | Status in P2-T04 |
|---|---|---|
| `duplication-lineage` — another Job On whose `copied_from_jobon_id` points at this Job On | Job On (this workstream) | **implemented now** by `JobOnLineageDependencyProbe` (the self-FK makes it also DB-enforced) |
| Peso, Pegamentos, Folha, Resumo, Reparação Interna, production-linked Boquilhas, movement/context references, other persisted records linked through Job On contexts | Controlo / Boquilhas / later modules | **not implemented here** — each owning workstream registers its own probe when it creates such a table (`JOB_ON.md` §9A examples list exactly these) |

The seam that lets later modules participate **without P2-T04 hardcoding their domain logic**:

```csharp
namespace DMO.Application.JobOn;

/// <summary>One contributing module's answer to "does anything depend on this Job On?".</summary>
public interface IJobOnDependencyProbe
{
    Task<JobOnDependencyReport> InspectAsync(JobOnDependencyTarget target, CancellationToken cancellationToken);
}

public sealed record JobOnDependencyTarget(
    Guid JobOnId, Guid? CmContextId, Guid? MfContextId, Guid? BqContextId);

public sealed record JobOnDependencyReport(string Source, IReadOnlyList<JobOnDependency> Dependencies)
{
    public static JobOnDependencyReport None(string source);
    public bool HasDependencies { get; }
}

public sealed record JobOnDependency(string Kind, string Description);
```

- The service receives `IEnumerable<IJobOnDependencyProbe>` from DI and evaluates **all** of them;
  any non-empty report refuses the delete.
- A probe reports a *fact*, never a permission and never a rule about another module's data.
- The Job On module never references a Controlo/Boquilhas/document type: the contract is the
  interface above, and the future workstreams supply implementations. `JOB_ON.md` §9A confirms
  "No dependency table is introduced" and "no generic soft-delete/archive/cancelled schema is
  introduced".
- No delete permission beyond the current access authority is invented: deletion is a Job On Create
  action (`ACCESS_MODEL.md` §8 owns Job On edit/delete/correction under `Job On Create`).

**Refusal and destruction rules.**

| Rule | Contract |
|---|---|
| refusal carrier | `JobOnResult.Refused(JobOnRefusalReason.DependencyExists, message, dependencies)` — a typed, actionable refusal naming the dependency kinds; nothing is deleted |
| no cascade | every FK is `RESTRICT`; the only rows a permitted delete removes are **this Job On's own context rows and its own row** |
| never destroyed | no other module's row, no Tool row, no Tool compatibility row, no lineage of another Job On, no measurement, no document, no history |
| atomicity | contexts + Job On are removed in one transaction; a failure at any step leaves the Job On and its contexts intact |
| concurrency | a stale version removes nothing; a concurrent dependency created after the probe is caught by the `RESTRICT` FK and fails the transaction (fail-closed, never a partial delete) |
| confirmation | every delete requires explicit confirmation; the date-threshold case additionally requires the acknowledgement |
| no soft delete | no `deleted_at`, no `active = false`, no archive flag, no cancellation state is introduced (`JOB_ON.md` §9A "No cancellation state") |

## 12. Repository / application interfaces

### 12.1 Layer placement (accepted conventions only)

| Layer | Location | Convention reused |
|---|---|---|
| domain types | `src/DMO.Domain/Tools/`, `src/DMO.Domain/JobOn/` | `DMO.Domain` depends on nothing (first real domain types; `docs/ARCHITECTURE.md`) |
| repository contracts | `src/DMO.Application/Repositories/` | flat `I{Name}Repository` beside `IUserRepository`, `ITemplateRepository`, `ITemplateModuleRepository`, `IAdminAccountRepository` |
| application area | `src/DMO.Application/Tools/`, `src/DMO.Application/JobOn/` | the accepted `{Area}Models.cs` + `{Area}Validator.cs` + `I{Area}Service.cs` + `{Area}Service.cs` pattern |
| persistence | `src/DMO.Infrastructure/Persistence/` (`Entities/`, `EntityConfigurations/`, `{Name}Repository.cs`) | accepted repository/configuration conventions |
| Web | `src/DMO.Web/Endpoints/`, `src/DMO.Web/Pages/JobOn/`, `src/DMO.Web/Pages/Ferramentas/` | accepted minimal-API group + Razor page conventions |

No new .NET project is created (`docs/ARCHITECTURE.md`: "No future project split is pre-authorised").
No generic repository abstraction, no unit-of-work, no mediator, no CQRS infrastructure and no second
`DbContext` is introduced (`docs/ARCHITECTURE.md` "Deliberately not used").

### 12.2 Repository contracts (exact)

```csharp
namespace DMO.Application.Repositories;

public interface IToolRepository
{
    Task<Tool?> GetByIdAsync(Guid toolId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Tool>> SearchAsync(ToolSearchCriteria criteria, CancellationToken cancellationToken);
    Task<Tool?> FindByIdentityAsync(ToolType type, string reference, string lot, CancellationToken cancellationToken);
    Task<Tool> CreatedAsync(Tool tool, IReadOnlyList<MachineCode> compatibleMachines, CancellationToken cancellationToken);
    Task<IReadOnlyList<ToolUsageOccurrence>> ListUsageOccurrencesAsync(Guid toolId, CancellationToken cancellationToken);
}

public interface IJobOnRepository
{
    Task<JobOn?> GetByIdAsync(Guid jobOnId, CancellationToken cancellationToken);
    Task<JobOn?> FindByProductionAsync(string reference, string productionNumber, CancellationToken cancellationToken);
    Task<IReadOnlyList<JobOnProductionListItem>> ListByReferenceAsync(string reference, CancellationToken cancellationToken);
    Task<JobOn> CreatedAsync(JobOn jobOn, IReadOnlyList<ToolContext> contexts, CancellationToken cancellationToken);
    Task<JobOn> UpdatedAsync(JobOn jobOn, IReadOnlyList<ToolContextChange> changes, CancellationToken cancellationToken);
    Task<JobOn> DuplicatedAsync(JobOn duplicate, IReadOnlyList<ToolContext> duplicatedContexts, int expectedSourceVersion, CancellationToken cancellationToken);
    Task DeletedAsync(Guid jobOnId, int expectedVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<JobOnDependency>> ListLineageDependentsAsync(Guid jobOnId, CancellationToken cancellationToken);
}
```

Rules that bind the implementations:

0. the repository-level search criteria record is
   `DMO.Application.Repositories.ToolSearchCriteria(string? Query, ToolType? Type, string? Reference,
   string? Lot, MachineCode? Machine, int Limit)` — the service validates its `ToolSearchQuery` first
   and then projects it into this criteria record, so exactly one query shape exists and there is no
   second validation layer;
1. every member takes `CancellationToken` as its mandatory last parameter (accepted convention);
2. `Task<T?>` for single reads, `Task<IReadOnlyList<T>>` for lists, `Task` for writes;
3. **every write that touches more than one row opens its own transaction**
   (`await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken)`
   … `await transaction.CommitAsync(cancellationToken)`) exactly like `TemplateRepository`. There is
   no ambient/unit-of-work transaction;
4. repositories map entities to application/domain records with a private static `Project(...)`
   (no EF type escapes `DMO.Infrastructure`);
5. repositories map PostgreSQL constraint violations to typed application failures by
   `PostgresException.SqlState` + `ConstraintName` (accepted `TryMapConstraintViolation` pattern),
   using constraint-name constants declared on the entity configuration classes;
6. repositories own no domain rule that belongs to the validator, and services contain no SQL.

### 12.3 Service contracts (exact)

```csharp
namespace DMO.Application.Tools;

public interface IToolService
{
    Task<ToolResult> SearchAsync(ToolSearchQuery query, CancellationToken cancellationToken);
    Task<ToolResult> GetAsync(Guid toolId, CancellationToken cancellationToken);
    Task<ToolResult> CreateAsync(CreateToolCommand command, CancellationToken cancellationToken);
}

namespace DMO.Application.JobOn;

public interface IJobOnService
{
    Task<JobOnResult> FindProductionsAsync(FindProductionsQuery query, CancellationToken cancellationToken);
    Task<JobOnResult> GetAsync(Guid jobOnId, CancellationToken cancellationToken);
    Task<JobOnResult> CreateAsync(CreateJobOnCommand command, CancellationToken cancellationToken);
    Task<JobOnResult> UpdateAsync(UpdateJobOnCommand command, CancellationToken cancellationToken);
    Task<JobOnResult> PreviewDuplicateAsync(Guid sourceJobOnId, CancellationToken cancellationToken);
    Task<JobOnResult> DuplicateAsync(DuplicateJobOnCommand command, CancellationToken cancellationToken);
    Task<JobOnResult> DeleteAsync(DeleteJobOnCommand command, CancellationToken cancellationToken);
}
```

Validator contract (pure static, error list, runs before any write):

```csharp
public static class ToolValidator
{
    public const int MaxLimit = 100;
    public static IReadOnlyList<string> Validate(CreateToolCommand command);
    public static IReadOnlyList<string> Validate(ToolSearchQuery query);
}

public static class JobOnValidator
{
    public static IReadOnlyList<string> Validate(FindProductionsQuery query);
    public static IReadOnlyList<string> Validate(CreateJobOnCommand command);
    public static IReadOnlyList<string> Validate(UpdateJobOnCommand command);
    public static IReadOnlyList<string> Validate(DuplicateJobOnCommand command);
    public static IReadOnlyList<string> Validate(DeleteJobOnCommand command);
}
```

Validation error codes (exact machine-readable strings returned inside `ValidationFailed`; these are
validation inputs, not domain states):

```text
REFERENCE_REQUIRED              PRODUCTION_NUMBER_REQUIRED     MACHINE_REQUIRED
MACHINE_UNKNOWN                 TOOL_TYPE_REQUIRED             TOOL_TYPE_UNKNOWN
PROCESSO_UNKNOWN                QUANTITY_NEGATIVE              MACHINE_COMPATIBILITY_REQUIRED
SEARCH_CRITERIA_REQUIRED        LIMIT_OUT_OF_RANGE             TOOL_NOT_FOUND
TOOL_TYPE_MISMATCH              DELETE_NOT_CONFIRMED           ASSOCIATION_ACTION_INVALID
DUPLICATE_ASSOCIATION_TYPE      SOURCE_JOBON_NOT_FOUND
```

Service rules:

1. the validator runs **first**; a non-empty error list returns `ValidationFailed` with no write;
2. contextual Tool resolution (existence + type match) happens inside the transaction (repository),
   so a race cannot bypass it;
3. `ConcurrencyConflictException` from a repository is translated to `Refused(StaleVersion)` — never
   surfaced as a 500 and never retried silently;
4. `NotFound` is returned for a missing Job On/Tool on a read or a mutation target;
5. services hold no `HttpContext`, no route knowledge and no presentation type: the Web layer maps
   results to transports (§13/§14);
6. services never read another module's tables and never reference a future-module type.

### 12.4 Dependency-probe implementation

| Implementation | Where | Reports |
|---|---|---|
| `JobOnLineageDependencyProbe : IJobOnDependencyProbe` | `src/DMO.Infrastructure/Persistence/` | `duplication-lineage` when another `job_ons` row has `copied_from_jobon_id` = the target |
| future Peso / Pegamentos / Folha / Resumo / Boquilhas / document probes | their own workstreams | their own dependency kinds |

Registered in `Program.cs` with
`builder.Services.AddScoped<IJobOnDependencyProbe, JobOnLineageDependencyProbe>();` — one additive
line per probe, so the delete flow automatically includes every registered module without P2-T04
knowing its domain.

### 12.5 DI registrations added by P2-T04 (complete list, additive)

`src/DMO.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IToolRepository, ToolRepository>();
services.AddScoped<IJobOnRepository, JobOnRepository>();
services.AddScoped<IJobOnDependencyProbe, JobOnLineageDependencyProbe>();
```

`src/DMO.Web/Program.cs`:

```csharp
builder.Services.AddScoped<IToolService, ToolService>();
builder.Services.AddScoped<IJobOnService, JobOnService>();
…
app.MapJobOnEndpoints();
app.MapFerramentasEndpoints();
```

These are the **only** composition changes. P2-T04 adds no policy, no module definition, no
availability entry, no destination route and no second registry.

---

## 13. Endpoint / route / policy matrix

### 13.1 Base paths

```text
Job On       /jobon            (Razor pages + minimal-API JSON endpoints)
Ferramentas  /ferramentas      (contextual only; JSON endpoints + one contextual ficha page)
```

`job-on` is the accepted `DestinationId` of `ModuleCatalog.JobOnView`/`ModuleCatalog.JobOnCreate`;
`/jobon` follows the master plan §9 route plan ("`/jobon` (or the accepted contract path)").
`Ferramentas` has `DestinationId == null` in the catalog and can therefore never become a
destination; P2-T04's `/ferramentas` routes are contextual surfaces reachable only from an
operational context, matching the global contract's conceptual `/ferramentas/{tool_id}` ficha
(`FERRAMENTAS.md` §2/§4).

### 13.2 Complete P2-T04 route/endpoint table

Every route declares **exactly one** policy through
`ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.<id>)`. No route declares two policies, no
route is anonymous, and no new policy is created (`ModuleAuthorizationPolicies` is untouched).

| # | Route | Kind | Purpose | Policy | Request carrier | Result carrier | Failure states |
|---|---|---|---|---|---|---|---|
| 1 | `GET /jobon` | page `Pages/JobOn/Index` | Job On consult: reference → productions, explicit selection, open a Job On | `dmo.module.job-on-view` | `?reference=` | rendered page (`JobOnProductionListItem[]`, empty / lookup-failed / permission-denied states) | validation failure (blank reference submitted), empty, lookup-failed, permission-denied |
| 2 | `GET /jobon/productions` | minimal API | reference → productions query for the consult surface | `dmo.module.job-on-view` | `?reference=` | `JobOnProductionListResponse` | 400 (`REFERENCE_REQUIRED`), 403, 200 empty |
| 3 | `GET /jobon/{jobonId:guid}` | page `Pages/JobOn/View` | read-only Job On sheet (occurrence + existing contexts with frozen values) | `dmo.module.job-on-view` | route `jobonId` | rendered page | 404, 403 |
| 4 | `GET /jobon/create` | page `Pages/JobOn/Create` | Job On create surface (simplified facts + optional CM/MF/BQ) | `dmo.module.job-on-create` | `?sourceReference=` (prefill only) | rendered page | 400, 403 |
| 5 | `GET /jobon/create/productions` | minimal API | reference → productions query **for the Create surface** (existing-production detection and duplicate-source choice) | `dmo.module.job-on-create` | `?reference=` | `JobOnProductionListResponse` | 400 (`REFERENCE_REQUIRED`), 403 |
| 6 | `POST /jobon` | minimal API | create the Job On (+ only the needed contexts) transactionally | `dmo.module.job-on-create` | `CreateJobOnRequest` | 201 `JobOnCreatedResponse` (`jobonId`, `version`) | 400 `validation-failed`, 409 `duplicate-production`, 403 |
| 7 | `GET /jobon/{jobonId:guid}/edit` | page `Pages/JobOn/Edit` | edit surface (facts + Tool associations) | `dmo.module.job-on-create` | route `jobonId` | rendered page | 404, 403 |
| 8 | `PUT /jobon/{jobonId:guid}` | minimal API | edit facts/associations + date-threshold gate | `dmo.module.job-on-create` | `UpdateJobOnRequest` | 200 `JobOnUpdatedResponse` (`jobonId`, `version`) | 400 `validation-failed`, 404, 409 `stale-version` \| `duplicate-production` \| `date-threshold-confirmation-required` \| `dependency-exists`, 403 |
| 9 | `GET /jobon/{jobonId:guid}/duplicate` | page `Pages/JobOn/Duplicate` | duplication preview + explicit source confirmation | `dmo.module.job-on-create` | route `jobonId` | rendered page (source ficha + `sourceVersion`) | 404, 403 |
| 10 | `POST /jobon/{jobonId:guid}/duplicate` | minimal API | duplication transaction | `dmo.module.job-on-create` | `DuplicateJobOnRequest` (`expectedSourceVersion`, new facts) | 201 `JobOnDuplicatedResponse` (`jobonId`, `sourceJobOnId`, `version`) | 400 `validation-failed`, 404, 409 `stale-version` \| `duplicate-production`, 403 |
| 11 | `DELETE /jobon/{jobonId:guid}` | minimal API | delete with confirmation + dependency rule | `dmo.module.job-on-create` | `?expectedVersion=&deleteConfirmed=&dateThresholdAcknowledged=` | 204 (no content) | 400 `validation-failed`, 404, 409 `stale-version` \| `dependency-exists` \| `date-threshold-confirmation-required`, 403 |
| 12 | `GET /ferramentas/tools` | minimal API | canonical Tool search/list (shared Tool orchestration) | `dmo.module.ferramentas` | `?query=&type=&reference=&lot=&machine=&limit=` | `ToolSearchResponse` (`items[]`, `limit`) | 400 `validation-failed`, 403 |
| 13 | `POST /ferramentas/tools` | minimal API | create the canonical Tool (returns the canonical `tool_id`) | `dmo.module.ferramentas` | `CreateToolRequest` | 201 `ToolCreatedResponse` (`toolId`) | 400 `validation-failed`, 409 `duplicate-identity`, 403 |
| 14 | `GET /ferramentas/tools/{toolId:guid}` | page `Pages/Ferramentas/Tool` | contextual Tool ficha (read-only) | `dmo.module.ferramentas` | route `toolId` | rendered page | 404, 403 |

**Route-count statement:** P2-T04 defines exactly these 14 routes. No other route, page, endpoint,
Hub, route registration, navigation entry or top-level destination is created. No standalone Tool
create page exists (§21 Q8).

### 13.3 Why the Create surface has its own productions read (route 5)

`ACCESS_MODEL.md` §8: `Job On Create` grants "the Job On creation/edit/management/delete surface
**and the reads technically required to operate it**", while `Job On View` is a separate assignable
module. The frozen policy mechanism is exactly one `ModuleAuthorizationRequirement` per policy
(`ModuleAuthorizationHandler` resolves `IModuleAccessService.HasModuleAsync` for the single required
module); there is **no** OR-policy, no implication rule and no "Create implies View" behaviour —
`NoProfilesRegressionTests.SiblingModule_SameDestination_DoesNotSatisfyGate` proves the sibling does
not satisfy the gate. Therefore:

- the reads the Create workflow needs are provided **under the Create policy** (route 5, and the
  `/jobon/create|edit|duplicate` pages, whose own data loads are gated by the page policy);
- the consult reads stay under the View policy (routes 1–3);
- no route is shared between two policies, no policy is modified, and no create capability is placed
  on a View route.

Recorded as §21 Q19 with the alternative (an authorized change to the access foundation) and why it
is not taken here.

### 13.4 Policy ownership, explicitly

| Policy | Owns in P2-T04 | Never gains |
|---|---|---|
| `dmo.module.job-on-view` | routes 1, 2, 3 — read/consult only | any create/edit/duplicate/delete capability. Job On View does **not** gain Create because it shares the `job-on` destination (`ACCESS_MODEL.md` §8; `JOB_ON_LIGHT.md` "Access"). Its one bounded global write action (confirming an existing verification occurrence) is **not implemented** in P2-T04 because verification is explicitly outside Beta Job On Light scope, so P2-T04's View surface is purely read. |
| `dmo.module.job-on-create` | routes 4–11 (the mutating surface plus the reads it needs) | no Approve/verification/Controlo/Boquilhas capability |
| `dmo.module.ferramentas` | routes 12, 13, 14 — contextual Tool search/list/detail/create | no top-level destination (§13.6); no approval capability |
| `dmo.module.ferramentas-approve` | **nothing in P2-T04** | P2-T04 implements no Tool approval/review/direct-edit capability, so no route carries this policy. It remains a valid canonical identity with its generated policy, unchanged (§21 Q20). |

`ADMIN` gains **no** operational access: `ModuleAuthorizationHandler` fails closed for a non-USER
current account (`current is not CurrentAccount.User(...)` ⇒ `context.Fail()`), so an ADMIN session is
denied on every P2-T04 route while keeping the ADMIN-only administration surfaces
(`ACCESS_MODEL.md` §1/§14; `ADMIN.md`).

### 13.5 Interim runtime state (mandatory, and easy to get wrong)

```text
ModuleRegistrations.CurrentBuildAvailable = []            (unchanged by P2-T04)
EmptyDestinationRouteRegistry                             (unchanged; production registration)
DestinationRouteRegistrations                             (unchanged; still empty)
```

Consequences the implementation and its tests must respect:

1. `AccessResolver` denies a **not-available** module (`AccessDeniedReason.UnavailableModule`), so
   even a user whose Template grants `Job On Create` is denied every P2-T04 route **until P2-T10
   registers the Module as available**. This is the intended interim state of the master plan
   sequence (P2-T04 = step 6, P2-T10a = step 7): P2-T04 makes the surface exist; P2-T10 makes it
   reachable.
2. P2-T04 therefore adds **no** operational route registration and **no** availability entry, and no
   navigation entry appears (`NavigationProjectionService` intersects granted ∩ available ∩
   non-contextual ∩ routed).
3. P2-T04 integration tests exercise the gates with **test-only** module registries
   (`ModuleRegistry.Create(...)` from controlled test definitions, mirroring the accepted
   `TestModuleDefinitions`/`TestNavigationComposition` pattern), never by editing production
   availability.
4. The regression "`CurrentBuildAvailable` stays `[]`" remains green (accepted
   `P2T03RegressionTests.RG1` precedent).

### 13.6 Routing boundary — P2-T04 vs P2-T10

| Concern | P2-T04 | P2-T10 |
|---|---|---|
| the physical page/endpoint exists and is server-gated | **YES** | — |
| `ModuleRegistrations.CurrentBuildAvailable` | **unchanged (`[]`)** | registers `job-on-*` only when the surface is real and usable |
| `IDestinationRouteRegistry` implementation / `DestinationRouteRegistrations` | **unchanged** | registers the real `/jobon` destination route through the single existing seam |
| navigation exposure | **none** | appears only when granted ∧ available ∧ non-contextual ∧ routed |
| Ferramentas top-level destination | **never** | **never** (`DestinationId` is `null` in the catalog) |

A physical Razor page existing does **not** make a module available, does not register a destination
and does not create a navigation entry. Nothing in P2-T04 may touch `ModuleCatalog`,
`ModuleRegistrations`, `DestinationRoutes`, `DestinationRouteRegistrations` or
`NavigationProjectionService`.

### 13.7 Razor page authorization declaration (exact accepted form)

```csharp
using DMO.Application.Access;
using DMO.Web.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace DMO.Web.Pages.JobOn;

/// <summary>Job On consult surface — read only (Job On View).</summary>
[Authorize(Policy = ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.JobOnView))]
public sealed class IndexModel : PageModel { … }
```

Every P2-T04 page carries exactly one such attribute; every P2-T04 endpoint group carries
`.RequireAuthorization(ModuleAuthorizationPolicies.PolicyName(...))`. This is the first production
use of the Module policies (today only `DirectRouteEnforcementTests` exercises them); the mechanism
is accepted and unchanged.

---

## 14. Failure / result vocabulary

### 14.1 Result unions (closed sets — the accepted outcome shape)

```csharp
namespace DMO.Application.Tools;

public abstract record ToolResult
{
    public sealed record SearchResults(IReadOnlyList<ToolSearchItem> Items) : ToolResult;
    public sealed record Found(ToolFicha Ficha) : ToolResult;
    public sealed record Created(Guid ToolId) : ToolResult;
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : ToolResult;
    public sealed record NotFound(Guid ToolId) : ToolResult;
    public sealed record DuplicateIdentity(Guid ExistingToolId, string Message) : ToolResult;
}

namespace DMO.Application.JobOn;

public abstract record JobOnResult
{
    public sealed record ProductionsFound(IReadOnlyList<JobOnProductionListItem> Productions) : JobOnResult;
    public sealed record Ficha(JobOnFicha Ficha) : JobOnResult;
    public sealed record DuplicationPreview(Guid SourceJobOnId, int SourceVersion, JobOnFicha Source) : JobOnResult;
    public sealed record Created(Guid JobOnId, int Version) : JobOnResult;
    public sealed record Updated(Guid JobOnId, int Version) : JobOnResult;
    public sealed record Duplicated(Guid JobOnId, Guid SourceJobOnId, int Version) : JobOnResult;
    public sealed record Deleted(Guid JobOnId) : JobOnResult;
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : JobOnResult;
    public sealed record NotFound(Guid JobOnId) : JobOnResult;
    public sealed record Refused(
        JobOnRefusalReason Reason,
        string Message,
        Guid? ExistingJobOnId = null,
        IReadOnlyList<JobOnDependency>? Dependencies = null) : JobOnResult;
}

public enum JobOnRefusalReason
{
    StaleVersion,
    DuplicateProduction,
    DependencyExists,
    DateThresholdConfirmationRequired,
}
```

No result type carries a lifecycle status, an availability state, a document state, a Controlo or
Boquilhas fact, a permission decision or a navigation target.

### 14.2 Domain reason tokens (exact transport strings)

| Reason token | Raised by | Meaning |
|---|---|---|
| `stale-version` | edit, duplicate (source), delete | the row changed after the operator observed it; nothing written |
| `duplicate-production` | create, duplicate, edit (renumbering onto an existing pair) | another Job On already owns this (`reference`, `production_number`); `existingJobOnId` names it |
| `duplicate-identity` | Tool create (application check + unique-index race) | a canonical Tool with this (`type`, `reference`, `lot`) already exists; `existingToolId` names it |
| `dependency-exists` | delete, context removal | dependent operational facts exist; `dependencies[]` names the contributing kinds; nothing deleted |
| `date-threshold-confirmation-required` | edit, delete | the Job On concerns a production date already reached/passed; explicit acknowledgement required — a **warning gate**, never a hard prohibition |
| `validation-failed` | every mutation/query | the `errors[]` list carries the §12.3 codes |
| `not-found` | reads and mutation targets | the Job On/Tool does not exist |

### 14.3 Transport mapping (exact, mirroring the accepted endpoint pattern)

| Result | HTTP | Body |
|---|---|---|
| `SearchResults` | 200 | `{ "items": [ … ], "limit": n }` |
| `ProductionsFound` | 200 | `{ "productions": [ … ] }` |
| `Found` / `Ficha` / `DuplicationPreview` | 200 | the corresponding response shape |
| `Created` (Job On) | 201 | `{ "jobonId": "…", "version": 1 }` |
| `Created` (Tool) | 201 | `{ "toolId": "…" }` |
| `Updated` | 200 | `{ "jobonId": "…", "version": n }` |
| `Duplicated` | 201 | `{ "jobonId": "…", "sourceJobOnId": "…", "version": 1 }` |
| `Deleted` | 204 | (empty) |
| `ValidationFailed` | 400 | `{ "reason": "validation-failed", "errors": [ … ] }` |
| `NotFound` | 404 | `{ "reason": "not-found" }` |
| `DuplicateIdentity` | 409 | `{ "reason": "duplicate-identity", "message": "…", "existingToolId": "…" }` |
| `Refused(StaleVersion)` | 409 | `{ "reason": "stale-version", "message": "…" }` |
| `Refused(DuplicateProduction)` | 409 | `{ "reason": "duplicate-production", "message": "…", "existingJobOnId": "…" }` |
| `Refused(DependencyExists)` | 409 | `{ "reason": "dependency-exists", "message": "…", "dependencies": [ { "kind": "…", "description": "…" } ] }` |
| `Refused(DateThresholdConfirmationRequired)` | 409 | `{ "reason": "date-threshold-confirmation-required", "message": "…" }` |
| anonymous caller | 401 | (policy challenge) |
| authenticated but not granted, or module not available | 403 | (policy forbid) |
| unexpected infrastructure failure | 500 | generic; logged at status level only, never a payload |

Rules:

- a denial is **never** rendered as an empty list, an empty page or a silent success
  (`SHARED_FRONTEND_CONTRACT_FREEZE.md` §4; `ACCEPTANCE_MATRIX.md` §9);
- a lookup failure is never mapped to `empty`;
- `409` is the single status for every "the current persisted state refuses this write", distinguished
  by the reason token; the write is never partially applied in any of those cases;
- no error body ever contains a connection string, a filesystem path, a secret or another user's data
  (accepted observability rule: status-level logging only);
- the Web layer performs no domain decision: it switches on the closed result set.

### 14.4 Presentation state mapping (consumer side)

| Outcome | Presented shared state (P2-T01 / P2-T03 vocabulary) |
|---|---|
| request in flight | `loading` |
| rows/candidates present | `ready` |
| zero rows/candidates | `empty` (explicit no-results; the ToolPicker may then offer `Criar ferramenta` **iff** supplied and enabled) |
| infrastructure failure | `lookup-failed` (+ supplied retry) |
| module/service unavailable | `unavailable` |
| 403 | `permission-denied` (never a blank surface) |
| mutation in flight | `saving` / `submitting` |
| 409 `stale-version` | `conflict` (never a silent retry) |
| 409 `date-threshold-confirmation-required` | warning + confirmation, then retry with acknowledgement |
| 409 `dependency-exists` | an actionable refusal naming the dependency kinds |
| 409 `duplicate-production` / `duplicate-identity` | an actionable refusal offering explicit selection of the existing record |

The mapping is presentation; it never alters the backend decision and never substitutes for the
server-side gate.

---

## 15. Concurrency

### 15.1 Mechanism (accepted, reused — not re-invented)

- `job_ons.version integer NOT NULL DEFAULT 1`, configured `.IsConcurrencyToken()`;
- write primitives compare the observed version inside the transaction and reject a stale write with
  `ConcurrencyConflictException` (domain type, `DMO.Application.Persistence`);
- the EF token stays active so a race between the compare and the save still surfaces as a typed
  conflict through `ConcurrencyConflictExceptionMapping.ToDomainConflict`, never a silent overwrite;
- services translate `ConcurrencyConflictException` into `Refused(StaleVersion)` → HTTP 409.

### 15.2 Per-operation matrix

| Operation | Observed version | On staleness | Rows written on staleness |
|---|---|---|---|
| Job On create | n/a (nothing pre-exists) | collision on the production unique index ⇒ `DuplicateProduction` | 0 |
| Job On edit | `UpdateJobOnCommand.ExpectedVersion` | `Refused(StaleVersion)` | 0 |
| Job On duplicate | `DuplicateJobOnCommand.ExpectedSourceVersion` (observed in the preview) | `Refused(StaleVersion)` | 0 (no new Job On, no contexts) |
| Job On delete | `DeleteJobOnCommand.ExpectedVersion` | `Refused(StaleVersion)` | 0 |
| Tool create | n/a | unique violation ⇒ `DuplicateIdentity` | 0 |
| Tool update | **does not exist** | — | — |
| context write | inherits the enclosing Job On version | the whole edit is refused | 0 |

### 15.3 Rules

1. **No silent retry, no last-write-wins, no automatic merge.** A refused mutation reports the typed
   reason and the surface reloads (`BETA_INTEGRATION_SEAMS.md` "Concurrency and stale data").
2. **One version increment per committed Job On mutation.** Create ⇒ `version = 1`; an edit changes
   it exactly once; a read never bumps it; duplication creates a new row at `version = 1` and does
   not touch the source version.
3. **The duplicate source is version-guarded**: the operator confirms exactly the source state they
   previewed (`BACKEND_FRONTEND_MODEL.md`: "concurrency/version checks" are backend-owned).
4. **Contexts are protected by the parent.** Context rows carry no version; the Job On version
   protects every context write in the same transaction (accepted `template_modules` reasoning).
5. **Tool concurrency is the unique index.** The Tool is immutable in P2-T04, so no version column
   exists; two concurrent creates of the same identity resolve deterministically to one Tool plus one
   `DuplicateIdentity` refusal. The first Tool-mutating workstream (change-request / direct edit) owns
   adding its own versioned primitive to `tools`.
6. **Deleted-row races** are reported as `NotFound`/`StaleVersion`, never as success.

### 15.4 Isolation

One `BEGIN`/`COMMIT` per write through `DmoDbContext.Database.BeginTransactionAsync` at the
repository level; no explicit isolation-level change, no advisory lock, no distributed lock and no
outbox. The uniqueness constraints and the `RESTRICT` foreign keys are the cross-transaction
guarantees.

---

## 16. Migration contract

### 16.1 One migration, one owner

P2-T04 owns **exactly one new migration**:

```text
src/DMO.Infrastructure/Migrations/<UTC timestamp>_ToolJobOnDomainCore.cs
src/DMO.Infrastructure/Migrations/<UTC timestamp>_ToolJobOnDomainCore.Designer.cs
```

- EF-generated (`dotnet ef migrations add ToolJobOnDomainCore` through the existing
  `DesignTimeDmoDbContextFactory`), not hand-written;
- namespace `DMO.Infrastructure.Migrations`, `public partial class ToolJobOnDomainCore : Migration`,
  the accepted `#nullable disable` + block-scoped-namespace shape of migrations 001/002;
- `DmoDbContextModelSnapshot.cs` is extended by EF as part of generating that migration (it snapshots
  the whole model and necessarily records the new tables) — this is the only modification of an
  existing persistence file, and it is EF-owned and mechanical.

### 16.2 Expected schema delta (exact)

Created: **6 tables** — `tools`, `tool_machines`, `job_ons`, `cm_contexts`, `mf_contexts`,
`bq_contexts` — with exactly the columns, nullability, defaults, checks, unique constraints, foreign
keys (all `RESTRICT`) and indexes of §3 and §4.

Not created: any other table, any seed/reference row, any trigger, any function, any view, any
extension, any sequence, any PostgreSQL enum type, any RLS policy, any `__EFMigrationsHistory`
manipulation and any column not listed in §3.

### 16.3 Safety rules

1. **Migrations 001 and 002 are never edited.** Their `.cs`/`.Designer.cs` files are byte-identical
   after P2-T04 (regression-asserted).
2. **No data-destroying operation**: no `DROP TABLE`, no `DROP COLUMN`, no `TRUNCATE`, no schema
   reset, no `EnsureDeleted`, no type change on an existing column, no rename. The migration is
   purely additive.
3. **No existing table/column is altered**, so it is safe to apply to a database already holding
   foundation data.
4. Applying the migration twice is a no-op (EF migration history); the migration runner (`--migrate`)
   is unchanged.
5. Applying the revision to the disposable PostgreSQL test database is part of P2-T04 verification;
   applying it to Supabase TEST is the operator's normal `--migrate` deployment step and is **not**
   performed or claimed by this authoring task.

### 16.4 Rollback expectations

`Down(MigrationBuilder migrationBuilder)` drops exactly the six created tables in reverse dependency
order:

```text
bq_contexts, mf_contexts, cm_contexts, job_ons, tool_machines, tools
```

- `Down` is the exact inverse of `Up` (EF-generated), so
  `dotnet ef database update <previous migration>` restores the pre-P2-T04 schema without touching
  foundation tables;
- no `Down` path deletes foundation data and no `Down` path performs a schema reset;
- rollback is a schema operation only; no application code calls `Down`.

### 16.5 `DmoDbContext.cs` is deliberately NOT modified

The protected register names `DmoDbContext.cs` as persistence foundation (master plan §12 row 10) and
its named seam is "new migrations are new files". EF discovers configurations automatically through
`ApplyConfigurationsFromAssembly(typeof(DmoDbContext).Assembly)`, so the new entities are mapped
**without** touching the context; the new repositories therefore obtain their sets with
`_context.Set<TEntity>()`.

Consequences, stated explicitly so review is unambiguous:

- `DmoDbContext.cs` remains byte-identical (regression-asserted);
- no `DbSet` property is added — the accepted `DbSet` convention stays intact for the foundation
  entities and is not semantically violated, because the context still owns exactly one model;
- if the Architect prefers explicit `DbSet` properties on the context, that is a contract amendment
  (an additive edit to a protected file) and must be authorized, not assumed.

## 17. Explicit non-scope

### 17.1 Never implemented by P2-T04 (authority-excluded)

| Excluded | Authority |
|---|---|
| full Job On lifecycle: revisions, status machine, verification catalogue/occurrences, family sheets/manual blocks, print orchestration | `JOB_ON_LIGHT.md` "Explicitly outside Beta"; handoff §6; `RECORD_LIFECYCLES.md` §1–§2 |
| full Ferramentas lifecycle: change requests, approval workflow, technical-condition dossier/history, utilisation history, Armazém location/movement ownership, maintenance experience | `FERRAMENTAS_LIGHT.md` "Explicitly outside Beta"; `RECORD_LIFECYCLES.md` §10 |
| repairers, the repairer register, machine→repairer assignment, repairer resolution, historical repairer preservation | `…DELTA.md` §3–§6; owned by `Controlo_Create → Definições` (P2-T05) and consumed by Boquilhas (P2-T07); the P2-T04 handoff §4 "does not model repairers or the machine-to-repairer assignment" |
| "Linha B"/"Linha C" grouping, shared B or C assignment, any machine grouping/inheritance/cascade | `…DELTA.md` §4.2 |
| a machine registry, a `machine_id` scheme, a machine table, machine lifecycle | `…DELTA.md` §4.4; handoff §3 |
| `Controlo_Create → Definições` and every operational setting (PDF directory, email lists, email templates) | `…DELTA.md` §1; P2-T05 |
| Controlo calculations, Peso, Comparação, Pegamentos, Folha, Resumo, shared Peso read model | P2-T05/P2-T06; `JOB_ON_LIGHT.md` |
| Boquilhas aggregate, movements, balance, close/reopen, History, BQ external-repair quantity | P2-T07 |
| documents/PDF generation, naming, directory resolution, availability states, email sending | P2-T08 |
| secondary navigation and current-destination wiring | P2-T09 |
| module availability registration, real destination registration, navigation exposure, cross-module links, end-to-end integration | P2-T10; master plan §13 |
| HISTÓRICO GLOBAL (`historia`), Armazém, Reparação Interna/Programada, Tampões, Admin audit | master plan §3.1, §14.1; `BETA_SCOPE.md` |
| a generic lifecycle/state engine, a document table for symmetry, an audit table for symmetry, a dependency table, a soft-delete/archive/cancellation schema | `RECORD_LIFECYCLES.md` §1; `JOB_ON.md` §9A; `ACCEPTANCE_MATRIX.md` §10 |
| `production_id`, `job_on_revision_id`, reverse-ID arrays, fake `cm_id`/`mf_id`/`bq_id`/`jobon_id`, module-private Tool identities | `IDENTITIES_AND_RELATIONSHIPS.md`; handoff §6 |
| mobile/tablet/touch layouts, breakpoint reflow, card conversion, required-column hiding, action relocation | master plan "DMO FIXED DESKTOP LAYOUT POLICY"; A1 freeze §1A |

### 17.2 Protected files P2-T04 must not modify

```text
src/DMO.Application/Access/ModuleCatalog.cs
src/DMO.Application/Access/ModuleRegistrations.cs
src/DMO.Application/Access/{ModuleRegistry,AccessResolver,AccessOutcome,ModuleResolve,ModuleAccessService}.cs
src/DMO.Web/Authorization/{ModuleAuthorizationPolicies,ModuleAuthorizationHandler,ModuleAuthorizationRequirement}.cs
src/DMO.Web/Authorization/AdministrationAuthorizationPolicies.cs
src/DMO.Web/Auth/*, src/DMO.Web/Startup/*, src/DMO.Application/Accounts/*, src/DMO.Application/Session/*
src/DMO.Infrastructure/Persistence/DmoDbContext.cs
src/DMO.Infrastructure/Migrations/20260922001736_*, src/DMO.Infrastructure/Migrations/20260922001757_*
src/DMO.Infrastructure/Persistence/Entities/{Template,User,AdminAccount,TemplateModule}Entity.cs
src/DMO.Infrastructure/Persistence/EntityConfigurations/{Template,User,AdminAccount,TemplateModule}*Configuration.cs
src/DMO.Infrastructure/Persistence/{UserRepository,TemplateRepository,TemplateModuleRepository,AdminAccountRepository}.cs
src/DMO.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs   ← additive registration lines ONLY
src/DMO.Web/Frontend/Shell/{DestinationRoutes,NavigationProjectionService}.cs
src/DMO.Web/Navigation/DestinationRouteRegistrations.cs
src/DMO.Web/Frontend/Shell/{ShellPresentationModels,ShellPresentationService}.cs
src/DMO.Web/Frontend/Shared/SharedFrontendExtensions.cs
src/DMO.Web/Frontend/Shared/Contracts/**                       ← every P2-T01/P2-T02/P2-T03 contract type
src/DMO.Web/Pages/Shared/**                                    ← including every shared component partial
src/DMO.Web/wwwroot/css/{dmo-tokens,dmo-shell,dmo-user-shell,dmo-components,dmo-admin-users,dmo-admin-templates}.css
src/DMO.Web/wwwroot/js/{dmo-focus,dmo-dense-table,dmo-tool-picker,dmo-measurement-rows}.js
src/DMO.Web/Pages/{Index,Login,AccessDenied}.*, src/DMO.Web/Pages/Administration/**
docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md
plans/contracts/P2-T02_*.md, plans/contracts/P2-T03_*.md
tests/**                                                       ← existing tests: keep passing, never weaken
```

The only accepted non-new files P2-T04 changes are: `src/DMO.Web/Program.cs` (two service
registrations, two `Map…Endpoints()` lines), `PersistenceServiceCollectionExtensions.cs` (two
repository + one probe registration lines), `DmoDbContextModelSnapshot.cs` (EF-generated snapshot
extension) and the planning/governance documents listed in Appendix D.

### 17.3 No Ferramentas top-level destination, ever

P2-T04 registers no `DestinationId` for Ferramentas, adds no navigation entry for it, and does not
change `ModuleCatalog` (`ferramentas`/`ferramentas-approve` keep `DestinationId == null`). The
contextual ficha page is reachable only from an operational context and only with the Ferramentas
grant (`FERRAMENTAS_LIGHT.md` "Contextual, not top-level"; `ACCESS_MODEL.md` §11;
`ACCEPTANCE_MATRIX.md` §9).

### 17.4 No P2-T05 or later leakage

No P2-T04 artifact names, imports, renders, fixtures or tests a Definições surface, repairer,
machine-assignment, PDF directory, email list/template, Peso, Comparação, Pegamentos, Folha, Resumo,
measurement field, Boquilhas aggregate/movement, document availability state, secondary navigation,
module availability or HISTÓRICO GLOBAL concept.

---

## 18. P2-T03 composition

### 18.1 Consumed primitives (unchanged)

| P2-T03 type | Consumed for | Mapping rule |
|---|---|---|
| `ToolPickerPresentation` | the Tool search/select surface on the Job On create/edit forms | `State` ← the §14.4 outcome mapping; `RegionLabel` ← a P2-T04-owned Portuguese label; `Query` ← the controlled search input; `Candidates` ← one `ToolPickerCandidatePresentation` per `ToolSearchItem` |
| `ToolPickerCandidatePresentation` | one candidate per canonical Tool | `Key` = **opaque** adapter key (never `tool_id` as a declared identity); `AccessibleContext` = supplied human context (e.g. "CM 5447T173 lote 12"); `Facts` = supplied `ToolPickerFactPresentation` items for type/reference/lot/machines/quantity/processo |
| `ToolPickerFactPresentation` | labelled Tool facts in candidates and origin context | `Label`/`Value` supplied verbatim; an absent fact is simply not supplied (never `—`, `0`, `false`) |
| `ToolPickerPresentation.OriginToken` | origin correlation for the picker subflow | opaque; **never** parsed, never declared a canonical identity, never embedded in a URL/route/path |
| `ToolPickerPresentation.CreateAction` | the `Criar ferramenta` affordance | supplied visible+enabled only when the caller holds the Ferramentas capability for the contract and the state is `Ready`/`Empty` |
| `ToolPickerPresentation.ExpectedTypeLabel` | the expected Tool type of the slot | supplied by P2-T04 from the context type (CM/MF/BQ) |
| `ToolSummaryRowPresentation` | compact rendering of the currently associated Tool on a Job On surface | `ItemKey` opaque; facts supplied from the Tool read; `Quantity` omitted when `null`; `Status` **not supplied** (P2-T04 has no Tool status) |
| `ToolSummaryFactPresentation` | the Tool facts inside a summary row | supplied verbatim |
| `DecisionBarPresentation` | form actions (Guardar / Duplicar / Eliminar / Cancelar) | actions supplied by P2-T04 with availability + disabled reasons; **no** domain transition semantics encoded |
| `CommonState` + `CommonStateRegionPresentation` (+ `_CommonStateRegion` partial) | every non-`ready` surface (loading/empty/lookup-failed/unavailable/permission-denied/saving/submitting/conflict) | delegated; P2-T04 renders **no** competing state surface |
| `RecordStatusPresentation` | supplied status text where a *supplied* status genuinely exists | not used to invent a Job On status (there is none) |
| `DenseTablePresentation` (+ `_DenseDataTable`) | the reference → productions list and the Tool search results list **on pages** | rows/keys supplied by P2-T04; the component fetches nothing |
| `AuditTrailPresentation` | **not used** — P2-T04 has no audit facts | — |
| `AvailabilityState` | **not used** — document availability is P2-T08 | — |
| `MeasurementRows*` | **not used** — measurements are P2-T05 | — |

### 18.2 Composition rules (binding)

1. **P2-T03 contracts and artifacts are not modified.** No shared contract type, partial, CSS block,
   asset, interaction model or test is changed by P2-T04 (`P2-T03 … CONTRACT` Appendix A.1/A.3;
   `WORKFLOW.md` cross-stream change protocol).
2. **Domain orchestration sits outside the shared primitives.** P2-T04 owns: the search query, the
   candidate mapping, the opaque↔canonical mapping, Tool creation, association persistence, origin
   state, return orchestration and every Job On decision. The shared components remain
   presentation/mechanics only.
3. **Opaque keys stay opaque** (`SHARED_FRONTEND_CONTRACT_FREEZE.md` §2; P2-T03 §2.3): no P2-T04
   contract type, adapter member, fixture, DOM hook or URL declares an opaque key to be `tool_id`,
   `jobon_id`, `cm_id`, `mf_id` or `bq_id`.
4. **No look-alike components.** P2-T04 creates no second picker, no second summary row, no second
   table, no second state region, no second action bar, no second focus helper and no second asset
   include helper. It consumes the accepted partials through
   `<partial name="_SharedComponentAssets" />` and the component partials.
5. **P2-T04 introduces its own stylesheet** (`src/DMO.Web/wwwroot/css/dmo-jobon.css`, new file, same
   precedent as the accepted per-surface `dmo-admin-users.css`/`dmo-admin-templates.css`) and
   **does not append to `dmo-components.css`**. Reason: the accepted
   `SharedComponentAssetTests` P2-T02 block scan runs from the `P2-T02 (A4)` marker to **end of
   file**, so anything appended there must contain no `@media`/`@container`/`@supports`, no
   `--dmo-*` token declaration and no colour literal. Keeping P2-T04 page CSS in its own file avoids
   perturbing a frozen scan region and avoids touching a protected asset.
6. **P2-T04 adds no JavaScript in the frozen asset files.** If an adapter script is genuinely needed
   (Tool create subflow submit interception, focus return), it is a **new** file
   (`src/DMO.Web/wwwroot/js/dmo-jobon.js`) and it reuses the frozen `window.dmoFocus` helper for
   focus restoration rather than implementing a second one. It installs an idempotency guard
   (`if (window.dmoJobOn) { … }`) following the accepted pattern.
7. **`ToolPicker` never auto-selects in the real surface.** The P2-T04 page must prove this
   behaviourally: a one-candidate result renders one entry with its own select control and no
   preselection (`FERRAMENTAS_LIGHT.md` acceptance criteria; P2-T03 §3.1.7).
8. **Fixed desktop.** Every P2-T04 surface is designed at 1366 × 768 with stable structural regions;
   a component keeps the region its consumer assigns; no width-conditional structural variant, no
   card conversion, no required-column hiding, no action relocation; smaller windows scroll
   (Appendix C).

### 18.3 P2-T03 boundary respected in the reverse direction

P2-T04 changes no P2-T03 test, pin, fixture or scan. The accepted
`P2T03TypeScan`/`P2T03ProductionScan` forbidden-vocabulary scans target P2-T03 owned paths only, so
P2-T04's own correctly-named domain types (`Tool`, `JobOn`, `ToolId`, …) do not redden them. P2-T04
must nonetheless keep its **shared-consumer** artifacts free of P2-T03-owned concepts it does not
need.

---

## 19. Supabase / PostgreSQL compatibility

### 19.1 Runtime target

PostgreSQL is the real runtime target: local development against Supabase **TEST** through
`Database__ConnectionString` (Npgsql, `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 / EF Core
10.0.12), migrations applied by the accepted `--migrate` command (`EfCoreMigrationRunner`), and a
separate disposable PostgreSQL database for integration tests. No second provider, no in-memory
production path and no provider-specific abstraction is introduced.

### 19.2 Feature usage (all supported by Supabase PostgreSQL)

| Feature used | Portability statement |
|---|---|
| `uuid` PKs + `gen_random_uuid()` | already used by migrations 001/002 on this target; `gen_random_uuid()` is built in on PostgreSQL 13+ (Supabase runs newer); no extension is required and none is created |
| `text`, `integer`, `date`, `boolean` | plain portable types |
| `timestamp with time zone` + `now()` | identical to the foundation tables (`DateTimeOffset` in CLR) |
| `CHECK` constraints incl. `btrim(x) <> ''` | identical convention to `users`/`admin_accounts` |
| unique indexes (multi-column) | portable; no partial/expression/filtered unique index is used |
| `ON DELETE RESTRICT` foreign keys, incl. a self-referencing FK | portable |
| transactions (`BEGIN`/`COMMIT`) | standard |
| `LIMIT` for the bounded Tool search | standard |
| `NULLS LAST` in the productions ordering | supported by PostgreSQL (documented; the equivalent `ORDER BY col IS NULL, col` form is an acceptable implementation detail) |

### 19.3 Deliberately avoided

No PostgreSQL-only exotic feature is required: no extension, no `citext`, no `hstore`/`jsonb`
document store, no table inheritance/partitioning, no trigger, no stored procedure, no `LISTEN`/
`NOTIFY`, no RLS policy, no `SECURITY DEFINER` function, no materialized view, no sequence used as a
canonical identity, no `GENERATED ... AS IDENTITY`, no `xmin`/system-column concurrency trick.

### 19.4 Supabase-specific considerations

- The application connects with a single connection string (accepted model). P2-T04 adds no new
  connection, no new secret, no service-role usage and no Auth/provider call.
- The schema is created in the default `public` schema exactly like the foundation tables (no
  `HasDefaultSchema`, no separate schema) — consistent with the accepted `DatabaseConnectivityTests`
  assertions (`table_schema = 'public'`).
- Supabase's `auth`/`storage` schemas are untouched. P2-T04 defines no Supabase RLS policy: access
  control is the application's module-policy gate, which is the accepted model
  (`ACCESS_MODEL.md` §13 "fail closed"; a hidden control is never the security boundary).
- **No live Supabase change is made or claimed by this authoring task.** Applying the migration to
  Supabase TEST is a later, operator-driven deployment step of the implementation workstream.
- Migration re-runnability, `Down` correctness and constraint creation are verified against the
  **disposable** PostgreSQL database (env-gated `DMO_TEST_POSTGRES_CONNECTION` suite) per
  `ACCEPTANCE_MATRIX.md` §10 ("migrations are exercised against disposable PostgreSQL where
  relevant"; "live Supabase changes are never implied by local tests").

### 19.5 What "compatible" must mean in evidence

The implementation must produce database-level evidence (disposable PostgreSQL) that:

1. the migration applies cleanly and creates **exactly** the six contracted tables with the
   contracted columns/nullability/defaults;
2. every contracted constraint exists with the contracted name, including
   `confdeltype = 'r'` (RESTRICT) on every P2-T04 foreign key;
3. the production uniqueness rule and the Tool identity tuple actually reject duplicates;
4. transactional rollback leaves no partial Job On/context set (forced-failure test);
5. the frozen values survive an attempted live Tool-metadata change (no propagation path).

**Compatibility verdict: PASS** — the contracted schema uses only features already exercised by
migrations 001/002 on this target, plus plain portable SQL.

## 20. Test-to-acceptance matrix

Tests are **specified here, not written in this task**. Every acceptance criterion of §22 maps to at
least one proof below and every proof below is **failing-if-removed**. The matrix is
**bidirectional-complete** (§20.12).

Proof classes: **U** = unit/executable model or contract assertion · **I** = integration/host
(HTTP + gates + composition) · **DB** = integration against the disposable PostgreSQL database
(env-gated `DMO_TEST_POSTGRES_CONNECTION`, `[SkippableFact]` + `PersistenceTestDatabase`) ·
**R** = rendered/static asset assertion · **S** = static/architecture scan (reflection + file
content) · **G** = regression on protected foundation.

Naming/placement follows the accepted convention:

```text
tests/DMO.UnitTests/Tools/                    ToolValidatorTests.cs, ToolContractTests.cs,
                                              ToolOrchestrationTests.cs, ToolRestrictionTests.cs
tests/DMO.UnitTests/JobOn/                    JobOnValidatorTests.cs, JobOnContractTests.cs,
                                              JobOnDeletionDependencyTests.cs
tests/DMO.IntegrationTests/Tools/             ToolEndpointsTests.cs, ToolAccessTests.cs
tests/DMO.IntegrationTests/JobOn/             JobOnEndpointsTests.cs, JobOnAccessTests.cs,
                                              JobOnSurfaceRenderingTests.cs, JobOnPickerRenderingTests.cs
tests/DMO.IntegrationTests/Persistence/       ToolRepositoryIntegrationTests.cs,
                                              JobOnRepositoryIntegrationTests.cs,
                                              JobOnDuplicationIntegrationTests.cs,
                                              JobOnDeleteDependencyIntegrationTests.cs,
                                              Migration003ToolJobOnDomainCoreTests.cs
tests/DMO.IntegrationTests/JobOn/             P2T04RegressionTests.cs, P2T04TypeScan.cs (helper)
```

Test methods follow the accepted P2-T03 convention `<ID>_<PascalCaseClauses>` (e.g.
`TOL1_DifferentLot_YieldsADifferentToolId`), each with an XML summary naming its ID and the AC it
proves. Evidence must separate committed test source inspected in Git from execution evidence
(`WORKFLOW.md` "Testing evidence"; `ACCEPTANCE_MATRIX.md` §11).

### 20.1 Canonical Tool and Tool search

| # | Class | Test | Proves |
|---|---|---|---|
| TOL1 | U/DB | `(CM, "5447T173", "12")` and `(CM, "5447T173", "13")` are two distinct Tools with distinct `tool_id`s; the second create is accepted | AC-3 |
| TOL2 | U | validator rejects a missing type / reference / lot / machine set and returns `TOOL_TYPE_REQUIRED`, `REFERENCE_REQUIRED`, `MACHINE_COMPATIBILITY_REQUIRED`, `MACHINE_UNKNOWN`, `PROCESSO_UNKNOWN`, `QUANTITY_NEGATIVE` before any write | AC-5, AC-6, AC-65 |
| TOL3 | U | `Quantity = null` stays `null` through the Tool read model and is never replaced by `0`; `Quantity = 0` renders as `0` | AC-7 |
| TOL4 | U | `ToolValidator.MaxLimit == 100`; `Limit` 0 and 101 fail with `LIMIT_OUT_OF_RANGE`; a search with no criterion fails with `SEARCH_CRITERIA_REQUIRED`; a blank reference fails with `REFERENCE_REQUIRED` | AC-42, AC-45 |
| TOL5 | DB | inserting a second `tools` row with the same (`tool_type`,`reference`,`lot`) is rejected by `tools_type_reference_lot_key` | AC-4 |
| TOL6 | DB | the repository/service duplicate-prevention query finds the existing Tool and the create is refused with its `tool_id`; exactly one row exists afterwards | AC-4 |
| TOL7 | DB | Tool create inserts the Tool **and** its compatibility rows in one transaction; a forced compatibility-row failure leaves no `tools` row and no orphan `tool_machines` row | AC-8 |
| TOL8 | U/S | `ToolSearchQuery` has exactly the contracted members and the search implementation exposes no rank/score/relevance/"best match" member | AC-10 |
| TOL9 | U/DB | two identical searches return the identical order (`tool_type, reference, lot, tool_id`), and the order is stable when rows are inserted in a different order | AC-11 |
| TOL10 | U | `MachineCode.All` is exactly `B1,B2,B3,C1,C2,C3`; the type exposes no `Line`/`Group`/`Linha` member and no parent/child relation; a `"Linha B"` value is rejected | AC-6, AC-17 |
| TOL11 | DB | with 5 matching Tools, `Limit = 2` returns exactly 2 items (limit honoured at the database level) | AC-42 |
| TOL12 | DB | a `Machine` filter returns only Tools having that `tool_machines` row; a Tool compatible with two machines appears in both filtered searches | AC-10 |
| TOL13 | I | `POST /ferramentas/tools` → 201 `{ "toolId": … }` and the persisted row is retrievable by that exact id | AC-8 |
| TOL14 | I | `POST /ferramentas/tools` with an existing identity → 409 `{ "reason": "duplicate-identity", "existingToolId": … }` and no second row | AC-4 |
| TOL15 | I | `GET /ferramentas/tools` with invalid/missing criteria → 400 `validation-failed` with the exact codes; a valid query with no matches → 200 with an empty `items` array | AC-42, AC-45 |
| TOL16 | R/S | no P2-T04 transport/response type or produced body carries a provisional, temporary, negative, display-key or client-supplied Tool identity; the only identity is `toolId` | AC-8, AC-98 |
| TOL17 | S | no second Tool registry exists: no additional Tool table/entity/`DbSet`/store type, and no per-surface Tool cache | AC-1 |
| TOL18 | S | the Tool service/endpoint surface exposes no update and no delete operation (reflection over `IToolService` and the mapped endpoint metadata) | AC-12 |
| TOL19 | DB | deleting a `tools` row referenced by a context or compatibility row is rejected by the `RESTRICT` foreign key | AC-12 |
| TOL20 | S | no Web/application code constructs a canonical `tool_id`; the only Tool identity producer is the Tool create transaction | AC-2 |
| TOL21 | I | a caller without `ferramentas` receives 403 on `/ferramentas/tools` (never an empty list) | AC-86, AC-87 |
| TOL22 | I | an ADMIN session receives 403 on `/ferramentas/tools` and the Job On routes | AC-88 |
| TOL23 | U | mapping N search items produces N `ToolPickerCandidatePresentation`s in supplied order with `SelectedCandidateKey = null` | AC-9 |
| TOL24 | U | mapping exactly one search item produces one candidate with `SelectedCandidateKey = null` (no auto-selection) | AC-9, AC-22 |

### 20.2 Job On occurrence, create, reference query, edit

| # | Class | Test | Proves |
|---|---|---|---|
| JOB1 | U | `JobOnValidator` rejects blank reference/production number and a missing/unknown machine with the exact codes before any write | AC-18, AC-65 |
| JOB2 | U | `"B4"`, `"LINHA B"`, `"Linha B"`, `"B"` are rejected as machines; only the six settled codes are accepted | AC-17 |
| JOB3 | U/S | no Job On type, command, result or route member names or represents a lifecycle status; the status vocabulary scan finds none | AC-15 |
| JOB4 | U/S | the Job On domain record/entity has exactly the contracted members and no `processo`, no quantity, no status, no `productionId`, no `revisionId` | AC-13, AC-14, AC-98 |
| JOB5 | DB | a second `job_ons` row with the same (`reference`,`production_number`) is rejected by `job_ons_reference_production_number_key` | AC-16 |
| JOB6 | I | `POST /jobon` twice with the same pair → first 201, second 409 `duplicate-production` + `existingJobOnId`; exactly one Job On exists | AC-16, AC-20 |
| JOB7 | DB | create with CM+MF selected and BQ empty creates exactly the `job_ons` row plus a `cm_contexts` row and an `mf_contexts` row, and **no** `bq_contexts` row | AC-27 |
| JOB8 | DB | create with no Tool selected creates zero context rows of all three kinds | AC-27 |
| JOB9 | DB | a forced failure while inserting a context rolls the whole create back: no `job_ons` row and no context rows remain | AC-19, AC-66, AC-67 |
| JOB10 | DB | after commit the `jobon_id` is visible to an independent context/connection and matches the value the create returned | AC-19 |
| JOB11 | DB/I | `GET /jobon/productions?reference=` returns **every** matching occurrence including older ones; no row is excluded for age and no "latest" flag/field exists | AC-21, AC-23 |
| JOB12 | I/U | with exactly one match the endpoint returns one item and the consuming surface leaves selection empty | AC-22 |
| JOB13 | I | 0 matches → `empty`; a forced repository failure → `lookup-failed`; a denied caller → `permission-denied`; the three render differently and none is aliased to `empty` | AC-24 |
| JOB14 | I | `?reference=` blank/whitespace → 400 `validation-failed` with `REFERENCE_REQUIRED`, never an empty list | AC-45 |
| JOB15 | U/DB | the productions result is ordered `production_number, production_date NULLS LAST, jobon_id`, deterministically across repeated calls | AC-25 |
| JOB16 | I | `GET /jobon/{id}` returns the occurrence with its existing contexts and their frozen triples | AC-26 |
| JOB17 | I | `GET /jobon/{id}` with an unknown id → 404, never an empty/fabricated ficha | AC-24 |
| JOB18 | S | the productions item carrier exposes exactly id + human production facts — no context id, no Tool fact, no Controlo/Boquilhas/document field | AC-40, AC-46 |
| JOB19 | S | the Tool search item carrier exposes exactly `tool_id` + Tool-owned facts — no status, availability, condition, stock or verdict | AC-41 |
| JOB20 | S | no contracted query exposes an unbounded list: productions require a reference, search requires ≥ 1 criterion and a bounded limit | AC-39 |
| JOB21 | S | no P2-T04 code path derives a canonical identity from display text (no reference/lot/machine → `tool_id` resolution exists) | AC-43 |
| JOB22 | U | N matching rows map to N result items with no merging, de-duplication, grouping or ranking in the mapping | AC-44 |
| JOB23 | S | no EF query exists outside the contracted repositories; no module queries another module's table | AC-46 |
| JOB24 | DB | an edit changes reference/production number/machine/production date and persists them, incrementing `version` exactly once | AC-68, AC-73 |
| JOB25 | DB | an edit with a stale `ExpectedVersion` raises the domain conflict, returns `stale-version`, and leaves every column unchanged | AC-72 |
| JOB26 | S | the update path writes only the four facts + context changes + `version` + `updated_at`; no status column is written anywhere | AC-74 |
| JOB27 | DB | an edit setting a Tool whose type does not match the slot fails with `TOOL_TYPE_MISMATCH` and writes nothing | AC-68 |
| JOB28 | I/DB | with a reached/passed `production_date`, `PUT` without acknowledgement → 409 `date-threshold-confirmation-required` (nothing written); the same request with acknowledgement → 200 and the values are persisted | AC-70, AC-71 |
| JOB29 | I | `DELETE` without `deleteConfirmed` → 400 `validation-failed` with `DELETE_NOT_CONFIRMED`, nothing deleted | AC-75 |
| JOB30 | I | `DELETE` on a reached/passed date without acknowledgement → 409 `date-threshold-confirmation-required`, nothing deleted | AC-76 |

### 20.3 Contexts and frozen snapshot

| # | Class | Test | Proves |
|---|---|---|---|
| CTX1 | DB | selecting the same Tool twice for one slot leaves one row with the identical context id (idempotent reuse) | AC-28, AC-29 |
| CTX2 | DB | a direct second insert of the same context type for one Job On is rejected by `cm_contexts_jobon_key` (`23505`) | AC-29 |
| CTX3 | DB | `cm_contexts.tool_type = 'MF'` is rejected by `cm_contexts_tool_type_check`; the same holds for `mf_contexts`/`bq_contexts` | AC-34 |
| CTX4 | DB | a context row cannot be inserted with a null `jobon_id`/`tool_id` or with an unknown id (NOT NULL + FK) | AC-30 |
| CTX5 | DB | after updating the canonical Tool's `reference`/`lot` directly, every existing context row still returns its original frozen triple | AC-31, AC-32 |
| CTX6 | S | no code path updates a context's frozen columns except the explicit re-selection step of the edit transaction; no trigger/worker/event propagates Tool changes | AC-32 |
| CTX7 | DB | re-selecting a different Tool for a slot updates the same context row (identical `cm_id`) and refreshes `tool_id` + the frozen triple | AC-35 |
| CTX8 | DB | `Remove` deletes exactly that context row in the same transaction as the fact change; the other contexts are untouched | AC-36 |
| CTX9 | U | the context domain record exposes no quantity, processo, operational note, baffle/calote, measurement or Boquilhas member | AC-33 |
| CTX10 | S | the three context tables declare exactly the contracted columns — none of the excluded facts has a column | AC-33 |
| CTX11 | S | no Tool/Job On type exposes a reverse collection member and no reverse-array column exists | AC-38 |
| CTX12 | DB | the reverse read by `tool_id` returns the occurrences using that Tool while no Tool row stores them | AC-30, AC-38 |
| CTX13 | I | reading a Job On with no contexts returns an empty context list and creates no rows | AC-26, AC-27 |
| CTX14 | DB | every new context — on create AND on duplication — freezes the triple read from the Tool row at creation time; a later Tool metadata change is never reflected in an existing context (§23 supersedes the former duplication wording) | AC-61 |
| CTX15 | DB | removing a context referenced by a test-double dependent row fails closed (`RESTRICT`) and nothing is deleted | AC-37 |
| CTX16 | U | context resolution by (`jobonId`, type) returns at most one context; the API exposes no list-of-contexts-for-a-type operation | AC-29 |
| CTX17 | S | each context entity/configuration declares exactly the contracted columns, no `version` column and no extra navigation | AC-104 |
| CTX18 | R | the Job On edit surface renders the frozen triple as read-only (no bound editable input for it) | AC-69 |

### 20.4 Duplication

| # | Class | Test | Proves |
|---|---|---|---|
| DUP1 | U | `DuplicateJobOnCommand` requires source id, expected source version, production number and machine; the validator returns the exact codes otherwise | AC-55, AC-65 |
| DUP2 | I/DB | the preview returns the source ficha plus `sourceVersion`, and the source `version`/`updated_at` are unchanged afterwards | AC-55 |
| DUP3 | DB | duplication produces a `jobon_id` different from the source and from every other Job On | AC-57 |
| DUP4 | DB | every duplicated context has a new id, none equals a source context id, and the count matches the source's context count | AC-58 |
| DUP5 | DB | after duplication the source row and all source context rows are byte-identical (same `version`, same frozen triples, same timestamps) | AC-59 |
| DUP6 | DB | each duplicated context references the same canonical `tool_id` as its source context | AC-60 |
| DUP7 | DB | after changing the live Tool's `reference`/`lot`, duplication snapshots the **current canonical Tool state** — the duplicate carries the NEW values while the source context keeps its historical frozen triple (§23 supersedes the former "copies the source frozen triple" reading) | AC-61 |
| DUP8 | DB | the duplicated row's `copied_from_jobon_id` equals the source id; a created (non-duplicated) Job On has `NULL` | AC-62 |
| DUP9 | DB | duplicating onto an existing (`reference`,`production_number`) is refused and creates no row | AC-16, AC-63 |
| DUP10 | DB | a stale `ExpectedSourceVersion` refuses the duplication: no new `job_ons` row, no new context rows | AC-64 |
| DUP11 | I | a duplication request forced to fail mid-transaction returns 409 and leaves zero new rows (total rollback) | AC-63 |
| DUP12 | I/DB | duplicating from the **oldest** available source of a reference (no newer production selected) succeeds | AC-56 |
| DUP13 | S | no duplication member, default or query selects a source implicitly (no "latest"/"previous" concept exists) | AC-55 |
| DUP14 | DB | duplication does not modify `tools` or `tool_machines` (row hashes unchanged) | AC-53 |
| DUP15 | I | after duplication the new Job On's CM Tool can be changed while the source's CM context keeps its `tool_id` and frozen triple | AC-60, AC-59 |
| DUP16 | S | the duplication request carrier contains no source context id and no `tool_id` reuse field driven by the client | AC-58 |
| DUP17 | DB | duplicating the same source twice produces two independent occurrences whose context identities are ALL distinct (source vs B vs C, per table) while every context references the same canonical `tool_id` — no context identity is ever reused (§23) | AC-58 |
| DUP18 | DB | the source `bq_id` and the duplicated `bq_id` are distinct while both reference the same canonical BQ `tool_id` — P2-T07 movements keyed by `bq_id` stay with their own production (§23) | AC-58 |

### 20.5 Delete and dependency rule

| # | Class | Test | Proves |
|---|---|---|---|
| DEP1 | S | every P2-T04 entity configuration declares `DeleteBehavior.Restrict` for every foreign key | AC-80 |
| DEP2 | DB | `pg_constraint.confdeltype = 'r'` for all eight P2-T04 foreign keys | AC-80, AC-106 |
| DEP3 | DB | a permitted delete removes this Job On's context rows and its own row, and `tools`/`tool_machines`/other Job Ons are unchanged | AC-79 |
| DEP4 | DB | deleting a Job On that is the recorded duplication source is refused both by the lineage probe and by the self-FK | AC-78 |
| DEP5 | I | the delete refusal body is 409 `dependency-exists` with `dependencies[]` containing kind `duplication-lineage` and a description | AC-77, AC-78 |
| DEP6 | U | the delete service inspects **every** registered probe (two test doubles are both invoked, order-independent) | AC-77 |
| DEP7 | I | with a registered test-double probe reporting a dependency, the delete is refused and no row is deleted | AC-82 |
| DEP8 | I | with that probe no longer registered (registration only, no production change), the same delete succeeds | AC-77, AC-82 |
| DEP9 | DB | a stale `ExpectedVersion` refuses the delete and leaves the Job On and its contexts intact | AC-81 |
| DEP10 | DB | a forced failure between the context delete and the Job On delete rolls back completely (contexts and row still present) | AC-79 |
| DEP11 | S | no soft-delete/archive/cancel column, flag or state exists in the schema or the domain types | AC-80, AC-98 |
| DEP12 | S | the delete API exposes no cascade/force/recursive option | AC-80 |
| DEP13 | U | the refusal is a typed, actionable result (not a generic exception) and names the contributing dependency kinds | AC-77 |

### 20.6 Routes, policy and access

| # | Class | Test | Proves |
|---|---|---|---|
| RTE1 | I | every mapped P2-T04 endpoint carries exactly one policy whose name equals `ModuleAuthorizationPolicies.PolicyName(<canonical module>)` | AC-83 |
| RTE2 | I | the 14 contracted routes exist with the contracted paths/verbs and the contracted per-route policy | AC-83 |
| RTE3 | I | no additional P2-T04 route/page/endpoint exists beyond the 14 (endpoint metadata + page list) | AC-83 |
| RTE4 | I | a `job-on-view`-only caller: routes 1–3 allow, routes 4–11 deny (403), including route 5's Create-only read | AC-84, AC-85, AC-89 |
| RTE5 | I | a `job-on-create`-only caller: routes 4–11 allow (including route 5) and the flow completes without any View grant | AC-90 |
| RTE6 | I | a `ferramentas`-only caller: routes 12–14 allow, Job On routes deny | AC-86 |
| RTE7 | I/S | no P2-T04 route carries `dmo.module.ferramentas-approve`; the policy set derived from the catalog is unchanged | AC-86 |
| RTE8 | I | an anonymous caller on any P2-T04 route receives 401/redirect and never a 200 with data | AC-87 |
| RTE9 | I | an ADMIN session receives 403 on every P2-T04 route while the administration surface still works | AC-88 |
| RTE10 | I | `GET /jobon/{id}` for an unauthenticated caller renders no protected data (denial, not a blank-but-rendered ficha) | AC-87 |
| RTE11 | S/I | `ModuleRegistrations.CurrentBuildAvailable` is still `[]` in the production composition | AC-92 |
| RTE12 | S/I | `DestinationRouteRegistrations` is still an empty type, `EmptyDestinationRouteRegistry` is still the production registration, and no P2-T04 route is registered anywhere | AC-91 |
| RTE13 | I | with the production composition (`CurrentBuildAvailable = []`), a user granted `Job On Create` in the persisted Template is still **denied** (module unavailable ⇒ fail closed) | AC-94 |
| RTE14 | I | a projection/navigation test with the P2-T04 test registry yields no `ferramentas` entry and no `job-on` entry unless the test registry registers the destination route | AC-93, AC-91 |
| RTE15 | S | `ModuleCatalog.cs` and `ModuleAuthorizationPolicies.cs` are byte-identical to the baseline | AC-101 |
| RTE16 | I/S | for every P2-T04 page model the `[Authorize]` policy equals the contracted policy for that route | AC-83 |
| RTE17 | I | a mutating verb against a `job-on-view` route is not served (405/404), never a successful mutation | AC-84 |
| RTE18 | I | every mapped P2-T04 route has `IAuthorizeData` metadata; no P2-T04 route is mapped without `RequireAuthorization` | AC-83 |

### 20.7 Tool orchestration and origin-state boundary

| # | Class | Test | Proves |
|---|---|---|---|
| ORC1 | S | exactly one Tool search/select/create orchestration exists (one service + one adapter boundary); no per-surface duplicate exists | AC-47 |
| ORC2 | U | one `candidate selected` event resolves exactly one canonical `tool_id`; a second resolution cannot change the first within one interaction | AC-48 |
| ORC3 | S | no shared-consumer type, member, constant, DOM hook or fixture declares an opaque key as `tool_id`/`jobon_id`/`cm_id`/`mf_id`/`bq_id` | AC-49 |
| ORC4 | U | `cancel requested` / `return requested` leave the origin association unchanged and select nothing | AC-50 |
| ORC5 | I | the inline Tool create subflow returns the persisted `tool_id` and the origin Job On create then persists that exact id in the context | AC-51 |
| ORC6 | U | pre-fill carries only values already known from the context; no unknown Tool fact is invented or defaulted | AC-52 |
| ORC7 | S | no P2-T04 type writes `tools`/`tool_machines` outside the Tool create transaction; the Job On area owns no Tool master fact | AC-53 |
| ORC8 | S/I | no standalone Tool create page or route exists and no server-side draft/store table exists | AC-54 |
| ORC9 | R | on the real Job On create surface, a one-candidate result renders one candidate entry with its own select control and **no** `aria-selected="true"` anywhere | AC-9 |
| ORC10 | R/S | the origin token round-trips verbatim through the P2-T04 usage and appears in no URL, route, filename or path | AC-49 |

### 20.8 Boundaries, protected files and regression

| # | Class | Test | Proves |
|---|---|---|---|
| BND1 | S | `ModuleRegistrations.CurrentBuildAvailable` is empty | AC-92 |
| BND2 | S | migration 001/002 files and `DmoDbContext.cs` are byte-identical (normalized hash) | AC-96 |
| BND3 | S | every P2-T01/P2-T02/P2-T03 contract, partial, CSS and JS file is byte-identical and `dmo-components.css` contains no P2-T04 marker | AC-97 |
| BND4 | S | the new P2-T04 stylesheet exists and contains no `@media`/`@container`/`@supports` structural breakpoint rule | AC-102 |
| BND5 | S | `production_id`, `job_on_revision_id`, `revisionId`, `productionId` and reverse-array tokens appear nowhere in P2-T04 sources, schema or payloads | AC-98 |
| BND6 | S | repairer, machine-assignment, machine-registry and Definições vocabulary appears nowhere in P2-T04 sources or schema | AC-99 |
| BND7 | S | P2-T05+ vocabulary (Definições, settings, Peso, Comparação, Pegamentos, Folha, Resumo, Boquilhas, PDF, document availability, secondary navigation, HISTÓRICO GLOBAL) appears nowhere in P2-T04 sources | AC-100 |
| BND8 | S | no reverse-array member, serialized collection or join table encodes Tool→JobOn navigation | AC-98 |
| BND9 | S | the changed-path allow-list holds: only Appendix B paths plus the documented additive composition/snapshot files changed | AC-95 |
| BND10 | G | the entire existing `DMO.UnitTests` suite passes with the same or higher count and zero failures | AC-105 |
| BND11 | G | the entire existing `DMO.IntegrationTests` suite passes with the same 71 environment-gated skips and zero failures | AC-105 |
| BND12 | S | no existing test file was modified or deleted | AC-105 |
| BND13 | I | no P2-T04 route registers module availability or a destination route (behavioural proof, not only a source scan) | AC-101 |
| BND14 | S | the `Program.cs` / `PersistenceServiceCollectionExtensions.cs` additions are exactly the contracted registration lines | AC-95, AC-101 |

### 20.9 Migration and PostgreSQL

| # | Class | Test | Proves |
|---|---|---|---|
| MIG1 | DB | after migration, `public` contains exactly the four foundation tables plus the six contracted tables and no other new table | AC-103, AC-104 |
| MIG2 | DB | every contracted column exists with the contracted type and nullability (catalog query on `information_schema.columns`) | AC-103 |
| MIG3 | DB | every contracted CHECK constraint exists with the contracted name and actually rejects a violating row | AC-103, AC-106 |
| MIG4 | DB | `tools_type_reference_lot_key`, `job_ons_reference_production_number_key`, the three context `*_jobon_key` and `tool_machines_tool_machine_key` exist and are unique | AC-103, AC-106 |
| MIG5 | DB | all eight foreign keys exist with the contracted names and `confdeltype = 'r'` | AC-80, AC-106 |
| MIG6 | DB | every contracted index exists with the contracted name and column list | AC-103 |
| MIG7 | DB | applying the migration twice is a no-op (no error, no schema change) | AC-103 |
| MIG8 | DB | `Down` drops exactly the six tables and leaves the foundation tables and their constraints intact | AC-103 |
| MIG9 | DB | after migration the foundation tables/constraints are byte-equivalent to the pre-migration catalog state | AC-96 |
| MIG10 | S | exactly one new migration file pair exists and `DmoDbContextModelSnapshot` records the six new tables only | AC-103 |
| MIG11 | DB | the migration applies cleanly to a disposable PostgreSQL database at the target version | AC-106 |
| MIG12 | S | the migration contains no `DROP`/`TRUNCATE`/schema reset and no seed/reference-data insert | AC-96, AC-103 |

### 20.10 Enterprise evidence requirements

Every DB-class test is `[SkippableFact]` behind `PersistenceTestDatabase.SkipIfNotConfigured()` and is
reported as **skipped environment-gated** when `DMO_TEST_POSTGRES_CONNECTION` is unset — never as
passing. The implementation response must state the exact command, the observed counts (total /
passed / failed / skipped), and which category the skips belong to
(`ACCEPTANCE_MATRIX.md` §11; `WORKFLOW.md` "Testing evidence").

### 20.11 Not required by this contract

- browser/DOM-level automation: the accepted authority for the shared primitives is
  "C# interaction-model unit tests + rendered markup/hook/attribute assertions + static-asset
  content assertions" (P2-T02 Q1 and P2-T03 Q4 defaults). P2-T04 follows the same evidence level.
- load/performance testing, accessibility audit tooling, mutation testing and visual regression
  tooling: not required by any authority for this workstream.

### 20.12 Completeness

```text
acceptance criteria (§22):                 106
test rows specified above:                 155
acceptance criteria with ≥ 1 proof:        106 / 106
test rows with ≥ 1 acceptance criterion:   155 / 155
uncovered acceptance criteria:             0
```

---

## 21. Authority questions

Every question below is **NON-BLOCKING**: each has a pinned default that is already reflected in the
physical schema, the interfaces and the route matrix of this contract, so implementation can be
authorized without further authority. **No physical-schema-critical question remains BLOCKING.**

| # | Question | Authority gap (exact) | Options | Pinned default | Implementation impact | Class |
|---|---|---|---|---|---|---|
| Q1 | Does the Job On production key need `machine` (or another fact) in addition to `reference + production_number`? | `docs/CREATION_AND_ASSOCIATION_LOGIC.md` fixes the expected pair and extends it only "if the real process allows more than one distinct Job On with the same combination"; the P2-T04 handoff §5 repeats this and forbids adding facts "for safety". `JOB_ON.md` §7 says typical duplicate changes include machine, and `BETA_VERSION.md` §4.1 mentions productions differing "by machine or production setup", but no authority asserts two *distinct* Job Ons sharing a reference + production number; the P2-T08 directory convention `<reference>/<production-number>/` would even place two machines' PDFs in one directory | (a) pair only; (b) + machine; (c) + machine + date | **(a) pair only.** Only (a) is fixed by an authority that explicitly conditions any extension; (b)/(c) would silently permit a second Job On for the same real production, which the same authority forbids ("must not create a second Job On for the same real production"). The failure mode of (a) is explicit and non-destructive (`409 duplicate-production` naming the existing Job On) — it never creates a wrong duplicate | schema (unique index) + create/duplicate flow. If a future authority decision requires the extension, the smallest change is `UNIQUE (reference, production_number, machine)` in a new migration plus validator/route wording; §4.2 already names it | NON-BLOCKING |
| Q2 | Does `processo` belong on the Job On, on the context, or only on the Tool? | `FERRAMENTAS.md` §3/§647 ("never stored as an independent Job On fact"), `JOB_ON.md` §6/§15 and `JOB_ON_LIGHT.md` ("displayed/consumed Tool fact … not a second Job On authority") agree; `BETA_VERSION.md` §4/§11 and `BACKEND_FRONTEND_MODEL.md` list `processo` in the Job On chain — read as a *displayed* fact, they are consistent | (a) Tool only (consumed); (b) + frozen copy on each context; (c) + Job On column | **(a) Tool only.** Three authorities fix it explicitly; (b)/(c) would create the "competing process authority" the global contract forbids, and no Beta acceptance criterion needs it | no column on `job_ons` or on any context table; surfaces read it through `cm_id → tool_id`, showing *unavailable* when no CM context exists | NON-BLOCKING |
| Q3 | Is a slot/type mismatch (e.g. an MF context pointing at a BQ Tool) a validation failure? | Authority fixes three context kinds each bound to its own Tool family (`CM → tool_id`, `identities/relationships`) but states no explicit validation rule | (a) enforce the match (fail closed); (b) accept any canonical Tool | **(a) enforce.** A context named MF referencing a non-MF Tool cannot be rendered or explained truthfully, and fail-closed matches the platform's other validation behaviour | validator code `TOOL_TYPE_MISMATCH`, a DB CHECK on each context table (`tool_type` = `'CM'`, `'MF'` or `'BQ'`), and one test | NON-BLOCKING |
| Q4 | Is the machine value set closed, and is `tool_machines` the right table name? | `…DELTA.md` §4.1 fixes the six operational machines but §4.4 states "no machine set is declared exhaustive for all future modules" and fixes no `machine_id` scheme; the global target names a per-instance `tool_machines` table (`FERRAMENTAS.md` §3) | (a) code value with CHECK on the six; (b) code value with no CHECK; (c) machine registry table | **(a).** A CHECK keeps the settled independence rule enforceable and honest; adding a machine later is a small, reviewable migration, whereas (b) silently accepts `"Linha B"`-style values and (c) is explicitly forbidden here | CHECK constraints on `job_ons.machine` and `tool_machines.machine`; table name `tool_machines` matches the global target's per-instance association table without inventing a machine identity | NON-BLOCKING |
| Q5 | Three context tables or one discriminated table? | Authority fixes three distinct context identities with three distinct captured-value families (`JOB_ON.md` §3, §5.6) but fixes no physical layout | (a) three tables; (b) one table + `context_type` | **(a).** P2-T05 adds CM-facing, MF-facing and BQ-facing value families; (b) becomes a sparse shared table with nullable per-type columns, i.e. a design that has to be undone | three near-identical tables with per-table type CHECKs and unique `jobon_id` indexes | NON-BLOCKING |
| Q6 | Does a context need its own machine value? | `JOB_ON.md` §5 allows a captured "machine/line context" *where the real sheet uses it*; the machine is otherwise a Job On fact | (a) no context machine column (read the Job On's); (b) freeze the Job On machine per context; (c) allow an independent per-context machine | **(a).** (b) duplicates a Job On fact with no authority; (c) would invent a per-context machine authority that the delta's autonomy rule does not mention | no machine column on any context table | NON-BLOCKING |
| Q7 | What ordering do reference → productions results use? | Authority is **silent** — no ordering, no "latest", no recency rule exists (`CORE-` and Beta documents describe only explicit selection) | (a) no guaranteed order; (b) `production_number, production_date NULLS LAST, jobon_id`; (c) newest first | **(b).** A deterministic *technical* order is required for stable lists/paging; it carries no industrial meaning, is documented as authority-silent, and cannot imply "latest" (the id tie-break makes it total) | one `ORDER BY`; a unit test pins the order | NON-BLOCKING |
| Q8 | Should there be a standalone Tool create **page** (with a server-side draft store to survive navigation)? | The Beta requires the origin workflow state to be preserved ("Missing Tool creation returns to the originating workflow without losing state"), but no authority authorizes a draft/draft-store entity and the Beta schema gate forbids tables for convenience | (a) inline create subflow only (no navigation, no draft store); (b) separate page + new server-side draft entity; (c) separate page + browser-only state | **(a).** It satisfies the acceptance criterion with the smallest model and adds no unauthorized table; (c) would lose state on a full navigation, and (b) needs its own authority | no create page route; the create subflow posts to `POST /ferramentas/tools` from the origin surface | NON-BLOCKING |
| Q9 | Does a Job On **create** with an already-passed production date also warn? | `JOB_ON.md` §9A scopes the threshold to *edit* and delete; create is not mentioned | (a) no create-time warning; (b) warn on create too | **(a).** The authority scopes the protection to edit/delete; adding a create gate would invent a rule. Recorded so a later authority decision can add it deliberately | create route unchanged; the threshold applies on `PUT` and `DELETE` only | NON-BLOCKING |
| Q10 | One `production_date` or separate planned vs actual dates? | `JOB_ON.md` §6/§9A distinguishes a *planned* production date (the threshold) from production actually occurring; `docs/CREATION_AND_ASSOCIATION_LOGIC.md` shows a single `production_date` in the minimal Job On; `JOB_ON_LIGHT.md`'s simplified field list contains no date at all | (a) one nullable `production_date` used as the §9A threshold; (b) `planned_production_date` + `actual_production_date`; (c) no date column (and therefore no threshold) | **(a).** The single column is the smallest model that implements the accepted warning and stays compatible with the repository's recorded minimal Job On; (c) would break the accepted date-threshold requirement | one `date` column, documented as planning data that is never proof of production | NON-BLOCKING |
| Q11 | Is `production_number` text or integer? | Authority fixes only that the number identifies the production and appears in the P2-T08 directory token `<production-number>` | (a) `text` (verbatim); (b) `integer` | **(a).** The stored value must reproduce the directory token exactly; integer storage would normalise away leading zeros and lose the operator's value | `text` column, trimmed, exact comparison | NON-BLOCKING |
| Q12 | Does a lot-only or machine-only Job On search need an index, and does a lot-only Tool search need one? | Authority fixes no query-performance requirement and no index | (a) contract only the indexes justified by contracted queries; (b) add speculative indexes | **(a).** §4.5 justifies each index by a contracted query; the Beta registry is small and an unjustified index would be schema for convenience | a lot-only Tool search filters after the type/reference predicate; no extra index | NON-BLOCKING |
| Q13 | Can a wrong Tool-owned field (e.g. a wrong lot) be corrected in P2-T04? | `JOB_ON.md` §9 requires correcting the wrong field **on the canonical Tool**, which is a Tool edit; Tool editing is the Ferramentas change-request/direct-edit lifecycle, explicitly outside Beta Light scope | (a) no Tool edit in P2-T04 (the operator creates the correct Tool and re-selects it); (b) add a Tool edit path | **(a).** Adding a Tool edit path would authorize a mutation outside Beta scope and pre-empt the change-request authority. The practical Beta workaround is a new correct Tool plus an explicit re-selection (which is a supported edit) | no Tool update route/primitive exists; §5.3.6 | NON-BLOCKING |
| Q14 | Are reference/lot compared case-sensitively? | Authority fixes no lexical normalisation for references or lots | (a) trimmed, exact (ordinal); (b) case-insensitive; (c) fully normalised | **(a).** Any normalisation rule would itself be an invented data rule and would need its own authority; exactness keeps "different lot = different Tool" unambiguous and auditable | trimming on input; unique indexes compare exact values | NON-BLOCKING |
| Q15 | What ordering does Tool search use? | Authority is silent; ranking/relevance is explicitly *not* the shared component's business | (a) no guaranteed order; (b) `tool_type, reference, lot, tool_id`; (c) relevance score | **(b).** Deterministic and meaning-free; (c) would invent an industrial ranking that `FERRAMENTAS_LIGHT.md` forbids in the picker | one `ORDER BY`; a unit test pins it | NON-BLOCKING |
| Q16 | At duplication, is the copied context's frozen triple re-read from the live Tool or copied from the source context? | `JOB_ON.md` §8 says "copy captured/manual values as reviewable starting values"; `BETA_VERSION.md` §4.1 says the duplicate receives new contexts with the same `tool_id` "until changed" | (a) copy the source's frozen triple verbatim; (b) re-read the live Tool | **(b) — SUPERSEDED by the Owner clarification §23.** The former pinned default (a) — "preserves exactly what the source recorded … avoids a silent 'refresh' of history" — is replaced: the new production must receive NEW snapshots of the CURRENT canonical Tool row (the source context contributes only its `tool_id`). Historical source contexts remain immutable. | the duplication service re-snapshots every copied context through its source `tool_id` using the same snapshot path as normal creation; a test where the live Tool changed before duplication asserts the duplicate carries the CURRENT values | NON-BLOCKING |
| Q17 | Does Job On create need a client idempotency key? | No authority fixes one, and the accepted foundation has no idempotency mechanism | (a) none (the production unique index makes a repeat explicit); (b) add one | **(a).** The unique production index already converts a retry into an explicit, non-destructive `duplicate-production` refusal with the existing id, which is exactly the required "no duplicate production" behaviour without a new mechanism | no idempotency column/key | NON-BLOCKING |
| Q18 | Is `copied_from_jobon_id` editable after duplication? | `JOB_ON.md` §8 calls the lineage "optional … structural lineage and does not make the new Job On a version of the source"; it fixes no edit rule | (a) set once, never rewritten; (b) editable | **(a).** Rewriting recorded lineage would silently rewrite history; making it immutable is the conservative reading and is also what the `RESTRICT` self-FK implies | lineage is not part of the update command | NON-BLOCKING |
| Q19 | How do "Create implies the reads it needs" and the frozen one-policy-per-module mechanism coexist? | `ACCESS_MODEL.md` §8 says Create grants the reads required to operate the surface, but the accepted policy projection is exactly one requirement per module with no OR/implication, and modifying it is a protected-area change | (a) provide the Create workflow's reads on Create-gated routes (route 5 + Create pages); (b) modify the access foundation to express implication/OR; (c) let a Create-only user depend on also holding View | **(a).** It satisfies §8 without touching protected foundation and without inventing composition rules. (b) is a separate, authorized change to protected foundation; (c) would contradict the accepted sibling/non-satisfaction semantics | one extra Create-gated read route; JobOnView keeps only read routes | NON-BLOCKING |
| Q20 | Is "Ferramentas Approve includes Ferramentas" expressible, and what happens to a Ferramentas Approve-only holder? | `ACCESS_MODEL.md` §11 states Approve "includes the Tool access needed for approval work", but the frozen policy mechanism cannot express an implication and P2-T04 implements no approval capability | (a) no P2-T04 route carries `ferramentas-approve`; document the consequence; (b) gate some Ferramentas routes with Approve too (which would then deny plain Ferramentas holders); (c) change the access foundation | **(a).** P2-T04 implements only the contextual search/detail/create subset, so there is no Approve-only behaviour to expose; (b) would break the plain Ferramentas grant; (c) is a separate authorized change. Consequence to record: a Template that should give approval rights must grant **both** modules until the access foundation authorizes an implication | no Approve-gated route; §13.4 | NON-BLOCKING |
| Q21 | Does the inline Tool create/search from a Job On surface require the `ferramentas` grant, and how does a Job On operator see the Tools it references? | `ACCESS_MODEL.md` §11 defines Ferramentas as the contextual Tool access/creation module and says opening a ficha from another module "never grants Ferramentas access by itself"; `JOB_ON_LIGHT.md` requires the Job On surface to display its associated CM/MF/BQ Tools | (a) Tool search/create/ficha stay `ferramentas`-gated, while the Job On **read model** projects the referenced Tools' current facts under the Job On policy; (b) also gate Tool search/create with the Job On policies; (c) persist Tool facts into the Job On model | **(a).** (b) would grant Tool creation without the Ferramentas module — a second access path to Tool master data; (c) is copying foreign module state (forbidden). (a) satisfies both authorities: the Job On shows the Tools it references (read-time projection, nothing persisted), while Tool **access** remains the Ferramentas module. Consequence: a Template whose operators must associate Tools grants both modules (an Admin/P2-T10 composition matter, not a code bypass) | the Job On ficha read model carries a read-time Tool summary projection; Tool search/create/ficha remain `ferramentas`-gated | NON-BLOCKING |
| Q22 | Must the "one shared Tool orchestration" also expose a Tool **list** surface with no criteria? | `FERRAMENTAS_LIGHT.md` lists "list/search of registered Tools" but the Beta binds no unbounded browse requirement, and an unbounded registry dump is not a contracted query | (a) one bounded search contract that also serves as the list (type/reference supplied); (b) an unfiltered list endpoint | **(a).** One contract, one route, bounded result; (b) would add an unbounded read with no authority | route 12 only; `SEARCH_CRITERIA_REQUIRED` for an empty criterion set | NON-BLOCKING |

---

## 22. Implementation acceptance criteria

P2-T04 is acceptable only when every criterion below is satisfied and proven by §20. **106 criteria.**

### Canonical Tool (AC-1 … AC-12)

| # | Criterion |
|---|---|
| AC-1 | `tools` is the only canonical Tool registry; no second Tool table, store, cache or per-module registry exists. |
| AC-2 | `tool_id` is allocated by the backend; no client/frontend-minted canonical Tool identity exists. |
| AC-3 | A different lot yields a different `tool_id`; the lot participates in the Tool identity tuple. |
| AC-4 | The same (`tool_type`,`reference`,`lot`) cannot be created twice; the second attempt is refused naming the existing `tool_id` (application check **and** unique index). |
| AC-5 | `tool_type` ∈ {CM,MF,BQ}; `processo` ∈ {NNPB,PS} or NULL; `quantity` ≥ 0 or NULL. |
| AC-6 | Every created Tool has one or more machine-compatibility rows from the settled six codes; no machine registry, no `machine_id` and no line/grouping column exists. |
| AC-7 | `quantity = NULL` is never stored, returned or rendered as `0`. |
| AC-8 | Tool create is atomic (Tool + its full compatibility set) and returns the real canonical `tool_id`; no provisional, fake, temporary or display-key identity is ever returned. |
| AC-9 | Tool search never auto-selects — including with exactly one result — and ambiguity remains explicit as N separate candidates. |
| AC-10 | Tool search applies only the contracted predicates; no ranking, compatibility verdict, stock, location, latest or "best match" filter is invented. |
| AC-11 | Tool search ordering is the contracted deterministic technical order and carries no industrial meaning. |
| AC-12 | No Tool update or delete path exists in P2-T04, and a referenced Tool cannot be deleted at the database level. |

### Job On occurrence (AC-13 … AC-26)

| # | Criterion |
|---|---|
| AC-13 | `jobon_id` identifies one production occurrence; `production_id` and `job_on_revision_id` exist nowhere. |
| AC-14 | A Job On stores exactly `reference`, `production_number`, `machine`, `production_date`, `copied_from_jobon_id`, `version` and system timestamps. |
| AC-15 | No Job On-wide lifecycle or status state exists in schema, domain types, commands, results, routes or tests. |
| AC-16 | (`reference`,`production_number`) is the production uniqueness rule; a second Job On for the pair is refused naming the existing `jobon_id`. |
| AC-17 | `machine` is one of the six settled codes; no "Linha B"/"Linha C" or any machine grouping/inheritance/cascade appears anywhere. |
| AC-18 | The Job On create surface captures only the simplified Beta fields. |
| AC-19 | The created `jobon_id` is durable at commit; a failed create leaves no partial Job On. |
| AC-20 | A retry after a successful commit creates no duplicate; it is refused with `duplicate-production`. |
| AC-21 | reference → productions returns every matching occurrence for the reference, with no "latest" implication. |
| AC-22 | reference → productions never auto-resolves, including when exactly one production matches. |
| AC-23 | Historical (older) occurrences are selectable, including as a duplication source. |
| AC-24 | Empty results, lookup failure and permission denial are three distinct outcomes in every consumer, never aliased. |
| AC-25 | reference → productions uses the contracted deterministic technical ordering, recorded as authority-silent. |
| AC-26 | The Job On read returns the occurrence plus its existing contexts with their frozen values. |

### Contexts and frozen snapshot (AC-27 … AC-38)

| # | Criterion |
|---|---|
| AC-27 | A context row exists only where its Tool slot was explicitly selected; no symmetry context is ever created (0 or 1 row per type per Job On). |
| AC-28 | `jobon_id + tool_id` context resolution is idempotent: reuse yields no second row and the same context id. |
| AC-29 | At most one context per type per Job On exists, enforced in the database. |
| AC-30 | Every context retains a direct canonical `tool_id` relation. |
| AC-31 | Every context freezes the Tool type, reference and lot as used at that occurrence. |
| AC-32 | A later change to canonical Tool facts never rewrites a persisted context; no propagation path exists. |
| AC-33 | Contexts freeze no quantity, no `processo`, no operational note, no Baffle/calote, no measurement and no Boquilhas fact. |
| AC-34 | A context's type and its frozen Tool type can never disagree (database check). |
| AC-35 | A context's Tool changes only through an explicit human re-selection, in place, on the same context row. |
| AC-36 | Removing a Tool association removes only that context row, inside the Job On transaction. |
| AC-37 | A context removal referenced by a dependent fact is refused; nothing is silently cascaded. |
| AC-38 | No reverse Tool→JobOn array or equivalent persisted collection exists. |

### Query contracts (AC-39 … AC-46)

| # | Criterion |
|---|---|
| AC-39 | Every contracted query has an explicit input and result carrier and is predicate-bound (no unbounded registry dump). |
| AC-40 | The reference → productions carrier carries identity plus human production facts only. |
| AC-41 | The Tool search carrier carries the canonical `tool_id` plus Tool-owned facts only. |
| AC-42 | The Tool search limit is bounded (1..100) and honoured at the database level. |
| AC-43 | No query infers identity from display text, and reference + lot + machine alone never resolves a Tool. |
| AC-44 | Ambiguity stays explicit in every contracted query result (N matches ⇒ N items). |
| AC-45 | A blank reference or an empty search criterion set is a validation failure, never an empty result. |
| AC-46 | All contracted reads and writes go through the application layer; no module reads another module's tables. |

### Tool orchestration (AC-47 … AC-54)

| # | Criterion |
|---|---|
| AC-47 | One shared Tool search/select/create orchestration serves every P2-T04 surface; no per-surface fork exists. |
| AC-48 | A selection returns exactly one canonical `tool_id` for exactly one explicit human action. |
| AC-49 | The opaque P2-T03 origin token/candidate key is never declared, parsed, persisted or transported as a canonical identity; the opaque→canonical mapping is adapter state. |
| AC-50 | Cancel/return preserves the originating Job On state with no Tool selected. |
| AC-51 | A missing Tool can be created inline and the origin workflow resumes with the returned canonical `tool_id`. |
| AC-52 | Pre-fill proposes only already-known values; unknown Tool facts are never invented. |
| AC-53 | The origin module never becomes Tool-master owner and creates no second Tool registry. |
| AC-54 | The Tool create subflow does not navigate away from the origin surface; no draft store and no standalone create page exists. |

### Duplication (AC-55 … AC-64)

| # | Criterion |
|---|---|
| AC-55 | Duplication requires an explicit source and never uses the latest or the previous implicitly. |
| AC-56 | A historical source is selectable for duplication. |
| AC-57 | Duplication creates a new `jobon_id`. |
| AC-58 | Duplication creates new context ids and never reuses a source context id. |
| AC-59 | The source Job On and all its contexts are unchanged after duplication. |
| AC-60 | Copied Tool selections retain the same canonical `tool_id` values until explicitly changed. |
| AC-61 | Copied contexts carry the source context's frozen triple (no live re-read) until explicitly changed. |
| AC-62 | `copied_from_jobon_id` records the explicit source relation. |
| AC-63 | Duplication is atomic: a failure leaves no new Job On and no new context. |
| AC-64 | A stale source version refuses the duplication and creates nothing. |

### Create, edit and delete (AC-65 … AC-82)

| # | Criterion |
|---|---|
| AC-65 | Every mutation validates its inputs before any write. |
| AC-66 | Create writes the Job On and only the needed contexts in one transaction. |
| AC-67 | A context-creation failure leaves no partial Job On. |
| AC-68 | Edit changes exactly the four Job On facts and the three Tool associations. |
| AC-69 | Edit never exposes the frozen triple as an ordinary editable field. |
| AC-70 | With a reached/passed production date, an unacknowledged edit is refused with the confirmation-required reason — a warning gate, never hard immutability. |
| AC-71 | With explicit acknowledgement the edit proceeds and is persisted. |
| AC-72 | A stale version refuses the edit and writes nothing. |
| AC-73 | Every committed Job On mutation increments `version` exactly once; a read never increments it. |
| AC-74 | No edit changes a Job On status, because no status exists. |
| AC-75 | Delete requires explicit confirmation. |
| AC-76 | A reached/passed production date requires the stronger acknowledgement before deletion. |
| AC-77 | Delete is refused when any registered dependency probe reports a dependency, naming the dependency kinds. |
| AC-78 | The P2-T04 duplication lineage is a dependency: a Job On used as a source cannot be deleted. |
| AC-79 | A permitted delete removes this Job On's contexts and its own row in one transaction and touches nothing else. |
| AC-80 | No delete path cascades into operational history; every P2-T04 foreign key is `RESTRICT`. |
| AC-81 | A stale version refuses the delete and removes nothing. |
| AC-82 | A dependency contributed by another module blocks the delete without P2-T04 hardcoding that module's logic. |

### Routes, policy and access (AC-83 … AC-94)

| # | Criterion |
|---|---|
| AC-83 | Every P2-T04 route/endpoint declares exactly one canonical module policy through `ModuleAuthorizationPolicies.PolicyName`. |
| AC-84 | No `job-on-view` route accepts a mutation; Job On View gains no Create capability. |
| AC-85 | Every mutation route carries `job-on-create`. |
| AC-86 | Every Ferramentas route carries `ferramentas`; no P2-T04 route carries `ferramentas-approve`. |
| AC-87 | An unauthorized or session-less caller is denied server-side on the direct route — never a blank surface and never a success. |
| AC-88 | ADMIN gains no operational access through any P2-T04 route. |
| AC-89 | A Job On View-only caller is denied every Create route. |
| AC-90 | A Job On Create-only caller can operate create/edit/duplicate/delete plus its contracted reads. |
| AC-91 | No P2-T04 route is registered in the destination-route seam and no navigation entry appears. |
| AC-92 | `ModuleRegistrations.CurrentBuildAvailable` remains `[]`. |
| AC-93 | Ferramentas remains contextual-only: no destination id, no top-level navigation entry. |
| AC-94 | An unavailable module fails closed, so P2-T04 surfaces are denied in the production composition until P2-T10 registers availability. |

### Boundaries (AC-95 … AC-106)

| # | Criterion |
|---|---|
| AC-95 | No application code outside the contracted P2-T04 paths changed. |
| AC-96 | Migrations 001/002 are unmodified and `DmoDbContext.cs` is byte-identical. |
| AC-97 | No P2-T01/P2-T02/P2-T03 artifact is modified; no P2-T04 CSS was appended to `dmo-components.css`; frozen assets are unchanged. |
| AC-98 | No `production_id`, `job_on_revision_id`, reverse array or fake identity appears in the new schema, code or payloads. |
| AC-99 | No repairer entity/table/field, no machine→repairer assignment and no machine registry appears. |
| AC-100 | No P2-T05-or-later concept or artifact appears (Definições, settings, Peso, Pegamentos, Folha, Resumo, Boquilhas, documents/PDF, secondary navigation, availability registration, HISTÓRICO GLOBAL). |
| AC-101 | No `ModuleCatalog` change, no new policy, no availability change and no route registration occurs. |
| AC-102 | No mobile/tablet/breakpoint structural variant is introduced by any P2-T04 surface. |
| AC-103 | Exactly one new migration exists and it owns exactly the six contracted tables. |
| AC-104 | The P2-T04 schema adds no seventh table and no unrequested column. |
| AC-105 | All existing tests keep passing unmodified; none is weakened or deleted. |
| AC-106 | The migration applies to PostgreSQL and creates exactly the contracted tables, indexes and constraints on a disposable database. |

---

## Appendix A — Protected boundaries

### A.1 Must not be touched by P2-T04

The §17.2 list, plus: `DMO.slnx`, `Directory.Build.props`, `Directory.Packages.props`, every
`*.csproj`, `appsettings*.json`, `.env`, `src/DMO.Infrastructure/Configuration/*`,
`src/DMO.Infrastructure/Database/*`, `src/DMO.Infrastructure/Migrations/DmoDbContextModelSnapshot.cs`
(EF-generated extension only), `src/DMO.Web/Pages/Shared/**`,
`tests/DMO.IntegrationTests/Frontend/Shared/**`, `tests/DMO.UnitTests/Frontend/Shared/**`.

### A.2 Accepted input, consume only

`ModuleCatalog`, `ModuleRegistry`, `AccessResolver`/`AccessOutcome`/`ModuleResolve`,
`ModuleAccessService`, the module/administration gates, authentication/session/current account,
migrations 001/002, the Template model and its administration, USER administration, root
routing/landing/no-access, `NavigationProjectionService`, the destination-route seam, the shared
shell, shared tokens/shell CSS, the A1 freeze document, `ModuleRegistrations`, and every P2-T01/
P2-T02/P2-T03 shared contract, partial, asset and test.

### A.3 P2-T05+ boundary (must be preserved)

P2-T04 must not create, name, render, fixture or test: a Definições or settings surface, a repairer
or repairer register, a machine→repairer assignment, a Peso/Comparação/Pegamentos/Folha/Resumo
concept or record, a Controlo calculation, a Boquilhas aggregate/movement/balance/close/reopen
concept, a document/PDF/availability/email concept, secondary navigation, module availability
registration, a destination route or a HISTÓRICO (local or global) concept. Those belong to
P2-T05…P2-T10 and to their own authored contracts.

---

## Appendix B — Implementation file ownership / expected paths

### B.1 New domain types — `src/DMO.Domain/`

```text
Tools/ToolId.cs  Tools/ToolType.cs  Tools/Processo.cs  Tools/MachineCode.cs
Tools/ToolCompatibility.cs  Tools/Tool.cs  Tools/ToolContextType.cs
Tools/ToolContextSnapshot.cs  Tools/ToolContext.cs
JobOn/JobOnId.cs  JobOn/JobOn.cs
```

### B.2 New application contracts — `src/DMO.Application/`

```text
Repositories/IToolRepository.cs   Repositories/IJobOnRepository.cs
Tools/ToolModels.cs               Tools/ToolValidator.cs
Tools/IToolService.cs             Tools/ToolService.cs
JobOn/JobOnModels.cs              JobOn/JobOnValidator.cs
JobOn/IJobOnService.cs            JobOn/JobOnService.cs
JobOn/IJobOnDependencyProbe.cs
```

### B.3 New persistence — `src/DMO.Infrastructure/`

```text
Persistence/Entities/ToolEntity.cs            Persistence/Entities/ToolMachineEntity.cs
Persistence/Entities/JobOnEntity.cs           Persistence/Entities/CmContextEntity.cs
Persistence/Entities/MfContextEntity.cs       Persistence/Entities/BqContextEntity.cs
Persistence/EntityConfigurations/ToolEntityConfiguration.cs
Persistence/EntityConfigurations/ToolMachineEntityConfiguration.cs
Persistence/EntityConfigurations/JobOnEntityConfiguration.cs
Persistence/EntityConfigurations/CmContextEntityConfiguration.cs
Persistence/EntityConfigurations/MfContextEntityConfiguration.cs
Persistence/EntityConfigurations/BqContextEntityConfiguration.cs
Persistence/ToolRepository.cs                 Persistence/JobOnRepository.cs
Persistence/JobOnLineageDependencyProbe.cs
Migrations/<timestamp>_ToolJobOnDomainCore.cs (+ .Designer.cs, EF-generated)
Migrations/DmoDbContextModelSnapshot.cs       (EF-generated extension)
Persistence/PersistenceServiceCollectionExtensions.cs  (additive registrations only)
```

### B.4 New Web surfaces — `src/DMO.Web/`

```text
Endpoints/JobOnEndpoints.cs                Endpoints/FerramentasEndpoints.cs
Pages/JobOn/Index.cshtml(.cs)              Pages/JobOn/View.cshtml(.cs)
Pages/JobOn/Create.cshtml(.cs)             Pages/JobOn/Edit.cshtml(.cs)
Pages/JobOn/Duplicate.cshtml(.cs)
Pages/Ferramentas/Tool.cshtml(.cs)
wwwroot/css/dmo-jobon.css                  (new file; dmo-components.css untouched)
wwwroot/js/dmo-jobon.js                    (only if genuinely required; frozen assets untouched)
Program.cs                                 (additive registrations + Map…Endpoints only)
```

### B.5 New tests — `tests/`

```text
DMO.UnitTests/Tools/**                 DMO.UnitTests/JobOn/**
DMO.IntegrationTests/Tools/**          DMO.IntegrationTests/JobOn/**
DMO.IntegrationTests/Persistence/ToolRepositoryIntegrationTests.cs
DMO.IntegrationTests/Persistence/JobOnRepositoryIntegrationTests.cs
DMO.IntegrationTests/Persistence/JobOnDuplicationIntegrationTests.cs
DMO.IntegrationTests/Persistence/JobOnDeleteDependencyIntegrationTests.cs
DMO.IntegrationTests/Persistence/Migration003ToolJobOnDomainCoreTests.cs
```

### B.6 Planning / governance (this contract-authoring task only)

```text
plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md       (this file)
plans/BETA_IMPLEMENTATION_MASTER_PLAN.md                        (B1 register + P2-T04 status)
plans/beta-workstreams/P2-T04-DOMAIN-CORE-TOOL-JOBON.md         (§5 contract-authored record)
```

---

## Appendix C — Fixed desktop obligations

Every P2-T04 operational surface inherits the binding **DMO FIXED DESKTOP LAYOUT POLICY**
(master plan; A1 freeze §1A; P2-T02 §4; P2-T03 §6):

1. canonical design/validation viewport **1366 × 768**, including the real 768 px vertical
   constraint; compact operational density is mandatory;
2. structural composition and control locations are fixed per surface: the reference/productions
   list, the Job On sheet, the CM/MF/BQ association regions, the Tool picker region, the summary
   region and the action bar keep their assigned structural region;
3. **no breakpoint-driven structural or semantic variant**: no `@media`/`@container`/`@supports`
   structural rule and no width listener is introduced by P2-T04, and no control moves between
   regions because of width;
4. no table→card conversion, no required-column hiding, no action relocation, no side-panel stacking
   below the work area, no alternate mobile/tablet navigation;
5. larger desktop resolutions preserve the same composition (extra space becomes whitespace or
   limited non-structural expansion);
6. smaller windows preserve composition and use page-level or local scrolling; an over-wide region
   uses a keyboard-reachable local overflow container;
7. mobile, tablet, touch-first and phone-specific layouts are out of scope;
8. design priority order: workflow stability → predictable control placement → information density
   → readability → consistency → visual polish.

---

## Appendix D — Governance record

### D.1 Status recorded by this authoring task

| Item | Status |
|---|---|
| P2-T04 | **CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW** |
| B1 (`plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §4) | **AWAITING PLAN REVIEW** (not resolved; resolution requires Architect `PLAN ACCEPT`) |
| Implementation | **NOT STARTED — NOT AUTHORIZED** |
| Application code modified | NO |
| Migration created | NO |
| Supabase modified | NO |
| `ModuleRegistrations.CurrentBuildAvailable` | `[]` (unchanged) |
| `DestinationRouteRegistrations` / route registry | unchanged (still empty) |

### D.2 Governance files updated by this authoring task

| File | Update |
|---|---|
| `plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md` | this contract (new) |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §4 B1 row status; §7 P2-T04 contract-status record; Appendix A B1 row |
| `plans/beta-workstreams/P2-T04-DOMAIN-CORE-TOOL-JOBON.md` | §5 record that the B1 contract has been authored and awaits review |

`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` is **not** edited: it is a settled
authority record for the settings/repairer/email/PDF area, its §14.1 statement about B1 was correct
at its commit, and B1's status register lives in the master plan §4. Editing it would be outside its
own area.

### D.3 Baseline verification recorded by the authoring run

```text
build (`dotnet build DMO.slnx`)          : succeeded, 0 warnings, 0 errors
DMO.UnitTests                            : 468 passed / 0 failed / 0 skipped
DMO.IntegrationTests                     : 201 passed / 71 environment-gated skipped / 0 failed
migrations beyond 001/002                : 0
P2-T04 application code                   : absent
CurrentBuildAvailable                     : []  (asserted by the accepted regression test)
working tree (application code)           : clean before this authoring task
```

These are **developer-reported local execution results** of the existing suite, not independent CI
evidence (`WORKFLOW.md` "Testing evidence").

---

## Appendix E — PLAN REVIEW gate

Implementation may begin only when the Architect has:

1. reviewed **this** file at a recorded SHA;
2. dispositioned the 22 authority questions of §21 (all NON-BLOCKING with pinned defaults);
3. confirmed the physical schema of §3/§4, the tuples of §4.2, the frozen set of §7.2, the
   transaction boundaries of §10/§11 and the route/policy matrix of §13.2;
4. returned an explicit `PLAN ACCEPT` (or `CORRECTION REQUIRED` / `REJECT`) per
   `dmo-beta-master/WORKFLOW.md` step 6.

Until then:

```text
P2-T04 CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW
B1 AWAITING PLAN ACCEPT
NOT IMPLEMENTED
```

---

## 23. OWNER CLARIFICATION — JOB ON CONTEXT SNAPSHOT INVARIANT (SUPERSEDES THE AFFECTED DUPLICATION RULES)

**Authority:** direct OWNER clarification to the Job On / P2-T04 authority, issued after P2-T04 is
CLOSED, as a focused clarification of Job On context creation only. P2-T04 is **not reopened** as a
whole; P2-T07 is **not modified**. This section is a NEW OWNER CLARIFICATION and **supersedes every
affected duplication-related rule of this contract**: where any earlier section conflicts with this
section, THIS section wins. Everything else — the frozen set of §7.2, the edit/re-selection rules of
§7.4, historical immutability of §7.6, the create/edit/delete transactions of §11, the delete
dependency rule, the 14-route matrix, failure vocabulary, concurrency, the migration contract and
the protected files — is preserved as-is.

### 23.1 The rule (normative)

> **Every new Job On creates new component context snapshots from the current canonical Tool
> identities selected for that Job On. This applies both to normal creation and duplication.
> Duplication reuses `tool_id` identities, never `cm_id`/`mf_id`/`bq_id` context identities.**

1. **New `jobon_id` ⇒ new context identities.** Whenever a NEW Job On is created — from scratch or
   by duplicating a previous Job On — it receives NEW `cm_id` / `mf_id` / `bq_id` context rows.
   Context identities from another Job On are NEVER reused: new `jobon_id` ⇒ new
   `cm_id` / `mf_id` / `bq_id`.
2. **The snapshot source is the current canonical Tool row.** Each new context row snapshots the
   CURRENT canonical Tool row (through the canonical `tool_id` selected for that Job On) at the
   moment the new Job On is created. The snapshot is composed with the existing snapshot schema and
   the existing snapshot composition logic: the accepted frozen set
   `tool_type` / `tool_reference` / `tool_lot` of §7.2 — **no new snapshot fields are invented**.
3. **Duplication reuses `tool_id`, never the previous context.** When duplicating a Job On, the
   source context is used ONLY to obtain the canonical `tool_id` needed to locate the Tool. The new
   context row is created from the CURRENT Tool state, never by cloning the previous context row:
   the previous `cm_id`/`mf_id`/`bq_id` snapshot is NOT the source of truth for the new context.
   Normal creation and duplication converge on the SAME snapshot creation path.
   Example: source snapshot `tool_id_CM, reference '5447T173'`; the Tool later becomes reference
   `5447T173X`; duplicating the source creates the new context with `5447T173X`, not with
   `5447T173`.
4. **Historical contexts stay immutable.** Creating or duplicating a new Job On never updates the
   source `cm_id` / `mf_id` / `bq_id`, never rewrites their frozen triples and never touches the
   source Job On (version, `updated_at`, contexts). The new production receives new snapshots; the
   previous production keeps its historical snapshots exactly as they were.
5. **P2-T07 interaction.** The only relationship that matters here is: new Job On → new `bq_id`
   snapshot. Boquilhas movements remain associated with the `bq_id` of their own production
   (202601 `bq_id_A` holds 202601's movements; 202602 `bq_id_B` holds 202602's; both may point at
   the same canonical `tool_id_BQ`). Old Boquilhas movements are NEVER migrated to a new `bq_id`.

### 23.2 Superseded duplication wording (explicit list)

The following earlier readings are **REPLACED** as of this clarification:

1. former §10.3 step 6: "tool_type/tool_reference/tool_lot = the SOURCE CONTEXT's frozen triple
   (copied verbatim; the live Tool is NOT re-read — JOB_ON.md §8 'copy captured/manual values as
   reviewable starting values')" — replaced by a FRESH snapshot of the CURRENT canonical Tool row
   read through the source context's `tool_id`;
2. former §10.5 bullet "never re-reads the live Tool to 'refresh' the copied triple (§21 Q16
   records this pinned default)" — replaced by §23.1.3;
3. former §21 Q16 pinned default (a) "copy the source's frozen triple verbatim" — replaced by
   default (b) "re-read the live Tool" as re-pinned in §21;
4. former §20.4 row DUP7 ("duplication still copies the **source context's** frozen triple") and
   former §20.3 row CTX14 duplication wording — replaced by the current-state snapshot reading.

`JOB_ON.md` §8 "copy captured/manual values as reviewable starting values" is read, for the frozen
context triple, as: the operator's duplication action IS the selection moment for the new
production; the new production starts from the CURRENT canonical Tool state, and the historical
source snapshot stays untouched as reviewable history. The operator can always explicitly
re-select (§7.4) after duplication.

### 23.3 Deliberately NOT introduced

Keep this a simple Job On snapshot invariant. NOT introduced: `production_id`,
`job_on_revision_id`, any rollover entity, any Tool history table, any snapshot version entity,
any context reuse, any reverse array on Tool, and any synchronization framework. No migration, no
schema change, no new table, no new column, no new route and no new domain concept. The
implementation of this clarification is a service-level correction of the duplication snapshot
path plus focused tests; the existing context schema and the existing snapshot composition logic
are reused unchanged.
