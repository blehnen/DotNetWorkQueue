# Issues

## Open
(none)

## Closed

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
