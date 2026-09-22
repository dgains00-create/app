# P2-T03 — IMPLEMENTATION RESPONSE

**Workstream:** P2-T03 — `ToolPicker` + `ToolSummaryRow` + `MeasurementRows` + `DecisionBar` (A5/A6).
**Status:** P2-T03 IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW.
**Implementation commit:** `8a84c35f0db45ebba50170f112433d8107be4fdf` (`DMO-MODULAR/main`).
**Implemented against:** accepted contract `71a12476b3298eacdda2918966199c306cc0ae29`
(Architect `PLAN ACCEPT` review `c8af762fbc75764c81ccb5ad09b24ae364a56516` in `dmo-work`).

---

## 1. Implementation summary

The four shared presentation primitives were implemented exactly against the accepted contract and
strictly within A-owned paths: `ToolPicker`, `ToolSummaryRow`, `MeasurementRows`, `DecisionBar`.

Delivered:

1. **30 new shared contract types** under `src/DMO.Web/Frontend/Shared/Contracts/` — carriers, event
   vocabularies and the three deterministic presentation-only interaction models.
2. **4 new shared Razor partials** under `src/DMO.Web/Pages/Shared/Components/` — the four rendered
   surfaces, each delegating every non-`ready` state to the accepted P2-T01 common-state partial.
3. **An additive `dmo-components.css` block** — published tokens only, no token declaration, no hex
   literal and no `@media`/`@container`/`@supports`, so the accepted P2-T02 static scans stay green.
4. **2 new generic static assets** — `dmo-tool-picker.js` and `dmo-measurement-rows.js`, thin DOM
   adapters over the normative C# models.
5. **An additive `_SharedComponentAssets.cshtml` extension** — the three accepted tags remain
   byte-identical and the two new `defer` tags are appended.
6. **104 new tests** — the accepted 52-criterion / 99-test matrix one-to-one plus 5 additive evidence
   tests for the Architect's O1–O5 observations.

Frozen behaviour implemented (all proven, §5): never auto-select including exactly one candidate;
`Enter` from search never selects; selection only through an explicit candidate control; the create
affordance only when the consumer supplies it visible and enabled and only in `ready`/`empty`;
cancel and `Escape` request cancel only with no discard path anywhere; verbatim opaque origin-token
roundtrip; invoking-control focus restoration through the accepted shared focus helper; a
supplied-facts-only summary row where a missing optional fact is never a zero, a false or any
placeholder; stable frontend row identity with a configurable minimum (including one) and the
supplied visible removal-disabled reason; documented focus targets after add and after remove;
supplied action order with a supplied group classification and the frozen pending
duplicate-invocation block; and no encoded transition semantics of any kind.

No domain, backend, persistence, authorization, route, availability, Auth or Supabase behaviour was
added, changed or proposed.

## 2. Exact file list

Implementation commit `8a84c35f0db45ebba50170f112433d8107be4fdf` — **53 files changed, 8166
insertions(+), 1 deletion(-)** (the single deletion is one replaced comment line inside the
authorized additive asset-helper extension).

### 2.1 New shared contract types — `src/DMO.Web/Frontend/Shared/Contracts/` (30 files)

```text
ToolPickerFactPresentation.cs          ToolPickerCandidatePresentation.cs
ToolPickerEventKind.cs                 ToolPickerEvent.cs
ToolPickerFocusTarget.cs               ToolPickerOutcome.cs
ToolPickerInteraction.cs               ToolPickerPresentation.cs
ToolSummaryFactPresentation.cs         ToolSummaryRowPresentation.cs
ToolSummaryRowEventKind.cs             ToolSummaryRowEvent.cs
MeasurementRowFieldKind.cs             MeasurementRowFieldOptionPresentation.cs
MeasurementRowFieldPresentation.cs     MeasurementRowPresentation.cs
MeasurementRowsPresentation.cs         MeasurementRowsEventKind.cs
MeasurementRowsEvent.cs                MeasurementRowsFocusKind.cs
MeasurementRowsFocusTarget.cs          MeasurementRowsOutcome.cs
MeasurementRowsInteraction.cs          DecisionBarActionGroup.cs
DecisionBarActionPresentation.cs       DecisionBarEventKind.cs
DecisionBarEvent.cs                    DecisionBarOutcome.cs
DecisionBarInteraction.cs              DecisionBarPresentation.cs
```

