# BETA_PLAN_CURRENT_STATE_CLEANUP_RESPONSE

**Task:** clean the current Beta plan state — remove superseded implementation authority from the
Beta planning/governance documents and bring them up to the CURRENT implemented state.

**Task class:** documentation/governance only. **No production code changed** (no domain,
application, infrastructure, web, migration, test, route or availability modification).

Scope respected: not a new audit, not a redesign, not an architecture review, not implementation,
not a contract rewrite. No availability or route registration was performed.

---

## Files changed

| File | Change |
|---|---|
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | Primary cleanup target (see below). |
| `plans/beta-workstreams/P2-T04-DOMAIN-CORE-TOOL-JOBON.md` | Status records updated; snapshot clarification recorded final (reviewed VERIFIED). |
| `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` | Stale "AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW" status removed; CLOSED recorded; final accepted behavior preserved. |
| `plans/beta-workstreams/P2-T06-CONTROLO-APPROVE.md` | Stale "AWAITING" status removed; CLOSED recorded; final accepted behavior preserved. |
| `plans/beta-workstreams/P2-T07-BOQUILHAS.md` | Active scope/authority replaced with the final CLOSED model; superseded records explicitly marked HISTORICAL; final §17 closure record added. |
| `plans/beta-workstreams/P2-T02-DENSE-TABLE-AUDIT-TRAIL.md` | Minimal status-marker correction only: the settled authority declares P2-T02 CLOSED, so the file's "IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION" and "P2-T03 remains NOT AUTHORIZED" claims were updated. Scope text untouched. |
| `plans/beta-workstreams/P2-T03-TOOLPICKER-ROWS-DECISIONBAR.md` | Minimal status-marker correction only: top call-out "CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW … Nothing in P2-T03 is implemented" marked historical (the contract was PLAN ACCEPTED and P2-T03 is implemented/CLOSED per the settled authority). Scope text untouched. |
| `dev/responses/BETA_PLAN_CURRENT_STATE_CLEANUP_RESPONSE.md` | This response (new file). |

No completed contract document (`plans/contracts/*`) was rewritten. Contract documents are
historical artifacts; the P2-T07 contract already carries its own §33 "SUPERSEDES" banner at the
top, and the master plan (§4/§7) is now the unambiguous current-state authority for every status
those documents record at authoring time.

---

## Stale P2-T07 rules removed/demoted

All of the following were removed from active/current-state sections of the master plan and the
P2-T07 workstream, and kept (where relevant for provenance) only under explicit
**SUPERSEDED / HISTORICAL ONLY** headings:

- standalone Boquilhas flow (`boquilhas_id -> tool_id`)
- `Início` movement type
- `Irreparável` movement type / bucket
- active aggregate summary / active/closed lifecycle
- close/reopen on the same `boquilhas_id`
- immutable close snapshot
- reopening table / reopen eligibility machinery
- `boquilha_close_snapshots` / `boquilha_reopenings` / `boquilha_machines` (six-table schema)
- 18 routes (3 pages + 15 endpoints)
- four-bucket balance model (Disponível/Em reparação/Irreparável/Entrada excecional)
- active-anchor partial unique B1 indexes (`IX_boquilhas_active_bq_id`/`IX_boquilhas_active_tool_id`)
- one-active-aggregate race logic / `Refused(ActiveAggregateExists)` mapping / K6–K8 race rows
- "IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION REVIEW"
- "awaiting ONE independent review … then close if VERIFIED"
- "Architect implementation review" as a pending P2-T07 gate

Replaced by the final CLOSED authority in every current-state location:

- production-linked movement register: one register per real `bq_id` / Job On context
  (`boquilhas_id -> bq_id -> jobon_id + tool_id`)
- movements remain valid after the production end date
- exactly three movement types: `saida`, `entrada`, `entrada_sem_reparacao`
- outstanding derived by replay: `SUM(saida) − SUM(entrada) − SUM(entrada_sem_reparacao)`
- no lifecycle (no active/closed/reopen), no standalone, no close/reopen
- 3 tables (`boquilhas`, `boquilha_movements`, `boquilha_movement_audit`)
- 15 routes (3 pages + 12 endpoints), all gated `dmo.module.boquilhas`
- final independent review `a96814f…` **VERIFIED** (`reports/P2_T07_FINAL_INDEPENDENT_REVIEW.md`);
  under the simplified workflow no additional Architect implementation review is required
