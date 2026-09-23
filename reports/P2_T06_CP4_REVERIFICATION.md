# P2-T06 — Controlo Approve — FOCUSED RE-VERIFICATION (CP4 correction)

**Workstream:** P2-T06 — Controlo_Approve (Aprovar + Histórico de Pesos).
**Task class:** focused independent re-verification of the sole last-blocking defect (contract
§26.4 matrix row CP4) and its minimal regression surface. **No full P2-T06 audit repeated; no
implementation change; no Architect review; no closure; no P2-T07/P2-T08/P2-T10 work.**
**Verdict:** **VERIFIED** — CP4 correction accepted; P2-T06 is READY FOR ARCHITECT IMPLEMENTATION
REVIEW. Original P2-T06 remains NOT CLOSED until Architect review and governance closure.

---

## 0. Verdict summary

| Item | Result |
|---|---|
| Focused correction SHA | `e784d0dadc64d515b350b744218ea5c8ab76dc1d` |
| Original verification | NOT VERIFIED — **CP4 only** (report `reports/P2_T06_CONTROLO_APPROVE_VERIFICATION.md` @ `8c49404`) |
| CP4 (matrix row implemented) | **PASS** |
| Supplied-carrier composition | **PASS** |
| No-carrier behavior | **PASS** |
| Dormancy | **PASS** |
| Deferred Comparação boundary | **PASS** |
| Negative-scope scan | **PASS** |
| P2-T06 matrix | **67 / 67** |
| Build | **PASS** — 0 errors (10 xUnit-analyzer warnings, same disclosed set as original) |
| Unit | **609 / 609 PASS** (was 605; +4 CP4) |
| Integration | **568 PASS / 2 SKIPPED** (pre-existing live-Supabase skips only) |
| Schema unchanged | **PASS** |
| Routes unchanged | **PASS** |
| Authorization unchanged | **PASS** |
| CurrentBuildAvailable | `[]` |
| Working tree | clean after committing only this report |

P2-T06 remains **NOT CLOSED** (no gate is closed by this report).

---

## 1. Reviewed objects / commit verification

| Item | Value |
|---|---|
| Contract | `plans/contracts/P2-T06_CONTROLO_APPROVE_CONTRACT.md` — §17.2 (carrier truth), §26.4 matrix rows CP1–CP4 (comparação block), §30 AC-CP1…AC-CP4, Q-COMP (§27.2), §25 migration contract — re-read in full for these sections |
| Original verification report | `reports/P2_T06_CONTROLO_APPROVE_VERIFICATION.md` @ `8c49404` — read completely; sole defect = row CP4 absent |
| Correction response | `dev/responses/P2_T06_IMPLEMENTATION_RESPONSE.md` §14 — read completely (§14.1 verification record, §14.2 seam, §14.3 tests, §14.4 scan refinement, §14.5 correction boundary, §14.6 totals) |
| **Focused correction SHA** | `e784d0dadc64d515b350b744218ea5c8ab76dc1d` (local `main` == `origin/main` == this SHA; working tree clean) |
| Correction ancestry | parent of `e784d0d` == `8c49404` (verification report commit), descendant of original implementation `e527ade` — chain and SHAs independently verified |
| Correction diff surface | `git diff 8c49404 e784d0d` = **exactly 5 files**: `src/DMO.Application/ControloApprove/ComparisonComposer.cs` (new), `tests/DMO.UnitTests/ControloApprove/ComparisonCompositionTests.cs` (new), `tests/DMO.IntegrationTests/ControloApprove/P2T06ProductionScan.cs` (+12 helper lines), `tests/DMO.IntegrationTests/ControloApprove/P2T06RegressionTests.cs` (BND6 refinement), `dev/responses/P2_T06_IMPLEMENTATION_RESPONSE.md` (docs). `git diff --check` clean |
| Untouched by the diff | every `src/DMO.Web/**` file, every migration + `DmoDbContext.cs`, `PersistenceServiceCollectionExtensions.cs`, `Program.cs`, all access/policy files, all other test files |

---

## 2. The new composition seam — PASS

`src/DMO.Application/ControloApprove/ComparisonComposer.cs` (82 lines, read completely) is:

