# DMO.Infrastructure

Shared infrastructure plumbing: PostgreSQL connection/configuration, the persistence
context and the migration-runner implementation.

No Phase 1 domain schema and no industrial business rules live here.

## P1-T01 contents

| Concern | Type |
| --- | --- |
| Connection configuration | `Database/DatabaseOptions.cs` |
| Connection resolution + validation | `Database/DatabaseConnectionResolver.cs` |
| Configuration failure | `Database/DatabaseConfigurationException.cs` |
| Persistence context (single, empty) | `Persistence/DmoDbContext.cs` |
| Migration runner implementation | `Persistence/EfCoreMigrationRunner.cs` |
| DI registration | `InfrastructureServiceCollectionExtensions.cs` |

## Deliberately absent

- no Phase 1 product tables;
- no entity classes for USER, ADMIN, Template, Module, Tool or any industrial concept;
- no raw permission/capability tables;
- no second database context;
- no generic repository abstraction;
- no connection string, credential, host, user or password default.

Configuration is supplied by the environment (see `src/DMO.Web/README.md`). A missing or
invalid connection string fails startup; it is never replaced by a production-looking default.
