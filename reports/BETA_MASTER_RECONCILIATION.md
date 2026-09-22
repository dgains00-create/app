# BETA_MASTER_RECONCILIATION

Reconciliation of the current `diogo-o/DMO-MODULAR` implementation against the
`diogo-o/dmo-beta-master` Beta authority.

This is a reconciliation/reporting artifact only. No application code, schema, migration,
route, test or configuration was modified. No implementation was performed.

---

## 1. Executive Summary

DMO-MODULAR at `main` is **still in the application-foundation phase**. It implements the
accepted shared foundation (authentication, account resolution, persistence of
ADMIN/USER/Template/module composition, Module Registry + fail-closed Access Resolver,
server-side Module authorization policies, Template administration, USER administration and
the P1-T07 account-aware navigation/USER shell). It contains **no operational Beta module**
whatsoever: no Job On, no Ferramentas/Tool, no Controlo (Peso/Comparação/Pegamentos/Folha/
Resumo), no Boquilhas, and no shared frontend feature components.

The single verified source-level fact that drives most of this report is:

```text
src/DMO.Application/Access/ModuleRegistrations.cs
    public static IReadOnlyList<ModuleDefinition> CurrentBuildAvailable { get; } = [];
```

The canonical 13-module vocabulary exists and the server-side gate exists, but **zero**
operational Module is registered available, **zero** destination route is registered
(`EmptyDestinationRouteRegistry`), and **zero** operational page/route/entity/service exists.
`DMO.Domain` contains only a README; `DMO.Infrastructure` contains only the 4 foundation
entities (`admin_accounts`, `users`, `templates`, `template_modules`).

**How much of Beta is already implemented.** Reported as a coarse capability inventory, not
invented percentages. From the Beta capability inventory reconciled in §5:

- The **shared foundation** that Beta declares as a dependency (BETA_SCOPE "Shared
  foundations", ACCEPTANCE_MATRIX §1) is **substantially implemented** and must be preserved.
- **Workstream A (shared frontend shell/contracts)** is **partially** implemented: the A1
  presentation contract is published/frozen and the shell/navigation projection exists, but
  the ten shared feature components are contract-only (no component code).
- Operational Beta modules **B–E are essentially not implemented at all** — they are
  genuine functional gaps, not presentation gaps.

**Major remaining categories of work.**

1. Complete Workstream A shared feature components (ProductionContextStrip, ToolPicker,
   ToolSummaryRow, DenseDataTable, RecordStatus, AvailabilityState, AuditTrail,
   MeasurementRows, DecisionBar) — none has implementation code.
2. Register the first real operational Module definitions and real destination routes as
   each feature surface lands (currently intentionally empty).
3. Implement the operational Beta modules B–E with their backend/domain contracts. These are
   blocked on backend/interface contracts that no Beta authority has yet published as
   concrete schema/endpoint contracts.
4. Recover the operational frontend presentation.

**Is major rework necessary?** **No.** The foundation is architecturally aligned with the
Beta authority and is explicitly protected by Beta (`implementation/CURRENT_FOUNDATION.md`,
`ACCEPTANCE_MATRIX.md` §1). Beta work is overwhelmingly **additive** (register modules,
register routes, add feature surfaces) rather than corrective. The only material
corrective items are small documentation/architecture bookkeeping items, listed in §11.

One reconciliation-relevant governance note: P1-T07's implementation commit is recorded by
Beta as `IMPLEMENTED / NOT YET ACCEPTED` (SOURCE_MANIFEST), so it is current code state but
**not** an accepted architectural result yet.

---

## 2. Baseline

| Item | Value |
|---|---|
| `diogo-o/dmo-beta-master` remote `main` SHA | `78da49248f6cf7a8cbe4ddd946f3c38abbaf322f` — "Record document lifecycle acceptance consolidation" |
| `diogo-o/DMO-MODULAR` remote `main` SHA | `0b47690936599b6a71342b68b1cf36cfe4b64264` — "P1-T07: navigation + user shell (root routing, landing, login, no-access, administration)" |
| Reconciliation date/time (UTC) | 2026-09-22T09:23Z (session start) — report produced 2026-09-22 |

Supporting (non-authoritative) repositories inspected for provenance and history:

| Item | Value |
|---|---|
| `diogo-o/dmo-master` remote `main` SHA | `8f1ca3e27e0eaf58c3dce285b544066565fa3dc1` |
| `diogo-o/dmo-master` remote `dmo-modular` SHA | `ae2a9b9d12132ee4b41dc0696f34c6439b5cca52` |
| `diogo-o/workbench` remote `main` SHA | `50edc6a6be1584f75b2ad48233503a45e049d841` |
| `diogo-o/dmo-work` remote `main` SHA | `ef4daeb1e6421cc17b19caec2c2f027a872b52d0` |

DMO-MODULAR is a **shallow clone** (`git rev-parse --is-shallow-repository` → `true`);
`main` is a single grafted commit. `origin/main` equals the local `HEAD` equals remote
`refs/heads/main` at the SHA above.

---

## 3. Authority Sources Read

### 3.1 `dmo-beta-master` (primary Beta authority) — read in full

Root: `README.md`, `INDEX.md`, `AUTHORITY.md`, `BETA_SCOPE.md`, `WORKFLOW.md`,
`IMPLEMENTATION_MODEL.md`, `ACCEPTANCE_MATRIX.md`, `SOURCE_MANIFEST.md`.

Architecture: `architecture/ACCESS_AND_NAVIGATION.md`, `architecture/APPLICATION_FOUNDATION.md`,
`architecture/BACKEND_FRONTEND_MODEL.md`, `architecture/CROSS_MODULE_FLOWS.md`,
`architecture/RECORD_LIFECYCLES.md`.

Contracts: `contracts/IDENTITIES_AND_RELATIONSHIPS.md`, `contracts/SHARED_FRONTEND.md`,
`contracts/DOCUMENTS_AND_FILES.md`.

Modules: `modules/JOB_ON_LIGHT.md`, `modules/FERRAMENTAS_LIGHT.md`, `modules/CONTROLO_CREATE.md`,
`modules/CONTROLO_APPROVE.md`, `modules/BOQUILHAS.md`.

Implementation: `implementation/CURRENT_FOUNDATION.md`, `implementation/BETA_INTEGRATION_SEAMS.md`.

Source archive: `sources/README.md`, `sources/dmo-master/global/DOCUMENT_FILE_MODEL.md`.

### 3.2 Upstream global authority referenced by the Beta (read for the module vocabulary)

`diogo-o/dmo-master` @ `dmo-modular` (`ae2a9b9d…`): `global/ACCESS_MODEL.md`
(the canonical "13 assignable modules" table, §1), `global/MODULAR_IMPLEMENTATION_MODEL.md`,
`BETA_VERSION.md`, `modules/CONTROLO.md`, `modules/HISTORIA.md`, `modules/TAMPOES.md`,
`modules/REPARACAO_PROGRAMADA.md`.

### 3.3 Historical/provenance sources consulted (evidence only, not Beta authority)

`diogo-o/workbench` @ `main`: `BETA_FRONTEND_IMPLEMENTATION_WORKSTREAMS.md`,
`dev/plans/BETA_FRONTEND_WORKSTREAM_A_PLAN.md`,
`dev/plans/BETA_FRONTEND_WORKSTREAM_A2_CORRECTION_PLAN.md`,
`dev/reviews/BETA_FRONTEND_WORKSTREAM_A2_CORRECTION_IMPLEMENTATION_REVIEW.md`.

`diogo-o/dmo-work` @ `main`: `plans/PHASE_1_ARCHITECTURE_PLAN.md` (referenced),
`dev/responses/P1-T07_NAVIGATION_USER_SHELL_IMPLEMENTATION_RESPONSE.md`.

Note: `workbench/BETA_DESIGN_RECONCILIATION_PLAN.md` — named by SOURCE_MANIFEST as the settled
frontend design authority — is **not present** in either inspected repository. It is recorded
as `HISTORICAL REFERENCE MISSING` by the Beta itself (§11, A2).

---

## 4. Current Implementation Sources Inspected

### 4.1 Access / Module Registry / authorization

`src/DMO.Application/Access/`: `ModuleId.cs`, `ModuleCatalog.cs`, `ModuleDefinition.cs`,
`ModuleSurfaceDescriptor.cs`, `ModuleRegistrations.cs`, `ModuleRegistry.cs`, `ModuleResolve.cs`,
`AccessResolver.cs`, `AccessOutcome.cs`, `ModuleAccessService.cs`,
`IModuleAccessService.cs`, `IModuleRegistry.cs`, `IAccessResolver.cs`.
`src/DMO.Web/Authorization/`: `AdministrationAuthorizationPolicies.cs`,
`AdminAuthorizationHandler.cs`, `AdminAuthorizationRequirement.cs`,
`ModuleAuthorizationPolicies.cs`, `ModuleAuthorizationHandler.cs`,
`ModuleAuthorizationRequirement.cs`.

### 4.2 Authentication / accounts / session

`src/DMO.Application/Authentication/`: `IAuthenticationBoundary.cs`, `AuthenticationPath.cs`,
`AuthenticationOutcome.cs`, `AuthenticatedIdentity.cs`, `AuthenticationRequest.cs`,
`AuthenticationFailureReason.cs`, `UserLoginIdentity.cs`, `IUserAuthenticationLookup.cs`.
`src/DMO.Application/Accounts/`: `AccountResolver.cs`, `AccountResolution.cs`,
`AccountMatch.cs`, `AccountType.cs`, `UserAccount.cs`, `AdminAccount.cs`,
`AdminAccountId.cs`, `NoAccessReason.cs`, `IAccountLookup.cs`.
`src/DMO.Application/Session/`: `CurrentAccount.cs`, `ICurrentAccountContext.cs`.
`src/DMO.Web/Auth/`: `SessionLoginService.cs`, `SessionAuthentication.cs`,
`CurrentAccountContext.cs`, `SupabaseAuthenticationService.cs`, `SupabaseOptions.cs`,
`SupabaseAdminUserService.cs`, `SupabaseAdminOptions.cs`.
`src/DMO.Web/Endpoints/`: `AuthEndpoints.cs`.

### 4.3 Persistence / domain

`src/DMO.Infrastructure/Migrations/20260922001736_AccountAndTemplateFoundation.cs`,
`…1757_TemplateModuleComposition.cs`, `DmoDbContextModelSnapshot.cs`,
`Persistence/Entities/{AdminAccountEntity,UserEntity,TemplateEntity,TemplateModuleEntity}.cs`,
`Persistence/EntityConfigurations/*`, `Persistence/{AdminAccountRepository,UserRepository,
TemplateRepository,TemplateModuleRepository,PersistenceAccountLookup,UserAuthenticationLookup}.cs`,
`Persistence/DmoDbContext.cs`.
`src/DMO.Application/Repositories/{ITemplateRepository,ITemplateModuleRepository,
IUserRepository,IAdminAccountRepository}.cs`,
`src/DMO.Application/Templates/{Template,TemplateModule}.cs`.
`src/DMO.Domain/` — contains only `DMO.Domain.csproj` + `README.md`.

### 4.4 Administration (Admin) surfaces

`src/DMO.Web/Pages/Administration/Index.cshtml(.cs)`,
`Pages/Administration/Users/{List,Create,Edit,Delete,ResendInvite,ResetPassword}.cshtml(.cs)`,
`Pages/Administration/Templates/{List,Create,Edit,Delete}.cshtml(.cs)`;
`src/DMO.Application/TemplateAdministration/*`, `src/DMO.Application/UserAdministration/*`.

### 4.5 Frontend / navigation / shell

`src/DMO.Web/Frontend/Shell/{DestinationRoutes,NavigationProjectionService,
ShellPresentationModels,ShellPresentationService}.cs`,
`src/DMO.Web/Frontend/Shared/SharedFrontendExtensions.cs`,
`src/DMO.Web/Navigation/{LandingSelector,UserLandingService,DestinationRouteRegistrations}.cs`,
`src/DMO.Web/Pages/{Index,Login,AccessDenied}.cshtml(.cs)`,
`src/DMO.Web/Pages/Shared/{_Layout,_PublicLayout,_Identity}.cshtml`,
`src/DMO.Web/Pages/Shared/Navigation/{_PrimaryNavigation,_SecondaryNavigation}.cshtml`,
`src/DMO.Web/Program.cs`,
`src/DMO.Web/wwwroot/css/{dmo-tokens,dmo-shell,dmo-user-shell,dmo-admin-templates,
dmo-admin-users}.css`.

### 4.6 Current implementation documentation

`README.md`, `docs/ARCHITECTURE.md`, `docs/CREATION_AND_ASSOCIATION_LOGIC.md`,
`docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md`,
`docs/SKELETON_RATIONALIZATION_P1-T01.md`, `docs/PROPOSED_TESTS_P1-T01.md`,
`src/*/README.md`, `tests/README.md`.

### 4.7 Tests inspected (evidence only; not modified)

`tests/DMO.UnitTests/`: `Access/*` (incl. `ModuleRegistryTests`, `AccessResolverTests`,
`ModuleAccessServiceTests`, `ModuleAuthorizationHandlerTests`, `NoProfilesRegressionTests`),
`Accounts/*`, `Authentication/*`, `Session/*`, `Navigation/{LandingSelectorTests,
UserLandingServiceTests}.cs`, `Frontend/Shared/NavigationProjectionServiceTests.cs`,
`TemplateAdministration/*`, `UserAdministration/*`.
`tests/DMO.IntegrationTests/`: `Navigation/{RootRoutingTests,NoAccessPageTests,
LoginPageTests,DirectRouteEnforcementTests,UserLandingPersistenceIntegrationTests,
TestNavigationComposition}.cs`, `Frontend/Shared/SharedShellTests.cs`,
`Persistence/*`, `Auth/*`, `AdministrationEndpointTests.cs`, `TechnicalEndpointTests.cs`.

### 4.8 Exhaustive absence search performed

```text
grep -rniE "jobon|job on|job-on|boquilha|peso|pegamento|resumo|controlo|ferramenta|
            armaz|reparac|tampo|historia|história|producao|produção|cm_id|mf_id|bq_id"
       src --include=*.cs --include=*.cshtml
→ only 5 files match, all vocabulary/presentation-level:
  ModuleCatalog.cs, ModuleId.cs, ModuleSurfaceDescriptor.cs,
  TemplateAdministrationValidator.cs (a comment), ModuleAuthorizationPolicies.cs (XML doc)
```

No operational entity, page, endpoint, service, route or table exists. `find src/DMO.Web/Pages`
returns only the root/Login/AccessDenied/Administration pages. There is no `.github/`
directory (no CI workflow).

---

## 5. Capability Reconciliation Matrix

Status values: **COMPLETE — PRESERVE**, **PARTIAL**, **DIVERGENT**, **MISSING**,
**UNAUTHORIZED**, **AUTHORITY AMBIGUITY**.
Gap types: **F**=Functional, **P**=Presentation, **W**=Wiring, **D**=Data-contract,
**C**=Authority conflict, **M**=current-implementation mismatch.

### 5.1 Foundation (Beta-declared dependency, not an operational module)

| Domain / Capability | Beta Authority | Current Implementation | Status | Gap Type | Required Action |
|---|---|---|---|---|---|
| Authentication + account resolution (ADMIN email, USER company_number) | `BETA_SCOPE.md` (shared foundations); `architecture/ACCESS_AND_NAVIGATION.md` access vocabulary | `Authentication/*`, `Accounts/AccountResolver.cs`, `Auth/SupabaseAuthenticationService.cs`, `Auth/SessionLoginService.cs`, `Endpoints/AuthEndpoints.cs` | COMPLETE — PRESERVE | — | PRESERVE |
| Dedicated ADMIN vs USER boundary; no ADMIN super-user | `implementation/CURRENT_FOUNDATION.md` P1-T05; `ACCEPTANCE_MATRIX.md` §2 | `Accounts/AccountType.cs`, `Authorization/AdminAuthorizationHandler.cs`, `Authorization/ModuleAuthorizationHandler.cs` (ADMIN → `context.Fail()`) | COMPLETE — PRESERVE | — | PRESERVE |
| Template-based Module access; one nullable `users.template_id` | `architecture/APPLICATION_FOUNDATION.md`; `ACCEPTANCE_MATRIX.md` §1 | `Migrations/…AccountAndTemplateFoundation.cs` (`users.template_id` FK, no membership table), `Repositories/IUserRepository.SetTemplateAsync` | COMPLETE — PRESERVE | — | PRESERVE |
| Module Registry + canonical module vocabulary | `implementation/CURRENT_FOUNDATION.md` P1-T04; `dmo-master/global/ACCESS_MODEL.md` §1 | `Access/ModuleCatalog.cs` (13 entries, exact IDs and destinations), `Access/ModuleRegistry.cs` | COMPLETE — PRESERVE | — | PRESERVE |
| Fail-closed access resolution (no partial grants) | `ACCEPTANCE_MATRIX.md` §2, §9 | `Access/AccessResolver.cs` whole-resolution denial on Unknown/Unavailable; `AccessOutcome.Denied` + `AccessDenialReason` | COMPLETE — PRESERVE | — | PRESERVE |
| Server-side Module authorization gate | `ACCEPTANCE_MATRIX.md` §2 ("hidden controls are presentation only") | `Authorization/ModuleAuthorizationPolicies.cs` (13 policies) + `ModuleAuthorizationHandler.cs` | COMPLETE — PRESERVE | — | PRESERVE |
| Template administration (composition, order, landing, concurrency) | `implementation/CURRENT_FOUNDATION.md` P1-T06 | `TemplateAdministration/*`, `Pages/Administration/Templates/*` | COMPLETE — PRESERVE | — | PRESERVE |
| USER administration (single nullable Template, no role grants) | `implementation/CURRENT_FOUNDATION.md` P1-T05 | `UserAdministration/*`, `Pages/Administration/Users/*` | COMPLETE — PRESERVE | — | PRESERVE |
| Access resolution: role label/Template name/provider claims never grant | `BETA_SCOPE.md`; `ACCEPTANCE_MATRIX.md` §2 | `NoProfilesRegressionTests`, `ModuleAccessService`, handler chain | COMPLETE — PRESERVE | — | PRESERVE |
| Operational Module availability registration | `architecture/ACCESS_AND_NAVIGATION.md` "Current build availability"; `ACCEPTANCE_MATRIX.md` §9 | `ModuleRegistrations.CurrentBuildAvailable = []` (deliberately honest) | COMPLETE — PRESERVE | — | PRESERVE (do not pre-register) |

### 5.2 Navigation / landing / routing

| Domain / Capability | Beta Authority | Current Implementation | Status | Gap Type | Required Action |
|---|---|---|---|---|---|
| Root `GET /` account-aware routing | `architecture/ACCESS_AND_NAVIGATION.md` "Root behavior" | `Pages/Index.cshtml.cs` (None→/Login, Admin→/Administration, User→landing or /AccessDenied) | COMPLETE — PRESERVE | — | PRESERVE |
| Landing selection rules | `architecture/ACCESS_AND_NAVIGATION.md` "Landing behavior" | `Navigation/LandingSelector.cs`, `Navigation/UserLandingService.cs` | COMPLETE — PRESERVE | — | PRESERVE |
| Invalid persisted landing fails closed (no rewrite) | same | `LandingSelector.InvalidExplicitLanding` → `UserLanding.NoAccess` | COMPLETE — PRESERVE | — | PRESERVE |
| Navigation = projection (granted ∩ available ∩ non-contextual ∩ routed) | `architecture/ACCESS_AND_NAVIGATION.md` "Runtime destination validity" | `Frontend/Shell/NavigationProjectionService.cs` | COMPLETE — PRESERVE | — | PRESERVE |
| Shared visible destinations collapse without merging grants | `architecture/ACCESS_AND_NAVIGATION.md` "Shared visible destinations" | `NavigationProjectionService.CreateDestination` groups by `DestinationId`, retains `GrantedModuleIds` (`ShellPresentationModels.cs`) | COMPLETE — PRESERVE | — | PRESERVE |
| Ferramentas / Ferramentas Approve never top-level | `architecture/ACCESS_AND_NAVIGATION.md` "Contextual-only Ferramentas" | `ModuleCatalog` (`DestinationId = null`) + projection `!Surface.IsContextualOnly` filter | COMPLETE — PRESERVE | — | PRESERVE |
| Direct-route enforcement server-side | `ACCEPTANCE_MATRIX.md` §9 | `DirectRouteEnforcementTests` (12 cases) via real policies + real handlers | COMPLETE — PRESERVE | — | PRESERVE |
| Destination route registration seam | `implementation/BETA_INTEGRATION_SEAMS.md` "Navigation/route-registration seam" | `Frontend/Shell/DestinationRoutes.cs` (`EmptyDestinationRouteRegistry`), `Navigation/DestinationRouteRegistrations.cs` (documentation seam, zero members) | COMPLETE — PRESERVE | — | PRESERVE (extend when a feature ships) |
| No provisional/fake production destinations | `ACCEPTANCE_MATRIX.md` §1 ("Production fake routes/fixtures: None") | `SharedShellTests.Shell_ProductionOutput_DoesNotContainProvisionalContractMarker` | COMPLETE — PRESERVE | — | PRESERVE |

### 5.3 Workstream A — shared frontend

| Domain / Capability | Beta Authority | Current Implementation | Status | Gap Type | Required Action |
|---|---|---|---|---|---|
| Shared shell / layout / identity regions | `IMPLEMENTATION_MODEL.md` Workstream A; `contracts/SHARED_FRONTEND.md` | `Pages/Shared/_Layout.cshtml`, `_Identity.cshtml`, `ShellPresentationService`, `dmo-tokens.css`, `dmo-shell.css`, `dmo-user-shell.css` | COMPLETE — PRESERVE | — | PRESERVE |
| A1 shared presentation contract published & frozen | `ACCEPTANCE_MATRIX.md` §1 ("A1 shared frontend contract: Accepted/frozen and preserved") | `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` | COMPLETE — PRESERVE | — | PRESERVE |
| Common async state vocabulary | `contracts/SHARED_FRONTEND.md` §"Common state vocabulary" | contract only (no component code) | PARTIAL | F/P | RECOVER PRESENTATION (implement with first consumer) |
| `ProductionContextStrip` | `IMPLEMENTATION_MODEL.md`; `contracts/SHARED_FRONTEND.md` §6 | contract only | MISSING | F | ADD MISSING (implement in A) |
| `ToolPicker` | same §7 | contract only | MISSING | F | ADD MISSING (implement in A) |
| `ToolSummaryRow` | same §8 | contract only | MISSING | F | ADD MISSING |
| `DenseDataTable` | same §9 | contract only | MISSING | F | ADD MISSING |
| `RecordStatus` | same §10 | contract only | MISSING | F | ADD MISSING |
| `AvailabilityState` | same §11 | contract only | MISSING | F | ADD MISSING |
| `AuditTrail` | same §12 | contract only | MISSING | F | ADD MISSING |
| `MeasurementRows` | same §13 | contract only | MISSING | F | ADD MISSING |
| `DecisionBar` | same §14 | contract only | MISSING | F | ADD MISSING |
| Secondary/local navigation pattern | `workbench/BETA_FRONTEND_IMPLEMENTATION_WORKSTREAMS.md` §4 (A scope) | `ShellPresentationModels.SecondaryDestinationPresentation`, `_SecondaryNavigation.cshtml`; **no producer** populates it (`ShellPresentationService` always passes `[]`) | PARTIAL | W | COMPLETE WIRING (when B–E supply local nav) |
| Current-destination marking | implicit presentation need | `IsCurrent` exists on both presentation records and is honored by both partials, but **no producer sets it** | PARTIAL | W | COMPLETE WIRING |

### 5.4 Workstream B — Job On Light + Ferramentas Light

| Domain / Capability | Beta Authority | Current Implementation | Status | Gap Type | Required Action |
|---|---|---|---|---|---|
| Job On Light create/view/edit | `modules/JOB_ON_LIGHT.md` | none (no entity/page/route/service) | MISSING | F | ADD MISSING |
| Job On Copy/duplicate → new `jobon_id` + new context IDs | `modules/JOB_ON_LIGHT.md` "Duplicate workflow" | none | MISSING | F | ADD MISSING |
| Reference → productions → record history | `modules/JOB_ON_LIGHT.md`; `IMPLEMENTATION_MODEL.md` B | none | MISSING | F | ADD MISSING |
| Optional CM/MF/BQ context association via `tool_id` | `modules/JOB_ON_LIGHT.md` | none | MISSING | F | ADD MISSING |
| Job On read projections (Controlo/Boquilhas/documents) | `architecture/CROSS_MODULE_FLOWS.md` | none | MISSING | F | ADD MISSING |
| Job On View vs Job On Create separation | `architecture/ACCESS_AND_NAVIGATION.md`; `dmo-master/global/ACCESS_MODEL.md` §8 | Module identities + server gate exist; no feature action | PARTIAL | F/W | COMPLETE WIRING with B |
| Ferramentas Light search/list/detail | `modules/FERRAMENTAS_LIGHT.md` | none | MISSING | F | ADD MISSING |
| Explicit Tool select/create → canonical `tool_id` return | `modules/FERRAMENTAS_LIGHT.md` | none | MISSING | F | ADD MISSING |
| Ferramentas contextual-only (no top-level route) | `modules/FERRAMENTAS_LIGHT.md` "Contextual, not top-level" | enforced by `ModuleCatalog` + projection | COMPLETE — PRESERVE | — | PRESERVE |
| Tool master data / `tool_id` identity | `modules/FERRAMENTAS_LIGHT.md`; global `TOOL_IDENTITY.md` | none | MISSING | F/D | ADD MISSING |

### 5.5 Workstream C — Controlo Create

| Domain / Capability | Beta Authority | Current Implementation | Status | Gap Type | Required Action |
|---|---|---|---|---|---|
| Peso draft/edit/calculation/submit (same `peso_id`) | `modules/CONTROLO_CREATE.md` "Peso" | none | MISSING | F | ADD MISSING |
| Peso `peso_id → cm_id` and "Job On por associar" pending case | `modules/CONTROLO_CREATE.md` "Pending association case" | none | MISSING | F/D | ADD MISSING |
| Water correction/density and glass-weight formulas | `modules/CONTROLO_CREATE.md` "Calculation ownership" | none | MISSING | F | ADD MISSING |
| Variable measurement rows (≥1 valid row) | `modules/CONTROLO_CREATE.md` "Measurement rows" | none | MISSING | F | ADD MISSING |
| Comparação with explicit `previous_peso_id`; stale rebuild | `modules/CONTROLO_CREATE.md` "Comparison" | none | MISSING | F | ADD MISSING |
| Pegamentos (CM/BQ/MF sections, nominal, `NotEvaluable`) | `modules/CONTROLO_CREATE.md` "Pegamentos" | none | MISSING | F | ADD MISSING |
| Folha (`controlo_sheet_id`) and Resumo (`resumo_id`) as distinct persisted records | `modules/CONTROLO_CREATE.md` "Folha and Resumo" | none | MISSING | F/D | ADD MISSING |
| Shared Peso renderer/read model published by C for D | `IMPLEMENTATION_MODEL.md`; `implementation/BETA_INTEGRATION_SEAMS.md` "C → D seam" | none | MISSING | F | ADD MISSING (D hard-depends) |
| Create-side history/document projections | `modules/CONTROLO_CREATE.md` "Included in Beta" | none | MISSING | F/P | ADD MISSING |
| Controlo Create distinct assignable module sharing `controlo` destination | `dmo-master/global/ACCESS_MODEL.md` §9 | identity + shared destination + independent gate exist | COMPLETE — PRESERVE | — | PRESERVE |

### 5.6 Workstream D — Controlo Approve

| Domain / Capability | Beta Authority | Current Implementation | Status | Gap Type | Required Action |
|---|---|---|---|---|---|
| Pending list / filters | `modules/CONTROLO_APPROVE.md` | none | MISSING | F | ADD MISSING |
| Review of the exact submitted Peso (no approval copy) | `modules/CONTROLO_APPROVE.md` "Identity and persistence" | none | MISSING | F/D | ADD MISSING |
| approve / reject / reopen on same `peso_id` | `modules/CONTROLO_APPROVE.md` | none | MISSING | F | ADD MISSING |
| Per-CM decisions; Folha decision; decision/audit history | `modules/CONTROLO_APPROVE.md` | none | MISSING | F | ADD MISSING |
| Explicit confirmed send-to-production action | `modules/CONTROLO_APPROVE.md` | none | MISSING | F | ADD MISSING |
| Independent Approve authorization from Create | `ACCEPTANCE_MATRIX.md` §6 | two Module identities + `SiblingModule_SameDestination_DoesNotSatisfyGate` test | COMPLETE — PRESERVE | — | PRESERVE |

### 5.7 Workstream E — Boquilhas

| Domain / Capability | Beta Authority | Current Implementation | Status | Gap Type | Required Action |
|---|---|---|---|---|---|
| Production-linked Boquilhas (`boquilhas_id → bq_id → jobon_id + tool_id`) | `modules/BOQUILHAS.md` | none | MISSING | F/D | ADD MISSING |
| Standalone Boquilhas (`boquilhas_id → tool_id`, no fake Job On) | `modules/BOQUILHAS.md` | none | MISSING | F/D | ADD MISSING |
| Four movement types only (Início/Saída/Entrada/Irreparável); `Editar` is an action | `modules/BOQUILHAS.md` "Movement vocabulary" | none | MISSING | F | ADD MISSING |
| Edit with before/after audit, no double balance effect | `modules/BOQUILHAS.md` "Edit/audit" | none | MISSING | F | ADD MISSING |
| Derived balance (no second mutable balance authority) | `modules/BOQUILHAS.md` "Balance" | none | MISSING | F/D | ADD MISSING |
| `business_date` vs `recorded_at` | `modules/BOQUILHAS.md` "Business date vs audit timestamp" | none | MISSING | F | ADD MISSING |
| Close/reopen on same `boquilhas_id` | `modules/BOQUILHAS.md` "Close/reopen" | none | MISSING | F | ADD MISSING |
| Canonical `repairer_id` on external Saída | `modules/BOQUILHAS.md` "Repairer" | none | MISSING | F/D | ADD MISSING |
| History (filters/table) | `modules/BOQUILHAS.md` "History" | none | MISSING | F | ADD MISSING |

### 5.8 Documents / PDF / cross-cutting

| Domain / Capability | Beta Authority | Current Implementation | Status | Gap Type | Required Action |
|---|---|---|---|---|---|
| Structured record is truth; path/filename never identity | `contracts/DOCUMENTS_AND_FILES.md` §1 | no document implementation exists; `README.md` states the separation as an intended rule | MISSING | F | ADD MISSING |
| Peso/Pegamentos/Resume naming + `<reference>/<production-number>/` | `contracts/DOCUMENTS_AND_FILES.md` §2, §3 | none | MISSING | F | ADD MISSING |
| Availability states (`Disponível`/`Ainda não gerado`/… ) | `contracts/DOCUMENTS_AND_FILES.md` §5 | none | MISSING | F/P | ADD MISSING |
| No local filesystem paths printed in PDFs | `contracts/DOCUMENTS_AND_FILES.md` §8 | none | MISSING | F | ADD MISSING |
| Beta anti-inference invariants (no auto-selection, warnings ≠ decisions) | `ACCEPTANCE_MATRIX.md` §2; `WORKFLOW.md` "Anti-invention rules" | no operational code exists to violate them; foundation honors them (no auto classification, role labels grant nothing) | COMPLETE — PRESERVE | — | PRESERVE (as a standing constraint) |

---

## 6. COMPLETE — PRESERVE

Everything in this section is already materially correct against Beta authority and **must
not be rebuilt, refactored or re-implemented** by future Beta work.

### 6.1 Canonical 13-module vocabulary and registry
- **Authority:** `dmo-master/global/ACCESS_MODEL.md` §1 ("Exactly 13 assignable modules exist
  in this contract", with exact names and visible destinations); `implementation/CURRENT_FOUNDATION.md` P1-T04.
- **Implementation:** `src/DMO.Application/Access/ModuleCatalog.cs` — 13 entries in canonical
  order with matching `DestinationId` values (`job-on`, `controlo`, `reparacao-interna`,
  `boquilhas`, `armazem`, `reparacao-programada`, `tampoes`, `historia`, and `null` for
  Ferramentas/Ferramentas Approve). `ModuleRegistry.cs` validates identity membership,
  destination/contextual consistency and duplicate-free registration.
- **Preserve because:** identities are stable, code-owned, and Beta requires that no
  Beta-only replacement IDs be introduced. Renaming/renumbering would break the access model,
  the persisted `template_modules.module_id` values and the Beta's identity contract.

### 6.2 Fail-closed whole-resolution access
- **Authority:** `architecture/ACCESS_AND_NAVIGATION.md` (fail closed); `ACCEPTANCE_MATRIX.md` §2, §9.
- **Implementation:** `AccessResolver.ResolveAccessAsync` returns `Denied` for
  `NotOperationalUser`, `NoTemplate`, `TemplateMissing`, `UnknownModule`, `UnavailableModule`,
  `ResolutionFailure`; `ModuleAccessService.HasModuleAsync` returns `false` for any denied
  outcome (no partial sibling survival). Verified by `AccessResolverTests`,
  `ModuleAccessServiceTests`, `ModuleRegistryTests`.
- **Preserve because:** Beta's access/navigation gate and every future module action depend on
  this exactly; softening it would silently grant access.

### 6.3 Server-side Module authorization projection
- **Authority:** `ACCEPTANCE_MATRIX.md` §2 ("all direct routes are protected server-side;
  hidden controls are presentation only, never enforcement").
- **Implementation:** `ModuleAuthorizationPolicies.AddModulePolicies` generates one policy
  (`dmo.module.<id>`) per catalog identity with exactly one `ModuleAuthorizationRequirement`;
  `ModuleAuthorizationHandler` resolves real effective access via `IModuleAccessService` and
  never reads claims/roles/Template names. Proven by `DirectRouteEnforcementTests`.
- **Preserve because:** this is the enforcement boundary every Beta route/action will reuse.

### 6.4 ADMIN/USER separation with no ADMIN super-user
- **Authority:** `implementation/CURRENT_FOUNDATION.md` P1-T05; `architecture/APPLICATION_FOUNDATION.md`
  ("ADMIN is not an operational USER super-role").
- **Implementation:** `AdminAuthorizationHandler` allows only `CurrentAccount.Admin`;
  `ModuleAuthorizationHandler` fails for anything that is not `CurrentAccount.User`;
  `AccessResolver` denies `AccountResolution.Admin` with `NotOperationalUser`.
- **Preserve because:** Beta §2 global invariants explicitly forbid ADMIN implicit operational
  super-user behavior.

### 6.5 Role labels / Template names / provider claims never grant
- **Authority:** `ACCEPTANCE_MATRIX.md` §2; `dmo-master/global/ACCESS_MODEL.md` §5.
- **Implementation:** `UserAccount.RoleLabel` is presentation-only; `AccountResolver` never
  classifies by role/email/claim; `NoProfilesRegressionTests.ProfileLabels_NeverGrant` and
  `SiblingModule_SameDestination_DoesNotSatisfyGate` prove it.
- **Preserve because:** these are explicit Beta invariants and the tests encode them.

### 6.6 Template foundation (composition, order, landing, concurrency, delete semantics)
- **Authority:** `architecture/APPLICATION_FOUNDATION.md` "Template foundation";
  `implementation/CURRENT_FOUNDATION.md` P1-T06.
- **Implementation:** `users.template_id` single nullable FK (verified in
  `20260922001736_AccountAndTemplateFoundation.cs`; no membership table);
  `template_modules` PK `(template_id, module_id)` + unique `(template_id, presentation_order)`;
  `TemplateAdministrationValidator` (unavailable/unknown modules surfaced, never silently
  repaired; landing must be represented); `ITemplateRepository.DeleteWithMembersAsync` atomic
  null-out + delete that never cascade-deletes USER rows.
- **Preserve because:** Beta requires exactly this model and forbids per-user overrides and
  membership tables.

### 6.7 Root routing, landing and no-access behavior
- **Authority:** `architecture/ACCESS_AND_NAVIGATION.md` "Root behavior" / "Landing behavior".
- **Implementation:** `Pages/Index.cshtml.cs`, `Navigation/LandingSelector.cs`,
  `Navigation/UserLandingService.cs`, `Pages/AccessDenied.cshtml.cs` (403 + generic content,
  no reason disclosure, inside the shared shell).
- **Preserve because:** the Beta landing/root contract is settled and the invalid-explicit-landing
  fail-closed decision must not be re-litigated.

### 6.8 Navigation projection and contextual-only Ferramentas
- **Authority:** `architecture/ACCESS_AND_NAVIGATION.md`; `modules/FERRAMENTAS_LIGHT.md` "Contextual, not top-level".
- **Implementation:** `NavigationProjectionService` (granted ∩ available ∩ non-contextual ∩
  routed, grouped by `DestinationId`, no provisional fixtures);
  `EmptyDestinationRouteRegistry`.
- **Preserve because:** Beta forbids a second navigation composer, fake production routes and
  top-level Ferramentas. `SharedShellTests` and `NavigationProjectionServiceTests` guard it.

### 6.9 P1-T07 login orchestration and public login surface
- **Authority:** `architecture/ACCESS_AND_NAVIGATION.md`; `dmo-master/global/ACCESS_MODEL.md` §4
  (ADMIN `email+password`; USER `company_number+password`).
- **Implementation:** `SessionLoginService` (single orchestration shared by `POST /auth/login`
  and `/Login`), `Pages/Login.cshtml`, generic indistinguishable failure message, no `ReturnUrl`.
- **Preserve because:** it is the accepted behavior-preserving extraction and the only login
  path; duplicating it would create a second auth model.

### 6.10 Administration surfaces (ADMIN-only, real destinations only)
- **Authority:** `implementation/CURRENT_FOUNDATION.md` P1-T05/P1-T06; `ACCEPTANCE_MATRIX.md` §1.
- **Implementation:** `AdministrationAuthorizationPolicies.PolicyName = "dmo.administration"`
  (deliberately outside `dmo.module.*`), applied to all 11 Administration pages; the Index page
  advertises only the two real destinations (Users, Templates).
- **Preserve because:** Beta treats Admin as a foundation dependency, not an operational module;
  the gate and Admin-only posture are accepted.

### 6.11 A1 shared frontend contract freeze
- **Authority:** `ACCEPTANCE_MATRIX.md` §1; `contracts/SHARED_FRONTEND.md` (consolidation of it).
- **Implementation:** `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` (published, accepted,
  consolidated into Beta `contracts/SHARED_FRONTEND.md`).
- **Preserve because:** A must implement to this frozen contract; re-freezing it would force
  coordinated changes across all B–E consumers.

### 6.12 Honest build availability (`CurrentBuildAvailable = []`)
- **Authority:** `architecture/ACCESS_AND_NAVIGATION.md` "Current build availability";
  `ACCEPTANCE_MATRIX.md` §9 ("Mark a Module current-build available only when the real feature ships").
- **Implementation:** `ModuleRegistrations.cs` is empty with the reason documented in-file.
- **Preserve because:** it is the exact honest state Beta requires. Must stay empty until a real
  feature surface is registered — pre-registering to "make navigation look right" is explicitly forbidden.

---

## 7. PARTIAL

### 7.1 Workstream A shared feature components (contract without implementation)
- **Exists:** the A1 presentation contract is frozen and published
  (`docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md`), with the generic shell,
  identity region, primary navigation, tokens/CSS and the common async state vocabulary.
- **Missing:** no implementation code for `ProductionContextStrip`, `ToolPicker`,
  `ToolSummaryRow`, `DenseDataTable`, `RecordStatus`, `AvailabilityState`, `AuditTrail`,
  `MeasurementRows`, `DecisionBar`. Verified by inspecting `src/DMO.Web/Frontend/`
  (only `Shell/` and `Shared/SharedFrontendExtensions.cs` exist) and
  `src/DMO.Web/Pages/Shared/` (only `_Layout`, `_PublicLayout`, `_Identity`, `Navigation/*`).
- **Must not touch:** `SharedFrontendExtensions`, `ShellPresentationService`,
  `NavigationProjectionService`, `_Layout`, `_Identity`, `_PrimaryNavigation`,
  `dmo-tokens.css`, `dmo-shell.css` — the A-owned files that are already accepted.

### 7.2 Secondary/local navigation and current-destination marking
- **Exists:** `SecondaryDestinationPresentation` and `IsCurrent` on both presentation records;
  `_SecondaryNavigation.cshtml` renders the list; `_PrimaryNavigation.cshtml` honors
  `IsCurrent` (`class="is-current"`, `aria-current="page"`).
- **Missing:** no producer. `ShellPresentationService.BuildAsync` always passes
  `[]` for `SecondaryNavigation` and never sets `IsCurrent`.
- **Must not touch:** the existing partials and record shapes; the missing piece is a producer
  owned by the consuming feature surfaces.

### 7.3 Access identities for operational modules exist but no feature action does
- **Exists:** `JobOnView`/`JobOnCreate`, `ControloCreate`/`ControloApprove`, `Boquilhas`,
  `Ferramentas`/`FerramentasApprove`, etc., with independent server-side policies that fail
  closed in production because nothing is available.
- **Missing:** the actual surfaces/actions behind them.
- **Must not touch:** the module identities, the shared-destination collapse, or the requirement
  that availability stays unregistered until a real surface exists.

---

## 8. DIVERGENT

No **operational** divergence from Beta authority was found, because no operational Beta
implementation exists to diverge. The items below are documentation/architecture bookkeeping
mismatches only; none requires a redesign.

### 8.1 Stale `src/DMO.Application/README.md`
- **Beta requirement / current intent:** `implementation/CURRENT_FOUNDATION.md` records
  P1-T04 → P1-T07 (`Module Registry`, `Access Resolver`, `Template administration`,
  `USER administration`, `navigation/landing`) as implemented; `DMO.Application` contains
  `Access/`, `Accounts/`, `Authentication/`, `Session/`, `TemplateAdministration/`,
  `UserAdministration/`, `Templates/`, `Repositories/`.
- **Current behavior:** `src/DMO.Application/README.md` still says under P1-T01 status:
  "Not implemented here, by task scope: Users, Templates, Module Registry behaviour, access
  resolution; authentication workflows".
- **Exact divergence:** the README contradicts the implemented and documented state.
- **Likely correction boundary:** documentation-only edit to `src/DMO.Application/README.md`.

### 8.2 Stale `src/DMO.Infrastructure/README.md`
- **Current behavior:** the README lists only P1-T01 contents and says "no Phase 1 product
  tables; no entity classes for USER, ADMIN, Template". In reality
  `src/DMO.Infrastructure/Persistence/Entities/` contains `AdminAccountEntity`, `UserEntity`,
  `TemplateEntity`, `TemplateModuleEntity`, plus migrations 001/002.
- **Exact divergence:** the README describes a pre-P1-T03 state.
- **Likely correction boundary:** documentation-only edit to `src/DMO.Infrastructure/README.md`.

### 8.3 Stale `src/DMO.Web/README.md` row
- **Current behavior:** the contents table lists "Production P1-T02 account lookup (fail closed)
  | `Resolution/UnavailableAccountLookup.cs`", but `src/DMO.Web/Resolution/` no longer exists
  (P1-T03 replaced it with `DMO.Infrastructure/Persistence/PersistenceAccountLookup.cs`).
- **Exact divergence:** the README references a removed file. (For contrast, `docs/ARCHITECTURE.md`
  explicitly notes "P1-T03 replaces it, not resolver semantics".)
- **Likely correction boundary:** documentation-only edit to `src/DMO.Web/README.md`.

---

## 9. MISSING

Every entry below was proven absent by direct inspection; the search evidence is given.
All of these are **Beta-authority-backed functional gaps**, i.e. they belong to Workstreams B–E
or to the document/PDF layer.

| # | Missing capability | Authority source | Expected behavior | Search evidence showing absence |
|---|---|---|---|---|
| 9.1 | Job On Light create/view/edit | `modules/JOB_ON_LIGHT.md` "Included in Beta" | Simplified Beta Job On sheet over a real `jobon_id` | No `jobon`/`jobon_id` occurrence in `src/` outside `ModuleCatalog` metadata; no Job On page/route/entity/service |
| 9.2 | Job On duplicate (new `jobon_id` + new cm/mf/bq contexts) | `modules/JOB_ON_LIGHT.md` "Duplicate workflow" | Assisted creation, source preserved, explicit source choice | none |
| 9.3 | Reference → productions listing | `modules/JOB_ON_LIGHT.md` "Search by reference" | Query productions for a reference, explicit selection | none |
| 9.4 | Ferramentas Light contextual Tool ficha (search/list/detail/create) | `modules/FERRAMENTAS_LIGHT.md` | Canonical `tool_id` select/create, return to origin | No `tool_id`, Tool entity, page or service in `src/` |
| 9.5 | Tool canonical identity / Tool master data | `modules/FERRAMENTAS_LIGHT.md`; global `TOOL_IDENTITY.md` | One canonical Tool registry, different lot = different Tool | No Tool table/entity; only the 4 foundation entities exist |
| 9.6 | Controlo Create: Peso draft/measurement/calculation/submit | `modules/CONTROLO_CREATE.md` | Same `peso_id` through the lifecycle | No `peso` occurrence in `src/`; no Controlo page/route/service |
| 9.7 | Peso "Job On por associar" pending case | `modules/CONTROLO_CREATE.md` "Pending association case" | `peso_id → tool_id` with pending display; explicit later association | none |
| 9.8 | Comparação with explicit `previous_peso_id` and stale rebuild | `modules/CONTROLO_CREATE.md` "Comparison" | Human-selected prior Peso; stale must be rebuilt | none |
| 9.9 | Pegamentos (sections, nominal, tolerance corridor, `NotEvaluable`) | `modules/CONTROLO_CREATE.md` "Pegamentos" | Persisted `pegamentos_id`; no invented nominal | No `pegamento` occurrence in `src/` |
| 9.10 | Folha (`controlo_sheet_id`) and Resumo (`resumo_id`) as distinct records | `modules/CONTROLO_CREATE.md` "Folha and Resumo"; `architecture/RECORD_LIFECYCLES.md` §7–8 | Two distinct persisted Controlo records | No `resumo`/`controlo_sheet` occurrence in `src/` |
| 9.11 | Shared Peso renderer/read model published by C | `implementation/BETA_INTEGRATION_SEAMS.md` "C → D seam" | One canonical read-only Peso representation consumed by D | none |
| 9.12 | Controlo Approve pending list / filters / review / approve / reject / reopen | `modules/CONTROLO_APPROVE.md` | Review the exact submitted `peso_id`; no approval copy | none |
| 9.13 | Boquilhas aggregate / four movements / edit-audit / balance / close-reopen / History | `modules/BOQUILHAS.md` | Append-only movement facts, derived balance, same `boquilhas_id` | No `boquilha`/`movement` occurrence in `src/` |
| 9.14 | Boquilhas repairer directory (`repairer_id`) | `modules/BOQUILHAS.md` "Repairer" | Canonical repairer stored on external Saída | none |
| 9.15 | Document/PDF generation + availability states + directory convention | `contracts/DOCUMENTS_AND_FILES.md` | `<reference>/<production-number>/`, settled filenames, distinct availability states | No `Pdf`/`Files` project, service or page; `DMO.Domain` has no types |
| 9.16 | Operational Module availability registrations | `implementation/BETA_INTEGRATION_SEAMS.md` "Navigation/route-registration seam" | Register a Module only when its real surface ships | `ModuleRegistrations.CurrentBuildAvailable = []` |
| 9.17 | Real destination routes | same | Register each real feature route | `EmptyDestinationRouteRegistry` is the production registration; `DestinationRouteRegistrations` has zero members |

**Dependencies between the missing items.** 9.1–9.5 (B) gate 9.6–9.11 (C) and 9.13 (E),
because Peso/Boquilhas consume Job On production context and canonical `tool_id`. 9.11 (C's
shared Peso read model) gates 9.12 (D). 9.15 depends on 9.1 (Job On context) for its directory
convention. 9.16–9.17 are per-feature wiring that can only follow each feature.

**Backend/interface blocker note.** Beta `INDEX.md` and `WORKFLOW.md` require that where live
backend behavior is not defined/published, the slice is recorded as a
`BACKEND / INTERFACE BLOCKER` rather than invented. No concrete Peso/Boquilhas/Job On
schema/query/endpoint contract has been published in Beta authority at the reconciled SHA;
those contracts must be authorized separately before the corresponding implementation.

---

## 10. UNAUTHORIZED CURRENT BEHAVIOR

Two presentation/wiring elements exist with no Beta authority establishing them. Neither is a
behavioural grant, and **neither should be deleted on the basis of this report**.

### 10.1 `Pages/Administration/Index.cshtml` — "Terminar sessão" form inside the Admin destination grid
- **What exists:** an authenticated logout `POST /auth/logout` form rendered inside
  `Administration/Index.cshtml`.
- **Authority status:** no Beta document defines an Admin landing-page logout control. It is not
  contradicted by Beta — `POST /auth/logout` itself is an accepted existing endpoint — and
  `Pages/AccessDenied.cshtml` carries the same pattern.
- **Assessment:** ambiguous. The endpoint is accepted; only the placement/visibility of the
  control on the Admin index is not established by Beta. Not recommended for removal without an
  explicit decision.

### 10.2 `ShellPresentationService` hardcoded Portuguese status strings
- **What exists:** `"Aplicação pronta."` and
  `"A navegação operacional está indisponível. O acesso continua fechado."` are hardcoded in
  `ShellPresentationService.cs`.
- **Authority status:** the fail-closed string is required by the A2 correction
  (`workbench/dev/plans/BETA_FRONTEND_WORKSTREAM_A2_CORRECTION_PLAN.md` §6.2 test 10, and the
  accepted A2 correction review). `"Aplicação pronta."` has no explicit Beta source.
- **Assessment:** effectively authorized as a shared-shell presentation detail; recording it only
  for completeness. No action.

### 10.3 Primitive shared shell presentation slots with no current consumer
- **What exists:** `.dmo-production-slot`, `.dmo-work-surface`, `.dmo-section-heading`,
  `.dmo-action-placeholder` ("Sem ações disponíveis") in `dmo-shell.css` and `_Layout.cshtml`;
  unset `SecondaryNavigation` and `IsCurrent`.
- **Authority status:** these are presentation scaffolding inside A-owned shell files. Beta's A
  scope explicitly includes "dense operational form, table, section … action styling", so this is
  within the shell workstream's remit rather than unauthorized product behavior.
- **Assessment:** not unauthorized in a material sense; recorded so it is not mistaken for
  missing feature implementation.

> No operational (domain) functionality lacking Beta authority exists, because no operational
> functionality exists.

---

## 11. AUTHORITY CONFLICTS / AMBIGUITIES

These are contradictions or gaps **within the authority corpus**. They are recorded, not
resolved by invention.

### A1 — História: "Beta module" vs "outside Beta scope" vs global assignable module
- `BETA_SCOPE.md` §"Explicitly not implied by this scope" does **not** list História among the
  excluded future items, while it explicitly excludes Armazém, Reparação
  Interna/Externa/Programada, Tampões and "future modules not explicitly listed here".
- `BETA_SCOPE.md` §"Operational Beta modules" lists only Job On Light, Ferramentas Light,
  Controlo Create, Controlo Approve, Boquilhas.
- `architecture/ACCESS_AND_NAVIGATION.md` §"Access vocabulary" says "Relevant Beta operational
  modules include:" and lists only Job On View/Create, Controlo Create/Approve, Boquilhas,
  Ferramentas, Ferramentas Approve.
- Meanwhile `dmo-master/global/ACCESS_MODEL.md` §1 (global authority) makes História the 11th
  canonical assignable module with its own `historia` destination, and `modules/HISTORIA.md`
  (global) defines it as "a read-only operational destination **when present in the approved
  product model**" and states it owns no operational facts.
- **Recorded as:** AUTHORITY AMBIGUITY. Resolution required before any História implementation.
- **Implementation consequence today:** `ModuleCatalog.cs` already registers `historia`
  (identity + `historia` destination) because the vocabulary must be final; the Module is
  correctly **not** available (`CurrentBuildAvailable = []`) and has **no** route.
  `DestinationRouteRegistrations.cs` explicitly defers operational route registration to the
  owning workstream. Therefore no implementation mismatch exists today — only the authority
  question is open.

### A2 — `BETA_DESIGN_RECONCILIATION_PLAN.md` unavailable
- `SOURCE_MANIFEST.md` records `workbench/BETA_DESIGN_RECONCILIATION_PLAN.md` as
  `HISTORICAL REFERENCE MISSING`, and `workbench/BETA_FRONTEND_IMPLEMENTATION_WORKSTREAMS.md` §1
  names it as "settled frontend design authority". A search of `workbench/main` and
  `dmo-beta-master/main` found no such file.
- **Recorded as:** AUTHORITY AMBIGUITY / missing authority. Frontend presentation decisions that
  the plan supposedly settled cannot be verified from the available corpus; `contracts/SHARED_FRONTEND.md`
  is the available substitute.

### A3 — Canonical order of the 13 modules is stated in two different orders
- `dmo-master/global/ACCESS_MODEL.md` §1 lists: Job On View, Job On Create, Controlo Create,
  Controlo Approve, Reparação Interna, Boquilhas, Armazém, Reparação Programada View,
  Reparação Programada Create, Tampões, História, Ferramentas, Ferramentas Approve.
- `implementation/CURRENT_FOUNDATION.md` (P1-T04 accepted facts) states "exactly 13 canonical
  code-owned Module identities" and matches the same set and (from the code) the same order.
- **Assessment:** no contradiction found on re-reading; both agree. Retained here only to note
  that the catalog order is presentation/audit ordering and never an authorization factor
  (as both the code and `ARCHITECTURE` comments state).

### A4 — P1-T07 acceptance status
- `SOURCE_MANIFEST.md` records the P1-T07 implementation commit `0b476909…` as
  `IMPLEMENTED / NOT YET ACCEPTED`, and `implementation/CURRENT_FOUNDATION.md` states
  "P1-T07 implementation exists != P1-T07 accepted implementation".
- **Assessment:** not a contradiction, but a formal status gap in the authority corpus: the
  current remote `main` HEAD is an implementation awaiting Architect implementation review.
  Beta work must treat this commit as current code state, not as an accepted architectural result.
  Recorded so the next task does not silently treat the shell as accepted foundation.

### A5 — Beta Peso approval/status vocabulary
- `architecture/RECORD_LIFECYCLES.md` §4 fixes: `Pendente`, `Aprovado`, `Não aprovado`, with
  Comparação as a record type and stale as a workflow condition.
- `dmo-master/modules/CONTROLO.md` (global) describes the same lifecycle but does not enumerate
  the same three literals in one place.
- **Assessment:** compatible (Beta narrows the global contract, as `AUTHORITY.md` permits); no
  conflict requiring escalation. Recorded for completeness.

---

## 12. Backend / Data Contract Delta

Only **actual** gaps are listed, traced against the current data path. Existing entities and
relationships that already suffice are called out so no duplicate table is proposed.

### 12.1 Schema / domain
- **Currently persisted (foundation only):** `admin_accounts`, `users`, `templates`,
  `template_modules` (migrations `20260922001736_AccountAndTemplateFoundation`,
  `20260922001757_TemplateModuleComposition`). `src/DMO.Domain` contains no types.
- **Missing contracts required by Beta:** canonical `tool_id` (Tool master with type, reference,
  lot, machine/line compatibility, quantity, processo); `jobon_id` (reference, production number,
  machine, processo); `cm_id`/`mf_id`/`bq_id` contexts; `peso_id`; `pegamentos_id`;
  `controlo_sheet_id`; `resumo_id`; `boquilhas_id`; `movement_id`; `repairer_id`; document
  record/generation state.
- **Why existing entities are insufficient:** `templates`/`template_modules` model *access*, not
  operations; `users`/`admin_accounts` model *accounts*. None can represent a production
  occurrence, a Tool, a Peso result or a Boquilhas movement. No reverse-ID arrays or
  `production_id`/`revision_id` should be introduced (`contracts/IDENTITIES_AND_RELATIONSHIPS.md`
  "No reverse-ID arrays", "No fake identities").
- **Ownership rule:** per `ACCEPTANCE_MATRIX.md` §10 and `architecture/APPLICATION_FOUNDATION.md`,
  each schema-bearing feature needs one justified owner/workstream and a separate authorized
  backend task. No table may be created for UI symmetry.

### 12.2 Services / application orchestration
- **Existing:** `AccountResolver`, `AccessResolver`, `ModuleAccessService`,
  `TemplateAdministrationService`, `UserAdministrationService`, `SessionLoginService`,
  `UserLandingService`, `NavigationProjectionService`, `ShellPresentationService`.
- **Missing:** Tool query/create; Job On select/create/context read;
  CM association/read; Peso create/read/update/calculate/submit; previous-Peso candidate query +
  explicit relation persistence; Comparação pairing/build/rebuild; Pegamentos persistence/evaluation;
  Folha/Resumo persistence; Approve pending/review/approve/reject/reopen; Boquilhas
  aggregate/movement/edit-audit/balance/close-reopen/History; repairer query;
  document availability/read.
- **All are Beta-authority-backed** (`modules/*.md` "Backend contracts required") but **none has a
  published concrete contract** at the reconciled SHA → per Beta these are
  `BACKEND / INTERFACE BLOCKER` items that require separate authorization, not invention.

### 12.3 Persistence
- **Existing:** EF Core `DmoDbContext` over PostgreSQL, migration runner
  (`IMigrationRunner`/`EfCoreMigrationRunner`), optimistic concurrency via `version` columns.
- **Missing:** all operational persistence. No new persistence *mechanism* is needed — the
  existing context/migration/concurrency infrastructure is the correct vehicle. Beta forbids
  a second database context / generic repository abstraction (`docs/ARCHITECTURE.md`
  "Deliberately not used").

### 12.4 Authorization
- **Existing:** 13 Module policies + ADMIN-only `dmo.administration` policy; server-side
  enforcement proven by `DirectRouteEnforcementTests`.
- **Missing:** none structurally. Each new operational route/action must declare the appropriate
  `ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.<Module>)`. No new authorization model is
  permitted (`architecture/ACCESS_AND_NAVIGATION.md` "Frontend must not define a second access model").

### 12.5 API / action wiring
- **Existing:** `POST /auth/login`, `POST /auth/logout`, `GET /auth/me`, `GET /health`, plus
  administration minimal-API endpoints (`MapUserAdministrationEndpoints`,
  `MapTemplateAdministrationEndpoints`).
- **Missing:** every operational endpoint. All are gated behind the same
  backend/interface-blocker rule as 12.2.

---

## 13. Frontend Delta

### 13.1 Missing routes
None exist for any operational destination. `EmptyDestinationRouteRegistry` returns `false` for
every destination id and `DestinationRouteRegistrations` is an empty documentation seam. Required
routes (all missing): `job-on`, `controlo`, `boquilhas`, plus whatever Reparação Interna,
Armazém, Reparação Programada, Tampões and História eventually require if a workstream owns them.
No Ferramentas top-level route may ever be added.

### 13.2 Missing actions
No operational action exists anywhere: no Tool search/select/create, no Job On create/edit/
duplicate, no Peso measurement/calculation/submit, no Comparação build/rebuild, no Pegamentos/
Folha/Resumo persist, no Approve decision/reopen, no Boquilhas movement/edit/close-reopen, no
document open/regenerate.

### 13.3 Missing presentation
- The nine A shared feature components (§7.1).
- Every operational page: there is no `Pages/JobOn/**`, `Pages/Controlo/**`, `Pages/Boquilhas/**`
  or `Pages/Ferramentas/**`.
- The shared Peso sheet/read model.
- Document availability presentation (`Disponível`/`Ainda não gerado`/… sets).
- Consuming producers for `SecondaryNavigation` and `IsCurrent` (§7.2).

### 13.4 Incorrect presentation
Only within the shell and only mildly:
- `_PrimaryNavigation.cshtml` shows the literal `Sem destinos operacionais disponíveis` whenever
  there are no live destinations. This is **required** for the current honest state (A2
  correction test 8) but is not yet a per-feature empty state; it needs no change now.
- Hardcoded shell status strings (§10.2). No Beta-established presentation is contradicted.

### 13.5 Existing correct frontend work to preserve
Everything in §6.6–§6.11: the shell/layout/identity regions, tokens/CSS, the navigation
projection and route seam, the root/login/no-access surfaces, the Administration pages, and the
A1 frozen contract document. Also preserve the *absence* of provisional production fixtures —
this was a deliberate, reviewed correction (`workbench` A2 correction plan + accepted review).

---

## 14. Recommended Implementation Sequence

Dependency-aware and deliberately **additive**, so that already-completed work is disturbed as
little as possible. This is a recommendation only; nothing here is implemented.

**Phase 0 — governance (no code).**
0.1 Obtain Architect implementation review for P1-T07 (`A4`), so the shell becomes accepted
    foundation before downstream dependence.
0.2 Resolve the História ambiguity (`A1`) and attempt recovery of
    `BETA_DESIGN_RECONCILIATION_PLAN.md` (`A2`).
0.3 Correct the three stale implementation READMEs (`§8.1–8.3`) as a documentation-only change.

**Phase 1 — Workstream A completion (contract → components).**
1.1 Implement the shared presentation components against the frozen A1 contract, starting with
    the ones needed by the first consumer: `RecordStatus`, `DenseDataTable`,
    `AvailabilityState`, `ProductionContextStrip`.
1.2 Add `ToolPicker` presentation, `ToolSummaryRow`, `AuditTrail`, `MeasurementRows`, `DecisionBar`.
1.3 Add the common async state primitives. Do not modify A-owned accepted files except by an
    accepted shared-contract change.

**Phase 2 — backend/interface authorization.**
2.1 Authorize and publish the concrete backend contracts for the first slice (Tool identity +
    Job On production context + CM/MF/BQ contexts) before writing any of its UI.
2.2 Authorize Controlo (Peso/Comparação/Pegamentos/Folha/Resumo) and Boquilhas contracts.
2.3 Keep each schema change owned by exactly one workstream.

**Phase 3 — Workstream B (Job On Light + Ferramentas Light).**
3.1 Tool search/select/create orchestration returning canonical `tool_id`.
3.2 Job On create/view/edit + reference→productions listing.
3.3 Job On duplicate (new `jobon_id` + new contexts; source preserved).
3.4 Publish B's production-context adapter and its read projections.

**Phase 4 — Workstream C (Controlo Create).**
4.1 Peso create/measurement/calculation/submit on one `peso_id`; pending-association case.
4.2 Comparação; Pegamentos; Folha; Resumo.
4.3 Publish C's shared Peso read model for D.

**Phase 5 — Workstream D (Controlo Approve).**
5.1 Pending list/filters + exact-submitted-record review consuming C's read model.
5.2 approve/reject/reopen + attribution + audit.

**Phase 6 — Workstream E (Boquilhas).**
6.1 Aggregate create (production-linked and standalone) + four movements.
6.2 Balance projection, edit-audit, History, close/reopen, repairer.

**Phase 7 — integration and honest availability.**
7.1 Register each Module in `ModuleRegistrations.CurrentBuildAvailable` **only** when its real
    surface and gate exist.
7.2 Register each real destination route in the shared route seam.
7.3 Add cross-module links, document availability and the final directory/PDF naming.

**Ordering constraints.** 3 before 4 and 6; 4.3 before 5; 1 before any 3–6 UI; 2 before 3–6
backend work. Never pre-register availability, never add a top-level Ferramentas route, never
create a fake `jobon_id`/`cm_id` and never fork C's Peso renderer.

---

## 15. Explicit Non-Work

The following are already complete/correct and **must not be part of the next implementation
task** — they must be neither rebuilt, refactored, renamed nor re-registered.

1. The canonical 13-module vocabulary and `ModuleCatalog` identities/order/destinations.
2. `ModuleRegistry` validation semantics.
3. `AccessResolver` fail-closed whole-resolution behavior and `AccessOutcome`/`AccessDenialReason`.
4. `ModuleAccessService` and the 13 server-side Module policies + `ModuleAuthorizationHandler`.
5. `AdminAuthorizationHandler` and the `dmo.administration` policy; ADMIN-only Administration pages.
6. `AccountResolver`, `AccountType`, `NoAccessReason`, `CurrentAccount`, `ICurrentAccountContext`.
7. Authentication boundary, `SessionLoginService`, `SessionAuthentication`, cookie session,
   `AuthEndpoints` contracts (status codes included).
8. `users.template_id` single-relation model; no membership table; no per-user Module overrides.
9. Template composition/order/landing validation and `DeleteWithMembersAsync` atomic delete.
10. USER administration workflows, including invite/reset/reassign semantics.
11. Root routing (`GET /`), `LandingSelector`, `UserLandingService`, `/Login`, `/AccessDenied`
    (including the fail-closed invalid-explicit-landing decision).
12. `NavigationProjectionService` projection algorithm and shared-destination collapse; the
    `IDestinationRouteRegistry` seam; `EmptyDestinationRouteRegistry` in production.
13. The shared shell: `_Layout`, `_PublicLayout`, `_Identity`, `_PrimaryNavigation`,
    `_SecondaryNavigation`, `ShellPresentationModels`, `ShellPresentationService`,
    `SharedFrontendExtensions`, `dmo-tokens.css`, `dmo-shell.css`, `dmo-user-shell.css`.
14. `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md` as the frozen A1 contract.
15. `ModuleRegistrations.CurrentBuildAvailable = []` — it must stay empty until a real feature
    ships; do not populate it to make navigation appear complete.
16. Migrations 001/002 and the four foundation entities; do not add a second context, a
    generic repository abstraction or reverse-ID array tables.
17. The reviewed absence of provisional production fixtures/destinations.
18. Existing tests as regression guards (must keep passing); do not modify tests to fit new code.

---

## 16. Verification Statement

- Report path: `reports/BETA_MASTER_RECONCILIATION.md`.
- No application code, page, route, service, entity, migration, configuration or test was
  modified; `reports/` is the only added path.
- All classifications cite a file path, route, class/method, test name or authority document.
- Where a requirement could not be proven from `dmo-beta-master`, it was not added to the delta
  (§11 records the ambiguities instead).
- Where absence could not be proven from `DMO-MODULAR`, it was not classified MISSING (§4.8
  records the exhaustive search used).
- Where the architecture differs but behaviour is equivalent (e.g. modular Host/Application/
  Infrastructure split vs the Beta's conceptual model), it was **not** classified divergent.

---

## Appendix Z — Terminology Correction Addendum (HISTÓRICO vs HISTÓRICO GLOBAL)

Added after the original reconciliation. It **corrects terminology only**; it changes no
classification, no evidence and no recommendation in this report.

The report's §11 A1 (and every other occurrence of "História" / "Histórico") conflated two
concepts that must remain distinct:

1. **HISTÓRICO (local / module-specific)** — history functionality *inside* an operational
   module (Peso → Histórico, Job On → Histórico, Boquilhas → Histórico, Armazém → Histórico,
   Reparações → Histórico). It means "show the records/actions/work already performed within
   this module or operational context". It is **not** a module identity and **not** a
   top-level destination. Authority for it is per-module: `modules/BOQUILHAS.md` §"History",
   `modules/CONTROLO_CREATE.md` "Create-side history/document availability views",
   `modules/CONTROLO_APPROVE.md` "attributed decision/reopen history",
   `modules/JOB_ON_LIGHT.md` "Reference History search/open tests". This is in Beta scope and
   is owned by the corresponding workstreams (P2-T05/P2-T06/P2-T07).

2. **HISTÓRICO GLOBAL (top-level aggregating module)** — the distinct top-level module whose
   responsibility is to aggregate authority-backed operational histories across the system. Its
   current **technical identity** is `historia` (`ModuleCatalog.Historia`, display label
   "História", destination `historia`). Authority: `dmo-master/global/ACCESS_MODEL.md` §1 (the
   11th canonical assignable module) and `dmo-master/modules/HISTORIA.md` (read-only
   relationship explorer). It is **absent from Beta authority** and is **DEFERRED BY DESIGN**
   for this Beta.

**Correction to §11 A1.** The item formerly labelled "História: Beta module vs outside Beta
scope vs global assignable module" is restated as: *HISTÓRICO GLOBAL is a global assignable
module but not a Beta operational module.* Evidence for local HISTÓRICO in Beta module files
was **not** and must not be treated as authority for HISTÓRICO GLOBAL; and HISTÓRICO GLOBAL's
absence from Beta does **not** remove any Beta module's local HISTÓRICO requirement.

**Correction to §6.12 / §9 / §10 / §13 / §14 / §15 references.** Wherever this report said
"História" as the module, read **HISTÓRICO GLOBAL**. Wherever it described a module's History
view, read **HISTÓRICO (local)**.

**Naming rule.** Do not rename the code identity `historia` during Beta planning or Beta
implementation. Record that `historia` is the current technical identity and that its intended
user-facing meaning maps to HISTÓRICO GLOBAL; a technical rename, if ever required, is a
separate later task that must not break existing IDs or catalog relationships.

**No classification changes.** The original counts stand. HISTÓRICO GLOBAL remains
DEFERRED BY DESIGN with its identity preserved, no route and no availability; local HISTÓRICO
remains a per-module requirement of the operational workstreams.
