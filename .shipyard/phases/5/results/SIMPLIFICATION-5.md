# Simplification Review: Phase 5

**Phase:** 5 — SQLite Inbox + SQLite-Outbox Sweep
**Date:** 2026-05-18
**Scope:** 12 file changes, +~1100 lines net.
**Findings:** 0 High, 0 Medium, 3 Low

## Verdict: CLEAN

## Findings

### Low / observations
- **L1 — Dead `IConnectionHeader<IDbConnection, IDbTransaction, IDbCommand>` registration** at `SqLiteMessageQueueSharedInit.cs:117-118`. Was already unused before Phase 5; PLAN-1.1 didn't remove it. Defer cleanup.
- **L2 — Sync and async send-handler `HandleExternalTransaction` forks duplicate ~80 lines** of insertion logic each. Extractable into a shared protected helper, but the structural differences (sync `command.ExecuteNonQuery()` vs `await _readerAsync.ExecuteNonQueryAsync(command)`) make a generic helper awkward. SqlServer/PG have the same shape with the same duplication. Defer.
- **L3 — `Path.GetFullPath()` + `:memory:` short-circuit logic duplicated** between `SqLiteExternalDbNameExtractor.Extract` and `SqliteNormalizedConnectionInformation.Container`. Both apply identical canonicalization; could extract to a shared static helper. 2 occurrences (Rule of Three not met). Defer.

## Pattern check
- **Duplication:** 2-occurrence fork-body duplication (sync vs async, structural difference); 2-occurrence canonicalization. Neither rises to action-required.
- **Abstractions:** New types each have a single purpose. No over-engineering.
- **Dead code:** L1 noted (pre-existing dead code, not introduced by Phase 5).
- **Complexity:** Largest file is `SqLiteRelationalProducerQueue.cs` (~180 lines) mirroring SqlServer counterpart shape. Within thresholds.
- **AI-bloat:** None confirmed. Comments accurately explain Phase 5-specific decisions (Approach B rationale, order-of-ops in commit/rollback, symmetric normalization purpose).

## Recommendation
Accept as-is. The 3 low-priority findings are not blocking and most match existing SqlServer/PG patterns.