- **application-level** — lives in `src/DMO.Application/ControloApprove/`, namespace
  `DMO.Application.ControloApprove` (contract Appendix B surface);
- **pure** — `public static class ComparisonComposer` with the single static function
  `ComparisonComposition? Compose(ComparisonCarrier? carrier)`; no state, no I/O, no clock, no
  randomness; output is a deterministic function of the argument only;
- **read-only / non-persistent** — the file references no repository, no
  `DbContext`/`DbSet`, no query, no `SaveAsync`, no transaction, no route primitive
  (independently verified by reading the file; the BND6 dormancy assertion pins the same
  token list in the scan: `Repository`, `DbContext`, `DbSet`, `MapGet`, `MapPost`,
  `MapGroup`, `RequireAuthorization`, `SaveAsync`, `Transaction`, `SqlQuery`,
  `ExecuteUpdate` → 0 occurrences);
- **dormant today** — repository-wide grep for `ComparisonComposer|ComparisonCarrier|
  ComparisonComposition|ComparisonPesoContext` over `src/**` returns **only the seam file
  itself** (declaration/definition); **zero production call sites**, zero DI registration,
  zero mention in `Program.cs` or `PersistenceServiceCollectionExtensions.cs` (both also
  byte-untouched by the diff);
- **not injected into the production workflow** — no service registration, no endpoint, no
  page consumes it; the review sheet path (`ReviewSheetReadModel` + shared
  `PesoSheetReadModel`) is untouched, so CP3 (no comparison region rendered today) is
  undisturbed;
- **not a producer of Comparação facts** — it only consumes a supplied carrier and returns a
  composition; it can create nothing, search nothing, persist nothing.

**Conceptual shape confirmed exactly as expected:**

```
ComparisonPesoContext(PesoId, Reference?, ProductionNumber?, SubmittedAt?)   // one side
ComparisonCarrier(Current, Previous)                                         // supplied input
ComparisonComposition(Current, Previous)
    CurrentPesoId   => Current.PesoId
    PreviousPesoId  => Previous.PesoId
ComparisonComposer.Compose(ComparisonCarrier?) => ComparisonComposition? | null
```

The types preserve **only supplied information**: `ComparisonComposition` carries the same two
context records it was given (same instances — nothing copied, rebuilt, completed or inferred)
and exposes the two ids as derived properties of the supplied sides. There is no other id
anywhere in the carrier, so substitution is unrepresentable by construction.

---

## 3. Supplied-carrier behavior — PASS

Independently proven at code level (the function's entire body is one null-check and one
constructor call) **and** behaviorally by the unit tests:

1. **exact supplied previous Peso identity preserved** — `PreviousPesoId => Previous.PesoId`
   — the only possible value is the supplied previous side's id (test 1 asserts equality with
   the supplied id on both `PreviousPesoId` and `Previous.PesoId`);
2. **exact current Peso identity/context preserved** — `CurrentPesoId => Current.PesoId`;
   context retained by instance (`Assert.Same`), record equality and every fact verbatim
   (test 1 + test 2);
3. **exact previous context preserved** — same proofs on `Previous` (test 2);
4. **NULL/optional supplied facts stay unchanged** — passthrough performs no completion; the
   sparse-carrier test asserts `Reference`/`ProductionNumber`/`SubmittedAt` remain `null` on
   both sides (test 2);
5. **no other Peso can be substituted** — the carrier exposes exactly two ids (current,
   previous); the composed output has no additional id source; the "tempting alternatives"
   carrier (recent `SubmittedAt`, current `Reference` on the current side) never influences
   the relation (tests 1 + 3);
6. **no lookup occurs** — the function body is `carrier is null ? null : new
   ComparisonComposition(carrier.Current, carrier.Previous)`; there is no query surface in
   the file (see §2 dormant-token assertion);
7. **no fallback occurs** — a single branch: carrier → exact composition; no carrier → null.
   There is no second source to fall back to;
