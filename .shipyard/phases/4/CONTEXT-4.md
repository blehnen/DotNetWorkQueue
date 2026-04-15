# Phase 4 Context: Design Decisions

**Phase:** 4
**Name:** CI Wiring (Jenkins + GitHub Actions)
**Captured:** 2026-04-15
**Mode:** Discussion capture (pre-research)

Phase 4 wires the new `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests` project (shipped in Phase 3) into the two CI surfaces: Jenkins (13 parallel Docker-agent stages + Coverlet + Codecov) and GitHub Actions (`.github/workflows/ci.yml`, net10.0 unit+integration job on ubuntu-latest).

## Locked Decisions

### 1. UDP multicast risk — optimistic approach

**Decision:** Ship the new Jenkins stage without a pre-emptive skip mechanism. If NetMQ beacon discovery fails on the Docker agent, open a follow-up issue and add the skip/fallback in a second PR.

**Why:** Phase 3's `NodeDiscoveryTests` rely on NetMQ UDP beacon discovery to simulate two scheduler nodes sharing a port. Jenkins Docker agents may or may not allow UDP multicast — unknown until tested. Three options were considered:

1. **Optimistic** (chosen) — ship and see. Minimal Jenkinsfile changes; if beacon works, we're done. If it fails on the first feature-branch run, we open an issue and add a skip mechanism.
2. **Pre-emptive `[TestCategory("BeaconRequired")]` + env-var skip** — defensive but pays a CI runtime cost every build and clutters the test class with CI-specific decoration before we know it's needed.
3. **Docker `--network=host`** — clean solution but introduces a one-off stage pattern that differs from the other 13 stages. Harder to audit, and the Jenkins admin (the user) would have to vouch that `--network=host` is safe on the shared agent.

Optimistic wins because the cost of a follow-up PR is low (Phase 4 is already small) and the other two options front-load complexity we don't know is needed. The ROADMAP already anticipates this: success criterion #5 says "Any beacon-skip decision is documented in a follow-up issue link in the Jenkinsfile comment."

**How to apply:** Architect should produce a single-wave plan that adds the GitHub Actions entry AND the Jenkins stage in one shot, with NO skip mechanism, NO `--network=host`, and NO `[TestCategory]` annotation. If beacon-dependent tests fail on the feature-branch run, the build-time flow opens a follow-up issue and adds the skip in a separate patch.

### 2. Validation via feature branch first

**Decision:** Push Phase 4 changes to a feature branch, let Jenkins and GitHub Actions run on it, confirm green, then merge to master.

**Why:** Matches the ROADMAP success-criteria language ("run the stage on a feature branch and confirm `NodeDiscoveryTests` pass in the Docker agent") and matches the `/shipyard:worktree` pattern for CI changes. Direct-to-master is faster but the risk of breaking the stagger pattern or surfacing a beacon failure on master is not worth the time saved.

**How to apply:** Build-time flow should create a feature branch (e.g. `phase-4-ci-wiring`) via `/shipyard:worktree create` or the equivalent `git worktree` + `git checkout -b`. All Phase 4 commits land on that branch. Merge to master only after both CI surfaces are confirmed green. Architect does not need to plan the branch creation — that's build-time orchestration — but plans should assume the working branch is `phase-4-ci-wiring` (or similar) and NOT `master`.

### 3. Exclude the new test project from Codecov

**Decision:** Do NOT wire Coverlet into the new Jenkins stage. The new project runs in Jenkins via `dotnet test` but does NOT report coverage to Codecov.

**Why:** The new test project exists to test the external `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` 0.4.0 NuGet. Coverlet can't instrument the NuGet DLL (it's already compiled without coverage hooks), so coverage data from this stage would only attribute to the DNQ core DLLs it pulls in via ProjectReference (`DotNetWorkQueue`, `DotNetWorkQueue.Transport.Memory`, `DotNetWorkQueue.IntegrationTests.Shared`). Those DLLs are already covered by the other 13 stages that exercise the same core, so adding a 14th coverage report is noise — and Codecov will try to reconcile 14 partial reports into one, which introduces merge noise without improving signal.

**How to apply:** Architect should plan the Jenkins stage as a plain `dotnet test …csproj -c Debug` invocation without the `--collect "XPlat Code Coverage"` / `--settings coverlet.runsettings` flags that the other 13 stages use. Do NOT upload any `.trx` or `coverage.cobertura.xml` from this stage. The stage is a test-only gate.

