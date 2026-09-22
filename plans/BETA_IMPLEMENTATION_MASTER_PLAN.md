# BETA_IMPLEMENTATION_MASTER_PLAN

Implementation sequencing authority for the remaining DMO Beta work.

This document is planning only. It changes no application code, no test, no schema, no
migration, no route and no configuration. It exists so that a later implementation agent can
work through the remaining Beta scope **without re-auditing the repositories, rediscovering
authority, guessing architecture, inventing module behavior, reopening settled questions or
duplicating completed work**.

Authority order used throughout:

1. `reports/BETA_MASTER_RECONCILIATION.md` — current-state and delta authority (this repo).
2. `diogo-o/dmo-beta-master` @ `main` — Beta functional/scope/workflow/acceptance authority.
3. `diogo-o/dmo-master` @ `dmo-modular` — global DMO architecture, identities, access model
   and module vocabulary that the Beta may not contradict.
4. Current `diogo-o/DMO-MODULAR` @ `main` — implementation state and integration seams.
5. `diogo-o/workbench` / `diogo-o/dmo-work` — historical planning/execution evidence only.

> **Environment note.** This planning run was produced in an environment without a runnable
> .NET SDK. **No build or test was executed and no build/test success is claimed.** All test
> requirements in this document are *specifications to be executed later*.

---

## DMO FIXED DESKTOP LAYOUT POLICY (binding)

**Product decision:** DMO is a fixed-layout desktop operational application, not a responsive
public website.

### Canonical design surface

- The canonical viewport is **1366 × 768**.
- Every remaining operational frontend surface is designed and validated at 1366 × 768 first.
- The fixed desktop structure preserves the structural location of navigation, primary and
  secondary actions, tables, filters, side panels, status information, history access, document
  actions and all other operational controls.
- A control must not move to another structural region merely because viewport width changes.

### Structural reflow is prohibited

Do not introduce breakpoint-driven semantic or structural variants. In particular, do not:

- move buttons or actions between headers, footers, table rows, side panels, dropdowns or
  overflow menus because of viewport width;
- turn horizontal groups into vertical stacks at a breakpoint;
- convert tables into cards or hide/reorder required columns at a breakpoint;
- move a side panel below the main work area;
- replace desktop navigation with a hamburger or alternate mobile/tablet navigation;
- change workflow order, action order or component meaning according to screen width.

Mobile, tablet, touch-first and phone-specific layouts are **out of scope** unless a later
explicit authority decision replaces this policy.

### Larger and smaller viewports

Larger desktop resolutions, including 1920 × 1080 and 2560 × 1440, preserve the same operational
composition and control locations. Additional space may become outer whitespace, larger margins
or limited non-structural expansion of a content region; it must not reinterpret the workflow.

When the available window is smaller than the canonical surface, preserve the composition and
use page-level scrolling or local horizontal/vertical scrolling. Scrolling is preferable to
structural reflow. Wide tables use local horizontal scrolling while retaining stable columns,
column order, compact rows and predictable row actions.

### Implementation constraints

Fixed layout does not require arbitrary absolute positioning. Grid, flexbox, reusable CSS,
controlled min/max sizing and overflow containers remain allowed when they preserve the fixed
composition. Navigation and side-panel widths, grid columns and action regions may be explicitly
controlled. A side panel stays beside its main work area; if the composition exceeds the
viewport, the containing region scrolls instead of moving the panel below it.

The 768 px canonical height is a real constraint. Avoid oversized headers, decorative cards,
marketing whitespace, repeated titles, excessive vertical padding/margins and unnecessary
stacking. Keep working data and important actions visible with compact operational density where
practical.

Design priorities are binding in this order:

1. workflow stability;
2. predictable control placement;
3. information density;
4. readability;
5. consistency between modules;
6. visual polish.

This policy binds the shared frontend contract and P2-T02 through P2-T10, including
DenseDataTable, AuditTrail, ToolPicker, ToolSummaryRow, MeasurementRows, DecisionBar,
ProductionContextStrip, Tool/Job On, Controlo Create, Controlo Approve, Boquilhas,
document/history surfaces, secondary navigation and final integration. It changes no domain
behavior, data contract, authorization, route, module availability or completed P2-T01
implementation.

---

## 0. Terminology (binding for all remaining Beta work)

Two different concepts must never be conflated in any current or future Beta document,
workstream or implementation.

### 0.1 HISTÓRICO (local / module-specific)

Cross-cutting history capability that exists **inside** an individual operational module:
Peso → Histórico, Job On → Histórico, Boquilhas → Histórico, Armazém → Histórico,
Reparações → Histórico.

It means "show the records/actions/work already performed within this module or operational
context". It is **not** a standalone module identity and **not** a top-level destination. Each
module owns and exposes its own Histórico view/filter/page according to its own authority.

In this plan, HISTÓRICO appears as: the Boquilhas **History** requirement inside **P2-T07**;
Create-side history/document-availability views inside **P2-T05**; attributed decision/reopen
history inside **P2-T06**; and the (out-of-Beta) histories of Armazém/Reparações belonging to
their own future workstreams.

