# P2-T05 — Controlo Create + Controlo_Create → Definições — INDEPENDENT VERIFICATION

**Verification class:** independent verification of the actual implementation (source-level,
commit-level, database-level, test-level). No implementation, no code modification, no migration,
no Supabase change.
**Repository:** `diogo-o/DMO-MODULAR` @ `main`.
**Verified implementation SHA:** `092743a97da2fe6b90dac04d69a9a3986f0997d7` (implementation commit;
reachable from HEAD — `git merge-base --is-ancestor` exit 0) plus the water-density correction
`66e7f0d` and the governance follow-ups `7db6035`/`b83e5c2`/`1bbbedf`/`db79772`/`f4e81da`/`0d34c47`.
**Verified remote main:** `0d34c4711028db38e0446a08de431c2cd3bac41b` (HEAD of the verified
checkout; `origin/main` up to date).

---

## 1. EXECUTIVE VERDICT

## NOT VERIFIED

P2-T05 Controlo_Create is **NOT VERIFIED** as-implemented: the delivered Create-surface adapter
(`src/DMO.Web/wwwroot/js/dmo-controlo.js`) contains **two blocking defects** (D1, D2) — a
reproducible runtime error on the standard post-submit view and the absence of the contracted
conflict reload/recovery — plus **two non-blocking defects** (D3, D4), all with exact citations in
§11. Everything else in scope — identity/anchoring, the Peso aggregate and rows, the context read,
the water-density correction and its legacy authority, historical semantics, calculations, the
schema/migration (independently applied and catalogued on a disposable PostgreSQL 16), the 17
routes and `controlo-create` gating, the Definições settings, fixed desktop, negative scope and
availability — **verified clean**, and **all claimed test totals were independently reproduced**
(§3–§8).

The defects are confined to the page-owned JS adapter and one markup element; none affects data
integrity, authorization, persistence or the 66 acceptance criteria (all proven by the green 84-row
matrix). P2-T05 is **ready for Architect implementation review only after the D1/D2 correction**,
and is **NOT yet CLOSED**; P2-T06 / P2-T07 / P2-T08 / P2-T10 remain NOT AUTHORIZED and were not
started.

---

## 2. Verification evidence base (what was run, independently)

| Item | Evidence |
|---|---|
| Build | `dotnet build DMO.slnx -c Debug`: **0 errors**; 1 warning = the pre-existing pinned xUnit2029 in the byte-identical protected `tests/DMO.IntegrationTests/Frontend/Shared/P2T02RegressionTests.cs:109`; 0 warnings in all P2-T05 code |
| Full unit suite | `dotnet test tests/DMO.UnitTests` — **577 passed / 0 failed / 0 skipped** |
| Full integration suite | `dotnet test tests/DMO.IntegrationTests` with `DMO_TEST_POSTGRES_CONNECTION` pointed at a **fresh disposable database** (`dmo_p2t05_verify`) in a disposable `postgres:16` container (host-local port, never Supabase TEST) — **479 passed / 2 skipped / 0 failed**; the 2 skips are the pre-existing live-Supabase Auth category-B tests (`LiveDevTestSupabaseAdminAuthTests`, `LiveDevTestSupabaseUserAuthTests`), unchanged posture |
| Targeted P2-T05 | unit filter `FullyQualifiedName~ControloCreate` = **68 passed**; integration filter `ControloCreate\|PesoRepository\|ControloSettingsRepository\|PesoJobOnDependencyProbe\|Migration004` = **94 passed**; total **162 passed / 0 failed / 0 skipped** |
| Focused water density | `WaterDensityLookupTests` = **42 passed**; plus the rendered-surface `WDL1` (inside the 94) = **43 total WDL rows passed** |
| Schema/migration | independent `dotnet ef database update` (all 4 migrations) on a **fresh** disposable DB (`dmo_p2t05_schema`), then direct `pg_constraint`/`information_schema`/`pg_indexes` catalog queries; `Down` verified by rollback to `20260922232349_ToolJobOnDomainCore` on a third fresh DB (`dmo_p2t05_down`); re-run idempotency verified ("No migrations were applied") |
| Negative/static scans | BND1–BND9, PID5, AUT5, LAY1, REP4, MAC6, SNA5 rows executed inside the green 479; plus an independent full `git diff b38993f..HEAD` inventory and git-blob byte-identity checks of every protected file |
| Legacy water-density audit | independent source-level audit of 22 legacy `WeightCalculator.cs` copies (3 structural variants), 30 legacy JS `DENSITY_TABLE`/`WATER_DENSITY_TABLE` copies (10 `app-core.js` + 20 `v5-*.bundle.js`, PORTAL_DMO distribution-labeled), 24 `WeightCalculatorTests.cs` copies, `02_PESO_COMPLETE_SPEC.md` §4, `CONTROLO_TECHNICAL_MODEL.md` (see §5) |

