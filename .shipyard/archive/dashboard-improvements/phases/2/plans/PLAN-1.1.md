# Remote Transport Test Retry

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Add assembly-level `[RetryOnFailure(MaxRetries = 1)]` to all 6 remote transport integration test projects to retry flaky network-dependent tests once on failure.

**Architecture:** MSTest 4.1.0 supports `[assembly: RetryOnFailure]` in AssemblyInfo.cs. One new file per project, no existing AssemblyInfo.cs files to conflict with. The attribute applies to all test methods in the assembly.

**Tech Stack:** C# / MSTest 4.1.0 / .NET 10.0 + .NET 4.8

---

<task id="1" name="Add RetryOnFailure to all 6 remote transport projects">
  <description>Create AssemblyInfo.cs in each of the 6 remote transport integration test projects with the assembly-level RetryOnFailure attribute.</description>
  <files>
    <create>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/AssemblyInfo.cs</create>
    <create>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/AssemblyInfo.cs</create>
    <create>Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/AssemblyInfo.cs</create>
    <create>Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/AssemblyInfo.cs</create>
    <create>Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/AssemblyInfo.cs</create>
    <create>Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/AssemblyInfo.cs</create>
  </files>
  <steps>
    <step>Create AssemblyInfo.cs in each project with the RetryOnFailure attribute</step>
    <step>Build all 6 projects to verify the attribute compiles on both targets</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet build "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.IntegrationTests.csproj" && dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.IntegrationTests.csproj" && dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" && dotnet build "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj" && dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj" && dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj"</command>
    <expected>All 6 builds succeed with 0 errors</expected>
  </verification>
</task>

### Task 1 Details

**Each AssemblyInfo.cs file is identical:**

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: RetryOnFailure(MaxRetries = 1)]
```

**Create this file in each of these 6 directories:**

1. `Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/AssemblyInfo.cs`
2. `Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/AssemblyInfo.cs`
3. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/AssemblyInfo.cs`
4. `Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/AssemblyInfo.cs`
5. `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/AssemblyInfo.cs`
6. `Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/AssemblyInfo.cs`

**Note:** Modern .NET SDK-style projects auto-include all .cs files — no .csproj edits needed.

---

### Final Verification

```bash
grep -r "RetryOnFailure" Source/ --include="*.cs" | wc -l
```
Expected: `6`
