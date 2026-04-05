# Message History — Implementation Plan

## Goal

Track the lifecycle of every message after it leaves the queue: when it was processed, how long it took, whether it succeeded or failed, the exception if any. This gives users an audit trail and lets the dashboard show historical data beyond what's currently in the queue tables.

Today, once a message is committed (success) or deleted, it's gone. Error messages persist in the error table, but successful messages vanish. Users have no way to answer "what happened to message X last Tuesday?"

## What to cherry-pick from the employer implementation

From the roadmap feature list, these are worth standardizing:

| Feature | Include? | Reasoning |
|---------|----------|-----------|
| Message history table | Yes | Core feature — the whole point |
| Status tracking (Waiting/Processing/Complete/Error/Deleted/Canceled) | Yes | Needed for meaningful history |
| Per-run tracking (start/end times, exception, return codes) | Yes | Makes history useful |
| History purging | Yes | Without it, history grows forever |
| Job cancellation support | Maybe later | Useful but separate concern — can layer on top |
| Per-message job logging | No | Too implementation-specific (LiteDB per-job files) |
| Custom extended metadata | No | Implementation-specific |
| Recurring jobs framework | No | Already have Schyntax scheduler |
| Job notification queue | No | Implementation-specific |
| Maintenance window scheduling | No | Implementation-specific |
| Follow-up job support | No | Implementation-specific |

## Design Decisions

### Where does history live?

**Option A: Same database, separate table(s)**
History table(s) live alongside the queue tables in the same database/connection. Simplest for transactional consistency — the commit/error write and the history write can share a transaction.

**Option B: Separate database**
The employer implementation used separate SQLite databases (HistoryQueue.db, HistoryTask.db). Keeps queue performance isolated from history growth. Adds connection management complexity.

**Recommendation: Option A.** Keep it simple. The history table is append-only and won't interfere with queue operations. If performance becomes an issue on a specific transport, users can partition or archive — but that's an optimization, not a design requirement.

### What triggers a history write?

History records are created at message lifecycle transitions:

1. **Message enqueued** — record created with status `Enqueued`, timestamp
2. **Message dequeued for processing** — update to `Processing`, start time
3. **Message committed (success)** — update to `Complete`, end time, duration
4. **Message errored** — update to `Error`, end time, exception text
5. **Message deleted** — update to `Deleted`, timestamp
6. **Message expired** — update to `Expired`, timestamp
7. **Message rolled back** — update retry count (status stays `Enqueued` for retry, or `Error` if max retries)

### History record schema

```
MessageHistory
├── HistoryId         (PK, auto-increment or GUID depending on transport)
├── QueueId           (the message's queue ID — string, same as dashboard)
├── CorrelationId     (from message headers, for cross-system tracing)
├── Status            (int: 0=Enqueued, 1=Processing, 2=Complete, 3=Error, 4=Deleted, 5=Expired)
├── EnqueuedUtc       (when the message was added to the queue)
├── StartedUtc        (when processing began — null if never dequeued)
├── CompletedUtc      (when processing finished — success, error, or delete)
├── DurationMs        (CompletedUtc - StartedUtc in milliseconds, null if not completed)
├── ExceptionText     (truncated exception for error status, null otherwise)
├── RetryCount        (number of times this message was retried before final status)
├── Route             (message route, if routing is enabled)
├── MessageType       (body type name from Queue-MessageBodyType header, if available)
├── Body              (serialized body bytes — nullable, only populated when StoreBody=true)
└── Headers           (serialized headers bytes — nullable, only populated when StoreBody=true)
```

This is a single table, not dual (HistoryQueue + HistoryTask from the employer implementation). We don't need the task/queue split because DotNetWorkQueue doesn't distinguish between tasks and queue items at the library level.

### How to hook into the message lifecycle

The existing decorator pattern is the right approach. Each lifecycle event already has an interface with logging/metrics/trace decorators:

| Event | Interface | Hook |
|-------|-----------|------|
| Enqueue | `ISendMessages` | After successful send, write Enqueued record |
| Dequeue | `IReceiveMessages` | After successful receive, write/update to Processing |
| Commit | `ICommitMessage` | After commit, update to Complete |
| Error | `IReceiveMessagesError` | After error handling, update to Error |
| Rollback | `IRollbackMessage` | After rollback, increment retry count |
| Delete | Transport-specific delete handlers | Update to Deleted |
| Expire | `IClearExpiredMessages` | Update to Expired |

