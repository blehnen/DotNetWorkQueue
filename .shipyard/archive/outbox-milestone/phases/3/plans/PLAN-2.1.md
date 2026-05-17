---
phase: sqlserver-implementation
plan: 2.1
wave: 2
dependencies: [1.1]
must_haves:
  - HandleExternalTx private method in SendMessageCommandHandler.cs implementing the caller-tx fork
  - Early-branch dispatch at top of Handle() that returns HandleExternalTx(commandSend) when ExternalTransaction != null
  - Smoke test confirming the fork compiles and is structurally correct (reflection-reached method-exists check)
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SendMessageCommandHandlerForkSmokeTests.cs
tdd: false
risk: medium
---

# Plan 2.1: Sync Handler Fork (Wave 2, parallel with PLAN-2.2)

## Context

Wave 2 adds the SqlServer sync handler-side fork that runs when a `RelationalSendMessageCommand` arrives with a caller-supplied `ExternalTransaction`. The fork:

1. Casts `command.ExternalTransaction` to `SqlTransaction` (safe — the W1 producer subclass guards this via `GuardSqlTransaction`; per **RESEARCH §11 Discrepancy #1** the existing handler is already SqlServer-typed end-to-end and the fork must honour that typing contract to reuse `SendMessage.BuildMetaCommand` / `BuildStatusCommand`, which require `SqlCommand` not `IDbCommand` per RESEARCH §2).
2. Reuses `tx.Connection` as the connection (never calls `connection.Open()`, never wraps in `using`).
3. Sets `cmd.Connection = sqlConn` and `cmd.Transaction = sqlTx` on every command (3 INSERTs: body, metadata, status — status is conditional on `_options.Value.EnableStatusTable`; the job-status command is conditional on a non-empty `jobName`).
4. Reuses `SendMessage.BuildMetaCommand` and `SendMessage.BuildStatusCommand` static builders verbatim (RESEARCH §2 confirmed these mutate `command.CommandText` + `Parameters` only and do NOT touch `Connection`/`Transaction`).
5. Never calls `tx.Commit()`, `tx.Rollback()`, `tx.Dispose()`, `conn.Close()`, or `conn.Dispose()`. The caller owns the lifecycle.

Per **RESEARCH §11 Discrepancy #2** direct fork unit tests are infeasible at the SqlServer.Tests project level because `SqlConnection`/`SqlTransaction`/`SqlCommand` are sealed and NSubstitute cannot mock them. The bulk of Phase 3 unit-test coverage lives in W1 around the producer subclass. This plan adds the fork code itself plus exactly one **structural smoke test** confirming via reflection that `HandleExternalTx` exists with the expected signature and that `Handle()` has an early-branch reference to it. Actual fork execution against a real database is Phase 6's territory.

Per **CONTEXT-3 Decision 2** the fork lives inside the existing `SendMessageCommandHandler.cs` file as a private method; no sibling class. Per **RESEARCH §1** the insertion point for the early-return branch is immediately after the lazy-init block (`if (!_messageExpirationEnabled.HasValue) { ... }`) at line 106 of `SendMessageCommandHandler.cs`. This guarantees `_options.Value` is materialized once before the fork branches (the fork relies on `_options.Value.EnableStatusTable` and `_messageExpirationEnabled.Value` exactly like the self-managed path).

## Dependencies

- PLAN-1.1 (Wave 1) — establishes `SqlServerRelationalProducerQueue<T>` (constructs `RelationalSendMessageCommand`) and the DI wiring that routes producer dispatch through the registered `SendMessageCommandHandler`. Without W1 the fork would be unreachable in any meaningful test.

This plan reads (does not modify) the following Phase 2 types:
- `RelationalSendMessageCommand` (constructs the inbound command; the handler treats it as the base `SendMessageCommand` and reads the inherited `ExternalTransaction` property)
- `SendMessageCommand.ExternalTransaction` (added by Phase 2 PLAN-1.1)

This plan reads (does not modify) `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessage.cs` — the static builders are reused unchanged.

## Tasks

### Task 1: Add HandleExternalTx private method + early-branch dispatch to SendMessageCommandHandler

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs` (insert early-branch at line 106-107; append private `HandleExternalTx` method before the existing `CreateStatusRecord` method at line 191).

**Step 1: Insert the early-branch at line 107**

After the closing `}` of the lazy-init block (currently line 106) and before `var jobName = ...` (currently line 108), insert:

```csharp
            if (commandSend.ExternalTransaction != null)
                return HandleExternalTx(commandSend);