---

## 3. Domain / identity — VERIFIED

- `peso_id` (`src/DMO.Domain/Controlo/PesoId.cs`) is one Peso record identity, backend-allocated
  (`PesoId.New()` inside the create transaction), never client-supplied (PID4).
- Anchoring is exclusive and DB-enforced: `pesos_anchor_check ((cm_id IS NULL)::int + (tool_id IS
  NULL)::int) = 1` (migration + live catalog verified). Normal Peso anchors to the real P2-T04
  `cm_id` (`FK_pesos_cm_contexts_cm_id` RESTRICT); pending Peso anchors truthfully to `tool_id`
  (`FK_pesos_tools_tool_id` RESTRICT) — exactly one anchor, never none, never both.
- `pesos` carries **no** `jobon_id`, `reference`, `production_number`, `machine`, `production_id`,
  `job_on_revision_id`, `previous_peso_id`, jsonb, or any legacy identity column (schema catalog +
  PID5 static scan).
- P2-T05 never recreates `cm_id`/`mf_id`/`bq_id`/`jobon_id`/`tool_id`: the only context-adjacent
  write (route 11) composes `IJobOnService.UpdateAsync` (Keep all facts + Set CM slot) so the
  `cm_contexts` row is created by **Job On's own** repository code; `DmoPesoContextRead` is a
  read-only single-table SELECT. The sole cross-stream additive Job On member is the accepted
  Q-CAND `ListPesoAssociationCandidatesAsync` (diff verified **additive-only**: one interface
  member + one record + one result case; no existing member or Job On route modified).
- Pending → associated preserves the same `peso_id`; the target `cm_id` must resolve to the
  Peso's anchor `tool_id` (`ASSOCIATION_MISMATCH` refusal otherwise); the direct Tool anchor is
  cleared on success in one transaction (PID6/PID7/PID8 passed).
- Tool and Job On identity semantics unchanged (blob-identical protected P2-T04 files; `tools`/
  `job_ons`/contexts never written by P2-T05 code).
- Q-CAND candidates are real `cm_contexts` rows resolving to the anchor Tool, never synthesized
  (PID9 unit row green).

## 4. Peso aggregate / rows — VERIFIED

- The domain `Peso` embeds `IReadOnlyList<PesoMeasurementRow> Rows` (`Peso.cs:40`); the repository
  write signatures remain **exactly the contracted separated signatures** `CreatedAsync(Peso,
  IReadOnlyList<PesoMeasurementRow>, …)` / `UpdatedAsync(...)` (`IPesoRepository.cs` verbatim
  §20.2).
- `GetByIdAsync` loads the Peso **with the full row set** (`PesoRepository.cs:45-63`); the read
  model and the submit recompute/verify consume the same persisted rows — no second row
  source-of-truth exists.
