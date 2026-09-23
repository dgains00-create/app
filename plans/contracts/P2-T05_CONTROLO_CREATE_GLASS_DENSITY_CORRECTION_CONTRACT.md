# P2-T05 Post-Closure Correction Contract — Glass Density Configuration per Processo (Owner rule)

**Workstream:** P2-T05 — Controlo_Create (post-closure correction slice, planning gate only).
**Contract class:** narrowly scoped **correction contract** — the ONLY delta is the source and
maintenance of the processo→glass-density operational values (`Controlo → Definições`), per the
Owner rule. Every other P2-T05 decision stays closed; nothing else is reopened.
**Status:** **AUTHORED — AWAITING ARCHITECT PLAN REVIEW.** No implementation was performed by this
contract; implementation is AUTHORIZED only by the Architect PLAN ACCEPT/review outcome, per
`dmo-beta-master/WORKFLOW.md`.
**Baseline:** accepted P2-T05 contract `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` @
`b38993f3efd7657b61648e9d65566e099d9b8481` (CLOSED implementation
`9fbfcf40cffd4f54c916b0bd0f31608cb595420a`; Architect implementation review ACCEPT
`ae99d1f24e5a6ff813bb96ef13d4bf8efd9ae41c`, dmo-work; closure record
`dev/reviews/P2-T05_CONTROLO_CREATE_CLOSURE.md` @ `3491097…`, dmo-work).

---

## 1. Authority chain (in precedence order)

1. **Owner rule — GLASS DENSITY CONFIGURATION** (issued by the Owner after P2-T05 closure;
   recorded verbatim + substantively in dmo-work `dev/rulings/P2_T05_GLASS_DENSITY_CONFIGURATION_OWNER_RULE.md`
   @ `409ac24…`). This is explicit new Owner authority under the closure record's supersession
   clause. It supersedes, within the exact scope of §5 below, the accepted decisions listed in §6.1.
2. **Accepted, closed P2-T05 state** — baseline contract `b38993f` and the closed implementation
   `9fbfcf4` (identity/anchoring, frozen semantics, water density, routes/gating, Definições
   ownership, schema, vocabulary). Consumed unchanged; superseded only where §6.1 says so.
