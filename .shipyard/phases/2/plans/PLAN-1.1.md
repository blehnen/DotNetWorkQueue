---
phase: phase-2-foundation
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - public interface IRelationalWorkerNotification : IWorkerNotification in Transport.RelationalDatabase
  - single read-only member DbTransaction Transaction { get; } typed as System.Data.Common.DbTransaction (NOT IDbTransaction)
  - full XML documentation on interface and member (summary + value + remarks)
  - 18-line LGPL-2.1 license header matching the existing Transport.RelationalDatabase files
  - contract unit test in Transport.RelationalDatabase.Tests asserting inheritance, property type, and read-only shape
  - Release build clean on net10.0 AND net8.0 with TreatWarningsAsErrors + GenerateDocumentationFile (no CS1591)
  - no new ADO.NET provider reference (Microsoft.Data.SqlClient / Npgsql / Microsoft.Data.Sqlite) introduced into Transport.RelationalDatabase
  - no `Tx` abbreviation in identifiers, prose, XML doc, or commit messages
files_touched:
  - Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalWorkerNotification.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/IRelationalWorkerNotificationContractTests.cs
tdd: false
risk: low
---

# Plan 1.1: Author IRelationalWorkerNotification interface + contract test

## Context

Phase 2 ships the inbox-pattern foundation: a single new public interface `IRelationalWorkerNotification : IWorkerNotification` in `DotNetWorkQueue.Transport.RelationalDatabase` carrying one read-only `DbTransaction` member. Per CONTEXT-2 §Decisions, the SQLite extractor and the `NormalizedConnectionInformation`-style wrapper defer to Phase 5 — Phase 2 is interface-only, additive, zero behavior change. Capability-cast pattern: when a notification instance is `IRelationalWorkerNotification`, `Transaction` is non-null. The presence of the interface IS the capability assertion (mirrors the outbox-milestone `IRelationalProducerQueue<T>` shape).

## Dependencies

None. This is the first plan in wave 1.

## Tasks

### Task 1: Author the IRelationalWorkerNotification interface
**Files:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalWorkerNotification.cs` (create)

**Action:** create

**Description:**

Create a new file at `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalWorkerNotification.cs` with the following content (exact shape — header copied from `Source/DotNetWorkQueue.Transport.RelationalDatabase/IConnectionHolder.cs` lines 1-18 verbatim, no edits to year or copyright):

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
using System.Data.Common;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Extends <see cref="IWorkerNotification"/> on the three relational transports (SqlServer, PostgreSQL, SQLite)
    /// to expose the active dequeue <see cref="DbTransaction"/> to the message handler, enabling the
    /// transactional inbox pattern: business writes can join the same transaction the library uses to dequeue
    /// and commit the queue message, so the two commit (or roll back) atomically.
    /// </summary>
    /// <remarks>
    /// Capability-cast pattern. Non-relational transports (Memory, Redis, LiteDb) never implement this interface.
    /// User handlers discover the capability via a single cast:
    /// <code>
    /// if (notification is IRelationalWorkerNotification relational)
    /// {
    ///     // write business data on relational.Transaction.Connection within relational.Transaction
    /// }
    /// </code>
    /// The interface is only implemented when <c>EnableHoldTransactionUntilMessageCommitted = true</c> on the
    /// transport options. With the option off, the cast cleanly fails and the inbox capability is not exposed.
    /// <para>
    /// Ownership contract — the library owns the transaction. User handlers MUST NOT call
    /// <c>Commit()</c>, <c>Rollback()</c>, <c>Dispose()</c>, or <c>Close()</c> on the exposed
    /// <see cref="DbTransaction"/> or its <see cref="DbTransaction.Connection"/>. User handlers MUST NOT stash
    /// the reference past the handler's return and MUST NOT pass it to another thread
    /// (<see cref="DbTransaction"/> is not thread-safe). The library commits on successful handler return and
    /// rolls back on handler throw — user signals rollback by throwing.
    /// </para>
    /// </remarks>
    public interface IRelationalWorkerNotification : IWorkerNotification
    {
        /// <summary>
        /// Gets the active <see cref="DbTransaction"/> for the in-flight dequeue. The user's handler may
        /// enlist business writes against this transaction's <see cref="DbTransaction.Connection"/>.
        /// </summary>
        /// <value>
        /// A non-null <see cref="DbTransaction"/> owned by the library. The transaction is committed by the
        /// library after the handler returns successfully and rolled back if the handler throws. The user
        /// must not mutate its lifecycle.
        /// </value>
        /// <remarks>
        /// Typed as the abstract <see cref="DbTransaction"/> (from <c>System.Data.Common</c>), not the
        /// <see cref="System.Data.IDbTransaction"/> interface, so callers may await async dispose / commit
        /// shapes the abstract base exposes. Never null when the containing interface is implemented.
        /// </remarks>
        DbTransaction Transaction { get; }
    }
}
```

