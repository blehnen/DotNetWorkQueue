# Research: Phase 2 -- Queue Name Validation

## Context

DotNetWorkQueue is a producer/distributed consumer library for .NET supporting multiple transports (SQL Server, PostgreSQL, SQLite, Redis, LiteDB, Memory). Queue names provided by users are directly concatenated into SQL table names, Redis key names, and LiteDB collection names without any validation. This creates a SQL injection risk surface for relational transports and potential key-collision or corruption risks for non-relational transports.

This research documents the exact structure of each transport's connection info class, how queue names flow into data store identifiers, existing test coverage, and patterns to follow for adding per-transport validation.

---

## 1. Connection Info Classes -- Structure and Constructor Patterns

### 1.1 Base Class: `BaseConnectionInformation`

- **File**: `Source/DotNetWorkQueue/Configuration/BaseConnectionInformation.cs`
- **Namespace**: `DotNetWorkQueue.Configuration`
- **Constructor**: `public BaseConnectionInformation(QueueConnection queueConnection)`
- **Queue name storage**: Stored in private `_queueName` field (line 28), exposed via `virtual string QueueName` property (line 58)
- **Existing validation**: **None**. The constructor stores `queueConnection.Queue` directly with no checks (line 37).
- **Key detail**: `QueueConnection` itself (line 44 of `QueueConnection.cs`) also performs **zero validation** -- it stores the queue string as-is.

### 1.2 SQL Server: `SqlConnectionInformation`

- **File**: `Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs`
- **Class**: `SqlConnectionInformation` (public)
- **Namespace**: `DotNetWorkQueue.Transport.SqlServer`
- **Constructor**: `public SqlConnectionInformation(QueueConnection queueConnection) : base(queueConnection)` (line 38)
- **Existing validation**: Validates the **connection string** via `SqlConnectionStringBuilder` (line 92), but performs **no queue name validation**.
- **Queue name access**: Inherited from base -- `QueueName` property.
- **Server/Container**: Extracted from connection string (`DataSource`, `InitialCatalog`).

### 1.3 PostgreSQL: `SqlConnectionInformation`

- **File**: `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs`
- **Class**: `SqlConnectionInformation` (public) -- same class name as SQL Server, different namespace
- **Namespace**: `DotNetWorkQueue.Transport.PostgreSQL`
- **Constructor**: `public SqlConnectionInformation(QueueConnection queueConnection) : base(queueConnection)` (line 36)
- **Existing validation**: Validates the **connection string** via `NpgsqlConnectionStringBuilder` (line 71), but performs **no queue name validation**.
- **Queue name access**: Inherited from base.

### 1.4 SQLite: `SqliteConnectionInformation`

- **File**: `Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs`
- **Class**: `SqliteConnectionInformation` (public)
- **Namespace**: `DotNetWorkQueue.Transport.SQLite`
- **Constructor**: `public SqliteConnectionInformation(QueueConnection queueConnection, IDbDataSource dataSource) : base(queueConnection)` (line 32)
- **Extra param**: `IDbDataSource dataSource` -- used to extract server info from connection string.
- **Existing validation**: Validates connection via `_dataSource.DataSource(value)` (line 71), but performs **no queue name validation**.
- **Queue name access**: Inherited from base.

### 1.5 LiteDB: `LiteDbConnectionInformation`

- **File**: `Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs`
- **Class**: `LiteDbConnectionInformation` (public)
- **Namespace**: `DotNetWorkQueue.Transport.LiteDb`
- **Constructor**: `public LiteDbConnectionInformation(QueueConnection queueConnection) : base(queueConnection)` (line 31)
- **Existing validation**: **None at all**. Constructor only sets `_server = "TODO; not known"` (line 33).
- **Queue name access**: Inherited from base.

### 1.6 Redis: `RedisConnectionInfo`

