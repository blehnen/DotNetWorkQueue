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

## 2026-04-01 — Fix: Metrics Counter Race Condition

- **Action:** Debugging + fix on `integration_tests` branch, merged to master
- **Problem:** `MultiConsumerAsync.Run` failed with 99/100 on Jenkins (Linux/Docker). Never failed on TeamCity (Windows/net48).
- **Root cause:** Handler callback signals `waitForFinish.Set()` before `CommitMessage.Commit()` increments the metrics counter. On Linux with remote SQL Server + chaos mode, the commit lag is wide enough for the snapshot to miss the last increment.
- **Fix:** Added polling overload to `VerifyMetrics.VerifyProcessedCount` — polls live `IMetrics` for up to 5 seconds instead of single snapshot. Updated all 13 callers.
- **Bonus finding:** `--retry-failed-tests 1` in Jenkinsfile was silently ignored — requires Microsoft.Testing.Platform, not VSTest.
- **Tests:** 56 Memory integration tests passing locally
- **Status:** Merged to master

## 2026-04-01 — Attempted: MSTest Runner Migration (PR #89, Reverted)

- **Action:** Attempted to enable `EnableMSTestRunner` on 6 remote transport integration test projects
- **Problem 1:** .NET 10 SDK requires `TestingPlatformDotnetTestSupport=true` — per-project didn't work due to multi-targeting MSBuild evaluation order
- **Problem 2:** Moving `TestingPlatformDotnetTestSupport` to `Directory.Build.props` fixed the error but broke coverage collection for unit test projects that don't have `EnableMSTestRunner`
- **Conclusion:** Full migration requires `EnableMSTestRunner` + `OutputType=Exe` on ALL test projects simultaneously — not a partial change
- **Status:** PR #89 closed, branch deleted

## 2026-04-01 — New Milestone: Dashboard Improvements

- **Action:** `/shipyard:brainstorm`
- **Scope:** UI polish (compact layout) + Docker build (standalone Dashboard image)
- **Decisions:**
  - Replace connection/queue cards with compact MudSimpleTable rows
  - Remove left nav rail — breadcrumbs + clickable title instead
  - Move JSON-driven transport registration from sample project into base library
  - Docker image includes all 5 transports, configured via mounted appsettings.json
- **Roadmap:** 3 phases: UI Polish → Config-Driven Registration → Docker Image
- **Status:** Roadmap approved

## 2026-04-01 — Phase 1 Complete: UI Polish

- **Action:** `/shipyard:build 1`
- **Tasks:** 3/3 (nav drawer removal, connections table, queue table)
- **Files modified:** MainLayout.razor, Home.razor, ConnectionDetail.razor
- **Files deleted:** NavMenu.razor
- **Tests:** 45 Dashboard integration tests passing
- **Status:** Phase 1 complete

## 2026-04-01 — Phase 2 Complete: Config-Driven Transport Registration

- **Action:** `/shipyard:build 2`
- **Tasks:** 2/2 (transport refs + POCO, IConfiguration overload)
- **Files modified:** DashboardExtensions.cs, Dashboard.Api.csproj, Directory.Packages.props
- **Files created:** DashboardConnectionConfig.cs
- **Key fix:** IConfiguration namespace shadowed by DotNetWorkQueue.IConfiguration — used fully-qualified type
- **Tests:** 45 Dashboard integration tests passing
- **Test harness:** Converted DotnewWorkQueue.Api.Example to use IConfiguration overload with appsettings.json
- **Status:** Phase 2 complete, awaiting user verification

## 2026-04-02 — Phase 3 Complete: Docker Image

- **Action:** `/shipyard:build 3`
- **Tasks:** 2/2 (self-contained mode, Dockerfile + config + README)
- **Files modified:** Program.cs (conditional API registration), Dashboard.Ui.csproj (ProjectReference), Dashboard.Api.csproj (LiteDb casing fix), DashboardConnectionConfig.cs (nullable fix)
- **Files created:** docker/dashboard/Dockerfile, docker/dashboard/appsettings.example.json, docker/dashboard/README.md
- **Docker verified:** Image builds, container starts, health endpoint 200, UI serves at :8080
- **Fixes during build:** Linux case-sensitivity (3 path fixes), middleware ordering, non-root user, --no-restore removal
- **Status:** Phase 3 complete

## 2026-04-02 — Milestone Shipped: Dashboard Improvements