- Every write opens its own transaction (`BeginTransactionAsync`); forced-failure rollback is
  proven (CRE1/CRE2/CRE5 green); SQLSTATE `23514` on the capacity/glass CHECKs maps to the same
  `ResultNonPositive` refusal the validator raises (C2 backstop, MES11 green); RESTRICT FK
  violations map to typed `CM_CONTEXT_NOT_FOUND`/`TOOL_NOT_FOUND`.
- Submit re-validates and **recomputes-and-verifies** the persisted rows with the Peso's FROZEN
  glass density and the current water-density resolution (`ControloCreateService.VerifyStoredResults`);
  non-positive re-derivations → `RESULT_NON_POSITIVE`, non-re-derivable stored results →
  `calculation-configuration-missing`; nothing is silently rewritten (CRE7/CRE8 green).

## 5. Water-density correction — VERIFIED (authority evidence confirmed independently)

- `ConfigurationCalculationConfiguration.AuthoritativeWaterDensityByCelsius` ships exactly **31
  values**, whole degrees **5–35 °C**. The values were independently confirmed **value-for-value
  against the actual legacy sources** (not the response's word, not internet values):
  - legacy C# `BA.Dmo.Domain.Modules.Peso.WeightCalculator.WaterDensityByCelsius`/`LookupDensity`
    (TD-25): **22 copies across the BA-DMO corpus (3 structural variants: canonical 17 byte-identical
    SHA256 `1926C3B9…`, 5 CRLF-only clones, 1 `Main\Spec` services-layout variant with identical
    values) — 31/31 exact match**;
  - legacy web app `DENSITY_TABLE` (`app-core.js`, 10 copies) and `WATER_DENSITY_TABLE`
    (`v5-operator.bundle.js` / `v5-manager.bundle.js`, 20 copies) from the PORTAL_DMO distributions
    (packages self-label "PORTAL DMO 5.5.0 — PACOTE ORGANIZADO"; the app-internal `APP_VERSION`
    constant reads `5.3.4-subject-separators` — provenance nuance only, the table is identical in
    every era copy): **31/31 exact match in all 30 files**;
  - legacy `WeightCalculatorTests.cs` (24 copies): 31-row density theory + rounding boundaries
    (20.4→20, 4.50→5, 35.49→35, 4.49/35.50 → error); behavior identical;
  - `02_PESO_COMPLETE_SPEC.md` §4: response's quote matches the actual file verbatim
    ("`lookupDensity(temperature)` devolve o valor da tabela `DENSITY_TABLE` … arredondada (5–35 °C).
    Se fora do intervalo, erro.");
  - `CONTROLO_TECHNICAL_MODEL.md`: "tabela de densidade da água (`WeightCalculator.LookupDensity`,
    divisor 5–35 °C) é código"; "divisor não snapshotado … o registo guardado é a verdade; recalculo
    é preview" — supports the no-schema-change decision (the response cites it as "G5"; the doc's
    §16 labels the density-divisor item "CLEANUP G5" while §17 numbers it G4 — documentation nuance
    only, no value impact).