- **File**: `Source/DotNetWorkQueue.Transport.Redis/RedisConnectionInfo.cs`
- **Class**: `RedisConnectionInfo` (**internal**, not public)
- **Namespace**: `DotNetWorkQueue.Transport.Redis`
- **Constructor**: `public RedisConnectionInfo(QueueConnection queueConnection) : base(queueConnection)` (line 35)
- **Existing validation**: Validates connection string via `ConfigurationOptions.Parse(value)` (line 78), but only if connection is not null/empty (line 37). Performs **no queue name validation**.
- **Queue name access**: Inherited from base.
- **Important**: Class is `internal`, so tests use `[assembly: InternalsVisibleTo]` or create it directly.

### 1.7 Memory: `ConnectionInformation`

- **File**: `Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs`
- **Class**: `ConnectionInformation` (public)
- **Namespace**: `DotNetWorkQueue.Transport.Memory`
- **Constructor**: `public ConnectionInformation(QueueConnection queueConnection) : base(queueConnection)` (line 34)
- **Existing validation**: **None**. Constructor body is empty (lines 35-37).
- **Queue name access**: Inherited from base. `Container` returns `QueueName` directly (line 59).

---

## 2. How Queue Names Are Used -- SQL Injection Risk Surface

### 2.1 Table Name Helpers

Queue names are transformed into table/collection names via `TableNameHelper` classes. All of them concatenate the queue name directly:

**Relational (PostgreSQL, SQLite)** -- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/TableNameHelper.cs`:
- `QueueName` => `_connectionInformation.QueueName` (line 77)
- `MetaDataName` => `string.Concat(QueueName, "MetaData")` (line 85)
- `StatusName` => `string.Concat(QueueName, "Status")` (line 93)
- `ConfigurationName` => `string.Concat(QueueName, "Configuration")` (line 101)
- `ErrorTrackingName` => `string.Concat(QueueName, "ErrorTracking")` (line 109)
- `MetaDataErrorsName` => `string.Concat(QueueName, "MetaDataErrors")` (line 117)
- `HistoryName` => `string.Concat(QueueName, "History")` (line 128)

**SQL Server** -- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerTableNameHelper.cs`:
- `QueueName` => `_schema.Schema + "." + _connectionInformation.QueueName` (line 82) -- prepends schema
- All other names derived identically via `string.Concat(QueueName, "Suffix")`

**LiteDB** -- `Source/DotNetWorkQueue.Transport.LiteDB/Basic/TableNameHelper.cs`:
- Identical pattern to RelationalDatabase `TableNameHelper`. Used as LiteDB collection names.

### 2.2 SQL String Construction -- Direct Interpolation

Table names derived from queue names are **directly interpolated into SQL command strings** without parameterization. Table/collection names cannot be parameterized in SQL -- they must appear as identifiers.

**`CommandStringCache.GetCommand(type, params object[] input)`** (line 78-81 of `CommandStringCache.cs`):
```csharp
public string GetCommand(CommandStringTypes type, params object[] input)
{
    return string.Format(GetCommand(type), input);
}
```
This is called by `DeleteTableCommandPrepareHandler` (line 41-42):
```csharp
dbCommand.CommandText = _commandCache.GetCommand(commandType, command.Table);
```

**SQL Server `SqlServerCommandStringCache`** examples of queue name in SQL:
- `DeleteTable`: `"IF OBJECT_ID('{0}', 'U') IS NOT NULL DROP TABLE {0};"` -- **`{0}` is the table name** (line 151)
- All SELECT/INSERT/UPDATE/DELETE statements use `TableNameHelper` properties directly in `$""` strings:
  - `$"SELECT ... FROM {TableNameHelper.StatusName} WITH (NOLOCK)"` (line 158)
  - `$"DELETE FROM {TableNameHelper.ErrorTrackingName} WHERE ..."` (line 201)
  - `$"UPDATE {TableNameHelper.QueueName} SET Body = @Body ..."` (line 229)

**SQL Server Schema `Table.Script()`** (line 108 of `Schema/Table.cs`):
```csharp
text.AppendFormat("CREATE TABLE [{0}].[{1}](\r\n", Owner, Name);
```
The `Name` comes from `_tableNameHelper.QueueName` (via `SqlServerMessageQueueSchema`).

