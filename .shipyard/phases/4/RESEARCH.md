# Phase 4 Research: CI, Documentation, Version Bump, and Cleanup

## Context

Branch `issue-101-drop-net48` has completed Phases 1-3, which removed `net48`/`netstandard2.0` targets from all `.csproj` files, deleted `#if NETFULL` / `#if NETSTANDARD2_0` conditional blocks from source code, and cleaned up Linq integration test projects. Phase 4 is the final phase: update CI, documentation, version, and commit all remaining changes.

---

## 1. Unstaged Changes from Prior Phases

**10 files modified but not staged/committed:**

| File | Nature of Change |
|------|-----------------|
| `.shipyard/STATE.json` | Phase status updated from 3/complete to 4/planning |
| `CLAUDE.md` | Appended "Code Quality" section (6 lines) |
| `Source/DotNetWorkQueue/ASendJobToQueue.cs` | Removed `#if NETFULL` block (18 lines): `SendAsync(LinqExpressionToRun)` overload |
| `Source/DotNetWorkQueue/Exceptions/CompileException.cs` | Removed `#if NETFULL` block (19 lines): `GetObjectData` serialization override + `using System.Runtime.Serialization` |
| `Source/DotNetWorkQueue/IJobScheduler.cs` | Removed `#if NETFULL` block (52 lines): two `AddUpdateJob` overloads taking `LinqExpressionToRun` |
| `Source/DotNetWorkQueue/IProducerMethodJobQueue.cs` | Removed `#if NETFULL` block (11 lines): `SendAsync(LinqExpressionToRun)` |
| `Source/DotNetWorkQueue/IProducerMethodQueue.cs` | Removed `#if NETFULL` block (45 lines): 6 methods (Send/SendAsync for `LinqExpressionToRun`) |
| `Source/DotNetWorkQueue/ISendJobToQueue.cs` | Removed `#if NETFULL` block (10 lines): `SendAsync(LinqExpressionToRun)` |
| `Source/DotNetWorkQueue/Queue/ProducerMethodJobQueue.cs` | Removed `#if NETFULL` block (8 lines): implementation of `SendAsync(LinqExpressionToRun)` |
| `Source/DotNetWorkQueue/Queue/ProducerMethodQueue.cs` | Removed `#if NETFULL` block (57 lines): 6 method implementations for `LinqExpressionToRun` |

**Total: 224 lines removed across 8 source files, plus CLAUDE.md and STATE.json updates.**

These changes are the tail end of Phase 3's NETFULL removal from the core library interfaces/implementations. They must be committed as part of Phase 4 (or as a separate "Phase 3 stragglers" commit before Phase 4 work begins).

**Important note:** `LinqExpressionToRun` class itself still exists in `Source/DotNetWorkQueue/Messages/LinqExpressionToRun.cs` and is referenced by 9 source files (compiler, decorator, serialization, integration test messages). Only the **public API surface** that accepted it on producer/scheduler interfaces was behind `#if NETFULL`. The class remains for the compiled LINQ expression path (`MessageExpressionPayloads.ActionText`).

---

## 2. GitHub Actions CI (`.github/workflows/ci.yml`)

### Current State (63 lines)
- **Runner:** `windows-latest` (needed only for net48)
- **SDK setup:** `8.0.x` and `10.0.100`
- **Comment (line 2-4):** Says "GitHub Actions runs unit tests on Windows (net48) for .NET Framework compatibility"
- **Comment (line 32):** Says "Unit tests run on net48 to validate .NET Framework compatibility"
- **8 test steps** with `-f net48` flag: Core, RelationalDatabase, PostgreSQL, Redis, SQLite, LiteDb, SqlServer, Memory
- **3 test steps** without `-f net48`: Dashboard.Api, Dashboard.Client (these never targeted net48)

### Required Changes

