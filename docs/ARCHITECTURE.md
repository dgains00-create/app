# DMO Modular — Architecture

How the repository is physically organised, and where each future boundary will live.

This file describes the **code structure**. Product/functional authority is
`diogo-o/dmo-master`; this file does not restate domain behaviour.

## Current structure (P1-T01)

```text
DMO.slnx

Directory.Build.props          shared build settings
Directory.Packages.props       central package versions

src/
  DMO.Web/                     application host
  DMO.Application/             application contracts/orchestration
  DMO.Domain/                  domain primitives (currently empty by design)
  DMO.Infrastructure/          shared infrastructure plumbing

tests/
  DMO.UnitTests/               unit test project
  DMO.IntegrationTests/        integration test project

docs/                          design and structure documentation
```

## Project responsibilities

### `src/DMO.Web` — host only

Startup/composition root, HTTP pipeline, configuration binding, dependency registration and
the minimal technical health/startup surface.

Owns **no** industrial business rule. Session/authentication wiring, the Module Registry and
navigation composition are *runtime* concerns that will be added here in later Phase 1
slices — none is pre-built in P1-T01.

### `src/DMO.Application` — application contracts

Contracts the runtime needs, kept free of infrastructure detail. P1-T01 materialises only
the migration-runner boundary (`Migrations/IMigrationRunner.cs`), because the host needs a
migration entry point without knowing how migrations are stored or executed.

Users, Templates, account resolution and authentication workflows arrive in later slices.

### `src/DMO.Domain` — domain primitives

Intentionally almost empty. P1-T01 forbids pre-modelling USER, ADMIN, Template, Module,
Tool, Job On, Controlo or any industrial entity merely to prepare for later tasks.

Types are added here only when a concrete task genuinely requires them.

### `src/DMO.Infrastructure` — shared plumbing

PostgreSQL connection/configuration, the single persistence context and the migration
mechanism. Contains no Phase 1 domain schema and no business rule.

## Reference direction

```text
DMO.Web            -> DMO.Infrastructure, DMO.Application, DMO.Domain
DMO.Infrastructure -> DMO.Application, DMO.Domain
DMO.Application    -> DMO.Domain
DMO.Domain         -> (nothing)
```

Dependencies point inward. The host may reference everything; nothing references the host.

Deliberately **not** used: microservices, message buses, CQRS infrastructure, mediator
frameworks, generic repository abstractions, event sourcing, plugin loaders, runtime
reflection-based discovery, multiple database contexts, distributed caching.

## Where future boundaries will live

Future functional/domain boundaries will be introduced by the task that owns them.

They should normally be represented inside the current four-project solution — as
namespaces, folders, services and persistence configuration within `DMO.Web`,
`DMO.Application`, `DMO.Domain` or `DMO.Infrastructure` — unless a concrete,
Architect-approved need justifies a new project/assembly.

**No future project split is pre-authorised by P1-T01.**

The previous README-only skeleton used `App/`, `Modules/`, `Infrastructure/` and `Shared/` as
documentation folders. Those names describe conceptual areas. They do **not** fix an assembly
layout: a future Admin, Boquilhas, Controlo, Tools, Files, Pdf or Contracts boundary is not
thereby committed to becoming its own .NET project.

Authentication and the Module Registry are **runtime** concerns and belong inside
`src/DMO.Web`, consistent with the runtime-owns-runtime boundary.

## Terminology

Preserved from the Master and the accepted plan:

```text
Module = a product-level assignable access unit
Phase  = a development/construction stage
```

A .NET project is a project. It is **not** a Module. Development slices are phases/tasks.
