# P2-T03 — `ToolPicker` + `ToolSummaryRow` + `MeasurementRows` + `DecisionBar` (A5/A6) — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T03), §11, §12.
Class: **Shared Beta primitive** (Workstream A, sub-steps A5/A6).
Depends on: **P2-T01**.
Authority blocker: **none** for the *presentation/mechanics*. The Tool **orchestration** that
consumes this picker belongs to P2-T04 and is authority-blocked there (B1).

## 1. Purpose

Implement the shared picker, summary row, measurement-row mechanics and decision bar that
Job On, Controlo Create, Controlo Approve and Boquilhas consume. Removes the reconciliation's
§7.1 PARTIAL for these four components.

## 2. Authority

- `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` §6/§7 (`ToolPicker`), §8
  (`ToolSummaryRow`), §13 (`MeasurementRows`), §14 (`DecisionBar`), §2 (carrier shapes are
  **not** canonical identities) — primary repository authority (frozen, accepted).
- `dmo-beta-master/contracts/SHARED_FRONTEND.md` §7, §8, §13, §14.
- `dmo-beta-master/IMPLEMENTATION_MODEL.md` Workstream A.
- `dmo-beta-master/modules/FERRAMENTAS_LIGHT.md` — explicit tool selection, no guessing.
- `dmo-beta-master/ACCEPTANCE_MATRIX.md` §2 (no auto-selection), §3 (no invented nominal),
  §5 (a failed lookup must not select a Tool).
- `workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md` §14 "A5"/"A6", §10.3.

## 3. Current implementation starting point

None of these components exists. `wwwroot/js` does not exist.

## 4. Scope (presentation and generic mechanics ONLY)

1. **`ToolPicker`** — explicit search state + candidate list; **never auto-selects**, including
   when exactly one candidate exists; ambiguous candidates stay separate; `Criar ferramenta`
   appears only when the consumer supplies it enabled; cancel returns to origin without
   selection; Enter on search never selects the first result; Escape requests cancel and never
   discards origin work without consumer confirmation; close/return restores invoking-control
   focus; consumer-supplied origin state is preserved through an opaque token.
2. **`ToolSummaryRow`** — renders supplied type/reference/lot/machines/quantity/process/status/
   actions; separates missing-optional from zero/false; performs no lookup or inference.
3. **`MeasurementRows`** — stable frontend row identity across add/remove/re-render;
   configurable minimum including at-least-one; removal disabled with a visible associated
   reason when the minimum would be violated; focus moves to the first editable control of a new
   row and to the nearest surviving control after removal; **no** measurement field, formula,
   tolerance or domain rule.
4. **`DecisionBar`** — primary/secondary/danger groups; visible disabled reason; pending state;
   duplicate invocation prevented; action order follows supplied order; **no**
   approve/reject/reopen/submit/close/movement rule encoded.

## 5. Explicit non-scope

- No Tool search algorithm, ranking, compatibility rule or auto-selection.
- No Tool creation, canonical identity, CM/MF/BQ association, persistence, endpoint or
  authorization.
- No domain validation, measurement schema, formula, tolerance or nominal value.
- Opaque carrier keys are **never** declared to be `tool_id`/`jobon_id` or any canonical
  identity.

## 6. Expected files/projects

```text
src/DMO.Web/Frontend/Shared/Contracts/            (picker/summary/rows/decision models)
src/DMO.Web/Pages/Shared/Components/              (Razor partials/view components)
src/DMO.Web/wwwroot/css/dmo-components.css        (additive)
src/DMO.Web/wwwroot/js/dmo-tool-picker.js         (new, picker presentation only)
src/DMO.Web/wwwroot/js/dmo-measurement-rows.js    (new, generic row mechanics only)
tests/DMO.UnitTests/Frontend/Shared/              (new tests)
tests/DMO.IntegrationTests/Frontend/Shared/       (new rendered tests)
```

## 7. Backend / access / persistence requirements

None.

## 8. Required tests

- **U:** never-auto-select including a single candidate; Enter on search does not select;
  Escape does not discard origin work; cancel restores origin with the opaque token preserved.
- **U:** `MeasurementRows` minimum enforcement (incl. at-least-one) with the exposed
  removal-disabled reason; stable row identity across edits.
- **U:** `DecisionBar` duplicate-invocation prevention while pending; disabled reason retained.
- **U/architecture:** no shared contract type or fixture declares an opaque key as a canonical
  identity.
- **UI:** focus rules for new/removed rows; focus returns to the invoking control after picker
  close/cancel.
- **R:** `SharedFrontendSource_DoesNotImportFeatureServices` remains green; full suite passes.

## 9. Acceptance criteria

1. Every A1-frozen `ToolPicker` interaction rule has a dedicated failing-if-removed test.
2. Minimum-row enforcement exposes the reason and blocks removal.
3. No canonical identity string appears in any shared contract type or fixture.
4. No feature semantics are encoded in any of the four components.
5. `CurrentBuildAvailable` still `[]`; no route changed.
6. No protected file modified.

## 10. Completion evidence

Committed components + tests; recorded build/test results; `git diff --name-only` limited to
A-owned paths.

## 11. Downstream dependents

P2-T04 (Tool orchestration + picker wiring), P2-T05 (`MeasurementRows` in Peso/Pegamentos),
P2-T06/P2-T07 (`DecisionBar` actions).

## 12. Regression guard

Do not modify protected foundation; do not weaken existing tests.
