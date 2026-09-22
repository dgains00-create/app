# P2-T03 — `ToolPicker` + `ToolSummaryRow` + `MeasurementRows` + `DecisionBar` — IMPLEMENTATION CONTRACT

**Status:** CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW.

This is a **contract specification only**. It creates no Razor page, component, partial, CSS,
JavaScript, test, route, backend behaviour, persistence, schema, migration, canonical identity,
endpoint, Supabase change or access change. P2-T03 implementation is **not** authorized by this
document; it becomes authorized only after an Architect `PLAN ACCEPT` on this contract (Appendix D).

P2-T04, P2-T05, P2-T06, P2-T07, P2-T08, P2-T09 and P2-T10 are **not** started, specified or
authorized here.

Classification vocabulary used below is the A1 vocabulary
(`docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` §2):

- **FINAL PRESENTATION CONTRACT** — frozen consumer-visible behaviour; a change requires §16 of the
  A1 freeze.
- **PROVISIONAL FRONTEND CONTRACT** — frontend carrier shape only; no backend/persistence/schema/
  canonical-ID authority.
- **P2-T03 PIN** — a PROVISIONAL slot pinned to a concrete carrier by this contract for
  implementation. A1 freeze §2 explicitly permits refining PROVISIONAL carrier shapes before
  component implementation, which is what a PIN does.

Baselines this contract was authored against (fetched and verified, not inherited from a report):

```text
DMO-MODULAR main  (origin/main = local main = remote main)  759fa0e69fb16e7125bfce7443caf0cd7bd911d1
P2-T01 implementation SHA (accepted input)                  72c38c26fa03a81465de72bed07c57f087530618
P2-T01 Architect review SHA                                 d71899765e8cc3cc2850407b4dd9db581e31e513
P2-T02 implementation SHA (accepted input, contract)        04601f9a827797481acd8ce4f76a6d2b1eb21af6
P2-T02 accepted contract SHA                                e79186a81d5cd934fe32a100bc8dd9dd08bf509a
P2-T02 Architect PLAN ACCEPT SHA                            f1ddb968e026dc6cf2569d8de64400d8c3044514
dmo-beta-master main                                        78da49248f6cf7a8cbe4ddd946f3c38abbaf322f
```

Local baseline evidence recorded by this authoring task (developer evidence, not independent CI):
`dotnet build DMO.slnx` 0 warnings / 0 errors; `DMO.UnitTests` 413 passed / 0 failed / 0 skipped;
`DMO.IntegrationTests` 152 passed / 71 environment-gated skipped / 0 failed;
`ModuleRegistrations.CurrentBuildAvailable` = `[]`; working tree CLEAN at `759fa0e`.

Deliverables of P2-T03 (implementation stage, only after `PLAN ACCEPT`):

1. new presentation contracts under `src/DMO.Web/Frontend/Shared/Contracts/` (Appendix B.1);
2. four new shared Razor partials under `src/DMO.Web/Pages/Shared/Components/` (Appendix B.2);
3. **additive** entries in `src/DMO.Web/wwwroot/css/dmo-components.css` (§3.6, Appendix B.3);
4. two new generic static assets `wwwroot/js/dmo-tool-picker.js`,
   `wwwroot/js/dmo-measurement-rows.js` (Appendix B.4);
5. new unit + rendered + static/architecture tests (§8).

### Required-section index

| Required item | Section |
|---|---|
| 1. Authority | §1 |
| 2. Exact component boundaries | §2 |
| 3. Presentation contracts | §3 |
| 4. State/interaction models | §4 |
| 5. Accessibility/focus rules | §5 |
| 6. Fixed desktop rules | §6 |
| 7. Explicit non-scope | §7 |
| 8. Test-to-acceptance matrix | §8 |
| 9. Acceptance criteria | §9 |
| 10. Unresolved authority questions | §10 |

---

## 1. Authority

### 1.1 Authority order applied

1. `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` (A1, Architect-accepted and frozen) —
   **primary repository authority** for `ToolPicker` (§7), `ToolSummaryRow` (§8),
   `MeasurementRows` (§13), `DecisionBar` (§14), the fixed desktop contract (§1A), the shared rules
   (§3), the common state vocabulary (§4) and the classification vocabulary (§2).
2. `dmo-beta-master` @ `main` — Beta functional/scope/acceptance authority:
   `contracts/SHARED_FRONTEND.md`, `IMPLEMENTATION_MODEL.md`, `modules/FERRAMENTAS_LIGHT.md`,
   `ACCEPTANCE_MATRIX.md`.
3. `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` — sequencing authority and the binding
   **DMO FIXED DESKTOP LAYOUT POLICY**.
4. `plans/beta-workstreams/P2-T03-TOOLPICKER-ROWS-DECISIONBAR.md` — binding workstream handoff.
5. Accepted P2-T01 and P2-T02 repository artifacts — accepted input to be **consumed, not
   redesigned** (§1.4).
6. `workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md` (accepted) §14 "A5"/"A6", §10.3 —
   historical/execution evidence only; its tablet/mobile clauses are superseded (§6.2).

### 1.2 Authority read for this contract

| Authority | Used for |
|---|---|
| A1 freeze §1A | fixed desktop contract binding all four components; ToolPicker/ToolSummaryRow/MeasurementRows/DecisionBar keep their consumer-assigned structural region |
| A1 freeze §2 | classification vocabulary; PROVISIONAL carrier refinement; "opaque keys must not be declared `tool_id`/`jobon_id` or another canonical identity" |
| A1 freeze §3 rules 1–9 | the nine rules shared by every component (ownership, supplied facts, no fetch/persist/authorize/infer, no invented data, disabled reason always visible and associated, never colour-only, no focus theft, pending prevents duplicate invocation, consumer-owned callbacks/targets) |
| A1 freeze §4 | the ten-state vocabulary and the mandated `empty != lookup-failed != unavailable != permission-denied` distinctions; per-state visible behaviour, allowed interaction and announcement |
| A1 freeze §7 | `ToolPicker` purpose, FINAL behaviour, carrier, outputs, states, keyboard/focus, non-responsibilities, change protocol — the interaction rules are implemented verbatim |
| A1 freeze §8 | `ToolSummaryRow` FINAL behaviour, carrier, outputs, states, keyboard/focus, non-responsibilities |
| A1 freeze §10 | `RecordStatus` reuse for `ToolSummaryRow` status (text always rendered; tone supplementary; unknown status neutral; no lifecycle/action inference) |
| A1 freeze §13 | `MeasurementRows` FINAL behaviour, carrier, outputs, states, keyboard/focus, non-responsibilities; "C owns all" domain schema/rules |
| A1 freeze §14 | `DecisionBar` FINAL behaviour, carrier, outputs, states, keyboard/focus, non-responsibilities |
| A1 freeze §15, §16 | ownership summary and the contract-change protocol |
| `dmo-beta-master/contracts/SHARED_FRONTEND.md` | Beta-facing consolidation of the same four components; fundamental boundary (shared components own presentation/mechanics, never persistence/authorization/domain identity/endpoints/formulas/decisions/association) |
| `dmo-beta-master/IMPLEMENTATION_MODEL.md` Workstream A | A owns `ToolPicker` presentation, `ToolSummaryRow`, `RecordStatus`, generic `MeasurementRows`, generic `DecisionBar`; A owns no module-specific domain rule, persistence, formula or authorization semantics; Workstream B owns search/create/association; C owns measurement schema/rules; C/D/E own `DecisionBar` actions |
| `dmo-beta-master/modules/FERRAMENTAS_LIGHT.md` | explicit candidate selection, never auto-select an ambiguous candidate, never infer Tool identity from reference+lot+machine, no invented Tool IDs, no per-module Tool registry, no compatibility rule inferred by the frontend, cancel returns to origin with state preserved, create returns the canonical `tool_id` **to the consumer** (never to the shared primitive) |
| `dmo-beta-master/ACCEPTANCE_MATRIX.md` | §2 global invariants (explicit human selection where the master requires selection; no identity inferred from display text; no product rule invented to make implementation easier; hidden controls are presentation only, never enforcement), §3 Workstream A required behaviour/evidence, §11 test-evidence gate |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §7 P2-T03 scope/non-scope/expected files/required tests/acceptance criteria; §11 P2-T03 test strategy; §12 Protected Work Register; §13 step 4; the binding fixed desktop policy |
| `plans/beta-workstreams/P2-T03-…md` | handoff §1 purpose, §2 authority, §4 scope, §5 non-scope, §6 expected files, §7 backend/auth/persistence = **none**, §8 required tests, §9 acceptance criteria, §11 downstream dependents, §12 regression guard; "Authority blocker: **none** for the presentation/mechanics" |
| `plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md` (accepted) | accepted precedent for: the deterministic presentation-only interaction model + thin JS adapter (Q1), the refusal to expand a frozen carrier with an unlisted slot (Q2 → AuditTrail entry status **ABSENT**), plain supplied text instead of consumer markup (Q3), additive-only CSS in the same stylesheet, per-page asset delivery, and the frozen-artifact/test-method regression pattern |
| P2-T01 committed source (72c38c2) | the exact reuse surface consumed unchanged: `CommonState`, `CommonStateTraits`, `CommonStateRegionPresentation`, `StatusTone`, `RecordStatusPresentation`, `AvailabilityState`, `AvailabilityTraits`, `AvailabilityPresentation`, `AvailabilityVersionPresentation`, `SharedActionPresentation`, the three partials, the `dmo-components.css` token pattern |
| P2-T02 committed source (04601f9) | the exact composition surface: `DenseDataTable` row/column/action key model, `DenseTableInteraction`, `_DenseDataTable.cshtml`, `_AuditTrail.cshtml`, `_SharedComponentAssets.cshtml`, `dmo-focus.js`, `dmo-dense-table.js`, and the P2-T02 test classes that constrain P2-T03's additivity (§1.7) |
| `dmo-master` @ `dmo-modular` `global/ACCESS_MODEL.md` | presentation is never the security boundary; visibility is never authorization (boundary only; no access work in P2-T03) |

### 1.3 Authority boundary for this workstream

The handoff records **"Authority blocker: none for the presentation/mechanics"**. The Tool
*orchestration* that consumes the picker belongs to P2-T04 and is authority-blocked there (B1).
P2-T03 therefore has **no blocking authority gap** and defines no backend/interface seam. The
questions in §10 are refinements of PROVISIONAL carrier shapes and of evidence mechanics, not
missing authority for behaviour.

P2-T03 has **no** backend, Auth, persistence, database or Supabase requirement
(handoff §7). The current Supabase TEST baseline (configuration loader verified; Admin live
Supabase Auth PASS; User TEST provisioning intentionally not yet available through the
application) is **not** a P2-T03 blocker and P2-T03 changes nothing about it: no Supabase
integration, no migration, no database call, no User provisioning, no login behaviour, no access
template.

### 1.4 Accepted input register (consume-only)

