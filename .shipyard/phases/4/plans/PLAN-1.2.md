---
phase: phase-4
plan: 1.2
wave: 1
dependencies: []
must_haves:
  - New Jenkins parallel stage runs the TaskScheduler Distributed integration tests on the Docker agent
  - Stage appended at END of parallel block (not alphabetical) to preserve stagger pattern for stages 1–13
  - Sleep offset = 65 seconds (stage 14 = (14-1)*5)
  - No Coverlet / Codecov wiring from the new stage (no --settings, --collect, --results-directory, or stash)
  - Coverage Report stage unchanged
files_touched:
  - Jenkinsfile
tdd: false
risk: medium
---

# PLAN-1.2: Jenkinsfile parallel stage for TaskScheduler Distributed integration tests

## Context

Phase 3 shipped `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests`, a net10.0-only integration test project. Phase 4 wires it into both CI surfaces. This plan handles the Jenkins surface only and is disjoint from PLAN-1.1 (which edits `ci.yml`), so the two run in parallel in Wave 1.

The risk tag is `medium` (not `low`) because of the open UDP-multicast question from CONTEXT-4 decision #1 — NetMQ beacon discovery may or may not work on the Docker bridge network. Per the locked optimistic approach, this plan does NOT pre-emptively skip or isolate; if the feature-branch run fails, the follow-up issue + skip mechanism is a separate PR.

## Locked Decisions (see `.shipyard/phases/4/CONTEXT-4.md`)

- **Decision #1 — optimistic UDP.** No `[TestCategory]`, no `--network=host`, no beacon-skip env var, no filter. Ship and see.
- **Decision #3 — no Coverlet in this stage.** The new stage is a plain `dotnet test` gate. It does NOT produce coverage data, does NOT stash anything, and does NOT feed the Coverage Report stage. Decision #3 was corrected on 2026-04-15: use `-c Debug` (matching the 13 existing stages), NOT `-c Release -p:CI=true`. `-p:CI=true` is a NuGet-packaging flag for deterministic Source Link, irrelevant to test execution.
- **Decision #4 — append at END, preserve stagger.** New stage is the 14th. Stagger formula `(n-1)*5` → `sleep(time: 65, unit: 'SECONDS')`. Do NOT renumber or shift any existing stage's sleep value.
- **Feature branch.** Assume the working branch is not `master`. Commit message should be descriptive but not reference a specific branch name.

## Verified facts from research