```

This is a 2-line addition. The body below is unchanged.

**Step 2: Append the HandleExternalTx method**

Immediately before the existing `private void CreateStatusRecord(...)` method (currently line 191), insert:

```csharp
        /// <summary>
        /// Caller-supplied-transaction fork of <see cref="Handle(SendMessageCommand)"/>. Reuses
        /// the caller's <see cref="SqlTransaction"/> and its <see cref="SqlConnection"/> for all
        /// queue INSERTs; never commits, rolls back, closes, or disposes the caller's resources.
        /// Invoked from <see cref="Handle"/> when <see cref="SendMessageCommand.ExternalTransaction"/>
        /// is non-null. The producer surface (<c>SqlServerRelationalProducerQueue&lt;T&gt;</c>)
        /// guarantees the transaction is a <see cref="SqlTransaction"/> and its connection's
        /// database matches the queue's configured database via the validator at the API boundary,
        /// so this method performs no validation of its own.
        /// </summary>
        /// <param name="commandSend">The send-message command carrying a non-null
        /// <see cref="SendMessageCommand.ExternalTransaction"/>.</param>
        /// <returns>The newly-inserted message ID.</returns>
        /// <exception cref="DotNetWorkQueueException">Thrown when the INSERT returns a zero ID
        /// or when the job-uniqueness query rejects the command.</exception>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        private long HandleExternalTx(SendMessageCommand commandSend)
        {
            // Cast guard: the producer subclass enforces this via GuardSqlTransaction, but
            // re-cast here without a check — an invalid type would have failed at the producer
            // surface with a clean diagnostic message before reaching this method.
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

                id = Convert.ToInt64(command.ExecuteScalar());
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

            CreateMetaDataRecord(commandSend.MessageData.GetDelay(), expiration, sqlConn, id,
                commandSend.MessageToSend, commandSend.MessageData, sqlTx);

            if (_options.Value.EnableStatusTable)
            {
                CreateStatusRecord(sqlConn, id, commandSend.MessageToSend, commandSend.MessageData, sqlTx);
            }

            if (!string.IsNullOrWhiteSpace(jobName))
            {
                _sendJobStatus.Handle(new SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>(
                    jobName, eventTime, scheduledTime, sqlConn, sqlTx));
            }

            // Deliberately NO trans.Commit() / Rollback() / Dispose() / sqlConn.Close().
            // The caller owns the transaction lifecycle.
            return id;
        }
```

The existing `CreateStatusRecord` and `CreateMetaDataRecord` helpers (lines 191-223 of the current file) are reused verbatim — they already accept `SqlConnection` and `SqlTransaction` parameters.

**Step 3: Verify the build is clean**

Run: `dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release --nologo`

Expected: `Build succeeded. 0 Error(s), 0 Warning(s)`.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs
git commit -m "shipyard(phase-3): add HandleExternalTx fork to SqlServer sync handler"
```

