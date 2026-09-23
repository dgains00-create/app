# P2-T06 — CONTRACT AUTHORING RESPONSE

**Workstream:** P2-T06 — Controlo Approve (Aprovar + Histórico de Pesos; review/decision core over
the exact submitted `peso_id`).
**Task class:** contract authoring only. **No implementation.**
**Status:** CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW.
**Precedence note:** P2-T05 (Controlo_Create) and its post-closure glass-density correction slice
are both **CLOSED**; P2-T06 is **NOT STARTED / NOT AUTHORIZED**; P2-T07 / P2-T08 / P2-T10 remain
**NOT AUTHORIZED** (each verified against the remote closure records before authoring).

---

## 1. Baseline

| Item | Value |
|---|---|
| DMO-MODULAR `main` at authoring (fetched before authoring; `origin/main`) | `7afcb0079ab2ee84ad2a0356d1f3576e832646e7` |
| P2-T05 status | **CLOSED** — original closure record dmo-work `dev/reviews/P2-T05_CONTROLO_CREATE_CLOSURE.md` (`19c48a4…`); implementation `9fbfcf4…`; review ACCEPT `ae99d1f…`; "next eligible gate P2-T06 planning/contract gate" |
| P2-T05 glass-density correction slice | **CLOSED** — closure `dev/reviews/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_CLOSURE.md` @ dmo-work `02bd53e03e61bcc7d627ea8e1545d79e6eff8a70` (remote `main`); implementation `ce516d0…`; verification `7afcb00…`; review ACCEPT `300f011…`; P2-T06 "NOT STARTED — subject to its own planning/contract gate" |
| P2-T06 | **NOT STARTED / NOT AUTHORIZED** (no route/type/table/migration before this task; verified scan) |
| P2-T07 / P2-T08 / P2-T10 | **NOT AUTHORIZED** (closure records §6/§8, unchanged) |
| `ModuleRegistrations.CurrentBuildAvailable` | `[]` (verified `src/DMO.Application/Access/ModuleRegistrations.cs` line 27) |
| dmo-beta-master `main` | `78da49248f6cf7a8cbe4ddd946f3c38abbaf322f` |
| dmo-work `main` | `02bd53e03e61bcc7d627ea8e1545d79e6eff8a70` |
| dmo-master `main` | `8f1ca3e27e0eaf58c3dce285b544066565fa3dc1` |
| Working tree at authoring | CLEAN (application code); `git status --porcelain` empty before authoring |
| Contract artifact | `plans/contracts/P2-T06_CONTROLO_APPROVE_CONTRACT.md` **(new)** |
| Response artifact | `dev/responses/P2_T06_CONTRACT_AUTHORING_RESPONSE.md` **(this file, new)** |
| Implementation authority boundary | P2-T06 is **not** implemented; only planning/contract/governance artifacts were written |

## 2. Authority read (completely, before authoring)

