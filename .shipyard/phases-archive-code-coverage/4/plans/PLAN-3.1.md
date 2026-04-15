---
phase: phase-4-litedb-redis-job-handlers
plan: "3.1"
wave: 3
dependencies: ["1.2"]
must_haves:
  - New test file for RedisJobQueueCreation
  - Wrapper delegation verified for all 4 members (IsDisposed, Scope, CreateJobSchedulerQueue, RemoveQueue)
  - Constructor null-guard test
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/RedisJobQueueCreationTests.cs
tdd: true
risk: low
---

# Plan 3.1 -- RedisJobQueueCreation Wrapper Tests

## Context

After Plan 1.2 refactors `RedisJobQueueCreation` to depend on `IQueueCreation` (interface) instead of concrete `RedisQueueCreation`, the wrapper becomes trivially testable. The 4 delegated members can all be verified by mocking `IQueueCreation` with NSubstitute.

`RedisJobQueueCreation` (post-refactor) members to test:
- `IsDisposed => _creation.IsDisposed`
- `Scope => _creation.Scope`
- `CreateJobSchedulerQueue(...) => _creation.CreateQueue()`
- `RemoveQueue() => _creation.RemoveQueue()`
- Constructor `Guard.NotNull` on the `creation` parameter

## Dependencies

- **Plan 1.2** must be complete (RedisJobQueueCreation must take `IQueueCreation` for the test to mock it)

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/RedisJobQueueCreationTests.cs" tdd="true">
  <action>
1. READ the refactored `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisJobQueueCreation.cs` to confirm the constructor takes `IQueueCreation`
2. CREATE `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/RedisJobQueueCreationTests.cs` with LGPL-2.1 header
3. Add the following test methods (use MSTest 3.x conventions):

```csharp
[TestClass]
public class RedisJobQueueCreationTests
{
    [TestMethod]
    public void Constructor_NullCreation_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new RedisJobQueueCreation(null));
    }

    [TestMethod]
    public void IsDisposed_Delegates_ToInnerCreation()
    {
        var inner = Substitute.For<IQueueCreation>();
        inner.IsDisposed.Returns(true);
        var sut = new RedisJobQueueCreation(inner);
        Assert.IsTrue(sut.IsDisposed);
        var temp = inner.Received(1).IsDisposed;
    }

    [TestMethod]
    public void Scope_Delegates_ToInnerCreation()
    {
        var inner = Substitute.For<IQueueCreation>();
        var scope = Substitute.For<ICreationScope>();
        inner.Scope.Returns(scope);
        var sut = new RedisJobQueueCreation(inner);
        Assert.AreSame(scope, sut.Scope);
    }

    [TestMethod]
    public void CreateJobSchedulerQueue_Delegates_ToInnerCreateQueue()
    {
        var inner = Substitute.For<IQueueCreation>();
        var expected = new QueueCreationResult(QueueCreationStatus.Success);
        inner.CreateQueue().Returns(expected);
        var sut = new RedisJobQueueCreation(inner);
        var result = sut.CreateJobSchedulerQueue(_ => { }, new QueueConnection("queue", "connection"));
        Assert.AreSame(expected, result);
        inner.Received(1).CreateQueue();
    }

    [TestMethod]
    public void RemoveQueue_Delegates_ToInnerRemoveQueue()
    {
        var inner = Substitute.For<IQueueCreation>();
        var expected = new QueueRemoveResult(QueueRemoveStatus.Success);
        inner.RemoveQueue().Returns(expected);
        var sut = new RedisJobQueueCreation(inner);
        var result = sut.RemoveQueue();
        Assert.AreSame(expected, result);
        inner.Received(1).RemoveQueue();
    }
}
```

4. READ `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisJobQueueCreation.cs` to verify the exact signatures of `CreateJobSchedulerQueue` and `RemoveQueue` -- adjust the test calls if my example signatures are off (especially the `CreateJobSchedulerQueue` parameters: `Action<IContainer> registerService, QueueConnection queueConnection, Action<IContainer> setOptions = null, bool enableRoute = false`)
5. Add required `using` directives:
   - `System;`
   - `DotNetWorkQueue;` (for IQueueCreation, QueueCreationResult, QueueRemoveResult)
   - `DotNetWorkQueue.Configuration;` (for QueueConnection)
   - `DotNetWorkQueue.Queue;` (for ICreationScope)
   - `DotNetWorkQueue.Transport.Redis.Basic;`
   - `Microsoft.VisualStudio.TestTools.UnitTesting;`
   - `NSubstitute;`
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~RedisJobQueueCreationTests" -c Debug 2>&1 | tail -10</verify>
  <done>RedisJobQueueCreationTests file exists with at least 5 tests (constructor null guard + 4 delegation tests). All tests pass on net10.0. No production code changed beyond Plan 1.2's refactor.</done>
</task>
