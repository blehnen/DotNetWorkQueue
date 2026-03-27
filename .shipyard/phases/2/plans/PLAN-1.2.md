# Plan 1.2: Queue Name Validation -- Non-Relational Transports (Redis, LiteDB, Memory)

---
phase: queue-name-validation
plan: "1.2"
wave: 1
dependencies: []
must_haves:
  - Per-transport queue name validation in Redis, LiteDB, and Memory connection info constructors
  - Redis allows hyphens in addition to alphanumeric, underscore, and dot
  - LiteDB enforces 256-character max length
  - Memory has no max length
  - Empty queue names rejected for Redis, LiteDB, and Memory (no existing tests pass empty)
  - New validation unit tests per transport
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis/RedisConnectionInfo.cs
  - Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs
  - Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs
  - Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs
  - Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs
  - Source/DotNetWorkQueue.Tests/Transport/Memory/ConnectionInformationTests.cs
tdd: true
---

## Context

Redis keys, LiteDB collection names, and Memory dictionary keys are all derived from queue names without validation. While these don't have the same SQL injection risk as relational transports, malicious queue names can break Redis Cluster hash-tag grouping (`{...}` patterns), violate LiteDB collection name restrictions, or cause unexpected behavior.

This plan adds validation to the three non-relational transports. Redis uses a broader character set (allowing hyphens) because Redis keys commonly contain hyphens. LiteDB enforces a 256-character limit. Memory transport enforces only the character set with no length limit.

Unlike the relational transports (Plan 1.1), none of these transports have existing tests that pass empty queue names, so empty names are rejected here.

## Dependencies

None. This is a Wave 1 plan. Can execute in parallel with Plan 1.1.

## Tasks

### Task 1: Add queue name validation to Redis connection info

**Files:**
- `Source/DotNetWorkQueue.Transport.Redis/RedisConnectionInfo.cs` (modify)
- `Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs` (modify)

**Action:** modify + test

**Description:**

**Redis connection info** (`Source/DotNetWorkQueue.Transport.Redis/RedisConnectionInfo.cs`):
- The class is `internal` (line 33). This is fine -- the test project already has `InternalsVisibleTo` access.
- Add `using System.Text.RegularExpressions;` to the imports
- In the constructor (after line 35, `base(queueConnection)`), add `ValidateQueueName(queueConnection.Queue);` as the first statement, before the existing connection string validation (line 37: `if (!string.IsNullOrWhiteSpace(queueConnection.Connection))`)
- Add the private static method -- Redis allows hyphens in addition to the base character set, and enforces a 512-character max:
  ```
  /// <summary>
  /// Validates that the queue name contains only safe characters for use as a Redis key component.
  /// </summary>
  private static void ValidateQueueName(string name)
  {
      if (string.IsNullOrEmpty(name))
          throw new ArgumentException("Queue name must not be null or empty.", nameof(name));
      if (name.Length > 512)
          throw new ArgumentException($"Queue name exceeds maximum length of 512 characters. Got {name.Length} characters.", nameof(name));
      if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_.\-]+$"))
          throw new ArgumentException("Queue name contains invalid characters. Only alphanumeric characters, underscores, dots, and hyphens are allowed.", nameof(name));
  }
  ```

