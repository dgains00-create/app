# OWNER CLARIFICATION — PLANNING CONTEXT AND ASSOCIATION FLOW — RESPONSE

**Status:** OWNER CLARIFICATION REGISTERED — AUTHORITY ONLY. **No application code, no test, no
schema, no migration, no route, no authorization and no configuration was changed by this
clarification.**
**Baseline:** clean `c01d836` (DMO-MODULAR `main`).
**P2-T04, P2-T05, P2-T06, P2-T07 remain CLOSED as implementation workstreams.** This
clarification changes **authority wording** (contracts/plans); it does **not** reopen any closed
workstream and does **not** implement anything. Where it supersedes earlier wording, the
superseded wording is explicitly listed inside each affected contract/plan and marked as such.

## 1. Authority record

| Item | Value |
|---|---|
| Clarification source | Direct OWNER clarification (dictated design directives, registered on the clean baseline `c01d836`) |
| Affected contracts | `plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md` (§24), `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` (§31), `plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md` (§34) |
| Affected plans | `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` (§7 P2-T04/P2-T05/P2-T06/P2-T07 records; §8 map rows and cross-cutting rules), `plans/beta-workstreams/P2-T04-DOMAIN-CORE-TOOL-JOBON.md`, `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md`, `plans/beta-workstreams/P2-T07-BOQUILHAS.md` |
| Implementation | **NOT EXECUTED BY DESIGN** — this registration is planning/authority only. Every implementation consequence requires its own authored, reviewed contract before code (Beta `WORKFLOW.md` plan-gate) |
| Commit | recorded by the committing task (see the task report) |

## 2. Decisions registered (normative)

1. **Job On is the planning centre.** Job On creates `jobon_id` and the `cm_id`/`mf_id`/`bq_id`
   context snapshots, and the needed context **flows out to the consuming modules** — Controlo via
   `cm_id`, Boquilhas via `bq_id`. No consuming module re-creates or re-owns that context (P2-T04
   §24; confirmation only — this is already the closed P2-T04 model).
2. **Controlo receives the production through the Resumo.** The Peso is **populated** by
   `cm_id`/Job On with machine, reference, lot, processo and the CM context. The Resumo
   (`resumo_id → jobon_id`) is fixed as the Controlo **entry point** carrying that production
   context. The Resumo record itself remains an unimplemented P2-T05 handoff item (Q-SCOPE) — this
   clarification registers its role as authority; no table/route was created (P2-T05 §31.1).
