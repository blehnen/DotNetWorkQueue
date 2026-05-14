# Research: Phase 5 — Negative-Path Coverage on Non-Relational Transports

## Context

Phase 5 ships 4 negative-path unit tests (one per non-relational transport) asserting that
`IProducerQueue<T>` on Memory, Redis, LiteDb, and SQLite does NOT satisfy
`IRelationalProducerQueue<T>`. Each test also runs a reflection-based assembly assertion
confirming no type in the transport's main assembly implements `IRelationalProducerQueue<T>`.
SQLite gets one extra assertion confirming its producer does not derive from
`RelationalProducerQueue<T>` base class.

No production code changes. Test code only.

---

## §1 — Concrete Producer-Queue Type Names + Init Class Names

### Key Finding: No Transport-Specific ProducerQueue Subclass Exists in Any Non-Relational Transport

A grep across `Source/` for `: ProducerQueue|: RelationalProducerQueue|class.*ProducerQueue` returned
only these hits (abridged to relevant entries):

```
Source/DotNetWorkQueue/Queue/ProducerQueue.cs
    public class ProducerQueue<T> : IProducerQueue<T>

Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs
    public class RelationalProducerQueue<T> : ProducerQueue<T>, IRelationalProducerQueue<T>

Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalProducerQueue.cs
    public sealed class SqlServerRelationalProducerQueue<TMessage> : RelationalProducerQueue<TMessage>

Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs
    public sealed class PostgreSqlRelationalProducerQueue<TMessage> : RelationalProducerQueue<TMessage>
```

**None of the 4 non-relational transports define a transport-specific ProducerQueue subclass.**

All 4 resolve `IProducerQueue<T>` through the fallback registration in
`DotNetWorkQueue/IoC/ComponentRegistration.cs` line 385:

```csharp
container.RegisterConditional(typeof(IProducerQueue<>), typeof(ProducerQueue<>),
    LifeStyles.Singleton);
```

This means the type-system check uses `typeof(ProducerQueue<TestMessage>)` for ALL four transports.

### Per-Transport Summary Table

| Transport | Concrete producer type | Assembly for reflection | Init class | Init namespace |
|-----------|----------------------|------------------------|-----------|----------------|
| Memory | `DotNetWorkQueue.Queue.ProducerQueue<T>` (core fallback) | `typeof(MemoryDashboardInit).Assembly` | `MemoryDashboardInit` | `DotNetWorkQueue.Transport.Memory.Basic` |
| Redis | `DotNetWorkQueue.Queue.ProducerQueue<T>` (core fallback) | `typeof(RedisQueueInit).Assembly` | `RedisQueueInit` | `DotNetWorkQueue.Transport.Redis.Basic` |
| LiteDb | `DotNetWorkQueue.Queue.ProducerQueue<T>` (core fallback) | `typeof(LiteDbMessageQueueInit).Assembly` | `LiteDbMessageQueueInit` | `DotNetWorkQueue.Transport.LiteDb.Basic` |
| SQLite | `DotNetWorkQueue.Queue.ProducerQueue<T>` (core fallback) | `typeof(SqLiteMessageQueueInit).Assembly` | `SqLiteMessageQueueInit` | `DotNetWorkQueue.Transport.SQLite.Basic` |

### Memory Transport Assembly Note

The `DotNetWorkQueue.Transport.Memory` NuGet assembly is a **thin Dashboard-extension layer**.
The primary Memory transport code (including `MemoryMessageQueueInit`) lives in the **core**
`DotNetWorkQueue` assembly under `Source/DotNetWorkQueue/Transport/Memory/Basic/MemoryMessageQueueInit.cs`
(namespace `DotNetWorkQueue.Transport.Memory.Basic`). The only public init type in the
`DotNetWorkQueue.Transport.Memory` binary is `MemoryDashboardInit`, which inherits from
`MemoryMessageQueueInit`.

For the reflection-based assembly assertion, the test must choose which assembly to scan:
- `typeof(MemoryDashboardInit).Assembly` → scans `DotNetWorkQueue.Transport.Memory.dll` (dashboard handlers only — the narrower, safer scan for the Memory-specific assembly invariant)
- `typeof(MemoryMessageQueueInit).Assembly` → scans the core `DotNetWorkQueue.dll` (much broader; tests the core assembly instead of the Memory transport assembly)

