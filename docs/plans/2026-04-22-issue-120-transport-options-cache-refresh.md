# Transport Options Factory Cache Refresh — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Issue:** [#120](https://github.com/blehnen/DotNetWorkQueue/issues/120)

**Goal:** When a `*MessageQueueTransportOptionsFactory.Create()` resolves against a non-existent queue and falls back to a fresh default-options instance, subsequent `Create()` calls must re-attempt the load instead of permanently caching the defaults. The happy path (options loaded successfully) continues to cache.

**Architecture:** All five factories share a "cache-on-null" pattern: lock, load from store, if load returned null construct a new default and **assign to the cache field**. We change the fallback so the default instance is returned but **not cached** — the `_options` field stays null until a real load succeeds. LiteDb has an extra `InMemoryOptionsCache` hop; we preserve that behavior and only change the final default-fallback assignment.

**Why option 1 (no caching of defaults) and not time-boxed cache:** Per the issue, happy-path cost is zero (still cached). The "pre-existence" window is transient — on a live queue, a single produce call writes the options and the next `Create()` caches successfully. Retrying the store lookup once per `Create()` during this window is cheap.

**Tech Stack:** MSTest 3.x, NSubstitute for the query handler mock. Integration test uses existing in-memory `SQLite` or `LiteDb` scaffolding.

**Scope:** 5 factory files + 1 integration test (one transport is enough — the shape is uniform across all five).

| Transport | File | Notes |
|---|---|---|
| PostgreSQL | `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/Factory/PostgreSqlMessageQueueTransportOptionsFactory.cs` | Canonical shape |
| SqlServer | `Source/DotNetWorkQueue.Transport.SqlServer/Basic/Factory/SQLServerMessageQueueTransportOptionsFactory.cs` | Filename has capital SQL |
| SQLite | `Source/DotNetWorkQueue.Transport.SQLite/Basic/Factory/SqLiteMessageQueueTransportOptionsFactory.cs` | |
| LiteDb | `Source/DotNetWorkQueue.Transport.LiteDB/Basic/Factory/LiteDbMessageQueueTransportOptionsFactory.cs` | Has `InMemoryOptionsCache` fallback — **preserve** |
| Redis | `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisTransportOptionsFactory.cs` | Different filename; read first before editing |

---

## Task 1: PostgreSQL — establish the pattern

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/Factory/PostgreSqlMessageQueueTransportOptionsFactory.cs:49-70`
- Create: `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/Factory/PostgreSqlMessageQueueTransportOptionsFactoryTests.cs`

**Step 1: Write failing tests**

```csharp
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Factory;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.Factory
{
    [TestClass]
    public class PostgreSqlMessageQueueTransportOptionsFactoryTests
    {
        private static (PostgreSqlMessageQueueTransportOptionsFactory sut,
                        IQueryHandler<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>,
                                      PostgreSqlMessageQueueTransportOptions> query)
            Build()
        {
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.ConnectionString.Returns("Host=localhost;Database=test");
            var query = Substitute.For<IQueryHandler<
                GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>,
                PostgreSqlMessageQueueTransportOptions>>();
            var sut = new PostgreSqlMessageQueueTransportOptionsFactory(connInfo, query);
            return (sut, query);
        }

        [TestMethod]
        public void Create_WhenStoreReturnsNull_ReturnsDefaultsWithoutCaching()
        {
            var (sut, query) = Build();
            query.Handle(Arg.Any<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>>())
                 .Returns((PostgreSqlMessageQueueTransportOptions)null);

            var first = sut.Create();
            var second = sut.Create();

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            // Critical assertion: the query handler was invoked on BOTH calls (cache did not stick)
            query.Received(2).Handle(Arg.Any<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>>());
        }

        [TestMethod]
        public void Create_WhenStoreReturnsOptions_CachesAndReturnsSameInstance()
        {
            var (sut, query) = Build();
            var stored = new PostgreSqlMessageQueueTransportOptions { EnableHistory = true };
            query.Handle(Arg.Any<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>>())
                 .Returns(stored);

            var first = sut.Create();
            var second = sut.Create();

            Assert.AreSame(first, second);
            Assert.IsTrue(first.EnableHistory);
            // Happy path: exactly one load
            query.Received(1).Handle(Arg.Any<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>>());
        }

        [TestMethod]
        public void Create_AfterDefaultFallback_ThenStoreHasOptions_ReturnsLoadedOptions()
        {
            var (sut, query) = Build();
            PostgreSqlMessageQueueTransportOptions stored = null;
            query.Handle(Arg.Any<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>>())
                 .Returns(_ => stored);

            var firstDefaults = sut.Create();
            Assert.IsFalse(firstDefaults.EnableHistory);

            stored = new PostgreSqlMessageQueueTransportOptions { EnableHistory = true };
            var second = sut.Create();

            Assert.IsTrue(second.EnableHistory, "After queue creation the factory must observe the new options");
        }
    }
}
```

**Step 2: Run tests — RED**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" \
  --filter "FullyQualifiedName~PostgreSqlMessageQueueTransportOptionsFactoryTests"
```

