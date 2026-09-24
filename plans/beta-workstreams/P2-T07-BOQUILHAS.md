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

Deliver the Boquilhas module as the **production-linked movement register** settled by the Owner
clarification (contract §33): one register per real `bq_id` / Job On context, exactly three
movement types (`saida | entrada | entrada_sem_reparacao`), movement editing with audit, derived
outstanding and History. **No standalone flow, no lifecycle, no close/reopen** (see §16–§17).

## 2. Authority

- `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §3, §4, §5, §6, §11 — **settled
  authority for the repairer model in this handoff**: the repairer register is owned by
  `Controlo_Create → Definições` (name is the only required data); `B1`,`B2`,`B3`,`C1`,`C2`,`C3`
  each hold an **independent** repairer assignment with no grouping rule; Boquilhas resolves the
  repairer **automatically** from the machine's current assignment; the repairer actually used is
  **historically preserved**; and the Job-On-dependent machine sidebar is **removed** from current
  visual authority.
- `dmo-beta-master/modules/BOQUILHAS.md` (full) — identity, flows, movement vocabulary, edit
  audit, balance, business date, close/reopen, repairer, history, acceptance. **SUPERSEDED /
  HISTORICAL ONLY for the affected rules:** the standalone flow, the `Início`/`Irreparável`
  vocabulary and the close/reopen lifecycle in that module document were superseded by the Owner
  clarification (contract §33) — see §16.
- `dmo-beta-master/architecture/RECORD_LIFECYCLES.md` §9 — Boquilhas lifecycle (affected rules
  superseded by contract §33, see §16).
- `dmo-beta-master/architecture/CROSS_MODULE_FLOWS.md` — Job On → Boquilhas (the "standalone
  validity" trace is superseded by contract §33, see §16).
- `dmo-beta-master/implementation/BETA_INTEGRATION_SEAMS.md` — Workstream E; "E → B" seam.
- `dmo-master/global/ACCESS_MODEL.md` §1/§11 — Boquilhas is one assignable module.
- `dmo-master/global/INFORMATION_MODEL.md` §"Boquilhas" — `movement_id -> boquilhas_id -> bq_id
  -> tool_id + jobon_id` (production-linked branch; the standalone variant is superseded by
  contract §33, see §16); `bq_id` is distinct from `boquilhas_id`.

> **Terminology (binding — master plan §0):** the History in this workstream is **HISTÓRICO
> (local)** — history functionality *inside* the module. It is not the top-level
> **HISTÓRICO GLOBAL** module (technical identity `historia`), which is DEFERRED BY DESIGN for
> this Beta. Do not merge the two, and do not treat one as authority for the other.

## 3. Current implementation starting point

Implemented under the accepted contract (`b884dd84…`) and corrected per the Owner clarification
(contract §33): migration 007 `BoquilhasDomain` (corrected cleanly pre-closure to the final
three-table schema), 15 routes (3 pages + 12 endpoints) gated `dmo.module.boquilhas`, full unit
and integration suites green (see §16–§17). Built on P2-T04 for canonical BQ Tool
selection/create and `bq_id` resolution, and on the shell/components from P2-T01…P2-T03.

## 4. Scope (FINAL — CLOSED model)

Boquilhas is a **production-linked movement register** per the Owner clarification
(contract §33):

1. Search/select/create BQ Tool context (shared Tool orchestration; returns to origin).
2. **Production-linked flow (final):** `boquilhas_id -> bq_id -> jobon_id + tool_id`. One register
   per real `bq_id` / Job On context; movements remain valid **after** the production end date.
   **No permanent standalone** (the former permanent `boquilhas_id -> tool_id` flow is
   SUPERSEDED — §16). **Provisional pré-JobOn `tool_id` anchor allowed** by the Owner
   clarification (contract §34 / §4.1 below): work may start on the canonical `tool_id` and, on a
   `bq_id` with the same master `tool_id`, the register is associated and becomes `bq_id →
   jobon_id`. No fake Job On/`bq_id`; the register identity is created WITHOUT any quantity event.
3. Exactly the **three** write movement types: `saida` (Saída), `entrada` (Entrada),
   `entrada_sem_reparacao` (Entrada sem reparação). `Editar` is an **action** on an existing
   movement — **never** a movement type. No `inicio` / `irreparavel`.
4. Movement forms and validation; recent movements.
5. Full **History** with filters (reference, lot, line, business date/period, movement type,
   repairer, register/file state, pagination); single click selects, double click opens.
6. **Edit** preserves before/after audit, authenticated user and system timestamp **without** a
   second quantity event or double balance effect.
7. **`business_date`** (editable) distinct from **`recorded_at`** (immutable).
8. **Derived outstanding:** `SUM(saida) − SUM(entrada) − SUM(entrada_sem_reparacao)` from
   movement facts alone (no second mutable balance authority). Excess Entrada recorded (not
   clamped/rejected); negative outstanding visible and non-blocking. `% utilização` is manual
   and never derived from movements.
9. **No lifecycle:** no active/closed/reopen, no close snapshots, no reopening records, no
   opening-facts surface (all SUPERSEDED — §16).
10. Canonical **`repairer_id`** stored on Saída with historical retention.
11. Production-line contextual panel **reading** (not owning) production context.
12. **Automatic repairer resolution** (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`
    §4, §5, §6): when a registration/movement is associated with a machine, the repairer is
    resolved from that machine's **current** assignment —
    `machine → current repairer assignment → repairer resolved`. The operator should not normally
    have to manually choose the repairer when the machine is already known. `B1`,`B2`,`B3`,`C1`,
    `C2`,`C3` resolve **independently**; there is no shared B/C repairer and no "Linha B"/"Linha C"
    model. The repairer used at the time of the movement is **historically preserved**: changing a
    machine's assignment later must **never** rewrite an earlier Boquilhas record. *(Ownership
    note — **SUPERSEDED** by the Owner clarification (contract §34 / §4.1 below): Boquilhas no
    longer merely "consumes" the register — the repairer register and the line/machine → repairer
    associations **belong to `Boquilhas > Definições`**, not to Controlo, not to Admin. The
    resolution mechanics and shape rules above are unchanged.)*
