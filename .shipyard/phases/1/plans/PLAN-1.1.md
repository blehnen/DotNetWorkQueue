# Redis ConnectionInfoTypes Removal

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Remove the dead `ConnectionInfoTypes` enum and convert Redis `ConnectionInfo` to a static class matching SqlServer/PostgreSQL.

**Architecture:** Single shared `ConnectionString.cs` defines `ConnectionInfo` class and `ConnectionInfoTypes` enum, referenced by both `Redis.IntegrationTests` (18 test files) and `Redis.Linq.Integration.Tests` (15 test files via ProjectReference). Converting to a static class with a static property eliminates all constructor/enum plumbing.

**Tech Stack:** C# / MSTest 4.1.0 / .NET 10.0 + .NET 4.8

---

<task id="1" name="Convert ConnectionInfo to static class">
  <description>Rewrite ConnectionString.cs: delete the ConnectionInfoTypes enum, convert ConnectionInfo from instance to static class with static ConnectionString property. This is the shared foundation — both Redis test projects reference this file.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/ConnectionString.cs</modify>
  </files>
  <steps>
    <step>Replace the entire file with a static class pattern matching SqlServer/PostgreSQL</step>
    <step>Build both Redis projects to see all compile errors (these are the 33 files that need updating)</step>
  </steps>
  <verification>
    <command>dotnet build "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.IntegrationTests.csproj" 2>&1 | head -5</command>
    <expected>Build errors expected — test files still reference ConnectionInfoTypes. This confirms the enum is fully removed.</expected>
  </verification>
</task>

### Task 1 Details

**Rewrite `Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/ConnectionString.cs` to:**

```csharp
using System;
using System.IO;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests
{
    public static class ConnectionInfo
    {
        private static string _connectionString;

        public static string ConnectionString
        {
            get
            {
                if (!string.IsNullOrEmpty(_connectionString))
                    return _connectionString;

                var connectionString = File.ReadAllText("connectionstring.txt");
                _connectionString = connectionString.Trim();

                if (string.IsNullOrEmpty(_connectionString))
                {
                    throw new NullReferenceException("connectionstring.txt is missing or contains no data");
                }

                return _connectionString;
            }
        }
    }
}
```

