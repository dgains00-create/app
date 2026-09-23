# P2-T05 Component/Context Identity & Relationship Verification

**Audit class:** focused identity/relationship verification (read-only). No implementation.
No code modification. No migrations. No Supabase changes.
**Repository:** `diogo-o/DMO-MODULAR` @ `main` `b38993f3efd7657b61648e9d65566e099d9b8481`.
**Scope relations:** `jobon_id`, `cm_id`, `mf_id`, `bq_id`, `peso_id`, `resumo_id`,
`pegamentos_id`.
**Authority read:** dmo-work rulings
`dev/rulings/P2_T05_PESO_APPROVAL_PDF_AUTHORITATIVE_DATA_RULE.md` and
`dev/rulings/P2_T05_PESO_IDENTITY_RULE.md`; dmo-master current `main`
`8f1ca3e27e0eaf58c3dce285b544066565fa3dc1` (`modules/CONTROLO.md`, `modules/JOB_ON.md`,
`DECISIONS.md`, `WORKFLOW_ARCHITECTURE_DECISIONS.md`,
`global/OPERATIONAL_RECORD_RELATIONSHIP_MODEL.md`, `global/PRODUCTION_RECORD_CHAIN.md`,
`global/TOOL_IDENTITY.md`); dmo-master modular line `ae2a9b9d12132ee4b41dc0696f34c6439b5cca52`
(`modules/CONTROLO.md`, `modules/JOB_ON.md`, `global/INFORMATION_MODEL.md`,
`global/ACCESS_MODEL.md`); accepted P2-T05 contract; P2-T04 shipped code and tests;
master plan §8 shared-data map.

---

## 1. EXECUTIVE VERDICT

## PASS WITH NON-BLOCKING OBSERVATIONS

**Basis:** P2-T05 has **no implementation** (no ControloCreate code, no `pesos` tables, no
migration — only the `controlo-create` module-catalog vocabulary). All identity/relationship
rules that **can** be violated in shipped code are implemented identically to the authoritative
rule, in the shipped P2-T04 Job On / Tool domain:

- `jobon_id`, `cm_id`, `mf_id`, `bq_id` are created **only** by Job On's own code, one frozen
  context per role per occurrence (DB-unique per `jobon_id`), with **no** `production_id` and
  **no** duplicate or downstream re-creation;
- the accepted P2-T05 contract reuses those IDs by reference only (`pesos.cm_id` /
  pending `tool_id`) and delegates the only possible context creation (missing CM) back to
  **Job On's own** `IJobOnService` Set path;
- Resumo and Pegamentos **do not exist yet** — they are plan-level identities (master plan §8)
  and P2-T05 handoff remainders (Q-SCOPE); there is therefore no code path that could mix
  contexts across Job Ons today.

**Observations (non-blocking, no correction to current code):** see §8 — (O1) the future
Resumo/Pegamentos contracts must add explicit composite same-Job-On integrity (FKs alone cannot
enforce it); (O2) dmo-master current `main` still carries a transitional "Resumo exact component
scope still requires workflow confirmation" note that is superseded by the Owner clarification
and the old-era authority; (O3) `FindProductionIdAsync` is a helper *name* for the
`(reference, production_number)` duplicate refusal, not a production identity.

---

## 2. CURRENT IMPLEMENTATION MAP (what actually exists in code/schema)

```text
tools (canonical Tool master: tool_id, CM|MF|BQ, reference/lot, processo)
  │  created by ToolService/ToolRepository (Ferramentas)
  ▼
job_ons (jobon_id)                        ← created by JobOnService.CreateAsync/DuplicateAsync
  │                                       ← JobOnId.New(); duplication creates NEW jobon_id (DUP3)
  ├── cm_contexts (cm_id)  FK→job_ons RESTRICT, FK→tools RESTRICT, frozen tool_type='CM',
  │                        UNIQUE cm_contexts_jobon_key (≤1 per jobon)
  ├── mf_contexts (mf_id)  FK→job_ons RESTRICT, FK→tools RESTRICT, frozen tool_type='MF',
  │                        UNIQUE mf_contexts_jobon_key (≤1 per jobon)
  └── bq_contexts (bq_id)  FK→job_ons RESTRICT, FK→tools RESTRICT, frozen tool_type='BQ',
                           UNIQUE bq_contexts_jobon_key (≤1 per jobon)

tool_machines (tool_id FK, machine B1..C3)   ← Tool-owned (ToolRepository)
```