- **No interpolation** exists in any source; the lookup rounds to the nearest whole degree with
  `MidpointRounding.AwayFromZero` (`ConfigurationCalculationConfiguration.cs:170`, identical to the
  legacy C# and equivalent to JS `Math.round` for the positive 5–35 °C domain). Representative
  values independently confirmed: 20 → **0.99717**, 25 → **0.99603**, 35 → **0.99305**; 20.5 → 21
  (0.99696), never a midpoint (WDL rows green).
- Operator enters **only** the water temperature: no water-density/divisor/factor member exists in
  any create/edit/calculate command or transport carrier (WDL1 unit + rendered WDL1 green; both
  `.cshtml` pages contain exactly one water input, `Temperatura da água (°C)`, min 5 / max 35).
- Capacity = `water_weight_g ÷ resolved water density` (service `ComputeRows`); glass density
  remains a separate processo-derived lookup (`cm_id → tool_id → processo`; pending `tool_id →
  processo`) via `Controlo:Calculation:GlassDensities`, frozen onto the Peso as
  `glass_density_g_cm3` and never refreshed (MES10 green).
- Fail-closed behavior verified: a supplied override section **replaces** (never merges) the
  built-in table, an omitted degree resolves nothing → `calculation-configuration-missing`
  (nothing written); rounded degrees outside 5–35 fail closed; `RESULT_NON_POSITIVE` intact.
- No schema change from the correction: commit `66e7f0d` touches **zero** migration/model/
  configuration files (verified by `git show`); the one-migration/8-table contract is untouched.
  (Non-blocking observation: the response's `66e7f0d` stat "+314/−123" excludes the new 586-line
  `WaterDensityLookupTests.cs`; the actual stat is 13 files +900/−123 — file count and deletion
  count match, insertions are explained by the excluded test file.)

## 6. Historical semantics — VERIFIED

- `water_temperature`, `capacity_cm3`, `glass_weight_g`, `glass_density_g_cm3` are persisted
  (schema verified); the density is frozen once at the first successful calculate/save and the
  draft-edit path recomputes rows with the **frozen** density (MES10); stored per-row results are
  never recomputed from current Tool/Job On state — no such path exists (SNA1/SNA2/SNA4/SNA5 green,
  static scan green).
- A later Job On edit changes only what the read model shows through documented traversal
  (reference/production/machine via `cm_id → jobon_id`); the Peso's own frozen facts (context triple
  via `cm_id`, inputs, density, per-row results, attribution) are never altered by it (SNA2 green).
- No ninth table, no water-density identity/table created (catalog verified), no PDF-data duplicate
  source of truth (authoritative-data ruling embodied: no document table; `BND4` green).

## 7. Calculation — VERIFIED

- `capacity = water_weight ÷ resolved water density`; `glass weight = (capacity + volume Marisa/BQ
  − volume Punção/PU) × glass density` — computed **backend-side** at `decimal.Round(…, 4,
  AwayFromZero)` per row and persisted; the frontend never redefines the formulas (WDL4 parity
  997.17 ÷ 0.99717 → 1000.0000 cm³; MES3/MES4 green).
- `RESULT_NON_POSITIVE` is refused **before any write** (validator-level and service-level, C2) and
  remains the mapped backstop of the CHECKs (MES11 green); missing configuration fails closed
  (`calculation-configuration-missing`, MES9); no silent zero/default anywhere.
- Precision/storage: `numeric(18,4)` on weights/volumes/capacity/density, `numeric(4,1)` on
  temperature; ≤ 2 dp presentation only (MES7 green).

## 8. Schema / migration — VERIFIED (independent run)

Applied all 4 migrations to a fresh disposable PostgreSQL 16 via `dotnet ef database update`
(design-time factory, fresh database created by the verifier). Live catalog:

| Fact | Contract | Verified |
|---|---|---|
| Tables | 8 (`pesos`, `peso_measurement_rows`, `repairers`, `machine_repairer_assignments`, `pdf_directory_settings`, `email_lists`, `email_list_recipients`, `email_templates`) | 19 public tables total (18 product + `__EFMigrationsHistory`); exactly the 8 Controlo tables added; **no ninth table** |
| Named CHECKs | 22 | 22, exact names and expressions (incl. `pesos_anchor_check`, `pesos_temperature_check`, `peso_measurement_rows_capacity_check`/`_glass_check`, `email_templates_document_type_check`, `machine_repairer_assignments_machine_check`, the six singleton/required checks) |
| FKs | 7, ALL `ON DELETE RESTRICT` | 7 with `confdeltype='r'`; the only cascade in the whole schema is the pre-existing foundation `FK_template_modules_templates_template_id` (`c`) |
| Unique keys | 6 | `peso_measurement_rows_peso_position_key`, `machine_repairer_assignments_machine_key`, `pdf_directory_settings_singleton_key`, `email_lists_name_key`, `email_list_recipients_list_address_key`, `email_templates_name_key` |
| Indexes | `IX_pesos_cm_id`, `IX_pesos_tool_id`, `machine_repairer_assignments_repairer_idx` | present (+ two EF auto FK-supporting indexes on the user FKs — same EF behavior as the accepted P2-T04 baseline) |
| Seed data | none | all 13 product tables 0 rows after applying |
| Down | exactly the 8 tables | rollback to `20260922232349_ToolJobOnDomainCore` leaves only the pre-P2-T05 schema (11 tables); 0 Controlo tables remain |
| Idempotency | re-apply no-op | "No migrations were applied. The database is already up to date." |
| Prior migrations | unchanged | git-blob byte-identity of migrations 001/002/003 (+ Designers) and `DmoDbContext.cs` between `b38993f` and HEAD: **IDENTICAL** (also pinned by content hash in `P2T05RegressionTests.BND8`) |
| Correction | no schema change | `66e7f0d` touches no migration/model file |

