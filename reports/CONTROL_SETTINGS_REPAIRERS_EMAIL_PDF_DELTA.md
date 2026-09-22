# CONTROL SETTINGS / REPAIRERS / EMAIL / PDF — AUTHORITY DELTA

**Class:** Functional authority — settled decisions. **Not** an implementation plan, **not** a
schema design, **not** a contract.
**Authority effect:** supersedes conflicting presentation assumptions wherever it conflicts with
them. It does not by itself close any authority blocker (B1–B4) and does not authorize any
implementation.
**Boundary:** documentation/authority only. This document changes no application code, no test,
no schema, no migration, no route, no runtime configuration, no Supabase/Auth setting and no
frontend source.

---

## 0. Purpose and authority position

This delta records newly **settled functional decisions** that were not yet written into the
existing authority set:

1. which Controlo module owns the settings area;
2. the reduced scope of Controlo Approve;
3. the repairer register and its minimal data;
4. per-machine autonomous repairer assignment (B1/B2/B3/C1/C2/C3);
5. automatic repairer resolution in Boquilhas;
6. historical preservation of the repairer actually used;
7. the local PDF/document directory setting;
8. email recipient lists;
9. email routing from known production context;
10. email templates;
11. removal of the Boquilhas machine sidebar from current authority;
12. the exact superseded visual assumptions.

### 0.1 Authority order (unchanged, extended by this file)

The repository's authority order from `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` continues to
apply:

1. `reports/BETA_MASTER_RECONCILIATION.md` — current-state and delta authority;
2. `diogo-o/dmo-beta-master` — Beta functional/scope/workflow/acceptance authority;
3. `diogo-o/dmo-master` — global architecture, identities, access model, module vocabulary;
4. `diogo-o/DMO-MODULAR` — implementation state and integration seams;
5. `diogo-o/workbench` / `diogo-o/dmo-work` — historical evidence only.

This file is **repository-recorded settled functional authority for the settings/repairer/email/
PDF area**. Where it and an existing repository authority file disagree, this file's settled
decisions win for this area; the existing files are updated in the same change to remove the
conflict (see §13). It does **not** override `dmo-master`/`dmo-beta-master` on anything outside
this area, and it does not reopen any closed reconciliation question.

### 0.2 What this file is *not* authority for

- It is not a physical schema, table or key design.
- It is not an implementation contract and closes no `PLAN ACCEPT` gate.
- It does not fix final placeholder syntax, final routing algorithms or final directory-validation
  procedure (see §7.3, §9.3, §10.3).
- It does not create, rename or reorder any `ModuleCatalog` identity (see §2.4).

---

## 1. Controlo_Create owns the settings area

### 1.1 Decision (settled)

The settings area belongs to **Controlo_Create**.

`Controlo_Create` includes the **operational configuration needed by the Controlo / Peso /
Boquilhas workflows**.

`Controlo_Approve` does **not** own these settings.

### 1.2 Functional model (user-facing)

```text
Controlo_Create
└── Definições
    ├── Reparadores              (repairer register — §3)
    ├── Reparador por máquina    (B1 B2 B3 C1 C2 C3 assignments — §4)
    ├── Diretório de PDF/documentos  (local base directory — §7)
    ├── Listas de email          (named recipient lists — §8)
    └── Templates de email       (subject/body/context — §10)
```

`Definições` is an **operational configuration surface inside Controlo_Create**. It is reached
through the Controlo_Create working area. The functional decision fixes **ownership**; the exact
in-surface organization (single page with sections, or several sub-surfaces) is an implementation
concern for the workstream that executes it and is not fixed here.

### 1.3 What "operational configuration" means here

Configuration that the Controlo/Peso/Boquilhas **workflows consume while doing work**: which
repairer a machine currently points to, where local documents are written, and who receives which
document by email.

It is **not**:

- access configuration (Templates, module grants, users);
- industrial/domain master data owned by another module (canonical Tool data, Job On occurrence,
  CM/MF/BQ contexts);
- a replacement for any module's own workflow state.

### 1.4 Not a new Admin module requirement