No `pesos`, no `peso_measurement_rows`, no `resumos`, no `pegamentos`, no
`controle_sheets`, no `production_id` table, no `job_on_revision_id`, no approval/audit table.
Current `src/` surface for P2-T05: only `ModuleCatalog.cs` (module id `controlo-create`,
destinations `controlo`) — P2-T01 artifact. `CurrentBuildAvailable` = `[]`.

All `Guid` minting of component-context identities (full inventory):

| Site | What it creates |
|---|---|
| `src/DMO.Application/JobOn/JobOnService.cs:102` (`CreateAsync`) | `jobon_id` for a new occurrence |
| `src/DMO.Application/JobOn/JobOnService.cs:120-126` (`CreateAsync`) | one new `cm_id`/`mf_id`/`bq_id` per **explicitly selected** slot; **no symmetry rows** (JOB8) |
| `src/DMO.Application/JobOn/JobOnService.cs:281-302` (`DuplicateAsync`) | NEW `jobon_id` + **NEW** context ids per duplicated context, keeping the source's `tool_id` and frozen triple verbatim (DUP3/DUP4/DUP6) |
| `src/DMO.Infrastructure/Persistence/JobOnRepository.cs:484,514,542` (`ApplySetAsync`) | new context id **only when no context row exists for that `jobon_id`**; else **in-place update of the SAME context row** (identity stable — CTX1/CTX7) |
| `src/DMO.Infrastructure/Persistence/JobOnRepository.cs:650-703` (`ApplyKeepAsync`/`AddContextAsync`) | re-inserts a kept context with its **ORIGINAL** context id |
| `src/DMO.Infrastructure/Persistence/ToolRepository.cs:187` | `tool_machine_id` (Tool-owned; not a component context) |

Nowhere else. Downstream/P2-T05 code **does not exist**, so it cannot mint any of these IDs.

Plan-level identities that do not exist in code: `peso_id` (+ `peso_measurement_row_id`),
`resumo_id`, `pegamentos_id` (master plan §8 shared-data map; P2-T05 contract §2.3/Q-SCOPE;
BND7/AC-Y5).

---

## 3. AUTHORITY MAP (required graph)

From dmo-master (both lines), the accepted P2-T05 contract, the master plan §8 map and the two
dmo-work rulings — all consistent:

```text
Job On occurrence (jobon_id)
  ├── cm_id  (frozen Tool-in-Job-On context, UNIQUE per jobon)
  ├── mf_id  (frozen Tool-in-Job-On context, UNIQUE per jobon)
  └── bq_id  (frozen Tool-in-Job-On context, UNIQUE per jobon)

peso_id  └─ cm_id └─ tool_id                    (Peso: CM context of the occurrence)
resumo_id ─ cm_id / mf_id / bq_id               (same occurrence's existing contexts)
pegamentos_id ─ cm_id / mf_id / bq_id           (same occurrence's existing contexts; optional)

New occurrence of the same reference  ⇒  NEW jobon_id ⇒ NEW cm_id/mf_id/bq_id ⇒ NEW downstream ids
No production_id. No second context layer. No jobon_id duplicated onto downstream rows.
Context identity ≠ tool_id (tool_id remains canonical Tool identity; never a replacement).
PDF is a derived artifact of the owning record (never stored in PostgreSQL).
```

Key authoritative statements verified in source authority:

- `global/OPERATIONAL_RECORD_RELATIONSHIP_MODEL.md` (dmo-master `main`): "`cm_id` = immutable CM
  snapshot UUID created for one Job On from a selected canonical `tool_id`" (same for `mf_id`,
  `bq_id`); "referenced by Peso through its CM relationship; referenced by Pegamentos through
  CM/MF/BQ relationships"; "The backend does not need a separate `production_id` merely to
  identify a production run when the complete production context is already stored by the exact
  `jobon_id`"; "specialist records pin the exact component IDs they actually operate on".
