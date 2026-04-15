# Research: Phase 4 CI Wiring

## 1. GitHub Actions (ci.yml)

### Current Shape

File: `/mnt/f/git/dotnetworkqueue/.github/workflows/ci.yml`

**Single job: `build-and-test`** on `ubuntu-latest`. Triggers on push to `master`/`*.*.x` and PRs to `master`.

Steps in order:
1. `actions/checkout@v4`
2. `actions/setup-dotnet@v4` — installs **both** `8.0.x` and `10.0.x`
3. `dotnet restore "Source/DotNetWorkQueue.sln"`
4. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug --no-restore`
5. Individual `dotnet test` steps — one per project, `--no-build -c Debug` (NO `-f net10.0` flag specified)

**Current test steps (lines 34–66):**
- Unit Tests - Core
- Unit Tests - RelationalDatabase
- Unit Tests - PostgreSQL
- Unit Tests - Redis
- Unit Tests - SQLite
- Unit Tests - LiteDb
- Unit Tests - SqlServer
- Unit Tests - Dashboard.Api
- Unit Tests - Dashboard.Client
- Unit Tests - Dashboard.Ui
- Unit Tests - Memory

**Critical finding:** All 11 steps are **unit tests only** — no Memory integration test (`DotNetWorkQueue.Transport.Memory.Integration.Tests`) is present in `ci.yml`. The ROADMAP and CONTEXT-4 both assume "the existing unit+integration job that runs Memory-transport-only integration tests" exists — **it does not**. The comment on line 32 says "net10.0 integration tests run on Jenkins", meaning integration tests run only on Jenkins, not GitHub Actions.

**Net framework targeting:** The build targets both `net10.0` and `net8.0` (setup-dotnet installs both SDKs). The `dotnet test` steps have no `-f` flag, so they run both frameworks. However the Phase 3 project targets `net10.0` only, so it will build and run correctly without a framework flag.

**No connection-string setup step exists** in ci.yml. Memory transport requires no connection string.

**Stale documentation alert:** `docs/jenkins-setup.md` (lines 207-215) contains a table saying GitHub Actions targets `net48` / Windows. This is **incorrect** — the actual `ci.yml` targets Linux/net10.0+net8.0 unit tests. The jenkins-setup.md was not updated after the net48 removal milestone.

### Exact Insertion Point

The new step should be appended **after line 66** (the final "Unit Tests - Memory" step), as a new named step:

```yaml
      - name: Integration Tests - TaskScheduler Distributed
        run: dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" -c Debug