**Architect decision required:** The ROADMAP says "non-relational transport assemblies do not
reference `IRelationalProducerQueue<T>`". For Memory, the meaningful transport assembly for this
invariant is `DotNetWorkQueue.Transport.Memory.dll` (since that is the Memory-specific DLL).
Use `typeof(MemoryDashboardInit).Assembly` to scan it. The core assembly scan would be a
category error (every transport shares the core assembly).

---

## §2 — Test Project Paths

All 4 non-relational test projects follow the `Basic/` subdirectory convention:

| Transport | Test project path | New file location |
|-----------|-------------------|-------------------|
| Memory | `Source/DotNetWorkQueue.Transport.Memory.Tests/` | `Basic/MemoryProducerQueueDoesNotImplementRelationalTests.cs` |
| Redis | `Source/DotNetWorkQueue.Transport.Redis.Tests/` | `Basic/RedisProducerQueueDoesNotImplementRelationalTests.cs` |
| LiteDb | `Source/DotNetWorkQueue.Transport.LiteDb.Tests/` | `Basic/LiteDbProducerQueueDoesNotImplementRelationalTests.cs` |
| SQLite | `Source/DotNetWorkQueue.Transport.SQLite.Tests/` | `Basic/SQLiteProducerQueueDoesNotImplementRelationalTests.cs` |

All test projects target **net10.0 only** (confirmed from their `.csproj` files).

---

## §3 — IRelationalProducerQueue Grep Result on Non-Relational Transport Source

Command: `grep -rn "IRelationalProducerQueue"` across:
- `Source/DotNetWorkQueue.Transport.Memory/`
- `Source/DotNetWorkQueue.Transport.Redis/`
- `Source/DotNetWorkQueue.Transport.LiteDB/`
- `Source/DotNetWorkQueue.Transport.SQLite/`

**Result: empty — zero matches.**

Phase 2/3/4 correctly did not introduce any `IRelationalProducerQueue` references into these 4
transport projects. The §3 invariant holds. No Phase 5 blocking surprise here.

---

## §4 — SQLite Producer Base Class Confirmation

SQLite has no transport-specific `ProducerQueue` subclass. The search for
`: RelationalProducerQueue` or `: ProducerQueue` in `Source/DotNetWorkQueue.Transport.SQLite/`
returned **zero matches**.

SQLite uses the same core `ProducerQueue<T>` fallback as Memory, Redis, and LiteDb. The extra
Decision-4 assertion in the SQLite test (`typeof(RelationalProducerQueue<TestMessage>).IsAssignableFrom(typeof(ProducerQueue<TestMessage>))`)
will trivially be `false` because `ProducerQueue<T>` is the BASE of `RelationalProducerQueue<T>`,
not a subclass.

**Architect note:** The Decision-4 assertion as written in CONTEXT-5 is technically sound — it
confirms `ProducerQueue<T>` does NOT derive from `RelationalProducerQueue<T>` (the inheritance
goes the other way: `RelationalProducerQueue<T>` extends `ProducerQueue<T>`). The assertion
`Assert.IsFalse(typeof(RelationalProducerQueue<TestMessage>).IsAssignableFrom(typeof(ProducerQueue<TestMessage>)))` is `true` by the type hierarchy — `ProducerQueue<T>` is NOT assignable to `RelationalProducerQueue<T>`. The test is correct as specified.

---

## §5 — MSTest Availability in Non-Relational Test Projects

All 4 test projects declare `MSTest.TestFramework` and `MSTest.TestAdapter` in their `.csproj`
files. Existing tests use `[TestClass]`, `[TestMethod]`, `Assert.ThrowsExactly<T>` (MSTest 4.x
pattern), and `Assert.IsNotNull`. No new NuGet dependencies are needed.

`Microsoft.VisualStudio.TestTools.UnitTesting` is available via the existing `MSTest.TestFramework`
reference in all 4 projects.

---

## §6 — Existing Test Patterns (Representative Samples)

