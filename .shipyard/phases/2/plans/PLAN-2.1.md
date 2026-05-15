---
phase: foundation-layer
plan: 2.1
wave: 2
dependencies: [1.1]
must_haves:
  - IExternalDbNameExtractor interface (no implementations in Phase 2)
  - ExternalTransactionValidator standalone class with 4 checks
  - 5 unit tests covering null-tx, null-conn, non-open-conn, db-name-mismatch, happy-path
files_touched:
  - Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/ExternalTransactionValidatorTests.cs
tdd: true
risk: low
---

# Plan 2.1: Validator + Extractor Interface (Wave 2)

## Context

Wave 2 (parallel with PLAN-2.2) adds the validator + extractor surface to `Transport.RelationalDatabase`. Three files, no overlap with PLAN-2.2's files. Both Wave 2 plans depend only on PLAN-1.1 (Wave 1) having landed.

This plan ships:
1. `IExternalDbNameExtractor` — a one-method interface in `Transport.RelationalDatabase`. Per-provider implementations (SqlServer = `OrdinalIgnoreCase`, PostgreSQL = `Ordinal`) are Phase 3/4 work; Phase 2 ships only the contract.
2. `ExternalTransactionValidator` — a standalone sealed class running the 4 validation checks from PROJECT.md §Validation. Uses `IConnectionInformation.Container` as the configured-DB accessor (RESEARCH.md §Section 6 confirmed both SqlServer's `SqlConnectionInformation` and PostgreSQL's `SqlConnectionInformation` populate `Container` with the database name — so the validator stays transport-agnostic and does not need to downcast `IConnectionInformation`).
3. 5 unit tests for the validator — one per failure mode plus the happy path. NSubstitute mocks `DbTransaction` and `DbConnection` (abstract bases per CLAUDE.md async-mocking lesson — even though this validator is sync, `DbTransaction.Connection` is defined on the abstract base, not on `IDbTransaction`, so mock the base).

Marker `IRetrySkippable` is not referenced here. The validator does not consume the marker; the marker is only read by retry decorators in Wave 3.

## Dependencies

- PLAN-1.1 (Wave 1) — `Transport.RelationalDatabase` must build clean, which is established by Task 3 of PLAN-1.1 (creating `IRetrySkippable.cs`). No type from PLAN-1.1 is directly imported here, but the project must build before this plan can.

## Tasks

### Task 1: Create IExternalDbNameExtractor interface

**Files:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs` (NEW)

**Action:** create

**Description:**
Create a new file with the standard LGPL header. One public interface, one method.

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
    /// Extracts the canonical database name from an open <see cref="DbConnection"/> for
    /// use by <see cref="Basic.ExternalTransactionValidator"/>. Per-provider implementations
    /// supply the appropriate name comparison semantics:
    /// <list type="bullet">
    ///   <item><description>SqlServer uses case-insensitive comparison (<c>StringComparer.OrdinalIgnoreCase</c>).</description></item>
    ///   <item><description>PostgreSQL uses case-sensitive comparison (<c>StringComparer.Ordinal</c>) to match the database's quoted-identifier semantics.</description></item>
    /// </list>
    /// Implementations live in <c>Transport.SqlServer</c> (Phase 3) and
    /// <c>Transport.PostgreSQL</c> (Phase 4); Phase 2 ships only this contract.
    /// </summary>
    public interface IExternalDbNameExtractor
    {
        /// <summary>
        /// Returns the canonical database name reported by the connection. Typically
        /// implemented via <c>connection.Database</c>.
        /// </summary>
        /// <param name="connection">An open <see cref="DbConnection"/> from the caller's
        /// transaction. Must not be null.</param>
        /// <returns>The database name as reported by the underlying ADO.NET provider.</returns>
        string Extract(DbConnection connection);
    }
}
```

The interface lives in the project root (alongside `IRetrySkippable` from PLAN-1.1).

