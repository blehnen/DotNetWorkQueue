# Review: Plan 1.1 -- SqlServer, PostgreSQL, SQLite Linq Integration Tests

**Reviewer:** Claude Opus 4.6
**Date:** 2026-04-07
**Verdict:** PASS

---

## Stage 1: Spec Compliance
**Verdict:** PASS

### Task 1: SqlServer Linq Integration Tests
- Status: PASS
- Evidence: Commit `e3a15db9` modifies 18 .cs files + 1 csproj (19 files, 282 deletions). `grep -r "NETFULL|net48"` returns zero matches. Csproj at `Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj` has `<TargetFramework>net10.0</TargetFramework>` (singular). No conditional PropertyGroup or ItemGroup blocks remain. No orphaned `#if`, `#else`, or `#endif` directives in any .cs file. `LinqMethodTypes.Dynamic` references: zero. `LinqMethodTypes.Compiled` DataRow attributes preserved (e.g., `ConsumerMethodCancelWork.cs`, `SimpleProducerMethod.cs`).
- Notes: File count matches plan exactly. SUMMARY reports build passes with 0 errors.

### Task 2: PostgreSQL Linq Integration Tests
- Status: PASS
- Evidence: Commit `5552038a` modifies 18 .cs files + 1 csproj (19 files, 282 deletions). `grep -r "NETFULL|net48"` returns zero matches. Csproj at `Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj` has `<TargetFramework>net10.0</TargetFramework>` (singular). No conditional blocks remain. No orphaned preprocessor directives. `LinqMethodTypes.Dynamic` references: zero. `LinqMethodTypes.Compiled` DataRow attributes preserved (e.g., `SimpleProducerMethod.cs` line 15-29).
- Notes: File count matches plan exactly. SUMMARY reports build passes with 0 errors.

### Task 3: SQLite Linq Integration Tests
- Status: PASS
- Evidence: Commit `22c6d9bc` modifies 17 .cs files + 1 csproj (18 files, 214 deletions). `grep -r "NETFULL|net48"` returns zero matches. Csproj at `Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj` has `<TargetFramework>net10.0</TargetFramework>` (singular). No conditional blocks remain. No orphaned preprocessor directives. `LinqMethodTypes.Dynamic` references: zero. `LinqMethodTypes.Compiled` DataRow attributes preserved (e.g., `SimpleMethodProducer.cs`, `ConsumerMethodCancelWork.cs`).
- Notes: File count matches plan exactly. SUMMARY reports build passes with 0 errors, 1 pre-existing SYSLIB0012 warning in dependency project.

---

## Stage 2: Code Quality
(Stage 1 passed)

### Critical
None.

### Important
None.

### Suggestions
- **Empty shell files left behind after NETFULL block removal** (ISSUE-021)
  - Files:
    - `Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs`
    - `Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs`
    - `Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs`
  - All three `ConsumerMethodMultipleDynamic.cs` files now contain only unused `using` directives and an empty namespace block. The entire class was inside `#if NETFULL`, so after removal nothing meaningful remains. These files should be deleted entirely to avoid confusion.
  - Remediation: Delete all three `ConsumerMethodMultipleDynamic.cs` files and verify the projects still build. Check if PLAN 1.2 projects (Redis, LiteDB, Memory) have the same pattern and apply there too.

### Integration Review
- **No conflicts with PLAN 1.2:** PLAN 1.2 targets Redis, LiteDB, and Memory Linq integration test projects -- completely disjoint file sets from PLAN 1.1's SqlServer, PostgreSQL, and SQLite projects.
- **Convention compliance:** Test .cs files follow the existing convention of no LGPL headers (only main library source files carry headers). Csproj formatting is consistent with other already-cleaned projects from Phase 2.

---

## Summary
**Verdict:** APPROVE
All three tasks are correctly implemented. NETFULL blocks removed, net48 targets stripped, `#else` branch code preserved, no orphaned directives, all csproj files use singular `<TargetFramework>net10.0</TargetFramework>`. One suggestion logged (ISSUE-021) for empty shell files that could be deleted.
Critical: 0 | Important: 0 | Suggestions: 1
