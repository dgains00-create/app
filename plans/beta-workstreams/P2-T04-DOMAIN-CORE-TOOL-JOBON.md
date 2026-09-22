# P2-T04 — Domain Core: Canonical Tool Identity + Job On Light + Ferramentas Light — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T04), §8, §9, §10, §11, §12.
Class: **Domain core** (shared by more than one operational module).
Depends on: **P2-T01, P2-T02, P2-T03** (shared primitives).
Authority blocker: **B1** — an authored, reviewed `PLAN ACCEPT` contract must exist before
execution (see §5).

## 1. Purpose

Establish the Canonical Tool identity and the Job On production occurrence that Controlo and
Boquilhas consume, and deliver Beta Job On Light plus the contextual Ferramentas Light Tool
ficha. This is the first operational slice and the first real domain model in `DMO.Domain`.

## 2. Authority

- `dmo-beta-master/modules/JOB_ON_LIGHT.md` (full) — included/explicitly-outside scope,
  duplicate workflow, reference search, acceptance criteria.
- `dmo-beta-master/modules/FERRAMENTAS_LIGHT.md` (full) — contextual-only Tool ficha, explicit
  select/create, no top-level route.
- `dmo-beta-master/architecture/CROSS_MODULE_FLOWS.md` — Job On → Controlo/Boquilhas flows.
- `dmo-beta-master/architecture/BACKEND_FRONTEND_MODEL.md` — canonical identity chain.
- `dmo-beta-master/architecture/RECORD_LIFECYCLES.md` §2–3 — no Job On-wide lifecycle state
  machine; CM/MF/BQ contexts freeze.
- `dmo-beta-master/contracts/IDENTITIES_AND_RELATIONSHIPS.md` — no reverse-ID arrays, no fake
  identities, `jobon_id` is the single production-occurrence identity (no revision id).
- `dmo-master/global/ACCESS_MODEL.md` §8 — Job On View/Create separation.
- `dmo-master` `BETA_VERSION.md` §2–§5 — minimum Beta Tool fields, simplified Job On identity,
  duplication rules.
- `dmo-beta-master/implementation/BETA_INTEGRATION_SEAMS.md` — Workstream B seam.
- Repo pattern: `docs/CREATION_AND_ASSOCIATION_LOGIC.md` (real-then-enriched identities).

## 3. Current implementation starting point

- Nothing operational exists (verified absence of `jobon`/`tool_id` outside catalog metadata).
- Reusable: `ModuleCatalog` identities `job-on-view`, `job-on-create`, `ferramentas`,
  `ferramentas-approve`; `ModuleAuthorizationPolicies`; `IModuleAccessService`; the
  `DmoDbContext` + EF migration + optimistic-concurrency framework; the shell + shared
  components from P2-T01…P2-T03.
- `src/DMO.Domain` currently contains only a README — this workstream adds its first types.

## 4. Scope

1. **Canonical Tool identity + master facts:** type CM/MF/BQ, reference, lot, machine/line
   compatibility, canonical quantity, processo `NNPB|PS`. **Different lot = different Tool.**
   One canonical registry; no duplicate Tools for contextual occurrences.
2. **`jobon_id`:** one production occurrence (reference, production number, machine, processo).
   **No** invented Job On lifecycle state machine; date-threshold edit is a warning, not hard
   immutability; delete forbidden when dependent operational facts exist.
3. **CM/MF/BQ contexts:** `jobon_id + tool_id -> context`, created only where the corresponding
   context is actually needed — never for symmetry. Contexts retain a direct relation to the
   canonical `tool_id` and freeze the values actually used.
4. **Job On create/view/edit** over the real `jobon_id`.
5. **Reference → productions** query with explicit human selection (never auto-resolved).
6. **Job On duplication:** new `jobon_id` + new context IDs; source unchanged; any historical
   source selectable (never forced to the latest); copied `tool_id`s retained until explicitly
   changed.
7. **One shared Tool search/select/create orchestration** returning a canonical `tool_id` and
   restoring origin state; consumes P2-T03's `ToolPicker`.
8. **Ferramentas Light contextual Tool ficha** — search/list/detail/create; **no top-level
   destination**.

## 5. Authority blocker B1 — required contract before execution

Per `dmo-beta-master/WORKFLOW.md`, the concrete backend/interface contract must be authored and
reviewed (`PLAN ACCEPT`) before implementation. The contract must fix, at minimum:

- physical schema and keys for Tool, Job On occurrence and CM/MF/BQ contexts, respecting
  "different lot = different Tool" and the production key (`reference + production_number`,
  extended only if authority requires);
- the query shapes for reference → productions, Tool search, and context resolution;
- transactional boundaries for create/duplicate;
- the dependency rule for delete refusal;
- the exact endpoint/route names and the module policy each carries.

Do **not** invent schema beyond what the authored contract fixes.

## 6. Explicit non-scope

- Full Job On lifecycle (revisions, verification catalogue, family sheets, print orchestration).
- Full Ferramentas change-request/approve lifecycle, technical-condition and utilisation
  dossier.
- Controlo calculations, Boquilhas movement ownership, Armazém.
- Any `production_id`, `job_on_revision_id`, or `tool.jobons[]` reverse array.
- Any fake `cm_id`/`jobon_id`.
- No `CurrentBuildAvailable` change and no route registration (P2-T10 does that).

## 7. Expected files/projects

```text
src/DMO.Domain/                                   (new Tool / JobOn / context primitives)
src/DMO.Application/                              (new Tools + JobOn application area,
                                                   repository contracts, use cases)
src/DMO.Infrastructure/Persistence/               (entities, configurations, repositories)
src/DMO.Infrastructure/Migrations/                (one NEW migration owning this schema)
src/DMO.Web/Pages/JobOn/                          (operational Job On surfaces)
src/DMO.Web/Pages/Ferramentas/                    (contextual Tool ficha)
src/DMO.Web/Endpoints/                            (tool + jobon endpoints)
tests/DMO.UnitTests/  tests/DMO.IntegrationTests/
```

## 8. Access requirements

Every route/action declares
`ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.<JobOnView|JobOnCreate|Ferramentas|FerramentasApprove>)`.
Job On View never grants Create actions. Ferramentas stays contextual-only and gains no
top-level destination. ADMIN gains nothing operational.

## 9. Backend / persistence requirements

Only what B1 fixes. Tool master; Job On occurrence; CM/MF/BQ contexts; an explicit
previous-source relation for duplication. No reverse-ID arrays. A new migration file only —
never edit migrations 001/002.

## 10. Required tests

See master plan §11 P2-T04. At minimum: different lot ⇒ different `tool_id`; ambiguity never
auto-resolves; duplication yields new `jobon_id` + contexts with an unchanged source; retained
Tool choices keep the same `tool_id`; date-threshold edit warns; delete refused with dependents;
direct-route denial per module; picker never auto-selects in the real surface.

## 11. Acceptance criteria

Every bullet in `modules/JOB_ON_LIGHT.md` "Acceptance criteria" and
`modules/FERRAMENTAS_LIGHT.md` "Acceptance criteria"; ambiguity requires explicit selection;
Ferramentas absent from top-level navigation; `CurrentBuildAvailable` unchanged.

## 12. Completion evidence

Committed implementation + tests + the `PLAN ACCEPT` contract this workstream was built against;
recorded build/test results.

## 13. Downstream dependents

P2-T05, P2-T07, P2-T08, P2-T10.