This decision does **not** create, require, imply or pre-authorize a new **global Admin module**
for these settings, and it does not move them into the existing ADMIN-only Administration
surfaces (`/Administration`, Users, Templates). The settings exist **inside Controlo_Create**
under its own module gate.

### 1.5 Access consequence

Definições and every action on it are gated by the **Controlo_Create** module policy
(`ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloCreate)`). A user who holds only
`controlo-approve` must not reach Definições, and the shared `controlo` destination does not merge
the grants. No new module policy, no new identity and no authorization-model change is introduced
by this decision (master plan §10 stays binding).

---

## 2. Controlo_Approve — reduced scope

### 2.1 Decision (settled)

Controlo_Approve is restricted to **exactly**:

- **Aprovar**
- **Histórico de Pesos**

### 2.2 Authority that must be removed or avoided

Controlo_Approve must not own, contain or reach:

| Not in Controlo_Approve | Owner |
|---|---|
| Definições | Controlo_Create |
| reparadores (repairer register / directory administration) | Controlo_Create |
| PDF directory configuration | Controlo_Create |
| email lists | Controlo_Create |
| email templates | Controlo_Create |
| any other operational setting | Controlo_Create |

### 2.3 Unchanged Approve responsibilities

This reduction removes **settings ownership only**. It does not reduce, redefine or reopen the
already-settled Approve **decision** responsibilities: pending/review list, review of the exact
submitted `peso_id` (no approval copy), per-CM human decisions, Folha decision, approve / reject /
reopen on the same record, attributed audit trail, and the explicitly confirmed
`Enviar para produção`. Those remain as recorded in the master plan §7 (P2-T06) and
`plans/beta-workstreams/P2-T06-CONTROLO-APPROVE.md`.

### 2.4 No identity change

No `ModuleCatalog` identity, order, destination or display label changes. `controlo-create` and
`controlo-approve` remain the two canonical assignable modules sharing the `controlo`
destination, with independent gates. **Histórico de Pesos** stays what it already is under the
binding terminology: **HISTÓRICO (local)** functionality inside Controlo Approve — it is **not**
the top-level HISTÓRICO GLOBAL module (technical identity `historia`), which remains DEFERRED BY
DESIGN for this Beta (master plan §0, §3.1, §14.1).

---

## 3. Repairer register

### 3.1 Decision (settled)

`Controlo_Create → Definições` contains a **simple repairer register**.

Repairer data is **intentionally minimal**.

### 3.2 Required repairer data

- **Name**

### 3.3 Fields that must NOT be invented as mandatory

The following must not be introduced as required (or as functional fields at all) in this
authority:

- address
- email
- phone
- supplier code
- tax data (NIF / VAT / fiscal fields)
- contact person

They are **not currently required**. Adding any of them later is a separate authority decision and
must not be assumed, pre-modelled or reserved "for symmetry".

### 3.4 Required user capabilities

The user must be able to:

1. **add** a repairer;
2. **edit** a repairer name;
3. **select** an existing repairer for a machine assignment.

### 3.5 Lifecycle — implementation concern only

If an active/inactive lifecycle is needed to preserve history safely (for example so a repairer
that is no longer used can stop appearing in selection without destroying the historical record),
it is recorded here **only as an implementation concern**. It must not expand the user-facing
functional model beyond what is necessary: the user-facing model stays
**add / edit name / select**. No status vocabulary, no mandatory deactivation workflow and no
administrative lifecycle UI is fixed by this authority.

### 3.6 Ownership boundary with Boquilhas

The register lives in Controlo_Create → Definições. Boquilhas **selects and consumes** a repairer;
Boquilhas does **not** administer the repairer directory. The existing rule that a repairer is
"not administered by Boquilhas" (master plan §8, Boquilhas row) is preserved and is now paired
with an explicit owner: **the repairer register is owned by Controlo_Create → Definições**.

This is the first recorded resolution of the open part of authority blocker **B3**, whose text
requires "canonical `repairer_id` directory source". The **directory source is now settled**
(Controlo_Create → Definições). B3 is **not** closed by this document: the physical schema, the
`repairer_id` key shape and the query/endpoint contract still require an authored, reviewed
`PLAN ACCEPT` before execution.

