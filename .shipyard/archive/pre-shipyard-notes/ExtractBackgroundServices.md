# Plan: Extract Background Maintenance Tasks from Consumers

## Problem Statement

Today, every consumer queue instance runs its own background maintenance tasks — heartbeat reset, expired message cleanup, and error message cleanup. This creates several issues:

1. **Duplicate work**: If 5 consumers connect to the same queue, all 5 run the same maintenance monitors scanning the same tables
2. **No maintenance without consumers**: If all consumers are down, no maintenance runs — stale messages aren't reset, expired messages aren't cleaned up
3. **Tight coupling**: Consumer startup/shutdown is interleaved with monitor lifecycle, making both harder to reason about
4. **Dashboard opportunity**: The dashboard already has connections to every queue and could host maintenance centrally

## Goal

Extract the three maintenance monitors into a standalone `IQueueMaintenanceService` that can be hosted by:
- **The dashboard** (centralized, per-connection/queue)
- **The consumer** (opt-in, preserves current behavior for users who don't run the dashboard)
- **A standalone host** (e.g. a Windows Service or `IHostedService` in a worker process)

## Current Architecture

### What runs inside consumers today

```
ConsumerQueue.Start()
  └─> QueueMonitor.Start()            (or RedisQueueMonitor.Start() for Redis)
       ├─ HeartBeatMonitor             → IResetHeartBeat.Reset()               [timer-based]
       ├─ ClearExpiredMessagesMonitor  → IClearExpiredMessages.ClearMessages()  [timer-based]
       ├─ ClearErrorMessagesMonitor    → IClearErrorMessages.ClearMessages()    [timer-based]
       └─ RedisDelayedProcessingMonitor → MoveDelayedRecordsCommand            [timer-based, Redis only]
  └─> PrimaryWorker.Start()
       └─ Worker threads → IMessageProcessing.Handle()  [message dequeue loop]
```

### Key distinction: monitors vs. per-message heartbeat

- **Monitors** (extract these): Timer-based, scan the whole queue periodically, don't depend on which messages are being processed
- **Per-message heartbeat worker** (keep in consumer): `HeartBeatWorker` runs per-message while it's being processed — this MUST stay with the consumer because it's tied to the message processing lifecycle

### Transport-specific monitors

Redis overrides `IQueueMonitor` entirely with `RedisQueueMonitor`, which adds a **4th monitor**: `RedisDelayedProcessingMonitor`. This moves delayed/scheduled records from a Redis sorted set to the pending queue when their scheduled time arrives.

**No other transports** currently override `IQueueMonitor` — only Redis registers a custom implementation (in `RedisQueueInit.cs` line 117).

This means the design must support transport-specific monitors, not just the 3 core ones. The `IQueueMaintenanceService` abstraction needs to delegate to whatever `IQueueMonitor` the transport registered (which already includes any transport-specific monitors), rather than hardcoding the 3 core monitors.

### Relevant interfaces

| Interface | What it does | Transport-specific? |
|---|---|---|
| `IResetHeartBeat` | Find & reset stale messages | Yes — each transport implements |
| `IClearExpiredMessages` | Delete expired messages | Yes — each transport implements |
| `IClearErrorMessages` | Delete old error messages | Yes — each transport implements |
| `IDelayedProcessingMonitor` | Move delayed records to pending (Redis) | Yes — Redis only |
| `IQueueMonitor` | Composite of all monitors for a transport | Overridden by Redis (`RedisQueueMonitor`) |
| `IHeartBeatConfiguration` | Timing/enable config | No — core interface |
| `IMessageExpirationConfiguration` | Timing/enable config | No — core interface |
| `IMessageErrorConfiguration` | Timing/enable/age config | No — core interface |
| `BaseMonitor` | Timer infrastructure | No — core class |
| `QueueMonitor` | Default composite of 3 core monitors | No — core class |

### Key files

| File | Purpose |
|---|---|
| `DotNetWorkQueue/Queue/QueueMonitor.cs` | Default composite — starts/stops 3 core monitors |
| `DotNetWorkQueue/Queue/BaseMonitor.cs` | Timer-based monitor base class |
| `DotNetWorkQueue/Queue/HeartBeatMonitor.cs` | Wraps `IResetHeartBeat.Reset()` |
| `DotNetWorkQueue/Queue/ClearExpiredMessagesMonitor.cs` | Wraps `IClearExpiredMessages.ClearMessages()` |
| `DotNetWorkQueue/Queue/ClearErrorMessagesMonitor.cs` | Wraps `IClearErrorMessages.ClearMessages()` |
| `Transport.Redis/Basic/RedisQueueMonitor.cs` | Redis override — adds `IDelayedProcessingMonitor` as 4th monitor |
| `Transport.Redis/Basic/RedisDelayedProcessingMonitor.cs` | Moves delayed records to pending queue |
| `DotNetWorkQueue/Queue/ConsumerQueue.cs` | Calls `_queueMonitor.Start()` at line 119 |
| `DotNetWorkQueue/Queue/ConsumerQueueAsync.cs` | Calls `_queueMonitor.Start()` at line 124 |
| `DotNetWorkQueue/IoC/ComponentRegistration.cs` | Registers monitors and configs |
| `Transport.Redis/Basic/RedisQueueInit.cs` | Overrides `IQueueMonitor` → `RedisQueueMonitor` (line 117) |

---

## Proposed Design

### Phase 1: New abstraction — `IQueueMaintenanceService`

Create a new interface and default implementation in core:

```csharp
namespace DotNetWorkQueue
{
    /// <summary>
    /// Runs queue maintenance tasks (heartbeat reset, expiration cleanup, error cleanup)
    /// independently of consumer message processing.
    /// </summary>
    public interface IQueueMaintenanceService : IDisposable
    {
        /// <summary>
        /// Start maintenance for a specific queue.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop maintenance.
        /// </summary>
        void Stop();

        /// <summary>
        /// Whether the service is currently running.
        /// </summary>
        bool IsRunning { get; }
    }
}
```

The default implementation (`QueueMaintenanceService`) wraps the transport's `IQueueMonitor` — which already includes any transport-specific monitors (e.g. Redis's delayed processing monitor). This means we don't hardcode the 3 core monitors; we delegate to whatever the transport registered.

