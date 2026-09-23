# P2-T05 — QUICK INDEPENDENT RE-VERIFICATION — D1/D2 FOCUSED CORRECTION

**Verification class:** quick independent re-verification, limited to the focused correction
(D1 — submitted-view adapter crash; D2 — stale-version recovery) and regression preservation.
No code modified, no migration, no Supabase, no availability registration, no Architect review.
**Repository:** `diogo-o/DMO-MODULAR` @ `main`.
**Verified correction SHA:** `9fbfcf40cffd4f54c916b0bd0f31608cb595420a` (correction commit;
HEAD of the verified checkout).
**Verified remote main:** `9fbfcf40cffd4f54c916b0bd0f31608cb595420a` (`origin/main` == HEAD,
verified after `git fetch origin main`).
**Correction diff inspected:** `2c568aa..9fbfcf40cffd4f54c916b0bd0f31608cb595420a` (5 files:
the adapter, the response doc §15, two new integration test files, the node harness).

---

## 1. EXECUTIVE VERDICT

## VERIFIED

The focused correction resolves both prior BLOCKING blockers with independent evidence:

- **D1 — PASS.** The submitted Peso view initializes the REAL shipped adapter without a
  `TypeError`; editable actions remain intentionally absent (only the disabled submit); the
  bindings that do exist keep working; the draft surface is unaffected.
- **D2 — PASS.** Every guarded mutation handled by this adapter that can receive a 409
  `stale-version` now enters the accepted conflict presentation with an explicit recovery
  control that reloads the authoritative current server state — no automatic retry, no
  auto-merge, no silent overwrite, no local version advance; non-stale failures keep their
  prior behavior; a later failure clears a stale conflict marker.
- **Regression preservation — PASS.** Build 0 errors; unit 577/0/0; integration 481/0/2
  (fresh disposable PostgreSQL 16; the 2 skips are the pre-existing live-Supabase Auth tests);
  targeted P2-T05 164/0/0; water-density 43; D1 focused 2/2; D2 focused S2–S6; negative-scope
  and availability pins green; the correction diff touches exactly the intended files.
- Prior NOT VERIFIED blockers (D1, D2) are **resolved**.
- P2-T05 is **READY FOR ARCHITECT IMPLEMENTATION REVIEW** and is still **NOT CLOSED**.
- D3/D4 remain unchanged and NON-BLOCKING (verified by diff: no file touched by the
  correction contains their locations — `Create.cshtml` and the `renderResults` row builder are
  byte-identical to the prior verified state).

---

## 2. D1 — SUBMITTED VIEW (PASS)

### 2.1 Source inspection (the real shipped adapter, `src/DMO.Web/wwwroot/js/dmo-controlo.js`)

- New page-owned null-safe helper `on(root, selector, event, handler)` (lines 149–152): binds
  **only when the target element exists** — absence is a valid state.
- **Every** single-element binder now routes through `on(...)` — create surface:
  `calculate`/`save`/`submit`/`cancel` (lines 216/226/245/258) and `associate`
  (line 264, previously already guarded — now uniform); Definições surface:
  `repairer-add` (284), `pdf-save` (331), `pdf-check` (344), `list-create` (366),
  `list-update` (403), `list-edit-cancel` (414), `template-create` (420),
  `template-update` (458), `template-edit-cancel` (469). The collection binders
  (rename/delete/cell rows) were already `forEach`-safe and are unchanged in shape.
- Static scan of the final file: **zero** unguarded `querySelector(...).addEventListener`
  patterns remain; the only direct `addEventListener` calls are the helper's internal
  `element.addEventListener` (guarded), the recovery button created by the adapter itself
  (never null), `forEach`-collection binders, and `document.addEventListener("DOMContentLoaded")`.
- **Nothing was restored to satisfy JS**: the correction changes **no** markup, `.cshtml`,
  or `.cs` file — the submitted view still renders exactly the disabled `submit` action. The
  diff proves it: no `Pages/` file in `2c568aa..HEAD`.
- No width-listener / breakpoint / `matchMedia` / `resize` token was added (LAY1 posture kept).

### 2.2 Rendered regression proof (integration, real routes, real DB)