- `global/PRODUCTION_RECORD_CHAIN.md` (dmo-master `main`): "Selecting canonical Tools into Job On
  roles creates **frozen historical component contexts**"; "A separate `production_id` is not
  required merely to restate this identity."
- `modules/CONTROLO.md` (dmo-master `main`): "Peso fundamentally measures the CM used in the
  relevant Job On context"; `peso_id └─ cm_id └─ tool_id`; "Do not add direct `production_id`
  merely to duplicate production context already reachable through the CM/Job On relationship";
  "Stable historical component contexts from Job On are reused where they already identify the
  exact measured subjects"; Pegamentos "measures/compares the exact CM + MF + BQ combination used
  in a Job On".
- `modules/CONTROLO.md` + `modules/JOB_ON.md` (dmo-master modular line): `resumo_id` →
  `jobon_id -> cm_id / mf_id / bq_id`; "The component relation uses the existing real identities
  (`cm_id`, `mf_id`, `bq_id`); **no generic component/context/container ID is invented**";
  "`cm_id`/`mf_id`/`bq_id` … These context IDs are **not Tools**. `tool_id` remains the canonical
  registered Tool identity."
- `DECISIONS.md` D-007 (dmo-master `main`): "Job On is the saved production context …
  Do not introduce `production_id` merely to duplicate an identity already provided by
  `jobon_id` plus its production fields."
- Master plan §8 shared-data map: Peso "normal production path `peso_id -> cm_id`; **do not**
  also persist `tool_id + jobon_id` merely for navigation"; CM/MF/BQ context "created only where
  the corresponding Tool context is actually needed"; Resumo "`resumo_id` … persisted record for
  one `jobon_id` context … record ≠ its PDF".
- Accepted P2-T05 contract: `pesos.cm_id` FK → `cm_contexts(cm_id)` RESTRICT / `tool_id` FK →
  `tools(tool_id)` RESTRICT with exclusive-anchor CHECK; deliberately absent `jobon_id`,
  `reference`, `production_number`, `machine` on `pesos`; no Peso-MF/BQ relation; no
  Resumo/Pegamentos/Folha tables (Q-SCOPE/BND7/AC-Y5); route 11 composes `IJobOnService`
  Keep+Set so any missing CM context is created **by Job On's own code**.
- dmo-work rulings: authoritative Peso record = single source of truth for P2-T06/P2-T08; one
  Peso belongs to one occurrence; same `peso_id` across edits/resubmits; no reference-level
  identity; no approval copy.

---

## 4. DIFFERENCES (implementation vs authority)

| # | Exact file / member | Current behavior | Required behavior | Severity | In P2-T05 scope? |
|---|---|---|---|---|---|
| D1 | (no code exists) | P2-T05 has no implementation | Peso persistence per contract (§16 `pesos`, `peso_measurement_rows`) | N/A — not yet built | Yes (P2-T05 implementation, authorized but not started) |
| D2 | `dmo-master` `modules/CONTROLO.md` (current `main`) | "Resumo exact component scope still requires workflow confirmation" (transitional note) | Resumo uses the **existing** Job On context IDs `cm_id`/`mf_id`/`bq_id` (Owner clarification; old-era `CONTROLO.md` modular: `resumo_id → jobon_id -> cm_id/mf_id/bq_id`) | Informational — newer Owner clarification and old-era authority already define it; no code exists to contradict | No (future Resumo contract — P2-T05 handoff remainder) |
| D3 | (no Resumo/Pegamentos code) | no record exists → no cross-Job-On mixing possible | future contracts must enforce same-Job-On composite integrity (see O1) | Non-blocking obligation | Future contracts (Q-SCOPE follow-ons) |
| D4 | `src/DMO.Infrastructure/Persistence/JobOnRepository.cs:759` `FindProductionIdAsync` | helper name reads like a production identity; actually returns the `jobon_id` of the row winning the `(reference, production_number)` unique race | no behavioral change — rename is cosmetic only; the read-model/authority already forbid a real `production_id` | Cosmetic (name), zero behavioral divergence | No |

**No behavioral difference exists between the implementation map (§2) and the authority map (§3)
for everything that is actually implemented.**

---

## 5. DUPLICATE-ID CHECK