| Line(s) | Current | Change To |
|---------|---------|-----------|
| 2-4 | Comment about net48 on Windows | Update to describe net8.0/net10.0 unit tests on Ubuntu |
| 15 | `runs-on: windows-latest` | `runs-on: ubuntu-latest` |
| 22-24 | SDK versions `8.0.x` + `10.0.100` | Keep both (still multi-targeting net8.0+net10.0) |
| 27 | `dotnet restore "Source\DotNetWorkQueue.sln"` | Change backslashes to forward slashes for Linux |
| 29 | `dotnet build "Source\DotNetWorkQueue.sln"` | Change backslashes to forward slashes for Linux |
| 32 | Comment about net48 | Remove or update |
| 35 | `-f net48` on Core tests | Remove `-f net48` (will run both net8.0 and net10.0) |
| 38 | `-f net48` on RelationalDatabase tests | Remove `-f net48` |
| 41 | `-f net48` on PostgreSQL tests | Remove `-f net48` |
| 44 | `-f net48` on Redis tests | Remove `-f net48` |
| 47 | `-f net48` on SQLite tests | Remove `-f net48` |
| 50 | `-f net48` on LiteDb tests | Remove `-f net48` |
| 53 | `-f net48` on SqlServer tests | Remove `-f net48` |
| 62 | `-f net48` on Memory tests | Remove `-f net48` |
| All `run:` lines | Backslash paths (`Source\...`) | Forward slash paths (`Source/...`) for Linux |

### Decision: windows-latest vs ubuntu-latest
With net48 gone, there is no reason to use `windows-latest`. The .NET SDK on Ubuntu supports net8.0 and net10.0. Switching to `ubuntu-latest` provides:
- Faster VM startup (~30s vs ~60s)
- Lower cost (GitHub Actions bills Linux at 1x, Windows at 2x)
- Consistency with Jenkins CI (which already runs on Linux Docker agents)

**Caveat:** All path separators in the workflow must change from `\` to `/`. This affects every `run:` line.

---

## 3. README.md

### Current State (205 lines)

| Line | Content | Action |
|------|---------|--------|
| 8 | "Targets .NET 4.8, .NET 8.0, .NET 10.0, and .NET Standard 2.0." | Change to "Targets .NET 8.0 and .NET 10.0." |
| 12 | "Queue / process LINQ statements (compiled or dynamic, expressed as a string)" | Change to "Queue / process LINQ expressions (compiled)" -- remove "dynamic" and "expressed as a string" |
| 61-66 | "Differences Between Versions" section about .NET Standard 2.0/8.0/10.0 missing dynamic LINQ | **Delete entire section** -- no longer applicable; all targets have identical features |
| 86-96 | Producer section mentions dynamic LINQ casting, `LinqExpressionToRun` usage for dynamic strings | **Review and simplify** -- dynamic LINQ casting notes and inline parsing examples are net48-only patterns. However, the compiled LINQ path still uses expressions with string parameters. Need to check if these examples are about dynamic or compiled LINQ. |
| 113 | "AppDomain.AssemblyResolve (MSDN)" link | Keep -- assembly resolution is still relevant for compiled LINQ consumers |
| 117-128 | "Security Considerations" section mentions "application domain sandbox" | Change "consider running the consumer in an application domain sandbox" to just warning about OS-level permissions -- AppDomain sandboxing is a net48-only feature |
| 187 | Third-Party Libraries: "JpLabs.DynamicCode" with blog link | **Remove** JpLabs.DynamicCode from the list (no longer in `/Lib` or any csproj) |

### Surprises Found
- Lines 86-106 (Producer LINQ section): The dynamic LINQ casting examples (`(IReceivedMessage<MessageExpression>) message`) and inline parsing (`Guid.NewGuid()` in string template) are specifically about the **dynamic** LINQ path. These code samples and surrounding text should be removed or reworked since dynamic LINQ is no longer available.
- Line 89: "Note: When passing `message` or `workerNotification` as arguments to dynamic LINQ..." -- this entire note is dynamic-LINQ-specific.

---

## 4. CLAUDE.md

### Current State (130 lines)

| Line(s) | Content | Action |
|---------|---------|--------|
| 7 | "Targets .NET 10.0, .NET 8.0, .NET Framework 4.8, and .NET Standard 2.0" | Change to "Targets .NET 10.0 and .NET 8.0" |
| 7 | "LINQ statements (compiled or dynamic)" | Change to "LINQ expressions (compiled)" |
| 43 | `dotnet test "Source\DotNetWorkQueue.AppMetrics.Tests\..."` | **Remove** -- project no longer exists |
| 82 | "LINQ expression variants for dynamic/compiled expressions" | Change to "LINQ expression variants for compiled expressions" |
| 88-89 | "Multi-targeting" section: `NETFULL` for .NET 4.8-specific code, `NETSTANDARD2_0` | **Delete entire section** -- no longer multi-targeting with conditional compilation |
| 99 | "Custom libraries in `/Lib`: Schyntax, Aq.ExpressionJsonSerializer, JpLabs.DynamicCode" | Remove JpLabs.DynamicCode from the list |
| 107 | CI convention: "GitHub Actions runs net48 unit tests only for .NET Framework compatibility validation" | Update to describe running net8.0/net10.0 unit tests on Ubuntu |
| 111 | Lesson about `#if NETFULL` guards | **Remove or mark as historical** -- no longer applicable |

