# P2-T05 — CONTRACT CORRECTION RESPONSE (C1–C4 + Q-PDF restatement)

**Workstream:** P2-T05 — Controlo_Create.
**Task class:** contract correction only (Architect plan review `PLAN REJECT — C1–C4 only`).
**No implementation. No application code. No migration. No Supabase.**
**Status:** **P2-T05 CONTRACT CORRECTED — AWAITING ARCHITECT RE-REVIEW.**

---

## 1. Baseline

| Item | Value |
|---|---|
| Reviewed contract (pre-correction) | `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` @ `c1adae808af11e1a9d68c8ed98e074259339f065` (blob `2f80638012d888499d42375262c1dd3fa3bdae65`) |
| Architect plan review | dmo-work `256081fae43d4192b879b65fca0bb43efe8cdbca` — `dev/reviews/P2-T05_CONTROLO_CREATE_CONTRACT_PLAN_REVIEW.md`: **PLAN REJECT — C1–C4 only**; **Q-PDF resolved**: ACCEPT DEFAULT — server-host filesystem configuration with server-side accessibility check; **27 authority questions ACCEPT DEFAULT / 0 REQUIRES CORRECTION / 0 BLOCKING** |
| Master authority evidence | dmo-work `01c3470f6a782d539d961d46c1ce2c6b1a32735d` — `dev/evidence/P2T05_DMO_MASTER_AUTHORITY_EVIDENCE.md` (provenance + verbatim extracts) |
| DMO-MODULAR remote `main` at correction | `c1adae808af11e1a9d68c8ed98e074259339f065` (clean) |
| Corrections applied | C1, C2, C3, C4 — exactly the review's minimum correction set; plus the mandatory Q-PDF restatement in the main normative sections |
| Working tree | CLEAN before and after |

## 2. What changed — per correction

### C1 — Test-to-acceptance matrix repaired (bidirectional and closed)

- **AC-N1 (fixed desktop) now has planned executable proof** — new `FIXED DESKTOP` group in
  §26.4: LAY1 (S-class static scan: no `@media`/`@container`/`@supports` structural rules, no
  width listener, no breakpoint variant, no table→card conversion, no required-column hiding,
  no action relocation), LAY2 (R-class render at the canonical 1366 × 768 validation viewport,
  region-stable composition), LAY3 (R-class keyboard-reachable local overflow, no hidden required
  columns, no action relocation).
- **Dangling keys resolved** — the undocumented alias keys `AC-A1`, `AC-A2`, `AC-A6`, `AC-A7`
  and `AC-N2` are removed from every row; each row now maps only to keys that exist in §30:
  PID1→AC-P1; PID5→AC-P1+AC-P3; PID6→AC-P5+AC-C8; SNA5→AC-H3; MAC6→AC-E4; BND2→AC-Y2;
  BND5→AC-P1+AC-P3; BND8→AC-Y6.
- **New criteria + proofs brought by C2/C3** (so their tests map back to the catalogue):
  AC-M10 (non-positive computed result → typed refusal) with row MES11; AC-R7 (calculate
  request-carrier identity) with row JRC7.
- **Added proofs:** SET12 (executable server-side check semantics of the Q-PDF ruling → AC-F2).
- **Mechanical audit (scripted, not guessed):** acceptance criteria **66** (P7 + M10 + H3 + C9 +
  R7 + D4 + E5 + F9 + G4 + Y7 + N1); test rows **84** (PID 10, MES 11, SNA 5, CRE 10, JRC 7,
  REP 5, MAC 7, SET 12, AUT 5, BND 9, LAY 3); **duplicate row ids 0; missing criteria 0;
  dangling references 0; orphan rows 0**; every one of the 66 criteria is referenced by ≥ 1 row
  and every row references only existing criteria. The old 64-count statement was replaced — it
  is not preserved.

### C2 — Typed result for the reachable CHECK violation (`RESULT_NON_POSITIVE`)

