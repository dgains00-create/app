# P2-T05 — Controlo Create (Peso, Comparação, Pegamentos, Folha, Resumo) + Shared Peso Read Model — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T05), §8, §9, §10, §11, §12.
Class: **Operational module** (Workstream C, Create side).
Depends on: **P2-T04** (canonical Tool + Job On context) and **P2-T03** (`MeasurementRows`).
Authority blocker: **B2** — an authored, reviewed `PLAN ACCEPT` contract must exist first.

## Binding fixed desktop layout

This handoff inherits the master plan's **DMO FIXED DESKTOP LAYOUT POLICY**. Controlo Create,
its production context, measurement rows, comparison/history/document surfaces and actions are
designed first at **1366 × 768**. Their structural placement stays stable at larger desktop
resolutions. Smaller windows use page or local table/region scrolling; no breakpoint may stack
or reorder the workflow, convert tables to cards, hide required columns or relocate actions.
Mobile/tablet variants are out of scope.

## 1. Purpose

Deliver the Create side of Controlo: Peso, Comparação, Pegamentos, Folha and Resumo, and
publish the canonical read-only Peso read model that Controlo Approve consumes.

## 2. Authority

- `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` — **settled functional authority for
  this handoff's settings scope**: Controlo_Create owns `Definições` (repairer register,
  per-machine repairer assignments, PDF/document base directory, email lists, email templates);
  Controlo_Approve owns none of it (§1, §2, §3, §4, §7, §8, §10).
- `dmo-beta-master/modules/CONTROLO_CREATE.md` (full) — included scope, pending association,
  calculation ownership, measurement rows, comparison, Pegamentos, Folha/Resumo, acceptance.
- `dmo-beta-master/architecture/RECORD_LIFECYCLES.md` §4–8 — Peso status vocabulary
  (`Pendente`, `Aprovado`, `Não aprovado`), Comparação as a record/workflow relation, stale as a
  workflow condition, Folha/Resumo as distinct records.
- `dmo-beta-master/architecture/CROSS_MODULE_FLOWS.md` — Job On → Controlo Create → Approve.
- `dmo-beta-master/implementation/BETA_INTEGRATION_SEAMS.md` — "C → D seam"; C owns the shared
  Peso renderer/read model.
- `dmo-beta-master/contracts/DOCUMENTS_AND_FILES.md` §2–3 — document identity/naming only
  (generation is P2-T08).
- `dmo-master/global/ACCESS_MODEL.md` §9 — Controlo Create as a distinct assignable module
  sharing the `controlo` destination.
- `dmo-master/global/INFORMATION_MODEL.md` — the Peso snapshot vs autopopulate distinction.

> **Terminology (binding — master plan §0):** the History in this workstream is **HISTÓRICO
> (local)** — history functionality *inside* the module. It is not the top-level
> **HISTÓRICO GLOBAL** module (technical identity `historia`), which is DEFERRED BY DESIGN for
> this Beta. Do not merge the two, and do not treat one as authority for the other.

## 3. Current implementation starting point

Nothing operational exists (no `peso`/`pegamento`/`resumo`/`controlo_sheet` occurrence anywhere
in `src/`). Reusable: the P2-T04 hidden context contract, P2-T03's `MeasurementRows`, and the
shell/components from P2-T01…P2-T03.

## 4. Scope

1. Select/create Job On context; display production context; resolve/reuse CM context.
2. **Peso** draft/edit/calculation/submission on **one** `peso_id`:
   - normal relation `peso_id -> cm_id`;
   - truthful pending case `peso_id -> tool_id`, displayed as **`Job On por associar`** (not an
     error; does not block measurement/approval; later association is explicit and clears the
     direct Tool anchor); **no** fake Job On/`cm_id`;
   - formulas exactly as Beta fixes them:
     `Capacidade/Volume do CM = Peso de água ÷ configured water-temperature value`;
     `Peso do vidro = (Capacidade do CM + Volume Marisa/BQ − Volume Punção/PU) × Densidade do vidro`;
   - water-temperature range 5–35 °C; individual CM results first-class (an average never hides
     a bad individual result); decimals normalized for presentation only.
3. **Variable measurement rows** with at least one valid row and stable row identity.
4. **Comparação** as a Peso record type: explicitly selected `previous_peso_id` (never
   auto-selected; cross-machine candidates valid when the same canonical Tool is compatible);
   explicit pairing persisted as `current_peso_id -> previous_peso_id`; stale after a reading
   change must be rebuilt before submission.
5. **Pegamentos**: `pegamentos_id -> jobon_id -> cm/mf/bq`; CM/BQ/MF sections; Costura 0° /
   Contra-costura 90°; signed ovalização; average; single-axis behavior; variable rows; nominal
   from canonical Tool data; tolerance corridor `nominal ± 0.20`; boundary crossing is a
   warning; missing nominal → `NotEvaluable` (never invented); invalid/missing required Tool
   context blocks that sheet with an actionable correction message; Pegamentos may legitimately
   be absent.
