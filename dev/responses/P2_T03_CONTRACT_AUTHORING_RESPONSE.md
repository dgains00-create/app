# P2-T03 — CONTRACT AUTHORING RESPONSE

**Workstream:** P2-T03 — `ToolPicker` + `ToolSummaryRow` + `MeasurementRows` + `DecisionBar` (A5/A6).
**Task class:** contract authoring only. No implementation.
**Status:** CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW.

---

## 1. Baseline

| Item | Value |
|---|---|
| DMO-MODULAR `main` at authoring (fetched; `origin/main` = local `main` = remote `main`) | `759fa0e69fb16e7125bfce7443caf0cd7bd911d1` |
| Working tree at authoring | CLEAN before authoring |
| P2-T01 implementation SHA (accepted input) | `72c38c26fa03a81465de72bed07c57f087530618` |
| P2-T01 Architect review SHA | `d71899765e8cc3cc2850407b4dd9db581e31e513` |
| P2-T02 implementation SHA / accepted contract SHA | `04601f9a827797481acd8ce4f76a6d2b1eb21af6` / `e79186a81d5cd934fe32a100bc8dd9dd08bf509a` |
| dmo-beta-master `main` | `78da49248f6cf7a8cbe4ddd946f3c38abbaf322f` |
| Contract artifact | `plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md` |
| **Contract SHA (committed and pushed to `DMO-MODULAR/main`)** | `71a12476b3298eacdda2918966199c306cc0ae29` |
| Response artifact | `dev/responses/P2_T03_CONTRACT_AUTHORING_RESPONSE.md` |
| Implementation authority boundary | P2-T03 is **not** implemented; only planning/contract/governance artifacts were written |

`git fetch origin` was run and the remote `main` SHA was verified before authoring. Nothing was
reviewed or changed in application code. The P2-T03 implementation diff boundary will be the
eventual implementation commit against `759fa0e69fb16e7125bfce7443caf0cd7bd911d1`.

Current Supabase TEST baseline (configuration loader verified; Admin live Supabase Auth PASS; User
live Auth intentionally not yet provisionable through the application; User provisioning dependent
on the Admin flow) is **not** a P2-T03 blocker and was not touched, extended or re-scoped.

## 2. Authority read

| Authority (read completely) | Used for |
|---|---|
| `plans/beta-workstreams/P2-T03-TOOLPICKER-ROWS-DECISIONBAR.md` | binding handoff: purpose §1, authority §2 (incl. "Authority blocker: **none** for the presentation/mechanics"), scope §4, non-scope §5, expected files §6, backend/auth/persistence = **none** §7, required tests §8, acceptance criteria §9, completion evidence §10, downstream §11, regression guard §12 |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §7 P2-T03 scope/non-scope/expected files/required tests/acceptance criteria, §11 P2-T03 test strategy (**U**/**UI**/**R**), §12 Protected Work Register, §13 step 4, §4 (B1 blocks P2-T04 only), and the binding **DMO FIXED DESKTOP LAYOUT POLICY** |
| `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` | §1A fixed desktop contract, §2 classifications ("opaque keys must not be declared `tool_id`/`jobon_id`"), §3 rules 1–9, §4 ten-state vocabulary and mandated distinctions, §7 `ToolPicker`, §8 `ToolSummaryRow`, §10 `RecordStatus`, §13 `MeasurementRows`, §14 `DecisionBar`, §15 ownership, §16 contract-change protocol |
| `dmo-beta-master/contracts/SHARED_FRONTEND.md` | Beta-facing consolidation of the four components; fundamental boundary; provisional-vs-final rules |
| `dmo-beta-master/IMPLEMENTATION_MODEL.md` | Workstream A/B/C/D/E ownership boundaries; A owns presentation/mechanics only |
| `dmo-beta-master/modules/FERRAMENTAS_LIGHT.md` | explicit candidate selection; never auto-select an ambiguous candidate; never infer identity from reference+lot+machine; no invented Tool IDs; no private Tool registry; no inferred compatibility; cancel restores origin unchanged; canonical `tool_id` returns to the **consumer** |
| `dmo-beta-master/ACCEPTANCE_MATRIX.md` | §2 global invariants, §3 Workstream A behaviour/evidence, §11 test-evidence gate |
| `plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md` (accepted) + its response | accepted precedent for the deterministic model + thin JS adapter (Q1), refusal to expand a frozen carrier (Q2 → AuditTrail entry status ABSENT), plain supplied text over consumer markup (Q3), additive same-stylesheet CSS, per-page/helper asset delivery, and the frozen-artifact/test-method regression pattern |
| P2-T01 + P2-T02 committed source | the exact reuse and constraint surface: the 10+3 P2-T01 artifacts, the 18 P2-T02 types, 3 partials, 2 assets, `_SharedComponentAssets.cshtml`, and the P2-T02 test classes whose assertions now constrain P2-T03's additivity (§4 below) |
| `workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md` (accepted) | §14 "A5"/"A6", §10.3, §15 expected A-owned paths — historical/execution evidence; its tablet/mobile clauses are superseded |
| `dmo-master` @ `dmo-modular` `global/ACCESS_MODEL.md` | presentation is never the security boundary (boundary statement only; no access work) |