### Phase 2: Decouple monitors from consumer

Today `IQueueMonitor` (either `QueueMonitor` or `RedisQueueMonitor`) is resolved from the consumer's container and started directly. The change:

1. **`QueueMaintenanceService`** — new class, wraps the transport's `IQueueMonitor`, adds last-run tracking, can be started/stopped independently
2. **`ConsumerQueue` / `ConsumerQueueAsync`** — check `MaintenanceMode` before calling `_queueMonitor.Start()`:
   - `Consumer` mode: start monitors as today (no behavior change)
   - `External` mode: skip `_queueMonitor.Start()` entirely — per-message heartbeat worker is unaffected since it's started separately via `HeartBeatScheduler`

**Important**: We do NOT change `QueueMonitor` or `RedisQueueMonitor` themselves. They continue to work exactly as they do today. The `QueueMaintenanceService` wraps them when used externally (dashboard, hosted service), and the consumer uses them directly in `Consumer` mode. This minimizes risk.

### Phase 3: Configuration — who runs maintenance?

Add a new configuration property to control where maintenance runs:

```csharp
namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Controls where queue maintenance tasks execute.
    /// </summary>
    public enum MaintenanceMode
    {
        /// <summary>
        /// Each consumer runs its own maintenance monitors (current behavior, default).
        /// </summary>
        Consumer,

        /// <summary>
        /// Maintenance is hosted externally (dashboard, standalone service, etc.).
        /// Consumer does NOT run monitors.
        /// </summary>
        External
    }
}
```

This goes on `QueueConsumerConfiguration`:

```csharp
/// <summary>
/// Gets or sets where maintenance tasks run. Default is Consumer (backward compatible).
/// </summary>
public MaintenanceMode MaintenanceMode { get; set; } = MaintenanceMode.Consumer;
```

When `MaintenanceMode.External`, `QueueMonitor.Start()` becomes a no-op for the three maintenance monitors. The per-message heartbeat worker is unaffected.

### Phase 4: Dashboard hosting

The dashboard already has `QueueContainer<T>` and `CreateAdminContainer()` per queue. Add a maintenance service layer:

```csharp
// In DashboardOptions
public bool HostMaintenance { get; set; } = false;
```