6. **Folha** (`controlo_sheet_id -> jobon_id`) and **Resumo**
   (`resumo_id -> jobon_id + applicable contexts`) as **distinct** persisted records.
7. **`Controlo_Create → Definições`** — the operational settings owned by this module
   (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §1, §3, §4, §7, §8, §10):
   - **Repairer register.** Required data is **name only**. Do **not** invent address, email,
     phone, supplier code, tax data or contact person as required or as functional fields. The
     user must be able to add a repairer, edit a repairer name, and select an existing repairer
     for a machine assignment. An active/inactive lifecycle is an **implementation concern only**
     and must not expand the user-facing model beyond add / edit name / select.
   - **Per-machine repairer assignment.** `B1`, `B2`, `B3`, `C1`, `C2`, `C3` each hold their
     **own independent** assignment. There is **no** grouping rule — no shared B or C repairer and
     no "Linha B"/"Linha C" model. Changing one machine must not change any other, and each
     assignment can be changed independently here.
   - **PDF/document base directory.** Configure the base directory, change it, and verify/check
     whether it is accessible. Operation configuration only: do not change the
     `<reference>/<production-number>/` structure, the document file names or any availability
     state (P2-T08 owns generation).
   - **Email recipient lists.** Create/edit a named list, associate recipients with it, and
     select/use lists for document sending rules. Recipient addresses must **never** be hardcoded
     in application code.
   - **Email templates.** Subject, body, and the applicable document type/context where needed.
     Templates should support contextual values already known by the application (reference,
     production, machine, date). **Do not invent the placeholder syntax** — no accepted syntax
     exists in this repository (§10.4).
   - `Definições` is reached **inside** the Controlo Create workflow. It is **not** a new
     destination, **not** a new Module, and **not** a new global Admin surface.
8. **Submit** transitions the same `peso_id` into reviewable state with backend truth for
   state/attribution. Create never approves its own record.
9. **Publish the canonical read-only Peso sheet/read model** for P2-T06.

## 5. Authority blocker B2 — required contract before execution

The authored, reviewed contract must fix: the Controlo schema and keys (`peso_id`,
`pegamentos_id`, `controlo_sheet_id`, `resumo_id`); the explicit `previous_peso_id` relation;
the calculate/persist/submit/read query shapes; transactional boundaries; the read-model shape
and its versioning; and the endpoint/route names with their module policies.

It must **also** fix the `Definições` surfaces settled in
`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`: the repairer register schema/key, the
per-machine assignment representation (one independent assignment per machine, no grouping), the
document base-directory setting and its accessibility check, the named email list + recipient
representation, and the email template representation. It must **not** invent repairer fields
beyond name, a machine grouping rule, a placeholder syntax, or exact email routing rules.

### 5.1 Contract status (recorded)

**P2-T05: CONTRACT ACCEPTED** (correction re-review ACCEPT `f54ac15a96797a0dd0c51cf85b9b179e16be4da3` /
`ceb9ee9…`, dmo-work).
**B2: RESOLVED — PLAN ACCEPT.**
**P2-T05 IMPLEMENTATION: IMPLEMENTED — CLOSED** (implementation response `dev/responses/P2_T05_IMPLEMENTATION_RESPONSE.md`; the post-closure glass-density Owner correction and the water-temperature → water-density lookup correction are implemented and independently verified, VERIFIED `7afcb00…`).

The B2 contract was authored at `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md`
(contract SHA `c1adae808af11e1a9d68c8ed98e074259339f065`; authoring response:
`dev/responses/P2_T05_CONTRACT_AUTHORING_RESPONSE.md`) and fixes, over the real closed P2-T04
seams: the Peso identity/anchoring model (production `cm_id` | truthful pending `tool_id`, DB-
enforced exclusive anchor, `Job On por associar`, no `production_id`/duplicate identity chain),
the weight **and** capacity registration (per-row `water_weight_g` plus backend-derived persisted
`capacity_cm3`/`glass_weight_g`, formulas exactly, 5–35 °C, `numeric(18,4)` storage, ≤ 2 dp
presentation), the measurement-row model (variable, ≥ 1, dense positional ordering), the
historical snapshot model (frozen: CM-context triple via `cm_id`, Peso inputs, density used,
per-row results, attribution; Job-On labels remain documented traversal), the create/edit/submit
transactions on one `peso_id` with the minimal P2-T06 handoff carrier and no delete path,
missing-CM creation composed through `IJobOnService` Set under the Controlo_Create gate, the
`PesoJobOnDependencyProbe`, the five `Definições` surfaces (name-only repairer register;
independent B1..C3 assignments, current-state only; PDF base directory configurable, changeable
and **checked server-side**; named email lists; email templates), an 8-table physical schema, ONE
migration contract, the 17-route matrix (each exactly `controlo-create`; Definições never a
destination), the failure/result vocabulary and published Peso read-model shapes, and a complete
test-to-acceptance matrix (**66 AC ↔ 84 rows**; audit missing 0 / dangling 0 / orphan 0).

