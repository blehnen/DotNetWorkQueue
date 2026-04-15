# Project: TaskScheduler Lock Contention Fix + Integration Tests

## Description

Fix issue #6 in `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` (lock contention in `TaskSchedulerJobCountSync.ProcessMessages`), release a new 0.4.0 NuGet package, then add a new integration test project in `DotNetWorkQueue` that exercises the published package end-to-end. The integration tests prove distributed task scheduling works correctly, catch regressions of the concurrency bug, and validate node discovery / broadcast behavior.

The work spans two repositories in strict sequence: the fix and unit tests ship first from the TaskScheduler repo, then the DotNetWorkQueue integration tests reference the published 0.4.0 package.

## Goals

1. Eliminate `_lockSocket` contention in `TaskSchedulerJobCountSync` so `IncreaseCurrentTaskCount` / `DecreaseCurrentTaskCount` no longer stall up to 10ms behind the receive loop.
2. Add unit tests in the TaskScheduler repo covering the new concurrency model.
3. Publish `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` 0.4.0 to NuGet with deterministic Source Link (`-p:CI=true`).
4. Create a new integration test project in DotNetWorkQueue that references the 0.4.0 NuGet package and proves distributed scheduling works end-to-end.
5. Wire the new integration tests into both Jenkins and GitHub Actions CI.

## Non-Goals

- Redesigning the `ITaskSchedulerBus` abstraction or the beacon/discovery protocol.
- Public API changes to the TaskScheduler module (fix is behavioral only).
- Migrating the TaskScheduler repo's test tooling (keep whatever it currently uses for unit tests).
- Throughput benchmarking gates in CI (too flaky — smoke-test locally only).
- Dual-reference (project + NuGet) plumbing in the DotNetWorkQueue test project.

## Requirements

### Workstream 1: Lock Contention Fix (TaskScheduler repo)

- Replace `_lockSocket` + `TryReceiveFrameString(10ms)` polling with a `NetMQPoller` that owns `_actor` on a single dedicated thread.
- Outbound `SetCount` messages from `IncreaseCurrentTaskCount` / `DecreaseCurrentTaskCount` go onto a `NetMQQueue<T>` (where T is a message tuple) rather than directly touching the socket.
- The poller listens on both `_actor.ReceiveReady` and the `NetMQQueue.ReceiveReady`, draining each in its callback — all socket I/O happens on the poller thread.
- `GetCurrentTaskCount` reads via `Interlocked.Read(ref _currentTaskCount) + _otherProcessorCounts.Values.Sum()` with no lock.
- `Interlocked.Increment` / `Decrement` still happen synchronously in the caller so callers get a correct return value; only the wire publish is deferred to the poller.
- `Start` publishes the initial broadcast through the queue path as well.
- `Dispose` stops the poller cleanly and disposes `_actor`; remove the `while(_running) Sleep(100)` hot-wait.
- Preserve public API: `ITaskSchedulerJobCountSync` signatures and behavior (return values, `RemoteCountChanged` event) unchanged.

### Workstream 2: Unit Tests (TaskScheduler repo)

- Concurrency regression test: spawn N threads hammering `Increase`/`Decrease`; assert no deadlock within timeout and final count equals the expected delta.
- State consistency test: after a scripted sequence of remote `SetCount` messages injected through a fake `ITaskSchedulerBus`, assert `GetCurrentTaskCount` returns the correct aggregate.
- Lifecycle test: `Start` → operate → `Dispose` completes without hanging.
- Match whatever test conventions already exist in the TaskScheduler repo (inspect before writing).

### Workstream 3: NuGet Release (TaskScheduler repo)

- Version bump to **0.4.0** in the .csproj.
- Update `CHANGELOG.md` with the fix description and link to issue #6.
- Build Release with `-p:CI=true` for deterministic Source Link.
- Pack `.nupkg` + `.snupkg` from a clean Release build.
- `dotnet nuget push "deploy/*.nupkg" --api-key $KEY --source https://api.nuget.org/v3/index.json` (CLI auto-picks matching `.snupkg`).
- Verify the package appears on nuget.org with green validation indicators before proceeding.