**Note**: SQL Server uses `[brackets]` around the owner/schema in `CREATE TABLE`, but the table name itself in most queries is **unbracketed** (e.g., `FROM dbo.MyQueueMetaData`).

### 2.3 Redis Key Construction

**`RedisNames`** (`Source/DotNetWorkQueue.Transport.Redis/Basic/RedisNames.cs`) builds key names via concatenation (lines 342-357):
```csharp
_names.Add("Pending", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Pending"));
// Pattern: {YADNQ_<queueName>_}Pending
```

While Redis keys don't have SQL injection risk, malicious queue names containing `}` could break the hash-tag grouping (the `{...}` pattern is used for Redis Cluster slot hashing).

### 2.4 LiteDB Collection Names

LiteDB collection names are derived from queue name + suffix. LiteDB has its own collection name restrictions.

### 2.5 Summary of Risk Surface

| Transport | Injection Type | Mechanism | Severity |
|-----------|---------------|-----------|----------|
| SQL Server | SQL injection via table names | Queue name concatenated into DDL/DML without quoting in most queries | **High** |
| PostgreSQL | SQL injection via table names | Same pattern as SQL Server via shared `CommandStringCache` | **High** |
| SQLite | SQL injection via table names | Same pattern via shared `CommandStringCache` | **High** |
| Redis | Key manipulation / hash-tag breaking | Queue name in `{YADNQ_<name>_}` pattern | **Medium** |
| LiteDB | Collection name injection | Queue name used as collection name prefix | **Medium** |
| Memory | None | In-memory dictionary keys only | **Low** |

---

## 3. Queue Names Used in Existing Tests

### 3.1 Unit Test Queue Names

Surveying all connection info test files:

| Test File | Queue Names Used | Compliant with `[a-zA-Z0-9_.]`? |
|-----------|-----------------|----------------------------------|
| `SqlServer.Tests/SqlConnectionInformationTests.cs` | `string.Empty`, `"blah"` | Yes |
| `PostgreSQL.Tests/SqlConnectionInformationTests.cs` | `string.Empty`, `"blah"` | Yes |
| `SQLite.Tests/SQLiteConnectionInformationTests.cs` | `string.Empty`, `"blah"` | Yes |
| `LiteDb.Tests/LiteDbConnectionInformationTests.cs` | `"blah"` | Yes |
| `Redis.Tests/RedisConnectionInfoTests.cs` | `"test"` | Yes |
| `Tests/Transport/Memory/ConnectionInformationTests.cs` | `"test"` | Yes |
| `Tests/Configuration/BaseConnectionInformationTests.cs` | `fixture.Create<string>()` (AutoFixture) | **NO -- AutoFixture generates GUIDs with hyphens** |

**Critical finding**: `BaseConnectionInformationTests` uses `fixture.Create<string>()` which generates strings like `"4d6f5a29-2b1e-4c8a-..."`. These contain **hyphens** which would fail the proposed validation regex `[a-zA-Z0-9_.]`. However, since validation is per-transport and `BaseConnectionInformation` itself will NOT be validated (per design decision), this is acceptable. The base class tests do not construct transport-specific classes.

### 3.2 Integration Test Queue Names

All integration tests use `GenerateQueueName.Create()` which generates names like:
- SQL Server: `"INTTEST" + md5hex` -- e.g., `"INTTESTa1b2c3d4e5f6..."` (alphanumeric only, ~39 chars)
- Redis: `"IT" + md5hex` -- e.g., `"ITa1b2c3d4e5f6..."` (alphanumeric only, ~34 chars)
- Memory: `"I" + md5hex` -- e.g., `"Ia1b2c3d4e5f6..."` (alphanumeric only, ~33 chars)

All integration test queue names are **compliant** with the proposed validation rules. The `GenerateQueueName.Create()` methods explicitly strip hyphens and underscores from the MD5 hex output.

### 3.3 Other Queue Names in Tests

`QueueCreatorTests` (SQL Server, SQLite) use `fixture.Create<string>()` for queue names. These tests create queue objects but the queue name flows through the transport's connection info constructor, so these **will be affected by validation**. The AutoFixture-generated strings contain hyphens.