New decorator: `IHistoryDecorator` or individual decorators per interface (consistent with existing pattern).

### Transport implementation

Each transport needs:

1. **Table creation** — add history table in `CreateQueueTablesAndSaveConfiguration`
2. **History write handler** — `ICommandHandler<WriteHistoryCommand>` per transport
3. **History query handler** — `IQueryHandlerAsync<GetHistoryQuery, IReadOnlyList<MessageHistoryRecord>>` for dashboard
4. **History purge handler** — `ICommandHandlerWithOutput<PurgeHistoryCommand, long>` for cleanup

The relational transports (SQL Server, PostgreSQL, SQLite) share most of the SQL via `Transport.RelationalDatabase`. Redis and LiteDB need transport-specific implementations. Memory transport gets an in-memory list.

### Configuration

```csharp
// On QueueConsumerConfiguration or a new IHistoryConfiguration
public interface IHistoryConfiguration
{
    bool Enabled { get; set; }                    // default: false (opt-in)
    int RetentionDays { get; set; }               // default: 30
    int MaxExceptionLength { get; set; }          // default: 4000 (truncate long stack traces)
    bool StoreBody { get; set; }                   // default: false (opt-in, increases storage)
    bool TrackEnqueue { get; set; }               // default: true
    bool TrackProcessing { get; set; }            // default: true
    bool TrackComplete { get; set; }              // default: true
    bool TrackError { get; set; }                 // default: true
    bool TrackDelete { get; set; }                // default: true
    bool TrackExpire { get; set; }                // default: true
}
```

Opt-in by default because not everyone wants the write overhead.

### History purging

Add a new monitor (like `ClearExpiredMessagesMonitor`) that periodically deletes history records older than `RetentionDays`. Runs as part of the maintenance monitors — works with both `MaintenanceMode.Consumer` and `MaintenanceMode.External`.

### Dashboard integration

**Dashboard API:**
- `GET /api/v1/dashboard/queues/{queueId}/history?pageIndex=0&pageSize=25&status=2` — paged history with optional status filter
- `GET /api/v1/dashboard/queues/{queueId}/history/{messageId}` — full history for a specific message (all status transitions)
- `GET /api/v1/dashboard/queues/{queueId}/history/stats` — summary: total processed, avg duration, error rate over last N hours
- `DELETE /api/v1/dashboard/queues/{queueId}/history` — manual purge

**Dashboard UI:**
- New "History" tab on queue detail page
- Table with columns: Message ID, Status, Enqueued, Duration, Type, Route
- Click to expand: full timeline of status transitions, exception text
- Filter by status, date range
- Summary stats at top: processed/hour, avg latency, error %

### Message ID logging scope

As a companion to history, push the message ID into the `ILogger` scope during handler execution. This lets users correlate their own application logs with specific messages without any storage cost on our side.

In the message handler decorator (`IMessageHandlerDecorator` / `IMessageHandlerAsyncDecorator`), wrap the handler call with:

```csharp
using (_log.BeginScope(new Dictionary<string, object> { ["MessageId"] = context.MessageId }))
{
    _handler.Handle(context, message);
}
```

Any logging the user does inside their handler automatically carries the message ID. Users with structured logging (Serilog, NLog) can then query Seq/Elasticsearch/Loki by message ID to get per-message logs without us storing anything.

This is zero-overhead (no storage, no config) and always-on — not gated by `IHistoryConfiguration.Enabled`.

### Metrics

New metrics when history is enabled:
- `dotnetworkqueue.{queue}.History.WriteTimer` — time to write a history record
- `dotnetworkqueue.{queue}.History.PurgeCounter` — records purged by retention cleanup

## Implementation Phases

### Phase 1: Core infrastructure
- `IHistoryConfiguration` interface + `HistoryConfiguration` class
- `MessageHistoryStatus` enum
- `MessageHistoryRecord` model
- `IWriteMessageHistory` interface
- `IQueryMessageHistory` interface
- `IPurgeMessageHistory` interface
- Register in `ComponentRegistration.cs`
- History configuration on `QueueConsumerConfiguration` (or separate)

### Phase 2: Relational transport implementation
- History table DDL for SQL Server, PostgreSQL, SQLite
- Add to `CreateQueueTablesAndSaveConfiguration` (conditional on config)
- `WriteMessageHistoryCommandHandler` in `Transport.RelationalDatabase`
- `GetMessageHistoryQueryHandler`
- `PurgeMessageHistoryCommandHandler`

