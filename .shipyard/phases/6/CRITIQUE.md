# Phase 6 Plan Critique (Feasibility Stress Test)

**Date:** 2026-05-14

## Overall Verdict: REVISE

Plans are architecturally sound and operationally feasible, but **critical hidden dependencies within waves are undeclared** and will break the parallel-execution model. Specifically:

- PLAN-1.2 must declare `dependencies: [1.1]` (TestBase class inheritance)
- PLAN-2.2 must declare `dependencies: [2.1]` (TestBase class inheritance)

Without these declarations, running PLAN-1.2 and PLAN-1.1 in true parallel will fail at test-discovery time with "class not found" errors for `SqlServerOutboxIntegrationTestBase`.

---

## Per-Plan Findings

### PLAN-1.1: SqlServer Outbox Method-Matrix Tests

**Status:** READY (standalone, no undeclared dependencies)

- **Files:** 3 new test files + 1 base class
- **Base class:** `SqlServerOutboxIntegrationTestBase` (lines 89–260 of plan)
- **Coverage:** 8 integration tests exercising sync/async × single/batch × commit/rollback
- **API surface:** Verified:
  - `QueueCreationContainer<SqlServerMessageQueueInit>` ✓
  - `SqlServerMessageQueueCreation` class ✓ (casing: uppercase SQL filename, but class is `SqlServer`)
  - `.Scope` property present ✓
  - `QueueContainer<SqlServerMessageQueueInit>` ✓
  - `CreateProducer<T>` returns `IProducerQueue<T>` ✓
  - Cast to `IRelationalProducerQueue<T>` valid ✓
  - `SqlServerTableNameHelper` exists ✓
  - `SqlConnectionInformation` in `DotNetWorkQueue.Transport.SqlServer` namespace ✓
  - `ConnectionInfo.ConnectionString` static property exists ✓
  - `FakeMessage` and `GenerateMessage.Create<T>()` available ✓
- **Naming:** Test class names match spec exactly ✓
- **Verification commands:** Concrete and runnable ✓

**Minor note:** Plan references `SqlConnectionInformation` from `DotNetWorkQueue.Transport.RelationalDatabase.Basic` (line 100) but actual class is in `DotNetWorkQueue.Transport.SqlServer` (root namespace, NOT .Basic). Builder should use correct namespace.

---

### PLAN-1.2: SqlServer Outbox — Validation + Retry Bypass + AdditionalMessageData

**Status:** REVISE — undeclared hidden dependency on PLAN-1.1

- **Dependencies declared:** `[]` (EMPTY)
- **Dependencies required:** `[1.1]` (REQUIRED)
- **Why:** All three test classes inherit from `SqlServerOutboxIntegrationTestBase` created in PLAN-1.1 Task 1:
  - Line 106: `public class SqlServerOutboxValidationTests : SqlServerOutboxIntegrationTestBase`
  - Line 236: `public class SqlServerOutboxRetryBypassTests : SqlServerOutboxIntegrationTestBase`
  - Line 361: `public class SqlServerOutboxAdditionalDataTests : SqlServerOutboxIntegrationTestBase`

The plan's Context (lines 42–46) acknowledges this ordering:
> "the build orders Task 1 of PLAN-1.1 before any PLAN-1.2 task because the base class symbol must compile first. Builder should land PLAN-1.1 Task 1 first."

However, this is documented in prose only. The `dependencies:` frontmatter declares `[]`, which means Shipyard will attempt to run PLAN-1.2 in parallel with PLAN-1.1. When the test project tries to compile PLAN-1.2's test classes before PLAN-1.1's base class is compiled, the build fails with `error CS0246: The type or namespace name 'SqlServerOutboxIntegrationTestBase' could not be found`.

**Impact:** The parallel wave-1 execution model is broken. Both plans are Wave 1 and thus should run in parallel; if one depends on the other, the dependency must be explicit in the frontmatter.

---

### PLAN-2.1: PostgreSQL Outbox Method-Matrix Tests

**Status:** READY (standalone, no undeclared dependencies)

