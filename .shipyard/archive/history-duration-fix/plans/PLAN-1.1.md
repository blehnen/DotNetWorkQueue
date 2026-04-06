# Normalize DurationMs Write-Side (RecordComplete + RecordError)

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

---
phase: fix-history-duration
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - Memory, RelationalDatabase, LiteDb, Redis all store DurationMs = 0 (not null) when StartedUtc is missing and status becomes Complete
  - Memory, RelationalDatabase, LiteDb, Redis all store DurationMs = 0 (not null) when StartedUtc is missing and status becomes Error
  - TDD: every production change preceded by a Red test commit or an updated assertion that fails against current code
  - No change to MessageHistoryRecord shape, metrics, OpenTelemetry, or API response contracts
files_touched:
  - Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs
  - Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs
  - Source/DotNetWorkQueue.Transport.LiteDB/Basic/WriteMessageHistoryHandler.cs
  - Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/WriteMessageHistoryHandlerTests.cs
tdd: true
---

**Goal:** Normalize the Memory, RelationalDatabase, and LiteDb transports' history writers so that when `RecordComplete` or `RecordError` runs and `StartedUtc` was never persisted, `DurationMs` is stored as `0L` (not null / not left unset). This eliminates the race-window inconsistency where sub-millisecond messages showed blank duration. UI and query-side read conversion — plus Redis write-side regression coverage — are handled in PLAN-1.2.

**Architecture:** Three transports, each with their own `WriteMessageHistoryHandler` implementing `IWriteMessageHistory`. The Memory transport uses a `ConcurrentDictionary` of `MessageHistoryRecord`. RelationalDatabase issues two UPDATEs (status+completed, then duration). LiteDb uses BSON document updates. Redis write-side already stores `0L` correctly (see line 69-80 of its handler) — its regression tests and read-side fix are consolidated into PLAN-1.2 Task 1. The three changes in this plan touch independent test projects and run sequentially as TDD cycles.

**Tech Stack:** C# (.NET 10 / .NET 8 / .NET Framework 4.8 / .NET Standard 2.0), MSTest 3.x, NSubstitute, FluentAssertions 6.12.2, LiteDB 5.x, StackExchange.Redis.

---

<task id="1" name="Memory transport: flip null->0 for RecordComplete and RecordError without StartedUtc" files="Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs, Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs" tdd="true">
  <description>
    The existing Memory tests `RecordComplete_WithoutStarted_DurationIsNull` (line 141) and `RecordError_WithoutStarted_DurationIsNull` (line 192) assert the current (buggy) behavior that leaves `DurationMs` null when StartedUtc was never set. Flip those assertions to `== 0`, rename the tests to match, confirm they fail, then patch the production handler to set `DurationMs = 0` in the else branch.
  </description>
  <files>
    <modify>Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs:141-203</modify>
    <modify>Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs:64-84</modify>
  </files>
  <steps>
    <step>RED: In the test file, rename `RecordComplete_WithoutStarted_DurationIsNull` to `RecordComplete_WithoutStarted_DurationIsZero`; change the final assertion from `Assert.IsNull(record.DurationMs);` to `Assert.AreEqual(0L, record.DurationMs);`. Do the same for `RecordError_WithoutStarted_DurationIsNull` -> `RecordError_WithoutStarted_DurationIsZero`.</step>
    <step>RED verify: run the Memory test filter; expect both renamed tests to FAIL with "Expected: 0, Actual: (null)".</step>
    <step>GREEN: In `WriteMessageHistoryHandler.cs` RecordComplete, add `else r.DurationMs = 0;` after the `if (r.StartedUtc.HasValue) r.DurationMs = (long)(now - r.StartedUtc.Value).TotalMilliseconds;`. Apply the identical change in RecordError.</step>
    <step>GREEN verify: re-run the same filter; all tests pass.</step>
    <step>Commit: "fix(memory-history): record DurationMs=0 when StartedUtc missing on complete/error"</step>
  </steps>
  <verification>
    <command>dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"</command>
    <expected>Passed! - Failed: 0, Passed: &gt;=12, Skipped: 0</expected>
  </verification>
  <acceptance>
    Both `RecordComplete_WithoutStarted_DurationIsZero` and `RecordError_WithoutStarted_DurationIsZero` pass. No other Memory history tests regress. `DurationMs` remains `long?` on the record model.
  </acceptance>
</task>

