# Documentation Report
**Phase:** 1 -- Drop net48/netstandard2.0 from library projects

## Summary
- API/Code docs: 0 files updated (Phase 4 scope)
- Architecture updates: 0 sections updated (no structural changes, only TFM removal)
- User-facing docs: 0 files updated now; 5 specific gaps identified for Phase 4

## Documentation Gaps Identified

All gaps below are **deferred to Phase 4** (version bump + docs), per the task instructions. This report catalogs them so nothing is missed.

### 1. README.md -- Target framework line (line 8)
- **Type:** Reference
- **File:** `/mnt/f/git/dotnetworkqueue/README.md`
- **Issue:** States "Targets .NET 4.8, .NET 8.0, .NET 10.0, and .NET Standard 2.0." Must change to "Targets .NET 8.0 and .NET 10.0."

### 2. README.md -- "Differences Between Versions" section (lines 61-66)
- **Type:** Reference
- **File:** `/mnt/f/git/dotnetworkqueue/README.md`
- **Issue:** This entire section discusses .NET Standard 2.0 / .NET 8.0 / .NET 10.0 missing dynamic LINQ compared to "the full framework version." Since net48 is removed and dynamic LINQ is removed from ALL targets, this section should be deleted entirely.

### 3. README.md -- Dynamic LINQ usage sections (lines 81-128)
- **Type:** Tutorial
- **File:** `/mnt/f/git/dotnetworkqueue/README.md`
- **Issue:** The "Usage -- LINQ Expressions" section includes extensive documentation on dynamic LINQ strings (casting `message`/`workerNotification`, string interpolation for value types, security warnings about `Environment.Exit`). Since `LinqExpressionToRun` overloads are removed from all public interfaces, this content needs significant revision. Compiled LINQ expressions (`Expression<Action<...>>`) still work; only the dynamic string-based path is gone. The security subsection about sandboxing dynamic LINQ can be removed.

### 4. README.md -- Third-Party Libraries (line 187)
- **Type:** Reference
- **File:** `/mnt/f/git/dotnetworkqueue/README.md`
- **Issue:** Lists "JpLabs.DynamicCode" under custom libraries in `/Lib`. This vendored DLL was deleted in Phase 1. Remove from list.

### 5. SECURITY.md -- Dynamic LINQ compilation section (lines 85-113)
- **Type:** Explanation
- **File:** `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue/SECURITY.md`
- **Issue:** The "Dynamic LINQ compilation" section (lines 85-113) describes `DynamicCodeCompiler`, `JpLabs.DynamicCode.Compiler`, `LinqExpressionToRun`, and the ".NET Framework 4.8 only" platform availability. Since net48 is dropped and `DynamicCodeCompiler` is deleted, this entire section can be simplified to state that dynamic LINQ string compilation is no longer supported. The "Platform availability" subsection (lines 99-102) and "What this means" subsection (lines 104-106) are now moot. The mitigations subsection (lines 108-113) can drop the ".NET 8+" bullet since all targets are now .NET 8+.

### 6. CHANGELOG.md -- Breaking changes entry needed
- **Type:** Reference
- **File:** `/mnt/f/git/dotnetworkqueue/CHANGELOG.md`
- **Issue:** Phase 1 introduces multiple breaking changes that need a changelog entry for the next version:
  - **Breaking:** Removed .NET Framework 4.8 and .NET Standard 2.0 targets. Now targets .NET 8.0 and .NET 10.0 only.
  - **Breaking:** Removed dynamic LINQ string compilation (`LinqExpressionToRun`). 6 overloads removed from `IProducerMethodQueue`, 2 overloads removed from `IJobScheduler`. `LinqCompiler.CompileAction` now throws `NotSupportedException`.
  - Deleted vendored `JpLabs.DynamicCode` DLL and `DynamicCodeCompiler` class.
  - Deleted vendored `Schyntax` binaries for net48 and netstandard2.0.
  - Removed `Microsoft.CSharp` PackageReference from core library (built-in on net8.0+).

### 7. CLAUDE.md -- Multi-targeting section
- **Type:** Reference
- **File:** `/mnt/f/git/dotnetworkqueue/CLAUDE.md`
- **Issue:** Line referencing "Targets .NET 10.0, .NET 8.0, .NET Framework 4.8, and .NET Standard 2.0" and the "Multi-targeting" section mentioning `NETFULL` / `NETSTANDARD2_0` conditional compilation. After Phase 1, these conditionals no longer exist in library code. Phase 4 should update CLAUDE.md accordingly.

## Removed Public API Surface (for changelog reference)

### IProducerMethodQueue (6 overloads removed)
All `Send`/`SendAsync` overloads accepting `LinqExpressionToRun`:
- `Send(LinqExpressionToRun, List<QueueDelay>)`
- `Send(LinqExpressionToRun, List<QueueDelay>, TimeSpan)`
- `Send(LinqExpressionToRun, List<QueueDelay>, DateTimeOffset)`
- `SendAsync(LinqExpressionToRun, List<QueueDelay>)`
- `SendAsync(LinqExpressionToRun, List<QueueDelay>, TimeSpan)`
- `SendAsync(LinqExpressionToRun, List<QueueDelay>, DateTimeOffset)`

### IJobScheduler (2 overloads removed)
- `AddUpdateJob(string, string, string, LinqExpressionToRun, ...)`
- `AddUpdateJob(string, string, string, LinqExpressionToRun, ..., bool, ...)`

### Deleted types
- `DotNetWorkQueue.LinqCompile.DynamicCodeCompiler` (entire class deleted)
- `LinqCompiler` constructor with `ObjectPool<DynamicCodeCompiler>` parameter removed; `CompileAction` now throws `NotSupportedException`

## No SECURITY.md Impact from GetObjectData Removal
The `CompileException.GetObjectData` override (removed in Phase 1 along with `#if NETFULL`) was a .NET binary serialization hook unrelated to the security concerns documented in SECURITY.md. No security documentation update needed for this specific removal.