- Jenkinsfile has 13 parallel stages in a single `parallel { ... }` block. Each stage has an explicit `sleep(time: N, unit: 'SECONDS')` call as its first `steps` action — there is NO loop or helper function, so we must write the sleep literally.
- Confirmed stagger table (grep'd live, 2026-04-15):
  - Stage 1 SqlServer = 0s (line 72)
  - Stage 2 SqlServer Linq = 5s (line 91)
  - ... every +5s per stage ...
  - Stage 13 Dashboard = 60s (line 276)
  - Stage 14 TaskScheduler Distributed = **65s** (new)
- `stage('Dashboard')` spans lines 273–297. Its closing `}` is on line 297. The `parallel` block's closing `}` is on line 298. New stage must insert between lines 297 and 298.
- Stage indentation: `stage('X') {` starts at 16 spaces of indent (4 indent levels deep). The inner `steps { ... }` block uses 20 spaces, and the `sh ...` / `sleep` calls use 24 spaces. Match this exactly.
- `Coverage Report` stage (starts line 301) `unstash`es 13 named stashes. The new stage contributes NO stash, so the Coverage Report stage needs ZERO changes — leave it alone.
- The Memory stage at lines 243–256 is the closest analog to what we want (no `withCredentials` block, no connection string setup), EXCEPT that it also includes `--settings`, `--collect`, `--results-directory`, and a `stash` — all of which MUST be omitted from the new stage.
- Phase 3 project has no JobScheduler-named tests, so do NOT add `--filter "FullyQualifiedName!~JobScheduler"`.
- Phase 3 project has no flaky-retry needs identified, and Memory/SQLite/LiteDB stages do not use `--retry-failed-tests`. Do NOT add `--retry-failed-tests`.

<task id="1" files="Jenkinsfile" tdd="false">
  <action>
Insert a new parallel stage `stage('TaskScheduler Distributed') { ... }` at the END of the `parallel` block inside `stage('Integration Tests')`. The insertion point is between the closing `}` of `stage('Dashboard')` (currently line 297) and the closing `}` of the `parallel` block (currently line 298).

Use the exact indentation of the existing Memory/Dashboard stages:
- `stage('TaskScheduler Distributed') {` at column 17 (16 spaces of indent)
- `agent { label 'docker' }` at column 21 (20 spaces)
- `steps {` at column 21 (20 spaces)
- `sleep(...)`, `sh '...'`, `sh '''...'''` body calls at column 25 (24 spaces)
- Body of triple-quoted `sh '''` block at column 29 (28 spaces)
- Closing `'''` and closing braces de-indent to match their openers.

Insert literally (preserve whitespace exactly; leading blank line separates from the Dashboard stage):

```groovy

                stage('TaskScheduler Distributed') {
                    agent { label 'docker' }
                    steps {
                        sleep(time: 65, unit: 'SECONDS')
                        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                        sh '''
                            dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" \
                                -f net10.0 -c Debug
                        '''
                    }
                }
```

**DO NOT include any of the following inside the new stage body** (Decision #3 + optimistic UDP):
- `--settings Source/coverage.runsettings`
- `--collect:"XPlat Code Coverage"` or any `--collect` flag
- `--results-directory coverage/int-*`
- `stash includes: 'coverage/**/*.xml', name: '...'`
- Any `unstash` / artifact upload / `codecov` command
- A `withCredentials([...]) { ... }` block — this project needs no connection string
- A `--filter` flag (no JobScheduler tests exist in the project)
- `--retry-failed-tests` (Memory / SQLite / LiteDB stages don't use it; neither do we)
- `-c Release` or `-p:CI=true` (those are for NuGet packaging; use `-c Debug` to match the 13 existing stages and avoid a redundant double-build)
- `--no-build` (the `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` line above already builds the solution; all other integration stages omit `--no-build` on the test invocation, match that pattern — the test invocation will no-op the build because the solution was just built)
- A `[TestCategory]` filter or `BEACON_SKIP=true` environment variable — the optimistic decision is to ship without a skip mechanism. If beacon discovery fails on the feature-branch run, that is handled in a follow-up PR, NOT in this plan.

**DO NOT touch**:
- Any existing stage's `sleep(time: N, unit: 'SECONDS')` value — all 13 must stay at 0/5/10/.../60 unchanged.
- The `Coverage Report` stage (starts at line 301). It unstashes 13 named stashes; since the new stage produces no stash, no change is required.
- The `post { ... }` block, the `environment { ... }` block, or any other top-level Jenkinsfile structure.

Commit the change with message `shipyard(phase-4): add TaskScheduler Distributed parallel stage to Jenkinsfile`.
  </action>
  <verify>
# 1. The new stage exists and appears exactly once
grep -c "stage('TaskScheduler Distributed')" Jenkinsfile
# Expected: 1

# 2. The project csproj path appears exactly once in the Jenkinsfile
grep -c "TaskScheduling.Distributed.TaskScheduler.Integration.Tests" Jenkinsfile
# Expected: 1

# 3. Stagger count increased by exactly 1 (from 13 to 14)
grep -c "sleep(time: " Jenkinsfile
# Expected: 14

# 4. The new 65s offset was added exactly once and the 13 existing offsets are unchanged
grep -n "sleep(time: " Jenkinsfile
# Expected output (exact values, in file order):
#   <line>:                        sleep(time: 0, unit: 'SECONDS')
#   <line>:                        sleep(time: 5, unit: 'SECONDS')
#   <line>:                        sleep(time: 10, unit: 'SECONDS')
#   <line>:                        sleep(time: 15, unit: 'SECONDS')
#   <line>:                        sleep(time: 20, unit: 'SECONDS')
#   <line>:                        sleep(time: 25, unit: 'SECONDS')
#   <line>:                        sleep(time: 30, unit: 'SECONDS')
#   <line>:                        sleep(time: 35, unit: 'SECONDS')
#   <line>:                        sleep(time: 40, unit: 'SECONDS')
#   <line>:                        sleep(time: 45, unit: 'SECONDS')
#   <line>:                        sleep(time: 50, unit: 'SECONDS')
#   <line>:                        sleep(time: 55, unit: 'SECONDS')
#   <line>:                        sleep(time: 60, unit: 'SECONDS')
#   <line>:                        sleep(time: 65, unit: 'SECONDS')

# 5. Decision #3: no coverage flags were added inside the new stage body.
# Extract just the new stage body and confirm it has zero coverage-related tokens.
awk "/stage\('TaskScheduler Distributed'\)/,/^                \}$/" Jenkinsfile | grep -cE "(XPlat Code Coverage|coverage.runsettings|--collect|--results-directory|coverage/int-|stash )"
# Expected: 0

# 6. Decision #3 (belt-and-braces): the new stage does NOT use Release or CI=true
awk "/stage\('TaskScheduler Distributed'\)/,/^                \}$/" Jenkinsfile | grep -cE "(-c Release|-p:CI=true)"
# Expected: 0

# 7. Decision #1: no beacon skip / network=host / TestCategory
awk "/stage\('TaskScheduler Distributed'\)/,/^                \}$/" Jenkinsfile | grep -cE "(--network=host|BEACON_SKIP|TestCategory|FullyQualifiedName!~NodeDiscovery)"
# Expected: 0

# 8. Coverage Report stage was not touched — unstash count still 13
grep -c "^                unstash " Jenkinsfile
# Expected: 13 (unchanged from before this change)

# 9. Confirm the Dashboard stage sleep is still 60s (no renumbering happened)
grep -B1 "stage('Dashboard')" Jenkinsfile >/dev/null && grep -A3 "stage('Dashboard')" Jenkinsfile | grep "sleep(time: 60"
# Expected: a matching line with sleep(time: 60, unit: 'SECONDS')
  </verify>
  <done>
- `Jenkinsfile` contains exactly one new `stage('TaskScheduler Distributed')` block, placed as the last stage inside the `parallel` block (after `stage('Dashboard')`, before the closing `}` of `parallel`).
- The new stage's first `steps` action is `sleep(time: 65, unit: 'SECONDS')` and the total `sleep(time: ` count in the file is 14.
- All 13 existing stagger offsets (0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60) are unchanged — verified by the `grep -n "sleep(time: "` output above.
- The new stage body is the exact literal block from the action section (sleep → build → sh-triple-quoted dotnet test), with NO coverage flags, NO `-c Release`, NO `-p:CI=true`, NO `stash`, NO `withCredentials`, NO `--filter`, NO `--retry-failed-tests`, NO beacon-skip.
- `Coverage Report` stage is unchanged (still unstashes 13 named stashes).
- Single commit on the working branch (not master) with the message specified above.
- Offline Jenkinsfile linting is best-effort only (no Jenkins CLI locally). Final lint happens when the feature branch is pushed and Jenkins runs the Multibranch Pipeline.
  </done>
</task>