13. **Removed from current visual authority:** the Boquilhas **machine/sidebar** concept that
    depends on Job On operational context is removed from the current frontend authority
    (`…DELTA.md` §11). Do **not** simulate Job On machine/reference state inside Boquilhas. If
    future Job On integration justifies such context, it may be reintroduced later from real
    backend authority. The production-line contextual panel (item 11) is a different, read-only
    reading of real supplied context and is unchanged.

### 4.1 Owner clarification — pré-JobOn anchor and repairer ownership (recorded)

A NEW OWNER CLARIFICATION (registered on the clean baseline; contract **§34**; record
`dev/responses/OWNER_CLARIFICATION_PLANNING_CONTEXT_AND_ASSOCIATIONS_RESPONSE.md`) supersedes the
affected §33 wording and registers as current authority — **authority only, no implementation**,
P2-T07 remains CLOSED, the current build (3 tables, 15 routes, movement model) is unchanged:

1. **Boquilhas may start work pré-JobOn, provisionally anchored on the canonical `tool_id`** (a
   real `tools` row, type `BQ`; never a fake Job On/bq_id/minted identity).
2. **The association point is the matching `bq_id`:** when Boquilhas receives a BQ context whose
   `bq_id` references the **same master `tool_id`** (`bq_contexts.tool_id` = the register's
   canonical `tool_id`), that is the point to **present/resolve the association** (human-confirmed,
   mirroring the accepted Peso associate pattern of P2-T05 §4.4 — anchor must match, no inference
   rule).
3. **After resolution the register becomes `bq_id → jobon_id`** (provisional `tool_id` anchor
   cleared; recorded movements keep their facts; nothing migrated/rewritten/replayed).
4. **This does NOT recreate a permanent standalone:** the `tool_id` anchor is transitional only;
   the settled production-linked register stands; lifecycle/close/reopen/balance machinery stays
   removed (§33 preserved otherwise).
5. **Repairers and the line/machine → repairer associations belong to `Boquilhas >
   Definições`**, not to Controlo, not to Admin — superseding the affected §3/§4/§6 wording
   ("Boquilhas consumes the register and does not administer it"; "repairer directory
   administration … owned by Controlo_Create → Definições"). Data-shape rules unchanged (name-only,
   no delete, independent per-machine assignment, no grouping, current-state with historical
   preservation). PDF/email settings stay with Controlo.
6. The provisional-anchor flow and the Definições re-homing require future authored,
   reviewed contracts (separate PLAN ACCEPT) — **not authorized here**.

## 5. Authority blocker B3 — required contract before execution

