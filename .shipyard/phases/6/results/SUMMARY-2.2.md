# Build Summary: Plan 2.2 (Phase 6 Wave 2 — PostgreSQL Validation + Retry-Bypass + AdditionalMessageData)

## Status: complete

## Tasks Completed

- Task 1: `PostgreSqlOutboxValidationTests.cs` (2 tests) — cross-DB mismatch (`postgres` system DB) + closed-connection validations both throw `InvalidOperationException` before queue insert.
- Task 2: `PostgreSqlOutboxRetryBypassTests.cs` (1 test) — committed-tx forces first-attempt failure; elapsed <2000ms confirms retry bypassed.
- Task 3: `PostgreSqlOutboxAdditionalDataTests.cs` (1 test) — `IAdditionalMessageData` correlation ID round-trip via direct metadata-table SQL.

## Commits

| SHA | Task | Subject |
|---|---|---|
| `32730700` | 1 | `test(postgresql): add PostgreSqlOutboxValidationTests (cross-DB + closed-conn)` |
| `9c59f9d4` | 2 | `test(postgresql): add PostgreSqlOutboxRetryBypassTests (single-attempt)` |
| `32d7902e` | 3 | `test(postgresql): add PostgreSqlOutboxAdditionalDataTests (correlation round-trip)` |

## Files Created

- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxValidationTests.cs` — NEW (2 [TestMethod])
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxRetryBypassTests.cs` — NEW (1 [TestMethod])
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxAdditionalDataTests.cs` — NEW (1 [TestMethod])

## Decisions Made

- **Task 3 simplified — mirrors Wave 1 (PLAN-1.2 SUMMARY/REVIEW) exactly.** No `EnablePriority`, no inline `CorrelationIdContainer`/`MessageCorrelationId` shells. Auto-assigned correlation ID compared against persisted `CorrelationID` column in MetaData table via direct SQL. Symmetric with `SqlServerOutboxAdditionalDataTests`. Trade-off acknowledged per REVIEW-1.2 Minor #1: modest reduction in regression-catching specificity vs cross-transport symmetry — symmetry won.
- **Cross-DB candidate: `postgres`** (always exists per RESEARCH §10). `NpgsqlConnectionStringBuilder.Database` (not `.InitialCatalog`). Input trimmed per RESEARCH §7 newline guard. Exception-message assertion uses `StringComparison.Ordinal` (PG case-sensitive comparator).

## Issues Encountered

- Pre-existing NU1902 OpenTelemetry advisory warnings (ISSUE-032) — 20 per Debug build, expected baseline.

## Verification Results

| Gate | Expected | Actual |
|---|---|---|
| 3 new test files exist | present | OK |
| `dotnet build` (Debug) | 0 errors | 0 errors, 20 pre-existing NU1902 |
| Runtime test execution | live PG | **Deferred to Jenkins** — local `connectionstring.txt` is empty by design; Jenkins injects connection on its PG integration agent |

## Hand-off to Phase 6 ship gate

After PLAN-2.2 lands (this build), the Phase 6 ship gate per PLAN-2.2 §"Phase 6 ship gate":

1. Push to feature branch.
2. Wait for Jenkins `PostgreSQL` stage + 13 other stages green on PR-138.
3. Confirm Codecov: new lines in `PostgreSqlRelationalProducerQueue<T>` + `HandleExternalTx`/`HandleExternalTxAsync` PG handler forks show ≥1 hit per branch.
4. Phase 6 PASS criteria satisfied → ready for Phase 7 (documentation).