- **Files:** 3 new test files + 1 base class
- **Base class:** `PostgreSqlOutboxIntegrationTestBase` (lines 80–240 of plan)
- **Coverage:** 8 integration tests (mirror PLAN-1.1 for PG)
- **API surface verified:**
  - `QueueCreationContainer<PostgreSqlMessageQueueInit>` ✓
  - `PostgreSqlMessageQueueCreation` class ✓
  - `.Scope` property present ✓
  - `QueueContainer<PostgreSqlMessageQueueInit>` ✓
  - `CreateProducer<T>` returns `IProducerQueue<T>` ✓
  - Cast to `IRelationalProducerQueue<T>` valid ✓
  - `NpgsqlConnection`, `NpgsqlTransaction` available ✓
  - TableNameHelper: plan says "in DotNetWorkQueue.Transport.PostgreSQL.Basic" (line 90) but ACTUAL location is `DotNetWorkQueue.Transport.RelationalDatabase.Basic` (shared base, no PG override)
  - `SqlConnectionInformation` in `DotNetWorkQueue.Transport.PostgreSQL` namespace ✓
  - `ConnectionInfo.ConnectionString` exists ✓
- **Naming:** Test class names correct ✓
- **Verification commands:** Concrete ✓
- **Wave dependency:** Correctly declares `dependencies: [1.1, 1.2]` for Wave 2 gating ✓

**Namespace correction needed:** Line 90 says `DotNetWorkQueue.Transport.PostgreSQL.Basic` for `TableNameHelper`, should be `DotNetWorkQueue.Transport.RelationalDatabase.Basic`.

---

### PLAN-2.2: PostgreSQL Outbox — Validation + Retry Bypass + AdditionalMessageData

**Status:** REVISE — undeclared hidden dependency on PLAN-2.1

