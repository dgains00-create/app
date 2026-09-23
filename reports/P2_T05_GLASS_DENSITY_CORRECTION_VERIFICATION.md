# P2-T05 Post-Closure Correction — INDEPENDENT VERIFICATION — Glass Density Configuration per Processo (Owner rule)

**Workstream:** P2-T05 — Controlo_Create (post-closure correction slice).
**Verification class:** formal INDEPENDENT VERIFICATION of the focused glass-density correction
only (no re-audit of the closed P2-T05 baseline; no modification of the implementation).
**Verdict:** **VERIFIED.**

| Item | Value |
|---|---|
| Verified correction SHA | `ce516d07ab885c3da940d7cec947df411358cd05` (DMO-MODULAR `main`) |
| Remote `main` at verification | `ce516d07ab885c3da940d7cec947df411358cd05` (local HEAD == origin/main; working tree CLEAN at start and end) |
| Contract | `plans/contracts/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_CONTRACT.md` @ `1c3f36e` (+ `0436cdd` SHA record) — read completely |
| Architect PLAN review | dmo-work `dev/reviews/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_PLAN_REVIEW.md` — **PLAN ACCEPT** @ `4924982f…` — read completely |
| Implementation response | `dev/responses/P2_T05_IMPLEMENTATION_RESPONSE.md` §16 — read completely; earlier sections used only for the closed-decision context |
| Change inspected | full diff `0436cdd..ce516d0` (51 files: 14 production src, 25 tests, 2 docs, new migration 005 pair + snapshot) |

This verification ran the REAL application/migrations against a **fresh disposable PostgreSQL 16**
database (`dmo_p2t05_verify`, created and dropped for this verification — never shared
Supabase TEST), executed the full and targeted test suites independently of the implementation
run, and re-derived every structural claim from source inspection. **No implementation file was
modified by this verification.**

---

## 1. Correction scope review (diff audit) — PASS

The complete change set `0436cdd..ce516d0` was audited file-by-file:

- **Exactly the approved delta**: one settings table + migration 005 (Up/Down), one repository +
  entity + EF configuration + one scoped DI registration, two routes in the existing Definições
  group, the glass-resolution seam re-point (water-only `ConfigurationCalculationConfiguration`),
  the Definições page section + adapter wiring (+7 CSS lines for a unit-label rule), the
  implementation-response §16, and the focused tests/pins.
