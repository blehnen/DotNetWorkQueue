# Phase 3 Simplification Report

**Phase:** 3 -- Transport-specific job handler tests + relational refactors
**Files analyzed:** 8 new test files, 2 refactored handlers
**Findings:** 1 HIGH, 2 MEDIUM, 2 LOW

(Note: This report was written by the orchestrator after the simplifier agent failed to complete its analysis.)

---

## High Priority

### Cross-transport test fixture duplication
- **Type:** Consolidate
- **Locations:**
  - `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerSendJobToQueueTests.cs`
  - `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlSendJobToQueueTests.cs`
  - `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteSendToJobQueueTests.cs`
  - Plus the SetJobLastKnownEventCommandHandlerTests in each transport project
- **Description:** All 6 of these test files repeat similar NSubstitute scaffolding for `IProducerMethodQueue`, `IRemoveMessage`, `IQueryHandler<>`, `CreateJobMetaData`, `IGetTimeFactory`. Each transport's test class also re-creates the same `IDbConnection` -> `IDbCommand` -> `IDataParameterCollection` mock chain for the SetJobLastKnownEvent tests.

  Phase 2 introduced `AdoNetMockFixture` in `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/TestHelpers/`, but the new transport test projects don't reference it (deliberately avoided cross-project reference).
- **Suggestion:** Either (a) duplicate `AdoNetMockFixture.cs` into each transport test project under `TestHelpers/`, or (b) extract a small NuGet-style internal helper project shared by all transport test projects, or (c) accept the duplication as the cost of avoiding inter-project test references.
- **Impact:** ~80-120 lines could be removed if the helper is shared. Recommended: defer until a future phase actually duplicates the AdoNetMockFixture into each project (pragmatic over premature DRY).

---

## Medium Priority

### Inconsistent protected-method exposure pattern
- **Type:** Reconcile style
- **Locations:**
  - `SqlServerSendJobToQueueTests.cs` -- uses `System.Reflection` to invoke protected methods
  - `PostgreSqlSendJobToQueueTests.cs` -- uses `TestablePostgreSqlSendJobToQueue` subclass
  - `SqliteSendToJobQueueTests.cs` -- uses `TestableSqliteSendToJobQueue` subclass
- **Description:** Two different patterns coexist for the same problem (testing protected `override` methods). The subclass pattern is more readable; reflection is more brittle (no compile-time check on method names).
- **Suggestion:** Convert the SqlServer test to use a `TestableSqlServerSendJobToQueue` subclass for consistency with the other two transports.
- **Impact:** Low LOC change, improves consistency.

### `CreateFixture` heavyweight initialization in PostgreSQL test
- **Type:** Reduce
- **Location:** `PostgreSqlSendJobToQueueTests.cs` (per reviewer's note)
- **Description:** `CreateFixture` builds heavyweight `QueueProducerConfiguration`/`Policies` that the tests don't actually exercise. Reviewer flagged this as "noise."
- **Suggestion:** Trim to the minimum required by the tests.

---

## Low Priority

### Mid-flight PostgreSQL re-refactor leaves no stale code
- **Type:** Verification only
- **Location:** `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs`
- **Description:** The handler was refactored twice -- first to inject `IDbConnectionFactory` (commit `8c5277a2`), then to drop the `(NpgsqlConnection)` cast and use `IDbConnection` directly (commit `9c77537d`). Verified no stale code or unused usings remain.

### Auditor noted unused generic type parameters
- **Type:** Cleanup candidate (deferred)
- **Location:** Both refactored handlers
- **Description:** `ICommandHandler<SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>>` and `ICommandHandler<SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>>` carry generic type args (the connection/transaction types) that the implementations no longer use after the refactor.
- **Description:** Could simplify to `ICommandHandler<SetJobLastKnownEventCommand>` -- but this would require changing the command type and DI registrations across multiple transports. Out of scope for Phase 3.

---

## Summary

- **Duplication found:** 1 cluster (cross-transport test fixture scaffolding). Real but not blocking; defer.
- **Dead code found:** None.
- **Complexity hotspots:** None.
- **AI bloat:** Minimal. Only the heavyweight `CreateFixture` in PostgreSQL is notable.
- **Estimated cleanup:** ~80-120 lines removable if `AdoNetMockFixture` is propagated.

## Recommendation

**Defer all findings.** Phase 3 tests are clean and focused. The High-priority finding (duplicated mock scaffolding) is real, but the right time to address it is when we get to Phase 4 (LiteDb + Redis) -- at that point we'll have 5+ transport test projects with the same pattern and the right shape of the helper will be clearer. Premature DRY now would force a guess at the API.
