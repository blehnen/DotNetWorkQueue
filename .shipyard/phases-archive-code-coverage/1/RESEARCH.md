# Phase 1 Research: Quick Wins

## Task A: ObjectPool Dead Code Investigation

### Verdict: DEAD CODE -- Delete

**Files:**
- `Source/DotNetWorkQueue/Cache/ObjectPool.cs` (class implementation, 68 lines)
- `Source/DotNetWorkQueue/IObjectPool.cs` (interface)
- `Source/DotNetWorkQueue/IPooledObject.cs` (interface)

**Evidence:**
- Grep for `ObjectPool` across all non-test `.cs` files found ZERO references outside those 3 files
- No DI registration in any `ComponentRegistration.cs` or transport init class
- No transport references the class
- Suspected leftover from dynamic LINQ removal

**Action:** Delete all 3 files. Verify clean build.

## Task B: In-Memory Trace Exporter

### Current Trace Infrastructure

**ActivitySource creation (core):**
- `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs:287-289`
- Name: `"dotnetworkqueue.instrumentationlibrary"`
- Registered as singleton in SimpleInjector container

**Integration test override:**
- `Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs:155-181`
- `CreateTrace(name)` creates a NEW `ActivitySource` with name `"dotnetworkqueue-{testType}"` (e.g., `"dotnetworkqueue-producer"`)
- This is registered via `.RegisterNonScopedSingleton(trace)` which REPLACES the core's default ActivitySource
- So the ActivitySource name that matters in integration tests is `"dotnetworkqueue-{testType}"`

**Current trace settings:**
- `Source/DotNetWorkQueue.IntegrationTests.Shared/TraceSettings.cs`
- `Enabled` is hardcoded to `false`
- When false, no OTLP exporter is created
- But critically, there's NEVER an `ActivityListener` -- even when Enabled=true, only an OTLP exporter is configured via `Sdk.CreateTracerProviderBuilder()`, which internally creates a listener

**Why TraceExtensions are at 0%:**
- Without an `ActivityListener`, `ActivitySource.StartActivity()` returns `null`
- The trace decorators (15+ in core) check for null activity and short-circuit
- TraceExtensions methods operate on `Activity` objects -- they never get called because activities are never created

### TraceExtensions Files (all at 0% coverage)

1. `Source/DotNetWorkQueue.Transport.Shared/Trace/TraceExtensions.cs`
2. `Source/DotNetWorkQueue.Transport.SqlServer/Trace/TraceExtensions.cs`
3. `Source/DotNetWorkQueue.Transport.PostgreSQL/Trace/TraceExtensions.cs`
4. `Source/DotNetWorkQueue.Transport.SQLite/Trace/TraceExtensions.cs`
5. `Source/DotNetWorkQueue.Transport.Redis/Trace/TraceExtensions.cs`
6. `Source/DotNetWorkQueue.Transport.LiteDB/Trace/TraceExtensions.cs`

**Pattern:** Each TraceExtensions file adds transport-specific tags to Activity spans (delay, expiration, priority from SendMessageCommand). The SqlServer one is 36 lines of tag-setting logic.

### Solution: ActivityListener in Shared Setup

**Where to add:** `SharedSetup.CreateTrace(string name)` in `IntegrationTests.Shared/SharedSetup.cs`

**How:**
1. Always create an `ActivityListener` that samples ALL activities from the test's ActivitySource
2. Store collected activities in a thread-safe collection (e.g., `ConcurrentBag<Activity>`)
3. The `ActivitySourceWrapper` already implements `IDisposable` -- extend it to hold the listener and collected activities
4. The listener ensures `StartActivity()` returns non-null, so trace decorators execute fully and TraceExtensions code paths are exercised
5. Tests can then assert on collected activities (span names, tags, parent-child relationships)

**ActivityListener setup pattern:**
```csharp
var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == traceName,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
    ActivityStarted = activity => collectedActivities.Add(activity),
};
ActivitySource.AddActivityListener(listener);
```

**Key insight:** The `Sdk.CreateTracerProviderBuilder()` path (when `Enabled=true`) does set up a listener internally. But we want an ALWAYS-ON in-memory listener that doesn't depend on `TraceSettings.Enabled` and doesn't need network access.

### Per-Transport AssemblyInit Files

Integration test projects have `AssemblyInit.cs` files but they don't configure tracing -- that's handled by `SharedSetup.CreateTrace()` which is called per-test. No changes needed to AssemblyInit files.