Expected: FAIL — `Create_AfterDefaultFallback_ThenStoreHasOptions_ReturnsLoadedOptions` returns defaults on second call (current bug).

**Step 3: Implement the fix**

Replace `Create()`:

```csharp
public PostgreSqlMessageQueueTransportOptions Create()
{
    if (string.IsNullOrEmpty(_connectionInformation.ConnectionString))
    {
        return new PostgreSqlMessageQueueTransportOptions();
    }

    if (_options != null) return _options;
    lock (_creator)
    {
        if (_options != null) return _options;

        var loaded = _queryOptions.Handle(new GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>());
        if (loaded != null)
        {
            _options = loaded;
            return _options;
        }

        // Queue does not yet exist in the store — return defaults but do NOT cache;
        // a future Create() after queue creation must observe the real options.
        return new PostgreSqlMessageQueueTransportOptions();
    }
}
```

**Step 4: Run tests — GREEN**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" \
  --filter "FullyQualifiedName~PostgreSqlMessageQueueTransportOptionsFactoryTests"
```

Expected: PASS, 3/3.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/Factory/PostgreSqlMessageQueueTransportOptionsFactory.cs \
        Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/Factory/
git commit -m "fix(postgres): options factory re-loads after default fallback (#120)"
```

---

## Task 2: SqlServer

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/Factory/SQLServerMessageQueueTransportOptionsFactory.cs` (note capital `SQL`)
- Create: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/Factory/SqlServerMessageQueueTransportOptionsFactoryTests.cs`

Mirror Task 1 — 3 tests, same fix shape. Substitute `SqlServerMessageQueueTransportOptions` for the Postgres type.

**Verification:**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" \
  --filter "FullyQualifiedName~SqlServerMessageQueueTransportOptionsFactoryTests"
