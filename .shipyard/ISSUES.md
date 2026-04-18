# Issues

## Open

_No open issues._

## Closed

### ISSUE-019: Missing SUMMARY-1.1.md artifact for Plan 1.1 (LiteDb history tests)
- **Severity:** Important
- **Source:** Plan 1.1 Review
- **Status:** Resolved — Phase 1 PLAN-1.3, 2026-04-16
- **Files:**
  - `.shipyard/phases/1/results/SUMMARY-1.1.md` (created)
- **Description:** The builder did not deposit a SUMMARY artifact before review. The test-run output (pass count, duration, LiteDB bug fix rationale) is unrecorded, breaking the audit trail. The results directory contains `REVIEW-1.2.md` but no `SUMMARY-1.1.md`.
- **Remediation:** Create `.shipyard/phases/1/results/SUMMARY-1.1.md` documenting the test run output and the LiteDB `Get()` workaround rationale.
- **Resolution:** Created SUMMARY-1.1.md documenting all 19 LiteDb history tests and the FindAll+LINQ workaround.

### ISSUE-020: LiteDbHistoryEnabledTests.CleanupAsync may double-dispose _scope after _creation.Dispose()
- **Severity:** Important
- **Source:** Plan 1.1 Review
- **Status:** Resolved — commit 4a474f10, 2026-04-16
- **Files:**
  - `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/LiteDbHistoryTests.cs` (lines 208-254, CleanupAsync)
- **Description:** `_scope` is assigned from `_creation.Scope`. `CleanupAsync` calls `_creation.Dispose()` then `_scope.Dispose()` independently. If `ICreationScope` is not idempotent on dispose, this is a double-dispose. The same pattern appears in MemoryHistoryTests, so it is likely safe, but it is unconfirmed.
- **Remediation:** Verify `ICreationScope.Dispose()` is idempotent. If confirmed, add a comment. If not, null `_scope` after `_creation.Dispose()` to guard against double-dispose.
- **Resolution:** Confirmed LiteDb CreationScope.Dispose() is idempotent (guarded by `_disposedValue`). Added 3-line clarifying comment in CleanupAsync.

### ISSUE-014: RelationalDatabase RecordComplete WHERE clause blocks DurationMs=0 write when StartedUtc IS NULL
- **Severity:** Important
- **Source:** Plan 1.1 Review
- **Status:** Resolved — commit b538823a, 2026-04-05
- **Files:**
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs` (line 131)
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/WriteMessageHistoryHandlerTests.cs` (`RecordComplete_WithoutStartedUtc_PassesDurationZero`)
- **Description:** The second UPDATE in `RecordComplete` contains `WHERE QueueID = @QueueID AND StartedUtc IS NOT NULL AND CompletedUtc IS NOT NULL AND DurationMs IS NULL`. When `StartedUtc` was never persisted, the WHERE predicate fails and the UPDATE is a no-op — leaving `DurationMs` as NULL in the database despite the C# correctly computing `durationMs = 0L`. The test `RecordComplete_WithoutStartedUtc_PassesDurationZero` verifies only that the `@DurationMs` parameter was set to `0L`, not that the row was actually updated. `RecordError` was correctly fixed (no StartedUtc guard). This is the residual unfixed portion of the original bug for the Complete path.
- **Resolution:** Removed `StartedUtc IS NOT NULL AND` from the WHERE clause. Strengthened the test to intercept and assert against the actual SQL CommandText, confirming the guard is absent.

