# P2-T02 — CONTRACT AUTHORING RESPONSE

**Workstream:** P2-T02 — `DenseDataTable` + `AuditTrail` (A4).
**Task class:** contract authoring only. No implementation.
**Status:** CONTRACT AUTHORED — AWAITING ARCHITECT PLAN ACCEPT.

---

## 1. Baseline

| Item | Value |
|---|---|
| DMO-MODULAR `main` at authoring (fetched, remote verified) | `bedd71c155498929d99060b676ab422104d14ed1` |
| dmo-MODULAR working tree at authoring | CLEAN before authoring |
| P2-T01 implementation SHA (accepted input) | `72c38c26fa03a81465de72bed07c57f087530618` |
| P2-T01 Architect review SHA | `d71899765e8cc3cc2850407b4dd9db581e31e513` |
| dmo-beta-master `main` | `78da49248f6cf7a8cbe4ddd946f3c38abbaf322f` |
| dmo-work `main` before this task | `d71899765e8cc3cc2850407b4dd9db581e31e513` |
| Contract artifact | `plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md` |
| Response artifact | `dev/responses/P2_T02_CONTRACT_AUTHORING_RESPONSE.md` |
| Implementation authority boundary | P2-T02 is **not** implemented; only planning/contract artifacts were written |

`git fetch origin` was run on DMO-MODULAR and the remote `main` SHA was verified before authoring.
The P2-T02 implementation diff boundary will be the eventual implementation commit against
`bedd71c155498929d99060b676ab422104d14ed1`; nothing was reviewed or changed in application code.

## 2. Authority Read

| Authority (read completely) | Used for |
|---|---|
| `plans/beta-workstreams/P2-T02-DENSE-TABLE-AUDIT-TRAIL.md` | handoff scope §4, non-scope §5, expected files §6, required tests §8, acceptance criteria §9, binding fixed desktop section |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §7 P2-T02 row, §11 P2-T02 test strategy, §12 Protected Work Register (rows 18, 19, 22, 23), §13 final integration sequence, Appendix A ledger 7.1/8.x, and the binding **DMO FIXED DESKTOP LAYOUT POLICY** |
| `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` | §1A fixed desktop contract, §2 classifications, §3 shared rules 1–9, §4 common state vocabulary, §9 `DenseDataTable`, §12 `AuditTrail`, §15 ownership, §16 contract-change protocol |
| `dmo-beta-master/WORKFLOW.md` | steps 4–12 gate chain; evidence classes; anti-invention and git/remote discipline |
| `dmo-beta-master/IMPLEMENTATION_MODEL.md` | Workstream A ownership boundary |
| `dmo-beta-master/ACCEPTANCE_MATRIX.md` | §2 global invariants, §3 Workstream A behaviour/evidence (no synthesized attribution), §11 test evidence gate |
| `dmo-beta-master/contracts/SHARED_FRONTEND.md` | `DenseDataTable`, `AuditTrail`, fundamental boundary, provisional-vs-final rules |
| `dmo-beta-master/AUTHORITY.md` | authority order (Beta canonical wins over historical workbench) |
| `workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md` (accepted) | §5.1 A-owned paths, §5.3 not-owned-by-A paths, §6.4 `DenseDataTable`, §6.7 `AuditTrail`, §9.3 responsive (superseded), §10.3 browser checks, §14 "A4" |
| `dmo-master` @ `dmo-modular` `global/ACCESS_MODEL.md` | UI presentation is never the security boundary |
| P2-T01 committed source (`72c38c2`) | exact reuse surface: `CommonState`, `CommonStateTraits`, `CommonStateRegionPresentation`, `StatusTone`, `RecordStatusPresentation`, `AvailabilityState`, `AvailabilityTraits`, `AvailabilityPresentation`, `AvailabilityVersionPresentation`, `SharedActionPresentation`, the 3 partials, the `dmo-components.css` token pattern |
| `reports/BETA_MASTER_RECONCILIATION.md` §7.1 | the PARTIAL this workstream removes |
| Beta module contracts (`modules/*.md`) | checked **only** for audit/history ordering authority — see §5 |

