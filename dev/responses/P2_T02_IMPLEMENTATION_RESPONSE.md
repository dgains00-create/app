# P2-T02 — `DenseDataTable` + `AuditTrail` — IMPLEMENTATION RESPONSE

**Workstream:** P2-T02 — `DenseDataTable` + `AuditTrail` (Workstream A, sub-step A4)
**Repository:** `diogo-o/DMO-MODULAR`, branch `main`
**Status:** **IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW**
**Not closed, not accepted, not self-reviewed.** P2-T03 is **not authorized** and was not started.

---

## 1. Baseline

| Item | Value |
|---|---|
| Starting `DMO-MODULAR/main` (local, before work) | `0b47690936599b6a71342b68b1cf36cfe4b64264` |
| Starting `DMO-MODULAR/origin/main` (implementation base) | `e79186a81d5cd934fe32a100bc8dd9dd08bf509a` |
| Accepted contract SHA | `e79186a81d5cd934fe32a100bc8dd9dd08bf509a` — `plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md` |
| Architect PLAN ACCEPT review SHA (`dmo-work`) | `f1ddb968e026dc6cf2569d8de64400d8c3044514` — `dev/reviews/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_PLAN_REREVIEW.md` ("Verdict: PLAN ACCEPT") |
| Implementation commit SHA | `04601f9a827797481acd8ce4f76a6d2b1eb21af6` (`P2-T02: implement DenseDataTable + AuditTrail (A4)`) |
| Working tree before work | CLEAN, local `main` fast-forwarded to `origin/main` (`e79186a`) with no local commits discarded |

Pre-flight verification performed before any code change:

1. `DMO-MODULAR` fetched; remote `main` recorded as `e79186a`.
2. Accepted contract SHA `e79186a` verified reachable from remote `main` (it **is** remote `main`).
3. Architect `PLAN ACCEPT` SHA `f1ddb968` verified reachable in `dmo-work` (`P2-T02: accept corrected
   contract plan`).
4. Working tree verified clean.
5. Accepted contract read completely (983 lines).
6. Architect PLAN ACCEPT re-review read completely; §4 disposals confirmed (Q1 accepted, Q2
   ABSENT, Q3 accepted).
7. P2-T02 handoff (`plans/beta-workstreams/P2-T02-DENSE-TABLE-AUDIT-TRAIL.md`) and master-plan
   §7/§11/§12 read.
8. Existing P2-T01 shared primitives inspected before implementation (all ten contract types and
   all three partials) so they are reused exactly as accepted.

## 2. Scope Implemented

Exactly two shared presentation primitives, and nothing else:

- **`DenseDataTable`** — supplied ordered columns/rows, opaque row identity, supplied cell text,
  optional supplied row `RecordStatus`, row-scoped supplied actions, controlled selection/open
  arbitration, controlled consumer-owned filter/paging/sort **presentation**, accepted P2-T01
  state surfaces, fixed desktop composition.
- **`AuditTrail`** — read-only supplied historical/audit event rendering in the supplied sequence,
  supplied attribution facts, optional detail/before/after, one optional generic detail action,
  accepted P2-T01 state surfaces.

No third component, no domain seam, no backend, no route and no P2-T03 concept was added.

## 3. Contract Trace