| Accepted input | State | Use in P2-T03 |
|---|---|---|
| `CommonState` + `CommonStateTraits` | P2-T01 CLOSED/ACCEPTED | the **sole** state vocabulary for all four components |
| `CommonStateRegionPresentation` + `_CommonStateRegion.cshtml` | P2-T01 CLOSED/ACCEPTED | the **sole** rendering of every non-`ready` surface of all four components |
| `RecordStatusPresentation` + `StatusTone` + `_RecordStatus.cshtml` | P2-T01 CLOSED/ACCEPTED | the **sole** status rendering for `ToolSummaryRow` |
| `SharedActionPresentation` | P2-T01 CLOSED/ACCEPTED | the **sole** generic action carrier for the picker's create/cancel/retry/state actions, `ToolSummaryRow` actions and every `DecisionBar` action (its mandatory disabled-reason rule is inherited, never re-implemented) |
| `AvailabilityState` / `AvailabilityPresentation` / `AvailabilityTraits` / `_AvailabilityState.cshtml` | P2-T01 CLOSED/ACCEPTED | **not** used by P2-T03 and **not** duplicated (document availability is P2-T08's surface) |
| `DenseDataTable` (types, partial, `DenseTableInteraction`) | P2-T02 CLOSED/ACCEPTED | **not** modified; **not** re-used for picker candidates (recorded decision D-P1 and §10 Q1); its accepted row/column/action-key and `SharedActionPresentation` patterns are the style precedent P2-T03 follows |
| `AuditTrail` (types, partial) | P2-T02 CLOSED/ACCEPTED | **not** used and **not** duplicated |
| `_SharedComponentAssets.cshtml` | P2-T02 A-owned (not hash-pinned) | extended **additively** with the two new `defer` script tags (Appendix B.5); existing tags unchanged |
| `dmo-focus.js` | P2-T02 A-owned generic helper | reused unchanged by the picker's close/cancel focus restoration and by the rows' focus-container fallback |
| `dmo-components.css` token-usage pattern | P2-T01/P2-T02 A-owned | extended additively (§3.6) |
| `SharedComponentRenderer` / `P2T02ComponentRenderer` test approach | P2-T02 A-owned test helper | reused for rendered tests; existing methods unchanged |
| Layout / shell (`_Layout`, `_PublicLayout`, `SharedFrontendExtensions`) | PROTECTED | not modified (§ Appendix A.1) |
| `ModuleRegistrations.CurrentBuildAvailable`, route registrations, availability | PROTECTED §12 #17/#21 | untouched; stays `[]` |

### 1.5 Composition map — where P2-T03 reuses accepted primitives

| P2-T03 surface | Accepted primitive reused | How |
|---|---|---|
| every non-`ready` surface of all four components | `CommonStateRegionPresentation` + `_CommonStateRegion.cshtml` + `CommonStateTraits` | projected through `ToStateRegion()` exactly as P2-T02 does; no P2-T03 partial renders a state heading, token, message or live-region attribute of its own |
| `ToolPicker` availability/retry action, disabled reasons | `SharedActionPresentation` | supplied retry passes into `CommonStateRegionPresentation.Actions`; the disabled-reason rule and its `aria-describedby` association are the accepted P2-T01 rendered behaviour |
| `ToolPicker` cancel/return and create affordances | `SharedActionPresentation` | supplied carrier with the inherited mandatory disabled-reason rule; the create affordance's visibility rule is §3.1.6 |
| `ToolSummaryRow` status | `RecordStatusPresentation` + `_RecordStatus.cshtml` | rendered by the accepted partial; tone supplementary; text always visible; unknown status neutral; no lifecycle/action inference |
| `ToolSummaryRow` actions, `DecisionBar` actions | `SharedActionPresentation` | the sole action carrier; `DecisionBar` adds exactly one presentation dimension (the group) and duplicates no member (§3.4.2) |
| measurement-row and decision-bar validation/status tone | `StatusTone` (+ `_RecordStatus.cshtml` where a *status* is meant) | tone vocabulary reused for supplementary tone; validation text is always visible and never colour-only |
| focus restoration after picker close/cancel and the rows' fallback target | `dmo-focus.js` | the picker/rows supply the documented hooks and call the accepted helper; no second focus helper is written |
| the frozen interaction rules as unit-testable behaviour | the accepted P2-T02 pattern: deterministic presentation-only C# model + thin JS adapter | applied to `ToolPickerInteraction`, `MeasurementRowsInteraction`, `DecisionBarInteraction` (§4.5) |
| additive CSS, asset delivery, rendered-test helper, regression pattern | accepted P2-T02 conventions | same stylesheet, same per-page/helper asset delivery, same renderer approach, **new** P2-T03 test classes (P2-T02 classes unchanged) |

### 1.6 Non-duplication register (explicit "do not reimplement")

P2-T03 must not create, fork, alias or restate:

- `CommonState`, any second state enum/token/heading/message set or any component-specific state
  markup;
- `RecordStatusPresentation`, `StatusTone`, a second status vocabulary or a status→lifecycle
  mapping;
- `AvailabilityState`, `AvailabilityPresentation`, `AvailabilityTraits` or any document-availability
  concept;
- `DenseDataTable`, `DenseTableRowPresentation`, `DenseTableColumnPresentation`,
  `DenseTableInteraction` or a second selection/open arbitration;
- `AuditTrail`, `AuditEntryPresentation` or a second supplied-history renderer;
- `SharedActionPresentation` or a second generic action carrier;
- `ProductionContextStrip` or any production-context concept (not in P2-T03 scope at all);
- any second focus helper, second stylesheet, second asset-include helper or second route seam.

### 1.7 Protected accepted artifacts and static constraints P2-T03 inherits

P2-T03 is **additive only**. The following must remain byte-identical (normalized line endings),
and the P2-T03 regression test re-asserts the accepted P2-T02 pins rather than replacing them:

- the 10 accepted P2-T01 contract files and 3 accepted P2-T01 partials pinned by
  `P2T02RegressionTests.FrozenP2T01Artifacts` (SHA-256, normalized `\r\n` → `\n`);
- the accepted P2-T01 test methods and shared-shell test methods pinned by
  `P2T02RegressionTests.FrozenTestMethods`;
- `wwwroot/css/dmo-tokens.css`, `dmo-shell.css`, `dmo-user-shell.css` (including the pre-existing
  shell `@media (max-width: 62rem)` rule that `SharedShellTests` asserts — **out of P2-T03
  authority**, §6.3);
- `wwwroot/js/dmo-focus.js`, `wwwroot/js/dmo-dense-table.js`;
- `tests/DMO.IntegrationTests/Frontend/Shared/P2T02RegressionTests.cs`,
  `SharedComponentAssetTests.cs`, `DenseDataTableRenderingTests.cs`, `AuditTrailRenderingTests.cs`,
  `P2T02ComponentRenderer.cs`, `SharedStatesRenderingTests.cs`, `SharedShellTests.cs`,
  `SharedComponentRenderer.cs`, and every `tests/DMO.UnitTests/Frontend/Shared/*` file.

**Three inherited constraints derived from the accepted P2-T02 tests** (they constrain P2-T03
because those tests scan from the P2-T02 CSS marker to end-of-file, and scan named asset paths):

1. `SharedComponentAssetTests.S2`/`S3` compute the "new block" as *everything from the `P2-T02 (A4)`
   CSS marker to end-of-file*. Therefore the appended P2-T03 CSS block must contain **no**
   `@media`, `@container`, `@supports`, **no** `--dmo-*` declaration line, **no** hex colour
   literal, and must reference only tokens already declared in `dmo-tokens.css`. P2-T03's own
   static tests assert the same for its own block; the constraint is recorded so implementation
   cannot accidentally redden accepted tests.
2. `SharedComponentAssetTests.S4` scans `dmo-focus.js` and `dmo-dense-table.js` with the vocabulary
   list `{tool, jobon, peso, boquilhas, controlo, ferramentas, armaz, histórico, tampões,
   aprovação}` (case-insensitive). P2-T03 therefore must **not** add picker/rows logic into those
   two files; the two new assets are separate files. The new assets' own static scan excludes the
   bare token `tool` (the frozen component identity `ToolPicker` contains it) and instead forbids
   the canonical-identity forms (`tool_id`, `toolId`, `ToolId`) and the domain list in §8.1 ST2.
3. `SharedComponentAssetTests.S5` asserts each of the three existing asset tags is emitted exactly
   once and that the helper emits no component markup. The additive extension of
   `_SharedComponentAssets.cshtml` (Appendix B.5) keeps that green.

### 1.8 Recorded authority silences (no behaviour invented)

Where authority is silent, this contract **does not invent domain behaviour**; it either pins a
purely presentational mechanism or records the silence and leaves the decision with the consumer.

| Silence | Disposition in P2-T03 |
|---|---|
| Search algorithm, matching, ranking, scoring, debouncing, result ordering | Consumer-owned (freeze §7 non-responsibilities). The picker renders candidates in the **supplied** order and raises `search requested` only. No member computes, filters, sorts or scores. |
| Search endpoint, request shape, query serialization | Consumer-owned (freeze §7; P2-T04). The picker performs no request. |
| Tool identity inference from reference/lot/machine | Never (FERRAMENTAS_LIGHT; ACCEPTANCE_MATRIX §2). The picker treats every key as opaque. |
| Tool compatibility / suitability rule | Never (freeze §7). An unsuitable candidate is simply not supplied. |
| Picker open/close animation, overlay modality, z-index, page placement | Consumer-owned page composition. The component supplies markup, hooks and the frozen events only. |
| Escape confirmation policy (does cancelling need a confirmation?) | Consumer-owned orchestration. The picker **requests** cancel; it never discards work and never decides confirmation. |
| Duplicate candidate-selection collapse (a second activation of the same candidate control) | Authority defines no such window for the picker (it defines one for `DenseDataTable` open only). Each explicit activation raises exactly one `candidate selected`. Deliberately **not** invented. |
| Candidate ordering/priority metadata | Not defined. Candidates render in supplied order; the consumer orders them. |
| Candidate status slot | **ABSENT**, following the accepted P2-T02 Q2 precedent: freeze §7's candidate carrier is "opaque candidate key plus supplied display facts" and does not authorize a status slot. A candidate's condition is expressed as a supplied fact. |
| Whether `create requested` may carry a separate supplied prefill carrier | §10 Q2 (default: origin token only). |
| Whether the create affordance is offered in `stale`/`conflict` | §10 Q3 (default: `ready`/`empty` only). |
| `ToolSummaryRow` selection arbitration | Consumer/enclosing-surface-owned (freeze §8: "optional `selected` when embedded in a selectable consumer surface"). P2-T03 carries a **controlled** `Selected` presentation fact and raises **no** selection event. |
| `ToolSummaryRow` fact ordering | A-owned **fixed slot order** for cross-module consistency (design priority 5, §6.1); supplied labels/labels-presence remain consumer facts. |
| `MeasurementRows` consumer row-content carrier (generic controlled field descriptors vs consumer-rendered markup) | §10 Q5 (default: generic controlled field descriptors). |
| `MeasurementRows` new-row position | Pinned: appended after the last supplied row (D-M5). |
| "nearest surviving corresponding control" after removal | Pinned as the same structural entry point in the nearest surviving row: its first editable field (D-M7). |
| Measurement field names, formulas, tolerance, nominal, capacity, weight, Pegamentos semantics, domain validation | **Never in A** (freeze §13: "C owns all"; handoff §4.3). |
| `DecisionBar` visual order of the three groups | Pinned: one action sequence in exactly the supplied order; the group is a supplied classification that never reorders (D-B1; §10 Q6). |
| `DecisionBar` confirmation policy, transition retry policy, error-summary focus | Consumer-owned (freeze §14 non-responsibilities). |

---

## 2. Exact component boundaries

### 2.1 Per-component boundary

| Component | OWNS (presentation + generic mechanics) | NEVER OWNS |
|---|---|---|
| **`ToolPicker`** | search region presentation and the controlled query input; the candidate list presentation; the explicit candidate select control; the selected-candidate presentation; create/cancel/retry affordance presentation gated by supplied availability; the origin-context/expected-type display region; the opaque origin-token roundtrip; the picker's state surfaces via P2-T01; the deterministic transition and focus model; the frozen presentation events | search algorithm; ranking; compatibility; Tool identity; Tool creation; association; persistence; endpoint; authorization; prefill values; confirmation policy; navigation; production-context facts |
| **`ToolSummaryRow`** | rendering supplied labelled facts in the fixed slot order; supplied status via P2-T01; supplied actions via P2-T01; the controlled `Selected` presentation fact; the absent-fact rule | lookup; identity resolution; compatibility; inference; totals/derivation; validation; mutation; selection arbitration; navigation; authorization; any Tool field catalog |
| **`MeasurementRows`** | stable frontend row identity; the generic add/remove mechanics; minimum-row enforcement and its associated reason; the per-row structural presentation; consumer-supplied field/row validation **display**; deterministic focus targeting; the frozen presentation events | measurement field names; field catalog; formulas; tolerance; nominal; capacity; weight; Pegamentos semantics; domain validation; calculations; persistence; submission; canonical identity |
| **`DecisionBar`** | the labelled action region; supplied action presentation in supplied group and order; enabled/disabled presentation with the associated supplied reason; pending presentation; the frozen duplicate-invocation block while pending; the frozen `action invoked` event | approve; reject; reopen; submit; close; movement; release; repair; warehouse transition; transition legality; confirmation; retry policy; authorization; persistence; error-summary policy |

All four are **domain-neutral**: they render supplied facts and raise generic presentation events.
None of them fetches, persists, authorizes, infers a domain fact, constructs a URL, resolves an
identity or executes a domain transition (freeze §3 rules 1–4, 9).

### 2.2 Shared boundary rules

1. **Consumers supply facts, state, availability, labels, groups and disabled reasons.** A supplies
   presentation, generic mechanics, keyboard/focus behaviour, accessibility behaviour and the
   common state surfaces.
2. **Missing data is never replaced with invented data.** No placeholder text, no default value, no
   zero/false substitution, no `—`, no guessed label.
3. **Display text is never treated as identity.** No supplied label or value is parsed, matched,
   compared or used as a key.
4. **A disabled control always exposes a visible, programmatically associated supplied reason.**
   Where a rule of this contract suppresses an affordance instead of disabling it, that is stated
   explicitly (§3.1.6 create; §3.1.7 candidate selectability).
5. **Meaning is never colour-only.** Status, tone, selection, pending, danger and validation always
   carry visible text and/or a programmatic fact.
6. **Async changes never steal focus** except where a documented recovery flow requires it
   (§5.1.4 pickup rules).
7. **Pending prevents duplicate invocation while preserving the accessible name** and exposing
   progress.
8. **A component raises only the frozen generic event** and never acts on it. Consumer callbacks,
   form targets and navigation remain consumer-owned.
9. **No component decides access.** A `permission-denied` surface is a *published decision supplied
   by the consumer*; hiding or omitting a control is presentation only, never enforcement.

### 2.3 Opaque carrier keys and the canonical-identity prohibition

Every key carried by the four components is **opaque consumer-controlled presentation data**:

```text
ToolPickerCandidatePresentation.Key     opaque candidate key
ToolPickerPresentation.OriginToken      opaque consumer-controlled origin-state token
ToolPickerPresentation.SelectedCandidateKey (controlled opaque candidate key)
ToolPickerEvent.CandidateKey/ActionKey  opaque keys echoed back verbatim
ToolSummaryRowPresentation.ItemKey      opaque item key
ToolSummaryRowEvent.ActionKey           opaque action key
MeasurementRowPresentation.Key          opaque frontend-only row key
MeasurementRowFieldPresentation.Key     opaque frontend-only field key
MeasurementRowFieldOptionPresentation.Value  opaque option value
DecisionBarActionPresentation.Action.Key     opaque consumer action key
```

Rules (binding, testable as static/reflection proof — §8.1 ST3):

- **No key may be declared, named, typed, documented or used as `tool_id`, `jobon_id`, `job_on_id`,
  `cm_id`, `mf_id`, `bq_id`, `peso_id`, `boquilhas_id`, `movement_id`, `repairer_id`,
  `pegamentos_id`, `controlo_sheet_id`, `resumo_id`, `production_id` or any other canonical
  identity** (freeze §2; handoff §5; master plan §7 P2-T03).
- No key is parsed, split, trimmed, normalized, case-folded, compared for meaning, formatted,
  validated against a pattern, used as a dictionary key with domain meaning, or embedded in a URL,
  route, endpoint, filename or path.
- Keys round-trip **verbatim** (ordinal equality), including whitespace, non-ASCII and
  canonical-looking strings. A canonical-looking supplied key stays an opaque key.
- Keys are never generated by the shared primitive **except** `MeasurementRows` frontend row keys
  (§3.3.3), which are explicitly frontend-only, never persisted, never sent to a backend and
  explicitly not canonical identities.
- No P2-T03 contract type, partial, stylesheet, script or test fixture may contain a canonical
  identity string; the assertion is a reflection scan over member names + `const` values plus a
  file scan over the new partials, assets and fixtures (§8.1 ST3).

---

## 3. Presentation contracts

### 3.1 `ToolPicker`

Owner: Workstream A (presentation, candidate-list mechanics, keyboard/focus, accessibility).
Orchestration owner: Workstream B (search, create, association, persistence, return). Consumers:
B, C, E.

#### 3.1.1 Responsibility

Render a supplied controlled search state and a supplied candidate list, arbitrate **only** the
explicit candidate-selection interaction, present the supplied origin/expected-type context, expose
the consumer-gated create/cancel/retry affordances, delegate every non-`ready` surface to the
accepted P2-T01 state region, and raise the frozen presentation events with the opaque origin token
preserved.

It is **not** responsible for searching, ranking, matching, inferring, creating, associating,
persisting, authorizing, navigating, confirming, or deciding what a selection means.

#### 3.1.2 Carrier: labelled fact (P2-T03 PIN)

`ToolPickerFactPresentation` — sealed record, private constructor, static `Create`.

| Member | Type | Rule |
|---|---|---|
| `Label` | `string` | **mandatory, non-blank** supplied visible label. Never merged into identity. |
| `Value` | `string` | **mandatory, non-blank** supplied display value, rendered verbatim. The component never parses it, compares it, reformats it or derives anything from it. |

An **omitted** fact is not rendered. The component never substitutes `—`, `0`, `false`, an empty
string or any other placeholder for an omitted fact.

#### 3.1.3 Carrier: candidate (P2-T03 PIN)

`ToolPickerCandidatePresentation` — sealed record, private constructor, static `Create`.

| Member | Type | Rule |
|---|---|---|
| `Key` | `string` | **mandatory, non-blank**, **opaque** candidate key. Never a canonical identity, endpoint or URL (§2.3). Echoed verbatim on `candidate selected`. |
| `AccessibleContext` | `string` | **mandatory, non-blank** supplied human context used to disambiguate the candidate for assistive technology and to qualify the select control's accessible name. The component **never** derives it from supplied fact values. |
| `Facts` | `IReadOnlyList<ToolPickerFactPresentation>` | **mandatory** (may be empty), rendered in exactly the supplied order. Absent optional facts are simply not supplied. |

Rules:

- **One candidate = one separately presented entry.** Supplied candidates are never merged,
  deduplicated, grouped, collapsed, ranked or compared — "ambiguous candidates remain separate"
  (freeze §7) is structural: N supplied candidates always render N entries in supplied order.
- **No status, no icon semantics, no availability, and no per-candidate action carrier.** A
  candidate's condition is expressed by the consumer as a supplied fact (recorded silence, §1.8).
- **Candidate selectability is not modelled.** A candidate the consumer does not want selectable is
  not supplied as a candidate (§3.1.7).

#### 3.1.4 Carrier: the complete picker input (P2-T03 PIN)

`ToolPickerPresentation` — sealed record with `Create`, carrying exactly:

| Member | Type | Default | Rule |
|---|---|---|---|
| `State` | `CommonState` | — | **mandatory**; undefined enum value rejected. |
| `RegionLabel` | `string` | — | **mandatory, non-blank** accessible name of the picker surface (freeze §7: "surface has an accessible name"). A owns no Tool-domain label, so it is supplied. |
| `Query` | `string` | `""` | controlled supplied query text, rendered verbatim in the search input and echoed on `search requested`. Never mutated by the component. |
| `Candidates` | `IReadOnlyList<ToolPickerCandidatePresentation>` | `[]` | supplied candidates in supplied order. Duplicate candidate keys are rejected at `Create`. |
| `SelectedCandidateKey` | `string?` | `null` | **controlled** explicitly selected opaque candidate key. When non-null it **must** reference a supplied candidate key, else `Create` fails before rendering. |
| `OriginToken` | `string?` | `null` | **opaque** consumer-controlled origin-state token, rendered as a data hook and echoed verbatim on every event. Never parsed (§2.3). |
| `OriginContext` | `IReadOnlyList<ToolPickerFactPresentation>` | `[]` | supplied displayed origin-context facts. |
| `ExpectedTypeLabel` | `string?` | `null` | optional supplied expected-Tool-type display label; non-blank when supplied. |
| `CreateAction` | `SharedActionPresentation?` | `null` | supplied create affordance (§3.1.6). |
| `CancelAction` | `SharedActionPresentation?` | `null` | supplied cancel/return affordance; when absent the component-owned default cancel control is rendered (§3.1.8). |
| `RetryAction` | `SharedActionPresentation?` | `null` | supplied retry, passed into the delegated P2-T01 state region on `LookupFailed`. |
| `Message` | `string?` | `null` | **mandatory non-blank when `State != Ready`** (as P2-T02). |
| `Reason` | `string?` | `null` | optional supplied reason, programmatically associated when supplied. |
| `StateActions` | `IReadOnlyList<SharedActionPresentation>` | `[]` | additional supplied state actions rendered by the P2-T01 partial. |
| `ResultSummary` | `string?` | `null` | optional supplied result/count text, rendered verbatim and announced politely. Counts are computed by the consumer; the picker never counts. |

No other input exists. There is **no** search-enabled flag, no ranking/priority input, no
compatibility flag, no candidate-status slot, no per-candidate enabled flag, no Tool schema, no
endpoint, no data source, no permission flag and no navigation target.

#### 3.1.5 Search state (FINAL PRESENTATION CONTRACT)

- The search state is **explicit and controlled**: the supplied `Query` is rendered verbatim as the
  current value of the search input, and the input is never mutated, cleared, trimmed or
  normalized by the component.
- The search input is **component-owned presentation** with an A-owned generic label
  (`ToolPickerPresentation.GenericSearchLabel = "Pesquisar"`); no search-enabled flag is invented —
  the picker's frozen behaviour is "explicit search state and candidate list" (freeze §7).
- The search region is presented iff `ShowsSearch` (§3.1.9). Invoking it (activation or `Enter`)
  raises `search requested` with the current controlled query and **nothing else**.
- The component performs **no** search. It never filters, orders, scores, counts or transforms the
  supplied candidates in response to a query change (AC-1, AC-16).

#### 3.1.6 Create affordance (FINAL PRESENTATION CONTRACT)

`Criar ferramenta` **exists if and only if the consumer supplies it and enables it**:

```text
create affordance rendered  ⟺  CreateAction is not null
                                ∧ CreateAction.Visible
                                ∧ CreateAction.Enabled
                                ∧ State ∈ { Ready, Empty }
```

- When the condition is false there is **no** create control, **no** create-related disabled reason
  and **no** create short-cut of any kind. The picker never invents a create affordance and never
  offers creation because of a failure or a pending state.
- `lookup-failed` **never** offers create: a failed lookup is not "no Tool exists" (freeze §7;
  ACCEPTANCE_MATRIX §2/§3; FERRAMENTAS_LIGHT). Nor do `loading`, `unavailable`, `permission-denied`,
  `saving`, `submitting`, `stale` or `conflict`.
- Because nothing disabled is rendered, the general disabled-reason rule (freeze §3 rule 5) is not
  weakened: a consumer that wants to explain why creation is unavailable supplies that explanation
  as the region's `Message`/`Reason`, rendered by the accepted P2-T01 partial.
- Invocation raises `create requested` carrying the origin token and the create action key. The
  picker creates nothing, associates nothing, persists nothing and constructs no request.

#### 3.1.7 Candidate selection (FINAL PRESENTATION CONTRACT)

- **Never auto-select.** No transition other than an explicit candidate-select activation changes
  the presented selection: construction, `CandidatesReceived`/`Sync`, a single candidate, a query
  change, a search request, a retry, a state change, a create request or a cancel can never select
  (AC-2, AC-3).
- **Exactly one candidate still does not auto-select.** One supplied candidate is presented exactly
  like many, with its own explicit select control (AC-2).
- **Candidate selection requires explicit user action.** The only selection input is the activation
  of that candidate's explicit select control (pointer activation, or `Enter`/`Space` while the
  control is focused). Activating a candidate's facts/label/body selects nothing; there is no row
  activation, no `Space`-on-entry selection and no `Enter`-on-entry selection.
- Selection raises exactly one `candidate selected` carrying that candidate's opaque key, and sets
  the presented selection to that key. Each explicit activation raises one event; no collapse
  window is invented (recorded silence, §1.8).
- Selection is **programmatic and non-colour-only**: the selected candidate's entry carries
  `aria-selected="true"` (or an equivalent programmatic selected fact) **and** a visible
  non-colour marker element.
- The select control is **component-owned** (mirroring the accepted P2-T02 explicit-open-control
  pattern) with an A-owned generic label
  (`ToolPickerPresentation.GenericSelectControlLabel = "Selecionar"`) and an accessible name that
  includes the candidate's supplied `AccessibleContext`. It is rendered for **every** supplied
  candidate and is enabled in every presented state except `saving`/`submitting` (§3.1.9).
- While `State` is `Saving`/`Submitting` (a B-owned create/association subflow is pending), select
  controls and the create affordance are unavailable and expose the region's supplied
  `Reason`/`Message` as their associated reason; the retained candidate list stays visible so the
  origin orientation and entered work are preserved.

#### 3.1.8 Cancel, return and Escape (FINAL PRESENTATION CONTRACT)

- **Cancel returns to origin without selection.** Invoking cancel raises exactly one
  `cancel requested`; it changes no selection, clears no query, mutates no origin state, performs no
  discard and does not itself close the picker (freeze §7: B owns return orchestration).
- **Cancel/return is preserved in every presented state**, including `unavailable` and
  `permission-denied` (freeze §7 states). When the consumer supplies `CancelAction`, its supplied
  label/visibility/enabled/disabled-reason govern; when it is absent the component renders its
  component-owned default cancel control with
  `ToolPickerPresentation.GenericCancelLabel = "Cancelar"` and the component-owned opaque key
  `ToolPickerPresentation.DefaultCancelActionKey = "cancel"`.
- **`Escape` requests cancel only.** `Escape` raises exactly the same `cancel requested` event as
  the cancel control, gated by the same affordance availability; it performs no selection, no query
  change, no origin mutation, no discard and no self-close. It is therefore impossible for `Escape`
  to silently discard consumer/origin work: the shared component has no discard path at all.
- **Close/return** (`ToolPickerInteraction.Close()`) raises `return requested` only, and performs no
  selection, no discard and no origin mutation. The consumer closes after its own subflow completes
  (freeze §7: "`return requested`/closed after B completes its subflow").
- Focus restoration (§5.1.4) applies to both `cancel requested` and `return requested`.

#### 3.1.9 Presence matrix and state behaviour (freeze §7 states)

