# Code Fixes Plan

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Fix 14 code-level items from CONCERNS.md and ISSUES.md across ~20 `.cs` source files.

**Architecture:** Small, independent edits to production and test `.cs` files. No behavioral changes.

**Tech Stack:** C# / .NET / MSTest / FluentAssertions

---

## Pre-Flight: Verify Already-Resolved Issues

Before starting, verify these issues that appear to be already fixed:

```bash
# ISSUE-002: Check if all transports already have RegexOptions.Compiled
grep -rn "RegexOptions.Compiled" Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs Source/DotNetWorkQueue.Transport.Redis/RedisConnectionInfo.cs Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs
# Expected: All 5 files should show RegexOptions.Compiled — if so, ISSUE-002 is already resolved

# ISSUE-001: Check if fixture variables are still unused
grep -n "var fixture" Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs Source/DotNetWorkQueue.Transport.SQLite.Tests/QueueCreatorTests.cs
# If fixture variables exist but are not used later in the method, they need removal. If no fixture lines exist, ISSUE-001 is already resolved.
```

If ISSUE-001 and/or ISSUE-002 are already resolved, skip those tasks and note in ISSUES.md.

---

## Task 1: Fix Stale XML Doc Comment on Memory ConnectionInformation (ISSUE-005)

**Files:**
- Modify: `Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs:28`

**Step 1: Fix the class summary**

Change:
```csharp
/// <summary>
/// Contains connection information for a SQL server queue
/// </summary>
```

To:
```csharp
/// <summary>
/// Contains connection information for a memory queue
/// </summary>
```

**Verification:**

```bash
grep "memory queue" Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs
# Expected: "Contains connection information for a memory queue"
```

---

## Task 2: Remove Unused Using Directives (ISSUE-006, ISSUE-010, ISSUE-011)

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs` (lines 2-6)
- Modify: `Source/DotNetWorkQueue/Queue/WorkerTerminate.cs` (line 20)
- Modify: `Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs` (line 21)

**Step 1: Remove 5 unused usings from RedisConnectionInfoTests.cs**

Remove these lines:
```csharp
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
```

Keep only:
```csharp
using System;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
```

**Step 2: Remove unused using from WorkerTerminate.cs**

Remove:
```csharp
using System.Threading;
```

**Step 3: Remove unused using from WaitForThreadToFinish.cs**

Remove:
```csharp
using System.Threading;
```

**Verification:**

```bash
grep -c "using System.Threading;" Source/DotNetWorkQueue/Queue/WorkerTerminate.cs
# Expected: 0
grep -c "using System.Threading;" Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs
# Expected: 0
grep -c "using System.Collections.Generic" Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs
# Expected: 0
```

---

## Task 3: Add QueueName Assertions to Tests (ISSUE-003, ISSUE-004)

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/SqlConnectionInformationTests.cs`
- Modify: `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/SqlConnectionInformationTests.cs`
- Modify: `Source/DotNetWorkQueue.Transport.SQLite.Tests/SQLiteConnectionInformationTests.cs`
- Modify: `Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs`
- Modify: `Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs`
- Modify: `Source/DotNetWorkQueue.Tests/Transport/Memory/ConnectionInformationTests.cs`

**Step 1: In each test file's `QueueName_Valid_Alphanumeric` method, add assertion after `Assert.IsNotNull(test)`**

Add this line after the `Assert.IsNotNull(test);` line in each file:
```csharp
            Assert.AreEqual("MyQueue123", test.QueueName);
```

**Verification:**

```bash
grep -rn "Assert.AreEqual.*MyQueue123.*QueueName" Source/ --include="*Tests*.cs"
# Expected: 6 matches (one per transport test file)
```

---

## Task 4: Fix Log Message Wording (ISSUE-009)

**Files:**
- Modify: `Source/DotNetWorkQueue/Queue/PrimaryWorker.cs`
- Modify: `Source/DotNetWorkQueue/Queue/Worker.cs`

**Step 1: In both files, change the Stop() log message**

Change:
```csharp
$"Stopping worker thread {WorkerName}"
```

To:
```csharp
$"Stopping worker {WorkerName}"
```

**Verification:**

```bash
grep -rn "worker thread" Source/DotNetWorkQueue/Queue/PrimaryWorker.cs Source/DotNetWorkQueue/Queue/Worker.cs
# Expected: 0 matches
```

---

## Task 5: Add Explicit Parentheses in MultiWorkerBase.Running (ISSUE-013)

**Files:**
- Modify: `Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs:62`

**Step 1: Add parentheses for clarity**

Change:
```csharp
public override bool Running => WorkerTask != null && !WorkerTask.IsCompleted || MessageProcessing != null && MessageProcessing.AsyncTaskCount > 0;
```

To:
```csharp
public override bool Running => (WorkerTask != null && !WorkerTask.IsCompleted) || (MessageProcessing != null && MessageProcessing.AsyncTaskCount > 0);
```

**Verification:**

```bash
grep "Running =>" Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs
# Expected: line with explicit parentheses grouping
```