| Contract item | Implementation | Evidence test(s) |
|---|---|---|
| §5.2 column contract, AC-1 | `DenseTableColumnPresentation`, `DenseTableColumnAlignment`, `DenseTableColumnWidthHint`; header rendering in `_DenseDataTable.cshtml` | `Columns_RenderInSuppliedOrderForEveryPriorityAndWidthHintCombination`, `Columns_EverySuppliedColumnIsPresentRegardlessOfPriority`, `Columns_AccessibleHeadingFallsBackToTheVisibleHeading`, `Rendering_ColumnOrder_IsTheSuppliedOrderForEveryPriorityAndWidthHint` |
| §5.3 row contract, AC-3, AC-12 | `DenseTableRowPresentation`, `DenseTableCellPresentation` | `Rows_WithACellCountDifferentFromTheColumnCountAreRejected`, `RowActions_ReuseTheAcceptedSharedActionCarrier`, `Rendering_DisabledRowAction_AssociatesItsReasonAndNamesTheRowContext` |
| §5.4 row status, AC-13 | status slot + `<partial _RecordStatus>` | `Rendering_RowStatus_ReusesTheAcceptedP2T01StatusPartial` |
| §5.5 controlled filter/paging/sort, AC-2, AC-14, AC-15, AC-16 | `DenseTableFilterPresentation`, `DenseTableFilterOptionPresentation`, `DenseTablePagingPresentation`, `DenseTableSortPresentation`, `DenseTableEvent(Kind)` | `Rows_SuppliedFilterPagingAndSortStateNeverTransformTheRows`, `Rendering_SuppliedFilterPagingAndSortState_NeverChangesTheRenderedRows`, `Rendering_ControlledAffordances_RenderTheirValuesAndConstructNoUrl`, `Rendering_ResultSummary_IsVerbatimInsideAPoliteLiveRegion`, `Rendering_SortAffordance_ExposesOnlyTheSuppliedDirection` |
| §5.6 selection/open, AC-4…AC-7, AC-17 | `DenseTableInteraction`, `DenseTableOutcome`, `wwwroot/js/dmo-dense-table.js` | `Click_SelectsTheRowWithoutEverRequestingOpen`, `Space_SelectsTheRowWithoutEverRequestingOpen`, `DoubleClick_RequestsOpenOnceAndLeavesTheRowSelected`, `ConsecutiveOpenActivations_ForTheSameRow_CollapseIntoOneRequest`, `Enter_And_TheExplicitOpenControl_BehaveIdentically`, `OpenDisabled_NoInputEverRequestsOpen`, `SelectionDisabled_NoInputSelects`, `Selection_IsSingleAndControlled`, `Rendering_SelectedRow_IsProgrammaticAndCarriesAVisibleMarker`, `Rendering_OpenControl_ExistsOnlyWhenTheConsumerEnablesOpen` |
| §5.8 inputs, AC-4 | `DenseTablePresentation` | `SelectedKey_MustReferenceASuppliedRowAndRequireSelection`, `Create_RejectsBlankMandatoryValuesAndAMissingNonReadyMessage`, `Create_RejectsASortThatDoesNotReferenceASuppliedSortableColumn` |
| §6.2 audit carrier, Q2, AC-20, AC-25 | `AuditEntryPresentation` (**no** status member) | `Attribution_IsNeverSynthesized`, `Entry_HasNoEntryLevelRecordStatus`, `Rendering_EntryCarriesNoEntryLevelStatus`, `Rendering_MissingActorOrTimestamp_ProducesNoAttributionText` |
| §6.3 ordering, AC-18 | `AuditTrailPresentation.Entries` rendered in supplied order; `<ul>`/`<li>` | `Entries_PreserveExactlyTheSuppliedSequence`, `AuditTypes_ExposeNoOrderingOrChronologyMember`, `Rendering_Entries_KeepExactlyTheSuppliedOrderInAPlainList` |
| §6.4 display, AC-19, AC-21 | entry meta/detail markup + labels | `Timestamp_DisplayTextIsVerbatimAndTheSemanticValueIsMachineOnly`, `DetailFacts_ArePresentOnlyWhenSupplied`, `Rendering_EntryFacts_RenderWithVisibleLabelsAndHooks` |
| §6.5 read-only, AC-24 | no mutation control anywhere; one optional `DetailAction` | `Entry_IsReadOnlyApartFromTheSuppliedDetailAction`, `Rendering_IsReadOnly_WithNoEditDeleteOrRestoreControl` |
| §7.1/§7.2 state integration, AC-8…AC-11, AC-22, AC-23 | `ShowsStateSurface` + `<partial _CommonStateRegion>` delegation | `Rendering_EmptyAndLookupFailed_AreDistinctAndNeverAliased`, `Rendering_EmptyRetainsHeaderContext_AndFailuresRenderNoDataBody`, `Rendering_UnavailableAndPermissionDenied_AssociateTheSuppliedReason`, `Rendering_LoadingWithRetainedRows_MarksTheRegionBusyAndLabelsEveryRowStale`, `Rendering_StaleRetainsRowsAndShowsTheAcceptedStaleSurface`, `Rendering_UnavailableAndPermissionDenied_RenderNoPartialEntryFacts`, `Rendering_LoadingIsABusyLabelledHistoryRegion_AndStaleRetainsEntries` |
| §8.1/§8.2 accessibility, AC-17, AC-30 | semantic `<table>`/`<caption>`/`<th scope>`, `aria-selected` + visible marker, associated disabled reasons, labelled focusable scroll container, no `role="grid"`, no `tabindex` on audit entries | `Rendering_ScrollContainer_IsFocusableLabelledAndTrapsNoFocus`, `Rendering_StructureIsAFixedSemanticTableWithNoCardConversion`, `Rendering_EntryList_IsACompactFixedDesktopListWithNoCards` |
| §9 styling, AC-29, AC-30 | additive `dmo-table*`/`dmo-audit*` blocks in `dmo-components.css` | `S1`, `S2`, `S3`, `G4` |
| §10.4 static assets, AC-15, AC-26 | `dmo-focus.js`, `dmo-dense-table.js`, `_SharedComponentAssets.cshtml` | `S4`, `S5` |
| Q3 plain encoded cell text | `DenseTableCellPresentation` (text only) | `Rendering_CellText_IsEncodedAsVisiblePlainTextAndNeverAsMarkup` |
| §13.1/§13.3 protected boundaries, AC-27, AC-28, AC-31 | no protected file touched | `G1`, `G2`, `G3`, `G4` |

