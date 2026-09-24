# P2-T07 — Boquilhas — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T07), §8, §9, §10, §11, §12.
Class: **Operational module** (Workstream E).
Depends on: **P2-T04** (canonical BQ Tool selection/create and optional `bq_id` resolution) and
**P2-T03** (`MeasurementRows`/`DecisionBar` where applicable).
Authority blocker: **B3** — an authored, reviewed `PLAN ACCEPT` contract must exist first.

## Binding fixed desktop layout

This handoff inherits the master plan's **DMO FIXED DESKTOP LAYOUT POLICY**. Boquilhas
operational tables, filters, movement actions, balance/status information and History surface are
designed first at **1366 × 768** with compact density and stable locations. Larger desktops
preserve that composition. Smaller windows use page or local scrolling; tables do not become
cards, required columns stay visible and actions do not move because of width. Mobile/tablet
variants are out of scope.

> **Scope note.** The **machine/sidebar** concept is **removed from current visual authority**
> (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §11): it depended on a Job On
> operational context that does not exist in the current surface. Implement **no** side panel
> that simulates Job On machine/reference state. The layout policy above still governs every
> surface that remains (tables, filters, actions, balance/status, History, and the read-only
> production-line contextual panel).

## 1. Purpose

Deliver the Boquilhas module: aggregate create (production-linked and standalone), the four
movements, movement editing with audit, derived balance, close/reopen and History.

## 2. Authority

- `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §3, §4, §5, §6, §11 — **settled
  authority for the repairer model in this handoff**: the repairer register is owned by
  `Controlo_Create → Definições` (name is the only required data); `B1`,`B2`,`B3`,`C1`,`C2`,`C3`
  each hold an **independent** repairer assignment with no grouping rule; Boquilhas resolves the
  repairer **automatically** from the machine's current assignment; the repairer actually used is
  **historically preserved**; and the Job-On-dependent machine sidebar is **removed** from current
  visual authority.
- `dmo-beta-master/modules/BOQUILHAS.md` (full) — identity, flows, movement vocabulary, edit
  audit, balance, business date, close/reopen, repairer, history, acceptance.
- `dmo-beta-master/architecture/RECORD_LIFECYCLES.md` §9 — Boquilhas lifecycle.
- `dmo-beta-master/architecture/CROSS_MODULE_FLOWS.md` — Job On → Boquilhas; standalone validity.
- `dmo-beta-master/implementation/BETA_INTEGRATION_SEAMS.md` — Workstream E; "E → B" seam.
- `dmo-master/global/ACCESS_MODEL.md` §1/§11 — Boquilhas is one assignable module.
- `dmo-master/global/INFORMATION_MODEL.md` §"Boquilhas" — `movement_id -> boquilhas_id -> bq_id
  -> tool_id + jobon_id`, and the standalone variant; `bq_id` is distinct from `boquilhas_id`.

> **Terminology (binding — master plan §0):** the History in this workstream is **HISTÓRICO
> (local)** — history functionality *inside* the module. It is not the top-level
> **HISTÓRICO GLOBAL** module (technical identity `historia`), which is DEFERRED BY DESIGN for
> this Beta. Do not merge the two, and do not treat one as authority for the other.

## 3. Current implementation starting point

Nothing operational exists (no `boquilha`/`movement` occurrence in `src/`). Depends on P2-T04 for
canonical BQ Tool selection/create and `bq_id` resolution, and on the shell/components from
P2-T01…P2-T03.

## 4. Scope

1. Search/select/create BQ Tool context.
2. **Production-linked flow:** `boquilhas_id -> bq_id -> jobon_id + tool_id`.
3. **Standalone flow:** `boquilhas_id -> tool_id` with **no** fake Job On/`bq_id`.
4. Active aggregate summary.
5. Exactly the **four** write movement types: `Início`, `Saída`, `Entrada`, `Irreparável`.
   `Editar` is an **action** on an existing movement — **never** a fifth movement type.
6. Movement forms and validation; recent movements.
7. Full **History** with filters (reference, lot, line, business date/period, movement type,
   repairer, aggregate/file state, pagination); single click selects, double click opens.
8. **Edit** preserves before/after audit, authenticated user and system timestamp **without** a
   second quantity event or double balance effect.
9. **`business_date`** (editable) distinct from **`recorded_at`** (immutable).
10. **Derived balance** buckets Disponível / Em reparação / Irreparável / Entrada excecional
    from movement facts (no second mutable balance authority). Saída ≤ available;
    Irreparável ≤ in-repair; excess Entrada recorded (not clamped/rejected); negative saldo
    visible and non-blocking. `% utilização` is manual and never derived from movements.
11. **Close/reopen** on the same `boquilhas_id`: immutable close snapshot; recorded reopen
    actor/time/reason; a failed close leaves the active state unchanged.
12. Canonical **`repairer_id`** stored on external Saída with historical retention.
13. Production-line contextual panel **reading** (not owning) production context.
14. **Automatic repairer resolution** (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`
    §4, §5, §6): when a registration/movement is associated with a machine, the repairer is
    resolved from that machine's **current** assignment —
    `machine → current repairer assignment → repairer resolved`. The operator should not normally
    have to manually choose the repairer when the machine is already known. `B1`,`B2`,`B3`,`C1`,
    `C2`,`C3` resolve **independently**; there is no shared B/C repairer and no "Linha B"/"Linha C"
    model. The repairer used at the time of the movement is **historically preserved**: changing a
    machine's assignment later must **never** rewrite an earlier Boquilhas record. Boquilhas
    **consumes** the register and does not administer it.
