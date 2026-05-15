---
phase: postgresql-implementation
plan: 2.1
wave: 2
dependencies: [1.1]
must_haves:
  - HandleExternalTx private method in PostgreSQL SendMessageCommandHandler.cs implementing the caller-tx fork
  - Fork materializes _getTime.GetCurrentUtcDate() and passes it as the eighth argument to CreateMetaDataRecord (PG-specific — RESEARCH §1, §11)
  - Fork uses NpgsqlDbType.Bytea (not SqlDbType.VarBinary) for body/headers parameters
  - Early-branch dispatch at top of Handle() that returns HandleExternalTx(commandSend) when ExternalTransaction != null
  - Lifecycle-invariant source comment uses the exact CONTEXT-4 Rule B wording
  - 3 structural smoke tests confirm the fork compiles and is structurally correct
files_touched:
  - Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SendMessageCommandHandlerForkSmokeTests.cs
tdd: false
risk: medium
---

# Plan 2.1: Sync Handler Fork (Wave 2, parallel with PLAN-2.2)

## Context

Wave 2 adds the PostgreSQL sync handler-side fork that runs when a `RelationalSendMessageCommand` arrives with a caller-supplied `ExternalTransaction`. The fork mirrors Phase 3 PLAN-2.1 (SqlServer) with three PG-specific deviations (RESEARCH §11):

1. **`_getTime.GetCurrentUtcDate()` materialization (RESEARCH §1 + §11).** PG's `CreateMetaDataRecord` takes an additional `DateTime currentTime` parameter (`SendMessageCommandHandler.cs:212-213`) that SqlServer's does NOT. The fork MUST call `_getTime.GetCurrentUtcDate()` and pass the result as the eighth argument — failure is a compile error (`CreateMetaDataRecord` has 8 required params: `TimeSpan?, TimeSpan, NpgsqlConnection, long, IMessage, IAdditionalMessageData, NpgsqlTransaction, DateTime`). `_getTime` is already injected (private field, set at constructor line 94); no constructor change is needed.

2. **`NpgsqlDbType.Bytea` (not `SqlDbType.VarBinary`).** Body/headers parameter types use `NpgsqlDbType.Bytea`. The existing `using NpgsqlTypes;` at line 32 of the handler file already imports the enum — no new using needed.

3. **`DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>` + `SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>` type parameters** (RESEARCH §1) — not `SqlConnection`/`SqlTransaction`.

The fork:

1. Casts `command.ExternalTransaction` to `NpgsqlTransaction` (safe — the W1 producer subclass guards this via `GuardNpgsqlTransaction`; per RESEARCH §1 the existing handler is already `NpgsqlConnection`/`NpgsqlTransaction`-typed end-to-end and the fork must honour that typing contract).
2. Reuses `tx.Connection` as the `NpgsqlConnection` (never calls `connection.Open()`, never wraps in `using`).
3. Sets `cmd.Connection = npgsqlConn` and `cmd.Transaction = npgsqlTx` on every command.
4. Reuses `SendMessage.BuildMetaCommand` / `BuildStatusCommand` static builders verbatim via the existing private `CreateStatusRecord` / `CreateMetaDataRecord` helpers (lines 189-223 of the current file), which already accept `NpgsqlConnection` + `NpgsqlTransaction` + `DateTime currentTime` parameters.
5. Never calls `tx.Commit()`, `tx.Rollback()`, `tx.Dispose()`, `npgsqlConn.Close()`, or `npgsqlConn.Dispose()`. The caller owns the lifecycle.

Per **CONTEXT-4 Decision 3** the fork lives inside the existing `SendMessageCommandHandler.cs` as a private method; no sibling class. Per **RESEARCH §1** the insertion point for the early-return branch is immediately after the lazy-init block (`if (!_messageExpirationEnabled.HasValue) { ... }`) at line 104 of `SendMessageCommandHandler.cs`, before line 106 (`var jobName = ...`).

Per **RESEARCH §5 + CLAUDE.md sync-vs-async mocking lesson** direct fork unit tests are infeasible at the PG.Tests project level because `NpgsqlConnection`/`NpgsqlTransaction`/`NpgsqlCommand` are sealed and NSubstitute cannot mock them. The bulk of Phase 4 unit-test coverage lives in W1 around the producer subclass. This plan adds the fork code itself plus exactly one **structural smoke test class** with three reflection/source-grep tests. Actual fork execution against a real database is Phase 6's territory.