| `CommonState` | Search region | Candidate region | Select controls | Create | Cancel/return | P2-T01 state surface |
|---|---|---|---|---|---|---|
| `Ready` | yes | yes (supplied, in order) | yes, enabled | iff §3.1.6 | yes | no |
| `Empty` | yes | explicit no-results (no candidate entries) | n/a | iff §3.1.6 | yes | yes (`empty`) |
| `LookupFailed` | yes (retained safe context) | no | n/a | **never** | yes | yes (`lookup-failed` + supplied retry) |
| `Stale` | yes | yes (retained, in order) | yes, enabled | **never** | yes | yes (`stale` + supplied refresh) |
| `Conflict` | yes | yes (retained, in order) | yes, enabled | **never** | yes | yes (`conflict` + supplied recovery) |
| `Loading` | no | no | n/a | **never** | yes | yes (`loading`, busy) |
| `Unavailable` | no | no | n/a | **never** | yes (preserved) | yes (`unavailable` + supplied reason) |
| `PermissionDenied` | no | no | n/a | **never** | yes (preserved) | yes (`permission-denied` + supplied reason, no leaked candidate data) |
| `Saving` / `Submitting` | yes (retained) | yes (retained) | **disabled** + associated supplied reason | **never** | per supplied cancel (`CancelAction` availability) | yes (busy, pending progress) |

Rules:

- **`empty` is not `lookup-failed`** and neither is rendered as the other (freeze §4). A failed
  lookup renders no candidate body at all, so it can never look like "no results".
- `unavailable`/`permission-denied` render **no** candidate data (no partial protected facts).
- Non-`ready` surfaces are rendered by `_CommonStateRegion.cshtml` only (§1.5).
- The result summary (when supplied) is rendered verbatim in a polite live region; the picker never
  computes a count.

#### 3.1.10 Frozen outputs/events

`ToolPickerEventKind` = `SearchRequested | CandidateSelected | CreateRequested | CancelRequested |
RetryRequested | ReturnRequested` (the complete frozen output vocabulary of freeze §7).

`ToolPickerEvent` — sealed record: `Kind`, `OriginToken` (verbatim on **every** kind), `CandidateKey`
(present only for `CandidateSelected`), `Query` (present only for `SearchRequested`), `ActionKey`
(present for `CreateRequested`/`CancelRequested`/`RetryRequested`).

- `search requested` carries the current **controlled** query, never a component-computed query.
- `candidate selected` carries only the opaque candidate key.
- `create requested` carries only the supplied carrier data: the origin token and the create action
  key (§10 Q2 default). The picker adds no prefill.
- `cancel requested`, `retry requested`, `return requested` carry no domain payload.
- **No event creates, associates, persists, authorizes or navigates.** No event carries a URL,
  route, endpoint, canonical identity or resolved target.

#### 3.1.11 Accessibility summary

See §5.1. Surface has a supplied accessible name; search input labelled; candidate facts labelled;
select controls keyboard reachable with candidate context in their accessible name; selected state
programmatic + non-colour-only; result count/state announced politely; disabled supplied actions
expose their associated reason; `Escape` never traps focus; close/cancel/return restores the
invoking control's focus.

### 3.2 `ToolSummaryRow`

Owner: Workstream A (supplied-fact rendering). Fact providers: B/owning feature adapters.
Consumers: B, C, E.

#### 3.2.1 Responsibility

Render supplied Tool facts compactly, in a fixed slot order, distinguishing a missing optional fact
from a supplied `0`/`false` value; render the supplied status through the accepted P2-T01 status
component; render supplied actions through the accepted P2-T01 action carrier; carry a controlled
selected presentation fact. It performs **no** lookup and **no** inference.

#### 3.2.2 Carrier: fact and row (P2-T03 PIN)

`ToolSummaryFactPresentation` — sealed record: `Label` (mandatory non-blank) + `Value` (mandatory
non-blank display text, rendered verbatim).

`ToolSummaryRowPresentation` — sealed record with `Create`, carrying exactly:

| Member | Type | Default | Rule |
|---|---|---|---|
| `State` | `CommonState` | — | **mandatory**; undefined value rejected. Parent-owned except as noted below. |
| `ItemKey` | `string` | — | **mandatory, non-blank, opaque** item key. Never a canonical identity. |
| `AccessibleContext` | `string?` | `null` | optional supplied human context used to qualify supplied action accessible names. When absent, the action's supplied label is used verbatim; the component derives no context from fact values. |
| `Type`, `Reference`, `Lot`, `Machines`, `Quantity`, `Process`, `Context` | `ToolSummaryFactPresentation?` | `null` | the frozen §8 rendered items as **optional presentation slots**. An absent slot renders nothing. |
| `AdditionalFacts` | `IReadOnlyList<ToolSummaryFactPresentation>` | `[]` | optional extra supplied facts, appended after the slots in supplied order. |
| `Status` | `RecordStatusPresentation?` | `null` | supplied status, rendered by the accepted P2-T01 partial (§1.5). |
| `Actions` | `IReadOnlyList<SharedActionPresentation>` | `[]` | supplied row-scoped actions; the accepted disabled-reason rule is inherited. |
| `Selected` | `bool` | `false` | **controlled** selected presentation fact of an enclosing selectable consumer surface. Rendered programmatically and non-colour-only. It raises no event. |
| `Message` | `string?` | `null` | **mandatory non-blank when `State != Ready`**; delegated to the P2-T01 region. |
| `Reason` | `string?` | `null` | optional supplied reason. |
| `StateActions` | `IReadOnlyList<SharedActionPresentation>` | `[]` | supplied state actions for the delegated region. |

#### 3.2.3 Rendering rules (FINAL PRESENTATION CONTRACT)

1. **Render only supplied facts.** The rendered fact set is exactly the supplied fact set. No fact
   is looked up, resolved, joined, inferred, computed, aggregated, defaulted, formatted, unit-ed,
   culture-formatted, zero-suppressed or derived from another fact.
2. **Fixed slot order for cross-module consistency** (fixed desktop priority 5): `Type`,
   `Reference`, `Lot`, `Machines`, `Quantity`, `Process`, `Context`, then `AdditionalFacts` in
   supplied order. Absent slots are skipped. The slots are A-owned **presentation positions with no
   domain meaning**: the primitive never validates, compares or interprets slot content, and both
   the label and the value are supplied by the consumer.
3. **No Tool fact catalog and no required fact.** The primitive defines no minimum fact set, no
   mandatory slot and no canonical Tool field list; a consumer may supply only `AdditionalFacts`.
4. **Missing optional ≠ zero.** `Quantity = "0"` renders `0`; an absent `Quantity` renders nothing
   — no `0`, no empty cell, no placeholder.
5. **Missing optional ≠ false.** A consumer expressing a boolean-ish fact supplies its own text
   (e.g. `"Não"`); an absent slot renders nothing — never `false`, `Não`, `—` or `0`.
6. **Status reuses the accepted P2-T01 component**: text always visible, tone supplementary and
   never colour-only, unknown supplied status neutral, no lifecycle or action inference. An absent
   status renders no status. P2-T03 defines **no** status vocabulary, catalogue or tone mapping.
7. **Actions are consumer-supplied and row-scoped.** Only supplied actions are rendered; only they
   enter the tab order, in supplied order. Invocation raises `action invoked` with the opaque item
   key and the opaque action key. No action is invented, reordered, promoted or grouped.
8. **`Selected` is a controlled presentation fact only.** The component performs no selection
   arbitration (no click/Space/Enter selection, no single-selection rule): selection belongs to the
   enclosing selectable consumer surface (recorded silence, §1.8).
9. **Compact density**: one compact row, hairline separation where the consumer places it, no card
   conversion, no decorative whitespace, facts never collapsed into one unlabelled string.
10. **No URL, no form target, no navigation.** The component emits no `href`, `action`, `method`,
    route or location change.

#### 3.2.4 Frozen outputs/events

`ToolSummaryRowEventKind` = `ActionInvoked`.

`ToolSummaryRowEvent`: `Kind`, `ItemKey` (opaque, verbatim), `ActionKey` (opaque, present for
`ActionInvoked`). No selection event is raised (recorded silence, §1.8).

#### 3.2.5 States

| `CommonState` | Rendering |
|---|---|
| `Ready` | the supplied facts/status/actions (normal row) |
| non-`ready` (`Loading`, `Empty`, `LookupFailed`, `Unavailable`, `PermissionDenied`, `Stale`, `Conflict`, `Saving`, `Submitting`) | the accepted P2-T01 state region with the supplied `Message`/`Reason`/`StateActions`; **no invented facts**, no partial protected facts for `unavailable`/`permission-denied`, and `lookup-failed` is never rendered as `empty` |

A `loading` placeholder is not selectable and carries no facts (freeze §8 states).

### 3.3 `MeasurementRows`

Owner: A (generic repeated-row mechanics). Domain owner: C (measurement schema, meaning, formulas,
validation). Consumer: C.

#### 3.3.1 Responsibility

Provide generic repeated-row editing mechanics: stable frontend row identity, supplied ordered rows,
configurable minimum enforcement, generic add/remove, per-row structural presentation,
consumer-supplied field/row validation **display**, deterministic focus targeting. It computes no
validation, no formula and no domain verdict.

#### 3.3.2 Carrier: field, row, input (P2-T03 PIN)

`MeasurementRowFieldKind` = `Text | Numeric | Choice` (generic HTML control selection only — no
numeric parsing, precision, rounding, unit, range, step, `min`/`max` or scale semantics; `Numeric`
selects only the native numeric input affordance and explicitly **not** any numeric rule).

`MeasurementRowFieldOptionPresentation`: `Value` (mandatory non-blank, **opaque** option value) +
`Label` (mandatory non-blank display label).

`MeasurementRowFieldPresentation` — sealed record with `Create`:

| Member | Type | Default | Rule |
|---|---|---|---|
| `Key` | `string` | — | **mandatory, non-blank, opaque** frontend field key; unique within the row (duplicates rejected). Never a canonical identity. |
| `Label` | `string` | — | **mandatory, non-blank** supplied visible label (the consumer owns all field naming). |
| `Value` | `string` | — | **mandatory** controlled supplied value, rendered verbatim; may be empty. Never parsed or validated. |
| `Kind` | `MeasurementRowFieldKind` | `Text` | generic control selection only. |
| `Options` | `IReadOnlyList<MeasurementRowFieldOptionPresentation>?` | `null` | supplied only for `Choice`; render-only (no filtering, no matching, no auto-correction); duplicate option values rejected. |
| `Editable` | `bool` | `true` | when `false` the control renders read-only/disabled and **must** carry `DisabledReason`. |
| `DisabledReason` | `string?` | `null` | **mandatory non-blank when `Editable == false`** (inherited disabled-reason rule). |
| `ValidationText` | `string?` | `null` | consumer-supplied validation **display** text, rendered verbatim. |
| `ValidationTone` | `StatusTone` | `Neutral` | supplementary tone; text is always present, so meaning is never colour-only. |

`MeasurementRowPresentation` — sealed record with `Create`: `Key` (mandatory non-blank **opaque
frontend row key**; duplicates rejected), `AccessibleContext` (mandatory non-blank supplied row
context used to disambiguate the row's controls for assistive technology), `Fields` (mandatory,
supplied order preserved), `ValidationText` (`string?` row-level supplied validation display),
`ValidationTone` (`StatusTone`, default `Neutral`).

`MeasurementRowsPresentation` — sealed record with `Create`, carrying exactly:

| Member | Type | Default | Rule |
|---|---|---|---|
| `State` | `CommonState` | — | **mandatory**; undefined value rejected. |
| `RegionLabel` | `string` | — | **mandatory, non-blank** accessible name. |
| `Rows` | `IReadOnlyList<MeasurementRowPresentation>` | `[]` | supplied controlled ordered rows; supplied order is authoritative and never re-sorted. |
| `MinimumRowCount` | `int` | — | **mandatory, ≥ 0**; the configurable minimum including the at-least-one case. |
| `MinimumViolationReason` | `string?` | `null` | **mandatory non-blank when `MinimumRowCount ≥ 1`** — the visible, associated reason exposed when removal would violate the minimum. Rejected at `Create` when `MinimumRowCount ≥ 1` and absent. |
| `AddEnabled` | `bool` | `true` | supplied add availability; when `false`, `AddDisabledReason` is mandatory. |
| `AddDisabledReason` | `string?` | `null` | mandatory non-blank when `AddEnabled == false`. |
| `RemoveEnabled` | `bool` | `true` | supplied per-row remove availability; when `false`, `RemoveDisabledReason` is mandatory. |
| `RemoveDisabledReason` | `string?` | `null` | mandatory non-blank when `RemoveEnabled == false`. |
| `StructuralMutationAllowed` | `bool` | `true` | supplied structural-mutation availability (set by the consumer from its state); when `false`, `StructuralMutationReason` is mandatory. |
| `StructuralMutationReason` | `string?` | `null` | mandatory non-blank when `StructuralMutationAllowed == false`. |
| `Message`, `Reason`, `StateActions` | as §3.1.4 | — | `Message` mandatory non-blank when `State != Ready`; delegated to P2-T01. |

Fail-closed at `Create` (all rejected **before rendering**): undefined `State`; blank `RegionLabel`;
negative `MinimumRowCount`; `Rows.Count < MinimumRowCount`; missing `MinimumViolationReason` when
`MinimumRowCount ≥ 1`; blank row key; duplicate row keys; blank row `AccessibleContext`; blank field
key; duplicate field keys within a row; blank field `Label`; `Options` supplied for a non-`Choice`
kind; `Choice` without options; duplicate option values; `Editable == false` without
`DisabledReason`; missing `AddDisabledReason` when `AddEnabled == false`; missing
`RemoveDisabledReason` when `RemoveEnabled == false`; missing `StructuralMutationReason` when
`StructuralMutationAllowed == false`; missing `Message` when `State != Ready`.

There is **no** measurement field catalog, no nominal, no tolerance, no formula, no capacity, no
weight, no unit, no precision, no Pegamentos concept and no domain rule anywhere in the carrier.

#### 3.3.3 Row identity (FINAL PRESENTATION CONTRACT)