Authority evidence for HISTÓRICO (local):
- `dmo-beta-master/modules/BOQUILHAS.md` §"History" and its acceptance criteria ("full History
  filters/table", "close/reopen retains the same `boquilhas_id` and full history").
- `dmo-beta-master/modules/CONTROLO_CREATE.md` §"Included in Beta" (Create-side history views).
- `dmo-beta-master/modules/CONTROLO_APPROVE.md` ("attributed decision/reopen history").
- `dmo-beta-master/modules/JOB_ON_LIGHT.md` ("Reference History search/open tests").

### 0.2 HISTÓRICO GLOBAL (top-level aggregating module)

A **distinct top-level module/destination** whose responsibility is to aggregate and provide
access to historical information across the operational system (Peso history, Job On history,
Tool history, Boquilhas history, Armazém history, repair history, other authority-backed
operational histories). It does **not** replace the local Histórico functionality inside
individual modules; it aggregates them.

Current technical identity: **`historia`** (`ModuleCatalog.Historia`, display label
"História", destination `historia`). Per the naming rule: do **not** rename code identities
during Beta planning or Beta implementation. Record that `historia` is the current technical
identity and that its intended user-facing meaning maps to HISTÓRICO GLOBAL; treat any technical
rename as a separate, later task that must not break existing IDs or catalog relationships.

Provisional display label: if HISTÓRICO GLOBAL ever reaches implementation, its user-facing
label becomes **HISTÓRICO GLOBAL** rather than the current "História". The catalogue entry in
the current build already carries that concept under the technical identity `historia`.

Authority evidence for HISTÓRICO GLOBAL:
- `dmo-master/global/ACCESS_MODEL.md` §1 — the 11th canonical assignable module with its own
  visible destination; `dmo-master/global/INFORMATION_MODEL.md` — the read-only relationship
  explorer role (the aggregating Information Web explorer).
- **No** HISTÓRICO GLOBAL evidence exists in `dmo-beta-master`: it is absent from
  `BETA_SCOPE.md`'s operational module list, from `architecture/ACCESS_AND_NAVIGATION.md`'s
  access vocabulary, and from `IMPLEMENTATION_MODEL.md`'s workstreams A–E. There is no
  `modules/HISTORIA*.md` file in `dmo-beta-master`.

### 0.3 The rule that follows

- Evidence for **HISTÓRICO (local)** is **never** authority for **HISTÓRICO GLOBAL**.
- Absence of **HISTÓRICO GLOBAL** from Beta is **not** absence of **HISTÓRICO (local)** inside a
  Beta module.
- The two are never merged.
- **HISTÓRICO GLOBAL is DEFERRED BY DESIGN for this Beta** (see §3.1 and §14.1): canonical
  identity preserved, no route, no availability, not merged into Admin, not renamed in code.


## 1. Baseline

| Item | Value |
|---|---|
| `diogo-o/DMO-MODULAR` remote `main` SHA (verified this run) | `7910f56d5b920f3b79aee027c8399823f6fdddfb` |
| DMO-MODULAR advanced since reconciliation commit? | **NO** — remote `main` is still exactly the reconciliation commit `7910f56…` |
| `diogo-o/dmo-beta-master` remote `main` SHA | `78da49248f6cf7a8cbe4ddd946f3c38abbaf322f` |
| Reconciliation report present | YES — `reports/BETA_MASTER_RECONCILIATION.md` |
| Reconciliation authority commit | `7910f56d5b920f3b79aee027c8399823f6fdddfb` |
| Supporting authority: `dmo-master` @ `dmo-modular` | `ae2a9b9d12132ee4b41dc0696f34c6439b5cca52` |
| Supporting provenance: `workbench` @ `main` | `50edc6a6be1584f75b2ad48233503a45e049d841` |
| Supporting provenance: `dmo-work` @ `main` | `ef4daeb1e6421cc17b19caec2c2f027a872b52d0` |
| Working tree at start | CLEAN |
| Application code modified by this task | **NO** |

Because DMO-MODULAR has **not** advanced, no post-reconciliation change inspection was
required and no reconciliation conclusion was invalidated.

Baseline test counts recorded by the previous run (evidence only; not re-executed here):
`DMO.UnitTests` 335 passed / 0 failed / 0 skipped; `DMO.IntegrationTests` 102 passed /
71 skipped / 0 failed (the 71 skips are environment-gated PostgreSQL/Supabase-live cases).

---

## 2. Settled Foundation

Everything below is classified **COMPLETE — PRESERVE** by the reconciliation and is treated as
settled. It must not be rebuilt, replaced, refactored or reinterpreted. §12 is the enforceable
register.

| Foundation area | Implementation anchor |
|---|---|
| Canonical 13-module vocabulary | `src/DMO.Application/Access/ModuleCatalog.cs` |
| Registry validation semantics | `src/DMO.Application/Access/ModuleRegistry.cs` |
| Fail-closed whole-resolution access | `src/DMO.Application/Access/AccessResolver.cs`, `AccessOutcome.cs` |
| Per-module access facade | `src/DMO.Application/Access/ModuleAccessService.cs` |
| Server-side module gate (13 policies) | `src/DMO.Web/Authorization/ModuleAuthorizationPolicies.cs`, `ModuleAuthorizationHandler.cs` |
| ADMIN-only administration gate | `src/DMO.Web/Authorization/AdministrationAuthorizationPolicies.cs`, `AdminAuthorizationHandler.cs` |
| Authentication boundary + login orchestration | `src/DMO.Web/Auth/SupabaseAuthenticationService.cs`, `SessionLoginService.cs`, `SessionAuthentication.cs` |
| Account resolution (ADMIN/USER, no role grants) | `src/DMO.Application/Accounts/AccountResolver.cs`, `AccountType.cs`, `NoAccessReason.cs` |
| Current-account boundary | `src/DMO.Application/Session/CurrentAccount.cs`, `src/DMO.Web/Auth/CurrentAccountContext.cs` |
| Persistence foundation + migrations 001/002 | `src/DMO.Infrastructure/Migrations/20260922001736_*`, `…1757_*` |
| Template model (`users.template_id`, composition, order, landing) | `src/DMO.Application/Templates/*`, `TemplateAdministration/*` |
| USER administration | `src/DMO.Application/UserAdministration/*`, `Pages/Administration/Users/*` |
| Template administration | `src/DMO.Application/TemplateAdministration/*`, `Pages/Administration/Templates/*` |
| Root routing / landing / no-access | `src/DMO.Web/Pages/Index.cshtml.cs`, `src/DMO.Web/Navigation/{LandingSelector,UserLandingService}.cs`, `Pages/AccessDenied.cshtml.cs` |
| Navigation projection + shared-destination collapse | `src/DMO.Web/Frontend/Shell/NavigationProjectionService.cs` |
| Route registration seam (honestly empty) | `src/DMO.Web/Frontend/Shell/DestinationRoutes.cs`, `src/DMO.Web/Navigation/DestinationRouteRegistrations.cs` |
| Shared shell presentation | `src/DMO.Web/Frontend/Shell/{ShellPresentationModels,ShellPresentationService}.cs`, `Pages/Shared/*`, `wwwroot/css/dmo-{tokens,shell,user-shell}.css` |
| A1 frozen shared frontend contract | `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` |
| Honest build availability (empty) | `src/DMO.Application/Access/ModuleRegistrations.cs` |

**Standing invariant carried into every workstream:** `ModuleRegistrations.CurrentBuildAvailable`
stays `[]` and `EmptyDestinationRouteRegistry` stays the production registration **until a real
operational destination is implemented and usable**.

---

## 3. Closed Reconciliation Questions

Every ambiguity recorded in `reports/BETA_MASTER_RECONCILIATION.md` §11 (plus the §10 wiring
ambiguities and the §8 documentation divergences) is dispositioned below.

### 3.1 Q1 — HISTÓRICO GLOBAL scope (was recorded as "História") → RESOLVED / DEFERRED BY DESIGN

> **Terminology (binding, see §0).** The topic formerly recorded as the "História" module is
> **HISTÓRICO GLOBAL** — the distinct top-level aggregating module whose current technical
> identity is `historia`. It is **not** the local HISTÓRICO functionality inside individual
> modules (Peso/Job On/Boquilhas/Armazém/Reparações histories). Evidence for one is never
> authority for the other, and neither is inferred from the other's absence.

**Exact question.** Is HISTÓRICO GLOBAL (a) part of Beta operational scope, (b) a global
assignable module only, (c) entitled to a distinct route, and (d) in-scope for this Beta build?

**Authority inspected.**
- `dmo-master` @ `dmo-modular` `global/ACCESS_MODEL.md` §1 — HISTÓRICO GLOBAL is the **11th
  canonical assignable module**, with its own visible destination (label "História").
- `dmo-master` @ `dmo-modular` `modules/HISTORIA.md` — "Architect status: **current — visible,
  independently assignable, read-only Information Web explorer**"; "owns no operational facts
  and writes no operational records"; read-only grant; no mutation endpoints; **"Future
  implementation: No future implementation is currently registered for this module."** Its
  §4 "Canonical entry points" traverse `ri_session_id/ri_item_id`, `tool_movement_id`,
  `programmed_repair_id` — identities belonging to modules that are **outside** Beta scope.
  Its §1 defines it as the read-only **relationship explorer** over canonical records — i.e.
  the aggregating role that distinguishes HISTÓRICO GLOBAL from local Histórico.
- `dmo-master/global/INFORMATION_MODEL.md` — the read-only Information Web explorer role.
- `dmo-beta-master` `BETA_SCOPE.md` — "Operational Beta modules" lists only Job On Light,
  Ferramentas Light, Controlo Create, Controlo Approve, Boquilhas. The "Explicitly not implied"
  list excludes Armazém, Reparação Interna/Externa/Programada, Tampões, "future modules not
  explicitly listed here".
- `dmo-beta-master` `architecture/ACCESS_AND_NAVIGATION.md` §"Access vocabulary" — "Relevant
  Beta operational modules include:" lists only Job On View/Create, Controlo Create/Approve,
  Boquilhas, Ferramentas, Ferramentas Approve.
- `dmo-beta-master` `INDEX.md` module list and `IMPLEMENTATION_MODEL.md` workstreams A–E — **no
  HISTÓRICO GLOBAL workstream**; no `modules/HISTORIA*.md` file exists in `dmo-beta-master`.
- **Local HISTÓRICO evidence exists and is separate:** `modules/BOQUILHAS.md` §"History" and
  acceptance criteria; `modules/CONTROLO_CREATE.md` "Create-side history/document availability
  views"; `modules/CONTROLO_APPROVE.md` "attributed decision/reopen history";
  `modules/JOB_ON_LIGHT.md` "Reference History search/open tests". This evidence is authority
  for **HISTÓRICO inside those modules** and is treated as such (see §7 P2-T05/P2-T06/P2-T07).
  It is **not** authority for HISTÓRICO GLOBAL.

**Resolution.**
1. HISTÓRICO GLOBAL **is** a canonical global assignable module (`ModuleCatalog.Historia`
   already registers the identity `historia` and destination `historia` — correct and to be
   preserved). Its intended user-facing meaning maps to HISTÓRICO GLOBAL; the technical identity
   `historia` is **not** renamed during this planning or the Beta implementation (§0.2).
2. HISTÓRICO GLOBAL is **not** a Beta operational module: it is absent from the Beta operational
   module lists and has no Beta workstream, no Beta module contract and no Beta acceptance
   criteria.
3. Therefore a HISTÓRICO GLOBAL **route/surface must not be invented in this Beta build**, and
   no module availability may be added for it.
4. Its data surface depends on RI, Armazém and Reparação Programada identities that are
   themselves outside Beta scope, so a Beta HISTÓRICO GLOBAL would be a partial, misleading
   explorer. `dmo-master/modules/HISTORIA.md` §11 records its own implementation as OPEN.
5. **This decision does not reduce any Beta module's local HISTÓRICO requirement.** The
   per-module histories remain in scope through their owning workstreams (P2-T05, P2-T06, P2-T07).

**Disposition:** **DEFERRED BY DESIGN** (post-Beta; global module, not a Beta deliverable).
**Preserved state:** `historia` identity in the catalog (do not delete); no route; not
available; not merged into Admin; not renamed in code. **Do not implement.**

### 3.2 Q2 — `BETA_DESIGN_RECONCILIATION_PLAN.md` unavailable → RESOLVED / NON-BLOCKING

**Exact question.** Does the missing "settled frontend design authority" block the Beta
frontend shared-component work?

**Evidence.** `workbench/BETA_FRONTEND_IMPLEMENTATION_WORKSTREAMS.md` line 9 and
`workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md` lines 14 and 77 name
`D:\workbench\BETA_DESIGN_RECONCILIATION_PLAN.md` as authority/untracked local file. Searches
of `workbench/main` (51 commits of history, `git log --all -- "*BETA_DESIGN_RECONCILIATION*"`
returns nothing) and `dmo-beta-master/main` found no such file. It was a **local, untracked**
artifact (`D:\…`), never committed.

**Resolution.** The A1 freeze (`docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md`) was itself
Architect-accepted (A2 correction review: "A2 CORRECTION ACCEPT — A3 AUTHORIZED ONLY") and is
**canonicalized** into `dmo-beta-master/contracts/SHARED_FRONTEND.md`. The canonical Beta
contract therefore exists without the missing plan. Its absence does not block A3–A6
(component) work.

**Disposition:** **RESOLVED — NON-BLOCKING.** No workstream may depend on the missing file.
Recorded as an accepted, recoverable provenance gap only. Do **not** attempt to reconstruct or
invent it.

### 3.3 Q3 — P1-T07 acceptance-status gap → RESOLVED / READY AS GOVERNANCE GATE

**Exact question.** Is the P1-T07 implementation (remote `main` HEAD) accepted foundation?

**Evidence.** `dmo-beta-master/SOURCE_MANIFEST.md` and
`implementation/CURRENT_FOUNDATION.md` both state: implementation commit `0b476909…` is
`IMPLEMENTED / NOT YET ACCEPTED`; no Architect implementation review exists at
`dmo-work/dev/reviews/P1-T07_NAVIGATION_USER_SHELL_IMPLEMENTATION_REVIEW.md`. Verified again
this run: `dmo-work/dev/reviews/` contains only
`P1-T07_NAVIGATION_USER_SHELL_PLAN_REVIEW.md` and `…_CORRECTED_PLAN_REVIEW.md` — the
implementation review is still **absent**. The P1-T07 implementation response
(`dmo-work/dev/responses/P1-T07_NAVIGATION_USER_SHELL_IMPLEMENTATION_RESPONSE.md`) records the
behavior and is useful evidence, but is explicitly *not* acceptance.

**Resolution.** There is **no missing application behavior** to implement. The gap is purely a
governance/acceptance record. Per Beta `WORKFLOW.md` ("Acceptance requires inspection of the
actual implementation commit"; "Dependent work may rely only on accepted results"), the correct
closure is an Architect implementation review, not code.

**Disposition:** **READY (governance)** — see workstream **P2-T00**. It is a *gate*, not a
feature: dependent Beta feature work may *start* against the current code state (as the
reconciliation already does), but should not claim P1-T07 as "accepted foundation" until the
review is recorded. No P1-T07 shell behavior may be rebuilt.

### 3.4 Q4 — Canonical module ordering → RESOLVED / ALREADY SATISFIED

**Exact question.** Is there an ordering conflict between the global access model and the
current catalog?

**Evidence.** `dmo-master/global/ACCESS_MODEL.md` §1 lists the 13 modules in one canonical
order; `ModuleCatalog.cs` reproduces exactly that set and order;
`implementation/CURRENT_FOUNDATION.md` states "exactly 13 canonical code-owned Module
identities". Both the code and the docs state that catalog order is presentation/audit ordering
and **never** an authorization factor; effective navigation order is the persisted Template
`presentation_order`.

**Disposition:** **ALREADY SATISFIED.** No conflict; no work. Recorded so no future agent
reopens it.

### 3.5 Q5 — Peso status vocabulary → RESOLVED / READY FOR IMPLEMENTATION

**Exact question.** Which Peso approval vocabulary and formula are authoritative?

**Evidence.** `dmo-beta-master/architecture/RECORD_LIFECYCLES.md` §4 fixes the status
vocabulary as exactly `Pendente`, `Aprovado`, `Não aprovado`, with Comparação as a
record type/workflow relation (not a status) and "stale" as a workflow condition (not a fourth
status). `dmo-beta-master/modules/CONTROLO_CREATE.md` §"Calculation ownership" fixes the
formulas:

```text
Capacidade / Volume do CM = Peso de água ÷ configured water-temperature value
Peso do vidro = (Capacidade do CM + Volume Marisa/BQ − Volume Punção/PU) × Densidade do vidro
```

with canonical water-temperature range 5–35 °C, individual CM results first-class (never hidden
by an average), variable rows with at least one valid row, and decimals presentation-normalized
without reducing calculation precision.

`dmo-master/modules/CONTROLO.md` describes the same lifecycle without a competing literal set —
it narrows, it does not contradict. Per `AUTHORITY.md` the Beta may simplify but not contradict
global authority.

**Disposition:** **RESOLVED.** Beta defines the vocabulary and formulas unambiguously. This is a
**READY FOR IMPLEMENTATION** requirement of workstreams P2-T05/P2-T06 — not an ambiguity, and
not something to invent.

### 3.6 Section-10 wiring ambiguities → RESOLVED

- **10.1 Admin index logout form.** `POST /auth/logout` is accepted foundation (P1-T05 pattern,
  reused in `Pages/AccessDenied.cshtml`). Only the *placement* of the control on
  `Administration/Index.cshtml` is not Beta-established.
  **Disposition: DO NOT IMPLEMENT** — no change. Removing or relocating it is not required by
  any authority. Ambiguity resolved as "no action".
- **10.2 Hardcoded shell status strings.** The fail-closed string is required by the accepted A2
  correction (`workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A2_CORRECTION_PLAN.md` §6.2 test 10
  and its accepted review). `"Aplicação pronta."` is an unrecorded but harmless shared-shell
  detail.
  **Disposition: DO NOT IMPLEMENT** — no change; it is accepted shared-shell presentation.
- **10.3 Unused shell CSS/slots** (`.dmo-production-slot`, `.dmo-work-surface`,
  `.dmo-section-heading`, `.dmo-action-placeholder`).
  **Disposition:** these are builders for workstream **P2-T01/P2-T03** presentation
  (section headings, status/action regions). Not unauthorized — **DEFERRED BY DESIGN** into the
  component workstreams that will consume them. Do not delete.

### 3.7 Section-8 documentation divergences → RESOLVED / ALREADY SATISFIED as planning

The three stale READMEs (`src/DMO.Application/README.md`, `src/DMO.Infrastructure/README.md`,
`src/DMO.Web/README.md`) contradict the implemented P1-T03…P1-T07 state. They are
documentation-only and stale; the reconciliation already identified them as non-authoritative
("Do not treat stale documentation as current architecture authority").

**Disposition:** **READY FOR IMPLEMENTATION** as a trivial documentation-only slice — folded
into workstream **P2-T01** (it shares the same ownership/verification boundary). Not a
structural obstacle, and explicitly **not** a reason to inspect them as architecture.

**Cumulative ambiguity closure:** 7 of 7 questions dispositioned; **0 remain BLOCKED BY
AUTHORITY** for the planning horizon.

---

## 4. Remaining Authority Blocks

**None block Phase 2.** The items below are *intra-workstream design decisions* that require an
authorized contract before code is written — exactly what Beta `WORKFLOW.md` calls the
plan-gate and `BACKEND / INTERFACE BLOCKER` protocol. They are not repository contradictions,
and they must not be resolved by invention.

| Ref | Nature | What is absent | Blocking workstream | Required artefact |
|---|---|---|---|---|
| B1 | Backend/interface contract | Concrete Tool/Job On/CM/MF/BQ query + mutation contract and physical schema have not been published as an accepted contract | P2-T04+ (execution only) | Authored, reviewed `PLAN ACCEPT` for P2-T04 per Beta `WORKFLOW.md` |
| B2 | Backend/interface contract | Concrete Peso/Pegamentos/Folha/Resumo calculate/persist/submit/read contract | P2-T05/P2-T06 (execution only) | Authored, reviewed `PLAN ACCEPT` for P2-T05 |
| B3 | Backend/interface contract | Concrete Boquilhas aggregate/movement/edit-audit/close-reopen contract and canonical `repairer_id` directory source | P2-T07 (execution only) | Authored, reviewed `PLAN ACCEPT` for P2-T07 |
| B4 | Backend/interface contract | Document generation contract + directory/filesystem capability (`Infrastructure/Files`, `Infrastructure/Pdf` do not exist) | P2-T08 (execution only) | Authored, reviewed `PLAN ACCEPT` for P2-T08 |

These do **not** block P2-T01, P2-T02, P2-T03 (shared frontend primitives), whose authority is
the already-accepted A1 freeze plus `dmo-beta-master/contracts/SHARED_FRONTEND.md` and
`IMPLEMENTATION_MODEL.md`. They also do not block P2-T04–P2-T08 **planning**, which this
document completes.

---

## 5. Beta Dependency Graph

```text
────────────────────────────────────────────────────────────────────────────
FOUNDATION            (implemented, protected — §2, §12)
  auth · accounts · Templates · ModuleRegistry · AccessResolver · module gates
  Template/USER administration · shell · navigation projection · landing/routing
────────────────────────────────────────────────────────────────────────────
        │
        ▼
SHARED BETA PRIMITIVES   (Phase 2, no domain dependency)
  P2-T00  P1-T07 acceptance gate (governance)
  P2-T01  A3  generic states · RecordStatus · AvailabilityState
  P2-T02  A4  DenseDataTable · AuditTrail
  P2-T03  A5/A6 ToolPicker presentation · ToolSummaryRow ·
               MeasurementRows · DecisionBar
  P2-T09  secondary navigation + IsCurrent wiring (shared shell extension)
────────────────────────────────────────────────────────────────────────────
        │  (primitives needed before any operational page is rendered)
        ▼
DOMAIN CORE             (shared by >1 operational module)
  P2-T04  Tool canonical identity + Job On production context (jobon_id, cm/mf/bq)
          ── owns canonical Tool + production occurrence; consumed by B, C, E
────────────────────────────────────────────────────────────────────────────
        │
        ├───────────────────────┬──────────────────────┐
        ▼                       ▼                      ▼
OPERATIONAL MODULES
  P2-T05 Controlo Create   P2-T06 Controlo Approve   P2-T07 Boquilhas
    (Peso, Comparação,        (review/decision over      (aggregate, 4 movements,
     Pegamentos, Folha,        the same peso_id;          edit-audit, balance,
     Resumo, shared Peso       consumes T05 read model)   close/reopen, History)
     read model)
        │                       │                      │
        └───────────────────────┴──────────────────────┘
                                ▼
CROSS-CUTTING FEATURES
  P2-T08  Documents/PDF: generation, directory convention, availability states
  (Ferramentas Light contextual Tool ficha is delivered inside P2-T04;
   HISTÓRICO GLOBAL is DEFERRED BY DESIGN — §3.1)
────────────────────────────────────────────────────────────────────────────
        │
        ▼
FINAL INTEGRATION
  P2-T10  per-module availability registration + real route registration +
          navigation exposure + end-to-end verification
          (incremental, per destination — never ahead of a real surface)
────────────────────────────────────────────────────────────────────────────
```

**Hard ordering rules.**
1. FOUNDATION before everything (it is already done).
2. SHARED PRIMITIVES before any operational page render (P2-T01…P2-T03 before P2-T04+).
3. DOMAIN CORE (P2-T04) before P2-T05 and P2-T07 (both consume canonical Tool + Job On context).
4. P2-T05 before P2-T06 (D consumes C's submitted Peso read model; D must not fork the renderer).
5. P2-T10 is **per-destination and strictly after** that destination is real.

---

## 6. Implementation Workstream Index

| ID | Title | Class | Depends on | Bilaterally blocks |
|---|---|---|---|---|
| P2-T00 | P1-T07 acceptance gate (governance record) | Governance | — | non-blocking; enables claiming P1-T07 accepted |
| P2-T01 | Shared generic states + `RecordStatus` + `AvailabilityState` (A3) | Shared primitive | — | P2-T02…P2-T07 |
| P2-T02 | `DenseDataTable` + `AuditTrail` (A4) | Shared primitive | — | P2-T04…P2-T07 |
| P2-T03 | `ToolPicker` presentation + `ToolSummaryRow` + `MeasurementRows` + `DecisionBar` (A5/A6) | Shared primitive | — | P2-T04…P2-T07 |
| P2-T09 | Secondary navigation + current-destination wiring | Shared primitive | — | P2-T04…P2-T07, P2-T10 |
| P2-T04 | Domain core: canonical Tool identity + Job On Light + Ferramentas Light | Domain core | P2-T01…P2-T03 | P2-T05, P2-T07 |
| P2-T05 | Controlo Create (Peso, Comparação, Pegamentos, Folha, Resumo) + shared Peso read model | Operational | P2-T04 | P2-T06 |
| P2-T06 | Controlo Approve (pending list, review, approve/reject/reopen) | Operational | P2-T05 | P2-T08, P2-T10 |
| P2-T07 | Boquilhas (aggregate, movements, edit-audit, balance, close/reopen, History) | Operational | P2-T04 | P2-T08, P2-T10 |
| P2-T08 | Documents / PDF / directory convention / availability | Cross-cutting | P2-T05, P2-T06, P2-T07 | P2-T10 |
| P2-T10 | Final integration: availability + routes + navigation + e2e | Integration | P2-T04…P2-T08 | — |

Not a workstream (see §14): HISTÓRICO GLOBAL, Armazém, Reparação Interna, Reparação Programada,
Tampões, Admin audit, full Job On lifecycle, full Ferramentas lifecycle.

---

## 7. Detailed Workstreams

### P2-T00 — P1-T07 Acceptance Gate (governance)

- **Authority:** `dmo-beta-master/SOURCE_MANIFEST.md` (`IMPLEMENTED / NOT YET ACCEPTED`);
  `implementation/CURRENT_FOUNDATION.md` ("P1-T07 implementation exists != accepted
  implementation"); `WORKFLOW.md` (acceptance requires inspection of the actual commit).
- **Current implementation starting point:** commit `0b476909…` is remote `main`'s shell work,
  now carried forward in `7910f56…`. Behavior is complete and already relied upon.
- **Scope:** produce an Architect implementation review of P1-T07 against the accepted corrected
  plan (`dmo-work/dev/reviews/P1-T07_NAVIGATION_USER_SHELL_CORRECTED_PLAN_REVIEW.md`) and the
  committed diff, and record `ACCEPT`/`CORRECTION REQUIRED`. No code change.
- **Explicit non-scope:** any change to `Index.cshtml(.cs)`, `Login.cshtml(.cs)`,
  `AccessDenied.cshtml(.cs)`, `Administration/Index.*`, `LandingSelector`,
  `UserLandingService`, `NavigationProjectionService`, `ShellPresentation*`,
  `DestinationRoutes`, `ModuleCatalog`, `ModuleRegistrations`. Do not rebuild the shell.
- **Expected artefacts:** a review document in the governance repository (`dmo-work`), not in
  `DMO-MODULAR/src|tests`.
- **Tests:** none (no code change). Existing P1-T07 tests remain the regression guard.
- **Acceptance criteria:** the review exists, cites the committed SHA, and states an explicit
  verdict; the verdict does not require behavior already proven in the P1-T07 response and the
  committed test source (`RootRoutingTests`, `NoAccessPageTests`, `LoginPageTests`,
  `DirectRouteEnforcementTests`, `UserLandingPersistenceIntegrationTests`).
- **Completion evidence:** committed review document with the inspected SHA.
- **Downstream dependents:** removes the caveat on P1-T07 as "accepted foundation" for
  P2-T10.
- **STATUS: CLOSED.** Architect implementation review
  `dmo-work/dev/reviews/P1-T07_NAVIGATION_USER_SHELL_IMPLEMENTATION_REVIEW.md` @
  `50e884135fb885f8d2a3e55393d25d246d69a39c` — verdict **ACCEPT**, inspecting
  `DMO-MODULAR@0b47690936599b6a71342b68b1cf36cfe4b64264`. No application code changed.
  Verification: build 0 warnings/0 errors; unit 335/335 passed; integration 102 passed /
  71 env-gated skipped / 0 failed; `git diff --check` clean; 0 migrations;
  `CurrentBuildAvailable` still `[]`. P1-T07 may now be treated as accepted foundation.

### P2-T01 — Shared generic states + `RecordStatus` + `AvailabilityState` (A3)

- **Authority:**
  - `dmo-beta-master/IMPLEMENTATION_MODEL.md` Workstream A (owns `RecordStatus`,
    `AvailabilityState`, "loading/empty/failure/permission states").
  - `dmo-beta-master/contracts/SHARED_FRONTEND.md` §"Common state vocabulary" (the ten states
    and the mandated distinctions `empty != lookup-failed != unavailable != permission-denied`),
    §10 (`RecordStatus`), §11 (`AvailabilityState`, eight distinct states).
  - `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` §4, §10, §11 (the frozen, Architect-
    accepted contract) — primary repository authority for this workstream.
  - `workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md` §14 "A3 — Generic states, status,
    and availability".
- **Current implementation starting point:** `src/DMO.Web/Frontend/Shared/` contains only
  `SharedFrontendExtensions.cs`. `Pages/Shared/` contains only `_Layout`, `_PublicLayout`,
  `_Identity`, `Navigation/*`. There is no component and no `wwwroot/js` directory.
- **Dependencies:** none.
- **Precise scope (BEHAVIOR + PRESENTATION, all presentation-only):**
  - one shared presentation model + Razor rendering for the common state region
    (`loading`, `ready`, `empty`, `lookup-failed`, `unavailable`, `permission-denied`,
    `saving`, `submitting`, `stale`, `conflict`);
  - `RecordStatus`: supplied status text is **always** rendered; optional tone/icon is
    supplementary and never color-only; an unknown supplied status uses neutral presentation;
    status grants no actions and infers no lifecycle;
  - `AvailabilityState`: the **eight** distinct states `available`, `not-generated`,
    `awaiting-approval`, `workspace-unavailable`, `file-missing`, `versions-available`,
    `not-applicable`, `lookup-failed`, each with explicit text; `not-applicable` is not an
    error; `lookup-failed` is never rendered as no-file/no-record; `file-missing` is distinct
    from `not-generated`.
- **Explicit non-scope:** no domain status catalogue, no lifecycle rules, no approval meaning,
  no authorization, no storage/filesystem lookup, no document generation, no module-specific
  terminology, no formula.
- **Expected files/projects:** `src/DMO.Web/Frontend/Shared/Contracts/` (presentation models),
  `src/DMO.Web/Pages/Shared/Components/` (Razor partials/view components),
  `src/DMO.Web/wwwroot/css/dmo-components.css` (new A-owned CSS), minimal additive
  `SharedFrontendExtensions` registration; tests under
  `tests/DMO.UnitTests/Frontend/Shared/` and `tests/DMO.IntegrationTests/Frontend/Shared/`.
- **Backend requirements:** none.
- **Access requirements:** none (presentation never decides access).
- **Persistence requirements:** none.
- **Required tests:** see §11 (P2-T01 row) — state-model unit tests, rendered tests proving all
  eight availability states stay distinct, non-color-only status, and absent actor/time
  synthesis is not applicable here.
- **Acceptance criteria (objective):**
  1. Every one of the ten states renders distinct, textual, non-color-only output.
  2. `empty`, `lookup-failed`, `unavailable` and `permission-denied` are never mapped to the same
     rendering.
  3. An unknown supplied `RecordStatus` renders neutrally and never guesses a tone.
  4. No shared type in the new contracts references a feature namespace or service.
  5. Disabled actions, where modelled, expose an associated supplied reason.
  6. `CurrentBuildAvailable` is still `[]` and no route changed.
- **Completion evidence:** committed component + tests, build/test results recorded by the
  implementing agent, and confirmation that no protected file changed.
- **STATUS: IMPLEMENTED** — implementation commit `72c38c26fa03a81465de72bed07c57f087530618`
  in `DMO-MODULAR/main`; response
  `dev/responses/P2_T01_IMPLEMENTATION_RESPONSE.md`. New A-owned paths:
  `src/DMO.Web/Frontend/Shared/Contracts/*` (10 presentation contracts),
  `src/DMO.Web/Pages/Shared/Components/{_CommonStateRegion,_RecordStatus,_AvailabilityState}.cshtml`,
  `src/DMO.Web/wwwroot/css/dmo-components.css`, plus unit/integration tests under
  `tests/**/Frontend/Shared/`. Additive registration was already satisfied by the existing
  `AddRazorPages()`, so `SharedFrontendExtensions.cs` is byte-identical. Documentation slice
  (ledger 8.1/8.2/8.3): `src/DMO.Application|DMO.Infrastructure|DMO.Web/README.md` refreshed.
  Verification: build 0 warnings/0 errors; unit 374/374 passed; integration 115 passed /
  71 env-gated skipped / 0 failed; `git diff --check` clean; 0 migrations; 0 provisional
  markers in `src/`; `CurrentBuildAvailable` still `[]`; no protected file modified.
  Awaiting Architect implementation review per `dmo-beta-master/WORKFLOW.md` step 12.
- **Downstream dependents:** P2-T02…P2-T07 (all consume status/availability/states).

### P2-T02 — `DenseDataTable` + `AuditTrail` (A4)

- **Authority:** `IMPLEMENTATION_MODEL.md` Workstream A; `contracts/SHARED_FRONTEND.md` §9
  (`DenseDataTable`), §12 (`AuditTrail`); A1 freeze §9, §12; A plan §14 "A4".
- **Current implementation starting point:** none; no table or audit presentation exists.
- **Dependencies:** P2-T01 (states).
- **Precise scope:**
  - `DenseDataTable`: single-click selects; double-click + Enter open **only where the consumer
    supplies** that behavior; explicit keyboard focus; loading/empty/error states stay distinct;
    consumer-owned filters/pagination; selection never silently triggers a domain action;
    horizontal overflow is keyboard reachable and does not trap focus; selected state is
    programmatic and non-color-only.
  - `AuditTrail`: renders only supplied actor/timestamp/action and optional before/after detail;
    preserves supplied ordering; **never** synthesizes attribution from the current session;
    never replaces a missing historical actor/time with the current user/time.
- **Explicit non-scope:** no fetching, backend filtering/paging, URL construction, domain
  sorting, mutation, authorization, automatic navigation, audit creation, timestamp generation,
  diff calculation.
- **Expected files/projects:** as P2-T01 (contracts, `Pages/Shared/Components/`,
  `dmo-components.css`, optional `wwwroot/js/dmo-dense-table.js`, `wwwroot/js/dmo-focus.js`);
  tests under the two `Frontend/Shared` test folders.
- **Backend / access / persistence requirements:** none.
- **Required tests:** selection vs open separation; Enter opens / Space selects; two clicks on
  one row invoke open once; focus survives re-render; audit trail never invents actor/time;
  empty/lookup-failed distinction.
- **Acceptance criteria:** the six bullet behaviors above each have a failing-if-removed test;
  no consumer URL is constructed by the component; no attribution synthesis is observable.
- **STATUS: P2-T02 IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION
  REVIEW.** Implementation commit `04601f9a827797481acd8ce4f76a6d2b1eb21af6`. The formal
  implementation contract is
  `plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md` at accepted contract SHA
  `e79186a81d5cd934fe32a100bc8dd9dd08bf509a`, accepted by Architect PLAN ACCEPT at
  `dmo-work` SHA `f1ddb968e026dc6cf2569d8de64400d8c3044514`; authoring response
  `dev/responses/P2_T02_CONTRACT_AUTHORING_RESPONSE.md`; implementation response
  `dev/responses/P2_T02_IMPLEMENTATION_RESPONSE.md`. The contract pins both components to the
  binding DMO fixed desktop layout policy (canonical 1366 × 768; no breakpoint reflow, no
  table-to-card conversion, no required-column hiding, no action relocation; keyboard-reachable
  local horizontal scroll), reuses the accepted P2-T01 state/status/action primitives without
  modification, protects the P2-T03 boundary (no ToolPicker/ToolSummaryRow/MeasurementRows/
  DecisionBar/ProductionContextStrip), defines no backend/persistence/route/authorization seam,
  and records 3 CONTRACT QUESTIONS (Q1 browser-level JS verification and Q3 plain-text vs
  rendered cell content defaults accepted; Q2 resolved by Architect review with AuditTrail
  entry status absent). Implementation is additive only: 17 new shared contract types, 3 new
  shared partials, 2 new generic static assets, additive CSS and new unit/rendered tests; no
  P2-T01 artifact modified, no route or availability registration added, `CurrentBuildAvailable`
  still `[]`. **Not self-accepted and not closed:** formal closure requires independent
  verification and an Architect implementation review per `dmo-beta-master/WORKFLOW.md` step 12.
  P2-T03 remains unauthorized and unimplemented.
- **Downstream dependents:** P2-T04 (Job On history), P2-T05/P2-T06 (Controlo lists),
  P2-T07 (Boquilhas History), P2-T08 (audit/decision history rendering).

### P2-T03 — `ToolPicker` presentation + `ToolSummaryRow` + `MeasurementRows` + `DecisionBar` (A5/A6)

- **Authority:** `IMPLEMENTATION_MODEL.md` Workstream A; `contracts/SHARED_FRONTEND.md` §7, §8,
  §13, §14; A1 freeze §7, §8, §13, §14; A plan §14 "A5"/"A6".
- **Current implementation starting point:** none.
- **Dependencies:** P2-T01.
- **Precise scope (presentation/mechanics ONLY):**
  - `ToolPicker`: explicit search state + candidate list; **never auto-selects**, including when
    exactly one result exists; ambiguous candidates stay separate; `Criar ferramenta` exposed
    only when the consumer supplies it enabled; cancel returns to origin without selection;
    enter on search never selects the first result; Escape requests cancel and never discards
    origin work without consumer confirmation; close/return restores invoking-control focus;
    preserves consumer-supplied origin state via an opaque token.
  - `ToolSummaryRow`: renders supplied type/reference/lot/machines/quantity/process/status/
    actions; separates missing-optional from zero/false; performs no lookup or inference.
  - `MeasurementRows`: stable frontend row identity across add/remove/re-render; configurable
    minimum (including at-least-one); removal disabled with a visible associated reason when the
    minimum would be violated; focus moves to the first editable control of a new row and to the
    nearest surviving control after removal; **no** measurement field, formula, tolerance or
    domain rule.
  - `DecisionBar`: primary/secondary/danger groups; visible disabled reason; pending state;
    duplicate invocation prevented; action order follows supplied order; **no** approve/reject/
    reopen/submit/close/movement rule encoded.
- **Explicit non-scope:** no Tool search algorithm, ranking/compatibility rule, auto-selection,
  Tool creation, canonical identity, CM/MF/BQ association, persistence, endpoint, authorization,
  domain validation, measurement schema, formulas, tolerances, nominal values.
- **Expected files/projects:** as P2-T01 + optional `wwwroot/js/dmo-tool-picker.js`,
  `wwwroot/js/dmo-measurement-rows.js`; tests in the two `Frontend/Shared` folders.
- **Backend / access / persistence:** none. Opaque carrier keys are **never** declared to be
  `tool_id`/`jobon_id` or any canonical identity (A1 freeze §2).
- **Required tests:** never-auto-select (including single candidate); cancel restores origin and
  focus; Escape does not discard work; minimum-row enforcement with exposed reason; add/remove
  focus rules; duplicate decision invocation prevented; no feature semantics in the components.
- **Acceptance criteria:** each A1-frozen interaction rule has a dedicated test; no canonical
  identity string appears in any shared contract type or fixture.
- **Downstream dependents:** P2-T04 (Tool orchestration is B/C/E shared), P2-T05 (MeasurementRows
  in Peso/Pegamentos), P2-T06/P2-T07 (DecisionBar actions).
- **STATUS: CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW.** The formal implementation
  contract is `plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md`; authoring response
  `dev/responses/P2_T03_CONTRACT_AUTHORING_RESPONSE.md`. The contract pins all four components to
  the binding DMO fixed desktop layout policy (canonical 1366 × 768; no breakpoint reflow, no card
  conversion, no required-control hiding, no action relocation, keyboard-reachable local scrolling),
  defines the exact per-component boundaries with carriers, states and deterministic
  unit-testable interaction models (`ToolPickerInteraction`, `MeasurementRowsInteraction`,
  `DecisionBarInteraction`), reuses the accepted P2-T01/P2-T02 primitives without modification
  (`CommonState`/`_CommonStateRegion`, `RecordStatusPresentation`/`_RecordStatus`, `StatusTone`,
  `SharedActionPresentation`, `dmo-focus.js`, the additive-CSS/token pattern), keeps the opaque
  carrier keys opaque (no `tool_id`/`jobon_id`/`cm_id`/`mf_id`/`bq_id` declaration anywhere),
  defines no backend/persistence/route/authorization/Supabase seam, protects the P2-T04+ boundary,
  and records a **complete** test-to-acceptance matrix (52 acceptance criteria ↔ 99 tests,
  bidirectional coverage) plus 6 CONTRACT QUESTIONS, all NON-BLOCKING with pinned defaults.
  **Not implemented and not self-accepted:** P2-T03 implementation is unauthorized until the
  Architect reviews the committed contract and returns `PLAN ACCEPT`, disposing Q1–Q6. No
  application code was changed by the authoring task; `CurrentBuildAvailable` is still `[]`;
  P2-T04 has not started.

### P2-T09 — Secondary navigation + current-destination wiring

- **Authority:** `reconciliation §7.2` (PARTIAL: records exist, no producer);
  `workbench/BETA_FRONTEND_IMPLEMENTATION_WORKSTREAMS.md` §4 (A owns "primary destination and
  local secondary navigation patterns"); A plan §9.2 "Active state and destination behavior".
- **Current implementation starting point:** `SecondaryDestinationPresentation` and `IsCurrent`
  exist (`ShellPresentationModels.cs`) and are honored by `_PrimaryNavigation.cshtml`
  (`class="is-current"`, `aria-current="page"`) and `_SecondaryNavigation.cshtml`, but
  `ShellPresentationService.BuildAsync` always passes `[]` and never sets `IsCurrent`.
- **Dependencies:** none. **Note:** this is additive wiring — the record shapes and partials are
  preserved, not redesigned.
- **Precise scope:** extend the shell presentation seam so a consuming surface can supply
  secondary destinations and current-destination identity, and so the primary/current marking is
  derived from the request path against the **already-projected** live destinations. No new
  access logic.
- **Explicit non-scope:** no change to `NavigationProjectionService` projection filters, no new
  destination authority, no feature pages, no second nav composer.
- **Expected files/projects:** `src/DMO.Web/Frontend/Shell/ShellPresentationService.cs`
  (additive), possibly `ShellPresentationModels.cs` (additive), the two nav partials only if a
  wiring defect is proven; tests in `Frontend/Shared`.
- **Backend / access / persistence:** none.
- **Required tests:** a supplied secondary list renders; `IsCurrent` marks exactly the matching
  primary destination; ADMIN and denied USER still render zero operational destinations; the
  secondary partial renders nothing when the list is empty (`_SecondaryNavigation` already
  guards `Model.Count > 0`).
- **Acceptance criteria:** current-destination marking is derived from the projected destination
  set only; no authorization decision is taken in presentation; empty secondary renders nothing.
- **Downstream dependents:** P2-T04…P2-T07 (local navigation), P2-T10.

### P2-T04 — Domain core: canonical Tool identity + Job On Light + Ferramentas Light

- **Authority:** `dmo-beta-master/modules/JOB_ON_LIGHT.md`;
  `dmo-beta-master/modules/FERRAMENTAS_LIGHT.md`;
  `dmo-beta-master/architecture/CROSS_MODULE_FLOWS.md`;
  `dmo-beta-master/architecture/BACKEND_FRONTEND_MODEL.md` (canonical identity chain);
  `dmo-beta-master/contracts/IDENTITIES_AND_RELATIONSHIPS.md`;
  `dmo-beta-master/architecture/RECORD_LIFECYCLES.md` §2–3 (Job On has no lifecycle state
  machine; CM/MF/BQ contexts freeze);
  `dmo-master/global/ACCESS_MODEL.md` §8 (Job On View/Create separation);
  `dmo-master/dmo-modular` `BETA_VERSION.md` §2–§5 (minimum Beta Tool fields, simplified Job On
  identity, duplication);
  `implementation/BETA_INTEGRATION_SEAMS.md` Workstream B.
- **Current implementation starting point:** nothing operational exists. Reusable seams:
  `ModuleCatalog` identities for `job-on-view`/`job-on-create`/`ferramentas`/
  `ferramentas-approve`; the module gate; `IModuleAccessService`; the persistence/migration
  framework; `docs/CREATION_AND_ASSOCIATION_LOGIC.md` (real-then-enriched identities).
- **Dependencies:** P2-T01, P2-T02, P2-T03. **Authority blocker B1** must be closed by an
  authored, `PLAN ACCEPT`-ed contract before execution.
- **Precise scope:**
  - canonical Tool identity and master facts (type CM/MF/BQ, reference, lot, machine/line
    compatibility, canonical quantity, processo `NNPB|PS`); **different lot = different Tool**;
  - `jobon_id` production occurrence (reference, production number, machine, processo) with
    **no** invented Job On lifecycle state machine;
  - `cm_id`/`mf_id`/`bq_id` production contexts (`jobon_id + tool_id -> context`), created only
    where actually needed — never for symmetry;
  - Job On create/view/edit; reference → productions query + explicit selection;
  - explicit Job On duplication: new `jobon_id` + new context IDs, source unchanged, any
    historical source selectable (never forced to the latest), copied `tool_id`s retained until
    explicitly changed; delete forbidden when dependent facts exist; date-threshold edit is a
    warning, not hard immutability;
  - one shared Tool search/select/create orchestration returning a canonical `tool_id` and
    restoring origin state;
  - Ferramentas Light contextual Tool ficha: **no top-level destination**.
- **Explicit non-scope:** full Job On lifecycle/revisions/print orchestration; full Ferramentas
  change-request/approve lifecycle and technical-condition/utilisation dossier; Controlo
  calculations; Boquilhas movement ownership; Armazém; any `production_id` / `job_on_revision_id`
  / `tool.id.jobons[]` reverse arrays; any fake `cm_id`/`jobon_id`.
- **Expected files/projects:** `src/DMO.Domain` (new Tool/JobOn context primitives — first real
  domain types, added only as this workstream requires),
  `src/DMO.Application/` (a Tools/JobOn application area + repository contracts),
  `src/DMO.Infrastructure/Persistence/` (entities, configurations, repository implementations),
  one new migration owning the Tool/JobOn/context schema,
  `src/DMO.Web/Pages/JobOn/` + `Pages/Ferramentas/`, `src/DMO.Web/Endpoints/` (tool/jobon
  endpoints), tests in both projects.
- **Access requirements:** every route/action declares
  `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.JobOnView | JobOnCreate | Ferramentas |
  FerramentasApprove)` as appropriate. Job On View never grants Create actions. Ferramentas
  stays contextual-only and gains no top-level destination.
- **Persistence requirements:** Tool master; Job On occurrence; CM/MF/BQ contexts; explicit
  previous-source relation for duplication; no reverse-ID arrays; unique/FK semantics per the
  accepted contract (production key at least `reference + production_number`, extended if
  authority requires).
- **Required tests:** see §11 (P2-T04 row).
- **Acceptance criteria:** every bullet in `modules/JOB_ON_LIGHT.md` "Acceptance criteria" and
  `modules/FERRAMENTAS_LIGHT.md` "Acceptance criteria"; ambiguity requires explicit selection;
  duplicated Job On produces a new `jobon_id` + new context IDs with unchanged source;
  Ferramentas remains absent from top-level navigation.
- **Completion evidence:** committed implementation + tests + the `PLAN ACCEPT` contract it was
  built against.
- **Downstream dependents:** P2-T05, P2-T07, P2-T08, P2-T10.

### P2-T05 — Controlo Create (Peso, Comparação, Pegamentos, Folha, Resumo) + shared Peso read model

- **Authority:** `dmo-beta-master/modules/CONTROLO_CREATE.md`;
  `architecture/RECORD_LIFECYCLES.md` §4–8; `architecture/CROSS_MODULE_FLOWS.md` (Job On →
  Controlo Create; Create → Approve);
  `implementation/BETA_INTEGRATION_SEAMS.md` "C → D seam" and "C owns the shared Peso
  renderer/read model";
  `contracts/DOCUMENTS_AND_FILES.md` §2–3 (document identity/naming only; generation is P2-T08);
  `dmo-master/global/ACCESS_MODEL.md` §9.
- **Current implementation starting point:** none. Depends on P2-T04's hidden context contract
  and P2-T03's `MeasurementRows`.
- **Dependencies:** P2-T04. **Authority blocker B2.**
- **Precise scope:**
  - select/create Job On context; display production context; resolve/reuse CM context;
  - Peso draft/edit/calculation/submission on **one** `peso_id`; normal relation
    `peso_id -> cm_id`; the truthful pending case `peso_id -> tool_id` shown as
    **`Job On por associar`** (not an error, does not block measurement/approval; later
    association is explicit and clears the direct Tool anchor); **no** fake Job On/`cm_id`;
  - formulas exactly as Beta fixes them (water capacity ÷ configured water-temperature value;
    glass weight = (CM capacity + Marisa/BQ volume − PU volume) × glass density); water range
    5–35 °C; individual CM results first-class and never hidden by an average; decimals
    normalized for presentation only;
  - variable measurement rows with at least one valid row and stable row identity;
  - Comparação as a Peso record type with an explicitly selected `previous_peso_id` (never
    auto-selected; cross-machine candidates valid when the same canonical Tool is compatible);
    explicit pairing; stale-after-reading-change must be rebuilt before submission;
    `current_peso_id -> previous_peso_id` persisted explicitly;
  - Pegamentos: `pegamentos_id -> jobon_id -> cm/mf/bq`; CM/BQ/MF sections; Costura 0° /
    Contra-costura 90°; signed ovalização; average; single-axis behavior; variable rows; nominal
    from canonical Tool data; tolerance corridor `nominal ± 0.20`; boundary crossing is a
    warning; missing nominal → `NotEvaluable` and never invented; invalid/missing required Tool
    context blocks that sheet with an actionable correction message; Pegamentos may legitimately
    be absent;
  - Folha (`controlo_sheet_id -> jobon_id`) and Resumo (`resumo_id -> jobon_id + applicable
    contexts`) as **distinct** persisted records;
  - submit transitions the same `peso_id` into reviewable state with backend truth for
    state/attribution; Create never approves its own record;
  - publish the canonical **read-only Peso sheet/read model** for P2-T06.
- **Explicit non-scope:** approval/rejection/reopen decisions (P2-T06); automatic previous-Peso
  selection; same-machine-only restriction; duplicate Tool registry; independent production
  identity; frontend-owned formulas or persistence; approval-copy Peso; document generation
  (P2-T08); PDF bytes.
- **Expected files/projects:** `src/DMO.Domain` (Controlo value objects),
  `src/DMO.Application/ControloCreate/` (+ repository contracts),
  `src/DMO.Infrastructure/Persistence/` + one migration owning Controlo schema,
  `src/DMO.Web/Pages/Controlo/` (Create-side), `src/DMO.Web/Endpoints/`,
  `src/DMO.Web/Frontend/Controlo/` (shared Peso sheet read model), tests in both projects.
- **Access requirements:** `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloCreate)`
  on every Create route/action; the shared `controlo` destination keeps Create and Approve gates
  independent.
- **Persistence requirements:** `peso_id`, `pegamentos_id`, `controlo_sheet_id`, `resumo_id`
  with the relations above; explicit `previous_peso_id` relation; no snapshot engine beyond what
  each record's own historical output requires; no second Job On/Peso process authority
  (processo is consumed through `cm_id -> tool_id`).
- **Required tests:** see §11 (P2-T05 row).
- **Acceptance criteria:** every bullet in `modules/CONTROLO_CREATE.md` "Acceptance criteria";
  same `peso_id` from draft to submit; previous Peso never automatic; stale Comparação requires
  rebuild; Folha and Resumo remain distinct records; warnings never approve/reject.
- **Completion evidence:** committed implementation + tests + published Peso read-model contract
  consumed by P2-T06.
- **Downstream dependents:** P2-T06, P2-T08, P2-T10.

### P2-T06 — Controlo Approve

- **Authority:** `dmo-beta-master/modules/CONTROLO_APPROVE.md`;
  `architecture/RECORD_LIFECYCLES.md` §4 (approval/rejection/reopen on the same `peso_id`);
  `architecture/ACCESS_AND_NAVIGATION.md` (independent Create/Approve enforcement);
  `implementation/BETA_INTEGRATION_SEAMS.md` "C → D seam" and "Workstream D".
- **Current implementation starting point:** none. The two-module identity split and the
  sibling-non-satisfaction rule already exist and are tested
  (`NoProfilesRegressionTests.SiblingModule_SameDestination_DoesNotSatisfyGate`).
- **Dependencies:** **P2-T05 (strictly)** — D consumes C's submitted Peso read model and must not
  fork the renderer. Authority blocker B2.
- **Precise scope:** pending/review list with filters over backend-reported reviewable facts;
  open the exact submitted `peso_id`; read measurements/results/warnings/production context;
  read the persisted explicit Comparação relation and enough current/previous context to
  understand it without heuristic reconstruction; per-CM human decisions (`Manter` /
  `Colocar de parte`) where required; Folha decision/review on the exact persisted
  `controlo_sheet_id`; approve; reject with note where required; reopen on the same record
  preserving decision/attribution history; attributed audit/history; explicit confirmed
  `Enviar para produção` only where the published contract allows it.
- **Explicit non-scope:** editing submitted measurement facts without reopen; approval-copy
  Peso; redefining formulas or Comparação pairing; forking/copying C's Peso renderer; Job
  On/Ferramentas/Boquilhas ownership; automatic decisions from warnings.
- **Expected files/projects:** `src/DMO.Application/ControloApprove/` (+ repository contracts),
  `src/DMO.Infrastructure/Persistence/` (decision/audit persistence, migration owned by this
  slice), `src/DMO.Web/Pages/Controlo/Approve/` (review mode reusing C's read-model
  presentation), `src/DMO.Web/Endpoints/`, tests in both projects.
- **Access requirements:** `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloApprove)`
  on every Approve route/action; Create does not grant Approve and vice versa; the shared
  `controlo` destination never merges the grants.
- **Persistence requirements:** status transitions on the existing `peso_id`; per-CM decision
  facts; `controlo_sheet_id` decision; actor/time attribution as backend facts; historical
  decision trail; concurrency/conflict behavior per the accepted contract.
- **Required tests:** see §11 (P2-T06 row).
- **Acceptance criteria:** every bullet in `modules/CONTROLO_APPROVE.md` "Acceptance criteria";
  submitted facts are read-only until an authorized reopen; approve/reject/reopen mutate the
  same record; warnings never decide; actor/time come from backend facts; direct route/action
  denial is server-side.
- **Completion evidence:** committed implementation + tests + explicit statement that C's
  renderer was reused, not copied.
- **Downstream dependents:** P2-T08, P2-T10.

### P2-T07 — Boquilhas

- **Authority:** `dmo-beta-master/modules/BOQUILHAS.md`;
  `architecture/RECORD_LIFECYCLES.md` §9; `architecture/CROSS_MODULE_FLOWS.md` (Job On →
  Boquilhas; standalone validity); `implementation/BETA_INTEGRATION_SEAMS.md` "Workstream E"
  and "E → B" seam; `dmo-master/global/ACCESS_MODEL.md` §1/§11 (Boquilhas is one assignable
  module).
- **Current implementation starting point:** none. Depends on P2-T04 for canonical BQ Tool
  selection/create and optional `bq_id` resolution.
- **Dependencies:** P2-T04 (not P2-T05). Authority blocker B3.
- **Precise scope:** search/select/create BQ Tool context; production-linked flow
  (`boquilhas_id -> bq_id -> jobon_id + tool_id`) and standalone flow
  (`boquilhas_id -> tool_id`, **no** fake Job On/`bq_id`); active aggregate summary; exactly the
  four write movement types `Início`, `Saída`, `Entrada`, `Irreparável`; `Editar` as an action on
  an existing movement (**never** a fifth movement type); movement forms and validation; recent
  movements; full History with filters (reference, lot, line, business date/period, movement
  type, repairer, aggregate/file state, pagination), single click selects, double click opens;
  edit preserving before/after audit, authenticated user and system timestamp **without** a
  second quantity event or double balance effect; `business_date` (editable) distinct from
  `recorded_at` (immutable); derived balance buckets Disponível / Em reparação / Irreparável /
  Entrada excecional from movement facts (no second mutable balance authority); Saída ≤ available
  quantity; Irreparável ≤ in-repair quantity; excess Entrada recorded not clamped/rejected;
  negative saldo visible and non-blocking; `% utilização` manual and not derived from movements;
  close/reopen on the same `boquilhas_id` with immutable close snapshot and recorded
  reopen actor/time/reason; failed close leaves the active state unchanged; canonical
  `repairer_id` stored on external Saída with historical retention; production-line contextual
  panel reading (not owning) production context.
- **Explicit non-scope:** mandatory Boquilhas PDF; internal Boquilhas settings/Admin tab;
  repairer directory administration; Job On planning ownership; Armazém stock/location;
  per-piece BQ identity; obsolete legacy movement types.
- **Expected files/projects:** `src/DMO.Domain` (movement/aggregate primitives),
  `src/DMO.Application/Boquilhas/` (+ repository contracts),
  `src/DMO.Infrastructure/Persistence/` + one migration owning Boquilhas schema,
  `src/DMO.Web/Pages/Boquilhas/`, `src/DMO.Web/Endpoints/`, tests in both projects.
- **Access requirements:** `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas)`
  on routes/actions; navigation availability remains projection only.
- **Persistence requirements:** aggregate + movements + edit/audit history + close snapshot +
  reopen record; `repairer_id` relation; no reverse-ID arrays; movement facts are the sole
  balance authority.
- **Required tests:** see §11 (P2-T07 row).
- **Acceptance criteria:** every bullet in `modules/BOQUILHAS.md` "Acceptance criteria" — the
  movement selector exposes only the four types; `Editar` is not a type; edit adds audit without
  a second movement; both flows work without fake identities; excess Entrada recorded; negative
  saldo non-blocking; business date ⊥ recorded timestamp; close/reopen retains the same
  `boquilhas_id` and full history; no mandatory PDF or settings tab.
- **Completion evidence:** committed implementation + tests + confirmation that no replacement
  aggregate is created by close/reopen.
- **Downstream dependents:** P2-T08, P2-T10.

### P2-T08 — Documents / PDF / directory convention / availability

- **Authority:** `dmo-beta-master/contracts/DOCUMENTS_AND_FILES.md` (full);
  `sources/dmo-master/global/DOCUMENT_FILE_MODEL.md` (archived upstream snapshot);
  `architecture/APPLICATION_FOUNDATION.md` "Documents" (record ≠ generated document ≠ path);
  `ACCEPTANCE_MATRIX.md` §8 (documents/PDF gate);
  `dmo-master/dmo-modular/BETA_VERSION.md` §8 (directory convention).
- **Current implementation starting point:** nothing. `DMO.Domain` has no types; there is no
  Files or Pdf project (`Infrastructure/Files`, `Infrastructure/Pdf` are recorded as "not yet
  implemented" in `docs/SKELETON_RATIONALIZATION_P1-T01.md`).
- **Dependencies:** P2-T05 (Peso), P2-T06 (approved/decision state), P2-T07 (Boquilhas where a
  file state applies). Authority blocker B4.
- **Precise scope:** structured owning record remains the only truth; deterministic
  `Peso_<reference>_<line>.pdf`, `Pegamentos_<reference>_<line>.pdf`,
  `Resume_<reference>_<line>.pdf`; directory `<reference>/<production-number>/`; filename/path
  never a join key or identity; historical rendering uses preserved historical context and never
  regenerates from today's mutable Tool fields; availability presentation uses exactly
  `Disponível`, `Ainda não gerado`, `A aguardar aprovação`, `Workspace indisponível`,
  `Ficheiro em falta`, `Versões disponíveis` from P2-T01's `AvailabilityState`; missing optional
  output (e.g. Pegamentos) is not a generic error; lookup failure is not an empty result;
  `file:///` paths are never printed into a PDF; no document metadata table is introduced merely
  for symmetry; official/frozen outputs are not silently regenerated.
- **Explicit non-scope:** inventing new Job On document identities (the three official Job On
  outputs belong to the global Job On contract and are not required by Beta Job On Light);
  a mandatory Pegamentos file; introducing a document table/identity just for symmetry.
- **Expected files/projects:** `src/DMO.Domain` (document-state value objects if required),
  `src/DMO.Application/Documents/` (+ generation/availability contracts),
  `src/DMO.Infrastructure` file/pdf adapters (new area, not a new project unless an
  Architect-approved need justifies it), `src/DMO.Web/Endpoints/` (open/regenerate where
  allowed), tests in both projects.
- **Access requirements:** document access is gated by the owning workflow/action permission —
  no separate document authorization model.
- **Persistence requirements:** owning-record state plus, only where an immutable output is
  genuinely required, persisted metadata; no artificial document identity.
- **Required tests:** see §11 (P2-T08 row).
- **Acceptance criteria:** the ten `contracts/DOCUMENTS_AND_FILES.md` §8 conditions.
- **Completion evidence:** committed implementation + tests + a rendered/parsed check that no
  filesystem path appears in output and that the three availability distinctions hold.
- **Downstream dependents:** P2-T10.

### P2-T10 — Final integration: availability + routes + navigation + end-to-end

- **Authority:** `implementation/BETA_INTEGRATION_SEAMS.md` "Navigation/route-registration seam"
  and "Access/action seam"; `architecture/ACCESS_AND_NAVIGATION.md` "Current build
  availability", "Runtime destination validity", "Direct-route enforcement";
  `ACCEPTANCE_MATRIX.md` §9, §12.
- **Current implementation starting point:** `ModuleRegistrations.CurrentBuildAvailable = []`;
  `EmptyDestinationRouteRegistry` is the production registration;
  `DestinationRouteRegistrations` is an empty documentation seam.
- **Dependencies:** whichever of P2-T04…P2-T08 is being exposed. **Incremental, per destination.**
- **Precise scope:** for each destination that is genuinely implemented and usable: register the
  Module definition in `ModuleRegistrations.CurrentBuildAvailable`; register the real route in
  the `IDestinationRouteRegistry` seam (extend the composition, do not create a second registry);
  verify granted + available + non-contextual + routed all hold before it appears in navigation;
  verify direct-route denial for every non-granted caller; wire cross-module links and document
  availability actions.
- **Explicit non-scope:** registering a Module before its surface exists; creating placeholder
  routes/pages; making Ferramentas top-level; exposing HISTÓRICO GLOBAL.
- **Expected files/projects:** `src/DMO.Application/Access/ModuleRegistrations.cs` (explicit,
  reviewable diff), the route-registry composition seam, feature pages as already delivered by
  P2-T04–P2-T08; integration tests in `tests/DMO.IntegrationTests/Navigation/`.
- **Access requirements:** unchanged model; only availability registration changes, and only
  honestly.
- **Persistence requirements:** none.
- **Required tests:** see §11 (P2-T10 row).
- **Acceptance criteria:** a newly registered destination appears only when granted **and**
  available **and** non-contextual **and** routed; a non-granted caller still receives the
  documented denial on the direct route; zero-destination builds keep
  `Sem destinos operacionais disponíveis`; `Ferramentas` never appears top-level.
- **Completion evidence:** the per-destination registration commit + the passing navigation and
  direct-route test set.
- **Downstream dependents:** none.

---

## 8. Shared Data / Domain Relationships

Traced before any table/entity is planned, so that modules that merely appear separately in the
UI do not acquire duplicate domain models. Authority:
`contracts/IDENTITIES_AND_RELATIONSHIPS.md`, `architecture/BACKEND_FRONTEND_MODEL.md`,
`architecture/CROSS_MODULE_FLOWS.md`, `architecture/RECORD_LIFECYCLES.md`,
`dmo-master/global/INFORMATION_MODEL.md`.

| Object | Identity | Source of truth / owner | Lifecycle | Live vs snapshot | Consumers | Historical requirement | Duplication verdict |
|---|---|---|---|---|---|---|---|
| Tool | `tool_id` | Ferramentas (canonical Tool master) | stable identity; edits via change-request in full system; **different lot = different Tool** | live Tool facts; Job On contexts freeze the values actually used | Job On, Controlo, Boquilhas, (later Armazém) | Job On CM/MF/BQ contexts preserve the values used; live Tool changes must not rewrite them | **No duplication** — one Tool registry; origin module never becomes Tool-data owner |
| Job On | `jobon_id` | Job On (one production occurrence) | **no** persisted Job On-wide status machine; date-threshold edit is a warning; delete forbidden with dependent facts | saved occurrence; duplication creates a **new** `jobon_id` | Controlo, Boquilhas, Pegamentos, documents | frozen CM/MF/BQ context per occurrence | **No duplication** — explicit `production_id`/`job_on_revision_id` forbidden |
| CM/MF/BQ context | `cm_id` / `mf_id` / `bq_id` | Job On (production-specific Tool context) | created only where the corresponding Tool context is actually needed | historical still for that occurrence | Peso (via `cm_id`), Pegamentos, Boquilhas (via `bq_id`) | freeze the selected Tool relation + required captured values | **No duplication** — must retain direct relation to canonical `tool_id`; context ≠ Tool |
| Peso | `peso_id` | Controlo | `Pendente` → `Aprovado` \| `Não aprovado`; reopen on the same record | preserved measurement/calculation/approval facts | Create, Approve, documents, HISTÓRICO GLOBAL (later) | preserved result facts + `cm_id` context | normal production path `peso_id -> cm_id`; **do not** also persist `tool_id + jobon_id` merely for navigation |
| Peso pending association | `peso_id -> tool_id` | Controlo | condition `Job On por associar`, not a status; later explicit association clears the direct anchor | truthful anchor to the actually controlled Tool | Create, Approve | must show the relation actually recorded | **No fake** Job On/`cm_id`, no second identity |
| Comparação | relation `current_peso_id -> previous_peso_id` | Controlo (Peso workflow) | explicitly selected; stale after reading change → rebuild before submit | explicit persisted relation; previous record immutable | Create, Approve | persists the exact pairing | **Not a status, not a separate identity** |
| Pegamentos | `pegamentos_id` | Controlo | may legitimately be absent; `NotEvaluable` when nominal is missing | nominal/limits actually used are preserved | Controlo, documents | preserved closed control must not drift with live Tool changes | **No invented nominal** |
| Folha de Controlo | `controlo_sheet_id` | Controlo | persisted component decisions/observations | persisted facts | Controlo, Approve | preserved decisions | **distinct from `resumo_id` — must not be merged** |
| Resumo | `resumo_id` | Controlo | persisted record for one `jobon_id` context | record is authority even when the PDF is absent | Controlo, documents | record ≠ its PDF | **No projection-of-Folha** |
| Boquilhas aggregate | `boquilhas_id` | Boquilhas | active → close (immutable snapshot) → archived → optional reopen | movements are append-only facts; balance derived | Boquilhas, documents, HISTÓRICO GLOBAL (later) | close snapshot + reopen actor/time/reason; movements never rewritten | production-linked via `bq_id`; standalone via direct `tool_id`; **no fake Job On**, **no per-piece identity** |
| Movement | `movement_id` | Boquilhas | append-only; `Editar` is an action producing audit, not a new movement | business date (editable) ⊥ recorded timestamp (immutable) | Boquilhas, HISTÓRICO GLOBAL (later) | before/after audit + actor + system timestamp | **no second balance authority** |
| Repairer | `repairer_id` | canonical repairer vocabulary | selected per external Saída | historical movements retain their repairer | Boquilhas | not rewritten when the directory/default changes | **not administered by Boquilhas** |
| Document output | derived (no identity) | owning record's workflow | availability state, not a domain lifecycle | frozen output corresponds to a frozen record state | Controlo, Boquilhas | rendered from preserved historical context | **filename/path is never an identity; no document table for symmetry** |

**Cross-cutting rules established by this map.**
1. `templates`/`template_modules` model **access** only; they are never reused as operational
   storage.
2. Reverse navigation is query-based — **no** `tool.jobons[]`-style arrays.
3. `processo` is owned by the canonical Tool and consumed through `cm_id -> tool_id`; Peso may
   preserve the configuration actually used but does not become a second process owner.
4. Only identities required by a workstream are created; empty context identities are not
   created for symmetry.
5. `src/DMO.Application/Access/ModuleCatalog.cs` is the reusable identity/catalog mechanism.
   Operational modules **consume** it (for gates and availability); they must not build a
   parallel registry.

---

## 9. Route / Module Availability Plan

**Governing rule (unchanged from the reconciliation and Beta `ACCEPTANCE_MATRIX.md` §9):**
a Module appears in `ModuleRegistrations.CurrentBuildAvailable` only when its destination is
genuinely implemented and usable — registered + available + non-contextual + routed + gated.
Never ahead of the surface.

| Destination | Module identity(ies) | Route | Becomes available in | Route registered in |
|---|---|---|---|---|
| Job On | `job-on-view`, `job-on-create` (share `job-on`) | `/jobon` (or the accepted contract path) | P2-T04 complete | P2-T10 (per destination) |
| Controlo | `controlo-create`, `controlo-approve` (share `controlo`) | `/controlo` | P2-T05 for Create; P2-T06 adds Approve actions on the same destination | P2-T10 |
| Boquilhas | `boquilhas` | `/boquilhas` | P2-T07 complete | P2-T10 |
| Ferramentas | `ferramentas`, `ferramentas-approve` | **no top-level route, ever** | never top-level; contextual entry inside Job On/Controlo/Boquilhas | n/a |
| HISTÓRICO GLOBAL | `historia` (technical identity; do not rename in code) | **no route in this Beta** | never in this Beta (DEFERRED BY DESIGN — §3.1) | n/a |
| Armazém / Reparação Interna / Reparação Programada / Tampões | respective identities | not planned here | outside Beta scope (`BETA_SCOPE.md`) | n/a |
| Administration | not a Module (`dmo.administration`) | `/Administration` (+ Users/Templates) | already live and accepted | already live |

**Interim state that must hold after every Phase 2 workstream:** if no destination is live,
navigation renders `Sem destinos operacionais disponíveis` and
`ModuleRegistrations.CurrentBuildAvailable` remains `[]` for the modules that are not yet real.
Partial registration is expected and correct (e.g. Job On available while Controlo is not).

---

## 10. Access Plan

No change to the authorization model is planned or permitted. The remaining work **consumes**
the accepted P1-T04/P1-T05 foundation.

| Element | Value | Notes for remaining work |
|---|---|---|
| Module policies | `dmo.module.<module-id>`, one per `ModuleCatalog` identity | Feature routes/actions declare the policy for their canonical Module. `ModuleAuthorizationPolicies` generates them — do not hand-maintain. |
| Administration policy | `dmo.administration` (deliberately outside `dmo.module.*`) | ADMIN-account functionality only; not a Module. Unchanged. |
| Shared-destination modules | `job-on-view`+`job-on-create`; `controlo-create`+`controlo-approve`; `reparacao-programada-view`+`-create` | One visible destination may carry several grants; the server-side gate still decides actions. Never merge permissions. |
| Contextual-only modules | `ferramentas`, `ferramentas-approve` | Zero top-level destinations. Opening a Tool ficha from another module grants nothing by itself. |
| Default / landing | Template `landing_destination_id` | Resolved by the accepted `LandingSelector`/`UserLandingService`; invalid explicit landing fails closed. Unchanged. |
| USER login identifier | `company_number` + password | Unchanged. |
| ADMIN login identifier | `email` + password; exactly one ADMIN; no operational Template; no super-user bypass | Unchanged. |
| Role label | presentation-only free text | Never grants access. Unchanged. |
| Availability | `ModuleRegistrations.CurrentBuildAvailable` | Honest, per-destination registration in P2-T10. |

---

## 11. Test Strategy

Test requirements are specified per workstream. They were **not** executed in this planning run.

Legend: **U** = unit, **I** = integration/host, **UI** = rendered/component or browser-level, **R** = regression on protected foundation.

### P2-T01 — shared generic states + `RecordStatus` + `AvailabilityState`
- **U:** every one of the ten presentation states is representable and carries distinct semantics; `empty`, `lookup-failed`, `unavailable`, `permission-denied` are mutually distinguishable in the presentation model.
- **U:** `RecordStatus` always carries text; an unknown status selects the neutral tone; status grants no action and infers no lifecycle.
- **U:** `AvailabilityState` keeps all **eight** states distinct; `not-applicable` is not styled as an error; `file-missing` is not `not-generated`; `lookup-failed` is not no-file/no-record.
- **UI:** rendered markup contains visible text (not color-only) for status and availability; disabled reasons are programmatically associated.
- **R:** every existing test in `DMO.UnitTests` and `DMO.IntegrationTests` still passes; `SharedShellTests` and `NavigationProjectionServiceTests` behavior unchanged.

### P2-T02 — `DenseDataTable` + `AuditTrail`
- **U:** single-click selects without invoking open; double-click/Enter invokes open only when the consumer supplies it; Space selects; two invocations of the same open collapse to one.
- **U:** `AuditTrail` never fills a missing actor/timestamp from the current session.
- **UI:** keyboard traversal of the table; focus survives re-render; selected state is programmatic; empty vs lookup-failed remain distinct in rendering.
- **R:** shared-shell tests and navigation projection tests unchanged.

### P2-T03 — `ToolPicker` + `ToolSummaryRow` + `MeasurementRows` + `DecisionBar`
- **U:** `ToolPicker` never auto-selects, **including with exactly one candidate**; Enter on search does not select the first result; Escape requests cancel and never discards origin work; cancel restores origin with the opaque origin token preserved.
- **U:** `MeasurementRows` enforces the configured minimum (including at-least-one) and exposes the removal-disabled reason; row identity is stable across add/remove/re-render.
- **U:** `DecisionBar` prevents duplicate invocation while pending; disabled reason is retained.
- **U/architecture:** no shared contract type or fixture declares an opaque key as `tool_id`/`jobon_id`/any canonical identity.
- **UI:** focus moves to the first editable control of a new row; focus returns to the nearest surviving control after removal; focus returns to the invoking control after picker close/cancel.
- **R:** no feature namespace/service is referenced from shared frontend types (`SharedFrontendSource_DoesNotImportFeatureServices` remains green).

### P2-T09 — secondary navigation + current marking
- **U:** a supplied secondary list renders; `IsCurrent` marks exactly the matching primary destination; empty secondary renders nothing.
- **I:** ADMIN and denied USER still project zero operational destinations; no authorization decision is taken in presentation.
- **R:** `RootRoutingTests`, `NoAccessPageTests`, `LoginPageTests`, `DirectRouteEnforcementTests` unchanged.

### P2-T04 — Tool identity + Job On Light + Ferramentas Light
- **U (domain/application):** different lot implies a different `tool_id`; ambiguity never auto-resolves; duplication creates a new `jobon_id` **and** new context IDs while the source is unchanged afterwards; retained Tool choices keep the same `tool_id` until explicitly changed; no Job On lifecycle status exists; the date-threshold edit is a warning; delete is refused when dependent operational facts exist.
- **I:** Job On create/read/edit round-trip; reference to productions query + explicit selection; CM/MF/BQ context create/reuse is idempotent for the same `jobon_id + tool_id`; Ferramentas Tool create returns a canonical `tool_id`; cancellation/return restores origin state.
- **I:** direct-route denial — Job On View does not reach Create actions; a non-granted caller receives the documented denial; ADMIN is not granted operational Job On.
- **UI:** `ToolPicker` never auto-selects in the real surface; the picker's create subflow restores the originating Job On state.
- **R:** `CurrentBuildAvailable` is only extended in P2-T10, never here; access and administration tests unchanged.

### P2-T05 — Controlo Create
- **U:** water-temperature range 5-35 C enforced; capacity and glass-weight formulas produce the authority-defined results; individual CM results remain visible (an average never hides a bad individual result); variable rows require at least one valid row; decimals are presentation-normalized without reducing calculation precision.
- **U:** previous Peso is never auto-selected; a cross-machine candidate remains valid when the same canonical Tool is compatible; a stale Comparison requires rebuild before submission; a missing nominal yields `NotEvaluable` and never invents a nominal; a boundary crossing warns but never blocks/approves/rejects.
- **I:** same `peso_id` from draft to submitted; pending association `peso_id -> tool_id` supported with no fake Job On/`cm_id`; later explicit association clears the direct anchor; Folha and Resumo persist as distinct records; Pegamentos may legitimately be absent.
- **UI:** dense Peso/Pegamentos/Folha/Resumo screens render supplied state; a missing required Tool context blocks that sheet with an actionable correction message and preserves draft state.
- **R:** Create cannot approve its own record; access tests unchanged; the shared Peso read model is published for P2-T06.

### P2-T06 — Controlo Approve
- **U:** warnings never trigger an automatic decision; per-CM decisions are explicit human facts.
- **I:** approve/reject/reopen mutate the same `peso_id`; submitted facts are read-only until an authorized reopen; reopen preserves the decision/attribution trail; the pending list returns only backend-reported reviewable facts; the exact persisted `previous_peso_id` relation is used (no heuristic).
- **I (access):** a Create-granted user without Approve is denied Approve actions on the shared `controlo` destination, and vice versa.
- **UI:** review mode reuses Create's read model (renderer contract test) — no forked renderer.
- **R:** all P2-T05 tests still pass; no approval-copy Peso is created.

### P2-T07 — Boquilhas
- **U:** exactly the four movement types are writable (`Início`, `Saída`, `Entrada`, `Irreparável`); `Editar` is not a movement type; Saída does not exceed available and Irreparável does not exceed in-repair; excess Entrada is recorded, not clamped; negative saldo remains visible and non-blocking; `% utilização` is never derived from movements; `business_date` is editable while `recorded_at` is immutable.
- **I:** editing a movement preserves before/after audit without creating a second quantity event and without double balance effect; the production-linked flow resolves/reuses `bq_id`; the standalone flow works with **no** fake Job On/`bq_id`; close/reopen retains the same `boquilhas_id` and full history; a failed close leaves the active state unchanged; the close snapshot is immutable; reopen records actor/time/reason; an external Saída stores the canonical `repairer_id` and it is not rewritten when the directory/default changes.
- **I:** balance is derivable from movement facts alone (no second mutable balance authority).
- **UI:** History filters/select/open; the movement selector shows only the four types; no mandatory PDF action and no internal settings tab.
- **R:** access tests unchanged; `CurrentBuildAvailable` untouched here.

### P2-T08 — documents / PDF
- **U:** the three availability distinctions hold (`file-missing` is not `not-generated`; `lookup-failed` is not empty; a missing optional Pegamentos is not an error); a filename is never used as an identity.
- **I:** the directory is `<reference>/<production-number>/`; filenames are `Peso_<reference>_<line>.pdf`, `Pegamentos_<reference>_<line>.pdf`, `Resume_<reference>_<line>.pdf`; a frozen official output is not silently regenerated from newer mutable facts; historical rendering uses preserved context after the source Tool changes.
- **I:** generated output contains no local filesystem path.
- **UI:** availability rendering distinguishes all supplied states; actions are enabled only when the operation can complete.
- **R:** owning-record access gates still decide document access; no artificial document identity/table appears.

### P2-T10 — final integration
- **I:** for each newly registered destination: it appears in navigation only when granted **and** available **and** non-contextual **and** routed; a non-granted caller still receives the documented denial on the direct route; a shared destination collapses to one entry while retaining all canonical grants; `Ferramentas` never appears top-level; zero-destination builds still render `Sem destinos operacionais disponíveis`.
- **R:** the protected regression set (`AccessResolverTests`, `ModuleAccessServiceTests`, `ModuleRegistryTests`, `ModuleAuthorizationHandlerTests`, `NoProfilesRegressionTests`, `RootRoutingTests`, `NoAccessPageTests`, `LoginPageTests`, `DirectRouteEnforcementTests`, `SharedShellTests`, `NavigationProjectionServiceTests`, `LandingSelectorTests`, `UserLandingServiceTests`) remains green after every registration step.

### Repository-wide verification discipline (for the implementing agent)
Run, in the repository's existing pattern: `dotnet build DMO.slnx`, then the unit project, then the integration project (PostgreSQL-gated cases only when a disposable database is provided), then the full solution. Record exact counts, the separation of skipped categories, and confirm `git diff --check` is clean. Local execution is developer evidence, not independent CI evidence (there is no `.github/` workflow in this repository).

---

## 12. Protected Work Register

Areas future agents must preserve. Default rule: **DO NOT MODIFY PROTECTED FOUNDATION.** If an extension is genuinely required, the exact seam is named; nothing else in the area may change.

| # | Protected area | Files | Named extension seam (if any) |
|---|---|---|---|
| 1 | Canonical Module vocabulary | `src/DMO.Application/Access/ModuleCatalog.cs` | none — identities/order/destinations frozen |
| 2 | Registry validation | `src/DMO.Application/Access/ModuleRegistry.cs` | none |
| 3 | Fail-closed resolution | `AccessResolver.cs`, `AccessOutcome.cs`, `ModuleResolve.cs` | none |
| 4 | Access facade | `ModuleAccessService.cs`, `IModuleAccessService.cs` | none |
| 5 | Server-side Module gate | `Authorization/ModuleAuthorizationPolicies.cs`, `ModuleAuthorizationHandler.cs`, `ModuleAuthorizationRequirement.cs` | features **name** policies; never hand-add a policy |
| 6 | ADMIN-only gate | `Authorization/AdministrationAuthorizationPolicies.cs`, `AdminAuthorizationHandler.cs` | none |
| 7 | Authentication/session | `Auth/SupabaseAuthenticationService.cs`, `SessionAuthentication.cs`, `CurrentAccountContext.cs`, `Endpoints/AuthEndpoints.cs` | none |
| 8 | Account resolution | `Accounts/AccountResolver.cs`, `AccountMatch.cs`, `AccountType.cs`, `NoAccessReason.cs`, `UserAccount.cs`, `AdminAccount.cs` | none |
| 9 | Current-account boundary | `Session/CurrentAccount.cs`, `ICurrentAccountContext.cs` | none |
| 10 | Persistence foundation | `Infrastructure/Migrations/20260922001736_*`, `...1757_*`, `DmoDbContext.cs`, foundation entities/configurations/repositories | new migrations are **new files**; never edit 001/002 |
| 11 | Template model | `Application/Templates/*`, `Infrastructure/Persistence/Template*` | none — `users.template_id` single relation is final |
| 12 | Template administration | `Application/TemplateAdministration/*`, `Pages/Administration/Templates/*` | none |
| 13 | USER administration | `Application/UserAdministration/*`, `Pages/Administration/Users/*` | none |
| 14 | Administration surface | `Pages/Administration/Index.cshtml(.cs)` | none |
| 15 | Root routing / landing / no-access | `Pages/Index.cshtml.cs`, `Navigation/LandingSelector.cs`, `Navigation/UserLandingService.cs`, `Pages/AccessDenied.cshtml(.cs)` | none — the fail-closed invalid-landing decision is settled |
| 16 | Navigation projection | `Frontend/Shell/NavigationProjectionService.cs` | none — filter/grouping algorithm frozen |
| 17 | Route seam | `Frontend/Shell/DestinationRoutes.cs`, `Navigation/DestinationRouteRegistrations.cs` | P2-T10 registers real routes through this single seam; never create a second registry |
| 18 | Shared shell | `Frontend/Shell/ShellPresentationModels.cs`, `ShellPresentationService.cs`, `Frontend/Shared/SharedFrontendExtensions.cs`, `Pages/Shared/_Layout.cshtml`, `_PublicLayout.cshtml`, `_Identity.cshtml`, `Pages/Shared/Navigation/*` | P2-T09 additive only; P2-T01...P2-T03 additive registrations |
| 19 | Shared tokens/shell CSS | `wwwroot/css/dmo-tokens.css`, `dmo-shell.css`, `dmo-user-shell.css` | new component CSS lives in a **new** `dmo-components.css`; do not rewrite existing selectors |
| 20 | A1 frozen contract | `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` | changes require the A1 section-16 contract-change protocol |
| 21 | Honest availability | `src/DMO.Application/Access/ModuleRegistrations.cs` | P2-T10 only, per real destination, explicit reviewable diff |
| 22 | Reviewed absence of provisional fixtures | production navigation | do not reintroduce `A2Fixtures`, `ProvisionalFixturesWhenNeeded`, `PROVISIONAL FRONTEND CONTRACT` markers, or fixture CSS |
| 23 | Existing tests | `tests/DMO.UnitTests/**`, `tests/DMO.IntegrationTests/**` | must keep passing; never weakened/deleted to force new work green |
| 24 | Out-of-Beta module identities | `ModuleCatalog` entries `armazem`, `reparacao-interna`, `reparacao-programada-*`, `tampoes`, `historia` | keep the identities; **no** routes, **no** availability, **no** deletion |

---

## 13. Final Integration Sequence

Per-destination, strictly after the destination is real. Each step is its own commit with its own verification. Never advance a step whose surface is incomplete.

```text
Step 1  P2-T00  Record the P1-T07 architecture acceptance (governance; no code).
Step 2  P2-T01  Shared generic states + RecordStatus + AvailabilityState.
Step 3  P2-T02  DenseDataTable + AuditTrail.
Step 4  P2-T03  ToolPicker presentation + ToolSummaryRow + MeasurementRows + DecisionBar.
Step 5  P2-T09  Secondary navigation + current-destination wiring (additive shell extension).
        -- shared primitives complete; CurrentBuildAvailable still [] --
Step 6  P2-T04  Domain core (Tool identity + Job On Light + Ferramentas Light).
                Ferramentas remains contextual-only; no top-level route.
Step 7  P2-T10a Register job-on availability + its real route; verify 4-condition gating
                and direct-route denial. Navigation shows Job On only for granted users.
Step 8  P2-T05  Controlo Create + shared Peso read model.
Step 9  P2-T10b Register controlo availability + route (Create actions live; Approve still
                denied because its Module is not yet available).
Step 10 P2-T06  Controlo Approve (consumes the shared Peso read model; no approval copy).
Step 11 P2-T10c controlo-approve Module availability registration (same destination).
Step 12 P2-T07  Boquilhas.
Step 13 P2-T10d Register boquilhas availability + route.
Step 14 P2-T08  Documents/PDF/directory/availability (gated by owning workflow permissions).
Step 15 P2-T10e Cross-module links, document actions, end-to-end verification across
                Job On <-> Controlo <-> Boquilhas <-> documents.
Step 16 Final full-suite verification and per-workstream acceptance evidence review.
```

**Never in this sequence:** exposing Ferramentas as a top-level destination; exposing HISTÓRICO GLOBAL; pre-registering a Module whose surface is incomplete; creating placeholder operational pages.

---

## 14. Explicit Non-Work

Investigated during this planning run and determined **not** to require implementation.

### 14.1 Out of Beta scope (global modules / future builds)
- **HISTÓRICO GLOBAL** (technical identity `historia`) — DEFERRED BY DESIGN; canonical identity preserved; no route; no availability; not renamed in code (section 3.1). Its data surface depends on RI/Armazém/RP identities that are themselves outside Beta scope. **This does not reduce any Beta module's local HISTÓRICO requirement** (P2-T05, P2-T06, P2-T07).
- **Armazém** — excluded by `BETA_SCOPE.md`. Identity preserved in the catalog; no work.
- **Reparação Interna** — same exclusion; identity preserved.
- **Reparação Programada (View + Create)** — same exclusion; identities preserved; also carries an open global authority point (`dmo-master/CURRENT_STATE.md` participation representation).
- **Tampões** — same exclusion; identity preserved. Global authority records Tampões as fully autonomous with no Job On relation; no Beta dependency exists.
- **Full Job On lifecycle** (revisions, verification catalogue, family sheets, print orchestration) and **full Ferramentas lifecycle** (change-request/approve dossier, technical-condition and utilisation history) — explicitly outside Beta (`modules/JOB_ON_LIGHT.md`, `modules/FERRAMENTAS_LIGHT.md`).
- **Admin audit** and any future Admin feature — not part of the Beta operational scope.

### 14.2 Investigated and requiring no change
- **Admin index logout form placement** — accepted endpoint; placement not Beta-established; no action (section 3.6).
- **Hardcoded shell status strings** — accepted shared-shell presentation; the fail-closed string is required by the accepted A2 correction; no action (section 3.6).
- **Stale READMEs** — documentation-only; folded into P2-T01 as a trivial slice, not treated as architecture.
- **Module catalog ordering** — already exactly matches `dmo-master/global/ACCESS_MODEL.md` section 1; no work (section 3.4).
- **`BETA_DESIGN_RECONCILIATION_PLAN.md`** — never committed (local untracked artifact); the accepted A1 freeze supersedes it and is canonicalized into `dmo-beta-master/contracts/SHARED_FRONTEND.md`; do not reconstruct or invent it (section 3.2).
- **Job On's three official document identities** (Ficha de Artigo, Job-On Moldes, Trabalho de Equipa) — owned by the global Job On contract; not required by Beta Job On Light; no Beta work.
- **A generic lifecycle/state engine** — explicitly forbidden by `architecture/RECORD_LIFECYCLES.md` section 1. Each record keeps its own semantics.
- **Any second access/navigation authority** — forbidden; the existing registry, resolver, policies and projection are the single authority.
- **New .NET projects without an approved need** — `docs/ARCHITECTURE.md` states no future project split is pre-authorised.
- **A second database context / generic repository abstraction / reverse-ID arrays / a `production_id` or `revision_id`** — forbidden by `docs/ARCHITECTURE.md` and `contracts/IDENTITIES_AND_RELATIONSHIPS.md`.

### 14.3 Deferred by design (later dependency stage, tracked)
- All of P2-T04...P2-T10 are deferred to a .NET-capable implementation environment. Nothing in this planning run authorizes their execution: each requires its own authored contract and Architect `PLAN ACCEPT` per `dmo-beta-master/WORKFLOW.md`.
- B1-B4 (section 4) are the concrete contracts to author first, in dependency order.

---

## Appendix A — Reconciliation Closure Ledger

| Reconciliation reference | Class | Disposition | Target |
|---|---|---|---|
| 7.1 A shared feature components | PARTIAL | PARTIAL: states/RecordStatus/AvailabilityState IMPLEMENTED (P2-T01); table/picker/rows/decisionbar remain | P2-T02, P2-T03 |
| 7.2 secondary nav / current marking | PARTIAL | READY FOR IMPLEMENTATION | P2-T09 |
| 7.3 access identities without feature actions | PARTIAL | READY FOR IMPLEMENTATION | P2-T04...P2-T07, P2-T10 |
| 7 (Q3) P1-T07 accepted-status gap | PARTIAL | CLOSED (P2-T00: ACCEPT review `50e8841…`) | — |
| 9.1 Job On create/view/edit | MISSING | READY FOR IMPLEMENTATION | P2-T04 |
| 9.2 Job On duplicate | MISSING | READY FOR IMPLEMENTATION | P2-T04 |
| 9.3 reference to productions | MISSING | READY FOR IMPLEMENTATION | P2-T04 |
| 9.4 Ferramentas Light Tool ficha | MISSING | READY FOR IMPLEMENTATION | P2-T04 |
| 9.5 Tool canonical identity | MISSING | READY FOR IMPLEMENTATION | P2-T04 |
| 9.6 Peso draft/measurement/submit | MISSING | READY FOR IMPLEMENTATION | P2-T05 |
| 9.7 Peso pending association | MISSING | READY FOR IMPLEMENTATION | P2-T05 |
| 9.8 Comparison | MISSING | READY FOR IMPLEMENTATION | P2-T05 |
| 9.9 Pegamentos | MISSING | READY FOR IMPLEMENTATION | P2-T05 |
| 9.10 Folha + Resumo | MISSING | READY FOR IMPLEMENTATION | P2-T05 |
| 9.11 shared Peso read model | MISSING | READY FOR IMPLEMENTATION | P2-T05 publishes, P2-T06 consumes |
| 9.12 Controlo Approve | MISSING | READY FOR IMPLEMENTATION | P2-T06 |
| 9.13 Boquilhas | MISSING | READY FOR IMPLEMENTATION | P2-T07 |
| 9.14 Boquilhas repairer | MISSING | READY FOR IMPLEMENTATION | P2-T07 |
| 9.15 documents/PDF | MISSING | READY FOR IMPLEMENTATION | P2-T08 |
| 9.16 Module availability registrations | MISSING | READY FOR IMPLEMENTATION | P2-T10 |
| 9.17 real destination routes | MISSING | READY FOR IMPLEMENTATION | P2-T10 |
| 8.1/8.2/8.3 stale READMEs | DIVERGENT (docs) | CLOSED (P2-T01 documentation slice) | — |
| 10.1 admin logout placement | UNAUTHORIZED (ambig.) | DO NOT IMPLEMENT | — |
| 10.2 shell status strings | UNAUTHORIZED (ambig.) | DO NOT IMPLEMENT | — |
| 10.3 unused shell slots | UNAUTHORIZED (ambig.) | DEFERRED BY DESIGN | P2-T01/P2-T03 consume |
| 11 A1 HISTÓRICO GLOBAL (recorded as História) | AMBIGUITY | RESOLVED, DEFERRED BY DESIGN | — |
| 11 A2 missing design plan | AMBIGUITY | RESOLVED, NON-BLOCKING | — |
| 11 A4 P1-T07 status | AMBIGUITY | RESOLVED, to P2-T00 | P2-T00 |
| 11 canonical order | AMBIGUITY | ALREADY SATISFIED | — |
| 11 Peso vocabulary | AMBIGUITY | RESOLVED | P2-T05/P2-T06 |

**Counts.** PARTIAL items dispositioned: **4/4**. MISSING items mapped: **17/17**. Ambiguities resolved: **7/7** (HISTÓRICO GLOBAL scope, missing design plan, P1-T07 status, canonical order, Peso vocabulary, plus the section-10 wiring set as three DO-NOT-IMPLEMENT resolutions). Ambiguities remaining BLOCKED BY AUTHORITY: **0**. Intra-workstream contract-authoring gaps (B1-B4): 4, none blocking planning.
