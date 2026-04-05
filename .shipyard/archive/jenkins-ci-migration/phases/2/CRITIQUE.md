# Phase 2: Queue Name Validation -- Plan Critique & Feasibility Report

**Date:** 2026-03-26
**Type:** plan-review
**Plans reviewed:** PLAN-1.1 (Relational Transports), PLAN-1.2 (Non-Relational Transports)

---

## Part 1: Plan Verification

### 1.1 Task Count Check

| Plan | Tasks | Limit (3) | Status |
|------|-------|-----------|--------|
| PLAN-1.1 | 3 | 3 | PASS |
| PLAN-1.2 | 3 | 3 | PASS |

### 1.2 Wave Ordering and Dependencies

Both plans are Wave 1 with no dependencies. They are designed to execute in parallel.

| Check | Status | Evidence |
|-------|--------|----------|
| Wave ordering correct | PASS | Both plans are Wave 1, no cross-dependencies declared |
| No circular dependencies | PASS | Neither plan depends on the other |
| Parallel execution safe | PASS | File sets are completely disjoint (see 1.3 below) |

### 1.3 File Conflict Analysis (Parallel Plans)

**PLAN-1.1 files_touched:**
- `Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs`
- `Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/SqlConnectionInformationTests.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/SqlConnectionInformationTests.cs`
- `Source/DotNetWorkQueue.Transport.SQLite.Tests/SQLiteConnectionInformationTests.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs`
- `Source/DotNetWorkQueue.Transport.SQLite.Tests/QueueCreatorTests.cs`

**PLAN-1.2 files_touched:**
- `Source/DotNetWorkQueue.Transport.Redis/RedisConnectionInfo.cs`
- `Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs`
- `Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs`
- `Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs`
- `Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs`
- `Source/DotNetWorkQueue.Tests/Transport/Memory/ConnectionInformationTests.cs`

**Overlap:** NONE. Zero shared files between the two plans. Safe for parallel execution.

### 1.4 Design Decision Compliance (CONTEXT-2.md)

| Decision | Plan Compliance | Status |
|----------|----------------|--------|
| Per-transport validation (not in base class) | Both plans add validation to individual transport classes, not `BaseConnectionInformation` | PASS |
| Throw `ArgumentException` with clear message | Both plans use `throw new ArgumentException(...)` | PASS |
| Validation at construction time (fail fast) | Both plans add validation in constructors before other logic | PASS |
| Allowed chars: alphanumeric + underscore + dot (base) | PLAN-1.1 uses `^[a-zA-Z0-9_.]+$`; PLAN-1.2 uses same for LiteDB/Memory | PASS |
| Redis also allows hyphens | PLAN-1.2 uses `^[a-zA-Z0-9_.\-]+$` for Redis | PASS |
| Per-transport max lengths | SqlServer 128, PostgreSQL 63, SQLite none, Redis 512, LiteDB 256, Memory none | PASS |
| Empty queue name policy: per-transport | PLAN-1.1 allows empty (backward compat); PLAN-1.2 rejects empty | PASS |

### 1.5 Roadmap Success Criteria Coverage

| # | Roadmap Criterion | Covered By | Status |
|---|-------------------|------------|--------|
| 1 | SQL injection patterns rejected with `ArgumentException` at construction time | PLAN-1.1 Tasks 1,2 + PLAN-1.2 Tasks 1,2,3 | PASS |
| 2 | Alphanumeric, underscore, dot names accepted | PLAN-1.1 Task 3 tests + PLAN-1.2 Tasks 1,2,3 tests | PASS |
| 3 | Redis additionally accepts hyphens | PLAN-1.2 Task 1 | PASS |
| 4 | Empty and null queue names rejected | PLAN-1.2 (Redis/LiteDB/Memory reject empty); PLAN-1.1 allows empty for backward compat | CAUTION -- see Gap 1 |
| 5 | Validation error messages clearly state permitted characters | Both plans include descriptive messages | PASS |
| 6 | All existing unit tests pass | PLAN-1.1 Task 2 fixes QueueCreatorTests; PLAN-1.2 has no broken tests | PASS |
| 7 | Unit tests cover valid/invalid/boundary cases | PLAN-1.1 Task 3 + PLAN-1.2 Tasks 1,2,3 all include tests | PASS |
| 8 | Per-transport test commands all pass | Both plans include verification commands for all 6 transport test projects | PASS |

