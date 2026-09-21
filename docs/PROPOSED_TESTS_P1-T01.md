# Proposed tests — P1-T01

> ## CORRECTED TEST INFRASTRUCTURE — NOT YET RE-EXECUTED
> ## AWAITING ARCHITECT VERIFICATION

These test files were **created but not executed** in the first submission. Per §9 of the
P1-T01 request, the Architect reviews each proposed test first to verify that it tests the
intended contract rather than merely the implementation chosen.

The test projects are **compiled** (`dotnet build`) so that they are known to be mechanically
valid. Compilation is not execution.

## Execution history

**First authorized execution run** (`dev/responses/P1-T01_TEST_EXECUTION_RESPONSE.md`,
DMO-MODULAR `67ca5d9`): 19 tests — 14 passed, **4 failed**, 1 skipped.

The failures were **test-construction defects**, not runtime defects, and remain part of the
durable history. They are not deleted or rewritten.

| Failing test | Cause |
| --- | --- |
| TechnicalEndpointTests (2) | `DmoWebApplicationFactory` had a `bool supplyConnectionString` constructor parameter, which xUnit cannot resolve for a class fixture |
| StartupConfigurationTests.Host_WhenConnectionStringSupplied_StartsSuccessfully | the `ConfigureAppConfiguration` hook ran too late for the entry point's eager database configuration validation, so the host exited before building an `IHost` |
| MigrationRunnerTests.ListPendingAsync_WithNoConfiguredConnection_Throws | the exception surfaced during `GetRequiredService<IMigrationRunner>()`, outside the asserted delegate |

**Second Architect review** (`dev/reviews/P1-T01_TEST_EXECUTION_REVIEW.md`, status
**CORRECTION REQUIRED — TEST INFRASTRUCTURE ONLY**) required two test-infrastructure
corrections, both applied:

1. **`DmoWebApplicationFactory` is now parameterless** and injects the placeholder connection
   string through **process-scoped environment configuration** before the real entry point
   runs, preserving and restoring any pre-existing process-scoped value on dispose. User and
   Machine scopes are never read or modified. The old "missing DB config" factory mode was
   removed, since that case is tested directly at the composition boundary.
2. **`MigrationRunnerTests.ListPendingAsync_WithNoConfiguredConnection_Throws`** now places the
   entire attempted migration-path operation — scope creation, `IMigrationRunner` resolution
   and `ListPendingAsync()` — inside the asserted delegate.

**The corrected tests have NOT been executed.** No runtime code, database model, migration
mechanism or connection configuration contract was changed by either correction.

Nothing below constitutes acceptance evidence.

## Architect review outcome (first review)

The Architect reviewed the first submission
(`dev/reviews/P1-T01_APPLICATION_SKELETON_REVIEW.md`, status **CORRECTION REQUIRED**):

| Test | Decision |
| --- | --- |
| §1 DatabaseConnectionResolverTests | ACCEPTED FOR EXECUTION |
| §2 StartupCommandsTests | ACCEPTED FOR EXECUTION as technical regression, **not** product acceptance evidence |
| §3 TechnicalEndpointTests.Health_ReturnsOkWithStatusPayload | **NEEDS CORRECTION** → corrected below |
| §3b Health_DoesNotExposeTheConnectionString | ACCEPTED FOR EXECUTION |
| §4 StartupConfigurationTests.Host_WhenConnectionStringAbsent_FailsStartup | **NEEDS CORRECTION** → corrected below |
| §4b Host_WhenConnectionStringSupplied_StartsSuccessfully | ACCEPTED FOR EXECUTION |
| §5 MigrationRunnerTests.PersistenceContext_DeclaresNoEntityTypes | ACCEPTED FOR EXECUTION |
| §5b MigrationRunnerTests.ListPendingAsync_WithNoConfiguredConnection_Throws | ACCEPTED FOR EXECUTION |
| §6 DatabaseConnectivityTests | ACCEPTED FOR EXECUTION **only** against an explicit disposable PostgreSQL database |

The two corrections are recorded in §3 and §4 below. They are documentation/test-code
corrections only: no runtime code, database model, migration mechanism or connection
configuration contract was changed.

**The corrected tests have not been executed.** Execution is gated pending Architect
verification of the corrected test code (see "Execution history" above).

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

