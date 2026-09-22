# Shared Frontend Contract Freeze

## 1. Status, authority, and baseline

**A1 contract specification only. No frontend implementation is authorized by this document.**

This document freezes shared presentation behavior, interaction semantics, accessibility/focus expectations, and frontend-only carrier shapes for Workstreams B–E. It creates no Razor Pages, CSS, JavaScript, routes, tests, backend behavior, domain rules, persistence, schema, migrations, canonical identities, or API endpoint contracts. A2–A8 remain unauthorized.

Implementation preflight performed on 2026-09-22:

```text
repository:               https://github.com/diogo-o/DMO-MODULAR.git
branch:                   main
working tree:             clean
HEAD:                     6725657848028b86b404d16b655f8ef0f3b7ade5
origin/main:              6725657848028b86b404d16b655f8ef0f3b7ade5
remote refs/heads/main:   6725657848028b86b404d16b655f8ef0f3b7ade5
```

The inspected baseline is `6725657848028b86b404d16b655f8ef0f3b7ade5`. It includes the published P1-T04 Module Registry, access resolver/service, canonical Module vocabulary, and thin ASP.NET server-side Module gate. It contains no browser UI, Razor Pages, shared frontend assets, operational routes, or live industrial Module registrations.

Authority:

1. accepted Workstream A plan, `dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md`, commit `7efe8f8df909012b27f032592d9473b12b41bc3a` in `diogo-o/workbench`;
2. Architect review `PLAN ACCEPT — A1 AUTHORIZED ONLY`, `dev/reviews/BETA_FRONTEND_WORKSTREAM_A_PLAN_REVIEW.md`, commit `b8dbe6e9c0d3bdc08ae13d502588899863c78e95`;
3. accepted umbrella frontend workstream plan and Beta Design Reconciliation Plan;
4. `diogo-o/dmo-master`, branch `dmo-modular`, for functional/domain/access authority;
5. current committed `DMO-MODULAR/main` for implementation seams.

## 1A. Binding fixed desktop layout contract

This contract inherits the master plan's **DMO FIXED DESKTOP LAYOUT POLICY**.

- Canonical design and validation viewport: **1366 × 768**.
- Shared components preserve the same structural composition and action locations at larger
  desktop resolutions.
- Breakpoint-driven structural reflow, table-to-card conversion, required-column hiding,
  action relocation, side-panel stacking and alternate mobile/tablet navigation are prohibited.
- Smaller windows use page or local overflow; they do not trigger a different semantic layout.
- Grid/flexbox and reusable CSS remain valid implementation tools when used to preserve this
  fixed desktop composition.
- Shared surfaces remain compact enough for the real 768 px vertical constraint: no oversized
  headers, decorative card inflation, repeated titles or marketing-style whitespace.

For DenseDataTable, all required columns and their order remain stable; an over-wide table uses
a keyboard-reachable local horizontal scroll container. Descriptor "priority" is presentation
metadata only and is never authority to hide, reorder or convert a required column at a
breakpoint. AuditTrail follows the same fixed desktop composition. ToolPicker,
ToolSummaryRow, MeasurementRows, DecisionBar and ProductionContextStrip keep their consumer-
assigned structural region; width alone never moves them elsewhere.

## 2. Contract classifications

### FINAL PRESENTATION CONTRACT

This classification freezes consumer-visible behavior: responsibility, interactions/events, visible state distinctions, keyboard/focus rules, accessibility behavior, and explicit non-responsibilities. A change requires section 16's approval process.

### PROVISIONAL FRONTEND CONTRACT

This classification means exactly:

```text
frontend development/testing only
no backend authority
no persistence authority
no schema
no canonical IDs
no endpoint naming
no definitive request/response DTOs
no domain ownership
```

Carrier fields below are semantic slots, not mandatory C# property names or API fields. Opaque keys are consumer-controlled frontend carrier data. They must not be declared to be `tool_id`, `jobon_id`, or another canonical identity.

## 3. Rules shared by every component

These are **FINAL PRESENTATION CONTRACT** rules:

