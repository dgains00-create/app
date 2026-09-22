# Supabase TEST Baseline Response

## Status

**PARTIAL.** The repository now has one dependency-free local configuration path shared by
`DMO.Web` and `DMO.IntegrationTests`, and all offline regression evidence passes. Live
Supabase verification could not proceed because this checkout contains no `.env`, no matching
process variables and no `dotnet user-secrets` store. No credential or association was
invented, no database was contacted, and no migration was applied.

Implementation commit: `53bd8bf8a440bcba8d8117c92355b09bcebe7a5b`.

Remote `main` at task start: `575a32e96f254c0715ff86836af9be7142e25453`.
The exact post-publication remote tip is reported in the task's final response because a
committed response file cannot contain the SHA of the commit that contains itself.

## 1. Configuration contract

| Consumer | Source and precedence | Required keys |
| --- | --- | --- |
| `DMO.Web` | `LocalEnvironmentFile` loads repository-root `.env` into the process first, without replacing existing process variables. `WebApplication.CreateBuilder` then applies normal .NET providers (`appsettings.json`, environment-specific JSON, Development user-secrets and environment). Process environment therefore remains authoritative. | `Database:ConnectionString` / `Database__ConnectionString`; `Supabase:ProjectUrl` / `Supabase__ProjectUrl`; `Supabase:PublishableKey` / `Supabase__PublishableKey`; `SupabaseAdmin:ServiceRoleKey` / `SupabaseAdmin__ServiceRoleKey` for the registered administration provider boundary. |
| `DMO.Infrastructure` | Receives the host `IConfiguration`; binds `DatabaseOptions`; validates the Npgsql connection string eagerly; registers one `DmoDbContext`. It has no independent environment loader or fallback. | `Database:ConnectionString`. |
| `DMO.IntegrationTests` | Assembly module initializer calls the same `LocalEnvironmentFile` loader before tests run. Routine host tests continue to use the existing process-scoped placeholders from `DmoWebApplicationFactory`. | Live tests use the direct process names below; disposable PostgreSQL tests use only `DMO_TEST_POSTGRES_CONNECTION`. |
| Live Admin Auth | Direct process reads; requires explicit live gate. | `DMO_SUPABASE_LIVE_TEST`, `Supabase__ProjectUrl`, `Supabase__PublishableKey`, `DMO_SUPABASE_ADMIN_EMAIL`, `DMO_SUPABASE_ADMIN_PASSWORD`. |
| Live User Auth | Direct process reads; requires explicit live gate and read access to the application database. | `DMO_SUPABASE_LIVE_TEST`, `Supabase__ProjectUrl`, `Supabase__PublishableKey`, `Database__ConnectionString`, `DMO_SUPABASE_USER_COMPANY_NUMBER`, `DMO_SUPABASE_USER_PASSWORD`. |

There were no duplicate or legacy Supabase variable names in code. `appsettings*.json` carries
no credential values. `launchSettings.json` sets only `ASPNETCORE_ENVIRONMENT=Development`.
The optional deployment command additionally consumes `AdminBootstrap__Email`,
`AdminBootstrap__DisplayName` and `AdminBootstrap__AuthIdentityId`; those are not required for
this baseline unless that command is invoked.

The current official Supabase Auth/password documentation and breaking-change changelog were
checked. The repository's hosted password-grant flow remains applicable; the current
self-hosted `/auth/v1` URL change does not alter this hosted-project integration.

## 2. `.env` variable-name comparison

No `.env*` file exists anywhere under `D:\DMO-MODULAR`. The likely prior checkout paths
`D:\workbench\DMO-MODULAR\.env` and `D:\workbench\.env` were also absent. There is no matching
process environment configuration and no user-secrets file for the Web project's
`UserSecretsId`. Values were never printed.

| Variable | Classification | Reason |
| --- | --- | --- |
| `Supabase__ProjectUrl` | MISSING | Required by application and both live tests. |
| `Supabase__PublishableKey` | MISSING | Required by application and both live tests. |
| `Database__ConnectionString` | MISSING | Required by application and live USER path/read verification. |
| `DMO_SUPABASE_ADMIN_EMAIL` | MISSING | Live Admin test credential only. |
| `DMO_SUPABASE_ADMIN_PASSWORD` | MISSING | Live Admin test credential only. |
| `DMO_SUPABASE_USER_COMPANY_NUMBER` | MISSING | Live User test credential only. |
| `DMO_SUPABASE_USER_PASSWORD` | MISSING | Live User test credential only. |
| `SupabaseAdmin__ServiceRoleKey` | MISSING | Required for full `DMO.Web` startup because the existing Admin user service is registered; not used by login tests. |
| `DMO_SUPABASE_LIVE_TEST` | NOT REQUIRED | Invocation gate; it was supplied explicitly for the targeted run and need not be persisted. |
| `DMO_TEST_POSTGRES_CONNECTION` | NOT REQUIRED | Disposable-database-only gate; deliberately not pointed at Supabase TEST. |