**Acceptance criteria:**
- `SendMessageCommandHandler.Handle()` contains an early-branch on `commandSend.ExternalTransaction != null` immediately after the lazy-init block.
- `HandleExternalTx(SendMessageCommand)` is a `private long` method with the body above.
- No `tx.Commit`, `tx.Rollback`, `tx.Dispose`, `sqlConn.Close`, or `sqlConn.Dispose` calls inside `HandleExternalTx` (grep gate).
- Release build clean (XML doc on the new private method via `<summary>` block — private XML doc is non-mandatory but included per CONTEXT-3 hard rule on XML docs).
- Existing self-managed-tx path (lines 117-180 of the original file) is preserved unchanged.

### Task 2: Add structural smoke test for the sync fork

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SendMessageCommandHandlerForkSmokeTests.cs`

**Step 1: Write the smoke test**

Per **RESEARCH §11 Discrepancy #2** and **CLAUDE.md sync-vs-async mocking lesson** we cannot invoke `Handle()` with a mocked `SqlConnection`/`SqlTransaction` (sealed types) without a live database. The smoke test instead confirms via reflection that:

1. `HandleExternalTx(SendMessageCommand)` exists as a private instance method on `SendMessageCommandHandler` with return type `long`.
2. `Handle(SendMessageCommand)` source contains the early-branch on `commandSend.ExternalTransaction != null`.

The first check is a real reflection assertion against the loaded assembly. The second is a grep-style assertion against the source file (we read it as a text file from disk via `AppDomain.CurrentDomain.BaseDirectory` is not viable, but the test project can use the well-known path `..\..\..\..\DotNetWorkQueue.Transport.SqlServer\Basic\CommandHandler\SendMessageCommandHandler.cs` resolved from the test bin output directory). Path resolution failure is itself a useful signal — the source file must exist where the architecture says it does.

```csharp
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
// (full LGPL header — copy from SendMessageCommandHandler.cs:1-18)
// ---------------------------------------------------------------------
using System.IO;
using System.Reflection;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Structural smoke tests for the SqlServer sync handler's HandleExternalTx fork.
    /// Per RESEARCH §11 Discrepancy #2 + CLAUDE.md sync-vs-async mocking lesson, direct
    /// execution tests of the fork are infeasible at the unit-test level (sealed
    /// SqlConnection/SqlTransaction/SqlCommand types) and live in Phase 6 integration
    /// tests against a real SqlServer instance. This test verifies only the structural
    /// shape of the fork: it exists, has the expected signature, and is invoked by Handle().
    /// </summary>
    [TestClass]
    public class SendMessageCommandHandlerForkSmokeTests
    {
        [TestMethod]
        public void HandleExternalTx_PrivateMethod_ExistsWithExpectedSignature()
        {
            var handlerType = typeof(DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SendMessageCommandHandler);
            // Type is internal — reach via Assembly + GetType (typeof above also works
            // because the test project has InternalsVisibleTo or sees internals via
            // ProjectReference; if the typeof fails to compile, fall back to:
            //   var handlerType = typeof(SqlServerMessageQueueInit).Assembly
            //       .GetType("DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SendMessageCommandHandler", throwOnError: true);
            // The fallback works regardless of InternalsVisibleTo.)

            var method = handlerType.GetMethod("HandleExternalTx",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(SendMessageCommand) },
                modifiers: null);

            Assert.IsNotNull(method, "HandleExternalTx(SendMessageCommand) must exist as a private instance method.");
            Assert.AreEqual(typeof(long), method.ReturnType, "HandleExternalTx must return long.");
        }

        [TestMethod]
        public void Handle_SourceContainsExternalTransactionEarlyBranch()
        {
            // Read SendMessageCommandHandler.cs from the source tree relative to the test
            // bin output. dotnet test runs from the project's bin directory; the source
            // file is 4 levels up + into the main project's Basic/CommandHandler folder.
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.SqlServer",
                "Basic", "CommandHandler",
                "SendMessageCommandHandler.cs");
            sourcePath = Path.GetFullPath(sourcePath);

            Assert.IsTrue(File.Exists(sourcePath), $"Expected source at {sourcePath} not found.");
            var content = File.ReadAllText(sourcePath);
            StringAssert.Contains(content, "commandSend.ExternalTransaction != null",
                "Handle() must contain the early-branch null-check on ExternalTransaction.");
            StringAssert.Contains(content, "return HandleExternalTx(commandSend);",
                "Handle() must dispatch to HandleExternalTx on the early branch.");
            StringAssert.Contains(content, "private long HandleExternalTx",
                "HandleExternalTx must be declared private long.");
        }

        [TestMethod]
        public void HandleExternalTx_DoesNotCommitOrRollbackOrCloseOrDispose()
        {
            // Source-level grep guard for the lifecycle-ownership contract from PROJECT.md
            // §Success Criteria #7. The fork must NEVER call Commit/Rollback/Close/Dispose
            // on the caller's transaction or connection.
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.SqlServer",
                "Basic", "CommandHandler",
                "SendMessageCommandHandler.cs");
            sourcePath = Path.GetFullPath(sourcePath);

            var content = File.ReadAllText(sourcePath);
            // Extract the body of HandleExternalTx by anchoring on its signature and the
            // closing-brace of the method (the next "        }" at column 8 after its body).
            var forkStart = content.IndexOf("private long HandleExternalTx", System.StringComparison.Ordinal);
            Assert.IsTrue(forkStart >= 0, "HandleExternalTx not found in source.");
            // Conservative end-bound: search 6000 chars forward (the fork is ~80 lines, plenty).
            var forkBody = content.Substring(forkStart, System.Math.Min(6000, content.Length - forkStart));

            Assert.IsFalse(forkBody.Contains(".Commit()"),    "HandleExternalTx must not call .Commit() on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".Rollback()"),  "HandleExternalTx must not call .Rollback() on the caller's transaction.");
            // Close and Dispose are looked for as method invocations on conn/tx — broad enough
            // to catch sqlConn.Close(), sqlTx.Dispose(), etc. False-positives unlikely because
            // the fork body has no other Close/Dispose surface.
            Assert.IsFalse(forkBody.Contains(".Close()"),     "HandleExternalTx must not call .Close() on the caller's connection.");
            Assert.IsFalse(forkBody.Contains(".Dispose()"),   "HandleExternalTx must not call .Dispose() on the caller's connection or transaction.");
        }
    }
}
```

**Step 2: Run test to verify it passes**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~SendMessageCommandHandlerForkSmokeTests" --nologo`

