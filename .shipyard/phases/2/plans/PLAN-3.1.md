---
phase: taskscheduler-nuget-0.4.0
plan: 3.1
wave: 3
dependencies: [2.1, 2.2]
must_haves:
  - Version bumped to 0.4.0 in the .csproj (single source of truth)
  - README.md install example updated to 0.4.0 to prevent doc drift
  - CHANGELOG [0.4.0] entry landed verbatim from DOCUMENTATION-1.md (date substituted)
  - ISSUE-028 <remarks> XML doc added to Start() on interface + impl (byte-identical between files)
  - All four changes in a single release commit on master
files_touched:
  - /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj
  - /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/CHANGELOG.md
  - /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/README.md
  - /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/ITaskSchedulerJobCountSync.cs
  - /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerJobCountSync.cs
tdd: false
risk: high
---

# PLAN-3.1 — The 0.4.0 release commit

**Target repo:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Wave:** 3 (depends on PLAN-2.1 merge, and PLAN-2.2 workflow fix if it ran)
**Risk:** HIGH — this is the single load-bearing commit of Phase 2. It defines what 0.4.0 *is*. Getting version/CHANGELOG/docs atomic into one commit is important because partial commits break the audit trail of "what shipped at this version".

All three tasks below stack into ONE commit at the end of Task 3. Tasks 1 and 2 stage file edits; Task 3 adds the XML doc edits and finalizes the commit + pushes master.

## Pre-conditions

- PLAN-2.1 is done. Master HEAD is a no-ff merge commit of phase-1-lock-fix.
- PLAN-2.2 is either SKIPPED (PLAN-1.1 was GREEN) or DONE (commit stacked on master). Either way, master's working tree is clean before this plan starts.
- PLAN-1.1 Task 2 Result captured the release commit subject/body templates; this plan's Task 3 uses them.

<task id="1" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj, /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/README.md" tdd="false">
  <action>
Bump the version in two places. Stage only — do NOT commit yet.

1. Edit `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj`:
   - Line 9: `<Version>0.3.0</Version>` -> `<Version>0.4.0</Version>`
   - Do not touch any other line; other NuGet metadata (PackageId, Authors, Description, license, symbols settings) is already correct per research doc section 1.3.

2. Edit `README.md`:
   - Line 30: the install example currently reads:
     ```
     <PackageReference Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler" Version="0.3.0" />
     ```
   - Change `Version="0.3.0"` to `Version="0.4.0"`.
   - If line 30 is no longer the exact line (file edits since research), grep for `Version="0.3.0"` and update the unique install-snippet occurrence. There should be exactly one.

Do not `git commit` yet — stage only.
  </action>
  <verify>
```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# csproj version is 0.4.0
grep -n '<Version>' Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj
# Expect: <Version>0.4.0</Version>

# README install example is 0.4.0, no 0.3.0 left
grep -c 'Version="0.3.0"' README.md
# Expect: 0
grep -c 'Version="0.4.0"' README.md
# Expect: 1 (or more, but at least 1)

# No stray 0.3.0 string outside CHANGELOG history
grep -rn '0\.3\.0' --include='*.cs' --include='*.csproj' --include='*.md' . \
  | grep -v CHANGELOG.md \
  | grep -v '.shipyard/'
# Expect: no output (all remaining 0.3.0 strings are in CHANGELOG.md history or .shipyard/ metadata)
```
  </verify>
  <done>
csproj line 9 reads `<Version>0.4.0</Version>`. README.md has no `Version="0.3.0"` string and has exactly one `Version="0.4.0"` install snippet. No `.cs`/`.csproj`/`.md` outside CHANGELOG.md or .shipyard/ still mentions 0.3.0. Changes are staged in the working tree but not committed.
  </done>
</task>

<task id="2" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/CHANGELOG.md" tdd="false">
  <action>
Land the `[0.4.0]` CHANGELOG entry from `.shipyard/phases/1/results/DOCUMENTATION-1.md` verbatim, with the date placeholder substituted for today's ISO date.

1. Determine today's date in `YYYY-MM-DD` format (builder's local date on the day of execution). Call this `$TODAY`.

2. Open `CHANGELOG.md` in the sibling repo. The current structure is:

```
# Changelog

### 0.3.0 2026-04-10
* ...
```

3. Insert the following block between the blank line below `# Changelog` and the `### 0.3.0 2026-04-10` heading. Copy VERBATIM from `DOCUMENTATION-1.md` lines 22–28, substituting `2026-04-XX` with `$TODAY`:

