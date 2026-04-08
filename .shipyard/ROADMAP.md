# Roadmap: Drop net48/netstandard2.0 (issue #101)

## Overview

Remove net48 and netstandard2.0 targets from the entire solution, delete all `#if NETFULL` / `#if NETSTANDARD2_0` conditional compilation, remove vendored JpLabs.DynamicCode, clean up Schyntax net48/netstandard2.0 DLLs, update CI, update README. Version 0.9.3 breaking change.

**Total scope:** ~30 csproj files, ~97 .cs files, 1 CI workflow, 1 README, 3 Lib directory deletions.

## Dependency Graph

```
Phase 1  (Core + Lib)
   |
   v
Phase 2  (Shared test infra + unit tests)        Phase 3a-3f  (Linq integration tests, by transport)
   |                                                  |
   +--------------------------------------------------+
   |
   v
Phase 4  (CI + Docs + Version bump)
```

Phase 1 is the foundation -- everything depends on it.
Phase 2 depends on Phase 1 (shared test helpers reference core interfaces).
Phases 3a-3f depend on Phase 2 (each Linq integration test project uses shared test helpers) and can execute in parallel with each other.
Phase 4 depends on all prior phases completing (CI and docs reflect the final state).

---

## Phase 1: Core Library, Transport Libraries, and Vendored DLL Cleanup

**Risk:** HIGH -- every project in the solution depends on these. Errors here break everything downstream.
**Scope:** ~15% of total files, but highest impact.
**Strategy:** Fail fast. If the core does not build after this phase, nothing else matters.

### What Changes

1. **DotNetWorkQueue.csproj** -- Remove net48/netstandard2.0 from TargetFrameworks. Remove all net48/netstandard2.0 PropertyGroup conditions and DefineConstants. Remove net48/netstandard2.0 Schyntax ItemGroups. Remove JpLabs.DynamicCode reference and its `_PackageFiles` entry. Remove net48/netstandard2.0 `_PackageFiles` entries for Schyntax. Update Description text.
2. **10 core .cs files** -- Remove all `#if NETFULL` blocks (dynamic LINQ method overloads, SoapFormatter, GetObjectData). Remove `#if NETSTANDARD2_0` / `#if !NETFULL` guards keeping only the modern code path. Files: `ASendJobToQueue.cs`, `ISendJobToQueue.cs`, `IProducerMethodQueue.cs`, `IProducerMethodJobQueue.cs`, `IJobScheduler.cs`, `CompileException.cs`, `ProducerMethodQueue.cs`, `ProducerMethodJobQueue.cs`, `ScheduledJob.cs`, `ProducerMethodJobQueueDecorator.cs`.
3. **8 transport library csproj files** (csproj-only, no .cs changes) -- Remove net48/netstandard2.0 from TargetFrameworks and delete their PropertyGroup/ItemGroup conditions. Projects: SqlServer, PostgreSQL, SQLite, Redis, LiteDB, Memory, RelationalDatabase, Shared.
4. **Delete vendored files** -- `Lib/JpLabs.DynamicCode/` (entire directory), `Lib/Schyntax/net48/`, `Lib/Schyntax/netstandard2.0/`.

### Files Touched