**Redis tests** (`Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs`):
- Add new test methods following the existing MSTest pattern in the file. The existing tests use `"test"` as a valid queue name and construct via `new RedisConnectionInfo(new QueueConnection("test", validConn))`. Check the existing file for the connection string format used.
- Add these tests:
  1. `QueueName_Valid_Alphanumeric` -- `"MyQueue123"`, no exception
  2. `QueueName_Valid_WithHyphen` -- `"my-queue"`, no exception (Redis-specific: hyphens allowed)
  3. `QueueName_Valid_WithUnderscoreAndDot` -- `"my_queue.v2"`, no exception
  4. `QueueName_Invalid_SqlInjection` -- `"queue; DROP TABLE"`, `ArgumentException`
  5. `QueueName_Invalid_SpecialChars` -- `"queue@#$%"`, `ArgumentException`
  6. `QueueName_Invalid_Spaces` -- `"my queue"`, `ArgumentException`
  7. `QueueName_Invalid_CurlyBrace` -- `"queue{tag}"`, `ArgumentException` (prevents Redis hash-tag manipulation)
  8. `QueueName_Empty_Throws` -- `""`, `ArgumentException`
  9. `QueueName_ExceedsMaxLength_512` -- `new string('a', 513)`, `ArgumentException`
  10. `QueueName_AtMaxLength_512` -- `new string('a', 512)`, no exception

- Construction pattern: `new RedisConnectionInfo(new QueueConnection(queueName, connectionString))`
- For invalid name tests, use `Assert.ThrowsExactly<ArgumentException>(() => new RedisConnectionInfo(new QueueConnection(badName, connStr)))`
- Ensure `using DotNetWorkQueue.Configuration;` is present in imports

**Acceptance Criteria:**
- `new RedisConnectionInfo(new QueueConnection("my-queue.v2", conn))` succeeds (hyphens OK for Redis)
- `new RedisConnectionInfo(new QueueConnection("queue{tag}", conn))` throws `ArgumentException`
- `new RedisConnectionInfo(new QueueConnection("", conn))` throws `ArgumentException`
- All existing Redis connection info tests continue to pass
- All new tests pass: `dotnet test "Source\DotNetWorkQueue.Transport.Redis.Tests\DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~RedisConnectionInfoTests"`

---

### Task 2: Add queue name validation to LiteDB connection info

**Files:**
- `Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs` (modify)
- `Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs` (modify)

**Action:** modify + test

**Description:**

**LiteDB connection info** (`Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs`):
- Add `using System.Text.RegularExpressions;` to the imports
- In the constructor (after line 31, `: base(queueConnection)`), add `ValidateQueueName(queueConnection.Queue);` as the first statement (before `_server = "TODO; not known";` on line 33)
- Add the private static method -- standard character set with 256-char max:
  ```
  /// <summary>
  /// Validates that the queue name contains only safe characters for use as a LiteDB collection name.
  /// </summary>
  private static void ValidateQueueName(string name)
  {
      if (string.IsNullOrEmpty(name))
          throw new ArgumentException("Queue name must not be null or empty.", nameof(name));
      if (name.Length > 256)
          throw new ArgumentException($"Queue name exceeds maximum length of 256 characters. Got {name.Length} characters.", nameof(name));
      if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_.]+$"))
          throw new ArgumentException("Queue name contains invalid characters. Only alphanumeric characters, underscores, and dots are allowed.", nameof(name));
  }
  ```

**LiteDB tests** (`Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs`):
- Add new test methods. The existing tests use `"blah"` as a valid queue name. Check the file for the construction pattern used.
- Add these tests:
  1. `QueueName_Valid_Alphanumeric` -- `"MyQueue123"`, no exception
  2. `QueueName_Valid_WithUnderscoreAndDot` -- `"my_queue.v2"`, no exception
  3. `QueueName_Invalid_SqlInjection` -- `"queue; DROP TABLE"`, `ArgumentException`
  4. `QueueName_Invalid_Hyphen` -- `"my-queue"`, `ArgumentException` (hyphens NOT allowed for LiteDB)
  5. `QueueName_Invalid_Spaces` -- `"my queue"`, `ArgumentException`
  6. `QueueName_Empty_Throws` -- `""`, `ArgumentException`
  7. `QueueName_ExceedsMaxLength_256` -- `new string('a', 257)`, `ArgumentException`
  8. `QueueName_AtMaxLength_256` -- `new string('a', 256)`, no exception