### ISSUE-015: Dead local function `MakeTrackingParam` in RecordComplete_WithoutStartedUtc_PassesDurationZero test
- **Severity:** Suggestion
- **Source:** Plan 1.1 Review
- **Status:** Resolved — commit b538823a, 2026-04-05
- **Files:**
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/WriteMessageHistoryHandlerTests.cs` (lines ~174-185 of the new test method)
- **Description:** The `MakeTrackingParam()` local function is defined inside `RecordComplete_WithoutStartedUtc_PassesDurationZero` but is never called. The implementation pivots to using `MakeTrackingCommand` with the `allParams` list instead. The dead function adds noise.
- **Resolution:** Removed the unused `MakeTrackingParam()` local function from the test method.

### ISSUE-016: Redundant Redis round-trip in orphan path of PurgeMessageHistoryHandler
- **Severity:** Important
- **Source:** Plan 1.2 Review
- **Status:** Resolved — commit 54477f41, 2026-04-16
- **Files:**
  - `Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs` (Purge method, loop body)
- **Description:** `rawCompleted` is read unconditionally before the `!rawStatus.HasValue` guard. When the hash is absent (orphan case), this is a wasted Redis round-trip returning `RedisValue.Null` that is immediately discarded by `continue`. In bulk orphan scans this doubles Redis calls in the hot path.
- **Remediation:** Move `var rawCompleted = db.HashGet(...)` inside the `rawStatus.HasValue` branch, after the orphan `continue`.
- **Resolution:** Moved `rawCompleted` HashGet from before the orphan guard to after it, eliminating one Redis round-trip per orphan record.

### ISSUE-017: Orphan test does not assert CompletedUtc is never read (fragile test)
- **Severity:** Important
- **Source:** Plan 1.2 Review
- **Status:** Resolved — commit ac91a41e, 2026-04-16
- **Files:**
  - `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs` (`Purge_Handles_Missing_Hash_Gracefully`)
- **Description:** The orphan test stubs `Status` to `RedisValue.Null` but does not stub `CompletedUtc` and does not assert it is never called. NSubstitute silently returns default `RedisValue.Null` for the unstubbed call. If the read order changes or the guard moves, the test passes against the wrong code path. Once ISSUE-016 is fixed, add `db.DidNotReceive().HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("CompletedUtc"), Arg.Any<CommandFlags>())` to make the contract explicit.
- **Remediation:** After applying ISSUE-016 fix, add the `DidNotReceive` assertion for `CompletedUtc` in the orphan test.
- **Resolution:** Added `db.DidNotReceive().HashGet(..., "CompletedUtc", ...)` assertion in `Purge_Handles_Missing_Hash_Gracefully` test.

### ISSUE-018: No test for Enqueued status in PurgeMessageHistoryHandler
- **Severity:** Suggestion
- **Source:** Plan 1.2 Review
- **Status:** Resolved — commit 67ca863a, 2026-04-16
- **Files:**
  - `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs`
- **Description:** `Purge_Skips_Processing_Records` covers status=1 (Processing). There is no test for status=0 (Enqueued), the other non-terminal state the original bug would have deleted. Adding `Purge_Skips_Enqueued_Records` would document that both active states are protected.
- **Remediation:** Add `Purge_Skips_Enqueued_Records` mirroring `Purge_Skips_Processing_Records` with `MessageHistoryStatus.Enqueued`.
- **Resolution:** Added `Purge_Skips_Enqueued_Records` test with XML doc explaining the RedisValue.Null-to-int collision for Enqueued=0.

### ISSUE-024: SharedSetup.CreateTrace leaks OpenTelemetry TracerProvider when TraceSettings.Enabled is true
- **Severity:** Suggestion
- **Source:** Phase 1 Plan 1.2 Review
- **Status:** Resolved — commit bf408f64, 2026-04-16
- **Files:**
  - `Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs` (lines 161-180, `CreateTrace` method)
- **Description:** Pre-existing latent issue (NOT introduced by Plan 1.2). When `TraceSettings.Enabled` is `true`, `CreateTrace` builds a `TracerProvider` via `Sdk.CreateTracerProviderBuilder()...Build()` and assigns it to a local variable `openTelemetry` which is immediately discarded. The provider is never disposed and the OTLP batch exporter background worker leaks for the test run. The 2-second sleep in `ActivitySourceWrapper.Dispose` is a workaround for the missing flush. This was harmless before because `TraceSettings.Enabled` is currently never true in CI, but if a future change enables it, exports will be incomplete and the provider will leak.
- **Remediation:** Store the provider on `ActivitySourceWrapper` (e.g., `private readonly TracerProvider _provider`) and dispose it in `Dispose()` before the source. Replace the `Thread.Sleep(2000)` with `_provider?.ForceFlush(2000)` for deterministic flushing.
- **Resolution:** Stored TracerProvider on ActivitySourceWrapper, replaced Thread.Sleep(2000) with ForceFlush(2000) + Dispose(). Null-safe when TraceSettings.Enabled is false.

### ISSUE-031: Transitive `System.Security.Cryptography.Xml 8.0.2` high-severity CVE (NU1903) via TaskScheduler integration tests
- **Severity:** High (CVE, but surface is test-only)
- **Source:** Phase 2 (dependency-refresh milestone) — surfaced during Debug build after bumping low-risk packages
- **Repo:** DotNetWorkQueue (this repo)
- **Status:** Resolved — Phase 3 Wave 5 PLAN-5.1, commit `3d6b9949`, 2026-04-17. Added `<PackageVersion Include="System.Security.Cryptography.Xml" Version="10.0.6" />` to `Source/Directory.Packages.props` and `<PackageReference>` to `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj`. NU1903 grep on `dotnet build -c Debug` returns 0 matches for this package.
- **Files:**
  - `Source/Directory.Packages.props`
  - `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj`
- **Description:** Debug build surfaced 4× NU1903 warnings: `Package 'System.Security.Cryptography.Xml' 8.0.2 has a known high severity vulnerability` (GHSA-37gx-xxp4-5rgx and GHSA-w3x6-4m5h-cxqf). The package was a transitive dep pulled in through the TaskScheduler 0.5.0 NuGet via test-only paths — not in `Directory.Packages.props` directly. Consumer shipping surface was unaffected.
- **Resolution:** Option 1 chosen — added a direct CPM `<PackageVersion>` at 10.0.6 to force the patched transitive. Cheap, reversible, no upstream coordination needed. Fix landed alongside Phase 3 major bumps in PR #118.

### ISSUE-023: Stray blank line and double blank line artifacts from NETFULL removal
- **Severity:** Suggestion
- **Source:** simplifier (Phase 3)
- **Status:** Resolved -- commit 9df8c735, 2026-04-07
- **Files:**
  - `Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs` (line 14, blank line between `[TestMethod]` and `[DataRow]`)
  - `Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj` (lines 22-23, double blank line where net48 ItemGroup was removed)
- **Description:** Phase 3 NETFULL removal left cosmetic artifacts: a stray blank line in PostgreSQL JobSchedulerTests where an empty `#if NETFULL`/`#else`/`#endif` block was removed, and a double blank line in the Memory csproj where the net48 conditional ItemGroup was deleted.
- **Resolution:** Deleted the blank line from PostgreSQL JobSchedulerTests. Removed one of the two blank lines from Memory csproj. Batched with ISSUE-021 cleanup.