3. **Global authority** — `dmo-master`: `modules/CONTROLO.md` §8.3 ("Calibration mappings are
   configurable and their active values belong to the Peso calculation configuration"),
   `global/INFORMATION_MODEL.md` (processo NNPB/PS owned by canonical Tool; Peso preserves the
   derived glass-density/calculation configuration as historical evidence), `modules/FERRAMENTAS.md`
   (Peso consumes processo through `cm_id → tool_id`).
4. **Legacy production evidence** (authoritative VALUES — §4): `BA-DMO` legacy engine, settings
   and UI (recovered, not invented).

## 2. The Owner rule (verbatim) and its normative restatement

```text
GLASS DENSITY CONFIGURATION — OWNER RULE

tool_id
→ indica apenas o processo da ferramenta
→ NNPB ou PS

Controlo → Definições
→ mantém o valor operacional atual da densidade do vidro por processo

NNPB → densidade configurável
PS   → densidade configurável

Novo Peso:
cm_id
→ tool_id
→ processo
→ densidade atual desse processo
→ cálculo
→ valor efetivamente usado fica congelado no peso_id

Alterar a densidade depois:
→ afeta apenas novos Pesos
→ não altera Pesos históricos
→ não recalcula silenciosamente Pesos já existentes

Não existe densidade configurada por tool_id individual.
```

Normative restatement (each is binding; implemented exactly):

- **R1 (identity):** `tool_id` carries only the processo classification — `NNPB` or `PS`.
  No density and no other calculation fact lives on `tool_id`; no per-`tool_id` density
  configuration exists anywhere.
- **R2 (operational value):** `Controlo → Definições` maintains the **current operational glass
  density per processo**, in g/cm³; **NNPB and PS each have an editable current value**; there are
  exactly two values, one per processo, both always present in the surface.
- **R3 (resolution):** a new Peso resolves `processo` from the anchor (`cm_id → tool_id → processo`;
  pending `tool_id → processo`) and uses the **current** operational density of that processo in the
  glass-weight calculation.
- **R4 (freeze):** the value effectively used is frozen on the Peso (`pesos.glass_density_g_cm3`)
  at first successful calculate/save; the Peso calculation never re-reads the setting afterwards.
- **R5 (later changes):** changing a density affects **only new Pesos**; existing/historical Pesos
  are **never altered, never rewritten and never silently recalculated** (no UPDATE of
  `glass_density_g_cm3` or of any stored per-row result after creation; no background recompute).
- **R6 (concurrency-of-meaning):** the frozen value is the historical truth for each Peso (mirror
  of the legacy OC-6 `ConstanteGlassUsada` rule); the setting is only the source for future Pesos.
- **R7 (validity):** every operative density must be a valid strictly positive decimal
  (`> 0`); nothing zero, negative or invented is ever used.
- **R8 (scope):** this rule does not touch water temperature / water density, identity/anchoring,
  approval, PDF/email, availability, or any other accepted P2-T05 decision.

## 3. Baseline currently accepted (what the closed P2-T05 does today)

| Fact | Accepted baseline (closed `9fbfcf4`) | Contract anchor |
|---|---|---|
| Glass density source | backend **deployment configuration** `Controlo:Calculation:GlassDensities` (processo-keyed entries `{"Processo": "NNPB"|"PS", "Density": …}`), read by `ConfigurationCalculationConfiguration.TryGetGlassDensity`; **no committed values**; missing mapping → typed `calculation-configuration-missing` (409), nothing invented | baseline §5.3, §26.2, Q-CALC (§27.2); implementation `…/Configuration/ConfigurationCalculationConfiguration.cs` |
| Mapping **values** | "exist in **no** repository authority" (recorded authority silence S3 / Q-CALC — mechanism contracted, values arrive as backend calculation configuration) | baseline §1.5 / §27.2 |
| Resolution path | `cm_id → tool_id → processo` (pending `tool_id → processo`) — same chain the Owner rule keeps | baseline §5.3, §16.1 |
| Freeze | `glass_density_g_cm3` written once at first successful calculate/save, never refreshed; submit re-derives with the frozen value; no silent recalculation path exists (SNA1–SNA5, MES10) | baseline §5.2/§5.3/§6.3; Architect review §5/§6 |
| Definições surfaces | exactly 5: repairers (§10), machine assignments (§11), PDF directory (§12), email lists (§13), email templates (§14); routes 13–17, all gated `dmo.module.controlo-create` | baseline §9, §21.3 |
| Schema | exactly 8 P2-T05 tables, one migration, no seeds | baseline §16, §25 |
| Concurrency | version-guarded settings writes → `stale-version` (409) | baseline §19, §26.2 |

## 4. Authoritative current values (recovered from legacy production authority — NOT invented)

The accepted baseline's "values exist in no repository authority" applied to the **new modular**
repositories. The **legacy production engine** (the same authority family already used for the
water-density table) does carry the values. Recovered evidence:

| Source | Evidence |
|---|---|
| `BA-DMO` legacy catalog (authoritative fallback constants) | `src/BA.Dmo.Domain/Modules/Peso/PesoModuleCatalog.cs` — `public const decimal ConstantNnpb = 2.4027m;` / `public const decimal ConstantPs = 2.4231m;` |
| Legacy settings store | `peso_settings` rows `constant_nnpb` / `constant_ps` (jsonb settings table) — `CONTROLO_TECHNICAL_MODEL.md`: "constantes `constant_nnpb`/`constant_ps` (densidade do vidro)" with "fallback catálogo 2.4027/2.4231" |
| Legacy resolution (OC-6) | `src/BA.Dmo.Application/Modules/Peso/PesoService.cs` `ResolveProcessDensityAsync` — reads `constant_nnpb`/`constant_ps`; valid-positive parse wins, else catalog constant; "Calculation always uses the configured value — never a hardcoded constant in the calc path" |
| Legacy freeze | the used constant is persisted per control (`PesoControl.ConstanteGlassUsada`) and takes precedence on recalculation — settings changes "change only the future" (OC-6) |
| Legacy UI | `src/BA.Dmo.Web/Pages/Peso/Responsavel.cshtml` — editable "Densidades do vidro (NNPB / PS)" inputs (step 0.0001, placeholders `2,4027` / `2,4231`); hints: "Valores usados em novos cálculos do processo correspondente. A densidade efetivamente usada é preservada em cada controlo (histórico imutável)." and "As alterações afetam apenas cálculos futuros — nunca reescrevem histórico." |

**CONCLUSION (fixed by this contract):** the current operational values, as of the available
legacy production evidence, are **NNPB = 2.4027 g/cm³** and **PS = 2.4231 g/cm³**. They become the
**initial operational values** of the new Definições surface (§5.2). They are configuration
values — operator-editable afterwards — and are not invented defaults: they are recovered with the
citations above.

## 5. Required delta (normative — exactly this, nothing more)

### 5.1 Persistence — exactly ONE new table

`glass_density_settings` — the smallest coherent delta (sibling of the five existing Definições
settings tables; no reuse of an unrelated table, no dual source of truth):

| Column | Type | Contract |
|---|---|---|
| `processo` | `text` NOT NULL, PRIMARY KEY | the closed process classification; CHECK `processo IN ('NNPB','PS')` (named `glass_density_settings_processo_check`) |
| `density_g_cm3` | `numeric(18,4)` NOT NULL | the current operational glass density (g/cm³); CHECK `density_g_cm3 > 0` (named `glass_density_settings_density_check`) — same `numeric(18,4)` and positivity posture as the accepted Peso facts |
| `version` | `int` NOT NULL | version-guarded writes, same pattern as sibling settings (§19/§26.2 `stale-version`) |
| `created_at` / `updated_at` | timestamps | same pattern as sibling settings |

- No foreign key (processo is the closed two-value set owned by the domain `Processo` enum —
  exactly like `machine_repairer_assignments.machine`).
- No extra index: PK covers the only query shape (read both rows; read one row).
- The closed decision "8-table P2-T05 schema / no ninth table" is superseded **to the exact extent
  of this one table** (see §6.1).

### 5.2 Seed — the two recovered values, with provenance

The correction migration inserts exactly two rows with the recovered authoritative values:
`('NNPB', 2.4027, version 0)` and `('PS', 2.4231, version 0)`, with a provenance comment citing
§4. This is a deliberate, documented exception to the baseline's "no seeds" posture (§25),
superseded to this exact extent (§6.1): the surface starts operational and truthful
(calculation-dependent operations must not fail `calculation-configuration-missing` on the
normal path), and the values remain operator-editable from then on.

### 5.3 Definições surface — routes (extension of the 17-route table, all gated EXACTLY
`dmo.module.controlo-create`)