- Row keys are **frontend-only** presentation identities: never persisted, never submitted, never a
  canonical measurement or database identity (freeze §13: "The row key is frontend-only, never a
  canonical measurement/database identity").
- **Identity is stable across edit and re-render.** Editing a row's field values never changes its
  key. Re-syncing the model with the controlled rows preserves every surviving supplied key exactly
  (no re-keying, no renumbering, no index-derived identity).
- **Identity is stable across removal.** After a removal the surviving rows keep their exact
  supplied keys and their relative order.
- **Add produces a new stable frontend row**: the primitive allocates a key that is not currently in
  the supplied/tracked row set, reports it (and its append index) in the outcome, and keeps it
  stable for as long as the consumer re-supplies that row.
- A new row is **appended after the last supplied row** (D-M5). The consumer applies the allocated
  key at the reported index when it supplies the updated row set.
- The primitive **never** generates a canonical-looking identity: the allocated key uses the
  configured frontend prefix (`MeasurementRowsInteraction.DefaultKeyPrefix = "dmo-row-"`) plus a
  monotonic counter, and is skipped forward if it would collide with an existing tracked key.

#### 3.3.4 Minimum enforcement (FINAL PRESENTATION CONTRACT)

- The minimum is configurable and **may be 1** (`MinimumRowCount = 1` is the at-least-one case).
- **Removal is disabled when it would violate the minimum**: while the tracked row count is
  `<= MinimumRowCount`, every remove control renders disabled and no `remove requested` is raised.
- **The disabled removal exposes a visible, programmatically associated supplied reason**
  (`MinimumViolationReason`, mandatory whenever `MinimumRowCount ≥ 1`). The primitive never invents
  the reason text.
- **Precedence**: when removal is blocked both by the minimum **and** by supplied availability
  (`RemoveEnabled == false` / `StructuralMutationAllowed == false`), the **minimum violation reason**
  is the associated reason, because it is the rule this contract freezes as always visible; the
  supplied availability reason is used when the minimum is not violated. (Deterministic and tested.)
- Zero rows is valid **only** when `MinimumRowCount == 0`; with `MinimumRowCount ≥ 1` a zero-row
  presentation fails closed at `Create` (freeze §13: "`empty`: valid only when minimum is zero").
- Consecutive removals can never take the tracked count below the minimum: the model applies each
  accepted removal to its tracked set immediately and re-syncs from the controlled input
  (§4.2.2).

#### 3.3.5 Focus and structure (FINAL PRESENTATION CONTRACT)

- Logical row-major order; the add control follows the rows in the tab order.
- **After add**: focus moves to the first editable control in the new row; when the new row supplies
  no editable field, the documented fallback is the add control.
- **After remove**: focus moves to the nearest surviving row's same structural entry point — its
  first editable control; when no editable control survives, the documented fallback is the add
  control.
- **Structure changes never reset focus to page start**: every `add`/`remove` outcome exposes a
  documented focus target (§4.2.3).
- Remove controls identify their row context in their accessible name (supplied
  `AccessibleContext`); repeated fields carry unique labels; structural changes are announced
  politely.
- Each row renders its supplied accessible context; validation display is associated with its
  field/row and transported with visible text (never colour-only).

#### 3.3.6 States (freeze §13)

| `CommonState` | Rows + supplied values | Add control | Remove controls | Field controls | State surface |
|---|---|---|---|---|---|
| `Ready` | rendered | enabled iff `AddEnabled` | enabled iff `RemoveEnabled` ∧ `StructuralMutationAllowed` ∧ `count > MinimumRowCount` | editable per field `Editable` | no |
| `Empty` | none (valid only when `MinimumRowCount == 0`) | enabled iff `AddEnabled` | n/a | n/a | yes (`empty` + supplied message/next action) |
| `Saving` / `Submitting` | **retained** | disabled + reason (structural mutation unavailable) | disabled + reason | read-only/disabled | yes (busy; values retained) |
| `Unavailable` | **retained** | disabled + supplied reason | disabled + supplied reason | read-only/disabled | yes (`unavailable` + supplied reason) |
| `PermissionDenied` | **retained** | disabled + supplied reason | disabled + supplied reason | read-only/disabled | yes (`permission-denied` + supplied reason) |
| `Stale` | **retained** | per supplied availability | per supplied availability | per supplied availability | yes (`stale` warning + supplied refresh) |
| `Conflict` | **retained** | per supplied availability | per supplied availability | per supplied availability | yes (`conflict` + supplied recovery) |
| `Loading` / `LookupFailed` | not rendered (parent-owned domain lookup) | no | no | no | yes (P2-T01, never `empty`) |

Values are **never** lost by a state change: every retained-state row renders its supplied values
verbatim.

#### 3.3.7 Frozen outputs/events

`MeasurementRowsEventKind` = `AddRequested | RemoveRequested | ValueChanged`.

`MeasurementRowsEvent`: `Kind`, `RowKey` (opaque, present for `RemoveRequested`/`ValueChanged`),
`FieldKey` (opaque, present for `ValueChanged`), `Value` (present for `ValueChanged`, verbatim).

- `add requested` carries the newly allocated frontend row key and its append index.
- `remove requested` carries the removed row's opaque frontend key.
- `value changed` is the generic binding hook through which the consumer receives a changed field
  value; the primitive performs **no** validation and produces **no** calculation or persistence
  event.

### 3.4 `DecisionBar`

Owner: A (generic action presentation). Action owners: C, D, E. Consumers: C, D, E.

#### 3.4.1 Responsibility

Present a supplied, ordered list of consumer actions, each in its supplied primary/secondary/danger
group, with supplied visible/enabled/disabled-reason/pending presentation, prevent duplicate
invocation while an action is pending, and raise the frozen generic event. It encodes no transition.

#### 3.4.2 Carrier (P2-T03 PIN)

`DecisionBarActionGroup` = `Primary | Secondary | Danger`.

`DecisionBarActionPresentation` — sealed record with `Create(SharedActionPresentation action,
DecisionBarActionGroup group)`:

- `Action` — the **accepted P2-T01** `SharedActionPresentation` (reused verbatim: key, label,
  visible, enabled, disabled reason, pending label). P2-T03 duplicates **no** member of it, and the
  accepted mandatory disabled-reason rule is inherited, not re-implemented.
- `Group` — **mandatory** (no default): the component never guesses an action's emphasis. An
  undefined enum value is rejected.

`DecisionBarPresentation` — sealed record with `Create`, carrying exactly: `State`
(`CommonState`, mandatory), `Actions` (`IReadOnlyList<DecisionBarActionPresentation>`, supplied
order authoritative; duplicate action keys rejected), `PendingActionKey` (`string?`, **controlled**;
when non-null it must reference a supplied action key), `RegionLabel` (`string?`, defaults to the
A-owned generic label `DecisionBarPresentation.GenericRegionLabel = "Ações"`), `StatusText`
(`string?`, optional supplied help/status text rendered verbatim), `Message` (`string?`, mandatory
non-blank when `State != Ready`), `Reason` (`string?`), `StateActions`
(`IReadOnlyList<SharedActionPresentation>`).

No action semantic, transition name, confirmation rule, retry policy, ordering rule or
authorization input exists.

#### 3.4.3 Order and groups (FINAL PRESENTATION CONTRACT)

- **Supplied order is authoritative.** The rendered action sequence equals the supplied sequence
  exactly. The group is a supplied **classification** that never reorders, never relocates and never
  filters: an interleaved supplied sequence renders interleaved.
- **Group assignment is preserved** and exposed per action (a stable group hook/marker) so a consumer
  and a test can observe it; the three groups are the only groups.
- **Group presentation is non-colour-only**: a `Danger` action carries a visible textual/semantic
  marker in addition to styling (freeze §14: "danger includes text/semantics").
- Actions never move to a different structural region: the bar is one fixed region in the consumer's
  assigned area (§6).

#### 3.4.4 Availability, pending and duplicate prevention (FINAL PRESENTATION CONTRACT)

- **Availability is exactly supplied.** The component disables no action because of the region
  state, the state token, a warning, a conflict or a stale marker: it cannot know which supplied
  action is a "mutation" without inventing domain semantics. Region state only adds the P2-T01 state
  surface (message/reason/recovery actions) alongside the bar.
- A supplied disabled action renders disabled and exposes its **supplied** reason programmatically
  associated; the component invents no reason text.
- **Pending state**: while the controlled `PendingActionKey` equals an action's key, that action
  renders its supplied `PendingLabel`, is marked busy, exposes progress visibly (text, not
  colour-only) and **retains its accessible name**.
- **Duplicate invocation is prevented while pending**: `Invoke(key)` raises **no** `action invoked`
  and no other event while `PendingActionKey == key`. No other action is blocked by another action's
  pending state; their own supplied availability governs.
- The component does **not** invent a timing/collapse window beyond the controlled pending fact
  (recorded silence, §1.8). The **consumer obligation** is therefore explicit: the consumer sets the
  controlled `PendingActionKey` synchronously when it accepts an invocation, and clears it on
  completion/failure through `Reset`.
- `permission-denied` availability is supplied by the published decision: an action is omitted or
  disabled exactly as supplied. **Hiding is presentation only, never enforcement** — the server-side
  gate remains the boundary (ACCEPTANCE_MATRIX §2; ACCESS_MODEL).

#### 3.4.5 States

| `CommonState` | Rendering |
|---|---|
| `Ready` | supplied actions in supplied order/group with supplied availability |
| `Saving` / `Submitting` | supplied actions; the pending action shows its pending label, is busy and cannot be re-invoked; other actions follow supplied availability; busy region |
| `Unavailable` | P2-T01 `unavailable` surface with the supplied reason and permitted alternatives (supplied `StateActions`) |
| `PermissionDenied` | P2-T01 `permission-denied` surface with the supplied reason; actions omitted/disabled exactly per the supplied decision |
| `Stale` / `Conflict` | P2-T01 surface with the supplied warning/summary and supplied recovery actions; **supplied** availability governs the bar's actions — the component disables nothing on its own |
| `Loading` / `Empty` / `LookupFailed` | P2-T01 surface with the supplied message/reason; the consumer may supply disabled actions with reasons |

#### 3.4.6 Frozen outputs/events

`DecisionBarEventKind` = `ActionInvoked`.

`DecisionBarEvent`: `Kind`, `ActionKey` (opaque consumer action key, verbatim).

No transition result, domain event, confirmation event, retry event or navigation target is defined
by A.

#### 3.4.7 No JavaScript adapter

`DecisionBar` requires **no** JavaScript: its duplicate-invocation guarantee is realised by the
controlled pending fact (the pending action renders `disabled` + busy) plus the normative
`DecisionBarInteraction` model (§4.3). No third script asset is introduced (deviation from the
handoff's optional asset list is avoided deliberately; the handoff lists the two scripts as
"optional"/expected, and this contract adds none beyond them).

### 3.5 Shared state integration

All four components use the accepted P2-T01 vocabulary. No component-specific state enum, token,
heading, message set or live-region attribute exists in P2-T03.

Delegation rule (all four):

- every non-`ready` surface renders through the accepted `CommonStateRegionPresentation` /
  `_CommonStateRegion.cshtml` semantics — same token, heading, message, live-region behaviour and
  associated reason as P2-T01; no P2-T03 partial re-implements a state surface;
- **`empty`, `lookup-failed`, `unavailable` and `permission-denied` remain mutually distinct
  renderings** and must never be collapsed or aliased;
- `lookup-failed` is never rendered as `empty` (and therefore never as "no Tool exists"), and an
  empty result is never rendered as a failure.

### 3.6 Styling / token contract

- **Additive only, same stylesheet**: new rules are appended to
  `src/DMO.Web/wwwroot/css/dmo-components.css` **after** the existing P2-T02 block. No parallel
  stylesheet. No pre-existing selector is rewritten, reordered or removed.
- **Existing design tokens only**: every `var(--dmo-*)` used must already be declared in
  `wwwroot/css/dmo-tokens.css`. **No new token, no `--dmo-*` declaration line and no hex colour
  literal** in the new block (this also keeps the accepted P2-T02 `S2`/`S3` assertions green, §1.7).
- **Scoped selectors**: only `dmo-picker*`, `dmo-summary*`, `dmo-rows*`, `dmo-decision*`, plus reuse
  of the accepted `visually-hidden` helper. No new generic utility classes.
- **No breakpoint/container/supports rule** and no width-conditional structural rule (§6.1.3).
- **Component visual contract**:
  - `ToolPicker`: one fixed structural composition — context/origin region, search row, candidate
    list, action row (create/cancel). Candidate entries are compact list entries with hairline
    separation; the selected entry uses background/border change **plus** the visible marker; the
    select control sits in a fixed trailing position within the entry, independent of width and
    fact content. The candidate list may use a local vertical/horizontal overflow container
    (§6.5). No cards, no decorative whitespace.
  - `ToolSummaryRow`: one compact dense row; facts in the fixed slot order with visible labels;
    status in its own slot; actions in a fixed trailing position; no card inflation.
  - `MeasurementRows`: rows as a compact grid with a stable column rhythm (row context / fields /
    remove control); the remove control sits in a fixed trailing position; a local horizontal
    overflow container wraps the row grid when the region is narrow (§6.5); validation text sits
    with its field/row, never overlaid.
  - `DecisionBar`: one fixed horizontal action region preserving supplied order; group styling plus a
    visible danger marker; pending text replaces neither the label nor the accessible name; no
    relocation into overflow menus and no vertical stacking (§6.1.4).
- No decorative styling beyond the above. Visual polish ranks last in the binding design priority
  order (workflow stability, control placement, density, readability, consistency, polish).

---

## 4. State and interaction models

Because the frozen interaction rules must be **failing-if-removed unit-tested** while this repository
has no JavaScript test runner and no authorized browser test package, each interactive component's
mechanics are implemented as a small, deterministic, presentation-only C# model, and the JavaScript
is a thin DOM adapter over the same rules (accepted P2-T02 pattern; §10 Q4).

### 4.1 `ToolPickerInteraction` (deterministic transition model)

```text
ToolPickerInteraction(bool cancelEnabled)
    bool                       CancelEnabled { get; }
    string?                    OriginToken   { get; }     // opaque, never parsed
    string?                    Query         { get; }     // controlled
    string?                    SelectedCandidateKey { get; }
    IReadOnlyList<string>      CandidateKeys { get; }
    bool                       HasSelection => SelectedCandidateKey is not null

    ToolPickerOutcome Open()
    ToolPickerOutcome QueryChanged(string query)
    ToolPickerOutcome CandidatesReceived(IReadOnlyList<string> candidateKeys)
    ToolPickerOutcome EnterFromSearch()
    ToolPickerOutcome SelectCandidate(string candidateKey)
    ToolPickerOutcome RequestCreate(string actionKey)
    ToolPickerOutcome Cancel(string? actionKey = null)
    ToolPickerOutcome Escape()
    ToolPickerOutcome RequestRetry(string actionKey)
    ToolPickerOutcome Close()
    void              Sync(string? query, IReadOnlyList<string> candidateKeys, string? selectedCandidateKey,
                           string? originToken)

ToolPickerOutcome(ToolPickerEvent? Event, ToolPickerFocusTarget FocusTarget, bool Blocked)
```

Transition table (normative):

| Transition | Event | Selection effect | Query effect | Focus target |
|---|---|---|---|---|
| `Open()` | none | none | none | `SearchInput` when the search region is presented, else `RegionHeading` |
| `QueryChanged(q)` | none | **none** (never selects) | adopts `q` as the controlled value; never searches | `Unchanged` |
| `CandidatesReceived(keys)` | none | **none** (never selects, even for one candidate) | none | `Unchanged` |
| `EnterFromSearch()` | `SearchRequested(Query)` **only** | **none** (never selects the first/only candidate) | none | `Unchanged` |
| `SelectCandidate(k)` | `CandidateSelected(k)` once | sets `SelectedCandidateKey = k` | none | `Unchanged` |
| `RequestCreate(a)` | `CreateRequested(a)` | **none** | none | `Unchanged` |
| `Cancel(a)` / `Escape()` | `CancelRequested(a ?? DefaultCancelActionKey)` once | `SelectedCandidateKey` **unchanged** (cancel never selects and never clears the presented selection) | **unchanged** (never cleared) | `InvokingControl` |
| `RequestRetry(a)` | `RetryRequested(a)` | none | none | `Unchanged` |
| `Close()` | `ReturnRequested` | none | none | `InvokingControl` |

Rules:

- `SelectCandidate(k)` throws when `k` is blank; when `k` is not among `CandidateKeys` it raises
  **no** event and returns `Blocked = true` (a selection can never name an unsupplied candidate).
- `Cancel`/`Escape` are gated by `CancelEnabled`; when cancel is unavailable they raise no event and
  return `Blocked = true`. Nothing else about them changes.
- `Escape()` is exactly `Cancel()`: same event, same gate, same focus target, same absence of
  discard. There is no other `Escape` behaviour (no clear-query, no self-close, no discard, no
  confirmation).
- `Sync(...)` re-syncs the controlled inputs (query, candidates, selection, origin token) without
  raising any event and **never** selecting: a candidate arriving by sync cannot become selected.
  `Sync` rejects a non-null selection that is not among the supplied candidate keys.
- The model holds no service, clock, session, identity, URL, route, endpoint, ranking or domain
  vocabulary, and exposes no member returning a `Uri`, route or navigation target. It performs no
  matching, filtering, ordering, scoring or counting.

### 4.2 `MeasurementRowsInteraction` (deterministic mechanics model)

```text
MeasurementRowsInteraction(int minimumRowCount, string keyPrefix = "dmo-row-")
    int                    MinimumRowCount { get; }
    IReadOnlyList<string>  RowKeys { get; }
    int                    RowCount { get; }
    bool                   AllowsStructuralMutation { get; }
    string?                StructuralMutationReason { get; }
    string?                LastAllocatedKey { get; }
    int?                   LastAllocatedIndex { get; }
    MeasurementRowsFocusTarget FocusTarget { get; }
    string?                RemovalDisabledReason { get; }   // the reason currently exposed for removal

    void                    Sync(IReadOnlyList<MeasurementRowPresentation> rows)
    void                    SetStructuralMutation(bool allowed, string? reason)
    MeasurementRowsOutcome  Add()
    MeasurementRowsOutcome  Remove(string rowKey)
    MeasurementRowsOutcome  ChangeValue(string rowKey, string fieldKey, string value)
    MeasurementRowsFocusTarget ResolveFocusTarget()

MeasurementRowsOutcome(MeasurementRowsEvent? Event, MeasurementRowsFocusTarget FocusTarget,
                       bool Refused, string? RefusalReason, string? AllocatedRowKey, int? AllocatedIndex)

MeasurementRowsFocusKind = FirstEditableFieldInRow | NearestSurvivingRowFirstEditableField | AddControl | None
MeasurementRowsFocusTarget(MeasurementRowsFocusKind Kind, string? RowKey, string? FieldKey)
```

#### 4.2.1 Deterministic semantics

- `Sync(rows)` adopts the consumer's controlled rows: it sets the tracked row keys and the supplied
  field-editability information, and it **preserves any tracked key that still exists** (no
  re-keying, no renumbering). It raises no event.
- `SetStructuralMutation(allowed, reason)` mirrors the consumer's state-derived structural
  availability (`Saving`/`Submitting`/`Unavailable`/`PermissionDenied` ⇒ not allowed) and the
  associated supplied reason.
- `Add()`:
  - refused with `StructuralMutationReason` when structural mutation is not allowed;
  - otherwise allocates a key from `keyPrefix` + a monotonic counter, skipping any candidate already
    tracked, **appends** it to the tracked keys, and raises `AddRequested(allocatedKey)` once with
    `AllocatedIndex = previous count`;
  - `FocusTarget = FirstEditableFieldInRow(allocatedKey, fieldKey: null)` (resolved after the next
    `Sync` by `ResolveFocusTarget()`).
- `Remove(rowKey)`:
  - refused with `StructuralMutationReason` when structural mutation is not allowed;
  - refused with `MinimumViolationReason` when `RowCount <= MinimumRowCount` (removal would violate
    the minimum);
  - refused with `RemoveDisabledReason` when the row is not removable per supplied availability
    (the minimum check above takes precedence, §3.3.4);
  - otherwise removes the key from the tracked set **immediately** (so consecutive removals can
    never breach the minimum before the consumer re-syncs), raises `RemoveRequested(rowKey)` once,
    and sets `FocusTarget = NearestSurvivingRowFirstEditableField(nearestKey, fieldKey: null)`.
- `ChangeValue(rowKey, fieldKey, value)` raises `ValueChanged(rowKey, fieldKey, value)` and **never
  changes row identity** and never validates. A blank value is allowed.
- `ResolveFocusTarget()` resolves the pending intent against the last synced rows: the first field
  with `Editable == true` in the target row, else `AddControl`. It never returns a page-start target
  (recorded silence: "structure changes never reset focus to page start").
- "Nearest surviving row" = the row that now occupies the removed row's index; when the removed row
  was last, the preceding row.

#### 4.2.2 Invariant

At every point, `RowCount >= MinimumRowCount` holds for every accepted sequence of `Sync`, `Add` and
`Remove` operations, and every surviving key is byte-identical to the key the consumer supplied for
that row.

#### 4.2.3 Focus-target contract

| Situation | Immediate target | Resolved target (after `Sync`) |
|---|---|---|
| after `Add()` | `FirstEditableFieldInRow(allocatedKey, null)` | first editable field key of the new row, else `AddControl` |
| after accepted `Remove(k)` | `NearestSurvivingRowFirstEditableField(nearestKey, null)` | first editable field key of the nearest surviving row, else `AddControl` |
| after a refused add/remove | `None` (no structural change, no focus move) | `None` |

### 4.3 `DecisionBarInteraction` (deterministic invocation model)

```text
DecisionBarInteraction(string? pendingActionKey = null)
    string?              PendingActionKey { get; }
    DecisionBarOutcome   Invoke(string actionKey)
    void                 Reset(string? pendingActionKey = null)
    void                 Sync(string? pendingActionKey)

DecisionBarOutcome(DecisionBarEvent? Event, bool DuplicateBlocked)
```

Semantics (normative):

- `Invoke(k)` throws when `k` is blank.
- `Invoke(k)` is **blocked** (`DuplicateBlocked = true`, no event) exactly when
  `PendingActionKey is not null && string.Equals(PendingActionKey, k, StringComparison.Ordinal)`.
  This is the frozen "duplicate invocation prevented while pending" rule.
- Otherwise `Invoke(k)` raises exactly one `ActionInvoked(k)`.
- **No other suppression exists**: a repeated invocation of an enabled, non-pending action raises a
  second event. There is no timer, no collapse window, no lock, no domain rule and no transition
  knowledge in the model.
- `Reset(pendingActionKey)` / `Sync(pendingActionKey)` adopt the consumer's controlled pending value
  (typically `null` on completion or on failure) and raise no event. Completion/reset is the
  consumer's act, and it re-enables the previously pending action.
- The consumer obligation (§3.4.4) is that it sets the controlled pending key synchronously when it
  accepts an invocation.

### 4.4 `ToolSummaryRow` — no interaction model

`ToolSummaryRow` owns no arbitration: it renders supplied facts/status/actions and raises only
`action invoked`. It therefore has no interaction model, and a contract test asserts that no
selection/click/Space/Enter arbitration member exists on any `ToolSummaryRow` type.

### 4.5 JavaScript: thin DOM adapter only

Two new generic assets, both domain-neutral:

```text
src/DMO.Web/wwwroot/js/dmo-tool-picker.js        picker presentation only
src/DMO.Web/wwwroot/js/dmo-measurement-rows.js   generic row mechanics only
```

Binding rules for both:

1. **Thin adapter.** They read the rendered `data-dmo-*` hooks, apply exact transitions of the
   normative C# model, and dispatch the frozen generic events. They contain **no** domain rule, no
   search algorithm, no ranking, no compatibility, no measurement field, no formula, no tolerance,
   no nominal, no transition legality, no confirmation policy and no validation.
2. **No I/O.** No `fetch(`, `XMLHttpRequest`, `WebSocket`, endpoint, `location.`, `href`,
   `window.open`, `submit(`, form target, storage write, cookie, timer or `setTimeout`-based
   domain logic.
3. **No width behaviour.** No `matchMedia`, no `resize` listener, no `ResizeObserver`-driven
   structural change: width never alters structure (§6).
4. **No focus trap.** No key handler may swallow `Tab`. `Escape`/`Enter` handlers are scoped to the
   component root and call `preventDefault()` only where the frozen interaction requires it
   (e.g. `Enter` in the search input must not submit a form).
5. **Idempotent.** Loading a script twice installs no second listener
   (`if (window.dmoToolPicker) return;` / `if (window.dmoMeasurementRows) return;`), matching the
   accepted `dmo-focus.js`/`dmo-dense-table.js` pattern.
6. **Focus restoration** reuses the accepted `window.dmoFocus` helper; no second focus helper is
   written.
7. Neither adapter is added to `dmo-focus.js` or `dmo-dense-table.js` (§1.7 constraint 2).

---

## 5. Accessibility and focus rules

Native semantics first; ARIA only where native HTML cannot express the frozen requirement.

### 5.1 `ToolPicker`

1. **Accessible name**: the surface is a labelled region carrying the supplied `RegionLabel`.
2. **Search**: the search input has a visible/programmatic label (supplied or the A-owned generic
   label); it is keyboard reachable; its current value is the controlled query.
3. **Candidates**: candidates render as a labelled list; every supplied fact renders with its
   visible label and is never collapsed into an unlabelled string; candidate entries are **not**
   selectable by body activation and are not in the tab order except for their select control.
4. **Focus**:
   - opening moves focus to the search input (accepted surface pattern) — the only focus move the
     picker initiates on open;
   - the search input, the candidate select controls, the create control (when rendered) and the
     cancel/return control are keyboard reachable, in a logical order (search → candidates in
     supplied order → create → cancel);
   - `Enter` in the search input requests search and **never** selects a candidate and never submits
     a form;
   - `Enter`/`Space` on a candidate's select control selects that candidate;
   - `Escape` requests cancel; it is never swallowed into a discard;
   - **close/cancel/return restores focus to the invoking control** through the accepted
     `data-dmo-focus-return` + `window.dmoFocus` mechanism; when the invoking control is gone the
     helper returns `false` and the consumer keeps control of its own recovery — the component
     never traps or forces focus;
   - no async change (search, retry, candidate arrival, create pending) steals focus.
5. **Selection**: the selected candidate exposes a programmatic selected fact **and** a visible
   non-colour marker.
6. **Announcements**: result count/state is announced politely; `lookup-failed`/`conflict` announce
   assertively through the accepted P2-T01 semantics without automatic focus theft.
7. **Disabled reasons**: every disabled supplied action renders disabled + `aria-disabled="true"` +
   `aria-describedby` resolving to the rendered reason element (accepted P2-T01 pattern).

### 5.2 `ToolSummaryRow`

1. Labelled facts with definition/labelled-cell semantics; no fact collapsed into an unlabelled
   string; supplied context available programmatically.
2. Only supplied actions enter the tab order, in supplied order; action accessible names include the
   supplied `AccessibleContext` when supplied (never derived from fact values).
3. A disabled action renders disabled + `aria-disabled="true"` + associated reason id.
4. Status renders as visible text with a supplementary tone token (never colour-only), not
   focusable.
5. The controlled `Selected` fact is programmatic and non-colour-only.
6. The row is not focusable unless it contains supplied actions.

### 5.3 `MeasurementRows`

1. Labelled region; each row carries its supplied `AccessibleContext`; repeated fields carry unique
   labels (consumer-supplied `Label`s, qualified by the row context in the accessible name).
2. Remove controls identify their row context in their accessible name.
3. Row-major logical order: row context → fields in supplied order → remove control; then the add
   control after the rows.
4. A field with `Editable == false` renders read-only/disabled with its supplied reason
   programmatically associated.
5. A disabled remove control renders disabled + `aria-disabled="true"` + `aria-describedby`
   resolving to the rendered reason (the minimum violation reason has precedence, §3.3.4).
6. Focus: after add → first editable control in the new row (fallback: add control); after remove →
   first editable control of the nearest surviving row (fallback: add control); structural changes
   never reset focus to page start and never steal focus onto an unrelated region.
7. Validation display is associated with its field/row and carries visible text plus a
   supplementary tone token (never colour-only); errors are summarised by the consumer (A provides
   the display hooks).
8. Structural changes (add/remove) are announced politely; supplied values are retained (never
   re-read or reset) across state changes.
9. No key handler swallows `Tab`; no focus trap.

### 5.4 `DecisionBar`

1. Labelled region (supplied or the A-owned generic label); visible help/status text when supplied.
2. Native activation semantics: every action is a native control; the tab order equals the visual
   order, which equals the supplied order.
3. Disabled actions render disabled + `aria-disabled="true"` + associated reason id.
4. Danger actions carry visible textual/semantic meaning in addition to styling.
5. Pending action: `aria-busy` + the supplied pending label as visible text; the accessible name is
   preserved (it is never replaced by the pending label alone).
6. Focus remains on the pending action while it is present; failure focus follows the consumer's own
   error-summary rules; a consumer-owned confirmation restores the invoking control's focus (the
   accepted `dmo-focus.js` mechanism).
