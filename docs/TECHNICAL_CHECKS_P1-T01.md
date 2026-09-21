# Technical checks — P1-T01

> These are **non-acceptance technical checks**, permitted by §8 of the P1-T01 request to
> establish that the skeleton is mechanically valid. They are **not** product acceptance
> tests. No task-specific test was executed (see `PROPOSED_TESTS_P1-T01.md`).

Environment: Windows, .NET SDK 10.0.400, ASP.NET Core runtime 10.0.11, x64.
Working directory: repository root.

---

## 1. Clean restore

```pwsh
dotnet restore DMO.slnx
```

Result — exit code 0:

```text
  Restored D:\DMO-MODULAR\src\DMO.Domain\DMO.Domain.csproj (in 64 ms).
  Restored D:\DMO-MODULAR\src\DMO.Application\DMO.Application.csproj (in 64 ms).
  Restored D:\DMO-MODULAR\src\DMO.Web\DMO.Web.csproj (in 119 ms).
  Restored D:\DMO-MODULAR\tests\DMO.UnitTests\DMO.UnitTests.csproj (in 166 ms).
  Restored D:\DMO-MODULAR\tests\DMO.IntegrationTests\DMO.IntegrationTests.csproj (in 167 ms).
  Restored D:\DMO-MODULAR\src\DMO.Infrastructure\DMO.Infrastructure.csproj (in 178 ms).
```

Run after deleting all `bin/` and `obj/` directories.

## 2. Build

```pwsh
dotnet build DMO.slnx --no-restore
```

Result — exit code 0:

```text
  DMO.Domain -> ...\src\DMO.Domain\bin\Debug\net10.0\DMO.Domain.dll
  DMO.Application -> ...\src\DMO.Application\bin\Debug\net10.0\DMO.Application.dll
  DMO.Infrastructure -> ...\src\DMO.Infrastructure\bin\Debug\net10.0\DMO.Infrastructure.dll
  DMO.UnitTests -> ...\tests\DMO.UnitTests\bin\Debug\net10.0\DMO.UnitTests.dll
  DMO.Web -> ...\src\DMO.Web\bin\Debug\net10.0\DMO.Web.dll
  DMO.IntegrationTests -> ...\tests\DMO.IntegrationTests\bin\Debug\net10.0\DMO.IntegrationTests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)
```

All six projects compile, including the two test projects (compilation is not execution).

## 3. Migration command with no database configuration

```pwsh
dotnet run --project src/DMO.Web --no-build -- migrate
```

Result — exit code 1, with the configuration failure reported and named:

```text
fail: DMO.Web[0]
      Migration runner failed: Database connection string is not configured. Set
      'Database:ConnectionString' (configuration or user-secrets) or the
      'Database__ConnectionString' environment variable. No default connection is assumed.
      DMO.Infrastructure.Database.DatabaseConfigurationException: ...
```

The runner refuses to run rather than defaulting to an implicit database.

## 4. Host startup with no database configuration

```pwsh
dotnet run --project src/DMO.Web --no-build
```

Result — exit code 1, single-line operator message, no stack trace:

```text
Startup failed: Database connection string is not configured. Set 'Database:ConnectionString'
(configuration or user-secrets) or the 'Database__ConnectionString' environment variable.
No default connection is assumed.
```

### Finding corrected during this check

The **first** version of this check did **not** fail: the host started and listened on
`http://localhost:5280` even though no database configuration was present, because the
configuration guard was evaluated lazily on the first `DmoDbContext` resolution and the
technical endpoint performs no database access.

