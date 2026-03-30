# Roadmap: CONCERNS.md Quick Wins & Accepted Risk Closures

## Overview

Close out all low-effort items from CONCERNS.md and ISSUES.md in a single PR. This covers 4 accepted-risk documentation closures, 5 small code/config fixes from CONCERNS.md, and all 13 open issues from ISSUES.md. Every change is cosmetic, documentation, or minor correctness -- no behavioral changes, no public API surface changes.

**Prerequisite**: None. All items are independent quick wins against the current `master` branch.

---

## Phase Summary

| Phase | Name | Complexity | Dependencies | Plans | Risk |
|-------|------|-----------|-------------|-------|------|
| 1 | Quick Wins & Accepted Risk Closures | Low | None | 3 | Very Low -- all changes are cosmetic or documentation |

---

## Phase 1: Quick Wins & Accepted Risk Closures

**Complexity**: Low
**Dependencies**: None
**Risk**: Very Low. All changes are cosmetic, documentation, build-config, or minor code quality. No behavioral changes. No public API changes. Verification is straightforward: unit tests pass, no new warnings.

### Task Grouping

The 22 items naturally cluster into 3 groups based on the type of change:

1. **Documentation & Status Updates** -- Items that only modify `.shipyard/` markdown files: accepted-risk rationale (C-1, C-2, H-1, L-3), status updates in CONCERNS.md and ISSUES.md, and missing SUMMARY file (ISSUE-012).
2. **Code Fixes** -- Items that modify `.cs` source files: unused variables/imports (ISSUE-001, ISSUE-006, ISSUE-010, ISSUE-011), test assertion improvements (ISSUE-003, ISSUE-004), log wording (ISSUE-009), operator precedence (ISSUE-013), Timer.DisposeAsync (ISSUE-007), sync-over-async test fix (ISSUE-008), stale XML doc comment (ISSUE-005), LiteDb Server property (L-5), and xUnit pragma removal (M-4 partial).
3. **Build & Config Fixes** -- Items that modify `.csproj`, `.gitignore`, `.xml`, or delete files: malformed DocumentationFile path (M-5), stale gitignore patterns and artifact removal (M-8), xUnit runner.json deletion (M-4 partial), and XML doc regeneration (N-4).

### Items by Task

| Task | Item IDs | Count |
|------|----------|-------|
| Task 1: Documentation & Status Updates | C-1, C-2, H-1, L-3, ISSUE-012 | 5 |
| Task 2: Code Fixes | ISSUE-001, ISSUE-002, ISSUE-003, ISSUE-004, ISSUE-005, ISSUE-006, ISSUE-007, ISSUE-008, ISSUE-009, ISSUE-010, ISSUE-011, ISSUE-013, L-5, M-4 (pragma) | 14 |
| Task 3: Build & Config Fixes | M-4 (xunit.runner.json), M-5, M-8, N-4 | 4 |

### Wave Assignment

- **Wave 1**: Task 1 (documentation only, no code dependencies) and Task 2 (code fixes, no build-config dependencies) -- these can execute in parallel.
- **Wave 2**: Task 3 (build & config fixes) -- N-4 (XML doc regeneration) requires a Release build which benefits from Task 2's code fixes being applied first, so the regenerated XML reflects the final state.

### Key Files

#### Task 1: Documentation & Status Updates
| File | Change |
|------|--------|
| `.shipyard/codebase/CONCERNS.md` | Mark C-1, C-2, H-1, L-3 with resolution status and rationale |
| `.shipyard/codebase/CONCERNS.md` | Mark M-4, M-5, M-8, L-5, N-4 as resolved after fixes |
| `.shipyard/ISSUES.md` | Mark all 13 issues as closed |
| `.shipyard/phases/7/SUMMARY-plan01.md` | Create missing SUMMARY file for Phase 7 Plan 01 (ISSUE-012) |

