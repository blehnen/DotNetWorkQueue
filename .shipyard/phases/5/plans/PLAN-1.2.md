---
phase: negative-path-coverage
plan: 1.2
wave: 1
dependencies: []
must_haves:
  - Redis.Tests.csproj gains a ProjectReference to Transport.RelationalDatabase
  - Redis negative-path test asserts ProducerQueue<T> does NOT implement IRelationalProducerQueue<T>
  - Redis negative-path test reflection-scans DotNetWorkQueue.Transport.Redis.dll for any IRelationalProducerQueue<> implementer
  - SQLite negative-path test asserts ProducerQueue<T> does NOT implement IRelationalProducerQueue<T>
  - SQLite negative-path test reflection-scans DotNetWorkQueue.Transport.SQLite.dll for any IRelationalProducerQueue<> implementer
  - SQLite negative-path test carries the Decision-4 extra assertion that ProducerQueue<T> does NOT derive from RelationalProducerQueue<T>
  - All 5 assertions pass; build clean on net10.0 with TreatWarningsAsErrors
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj
  - Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/RedisProducerDoesNotImplementRelationalTests.cs
  - Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteProducerDoesNotImplementRelationalTests.cs
tdd: false
risk: low
---

# Plan 1.2: Redis + SQLite Negative-Path Tests (Wave 1)

## Context

Phase 5 ships defensive verification that the 4 non-relational transports do NOT accidentally implement the outbox interface `IRelationalProducerQueue<T>`. This plan covers Redis and SQLite. PLAN-1.1 (parallel, same wave) covers Memory and LiteDb.

Per **RESEARCH §1** none of the 4 non-relational transports define a transport-specific `ProducerQueue` subclass. All 4 resolve `IProducerQueue<T>` through the core fallback in `DotNetWorkQueue/IoC/ComponentRegistration.cs:385`. The type-system check therefore targets `typeof(ProducerQueue<TestMessage>)` for both Redis and SQLite.

Per **CONTEXT-5 Decision 4** SQLite carries one **extra** assertion in addition to the two standard ones: it confirms `ProducerQueue<T>` does NOT derive from `RelationalProducerQueue<T>`. Rationale: SQLite is the closest non-relational transport in shape to the relational ones (ROADMAP §Phase 5 explicitly flags it as the "deferred relational case"). The extra assertion catches a hypothetical "accidentally inherits from `RelationalProducerQueue<T>` base" misconfiguration. Per **RESEARCH §4** the assertion is trivially `false` by the type hierarchy (`ProducerQueue<T>` is the BASE of `RelationalProducerQueue<T>`, not a subclass), so the test is sound — but the value is regression detection: if a future change accidentally introduces a SQLite-specific subclass that derives from `RelationalProducerQueue<T>`, the assertion would flip.

Per **RESEARCH §Critical Project Reference Requirements** `DotNetWorkQueue.Transport.Redis.Tests.csproj` does NOT currently reference `Transport.RelationalDatabase` and MUST gain a `<ProjectReference>` before the test code can compile. `SQLite.Tests.csproj` already has the reference (researcher confirmed).

Per **RESEARCH §1 / §Risks** note the SQLite init class name uses camelCase `SqLiteMessageQueueInit` (lowercase 'q', capital 'L') in namespace `DotNetWorkQueue.Transport.SQLite.Basic`. The directory is `SQLite` (all-caps) but the type uses `SqLite` casing. Builder must reference the exact name.

## Dependencies

None within Phase 5. Phase 2 + Phase 3 + Phase 4 all shipped.

## Tasks

### Task 1: Redis — add project reference + negative-path test

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj`
- Create: `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/RedisProducerDoesNotImplementRelationalTests.cs`

**Action:** modify (csproj) + create (test file)

**Description:**

**Step 1: Add ProjectReference to Redis.Tests.csproj**

The current Redis.Tests.csproj project-ref `<ItemGroup>` contains 2 entries (`Transport.Redis` + core `DotNetWorkQueue`). Add a third entry. Final state of the project-ref ItemGroup:

```xml
  <ItemGroup>
    <ProjectReference Include="..\DotNetWorkQueue.Transport.Redis\DotNetWorkQueue.Transport.Redis.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue\DotNetWorkQueue.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue.Transport.RelationalDatabase\DotNetWorkQueue.Transport.RelationalDatabase.csproj" />
  </ItemGroup>
