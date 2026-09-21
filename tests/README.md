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
two assertion boundaries), not runtime defects. A second Architect review
(`dmo-work/dev/reviews/P1-T01_TEST_EXECUTION_REVIEW.md`) required a test-infrastructure
correction, which is applied here:

- `Host/DmoWebApplicationFactory.cs` is now **parameterless**, and injects the placeholder
  connection string through **process-scoped environment configuration** before the real entry
  point runs, preserving and restoring any pre-existing process-scoped value on dispose. User
  and Machine scopes are never read or modified.
- `MigrationRunnerTests.ListPendingAsync_WithNoConfiguredConnection_Throws` now places the
  entire attempted migration-path operation inside the asserted delegate.

**The corrected tests have not been executed.** Execution is gated pending Architect
verification of the corrected test code.

The projects **compile** (verified with `dotnet build`), so they are known to be mechanically
valid. Compilation is not execution.

The full test-protocol record for every proposed test — purpose, behaviour verified,
preconditions, action, assertions, required non-effects, what it proves, what it does not
prove — is in `../docs/PROPOSED_TESTS_P1-T01.md`.

## Rules

Tests are evidence, not product authority. A test becomes acceptance evidence only after the
Architect confirms it represents the accepted Master/plan behaviour. A green test is never
authority by itself.

## Running (only after Architect authorisation)

```pwsh
dotnet test DMO.slnx
```

`DMO.IntegrationTests/DatabaseConnectivityTests.cs` requires
`DMO_TEST_POSTGRES_CONNECTION` to be set to a **disposable** PostgreSQL database; it is
skipped otherwise, so it can never silently target a development or production database.
