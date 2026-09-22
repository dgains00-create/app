# P2-T02 — `DenseDataTable` + `AuditTrail` — IMPLEMENTATION CONTRACT

**Status:** CONTRACT AUTHORED — AWAITING ARCHITECT PLAN ACCEPT.

This is a **contract specification only**. It creates no Razor page, component, partial, CSS,
JavaScript, test, route, backend behaviour, persistence, schema, migration, canonical identity
or API endpoint. P2-T02 implementation is **not** authorized by this document; it becomes
authorized only after an Architect `PLAN ACCEPT` on this contract (see §15).

Classification vocabulary used below is the A1 vocabulary (`docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` §2):

- **FINAL PRESENTATION CONTRACT** — frozen consumer-visible behaviour; change requires §16 of the A1 freeze.
- **PROVISIONAL FRONTEND CONTRACT** — frontend carrier shape only; no backend/persistence/schema/canonical-ID authority.
- **P2-T02 PIN** — a PROVISIONAL slot pinned to a concrete carrier by this contract for implementation.

Baseline this contract was authored against:

```text
DMO-MODULAR main             bedd71c155498929d99060b676ab422104d14ed1
P2-T01 implementation SHA    72c38c26fa03a81465de72bed07c57f087530618
P2-T01 Architect review SHA  d71899765e8cc3cc2850407b4dd9db581e31e513
```

---

## 1. Scope

Implement exactly **two** shared presentation primitives (Workstream A, sub-step **A4**):

**A. `DenseDataTable`** — a reusable dense operational table presentation primitive: stable
columns, stable rows, controlled selection, consumer-gated open, supplied row actions, supplied
status, and the accepted P2-T01 state surfaces for loading/empty/lookup-failed/unavailable/
permission-denied/stale/conflict.

**B. `AuditTrail`** — a shared presentation primitive that renders supplied historical/audit
event facts (opaque entry key, actor display fact, timestamp display + semantic value, action
text, optional before/after detail, optional generic detail action) in the supplied order, with
the accepted P2-T01 state surfaces.

Both are **presentation and generic interaction mechanics only**, owned by Workstream A, consumed
by B–E. Both are **domain-neutral**: they render supplied facts and raise generic presentation
events; they own no module semantics.

Deliverables of P2-T02 (implementation stage, after PLAN ACCEPT):

1. new presentation contracts under `src/DMO.Web/Frontend/Shared/Contracts/` (§14.1);
2. new shared Razor partials under `src/DMO.Web/Pages/Shared/Components/` (§14.2);
3. **additive** entries in `src/DMO.Web/wwwroot/css/dmo-components.css` (§9);
4. two new generic static assets `wwwroot/js/dmo-focus.js`, `wwwroot/js/dmo-dense-table.js` (§14.4);
5. new unit + rendered integration tests (§10).

## 2. Non-Scope

P2-T02 must not contain, implement, or specify:

- any ToolPicker, ToolSummaryRow, MeasurementRows, DecisionBar or ProductionContextStrip concept
  (P2-T03 and later). No picker candidate model, no measurement row schema, no action-bar groups,
  no production-context strip;
- any fetching, backend filtering/sorting/paging, query engine, filter DSL, search system,
  data-grid state machine, pagination framework or generic repository/API/repository/DbContext/
  migration/endpoint;
- any domain semantics: no Tool, Job On, Peso, BQ/Boquilhas, repair, approval, warehouse,
  production, Controlo, Ferramentas, Armazém, Tampões or História meaning; no column set, status
  vocabulary, lifecycle, formula, tolerance, nominal value or business rule;
- any authorization decision, route registration, navigation registration, `CurrentBuildAvailable`
  change, availability registration or protected-foundation change (§13);
- any URL construction, `href`/`action`/`method` emission, `location`/`window.open` use, form
  submission or automatic navigation;
- audit creation, actor resolution, timestamp generation, clock access, culture/locale date
  formatting, diff calculation, ordering inference, correction workflow or persistence;
- any mutation control on history (no edit/delete/restore) and no domain mutation triggered by
  selection;
- any breakpoint-driven structural variant (§4);
- redesign, replacement or modification of any accepted P2-T01 contract type, partial, existing
  CSS selector or existing test method (§13.3).

## 3. Dependencies