- **Protected files untouched** (scan of the changed-path list): no `Program.cs`,
  `ModuleRegistrations.cs`, `ModuleCatalog`, `DmoDbContext.cs`, migrations 001/002/003/004 (the
  snapshot was extended by EF; 004's `CreateTable` calls are byte-identical — BND2/BND8 green),
  no Supabase/Auth/Authorization/Navigation/appsettings files, no `Create.cshtml` (operator
  surface), no `dmo-dense-table.js`, no Tool/JobOn/identity carriers (`ControloCreateModels.cs`
  unchanged), no availability/destination/navigation registration.
- **No later workstream appears** in the new sources (GD-S1 + BND2/BND3/BND4 vocabulary scans
  green).

## 2. Legacy values / initial state — PASS

Independently re-verified directly in the legacy production tree (not taken from the response):

| Evidence | Verdict |
|---|---|
| `BA-DMO/src/BA.Dmo.Domain/Modules/Peso/PesoModuleCatalog.cs` — `public const decimal ConstantNnpb = 2.4027m;` / `ConstantPs = 2.4231m;` | exact match (read directly) |
| `BA-DMO/AI-CONTEXT/docs/technical/CONTROLO_TECHNICAL_MODEL.md` — `peso_settings` keys `constant_nnpb`/`constant_ps` (densidade do vidro), "fallback catálogo 2.4027/2.4231" | exact match (read directly) |
| `BA-DMO/src/BA.Dmo.Application/Modules/Peso/PesoService.cs` — `ResolveProcessDensityAsync` (settings parse `value > 0` wins, else catalog); `control.ConstanteGlassUsada ?? …` / `??=` (OC-6 frozen-constant-wins) | exact match (read directly) |
| `BA-DMO/src/BA.Dmo.Web/Pages/Peso/Responsavel.cshtml` — editable NNPB/PS inputs `step="0.0001"` placeholders `2,4027`/`2,4231` | exact match (read directly) |

- Migration 005 seeds **exactly two rows**: `('NNPB', 2.4027, version 1)` and `('PS', 2.4231,
  version 1)` — no invented values, exact four-decimal literals, no extra rows (the fresh-DB
  query returned exactly these two rows with populated timestamps and no others).
- **Version convention (Architect N-2):** seed rows carry `version 1`, consistent with the
  established sibling-settings convention (`version integer NOT NULL DEFAULT 1` on all four 004
  settings tables); first guarded PUT from the fresh surface sends `expectedVersion 1` → writes
  version 2 — identical stale-version behavior to every other Definições row. Documented in the
  migration comment and response §16.2. No new concurrency model exists.

## 3. Schema / migration (fresh disposable PostgreSQL) — PASS

Independent run on a fresh disposable database (`dotnet ef migrations update` end-to-end +
direct catalog queries):

- Migration `20260923122429_GlassDensitySettings` is the **fifth** migration overall (history:
  001, 002, 003, 004, 005 — exactly five rows, in generation order, applied cleanly).
- `glass_density_settings` is the **only** new table (20 public tables after, 19 before the
  correction; fresh-DB catalog).
- Physical shape (catalog queries): `processo text NOT NULL` **PK**; `density_g_cm3 numeric(18,4)
  NOT NULL`; `version integer NOT NULL DEFAULT 1`; `created_at`/`updated_at timestamptz NOT NULL
  DEFAULT now()`.
- Named constraints: `PK_glass_density_settings` (processo), `glass_density_settings_processo_check`
  (`processo IN ('NNPB','PS')`), `glass_density_settings_density_check` (`density_g_cm3 > 0`).
- **No extra index** beyond the PK (single-row index catalog entry) — per contract §5.1.
- CHECKs enforced at the database (direct INSERTs of `('TOOL', 2.5, 1)` and `('NNPB', 0, 1)` both
  rejected with the exact constraint names).
- **Down** (real `dotnet ef database update 20260923045054_ControloCreateDomain`): removes ONLY
  `glass_density_settings`; 19 tables remain; history drops to four migrations — the other
  nineteen tables and migrations 001–004 are untouched (no drift).
- **Re-apply** restores the exact initial state: 20 tables, exactly the two canonical rows at
  version 1, five migrations in history; a further re-run is a no-op (GD-M6).
- No `pesos`/`peso_measurement_rows` column change exists anywhere in the correction diff.

## 4. Settings ownership (current operational value) — PASS

- `Controlo → Definições` owns the current operational glass density per processo: the new
  settings table/repository/service surface is reached only through `ControloDefinicoesService`
  (`ListGlassDensitiesAsync` / `UpdateGlassDensityAsync`) and routes 18/19 inside the Definições
  group.
- The Tool owns **no** editable density: `ToolModels.cs`/`ToolEntity.cs`/the `tools` migration
  carry no density member/column (source scan + GD-S3); no per-`tool_id` density exists anywhere
  (GD-S3 scans the new domain/entity/migration sources).
- The new Peso resolution chain `cm_id → tool_id → processo → current glass_density_settings row
  → density_g_cm3` is implemented in `ControloCreateService.TryResolveCalculationFactsAsync`
  (reads the CURRENT row per processo; absent row (defensive only) → `null` →
  `calculation-configuration-missing`, nothing invented). Both NNPB and PS verified (GD-U1,
  GD-I1, GD-I6).

## 5. Freeze / history semantics — PASS (critical)

Code-verified and independently executed:

1. New Peso resolves the current process density at first successful calculate/save and **freezes
   it** into `pesos.glass_density_g_cm3` (write-once at create).
2. `UpdateAsync` (recalculation) uses `persisted.GlassDensityGCm3` — the FROZEN value — and the
   Peso repository writes back the Peso's own unchanged frozen value; no path re-resolves
   current settings for an existing Peso.
3. `SubmitAsync` recompute-verify re-derives with the frozen value (a current-settings re-derive
   would mismatch the stored rows and be refused).
4. Later Definições changes affect ONLY new Pesos; historical Pesos are never rewritten.

Independent proofs executed: GD-U4/GD-U5 (unit), GD-I6 (HTTP end-to-end: save → freeze 2.4027 →
PUT settings 3.00 → existing Peso keeps frozen 2.4027 and rows through edit + submit → new Peso
freezes 3.00), DB MES10 (real repositories: seeded 2.4027/v1 → settings write 3.00/v2 → existing
Peso keeps frozen value and rows 2414.7135 → new Peso 3.00/3015.0000). All green. The
distinction **current configuration ≠ historical frozen Peso fact** holds (R6).

## 6. Calculation source change — PASS

- `IControloCalculationConfiguration` no longer declares `TryGetGlassDensity` (it is WATER-only),
  and `ConfigurationCalculationConfiguration` binds only `Controlo:Calculation:WaterDensities`
  (source scan). **`Controlo:Calculation:GlassDensities` has no code reader** in the whole
  `src` tree — the literal appears only in three doc comments (GD-S2 comment-aware scan green).
  There is no competing runtime source for glass density; the table row is the single source.
- **Water-density preservation** (source-verified + suite-green): operator inputs only the water
  temperature; the authoritative 31-value built-in table (5–35 °C) is byte-identical; the lookup
  rounds with `MidpointRounding.AwayFromZero` to the nearest whole degree; **no interpolation**;
  the override section semantics unchanged; glass and water remain separate facts (WDL2/WDL3/WDL6
  green).

## 7. Routes / access — PASS

- Exactly the two approved routes exist in the Definições group:
  `GET /controlo/create/definicoes/glass-densities` and
  `PUT /controlo/create/definicoes/glass-densities/{processo}` (endpoint source; the access-test
  inventory includes both).
- Both inherit the group's single `RequireAuthorization(ModuleAuthorizationPolicies.PolicyName(
  ModuleCatalog.ControloCreate))` — the canonical `dmo.module.controlo-create` policy; AUT4's
  EndpointDataSource metadata row (inside the green suite) asserts every `/controlo/create/*`
  endpoint carries exactly that policy and no approve/admin policy.
- No new capability, no approval capability, no availability/destination/navigation registration
  (diff audit + GD-S4 + BND1).
- No direct-route regression: AUT1 (no grants → 403 on every route incl. 18/19), AUT2
  (approve-only → 403), AUT3 (admin → 403), AUT4 (no approve surface) all green.

## 8. Update / concurrency — PASS

- NNPB and PS update independently (per-processo isolation in repository + service; GD-I2 both
  directions, GD-DB2).
- `PROCESSO_UNKNOWN` for any non-canonical path token (validator + GD-V1/GD-I4/GD-DB3 — no alias,
  case-fold or other processo).
- `DENSITY_NOT_POSITIVE` for zero/negative density (GD-V2/GD-I4 (0 and −1)/GD-DB3).
- Version enforced: explicit in-transaction compare → `ConcurrencyConflictException` →
  `SettingsResult.Refused(StaleVersion)` → HTTP 409 `stale-version`; nothing written (GD-I3:
  row keeps newer value/version after the refused stale write; GD-DB2: repository throws on
  stale; the version bumps exactly once per committed mutation).
- The frontend reuses the existing D2 conflict/reload handling: the glass-density PUT failure
  path calls the shared `renderMutationFailure` → `renderConflict` (reload recovery), no
  automatic retry, no silent overwrite — proven behaviorally by harness scenarios S7 (stale →
  conflict + recovery, exactly ONE request, observed version sent) and S8 (validation-failed →
  plain errors, no conflict marker).

## 9. Definições UI — PASS

Rendered-page source + rendered assertions:
- The page gains exactly ONE approved section ("Densidade do vidro (g/cm³)") as the sixth
  section after the five existing ones — nothing moved, nothing redesigned (LAY2 rendered order
  green: six section titles in structural order).
- Exactly two rows: NNPB and PS, each with an editable numeric input `step="0.0001"`
  `min="0.0001"` (four-decimal behavior), the unit `g/cm³` displayed per row and in the title,
  the observed version on the input, a per-processo "Guardar" action (LAY2 rendered row: two
  `data-dmo-glass-density-row` entries, two unit spans, `step="0.0001"`, rendered 2.4027/2.4231).
- No per-tool selection, no process creation/deletion, no new module/tab, no movement of
  existing settings; fixed-desktop posture unchanged (breakpoint-free stylesheet rule intact).

## 10. Closed decisions remain closed — PASS

| Closed decision | Status |
|---|---|
| `tool_id`/`cm_id` identity semantics; Job On owns `cm_id`; no `production_id` | unchanged (no identity source touched; PID/BND rows green) |
| Pending Peso association; same `peso_id` on association; Peso aggregate rows | unchanged (no association/anchor code touched) |
| Water-density implementation (temperature input, 31-value table, 5–35 °C, AwayFromZero, no interpolation) | unchanged and green (§6) |
| D1 submitted-view fix | adapter untouched in its D1 behavior; S0/S1 green |
| D2 stale-version recovery | reused by the new PUT; S2–S8 green |
| D3/D4 non-blocking status | untouched, still recorded non-blocking |
| Approval scope (P2-T06) / PDF generation-send scope (P2-T08) | no implementation appears (BND2/BND4, GD-S1) |
| Fixed desktop rule | unchanged (LAY1/LAY2/LAY3 green) |
| `CurrentBuildAvailable = []` | unchanged (source `[]` + BND1 + GD-S4 green) |

## 11. Independent test execution (this verification run, fresh disposable PostgreSQL)

| Claim | Implementation claim | Independent result |
|---|---|---|
| build | 0 errors | **0 errors** (1 pre-existing pinned xUnit2029 warning in byte-identical protected `P2T02RegressionTests.cs`) |
| full unit | 586 passed | **586 passed / 0 failed / 0 skipped** |
| full integration | 500 passed / 2 skipped | **500 passed / 0 failed / 2 skipped** (fresh DB; the 2 skips are the pre-existing live-Supabase Auth rows) |
| targeted correction | 28 | **28** (9 unit: GD-U1×2+GD-U2..U6, GD-V1/V2; 19 integration: GD-I1..I6, GD-S1..S4, GD-M1..M6, GD-DB1..DB3) |
| targeted P2-T05 | 207 | **207** (77 unit + 130 integration) |
| water-density focused | 43 | **43** (42 `WaterDensityLookupTests` + rendered WDL1), unchanged and green |
| D1/D2 regression | S0–S8 green | **ALL 9 behavioral scenarios PASS** (direct `node` run of the harness against the shipped adapter) |
| migration/schema | §16.9 | independently re-verified on a FRESH disposable DB (§3 above) |
| freeze/history | GD-U4/5, GD-I6, MES10 | independently executed, green (§5) |
| routes/access | AUT rows | AUT1–AUT5 green (§7) |
| negative scope | BND1–BND9, GD-S1–S4, TOL17 | green (22 regression/access rows + 4 ToolRestriction rows) |
| CurrentBuildAvailable | `[]` | source `[]`; file untouched; BND1/GD-S4 green |
| working tree | CLEAN | CLEAN at start and end of this verification |

## 12. Verdict

**VERIFIED.**

- schema: **PASS**
- initial values: **PASS**
- settings ownership: **PASS**
- current-value resolution: **PASS**
- frozen-history semantics: **PASS**
- routes/access: **PASS**
- concurrency: **PASS**
- water-density preservation: **PASS**
- negative scope: **PASS**
- CurrentBuildAvailable: **PASS**

The correction is **READY FOR ARCHITECT IMPLEMENTATION REVIEW**. The original P2-T05 workstream
remains **closed**; this correction slice is **not yet closed** (its closure is the Architect
implementation review + closure record). P2-T06 / P2-T07 / P2-T08 / P2-T10 remain NOT
AUTHORIZED — nothing of them was started by this verification.

---

**STOP — this record performs independent verification only: no implementation change, no
Architect review, no slice closure. NEXT GATE: Architect implementation review of this
correction slice only.**