When enabled, `DashboardApi.InitializeFromOptions()` creates a `IQueueMaintenanceService` per queue (via the admin container) and starts it. On dispose, it stops all maintenance services.

**Configuration in appsettings.json:**
```json
{
  "Dashboard": {
    "HostMaintenance": true,
    "MaintenanceIntervals": {
      "HeartBeatResetSeconds": 15,
      "ExpiredMessageCleanupSeconds": 30,
      "ErrorMessageCleanupSeconds": 86400,
      "ErrorMessageMaxAgeDays": 30
    }
  }
}
```

### Phase 5: Standalone hosted service (optional)

For users who want centralized maintenance without the dashboard, provide an `IHostedService` adapter:

```csharp
namespace DotNetWorkQueue.Hosting
{
    /// <summary>
    /// Hosts queue maintenance as a .NET Generic Host background service.
    /// </summary>
    public class QueueMaintenanceHostedService : BackgroundService
    {
        // Wraps IQueueMaintenanceService, starts on StartAsync, stops on StopAsync
    }
}
```

This could live in core or a new `DotNetWorkQueue.Hosting` package (depending on whether we want the `Microsoft.Extensions.Hosting` dependency in core).

---

## Implementation Steps

### Step 1: Create `IQueueMaintenanceService` and default implementation

**Files to create:**
- `DotNetWorkQueue/IQueueMaintenanceService.cs` — interface
- `DotNetWorkQueue/Queue/QueueMaintenanceService.cs` — implementation