| Dependency | State | Use in P2-T02 |
|---|---|---|
| P2-T01 — `CommonState`, `CommonStateRegionPresentation`, `CommonStateTraits` | CLOSED / ACCEPTED (`72c38c2`, review `d718997`) | consumed unchanged; sole state vocabulary |
| P2-T01 — `RecordStatusPresentation`, `StatusTone` | CLOSED / ACCEPTED | consumed unchanged; sole status presentation |
| P2-T01 — `SharedActionPresentation` | CLOSED / ACCEPTED | consumed unchanged; sole generic action carrier (already enforces the disabled-reason rule) |
| P2-T01 — `_CommonStateRegion.cshtml`, `_RecordStatus.cshtml` partials | CLOSED / ACCEPTED | consumed unchanged for all non-ready surfaces |
| P2-T01 — `dmo-components.css` token-usage pattern | CLOSED / ACCEPTED | extended additively (§9) |
| Layout / shell (`_Layout`, `SharedFrontendExtensions` behaviour) | PROTECTED (§12 #18) | not modified; asset delivery follows the existing per-page convention (§14.5) |
| Fixed desktop layout policy | BINDING (`bedd71c`) | controlling constraint (§4) |
| P2-T03 | NOT STARTED | boundary protected (§13.2) |

P2-T02 introduces no new project reference, package or `Directory.Packages.props` change.

## 4. Fixed Desktop Layout Constraints

**This overrides any earlier responsive phrasing for P2-T02.** The controlling authority is the
master plan's binding **DMO FIXED DESKTOP LAYOUT POLICY** and A1 freeze **§1A**, both recorded by
commit `bedd71c155498929d99060b676ab422104d14ed1`.

### 4.1 Binding rules

1. **Canonical viewport `1366 × 768`.** Both components are designed and validated at
   `1366 × 768` first, including the real 768 px vertical constraint.
2. **Structural composition is fixed.** Navigation, actions, tables, filters, side panels, status
   information, history access and document actions keep their structural location. A control must
   not move to another structural region because viewport width changes.
3. **No breakpoint-driven semantic or structural variant.** P2-T02 introduces **no** `@media`,
   `@container`, or equivalent width-conditional rule, and no JS width listener, for
   `DenseDataTable` or `AuditTrail`.
4. **`DenseDataTable` specifically:** all supplied columns and their order remain stable at every
   desktop width; no table-to-card conversion; **no required-column hiding, no column reordering
   and no action relocation**; an over-wide table uses a **keyboard-reachable local horizontal
   scroll container**; row density stays compact.
5. **Column `Priority` is presentation metadata only** and is **never** authority to hide,
   reorder or convert a column (§5.2).
6. **`AuditTrail` specifically:** same fixed composition — compact entries, stable structural
   region, no cards, no stacked variant, no oversized headers or decorative vertical whitespace.
7. **Larger desktop resolutions** (1920 × 1080, 2560 × 1440) preserve the same composition and
   control locations; extra space becomes outer whitespace/margins, never a different structure.
8. **Smaller available width/height** preserves composition and uses page-level or local
   horizontal/vertical scrolling. **Scrolling is always preferred to structural reflow.**
9. **Compact density is mandatory**, not decorative: no oversized headers, no decorative card
   inflation, no repeated titles, no marketing-style whitespace, no unnecessary stacking.
10. Mobile, tablet, touch-first and phone-specific layouts are **out of scope**.

### 4.2 Superseded earlier phrasing (recorded explicitly)

The accepted Workstream A plan (`workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md`) §9.3
(tablet stacking, mobile consultation layout) and the tablet-width rendered checks in its §10.3
are **superseded for P2-T02** by the binding fixed desktop policy. `IMPLEMENTATION_MODEL.md`'s
"responsive behavior" listing for Workstream A and `ACCEPTANCE_MATRIX.md` §3's "responsive rendered
checks" evidence item are likewise not applicable to P2-T02 in their tablet/mobile reading.

P2-T02 therefore does **not** produce tablet/mobile variants and does **not** add tablet-width
rendered checks. This is a narrowing authorized by the later binding policy, not an omission.

### 4.3 Protected pre-existing shell breakpoints are out of P2-T02 authority

`wwwroot/css/dmo-shell.css` (protected, §12 #19) already contains a pre-existing
`@media (max-width: 62rem)` rule asserted by the existing `SharedShellTests`. P2-T02 **must not
modify, remove or "fix" protected shell CSS** to satisfy §4. The §4.1 prohibition applies to the
**new** P2-T02 selectors and to P2-T02 behaviour only.

## 5. `DenseDataTable` Contract

Owner: Workstream A (presentation, selection/open mechanics, keyboard/focus, accessibility).
Consumers: B, C, D, E. Data, query state, target and actions: consumer-owned.

### 5.1 Responsibility

`DenseDataTable` is responsible for:

- rendering a supplied, ordered column set and a supplied, ordered row set as a dense, semantic
  table;
- arbitrating selection versus open as two separate behaviours (§5.6);
- rendering supplied row actions, their disabled reasons and the row status slot;
- rendering the accepted P2-T01 state surfaces for every non-`ready` state, delegating to the
  accepted P2-T01 components rather than inventing table-specific states (§7);
- rendering supplied controlled filter/paging/sort affordances without applying them (§5.5);
- keeping its structure stable on the fixed desktop surface (§4).

It is **not** responsible for: fetching, filtering, sorting, paging, querying, counting,
resolving identities, constructing URLs, navigating, mutating, authorizing, or interpreting cell
text. Selection and open produce presentation events only; the consumer decides what they mean.

### 5.2 Column contract (P2-T02 PIN)

`DenseTableColumnPresentation` — sealed record, private constructor, static `Create`, in
`DMO.Web.Frontend.Shared.Contracts`.

| Member | Type | Rule |
|---|---|---|
| `Key` | `string` | **mandatory, non-blank**, **opaque** consumer key. Never a canonical identity, never an endpoint, never a URL. Used for header association, sort hooks and event carriers only. |
| `Heading` | `string` | **mandatory, non-blank** visible header text. |
| `AccessibleHeading` | `string?` | optional; when blank/absent the `Heading` is used. Never merged into identity. |
| `Alignment` | `DenseTableColumnAlignment` | `Start` (default) \| `Center` \| `End`. Supplied display alignment only. |
| `Priority` | `int?` | optional **presentation metadata only**. Rendered as `data-dmo-column-priority` for consumers. **Never** authority to hide, reorder, collapse or convert the column at any width (§4.1.5). |
| `Sortable` | `bool` | default `false`. When `true` the component renders a sort affordance for this column and may raise `sort requested`. It never sorts. |
| `WidthHint` | `DenseTableColumnWidthHint` | `Auto` (default) \| `Compact` \| `Wide`. Advisory, token-driven min-width only; **never** authority to hide/reorder. |

Rules:

- **Stable order.** The rendered column order is exactly the supplied list order, at every
  desktop width, for every combination of `Priority`/`WidthHint`.
- **All supplied columns render.** There is no required/optional column-hiding behaviour in
  P2-T02. Authority's "required column" language (freeze §1A) exists to forbid hiding; because
  P2-T02 hides no column at any width, no `IsRequired` carrier and no hidden-column behaviour is
  introduced.
- **No truncation hiding data.** Cell content is not truncatable by default: no `text-overflow:
  ellipsis`, no clamping, no `visibility:hidden` on required data. Long values wrap within the
  cell; the table scrolls locally instead of hiding or truncating (§4.1.4, §9.3).
- **Width behaviour.** Column widths are deterministic and content-driven, with
  `WidthHint` supplying only an advisory token-driven minimum. Total width may exceed the region;
  the table then uses its local horizontal scroll container. No pixel/percentage widths are
  invented, and no per-column pixel width carrier is part of P2-T02.
- **Header treatment.** Headers are a semantic header row (`<thead>` + `<th scope="col">`);
  header text is visible text; a `Sortable` header carries the sort affordance and its current
  direction is exposed programmatically (`aria-sort`) **only** from supplied sort state (§5.5).
- **Text alignment** is exactly the supplied `Alignment`; the component invents none.

### 5.3 Row contract (P2-T02 PIN)

`DenseTableRowPresentation` — sealed record with static `Create`.

| Member | Type | Rule |
|---|---|---|
| `Key` | `string` | **mandatory, non-blank**, **opaque** stable row identity across re-render. Never a canonical identity; never used to build a URL. |
| `AccessibleContext` | `string` | **mandatory, non-blank**. Supplied human context used to disambiguate the row for assistive technology and to qualify row-action accessible names (freeze §9: "row actions include row context"). The component **never** derives it from cell display text (freeze §3 rule 4). |
| `Cells` | `IReadOnlyList<DenseTableCellPresentation>` | **mandatory**; count **must equal** the column count, else `Create` fails before rendering. Cell text is supplied plain text rendered Razor-encoded; the component never parses or interprets it. |
| `Status` | `RecordStatusPresentation?` | optional supplied row status (§5.4). |
| `Actions` | `IReadOnlyList<SharedActionPresentation>` | optional row-scoped generic actions (P2-T01 carrier; disabled actions already require a supplied reason). Empty by default. |

Rules:

- **Order and identity.** Rows render in exactly the supplied order; `Key` is the only stable
  identity used for selection and event carriers.
- **Compact density.** Uniform compact row height, single hairline row separation; no per-row
  cards, no variable decorative padding.
- **Read-only by default.** A row with no supplied actions and selection disabled is fully
  read-only: no interactive element is rendered inside it.
- **No per-row selectability flag** and no per-row open flag are invented; selection and open are
  table-level consumer decisions (§5.6).
- **Row actions are row-scoped only.** The frozen output is `action invoked` with opaque
  **row/action** keys. Table-scoped/global actions belong to the consumer's own `DecisionBar`
  region (P2-T03) and are **not** rendered by `DenseDataTable`.
- **Actions cell.** When at least one supplied row carries actions, the component renders one
  component-owned trailing actions cell as the **last** rendered column. Its position is fixed and
  independent of width, `Priority`, `WidthHint` or row content. Its header text comes from the
  supplied `ActionsColumnHeading`, else a generic A-owned label.
- **Explicit open control.** When `OpenEnabled` is true, each row renders an explicit open control
  (pointer/keyboard parity, freeze §9). When `OpenEnabled` is false, **no** open control is
  rendered for any row.

`DenseTableCellPresentation` — sealed record with static `Create(string text)`: mandatory non-blank
`Text`, rendered as encoded visible text.

### 5.4 Row status slot

The optional `Status` member is a P2-T01 `RecordStatusPresentation` rendered by the accepted
`_RecordStatus.cshtml` partial (tone is supplementary; text always visible). P2-T02 defines **no**
table-specific status vocabulary, no status→column mapping and no lifecycle inference. A row
without a supplied status renders no status.

### 5.5 Controlled filter / paging / sort (P2-T02 PIN)

These are **controlled, consumer-owned facts**. The component renders what it is given and
performs **no** transformation. It contains no filter operators, no boolean expressions, no query
semantics, no sorting algorithm, no paging arithmetic and no result counting.

| Carrier | Members | Behaviour |
|---|---|---|
| `DenseTableFilterPresentation` | `Key` (mandatory opaque), `Label` (mandatory), `Value` (mandatory controlled string), `Options` (`IReadOnlyList<DenseTableFilterOptionPresentation>?`, opaque `Value` + `Label`) | rendered only when supplied; `Options` present ⇒ a `<select>` of the supplied options, absent ⇒ a text input, both pre-filled with the supplied `Value`. No operators, no multi-condition model. Rendered **without** `action`/`method`/URL; the consumer owns the form target and wiring (freeze §3 rule 9). |
| `DenseTablePagingPresentation` | `PageLabel` (mandatory supplied display text, e.g. a page indicator the **consumer** computed), `Previous`/`Next` (`SharedActionPresentation?`) | rendered only when supplied. Page arithmetic is never performed by the component. Prev/next are supplied actions, so their disabled state already requires a supplied reason. |
| `DenseTableSortPresentation` | `ColumnKey` (mandatory opaque, must reference a supplied `Sortable` column), `Direction` (`DenseTableSortDirection` = `Ascending` \| `Descending`) | controlled current sort presentation only: renders the affordance/`aria-sort`. The component never reorders rows. |
| `ResultSummary` | `string?` | supplied result/metadata text (counts are computed by the consumer), rendered verbatim and announced politely (§8). |

Enforcement: **supplied filter/paging/sort state must never change the rendered row set or row
order** — the rendered rows are exactly the supplied rows (§11 AC-2).

`DenseTableEvent` / `DenseTableEventKind` — the frozen generic event vocabulary
(`RowSelected`, `OpenRequested`, `ActionInvoked`, `FilterChanged`, `PageChanged`, `SortRequested`),
each carrying only opaque keys/carriers (`RowKey`, `ActionKey`, `FilterKey`, `FilterValue`,
`PageCarrier`, `SortColumnKey`, `SortDirection`). The component **raises** these and **never acts**
on them. No event carries a URL, route or resolved target.

### 5.6 Selection and open mechanics (FINAL PRESENTATION CONTRACT)

Selection and open are **separate behaviours** (freeze §9; handoff §4.1):

| Input | Selection | Open |
|---|---|---|
| single pointer click | selects the row | **never** |
| `Space` | selects the row | **never** |
| double click | selects the row (idempotent) | requests open, **once per gesture** |
| `Enter` | unchanged | requests open, **once per activation burst** |
| explicit open control | unchanged | requests open, **once per activation burst** |

Rules:

- At most **one** selected row; selection is a programmatic fact (`aria-selected`), never
  colour-only (§8).
- Selection is enabled/disabled by the supplied `SelectionEnabled`; open by the supplied
  `OpenEnabled`. When `OpenEnabled` is false no open is ever requested and no open control exists.
- **Duplicate-open collapse.** Two consecutive open activations for the **same** row collapse into
  a single open invocation; a selection input or an interaction with a different row closes the
  collapse window.
- `SelectedKey` is **controlled**: a non-null `SelectedKey` must reference a supplied row key
  (else the presentation fails before rendering). When `SelectionEnabled` is false, `SelectedKey`
  must be null.
- **Selection triggers no domain action**: neither selection nor open emits an `ActionInvoked` or
  any mutation. Selection produces only a `RowSelected` presentation event.
- **No URL is constructed**: the component produces no URL, route, `href`, `action`, `method` or
  navigation; open carries only the opaque row key and the consumer resolves the target.

### 5.7 Unit-testable interaction model (implementation mechanism)

Because the frozen interaction rules must be **failing-if-removed unit-tested** while the
repository has no JS test runner and no authorized browser test package, the arbitration is
implemented as a small, deterministic, presentation-only C# model and the JS is a thin DOM
adapter over the same rules.

`DenseTableInteraction` (sealed class in `DMO.Web.Frontend.Shared.Contracts`):

```text
DenseTableInteraction(bool selectionEnabled, bool openEnabled)
    bool  SelectionEnabled { get; }
    bool  OpenEnabled      { get; }
    string? SelectedKey    { get; }
    DenseTableOutcome Click(string rowKey)
    DenseTableOutcome DoubleClick(string rowKey)
    DenseTableOutcome Space(string rowKey)
    DenseTableOutcome Enter(string rowKey)
    DenseTableOutcome ActivateOpen(string rowKey)
    void Reset(string? selectedKey = null)

DenseTableOutcome(bool Selected, bool OpenRequested, DenseTableEvent? Event)
```

Semantics are exactly §5.6. The model: holds no service, no clock, no identity, no URL and no
domain vocabulary; exposes no member returning a `Uri`, `href`, `route` or navigation target.

### 5.8 Consumer-supplied input summary (minimum explicit input)

`DenseTablePresentation` — sealed record with `Create` carrying exactly:

`State` (`CommonState`, mandatory), `Caption` (mandatory non-blank accessible name),
`Columns`, `Rows`, `SelectionEnabled` (default `false`), `SelectedKey` (`string?`, default null),
`OpenEnabled` (default `false`), `Message` (`string?`, **mandatory when `State != Ready`**),
`Reason` (`string?`, programmatically associated when supplied), `StateActions`
(`IReadOnlyList<SharedActionPresentation>`, e.g. supplied retry / next / refresh / recovery),
`ResultSummary` (`string?`), `Filters`, `Paging`, `Sort`, `ActionsColumnHeading` (`string?`),
`RegionLabel` (`string?`, defaults to `Caption`).

No other input is added: no module column set, no data source, no query, no target, no permission
flag, no domain status vocabulary.

## 6. `AuditTrail` Contract

Owner: Workstream A (supplied-fact rendering). Fact owner: the supplying feature. Consumers: B, C, D, E.

**Distinct concepts — must not be conflated:**

```text
AuditTrail      = this shared presentation primitive for supplied historical events
HISTÓRICO       = local, module-specific history functionality inside a module
HISTÓRICO GLOBAL= a future top-level aggregating module (not Beta, not P2-T02)
```

### 6.1 Responsibility

Render supplied, attributed historical event facts compactly and accessibly, in the supplied
order, delegating all non-`ready` surfaces to the accepted P2-T01 components.

It must not invent or own domain history: no audit creation, no actor resolution, no timestamp
generation, no clock/locale access, no diff calculation, no ordering inference, no persistence,
no correction workflow, no authorization.

### 6.2 Event carrier (P2-T02 PIN)

`AuditEntryPresentation` — sealed record with static `Create`.

| Member | Type | Rule |
|---|---|---|
| `Key` | `string` | **mandatory, non-blank**, **opaque** stable event identity. Never a canonical identity, never a URL/endpoint. |
| `ActionText` | `string` | **mandatory, non-blank** supplied action/event text. The core supplied display text of the entry. |
| `Actor` | `string?` | optional supplied actor **display fact**, rendered verbatim. Absent ⇒ no actor text is rendered (never substituted). |
| `ActorUnavailableText` | `string?` | optional supplied explicit "attribution unavailable" text, rendered verbatim **only** when `Actor` is absent (freeze §12: "show unavailable attribution explicitly when supplied"). |
| `TimestampText` | `string?` | optional supplied **human-readable** timestamp display value, rendered verbatim. The component performs **no** formatting, parsing or culture derivation. |
| `TimestampValue` | `DateTimeOffset?` | optional supplied **semantic** timestamp, rendered only as the `<time datetime="…">` machine value alongside the supplied display text. Never used to sort or to fill `TimestampText`. |
| `Detail` | `string?` | optional supplied concise detail/description. |
| `BeforeDetail` | `string?` | optional supplied before-state display detail. |
| `AfterDetail` | `string?` | optional supplied after-state display detail. |
| `DetailAction` | `SharedActionPresentation?` | optional single generic consumer action (freeze §12: "generic detail action"). |

**Not included** (authority does not support them): generic metadata dictionaries, arbitrary
key/value bags, source/context identity fields, canonical IDs, user IDs, actor resolution hooks,
diff payloads, mutation controls.

Rules:

- No field is a canonical identity and none may be declared as `tool_id`/`jobon_id`/`peso_id` or
  any other canonical identity (freeze §2).
- The component has **no** access to any session, current-account, clock or service. A missing
  actor/timestamp is **never** filled with the current user or the current time (freeze §12;
  handoff §4.2; `ACCEPTANCE_MATRIX` §3 "no synthesized attribution").
- `BeforeDetail`/`AfterDetail`/`Detail` render only when supplied; no diff is computed.

### 6.3 Ordering

`AuditTrailPresentation.Entries` renders in **exactly the supplied sequence**. The component:

- performs no sorting, no reordering and no chronology inference (**ordering inference is an
  explicit non-responsibility**, freeze §12);
- asserts no chronological direction. It does not label the list newest-first or oldest-first and
  does not default to either;
- uses no `<ol>` ordering claim; entries are rendered in a plain semantic list.

Recorded **authority silence** (not a guess, not a blocking question): no Beta or freeze authority
states a newest-first or oldest-first rule. Authority resolves this by **delegation** — "preserve
supplied ordering". Any per-module chronological direction (Job On history, Controlo decision
history, Boquilhas movement history, document history) is later module authority (P2-T04, P2-T06,
P2-T07, P2-T08) supplied to this component as input order.

### 6.4 Display contract

- A labelled history region carrying the supplied `RegionLabel`; compact entry presentation.
- Per entry: the supplied `ActionText` is the prominent element; the supplied `Actor`
  (or `ActorUnavailableText`) and supplied `TimestampText` are rendered as labelled facts on a
  compact line; `Detail`/`BeforeDetail`/`AfterDetail` render only when supplied, under generic
  A-owned labels ("Detalhe", "Antes", "Depois", "Ator", "Data/hora").
- `TimestampText` is always the visible timestamp text; `TimestampValue`, when supplied, is the
  machine value only. Visible text is never derived from `TimestampValue`.
- No cards, no oversized headers, no decorative vertical whitespace, no stacked variant (§4).
- Optional supplied parent-controlled filter/page context is represented by
  `AuditTrailPresentation.ContextText` (supplied display text rendered verbatim; the component
  never computes, applies or filters). Parent-owned filter/page controls remain in the parent's
  own region and are **not** part of the `AuditTrail` surface in P2-T02.

### 6.5 Read-only rule

`AuditTrail` is **read-only presentation**. It renders:

- no edit, delete, restore, correct, undo or mutate control of any kind;
- exactly one optional interactive element per entry: the supplied generic `DetailAction`
  (freeze §12 "optional … generic detail action"), plus any actions the region state supplies
  (e.g. retry) — both rendered through the accepted P2-T01 action carrier with the accepted
  disabled-reason rule.

It performs no persistence and raises no domain event. `DetailAction` produces only the frozen
generic presentation event; the consumer owns the detail flow.

### 6.6 Consumer-supplied input summary

`AuditTrailPresentation` — sealed record with `Create` carrying exactly:

`State` (`CommonState`, mandatory), `RegionLabel` (mandatory non-blank accessible name),
`Entries` (`IReadOnlyList<AuditEntryPresentation>`), `Message` (`string?`, **mandatory when
`State != Ready`**), `Reason` (`string?`, programmatically associated when supplied), `Actions`
(`IReadOnlyList<SharedActionPresentation>`, e.g. supplied retry/recovery), `ContextText`
(`string?`).

## 7. Shared State Integration

Both components use the **accepted P2-T01 vocabulary**. No table-specific or audit-specific state
enum, token or message set exists in P2-T02.

Delegation rule (both components):

- every non-`ready` state renders through the accepted `CommonStateRegionPresentation` /
  `_CommonStateRegion.cshtml` semantics — same token, heading, message, live-region behaviour and
  associated reason as P2-T01; no P2-T02 partial re-implements a state surface;
- **`empty`, `lookup-failed`, `unavailable` and `permission-denied` remain mutually distinct**
  renderings (P2-T01 freeze §4 rule); P2-T02 must not collapse or alias them;
- **`empty` is not `lookup-failed`**: an empty result is never rendered as a failure and a failed
  lookup is never rendered as an empty result.

### 7.1 `DenseDataTable` per-state behaviour (freeze §9)

| `CommonState` | Required rendering |
|---|---|
| `Ready` | the table: caption, header row, supplied rows in supplied order, optional result summary, optional filters/paging/sort affordances. |
| `Empty` | **column/header context is retained** (the supplied caption + header row) plus the P2-T01 `empty` presentation: explicit supplied message + supplied next action. Never the `lookup-failed` token or message. |
| `Loading` | the region is marked busy; when the consumer supplies retained rows they are rendered in supplied order and **each retained row is labelled stale** with visible, non-colour-only text; when no retained rows are supplied the P2-T01 `loading` surface (progress/skeleton text) is rendered. |
| `Stale` | retained rows plus the P2-T01 `stale` surface (warning + supplied refresh action). |
| `LookupFailed` | the P2-T01 `lookup-failed` surface (assertive, supplied retry). **No empty-result body and no `empty` message is rendered.** |
| `Unavailable` | the P2-T01 `unavailable` surface with the supplied reason programmatically associated. |
| `PermissionDenied` | the P2-T01 `permission-denied` surface with the supplied reason. **No partial row data is rendered.** |
| `Conflict` | the P2-T01 `conflict` surface with the supplied summary/recovery. The table never resolves a conflict. |
| `Saving` / `Submitting` | not table-region states. A pending row action is expressed through the supplied action's pending presentation; the component adds no table saving/submitting surface. |

For `LookupFailed`, `Unavailable`, `PermissionDenied` and `Conflict` the data body is **not**
rendered (so a failure can never look like an empty table).

### 7.2 `AuditTrail` per-state behaviour (freeze §12)

| `CommonState` | Required rendering |
|---|---|
| `Ready` | supplied entries in supplied order. |
| `Loading` | busy, labelled history region (P2-T01 `loading`). |
| `Empty` | P2-T01 `empty`: explicit no-history, **not** a failure. |
| `LookupFailed` | P2-T01 `lookup-failed` with supplied retry. |
| `Unavailable` / `PermissionDenied` | P2-T01 surface with the supplied reason, **without any partial protected entry facts** (no entry markup is rendered). |
| `Stale` | retained entries plus the P2-T01 `stale` warning (supplied refresh action). |
| `Conflict` / `Saving` / `Submitting` | normally parent-owned; the component never alters history and adds no audit-specific surface. |

## 8. Accessibility Contract

Native semantics first; ARIA only where native HTML cannot express the frozen requirement.

### 8.1 `DenseDataTable`

- **Semantic table**: `<table>` with a `<caption>` carrying the supplied `Caption`; a header row of
  `<th scope="col">`; data cells `<td>`. Header associations come from native `scope`, not from
  invented ARIA.
- **Visible text**: every state has visible heading/message text; selection, status and action
  meaning is never colour-only (freeze §3 rule 6).
- **Selection is programmatic and non-colour-only**: the selected row carries
  `aria-selected="true"` **and** a visible non-colour indicator element (a marker glyph), plus a
  CSS treatment. At most one row is selected.
- **Focus**: the selected row's interactive elements are keyboard reachable in logical order;
  focus is visible (`:focus-visible`, `--dmo-focus`). Focus survives re-render on the selected row
  when it is still present; when filtering/paging removes it, focus moves predictably in the order
  **result summary → table heading → first row**.
- **Row actions**: each renders as a native `<button>` (or a link only if the consumer supplies
  it — P2-T02 supplies controls, not URLs) carrying its opaque action key; a disabled action
  renders `aria-disabled="true"` **and** `disabled`, with its supplied reason associated through
  `aria-describedby` → rendered `id` (the accepted P2-T01 pattern). The action's accessible name
  includes the row's supplied `AccessibleContext` (freeze §9 "row actions include row context").
- **Result summary** is rendered in a polite live region; state changes use the P2-T01 region
  semantics (busy for loading, assertive for `lookup-failed`/`conflict`, otherwise no live region).
- **Horizontal overflow is keyboard reachable and does not trap focus**: the scroll container is
  focusable (`tabindex="0"`) with an accessible name and no key handler that swallows `Tab`; it
  has a visible focus style.
- **Sort affordance** exposes current direction programmatically (`aria-sort` on the header) from
  supplied state only.
- No `role="grid"`, no custom composite-widget ARIA and no JS-managed roving tabindex are
  introduced where native table semantics suffice.

### 8.2 `AuditTrail`

- **Semantic list**: a labelled region containing a plain `<ul>` of `<li>` entries — no `<ol>`
  chronology claim (§6.3) and no `role="timeline"` invention.
- Each entry exposes its supplied facts with visible labels: action text prominent; actor,
  timestamp, before, after, detail labelled; `TimestampText` visible; `TimestampValue` only as the
  `<time datetime="…">` machine value.
- **Entries are focusable only when they contain a supplied action** (freeze §12); otherwise a row
  is not in the tab order. The supplied `DetailAction` is a native control with the accepted
  disabled-reason association.
- The region's state surfaces inherit the P2-T01 accessibility behaviour (busy/assertive/labelled
  reason); `unavailable`/`permission-denied` never leak partial entry facts.