**Acceptance Criteria:**
- File exists at the exact path with LGPL header.
- Exactly one public interface `IExternalDbNameExtractor` in namespace `DotNetWorkQueue.Transport.RelationalDatabase`.
- Exactly one method `string Extract(DbConnection connection)`.
- `dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release --nologo` succeeds (XML doc must cover the interface + method or the Release build fails).
- Layering grep returns empty: `grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs` → no output.

### Task 2: Create ExternalTransactionValidator class

**Files:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` (NEW)

**Action:** create

**Description:**
Create a new file in the `Basic/` subfolder (matches existing layout — `Transport.RelationalDatabase` keeps top-level interfaces at the root and concrete helpers under `Basic/`).

The class is a standalone sealed class (per CONTEXT-2 Decision 4) constructor-injected with `IExternalDbNameExtractor` and `IConnectionInformation`. The `Validate(DbTransaction)` method runs the 4 checks from PROJECT.md §Validation in the documented order. Critical: use `_connectionInfo.Container` as the configured DB name (RESEARCH.md §Section 6 confirmed both relational transports populate `Container` with the database name).

The 4th check (DB-name comparison) uses `StringComparison.Ordinal` for the comparison — the comparison semantics are intentionally encoded in the per-provider extractor by normalizing case at extract time, NOT in the validator. **Rationale:** keeps the validator transport-agnostic and free of `StringComparer` injection complexity. If a future requirement needs the comparer at the validator surface, it can be lifted out then; for now, the extractor returns the canonical form and the validator uses `Ordinal`.

**Architect note (gap from RESEARCH.md):** RESEARCH.md §Section 6 implied the validator might inject `StringComparison`. After review of the per-provider semantics (SqlServer = `OrdinalIgnoreCase` against `InitialCatalog`; PostgreSQL = `Ordinal` against `Database`), the cleanest decomposition is to let each Phase 3/4 extractor lowercase or pass through as appropriate. Phase 3 SqlServer extractor will `return connection.Database.ToUpperInvariant()` and the queue config side similarly upper-cases (or both sides normalize the same way); Phase 4 PostgreSQL passes through verbatim. **Flag for verifier:** ensure Phase 3/4 plans encode the case-normalization convention symmetrically across the extractor side AND the configured-DB side.

```csharp
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
// (full LGPL header)
// ---------------------------------------------------------------------
using System;
using System.Data;
using System.Data.Common;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Validates a caller-supplied <see cref="DbTransaction"/> before it is used by
    /// the relational producer to enqueue messages. Runs four checks in order:
    /// <list type="number">
    ///   <item><description>Transaction is non-null (throws <see cref="ArgumentNullException"/>).</description></item>
    ///   <item><description>Transaction's <c>Connection</c> is non-null (throws <see cref="InvalidOperationException"/> — "transaction disposed or completed").</description></item>
    ///   <item><description>Connection state is <see cref="ConnectionState.Open"/> (throws <see cref="InvalidOperationException"/>).</description></item>
    ///   <item><description>Database name reported by the connection (via the injected
    ///       <see cref="IExternalDbNameExtractor"/>) equals the queue's configured
    ///       database (via <see cref="IConnectionInformation.Container"/>). Comparison is
    ///       <see cref="StringComparison.Ordinal"/>; per-provider case semantics are
    ///       encoded by the extractor's normalization. Mismatch throws
    ///       <see cref="InvalidOperationException"/> with both database names in the
    ///       message.</description></item>
    /// </list>
    /// </summary>
    public sealed class ExternalTransactionValidator
    {
        private readonly IExternalDbNameExtractor _extractor;
        private readonly IConnectionInformation _connectionInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalTransactionValidator"/>
        /// class.
        /// </summary>
        /// <param name="extractor">Per-provider database-name extractor.</param>
        /// <param name="connectionInfo">Queue's configured connection information. The
        /// <see cref="IConnectionInformation.Container"/> property supplies the expected
        /// database name on both SqlServer and PostgreSQL transports.</param>
        public ExternalTransactionValidator(IExternalDbNameExtractor extractor,
            IConnectionInformation connectionInfo)
        {
            Guard.NotNull(() => extractor, extractor);
            Guard.NotNull(() => connectionInfo, connectionInfo);
            _extractor = extractor;
            _connectionInfo = connectionInfo;
        }

        /// <summary>
        /// Runs the four validation checks against the caller-supplied transaction.
        /// </summary>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <exception cref="ArgumentNullException">Transaction is null.</exception>
        /// <exception cref="InvalidOperationException">Connection is null, not open, or
        /// points to a different database than the queue's configured container.</exception>
        public void Validate(DbTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            var connection = transaction.Connection;
            if (connection == null)
                throw new InvalidOperationException(
                    "Caller-supplied transaction has a null Connection. The transaction " +
                    "has been disposed or its work has already been committed/rolled back.");

            if (connection.State != ConnectionState.Open)
                throw new InvalidOperationException(
                    $"Caller-supplied transaction's connection is not open " +
                    $"(state = {connection.State}). The connection must be open before " +
                    $"the producer can enlist its commands in the caller's transaction.");

            var actual = _extractor.Extract(connection);
            var expected = _connectionInfo.Container;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Caller-supplied transaction's connection points to database " +
                    $"'{actual}' but the queue is configured for database '{expected}'. " +
                    $"The outbox pattern requires the queue tables and the caller's " +
                    $"business data to live in the same database.");
        }
    }
}
```

Do NOT register this class in DI yet — DI wiring is Phase 3/4 (the validator needs a per-transport `IExternalDbNameExtractor` implementation, which is also Phase 3/4 work).

**Acceptance Criteria:**
- File exists at the path above with the full LGPL header.
- Class is `public sealed`, namespace `DotNetWorkQueue.Transport.RelationalDatabase.Basic`.
- Constructor takes `IExternalDbNameExtractor` + `IConnectionInformation` and guards both.
- `Validate(DbTransaction)` method runs the 4 checks in the documented order with the documented exception types.
- The 4th check's error message contains both the actual and expected database names (per PROJECT.md §Non-Functional Diagnostics).
- `dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release --nologo` succeeds (XML doc required on class + constructor + method or Release build fails).
- Layering grep returns empty: `grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` → no output.

### Task 3: Add 5 unit tests for ExternalTransactionValidator

**Files:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/ExternalTransactionValidatorTests.cs` (NEW)