## 9. Routes / authorization — VERIFIED

- Exactly the accepted **17 route groups**: 2 Razor pages (`/controlo/create` route 1,
  `/controlo/create/definicoes` route 12) + 15 minimal-API groups (routes 2–11 in
  `ControloCreateEndpoints.cs`: productions, jobons/{id}, association-candidates, POST pesos, GET
  pesos/{id}, PUT pesos/{id}, POST calculate, POST submit, POST associate, POST cm-association;
  routes 13–17 in `ControloDefinicoesEndpoints.cs`: repairers GET/POST/PUT, machine-assignments
  GET/PUT, pdf-directory GET/PUT/POST-check, email-lists GET/POST/GET-id/PUT/DELETE, email-templates
  GET/POST/GET-id/PUT/DELETE). Enumerated from the source.
- Every route (endpoints and both pages) requires exactly `dmo.module.controlo-create`
  (`ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloCreate)`; the Razor constant pinned
  in `ControloPolicyNames.cs` and asserted equal to the canonical projection by AUT5). No route
  carries `controlo-approve`/`dmo.administration` (scan: only comment prose mentions the tokens);
  no policy created; ADMIN gains nothing (AUT3 green).
- **Route 8 is stateless**: `POST /controlo/create/calculate` — no path identity parameter; the
  request carrier has `cmId` XOR `pendingToolId` (validator `PESO_ANCHOR_REQUIRED`/
  `PESO_ANCHOR_CONFLICT`); it never creates a Peso/id/version, never resolves a persisted record and
  **never returns record-not-found** (its result mapping has no 404 path; unknown anchors are 400
  `CM_CONTEXT_NOT_FOUND`/`TOOL_NOT_FOUND`; unavailable configuration is 409 —
  `calculation-configuration-missing`). JRC7 green.
- Settings routes are protected equally (AUT1/AUT2 green: `controlo-approve`-only holder denied
  routes 12–17 and every Peso route; shared `controlo` destination never merges grants).
- Interim availability: `ModuleRegistrations.CurrentBuildAvailable = []` (byte-identical blob),
  `DestinationRouteRegistrations` untouched/empty; integration hosts use test-only module
  registries (BND1 green).

## 10. Settings / UI / negative scope — VERIFIED

- **Repairer registry**: `name` is the only business field (schema + REP4 static scan: no
  address/email/phone/supplier/tax/contact field); add/rename/select only; no delete route (REP3);
  referenced repairers DB-protected (REP5).
- **Machine assignments**: six independent rows, closed CHECK `B1..C3`; set/change/clear one
  machine never touches another (MAC1–MAC5 green); no group/line column, no cascade (MAC6).
