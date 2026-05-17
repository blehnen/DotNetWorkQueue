---
phase: foundation-layer
plan: 3.2
wave: 3
dependencies: [1.1, 2.2]
must_haves:
  - IRetrySkippable bypass branch in PostgreSQL sync RetryCommandHandlerOutputDecorator.Handle()
  - IRetrySkippable bypass branch in PostgreSQL async RetryCommandHandlerOutputDecoratorAsync.HandleAsync()
  - 2 unit tests confirming the bypass branch fires (sync + async) WITHOUT touching IPolicies.Registry
files_touched:
  - Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs
tdd: true
risk: low
---

# Plan 3.2: PostgreSQL Retry-Decorator Bypass Branches (Wave 3)

## Context

PLAN-3.2 mirrors PLAN-3.1 for PostgreSQL. Same three-task shape, different file paths. Both Wave 3 plans run in parallel — there is **no file overlap with PLAN-3.1**, and both plans depend only on Waves 1 and 2 having completed.

RESEARCH.md §Section 4 verified that the PostgreSQL retry decorators are byte-for-byte equivalent to the SqlServer versions except for the namespace and the policy-definition import (`DotNetWorkQueue.Transport.PostgreSQL.Basic` instead of `DotNetWorkQueue.Transport.SqlServer.Basic`). The bypass branch insertion point is identical: immediately after `Guard.NotNull(() => command, command);`.

PostgreSQL .csproj already references `Transport.RelationalDatabase` (verified — `Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj` line 55), so no `<ProjectReference>` edit is needed.

## Dependencies

- PLAN-1.1 (Wave 1) — `IRetrySkippable` must exist.
- PLAN-2.2 (Wave 2) — `RelationalSendMessageCommand` used by the bypass tests as the production input shape.

(Same dependency shape as PLAN-3.1; the two Wave 3 plans share dependencies but no files.)

## Tasks

### Task 1: Add bypass branch to PostgreSQL sync retry decorator

**Files:**
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs` (MODIFY)

**Action:** modify

**Description:**
Add a 3-line early-return branch at the top of `Handle()`, after `Guard.NotNull` and before `ResiliencePipeline pipeline = null;`. The PostgreSQL file (RESEARCH.md §Section 4, lines 49–67) has identical structure to the SqlServer file — same insertion point.

Add `using DotNetWorkQueue.Transport.RelationalDatabase;` to the existing `using` block. Current using order in the file (RESEARCH.md verified, lines 19–23):
```csharp
using System;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using Polly;
```

Insert `using DotNetWorkQueue.Transport.RelationalDatabase;` in alphabetical order, between `DotNetWorkQueue.Transport.PostgreSQL.Basic` and `DotNetWorkQueue.Transport.Shared`.

Then modify `Handle()`:

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

Do not change anything else.

**Acceptance Criteria:**
- File contains exactly one new `using DotNetWorkQueue.Transport.RelationalDatabase;` directive.
- File contains exactly one new bypass branch with the pattern `if (command is IRetrySkippable skippable && skippable.SkipRetry)` immediately after `Guard.NotNull`.
- `dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release --nologo` succeeds with 0 warnings.
- Existing 3 tests in PostgreSQL `RetryCommandHandlerOutputDecoratorTests.cs` still pass.

### Task 2: Add bypass branch to PostgreSQL async retry decorator

**Files:**
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` (MODIFY)

**Action:** modify

**Description:**
Mirror Task 1 for the async file (RESEARCH.md §Section 4, lines 50–67). Insert the same `using DotNetWorkQueue.Transport.RelationalDatabase;` directive (alphabetical insertion).

Modify `HandleAsync()`:

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

Use `.ConfigureAwait(false)` to match the existing async pattern (the file already uses it on lines 65 and 67).