- **Closing a consumer detail surface restores invoking focus** through the shared generic focus
  helper (§14.4); the component supplies the hooks, the consumer owns the surface.
- Change meaning is never colour-only; visible text always carries it.

## 9. Styling / Token Contract

- **Additive only, same stylesheet.** New rules are appended to
  `src/DMO.Web/wwwroot/css/dmo-components.css`. No parallel/duplicate stylesheet is created and no
  pre-existing selector (P2-T01 or shell) is rewritten or removed (§12 #19).
- **Existing design tokens only.** Every `var(--dmo-*)` used must already be defined in
  `wwwroot/css/dmo-tokens.css`. **No new token and no hex palette** is introduced. Reused tokens
  include at least `--dmo-line`, `--dmo-line-strong`, `--dmo-surface`, `--dmo-surface-muted`,
  `--dmo-ink`, `--dmo-muted`, `--dmo-radius`, `--dmo-accent`, `--dmo-danger`, `--dmo-warning`,
  `--dmo-success`, `--dmo-focus`, `--dmo-control-height`.
- **Scoped selectors**: only `dmo-table*` and `dmo-audit*` (plus reuse of the P2-T01
  `visually-hidden` helper for supplied assistive text). No new generic utility classes beyond
  what P2-T02 needs.
- **No breakpoint/reflow rules**: the new blocks contain no `@media`, no `@container` and no
  width-conditional structural rule (§4.1.3).
- **`DenseDataTable` visual contract**
  - compact density: header and rows use a single uniform compact vertical rhythm; a single
    hairline row separation (`--dmo-line`); no zebra striping required; no card containers;
  - caption/header treatment: visible header text, emphasis by weight, not by colour alone;
  - selected treatment: background/border change **plus** the visible marker element;
  - action location: the trailing actions cell is the last column, fixed position;
  - status placement: inside the row's designated status slot, rendered by the P2-T01 status
    partial; never overlaid on cell content;
  - overflow: the local horizontal scroll container (`.dmo-table__scroll`, `tabindex="0"`) with a
    visible focus style; no focus trap;
  - empty/state placement: the P2-T01 state region renders directly below the retained
    header context (empty) or as the region content (other non-ready states) with consistent
    compact spacing.
- **`AuditTrail` visual contract**
  - compact event density: one entry per compact block, hairline separation, no cards, no
    decorative vertical whitespace;
  - timestamp placement: on the entry's compact meta line with its label, never as a large
    decorative header;
  - actor/action hierarchy: supplied `ActionText` is the prominent element; actor/timestamp are
    secondary, compact meta;
  - detail placement: `Detail`/`BeforeDetail`/`AfterDetail` render below the meta line, only when
    supplied, with compact spacing;
  - state presentation: the P2-T01 state region, consistent with the table's state spacing.
- No decorative styling beyond the above. Visual polish ranks last in the binding design priority
  order (workflow stability, control placement, density, readability, consistency, polish).

## 10. Test Contract

Tests are **specified here, not written in this task**. Requirements below are the minimum for
P2-T02 acceptance; all are failing-if-removed. Evidence must separate committed test source
inspected in Git from execution evidence (`dmo-beta-master/WORKFLOW.md` "Testing evidence").

Naming/placement: unit tests under `tests/DMO.UnitTests/Frontend/Shared/`; rendered tests under
`tests/DMO.IntegrationTests/Frontend/Shared/`. Rendered tests must exercise the **real compiled**
partials through the existing test-host view engine (the P2-T01 `SharedComponentRenderer`
approach). P2-T02 may add new helper methods/classes, but **must not modify any existing test
method or its assertions**.

### 10.1 `DenseDataTable` — unit (model/contract)

| # | Test | Proves |
|---|---|---|
| U1 | supplied columns render in supplied order; the rendered order is identical for every `Priority` and `WidthHint` combination | stable column order; priority is never authority to hide/reorder (§5.2, AC-1) |
| U2 | rows render in supplied order; changing supplied filter/paging/sort state leaves the rendered rows and their order byte-identical | no filtering/sorting/paging transformation (§5.5, AC-2) |
| U3 | a row whose cell count differs from the column count fails before rendering | no silent misalignment (§5.3) |
| U4 | `Click` and `Space` select and never request open | selection ≠ open (§5.6, AC-4) |
| U5 | `DoubleClick` requests open exactly once and leaves the row selected | double click opens once |
| U6 | two consecutive `ActivateOpen`/`Enter` activations for the same row produce exactly one open request; a different row or a selection input reopens the window | duplicate-open collapse (§5.6, AC-5) |
| U7 | with `OpenEnabled = false`, `DoubleClick`/`Enter`/`ActivateOpen` produce no open request | consumer-gated open (§5.6, AC-7) |
| U8 | with `SelectionEnabled = false`, no input selects; with `SelectionEnabled = true`, at most one row is selected and `SelectedKey` equals the controlled value | controlled single selection (§5.6, AC-4) |
| U9 | `Create` rejects a non-null `SelectedKey` absent from the supplied rows, and rejects a `SelectedKey` when selection is disabled | no phantom selection (§5.6) |
| U10 | `Create` rejects a blank `Caption`, blank cell text, blank column `Key`/`Heading`, blank row `Key`/`AccessibleContext`, and a missing `Message` when `State != Ready` | fail-closed carriers; no invented text (§5.2, §5.3, §5.8) |
| U11 | no `DenseDataTable` type exposes a member returning a `Uri`, URL, route or navigation target, and no contract member/emitted event contains a URL string | no URL construction (AC-15) |
| U12 | `DenseTableRowPresentation.Actions` reuses `SharedActionPresentation`, and a disabled supplied action without a reason is rejected | disabled-action reason rule (§5.3, AC-12) |
| U13 | no `DenseDataTable` contract type references a feature/domain namespace, service, `DbContext`, `HttpClient`, Supabase, or a session/clock | domain-neutral boundary (AC-26, AC-25) |

### 10.2 `AuditTrail` — unit (model/contract)

| # | Test | Proves |
|---|---|---|
| U14 | entries render in exactly the supplied order for any supplied sequence; no ordering metadata changes it | no sorting/chronology inference (§6.3, AC-18) |
| U15 | `ActionText` is mandatory; `Actor`/`TimestampText` absent ⇒ no attribution text is produced; `ActorUnavailableText` renders only when `Actor` is absent | no synthesized attribution (§6.2, AC-20) |
| U16 | no `AuditTrail` type/member references `ICurrentAccountContext`, session, clock, `DateTime.Now`/`DateTime.UtcNow`, or any service; the type has no formatting/parsing member for timestamps | structural no-synthesis guarantee (§6.2, AC-25) |
| U17 | supplied `TimestampText` is preserved verbatim; `TimestampValue`, when supplied, is exposed as a machine value only and never used to populate the display text | no locale/format derivation (§6.2, §6.4) |
| U18 | `BeforeDetail`/`AfterDetail`/`Detail` render only when supplied; no diff is computed | no invented detail (§6.2, AC-21) |
| U19 | the only interactive element carried by an entry is the optional supplied `DetailAction`; no edit/delete/restore member exists | read-only rule (§6.5, AC-24) |
| U20 | no `AuditTrail` contract type references a feature/domain namespace, service or canonical identity | domain-neutral boundary (AC-26) |

### 10.3 Rendered (real compiled partials)

| # | Test | Proves |
|---|---|---|
| R1 | `empty` rendered surfaces for both components carry the P2-T01 `empty` token/heading and the supplied message, and never the `lookup-failed` token/message; `lookup-failed` carries its own token, assertive announcement and supplied retry and never the `empty` token/message | §7 distinction (AC-8, AC-9, AC-22) |
| R2 | `DenseDataTable` `empty` retains the caption/header context; `lookup-failed`/`unavailable`/`permission-denied`/`conflict` render no data body | §7.1 (AC-8, AC-9, AC-11) |
| R3 | `DenseDataTable` `loading` marks the region busy and each supplied retained row carries visible stale text and a stale hook | §7.1 (AC-10) |
| R4 | selected row exposes `aria-selected="true"` plus the visible marker element; exactly one row is selected for a supplied `SelectedKey` | programmatic, non-colour selection (AC-17) |
| R5 | row actions render with `data-dmo-action`/`data-dmo-row-key`; a disabled action renders `aria-disabled="true"`, `disabled`, an `aria-describedby` that resolves to the rendered reason `id`, and an accessible name containing the supplied row context | disabled reasons + row context (§8.1, AC-12) |
| R6 | the explicit open control renders **iff** `OpenEnabled`; the actions cell renders last and only when actions are supplied | AC-6, AC-7, §5.3 |
| R7 | row status renders through the accepted P2-T01 status partial (tone token + visible text); a row with no supplied status renders no status | §5.4 (AC-13) |
| R8 | supplied `ResultSummary` renders verbatim inside a polite live region | AC-14 |
| R9 | supplied filter/paging/sort affordances render with their controlled values and **no** `action`/`method`/`href` attribute is produced by the component | §5.5, AC-15 |
| R10 | `AuditTrail` entries render actor/timestamp/action/before/after/detail with their visible labels and hooks; a missing actor/timestamp produces no attribution text | §6.4 (AC-19, AC-20) |
| R11 | `AuditTrail` `unavailable`/`permission-denied` render only the P2-T01 state surface with the supplied reason and no entry markup | §7.2 (AC-23) |
| R12 | `AuditTrail` renders no edit/delete/restore control for any state | §6.5 (AC-24) |
| R13 | the table scroll container is focusable (`tabindex="0"`), labelled, and no component markup renders a `role="grid"` or a focus-trapping handler | §8.1 (AC-30) |
| R14 | the P2-T01 rendered suites still pass unchanged and `Rendering_LiveShellAndNavigationBehaviourIsUnchanged` stays green | regression R (`ACCEPTANCE_MATRIX` §11) |

### 10.4 CSS / static-asset

| # | Test | Proves |
|---|---|---|
| S1 | `/css/dmo-components.css` resolves and contains the new `dmo-table*`/`dmo-audit*` selectors plus the P2-T01 selectors unchanged | additive CSS (§9) |
| S2 | the new P2-T02 CSS blocks contain **no** `@media`, no `@container` and no width-conditional rule | no breakpoint reflow (§4.1.3, AC-30) |
| S3 | every `var(--dmo-…)` referenced by the new rules is defined in `dmo-tokens.css` | token reuse, no new design system (§9, AC-29) |
| S4 | `/js/dmo-focus.js` and `/js/dmo-dense-table.js` resolve and contain no `fetch(`, `XMLHttpRequest`, `location.`/`href`/`window.open`, no `submit(`, and no domain vocabulary | generic, non-fetching, non-navigating assets (AC-15, AC-26) |
| S5 | the asset include helper emits each asset tag at most once per render | no duplicated includes (§14.5) |

### 10.5 Regression / protected foundation

| # | Test | Proves |
|---|---|---|
| G1 | `ModuleRegistrations.CurrentBuildAvailable` is `[]` | AC-27 |
| G2 | no route is added or changed (`DestinationRoutes`/`DestinationRouteRegistrations` and page routes unchanged) | AC-27 |
| G3 | the full pre-existing unit and integration suites pass with no weakened or deleted test | regression R |
| G4 | the P2-T01 contract files, P2-T01 partials and pre-existing CSS selectors are unchanged | additive-only boundary (§13.3, AC-28) |

### 10.6 Not required by P2-T02

Browser/DOM-level verification of the two JS adapters (keystroke-to-event wiring in a real
browser, computed focus survival) is **not** schedulable in P2-T02 with the current
infrastructure; see CONTRACT QUESTION Q1. Recorded as deferred, not omitted.

## 11. Acceptance Criteria

Objective and checkable. "Failing-if-removed test" means the referenced test in §10.

**DenseDataTable**

1. **AC-1** Given N supplied columns, `DenseDataTable` renders them in exactly the supplied
   order, and the rendered column set and order are identical for every `Priority` and
   `WidthHint` combination (U1, R1).
2. **AC-2** Given supplied rows, the rendered row set and row order equal the supplied rows and
   order, and are unaffected by any supplied filter, paging or sort state (U2).
3. **AC-3** A row supplying a cell count different from the column count is rejected before
   rendering (U3).
4. **AC-4** A single pointer click and a `Space` activation select a row and never raise an open
   request; at most one row is selected at any time; the selected row is the supplied controlled
   `SelectedKey` (U4, U8, R4).
5. **AC-5** A double click and an `Enter` activation raise exactly one open request, and two
   consecutive open activations for the same row raise exactly one open request (U5, U6).
6. **AC-6** An explicit open control is rendered if and only if `OpenEnabled` is true, and it
   requests open exactly as `Enter` does (R6).
7. **AC-7** When `OpenEnabled` is false, no open request is raised and no open control is
   rendered (U7, R6).
8. **AC-8** `State = Empty` renders the retained column/header context plus the accepted P2-T01
   `empty` presentation with the supplied message and next action, and never renders the
   `lookup-failed` token or message (R1, R2).
9. **AC-9** `State = LookupFailed` renders the accepted P2-T01 `lookup-failed` presentation
   (assertive announcement, supplied retry) and never renders the `empty` token, the `empty`
   message or an empty-result body (R1, R2).
10. **AC-10** `State = Loading` marks the region busy, and every supplied retained row is labelled
    stale with visible non-colour-only text (R3).
11. **AC-11** `State = Unavailable` and `State = PermissionDenied` render the accepted P2-T01
    presentations with the supplied reason programmatically associated, and render no partial row
    data (R2).
12. **AC-12** Every supplied row action renders as a control carrying its opaque action key and
    the row's opaque key, positioned in the fixed trailing actions cell; a disabled action
    exposes `aria-disabled="true"` with its supplied reason programmatically associated; the
    action's accessible name includes the supplied row context (U12, R5, R6).
13. **AC-13** A supplied row status renders through the accepted P2-T01 `RecordStatus`
    presentation; a row without a supplied status renders no status (R7).
14. **AC-14** Supplied `ResultSummary` text is rendered verbatim and exposed as a polite live
    region (R8).
15. **AC-15** The component constructs no consumer URL: no member, attribute, markup or emitted
    event produced by P2-T02 contains a URL, `href`, `action`, `method` or route target (U11, R9,
    S4).
16. **AC-16** The component contains no filter operator model, no query expression, no sorting
    algorithm, no paging arithmetic and no result counting; supplied rows are never transformed
    (U2, §5.5).
17. **AC-17** The selected row exposes a programmatic selected fact (`aria-selected="true"`) and a
    visible non-colour indicator (R4).

**AuditTrail**

18. **AC-18** Supplied entries render in exactly the supplied order; the component performs no
    sorting, reordering or chronology inference (U14).
19. **AC-19** Each entry renders its supplied action text; supplied timestamp display text renders
    verbatim and the supplied semantic value appears only as the machine value (U17, R10).
20. **AC-20** A supplied actor renders verbatim; when actor or timestamp is absent the component
    renders no substitute attribution text and never any current-session/current-user/current-time
    value (U15, U16, R10).
21. **AC-21** `Detail`/`BeforeDetail`/`AfterDetail` render only when supplied, with their visible
    labels; no diff is computed (U18).
22. **AC-22** `AuditTrail` `empty` (explicit no-history) and `lookup-failed` (failure with
    supplied retry) render as distinct accepted P2-T01 presentations and are never the same
    rendering (R1).
23. **AC-23** `AuditTrail` `unavailable`/`permission-denied` render only the accepted P2-T01 state
    surface with the supplied reason, with no partial entry markup (R11).
24. **AC-24** The component exposes no edit/delete/restore/mutation control and no persistence;
    the only interactive element carried per entry is the optional supplied generic detail action
    (U19, R12).
25. **AC-25** No P2-T02 type, partial or script references a session, current-account, clock,
    time provider or any service; missing actor/time is never synthesized (U13, U16).

**Shared**

26. **AC-26** No P2-T02 contract type, partial or script references a feature/domain namespace,
    service, `DbContext`, `HttpClient`, Supabase, canonical identity or module terminology
    (U13, U20, S4).
27. **AC-27** `ModuleRegistrations.CurrentBuildAvailable` remains `[]`; no route is added or
    changed; no module/availability registration is touched (G1, G2).
28. **AC-28** P2-T02 is additive only: no accepted P2-T01 contract type, partial, CSS selector or
    existing test method is modified, and every pre-existing test still passes (G3, G4).
29. **AC-29** No new design token is introduced; new CSS is scoped to `dmo-table*`/`dmo-audit*`
    and uses only tokens already published in `dmo-tokens.css` (S3).
30. **AC-30** The new component CSS contains no breakpoint/container rule; there is no
    table-to-card conversion, no required-column hiding, no column reordering and no action
    relocation at any width; over-width content uses a keyboard-reachable local horizontal scroll
    container with no focus trap (S2, R13).

31. **AC-31** No protected foundation file (§13.1) is modified; the implementation diff touches
    only A-owned paths (§14).

## 12. Contract Questions

Three questions were recorded. Architect review accepted the Q1 and Q3 defaults and resolved Q2
to **ABSENT**. The corrected contract remains subject to Architect re-review and does not
self-authorize implementation.

### Q1 — DOM/browser-level verification authority for the two JS adapters

- **Decision:** whether `dmo-dense-table.js`/`dmo-focus.js` DOM wiring (keystroke → event, computed
  focus survival) must be verified in a real browser as part of P2-T02, or whether the C#
  interaction model unit tests + rendered markup assertions + static-asset content assertions are
  sufficient P2-T02 evidence.
- **Authority inspected:** master plan §11 P2-T02 (interaction rules required as **U** tests);
  master plan §12 #23 and A plan §10.3 (browser checks require an Architect-approved, centrally
  pinned browser test package, and the "do not create a new test project without approval" rule);
  A plan §14 A7 (responsive/accessibility/focus validation) — whose tablet clauses are superseded
  by the fixed desktop policy (§4.2).
