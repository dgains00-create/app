# P2-T05 — CONTRACT AUTHORING RESPONSE

**Workstream:** P2-T05 — Controlo_Create (Peso create/measurement core + `Controlo_Create →
Definições`).
**Task class:** contract authoring only. **No implementation.**
**Status:** CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW.

---

## 1. Baseline

| Item | Value |
|---|---|
| DMO-MODULAR `main` at authoring (fetched before/after; `origin/main` = `c1b457f312e63bd32a718888d06793e249eec051`) | `c1b457f312e63bd32a718888d06793e249eec051` |
| P2-T04 status | **CLOSED** — Architect focused re-review ACCEPT `b6f7a01c99fc8c517cca5cab335af5d47fb9e2f9` (dmo-work `main`), superseding the prior REJECT; P2-T04 remains CLOSED throughout this authoring run |
| dmo-beta-master `main` | `78da49248f6cf7a8cbe4ddd946f3c38abbaf322f` |
| dmo-master `main` | `610c8b4d3084864a750f6aa00507b5273ef09567b` |
| dmo-work `main` | `b6f7a01c99fc8c517cca5cab335af5d47fb9e2f9` (P2-T04 FINAL ARCHITECT RE-REVIEW) |
| Working tree at authoring | CLEAN before authoring (application code); `git status --porcelain` empty |
| Contract artifact | `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` **(new)** |
| Response artifact | `dev/responses/P2_T05_CONTRACT_AUTHORING_RESPONSE.md` **(this file, new)** |
| Implementation authority boundary | P2-T05 is **not** implemented; only planning/contract/governance artifacts were written |

## 2. Authority read (completely, before authoring)

| Authority | Used for |
|---|---|
| `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` | binding handoff: scope §4 (incl. Definições ownership), non-scope §6, B2 §5, access §8, persistence §9, tests §10, acceptance §11, terminology §2 |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §0 terminology, §4 B2 register, §5 graph, §7 P2-T05/P2-T06/P2-T08, §8 rules 6–7, §9, §10, §11, §12 protected register, §13 sequence, Appendix A |
| `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` | **the settled settings authority** — §1–§16 in full |
| `reports/BETA_MASTER_RECONCILIATION.md` | §5.5 Workstream C; §5.8; §7 partials |
| `plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md` + `dev/responses/P2_T04_IMPLEMENTATION_RESPONSE.md` + the two dmo-work Architect reviews | every real seam consumed: identities/schema (6 tables), frozen triple, reference→productions, shared Tool orchestration, association Set semantics, `IJobOnDependencyProbe` seam, result/transport vocabulary, concurrency incl. the §15.1 `SaveAsync` correction, one-migration contract, route/policy pattern, protected files |
| `docs/CREATION_AND_ASSOCIATION_LOGIC.md` | `peso_id -> cm_id -> tool_id + jobon_id`, pending association, no duplicate identities, PDF directory logic |
| `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` | §1A fixed desktop, §4 states, §5 Definições-is-a-surface, §7/§8/§10/§13/§14 component contracts |
| `dmo-beta-master` full set | `modules/CONTROLO_CREATE.md`, `modules/CONTROLO_APPROVE.md`, `RECORD_LIFECYCLES.md` (§4–§8), `CROSS_MODULE_FLOWS.md`, `BACKEND_FRONTEND_MODEL.md`, `IDENTITIES_AND_RELATIONSHIPS.md`, `BETA_INTEGRATION_SEAMS.md`, `DOCUMENTS_AND_FILES.md`, `ACCEPTANCE_MATRIX.md`, `WORKFLOW.md` |
| `dmo-master` full set | `modules/CONTROLO.md` (current Peso/Controlo functional authority — identity/anchoring, inputs/calculations, formulas, rows/status, history, documents), `global/INFORMATION_MODEL.md`, `global/ACCESS_MODEL.md` §9, `dmo-modular/BETA_VERSION.md` |
| Historical evidence (evidence only, superseded where conflicting) | legacy `N06_peso.sql` + the legacy Peso operator visual (`BA-DMO` local material) — used to understand the legacy row structure (per-CM water weight → capacity → glass weight) and the legacy **browser/computer-local** reports directory concept that grounds the BLOCKING Q-PDF question |

