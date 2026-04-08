# Phase 3 Plan Critique -- Feasibility Stress Test
**Date:** 2026-04-07

## Per-Plan/Task Findings

### PLAN-1.1: SqlServer, PostgreSQL, SQLite

#### Task 1 -- SqlServer Linq Integration Tests
- **Directory exists:** YES -- `Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/`
- **File count accuracy:** EXACT -- Plan claims 18 .cs files, actual grep found 18 files with `#if NETFULL`
- **Subdirectory breakdown:** EXACT -- ConsumerMethod(8), ConsumerMethodAsync(4), ProducerMethod(5), JobScheduler(1)
- **csproj exists:** YES -- `DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj`
- **csproj contains net48:** YES -- `<TargetFrameworks>net10.0;net48</TargetFrameworks>` at line 4, plus 2 conditional PropertyGroups and 1 conditional ItemGroup
- **Verify command:** Syntactically correct. Uses `grep -v "/obj/"` to filter build artifacts.

#### Task 2 -- PostgreSQL Linq Integration Tests
- **Directory exists:** YES -- `Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/`
- **File count accuracy:** EXACT -- Plan claims 18 .cs files, actual grep found 18
- **Subdirectory breakdown:** EXACT -- ConsumerMethod(8), ConsumerMethodAsync(4), ProducerMethod(5), JobScheduler(1)
- **csproj exists:** YES -- `DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj`
- **csproj contains net48:** YES -- TargetFrameworks line 4, 2 conditional PropertyGroups, 1 conditional ItemGroup
- **Verify command:** Syntactically correct.

#### Task 3 -- SQLite Linq Integration Tests
- **Directory exists:** YES -- `Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/`
- **File count accuracy:** EXACT -- Plan claims 17 .cs files, actual grep found 17
- **Subdirectory breakdown:** EXACT -- ConsumerMethod(8), ConsumerMethodAsync(4), ProducerMethod(5), JobScheduler(0)
- **csproj exists:** YES -- `DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj`
- **csproj contains net48:** YES -- TargetFrameworks line 4, 2 conditional PropertyGroups, 1 conditional ItemGroup
- **Verify command:** Syntactically correct.

### PLAN-1.2: Redis, LiteDB, Memory

#### Task 1 -- Redis Linq Integration Tests
- **Directory exists:** YES -- `Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/`
- **File count accuracy:** EXACT -- Plan claims 15 .cs files, actual grep found 15
- **Subdirectory breakdown:** EXACT -- ConsumerMethod(8), ConsumerMethodAsync(4), ProducerMethod(2), JobScheduler(1)
- **csproj exists:** YES -- `DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj`
- **csproj contains net48:** YES -- TargetFrameworks line 4, 2 conditional PropertyGroups, NO conditional ItemGroup (plan correctly notes this)
- **Verify command:** Syntactically correct.

#### Task 2 -- LiteDB Linq Integration Tests
- **Directory exists:** YES -- `Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/`
- **File count accuracy:** EXACT -- Plan claims 17 .cs files, actual grep found 17
- **Subdirectory breakdown:** EXACT -- ConsumerMethod(8), ConsumerMethodAsync(4), ProducerMethod(5), JobScheduler(0)
- **csproj exists:** YES -- csproj filename uses lowercase "b": `DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj` (plan correctly notes this casing)
- **csproj contains net48:** YES -- TargetFrameworks line 4, 2 conditional PropertyGroups, 1 conditional ItemGroup
- **Verify command:** Syntactically correct.

#### Task 3 -- Memory Linq Integration Tests
- **Directory exists:** YES -- `Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/`
- **File count accuracy:** EXACT -- Plan claims 12 .cs files, actual grep found 12
- **Subdirectory breakdown:** EXACT -- ConsumerMethod(2), ConsumerMethodAsync(1), ProducerMethod(7), JobScheduler(2)
- **csproj exists:** YES -- `DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj`
- **csproj contains net48:** YES -- TargetFrameworks line 4, 2 conditional PropertyGroups, 1 conditional ItemGroup
- **Verify command:** Syntactically correct.

## Cross-Cutting Checks

| Check | Result |
|-------|--------|
| No shared files between 6 project directories | PASS -- all directories are independent |
| No NETSTANDARD2_0 in scope | PASS -- 0 matches across all 6 directories |
| No forward references between Wave 1 plans | PASS -- no inter-plan dependencies |
| Perl regex in builder note handles multiline blocks | PASS -- `perl -0777 -pe 's/\n*#if NETFULL\b.*?#endif[^\n]*//gs'` uses slurp mode with dotall flag |
| WSL cross-mount workaround documented | PASS -- builder note specifies write to /tmp then cp back |

## Roadmap Discrepancy (Non-Blocking)

The ROADMAP.md file counts for Phase 3 sub-phases are stale:

| Sub-phase | Roadmap claims | Actual | Delta |
|-----------|---------------|--------|-------|
| 3a SqlServer | 13 | 18 | +5 |
| 3b PostgreSQL | 13 | 18 | +5 |
| 3c SQLite | 13 | 17 | +4 |
| 3d Redis | 14 | 15 | +1 |
| 3e LiteDB | 13 | 17 | +4 |
| 3f Memory | 10 | 12 | +2 |

The plans use the correct (actual) counts. The roadmap counts were likely from an earlier enumeration before all NETFULL blocks were identified. This does not affect execution.

## Overall Verdict: **READY**

Both plans are accurate, complete, and ready for builder execution. Every file path verified, every count confirmed, no conflicts, no hidden dependencies. The only finding is a cosmetic discrepancy in the roadmap documentation.
