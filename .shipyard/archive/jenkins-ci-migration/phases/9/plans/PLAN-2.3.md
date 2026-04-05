# Plan 2.3: TODO/HACK Audit + Integration Test Binder Fix (M-3, N-3)

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Replace all 4 TODO/HACK comments in production code with descriptive NOTE comments, and fix the integration test serializer to use `DenyListSerializationBinder`.
**Architecture:** The 4 TODO/HACK comments are in production source files across 3 projects. Each is replaced with a NOTE comment that explains the design decision or defers the work with a reference. The binder fix adds `SerializationBinder = new DenyListSerializationBinder()` to the test helper class in `Helpers.cs` to match the production serializer's security pattern.
**Tech Stack:** C# comment changes, Newtonsoft.Json SerializationBinder

## Dependencies
- PLAN-1.1 (Wave 1 -- CPM must be complete so `.csproj` files are stable)

## Tasks

### Task 1: Replace TODO/HACK comments with NOTE comments in production code
**Files:**
- Modify: `Source/DotNetWorkQueue/Factory/InterceptorFactory.cs` (line 52)
- Modify: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/QueryHandler/ReceiveMessage.cs` (line 175)
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/QueryHandler/CreateDequeueStatement.cs` (line 237)
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/Message/ReceiveMessage.cs` (line 100)

**Steps:**

1. **InterceptorFactory.cs line 52** -- Replace:
```csharp
            //HACK for now - it's not clear to me if simple injector supports this pattern
```
With:
```csharp
            //NOTE: SimpleInjector decorator pattern limitation -- manual wrapping is required here because
            //the container does not support decorating an open-generic service resolved by type at runtime.
```

2. **ReceiveMessage.cs (PostgreSQL) line 175** -- Replace:
```csharp
            { //TODO - cache based on route
```
With:
```csharp
            { //NOTE: Route-based caching deferred; see CONCERNS.md L-4
```

3. **CreateDequeueStatement.cs (SqlServer) line 237** -- Replace:
```csharp
            { //TODO - cache based on route
```
With:
```csharp
            { //NOTE: Route-based caching deferred; see CONCERNS.md L-4
```

4. **ReceiveMessage.cs (SqlServer) line 100** -- Replace:
```csharp
            //TODO - we could consider using a task to update the status table
```
With:
```csharp
            //NOTE: Synchronous status update is intentional; async would add complexity without measurable benefit at current scale.
```

**Verify:**
```bash
# Should return 0 hits in production code (excluding test files)
grep -rn "TODO\|HACK" Source/ --include="*.cs" | grep -v "Tests" | grep -v "IntegrationTests" | grep -v "ReSharper" | wc -l
```

### Task 2: Fix integration test serializer to use DenyListSerializationBinder
**Files:**
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/Helpers.cs` (line 110-113)

**Steps:**

1. Add the using directive at the top of `Helpers.cs`:
```csharp
using DotNetWorkQueue.Serialization;
```

2. Modify the `_serializerSettings` initialization in the `SerializerThatWillCrashOnDeSerialization` class (around line 110-113). Change from:
```csharp
    private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All
    };
```
To:
```csharp
    private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All,
        SerializationBinder = new DenyListSerializationBinder()
    };
```

The `DenyListSerializationBinder` class:
- Lives in namespace `DotNetWorkQueue.Serialization`
- Has a parameterless constructor that initializes with default denied types
- Is already referenced by the `DotNetWorkQueue.IntegrationTests.Shared` project (it has a `ProjectReference` to `DotNetWorkQueue.csproj`)

**Verify:**
```bash
grep -q "DenyListSerializationBinder" Source/DotNetWorkQueue.IntegrationTests.Shared/Helpers.cs && echo "Binder fix: PASS"
dotnet build "Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" -c Debug
```

### Task 3: Validate all changes compile and tests pass
**Files:** None (verification only)

**Steps:**

1. Build the affected projects to confirm no compilation errors:
```bash
dotnet build "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" -c Debug
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Debug
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Debug
dotnet build "Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" -c Debug
```

2. Run unit tests for the core project (InterceptorFactory is tested here):
```bash
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"
```

3. Run in-memory integration tests (uses the modified Helpers.cs serializer):
```bash
dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj"
```

**Verify:**
```bash
dotnet build "Source/DotNetWorkQueue.sln" -c Debug && echo "BUILD: PASS"
```

## Verification

```bash
# 1. Zero TODO/HACK in production code
count=$(grep -rn "TODO\|HACK" Source/ --include="*.cs" | grep -v "Tests" | grep -v "IntegrationTests" | grep -v "ReSharper" | wc -l)
[ "$count" -eq 0 ] && echo "TODO/HACK audit: PASS ($count)" || echo "FAIL: $count TODO/HACK remain"

# 2. Binder fix applied
grep -q "DenyListSerializationBinder" Source/DotNetWorkQueue.IntegrationTests.Shared/Helpers.cs && echo "Binder fix: PASS"

# 3. NOTE comments exist
grep -q "NOTE: SimpleInjector decorator pattern" Source/DotNetWorkQueue/Factory/InterceptorFactory.cs && echo "InterceptorFactory NOTE: PASS"
grep -q "NOTE: Route-based caching deferred" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/QueryHandler/ReceiveMessage.cs && echo "PostgreSQL NOTE: PASS"
grep -q "NOTE: Route-based caching deferred" Source/DotNetWorkQueue.Transport.SqlServer/Basic/QueryHandler/CreateDequeueStatement.cs && echo "SqlServer dequeue NOTE: PASS"
grep -q "NOTE: Synchronous status update" Source/DotNetWorkQueue.Transport.SqlServer/Basic/Message/ReceiveMessage.cs && echo "SqlServer receive NOTE: PASS"

# 4. Build succeeds
dotnet build "Source/DotNetWorkQueue.sln" -c Debug

# 5. Unit tests pass
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"
```