### 2.2 New shared Razor partials — `src/DMO.Web/Pages/Shared/Components/` (4 new files)

```text
_ToolPicker.cshtml            _ToolSummaryRow.cshtml
_MeasurementRows.cshtml       _DecisionBar.cshtml
```

### 2.3 Modified — additive only (3 files)

```text
src/DMO.Web/Pages/Shared/Components/_SharedComponentAssets.cshtml   (two appended defer tags;
                                                                      the three accepted tags are
                                                                      byte-identical)
src/DMO.Web/wwwroot/css/dmo-components.css                          (appended P2-T03 block)
src/DMO.Web/wwwroot/css/… (no token/shell/user-shell change)
```

### 2.4 New generic static assets — `src/DMO.Web/wwwroot/js/` (2 files)

```text
dmo-tool-picker.js            dmo-measurement-rows.js
```

### 2.5 New tests (15 files)

```text
tests/DMO.UnitTests/Frontend/Shared/
  P2T03TypeScan.cs                       (unit-side reflection scan helper)
  ToolPickerContractTests.cs             (TP1–TP18)
  ToolSummaryRowContractTests.cs         (TR1–TR9 + no extra)
  MeasurementRowsContractTests.cs        (MR1–MR16 + O5 evidence)
  DecisionBarContractTests.cs            (DB1–DB11)
tests/DMO.IntegrationTests/Frontend/Shared/
  P2T03ComponentRenderer.cs              (rendered-test helper)
  P2T03Fixtures.cs                       (P2-T03 presentation fixture)
  P2T03ProductionScan.cs                 (architecture scan helper)
  ToolPickerRenderingTests.cs            (RTP1–RTP9 + O1, O2 evidence)
  ToolSummaryRowRenderingTests.cs        (RTR1–RTR5 + O4 evidence)
  MeasurementRowsRenderingTests.cs       (RMR1–RMR8)
  DecisionBarRenderingTests.cs           (RDB1–RDB6 + O3 evidence)
  P2T03SharedRenderingTests.cs           (RTS1–RTS3)
  P2T03StaticAssetTests.cs               (ST1–ST9)
  P2T03RegressionTests.cs                (RG1–RG5)
```

No file outside the accepted contract's Appendix B ownership list was created or modified.

## 3. Test counts (executed evidence)

Commands run in `D:\DMO-MODULAR` on the implementation commit:

```text
dotnet build DMO.slnx -c Debug   →  Build succeeded, 0 Error(s), 1 Warning(s)
DMO.UnitTests                    →  468 passed / 0 failed / 0 skipped   (total 468)
DMO.IntegrationTests             →  201 passed / 71 skipped / 0 failed  (total 272)
```

Baseline before P2-T03 (recorded in the authoring task and re-verified at the start of this task):

```text
DMO.UnitTests                    →  413 passed / 0 failed / 0 skipped
DMO.IntegrationTests             →  152 passed / 71 skipped / 0 failed
```

Delta: **+55 unit** (54 matrix rows + 1 O-evidence) and **+49 integration** (45 matrix rows +
4 O-evidence) = **+104 P2-T03 test methods**, all passing.

### 3.1 The single build warning is pre-existing and in a protected accepted test

```text
tests/DMO.IntegrationTests/Frontend/Shared/P2T02RegressionTests.cs(109,9):
warning xUnit2029: Do not use Assert.Empty to check if a value does not exist in a collection.
```

That file is an accepted P2-T02 artifact and is **not modified** by P2-T03 (contract AC-44). It
surfaces only when the integration test project is recompiled from scratch (`-t:Rebuild`); the
incremental baseline build showed zero warnings because that project was not recompiled. Repairing
it would mean editing an accepted test assertion, which P2-T03 is explicitly forbidden to do. Every
P2-T03 file compiles with **zero warnings** (verified with `dotnet build DMO.slnx -t:Rebuild`).

### 3.2 Skipped tests