No Beta reconciliation was reopened and P2-T01 was not reopened. P2-T01's accepted primitives are
consumed as-is and are explicitly protected from modification by the contract (§13.3).

## 3. Contract Created

`plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md`, containing exactly the required
sections:

| Required section | Present |
|---|---|
| 1. Scope | yes |
| 2. Non-Scope | yes |
| 3. Dependencies | yes |
| 4. Fixed Desktop Layout Constraints | yes (incl. §4.2 supersession record and §4.3 protected-shell breakpoint carve-out) |
| 5. DenseDataTable Contract | yes (§5.1–§5.8: responsibility, columns, rows, status slot, controlled filter/paging/sort, selection/open mechanics, unit-testable interaction model, input summary) |
| 6. AuditTrail Contract | yes (§6.1–§6.6) |
| 7. Shared State Integration | yes (per-state tables for both components) |
| 8. Accessibility Contract | yes |
| 9. Styling / Token Contract | yes |
| 10. Test Contract | yes (unit, rendered, CSS/static-asset, regression) |
| 11. Acceptance Criteria | yes (AC-1…AC-31) |
| 12. Contract Questions | yes (Q1–Q3 + recorded authority silences) |
| 13. Protected Boundaries | yes (§13.1 foundation, §13.2 P2-T03, §13.3 P2-T01) |
| 14. Implementation File Ownership / Expected Paths | yes |
| 15. PLAN ACCEPT Gate | yes |

Repository path convention: `plans/` in DMO-MODULAR is the workstream planning/authority location
(`plans/BETA_IMPLEMENTATION_MASTER_PLAN.md`, `plans/beta-workstreams/*`). A `plans/contracts/`
directory did not exist; it was created because the requested preferred artifact path is
`plans/contracts/…` and no competing convention exists. This is stated in the contract.

## 4. Decisions Settled