#### Task 2: Code Fixes
| File | Change |
|------|--------|
| `Source/DotNetWorkQueue.Tests/QueueCreatorTests.cs` | Remove unused `fixture` variable (ISSUE-001) |
| `Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs` | Add `RegexOptions.Compiled` to ValidateQueueName regex (ISSUE-002) |
| `Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs` | Add `RegexOptions.Compiled` (ISSUE-002); replace "TODO; not known" Server value (L-5) |
| `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs` | Add `RegexOptions.Compiled` (ISSUE-002) |
| `Source/DotNetWorkQueue.Transport.Redis/RedisConnectionInfo.cs` | Add `RegexOptions.Compiled` (ISSUE-002) |
| `Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs` | Add `RegexOptions.Compiled` (ISSUE-002) |
| `Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs` | Add `RegexOptions.Compiled` (ISSUE-002) |
| `Source/DotNetWorkQueue.Transport.SqlServer.Tests/SqlConnectionInformationTests.cs` | Add `Assert.AreEqual("MyQueue123", ...)` (ISSUE-003) |
| `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/SqlConnectionInformationTests.cs` | Add `Assert.AreEqual("MyQueue123", ...)` (ISSUE-003) |
| `Source/DotNetWorkQueue.Transport.SQLite.Tests/SQLiteConnectionInformationTests.cs` | Add `Assert.AreEqual("MyQueue123", ...)` (ISSUE-003) |
| `Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs` | Add `Assert.AreEqual` (ISSUE-004); remove 5 unused `using` directives (ISSUE-006) |
| `Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs` | Add `Assert.AreEqual` (ISSUE-004) |
| `Source/DotNetWorkQueue.Tests/Transport/Memory/ConnectionInformationTests.cs` | Add `Assert.AreEqual` (ISSUE-004); fix stale XML doc comment (ISSUE-005) |
| `Source/DotNetWorkQueue.Dashboard.Client/DashboardConsumerClient.cs` | Replace `_heartbeatTimer.Dispose()` with `await _heartbeatTimer.DisposeAsync()` in DisposeAsync (ISSUE-007) |
| `Source/DotNetWorkQueue.Dashboard.Client.Tests/DashboardConsumerClientTests.cs` | Fix sync-over-async test assertion (ISSUE-008) |
| `Source/DotNetWorkQueue/Queue/PrimaryWorker.cs` | Change log "Stopping worker thread" to "Stopping worker" (ISSUE-009) |
| `Source/DotNetWorkQueue/Queue/Worker.cs` | Change log "Stopping worker thread" to "Stopping worker" (ISSUE-009) |
| `Source/DotNetWorkQueue/Queue/WorkerTerminate.cs` | Remove unused `using System.Threading;` (ISSUE-010) |
| `Source/DotNetWorkQueue/Queue/WaitForThreadToFinish.cs` | Remove unused `using System.Threading;` (ISSUE-011) |
| `Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs` | Add explicit parentheses in `Running` property (ISSUE-013) |
| `Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerAsync/Implementation/SimpleConsumerAsync.cs` | Remove `#pragma warning disable xUnit1013` (M-4 partial) |

#### Task 3: Build & Config Fixes
| File | Change |
|------|--------|
| `Source/xunit.runner.json` | Delete file (M-4) |
| `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj` | Remove leading `>` from 3 DocumentationFile entries (M-5) |
| `.gitignore` | Add patterns for `*.7z`, `TeamCity_*.zip`, `codcov*.txt`, `*.DotSettings.user`, working notes (M-8) |
| `Source/DotNetWorkQueue/DotNetWorkQueue.xml` | Regenerate via Release build (N-4) |

### Success Criteria

1. **CONCERNS.md updated**: C-1, C-2, H-1, L-3 marked with "Accepted Risk" or "Will Not Fix" status and documented rationale
2. **CONCERNS.md updated**: M-4, M-5, M-8, L-5, N-4 marked as resolved with date
3. **ISSUES.md updated**: All 13 issues (ISSUE-001 through ISSUE-013) marked as closed with date
4. **All unit tests pass**: `dotnet test Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj` and all other unit test projects exit 0
5. **No new warnings**: `dotnet build Source/DotNetWorkQueueNoTests.sln -c Release` produces no new warnings
6. **Zero xUnit artifacts**: `Source/xunit.runner.json` deleted; no `xUnit1013` pragma in codebase
7. **Zero malformed DocumentationFile**: `grep ">&gt;" Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj` returns no hits
8. **Zero "TODO; not known"**: `grep -r "TODO; not known" Source/ --include="*.cs"` returns no hits
9. **Stale patterns in .gitignore**: `*.7z`, `*.DotSettings.user`, `codcov*.txt` patterns present in `.gitignore`
10. **XML doc is current**: `grep -c "AbortWorkerThread" Source/DotNetWorkQueue/DotNetWorkQueue.xml` returns 0

---

## Parallelism Notes

- **Task 1 and Task 2 are independent** (Wave 1). Task 1 modifies only `.shipyard/` markdown files. Task 2 modifies only `.cs` source and test files. No file overlap.
- **Task 3 depends on Task 2** (Wave 2). The XML documentation regeneration (N-4) should happen after all code fixes are applied so the generated XML reflects the final codebase state.
- Within Task 2, all code fixes are independent of each other (different files, no cross-dependencies).

## Breaking Changes

None. All changes are internal: unused import removal, test assertion improvements, log message wording, build configuration fixes, and documentation updates. No public API surface is affected.

## Risk Assessment

- **Very Low overall risk**. Every change is either documentation-only, removes dead code, improves test assertions, or fixes build configuration. No behavioral changes.
- **Highest-risk item**: ISSUE-007 (Timer.DisposeAsync) -- changes disposal semantics slightly, but only within the `DisposeAsync` path which is already async. The synchronous `Dispose()` path is unchanged.
- **N-4 (XML regeneration)** requires a Release build, which may surface warnings from other unrelated issues. The build should succeed since the codebase already builds in Debug mode; Release adds `TreatWarningsAsErrors` so any pre-existing warnings could block. Mitigation: fix M-5 (malformed DocumentationFile) first.