`.gitignore` contains both `*.env` and `.env*`; `git check-ignore -v .env` resolves to that
rule, and `.env` is not tracked.

## 3. Changes made

- Added `LocalEnvironmentFile`, scoped to the repository containing `DMO.slnx`.
- Supports blank/comment lines, `NAME=value`, quoted values and optional `export`; it never
  logs names or values and reports malformed input by line number only.
- Existing process variables are never overwritten, preserving normal .NET override behavior.
- `DMO.Web` loads `.env` before `WebApplication.CreateBuilder`.
- `DMO.IntegrationTests` loads the same file through a module initializer before live tests.
- Added three unit tests and documented the local contract. No third-party package was added.

## 4. Live Admin Auth result

**SKIPPED — configuration blocker.** With `DMO_SUPABASE_LIVE_TEST=1`, the existing Admin test
was discovered and reported the four absent keys listed above. No network request occurred.
This is not recorded as an Auth or credential failure because the adapter never ran.

## 5. Live User Auth result

**SKIPPED — configuration blocker.** With `DMO_SUPABASE_LIVE_TEST=1`, the existing User test
was discovered and reported the five absent keys listed above. Neither the database lookup nor
the Supabase password grant ran.

## 6. Internal-user resolution result

**NOT TESTED.** Code inspection confirms the accepted chain:

`Supabase provider subject -> PersistenceAccountLookup(users.auth_identity_id) -> AccountResolver`

The live User test currently proves carrier lookup plus Supabase Auth only. Because no live
identity/database configuration was available, the returned provider subject could not be
read through `PersistenceAccountLookup`. The exact first missing runtime prerequisite is
`Database__ConnectionString`; the live Auth keys/credentials are also absent.

## 7. Access-template resolution result

**NOT TESTED.** The existing architecture continues from a resolved active `UserAccount` via
its nullable `template_id` to `AccessResolver`, `TemplateRepository`,
`TemplateModuleRepository` and the canonical Module Registry. No live result can be claimed
until the identity step above runs. No missing association in Supabase TEST can be asserted
without connecting to it.

## 8. Database connectivity result

**FAIL (not configured).** No `Database__ConnectionString` was available, so even a read-only
connectivity query could not be attempted. The destructive `DatabaseConnectivityTests` case
was not repointed: it drops and recreates schema `public` and is explicitly disposable-only.

Safe write/read smoke test: **NOT RUN.** There is no proven isolated shared-database test in the
current suite, so no write was attempted.

## 9. Migration/schema-state result

Repository migrations:

1. `20260922001736_AccountAndTemplateFoundation` — creates `admin_accounts`, `templates` and
   `users`, their constraints and indexes.
2. `20260922001757_TemplateModuleComposition` — creates `template_modules`, its FK and unique
   presentation-order index.

Supabase TEST state: **UNKNOWN.** Without database configuration, neither
`__EFMigrationsHistory` nor `information_schema` could be read. The `Up` paths were inspected:
they create the accepted foundation objects; no migration was applied. If either migration is
pending, this task requires a separate proven live inspection before applying it.

Migrations applied: **NONE**.

## 10. Classification of the 69 PostgreSQL-gated tests

| Class | Cases | Classification | Evidence |
| --- | ---: | --- | --- |
| `DatabaseConnectivityTests` | 1 | DISPOSABLE DATABASE ONLY | Drops and recreates `public`. |
| `Migration001AccountAndTemplateFoundationTests` | 21 | DISPOSABLE DATABASE ONLY | Applies migrations and mutates foundation tables/constraints. |
| `Migration002TemplateModuleCompositionTests` | 4 | DISPOSABLE DATABASE ONLY | Applies migrations and mutates composition data. |
| `AdminSingletonInvariantTests` | 1 | DISPOSABLE DATABASE ONLY | Applies migrations and mutates singleton data. |
| `AdminBootstrapIntegrationTests` | 2 | DISPOSABLE DATABASE ONLY | Applies migrations and mutates/cleans Admin data. |
| `PersistenceAccountLookupIntegrationTests` | 1 | DISPOSABLE DATABASE ONLY | Applies migrations and mutates/cleans account data. |
| `UserRepositoryIntegrationTests` | 3 | DISPOSABLE DATABASE ONLY | Applies migrations and performs repository writes. |
| `TemplateRepositoryIntegrationTests` | 2 | DISPOSABLE DATABASE ONLY | Applies migrations and performs repository writes. |
| `ModuleAccessResolverIntegrationTests` | 5 | DISPOSABLE DATABASE ONLY | Applies migrations and mutates/cleans Template/User composition. |
| `UserLandingPersistenceIntegrationTests` | 7 | DISPOSABLE DATABASE ONLY | Applies migrations and mutates/cleans Template/User composition. |
| `UserAdministrationPersistenceIntegrationTests` | 8 | DISPOSABLE DATABASE ONLY | Applies migrations and performs administration writes/cleanup. |
| `TemplateAdministrationPersistenceIntegrationTests` | 14 | DISPOSABLE DATABASE ONLY | Applies migrations and performs broad Template/User writes/cleanup. |

