# Dashboard Metrics Display — Implementation Plan

## Goal

Display queue metrics from the in-process `IMetrics` / `MetricsSnapshot` in the Dashboard API and UI. No external metrics backend required — the dashboard reads snapshots directly from admin containers.

## Current State

- Metrics are emitted via `System.Diagnostics.Metrics` (`MetricsNet` in `DotNetWorkQueue.Metrics.Net`)
- `IMetrics.GetCollectedMetrics()` returns a `MetricsSnapshot` with `Counters` (Dict<string, long>) and `Meters` (Dict<string, long>)
- `MetricsNet` is registered as the default `IMetrics` in `ComponentRegistration.cs`
- Each queue gets its own `IMetrics` context (keyed by queue name)
- Admin containers (`CreateAdminContainer`) have `IMetrics` available
- Timers and histograms are NOT currently captured in `MetricsSnapshot` — only counters and meters

## Problem: Admin Container Metrics Are Empty

The admin container creates a fresh `IMetrics` instance. It does not share the `IMetrics` from a running consumer/producer. So `GetCollectedMetrics()` on an admin container returns empty data.

For the dashboard to read metrics, we need one of:

**Option A: Share IMetrics across containers (same process)**
When the dashboard is embedded in the same process as consumers (e.g. a worker service that also hosts the dashboard API), wire up a shared `IMetrics` registry keyed by queue name. Consumers register their `IMetrics` instance; the dashboard reads from it.

**Option B: Expose metrics via the consumer, dashboard polls** — REJECTED.
Two problems: (1) HTTP polling from every consumer adds latency and network chatter, (2) producers don't report in, so you only get half the picture. For cross-process metrics, users should use OpenTelemetry exporters into a time-series database (Prometheus, etc.) — that's purpose-built for this.

**Option C: Dashboard hosts its own consumer-like metrics collection**
When `HostMaintenance` is true, the dashboard already runs monitors for the queue. We could also attach metric decorators to those monitors so the dashboard's own `IMetrics` instance collects maintenance-related metrics (heartbeat resets, expired message cleanup counts).

### Recommendation

Go with **Option A** for embedded scenarios (most common) and **Option C** as a bonus for `HostMaintenance` queues. Cross-process metrics are out of scope — users who run consumers on separate machines already have (or should have) OpenTelemetry exporters feeding Grafana/Prometheus.

## Implementation Steps

### Step 1: MetricsRegistry — Share IMetrics Across Containers

Create a static or singleton registry that consumers/producers register their `IMetrics` into, keyed by queue name + connection string.

**New file:** `Source/DotNetWorkQueue/Metrics/MetricsRegistry.cs`

```csharp
public class MetricsRegistry : IMetricsRegistry
{
    private static readonly ConcurrentDictionary<string, IMetrics> _registry = new();

    public void Register(string queueName, string connectionString, IMetrics metrics);
    public void Unregister(string queueName, string connectionString);
    public IMetrics Get(string queueName, string connectionString);
    public MetricsSnapshot GetSnapshot(string queueName, string connectionString);
    public IReadOnlyDictionary<string, IMetrics> GetAll();
}
```

**Key:** `$"{queueName}|{connectionString}"` (same pattern as `QueueConnection`).

Register in `ComponentRegistration.cs` as singleton. Consumer/producer queues call `Register()` on start, `Unregister()` on dispose.

### Step 2: Wire Registration Into Consumer/Producer Queues

Modify these files to register/unregister metrics:

- `ConsumerQueue.Start()` — call `_metricsRegistry.Register(...)` with the queue's `IMetrics`
- `ConsumerQueue.Dispose()` — call `_metricsRegistry.Unregister(...)`
- Same for `ConsumerQueueAsync`, `ProducerQueue`, `ProducerMethodQueue`
- The `IMetrics` instance is already available in these classes via DI

### Step 3: Dashboard API — Metrics Endpoint

**New endpoint:** `GET /api/v1/dashboard/queues/{queueId}/metrics`

**Response model:** `MetricsResponse`

```csharp
public class MetricsResponse
{
    public bool Available { get; set; }          // false if no consumer has registered metrics
    public DateTime? SnapshotUtc { get; set; }   // when the snapshot was taken
    public Dictionary<string, long> Counters { get; set; }
    public Dictionary<string, long> Meters { get; set; }
}
```

**Logic in `DashboardService`:**
1. Look up the queue's `QueueConnection` (name + connection string) from `DashboardQueueInfo`
2. Try `MetricsRegistry.GetSnapshot(queueName, connectionString)`
3. If found, return the snapshot. If not, return `Available = false`

For `HostMaintenance` queues, also try the admin container's `IMetrics` as fallback (maintenance monitors will have recorded heartbeat reset / expiration cleanup counts).