| # | Route | Behavior |
|---|---|---|
| 18 | `GET /controlo/create/definicoes/glass-densities` | returns both current operational values + versions: `[{"processo":"NNPB","densityGcm3":…,"version":…}, {"processo":"PS",…}]` — always exactly two rows |
| 19 | `PUT /controlo/create/definicoes/glass-densities/{processo}` | body `{ densityGcm3, expectedVersion }`; updates ONLY that processo's row (one processo never touches the other — MAC2–MAC4 posture); version-guarded: mismatch → `stale-version` (409), nothing written; success returns the new row/version |

- Refusals: `validation-failed` (400) with codes `PROCESSO_UNKNOWN` (path not `NNPB`/`PS`) and
  `DENSITY_NOT_POSITIVE` (value `≤ 0`); `stale-version` (409); `not-found` (404, defensive).
- The Definições page gains one section rendering the two rows with editable inputs
  (accepts positive values, ≤ 4 decimal places — legacy step 0.0001 posture), save per processo,
  and the existing success/conflict presentations (version bump on success; `stale-version` →
  the accepted `renderConflict` recovery, D2 pattern). Fixed desktop posture unchanged
  (baseline §24/Appendix C).

### 5.4 Calculation resolution change (the only calculation delta)

- Glass density resolution switches source: at calculate/save/create/submit time the backend reads
  the **current operational value** of the anchor's processo from the Definições settings store
  (`glass_density_settings`), via the existing Definições repository/service pattern
  (baseline §20.2/§20.3), instead of `Controlo:Calculation:GlassDensities`.