```

**Step 2: Create the test file**

File: `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/RedisProducerDoesNotImplementRelationalTests.cs`

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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    /// <summary>
    /// Phase 5 negative-path coverage: the Redis transport is non-relational and MUST NOT
    /// accidentally implement the outbox interface <see cref="IRelationalProducerQueue{T}"/>
    /// shipped in Phase 2 and consumed by Phases 3 (SqlServer) and 4 (PostgreSQL).
    /// </summary>
    [TestClass]
    public class RedisProducerDoesNotImplementRelationalTests
    {
        private sealed class TestMessage
        {
            public string Body { get; set; }
        }

        [TestMethod]
        public void Redis_ProducerQueue_DoesNotImplement_IRelationalProducerQueue()
        {
            // Decision 1: type-system check. Redis resolves IProducerQueue<T> via the core
            // fallback registration to ProducerQueue<T>; that type must NOT implement the
            // relational outbox interface.
            Assert.IsFalse(
                typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(ProducerQueue<TestMessage>)),
                "Redis transport invariant violated: ProducerQueue<T> must NOT implement " +
                "IRelationalProducerQueue<T>. Redis is a non-relational transport with no " +
                "external-transaction outbox surface.");

            // Decision 2: reflection-based assembly assertion. Scan the Redis transport
            // assembly (anchored on RedisQueueInit — RESEARCH §1) for ANY type implementing
            // the closed- or open-generic form of IRelationalProducerQueue<>.
            var transportAssembly = typeof(RedisQueueInit).Assembly;
            var allTypes = transportAssembly.GetTypes();
            var anyImplementsRelational = allTypes.Any(t =>
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRelationalProducerQueue<>)));
            Assert.IsFalse(anyImplementsRelational,
                $"Redis transport invariant violated: assembly " +
                $"'{transportAssembly.GetName().Name}' must NOT contain any type " +
                "implementing IRelationalProducerQueue<T>.");
        }
    }
}
```

**Acceptance Criteria:**
- `Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj` contains the new `<ProjectReference>` for `Transport.RelationalDatabase`.
- New file `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/RedisProducerDoesNotImplementRelationalTests.cs` exists with the verbatim content above.
- `dotnet build "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" -c Release` succeeds (TreatWarningsAsErrors clean).
- `dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~RedisProducerDoesNotImplementRelationalTests"` reports **1 passed, 0 failed**.
- No pre-existing Redis.Tests test is broken (full suite still green).

---

