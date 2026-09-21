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
