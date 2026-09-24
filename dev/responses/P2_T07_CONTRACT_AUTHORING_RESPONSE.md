# P2-T07 — CONTRACT AUTHORING RESPONSE

**Workstream:** P2-T07 — Boquilhas (BQ external-repair quantity workflow; aggregate + movement
ledger + edit-audit + close/reopen + local Histórico).
**Task class:** contract authoring only. **No implementation.**
**Status:** CONTRACT CORRECTED (B1) — AWAITING FOCUSED ARCHITECT PLAN RE-REVIEW. The Architect
PLAN review (dmo-work `542a08a1…`) returned **PLAN REJECT — blocking finding B1 only**; the B1
correction (partial unique indexes + exact 23505 mapping + concurrent test rows) has been
applied to the contract (see §12). **Not accepted; implementation NOT AUTHORIZED.**
**Precedence note:** P2-T05 (Controlo_Create) and its post-closure glass-density correction slice
are **CLOSED**; P2-T06 (Controlo Approve) is **CLOSED** (its closure record announces the
**P2-T07 planning/contract gate** as the next eligible gate, recorded only — NOT AUTHORIZED);
P2-T08 / P2-T10 remain **NOT AUTHORIZED** (each verified against the remote closure records
before authoring).

---

## 1. Baseline

| Item | Value |
|---|---|
| DMO-MODULAR `main` at authoring (fetched before authoring; `origin/main`) | `8f9e4f85af774453d6b99f66fc3dbaa7d5196a11` |
| P2-T05 status | **CLOSED** — closure `dev/reviews/P2-T05_CONTROLO_CREATE_CLOSURE.md` (`3491097…`); implementation `092743a…` + corrections `66e7f0d…`/`9fbfcf4…`; review ACCEPT `ae99d1f…` |
| P2-T05 glass-density correction slice | **CLOSED** — closure `dev/reviews/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_CLOSURE.md` (@ dmo-work `02bd53e…`); implementation `ce516d0…`; review ACCEPT `300f011…` |
| P2-T06 status | **CLOSED** — closure `dev/reviews/P2-T06_CONTROLO_APPROVE_CLOSURE.md` (@ dmo-work `fcfa81f…`, remote `main`); contract `dd0e163…`; PLAN ACCEPT `947c5f7…`; implementation `e527ade…` + CP4 `e784d0d…`; verification `8f9e4f8…`; review ACCEPT `4d88dbe…`; "NEXT ELIGIBLE GATE: **P2-T07 planning/contract gate** (recorded only — NOT AUTHORIZED)" |
| P2-T07 | **NOT STARTED / NOT AUTHORIZED** (no route/type/table/migration before this task; verified scan) |
| P2-T08 / P2-T10 | **NOT AUTHORIZED** (closure records, unchanged) |
| `ModuleRegistrations.CurrentBuildAvailable` | `[]` (verified `src/DMO.Application/Access/ModuleRegistrations.cs` line 27) |
| dmo-beta-master `main` | `78da49248f6cf7a8cbe4ddd946f3c38abbaf322f` |
| dmo-work `main` | `fcfa81f3aabd5457d73e50c86089b4de18e79225` |
| dmo-master `main` | `8f1ca3e27e0eaf58c3dce285b544066565fa3dc1` (`dmo-modular` branch `ae2a9b9d12132ee4b41dc0696f34c6439b5cca52`) |
| Working tree at authoring | CLEAN (application code); `git status --porcelain` empty before authoring |
| Contract artifact | `plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md` **(new)** |
| Response artifact | `dev/responses/P2_T07_CONTRACT_AUTHORING_RESPONSE.md` **(this file, new)** |
| Implementation authority boundary | P2-T07 is **not** implemented; only planning/contract/governance artifacts were written |

## 2. Authority read (completely, before authoring)