Notes for the executor:
- Copy lines 1-18 of `Source/DotNetWorkQueue.Transport.RelationalDatabase/IConnectionHolder.cs` verbatim as the header — do not retype.
- The single `using System.Data.Common;` directive is the ONLY import needed. `IWorkerNotification` resolves via the current namespace `DotNetWorkQueue.Transport.RelationalDatabase` walking up to root `DotNetWorkQueue` (the canonical C# namespace walk-up rule). Do NOT add `using DotNetWorkQueue;` — it is redundant and triggers IDE0005 in some toolchains.
- Do NOT add `using System.Data;` — the file does not reference `IDbTransaction` (intentional — `DbTransaction` is the chosen type per RESEARCH §7).
- Make sure file ends with a single trailing newline (matches repo convention).
- No `Tx` token anywhere — `Transaction` is fully spelled in identifiers, XML doc, and any commit message.

**Acceptance Criteria:**
- File `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalWorkerNotification.cs` exists.
- Header is the 18-line LGPL-2.1 block byte-identical to `IConnectionHolder.cs` lines 1-18.
- Declares `public interface IRelationalWorkerNotification : IWorkerNotification` in namespace `DotNetWorkQueue.Transport.RelationalDatabase`.
- Single member: `DbTransaction Transaction { get; }` — read-only (no setter).
- Member type is `System.Data.Common.DbTransaction` (NOT `System.Data.IDbTransaction`).
- `<summary>` present on both the interface and the property. `<remarks>` present on the interface (capability-cast + ownership contract) and on the property (type-choice rationale). `<value>` present on the property.
- File has exactly one `using` directive: `using System.Data.Common;`.
- `dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release -p:CI=true --nologo` succeeds on net10.0 AND net8.0 (TreatWarningsAsErrors enabled — no CS1591 missing-XML-doc errors).

---

### Task 2: Author the contract unit test
**Files:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/IRelationalWorkerNotificationContractTests.cs` (create)

**Action:** create

**Description:**

Create a contract unit test at `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/IRelationalWorkerNotificationContractTests.cs` that locks the public shape of the new interface against accidental drift. Use MSTest 3.x assertions (`Assert.IsTrue`, `Assert.AreEqual`, `Assert.IsNotNull`, `Assert.IsNull`) — no FluentAssertions needed for reflection assertions.

Exact content:

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
using System.Data.Common;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests
{
    /// <summary>
    /// Locks the public contract of <see cref="IRelationalWorkerNotification"/>: inheritance from
    /// <see cref="IWorkerNotification"/>, single read-only <see cref="DbTransaction"/> property,
    /// and public visibility. Failure of any assertion here indicates an accidental breaking change
    /// to the inbox-pattern capability surface.
    /// </summary>
    [TestClass]
    public class IRelationalWorkerNotificationContractTests
    {
        [TestMethod]
        public void Interface_Is_Public()
        {
            var type = typeof(IRelationalWorkerNotification);
            Assert.IsTrue(type.IsInterface, "IRelationalWorkerNotification must be an interface.");
            Assert.IsTrue(type.IsPublic, "IRelationalWorkerNotification must be public.");
        }

        [TestMethod]
        public void Interface_Inherits_IWorkerNotification()
        {
            var type = typeof(IRelationalWorkerNotification);
            Assert.IsTrue(
                typeof(IWorkerNotification).IsAssignableFrom(type),
                "IRelationalWorkerNotification must inherit IWorkerNotification.");
        }

        [TestMethod]
        public void Transaction_Property_Exists_With_Expected_Type()
        {
            var prop = typeof(IRelationalWorkerNotification)
                .GetProperty("Transaction", BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(prop, "Transaction property must exist on IRelationalWorkerNotification.");
            Assert.AreEqual(
                typeof(DbTransaction),
                prop!.PropertyType,
                "Transaction property must be typed as System.Data.Common.DbTransaction (NOT System.Data.IDbTransaction).");
        }

        [TestMethod]
        public void Transaction_Property_Is_Read_Only()
        {
            var prop = typeof(IRelationalWorkerNotification)
                .GetProperty("Transaction", BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(prop);
            Assert.IsTrue(prop!.CanRead, "Transaction must expose a getter.");
            Assert.IsNull(
                prop.GetSetMethod(nonPublic: false),
                "Transaction must NOT expose a public setter — the library owns the transaction.");
        }

        [TestMethod]
        public void Interface_Declares_Exactly_One_New_Property()
        {
            // DeclaredOnly excludes IWorkerNotification members; only the additive surface should appear.
            var declared = typeof(IRelationalWorkerNotification).GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.AreEqual(1, declared.Length,
                "IRelationalWorkerNotification must declare exactly one new property (Transaction).");
            Assert.AreEqual("Transaction", declared[0].Name);
        }
    }
}
```

Notes for the executor:
- File goes at the root of the test project (matches the namespace pattern `DotNetWorkQueue.Transport.RelationalDatabase.Tests`, where root-level files like `TestHelpers/` siblings already exist). No subdirectory needed.
- The 18-line license header is byte-identical to Task 1 — same source.
- Test project targets net10.0 only (per RESEARCH §4) — no multi-target concerns.
- No NSubstitute / AutoFixture / FluentAssertions needed — pure reflection + MSTest.
- The fifth test (`Interface_Declares_Exactly_One_New_Property`) is a tripwire against future drift: if a sixth interface member is added without ceremony, this test fails loudly.

**Acceptance Criteria:**
- File `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/IRelationalWorkerNotificationContractTests.cs` exists with the 18-line header.
- Five `[TestMethod]` methods are present:
  - `Interface_Is_Public`
  - `Interface_Inherits_IWorkerNotification`
  - `Transaction_Property_Exists_With_Expected_Type`
  - `Transaction_Property_Is_Read_Only`
  - `Interface_Declares_Exactly_One_New_Property`
- `dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~IRelationalWorkerNotificationContractTests"` passes — 5 / 5 tests.
- Existing test suite (`dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj"`) remains green — no regressions in any other test class.

---

### Task 3: Run Phase 2 verification gates
**Files:** none modified — verification only.

**Action:** verify

**Description:**

Run the four verification commands listed under `## Verification` below. All four must produce the expected output. Capture the results in the Phase 2 summary artifact (`.shipyard/phases/2/results/SUMMARY-1.1.md` — created by the builder per repo workflow). If any gate fails, do NOT amend the plan — surface the failure to the user.

The grep guards (gates 3 and 4) are positive-shape checks: they MUST return zero matches. The intent is to fail loudly if a future edit introduces a forbidden token.

**Acceptance Criteria:**
- Release build gate (net10.0 + net8.0) passes.
- Unit test gate (RelationalDatabase.Tests) passes with the new 5 tests included.
- Grep gate: zero matches for `Microsoft\.Data\.SqlClient|Npgsql|Microsoft\.Data\.Sqlite` in `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj`.
- Grep gate: zero matches for `\bTx\b|\bTX\b` (word-boundary, case-sensitive) in either of the two new files, EXCLUDING the legitimate word `Transaction` / `transaction`.

## Verification

Run from repo root:

```bash
# Gate 1 — Release build clean on both TFMs, TreatWarningsAsErrors active.
dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release -p:CI=true --nologo

# Gate 2 — Existing + new tests pass.
dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --nologo

# Gate 3 — No ADO.NET provider reference leaked into the shared csproj. Expected: zero matches.
grep -nE "Microsoft\.Data\.SqlClient|Npgsql|Microsoft\.Data\.Sqlite" \
  "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" \
  ; test $? -eq 1   # grep exit 1 = no matches; bash test confirms

# Gate 4 — No `Tx` abbreviation in the two new files (case-sensitive, word-boundary). Expected: zero matches.
grep -nE "\b(Tx|TX)\b" \
  "Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalWorkerNotification.cs" \
  "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/IRelationalWorkerNotificationContractTests.cs" \
  ; test $? -eq 1
```

Expected end states:
- Gate 1: `Build succeeded. 0 Warning(s) 0 Error(s)`.
- Gate 2: `Passed:` line includes the 5 new tests; total prior test count + 5 = new total; `Failed: 0`.
- Gate 3: grep prints nothing; `test $? -eq 1` returns success (no matches).
- Gate 4: grep prints nothing; `test $? -eq 1` returns success (no matches).
