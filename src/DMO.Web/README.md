# DMO.Web

Application host only: startup/composition root, HTTP pipeline, configuration binding,
dependency registration and the minimal technical health/startup surface.

It owns no industrial business rule.

## Contents

| Concern | File |
| --- | --- |
| Composition root | `Program.cs` |
| Technical endpoints | `Endpoints/TechnicalEndpoints.cs` |
| Technical startup commands (migrate / run) | `Startup/StartupCommands.cs` |

## Technical endpoint

```text
GET /health  ->  {"status":"ok","environment":"<host environment>"}
```

Startup/liveness only. It reads no product data and does not touch the database.

## Configuration

| Key | Environment variable | Purpose |
| --- | --- | --- |
| `Database:ConnectionString` | `Database__ConnectionString` | PostgreSQL connection string |

There is **no default** and no committed credential. A missing or invalid value fails
startup loudly (`DatabaseConfigurationException`); the host never substitutes a
production-looking default.

Local development uses user-secrets (already initialised for this project):

```pwsh
dotnet user-secrets set "Database:ConnectionString" "<postgres connection string>" --project src/DMO.Web
```

## Running

```pwsh
dotnet restore
dotnet build
dotnet run --project src/DMO.Web                            # start the host
dotnet run --project src/DMO.Web -- migrate                 # apply pending migrations and exit
```

At P1-T01 the migration set is intentionally empty: `migrate` reports that nothing was
applied and leaves the schema untouched.

## Cross-cutting boundaries

Run as a host process only. Session/authentication wiring, the Module Registry, navigation
composition and account resolution arrive in later Phase 1 slices (P1-T02 onward); none is
implemented or stubbed here beyond the folder boundaries documented at the repository root.