`71` environment-gated skips remain: they are the pre-existing PostgreSQL/Supabase-live integration
cases that require a disposable database. **No P2-T03 test is skipped** — all 104 P2-T03 test
methods execute and pass. Unexpected skipped tests: **NONE**.

## 4. Acceptance matrix completion

The accepted matrix is complete, one-to-one, and verified mechanically from the committed test
source (method-name id audit):

| Group | Contract ids | Implemented | Missing | Duplicates |
|---|---|---|---|---|
| `TP` ToolPicker unit | TP1–TP18 | 18 | — | — |
| `RTP` ToolPicker rendered | RTP1–RTP9 | 9 | — | — |
| `TR` summary row unit | TR1–TR9 | 9 | — | — |
| `RTR` summary row rendered | RTR1–RTR5 | 5 | — | — |
| `MR` rows unit | MR1–MR16 | 16 | — | — |
| `RMR` rows rendered | RMR1–RMR8 | 8 | — | — |
| `DB` action region unit | DB1–DB11 | 11 | — | — |
| `RDB` action region rendered | RDB1–RDB6 | 6 | — | — |
| `RTS` shared rendered | RTS1–RTS3 | 3 | — | — |
| `ST` static / architecture | ST1–ST9 | 9 | — | — |
| `RG` regression | RG1–RG5 | 5 | — | — |
| **Total** | **99** | **99** | **0** | **0** |

All 52 acceptance criteria (AC-1 … AC-52) are therefore covered by their contracted proofs, and no
test in the matrix is missing, renamed, merged or reduced. Five **additional** evidence tests were
added for the Architect's O1–O5 observations (contract §8 preamble: the catalogue is "the minimum
for P2-T03 acceptance"), giving 104 P2-T03 test methods in total.

## 5. Fixed desktop evidence (1366 × 768)

Design authority: the binding **DMO FIXED DESKTOP LAYOUT POLICY** and A1 freeze §1A, pinned by
contract §6.

| Requirement | Evidence |
|---|---|
| canonical viewport 1366 × 768 bound, compact density | contract §6.1; CSS uses compact single-rhythm rows, control height token and hairline separation only |
| no breakpoint-driven reflow | `ST1`, `ST7`, `RTS3`: the P2-T03 CSS block contains no `@media`, `@container` or `@supports`; both new assets contain no `matchMedia`, no `ResizeObserver` and no resize listener |
| no card conversion | the components render list entries, one compact row per record and one fixed horizontal action region; no card container in the P2-T03 CSS block |
| no required-control hiding / no action relocation / no overflow-menu substitution | `ST8` asserts the required control hooks exist in the partials with no relocation branch; the CSS keeps the trailing select/remove/action positions fixed and never stacks the action region; `RTS2` asserts every required control is present per state; `RDB1`/`RDB5` assert the rendered control order equals the supplied order in one region |
| required controls present per state | the presence matrices of contract §3.1.9/§3.3.6 are model facts proven by `TP14` (picker) and by `RMR1`/`RMR3`/`RMR5` (rows), and rendered by `RTS2` |
| smaller windows scroll instead of reflowing | `RMR8` and `RTS3`: the candidate list and the row grid render inside component-owned keyboard-reachable labelled overflow containers (`tabindex="0"`, `role="region"`, visible focus style) with `overflow-x: auto`; `RFR`-style page scrolling remains available (no fixed clipping height, no scroll lock) |
| no focus trap | the six-check `Escape`/`Enter` handlers are the only key handlers; the picker asset references no Tab key at all (`ST2`), and the rows asset installs no key handler (`ST2`, `RMR6`) |
| mobile/tablet variants out of scope | none produced; no responsive proof requested (contract §6.2, §8.4) |

No mobile, tablet, breakpoint, container-query or width-listener rule exists anywhere in the new
CSS or the new assets.

## 6. Regression evidence