---

## 4. Each machine is autonomous

### 4.1 Decision (settled)

The operational machines are:

```text
B1   B2   B3   C1   C2   C3
```

Each machine has its **OWN independent repairer assignment**.

### 4.2 There is NO grouping rule

The following are **forbidden** by this authority:

- all B machines share one repairer;
- all C machines share one repairer;
- modelling the assignments as **"Linha B"** and **"Linha C"**;
- any other grouping, inheritance or cascade between machines.

### 4.3 Correct model

```text
B1 → selected repairer
B2 → selected repairer
B3 → selected repairer
C1 → selected repairer
C2 → selected repairer
C3 → selected repairer
```

**Changing B1 must not change B2, B3, C1, C2 or C3.** Each assignment can be changed
independently in `Controlo_Create → Definições`.

### 4.4 Consequences for existing vocabulary

The machines B1/B2/B3/C1/C2/C3 already appear in this repository's document-naming convention
(`Peso_<reference>_<machine>.pdf`, master plan §7 P2-T08; `docs/CREATION_AND_ASSOCIATION_LOGIC.md`
"PDF directory logic"). The naming convention is unchanged. What this decision adds is that
**the machine is an independent operational actor with its own current repairer assignment**, and
that the machine is never resolved through a line-group.

**Not invented by this document:** machine identity is not a new canonical identity, no
`machine_id` scheme is fixed, no machine registry is created, and no machine set is declared
exhaustive for all future modules. The six machines above are the operational machines this
decision covers. Where the machine already exists as known production context (Job On
occurrence / canonical Tool machine compatibility), it is **consumed**, not duplicated.

---

## 5. Boquilhas — automatic repairer resolution

### 5.1 Decision (settled)

In Boquilhas, when a registration/movement is associated with a machine:

```text
machine
→ current repairer assignment
→ repairer automatically resolved
```

### 5.2 Worked example

```text
B1
→ current B1 repairer
→ repairer automatically associated with the Boquilhas record
```

### 5.3 Operator expectation

The operator should **not normally need to manually choose** the repairer for every Boquilhas
registration **when the machine is already known**.

### 5.4 Boundaries of this decision

- This **does not** remove the requirement that the repairer actually used be a recorded fact
  (§6). It says the value is resolved from configuration instead of being re-typed per record.
- This **does not** re-define which Boquilhas movements carry a repairer. The existing settled
  rule (canonical `repairer_id` stored on external **Saída** with historical retention) is
  unchanged and is not contradicted: automatic resolution supplies the value; the movement still
  records it.
- Where an override is genuinely required (for example a repairer other than the machine's current
  assignment was actually used), it remains an implementation-level decision for the Boquilhas
  workstream's authored contract **unless and until** a separate authority decision fixes it.
  This document does not invent an override rule and does not forbid one.
- This **does not** make Boquilhas depend on Job On, and does not authorize simulating Job On
  context inside Boquilhas (§11).

---

## 6. Historical preservation

### 6.1 Decision (settled, binding)

Changing a machine's repairer assignment in the future **MUST NOT** rewrite historical Boquilhas
records.

### 6.2 Worked example

```text
2026:  B1 → Repairer A
       Boquilha movement recorded using Repairer A

2027:  B1 → Repairer C

The 2026 movement must still show Repairer A.
```

### 6.3 The rule

**The repairer used at the time of the movement must be historically preserved.**

This is the same historical-preservation principle already established for the other retained
facts in this system — the Boquilhas close snapshot, movement before/after audit, frozen
CM/MF/BQ contexts, preserved Peso measurement/calculation facts and the preserved Pegamentos
nominal. It is now explicitly extended to the **repairer of a Boquilhas record**.

### 6.4 Implementation neutrality (deliberate)

This document **does not prescribe** a specific database implementation (snapshot column vs
retained reference, denormalized label vs identity-only relation, etc.). The existing
architecture already establishes the pattern family to choose from: minimum relation, minimum
snapshot, preserved historical context, no reverse-ID arrays. The authored Boquilhas contract
(B3) fixes the exact mechanism and must be consistent with whichever existing pattern the
architecture already establishes for retained historical facts.