| Authority | Used for |
|---|---|
| `plans/beta-workstreams/P2-T06-CONTROLO-APPROVE.md` | binding handoff: settled two-surface scope (§4), settings excluded (§4.1), non-scope (§5), access (§7), persistence (§8), tests (§9), acceptance (§10) |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §0 terminology (HISTÓRICO local vs GLOBAL), §4 B2, §5 graph (P2-T05 before P2-T06, hard rule 4), §7 P2-T06 reduced scope, §8 shared-data map (Peso/Comparação/Folha rows), §9 route plan, §10 access plan, §11 P2-T06 tests, §12 protected register, §13 steps 10–11, Appendix A |
| `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` | §2 (Approve = exactly Aprovar + Histórico de Pesos; owns no setting — the settled scope reduction), §9 (routing intent; no invented rules), §12 (superseded visuals V1/V3/V4/V5), §14 (P2-T06 row) |
| `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` (accepted, CLOSED) | `peso_id` identity/anchoring (§3), status vocabulary + **reviewable predicate** (§3.1), frozen facts (§5/§6), lifecycle ("P2-T06 —► 'aprovado' | 'nao_aprovado' on the same row"; "submitted_at is never cleared by Create (reopen is P2-T06)") (§5.7/§7.3/§7.4), schema (§16/§17 incl. the **deferred pending-list index** §17.5), concurrency (§19 SaveAsync), routes/policies (§21/§22), failure vocabulary (§26.1/§26.2), **published read-model shapes `PesoSheetReadModel` (§26.3)**, downstream seams (§29), acceptance (§30) |
| `plans/contracts/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_CONTRACT.md` + closure | frozen-on-Peso glass density — never re-read by review; settings changes affect new Pesos only |
| `dev/responses/P2_T05_IMPLEMENTATION_RESPONSE.md` (final) | shipped seams: submit leaves `status` untouched (reopen mechanics), `IControloCreateService.GetAsync`, aggregate-embedded rows, 17 routes/gates, D1/D2 patterns (`renderConflict` + explicit reload; submitted-view adapter safety), migration 004/005 state |
| `reports/P2_T05_INDEPENDENT_VERIFICATION.md` / `REVERIFICATION_D1_D2.md` / `GLASS_DENSITY_CORRECTION_VERIFICATION.md` | verification state; D3/D4 non-blocking carry-forward |
| `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` | §1A fixed desktop, §4 states (`conflict` = message + recovery choices), §9 table selection/open, §12 AuditTrail (no synthesized actor/time), §14 DecisionBar |
| `plans/contracts/P2-T02_*` / `P2-T03_*` (accepted) | the consumed component contracts (table = selection surface; actions outside the table; no per-row grids) |
| `dmo-beta-master` full set | `modules/CONTROLO_APPROVE.md` (full), `modules/CONTROLO_CREATE.md`, `RECORD_LIFECYCLES.md` §4–§8/§12, `ACCESS_AND_NAVIGATION.md`, `CROSS_MODULE_FLOWS.md`, `BACKEND_FRONTEND_MODEL.md`, `implementation/BETA_INTEGRATION_SEAMS.md` (C→D seam, Workstream D), `contracts/IDENTITIES_AND_RELATIONSHIPS.md`, `contracts/SHARED_FRONTEND.md`, `ACCEPTANCE_MATRIX.md` §2/§6/§9–§11, `WORKFLOW.md` |
| `dmo-work` accepted rulings/reviews | `dev/rulings/P2_T05_PESO_IDENTITY_RULE.md` (**binding P2-T06 obligations**: same `peso_id`; who/when/`pesos.version` audit; reopen preserves history + restores draft-editable handoff + never `aprovado` before re-submission + define "material edit"), `dev/rulings/P2_T05_PESO_APPROVAL_PDF_AUTHORITATIVE_DATA_RULE.md` (approval data is a consumer of the same authoritative record — no separate dataset), `dev/rulings/P2_T05_GLASS_DENSITY_CONFIGURATION_OWNER_RULE.md`, `dev/reviews/P2-T05_CONTROLO_CREATE_CLOSURE.md`, `dev/reviews/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_CLOSURE.md` |
| `dmo-master` (current) | `modules/CONTROLO.md` (full: §2 access, §4 approval/reopen attribution, §5/§6 pending never blocks approval, §10 Comparação per-CM `Manter`/`Colocar de parte` + justification, §13 Folha five family items, §15 actors + **Folha states `Rascunho → Submetida → Aprovada / Rejeitada`** + decision/reopen ownership + general-vs-per-CM approval, §16 history traversal, §17 **`Enviar para produção`** explicit/confirmed/from-approved-Peso + send-block semantics, §18 invariants), `global/ACCESS_MODEL.md` §9 (Approve surface: review/approval/rejection, per-CM Comparação decisions, Folha decision, reopen, explicit confirmed send-to-production; separate modules; hidden control never the security boundary), `global/INFORMATION_MODEL.md` |
| `src/` (read-only inspection, remote-main state) | `ModuleCatalog` (`ControloApprove` = `controlo-approve` → destination `controlo`), `ModuleRegistrations` (`[]`), `PesoSheetReadModel.cs`, `ControloCreateService.SubmitAsync` (status-preserving `with`), `Pages/Controlo/Create.cshtml` (page-owned renderer; **no shared sheet partial exists** — grounds §16 Q-RENDER), `ControloPolicyNames.cs`, migrations 001–005 |
| Historical evidence | not needed beyond the rulings; every legacy statement that would conflict is superseded by the current authority set |

## 3. Contract created

`plans/contracts/P2-T06_CONTROLO_APPROVE_CONTRACT.md` — the complete implementation contract with
all required sections plus appendices:

| Required definition | Where |
|---|---|
| 1. exact domain/state vocabulary | §3 (Peso statuses fixed: `pendente`/`aprovado`/`nao_aprovado`; reviewable predicate fixed; decision events `aprovado`/`nao_aprovado`/`reaberto`; per-CM `Manter`/`Colocar de parte`; Folha `rascunho/submetida/aprovada/rejeitada`; transport tokens) |
| 2. existing identities consumed | §4 (`peso_id` same record; `cm_id`/`tool_id`/`jobon_id`/`user_id` consumed; no creation) |
| 3. any new IDs genuinely required | §5 (exactly one: `peso_review_decision_id`) |
| 4. physical persistence schema | §6 (one new table `peso_review_decisions`, exact columns; no `pesos` column change) |
| 5. FK/unique/check/index semantics | §7 (2 RESTRICT FKs, 4 CHECKs, 3 justified indexes incl. the deferred `IX_pesos_reviewable`) |
| 6. repository contracts | §8 (`IPesoReviewRepository`, exact members) |
| 7. application service contracts | §9 (`IControloApproveService`, exact members; closes shared reads) |
| 8. transaction boundaries | §10 (one transaction per decision; event immutability) |
| 9. concurrency/version behavior | §11 (version guard + SaveAsync mapping; no silent retry/merge; D2 conflict/reload) |
| 10. result/error vocabulary | §12 (closed `ReviewResult` union + tokens incl. `not-reviewable`, `already-decided`) |
| 11. route matrix | §13 (exactly 9 routes: 2 pages + 7 endpoints) |
| 12. authorization policy per route | §14 (exactly `controlo-approve` everywhere; Create/Approve independence) |
| 13. review list/read/detail contracts | §15 (pending query/filter contract, history query, review sheet, decision trail) |
| 14. shared Peso read-model reuse | §16 (exact `PesoSheetReadModel` type; renderer-parity contract test; no fork/copy/second renderer) |
| 15. per-CM decision contract | §17 (closed vocabulary; carrier deferred — no invented persistence) |
| 16. Folha decision contract | §18 (closed vocabulary/ownership; carrier deferred — no invented persistence) |
| 17. approve/reject/reopen semantics | §19 (exact flows incl. the Identity-Rule mapping and the "material edit" definition) |
| 18. local Histórico behavior | §20 (filters, rows, selection/open, trail; HISTÓRICO GLOBAL boundary) |
| 19. fixed-desktop UI regions | §21 (R1–R5, H1–H3; no reflow/relocation/card conversion; shell untouched) |
| 20. negative-scope protections | §22 (BND-B1…BND-B10) |
| 21. full test-to-acceptance matrix | §26.4 (**61 AC ↔ 67 rows; missing 0, dangling 0, orphan 0**) |
| Extras | §23 `Enviar para produção` authority trace + gap (Q-SEND); §24 downstream seams; §25 migration contract; §27/§28 authority questions; §30 acceptance criteria; Appendices A–E |

## 4. Key settled decisions

1. **The reviewable circuit is the fixed P2-T05 predicate** — `submitted_at IS NOT NULL AND
   status = 'pendente'` — consumed, never redefined; the pending list is a **query**, not a queue
   table.
2. **Approve/reject/reopen write lifecycle only:** exactly `pesos.status`, `version`,
   `updated_at` (+ `submitted_at`/`submitted_by_user_id` cleared on reopen) and the new decision
   trail. No P2-T06 write path touches any Peso input/row (AC-R1/R2/O5; structural, proven by
   scan + DB).
3. **One new table only** (`peso_review_decisions`) carrying the Identity Rule's audit facts:
   who (`decided_by_user_id`), when (`decided_at`), and **which record version was decided**
   (`pesos_version_at_decision`), plus `prior_status` and reason; append-only/immutable after
   COMMIT; plus the **single deferred pending-list index** `IX_pesos_reviewable(status,
   submitted_at DESC)` that P2-T05 §17.5 reserved for this slice. Supersession of "9 tables"
   recorded to exactly this extent (§25.3).
4. **Reopen semantics (Q-REOPEN, pinned):** same `peso_id`; `status := 'pendente'`;
   `submitted_at`/`submitted_by` cleared → the **closed** Create edit/submit routes work again;
   prior decision/actor/time/reason preserved in the append-only trail; the record is **never
   `aprovado` without a NEW approval** (the Identity Rule's "never `aprovado` before
   re-submission" mapping; `'nao_aprovado'` as the interim status is mechanically impossible with
   the closed submit route — recorded with proof); "material edit" defined exactly as any change
   to Peso inputs or computed results. P2-T06 is never an editor of measurement facts.