### Unstaged CLAUDE.md Change
The current diff appends a "Code Quality" section (lines 125-130). This is a new section that should be kept. The Phase 4 edits to CLAUDE.md must be applied on top of this unstaged addition.

---

## 5. Version Bump

### Current State
- `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` line 7: `<Version>0.9.18</Version>`
- Description (line 8): "Work queue for dot net 8.0 and 10.0" -- already updated; no net48 reference.
- The phase scope says "Update to 0.9.3" but this is **wrong** -- version 0.9.3 already exists in CHANGELOG.md (2026-03-10, Dashboard consumer tracking). The current version is 0.9.18.
- Memory note from `project_net48_removal.md` says "version 0.9.3" but this is stale -- the project has had 15 releases since then.

### Recommendation
- Bump to **0.9.19** (next sequential version)
- Add CHANGELOG.md entry at the top for 0.9.19 documenting the net48/netstandard2.0 removal

### CHANGELOG Entry Content
```
### 0.9.19 -- 2026-04-07
- **Breaking:** Drop .NET Framework 4.8 and .NET Standard 2.0 targets; now targets .NET 8.0 and .NET 10.0 only (GitHub #101)
- Remove dynamic LINQ expression support (was .NET Framework 4.8 only)
- Remove `#if NETFULL` / `#if NETSTANDARD2_0` conditional compilation throughout
- Remove JpLabs.DynamicCode from `/Lib`
- GitHub Actions CI: switch from Windows (net48) to Ubuntu (net8.0/net10.0)
```

---

## 6. ISSUE-021: Empty Shell Files

All 7 files confirmed to exist and contain only dead weight:

| File | Lines | Content |
|------|-------|---------|
| `.../SqlServer.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs` | 6 | 2 usings + empty namespace |
| `.../PostgreSQL.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs` | 6 | 2 usings + empty namespace |
| `.../SQLite.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs` | 6 | 2 usings + empty namespace |
| `.../Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs` | 6 | 2 usings + empty namespace |
| `.../LiteDB.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs` | 6 | 2 usings + empty namespace |
| `.../Memory.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs` | 7 | Leading blank + 2 usings + empty namespace |
| `.../Memory.Linq.Integration.Tests/ProducerMethod/SimpleMethodProducerDynamicListSend.cs` | 9 | 8 usings, no namespace/class |

**Action:** Delete all 7 files. No csproj changes needed (they use glob-based inclusion).

---

## 7. ISSUE-022: No-op Dynamic Test Parameters

### PostgreSQL JobSchedulerTests.cs
- Line 15-16: `[DataRow(true, false), DataRow(true, true)]`
- The `DataRow(true, true)` passes `dynamic=true`, which hits an `if (!dynamic)` guard in the shared implementation and does nothing.
- **Action:** Remove `DataRow(true, true)`, leaving only `DataRow(true, false)`. Also remove the blank line on line 14 (ISSUE-023).
- The `bool dynamic` parameter can be removed from this method and the `interceptors` parameter renamed, but that requires changing the shared implementation and all 7 callers -- may be out of scope for Phase 4.

### LiteDb JobSchedulerTests.cs
- Line 14: `[DataRow(false), DataRow(true)]`
- `DataRow(true)` passes `dynamic=true`, which is a no-op.
- **Action:** Remove `DataRow(true)`, leaving only `DataRow(false)`.
- Same note about removing `bool dynamic` parameter entirely.

---

## 8. ISSUE-023: Cosmetic Artifacts

### PostgreSQL JobSchedulerTests.cs
- Line 14: blank line between `[TestMethod]` and `[DataRow]` -- delete it.

### Memory Linq Integration Tests csproj
- Lines 22-23: double blank line before `</Project>` -- remove one blank line.

---

## 9. Complications and Surprises

### 9.1 Version number mismatch
The Phase 4 scope says "Update to 0.9.3" but 0.9.3 already exists (released 2026-03-10). Current version is 0.9.18. Must bump to 0.9.19.

### 9.2 `LinqExpressionToRun` class survives
The class itself and its usage in `LinqCompiler`, `MessageMethodHandling`, metric decorators, and integration test helpers remain. Only the NETFULL-gated public API surface was removed. The `DynamicCode` metric timer in `IMessageMethodHandlingDecorator.cs` still references it for the `ActionText` payload type. This is correct -- compiled LINQ expressions that are serialized as text still go through that code path.

### 9.3 Dynamic LINQ in README needs careful editing
The README LINQ section (lines 82-128) mixes dynamic and compiled LINQ concepts. The casting notes (lines 89-95) and inline value parsing (lines 99-106) are specifically for the dynamic path. But the compiled LINQ expression path still exists. Need to remove dynamic-specific content while preserving compiled LINQ documentation.

### 9.4 CLAUDE.md has unstaged additions
The "Code Quality" section was added but not committed. Phase 4 edits must be applied on top of this.

### 9.5 `AppMetrics.Tests` in CLAUDE.md is stale
CLAUDE.md line 43 references `DotNetWorkQueue.AppMetrics.Tests` which no longer exists (removed in 0.9.1). This is pre-existing tech debt, not from this branch, but should be cleaned up.

### 9.6 README Security section references AppDomain sandboxing
Line 124: "consider running the consumer in an application domain sandbox" -- AppDomain sandboxing is not available in .NET 8/10. This needs rewording.

### 9.7 SECURITY.md also references dynamic LINQ
`Source/DotNetWorkQueue/SECURITY.md` mentions dynamic LINQ and `LinqExpressionToRun`. Should be reviewed for accuracy but may be out of Phase 4 scope.

---

## 10. Summary of All Work Items

| # | Item | Files | Complexity |
|---|------|-------|------------|
| 1 | Commit unstaged Phase 3 changes | 10 files | Low -- just commit |
| 2 | Rewrite GitHub Actions CI | `.github/workflows/ci.yml` | Medium -- ubuntu, remove -f net48, fix paths |
| 3 | Update README.md | `README.md` | Medium -- remove net48/dynamic refs, rework LINQ section |
| 4 | Update CLAUDE.md | `CLAUDE.md` | Medium -- many scattered changes |
| 5 | Version bump + CHANGELOG | `DotNetWorkQueue.csproj`, `CHANGELOG.md` | Low |
| 6 | Delete empty shell files (ISSUE-021) | 7 files to delete | Low |
| 7 | Fix no-op test params (ISSUE-022) | 2 test files | Low |
| 8 | Fix cosmetic artifacts (ISSUE-023) | 2 files | Low |
