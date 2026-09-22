# P2-T02 — `DenseDataTable` + `AuditTrail` (A4) — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T02), §11, §12.
Class: **Shared Beta primitive** (Workstream A, sub-step A4).
Depends on: **P2-T01** (common states).
Authority blocker: **none**.

## Binding fixed desktop layout

This handoff inherits the master plan's **DMO FIXED DESKTOP LAYOUT POLICY** and the shared
frontend contract's fixed desktop section.

- Canonical design and browser-validation surface: **1366 × 768**.
- `DenseDataTable` uses compact rows, stable required columns, stable column order and
  predictable row/action locations.
- It never converts to cards, hides required columns or moves actions because of a breakpoint.
- An over-wide table uses a keyboard-reachable local horizontal scroll container.
- `AuditTrail` uses the same fixed desktop composition and remains structurally identical at
  larger desktop resolutions.
- Smaller windows scroll rather than trigger structural reflow; mobile/tablet variants are out
  of scope.

## 1. Purpose

Implement the shared dense table and audit-trail presentation that Job On history, Controlo
lists, Boquilhas History and document/audit rendering depend on. Removes the reconciliation's
§7.1 PARTIAL for these two components.

## 2. Authority

- `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` §9 (`DenseDataTable`), §12 (`AuditTrail`)
  — primary repository authority (frozen, accepted).
- `dmo-beta-master/contracts/SHARED_FRONTEND.md` §9, §12.
- `dmo-beta-master/IMPLEMENTATION_MODEL.md` Workstream A.
- `dmo-beta-master/ACCEPTANCE_MATRIX.md` §3 (no synthesized attribution), §11.
- `workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md` §14 "A4" and §10.3 browser checks.

## 3. Current implementation starting point

No table and no audit presentation exists. `wwwroot/js` does not exist. P2-T01's state
vocabulary and `dmo-components.css` are the base.

## 4. Scope

1. **`DenseDataTable`**:
   - single click selects; double click **and** Enter open **only where the consumer supplies**
     the open behavior;
   - Space selects;
   - two activations of the same open collapse to one invocation;
   - explicit keyboard focus traversal; focus survives re-render;
   - the selected state is a programmatic fact (e.g. `aria-selected`), not color-only;
   - loading/empty/error states remain the P2-T01 distinct states;
   - filters and pagination are consumer-owned (the component renders what it is given);
   - horizontal overflow is keyboard reachable and does not trap focus;
   - the component constructs no URL and triggers no domain mutation on selection.
2. **`AuditTrail`**:
   - renders only supplied actor / timestamp / action / optional before-after detail;
   - preserves the consumer-supplied ordering;
   - **never** synthesizes attribution from the current session;
   - never replaces a missing historical actor/time with the current user/time.

## 5. Explicit non-scope

- No fetching, backend filtering/sorting/paging, URL construction, domain sorting.
- No mutation, authorization, automatic navigation.
- No audit creation, timestamp generation or diff calculation.
- No feature-specific column set or terminology.

## 6. Expected files/projects

```text
src/DMO.Web/Frontend/Shared/Contracts/            (table/audit presentation models)
src/DMO.Web/Pages/Shared/Components/              (Razor partials/view components)
src/DMO.Web/wwwroot/css/dmo-components.css        (additive)
src/DMO.Web/wwwroot/js/dmo-dense-table.js         (new, generic selection/open only)
src/DMO.Web/wwwroot/js/dmo-focus.js               (new, generic focus helpers)
tests/DMO.UnitTests/Frontend/Shared/              (new tests)
tests/DMO.IntegrationTests/Frontend/Shared/       (new rendered tests)
```

## 7. Backend / access / persistence requirements

None.

## 8. Required tests

- **U:** click selects without opening; double-click/Enter opens only when supplied; Space
  selects; duplicate open collapses to one.
- **U:** `AuditTrail` never invents actor/time.
- **UI:** keyboard traversal; focus survives re-render; selected state is programmatic; empty
  vs `lookup-failed` remain distinct.
- **R:** `SharedShellTests`, `NavigationProjectionServiceTests` unchanged; full suite passes.

## 9. Acceptance criteria

1. Selection and open are separate behaviors, each failing-if-removed tested.
2. The component constructs no consumer URL and mutates nothing on selection.
3. No attribution synthesis is observable.
4. At 1366 × 768, required columns remain stable and rows remain compact; over-width content
   uses keyboard-reachable local horizontal scrolling.
5. At larger desktop resolutions, table, AuditTrail and action placement remain structurally
   identical.
6. No table-to-card conversion, breakpoint-driven required-column hiding, action relocation or
   mobile/tablet variant exists.
7. `CurrentBuildAvailable` still `[]`; no route changed.
8. No protected file modified.

## 10. Completion evidence

Committed components + tests; recorded build/test results; `git diff --name-only` limited to
A-owned paths.

## 11. Downstream dependents

P2-T04 (Job On history), P2-T05/P2-T06 (Controlo lists), P2-T07 (Boquilhas History), P2-T08.

## 12. Regression guard

Do not modify protected foundation; do not weaken existing tests.

## 13. Authored contract (planning gate)

The formal implementation contract for this workstream is
`plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md`, with authoring response
`dev/responses/P2_T02_CONTRACT_AUTHORING_RESPONSE.md`.

Status: **CONTRACT AUTHORED — AWAITING ARCHITECT PLAN ACCEPT.** Implementation is not authorized
and has not started. This handoff remains the scope summary; the contract is the binding
implementation specification once the Architect returns `PLAN ACCEPT`.