5. **Human-decision only:** decision commands carry identity/version/reason **only** — no
   warning/calculation input; no automatic transition exists; per-CM `Manter`/`Colocar de parte`
   is closed vocabulary that nothing auto-selects.
6. **Renderer reuse (Q-RENDER, pinned):** the review sheet embeds the **exact** shared
   `PesoSheetReadModel` type composed through the shared application read under `controlo-approve`
   routes; same field order/labels/≤ 2-dp normalization enforced by a **renderer-parity contract
   test** (Create submitted view vs Approve review view); the shared-partial extraction that
   would modify the CLOSED `Create.cshtml` is the Architect-selectable alternative (cross-stream).
7. **Carrier deferrals — no invention:** Comparação relation/per-CM decisions (§17) and Folha
   decision (§18) are NOT persisted in the accepted schema (P2-T05 Q-SCOPE/BND7); their
   vocabulary, semantics and D-side rules are fixed from authority, and **no table, route or
   type is created for them**; enablement is the P2-T05 handoff remainder's own gate. The review
   surface renders no comparison region and no warning region today (the read model carries
   neither) and never fabricates either.
8. **`Enviar para produção` (Q-SEND, pinned, authority-traced §23):** the action exists in the
   master (confirmed, from an approved Peso, send-block does not invalidate approval) and in the
   Approve surface (ACCESS_MODEL §9); its **execution** is P2-T08's contract (delta §14; Q-ROUTE);
   the Beta conditions inclusion on a published backend contract that does not exist. P2-T06
   contracts the **initiation affordance only** — explicit confirmation, never automatic, on
   approved Pesos — currently **unavailable-with-reason**, with **zero send persistence** and no
   P2-T08 mechanics. Recorded as an authority gap with the caveat that the Architect may
   disposition it to REQUIRES OWNER DECISION.
9. **Concurrency = accepted pattern:** version tokens + `SaveAsync` mapping → 409
   `stale-version`, nothing written; negative state guards `not-reviewable`/`already-decided`;
   `conflict` presentation with explicit reload recovery (D2 pattern); no silent retry/merge.
10. **Boundaries:** no Definições/settings surface; no PDF/email/file code; no Boquilhas; no
    availability registration (`CurrentBuildAvailable` stays `[]`); no HISTÓRICO GLOBAL; no
    second calculation engine; no generic lifecycle infrastructure; migrations 001–005 and all
    P2-T05 artifacts byte-identical (one new migration owns exactly the one table + one index).

## 5. Contract question dispositions

**BLOCKING — 0.** **REQUIRES OWNER DECISION — 0** (Q-SEND carries the explicit note that the
Architect may disposition it to `REQUIRES OWNER DECISION`; the pinned default adds no invented
behavior and removes none that authority mandates — §23.3).

**NON-BLOCKING — 10**, each with a pinned default reflected in the schema/interfaces/routes:

| Q | Question | Pinned default |
|---|---|---|
| Q-SEND | `Enviar para produção` executable semantics / interim presence / state change | initiation-only affordance on approved Pesos; unavailable-with-reason until P2-T08's contract; zero P2-T06 send persistence (§23) |
| Q-REOPEN | reopen target status; "material edit → `nao_aprovado`" letter of the Identity Rule | reopen → `pendente` + cleared handoff; new approval required for `aprovado`; material edit = any change to Peso inputs/computed results (§19.2) |
| Q-NOTE | reject/reopen reason mandatory? | non-blank reason required for reject and reopen; none for approve (§19.3) |
| Q-FOLHA | Folha decision surface before `controlo_sheet_id` exists | vocabulary/ownership fixed (§18.1); no routes/persistence until the Folha remainder contract |
| Q-PERCM | per-CM decision persistence before Comparação exists | vocabulary fixed (§17.1); no carrier/table/route invented |
| Q-COMP | Comparação read when no relation is persisted | exact-relation read contract pinned; no heuristic/latest/mutation; region absent today (§17.2) |
| Q-RENDER | renderer reuse mechanics (no shared partial shipped) | same read-model type + presentation discipline + parity test (§16); shared-partial extraction = optional cross-stream change |
| Q-WARN | warnings region | render only when the shared read model supplies warnings; never decide (§15.3) |
| Q-PAGE | paging/ordering bounds | 1-based, pageSize 1–100, deterministic ordering (§15) |
| Q-DECISIONS | reopen event token | `reaberto` (from authority verb `reabrir`) — consistent past-participle set (§3.2) |