---

## Part 2: Per-Plan Feasibility Critique

### PLAN-1.1: Relational Transports (SqlServer, PostgreSQL, SQLite)

#### File Path Verification

| File | Exists | Status |
|------|--------|--------|
| `Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Transport.SqlServer.Tests/SqlConnectionInformationTests.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/SqlConnectionInformationTests.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Transport.SQLite.Tests/SQLiteConnectionInformationTests.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Transport.SQLite.Tests/QueueCreatorTests.cs` | Yes | PASS |

#### API Surface Verification

| Claim | Actual | Status |
|-------|--------|--------|
| SqlServer constructor at line 38 with `: base(queueConnection)` | Line 38: `public SqlConnectionInformation(QueueConnection queueConnection) : base(queueConnection)` | PASS |
| SqlServer calls `ValidateConnection` at line 40 | Line 40: `ValidateConnection(queueConnection.Connection);` | PASS |
| PostgreSQL constructor at line 36 | Line 36: `public SqlConnectionInformation(QueueConnection queueConnection) : base(queueConnection)` | PASS |
| PostgreSQL calls `ValidateConnection` at line 38 | Line 38: `ValidateConnection(queueConnection.Connection);` | PASS |
| SQLite constructor at line 32 with `IDbDataSource` param | Line 32: `public SqliteConnectionInformation(QueueConnection queueConnection, IDbDataSource dataSource) : base(queueConnection)` | PASS |
| Existing `ValidateConnection` private method pattern | Confirmed in all 3 files -- identical pattern to follow | PASS |

#### QueueCreatorTests `fixture.Create<string>()` Verification

| File | Occurrences | Lines | Status |
|------|-------------|-------|--------|
| SqlServer `QueueCreatorTests.cs` | 7 | 21, 37, 53, 69, 85, 105, 122 | PASS -- matches plan claim |
| PostgreSQL `QueueCreatorTests.cs` | 7 | 22, 38, 54, 70, 86, 106, 122 | PASS -- matches plan claim |
| SQLite `QueueCreatorTests.cs` | 7 | 26, 42, 53, 64, 75, 90, 101 | PASS -- matches plan claim |

**Note on exception type preservation**: After replacing `fixture.Create<string>()` with `"TestQueue"`, the queue name validation passes (it is a valid name), so the code proceeds to connect to the database. SQL Server tests expect `SqlException`, PostgreSQL tests expect `NpgsqlException` -- these will still be thrown because the connection attempt fails. SQLite tests do not assert exceptions. The fix is correct.

#### Existing Test Backward Compatibility

| Test File | Queue Names Used | Would Break? | Status |
|-----------|-----------------|--------------|--------|
| `SqlConnectionInformationTests.cs` | `string.Empty`, `"blah"` | No -- empty allowed, "blah" is compliant | PASS |
| PostgreSQL `SqlConnectionInformationTests.cs` | `string.Empty`, `"blah"` | No | PASS |
| `SQLiteConnectionInformationTests.cs` | `string.Empty`, `"blah"` | No | PASS |

#### Issues Found

**ISSUE 1.1-A (LOW)**: The SQLite connection info test class is named `SqLiteConnectionInformationTests` (capital L), not `SQLiteConnectionInformationTests` as the plan sometimes implies. The plan's test filter `--filter "FullyQualifiedName~QueueName_"` would still work since it filters by method name, not class name. No action needed.

---

### PLAN-1.2: Non-Relational Transports (Redis, LiteDB, Memory)

#### File Path Verification

| File | Exists | Status |
|------|--------|--------|
| `Source/DotNetWorkQueue.Transport.Redis/RedisConnectionInfo.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs` | Yes | PASS |
| `Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs` | Yes | PASS |
| `Source/DotNetWorkQueue.Tests/Transport/Memory/ConnectionInformationTests.cs` | Yes | PASS |