- **Does P2-T05 generate new `cm_id`/`mf_id`/`bq_id` values?** **NO.** P2-T05 has no code. The
  accepted contract's only context-adjacent action (route 11, missing CM context) composes
  `IJobOnService.UpdateAsync` (Keep all facts + Set CM slot) — the `cm_id` is created **by Job
  On's own application/repository code** (`JobOnRepository.ApplySetAsync`), never by Controlo
  code, and only when none exists for that `jobon_id` (unique key backstop).
- **Does it reuse P2-T04 context IDs?** **YES (contractually).** `pesos.cm_id` is an FK to the
  P2-T04 `cm_contexts(cm_id)`; the pending anchor `tool_id` is the truthful pre-association
  state, cleared on association to the existing `cm_id` — never a replacement identity while a
  `cm_id` exists (contract §3, §4.4; master plan §8).
- **Is any duplicate production identity being introduced?** **NO.** `production_id` /
  `job_on_revision_id` appear in `src/` only inside doc comments asserting their absence
  (`JobOnService.cs:18`, `JobOn.cs:19-20`, `JobOnId.cs:11`, `JobOnEntity.cs:14`) and are covered
  by negative tests (P2-T04 `BND5_NoLegacyOrFakeProductionIdentityToken…`,
  `JOB3_NoJobOnTypeCommandResultOrRouteDeclaresALifecycleStatus`, `ORC3_NoOpaqueKeyIsDeclaredAsACanonicalIdentity`).
  No relationship duplicates `jobon_id` on downstream rows: `pesos` deliberately has **no**
  `jobon_id` column (§16.1); no reverse arrays anywhere.

---

## 6. SAME-JOBON INTEGRITY CHECK (Resumo/Pegamentos)

**Trace in shipped schema:**

- `cm_id → jobon_id`: `cm_contexts.jobon_id` FK → `job_ons` (RESTRICT), plus
  `cm_contexts_jobon_key` UNIQUE → a `cm_id` identifies **exactly one** Job On occurrence.
- `mf_id → jobon_id`: `mf_contexts.jobon_id` FK (RESTRICT) + `mf_contexts_jobon_key` UNIQUE.
- `bq_id → jobon_id`: `bq_contexts.jobon_id` FK (RESTRICT) + `bq_contexts_jobon_key` UNIQUE.
- Job On ownership is exclusive: **only** `JobOnService`/`JobOnRepository` insert into these
  tables (full `Guid.NewGuid()` inventory in §2; `JOB7`/`JOB8`/`CTX2`/`DUP4` prove creation
  discipline).

**Can a Resumo/Pegamentos record mix `cm_id` from J1 + `mf_id` from J2 + `bq_id` from J1?**
**NOT POSSIBLE TODAY** — because no Resumo/Pegamentos table, entity, repository, service or
route exists anywhere in `src/` (and the P2-T05 contract creates none: §16 explicitly lists no
Resumo/Pegamentos/Folha table; `BND7`/`AC-Y5`). There is nothing that could combine the IDs.

**Will FKs alone prevent it when they are built?** **NO — and this is the sole integrity gap to
carry forward.** Three independent FKs (`resumo.cm_id → cm_contexts`, `resumo.mf_id →
mf_contexts`, `resumo.bq_id → bq_contexts`) prove *existence* of each context but cannot prove
that the three contexts share one `jobon_id`. PostgreSQL gives no cross-table same-parent
inference from single-column FKs. The future Resumo/Pegamentos contracts (Q-SCOPE follow-ons)
must therefore add **explicit composite same-Job-On validation** (service-level check resolving
each context's `jobon_id` and asserting equality before write — the same-Job-On invariant of the
Authoritative Identity Rule), because domain validation is their obligation; a DB composite
trigger would be an additional backstop if the Architect later chooses one.

---

## 7. TEST COVERAGE

**Shipped P2-T04 tests proving the identity rules that exist today (all PASS at the accepted
baseline):**