## 4. `DenseDataTable` Implementation

New presentation contracts (`src/DMO.Web/Frontend/Shared/Contracts/`, namespace
`DMO.Web.Frontend.Shared.Contracts`, sealed records with private constructor + static `Create`):

- `DenseTableColumnAlignment.cs`, `DenseTableColumnWidthHint.cs`, `DenseTableSortDirection.cs`
- `DenseTableColumnPresentation.cs` — opaque `Key`, mandatory visible `Heading`, optional
  `AccessibleHeading`, supplied `Alignment`, `Priority` (presentation metadata only),
  `Sortable`, advisory `WidthHint`
- `DenseTableCellPresentation.cs` — mandatory non-blank plain `Text`, no markup carrier
- `DenseTableRowPresentation.cs` — opaque `Key`, mandatory `AccessibleContext`, cells, optional
  `Status`, optional row-scoped `Actions` (accepted P2-T01 carrier)
- `DenseTableFilterOptionPresentation.cs`, `DenseTableFilterPresentation.cs`,
  `DenseTablePagingPresentation.cs`, `DenseTableSortPresentation.cs`
- `DenseTablePresentation.cs` — the single consumer input; fails closed on caption/blank values,
  cell-count mismatch, phantom `SelectedKey`, missing non-ready `Message` and a sort that does not
  reference a supplied sortable column; exposes derived presentation facts only
- `DenseTableEventKind.cs`, `DenseTableEvent.cs` — frozen generic event vocabulary carrying only
  opaque keys/carriers
- `DenseTableOutcome.cs`, `DenseTableInteraction.cs` — the deterministic, presentation-only
  selection/open arbitration (normative statement of §5.6)

New partial: `src/DMO.Web/Pages/Shared/Components/_DenseDataTable.cshtml`.

Behaviour actually implemented:

- columns render in exactly the supplied order at every combination of `Priority`/`WidthHint`; no
  column is hidden, reordered or converted, and `Priority` is emitted only as
  `data-dmo-column-priority`;
- rows render in exactly the supplied order; supplied filter/paging/sort state is rendered and
  **never** applied (no filter operator, no sort algorithm, no page arithmetic, no counting);
- one component-owned trailing control cell, always the last rendered column, containing the
  explicit open control (iff `OpenEnabled`) and the row-scoped supplied actions;
- optional row status rendered through the accepted P2-T01 `_RecordStatus` partial in a
  component-owned status slot inside the row's leading cell — in flow, never overlaid on cell
  content, never hiding or truncating supplied cell text, and never adding a column;