## 3. Contract created

`plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` — exactly the 30 required sections plus five
appendices:

| Required item | Present |
|---|---|
| 1. Authority | yes — §1 (order, supersession record, read files, boundary, accepted-input register, 20 recorded silences) |
| 2. Scope / non-scope | yes — §2 (incl. §2.3 the Comparação/Pegamentos/Folha/Resumo scope-boundary record, Q-SCOPE) |
| 3. Peso identity | yes — §3 (what `peso_id` is; anchoring; forbidden identities; uniqueness default) |
| 4. CM/JobOn relationship | yes — §4 (explicit selection, no auto-resolution, real seams, missing-CM creation, association action) |
| 5. Measurement model | yes — §5 (weight + capacity both registered; formulas exact; rows exact; precision) |
| 6. Historical snapshot model | yes — §6 (live refs vs frozen facts; why Job-On labels are traversal, not frozen; guarantees) |
| 7. Create/edit/delete behavior | yes — §7 (create/calculate/edit/submit transactions; no-delete rule) |
| 8. Controlo_Create UI contract | yes — §8 (regions R1–R8, save/validation/success, Definições access) |
| 9. Definições ownership | yes — §9 (five areas, Controlo_Create gate, site-wide, not a destination) |
| 10. Repairer registry | yes — §10 (name only; add/edit/select; no lifecycle column; RESTRICT protection) |
| 11. Machine assignment | yes — §11 (B1..C3 independent; no grouping; current-state only with downstream-snapshot proof) |
| 12. PDF directory settings | yes — §12 (persistence FIXED; check semantics → BLOCKING Q-PDF) |
| 13. Email lists | yes — §13 (named lists, recipients, replace-all, validation, no hardcoding) |
| 14. Email templates | yes — §14 (name/subject/body/document_type; no placeholder syntax) |
| 15. Email/document seam | yes — §15 (settings here; sending is P2-T08; Pegamentos presence seam) |
| 16. Physical schema | yes — §16 (8 tables, exact columns) |
| 17. Keys/constraints/indexes | yes — §17 (PKs, uniques, 7 RESTRICT FKs, 22 CHECKs, index register) |
| 18. Transactions | yes — §18 (per-operation atomic units) |
| 19. Concurrency | yes — §19 (version tokens; SaveAsync mapping; per-op matrix) |
| 20. Repository/application interfaces | yes — §20 (6 repository contracts, 2 service contracts, 5 integration seams incl. the PesoJobOnDependencyProbe) |
| 21. Routes/endpoints | yes — §21 (17 routes: 2 pages + 15 minimal-API/groups, one policy each) |
| 22. Authorization | yes — §22 (controlo-create everywhere; no approve leakage; ADMIN denied) |
| 23. P2-T03/P2-T04 composition | yes — §23 (component mapping + binding rules) |
| 24. Fixed desktop | yes — §24 (1366 × 768, no reflow) |
| 25. Migration contract | yes — §25 (ONE migration; 8 tables; 001/002/003 untouched; safe Down) |
| 26. Failure/result vocabulary | yes — §26 (closed unions, tokens, transport mapping, published read-model shapes) |
| 27. Test-to-acceptance matrix | yes — §26.4 (complete matrix: PESO IDENTITY / MEASUREMENTS / SNAPSHOT / CREATE / JOB ON-CM / REPAIRERS / MACHINE / SETTINGS / AUTH / BOUNDARIES) |
| 28. Authority questions | yes — §27 (+ §28 summary): **1 BLOCKING** (Q-PDF), **26 NON-BLOCKING** with pinned defaults |
| 29. Explicit downstream seams | yes — §29 (P2-T06/P2-T07/P2-T08/P2-T10 + the P2-T05 handoff remainder) |
| 30. Implementation acceptance criteria | yes — §30 (**64 criteria**, AC-P1…AC-N1, bidirectionally mapped) |
| Appendices | A protected boundaries; B file ownership; C fixed desktop; D governance record; E PLAN REVIEW gate |

