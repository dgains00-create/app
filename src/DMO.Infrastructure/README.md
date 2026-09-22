# DMO.Infrastructure

Shared infrastructure plumbing: PostgreSQL connection/configuration, the persistence context
and the migration-runner implementation.

No industrial business rules live here.

## Contents

| Concern | Type |
| --- | --- |
| Connection configuration | `Database/DatabaseOptions.cs` |
| Connection resolution + validation | `Database/DatabaseConnectionResolver.cs` |
| Configuration failure | `Database/DatabaseConfigurationException.cs` |
| Persistence context | `Persistence/DmoDbContext.cs` |
| Migration runner implementation | `Persistence/EfCoreMigrationRunner.cs` |
| Product migrations | `Migrations/20260922001736_*` (account/template foundation), `Migrations/20260922001757_*` (template module composition) |
| Foundation entities + configurations | `Persistence/` (accounts, templates, template modules) |
| Repository primitives | `Persistence/` (users, templates, accounts) |
| DI registration | `InfrastructureServiceCollectionExtensions.cs` |

## Boundaries

- one database context only; no second context and no generic repository abstraction;
- migrations 001/002 are frozen; later schema changes arrive as **new** migration files;
- no connection string, credential, host, user or password default.

Configuration is supplied by the environment (see `src/DMO.Web/README.md`). A missing or
invalid connection string fails startup; it is never replaced by a production-looking default.