8. **no heuristic occurs** — no "latest", date, machine, reference or candidate-selection
   logic anywhere in the file (also CP1 scan: the P2-T06 code sources contain no
   `latest`/`LatestPeso`/`PreviousWeight`/`pair`/`Pairing`/`reconstruct` token outside
   comments; the seam's doc comments are masked by the comment-aware scanner, and its real
   code has none of them either);
9. **input/carrier is not mutated** — the records are C# `record` types (immutable by
   construction) and the tests assert every supplied fact unchanged after composing
   (test 3);
10. **output derives only from the supplied carrier** — composing the same carrier twice
    yields an equal result (deterministic, no hidden state) and a different supplied
    previous id yields a different output (test 3).

No "latest Peso", date, machine, reference or candidate-selection logic exists in the seam.

---

## 4. No-carrier behavior — PASS

`Compose(null)` returns `null` — verified by reading the single-branch body and by
`CP4_WithoutACarrier_NothingIsComposed_NoSyntheticComparisonExists`.

No attempt to manufacture: `previous_peso_id` (nothing to derive it from — no query, no
default), previous Peso context (no synthesis), comparison state (no object is constructed).
This is exactly §17.2's "today: no carrier exists ⇒ the review surface renders no comparison
region" pinned at the composition function level (AC-CP4: no fabricated relation, no
synthetic comparison state).

---

## 5. CP4 test — PASS (genuine behavioral coverage)

`tests/DMO.UnitTests/ControloApprove/ComparisonCompositionTests.cs` (140 lines, read
completely) — **4 tests, all executed green (4/4)**, all exercising the REAL
`ComparisonComposer.Compose` function (no mocks, no token/shape-only assertions; the
assertions are behavioral: `Assert.IsType<ComparisonComposition>` on the actual return,
`Assert.Same`/`Assert.Equal` on actual instances and values):

| Claimed coverage | Test | Evidence |
|---|---|---|
| exact previous id | `CP4_WithASuppliedCarrier_TheExactSuppliedPreviousPesoIdIsRetained` | `PreviousPesoId == PreviousPesoId` (supplied id `66666666-...`), plus `Previous.PesoId` and both current-side ids |
| current/previous context retention | `CP4_WithASuppliedCarrier_CurrentAndPreviousContextAreRetainedExactly` | `Assert.Same` (same instances), record equality, every fact verbatim (`Reference`/`ProductionNumber`/`SubmittedAt` both sides) |
| NULL supplied facts kept NULL | same test | sparse carrier → all six context members asserted `Null` on both sides; `PreviousPesoId` still exact |
| no substitution / no fallback | `CP4_WithASuppliedCarrier_NoSubstitutionNoFallbackNoMutationAndNoHiddenInput` | "tempting alternatives" (recent submission, current reference) never used; different supplied previous id → different output |
| immutability / determinism | same test | carrier facts asserted unchanged after composing; same carrier twice → `Assert.Equal` results; different carrier → `Assert.NotEqual` previous id |
| no-carrier branch | `CP4_WithoutACarrier_NothingIsComposed_NoSyntheticComparisonExists` | `Assert.Null(ComparisonComposer.Compose(carrier: null))` |

Row CP4 of §26.4 requires "a unit test over the composition function with a supplied carrier"
proving exact id + context, no fallback/substitution/mutation — satisfied by the above, which
also proves AC-CP1 (when the carrier exists branch), AC-CP2 (context from the relation, never
inference) and AC-CP4 (carrier absence recorded, nothing fabricated).

---

## 6. Deferred-CORRECTLY boundary — PASS (no actual Comparação persistence)

The correction introduced **no** Comparação persistence — all independently verified:

- **no `previous_peso_id` DB column** — `information_schema.columns` on the fresh disposable
  database after the full suite's migration cycle: the only `previous_*` columns in `pesos`
  are the closed P2-T05 facts `previous_production_end_reference` and
  `previous_average_weight_reference` (reference data, not a comparison relation); the
  migration sources contain no `previous_peso_id` text (grep);
- **no comparison table** — 21 public tables, exactly the closed 20 + `peso_review_decisions`
  (full table register inspected); the migration source contains exactly one
  `CreateTable` (BND6 assertion: 1 created table == `peso_review_decisions`);
