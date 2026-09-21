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

## P1-T02 — authentication + account boundary

P1-T02 introduces the authentication/account boundary only. It stops before
Template/Module/access resolution.

```text
src/DMO.Application/Authentication   contracts: IAuthenticationBoundary, typed AdminLoginRequest
                                       (email + password) and UserLoginRequest
                                       (company_number + password), AuthenticatedIdentity
                                        (ProviderSubject + AuthenticationPath only)
src/DMO.Application/Accounts         IAccountResolver + IAccountLookup split; real
                                       AccountResolver mapping semantics; ADMIN/USER
                                       account records; NoAccess states
src/DMO.Application/Session          read-only ICurrentAccountContext + CurrentAccount

src/DMO.Web/Auth                     runtime: SupabaseAuthenticationService (ADMIN path:
                                       real Supabase Auth DEV/TEST, publishable key only),
                                       SessionAuthentication (cookie scheme session element),
                                       CurrentAccountContext (real ICurrentAccountContext)
src/DMO.Web/Resolution               UnavailableAccountLookup — production P1-T02 lookup:
                                       no persisted mapping, always fails closed (P1-T03
                                       replaces it, not resolver semantics)
src/DMO.Web/Endpoints/AuthEndpoints  POST /auth/login, POST /auth/logout, GET /auth/me
```

Key decisions recorded here:

- ADMIN authentication is **real** Supabase Auth against the DEV/TEST project
  (`jixnteypqqrltsxgwzpv`, `eu-west-1`) using only the project URL and the **publishable
  key** (`apikey` header; publishable keys are never sent as Bearer tokens). No
  `service_role`, no secret key, no old schema/migration reuse. This project is DEV/TEST
  only; the future production backend is a separate clean Supabase project.
- USER authentication has **no provider flow in P1-T02**: the durable
  `company_number → provider identity` mapping requires P1-T03 persistence. The USER login
  contract stays `company_number + password`; email is never a USER login identifier.
- Authentication is separated from account resolution. In P1-T02 production a session is
  never established because resolution always fails closed (`UnavailableAccountLookup`).
- Provider identity is internal linkage only; provider claims/roles never classify access.
- Local configuration uses `dotnet user-secrets` (already initialised); the ignored
  `*.env` file is never read by the application — no `.env` loader is added. Environment
  variables (`Supabase__ProjectUrl`, `Supabase__PublishableKey`, `Database__ConnectionString`)
  are the deployed/test-host form.

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