- **Triggering condition (§5.3):** any computed per-row result that is not strictly positive —
  `capacity_cm3 <= 0` (invalid divisor configuration) or `glass_weight_g <= 0` (e.g. entered
  `Volume Punção/PU` > `Capacidade do CM + Volume Marisa/BQ`).
- **Domain/application result (§26.1/§26.2):** `ValidationFailed(["RESULT_NON_POSITIVE"])`
  raised by the pure validator (pre-write) on create/update/submit/calculate; the same
  `ResultNonPositive` domain refusal is the repository backstop.
- **Transport/HTTP (§26.2):** 400 `validation-failed` with `errors[] = ["RESULT_NON_POSITIVE"]`;
  never a 500, never an invented generic error.
- **CHECK relationship (§20.2/§17.4):** the physical CHECKs
  `peso_measurement_rows_capacity_check` (`capacity_cm3 > 0`) and
  `peso_measurement_rows_glass_check` (`glass_weight_g > 0`) are **not weakened or removed**;
  SQLSTATE `23514` on those constraint names maps to the same domain refusal.
- **Transaction integration (§7.1 step 4, §7.3 step 6, §7.4 step 5):** refusal → ROLLBACK,
  nothing written.
- **Planned test proof (§26.4 MES11 → AC-M10):** validator refusal + nothing written + CHECKs
  still present and rejecting direct inserts + mapped backstop (no 500).

### C3 — Route 8 calculate identity pin (`POST /controlo/create/calculate`)

- **Exact route:** `POST /controlo/create/calculate` — the `{pesoId}` path parameter is removed
  (a calculation may happen before the first save, so no persisted id exists; no client-supplied
  id may stand in for one).
- **Request carrier (`CalculatePesoRequest`):** exactly one anchor from the accepted identity
  chain — `cmId` (existing `cm_contexts.cm_id`) xor `pendingToolId` (existing `tools.tool_id`) —
  plus the current input facts (temperature, volumes, SAP references, row water weights). No
  `peso_id`, no `jobon_id`, no client-minted identity.
- **What the anchor means:** calculation context (density via `cm_id → tool_id → processo`, or
  `tool_id → processo` pending; divisor via temperature) — never a stored draft/reference.
- **Validation (§5.5):** exactly one anchor (`PESO_ANCHOR_REQUIRED`/`PESO_ANCHOR_CONFLICT`),
  anchor must exist (`CM_CONTEXT_NOT_FOUND`/`TOOL_NOT_FOUND` — 400, not 404), temperature/rows/
  weights as contracted, `RESULT_NON_POSITIVE` for non-positive computed results.
- **Not-found/refusal behavior:** 404 is **never** returned (the request never targets a
  persisted record); 400 `validation-failed` + token list; 409 `calculation-configuration-missing`; 403 denied.
- **Authorization:** `controlo-create` (single canonical policy, §21.2/§22).
- **Response carrier:** 200 `PesoCalculationResponse` (per-row results at full `numeric(18,4)`
  + ≤ 2 dp presentation, resolved divisor/density display facts, echoed anchor); **no write, no
  version bump, no row creation, no id allocation**; saves recompute so stored == authoritative.