| Row | Evidence | Result |
|---|---|---|
| `RG1` | `ModuleRegistrations.CurrentBuildAvailable` is `[]` | PASS |
| `RG2` | `EmptyDestinationRouteRegistry` is still the production registration; `DestinationRouteRegistrations` declares no member; no `@page`/`MapGet`/`MapPost` in any new partial | PASS |
| `RG3` | the accepted P2-T02 pins still validate unchanged — the pinned P2-T01 artifact SHA-256 hashes and the frozen accepted test-method names are read from `P2T02RegressionTests` by reflection and re-verified | PASS |
| `RG4` | the accepted P2-T02 partials still carry their hooks (`data-dmo-dense-table`, `data-dmo-audit`), the accepted P2-T01 partials still carry theirs, every accepted CSS selector is still present, and the accepted `dmo-focus.js`/`dmo-dense-table.js` are unchanged | PASS |
| `RG5` | `SharedShellTests.SharedFrontendSource_DoesNotImportFeatureServices` still exists unchanged, and no P2-T03 contract source declares a feature namespace | PASS |

Additional whole-suite regression evidence: the pre-existing 413 unit and 152 integration tests all
still pass, unchanged. No existing test method, assertion, helper, pinned hash or fixture was
modified, weakened or deleted.

### 6.1 P2-T02 inherited constraints (the Architect's independently verified constraints)

| # | Constraint | How P2-T03 preserved it |
|---|---|---|
| 1 | CSS appended after the P2-T02 marker is scanned to EOF, so it must remain compatible with the accepted `S2`/`S3` scans | the P2-T03 block contains no `@media`, `@container`, `@supports`, no `--dmo-*:` declaration line, no hex literal, and references only tokens already published in `dmo-tokens.css` (`ST1` asserts the same for the P2-T03 block). The accepted `S2`/`S3` tests pass unchanged. |
| 2 | the accepted shared assets are scanned for the bare token `tool` | picker and rows logic lives **only** in the two new files; `dmo-focus.js` and `dmo-dense-table.js` are byte-identical, so the accepted `S4` scan passes unchanged. The two new assets are scanned by the new `ST2` with a vocabulary list that deliberately excludes the bare token `tool` (the frozen component identity contains it) and forbids `tool_id`/`toolId`/`ToolId` instead. |
| 3 | `_SharedComponentAssets.cshtml` edits must remain additive | two `defer` tags appended after the three accepted tags; those three lines are byte-identical, so the accepted `S5` assertion (each existing tag exactly once, no component markup) passes unchanged. Verified by `ST9`. |
| 4 | no P2-T02 test may be weakened to make P2-T03 easier | no `tests/**` file that predates P2-T03 was touched (53-file diff); `RG3`/`RG4` re-verify the accepted pins, and the single pre-existing build warning was **left in place** rather than repaired because repairing it would edit an accepted test assertion. |

## 7. Architect decisions Q1–Q6 — implementation compliance

| Decision | Implemented as | Evidence |
|---|---|---|
| **Q1 — picker-owned candidate list; `ToolPicker` is not forced onto `DenseDataTable`** | the picker renders its own labelled candidate list with one component-owned explicit select control per candidate; `DenseDataTable`/`DenseTableInteraction` are neither referenced nor duplicated; no body/label activation selects and no `Enter`-opens path exists | `RTP1`, `RTP2`, `RTP9`, `TP4`, `TP6`, `ST3`; `DenseDataTable` untouched and re-verified by `RG4` |
| **Q2 — `create requested` carries the origin token and the action key only** | `ToolPickerEvent.CreateRequested(originToken, actionKey)`; no prefill carrier exists in any type or in the adapter event detail | `TP10`, `TP16`, `ST3` |
| **Q3 — create affordance only in `ready`/`empty`** | `ShowsCreate` is exactly "supplied ∧ visible ∧ enabled ∧ state ∈ {Ready, Empty}"; `lookup-failed`, `loading`, `unavailable`, `permission-denied`, `stale`, `conflict`, `saving`, `submitting` never expose it, and a hidden or disabled create action renders nothing at all (no disabled create reason is invented) | `TP11`, `RTP3`, `RTP4`, `RTP7` |
| **Q4 — accepted verification strategy; no browser package** | C# interaction-model unit tests (`TP`, `MR`, `DB`) + rendered markup/hook assertions (`RTP`, `RTR`, `RMR`, `RDB`, `RTS`) + static-asset assertions (`ST`). No browser or JS test package, package reference or project was added; DOM-level verification stays deferred. | the 104 tests are all model/rendered/static; the diff contains no `.csproj`, `Directory.Packages.props` or project change |
| **Q5 — generic controlled field descriptors; no consumer markup injection** | `MeasurementRowFieldPresentation` carries key/label/value/kind/options/editable/disabled reason/validation display; there is no `IHtmlContent`, no template delegate and no markup-injection path; `Numeric` selects only the native input affordance and attaches no numeric rule | `MR12`, `MR13`, `MR14`, `RMR2`, `ST3` |
| **Q6 — one supplied sequence with the group as a classification** | one action sequence renders in exactly the supplied order; `DecisionBarActionPresentation.Group` never reorders, relocates or filters; only the three groups exist and the group is mandatory | `DB1`, `DB2`, `RDB1`, `RDB2` |