- **Why insufficient:** authority requires the interaction *behaviours* and requires U-level
  evidence, but does not state whether a new browser package is authorized for A4; no browser test
  package exists in the repository today and adding one is itself an Architect decision.
- **Non-blocking default:** implement the arbitration as the C# `DenseTableInteraction` model
  (unit-tested, U4–U8) plus rendered markup/hook assertions (R4–R6, R13) and static-asset content
  assertions (S4); defer DOM-level browser verification to a separately authorized browser package
  or to consumer integration (A8)/P2-T10. **Architect disposition: ACCEPTED DEFAULT PRESERVED.**

### Q2 — per-entry `RecordStatus` on `AuditTrail` entries — RESOLVED

- **Resolution:** **ABSENT**. `AuditEntryPresentation` carries no `Status` or equivalent generic
  status field, and `AuditTrail` renders no entry-level `RecordStatus`.
- **Authority basis:** A1 freeze §12 defines the AuditTrail carrier as entry key, actor,
  timestamp, action, optional before/after detail, generic detail action and optional
  parent-controlled filter/page context. It does not authorize an entry-level status slot.
- **Architect disposition:** generic P2-T01 `RecordStatus` availability does not expand the
  AuditTrail carrier. Change meaning remains visible through the authority-backed supplied action
  and detail facts. No replacement field is introduced.