**Note (corrected 2026-04-15 post-research):** An earlier draft of this doc specified `-c Release -p:CI=true` for the test invocation, but that's wrong on both counts:
1. `-p:CI=true` is a NuGet packaging flag (enables `ContinuousIntegrationBuild` for deterministic Source Link paths per CLAUDE.md), not a test-run flag. It only matters for `dotnet build -c Release -p:CI=true` when producing .nupkg files.
2. All 13 existing Jenkins integration test stages run `-c Debug` — Release builds are reserved for the packaging stages at the end of the pipeline, not for per-project test execution. Using Release here would double-build the test project and drift from the existing pattern.

The new stage uses `-c Debug` with no `-p:CI=true`, matching the other 13 stages one-for-one.

### 4. Jenkins stage placement — append, preserve stagger pattern

**Decision:** Append the new stage to the end of the Jenkins parallel block. Preserve the 5s staggered-startup pattern (stages 1–13 stagger 0s, 5s, 10s, …, 60s; new stage 14 staggers at 65s).

**Why:** Inserting alphabetically or adjacent to the Memory stage would shift every downstream stagger offset and make the diff huge. Appending is a one-line change that preserves the stagger pattern literally.

**How to apply:** Architect should plan the new stage as the last entry in the parallel block, with a `sleep 65` at the start of its body (matching the formula `(stageIndex - 1) * 5`). Verify the actual stagger formula in Jenkinsfile during research — if Jenkins uses a `for`-loop or an index map, the new entry may need to be registered differently.

### 5. GitHub Actions placement — add to existing unit+integration job

**Decision:** Add the new project to the existing unit+integration job in `.github/workflows/ci.yml` — whichever job currently runs the Memory-transport integration tests on `ubuntu-latest` / `net10.0`. No new job.

**Why:** CLAUDE.md states `.github/workflows/ci.yml` "runs net10.0 unit tests on ubuntu-latest for CI validation". Phase 3's test project is the only one in DNQ that binds NetMQ beacon ports; the rest of the Memory-transport tests are lightweight. GitHub Actions' `ubuntu-latest` agent has unrestricted UDP by default (unlike Jenkins Docker with bridged networking), so beacon discovery should Just Work there. Adding a new job would duplicate setup (checkout, restore, build) for no coverage benefit.

**How to apply:** Architect should plan a single `dotnet test` matrix entry (or test-project list addition) in the existing job's step(s) that run Memory integration tests. Research should identify the exact step — `ci.yml` may have multiple `dotnet test` invocations (one per project) or a single matrix strategy.

## Out-of-Scope for Phase 4

- **UDP multicast skip mechanism.** Only added if the first feature-branch run fails. Separate PR if needed.
- **Coverlet wiring for the new test project.** Explicitly excluded (decision #3).
- **New Jenkins agent / image.** Stage runs on the existing Docker agent label.
- **Changes to the stagger formula** for stages 1–13. Only the 14th stage gets a new stagger entry.
- **Pre-push local Jenkinsfile linting** beyond `cat Jenkinsfile`. The repo has no Jenkins CLI configured for local syntax validation; the feature-branch push IS the lint.
- **Adding the project to any `test.sh` / `run-tests.ps1` / other local helper scripts** if they exist. Scope is strictly CI surfaces.
- **Bumping versions, updating release notes, or touching Directory.Build.props.** None of those are Phase 4 concerns.

## Release-Hard Constraints

- **Preserve the 5s staggered-startup pattern** (ROADMAP success criterion #3). Verify via research that the pattern is still `(n-1)*5` seconds.
- **No existing stage regression** (ROADMAP success criterion #4). All previously-green stages stay green after the Jenkinsfile diff.
- **`net10.0` only** in the GitHub Actions job (Phase 3's project is `net10.0`-only, matches the existing unit+integration job).
- **No commit to `master` during build.** All Phase 4 commits land on the feature branch; the merge is a separate, explicit step after CI validation.

## Known Risks

1. **NetMQ beacon blocked in Docker.** If it fails on the feature-branch run, the build flow must open a follow-up issue, comment the skip mechanism in the Jenkinsfile, and revise the plan. This is pre-approved via decision #1.
2. **Jenkinsfile structure differs from assumption.** The stagger may be implemented as a map/loop, not inline sleeps. Research should confirm the exact shape before the architect commits to a line-level edit.
3. **`ci.yml` may have changed since CLAUDE.md was written.** The "unit+integration job runs net10.0" statement is from a snapshot; research should verify the current shape.
4. **`.github/workflows/ci.yml` may not currently include the Memory integration test project.** If it doesn't, the architect needs a decision: add both the Memory project AND the new Phase 3 project, or just the new one? The ROADMAP says "the existing unit+integration job that runs Memory-transport-only integration tests" — which implies Memory is already there. Research will confirm.

## Feature Branch Name (suggested)

`phase-4-ci-wiring` — short, descriptive, matches the Shipyard branch-naming convention used in prior phases. Architect can override if a different convention is in use.
