# Phase 2 Plan Critique

**Phase:** 2-coverage-job-scheduler-handlers
**Date:** 2026-04-12
**Type:** plan-review (feasibility stress test)

## Plans Reviewed

- PLAN-1.1 - CreateJobTablesCommandHandler tests (new file)
- PLAN-1.2 - GetJobIdQueryHandler + GetJobLastKnownEventQueryHandler tests (new files)
- PLAN-1.3 - Expand 4 existing dashboard test files

All three plans are Wave 1, no declared dependencies.

## PLAN-1.1 - CreateJobTablesCommandHandler Tests

| # | Check | Status | Evidence |
|---|-------|--------|----------|
| 1 | Source handler exists | PASS | `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/CommandHandler/CreateJobTablesCommandHandler.cs` present. |
| 2 | Ctor signature matches plan | PASS | File lines 41-43: `(IDbConnectionFactory, IPrepareCommandHandler<CreateJobTablesCommand<ITable>>, ITransactionFactory)` - matches plan verbatim. |
| 3 | Handle() flow matches plan narrative | PASS | Lines 54-71: `Create()` -> `Open()` -> `_transactionFactory.Create(conn).BeginTransaction()` -> `conn.CreateCommand()` -> `commandSql.Transaction = trans` -> `_prepareCommandHandler.Handle(command, commandSql, CommandStringTypes.CreateJobTables)` -> `ExecuteNonQuery()` -> `trans.Commit()` -> `new QueueCreationResult(QueueCreationStatus.Success)`. Plan's four happy-path assertions target exactly these steps. |
| 4 | Guard.NotNull for all 3 deps | PASS | Lines 45-47. Supports the 3 null-guard tests. |
| 5 | Target folder new | PASS | Plan correctly notes `Basic/CommandHandler/` doesn't exist and must be created. |
| 6 | Reference style file exists | PASS | `DoesJobExistQueryHandlerTests.cs` exists (62 TestClass/Method/Substitute occurrences - rich pattern to copy). |
| 7 | Verify command valid | PASS | `dotnet test --filter "FullyQualifiedName~CreateJobTablesCommandHandlerTests"` is syntactically valid MSTest filter syntax. |
| 8 | Files touched isolation | PASS | Touches only 1 new file under `Basic/CommandHandler/`. No overlap with 1.2 or 1.3. |

**Minor notes (non-blocking):**
- Plan hedges on `Guard.NotNull` exception type ("check Guard.NotNull behavior first"). Builder should confirm but this is appropriate caution, not a gap.
- `IDbConnectionFactory.Create()` returns `IDbConnection` - the plan says mock this correctly; signatures align.
- `ITransactionFactory.Create(conn)` returns `ITransactionWrapper` whose `BeginTransaction()` returns `IDbTransaction` - plan gets the double-hop right (an easy thing to miss).

**Verdict:** READY.

## PLAN-1.2 - GetJobIdQueryHandler + GetJobLastKnownEventQueryHandler Tests

| # | Check | Status | Evidence |
|---|-------|--------|----------|
| 1 | GetJobIdQueryHandler exists | PASS | File present at `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/QueryHandler/GetJobIdQueryHandler.cs`. |
| 2 | GetJobId ctor matches plan | PASS | Line 38: `GetJobIdQueryHandler(IPrepareQueryHandler<GetJobIdQuery<T>, T> prepareQuery, IDbConnectionFactory, IReadColumn)`. Plan's 3-dep description is exact. |
| 3 | GetJobId Handle matches plan | PASS | Lines 52, 59, 64: `Handle(GetJobIdQuery<T>)`, calls `_prepareQuery.Handle(query, command, CommandStringTypes.GetJobId)`, returns `_readColumn.ReadAsType<T>(CommandStringTypes.GetJobId, 0, reader)`. |
| 4 | GetJobLastKnownEventQueryHandler exists | PASS | File present at same folder. |
| 5 | GetJobLastKnownEvent ctor/Handle match plan | PASS | Line 39: `(IPrepareQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>, IDbConnectionFactory, IReadColumn)`. Line 52 `Handle(GetJobLastKnownEventQuery)`. Line 62: `reader.Read() ? _readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetJobLastKnownEvent, 0, reader) : default(DateTimeOffset)`. Plan's branch enumeration is correct. |
| 6 | Both handlers use Guard.NotNull on 3 deps | PASS | GetJobId lines 42-44; GetJobLastKnownEvent lines 43-45. |
| 7 | Query ctor args | PASS | `GetJobIdQuery(string jobName)` at `Transport.Shared/Basic/Query/GetJobIDQuery.cs:28` and `GetJobLastKnownEventQuery(string jobName)` at `GetJobLastKnownEventQuery.cs:33`. Plan's `new GetJobIdQuery<long>("jobName")` and `new GetJobLastKnownEventQuery("jobName")` calls are valid. |
| 8 | Target folder already exists | PASS | Plan correctly notes `Basic/QueryHandler/` folder already exists. |
| 9 | Verify commands valid | PASS | Both filter expressions parse. |
| 10 | No file overlap with 1.1 / 1.3 | PASS | Two NEW files; plan 1.3 touches 4 different existing files in same folder - no collision. |