### Step 4: Categorize Metrics for Display

The raw metric names are verbose (`queueName.contextName.HandleTimer`, etc.). Group them for the UI.

**Categories:**

| Category | Metrics | Description |
|----------|---------|-------------|
| **Message Processing** | `HandleTimer`, `HandleCompiledMethodTimer`, `HandleDynamicMethodTimer` | Time spent in user message handler |
| **Send** | `SendTimer`, `SendAsyncTimer`, `SendBatchTimer`, `SendMessagesMeter`, `SendMessagesErrorMeter` | Producer throughput and errors |
| **Serialization** | `ConvertMessageToBytesTimer`, `ConvertBytesToMessageTimer`, `ConvertToBytesHistogram` | Serialization overhead |
| **Heartbeat** | `SendTimer` (heartbeat context), `ResetTimer`, `ResetCounter` | Heartbeat send/reset activity |
| **Expiration** | `ClearMessages.ResetTimer`, `ClearMessages.ResetCounter` | Expired message cleanup |
| **Errors** | `MessageFailedProcessingErrorMeter`, `MessageFailedProcessingRetryMeter`, `PoisonHandleMeter`, `RollbackCounter` | Error rates |
| **LINQ** | `CompileActionTimer`, `LinqActionCacheHitCounter`, `LinqActionCacheMissCounter` | LINQ compilation stats |
| **Interceptors** | `BytesToMessageTimer`, `MessageToBytesTimer`, `MessageToBytesDeltaHistogram` | Interceptor processing |

Strip the queue name prefix before displaying. The context name (e.g. "heartbeat", "serialization") is already embedded in the metric name.

### Step 5: Dashboard UI — Metrics Tab

Add a "Metrics" tab to the queue detail page in `DotNetWorkQueue.Dashboard.Ui`.

**Layout:**
- Summary cards at top: Messages Sent (meter), Messages Handled (counter), Errors (meter), Heartbeat Resets (counter)
- Grouped table below with all metrics by category
- Auto-refresh every 10 seconds (configurable)
- "No metrics available" message when `Available = false` (consumer not running or not in same process)

**No charts in v1.** Counters and meters are cumulative totals, not time-series. Displaying current values in a table is honest and useful. Time-series charting would require storing snapshots over time, which is a bigger feature (and is what Grafana/Prometheus is for).

### Step 6: Expand MetricsSnapshot (Optional)

Currently `MetricsSnapshot` only captures counters and meters. Timers and histograms are lost.

Consider adding to `MetricsSnapshot`:
- `Timers`: Dictionary<string, TimerSnapshot> (count, mean, min, max, p95, p99)
- `Histograms`: Dictionary<string, HistogramSnapshot> (count, mean, min, max)

This requires changes to `MetricsNet`, `TimerNet`, `HistogramNet` to track running statistics. This is useful but not required for v1 — counters and meters cover the most important operational data. Can be a follow-up.

## Files to Create/Modify

### New Files
- `Source/DotNetWorkQueue/Metrics/IMetricsRegistry.cs` — interface
- `Source/DotNetWorkQueue/Metrics/MetricsRegistry.cs` — implementation
- `Source/DotNetWorkQueue.Dashboard.Api/Models/MetricsResponse.cs` — response model
- `Source/DotNetWorkQueue.Dashboard.Ui/Pages/QueueMetrics.razor` — (or add to existing queue detail page)

### Modified Files
- `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs` — register `IMetricsRegistry`
- `Source/DotNetWorkQueue/Queue/ConsumerQueue.cs` — register/unregister metrics
- `Source/DotNetWorkQueue/Queue/ConsumerQueueAsync.cs` — register/unregister metrics
- `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` — register metrics
- `Source/DotNetWorkQueue.Dashboard.Api/Services/IDashboardService.cs` — add `GetMetricsAsync`
- `Source/DotNetWorkQueue.Dashboard.Api/Services/DashboardService.cs` — implement
- `Source/DotNetWorkQueue.Dashboard.Api/Controllers/QueuesController.cs` — add endpoint

## Test Plan

- **Unit tests:** `MetricsRegistry` — register, unregister, get, concurrent access
- **Unit tests:** `DashboardService.GetMetrics` — available vs unavailable, snapshot content
- **Integration tests:** SQLite in-memory — start consumer, verify metrics endpoint returns data with `Available = true`
- **Integration tests:** Verify metrics endpoint returns `Available = false` when no consumer is running

## Out of Scope (Future)

- Time-series storage and charting (use Grafana for that)
- Cross-process metrics collection (Option B above)
- Timer/histogram detail in `MetricsSnapshot` (Step 6)
- Alerting or threshold configuration
