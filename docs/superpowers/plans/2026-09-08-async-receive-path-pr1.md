# Async receive path — PR 1: the awaitable throttle

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give `IWaitForEventOrCancel` and `IWaitForEventOrCancelThreadPool` an awaitable wait, so a later PR can stop blocking a thread-pool thread to enforce consumer backpressure.

**Architecture:** `WaitForEventOrCancel` wraps a `ManualResetEventSlim`, which has no `WaitAsync`. A `TaskCompletionSource<bool>` is added alongside it, swapped on `Reset` only when already completed so waiters are never orphaned. The sync path is untouched. Nothing calls the new methods in this PR — it is purely additive.

**Tech Stack:** .NET 10 / .NET 8, MSTest 4.2.3, NSubstitute, AutoFixture.

**Spec:** `docs/superpowers/specs/2026-09-08-async-receive-path-design.md`

## Scope

This plan is **PR 1 of the seven** in the spec. PR 2 (`IReceiveMessages`) is the project's
go/no-go gate and gets its own plan once this lands, when the async-wait API is fixed
rather than assumed.

## Global Constraints

- Target frameworks are `net10.0` and `net8.0`. Anything written must compile on both.
- `ValueTask<T>` is the return type for new async methods (spec decision). `CA2012` is
  raised to error in Task 1 and must stay green.
- Never use `Task.Run` to make sync work look async.
- `ConfigureAwait(false)` on every await.
- LGPL-2.1 header on every new non-test `.cs` file — copy the header from the file being
  modified. Test files in this repo do not carry it.
- `.cs` files are CRLF with no BOM (`.gitattributes`: `*.cs text eol=crlf`). Match the
  file you are editing; verify with `head -c3 <file> | od -An -tx1`.
- MSTest 4.x has no `Assert.ThrowsException`. Use `Assert.Throws<T>` or
  `Assert.ThrowsExactly<T>`.
- Release builds set `TreatWarningsAsErrors`. Check with
  `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true` before the
  final commit.

## File Structure

| file | responsibility |
|---|---|
| `.editorconfig` | raise `CA2012` to error, before any `ValueTask` exists |
| `Source/DotNetWorkQueue/IWaitForEventOrCancel.cs` | add `WaitAsync` to the contract |
| `Source/DotNetWorkQueue/Queue/WaitForEventOrCancel.cs` | TCS alongside the `ManualResetEventSlim` |
| `Source/DotNetWorkQueue/IWaitForEventOrCancelThreadPool.cs` | add `WaitAsync(IWorkGroup)` |
| `Source/DotNetWorkQueue/TaskScheduling/WaitForEventOrCancelThreadPool.cs` | route to the per-group instance |
| `Source/DotNetWorkQueue.Tests/Queue/WaitForEventOrCancelTests.cs` | interleaving tests |
| `Source/DotNetWorkQueue.Tests/TaskScheduling/WaitForEventOrCancelThreadPoolTests.cs` | per-group routing tests |

---

### Task 1: Raise CA2012 to error

Do this first. Every `ValueTask` written afterwards is then written under the analyzer
rather than audited after the fact. CA2012 does **not** fire by default in this repo —
verified — so without this step the rest of the project has no guard.

**Files:**
- Modify: `.editorconfig`

**Interfaces:**
- Consumes: nothing
- Produces: nothing in code; a build-time guarantee that `ValueTask` misuse fails the build

- [ ] **Step 1: Confirm CA2012 is currently off**

```bash
mkdir -p /tmp/ca2012 && cd /tmp/ca2012
cat > p.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
EOF
cat > C.cs <<'EOF'
using System.Threading.Tasks;
public class C
{
    public ValueTask<int> GetAsync() => new ValueTask<int>(1);
    public int BlockOnIt() => GetAsync().Result;
}
EOF
dotnet build -c Release -warnaserror
```

Expected: **Build succeeded** — proving the rule is off by default and this task is needed.

- [ ] **Step 2: Add the rule**

Append to `.editorconfig`, in the existing `dotnet_diagnostic` block that already contains
`CA2263`, `CA1859`, `CA1822` and `CA1806`:

```ini
# ValueTask has usage rules a test suite will not catch: a ValueTask over an
# already-completed result tolerates double-await and .Result silently, and only
# breaks under IValueTaskSource pooling. The analyzer is deterministic where a test
# is incidental. Raised to error deliberately - see the async receive path spec.
dotnet_diagnostic.CA2012.severity = error
```

- [ ] **Step 3: Verify the rule now fires**

```bash
cd /tmp/ca2012 && cp /mnt/f/git/dotnetworkqueue/.editorconfig . && dotnet build -c Release
```

Expected: **Build FAILED** with `error CA2012: ValueTask instances should not have their result directly accessed`.

- [ ] **Step 4: Verify the repository still builds**

Run: `dotnet build "Source/DotNetWorkQueue.sln" -c Debug`

Expected: success. If any existing code violates CA2012, fix those call sites in this
task — do not lower the severity. There were no `ValueTask` returns in the repository at
the time of writing, so this should be a no-op.

- [ ] **Step 5: Clean up and commit**

```bash
rm -rf /tmp/ca2012
git add .editorconfig
git commit -m "build: fail the build on ValueTask misuse (CA2012)"
```

---

### Task 2: `WaitAsync` on `IWaitForEventOrCancel`

**Files:**
- Modify: `Source/DotNetWorkQueue/IWaitForEventOrCancel.cs`
- Modify: `Source/DotNetWorkQueue/Queue/WaitForEventOrCancel.cs`
- Test: `Source/DotNetWorkQueue.Tests/Queue/WaitForEventOrCancelTests.cs`

**Interfaces:**
- Consumes: nothing
- Produces: `ValueTask<bool> IWaitForEventOrCancel.WaitAsync()` — returns `true` when
  signalled, `false` when cancelled. Mirrors `bool Wait()` exactly.

- [ ] **Step 1: Write the failing tests**

Add to `Source/DotNetWorkQueue.Tests/Queue/WaitForEventOrCancelTests.cs`. These cover the
interleavings the spec calls out, not just the happy path — a lost wake-up here is an
intermittent hang in a consumer.

```csharp
[TestMethod]
public async Task WaitAsync_ReturnsImmediately_WhenAlreadySignaled()
{
    using var test = Create();
    // constructed signaled, per ManualResetEventSlim(true)
    Assert.IsTrue(await test.WaitAsync());
}

[TestMethod]
public async Task WaitAsync_Blocks_UntilSet()
{
    using var test = Create();
    test.Reset();

    var waiter = test.WaitAsync().AsTask();
    Assert.IsFalse(waiter.IsCompleted, "must not complete while reset");

    test.Set();
    Assert.IsTrue(await waiter);
}

[TestMethod]
public async Task WaitAsync_ReturnsFalse_WhenCancelled()
{
    using var test = Create();
    test.Reset();

    var waiter = test.WaitAsync().AsTask();
    test.Cancel();

    Assert.IsFalse(await waiter);
}

[TestMethod]
public async Task WaitAsync_ResetDuringPendingWait_DoesNotOrphanTheWaiter()
{
    //the lost wake-up case: Reset must not swap out a TCS that already has waiters,
    //or this waiter sleeps until some later Set that it was never told about
    using var test = Create();
    test.Reset();

    var waiter = test.WaitAsync().AsTask();
    test.Reset();                       // second reset, waiter already pending
    test.Set();

    var completed = await Task.WhenAny(waiter, Task.Delay(TimeSpan.FromSeconds(5)));
    Assert.AreSame(waiter, completed, "waiter was orphaned by Reset");
    Assert.IsTrue(await waiter);
}

[TestMethod]
public async Task WaitAsync_SetThenReset_BeforeWaiterResumes_StillReleases()
{
    using var test = Create();
    test.Reset();

    var waiter = test.WaitAsync().AsTask();
    test.Set();
    test.Reset();                       // immediately re-armed

    var completed = await Task.WhenAny(waiter, Task.Delay(TimeSpan.FromSeconds(5)));
    Assert.AreSame(waiter, completed, "a Set that happened must release its waiter");
    Assert.IsTrue(await waiter);
}

[TestMethod]
public async Task WaitAsync_ManyWaiters_AllReleasedByOneSet()
{
    using var test = Create();
    test.Reset();

    var waiters = new Task<bool>[50];
    for (var i = 0; i < waiters.Length; i++)
        waiters[i] = test.WaitAsync().AsTask();

    test.Set();

    var all = Task.WhenAll(waiters);
    var completed = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(10)));
    Assert.AreSame(all, completed, "one Set must release every waiter");
    foreach (var r in await all)
        Assert.IsTrue(r);
}

[TestMethod]
public async Task WaitAsync_IfDisposed_Exception()
{
    var test = Create();
    test.Dispose();
    await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await test.WaitAsync());
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:
```bash
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" \
  -f net10.0 --filter "FullyQualifiedName~WaitForEventOrCancelTests"
