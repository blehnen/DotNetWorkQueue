---
phase: async-dispose-fix
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - DashboardConsumerClient implements both IDisposable and IAsyncDisposable
  - DisposeAsync() properly awaits HTTP DELETE unregistration
  - Synchronous Dispose() does not call .GetAwaiter().GetResult() (no sync-over-async)
  - Both Dispose() and DisposeAsync() are idempotent via shared _disposed flag
  - GC.SuppressFinalize(this) called in both disposal paths
  - Unit tests cover DisposeAsync behavior, cross-dispose safety, and sync Dispose not blocking
files_touched:
  - Source/DotNetWorkQueue.Dashboard.Client/DashboardConsumerClient.cs
  - Source/DotNetWorkQueue.Dashboard.Client.Tests/DashboardConsumerClientTests.cs
tdd: true
---

# Plan 1.1: Implement IAsyncDisposable on DashboardConsumerClient

## Problem

`DashboardConsumerClient.Dispose()` (line 242-267) calls `.ConfigureAwait(false).GetAwaiter().GetResult()` on `_httpClient.DeleteAsync(...)`, which is a sync-over-async anti-pattern that can deadlock in SynchronizationContext environments (ASP.NET, WPF, WinForms). The class does not implement `IAsyncDisposable`, so callers cannot use `await using`.

## Design (from RESEARCH.md, Option B -- recommended)

- **`DisposeAsync()`**: Idempotency check via `Interlocked.CompareExchange(ref _disposed, 1, 0)`. Delegate to existing `StopAsync()` which already correctly stops the timer and sends the HTTP DELETE best-effort. Then dispose the timer, dispose HttpClient if owned, call `GC.SuppressFinalize(this)`.
- **`Dispose()` revised**: Same idempotency check. Stop and dispose the timer synchronously. Skip the HTTP DELETE entirely (no sync-over-async). Clear `_consumerId`. Dispose HttpClient if owned. Call `GC.SuppressFinalize(this)`. Add XML comment documenting that callers should prefer `DisposeAsync()` or call `StopAsync()` before `Dispose()` for graceful unregistration; the server's heartbeat pruning handles orphaned consumers.
- Both methods share the `_disposed` int field (already exists, already uses `Interlocked.CompareExchange`), so calling one then the other is a no-op.
- The project targets net10.0 and net8.0 only -- `IAsyncDisposable`, `ValueTask`, and `Timer.DisposeAsync()` are all available without polyfills or conditional compilation.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Dashboard.Client.Tests/DashboardConsumerClientTests.cs" tdd="true">
  <action>
    Add the following new test methods to the existing `DashboardConsumerClientTests` class. Use the same patterns already in the file: MSTest `[TestClass]`/`[TestMethod]`, FluentAssertions, the existing `MockHandler` inner class for HTTP mocking, and the existing helper methods (`CreateClient`, `CreateOptions`, mock handler setup).

    New tests to add (all async `Task`-returning methods):

    1. **`DisposeAsync_Is_Idempotent`** -- Create a client, call `await client.DisposeAsync()` twice. Second call should not throw.

    2. **`DisposeAsync_With_Registration_Sends_Delete`** -- Create a client with a mock handler that returns 200 for POST (register) and 200 for DELETE. Call `StartAsync()` to register, then `await client.DisposeAsync()`. Assert the mock handler received an HTTP DELETE request to the consumers endpoint.

    3. **`DisposeAsync_Without_Registration_Does_Not_Send_Delete`** -- Create a client, call `await client.DisposeAsync()` without ever calling `StartAsync()`. Assert no DELETE request was sent.

    4. **`DisposeAsync_Delete_Throws_Is_Swallowed`** -- Create a client with a mock handler that returns 200 for POST but throws `HttpRequestException` for DELETE. Call `StartAsync()`, then `await client.DisposeAsync()`. Should not throw.

    5. **`DisposeAsync_Owned_HttpClient_Is_Disposed`** -- Create a client using the constructor that owns the HttpClient. Call `await client.DisposeAsync()`. Verify the HttpClient is disposed (attempting to use it throws `ObjectDisposedException`).

    6. **`DisposeAsync_Then_Dispose_Is_Safe`** -- Call `await client.DisposeAsync()` then `client.Dispose()`. No exception.

    7. **`Dispose_Then_DisposeAsync_Is_Safe`** -- Call `client.Dispose()` then `await client.DisposeAsync()`. No exception.

    These tests should be written BEFORE the implementation changes in Task 2, so they will initially fail (TDD red phase).
  </action>
  <verify>dotnet test "Source\DotNetWorkQueue.Dashboard.Client.Tests\DotNetWorkQueue.Dashboard.Client.Tests.csproj" --filter "FullyQualifiedName~DisposeAsync" --no-build 2>&1 | head -5</verify>
  <done>All 7 new DisposeAsync test methods exist in the test file and compile. They will fail until Task 2 is implemented (DashboardConsumerClient does not yet implement IAsyncDisposable).</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Dashboard.Client/DashboardConsumerClient.cs" tdd="true">
  <action>
    Modify `DashboardConsumerClient` to implement `IAsyncDisposable` alongside the existing `IDisposable`.

    **Step 1 -- Change class declaration (line 32):**
    From: `public class DashboardConsumerClient : IDisposable`
    To: `public class DashboardConsumerClient : IDisposable, IAsyncDisposable`

    **Step 2 -- Add `DisposeAsync()` method** (add after the existing `Dispose()` method, before the `RegistrationResult` class):
    ```csharp
    /// <summary>
    /// Asynchronously disposes the client, gracefully unregistering from the dashboard.
    /// Prefer this over <see cref="Dispose()"/> to ensure the HTTP DELETE unregistration completes.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            return;

        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        catch
        {
            // Best-effort unregistration
        }

        _heartbeatTimer.Dispose();

        if (_ownsHttpClient)
            _httpClient.Dispose();

        GC.SuppressFinalize(this);
    }
    ```

    Key points:
    - Delegates to `StopAsync()` which already stops the timer via `_heartbeatTimer.Change(Timeout.Infinite, ...)` and sends the HTTP DELETE best-effort.
    - Wraps `StopAsync` in try/catch because it is best-effort.
    - After `StopAsync`, disposes the timer and optionally the HttpClient.
    - Note: `StopAsync` internally checks `_consumerId.HasValue` and skips DELETE if not registered (already idempotent).
    - Note: `StopAsync` also calls `_heartbeatTimer.Change(Timeout.Infinite, ...)` which is safe to call before `_heartbeatTimer.Dispose()`.

    **Step 3 -- Revise existing `Dispose()` method (lines 242-267):**
    Replace the entire Dispose method body with:
    ```csharp
    /// <summary>
    /// Synchronously disposes the client. Does NOT attempt HTTP unregistration to avoid sync-over-async deadlocks.
    /// Callers should prefer <see cref="DisposeAsync()"/> or call <see cref="StopAsync(CancellationToken)"/> before Dispose()
    /// for graceful unregistration. The dashboard server's heartbeat pruning will handle orphaned consumers.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            return;

        _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _heartbeatTimer.Dispose();

        // Do not attempt HTTP DELETE synchronously -- sync-over-async causes deadlocks
        // in SynchronizationContext environments. The server prunes consumers that miss heartbeats.
        _consumerId = null;

        if (_ownsHttpClient)
            _httpClient.Dispose();

        GC.SuppressFinalize(this);
    }
    ```

    **Step 4 -- Verify existing tests still pass.** The existing `Dispose_With_Registration_Sends_Delete` test (line 588) asserts that a DELETE is sent on sync `Dispose()`. This test MUST be updated to reflect the new behavior: sync `Dispose()` no longer sends DELETE. Update that test to assert that DELETE is NOT sent on sync `Dispose()`, and add a comment explaining that `DisposeAsync` is the path that sends DELETE.

    Similarly, `Dispose_Delete_Throws_Is_Swallowed` (line 640) tests exception handling during sync dispose DELETE -- this test should be removed or converted to verify the new no-DELETE behavior, since the sync path no longer attempts the HTTP call.
  </action>
  <verify>dotnet test "Source\DotNetWorkQueue.Dashboard.Client.Tests\DotNetWorkQueue.Dashboard.Client.Tests.csproj" -c Debug</verify>
  <done>All tests pass (both existing and new DisposeAsync tests). `DashboardConsumerClient` implements `IAsyncDisposable`. The sync `Dispose()` no longer contains `.GetAwaiter().GetResult()`. The `DisposeAsync()` method delegates to `StopAsync()` for proper async HTTP DELETE. Both methods are idempotent via the shared `_disposed` flag. Build succeeds with no warnings for Debug configuration.</done>