The functional requirement is what is binding here: **a later assignment change is not allowed to
change what an earlier record shows.**

---

## 7. PDF / document directory settings

### 7.1 Decision (settled)

`Controlo_Create → Definições` also owns the **local PDF/document directory configuration**.

### 7.2 Required behavior

1. configure the **base directory**;
2. **change** the directory;
3. **verify/check whether the directory is accessible**.

### 7.3 Ownership

This is operational configuration and **does not belong in Controlo_Approve**. No new **global
Admin module** requirement is invented for this setting; it lives in Controlo_Create → Definições
under the Controlo_Create gate.

### 7.4 Relationship to the existing directory convention (unchanged)

The **directory convention** already settled elsewhere is **not** changed by this decision:

```text
<reference>/
└── <production-number>/
    ├── Peso_<reference>_<machine>.pdf
    ├── Pegamentos_<reference>_<machine>.pdf
    └── Resume_<reference>_<machine>.pdf
```

This decision fixes only **where the base of that structure is configured** and that it is
configurable, changeable and checkable. It does not change filenames, does not change the
`<reference>/<production-number>/` structure, does not make the path an identity, and does not
alter any document availability state.

**Not fixed here (implementation concern):** what "accessible" means operationally (existence,
read/write, permission, reachable root), how the check is presented, and what happens to pending
work when the configured directory is unavailable. The existing rule that a **lookup failure is
not an empty result** and that a missing optional output is **not** a generic error remains
binding.

### 7.5 Relationship to P2-T08

Document **generation**, naming and availability states remain P2-T08 and remain blocked by B4.
What this delta adds is that the **base directory is operator-configurable through Controlo_Create
→ Definições** rather than a fixed deployment constant. Nothing in P2-T08's settled rules is
reopened.

---

## 8. Email lists

### 8.1 Decision (settled)

`Controlo_Create → Definições` also owns **email recipient lists**.

### 8.2 Required user capabilities

- create/edit a **named** email list;
- **associate** email recipients with that list;
- **select/use** lists for document sending rules.

### 8.3 Example concept

```text
List 1
→ recipients (...)

List 2
→ recipients (...)
```

### 8.4 Binding rule

**Do not hardcode recipient addresses in application code.** Recipients are configuration data
owned by Controlo_Create → Definições.

### 8.5 Not fixed here

The number of lists, the list naming constraints, the internal recipient representation, whether
one address may appear in several lists, and validation/duplicate handling are implementation
concerns for the workstream that executes this decision. No such rule is invented here.

---

## 9. Email routing

### 9.1 Decision (settled)

The email workflow should use **known production context where possible**.

### 9.2 Example concept

```text
Peso PDF
→ production/machine context already known
→ appropriate configured email list
→ email template
→ PDF attachment
→ preview
→ send
```

### 9.3 Binding intent

The system should **avoid forcing the operator to manually re-select known context or recipients
when configuration already determines them**.

### 9.4 Explicit limitation

**Do not invent exact automatic routing rules beyond what existing authority supports.** This
document fixes the *intent* (use known context; do not re-ask for what configuration already
determines) and the *components* it draws on (the configured list of §8, the template of §10, the
generated document of §7/P2-T08). It does **not** fix:

- the exact machine/context → list mapping rule;
- fallback behaviour when no list is configured for a context;
- multi-recipient / multi-list precedence;
- whether preview is mandatory before send in every path;
- the exact transport or account used to send.

Those belong to the implementing workstream's authored contract, must be consistent with existing
authority, and must not contradict §8.4 (no hardcoded recipients) or §2.1 (no settings in
Controlo_Approve).

### 9.5 Boundary

Email sending is **not** part of the already-settled document rules in a way that changes them:
the structured record remains the only truth, the filename/path remains not an identity, and a
frozen output is not silently regenerated. Sending a document does not create a second document
authority.

---

## 10. Email templates

### 10.1 Decision (settled)

`Controlo_Create → Definições` owns **email templates**.