| Test | Proves |
|---|---|
| `Migration003ToolJobOnDomainCoreTests.MIG4` | the six unique keys exist — incl. `cm_contexts_jobon_key`, `mf_contexts_jobon_key`, `bq_contexts_jobon_key` (≤1 context per role per occurrence) |
| `Migration003ToolJobOnDomainCoreTests.MIG5` | all FKs `RESTRICT` (contexts cannot outlive their Job On; Tool/Job On cannot be deleted under contexts) |
| `Migration003ToolJobOnDomainCoreTests.MIG3` | frozen-triple CHECKs (`tool_type = 'CM'|'MF'|'BQ'`) reject role violations |
| `JobOnRepositoryIntegrationTests.CTX1` | reselecting the **same** Tool reuses the **same** context row (identity stable) |
| `JobOnRepositoryIntegrationTests.CTX2` | a second context of the same type for one Job On is **rejected** (unique key) |
| `JobOnRepositoryIntegrationTests.CTX7` | reselecting a different Tool updates the same context row **in place** (same `cm_id`/`mf_id`/`bq_id`) |
| `JobOnRepositoryIntegrationTests.CTX5/CTX6` | a live Tool metadata change never rewrites the frozen triple; no refresh path |
| `JobOnRepositoryIntegrationTests.CTX8/CTX15` | remove deletes only that context row; a dependent-row removal fails closed (RESTRICT) |
| `JobOnRepositoryIntegrationTests.JOB7/JOB8` | contexts are created only for explicitly selected slots — **no symmetry rows** |
| `JobOnDuplicationIntegrationTests.DUP3/DUP4/DUP6/DUP7` | duplication creates a NEW `jobon_id` and NEW context ids, same canonical `tool_id`, frozen triple copied verbatim (same reference ⇒ new occurrence ⇒ new context IDs) |
| `JobOnContractTests.JOB21` | no code path derives a canonical identity from display text |
| `JobOnContractTests.CTX9/CTX11/CTX16/ORC3` | context/snapshot carry no excluded fact; no reverse collections; at most one context per type; no opaque key as canonical identity |
| `P2T04RegressionTests.BND5/BND7` | no fake/legacy production-identity token; no P2-T05+ vocabulary (incl. Resumo/Pegamentos) in P2-T04 sources |
| `P2T03ProductionScan` / `P2T03TypeScan` | `pegamentos`, `resumo`, `controlo_sheet`, `production` are forbidden-vocabulary tokens in the shared frontend baseline |

**Resumo/Pegamentos same-Job-On integrity tests:** **MISSING TEST COVERAGE** — by construction
(no Resumo/Pegamentos implementation exists; P2-T05's accepted matrix has no rows for them,
correctly reflecting Q-SCOPE). The future Resumo/Pegamentos contracts must add the same-Job-On
mixing refusals and their tests. Per the task mandate, no test was added.

---

## 8. RECOMMENDED NEXT ACTION

## NO CORRECTION REQUIRED

(For the current code and the accepted P2-T05 contract. Nothing shipped violates the identity
rules; `production_id` does not exist; Peso reuses the existing `cm_id`; P2-T05 mints none of
the component IDs.)

**Non-blocking observations to carry into future contracts (recorded, not corrections):**

- **O1 (binding for future contracts):** the Q-SCOPE follow-on contracts for Resumo and
  Pegamentos must contract and enforce **composite same-Job-On integrity** —
  `cm_id`/`mf_id`/`bq_id` of one record must resolve to one `jobon_id` — via explicit
  domain/repository validation (FKs alone cannot enforce it); tests must prove the J1+J2 mixing
  refusal. The same obligation applies to the Peso association candidate surface already
  contracted (candidate `cm_id` must resolve to the anchor `tool_id` — `ASSOCIATION_MISMATCH`).
- **O2 (informational):** dmo-master current `main` `modules/CONTROLO.md` still carries "Resumo
  exact component scope still requires workflow confirmation"; the Owner clarification
  (Resumo uses the existing Job On component-context IDs) and the old-era modular
  `CONTROLO.md` (`resumo_id → cm_id/mf_id/bq_id`, "no generic component ID invented") already
  define the required behavior; no code is affected. The future Resumo contract should cite the
  clarification.
- **O3 (cosmetic):** `JobOnRepository.FindProductionIdAsync` is a helper name for the
  `(reference, production_number)` duplicate-race refusal (returns the winning `jobon_id`); it is
  not and never was a production identity. Optional rename during a future refactor.

**STOP CONDITIONS HONORED.** Report ends here: no code modified, no migration, no Supabase
change, no P2-T06/T07/T08 work started.