The **Architect plan review** (`dev/reviews/P2-T05_CONTROLO_CREATE_CONTRACT_PLAN_REVIEW.md` in
`diogo-o/dmo-work`, commit `256081fae43d4192b879b65fca0bb43efe8cdbca`) returned **PLAN REJECT —
C1–C4 only**. The review **resolved** the recorded BLOCKING question **Q-PDF**:
**ACCEPT DEFAULT — server-host filesystem configuration with server-side accessibility check**
(the configured path is a server-host-visible path; the check executes server-side with the
typed result vocabulary; the browser only edits/submits the configuration; no browser-local
claim; P2-T08 owns generation/storage/send). All **27 authority questions are ACCEPT DEFAULT,
0 BLOCKING**. The four minimum corrections were applied by the correction task at the corrected
contract commit on DMO-MODULAR `main` (correction response:
`dev/responses/P2_T05_CONTRACT_CORRECTION_RESPONSE.md`): **C1** test-matrix repair (AC-N1 proof
rows; dangling `AC-A1/A2/A6/A7`/`AC-N2` keys removed/remapped; counts re-audited), **C2** the
typed `RESULT_NON_POSITIVE` token for the reachable non-positive computed-result CHECK violation,
**C3** the calculate-route identity pin (`POST /controlo/create/calculate` — request-carrier
identity, no path identity, no resolution, never 404), **C4** the Appendix D.3 authority-SHA/
provenance correction (the recorded `610c8b4…` is unretrievable; replaced by verified current
heads plus the preserved dmo-work evidence record `dev/evidence/P2T05_DMO_MASTER_AUTHORITY_EVIDENCE.md`).

*(Recorded at authoring time — **HISTORICAL / SUPERSEDED** by the status update below.)* The
contract is **not** self-accepted: it awaits the Architect re-review per
`dmo-beta-master/WORKFLOW.md` step 6. Implementation is **NOT STARTED — NOT AUTHORIZED**.
`ModuleRegistrations.CurrentBuildAvailable` remains `[]`. **P2-T05 is not marked started and no
workstream after P2-T05 is authorized.**

> **Status update (implementation task):** the Architect correction re-review
> (`f54ac15a96797a0dd0c51cf85b9b179e16be4da3` / `ceb9ee9…`, dmo-work) returned **PLAN ACCEPT**;
> P2-T05 implementation was authorized, executed against the accepted contract, and recorded in
> `dev/responses/P2_T05_IMPLEMENTATION_RESPONSE.md`. The workstream is now
> **IMPLEMENTED — CLOSED** (final accepted behavior preserved: Peso with the same-`peso_id`
> lifecycle, `Controlo_Create → Definições` with the repairer register, independent per-machine
> repairer assignments, the PDF base-directory setting, email lists and email templates, glass
> density settings with the water-density behavior, the shared Peso read model); the post-closure
> **glass-density Owner correction** (glass density per processo NNPB/PS in Definições,
> migration 005, seeds NNPB 2.4027 / PS 2.4231) and the **water-temperature → water-density
> lookup** correction are implemented and independently verified (VERIFIED `7afcb00…`).
> `ModuleRegistrations.CurrentBuildAvailable` remains `[]`, no route is registered, and
> **P2-T06 is also CLOSED; P2-T07 CLOSED; P2-T08 / P2-T09 / P2-T10 remain NOT IMPLEMENTED**.

### 5.2 Owner clarification — Resumo entry; Peso anchoring; repairer family ownership (recorded)

A NEW OWNER CLARIFICATION (registered on the clean baseline; contract **§31**; record
`dev/responses/OWNER_CLARIFICATION_PLANNING_CONTEXT_AND_ASSOCIATIONS_RESPONSE.md`) registers the
following as current authority — **authority only, no implementation**, P2-T05 remains CLOSED:

1. **Controlo receives the production through the Resumo (Resumo da produção).** The Peso is
   **populated by `cm_id` / Job On** with machine, reference, lot, processo and the CM context.
   The Resumo is fixed as the Controlo **entry point**; the `resumo_id` record itself remains an
   unimplemented handoff item (Q-SCOPE) — its role is authority only, no table/route was created.