- **PDF directory**: absolute server-host path (validator `DIRECTORY_REQUIRED`/`DIRECTORY_INVALID`;
  UI labels it "caminho absoluto no servidor"); check executes **server-side**
  (`ServerHostPdfDirectoryProbe`, probe file created and removed inside the probe; no production
  document IO) with the exact typed vocabulary `not-configured | ok | directory-not-found |
  not-a-directory | access-denied | invalid-path | check-failed` (SET2/SET3/SET12 green); no
  browser filesystem access anywhere (scan of the adapter: no File System Access API, no FileReader,
  no `file://` handling — the browser only submits the configuration).
- **Email lists/templates**: named lists with atomic replace-all recipient sets in one transaction
  (SET4/SET5 green); minimal address shape (Q-ADDR), unique names, `duplicate-name` 409, confirmed
  deletes with `DELETE_NOT_CONFIRMED` (SET6/SET9 green); templates carry name/subject/body + the
  three-value document type or NULL, text verbatim (SET7 green); **no email send workflow exists**
  (no Smtp/MailKit/send vocabulary anywhere in `src/`; templates/lists are configuration only);
  no hardcoded recipient address (SET8 scan green).
- **UI / fixed desktop**: canonical surface designed for 1366 × 768; regions R1–R8 present in
  structural order (strip, production selection, CM context, inputs, results, actions, status,
  document seam); Definições is one page with the five sections (Reparadores, Reparador por máquina,
  Diretório de PDF/documentos, Listas de email, Templates de email); `dmo-controlo.css` has **no**
  `@media`/`@container`/`@supports` rule (the only matches are comment prose stating their absence),
  no width listener, no table→card conversion, no required-column hiding, no action relocation;
  over-wide tables use keyboard-reachable local overflow (`tabindex="0"` scroll regions);
  water temperature appears as operator input; **no water-density/divisor input exists** (LAY1/LAY2/
  LAY3/WDL1 rendered rows green).
- **Negative scope**: no P2-T06 approval workflow surface (BND2), no P2-T07 Boquilhas anything
  (BND3/MAC6), no P2-T08 PDF/email-send/availability/routing/placeholder code (BND4), no duplicate
  Tool/JobOn/CM identities (BND5), no machine registry/grouping (BND6), no Comparação/
  Pegamentos/Folha/Resumo tables or `previous_peso_id` (BND7), protected migrations 001/002/003 +
  `DmoDbContext.cs` byte-identical (BND8; git-blob-verified independently), exactly one new migration
  owning exactly the eight tables (BND9; independently verified), no deviation from the contracted
  paths in the whole `b38993f..HEAD` diff (BND9 diff inventory: only P2-T05-owned paths, the two
  sanctioned Job On additive files, test pins, governance docs).
- **Availability**: `ModuleRegistrations.CurrentBuildAvailable = []` (verified, byte-identical to
  the pre-P2-T05 baseline); no destination route registration attributable to P2-T05;
  `DestinationRouteRegistrations.cs` byte-identical.

---

## 11. DEFECTS FOUND (exact citations)

### D1 — BLOCKING: the adapter throws a `TypeError` on every load of the submitted-view surface

- **Exact location:** `src/DMO.Web/wwwroot/js/dmo-controlo.js:162` (also `:172`, `:204`) —
  `root.querySelector("[data-dmo-action='calculate']").addEventListener(...)` (likewise `'save'`,
  `'cancel'`) executes **without a null guard**. In the submitted state the page intentionally
  renders **only** the disabled `submit` action (`src/DMO.Web/Pages/Controlo/Create.cshtml.cs:390-398`,
  `BuildDraftRegions` → `DecisionBarPresentation.Create(CommonState.Conflict, [disabled "submit"])`;
  `_DecisionBar.cshtml` emits `data-dmo-action="@action.Key"` only for supplied actions).
- **Trigger:** any load of `/controlo/create?pesoId=<submitted>` — including the standard
  **post-submit `window.location.reload()`** at `dmo-controlo.js:197` — throws `TypeError: Cannot
  read properties of null (reading 'addEventListener')` at line 162, killing **all** subsequent
  wiring on that view (submit re-wire, cancel, and the associate handler at `:208`).