### ISSUE-022: No-op `dynamic=true` test case in PostgreSQL JobSchedulerTests
- **Severity:** Important
- **Source:** simplifier (Phase 3)
- **Status:** Resolved -- commit 9df8c735, 2026-04-07
- **Files:**
  - `Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs` (line 16, `DataRow(true, true)`)
  - `Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs` (pre-existing, `DataRow(true)`)
- **Description:** The shared `JobSchedulerTests.Run<>()` implementation guards all test logic with `if (!dynamic)`. When `dynamic=true`, the test creates a queue, executes zero assertions, then tears down -- a vacuously passing no-op.
- **Resolution:** Removed `DataRow(true, true)` from PostgreSQL and `DataRow(true)` from LiteDb. Removed vestigial `bool dynamic` parameter from shared implementation and all 6 transport callers.

### ISSUE-021: Empty shell files after NETFULL removal
- **Severity:** Suggestion
- **Source:** Phase 3 Plan 1.1 + 1.2 Reviews
- **Status:** Resolved -- commit d410f2f1, 2026-04-07
- **Files:**
  - `Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs`
  - `Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs`
  - `Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs`
  - `Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs`
  - `Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs`
  - `Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs`
  - `Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/ProducerMethod/SimpleMethodProducerDynamicListSend.cs`
- **Description:** After removing `#if NETFULL` blocks, these files contained only unused `using` directives and an empty namespace. The entire class in each file was NETFULL-only, so nothing meaningful remained.
- **Resolution:** Deleted all 7 empty shell files.

