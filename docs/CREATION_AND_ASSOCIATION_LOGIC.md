# DMO Modular — Creation and Association Logic

This file records the agreed creation logic for the reduced modular version of DMO so that the first modules can work now without losing the structure needed for the future full application.

## Objective

Build the first operational version around:

1. Admin / Users / Templates
2. Boquilhas
3. Controlo: Peso Criar, Peso Aprovar, Pegamentos and Resumo

The reduced version must already create and reuse the real central identities that will remain valid when Armazem and the full Job On module are added later.

The goal is not to simulate the future application. The goal is to create a smaller but structurally compatible slice of the real backend.

## Core principle

Central identities exist before every module that will eventually use them.

A module may create or enrich one of those identities when that is necessary for its current workflow. Later modules add more capabilities and relationships around the same identity.

    tool_id
    -> may exist before Armazem

    jobon_id
    -> may exist before the full Job On module

    cm_id / mf_id / bq_id
    -> may exist before the full Job On UI/workflow

The future modules do not replace those IDs. They enrich them.

## Tool identity

tool_id is the canonical identity of any Tool. It is generic and is not owned exclusively by Boquilhas, Controlo, Armazem or Job On.

Examples:

    tool_id T100
    -> type = BQ

    tool_id T200
    -> type = CM

    tool_id T300
    -> type = MF

### Temporary Tool creation page

Until the complete Ferramentas / Armazem workflow exists, the application may expose a small temporary Tool creation page.

Its purpose is only to create the real canonical Tool identity and the minimum Tool data required by Boquilhas and Controlo.

It must not pretend to be the final Armazem or Ferramentas module.

Boquilhas and Controlo then select and reuse those real tool_id values.

Later:

    Armazem
    -> adds positions, movements and physical logistics around the same tool_id

The Tool must not be recreated when Armazem is implemented.

## Minimal Job On identity

The reduced version may create a real jobon_id before the full Job On module exists.

The initial jobon_id contains only the real production identity needed now.

Minimum example:

    jobon_id J001
    +- production_number = 202601
    +- reference = 5447T173
    +- production_date = real production date

This is a real Job On identity with a reduced set of fields. It is not a fake placeholder.

The full Job On module is added later and enriches the same jobon_id with the remaining Job On data and workflow.

## Production occurrence contexts

A Tool identity alone is not enough to distinguish the same Tool being used in different productions.

Therefore the reduced version already creates the occurrence identities:

    jobon_id J001
    +- cm_id C001
    |  +- tool_id T200
    +- mf_id M001
    |  +- tool_id T300
    +- bq_id B001
       +- tool_id T100

Meaning:

- tool_id = which Tool it is;
- jobon_id = which production occurrence it belongs to;
- cm_id, mf_id and bq_id = that Tool in that specific Job On context.

This prevents records from different productions being mixed together.

## Boquilhas creation flow

When Boquilhas registers work for a production:

1. Select or create the real BQ tool_id.
2. Enter or select the production identity.
3. Search for an existing jobon_id for that exact production.
4. If it exists, reuse it.
5. If it does not exist, create the minimal jobon_id.
6. Find or create the bq_id linking jobon_id + tool_id.
7. Persist the Boquilhas record against that bq_id.

Example:

    jobon_id J001
    +- reference = 5447T173
    +- production_number = 202601
    +- bq_id B001
       +- tool_id T100

Boquilhas must not create a second Job On for the same real production merely because another module has not yet used it.

## Controlo / Peso creation flow

When Peso is created for the same production:

1. Select or create the real CM tool_id.
2. Enter or select the same production identity.
3. Search for the existing jobon_id.
4. Reuse the existing jobon_id if it is the same production.
5. Find or create the cm_id linking jobon_id + CM tool_id.
6. Create peso_id against that cm_id.

Example:

    jobon_id J001
    +- reference = 5447T173
    +- production_number = 202601
    +- bq_id B001
    |  +- tool_id T100
    +- cm_id C001
       +- tool_id T200

    peso_id P001
    +- cm_id C001

Peso must not create a new Job On if Boquilhas already created the correct one for that production.

## Same production, shared identity

Boquilhas and Controlo contribute data to the same production context.

Example:

    5447T173 / 202601

    jobon_id J001
    +- BQ
    |  +- bq_id B001
    |     +- tool_id T100
    +- CM
    |  +- cm_id C001
    |     +- tool_id T200
    +- Peso
    |  +- peso_id P001 -> cm_id C001
    +- Pegamentos
    +- Resumo

The modules remain separate, but their data is connected through the same backend identities.

## Job On uniqueness rule

The application must have a clear rule for deciding whether a Job On already exists.

For the reduced version, the expected business key is currently:

    reference + production_number

If the real process allows more than one distinct Job On with the same combination, the additional real distinguishing field must be included.

The system must not decide that two Job Ons are the same merely because they look similar.

## PDF directory logic

The same Job On identity provides the production information needed to resolve the document directory.

Planned structure:

    <reference>/
    +- <production-number>/
       +- Peso_<reference>_<machine>.pdf
       +- Pegamentos_<reference>_<machine>.pdf
       +- Resume_<reference>_<machine>.pdf

Example:

    5447T173/
    +- 202601/
       +- Peso_5447T173_B3.pdf
       +- Pegamentos_5447T173_B3.pdf
       +- Resume_5447T173_B3.pdf

The directory is an output location. It does not replace database identity or database history.

## Future Job On module

When the full Job On module is added, it must reuse the existing jobon_id.

It expands the minimal record into the complete Job On record and workflow, adding only the fields and capabilities that are actually required.

The existing cm_id, mf_id, bq_id, tool_id, peso_id, Boquilhas records and documents remain connected to the same identities.

No replacement Job On should be created simply because the full module now exists.

## Future Armazem module

Armazem also reuses the existing tool_id.

It adds the physical and logistical lifecycle around that Tool, for example:

    tool_id
    +- position
    +- movements
    +- entries
    +- exits
    +- destination
    +- location history

It must not create a second Tool identity for Tools that already exist because of Boquilhas or Controlo.

## Architectural result

The intended growth model is:

    NOW

    Admin
    + Tools identity
    + minimal Job On identity
    + cm_id / mf_id / bq_id
    + Boquilhas
    + Controlo

Then:

    LATER

    same tool_id
    -> enriched by Armazem

    same jobon_id
    -> enriched by full Job On module

    same cm_id / mf_id / bq_id
    -> continue to identify Tool occurrence in that production

The backend grows by adding capabilities and relations around stable identities. It should not require a full rewrite when later modules are introduced.

## MUST NOT

Do not:

- create a new Job On in each module for the same production;
- use only tool_id for production-specific Peso or BQ records when production occurrence matters;
- replace existing tool_id values when Armazem is introduced;
- replace existing jobon_id values when the full Job On module is introduced;
- invent future Job On workflow fields before they are needed;
- make Boquilhas depend on the full Job On UI;
- make Controlo depend on the full Job On UI;
- mix records from different productions just because they use the same Tool;
- use the filesystem directory as the source of truth instead of persisted records.

## Current implementation order

    Phase 1
    Admin / Login / Users / Templates

    Phase 2
    Boquilhas
    + Tool creation/selection
    + minimal Job On creation/reuse
    + bq_id creation/reuse
    + persistent records

    Phase 3
    Controlo
    + Tool creation/selection for CM/MF where required
    + existing Job On lookup/reuse
    + cm_id / mf_id creation/reuse
    + Peso Criar
    + Peso Aprovar
    + Pegamentos
    + Resumo
    + PDFs

After these phases are stable, later modules can be added around the same identities instead of forcing a backend migration.