<task id="2" name="RelationalDatabase transport: set DurationMs=0 when StartedUtc is NULL in RecordComplete and RecordError" files="Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs, Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/WriteMessageHistoryHandlerTests.cs" tdd="true">
  <description>
    RelationalDatabase `RecordComplete` (line 98) runs two UPDATE statements. The second filters `WHERE ... StartedUtc IS NOT NULL`, leaving `DurationMs` NULL when StartedUtc was never persisted. `RecordError` (line 146) similarly uses `durationMs = (long?)null` when StartedUtc is null. Add a test against the mocked `IDbConnectionFactory` that verifies the `@DurationMs` parameter is `0L` (not `DBNull`) when no StartedUtc exists, then fix production.
    NOTE: Roadmap described a single-UPDATE pattern — the code has since been refactored to two UPDATEs. The fix path is: (a) in `RecordComplete`, remove the `StartedUtc IS NOT NULL` guard from the second UPDATE's WHERE clause so the 0-path also writes (and `durationMs` already evaluates to `0L` when `startTime.HasValue` is false); (b) in `RecordError`, change `(long?)null` to `0L` and pass the int64 directly (no DBNull.Value branch for this value).
  </description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs:98-175</modify>
    <modify>Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/WriteMessageHistoryHandlerTests.cs</modify>
  </files>
  <steps>
    <step>RED: In the RelationalDatabase test project, add `RecordComplete_WithoutStartedUtc_PassesDurationZero` and `RecordError_WithoutStartedUtc_PassesDurationZero`. Using NSubstitute, mock the connection/command to return NULL from the StartedUtc lookup, invoke `RecordComplete`/`RecordError`, then assert `command.Parameters["@DurationMs"].Value` equals `0L` (and not `DBNull.Value`).</step>
    <step>RED verify: run the RelationalDatabase WriteMessageHistoryHandler filter; expect 2 failures.</step>
    <step>GREEN: In `WriteMessageHistoryHandler.cs`, update `RecordComplete` second UPDATE's WHERE clause to drop `StartedUtc IS NOT NULL AND` (keep `CompletedUtc IS NOT NULL AND DurationMs IS NULL`). The existing code already computes `durationMs = startTime.HasValue ? ... : 0L`, so removing the WHERE guard writes the 0. In `RecordError`, replace `(long?)null` with `0L` and change `AddParameter(command, "@DurationMs", DbType.Int64, (object)durationMs ?? DBNull.Value)` to `AddParameter(command, "@DurationMs", DbType.Int64, durationMs)`.</step>
    <step>GREEN verify: re-run filter; tests pass.</step>
    <step>Commit: "fix(relational-history): write DurationMs=0 instead of NULL when StartedUtc missing"</step>
  </steps>
  <verification>
    <command>dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"</command>
    <expected>Passed! - Failed: 0, Passed: &gt;=2, Skipped: 0</expected>
  </verification>
  <acceptance>
    Both new tests pass. Manual code reading confirms the second UPDATE no longer filters by `StartedUtc IS NOT NULL`, and `RecordError` passes `0L` for `@DurationMs` when StartedUtc lookup returned null.
  </acceptance>
</task>

<task id="3" name="LiteDb transport: explicit DurationMs=0 in RecordComplete and RecordError when StartedUtc==0" files="Source/DotNetWorkQueue.Transport.LiteDB/Basic/WriteMessageHistoryHandler.cs, Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/WriteMessageHistoryHandlerTests.cs" tdd="true">
  <description>
    LiteDb `RecordComplete` (line 82) and `RecordError` (line 102) skip writing `DurationMs` when `record.StartedUtc == 0`. Because `HistoryTable.DurationMs` is `long` with default 0, the stored value happens to be 0 — but only because LiteDB preserves the prior value. This is fragile. Add explicit `else record.DurationMs = 0;` in both methods. Tests already exist asserting `DurationMs == 0` for the error-without-start case (LiteDb tests:232), and the Complete-without-start path is untested — add it as the Red test first.
  </description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/WriteMessageHistoryHandlerTests.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.LiteDB/Basic/WriteMessageHistoryHandler.cs:82-120</modify>
  </files>
  <steps>
    <step>RED: Add test `RecordComplete_WithoutProcessingStart_StoresDurationZero` in LiteDb WriteMessageHistoryHandlerTests. It calls `RecordEnqueue("q1", ...)`, skips `RecordProcessingStart`, directly flips the record's Status to Processing via a manual update (since RecordComplete requires Status==Processing to match), then calls `RecordComplete` and asserts `record.DurationMs.Should().Be(0)` AND `record.StartedUtc.Should().Be(0)` to document the state. Alternative simpler form: call RecordProcessingStart then immediately reset `StartedUtc` to 0 via col.Update before RecordComplete. Pick whichever is cleanest and document in the test name.</step>
    <step>RED verify: expect FAIL if the current code path leaves DurationMs at whatever LiteDb previously stored (could be 0 on insert default, could be stale — the explicit test will either pass accidentally or fail). If it passes accidentally, skip to GREEN and commit as a "lock in" test. If it fails, proceed normally.</step>
    <step>GREEN: In `WriteMessageHistoryHandler.cs` `RecordComplete`, change `if (record.StartedUtc > 0) record.DurationMs = (long)(now - new DateTime(record.StartedUtc, DateTimeKind.Utc)).TotalMilliseconds;` to `record.DurationMs = record.StartedUtc > 0 ? (long)(now - new DateTime(record.StartedUtc, DateTimeKind.Utc)).TotalMilliseconds : 0L;`. Same mechanical change in `RecordError`.</step>
    <step>GREEN verify: run LiteDb WriteMessageHistoryHandler filter; all tests pass including existing `RecordError_NoProcessingStart` at line 220-ish that already asserts DurationMs==0.</step>
    <step>Commit: "fix(litedb-history): explicitly write DurationMs=0 when StartedUtc not persisted"</step>
  </steps>
  <verification>
    <command>dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"</command>
    <expected>Passed! - Failed: 0, Passed: &gt;=10, Skipped: 0</expected>
  </verification>
  <acceptance>
    Both RecordComplete and RecordError explicitly assign `record.DurationMs = 0;` in the else branch. New test passes. Existing tests continue to pass.
  </acceptance>
</task>