- **CLOSED**

Locations updated: master plan §4 (B3 rows + blocker-status table), §5 dependency graph, §6
workstream index, §7 P2-T07 section (authority, starting point, precise scope, persistence,
acceptance criteria, completion evidence, status + historical block), §8 Shared Data rows
(Boquilhas register, Movement), §11 P2-T07 required tests, §13 Final Integration Sequence status,
§14.3 note, Appendix A rows 9.13/9.14 + B2 + counts; workstream file §1/§2/§3/§4/§5/§9/§10/§11/§12/
§14/§15/§16, with a new §17 closure record.

---

## P2-T04 snapshot rule recorded

Current Job On / context authority is now unambiguous in the master plan (§7 P2-T04 OWNER
CLARIFICATION block and §4 B1 rows) and the P2-T04 workstream (§5.2):

- every **new** Job On — created normally **OR** by duplication — creates **NEW context
  snapshots**: `tool_id_CM → new cm_id`, `tool_id_MF → new mf_id`, `tool_id_BQ → new bq_id`;
- duplication reuses canonical `tool_id` identities, **never** previous context IDs
  (`cm_id`/`mf_id`/`bq_id`);
- new snapshots use the **CURRENT** canonical Tool state;
- old context snapshots (and the source Job On) remain immutable;
- no `production_id`, no `job_on_revision_id`, no rollover entity, no Tool history table, no sync
  framework;
- prior wording that duplication clones the source context's frozen triple is marked
  **SUPERSEDED** (master §4 B1 row; workstream §5.2).

**Focused review status (reality check performed against the repository):** the task brief
expected the focused independent review to be "pending", with the guardrail "do not mark the
focused review as complete **unless a review commit actually exists**". A review commit **does
exist** on `main`: `787f5a9` — "P2-T04: FOCUSED INDEPENDENT REVIEW of the Owner clarification
(Job On context snapshot invariant - VERIFIED)" — with report
`reports/P2_T04_JOB_ON_SNAPSHOT_INVARIANT_REVIEW.md` (verdict **VERIFIED**; "OWNER CLARIFICATION
CLOSED"). Per the guardrail, the documents therefore record the focused review as **COMPLETE —
VERIFIED** (`787f5a9…`), implementation `d2b3b3c…`, governance/response head `b7457ec…`.

Current recorded status: **P2-T04 CLOSED** + snapshot Owner clarification **IMPLEMENTED** +
focused independent review **VERIFIED (787f5a9…)**. This clarification does **not** reopen
P2-T04. The master plan's REMAINING BETA WORK section therefore lists item 1 as COMPLETE (with
the evidence) so no future agent schedules a duplicate review.

---

## P2-T05 status

**CLOSED.** Stale "IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION
REVIEW" wording removed (master plan §7 and workstream §5.1). Final accepted behavior preserved
unchanged: Peso with the same-`peso_id` lifecycle, `Controlo_Create → Definições` (repairer
register, independent per-machine repairer assignments for `B1 B2 B3 C1 C2 C3`, PDF base
directory setting, email lists, email templates), glass density settings, water-density behavior,
shared Peso read model. The post-closure **glass-density Owner correction** and the
**water-temperature → water-density lookup** correction remain recorded as implemented and
independently verified (VERIFIED `7afcb00…`).

## P2-T06 status

**CLOSED.** Stale "IMPLEMENTED — AWAITING INDEPENDENT VERIFICATION / ARCHITECT IMPLEMENTATION
REVIEW" wording removed (master plan §7 and workstream §13). Preserved: Aprovar + Histórico de
Pesos, same `peso_id`, approve/reject/reopen, shared Peso read model (renderer-parity), no
Definições in Approve, P2-T08 owns real document/send execution. Final independent re-verification
of the CP4 correction recorded VERIFIED (`8f9e4f8…`).

## P2-T07 status