`ControloSurfaceRenderingTests.D1_SubmittedViewOmitsEditableActionsAndKeepsTheDisabledSubmit`
(executed in my run, **PASS**): a real Peso is created and **submitted through the REAL
routes** (`POST /controlo/create/pesos` → `POST .../submit`), then the page opens in the
submitted state and the markup is asserted: `data-dmo-action="calculate"`/`"save"`/`"cancel"`
**absent**; exactly one `data-dmo-action="submit"` (disabled, "Submetido" + reason span);
adapter state region (`data-dmo-controlo-state`, `data-dmo-peso-id`,
`data-dmo-submitted="true"`) still rendered — the exact pre-fix crash condition.

### 2.3 Behavioral regression proof (node harness over the REAL shipped adapter)

`DmoControloAdapterBehaviorTests.D1_D2_TheAdapterInitializesSafelyOnTheSubmittedViewAndRecoversFromStaleVersion`
(executed in my run, **PASS**; node v24.19.0) executes `dmo-controlo-adapter.behavior.mjs`,
which loads the **actual shipped adapter file from disk** (`fs.readFileSync` of
`src/DMO.Web/wwwroot/js/dmo-controlo.js`, passed absolute by the wrapper via
`P2T04ProductionScan.RepositoryRoot()`) into a minimal DOM/window/fetch stub and drives the
real click handlers:

- **S0** — normal draft surface: calculate preview renders, create-save posts and shows
  "Controlo guardado (versão 1)", no conflict marker on success (refactor control).
- **S1** — submitted view (editable actions absent, one disabled submit, pending associate):
  `loadAdapter` **does not throw**; the disabled submit never fires and issues no request
  (platform semantics); the associate binding still works, sends the observed version, and
  surfaces the typed `already-submitted` refusal with no conflict marker.

### 2.4 Regression sensitivity (pre-fix adapter, reproduced independently)

The **sealed pre-fix blob** (`git show 2c568aa:src/.../dmo-controlo.js`, byte-exact-verified
23649 == 23649) run through the same harness **fails S1** with exactly the documented crash:
`TypeError: Cannot read properties of null (reading 'addEventListener')`. S0 and S6 pass on
the pre-fix adapter — explaining why the previous green suites could not see the defect and
proving the scenarios genuinely detect it.

**D1 verdict: PASS.**

---

## 3. D2 — STALE-VERSION RECOVERY (PASS)

### 3.1 Source inspection (real shipped adapter)

- Single page-owned failure presentation `renderMutationFailure(root, response)`
  (lines 161–168): typed `payload.reason === "stale-version"` → `renderConflict`; **every
  other** typed failure (validation-failed, already-submitted,
  calculation-configuration-missing, …) → existing `renderErrors` **exactly as before**.
- `renderConflict(root, message)` (lines 174–193): state region visible, `role="alert"`,
  `data-dmo-conflict="true"`, clear heading ("Conflito — os dados foram alterados por outra
  ação; nada foi guardado."), the typed server message, and exactly ONE recovery control —
  "Recarregar estado atual" (`data-dmo-conflict-reload`) → `window.location.reload()`
  (authoritative current server state). No retry logic, no merge logic, no write of any kind.
- **Applied consistently to every guarded mutation the adapter handles**: draft save (PUT
  peso, 240), submit (253), associate (274), repairer rename (298), machine-assignment
  set/clear (312/324), pdf-directory save (340), email-list update and delete (410/383),
  email-template update and delete (465/436). (`calculate`/creates also route through the same
  helper — harmless: they carry no version and can never receive the token.) All other
  non-OK results are unaffected.
- Anchor `version` is refreshed **only on success** (`setAnchor` at 236 — unchanged);
  `renderConflict` never touches `data-dmo-version` → no local observed version is silently
  advanced; no overwrite of the newer server version.
- `renderErrors` now removes any stale `data-dmo-conflict` marker (line 88) → a later
  non-conflict failure never leaves a false conflict presentation behind.

### 3.2 Behavioral proofs (node harness, real adapter — all executed in my run)

| Scenario | Guarded mutation | Proven |
|---|---|---|
| S2 | draft save (PUT) → 409 `stale-version` | exactly ONE request (no auto-retry); observed version 3 sent; conflict region visible with `data-dmo-conflict="true"`; clear message + typed server message; recovery control labelled "Recarregar estado atual"; `data-dmo-version` **still "3"** (not advanced); clicking recovery calls `location.reload()` |
| S3 | submit → 409 `stale-version` | same conflict/recovery; exactly one request |
| S4 | machine-assignment set (Definições) → 409 | same conflict/recovery; observed version sent |
| S5 | email-list update (Definições) → 409 | same conflict/recovery |
| S6 | non-stale failures: 400 `validation-failed` and 409 `already-submitted` | existing `renderErrors` presentation: typed message, **no** conflict marker, **no** recovery control; exactly one request |

Additionally, a **verifier scratch scenario** (copy of the harness in the OS temp dir — no
repository file touched; repo working tree stayed clean) proved the "later errors clear stale
conflict markers" transition end-to-end: conflict shown first (`data-dmo-conflict="true"`,
recovery control present), then a 400 `validation-failed` on the same surface →
`data-dmo-conflict` cleared, recovery control gone, validation token shown. All 8 scenarios
(S0–S7) passed on the fixed adapter.

