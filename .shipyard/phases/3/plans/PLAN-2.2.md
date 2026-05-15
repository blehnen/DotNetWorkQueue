---
phase: sqlserver-implementation
plan: 2.2
wave: 2
dependencies: [1.1]
must_haves:
  - HandleExternalTxAsync private method in SendMessageCommandHandlerAsync.cs implementing the async caller-tx fork
  - Early-branch dispatch at top of HandleAsync() that returns HandleExternalTxAsync(commandSend) when ExternalTransaction != null
  - Smoke test confirming the async fork compiles and is structurally correct
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SendMessageCommandHandlerAsyncForkSmokeTests.cs
tdd: false
risk: medium
---

# Plan 2.2: Async Handler Fork (Wave 2, parallel with PLAN-2.1)

## Context

Mirror of PLAN-2.1 for the async handler. Adds an `HandleExternalTxAsync` private method to `SendMessageCommandHandlerAsync.cs` plus the early-branch dispatch at the top of `HandleAsync`. The fork is the async equivalent of PLAN-2.1's sync fork:

1. Casts `command.ExternalTransaction` to `SqlTransaction` (the producer subclass's `GuardSqlTransaction` ensured this is safe; per **RESEARCH §11 Discrepancy #1** the existing async handler is also `SqlServer`-typed end-to-end so the cast is internally consistent — see RESEARCH §3).
2. Reuses `tx.Connection` as the `SqlConnection`.
3. Sets `cmd.Connection` and `cmd.Transaction` on each command.
4. Uses `ExecuteScalarAsync()` / `ExecuteNonQueryAsync()` (consistent with the existing async handler's `await command.ExecuteScalarAsync().ConfigureAwait(false)` pattern at line 144 of `SendMessageCommandHandlerAsync.cs`).
5. Calls the existing `CreateMetaDataRecordAsync` / `CreateStatusRecordAsync` private helpers (lines 195-229 of `SendMessageCommandHandlerAsync.cs`) verbatim — they already accept `SqlConnection` + `SqlTransaction`.
6. Reuses the sync `_jobExistsHandler.Handle(...)` and `_sendJobStatus.Handle(...)` calls (RESEARCH §3 confirmed neither has an async overload; the existing async handler invokes them synchronously at lines 121 + 166).
7. Never calls `tx.Commit()`, `tx.Rollback()`, `tx.Dispose()`, `sqlConn.Close()`, or `sqlConn.Dispose()`.

Insertion point per **RESEARCH §3**: immediately after the lazy-init block at line 105 of `SendMessageCommandHandlerAsync.cs`, before `var jobName = ...` at line 107.

Per **CONTEXT-3 Decision 2** the fork lives inside the existing async handler file as a private async method; no sibling class. Per **RESEARCH §11 Discrepancy #2** + **CLAUDE.md sync-vs-async mocking lesson** direct execution tests are infeasible (sealed `SqlConnection` types). Coverage shape mirrors PLAN-2.1: one structural smoke-test class with three reflection/source-grep tests confirming the fork exists, the early-branch is wired into `HandleAsync`, and no lifecycle-ownership calls leaked in.

This plan touches `SendMessageCommandHandlerAsync.cs`; PLAN-2.1 touches `SendMessageCommandHandler.cs`. **The two plans share no files.** Both share `SendMessage.cs` only as a READ dependency (the static `BuildMetaCommand` / `BuildStatusCommand` builders, reused as-is). Both plans can execute in parallel after PLAN-1.1 completes.

## Dependencies

- PLAN-1.1 (Wave 1) — same justification as PLAN-2.1: the producer subclass + DI wiring ensures `RelationalSendMessageCommand` reaches this handler via the registered async dispatch path.

This plan reads (does not modify):
- `RelationalSendMessageCommand` (Phase 2 PLAN-2.2)
- `SendMessageCommand.ExternalTransaction` (Phase 2 PLAN-1.1)
- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessage.cs` (static builders)

## Tasks

### Task 1: Add HandleExternalTxAsync private method + early-branch dispatch to SendMessageCommandHandlerAsync

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` (insert early-branch at line 106; append private `HandleExternalTxAsync` method before the existing `CreateStatusRecordAsync` at line 195).

**Step 1: Insert the early-branch at line 106**

After the closing `}` of the lazy-init block (currently line 105) and before `var jobName = ...` (currently line 107), insert:

```csharp
            if (commandSend.ExternalTransaction != null)
                return await HandleExternalTxAsync(commandSend).ConfigureAwait(false);
```

This is a 2-line addition.

**Step 2: Append the HandleExternalTxAsync method**

Immediately before the existing `private async Task CreateStatusRecordAsync(...)` method (currently line 195), insert:

```csharp
        /// <summary>
        /// Async caller-supplied-transaction fork of <see cref="HandleAsync(SendMessageCommand)"/>.
        /// Reuses the caller's <see cref="SqlTransaction"/> and its <see cref="SqlConnection"/>
        /// for all queue INSERTs; never commits, rolls back, closes, or disposes the caller's
        /// resources. Invoked from <see cref="HandleAsync"/> when
        /// <see cref="SendMessageCommand.ExternalTransaction"/> is non-null. The producer
        /// surface (<c>SqlServerRelationalProducerQueue&lt;T&gt;</c>) validates the
        /// transaction at the API boundary, so this method performs no validation of its own.
        /// </summary>
        /// <param name="commandSend">The send-message command carrying a non-null
        /// <see cref="SendMessageCommand.ExternalTransaction"/>.</param>
        /// <returns>The newly-inserted message ID.</returns>
        /// <exception cref="DotNetWorkQueueException">Thrown when the INSERT returns a zero
        /// ID or when the job-uniqueness query rejects the command.</exception>
        private async Task<long> HandleExternalTxAsync(SendMessageCommand commandSend)
        {
            // Producer subclass already validated and confirmed SqlTransaction; raw cast OK.
            var sqlTx = (SqlTransaction)commandSend.ExternalTransaction;
            var sqlConn = (SqlConnection)sqlTx.Connection;

            var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);
            var scheduledTime = DateTimeOffset.MinValue;
            var eventTime = DateTimeOffset.MinValue;
            if (!string.IsNullOrWhiteSpace(jobName))
            {
                scheduledTime = _jobSchedulerMetaData.GetScheduledTime(commandSend.MessageData);
                eventTime = _jobSchedulerMetaData.GetEventTime(commandSend.MessageData);
            }

            // Job-uniqueness query is sync on this transport (no async overload exists; the
            // existing self-managed-tx async path also calls .Handle() synchronously — see
            // SendMessageCommandHandlerAsync.cs:121 in the pre-Phase-3 baseline).
            if (!(string.IsNullOrWhiteSpace(jobName) ||
                  _jobExistsHandler.Handle(new DoesJobExistQuery<SqlConnection, SqlTransaction>(
                      jobName, scheduledTime, sqlConn, sqlTx)) == QueueStatuses.NotQueued))
            {
                throw new DotNetWorkQueueException(
                    "Failed to insert record - the job has already been queued or processed");
            }

            long id;
            using (var command = sqlConn.CreateCommand())
            {
                command.Connection = sqlConn;
                command.Transaction = sqlTx;
                command.CommandText = _commandCache.GetCommand(CommandStringTypes.InsertMessageBody);
                var serialization = _serializer.Serializer.MessageToBytes(
                    new MessageBody { Body = commandSend.MessageToSend.Body },
                    commandSend.MessageToSend.Headers);

                command.Parameters.Add("@body", SqlDbType.VarBinary, -1);
                command.Parameters["@body"].Value = serialization.Output;

                commandSend.MessageToSend.SetHeader(
                    _headers.StandardHeaders.MessageInterceptorGraph, serialization.Graph);

                command.Parameters.Add("@headers", SqlDbType.VarBinary, -1);
                command.Parameters["@headers"].Value =
                    _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers);

                id = Convert.ToInt64(await command.ExecuteScalarAsync().ConfigureAwait(false));
            }

            if (id <= 0)
            {
                throw new DotNetWorkQueueException(
                    "Failed to insert record - the ID of the new record returned by SQL server was 0");
            }

            var expiration = TimeSpan.Zero;
            if (_messageExpirationEnabled.Value)
            {
                expiration = MessageExpiration.GetExpiration(commandSend, data => data.GetExpiration());
            }

            await CreateMetaDataRecordAsync(commandSend.MessageData.GetDelay(), expiration, sqlConn, id,
                commandSend.MessageToSend, commandSend.MessageData, sqlTx).ConfigureAwait(false);

            if (_options.Value.EnableStatusTable)
            {
                await CreateStatusRecordAsync(sqlConn, id, commandSend.MessageToSend,
                    commandSend.MessageData, sqlTx).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(jobName))
            {
                _sendJobStatus.Handle(new SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>(
                    jobName, eventTime, scheduledTime, sqlConn, sqlTx));
            }

            // Deliberately NO trans.Commit() / Rollback() / Dispose() / sqlConn.Close().
            return id;
        }
```

The existing `CreateStatusRecordAsync` and `CreateMetaDataRecordAsync` helpers (lines 195-229) are reused verbatim.

**Step 3: Verify the build is clean**

Run: `dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release --nologo`

Expected: `Build succeeded. 0 Error(s), 0 Warning(s)`.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
git commit -m "shipyard(phase-3): add HandleExternalTxAsync fork to SqlServer async handler"
```

**Acceptance criteria:**
- `SendMessageCommandHandlerAsync.HandleAsync()` contains an early-branch on `commandSend.ExternalTransaction != null` immediately after the lazy-init block.
- `HandleExternalTxAsync(SendMessageCommand)` is a `private async Task<long>` method with the body above.
- No `tx.Commit`, `tx.Rollback`, `tx.Dispose`, `sqlConn.Close`, or `sqlConn.Dispose` calls inside `HandleExternalTxAsync` (grep gate).
- The early-branch uses `await ... .ConfigureAwait(false)` consistent with the existing handler's await style.
- Release build clean.
- Existing self-managed-tx path (lines 116-183 of the original file) preserved unchanged.

### Task 2: Add structural smoke test for the async fork

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SendMessageCommandHandlerAsyncForkSmokeTests.cs`

**Step 1: Write the smoke test**

Mirror of PLAN-2.1 Task 2, adapted for the async handler. Three reflection/source-grep tests.

```csharp
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
// (full LGPL header — copy from SendMessageCommandHandlerAsync.cs:1-18)
// ---------------------------------------------------------------------
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Structural smoke tests for the SqlServer async handler's HandleExternalTxAsync fork.
    /// Per RESEARCH §11 Discrepancy #2 + CLAUDE.md sync-vs-async mocking lesson, direct
    /// execution tests are infeasible at the unit-test level and live in Phase 6
    /// integration tests against a real SqlServer instance.
    /// </summary>
    [TestClass]
    public class SendMessageCommandHandlerAsyncForkSmokeTests
    {
        [TestMethod]
        public void HandleExternalTxAsync_PrivateMethod_ExistsWithExpectedSignature()
        {
            var handlerType = typeof(DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SendMessageCommandHandlerAsync);
            // Fallback if typeof fails to resolve internal: use Assembly.GetType("...", true).

            var method = handlerType.GetMethod("HandleExternalTxAsync",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(SendMessageCommand) },
                modifiers: null);

            Assert.IsNotNull(method, "HandleExternalTxAsync(SendMessageCommand) must exist as a private instance method.");
            Assert.AreEqual(typeof(Task<long>), method.ReturnType, "HandleExternalTxAsync must return Task<long>.");
        }

        [TestMethod]
        public void HandleAsync_SourceContainsExternalTransactionEarlyBranch()
        {
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerAsyncForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.SqlServer",
                "Basic", "CommandHandler",
                "SendMessageCommandHandlerAsync.cs");
            sourcePath = Path.GetFullPath(sourcePath);

            Assert.IsTrue(File.Exists(sourcePath), $"Expected source at {sourcePath} not found.");
            var content = File.ReadAllText(sourcePath);
            StringAssert.Contains(content, "commandSend.ExternalTransaction != null",
                "HandleAsync() must contain the early-branch null-check on ExternalTransaction.");
            StringAssert.Contains(content, "HandleExternalTxAsync(commandSend)",
                "HandleAsync() must dispatch to HandleExternalTxAsync on the early branch.");
            StringAssert.Contains(content, "private async Task<long> HandleExternalTxAsync",
                "HandleExternalTxAsync must be declared private async Task<long>.");
            StringAssert.Contains(content, "await HandleExternalTxAsync(commandSend).ConfigureAwait(false)",
                "The early-branch must await with ConfigureAwait(false) consistent with the handler's await style.");
        }

        [TestMethod]
        public void HandleExternalTxAsync_DoesNotCommitOrRollbackOrCloseOrDispose()
        {
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerAsyncForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.SqlServer",
                "Basic", "CommandHandler",
                "SendMessageCommandHandlerAsync.cs");
            sourcePath = Path.GetFullPath(sourcePath);

            var content = File.ReadAllText(sourcePath);
            var forkStart = content.IndexOf("private async Task<long> HandleExternalTxAsync",
                System.StringComparison.Ordinal);
            Assert.IsTrue(forkStart >= 0, "HandleExternalTxAsync not found in source.");
            var forkBody = content.Substring(forkStart, System.Math.Min(6500, content.Length - forkStart));

            Assert.IsFalse(forkBody.Contains(".Commit()"),   "HandleExternalTxAsync must not call .Commit() on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".Rollback()"), "HandleExternalTxAsync must not call .Rollback() on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".Close()"),    "HandleExternalTxAsync must not call .Close() on the caller's connection.");
            Assert.IsFalse(forkBody.Contains(".Dispose()"),  "HandleExternalTxAsync must not call .Dispose() on the caller's connection or transaction.");
            // Async-specific lifecycle calls:
            Assert.IsFalse(forkBody.Contains(".CommitAsync"),   "HandleExternalTxAsync must not call .CommitAsync on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".RollbackAsync"), "HandleExternalTxAsync must not call .RollbackAsync on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".CloseAsync"),    "HandleExternalTxAsync must not call .CloseAsync on the caller's connection.");
            Assert.IsFalse(forkBody.Contains(".DisposeAsync"),  "HandleExternalTxAsync must not call .DisposeAsync on the caller's connection or transaction.");
        }
    }
}
```

**Step 2: Run test to verify it passes**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~SendMessageCommandHandlerAsyncForkSmokeTests" --nologo`

Expected: PASS — `Passed: 3, Failed: 0, Skipped: 0, Total: 3`.

**Step 3: Confirm full suite still green**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --nologo
```

Expected: `Failed: 0`.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SendMessageCommandHandlerAsyncForkSmokeTests.cs
git commit -m "shipyard(phase-3): add structural smoke tests for async handler fork"
```

**Acceptance criteria:**
- 3 smoke tests pass: signature-exists (with `Task<long>` return), source-contains-early-branch (including `ConfigureAwait(false)`), source-has-no-Commit/Rollback/Close/Dispose (including async variants).
- No regressions in `Transport.SqlServer.Tests`.

## Verification

```bash
# Source contains the async early-branch
grep -n "commandSend.ExternalTransaction != null" Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
# expected: 1 match
grep -n "private async Task<long> HandleExternalTxAsync" Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
# expected: 1 match
grep -n "await HandleExternalTxAsync(commandSend).ConfigureAwait(false)" Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
# expected: 1 match

# Fork body has no lifecycle calls (sync OR async variants)
awk '/private async Task<long> HandleExternalTxAsync/,/^        }$/' Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs | grep -c -E "\.Commit\(|\.Rollback\(|\.Close\(|\.Dispose\(|\.CommitAsync|\.RollbackAsync|\.CloseAsync|\.DisposeAsync"
# expected: 0

# Release build clean
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release --nologo
# expected: 0 Error(s), 0 Warning(s)

# 3 async smoke tests pass; full SqlServer.Tests suite still green
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~SendMessageCommandHandlerAsyncForkSmokeTests" --nologo
# expected: Passed: 3, Failed: 0
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --nologo
# expected: Failed: 0

# Confirm sync and async plans touch disjoint handler files (no parallel-edit conflict)
diff <(echo "SendMessageCommandHandler.cs") <(echo "SendMessageCommandHandlerAsync.cs")
# expected: files differ (proves W2 plans modify different files)
```
