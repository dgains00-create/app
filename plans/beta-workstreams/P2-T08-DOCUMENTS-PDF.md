# P2-T08 — Documents / PDF / Directory Convention / Availability — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T08), §8, §9, §10, §11, §12.
Class: **Cross-cutting feature**.
Depends on: **P2-T05** (Peso **and the `Definições` settings it owns** — the configured base
directory, email lists and email templates), **P2-T06** (approved/decision state), **P2-T07**
(Boquilhas where a file state applies).
Authority blocker: **B4** — document-generation contract and the filesystem/PDF capability must
be authorized first.

## Binding fixed desktop layout

This handoff inherits the master plan's **DMO FIXED DESKTOP LAYOUT POLICY** for every
document/history/availability surface it renders. Design and validation start at
**1366 × 768**. Document actions, version/availability information and history access keep
stable structural locations at larger desktops. Smaller windows scroll rather than moving
actions into different regions or creating alternate card/mobile layouts. Generated PDF
geometry remains governed by its document contract, not by browser viewport breakpoints.

## 1. Purpose

Deliver the document layer: deterministic naming, the directory convention, availability
presentation and safe historical rendering — with the structured record remaining the only
truth.

## 2. Authority

- `reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §7, §8, §9, §10 — **settled authority
  for this handoff's configuration inputs**: the document **base directory is operator-configured
  in `Controlo_Create → Definições`** (configure / change / verify accessibility); the **email
  recipient lists** and **email templates** are owned there; sending must use known production
  context where possible and must never hardcode recipient addresses.
- `dmo-beta-master/contracts/DOCUMENTS_AND_FILES.md` (full) — identity rules, naming, directory
  convention, availability states, PDF rules, acceptance (§8).
- `dmo-beta-master/sources/dmo-master/global/DOCUMENT_FILE_MODEL.md` — archived upstream
  snapshot.
- `dmo-beta-master/architecture/APPLICATION_FOUNDATION.md` "Documents" — record != generated
  document != path.
- `dmo-beta-master/ACCEPTANCE_MATRIX.md` §8 — documents/PDF gate.
- `dmo-master` `BETA_VERSION.md` §8 — directory convention.
- Repo status: `docs/SKELETON_RATIONALIZATION_P1-T01.md` records `Infrastructure/Files` and
  `Infrastructure/Pdf` as not yet implemented.

## 3. Current implementation starting point

Nothing exists. `DMO.Domain` has no document types; there is no Files or Pdf project. P2-T01's
`AvailabilityState` provides the presentation vocabulary.

## 4. Scope

1. Structured owning record remains the **only** truth; filename/path is never a join key or
   identity.
2. Deterministic filenames: `Peso_<reference>_<line>.pdf`, `Pegamentos_<reference>_<line>.pdf`,
   `Resume_<reference>_<line>.pdf`.
3. Directory convention: `<reference>/<production-number>/`.
4. Historical rendering uses the preserved historical context and **never** regenerates from
   today's mutable Tool fields.
5. Availability presentation using exactly `Disponível`, `Ainda não gerado`,
   `A aguardar aprovação`, `Workspace indisponível`, `Ficheiro em falta`, `Versões disponíveis`
   (mapped to P2-T01's `AvailabilityState`).
6. A missing optional output (e.g. Pegamentos) is **not** a generic error; a lookup failure is
   **not** an empty result.
7. Local filesystem paths (`file:///`) are **never** printed into a PDF.
8. No document metadata table introduced merely for symmetry; official/frozen outputs are not
   silently regenerated.
9. **Configured base directory** (`…DELTA.md` §7): the base of the
   `<reference>/<production-number>/` structure is resolved from the operator-configured
   `Controlo_Create → Definições` setting — not from a hardcoded constant — and must follow a
   change to that setting. The names, the directory structure and every availability state are
   unchanged. Where the configured directory is not accessible, that condition must be
   distinguishable from a missing file and from an empty result.
10. **Sending through configured lists and templates** (`…DELTA.md` §8, §9, §10): document
    sending selects the configured **email list** and **email template**, attaches the generated
    document, previews where the contract requires it, and sends. Recipient addresses are
    **never** hardcoded in application code. The workflow should use known production context
    where possible and must not force the operator to re-select context or recipients that
    configuration already determines.
    **Do not invent** exact automatic routing rules beyond that intent, and **do not fix** an
    email-template placeholder syntax — none exists in this repository today.

## 5. Authority blocker B4 — required contract before execution

The authored, reviewed contract must fix: the generation capability location (inside the
existing projects unless an Architect-approved need justifies otherwise), the ownership/gating
of open/regenerate actions, the exact state transitions that produce each availability value,
and the endpoint names with their owning workflow permission.

It must **also** fix (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §7, §9): how the
operator-configured base directory is read and followed, what "accessible" means operationally
and how a failed check is represented, and the sending path's use of the configured email list
and template — including what happens when no list is configured for a context and whether
preview is mandatory on every path. It must **not** invent exact routing rules beyond that intent
or a template placeholder syntax.

## 6. Explicit non-scope

- Inventing new Job On document identities (the three official Job On outputs are owned by the
  global Job On contract and are not required by Beta Job On Light).
- A mandatory Pegamentos file.
- Introducing a document table/identity just for symmetry.
- **The settings surfaces themselves** — the repairer register, machine assignments, document
  base-directory configuration, email lists and email templates are owned and implemented by
  **P2-T05 under `Controlo_Create → Definições`**. P2-T08 only **consumes** the configured
  directory, list and template; it must not create a second settings surface, must not move them
  into a global Admin module and must not place them in Controlo_Approve.
- Inventing exact automatic email routing rules beyond "use known production context"; fixing the
  email-template placeholder syntax; provisioning an email transport/account.
- No `CurrentBuildAvailable` change and no route registration.

## 7. Expected files/projects

```text
src/DMO.Domain/                                   (document-state value objects, if required)
src/DMO.Application/Documents/                    (generation + availability contracts)
src/DMO.Infrastructure/                           (file/pdf adapters; no new project unless approved)
src/DMO.Web/Endpoints/                            (open/regenerate where allowed)
tests/DMO.UnitTests/  tests/DMO.IntegrationTests/
```

## 8. Access requirements

Document access is gated by the **owning workflow/action** permission — no separate document
authorization model.

## 9. Backend / persistence requirements

Owning-record state plus, only where an immutable output is genuinely required, persisted
metadata. No artificial document identity.

## 10. Required tests

See master plan §11 P2-T08: the three availability distinctions; filename is never an identity;
directory/filenames exact; frozen output not silently regenerated; historical rendering after
the source Tool changes; no filesystem path in output; actions enabled only when the operation
can complete.

Additionally (`reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md` §7, §8, §9): the base
directory is resolved from the configured setting and follows a change to it; an inaccessible
directory is distinguishable from a missing file and from an empty result; the naming and
`<reference>/<production-number>/` structure are unchanged; sending selects a configured list and
template, and no code path contains a hardcoded recipient address.

## 11. Acceptance criteria

The `contracts/DOCUMENTS_AND_FILES.md` §8 conditions all hold, plus: the base directory comes
from the operator-configured `Controlo_Create → Definições` setting rather than a hardcoded
constant; sending uses the configured email list and template with no hardcoded recipient
address; `Definições` itself is delivered by P2-T05 and is not duplicated here;
`CurrentBuildAvailable` unchanged.

## 12. Completion evidence

Committed implementation + tests + a rendered/parsed check that no filesystem path appears in
output and that the three availability distinctions hold.

## 13. Downstream dependents

P2-T10.