Per **CONTEXT-4 Rule B** the lifecycle-invariant source comment MUST use the exact wording `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.` — the smoke test's source-text grep checks for the forbidden substrings `.Commit()`, `.Rollback()`, `.Close()`, `.Dispose()` and would false-positive on a SqlServer-style comment like "Deliberately NO trans.Commit() / Rollback() / Dispose()". The rephrase converges both handlers' fork comments on the source-side-correct wording (Phase 3 SUMMARY-2.2 lesson).

## Dependencies

- PLAN-1.1 (Wave 1) — establishes `PostgreSqlRelationalProducerQueue<T>` (constructs `RelationalSendMessageCommand`) and the DI wiring that routes producer dispatch through the registered `SendMessageCommandHandler`. Without W1 the fork would be unreachable in any meaningful test.

This plan reads (does not modify):
- `RelationalSendMessageCommand` (Phase 2 PLAN-2.2)
- `SendMessageCommand.ExternalTransaction` (Phase 2 PLAN-1.1)
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessage.cs` (static builders)

## Tasks

### Task 1: Add HandleExternalTx private method + early-branch dispatch to PostgreSQL SendMessageCommandHandler

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs` — insert early-branch at line 105 (after lazy-init block ends at line 104, before `var jobName = ...` at line 106); append private `HandleExternalTx` method before the existing `CreateStatusRecord` method at line 189.

**Step 1: Insert the early-branch immediately after the lazy-init block**

After the closing `}` of the `if (!_messageExpirationEnabled.HasValue) { ... }` block (currently line 104) and before `var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);` (currently line 106), insert:

```csharp
            if (commandSend.ExternalTransaction != null)
                return HandleExternalTx(commandSend);
```

This is a 2-line addition. The body below is unchanged.

**Step 2: Append the HandleExternalTx method**

Immediately before the existing `private void CreateStatusRecord(...)` method (currently line 189), insert:

```csharp
        /// <summary>
        /// Caller-supplied-transaction fork of <see cref="Handle(SendMessageCommand)"/>. Reuses
        /// the caller's <see cref="NpgsqlTransaction"/> and its <see cref="NpgsqlConnection"/>
        /// for all queue INSERTs; never commits, rolls back, closes, or disposes the caller's
        /// resources. Invoked from <see cref="Handle"/> when
        /// <see cref="SendMessageCommand.ExternalTransaction"/> is non-null. The producer
        /// surface (<c>PostgreSqlRelationalProducerQueue&lt;T&gt;</c>) guarantees the
        /// transaction is an <see cref="NpgsqlTransaction"/> and its connection's database
        /// matches the queue's configured database via the validator at the API boundary,
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
            // Cast guard: the producer subclass enforces this via GuardNpgsqlTransaction;
            // an invalid type would have failed at the producer surface with a clean
            // diagnostic message before reaching this method.
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

                id = Convert.ToInt64(command.ExecuteScalar());
            }

            if (id <= 0)
            {
                throw new DotNetWorkQueueException(
                    "Failed to insert record - the ID of the new record returned by server was 0");
            }

            var expiration = TimeSpan.Zero;
            if (_messageExpirationEnabled.Value)
            {
                expiration = MessageExpiration.GetExpiration(commandSend, data => data.GetExpiration());
            }

            // PG-specific: CreateMetaDataRecord takes a DateTime currentTime as the eighth
            // argument (see SendMessage.BuildMetaCommand). Materialize once via the injected
            // IGetTime so the metadata row matches the self-managed-tx path's clock semantics.
            CreateMetaDataRecord(commandSend.MessageData.GetDelay(), expiration, npgsqlConn, id,
                commandSend.MessageToSend, commandSend.MessageData, npgsqlTx,
                _getTime.GetCurrentUtcDate());

            if (_options.Value.EnableStatusTable)
            {
                CreateStatusRecord(npgsqlConn, id, commandSend.MessageToSend, commandSend.MessageData, npgsqlTx);
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

The existing `CreateStatusRecord` and `CreateMetaDataRecord` helpers (lines 189-223 of the current file) are reused verbatim — they already accept `NpgsqlConnection`, `NpgsqlTransaction`, and (for `CreateMetaDataRecord`) the `DateTime currentTime` parameter.

**Architect note for builder (CONTEXT-4 Rule B):** The trailing lifecycle-invariant comment MUST use the EXACT wording `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.` — Phase 3 PLAN-2.1 used a different phrasing ("Deliberately NO trans.Commit() / Rollback() ...") that left the forbidden substrings `.Commit()` / `.Rollback()` in the source, requiring a test-side comment-stripping workaround. The wording above eliminates that need entirely: no period+method-name+parens substrings appear, so the smoke test's plain source-text grep is correct without preprocessing. Phase 3 SUMMARY-2.2 documents the rephrase decision; Phase 4 converges both sync and async forks on the source-side rephrase for cross-plan consistency.

**Step 3: Verify the build is clean**

Run: `dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release --nologo`

Expected: `Build succeeded. 0 Error(s), 0 Warning(s)` (modulo pre-existing NU1902 advisory warnings).

A compile failure here is most likely caused by:
- Omitting `_getTime.GetCurrentUtcDate()` from the `CreateMetaDataRecord` call (CS7036: required parameter `currentTime` not provided).
- Using `SqlDbType.VarBinary` instead of `NpgsqlDbType.Bytea` (CS0246 if `using System.Data` is missing, or CS1503 type mismatch if both are imported).

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs
git commit -m "shipyard(phase-4): add HandleExternalTx fork to PostgreSQL sync handler"
```

