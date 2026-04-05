1. ~~Display metrics from metrics backend in dashboard.ui (via dashboard.api if needed)~~ **SKIPPED** — metrics are already emitted via `System.Diagnostics.Metrics` and users can export them with OpenTelemetry into Prometheus/Grafana. In-dashboard display would only show cumulative totals (no time-series), which isn't useful enough to justify the complexity. Plan exists at `Source/DashboardMetrics.md` if revisited later.
2. Add the concept of message history to dotnetworkqueue and the dashboard
   1. I implemented this at my employeer as a add-on to SQLite. I don't own that code and so cannot provide it for samples.

Here is the analysis of the added features. We would like to make this standard. Note that the extra feaatures used SQLlite as the transport; job logging used LiteDB.
We want to cherry pick features in all transports.

I think we should discuss what makes sense to add stanard out of this list, as some features don't really apply (maintenance window violations).


Here's the comprehensive list of features added on top of DotNetWorkQueue in this project:

  ---
  Extra Features Built on DotNetWorkQueue                                                                                                       
  1. Dual-Database Message History                                                                                                              
  - Separate SQLite history databases (HistoryQueue.db and HistoryTask.db) that mirror completed/historical entries
  - Schema versioning with migrations (currently v2)
  - Status tracking: Waiting, Processing, Complete, Error, Deleted, Canceled
  - Per-run tracking with start/end times, exception text, and return codes

  1. Per-Message Job Logging

  - Each job gets its own dedicated LiteDB file (Log{JobId}.db)
    - This was done to threading issues - standard code needs to be a bit more generic...
  - Serilog integration with structured logging (level, timestamp, message, source context)
  - Automatic logger creation per job execution
  - Logs capture cancellation requests (including who requested), deletion events, occurrence failures, and maintenance window violations

  1. Custom Extended Metadata on Messages (NOTE - implementation specific - skip?)

  - JobType / JobTypeName — categorizes jobs (General vs System, "ReleasePK", "PkMover", etc.)
  - UserId — tracks who submitted the job
  - IgnoreJobWindow — flag to bypass scheduling constraints
  - RecurringJobId — links occurrences to parent recurring jobs

  4. Recurring Jobs Framework (NOTE - implementation specific - skip?)

  - Full recurring job definitions persisted in the queue DB
  - Recurrence patterns: None, Daily, Weekly with configurable frequency
  - Multi-day weekly scheduling (e.g., Mon/Wed/Fri)
  - Automatic future occurrence pre-generation via maintenance task
  - Soft-delete support (Active / Deletion Pending)
  - Input model versioning for safe schema evolution

  5. Job Notification Queue (NOTE - implementation specific - skip?)

  - Separate SQLite queue (JobNotification.db) for post-completion alerts
  - Captures last 10 log entries from job execution
  - Email notifications with job status and summary
  - Exponential backoff retry (1s through 7400s)

  6. Job Cancellation Support - might be worth adding canceling a running message to the dashboard? Message implementation would need to respond, it could ignore

  - In-memory tracking of running jobs via ConcurrentDictionary
  - Interactive cancellation requests with username logging
  - Distinguishes user-requested deletions from system-initiated ones

  7. Dual Queue Architecture (Serial + Concurrent) (SKIP)

  - Serial queue limited to 1 job at a time
  - Concurrent queue limited to processor count
  - Route-based filtering on the same underlying database

  8. Job History Purging 

  - Scheduled task to auto-purge old history records
  - Configurable retention period (minimum 1 day)
  - Cleans up associated LiteDB log files

  9. Maintenance Window Scheduling (Skip)

  - Configurable execution windows per job type
  - Override flag (IgnoreJobScheduleWindow)
  - Logs warnings and aborts jobs outside their window

  10. Follow-up Job Support (skip)

  - Jobs can spawn child/follow-up jobs on completion
  - Child jobs inherit recurring job context from parent
  - Can specify different concurrency route

  11. Correlation ID & Distributed Tracing

  - OpenTracing integration with span creation per job
  - Tags spans with job name, ID, and status
  - Errors logged to trace spans

  12. App Metrics Integration

  - Timer metrics per job type and execution context
  - Custom tagging (user ID, etc.)

  13. Input Model Versioning

  - Each recurring job stores an InputModelVersion
  - Supports breaking changes in job input schemas across 10+ job types

  14. Database Type Abstraction

  - DatabaseTypes enum across Queue, HistoryQueue, Task, HistoryTask, JobNotification
  - Database-agnostic table naming via TableNames class
  - Schema versioning and migration support

  ---
  These features collectively add a production-grade audit trail, per-job observability, flexible scheduling, and operational safety layer on
  top of the base DotNetWorkQueue library. If you're looking to standardize these, the big-ticket items are probably message history,
  per-message logging, recurring jobs, notifications, and the extended metadata columns.

3. ~~I think we should consider moving all 'background' jobs out of the consumers (i.e. reset heartbeat, delete expired messages, etc) and into a dedicated services type class. That class could then be hosted by the dashboard (per connection /queue) or the user could inject/configure it in the consumers to keep the same behavior (if they don't want to run dashboard.api).~~ **DONE (v0.9.5, PR #62)** — `MaintenanceMode.External` on consumers, `IQueueMaintenanceService` in core, `HostMaintenance` in Dashboard API.

4. ~~Consider allowing SQLite db's to be created with WAL mode for the queue. This would be a creation setting; I think it should default to true. I've noticed that the queue performs better in this mode; all integration tests already use WAL mode.~~ **DONE (v0.9.5)** — `EnableWalMode` on `SqLiteMessageQueueTransportOptions`, defaults to `true`. Set during `CreateQueue()` for file-based databases.