- `Controlo:Calculation:GlassDensities` **loses authority** for glass density (deployment entries
  are no longer read; section may be removed; no dual source of truth). The water-density override
  section `Controlo:Calculation:WaterDensities` and the built-in 31-value table are **unchanged**
  (baseline §5.3/Q-CALC; Owner water rule).
- Seam: `IControloCalculationConfiguration.TryGetGlassDensity` is replaced by (or re-pointed to)
  the operational-store read. The value read is `numeric(18,4)` positive — no rounding drift into
  the frozen `glass_density_g_cm3`.
- Fail-closed stays exact: a missing row (defensive only — rows are seeded) →
  `calculation-configuration-missing` (409), nothing written, nothing invented. With the seed in
  place, the glass side of MES9 is defensive; the water side is unchanged.
- **Freeze and immutability unchanged** (R4/R5): the resolved value is written once into
  `pesos.glass_density_g_cm3` at first successful calculate/save; submit re-derives with the frozen
  value; no path rewrites a Peso's frozen density or stored per-row results; later setting changes
  affect only new Pesos. Pesos created before this correction keep their frozen values untouched.

### 5.5 Audit metadata (existing infrastructure where available)

- The new table carries `version` + `created_at`/`updated_at` — the existing Definições metadata
  pattern (sibling tables; §19).
- The **historical audit of operative values is preserved by construction**: each Peso freezes the
  density actually used (`glass_density_g_cm3`) — the same historical-truth rule as legacy OC-6 —
  so every value ever applied remains visible per Peso; setting versions + timestamps cover the
  configuration side. The shared P2-T02 AuditTrail primitive may be attached by the
  implementation only if its accepted design chooses (documented in the implementation response);
  no new audit system is introduced and Admin audit stays out of scope.

### 5.6 Migration contract (baseline §25, amended to this extent)

- Exactly ONE new additive correction migration (the sixth migration overall, after
  `20260923045054_ControloCreateDomain`): creates `glass_density_settings`, inserts the two §5.2
  rows; `Down` drops the table; re-apply no-op.
- Prior migrations (001–004 and their Designer files), `DmoDbContext.cs`,
  `ModuleRegistrations.cs`, `DestinationRouteRegistrations.cs`: **byte-identical** (protected-file
  rule, baseline Appendix A / §25.3). No column on `pesos` or `peso_measurement_rows` changes.
- No availability registration, no destination/navigation entry (baseline §21.6/§22;
  `CurrentBuildAvailable` stays `[]`; P2-T10 owns availability).

### 5.7 Required tests (implementation evidence, baseline §26.4 style)

- **U** — resolution returns the current operational value per processo; absent row → fail-closed
  `calculation-configuration-missing`; `DENSITY_NOT_POSITIVE` validation; version-guard logic.
- **I/DB** — create+save a Peso → `glass_density_g_cm3` == setting at that moment; change the
  setting → the existing Peso's read model (density + per-row results) **unchanged**; a new Peso
  uses the new value; `stale-version` PUT → 409, nothing written; per-processo independence;
  routes 18/19 gated `dmo.module.controlo-create` (an `controlo-approve`-only caller denied).