1. A owns presentation and generic interaction mechanics only.
2. Consumers supply facts, state, action availability, disabled reasons, and domain labels.
3. Components do not fetch, persist, authorize, infer domain facts, construct URLs, or execute domain transitions.
4. Missing data is never replaced with invented data; display text is never treated as identity.
5. A disabled control exposes a visible, programmatically associated supplied reason.
6. Status, warning, selection, error, and availability meaning is never color-only.
7. Async changes preserve context and avoid stealing focus unless a documented recovery flow requires it.
8. Pending actions prevent duplicate invocation while preserving accessible names and exposing progress.
9. Consumer-supplied callbacks, form targets, and navigation actions remain consumer-owned; A raises only the frozen generic event.

## 4. Common state vocabulary

This vocabulary is a **FINAL PRESENTATION CONTRACT**, not a backend error taxonomy. Consumers map real outcomes into these states without requiring matching backend codes.

| State | Semantic meaning | Visible behavior | Interaction allowed | Accessibility / announcement |
|---|---|---|---|---|
| `loading` | Initial/replacement information is being obtained. | Concise progress; retain stable context when possible; never appear empty. | Data-dependent actions unavailable; supplied cancel/unrelated navigation may remain. | Mark affected region busy and announce politely once. |
| `ready` | Required presentation information is available. | Normal content, selection, and supplied actions. | According to supplied availability. | Normal labelled landmarks, controls, and logical focus order. |
| `empty` | Request succeeded but returned no items/records. | Explicit no-results/no-records message and supplied next action. | Supplied search/filter/reset/create may remain. | Associate message with results region and announce updated result state politely. |
| `lookup-failed` | A requested lookup did not complete. | Error plus supplied retry; never show as empty/unavailable. | Retained safe context/retry may remain; dependent actions unavailable. | Assertive affected-region announcement without automatic focus theft. |
| `unavailable` | Required context/service is presently unavailable for a non-permission reason. | Supplied reason and next step; do not imply no records. | Dependent actions unavailable; unrelated navigation may remain. | Programmatically link reason to affected region/actions. |
| `permission-denied` | Published access decision denies the surface/action. | Explicit access message, never a blank surface. | Prohibited interaction unavailable; unrelated allowed navigation remains. | Identify unavailable function without exposing protected data; hiding is not enforcement. |
| `saving` | Generic save is in progress. | Stable content, pending label/indicator, retained entered values. | Duplicate save unavailable; consumer controls other safe actions. | Mark busy and announce start/completion without replacing accessible name. |
| `submitting` | Consumer-owned transition is in progress. | Consumer-supplied pending label distinct from saving. | Duplicate transition unavailable; no outcome inferred. | Mark busy; announce pending transition and supplied result. |
| `stale` | Consumer says shown information is older than its accepted source. | Warning plus supplied refresh/reload while retaining current view. | Mutations unavailable unless explicitly supplied safe. | Text plus icon/marker, never color only; keep focus stable. |
| `conflict` | Consumer reports a concurrency conflict. | Clear message and supplied recovery choices; retain entered work where possible. | No silent retry; only supplied recovery actions. | Assertive announcement; labelled recovery controls; relevant focus restoration. |

Not every component renders every state. A consumer must never map `lookup-failed`, `permission-denied`, or `conflict` to `empty`.

## 5. Access and navigation seam

### Published/final P1-T04 reality

These are published application/access inputs, not provisional frontend contracts:

- `IModuleAccessService`;
- `IModuleRegistry`;
- canonical Module vocabulary;
- `ModuleDefinition`;
- `ModuleSurfaceDescriptor`;
- stable `DestinationId` metadata;
- server-side Module policies and gate.

### Not-yet-live operational destinations

No industrial Module registrations or protected operational routes are live at the baseline:

```text
P1-T04 access contracts = published/final
industrial destination availability = not live until owning workstream registers its real Module/route
navigation fixtures = PROVISIONAL FRONTEND CONTRACT
```

Future navigation presentation groups granted, available definitions by stable `DestinationId`. Multiple assignable Modules may project onto one destination without merging authorization. Navigation visibility never substitutes for the server-side gate.

`Ferramentas` and `Ferramentas Approve` remain contextual-only and must never produce top-level navigation destinations.

A must not create permission enums, roles, profiles, job-title variants, access stores, Template resolution, or route authorization. Fixtures may simulate destinations only in frontend tests/demos and never register/grant a production Module.

## 6. `ProductionContextStrip`

### Purpose, owner, and consumers

