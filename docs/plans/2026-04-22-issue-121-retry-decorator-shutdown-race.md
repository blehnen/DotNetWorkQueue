# Retry Decorator Shutdown-Race Fix — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Issue:** [#121](https://github.com/blehnen/DotNetWorkQueue/issues/121)

**Goal:** Relational-transport retry decorators no longer throw `ObjectDisposedException` during shutdown when `ResiliencePipelineRegistry` has been disposed before the last command-handler call.

**Architecture:** Polly's `ResiliencePipelineRegistry.TryGetPipeline(...)` throws `ObjectDisposedException` on a disposed registry rather than returning `false`. The decorators already have a "no policy found → run decorated handler directly" fallback — we extend that fallback to also trigger on ODE. One narrow `catch` per decorator, matching the existing fallback semantics.

**Why a narrow `catch` (not a `_disposed` flag or dispose-ordering refactor):** Disposal races in .NET have no non-throwing alternative — the BCL itself uses `catch (ObjectDisposedException)` in timer callbacks, socket shutdown, etc. A check-then-call would have a TOCTOU race. The deeper architectural fix (container-level disposal ordering) is deferred to a follow-up issue.

**Scope:** 13 decorator files across 3 transports.

| Decorator | SqlServer | PostgreSQL | SQLite |
|---|---|---|---|
| `RetryCommandHandlerDecorator` | ✓ | ✓ | ✓ |
| `RetryCommandHandlerOutputDecorator` | ✓ | ✓ | ✓ |
| `RetryCommandHandlerOutputDecoratorAsync` | ✓ | ✓ | ✓ |
| `RetryQueryHandlerDecorator` | ✓ | ✓ | ✓ |
| `BeginTransactionRetryDecorator` | — | — | ✓ (variant shape) |

**Tech Stack:** Polly 8.6.5, MSTest 3.x, NSubstitute. Tests mock `IPolicies` to expose a disposed `ResiliencePipelineRegistry<string>`.

**Pre-work:** File follow-up issue "Investigate deeper fix for disposal-ordering race (related #121)" before merging this plan's PR. Record the issue number in the PR description.

---

## Task 1: Establish the pattern — fix `SqlServer/RetryCommandHandlerDecorator.cs` with TDD

This task defines the code and test shape. Remaining tasks apply the same shape mechanically.

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerDecorator.cs:48-56`
- Create: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerDecoratorTests.cs`

**Step 1: Write failing test — disposed registry falls through to decorated handler**

```csharp
using System;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SqlServer.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Polly.Registry;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Decorator
{
    [TestClass]
    public class RetryCommandHandlerDecoratorTests
    {
        [TestMethod]
        public void Handle_WhenRegistryDisposed_FallsThroughToDecorated()
        {
            var decorated = Substitute.For<ICommandHandler<FakeCommand>>();
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            registry.Dispose();
            policies.Registry.Returns(registry);

            var sut = new RetryCommandHandlerDecorator<FakeCommand>(decorated, policies);
            var cmd = new FakeCommand();

            sut.Handle(cmd); // must NOT throw

            decorated.Received(1).Handle(cmd);
        }

        [TestMethod]
        public void Handle_WhenPipelineRegistered_ExecutesThroughPipeline()
        {
            var decorated = Substitute.For<ICommandHandler<FakeCommand>>();
            var policies = Substitute.For<IPolicies>();
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(TransportPolicyDefinitions.RetryCommandHandler,
                (b, _) => b.AddRetry(new Polly.Retry.RetryStrategyOptions()));
            policies.Registry.Returns(registry);

            var sut = new RetryCommandHandlerDecorator<FakeCommand>(decorated, policies);
            var cmd = new FakeCommand();

            sut.Handle(cmd);

            decorated.Received(1).Handle(cmd);
            registry.Dispose();
        }

        public sealed class FakeCommand { }
    }
}
```

**Step 2: Run test — confirm RED**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" \
  --filter "FullyQualifiedName~RetryCommandHandlerDecoratorTests.Handle_WhenRegistryDisposed_FallsThroughToDecorated"
```

Expected: FAIL — test throws `ObjectDisposedException` from `TryGetPipeline`.

**Step 3: Implement the fix**

Replace the body of `Handle(TCommand command)`:

```csharp
public void Handle(TCommand command)
{
    ResiliencePipeline pipeline = null;
    try
    {
        _policies.Registry.TryGetPipeline(TransportPolicyDefinitions.RetryCommandHandler, out pipeline);
    }
    catch (ObjectDisposedException)
    {
        // Registry was disposed during shutdown race; fall through to direct handler.
    }

    if (pipeline != null)
        pipeline.Execute(_ => _decorated.Handle(command));
    else
        _decorated.Handle(command);
}
```

Add `using System;` if absent.

**Step 4: Run test — confirm GREEN**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" \
  --filter "FullyQualifiedName~RetryCommandHandlerDecoratorTests"
```

Expected: PASS, 2/2.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerDecorator.cs \
        Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerDecoratorTests.cs