## 4. Key settled decisions

1. **Peso anchoring** — exactly one of real `cm_id` (production) or real `tool_id` (truthful
   pending, "Job On por associar"), DB-enforced by CHECK; no `jobon_id` column; no
   `production_id`; no duplicate identity chain; `cm_id` FK + `tool_id` FK both RESTRICT.
2. **Weight AND capacity registered** — per-row `water_weight_g` (entered) and per-row derived
   `capacity_cm3` + `glass_weight_g` (backend-computed, persisted); formulas contracted exactly
   (capacity = weight ÷ temperature-table divisor; glass = (capacity + Marisa/BQ − Punção/PU) ×
   density); the divisor/density **values** are recorded authority silences (Q-CALC) — nothing
   invented; temperature 5–35 °C; numeric(18,4) storage, ≤ 2 dp presentation only.
3. **Historical snapshot** — frozen: the CM-context triple (via `cm_id`, P2-T04's frozen
   context), the Peso's own inputs, the density used, the per-row results, attribution. Not
   frozen: reference/production/machine (traversal — the information-web rule; explicitly
   documented and tested so "later Job On changes do not silently alter frozen facts" is a true,
   precise claim). No snapshot engine.
4. **Create lifecycle** — create → draft edit → submit on the **same** `peso_id`; status
   vocabulary exactly `pendente`/`aprovado`/`nao_aprovado` (P2-T05 writes `pendente` only);
   `submitted_at`/`submitted_by_user_id` = the minimal P2-T06 handoff carrier; no delete path;
   version-guarded with the accepted save-time-race mapping.
5. **Missing CM context** — created through `IJobOnService`'s Set association under a
   `controlo-create` route (route 11) — the accepted "consuming workflow creates the missing
   context later" rule with no second authority and no access-model change; `ferramentas` Tool
   access stays `ferramentas`-gated.
6. **Repairers** — name only; add/edit-name/select; no status/delete UI; RESTRICT-only
   historical protection (no lifecycle column needed in this contract).
7. **Machines** — `B1 B2 B3 C1 C2 C3` independent, one current assignment row each, no grouping,
   no registry, no history table (P2-T07 snapshots `repairer_id` on its own movements — proof
   recorded §11.4).
8. **PDF directory** — persistence/change FIXED (single-row table, version-guarded); the
   **"accessible" check semantics are BLOCKING (Q-PDF)**: the legacy authority is a
   browser/computer-local directory, the current runtime is a server-side web monolith, and no
   repository authority records where the web process will be hosted — the check operation may
   not be invented to a fabricated semantic.