Totals:

- SAFE AGAINST SHARED SUPABASE TEST: **0**
- DISPOSABLE DATABASE ONLY: **69**
- UNCLEAR / REQUIRES REVIEW: **0**

Even tests with targeted cleanup still invoke `Database.MigrateAsync()` and were authored for
one serialized disposable database. None was run against Supabase TEST.

## 11. Build/test regression result

| Verification | Result |
| --- | --- |
| `dotnet build DMO.slnx -c Debug` | PASS — 0 errors, 1 pre-existing `xUnit2029` warning in `P2T02RegressionTests.cs`. |
| Full unit suite | PASS — 413 pass, 0 fail, 0 skip. Baseline increase from 410 is exactly the three new loader tests. |
| Full integration normal run | PASS — 152 pass, 0 fail, 71 skip, total 223. Both gates were explicitly absent for this run. |
| Live Supabase targeted (`DMO_SUPABASE_LIVE_TEST=1`) | 0 pass, 0 fail, 2 skip; each skip named the absent configuration contract. |
| P2-T02 regression filter | PASS — 5 pass, 0 fail, 0 skip. No P2-T02 source/contract was modified. |

## 12. Remaining blockers

1. Place a local, ignored `D:\DMO-MODULAR\.env` containing the seven required live keys (and
   `SupabaseAdmin__ServiceRoleKey` only when full Web startup is required). Do not commit it.
2. Re-run Admin and User live Auth with `DMO_SUPABASE_LIVE_TEST=1`.
3. Only after Auth succeeds, run read-only provider-subject -> internal USER -> Template ->
   effective-access verification.
4. Run a read-only PostgreSQL connectivity/schema-ledger query; do not use
   `DMO_TEST_POSTGRES_CONNECTION` or the 69 disposable cases against Supabase TEST.
5. Decide whether any proven pending accepted migration should be applied only after the live
   schema report exists. No speculative migration is warranted.

## 13-16. Git/publication state

- Exact implementation commit SHA: `53bd8bf8a440bcba8d8117c92355b09bcebe7a5b`.
- Remote `main` before publication: `575a32e96f254c0715ff86836af9be7142e25453`.
- Implementation reachable from the pre-publication remote `main`: **NO** (local commit at the
  time of this report). Post-publication reachability and exact final remote tip are confirmed
  in the final task response.
- Working tree at implementation commit: **CLEAN** before creation of this response file.

## Required final-format summary

Supabase TEST baseline status: **PARTIAL**

Configuration contract: **PASS**

.env loaded by application: **YES when present; currently absent**

.env loaded by integration tests: **YES when present; currently absent**

Configuration names aligned: **YES in code; local values are missing**

Live Supabase Admin Auth: **SKIPPED**

Live Supabase User Auth: **SKIPPED**

Internal user resolution: **NOT TESTED**

Access template resolution: **NOT TESTED**

Database connectivity: **FAIL — configuration absent**

Safe write/read smoke test: **NOT RUN**

Supabase TEST schema state: **UNKNOWN**

Migrations applied: **NONE**

69 PostgreSQL-gated tests:

- SAFE AGAINST SUPABASE TEST: **0**
- DISPOSABLE ONLY: **69**
- UNCLEAR: **0**

Build: **PASS**

Unit: **413 pass / 0 fail / 0 skip**

Integration normal run: **152 pass / 0 fail / 71 skip**

Live Supabase targeted: **0 pass / 0 fail / 2 skip**

P2-T02 regression: **PASS**

Secrets exposed: **NO**

.env committed: **NO**

P2-T03 started: **NO**

Implementation commit: `53bd8bf8a440bcba8d8117c92355b09bcebe7a5b`

Remote main before publication: `575a32e96f254c0715ff86836af9be7142e25453`

Implementation reachable from pre-publication remote main: **NO**

Working tree at implementation commit: **CLEAN**