- one selected row: `aria-selected="true"` plus a visible marker glyph and a CSS treatment;
- disabled row actions render `disabled` + `aria-disabled="true"` + `aria-describedby` →
  rendered reason `id`, with the row's supplied `AccessibleContext` in the accessible name
  (`"{label} — {context}"`);
- every non-`ready` state delegates to the accepted P2-T01 `_CommonStateRegion` partial; **no**
  table-specific state vocabulary or message set exists;
- `empty` retains the caption + header row and shows the P2-T01 `empty` surface;
  `lookup-failed` / `unavailable` / `permission-denied` / `conflict` render **no** table and no
  data body;
- `loading` with retained rows marks the region `aria-busy="true"` and labels **every** retained
  row with visible "Desatualizada" text plus a stale hook; `loading` without retained rows renders
  the P2-T01 `loading` surface;
- `stale` retains the rows and renders the P2-T01 `stale` surface with the supplied refresh action;
- `saving`/`submitting` render the accepted P2-T01 surface only (see §14, note 5);
- no `href`, `action`, `method`, `<form>`, URL, route or navigation target is produced.

## 5. `AuditTrail` Implementation

New presentation contracts:

- `AuditEntryPresentation.cs` — opaque `Key`, mandatory `ActionText`, optional `Actor`,
  optional `ActorUnavailableText`, optional `TimestampText` (verbatim display) and
  `TimestampValue` (machine value only), optional `Detail`/`BeforeDetail`/`AfterDetail`, optional
  single `DetailAction`. **No `Status` and no renamed equivalent field.**
- `AuditTrailPresentation.cs` — state, mandatory `RegionLabel`, supplied `Entries`, mandatory
  non-ready `Message`, optional `Reason`, optional region `Actions`, optional supplied
  `ContextText`.

New partial: `src/DMO.Web/Pages/Shared/Components/_AuditTrail.cshtml`.

Behaviour actually implemented:

- entries render in **exactly** the supplied sequence in a plain `<ul>`/`<li>` list (never `<ol>`,
  never `role="timeline"`); no sorting, no reordering, no chronology inference, no
  newest-first/oldest-first label and no ordering member exists;
- per entry: the supplied action text is prominent; supplied actor (or the supplied explicit
  unavailable text, only when the actor is absent) and supplied timestamp display text render as
  labelled meta facts; `Detail`/`Antes`/`Depois` render only when supplied, under generic A-owned
  labels;
- `TimestampText` is always the visible value; `TimestampValue` appears only as
  `<time datetime="…">`; when no display text is supplied, **no** timestamp is rendered at all;
- the entry's only interactive element is the optional supplied generic `DetailAction` (native
  `<button>`, accepted disabled-reason association, `data-dmo-focus-return` hook for the shared
  focus helper); no edit/delete/restore/correct/undo control exists in any state;
- `unavailable` / `permission-denied` render only the accepted P2-T01 surface with the supplied
  reason — supplied entries are **not** rendered, so no partial protected fact leaks;
- `loading` is a busy labelled history region; `empty` is explicit no-history and never a failure;
  `lookup-failed` is assertive with the supplied retry; `stale` retains the entries with the
  P2-T01 warning; `conflict`/`saving`/`submitting` render the accepted P2-T01 surface only.

## 6. Q1 / Q2 / Q3 Compliance

- **Q1 — accepted default respected.** The frozen interaction rules are implemented as the
  deterministic, presentation-only C# `DenseTableInteraction` model and are unit-tested
  (U4–U8). `wwwroot/js/dmo-dense-table.js` is a thin DOM adapter over the same rules with stable
  `data-dmo-*` hooks. Rendered markup/hook assertions (R4, R5, R6, R13) and static-asset content
  assertions (S4) are the P2-T02 evidence. **No browser test package, framework or new test
  project was introduced**; DOM/browser-level verification remains deferred as accepted.