### Memory.Tests pattern
File: `Basic/CommandHandler/DashboardDeleteMessageCommandHandlerTests.cs`
```csharp
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic.CommandHandler;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.CommandHandler
{
    [TestClass]
    public class DashboardDeleteMessageCommandHandlerTests
    {
        [TestMethod]
        public void Create_Default() { ... }

        [TestMethod]
        public void Create_NullDataStorage_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DashboardDeleteMessageCommandHandler(null));
        }
    }
}
```
Namespace convention: `DotNetWorkQueue.Transport.Memory.Tests.Basic.{Subdirectory}`

### Redis.Tests pattern
File: `Basic/RedisMessageTests.cs`
```csharp
using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisMessageTests
    {
        [TestMethod]
        public void Create_Null_Message_OK() { ... }
    }
}
```
Namespace convention: `DotNetWorkQueue.Transport.Redis.Tests.Basic`

### LiteDb.Tests pattern
File: `Basic/LiteDbJobSchedulerLastKnownEventTests.cs`
```csharp
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.Shared;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic
{
    [TestClass]
    public class LiteDbJobSchedulerLastKnownEventTests
    {
        [TestMethod]
        public void Create_Default() { ... }
    }
}
```
Namespace convention: `DotNetWorkQueue.Transport.LiteDb.Tests.Basic`

### SQLite.Tests pattern
File: `Basic/SqliteJobSchemaTests.cs`
```csharp
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class SqliteJobSchemaTests
    {
        [TestMethod]
        public void GetSchema_ReturnsExactlyOneTable() { ... }
    }
}
```
Namespace convention: `DotNetWorkQueue.Transport.SQLite.Tests.Basic`

---

## Critical: Project Reference Requirements

### PLAN-BLOCKING FINDING

To use `IRelationalProducerQueue<T>` and `RelationalProducerQueue<T>` in the negative-path
assertions, the test projects need access to `DotNetWorkQueue.Transport.RelationalDatabase.dll`.

Current state:

| Test project | Has `Transport.RelationalDatabase` reference? |
|-------------|----------------------------------------------|
| `DotNetWorkQueue.Transport.Memory.Tests` | **NO** — must add `<ProjectReference>` |
| `DotNetWorkQueue.Transport.Redis.Tests` | **NO** — must add `<ProjectReference>` |
| `DotNetWorkQueue.Transport.LiteDb.Tests` | YES (already present) |
| `DotNetWorkQueue.Transport.SQLite.Tests` | YES (already present) |

PLAN-1.1 (Memory + LiteDb) and PLAN-1.2 (Redis + SQLite) each require the architect to add a
`<ProjectReference Include="..\DotNetWorkQueue.Transport.RelationalDatabase\DotNetWorkQueue.Transport.RelationalDatabase.csproj" />`
to `Memory.Tests.csproj` and `Redis.Tests.csproj` respectively.

**This is a `.csproj` edit, not a NuGet dependency — it does not violate CONTEXT-5's "no new
NuGet dependencies" hard rule.** However, the architect must note this as an additional task
in each plan.

---

## Comparison Matrix

| Criteria | Memory | Redis | LiteDb | SQLite |
|----------|--------|-------|--------|--------|
| Transport-specific ProducerQueue subclass | None | None | None | None |
| Producer type for type-system check | `ProducerQueue<T>` (core) | `ProducerQueue<T>` (core) | `ProducerQueue<T>` (core) | `ProducerQueue<T>` (core) |
| Init class for assembly reflection | `MemoryDashboardInit` | `RedisQueueInit` | `LiteDbMessageQueueInit` | `SqLiteMessageQueueInit` |
| Assembly name | `DotNetWorkQueue.Transport.Memory` | `DotNetWorkQueue.Transport.Redis` | `DotNetWorkQueue.Transport.LiteDb` | `DotNetWorkQueue.Transport.SQLite` |
| `IRelationalProducerQueue` in source | ABSENT | ABSENT | ABSENT | ABSENT |
| `Transport.RelationalDatabase` project ref in test | Missing — must add | Missing — must add | Present | Present |
| Extra SQLite assertion (Decision 4) | N/A | N/A | N/A | Required |
| Test project target frameworks | net10.0 only | net10.0 only | net10.0 only | net10.0 only |

---

## Recommendation

**All 4 negative-path tests are straightforward.** The type-system check is identical across all
4 transports because they all share `ProducerQueue<T>` from the core. The reflection-based
assembly assertion uses the transport-specific init class to anchor the assembly.

