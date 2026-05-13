---
phase: foundation-layer
plan: 3.1
wave: 3
dependencies: [1.1, 2.2]
must_haves:
  - IRetrySkippable bypass branch in SqlServer sync RetryCommandHandlerOutputDecorator.Handle()
  - IRetrySkippable bypass branch in SqlServer async RetryCommandHandlerOutputDecoratorAsync.HandleAsync()
  - 2 unit tests confirming the bypass branch fires (sync + async) WITHOUT touching IPolicies.Registry
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs
  - Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs
tdd: true
risk: low
---

# Plan 3.1: SqlServer Retry-Decorator Bypass Branches (Wave 3)

## Context

Wave 3 lands the production retry-decorator bypass branches on the two relational transports. PLAN-3.1 covers SqlServer (sync + async). PLAN-3.2 (parallel, no file overlap) covers PostgreSQL. Both Wave 3 plans depend on Waves 1 + 2 having completed:
- Wave 1 (PLAN-1.1) ships `IRetrySkippable` and `SendMessageCommand.ExternalTransaction`.
- Wave 2 (PLAN-2.2) ships `RelationalSendMessageCommand : SendMessageCommand, IRetrySkippable` which the bypass tests use as the production input shape.

The actual branch addition is mechanical — RESEARCH.md §Section 4 and the Phase 1 PoC at `_SpikePollyBypassPoC.cs` (deleted by PLAN-1.1) already proved the 3-line shape. SqlServer .csproj already references `Transport.RelationalDatabase` (verified — `Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj` line 44), so no `<ProjectReference>` edit is needed.

The 2 unit tests use the **`policies.Received().Registry` property-getter assertion pattern** from Phase 1 SUMMARY-1.1 — `IPolicies.Registry` returns the sealed `ResiliencePipelineRegistry<string>` and is unmockable, so we assert on the `IPolicies` substitute's property-getter call count instead. The bypass branch must fire BEFORE the registry getter is touched, so `DidNotReceiveWithAnyArgs().Registry` proves the bypass happened.

## Dependencies

- PLAN-1.1 (Wave 1) — `IRetrySkippable` must exist.
- PLAN-2.2 (Wave 2) — `RelationalSendMessageCommand` is used as the production input shape in the bypass tests. (Strictly speaking, the tests could use any `IRetrySkippable`-implementing fake, but using the real production class makes the bypass tests an integration of the marker mechanism end-to-end.)

## Tasks

### Task 1: Add bypass branch to SqlServer sync retry decorator

**Files:**
- `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs` (MODIFY)

**Action:** modify

**Description:**
Add a 3-line early-return branch at the top of `Handle()`, after the existing `Guard.NotNull` and before the `ResiliencePipeline pipeline = null;` line. The branch matches the design proven in the Phase 1 PoC (`_SpikePollyBypassPoC._SpikePatchedRetryDecorator.Handle`).

Add `using DotNetWorkQueue.Transport.RelationalDatabase;` to the `using` block (alphabetical insertion — between `DotNetWorkQueue.Transport.Shared` and `DotNetWorkQueue.Transport.SqlServer.Basic`).

Current `Handle()` body (RESEARCH.md verified, lines 49–66):

```csharp
public TOutput Handle(TCommand command)
{
    Guard.NotNull(() => command, command);
    ResiliencePipeline pipeline = null;
    try
    {
        _policies.Registry.TryGetPipeline(TransportPolicyDefinitions.RetryCommandHandler, out pipeline);
    }
    ...
}
```

New `Handle()` body:

```csharp
public TOutput Handle(TCommand command)
{
    Guard.NotNull(() => command, command);

    if (command is IRetrySkippable skippable && skippable.SkipRetry)
        return _decorated.Handle(command);

    ResiliencePipeline pipeline = null;
    try
    {
        _policies.Registry.TryGetPipeline(TransportPolicyDefinitions.RetryCommandHandler, out pipeline);
    }
    ...
}
```

Do not change anything else — the existing no-pipeline branch, shutdown-race catch, and pipeline-execute branches all stay verbatim.

