# Build Summary: Plan 1.3 -- Simplification Review Feedback

## Status: complete

## Tasks Completed

- **Change 1:** Shared `SimpleProducerWithTraceVerification` helper -- COMPLETE
- **Change 2:** Listener always-on, collection opt-in via `bool collectActivities = false` -- COMPLETE
- **Change 3:** Reverted `SharedSetup` from `public` to `internal` -- COMPLETE

## Files Modified

- `Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs`
  - `ActivitySourceWrapper` constructor takes optional `bool collectActivities = false`
  - Listener is always registered (preserves coverage cascade)
  - `ActivityStarted` callback only wired when opted in
  - `SharedSetup` reverted to `internal`
  - `CreateTrace(string name, bool collectActivities = false)` overload

- `Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/Implementation/SimpleProducerWithTraceVerification.cs` (NEW)
  - Reusable trace verification helper for any transport
  - Sends one message, asserts at least one `SendMessage` activity collected

- `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Producer/SimpleProducer.cs`
  - `RunWithTraceVerification` shrunk from ~40 lines to 4-line delegation

## Verification Results

| Check | Result |
|-------|--------|
| `dotnet build Source/DotNetWorkQueue.sln -c Debug` | 0 warnings, 0 errors |
| `RunWithTraceVerification` filter | 1 passed (557 ms) |
| Full Memory integration suite | 57 passed, 0 failed (7m 53s) |

## Commit

`139ec4bf shipyard(phase-1): apply simplification review feedback`

## Key Decisions

- **Listener always-on, collection opt-in:** Per user direction, kept the listener always registered so the trace coverage cascade across all transports still works automatically. Only the `ConcurrentBag<Activity>.Add` callback is gated behind the opt-in flag. This preserves Phase 1's primary goal while addressing the simplifier's concern about always-on collection.
- **Helper location:** New shared helper went in `Implementation/` alongside existing `SimpleProducer.cs` to follow the established pattern. Other transports can now adopt `RunWithTraceVerification` with a 4-line test method.