```markdown
### 0.4.0 $TODAY

* Fix: rewrite `TaskSchedulerJobCountSync` message loop to eliminate the lock-contention deadlock between `IncreaseCurrentTaskCount` / `DecreaseCurrentTaskCount` and the legacy `ProcessMessages` loop. The old `_lockSocket` + polling pattern has been replaced with a `NetMQPoller` driving the existing `NetMQActor` plus a new `NetMQQueue<SetCountMsg>` for outbound counter updates; all socket I/O now runs on a dedicated background thread (`TaskSchedulerJobCountSync.Poller`, `IsBackground = true`) owned exclusively by the poller. Closes [issue #6](https://github.com/blehnen/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/issues/6).
* **Behavior change:** `TaskSchedulerJobCountSync.Start()` is now non-blocking. It still performs the host-address handshake, the ~1.1s beacon grace sleep, and the initial `BroadCast` synchronously on the caller thread, but socket-poll wiring (`ReceiveReady` handlers + `NetMQPoller` construction) is now spawned onto a dedicated background thread and `Start()` returns as soon as that thread is running. Callers that subclass or wrap `TaskSchedulerJobCountSync` should not rely on `Start()` blocking for the lifetime of the poller. The public interface signature on `ITaskSchedulerJobCountSync` is unchanged.
* `Dispose` now calls `_poller.Stop()`, joins the poller thread with a 5-second timeout (logging a warning on timeout), and disposes `_outbound`, `_actor`, and `_poller` in order. Existing socket-close error suppression (Win32 `10035` / `10054`) is preserved.
* Add unit and integration tests covering the new poller lifecycle, outbound queue draining, and shutdown timing.

```

One blank line above the heading, one blank line below the final bullet (before the `### 0.3.0 2026-04-10` heading), matching the visual spacing the existing file uses between release sections.

Substitute `$TODAY` literally with the ISO date (e.g. `2026-04-14`). Do NOT leave `$TODAY` or `2026-04-XX` in the file.

Do not commit yet — stage only.
  </action>
  <verify>
```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# 1. The new heading exists with a real ISO date (no XX, no $TODAY)
grep -E '^### 0\.4\.0 [0-9]{4}-[0-9]{2}-[0-9]{2}$' CHANGELOG.md
# Expect: one match, e.g. ### 0.4.0 2026-04-14

# 2. Placeholders are gone
grep -c '0\.4\.0 2026-04-XX' CHANGELOG.md
# Expect: 0
grep -c '\$TODAY' CHANGELOG.md
# Expect: 0

# 3. 0.4.0 appears above 0.3.0 in file order
awk '/^### 0\.4\.0/ {found4=NR} /^### 0\.3\.0/ {found3=NR} END {if (found4 && found3 && found4 < found3) print "ORDER OK"; else print "ORDER BAD"}' CHANGELOG.md
# Expect: ORDER OK

# 4. Issue #6 link is present in the new entry
sed -n '/^### 0\.4\.0/,/^### 0\.3\.0/p' CHANGELOG.md | grep -c 'issues/6'
# Expect: 1
```
  </verify>
  <done>
`CHANGELOG.md` has a new `### 0.4.0 YYYY-MM-DD` heading above `### 0.3.0 2026-04-10`, with four bullets copied verbatim from DOCUMENTATION-1.md, today's actual ISO date in the heading, no placeholder strings, and the issue #6 link intact. Staged but not committed.
  </done>
</task>

<task id="3" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/ITaskSchedulerJobCountSync.cs, /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerJobCountSync.cs" tdd="false">
  <action>
Land ISSUE-028 (deferred from Phase 1): add a `<remarks>` XML doc block to `Start()` on BOTH the interface and the implementation. The `<remarks>` content must be byte-identical between the two files so IDE tooltips match across the abstraction (DOCUMENTATION-1.md line 37).

The existing `<summary>Starts this instance.</summary>` is unchanged — `<remarks>` is additive below it.

1. Edit `Source/ITaskSchedulerJobCountSync.cs` lines 52–55. Current shape:
```csharp
        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start();
```
Change to:
```csharp
        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <remarks>
        /// Non-blocking. The caller thread performs the host-address handshake,
        /// the ~1.1s beacon grace sleep, and the initial <c>BroadCast</c>
        /// synchronously, then returns as soon as the dedicated background
        /// poller thread is running. Callers must not rely on <see cref="Start"/>
        /// blocking for the lifetime of the poller.
        /// </remarks>
        void Start();
```