| Authority | Used for |
|---|---|
| `plans/beta-workstreams/P2-T07-BOQUILHAS.md` | binding handoff: purpose (§1), authority (§2), starting point (§3), scope §4 (both flows, exactly four movement types, edit-audit, dates, derived balance, close/reopen, repairer resolution, sidebar removal), blocker B3 (§5), non-scope (§6), expected files (§7), access (§8), persistence (§9), required tests (§10), acceptance (§11) |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §0 terminology (HISTÓRICO local vs GLOBAL), §4 B3, §5 graph (P2-T04 before P2-T07, hard rule 3), §7 P2-T07 (scope + settled repairer rules + removed sidebar), §8 shared-data map (Boquilhas/Movement/Repairer/Machine-assignment rows), §9 route plan (`/boquilhas`, P2-T10 registration), §10 access plan, §11 P2-T07 tests, §12 protected register, §13 steps 12–13, Appendix A 9.13/9.14 |
| `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` | §3 (register owned by Controlo_Create → Definições; Boquilhas consumes), §4 (six independent machines, no grouping), §5 (automatic resolution), §6 (historical preservation — binding), §11 (machine sidebar removed), §12 (superseded visuals V2/V6/V7/V8) |
| `plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md` (accepted, CLOSED) | `tools`/`tool_machines`/`job_ons`/`bq_contexts` (§3), context rules (§7), query contracts (§8: reference → productions, Tool search incl. `Type` filter, ficha), **shared Tool search/select/create orchestration** (§9), `ToolAssociationChange` Set (BQ slot) (§11.2), repository/service contracts (§12: `IToolService`/`IJobOnService`), probe seam (§12.4), route matrix (14 routes incl. `ferramentas` 12/13), failure vocabulary (§14), concurrency (§15), migration (§16) |
| `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` (accepted, CLOSED) | `repairers` (§16.3) + `machine_repairer_assignments` (§16.4) — the consumed Definições state; **current-state-only assignments** (§11.4/Q-HIST) with the P2-T07 consumption seam (§29); the exclusive-anchor pattern mirrored here (§3.2); missing-context composition through the Job On contract (§21.4); "Tool search/create remains `ferramentas`-gated; no second Tool access path" (§22/AC-R6); transactions (§18), concurrency (§19 `SaveAsync`), failure vocabulary (§26), downstream seams (§29) |
| `plans/contracts/P2-T06_CONTROLO_APPROVE_CONTRACT.md` (accepted, CLOSED) + closure | the most recent accepted contract (section pattern, result-union shape, route matrix, BND table, matrix audit, `…PolicyNames` constants, interim runtime state); P2-T06 closure records the P2-T07 gate as next |
| `dev/responses/P2_T04_IMPLEMENTATION_RESPONSE.md`, `P2_T05_IMPLEMENTATION_RESPONSE.md`, `P2_T06_IMPLEMENTATION_RESPONSE.md` | shipped seams: `SaveAsync` mapping, probe additive registration, page-owned assets pattern, D1/D2 `renderConflict` reload recovery |
| `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` | §1A fixed desktop, §4 states (`conflict` = message + recovery; empty ≠ lookup-failed ≠ permission-denied), §9 DenseDataTable (select vs open; actions outside the table), §12 AuditTrail (never synthesizes actor/time), §14 DecisionBar |
| `plans/contracts/P2-T02_*` / `P2-T03_*` (accepted) | the consumed component contracts (table selection/open, no per-row grids; ToolPicker mechanics; opaque keys) |
| `dmo-beta-master` full set | `modules/BOQUILHAS.md` (full), `modules/JOB_ON_LIGHT.md`, `modules/FERRAMENTAS_LIGHT.md`, `RECORD_LIFECYCLES.md` (§9 Boquilhas; §12 invariants), `CROSS_MODULE_FLOWS.md` (both flows; shared Tool flow), `BACKEND_FRONTEND_MODEL.md` (identity chain; movement edit ≠ second event), `ACCESS_AND_NAVIGATION.md` (one assignable module; direct-route enforcement), `implementation/BETA_INTEGRATION_SEAMS.md` (Workstream E; B→E seam; testing seam), `contracts/IDENTITIES_AND_RELATIONSHIPS.md` (identity map; no fake identities), `contracts/SHARED_FRONTEND.md`, `ACCEPTANCE_MATRIX.md` (§7 Boquilhas evidence), `WORKFLOW.md` |
| `dmo-work` accepted rulings/reviews | `dev/reviews/P2-T05_CONTROLO_CREATE_CLOSURE.md`, `dev/reviews/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_CLOSURE.md`, `dev/reviews/P2-T06_CONTROLO_APPROVE_CLOSURE.md`, `dev/reviews/P2-T04_DOMAIN_CORE_TOOL_JOBON_IMPLEMENTATION_REVIEW.md` — gate/closure authority; the Peso rulings read for their general identity discipline only (no Boquilhas-specific rule reopened) |
| `dmo-master` (current) | `modules/BOQUILHAS.md` (full — identity, opening facts, movement vocabulary, trace lifecycle, balance, excess return, repairer, utilisation/dates, Job On crossing rules, History, settings/non-settings, module surface, invariants, modular note — with the recorded supersessions of §1.1: the ADMIN-managed register and per-line-config-in-Admin statements are superseded by the settled delta), `global/ACCESS_MODEL.md` §1/§11, `global/INFORMATION_MODEL.md` |
| `src/` (read-only inspection, remote-main state) | `ModuleCatalog` (`Boquilhas` = `boquilhas` → destination `boquilhas`), `ModuleRegistrations` (`[]`), `JobOnPolicyNames`/`ControloPolicyNames` pattern, migrations 001–006 (21 public tables), `Endpoints/*` group pattern |
| Historical evidence | not needed beyond the closure records; every legacy statement conflicting with the settled delta is superseded and recorded as such (§1.1) |