**Files affected**:
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs` (line 28: `new QueueConnection(queue, GoodConnection)`)
- `Source/DotNetWorkQueue.Transport.SQLite.Tests/QueueCreatorTests.cs` (line 33: `new QueueConnection(queue, _goodConnection)`)

These tests will need their `queue` variables changed from `fixture.Create<string>()` to a compliant name.

---

## 4. Existing Test Files for Connection Info

| Transport | Test File | Tests Present | Patterns |
|-----------|-----------|---------------|----------|
| Base | `Source/DotNetWorkQueue.Tests/Configuration/BaseConnectionInformationTests.cs` | GetSet_Connection, GetSet_Queue, Test_Clone, Test_Equals, Test_Server, Test_Container | MSTest, AutoFixture+NSubstitute |
| SQL Server | `Source/DotNetWorkQueue.Transport.SqlServer.Tests/SqlConnectionInformationTests.cs` | GetSet_Connection, GetSet_Connection_Bad_Exception, Test_Clone | MSTest, direct construction, `Assert.ThrowsExactly<ArgumentException>` for bad connection |
| PostgreSQL | `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/SqlConnectionInformationTests.cs` | GetSet_Connection, GetSet_Connection_Bad_Exception, Test_Clone | Identical pattern to SQL Server |
| SQLite | `Source/DotNetWorkQueue.Transport.SQLite.Tests/SQLiteConnectionInformationTests.cs` | GetSet_Connection, Test_Clone | MSTest, passes `null` as `IDbDataSource` |
| LiteDB | `Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs` | LiteDbConnectionInformation_Test, Clone_Test | MSTest, direct assertions |
| Redis | `Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs` | CreateNullInputTest, CreateTest, CloneTest | MSTest, `Assert.ThrowsExactly<NullReferenceException>` for null |
| Memory | `Source/DotNetWorkQueue.Tests/Transport/Memory/ConnectionInformationTests.cs` | ConnectionInformation_Test, Clone_Test | MSTest, direct assertions |

**Pattern for adding validation tests**: Follow the existing `GetSet_Connection_Bad_Exception` pattern used in SQL Server and PostgreSQL tests. Use `Assert.ThrowsExactly<ArgumentException>` with a delegate that constructs the connection info with an invalid queue name. Each transport test file should get new tests for:
1. Valid queue names succeed
2. Invalid characters throw `ArgumentException`
3. Empty/null queue names (decide: allow or reject)
4. Over-length queue names throw `ArgumentException` (for transports with limits)
5. Edge cases: names at exactly the max length, names with only dots, etc.

---

## 5. Convention Check

### 5.1 Guard Pattern (`Source/DotNetWorkQueue/netfx/System/Guard.cs`)

The codebase uses the NETFx `Guard` class (namespace `DotNetWorkQueue.Validation`) for parameter validation:

- `Guard.NotNull(() => param, param)` -- throws `ArgumentNullException` (line 52-57)
- `Guard.NotNullOrEmpty(() => param, param)` -- throws `ArgumentNullException`/`ArgumentException` (line 66-71)
- `Guard.IsValid(() => param, param, validateFunc, message)` -- throws `ArgumentException` if `validateFunc` returns false (line 80-84)

**Recommendation**: Use `Guard.IsValid` for queue name validation, OR add a dedicated static validation method per transport and throw `ArgumentException` directly. The `Guard.IsValid` approach would look like:
```csharp
Guard.IsValid(() => queueConnection.Queue, queueConnection.Queue,
    name => IsValidQueueName(name),
    "Queue name contains invalid characters. Only alphanumeric, underscore, and dot are allowed.");
```

However, the design decision says to throw `ArgumentException` at construction time. Using a dedicated private `ValidateQueueName` method is more consistent with the existing `ValidateConnection` pattern already in SQL Server and PostgreSQL connection info classes.

### 5.2 Existing ValidateConnection Pattern

SQL Server and PostgreSQL already have a private `ValidateConnection(string value)` method called from the constructor. The queue name validation should follow the same pattern:

```csharp
// In constructor, after base(queueConnection):
ValidateQueueName(queueConnection.Queue);

