# DMO.Web

Application host only: startup/composition root, HTTP pipeline, configuration binding,
dependency registration, the technical health/startup surface, and the runtime
authentication/account boundary.

It owns no industrial business rule.

## Contents

| Concern | File |
| --- | --- |
| Composition root | `Program.cs` |
| Technical endpoints | `Endpoints/TechnicalEndpoints.cs` |
| Authentication + current-account endpoints | `Endpoints/AuthEndpoints.cs` |
| Technical startup commands (migrate / run) | `Startup/StartupCommands.cs` |
| Supabase Auth configuration (DEV/TEST, ADMIN path) | `Auth/SupabaseOptions.cs` |
| Production authentication boundary | `Auth/SupabaseAuthenticationService.cs` |
| Runtime session element (cookie scheme) | `Auth/SessionAuthentication.cs` |
| Real current-account context | `Auth/CurrentAccountContext.cs` |
| Production P1-T02 account lookup (fail closed) | `Resolution/UnavailableAccountLookup.cs` |

## Technical endpoint

```text
GET /health  ->  {"status":"ok","environment":"<host environment>"}
```

Startup/liveness only. It reads no product data and does not touch the database.

## Authentication + current-account surface (P1-T02)

```text
POST /auth/login    ADMIN: {"email", "password"}      -> 200 (session) | 401 | 502 | 503 | 403 (fail closed)
                    USER:  {"companyNumber", "password"} -> 503 in P1-T02 (no USER provider flow yet)
POST /auth/logout   clears only the runtime session state
GET  /auth/me       {"accountType": "admin"|"user"|"none", ...} — read-only, never grants access
```

- ADMIN authentication is **real Supabase Auth** against the DEV/TEST project
  (`jixnteypqqrltsxgwzpv`). The **publishable key** travels only in the `apikey` header;
  no `service_role` and no secret key is used. This project is DEV/TEST only — the future
  production backend is a separate clean Supabase project.
- A session is established only when authentication succeeds **and** account resolution
  returns the active ADMIN/USER. In P1-T02 production no account can resolve
  (`UnavailableAccountLookup` until P1-T03), so `POST /auth/login` always fails closed and
  no session is ever created.
- USER login contract stays `company_number + password`. No email mapping is invented; the
  durable mapping is P1-T03 persistence.
- Provider identity is internal linkage only. Provider claims/roles never classify access.

## Configuration

| Key | Environment variable | Purpose |
| --- | --- | --- |
| `Database:ConnectionString` | `Database__ConnectionString` | PostgreSQL connection string |
| `Supabase:ProjectUrl` | `Supabase__ProjectUrl` | DEV/TEST Supabase project URL |
| `Supabase:PublishableKey` | `Supabase__PublishableKey` | Supabase publishable key (ADMIN Auth) |

There are **no defaults** and no committed credentials. Missing or invalid values fail
startup loudly (`DatabaseConfigurationException` / `SupabaseConfigurationException`); the
host never substitutes a production-looking default and never degrades to an
unauthenticated mode.

Local development uses user-secrets (already initialised for this project):

```pwsh
dotnet user-secrets set "Database:ConnectionString" "<postgres connection string>" --project src/DMO.Web
dotnet user-secrets set "Supabase:ProjectUrl" "https://jixnteypqqrltsxgwzpv.supabase.co" --project src/DMO.Web
dotnet user-secrets set "Supabase:PublishableKey" "<publishable key>" --project src/DMO.Web
```

The ignored `*.env` file is **never read** by the application; no `.env` loader is added.
It is a developer-side scratch file whose values are copied into user-secrets or exported
to environment variables.

## Running

```pwsh
dotnet restore
dotnet build
dotnet run --project src/DMO.Web                            # start the host
dotnet run --project src/DMO.Web -- migrate                 # apply pending migrations and exit
```

At P1-T02 the migration set is intentionally empty: `migrate` reports that nothing was
applied and leaves the schema untouched.

## Cross-cutting boundaries

Run as a host process only. P1-T02 adds the authentication/account boundary (ADMIN Supabase
Auth DEV/TEST, provider-neutral account resolution, runtime session, read-only current
account). Module Registry, navigation composition, Template/Module access resolution and the
USER provider mapping arrive in later slices (P1-T03 onward); none is implemented here.