**Acceptance criteria:**
- `SendMessageCommandHandler.Handle()` contains an early-branch on `commandSend.ExternalTransaction != null` immediately after the lazy-init block (line ~105–106 of the modified file).
- `HandleExternalTx(SendMessageCommand)` is a `private long` method with the body above.
- Fork uses `(NpgsqlTransaction)commandSend.ExternalTransaction` cast and `(NpgsqlConnection)npgsqlTx.Connection` cast.
- Fork uses `NpgsqlDbType.Bytea` for both `@body` and `@headers` parameters (NOT `SqlDbType.*`).
- Fork calls `CreateMetaDataRecord(..., _getTime.GetCurrentUtcDate())` — `_getTime.GetCurrentUtcDate()` appears exactly once in the fork body (RESEARCH §1 PG-specific requirement).
- Fork uses `DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>` and `SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>` type parameters.
- The trailing lifecycle comment uses the EXACT CONTEXT-4 Rule B wording: `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.` (no method-call substrings).
- No `npgsqlTx.Commit`, `npgsqlTx.Rollback`, `npgsqlTx.Dispose`, `npgsqlConn.Close`, or `npgsqlConn.Dispose` invocations inside `HandleExternalTx` (grep gate enforces 0 matches for `.Commit()`, `.Rollback()`, `.Close()`, `.Dispose()` in the fork body).
- Release build clean; existing self-managed-tx path (lines 115-178 of the original file) preserved unchanged.

### Task 2: Add 3 structural smoke tests for the sync fork

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SendMessageCommandHandlerForkSmokeTests.cs`

**Step 1: Write the smoke test class**

Per RESEARCH §5 and CLAUDE.md sync-vs-async mocking lesson, we cannot invoke `Handle()` with mocked `NpgsqlConnection`/`NpgsqlTransaction` (sealed). The smoke test class contains exactly **3 `[TestMethod]`** that verify:

1. **`HandleExternalTx_PrivateMethod_ExistsWithExpectedSignature`** — reflection assertion that `HandleExternalTx(SendMessageCommand)` exists as a private instance method on `SendMessageCommandHandler` with return type `long`.
2. **`Handle_SourceContainsExternalTransactionEarlyBranch`** — source-text grep confirms `Handle()` has the early-branch null-check, dispatches to `HandleExternalTx`, and declares `private long HandleExternalTx`.
3. **`HandleExternalTx_DoesNotCommitOrRollbackOrCloseOrDispose`** — source-text grep on the fork body confirms no `.Commit()` / `.Rollback()` / `.Close()` / `.Dispose()` substrings. Because CONTEXT-4 Rule B requires the lifecycle comment to use word forms (no method-call substrings), no preprocessing is needed.

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

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Structural smoke tests for the PostgreSQL sync handler's HandleExternalTx fork.
    /// Per RESEARCH §5 + CLAUDE.md sync-vs-async mocking lesson, direct execution tests
    /// of the fork are infeasible at the unit-test level (sealed NpgsqlConnection /
    /// NpgsqlTransaction / NpgsqlCommand types) and live in Phase 6 integration tests
    /// against a real PostgreSQL instance. This test verifies only the structural shape
    /// of the fork: it exists, has the expected signature, and is invoked by Handle().
    /// </summary>
    [TestClass]
    public class SendMessageCommandHandlerForkSmokeTests
    {
        [TestMethod]
        public void HandleExternalTx_PrivateMethod_ExistsWithExpectedSignature()
        {
            // SendMessageCommandHandler is internal — typeof reaches it because the test
            // project has access via ProjectReference + InternalsVisibleTo. If the typeof
            // fails to compile in the test project, fall back to:
            //   var handlerType = typeof(PostgreSqlMessageQueueInit).Assembly
            //       .GetType("DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler.SendMessageCommandHandler",
            //                throwOnError: true);
            var handlerType = typeof(DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler.SendMessageCommandHandler);

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
            // Resolve the source file relative to the test bin output. dotnet test runs
            // from the project's bin directory; the source file is 4 levels up + into
            // the main PG project's Basic/CommandHandler folder.
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.PostgreSQL",
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
            // on the caller's transaction or connection. CONTEXT-4 Rule B mandates the
            // lifecycle comment uses word forms ("no Commit, Rollback, Close, or Dispose")
            // so plain substring search is safe — no preprocessing needed.
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.PostgreSQL",
                "Basic", "CommandHandler",
                "SendMessageCommandHandler.cs");
            sourcePath = Path.GetFullPath(sourcePath);

            var content = File.ReadAllText(sourcePath);
            // Extract the body of HandleExternalTx by anchoring on its signature.
            // Conservative end-bound: 6000 chars forward (fork is ~80 lines, plenty).
            var forkStart = content.IndexOf("private long HandleExternalTx",
                System.StringComparison.Ordinal);
            Assert.IsTrue(forkStart >= 0, "HandleExternalTx not found in source.");
            var forkBody = content.Substring(forkStart, System.Math.Min(6000, content.Length - forkStart));

            Assert.IsFalse(forkBody.Contains(".Commit()"),   "HandleExternalTx must not call .Commit() on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".Rollback()"), "HandleExternalTx must not call .Rollback() on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".Close()"),    "HandleExternalTx must not call .Close() on the caller's connection.");
            Assert.IsFalse(forkBody.Contains(".Dispose()"),  "HandleExternalTx must not call .Dispose() on the caller's connection or transaction.");
        }
    }
}
```