No Beta reconciliation was reopened; P2-T01 and P2-T02 were not reopened, re-scoped or modified.

## 3. Contract created

`plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md`, containing exactly the ten
required items plus four appendices:

| Required item | Present |
|---|---|
| 1. Authority | yes — §1 (§1.1 authority order, §1.2 authority read, §1.3 authority boundary, §1.4 accepted-input register, §1.5 composition map, §1.6 non-duplication register, §1.7 protected artifacts + inherited constraints, §1.8 recorded authority silences) |
| 2. Exact component boundaries | yes — §2 (per-component owns/never-owns, nine shared boundary rules, opaque-key/canonical-identity prohibition) |
| 3. Presentation contracts | yes — §3.1 `ToolPicker`, §3.2 `ToolSummaryRow`, §3.3 `MeasurementRows`, §3.4 `DecisionBar`, §3.5 shared state integration, §3.6 styling/token contract |
| 4. State/interaction models | yes — §4.1 `ToolPickerInteraction` (transition table), §4.2 `MeasurementRowsInteraction` (semantics + invariant + focus-target contract), §4.3 `DecisionBarInteraction`, §4.4 no model for `ToolSummaryRow`, §4.5 JS thin-adapter rules |
| 5. Accessibility/focus rules | yes — §5.1–§5.5 |
| 6. Fixed desktop rules | yes — §6.1 binding rules, §6.2 supersession record, §6.3 protected-shell breakpoint carve-out, §6.4 binding control-presence proof, §6.5 reduced-width scrolling |
| 7. Explicit non-scope | yes — §7 (10 groups) |
| 8. Test-to-acceptance matrix | yes — §8.1 catalogue (99 tests), §8.2 criterion→proof matrix (52 criteria), §8.3 completeness, §8.4 deferred browser evidence |
| 9. Acceptance criteria | yes — §9 (AC-1…AC-52) |
| 10. Unresolved authority questions | yes — §10 (Q1–Q6, all NON-BLOCKING with pinned defaults and impact analysis) |
| Appendices | A protected boundaries; B file ownership/expected paths; C governance record; D PLAN REVIEW gate |

**Test-to-acceptance completeness (audited in both directions):** 52 acceptance criteria ↔ 99
catalogued tests, with **zero** criteria lacking a proof and **zero** tests lacking a criterion.

## 4. Accepted-primitive composition (P2-T01 / P2-T02)

