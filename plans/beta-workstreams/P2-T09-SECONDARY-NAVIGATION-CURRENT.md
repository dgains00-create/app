# P2-T09 — Secondary Navigation + Current-Destination Wiring — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T09), §11, §12.
Class: **Shared Beta primitive** (shared shell extension).
Depends on: nothing.
Authority blocker: **none**.

## 1. Purpose

Close the reconciliation's §7.2 PARTIAL: `SecondaryDestinationPresentation` and `IsCurrent`
already exist and are honored by the partials, but **no producer** populates them
(`ShellPresentationService.BuildAsync` always passes `[]` and never sets `IsCurrent`). This
workstream is strictly additive wiring — the record shapes and partials are preserved.

## 2. Authority

- `reports/BETA_MASTER_RECONCILIATION.md` §7.2 (the recorded PARTIAL).
- `workbench/BETA_FRONTEND_IMPLEMENTATION_WORKSTREAMS.md` §4 — A owns "primary destination and
  local secondary navigation patterns" and active state.
- `workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md` §9.2 "Active state and destination
  behavior" and §5.1/§5.2 ownership.
- `dmo-beta-master/architecture/ACCESS_AND_NAVIGATION.md` — navigation is a projection of
  effective access; presentation never decides access.

## 3. Current implementation starting point

- `src/DMO.Web/Frontend/Shell/ShellPresentationModels.cs` — `SecondaryDestinationPresentation`
  and `IsCurrent` already exist.
- `src/DMO.Web/Pages/Shared/Navigation/_PrimaryNavigation.cshtml` already honors `IsCurrent`
  (`class="is-current"`, `aria-current="page"`).
- `src/DMO.Web/Pages/Shared/Navigation/_SecondaryNavigation.cshtml` already renders a supplied
  list and guards `Model.Count > 0`.
- `src/DMO.Web/Frontend/Shell/ShellPresentationService.cs` currently always passes `[]` and
  never sets `IsCurrent` — this is the missing producer.

## 4. Scope

1. Extend `ShellPresentationService` so a consuming surface can supply secondary destinations.
2. Derive `IsCurrent` for primary destinations from the request path against the
   **already-projected** live destinations (no new authority, no new access logic).
3. Keep the existing record shapes and partials; change them only if a wiring defect is proven.

## 5. Explicit non-scope

- No change to `NavigationProjectionService` projection filters or the shared-destination
  collapse algorithm (protected).
- No new destination authority; no second navigation composer.
- No feature pages, no route registration, no `CurrentBuildAvailable` change.
- No authorization decision in presentation.

## 6. Expected files/projects

```text
src/DMO.Web/Frontend/Shell/ShellPresentationService.cs   (additive)
src/DMO.Web/Frontend/Shell/ShellPresentationModels.cs    (additive, only if required)
src/DMO.Web/Pages/Shared/Navigation/*.cshtml             (only if a wiring defect is proven)
tests/DMO.UnitTests/Frontend/Shared/                     (new tests)
tests/DMO.IntegrationTests/Frontend/Shared/              (new tests)
```

## 7. Backend / access / persistence requirements

None.

## 8. Required tests

- **U:** a supplied secondary list is rendered; `IsCurrent` marks exactly the matching primary
  destination; an empty secondary list renders nothing.
- **I:** ADMIN and a denied USER still project zero operational destinations.
- **R:** `RootRoutingTests`, `NoAccessPageTests`, `LoginPageTests`, `DirectRouteEnforcementTests`
  unchanged; full suite passes.

## 9. Acceptance criteria

1. Current-destination marking derives only from the projected destination set.
2. No authorization decision is taken in presentation.
3. Empty secondary navigation renders nothing (no empty container).
4. `CurrentBuildAvailable` still `[]`; no route changed.
5. The existing partials and record shapes are preserved (or a proven wiring defect is
   documented).

## 10. Completion evidence

Committed additive wiring + tests; recorded build/test results; `git diff` demonstrating the
projection algorithm is untouched.

## 11. Downstream dependents

P2-T04, P2-T05, P2-T06, P2-T07 (local navigation), P2-T10.

## 12. Regression guard

Do not modify `NavigationProjectionService`, `ModuleCatalog`, `ModuleRegistrations`, the module
gates, or existing tests.