- **Q2 — AuditTrail entry status ABSENT.** `AuditEntryPresentation` carries no `Status` member,
  no `RecordStatusPresentation`/`StatusTone` member and no renamed equivalent. `AuditTrail`
  renders no entry-level status. Asserted structurally (`Entry_HasNoEntryLevelRecordStatus`) and
  in rendered markup (`Rendering_EntryCarriesNoEntryLevelStatus`: no `dmo-status`, no
  `data-dmo-status`, no `role="status"` in the audit surface).
- **Q3 — plain Razor-encoded text respected.** `DenseTableCellPresentation` carries plain text
  only; no `IHtmlContent`, no markup slot, no encoding relaxation. The raw-markup test proves the
  supplied text is emitted encoded (`&lt;b&gt;x&lt;/b&gt; &amp; &quot;y&quot;`) and never as
  markup.

## 7. Fixed Desktop Compliance

- Canonical surface **1366 × 768**: both components use a compact single-rhythm density
  (`--dmo-space-*`, `--dmo-control-height`), a single hairline separation (`--dmo-line`) and no
  decorative inflation, so the real 768 px vertical constraint is respected.
- **No** `@media`, `@container` or `@supports` rule exists in the new CSS block (asserted by S2);
  the protected pre-existing shell `@media (max-width: 62rem)` rule is untouched.
- No table-to-card conversion, no required-column hiding, no column reordering and no action
  relocation at any width; `Priority` and `WidthHint` cannot hide or reorder anything (asserted at
  model and rendered level, across every combination).
- An over-wide table uses the local horizontal scroll container `.dmo-table__scroll` with
  `overflow-x: auto`, `tabindex="0"`, a supplied accessible name and a visible
  `:focus-visible` style; no key handler swallows `Tab`, so no focus trap exists.
- Cell content wraps instead of being truncated: no `text-overflow: ellipsis`, no clamping and no
  `visibility:hidden` on data.
- `AuditTrail` keeps the same fixed composition: compact entries, hairline separation, no cards,
  no stacked variant, no oversized headers.
- Mobile/tablet/touch variants are absent by design (narrowing authorized by the binding policy).

## 8. Protected P2-T01

**Preserved — confirmed by construction and by test.**

- The 10 accepted P2-T01 contract files and the 3 accepted P2-T01 partials are **byte-identical**:
  `P2T02RegressionTests.G4_ProtectedP2T01Artifacts_AreUnchanged` pins their normalized SHA-256 and
  passes (`src/` shows them as unmodified in `git status`).
- Every pre-existing P2-T01 CSS selector remains present (S1, G4); the stylesheet change is
  **pure addition** (359 insertions, 0 deletions).
- No accepted P2-T01 test method was modified, renamed or deleted (G3 pins the P2-T01 and
  shared-shell test method names; the full P2-T01 suites still pass).
- P2-T01 primitives are consumed, never redesigned: `CommonState`, `CommonStateTraits`,
  `CommonStateRegionPresentation`, `RecordStatusPresentation`, `StatusTone`,
  `SharedActionPresentation`, `_CommonStateRegion.cshtml`, `_RecordStatus.cshtml` and the
  accepted token/CSS conventions.
- Delegation is by the real partial (`<partial name="~/Pages/Shared/Components/_CommonStateRegion.cshtml">`),
  so no P2-T02 file re-implements a state surface. The P2-T02 partials reference the P2-T01
  partials by explicit app-root path because the Razor partial tag helper's default search
  locations do not include `Pages/Shared/Components`; no view-location or
  `SharedFrontendExtensions` change was made to achieve this.
- The accepted P2-T01 test helper `SharedComponentRenderer` was **not modified**; P2-T02 adds its
  own additive `P2T02ComponentRenderer` helper (explicitly permitted by contract §10).

## 9. P2-T03 Boundary

Untouched. No `ToolPicker`, `ToolSummaryRow`, `MeasurementRows`, `DecisionBar` or
`ProductionContextStrip` concept, carrier, behaviour, slot or partial exists anywhere in the
change; no picker candidate model, no search/create/association state, no minimum-row rule, no
action-group model, no pending-action deduplication and no production-context fact was
introduced. The only seams provided are the generic ones P2-T02 itself needs: the opaque
row/column/action key model, the accepted `SharedActionPresentation` carrier and the row-scoped
`DenseTableInteraction` arbitration. Asserted by `G4_ProtectedP2T01SelectorsRemainAndP2T02PartialsConstructNoUrl`.

