---
phase: 4-ci-docs-version
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - Commit the 10 unstaged files from prior phases
  - Delete 7 empty shell files (ISSUE-021)
  - Fix no-op dynamic test parameter (ISSUE-022) and cosmetic artifacts (ISSUE-023)
files_touched:
  # Unstaged from prior phases (commit only, no edits needed)
  - .shipyard/STATE.json
  # Note: CLAUDE.md excluded -- handled by PLAN-1.3 (edit + commit)
  - Source/DotNetWorkQueue/ASendJobToQueue.cs
  - Source/DotNetWorkQueue/Exceptions/CompileException.cs
  - Source/DotNetWorkQueue/IJobScheduler.cs
  - Source/DotNetWorkQueue/IProducerMethodJobQueue.cs
  - Source/DotNetWorkQueue/IProducerMethodQueue.cs
  - Source/DotNetWorkQueue/ISendJobToQueue.cs
  - Source/DotNetWorkQueue/Queue/ProducerMethodJobQueue.cs
  - Source/DotNetWorkQueue/Queue/ProducerMethodQueue.cs
  # ISSUE-021: Delete empty shell files
  - Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs
  - Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs
  - Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs
  - Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs
  - Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs
  - Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/ProducerMethod/SimpleMethodProducerDynamicListSend.cs
  # ISSUE-022: Fix no-op dynamic test rows
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs
  - Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs
  # ISSUE-022 (full cleanup): Remove vestigial dynamic parameter from shared impl + all 6 callers
  - Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/Implementation/JobSchedulerTests.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs
  - Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs
  - Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs
  - Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs
  # ISSUE-023: Cosmetic blank line fixes
  - Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj
tdd: false
---

# Plan 1.1 -- Code Cleanup: Unstaged Sweep + ISSUE-021 + ISSUE-022 + ISSUE-023

## Context

Phase 4 is the final phase. Before touching docs/CI/version, we must first land all outstanding code changes:

1. **Unstaged changes:** 9 files modified in prior phases (8 core .cs files with `#if NETFULL` removal, plus STATE.json) were never committed. They need to be staged and committed as-is -- no edits, just a commit. (CLAUDE.md is excluded -- PLAN-1.3 handles its edits and commit.)
2. **ISSUE-021:** 7 files are empty shells (only unused `using` directives and empty namespaces) left behind after NETFULL block removal in Phase 3. Delete them.
3. **ISSUE-022:** The `bool dynamic` parameter in the shared `JobSchedulerTests.Run()` is now vestigial -- the `if (!dynamic)` guard means `dynamic=true` always produces a no-op test. PostgreSQL has `DataRow(true, true)` and LiteDb has `DataRow(true)` that exercise this dead path. Since dynamic LINQ is permanently removed, the cleanest fix is to remove the `dynamic` parameter entirely from the shared implementation and all 6 callers.
4. **ISSUE-023:** Cosmetic stray/double blank lines in PostgreSQL JobSchedulerTests (line 14) and Memory csproj (lines 22-23).

## Tasks

<task id="1" files=".shipyard/STATE.json, Source/DotNetWorkQueue/ASendJobToQueue.cs, Source/DotNetWorkQueue/Exceptions/CompileException.cs, Source/DotNetWorkQueue/IJobScheduler.cs, Source/DotNetWorkQueue/IProducerMethodJobQueue.cs, Source/DotNetWorkQueue/IProducerMethodQueue.cs, Source/DotNetWorkQueue/ISendJobToQueue.cs, Source/DotNetWorkQueue/Queue/ProducerMethodJobQueue.cs, Source/DotNetWorkQueue/Queue/ProducerMethodQueue.cs" tdd="false">
  <action>Stage and commit 9 unstaged files from prior phases. These are phase 1/2/3 changes (NETFULL removal from 8 core .cs files, STATE.json) that were completed but never committed. CLAUDE.md is excluded -- PLAN-1.3 handles it. No edits needed -- just `git add` the specific files and commit with message "shipyard(phase-4): commit unstaged phase 1-3 core library changes (issue #101)".</action>
  <verify>git log -1 --stat | head -15</verify>
  <done>Commit exists showing 9 files. `git status` no longer shows them as modified. CLAUDE.md still shows as modified (expected -- PLAN-1.3 handles it).</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs, Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs, Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs, Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs, Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs, Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs, Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/ProducerMethod/SimpleMethodProducerDynamicListSend.cs" tdd="false">
  <action>Delete all 7 empty shell files listed in ISSUE-021. Use `git rm` to delete and stage in one step:

```bash
git rm \
  Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs \
  Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs \
  Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs \
  Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs \
  Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs \
  Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs \
  Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/ProducerMethod/SimpleMethodProducerDynamicListSend.cs
```

Then commit with message "shipyard(phase-4): delete 7 empty shell files after NETFULL removal (ISSUE-021)".</action>
  <verify>ls Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/ConsumerMethod/ConsumerMethodMultipleDynamic.cs 2>&1 | grep -c "No such file"</verify>
  <done>All 7 files no longer exist on disk. `git log -1 --stat` shows 7 deletions.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/Implementation/JobSchedulerTests.cs, Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs, Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs, Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs, Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs, Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs, Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/JobScheduler/JobSchedulerTests.cs, Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" tdd="false">
  <action>Fix ISSUE-022 (remove vestigial `dynamic` parameter) and ISSUE-023 (cosmetic blank lines):

**ISSUE-022 -- Shared implementation** (`Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/Implementation/JobSchedulerTests.cs`):
- Remove `bool dynamic,` parameter from `Run()` method signature (line 11)
- Remove the `if (!dynamic)` guard (line 32) and closing brace (line 39), keeping the body at the same indent level (dedent one level)

**ISSUE-022 -- 6 transport callers** (each file's JobScheduler/JobSchedulerTests.cs):
- **PostgreSQL**: Remove `DataRow(true, true)` (the no-op row). Change `DataRow(true, false)` to just `[DataRow(true)]`. Remove `bool dynamic` parameter. Remove `, dynamic` from the `consumer.Run(...)` call. Also fix ISSUE-023 stray blank line between `[TestMethod]` and `[DataRow]` (line 14).
- **LiteDb**: Remove `DataRow(true)` (the no-op row). Change `DataRow(false)` to remove it (single parameterless test). Remove `bool dynamic` parameter. Remove `, dynamic` from the `consumer.Run(...)` call. Convert method to parameterless `[TestMethod]` (no DataRow needed).
- **SqlServer**: Change `DataRow(true, false)` to `[DataRow(true)]`. Remove `bool dynamic` parameter. Remove `, dynamic` from the `consumer.Run(...)` call.
- **SQLite**: `DataRow(false, false)` becomes `[DataRow(false)]`, `DataRow(false, true)` becomes `[DataRow(true)]`. Remove `bool dynamic` parameter, keep `bool inMemoryDb`. Remove `, dynamic` from the `consumer.Run(...)` call.
- **Redis**: Remove `DataRow(false)` entirely. Remove `bool dynamic` parameter. Remove `, dynamic` from the `consumer.Run(...)` call. Convert to parameterless `[TestMethod]`.
- **Memory**: Remove `DataRow(false)` entirely. Remove `bool dynamic` parameter. Remove `, dynamic` from the `consumer.Run(...)` call. Convert to parameterless `[TestMethod]`.

**ISSUE-023 -- Memory csproj** (`DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj`):
- Remove one of the two blank lines at lines 22-23 (between `</ItemGroup>` and `</Project>`).

Commit with message "shipyard(phase-4): remove vestigial dynamic parameter from JobSchedulerTests (ISSUE-022, ISSUE-023)".</action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet build Source/DotNetWorkQueue.sln -c Debug --verbosity quiet 2>&1 | tail -5</verify>
  <done>Solution builds with 0 errors. `grep -rn 'bool dynamic' Source/DotNetWorkQueue.Transport.*.Linq.Integration.Tests/JobScheduler/` returns 0 matches. `grep -rn 'bool dynamic' Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/Implementation/JobSchedulerTests.cs` returns 0 matches. No double blank lines in Memory csproj. No stray blank line in PostgreSQL JobSchedulerTests.</done>
</task>

## Builder Notes

- Task 1 is a pure git operation -- no file edits. Just stage and commit.
- Task 2 is `git rm` + commit. No edits.
- Task 3 requires careful edits across 8 files. The shared implementation change (removing the `if (!dynamic)` guard) requires dedenting the body. Each caller has a slightly different signature:
  - SqlServer/PostgreSQL: `(bool interceptors, bool dynamic)` -- remove `dynamic`, keep `interceptors`
  - SQLite: `(bool dynamic, bool inMemoryDb)` -- remove `dynamic`, keep `inMemoryDb`
  - Redis/Memory/LiteDb: `(bool dynamic)` -- remove parameter entirely, convert to parameterless method
- For the shared impl, the `if (!dynamic) { ... }` block on lines 32-39 should be replaced by just the body (lines 33-38) at the outer indent level.
- Tasks 1 and 2 are independent and can be done in either order. Task 3 should be last since it requires a build verification.
