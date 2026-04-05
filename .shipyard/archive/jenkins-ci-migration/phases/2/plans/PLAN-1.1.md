# Plan 1.1: Queue Name Validation -- Relational Transports (SqlServer, PostgreSQL, SQLite)

---
phase: queue-name-validation
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - Per-transport queue name validation in SqlServer, PostgreSQL, and SQLite connection info constructors
  - Reject names with SQL injection characters, spaces, and other non-permitted characters
  - Allow empty queue names for backward compatibility
  - Enforce per-transport max lengths (SqlServer 128, PostgreSQL 63, SQLite none)
  - Fix QueueCreatorTests in all 3 transport test projects that use fixture.Create<string>()
  - New validation unit tests per transport
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs
  - Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/SqlConnectionInformationTests.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/SqlConnectionInformationTests.cs
  - Source/DotNetWorkQueue.Transport.SQLite.Tests/SQLiteConnectionInformationTests.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs
  - Source/DotNetWorkQueue.Transport.SQLite.Tests/QueueCreatorTests.cs
tdd: true
---

## Context

Queue names are concatenated directly into SQL table names via `TableNameHelper` and `SqlServerTableNameHelper` without any sanitization. A queue name like `]; DROP TABLE Users--` would result in SQL injection at the DDL/DML level. This plan adds fail-fast validation to the three relational transport connection info classes (SqlServer, PostgreSQL, SQLite), plus fixes existing tests that generate non-compliant queue names via AutoFixture.

All three relational transports share the same allowed character regex (`^[a-zA-Z0-9_.]+$`) but differ in max length enforcement. The design decision specifies per-transport validation with NO changes to `BaseConnectionInformation`.

## Dependencies

None. This is a Wave 1 plan. Can execute in parallel with Plan 1.2.

## Tasks

### Task 1: Add queue name validation to SQL Server and PostgreSQL connection info classes

**Files:**
- `Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs` (modify)
- `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs` (modify)

**Action:** modify

**Description:**

In each file, add a private static method `ValidateQueueName(string name)` and call it from the constructor immediately after the `base(queueConnection)` call, before the existing `ValidateConnection` call.

**SQL Server** (`Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs`):
- Add `using System.Text.RegularExpressions;` to the imports
- In the constructor (line 38, after `: base(queueConnection)`), add `ValidateQueueName(queueConnection.Queue);` as the first statement
- Add the private static method:
  ```
  private static void ValidateQueueName(string name)
  {
      if (string.IsNullOrEmpty(name)) return; // allow empty for backward compatibility
      if (name.Length > 128)
          throw new ArgumentException($"Queue name exceeds maximum length of 128 characters. Got {name.Length} characters.", nameof(name));
      if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_.]+$"))
          throw new ArgumentException("Queue name contains invalid characters. Only alphanumeric characters, underscores, and dots are allowed.", nameof(name));
  }
  ```
- Add XML doc comment to the `ValidateQueueName` method: `/// <summary>Validates that the queue name contains only safe characters for use as a SQL Server table name identifier.</summary>`

**PostgreSQL** (`Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs`):
- Identical pattern to SQL Server except max length is 63 (PostgreSQL identifier limit)
- Add `using System.Text.RegularExpressions;` to the imports
- In the constructor (line 36, after `: base(queueConnection)`), add `ValidateQueueName(queueConnection.Queue);` as the first statement
- Add the private static method with max length 63 instead of 128
- Same XML doc comment adjusted for "PostgreSQL identifier"

**Conventions to follow:**
- LGPL-2.1 license header is already present in both files (do not modify)
- Follow the existing `ValidateConnection` private method pattern already in these files
- Use `System` namespace `ArgumentException` (already imported)

**Acceptance Criteria:**
- `new SqlConnectionInformation(new QueueConnection("valid_name.test", goodConnStr))` succeeds
- `new SqlConnectionInformation(new QueueConnection("; DROP TABLE--", goodConnStr))` throws `ArgumentException`
- `new SqlConnectionInformation(new QueueConnection("", goodConnStr))` succeeds (backward compat)
- `new SqlConnectionInformation(new QueueConnection(new string('a', 129), goodConnStr))` throws `ArgumentException` (SQL Server)
- `new SqlConnectionInformation(new QueueConnection(new string('a', 64), goodConnStr))` throws `ArgumentException` (PostgreSQL)
- `new SqlConnectionInformation(new QueueConnection(new string('a', 128), goodConnStr))` succeeds (SQL Server boundary)
- `new SqlConnectionInformation(new QueueConnection(new string('a', 63), goodConnStr))` succeeds (PostgreSQL boundary)

