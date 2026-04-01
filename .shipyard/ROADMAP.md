# Roadmap: Integration Test Cleanup

## Phase 1: Redis ConnectionInfoTypes Removal
- **Scope:** Delete the `ConnectionInfoTypes` enum from `ConnectionString.cs`, convert `ConnectionInfo` from instance class to static class matching the SqlServer/PostgreSQL pattern, and update all 34 Redis test files to remove the `ConnectionInfoTypes.Linux` DataRow parameter, the `type` method parameter, and the `new ConnectionInfo(type).ConnectionString` call pattern.
- **Dependencies:** None
- **Risk:** Medium -- 34 files touched across 2 projects; mechanical but high file count means easy to miss one. Compiler will catch any misses.
- **Success criteria:**
  - `ConnectionInfoTypes` enum no longer exists anywhere in the codebase
  - `ConnectionInfo` in `Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/ConnectionString.cs` is a `public static class` with a static `ConnectionString` property
  - All 34 Redis test files compile without `ConnectionInfoTypes` references
  - `dotnet build "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.IntegrationTests.csproj"` succeeds
  - `dotnet build "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj"` succeeds

## Phase 2: Remote Transport Test Retry
- **Scope:** Add `[assembly: RetryOnFailure(MaxRetries = 1)]` to the existing `AssemblyInfo.cs` in each of the 6 remote transport integration test projects: Redis (2), SqlServer (2), PostgreSQL (2).
- **Dependencies:** None (independent of Phase 1; can execute in parallel)
- **Risk:** Low -- 6 one-line additions to existing files. Only risk is MSTest `RetryOnFailure` attribute availability on net48, which needs verification during planning.
- **Success criteria:**
  - All 6 `AssemblyInfo.cs` files contain `[assembly: RetryOnFailure(MaxRetries = 1)]`
  - All 6 projects build successfully on both net10.0 and net48 targets
  - `dotnet build "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.IntegrationTests.csproj"` succeeds
  - `dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.IntegrationTests.csproj"` succeeds
  - `dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj"` succeeds
  - `dotnet build "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj"` succeeds
  - `dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj"` succeeds
  - `dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj"` succeeds