- **Why the green suites miss it:** no test executes the adapter's JavaScript; the LAY2/WDL1
  rendered rows assert server-rendered markup only.
- **Violated contract:** §8.6 (the submitted-only presentation must remain a correct, functional
  surface after submit — server gating holds, but the delivered adapter dies on the natural
  terminal state of the workflow); §8.5 (non-ready state handling); the shared freeze §4 state
  handling is bypassed with an exception. No data-integrity impact (the backend refuses every
  post-submit mutation), but the runtime error is real and reachable.
- **Correction (minimal):** guard each binding (`var el = ...; if (el) el.addEventListener(...)`)
  or bind through the existing `value()`-style null-safe helper; re-verify with a rendered test
  that opens a submitted draft.

### D2 — BLOCKING: no reload/recovery on `stale-version` conflict

- **Exact location:** `src/DMO.Web/wwwroot/js/dmo-controlo.js:179-188` (save), `:194-201` (submit),
  `:217-222` (associate) — every non-OK path calls only `renderErrors(root, response.body)`; the
  anchor `version` is refreshed **only on success** (`setAnchor` at `:182`), so a 409 `stale-version`
  repeats forever on re-save and no reload/recovery affordance is offered.
- **Violated contract:** §8.4 (`"stale-version` → `conflict` presentation with reload/recovery"`),
  §19.3 ("a refused mutation reports the typed reason and **the surface reloads** (`conflict`
  presentation, freeze §4)"), and freeze §4 line 104 (`conflict` = "Clear message and **supplied
  recovery choices**; retain entered work where possible"). The typed reason **is** displayed
  (partial compliance; no silent overwrite — `expectedVersion` is sent on every guarded write), but
  the contracted recovery path is absent.
- **Correction (minimal):** on a 409 `stale-version` response show the typed `conflict` state with
  a reload/recovery control (or reload automatically per §19.3); re-verify with a rendered test.

### D3 — NON-BLOCKING: the `CALCULATION_CONFIGURATION_MISSING` R5 region is dead code

- **Exact location:** `src/DMO.Web/Pages/Controlo/Create.cshtml:256-259` (region
  `[data-dmo-calculation-missing]`) is only ever **hidden** by `dmo-controlo.js:100-101` and never
  shown; a 409 `calculation-configuration-missing` surfaces only through the generic state region
  (`renderErrors`), so the contracted dedicated R5 state (§8.3 R5 line 820) never renders. The
  dormant element also contains the Portuguese typo "configure a **cálcula** e repita" (line 258;
  should be "cálculo"). The typed refusal remains communicated, so this is presentation only.
- **Correction:** show the region on the 409 token (and fix the typo).

### D4 — NON-BLOCKING: the Calcular preview renders blank `Desvio cm³` / `Desvio %` cells

- **Exact location:** `src/DMO.Web/wwwroot/js/dmo-controlo.js:108` — `renderResults` builds each
  preview row as `[position, waterWeight, capacity, "", "", glassWeight]`; the two per-row
  deviation columns (derived from the average; §8.3 R5 line 820 requires `Desvio cm³` / `Desvio %`
  in the per-row results table) are empty for a newly calculated Peso until a save/reload (the
  server-rendered draft view does compute them — `Create.cshtml.cs` `PesoResultsPresentation.
  DeviationCm3/DeviationPercent`). No data impact; visible gap in the primary Calcular preview.
- **Correction:** compute and render the two deviation cells in `renderResults` (or leave the
  columns to the server after save), consistent with the server-side derivation.

### Minor observations (recorded, not defects)

- **O1** — the implementation response's correction-commit stat "+314/−123" excludes the new
  `WaterDensityLookupTests.cs` (586 insertions); actual `66e7f0d` stat: 13 files, +900/−123.
- **O2** — `CONTROLO_TECHNICAL_MODEL.md` labels the "water divisor not snapshotted / registo
  guardado é a verdade" item "CLEANUP G5" in §16 but numbers it G4 in §17; the response's G5
  attribution is faithful to the §16 label.
- **O3** — the legacy portal's app-internal `APP_VERSION` reads `5.3.4-subject-separators` while
  the 5.5.0 label comes from the distribution docs; the `DENSITY_TABLE` is byte-identical in every
  era copy, so no value-level impact.
- **O4** — the disclosed P2-T04 test-pin changes (DUP13 closed-surface 7→8; P2T04RegressionTests
  BND7 exclusion of the three Job On additive files; MigrationRunnerTests/DatabaseConnectivityTests
  counts) match the disclosed list and the actual diff; no test was weakened (all suits green).

---

## 12. Required fields

| Field | Value |
|---|---|
| Verdict | **NOT VERIFIED** (D1, D2 blocking; D3, D4 non-blocking — §11) |
| Verified implementation SHA | `092743a97da2fe6b90dac04d69a9a3986f0997d7` (+ correction `66e7f0d`, governance `0d34c47` chain) |
| Verified remote main | `0d34c4711028db38e0446a08de431c2cd3bac41b` (`origin/main`, HEAD, up to date) |
| Build result | 0 errors; 1 pre-existing pinned warning (xUnit2029, protected `P2T02RegressionTests.cs`) |
| Unit totals | **577 passed / 0 failed / 0 skipped** (claimed 577 — **confirmed**) |
| Integration totals | **479 passed / 2 skipped / 0 failed** (claimed 479/2 — **confirmed**; the 2 skips are the pre-existing live-Supabase Auth tests) |
| Targeted P2-T05 totals | **162 passed / 0 failed / 0 skipped** (68 unit + 94 integration; claimed 162 — **confirmed**) |
| Water-density focused totals | **43 passed** (42 `WaterDensityLookupTests` + rendered WDL1; claimed 43 — **confirmed**) |
| Schema result | **PASS** — independent apply + catalog audit on disposable PostgreSQL 16: 8 tables, 22 named CHECKs, 7 RESTRICT FKs, 6 unique keys, declared indexes, 0 seed rows, Down = exactly the 8 tables, re-apply no-op, prior migrations and `DmoDbContext.cs` byte-identical, no ninth table, correction changed no schema |
| Negative-scope result | **PASS** — BND1–BND9/PID5/AUT5/LAY1/REP4/MAC6/SNA5 green in my run; independent diff inventory and git-blob identity checks confirm no P2-T06/T07/T08 leakage, no duplicate identities, no machine grouping, no document table, no availability/navigation registration |
| Availability result | **PASS** — `ModuleRegistrations.CurrentBuildAvailable = []` (byte-identical), `DestinationRouteRegistrations` untouched, no route/destination/navigation entry attributable to P2-T05 |
| Working-tree state | **CLEAN** before and after verification (no implementation file modified by this verification; scratch logs removed) |

---

## 13. Governance / next gate

- P2-T05 is **NOT CLOSED**. The next gate is the **Architect implementation review** — after the
  D1/D2 (and ideally D3/D4) correction and a short re-verification of the create-surface adapter.
- Nothing was implemented, no code was modified, no migration was created, no Supabase was
  touched, no availability was registered by this verification.
- **P2-T06 / P2-T07 / P2-T08 / P2-T10 were NOT started** and remain NOT AUTHORIZED.

*Verification run: build + full unit + full integration on a fresh disposable PostgreSQL 16
(`postgres:16` container, `dmo_p2t05_verify`/`dmo_p2t05_schema`/`dmo_p2t05_down` databases —
never the shared Supabase TEST), targeted runs, independent `dotnet ef database update` + catalog
queries, legacy water-density source audit across 22 C# + 30 JS + 24 test + 2 spec/technical-model
copies, protected-file git-blob identity checks, complete `b38993f..HEAD` diff inventory.*