| Reused unchanged | How P2-T03 consumes it |
|---|---|
| `CommonState`, `CommonStateTraits`, `CommonStateRegionPresentation`, `_CommonStateRegion.cshtml` | the sole state vocabulary and the sole rendering of every non-`ready` surface of all four components (`ToStateRegion()` pattern); the four mandated distinctions stay distinct; no P2-T03 partial renders a state token/heading/message/live-region of its own |
| `RecordStatusPresentation`, `StatusTone`, `_RecordStatus.cshtml` | the sole status rendering (`ToolSummaryRow`) and the reused tone vocabulary (measurement validation display) |
| `SharedActionPresentation` | the sole generic action carrier for picker create/cancel/retry/state actions, summary-row actions and **every** `DecisionBar` action; `DecisionBarActionPresentation` adds exactly **one** dimension (the supplied group) and duplicates no member; the mandatory disabled-reason rule is inherited |
| `dmo-focus.js` | the picker's close/cancel focus restoration and the rows' fallback target; no second focus helper |
| `dmo-components.css` token pattern | appended P2-T03 block, existing tokens only, no token declaration, no hex literal, no breakpoint rule |
| `_SharedComponentAssets.cshtml` | extended **additively** with the two new `defer` tags; the three existing tags remain byte-identical |
| P2-T02 interaction-model + thin-JS-adapter pattern, rendered-test helper, regression pattern | applied as three normative C# models plus two thin adapters, new rendered tests and a **new** `P2T03RegressionTests` (P2-T02's test classes untouched) |

Not used and not duplicated: `AvailabilityState`/`AvailabilityPresentation`/`_AvailabilityState`
(P2-T08's surface), `DenseDataTable`/`DenseTableInteraction`, `AuditTrail`,
`ProductionContextStrip`.

**Three inherited constraints discovered from the accepted P2-T02 tests and recorded in the
contract (§1.7)** — these are the concrete composition facts an implementer would otherwise
violate:

1. `SharedComponentAssetTests.S2`/`S3` compute the "new block" as everything from the `P2-T02 (A4)`
   CSS marker to end-of-file, so the **appended** P2-T03 CSS must itself contain no
   `@media`/`@container`/`@supports`, no `--dmo-*` declaration, no hex literal, and only published
   tokens.
2. `SharedComponentAssetTests.S4` scans `dmo-focus.js`/`dmo-dense-table.js` with a vocabulary list
   containing the bare token `tool`, so picker/rows logic must live in the **two new files**; the
   new files' own scan therefore excludes the bare token `tool` (the frozen component identity
   `ToolPicker` contains it) and forbids the canonical forms `tool_id`/`toolId`/`ToolId` instead.
3. `SharedComponentAssetTests.S5` requires the asset helper to emit each existing tag exactly once
   and no component markup, so the helper extension is additive only.

## 5. Decisions settled (pinned)