### 3.3 Regression sensitivity (pre-fix adapter, reproduced independently)

Same sealed pre-fix blob: **S2 and S3 fail** ("conflict marker set: null !== 'true'") — the
pre-fix adapter shows plain errors and no conflict state on 409 `stale-version`, exactly the
documented D2 gap; **S4 and S5 fail** at load with the same class of
`Cannot read properties of null (reading 'addEventListener')` (unguarded pdf-save binder on
the sparser Definições stub) — further pre-fix evidence that the null-binding defect class
was real and reachable. S6 passes pre-fix (non-stale behavior unchanged).

**D2 verdict: PASS.**

---

## 4. REGRESSION PRESERVATION (PASS)

### 4.1 Reproduced totals (this verification run, 2026-09-23, node v24.19.0, dotnet 10.0.400,
docker 29.8.0, fresh disposable database `dmo_p2t05_reverify` on the disposable
`postgres:16` container `dmo-p2t05-pg`; never Supabase, never shared TEST)

| Item | Claimed | Independently reproduced |
|---|---|---|
| Build (`dotnet build DMO.slnx -c Debug`) | 0 errors | **0 errors**; 1 warning = pre-existing pinned xUnit2029 in byte-identical protected `tests/DMO.IntegrationTests/Frontend/Shared/P2T02RegressionTests.cs:109`; 0 warnings in correction code |
| Full unit suite | 577 / 0 / 0 | **577 passed / 0 failed / 0 skipped** |
| Full integration suite (disposable PG) | 481 / 0 / 2 | **481 passed / 0 failed / 2 skipped** (the 2 skips are the pre-existing live-Supabase Auth category-B tests, unchanged posture) |
| Targeted P2-T05 | 164 / 0 / 0 | **164 passed / 0 failed / 0 skipped** = 68 unit (`~ControloCreate`) + 96 integration (`ControloCreate\|PesoRepository\|ControloSettingsRepository\|PesoJobOnDependencyProbe\|Migration004`) — 94 baseline + the 2 new rows |
| D1 focused | rendered + behavioral S0/S1 | **2/2 PASS**: `D1_SubmittedViewOmitsEditableActionsAndKeepsTheDisabledSubmit` + `D1_D2_TheAdapterInitializesSafelyOnTheSubmittedViewAndRecoversFromStaleVersion` (explicit isolated run, no skips) |
| D2 focused | behavioral S2–S6 | **5/5 PASS** inside the wrapper (plus my scratch S7), verified twice: inside the suite and by direct `node` runs |
| Water-density focused | 43 | **43**: 42 unit `WaterDensityLookupTests` + rendered `WDL1` (in the 96) |
| Node harness / engine | real node over real adapter | harness executed with node v24.19.0; loads the shipped `dmo-controlo.js` byte-for-byte from disk |
| Negative scope / BND / availability | green | inside the green 481 + green 96 (`P2T05RegressionTests`/`P2T05ProductionScan` are in the `ControloCreate` namespace); `ModuleRegistrations.CurrentBuildAvailable = []` (line 27, file not in the diff); no width/breakpoint token in the adapter; `DestinationRouteRegistrations` untouched |
| Schema/domain/routes changed by correction | NO | **NO** — `git diff --name-only 2c568aa..HEAD` = exactly the 5 intended files (response doc §15, `dmo-controlo.js`, `ControloSurfaceRenderingTests.cs`, `DmoControloAdapterBehaviorTests.cs`, `dmo-controlo-adapter.behavior.mjs`); zero files under `src/DMO.Domain`, `src/DMO.Application`, `src/DMO.Infrastructure`, `src/DMO.Communication`, `src/DMO.Web/Pages`, migrations |
| Working tree | clean | **CLEAN** before and after (0 modified files; scratch artifacts kept outside the repo in the OS temp dir and removed) |
| CurrentBuildAvailable | `[]` | `[]` (unchanged) |