### Q3 — plain supplied text vs. consumer-rendered cell markup

- **Decision:** whether `DenseTableCellPresentation` carries supplied **plain text** (P2-T02 PIN)
  or consumer-rendered markup (`IHtmlContent`) as the A1 freeze §9 carrier phrase
  "consumer-rendered cell content" could be read.
- **Authority inspected:** A1 freeze §9 carrier ("consumer-rendered cell content"); A1 freeze §2
  (PROVISIONAL carrier fields are "semantic slots, not mandatory C# property names", and may be
  refined before component implementation); A1 freeze §3 rules 2–4 (consumers supply facts; the
  component does not infer; display text is never identity); P2-T01 precedent (plain-text
  carriers: `AvailabilityPresentation.Text`, `CommonStateRegionPresentation.Message`).
- **Why insufficient:** the freeze names consumer-rendered content but classifies the slot as
  PROVISIONAL and does not settle whether a shared primitive accepts consumer markup.
- **Non-blocking default:** **plain supplied text**, Razor-encoded. Rationale: keeps the shared
  primitive free of consumer markup injection, keeps display text from being treated as identity,
  and matches the accepted P2-T01 carrier style. Richer cell rendering would be an A1 §16
  contract change. **Architect disposition: ACCEPTED DEFAULT PRESERVED.**