7. No `role` invention beyond a labelled region/group.

### 5.5 Shared accessibility rules

No meaning is colour-only anywhere. Every async change preserves context and announces without
focus theft. Every disabled control exposes a visible, programmatically associated supplied reason.
Every non-`ready` surface is announced per the accepted P2-T01 semantics.

---

## 6. Fixed desktop rules

**This overrides any earlier responsive/tablet phrasing for P2-T03.** The controlling authority is
the master plan's binding **DMO FIXED DESKTOP LAYOUT POLICY** and A1 freeze **§1A**.

### 6.1 Binding rules

1. **Canonical viewport `1366 × 768`.** All four components are designed and validated at
   `1366 × 768` first, including the real 768 px vertical constraint (compact density is mandatory).
2. **Structural composition is fixed.** Each component keeps the structural region its consumer
   assigned. A control must not move to another structural region because viewport width changes.
3. **No breakpoint-driven semantic or structural variant.** P2-T03 introduces **no** `@media`,
   `@container`, `@supports` or equivalent width-conditional rule, and no JS width listener, for any
   of the four components.
4. **No card conversion, no required-control hiding, no action relocation.** Required controls
   (search, candidates, select controls, create when supplied, cancel/return, add/remove, action
   buttons, status, validation display) remain present and in place. No group, priority or width may
   move an action into an overflow menu, stack the action row vertically, or hide a field.
5. **Larger desktop resolutions** (1920 × 1080, 2560 × 1440) preserve the same composition and
   control locations; extra space becomes outer whitespace/margins or limited non-structural
   expansion of a content region only.
6. **Smaller available width/height preserves composition and scrolls.** Page-level or local
   overflow is always preferred to structural reflow (§6.5).
7. **Compact density is mandatory**: no oversized headers, decorative card inflation, repeated
   titles, marketing whitespace or unnecessary stacking.
8. **Mobile, tablet, touch-first and phone-specific layouts are out of scope.**

### 6.2 Superseded earlier phrasing (recorded explicitly)

The accepted Workstream A plan §9.3 (tablet stacking / mobile consultation layout) and its §10.3
tablet-width rendered checks are **superseded for P2-T03** by the binding fixed desktop policy.
`IMPLEMENTATION_MODEL.md`'s "responsive behavior" listing for Workstream A and
`ACCEPTANCE_MATRIX.md` §3's "responsive rendered checks" evidence item are likewise not applicable
to P2-T03 in their tablet/mobile reading.

P2-T03 therefore produces **no** tablet/mobile variants and adds **no** tablet-width rendered
checks, and requires no mobile/browser responsive behaviour. This is a narrowing authorized by the
later binding policy, not an omission.

### 6.3 Protected pre-existing shell breakpoints are out of P2-T03 authority

`wwwroot/css/dmo-shell.css` (protected) already contains a pre-existing
`@media (max-width: 62rem)` rule asserted by the existing `SharedShellTests`. P2-T03 **must not**
modify, remove or "fix" protected shell CSS to satisfy §6. The §6.1 prohibition applies to the
**new** P2-T03 selectors and behaviour only.

### 6.4 Structural composition and control-presence proof (binding)

Presence rules (all tested statically/rendered, §8.1 RT/ST rows):

| Component | Fixed structural regions (never width-dependent) | Required controls |
|---|---|---|
| `ToolPicker` | origin/expected-type context region → search row → candidate list → action row | search input (per §3.1.9); one select control per supplied candidate; create control iff §3.1.6; cancel/return control in every presented state |
| `ToolSummaryRow` | facts region (fixed slot order) → status slot → trailing actions region | one entry per supplied fact; status iff supplied; one control per supplied action |
| `MeasurementRows` | region label → row grid (row context / fields / trailing remove control) → add region | add control; one remove control per row; one control per supplied editable field |
| `DecisionBar` | one action region → optional status/help text → optional state surface | one control per supplied visible action, in supplied order |

No component may render a structure that varies by width, and no required control may be absent
because of width, group, priority or state other than as explicitly permitted by §3.1.6 and the
presence matrices in §3.1.9 / §3.3.6.

### 6.5 Reduced width: scrolling, not reflow

1. Each component's own content never uses `overflow: hidden` in a way that hides a required
   control, never sets a fixed pixel height that clips rows, and never locks page scrolling
   (`height: 100vh`, `position: fixed` overlays are not used for component layout).
2. Where a component's own content can exceed its assigned region, it uses a **keyboard-reachable
   local overflow container** (the accepted P2-T02 pattern: focusable, accessible name, visible
   focus style, no key handler that swallows `Tab`, no focus trap). At minimum:
   `MeasurementRows` wraps its row grid and `ToolPicker` wraps its candidate list.
3. `ToolSummaryRow` and `DecisionBar` are compact single-region components: they must remain on one
   logical line and let the **page** scroll rather than stacking.
4. Scrolling is always preferred to structural reflow; the same composition is preserved.

---

## 7. Explicit non-scope

P2-T03 must not contain, implement, or specify:

1. **Tool orchestration/domain**: any Tool search algorithm, matching, ranking, scoring, ordering,
   result merging, compatibility/suitability rule, canonical Tool identity, `tool_id` creation or
   return, CM/MF/BQ association, Job On context, Tool ficha, change-request or approval lifecycle.
2. **Persistence/backend**: any fetching, repository, `DbContext`, `HttpClient`, Supabase,
   migration, schema, endpoint, request/response DTO, database call, file-system access, cache,
   cookie or local storage. P2-T03 has **no** backend/persistence requirement (handoff §7).
3. **Auth/access**: any login, session, template, provisioning, role, permission enum, authorization
   decision, module gate, availability registration, `CurrentBuildAvailable` change or User
   provisioning. The current Supabase TEST baseline is not a P2-T03 blocker and is not touched.
4. **Routing/navigation**: any route, `@page`, `MapGet`/`MapPost`, URL construction, `href`/`action`/
   `method` emission, `location`/`window.open` use, form submission, automatic navigation, secondary
   navigation or current-destination marking. P2-T09 and P2-T10 own those.
5. **Measurement domain**: any measurement field name/catalog, sheet layout, formula, calculation,
   tolerance, nominal, capacity, weight, water temperature, density, Pegamentos/Folha/Resumo/
   Comparação/Peso semantics, `% utilização`, units, precision/rounding rule, or domain validation
   verdict. Consumers (C) own row contents entirely.
6. **Decision semantics**: any approve/reject/reopen/submit/close/movement/release/repair/warehouse
   transition, movement vocabulary (`Início`/`Saída`/`Entrada`/`Irreparável`), per-CM decision,
   close/reopen rule, confirmation policy, retry policy, transition ordering or automatic decision.
7. **Duplicated accepted primitives**: any second state/status/availability/action vocabulary, any
   `DenseDataTable`/`AuditTrail`/`ProductionContextStrip` concept or equivalent reimplementation
   (§1.6).
8. **P2-T04 and later leakage**: any canonical identity string; any Tool/Job On/Controlo/Boquilhas/
   Armazém/document/history concept; any `ProductionContextStrip` type or partial; any Ferramentas
   top-level destination concept; any HISTÓRICO GLOBAL concept.
9. **Protected foundation**: any modification listed in Appendix A; any redesign, replacement or
   modification of an accepted P2-T01/P2-T02 contract type, partial, existing CSS selector, asset or
   existing test method.
10. **Responsive variants**: any breakpoint-driven structural variant, card conversion,
    required-control hiding or action relocation (§6).

---

## 8. Test-to-acceptance matrix

Tests are **specified here, not written in this task**. Every acceptance criterion in §9 maps to at
least one executable or static proof below; every test below is **failing-if-removed**.

Naming/placement: unit tests under `tests/DMO.UnitTests/Frontend/Shared/`; rendered tests under
`tests/DMO.IntegrationTests/Frontend/Shared/` (real compiled partials through the existing test-host
view engine, reusing the accepted `SharedComponentRenderer`/`P2T02ComponentRenderer` approach);
P2-T03 adds **new** test classes and a **new** `P2T03RegressionTests` class and modifies **no**
existing test method, assertion, pinned hash or helper. Evidence must separate committed test source
inspected in Git from execution evidence (`dmo-beta-master/WORKFLOW.md` "Testing evidence";
`ACCEPTANCE_MATRIX.md` §11).

Proof classes: **U** = unit/executable model or contract assertion; **R** = rendered (real partial)
integration assertion; **S** = static/architecture scan (reflection + file content);
**G** = regression on protected foundation.

### 8.1 Test catalogue

#### `ToolPicker` — unit (U)

| # | Test | Proves |
|---|---|---|
| TP1 | construction/sync exposes the controlled query and the supplied candidates in supplied order; no event is raised | explicit search state + explicit candidate list (§3.1.5, §3.1.9) |
| TP2 | with exactly **one** supplied candidate, no transition (`Open`, `QueryChanged`, `CandidatesReceived`, `Sync`, `EnterFromSearch`, `RequestCreate`, `RequestRetry`) selects it | single candidate never auto-selects (AC-2) |
| TP3 | with **multiple** supplied candidates, no transition selects any of them; `SelectedCandidateKey` stays `null` | multiple candidates never auto-select (AC-2) |
| TP4 | the only transition that changes `SelectedCandidateKey` is `SelectCandidate(k)` for a supplied `k`; `SelectCandidate` for an unsupplied/blank key raises no event and reports `Blocked` | selection requires explicit user action (AC-4) |
| TP5 | `EnterFromSearch()` raises exactly one `SearchRequested` carrying the **controlled** query and **never** `CandidateSelected`; with exactly one candidate the selection is still `null` | Enter does not select (AC-3) |
| TP6 | `SelectCandidate(k)` raises exactly one `CandidateSelected(k)` with the opaque key; repeated activation raises one event per activation | explicit candidate action selects (AC-4) |
| TP7 | `Escape()` raises exactly one `CancelRequested` and nothing else: selection unchanged, query unchanged, no `ReturnRequested`, `FocusTarget = InvokingControl` | Escape requests cancel only (AC-8) |
| TP8 | `Cancel()` raises exactly one `CancelRequested` with the supplied action key (or the component-owned default key) and leaves `SelectedCandidateKey` unchanged | cancel returns without selection (AC-7) |
| TP9 | `Close()` raises exactly one `ReturnRequested`, changes no selection and no query, and targets `InvokingControl` | close path performs no discard (AC-9) |
| TP10 | the origin token round-trips **verbatim** on every event kind for `null`, `""`, whitespace-only, non-ASCII and a canonical-looking opaque string (`Ordinal` equality) | origin token preserved (AC-10) |
| TP11 | the create affordance rule: rendered iff supplied ∧ `Visible` ∧ `Enabled` ∧ `State ∈ {Ready, Empty}`; absent for a disabled/hidden/absent create action and for `LookupFailed`/`Unavailable`/`PermissionDenied`/`Loading`/`Stale`/`Conflict`/`Saving`/`Submitting` | Create Tool visibility controlled solely by consumer (AC-6) |
| TP12 | `Create` fails closed on: undefined state; blank region label; blank candidate key; duplicate candidate keys; blank candidate accessible context; blank fact label/value; a non-null `SelectedCandidateKey` not among the supplied candidates; missing `Message` when `State != Ready` | fail-closed carriers (AC-2, AC-4) |
| TP13 | no `ToolPicker` candidate type carries a status/`RecordStatusPresentation`-equivalent member, and no picker-owned status enum/tone map exists | candidate status absent (§1.8; accepted P2-T02 Q2 precedent) |
| TP14 | `ShowsSearch` is true for `Ready`/`Empty`/`LookupFailed`/`Stale`/`Conflict` and false for `Loading`/`Unavailable`/`PermissionDenied`; select controls are unavailable only in `Saving`/`Submitting` | presence matrix (§3.1.9) |
| TP15 | focus targets per transition: `Open` → search input (else region heading); `Cancel`/`Escape`/`Close` → invoking control; `QueryChanged`/`CandidatesReceived`/`EnterFromSearch`/`SelectCandidate`/`RequestCreate`/`RequestRetry` → unchanged | focus model (§4.1, §5.1) |
| TP16 | no `ToolPicker` type exposes a member returning a `Uri`/route/navigation target, and no member name or `const` value contains a canonical identity token | no URL construction; no identity leakage (AC-45) |
| TP17 | no `ToolPicker` type references a feature/domain namespace, service, `DbContext`, `HttpClient`, Supabase, session or clock | domain-neutral boundary (AC-46) |
| TP18 | no `ToolPicker` type exposes a member that computes matches, order, score, rank, count or a lookup key | no search algorithm/ranking invented (§7.1; AC-16) |

#### `ToolPicker` — rendered (R)

| # | Test | Proves |
|---|---|---|
| RTP1 | a single-candidate `Ready` render contains one candidate entry with one explicit select control and **no** `aria-selected="true"` anywhere | single candidate never auto-selects, structurally (AC-2) |
| RTP2 | N supplied candidates render N separate entries in supplied order, each with its labelled facts and its own select control; no merge/dedupe/rank marker exists | ambiguous candidates remain separate (AC-5) |
| RTP3 | the create control renders iff supplied visible+enabled; otherwise no create control and no create-disabled reason exists in the markup | Create Tool visibility (AC-6) |
| RTP4 | the cancel/return control renders in every presented state, including `unavailable` and `permission-denied`; in those states no candidate body and no create control render | cancel preserved; prohibited operations absent (AC-7, AC-14) |
| RTP5 | the search input carries the controlled query verbatim, has a label, and the picker markup emits no `action`/`method`/`href` and no `<form>` target | controlled query; no URL/form target (AC-1, AC-45) |
| RTP6 | the picker root carries the origin-token hook with the supplied value verbatim; the markup contains no `location.`/`window.open`/route construction | origin token preserved; no navigation (AC-10, AC-45) |
| RTP7 | non-`ready` surfaces render with the accepted P2-T01 state classes/tokens only: `empty` ≠ `lookup-failed`; `lookup-failed` renders its own token, an assertive announcement and the supplied retry and **no** create; `unavailable`/`permission-denied` render distinct surfaces with the reason associated | state delegation and distinctness (AC-13, AC-14) |
| RTP8 | the picker markup carries the documented focus-return hook and the picker asset installs no key handler that swallows `Tab` | focus restoration contract (AC-9, AC-49) |
| RTP9 | the selected candidate (controlled) renders `aria-selected="true"` **plus** a visible non-colour marker | programmatic, non-colour selection (AC-4) |

#### `ToolSummaryRow` — unit (U)