- **Planned test proof (§26.4 JRC7 → AC-R7):** statelessness, id-free carrier, no resolution/no
  404, anchor validation, gate, recompute parity. No other route was altered (route count stays
  17; only row 8's path/carrier changed).

### C4 — Appendix D.3 provenance corrected

- The unretrievable `dmo-master main: 610c8b4d3084864a750f6aa00507b5273ef09567b` record is
  **removed and replaced with the verified current heads** — dmo-master `main`
  `8f1ca3e27e0eaf58c3dce285b544066565fa3dc1`, modular line (`dmo-modular`) `ae2a9b9d12132ee4b41dc0696f34c6439b5cca52` — with the explicit statement that **no replacement SHA is invented**.
- The provenance note distinguishes **verified quotation provenance** (old-era passages
  independently verified verbatim-accurate against recovered lineage commits `60c20d3`/
  `b2b813b`) from the **unavailable original commit identity**, and references the preserved
  evidence record `dev/evidence/P2T05_DMO_MASTER_AUTHORITY_EVIDENCE.md` (dmo-work `01c3470f…`).
- It records that **no live dmo-master contradiction** exists with this contract's model.
- Contract §1.2's master-authority note was aligned with the same corrected state.

### Q-PDF restatement (mandatory, main normative sections)

- **§12.2:** new binding rows — execution host (server-host filesystem path; site-wide per
  Q-SITE; browser never accesses workstation paths; no File System Access API; no client↔server
  same-filesystem assumption), check semantics (server-side probe: `invalid-path`,
  `directory-not-found`, `not-a-directory`, `access-denied`, `check-failed`, `ok`,
  `not-configured`; never claims workstation/browser reachability; creates no documents).
- **§12.3:** retitled "The deployment question … **RESOLVED by the Architect plan review**" with
  the quoted ruling and its consequences.
- **§27.1:** "BLOCKING — NONE" (Q-PDF removed from the blocking table); **§27.2:** Q-PDF row as
  ACCEPT DEFAULT (Architect ruling) with the full meaning; **§28:** BLOCKING 0 / NON-BLOCKING 27.
- **§1.2/S15/App E** no longer present Q-PDF as blocking.
- **P2-T08 boundary unchanged:** generation/storage/send remains P2-T08's contract; P2-T05 owns
  the setting and its server-side check only.

## 3. Authority-question status (unchanged by correction, verified)

**27 ACCEPT DEFAULT — 0 REQUIRES CORRECTION — 0 BLOCKING** (Q-PDF resolved by the Architect
ruling; the 26 original defaults unchanged). Stale-reference sweep performed across all modified
files: no `Q-PDF BLOCKING`, no `1 BLOCKING`, no "AWAITING PDF DECISION", no "unresolved
filesystem semantics" reference remains.

## 4. Files changed (contract/planning/governance only)

| Path | Change |
|---|---|
| `plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md` | the corrected contract (C1–C4 + Q-PDF restatement) |
| `plans/BETA_IMPLEMENTATION_MASTER_PLAN.md` | §4 B2 row + blocker-status table; §7 P2-T05 CONTRACT STATUS block; Appendix A B2 row + counts tail (status records only) |
| `plans/beta-workstreams/P2-T05-CONTROLO-CREATE.md` | §5.1 correction-status record |
| `dev/responses/P2_T05_CONTRACT_CORRECTION_RESPONSE.md` | this response (new) |

Verified absent from the diff: `src/**`, `tests/**`, `**/Migrations/**`, `wwwroot/**`,
`*.csproj`, `Program.cs`, `.env`/Supabase configuration, `docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md`, `reports/**`.

## 5. Verification statements

```text
Application code modified    : NO
Tests modified               : NO
Migration created/modified   : NO
Supabase modified            : NO
CurrentBuildAvailable        : [] (unchanged)
DestinationRouteRegistrations: unchanged (still empty)
P2-T04                       : CLOSED (unchanged)
P2-T05 implementation        : NOT AUTHORIZED  (awaiting Architect re-review ACCEPT)
P2-T06 / P2-T07 / P2-T08     : NOT AUTHORIZED
working tree (application)   : CLEAN before and after
```

## 6. Next gate

```text
ARCHITECT PLAN RE-REVIEW REQUIRED BEFORE P2-T05 IMPLEMENTATION
Corrected contract commit = <this correction commit, pushed to DMO-MODULAR/main>
```

The Architect must review the corrected contract at its pushed SHA and return `PLAN ACCEPT`
(or further `CORRECTION REQUIRED`/`REJECT`). Until then: P2-T05 remains
**P2-T05 CONTRACT CORRECTED — AWAITING ARCHITECT RE-REVIEW**, **B2 AWAITING PLAN ACCEPT**,
**NOT IMPLEMENTED / NOT AUTHORIZED**; P2-T06/P2-T07/P2-T08 remain **NOT AUTHORIZED**.