### Recorded authority silences (not questions, not inventions)

- **AuditTrail chronological direction** (newest-first vs oldest-first): authority is silent and
  explicitly delegates ordering to the supplier; P2-T02 renders the supplied order and asserts no
  direction (§6.3). Per-module direction is later module authority.
- **Column width control**: authority provides no per-column width carrier; P2-T02 defines
  deterministic content-driven widths with an advisory `WidthHint` and no pixel contract (§5.2).
- **Table-scoped (non-row) actions**: authority's frozen output carries a **row** key, so P2-T02
  contracts row-scoped actions only; global action surfaces belong to the consumer's `DecisionBar`
  region (P2-T03).

## 13. Protected Boundaries

### 13.1 Protected foundation (must not be touched by P2-T02)

Per master plan §12: canonical Module vocabulary/`ModuleCatalog`, `ModuleRegistry`,
`AccessResolver`/`AccessOutcome`/`ModuleResolve`, `ModuleAccessService`, the server-side Module
gate and ADMIN gate, authentication/session/current-account, account resolution, persistence
foundation and migrations 001/002, Template model and administration, USER administration,
Administration surface, root routing/landing/no-access, `NavigationProjectionService`,
`DestinationRoutes`/`DestinationRouteRegistrations`, the shared shell (`ShellPresentationModels`,
`ShellPresentationService`, `SharedFrontendExtensions`, `_Layout`, `_PublicLayout`, `_Identity`,
`Pages/Shared/Navigation/*`), shared tokens/shell CSS, the A1 frozen contract document,
`ModuleRegistrations`, the reviewed absence of provisional fixtures, and existing tests.