// Private method:
private static void ValidateQueueName(string name)
{
    // validation logic, throw ArgumentException on failure
}
```

### 5.3 License Headers

Production `.cs` files require the LGPL-2.1 header. Test files mostly omit it. New validation code in production files must include the header. New test methods added to existing test files should follow the file's existing convention (no header).

### 5.4 XML Documentation

All public methods/types in production code require XML doc comments (enforced by `TreatWarningsAsErrors` in Release builds).

---

## 6. Implementation Notes

### 6.1 Where to Add Validation

For each transport, add a `ValidateQueueName` call in the constructor of its connection info class:

| Transport | File to Modify | Constructor Line | Notes |
|-----------|---------------|-----------------|-------|
| SQL Server | `Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs` | After line 38 (`: base(queueConnection)`) | Add before `ValidateConnection` |
| PostgreSQL | `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs` | After line 36 | Add before `ValidateConnection` |
| SQLite | `Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs` | After line 32 | Add before `ValidateConnection` |
| LiteDB | `Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs` | After line 31 | Constructor currently has no validation |
| Redis | `Source/DotNetWorkQueue.Transport.Redis/RedisConnectionInfo.cs` | After line 35 | Add before connection validation |
| Memory | `Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs` | After line 34 | Constructor currently empty |

### 6.2 Empty Queue Name Handling

**Current behavior**: Several existing tests pass `string.Empty` as the queue name (SQL Server, PostgreSQL, SQLite connection info tests). The `TableNameHelper` handles empty names by returning `"Error-Name-Not-Set"` instead of concatenating.

**Decision needed**: Should empty queue names be allowed? The existing tests suggest they currently work. Options:
1. **Allow empty** (maintain backward compat) -- only validate non-empty names
2. **Reject empty** -- would require updating tests that pass `string.Empty`

Given that `string.Empty` is used in unit tests that test connection string parsing (not queue functionality), **allowing empty names is safest** for backward compatibility. The validation regex should only be enforced when the name is non-empty.

### 6.3 Validation Regex by Transport

Per design decisions in CONTEXT-2.md:

| Transport | Regex | Max Length | Allow Empty |
|-----------|-------|------------|-------------|
| SQL Server | `^[a-zA-Z0-9_.]+$` | 128 | Yes (existing tests) |
| PostgreSQL | `^[a-zA-Z0-9_.]+$` | 63 | Yes (existing tests) |
| SQLite | `^[a-zA-Z0-9_.]+$` | None | Yes (existing tests) |
| Redis | `^[a-zA-Z0-9_.\-]+$` (also hyphens) | 512 | No (existing test passes "test") |
| LiteDB | `^[a-zA-Z0-9_.]+$` | 256 | No |
| Memory | `^[a-zA-Z0-9_.]+$` | None | No |

### 6.4 Test Files to Modify

New validation tests should be added to these existing files:

| Transport | Test File |
|-----------|-----------|
| SQL Server | `Source/DotNetWorkQueue.Transport.SqlServer.Tests/SqlConnectionInformationTests.cs` |
| PostgreSQL | `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/SqlConnectionInformationTests.cs` |
| SQLite | `Source/DotNetWorkQueue.Transport.SQLite.Tests/SQLiteConnectionInformationTests.cs` |
| LiteDB | `Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs` |
| Redis | `Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs` |
| Memory | `Source/DotNetWorkQueue.Tests/Transport/Memory/ConnectionInformationTests.cs` |

**Tests that need queue names updated** (AutoFixture generates non-compliant names):
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs` -- change `fixture.Create<string>()` to a fixed compliant name like `"TestQueue"` for the queue parameter
- `Source/DotNetWorkQueue.Transport.SQLite.Tests/QueueCreatorTests.cs` -- same fix

### 6.5 Suggested Test Cases per Transport