**CLOSED** — final independent review `a96814f…` **VERIFIED**
(`reports/P2_T07_FINAL_INDEPENDENT_REVIEW.md`), final model as summarised above
(production-linked movement register, 3 types, derived outstanding, no lifecycle/standalone/
close-reopen, 3 tables, 15 routes). `CurrentBuildAvailable` stays `[]`.

## P2-T08 status

**NOT IMPLEMENTED / NOT AUTHORIZED** (unchanged). B4 remains OPEN; no contract authored; no
documents/PDF/files/email code. Remaining scope kept in the master plan: Peso PDF, Pegamentos PDF
where applicable, Resumo PDF, configured base directory (`<reference>/<production-number>/`,
deterministic filenames), availability states, historical rendering from preserved facts,
configured email lists/templates, preview/send behavior as eventually contracted. **Not
implemented in this task.**

## P2-T09 status

**PLANNED / NOT IMPLEMENTED** (unchanged). Purpose: secondary navigation + current-destination
wiring (`IsCurrent` producer). Explicit status line added to the master plan §7.

## P2-T10 status

**NOT IMPLEMENTED** (unchanged). `CurrentBuildAvailable` remains `[]` (verified in
`src/DMO.Application/Access/ModuleRegistrations.cs`), `DestinationRouteRegistrations` remains
empty. P2-T10 still owns: real destination availability, real route registration, navigation
exposure, cross-module integration, final end-to-end. **Nothing was registered in this task.**

## CurrentBuildAvailable

`[]` — confirmed unchanged (`ModuleRegistrations.CurrentBuildAvailable = []`; code untouched).

---

## Remaining Beta work (as recorded in the master plan)

1. **Focused independent review of the Job On snapshot Owner clarification** — **COMPLETE /
   VERIFIED** (`787f5a9…`; `reports/P2_T04_JOB_ON_SNAPSHOT_INVARIANT_REVIEW.md`). Implementation
   was already complete; no new functionality was involved.
2. **P2-T09** — secondary navigation / current-destination wiring. PLANNED / NOT IMPLEMENTED.
3. **P2-T08** — documents/PDF/files/email consumption. NOT IMPLEMENTED.
4. **P2-T10** — availability/routes/navigation/cross-module integration. NOT IMPLEMENTED.
5. **Final full-suite / end-to-end verification** — after P2-T08/P2-T09/P2-T10 land.

Everything else in the implemented Beta path is **CLOSED** (P2-T00…P2-T07).

---

## Verification sweep

Searched the planning documents for the stale phrase list — `standalone`, `Início`,
`Irreparável`, `close/reopen`, `active aggregate`, `boquilha_close_snapshots`,
`boquilha_reopenings`, `18 routes`, `6 tables`, `awaiting independent verification`,
`awaiting Architect implementation review`:

- **Master plan and the touched workstreams:** every remaining hit is either (a) a negation of the
  final model ("**no** standalone flow", "no `Início`/`Irreparável`", "no close/reopen", "no
  active-anchor partial unique indexes"), (b) inside an explicitly-labelled
  **SUPERSEDED / HISTORICAL ONLY** record (master §4 B3 row, §7 P2-T07 historical block, §14.3,
  workstream §14–§16), or (c) unrelated (e.g. `AvailabilityState` value `awaiting-approval` in
  P2-T01; "not a standalone module identity" in master §0.1 — HISTÓRICO terminology).
- **Contract documents** (`plans/contracts/*`) retain their historical status lines and
  superseded design content by design — they are frozen artifacts of the moment they were
  authored/accepted; the P2-T07 contract additionally carries its §33 SUPERSEDES banner, and the
  task instructed not to rewrite completed contracts. The master plan is the current-state
  authority and explicitly supersedes those statuses.
- `CurrentBuildAvailable = []` confirmed; P2-T08/P2-T09/P2-T10 confirmed not implemented.

No production code, test, migration, route or availability was changed (`git diff` covers
`plans/**` and `dev/responses/**` only).

---

Response record: committed at `43b7ab4` and pushed to `diogo-o/DMO-MODULAR` remote `main`
(787f5a9..43b7ab4). Working tree clean.