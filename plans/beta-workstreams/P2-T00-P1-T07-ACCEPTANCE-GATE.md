# P2-T00 — P1-T07 Acceptance Gate (governance) — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T00).
Class: **Governance** (no application code).
Depends on: nothing.

## 1. Purpose

Close the reconciliation-recorded P1-T07 accepted-status gap so the shell may be relied upon
as accepted foundation. This is **not** a feature task and changes **no** application code.

## 2. Authority

- `dmo-beta-master/SOURCE_MANIFEST.md` — `P1-T07 navigation + USER shell implementation commit
  0b476909366…` recorded `IMPLEMENTED / NOT YET ACCEPTED`; the implementation review path
  `dmo-work/dev/reviews/P1-T07_NAVIGATION_USER_SHELL_IMPLEMENTATION_REVIEW.md` is `NOT FOUND`.
- `dmo-beta-master/implementation/CURRENT_FOUNDATION.md` — "P1-T07 implementation exists !=
  P1-T07 accepted implementation"; "Until an Architect implementation review is committed, Beta
  work must treat the implementation commit as current code state, not an accepted architectural
  result."
- `dmo-beta-master/WORKFLOW.md` — step 12: "Architect inspects the actual commit/diff and
  returns ACCEPT / ACCEPT WITH MINOR CORRECTION / CORRECTION REQUIRED / REJECT".
- `dmo-work/dev/reviews/P1-T07_NAVIGATION_USER_SHELL_CORRECTED_PLAN_REVIEW.md` — the accepted
  corrected plan this implementation must be judged against (PLAN ACCEPT, §31.1 fail-closed).
- `dmo-work/dev/responses/P1-T07_NAVIGATION_USER_SHELL_IMPLEMENTATION_RESPONSE.md` — evidence of
  what was implemented (not acceptance).

## 3. Current implementation starting point

- Shell work is present in `main`. The reconciliation baseline is `7910f56…`, carrying
  `0b476909…` as its parent.
- Files implementing P1-T07: `src/DMO.Web/Pages/Index.cshtml(.cs)`,
  `Pages/Login.cshtml(.cs)`, `Pages/AccessDenied.cshtml(.cs)`,
  `Pages/Administration/Index.cshtml(.cs)`,
  `src/DMO.Web/Navigation/{LandingSelector,UserLandingService,DestinationRouteRegistrations}.cs`,
  `src/DMO.Web/Auth/SessionLoginService.cs`,
  `src/DMO.Web/Frontend/Shell/ShellPresentationModels.cs` (additive `LandingDestinationId`),
  `src/DMO.Web/Frontend/Shell/NavigationProjectionService.cs` (propagation only),
  `src/DMO.Application/Access/AccessOutcome.cs` (additive `LandingDestinationId`),
  `src/DMO.Application/Access/AccessResolver.cs` (propagation only),
  `src/DMO.Application/Access/ModuleAccessService.cs` (mechanical arity).
- Existing evidence: `tests/DMO.IntegrationTests/Navigation/{RootRoutingTests,NoAccessPageTests,
  LoginPageTests,DirectRouteEnforcementTests,UserLandingPersistenceIntegrationTests}.cs`,
  `tests/DMO.UnitTests/Navigation/{LandingSelectorTests,UserLandingServiceTests}.cs`,
  `tests/DMO.UnitTests/Frontend/Shared/NavigationProjectionServiceTests.cs`.

## 4. Scope

1. Inspect the committed diff of the P1-T07 implementation against the accepted corrected plan.
2. Verify each plan-settled behavior exists in the committed source (not merely in the response
   document): root routing, landing selection incl. invalid-explicit fail-closed, `/Login`
   public surface + dispatch, `/AccessDenied` 403 + non-disclosure, `/Administration` gating,
   direct-route enforcement, `CurrentBuildAvailable` still `[]`, no provisional fixtures.
3. Produce the Architect implementation review with an explicit verdict.

## 5. Explicit non-scope

- Any change to `src/**` or `tests/**`.
- Rebuilding, refactoring or "improving" the shell, landing, login or no-access behavior.
- Re-opening the settled §31.1 fail-closed decision.
- Changes to `ModuleCatalog`, `ModuleRegistrations`, `DestinationRoutes` or any authorization
  policy.

## 6. Expected artefact

A review document in the **governance** repository (`diogo-o/dmo-work`,
`dev/reviews/P1-T07_NAVIGATION_USER_SHELL_IMPLEMENTATION_REVIEW.md`), citing the inspected SHA
and the exact files/diff. **No** file is added to `DMO-MODULAR/src` or `DMO-MODULAR/tests`.

## 7. Acceptance criteria

1. The review exists, cites the inspected implementation SHA, and states one of
   `ACCEPT` / `ACCEPT WITH MINOR CORRECTION` / `CORRECTION REQUIRED` / `REJECT`.
2. The verdict is grounded in the committed diff and the existing committed test source.
3. The review confirms: `/` is account-aware with exact redirects; invalid explicit persisted
   landing fails closed with no fallback and no Template mutation; `_PrimaryNavigation` renders
   `Sem destinos operacionais disponíveis` when there are no live destinations; ADMIN receives
   no operational destinations; denied USER yields `AccessResolutionFailed = true`; no
   `A2Fixtures`/`PROVISIONAL FRONTEND CONTRACT` exists in production; `CurrentBuildAvailable`
   is `[]`.
4. If the review requires a correction, that correction must state the exact defect and the
   minimum boundary — a correction may **not** authorize a shell rewrite.

## 8. Required tests

None added. The existing P1-T07 test set is the evidence base and must be cited by name. If a
correction is required, its tests belong to the correction slice, not to this gate.

## 9. Completion evidence

- The committed review document with the inspected SHA.
- Explicit statement that no application code changed.

## 10. Downstream dependents

- Removes the caveat on P1-T07 as "accepted foundation" for P2-T10 and for any workstream that
  cites the shell as accepted.

## 11. Regression guard

Nothing may weaken or delete the existing P1-T07 tests. `CurrentBuildAvailable` must remain
`[]` after this workstream.