3. **Peso pré-JobOn may use `tool_id`; correspondence by the same UUID.** A Peso created before
   its Job On/CM context may keep the truthful pending `tool_id` anchor; when the corresponding CM
   context exists, the correspondence is **the same `tool_id` UUID** (candidate `cm_id` whose
   `cm_contexts.tool_id` equals the Peso's anchor `tool_id`), and on association the record passes
   to `cm_id`. This pins the already-implemented P2-T05 associate rule (P2-T05 §31.2; confirmation
   only — no behavior change).
4. **Boquilhas may start work pré-JobOn linked to `tool_id`; association at the matching
   `bq_id`.** When Boquilhas receives context with a `bq_id` whose **master `tool_id`** matches,
   that is the point to present/resolve the association of the register to that `bq_id`; after
   resolution the register becomes `bq_id → jobon_id`. This **does NOT recreate a permanent
   standalone**: the `tool_id` anchor is a provisional, transitional anchor; the settled register
   remains production-linked (one register per real `bq_id`) (P2-T07 §34; **supersedes** the
   affected §33.1 wording "there is NO standalone (`tool_id`) anchor").
5. **Repairers and line/machine → repairer associations belong to `Boquilhas > Definições`** —
   not to Controlo, not to Admin. This **supersedes** the settled delta ownership
   (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §3/§4 and the P2-T05/P2-T07 passages
   that made Controlo_Create → Definições the repairer-register owner) for the repairer family
   only. PDF-directory, email-list and email-template settings are **not** moved by this
   clarification; they remain under `Controlo_Create → Definições`. The implemented physical
   `repairers`/`machine_repairer_assignments` tables and their current routes stay in place until a
   future workstream contract re-homes them (P2-T05 §31.3, P2-T07 §34.2).
6. **Beta Ferramentas can be created without Armazém.** Absence of an Armazém location never
   blocks creation/use of `tool_id` (P2-T04 §24; confirmation only — already the closed P2-T04
   model; Armazém owns location truth and is outside Beta).
7. **Surgical queries / light packets (standing rule).** Queries and read models must be
   context-specific and carry only the data the surface needs; no global scans, no loading whole
   relations to filter in the frontend. Binding for all remaining and future Beta backend work
   (registered in the master plan §8 cross-cutting rules).
8. **Controlo Create and Controlo Approve remain distinct modules.** No generic architecture may
   be imposed that forces their workflows/pages to be identical. The two-module split (separate
   identities, separate gates, sibling-non-satisfaction) is preserved (registered in the master
   plan §7 P2-T06 record).

## 3. Concrete conflicts found and how they were dispositioned

| # | Conflict | Disposition |
|---|---|---|
| C1 | **Boquilhas §33.1 item 1** ("there is NO standalone (`tool_id`) anchor: every Boquilhas register belongs to a REAL Job On / production") vs **decision 4** (pré-JobOn work may start on `tool_id` until a matching `bq_id` associates it) | **SUPERSEDED** by contract §34 (new Owner clarification). The permanent-standalone model stays removed; only a **provisional transitional** `tool_id` anchor is introduced, resolving to `bq_id → jobon_id` on master-`tool_id` match. Affected wording listed in §34.2. |
| C2 | **Repairer ownership**: the settled delta (`CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §3/§4) and P2-T05 §9/§10/§11 (+ master plan §7/§8) make `Controlo_Create → Definições` the owner of the repairer register and the per-machine repairer assignments; **decision 5** moves the repairer family to `Boquilhas > Definições` | **SUPERSEDED for the repairer family** by P2-T05 §31.3 / P2-T07 §34.2. The delta report itself is a report, not a contract/plan, and was **not edited** (the user directive limited edits to contracts/plans; the supersession is recorded in the contract/plan layer and here). PDF/email settings stay with Controlo. No physical move now. |
| C3 | **Resumo entry point**: the Resumo is an explicitly unauthorized P2-T05 handoff remainder (Q-SCOPE: no `resumo` table/route exists) while **decision 2** fixes it as the Controlo entry point | **Registered as authority, not implemented.** P2-T05 §31.1 records the role; the implementation of the Resumo remains a future authored contract (same workstream, separate PLAN ACCEPT per Q-SCOPE). No table/route was created. |
| C4 | **"Boquilhas has no settings surface and no internal Definições tab"** (P2-T07 §1.1 row 2, master plan §7 P2-T07 non-scope) vs **decision 5** (Boquilhas > Definições owns the repairer family) | **SUPERSEDED for the repairer family.** A Boquilhas Definições surface (repairer register + line/machine → repairer associations) becomes authority; its exact shape/routes are a future contract. The rest of the Boquilhas non-scope (PDF, availability, job planning ownership…) is unchanged. |
| C5 | **Controlo_Approve "owns no operational setting"** (master plan §7 P2-T06) vs **decision 5** moving repairer settings to Boquilhas | **Consistent, not a conflict.** Controlo_Approve still owns no settings; the repairer family leaves Controlo_Create for Boquilhas. The P2-T06 statement is preserved and the "not owned by Controlo_Approve" part remains true under both owners. |
| C6 | **P2-T04 machine-context note** ("repairer/machine-to-repairer configuration belongs to Controlo_Create → Definições (P2-T05)") and **P2-T05 workstream §6 non-scope** (repairers owned by Controlo_Create) vs **decision 5** | **SUPERSEDED for the repairer family** — the affected sentences carry a supersession pointer to the clarification record (see the P2-T04/P2-T05 workstream updates). |

## 4. What was changed (complete list)

| File | Change |
|---|---|
| `plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md` | new **§24** — OWNER CLARIFICATION: Job On planning centre + context flow; Ferramentas creation without Armazém (confirmations; no supersession) |
| `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` | new **§31** — OWNER CLARIFICATION: Resumo entry + Peso population (decision 2), pending-`tool_id` → `cm_id` correspondence (decision 3), repairer ownership transfer (decision 5; supersedes §9/§10/§11 ownership wording), Create/Approve remain distinct (decision 8) |
| `plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md` | new **§34** — OWNER CLARIFICATION: provisional pré-JobOn `tool_id` anchor + `bq_id` association on master-`tool_id` match (supersedes affected §33.1 wording), repairer family owned by `Boquilhas > Definições` (supersedes affected §1.1 wording) |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | registered-authority note after the delta paragraph; §7 records for P2-T04/P2-T05/P2-T06/P2-T07; §8 map rows + cross-cutting rules updated (repairer family ownership; Resumo entry; Boquilhas provisional anchor; surgical-reads standing rule) |
| `plans/beta-workstreams/P2-T04-DOMAIN-CORE-TOOL-JOBON.md` | §5.3 record + supersession pointers on the affected non-scope sentence |
| `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` | §5.2 clarification record + supersession pointer on the affected non-scope sentence |
| `plans/beta-workstreams/P2-T07-BOQUILHAS.md` | §3/§4 clarification record + supersession pointers on the affected flow/non-scope wording |
| `dev/responses/OWNER_CLARIFICATION_PLANNING_CONTEXT_AND_ASSOCIATIONS_RESPONSE.md` | this response (new) |

## 5. Explicit non-change

- **No code, no test, no migration, no route, no authorization, no configuration** was added,
  removed or modified.
- `ModuleRegistrations.CurrentBuildAvailable` stays `[]`; `DestinationRouteRegistrations` stays
  empty; no destination is registered or made available.
- No closed workstream (P2-T04…P2-T07) is reopened; no P2-T08/P2-T09/P2-T10 authorization is
  implied or granted.
- The `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` report is **not edited** — its
  affected ownership passages are superseded by this clarification in the contract/plan layer
  (see C2).
- No redesign: no new identity, no new table shape, no new grouping/lifecycle/balance/role
  machinery is introduced by this registration.