git commit -m "fix(sqlserver): retry decorator handles disposed Polly registry at shutdown (#121)"
```

---

## Task 2: Apply pattern to remaining SqlServer decorators (3 files)

Files (same edit shape as Task 1 — wrap `TryGetPipeline` in `try/catch (ObjectDisposedException)`, use local `pipeline` var, branch on `pipeline != null`):

- `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryQueryHandlerDecorator.cs:51` (returns a value — adapt: on caught exception, run `_decorated.Handle(query)` directly and return its result)
- `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs:51` (returns `TOutput` — same value-returning shape as query decorator)
- `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs:52` (`async Task<TOutput>` — use `await pipeline.ExecuteAsync(...)`)

**Step 1:** Add 3 parallel test files modeled on Task 1 (disposed-registry-falls-through + happy-path). Async tests use `async Task` methods and `await`. Put them next to the existing file patterns under `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/`.

**Step 2:** Apply the pattern to the 3 production files.

**Step 3:** Run the SqlServer unit suite:

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" \
  --filter "FullyQualifiedName~Decorator"
```

Expected: PASS.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryQueryHandlerDecorator.cs \
        Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs \
        Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs \
        Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/
git commit -m "fix(sqlserver): all retry decorators handle disposed Polly registry at shutdown (#121)"
```

---

## Task 3: Apply pattern to PostgreSQL decorators (4 files)

Files:

- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerDecorator.cs:50`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryQueryHandlerDecorator.cs:51`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs:52`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs:52`

**Step 1:** Mirror Task 1 + Task 2's test files into `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/` (4 test classes).

**Step 2:** Apply the same pattern to the 4 production files.

**Step 3:** Run the PostgreSQL unit suite:

```bash
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" \
  --filter "FullyQualifiedName~Decorator"
```

Expected: PASS.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/ \
        Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/
git commit -m "fix(postgres): all retry decorators handle disposed Polly registry at shutdown (#121)"
```

---

## Task 4: Apply pattern to SQLite uniform decorators (4 files)

Files (same as PostgreSQL's 4, under `DotNetWorkQueue.Transport.SQLite/Decorator/`):

- `RetryCommandHandlerDecorator.cs:51`
- `RetryQueryHandlerDecorator.cs:52`
- `RetryCommandHandlerOutputDecorator.cs:53`
- `RetryCommandHandlerOutputDecoratorAsync.cs:53`

**Step 1:** Mirror tests into `Source/DotNetWorkQueue.Transport.SQLite.Tests/Decorator/`.

**Step 2:** Apply the pattern.

**Step 3:** Run the SQLite unit suite:

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" \
  --filter "FullyQualifiedName~Decorator"
```

Expected: PASS.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SQLite/Decorator/Retry* \
        Source/DotNetWorkQueue.Transport.SQLite.Tests/Decorator/
git commit -m "fix(sqlite): uniform retry decorators handle disposed Polly registry at shutdown (#121)"
```

---

## Task 5: Fix SQLite `BeginTransactionRetryDecorator` variant

This one caches `_pipeline` as a field on first call — the shape differs from the uniform case.

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SQLite/Decorator/BeginTransactionRetryDecorator.cs:56-64`
- Create: `Source/DotNetWorkQueue.Transport.SQLite.Tests/Decorator/BeginTransactionRetryDecoratorTests.cs`

**Step 1: Write failing test**

Target shape: construct with disposed registry, call `BeginTransaction()`, assert it did not throw and delegated to `_decorated.BeginTransaction()`. Follow Task 1's test scaffolding.

**Step 2: Run test — RED**

**Step 3: Implement the fix**

Replace `BeginTransaction()`:

```csharp
public IDbTransaction BeginTransaction()
{
    if (_pipeline == null)
    {
        try
        {
            _policies.Registry.TryGetPipeline(TransportPolicyDefinitions.BeginTransaction, out _pipeline);
        }
        catch (ObjectDisposedException)
        {
            // Registry disposed during shutdown; fall through.
        }
    }
    if (_pipeline == null) return _decorated.BeginTransaction();
    return _pipeline.Execute(_ => _decorated.BeginTransaction());
}
```

Add `using System;` if absent.

**Step 4: Run test — GREEN**

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SQLite/Decorator/BeginTransactionRetryDecorator.cs \
        Source/DotNetWorkQueue.Transport.SQLite.Tests/Decorator/BeginTransactionRetryDecoratorTests.cs
git commit -m "fix(sqlite): BeginTransactionRetryDecorator handles disposed Polly registry at shutdown (#121)"
```

---

## Task 6: Regression pass + PR

**Step 1: Full relational unit-test suite**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" && \
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" && \
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" && \
dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj"
```

Expected: PASS on all four.

**Step 2: Open PR**

```bash
gh pr create --base master --title "fix(transports): retry decorators handle disposed Polly registry at shutdown (#121)" \
  --body "Closes #121. Follow-up investigation for deeper disposal-ordering fix filed as #<FOLLOWUP>.

- 13 decorators patched: SqlServer (4), PostgreSQL (4), SQLite (5 incl. BeginTransactionRetryDecorator variant)
- 13 unit tests added covering disposed-registry + happy-path
- All relational unit-test projects green"
```

Replace `<FOLLOWUP>` with the issue number created as pre-work.

---

## Acceptance

- [x] 13 decorator files patched (SqlServer 4 + PostgreSQL 4 + SQLite 5).
- [x] Each decorator has a unit test simulating a disposed `ResiliencePipelineRegistry` and asserting `_decorated.Handle(...)` runs without throwing.
- [x] Each decorator has a happy-path test asserting the registered pipeline is executed when present.
- [x] All 4 relational unit-test projects pass.
- [x] Follow-up issue filed for the deeper disposal-ordering refactor ("related #121").
- [x] PR body references the follow-up issue number.