## 8. Architect observations O1–O5 — evidence

Each observation concerned a behaviour the accepted contract **already pinned normatively**; no
carrier and no semantics were invented, and no contract change was made.

| # | Implemented behaviour | Source | Test evidence | New carrier invented? |
|---|---|---|---|---|
| **O1** | the supplied expected-type display label and the supplied origin-context facts render with visible labels in their own context region (the accepted `ExpectedTypeLabel`/`OriginContext` carriers, qualified by the generic A-owned label `GenericExpectedTypeQualifier`) | `_ToolPicker.cshtml` (context region, `data-dmo-picker-expected-type`, `data-dmo-picker-origin-context`, `data-dmo-origin-fact`), `ToolPickerPresentation.ExpectedTypeLabel`/`OriginContext` | `O1_SuppliedExpectedTypeAndOriginContextRenderWithVisibleLabels` | **NO** — the carriers are contract §3.1.4; the only addition is one generic A-owned UI qualifier constant |
| **O2** | a supplied result summary renders verbatim inside a polite live region (`role="status"`, `aria-live="polite"`); the component never counts | `_ToolPicker.cshtml` (`data-dmo-picker-summary`), `ToolPickerPresentation.ResultSummary` | `O2_SuppliedResultSummaryRendersVerbatimInsideAPoliteLiveRegion` | **NO** — `ResultSummary` is the contract §3.1.4 carrier |
| **O3** | the optional supplied status/help text renders verbatim inside the labelled action region | `_DecisionBar.cshtml` (`data-dmo-decision-status`), `DecisionBarPresentation.StatusText` | `O3_SuppliedStatusTextRendersVerbatimInTheLabelledRegion` (also asserted in `RDB5`) | **NO** — `StatusText` is the contract §3.4.2 carrier |
| **O4** | supplied action **visibility** is honoured for the summary row: a `Visible = false` supplied action renders no control, no reason and no label; a visible supplied action still renders | `_ToolSummaryRow.cshtml` (`Model.VisibleActions`), `ToolSummaryRowPresentation.VisibleActions` | `O4_SuppliedActionVisibilityIsHonoured_WithNoInventedControl` (also asserted in `RTR2`) | **NO** — `Visible` is inherited from the accepted `SharedActionPresentation` |
| **O5** | the supplied `State` and the supplied `StructuralMutationAllowed` flag stay consistent with the pinned mapping: `saving`, `submitting`, `unavailable` and `permission-denied` block structural mutation even when the supplied flag says otherwise, a `false` flag blocks it in an otherwise editable state, and the supplied structural reason is the one exposed to removal | `MeasurementRowsPresentation.StructuralMutationBlocked`/`AddControlEnabled`/`RemoveControlEnabled`/`RemovalDisabledReason`, `MeasurementRowsInteraction.SetStructuralMutation` | `O5_SuppliedStateAndStructuralFlag_StayConsistentPerThePinnedMapping` | **NO** — both supplied inputs are contract §3.3.2 carriers; the contract's §3.3.6/§4.2.1 mapping is applied as written |

## 9. P2-T01 / P2-T02 protection

Reused unchanged (no duplication, no mutation):