1. `QueueName_Valid_Alphanumeric` -- `"MyQueue123"` succeeds
2. `QueueName_Valid_WithUnderscore` -- `"My_Queue"` succeeds
3. `QueueName_Valid_WithDot` -- `"My.Queue"` succeeds
4. `QueueName_Invalid_SqlInjection` -- `"queue; DROP TABLE Users--"` throws `ArgumentException`
5. `QueueName_Invalid_SpecialChars` -- `"queue@#$"` throws `ArgumentException`
6. `QueueName_Invalid_Spaces` -- `"my queue"` throws `ArgumentException`
7. `QueueName_Empty_Allowed` (for SQL transports) or `QueueName_Empty_Throws`
8. `QueueName_TooLong_Throws` (for transports with max length)
9. `QueueName_AtMaxLength_Succeeds` (boundary test)
10. `QueueName_Invalid_Hyphen` (for non-Redis transports) / `QueueName_Valid_WithHyphen` (for Redis)

---

## 7. Dependency Analysis

### 7.1 `using` Additions Needed

The validation logic needs `System.Text.RegularExpressions` for regex validation. This is a standard .NET library available across all target frameworks (.NET 10, 8, Framework 4.8, Standard 2.0).

Alternatively, a character-by-character check avoids the regex dependency entirely, which may be preferable for a constructor hot path.

### 7.2 No Cross-Transport Dependencies

Each transport validates independently. No shared validation code is needed in the base class or shared projects. This aligns with the design decision of per-transport validation with no changes to `BaseConnectionInformation`.

---

## Sources

1. `Source/DotNetWorkQueue/Configuration/BaseConnectionInformation.cs` -- base class, no validation
2. `Source/DotNetWorkQueue/Configuration/QueueConnection.cs` -- DTO, no validation
3. `Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs` -- SQL Server connection info
4. `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs` -- PostgreSQL connection info
5. `Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs` -- SQLite connection info
6. `Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs` -- LiteDB connection info
7. `Source/DotNetWorkQueue.Transport.Redis/RedisConnectionInfo.cs` -- Redis connection info
8. `Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs` -- Memory connection info
9. `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerTableNameHelper.cs` -- SQL Server table name construction
10. `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/TableNameHelper.cs` -- shared relational table name construction
11. `Source/DotNetWorkQueue.Transport.LiteDB/Basic/TableNameHelper.cs` -- LiteDB collection name construction
12. `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisNames.cs` -- Redis key name construction
13. `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerCommandStringCache.cs` -- SQL string interpolation with table names
14. `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/CommandStringCache.cs` -- base command cache with `string.Format`
15. `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/CommandPrepareHandler/DeleteTableCommandPrepareHandler.cs` -- table name into SQL
16. `Source/DotNetWorkQueue.Transport.SqlServer/Schema/Table.cs` -- `CREATE TABLE` SQL generation
17. `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerMessageQueueSchema.cs` -- schema definition using table names
18. `Source/DotNetWorkQueue/netfx/System/Guard.cs` -- validation guard utility
19. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/GenerateQueueName.cs` -- integration test queue name generation
20. `Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/GenerateQueueName.cs` -- Redis integration test queue names
21. `.shipyard/codebase/CONVENTIONS.md` -- project conventions

## Uncertainty Flags

- **Empty queue name policy**: The design says "throw ArgumentException at construction time" but existing tests pass `string.Empty`. Need to decide: enforce non-empty or allow empty as a special case. Recommendation is to allow empty for backward compatibility.
- **Redis `internal` class testing**: `RedisConnectionInfo` is `internal`. Need to verify `InternalsVisibleTo` is set for the test project, or tests will not compile. The existing `RedisConnectionInfoTests.cs` already constructs it directly, so this is likely already configured.
- **AutoFixture in QueueCreatorTests**: The exact set of tests using `fixture.Create<string>()` for queue names that flow through transport constructors needs a full audit. The two identified files (SQL Server, SQLite) are confirmed, but there may be others.
- **Character-by-character vs regex**: Performance implications of using `Regex` in a constructor that may be called frequently in test scenarios vs. a simple loop. No benchmarks gathered.
