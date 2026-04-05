# Phase 4 Research: IntegrationTests.Metrics Removal

## What the project provides

`DotNetWorkQueue.IntegrationTests.Metrics` is a standalone project (GUID `{B7974956-3764-4B0C-B6F2-0B8F8A25BEFE}`) that provides lightweight `IMetrics` implementations for integration tests. It contains 7 types:

| Type | Role | Tracks values? |
|------|------|---------------|
| `Metrics` | Root `IMetrics` impl with `ConcurrentDictionary`-backed counters/meters | YES -- Counter and Meter values are accumulated |
| `Counter` | `ICounter` impl using `Interlocked` operations | YES |
| `Meter` | `IMeter` impl using `Interlocked` operations | YES |
| `MetricsContext` | `IMetricsContext` delegating to inner `Metrics` | YES (delegates) |
| `Histogram` | `IHistogram` -- no-op | No |
| `Timer` | `ITimer` -- no-op (missing `NewContext(Action<TimeSpan>)` overload) | No |
| `TimerContext` | `ITimerContext` -- no-op | No |

**Critical finding**: The `Metrics` class is NOT a pure no-op. Its `Counter` and `Meter` types actually accumulate values, and `GetCollectedMetrics()` returns a `MetricsSnapshot` with real data. This data is consumed by `VerifyMetrics` (in IntegrationTests.Shared) to assert correct message counts after integration test runs.

## Core library NoOp alternatives

`DotNetWorkQueue.Metrics.NoOp.MetricsNoOp` (in the core library) is a true no-op -- all counters/meters discard values, and `GetCollectedMetrics()` returns empty dictionaries. **It cannot replace IntegrationTests.Metrics without breaking all metric verification in integration tests.**

## Dependency graph

- **IntegrationTests.Metrics.csproj** targets `net48` only, references only `DotNetWorkQueue.csproj`
- **IntegrationTests.Shared.csproj** has a `ProjectReference` to IntegrationTests.Metrics (line 37)
- **InternalsVisibleForTests.cs** (line 25) grants internal access to `DotNetWorkQueue.IntegrationTests.Metrics`
- **DotNetWorkQueue.sln** has the project at line 16 with build configs at lines 150-161
- **DotNetWorkQueueNoTests.sln** does NOT reference this project

## Usage scope

- **1 explicit `using` statement**: `ProducerMethodMultipleDynamicShared.cs` line 9 (inside `#if NETFULL`)
- **~30 files** in IntegrationTests.Shared use `new Metrics.Metrics(...)` -- these resolve via namespace, not a using directive, because IntegrationTests.Shared already has a project reference
- **`VerifyMetrics.cs`** reads `MetricsSnapshot` returned by `metrics.GetCurrentMetrics()` (an extension method in `ExtensionMethods.cs` that calls `GetCollectedMetrics()`)
- **`ExtensionMethods.cs`** defines `GetCurrentMetrics()` extension on `IMetrics`

## Strategy

Since the types must actually track values, the correct approach is to **move the 7 source files into IntegrationTests.Shared** (as internal types) rather than replacing them with core NoOp types. This:
1. Eliminates the separate project entirely
2. Preserves all metric-tracking behavior
3. Requires no changes to the ~30 consumer files (they already resolve `Metrics.Metrics` via namespace)

The namespace can stay as `DotNetWorkQueue.IntegrationTests.Metrics` -- the files just move physically into IntegrationTests.Shared. No code changes needed in the 30+ consumer files since they already reference via the shared project.

Only `ProducerMethodMultipleDynamicShared.cs` has an explicit `using DotNetWorkQueue.IntegrationTests.Metrics;` which will continue to work since the namespace is preserved.

The `InternalsVisibleTo` entry can be removed because the `Metrics` class (and `MetricsContext`) are `public`/`internal` -- and once inside IntegrationTests.Shared, internal types are accessible without it. The only types marked `internal` in IntegrationTests.Metrics are: `Histogram`, `Timer`, `TimerContext`, `MetricsContext`. These are only instantiated from within the `Metrics` class itself, so moving them together preserves access.
