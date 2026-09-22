# Tests

Test projects, ready for Architect-approved tests.

## Projects

| Project | Purpose |
| --- | --- |
| `DMO.UnitTests/` | Isolated tests of application/infrastructure units. |
| `DMO.IntegrationTests/` | Host-level tests: startup, HTTP surface, persistence/migration wiring. |

## P1-T01 status

> CORRECTED TEST INFRASTRUCTURE — NOT YET RE-EXECUTED — AWAITING ARCHITECT VERIFICATION

The proposed tests were reviewed by the Architect
(`dmo-work/dev/reviews/P1-T01_APPLICATION_SKELETON_REVIEW.md`), corrected, and then
**authorized for execution**. The first authorized run
(`dmo-work/dev/responses/P1-T01_TEST_EXECUTION_RESPONSE.md`, DMO-MODULAR `67ca5d9`) produced:

```text
19 tests — 14 passed, 4 failed, 1 skipped
```

The four failures were **test-construction defects** (a class-fixture constructor argument and
two assertion boundaries), not runtime defects. Two further Architect reviews
(`P1-T01_TEST_EXECUTION_REVIEW.md`, then `P1-T01_TEST_INFRASTRUCTURE_CORRECTION_PLAN_V2_REVIEW.md`)
required a test-infrastructure correction, which is applied here:

- `Host/DmoWebApplicationFactory.cs` is **parameterless**, and injects the placeholder
  connection string through **process-scoped environment configuration** before the real entry
  point runs, preserving and restoring any pre-existing process-scoped value on dispose. User
  and Machine scopes are never read or modified.
- `MigrationRunnerTests.ListPendingAsync_WithNoConfiguredConnection_Throws` places the entire
  attempted migration-path operation inside the asserted delegate.
- `Host/ProcessEnvironmentCollection.cs` declares a named xUnit collection with
  `DisableParallelization = true`, and `TechnicalEndpointTests` + `StartupConfigurationTests`
  are placed in it — see "Serialized collection" below.

**The corrected tests have not been executed.** Execution is gated pending Architect
verification of the corrected test code.

## Serialized collection

`Host/ProcessEnvironmentCollection.cs` (`Name = "ProcessEnvironment"`,
`DisableParallelization = true`) serializes the test classes that touch the process-global
`Database__ConnectionString`.

`DmoWebApplicationFactory` sets that variable for its lifetime, so two factories alive at the
same time would interleave their capture/restore sequences and could leave the process in the
wrong state. xUnit may run different test classes concurrently unless collection
parallelization is constrained, so serialization is declared rather than assumed away.

| Class | In collection? | Why |
| --- | --- | --- |
| `TechnicalEndpointTests` | **Yes** | constructs the factory via `IClassFixture<DmoWebApplicationFactory>` |
| `StartupConfigurationTests` | **Yes** | constructs the factory directly (`new DmoWebApplicationFactory()`) |
| `MigrationRunnerTests` | No | reads only the compile-time constant `PlaceholderConnectionString`; never constructs the factory, never mutates `Database__ConnectionString`, never depends on the factory lifetime, never observes process environment state |
| `StartupCommandsTests` | No | no factory reference, no process state |
| `DatabaseConnectivityTests` | No | no factory reference; uses `DMO_TEST_POSTGRES_CONNECTION` |

Excluding `MigrationRunnerTests` is a deliberate Architect decision (plan V2 review §1):
serializing it would add no safety. Assembly-wide parallelization disable was rejected as
broader than the actual hazard.

The projects **compile** (verified with `dotnet build`), so they are known to be mechanically
valid. Compilation is not execution.

The full test-protocol record for every proposed test — purpose, behaviour verified,
preconditions, action, assertions, required non-effects, what it proves, what it does not
prove — is in `../docs/PROPOSED_TESTS_P1-T01.md`.

## P1-T02 status

> IMPLEMENTED — default suite runs without network and without real credentials.

P1-T02 adds the authentication + account boundary tests approved in
`dmo-work/dev/responses/P1-T02_AUTH_ACCOUNT_BOUNDARY_PLAN_V5.md` (plan commit
`cdf81593`):

| Area | Coverage | Fakes/data |
| --- | --- | --- |
| `DMO.UnitTests/Authentication` | USER contract `company_number + password`; ADMIN contract `email + password`; failure reasons are credential/provider facts only; **real** `SupabaseAuthenticationService` request shape (GoTrue password grant, `apikey` header), user-verification call, error mapping; no token into the application; Supabase config fails loud | test-only fake adapters and `FakeHttpMessageHandler` (transport only) |
| `DMO.UnitTests/Accounts` | **real** `AccountResolver` semantics: active/inactive ADMIN/USER, unknown, ambiguous; provider claims grant nothing; `UnavailableAccountLookup` returns no matches; type shapes carry no Template/Module/access | `FakeAccountLookup` as data source only |
| `DMO.UnitTests/Session` | **real** `CurrentAccountContext` with stub session identity: none/admin/user/no-access outcomes; read-only contract | stub session + `FakeAccountLookup` |
| `DMO.IntegrationTests/Auth` | host plumbing (endpoints + real cookie session), production composition registers real boundaries, production fail-closed posture, missing Supabase config fails loud | `FakeTestAuthAdapter`/`FakeTestAccountLookup` registered only in the test host; transport stubbed where the real service runs |
| Live DEV/TEST test | **environment-gated and skipped by default** (`DMO_SUPABASE_LIVE_TEST=1` plus DEV/TEST config + ADMIN email/password) — real password-grant authentication against the DEV/TEST project; read-only, no account resolution, no session | real transport only when explicitly opted in |

Default suite: **no network, no real credentials**. The live test mirrors the P1-T01
`DMO_TEST_POSTGRES_CONNECTION` gating pattern (`Xunit.SkippableFact`).

All classes that construct `DmoWebApplicationFactory` are placed in the serialized
`ProcessEnvironment` collection (the factory now sets three process-scoped placeholders:
`Database__ConnectionString`, `Supabase__ProjectUrl`, `Supabase__PublishableKey`).

Zero schema/migrations: no EF entity or migration exists for P1-T02.

## Rules

Tests are evidence, not product authority. A test becomes acceptance evidence only after the
Architect confirms it represents the accepted Master/plan behaviour. A green test is never
authority by itself.

## Running (only after Architect authorisation)

```pwsh
dotnet test DMO.slnx
```

For local development and explicitly opted-in live verification, the application and the
integration-test assembly load the repository-root `.env` before .NET configuration/test
discovery. Existing process environment variables take precedence. The file remains ignored by
Git and must use the normal .NET environment names (`Database__ConnectionString`,
`Supabase__ProjectUrl`, `Supabase__PublishableKey`) plus the live-test gate/credential names
documented below. No value is logged or committed.

`DMO.IntegrationTests/DatabaseConnectivityTests.cs` requires
`DMO_TEST_POSTGRES_CONNECTION` to be set to a **disposable** PostgreSQL database; it is
skipped otherwise, so it can never silently target a development or production database.

`DMO.IntegrationTests/Auth/LiveDevTestSupabaseAdminAuthTests.cs` requires an explicit
opt-in (`DMO_SUPABASE_LIVE_TEST=1`) together with real DEV/TEST configuration
(`Supabase__ProjectUrl`, `Supabase__PublishableKey`, `DMO_SUPABASE_ADMIN_EMAIL`,
`DMO_SUPABASE_ADMIN_PASSWORD`); it is skipped otherwise, so routine runs never contact the
DEV/TEST Supabase project. It performs read-only authentication calls only.