### Phase 3: Lifecycle decorators
- `SendMessagesHistoryDecorator` — writes Enqueued record
- `ReceiveMessagesHistoryDecorator` — updates to Processing
- `CommitMessageHistoryDecorator` — updates to Complete
- `ReceiveMessagesErrorHistoryDecorator` — updates to Error
- `RollbackMessageHistoryDecorator` — increments retry count
- `ClearExpiredMessagesHistoryDecorator` — updates to Expired
- Register decorators conditionally (only when history is enabled)

### Phase 4: Redis + LiteDB + Memory transport implementations
- Redis: history in a sorted set or hash per queue
- LiteDB: history collection
- Memory: `ConcurrentDictionary<string, List<MessageHistoryRecord>>`

### Phase 5: History purge monitor
- `ClearHistoryMonitor` : `BaseMonitor` — periodic purge based on retention
- Add to `QueueMonitor` (conditional on history enabled)
- `IHistoryMonitorTimespan` for configurable interval

### Phase 6: Dashboard API + UI
- History query/command handlers per transport (dashboard pattern)
- API endpoints on `QueuesController`
- Response models
- Dashboard UI: History tab, timeline view

### Phase 7: Tests
- Unit tests for decorators, configuration, purge logic
- Integration tests across all transports (SQL Server, PostgreSQL, SQLite, Redis, LiteDB, Memory) — CI runs them all
- Dashboard API integration tests for history endpoints

## Files to create (estimated)

**Core library (~10 files):**
- `IHistoryConfiguration.cs`, `HistoryConfiguration.cs`
- `MessageHistoryStatus.cs` (enum)
- `MessageHistoryRecord.cs` (model)
- `IWriteMessageHistory.cs`, `IPurgeMessageHistory.cs`, `IQueryMessageHistory.cs`
- History decorators (6 files, one per lifecycle event)
- `ClearHistoryMonitor.cs`

**Transport.RelationalDatabase (~5 files):**
- History table DDL
- Write/query/purge command handlers
- History-specific queries

**Per transport (Redis, LiteDB, Memory — ~3-4 files each):**
- Transport-specific history handlers

**Dashboard.Api (~3 files):**
- History response models
- Controller endpoints
- Service layer methods

**Dashboard.Ui (~1-2 files):**
- History tab component

## Future: Per-Message Cancellation

Not part of this implementation — deferred to a follow-up after message history ships.

**Design sketch:**
- `ConcurrentDictionary<string, CancellationTokenSource>` in the consumer, keyed by message ID
- Entry created at dequeue, removed at commit/error
- New `ICancelRunningMessage` interface registered in the container — dashboard API (in-process only) calls it to signal cancellation
- Add `workerNotification.MessageCancellation` token alongside existing `WorkerStopping` — linked so either fires the handler's token
- Best-effort: if the message finishes before the cancel fires, the token goes unused
- User's handler must check the token — cancellation is cooperative, not forced
- Dashboard UI: "Cancel" button on messages in Processing status
- ~5-6 new files, independent of history

## Resolved Questions

1. **Should history track individual retries as separate records, or update a single record?** DECIDED: Single record per message with retry count and last exception.

2. **Should the history table include the message body?** DECIDED: Configurable via `IHistoryConfiguration.StoreBody`, default `false`.

3. **Should history be enabled per-queue or globally?** DECIDED: Per-queue via `IHistoryConfiguration` on the consumer/producer configuration.

4. **How does this interact with `MaintenanceMode.External`?** DECIDED: History purge monitor follows the same pattern as other monitors — runs in consumer or externally. No special handling.

5. **Should the history write be synchronous or async?** DECIDED: Best-effort — log the error, don't fail the message. History is observability, not correctness.

6. **Per-message job logging?** DECIDED: Not storing logs in history — too much data. Instead, push message ID into `ILogger` scope during handler execution so users can correlate their own logs by message ID in their existing log infrastructure (Seq, Elasticsearch, Loki, etc.). Zero storage cost, always-on.

7. **Per-message cancellation?** DECIDED: Deferred to a follow-up after history ships. Design sketch included above.

8. **Integration test coverage?** DECIDED: All 6 transports (SQL Server, PostgreSQL, SQLite, Redis, LiteDB, Memory). CI runs them all.