| # | Decision | Authority basis |
|---|---|---|
| D-P1 | The picker renders a picker-owned labelled candidate list with one component-owned explicit select control per candidate, fully reusing P2-T01 state/status/action primitives, and does **not** adopt `DenseDataTable` selection/open semantics. | freeze §7 candidate carrier + explicit candidate control; P2-T02 §13.2 deferred the candidate model to P2-T03; `DenseDataTable`'s frozen single-click-selects / Enter-requests-open semantics are not authorized for the picker. Recorded as §10 Q1. |
| D-P2 | The search state is explicit and controlled (`Query` rendered verbatim, never mutated); the picker searches nothing and raises `search requested` with the controlled query. | freeze §7 carrier ("common state and controlled query") + outputs + non-responsibilities |
| D-P3 | Candidate selection is raised **only** by activating that candidate's explicit select control; body/label activation selects nothing; each activation raises one event (no collapse window invented). | freeze §7 ("even one result requires explicit human selection"; "Enter/Space on an explicit candidate control selects that candidate"); accepted P2-T02 D6/D8 analogue; silence recorded in §1.8 |
| D-P4 | `SelectedCandidateKey` is controlled and fail-closed: a non-null value must reference a supplied candidate, else `Create` rejects before rendering. | freeze §7 carrier; accepted `DenseTablePresentation.Create` precedent |
| D-P5 | The candidate carrier has **no** status slot; a candidate's condition is a supplied fact. | accepted P2-T02 Q2 (generic `RecordStatus` availability does not expand a frozen carrier); freeze §7 candidate carrier |
| D-P6 | `Criar ferramenta` is rendered **iff** supplied ∧ `Visible` ∧ `Enabled` ∧ state ∈ {`ready`,`empty`}; nothing disabled is rendered, so no invented reason is needed. | freeze §7 ("only when supplied visible/enabled"); handoff §4.1; the consumer explains unavailability through the state message/reason |
| D-P7 | Cancel/return is present in every presented state (supplied action when given, else a component-owned generic control), raises `cancel requested` only, and changes no selection, query or origin state. | freeze §7 states (`unavailable`: "reason and cancel"; `permission-denied`: "cancel/return preserved") |
| D-P8 | `Escape` is exactly cancel: same event, same gate, same focus target, no discard path, no query clear, no self-close, no confirmation decision. It is therefore structurally impossible for `Escape` to discard origin work. | freeze §7 ("Escape requests cancel when safe; it never discards origin work without consumer confirmation") + §3 rule 7 |
| D-P9 | Commit-style close (`Close()`) raises `return requested` only; focus restoration applies to cancel and return via the accepted `dmo-focus.js`. | freeze §7 outputs ("`return requested`/closed after B completes its subflow"); focus rule |
| D-P10 | The origin token is optional, opaque and echoed **verbatim** on every event kind; never parsed, trimmed, normalized, interpreted or embedded in a URL; never a canonical identity. | freeze §2 + §7 carrier; handoff §4.1/§5; master plan §7 |
| D-P11 | `create requested` carries the origin token and the create action key only. | freeze §7 output phrase; smallest reading; §10 Q2 |
| D-T1 | `ToolSummaryRow` renders named optional fact slots in a fixed A-owned order (`Type`, `Reference`, `Lot`, `Machines`, `Quantity`, `Process`, `Context`, then supplied `AdditionalFacts`), each with consumer-supplied label/value, plus optional `Status` (P2-T01) and supplied actions. | freeze §8 FINAL behaviour list + carrier ("labelled display facts"); master plan §7 P2-T03; consistency/placement design priority; the slots carry no domain meaning and the primitive validates/derives nothing |
| D-T2 | A missing optional fact renders **nothing** — never `0`, `false`, `—`, empty or any placeholder; a supplied `"0"` renders as `0`. | freeze §8 ("distinguish missing optional facts from zero/false"); handoff §4.2 |
| D-T3 | `ToolSummaryRow` performs no selection arbitration: `Selected` is a controlled presentation fact and no selection event is raised. | freeze §8 outputs ("optional `selected` when embedded in a selectable consumer surface"); no arbitration authority exists |
| D-M1 | Row keys are frontend-only opaque identities, stable across edit/re-render and across removal, never canonical, never persisted. | freeze §13 FINAL behaviour + carrier |
| D-M2 | `MinimumRowCount` is configurable including `1`; removal is refused while `RowCount <= MinimumRowCount`; the model applies each accepted removal immediately so consecutive removals can never breach the minimum. | freeze §13 ("configurable minimum rows, including at-least-one mode"; "removal disabled with visible reason when minimum would be violated"); the immediate-application rule is the presentational mechanism that makes the frozen invariant true |
| D-M3 | `MinimumViolationReason` is mandatory non-blank whenever `MinimumRowCount ≥ 1`, and takes precedence over supplied-availability reasons when both block removal. | freeze §13 + §3 rule 5; the frozen requirement is that the disabled reason is always visible; reason text is supplied, never invented |
| D-M4 | Zero rows is accepted only when `MinimumRowCount == 0`; otherwise `Create` fails closed. | freeze §13 states |
| D-M5 | `Add` allocates a deterministic, non-colliding frontend key with a configurable prefix and **appends** it, reporting the key and index; the consumer applies it to its controlled row set. | freeze §13 ("generic add/remove", "stable frontend row identity"); handoff §4.3 ("add produces a new stable frontend row") |
| D-M6 | Consumer row content is supplied as **generic controlled field descriptors** (`Text`/`Numeric`/`Choice`, consumer labels/values/options/validation display) — not consumer markup. | freeze §13 carrier + "A owns generic repeated-row editing mechanics"; accepted P2-T02 Q3 direction (no markup injection); §10 Q5 |
| D-M7 | Focus: after add → first editable field of the new row (fallback: add control); after removal → first editable field of the nearest surviving row = the row now at the removed index, else the preceding row (fallback: add control); never page start. | freeze §13 keyboard/focus ("after add, focus first editable control in new row"; "after remove, focus nearest surviving corresponding control or add control"; "never reset focus to page start"). The "corresponding control" reading is pinned as the row's structural entry point and recorded in §1.8. |
| D-M8 | Structural mutation is refused with the supplied reason while saving/submitting/unavailable/permission-denied, and supplied values are always retained. | freeze §13 states ("prevent duplicate structural mutation as supplied"; "retain values"; "supplied read-only/disabled reason") |
| D-B1 | One action sequence rendered in **exactly the supplied order**, with the group as a supplied classification (stable hook + emphasis) that never reorders or relocates. | master plan §7 P2-T03 + handoff §4.4 ("action order follows supplied order"); fixed-desktop "no action relocation". Recorded as §10 Q6. |
| D-B2 | The group is mandatory (no default) and only `primary`/`secondary`/`danger` exist; `Danger` carries a visible textual/semantic marker in addition to styling. | freeze §14 ("primary, secondary, and danger action groups"; "danger includes text/semantics") |
| D-B3 | `DecisionBarActionPresentation` **composes** the accepted `SharedActionPresentation` and adds only the group — no member is duplicated, so the accepted disabled-reason rule is inherited rather than re-implemented. | freeze §3 rule 5 + §14 carrier; composition instruction in the task |
| D-B4 | Duplicate invocation is blocked **exactly while the controlled pending key equals the invoked key**; no timer, collapse window or lock is invented, and no other action is blocked by another's pending state. | freeze §3 rule 8 + §14 ("duplicate invocation prevented while supplied action is pending"); accepted P2-T02 duplicate-open collapse deliberately **not** extended, recorded in §1.8 |
| D-B5 | Availability is exactly supplied: the bar disables nothing because of the region state, its token, a warning, a conflict or a stale marker (the component cannot know which action is a "mutation" without inventing domain semantics); region state only adds the P2-T01 surface. | freeze §14 ("consumers own action, label, availability, and semantic consequence"; "A encodes no approve, reject, reopen, submit, close, or movement rule"); ACCEPTANCE_MATRIX §2 |
| D-B6 | `DecisionBar` requires **no** JavaScript adapter: the pending block is realised by the controlled pending fact (pending control rendered `disabled` + busy) plus the normative model; the consumer must set the controlled pending key synchronously and clear it on completion through `Reset`. | freeze §14 (native activation semantics; pending prevents duplicate invocation); keeps the handoff's two-asset list intact and invents no asset |
| D-A1 | Additive-only discipline: new CSS appended after the P2-T02 block; two new scripts; a new `P2T03RegressionTests`; **no** accepted P2-T01/P2-T02 artifact, test method, assertion, helper or pinned hash modified (the sole accepted-artifact edit is the additive asset-helper extension). | master plan §12 #19/#23; accepted P2-T02 §13.3 pattern; task instruction ("Do not modify P2-T01/P2-T02 contracts") |
| D-A2 | No backend/Auth/persistence/Supabase work of any kind; `CurrentBuildAvailable` stays `[]`; no route, migration, package, project or configuration change. | handoff §7; master plan §9/§12 #17/#21; task Supabase boundary |