## 6. Fixed desktop compliance

Binding policy restated in §21 and Appendix C: canonical 1366 × 768; region-stable R1–R5 and
H1–H3; no breakpoint structural variant, no table→card conversion, no required-column hiding, no
action relocation; keyboard-reachable local overflow; no mobile/tablet variants; new
`dmo-controlo-approve.css` without structural `@media`/`@container`/`@supports` rules; shared
header/navigation untouched.

## 7. Identity discipline (evidence of inspection)

- `peso_id` is the **same record** from creation (P2-T05) through submission, review, decision
  and reopen; the contract creates no approval-copy Peso, no `production_id`, no
  `job_on_revision_id`, no `approval_peso_id`.
- No `cm_id`/`tool_id`/`jobon_id` is created by P2-T06; anchors appear only through the shared
  `PesoSheetReadModel` projections; the pending `tool_id` anchor never blocks review.
- The only new identity is `peso_review_decision_id` (one append-only decision event).
- `submitted_at`/`submitted_by_user_id` remain the handoff carrier facts; the decision trail adds
  who/when/version/reason/prior-status — never duplicating Peso operational facts (authoritative
  data rule).

## 8. Supabase / backend boundary

No Supabase change, no migration, no database call, no endpoint, no DTO, no repository, no
`DbContext` change, no Auth/session work, no availability registration, no route registration.
Migrating Supabase TEST is a later operator deployment step of the implementation workstream,
never a claim of this task.

## 9. Files changed

Planning/contract/governance artifacts only. **No `src/`, `tests/`, migration, route, runtime
configuration, CSS/JS/Razor source, Supabase or backend code was created or modified.**

| Path | Change |
|---|---|
| `plans/contracts/P2-T06_CONTROLO_APPROVE_CONTRACT.md` | new — the implementation contract |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §7 P2-T06 CONTRACT STATUS record; Appendix A 9.12 row (status records only) |
| `plans/beta-workstreams/P2-T06-CONTROLO-APPROVE.md` | §13 contract-authored record (pointer + status) |
| `dev/responses/P2_T06_CONTRACT_AUTHORING_RESPONSE.md` | new — this response |

Verified absent from the diff: `src/**`, `tests/**`, `**/Migrations/**`, `**/*.csproj`,
`Directory.*`, `wwwroot/**`, `Program.cs`, any `.env`/Supabase configuration, and
`docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md`.

## 10. Baseline verification (before/after authoring)

```text
DMO-MODULAR HEAD/origin/main : 7afcb0079ab2ee84ad2a0356d1f3576e832646e7 (unchanged)
P2-T05                        : CLOSED (unchanged)
P2-T05 correction slice       : CLOSED (unchanged)
P2-T06                        : NOT STARTED — contract authored, NOT implemented
P2-T07 / P2-T08 / P2-T10      : NOT AUTHORIZED (unchanged)
build/tests                   : not modified by this task (authoring only)
CurrentBuildAvailable         : [] (unchanged)
migrations beyond 005         : 0 (unchanged)
application code modified     : NO
Supabase modified             : NO
working tree (after commit)   : CLEAN
```

## 11. Next gate

```text
ARCHITECT PLAN REVIEW REQUIRED BEFORE P2-T06 IMPLEMENTATION
P2-T06 contract SHA = dd0e16390e46d49c811d1597de674dcc68023813 (pushed to DMO-MODULAR/main)
```

The Architect must review the committed contract
(`plans/contracts/P2-T06_CONTROLO_APPROVE_CONTRACT.md`) at its pushed SHA and return `PLAN ACCEPT`
(or `CORRECTION REQUIRED`/`REJECT`), dispositioning the 10 questions of §28 — in particular
Q-SEND, Q-REOPEN, Q-FOLHA/Q-PERCM/Q-COMP and Q-RENDER — per `dmo-beta-master/WORKFLOW.md` steps
4–7.

Until then:

- P2-T06 implementation is **not** authorized and **has not** started;
- P2-T06 status is `CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW`;
- P2-T07 / P2-T08 / P2-T10 remain **NOT AUTHORIZED**;
- this response does **not** self-accept the contract and does not mark P2-T06 started;
- `ModuleRegistrations.CurrentBuildAvailable` remains `[]`.