2. Edit `Source/TaskSchedulerJobCountSync.cs` lines 103–106. Current shape:
```csharp
        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
```
Change to the same `<remarks>` block, byte-identical to the interface version:
```csharp
        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <remarks>
        /// Non-blocking. The caller thread performs the host-address handshake,
        /// the ~1.1s beacon grace sleep, and the initial <c>BroadCast</c>
        /// synchronously, then returns as soon as the dedicated background
        /// poller thread is running. Callers must not rely on <see cref="Start"/>
        /// blocking for the lifetime of the poller.
        /// </remarks>
        public void Start()
```

The `<see cref="Start"/>` reference resolves correctly in both contexts: on the interface it resolves to `ITaskSchedulerJobCountSync.Start`, on the impl it resolves to `TaskSchedulerJobCountSync.Start`. Both are what the reader wants.

3. Confirm the clean Release build compiles with zero warnings (TreatWarningsAsErrors is on). Any malformed XML doc will surface as CS1570 / CS1574 and fail the build.

4. Commit all five files staged across Tasks 1, 2, and 3 as a single release commit. Mirror the subject/body template captured in `PLAN-1.1.md` Task 2 Result. Default template if the 0.3.0 commit message pattern is "Release X.Y.Z: <subject>":

```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

git add Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj \
        README.md \
        CHANGELOG.md \
        Source/ITaskSchedulerJobCountSync.cs \
        Source/TaskSchedulerJobCountSync.cs

git commit -m "Release 0.4.0: lock-contention fix for TaskSchedulerJobCountSync

Eliminates the _lockSocket + polling pattern in TaskSchedulerJobCountSync
by driving NetMQActor with a NetMQPoller and routing outbound
IncreaseCurrentTaskCount / DecreaseCurrentTaskCount through a
NetMQQueue<SetCountMsg>. All socket I/O now runs on a dedicated
background thread, owned by the poller. GetCurrentTaskCount is now
lock-free via Interlocked.Read. Start() is non-blocking; Dispose
stops the poller with a 5s timeout before disposing the actor.

Closes issue #6. Full details in CHANGELOG.md and the <remarks> block
on Start() on ITaskSchedulerJobCountSync and TaskSchedulerJobCountSync."
```

If `PLAN-1.1.md` Task 2 Result captured a different subject-line convention (e.g. the 0.3.0 commit used a different prefix), use that template instead. The builder MUST consult PLAN-1.1 Task 2 Result before finalizing the commit message.

5. Push master to origin (but NOT the tag — the tag goes in PLAN-5.1 after the local pack verification in PLAN-4.1):

```bash
git push origin master
```

If PLAN-2.2 ran, the same `git push origin master` pushes both the workflow-fix commit and the release commit in one go — that is intentional. Pushing them separately would leave master in a momentarily broken state for CI.
  </action>
  <verify>
```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# 1. Both files have the <remarks> block
grep -c '<remarks>' Source/ITaskSchedulerJobCountSync.cs
# Expect: at least 1
grep -c '<remarks>' Source/TaskSchedulerJobCountSync.cs
# Expect: at least 1

# 2. The <remarks> content is byte-identical between the two files
diff <(sed -n '/<remarks>/,/<\/remarks>/p' Source/ITaskSchedulerJobCountSync.cs) \
     <(sed -n '/<remarks>/,/<\/remarks>/p' Source/TaskSchedulerJobCountSync.cs)
# Expect: no output (files match on the remarks block)

# 3. Clean Release build with CI=true — zero errors, zero warnings
rm -rf Source/bin Source/obj
dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Release -p:CI=true
# Expect: Build succeeded. 0 Error(s). 0 Warning(s).

# 4. Tests still green on the release commit
dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj" -c Release --no-build
# Expect: all tests pass

# 5. Release commit exists on master with the right files
git log -1 --stat master | grep -E '(csproj|README\.md|CHANGELOG\.md|ITaskSchedulerJobCountSync\.cs|TaskSchedulerJobCountSync\.cs)' | wc -l
# Expect: 5 (one line per file in the diff stat)

# 6. origin/master is up to date with local master
git fetch origin
git rev-parse master
git rev-parse origin/master
# Expect: same SHA
```
  </verify>
  <done>
All six verify steps pass. Master on origin now contains the merge commit, optionally the workflow-fix commit from PLAN-2.2, and the release commit. The working tree is clean. No tag has been created yet — the tag is deliberately deferred to PLAN-5.1 until after PLAN-4.1's local pack verification. CI build job (not publish) will run automatically on the master push; it can run in parallel with PLAN-4.1.
  </done>
</task>