| # | Test | Proves |
|---|---|---|
| TR1 | the rendered fact set equals exactly the supplied fact set, in the fixed slot order then `AdditionalFacts` in supplied order; each label/value is preserved verbatim | supplied facts only, no lookup/inference (AC-17, AC-20) |
| TR2 | `Quantity` absent ⇒ no quantity entry and no `"0"` value anywhere in the row; `Quantity = "0"` ⇒ rendered as `0` | missing optional ≠ zero (AC-18) |
| TR3 | an absent boolean-ish slot ⇒ no `false`/`Não`/`—`/`0`/empty placeholder is produced; a consumer-supplied explicit value renders verbatim | missing optional ≠ false (AC-19) |
| TR4 | no `ToolSummaryRow` type exposes a member that looks up, resolves, derives, aggregates, formats, parses or defaults a fact, and no required fact/slot exists | no lookup/inference; no Tool fact catalog (AC-17, AC-20) |
| TR5 | `Status` reuses the accepted `RecordStatusPresentation` (single accepted type) and no `ToolSummaryRow`-owned status vocabulary/tone map/enum exists | status reuse (AC-21) |
| TR6 | `Actions` reuse `SharedActionPresentation`; a disabled supplied action without a reason is rejected; supplied order is preserved | action reuse and disabled-reason rule (AC-22, AC-37) |
| TR7 | `Create` fails closed on: undefined state; blank item key; blank fact label/value; blank action label; missing `Message` when `State != Ready` | fail-closed carriers (AC-17) |
| TR8 | no `ToolSummaryRow` type exposes selection/click/Space/Enter arbitration; `Selected` is a controlled presentation fact only | no selection arbitration invented (§1.8) |
| TR9 | no `ToolSummaryRow` type references a feature/domain namespace, service, session, clock, `DbContext`, `HttpClient` or Supabase, and no member name/`const` value contains a canonical identity token | domain-neutral; no identity leakage (AC-45, AC-46) |

#### `ToolSummaryRow` — rendered (R)

| # | Test | Proves |
|---|---|---|
| RTR1 | every supplied fact renders with its visible label (labelled/definition semantics); no fact is collapsed into one unlabelled string; absent slots produce no markup | supplied facts only (AC-17) |
| RTR2 | only supplied actions enter the tab order, in supplied order; a disabled action renders `disabled` + `aria-disabled="true"` + `aria-describedby` resolving to the rendered reason `id`; the accessible name includes the supplied context when supplied | actions and disabled reasons (AC-22) |
| RTR3 | absent optional slots render no placeholder cell/text (`—`, `0`, `false`, empty) | missing ≠ zero/false (AC-18, AC-19) |
| RTR4 | a supplied status renders through the accepted P2-T01 status markup (tone token + visible text); a row with no status renders no status markup | status reuse (AC-21) |
| RTR5 | the partial emits no `href`/`action`/`method`/`<form>`/`location.`/`window.open`, and no canonical identity string appears in the markup | no URL/identity (AC-45) |

#### `MeasurementRows` — unit (U)

| # | Test | Proves |
|---|---|---|
| MR1 | `MinimumRowCount = 1` with one tracked row ⇒ `Remove` is refused, raises no `RemoveRequested`, and exposes exactly the supplied `MinimumViolationReason` | minimum = 1 case (AC-27) |
| MR2 | with `MinimumRowCount = k`, `Remove` is refused while `RowCount <= k` and accepted while `RowCount > k`; after each accepted removal the invariant `RowCount >= k` holds | minimum enforcement (AC-26, AC-27) |
| MR3 | `Create` rejects `MinimumRowCount ≥ 1` without `MinimumViolationReason` and accepts `MinimumRowCount = 0` without one | disabled removal reason mandatory and visible (AC-28) |
| MR4 | after `ChangeValue` (and repeated `Sync` with the same rows) every tracked row key is `Ordinal`-identical to the supplied key; no re-keying/renumbering member exists | stable row id across edit (AC-25) |
| MR5 | removing one row leaves exactly the other supplied keys, unchanged and in supplied order | stable surviving ids after removal (AC-26) |
| MR6 | `Add` allocates a key that is not already tracked, appends it (`AllocatedIndex` = previous count), and keeps it stable across subsequent `Sync`s while the row survives; allocated keys carry no canonical identity shape | add produces a new stable frontend row; no canonical identity (AC-29, AC-45) |
| MR7 | after `Add`, the immediate target is `FirstEditableFieldInRow(allocatedKey)` and, after `Sync`, `ResolveFocusTarget()` returns the new row's first editable field key, or `AddControl` when the row has no editable field | focus after add (AC-30) |
| MR8 | after an accepted `Remove(k)`, the immediate target is the nearest surviving row (the row now at the removed index, else the preceding row) and, after `Sync`, `ResolveFocusTarget()` returns its first editable field key, or `AddControl` when none is editable | focus after removal (AC-31) |
| MR9 | every `Add`/`Remove` outcome exposes a focus target or an explicit `None` for a refused operation; no outcome yields a page-start/undefined target | structure changes never reset focus to page start (AC-31) |
| MR10 | zero rows is accepted only when `MinimumRowCount == 0`; `Create` rejects `Rows.Count < MinimumRowCount` | minimum-0 and fail-closed minimum (AC-26) |
| MR11 | with structural mutation disallowed, `Add` and `Remove` are refused with the supplied structural reason, no event is raised, and the tracked/supplied values are unchanged | saving/submitting/unavailable read-only retention (AC-32) |
| MR12 | `Create` fails closed on every rejection listed in §3.3.2 (blank/duplicate row and field keys, blank row context, blank label, options misuse, non-editable without reason, missing availability/structural/minimum reasons, missing `Message` when `State != Ready`, negative minimum) | fail-closed carriers (AC-27, AC-28) |
| MR13 | no `MeasurementRows` type exposes a member named or typed with a measurement/domain concept (`nominal`, `tolerance`, `capacidade`, `peso`, `pegamentos`, `costura`, `ovalização`, `densidade`, `formula`) and no member computes a validation verdict or a calculation | no measurement domain in A (AC-33) |
| MR14 | a deliberately out-of-domain supplied field value (e.g. an out-of-range number or arbitrary text) is accepted verbatim with no rejection, no normalisation and no computed verdict | no domain validation, no formula (AC-33, AC-34) |
| MR15 | arbitrary supplied field/row/option keys of any shape (including canonical-looking strings) are accepted, echoed verbatim and never interpreted | keys are opaque (§2.3; AC-45) |
| MR16 | no `MeasurementRows` type references a feature/domain namespace, service, session, clock, `DbContext`, `HttpClient` or Supabase | domain-neutral boundary (AC-46) |

#### `MeasurementRows` — rendered (R)

| # | Test | Proves |
|---|---|---|
| RMR1 | in `Ready`, the add control renders with its supplied availability; a disabled add control renders `aria-describedby` → the rendered supplied reason | add availability + disabled reason (AC-32) |
| RMR2 | each row renders its supplied accessible context, its supplied fields with unique labels, and a remove control whose accessible name includes the row context | row context and controls (AC-34, AC-49) |
| RMR3 | at the minimum, **every** remove control renders `disabled` + `aria-disabled="true"` + `aria-describedby` resolving to the rendered minimum-violation reason text; above the minimum it renders enabled | disabled removal reason visible (AC-28) |
| RMR4 | supplied field/row validation text renders visibly with its tone token (text present, never colour-only); no validation text appears that was not supplied | validation display only, no computation (AC-33) |
| RMR5 | `Saving`/`Submitting`/`Unavailable`/`PermissionDenied` render the supplied rows with all controls disabled/read-only **and** the accepted P2-T01 state surface with the supplied message/reason; supplied values are present verbatim | retained values; supplied read-only reason (AC-32) |
| RMR6 | the add/remove/field controls carry the documented hooks; structural announcements use a polite live region; no component handler swallows `Tab` | focus/announcement contract (AC-30, AC-31, AC-49) |
| RMR7 | the rendered rows region and its markup contain no measurement/domain token from the §8.1 ST2 list | no measurement domain in the rendering (AC-33) |
| RMR8 | the row grid renders inside the component-owned local overflow container hook and the P2-T03 CSS block contains an `overflow-x`/`overflow-y` rule and no breakpoint rule | reduced-width local scrolling, no reflow (AC-49) |

#### `DecisionBar` — unit (U)

| # | Test | Proves |
|---|---|---|
| DB1 | the model/presentation reports the action sequence exactly as supplied for every group interleaving (e.g. `[Primary A, Secondary B, Primary C]` stays `A, B, C`) | supplied order preserved (AC-35) |
| DB2 | each action reports its supplied group unchanged; an undefined group value, and a group default, do not exist; only `Primary`/`Secondary`/`Danger` are representable | group assignment preserved and mandatory (AC-36) |
| DB3 | a disabled action carries its supplied reason (inherited from `SharedActionPresentation`); `Create` rejects a disabled action with no reason and rejects a blank action key/label | disabled reason visible, never invented (AC-37) |
| DB4 | with `PendingActionKey == k`, the presentation exposes the pending label for `k`, the region/action busy fact, and the action's accessible name unchanged | pending state (AC-38) |
| DB5 | `Invoke(k)` while `PendingActionKey == k` raises no event and reports `DuplicateBlocked = true`, repeatedly; after `Reset(null)`/`Sync(null)` the same `k` invokes once more | duplicate invocation blocked; completion/reset (AC-39) |
| DB6 | `Invoke(other)` while `k` is pending raises `ActionInvoked(other)` and leaves the pending action/state unchanged | pending scope is per action (AC-39) |
| DB7 | `Invoke(k)` for an enabled, non-pending action raises exactly one event each time — no collapse window, timer or lock exists | availability exactly supplied; no invented suppression (AC-41) |
| DB8 | no `DecisionBar` member name, `const` value, group member or default label contains a forbidden transition/domain token (`approve`, `reject`, `reopen`, `submit`, `close`, `movement`, `release`, `repair`, `warehouse`, `armaz`, `irreparável`, `início`, `saída`, `entrada`), and no `DecisionBar` type exposes such a member (`CommonState.Submitting` is P2-T01 vocabulary and is not a `DecisionBar` member) | no domain semantics encoded (AC-40) |
| DB9 | the model raises the event for any enabled supplied action regardless of `State` (`Stale`, `Conflict`, `PermissionDenied` with supplied availability) | the component disables nothing on its own (AC-41) |
| DB10 | `Create` fails closed on: undefined state; duplicate action keys; blank action key/label; missing `Message` when `State != Ready`; a `PendingActionKey` not referencing a supplied action; an undefined group | fail-closed carriers (AC-35, AC-36) |
| DB11 | no `DecisionBar` type references a feature/domain namespace, service, session, clock, `DbContext`, `HttpClient` or Supabase; no member returns a `Uri`/route; no member name/`const` contains a canonical identity token | domain-neutral; no identity leakage (AC-45, AC-46, AC-42) |

#### `DecisionBar` — rendered (R)

| # | Test | Proves |
|---|---|---|
| RDB1 | rendered control order equals the supplied order exactly; each control carries its opaque action key, its group marker and its accessible name | supplied order + group preserved (AC-35, AC-36) |
| RDB2 | a `Danger` action renders a visible textual/semantic marker in addition to styling (non-colour-only) | danger text/semantics (AC-36, AC-51) |
| RDB3 | a disabled action renders `disabled` + `aria-disabled="true"` + `aria-describedby` resolving to the rendered supplied reason | disabled reason visible (AC-37) |
| RDB4 | the pending action renders its supplied pending label, `aria-busy="true"`, keeps its accessible name, and its pending text is visible (not colour-only) | pending state (AC-38) |
| RDB5 | the region is a labelled landmark (supplied or generic label) with one control per supplied visible action, in visual = supplied order, native activation semantics | accessibility/order (AC-42, AC-51) |
| RDB6 | the partial emits no `href`/`action`/`method`/`<form>`/`location.`/`window.open` and no canonical identity string | no URL/identity (AC-42, AC-45) |

#### Shared rendered (R)

| # | Test | Proves |
|---|---|---|
| RTS1 | all four components' non-`ready` surfaces render through the accepted P2-T01 state partial with its tokens, and `empty`/`lookup-failed`/`unavailable`/`permission-denied` are pairwise distinct renderings | P2-T01 composition and distinctness (AC-43) |
| RTS2 | each component's root renders its stable structural hook (`data-dmo-picker`, `data-dmo-summary-row`, `data-dmo-measurement-rows`, `data-dmo-decision-bar`) and every control required by §6.4 is present for its state | fixed composition / required controls present (AC-48) |
| RTS3 | an over-width component region renders inside a keyboard-reachable, labelled local overflow container with no focus trap; no component markup sets a structural width/variant attribute | scrolling instead of reflow (AC-49) |

#### Static / architecture (S)

| # | Test | Proves |
|---|---|---|
| ST1 | `dmo-components.css` resolves; the P2-T03 block contains no `@media`/`@container`/`@supports`, no `--dmo-*` declaration line and no hex literal; every referenced `var(--dmo-*)` is declared in `dmo-tokens.css`; every pre-existing P2-T01/P2-T02 selector is still present | additive token-only breakpoint-free CSS (AC-52, AC-48) |
| ST2 | `/js/dmo-tool-picker.js` and `/js/dmo-measurement-rows.js` resolve and contain no `fetch(`, `XMLHttpRequest`, `WebSocket`, `location.`, `href`, `window.open`, `submit(`, `matchMedia`, `ResizeObserver`, `addEventListener('resize'`; each contains its idempotency guard (`if (window.dmo…`) and none of the domain vocabulary `jobon`, `peso`, `boquilhas`, `controlo`, `ferramentas`, `armaz`, `histórico`, `tampões`, `aprovação`, `aprovar`, `rejeitar`, `reabrir`, `irreparável`, `pegamentos`, `nominal`, `tolerância`, `densidade`, `movement`, `repairer`, `warehouse`, `tool_id`, `toolId`, `ToolId` | generic, non-fetching, non-navigating, non-responsive, domain-neutral assets (AC-49, AC-50, AC-52); note: the bare token `tool` is deliberately excluded because the frozen component identity `ToolPicker` contains it (§1.7 constraint 2) |
| ST3 | reflection over every P2-T03 contract type: no member name, no `const`/`static readonly string` value and no enum member name matches or contains a canonical identity token; plus a file scan of the four new partials, the two new assets and every P2-T03 test fixture for the same tokens and for `tool_id`/`jobon_id`/`cm_id`/`mf_id`/`bq_id`/`peso_id`/`boquilhas_id`/`movement_id`/`repairer_id` | no canonical identity leakage (AC-45) |
| ST4 | no P2-T03 contract type or fixture declares a `using` of, or references, a feature namespace, service, `DbContext`, `HttpClient`, Supabase type, session, clock or endpoint; and `SharedShellTests.SharedFrontendSource_DoesNotImportFeatureServices` remains green | no feature-service import / no backend dependency (AC-46, AC-50) |
| ST5 | the four new partials contain no `@page`, `MapGet`, `MapPost`, `href=`, `action=`/`method=` attribute, `<form`, `location.` or `window.open`; no P2-T03 test scans/mutates a route registry | no route change (AC-47) |
| ST6 | no P2-T03 source declares or renders a `ProductionContextStrip`, Tool canonical identity, Job On context, or any Controlo/Peso/Pegamentos/Folha/Resumo/Comparação/Boquilhas/Armazém/document/history concept or partial | no P2-T04+ leakage (AC-50) |
| ST7 | the P2-T03 CSS block and both new assets contain no width-conditional rule or width listener, and the four partials contain no width/breakpoint-conditional structural branch | fixed desktop static proof (AC-48) |
| ST8 | each new partial renders the required control set from §6.4 with the documented hooks and no state-dependent relocation of a control between structural regions | required controls remain in place (AC-48) |
| ST9 | `_SharedComponentAssets.cshtml` still emits each of the three accepted asset tags exactly once and additionally emits each new asset tag exactly once; `_Layout`/`_PublicLayout` are untouched | additive asset delivery (AC-52) |

#### Regression (G)

| # | Test | Proves |
|---|---|---|
| RG1 | `ModuleRegistrations.CurrentBuildAvailable` is `[]` | honest availability unchanged (AC-47) |
| RG2 | no route/destination registration is added or changed: `EmptyDestinationRouteRegistry` is still the production registration, `DestinationRouteRegistrations` declares no member, no `@page` exists in the new partials | no route change (AC-47) |
| RG3 | the full pre-existing unit and integration suites pass; the accepted P2-T02 pinned P2-T01 artifact hashes and frozen test-method names still validate; no existing test method, assertion, helper or pinned hash was modified | no weakened suite; additive only (AC-44) |
| RG4 | no protected foundation file (Appendix A.1) is modified; the accepted P2-T01/P2-T02 contract types, partials, CSS selectors, assets and tests are unchanged | protected boundaries (AC-44, AC-51) |
| RG5 | `SharedShellTests.SharedFrontendSource_DoesNotImportFeatureServices` and the accepted P2-T02 regression class remain green, unchanged | regression R (AC-46, AC-44) |

### 8.2 Matrix: acceptance criterion → proof