---

### Task 2: Add queue name validation to SQLite connection info and fix all 3 QueueCreatorTests files

**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs` (modify)
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs` (modify)
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs` (modify)
- `Source/DotNetWorkQueue.Transport.SQLite.Tests/QueueCreatorTests.cs` (modify)

**Action:** modify

**Description:**

**SQLite connection info** (`Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs`):
- Add `using System.Text.RegularExpressions;` to the imports
- In the constructor (line 32, after `: base(queueConnection)`), add `ValidateQueueName(queueConnection.Queue);` as the first statement (before the existing connection validation logic)
- Add the private static method -- same as SQL Server/PostgreSQL but with **no max length check** (SQLite has no table name length limit per design decision):
  ```
  private static void ValidateQueueName(string name)
  {
      if (string.IsNullOrEmpty(name)) return;
      if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_.]+$"))
          throw new ArgumentException("Queue name contains invalid characters. Only alphanumeric characters, underscores, and dots are allowed.", nameof(name));
  }
  ```
- Add XML doc comment: `/// <summary>Validates that the queue name contains only safe characters for use as a SQLite table name identifier.</summary>`

**Fix QueueCreatorTests** -- In all 3 transport test files, replace every `fixture.Create<string>()` that is assigned to a `queue` variable with the literal string `"TestQueue"`. These files are:
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs` -- 7 occurrences of `var queue = fixture.Create<string>();` (lines 21, 37, 53, 69, 85, 105, 122)
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs` -- 7 occurrences (lines 22, 38, 54, 70, 86, 106, 122)
- `Source/DotNetWorkQueue.Transport.SQLite.Tests/QueueCreatorTests.cs` -- 7 occurrences (lines 26, 42, 53, 64, 75, 90, 101)

The replacement is: change `var queue = fixture.Create<string>();` to `var queue = "TestQueue";`

These tests create `QueueConnection(queue, connectionString)` which flows through the transport's connection info constructor. AutoFixture generates GUID strings like `"4d6f5a29-2b1e-4c8a-..."` containing hyphens, which will now fail validation.

**Acceptance Criteria:**
- SQLite validation: `new SqliteConnectionInformation(new QueueConnection("; DROP TABLE", connStr), dataSource)` throws `ArgumentException`
- SQLite allows empty: `new SqliteConnectionInformation(new QueueConnection("", connStr), dataSource)` succeeds
- SQLite has no length limit: `new SqliteConnectionInformation(new QueueConnection(new string('a', 500), connStr), dataSource)` succeeds
- All existing tests in QueueCreatorTests pass after the `fixture.Create<string>()` replacement:
  - `dotnet test "Source\DotNetWorkQueue.Transport.SqlServer.Tests\DotNetWorkQueue.Transport.SqlServer.Tests.csproj" --filter "FullyQualifiedName~QueueCreatorTests"`
  - `dotnet test "Source\DotNetWorkQueue.Transport.PostgreSQL.Tests\DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" --filter "FullyQualifiedName~QueueCreatorTests"`
  - `dotnet test "Source\DotNetWorkQueue.Transport.SQLite.Tests\DotNetWorkQueue.Transport.SQLite.Tests.csproj" --filter "FullyQualifiedName~QueueCreatorTests"`

---

### Task 3: Add validation unit tests for all 3 relational transports

**Files:**
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/SqlConnectionInformationTests.cs` (modify)
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/SqlConnectionInformationTests.cs` (modify)
- `Source/DotNetWorkQueue.Transport.SQLite.Tests/SQLiteConnectionInformationTests.cs` (modify)

**Action:** test

**Description:**

Add new test methods to each existing test class. Follow the existing MSTest patterns in each file (use `[TestMethod]`, `Assert.ThrowsExactly<ArgumentException>`, direct construction).