- `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`
- `Source/DotNetWorkQueue/ASendJobToQueue.cs`
- `Source/DotNetWorkQueue/ISendJobToQueue.cs`
- `Source/DotNetWorkQueue/IProducerMethodQueue.cs`
- `Source/DotNetWorkQueue/IProducerMethodJobQueue.cs`
- `Source/DotNetWorkQueue/IJobScheduler.cs`
- `Source/DotNetWorkQueue/Exceptions/CompileException.cs`
- `Source/DotNetWorkQueue/Queue/ProducerMethodQueue.cs`
- `Source/DotNetWorkQueue/Queue/ProducerMethodJobQueue.cs`
- `Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs`
- `Source/DotNetWorkQueue/Trace/Decorator/ProducerMethodJobQueueDecorator.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj`
- `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj`
- `Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj`
- `Source/DotNetWorkQueue.Transport.LiteDB/DotNetWorkQueue.Transport.LiteDb.csproj`
- `Source/DotNetWorkQueue.Transport.Memory/DotNetWorkQueue.Transport.Memory.csproj`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj`
- `Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj`
- `Lib/JpLabs.DynamicCode/` (deleted)
- `Lib/Schyntax/net48/` (deleted)
- `Lib/Schyntax/netstandard2.0/` (deleted)

### Success Criteria

1. `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug` succeeds with 0 errors
2. `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release` succeeds with 0 errors, 0 warnings
3. `grep -r "NETFULL\|NETSTANDARD2_0" Source/DotNetWorkQueue/ --include="*.cs" --include="*.csproj"` returns 0 matches
4. `grep -r "net48\|netstandard2.0" Source/DotNetWorkQueue/ --include="*.csproj"` returns 0 matches
5. All 8 transport library csproj files have only `net10.0;net8.0` in TargetFrameworks
6. `Lib/JpLabs.DynamicCode/` does not exist
7. `Lib/Schyntax/net48/` and `Lib/Schyntax/netstandard2.0/` do not exist

---

## Phase 2: Shared Test Infrastructure and Unit Tests

**Risk:** MEDIUM -- the shared test helpers define the dynamic LINQ test patterns used by every Linq integration test. Getting the conditional removal wrong here propagates downstream.
**Scope:** ~20% of total files.
**Depends on:** Phase 1.

### What Changes

1. **DotNetWorkQueue.IntegrationTests.Shared** -- 19 .cs files: remove `#if NETFULL` blocks (dynamic LINQ test cases, `LinqMethodTypes.Dynamic`), keep modern code paths. 1 csproj: remove net48 from TargetFrameworks and conditional PropertyGroup/ItemGroup blocks.
2. **DotNetWorkQueue.Tests** -- 1 .cs file (`CompileExceptionTests.cs`): remove `#if NETFULL` guard. 1 csproj: remove net48 and conditional blocks.
3. **8 unit test csproj files** (csproj-only, no .cs changes) -- Remove net48 from TargetFrameworks and conditional PropertyGroup/ItemGroup blocks. Projects: SqlServer.Tests, PostgreSQL.Tests, SQLite.Tests, Redis.Tests, LiteDb.Tests, Memory.Tests, RelationalDatabase.Tests, AppMetrics.Tests (if applicable).
4. **6 base integration test csproj files** (csproj-only) -- Remove net48 from TargetFrameworks and conditional blocks. Projects: SqlServer.IntegrationTests, PostgreSQL.Integration.Tests, SQLite.Integration.Tests, Redis.IntegrationTests, LiteDB.IntegrationTests, Memory.Integration.Tests.

### Files Touched

- `Source/DotNetWorkQueue.IntegrationTests.Shared/` -- 19 .cs files + 1 csproj
- `Source/DotNetWorkQueue.Tests/Exceptions/CompileExceptionTests.cs`
- `Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj`
- 8 unit test csproj files (listed above)
- 6 base integration test csproj files (listed above)

### Success Criteria

1. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds with 0 errors
2. `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"` -- all tests pass
3. `grep -r "NETFULL\|NETSTANDARD2_0" Source/DotNetWorkQueue.IntegrationTests.Shared/ Source/DotNetWorkQueue.Tests/ --include="*.cs" --include="*.csproj"` returns 0 matches
4. `grep -r "net48" Source/DotNetWorkQueue.Tests/ Source/DotNetWorkQueue.IntegrationTests.Shared/ --include="*.csproj"` returns 0 matches
5. All unit test and base integration test csproj files have only `net10.0` in TargetFrameworks

---

## Phase 3a: SqlServer Linq Integration Tests -- COMPLETE

