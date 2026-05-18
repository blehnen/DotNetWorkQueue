# Plan 1.1: Negative-Path Coverage on Memory/Redis/LiteDb

## Context

Defensive phase: assert capability-cast pattern correctly fails on Memory, Redis, and LiteDb (PROJECT.md §SC #3). No production code change expected — pure verification that Phase 2's `IRelationalWorkerNotification` interface didn't accidentally leak into the three non-relational transport assemblies.

## Dependencies
None (depends on Phases 3-5 completion, which is satisfied).

## Tasks

### Task 1: Memory transport negative-path test
**Files:** `Source/DotNetWorkQueue.Transport.Memory.Tests/Basic/MemoryDoesNotImplementIRelationalWorkerNotificationTests.cs`
**Action:** create
**Description:**

Create `[TestClass] MemoryDoesNotImplementIRelationalWorkerNotificationTests` with two `[TestMethod]`s:

1. **`WorkerNotification_Does_Not_Implement_IRelationalWorkerNotification`** — type-system check:
   ```csharp
   Assert.IsFalse(typeof(IRelationalWorkerNotification).IsAssignableFrom(typeof(WorkerNotification)),
       "Core WorkerNotification (used by Memory transport) must NOT implement IRelationalWorkerNotification.");
   ```

2. **`Memory_Transport_Assembly_Contains_No_IRelationalWorkerNotification_Implementor`** — assembly-scan invariant:
   ```csharp
   var transportAssembly = typeof(MemoryMessageQueueInit).Assembly;
   var anyImplementsRelational = transportAssembly.GetTypes()
       .Any(t => typeof(IRelationalWorkerNotification).IsAssignableFrom(t));
   Assert.IsFalse(anyImplementsRelational,
       "Memory transport assembly must NOT contain any type implementing IRelationalWorkerNotification.");
   ```

Note: `MemoryMessageQueueInit.Assembly` resolves to the core `DotNetWorkQueue` assembly (Memory transport lives in core). The assembly-scan therefore covers the entire core surface — confirming `IRelationalWorkerNotification` (which lives in `Transport.RelationalDatabase`) isn't accidentally implemented in core.

Usings: `System.Linq`, `System.Reflection` (if needed), `DotNetWorkQueue.Queue` (for `WorkerNotification`), `DotNetWorkQueue.Transport.Memory.Basic` (for `MemoryMessageQueueInit`), `DotNetWorkQueue.Transport.RelationalDatabase`, `Microsoft.VisualStudio.TestTools.UnitTesting`.

LGPL-2.1 18-line header.

**Acceptance Criteria:**
- File exists; 2 tests pass.
- No `Tx` token in file.

### Task 2: Redis transport negative-path test
**Files:** `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/RedisDoesNotImplementIRelationalWorkerNotificationTests.cs`
**Action:** create
**Description:**

Mirror Task 1 with Redis-specific anchors:
- Anchor type for assembly scan: `RedisQueueInit` (from `DotNetWorkQueue.Transport.Redis.Basic`).
- Test class: `RedisDoesNotImplementIRelationalWorkerNotificationTests`.
- Otherwise identical 2-test shape.

**Acceptance Criteria:**
- File exists; 2 tests pass.

### Task 3: LiteDb transport negative-path test + final grep gate
**Files:**
- `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbDoesNotImplementIRelationalWorkerNotificationTests.cs` (create)

**Action:** create

**Description:**

Mirror Task 1 with LiteDb-specific anchors:
- Anchor type for assembly scan: `LiteDbMessageQueueInit` (from `DotNetWorkQueue.Transport.LiteDb.Basic`).
- Test class: `LiteDbDoesNotImplementIRelationalWorkerNotificationTests`.
- Otherwise identical 2-test shape.

**Acceptance Criteria:**
- File exists; 2 tests pass.

## Verification

```bash
# Gate 1: Release build all 3 transports.
dotnet build "Source/DotNetWorkQueue.Transport.Memory/DotNetWorkQueue.Transport.Memory.csproj" -c Release -p:CI=true --nologo 2>&1 | tail -3
dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" -c Release -p:CI=true --nologo 2>&1 | tail -3
dotnet build "Source/DotNetWorkQueue.Transport.LiteDb/DotNetWorkQueue.Transport.LiteDb.csproj" -c Release -p:CI=true --nologo 2>&1 | tail -3
# expect: 0 errors on each.

# Gate 2: All 3 negative-path test suites pass.
dotnet test "Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj" --filter "FullyQualifiedName~MemoryDoesNotImplementIRelationalWorkerNotificationTests" --nologo 2>&1 | tail -3
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~RedisDoesNotImplementIRelationalWorkerNotificationTests" --nologo 2>&1 | tail -3
dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~LiteDbDoesNotImplementIRelationalWorkerNotificationTests" --nologo 2>&1 | tail -3
# expect: 2/2 pass on each.

# Gate 3: source grep guard — zero references to IRelationalWorkerNotification in the 3 non-relational transport assemblies.
grep -rln "IRelationalWorkerNotification" Source/DotNetWorkQueue.Transport.Memory Source/DotNetWorkQueue.Transport.Redis Source/DotNetWorkQueue.Transport.LiteDb --include="*.cs"
# expect: exit code 1 (no matches; test files don't live in production source paths so don't trigger).

# Gate 4: full test suites still green (regression check).
dotnet test "Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj" --nologo 2>&1 | tail -3
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --nologo 2>&1 | tail -3
dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --nologo 2>&1 | tail -3
# expect: 0 failures on each.
```