## 3. Contract created

`plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md` — the complete implementation contract with all
required sections plus appendices:

| Required definition | Where |
|---|---|
| 1. exact domain/state vocabulary | §3 (movement tokens `inicio\|saida\|entrada\|irreparavel`; aggregate status `active\|closed`; balance buckets; resolution vocabulary; transport tokens) |
| 2. existing identities consumed | §4 (`tool_id`, `bq_id`, `jobon_id`, `user_id`, `repairer_id`, machine codes — all consumed, none created) |
| 3. any new IDs genuinely required | §5 (exactly three: `boquilhas_id`, `movement_id`, plus the child-row identities of the aggregate/movement/close/reopen structures) |
| 4. physical persistence schema | §6 (**six tables**: `boquilhas`, `boquilha_machines`, `boquilha_movements`, `boquilha_movement_audit`, `boquilha_close_snapshots`, `boquilha_reopenings`; exact columns) |
| 5. FK/unique/check/index semantics | §7 (14 RESTRICT FKs, 16 CHECKs incl. the exclusive-anchor, Saída-required and Entrada-facts CHECKs, 1 unique, 8 justified indexes) |
| 6. repository contracts | §8 (`IBoquilhasRepository`, exact members + the consumed P2-T04/P2-T05 application reads) |
| 7. application service contracts | §9 (`IBoquilhasService`, exact members; composition + decision rules) |
| 8. transaction boundaries | §10 (one transaction per mutation; immutability by construction) |
| 9. concurrency/version behavior | §11 (aggregate + movement version guards; SaveAsync mapping; D2 conflict/reload) |
| 10. result/error vocabulary | §12 (closed `BoquilhasResult` union + validation codes + transport mapping incl. `saida-exceeds-available`, `irreparavel-exceeds-in-repair`, `active-aggregate-exists`, `already-closed`, `not-closed`, `not-last-closed`, `aggregate-closed`, `only-one-inicio`) |
| 11. route matrix | §13 (**exactly 18 routes**: 3 pages + 15 endpoints, all `boquilhas`) |
| 12. authorization policy per route | §14 (exactly `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas)` everywhere) |
| 13. identity model — binding | §15 (tool_id canonical vs bq_id frozen; exclusive anchor; both flows) |
| 14. Tool selection/creation | §16 (shared orchestration consumed; BQ-only candidates; no auto-select; contextual create → origin; no second registry) |
| 15. movement contract | §17 (four types; per-type forms/validations; Início-once; excess Entrada recorded) |
| 16. balance derivation | §18 (exact replay formulas; physical order; no second authority) |
| 17. edit/audit contract | §19 (same row; before/after audit; no double balance; immutables; no annulment) |
| 18. dates | §20 (business_date ⊥ recorded_at; opening_date) |
| 19. repairer resolution | §21 (machine → current assignment → suggested; final selected stored; historical preservation; consume-only) |
| 20. production-linked vs standalone | §22 (both flows exact; BQ-context creation via `IJobOnService`; contextual panel read-only) |
| 21. active aggregate, utilisation, close/reopen | §23 (summary; manual utilisation; close snapshot; reopen eligibility; opening-facts) |
| 22. local Histórico | §24 (filter set; backend-applied; table interaction rule) |
| 23. fixed-desktop UI regions | §25 (R1–R6, N1–N4, H1–H3; no reflow/relocation/card conversion; shell untouched) |
| 24. negative-scope protections | §26 (BND-B1…BND-B10) |
| 25. downstream seams | §27 (P2-T08/P2-T10; `BoquilhasDependencyProbe` registration) |
| 26. migration contract | §28 (ONE additive migration 007 owning exactly the six tables; 21 → 27 tables) |
| 27. full test-to-acceptance matrix | §29 (**83 AC ↔ 83 rows; missing 0, dangling 0, orphan 0**) |
| Extras | §30/§31 authority questions (12 NON-BLOCKING pinned defaults); §32 acceptance criteria; Appendices A–E |