### ISSUE-001: Unused `fixture` variable in QueueCreatorTests after Plan 1.1 refactor
- **Severity:** Important
- **Source:** Plan 1.1 Review
- **Files:**
  - `Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs` (lines 20, 36, 52, 68, 104)
  - `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs` (lines 21, 37, 53, 69, 105)
  - `Source/DotNetWorkQueue.Transport.SQLite.Tests/QueueCreatorTests.cs` (lines 25, 41, 52, 63, 89)
- **Description:** After replacing `fixture.Create<string>()` with `"TestQueue"`, the `fixture` variable is unused in 5 of 7 methods per file (15 total). This produces CS0219 warnings on Debug and build failures on Release (`TreatWarningsAsErrors`).
- **Remediation:** Remove the unused `var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());` line from the 5 methods per file that no longer reference `fixture`.
- **Resolution:** Already resolved -- no unused fixture variables found in transport QueueCreatorTests files. Closed 2026-03-30.

### ISSUE-002: Regex not compiled in ValidateQueueName across relational transports
- **Severity:** Important
- **Source:** Plan 1.1 Review
- **Files:**
  - `Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs` (line 93)
  - `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs` (line 72)
  - `Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs` (line 68)
- **Description:** `Regex.IsMatch(name, @"^[a-zA-Z0-9_.]+$")` recompiles the pattern on each call. While .NET's regex cache mitigates this, a `private static readonly Regex` with `RegexOptions.Compiled` would be more explicit and performant.
- **Remediation:** Add `private static readonly Regex ValidNamePattern = new Regex(@"^[a-zA-Z0-9_.]+$", RegexOptions.Compiled);` and use `ValidNamePattern.IsMatch(name)`.
- **Resolution:** Already resolved -- all 5 transports already use `RegexOptions.Compiled`. Closed 2026-03-30.

### ISSUE-003: QueueName_Valid_Alphanumeric tests do not assert QueueName property value
- **Severity:** Suggestion
- **Source:** Plan 1.1 Review
- **Files:**
  - `Source/DotNetWorkQueue.Transport.SqlServer.Tests/SqlConnectionInformationTests.cs` (line 46)
  - `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/SqlConnectionInformationTests.cs` (line 46)
  - `Source/DotNetWorkQueue.Transport.SQLite.Tests/SQLiteConnectionInformationTests.cs` (line 36)
- **Description:** Plan Task 3 describes verifying `QueueName == "MyQueue123"` but tests only assert `IsNotNull`. Adding the property assertion would strengthen the test.
- **Remediation:** Add `Assert.AreEqual("MyQueue123", test.QueueName);` after `Assert.IsNotNull`.
- **Resolution:** Added `Assert.AreEqual("MyQueue123", test.QueueName)` to 3 relational transport test files. Closed 2026-03-30.

### ISSUE-004: QueueName_Valid_Alphanumeric tests only assert IsNotNull (non-relational transports)
- **Severity:** Suggestion
- **Source:** Plan 1.2 Review
- **Files:**
  - `Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs` (line 50)
  - `Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs` (line 35)
  - `Source/DotNetWorkQueue.Tests/Transport/Memory/ConnectionInformationTests.cs` (line 38)