**Step 2: Run test to verify it passes**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~SendMessageCommandHandlerForkSmokeTests" --nologo`

Expected: PASS — `Passed: 3, Failed: 0, Skipped: 0, Total: 3`.

If `HandleExternalTx_DoesNotCommitOrRollbackOrCloseOrDispose` fails, the most likely cause is a Phase 3-style lifecycle comment containing `.Commit()`/`.Rollback()` substrings (e.g., "Deliberately NO trans.Commit()..."). Rephrase the comment to use word forms per CONTEXT-4 Rule B; do NOT modify the test.

**Step 3: Confirm full suite still green**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --nologo
```

Expected: `Failed: 0`.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SendMessageCommandHandlerForkSmokeTests.cs
git commit -m "shipyard(phase-4): add structural smoke tests for PG sync handler fork"
```

**Acceptance criteria:**
- 3 smoke tests pass: signature-exists (private `long HandleExternalTx(SendMessageCommand)`), source-contains-early-branch (`commandSend.ExternalTransaction != null` + `return HandleExternalTx(commandSend);` + `private long HandleExternalTx`), source-has-no-Commit/Rollback/Close/Dispose.
- The lifecycle-ownership grep test (Test 3) explicitly enforces PROJECT.md §Success Criteria #7 at the source-text level (Phase 6 integration tests confirm runtime side).
- No regressions in `Transport.PostgreSQL.Tests`.

## Verification

```bash
# Source contains the early-branch
grep -c "commandSend.ExternalTransaction != null" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs
# expected: 1
grep -c "private long HandleExternalTx" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs
# expected: 1
grep -c "return HandleExternalTx(commandSend);" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs
# expected: 1

# PG-specific: _getTime.GetCurrentUtcDate() appears in the fork body
awk '/private long HandleExternalTx/,/^        }$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs | grep -c "_getTime.GetCurrentUtcDate()"
# expected: 1

# PG-specific: NpgsqlDbType.Bytea used (NOT SqlDbType.VarBinary)
awk '/private long HandleExternalTx/,/^        }$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs | grep -c "NpgsqlDbType.Bytea"
# expected: 2 (one each for @body and @headers)
awk '/private long HandleExternalTx/,/^        }$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs | grep -c "SqlDbType"
# expected: 0

# CONTEXT-4 Rule B: lifecycle comment uses word forms (no method-call substrings)
awk '/private long HandleExternalTx/,/^        }$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs | grep -c "Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here."
# expected: 1

# Fork body has no lifecycle calls
awk '/private long HandleExternalTx/,/^        }$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs | grep -cE "\.Commit\(\)|\.Rollback\(\)|\.Close\(\)|\.Dispose\(\)"
# expected: 0

# Release build clean
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release --nologo
# expected: 0 Error(s), 0 Warning(s) (modulo pre-existing NU1902)

# 3 smoke tests pass; full PG.Tests suite still green
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~SendMessageCommandHandlerForkSmokeTests" --nologo
# expected: Passed: 3, Failed: 0
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --nologo
# expected: Failed: 0
```