- **R** — Definições page renders the two editable rows with current values and
  success/conflict/recovery presentations (D2 pattern); frozen density still shown on the Peso
  surface ("Densidade usada").
- **S** — no per-`tool_id` density token anywhere; `Controlo:Calculation:GlassDensities` not read
  (authority removed); protected files byte-identical; exactly one new migration; no ninth-table
  beyond `glass_density_settings`; no availability registration.
- **Regression** — full unit + integration suites green on disposable PostgreSQL; prior targeted
  P2-T05 matrix rows stay green (the response reports exact counts/deltas).

## 6. Closed-decision reconciliation (explicit)

### 6.1 Superseded by this Owner rule (exact extent)

| Closed decision | Superseded to |
|---|---|
| "Glass density mapping values arrive as backend calculation configuration `Controlo:Calculation:GlassDensities`" (baseline Q-CALC / §5.3 mechanism) | the operative values are maintained in `Controlo → Definições` (§5.1–§5.4); the config section loses authority |
| "8-table P2-T05 schema / no ninth table" | exactly one additional Definições settings table — `glass_density_settings` (§5.1); nothing else |
| "No seeds in the P2-T05 migration" (baseline §25) | exactly the two provenance-documented rows (§5.2) |

### 6.2 NOT reopened (unchanged, still binding)

- `tool_id` vs `cm_id` identities; `cm_id` frozen Job On context; P2-T05 never creates `cm_id`;
  no `production_id`; normal anchor `cm_id`, pending `tool_id`; pending→associated preserves
  `peso_id`; Peso aggregate owns its rows.
- Water temperature is operator input; water density automatically resolved; 31 values 5–35 °C;
  AwayFromZero nearest-whole-degree; no interpolation.
- Water density and glass density remain **separate**; no manual water-density/divisor field.
- Frozen calculation semantics (glass-density freeze, no silent recalculation) — restated, not
  changed (§5.4).
- `Controlo_Create` and Definições belong to P2-T05; approval NOT part of P2-T05; PDF
  generation/send NOT part of P2-T05; `CurrentBuildAvailable` remains `[]` after this correction.
- The 17 accepted routes and their gating; the five existing Definições surfaces; all other
  schema/constraints/checks; failure vocabulary (extended only by `PROCESSO_UNKNOWN` /
  `DENSITY_NOT_POSITIVE`); D3/D4 carry-forward (still non-blocking, untouched).

## 7. Explicitly NOT in this contract

- No per-`tool_id` density, no per-lot/line/machine density, no density history UI, no delete
  path for settings rows.
- No change to water density, temperature input, or any Peso identity/anchoring/status behavior.
- No approval, PDF, email, availability, navigation or module-registration work.
- No amendment of the accepted baseline contract `b38993f` itself beyond the supersession table
  above; no reopening of comparation/Pegamentos/Folha/Resumo or read-model rendering.
- No implementation is performed by this task at all.

## 8. Planning-gate record

| Item | Value |
|---|---|
| Contract file | `plans/contracts/P2-T05_CONTROLO_CREATE_GLASS_DENSITY_CORRECTION_CONTRACT.md` (this file) |
| Authoring commit | `9e1e211` (this commit; full SHA recorded on DMO-MODULAR remote `main`) |
| Owner-rule record | dmo-work `dev/rulings/P2_T05_GLASS_DENSITY_CONFIGURATION_OWNER_RULE.md` @ `409ac24…` |
| Baseline | accepted contract `b38993f`; CLOSED implementation `9fbfcf4`; closure `3491097…` |
| DMO-MODULAR remote `main` before this task | `f5ab56d28fffb22541907f396d8eff06215a99f4` |
| Implementation performed | **NONE** (docs-only planning commit) |
| Status | **AUTHORED — AWAITING ARCHITECT PLAN REVIEW** (PLAN ACCEPT / corrections / reject) |