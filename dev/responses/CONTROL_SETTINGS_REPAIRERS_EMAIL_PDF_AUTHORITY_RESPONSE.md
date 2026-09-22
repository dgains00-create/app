# CONTROL SETTINGS / REPAIRERS / EMAIL / PDF — AUTHORITY RECORDING RESPONSE

**Task class:** authority / governance documentation only.
**Status:** AUTHORITY RECORDED.
**Boundary:** no application code, no test, no schema, no migration, no route, no runtime
configuration, no Supabase/Auth change, no user provisioning. P2-T04 has not started.

---

## 1. Baseline

| Item | Value |
|---|---|
| Repository | `diogo-o/DMO-MODULAR` |
| Branch | `main` |
| HEAD at start | `3321d1963c939a4921a3ccfce9e6aeeebcd6f8ca` |
| `origin/main` at start | `3321d1963c939a4921a3ccfce9e6aeeebcd6f8ca` |
| Working tree at start | CLEAN |

## 2. Settled decisions recorded

1. **Controlo_Create owns settings.** The settings area (operational configuration needed by the
   Controlo / Peso / Boquilhas workflows) belongs to Controlo_Create. Controlo_Approve owns none
   of it.
2. **Controlo_Approve scope reduced** to exactly **Aprovar** and **Histórico de Pesos**.
3. **Repairer register** in `Controlo_Create → Definições`; **name is the only required data**;
   the user can add a repairer, edit a name and select an existing repairer for a machine
   assignment. No address/email/phone/supplier code/tax data/contact person. Active/inactive
   lifecycle recorded as an **implementation concern only**.
4. **Machine autonomy.** `B1`, `B2`, `B3`, `C1`, `C2`, `C3` each hold an **independent** repairer
   assignment. **No** grouping rule, **no** shared B/C repairer, **no** "Linha B"/"Linha C" model.
5. **Automatic repairer resolution in Boquilhas**: `machine → current repairer assignment →
   repairer resolved`; the operator normally does not re-select the repairer when the machine is
   known.
6. **Historical preservation**: changing an assignment must never rewrite historical Boquilhas
   records; the repairer used at the time of the movement is preserved. No specific database
   implementation is prescribed.
7. **PDF/document base directory** configuration owned by `Controlo_Create → Definições`:
   configure, change, verify/check accessibility.
8. **Email recipient lists** owned there: create/edit a named list, associate recipients, select
   for sending rules; recipients are never hardcoded in application code.
9. **Email routing** should use known production context where possible and avoid forcing
   re-selection of what configuration already determines; no exact routing rules invented.
10. **Email templates** owned there: subject, body, applicable document type/context, supporting
    contextual values already known. **No placeholder syntax fixed** (none exists in the repo).
11. **Boquilhas machine sidebar removed** from current visual/frontend authority; do not simulate
    Job On machine/reference state inside Boquilhas; reintroducible later only from real backend
    authority.
12. **Superseded visual assumptions** recorded (9 rows), with no source visual file deleted.

## 3. Outputs

| Path | Role |
|---|---|
| `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` | **New focused authority delta** — the settled decisions, their boundaries, superseded visual items, affected workstreams and open points |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | Authority pointer; settings-ownership, machine-autonomy and repairer rules in §8; updated P2-T04/T05/T06/T07/T08 scope; updated B2/B3/B4 blocker text; new §9 availability row; new §11 test requirements; 11 new Appendix A ledger rows |
| `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` | `Definições` ownership added to scope, contract gate, access, persistence, tests and acceptance |
| `plans/beta-workstreams/P2-T06-CONTROLO-APPROVE.md` | Scope reduced to Aprovar + Histórico de Pesos; settings explicitly excluded; access/test/acceptance updated |
| `plans/beta-workstreams/P2-T07-BOQUILHAS.md` | Register ownership, automatic resolution, machine autonomy, historical preservation, sidebar removal, B3 update |
| `plans/beta-workstreams/P2-T08-DOCUMENTS-PDF.md` | Configured base directory; sending via configured lists/templates; dependency on the P2-T05 settings |
| `plans/beta-workstreams/P2-T04-DOMAIN-CORE-TOOL-JOBON.md` | Machine-autonomy note; repairers/machine assignment explicitly out of scope |
| `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` | `Definições` is a surface, not a destination; Approve owns no settings; sidebar removed |
| `dev/responses/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_AUTHORITY_RESPONSE.md` | This response |

**New authority file created:** `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md`.

`reports/BETA_MASTER_RECONCILIATION.md` was **inspected and deliberately not edited**: it is an
audited current-state report of a specific commit, not a living governance file; rewriting it
would corrupt its evidence value.

## 4. Why a new file rather than only editing existing ones

Every existing authoritative file governs a *different* subject (Controlo Create workflow,
Controlo Approve workflow, Boquilhas workflow, documents/PDF, Tool/Job On domain, shared frontend
contract). The settings/repairer/email/PDF configuration area is **cross-cutting** and had no
owner: `Definições` appeared in none of them, the repairer register was recorded only as "not
administered by Boquilhas" with **no** named owner, and machine assignments and email
configuration were absent entirely.

Creating the delta gives the area a single authority home, and the existing files were then
**updated in the same change** so that no competing authority remains. No duplicate authority was
created.

## 5. Authority effects

| Item | Effect |
|---|---|
| **B2** (Peso/Pegamentos/Folha/Resumo contract) | Extended to cover the `Definições` surfaces. Still open. |
| **B3** (Boquilhas contract) | Partially resolved: the canonical `repairer_id` **directory source**, the machine-assignment model and the historical-preservation requirement are now settled. The physical schema/query/endpoint contract is still required. Still open. |
| **B4** (document generation + filesystem/PDF capability) | Extended to cover the operator-configured base directory and sending through configured lists/templates. Still open. |
| **B1** (Tool/Job On/context contract) | Unchanged. |
| Module catalogue / identities | **Unchanged.** No identity, order, destination or label change. |
| Authorization model | **Unchanged.** No new policy; `Definições` uses the existing Controlo_Create policy. |
| Availability / routes | **Unchanged.** `CurrentBuildAvailable` stays `[]`; no route registered. |

## 6. Boundary verification

```text
Application code modified                  = NO
Frontend source modified                   = NO
Tests modified / weakened / deleted        = NO
Database schema modified                   = NO
Migration added or modified                = NO
Supabase / .env / Auth modified            = NO
Users provisioned                          = NO
Route or Module availability registered    = NO
P2-T04 started                             = NO
P2-T02/P2-T03 accepted artifacts touched   = NO
Repairer fields beyond name invented       = NO
Machine grouping (Linha B / Linha C) added = NO
Email placeholder syntax invented          = NO
Exact email routing rules invented         = NO
Source visual files deleted                = NO
```

Verified by `git diff --name-only`: every changed/deleted file is documentation (`reports/**`,
`plans/**`, `docs/**`, `dev/responses/**`). No `src/**`, `tests/**`, migration, solution, project
or runtime configuration file appears in the diff.

## 7. Explicitly left open (not decided here)

Physical schema/keys for the new configuration; in-`Definições` surface organization; operational
meaning of "directory accessible"; machine identity representation; exact context→email-list
routing and fallbacks; email-template placeholder syntax; repairer override on a Boquilhas
movement; whether an active/inactive repairer lifecycle is implemented; per-assignment audit
trail; email send transport/account.

## 8. Next gate

This was an authority-recording task. It authorizes **no** implementation. The affected
workstreams (P2-T05, P2-T06, P2-T07, P2-T08) still require their authored, reviewed
`PLAN ACCEPT` contracts before any execution, and P2-T04 has not started.