- **no comparison repository** — no new repository type; `PesoReviewRepository` (the only
  P2-T06 repository) touches only lifecycle columns + the decision table (R2/O5 scan
  unchanged, passing);
- **no comparison persistence service** — nothing registered in
  `PersistenceServiceCollectionExtensions.cs` (file byte-untouched by the diff);
- **no comparison endpoint** — the 9-route surface is unchanged (`ControloApproveEndpoints.cs`
  untouched; no `MapGet`/`MapPost`/`MapGroup` in the seam);
- **no comparison mutation** — the seam has no write surface; the decision routes remain the
  only mutation paths and still write no pairing (CP2/CP3 scans unchanged, passing);
- **no candidate lookup, no automatic previous-Peso resolution** — the seam cannot query;
  there is no code anywhere in P2-T06 that resolves a previous Peso (CP1 scan + reading);
- **no DI registration making the seam an active producer** — grep over `src/**` for the
  seam types returns only the seam file itself.

The seam exists only to define composition behavior WHEN a future authoritative carrier is
supplied — exactly the §17.2 / Q-COMP "DEFERRED-CORRECTLY" posture: **no relation to read
today; exact-relation read contract pinned for the future carrier; no heuristic, no latest,
no mutation**.

---

## 7. Dormancy — PASS

Repository-wide grep over `src/**`:

- `ComparisonComposer` → only `src/DMO.Application/ControloApprove/ComparisonComposer.cs`
  (definition; no call site);
- `ComparisonCarrier` / `ComparisonComposition` / `ComparisonPesoContext` → only the same
  file.

Consumers exist **only** in test/contract evidence: `ComparisonCompositionTests.cs` (4 calls)
and the scan pins (`P2T06ProductionScan.ComparisonCompositionSeamPath`,
`P2T06RegressionTests` BND6). **No active production call site.** The seam is dormant; zero
production behavior change (the review sheet, routes and services are byte-untouched).

---

## 8. Negative-scope scans — PASS (honest refinement)

**The carve-out is intentional, minimal and pinned:**

- `P2T06ProductionScan.ComparisonCompositionSeamPath` names **exactly one file**:
  `src/DMO.Application/ControloApprove/ComparisonComposer.cs`;
- BND6 applies the deferred-carrier token list (`previous_peso_id`, `PreviousPesoId`,
  `controlo_sheet_id`, `ControloSheetId`, `comparison`, `Comparison`, `per_cm`, `PerCm`,
  `cm_decision`) to **every other** P2-T06 production source — unchanged breadth minus the
  one pinned file;
- the carve-out **cannot shelter an unrelated file**: BND6 asserts the seam file exists AND
  that its real code exposes `PreviousPesoId` (the pinned member) — the seam genuinely is the
  contract pin;
- BND6 asserts the seam's dormancy/purity token list (see §2) — the carve-out does not admit
  any persistence/route/query surface;
- the migration assertions are unchanged (exactly 1 created table ==
  `peso_review_decisions`), so the schema-level negative protection is intact.

**Independent cross-check (this verifier):** raw grep of the deferred-carrier tokens over all
P2-T06-owned production sources (Application/ControloApprove, Domain/Controlo, the three
persistence files, migration, Web pages + endpoints + assets, `Program.cs`, DI extensions):

- every raw hit of `comparison`/`Comparison` in P2-T06 files is a substring of
  `StringComparison.Ordinal` — excluded by the whole-token boundary rule of the
  comment-aware scanner (`(?<![A-Za-z0-9_])…(?![A-Za-z0-9_])`);
- `PerCm` raw hits are substrings of the pinned constants type name
  `PerCmDecisionVocabulary` (also excluded by the boundary rule; D3's per-CM vocabulary scan
  is unchanged and passing);
- raw hits in `Peso.cs`, `PesoEntity.cs`, `PesoRepository.cs`, `EmailListRepository.cs`,
  `EmailTemplateRepository.cs`, `JobOnRepository.cs`, `ToolRepository.cs`,
  `UserRepository.cs` are **outside** the P2-T06 `ProductionSourcePaths` set (closed P2-T05
  / P2-T02 files, not P2-T06-owned sources);
