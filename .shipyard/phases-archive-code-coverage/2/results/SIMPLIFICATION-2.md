# Simplification Report
**Phase:** 2 - Code coverage improvements (RelationalDatabase handler tests)
**Date:** 2026-04-12
**Files analyzed:** 7 (3 new, 4 modified)
**Findings:** 1 High, 2 Medium, 3 Low

---

## High Priority

### Duplicated NSubstitute scaffolding across three new query/command handler test files
- **Type:** Consolidate
- **Effort:** Moderate
- **Locations:**
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/CommandHandler/CreateJobTablesCommandHandlerTests.cs:116-165` (`CreateFixture` + `TestFixture` class)
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetJobIdQueryHandlerTests.cs:99-145` (`CreateFixture` + `TestFixture` class)
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetJobLastKnownEventQueryHandlerTests.cs:100-146` (`CreateFixture` + `TestFixture` class)
- **Description:** All three new test files repeat the same ADO.NET mocking choreography:
  `Substitute.For<IDbConnectionFactory>()` -> returns `IDbConnection` -> returns `IDbCommand` -> returns `IDataReader`, plus a private `TestFixture` DTO exposing each mock as a property. Only the command/query type and the `IPrepareQueryHandler`/`IPrepareCommandHandler` generic arguments differ. This is the Rule of Three trigger (3 occurrences), and the two query-handler fixtures additionally share the `IReadColumn` mock setup verbatim.
- **Suggestion:** Extract a shared helper in the test project, e.g.
  `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/TestHelpers/AdoNetMockFixture.cs`, exposing a generic builder:
  ```csharp
  internal sealed class AdoNetMockFixture
  {
      public IDbConnectionFactory ConnectionFactory { get; }
      public IDbConnection Connection { get; }
      public IDbCommand Command { get; }
      public IDataReader Reader { get; }
      public IReadColumn ReadColumn { get; }
      public ITransactionFactory TransactionFactory { get; }
      public ITransactionWrapper TransactionWrapper { get; }
      public IDbTransaction Transaction { get; }
      public static AdoNetMockFixture Create(bool withTransaction = false) { ... }
  }
  ```
  Each test file then declares only its handler-specific fields (`IPrepareQueryHandler<TQuery,TResult>`, query instance). Expected savings: ~120 lines across the 3 files; new helper ~60 lines net.
- **Impact:** ~60 lines removed, single point of change when the ADO.NET mocking contract evolves, and consistency with existing test-helper patterns in the project.

---

## Medium Priority

### Divergent fixture style: `CreateFixture()` vs `CreateHandler(int rowCount)` tuple pattern
- **Type:** Refactor
- **Effort:** Moderate
- **Locations:**
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardJobsQueryHandlerTests.cs:121` (`private static (... , ... , ...) CreateHandler(int rowCount)`)
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetDashboardErrorRetriesQueryHandlerTests.cs:129` (same shape)
  - Async variants at `GetDashboardJobsQueryHandlerAsyncTests.cs` and `GetDashboardErrorRetriesQueryHandlerAsyncTests.cs`
  - Contrast with `CreateFixture()` used in the three new files above
- **Description:** The Phase 2 work introduced two different fixture idioms in the same test project: the dashboard tests use a 3-tuple `(handler, readColumn, reader)` returned from `CreateHandler(int rowCount)`, while the new job-table/job-id tests use a named `TestFixture` class returned from `CreateFixture()`. Both compose the same underlying mocks. The tuple form loses names (`var (handler, _, _) = CreateHandler(0)` appears multiple times) and forces callers to discard unused slots.
- **Suggestion:** After the High-priority extraction lands, reconcile both styles onto the shared `AdoNetMockFixture`. The dashboard tests can call `fixture.SetupRows(count)` instead of passing `rowCount` through a constructor. This keeps one idiom in the project.
- **Impact:** Consistent test style; removes `var (handler, _, _) = ...` discards; eliminates two parallel helper shapes.

### Near-duplicate body between sync and async dashboard test pairs
- **Type:** Consolidate
- **Effort:** Moderate
- **Locations:**
  - `GetDashboardJobsQueryHandlerTests.cs` and `GetDashboardJobsQueryHandlerAsyncTests.cs`
  - `GetDashboardErrorRetriesQueryHandlerTests.cs` and `GetDashboardErrorRetriesQueryHandlerAsyncTests.cs`
- **Description:** The sync and async variants share near-identical test method bodies and `CreateHandler` helpers, differing only in `.Handle(...)` vs `await .HandleAsync(...)` and one type parameter. Each async file re-declares the same tuple helper.
- **Suggestion:** Either (a) have the async test file call a shared builder from a `DashboardTestHelpers` static class, or (b) have the async handler's `CreateHandler` delegate to the sync version's helper to eliminate the duplicated mocking block. Do not merge the test classes themselves - keeping sync/async fixtures separate is correct - but the mock wiring should not be duplicated.
- **Impact:** ~40 lines removed across the two async files.

---

## Low Priority

- **Redundant constructor null-argument tests.** `CreateJobTablesCommandHandlerTests.cs:84-114`, `GetJobIdQueryHandlerTests.cs:72-98`, and `GetJobLastKnownEventQueryHandlerTests.cs:73-99` each have 3 near-identical `Constructor_Null*_Throws` methods that differ only in which argument is nulled. Consider a `[DataTestMethod]`/`[DynamicData]` parameterized test, or accept these as-is since explicit per-arg tests read well. Effort: Trivial. Recommendation: defer unless the pattern spreads further.

- **Test naming mix of tenses/voices.** `Handle_OpensConnection_AndReturnsSuccess` (compound assertion in name) vs `Handle_ExecutesNonQuery_OnCommand` vs `Handle_CommitsTransaction`. The first method asserts four things (`IsNotNull`, status, factory call, connection.Open). Split into `Handle_ReturnsSuccessStatus` and `Handle_OpensConnection`, or rename to `Handle_OpensConnectionAndReturnsSuccess`. Effort: Trivial. `CreateJobTablesCommandHandlerTests.cs:35-45`.

- **`Handle_CommitsTransaction` asserts four distinct behaviors.** `CreateJobTablesCommandHandlerTests.cs:71-81` verifies `TransactionFactory.Create`, `BeginTransaction`, `Commit`, and the `DbCommand.Transaction` setter in one test. The assertion-per-test rule would split these. Effort: Trivial. Low impact - acceptable for related transaction-lifecycle verification.

---

## Summary

- **Duplication found:** 1 significant cluster (fixture scaffolding) across 3 new files, plus sync/async dashboard duplication across 4 files.
- **Dead code found:** None.
- **Complexity hotspots:** None - all methods well under thresholds. The longest method is `CreateFixture()` at ~35 lines, which is acceptable for a test builder.
- **AI bloat patterns:** Minimal. No over-commented blocks, no redundant try/catch, no defensive null checks. The LGPL headers are project-standard and not bloat. Constructor null-guard test triplication is the closest to AI bloat but is idiomatic.
- **Estimated cleanup impact:** ~100-160 lines removable if High + Medium priority items are actioned; new helper adds ~60 lines for a net saving of ~40-100 lines and a single source of truth for ADO.NET test mocking.

## Recommendation

**Defer is acceptable.** The Phase 2 tests are clean, focused, and follow good unit-test hygiene. The duplication is real but confined to fixture plumbing - the assertions themselves are distinct and valuable. The High-priority consolidation is worth doing before the next wave of handler tests lands (to prevent the pattern from spreading to 5-10 copies), but it does not block shipping Phase 2. If the next phase adds more `CommandHandler`/`QueryHandler` tests against the same ADO.NET surface, action the High finding first; otherwise park it.

The sync-vs-async-and-named-vs-tuple fixture style divergence is the most important signal here: two builders working in parallel independently chose different idioms. Pick one before the codebase locks in both.