**Acceptance Criteria:**
- File contains exactly one new `using DotNetWorkQueue.Transport.RelationalDatabase;` directive.
- File contains exactly one new bypass branch with the pattern `if (command is IRetrySkippable skippable && skippable.SkipRetry)` immediately after `Guard.NotNull` in `HandleAsync()`.
- Branch return uses `await _decorated.HandleAsync(command).ConfigureAwait(false)`.
- `dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release --nologo` succeeds with 0 warnings.
- Existing 3 tests in PostgreSQL `RetryCommandHandlerOutputDecoratorAsyncTests.cs` still pass.

### Task 3: Add bypass-branch unit tests (sync + async)

**Files:**
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` (NEW)

**Action:** test

**Description:**
Create a new test file mirroring the SqlServer version (PLAN-3.1 Task 3) — one `[TestClass]`, two `[TestMethod]` (sync + async). The only differences from the SqlServer test file are:
- Namespace: `DotNetWorkQueue.Transport.PostgreSQL.Tests.Decorator`.
- `using DotNetWorkQueue.Transport.PostgreSQL.Decorator;` (replacing the SqlServer using).
- Constructs `RetryCommandHandlerOutputDecorator<,>` / `RetryCommandHandlerOutputDecoratorAsync<,>` from the PostgreSQL namespace.

Same `IPolicies` property-getter assertion pattern (Phase 1 SUMMARY-1.1) — `policies.DidNotReceiveWithAnyArgs().Registry` proves the bypass branch short-circuited before any registry access.

Same production input shape — `RelationalSendMessageCommand` (from `Transport.RelationalDatabase.Basic.Command`) carrying a `Substitute.For<DbTransaction>()`, which exercises `SkipRetry == true` via `ExternalTransaction != null`.

```csharp
// (full LGPL header)
using System.Data.Common;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.PostgreSQL.Decorator;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Decorator
{
    /// <summary>
    /// Verifies the IRetrySkippable bypass branch on the PostgreSQL retry decorator.
    /// Mirrors the SqlServer bypass tests.
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

Visibility note: PostgreSQL decorators are `internal` (same as SqlServer — RESEARCH.md §Section 4). The PostgreSQL.Tests project already constructs them directly (`RetryCommandHandlerOutputDecoratorTests.cs` does this), so `InternalsVisibleTo` is already configured.

**Acceptance Criteria:**
- File exists at the path above with LGPL header.
- Test class in namespace `DotNetWorkQueue.Transport.PostgreSQL.Tests.Decorator` with exactly 2 `[TestMethod]` methods.
- Both tests construct `RelationalSendMessageCommand` with a non-null mocked `DbTransaction`.
- Both tests assert on `policies.DidNotReceiveWithAnyArgs().Registry` (property-getter pattern).
- MSTest 4.x assertions only (`Assert.AreEqual`, no `Assert.ThrowsException<>`).
- `dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~RetryCommandHandlerOutputDecoratorBypassTests" --nologo` reports 2 passed, 0 failed.

## Verification

```bash
# Both decorator files modified
grep -n "IRetrySkippable skippable" Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs
# expected: 1 match line ~54

grep -n "IRetrySkippable skippable" Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs
# expected: 1 match line ~55

# New test file exists
test -f Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs

# PostgreSQL main project builds clean Release
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release --nologo
# expected: 0 Error(s), 0 Warning(s)

# Bypass tests pass
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~RetryCommandHandlerOutputDecoratorBypassTests" --nologo
# expected: Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2

# All existing PostgreSQL.Tests decorator tests still pass
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~Decorator" --nologo
# expected: all decorator tests pass (Failed: 0)

# Whole PostgreSQL.Tests suite still passes
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --nologo
# expected: Failed: 0

# Phase 2 end-to-end: full solution still builds Release (catches any cross-project breakage from PLAN-1.1 + 2.x + 3.x landing together)
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true --nologo
# expected: 0 Error(s), 0 Warning(s) — Release/CI build is the strictest verification

# Layering invariant on Transport.RelationalDatabase still holds
grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase/ --include="*.cs" --include="*.csproj"
# expected: no matches (exit code 1)
```
