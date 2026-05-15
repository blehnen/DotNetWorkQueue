# Phase 3 Verification

**Phase:** SqlServer Implementation + Unit Tests
**Type:** build-verify
**Date:** 2026-05-14

## Overall Status: PASS

All 8 Phase 3 exit criteria (CONTEXT-3.md §Phase 3 Exit Criteria) are satisfied. SqlServer outbox surface (extractor + producer subclass + sync/async `HandleExternalTx` forks + DI wiring) is fully implemented at the unit/structural-test level. Runtime atomic-commit/rollback verification deferred to Phase 6 integration tests by design (CONTEXT-3 Exit Criteria #3, #4, #5 and PROJECT.md §Success Criteria #4-#8).

## Exit Criteria Coverage (CONTEXT-3.md §Phase 3 Exit Criteria — 8 items)

| # | Criterion | Status | Evidence |
|---|-----------|--------|----------|
| 1 | `Transport.SqlServer` builds clean on net10.0 + net8.0 with `TreatWarningsAsErrors` and zero new XML-doc warnings | PASS | `dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release --nologo` → `0 Error(s)`, `14 Warning(s)` all pre-existing NU1902 OpenTelemetry advisories (ISSUE-032, predates Phase 1). No CS-prefixed (compiler) or CS1591 (missing XML doc) warnings present. Both `net10.0` and `net8.0` target frameworks emit the same NU1902 set and no others. |
| 2 | All new SqlServer unit tests pass; all existing SqlServer unit tests still pass | PASS | `dotnet test ... SqlServer.Tests` → `Passed!  - Failed: 0, Passed: 156, Skipped: 0, Total: 156, Duration: 17 s`. Baseline pre-Phase-3 was 141; +15 net additions (9 Wave 1 + 6 Wave 2) → 156 actual matches expected. Zero regressions. |
| 3 | SimpleInjector capability-cast smoke test passes for `IRelationalProducerQueue<T>` resolution from SqlServer container | PASS (type-system check); runtime DEFERRED to Phase 6 | `dotnet test ... --filter "FullyQualifiedName~SqlServerRelationalProducerQueue_ImplementsIRelationalProducerQueue"` → `Failed: 0, Passed: 1`. Test asserts `IsAssignableFrom(IRelationalProducerQueue<>, SqlServerRelationalProducerQueue<>)` + `RelationalProducerQueue<>` + `IProducerQueue<>` (3 type-system assertions). Runtime `container.GetInstance<IProducerQueue<T>>() is IRelationalProducerQueue<T>` deferred to Phase 6 (CONTEXT-3 line 105 + SUMMARY-1.1 Decisions: SimpleInjector `EnableAutoVerification` diagnostics from unrelated pre-existing transient-disposable issues block the runtime resolution path in unit tests; the static type-system check + DI grep gate combined prove capability-cast at the structural level). |
| 4 | Mock-based unit test confirms zero `Commit`/`Rollback`/`Dispose`/`Close` calls on caller's `DbTransaction`/`DbConnection` across sync + async + single + batch (PROJECT.md §Success Criteria #7) | PASS (source-text level); runtime mock-assertion DEFERRED to Phase 6 | `SendMessageCommandHandlerForkSmokeTests.HandleExternalTx_DoesNotCommitOrRollbackOrCloseOrDispose` (sync) + `SendMessageCommandHandlerAsyncForkSmokeTests` async equivalent assert the fork's source body (line-comment-stripped) contains zero `.Commit(`, `.Rollback(`, `.Close(`, `.Dispose(` calls. Sync grep result: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs:202 private long HandleExternalTx(SendMessageCommand commandSend)` exists; matching async at `SendMessageCommandHandlerAsync.cs:203`. Producer-side `SqlServerRelationalProducerQueueTests` covers the batch path (6 producer tests + 1 capability test = 7). 15-test filter aggregate: `Failed: 0, Passed: 15`. Live `DbTransaction`/`DbConnection` mock-call-count assertions deferred to Phase 6 per CONTEXT-3 design — `Microsoft.Data.SqlClient.SqlConnection` and `SqlTransaction` are sealed (NSubstitute can't mock), and unit-level coverage uses the source-grep guardrail. |
| 5 | Retry-bypass integration: caller-tx path dispatches into `HandleExternalTx` fork via the SqlServer registered handler chain | PASS (structural); full integration DEFERRED to Phase 6 | Fork dispatch verified at source level: `grep "commandSend.ExternalTransaction != null"` returns one match per handler (`SendMessageCommandHandler.cs:108` and `SendMessageCommandHandlerAsync.cs:107`). The Polly bypass itself was landed in Phase 2 PLAN-3.1 (`RetryCommandHandlerOutputDecorator` `IRetrySkippable` check, see commit history). `RetryCommandHandlerOutputDecoratorBypassTests` ran clean in the 15-test filter aggregate. End-to-end retry-bypass against a real transient failure deferred to Phase 6 integration tests (CONTEXT-3 line 107 explicitly defers this; PROJECT.md §Success Criteria #8 partial — full integration coverage Phase 6). |
| 6 | `SqlServerExternalDbNameExtractor` uses case-insensitive comparison; `"MyDb"`/`"mydb"` equal, `"MyDb"`/`"MyOtherDb"` unequal | PASS | `dotnet test ... --filter "FullyQualifiedName~SqlServerExternalDbNameExtractorTests"` → `Failed: 0, Passed: 2`. Implementation `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs` normalizes via `connection.Database?.ToUpperInvariant() ?? string.Empty` per CONTEXT-2 PLAN-2.1 normalization convention (equivalent to OrdinalIgnoreCase comparison once both sides are upper-invariant — verified against the 2 unit tests which exercise mixed-case + differing-name cases). |
| 7 | No `SqlConnection` casts in any handler-internal code that leaks into `Transport.RelationalDatabase` | PASS | `grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase/ --include="*.cs" --include="*.csproj"` → zero matches (no output). The deliberate `SqlTransaction` cast inside `SqlServerRelationalProducerQueue.GuardSqlTransaction` and inside the `HandleExternalTx` fork is internal to `Transport.SqlServer` per CONTEXT-3 Decision 2 (lines 90–94) and does NOT leak into the shared abstraction layer. |
| 8 | `SQLServerMessageQueueInit` registers extractor + validator + 3 open-generic producer mappings | PASS | `grep -c "SqlServerRelationalProducerQueue<>" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs` → `3` (the 3 open-generic producer mappings — `IProducerQueue<>`, `IRelationalProducerQueue<>`, `RelationalProducerQueue<>` → `SqlServerRelationalProducerQueue<>`). `grep -c "SqlServerExternalDbNameExtractor"` → `1`. `grep -c "container.Register<ExternalTransactionValidator>"` → `1`. All three counts match plan/CONTEXT-3 §Exit Criterion 8. |

## Test Counts (this phase)

- New tests added in Phase 3: 9 unit (Wave 1: 2 extractor + 7 producer including capability-cast) + 6 smoke (Wave 2: 3 sync fork + 3 async fork) = **15**.
- SqlServer.Tests baseline before Phase 3: **141** (SUMMARY-1.1 §Verification Results).
- SqlServer.Tests after Phase 3: **156** (expected: 141 + 15 = 156, actual: 156). Match.

## Regression Check (sanity)

- `dotnet test Transport.RelationalDatabase.Tests` → `Failed: 0, Passed: 221, Skipped: 0, Total: 221`. Phase 2 foundation unchanged; no regression from Phase 3 work.
- Full SqlServer.Tests suite remains 100% green at 156/156.

## Pre-existing Issues Carry-Forward

- **ISSUE-032** (`Open` per `.shipyard/ISSUES.md` line 269): NU1902 OpenTelemetry advisory escalates to error on `Transport.SQLite` Release CI build only. Out of scope for Phase 3 (does NOT affect Transport.SqlServer Release build, which completes with NU1902 as warning only — confirmed by exit-criterion-1 evidence above). Tracking continues; recommended fix is OpenTelemetry version bump in a future dependency-refresh milestone or `<NoWarn>NU1902</NoWarn>` override on Transport.SQLite csproj.

## Deferrals (by Design, NOT Failures)

The three items below are deferred to Phase 6 integration testing **per CONTEXT-3 and PROJECT.md design**, not because of any Phase 3 implementation gap:

1. **Runtime capability-cast resolution** (Exit Criterion #3): `container.GetInstance<IProducerQueue<MyEvent>>() is IRelationalProducerQueue<MyEvent>` requires building a full SimpleInjector container against a real `IConnectionInformation`; unit tests use the type-system equivalent (`IsAssignableFrom`) to prove the structural property. SUMMARY-1.1 Decision 2 documents the EnableAutoVerification blocker.
2. **Runtime `DbTransaction`/`DbConnection` mock-call-count assertion** (Exit Criterion #4): `Microsoft.Data.SqlClient.SqlConnection`/`SqlTransaction` are sealed and cannot be NSubstitute-mocked. Unit tests use source-grep enforcement; live mock assertions land in Phase 6 against real SqlServer.
3. **End-to-end retry-bypass on forced transient failure** (Exit Criterion #5): The bypass branch is Phase 2 PLAN-3.1 (verified there); Phase 3 confirms the SqlServer producer dispatches into the fork at the structural level. Real transient-failure verification (forced lock conflict / short timeout) is Phase 6.

All three are explicitly called out as Phase-6-scoped in CONTEXT-3.md (lines 105–107) and the ROADMAP.md Phase 6 success criteria.

## Verdict for /shipyard:build orchestrator

**complete** — Phase 3 is fully built and verified. All 8 exit criteria PASS at the level Phase 3 was scoped to deliver (unit + structural + DI). Deferred items are by design and tracked into Phase 6 integration. No new issues, no regressions, no gaps requiring fix-up before Phase 4 begins.