**Status: B3 CLOSED** — the required contract was authored, PLAN ACCEPT-ed and implemented; the
blocker description below is **HISTORICAL / SUPERSEDED ONLY** (its close/reopen representation
requirement was superseded by contract §33, see §16).

The authored, reviewed contract had to fix: the aggregate/movement schema and keys
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
- **Repairer directory administration** — *(historical ownership reading; **SUPERSEDED** by the
  Owner clarification §4.1.5: the repairer register and the line/machine → repairer associations
  **belong to `Boquilhas > Definições`**, not to Controlo, not to Admin; the current build keeps
  the implemented tables/routes until a future workstream contract re-homes them.)*
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

## 9. Backend / persistence requirements (FINAL)

Per B3/§33. Exactly **three** tables — `boquilhas`, `boquilha_movements`,
`boquilha_movement_audit` — in ONE corrected migration 007 (`20260924051151_BoquilhasDomain`;
23 product tables / 24 raw). No lifecycle tables (`boquilha_close_snapshots`,
`boquilha_reopenings`, `boquilha_machines`), no `status` column, no active-anchor partial unique
indexes; one register per real BQ context (plain unique key); `repairer_id` relation; no
reverse-ID arrays; movement facts are the sole outstanding authority. The repairer actually used
must remain readable on the historical record after the machine's current assignment changes; the
machine assignments themselves are **read** from the Controlo_Create → Definições configuration
and are not owned here.

## 10. Required tests

See master plan §11 P2-T07: exactly three movement types (`saida`, `entrada`,
`entrada_sem_reparacao`); `Editar` is not a type (no `inicio`/`irreparavel`); outstanding
constraints (replay, never stored); excess Entrada recorded; negative outstanding non-blocking;
`% utilização` not derived; `business_date` != `recorded_at`; edit audit without a second
movement; production-linked flow without fake identities; one register per real `bq_id` / Job On
context (no quantity event at register creation); movements remain valid after the production end
date; repairer historical retention.

Additionally (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §4, §5, §6, §11): a
registration/movement associated with a machine resolves the repairer automatically and does not
force re-selection; the six machines resolve independently and changing one assignment changes no
other machine's resolution; a later assignment change leaves an earlier record's repairer
unchanged; Boquilhas cannot administer the repairer register; no machine/reference sidebar is
rendered.

## 11. Acceptance criteria

The register is production-linked only (one register per real `bq_id` / Job On context; no
standalone flow, no fake Job On/`bq_id`); the movement selector exposes exactly the three types;
`Editar` is not a type; edit adds audit without a second movement; movements remain valid after
the production end date; outstanding equals `SUM(saida) − SUM(entrada) −
SUM(entrada_sem_reparacao)` from movement facts alone; no mandatory PDF; no settings tab; no
lifecycle/close/reopen structures; automatic repairer resolution from the machine's current
independent assignment; historical repairer preservation; no machine sidebar;
`CurrentBuildAvailable` unchanged.

## 12. Completion evidence

Committed implementation + tests + the final independent review `a96814f…` **VERIFIED**
(`reports/P2_T07_FINAL_INDEPENDENT_REVIEW.md`) — **P2-T07 CLOSED** (see §17).

## 13. Downstream dependents

P2-T08, P2-T10.

## 14. Contract-authored record

> **HISTORICAL / SUPERSEDED ONLY.** The contracted model recorded in §14–§15 (standalone flow,
> four movement types incl. Início, balance buckets, close/reopen, six tables, 18 routes,
> active-anchor B1 machinery) was **superseded by the Owner clarification, contract §33** (see
> §16); the workstream is **CLOSED** (see §17). The record is kept for provenance only and is NOT
> current authority.

**Status at the time: IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION
REVIEW**
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

> **HISTORICAL / SUPERSEDED ONLY** — see the §14 call-out and §16/§17.

**Status at the time: IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION
REVIEW.**

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

The workstream was **NOT closed at that time**: it awaited ONE independent review of the
corrected P2-T07 — which then returned **VERIFIED** and closed it (see §17).
`ModuleRegistrations.CurrentBuildAvailable` remains `[]`.
**P2-T08 / P2-T10 remain NOT IMPLEMENTED / NOT AUTHORIZED.**

## 16. Owner clarification — the production movement register (correction record)

> **APPLIED + VERIFIED — see §17 for the final status.** This record describes the Owner
> clarification (contract §33) that superseded the §14–§15 model; **the current authority is the
> final model below and §17.**