Display supplied human-facing production context, visually separate from editable content.

- presentation owner: A;
- data/orchestration provider: B;
- consumers: B, C, D, E.

### FINAL PRESENTATION CONTRACT behavior

- visibly label Reference, Production, Machine, and Processo;
- optionally display distinct CM, MF, and BQ summaries when supplied;
- distinguish available, loading, lookup-failed, unavailable, and permission-denied states;
- keep primary facts stable during local feature work;
- never use a filesystem path or internal ID as the human-facing production identity;
- never lookup, derive, repair, associate, or persist production facts.

### Fixed desktop geometry

At 1366 × 768 the table uses compact row density, stable columns and predictable action
placement. The same column order and operational structure remain at larger desktop
resolutions. If the working region is too narrow, the table scrolls locally on the horizontal
axis; it never converts to cards, hides required columns or relocates actions by breakpoint.
AuditTrail retains the same structural region and compact desktop presentation.

### PROVISIONAL FRONTEND CONTRACT carrier shape and inputs

- common state;
- supplied display strings for reference, production number, machine, and process;
- optional supplied CM/MF/BQ summary items;
- optional detail/reason;
- optional consumer-supplied retry/action availability;
- accessible region label.

Any summary key is opaque frontend carrier data. No slot is a definitive API field.

### Outputs/events

- optional `retry requested`;
- optional supplied summary-action invocation.

The component produces no changed production context.

### States

- `loading`: labelled progress/placeholders; stale values are not presented as current;
- `ready`: all supplied required facts and optional summaries;
- `lookup-failed`: failure plus supplied retry, not an empty strip;
- `unavailable`: supplied reason;
- `permission-denied`: access outcome without leaked context;
- `empty` is not a ready context and is handled by the parent as no selection/unavailable.

### Keyboard/focus and accessibility

- strip is not in tab order unless supplied actions exist; actions follow reading order;
- async changes do not steal focus;
- use a labelled region/group; every value has a visible/programmatic label;
- common state announcements apply; absent optional CM/MF/BQ is not an error.

### Explicit non-responsibilities

No Job On/Tool lookup, CM/MF/BQ association, identity mapping, persistence, validation, route navigation, document naming, or authorization.

### Contract changes

A approves presentation changes; B approves adapter-seam changes. Breaking changes require A+B and compatibility review from C, D, and E.

## 7. `ToolPicker`

### Purpose, owner, and consumers

Reusable search/select/create presentation preserving origin orientation and unsaved state.

- presentation owner: A;
- search/create/association orchestration owner: B;
- consumers: B, C, E.

### FINAL PRESENTATION CONTRACT behavior

- explicit search state and candidate list;
- never auto-select a candidate;
- even one result requires explicit human selection;
- ambiguous candidates remain separate/selectable;
- no-results may expose `Criar ferramenta` only when supplied visible/enabled;
- preserve consumer-supplied origin state through search, create transition, cancel, and return;
- cancel returns to origin without selection;
- close/return restores focus to the invoking control;
- display supplied expected Tool type and origin context;
- A owns presentation only; B owns search, create, association, persistence, and return orchestration.

### PROVISIONAL FRONTEND CONTRACT carrier shape and inputs

- common state and controlled query;
- expected Tool type display label and origin-context display facts;
- candidate list: opaque candidate key plus supplied display facts;
- controlled explicitly selected opaque key, if any;
- create/cancel/retry availability and disabled reasons;
- opaque consumer-controlled origin-state token;
- invoking-control/focus-return reference through the eventual presentation mechanism.

Candidate/origin keys are opaque UI data, never declared `tool_id`, `jobon_id`, or another canonical ID.

### Outputs/events

- `search requested` with query;
- `candidate selected` with opaque candidate key;
- `create requested` with only supplied provisional prefill/origin carrier data;
- `cancel requested`;
- `retry requested`;
- `return requested`/closed after B completes its subflow.

Events do not create or associate a Tool.

### States

- `loading`: busy results; no implicit selection;
- `ready`: candidates and explicit selection controls;
- `empty`: no-results plus supplied create action when allowed;
- `lookup-failed`: failure/retry, never no-results;
- `unavailable`: supplied reason and cancel;
- `permission-denied`: supplied prohibited operations unavailable; cancel/return preserved;
- `saving`/`submitting`: B-owned create/association subflow; duplicate action unavailable;
- `stale`/`conflict`: only when supplied by B.

