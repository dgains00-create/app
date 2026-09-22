# P2-T08 — Documents / PDF / Directory Convention / Availability — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T08), §8, §9, §10, §11, §12.
Class: **Cross-cutting feature**.
Depends on: **P2-T05** (Peso), **P2-T06** (approved/decision state), **P2-T07** (Boquilhas where
a file state applies).
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

## 5. Authority blocker B4 — required contract before execution

The authored, reviewed contract must fix: the generation capability location (inside the
existing projects unless an Architect-approved need justifies otherwise), the ownership/gating
of open/regenerate actions, the exact state transitions that produce each availability value,
and the endpoint names with their owning workflow permission.

## 6. Explicit non-scope

- Inventing new Job On document identities (the three official Job On outputs are owned by the
  global Job On contract and are not required by Beta Job On Light).
- A mandatory Pegamentos file.
- Introducing a document table/identity just for symmetry.
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

## 11. Acceptance criteria

The `contracts/DOCUMENTS_AND_FILES.md` §8 conditions all hold.

## 12. Completion evidence

Committed implementation + tests + a rendered/parsed check that no filesystem path appears in
output and that the three availability distinctions hold.

## 13. Downstream dependents

P2-T10.