### Task 2: SQLite — negative-path test with Decision-4 extra assertion

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteProducerDoesNotImplementRelationalTests.cs`

**Action:** create

**Description:**

SQLite.Tests.csproj already references `Transport.RelationalDatabase` (RESEARCH §Critical), so no `.csproj` change is needed. The init class is `SqLiteMessageQueueInit` (RESEARCH §1 — note exact camelCase: `SqLite` not `SQLite` for the type name, even though the directory and namespace tail use `SQLite`). The namespace is `DotNetWorkQueue.Transport.SQLite.Basic`.

This test carries THREE assertions:
1. **Decision 1:** type-system check that `ProducerQueue<T>` does NOT implement `IRelationalProducerQueue<T>`.
2. **Decision 2:** reflection-based assembly assertion that no type in `DotNetWorkQueue.Transport.SQLite.dll` implements `IRelationalProducerQueue<>`.
3. **Decision 4 (SQLite-only extra):** `ProducerQueue<T>` does NOT derive from `RelationalProducerQueue<T>`.

File: `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteProducerDoesNotImplementRelationalTests.cs`

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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// Phase 5 negative-path coverage: the SQLite transport is the closest non-relational
    /// transport in shape to the SqlServer/PostgreSQL relational transports (ROADMAP §Phase 5
    /// flags SQLite as the explicitly-deferred relational case). The Decision-4 extra
    /// assertion below specifically guards against an accidental inheritance from
    /// <see cref="RelationalProducerQueue{T}"/>.
    /// </summary>
    [TestClass]
    public class SqliteProducerDoesNotImplementRelationalTests
    {
        private sealed class TestMessage
        {
            public string Body { get; set; }
        }

        [TestMethod]
        public void Sqlite_ProducerQueue_DoesNotImplement_IRelationalProducerQueue()
        {
            // Decision 1: type-system check. SQLite resolves IProducerQueue<T> via the core
            // fallback registration to ProducerQueue<T>; that type must NOT implement the
            // relational outbox interface.
            Assert.IsFalse(
                typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(ProducerQueue<TestMessage>)),
                "SQLite transport invariant violated: ProducerQueue<T> must NOT implement " +
                "IRelationalProducerQueue<T>. SQLite's outbox surface is explicitly deferred.");

            // Decision 2: reflection-based assembly assertion. Scan the SQLite transport
            // assembly (anchored on SqLiteMessageQueueInit — RESEARCH §1) for ANY type
            // implementing the closed- or open-generic form of IRelationalProducerQueue<>.
            var transportAssembly = typeof(SqLiteMessageQueueInit).Assembly;
            var allTypes = transportAssembly.GetTypes();
            var anyImplementsRelational = allTypes.Any(t =>
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRelationalProducerQueue<>)));
            Assert.IsFalse(anyImplementsRelational,
                $"SQLite transport invariant violated: assembly " +
                $"'{transportAssembly.GetName().Name}' must NOT contain any type " +
                "implementing IRelationalProducerQueue<T>.");

            // Decision 4 (SQLite-only extra): SQLite is the explicitly-deferred relational
            // case (ROADMAP §Phase 5). Defend against the "accidentally inherits from
            // RelationalProducerQueue<T>" misconfiguration. By construction this asserts that
            // ProducerQueue<T> is NOT assignable to RelationalProducerQueue<T> — the
            // inheritance goes the other way (RelationalProducerQueue<T> : ProducerQueue<T>),
            // so this assertion is true by the current type hierarchy. The regression value
            // is future-proofing: if a SQLite-specific subclass is ever introduced that
            // derives from RelationalProducerQueue<T>, the assembly assertion above would
            // also catch it — this extra check makes the SQLite-specific intent explicit.
            Assert.IsFalse(
                typeof(RelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(ProducerQueue<TestMessage>)),
                "SQLite transport invariant violated: ProducerQueue<T> must NOT derive from " +
                "RelationalProducerQueue<T>. SQLite's outbox surface is explicitly deferred " +
                "and must not accidentally pick up the relational base.");
        }
    }
}
```

**Acceptance Criteria:**
- New file `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteProducerDoesNotImplementRelationalTests.cs` exists with the verbatim content above.
- Test method contains exactly **3** `Assert.IsFalse` calls (Decision 1, Decision 2, Decision 4).
- `dotnet build "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" -c Release` succeeds (TreatWarningsAsErrors clean).
- `dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --filter "FullyQualifiedName~SqliteProducerDoesNotImplementRelationalTests"` reports **1 passed, 0 failed**.
- No pre-existing SQLite.Tests test is broken (full suite still green).

## Verification

```bash
# Build both affected test projects in Release (TreatWarningsAsErrors enabled).
dotnet build "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" -c Release
dotnet build "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" -c Release

# Run the 2 new negative-path tests by name.
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" \
  --filter "FullyQualifiedName~RedisProducerDoesNotImplementRelationalTests"
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" \
  --filter "FullyQualifiedName~SqliteProducerDoesNotImplementRelationalTests"

# Confirm no regressions in the full unit-test suites for both projects.
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj"
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj"
```

Expected: both filtered runs report **1 passed**. Both full suites stay green (same count as pre-change baseline).