```

Expected: compile failure — `'IWaitForEventOrCancel' does not contain a definition for 'WaitAsync'`.

- [ ] **Step 3: Add the method to the interface**

In `Source/DotNetWorkQueue/IWaitForEventOrCancel.cs`, add inside the interface, after `Wait()`:

```csharp
        /// <summary>
        /// Waits to be notified to stop waiting, without blocking a thread.
        /// </summary>
        /// <returns><c>true</c> if signaled; <c>false</c> if cancelled.</returns>
        /// <remarks>
        /// The asynchronous twin of <see cref="Wait"/> and returns the same values.
        /// Exists because a consumer that blocks a thread-pool thread here cannot complete
        /// the very continuations that would release it.
        /// </remarks>
        ValueTask<bool> WaitAsync();
```

Add `using System.Threading.Tasks;` to the file's usings if it is not already present.

- [ ] **Step 4: Implement it**

In `Source/DotNetWorkQueue/Queue/WaitForEventOrCancel.cs`:

Add to the usings:

```csharp
using System.Threading.Tasks;
```

Add the field beside `_resetEvent`:

```csharp
        //ManualResetEventSlim has no WaitAsync, so async waiters park on this instead.
        //RunContinuationsAsynchronously matters: without it a continuation runs inline on
        //whichever thread called Set, which is a scheduler thread we must not occupy.
        private TaskCompletionSource<bool> _asyncWait;
```

In the constructor, after `_cancellationTokenSource = new CancellationTokenSource();`:

```csharp
            //constructed signaled, matching ManualResetEventSlim(true) above
            _asyncWait = CreateCompletedWait();
```

Add these members:

```csharp
        private static TaskCompletionSource<bool> CreateWait() =>
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private static TaskCompletionSource<bool> CreateCompletedWait()
        {
            var source = CreateWait();
            source.SetResult(true);
            return source;
        }

        /// <inheritdoc />
        public async ValueTask<bool> WaitAsync()
        {
            ThrowIfDisposed();

            //volatile read - Reset may swap this between the read and the await
            var wait = Volatile.Read(ref _asyncWait);
            return await wait.Task.ConfigureAwait(false);
        }
```

Change `Reset` to:

```csharp
        /// <summary>
        /// Resets the wait status, causing <see cref="Wait" /> calls to wait.
        /// </summary>
        public void Reset()
        {
            ThrowIfDisposed();
            _resetEvent.Reset();

            //Only re-arm a wait that has already completed. Replacing one that still has
            //waiters would orphan them: the next Set would complete the new instance while
            //they went on awaiting the old one, and a worker would stop dequeuing until
            //something unrelated freed a thread.
            var current = Volatile.Read(ref _asyncWait);
            if (current.Task.IsCompleted)
                Interlocked.CompareExchange(ref _asyncWait, CreateWait(), current);
        }
```

Change `Set` to:

```csharp
        /// <summary>
        /// Sets the state to signaled; any <see cref="Wait" /> calls will return
        /// </summary>
        public void Set()
        {
            ThrowIfDisposed();
            _resetEvent.Set();
            Volatile.Read(ref _asyncWait).TrySetResult(true);
        }
```

Change `Cancel` to:

```csharp
        /// <summary>
        /// Cancels any current <see cref="Wait" /> calls
        /// </summary>
        public void Cancel()
        {
            ThrowIfDisposed();
            _cancellationTokenSource.Cancel();

            //false is what the synchronous Wait returns when cancelled; async waiters
            //get the same answer rather than an exception
            Volatile.Read(ref _asyncWait).TrySetResult(false);
        }
```

- [ ] **Step 5: Run the tests to verify they pass**

Run:
```bash
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" \
  -f net10.0 --filter "FullyQualifiedName~WaitForEventOrCancelTests"