**Acceptance Criteria:**
- File contains exactly one new `using DotNetWorkQueue.Transport.RelationalDatabase;` directive in the `using` block.
- File contains exactly one new branch with the pattern `if (command is IRetrySkippable skippable && skippable.SkipRetry)` immediately after `Guard.NotNull(() => command, command);` in `Handle()`.
- `dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release --nologo` succeeds with 0 warnings.
- Existing 3 tests in `RetryCommandHandlerOutputDecoratorTests.cs` (sync; `Handle_WhenRegistryDisposed_FallsThroughToDecorated`, `Handle_WhenPipelineRegistered_ExecutesThroughPipeline`, `Handle_WhenNoPipelineRegistered_CallsDecoratedDirectly`) still pass — they use `FakeCommand` which does not implement `IRetrySkippable`, so the new branch is skipped and the existing assertions remain valid.

### Task 2: Add bypass branch to SqlServer async retry decorator

**Files:**
- `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` (MODIFY)

**Action:** modify

**Description:**
Mirror Task 1 for the async file. Add `using DotNetWorkQueue.Transport.RelationalDatabase;` and the bypass branch at the top of `HandleAsync()`. The async branch awaits the inner handler:

```csharp
public async Task<TOutput> HandleAsync(TCommand command)
{
    Guard.NotNull(() => command, command);

    if (command is IRetrySkippable skippable && skippable.SkipRetry)
        return await _decorated.HandleAsync(command).ConfigureAwait(false);

    ResiliencePipeline pipeline = null;
    try
    {
        _policies.Registry.TryGetPipeline(TransportPolicyDefinitions.RetryCommandHandlerAsync, out pipeline);
    }
    ...
}
```

Use `.ConfigureAwait(false)` to match the existing async style in the file (lines 65–67 already use it).

**Acceptance Criteria:**
- File contains exactly one new `using DotNetWorkQueue.Transport.RelationalDatabase;` directive.
- File contains exactly one new branch with the pattern `if (command is IRetrySkippable skippable && skippable.SkipRetry)` immediately after `Guard.NotNull(() => command, command);` in `HandleAsync()`.
- Branch return uses `await _decorated.HandleAsync(command).ConfigureAwait(false)`.
- `dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release --nologo` succeeds with 0 warnings.
- Existing 3 tests in `RetryCommandHandlerOutputDecoratorAsyncTests.cs` still pass.

### Task 3: Add bypass-branch unit tests (sync + async)