15. **Removed from current visual authority:** the Boquilhas **machine/sidebar** concept that
    depends on Job On operational context is removed from the current frontend authority
    (`…DELTA.md` §11). Do **not** simulate Job On machine/reference state inside Boquilhas. If
    future Job On integration justifies such context, it may be reintroduced later from real
    backend authority. The production-line contextual panel (item 13) is a different, read-only
    reading of real supplied context and is unchanged.

## 5. Authority blocker B3 — required contract before execution

The authored, reviewed contract must fix: the aggregate/movement schema and keys
(`boquilhas_id`, `movement_id`, `repairer_id`); the balance derivation rule; the edit/audit
representation; the close/reopen representation; the History filter/query shapes; transactional
boundaries; and the endpoint/route names with their module policy.

It must **also** fix — consistently with `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`:
the `repairer_id` schema shape; how the machine's current assignment is read for automatic
resolution; and the mechanism by which the repairer used at the time of the movement is
**historically preserved** (choose the pattern the existing architecture already establishes for
retained historical facts — minimum relation, minimum snapshot; do **not** invent a new
mechanism, and do **not** prescribe one in this handoff). It must **not** invent repairer fields
beyond name, a machine grouping rule, or a locally simulated Job On machine state.

The directory source half of B3 is **settled** by the delta (§3.6): the register lives in
`Controlo_Create → Definições`. The physical contract remains open.

## 6. Explicit non-scope

- Mandatory Boquilhas PDF; internal Boquilhas settings/Admin tab.
- **Repairer directory administration** — the register is owned by `Controlo_Create → Definições`;
  Boquilhas only selects/consumes a repairer.
- **Any machine/reference sidebar simulating Job On context** (`…DELTA.md` §11).
- Job On planning ownership; Armazém stock/location; per-piece BQ identity.
- Obsolete legacy movement types.
- No `CurrentBuildAvailable` change and no route registration.

## 7. Expected files/projects

```text
src/DMO.Domain/                                   (movement/aggregate primitives)
src/DMO.Application/Boquilhas/                    (use cases + repository contracts)
src/DMO.Infrastructure/Persistence/ + Migrations/ (Boquilhas schema, NEW migration)
src/DMO.Web/Pages/Boquilhas/
src/DMO.Web/Endpoints/
tests/DMO.UnitTests/  tests/DMO.IntegrationTests/
```

## 8. Access requirements

`ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas)` on routes/actions; navigation
availability remains a projection only.

## 9. Backend / persistence requirements

Per B3. Aggregate + movements + edit/audit history + close snapshot + reopen record;
`repairer_id` relation; no reverse-ID arrays; movement facts are the sole balance authority.
The repairer actually used must remain readable on the historical record after the machine's
current assignment changes; the machine assignments themselves are **read** from the
Controlo_Create → Definições configuration and are not owned here.

## 10. Required tests

See master plan §11 P2-T07: four movement types only; `Editar` is not a type; balance
constraints; excess Entrada recorded; negative saldo non-blocking; `% utilização` not derived;
`business_date` != `recorded_at`; edit audit without a second movement; both flows without fake
identities; close/reopen same id with full history; failed close no-op; repairer historical
retention.

Additionally (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §4, §5, §6, §11): a
registration/movement associated with a machine resolves the repairer automatically and does not
force re-selection; the six machines resolve independently and changing one assignment changes no
other machine's resolution; a later assignment change leaves an earlier record's repairer
unchanged; Boquilhas cannot administer the repairer register; no machine/reference sidebar is
rendered.