## 10. Tests

All commands run in this working copy after `dotnet build DMO.slnx -c Debug`.

| Command | Result |
|---|---|
| `dotnet build DMO.slnx -c Debug` | **Build succeeded — 0 warnings, 0 errors** |
| `dotnet test tests/DMO.UnitTests` (full) | **410 passed / 0 failed / 0 skipped** (pre-P2-T02 baseline 374; +36 new) |
| `dotnet test tests/DMO.IntegrationTests` (full) | **152 passed / 0 failed / 71 skipped** (pre-P2-T02 baseline 115 passed / 71 skipped; +37 new) |
| Targeted P2-T02 (unit, filter `DenseDataTable|DenseTable|AuditTrail` in `Frontend.Shared`) | **36 passed / 0 failed / 0 skipped** |
| Targeted P2-T02 (integration: `DenseDataTableRenderingTests`) | **18 passed / 0 failed / 0 skipped** |
| Targeted P2-T02 (integration: `AuditTrailRenderingTests`) | **9 passed / 0 failed / 0 skipped** |
| Targeted P2-T02 (integration: `SharedComponentAssetTests`) | **5 passed / 0 failed / 0 skipped** |
| Targeted P2-T02 (integration: `P2T02RegressionTests`) | **5 passed / 0 failed / 0 skipped** |
| Targeted P2-T02 total | **73 passed / 0 failed / 0 skipped** |

New test files (all additive; no existing test file was edited):

- `tests/DMO.UnitTests/Frontend/Shared/DenseTableInteractionTests.cs` (U4–U8, 10 tests)
- `tests/DMO.UnitTests/Frontend/Shared/DenseDataTableContractTests.cs` (U1–U3, U9–U13, 14 tests)
- `tests/DMO.UnitTests/Frontend/Shared/AuditTrailContractTests.cs` (U14–U20, 12 tests)
- `tests/DMO.IntegrationTests/Frontend/Shared/P2T02ComponentRenderer.cs` (renderer helper)
- `tests/DMO.IntegrationTests/Frontend/Shared/DenseDataTableRenderingTests.cs` (R1–R9, R13, 18 tests)
- `tests/DMO.IntegrationTests/Frontend/Shared/AuditTrailRenderingTests.cs` (R1, R10–R12, 9 tests)
- `tests/DMO.IntegrationTests/Frontend/Shared/SharedComponentAssetTests.cs` (S1–S5, 5 tests)
- `tests/DMO.IntegrationTests/Frontend/Shared/P2T02RegressionTests.cs` (G1–G4, 5 tests)

Contract-row coverage is complete: **U1–U20**, **R1–R14**, **S1–S5**, **G1–G4**. R14 (the P2-T01
rendered suites and `Rendering_LiveShellAndNavigationBehaviourIsUnchanged`) is satisfied by the
unmodified pre-existing suites, which pass in the full run and are additionally pinned by G3.

**No existing test was weakened, deleted or rewritten, and no live Supabase or database
configuration was enabled to influence the result.**

## 11. Skipped Tests

**71 skipped integration tests — all pre-existing, all environment-gated, none P2-T02.**

They are the same 71 `[SkippableFact]` tests that were skipped before P2-T02 (baseline recorded
in the P2-T01 ledger entry) and they skip because no live test database / live Supabase
credentials are configured. Groups:

| Group | Gate |
|---|---|
| `DatabaseConnectivityTests`, `PersistenceTestDatabase` | live test database connection string |
| `Migration001AccountAndTemplateFoundationTests`, `Migration002TemplateModuleCompositionTests` | live test database |
| `ModuleAccessResolverIntegrationTests`, `PersistenceAccountLookupIntegrationTests`, `TemplateAdministrationPersistenceIntegrationTests` | live test database |
| `AdminBootstrapIntegrationTests`, `AdminSingletonInvariantTests` | live test database |
| `LiveDevTestSupabaseAdminAuthTests`, `LiveDevTestSupabaseUserAuthTests` | live Supabase project + credentials |

