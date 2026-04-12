# Review: Plan 1.3

## Verdict: PASS

## Stage 1 -- Spec Compliance

### Task 1: Gap analysis (no file changes)
- Status: PASS
- Evidence: SUMMARY-1.3.md documents handler shape (ctor: `IDbConnectionFactory`, `IPrepareQueryHandler<...>`, `IReadColumn` — all `Guard.NotNull`). Confirmed against source: `GetDashboardJobsQueryHandler.cs` lines 39-41, `GetDashboardJobsQueryHandlerAsync.cs` lines 40-42, `GetDashboardErrorRetriesQueryHandler.cs` lines 39-41, `GetDashboardErrorRetriesQueryHandlerAsync.cs` lines 40-42 — each has exactly 3 `Guard.NotNull` calls (no hidden 4th param). Confirms 3 ctor null-guard tests per file is correct and complete.

### Task 2: Expand GetDashboardJobs tests (sync + async)
- Status: PASS
- Evidence: commit `3af63917`.
  - `GetDashboardJobsQueryHandlerTests.cs`: 5 new tests added (`Handle_Returns_Multiple_Jobs_From_Reader` lines 45-64, `Handle_Invokes_PrepareQuery_With_Correct_CommandString` lines 66-89, three `Constructor_Throws_When_*_Is_Null` lines 91-119). `CreateHandler(int rowCount)` helper lines 121-149 correctly builds an N-row reader sequence via `Returns(true, rest)` with a trailing `false`. Pre-existing happy-path + empty-reader tests untouched (lines 18-43).
  - `GetDashboardJobsQueryHandlerAsyncTests.cs`: 6 new tests added (`HandleAsync_Returns_Multiple_Jobs_From_Reader` lines 64-83, `HandleAsync_Awaited_Result_Matches_Mocked_Reader_Output` lines 85-104 asserting `task.IsCompletedSuccessfully`, `HandleAsync_Invokes_PrepareQuery_With_Correct_CommandString` lines 106-128, three ctor null-guards lines 130-158). Async uses abstract `DbConnection`/`DbCommand`/`DbDataReader` and mocks `ReadAsync(CancellationToken)`/`ExecuteReaderAsync(CancellationToken)` — correct for `GetDashboardJobsQueryHandlerAsync` which internally uses `Arg.Any<CancellationToken>()` patterns. No cancellation-token test added (per CONTEXT decision — `HandleAsync` takes no `CancellationToken` param).

### Task 3: Expand GetDashboardErrorRetries tests (sync + async)
- Status: PASS
- Evidence: commit `c61ba49f`. Grep confirms structural parity with Jobs files:
  - `GetDashboardErrorRetriesQueryHandlerTests.cs`: 5 new `[TestMethod]`s at lines 47/74/99/109/119 (multi-row, prepare-query invocation, 3 ctor null-guards); pre-existing tests at lines 18/37 untouched; `CreateHandler(int rowCount)` helper at line 129 returning `IDataReader`.
  - `GetDashboardErrorRetriesQueryHandlerAsyncTests.cs`: 6 new `[TestMethod]`s at lines 66/89/110/134/144/154 (multi-row, awaited-task completion, prepare-query invocation, 3 ctor null-guards); pre-existing tests at lines 37/56 untouched; `CreateHandler(int rowCount)` at line 164 uses `DbDataReader`/`ReadAsync(Arg.Any<CancellationToken>())`.
  - No `CancellationToken` parameter on any `HandleAsync(query)` call in test code — only used inside mock `Arg.Any<CancellationToken>()` which is correct for the abstract ADO.NET Async APIs.
- Plan's 22-test total reconciles: sync 5+5 = 10, async 6+6 = 12, total 22.

**Stage 1 Verdict:** PASS.

## Stage 2 -- Code Quality

### Critical
(none)

### Important
(none)

### Suggestions
- `CreateHandler` rowCount loop at `GetDashboardJobsQueryHandlerTests.cs:137-139` (and mirrors in the other 3 files) builds a `bool[rowCount]` where the last element is assigned `false` but `rest[rowCount-1]` is never reached as a subsequent `Read()` return — the `Returns(true, rest)` sequence uses the first `true` plus `rest` values. Effective sequence is `true, rest[0..rowCount-1]` which is `rowCount` trues followed by one `false`. That is actually `rowCount+1` return values where only the last is `false`, so for `rowCount=3` the handler reads 3 true + 1 false = 3 iterations. This is correct but the `rest[rowCount - 1] = false` line is deliberately setting the tail to `false` which is what terminates the loop. Readable but worth a 1-line comment explaining the off-by-one intention for future maintainers.
  - Remediation: Add `// rest has rowCount entries: (rowCount-1) trues + 1 false; combined with the leading true this yields exactly rowCount rows` above line 137 in each of the 4 files.
- `Handle_Invokes_PrepareQuery_With_Correct_CommandString` in `GetDashboardJobsQueryHandlerTests.cs:66-89` duplicates the substitute setup already present in `CreateHandler`. Not harmful because this test asserts `prepareQuery.Received(...)` which requires a direct reference to the substitute (returning it from `CreateHandler` would be cleaner).
  - Remediation: Optionally extend `CreateHandler` to also return `prepareQuery` and `connection` to remove duplication across the 4 files — low priority, purely cosmetic.
- Async prepare-query assertion tests (e.g. `GetDashboardJobsQueryHandlerAsyncTests.cs:107-128`) do not assert `connection.Received(1).OpenAsync(...)` whereas the sync counterpart at line 88 does assert `Open()`. Minor parity gap — the async handlers call `OpenAsync` which is worth asserting for symmetry.
  - Remediation: Add `await connection.Received(1).OpenAsync(Arg.Any<CancellationToken>());` after the `prepareQuery.Received(...)` line in both async files.

### Positive
- Builder correctly followed the CONTEXT decisions: no cancellation tests on `HandleAsync` (since the handler signature takes no `CancellationToken`), no `// TODO` banners, and the `CreateHandler(int rowCount)` helper style preserved instead of forcibly migrating to `CreateFixture`. Consistent within each file.
- Uses MSTest 3.x idioms throughout: `Assert.ThrowsExactly<ArgumentNullException>` (not `ThrowsException`), `Assert.ContainsSingle`, `Assert.IsEmpty`. Matches project standard.
- Async tests use abstract `DbConnection`/`DbCommand`/`DbDataReader` rather than `IDbConnection` — required because async extension methods cannot be intercepted on the interfaces. Correct choice.
- No modifications to existing tests; no removed tests; no other files touched in the commits (verified via summary and git diff scope).
- Source handlers confirmed to have exactly 3 `Guard.NotNull` calls each — 3 ctor null-guard tests per file is complete, not missing any param.

## Summary
**Verdict:** APPROVE
Tests are correctly structured, match existing conventions, cover all planned gaps, and align with CONTEXT decisions (no cancellation tests, no TODO banners, `CreateHandler` helper preserved). Only cosmetic suggestions remain — none block merge.
Critical: 0 | Important: 0 | Suggestions: 3

Note: I did not execute `dotnet test` in this review session (builder already verified 18 pass per filter in SUMMARY-1.3.md). Test execution should be re-run by the verifier stage as standard gate.
