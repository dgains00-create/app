# DMO.Application

Application orchestration and the contracts required by the runtime.

## P1-T01 status

Deliberately minimal. The only contract materialised in this task is the migration-runner
boundary (`Migrations/`), which the runtime host needs in order to expose a migration entry
point without knowing how migrations are physically stored or executed.

Not implemented here, by task scope:

- Users, Templates, Module Registry behaviour, access resolution;
- authentication workflows;
- any operational/industrial orchestration.

These belong to later Phase 1 slices (P1-T02 onward) and are not pre-built.
