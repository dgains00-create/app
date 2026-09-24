# P2-T04 — Job On Context Snapshot Invariant — FOCUSED INDEPENDENT REVIEW (OWNER CLARIFICATION)

**Workstream:** P2-T04 — Domain Core Tool + Job On.
**Task class:** one focused independent review of the OWNER clarification — "every new Job On
(normal creation AND duplication) creates NEW CM/MF/BQ context snapshots from the CURRENT canonical
`tool_id` state". P2-T04 is NOT reopened as a whole; no other Job On behavior was reviewed; **no
implementation was modified** (report only).
**Verdict:** **VERIFIED**

---

## 0. Verdict summary

| Item | Result |
|---|---|
| Implementation SHA | `d2b3b3c5b91b7f2a4ffec0bcd609b6c115e322fc` |
| Governance / response SHA | `b7457ecdae1b92e99b7814081374ebc67c7aa2ae` (records only; `local main` == `origin/main`; working tree clean) |
| Baseline before this work | `a96814f` (P2-T07 final independent review) |
| Normal creation fresh snapshot | **PASS** |
| Duplication fresh snapshot | **PASS** |
| Current Tool state used | **PASS** |
| Context IDs never reused | **PASS** |
| Source immutable | **PASS** |
| Multiple duplication identity | **PASS** |
| BQ/P2-T07 identity regression | **PASS** |
| Negative scope | **PASS** |
| Build | 0 errors (+ 12 warnings, all disclosed pre-existing analyzer warnings in P2T02/P2T06 protected + PesoReview files; 0 warnings in all changed code) |
| Full unit suite | **658 / 658 PASS** |
| Focused snapshot/duplication rows (disposable PostgreSQL 16.15) | **18 / 18 PASS** (DUP2–DUP10, DUP12, DUP14, DUP17, DUP18 + CTX14 + SNAP1–SNAP4; the same filter run also passed the 2 adjacent Tool-repository rows MIG10/SNA5 — 20/20 in that run) |
| P2-T04 regression block | **18 / 18 PASS** |
| P2-T07 Boquilhas regression block | **67 / 67 PASS** |
| Schema evidence | 7 migration ids / 23 product tables on a fresh disposable PostgreSQL — identical to the pre-clarification P2-T07 final state; **zero schema change** |

**FINAL STATUS:**
**OWNER CLARIFICATION CLOSED**

---

## 1. Reviewed objects / commit verification

| Item | Value |
|---|---|
| Owner clarification | `plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md` **§23** — OWNER CLARIFICATION — JOB ON CONTEXT SNAPSHOT INVARIANT: §23.1 normative rule (1 new `jobon_id` ⇒ new `cm_id`/`mf_id`/`bq_id`; 2 snapshot source = current canonical Tool row; 3 duplication reuses `tool_id`, never the previous context; 4 historical contexts stay immutable; 5 P2-T07 interaction — new Job On ⇒ new `bq_id`, movements never migrated), §23.2 explicit superseded-wording list (former §10.3 step 6, former §10.5 bullet, former §21 Q16 default (a), former §20.4 DUP7, former §20.3 CTX14 duplication reading), §23.3 deliberately-not-introduced list |
| Response record | `dev/responses/P2_T04_JOB_ON_SNAPSHOT_OWNER_CLARIFICATION_RESPONSE.md` — read in full; every reported fact independently reproduced below |
| Implementation diff surface | `git diff a96814f d2b3b3c`: 9 files — contract §23 (+superseded marks), `src/DMO.Application/JobOn/JobOnService.cs`, `src/DMO.Application/Repositories/IJobOnRepository.cs` (doc-only), `src/DMO.Infrastructure/Persistence/JobOnRepository.cs` (remarks/comments only), `src/DMO.Web/Pages/JobOn/Duplicate.cshtml` (comment + operator hint), tests (SNAP1–SNAP4, DUP7 flipped, DUP17/DUP18 added, CTX14 updated, `P2T04TestStore` comment) |
| Governance diff surface | `git show b7457ec`: response file + master plan B1/§7 + workstream §5.2 records only — no `src/`, no `tests/`, no migrations |
| Protected surface | between `a96814f` and `HEAD`, the ONLY `src/` changes are the 4 clarification files above; migrations, `DmoDbContext.cs`, ModuleCatalog/policies, Boquilhas surface, routes and `ModuleRegistrations.cs` untouched |
| Source review | read completely: `JobOnService.CreateAsync` / `DuplicateAsync` / `ResolveToolAsync` / `BuildFichaAsync`, `JobOnRepository.CreatedAsync` / `DuplicatedAsync` / `InsertContextFromLiveToolAsync` / `InsertContextCopy` / `InsertContext`, `ToolContext` / `ToolContextSnapshot` (domain), `ToolTokens.RequiredToolType`, the SNAP/DUP/CTX14 test rows and the `P2T04TestStore` double |

