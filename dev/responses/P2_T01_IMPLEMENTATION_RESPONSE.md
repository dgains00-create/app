# P2-T01 — SHARED GENERIC STATES + `RecordStatus` + `AvailabilityState` (A3) — IMPLEMENTATION RESPONSE

## 1. Baseline

| Item | Value |
|---|---|
| Starting DMO-MODULAR SHA | `1d73219456abb11160718cd1641eb433777ebc1e` (`main`, clean tree) |
| Handoff used | `plans/beta-workstreams/P2-T01-SHARED-STATES-STATUS-AVAILABILITY.md` |
| Master plan | `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T01), §11, §12, Appendix A |
| Reconciliation reference | `reports/BETA_MASTER_RECONCILIATION.md` §7.1, §8.1/8.2/8.3 |
| dmo-beta-master authority | `78da49248f6cf7a8cbe4ddd946f3c38abbaf322f` |
| .NET SDK in environment | **AVAILABLE** — `10.0.401` |

## 2. Authority

| Authority | Section used |
|---|---|
| `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` | §3 rules (1, 4, 5, 6, 9), §4 (ten-state vocabulary + mandated distinctions), §10 (`RecordStatus`), §11 (`AvailabilityState`) |
| `dmo-beta-master/contracts/SHARED_FRONTEND.md` | "Common state vocabulary" (`empty != lookup-failed`, `empty != unavailable`, `empty != permission-denied`), `RecordStatus` ("understandable without color"; "does not invent state machines"), `AvailabilityState` ("Must distinguish situations such as…"; "Optional missing output is not automatically an error") |
| `dmo-beta-master/contracts/DOCUMENTS_AND_FILES.md` | §5 availability meanings, documents/versions |
| `dmo-beta-master/IMPLEMENTATION_MODEL.md` | Workstream A — ownership of generic UI states |
| `dmo-beta-master/ACCEPTANCE_MATRIX.md` | anti-inference and fail-closed presentation |
| `dmo-master/global/ACCESS_MODEL.md` | presentation does not decide access |
| `workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md` | §14 "A3"; §5.1 path ownership |

## 3. Changes Made

New A-owned paths only. No protected file (§12 of the master plan) was modified.

### 3.1 New presentation contracts — `src/DMO.Web/Frontend/Shared/Contracts/`

| File | Purpose |
|---|---|
| `CommonState.cs` | the ten frozen presentation states |
| `CommonStateTraits.cs` | presentation traits: `CssToken`, `IsBusy`, `IsAssertive`, `RequiresAssociatedReason`, `MutuallyDistinctFromEmpty`, `All` |
| `CommonStateRegionPresentation.cs` | common-state region model; distinct `Empty`/`LookupFailed`/`Unavailable`/`PermissionDenied` factories; optional wrapping state |
| `StatusTone.cs` | generic optional tone (neutral/info/success/warning/danger) |
| `RecordStatusPresentation.cs` | supplied text always rendered; unknown tone → neutral; no actions, no lifecycle |
| `AvailabilityState.cs` | the eight frozen availability outcomes |
| `AvailabilityTraits.cs` | `CssToken`, `IsError`, `IsNeutral`, `AllowsVersions`, `AllowsRetry`, `RequiresDetail`, supplied-carrier validation |
| `AvailabilityPresentation.cs` | availability model; explicit text + detail; supplied versions/actions |
| `AvailabilityVersionPresentation.cs` | opaque consumer-supplied version item |
| `SharedActionPresentation.cs` | generic action; a disabled action must carry a supplied reason |

### 3.2 New shared component partials — `src/DMO.Web/Pages/Shared/Components/`

| File | Purpose |
|---|---|
| `_CommonStateRegion.cshtml` | renders one state; distinct token/heading/live-region; assertive announcement for `lookup-failed`/`conflict`; busy marking; associated reason; wrapping state preserved without replacing the outcome |
| `_RecordStatus.cshtml` | renders supplied text plus supplementary tone/marker (`aria-hidden`); no actions |
| `_AvailabilityState.cshtml` | renders the eight outcomes with explicit text; versions and actions only where the state admits them |

### 3.3 New A-owned stylesheet

`src/DMO.Web/wwwroot/css/dmo-components.css` — new file, scoped to `.dmo-state*`,
`.dmo-status*`, `.dmo-availability*` and the shared `.visually-hidden` helper. It consumes only
the tokens published in `dmo-tokens.css` (`var(--dmo-*)`); **no existing selector was rewritten**
and `dmo-tokens.css`/`dmo-shell.css`/`dmo-user-shell.css` were not touched.

### 3.4 Additive registration

None was required: `SharedFrontendExtensions` already calls `AddRazorPages()`, which discovers
`Pages/Shared/Components/*.cshtml`. The seam was therefore left byte-identical
(`git diff` on `SharedFrontendExtensions.cs` is empty), which is the minimal additive outcome.

### 3.5 Tests

| Path | Contents |
|---|---|
| `tests/DMO.UnitTests/Frontend/Shared/CommonStateVocabularyTests.cs` | ten states; distinct tokens; the four mandated distinctions; busy/assertive/reason traits; undefined-state rejection |
| `tests/DMO.UnitTests/Frontend/Shared/RecordStatusPresentationTests.cs` | text always present; unknown tone → neutral; grants no action/lifecycle; no feature-namespace reference |
| `tests/DMO.UnitTests/Frontend/Shared/AvailabilityPresentationTests.cs` | eight states distinct; `not-applicable` not an error; `file-missing != not-generated`; `lookup-failed` not no-file/no-record; versions/versions-on-failure rules |
| `tests/DMO.UnitTests/Frontend/Shared/SharedActionPresentationTests.cs` | disabled action requires a supplied reason; opaque key is not a canonical identity |
| `tests/DMO.IntegrationTests/Frontend/Shared/SharedComponentRenderer.cs` | renders the **real compiled** partials through the test host's `IRazorViewEngine` |
| `tests/DMO.IntegrationTests/Frontend/Shared/SharedStatesRenderingTests.cs` | rendered-markup assertions: four distinct states, assertive/busy announcement, associated disabled reasons, neutral unknown status, eight availability tokens, `not-applicable != lookup-failed` styling, CSS asset resolution, and a live `/Login` regression guard |

### 3.6 Documentation slice (ledger rows 8.1/8.2/8.3)

Updated to describe the current system rather than the P1-T01/P1-T02 skeleton:
`src/DMO.Application/README.md`, `src/DMO.Infrastructure/README.md`, `src/DMO.Web/README.md`.

## 4. Acceptance Criteria Evidence

| # | Criterion | Evidence |
|---|---|---|
| 1 | All ten states render distinct, textual, non-color-only output | `CommonStateVocabularyTests.Vocabulary_HasExactlyTheTenFrozenStates`, `EveryState_HasADistinctCssToken_…`; `SharedStatesRenderingTests` asserts visible message text per state |
| 2 | `empty`, `lookup-failed`, `unavailable`, `permission-denied` never the same rendering | `CommonStateVocabularyTests.MatchedDistinctions_…`, `Region_TheFourDistinctSurfaces_…`; `SharedStatesRenderingTests.CommonState_FourDistinctStates_RenderDistinctTextualMarkup` |
| 3 | Unknown `RecordStatus` renders neutrally and guesses no tone | `RecordStatusPresentationTests.UnknownSuppliedTone_UsesNeutralAndNeverGuesses`; `SharedStatesRenderingTests.RecordStatus_UnknownStatus_RendersNeutralToneWithVisibleText` |
| 4 | No shared contract type references a feature namespace or service | `RecordStatusPresentationTests.NoSharedContractTypeReferencesAFeatureNamespaceOrService` |
| 5 | Any modelled disabled action exposes an associated supplied reason | `SharedActionPresentationTests.DisabledAction_WithoutSuppliedReason_Throws`; `SharedStatesRenderingTests.CommonState_DisabledAction_ReasonIsProgrammaticallyAssociated`, `Availability_DisabledAction_ReasonIsProgrammaticallyAssociated` |
| 6 | `ModuleRegistrations.CurrentBuildAvailable` still `[]`; no route changed | `ModuleRegistrations.cs` unchanged; no `Pages/*.cshtml.cs` route added; `git diff` shows no route/registry change |
| 7 | No protected file (§12) modified | §6 below |

## 5. Verification

Environment: .NET SDK `10.0.401`, Debian trixie. ICU was resolved locally and the SDK ran with
globalization enabled (no `InvariantGlobalization` weakening).

| Command | Result |
|---|---|
| `dotnet build DMO.slnx -c Debug` | **Build succeeded** — `0 Warning(s)`, `0 Error(s)` |
| `dotnet test tests/DMO.UnitTests` | **Passed** — Failed 0, **Passed 374**, Skipped 0 |
| `dotnet test tests/DMO.IntegrationTests` | **Passed** — Failed 0, **Passed 115**, Skipped 71 |
| `git diff --check` | clean |
| migrations changed | 0 |
| provisional markers in `src/` (`A2Fixtures`, `PROVISIONAL FRONTEND CONTRACT`) | 0 |

Baseline before P2-T01 was unit 335 / integration 102 passed. P2-T01 adds **39 unit** and
**13 integration** tests; the pre-existing 335/102 all still pass (criterion R). The 71
integration skips are the environment-gated disposable-PostgreSQL and live DEV/TEST Supabase
cases and are intentional.

Evidence class per `WORKFLOW.md` "Testing evidence": **developer/local execution evidence**.
The repository has no `.github/` workflow, so no independent CI evidence exists. The committed
test source is the durable evidence base.

No test was weakened or deleted.

## 6. Protected Foundation

| Protected area (§12) | Status |
|---|---|
| Canonical 13-module vocabulary (`ModuleCatalog.cs`) | unchanged |
| Registry validation (`ModuleRegistry.cs`) | unchanged |
| Fail-closed access resolution (`AccessResolver`, `AccessOutcome`) | unchanged |
| Per-module facade (`ModuleAccessService`) | unchanged |
| Server-side module gate + ADMIN gate | unchanged |
| Authentication/session/accounts/current-account | unchanged |
| Persistence foundation + migrations 001/002 | unchanged |
| Template model + Template/USER administration | unchanged |
| Root routing / landing / no-access | unchanged |
| Navigation projection (`NavigationProjectionService`) | unchanged |
| Route registration seam (`DestinationRoutes`, `DestinationRouteRegistrations`) | unchanged |
| Shared shell presentation (`_Layout`, `_Identity`, `_PrimaryNavigation`, `dmo-tokens.css`, `dmo-shell.css`) | unchanged |
| A1 frozen contract document | unchanged |
| Honest build availability (`ModuleRegistrations.cs` = `[]`) | unchanged |

The three modified README files are documentation, not protected architectural artifacts; the
ledger (row 8.1/8.2/8.3) explicitly folds them into P2-T01 as a documentation-only slice.

## 7. Changed Files

### 7.1 New — `src/DMO.Web/Frontend/Shared/Contracts/`
`CommonState.cs`, `CommonStateTraits.cs`, `CommonStateRegionPresentation.cs`, `StatusTone.cs`,
`RecordStatusPresentation.cs`, `AvailabilityState.cs`, `AvailabilityTraits.cs`,
`AvailabilityPresentation.cs`, `AvailabilityVersionPresentation.cs`, `SharedActionPresentation.cs`.

### 7.2 New — `src/DMO.Web/Pages/Shared/Components/`
`_CommonStateRegion.cshtml`, `_RecordStatus.cshtml`, `_AvailabilityState.cshtml`.

### 7.3 New — other
`src/DMO.Web/wwwroot/css/dmo-components.css`;
`tests/DMO.UnitTests/Frontend/Shared/{CommonStateVocabularyTests,RecordStatusPresentationTests,AvailabilityPresentationTests,SharedActionPresentationTests}.cs`;
`tests/DMO.IntegrationTests/Frontend/Shared/{SharedComponentRenderer,SharedStatesRenderingTests}.cs`;
`dev/responses/P2_T01_IMPLEMENTATION_RESPONSE.md`.

### 7.4 Modified — documentation only
`src/DMO.Application/README.md`, `src/DMO.Infrastructure/README.md`, `src/DMO.Web/README.md`;
`plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` (P2-T01 status closure + Appendix A ledger rows).

## 8. Explicit Non-Work

Confirmed **not started**: P2-T02, P2-T03, P2-T04, P2-T05, P2-T06, P2-T07, P2-T08, P2-T09,
P2-T10.

Specifically not begun: `DenseDataTable`/`AuditTrail`; `ToolPicker`/`ToolSummaryRow`/
`MeasurementRows`/`DecisionBar`/`ProductionContextStrip`; Tool/JobOn domain core; Controlo
Create; Controlo Approve; Boquilhas; PDF/documents; secondary navigation; final integration.

Additionally not performed: no domain status catalogue, lifecycle rule, approval meaning or
authorization; no storage/filesystem lookup or document generation; no module-specific
terminology or formula; no `CurrentBuildAvailable` change; no route registration; no migration;
no shell rewrite; no technical rename of the `historia` identity (HISTÓRICO = local module
history remains distinct from HISTÓRICO GLOBAL).

## 9. Next Boundary

The three reconciliation §7.1 capabilities this workstream owns (common states, `RecordStatus`,
`AvailabilityState`) are now implemented. Reconciliation §7.1 remains PARTIAL only for the
components owned by P2-T02/P2-T03.

The next eligible workstreams per master plan §13 are **P2-T02** (`DenseDataTable` + `AuditTrail`)
and **P2-T03** (`ToolPicker` + rows + `DecisionBar`), each requiring its own authored contract and
Architect `PLAN ACCEPT` before implementation.

**P2-T02 onward was not started.** This task stops at the P2-T01 boundary.