2. **Peso pré-JobOn anchoring (confirmation).** A Peso may keep the truthful pending `tool_id`
   anchor; the correspondence with the CM context is the **same canonical `tool_id` UUID**
   (candidate `cm_id` whose `cm_contexts.tool_id` equals the anchor); on association the record
   passes to `cm_id`. This pins the already-implemented associate rule — no behavior change.
3. **Repairer family ownership transfer (SUPERSEDES).** The repairer register and the
   line/machine → repairer associations **belong to `Boquilhas > Definições`**, not to Controlo,
   not to Admin — superseding the affected §5.1 bullet above ("the five `Definições` surfaces
   (name-only repairer register; independent B1..C3 assignments…)"), the §5 B3-authority wording
   and the settled delta ownership (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`
   §3/§4) **for the repairer family only**. PDF-directory/email-list/email-template settings stay
   with `Controlo_Create → Definições`. The data-shape rules are unchanged (name-only, no delete,
   independent per-machine assignment, no grouping, current-state with historical preservation).
   The implemented `repairers`/`machine_repairer_assignments` tables and the Definições routes
   stay exactly as they are; the re-homing requires a future workstream contract.
4. **Controlo Create and Controlo Approve remain distinct modules.** No generic architecture may
   force their workflows/pages to be identical (preservation; P2-T06 unaffected).

## 6. Explicit non-scope

- Approval/rejection/reopen decisions (P2-T06).
- **Any settings surface under Controlo_Approve** — Approve is Aprovar + Histórico de Pesos only
  (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §2).
- A new global Admin module for these settings; moving them into the existing ADMIN-only
  Administration surfaces; registering `Definições` as a destination (§1.4).
- Repairer fields beyond name; a grouping of machines into a line/group assignment (§3.3, §4.2).
  *(Ownership note — **SUPERSEDED** by the Owner clarification (§5.2.3): the repairer register and
  the line/machine → repairer associations belong to `Boquilhas > Definições`, not to Controlo,
  not to Admin; the shape rules above remain.)*
- Automatic previous-Peso selection; same-machine-only restriction.
- Duplicate Tool registry; independent production identity.
- Frontend-owned formulas or persistence; approval-copy Peso.
- Document generation or PDF bytes (P2-T08); the exact email routing rules (§9.4); the email
  template placeholder syntax (§10.4).
- Any snapshot engine beyond what each record's own historical output requires.
- No `CurrentBuildAvailable` change and no route registration.

## 7. Expected files/projects

```text
src/DMO.Domain/                                   (Controlo value objects)
src/DMO.Application/ControloCreate/               (use cases + repository contracts,
                                                   including the Definições settings contracts)
src/DMO.Infrastructure/Persistence/ + Migrations/ (Controlo schema, NEW migration)
src/DMO.Web/Pages/Controlo/                       (Create-side surfaces + Definições area)
src/DMO.Web/Frontend/Controlo/                    (shared Peso sheet read model)
src/DMO.Web/Endpoints/
tests/DMO.UnitTests/  tests/DMO.IntegrationTests/
```

## 8. Access requirements

`ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloCreate)` on every Create
route/action **and on every `Definições` route/action**. The shared `controlo` destination keeps
Create and Approve gates independent, so a caller holding only `controlo-approve` receives the
documented denial on `Definições` and on every settings action.

## 9. Backend / persistence requirements

Per B2. Persist the records and their relations; the process classification is consumed through
`cm_id -> tool_id` and Peso preserves the configuration actually used as historical evidence —
not as a second authoritative process owner. No second Job On/Peso authority.

## 10. Required tests

See master plan §11 P2-T05: formulas, water range, individual results visible, `NotEvaluable`,
stale rebuild, no auto-selection, pending association, distinct Folha/Resumo, same `peso_id`
through the lifecycle, Create cannot approve.

Additionally, for `Definições` (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`):
add/edit-name/select a repairer with name as the only required data; each of
`B1`,`B2`,`B3`,`C1`,`C2`,`C3` holds an independent assignment and changing one leaves the other
five unchanged; the document base directory can be configured, changed and checked; a named email
list can be created/edited with recipients associated and selected for a sending rule; an email
template carries subject/body/document context; no hardcoded recipient address exists in
application code; `Definições` is denied to a `controlo-approve`-only caller and is not a
registered destination.

## 11. Acceptance criteria

Every bullet in `modules/CONTROLO_CREATE.md` "Acceptance criteria"; one `peso_id` from draft to
submit; previous Peso never automatic; stale Comparação requires rebuild; Folha and Resumo stay
distinct records; warnings never approve/reject; the `Definições` settings exist under the
Controlo_Create gate and nowhere else; no repairer field beyond name is required; no machine
grouping rule exists; `CurrentBuildAvailable` unchanged.

## 12. Completion evidence

Committed implementation + tests + the published Peso read-model contract consumed by P2-T06.

## 13. Downstream dependents

P2-T06, P2-T08, P2-T10.