---

## 2. Invariant 1 — NORMAL CREATE — **PASS**

Code path (`JobOnService.CreateAsync`, lines 102–126): for each explicitly selected slot `tool_id`
(CM/MF/BQ) the service resolves the Tool through `ResolveToolAsync(contextType, toolId)` which reads
the LIVE canonical Tool (`_tools.GetByIdAsync`) and composes `ToolContextSnapshot(tool.Type,
tool.Reference, tool.Lot)` — the accepted frozen set of §7.2, nothing else. Every context receives a
fresh identity (`Guid.NewGuid()`), and the repository re-reads the live Tool a second time INSIDE the
write transaction (`JobOnRepository.InsertContextFromLiveToolAsync`) so the persisted snapshot is
authoritatively the current canonical row.

Proof rows (all executed in this review):

- **SNAP1** — create from scratch with CM/MF/BQ selected ⇒ 3 NEW distinct context ids, each pointing
  at the selected canonical `tool_id`, each frozen to the CURRENT Tool triple (`PASS`).
- **CTX14** (create leg) — a create after a Tool metadata change freezes the live values at selection
  time; a later Tool change is never reflected in the already-created context (`PASS`).

## 3. Invariant 2 — DUPLICATE — **PASS**

Code path (`JobOnService.DuplicateAsync`, lines 292–313): for EACH source context the service
resolves `context.ToolId` against the live canonical Tool through the SAME
`ResolveToolAsync(context.ContextType, context.ToolId.Value, …)` path normal creation uses, and
builds a NEW `ToolContext` with a fresh `Guid.NewGuid()` identity and `resolution.Frozen!` — a NEW
snapshot of the CURRENT canonical Tool row at duplication time. The source context contributes ONLY
its `tool_id`; `context.Frozen` of the source is never read on the snapshot path. The repository
(`DuplicatedAsync` → `InsertContextCopy`) persists exactly the supplied service-built snapshots —
it never clones the source context's frozen triple (confirmed in `JobOnRepository.cs` lines 316–323
and 642–651). **Duplication does NOT use the frozen source snapshot as the new snapshot source.**

Proof rows (all executed):

- **SNAP2** — duplicate with all three Tools changed after the source recorded its snapshots ⇒
  duplicate carries the POST-change values on fresh identities (HTTP endpoint through the real
  service) (`PASS`).
- **DUP7** — same scenario against the REAL schema over disposable PostgreSQL on all three context
  tables (`cm_contexts`/`mf_contexts`/`bq_contexts`); the duplicate row holds `{reference}-live|99-live`
  while the source context row keeps its historical triple and the live Tool row holds the new values
  (`PASS`).
- **CTX14** (duplication leg) — Raw-SQL Tool metadata change, then duplication via the real
  `JobOnService`/repository pair: the duplicated `cm_contexts` row contains `{updatedReference}|{updatedLot}`,
  the source row still contains `{reference}|01` (`PASS`).

## 4. Invariant 3 — IDENTITY — **PASS**

- New context identities: service issues `Guid.NewGuid()` per duplicated context; repository persists
  `context.ContextId` as the row's `cm_id`/`mf_id`/`bq_id` (legacy-free, per context table).
