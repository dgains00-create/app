# Skeleton rationalization — P1-T01

This file records how the previous README-only folder skeleton relates to the real project
tree introduced by P1-T01, so no reader has to guess which structure is current.

Added by P1-T01. The Architect reviews the moves recorded here.

## Why anything moved at all

The previous tree (`App/`, `Modules/`, `Infrastructure/`, `Shared/`) was **documentation
boundaries only** — thirteen `README.md` files and no compilable code. P1-T01 introduces a
real .NET solution, which needs actual project directories.

Keeping both trees side by side would create two competing descriptions of the same
architecture. The conceptual boundaries of the old tree are preserved, but each one now
maps onto a real project.

## Mapping

| Previous location | New location | Status of the old folder |
| --- | --- | --- |
| `App/Runtime/` | `src/DMO.Web/` | removed — superseded by the real host project |
| `App/Auth/` | not yet created as a project | removed — see below |
| `App/ModuleRegistry/` | not yet created as a project | removed — see below |
| `Infrastructure/Database/` | `src/DMO.Infrastructure/` | removed — superseded by the real project |
| `Infrastructure/Files/` | not yet created | removed — see below |
| `Infrastructure/Pdf/` | not yet created | removed — see below |
| `Modules/Admin/` | not yet created | removed — see below |
| `Modules/Boquilhas/` | not yet created | removed — see below |
| `Modules/Controlo/` | not yet created | removed — see below |
| `Modules/Tools/` | not yet created | removed — see below |
| `Shared/Common/` | not yet created | removed — see below |
| `Shared/Contracts/` | not yet created | removed — see below |

The old tree's intent is preserved in `docs/ARCHITECTURE.md`, which carries forward the
forward-looking structure (including the `Modules/` and `Shared/` areas) as the documented
target for later phases.

## Why the unimplemented boundaries did not become projects

The request is explicit:

> do not multiply projects without a concrete need
> An almost-empty Domain project is acceptable.

Creating empty `DMO.Modules.Admin`, `DMO.Modules.Boquilhas`, `DMO.Shared.Contracts`,
`DMO.Infrastructure.Files` and `DMO.Infrastructure.Pdf` projects in P1-T01 would mean
creating project boundaries for behaviour that does not exist yet, and would pre-empt
decisions that belong to the tasks which own that behaviour (P1-T05 onward, Phase 2, Phase 3).

Each becomes a real project in the task that first has real code for it. The boundary
intent is not lost: it is recorded in `docs/ARCHITECTURE.md`.

## What was deliberately preserved

- `README.md` — retained, but its stale construction status was corrected (see below).
- `docs/CREATION_AND_ASSOCIATION_LOGIC.md` — retained **unchanged**. It is current
  forward-looking design material for Phases 2/3 and contains no Phase 1 implementation
  instruction that P1-T01 contradicts.

## Documentation change made to the root README

The previous root `README.md` stated:

> The existing modules should be adapted to richer backend relationships when those
> relationships actually exist. Future compatibility is desirable, but must not be forced
> by inventing Job On or Armazém behavior early.

and, in its "Shared backend identities" section:

> A Job On reference/production may be recorded as identifying information where useful, but
> the application must not invent `jobon_id`, `cm_id`, `bq_id` or other relationships merely
> to imitate the future architecture.

The accepted Master (`global/MODULAR_IMPLEMENTATION_MODEL.md` §3–§5) and the accepted
Phase 1 plan (§A3) settle the opposite: early phases **may** create and reuse the real
canonical identities (`tool_id`, minimal `jobon_id`, `cm_id`/`mf_id`/`bq_id`) when a real
current workflow needs them, and later phases enrich those identities rather than replacing
them.

That conflict does not block P1-T01 — Phase 1 creates no industrial schema at all — and
`docs/CREATION_AND_ASSOCIATION_LOGIC.md` already states the current Master position
correctly. So the stale paragraphs were corrected in place rather than left contradicting
the current Master, and the current status section was updated to reflect that the
application skeleton now exists.

This is a documentation alignment only. **No product rule was changed or invented.**
