---
phase: negative-path-coverage
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - Memory.Tests.csproj gains a ProjectReference to Transport.RelationalDatabase
  - Memory negative-path test asserts ProducerQueue<T> does NOT implement IRelationalProducerQueue<T>
  - Memory negative-path test reflection-scans DotNetWorkQueue.Transport.Memory.dll for any IRelationalProducerQueue<> implementer
  - LiteDb negative-path test asserts ProducerQueue<T> does NOT implement IRelationalProducerQueue<T>
  - LiteDb negative-path test reflection-scans DotNetWorkQueue.Transport.LiteDb.dll for any IRelationalProducerQueue<> implementer
  - All 4 assertions pass; build clean on net10.0 with TreatWarningsAsErrors
files_touched:
  - Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj
  - Source/DotNetWorkQueue.Transport.Memory.Tests/Basic/MemoryProducerDoesNotImplementRelationalTests.cs
  - Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbProducerDoesNotImplementRelationalTests.cs
tdd: false
risk: low
---

# Plan 1.1: Memory + LiteDb Negative-Path Tests (Wave 1)

## Context

Phase 5 ships defensive verification that the 4 non-relational transports (Memory, Redis, LiteDb, SQLite) do NOT accidentally implement the outbox interface `IRelationalProducerQueue<T>` introduced in Phase 2 and exercised by Phases 3 (SqlServer) and 4 (PostgreSQL). This plan covers Memory and LiteDb. PLAN-1.2 (parallel, same wave) covers Redis and SQLite.

Per **RESEARCH §1** none of the 4 non-relational transports define a transport-specific `ProducerQueue` subclass. All 4 resolve `IProducerQueue<T>` through the core fallback `RegisterConditional(typeof(IProducerQueue<>), typeof(ProducerQueue<>), ...)` in `DotNetWorkQueue/IoC/ComponentRegistration.cs:385`. The type-system check therefore targets `typeof(ProducerQueue<TestMessage>)` for both Memory and LiteDb.

Per **CONTEXT-5 Decision 1** each test uses `IsAssignableFrom` (no DI machinery) to bypass the SimpleInjector `EnableAutoVerification` surface that Phase 3 hit. Per **CONTEXT-5 Decision 2** each test also runs a reflection-based assembly assertion confirming no type in the transport's main assembly implements `IRelationalProducerQueue<>` (closed-form generic check via `GetGenericTypeDefinition()`).

Per **RESEARCH §1 Memory note** the Memory reflection assertion anchors on `MemoryDashboardInit` (in `DotNetWorkQueue.Transport.Memory.dll`), NOT `MemoryMessageQueueInit` (which lives in the core `DotNetWorkQueue.dll` and would be a category error — every transport shares core).

Per **RESEARCH §Critical Project Reference Requirements** `DotNetWorkQueue.Transport.Memory.Tests.csproj` does NOT currently reference `Transport.RelationalDatabase` and MUST gain a `<ProjectReference>` before any test code can compile against `IRelationalProducerQueue<>`. `LiteDb.Tests.csproj` already has the reference (researcher confirmed).

## Dependencies

None within Phase 5. Phase 2 (foundation: `IRelationalProducerQueue<T>` interface) + Phase 3 (SqlServer) + Phase 4 (PostgreSQL) all shipped. Phase 5 confirms the absence of relational-producer types on the OTHER transports.

## Tasks

### Task 1: Memory — add project reference + negative-path test

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj`
- Create: `Source/DotNetWorkQueue.Transport.Memory.Tests/Basic/MemoryProducerDoesNotImplementRelationalTests.cs`

**Action:** modify (csproj) + create (test file)

**Description:**

**Step 1: Add ProjectReference to Memory.Tests.csproj**

The current Memory.Tests.csproj `<ItemGroup>` for project refs contains 2 entries (`Transport.Memory` + core `DotNetWorkQueue`). Add a third entry referencing `Transport.RelationalDatabase`. Final state of the project-ref ItemGroup:

```xml
  <ItemGroup>
    <ProjectReference Include="..\DotNetWorkQueue.Transport.Memory\DotNetWorkQueue.Transport.Memory.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue\DotNetWorkQueue.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue.Transport.RelationalDatabase\DotNetWorkQueue.Transport.RelationalDatabase.csproj" />
  </ItemGroup>