**Files:**
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` (NEW)

**Action:** test

**Description:**
Create a single new test file containing both the sync and async bypass tests (one file, one `[TestClass]`, two `[TestMethod]` — keeps the bypass-mechanism tests co-located for review). File name distinguishes from the existing `RetryCommandHandlerOutputDecoratorTests.cs` to make the new bypass-coverage intent obvious.

Per CLAUDE.md and Phase 1 SUMMARY-1.1: `IPolicies.Registry` returns the sealed `ResiliencePipelineRegistry<string>`, which NSubstitute cannot intercept. The bypass test pattern is to assert on the `IPolicies` substitute's `Registry` property getter — `DidNotReceiveWithAnyArgs().Registry` proves the bypass branch short-circuited before any pipeline lookup. This is the same pattern used in the Phase 1 PoC (`_SpikePollyBypassPoC.cs` lines 164 and 188).

Construct the production input via `RelationalSendMessageCommand` (PLAN-2.2 Task 1) with a non-null `DbTransaction` mock — that triggers `SkipRetry == true` via `ExternalTransaction != null`. The handler is `Substitute.For<ICommandHandlerWithOutput<SendMessageCommand, long>>()` (sync) or its async equivalent.

```csharp
// (full LGPL header)
using System.Data.Common;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Decorator
{
    /// <summary>
    /// Verifies the IRetrySkippable bypass branch on the SqlServer retry decorator.
    /// When a command implements IRetrySkippable with SkipRetry == true (the production
    /// case: a RelationalSendMessageCommand carrying a caller-supplied DbTransaction),
    /// the decorator must invoke the inner handler exactly once WITHOUT consulting the
    /// Polly pipeline registry.
    /// </summary>
    [TestClass]
    public class RetryCommandHandlerOutputDecoratorBypassTests
    {
        private static RelationalSendMessageCommand BuildCommandWithTx()
        {
            var msg = Substitute.For<IMessage>();
            var data = new AdditionalMessageData();
            var tx = Substitute.For<DbTransaction>();
            return new RelationalSendMessageCommand(msg, data, tx);
        }

        [TestMethod]
        public void Handle_WhenCommandSkipsRetry_InvokesInnerOnce_AndDoesNotAccessRegistry()
        {
            var decorated = Substitute.For<ICommandHandlerWithOutput<SendMessageCommand, long>>();
            decorated.Handle(Arg.Any<SendMessageCommand>()).Returns(42L);
            var policies = Substitute.For<IPolicies>();

            var sut = new RetryCommandHandlerOutputDecorator<SendMessageCommand, long>(decorated, policies);
            var command = BuildCommandWithTx();

            var result = sut.Handle(command);

            Assert.AreEqual(42L, result);
            decorated.Received(1).Handle(command);
            // Property-getter assertion pattern (Phase 1 SUMMARY-1.1 — Registry is the
            // sealed ResiliencePipelineRegistry<string> and is unmockable; assert the
            // getter was never read instead).
            _ = policies.DidNotReceiveWithAnyArgs().Registry;
        }

        [TestMethod]
        public async Task HandleAsync_WhenCommandSkipsRetry_InvokesInnerOnce_AndDoesNotAccessRegistry()
        {
            var decorated = Substitute.For<ICommandHandlerWithOutputAsync<SendMessageCommand, long>>();
            decorated.HandleAsync(Arg.Any<SendMessageCommand>()).Returns(Task.FromResult(42L));
            var policies = Substitute.For<IPolicies>();

            var sut = new RetryCommandHandlerOutputDecoratorAsync<SendMessageCommand, long>(decorated, policies);
            var command = BuildCommandWithTx();

            var result = await sut.HandleAsync(command);

            Assert.AreEqual(42L, result);
            await decorated.Received(1).HandleAsync(command);
            _ = policies.DidNotReceiveWithAnyArgs().Registry;
        }
    }
}
```

Visibility note: `RetryCommandHandlerOutputDecorator<,>` and `RetryCommandHandlerOutputDecoratorAsync<,>` are `internal`. The SqlServer.Tests project must already see them (the existing `RetryCommandHandlerOutputDecoratorTests.cs` constructs them directly — see line 44 in that file). This is via `InternalsVisibleTo` configured in the main project. No new visibility wiring needed.

**Acceptance Criteria:**
- File exists at the path above with LGPL header.
- Test class in namespace `DotNetWorkQueue.Transport.SqlServer.Tests.Decorator` with exactly 2 `[TestMethod]` methods.
- Both tests construct `RelationalSendMessageCommand` with a non-null mocked `DbTransaction`.
- Both tests assert on `policies.DidNotReceiveWithAnyArgs().Registry` (property-getter pattern, NOT method-call assertion on `Registry.TryGetPipeline`).
- Both tests use MSTest 4.x: `Assert.AreEqual` (NOT `Assert.ThrowsException<>`).
- `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~RetryCommandHandlerOutputDecoratorBypassTests" --nologo` reports 2 passed, 0 failed.

## Verification

```bash
# Both decorator files modified
grep -n "IRetrySkippable skippable" Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs
# expected: 1 match line ~53

grep -n "IRetrySkippable skippable" Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs
# expected: 1 match line ~54

# New test file exists
test -f Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs

# SqlServer main project builds clean Release
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release --nologo
# expected: 0 Error(s), 0 Warning(s)

# Bypass tests pass
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~RetryCommandHandlerOutputDecoratorBypassTests" --nologo
# expected: Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2

# All existing SqlServer.Tests decorator tests still pass (no regressions)
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~Decorator" --nologo
# expected: all decorator tests pass (Failed: 0)

# Whole SqlServer.Tests suite still passes
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --nologo
# expected: Failed: 0

# Layering invariant on Transport.RelationalDatabase still holds
grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase/ --include="*.cs" --include="*.csproj"
# expected: no matches (exit code 1)
```