The implementation wraps the transport's `IQueueMonitor` (which already includes all transport-specific monitors like Redis's delayed processing). It adds:
- `IsRunning` state tracking
- Last-run timestamps per monitor (for observability)
- Error isolation (a monitor failure doesn't crash the service)

It does NOT duplicate or replace `QueueMonitor`/`RedisQueueMonitor` — it delegates to them.

**Risk: Low** — new code, no existing behavior changes yet.

### Step 2: Add `MaintenanceMode` configuration

**Files to modify:**
- `DotNetWorkQueue/Configuration/QueueConsumerConfiguration.cs` — add `MaintenanceMode` property
- Create `DotNetWorkQueue/Configuration/MaintenanceMode.cs` — enum

**Risk: Low** — additive only, default is `Consumer` (backward compatible).

### Step 3: Consumer checks `MaintenanceMode` before starting monitors

**Files to modify:**
- `DotNetWorkQueue/Queue/ConsumerQueue.cs` — check `MaintenanceMode` before `_queueMonitor.Start()`
- `DotNetWorkQueue/Queue/ConsumerQueueAsync.cs` — same change

No changes to `QueueMonitor` or `RedisQueueMonitor` — they remain unchanged.

**Risk: Medium** — this is the behavioral change. Must ensure:
- Default `Consumer` mode is identical to today (monitors run in consumer)
- When `External`, monitors don't start but per-message heartbeat worker still works (it uses `HeartBeatScheduler`, which is started separately and is NOT part of `QueueMonitor`)
- Thread safety and disposal ordering preserved
- Method queue variants (`ConsumerMethodQueue`, `ConsumerMethodQueueAsync`) also need the same check since they delegate to the consumer queues internally

### Step 4: Register in IoC

**Files to modify:**
- `DotNetWorkQueue/IoC/ComponentRegistration.cs` — register `IQueueMaintenanceService` → `QueueMaintenanceService`

**Risk: Low** — just a registration addition.

### Step 5: Wire into `QueueContainer.CreateAdminContainer()`

**Files to modify:**
- `DotNetWorkQueue/QueueContainer.cs` — ensure `IQueueMaintenanceService` is resolvable from admin containers (for dashboard hosting)

**Risk: Low** — admin containers already resolve transport handlers; this adds one more.

### Step 6: Dashboard integration

**Files to modify:**
- `DotNetWorkQueue.Dashboard.Api/Configuration/DashboardOptions.cs` — add `HostMaintenance` and interval config
- `DotNetWorkQueue.Dashboard.Api/DashboardApi.cs` — create and manage `IQueueMaintenanceService` per queue
- `DotNetWorkQueue.Dashboard.Api/Controllers/QueuesController.cs` — optional: add endpoint to view maintenance status

**Risk: Medium** — the dashboard needs to correctly lifecycle the maintenance services (start on init, stop on dispose, handle errors gracefully). Must not block dashboard startup if a queue's transport is unreachable.

### Step 7: Add `IHostedService` adapter (optional, can defer)

**Files to create:**
- New project or in core: `QueueMaintenanceHostedService`

**Risk: Low** — thin adapter over the interface.

### Step 8: Tests

**Unit tests:**
- `QueueMaintenanceService` starts/stops monitors correctly
- `QueueMaintenanceService` tracks last-run timestamps
- Consumer skips `_queueMonitor.Start()` when `MaintenanceMode.External`
- Consumer starts `_queueMonitor` normally when `MaintenanceMode.Consumer`
- Per-message heartbeat worker still runs in `External` mode
- Dashboard creates/disposes maintenance services when `HostMaintenance = true`
- Dashboard does NOT create maintenance services when `HostMaintenance = false`

**Integration tests — Memory transport (no external services):**
- `Consumer` mode — stale messages are reset by consumer (existing behavior, verify not broken)
- `Consumer` mode — expired messages are cleaned up by consumer (existing behavior)
- `Consumer` mode — error messages are cleaned up by consumer (existing behavior)
- `External` mode — consumer does NOT reset stale messages (proves monitors aren't running)
- `External` mode — consumer does NOT clean expired messages
- `External` mode — `QueueMaintenanceService` started separately DOES reset stale messages
- `External` mode — `QueueMaintenanceService` started separately DOES clean expired messages
- `External` mode — `QueueMaintenanceService` started separately DOES clean error messages

**Integration tests — Dashboard-hosted (Memory transport via TestServer):**
- Dashboard with `HostMaintenance = true` — stale messages are reset
- Dashboard with `HostMaintenance = true` — expired messages are cleaned
- Dashboard with `HostMaintenance = false` — no maintenance runs
- Maintenance status endpoint returns correct `isRunning` and last-run timestamps

**Integration tests — Redis:**
- `External` mode — `QueueMaintenanceService` moves delayed records to pending (4th monitor)
- `Consumer` mode — delayed processing still works (existing behavior, verify not broken)

**Test approach:**
- Memory transport first (simplest, no external services, runs on CI)
- Adapt existing stale/expired/error test patterns — they already set up the scenarios, just need to split who runs maintenance
- Redis delayed processing tests only needed locally (requires Redis server)

---

## Migration Path for Users

1. **No action required** — default `MaintenanceMode.Consumer` preserves current behavior
2. **To use dashboard-hosted maintenance:**
   - Set `HostMaintenance = true` in dashboard config
   - Set `MaintenanceMode = MaintenanceMode.External` on consumer configurations
   - Consumers stop running their own monitors; dashboard takes over
3. **Gradual migration** — can run both simultaneously during transition (monitors are idempotent — duplicate resets/cleanups are safe, just wasteful)

## Resolved Questions

1. **Singleton per queue or per container?** → Per admin container (one per queue in the dashboard). The user can override behavior per queue via the container.

2. **Dashboard expose maintenance status via API?** → Yes, include in this work. Add endpoint and wire into UI.

3. **Add "last run" timestamps?** → Yes, high debugging value, small effort.

4. **`IHostedService` adapter — core or separate package?** → Defer. Easy to add later without another NuGet package decision now.

5. **Per-message heartbeat worker — any changes?** → No. `HeartBeatWorker` stays in the consumer regardless of `MaintenanceMode`. It uses `HeartBeatScheduler` (separate from `QueueMonitor`). Document that `MaintenanceMode.External` only affects the queue-wide monitors, not per-message heartbeat updates.

## Resolved Open Questions

1. **Future transports**: Any new transport that overrides `IQueueMonitor` to add custom monitors (like Redis did) will automatically be supported, since `QueueMaintenanceService` wraps the transport's `IQueueMonitor` rather than hardcoding specific monitors. No special handling needed. **Takeaway**: Consider adding developer/contributor docs in a future pass — the project doesn't have them today.

2. **Concurrent maintenance safety**: Monitors are already written to be multi-process safe, so running both consumer and dashboard maintenance simultaneously is just wasteful, not harmful. Document the recommended setup; no runtime detection needed.