### 10.2 A template may contain

- subject;
- body;
- applicable **document type/context** where needed.

### 10.3 Contextual values

Templates should support contextual values already known by the application, such as
**reference / production / machine / date**.

### 10.4 Explicit limitation on syntax

**Do not invent the final placeholder syntax in this authority task unless one already exists.**

At the time of writing, this repository records **no** accepted email-template placeholder syntax
anywhere. Therefore **no syntax is fixed here**: no delimiter, no token vocabulary, no binding
grammar, no escaping rule and no missing-value behaviour is established by this document. The
implementing workstream must adopt a syntax that is authored, reviewed and accepted in its own
contract, or explicitly inherit one if authority for it already exists at that time.

### 10.5 Not fixed here

Template versioning, template language/locale, template selection precedence and template
permission granularity are not fixed by this document.

---

## 11. Boquilhas sidebar — removed from current authority

### 11.1 The finding (recorded)

The current Boquilhas visual authority contains a **machine/sidebar** concept that depends on
**Job On operational context**.

That sidebar is **NOT currently valid** as an independently functional Boquilhas surface,
because the Job On operational context it depends on does not exist in the current Beta surface.

### 11.2 Decision (settled)

- **Remove** the Boquilhas machine sidebar from the current visual/frontend authority.
- **Do not simulate** Job On machine/reference state inside Boquilhas.
- If **future Job On integration** justifies such context, it **can be reintroduced later from
  real backend authority**.

### 11.3 What this does and does not change

- It **removes** a presentation element from current authority. It does not create a replacement
  panel, and it does not authorize inventing a Boquilhas-local machine state.
- It does **not** remove the settled Boquilhas requirement for a **production-line contextual
  panel reading (not owning) production context**, which is a read-only reading of context
  supplied by real authority and is a different thing from a locally simulated machine/reference
  sidebar.
- It **does not** change any Boquilhas workflow, movement type, balance rule, history filter,
  close/reopen rule or repairer rule.
- It **does not** implement the UI change: this task is authority-only. The implementing
  workstream performs the removal.

---

## 12. Superseded visual assumptions

The current supplied visual prototypes may still show old presentation assumptions. The following
are recorded as **SUPERSEDED** where they conflict with the settled decisions above.

| # | Prototype shows | Status | Settled authority |
|---|---|---|---|
| V1 | **Definições** inside **Controlo_Approve** | **SUPERSEDED** | `Controlo_Create → Definições` owns settings (§1); Approve is restricted to Aprovar + Histórico de Pesos (§2) |
| V2 | **Boquilhas machine sidebar** (Job-On-dependent) | **SUPERSEDED** | removed from current authority; not simulated; reintroducible later from real backend authority (§11) |
| V3 | Repairer administration reachable from Controlo_Approve (or from Boquilhas) | **SUPERSEDED** | repairer register lives in `Controlo_Create → Definições` (§3.6) |
| V4 | PDF/document directory configured in Controlo_Approve or as a global Admin setting | **SUPERSEDED** | `Controlo_Create → Definições` (§7) |
| V5 | Email lists / email templates presented under Controlo_Approve or as a global Admin setting | **SUPERSEDED** | `Controlo_Create → Definições` (§8, §10) |
| V6 | A single repairer shown per **line/group** (B / C), or any grouping of machine-to-repairer assignment | **SUPERSEDED** | six independent machine assignments, no grouping rule (§4) |
| V7 | Repairer typed/selected manually on every Boquilhas registration although the machine is known | **SUPERSEDED** | automatic resolution from the machine's current assignment (§5) |
| V8 | Any presentation in which a repairer assignment change retroactively changes an already-recorded movement | **SUPERSEDED** | historical preservation (§6) |
| V9 | Repairer forms presenting address / email / phone / supplier code / tax data / contact person as required | **SUPERSEDED** | name only is required; those fields are not required (§3.3) |

### 12.1 Handling of prototype material

The conflicting items are recorded as superseded **details**. This document does **not** delete,
move or edit any source visual file. Superseding a prototype detail does not make the prototype a
functional authority: functional authority remains the documents named in §0.1 plus this delta.

