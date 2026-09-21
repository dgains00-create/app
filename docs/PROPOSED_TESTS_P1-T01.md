# Proposed tests — P1-T01

> ## TASK-SPECIFIC TESTS NOT EXECUTED — AWAITING ARCHITECT REVIEW

These test files were **created but never executed**. Per §9 of the P1-T01 request, the
Architect reviews each proposed test first to verify that it tests the intended contract
rather than merely the implementation chosen.

The test projects were **compiled** (`dotnet build`) so that they are known to be
mechanically valid. Compilation is not execution: no test method was run, and no test result
exists.

The Architect will decide for each test:

- ACCEPTED FOR EXECUTION;
- NEEDS CORRECTION;
- REJECTED.

Nothing below constitutes acceptance evidence.

---

## Scope note

P1-T01 builds infrastructure only. The request states that the technical checks in §8
("build/run baseline only") are **not** product acceptance tests, and that test projects
should exist "ready for Architect-approved tests" without being filled with speculative
product tests.

The proposed tests below therefore verify only the mechanical contracts that P1-T01 actually
creates. They deliberately do **not** test login, accounts, Templates, Modules, permissions
or access resolution — none of which exists yet.

---

## 1. Database configuration fails loudly when absent

```text
Test:             DatabaseConnectionResolverTests.GetConnectionString_WhenUnset_Throws
                  (and the Blank / NotParseable / HostMissing / DatabaseMissing variants)
Purpose:          Verify the infrastructure refuses to operate without real database
                  configuration instead of substituting a default.
Master/plan
behaviour:        P1-T01 request §5: "The application should fail clearly when required
                  database configuration is absent or invalid; do not silently substitute a
                  production-looking default."
Preconditions:    A DatabaseOptions instance with no connection string (or a deliberately
                  invalid one).
Action:           Call DatabaseConnectionResolver.GetConnectionString().
Assertions:       A DatabaseConfigurationException is thrown; the message names the
                  configuration key 'Database:ConnectionString'.
Required
non-effects:      No usable connection string is returned; no default host/database/credential
                  is invented.
What this proves: The configuration boundary rejects missing/invalid input loudly.
What this does
NOT prove:        That any real environment's connection string is correct; that Npgsql can
                  reach any database; that the app is safe to deploy.
File/path:        tests/DMO.UnitTests/Database/DatabaseConnectionResolverTests.cs
```

```text
Test:             DatabaseConnectionResolverTests.GetConnectionString_WhenValid_ReturnsValue...
Purpose:          Negative control for the above: a valid value is accepted unchanged, so the
                  guard is not simply rejecting everything.
Master/plan
behaviour:        Same §5 requirement (configuration is honoured when present).
Preconditions:    A syntactically valid PostgreSQL connection string.
Action:           Call GetConnectionString() and re-parse the result.
Assertions:       No exception; Host and Database match the configured values.
Required
non-effects:      The resolver does not substitute a different host or database.
What this proves: The guard is specific to missing/invalid configuration.
What this does
NOT prove:        Any real database connectivity.
File/path:        tests/DMO.UnitTests/Database/DatabaseConnectionResolverTests.cs
```

## 2. The migration command is selected only by its own argument

```text
Test:             StartupCommandsTests (IsMigrationCommand_* family)
Purpose:          Verify that starting the host does not accidentally run migrations, and
                  that the migration entry point is reachable.
Master/plan
behaviour:        P1-T01 request §3/§4: runnable web host plus a migration mechanism/runner
                  boundary as distinct technical entry points.
Preconditions:    Various argument arrays.
Action:           Call StartupCommands.IsMigrationCommand(args).
Assertions:       'migrate' / '--migrate' (any casing) as the FIRST argument -> true;
                  no arguments, a later-position 'migrate', or ordinary host switches
                  (--urls, --environment) -> false.
Required
non-effects:      An ordinary host startup never selects the migration path.
What this proves: The two entry points are separated by an explicit, tested rule.
What this does
NOT prove:        That a migration run succeeds against a database; that Program.cs is
                  correctly wired (that is covered by the integration tests below).
File/path:        tests/DMO.IntegrationTests/StartupCommandsTests.cs
```