A NEW OWNER CLARIFICATION (contract §33; implementation-response §21) **supersedes the affected
rules** of the accepted contract — the old independent-verification gate was NOT run before this
correction. Superseded: the standalone Boquilhas flow, the Início movement type, the Irreparável
movement semantics, the open/closed lifecycle (status/close/reopen/close snapshots/reopening
history), the B1 active-aggregate machinery (the ACTIVE partial unique indexes, their 23505
mapping, the active pre-check, the K6–K8 race rows — NOT replaced by any lock) and the four-bucket
balance model.

Final model: Boquilhas is a **historical movement register associated with a REAL production** —
`boquilhas_id → bq_id → jobon_id + tool_id` (production-linked only; movements valid AFTER the
production end date); exactly THREE movement types `saida | entrada | entrada_sem_reparacao`
(Saída / Entrada / Entrada sem reparação; Editar stays an action); the outstanding is derived by
replay (`Σ Saída − Σ Entrada − Σ Entrada sem reparação`), never stored; the register identity is
created WITHOUT any quantity event; edit/audit, dates, repairer resolution + historical
preservation, shared Tool orchestration, Histórico, fixed desktop and the
`dmo.module.boquilhas` gate are preserved.

Correction executed: migration 007 was **corrected cleanly pre-closure** (the unreviewed
20260924031924 pair replaced by 20260924051151_BoquilhasDomain): final schema **THREE tables**
(`boquilhas` with the plain one-register-per-BQ-context unique key, `boquilha_movements` with the
closed three-type + Saída-required CHECKs, `boquilha_movement_audit`); the lifecycle tables
(`boquilha_close_snapshots`, `boquilha_reopenings`, `boquilha_machines`), the `status` column and
every lifecycle-only index are gone; **23 product tables / 24 raw**; final route count **15 = 3
pages + 12 endpoints** (close/reopen/opening-facts/standalone routes removed).

Verification (fresh disposable PostgreSQL 16.15): build 0 errors; **unit 658/658**; **integration
633 passed / 0 failed / 2 pre-existing live-Supabase skips**; focused Boquilhas 67/67 (production
association incl. one-register-per-BQ-context, production-ended movements, 3-type vocabulary,
replay incl. the Owner example → 0, Entrada sem reparação semantics, edit/audit single-event +
stale refusals, repairer history, schema facts, real Down/re-apply); 9 node-adapter behavioral
scenarios PASS; auth negatives and negative-scope scans green; `CurrentBuildAvailable` stays `[]`.

**NEXT GATE (now closed):** one independent review of the corrected P2-T07, then close if
VERIFIED — **COMPLETED: the final independent review `a96814f…` returned VERIFIED and closed
P2-T07** (see §17). **P2-T08 / P2-T10 remain NOT IMPLEMENTED / NOT AUTHORIZED.**

## 17. Final status — CLOSED

**Status: IMPLEMENTED + OWNER CLARIFICATION CORRECTION APPLIED — VERIFIED — CLOSED.**

- Final independent review: `reports/P2_T07_FINAL_INDEPENDENT_REVIEW.md` at commit `a96814f…`
  — verdict **VERIFIED**; recommends **CLOSED**; under the simplified workflow **no additional
  Architect implementation review is required** (the review found no concrete architectural
  defect). The old 83 AC / 86-row matrix was deliberately not reconstructed — the review
  verified the final §33 model.
- Final model (current authority): **production-linked movement register** — one register per
  real `bq_id` / Job On context (`boquilhas_id → bq_id → jobon_id + tool_id`); movements remain
  valid after the production end date; exactly **three** movement types
  `saida | entrada | entrada_sem_reparacao`; outstanding derived by replay
  (`SUM(saida) − SUM(entrada) − SUM(entrada_sem_reparacao)`); **no lifecycle** (no
  active/closed/reopen), **no standalone**, **no close/reopen**; **3 tables** (`boquilhas`,
  `boquilha_movements`, `boquilha_movement_audit`); **15 routes** (3 pages + 12 endpoints), all
  gated `dmo.module.boquilhas`.
- `ModuleRegistrations.CurrentBuildAvailable` remains `[]`; no availability or route registered
  (P2-T10 owns that).
- **P2-T08 / P2-T10 remain NOT IMPLEMENTED / NOT AUTHORIZED.**