**Risk:** LOW -- mechanical removal of `#if NETFULL` blocks. Pattern identical across all transport Linq tests.
**Scope:** ~8% of total files.
**Depends on:** Phase 2.
**Parallel with:** Phases 3b, 3c, 3d, 3e, 3f.

### What Changes

Remove `#if NETFULL` blocks from 13 .cs files (dynamic LINQ test methods using `LinqMethodTypes.Dynamic`). Update 1 csproj: remove net48 from TargetFrameworks and conditional blocks.

### Files Touched

- `Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/` -- 13 .cs files + 1 csproj

### Success Criteria

1. `grep -r "NETFULL" Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/ --include="*.cs"` returns 0 matches
2. `grep -r "net48" Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/ --include="*.csproj"` returns 0 matches
3. `dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj" -c Debug` succeeds

---

## Phase 3b: PostgreSQL Linq Integration Tests -- COMPLETE

**Risk:** LOW -- identical pattern to Phase 3a.
**Scope:** ~8% of total files.
**Depends on:** Phase 2.
**Parallel with:** Phases 3a, 3c, 3d, 3e, 3f.

### What Changes

Remove `#if NETFULL` blocks from 13 .cs files. Update 1 csproj.

### Files Touched

- `Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/` -- 13 .cs files + 1 csproj

### Success Criteria

1. `grep -r "NETFULL" Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/ --include="*.cs"` returns 0 matches
2. `grep -r "net48" Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/ --include="*.csproj"` returns 0 matches
3. `dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj" -c Debug` succeeds

---

## Phase 3c: SQLite Linq Integration Tests -- COMPLETE

**Risk:** LOW -- identical pattern.
**Scope:** ~8% of total files.
**Depends on:** Phase 2.
**Parallel with:** Phases 3a, 3b, 3d, 3e, 3f.

### What Changes

Remove `#if NETFULL` blocks from 13 .cs files. Update 1 csproj.

### Files Touched

- `Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/` -- 13 .cs files + 1 csproj

### Success Criteria

1. `grep -r "NETFULL" Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/ --include="*.cs"` returns 0 matches
2. `grep -r "net48" Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/ --include="*.csproj"` returns 0 matches
3. `dotnet build "Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj" -c Debug` succeeds

---

## Phase 3d: Redis Linq Integration Tests -- COMPLETE

**Risk:** LOW -- identical pattern.
**Scope:** ~8% of total files.
**Depends on:** Phase 2.
**Parallel with:** Phases 3a, 3b, 3c, 3e, 3f.

### What Changes

Remove `#if NETFULL` blocks from 14 .cs files. Update 1 csproj.

### Files Touched

- `Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/` -- 14 .cs files + 1 csproj

### Success Criteria

1. `grep -r "NETFULL" Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ --include="*.cs"` returns 0 matches
2. `grep -r "net48" Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ --include="*.csproj"` returns 0 matches
3. `dotnet build "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj" -c Debug` succeeds

---

## Phase 3e: LiteDB Linq Integration Tests -- COMPLETE

**Risk:** LOW -- identical pattern.
**Scope:** ~8% of total files.
**Depends on:** Phase 2.
**Parallel with:** Phases 3a, 3b, 3c, 3d, 3f.

### What Changes

Remove `#if NETFULL` blocks from 13 .cs files. Update 1 csproj.

### Files Touched

- `Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/` -- 13 .cs files + 1 csproj

### Success Criteria

1. `grep -r "NETFULL" Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/ --include="*.cs"` returns 0 matches
2. `grep -r "net48" Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/ --include="*.csproj"` returns 0 matches
3. `dotnet build "Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj" -c Debug` succeeds

---

## Phase 3f: Memory Linq Integration Tests -- COMPLETE

**Risk:** LOW -- identical pattern.
**Scope:** ~7% of total files.
**Depends on:** Phase 2.
**Parallel with:** Phases 3a, 3b, 3c, 3d, 3e.

### What Changes

Remove `#if NETFULL` blocks from 10 .cs files. Update 1 csproj.

### Files Touched

- `Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/` -- 10 .cs files + 1 csproj

### Success Criteria