### 12.2 Where "Definições" may legitimately still appear visually

A visual prototype may show a Definições entry if it is presented as belonging to
**Controlo_Create**. What is superseded is **Definições under Controlo_Approve**, not the
existence of a Definições surface.

---

## 13. Files updated by this authority change

This delta is paired with targeted updates to the existing authority files it would otherwise
conflict with. No duplicate competing authority is created.

| File | Update |
|---|---|
| `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` | **this file** — the settled decisions |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §0.4 authority pointer to this delta; settings-ownership row in §8; repairer row in §8; new §7 text for P2-T05/P2-T06/P2-T07/P2-T08; Appendix A rows |
| `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` | Definições ownership added to scope |
| `plans/beta-workstreams/P2-T06-CONTROLO-APPROVE.md` | reduced scope recorded; settings explicitly out |
| `plans/beta-workstreams/P2-T07-BOQUILHAS.md` | repairer register owner, automatic resolution, historical preservation, machine autonomy, sidebar removal |
| `plans/beta-workstreams/P2-T08-DOCUMENTS-PDF.md` | configurable base directory, email lists/templates/routing ownership and non-scope |
| `plans/beta-workstreams/P2-T04-DOMAIN-CORE-TOOL-JOBON.md` | machine autonomy + no line-grouping note where machine context is consumed |
| `dev/responses/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_AUTHORITY_RESPONSE.md` | response record for this authority task |

`reports/BETA_MASTER_RECONCILIATION.md` is **not** edited: it is an audited current-state report of
a specific commit, not a living governance file, and rewriting it would corrupt its evidence.

---

## 14. Affected future implementation workstreams

No workstream is started, authorized or re-sequenced by this document. The table records **which**
future workstreams must implement these decisions, and which pages/areas are affected.

| Workstream | Effect of this delta | Affected pages / areas |
|---|---|---|
| **P2-T04** — Domain core (Tool + Job On Light + Ferramentas Light) | **Consumes**, no new scope. Machine context consumed, never grouped into a line; no `machine_id` scheme invented; repairer/machine assignment is **not** modelled here. | Job On Light surfaces, `ProductionContextStrip` context |
| **P2-T05** — Controlo Create | **Scope extended** with `Controlo_Create → Definições`: repairer register, machine repairer assignments, PDF/document base directory, email lists, email templates. New surfaces and their endpoints; all under the Controlo_Create gate. B2's authored contract must now include these. | `Pages/Controlo/` Create side; new Definições area (repairers, machine assignments, document directory, email lists, email templates) |
| **P2-T06** — Controlo Approve | **Scope reduced** to Aprovar + Histórico de Pesos. Any planned settings surface is removed. | `Pages/Controlo/Approve/`; pending list; review; decision; history. **No** Definições surface |
| **P2-T07** — Boquilhas | **Scope refined**: repairer resolved automatically from the machine's current assignment; the repairer used is historically preserved; the repairer directory is consumed, not administered; the machine sidebar is removed from the surface. B3's authored contract must reflect all of this. | `Pages/Boquilhas/`; movement forms; recent movements; History filters; **no** machine sidebar |
| **P2-T08** — Documents / PDF | **Configured base directory** (Controlo_Create → Definições) instead of a fixed constant; email lists / templates / routing consumed for sending. Generation, naming, availability states and B4 are otherwise unchanged. | Document availability surfaces; open/regenerate actions; sending flow (list + template + attachment + preview + send) |
| **P2-T10** — Final integration | **No registration change by this delta.** Definições is reachable **inside** Controlo_Create — it is **not** a new destination and must not be registered as one. `CurrentBuildAvailable` is untouched here. | Controlo create destination surface only; no new destination, no new route |
| Direct-route / access verification | Definições and each settings action must be denied to a caller holding only `controlo-approve`. | Test set for the Controlo surfaces |

### 14.1 Effect on authority blockers