That contradicted §5 of the request ("The application should fail clearly when required
database configuration is absent or invalid"). It was corrected by validating the connection
string during composition in `AddDmoInfrastructure`, and by handling the failure in
`Program.cs` so it is reported as one clear line with exit code 1 instead of an unhandled
exception. The check above is the result **after** that correction.

## 5. Host startup with configuration, and the technical endpoint

```pwsh
$env:Database__ConnectionString="Host=127.0.0.1;Port=5432;Database=dmo_skeleton_check;Username=check;Password=check"
dotnet run --project src/DMO.Web --no-build
```

Result — exit code 0 while running; host started:

```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5280
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

Request:

```pwsh
Invoke-WebRequest -Uri "http://localhost:5280/health" -UseBasicParsing
```

Result:

```text
HTTP STATUS: 200
BODY: {"status":"ok","environment":"Development"}
```

This exercises the minimal technical startup surface only. It reads no product data and
does not touch the database.

## 6. Migration mechanism against a non-live server

```pwsh
$env:Database__ConnectionString="Host=127.0.0.1;Port=5432;Database=dmo_skeleton_check;Username=check;Password=check;Timeout=3"
dotnet run --project src/DMO.Web --no-build -- migrate
```

Result — exit code 1, failing at the **connection** stage rather than the configuration stage:

```text
fail: Microsoft.EntityFrameworkCore.Database.Connection[20004]
      An error occurred using the connection to database 'dmo_skeleton_check' on server
      'tcp://127.0.0.1:5432'.
      Migration runner failed: Failed to connect to 127.0.0.1:5432
      Npgsql.NpgsqlException (0x80004005): Failed to connect to 127.0.0.1:5432
```

This proves the migration mechanism is genuinely wired through to Npgsql: valid
configuration is accepted and the runner proceeds to open a real connection attempt.

## 7. Migration run against a live database — NOT PERFORMED

No PostgreSQL server was available in this environment:

- Docker daemon not running (`docker ps` -> cannot connect to the Docker API at
  `npipe:////./pipe/dockerDesktopLinuxEngine`);
- no local PostgreSQL service or `psql`/`pg_ctl` on `PATH`;
- no `C:\Program Files\PostgreSQL` installation.

**No database was contacted during P1-T01.** In particular, no Supabase project was touched —
neither DEV (`fsxmxyaghxzhpdydamml`) nor PROD (`bddfhrymchktqotpzgb`).

A repeatable migration run against a real database is covered by the proposed test
`DatabaseConnectivityTests`, which is skipped unless `DMO_TEST_POSTGRES_CONNECTION` names a
**disposable** database. It is not executed, per the test protocol.

---

## Summary

| # | Check | Command | Outcome |
| --- | --- | --- | --- |
| 1 | Clean restore | `dotnet restore DMO.slnx` | exit 0, 6 projects restored |
| 2 | Build | `dotnet build DMO.slnx --no-restore` | exit 0, 0 warnings, 0 errors |
| 3 | Migrate, no configuration | `dotnet run ... -- migrate` | exit 1, configuration failure named |
| 4 | Host startup, no configuration | `dotnet run ...` | exit 1, one clear line, no stack trace |
| 5 | Host startup + `/health` | `dotnet run ...` + HTTP GET | HTTP 200 `{"status":"ok","environment":"Development"}` |
| 6 | Migrate, non-live server | `dotnet run ... -- migrate` | exit 1 at connection stage (mechanism wired) |
| 7 | Migrate, live database | — | not performed; no PostgreSQL available |

**No test method was executed. `dotnet test` was never run.**

---

## Correction pass (Architect review: CORRECTION REQUIRED)

The Architect reviewed the first submission
(`dmo-work/dev/reviews/P1-T01_APPLICATION_SKELETON_REVIEW.md`) and required two test
corrections plus one documentation wording correction. Details are in
`PROPOSED_TESTS_P1-T01.md`.

**No runtime code, database model, migration mechanism or connection configuration contract
was changed by the correction.** Only documentation and test files were edited.

### Re-verified after the correction

```pwsh
dotnet build DMO.slnx
```

Result — exit code 0:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

All six projects recompiled, including both test projects after the corrections.

### Not re-run

The startup/`/health`/migration checks in §1–§7 above were **not** re-run, because the
correction changed no runtime code — `Program.cs`,
`InfrastructureServiceCollectionExtensions.cs`, `TechnicalEndpoints.cs` and the migration
runner are byte-identical to the versions those checks exercised.

### Still not executed

No test method was executed by that correction pass. `dotnet test` was not run at that point;
test execution remained gated pending Architect verification of the correction commit.

---

## Test execution and test-infrastructure correction

### First authorized test execution

After the Architect accepted the corrections and authorized execution
(`dmo-work/dev/reviews/P1-T01_TEST_EXECUTION_AUTHORIZATION.md`), the approved tests were run
once against DMO-MODULAR `67ca5d9`:

```pwsh
dotnet test DMO.slnx --logger "console;verbosity=detailed"
```

Result — exit code 1: **19 tests — 14 passed, 4 failed, 1 skipped**.

The four failures were **test-construction defects**, not runtime defects. The startup,
`/health` and migration checks in §1–§7 above all remain valid: no runtime code was involved
in any failure. Full detail is in `dmo-work/dev/responses/P1-T01_TEST_EXECUTION_RESPONSE.md`.

### Test-infrastructure correction

A second Architect review (`dmo-work/dev/reviews/P1-T01_TEST_EXECUTION_REVIEW.md`, status
**CORRECTION REQUIRED — TEST INFRASTRUCTURE ONLY**) required two test-only corrections, both
applied:

1. `DmoWebApplicationFactory` made parameterless and switched to process-scoped environment
   injection (restored on dispose), so the real entry point sees the placeholder connection
   string during eager configuration validation.
2. `MigrationRunnerTests.ListPendingAsync_WithNoConfiguredConnection_Throws` now places the
   entire attempted migration-path operation inside the asserted delegate.

**No runtime code, database model, migration mechanism or connection configuration contract
was changed.** Only test files and documentation were edited.

### Re-verified after the test-infrastructure correction

```pwsh
dotnet build DMO.slnx
```

Result — exit code 0:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### The corrected tests have not been executed

`dotnet test` was **not** run after the test-infrastructure correction. Execution is gated
pending Architect verification of the corrected test code. The §1–§7 startup/`/health`/migration
checks were not re-run either, because no runtime code changed.