## 11. Acceptance criteria

Every bullet in `modules/BOQUILHAS.md` "Acceptance criteria"; the movement selector exposes only
the four types; no mandatory PDF; no settings tab; no replacement aggregate on close/reopen;
automatic repairer resolution from the machine's current independent assignment; historical
repairer preservation; no machine sidebar; `CurrentBuildAvailable` unchanged.

## 12. Completion evidence

Committed implementation + tests + explicit confirmation that close/reopen creates no
replacement aggregate.

## 13. Downstream dependents

P2-T08, P2-T10.

## 14. Contract-authored record

**Status: IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW**
(contract-authoring and B1-correction tasks recorded in §14–§15 below; the implementation is
executed per §15).

The implementation contract is `plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md` (authoring
response `dev/responses/P2_T07_CONTRACT_AUTHORING_RESPONSE.md`). It fixes — over the accepted,
**CLOSED** P2-T04/P2-T05 state and the settled delta — the identity core (DB-enforced exclusive
anchor `boquilhas_id → bq_id` production-linked | `boquilhas_id → tool_id` standalone; no fake
Job On/`bq_id`; no `production_id`/reverse arrays), the shared Tool orchestration consumption
(BQ-only candidates, no auto-select, contextual create returning to origin, no second
registry), exactly the four movement types (`Editar` never a type; Início created with the
aggregate), the replay-derived balance buckets (no second mutable balance authority; Saída ≤
available and Irreparável ≤ in-repair as 409 refusals; excess Entrada recorded; negative saldo
non-blocking), edit-with-audit on the same `movement_id` (no double balance effect;
`movement_type`/`recorded_at` immutable), `business_date` ⊥ `recorded_at`, repairer consumption
(automatic resolution from the machine's current independent assignment; final selected
`repairer_id` stored with historical retention; no administration), close/reopen on the same
`boquilhas_id` (immutable snapshot; atomic failed close; recorded reopen), the manual
`% utilização` still, the local Histórico (backend filters; HISTÓRICO GLOBAL boundary), SIX new
tables in ONE additive migration (007), exactly 18 routes all gated `dmo.module.boquilhas`, and
a complete test-to-acceptance matrix (**83 AC ↔ 86 rows; missing 0, dangling 0, orphan 0** —
the B1-correction rows K6–K8 cover concurrent create/create and create/reopen races) with
12 NON-BLOCKING authority questions carrying pinned defaults (Q-EDIT-FIELDS, Q-MACHINE,
Q-INICIO, Q-EXCESS, Q-ORDER, Q-CREATE, Q-REOPEN-ELIG, Q-REFLOT, Q-UTIL, Q-LINE, Q-CLOSE-DATE,
Q-ANUL) — **0 BLOCKING, 0 REQUIRES OWNER DECISION**.

The Architect PLAN review (dmo-work `542a08a1…`) returned **PLAN REJECT — blocking finding B1
only**: the one-active-aggregate-per-anchor refusal was not race-safe as contracted. The B1
correction (partial unique indexes `IX_boquilhas_active_bq_id`/`IX_boquilhas_active_tool_id` +
exact 23505 → `Refused(ActiveAggregateExists)` mapping + race-safe create/reopen semantics) is
applied at the corrected contract commit (`b884dd84…`).

## 15. Implementation record

**Status: IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW.**

The focused Architect PLAN re-review (dmo-work `7c2479ebae50f8fe18a770fd373cffaae65a48e1`)
returned **PLAN ACCEPT** — implementation AUTHORIZED against the corrected contract
`b884dd84…` only. The P2-T07 implementation is executed per
`dev/responses/P2_T07_IMPLEMENTATION_RESPONSE.md`: migration 007 `BoquilhasDomain` (the SIX
contracted tables; the two ACTIVE partial unique active-anchor backstops; the scoped 23505 →
`Refused(ActiveAggregateExists)` mapping), the movement ledger + replay-derived balance, the
edit+audit single-event semantics, close/reopen on the SAME `boquilhas_id`, the local
Histórico, 18 routes all gated `dmo.module.boquilhas`, and the full P2-T07 matrix (83 AC ↔ 86
rows incl. K6/K7/K8 REAL PostgreSQL races). Full unit + integration suites green on a fresh
disposable PostgreSQL.

The workstream is **NOT self-verified and NOT closed**: it awaits independent verification and
the Architect implementation review. `ModuleRegistrations.CurrentBuildAvailable` remains `[]`.
**P2-T08 / P2-T10 remain NOT AUTHORIZED.**