**Action:** test

**Description:**
Create a new test file at the path above. Test project location confirmed via RESEARCH.md §Section 8: `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/` with the `Basic/` subfolder convention for tests against `Basic/` classes. Existing tests in `Basic/Command/` use MSTest 4.x + NSubstitute (`[TestClass]`, `[TestMethod]`, `Substitute.For<T>()`).

Five tests, matching CONTEXT-2 Exit Criterion 4:

1. `Validate_WhenTransactionIsNull_ThrowsArgumentNullException`
2. `Validate_WhenConnectionIsNull_ThrowsInvalidOperationException`
3. `Validate_WhenConnectionNotOpen_ThrowsInvalidOperationException`
4. `Validate_WhenDatabaseNameMismatch_ThrowsInvalidOperationExceptionWithBothNames`
5. `Validate_WhenAllChecksPass_DoesNotThrow`

CLAUDE.md MSTest 4.x assertions: use `Assert.ThrowsExactly<T>(() => ...)` (NOT `Assert.ThrowsException<>`). Use `Assert.AreEqual` for equality. For `DbTransaction` / `DbConnection`, mock the **abstract base classes** via `Substitute.For<DbTransaction>()` and `Substitute.For<DbConnection>()` per CLAUDE.md async-mocking lesson (even though this validator is sync, `DbTransaction.Connection` is on the abstract base, not on `IDbTransaction`).

Skeleton for the happy-path test (mirror this shape for the others):