Zero P2-T02 tests were skipped: all 73 new tests executed.

## 12. Changed Files

**Modified (1 file — pure addition, 359 insertions / 0 deletions):**

| Path | Purpose |
|---|---|
| `src/DMO.Web/wwwroot/css/dmo-components.css` | append additive, token-only, breakpoint-free `dmo-table*`/`dmo-audit*` blocks (contract §9) |

**New presentation contracts (17 files, `src/DMO.Web/Frontend/Shared/Contracts/`):**
`DenseTableColumnAlignment.cs`, `DenseTableColumnWidthHint.cs`, `DenseTableSortDirection.cs`,
`DenseTableColumnPresentation.cs`, `DenseTableCellPresentation.cs`, `DenseTableRowPresentation.cs`,
`DenseTableFilterOptionPresentation.cs`, `DenseTableFilterPresentation.cs`,
`DenseTablePagingPresentation.cs`, `DenseTableSortPresentation.cs`, `DenseTablePresentation.cs`,
`DenseTableEventKind.cs`, `DenseTableEvent.cs`, `DenseTableOutcome.cs`, `DenseTableInteraction.cs`,
`AuditEntryPresentation.cs`, `AuditTrailPresentation.cs`.

**New shared partials (3 files, `src/DMO.Web/Pages/Shared/Components/`):**
`_DenseDataTable.cshtml`, `_AuditTrail.cshtml`, `_SharedComponentAssets.cshtml`.

**New generic static assets (2 files, `src/DMO.Web/wwwroot/js/`):**
`dmo-focus.js`, `dmo-dense-table.js`.

**New tests (8 files):** as listed in §10.

**Planning / governance (2 files, status bookkeeping only):**
`plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` (P2-T02 status line),
`plans/beta-workstreams/P2-T02-DENSE-TABLE-AUDIT-TRAIL.md` (§13 status line).

**This response:** `dev/responses/P2_T02_IMPLEMENTATION_RESPONSE.md`.

Every changed implementation path maps directly to accepted P2-T02 scope (§14 of the contract).

## 13. Explicit Non-Work

- **Backend: none.** No migration, no repository, no database entity, no API endpoint, no backend
  pagination/filtering/sorting, no query service, no `DbContext` usage.
  `src/DMO.Application`, `src/DMO.Infrastructure` and `src/DMO.Domain` are untouched
  (`git status` is empty for all three).
- **Routes / availability: none.** No route was added or changed; `DestinationRouteRegistrations`
  still carries zero registrations; `EmptyDestinationRouteRegistry` is still the resolved
  authority; `ModuleRegistrations.CurrentBuildAvailable` is still `[]`; no availability or module
  registration was touched.
- **Operational module exposure: none.** No module page exists; the two components render only
  when a future consumer page supplies the model, and no page was added.
- **Protected foundation: none touched.** No shell file, no `_Layout`/`_PublicLayout`, no
  `SharedFrontendExtensions`, no `dmo-tokens.css`/`dmo-shell.css`/`dmo-user-shell.css`, no
  `Directory.*`, no `.csproj`, no package, no provisional-fixture marker, no P2-T01 artifact, no
  existing test method.
- **P2-T03: none.** No P2-T03 concept, carrier or partial; P2-T03 remains **NOT AUTHORIZED** and
  was not started.
- **`HISTÓRICO GLOBAL`: none.** `AuditTrail` remains a shared supplied-event presentation
  primitive, not a module; the `AuditTrail` / `HISTÓRICO` / `HISTÓRICO GLOBAL` distinction is
  documented in the carrier and preserved in code.
- **Design system: none.** No new token, no hex palette, no parallel stylesheet, no new utility
  class family.
- **Environment: none.** No live Supabase/database configuration was enabled.

## 14. Remaining Issues

**No blocking issue, no known defect, no failed or skipped P2-T02 test.** The following are the
interpretation decisions taken where the accepted contract admits more than one reading. They are
disclosed for Architect verification rather than presented as silent deviations.

