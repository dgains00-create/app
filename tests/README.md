# Tests

Test projects, ready for Architect-approved tests.

## Projects

| Project | Purpose |
| --- | --- |
| `DMO.UnitTests/` | Isolated tests of application/infrastructure units. |
| `DMO.IntegrationTests/` | Host-level tests: startup, HTTP surface, persistence/migration wiring. |

## P1-T01 status

The proposed tests for P1-T01 were **created but NOT executed**, as required by the task:

> TASK-SPECIFIC TESTS NOT EXECUTED — AWAITING ARCHITECT REVIEW

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