```csharp
// (LGPL header)
using System;
using System.Data;
using System.Data.Common;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class ExternalTransactionValidatorTests
    {
        private const string QueueDb = "MyQueueDb";

        private static (ExternalTransactionValidator sut, DbTransaction tx, DbConnection conn)
            BuildSut(string actualDbFromExtractor = QueueDb,
                     ConnectionState connState = ConnectionState.Open,
                     bool nullConnectionOnTx = false)
        {
            var extractor = Substitute.For<IExternalDbNameExtractor>();
            extractor.Extract(Arg.Any<DbConnection>()).Returns(actualDbFromExtractor);
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.Container.Returns(QueueDb);

            var conn = Substitute.For<DbConnection>();
            conn.State.Returns(connState);

            var tx = Substitute.For<DbTransaction>();
            // DbTransaction.Connection getter — NSubstitute on abstract base
            tx.Connection.Returns(nullConnectionOnTx ? null : conn);

            var sut = new ExternalTransactionValidator(extractor, connInfo);
            return (sut, tx, conn);
        }

        [TestMethod]
        public void Validate_WhenTransactionIsNull_ThrowsArgumentNullException()
        {
            var (sut, _, _) = BuildSut();
            Assert.ThrowsExactly<ArgumentNullException>(() => sut.Validate(null));
        }

        [TestMethod]
        public void Validate_WhenConnectionIsNull_ThrowsInvalidOperationException()
        {
            var (sut, tx, _) = BuildSut(nullConnectionOnTx: true);
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(tx));
            StringAssert.Contains(ex.Message, "null Connection");
        }

        [TestMethod]
        public void Validate_WhenConnectionNotOpen_ThrowsInvalidOperationException()
        {
            var (sut, tx, _) = BuildSut(connState: ConnectionState.Closed);
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(tx));
            StringAssert.Contains(ex.Message, "Closed");
        }

        [TestMethod]
        public void Validate_WhenDatabaseNameMismatch_ThrowsInvalidOperationExceptionWithBothNames()
        {
            var (sut, tx, _) = BuildSut(actualDbFromExtractor: "WrongDb");
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => sut.Validate(tx));
            // Diagnostics requirement (PROJECT.md §Non-Functional Diagnostics):
            StringAssert.Contains(ex.Message, "WrongDb");
            StringAssert.Contains(ex.Message, QueueDb);
        }

        [TestMethod]
        public void Validate_WhenAllChecksPass_DoesNotThrow()
        {
            var (sut, tx, _) = BuildSut();
            sut.Validate(tx); // must not throw
        }
    }
}
```

Note: `Microsoft.VisualStudio.TestTools.UnitTesting.StringAssert.Contains` is the MSTest 4.x form (no signature change vs MSTest 3.x for this method).

**Acceptance Criteria:**
- File exists at the path above with LGPL header.
- Test class `ExternalTransactionValidatorTests` in namespace `DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic`.
- Exactly 5 `[TestMethod]` methods with the names above.
- All 5 tests use `Assert.ThrowsExactly<T>` for exception assertions (no `Assert.ThrowsException<>`).
- All 5 tests use NSubstitute on the abstract bases `DbTransaction` + `DbConnection` (NOT on `IDbTransaction` / `IDbConnection`).
- The DB-name-mismatch test asserts BOTH names are in the error message.
- `dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug --filter "FullyQualifiedName~ExternalTransactionValidatorTests" --nologo` reports 5 passed, 0 failed.

## Verification

```bash
# Files exist and compile
test -f Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs
test -f Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs
test -f Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/ExternalTransactionValidatorTests.cs

# Release build (TreatWarningsAsErrors + XML doc gen)
dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release --nologo
# expected: 0 Error(s), 0 Warning(s)

# All 5 validator unit tests pass
dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug --filter "FullyQualifiedName~ExternalTransactionValidatorTests" --nologo
# expected: Passed!  - Failed: 0, Passed: 5, Skipped: 0, Total: 5

# Existing test suite (no regressions)
dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug --nologo
# expected: Failed: 0 (all pre-existing tests still pass)

# Layering invariant (CONTEXT-2 Hard Rules)
grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase/ --include="*.cs" --include="*.csproj"
# expected: no matches (exit code 1)
```