- **Description:** Same pattern as ISSUE-003. The valid alphanumeric tests assert `IsNotNull` but do not verify `test.QueueName == "MyQueue123"`, which would confirm the name survives validation and is stored correctly.
- **Remediation:** Add `Assert.AreEqual("MyQueue123", test.QueueName);` after `Assert.IsNotNull(test)` in each test.
- **Resolution:** Added `Assert.AreEqual("MyQueue123", test.QueueName)` to Redis, LiteDb, Memory test files. Closed 2026-03-30.

### ISSUE-005: Stale XML doc comment on Memory ConnectionInformation class
- **Severity:** Suggestion
- **Source:** Plan 1.2 Review
- **Files:**
  - `Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs` (line 28)
- **Description:** The class summary says "Contains connection information for a SQL server queue" but this is the Memory transport's connection info class.
- **Remediation:** Change to "Contains connection information for a memory transport queue".
- **Resolution:** Fixed XML doc from 'SQL server queue' to 'memory queue' in Memory ConnectionInformation. Closed 2026-03-30.

### ISSUE-006: Unused using directives in RedisConnectionInfoTests
- **Severity:** Suggestion
- **Source:** Plan 1.2 Review
- **Files:**
  - `Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs` (lines 2-5)
- **Description:** `System.Collections.Generic`, `System.Linq`, `System.Text`, `System.Threading`, `System.Threading.Tasks` are unused imports that pre-date the current changes.
- **Remediation:** Remove the five unused `using` statements.
- **Resolution:** Removed 5 unused using directives from RedisConnectionInfoTests.cs. Closed 2026-03-30.

### ISSUE-007: DisposeAsync uses synchronous Timer.Dispose() instead of Timer.DisposeAsync()
- **Severity:** Suggestion
- **Source:** Plan 1.1 Review (Phase 3)
- **Files:**
  - `Source/DotNetWorkQueue.Dashboard.Client/DashboardConsumerClient.cs` (line 260)
- **Description:** `DisposeAsync()` calls `_heartbeatTimer.Dispose()` synchronously. Since the project targets net8.0+ where `Timer.DisposeAsync()` is available, using `await _heartbeatTimer.DisposeAsync()` would ensure any in-flight timer callback completes before disposal finishes, avoiding a potential race where a heartbeat callback fires concurrently with resource cleanup.
- **Remediation:** Replace `_heartbeatTimer.Dispose();` with `await _heartbeatTimer.DisposeAsync().ConfigureAwait(false);` in the `DisposeAsync` method.
- **Resolution:** Replaced `_heartbeatTimer.Dispose()` with `await _heartbeatTimer.DisposeAsync().ConfigureAwait(false)`. Closed 2026-03-30.

### ISSUE-008: DisposeAsync_Owned_HttpClient_Is_Disposed test uses sync-over-async in assertion
- **Severity:** Suggestion
- **Source:** Plan 1.1 Review (Phase 3)
- **Files:**
  - `Source/DotNetWorkQueue.Dashboard.Client.Tests/DashboardConsumerClientTests.cs` (line 845)
- **Description:** The test verifies HttpClient disposal by calling `httpClient.GetAsync(...).GetAwaiter().GetResult()` inside an `Action` lambda. While acceptable in test code, this could be replaced with an async assertion using `Func<Task>` and `act.Should().ThrowAsync<ObjectDisposedException>()` for consistency with the project's async test patterns.
- **Remediation:** Replace the `Action`/`act.Should().Throw<ObjectDisposedException>()` pattern with `Func<Task> act = () => httpClient.GetAsync("http://localhost:5000/test"); await act.Should().ThrowAsync<ObjectDisposedException>();`.
- **Resolution:** Replaced sync-over-async `Action`/`Throw` with `Func<Task>`/`ThrowAsync` pattern. Closed 2026-03-30.