- Construction pattern: `new LiteDbConnectionInformation(new QueueConnection(queueName, connectionString))`
- Ensure `using DotNetWorkQueue.Configuration;` is present in imports

**Acceptance Criteria:**
- `new LiteDbConnectionInformation(new QueueConnection("my-queue", conn))` throws `ArgumentException`
- `new LiteDbConnectionInformation(new QueueConnection("", conn))` throws `ArgumentException`
- `new LiteDbConnectionInformation(new QueueConnection(new string('a', 256), conn))` succeeds
- All existing LiteDB connection info tests continue to pass
- All new tests pass: `dotnet test "Source\DotNetWorkQueue.Transport.LiteDb.Tests\DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~LiteDbConnectionInformation"`

---

### Task 3: Add queue name validation to Memory transport connection info

**Files:**
- `Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs` (modify)
- `Source/DotNetWorkQueue.Tests/Transport/Memory/ConnectionInformationTests.cs` (modify)

**Action:** modify + test

**Description:**

**Memory connection info** (`Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs`):
- Add `using System.Text.RegularExpressions;` to the imports
- In the constructor (after line 34, `: base(queueConnection)`), add `ValidateQueueName(queueConnection.Queue);` inside the currently-empty constructor body
- Add the private static method -- standard character set, no max length:
  ```
  /// <summary>
  /// Validates that the queue name contains only safe characters.
  /// </summary>
  private static void ValidateQueueName(string name)
  {
      if (string.IsNullOrEmpty(name))
          throw new ArgumentException("Queue name must not be null or empty.", nameof(name));
      if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_.]+$"))
          throw new ArgumentException("Queue name contains invalid characters. Only alphanumeric characters, underscores, and dots are allowed.", nameof(name));
  }
  ```

**Note:** This file is in the core `DotNetWorkQueue` project (not a separate transport project) since the Memory transport lives inside the core library. It still gets per-transport validation -- the `ConnectionInformation` class at `Transport/Memory/ConnectionInformation.cs` is the Memory transport's connection info, not the base class.

**Memory tests** (`Source/DotNetWorkQueue.Tests/Transport/Memory/ConnectionInformationTests.cs`):
- Add new test methods. The existing tests use `"test"` as a valid queue name.
- Add these tests:
  1. `QueueName_Valid_Alphanumeric` -- `"MyQueue123"`, no exception
  2. `QueueName_Valid_WithUnderscoreAndDot` -- `"my_queue.v2"`, no exception
  3. `QueueName_Invalid_SqlInjection` -- `"queue; DROP TABLE"`, `ArgumentException`
  4. `QueueName_Invalid_Hyphen` -- `"my-queue"`, `ArgumentException`
  5. `QueueName_Invalid_Spaces` -- `"my queue"`, `ArgumentException`
  6. `QueueName_Empty_Throws` -- `""`, `ArgumentException`

- Construction pattern: `new ConnectionInformation(new QueueConnection(queueName, ""))` (Memory transport uses empty or arbitrary connection strings)
- Ensure `using DotNetWorkQueue.Configuration;` is present in imports

**Acceptance Criteria:**
- `new ConnectionInformation(new QueueConnection("valid_name", ""))` succeeds
- `new ConnectionInformation(new QueueConnection("my-queue", ""))` throws `ArgumentException`
- `new ConnectionInformation(new QueueConnection("", ""))` throws `ArgumentException`
- No max length restriction for Memory transport (long names accepted)
- All existing Memory connection info tests continue to pass
- All new tests pass: `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~ConnectionInformationTests"`

## Verification

Run all affected test suites to confirm both existing and new tests pass:

```bash
dotnet test "Source\DotNetWorkQueue.Transport.Redis.Tests\DotNetWorkQueue.Transport.Redis.Tests.csproj" && \
dotnet test "Source\DotNetWorkQueue.Transport.LiteDb.Tests\DotNetWorkQueue.Transport.LiteDb.Tests.csproj" && \
dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"
```