1. `grep -r "NETFULL" Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/ --include="*.cs"` returns 0 matches
2. `grep -r "net48" Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/ --include="*.csproj"` returns 0 matches
3. `dotnet build "Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" -c Debug` succeeds

---

## Phase 4: CI, Documentation, and Version Bump

**Risk:** LOW -- cosmetic and configuration changes. No functional code affected.
**Scope:** ~5% of total effort.
**Depends on:** Phases 1, 2, 3a-3f (all prior phases complete).

### What Changes

1. **GitHub Actions CI** (`.github/workflows/ci.yml`) -- Remove all `-f net48` flags from test steps. Remove `windows-latest` runner (switch to `ubuntu-latest` since net48 no longer needed). Remove `dotnet-version: 10.0.100` explicit version if standard `10.0.x` wildcard works. Update comments to reflect net48 removal.
2. **README.md** -- Remove "Targets .NET 4.8" and ".NET Standard 2.0" from description. Remove "No support for dynamic LINQ statements" limitation section. Remove dynamic LINQ code examples and casting notes. Remove JpLabs.DynamicCode from custom libraries list. Remove application domain sandbox security note (only relevant to dynamic LINQ).
3. **CLAUDE.md** -- Update project overview to reflect net10.0/net8.0 only. Remove net48 test commands. Update conventions section.
4. **Version bump** -- Update `<Version>` to `0.9.3` in `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`. Update Description to remove net48/netstandard2.0 references.

### Files Touched

- `.github/workflows/ci.yml`
- `README.md`
- `CLAUDE.md`
- `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (version only -- TFM already done in Phase 1)

### Success Criteria

1. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` -- 0 errors
2. `dotnet build "Source/DotNetWorkQueue.sln" -c Release` -- 0 errors, 0 warnings
3. `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"` -- all tests pass
4. `grep -r "NETFULL\|NETSTANDARD2_0" Source/ --include="*.cs" --include="*.csproj"` -- 0 matches
5. `grep -r "net48\|netstandard2.0" Source/ --include="*.csproj"` -- 0 matches
6. `grep "JpLabs\|DynamicCode" README.md` -- 0 matches
7. `grep "dynamic LINQ" README.md` -- 0 matches
8. Version in DotNetWorkQueue.csproj is `0.9.3`
9. GitHub Actions workflow does not reference `net48` or `windows-latest`

---

## Risk Summary

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Core csproj changes break all downstream builds | Medium | Critical | Phase 1 builds with `DotNetWorkQueueNoTests.sln` first (no test project deps) |
| Removing wrong branch of `#if` conditional | Low | High | Each `#if NETFULL` block: delete the NETFULL branch, keep the `#else` branch. Each `#if !NETFULL`: keep the body, remove the guard. Verify with grep for zero remaining occurrences |
| Missing a csproj file | Low | Medium | Full grep enumeration done -- 30 csproj files identified |
| Integration tests fail due to removed dynamic LINQ test cases | Low | Low | Tests that only existed for dynamic LINQ are expected to be removed entirely. Remaining tests cover the same queue operations via standard LINQ |
| `CompileException.cs` removal breaks compilation | Low | Medium | Check if `CompileException` is referenced outside `#if NETFULL` guards. If so, keep the class but remove only the NETFULL-specific members |

## Execution Order

| Wave | Phases | Can Parallelize? |
|------|--------|-----------------|
| 1 | Phase 1 | No -- foundation |
| 2 | Phase 2 | No -- depends on Phase 1 |
| 3 | Phases 3a, 3b, 3c, 3d, 3e, 3f | Yes -- all 6 are independent |
| 4 | Phase 4 | No -- depends on all prior |

**Estimated plans per phase:**
- Phase 1: 1 plan (3 tasks)
- Phase 2: 1 plan (3 tasks)
- Phases 3a-3f: 1 plan each (2-3 tasks each), parallelizable
- Phase 4: 1 plan (3 tasks)
- **Total: 10 plans**
