# Shipyard History

## 2026-03-26 — Project Initialized

- **Action:** `/shipyard:init`
- **Settings:** Interactive mode, manual git, detailed review, all-Opus routing
- **Status:** Ready for planning

## 2026-03-26 — Project Definition & Roadmap

- **Action:** `/shipyard:brainstorm`
- **Scope:** Security & Stability Fixes — C-1, C-2, H-2, H-5, H-7
- **Decisions:**
  - C-1: Deny-list binder as default (non-breaking), optional allow-list binder for maximum lockdown
  - C-2: Wiki security page (documentation only, no runtime changes)
  - H-2: Per-transport queue name validation (not in base class)
  - H-5: Implement IAsyncDisposable on DashboardConsumerClient
  - H-7: Remove IntegrationTests.Metrics project (replace stub types with core NoOp types)
- **Roadmap:** 5 phases, Phases 1-4 parallel, Phase 5 depends on Phase 1
- **Status:** Roadmap approved, ready for Phase 1 planning

## 2026-03-26 — Phase 1 Planned

- **Action:** `/shipyard:plan 1`
- **Plans:** 3 plans across 2 waves
  - Wave 1: PLAN-1.1 (DenyListSerializationBinder + tests) | PLAN-1.2 (AllowListSerializationBinder + tests)
  - Wave 2: PLAN-2.1 (Wire into serializers, DI registration, integration tests)
- **Design Decisions:** Deserialization-only protection, method-based extensibility, leave test helper as-is
- **Critique Verdict:** READY — all file paths verified, API surface confirmed, baseline tests green
- **Status:** Ready for build

## 2026-03-26 — Phase 1 Complete

- **Action:** `/shipyard:build 1`
- **Plans executed:** 3/3 (PLAN-1.1, PLAN-1.2, PLAN-2.1)
- **Files created:** DenyListSerializationBinder.cs, AllowListSerializationBinder.cs + test files
- **Files modified:** JsonSerializer.cs, JsonSerializerInternal.cs, ComponentRegistration.cs + test files
- **Review results:** All 3 plans passed spec compliance + code quality review
- **Security audit:** PASS — expanded deny-list to 29 gadget types per audit recommendation
- **Simplification:** Fixed per-call settings allocation in JsonSerializerInternal, removed duplicate test
- **Tests:** 873 passing, 0 failures
- **Status:** Phase 1 complete

## 2026-03-26 — Phase 2 Planned

- **Action:** `/shipyard:plan 2`
- **Plans:** 2 plans in Wave 1 (parallel)
  - PLAN-1.1: Relational transports (SqlServer, PostgreSQL, SQLite) + fix QueueCreatorTests
  - PLAN-1.2: Non-relational transports (Redis, LiteDB, Memory)
- **Design Decisions:** Per-transport validation, per-transport max lengths, no base class changes, empty names allowed for backward compat
- **Critique Verdict:** READY — all 15 file paths verified, zero file overlap
- **Status:** Ready for build

## 2026-03-26 — Phase 2 Complete

- **Action:** `/shipyard:build 2`
- **Plans executed:** 2/2 (PLAN-1.1, PLAN-1.2)
- **Files modified:** 6 production + 9 test files across all transports
- **Review results:** Both plans passed spec compliance + code quality review
- **Fixes applied:** Removed unused fixture variables (Release build fix), compiled regex patterns
- **Security audit:** PASS — regex anchored, no ReDoS risk, all entry points covered
- **Simplification:** Ship as-is — per-transport duplication is manageable and intentional
- **Tests:** 49 new validation tests, all passing
- **Status:** Phase 2 complete

## 2026-03-27 — Phase 3 Planned

- **Action:** `/shipyard:plan 3`
- **Plans:** 1 plan (PLAN-1.1), 2 tasks (TDD: tests first, then implementation)
- **Approach:** DisposeAsync delegates to StopAsync; sync Dispose skips HTTP DELETE (server prunes via heartbeat timeout)
- **Critique Verdict:** READY — all line numbers and API surface verified against actual source
- **Status:** Ready for build

## 2026-03-27 — Phase 3 Complete