Key changes from the original:
- `public class ConnectionInfo` → `public static class ConnectionInfo`
- Deleted constructor `public ConnectionInfo(ConnectionInfoTypes type)`
- `ConnectionString` property is now `public static` (was instance with static backing field)
- Deleted `public enum ConnectionInfoTypes { Linux = 0 }`
- Kept `.Trim()` from original (SqlServer doesn't trim, but Redis did — preserve behavior)

---

<task id="2" name="Update Redis.IntegrationTests test files (18 files)">
  <description>Remove ConnectionInfoTypes.Linux from DataRow attributes, remove the type parameter from method signatures, and replace new ConnectionInfo(type).ConnectionString with ConnectionInfo.ConnectionString in all 18 test files.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Admin/SimpleConsumer.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Consumer/SimpleConsumer.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Consumer/ConsumerCancelWork.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Consumer/ConsumerErrorTable.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Consumer/ConsumerExpiredMessage.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Consumer/ConsumerHeartbeat.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Consumer/ConsumerPoisonMessage.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Consumer/ConsumerRollBack.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/ConsumerAsync/SimpleConsumerAsync.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/ConsumerAsync/ConsumerAsyncErrorTable.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/ConsumerAsync/ConsumerAsyncPoisonMessage.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/ConsumerAsync/ConsumerAsyncRollBack.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/ConsumerAsync/MultiConsumerAsync.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Producer/SimpleProducer.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Producer/SimpleProducerAsync.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Route/RouteTests.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/Route/RouteMultiTests.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/History/SimpleHistoryTests.cs</modify>
  </files>
  <steps>
    <step>For each file, apply three mechanical transformations (see patterns below)</step>
    <step>Build the project to verify zero compile errors</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet build "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.IntegrationTests.csproj"</command>
    <expected>Build succeeded. 0 Warning(s). 0 Error(s).</expected>
  </verification>
</task>

### Task 2 Details

Three mechanical transformations per file:

**Transform A — DataRow attributes: remove the `ConnectionInfoTypes.Linux` argument**

Before:
```csharp
[DataRow(500, 0, 240, 5, ConnectionInfoTypes.Linux),
 DataRow(50, 5, 200, 10, ConnectionInfoTypes.Linux)]
```

After:
```csharp
[DataRow(500, 0, 240, 5),
 DataRow(50, 5, 200, 10)]
```

**Transform B — Method signature: remove the `ConnectionInfoTypes type` parameter**

Before:
```csharp
public void Run(int messageCount, int runtime, int timeOut, int workerCount, ConnectionInfoTypes type)
```

After:
```csharp
public void Run(int messageCount, int runtime, int timeOut, int workerCount)
```

**Transform C — Connection string access: replace instantiation with static access**

Before:
```csharp
var connectionString = new ConnectionInfo(type).ConnectionString;
```

After:
```csharp
var connectionString = ConnectionInfo.ConnectionString;
```

**Important:** Some files may have slightly different parameter lists (e.g., `ConsumerErrorTable` has `messageCount, timeOut, workerCount` without `runtime`). The `ConnectionInfoTypes type` parameter is always the **last** parameter before any other enum like `LinqMethodTypes`. Remove only the `ConnectionInfoTypes type` parameter and its corresponding DataRow value.

---

<task id="3" name="Update Redis.Linq.Integration.Tests test files (15 files)">
  <description>Same three transformations as Task 2, but for the 15 Linq test files. These files have an additional LinqMethodTypes parameter AFTER ConnectionInfoTypes — be careful to remove only the ConnectionInfoTypes parameter and its DataRow value.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/SimpleMethodConsumer.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodCancelWork.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodErrorTable.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodExpiredMessage.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodHeartbeat.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodPoisonMessage.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodRollBack.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethodAsync/SimpleConsumerMethodAsync.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethodAsync/ConsumerMethodAsyncErrorTable.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethodAsync/ConsumerMethodAsyncPoisonMessage.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethodAsync/ConsumerMethodAsyncRollBack.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ProducerMethod/SimpleMethodProducer.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ProducerMethod/SimpleMethodProducerAsync.cs</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs</modify>
  </files>
  <steps>
    <step>For each file, apply the same three transformations — but note the LinqMethodTypes parameter stays</step>
    <step>Build the project to verify zero compile errors</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet build "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj"</command>
    <expected>Build succeeded. 0 Warning(s). 0 Error(s).</expected>
  </verification>
</task>

### Task 3 Details

**Linq files have an extra parameter — remove only ConnectionInfoTypes, keep LinqMethodTypes.**

Before (typical Linq test):
```csharp
[DataRow(100, 0, 240, 5, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
 DataRow(50, 5, 200, 10, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic)]
public void Run(int messageCount, int runtime, int timeOut, int workerCount, ConnectionInfoTypes type, LinqMethodTypes linqMethodTypes)
```

After:
```csharp
[DataRow(100, 0, 240, 5, LinqMethodTypes.Dynamic),
 DataRow(50, 5, 200, 10, LinqMethodTypes.Dynamic)]
public void Run(int messageCount, int runtime, int timeOut, int workerCount, LinqMethodTypes linqMethodTypes)
```

Connection string access is the same Transform C as Task 2.

**JobSchedulerTests.cs** may have a different pattern — check its signature before applying. It may not have the standard Run method.

---

### Final Verification

After all three tasks, confirm no references remain:

```bash
grep -r "ConnectionInfoTypes" Source/ --include="*.cs" | wc -l
```
Expected: `0`

```bash
dotnet build "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.IntegrationTests.csproj" && dotnet build "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj"
```
Expected: Both succeed with 0 errors.
