# Normalize DurationMs Read-Side + Dashboard UI Display

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

---
phase: fix-history-duration
plan: 1.2
wave: 2
dependencies: [1.1]
must_haves:
  - Redis QueryMessageHistoryHandler preserves DurationMs=0 (does not convert 0 to null)
  - Redis write-side regression tests lock in DurationMs=0L contract for RecordComplete/RecordError without StartedUtc
  - LiteDb QueryMessageHistoryHandler preserves DurationMs=0 (does not convert 0 to null)
  - Dashboard UI HistoryTab.FormatDuration renders "< 1 ms" when ms==0
  - Dashboard UI HistoryTab.FormatDuration continues to render "-" when ms is null (Enqueued/Processing rows that never completed)
  - Memory Dashboard API integration tests pass end-to-end
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis/Basic/QueryMessageHistoryHandler.cs
  - Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/QueryMessageHistoryHandlerTests.cs
  - Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs
  - Source/DotNetWorkQueue.Transport.LiteDB/Basic/QueryMessageHistoryHandler.cs
  - Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/QueryMessageHistoryHandlerTests.cs
  - Source/DotNetWorkQueue.Dashboard.Ui/Components/Shared/HistoryTab.razor
tdd: true
---

**Goal:** Stop Redis and LiteDb from converting a stored `0` back to `null` on read; update the Dashboard's `FormatDuration` to render `"< 1 ms"` for `0` while preserving `"-"` for `null`. PLAN-1.1's write-side fix is a precondition — without it, there is nothing to read back as `0`.

**Architecture:** Read path consists of two transport-specific `QueryMessageHistoryHandler` classes that map raw storage (Redis hash entries / LiteDB documents) to `MessageHistoryRecord`. Both currently collapse `0` to `null` at the mapping boundary. The UI is a Blazor Server page (`HistoryTab.razor`) that calls `FormatDuration(long? ms)` per row. Three states must be distinguishable after this plan: `null` (never completed), `0` (sub-ms completion), `>0` (measured).

**Tech Stack:** C# (.NET 10 / .NET 8 / .NET Framework 4.8 / .NET Standard 2.0), Blazor Server, MudBlazor 9.1.0, MSTest 3.x.

**Null-rendering decision:** Preserve existing behavior. `FormatDuration(null)` returns `"-"` (unchanged). `FormatDuration(0)` returns `"< 1 ms"` (new). `FormatDuration(>0)` returns existing formatted string. This is the minimal non-breaking change — rows in Enqueued/Processing status (where DurationMs is genuinely null because the row never completed) continue to show `-`, while completed-but-sub-ms rows show `< 1 ms`.

---

<task id="1" name="Redis transport: read-side fix + write-side regression coverage" files="Source/DotNetWorkQueue.Transport.Redis/Basic/QueryMessageHistoryHandler.cs, Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/QueryMessageHistoryHandlerTests.cs, Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs" tdd="true">
  <description>
    Two concerns bundled because both target the Redis transport and share the same DurationMs=0 contract. (a) READ FIX: QueryMessageHistoryHandler line 124 reads `DurationMs = durationMs > 0 ? durationMs : (long?)null`. After PLAN-1.1, a sub-ms Complete row stores `0L` on Redis — the query handler then silently converts it back to null, re-creating the exact bug. Use `CompletedUtc` as the discriminator: if `completedTicks > 0` (row completed), preserve DurationMs as stored (including 0); if `completedTicks == 0` (row still running), return null regardless. (b) WRITE-SIDE REGRESSION TESTS: Redis `WriteMessageHistoryHandler` already computes `durationMs = startedTicks > 0 ? ... : 0L` in both RecordComplete and RecordError (lines 69-80). No production change is needed on the write side. Add regression tests that prove `DurationMs = 0L` is HashSet-written when `StartedUtc` is `0L`. This locks in the contract that the read-side fix above relies on.
  </description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/QueryMessageHistoryHandlerTests.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis/Basic/QueryMessageHistoryHandler.cs:124</modify>
  </files>
  <steps>
    <step>RED (read-side): Add a test `LoadRecord_CompletedStatus_DurationZero_PreservesZero` in Redis QueryMessageHistoryHandlerTests. Arrange an IDatabase mock returning HashGetAll with Status=Complete(2), CompletedUtc=someTicks, DurationMs=0L, StartedUtc=someTicks. Assert `record.DurationMs.Should().Be(0L)` (not null). Also add `LoadRecord_EnqueuedStatus_NoCompletedUtc_DurationIsNull` with Status=Enqueued(0), CompletedUtc=0, DurationMs=0, asserting `record.DurationMs.Should().BeNull()`.</step>
    <step>RED (write-side lock-in): In WriteMessageHistoryHandlerTests, add `RecordComplete_WithoutStartedUtc_WritesDurationZero` and `RecordError_WithoutStartedUtc_WritesDurationZero`. Using the existing TestableWriteMessageHistoryHandler pattern, intercept HashSet (introduce `protected virtual GetDb()` seam if required — no behavioral change, just a test hook). Verify the HashEntry with `Name=="DurationMs"` is written with value `0L` when the mocked HashGet for StartedUtc returns 0.</step>
    <step>RED verify: the read-side `_PreservesZero` test FAILS ("Expected 0, Actual null"); `_DurationIsNull` PASSES trivially; the two write-side tests PASS against current green code (lock-in).</step>
    <step>GREEN: Replace QueryMessageHistoryHandler.cs line 124 `DurationMs = durationMs > 0 ? durationMs : (long?)null,` with `DurationMs = completedTicks > 0 ? durationMs : (long?)null,`. No production change to WriteMessageHistoryHandler (except optional `protected virtual GetDb()` seam).</step>
    <step>GREEN verify: re-run the Redis query-handler AND write-handler filters; all tests pass.</step>
    <step>Commit: "fix(redis-history): preserve DurationMs=0 for completed rows on read + lock in write-side contract"</step>
  </steps>
  <verification>
    <command>dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~QueryMessageHistoryHandler|FullyQualifiedName~WriteMessageHistoryHandler"</command>
    <expected>Passed! - Failed: 0, Passed: &gt;=10, Skipped: 0</expected>
  </verification>
  <acceptance>
    Line 124 of QueryMessageHistoryHandler.cs reads `DurationMs = completedTicks > 0 ? durationMs : (long?)null,`. Four new tests exist (2 query + 2 write regression), all passing. If a `GetDb()` seam was added to WriteMessageHistoryHandler, it is `protected virtual` only — no behavioral change. Existing Redis history tests continue to pass.
  </acceptance>