#### API Surface Verification

| Claim | Actual | Status |
|-------|--------|--------|
| Redis class is `internal` (line 33) | Line 26: `internal class RedisConnectionInfo : BaseConnectionInformation` | CAUTION -- line number off (26 not 33), but class is indeed `internal` |
| Redis constructor at line 35 | Line 35: `public RedisConnectionInfo(QueueConnection queueConnection) : base(queueConnection)` | PASS |
| Redis connection validation at line 37 | Line 37: `if (!string.IsNullOrEmpty(queueConnection.Connection))` | PASS |
| `InternalsVisibleTo` for Redis test project | `InternalsVisibleForTests.cs:21` has `[assembly: InternalsVisibleTo("DotNetWorkQueue.Transport.Redis.Tests")]` | PASS |
| LiteDB constructor at line 31 | Line 31: `public LiteDbConnectionInformation(QueueConnection queueConnection) : base(queueConnection)` | PASS |
| LiteDB `_server = "TODO; not known"` at line 33 | Line 33: `_server = "TODO; not known";` | PASS |
| Memory constructor at line 34, empty body | Line 34-37: constructor with empty body | PASS |

#### Existing Test Backward Compatibility

| Test File | Queue Names Used | Would Break? | Status |
|-----------|-----------------|--------------|--------|
| `RedisConnectionInfoTests.cs` | `"test"` | No -- compliant | PASS |
| `LiteDbConnectionInformationTests.cs` | `"blah"` | No -- compliant | PASS |
| `ConnectionInformationTests.cs` (Memory) | `"test"` | No -- compliant | PASS |

#### Redis `fixture.Create<string>()` Usage Check

6 Redis test files use `fixture.Create<string>()`, but inspection confirms they are used for command-specific data fields (e.g., `DeleteMessageCommand<string>(number)` for `QueueId`), NOT for queue names in `QueueConnection` or `RedisConnectionInfo`. These tests are NOT affected.

#### Issues Found

**ISSUE 1.2-A (LOW)**: Plan states Redis class is `internal` at "line 33" -- the actual `internal class` declaration is at line 26. The constructor IS at line 35 as stated. This is a minor documentation inaccuracy with no implementation impact.

**ISSUE 1.2-B (MEDIUM)**: The Redis `CreateNullInputTest` in `RedisConnectionInfoTests.cs` (line 18) passes `null` to the constructor: `new RedisConnectionInfo(null)`. This currently throws `NullReferenceException`. After validation is added, `ValidateQueueName(queueConnection.Queue)` would be called BEFORE the null check, and accessing `queueConnection.Queue` when `queueConnection` is `null` would ALSO throw `NullReferenceException`. However, the plan places `ValidateQueueName(queueConnection.Queue)` AFTER `base(queueConnection)`. The base constructor accesses `queueConnection.Queue` (line 37 of `BaseConnectionInformation.cs`), so the `NullReferenceException` would still be thrown in the base constructor before reaching the validation call. This test remains unaffected. No issue.

**ISSUE 1.2-C (INFORMATIONAL)**: The Redis `CreateTest` passes `"test"` as the connection string. This enters the `ValidateConnection` path (`ConfigurationOptions.Parse("test")`). StackExchange.Redis `ConfigurationOptions.Parse` treats `"test"` as a hostname, so it does not throw. This existing behavior is unaffected by the plan.

---

## Part 3: Coverage Matrix