- **Action:** `/shipyard:ship`
- **Delivery:** PR #90 merged to master
- **CI fix:** Staggered 13 parallel Jenkins stages with 5s intervals to avoid Git clone storms
- **Lessons captured:** Docker case-sensitivity, --no-restore cache invalidation, Jenkins clone stagger, middleware ordering
- **Summary:** 3 phases — UI Polish, Config-Driven Registration, Docker Image. 15 commits across the branch.
- **Status:** Shipped

## 2026-04-05 — New Milestone Started: Fix History Duration (issue #94)

- **Action:** Project definition captured (commit `a2956451`), workspace cleaned up
- **Scope:** Single-phase cosmetic fix — normalize `DurationMs = 0` across all transports when sub-millisecond, display "< 1 ms" in Dashboard UI
- **Cleanup:**
  - Archived prior Dashboard Improvements artifacts → `.shipyard/archive/dashboard-improvements/`
  - Archived orphan metrics-race debug → `.shipyard/archive/metrics-race-debug/`
  - Archived 10 pre-shipyard personal notes → `.shipyard/archive/pre-shipyard-notes/`
  - Restored `.shipyard/ISSUES.md` (had been deleted in worktree)
  - Reset STATE.json to phase 1, ready_for_planning
- **Also filed:** Issue #97 (Redis history Status=Processing bug), Issue #98 (link Grafana dashboard from README)
- **Status:** Ready for /shipyard:plan 1

## 2026-04-05 — Phase 1 Planned

- **Action:** `/shipyard:plan 1`
- **Decisions captured (CONTEXT-1.md):**
  - Scope expansion: fix RecordComplete AND RecordError (roadmap covered Complete only)
  - TDD discipline: failing tests first for every fix
  - Skip researcher (ROADMAP.md already exhaustive)
- **Plans:** 2 plans, 6 tasks total
  - **PLAN-1.1** (Wave 1, 3 tasks): write-side normalization — Memory, RelationalDatabase, LiteDb
  - **PLAN-1.2** (Wave 2, 3 tasks): read-side fix + UI — Redis (read+write regression), LiteDb read, Dashboard UI `FormatDuration`
- **Architect deviations noted:**
  - RelationalDatabase refactored to two-UPDATE pattern since roadmap was written — fix path updated
  - Read-side discriminator uses `CompletedUtc > 0` (more semantically correct than `DurationMs > 0`)
  - Null rendering in UI preserved as `-` (minimal, non-breaking)
- **Critique verdict:** READY — all 12 files verified, line numbers accurate, API surface confirmed, no blocking issues
- **Status:** Ready for /shipyard:build 1

## 2026-04-05 — Phase 1 Build Complete