```

**Step 2: Create the test file**

File: `Source/DotNetWorkQueue.Transport.Memory.Tests/Basic/MemoryProducerDoesNotImplementRelationalTests.cs`

Write verbatim:

```csharp
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System.Linq;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Memory.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic
{
    /// <summary>
    /// Phase 5 negative-path coverage: the Memory transport is non-relational and MUST NOT
    /// accidentally implement the outbox interface <see cref="IRelationalProducerQueue{T}"/>
    /// shipped in Phase 2 and consumed by Phases 3 (SqlServer) and 4 (PostgreSQL).
    /// </summary>
    [TestClass]
    public class MemoryProducerDoesNotImplementRelationalTests
    {
        private sealed class TestMessage
        {
            public string Body { get; set; }
        }

        [TestMethod]
        public void Memory_ProducerQueue_DoesNotImplement_IRelationalProducerQueue()
        {
            // Decision 1: type-system check. Memory resolves IProducerQueue<T> via the core
            // fallback registration to ProducerQueue<T>; that type must NOT implement the
            // relational outbox interface.
            Assert.IsFalse(
                typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(ProducerQueue<TestMessage>)),
                "Memory transport invariant violated: ProducerQueue<T> must NOT implement " +
                "IRelationalProducerQueue<T>. Memory is a non-relational transport with no " +
                "external-transaction outbox surface.");

            // Decision 2: reflection-based assembly assertion. Scan the Memory-specific NuGet
            // assembly (anchored on MemoryDashboardInit — RESEARCH §1) for ANY type implementing
            // the closed- or open-generic form of IRelationalProducerQueue<>.
            var transportAssembly = typeof(MemoryDashboardInit).Assembly;
            var allTypes = transportAssembly.GetTypes();
            var anyImplementsRelational = allTypes.Any(t =>
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRelationalProducerQueue<>)));
            Assert.IsFalse(anyImplementsRelational,
                $"Memory transport invariant violated: assembly " +
                $"'{transportAssembly.GetName().Name}' must NOT contain any type " +
                "implementing IRelationalProducerQueue<T>.");
        }
    }
}
```

**Acceptance Criteria:**
- `Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj` contains the new `<ProjectReference>` for `Transport.RelationalDatabase`.
- New file `Source/DotNetWorkQueue.Transport.Memory.Tests/Basic/MemoryProducerDoesNotImplementRelationalTests.cs` exists with the verbatim content above.
- `dotnet build "Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj" -c Release` succeeds (TreatWarningsAsErrors clean).
- `dotnet test "Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj" --filter "FullyQualifiedName~MemoryProducerDoesNotImplementRelationalTests"` reports **1 passed, 0 failed**.
- No pre-existing Memory.Tests test is broken (full suite still green).

---

### Task 2: LiteDb — negative-path test

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbProducerDoesNotImplementRelationalTests.cs`

**Action:** create

**Description:**

LiteDb.Tests.csproj already references `Transport.RelationalDatabase` (RESEARCH §Critical Project Reference Requirements), so no `.csproj` change is needed. The init class is `LiteDbMessageQueueInit` in namespace `DotNetWorkQueue.Transport.LiteDb.Basic` (RESEARCH §1 — note lowercase 'b' in 'LiteDb', the directory casing is `LiteDB` but the namespace is `LiteDb`).

File: `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbProducerDoesNotImplementRelationalTests.cs`

Write verbatim:

```csharp
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System.Linq;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic
{
    /// <summary>
    /// Phase 5 negative-path coverage: the LiteDb transport is non-relational and MUST NOT
    /// accidentally implement the outbox interface <see cref="IRelationalProducerQueue{T}"/>
    /// shipped in Phase 2 and consumed by Phases 3 (SqlServer) and 4 (PostgreSQL).
    /// </summary>
    [TestClass]
    public class LiteDbProducerDoesNotImplementRelationalTests
    {
        private sealed class TestMessage
        {
            public string Body { get; set; }
        }

        [TestMethod]
        public void LiteDb_ProducerQueue_DoesNotImplement_IRelationalProducerQueue()
        {
            // Decision 1: type-system check. LiteDb resolves IProducerQueue<T> via the core
            // fallback registration to ProducerQueue<T>; that type must NOT implement the
            // relational outbox interface.
            Assert.IsFalse(
                typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(ProducerQueue<TestMessage>)),
                "LiteDb transport invariant violated: ProducerQueue<T> must NOT implement " +
                "IRelationalProducerQueue<T>. LiteDb is a non-relational transport with no " +
                "external-transaction outbox surface.");

            // Decision 2: reflection-based assembly assertion. Scan the LiteDb transport
            // assembly (anchored on LiteDbMessageQueueInit — RESEARCH §1) for ANY type
            // implementing the closed- or open-generic form of IRelationalProducerQueue<>.
            var transportAssembly = typeof(LiteDbMessageQueueInit).Assembly;
            var allTypes = transportAssembly.GetTypes();
            var anyImplementsRelational = allTypes.Any(t =>
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRelationalProducerQueue<>)));
            Assert.IsFalse(anyImplementsRelational,
                $"LiteDb transport invariant violated: assembly " +
                $"'{transportAssembly.GetName().Name}' must NOT contain any type " +
                "implementing IRelationalProducerQueue<T>.");
        }
    }
}
```

**Acceptance Criteria:**
- New file `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbProducerDoesNotImplementRelationalTests.cs` exists with the verbatim content above.
- `dotnet build "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" -c Release` succeeds (TreatWarningsAsErrors clean).
- `dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~LiteDbProducerDoesNotImplementRelationalTests"` reports **1 passed, 0 failed**.
- No pre-existing LiteDb.Tests test is broken (full suite still green).

## Verification

```bash
# Build both affected test projects in Release (TreatWarningsAsErrors enabled).
dotnet build "Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj" -c Release
dotnet build "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" -c Release

# Run the 2 new negative-path tests by name.
dotnet test "Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj" \
  --filter "FullyQualifiedName~MemoryProducerDoesNotImplementRelationalTests"
dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" \
  --filter "FullyQualifiedName~LiteDbProducerDoesNotImplementRelationalTests"

# Confirm no regressions in the full unit-test suites for both projects.
dotnet test "Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj"
dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj"
```

Expected: both filtered runs report **1 passed**. Both full suites stay green (same count as pre-change baseline).