> **Architect classification.** ACCEPTED FOR EXECUTION **as technical regression, not as
> product/Master acceptance evidence.** The existence of a migration mechanism is required by
> P1-T01; the convention that `migrate` must be the first CLI argument is an implementation
> choice, not a Master/product contract. A pass protects the chosen technical entry point; it
> is **not** acceptance evidence for any product behaviour.

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

## 3. The technical startup endpoint answers with exactly the accepted shape

> **CORRECTED** following Architect review §4.3 (NEEDS CORRECTION). The previous version
> asserted only the absence of three named properties (`user`, `modules`, `templates`), which
> could still pass if unrelated product/application information were added under another
> name. It now asserts the exact accepted response shape.

```text
Test:             TechnicalEndpointTests.Health_ReturnsOkWithExactlyTheAcceptedTechnicalShape
Purpose:          Verify the runnable host exposes a minimal technical startup surface, and
                  that the surface is exactly the accepted technical shape.
Master/plan
behaviour:        P1-T01 request §3: "provides a minimal technical health/startup endpoint or
                  equivalent"; §4: DMO.Web is the host and owns no industrial business rule.
Preconditions:    The host started in-process via WebApplicationFactory, with a placeholder
                  (non-connecting) connection string so eager configuration validation passes.
Action:           GET /health.
Assertions:       HTTP 200; the body is a JSON object;
                  the field-name set is EXACTLY { "status", "environment" } (compared as an
                  ordered set, so an added field fails regardless of its name);
                  status == "ok"; environment is present and non-blank;
                  the object has exactly 2 members.
Required
non-effects:      The endpoint exposes no account, Template, Module, permission or industrial
                  data — enforced by the exact-shape equality, not by name blacklisting.
What this proves: The host starts and serves a technical surface whose payload is exactly the
                  accepted P1-T01 shape. Adding ANY additional field to the response fails
                  this test, whatever the field is called.
What this does
NOT prove:        Database connectivity; any Phase 1 business behaviour; production
                  readiness; that the endpoint is suitably protected for deployment; that a
                  future task's deliberate extension of the surface is wrong (a later accepted
                  task may widen the contract, at which point this assertion is updated).
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

## 4. Host composition requires database configuration

> **CORRECTED** following Architect review §4.5 (NEEDS CORRECTION). The previous version
> coupled acceptance to `WebApplicationFactory.CreateClient()` throwing the concrete
> `DatabaseConfigurationException`. That is not a stable boundary: `Program.cs` catches the
> exception and converts it to process exit code 1, so an in-process host may surface the
> failure through hosting mechanics rather than the original exception type.
>
> The corrected tests assert the composition boundary directly (Architect option 1), which is
> the smallest reliable form. The process-level behaviour (exit code 1 plus the
> operator-facing message) is covered by the manual startup check recorded in
> `TECHNICAL_CHECKS_P1-T01.md` §F.4, so no heavy process-test infrastructure was introduced.

```text
Test:             StartupConfigurationTests.AddDmoInfrastructure_WhenConnectionStringAbsent_Throws
Purpose:          Verify host composition refuses to build without required database
                  configuration.
Master/plan
behaviour:        P1-T01 request §5: fail clearly when required database configuration is
                  absent; never silently substitute a production-looking default.
Preconditions:    An empty in-memory IConfiguration (no Database:ConnectionString).
Action:           services.AddDmoInfrastructure(configuration).
Assertions:       DatabaseConfigurationException is thrown and names the configuration key.
Required
non-effects:      Composition does not complete; no default database target is registered.
What this proves: The stable composition boundary rejects missing configuration, independently
                  of how a web host would surface the failure.
What this does
NOT prove:        The process exit code; the operator-facing message text (both covered by the
                  manual startup check); behaviour under a malformed rather than absent value
                  (covered by the theory below).
File/path:        tests/DMO.IntegrationTests/StartupConfigurationTests.cs
```

```text
Test:             StartupConfigurationTests.AddDmoInfrastructure_WhenConnectionStringInvalid_Throws
                  (theory: "", "   ", "not a connection string", "Database=dmo")