**Minor notes:**
- Plan closes the generic at `GetJobIdQueryHandler<long>` - reasonable since `GetJobIdQuery<T>` is used for job ids and long is the canonical choice.
- `readColumn.ReadAsType<long>` / `ReadAsDateTimeOffset` are interface methods on `IReadColumn` (shared). Mockable with NSubstitute. No known sealed-type trap.

**Verdict:** READY.

## PLAN-1.3 - Expand Dashboard Job/Error Handler Tests

| # | Check | Status | Evidence |
|---|-------|--------|----------|
| 1 | All 4 handler sources exist | PASS | GetDashboardJobsQueryHandler(.cs/Async.cs) and GetDashboardErrorRetriesQueryHandler(.cs/Async.cs) all present. |
| 2 | All 4 existing test files exist | PASS | Sync + Async test files for both present in `Transport.RelationalDatabase.Tests/Basic/QueryHandler/`. |
| 3 | Ctor shape uniform across the 4 handlers | PASS | Each has 3 deps: `IPrepareQueryHandler<...>`, `IDbConnectionFactory`, `IReadColumn`, with `Guard.NotNull` on all three. Sync returns `IReadOnlyList<DashboardJob>` / `IReadOnlyList<DashboardErrorRetry>`; Async versions return `Task<IReadOnlyList<...>>` via `HandleAsync`. |
| 4 | Plan task 1 adds only comments | PASS | Task 1 is a read-and-annotate pass that won't alter test behavior. Safe pre-work. |
| 5 | Task 2/3 add new `[TestMethod]` tests | PASS | Plan explicitly says "do not remove/rename existing tests" - additive only. |
| 6 | Coverage threshold stated | PASS | >70% line coverage per class via Coverlet cobertura. Concrete, measurable. |
| 7 | Verify command valid | PASS | `--filter "FullyQualifiedName~GetDashboardJobs" --collect:"XPlat Code Coverage"` is valid. |
| 8 | No file overlap with 1.1 / 1.2 | PASS | 4 existing files, all distinct from 1.1 (new CommandHandler file) and 1.2 (new QueryHandler files). |
| 9 | Risk labeling | CAUTION | Plan self-labels risk as `medium`. Touching 4 files in one plan is the highest-coupling plan of the three, though all 4 are additive-only. |

**Concerns (non-blocking):**

1. **Cancellation token assertion is speculative.** Plan task 2 says "Async file only - a cancellation / task-completion test". Verified by grep that current `HandleAsync(GetDashboardJobsQuery query)` / `HandleAsync(GetDashboardErrorRetriesQuery query)` signatures do NOT accept a `CancellationToken` parameter. The plan wording "task-completion test that awaits the handler" is fine, but the word "cancellation" should be dropped or clarified as "the async method completes" - there is no token to cancel. Builder should simply await and assert the result; an actual cancellation test would be dead code since the method doesn't observe a token. Recommend tightening Task 2's wording before build.

2. **Task 1 emits `// TODO: coverage gaps` banner comments** into production test files. These stay in the codebase even after Tasks 2/3 close the gaps. Either remove the comments at the end of Task 3 or reframe them as a scratch file/analysis artifact. Not a blocker, but leaves low-value cruft.

3. **Coverage measurement dependency.** The >70% threshold requires Coverlet to run locally and the builder must parse `coverage.cobertura.xml`. Plans 1.1 and 1.2 measure pass/fail on tests directly. Plan 1.3's success criterion is stricter and more fragile (coverage tool invocation, report parsing). Mitigation: the plan uses the right invocation (`--collect:"XPlat Code Coverage"`) so this should just work, but verifier notes the extra tooling dependency.

**Verdict:** CAUTION - ready to build with two small copy-edits (cancellation wording, TODO comment lifecycle).

## Cross-Plan Checks

| Check | Result |
|-------|--------|
| File conflicts between plans | NONE - disjoint file sets. |
| Circular dependencies | NONE - all plans have empty `dependencies:`. |
| Parallel-safety in Wave 1 | OK - can run in parallel; no shared source files, no shared test fixtures, no shared project-level config. |
| All plans target same test project | YES - `DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj`. Multiple parallel edits to the same `.csproj` file are not expected (only .cs files added/edited). |
| Build-level risk | LOW - adding `.cs` files under an existing `<Compile Include="**\*.cs" />` style SDK project picks them up automatically; no `.csproj` edits required. |
| Coverage of phase goal | OK - Phase 2 goal is "unit tests for shared job scheduler handlers in Transport.RelationalDatabase". Plans 1.1 and 1.2 directly add the missing handler tests; plan 1.3 raises coverage on the adjacent dashboard handlers. Together they address the rescoped phase scope per RESEARCH.md/CONTEXT-2.md. |
| Pattern consistency | OK - all three plans anchor on the same reference (`DoesJobExistQueryHandlerTests`) and same mocking library (NSubstitute). |

## Overall Verdict

**READY WITH CAUTION**

- PLAN-1.1: READY
- PLAN-1.2: READY
- PLAN-1.3: CAUTION - two copy-edits recommended before build (drop `CancellationToken` wording since `HandleAsync` takes no token; decide whether coverage-gap TODO comments stay in the repo after Tasks 2/3 close them). Neither blocks execution; both are quality concerns the builder can resolve inline.

All three plans can proceed in parallel as Wave 1. The phase is feasible as specified.