```

Expected: PASS, including the pre-existing synchronous tests, which must be unaffected.

- [ ] **Step 6: Run the tests repeatedly to catch a race**

An interleaving bug will not fail every run. Run the class 20 times:

```bash
for i in $(seq 1 20); do
  dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" \
    -f net10.0 --filter "FullyQualifiedName~WaitForEventOrCancelTests" \
    --nologo -v q || { echo "FAILED on run $i"; break; }
done
```

Expected: 20 passes. A single failure means the Set/Reset handling is wrong — fix it
rather than re-running.

- [ ] **Step 7: Commit**

```bash
git add Source/DotNetWorkQueue/IWaitForEventOrCancel.cs \
        Source/DotNetWorkQueue/Queue/WaitForEventOrCancel.cs \
        Source/DotNetWorkQueue.Tests/Queue/WaitForEventOrCancelTests.cs
git commit -m "feat: awaitable wait on IWaitForEventOrCancel"
```

---

### Task 3: `WaitAsync` on `IWaitForEventOrCancelThreadPool`

**Files:**
- Modify: `Source/DotNetWorkQueue/IWaitForEventOrCancelThreadPool.cs`
- Modify: `Source/DotNetWorkQueue/TaskScheduling/WaitForEventOrCancelThreadPool.cs`
- Create: `Source/DotNetWorkQueue.Tests/TaskScheduling/WaitForEventOrCancelThreadPoolTests.cs`

**Interfaces:**
- Consumes: `ValueTask<bool> IWaitForEventOrCancel.WaitAsync()` from Task 2
- Produces: `ValueTask<bool> IWaitForEventOrCancelThreadPool.WaitAsync(IWorkGroup group)` —
  routes to the per-group instance, or to the shared one when `group` is null. This is what
  PR 2's consumer loop awaits.

- [ ] **Step 1: Write the failing tests**

Create `Source/DotNetWorkQueue.Tests/TaskScheduling/WaitForEventOrCancelThreadPoolTests.cs`:

```csharp
using System;
using System.Threading.Tasks;
using DotNetWorkQueue.TaskScheduling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    [TestClass]
    public class WaitForEventOrCancelThreadPoolTests
    {
        [TestMethod]
        public async Task WaitAsync_NullGroup_UsesTheSharedWait()
        {
            using var test = Create();
            Assert.IsTrue(await test.WaitAsync(null));
        }

        [TestMethod]
        public async Task WaitAsync_Group_ReturnsAfterSet()
        {
            using var test = Create();
            var group = Substitute.For<IWorkGroup>();

            test.Reset(group);
            var waiter = test.WaitAsync(group).AsTask();
            Assert.IsFalse(waiter.IsCompleted, "must not complete while the group is reset");

            test.Set(group);
            Assert.IsTrue(await waiter);
        }

        [TestMethod]
        public async Task WaitAsync_GroupsAreIndependent()
        {
            //a full group must not release a waiter on a different group
            using var test = Create();
            var groupOne = Substitute.For<IWorkGroup>();
            var groupTwo = Substitute.For<IWorkGroup>();

            test.Reset(groupOne);
            test.Reset(groupTwo);

            var waiterOne = test.WaitAsync(groupOne).AsTask();
            test.Set(groupTwo);

            Assert.IsFalse(waiterOne.IsCompleted, "setting another group released this one");

            test.Set(groupOne);
            Assert.IsTrue(await waiterOne);
        }

        [TestMethod]
        public async Task WaitAsync_IfDisposed_Exception()
        {
            var test = Create();
            test.Dispose();
            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
                async () => await test.WaitAsync(null));
        }

        private static WaitForEventOrCancelThreadPool Create()
        {
            var factory = Substitute.For<IWaitForEventOrCancelFactory>();
            factory.Create().Returns(_ => new Queue.WaitForEventOrCancel());
            return new WaitForEventOrCancelThreadPool(factory);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:
```bash
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" \
  -f net10.0 --filter "FullyQualifiedName~WaitForEventOrCancelThreadPoolTests"
```

Expected: compile failure — no `WaitAsync` on `IWaitForEventOrCancelThreadPool`.

Both `WaitForEventOrCancel` and `WaitForEventOrCancelThreadPool` are `internal`. That is
fine and needs no change: `Source/DotNetWorkQueue/InternalsVisibleForTests.cs` already
declares `InternalsVisibleTo("DotNetWorkQueue.Tests")`, which is the assembly this test
lives in.

If `IWaitForEventOrCancelFactory` is not in the `DotNetWorkQueue` namespace, add the
correct `using` — find it with
`grep -rn "interface IWaitForEventOrCancelFactory" Source/DotNetWorkQueue`.

- [ ] **Step 3: Add the method to the interface**

In `Source/DotNetWorkQueue/IWaitForEventOrCancelThreadPool.cs`, after `Wait`:

```csharp
        /// <summary>
        /// Waits until notified to stop waiting, without blocking a thread.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns><c>true</c> if signaled; <c>false</c> if cancelled.</returns>
        ValueTask<bool> WaitAsync(IWorkGroup group);
```

Add `using System.Threading.Tasks;` if absent.

- [ ] **Step 4: Implement it**

In `Source/DotNetWorkQueue/TaskScheduling/WaitForEventOrCancelThreadPool.cs`, directly
below `Wait`, mirroring its structure exactly:

```csharp
        /// <inheritdoc />
        public ValueTask<bool> WaitAsync(IWorkGroup group)
        {
            ThrowIfDisposed();

            if (group == null)
            {
                return _waitForEvent.Value.WaitAsync();
            }

            return GetOrAddGroup(group).WaitAsync();
        }
```

Add `using System.Threading.Tasks;` if absent.

- [ ] **Step 5: Run the tests to verify they pass**

Run:
```bash
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" \
  -f net10.0 --filter "FullyQualifiedName~WaitForEventOrCancelThreadPoolTests"
```

Expected: PASS.

- [ ] **Step 6: Verify nothing else broke, including the Release gate**

```bash
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" -f net10.0
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true
```

Expected: all unit tests pass; Release build succeeds. The Release build is where
`TreatWarningsAsErrors` and the XML documentation requirement bite, so a missing `<param>`
or `<returns>` on the new members fails here rather than in CI.

- [ ] **Step 7: Check the line endings you have written**

```bash
for f in Source/DotNetWorkQueue/IWaitForEventOrCancel.cs \
         Source/DotNetWorkQueue/Queue/WaitForEventOrCancel.cs \
         Source/DotNetWorkQueue/IWaitForEventOrCancelThreadPool.cs \
         Source/DotNetWorkQueue/TaskScheduling/WaitForEventOrCancelThreadPool.cs \
         Source/DotNetWorkQueue.Tests/TaskScheduling/WaitForEventOrCancelThreadPoolTests.cs; do
  printf '%-88s BOM=%s %s\n' "$f" \
    "$(head -c3 "$f" | od -An -tx1 | tr -d ' \n')" \
    "$(file "$f" | grep -o CRLF || echo LF)"
done
```

Expected: every file CRLF. A BOM of `efbbbf` is only correct if the original file had one —
compare against `git show HEAD:<file> | head -c3 | od -An -tx1`. Adding a BOM to a file
that lacked one has broken this repository before.

- [ ] **Step 8: Commit**

```bash
git add Source/DotNetWorkQueue/IWaitForEventOrCancelThreadPool.cs \
        Source/DotNetWorkQueue/TaskScheduling/WaitForEventOrCancelThreadPool.cs \
        Source/DotNetWorkQueue.Tests/TaskScheduling/WaitForEventOrCancelThreadPoolTests.cs
git commit -m "feat: awaitable per-work-group wait on the scheduler throttle"
```

---

## Done means

- `CA2012` fails the build on `ValueTask` misuse, verified both ways
- `IWaitForEventOrCancel.WaitAsync` and `IWaitForEventOrCancelThreadPool.WaitAsync` exist
  and return the same values as their synchronous twins
- the interleaving tests pass 20 consecutive runs
- the synchronous path is untouched and its existing tests still pass
- `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true` succeeds
- nothing calls the new methods yet; this PR changes no behaviour

## Not in this PR

- Any change to `MessageProcessingAsync`, `ProcessMessageAsync` or
  `SchedulerMessageHandler`. They keep blocking until PR 2.
- Any transport change.
- `IQueueWait.WaitAsync` — that is PR 6, and it has its own contract in the spec: it must
  return rather than throw on stop.
