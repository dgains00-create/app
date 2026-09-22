# P2-T06 — Controlo Approve — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T06), §8, §9, §10, §11, §12.
Class: **Operational module** (Workstream D).
Depends on: **P2-T05 (strictly)** — D consumes C's submitted Peso read model and must not fork
the renderer.
Authority blocker: **B2** (shared with P2-T05).

## Binding fixed desktop layout

This handoff inherits the master plan's **DMO FIXED DESKTOP LAYOUT POLICY**. The pending list,
review surface, audit/history region and DecisionBar are designed first at **1366 × 768** with
stable columns and stable action placement. Larger desktops preserve the same operational
composition. Smaller windows scroll instead of moving the DecisionBar, changing review order,
hiding required columns or converting the list to cards. Mobile/tablet variants are out of
scope.

## 1. Purpose

Deliver the Approve side of Controlo: pending list, review of the exact submitted record,
approve/reject/reopen on the same `peso_id`, per-CM and Folha decisions, and attributed
decision/audit history.

## 2. Authority

- `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §2 — **settled: Controlo_Approve is
  restricted to Aprovar + Histórico de Pesos and owns no operational setting.** Definições,
  repairers, the PDF directory configuration, email lists, email templates and every other
  operational setting belong to `Controlo_Create → Definições`.
- `dmo-beta-master/modules/CONTROLO_APPROVE.md` (full) — included scope and acceptance.
- `dmo-beta-master/architecture/RECORD_LIFECYCLES.md` §4 — approval/rejection/reopen on the
  same `peso_id`; warnings never auto-reject.
- `dmo-beta-master/architecture/ACCESS_AND_NAVIGATION.md` — independent Create/Approve
  enforcement; hidden controls are presentation only.
- `dmo-beta-master/implementation/BETA_INTEGRATION_SEAMS.md` — "C → D seam"; Workstream D.
- `dmo-master/global/ACCESS_MODEL.md` §9 — Controlo Approve as a distinct assignable module.
- `dmo-beta-master/ACCEPTANCE_MATRIX.md` §6 — independent Approve authorization from Create.

> **Terminology (binding — master plan §0):** the History in this workstream is **HISTÓRICO
> (local)** — history functionality *inside* the module. It is not the top-level
> **HISTÓRICO GLOBAL** module (technical identity `historia`), which is DEFERRED BY DESIGN for
> this Beta. Do not merge the two, and do not treat one as authority for the other.

## 3. Current implementation starting point

Nothing operational exists. The two-module identity split and the sibling-non-satisfaction rule
already exist and are tested
(`NoProfilesRegressionTests.SiblingModule_SameDestination_DoesNotSatisfyGate`).

## 4. Scope

**Settled scope — exactly two responsibilities:** **Aprovar** and **Histórico de Pesos**
(`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §2).

1. Pending/review list with filters over backend-reported reviewable facts.
2. Open the **exact submitted** `peso_id` (no approval copy).
3. Read measurements/results/warnings/production context.
4. Read the persisted explicit Comparação relation and enough current/previous context to
   understand it **without heuristic reconstruction**.
5. Per-CM human decisions (`Manter` / `Colocar de parte`) where required.
6. Folha decision/review on the exact persisted `controlo_sheet_id`.
7. Approve; reject with note where required; reopen on the **same** record preserving
   decision/attribution history.
8. Attributed audit/history.
9. Explicit confirmed `Enviar para produção` only where the published contract allows it.

### 4.1 Settings are excluded (settled)

This module owns **no** settings surface. Do **not** create, add or reach:

- Definições;
- the repairer register, repairer administration or per-machine repairer assignments;
- the PDF/document directory configuration;
- email recipient lists;
- email templates;
- any other operational setting.

All of the above belong to `Controlo_Create → Definições` and are gated by the Controlo_Create
policy. If the delivered frontend prototype shows a Definições entry under Controlo_Approve, that
presentation detail is **SUPERSEDED** (`…DELTA.md` §12 row V1) and must not be implemented.

**Histórico de Pesos** in this scope is HISTÓRICO (local) — history inside this module. It is not
HISTÓRICO GLOBAL (`historia`), which stays DEFERRED BY DESIGN.

## 5. Explicit non-scope

- **Operational settings of any kind** (owned by Controlo_Create → Definições).
- Editing submitted measurement facts without a reopen.
- Approval-copy Peso; redefining formulas or Comparação pairing.
- Forking or copying C's Peso renderer.
- Job On / Ferramentas / Boquilhas ownership.
- Automatic decisions from warnings.
- No `CurrentBuildAvailable` change and no route registration.

## 6. Expected files/projects

```text
src/DMO.Application/ControloApprove/              (use cases + repository contracts)
src/DMO.Infrastructure/Persistence/ + Migrations/ (decision/audit persistence, NEW migration)
src/DMO.Web/Pages/Controlo/Approve/               (review mode reusing C's read model;
                                                   NO settings surface)
src/DMO.Web/Endpoints/
tests/DMO.UnitTests/  tests/DMO.IntegrationTests/
```

## 7. Access requirements

`ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloApprove)` on every Approve
route/action. Create does not grant Approve and vice versa; the shared `controlo` destination
never merges the grants. A `controlo-approve`-only caller is denied on every Controlo_Create
→ Definições route/action.

## 8. Backend / persistence requirements

Status transitions on the existing `peso_id`; per-CM decision facts; `controlo_sheet_id`
decision; actor/time attribution as backend facts; a historical decision trail; concurrency
behavior per the accepted contract.

## 9. Required tests

See master plan §11 P2-T06: warnings never decide; submitted facts read-only until an authorized
reopen; approve/reject/reopen mutate the same record; actor/time from backend facts; Create vs
Approve denial on the shared destination; review mode reuses the C read model.

Additionally (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §2): the Approve surface
contains no settings surface; a `controlo-approve`-only caller cannot reach Controlo_Create
→ Definições; the module owns no repairer, directory, email-list or email-template responsibility.

## 10. Acceptance criteria

Every bullet in `modules/CONTROLO_APPROVE.md` "Acceptance criteria"; no approval copy; no forked
renderer; direct route/action denial is server-side; the Approve scope is exactly
**Aprovar + Histórico de Pesos** with no operational settings; `CurrentBuildAvailable` unchanged.

## 11. Completion evidence

Committed implementation + tests + explicit statement that C's read model was reused (with the
contract test proving it), not copied.

## 12. Downstream dependents

P2-T08, P2-T10.