Template for each test (adapt per transport):

```csharp
// LGPL-2.1 license header required
using System.Linq;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.<X>.Basic;  // for typeof(XInit)
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.<X>.Tests.Basic
{
    [TestClass]
    public class <X>ProducerQueueDoesNotImplementRelationalTests
    {
        private sealed class TestMessage { }

        [TestMethod]
        public void ProducerQueue_DoesNotImplement_IRelationalProducerQueue()
        {
            // Type-system check
            Assert.IsFalse(
                typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(ProducerQueue<TestMessage>)),
                "ProducerQueue<T> must not implement IRelationalProducerQueue<T> — " +
                "<X> is a non-relational transport.");

            // Reflection-based assembly assertion
            var transportAssembly = typeof(<XInit>).Assembly;
            var allTypes = transportAssembly.GetTypes();
            bool anyImplementsRelational = allTypes.Any(t =>
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRelationalProducerQueue<>)));
            Assert.IsFalse(anyImplementsRelational,
                $"Transport assembly '{transportAssembly.GetName().Name}' must not " +
                "contain any type implementing IRelationalProducerQueue<T>.");
        }
    }
}
```

SQLite additionally appends:
```csharp
// Decision-4: confirm SQLite producer does NOT derive from RelationalProducerQueue<T>
Assert.IsFalse(
    typeof(RelationalProducerQueue<TestMessage>).IsAssignableFrom(
        typeof(ProducerQueue<TestMessage>)),
    "SQLite producer must not derive from RelationalProducerQueue<T> base — " +
    "outbox surface is explicitly deferred for SQLite.");
```

---

## Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Memory.Tests or Redis.Tests `.csproj` edit forgotten | Med | High — compile error | Each plan explicitly lists `.csproj` edit as Task 0 before the test file task |
| `MemoryDashboardInit` assembly is the wrong scope for Memory reflection check | Low | Low — the test still passes (the Dashboard DLL has no relational types) | Document the scope choice; Memory's "real" transport assembly is core, but `DotNetWorkQueue.Transport.Memory.dll` is the correct NuGet boundary to audit |
| `SqLiteMessageQueueInit` namespace typo — the directory is `SQLite` but init uses `SqLite` casing | Low | High — compile error | Confirmed: `namespace DotNetWorkQueue.Transport.SQLite.Basic` and class `SqLiteMessageQueueInit` (camelCase prefix, Pascal S — matches exactly as found in source) |
| `LiteDbMessageQueueInit` namespace discrepancy (directory `LiteDB`, namespace `LiteDb`) | Low | High — compile error | Confirmed: class is in namespace `DotNetWorkQueue.Transport.LiteDb.Basic` (lowercase 'b' in Db) per the init file's `using` directives |

---

## Sources

All findings are from direct source-file inspection of the repository at `/mnt/f/git/dotnetworkqueue`:

1. `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs` line 385 — fallback `ProducerQueue<>` registration
2. `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` — `public class ProducerQueue<T> : IProducerQueue<T>`
3. `Source/DotNetWorkQueue.Transport.Memory/Basic/MemoryDashboardInit.cs` — only public init in Memory assembly
4. `Source/DotNetWorkQueue/Transport/Memory/Basic/MemoryMessageQueueInit.cs` — core-hosted Memory init
5. `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs` — Redis init class
6. `Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbMessageQueueInit.cs` — LiteDb init class
7. `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueInit.cs` — SQLite init class
8. `Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj` — no RelationalDatabase ref
9. `Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj` — no RelationalDatabase ref
10. `Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj` — RelationalDatabase ref present
11. `Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj` — RelationalDatabase ref present
12. Grep output: zero `IRelationalProducerQueue` matches in all 4 non-relational transport source directories
13. `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalProducerQueueTests.cs` — reference pattern for the negative tests (Phase 3 template)

---

## Uncertainty Flags

- **None blocking.** All 6 research sections are fully answered from source inspection.
- The Memory assembly-scoping question (§1 note) is an architect-level design decision, not a research gap. Both `MemoryDashboardInit` and `MemoryMessageQueueInit` are concrete options; the recommended choice (`MemoryDashboardInit`) correctly scopes the assertion to the Memory-specific NuGet boundary.