Expected: PASS — `Passed: 3, Failed: 0, Skipped: 0, Total: 3`.

**Step 3: Confirm full suite still green**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --nologo
```

Expected: `Failed: 0`.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SendMessageCommandHandlerForkSmokeTests.cs
git commit -m "shipyard(phase-3): add structural smoke tests for sync handler fork"
```

**Acceptance criteria:**
- 3 smoke tests pass: signature-exists, source-contains-early-branch, source-has-no-Commit/Rollback/Close/Dispose.
- The lifecycle-ownership grep test (Test 3) explicitly enforces PROJECT.md §Success Criteria #7 at the source-text level (Phase 6 integration tests confirm it at the runtime level).
- No regressions in `Transport.SqlServer.Tests`.

## Verification

```bash
# Source contains the early-branch
grep -n "commandSend.ExternalTransaction != null" Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs
# expected: 1 match (the new early-branch)
grep -n "private long HandleExternalTx" Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs
# expected: 1 match
grep -c "Commit\|Rollback\|\.Close()\|\.Dispose()" <(awk '/private long HandleExternalTx/,/^        }$/' Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs)
# expected: 0 — the fork has no lifecycle calls

# Release build clean
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release --nologo
# expected: 0 Error(s), 0 Warning(s)

# 3 smoke tests pass; full SqlServer.Tests suite still green
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~SendMessageCommandHandlerForkSmokeTests" --nologo
# expected: Passed: 3, Failed: 0
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --nologo
# expected: Failed: 0
```