## 4. Key settled decisions

1. **Identity/anchor**: `tool_id` = canonical Tool; `bq_id` = frozen Job On BQ context — **not
   competing identities**. Each aggregate anchors to exactly one of them, **DB-enforced**
   (`CHECK ((bq_id IS NULL)::int + (tool_id IS NULL)::int = 1)`, the accepted P2-T05 `pesos`
   pattern). No `production_id`, no `job_on_revision_id`, no fake Job On/`bq_id`, no reverse
   arrays, no per-piece BQ identity, no reference/lot copy (traversal through the anchor).
2. **Movement vocabulary**: exactly `inicio|saida|entrada|irreparavel` (CHECK); `Editar` is an
   action, never a type (no token, no selector entry). The **Início movement is created with
   the aggregate** in the create transaction (the opening quantity IS the Início); later
   `inicio` appends are refused (`only-one-inicio`).
3. **Balance**: derived **only** from movement facts by exact replay in physical order
   (`recorded_at ASC, movement_id ASC`); buckets Disponível / Em reparação / Irreparável /
   Entrada excecional; invariant `Disponível + Em reparação + Irreparável = Σ(início)`; **no
   second mutable balance authority of any kind**; Saída ≤ Disponível and Irreparável ≤ Em
   reparação are 409 refusals (`saida-exceeds-available`, `irreparavel-exceeds-in-repair`);
   excess Entrada recorded on the movement (`expected_return_quantity` +
   `excess_received_quantity` = `GREATEST(0, …)` facts computed by replay in-transaction,
   CHECK-enforced) — never clamped, never rejected; negative saldo visible and non-blocking.
4. **Edit = replace + audit**: `Editar` updates the SAME `movement_id` row (quantity /
   business_date / machine / repairer / observations; `movement_type` + `recorded_at`
   immutable) and appends one before/after audit row in the same transaction; validation
   replays the ledger with the edited row's current values excluded then validates the new
   values as an append — **one net quantity event, no double balance effect**. Audit/
   snapshot/reopen rows are append-only by construction (no UPDATE/DELETE path). No annulment
   carrier in P2-T07.
5. **Dates**: `business_date` (operator-editable, drives the operational calendar/filters) ⊥
   `recorded_at` (immutable backend timestamp); editing a date never rewrites `recorded_at`,
   never reorders the ledger, never changes the balance. `opening_date` is an editable business
   date.