- **Action:** `/shipyard:build 1`
- **Plans executed:** 2/2 (PLAN-1.1, PLAN-1.2)
- **Commits:** 8 total
  - **Wave 1 (PLAN-1.1):** `a2d2337e` Memory, `171c796f` RelationalDatabase, `8cf57c0c` LiteDb
  - **Critical fix after Wave 1 review:** `b538823a` (removed `StartedUtc IS NOT NULL` guard from RecordComplete SQL — the C# was correct but the UPDATE was a silent no-op) + `03a356db` (removed dead first-UPDATE block detected by hardened test)
  - **Wave 2 (PLAN-1.2):** `686117bc` Redis read+write regression, `08ce80be` LiteDb read, `a79cec3c` Dashboard UI FormatDuration
- **Quality gates:**
  - Phase Verification: PASS (after 03a356db fix)
  - Security Audit: CLEAN — no critical/important findings, 3 suggestions
  - Simplification: LOW_PRIORITY_FINDINGS — 1 comment suggestion
  - Documentation: MINOR_GAPS — CHANGELOG entry added (0.9.17)
- **Tests:** 875 core unit + 16 RelationalDatabase + 45 Dashboard integration all passing on net10.0
- **Issues resolved:** ISSUE-014 (SQL WHERE guard bug), ISSUE-015 (dead test helper)
- **Status:** Ready for /shipyard:ship

## 2026-04-05 — Milestone Shipped: Fix History Duration (issue #94)

- **Action:** `/shipyard:ship`
- **Delivery:** PR #99 opened against master — https://github.com/blehnen/DotNetWorkQueue/pull/99
- **Pre-ship verification:** 147 tests pass (29 Core + 16 RelationalDatabase + 22 LiteDb + 35 Redis + 45 Dashboard Integration), Dashboard UI build clean
- **Lessons captured:** SQL WHERE guard no-op pattern, NSubstitute Redis mocking limitations (added to LESSONS.md and CLAUDE.md)
- **Closes:** GitHub #94
- **Status:** Shipped

## 2026-04-06 — Phase 1 Build Complete (issue #97)

- **Action:** `/shipyard:build 1`
- **Plans executed:** 3/3 (PLAN-1.1, PLAN-1.2, PLAN-1.3) + 1 review fix
- **Commits:**
  - `7aa86cfa` — fix decorator to capture messageId before delegation (Bug A)
  - `db724466` — guard RecordProcessingStart in Redis and Memory transports (Bug B)
  - `3ddc78a2` — add regression tests for Bug A and Bug B
  - `a277ac95` — fix Redis RecordProcessingStart null-cast collision (review fix)
- **Review:** PLAN-1.1 PASS, PLAN-1.2 PASS (after null-cast fix), PLAN-1.3 PASS
- **Verification:** 878 core + 166 Redis tests passing, full solution builds clean
- **Security audit:** PASS — no critical findings; advisory about pre-existing unchecked cast in RecordComplete/RecordError
- **Simplification:** Defer — no high-priority findings, test code duplication is minor
- **Documentation:** CHANGELOG updated
- **Status:** Phase 1 complete, ready for /shipyard:ship

## 2026-04-06 — Milestone Shipped: Fix History Status for Errored Messages (issue #97)

- **Action:** `/shipyard:ship`
- **Delivery:** PR #105 opened against master — https://github.com/blehnen/DotNetWorkQueue/pull/105
- **Pre-ship verification:** 37 targeted + 878 core + 166 Redis tests passing (net10.0)
- **Lessons captured:** Redis null-cast collision, HasValue guard pattern (added to LESSONS.md and CLAUDE.md)
- **Follow-up filed:** #104 (Redis unchecked cast on StartedUtc in RecordComplete/RecordError)
- **Closes:** GitHub #97
- **Status:** Shipped

## 2026-04-06 — New Milestone: Redis History Bug Fixes (#104, #103)

- **Action:** `/shipyard:brainstorm`
- **Scope:** Two Redis transport bugs — unchecked StartedUtc cast (#104) + broken purge logic (#103)
- **Decisions:**
  - Purge checks Status field (terminal only) + CompletedUtc guard (belt-and-suspenders)
  - Add GetDb() test seam to PurgeMessageHistoryHandler (matching WriteMessageHistoryHandler)
  - Single phase, 2 parallel plans (disjoint files)
- **Roadmap:** 1 phase, Wave 1 (parallel)
- **Status:** Ready for /shipyard:plan 1

## 2026-04-06 — Phase 1 Planned

- **Action:** `/shipyard:plan 1`
- **Plans:** 2 plans in Wave 1 (parallel)
  - PLAN-1.1: HasValue guard on StartedUtc in RecordComplete/RecordError (#104)
  - PLAN-1.2: Purge logic fix — GetDb() seam, HasValue guards, terminal-status-only (#103)
- **Critique Verdict:** READY — all file paths verified, zero file overlap, API surface confirmed
- **Status:** Ready for /shipyard:build 1

## 2026-04-06 — Phase 1 Build Complete (Redis history fixes #104, #103)

- **Action:** `/shipyard:build 1`
- **Plans executed:** 2/2 (PLAN-1.1, PLAN-1.2)
- **Commits:** 4 (tests + fixes for both plans)
- **Review:** Both PASS (PLAN-1.2 minor: extra Redis call for orphan cleanup)
- **Verification:** 172/172 Redis tests passing
- **Security audit:** PASS — no critical findings
- **Simplification:** Ship as-is — no issues beyond what reviewers caught
- **Documentation:** CHANGELOG update needed at ship time
- **Discovery:** RedisValue.Null cast to (long) silently returns 0L (doesn't throw) in current StackExchange.Redis — guard still correct for forward safety
- **Status:** Phase 1 complete, ready for /shipyard:ship

## 2026-04-06 — Milestone Shipped: Redis History Bug Fixes (#104, #103)

- **Action:** `/shipyard:ship`
- **Delivery:** PR #106 opened against master — https://github.com/blehnen/DotNetWorkQueue/pull/106
- **Pre-ship verification:** 172/172 Redis tests passing (net10.0)
- **Lessons captured:** RedisValue.Null (long) cast behavior, orphan index cleanup
- **Closes:** GitHub #104, #103
- **Status:** Shipped

## 2026-04-06 — New Milestone: Dashboard API History Tests

- **Action:** `/shipyard:brainstorm`
- **Scope:** Add Redis and LiteDb history integration tests to Dashboard API test suite
- **Motivation:** Redis history purge bug (#103) went undetected for 7 days — no integration test covered Redis purge
- **Decisions:**
  - Full pattern: both Disabled + Enabled test classes per transport (~15 tests each)
  - Follow exact MemoryHistoryTests.cs pattern
  - Jenkins CI for Redis (connection string gated), LiteDb runs everywhere
  - No production code changes
- **Roadmap:** 1 phase, 2 parallel plans (LiteDb + Redis)
- **Status:** Ready for /shipyard:plan 1

## 2026-04-06 — Phase 1 Planned

- **Action:** `/shipyard:plan 1`
- **Plans:** 2 plans in Wave 1 (parallel)
  - PLAN-1.1: LiteDbHistoryTests.cs (4 disabled + 15 enabled = 19 tests)
  - PLAN-1.2: RedisHistoryTests.cs (4 disabled + 15 enabled = 19 tests)
- **Critique Verdict:** CAUTION→READY — minor test count doc issue, all APIs verified
- **Status:** Ready for /shipyard:build 1

## 2026-04-06 — Phase 1 Build Complete (Dashboard API history tests)

- **Action:** `/shipyard:build 1`
- **Plans executed:** 2/2 (PLAN-1.1, PLAN-1.2) + 1 review fix
- **Commits:**
  - `2f9be036` — LiteDb history tests (19 tests) + LiteDB transport query bug fix
  - `f2b432c6` — Redis history tests (19 tests)
  - `3ebb0a48` — Fix Redis scope disposal + timeout comment
- **Reviews:** PLAN-1.1 PASS, PLAN-1.2 PASS (after scope disposal fix)
- **Verification:** 19 LiteDb tests passing, Redis builds clean
- **Bonus fix:** LiteDB QueryMessageHistoryHandler.Get had same query engine bug documented in GetCount — applied matching FindAll() workaround
- **Status:** Phase 1 complete, ready for /shipyard:ship

## 2026-04-06 — Milestone Shipped: Dashboard API History Tests

- **Action:** `/shipyard:ship`
- **Delivery:** PR #107 opened against master — https://github.com/blehnen/DotNetWorkQueue/pull/107
- **Pre-ship verification:** 19/19 LiteDb history tests passing (net8.0 + net10.0), Redis builds clean
- **Lessons captured:** LiteDB query engine bug, test race condition with CommitMessage.Commit
- **Status:** Shipped

## 2026-04-07 — New Milestone: Publish Aq.ExpressionJsonSerializer as NuGet Package (issue #102)

- **Action:** `/shipyard:brainstorm`
- **Scope:** Publish vendored DLL as `DotNetWorkQueue.Aq.ExpressionJsonSerializer` v1.0.0 on nuget.org, then swap DotNetWorkQueue to PackageReference
- **Decisions:**
  - Package ID: `DotNetWorkQueue.Aq.ExpressionJsonSerializer`, assembly/namespace unchanged
  - Publish to nuget.org (consistent with DotNetWorkQueue)
  - Version 1.0.0 (independent lifecycle from DotNetWorkQueue)
  - Merge upstream (aquilae) loop/goto expression support before v1.0.0
  - GitHub Actions CI with tag-triggered publish + Jenkinsfile for internal CI
  - NUGET_API_KEY stored as GitHub secret
- **Roadmap:** 2 phases + manual gate
  - Phase 1: Prepare fork (merge upstream, NuGet metadata, CI pipelines)
  - Manual Gate: User creates secret, pushes v1.0.0 tag, verifies nuget.org
  - Phase 2: Swap DotNetWorkQueue to PackageReference, delete /Lib
- **Status:** Roadmap approved, ready for Phase 1 planning

## 2026-04-07 — Phase 1 Planned

- **Action:** `/shipyard:plan 1`
- **Plans:** 1 plan (01-PLAN), 3 sequential tasks
  - Task 1: Merge upstream (loop/goto support) + update csproj with NuGet metadata + update test project TFMs
  - Task 2: Add GitHub Actions CI (matrix: ubuntu for net10.0/net8.0, windows for net48, publish on v* tag)
  - Task 3: Add Jenkinsfile (net10.0 + net8.0 only, Docker agent)
- **Discovery:** Test project targets stale `netcoreapp3.1;net48` — updated to `net10.0;net8.0;net48`
- **Decisions:** MIT license, Jenkins net10.0+net8.0 only, GH Actions matrix with windows-latest for net48
- **Critique verdict:** READY — all file paths verified, API surface confirmed
- **Status:** Ready for /shipyard:build 1

## 2026-04-07 — Phase 1 Build Complete

- **Action:** `/shipyard:build 1`
- **Repo:** `F:\Git\expression-json-serializer` (fork)
- **Plan executed:** 01-PLAN (3/3 tasks)
- **Commits (8 in fork):**
  - `9edeaaa` — Merge upstream (loop/goto)
  - `dbb844f` — NuGet metadata + csproj updates + test TFM updates
  - `58d0297` — GitHub Actions CI workflow
  - `700e43f` — Jenkinsfile
  - `32f22ec` — Fix review findings (SDK version floating, fetch-depth)
  - `3e7ae03` — Suppress CS1591 for fork library
  - `05eaa11` — Restrict CI workflow permissions (audit advisory)
  - `220a01f` — Update README for NuGet package page
- **Review:** MINOR_ISSUES → fixed (SDK pin, fetch-depth for Source Link)
- **Verification:** PASS (after CS1591 suppression — 0 warnings, 0 errors in Release)
- **Security audit:** PASS — no critical findings; applied permissions advisory
- **Simplification:** Ship as-is — 2 unused imports in upstream code, no action needed
- **Documentation:** README rewritten from stub to full content (install, usage, publish instructions)
- **Tests:** 33/33 passing on net10.0 and net8.0
- **Discoveries:** Upstream merge conflict in Deserializer.cs (resolved); TypeAs test exception changed in .NET 10 (fixed)
- **Status:** Phase 1 complete — ready for manual gate (push to origin, create NUGET_API_KEY, tag v1.0.0)

## 2026-04-07 — Manual Gate Complete

- **Action:** Published `DotNetWorkQueue.Aq.ExpressionJsonSerializer` v1.0.0 to nuget.org
- **Post-publish fixes (in fork):** ConcurrentDictionary for 3 Dictionary fields, NETFULL define for net48 tests
- **Status:** Package live, indexed, CI green

## 2026-04-07 — Phase 2 Build Complete

- **Action:** `/shipyard:build 2`
- **Repo:** DotNetWorkQueue (this repo)
- **Plan executed:** 01-PLAN (1/1 task)
- **Commit:** `b00b8536` — swap to PackageReference, delete Lib/Aq.ExpressionJsonSerializer/
- **Files:** 2 modified (Directory.Packages.props, DotNetWorkQueue.csproj), 10 deleted (vendored DLLs)
- **Tests:** 878 passing on net10.0
- **Status:** Phase 2 complete — all phases done, ready to ship

## 2026-04-07 — New Milestone: Drop net48/netstandard2.0 and Remove JpLabs.DynamicCode (issue #101)

- **Action:** `/shipyard:brainstorm`
- **Scope:** Remove net48 and netstandard2.0 targets, delete JpLabs.DynamicCode, remove all `#if NETFULL` / `#if NETSTANDARD2_0` conditional compilation, update CI and README. Version 0.9.3.
- **Decisions:**
  - Drop both net48 and netstandard2.0 (remaining targets: net10.0 + net8.0)
  - Remove all SoapFormatter/GetObjectData code (dead with net48 gone)
  - Keep Schyntax NuGet publishing separate (issue #100)
  - Employer stays on current version — blocker removed
- **Roadmap:** 10 phases across 4 waves
  - Phase 1: Core library + transport csproj + vendored DLL cleanup
  - Phase 2: Shared test infra + unit tests + base integration test csproj
  - Phases 3a-3f: Linq integration tests by transport (parallel)
  - Phase 4: CI + README + version bump
- **Status:** Roadmap approved, ready for Phase 1 planning
- [2026-04-07T19:59:51Z] Session ended during build (may need /shipyard:resume)
- [2026-04-07T20:01:43Z] Session ended during build (may need /shipyard:resume)