### 4.2 Untouched invariants (verified from the diff, no re-audit needed)

Because the diff is exactly the 5 files above, these are untouched by construction
(`git diff --name-only` + `git diff --stat` on each protected area, all empty):
domain, schema/migrations, identities, routes, authorization, water-density implementation
(31-value table, `AwayFromZero`, no interpolation, glass lookup), fixed-desktop policy
(markup/CSS untouched), `ModuleRegistrations.CurrentBuildAvailable`. The two pre-existing
non-blocking defects remain byte-identical: D3 (`Create.cshtml` R5 region/typo) and D4
(blank Desvio cells — `renderResults` row builder untouched by the diff). No test was
weakened or deleted (unit and integration totals grew only by the 2 new rows).

---

## 5. REQUIRED FIELDS

| Field | Value |
|---|---|
| verified correction SHA | `9fbfcf40cffd4f54c916b0bd0f31608cb595420a` |
| verified remote main | `9fbfcf40cffd4f54c916b0bd0f31608cb595420a` (origin/main == HEAD) |
| D1 | **PASS** |
| D2 | **PASS** |
| build | **0 errors** (1 pre-existing pinned xUnit2029 warning, protected file) |
| unit | **577 passed / 0 failed / 0 skipped** |
| integration | **481 passed / 0 failed / 2 skipped** (disposable fresh PostgreSQL 16; skips = live-Supabase Auth) |
| targeted P2-T05 | **164 passed / 0 failed / 0 skipped** (68 unit + 96 integration) |
| D1 focused | **2/2 PASS** (rendered `D1_SubmittedView…` + behavioral S0/S1) |
| D2 focused | **S2–S6 PASS** (save/submit/machine-assignment/email-list stale-version + non-stale preservation; + verifier scratch S7 conflict-marker clearing) |
| water-density focused | **43 passed** (42 unit + rendered WDL1) |
| regression preservation | **PASS** (full suites green; sensitivity proven on the sealed pre-fix adapter: S1 TypeError and S2/S3 no-conflict-state reproduced exactly) |
| schema/domain/routes changed by correction | **NO** |
| CurrentBuildAvailable | `[]` |
| working tree | **clean** (0 modified files at close) |
| Verdict | **VERIFIED** — D1 PASS, D2 PASS, regression preservation PASS; prior NOT VERIFIED blockers resolved |

---

## 6. GOVERNANCE

- **P2-T05 is READY FOR ARCHITECT IMPLEMENTATION REVIEW** and is still **NOT CLOSED**.
- D3 (`CALCULATION_CONFIGURATION_MISSING` R5 dead region / "cálcula" typo) and D4 (blank
  `Desvio cm³` / `Desvio %` preview cells) remain unchanged and **NON-BLOCKING**; they were
  intentionally not fixed and do not affect this verdict.
- Nothing was implemented by this re-verification; no code, migration, schema, or Supabase
  object was modified; only this report was committed. No availability was registered.
- **P2-T06 / P2-T07 / P2-T08 / P2-T10 remain NOT AUTHORIZED and were NOT started.**
- Next gate: **Architect implementation review**.

*Re-verification run: `git diff 2c568aa..HEAD` inventory; full build; full unit; full
integration on a fresh disposable PostgreSQL 16 (`dmo_p2t05_reverify`, container
`dmo-p2t05-pg`, host port 55433); targeted P2-T05 and water-density filters; explicit D1/D2
focused run; direct `node` execution of the shipped harness against both the current and the
sealed pre-fix adapter blobs (byte-exact extraction, size-verified); verifier scratch S7;
static scans of the final adapter (binding patterns, width tokens); HEAD == origin/main and
clean-tree checks.*