### Workstream 4: Integration Test Project (DotNetWorkQueue repo)

- New project: `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.IntegrationTests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.IntegrationTests.csproj`
- Target frameworks: `net10.0;net8.0` matching the rest of the repo.
- `PackageReference` to `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` 0.4.0 (NuGet only — no project reference).
- Test stack: MSTest 3.x, NSubstitute, AutoFixture, FluentAssertions 6.12.2 (matching house style).
- Added to `DotNetWorkQueue.sln` (and `DotNetWorkQueueNoTests.sln` if applicable).
- Test classes:
  1. **`EndToEndSchedulingTests`** — spin up 2–3 in-process scheduler nodes on localhost, dispatch jobs, assert all are consumed across the nodes, remote counts converge.
  2. **`ConcurrencyRegressionTests`** — hammer `Increase`/`Decrease` from many threads while the receive loop runs; assert no deadlock within a generous timeout and final counts are consistent.
  3. **`NodeDiscoveryTests`** — verify `Broadcast` / `AddedNode` / `RemovedNode` wire messages produce the expected state transitions; verify `RemoteCountChanged` raises.
- Use a Memory transport for the underlying DNQ queue so no external services are required.

### Workstream 5: CI Wiring

- Jenkinsfile: add a new parallel stage for the integration test project; match the existing staggered-startup pattern (5s interval) to avoid clone storms.
- GitHub Actions: add the project to the unit+integration test job in `.github/workflows/ci.yml`.
- Verify NetMQ UDP beacon discovery works inside the Jenkins Docker agents before enabling; if UDP multicast is blocked, either pin the bus to a specific TCP endpoint in the tests or skip beacon-based tests in that environment.

## Non-Functional Requirements

- No public API changes in the TaskScheduler module (behavioral fix only).
- All existing DotNetWorkQueue tests continue to pass.
- New integration tests must run deterministically in both Jenkins Docker agents and GitHub Actions ubuntu-latest runners — no flakiness tolerated.
- No network calls to external services; all tests use loopback + inproc.
- Both net10.0 and net8.0 must pass.
- FluentAssertions pinned to 6.12.2 (last MIT version).
- License headers on all new files per `DotNetWorkQueue.licenseheader`.

## Success Criteria

1. `TaskSchedulerJobCountSync` no longer contains the `_lockSocket` mutex around `TryReceiveFrameString`; all socket access is on the poller thread.
2. Concurrency regression unit test in the TaskScheduler repo passes reliably under load.
3. `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` 0.4.0 is published on nuget.org with deterministic Source Link (green validation indicators).
4. New integration test project in DotNetWorkQueue builds against the 0.4.0 NuGet and all three test classes pass locally.
5. Jenkins parallel stage runs the new integration tests and they pass on first commit.
6. GitHub Actions CI runs the new integration tests and they pass.
7. `dotnet build "Source/DotNetWorkQueue.sln" -c Release -p:CI=true` — 0 errors, 0 warnings.

## Constraints

- **Strict sequencing**: fix → unit tests → pack → push NuGet → integration tests. The integration test project cannot exist before 0.4.0 is published because it has no project reference.
- Work spans two repositories: `F:\Git\DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` (fix + unit tests + NuGet) and `F:\Git\DotNetWorkQueue` (integration tests + CI wiring).
- NuGet versions are one-way: once 0.4.0 is pushed, cannot re-push or downgrade. Validate locally before pushing.
- NuGet pushes always use `dotnet nuget push "deploy/*.nupkg"` form so `.snupkg` is picked up automatically.
- Must target net10.0 and net8.0 for both repos where applicable.
- Jenkins Docker agents may have UDP multicast restrictions — validate before enabling beacon-dependent tests there.