1. **Component-owned trailing cell condition (§5.3 + AC-6).** §5.3 states the trailing actions
   cell renders "when at least one supplied row carries actions", while §5.3/AC-6 require an
   explicit open control for **every** row whenever `OpenEnabled` is true. The single
   component-owned trailing cell therefore renders when actions are supplied **or** `OpenEnabled`
   is true; with no supplied action and `OpenEnabled = false` it is absent entirely (asserted by
   R6). No extra column is ever added.
2. **Row status placement (§5.4, §9).** No status column is contracted and the control column is
   the only component-owned column, so the row status renders in a component-owned status slot
   inside the row's **leading** cell, in normal flow (never overlaid, never hiding or truncating
   supplied cell text, never adding or reordering a column).
3. **Single event per outcome (§5.6, §5.7).** `DenseTableOutcome` carries one optional event. A
   double click selects **and** opens, so it reports `Selected = true` with
   `Event = OpenRequested`; when `OpenEnabled` is false it reports `Event = RowSelected`. Two
   consecutive double clicks legitimately raise two open requests because the gesture's own
   selection input reopens the duplicate-open window (§5.6).
4. **Loading with retained rows (§7.1).** Read literally, the P2-T01 `loading` surface renders
   only when no retained rows are supplied; with retained rows the region is marked busy and each
   retained row carries visible stale text. Both readings are implemented exactly that way
   (asserted by R3).
5. **`Saving`/`Submitting` (§7.1).** These are declared "not table-region states"; the component
   adds no table saving/submitting surface and delegates such a state to the accepted P2-T01
   region, consistent with the §7 delegation rule for every non-`ready` state.
6. **Asset helper and AC-15.** `_SharedComponentAssets.cshtml` emits static asset `href`/`src`
   paths exactly as authorized by §14.5. AC-15's "no URL/`href`/`action`/`method`" rule is
   asserted on the two component partials and on their rendered output (R9, G4); the helper emits
   no consumer URL, no route and no component markup.
7. **`DenseTableFilterPresentation.Value` (§5.5).** The contract calls it a "mandatory controlled
   string" (not "non-blank"), so a null value is rejected while an empty string is accepted — an
   empty controlled text-filter value is meaningful and the component never interprets it.
8. **`ToStateRegion()` projection.** Both region records expose a derived projection onto the
   accepted P2-T01 `CommonStateRegionPresentation` so the partials delegate rather than
   re-implement. It is a derived read-only projection, not an input: the contracted input member
   sets are unchanged. It throws for `Ready`, which renders the table/entries instead of a state
   surface.
9. **Sort affordance direction in JS.** The adapter reads the current server-rendered `aria-sort`
   of the clicked sortable header and requests the opposite direction. This is a presentation
   affordance only: the component still never sorts and the consumer owns the actual ordering.
10. **Deferred DOM/browser verification (Q1).** Keystroke-to-event wiring and computed focus
    survival are **not** verified in a real browser, by the accepted Q1 default. Recorded as
    deferred, not omitted.
11. **Local horizontal scroll reachability.** The scroll container is focusable and labelled, but
    it contains no key handler at all, so no key is swallowed; the CSS focus style is
    `:focus-visible`. Verified statically and in markup, not in a browser (see note 10).

## 15. Next Boundary

**P2-T02 awaits independent verification and Architect implementation review.**

Required next steps per `dmo-beta-master/WORKFLOW.md` step 12:

1. independent verification of this implementation response against the accepted contract at
   `e79186a81d5cd934fe32a100bc8dd9dd08bf509a`;
2. Architect implementation review returning an explicit disposition;
3. formal closure/acceptance only by that review — **this document does not self-accept and does
   not mark P2-T02 CLOSED or ACCEPTED**.

P2-T02 status recorded in planning authority:

```text
P2-T02 STATUS = P2-T02 IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW
P2-T03 STATUS = NOT AUTHORIZED
```

**P2-T03 must not be started from this response.** No P2-T03 contract exists, none was authored
here, and no P2-T03 implementation, carrier or partial was introduced.