Explicitly:

- **`_Layout` and `_PublicLayout` are not modified** (no `<link>`/`<script>` insertion into the
  protected shell). Asset delivery follows the existing per-page convention (§14.5).
- **`dmo-tokens.css`, `dmo-shell.css`, `dmo-user-shell.css` are not modified**, including the
  pre-existing shell `@media` rule (§4.3).
- **`SharedFrontendExtensions.cs` is not modified** unless a genuinely required additive
  registration is authorized; the P2-T02 design requires none (static assets need no registration;
  `AddRazorPages()` already discovers `Pages/Shared/Components/*` and `UseStaticFiles()` already
  serves `wwwroot`).
- **No migration, no `Directory.*`/`.csproj`/package change, no route, no
  `CurrentBuildAvailable` change, no provisional-fixture marker** (`A2Fixtures`,
  `ProvisionalFixturesWhenNeeded`, `PROVISIONAL FRONTEND CONTRACT` markers, fixture CSS).

### 13.2 P2-T03 boundary (must be preserved)

`DenseDataTable` and `AuditTrail` must not include any `ToolPicker`, `ToolSummaryRow`,
`MeasurementRows`, `DecisionBar` or `ProductionContextStrip` concept, carrier, behaviour or
partial. The only seam P2-T02 defines for later consumers is the generic one it needs itself:

- the opaque row/column/action key model and the accepted P2-T01 `SharedActionPresentation`;
- the `DenseTableInteraction` selection/open arbitration (row-scoped).

No picker candidate model, no search/create/association state, no measurement row schema, no
minimum-row rule, no action-group (primary/secondary/danger) model, no pending-action
deduplication, no production-context fact or slot is defined. P2-T03 owns those.

### 13.3 P2-T01 boundary (accepted input, consume only)

P2-T02 **consumes** the accepted P2-T01 primitives and **does not** redesign, replace, rename,
reclassify or edit them. Specifically, P2-T02 must not modify:

- the 10 P2-T01 contract files (`CommonState`, `CommonStateTraits`, `CommonStateRegionPresentation`,
  `StatusTone`, `RecordStatusPresentation`, `AvailabilityState`, `AvailabilityTraits`,
  `AvailabilityPresentation`, `AvailabilityVersionPresentation`, `SharedActionPresentation`);
- the 3 P2-T01 partials (`_CommonStateRegion`, `_RecordStatus`, `_AvailabilityState`);
- any existing P2-T01 CSS selector or declaration;
- any existing test method or assertion.

If a change to a P2-T01 contract really appears necessary, it is an A1 §16 contract-change
proposal, not part of P2-T02.

## 14. Implementation File Ownership / Expected Paths

All paths are A-owned (§5.1 of the accepted Workstream A plan) or planning/governance paths.
Nothing outside this list may change.

### 14.1 New presentation contracts — `src/DMO.Web/Frontend/Shared/Contracts/`

```text
DenseTableColumnAlignment.cs          (enum: Start, Center, End)
DenseTableColumnWidthHint.cs          (enum: Auto, Compact, Wide)
DenseTableSortDirection.cs            (enum: Ascending, Descending)
DenseTableColumnPresentation.cs
DenseTableCellPresentation.cs
DenseTableRowPresentation.cs
DenseTableFilterOptionPresentation.cs
DenseTableFilterPresentation.cs
DenseTablePagingPresentation.cs
DenseTableSortPresentation.cs
DenseTablePresentation.cs
DenseTableEventKind.cs                (enum: RowSelected, OpenRequested, ActionInvoked, FilterChanged, PageChanged, SortRequested)
DenseTableEvent.cs
DenseTableOutcome.cs
DenseTableInteraction.cs
AuditEntryPresentation.cs
AuditTrailPresentation.cs
```

Conventions follow P2-T01: namespace `DMO.Web.Frontend.Shared.Contracts`; sealed records with a
private constructor + static `Create` (plus named factories where they remove ambiguity);
mandatory values rejected with `ArgumentException`/`ArgumentOutOfRangeException`; **no `using`
directives** beyond framework types (no service, `DbContext`, `HttpClient`, Supabase or feature
dependency is possible from the contracts).

### 14.2 New shared Razor partials — `src/DMO.Web/Pages/Shared/Components/`

```text
_DenseDataTable.cshtml                (the table surface + P2-T01 state delegation)
_AuditTrail.cshtml                    (the history surface + P2-T01 state delegation)
_SharedComponentAssets.cshtml         (optional single-line asset include helper, §14.5)
```

### 14.3 Additive stylesheet

```text
src/DMO.Web/wwwroot/css/dmo-components.css      (append P2-T02 blocks; no existing selector rewritten)
```

### 14.4 New generic static assets

```text
src/DMO.Web/wwwroot/js/dmo-focus.js          (generic focus capture/restore + documented fallback; no focus trap)
src/DMO.Web/wwwroot/js/dmo-dense-table.js    (generic selection/open arbitration + generic event dispatch; no fetch/URL)
```

Both are domain-neutral, idempotent if loaded twice, and must not read or write `location`,
`href`, `window.open`, forms or any endpoint. The C# `DenseTableInteraction` model is the
normative statement of the arbitration rules; the JS is a thin DOM adapter over the same rules.

### 14.5 Asset delivery (settled)

The component partials render **markup + stable `data-dmo-*` hooks only**; they do **not** emit
`<link>`/`<script>` tags and do **not** modify the protected shell. Asset delivery follows the
existing repository convention, where each page declares its own asset links (as the existing
Administration pages do):

```html
<link rel="stylesheet" href="~/css/dmo-components.css" asp-append-version="true" />
<script src="~/js/dmo-focus.js" defer></script>
<script src="~/js/dmo-dense-table.js" defer></script>
```

`_SharedComponentAssets.cshtml` is provided as an A-owned, additive convenience that emits exactly
one tag per asset, so a consumer needs a single `<partial name="_SharedComponentAssets" />` line.
Direct tags remain permitted. Neither route touches `_Layout`.

### 14.6 Tests

```text
tests/DMO.UnitTests/Frontend/Shared/            (DenseDataTable*Tests.cs, AuditTrail*Tests.cs)
tests/DMO.IntegrationTests/Frontend/Shared/     (rendered tests; reusable renderer helper(s))
```

Existing test methods must remain unchanged (§10).

### 14.7 Planning / governance (this contract authoring task only)

```text
plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md   (this contract; new)
dev/responses/P2_T02_CONTRACT_AUTHORING_RESPONSE.md              (authoring response; new)
plans/BETA_IMPLEMENTATION_MASTER_PLAN.md                         (P2-T02 status line only)
plans/beta-workstreams/P2-T02-DENSE-TABLE-AUDIT-TRAIL.md         (pointer to this contract only)
```

## 15. PLAN ACCEPT Gate

P2-T02 implementation is **not authorized** until all of the following hold:

1. this contract is committed and pushed to `DMO-MODULAR/main` and verifiable at the recorded SHA;
2. the Architect reviews **this contract** (not the handoff text) and returns **`PLAN ACCEPT`**
   (per `dmo-beta-master/WORKFLOW.md` steps 4–7);
3. the Architect's disposition of §12 Q1–Q3 is recorded in the review;
4. the implementation plan, recorded implementation SHA and implementation response then follow
   `WORKFLOW.md` steps 7–12 with their own Architect implementation review.

Until then:

```text
P2-T02 STATUS = CONTRACT AUTHORED — AWAITING ARCHITECT PLAN ACCEPT
```

No Razor page, component, partial, CSS, JS, test, route, registration or application change may be
made for P2-T02 before step 2. This document does not self-accept, does not mark P2-T02
`IMPLEMENTED`, and does not authorize P2-T03.