| AC | Criterion (short) | Proof | Class |
|---|---|---|---|
| AC-1 | explicit controlled search state; no search performed | TP1, TP5, RTP5 | U, R |
| AC-2 | never auto-selects, including exactly one candidate | TP2, TP3, TP12, RTP1, RTP9 | U, R |
| AC-3 | `Enter` from search never selects | TP5, RTP5 | U, R |
| AC-4 | explicit candidate action selects; selection requires explicit action | TP4, TP6, TP12, RTP9 | U, R |
| AC-5 | ambiguous candidates remain separate | RTP2, TP1 | R, U |
| AC-6 | `Criar ferramenta` exists iff consumer supplies it enabled | TP11, RTP3 | U, R |
| AC-7 | cancel returns without selection | TP8, RTP4 | U, R |
| AC-8 | `Escape` requests cancel only; never discards origin work | TP7 | U |
| AC-9 | close/cancel/return restore invoking-control focus | TP9, TP15, RTP8 | U, R |
| AC-10 | origin token survives the roundtrip unchanged | TP10, RTP6 | U, R |
| AC-11 | origin token is opaque: never parsed/interpreted/as-URL | TP10, TP16, ST3 | U, S |
| AC-12 | `create requested` carries only supplied carrier data; no creation/association | TP11, TP16, ST6 | U, S |
| AC-13 | non-`ready` surfaces delegate to P2-T01 | RTP7, RTS1 | R |
| AC-14 | `empty`/`lookup-failed`/`unavailable`/`permission-denied` stay distinct; a failed lookup offers no create and no candidate body | TP11, TP14, RTP4, RTP7, RTS1 | U, R |
| AC-15 | disabled supplied actions expose their supplied reason | RTP7, TP12 | R, U |
| AC-16 | no search algorithm/ranking/compatibility/count in the picker | TP18, TP17 | U |
| AC-17 | `ToolSummaryRow` renders only supplied facts; no lookup/inference/derivation | TR1, TR4, TR7, RTR1 | U, R |
| AC-18 | missing optional ≠ zero | TR2, RTR3 | U, R |
| AC-19 | missing optional ≠ false | TR3, RTR3 | U, R |
| AC-20 | no Tool fact catalog / no required fact; slots are presentation only | TR1, TR4 | U |
| AC-21 | status reuses accepted `RecordStatus`; no status vocabulary of its own | TR5, RTR4 | U, R |
| AC-22 | actions reuse `SharedActionPresentation`; only supplied actions are interactive | TR6, RTR2 | U, R |
| AC-23 | no Tool/domain meaning: no compatibility/identity/selection policy/navigation/mutation | TR4, TR8, TR9 | U |
| AC-24 | item and action keys are opaque | TR7, TR9, ST3 | U, S |
| AC-25 | stable frontend row identity across edit/re-render | MR4 | U |
| AC-26 | configurable minimum; surviving ids stable after removal; minimum never breached | MR2, MR5, MR10 | U |
| AC-27 | removal blocked at the minimum; minimum = 1 case | MR1, MR2, MR12 | U |
| AC-28 | disabled removal exposes the visible supplied reason | MR3, RMR3 | U, R |
| AC-29 | add allocates a new stable frontend-only row key, appended | MR6 | U |
| AC-30 | focus moves to the first editable control of the new row | MR7, RMR6 | U, R |
| AC-31 | after removal focus moves to the nearest surviving control; never page start | MR8, MR9, RMR6 | U, R |
| AC-32 | structural mutation refused while saving/submitting/unavailable/permission-denied with the supplied reason; values retained | MR11, RMR1, RMR5 | U, R |
| AC-33 | no measurement field/formula/tolerance/nominal/capacity/weight/Pegamentos/domain validation | MR13, MR14, RMR4, RMR7 | U, R |
| AC-34 | consumers own row contents; supplied keys/labels/values round-trip verbatim | MR14, MR15, RMR2 | U, R |
| AC-35 | supplied action order preserved exactly | DB1, DB10, RDB1 | U, R |
| AC-36 | group assignment preserved, mandatory, non-colour-only danger | DB2, DB10, RDB1, RDB2 | U, R |
| AC-37 | disabled supplied action exposes its supplied reason; no invented reason | DB3, RDB3 | U, R |
| AC-38 | pending state visible, accessible name preserved, never colour-only | DB4, RDB4 | U, R |
| AC-39 | duplicate invocation blocked while pending; completion/reset releases it | DB5, DB6 | U |
| AC-40 | no transition semantics encoded (approve/reject/reopen/submit/close/movement/release/repair/warehouse) | DB8 | U |
| AC-41 | availability is exactly supplied; nothing else suppresses invocation | DB7, DB9 | U |
| AC-42 | labelled region, visual = supplied order, no URL/form target | RDB5, RDB6, DB11 | R, U |
| AC-43 | composition: P2-T01 primitives reused; no duplicate state/status/action vocabulary; no `DenseDataTable`/`AuditTrail`/`AvailabilityState` reimplementation | RTS1, TR5, TR6, DB3, TP13, ST6 | R, U, S |
| AC-44 | no accepted P2-T01/P2-T02 artifact or test modified | RG3, RG4, RG5 | G |
| AC-45 | no canonical identity string anywhere in P2-T03 contracts/partials/assets/fixtures | TP16, TR9, RTR5, MR15, DB11, ST3 | U, R, S |
| AC-46 | no feature service/namespace/backend dependency | TP17, TR9, MR16, DB11, ST4, RG5 | U, S, G |
| AC-47 | no backend/persistence/route/authorization/availability change; `CurrentBuildAvailable` = `[]` | ST5, RG1, RG2 | S, G |
| AC-48 | fixed desktop structure: no breakpoint rule, no card conversion, no rearrangement, no action relocation, required controls present | ST1, ST7, ST8, RTS2, RTP1, RTP3, RTP4, RMR1, RMR2, RMR3, RDB1 | S, R |
| AC-49 | reduced width handled by keyboard-reachable local scrolling/page scroll; no focus trap | ST2, ST7, RMR8, RTP8, RTS3, RMR6 | S, R |
| AC-50 | no P2-T04+ leakage (Tool identity, Job On, Controlo, Boquilhas, documents, `ProductionContextStrip`) | ST2, ST6, TP17 | S, U |
| AC-51 | no protected foundation file modified; only A-owned and governance paths change | RG4, RTS2, RDB5 | G, R |
| AC-52 | additive token-only CSS; both new assets generic, idempotent, fetch-free, navigation-free | ST1, ST2, ST9 | S |

### 8.3 Completeness

- **Every acceptance criterion AC-1…AC-52 maps to at least one executable (U/R) or static (S/G)
  proof**, as tabulated in §8.2. No criterion is supported by prose alone.
- **Every catalogued test is referenced by at least one acceptance criterion.** The coverage is
  exhaustive across the four component groups (`TP`/`RTP`, `TR`/`RTR`, `MR`/`RMR`, `DB`/`RDB`), the
  shared rendered group (`RTS`) and the static/regression groups (`ST`, `RG`); the coverage audit was
  performed in both directions (criterion → tests, test → criterion).
- **Fixed desktop proof is both static and rendered** (ST1/ST7/ST8 + RTS2/RTS3 + the presence
  matrices in §3.1.9/§3.3.6/§6.4), so the policy is objectively checkable rather than aspirational.
- **No mobile/tablet/browser-responsive proof is required** (§6.2, §8.4).

### 8.4 Not required by P2-T03

Browser/DOM-level verification of the two JS adapters (real keystroke→event wiring, computed focus
survival, real `Escape`/`Enter` delivery, real focus movement after add/remove) is **not
schedulable in P2-T03** with the current infrastructure: there is no JS test runner and no
Architect-authorized browser test package, and adding one is itself an Architect decision. The
accepted P2-T02 Q1 default is therefore extended to P2-T03:

```text
C# interaction-model unit tests (TP/MR/DB rows)
+ rendered markup/hook/attribute assertions (RT/RMR/RDB/RTS rows)
+ static-asset content assertions (ST rows)
= P2-T03 focus/interaction evidence
```

DOM-level verification is recorded as deferred (not omitted) and belongs to a separately authorized
browser package, consumer integration (A8) or P2-T10 (see §10 Q4).

---

## 9. Acceptance criteria

Objective and checkable. "Failing-if-removed test" means the referenced test in §8.

**`ToolPicker`**

- **AC-1** The picker renders an explicit, labelled search state carrying the supplied controlled
  query verbatim and an explicit candidate list in supplied order, and performs no search,
  filtering, ordering, scoring or counting of its own (TP1, TP5, RTP5).
- **AC-2** No supplied candidate is ever auto-selected — including when exactly one candidate is
  supplied — and no transition other than an explicit candidate-select activation changes the
  presented selection (TP2, TP3, TP12, RTP1, RTP9).
- **AC-3** `Enter` in the search input raises `search requested` with the controlled query and
  **never** selects a candidate, never selects the first/only candidate and never submits a form
  (TP5, RTP5).
- **AC-4** Candidate selection requires an explicit activation of that candidate's select control,
  which raises exactly one `candidate selected` carrying the opaque candidate key, and the selected
  candidate is presented programmatically **and** with a visible non-colour marker (TP4, TP6, TP12,
  RTP9).
- **AC-5** N supplied candidates render as N separate entries in supplied order, with no merging,
  deduplication, grouping, collapsing or ranking (RTP2, TP1).
- **AC-6** The `Criar ferramenta` affordance exists **if and only if** the consumer supplies it
  visible **and** enabled **and** the state is `ready`/`empty`; otherwise no create control and no
  create-related disabled reason exist anywhere in the rendering (TP11, RTP3).
- **AC-7** Cancel returns to the origin without selection: it raises `cancel requested` only, changes
  no selection, clears no query and mutates no origin state; the cancel/return affordance is present
  in every presented state including `unavailable` and `permission-denied` (TP8, RTP4).
- **AC-8** `Escape` requests cancel and nothing else: same event, same gate and same focus target as
  the cancel control, with no discard, no confirmation decision, no self-close and no query clear —
  so `Escape` can never silently discard consumer/origin work (TP7).
- **AC-9** Close, cancel and return restore focus to the invoking control through the accepted shared
  focus mechanism; the component traps no focus and forces none when the invoker is gone (TP9, TP15,
  RTP8).
- **AC-10** The opaque origin token survives the roundtrip unchanged: every raised event carries the
  supplied token verbatim (ordinal equality) for `null`, empty, whitespace, non-ASCII and
  canonical-looking values (TP10, RTP6).
- **AC-11** The origin token is opaque: it is never parsed, trimmed, normalized, interpreted, matched
  or embedded in a URL, route, endpoint, filename or path, and it is never declared a canonical
  identity (TP10, TP16, ST3).
- **AC-12** `create requested` carries only supplied carrier data (the origin token and the create
  action key) and the picker creates, associates or persists nothing (TP11, TP16, ST6).
- **AC-13** Every non-`ready` picker surface is rendered by the accepted P2-T01 state region; the
  picker defines no state token, heading, message or live-region attribute of its own (RTP7, RTS1).
- **AC-14** `empty`, `lookup-failed`, `unavailable` and `permission-denied` render as four distinct
  surfaces; `lookup-failed` never renders as `empty`, never offers create, and never renders a
  candidate body; `unavailable`/`permission-denied` render no candidate data and preserve
  cancel/return (TP11, TP14, RTP4, RTP7, RTS1).
- **AC-15** Every disabled supplied picker action exposes its supplied reason programmatically
  associated; the picker invents no reason text (RTP7, TP12).
- **AC-16** The picker contains no search algorithm, matching, ranking, scoring, ordering, counting,
  compatibility rule or lookup key (TP18, TP17).

**`ToolSummaryRow`**

- **AC-17** The row renders exactly the supplied facts in the fixed slot order (then
  `AdditionalFacts` in supplied order), with each supplied label and value verbatim, and performs no
  lookup, resolution, join, inference, derivation, aggregation, formatting, parsing or defaulting
  (TR1, TR4, TR7, RTR1).
- **AC-18** A missing optional fact is not rendered as zero: an absent `Quantity` produces no
  quantity entry and no `0`, while a supplied `"0"` renders as `0` (TR2, RTR3).
- **AC-19** A missing optional fact is not rendered as false: an absent boolean-ish slot produces no
  `false`, `Não`, `—`, `0` or empty placeholder, while a consumer-supplied explicit value renders
  verbatim (TR3, RTR3).
- **AC-20** The primitive defines no Tool fact catalog, no required fact and no mandatory slot; the
  slots are A-owned presentation positions with no domain meaning (TR1, TR4).
- **AC-21** Supplied status renders through the accepted P2-T01 `RecordStatus` component (visible
  text, supplementary tone, unknown status neutral, no lifecycle/action inference); no
  `ToolSummaryRow` status vocabulary, catalogue or tone map exists, and an absent status renders
  nothing (TR5, RTR4).
- **AC-22** Supplied actions render through the accepted P2-T01 action carrier in supplied order;
  only supplied actions are interactive; a disabled action without a supplied reason is rejected
  (TR6, RTR2).
- **AC-23** The row encodes no Tool or domain meaning: no compatibility, identity resolution,
  selection policy, navigation, mutation or authorization, and no selection arbitration of its own
  (TR4, TR8, TR9).
- **AC-24** The item key and every action key are opaque and never canonical identities (TR7, TR9,
  ST3).

**`MeasurementRows`**

- **AC-25** Row identity is a stable frontend-only identity: editing field values never changes a row
  key, and re-rendering/re-syncing preserves every surviving key exactly (MR4).
- **AC-26** The minimum row count is configurable (including `1`), is never breached by any accepted
  sequence of add/remove operations, and surviving row keys after a removal are exactly the other
  supplied keys in supplied order (MR2, MR5, MR10).
- **AC-27** Removal is disabled when it would violate the minimum — including the `MinimumRowCount =
  1` case — and a zero-row presentation is rejected when the minimum is at least one (MR1, MR2,
  MR12).
- **AC-28** The disabled removal exposes a visible, programmatically associated supplied reason; the
  reason is mandatory whenever `MinimumRowCount ≥ 1` and is never invented by the component; the
  minimum violation reason takes precedence over supplied-availability reasons (MR3, RMR3).
- **AC-29** `Add` produces a new stable frontend row: a newly allocated, non-colliding, frontend-only
  key appended after the last supplied row, reported with its index, stable while the row survives
  and never a canonical identity (MR6).
- **AC-30** After an add, focus moves to the first editable control in the new row, with the add
  control as the documented fallback when the row supplies no editable field (MR7, RMR6).
- **AC-31** After a removal, focus moves to the nearest surviving row's first editable control (the
  row now at the removed index, else the preceding row), with the add control as the documented
  fallback, and focus is never reset to page start (MR8, MR9, RMR6).
- **AC-32** While saving/submitting/unavailable/permission-denied, structural mutation (add/remove)
  is refused with the supplied associated reason, field controls render read-only/disabled with the
  supplied reason, and all supplied values are retained verbatim (MR11, RMR1, RMR5).
- **AC-33** The component defines and computes no measurement domain content: no field name/catalog,
  formula, tolerance, nominal, capacity, weight, Pegamentos semantics or domain validation verdict
  appears in, or is produced by, the component (MR13, MR14, RMR4, RMR7).
- **AC-34** Consumers own row contents: arbitrary supplied field keys, labels, options and values
  (including out-of-domain values) are accepted and rendered verbatim with no validation,
  normalisation or rejection by the primitive (MR14, MR15, RMR2).

**`DecisionBar`**

- **AC-35** The rendered action sequence equals the supplied sequence exactly, for every interleaving
  of groups (DB1, RDB1).
- **AC-36** Each action's supplied group is preserved, the group is mandatory, only
  `primary`/`secondary`/`danger` exist, and danger carries visible textual/semantic meaning in
  addition to styling (DB2, RDB1, RDB2).
- **AC-37** Every disabled supplied action exposes its supplied, programmatically associated reason;
  the component invents no reason and rejects a disabled action without one (DB3, RDB3).
- **AC-38** While an action is pending, it presents the supplied pending label and a busy fact as
  visible text, and retains its accessible name (DB4, RDB4).
- **AC-39** Duplicate invocation is prevented while the action is pending — no `action invoked` is
  raised for a pending action key — and completion/reset releases the block so the action can be
  invoked again (DB5, DB6).
- **AC-40** No `DecisionBar` member, label, group rule or state rule encodes approve, reject, reopen,
  submit, close, movement, release, repair or warehouse-transition semantics (DB8).
- **AC-41** Action availability is exactly the supplied availability: the component disables nothing
  because of the region state, and no suppression other than the frozen pending block exists (DB7,
  DB9).
- **AC-42** The bar is a labelled region with native-activation controls whose tab order equals the
  supplied visual order, and it emits no URL, `href`, `action`, `method`, form target or navigation
  (RDB5, RDB6, DB11).

**Shared / architecture / fixed desktop**

- **AC-43** P2-T03 composes with the accepted P2-T01/P2-T02 primitives instead of duplicating them:
  `CommonState`/`CommonStateRegionPresentation`/`_CommonStateRegion`, `RecordStatusPresentation`/
  `_RecordStatus`, `StatusTone`, `SharedActionPresentation`, the `dmo-focus.js` helper and the CSS
  token pattern are reused unchanged, and no second state/status/availability/action vocabulary,
  `DenseDataTable`, `AuditTrail`, `AvailabilityState` or `ProductionContextStrip` equivalent is
  created (RTS1, TR5, TR6, DB3, ST6).
- **AC-44** P2-T03 is additive only: no accepted P2-T01/P2-T02 contract type, partial, CSS selector,
  asset, test method, assertion, helper or pinned hash is modified, and every pre-existing test
  still passes (RG3, RG4, RG5).
- **AC-45** No canonical identity string appears in any P2-T03 contract type, partial, stylesheet,
  script or fixture; every opaque key stays opaque and is never named `tool_id`, `jobon_id`,
  `cm_id`, `mf_id`, `bq_id` or any other canonical identity (TP16, TR9, MR15, DB11, ST3).
- **AC-46** No P2-T03 source references a feature namespace, service, `DbContext`, `HttpClient`,
  Supabase, session, clock, endpoint or route; `SharedFrontendSource_DoesNotImportFeatureServices`
  remains green (TP17, TR9, MR16, DB11, ST4, RG5).
- **AC-47** P2-T03 changes no backend, persistence, schema, migration, route, authorization or
  availability behaviour: `ModuleRegistrations.CurrentBuildAvailable` remains `[]`, no
  `DestinationRoutes`/`DestinationRouteRegistrations` change, and no Supabase/Auth/provisioning
  behaviour is touched (ST5, RG1, RG2).
- **AC-48** At the canonical `1366 × 768` surface and at every larger desktop resolution the four
  components keep their consumer-assigned structural regions and the same composition: no
  breakpoint-driven semantic or structural variant, no table/card conversion, no required-control
  hiding, no action relocation and no stacking of the action region (ST1, ST7, ST8, RTS2).
- **AC-49** When the available window is smaller, composition is preserved and page-level or local
  keyboard-reachable scrolling is used instead of reflow; no component traps focus or swallows `Tab`
  (ST2, ST7, RMR8, RTP8, RTS3, RMR6).
- **AC-50** No P2-T04 or later concept leaks into P2-T03: no Tool canonical identity, Job On
  context, CM/MF/BQ association, Controlo/Peso/Pegamentos/Folha/Resumo/Comparação, Boquilhas,
  Armazém, documents/PDF, history, `ProductionContextStrip`, route or availability concept (ST2,
  ST6, TP17).
- **AC-51** No protected foundation file (Appendix A.1) is modified; the P2-T03 diff touches only
  A-owned paths and planning/governance paths (RG4, RTS2, RDB5).
- **AC-52** No new design token is introduced; the new CSS is scoped to `dmo-picker*`/`dmo-summary*`/
  `dmo-rows*`/`dmo-decision*` and uses only published tokens with no breakpoint/container/supports
  rule, no token declaration and no hex literal; the two new assets are generic, domain-neutral,
  idempotent, fetch-free and navigation-free (ST1, ST2, ST9).

---

## 10. Unresolved authority questions

Six questions were identified. Each states the exact missing authority, the implementation impact,
the smallest options and a pinned default. **None is BLOCKING** for the PLAN REVIEW gate: the
handoff records "Authority blocker: **none** for the presentation/mechanics", and A1 freeze §2
explicitly permits refining PROVISIONAL carrier shapes before implementation. Implementation
remains unauthorized until the Architect disposes Q1–Q6 and returns `PLAN ACCEPT`; if the Architect
rejects a pinned default, this contract returns to `CORRECTION REQUIRED` rather than proceeding on
the default.

### Q1 — `ToolPicker` candidate-list mechanism: picker-owned list vs delegation to `DenseDataTable` — NON-BLOCKING

- **Decision:** whether supplied candidates are rendered by a picker-owned candidate list (this
  contract's PIN) or delegated to the **accepted** `DenseDataTable` (which the task and freeze §15
  both make available).
- **Missing authority:** A1 freeze §7 defines the candidate carrier as "candidate list: opaque
  candidate key plus supplied display facts" and requires an "explicit candidate control", but never
  states whether that list is a table. `dmo-beta-master/contracts/SHARED_FRONTEND.md` lists
  `DenseDataTable` consumers as "Job On history, Controlo lists and Boquilhas History" — not the
  picker. P2-T02 §13.2 explicitly deferred "no picker candidate model" to P2-T03, implying P2-T03
  owns the candidate model but not which accepted primitive renders it.
- **Why insufficient:** delegation is technically possible (a `DenseDataTable` with
  `SelectionEnabled`/`OpenEnabled` and one supplied per-row select action) but it would import
  frozen table semantics that freeze §7 does not authorize for a picker: single-click/Space body
  selection, `Enter`-requests-open (which the picker must not have), a mandatory column model
  (`DenseTableColumnPresentation`) where freeze §7 supplies only "display facts", plus filter/
  paging/sort affordances with no picker authority. Body-click selection in particular sits
  awkwardly beside "candidate selection requires explicit user action".