```

No `--no-build` flag because the project is already built by the earlier `dotnet build` step.
No `-f net10.0` flag needed (project is net10.0-only so it will only run that TFM).
No connection string setup — Memory transport uses in-process "none" connection.

**DECISION REQUIRED — insertion context:** CONTEXT-4 decision #5 says "add to the existing unit+integration job that runs Memory-transport-only integration tests." Since NO such integration tests exist in ci.yml, the architect must treat this as "append to the existing `build-and-test` job after the last unit test step." This does not change what needs to be done — it just means the architect should not search for a Memory integration test step to use as a template; the new step is the FIRST integration test in ci.yml.

### Gotchas

- The build step uses `--no-restore`, and the new test project is in the solution, so it will be built automatically. The `dotnet test` call can use `--no-build` only if the build step also built it. Since `dotnet build "Source/DotNetWorkQueue.sln"` builds the whole solution, `--no-build` IS safe here and matches the pattern of all other test steps.
- No `--no-restore` on the test step — consistent with all existing test steps which pass `--no-build` without `--no-restore`.
- The `coverlet.collector` PackageReference IS present in the Phase 3 `.csproj`. Since CONTEXT-4 decision #3 only excludes Coverlet from Jenkins, and the GitHub Actions job does NOT use Coverlet/coverage collection at all (no `--collect` or `--settings` flags anywhere in ci.yml), this is a non-issue.

---

## 2. Jenkinsfile

### Stagger Pattern (EXACT mechanism)

File: `/mnt/f/git/dotnetworkqueue/Jenkinsfile`

The stagger is implemented as **an explicit `sleep(time: N, unit: 'SECONDS')` call at the start of each parallel stage's `steps` block**. There is no loop, no index map, no helper function.

Confirmed sequence (lines 72, 91, 109, 129, 148, 166, 185, 200, 215, 230, 246, 261, 276):

| Stage | Sleep (seconds) |
|-------|----------------|
| SqlServer | 0 |
| SqlServer Linq | 5 |
| PostgreSQL | 10 |
| PostgreSQL Linq | 15 |
| Redis | 20 |
| Redis Linq | 25 |
| SQLite | 30 |
| SQLite Linq | 35 |
| LiteDB | 40 |
| LiteDB Linq | 45 |
| Memory | 50 |
| Memory Linq | 55 |
| Dashboard | 60 |

Formula confirmed: `sleep = (stageIndex - 1) * 5`. Stage 14 = `(14-1) * 5 = 65` seconds.

### Existing Stage Template (Memory stage — closest analogue)

Lines 243–256:

```groovy
stage('Memory') {
    agent { label 'docker' }
    steps {
        sleep(time: 50, unit: 'SECONDS')
        sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
        sh '''
            dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" \
                -f net10.0 -c Debug \
                --filter "FullyQualifiedName!~JobScheduler" \
                --settings Source/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory coverage/int-memory
        '''
        stash includes: 'coverage/**/*.xml', name: 'cov-memory'
    }
}
```

The new stage must **omit** the `--settings`, `--collect`, `--results-directory`, and `stash` lines (per CONTEXT-4 decision #3).

The new stage uses `-c Release -p:CI=true` per the CONTEXT-4 decision #3 text ("plain `dotnet test …csproj -c Release -p:CI=true`"). Note: all other integration test stages use `-c Debug`. The CONTEXT-4 decision explicitly specifies `Release` + `CI=true`. Architect should follow CONTEXT-4 over the existing pattern.

**However**, there is a tension: the `dotnet build` line at the start of every stage uses `-c Debug`, producing Debug binaries. If the stage then runs `dotnet test -c Release`, it will trigger a separate Release build on the agent. This is likely intentional in CONTEXT-4 (the stage runs as a gate, not a coverage contributor, and Release builds with CI=true are the canonical "does it actually work" check). The architect should preserve this as specified.

### Coverlet + Codecov Wiring (what to OMIT per decision #3)

What other stages DO (must NOT be included in the new stage):
1. `--settings Source/coverage.runsettings` — points to `/mnt/f/git/dotnetworkqueue/Source/coverage.runsettings`
2. `--collect:"XPlat Code Coverage"` — triggers Coverlet data collector
3. `--results-directory coverage/int-<name>` — output directory for `.xml` files
4. `stash includes: 'coverage/**/*.xml', name: 'cov-<name>'` — stashes the cobertura XML for the Coverage Report stage

The Coverage Report stage (lines 301–351) explicitly `unstash`es 14 named stashes by name. Since the new stage produces no stash, the Coverage Report stage needs NO changes — it will still find and merge all 14 existing named stashes. Codecov upload uses `coverage/report/Cobertura.xml` (the merged report from ReportGenerator), not individual stage files.

**coverlet.runsettings location:** `Source/coverage.runsettings` (confirmed at `/mnt/f/git/dotnetworkqueue/Source/coverage.runsettings`). Excludes test projects from instrumentation via `[*.Tests]*,[*.IntegrationTests]*,[*.IntegrationTests.*]*,[DotNetWorkQueue.IntegrationTests.Shared]*`. The Phase 3 project name `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests` would match `[*.IntegrationTests]*` — so even if coverage were accidentally wired, the test project itself would be excluded from instrumentation. This doesn't change decision #3 but confirms no coverage data loss risk.

### Exact Insertion Point for New Stage

Insert the new stage block **after line 297** (closing `}` of the `Dashboard` stage) and **before line 298** (closing `}` of the `parallel` block).

Surrounding context:
```
Line 294:            stash includes: 'coverage/**/*.xml', name: 'cov-dashboard'
Line 295:        }
Line 296:    }
Line 297:    }                    <-- closing brace of stage('Dashboard')
Line 298:}                        <-- closing brace of parallel block
Line 299:}                        <-- closing brace of stage('Integration Tests')
```

New stage inserts between lines 297 and 298.

### 14th-Stage Stagger Offset

`sleep(time: 65, unit: 'SECONDS')` — formula `(14-1) * 5 = 65`. Confirmed.

### Gotchas

1. **Build step in each stage uses `-c Debug`.** If CONTEXT-4's `-c Release -p:CI=true` is used for `dotnet test`, the stage will build twice (once via `dotnet build -c Debug` at the top, then again implicitly via `dotnet test -c Release`). This is a minor inefficiency but not a correctness issue. Alternative: match other stages with `-c Debug` and drop `-p:CI=true` — but the architect should follow CONTEXT-4 explicitly. This tension is noted for the architect to decide.

2. **`--filter "FullyQualifiedName!~JobScheduler"` on other stages.** The Phase 3 project has no `JobScheduler`-named tests, so this filter is not needed. Omitting it is correct.

3. **`--retry-failed-tests 1` on database-transport stages.** Memory/SQLite/LiteDB/Dashboard stages do NOT use `--retry-failed-tests`. The new stage should not include it either.

4. **`docs/jenkins-setup.md` says 13 parallel stages** (line 66, line 193-196). After Phase 4 ships, this will need updating to 14. Out-of-scope for Phase 4 per CONTEXT-4, but the architect should add a doc update task or note it.

---

## 3. Supporting Infrastructure

### coverlet.runsettings location

`/mnt/f/git/dotnetworkqueue/Source/coverage.runsettings` — confirmed. Referenced in Jenkinsfile as `Source/coverage.runsettings` (relative to workspace root). Not referenced by GitHub Actions at all.

### docs/jenkins-setup.md summary

File: `/mnt/f/git/dotnetworkqueue/docs/jenkins-setup.md`

Relevant facts:
- Docker agent label: `docker` (confirmed — every Jenkinsfile stage uses `agent { label 'docker' }`)
- Docker image: `dotnetworkqueue-ci:latest` (built locally on each host from `docker/Dockerfile`)
- Pull strategy: **Never pull** — image must be pre-built on the Docker host before a new project's dependencies are available. No action needed for Phase 4 since the new project's NuGet package (`DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler 0.4.0`) will be pulled from nuget.org at restore time, not baked into the image.
- **Stale documentation:** The CI split table (lines 205-215) says GitHub Actions targets `net48`/Windows. This is incorrect for the current state. Not a blocker for Phase 4 but should be flagged.
- Jenkins job is a **Multibranch Pipeline** — builds `master` and every open PR automatically. The feature branch `phase-4-ci-wiring` will trigger a Jenkins build when pushed.

### Jenkinsfile pre-push linting

No pre-push hook, no `Jenkinsfile.linter.sh`, no lint script found. CONTEXT-4 confirms this: "the repo has no Jenkins CLI configured for local syntax validation; the feature-branch push IS the lint."

---

## 4. Risk Assessment

### UDP Multicast Visibility in Docker

**Risk: UNKNOWN until tested.** The Jenkinsfile has zero network-mode configuration: no `--network=host`, no `cap_add`, no `sysctl`. The Docker agents run containers in the default bridge network. NetMQ beacon discovery uses UDP multicast (224.0.0.1:5670 by default). UDP multicast between containers on the same bridge network is typically blocked on Docker's default bridge.

**What this means for the new stage:** `NodeDiscoveryTests` in the Phase 3 project spin up two in-process scheduler nodes that use NetMQ beacon. On GitHub Actions (`ubuntu-latest`, not Docker-isolated), UDP multicast on loopback works. On Jenkins Docker agents (bridge network), it may fail.

**Per CONTEXT-4 decision #1:** Ship optimistic. If it fails on the feature-branch run, open an issue and add a skip mechanism.

**Mitigation already in place:** The feature-branch-first validation (decision #2) surfaces this before master merge.

**Grep result for network flags in Jenkinsfile:** No matches for `network`, `--net`, `cap_add`, `sysctl`, `privileged` — confirmed no special Docker networking is configured.

### Other Risks

| Risk | Likelihood | Impact | Notes |
|------|-----------|--------|-------|
| UDP multicast blocked in Docker bridge | Medium | Medium | Feature-branch run will reveal. Follow-up issue path pre-approved. |
| CONTEXT-4 specifies `-c Release -p:CI=true` but all other stages use `-c Debug` | Low | Low | Creates a double-build in the stage. Architect must decide: follow CONTEXT-4 exactly or align with existing pattern. |
| `docs/jenkins-setup.md` still says "13 parallel stages" | Low | Low | Stale doc, not a CI blocker. Consider a doc update in Phase 4 or a follow-up. |
| `ci.yml` `docs/jenkins-setup.md` CI split table is stale (net48/Windows claim) | Low | Low | Misleading for future maintainers. Out of scope for Phase 4. |
| Phase 3 project has `coverlet.collector` PackageReference | None | None | The GitHub Actions job doesn't invoke coverage collection, so this is inert there. Jenkins stage explicitly omits coverage flags. |
| No `[TestCategory]` decorators in Phase 3 tests | None | None | Confirmed by grep — no TestCategory attributes exist. No filter needed. |

---

## 5. Summary and Recommendations for the Architect

### What the architect needs to produce

**File 1: `.github/workflows/ci.yml`**
- Add one new step after line 66 (after "Unit Tests - Memory"):
  ```yaml
        - name: Integration Tests - TaskScheduler Distributed
          run: dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" -c Debug --no-build
  ```
  Note: `--no-build` is correct because the prior `dotnet build "Source/DotNetWorkQueue.sln"` step already builds the whole solution. No `-f` flag needed (net10.0-only project). No connection string setup needed.

**File 2: `Jenkinsfile`**
- Add one new `stage` block inside the `parallel` block, after the closing `}` of `stage('Dashboard')` (after line 297, before line 298):
  ```groovy
              stage('TaskScheduler Distributed') {
                  agent { label 'docker' }
                  steps {
                      sleep(time: 65, unit: 'SECONDS')
                      sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
                      sh '''
                          dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" \
                              -f net10.0 -c Release -p:CI=true
                      '''
                  }
              }
  ```
  No `--settings`, no `--collect`, no `--results-directory`, no `stash` (per CONTEXT-4 decision #3).
  No `withCredentials` block (no connection string needed).
  No `--filter` (no JobScheduler tests to exclude).
  No `--retry-failed-tests` (matches Memory/SQLite/LiteDB pattern).

  **Architect decision point:** The `dotnet build -c Debug` at the top of each stage is the existing pattern. The `dotnet test -c Release -p:CI=true` is what CONTEXT-4 prescribes. This means the stage builds Debug first (wasted work) then runs Release. Alternative: skip the `dotnet build` line and just run `dotnet test -c Release -p:CI=true` (which will build as part of test if needed). The architect should make an explicit choice and document it.

**Coverage Report stage:** No changes needed. The stage unstashes 14 named stashes (none from the new stage) and merges them. The new stage contributes no stash, which is correct per decision #3.

### Potential plan shape

Single wave, 2 tasks:
1. Edit `ci.yml` — append one test step
2. Edit `Jenkinsfile` — append one parallel stage

Both edits are independent and can be planned in the same wave. The Jenkinsfile edit is the only one with any structural complexity (indentation, closing brace placement).

### Blocking unknowns that need architect decision

1. **`-c Release -p:CI=true` vs `-c Debug` for the Jenkins `dotnet test` invocation.** CONTEXT-4 says `Release`; all other integration stages use `Debug`. The architect must pick one and note it. Research recommendation: use `-c Debug` to align with the 13 existing stages and avoid a redundant Release build, and drop `-p:CI=true` (that flag is for NuGet packing, not test runs). But CONTEXT-4 is explicit — follow it unless the user overrides.

2. **`--no-build` in `ci.yml`.** The prior `dotnet build` step builds the full solution, so `--no-build` is safe and matches all other test steps. This is not blocking but should be confirmed.

---

## Sources

1. `/mnt/f/git/dotnetworkqueue/.github/workflows/ci.yml` — read in full
2. `/mnt/f/git/dotnetworkqueue/Jenkinsfile` — read in full (363 lines)
3. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/4/CONTEXT-4.md` — locked decisions
4. `/mnt/f/git/dotnetworkqueue/.shipyard/ROADMAP.md` — Phase 4 scope (lines 258-320)
5. `/mnt/f/git/dotnetworkqueue/docs/jenkins-setup.md` — Jenkins architecture reference
6. `/mnt/f/git/dotnetworkqueue/Source/coverage.runsettings` — Coverlet configuration
7. `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj` — Phase 3 project file

## Uncertainty Flags

- **Decision Required (architect):** `-c Release -p:CI=true` vs `-c Debug` for the Jenkins stage's `dotnet test` invocation. CONTEXT-4 specifies Release. Existing pattern is Debug. Both work; the choice affects whether the stage runs a redundant build.
- **Stale doc (not blocking):** `docs/jenkins-setup.md` CI split table claims GitHub Actions targets `net48`/Windows. The actual `ci.yml` targets Linux/net10.0+net8.0. Out of scope for Phase 4 but should be corrected in a follow-up.
- **CONTEXT-4 assumption mismatch (not blocking):** CONTEXT-4 decision #5 assumes Memory integration tests already run in GitHub Actions ("the existing unit+integration job that runs Memory-transport-only integration tests"). They do not — GitHub Actions runs unit tests only. The Phase 3 project will be the FIRST integration test in `ci.yml`. This does not change what to do but the architect should be aware the "existing" step being referenced does not exist.
