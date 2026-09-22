# BETA FIXED DESKTOP LAYOUT POLICY UPDATE — RESPONSE

## 1. Decision

**FIXED DESKTOP LAYOUT** is now binding frontend authority. The canonical design and validation
viewport is **1366 × 768**.

DMO is a fixed-layout desktop operational application, not a responsive public website.
Breakpoint-driven structural reflow is prohibited. Mobile and tablet layouts are out of scope.

## 2. Rationale

The decision prioritizes operational workflow stability, predictable control placement,
information density and consistency between modules. It reduces breakpoint-related regressions
and prevents a learned action, table column, filter or side panel from moving merely because the
viewport changes.

Larger desktop viewports preserve the same composition. Smaller windows preserve it through
page-level or local overflow rather than a different workflow layout.

## 3. Files Updated

- `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md`
- `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md`
- `plans/beta-workstreams/P2-T02-DENSE-TABLE-AUDIT-TRAIL.md`
- `plans/beta-workstreams/P2-T03-TOOLPICKER-ROWS-DECISIONBAR.md`
- `plans/beta-workstreams/P2-T04-DOMAIN-CORE-TOOL-JOBON.md`
- `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md`
- `plans/beta-workstreams/P2-T06-CONTROLO-APPROVE.md`
- `plans/beta-workstreams/P2-T07-BOQUILHAS.md`
- `plans/beta-workstreams/P2-T08-DOCUMENTS-PDF.md`
- `plans/beta-workstreams/P2-T09-SECONDARY-NAVIGATION-CURRENT.md`
- `plans/beta-workstreams/P2-T10-FINAL-INTEGRATION.md`
- `dev/responses/BETA_FIXED_DESKTOP_LAYOUT_POLICY_UPDATE_RESPONSE.md`

`P2-T01-SHARED-STATES-STATUS-AVAILABILITY.md` was inspected and left unchanged: P2-T01 is
already implemented and verified, contains no conflicting responsive assumption, and this task
must not reopen or modify that implementation.

## 4. Conflicting Responsive Assumptions Removed

No existing plan explicitly required mobile/tablet layouts, breakpoint-driven semantic layouts
or table-to-card conversion. The gap was absence of one binding viewport/layout authority, so
future agents could still infer responsive restructuring.

The one ambiguous shared-contract statement was:

> column descriptors: heading, accessible label, display alignment/priority, consumer-rendered cell content

It is now clarified: descriptor priority is presentation metadata only and is not authority to
hide, reorder or reflow a required column. The existing statement that horizontal overflow must
be keyboard reachable was retained and strengthened into the required solution for over-wide
tables: stable columns plus local horizontal scrolling.

Each future frontend handoff now expressly prohibits structural responsive variants and inherits
the canonical 1366 × 768 surface.

## 5. P2-T02 Impact

`DenseDataTable` must:

- be designed and browser-validated first at 1366 × 768;
- use compact row density;
- preserve required columns, column order and action placement;
- never convert to cards or hide required columns by breakpoint;
- use keyboard-reachable local horizontal scrolling when over-wide;
- retain the same operational structure at larger desktop resolutions.

`AuditTrail` inherits the same fixed desktop composition. P2-T02 remains unstarted.

## 6. Later Workstream Impact

The policy is inherited by:

- P2-T03 — ToolPicker, ToolSummaryRow, MeasurementRows and DecisionBar;
- P2-T04 — Tool, Job On and ProductionContextStrip;
- P2-T05 — Controlo Create, comparison, history and document surfaces;
- P2-T06 — Controlo Approve, pending list, audit/history and DecisionBar;
- P2-T07 — Boquilhas tables, filters, actions, side panel and History;
- P2-T08 — document/history/availability surfaces;
- P2-T09 — secondary navigation and current-destination presentation;
- P2-T10 — final viewport and integration verification.

## 7. Explicit Non-Changes

No domain behavior, data contract, authorization, module scope, route decision, module
availability, HISTÓRICO/HISTÓRICO GLOBAL semantics, backend architecture or protected
foundation was changed. The completed P2-T01 implementation was not modified.

## 8. Implementation State

- Application code modified: **NO**
- Frontend code modified: **NO**
- Planning/authority documentation only: **YES**
- P2-T02 implementation started: **NO**
- P2-T02 remains the next implementation workstream: **YES**