### Keyboard/focus and accessibility

- opening focuses heading/search according to accepted surface pattern;
- search, candidates, explicit select controls, create, and cancel are keyboard reachable;
- Enter on search requests search and never selects the first result;
- Enter/Space on an explicit candidate control selects that candidate;
- Escape requests cancel when safe; it never discards origin work without consumer confirmation;
- close/cancel/return restores invoking-control focus;
- surface has an accessible name; result count/state is announced politely;
- selected state is programmatic and non-color-only; facts are labelled; disabled reasons exposed.

### Explicit non-responsibilities

No search algorithm, compatibility/ranking rule, auto-selection, Tool creation, canonical identity, CM/MF/BQ association, persistence, endpoint, authorization, or domain validation.

### Contract changes

A approves presentation behavior; B approves orchestration-seam changes; C/E review affected changes. Requests to auto-select or define Tool semantics are outside A.

## 8. `ToolSummaryRow`

### Purpose, owner, and consumers

Compact generic rendering of supplied Tool facts.

- presentation owner: A;
- fact providers: B/owning feature adapters;
- consumers: B, C, E.

### FINAL PRESENTATION CONTRACT behavior

- render supplied type, reference, lot, machine/line compatibility, quantity, process, concise context, status, and actions;
- distinguish missing optional facts from zero/false;
- perform no lookup or Tool inference;
- action visibility/enabled state is supplied.

### PROVISIONAL FRONTEND CONTRACT carrier shape and inputs

- opaque item key;
- labelled display facts;
- common state and optional supplied `RecordStatus` input;
- generic action descriptors with visibility, enabled state, and disabled reason.

### Outputs/events

- `action invoked` with opaque item/action keys;
- optional `selected` when embedded in a selectable consumer surface.

### States

- `loading`: parent-owned; placeholder is not selectable;
- `ready`: supplied facts/actions;
- `unavailable`/`permission-denied`: supplied reason/actions without invented facts;
- row `lookup-failed` only when the consumer identifies a failed supplied item; otherwise parent-owned.

### Keyboard/focus and accessibility

- only interactive actions enter tab order, in reading order;
- use labelled cells/definition semantics; status is text; disabled reasons exposed;
- facts are not collapsed into an unlabeled string.

### Explicit non-responsibilities

No lookup, identity resolution, compatibility, mutation, selection policy, navigation construction, or authorization.

### Contract changes

A owns presentation; B approves Tool-fact carrier changes; C/E review affected fixture/use changes.

## 9. `DenseDataTable`

### Purpose, owner, and consumers

Dense accessible tabular presentation with controlled selection, opening, filtering, and pagination.

- presentation/interaction owner: A;
- data/action owners: consumers;
- consumers: B, C, D, E.

### FINAL PRESENTATION CONTRACT behavior

- one selected row where selection is enabled;
- single click selects;
- double click invokes consumer-supplied open action;
- Enter invokes consumer-supplied open action;
- Space selects without opening;
- explicit open action exists for pointer/keyboard parity;
- component never invents URL, route, or navigation;
- filter/page inputs are controlled by consumer;
- loading, empty, lookup-failed/error, selected, and disabled-action states are visible;
- keyboard focus is visible;
- selection uses a stable opaque consumer-supplied key.

### PROVISIONAL FRONTEND CONTRACT carrier shape and inputs

- column descriptors: heading, accessible label, display alignment/priority, consumer-rendered
  cell content; priority is not permission to hide, reorder or reflow a required column;
- rows with opaque keys and supplied display cells;
- controlled selected key;
- controlled filters, page index/size, supplied total/result metadata;
- generic row/table actions and disabled reasons;
- state, caption/label, supplied messages, optional sorting presentation.

Descriptors define no backend query, API DTO, entity, or canonical ID.

### Outputs/events

- `row selected` and `open requested` with opaque row key;
- `filter changed` with provisional filter carrier;
- `page changed` with requested page carrier;
- `action invoked` with opaque row/action keys;
- optional `sort requested` for consumer-supplied sortable columns.

### States