Purpose:          Verify malformed configuration is rejected, not only absent configuration.
Master/plan
behaviour:        P1-T01 request §5: fail clearly when required database configuration is
                  invalid.
Preconditions:    IConfiguration supplying the key with a blank/unparseable value, or one
                  missing Host/Database.
Action:           services.AddDmoInfrastructure(configuration).
Assertions:       DatabaseConfigurationException is thrown in every case.
Required
non-effects:      No implicit or default database target is invented.
What this proves: The guard covers invalid input, not just missing input.
What this does
NOT prove:        That a syntactically valid string points at a reachable database.
File/path:        tests/DMO.IntegrationTests/StartupConfigurationTests.cs
```

```text
Test:             StartupConfigurationTests.AddDmoInfrastructure_WhenConnectionStringSupplied_Succeeds
Purpose:          Required non-effect partner: prove the guard is not simply rejecting
                  everything.
Preconditions:    IConfiguration supplying a valid syntactic connection string.
Action:           services.AddDmoInfrastructure(configuration).
Assertions:       No exception; a DatabaseConnectionResolver registration exists.
Required
non-effects:      Registering infrastructure does not open a database connection.
What this proves: The previous tests fail for the intended reason (missing/invalid
                  configuration), not because composition always fails.
What this does
NOT prove:        That the supplied connection string is reachable or correct.
File/path:        tests/DMO.IntegrationTests/StartupConfigurationTests.cs
```

```text
Test:             StartupConfigurationTests.Host_WhenConnectionStringSupplied_StartsSuccessfully
Purpose:          Required non-effect partner at host level: valid configuration does not
                  prevent host startup and does not force an immediate database connection.
Preconditions:    The parameterless DmoWebApplicationFactory, which injects the placeholder
                  connection string into PROCESS-scoped configuration before the real entry
                  point runs.
Action:           CreateClient().
Assertions:       A client is produced (host started).
Required
non-effects:      No database connection is attempted merely by starting.
What this proves: Host startup is not blocked by valid configuration, and starting does not
                  eagerly connect.
What this does
NOT prove:        That the supplied connection string is reachable or correct; process-level
                  exit-code behaviour when configuration is absent.
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
Action:           The ENTIRE attempted migration-path operation, inside the asserted
                  delegate: creating the scope, resolving IMigrationRunner, and calling
                  ListPendingAsync().
Assertions:       DatabaseConfigurationException is thrown; its message names the
                  configuration key.
Required
non-effects:      The runner does not fall back to a default database.
What this proves: The migration path cannot silently target an unintended database.
What this does
NOT prove:        Migration behaviour against a real database.
Note:             Database configuration is validated when the persistence services are
                  resolved, not only when the runner method executes. The accepted contract
                  is "attempting the migration path with no database configuration raises
                  DatabaseConfigurationException"; it does not require the exception to
                  originate specifically inside ListPendingAsync().
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

### Architect execution conditions for test 6

From `dev/reviews/P1-T01_APPLICATION_SKELETON_REVIEW.md` §4.9:

- `DMO_TEST_POSTGRES_CONNECTION` must point to an **explicitly disposable** database;
- **never** DEV or PROD;
- the developer must identify the disposable target in the execution response;
- if no disposable PostgreSQL target exists, the test is left unexecuted and that fact is
  reported;
- no production/development Supabase database may be created or used merely to satisfy it.

At the time of this correction no disposable PostgreSQL target was available in this
environment, so this test remains unexecuted and the fact will be reported at execution time.

## EF Core migration history — Architect decision

The first response raised `__EFMigrationsHistory` as a possible open conflict. The Architect
decided (`dev/reviews/P1-T01_APPLICATION_SKELETON_REVIEW.md` §3): **no conflict.**

- EF Core migration bookkeeping is infrastructure, not product schema.
- It is acceptable for EF Core to create/use `__EFMigrationsHistory` when migrations actually
  require it.
- `EfCoreMigrationRunner.ApplyPendingAsync()` returns before `MigrateAsync()` when there are
  zero pending migrations, so the zero-migration path is intentionally inert.
- The migration mechanism must **not** be redesigned to avoid the EF history table.

No implementation change was made in response. The §6 test asserts on the public BASE TABLE
set, so the history table (created only once a migration is actually applied) is outside the
compared set.
