# Phase 4 Plan Verification

**Phase:** PostgreSQL Implementation + Unit Tests  
**Date:** 2026-05-14  
**Type:** plan-review  
**Reviewer:** Senior Verification Engineer

## Results

| # | Criterion | Status | Evidence |
|---|-----------|--------|----------|
| 1 | ROADMAP §Phase 4 Success Criterion #1: Transport.PostgreSQL builds clean (net10.0 + net8.0) | PLAN-FORWARD | Acceptance criteria in PLAN-1.1 Task 3 require Release build of `DotNetWorkQueue.Transport.PostgreSQL.csproj` with zero new warnings. Verification gate present. |
| 2 | ROADMAP §Phase 4 Success Criterion #2: All new PG unit tests pass; existing tests still pass | PLAN-FORWARD | PLAN-1.1 covers extractor (2 tests) + producer subclass (6 tests) + capability-cast smoke test (1 test) = 9 tests. Full suite rerun required per PLAN-1.1 Task 3 verification section. PLAN-2.1/2.2 each add 3 structural smoke tests. Regression gate present. |
| 3 | ROADMAP §Phase 4 Success Criterion #3: Capability-cast works for PostgreSQL | COVERED | PLAN-1.1 Task 3 includes `CapabilityCast_TypeImplementsRelationalProducerQueueShapes` test verifying `IsAssignableFrom` for `IRelationalProducerQueue<T>`, `RelationalProducerQueue<T>`, and `IProducerQueue<T>`. |
| 4 | ROADMAP §Phase 4 Success Criterion #4: Risk #2 closed (NpgsqlBatch deferral documented) | COVERED | CONTEXT-4 Decision 1 explicitly rejects `NpgsqlBatch` experiment, chooses sequential-loop pattern mirroring Phase 3. Documented as deferral for Phase 7 wiki draft. |
| 5 | ROADMAP §Phase 4 Success Criterion #5: Risk #3 closed (case-sensitive validator unit test) | COVERED | PLAN-1.1 Task 1 includes `Extract_PreservesCase_NoUpperCasing` test (lines 95–113) asserting case-sensitive comparison: `"MyDb"` vs `"mydb"` produce different outputs. PLAN-1.1 Task 2 includes `Send_ValidatorRejectsCaseMismatch_ThrowsBeforeCastGuard` test (lines 333–355) confirming validator fires BEFORE cast guard on case mismatch. |
| 6 | ROADMAP §Phase 4 Success Criterion #6: No NpgsqlConnection casts in non-fork handler code | PLAN-FORWARD | Grep verification gate in PLAN-1.1 Task 3 (lines 769–770): `grep -rn "using Npgsql\|using Microsoft\.Data\.SqlClient" Source/DotNetWorkQueue.Transport.RelationalDatabase/` must show zero matches, confirming layering invariant. Fork casts are allowed per CONTEXT-4 Decision 3. |
| 7 | PROJECT.md Success Criteria #1: IRelationalProducerQueue<T> exists and is implemented | COVERED | PLAN-1.1 establishes `PostgreSqlRelationalProducerQueue<T>` implementing the interface via DI registrations (3× `RegisterConditional` per Rule A). |
| 8 | PROJECT.md Success Criteria #3: Capability-cast pattern works (producer can cast to IRelationalProducerQueue<T>) | COVERED | Same as #3 above; PLAN-1.1 Task 3 smoke test. |
| 9 | PROJECT.md Success Criteria #7: Caller-owned resources not disposed (unit test via mocked DbTransaction) | COVERED | PLAN-1.1 Task 2 tests verify validator runs, cast guard fires, but never calls Commit/Rollback/Dispose/Close. PLAN-2.1 and PLAN-2.2 include source-text smoke tests (Tasks 2 each) explicitly grepping fork bodies for `.Commit()`, `.Rollback()`, `.Close()`, `.Dispose()` substrings (PLAN-2.1 Task 2 lines 315–318; PLAN-2.2 Task 2 lines 315–323). |
| 10 | CONTEXT-4 Rule A: DI registrations use RegisterConditional (not plain Register) | PASS | PLAN-1.1 Task 3 DI block (lines 676–678) contains exactly 3 `container.RegisterConditional(typeof(...ProducerQueue<>), ...)` registrations for `IProducerQueue<>`, `IRelationalProducerQueue<>`, and `RelationalProducerQueue<>` → `PostgreSqlRelationalProducerQueue<>`. Verification grep gate (line 734) checks for 3 matches on `RegisterConditional` + 0 matches on plain `Register(typeof(...ProducerQueue<>)...)`. |
| 11 | CONTEXT-4 Rule B: Lifecycle comment uses exact wording (no forbidden substrings) | PASS | PLAN-2.1 Task 1 (line 172) and PLAN-2.2 Task 1 (line 175) both contain exact comment: `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.` Verification smoke tests (PLAN-2.1 Task 2 lines 315–318, PLAN-2.2 Task 2 lines 315–323) explicitly grep for absence of `.Commit()`, `.Rollback()`, `.Close()`, `.Dispose()` substrings in fork bodies. No comment-stripping preprocessing needed (unlike Phase 3 PLAN-2.1). |
| 12 | CONTEXT-4 Rule C: Producer-subclass constructor has 11 parameters | PASS | PLAN-1.1 Task 2 constructor signature (lines 472–483) explicitly lists 11 parameters: 6 base (`QueueProducerConfiguration`, `ISendMessages`, `IMessageFactory`, `ILogger`, `GenerateMessageHeaders`, `AddStandardMessageHeaders`) + 5 new (`sendHandler`, `sendHandlerAsync`, `validator`, `sentMessageFactory`, `ownMessageFactory`). Verification grep gate (lines 745–750) counts commas (should be 10 for 11 params). |
| 13 | Decision 2 encoded: Extractor is pass-through (NO normalization) | PASS | PLAN-1.1 Task 1 implementation (line 174) returns `connection.Database ?? string.Empty` with NO `.ToUpperInvariant()` or `.ToLowerInvariant()`. Test `Extract_PreservesCase_NoUpperCasing` (lines 95–113) verifies case is preserved verbatim. Verification grep gate (line 730) checks for zero matches on `ToUpperInvariant\|ToLowerInvariant` in extractor source. |
| 14 | PG-specific: _getTime.GetCurrentUtcDate() materialization in handler forks | PASS | PLAN-2.1 Task 1 (line 159) calls `_getTime.GetCurrentUtcDate()` and passes result as eighth argument to `CreateMetaDataRecord(...)`. PLAN-2.2 Task 1 (line 161) mirrors for async: `await CreateMetaDataRecordAsync(..., _getTime.GetCurrentUtcDate())` — no await on `_getTime` itself (sync method). Verification grep gates (PLAN-2.1 line 364; PLAN-2.2 line 368) require exactly 1 match for `_getTime.GetCurrentUtcDate()` in fork bodies. |
| 15 | PG-specific: NpgsqlDbType.Bytea used (not SqlDbType.VarBinary) | PASS | PLAN-2.1 Task 1 body (lines 129, 135) uses `command.Parameters.Add("@body", NpgsqlDbType.Bytea, -1)` and `command.Parameters.Add("@headers", NpgsqlDbType.Bytea, -1)`. PLAN-2.2 Task 1 body (lines 131, 137) mirrors. Verification grep gates (PLAN-2.1 line 368–370; PLAN-2.2 line 372–374) require 2 matches for `NpgsqlDbType.Bytea` and 0 matches for `SqlDbType` in fork bodies. |
| 16 | PG-specific: Query/Command type parameters (NpgsqlConnection, NpgsqlTransaction) | PASS | PLAN-2.1 Task 1 (lines 113–114) constructs `DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>(...)` and PLAN-2.2 Task 1 (lines 115–116) mirrors. Both plans (lines 168–169 and 171–172 respectively) construct `SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>(...)`. All 3 instances per plan confirmed. |
| 17 | Task counts per plan: ≤3 tasks | PASS | PLAN-1.1: 3 tasks (extractor + producer + DI wiring). PLAN-2.1: 2 tasks (sync fork + smoke tests). PLAN-2.2: 2 tasks (async fork + smoke tests). Total: 7 tasks across 3 plans. |
| 18 | Wave ordering: Wave 1 has no dependencies; Wave 2 depends on Wave 1 | PASS | PLAN-1.1 (wave: 1, dependencies: []) — no predecessors. PLAN-2.1 (wave: 2, dependencies: [1.1]). PLAN-2.2 (wave: 2, dependencies: [1.1]). Correct. |
| 19 | Parallel-wave file disjointness: PLAN-2.1 and PLAN-2.2 share no modified files | PASS | PLAN-2.1 modifies `SendMessageCommandHandler.cs` (+ creates `SendMessageCommandHandlerForkSmokeTests.cs`). PLAN-2.2 modifies `SendMessageCommandHandlerAsync.cs` (+ creates `SendMessageCommandHandlerAsyncForkSmokeTests.cs`). Disjoint. Same shape as Phase 3 PLAN-2.1/2.2. Parallel execution safe. |
| 20 | File paths cited in plans match RESEARCH.md §1, §2, §3 | PASS | PLAN-1.1: creates extractor at `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlExternalDbNameExtractor.cs` (§1 confirmed), producer at `PostgreSqlRelationalProducerQueue.cs` (§1 confirmed), init at `PostgreSQLMessageQueueInit.cs` (§2 confirmed). PLAN-2.1/2.2: sync/async handlers at `CommandHandler/SendMessageCommandHandler[Async].cs` (§1 confirmed, lines 19–20). Tests at confirmed paths. All reference paths verified against live filesystem. |
| 21 | Acceptance criteria are testable (runnable verification per task) | PASS | PLAN-1.1 Task 1: `dotnet test` command with filter. Task 2: `dotnet test` + grep for case-sensitive behavior. Task 3: Release build command + grep gates (RegisterConditional count, extractor normalization, ctor param count, no NpgsqlConnection in RelationalDatabase). PLAN-2.1 Task 1: Release build for compile gate. Task 2: `dotnet test` on smoke tests. PLAN-2.2 Task 1: Build command. Task 2: `dotnet test` + grep gates (no .CommitAsync/.RollbackAsync/.CloseAsync/.DisposeAsync in async fork). All verification commands are concrete and runnable. |
| 22 | CONTEXT-4 Rule B also applies to PLAN-2.2 async fork | PASS | PLAN-2.2 Task 1 (line 175) uses identical comment text as PLAN-2.1. PLAN-2.2 Task 2 extends smoke test to also grep for async lifecycle variants (.CommitAsync, .RollbackAsync, .CloseAsync, .DisposeAsync — lines 320–323). Consistent across both forks. |