- `loading`: busy table/skeleton; retained rows must be labelled stale;
- `ready`: rows and controls;
- `empty`: headers/context plus explicit message and supplied next action;
- `lookup-failed`: failure/retry, never empty;
- `unavailable`/`permission-denied`: reason and allowed surrounding controls;
- `stale`: retained rows plus warning/refresh;
- `conflict`: supplied summary/recovery; table never resolves it.

### Keyboard/focus and accessibility

- visible focus on row/cell/action; Space selects; Enter/open control/double click request open;
- preserve focus on selected row after re-render when present;
- if filtering/paging removes it, focus moves predictably to result summary/table heading/first row;
- horizontal overflow is keyboard reachable and does not trap focus;
- semantic table, caption/name, header associations, programmatic/non-color selected state;
- announce result counts and state changes; row actions include row context; disabled reasons associated.

### Explicit non-responsibilities

No fetching, backend filtering/paging, URL construction, domain sorting, mutation, authorization, or automatic navigation.

### Contract changes

A owns interaction/presentation. Breaking changes require A and every affected B–E consumer, with coordinated fixtures and merge order.

## 10. `RecordStatus`

### Purpose, owner, and consumers

Render supplied status meaning as text plus semantic presentation.

- presentation owner: A;
- status vocabulary owner: consumer/domain owner;
- consumers: B, C, D, E.

### FINAL PRESENTATION CONTRACT behavior

- always show supplied status text;
- optional tone/icon/marker is supplementary; never color-only;
- unknown supplied status uses neutral presentation, never guessed semantics;
- status presentation grants no actions and infers no lifecycle.

### PROVISIONAL FRONTEND CONTRACT carrier shape and inputs

Status text, optional generic tone, optional icon/marker label, optional assistive description.

### Outputs/events

None. Adjacent actions belong to the consumer or `DecisionBar`.

### States

The status renders in ready content. Parent owns loading, empty, lookup-failed, unavailable, and permission states. Missing status is supplied as unavailable/unknown text, never inferred.

### Keyboard/focus and accessibility

Not focusable unless a separate interactive explanation is supplied. Text is always present; avoid live announcements unless a meaningful transition is supplied.

### Explicit non-responsibilities

No domain status catalogue, lifecycle transition, calculation, approval meaning, action availability, or authorization.

### Contract changes

A owns generic presentation; feature owners own status vocabulary. New domain statuses should use existing neutral/tone inputs without changing A.

## 11. `AvailabilityState`

### Purpose, owner, and consumers

Render document/lookup availability without collapsing materially different conditions.

- presentation owner: A;
- fact owner: supplying feature/backend adapter;
- consumers: B, C, D.

### FINAL PRESENTATION CONTRACT behavior

Keep these states distinct:

```text
available
not-generated
awaiting-approval
workspace-unavailable
file-missing
versions-available
not-applicable
lookup-failed
```

- every state has explicit text and semantic treatment;
- `not-applicable` is not an error;
- `lookup-failed` is not no-file/no-record;
- `file-missing` is not `not-generated`;
- actions/disabled reasons are consumer-supplied;
- component never checks storage or regenerates output.

### PROVISIONAL FRONTEND CONTRACT carrier shape and inputs

- one of eight presentation states;
- supplied label/detail;
- optional versions as opaque presentation items;
- generic actions with visible/enabled/disabled reason/pending state;
- accessible region label.

### Outputs/events

- `action invoked` with consumer action key;
- `version selected` with opaque version key;
- optional `retry requested` for lookup failure.

### States

- `available`: supplied open/download may be enabled;
- `not-generated`: explain absence; generation exists only if supplied;
- `awaiting-approval`: pending approval plus supplied allowed actions;
- `workspace-unavailable`: workspace reason, no invented fallback;
- `file-missing`: missing derived file without denying owning record;
- `versions-available`: explicit supplied choice/action;
- `not-applicable`: neutral explanation, no error styling;
- `lookup-failed`: failure/retry, not another availability state.

Common `loading`, `permission-denied`, `saving`, `submitting`, `stale`, and `conflict` may wrap but never replace the eight outcomes.

### Keyboard/focus and accessibility

Only actions/choices enter tab order; version/action order is predictable; refresh does not steal focus. State text is visible, icon/tone supplementary, and disabled reasons associated.

### Explicit non-responsibilities