- the ONLY P2-T06 production code containing whole-token deferred-carrier identifiers is the
  pinned seam file itself (`PreviousPesoId`, `Comparison*` type names).

All other negative-scope scans (BND2/BND3/BND1-settings/BND4/H4/BND7/BND8/I4/I5/D2/D3/CP1/
R2/O5/L1/L3/MG1) still pass over the P2-T06 source set **including** the new seam file — the
seam's real code contains none of their tokens (the seam's doc comments would be masked by
the comment-aware scanner; its code carries none of them anyway). Negative-scope protection
is not weakened; the refinement turns a previously over-approximated blanket token ban (which
made row CP4 unimplementable in source) into the contracted single-file pin with dormancy
proof.

---

## 9. Regression execution (this verifier, fresh disposable PostgreSQL only)

| Check | Result |
|---|---|
| CP4 unit tests (filtered) | **4 / 4 PASS** |
| Full Unit suite | **609 / 609 PASS** (0 skipped) — exactly the claimed 609 (605 + 4) |
| Full Integration suite | **568 PASS / 2 SKIPPED** on fresh disposable PostgreSQL 16.15 (`dmo_reverify_cp4`, created for this run) — the 2 skips are the pre-existing live-Supabase auth tests (`LiveDevTestSupabaseUserAuthTests`, `LiveDevTestSupabaseAdminAuthTests`; `DMO_SUPABASE_LIVE_TEST` not set), same two skips as the original verification |
| P2-T06 targeted integration tests (filter `DMO.IntegrationTests.ControloApprove`) | re-run separately; all green (included in the 568) |
| Build | `dotnet build DMO.slnx -t:Rebuild` (full clean rebuild): **0 errors**; 10 warnings = 9 xUnit-analyzer in new P2-T06-owned test files + 1 pre-existing in `P2T02RegressionTests` — same disclosed set as the original verification |
| Negative-scope scan | PASS (`BND6_NoComparisonFolhaPerCmOrSendCarrierIsInvented` + all other regression rows green; independent raw-token cross-check in §8) |
| Migration count / schema | **unchanged** — `__EFMigrationsHistory` = 6 rows (001–005 + `20260923171223_ControloApproveDomain` last); 21 public tables; no `previous_peso_id` column, no comparison table (live `information_schema` inspection on the fresh DB after the suite's migration cycle); migration sources + `DmoDbContext.cs` byte-untouched by the diff and MG1 hash-pinned test green |
| Routes | **unchanged** — zero `src/DMO.Web/**` bytes changed by the correction; the 9-route surface (2 pages + 7 endpoints) is the one already verified |
| Authorization | **unchanged** — zero access/policy file bytes changed; A1–A6 tests green in the 568 |
| `CurrentBuildAvailable` | `[]` in source (`ModuleRegistrations.cs`), asserted at runtime by BND1 (green) |
| Migration internals (`Down`/re-apply, reopen, read-model parity, historical freeze, access architecture) | **not re-audited** — the CP4 diff touches none of those areas (per task scope); previously verified state stands |

Per the task scope, the previously-verified areas (reopen, read-model parity, historical
freeze, access architecture, migration internals) were re-run only through the full suites
(untouched by the diff; all green) and not manually re-audited.

---

## 10. Matrix realization — 67 / 67

- §26.4 declares 67 rows (IDENTITY 7 + ACCESS 6 + READ-ONLY 5 + DECISIONS 6 + REJECT 3 +
  REOPEN 5 + COMPARAÇÃO 4 + PENDING 4 + HISTORY 5 + CONCURRENCY 5 + RENDERER 3 + MIGRATION 3 +
  DESKTOP 3 + BOUNDARIES 8 = 67); the correction adds the missing row:
  - **CP1** → `P2T06RegressionTests.CP1_NoHeuristicPreviousPesoLogicExists` (unchanged);
  - **CP2** → `BND6_NoComparisonFolhaPerCmOrSendCarrierIsInvented` + MG1 + R2/O5 (unchanged,
    passing — migration scan + update audit);
  - **CP3** → `ControloApproveEndpointsTests.CP3_TheReviewSheetCarriesNoComparisonRegionAndNoPreviousPesoRelation`
    (unchanged, passing);
  - **CP4** → `ComparisonCompositionTests` (4 tests, class `CP4_*`, unit over the composition
    function with a supplied carrier) — **new, green**;
- 61 AC ↔ 67 rows completeness argument (missing=dangling=orphan=0) is unchanged by the
  correction (test-only + scan-helper changes);
- the implementation response's claim **"67/67 rows proven"** is now accurate.

---

## 11. Claims cross-check (correction totals vs independently reproduced values)

| Claim (`e784d0d` response §14.6) | Independently reproduced |
|---|---|
| P2-T06 matrix 67/67 | ✅ 67/67 (CP4 row now implemented and green) |
| Unit 609/609 | ✅ 609/609 PASS (0 skipped) |
| Integration 568 PASS / 2 skipped | ✅ 568 PASS / 2 SKIPPED (same two live-Supabase skips) |
| Build PASS | ✅ 0 errors; 10 analyzer warnings (same set as original verification) |
| Schema changed: NO | ✅ 6 migrations / 21 tables; no previous_peso_id column; no comparison table |
| Routes changed: NO | ✅ zero Web byte-change; 9-route surface intact |
| Authorization changed: NO | ✅ zero access/policy byte-change; A1–A6 green |
| CurrentBuildAvailable: [] | ✅ `[]` in source + runtime assertion green |
| Working tree clean | ✅ clean at inspection; only this report added |

---

## 12. Output record

```text
focused correction SHA:         e784d0dadc64d515b350b744218ea5c8ab76dc1d (HEAD == origin/main)
original verification:          NOT VERIFIED — sole defect CP4 (report 8c49404)
verdict:                        VERIFIED (focused re-verification, CP4 + minimal regression surface)
CP4:                            PASS (seam + 4 unit tests over the real composition function)
supplied-carrier composition:   PASS (exact ids/context; same instances; NULL preserved; no
                                substitution/lookup/fallback/heuristic/mutation; deterministic)
no-carrier behavior:            PASS (Compose(null) → null; nothing manufactured)
dormancy:                       PASS (zero production call sites; no DI; review sheet untouched)
deferred Comparação boundary:   PASS (no column/table/repository/service/endpoint/mutation/
                                lookup/auto-resolution; Q-COMP remains DEFERRED-CORRECTLY)
negative-scope scan:            PASS (single pinned seam carve-out; every other source token-free;
                                seam existence + PreviousPesoId + dormancy asserted; migration
                                assertions unchanged; independent raw-token cross-check clean)
P2-T06 matrix:                  67 / 67 rows
build:                          PASS (0 errors; 10 disclosed analyzer warnings)
unit:                           609 / 609 PASS
integration:                    568 PASS / 2 pre-existing live-Supabase SKIPS
                                (fresh disposable PostgreSQL 16.15, dmo_reverify_cp4)
schema unchanged:               PASS (6 migrations; 21 tables; no comparison relation)
routes unchanged:               PASS
authorization unchanged:        PASS
CurrentBuildAvailable:          []
working tree:                   clean before report; only this report added by the verifier
```

---

## 13. Governance state after this re-verification

| Item | Status |
|---|---|
| P2-T06 | **VERIFIED (focused re-verification) — READY FOR ARCHITECT IMPLEMENTATION REVIEW** (NOT CLOSED) |
| Original P2-T06 | **NOT CLOSED** — closure requires Architect implementation review + governance closure |
| P2-T05 + correction slice | CLOSED (unchanged) |
| P2-T07 / P2-T08 / P2-T10 | NOT AUTHORIZED (no work performed) |
| Supabase / live TEST | untouched (disposable PostgreSQL only — `dmo_reverify_cp4` on the local `postgres:16-alpine` container) |
| Implementation modified | **NO** (report only) |
| `ModuleRegistrations.CurrentBuildAvailable` | `[]` (unchanged) |

**Verdict: VERIFIED.**

P2-T06 is **READY FOR ARCHITECT IMPLEMENTATION REVIEW**. Original P2-T06 remains **NOT
CLOSED** until Architect review and governance closure.

NEXT GATE: Architect implementation review of P2-T06.