- **Action:** `/shipyard:build 3`
- **Plans executed:** 1/1 (PLAN-1.1)
- **Files modified:** DashboardConsumerClient.cs (IAsyncDisposable + revised Dispose), DashboardConsumerClientTests.cs (7 new + 2 updated tests)
- **Review results:** Spec compliance PASS, code quality APPROVED (0 Critical, 0 Important)
- **Security audit:** PASS — no resource leaks, no race conditions, proper auth on HTTP DELETE
- **Tests:** 92 passing on both net8.0 and net10.0
- **Status:** Phase 3 complete

## 2026-03-27 — Phase 4 Planned

- **Action:** `/shipyard:plan 4`
- **Plans:** 1 plan (PLAN-1.1), 3 tasks
- **Key finding:** IntegrationTests.Metrics types accumulate values for test assertions — cannot replace with NoOp. Moving files into IntegrationTests.Shared instead.
- **Critique Verdict:** READY — all file paths verified, move strategy sound
- **Status:** Ready for build

## 2026-03-27 — Phase 4 Complete

- **Action:** `/shipyard:build 4`
- **Plans executed:** 1/1 (PLAN-1.1)
- **Changes:** Moved 7 metric files to IntegrationTests.Shared/Metrics/, removed ProjectReference, removed from .sln and InternalsVisibleForTests.cs, deleted project directory
- **Tests:** Full solution builds (0 errors), 56 in-memory integration tests pass (metrics verification works)
- **Status:** Phase 4 complete

## 2026-03-27 — Phase 5 Planned

- **Action:** `/shipyard:plan 5`
- **Plans:** 1 plan (PLAN-1.1), 1 task: create SECURITY.md
- **Scope:** Documentation only — Dynamic LINQ risks, serialization binder protections, deployment recommendations
- **Status:** Ready for build

## 2026-03-27 — Phase 5 Complete

- **Action:** `/shipyard:build 5`
- **Plans executed:** 1/1 (PLAN-1.1)
- **File created:** `Source/DotNetWorkQueue/SECURITY.md` (179 lines, 7 sections)
- **Review:** Spec PASS, quality APPROVED — fixed deny-list count (29→30)
- **Status:** Phase 5 complete

## 2026-03-27 — All Phases Complete

- **Milestone:** Security & Stability Fixes — 5/5 phases delivered
  - Phase 1: Serialization Security (DenyList + AllowList binders)
  - Phase 2: Queue Name Validation (all 6 transports)
  - Phase 3: Async Dispose Fix (IAsyncDisposable on DashboardConsumerClient)
  - Phase 4: Stale Project Cleanup (IntegrationTests.Metrics removed)
  - Phase 5: Security Documentation (SECURITY.md)
- **Status:** Ready for review and merge

## 2026-03-27 — New Milestone: Thread Management Modernization

- **Action:** `/shipyard:brainstorm`
- **Scope:** M-1 (Remove Thread.Abort) + M-2 (Replace Manual Threads)
- **Decisions:**
  - Remove Thread.Abort entirely (not deprecate) — existing cancellation tokens are sufficient
  - Replace new Thread() with Task.Run(LongRunning) on all targets including net48
  - Replace Thread.Sleep(20) spin-wait with ManualResetEventSlim
- **Roadmap:** Phase 6 (abort removal, independent PR), Phase 7 (thread replacement, depends on 6)
- **Constraint:** PR #82 must merge before code changes begin
- **Status:** Roadmap approved, ready for Phase 6 planning

## 2026-03-28 — Phase 6 Planned

- **Action:** `/shipyard:plan 6`
- **Plans:** 3 plans across 2 waves
  - Wave 1: PLAN-1.1 (delete abort files, remove DI, simplify StopThread) | PLAN-1.2 (remove 5 ThreadAbortException catches)
  - Wave 2: PLAN-2.1 (remove config property, update comments, remove tests, verify)
