# DMO.Application

Application orchestration and the contracts required by the runtime.

## Contents

| Concern | Location |
| --- | --- |
| Migration-runner boundary | `Migrations/` |
| Canonical Module vocabulary + registry | `Access/ModuleCatalog.cs`, `Access/ModuleRegistry.cs` |
| Fail-closed access resolution | `Access/AccessResolver.cs`, `Access/AccessOutcome.cs`, `Access/ModuleResolve.cs` |
| Access facade | `Access/ModuleAccessService.cs` |
| Account resolution + account models | `Accounts/` |
| Authentication boundary + session | `Authentication/`, `Session/` |
| Template model | `Templates/` |
| Template administration | `TemplateAdministration/` |
| USER administration | `UserAdministration/` |

## Honest build availability

`Access/ModuleRegistrations.cs` is the only source of build availability. It is currently
empty: no industrial operational surface is implemented yet, so no operational Module is
advertised as available. A Module is registered there only when its real functional surface
and server-side enforcement exist.

## Boundaries

Application orchestration only. Persistence plumbing lives in `DMO.Infrastructure`;
host/composition and HTTP surfaces live in `DMO.Web`. No industrial business rule is
duplicated across those projects.
