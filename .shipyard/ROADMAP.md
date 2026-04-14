# Roadmap: TaskScheduler Lock Contention Fix + Integration Tests

## Overview

Fix issue #6 in the sibling `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
repository (lock contention in `TaskSchedulerJobCountSync.ProcessMessages`),
release a new `0.4.0` NuGet package with deterministic Source Link, then add a
new integration test project in DotNetWorkQueue that references the published
package and proves distributed task scheduling works end-to-end.

The work spans two repositories in strict sequence: the fix and unit tests
ship first from the TaskScheduler repo, then the DotNetWorkQueue integration
tests reference the published `0.4.0` package. The DNQ integration phase
cannot begin until the package is visible on nuget.org -- there is no
project-reference fallback.

## Repository Map

| Repo | Path | Phases |
|------|------|--------|
| TaskScheduler | `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` | 1, 2 |
| DotNetWorkQueue (this repo) | `/mnt/f/git/dotnetworkqueue` | 3, 4 |

## Dependency Graph

```
Phase 1  (TaskScheduler: Lock contention fix + unit tests)       [PENDING]
   |
   v
Phase 2  (TaskScheduler: NuGet 0.4.0 release + verify on nuget.org) [PENDING]
   |
   v   (hard gate: package must be visible on nuget.org)
   |
Phase 3  (DotNetWorkQueue: Integration test project, green locally) [PENDING]
   |
   v
Phase 4  (DotNetWorkQueue: Jenkins + GitHub Actions CI wiring)    [PENDING]
```

Phase 2 depends on Phase 1: cannot pack a fix that does not exist or whose
unit tests do not pass. Phase 3 depends on Phase 2 publishing `0.4.0` to
nuget.org because the new test project uses `PackageReference` only. Phase 4
is split from Phase 3 to isolate CI-environment risk (NetMQ UDP beacon
behavior in Docker agents) from pure test-authoring risk.

---

## Phase 1: TaskScheduler Lock Contention Fix + Unit Tests