- **Options (smallest first):** (a) picker-owned labelled candidate list with one component-owned
  explicit select control per candidate, fully reusing P2-T01 state/status/action primitives
  (**PIN**); (b) delegate the candidate region to `DenseDataTable` with `OpenEnabled = false`,
  `SelectionEnabled` per the picker's controlled selection and one supplied select action per row,
  accepting the extra column model and table selection semantics.
- **Implementation impact:** (a) = the carriers in §3.1.2–§3.1.4 and the transition model §4.1 as
  written. (b) = `ToolPickerPresentation` would carry `DenseTableColumnPresentation` columns plus a
  candidate→cell mapping, RTP1/RTP2/TP4/TP6 would be rewritten against table markup, and §3.1.7's
  "no body activation" rule would have to be reconciled with `DenseDataTable`'s frozen
  single-click selection.
- **Pinned default:** **(a)**. The picker reuses every accepted *state*, *status* and *action*
  primitive (§1.5) but does not adopt the accepted *table* semantics, because freeze §7's frozen
  picker rules (never auto-select, explicit candidate control, `Enter` never opens/selects) are
  narrower than `DenseDataTable`'s frozen selection/open arbitration.

### Q2 — `create requested` payload: origin token only vs a supplied provisional prefill carrier — NON-BLOCKING

- **Decision:** whether `create requested` must be able to round-trip a **separate** supplied
  provisional prefill carrier in addition to the opaque origin token.
- **Missing authority:** freeze §7 lists the output as "`create requested` with only supplied
  provisional prefill/origin carrier data" — permissive of carrying supplied carrier data, but
  freeze §7's carrier list names only one opaque carrier ("opaque consumer-controlled origin-state
  token") and never defines a prefill slot, its shape or its roundtrip rule.
- **Why insufficient:** the phrase authorizes *carrying supplied* data without stating whether that
  data is a second carrier or the origin token itself.
- **Options (smallest first):** (a) `create requested` carries the origin token and the create
  action key only; a consumer needing prefill round-trips it inside its own opaque origin token
  (**PIN**); (b) add one optional opaque `CreatePrefillToken` carrier echoed verbatim on
  `create requested`.
- **Implementation impact:** (a) = §3.1.4/§3.1.10 as written. (b) = one extra optional carrier
  member + one hook attribute + two test rows; no frozen behaviour changes.
- **Pinned default:** **(a)** — one opaque consumer-controlled carrier satisfies "preserve
  consumer-supplied origin state" without inventing a second prefill slot that no authority defines.

### Q3 — create affordance in `stale`/`conflict` — NON-BLOCKING

- **Decision:** whether the create affordance is offered only in `ready`/`empty` (PIN) or also in
  `stale`/`conflict` when the consumer supplies it enabled.
- **Missing authority:** freeze §7 states only "no-results may expose `Criar ferramenta` only when
  supplied visible/enabled"; it makes no statement about `stale`/`conflict` (which are "only when
  supplied by B"). The handoff and the master plan require only that create exist when the consumer
  supplies it enabled.
- **Why insufficient:** "may expose in no-results" is permission, not an exhaustive state list, and
  no authority says whether creating a Tool while candidates are stale or conflicting is
  presentationally acceptable.
- **Options (smallest first):** (a) `ready`/`empty` only (**PIN**) — a failed lookup, an unavailable
  context, a permission decision and a pending subflow never expose creation, and stale/conflicting
  candidate data is resolved first; (b) `ready`/`empty`/`stale`/`conflict`.
- **Implementation impact:** (a)/(b) differ by one condition in `ShowsCreate` plus one test row. No
  frozen behaviour, carrier or markup structure changes.
- **Pinned default:** **(a)** — the narrowest rule that satisfies "a failed lookup must never lead to
  creation" and keeps create out of every state whose data the consumer has flagged as untrustworthy
  or conflicting.

### Q4 — DOM/browser-level verification authority for the two new JS adapters and the focus mechanics — NON-BLOCKING

- **Decision:** whether `dmo-tool-picker.js`/`dmo-measurement-rows.js` DOM wiring (real keystroke →
  event delivery, computed focus movement after add/remove, real `Escape`/`Enter` handling, real
  focus restoration to the invoking control) must be verified in a real browser as part of P2-T03.
- **Missing authority:** the master plan §11 P2-T03 requires the interaction rules as **U** tests
  and the focus rules as **UI** tests; the A plan §10.3 requires an Architect-approved, centrally
  pinned browser test package and forbids creating a new test project without approval. No browser
  test package or JS test runner exists in the repository.
- **Why insufficient:** authority requires the *behaviours* and U-level evidence, but never states
  whether a browser/JS runner is authorized for A5/A6, and adding one is itself an Architect
  decision.
- **Options (smallest first):** (a) the accepted P2-T02 Q1 default extended to P2-T03: deterministic
  C# interaction models (unit-tested) + rendered markup/hook assertions + static-asset content
  assertions, with DOM-level verification deferred (**PIN**); (b) authorize a browser/JS test
  package for P2-T03 and add real-browser focus/keystroke tests.
- **Implementation impact:** (a) = §8's U/R/S test set and §8.4's deferred note. (b) = a new approved
  test package, new pinned versions in `Directory.Packages.props`, a new test project or fixture, and
  additional CI-less local execution evidence — an authorization the Architect must grant explicitly
  because it touches shared build files.
- **Pinned default:** **(a)** — identical to the Architect-accepted P2-T02 Q1 disposition, and the
  focus-target rules are expressed as deterministic model facts (§4.2.3) precisely so they are
  unit-testable without a browser.

### Q5 — `MeasurementRows` consumer row-content carrier: generic controlled field descriptors vs consumer-rendered markup — NON-BLOCKING

- **Decision:** whether `MeasurementRows` renders consumer row content through **generic controlled
  field descriptors** (this contract's PIN) or accepts **consumer-rendered row markup/template**, as
  freeze §13's carrier phrase "controlled ordered rows and consumer-rendered row fields/template"
  could be read.
- **Missing authority:** freeze §13 names a "consumer-rendered row fields/template" carrier but also
  classifies the shape as PROVISIONAL and states that A owns only "generic repeated-row editing
  mechanics"; it never states whether the shared primitive accepts consumer markup or exposes a
  generic control model. P2-T02's accepted Q3 resolved the analogous table-cell question in favour
  of plain supplied text, explicitly noting that consumer-rendered markup would be an A1 §16
  contract change — but that precedent does not settle an *editable* row, where plain text is not
  viable.
- **Why insufficient:** markup injection into a shared primitive conflicts with the accepted P2-T02
  Q3 direction and with the component's own focus requirement ("first editable control in the new
  row" must be determinable), while a generic descriptor model risks becoming a form framework.
- **Options (smallest first):** (a) generic controlled field descriptors with three generic kinds
  (`Text`/`Numeric`/`Choice`), consumer-supplied keys/labels/values/options/validation display, plus
  the deterministic focus target (**PIN**); (b) an `IHtmlContent`/template delegate per row with
  component-supplied structural chrome and a documented `data-dmo-*` contract the consumer must
  honour; (c) a documented A1 §16 contract-change proposal.
- **Implementation impact:** (a) = §3.3.2 and §4.2 as written, focus targets are deterministic model
  facts. (b) = `MeasurementRowsPresentation.Rows` becomes consumer markup (or a row-template
  delegate), the focus target becomes a DOM/hook contract (unverifiable without Q4(b)), field/option
  carriers and most MR/RMR rows are rewritten, and the "no markup injection" direction of the
  accepted P2-T02 Q3 is reversed for this component. (c) blocks P2-T03 pending the A1 §16 protocol.
- **Pinned default:** **(a)** — it satisfies "A owns generic repeated-row editing mechanics" and
  "consumers own row contents" simultaneously, keeps the shared primitive markup-free, and makes the
  frozen focus rules unit-testable. The pin is deliberately recorded as the strongest candidate for
  Architect correction.

### Q6 — `DecisionBar` group semantics: classification in one supplied sequence vs three fixed group slots — NON-BLOCKING

- **Decision:** whether the three groups are a **classification carried by each action** in one
  supplied sequence (PIN) or three **fixed structural slots** rendered in a fixed order with the
  supplied order preserved *within* each slot.
- **Missing authority:** freeze §14 requires "primary, secondary, and danger action groups" but never
  states whether the groups are regions or classifications; the handoff and the master plan §7
  P2-T03 both require that "action order follows supplied order", which is only literally satisfiable
  in the classification reading when supplied groups interleave.
- **Why insufficient:** the two readings diverge observably for a supplied interleaved sequence
  (e.g. `[primary A, secondary B, primary C]`), and neither authority text resolves the divergence.
- **Options (smallest first):** (a) one action sequence rendered in exactly the supplied order, with
  the group carried as a supplied classification exposed via a stable hook and emphasis styling
  (**PIN**); (b) three fixed group regions in a fixed slot order, supplied order preserved within
  each group.
- **Implementation impact:** (a) = §3.4.3 and §6.4 as written (order is exactly supplied).
  (b) = the rendered order becomes group-major, `RDB1`/`DB1` are rewritten, and the master plan's
  "action order follows supplied order" requires a within-group reading that must be recorded.
- **Pinned default:** **(a)** — it satisfies "supplied action order" and the binding "no action
  relocation" rule literally, and it keeps the shared primitive from inventing an ordering
  transformation the consumer did not supply.

---

## Appendix A — Protected boundaries

### A.1 Protected foundation (must not be touched by P2-T03)

Per master plan §12 and the accepted P2-T02 §13.1: canonical Module vocabulary/`ModuleCatalog`,
`ModuleRegistry`, `AccessResolver`/`AccessOutcome`/`ModuleResolve`, `ModuleAccessService`, the
server-side Module gate and ADMIN gate, authentication/session/current-account, account resolution,
persistence foundation and migrations 001/002, the Template model and administration, USER
administration, the Administration surface, root routing/landing/no-access,
`NavigationProjectionService`, `DestinationRoutes`/`DestinationRouteRegistrations`, the shared shell
(`ShellPresentationModels`, `ShellPresentationService`, `SharedFrontendExtensions`, `_Layout`,
`_PublicLayout`, `_Identity`, `Pages/Shared/Navigation/*`), shared tokens/shell CSS, the A1 frozen
contract document, `ModuleRegistrations`, the reviewed absence of provisional fixtures, and existing
tests.

Explicitly:

- **`_Layout` and `_PublicLayout` are not modified** (no `<link>`/`<script>` insertion into the
  protected shell). Asset delivery follows the existing per-page convention plus the additive helper
  (Appendix B.5).
- **`dmo-tokens.css`, `dmo-shell.css`, `dmo-user-shell.css` are not modified**, including the
  pre-existing shell `@media` rule (§6.3).
- **`SharedFrontendExtensions.cs` is not modified** unless a genuinely required additive registration
  is authorized; the P2-T03 design requires none.
- **No migration, no `Directory.*`/`.csproj`/`package` change, no new project, no route, no
  `CurrentBuildAvailable` change, no Supabase/Auth/config change, no provisional-fixture marker**
  (`A2Fixtures`, `ProvisionalFixturesWhenNeeded`, `PROVISIONAL FRONTEND CONTRACT` markers, fixture
  CSS).

### A.2 P2-T01 / P2-T02 boundary (accepted input, consume only)

P2-T03 **consumes** the accepted primitives and does not redesign, replace, rename, reclassify or
edit them: the 10 P2-T01 contract files, the 3 P2-T01 partials, every existing P2-T01/P2-T02 CSS
selector, the 18 P2-T02 contract types, the 3 P2-T02 partials, the 2 P2-T02 static assets, the
`P2T02RegressionTests` pinned artifact hashes and frozen test-method names, and every existing test
method or assertion. The **only** accepted artifact P2-T03 modifies is
`_SharedComponentAssets.cshtml`, and only additively (Appendix B.5). If a change to a P2-T01/P2-T02
contract really appears necessary, it is an A1 §16 contract-change proposal, not part of P2-T03.

### A.3 P2-T04+ boundary (must be preserved)

P2-T03 must not create, name, render, fixture or test any Tool canonical identity, Job On/`jobon_id`
occurrence, CM/MF/BQ context, Controlo/Peso/Comparação/Pegamentos/Folha/Resumo concept or record,
Boquilhas/movement/repairer concept, Armazém/Reparação/Tampões concept, document/PDF/availability
concept, `ProductionContextStrip` type or partial, secondary navigation, route registration, module
availability registration or HISTÓRICO (local or global) concept. Tool orchestration, canonical
identity return, search/create backends and persistence belong to P2-T04 (authority blocker B1) and
are explicitly outside P2-T03.

---

## Appendix B — Implementation file ownership / expected paths

All paths are A-owned (accepted Workstream A plan §5.1) or planning/governance paths. Nothing
outside this list may change.

### B.1 New presentation contracts — `src/DMO.Web/Frontend/Shared/Contracts/`

```text
ToolPickerFactPresentation.cs
ToolPickerCandidatePresentation.cs
ToolPickerEventKind.cs                    (enum: SearchRequested, CandidateSelected, CreateRequested,
                                                  CancelRequested, RetryRequested, ReturnRequested)
ToolPickerEvent.cs
ToolPickerFocusTarget.cs                  (enum: Unchanged, SearchInput, RegionHeading, InvokingControl)
ToolPickerOutcome.cs
ToolPickerInteraction.cs
ToolPickerPresentation.cs
ToolSummaryFactPresentation.cs
ToolSummaryRowPresentation.cs
ToolSummaryRowEventKind.cs                (enum: ActionInvoked)
ToolSummaryRowEvent.cs
MeasurementRowFieldKind.cs                (enum: Text, Numeric, Choice)
MeasurementRowFieldOptionPresentation.cs
MeasurementRowFieldPresentation.cs
MeasurementRowPresentation.cs
MeasurementRowsPresentation.cs
MeasurementRowsEventKind.cs              (enum: AddRequested, RemoveRequested, ValueChanged)
MeasurementRowsEvent.cs
MeasurementRowsFocusKind.cs               (enum: FirstEditableFieldInRow,
                                                  NearestSurvivingRowFirstEditableField,
                                                  AddControl, None)
MeasurementRowsFocusTarget.cs
MeasurementRowsOutcome.cs
MeasurementRowsInteraction.cs
DecisionBarActionGroup.cs                 (enum: Primary, Secondary, Danger)
DecisionBarActionPresentation.cs
DecisionBarEventKind.cs                   (enum: ActionInvoked)
DecisionBarEvent.cs
DecisionBarOutcome.cs
DecisionBarInteraction.cs
DecisionBarPresentation.cs
```

Conventions follow P2-T01/P2-T02: namespace `DMO.Web.Frontend.Shared.Contracts`; sealed records with
a private constructor + static `Create` (plus named factories where they remove ambiguity); mandatory
values rejected with `ArgumentException`/`ArgumentOutOfRangeException`; **no `using` directives**
beyond framework types; no service, `DbContext`, `HttpClient`, Supabase or feature dependency is
reachable from the contracts.

### B.2 New shared Razor partials — `src/DMO.Web/Pages/Shared/Components/`

```text
_ToolPicker.cshtml            (picker surface + P2-T01 state delegation + focus-return hook)
_ToolSummaryRow.cshtml        (compact supplied-fact row + P2-T01 status/state delegation)
_MeasurementRows.cshtml       (row mechanics surface + P2-T01 state delegation)
_DecisionBar.cshtml           (action region + P2-T01 state delegation)
```

### B.3 Additive stylesheet

```text
src/DMO.Web/wwwroot/css/dmo-components.css   (append the P2-T03 block after the P2-T02 block; no
                                              existing selector rewritten; no token declaration;
                                              no hex literal; no @media/@container/@supports)
```

### B.4 New generic static assets

```text
src/DMO.Web/wwwroot/js/dmo-tool-picker.js        (picker presentation only; thin adapter; no fetch/URL)
src/DMO.Web/wwwroot/js/dmo-measurement-rows.js   (generic row mechanics only; thin adapter; no domain rule)
```

Both are domain-neutral, idempotent if loaded twice, and must not read or write `location`, `href`,
`window.open`, forms, endpoints or storage. The C# interaction models are the normative statements
of the rules; the JS are thin DOM adapters over the same rules (§4.5).

### B.5 Asset delivery (additive)

`_SharedComponentAssets.cshtml` is extended **additively** with the two new `defer` script tags;
the three existing tags remain byte-identical so the accepted S5 assertion stays green. The partials
render markups and stable `data-dmo-*` hooks only; they emit no `<link>`/`<script>` and do not
modify the protected shell. Direct per-page tags remain permitted.

### B.6 Tests

```text
tests/DMO.UnitTests/Frontend/Shared/            (ToolPicker*Tests.cs, ToolSummaryRow*Tests.cs,
                                                 MeasurementRows*Tests.cs, DecisionBar*Tests.cs)
tests/DMO.IntegrationTests/Frontend/Shared/      (rendered tests, static-asset tests, architecture
                                                 scans, P2T03RegressionTests.cs)
```

Existing test methods, assertions, helpers and pinned hashes must remain unchanged (§8).

### B.7 Planning / governance (this contract-authoring task only)

```text
plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md   (this contract; new)
dev/responses/P2_T03_CONTRACT_AUTHORING_RESPONSE.md              (authoring response; new)
plans/BETA_IMPLEMENTATION_MASTER_PLAN.md                         (P2-T03 status line only)
plans/beta-workstreams/P2-T03-TOOLPICKER-ROWS-DECISIONBAR.md     (pointer to this contract only)
```

---

## Appendix C — Governance record

```text
P2-T03 STATUS = CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW
```

Recorded by this authoring task: this contract is committed and pushed to `DMO-MODULAR/main` and
verifiable at the SHA recorded below; the master plan P2-T03 entry and the P2-T03 handoff carry the
status pointer above; no application code, test, migration, route, CSS, JavaScript, Razor source,
configuration, Supabase integration or database artifact was created or modified.

Baseline and verification evidence for this authoring task:

```text
working tree before authoring : CLEAN
origin/main == local main     : verified by git fetch origin
build                          : dotnet build DMO.slnx → 0 warnings / 0 errors
unit suite                     : 413 passed / 0 failed / 0 skipped
integration suite              : 152 passed / 71 env-gated skipped / 0 failed
CurrentBuildAvailable          : []  (unchanged)
migrations added               : 0
application code modified      : NO
P2-T04 started                 : NO
```

---

## Appendix D — PLAN REVIEW gate

P2-T03 implementation is **not authorized** until all of the following hold:

1. this contract is committed and pushed to `DMO-MODULAR/main` and verifiable at the recorded SHA;
2. the Architect reviews **this contract** (not the handoff text) and returns **`PLAN ACCEPT`**
   (per `dmo-beta-master/WORKFLOW.md` steps 4–7);
3. the Architect's disposition of §10 Q1–Q6 is recorded in the review;
4. the implementation plan, recorded implementation SHA and implementation response then follow
   `WORKFLOW.md` steps 7–12 with their own Architect implementation review.

Until then:

```text
P2-T03 STATUS = CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW
```

No Razor page, component, partial, CSS, JavaScript, test, route, registration, Supabase change,
database change or application change may be made for P2-T03 before step 2. This document does not
self-accept, does not mark P2-T03 `IMPLEMENTED`, does not weaken any existing test, and does not
authorize P2-T04 or any later workstream.