- `cm_id_new != cm_id_source`, `mf_id_new != mf_id_source`, `bq_id_new != bq_id_source`: proven by
  **SNAP2** (Assert.All per type), **SNAP3**, **SNAP4**, **DUP4** (every duplicated context has a new
  id, count matches the source), **DUP17** (per-table ids of source/B/C all pairwise distinct),
  **DUP18** (source `bq_id` ≠ duplicated `bq_id`).
- Same canonical `tool_id`: **SNAP1/2/3/4** assert `tool_id` equality between source context and new
  context per type; **DUP6** proves it on the real schema for every context table.

## 5. Invariant 4 — CURRENT-STATE PROOF — **PASS**

Executed end-to-end on the disposable PostgreSQL (fresh database `dmo_review`, migrations applied
from scratch):

1. Create source Job On (CM/MF/BQ) — **SNAP2/DUP7** arrange exactly this.
2. Change an allowed canonical Tool fact (reference/lot of the canonical Tool row; the allowed set is
   the frozen triple fields) after the source recorded its snapshots.
3. Duplicate the source.
4. Assert: the new context contains the UPDATED canonical Tool value (`5447T173-live` / `{reference}-live|99-live`),
   while the source context still contains the OLD frozen value (`5447T173` / `{reference}|01`).

Both the HTTP/service path (SNAP2) and the real repository path (DUP7, CTX14) prove the updated
value lands in the new context and the old value stays in the source. **PASS.**

## 6. Invariant 5 — SOURCE IMMUTABILITY — **PASS**

- Code: `DuplicatedAsync` reads the source with `AsNoTracking()` (never tracked ⇒ never modified),
  only validates the source version, and inserts ONLY new rows (new `job_ons` + new context rows).
  The source row, its `version`, `updated_at` and its context rows are never written.
- Proof rows: **SNAP2** (source `version` unchanged and source frozen triples byte-identical after
  duplication), **DUP5** (source row and its context rows byte-identical before/after, real schema),
  **DUP17** (exactly one source row with `copied_from_jobon_id IS NULL`; total rows = source + 2
  duplicates only), **DUP14** (`tools`/`tool_machines` untouched by duplication).

## 7. Invariant 6 — MULTIPLE DUPLICATIONS — **PASS**

Same source duplicated twice:

- **SNAP3** (endpoint): occurrences B and C have distinct `jobon_id`s; their CM context ids are
  distinct from each other and from the source's; both keep the source's canonical `tool_id`.
- **DUP17** (real schema): per table (`cm_contexts`/`mf_contexts`/`bq_contexts`) the three context
  ids (source, B, C) are all pairwise distinct; every duplicate context references the same canonical
  `tool_id` set as the source; the source row stays unique as the non-`copied_from` row.

## 8. Invariant 7 — BQ / P2-T07 REGRESSION — **PASS**

- **SNAP4** (endpoint): source production `bq_id_A` ≠ duplicated production `bq_id_B`, while both
  contexts reference the same canonical BQ `tool_id`.
- **DUP18** (real schema): `bq_contexts` — `bq_id_A != bq_id_B`, same canonical `tool_id`, exactly
  two `bq_contexts` rows for the tool, exactly one BQ context per production (`jobon_id`-scoped).
- P2-T07 behavior: zero Boquilhas source files changed between `a96814f` and `HEAD` (git diff
  empty for `Boquilhas`/`boquilha_*` paths); Contract §23.1.5 and §23.3 record that Boquilhas
  movements stay keyed by the `bq_id` of their own production and are NEVER migrated to a new
  `bq_id`. Existing movements therefore remain associated with their original `bq_id_A`.
- Regression: **P2-T07 Boquilhas block 67/67 PASS** on the disposable PostgreSQL (includes
  `P2T07RegressionTests` and the movement-ledger rows).

## 9. Invariant 8 — NEGATIVE SCOPE — **PASS**

Verified absent (git/`src` diff `a96814f..HEAD`, contract §23.3, live schema, regression rows):

- **No `production_id`** — BND5 (`P2T04RegressionTests`) green; no token in the clarification diff.
- **No `job_on_revision_id`** — BND5 green; no revision entity/field anywhere.
- **No rollover entity** — none in diff, contract §23.3, or schema.
- **No Tool history table** — transcript tables = 7 migrations / 23 product tables, byte-identical
  to the pre-clarification P2-T07 final state; the clarification adds no table/column.
