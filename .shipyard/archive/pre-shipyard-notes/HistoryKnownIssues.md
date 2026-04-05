# Message History — Known Issues

Found during integration testing of the history feature. All transports with saved configuration (SQLite, SQL Server, PostgreSQL) work correctly. These issues affect transports without persistent config or with config persistence bugs.

## 1. LiteDB in-memory mode doesn't persist transport options

**Symptom:** `EnableHistory = true` set during `CreateQueue()` comes back as `false` when loaded by the options factory in a new container. History table is created but the bridge in `SetDefaultsIfNeeded` sees `EnableHistory = false`, so decorators don't record events.

**Affects:** LiteDB Memory and Shared connection types. Direct (file-based) mode likely works but has file locking issues in the test cleanup.

**Root cause:** LiteDB's `GetQueueOptionsQuery` handler doesn't correctly save/load newly added properties in in-memory mode. The options are serialized to the Configuration collection during `CreateQueue()`, but when deserialized in a new container, newly added properties (like `EnableHistory`) default to `false`.

**Fix:** Investigate LiteDB's options serialization/deserialization. May need explicit property handling or a migration path for new options.

## 2. Redis doesn't support message history

**Symptom:** Redis has no persistent queue configuration. `EnableHistory` is hardcoded to `false` in `RedisBaseTransportOptions`. History handlers were removed.

**Root cause:** Redis doesn't save/load transport options — everything is hardcoded. There's no `SetDefaultsIfNeeded` bridge because there are no saved options to bridge from.

**Fix:** Add persistent queue configuration to Redis (stored in a Redis hash per queue). Once Redis can save/load transport options, history handlers can be re-added and the bridge will work.

## 3. Memory transport doesn't support message history

**Symptom:** Same as Redis — `EnableHistory` is hardcoded to `false`, history handlers were removed.

**Root cause:** Memory transport has no persistent configuration. Transport options are compile-time defaults.

**Fix:** Add per-queue configuration to the Memory transport (stored in the static `DataStorage`). This is simpler than Redis since it's all in-process.

## 4. Dashboard History tab shows empty for unsupported transports

**Symptom:** The History tab appears in the dashboard UI for all queues, even Redis and Memory where history isn't supported. Shows empty results.

**Fix:** Either hide the tab when the transport doesn't support history, or show a message explaining history isn't available. Requires the API to expose whether history is supported for a given queue (could add `EnableHistory` to the features response).

## 5. History query uses in-memory pagination

**Symptom:** `QueryMessageHistoryHandler.Get()` uses C# skip/take instead of SQL-level pagination. Works correctly but scans the full result set.

**Root cause:** `LIMIT...OFFSET` is SQLite/PostgreSQL-only; `OFFSET...FETCH` is SQL Server-only. The shared relational handler can't use either without transport-specific SQL.

**Fix:** Move pagination queries to transport-specific command string caches (same pattern as existing dashboard message list queries). Each transport provides its own pagination SQL.

## 6. LiteDB file-based (Direct) mode has test cleanup issues

**Symptom:** LiteDB Direct connection type causes `IOException: file is being used by another process` during test cleanup. The admin container holds the database file open.

**Root cause:** The test creates an admin container to verify history records. The admin container opens the LiteDB file, and the test cleanup tries to delete it before the container is fully disposed.

**Fix:** Ensure admin container is disposed before queue removal, or add a retry/delay in test cleanup.