## 3. The technical startup endpoint answers and leaks nothing

```text
Test:             TechnicalEndpointTests.Health_ReturnsOkWithStatusPayload
Purpose:          Verify the runnable host exposes a minimal technical startup surface.
Master/plan
behaviour:        P1-T01 request §3: "provides a minimal technical health/startup endpoint or
                  equivalent"; §4: DMO.Web is the host and owns no industrial business rule.
Preconditions:    The host started in-process via WebApplicationFactory, with a placeholder
                  (non-connecting) connection string so eager configuration validation passes.
Action:           GET /health.
Assertions:       HTTP 200; body contains status = "ok"; body contains no 'user', 'modules'
                  or 'templates' property.
Required
non-effects:      The endpoint exposes no account, Template, Module or industrial data.
What this proves: The host starts and serves a minimal technical surface only.
What this does
NOT prove:        Database connectivity; any Phase 1 business behaviour; production
                  readiness; that the endpoint is suitably authenticated for deployment.
File/path:        tests/DMO.IntegrationTests/TechnicalEndpointTests.cs
```

```text
Test:             TechnicalEndpointTests.Health_DoesNotExposeTheConnectionString
Purpose:          Verify the technical surface does not leak credential material.
Master/plan
behaviour:        P1-T01 request §5 (secrets come from configuration, never committed) and
                  §6 forbidden scope (no business surface).
Preconditions:    As above; the factory injects a known placeholder connection string.
Action:           GET /health and read the raw body.
Assertions:       The body does not contain 'Password', 'Host=' or the placeholder database
                  name.
Required
non-effects:      No configuration value or secret appears in the response.
What this proves: The technical endpoint reflects no configuration detail.
What this does
NOT prove:        That other (future) endpoints are leak-free; that logs are leak-free.
File/path:        tests/DMO.IntegrationTests/TechnicalEndpointTests.cs
```

## 4. The host fails startup without database configuration

```text
Test:             StartupConfigurationTests.Host_WhenConnectionStringAbsent_FailsStartup
Purpose:          Verify fail-fast startup, not just fail-fast resolver behaviour.
Master/plan
behaviour:        P1-T01 request §5: fail clearly when required database configuration is
                  absent; never silently substitute a production-looking default.
Preconditions:    WebApplicationFactory with NO Database:ConnectionString supplied.
Action:           CreateClient() (forces host startup).
Assertions:       DatabaseConfigurationException is thrown and names the configuration key.
Required
non-effects:      The host does not start; no default database is targeted.
What this proves: The failure surfaces at startup, before the process serves traffic.
What this does
NOT prove:        Behaviour under a malformed (rather than absent) production value.
File/path:        tests/DMO.IntegrationTests/StartupConfigurationTests.cs
```

```text
Test:             StartupConfigurationTests.Host_WhenConnectionStringSupplied_StartsSuccessfully
Purpose:          Required non-effect partner: prove the fail-fast guard is not simply
                  preventing every startup.
Preconditions:    WebApplicationFactory WITH a placeholder connection string.
Action:           CreateClient().
Assertions:       A client is produced (host started).
Required
non-effects:      No database connection is attempted merely by starting.
What this proves: The previous test fails for the intended reason (missing configuration).
What this does
NOT prove:        That the supplied connection string is reachable or correct.
File/path:        tests/DMO.IntegrationTests/StartupConfigurationTests.cs
```

## 5. The persistence model declares no Phase 1 entity

