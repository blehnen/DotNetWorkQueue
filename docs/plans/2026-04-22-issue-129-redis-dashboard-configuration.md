# Redis Dashboard Configuration Read — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Issue:** [#129](https://github.com/blehnen/DotNetWorkQueue/issues/129)

**Goal:** Redis dashboard Configuration tab renders the persisted transport options JSON instead of "(no configuration)".

**Architecture:** The `_redisNames.Configuration` key already holds the persisted JSON (`RedisTransportOptionsFactory.cs:58` reads it back into options). The dashboard-side query handler currently returns `null` from a stale stub. Read the key, return bytes; null-safe when the queue has never had options persisted.

**Tech Stack:** StackExchange.Redis, NSubstitute, MSTest 3.x. Test seam follows the existing `protected virtual IDatabase GetDb()` pattern used by `QueryMessageHistoryHandler`, `PurgeMessageHistoryHandler`, `WriteMessageHistoryHandler`.

**Follow-up issue filed?** N/A.

---

## Task 1: Add `GetDb()` test seam to `GetDashboardConfigurationQueryHandlerAsync`

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.Redis/Basic/QueryHandler/GetDashboardConfigurationQueryHandlerAsync.cs`

**Step 1: Add the virtual seam**

Below the constructor, add:

```csharp
protected virtual IDatabase GetDb() => _connection.Connection.GetDatabase();
```

Add `using StackExchange.Redis;` at the top.

**Step 2: Build verify**

```bash
dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" -c Debug
```

Expected: Build succeeded, 0 errors.

**Step 3: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.Redis/Basic/QueryHandler/GetDashboardConfigurationQueryHandlerAsync.cs
git commit -m "refactor(redis): add GetDb() virtual seam on dashboard config query handler (#129)"
```

---

## Task 2: Write failing tests for the new read path

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/QueryHandler/GetDashboardConfigurationQueryHandlerAsyncTests.cs`

**Step 1: Create the test file**

Follow the pattern in `QueryMessageHistoryHandlerTests.cs` (the `TestableQueryHandler` subclass).

```csharp
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardConfigurationQueryHandlerAsyncTests
    {
        private class TestableHandler : GetDashboardConfigurationQueryHandlerAsync
        {
            private readonly IDatabase _db;
            public TestableHandler(IRedisConnection connection, RedisNames redisNames, IDatabase db)
                : base(connection, redisNames) { _db = db; }
            protected override IDatabase GetDb() => _db;
        }

        private static TestableHandler CreateHandler(IDatabase db, out RedisNames names)
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            names = Substitute.For<RedisNames>(connInfo);
            names.Configuration.Returns("queue:test:Configuration");
            return new TestableHandler(connection, names, db);
        }

        [TestMethod]
        public async Task HandleAsync_ReturnsBytes_WhenKeyHasValue()
        {
            var db = Substitute.For<IDatabase>();
            db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
              .Returns(Task.FromResult<RedisValue>("{\"EnableHistory\":true}"));

            var handler = CreateHandler(db, out _);
            var result = await handler.HandleAsync(new GetDashboardConfigurationQuery());

            Assert.IsNotNull(result);
            Assert.AreEqual("{\"EnableHistory\":true}", Encoding.UTF8.GetString(result));
        }

        [TestMethod]
        public async Task HandleAsync_ReturnsNull_WhenKeyHasNoValue()
        {
            var db = Substitute.For<IDatabase>();
            db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
              .Returns(Task.FromResult<RedisValue>(RedisValue.Null));

            var handler = CreateHandler(db, out _);
            var result = await handler.HandleAsync(new GetDashboardConfigurationQuery());

            Assert.IsNull(result);
        }
    }
}
```

**Step 2: Run tests — confirm they fail**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" \
  --filter "FullyQualifiedName~GetDashboardConfigurationQueryHandlerAsyncTests"
```

Expected: FAIL — `HandleAsync_ReturnsBytes_WhenKeyHasValue` returns null (stale stub still in place).

**Step 3: Commit (RED)**

```bash
git add Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/QueryHandler/
git commit -m "test(redis): add failing test for dashboard config read (#129)"
```

---

## Task 3: Implement the read — `HandleAsync` calls `GetDb().StringGetAsync(Configuration)`

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.Redis/Basic/QueryHandler/GetDashboardConfigurationQueryHandlerAsync.cs`

**Step 1: Replace the stub**

```csharp
public async Task<byte[]> HandleAsync(GetDashboardConfigurationQuery query)
{
    var json = await GetDb().StringGetAsync(_redisNames.Configuration).ConfigureAwait(false);
    return json.HasValue ? System.Text.Encoding.UTF8.GetBytes(json) : null;
}
```

**Step 2: Run tests — confirm they pass**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" \
  --filter "FullyQualifiedName~GetDashboardConfigurationQueryHandlerAsyncTests"
```

Expected: PASS, 2/2 tests.

**Step 3: Full Redis unit suite regression check**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj"
```

Expected: PASS, no new failures.

**Step 4: Commit (GREEN)**

```bash
git add Source/DotNetWorkQueue.Transport.Redis/Basic/QueryHandler/GetDashboardConfigurationQueryHandlerAsync.cs
git commit -m "fix(redis): dashboard Configuration tab reads persisted options JSON (#129)"
```

---

## Task 4: Manual smoke-test and PR

**Step 1: Start the Redis dashboard against a queue with persisted options**

Use an existing Redis integration-test connection string or a local Redis. Produce one message on a Redis queue with non-default options (e.g., `EnableHistory=true`) to populate the `Configuration` key.

**Step 2: Verify in browser**

Navigate to the Redis queue's Configuration tab in Dashboard.Ui. Expected: JSON body with the enabled options, not "(no configuration)".

**Step 3: Open PR**

```bash
gh pr create --base master --title "fix(redis): dashboard Configuration tab reads persisted options JSON (#129)" \
  --body "Closes #129. Redis dashboard Configuration tab now reads from _redisNames.Configuration via the new GetDb() test seam. Manual smoke-test performed against a local Redis with EnableHistory=true."
```

---

## Acceptance

- [x] Redis Configuration tab renders options JSON (Enable* flags, HistoryOptions) matching other transports.
- [x] Null path handled gracefully (queue never produced → "(no configuration)" still shows).
- [x] 2 unit tests: one for populated key, one for null.
- [x] Full Redis unit test suite still passes.
- [x] Manual smoke-test in Dashboard.Ui confirms rendered output.