**Repository:** `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
(`/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`)

**Risk: HIGH** -- This is concurrent socket-management code using NetMQ. Bugs
in the poller wiring can produce deadlocks, dropped messages, or lost events
that only surface under load. Highest-risk phase, runs first to fail fast.

**Depends on:** Nothing (new work in sibling repo).

### Objective

Eliminate `_lockSocket` contention in `TaskSchedulerJobCountSync` so
`IncreaseCurrentTaskCount` / `DecreaseCurrentTaskCount` no longer stall up to
10ms behind the receive loop. Replace the `TryReceiveFrameString(10ms)`
polling pattern with a `NetMQPoller` that owns `_actor` on a single dedicated
thread, and route outbound `SetCount` messages through a `NetMQQueue<T>`.

### Scope

1. **Refactor `TaskSchedulerJobCountSync`**
   - Introduce a `NetMQPoller` that owns `_actor` on one dedicated thread.
   - Outbound sends move to a `NetMQQueue<T>` (message tuple); all socket
     I/O happens inside poller callbacks.
   - `GetCurrentTaskCount` reads via
     `Interlocked.Read(ref _currentTaskCount) + _otherProcessorCounts.Values.Sum()`,
     no lock.
   - `Increase` / `Decrease` call `Interlocked.Increment/Decrement` synchronously
     (so return values stay correct) and enqueue the wire publish.
   - `Start` publishes the initial broadcast through the queue path.
   - `Dispose` stops the poller cleanly and disposes `_actor`; delete the
     `while(_running) Sleep(100)` hot-wait.
   - Public `ITaskSchedulerJobCountSync` API unchanged (signatures, return
     values, `RemoteCountChanged` event).

2. **Add unit tests** (match existing test conventions in the repo; inspect
   before writing):
   - Concurrency regression test: N threads hammering `Increase`/`Decrease`,
     assert no deadlock within timeout and final count equals expected delta.
   - State consistency test: scripted sequence of remote `SetCount` messages
     injected through a fake `ITaskSchedulerBus`; assert `GetCurrentTaskCount`
     returns the correct aggregate.
   - Lifecycle test: `Start` -> operate -> `Dispose` completes without hanging.

### Success Criteria

1. `TaskSchedulerJobCountSync` no longer contains `_lockSocket` around
   `TryReceiveFrameString`; all socket access happens on the poller thread.
2. Public API surface (`ITaskSchedulerJobCountSync`) is byte-identical to the
   pre-fix version -- no binary-breaking changes.
3. New unit tests pass reliably (run the concurrency test under a loop
   locally to shake out flakiness before declaring done).
4. All pre-existing tests in the TaskScheduler repo continue to pass.
5. `dotnet build -c Debug` and `dotnet build -c Release` both clean.

### Verification

```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler
dotnet build -c Debug
dotnet test        # all unit tests green
dotnet build -c Release -p:CI=true    # clean release build
```

---

## Phase 2: TaskScheduler NuGet 0.4.0 Release

**Repository:** `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`

**Risk: MEDIUM** -- NuGet push is irreversible: a wrong version or a package
missing `.snupkg` cannot be corrected without burning a version number. The
packaging mechanics themselves are well-understood (we have lessons on
`-p:CI=true`, `deploy/*.nupkg` form, and version ordering), but the
one-way nature of the action warrants medium risk.

**Depends on:** Phase 1.

### Objective

Version-bump, changelog, pack with deterministic Source Link, push `.nupkg`
and `.snupkg` to nuget.org, and verify the package is visible with green
validation indicators before unblocking Phase 3.

### Scope

1. Bump the project `Version` to `0.4.0` in the `.csproj`.
2. Update `CHANGELOG.md` with the fix description and a link to issue #6.
3. Build Release with `-p:CI=true` from a clean tree (`rm -rf obj bin` first
   in all projects).
4. Pack `.nupkg` + `.snupkg` into a `deploy/` directory.
5. Push with `dotnet nuget push "deploy/*.nupkg" --api-key $KEY --source https://api.nuget.org/v3/index.json`
   (CLI auto-picks up the matching `.snupkg`).
6. Verify on nuget.org:
   - Package page loads for version `0.4.0`.
   - Source Link / deterministic build / symbols show green validation.
   - `dotnet add package DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler --version 0.4.0`
     succeeds from a throwaway console project.

### Success Criteria

1. `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler 0.4.0` is
   publicly listed on nuget.org with symbols and deterministic Source Link
   (all green validation indicators).
2. A fresh `dotnet restore` in a scratch project can pull `0.4.0` from
   nuget.org successfully.
3. `CHANGELOG.md` committed with the fix description and issue-link.
4. Git tag `v0.4.0` (or the repo's convention) applied.

### Verification

```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler
rm -rf **/obj **/bin deploy
dotnet build -c Release -p:CI=true
dotnet pack  -c Release -p:CI=true -o deploy
ls deploy/*.nupkg deploy/*.snupkg
# push:
dotnet nuget push "deploy/*.nupkg" --api-key $NUGET_KEY --source https://api.nuget.org/v3/index.json
# verify (scratch dir):
mkdir /tmp/nuget-verify && cd /tmp/nuget-verify
dotnet new console && dotnet add package DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler --version 0.4.0
```

---

## Phase 3: DotNetWorkQueue Integration Test Project (Green Locally)

**Repository:** `DotNetWorkQueue` (this repo)

**Risk: MEDIUM** -- Authoring three test classes against a brand-new package
where the fix is live means tests can hit real concurrency edges. Using a
Memory DNQ transport and loopback NetMQ keeps external dependencies out, but
NetMQ beacon discovery on localhost may still have surprises. Medium risk
because flakes here block Phase 4.

**Depends on:** Phase 2 (NuGet `0.4.0` visible on nuget.org).

### Objective

Create a new integration test project that `PackageReference`s
`DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler 0.4.0` and exercises
distributed scheduling end-to-end. Tests must be green locally on both
`net10.0` and `net8.0` before any CI wiring is touched.

### Scope

1. **Create project**
   - `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.IntegrationTests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.IntegrationTests.csproj`
   - Target frameworks: `net10.0;net8.0`.
   - `PackageReference` to
     `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` version `0.4.0`
     (NuGet only, no `ProjectReference`).
   - Test stack: MSTest 3.x, NSubstitute, AutoFixture, FluentAssertions
     `6.12.2` (pinned per house style).
   - License header per `DotNetWorkQueue.licenseheader` on every new file.
   - Added to `DotNetWorkQueue.sln` (and `DotNetWorkQueueNoTests.sln` if that
     solution already includes integration test projects -- inspect first).

2. **Test classes**
   1. `EndToEndSchedulingTests` -- spin up 2-3 in-process scheduler nodes on
      localhost, dispatch jobs, assert all are consumed across the nodes and
      remote counts converge.
   2. `ConcurrencyRegressionTests` -- hammer `Increase`/`Decrease` from many
      threads while the receive loop runs; assert no deadlock within a
      generous timeout and final counts are consistent (this is the
      cross-repo regression guard for Phase 1's fix).
   3. `NodeDiscoveryTests` -- verify `Broadcast` / `AddedNode` / `RemovedNode`
      wire messages produce the expected state transitions; verify
      `RemoteCountChanged` raises.

3. **Test infrastructure**
   - Use the Memory DNQ transport for the underlying queue so no external
     services are required.
   - All bus endpoints use loopback / inproc.
   - If the existing integration test harness provides a shared
     `ActivityListener` (Phase-5-code-coverage introduced one in
     `IntegrationTests.Shared`), reuse that pattern so trace decorators are
     exercised; do not add a new listener.

### Success Criteria

1. Project builds clean for both `net10.0` and `net8.0`:
   `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` -- 0 errors, 0 new
   warnings.
2. All three test classes pass locally. Run the full project 5 times in a
   loop to shake out flakiness:
   `for i in 1 2 3 4 5; do dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.IntegrationTests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.IntegrationTests.csproj" || break; done`
3. The test project references NuGet `0.4.0` -- `dotnet list package` shows
   the NuGet version, no project reference to the sibling repo.
4. `dotnet build "Source/DotNetWorkQueue.sln" -c Release -p:CI=true` -- 0
   errors, 0 warnings.
5. All pre-existing DotNetWorkQueue tests continue to pass unchanged.

### Verification

```bash
cd /mnt/f/git/dotnetworkqueue
dotnet restore "Source/DotNetWorkQueue.sln"
dotnet build "Source/DotNetWorkQueue.sln" -c Debug
dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.IntegrationTests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.IntegrationTests.csproj"
dotnet build "Source/DotNetWorkQueue.sln" -c Release -p:CI=true
```

---

## Phase 4: CI Wiring (Jenkins + GitHub Actions)

**Repository:** `DotNetWorkQueue` (this repo)

**Risk: MEDIUM** -- Jenkins Docker agents may restrict UDP multicast
(required for NetMQ beacon discovery). Jenkins has 13 parallel stages
already and a 5s staggered-startup pattern that must be preserved. The
GitHub Actions path is lower risk since the unit+integration job already
runs a batch of Memory-transport test projects.

**Depends on:** Phase 3 (tests green locally).

### Objective

Add the new integration test project to both CI surfaces so it runs on every
PR and on master, without destabilising existing stages.

### Scope

1. **GitHub Actions** (`.github/workflows/ci.yml`)
   - Add the new project to the existing unit+integration job that runs
     Memory-transport-only integration tests.
   - Confirm `net10.0` is the targeted framework in that job (existing CI
     workflow runs net10.0 on ubuntu-latest per CLAUDE.md).

2. **Jenkinsfile**
   - Add a new parallel stage for the integration test project.
   - Match the existing 5s staggered-startup pattern to avoid GitHub clone
     rate-limiting (13 -> 14 parallel stages).
   - Stage runs on the existing Docker agent label (no new agent image).
   - Confirm `.trx` / coverage output flows into the existing Coverlet +
     Codecov collection pipeline.

3. **UDP multicast validation**
   - Before enabling the Jenkins stage on master, run the stage on a feature
     branch and confirm `NodeDiscoveryTests` pass in the Docker agent.
   - If UDP multicast is blocked in the agent, pin the bus to a specific
     loopback TCP endpoint in the tests (add a TestContext hook) or
     `[TestCategory("BeaconRequired")]` and skip under a Jenkins env var. Do
     not merge a skip without opening a follow-up issue.

### Success Criteria

1. A PR-triggered GitHub Actions run exercises the new integration test
   project and it passes on `ubuntu-latest` / `net10.0`.
2. A Jenkins master build runs the new parallel stage and it passes on the
   Docker agent with Coverlet output uploaded to Codecov.
3. Jenkins total parallel stages increases by exactly one with the 5s
   staggered-startup pattern preserved.
4. No existing CI stage regresses (all previously-green stages still green).
5. Any beacon-skip decision is documented in a follow-up issue link in the
   Jenkinsfile comment.

### Verification

```bash
# locally, lint the pipeline YAML + Groovy:
cat .github/workflows/ci.yml
cat Jenkinsfile
# then push a feature branch and observe:
#   - GitHub Actions: new project ran under unit+integration job, passed
#   - Jenkins:        new parallel stage ran, passed, 5s stagger preserved
#   - Codecov:        new test project reports coverage
```

---

## Phase Summary

| Phase | Repo | Name | Risk | Depends On |
|-------|------|------|------|------------|
| 1 | TaskScheduler | Lock contention fix + unit tests | High | -- |
| 2 | TaskScheduler | NuGet 0.4.0 release + verify | Medium | 1 |
| 3 | DotNetWorkQueue | Integration test project (green locally) | Medium | 2 (hard gate: nuget.org visibility) |
| 4 | DotNetWorkQueue | Jenkins + GitHub Actions CI wiring | Medium | 3 |

## Execution Order Rationale

- **Phase 1 first (highest risk)**: the concurrency refactor is where bugs
  will actually bite. Landing it first means failures show up while we have
  the most cognitive budget on the problem. This also follows the
  fail-fast principle.
- **Phase 2 gated on Phase 1**: never pack an untested fix.
- **Phase 3 gated on Phase 2 (hard gate)**: the new test project uses
  `PackageReference` only -- it literally cannot compile until `0.4.0` is
  resolvable from nuget.org. Do not attempt a dual-reference (project +
  NuGet) workaround; PROJECT.md explicitly rules this out as a non-goal.
- **Phase 4 split from Phase 3**: CI-environment risk (Docker + NetMQ UDP
  beacon) is distinct from test-authoring risk. Splitting lets us confirm
  green local tests before touching the Jenkinsfile that already drives 13
  parallel stages.

## Key Risks and Mitigations

| Risk | Mitigation |
|------|-----------|
| NetMQ poller deadlock under high contention | Phase 1 ships a dedicated concurrency regression unit test; Phase 3 re-runs the equivalent from outside the package. |
| NuGet `0.4.0` pushed with missing `.snupkg` or non-deterministic Source Link | Always use `dotnet nuget push "deploy/*.nupkg"` form; always `-p:CI=true`; always clean `obj`/`bin` before pack (per CLAUDE.md lessons). |
| Wrong version pushed (irrecoverable) | Verify `.csproj` Version and `CHANGELOG.md` match before push; Phase 2 is its own phase specifically so packaging gets a dedicated review gate. |
| UDP multicast blocked on Jenkins Docker agents | Phase 4 explicitly validates this on a feature branch before merging to master; fallback is TCP-pinned endpoint or categorised skip with follow-up issue. |
| Integration test flakiness surfaces only in CI | Phase 3 requires 5 consecutive local runs before moving on; Phase 4 uses a feature branch for Jenkins pre-flight. |
| `FluentAssertions` accidental upgrade past 6.12.2 | Pin version explicitly in the new `.csproj`; house convention enforced by CLAUDE.md. |

## Non-Goals (from PROJECT.md)

- Redesigning `ITaskSchedulerBus` or the beacon/discovery protocol.
- Public API changes in the TaskScheduler module.
- Migrating TaskScheduler repo's test tooling.
- Throughput benchmarking gates in CI.
- Dual-reference (project + NuGet) plumbing in the DNQ test project.