- **Key finding:** IAbortWorkerThread removed entirely (not gutted to no-op) — cleaner than keeping dead abstractions
- **Critique Verdict:** READY — all 16 file paths verified, zero overlap between parallel plans
- **Status:** Ready for build (after PR #82 merges)

## 2026-03-28 — Phase 6 Complete

- **Action:** `/shipyard:build 6`
- **Plans executed:** 3/3 (PLAN-1.1, PLAN-1.2, PLAN-2.1)
- **Deleted:** IAbortWorkerThread.cs, AbortWorkerThread.cs, IAbortWorkerThreadDecorator.cs, AbortWorkerThreadTests.cs
- **Modified:** StopThread.cs (simplified), ComponentRegistration.cs (2 DI lines), 5 catch blocks removed, config property removed, 2 test methods removed, comments updated, README.md updated
- **Review:** All 8 success criteria PASS
- **Tests:** 873 unit + 56 integration passing
- **Status:** Phase 6 complete

## 2026-03-28 — Phase 7 Complete

- **Action:** `/shipyard:build 7`
- **Plans executed:** 2/2 (01-PLAN, 02-PLAN)
- **Plan 01:** BaseMonitor.Cancel() now uses ManualResetEventSlim instead of Thread.Sleep(20)
- **Plan 02:** Replaced new Thread() with Task.Factory.StartNew(LongRunning) in PrimaryWorker/Worker, adapted MultiWorkerBase/WorkerTerminate/StopThread/WaitForThreadToFinish, rewrote tests
- **Review:** All 8 success criteria PASS
- **Tests:** 875 unit + 56 integration passing
- **Status:** Phase 7 complete — Thread Management Modernization milestone done

## 2026-03-28 — Milestone Complete: Thread Management Modernization

- **Phases:** 6 (Remove Thread.Abort) + 7 (Replace Manual Threads) both complete
- **Summary:** Zero Thread.Abort, zero new Thread(), zero Thread.Sleep spin-waits. All workers use Task.Factory.StartNew(LongRunning). BaseMonitor uses ManualResetEventSlim. README updated.
- **Status:** Shipped

## 2026-03-29 — Milestone Shipped: Thread Management Modernization

- **Action:** `/shipyard:ship`
- **Delivery:** PR merged to master
- **CI fix:** Excluded JobSchedulerTests from GitHub Actions (timing-sensitive)
- **Lessons captured:** Task.Factory.StartNew vs Task.Run, chain migration, ManualResetEventSlim, internal queue name compliance
- **Status:** Shipped

## 2026-03-30 — Milestone Complete: Tier B Moderate Fixes

- **Action:** Phase 8 built and merged (PR #85)
- **Plans executed:** 3/3 (PLAN-1, PLAN-2, PLAN-3) + Task 4
  - PLAN-1: Central Package Management (Directory.Packages.props, 36 .csproj updated)
  - PLAN-2: Dashboard exception filter hardening + CORS support (H-4, H-3/M-9)
  - PLAN-3: Health check endpoint + README update (H-3)
  - Task 4: TODO/HACK audit + serialization binder fix (M-3, N-3)
- **Additional fixes in PR:** .gitattributes + line ending normalization, UTF-16→UTF-8 conversion for GlobalSuppressions.cs files
- **Status:** Shipped

## 2026-03-30 — New Milestone: Jenkins CI Migration

- **Action:** `/shipyard:brainstorm`
- **Scope:** Migrate CI from TeamCity to Jenkins with 6 Docker agents on Linux
- **Decisions:**
  - All 21 net48-only test projects need net10.0 added (Linux Docker agents can't run net48)
  - Code coverage switches from dotCover (JetBrains) to Coverlet (Cobertura format)
  - 6 parallel agents balanced by transport test duration (~63 min target vs ~2hr TeamCity)
  - Connection strings injected via Jenkins Credentials (currently TeamCity build params)
  - Redis connection string is hardcoded in source — acceptable for now
  - GitHub Actions keeps net48 unit tests for framework compatibility
  - Codecov.io integration preserved via Cobertura upload
- **Roadmap:** 5 phases: Multi-target tests → Coverlet → Docker+Jenkinsfile → Jenkins setup → E2E validation
- **Status:** Roadmap approved, ready for Phase 1 planning

## 2026-03-30 — Phase 1 Complete: Multi-Target Test Projects

- **Action:** Built on `jenkins` branch
- **Changes:** Multi-targeted 22 test projects to `net10.0;net48`. Added `#if NETFULL` guards for Dynamic LINQ, GetObjectData serialization, conditioned Soap reference. Fixed LiteDB csproj casing for Linux.
- **Commits:** 10 commits (`0c37c677`..`c7127d3c`)
- **Status:** Phase 1 complete

## 2026-03-30 — Phase 2 Complete: Code Coverage Migration

- **Action:** Built on `jenkins` branch
- **Changes:** Added `coverlet.collector` to all test projects. Removed `global.json` SDK pin (dotCover workaround). Cobertura XML output for Codecov.io.
- **Commits:** 2 commits (`7012699f`, `5e6ad858`)
- **Status:** Phase 2 complete

## 2026-03-31 — Phase 3 Complete: Docker Agent Image + Jenkinsfile

- **Action:** Built on `jenkins` branch
- **Changes:** Created `docker/Dockerfile` (Ubuntu, .NET 8+10 SDKs, Java 21 JRE, libsqlite3). Created `Jenkinsfile` with 13 parallel integration test stages, label-based agents, Codecov CLI upload.
- **Commits:** 10 commits (`c6cd36d4`..`1c72f93f`)
- **Status:** Phase 3 complete

## 2026-03-31 — Phase 4 Complete: Jenkins Setup + GitHub Actions Update

- **Action:** Built on `jenkins` branch
- **Changes:** Created `docs/jenkins-setup.md` (plugins, Docker cloud, credentials, Multibranch Pipeline). Updated `.github/workflows/ci.yml` for net48 unit tests only.
- **Commits:** 6 commits (`f2ae4b46`..`1ec99846`)
- **Status:** Phase 4 complete

## 2026-03-31 — Phase 5 Complete: End-to-End Validation

- **Action:** Iterative fixes on `jenkins` branch during real Jenkins pipeline runs
- **Changes:** Connection string injection to bin dirs, Dashboard transport strings, Redis format fix, SQLite native libs, JobScheduler test exclusion, Codecov CLI syntax, Redis connectionstring.txt refactor (PR #87).
- **Commits:** 8 commits (`bdaf881a`..`2edf7416`)
- **Bug fixes:** `#if NETFULL` guard, BaseMonitor disposal race, time offset tolerance, LiteDB casing
- **Status:** Phase 5 complete

## 2026-03-31 — Milestone Shipped: Jenkins CI Migration

- **Action:** `/shipyard:ship`
- **Delivery:** All work merged to master via PR #86 (jenkins) and PR #87 (Redis refactor)
- **Summary:** 37 commits, 230 files changed. 22 test projects multi-targeted, Coverlet coverage, Docker agent image, Jenkinsfile with 13 parallel stages, Jenkins setup guide, GitHub Actions narrowed to net48 unit tests.
- **Tests:** 875 unit tests passing on net10.0
- **Status:** Shipped

## 2026-03-31 — New Milestone: Integration Test Cleanup

- **Action:** `/shipyard:brainstorm`
- **Scope:** Redis dead code removal + remote transport test retry
- **Decisions:**
  - Remove `ConnectionInfoTypes` enum (vestigial from Windows Redis support) — convert to static class matching SqlServer/PostgreSQL
  - Add `[assembly: RetryOnFailure(MaxRetries = 1)]` to 6 remote transport test projects (Redis, SqlServer, PostgreSQL)
  - No changes to LiteDB/SQLite/Memory, no `#if NETFULL` cleanup, no callback signature refactoring
- **Roadmap:** 2 independent phases: Phase 1 (Redis enum removal, 34 files), Phase 2 (retry attribute, 6 files)
- **Status:** Roadmap approved, ready for planning

## 2026-03-31 — Phases 1 & 2 Complete: Integration Test Cleanup

- **Action:** `/shipyard:build` (both phases in parallel)
- **Phase 1 — Redis ConnectionInfoTypes Removal:**
  - Deleted `ConnectionInfoTypes` enum, converted `ConnectionInfo` to static class
  - Updated 34 test files across Redis.IntegrationTests (18) and Redis.Linq.Integration.Tests (15 + 1 bug fix)
- **Phase 2 — Remote Transport Test Retry:**
  - Original plan used `[assembly: RetryOnFailure]` — discovered MSTest `RetryAttribute` is method-level only
  - Pivoted to `Microsoft.Testing.Extensions.Retry` NuGet package with `--retry-failed-tests 1` CLI flag
  - Added package to 6 .csproj files, updated 6 Jenkinsfile stages
- **Review:** Spec compliance PASS, code quality PASS
- **Commit:** `eb84367c` (42 files changed)
- **Status:** All phases complete, ready to ship