## 6. Contract question dispositions

**Count: 6 — all NON-BLOCKING, each with a pinned default.** None blocks the PLAN REVIEW gate: the
handoff records "Authority blocker: none for the presentation/mechanics", and A1 freeze §2
explicitly permits refining PROVISIONAL carrier shapes before component implementation. If the
Architect rejects a pinned default the contract returns to `CORRECTION REQUIRED` rather than
proceeding on the default.

1. **Q1 — candidate-list mechanism** (picker-owned list **vs** delegation to the accepted
   `DenseDataTable`). *Default:* picker-owned list; the picker reuses every accepted state/status/
   action primitive but not the accepted *table* selection/open semantics, which freeze §7 does not
   authorize for a picker.
2. **Q2 — `create requested` payload** (origin token only **vs** a separate supplied prefill
   carrier). *Default:* origin token + action key only; a consumer needing prefill round-trips it
   inside its own opaque origin token.
3. **Q3 — create affordance in `stale`/`conflict`** (default `ready`/`empty` only **vs** including
   stale/conflict). *Default:* `ready`/`empty` only.
4. **Q4 — DOM/browser-level verification of the two JS adapters and the focus mechanics** (accepted
   P2-T02 Q1 pattern **vs** a newly authorized browser/JS test package). *Default:* the accepted
   P2-T02 pattern extended to P2-T03 (C# model unit tests + rendered hook assertions + static-asset
   assertions); DOM-level verification deferred. Adding a browser package would touch shared build
   files and is an Architect decision.