```text
Test:             MigrationRunnerTests.PersistenceContext_DeclaresNoEntityTypes
Purpose:          Verify the skeleton models no Phase 1 product schema.
Master/plan
behaviour:        P1-T01 request §5: "Do NOT create Phase 1 product tables yet" — users,
                  admin_accounts, templates, template_modules, permissions, capabilities,
                  roles, audit, settings, any industrial schema; §4: "No Phase 1 domain
                  schema yet".
Preconditions:    A DmoDbContext built with an Npgsql provider and a placeholder connection
                  string (the model is read without opening a connection).
Action:           Inspect context.Model.GetEntityTypes().
Assertions:       The entity type set is empty; none of the forbidden table names appears.
Required
non-effects:      No product table is implied by the model.
What this proves: The skeleton introduces no Phase 1 schema through the persistence context.
What this does
NOT prove:        That no product table exists in a real database (that is what the
                  database-backed test below checks); that no future task adds one.
File/path:        tests/DMO.IntegrationTests/MigrationRunnerTests.cs
```

```text
Test:             MigrationRunnerTests.ListPendingAsync_WithNoConfiguredConnection_Throws
Purpose:          Verify the migration runner respects the same fail-fast configuration rule
                  as the rest of the infrastructure.
Master/plan
behaviour:        P1-T01 request §5, applied to the migration mechanism.
Preconditions:    DI container with infrastructure registered and no connection string.
Action:           IMigrationRunner.ListPendingAsync().
Assertions:       DatabaseConfigurationException is thrown.
Required
non-effects:      The runner does not fall back to a default database.
What this proves: The migration path cannot silently target an unintended database.
What this does
NOT prove:        Migration behaviour against a real database.
File/path:        tests/DMO.IntegrationTests/MigrationRunnerTests.cs
```

## 6. A real migration run applies nothing and changes no schema

```text
Test:             DatabaseConnectivityTests
                  .ApplyPending_AgainstRealDatabase_AppliesNothingAndLeavesSchemaUntouched
Purpose:          Verify against a real PostgreSQL database that the migration mechanism runs
                  and that Phase 1 creates no schema.
Master/plan
behaviour:        P1-T01 request §5: "A migration history/mechanism may exist with zero product
                  migrations." §8 explicitly permits "migration-runner startup behavior where
                  no product migration exists" as a technical check.
Preconditions:    DMO_TEST_POSTGRES_CONNECTION set to a DISPOSABLE PostgreSQL database.
                  SKIPPED when unset — the test never guesses a target.
Action:           Read the public base tables; call IMigrationRunner.ApplyPendingAsync();
                  read the public base tables again.
Assertions:       AppliedCount == 0; AppliedMigrations is empty; the table list is identical
                  before and after.
Required
non-effects:      No table is created, altered or dropped.
What this proves: The migration mechanism is wired and is inert with zero product migrations.
What this does
NOT prove:        That the schema of any real environment is correct; that future migrations
                  will behave correctly.
File/path:        tests/DMO.IntegrationTests/DatabaseConnectivityTests.cs
```

---

## Tests deliberately NOT proposed

Per request §4 and §6, no tests were written for:

- authentication, login, session or credential handling;
- USER / ADMIN accounts or classification;
- Templates, Template Module composition or landing destinations;
- the Module Registry, access resolution or navigation;
- permissions, capabilities or roles;
- any operational or industrial workflow (Boquilhas, Controlo, Job On, Armazém, Tools,
  repairs, PDF/file workflow).

None of these exists in P1-T01, so any test for them would be a speculative product test
explicitly forbidden by the request.

## Environment note for the Architect

Only test 6 touches a database, and it is skipped unless
`DMO_TEST_POSTGRES_CONNECTION` is explicitly set. No test in this task was pointed at any
Supabase project — neither DEV (`fsxmxyaghxzhpdydamml`) nor PROD
(`bddfhbyrmchktqotpzgb`) — and no database was contacted during P1-T01.

If the Architect authorises execution, the connection string should name a disposable
database explicitly, so the run cannot be mistaken for a DEV or PROD operation.
