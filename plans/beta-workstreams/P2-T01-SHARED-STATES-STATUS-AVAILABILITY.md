# P2-T01 — Shared Generic States + `RecordStatus` + `AvailabilityState` (A3) — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T01), §11, §12.
Class: **Shared Beta primitive** (Workstream A, sub-step A3).
Depends on: nothing.
Authority blocker: **none** — the A1 freeze is Architect-accepted and canonicalized in Beta.

## 1. Purpose

Implement the shared, presentation-only state vocabulary and the `RecordStatus` and
`AvailabilityState` components that every operational module consumes. This removes the
reconciliation's §7.1 PARTIAL for these three capabilities.

## 2. Authority

- `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` §4 (state vocabulary), §10
  (`RecordStatus`), §11 (`AvailabilityState`) — **primary repository authority** (frozen,
  Architect-accepted).
- `dmo-beta-master/contracts/SHARED_FRONTEND.md` "Common state vocabulary" and the mandated
  distinctions `empty != lookup-failed`, `empty != unavailable`, `empty != permission-denied`;
  §10, §11.
- `dmo-beta-master/IMPLEMENTATION_MODEL.md` Workstream A — ownership of generic UI states.
- `dmo-beta-master/ACCEPTANCE_MATRIX.md` §3, §11 — anti-inference and fail-closed presentation.
- `workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md` §14 "A3" and §5.1 path ownership.

## 3. Current implementation starting point

- `src/DMO.Web/Frontend/Shared/` contains only `SharedFrontendExtensions.cs`.
- `Pages/Shared/` contains only `_Layout`, `_PublicLayout`, `_Identity`, `Navigation/*`.
- `wwwroot/css` has `dmo-tokens.css`, `dmo-shell.css`, `dmo-user-shell.css`,
  `dmo-admin-templates.css`, `dmo-admin-users.css` — no `dmo-components.css`.
- No component, no `Frontend/Shared/Contracts/`, no `Pages/Shared/Components/`, no
  `wwwroot/js`.
- The shell already renders a fail-closed status and an empty-navigation state
  (`ShellPresentationService`, `_PrimaryNavigation.cshtml`); those are protected and unchanged.

## 4. Scope

1. **Common state region** — a shared presentation model + rendering for
   `loading`, `ready`, `empty`, `lookup-failed`, `unavailable`, `permission-denied`, `saving`,
   `submitting`, `stale`, `conflict`. `empty`, `lookup-failed`, `unavailable` and
   `permission-denied` must be **mutually distinct** renderings.
2. **`RecordStatus`** — supplied status text is **always** rendered; optional tone is
   supplementary and never color-only; unknown supplied status uses the neutral tone; status
   grants no action and infers no lifecycle.
3. **`AvailabilityState`** — the **eight** states `available`, `not-generated`,
   `awaiting-approval`, `workspace-unavailable`, `file-missing`, `versions-available`,
   `not-applicable`, `lookup-failed`, each with explicit text. `not-applicable` is not an
   error; `file-missing` is not `not-generated`; `lookup-failed` is never rendered as
   no-file/no-record.
4. New A-owned component CSS in **`wwwroot/css/dmo-components.css`** (do not rewrite existing
   selectors).
5. Additive registration only through `SharedFrontendExtensions`.

## 5. Explicit non-scope

- No domain status catalogue, lifecycle rule, approval meaning or authorization.
- No storage/filesystem lookup, no document generation, no module-specific terminology.
- No measurement field, formula, tolerance or nominal value.
- No change to `_Layout`, `_PrimaryNavigation`, `ShellPresentationService`,
  `NavigationProjectionService`, `dmo-tokens.css`, `dmo-shell.css` (protected, §12 of the plan).
- No `CurrentBuildAvailable` or route registration.

## 6. Expected files/projects

```text
src/DMO.Web/Frontend/Shared/Contracts/            (new) presentation models
src/DMO.Web/Pages/Shared/Components/              (new) shared Razor partials/view components
src/DMO.Web/wwwroot/css/dmo-components.css        (new)
src/DMO.Web/Frontend/Shared/SharedFrontendExtensions.cs   (additive registration only)
tests/DMO.UnitTests/Frontend/Shared/              (new tests)
tests/DMO.IntegrationTests/Frontend/Shared/       (new rendered tests)
```

## 7. Backend / access / persistence requirements

None. The components are presentation-only and never decide access or persist anything.

## 8. Required tests

- **U:** all ten states representable and semantically distinct; the four mandated
  distinctions hold.
- **U:** `RecordStatus` always carries text; unknown status selects neutral; grants no action.
- **U:** `AvailabilityState` keeps all eight states distinct; `not-applicable` not styled as
  error; `file-missing` != `not-generated`; `lookup-failed` != empty.
- **UI:** rendered markup exposes visible text (not color-only); disabled reasons are
  programmatically associated.
- **R:** the full existing suite still passes; `SharedShellTests` and
  `NavigationProjectionServiceTests` behavior unchanged.

## 9. Acceptance criteria

1. All ten states render distinct, textual, non-color-only output.
2. `empty`, `lookup-failed`, `unavailable`, `permission-denied` are never the same rendering.
3. An unknown `RecordStatus` renders neutrally and guesses no tone.
4. No shared contract type references a feature namespace or service.
5. Any modelled disabled action exposes an associated supplied reason.
6. `ModuleRegistrations.CurrentBuildAvailable` is still `[]`; no route changed.
7. No protected file (§12 of the master plan) was modified.

## 10. Completion evidence

Committed components + tests; recorded `dotnet build DMO.slnx` and test results from the
implementing agent; explicit `git diff --name-only` showing only A-owned paths.

## 11. Downstream dependents

P2-T02, P2-T03, P2-T04, P2-T05, P2-T06, P2-T07, P2-T08 (all consume states/status/availability).

## 12. Regression guard

Do not reintroduce provisional fixtures. Do not modify protected foundation. Do not weaken or
delete existing tests.