| Blocker | Effect |
|---|---|
| **B2** (Peso/Pegamentos/Folha/Resumo contract → blocks P2-T05/P2-T06) | **Extended.** The authored contract must now also cover the Definições surfaces and their endpoints. Still open. |
| **B3** (Boquilhas contract → blocks P2-T07) | **Partially resolved.** The canonical `repairer_id` **directory source** is now settled (Controlo_Create → Definições), the machine-assignment model is settled, and the historical-preservation requirement is settled. The physical schema/query/endpoint contract is still required. Still open. |
| **B4** (document generation + filesystem/PDF capability → blocks P2-T08) | **Extended.** The base directory is operator-configurable via Controlo_Create → Definições; the contract must cover configuration, change and accessibility verification. Still open. |
| **B1** (Tool/Job On/context contract → blocks P2-T04) | **Unchanged.** |

---

## 15. Explicit non-changes

The following are unchanged and must not be read as changed by this delta:

1. **Module identities.** No `ModuleCatalog` identity, order, destination or label changes.
   `controlo-create` and `controlo-approve` still share the `controlo` destination with
   independent gates.
2. **Authorization model.** No new module policy, no ADMIN super-user, no merge of Create/Approve
   grants, no change to `ModuleAuthorizationPolicies` or the administration gate.
3. **HISTÓRICO terminology.** HISTÓRICO (local) and HISTÓRICO GLOBAL (`historia`, DEFERRED BY
   DESIGN) remain distinct. "Histórico de Pesos" is HISTÓRICO (local).
4. **Document convention.** Filenames, `<reference>/<production-number>/` structure, availability
   states, "record ≠ generated document ≠ path" and "no filesystem path in output" are unchanged.
5. **Boquilhas rules.** Four movement types only, `Editar` is an action not a type, derived
   balance, `business_date` ⊥ `recorded_at`, close/reopen on the same `boquilhas_id`, no fake
   Job On/`bq_id` in the standalone flow — all unchanged.
6. **Controlo Create rules.** Formulas, water range, individual CM results, Comparação selection
   and staleness, Folha ≠ Resumo, `Job On por associar`, no approval copy — all unchanged.
7. **Availability.** `ModuleRegistrations.CurrentBuildAvailable` stays `[]`; no route is
   registered; no destination is exposed.
8. **Protected foundation.** Master plan §12's 24 protected areas and their extension seams are
   untouched.
9. **The fixed desktop layout policy.** Binds every surface this delta affects, including the new
   Definições surfaces.
10. **P2-T04.** Not started, and not started by this document.

---

## 16. Open points deliberately left open

Recorded so a later reader does not mistake silence for a decision:

| # | Open point | Who fixes it |
|---|---|---|
| O1 | Physical schema, keys and query shapes for repairers, machine assignments, email lists and email templates | B2/B3/B4 authored contracts |
| O2 | Exact in-Definições surface organization (one page with sections vs several sub-surfaces) | P2-T05 implementation within the accepted contract |
| O3 | Meaning and presentation of "directory accessible" | B4 contract |
| O4 | Machine identity representation (`machine_id` scheme or consumption of existing machine context) | B2/B3 contracts, consistent with §4.4 |
| O5 | Exact machine/context → email list routing rule and fallbacks | Implementing contract, per §9.4 |
| O6 | Email-template placeholder syntax | Implemented contract; **not** fixed here (§10.4) |
| O7 | Repairer override on a Boquilhas movement when the machine's current assignment was not the one actually used | B3 contract, per §5.4 |
| O8 | Whether an active/inactive repairer lifecycle is implemented, and its shape | Implementation concern per §3.5 |
| O9 | Whether a per-machine assignment change requires an audit trail, and its shape | B2 contract |
| O10 | Send transport/account and its configuration ownership | Separate decision if needed; out of scope here |

---

## 17. Verification statement

- Application code modified by this authority change: **NO**.
- Tests modified: **NO**.
- Database schema or migration added/modified: **NO**.
- Supabase, `.env` or Auth modified: **NO**.
- Route or module availability registered: **NO**.
- Users provisioned: **NO**.
- P2-T04 (or any later workstream) started: **NO**.
- Source visual files deleted or edited: **NO**.
- Only documentation/authority files changed: **YES**.
