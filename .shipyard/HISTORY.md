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