```

Expected: PASS, 3/3.

**Commit:** `fix(sqlserver): options factory re-loads after default fallback (#120)`

---

## Task 3: SQLite

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SQLite/Basic/Factory/SqLiteMessageQueueTransportOptionsFactory.cs`
- Create: `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/Factory/SqLiteMessageQueueTransportOptionsFactoryTests.cs`

Mirror Task 1 — 3 tests, same fix shape. Substitute `SqLiteMessageQueueTransportOptions`.

**Verification:**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" \
  --filter "FullyQualifiedName~SqLiteMessageQueueTransportOptionsFactoryTests"
```

Expected: PASS, 3/3.

**Commit:** `fix(sqlite): options factory re-loads after default fallback (#120)`

---

## Task 4: LiteDb — preserve `InMemoryOptionsCache` fallback

LiteDb's `Create()` has an extra hop: when the query returns null, it checks a static `InMemoryOptionsCache` keyed by `{QueueName}|{ConnectionString}` before falling back to defaults. That behavior must survive.

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.LiteDB/Basic/Factory/LiteDbMessageQueueTransportOptionsFactory.cs:59-85`
- Create: `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/Factory/LiteDbMessageQueueTransportOptionsFactoryTests.cs`

**Step 1: Write failing tests — 4 this time**

Tests 1-3: same as Task 1 (null load, happy path, after-fallback-reloads).

Test 4: `InMemoryOptionsCache` fallback still works. Call `LiteDbMessageQueueTransportOptionsFactory.SaveToCache(connInfo, stored)` with `EnableHistory=true`, then call `Create()` with a query mock that returns null. Assert the result is the cached instance, and assert `Create()` called again returns the same cached instance (cache DOES stick for the static-cache hit — only fresh-defaults skip caching).

**Step 2: Run tests — RED**

**Step 3: Implement the fix**

```csharp
public LiteDbMessageQueueTransportOptions Create()
{
    if (string.IsNullOrEmpty(_connectionInformation.ConnectionString))
    {
        return new LiteDbMessageQueueTransportOptions();
    }

    if (_options != null) return _options;
    lock (_creator)
    {
        if (_options != null) return _options;

        var loaded = _queryOptions.Handle(new GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>());
        if (loaded != null)
        {
            _options = loaded;
            return _options;
        }

        // Fallback: static cache for in-memory mode (producer/consumer with separate DB instances)
        var key = $"{_connectionInformation.QueueName}|{_connectionInformation.ConnectionString}";
        if (InMemoryOptionsCache.TryGetValue(key, out var cached))
        {
            _options = cached;
            return _options;
        }

        // Not in store, not in static cache — return defaults without caching
        return new LiteDbMessageQueueTransportOptions();
    }
}
```

**Step 4: Run tests — GREEN. Run the LiteDb unit test suite to check InMemoryOptionsCache callers:**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj"
```

Expected: PASS — no regressions in `SaveToCache`-dependent paths.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.LiteDB/Basic/Factory/LiteDbMessageQueueTransportOptionsFactory.cs \
        Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/Factory/
git commit -m "fix(litedb): options factory re-loads after default fallback; preserve InMemoryOptionsCache (#120)"
```

---

## Task 5: Redis — read the file, adapt the pattern

Redis's factory is named `RedisTransportOptionsFactory.cs` (not `RedisMessageQueueTransportOptionsFactory`) and its load mechanism uses `StringGetAsync` against `_redisNames.Configuration` rather than an `IQueryHandler<GetQueueOptionsQuery<T>, T>`.

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisTransportOptionsFactory.cs`
- Create: `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/RedisTransportOptionsFactoryTests.cs`

**Step 1: Read the current file**

```bash
cat Source/DotNetWorkQueue.Transport.Redis/Basic/RedisTransportOptionsFactory.cs
```

Identify the equivalent "load → null → cache default" path. If Redis already behaves correctly (e.g., doesn't cache a default), write a characterization test asserting that and close this task early with "N/A for Redis — factory shape differs". Otherwise apply the same refactor: separate "cache only if loaded real data" from "return defaults uncached".

**Step 2-5:** If a refactor is needed, mirror Task 1's TDD cycle. If N/A, commit a characterization test that locks in the correct behavior.

**Commit (one of):**

- If fixed: `fix(redis): options factory re-loads after default fallback (#120)`
- If N/A: `test(redis): characterize options factory does not cache defaults (#120)`

---

## Task 6: Integration test — end-to-end proof across a container lifetime

Use SQLite (no external service required) to exercise the full flow: resolve `IBaseTransportOptions` against a non-existent queue, create the queue with non-default options, re-resolve, assert the observed options changed.

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/TransportOptionsCacheRefresh/TransportOptionsCacheRefreshTests.cs`

(If that integration project layout differs, use the existing SQLite integration-test project path and place the file under a matching folder.)

**Step 1: Write the test**

Pseudocode — adapt to the existing `QueueContainer<SqLiteMessageQueueInit>` scaffolding used elsewhere in the SQLite integration suite:

1. Build a `QueueContainer<SqLiteMessageQueueInit>` pointing at a not-yet-created queue (unique queue name; connection string to a temp DB file or `:memory:`).
2. Resolve `IBaseTransportOptions` (or the typed `SqLiteMessageQueueTransportOptions`) from the container. Assert defaults.
3. Create the queue with `EnableHistory = true` (or any non-default) via the transport's `QueueCreationContainer<SqLiteMessageQueueInit>`.
4. Resolve options again from the **same** first container.
5. Assert `EnableHistory == true`.

**Step 2: Run it**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" \
  --filter "FullyQualifiedName~TransportOptionsCacheRefreshTests"
```

Expected: PASS. This fails today because the cache-on-default bug sticks.

**Step 3: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/TransportOptionsCacheRefresh/
git commit -m "test(sqlite): integration test for options factory cache refresh (#120)"
```

---

## Task 7: Regression pass + PR

**Step 1: Full unit test suite on affected transports**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" && \
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" && \
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" && \
dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" && \
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj"
```

Expected: PASS.

**Step 2: Open PR**

```bash
gh pr create --base master --title "fix(transports): options factory re-loads after default fallback (#120)" \
  --body "Closes #120.

- 5 factories: PostgreSQL, SqlServer, SQLite, LiteDb, Redis
- Cache-on-default removed; cache-on-loaded preserved
- LiteDb's InMemoryOptionsCache fallback preserved
- 13+ unit tests (3 per factory covering null/happy/after-fallback)
- SQLite integration test proving end-to-end observation of new options after queue creation"
```

---

## Acceptance

- [x] All 5 factory files patched (or Redis characterized).
- [x] Per-factory: test asserting null load does NOT cache; happy path caches; after-default-fallback + new store value observes the new value.
- [x] LiteDb: static `InMemoryOptionsCache` fallback still works and its hit does cache (only fresh defaults skip caching).
- [x] Integration test on SQLite proving: resolve-before-create → defaults; create queue with non-defaults; re-resolve → non-defaults.
- [x] All 5 transport unit-test projects pass; SQLite integration suite passes.