## Gaps

None identified. All phase requirements, CONTEXT-4 rules, and PG-specific deviations are explicitly encoded in plan tasks with measurable acceptance criteria and verification gates.

## Recommendations

1. **Plan execution order:** PLAN-1.1 (Wave 1) must complete before PLAN-2.1 and PLAN-2.2 (Wave 2) begin. Both Wave 2 plans can execute in parallel after Wave 1.
2. **Release build validation:** Each plan includes a Release build verification step. These are non-negotiable gates for `TreatWarningsAsErrors` + XML doc generation.
3. **Smoke test design (PLAN-2.1/2.2 Task 2):** The source-text grep approach for structural verification is correct and approved per Phase 3 precedent. No execution-level tests are feasible for the handler forks due to sealed `NpgsqlConnection`/`NpgsqlTransaction` types.
4. **Phase 6 integration test readiness:** Wave 1 unit tests establish foundation; Phase 6 integration tests will exercise `HandleExternalTx[Async]` against real PostgreSQL instances. Current unit-test coverage (mocks + structural smoke tests) is appropriately scoped for Phase 4.

## Verdict

**PASS** — Phase 4 plans are complete, well-formed, and ready for builder execution. All ROADMAP phase requirements, PROJECT.md success criteria, CONTEXT-4 hard rules, and PG-specific deviations from RESEARCH.md are explicitly encoded with measurable acceptance criteria and runnable verification gates. File paths are accurate, task counts are within limits (≤3 per plan), wave ordering is correct, and parallel-wave file disjointness is confirmed. No gaps or conflicts identified.

---

**Plan Summary for Builder:**

- **PLAN-1.1 (Wave 1):** 3 tasks — extractor (pass-through, case-sensitive Risk #3 test) + producer subclass (11-param ctor, 4 tx-aware overrides, cast guard) + DI wiring (3× `RegisterConditional` per Rule A). ~9 unit tests.
- **PLAN-2.1 (Wave 2, parallel):** 2 tasks — sync fork in `SendMessageCommandHandler.cs` (early-branch on `ExternalTransaction != null`, `_getTime.GetCurrentUtcDate()` materialization, `NpgsqlDbType.Bytea` parameters, lifecycle comment per Rule B) + 3 structural smoke tests (reflection signature, source-grep early-branch, source-grep no-lifecycle-calls).
- **PLAN-2.2 (Wave 2, parallel):** 2 tasks — async fork in `SendMessageCommandHandlerAsync.cs` (mirror of sync, with `async Task<long>` signature and `.ConfigureAwait(false)` pattern) + 3 structural smoke tests (including async variants of lifecycle checks).

All plans are ready for execution with no plan rework required.
