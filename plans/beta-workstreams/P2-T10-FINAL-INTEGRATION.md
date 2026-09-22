# P2-T10 — Final Integration: Availability + Routes + Navigation + End-to-End — IMPLEMENTATION HANDOFF

Master plan: `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` §7 (P2-T10), §9, §11, §12, §13.
Class: **Final integration** — incremental, **per destination**.
Depends on: whichever of **P2-T04…P2-T08** is being exposed.
Authority blocker: none beyond the destination's own workstream.

## 1. Purpose

Wire each completed destination into the application honestly: register Module availability and
the real route only when the surface exists and is gated, and verify end-to-end.

## 2. Authority

- `dmo-beta-master/implementation/BETA_INTEGRATION_SEAMS.md` — "Navigation/route-registration
  seam", "Access/action seam".
- `dmo-beta-master/architecture/ACCESS_AND_NAVIGATION.md` — "Current build availability",
  "Runtime destination validity", "Direct-route enforcement".
- `dmo-beta-master/ACCEPTANCE_MATRIX.md` §9, §12.
- `reports/BETA_MASTER_RECONCILIATION.md` §6.8, §6.12 (preserved honest state).

## 3. Current implementation starting point

- `src/DMO.Application/Access/ModuleRegistrations.cs` — `CurrentBuildAvailable = []` (honest).
- `src/DMO.Web/Frontend/Shell/DestinationRoutes.cs` — `EmptyDestinationRouteRegistry` is the
  production registration.
- `src/DMO.Web/Navigation/DestinationRouteRegistrations.cs` — empty documentation seam.

## 4. Scope (per destination that is genuinely implemented and usable)

1. Register the Module definition in `ModuleRegistrations.CurrentBuildAvailable`.
2. Register the real route in the `IDestinationRouteRegistry` seam — extend the composition;
   never create a second registry.
3. Verify `granted + available + non-contextual + routed` all hold before it appears in
   navigation.
4. Verify direct-route denial for every non-granted caller.
5. Wire cross-module links and document-availability actions.

## 5. Explicit non-scope

- Registering a Module before its surface exists.
- Creating placeholder routes/pages.
- Making Ferramentas top-level; exposing HISTÓRICO GLOBAL (see terminology note below).
- No broad refactor of the projection or registry.

## 6. Expected files/projects

```text
src/DMO.Application/Access/ModuleRegistrations.cs   (explicit, reviewable, per-destination diff)
src/DMO.Web/Navigation/DestinationRouteRegistrations.cs  (real route registration)
feature pages delivered by the owning workstream
tests/DMO.IntegrationTests/Navigation/
```

## 7. Access requirements

Unchanged model. Only availability registration changes, and only honestly.

## 8. Backend / persistence requirements

None.

## 9. Required tests

See master plan §11 P2-T10: the four-condition rule; direct-route denial; shared-destination
collapse retaining all grants; `Ferramentas` never top-level; zero-destination builds still
render `Sem destinos operacionais disponíveis`; the protected regression set stays green after
every registration step.

## 10. Acceptance criteria

1. A newly registered destination appears only when granted **and** available **and**
   non-contextual **and** routed.
2. A non-granted caller still receives the documented denial on the direct route.
3. Zero-destination builds keep the honest empty state.
4. `Ferramentas` never appears top-level.
5. HISTÓRICO GLOBAL is not exposed in this Beta.

## 11. Completion evidence

The per-destination registration commit + the passing navigation and direct-route test set.

## 12. Downstream dependents

None.

---

## Terminology note (binding)

**HISTÓRICO** (local) and **HISTÓRICO GLOBAL** (top-level, technical identity `historia`) are
different concepts and must never be conflated:

- **HISTÓRICO** = history functionality *inside* an operational module (Peso, Job On,
  Boquilhas, Armazém, Reparações), owned by that module's workstream. It is **not** a module
  identity and is **not** registered as a destination. It appears in this plan as the History
  requirement inside P2-T07 and as project-specific histories inside their owning workstreams.
- **HISTÓRICO GLOBAL** = the distinct top-level aggregating module whose current technical
  identity in `ModuleCatalog` is `historia`. It aggregates authority-backed operational
  histories. It is **DEFERRED BY DESIGN** for this Beta (see master plan §3.1 and §14.1):
  identity preserved, no route, no availability. Do not rename the code identity during Beta
  implementation; a technical rename, if ever required, is a separate task.
