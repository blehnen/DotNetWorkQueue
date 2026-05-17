---
phase: postgresql-implementation
plan: 2.2
wave: 2
dependencies: [1.1]
must_haves:
  - HandleExternalTxAsync private method in PostgreSQL SendMessageCommandHandlerAsync.cs implementing the async caller-tx fork
  - Async fork materializes _getTime.GetCurrentUtcDate() and passes it as the eighth argument to CreateMetaDataRecordAsync (PG-specific — RESEARCH §1, §11)
  - Async fork uses NpgsqlDbType.Bytea (not SqlDbType.VarBinary) for body/headers parameters
  - Early-branch dispatch at top of HandleAsync() that awaits HandleExternalTxAsync(commandSend) when ExternalTransaction != null
  - Lifecycle-invariant source comment uses the exact CONTEXT-4 Rule B wording
  - 3 structural smoke tests confirm the async fork compiles and is structurally correct
files_touched:
  - Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SendMessageCommandHandlerAsyncForkSmokeTests.cs
tdd: false
risk: medium
---

# Plan 2.2: Async Handler Fork (Wave 2, parallel with PLAN-2.1)

## Context

Mirror of PLAN-2.1 for the async handler. Adds a `HandleExternalTxAsync` private method to `SendMessageCommandHandlerAsync.cs` plus the early-branch dispatch at the top of `HandleAsync`. The fork is the async equivalent of PLAN-2.1's sync fork:

1. Casts `command.ExternalTransaction` to `NpgsqlTransaction` (the producer subclass's `GuardNpgsqlTransaction` ensured this is safe; per RESEARCH §1 the existing async handler is also `NpgsqlConnection`/`NpgsqlTransaction`-typed end-to-end).
2. Reuses `tx.Connection` as the `NpgsqlConnection`.
3. Sets `cmd.Transaction` on each command.
4. Uses `ExecuteScalarAsync()` / `ExecuteNonQueryAsync()` (consistent with the existing async handler's `await command.ExecuteScalarAsync().ConfigureAwait(false)` pattern at line 145 of `SendMessageCommandHandlerAsync.cs`).
5. Calls the existing `CreateMetaDataRecordAsync` / `CreateStatusRecordAsync` private helpers (lines 196-231 of `SendMessageCommandHandlerAsync.cs`) verbatim — they already accept `NpgsqlConnection` + `NpgsqlTransaction` + (for `CreateMetaDataRecordAsync`) `DateTime currentTime`.
6. Reuses the sync `_jobExistsHandler.Handle(...)` and `_sendJobStatus.Handle(...)` calls (RESEARCH §3 confirmed neither has an async overload; the existing async handler invokes them synchronously at lines 122 + 167).
7. Never calls `tx.Commit()`, `tx.Rollback()`, `tx.Dispose()`, `tx.CommitAsync(...)`, `tx.RollbackAsync(...)`, `npgsqlConn.Close()`, `npgsqlConn.CloseAsync(...)`, `npgsqlConn.Dispose()`, or `npgsqlConn.DisposeAsync(...)`.

Three PG-specific deviations from Phase 3 PLAN-2.2 (mirror of PLAN-2.1's deviations — RESEARCH §11):

1. **`_getTime.GetCurrentUtcDate()` materialization (RESEARCH §1, §11, Uncertainty Flag #4).** PG's `CreateMetaDataRecordAsync` takes an additional `DateTime currentTime` parameter (`SendMessageCommandHandlerAsync.cs:221-222`) that SqlServer's does NOT. The async fork MUST call `_getTime.GetCurrentUtcDate()` (a **sync** method on `IGetTime` — no await needed) and pass the result as the eighth argument. Failure is a compile error.

2. **`NpgsqlDbType.Bytea` (not `SqlDbType.VarBinary`).** Same as PLAN-2.1.

3. **`DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>` + `SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>` type parameters.** Same as PLAN-2.1.

Insertion point per **RESEARCH §1**: immediately after the lazy-init block at line 106 of `SendMessageCommandHandlerAsync.cs`, before `var jobName = ...` at line 108.

Per **CONTEXT-4 Decision 3** the fork lives inside the existing async handler file as a private async method; no sibling class. Per **RESEARCH §5 + CLAUDE.md sync-vs-async mocking lesson** direct execution tests are infeasible (sealed `NpgsqlConnection` types). Coverage shape mirrors PLAN-2.1: one structural smoke-test class with three reflection/source-grep tests confirming the fork exists with the correct async signature, the early-branch is wired into `HandleAsync` with `.ConfigureAwait(false)`, and no lifecycle-ownership calls (sync OR async variants) leaked in.

This plan touches `SendMessageCommandHandlerAsync.cs`; PLAN-2.1 touches `SendMessageCommandHandler.cs`. **The two plans share no files.** Both share `SendMessage.cs` only as a READ dependency. **Both plans can execute in parallel after PLAN-1.1 completes.**

Per **CONTEXT-4 Rule B** the lifecycle-invariant source comment MUST use the exact wording `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.` — converges with PLAN-2.1 on the source-side rephrase (Phase 3 SUMMARY-2.2 lesson) so neither fork needs test-side comment preprocessing.

## Dependencies

- PLAN-1.1 (Wave 1) — same justification as PLAN-2.1: the producer subclass + DI wiring ensures `RelationalSendMessageCommand` reaches this handler via the registered async dispatch path.

This plan reads (does not modify):
- `RelationalSendMessageCommand` (Phase 2 PLAN-2.2)
- `SendMessageCommand.ExternalTransaction` (Phase 2 PLAN-1.1)
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessage.cs` (static builders)

## Tasks

### Task 1: Add HandleExternalTxAsync private method + early-branch dispatch to PostgreSQL SendMessageCommandHandlerAsync

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` — insert early-branch at line 107 (after lazy-init block ends at line 106, before `var jobName = ...` at line 108); append private `HandleExternalTxAsync` method before the existing `CreateStatusRecordAsync` at line 196.

**Step 1: Insert the early-branch immediately after the lazy-init block**

After the closing `}` of the `if (!_messageExpirationEnabled.HasValue) { ... }` block (currently line 106) and before `var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);` (currently line 108), insert:

```csharp
            if (commandSend.ExternalTransaction != null)
                return await HandleExternalTxAsync(commandSend).ConfigureAwait(false);
```

This is a 2-line addition. The `.ConfigureAwait(false)` matches the existing handler's await style (line 145 + 157 + 162 of the current file).

**Step 2: Append the HandleExternalTxAsync method**

Immediately before the existing `private async Task CreateStatusRecordAsync(...)` method (currently line 196), insert:

```csharp
        /// <summary>
        /// Async caller-supplied-transaction fork of <see cref="HandleAsync(SendMessageCommand)"/>.
        /// Reuses the caller's <see cref="NpgsqlTransaction"/> and its
        /// <see cref="NpgsqlConnection"/> for all queue INSERTs; never commits, rolls back,
        /// closes, or disposes the caller's resources. Invoked from <see cref="HandleAsync"/>
        /// when <see cref="SendMessageCommand.ExternalTransaction"/> is non-null. The producer
        /// surface (<c>PostgreSqlRelationalProducerQueue&lt;T&gt;</c>) validates the
        /// transaction at the API boundary, so this method performs no validation of its own.
        /// </summary>
        /// <param name="commandSend">The send-message command carrying a non-null
        /// <see cref="SendMessageCommand.ExternalTransaction"/>.</param>
        /// <returns>The newly-inserted message ID.</returns>
        /// <exception cref="DotNetWorkQueueException">Thrown when the INSERT returns a zero
        /// ID or when the job-uniqueness query rejects the command.</exception>
        private async Task<long> HandleExternalTxAsync(SendMessageCommand commandSend)
        {
            // Producer subclass already validated and confirmed NpgsqlTransaction; raw cast OK.
            var npgsqlTx = (NpgsqlTransaction)commandSend.ExternalTransaction;
            var npgsqlConn = (NpgsqlConnection)npgsqlTx.Connection;

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
            // SendMessageCommandHandlerAsync.cs line ~122 in the pre-Phase-4 baseline).
            if (!(string.IsNullOrWhiteSpace(jobName) ||
                  _jobExistsHandler.Handle(new DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>(
                      jobName, scheduledTime, npgsqlConn, npgsqlTx)) == QueueStatuses.NotQueued))
            {
                throw new DotNetWorkQueueException(
                    "Failed to insert record - the job has already been queued or processed");
            }

            long id;
            using (var command = npgsqlConn.CreateCommand())
            {
                command.Transaction = npgsqlTx;
                command.CommandText = _commandCache.GetCommand(CommandStringTypes.InsertMessageBody);
                var serialization = _serializer.Serializer.MessageToBytes(
                    new MessageBody { Body = commandSend.MessageToSend.Body },
                    commandSend.MessageToSend.Headers);

                command.Parameters.Add("@body", NpgsqlDbType.Bytea, -1);
                command.Parameters["@body"].Value = serialization.Output;

                commandSend.MessageToSend.SetHeader(
                    _headers.StandardHeaders.MessageInterceptorGraph, serialization.Graph);

                command.Parameters.Add("@headers", NpgsqlDbType.Bytea, -1);
                command.Parameters["@headers"].Value =
                    _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers);

                id = Convert.ToInt64(await command.ExecuteScalarAsync().ConfigureAwait(false));
            }

            if (id <= 0)
            {
                throw new DotNetWorkQueueException(
                    "Failed to insert record - the ID of the new record returned by the server was 0");
            }

            var expiration = TimeSpan.Zero;
            if (_messageExpirationEnabled.Value)
            {
                expiration = MessageExpiration.GetExpiration(commandSend, data => data.GetExpiration());
            }

            // PG-specific: CreateMetaDataRecordAsync takes a DateTime currentTime as the
            // eighth argument. IGetTime.GetCurrentUtcDate() is synchronous — invoke directly,
            // no await needed.
            await CreateMetaDataRecordAsync(commandSend.MessageData.GetDelay(), expiration,
                npgsqlConn, id, commandSend.MessageToSend, commandSend.MessageData, npgsqlTx,
                _getTime.GetCurrentUtcDate()).ConfigureAwait(false);

            if (_options.Value.EnableStatusTable)
            {
                await CreateStatusRecordAsync(npgsqlConn, id, commandSend.MessageToSend,
                    commandSend.MessageData, npgsqlTx).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(jobName))
            {
                _sendJobStatus.Handle(new SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>(
                    jobName, eventTime, scheduledTime, npgsqlConn, npgsqlTx));
            }

            // Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.
            return id;
        }
```

The existing `CreateStatusRecordAsync` and `CreateMetaDataRecordAsync` helpers (lines 196-231 of the current file) are reused verbatim — they already accept `NpgsqlConnection`, `NpgsqlTransaction`, and (for `CreateMetaDataRecordAsync`) the `DateTime currentTime` parameter.

**Architect note for builder (CONTEXT-4 Rule B):** The trailing lifecycle-invariant comment MUST use the EXACT wording `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.` — same wording as PLAN-2.1's sync fork. This keeps both forks aligned on the source-side rephrase so neither smoke test needs comment-stripping preprocessing. Phase 3 SUMMARY-2.2 documents the rephrase decision; Phase 4 converges both handlers on it for consistency.

**Architect note for builder (PG-specific compile gates):**
- `_getTime.GetCurrentUtcDate()` returns `DateTime` synchronously — do NOT add `await` in front of it. The method is on the `IGetTime` interface (synchronous), not an async method.
- `CreateMetaDataRecordAsync` is `private async Task` (not `Task<>`), so the call site uses `await ...CreateMetaDataRecordAsync(...).ConfigureAwait(false)` — no return value to capture.
- The early-branch uses `return await HandleExternalTxAsync(commandSend).ConfigureAwait(false)` not `return HandleExternalTxAsync(commandSend)` — `HandleAsync` is `async Task<long>` so the await is needed to unwrap the result.

**Step 3: Verify the build is clean**

Run: `dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release --nologo`

Expected: `Build succeeded. 0 Error(s), 0 Warning(s)` (modulo pre-existing NU1902 advisory warnings).

Common compile failures and their root causes:
- CS7036 / CS1503 on `CreateMetaDataRecordAsync` — missing `_getTime.GetCurrentUtcDate()` eighth argument (PG-specific, RESEARCH §11).
- CS0246 / CS1503 on `Bytea` — wrong DbType enum (PLAN-2.1 mistake form).
- CS4032 / CS1996 — missing `async` keyword on the new method, OR using `await` on `_getTime.GetCurrentUtcDate()` (which is sync).

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
git commit -m "shipyard(phase-4): add HandleExternalTxAsync fork to PostgreSQL async handler"
```

**Acceptance criteria:**
- `SendMessageCommandHandlerAsync.HandleAsync()` contains an early-branch on `commandSend.ExternalTransaction != null` immediately after the lazy-init block (line ~107–108 of the modified file), with `await ... .ConfigureAwait(false)`.
- `HandleExternalTxAsync(SendMessageCommand)` is a `private async Task<long>` method with the body above.
- Fork uses `(NpgsqlTransaction)commandSend.ExternalTransaction` cast and `(NpgsqlConnection)npgsqlTx.Connection` cast.
- Fork uses `NpgsqlDbType.Bytea` for both `@body` and `@headers` parameters.
- Fork calls `await CreateMetaDataRecordAsync(..., _getTime.GetCurrentUtcDate()).ConfigureAwait(false)` — `_getTime.GetCurrentUtcDate()` appears exactly once in the fork body (PG-specific requirement).
- Fork uses `DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>` and `SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>`.
- Fork uses `command.ExecuteScalarAsync()` (NOT sync `ExecuteScalar`).
- The trailing lifecycle comment uses the EXACT CONTEXT-4 Rule B wording: `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.`
- No `npgsqlTx.Commit`, `npgsqlTx.Rollback`, `npgsqlTx.Dispose`, `npgsqlConn.Close`, `npgsqlConn.Dispose` invocations inside `HandleExternalTxAsync`, AND no `.CommitAsync` / `.RollbackAsync` / `.CloseAsync` / `.DisposeAsync` variants either (grep gate enforces 0 matches for all 8 patterns in the fork body).
- Release build clean; existing self-managed-tx path (lines 117-184 of the original file) preserved unchanged.

### Task 2: Add 3 structural smoke tests for the async fork

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SendMessageCommandHandlerAsyncForkSmokeTests.cs`

**Step 1: Write the smoke test class**

Mirror of PLAN-2.1 Task 2, adapted for the async handler. Three reflection/source-grep tests:

1. **`HandleExternalTxAsync_PrivateMethod_ExistsWithExpectedSignature`** — reflection assertion that the method exists with return type `Task<long>` (not `long`).
2. **`HandleAsync_SourceContainsExternalTransactionEarlyBranch`** — source grep confirms `commandSend.ExternalTransaction != null`, the dispatch site uses `await HandleExternalTxAsync(commandSend).ConfigureAwait(false)`, and the method is declared `private async Task<long> HandleExternalTxAsync`.
3. **`HandleExternalTxAsync_DoesNotCommitOrRollbackOrCloseOrDispose`** — source-text grep on the fork body confirms no `.Commit()` / `.Rollback()` / `.Close()` / `.Dispose()` substrings AND no `.CommitAsync` / `.RollbackAsync` / `.CloseAsync` / `.DisposeAsync` substrings.

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

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Structural smoke tests for the PostgreSQL async handler's HandleExternalTxAsync fork.
    /// Per RESEARCH §5 + CLAUDE.md sync-vs-async mocking lesson, direct execution tests
    /// are infeasible at the unit-test level and live in Phase 6 integration tests
    /// against a real PostgreSQL instance.
    /// </summary>
    [TestClass]
    public class SendMessageCommandHandlerAsyncForkSmokeTests
    {
        [TestMethod]
        public void HandleExternalTxAsync_PrivateMethod_ExistsWithExpectedSignature()
        {
            // Fallback if typeof fails to resolve internal: use Assembly.GetType("...", true).
            var handlerType = typeof(DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler.SendMessageCommandHandlerAsync);

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
                "DotNetWorkQueue.Transport.PostgreSQL",
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
            // CONTEXT-4 Rule B mandates the lifecycle comment uses word forms
            // ("no Commit, Rollback, Close, or Dispose") so plain substring search
            // is safe — no preprocessing needed for either sync or async variants.
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerAsyncForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.PostgreSQL",
                "Basic", "CommandHandler",
                "SendMessageCommandHandlerAsync.cs");
            sourcePath = Path.GetFullPath(sourcePath);

            var content = File.ReadAllText(sourcePath);
            var forkStart = content.IndexOf("private async Task<long> HandleExternalTxAsync",
                System.StringComparison.Ordinal);
            Assert.IsTrue(forkStart >= 0, "HandleExternalTxAsync not found in source.");
            // Conservative end-bound: 6500 chars forward (fork is ~85 lines, plenty).
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

Run: `dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~SendMessageCommandHandlerAsyncForkSmokeTests" --nologo`

Expected: PASS — `Passed: 3, Failed: 0, Skipped: 0, Total: 3`.

If `HandleExternalTxAsync_DoesNotCommitOrRollbackOrCloseOrDispose` fails, the most likely cause is a Phase 3-style lifecycle comment containing method-call substrings. Rephrase the comment per CONTEXT-4 Rule B; do NOT modify the test.

**Step 3: Confirm full suite still green**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --nologo
```

Expected: `Failed: 0`.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SendMessageCommandHandlerAsyncForkSmokeTests.cs
git commit -m "shipyard(phase-4): add structural smoke tests for PG async handler fork"
```

**Acceptance criteria:**
- 3 smoke tests pass: signature-exists (with `Task<long>` return), source-contains-early-branch (including `await ... .ConfigureAwait(false)` and `private async Task<long>` declaration), source-has-no-Commit/Rollback/Close/Dispose (including async variants).
- No regressions in `Transport.PostgreSQL.Tests`.

## Verification

```bash
# Source contains the async early-branch
grep -c "commandSend.ExternalTransaction != null" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
# expected: 1
grep -c "private async Task<long> HandleExternalTxAsync" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
# expected: 1
grep -c "await HandleExternalTxAsync(commandSend).ConfigureAwait(false)" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
# expected: 1

# PG-specific: _getTime.GetCurrentUtcDate() appears in the fork body
awk '/private async Task<long> HandleExternalTxAsync/,/^        }$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs | grep -c "_getTime.GetCurrentUtcDate()"
# expected: 1

# PG-specific: NpgsqlDbType.Bytea used (NOT SqlDbType.VarBinary)
awk '/private async Task<long> HandleExternalTxAsync/,/^        }$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs | grep -c "NpgsqlDbType.Bytea"
# expected: 2 (one each for @body and @headers)
awk '/private async Task<long> HandleExternalTxAsync/,/^        }$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs | grep -c "SqlDbType"
# expected: 0

# Async fork uses ExecuteScalarAsync (not sync ExecuteScalar)
awk '/private async Task<long> HandleExternalTxAsync/,/^        }$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs | grep -c "ExecuteScalarAsync()"
# expected: 1

# CONTEXT-4 Rule B: lifecycle comment uses word forms
awk '/private async Task<long> HandleExternalTxAsync/,/^        }$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs | grep -c "Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here."
# expected: 1

# Fork body has no lifecycle calls (sync OR async variants — 8 patterns)
awk '/private async Task<long> HandleExternalTxAsync/,/^        }$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs | grep -cE "\.Commit\(\)|\.Rollback\(\)|\.Close\(\)|\.Dispose\(\)|\.CommitAsync|\.RollbackAsync|\.CloseAsync|\.DisposeAsync"
# expected: 0

# Release build clean
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release --nologo
# expected: 0 Error(s), 0 Warning(s) (modulo pre-existing NU1902)

# 3 async smoke tests pass; full PG.Tests suite still green
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~SendMessageCommandHandlerAsyncForkSmokeTests" --nologo
# expected: Passed: 3, Failed: 0
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --nologo
# expected: Failed: 0

# Confirm sync and async plans touch disjoint handler files (no parallel-edit conflict)
diff <(echo "SendMessageCommandHandler.cs") <(echo "SendMessageCommandHandlerAsync.cs")
# expected: files differ (proves W2 plans modify different files)
```