| Accepted primitive | How P2-T03 consumed it |
|---|---|
| `CommonState`, `CommonStateTraits`, `CommonStateRegionPresentation`, `_CommonStateRegion.cshtml` | the sole state vocabulary and the sole rendering of every non-`ready` surface of all four components; the four mandated distinctions stay distinct |
| `RecordStatusPresentation`, `StatusTone`, `_RecordStatus.cshtml` | the sole status rendering (summary row) and the reused supplementary tone vocabulary (validation display) |
| `SharedActionPresentation` | the sole generic action carrier for picker create/cancel/retry/state actions, summary-row actions and every action-region action; `DecisionBarActionPresentation` composes it and adds exactly one dimension (the supplied group) |
| `dmo-focus.js` | the picker's cancel/return focus restoration and the rows' documented fallback; no second focus helper was written |
| the additive-CSS/token pattern | appended P2-T03 block using published tokens only |
| the P2-T02 rendered-test / regression approach | new P2-T03 helper, fixture, renderer and regression classes; the P2-T02 classes are untouched |

Not used and not duplicated: `AvailabilityState`/`AvailabilityPresentation`/`_AvailabilityState`,
`DenseDataTable`/`DenseTableInteraction`, `AuditTrail`, `ProductionContextStrip`.

## 10. Boundary report

```text
CurrentBuildAvailable                     = []
Operational destinations newly exposed    = NONE
Backend changes                           = NONE
Persistence / migrations added            = NONE (0 migrations)
Route / destination registration changes  = NONE
Authorization / access changes            = NONE
Supabase / .env / Auth changes            = NONE
User provisioning                         = NONE
P2-T04 implementation leakage             = NONE
Protected foundation files modified       = NONE
```

- No `Program.cs`, `ModuleRegistrations.cs`, `ModuleCatalog.cs`, `DestinationRoutes.cs`,
  `DestinationRouteRegistrations.cs`, `SharedFrontendExtensions.cs`, `_Layout.cshtml`,
  `_PublicLayout.cshtml`, `dmo-tokens.css`, `dmo-shell.css`, `dmo-user-shell.css` or migration file
  was changed.
- No `.csproj`, `Directory.Packages.props`, package or project was added or changed: the two new
  scripts need no registration (`UseStaticFiles()` already serves `wwwroot`) and the four partials
  are discovered by the existing `AddRazorPages()`.
- The Supabase TEST baseline is untouched. P2-T03 has no Auth, provisioning, persistence or database
  requirement; nothing in the implementation reads, writes or configures Supabase, and no database
  test was run.
- P2-T04 has **not** started. The implemented sources contain no Tool canonical identity, no
  production-occurrence context, no association concept and no route or availability registration
  (`ST6` scans the contract sources, partials, assets and fixture for later-workstream concepts;
  `DB8`/`MR13`/`TP17`/`TR9`/`MR16`/`DB11` assert the domain-neutral member surface).

## 11. Verification method and evidence classes

Following `dmo-beta-master/WORKFLOW.md` "Testing evidence" and `ACCEPTANCE_MATRIX.md` §11:

1. **Committed test source inspected in Git** — 15 new test files in commit
   `8a84c35f0db45ebba50170f112433d8107be4fdf`, with the id-to-method mapping audited mechanically
   (99/99 ids, no duplicates, no missing, no extra).
2. **Developer-reported local execution** — the counts in §3, produced by `dotnet build`,
   `dotnet test tests/DMO.UnitTests` and `dotnet test tests/DMO.IntegrationTests` on that commit.
3. **Independently observable CI evidence** — **none available**: this repository has no `.github/`
   workflow. Local execution is developer evidence, not independent verification, which is why this
   response does not claim acceptance.

## 12. Next gate

```text
P2-T03 STATUS = IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW
```

Implementation commit `8a84c35f0db45ebba50170f112433d8107be4fdf` is pushed to `DMO-MODULAR/main`.
Formal closure requires independent verification and an Architect implementation review against the
accepted contract per `dmo-beta-master/WORKFLOW.md` step 12.

Until that review:

- P2-T03 is **not** self-accepted and is **not** marked CLOSED;
- **P2-T04 is not authorized and has not started** (authority blocker B1 remains open);
- no P2-T03 artifact may be reinterpreted as authorization to expose a destination, register a
  Module, add a route or extend availability.