| Roadmap Success Criterion | PLAN-1.1 | PLAN-1.2 | Coverage |
|--------------------------|----------|----------|----------|
| SQL injection patterns rejected at construction | Task 1 (SqlServer, PostgreSQL), Task 2 (SQLite) | Task 1 (Redis), Task 2 (LiteDB), Task 3 (Memory) | FULL |
| Alphanumeric + underscore + dot accepted | Task 3 (tests) | Tasks 1,2,3 (tests) | FULL |
| Redis hyphens accepted | -- | Task 1 | FULL |
| Empty/null rejection | Allows empty (backward compat) | Rejects empty | FULL (see Gap 1) |
| Clear error messages | Tasks 1,2 | Tasks 1,2,3 | FULL |
| All existing tests pass | Task 2 (QueueCreatorTests fix) | No fixes needed | FULL |
| Validation unit tests | Task 3 (9 tests x 3 transports) | Tasks 1,2,3 (8-10 tests x 3 transports) | FULL |
| Per-transport test commands | Verification section | Verification section | FULL |

---

## Gaps

### Gap 1: Inconsistent Empty Queue Name Policy (LOW RISK)

The roadmap criterion says "Empty and null queue names are rejected (some transports may already handle this)." PLAN-1.1 allows empty queue names for backward compatibility (existing SQL Server, PostgreSQL, SQLite tests use `string.Empty`). PLAN-1.2 rejects empty names (no existing tests use empty names for Redis, LiteDB, Memory).

This inconsistency is INTENTIONAL and DOCUMENTED in both CONTEXT-2.md and RESEARCH.md. The rationale is sound: changing behavior for existing tests would require modifying more files and could break user code. The roadmap criterion itself notes "(some transports may already handle this)." The parenthetical acknowledges per-transport variation.

**Verdict:** Acceptable. No action needed.

### Gap 2: Null Queue Name Handling Not Explicit (LOW RISK)

The roadmap criterion says "Empty and null queue names are rejected." The plans address empty names explicitly but do not explicitly address `null` queue names. However, `QueueConnection` stores the queue name, and passing `null` to `string.IsNullOrEmpty()` returns `true`, so:
- For PLAN-1.1: `null` is treated like empty (allowed, returns early).
- For PLAN-1.2: `null` triggers `IsNullOrEmpty` which is `true`, so it throws `ArgumentException`.

The behavior is correct for both approaches, though PLAN-1.1 technically allows `null` names. In practice, `QueueConnection` itself may already handle `null` differently, and `BaseConnectionInformation` stores it. This is unlikely to cause issues in production since no one intentionally uses `null` queue names.

**Verdict:** Acceptable. No action needed, but builder should be aware.

### Gap 3: No RelationalDatabase.Tests Impact Assessment (INFORMATIONAL)

The `DotNetWorkQueue.Transport.RelationalDatabase.Tests` project is listed in the roadmap success criteria test commands but is NOT touched or verified by either plan. This project tests the shared relational database layer (including `TableNameHelper`). Since validation happens at construction time (before `TableNameHelper` is ever invoked), and the `RelationalDatabase.Tests` project likely creates mock `IConnectionInformation` via NSubstitute (not real connection info classes), it should be unaffected. But neither plan explicitly verifies this.

**Verdict:** Low risk. Builder could optionally run `dotnet test "Source\DotNetWorkQueue.Transport.RelationalDatabase.Tests\DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj"` as a regression check.

---

## Recommendations

1. **No blocking issues found.** Both plans are ready for execution.
2. **Optional regression check**: After implementation, run the RelationalDatabase.Tests project to confirm no unexpected breakage.
3. **Builder awareness**: The SQLite test class is named `SqLiteConnectionInformationTests` (not `SQLiteConnectionInformationTests`). When adding test methods, ensure they are added to the correct class.
4. **Builder awareness**: The Memory transport's `ConnectionInformation.cs` is inside the CORE project (`DotNetWorkQueue`), not a separate transport project. Changes to this file affect the core project build, though the risk is minimal since it is a leaf class.

---

## Overall Verdict

**READY**

Both plans are well-structured, thoroughly researched, and feasible. All file paths verified as correct. All API surfaces (constructor signatures, class visibility, existing patterns) match the plan descriptions. The QueueCreatorTests `fixture.Create<string>()` issue is correctly identified and the fix approach is sound. No file conflicts between parallel plans. Design decisions from CONTEXT-2.md are correctly reflected. All roadmap success criteria are covered. The identified gaps are low-risk, intentional design choices with documented rationale.