No filesystem/storage lookup, record-existence inference, generation, approval, version definition, path exposure, or authorization.

### Contract changes

A owns rendering/distinctions. B/C/D approve any merge/removal/reinterpretation; feature owners control mapped facts.

## 12. `AuditTrail`

### Purpose, owner, and consumers

Generic chronological/history rendering of supplied attributed facts.

- presentation owner: A;
- fact owner: supplying feature/backend record;
- consumers: B, C, D, E.

### FINAL PRESENTATION CONTRACT behavior

- render only supplied actor, timestamp, action, and optional before/after detail;
- preserve supplied ordering or explicitly supplied display order;
- never synthesize attribution from current session;
- never replace missing historical actor/time with current user/time;
- show unavailable attribution explicitly when supplied.

### PROVISIONAL FRONTEND CONTRACT carrier shape and inputs

- common state and ordered entries;
- opaque entry key;
- actor display fact, timestamp display/semantic value, action text;
- optional before/after display details and generic detail action;
- optional parent-controlled filter/page context.

### Outputs/events

- optional `detail requested` with opaque entry key;
- optional parent-owned filter/page events.

### States

- `loading`: busy labelled history region;
- `ready`: supplied entries;
- `empty`: explicit no-history, not failure;
- `lookup-failed`: failure/retry;
- `unavailable`/`permission-denied`: reason without partial protected facts;
- `stale`: retained history plus warning;
- `conflict`, `saving`, `submitting` are normally parent-owned and never alter history.

### Keyboard/focus and accessibility

Entries focus only when containing supplied actions; closing detail restores invoking focus. Use list/table/timeline semantics with labelled actor, timestamp, action, before, after; timestamps are human-readable; change meaning is not color-only.

### Explicit non-responsibilities

No audit creation, actor resolution, timestamp generation, diff calculation, persistence, ordering inference, correction workflow, or authorization.

### Contract changes

A owns rendering; feature owners own fact semantics. Breaking shapes require A and every affected B–E consumer.

## 13. `MeasurementRows`

### Purpose, owner, and consumers

Generic repeated-row editing mechanics without measurement fields/rules.

- generic presentation/mechanics owner: A;
- domain schema, formulas, meaning, and validation owner: C;
- consumer: C.

### FINAL PRESENTATION CONTRACT behavior

- stable frontend row identity across add/remove/re-render;
- generic add/remove;
- configurable minimum rows, including at-least-one mode;
- removal disabled with visible reason when minimum would be violated;
- consumer-supplied validation display hooks;
- deterministic keyboard/focus mechanics;
- no fixed old-design row cardinality.

### PROVISIONAL FRONTEND CONTRACT carrier shape and inputs

- opaque UI-only row key;
- controlled ordered rows and consumer-rendered row fields/template;
- minimum count;
- add/remove availability/reasons;
- consumer-supplied field/row validation display;
- parent saving/submitting state and labels.

The row key is frontend-only, never a canonical measurement/database identity.

### Outputs/events

- `add requested`;
- `remove requested` with opaque row key;
- generic `row value changed`/validation hook through eventual consumer binding;
- no calculation/persistence event from A.

### States

- `ready`: editing according to supplied availability;
- `empty`: valid only when minimum is zero, with supplied add affordance;
- `saving`/`submitting`: retain values and prevent duplicate structural mutation as supplied;
- `permission-denied`/`unavailable`: supplied read-only/disabled reason;
- `conflict`/`stale`: retain rows and show parent recovery/warning;
- domain-data lookup states are parent-owned.

### Keyboard/focus and accessibility

- logical row-major order;
- after add, focus first editable control in new row;
- after remove, focus nearest surviving corresponding control or add control;
- remove identifies row context; structure changes never reset focus to page start;
- each row has supplied accessible context; repeated fields have unique labels; errors associated/summarized; minimum disabled reason exposed; add/remove announced politely.

### Explicit non-responsibilities

No domain row schema, measurement meaning, formulas, tolerances, nominal values, validation rules, calculations, canonical identity, persistence, or submission. C owns all.

### Contract changes

A+C jointly approve: A controls generic mechanics/accessibility; C controls domain integration. No feature rules may be added to A.

## 14. `DecisionBar`

### Purpose, owner, and consumers

Consistent generic presentation of consumer-supplied actions.