- **Dependencies declared:** `[1.1, 1.2]` (Wave-2 gate: correct)
- **Dependencies required:** `[1.1, 1.2, 2.1]` (also PLAN-2.1's base class)
- **Why:** All three test classes inherit from `PostgreSqlOutboxIntegrationTestBase` created in PLAN-2.1 Task 1:
  - Line 85: `public class PostgreSqlOutboxValidationTests : PostgreSqlOutboxIntegrationTestBase`
  - Line 194: `public class PostgreSqlOutboxRetryBypassTests : PostgreSqlOutboxIntegrationTestBase`
  - Line 293: `public class PostgreSqlOutboxAdditionalDataTests : PostgreSqlOutboxIntegrationTestBase`

The plan's Context (lines 39–40) acknowledges:
> "PLAN-2.1 base class (`PostgreSqlOutboxIntegrationTestBase`) — same wave; Task 1 lands first."

But `dependencies: [1.1, 1.2]` does not include `2.1`. Wave 2 gating requires PLAN-1.1/1.2 to land before Wave 2 executes, but within Wave 2, PLAN-2.1 and PLAN-2.2 could run in parallel. If they do, PLAN-2.2's test class inheritance on `PostgreSqlOutboxIntegrationTestBase` will fail to compile.

**Impact:** Within Wave 2, PLAN-2.1 Task 1 must complete before PLAN-2.2 tasks execute. This must be declared in `dependencies: [1.1, 1.2, 2.1]`.

---

## §4. Hidden Wave-1 Dependency Check (CRITICAL)

### Finding

Both Wave-1 plans (PLAN-1.1 and PLAN-1.2) and Wave-2 intra-wave dependency (PLAN-2.1 → PLAN-2.2) have **undeclared symbol dependencies** that will cause compile failures if plans execute in true parallel:

| Plan | Declared Dependencies | Actual Dependency | Issue |
|---|---|---|---|
| PLAN-1.2 | `[]` | PLAN-1.1 (base class) | **Hidden** — declared as empty, but test code inherits from base created in PLAN-1.1 |
| PLAN-2.2 | `[1.1, 1.2]` | PLAN-2.1 (base class) | **Hidden** — Wave-2 gate satisfied, but intra-wave dependency on PLAN-2.1 not declared |

### Root Cause

Plans document intra-wave ordering in prose ("the build orders Task 1 of PLAN-1.1 before any PLAN-1.2 task") but do not encode the dependency in the YAML `dependencies:` field. The architect relied on implicit ordering (Task 1 lands first within the same plan group) instead of explicit inter-plan dependency declarations.

### Outcome

- **PLAN-1.1 + PLAN-1.2:** If builder runs tests immediately after both compile, PLAN-1.2 tests will fail to instantiate test class definitions because `SqlServerOutboxIntegrationTestBase` may not yet be compiled (true parallel execution).
- **PLAN-2.1 + PLAN-2.2:** Within Wave 2, PLAN-2.2 tests will fail similarly.

The prose acknowledgment in each plan ("builder should land PLAN-1.1 Task 1 first") is helpful but insufficient. Shipyard's parallel model requires explicit `dependencies:` declarations.

---

## §5. API Surface Validation

| Assumption | Codebase Result | Status |
|---|---|---|
| `QueueCreationContainer<SqlServerMessageQueueInit>` constructor exists | ✓ Verified in `/Source/DotNetWorkQueue/QueueCreationContainer.cs:31` | PASS |
| `SqlServerMessageQueueCreation.Scope: ICreationScope` property | ✓ Line 119 of SQLServerMessageQueueCreation.cs | PASS |
| `QueueContainer<T>.CreateProducer<T>` returns `IProducerQueue<T>` | ✓ Line 338 of QueueContainer.cs | PASS |
| `IRelationalProducerQueue<T>` interface exists with Send overloads | ✓ Verified (4 Send variants documented) | PASS |
| `SqlServerTableNameHelper` class | ✓ Exists in SqlServer/Basic namespace | PASS |
| `SqlConnectionInformation` class | ✓ SqlServer namespace (NOT .Basic) | PASS with namespace correction needed in plans |
| `PostgreSQL TableNameHelper` | ✗ **NOT in PostgreSQL.Basic** — shared base class in RelationalDatabase.Basic | FAIL — plan doc is wrong (line 90, PLAN-2.1) |
| `SqlConnectionInformation` (PostgreSQL) | ✓ PostgreSQL namespace (root, NOT .Basic) | PASS with namespace correction needed |
| `ConnectionInfo.ConnectionString` static property | ✓ Both projects have `ConnectionString.cs` with static `ConnectionInfo` class | PASS |
| `FakeMessage`, `GenerateMessage.Create<T>()` | ✓ DotNetWorkQueue.IntegrationTests.Shared | PASS |
| `ExternalTransactionValidator` registered | ✓ Both SqlServer and PostgreSQL register in MessageQueueInit | PASS |
| `IRetrySkippable` marker interface | ✓ RelationalDatabase/IRetrySkippable.cs | PASS |
| Priority column (SqlServer byte, PostgreSQL int) | ✓ SqlServer is byte (line 159 VerifyQueueData.cs), PG is int (line 100) | PASS |
| `AdditionalMessageData` class with `.SetPriority()` method | ✓ Verified in SharedClasses.cs usage | PASS |
| CorrelationId implementation available for tests | ⚠ Requires builder to substitute existing type; inline shells in plans won't compile (ICorrelationId.Id is ISetting, not IMessageId) | CAUTION — documented in plans as builder fallback |

**Summary:** Core APIs are present and correct. Namespace paths in PLAN-2.1 are documented incorrectly but easily corrected. CorrelationId shells are acknowledged in plans as placeholders.

---

## §6. File & Path Validation

| Path | Status | Note |
|---|---|---|
| `/Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/` | EXISTS | Folder: no dot. Csproj: `DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj` (WITH dot) |
| `/Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/` | EXISTS | Folder: dot before Integration. Csproj: `DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj` (matches) |
| `Outbox/` subfolder | NOT EXISTS | Plans create this; parent directories writable ✓ |
| `connectionstring.txt` copy to bin | CONFIGURED | Both csproj files have `<None Update="connectionstring.txt">` with CopyToOutputDirectory ✓ |

---

## §7. Test Naming & Verification Commands

All test class names and methods are exact matches to plan specifications. Verification commands are concrete and executable:

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" \
  -c Debug \
  --filter "FullyQualifiedName~SqlServerOutboxSendTests|FullyQualifiedName~SqlServerOutboxSendAsyncTests"
```

Commands are correctly formatted (filter expressions match test class names, casing correct).

---

## §8. Complexity Flags

| Plan | Files | Directories | Complexity |
|---|---|---|---|
| PLAN-1.1 | 3 (TestBase + 2 test classes) | 1 (Outbox/) | LOW |
| PLAN-1.2 | 3 (3 test classes) | 1 (Outbox/) | LOW |
| PLAN-2.1 | 3 (TestBase + 2 test classes) | 1 (Outbox/) | LOW |
| PLAN-2.2 | 3 (3 test classes) | 1 (Outbox/) | LOW |

No plans touch >3 files or >1 new directory. No cross-project file modifications. No NuGet dependency additions.

---

## Recommendations

### 1. CRITICAL: Fix Hidden Dependencies (Blocking)

Before execution:

**PLAN-1.2:** Update frontmatter to:
```yaml
dependencies: [1.1]
```

**PLAN-2.2:** Update frontmatter to:
```yaml
dependencies: [1.1, 1.2, 2.1]
```

**Rationale:** Makes explicit the documented prose requirement ("Task 1 of PLAN-1.1 before any PLAN-1.2 task"). Shipyard's execution engine will enforce correct ordering.

### 2. Fix Namespace Paths in PLAN-2.1

**Line 90:** Change
```
- `TableNameHelper` (in `DotNetWorkQueue.Transport.PostgreSQL.Basic`) instead of
```
to
```
- `TableNameHelper` (in `DotNetWorkQueue.Transport.RelationalDatabase.Basic`) instead of
```

**Lines 201, 343:** Ensure builder imports are from correct namespace.

### 3. Fix SqlConnectionInformation Namespace in PLAN-1.1

**Line 100:** Base class already imports from correct root namespace (verified in code), but prose at line 100 should clarify it's `DotNetWorkQueue.Transport.SqlServer` (NOT `.Basic`). Current code shape is correct; doc clarity only.

### 4. CorrelationId Shells: Confirm Fallback Before Build

Plans include placeholder inline `CorrelationIdContainer` and `MessageCorrelationId` classes in PLAN-1.2 Task 3 and PLAN-2.2 Task 3. These won't compile as-is:
- `ICorrelationId.Id` is type `ISetting` (not `IMessageId`)
- Existing `DotNetWorkQueue.Transport.Memory.Basic.MessageCorrelationId` is public and takes a `Guid` constructor

**Action:** Builder should either (a) substitute the existing Memory.MessageCorrelationId (if test project references it) or (b) implement a minimal concrete `ICorrelationId` conforming to the actual interface signature. Plans document this as a known fallback; no blocker.

---

## Verdict

### Current State: **REVISE**

Plans are operationally sound and all APIs exist. However, **undeclared hidden dependencies will cause build failures** if Wave 1 (or Wave 2 intra-wave) tasks execute in true parallel. These dependencies are documented in prose but not encoded in YAML `dependencies:` fields.

### To Proceed: **READY** (after fixes applied)

Apply the four recommendations above:

1. Add `dependencies: [1.1]` to PLAN-1.2 frontmatter.
2. Add `dependencies: [1.1, 1.2, 2.1]` to PLAN-2.2 frontmatter.
3. Fix namespace paths in PLAN-2.1 doc.
4. Confirm CorrelationId implementation with builder (documented fallback, no revision needed).

Once these are complete, plans are **READY for execution** with high confidence.

---

## Summary Checklist

- [x] Test project paths exist and are writable
- [x] Integration test APIs (containers, producer queues, table helpers) verified
- [x] Verification commands are concrete and runnable
- [x] All test class names match spec exactly
- [x] No regressions expected (subfolders are new, no overwrites)
- [x] No new NuGet dependencies required
- [ ] **Hidden dependencies properly declared (BLOCKING)**
- [x] Namespace paths correct (with minor doc clarifications needed)
- [x] File count and complexity within normal bounds

**Phase 6 can proceed to BUILD stage once hidden dependencies are declared in YAML.**