5. **Q5 — `MeasurementRows` row-content carrier** (generic controlled field descriptors **vs**
   consumer-rendered markup/template as freeze §13's carrier phrase could be read). *Default:*
   generic controlled field descriptors; markup injection would reverse the accepted P2-T02 Q3
   direction and make the frozen focus rules untestable without a browser. This is the most
   consequential pin and the most likely candidate for Architect correction.
6. **Q6 — `DecisionBar` group semantics** (one supplied sequence with the group as a classification
   **vs** three fixed group slots with within-group order). *Default:* one supplied sequence; the
   alternative makes the master plan's "action order follows supplied order" observably false for an
   interleaved supplied sequence.

**Recorded authority silences (not questions, no behaviour invented)** — 19 entries in contract
§1.8, including: picker search algorithm/ranking/debounce; endpoint shape; Tool identity inference;
compatibility; overlay placement/animation; Escape confirmation policy; duplicate candidate-selection
collapse; candidate ordering metadata; candidate status slot; `ToolSummaryRow` selection arbitration
and fact ordering; measurement field names/formulas/tolerance/nominal/validation; `DecisionBar`
confirmation and retry policy.

## 7. Fixed desktop compliance (1366 × 768)

- The canonical viewport is stated as binding in contract §6.1.1, with the real 768 px vertical
  constraint called out as a compact-density requirement.
- §6.1.3/§6.1.4 forbid any `@media`/`@container`/`@supports`/width-listener rule for the four
  components, and forbid card conversion, required-control hiding, action relocation and action-row
  stacking.
- §6.4 is a binding **control-presence proof** (which controls must exist in which structural
  region), tested statically (ST8) and in rendering (RTS2 + the presence rows), so "required controls
  remain present" is checkable rather than aspirational.
- §6.5 requires local keyboard-reachable overflow containers (candidate list, row grid) and page
  scrolling, with no focus trap, no clipping fixed height and no scroll lock.
- §6.2 records the supersession of the A plan §9.3/§10.3 tablet clauses,
  `IMPLEMENTATION_MODEL.md`'s "responsive behavior" listing and `ACCEPTANCE_MATRIX.md` §3's
  "responsive rendered checks" item; §6.3 carves out the protected pre-existing shell `@media` rule.
- No mobile/tablet/browser-responsive proof is required (contract §8.4).

## 8. P2-T04 boundary protection and non-scope

- Contract §7 (10 groups) and Appendix A.3 forbid any Tool canonical identity, Job On/`jobon_id`
  occurrence, CM/MF/BQ context, Controlo/Peso/Comparação/Pegamentos/Folha/Resumo concept,
  Boquilhas/movement/repairer concept, Armazém/Reparação/Tampões concept, document/PDF/availability
  concept, `ProductionContextStrip` type or partial, secondary navigation, route registration,
  module availability registration or HISTÓRICO concept.
- Opaque carrier keys are never declared, named, typed or used as `tool_id`/`jobon_id`/`cm_id`/
  `mf_id`/`bq_id` or any canonical identity (contract §2.3), proven by reflection plus file scans
  (ST3, TP16, TR9, MR15, DB11).
- The picker preserves the frozen FERRAMENTAS_LIGHT interaction contract **without** implementing
  any part of it: it never auto-selects, never infers identity, never creates, never associates and
  returns the canonical `tool_id` to the consumer rather than to the shared primitive.
- **P2-T04 is not started, specified or authorized.** Authority blocker B1 remains P2-T04's.

## 9. Supabase / backend boundary

- No Supabase integration, migration, database call, endpoint, DTO, repository, `DbContext`,
  `HttpClient`, Auth flow, session, access template, login behaviour or User provisioning was
  added, changed or proposed.
- The current Supabase TEST baseline is recorded as **not a P2-T03 blocker** (contract §1.3) and
  P2-T03 defines no backend/interface seam. Nothing in this task expands P2-T03 into Auth,
  provisioning or persistence work.

## 10. Files changed

Planning/contract/governance artifacts only. **No `src/`, `tests/`, migration, route, runtime
configuration, application CSS, JavaScript, Razor source, Supabase or backend code was created or
modified.**

| Path | Change |
|---|---|
| `plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md` | new (the implementation contract) |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | P2-T03 STATUS block only: `CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW` + contract pointer. Not `IMPLEMENTED`, not `PLAN ACCEPT`. |
| `plans/beta-workstreams/P2-T03-TOOLPICKER-ROWS-DECISIONBAR.md` | pointer line to the authored contract only |
| `dev/responses/P2_T03_CONTRACT_AUTHORING_RESPONSE.md` | new (this response) |

Verified absent from the diff: `src/**`, `tests/**`, `**/Migrations/**`, `**/*.csproj`,
`Directory.*`, `wwwroot/**`, `Program.cs`, any `.env`/Supabase configuration, and
`docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md`.

## 11. Build / test baseline (unchanged by this task)

Because P2-T03 must not change application code, no implementation test was added and no test was
weakened. The baseline was verified practically:

```text
dotnet build DMO.slnx        → Build succeeded, 0 Warning(s), 0 Error(s)
DMO.UnitTests                → 413 passed / 0 failed / 0 skipped
DMO.IntegrationTests         → 152 passed / 71 environment-gated skipped / 0 failed
CurrentBuildAvailable        → []
migrations added             → 0
application code modified    → NO
```

Local execution is developer evidence, not independent CI evidence (this repository has no
`.github/` workflow). `ACCEPTANCE_MATRIX.md` §11 evidence separation is respected.

## 12. Next gate

```text
ARCHITECT PLAN REVIEW REQUIRED BEFORE P2-T03 IMPLEMENTATION
P2-T03 contract SHA = 71a12476b3298eacdda2918966199c306cc0ae29 (pushed to DMO-MODULAR/main)
```

The Architect must review the committed contract
(`plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md`) at its pushed SHA and return
`PLAN ACCEPT` (or `CORRECTION REQUIRED`/`REJECT`), recording a disposition for Q1–Q6, per
`dmo-beta-master/WORKFLOW.md` steps 4–7.

Until then:

- P2-T03 implementation is **not** authorized and has **not** started;
- no `ToolPicker`/`ToolSummaryRow`/`MeasurementRows`/`DecisionBar` contract type, partial, CSS, JS,
  fixture or test exists;
- **P2-T04 has not started**;
- this response does **not** self-accept the contract and does not mark P2-T03 `IMPLEMENTED`.