</task>

## Verification Checklist

After both tasks are complete, run:

```bash
# All Dashboard.Client tests pass
dotnet test "Source\DotNetWorkQueue.Dashboard.Client.Tests\DotNetWorkQueue.Dashboard.Client.Tests.csproj" -c Debug

# Build succeeds (no warnings in release would require XML docs, which are included above)
dotnet build "Source\DotNetWorkQueue.Dashboard.Client\DotNetWorkQueue.Dashboard.Client.csproj" -c Release
```

## Notes

- The `DashboardConsumerClient` uses `Interlocked.CompareExchange(ref _disposed, 1, 0)` (not the `Interlocked.Increment(ref _disposeCount)` pattern used elsewhere in the codebase). This is because the class was added more recently. Both patterns achieve idempotent disposal; the existing pattern is preserved as-is.
- The existing `StopAsync` method (lines 185-206) already handles: stopping the timer, checking `_consumerId.HasValue`, sending HTTP DELETE best-effort, and clearing `_consumerId`. `DisposeAsync` reuses this rather than duplicating the logic.
- No changes needed to `DashboardConsumerClient.csproj` -- `IAsyncDisposable` is in-box for net8.0 and net10.0.
- No other projects reference `DashboardConsumerClient` directly, so no callers need updating. The change is purely additive (new interface implementation).
- Two existing tests must be updated in Task 2 Step 4: `Dispose_With_Registration_Sends_Delete` and `Dispose_Delete_Throws_Is_Swallowed`, because sync `Dispose()` no longer sends the HTTP DELETE.