- presentation owner: A;
- action/transition owners: consumers;
- consumers: C, D, E.

### FINAL PRESENTATION CONTRACT behavior

- primary, secondary, and danger action groups;
- visible disabled reason;
- pending/saving/submitting presentation;
- duplicate invocation prevented while supplied action is pending;
- consumers own action, label, availability, and semantic consequence;
- A encodes no approve, reject, reopen, submit, close, or movement rule.

### PROVISIONAL FRONTEND CONTRACT carrier shape and inputs

- generic action descriptors: opaque action key, label, primary/secondary/danger group, visible, enabled, disabled reason, optional pending label;
- controlled common state and pending action key;
- optional supplied status/help text and region label.

### Outputs/events

- `action invoked` with opaque consumer action key.

No transition result/domain event is defined by A.

### States

- `ready`: supplied availability;
- `saving`/`submitting`: pending label and duplicate action unavailable;
- `permission-denied`: action omitted/disabled per published decision, never merely hidden as enforcement;
- `unavailable`: reason and permitted alternatives;
- `stale`/`conflict`: mutations unavailable unless explicitly supplied safe; recovery may remain;
- `loading`/`empty`/`lookup-failed`: parent may disable bar with supplied reason.

### Keyboard/focus and accessibility

- logical visual action order and native activation semantics;
- focus remains on pending action while present;
- failure focus follows consumer error-summary rules;
- consumer-owned confirmation restores invoking focus;
- labelled region; danger includes text/semantics; disabled reasons associated; pending announced while accessible name remains.

### Explicit non-responsibilities

No approval/rejection/reopen rules, transition ordering, confirmation policy, authorization decision, persistence, retry policy, or domain validation.

### Contract changes

A owns generic presentation; C/D/E own actions. A plus every affected consumer approves breaking changes. Feature semantics never move into A.

## 15. Ownership and integration summary

| Contract | A responsibility | Provider/consumer responsibility | Consumers |
|---|---|---|---|
| `ProductionContextStrip` | presentation/state/accessibility | B supplies production adapter/facts | B, C, D, E |
| `ToolPicker` | picker presentation/focus/events | B owns search/create/association | B, C, E |
| `ToolSummaryRow` | compact supplied-fact presentation | B/feature adapter supplies facts/actions | B, C, E |
| `DenseDataTable` | selection/open/filter/page mechanics | consumer owns data, query, target, actions | B, C, D, E |
| `RecordStatus` | textual/semantic presentation | consumer owns domain status vocabulary | B, C, D, E |
| `AvailabilityState` | distinct availability presentation | B/C/D map authoritative facts/actions | B, C, D |
| `AuditTrail` | supplied-fact rendering | consumer owns audit facts | B, C, D, E |
| `MeasurementRows` | generic repeated-row mechanics | C owns schema, meaning, formulas, validation | C |
| `DecisionBar` | generic action presentation | C/D/E own actions/transitions | C, D, E |
| navigation presentation | future rendering of accepted access output | P1-T04 owns registry/access/gate; feature owners register Modules/routes | A, Admin, B–E |

## 16. Contract-change protocol

No consumer may silently edit or fork a shared contract. A proposal states:

1. current element;
2. requested change/reason;
3. affected consumers;
4. final/provisional classification;
5. compatibility impact;
6. fixture/test updates;
7. merge order.

Approval:

- A approves every shared presentation/accessibility change;
- the named provider/consumer owner approves its adapter seam;
- every affected consumer approves a breaking change;
- integration lead resolves incompatible cross-stream requests;
- backend/domain/API/persistence changes require a separate authorized task.

Consumers may develop against accepted provisional fixtures after freeze acceptance. They must not create private look-alike components. When a real feature/backend contract is published, the consumer adapts at its boundary and retires the fixture; the backend is never changed merely to fit a fixture.

## 17. A1 stop boundary

This freeze does not create or authorize:

- Razor Pages or other UI pages;
- CSS, JavaScript, components, or static assets;
- `Program.cs`/project-file changes or packages;
- routes or industrial Module registrations;
- tests;
- auth/access changes;
- backend/domain changes;
- persistence, database, or migrations;
- A2–A8 work.

The next action after publication is Architect review of this A1 freeze. Workstream A stops until explicit next-stage authorization.