| # | Decision | Authority basis |
|---|---|---|
| D1 | Both components consume a P2-T01 `CommonState`; no table/audit-specific state enum, token or message set is created. | freeze §4/§9/§12; handoff §4; master plan §7 P2-T02 |
| D2 | All non-`ready` surfaces delegate to the accepted P2-T01 `CommonStateRegionPresentation`/`_CommonStateRegion` semantics; the four mandated distinctions stay distinct. | freeze §3 rule 6, §4, §9, §12; P2-T01 as accepted |
| D3 | Column order is exactly the supplied order; `Priority` is presentation metadata only and never hides, reorders or converts a column. | freeze §1A ("Descriptor 'priority' is presentation metadata only…") |
| D4 | `DenseDataTable.empty` retains caption/header context plus the P2-T01 `empty` presentation; `lookup-failed` renders no data body and never the `empty` token/message. | freeze §9 States ("empty: headers/context plus explicit message…"; "lookup-failed: failure/retry, never empty") |
| D5 | `loading` marks the region busy and labels every supplied retained row stale with visible non-colour-only text. | freeze §9 States ("loading: busy table/skeleton; retained rows must be labelled stale") |
| D6 | Selection and open are separate: single click and `Space` select only; double click and `Enter` open only; an explicit open control provides pointer/keyboard parity. | freeze §9 FINAL behaviour; handoff §4.1 |
| D7 | Open is gated by a consumer-supplied `OpenEnabled`; no open control and no open request exists when it is false. | freeze §9 ("double click invokes consumer-supplied open action"); handoff §4.1 ("only where the consumer supplies") |
| D8 | Two consecutive open activations for the same row collapse to one invocation; a selection input or a different row reopens the window. | handoff §4.1; master plan §11 ("two clicks on one row invoke open once") |
| D9 | Selection is single, controlled and programmatic (`aria-selected`), with a visible non-colour indicator; `SelectedKey` must reference a supplied row. | freeze §9 ("one selected row…", "selection uses a stable opaque consumer-supplied key", "programmatic/non-color selected state") |
| D10 | The arbitration is implemented as a deterministic presentation-only C# model (`DenseTableInteraction`) so the frozen rules can be **unit**-tested, with the JS as a thin DOM adapter over the same rules. | master plan §11 P2-T02 requires the interaction rules as **U** tests; no JS test runner or authorized browser package exists (Q1) |
| D11 | Row actions are row-scoped only, reusing `SharedActionPresentation`; the frozen `action invoked` output carries a **row** key, so table-scoped actions belong to the consumer's `DecisionBar` region (P2-T03). | freeze §9 Outputs; freeze §15 (`DecisionBar` ownership); P2-T03 boundary |
| D12 | A component-owned trailing actions cell is always the last rendered column when actions exist; its position never depends on width/priority. | fixed desktop policy (predictable control placement; no action relocation) |
| D13 | Row `AccessibleContext` is mandatory; row-action accessible names include it. The component never derives context from cell display text. | freeze §9 accessibility ("row actions include row context"); freeze §3 rule 4 (display text is never identity); P2-T01 mandatory-label precedent |
| D14 | Filtering/paging/sorting are **controlled consumer-owned facts**: the component renders supplied affordances and emits generic events, performs no transformation and emits no `action`/`method`/`href`. | freeze §9 carrier + outputs; handoff §4.1; master plan §7 non-scope ("no fetching, backend filtering/sorting/paging") |
| D15 | An explicit rule that supplied filter/paging/sort state never changes the rendered row set/order (the objective proof of "no transformation"). | derived from D14; freeze §9 ("filters/pagination are consumer-owned facts") |
| D16 | `AuditTrail` renders entries in exactly the supplied order, asserts no chronological direction, and uses a plain list (no `<ol>` chronology claim). | freeze §12 ("preserve supplied ordering"; "ordering inference" is a non-responsibility) |
| D17 | No synthesized attribution: the type has no session/clock/service access; missing actor/timestamp is never substituted; supplied explicit "unavailable attribution" text is the only fallback. | freeze §12; handoff §4.2; `ACCEPTANCE_MATRIX` §3 |
| D18 | Timestamp display text is supplied verbatim; a supplied semantic value appears only as the `<time datetime>` machine value; no locale/format derivation. | freeze §12 carrier ("timestamp display/semantic value"); "no timestamp generation" non-scope |
| D19 | `AuditTrail` is read-only; the only optional per-entry interactive element is the supplied generic detail action; no edit/delete/restore. | freeze §12 (optional generic detail action; no correction workflow) |
| D20 | AuditTrail filtering/paging stays with the parent; the component carries only optional supplied `ContextText`. | freeze §12 ("optional parent-controlled filter/page context") |
| D21 | Asset delivery uses the existing per-page convention plus an optional A-owned `_SharedComponentAssets.cshtml`; the protected `_Layout` is not modified. | §12 #18 protected shell; existing repository convention (Administration pages declare their own links) |
| D22 | New CSS is appended to `dmo-components.css` using only existing tokens, scoped to `dmo-table*`/`dmo-audit*`, with no breakpoint/container rule. | §12 #19; fixed desktop policy §"Structural reflow is prohibited" |
| D23 | P2-T02 explicitly does not modify pre-existing protected shell `@media` rules, and this is carved out from the no-breakpoint rule. | §12 #19; existing `SharedShellTests` asserts the shell breakpoint |
| D24 | P2-T02 must not modify any accepted P2-T01 contract/partial/CSS selector or any existing test method. | task instruction ("P2-T01 is protected input"); master plan §12 #23 |
| D25 | Row cells are supplied plain text (Razor-encoded); the component never parses or interprets cell text. | freeze §2 (PROVISIONAL slot may be refined), §3 rules 2–4; P2-T01 plain-text precedent (recorded as Q3 for Architect confirmation) |