6. **Repairer (settled delta consumed)**: register + per-machine assignments are owned by
   `Controlo_Create → Definições` (closed P2-T05); Boquilhas consumes read-only. External Saída
   carries `machine` (from the aggregate's registered machine set) + the **final selected
   canonical `repairer_id`** — machine → current assignment → resolved/suggested (operator not
   forced to re-type); `assignment-unavailable` is an explicit non-error state; the movement
   stores the value, so **later assignment changes never rewrite earlier records** (historical
   preservation = the movement-row `repairer_id`, the minimum snapshot the closed P2-T05 §11.4
   explicitly assigns to this flow).
7. **Close/reopen on the SAME `boquilhas_id`**: close = one transaction (status → `closed`,
   version bump, immutable snapshot row with replay-computed buckets + the manual utilisation
   still + backend closed_by/closed_at); failed close = atomic (still active, no snapshot).
   Reopen = one transaction (reason required; eligibility: closed ∧ **last** closed trace for
   the same BQ Tool context ∧ no other active aggregate for the same context; reopen row with
   backend actor/time/reason referencing the exact close). Cycles keep the identity; per-close
   snapshots accumulate. No replacement aggregate, no fake context.
8. **Utilisation**: one manual `utilisation_percent` still on the aggregate (0–100 or NULL,
   editable, captured as-is in the close snapshot) — never derived from movements, never
   synced, never a progress bar.
9. **Consumed seams**: Tool search/create stays on the `ferramentas`-gated P2-T04 routes (the
   accepted "no second Tool access path" rule); BQ-context creation composes
   `IJobOnService.UpdateAsync` (BQ-slot Set) exactly like P2-T05 route 11 composes CM; machine
   assignments + repairer register read through the closed P2-T05 application contracts under
   Boquilhas-gated routes; `BoquilhasDependencyProbe` registered additively on the accepted
   probe seam (Job On delete protection).
10. **Boundaries**: no settings/Admin tab; no repairer administration; no PDF/file/email/
    document identity (P2-T08); no HISTÓRICO GLOBAL (`historia`); no availability registration
    (`CurrentBuildAvailable` stays `[]`); no machine/reference sidebar simulating Job On state
    (the read-only production-line contextual panel reads real context only); no fifth movement
    type; migrations 001–006 + `DmoDbContext.cs` byte-identical (one new migration owns exactly
    the six tables).

## 5. Contract question dispositions

**BLOCKING — 0.** **REQUIRES OWNER DECISION — 0.**

**NON-BLOCKING — 12**, each with a pinned default reflected in the schema/interfaces/routes
(§30.2); closed rules (standalone validity, repairer ownership, machine independence,
historical repairer preservation, movement vocabulary, no fake Job On, Editar not a type,
close/reopen same identity, movement facts as balance authority) are **not** re-asked:

| Q | Question | Pinned default |
|---|---|---|
| Q-EDIT-FIELDS | Which movement fields does `Editar` change? | quantity/business_date/machine/repairer_id/observations editable; movement_type + recorded_at immutable (§19) |
| Q-MACHINE | Is `machine` mandatory on external Saída? | required (from the aggregate's registered set) so automatic resolution operates; optional elsewhere (§21) |
| Q-INICIO | When is the Início created; more Inícios? | created with the aggregate; later `inicio` appends refused (§17/§22.2) |
| Q-EXCESS | Excess-Entrada persistence? | expected/excess facts on the Entrada row, computed by replay in-transaction; bucket = sum (§17.2/§18) |
| Q-ORDER | Replay order? | `recorded_at ASC, movement_id ASC`; date edits never reorder or change balance (§18) |
| Q-CREATE | Second active aggregate for the same context? | refused while an active aggregate exists for the same anchor; open/reopen after close (§23) |
| Q-REOPEN-ELIG | Exact reopen eligibility | closed ∧ last-closed for the anchor ∧ no other active aggregate for the anchor (§23.3) |
| Q-REFLOT | Reference/lot on the aggregate? | no copy — traversal through the anchor (live Tool / frozen triple) (§15.3/§24) |
| Q-UTIL | Manual utilisation shape | one 0–100-or-NULL edit on the aggregate, captured in the close snapshot, never a progress bar (§23) |
| Q-LINE | "Linha atual" field? | no dedicated column — machine set + per-movement machine facts (§17.3/§23.4) |
| Q-CLOSE-DATE | Close date/time operator or backend? | backend (`closed_at`/`closed_by_user_id`) in the immutable snapshot (§23.2) |
| Q-ANUL | Annulment/removal action in P2-T07? | none — Editar is the correction mechanism; future annulments need own authority and must be registered + confirmed (§19.3) |

## 6. Fixed desktop compliance

Binding policy restated in §25 and Appendix C: canonical 1366 × 768; region-stable R1–R6, N1–N4
and H1–H3; no breakpoint structural variant, no table→card conversion, no required-column
hiding, no action relocation; keyboard-reachable local overflow; no mobile/tablet variants; new
`dmo-boquilhas.css` without structural `@media`/`@container`/`@supports` rules; shared
header/navigation untouched; `% utilização` never a progress bar; single-click select /
double-click open / actions-outside-the-table on every grid (incl. the movement ledger).

## 7. Identity discipline (evidence of inspection)

- `tool_id` and `bq_id` are consumed as the two legal, mutually exclusive aggregate anchors —
  never minted, never duplicated, never competing (`bq_id` = frozen Job On BQ context; the
  bq-context row is created only by Job On's own application contract).
- `boquilhas_id` stays the SAME identity across create → movements → close → reopen cycles; no
  replacement aggregate, no `production_id`, no `job_on_revision_id`.
- `movement_id` is one quantity event; edits replace the same row + audit; the audit id is an
  audit-row id, never a second quantity identity.
- No fake Job On/bq_id in the standalone flow; no reverse-ID arrays; no per-piece BQ UUID; no
  client-minted ids; no second Tool registry.

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
| `plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md` | new — the implementation contract |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §4 B3 row (authored status); §7 P2-T07 CONTRACT STATUS record; Appendix A 9.13/9.14 rows (status records only) |
| `plans/beta-workstreams/P2-T07-BOQUILHAS.md` | §13 contract-authored record (pointer + status) |
| `dev/responses/P2_T07_CONTRACT_AUTHORING_RESPONSE.md` | new — this response |

Verified absent from the diff: `src/**`, `tests/**`, `**/Migrations/**`, `**/*.csproj`,
`Directory.*`, `wwwroot/**`, `Program.cs`, any `.env`/Supabase configuration, and
`docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md`.

## 10. Baseline verification (before/after authoring)

```text
DMO-MODULAR HEAD/origin/main : 8f9e4f85af774453d6b99f66fc3dbaa7d5196a11 (unchanged)
P2-T05                        : CLOSED (unchanged)
P2-T05 correction slice       : CLOSED (unchanged)
P2-T06                        : CLOSED (unchanged; next eligible gate = P2-T07 planning/contract gate)
P2-T07                        : NOT STARTED — contract authored, NOT implemented
P2-T08 / P2-T10               : NOT AUTHORIZED (unchanged)
build/tests                   : not modified by this task (authoring only)
CurrentBuildAvailable         : [] (unchanged)
migrations beyond 006         : 0 (unchanged; 21 public tables)
application code modified     : NO
Supabase modified             : NO
working tree (after commit)   : CLEAN
```

## 11. Next gate

```text
ARCHITECT PLAN REVIEW REQUIRED BEFORE P2-T07 IMPLEMENTATION
P2-T07 contract SHA = dcca794685c03969d97295887e57e0f60e0bd7d8 (pushed to DMO-MODULAR/main)
```

The Architect must review the committed contract
(`plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md`) at its pushed SHA and return `PLAN ACCEPT`
(or `CORRECTION REQUIRED`/`REJECT`), dispositioning the 12 questions of §30/§31 — in
particular Q-EDIT-FIELDS, Q-MACHINE, Q-INICIO, Q-EXCESS, Q-ORDER, Q-CREATE, Q-REOPEN-ELIG,
Q-REFLOT, Q-UTIL, Q-LINE, Q-CLOSE-DATE and Q-ANUL — per `dmo-beta-master/WORKFLOW.md` steps
4–7.

Until then:

- P2-T07 implementation is **not** authorized and **has** not started;
- P2-T07 status is `CONTRACT CORRECTED (B1) — AWAITING FOCUSED ARCHITECT PLAN RE-REVIEW`;
- P2-T08 / P2-T10 remain **NOT AUTHORIZED**;
- this response does **not** self-accept the contract and does not mark P2-T07 started;
- `ModuleRegistrations.CurrentBuildAvailable` remains `[]`.

## 12. Focused B1 correction (this task)

| Item | Value |
|---|---|
| Architect PLAN review | `dev/reviews/P2-T07_BOQUILHAS_CONTRACT_PLAN_REVIEW.md` @ dmo-work `542a08a1bcf1340306f8e337a6c597580a921f3d` |
| Original decision | **PLAN REJECT** — blocking findings **B1 only**; all 12 authority questions independently adjudicated **ACCEPT DEFAULT** (0 REQUIRES OWNER DECISION, 0 BLOCKING) |
| Defect (verbatim finding) | the `active-aggregate-exists` refusal (one-active-aggregate-per-anchor invariant) was not race-safe as contracted: create has no version guard and the in-transaction application scan cannot serialize concurrent creates for the same anchor under READ COMMITTED with no unique anchor tuple (§7.2), no partial unique index (§7.5), no isolation override and no advisory locks (§10) — two active aggregates on one anchor could both commit |
| Correction applied | **B1 only** — (1) partial unique indexes `IX_boquilhas_active_bq_id` (`bq_id` WHERE `status = 'active' AND bq_id IS NOT NULL`) and `IX_boquilhas_active_tool_id` (`tool_id` WHERE `status = 'active' AND tool_id IS NOT NULL`) in §7.2 (semantic) and §7.5 (physical); the exclusive-anchor CHECK keeps the two predicates mutually exclusive, serializing per truthful anchor type; (2) the exact scoped mapping: 23505 on either index → `Refused(ActiveAggregateExists)`, no other 23505 source mapped (§7.2, §8.2); (3) application pre-check = normal-path refusal only; the database index = the concurrency authority; create (§22.2) and reopen (§23.3) race semantics with full-transaction rollback (no partial aggregate, no orphan Início; failed reopen preserves `closed` state/snapshot/history; no partial reopening record); (4) transaction rules §10 (incl. explicit no advisory-lock/no-SERIALIZABLE/no-table-lock/no-lock-table/no-mutex) and §11 matrix/rules; (5) migration-007 delta §28.2 (indexes only — tables 6, migrations 1 unchanged); (6) test rows K6 (production-linked create race), K7 (standalone create race), K8 (create-vs-reopen race) in §29 — **83 AC / 86 rows**; no new criterion; mappings to existing AC-C6/AC-C7/AC-K3/AC-K4; **missing 0, dangling 0, orphan 0** |
| Not changed | every Architect PASS domain (identity/linked-vs-standalone, tool orchestration, movement vocabulary, replay balance, quantity rules, edit semantics, dates, repairer resolution/history, aggregate/create model, close, reopen identity, utilisation, local Histórico, exactly 18 routes, `dmo.module.boquilhas` gate, P2-T05/T08/T10 boundaries, fixed desktop, `CurrentBuildAvailable = []`); authority questions stay **12 ACCEPT DEFAULT / 0 OWNER / 0 BLOCKING** (B1 is a concurrency implementation-contract defect, not a new Owner question) |
| Status after correction | **CORRECTED — AWAITING FOCUSED ARCHITECT PLAN RE-REVIEW** (NOT accepted; implementation NOT AUTHORIZED) |
| Next gate | **focused Architect PLAN re-review of B1 only** — expected ACCEPT after the correction |
| Files changed (this correction) | `plans/contracts/P2-T07_BOQUILHAS_CONTRACT.md` (B1 correction + App. D.5 record), `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` (§4 B3 row + §7 CONTRACT STATUS — status records only), `plans/beta-workstreams/P2-T07-BOQUILHAS.md` (§14 status record), `dev/responses/P2_T07_CONTRACT_AUTHORING_RESPONSE.md` (this section) |
| Implementation performed | **NONE** — docs/governance only; no `src/**`, no `tests/**`, no migration, no Supabase change |
| `ModuleRegistrations.CurrentBuildAvailable` | `[]` (unchanged) |