9. **Email** — named lists + recipients (atomic replace-all, no hardcoded addresses), templates
   (subject/body/three-value document type), **no routing rule** (P2-T08's decision, master's
   Line-group concept and the delta's no-invention rule reconciled in §13.4/Q-ROUTE), **no
   placeholder syntax**.
10. **Boundaries** — no P2-T06 (approval) behavior; no P2-T07/P2-T08 implementation; Comparação/
    Pegamentos/Folha/Resumo/read-model rendering remain P2-T05 handoff items but are explicitly
    NOT authored here (Q-SCOPE, NON-BLOCKING); `CurrentBuildAvailable` stays `[]`; `Definições`
    is not a destination; one new migration owns exactly 8 tables.

## 5. Contract question dispositions

**BLOCKING — 1:**

- **Q-PDF** — deployment semantics of the "local PDF directory" and its accessibility check
  (task Part 18 mandates recording this as BLOCKING rather than inventing browser filesystem
  access). The schema, the configure/change surface and the `not-configured` state are
  unaffected and remain FIXED; the check operation's executable semantics wait on the Architect's
  decision (smallest default: server-process-local absolute path).

**NON-BLOCKING — 26** (Q-UNIQ, Q-CALC, Q-UNIT, Q-PREC, Q-NOMINAL, Q-DELETE, Q-DUP, Q-REP,
Q-HIST, Q-ROUTE, Q-PLACE, Q-DOCTYPE, Q-SITE, Q-CONC, Q-SETAUDIT, Q-CAND, Q-REANCHOR, Q-ROWLBL,
Q-ORDER, Q-NAME, Q-ADDR, Q-SURF, Q-DOCREAD, Q-PCS, Q-PRODLABEL, Q-SCOPE) — every one with a
pinned default already reflected in the schema/interfaces/routes; none blocks the PLAN REVIEW
gate.

## 6. Fixed desktop compliance

- Binding policy restated in §24 and Appendix C; canonical 1366 × 768; no breakpoint structural
  variant; region-stable R1–R8 and the five Definições sections; local keyboard-reachable
  overflow for wide regions; no mobile/tablet variants; new `dmo-controlo.css` (no
  `@media`/`@container`/`@supports` structural rules in the P2-T05-owned block).

## 7. P2-T04 reuse / no duplicate identity (evidence of inspection)

The contract consumed the real shipped seams: `cm_contexts`/`mf_contexts`/`bq_contexts` with the
frozen triple, `job_ons` (reference/production_number/machine), `tools` (type/reference/lot/
processo), `IJobOnService`/`IToolService`, the 14 P2-T04 routes (Tool search/create reused),
`IJobOnDependencyProbe` (P2-T05 supplies `PesoJobOnDependencyProbe`), the `SaveAsync`
concurrency-mapping pattern, `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloCreate)`,
`users` for backend attribution. No Tool master, no Job On, no CM identity is recreated; no fake
`cm_id`/`tool_id`/`jobon_id` appears anywhere in the contract.

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
| `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` | new — the implementation contract |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §4 B2 row status + blocker-status table; §7 P2-T05 CONTRACT STATUS block; Appendix A B2 row (status records only) |
| `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` | §5 contract-authored record (pointer + status) |
| `dev/responses/P2_T05_CONTRACT_AUTHORING_RESPONSE.md` | new — this response |

Verified absent from the diff: `src/**`, `tests/**`, `**/Migrations/**`, `**/*.csproj`,
`Directory.*`, `wwwroot/**`, `Program.cs`, any `.env`/Supabase configuration, and
`docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md`.

## 10. Baseline verification (before/after authoring)

```text
DMO-MODULAR HEAD/origin/main : c1b457f312e63bd32a718888d06793e249eec051 (unchanged)
P2-T04                        : CLOSED (Architect ACCEPT b6f7a01c) — unchanged
build/tests                   : not modified by this task (authoring only)
CurrentBuildAvailable         : [] (unchanged)
migrations beyond 001–003     : 0 (unchanged)
application code modified     : NO
Supabase modified             : NO
working tree (after commit)   : CLEAN
```

## 11. Next gate

```text
ARCHITECT PLAN REVIEW REQUIRED BEFORE P2-T05 IMPLEMENTATION
P2-T05 contract SHA = <contract commit> (pushed to DMO-MODULAR/main)
```

The Architect must review the committed contract
(`plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md`) at its pushed SHA and return `PLAN ACCEPT`
(or `CORRECTION REQUIRED`/`REJECT`), dispositioning the 27 questions of §28 — in particular the
single BLOCKING item Q-PDF and the Q-CALC default — per `dmo-beta-master/WORKFLOW.md` steps 4–7.

Until then:

- P2-T05 implementation is **not** authorized and has **not** started;
- **B2 remains open**; P2-T05 status is `CONTRACT AUTHORED — AWAITING ARCHITECT PLAN REVIEW`;
- P2-T06/P2-T07/P2-T08 remain **NOT AUTHORIZED**;
- this response does **not** self-accept the contract and does not mark P2-T05 `IMPLEMENTED`.