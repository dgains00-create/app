# P2-T07 — Boquilhas — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T07), §8, §9, §10, §11, §12.
Class: **Operational module** (Workstream E).
Depends on: **P2-T04** (canonical BQ Tool selection/create and optional `bq_id` resolution) and
**P2-T03** (`MeasurementRows`/`DecisionBar` where applicable).
Authority blocker: **B3** — an authored, reviewed `PLAN ACCEPT` contract must exist first.

## Binding fixed desktop layout

This handoff inherits the master plan's **DMO FIXED DESKTOP LAYOUT POLICY**. Boquilhas
operational tables, filters, movement actions, balance/status information, side panel and
History surface are designed first at **1366 × 768** with compact density and stable locations.
Larger desktops preserve that composition. Smaller windows use page or local scrolling; tables
do not become cards, required columns stay visible, the side panel does not move below content
and actions do not move because of width. Mobile/tablet variants are out of scope.

## 1. Purpose

Deliver the Boquilhas module: aggregate create (production-linked and standalone), the four
movements, movement editing with audit, derived balance, close/reopen and History.

## 2. Authority

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

## 5. Authority blocker B3 — required contract before execution

The authored, reviewed contract must fix: the aggregate/movement schema and keys
(`boquilhas_id`, `movement_id`, `repairer_id`); the balance derivation rule; the edit/audit
representation; the close/reopen representation; the History filter/query shapes; transactional
boundaries; and the endpoint/route names with their module policy.

## 6. Explicit non-scope

- Mandatory Boquilhas PDF; internal Boquilhas settings/Admin tab.
- Repairer directory administration.
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

## 10. Required tests

See master plan §11 P2-T07: four movement types only; `Editar` is not a type; balance
constraints; excess Entrada recorded; negative saldo non-blocking; `% utilização` not derived;
`business_date` != `recorded_at`; edit audit without a second movement; both flows without fake
identities; close/reopen same id with full history; failed close no-op; repairer historical
retention.

## 11. Acceptance criteria

Every bullet in `modules/BOQUILHAS.md` "Acceptance criteria"; the movement selector exposes only
the four types; no mandatory PDF; no settings tab; no replacement aggregate on close/reopen.

## 12. Completion evidence

Committed implementation + tests + explicit confirmation that close/reopen creates no
replacement aggregate.

## 13. Downstream dependents

P2-T08, P2-T10.