**SQL Server tests** (`SqlConnectionInformationTests.cs`) -- add these test methods to the existing `SqlConnectionInformationTests` class. Use the existing `GoodConnection` constant already defined in the file for the connection string:

1. `QueueName_Valid_Alphanumeric` -- construct with `"MyQueue123"`, assert no exception, verify `QueueName == "MyQueue123"`
2. `QueueName_Valid_WithUnderscoreAndDot` -- construct with `"My_Queue.v2"`, assert no exception
3. `QueueName_Invalid_SqlInjection` -- construct with `"queue; DROP TABLE Users--"`, assert `ArgumentException`
4. `QueueName_Invalid_SpecialChars` -- construct with `"queue@#$%"`, assert `ArgumentException`
5. `QueueName_Invalid_Spaces` -- construct with `"my queue"`, assert `ArgumentException`
6. `QueueName_Invalid_Hyphen` -- construct with `"my-queue"`, assert `ArgumentException`
7. `QueueName_Empty_Allowed` -- construct with `""`, assert no exception (backward compat)
8. `QueueName_ExceedsMaxLength_128` -- construct with `new string('a', 129)`, assert `ArgumentException`
9. `QueueName_AtMaxLength_128` -- construct with `new string('a', 128)`, assert no exception

Construction pattern: `new SqlConnectionInformation(new QueueConnection(queueName, GoodConnection))`

The file needs `using DotNetWorkQueue.Configuration;` if not already present.

**PostgreSQL tests** (`SqlConnectionInformationTests.cs` in PostgreSQL.Tests) -- identical test methods as SQL Server except:
- Max length tests use 63/64 instead of 128/129
- Test names reflect PostgreSQL: `QueueName_ExceedsMaxLength_63`, `QueueName_AtMaxLength_63`
- Use the existing `GoodConnection` constant in the file

Construction pattern: `new SqlConnectionInformation(new QueueConnection(queueName, GoodConnection))`

**SQLite tests** (`SQLiteConnectionInformationTests.cs`) -- same character validation tests but:
- No max length test (SQLite has no limit)
- Construction requires `IDbDataSource` parameter: `new SqliteConnectionInformation(new QueueConnection(queueName, connStr), null)` (existing tests pass `null` for `IDbDataSource`)
- Use the existing connection string pattern from the file (check what existing tests use)

Test naming convention: Follow existing pattern of PascalCase_With_Underscores (e.g., `GetSet_Connection_Bad_Exception` is the existing pattern).

**Acceptance Criteria:**
- All new validation tests pass:
  - `dotnet test "Source\DotNetWorkQueue.Transport.SqlServer.Tests\DotNetWorkQueue.Transport.SqlServer.Tests.csproj" --filter "FullyQualifiedName~QueueName_"`
  - `dotnet test "Source\DotNetWorkQueue.Transport.PostgreSQL.Tests\DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" --filter "FullyQualifiedName~QueueName_"`
  - `dotnet test "Source\DotNetWorkQueue.Transport.SQLite.Tests\DotNetWorkQueue.Transport.SQLite.Tests.csproj" --filter "FullyQualifiedName~QueueName_"`
- All pre-existing tests in these files continue to pass:
  - `dotnet test "Source\DotNetWorkQueue.Transport.SqlServer.Tests\DotNetWorkQueue.Transport.SqlServer.Tests.csproj"`
  - `dotnet test "Source\DotNetWorkQueue.Transport.PostgreSQL.Tests\DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj"`
  - `dotnet test "Source\DotNetWorkQueue.Transport.SQLite.Tests\DotNetWorkQueue.Transport.SQLite.Tests.csproj"`

## Verification

Run all three transport test suites to confirm both existing and new tests pass:

```bash
dotnet test "Source\DotNetWorkQueue.Transport.SqlServer.Tests\DotNetWorkQueue.Transport.SqlServer.Tests.csproj" && \
dotnet test "Source\DotNetWorkQueue.Transport.PostgreSQL.Tests\DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" && \
dotnet test "Source\DotNetWorkQueue.Transport.SQLite.Tests\DotNetWorkQueue.Transport.SQLite.Tests.csproj"
```