## 5. Contract Question Dispositions

**Count: 3.** Architect review accepted the Q1 and Q3 defaults and resolved Q2 to **ABSENT**.
The corrected contract remains awaiting Architect re-review.

1. **Q1 — DOM/browser-level verification authority for `dmo-dense-table.js`/`dmo-focus.js`.**
   Authority requires the interaction behaviours as U-level evidence but the repository has no JS
   test runner and no authorized browser test package (A plan §10.3 requires Architect approval to
   add one); A plan §14 A7's tablet clauses are superseded. *Default:* C# interaction-model unit
   tests + rendered markup/hook assertions + static-asset content assertions; defer DOM-level
   verification to a separately authorized browser package or to consumer integration / P2-T10.
2. **Q2 — per-entry `RecordStatus` on `AuditTrail` entries — RESOLVED.** AuditTrail entry status
   is **absent**. A1 freeze §12 defines the AuditTrail carrier and does not authorize an
   entry-level status slot; generic P2-T01 `RecordStatus` availability does not expand that
   carrier. No replacement field is introduced.
3. **Q3 — plain supplied text vs. consumer-rendered cell markup.** Freeze §9 names
   "consumer-rendered cell content" but classifies the slot PROVISIONAL (§2). *Default:* plain
   supplied text, Razor-encoded; richer rendering would be an A1 §16 change.

**Recorded authority silences (not questions, no behaviour invented):**

- AuditTrail newest-first vs oldest-first: authority is silent and delegates ordering to the
  supplier; P2-T02 asserts no direction and renders the supplied sequence. Per-module direction is
  later module authority (P2-T04/P2-T06/P2-T07/P2-T08).
- Per-column width carrier: authority provides none; P2-T02 defines deterministic content-driven
  widths with an advisory `WidthHint` and no pixel contract.
- Table-scoped (non-row) actions: the frozen output carries a row key, so only row-scoped actions
  are contracted.

## 6. Fixed Desktop Policy Compliance

- Canonical viewport **1366 × 768** is stated as binding in contract §4.1.1, with the 768 px
  vertical constraint called out as a density requirement.
- Contract §4.1.3 forbids any `@media`/`@container`/width-listener structural rule for the two
  components; §4.1.4 forbids table-to-card conversion, required-column hiding, column reordering
  and action relocation, and requires a keyboard-reachable local horizontal scroll container.
- AC-1, AC-2, AC-30 and tests U1, U2, S2, R13 make the policy objectively checkable rather than
  aspirational (column/row order invariance under priority/width/sort state; no breakpoint rule in
  the new CSS; keyboard-reachable scroll container).
- Larger resolutions preserve composition (§4.1.7); smaller windows scroll rather than reflow
  (§4.1.8); mobile/tablet out of scope (§4.1.10).
- **Supersession recorded** (§4.2): A plan §9.3 (tablet stacking / mobile) and its §10.3
  tablet-width checks, plus `IMPLEMENTATION_MODEL.md`'s "responsive behavior" listing and
  `ACCEPTANCE_MATRIX.md` §3's "responsive rendered checks" evidence item, are not applicable to
  P2-T02. The superseding authority is the binding policy recorded by `bedd71c…`.
- **Protected shell carve-out recorded** (§4.3): the pre-existing shell `@media` rule asserted by
  `SharedShellTests` is out of P2-T02 authority and must not be removed or "fixed".
- No oversized headers, decorative cards, marketing whitespace, repeated titles or unnecessary
  stacking are permitted in either component (§4.1.9, §9).

## 7. P2-T01 Reuse

P2-T02 introduces **no** replacement state, status or action vocabulary:

| P2-T01 primitive | How P2-T02 consumes it |
|---|---|
| `CommonState` | the sole state vocabulary for both components' region state |
| `CommonStateRegionPresentation` + `_CommonStateRegion.cshtml` | the sole rendering for every non-`ready` surface of both components (empty/lookup-failed/unavailable/permission-denied/loading/stale/conflict), preserving the four mandated distinct surfaces |
| `CommonStateTraits` | busy/assertive/reason semantics arrive through the accepted traits; P2-T02 restates none |
| `RecordStatusPresentation` + `_RecordStatus.cshtml` | the sole DenseDataTable row-status rendering (tone supplementary, text always visible, no lifecycle/action inference); AuditTrail has no entry-status slot |
| `SharedActionPresentation` | the sole generic action carrier for row actions, state/retry/refresh/recovery actions and the audit detail action — including the accepted mandatory disabled-reason rule |
| `StatusTone` | the sole tone vocabulary for status |
| `dmo-components.css` token-usage pattern | extended additively with the same `var(--dmo-*)`-only discipline; no new token |
| `SharedComponentRenderer` test approach | reused for rendered tests; existing methods unchanged |
| `AvailabilityState` / `AvailabilityPresentation` | **not** used by P2-T02 (document availability is a different surface); explicitly not duplicated either |

Contract §13.3 makes the P2-T01 boundary enforceable: no P2-T01 contract file, partial, CSS
selector or existing test method may change. AC-28 and test G4 verify it.

## 8. P2-T03 Boundary Protection

- Contract §2 and §13.2 forbid any `ToolPicker`, `ToolSummaryRow`, `MeasurementRows`, `DecisionBar`
  or `ProductionContextStrip` carrier, behaviour, partial or vocabulary in P2-T02.
- The only generic seam P2-T02 defines is the one it needs itself: opaque row/column/action keys,
  the accepted `SharedActionPresentation`, and the row-scoped selection/open arbitration. No
  candidate model, search/create/association state, measurement row schema, minimum-row rule,
  action-group (primary/secondary/danger) model, pending-deduplication or production-context slot
  is defined.
- Table-scoped/global actions are explicitly assigned to the consumer's `DecisionBar` region
  (P2-T03) rather than absorbed into `DenseDataTable` (D11).
- The contract forbids turning P2-T02 into a filter/pagination/query framework (§2, §5.5, AC-16):
  no operators, expressions, algorithms, arithmetic or counting.

## 9. Files Changed

Planning/contract artifacts only. **No `src/`, `tests/`, migrations, runtime configuration,
application CSS, Razor source or backend code was modified.**

| Path | Change |
|---|---|
| `plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md` | new (contract) |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | P2-T02 status line only: `CONTRACT AUTHORED — AWAITING ARCHITECT PLAN ACCEPT` + contract pointer. Not `IMPLEMENTED`, not `PLAN ACCEPT`. |
| `plans/beta-workstreams/P2-T02-DENSE-TABLE-AUDIT-TRAIL.md` | one pointer line to the authored contract |
| `dev/responses/P2_T02_CONTRACT_AUTHORING_RESPONSE.md` | new (this response) |

Verified absent from the diff: `src/**`, `tests/**`, `**/Migrations/**`, `**/*.csproj`,
`Directory.*`, `wwwroot/**`, `Program.cs`, `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md`.

## 10. Next Gate

```text
ARCHITECT PLAN ACCEPT REQUIRED BEFORE P2-T02 IMPLEMENTATION
```

The Architect must review the committed contract
(`plans/contracts/P2_T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md`) at its pushed SHA and return
`PLAN ACCEPT` (or `CORRECTION REQUIRED`/`REJECT`), recording a disposition for Q1–Q3, per
`dmo-beta-master/WORKFLOW.md` steps 4–7.

Until `PLAN ACCEPT`:

- P2-T02 implementation is **not** authorized and has **not** started;
- no `DenseDataTable`/`AuditTrail` contract, partial, CSS, JS or test exists;
- P2-T03 has **not** started;
- this response does **not** self-accept the contract.