</task>

<task id="2" name="LiteDb QueryMessageHistoryHandler: stop converting DurationMs 0 to null" files="Source/DotNetWorkQueue.Transport.LiteDB/Basic/QueryMessageHistoryHandler.cs, Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/QueryMessageHistoryHandlerTests.cs" tdd="true">
  <description>
    Line 100 reads `DurationMs = h.DurationMs > 0 ? h.DurationMs : (long?)null`. Same bug as Redis. `HistoryTable.CompletedUtc` is `long` (stored as ticks, 0 when not set). Apply the identical discriminator pattern: use `h.CompletedUtc` to decide null vs preserve.
  </description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/QueryMessageHistoryHandlerTests.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.LiteDB/Basic/QueryMessageHistoryHandler.cs:100</modive>
  </files>
  <steps>
    <step>RED: Add `Query_CompletedRow_DurationZero_PreservesZero` and `Query_EnqueuedRow_NoCompletion_DurationIsNull` tests in LiteDb QueryMessageHistoryHandlerTests. First test inserts a HistoryTable row with Status=2 (Complete), CompletedUtc=somevalue>0, DurationMs=0 and asserts mapped record.DurationMs == 0L. Second test inserts Status=0, CompletedUtc=0, DurationMs=0 and asserts mapped record.DurationMs is null.</step>
    <step>RED verify: first test FAILS, second PASSES.</step>
    <step>GREEN: Replace line 100 `DurationMs = h.DurationMs > 0 ? h.DurationMs : (long?)null,` with `DurationMs = h.CompletedUtc > 0 ? h.DurationMs : (long?)null,`.</step>
    <step>GREEN verify: both tests pass.</step>
    <step>Commit: "fix(litedb-history): preserve DurationMs=0 for completed rows on read"</step>
  </steps>
  <verification>
    <command>dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~QueryMessageHistoryHandler"</command>
    <expected>Passed! - Failed: 0, Passed: &gt;=2, Skipped: 0</expected>
  </verification>
  <acceptance>
    Line 100 uses `h.CompletedUtc > 0` as the discriminator. Both new tests pass. Existing query-handler tests continue to pass.
  </acceptance>
</task>

<task id="3" name="Dashboard UI: render '< 1 ms' for DurationMs==0, preserve '-' for null" files="Source/DotNetWorkQueue.Dashboard.Ui/Components/Shared/HistoryTab.razor" tdd="false">
  <description>
    `FormatDuration` in HistoryTab.razor:151 currently returns `"-"` for null and `$"{ms}ms"` for any non-null value (so 0 renders as "0ms"). Change the zero path to return `"< 1 ms"`. Keep the null path exactly as-is. No test framework exists for the Razor component directly; verification is via the Dashboard.Api.Integration.Tests suite (Memory filter) plus a build check.
  </description>
  <files>
    <modify>Source/DotNetWorkQueue.Dashboard.Ui/Components/Shared/HistoryTab.razor:151-158</modify>
  </files>
  <steps>
    <step>Open HistoryTab.razor. Locate `FormatDuration(long? ms)`. After the `if (!ms.HasValue) return "-";` line, insert `if (ms == 0) return "< 1 ms";`. Leave the remaining three lines unchanged.</step>
    <step>Build the Dashboard UI project alone to catch any Razor syntax error fast.</step>
    <step>Run the Memory-filtered Dashboard API integration tests to confirm the full read path (write handler -> query handler -> API -> UI-adjacent serialization) produces no regression. Memory tests are used because they do not require external services.</step>
    <step>Commit: "feat(dashboard-ui): render '&lt; 1 ms' for sub-millisecond history durations"</step>
  </steps>
  <verification>
    <command>dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj" -c Debug</command>
    <expected>Build succeeded. 0 Error(s)</expected>
  </verification>
  <acceptance>
    `FormatDuration(null)` returns `"-"`. `FormatDuration(0)` returns `"< 1 ms"`. `FormatDuration(500)` returns `"500ms"`. `FormatDuration(1500)` returns `"1.5s"`. Dashboard UI project builds clean with no warnings.
  </acceptance>
</task>

## Verification

End-to-end verification on Memory transport (canonical success criterion from PROJECT.md #6) is run at the plan level after all three tasks complete. This proves the full write-side (PLAN-1.1) + read-side + UI path behaves correctly without requiring external services.

```bash
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory"
```

Expected: Build succeeded, 0 Error(s), Failed: 0 on each test project. All Memory-transport Dashboard API integration tests pass, confirming sub-millisecond completions surface as `DurationMs=0` through the API and render as `"< 1 ms"` in the UI.