### ISSUE-009: Log messages in PrimaryWorker.Stop() and Worker.Stop() still say "worker thread"
- **Severity:** Suggestion
- **Source:** Phase 7 Plan 02 Review
- **Files:**
  - `Source/DotNetWorkQueue/Queue/PrimaryWorker.cs` (line 101)
  - `Source/DotNetWorkQueue/Queue/Worker.cs` (line 96)
- **Description:** Plan 02 Task 1 specifies changing log messages from `"Stopping worker thread {WorkerName}"` to `"Stopping worker {WorkerName}"` to reflect the Thread-to-Task migration. Both files still use the word "thread" in the Stop() log message. This is cosmetic only and does not affect functionality.
- **Remediation:** Change `$"Stopping worker thread {WorkerName}"` to `$"Stopping worker {WorkerName}"` in both files.
- **Resolution:** Changed 'Stopping worker thread' to 'Stopping worker' in PrimaryWorker.cs and Worker.cs. Closed 2026-03-30.

### ISSUE-010: Unused `using System.Threading;` in WorkerTerminate.cs
- **Severity:** Suggestion
- **Source:** Phase 7 Plan 02 Review
- **Files:**
  - `Source/DotNetWorkQueue/Queue/WorkerTerminate.cs` (line 20)
- **Description:** After replacing `Thread` with `Task`, `WorkerTerminate.cs` no longer references any type from `System.Threading` (`Thread`, `Monitor`, `Interlocked`, `CancellationToken` are all absent). The `using System.Threading;` directive is now unused.
- **Remediation:** Remove `using System.Threading;` from WorkerTerminate.cs.
- **Resolution:** Removed unused `using System.Threading;` from WorkerTerminate.cs. Closed 2026-03-30.

### ISSUE-011: Unused `using System.Threading;` in WaitForThreadToFinish.cs
- **Severity:** Suggestion
- **Source:** Phase 7 Plan 02 Review
- **Files:**
  - `Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs` (line 21)
- **Description:** After removing the Thread polling loop, `WaitForThreadToFinish.cs` no longer references any type from `System.Threading`. The using directive is unused.
- **Remediation:** Remove `using System.Threading;` from WaitForThreadToFinish.cs.
- **Resolution:** Removed unused `using System.Threading;` from WaitForThreadToFinish.cs. Closed 2026-03-30.

### ISSUE-012: Missing SUMMARY file for Phase 7 Plan 01 (BaseMonitor)
- **Severity:** Suggestion
- **Source:** Phase 7 Review
- **Files:**
  - `.shipyard/phases/7/SUMMARY-plan01.md` (missing)
- **Description:** Plan 02 has a summary at `.shipyard/phases/7/SUMMARY-plan02.md` but Plan 01 (BaseMonitor spin-wait replacement) has no corresponding summary file. The implementation is correct, but the missing summary breaks the documentation trail.
- **Remediation:** Create `.shipyard/phases/7/SUMMARY-plan01.md` documenting the BaseMonitor changes.
- **Resolution:** Created `.shipyard/phases/7/SUMMARY-plan01.md` documenting BaseMonitor modernization. Closed 2026-03-30.

### ISSUE-013: MultiWorkerBase.Running lacks explicit parentheses for operator precedence clarity
- **Severity:** Suggestion
- **Source:** Phase 7 Plan 02 Review
- **Files:**
  - `Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs` (line 62)
- **Description:** The expression `WorkerTask != null && !WorkerTask.IsCompleted || MessageProcessing != null && MessageProcessing.AsyncTaskCount > 0` relies on implicit operator precedence (`&&` before `||`). While correct, explicit parentheses would improve readability: `(WorkerTask != null && !WorkerTask.IsCompleted) || (MessageProcessing != null && MessageProcessing.AsyncTaskCount > 0)`.
- **Remediation:** Add parentheses to make the grouping explicit.
- **Resolution:** Added explicit parentheses to `MultiWorkerBase.Running` property expression. Closed 2026-03-30.