- **No new migration / schema change** — `git diff a96814f d2b3b3c -- src/DMO.Infrastructure/Persistence/Migrations` empty;
  migrations 001–007 and `DmoDbContext.cs` untouched (BND2 green).
- **No P2-T07 behavior change** — no Boquilhas file changed; Boquilhas regression block green.
- **No availability registration** — `ModuleRegistrations.CurrentBuildAvailable` stays `[]`
  (source read + BND1/RTE11 rows green); no destination-route registration (RTE12 green);
  no policy change (RTE15 green).
- No new route, endpoint, model or repository behavior change (diff surface = 4 `src` files,
  service-level only + comments).
- No snapshot engine / live-copy helper added (**SNA5** green).

## 10. Run record (all executed in this review)

| Suite | Command / env | Result |
|---|---|---|
| Build | `dotnet build DMO.slnx -c Debug` | **0 errors**; 12 warnings — the disclosed pre-existing analyzer set (P2T02/P2T06 protected files + PesoReview rows, xUnit analyzers in test files); 0 warnings in all changed code |
| Unit | `dotnet test tests/DMO.UnitTests -c Debug --no-build` | **658 / 658 PASS** (0 failed, 0 skipped) |
| Focused snapshot/duplication | `dotnet test tests/DMO.IntegrationTests --filter "~JobOnDuplicationIntegrationTests\|~SNAP\|~CTX14"` with `DMO_TEST_POSTGRES_CONNECTION` → fresh disposable PostgreSQL 16.15 (`postgres:16-alpine` container, new database `dmo_review`, migrations applied from scratch: 7 ids) | **18 / 18 PASS** (DUP2–DUP10, DUP12, DUP14, DUP17, DUP18; CTX14; SNAP1–SNAP4) — plus the 2 adjacent rows the filter also matched (MIG10, SNA5) PASS, run total **20/20** |
| P2-T04 regression | `--filter "~P2T04RegressionTests"` | **18 / 18 PASS** |
| P2-T07 Boquilhas regression | `--filter "~Boquilhas"` | **67 / 67 PASS** |
| Schema evidence | `pg_tables` + `__EFMigrationsHistory` on `dmo_review` | 23 product tables / 7 migration ids — identical to pre-clarification final state |

No unrelated acceptance matrix was rerun (no concrete failure required it).

## 11. Reviewer observations (non-blocking)

- The duplication snapshot is composed at the application-service layer through the same
  `ResolveToolAsync` path normal creation uses for its pre-check (normal creation additionally
  re-freezes inside the write transaction; duplication persists exactly the service-composed
  snapshot). This matches §23.1.2/§23.1.3 ("the new context's frozen triple is always a NEW snapshot
  of the CURRENT canonical Tool row at duplication time"; "normal creation and duplication converge
  on the SAME snapshot creation path") — the source context's frozen triple is in no code path a
  source for the new snapshot.
- `SNAP2`/`DUP7`/`CTX14` all probe reference/lot (the allowed canonical Tool facts of the frozen
  set); a concurrent Tool-row change between the service read and the insert is the same
  read-then-write race class every create path has and is not part of the clarified invariant
  (the clarification fixes the snapshot SOURCE, which is proven correct).

---

## 12. Conclusion

Every binding element of the OWNER clarification (contract §23) is implemented as specified and
independently reproduced: normal creation and duplication both create NEW `cm_id`/`mf_id`/`bq_id`
context snapshots from the CURRENT canonical Tool state; duplication reuses only the canonical
`tool_id` identities, never context identities; source Job On and contexts stay immutable; repeated
duplications never reuse context ids; `bq_id_A != bq_id_B` while both reference the same canonical
BQ `tool_id`, with P2-T07 movements untouched and the Boquilhas regression green; the negative
scope (no `production_id`, no revision, no rollover, no history table, no migration/schema, no
P2-T07 change, no availability registration) holds. No implementation was modified; only this
report is committed.

**FINAL STATUS:**
**OWNER CLARIFICATION CLOSED**