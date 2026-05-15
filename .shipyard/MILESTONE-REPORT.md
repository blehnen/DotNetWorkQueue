# Milestone Report: Outbox Pattern Support for Relational Transports

**Completed:** 2026-05-15
**Branch:** `feature/outbox-pattern`
**Commit range:** `194ba0a3` (project capture) → `24a1e3d7` (CodeRabbit fixes)
**Total commits:** 77
**Total diff:** 172 files changed, +23,982 / -5 lines
**PR:** [#138](https://github.com/blehnen/DotNetWorkQueue/pull/138)
**CI:** Jenkins build #6 SUCCESS on full 14-stage matrix (GH Actions, CodeRabbit, codecov/patch, codecov/project all green)

---

## Executive Summary

Delivered transactional outbox pattern support for the SqlServer and PostgreSQL transports across 7 phases of work, opening a parallel opt-in send surface on relational transports that accepts a caller-supplied `DbTransaction`. The existing producer surface is preserved; non-relational transports (Memory, Redis, LiteDb, SQLite) are unaffected. Producer never commits, rolls back, or disposes caller-owned resources. Retry decorator bypasses on the caller-tx path. Cross-DB validation throws before any write. 24 integration tests validated on Jenkins across both transports.

---

## Phase Summaries

### Phase 1 — Spike
**Verdict:** PASS

Interface contract experiments validated the `IRelationalProducerQueue<T>` capability-cast pattern. Established the lifecycle contract (caller owns connection + transaction + retry).

### Phase 2 — Foundation
**Verdict:** PASS

Shared layer in `Transport.RelationalDatabase`:
- `IRelationalProducerQueue<T>` derived interface
- `RelationalProducerQueue<T>` abstract base class
- `RelationalSendMessageCommand` with `IRetrySkippable` marker (Polly bypass on caller-tx path)
- `ExternalTransactionValidator` (4 checks: null transaction, null connection, closed connection, cross-DB)
- `IExternalDbNameExtractor` pluggable interface

### Phase 3 — SqlServer
**Verdict:** PASS

- `SqlServerRelationalProducerQueue<T>` with capability-cast registration
- `SqlServerExternalDbNameExtractor` (mid-Phase-6 amended to verbatim pass-through, commit `994e1404`)
- `HandleExternalTransaction` + `HandleExternalTransactionAsync` forks in `SendMessage.cs` / `SendMessageAsync.cs`
- DI wiring (`SqlServerMessageQueueInit`)
- Unit tests with mocked `DbConnection`/`DbCommand`/`DbDataReader` (CLAUDE.md async-handler lesson respected)

### Phase 4 — PostgreSQL
**Verdict:** PASS

- `PostgreSqlRelationalProducerQueue<T>` mirroring SqlServer wiring
- `PostgreSqlExternalDbNameExtractor` (native pass-through; no Phase 6 fix required)
- `HandleExternalTransaction` + `HandleExternalTransactionAsync` forks
- DI wiring (`PostgreSqlMessageQueueInit`)

### Phase 5 — Negative-Path Coverage
**Verdict:** PASS

Producer-cast negative-path unit tests across 4 non-relational transports: Memory, Redis, LiteDb, SQLite. Confirms the `is IRelationalProducerQueue<T>` cast returns false and the interface is genuinely absent. Test-only; no production-code surface.

### Phase 6 — Integration Tests (SqlServer + PostgreSQL)
**Verdict:** PASS

24 integration tests (12 per transport, method-coverage matrix):
- 8 method-matrix tests/transport (Send/SendAsync × single/batch × commit/rollback)
- 2 validation tests/transport (cross-DB + closed-connection)
- 1 retry-bypass test/transport (committed-tx technique, <2000ms wall-clock)
- 1 `IAdditionalMessageData` round-trip test/transport (correlation-ID round-trip)

Mid-build: discovered + fixed Phase 3 `SqlServerExternalDbNameExtractor` symmetry bug (commit `994e1404` — `.ToUpperInvariant()` → verbatim pass-through, matching PG's design). ISSUE-036 (`Tx → Transaction` rename) caught during PR review; resolved across production + test code in 2 commits.

### Phase 7 — Documentation
**Verdict:** PASS

- `docs/outbox-pattern.md` (205 lines tutorial + reference per CONTEXT-7 hybrid design)
- README pointer under "High-level features"
- Closed ISSUE-032 inline: `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` on `Transport.SQLite.csproj`
- net8.0 XML-doc gate closed on `Transport.RelationalDatabase.csproj`
- Full-solution `dotnet build -c Release -p:CI=true` returns 0 errors, 0 CS1591
- Source Link metadata verified in nuspec

---

## PROJECT.md Success Criteria — Coverage Rollup

| §SC | Criterion | Phase(s) | Status |
|---|---|---|---|
| #1 | `IRelationalProducerQueue<T>` exists in Transport.RelationalDatabase, implemented by SqlServer + PG producers | 2, 3, 4 | **MET** |
| #2 | Memory, Redis, LiteDb, SQLite producers do NOT implement the interface | 5 | **MET** |
| #3 | Capability-cast pattern works at runtime | 3, 4, 6 | **MET** |
| #4 | Atomic commit verified (queue row + business row visible) | 6 | **MET** (4 commit-path tests/transport) |
| #5 | Atomic rollback verified (neither row present) | 6 | **MET** (4 rollback-path tests/transport) |
| #6 | Cross-DB validation throws `InvalidOperationException` before write | 6 | **MET** |
| #7 | Caller-owned resources not disposed (Commit/Rollback/Dispose/Close never invoked) | 2, 3, 4 | **MET** (unit tests + structural smoke tests pin lifecycle invariants) |
| #8 | Polly retry decorator bypass under transient failure | 3, 6 | **MET** (structural unit pin in Phase 3; integration-level pin in Phase 6, <2000ms wall-clock) |
| #9 | All existing tests still pass; no regressions | All | **MET** (Jenkins green on full matrix) |
| #10 | `docs/outbox-pattern.md` covers lifecycle, retry, capability-cast, schema-deploy, DB-name semantics, supported-transports | 7 | **MET** |
| #11 | Jenkins green on draft PR before merge | 6, 7 | **MET** (PR-138 build #6 = `24a1e3d7` SUCCESS on full 14-stage matrix) |

**All 11 success criteria satisfied.**

---

## Key Decisions (aggregated across phases)

- **Pass-through DB-name comparison.** SqlServer and PostgreSQL extractors both return `connection.Database` verbatim (no `ToUpperInvariant`). The validator uses `StringComparer.Ordinal` symmetrically. Surfaced as Phase 3 amendment during Phase 6 integration testing (commit `994e1404`); aligns both transports under a single design.
- **CI-gating between waves.** Phase 6 used Wave 1 (SqlServer) → draft PR → Jenkins SqlServer green → Wave 2 (PostgreSQL) flow per CONTEXT-6 Decision 4. Caught the extractor symmetry bug before PG work started; pattern reusable for future split-CI phases.
- **`Tx → Transaction` rename across the feature.** Internal abbreviations drifted to `Tx` during Phases 2-5; user review during Phase 6 surfaced the inconsistency (ISSUE-036). Resolved across production + test code in 2 commits before merge.
- **Task 3 simplification on `*OutboxAdditionalDataTests` (both transports).** Plans called for `data.SetPriority(7)` + priority column assertion; builder simplified to correlation-only round-trip via auto-assigned `data.CorrelationId`. Symmetric on both transports; tracked as ISSUE-037 for future strengthening.
- **Wiki page deferred.** Phase 7 CONTEXT-7 Decision 1 ships `docs/outbox-pattern.md` only; the GitHub Wiki update is a manual post-ship task. Matches the existing pattern (README points to Wiki for in-depth docs).

---

## Documentation Status

| Asset | Status |
|---|---|
| API XML doc comments on `IRelationalProducerQueue<T>` + concrete producers | **Complete** — Phase 2-4 builders added docs as they went; Phase 7 PLAN-1.1 confirmed zero CS1591 warnings |
| `docs/outbox-pattern.md` (tutorial + 5 reference sections) | **Complete** — 205 lines, single SqlServer worked example, PG variation in prose |
| README integration | **Complete** — single bullet under "High-level features" pointing to `docs/outbox-pattern.md` |
| Wiki page | **Deferred** — manual post-ship task per CONTEXT-7 Decision 1 |
| Per-phase Shipyard artifacts | **Complete** — VERIFICATION.md in all 7 phases; AUDIT/SIMPLIFICATION/DOCS in Phases 6+7 |

---

## Known Issues (open at ship time)

| ID | Severity | Description | Status |
|---|---|---|---|
| ISSUE-033 | Minor | Fork-body end-bound overreach in PG sync smoke test | Open (phase 4 carry-forward) |
| ISSUE-034 | Minor | Fragile relative source-file path in fork smoke tests | Open (phase 4 carry-forward) |
| ISSUE-035 | Minor | Path-resolution block duplicated across smoke tests | Open (phase 4 carry-forward) |
| ISSUE-037 | Minor | `*OutboxAdditionalDataTests` priority round-trip not asserted (both transports symmetric) | Open (phase 6 simplification) |
| ISSUE-039 | Low | PROJECT.md "OrdinalIgnoreCase vs Ordinal" text outdated post-Phase-3 pass-through fix | Open (phase 7 surfaced) |
| ISSUE-040 | Low | `docs/outbox-pattern.md` has no SendAsync worked example | Open (phase 7 deferred coverage) |
| ISSUE-041 | Low | `IRelationalProducerQueue<T>` XML doc link is plain-text `<c>` not `<see href>` hyperlink | Open (phase 7 — needs stable master URL post-merge) |

All open issues are NON-BLOCKING for ship gate. None affect runtime correctness; all are documentation polish or test coverage strengthening candidates for future iteration.

---

## Resolved Issues Notable to This Branch

| ID | Description | Resolution |
|---|---|---|
| ISSUE-032 | NU1902 OpenTelemetry advisory escalates Release CI build on Transport.SQLite | Resolved 2026-05-15 (Phase 7 PLAN-1.1 / commit `88ff8996`) — `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to all 3 Release blocks |
| ISSUE-036 | `Tx` abbreviation drift across outbox feature | Resolved Phase 6 / commit `9858f04f` (production + tests) + Phase 6 commit `ef848165` (PG Wave 2 follow-up) |
| ISSUE-038 | PG Wave 2 plans authored pre-rename used `tx`; builder followed plan literally | Resolved Phase 6 / commit `ef848165` — rename across 3 PG outbox test files |

---

## Metrics

- **Phases:** 7/7 complete
- **Plans:** ~25 across all phases (per `.shipyard/phases/{N}/plans/`)
- **Tasks:** ~60 across all plans
- **Commits:** 77 on `feature/outbox-pattern`
- **Files created:** ~50 (new source files, test files, docs, csproj edits, Shipyard artifacts)
- **Files modified:** ~120 (including Shipyard tracking files updated across phases)
- **Diff size:** +23,982 / -5 lines (gross; Shipyard tracking artifacts account for the majority — production diff is far smaller)
- **Integration tests added:** 24 (12 SqlServer + 12 PostgreSQL)
- **Unit tests added:** ~50+ across Transport.RelationalDatabase.Tests, SqlServer.Tests, PostgreSQL.Tests, plus negative-path tests across 4 non-relational transports
- **Production csprojs changed:** 2 (Transport.RelationalDatabase + Transport.SQLite; warning-policy tuning only)
- **New public API surface:** `IRelationalProducerQueue<T>` derived interface + `RelationalProducerQueue<T>` abstract base + 2 concrete implementations (`SqlServerRelationalProducerQueue<T>` + `PostgreSqlRelationalProducerQueue<T>`)
- **External-tx handler forks:** 4 (`HandleExternalTransaction` + `HandleExternalTransactionAsync` in both SqlServer and PostgreSQL)
- **CI matrix:** 14 stages green on Jenkins PR-138 build #6

---

## Ship-time gate evidence

| Gate | Source | Result |
|---|---|---|
| Phase verdicts | `.shipyard/phases/{1..7}/VERIFICATION.md` | All PASS |
| Per-phase reviews | `.shipyard/phases/{2..7}/results/REVIEW-*.md` | All PASS or PASS-after-fix |
| Phase 6 audit | `.shipyard/phases/6/AUDIT.md` | CLEAN |
| Phase 7 audit | `.shipyard/phases/7/AUDIT.md` | CLEAN (2 advisories addressed inline) |
| Ship-time cross-phase audit | `.shipyard/AUDIT-SHIP.md` | (in progress at report-write time) |
| Jenkins matrix | PR-138 build #6 on `24a1e3d7` | SUCCESS (all 14 stages + GHA + CodeRabbit + codecov) |
| Release build clean | Phase 7 PLAN-2.1 evidence | 0 errors, 0 CS1591 on full-solution Release `-c Release -p:CI=true` |
| Source Link metadata | Phase 7 PLAN-2.1 nuspec spot-check | `<repository ... commit="9156ad25..." />` present |