---

## Task 6: Fix Timer.DisposeAsync (ISSUE-007)

**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Client/DashboardConsumerClient.cs:260`

**Step 1: Replace synchronous Timer.Dispose with async version**

Find the `DisposeAsync` method and change:
```csharp
_heartbeatTimer.Dispose();
```

To:
```csharp
await _heartbeatTimer.DisposeAsync().ConfigureAwait(false);
```

**Verification:**

```bash
grep -n "heartbeatTimer.Dispose()" Source/DotNetWorkQueue.Dashboard.Client/DashboardConsumerClient.cs
# Expected: 0 matches (synchronous Dispose should be gone from DisposeAsync method)
grep -n "heartbeatTimer.DisposeAsync()" Source/DotNetWorkQueue.Dashboard.Client/DashboardConsumerClient.cs
# Expected: 1 match
```

---

## Task 7: Fix Sync-Over-Async Test Pattern (ISSUE-008)

**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Client.Tests/DashboardConsumerClientTests.cs:845-846`

**Step 1: Replace sync-over-async assertion with async version**

Change:
```csharp
            Action act = () => httpClient.GetAsync("http://localhost:5000/test").GetAwaiter().GetResult();
            act.Should().Throw<ObjectDisposedException>();
```

To:
```csharp
            Func<Task> act = () => httpClient.GetAsync("http://localhost:5000/test");
            await act.Should().ThrowAsync<ObjectDisposedException>();
```

**Verification:**

```bash
grep -n "GetAwaiter().GetResult()" Source/DotNetWorkQueue.Dashboard.Client.Tests/DashboardConsumerClientTests.cs
# Expected: 0 matches
grep -n "ThrowAsync<ObjectDisposedException>" Source/DotNetWorkQueue.Dashboard.Client.Tests/DashboardConsumerClientTests.cs
# Expected: 1 match
```

---

## Task 8: Fix LiteDb Server Property (L-5)

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs:39`

**Step 1: Replace TODO placeholder with connection string value**

LiteDb connection strings use `FileName=path` format. Change:
```csharp
_server = "TODO; not known";
```

To:
```csharp
_server = queueConnection.Connection;
```

This returns the full connection string as the server identifier, matching how the Memory transport returns `ConnectionString` for its `Server` property. For LiteDb, the connection string contains the file path (e.g., `FileName=c:\temp\test.db;`), which is the meaningful server-equivalent value.

**Step 2: Update existing test assertion**

In `Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs`, the `LiteDbConnectionInformation_Test` method tests QueueName and ConnectionString but not Server. No existing test will break from this change since nothing asserts `Server == "TODO; not known"`.

**Verification:**

```bash
grep -rn "TODO; not known" Source/ --include="*.cs"
# Expected: 0 matches
dotnet test Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj --filter "FullyQualifiedName~LiteDbConnectionInformation" --no-build -v q
# Expected: All pass
```

---

## Task 9: Remove xUnit Pragma (M-4 partial)

**Files:**
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerAsync/Implementation/SimpleConsumerAsync.cs:105`

**Step 1: Remove the xUnit pragma**

Remove these lines:
```csharp
#pragma warning disable xUnit1013
```

And the corresponding restore (if present):
```csharp
#pragma warning restore xUnit1013
```

**Verification:**

```bash
grep -rn "xUnit1013" Source/ --include="*.cs"
# Expected: 0 matches
```

---

## Task 10: Remove Unused Fixture Variables (ISSUE-001) — If Still Present

**Files (verify first):**
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs`
- Modify: `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs`
- Modify: `Source/DotNetWorkQueue.Transport.SQLite.Tests/QueueCreatorTests.cs`

**Step 1: Verify issue still exists**

```bash
grep -n "var fixture" Source/DotNetWorkQueue.Transport.SqlServer.Tests/QueueCreatorTests.cs Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs Source/DotNetWorkQueue.Transport.SQLite.Tests/QueueCreatorTests.cs
```

If no unused fixture lines found, mark ISSUE-001 as already resolved and skip.

**Step 2: If fixture lines exist, check each method**

For each method that has `var fixture = new Fixture()...` — check if `fixture` is used later in that method. If not used, remove the line.

**Verification:**

```bash
dotnet build Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj -c Release 2>&1 | grep -i "CS0219\|warning"
# Expected: No CS0219 (unused variable) warnings for fixture
```

---

## Final Verification

After all tasks, run the affected unit test projects:

```bash
dotnet test Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj -v q
dotnet test Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj -v q
dotnet test Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj -v q
dotnet test Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj -v q
dotnet test Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj -v q
dotnet test Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj -v q
dotnet test Source/DotNetWorkQueue.Dashboard.Client.Tests/DotNetWorkQueue.Dashboard.Client.Tests.csproj -v q
```

All should pass with 0 failures.

**Commit:**

```bash
git add -A Source/
git commit -m "fix: resolve 14 code-level concerns and issues (ISSUE-001 through ISSUE-013, L-5, M-4)"
```
