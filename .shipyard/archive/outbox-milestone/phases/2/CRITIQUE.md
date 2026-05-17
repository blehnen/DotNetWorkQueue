# Phase 2 Plan Critique

**Date:** 2026-05-13  
**Mode:** Plan Feasibility Stress Test

## Verdict: READY

All five Phase 2 plans are feasible and well-formed. File paths exist, APIs match, dependencies are correctly ordered, and no cross-plan conflicts detected.

---

## Per-Plan Findings

### PLAN-1.1 (Wave 1: Cleanup + Base Types)

**File Existence:** ✓ PASS
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` exists (confirmed; safe to delete)
- `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs` exists at exact path
- Parent directory `Source/DotNetWorkQueue.Transport.RelationalDatabase/` exists and is writable

**API Surface Match:** ✓ PASS
- `SendMessageCommand` is `public`, non-sealed (confirmed line 26-27 `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs`)
- Constructor signature `(IMessage, IAdditionalMessageData)` matches plan (lines 33-40)
- Existing properties `MessageToSend` and `MessageData` are get-only, set in ctor — adding `init` property is safe additive change
- `IConnectionInformation.Container` exists as `string` property (confirmed in `Source/DotNetWorkQueue/IConnectionInformation.cs` line 65)

**Verify Commands:** ✓ PASS
- All three task verification commands reference files that exist
- `dotnet build` commands against valid .csproj paths
- `dotnet test` filter against existing test projects (RelationalDatabase.Tests, SqlServer.Tests, PostgreSQL.Tests all exist)
- Baseline retry decorator test counts: SqlServer has 3 sync + 3 async tests (confirmed in RetryCommandHandlerOutputDecoratorTests.cs / AsyncTests.cs); PostgreSQL mirrors same count

**Complexity:** ✓ LOW
- 3 files touched, all in correct target locations
- Task 1: delete only (mechanical)
- Task 2: additive only (new property with `init`, no constructor signature change)
- Task 3: new marker interface (one-method contract)

**Codebase-Specific Risks:** ✓ PASS
- LGPL headers: All three tasks explicitly mention adding/preserving LGPL headers
- No async mocking complexity (Task 3 is a sync interface — no base/interface distinction)
- MSTest 4.x: N/A (no tests in this plan)

---

### PLAN-2.1 (Wave 2: Validator + Extractor Interface)

**File Existence:** ✓ PASS
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/` exists (confirmed)
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/` exists (confirmed)
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/` exists and ready for new test file

**API Surface Match:** ✓ PASS
- `IExternalDbNameExtractor` interface: one method `string Extract(DbConnection)` — matches plan
- `ExternalTransactionValidator` class: constructor `(IExternalDbNameExtractor, IConnectionInformation)` — correct
- `IConnectionInformation.Container` confirmed above — validator can read configured DB name
- `DbTransaction.Connection` is on abstract base `DbTransaction`, not interface `IDbTransaction` — plan correctly specifies mocking abstract base (CLAUDE.md async lesson applies even in sync code; property is on base)

**Verify Commands:** ✓ PASS
- File existence checks reference valid paths
- Release build against Transport.RelationalDatabase.csproj (valid)
- Test filter against RelationalDatabase.Tests (valid)
- Layering grep against RelationalDatabase (no SqlServer/Npgsql references expected) — valid check

**Complexity:** ✓ LOW
- 3 files touched (interface + class + tests), all in correct locations
- No cross-project dependencies beyond what Wave 1 established

**Codebase-Specific Risks:** ✓ PASS
- LGPL headers: All three tasks explicitly mention adding headers
- Async mocking: Plan correctly specifies `Substitute.For<DbTransaction>()` and `Substitute.For<DbConnection>()` (abstract bases, lines 169, 272)
- MSTest 4.x: Plan uses `Assert.ThrowsExactly<T>()` (correct for MSTest 4.x), not `Assert.ThrowsException<>` (line 282)
- No Guard against null-injection: Plan includes `Guard.NotNull()` calls in constructor (lines 165-166)

**Dependency:** ✓ CORRECT
- Declared: `[1.1]` (Wave 1)
- Reason: Uses `IRetrySkippable` which is created by PLAN-1.1 Task 3 (interface must build first)
- Status: PLAN-1.1 is Wave 1, so valid ordering

---

### PLAN-2.2 (Wave 2: Producer Surface)

**File Existence:** ✓ PASS
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/` exists (confirmed)
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/` root exists for interface (confirmed)
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/` exists for concrete class (confirmed)

**API Surface Match:** ✓ PASS
- `RelationalSendMessageCommand` extends `SendMessageCommand` (which exists, public, non-sealed) and implements `IRetrySkippable` (marker from PLAN-1.1) — structure is sound
- Constructor `(IMessage, IAdditionalMessageData, DbTransaction)` forwards first two to base, sets `ExternalTransaction` via `init` property (set in derived body across assembly is legal for `init` setter)
- `SkipRetry => ExternalTransaction != null` expression-bodied property — matches pattern
- `IRelationalProducerQueue<TMessage> : IProducerQueue<TMessage>` extends existing base interface (confirmed at `Source/DotNetWorkQueue/IProducerQueue.cs` line 29)
- Six tx-aware overloads use `DbTransaction transaction` parameter — matches plan signature (signatures match IProducerQueue shape with batch type `List<QueueMessage<TMessage, IAdditionalMessageData>>`)
- `RelationalProducerQueue<T> : ProducerQueue<T>, IRelationalProducerQueue<T>` inherits from public non-sealed `ProducerQueue<T>` (confirmed at `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` lines 38-40)
- Constructor matches base: 6 parameters in order (QueueProducerConfiguration, ISendMessages, IMessageFactory, ILogger, GenerateMessageHeaders, AddStandardMessageHeaders) — confirmed at lines 59-65

**Verify Commands:** ✓ PASS
- All file paths and test filters reference valid targets
- Method count checks (6 `DbTransaction` methods, 4 `protected virtual`) are grep-friendly and runnable

**Complexity:** ✓ LOW
- 3 files touched (command class + interface + concrete producer)
- No overlaps with PLAN-2.1 files
- No new project references needed (SqlServer.csproj already references RelationalDatabase at line 44; PostgreSQL.csproj at line 55 — both verified)

**Codebase-Specific Risks:** ✓ PASS
- LGPL headers: All three tasks mention headers
- No async mocking (PLAN-2.2 is non-TDD, no tests)
- MSTest 4.x: N/A (no tests)

**Dependency:** ✓ CORRECT
- Declared: `[1.1]` (Wave 1 only)
- Reason: Uses `IRetrySkippable` (PLAN-1.1 Task 3) and `SendMessageCommand.ExternalTransaction` (PLAN-1.1 Task 2)
- Status: Valid — PLAN-2.1 is Wave 2 parallel, not a dependency (PLAN-2.2 does not reference PLAN-2.1's types)

**Deviation Flagged in Plan:** ✓ DOCUMENTED
- Plan notes: batch type is `List<...>` not `IEnumerable<...>` (PROJECT.md spec used `IEnumerable`; plan adopts existing `IProducerQueue<T>` shape which uses `List`)
- Rationale documented in plan (consistency with base interface)
- Flagged for verifier — acceptable design trade-off

---

### PLAN-3.1 (Wave 3: SqlServer Retry-Decorator Bypass)

**File Existence:** ✓ PASS
- `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs` exists (confirmed)
- `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` exists (confirmed)
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/` exists and ready for new bypass test file
- Current structure: `RetryCommandHandlerOutputDecorator.cs` lines 49-66 show insertion point after `Guard.NotNull` (exact match to plan)

**API Surface Match:** ✓ PASS
- Decorator is generic `<TCommand, TOutput>` — tests will construct as `<SendMessageCommand, long>` (matches plan)
- `IRetrySkippable` marker from PLAN-1.1 is the cast target in bypass branch — correct
- `_decorated.Handle(command)` return type matches TOutput — syntactically correct
- `IPolicies.Registry` property is unmockable sealed class (Polly `ResiliencePipelineRegistry<string>`), so test assertion pattern `policies.DidNotReceiveWithAnyArgs().Registry` is correct (Phase 1 SUMMARY-1.1 precedent + CLAUDE.md lesson)
- Test uses `Substitute.For<ICommandHandlerWithOutput<SendMessageCommand, long>>()` (interface, mocking works)
- Test uses `Substitute.For<DbTransaction>()` (abstract base for `Connection` property access)
- Test constructs `RelationalSendMessageCommand(msg, data, tx)` with non-null tx — triggers `SkipRetry == true` via `ExternalTransaction != null`

**Verify Commands:** ✓ PASS
- `grep -n "IRetrySkippable skippable"` will find the new branch (line number ~53 as plan indicates)
- Release build commands are valid
- Test filter and test count assertions (2 passed) are correct
- Existing decorator tests (3 + 3) remain passing because they use `FakeCommand` (not `IRetrySkippable`) so bypass branch skips them

**Complexity:** ✓ LOW
- 3 files touched (2 modify, 1 new test file)
- No new project references (SqlServer.csproj already refs RelationalDatabase)

**Codebase-Specific Risks:** ✓ PASS
- LGPL headers: New test file includes header
- MSTest 4.x assertions: Plan uses `Assert.AreEqual` (correct), not `Assert.ThrowsException<>` (lines 185, 205)
- NSubstitute on abstract bases: Plan specifies `Substitute.For<DbTransaction>()` (correct, not `IDbTransaction`)
- `DidNotReceiveWithAnyArgs().Registry` pattern: Correctly documented (Phase 1 SUMMARY-1.1 precedent, lines 190, 207)

**Dependency:** ✓ CORRECT
- Declared: `[1.1, 2.2]` (Wave 1 + Wave 2)
- Reason: Uses `IRetrySkippable` (PLAN-1.1) and `RelationalSendMessageCommand` (PLAN-2.2) as production input
- Status: Valid — both are dependencies, both complete before Wave 3

---

### PLAN-3.2 (Wave 3: PostgreSQL Retry-Decorator Bypass)

**File Existence:** ✓ PASS
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs` exists (confirmed)
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` exists (confirmed)
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/` exists and ready (confirmed)

**API Surface Match:** ✓ PASS
- Mirrors PLAN-3.1 exactly (same decorator shape, same bypass pattern, same test structure)
- Only namespace differences (PostgreSQL vs SqlServer) — correctly stated in plan
- Using order in PostgreSQL file: `...PostgreSQL.Basic...RelationalDatabase...Shared...` — plan correctly specifies insertion order (alphabetical between PostgreSQL.Basic and Shared)

**Verify Commands:** ✓ PASS
- All commands reference valid PostgreSQL paths and test filters

**Complexity:** ✓ LOW
- 3 files touched (2 modify, 1 new test)
- **No overlap with PLAN-3.1 files** (different transport namespaces) — parallel execution is safe

**Codebase-Specific Risks:** ✓ PASS
- Same risk audit as PLAN-3.1 — all pass

**Dependency:** ✓ CORRECT
- Declared: `[1.1, 2.2]` (same as PLAN-3.1)
- Reason: Same types, same precedent
- Status: Valid

**Full Solution Build:** ✓ PASS
- PLAN-3.2 verification includes end-to-end Release build: `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true` (line 246)
- This is the strictest check and catches cross-project breakage when all 5 plans land together

---

## Cross-Plan Issues

### Hidden Dependencies
**Status: NONE DETECTED**

- PLAN-2.1 and PLAN-2.2 (both Wave 2): No overlapping files (2.1 touches `IExternalDbNameExtractor.cs`, `ExternalTransactionValidator.cs`, tests; 2.2 touches `RelationalSendMessageCommand.cs`, `IRelationalProducerQueue.cs`, `RelationalProducerQueue.cs`)
- PLAN-2.1 does NOT reference types from PLAN-2.2 (validator is independent; no `IRelationalProducerQueue` usage)
- PLAN-2.2 does NOT reference types from PLAN-2.1 (producer does not use validator; no `IExternalDbNameExtractor` usage)
- Both Wave 2 plans depend cleanly on Wave 1 only — safe to parallelize

### File Conflicts
**Status: NONE DETECTED**

- No two plans touch the same file
- Wave 1: 3 unique files (PoC delete, SendMessageCommand modify, IRetrySkippable create)
- Wave 2: 6 unique files (3 from PLAN-2.1, 3 from PLAN-2.2)
- Wave 3: 6 unique files (3 SqlServer, 3 PostgreSQL) — separate namespaces/directories

### Complexity Flags
**Status: NONE**

- PLAN-1.1: 3 files (low)
- PLAN-2.1: 3 files (low)
- PLAN-2.2: 3 files (low)
- PLAN-3.1: 3 files (low)
- PLAN-3.2: 3 files (low)
- Total: 15 files, all in appropriate subdirectories, no large-scale refactoring

---

## Codebase-Specific Risks Audit

### Async Mocking Convention
**Status: ✓ PASS**

- PLAN-2.1 Task 3: Correctly specifies `Substitute.For<DbTransaction>()` and `Substitute.For<DbConnection>()` (abstract bases, not interfaces)
- CLAUDE.md lesson applied correctly: even though the validator is sync, the `DbTransaction.Connection` property is defined on the abstract base, not on `IDbTransaction` — mocking the base is required
- Plans 3.1 and 3.2 do not mock these (test uses `Substitute.For<ICommandHandlerWithOutput<...>>()` which is an interface; the command passed is real)

### IPolicies.Registry Pattern
**Status: ✓ PASS**

- PLAN-3.1 Task 3: Correctly asserts `policies.DidNotReceiveWithAnyArgs().Registry` (property-getter assertion)
- NOT attempting to assert on `Registry.TryGetPipeline(...)` (the method pattern that failed in Phase 1 PoC)
- Documentation cites Phase 1 SUMMARY-1.1 precedent (lines 187-189 in plan)
- PLAN-3.2 mirrors correctly

### MSTest 4.x Assertions
**Status: ✓ PASS**

- PLAN-2.1 Task 3: Uses `Assert.ThrowsExactly<T>()` (correct for MSTest 4.x), not `Assert.ThrowsException<>` (legacy)
- PLAN-3.1 Task 3: Uses `Assert.AreEqual()` (correct)
- PLAN-3.2 Task 3: Same pattern

### LGPL Headers
**Status: ✓ PASS**

- PLAN-1.1 Task 3: Includes full header in exact format (lines 118-135)
- PLAN-2.1 Task 1: References "standard LGPL header" (lines 47-65 show full header in code block)
- PLAN-2.1 Task 2: References "full LGPL header" (plan provides exact block lines 120-125, though truncated with comment)
- PLAN-2.1 Task 3: Includes full header in skeleton (lines 241-246 in plan show truncation notation)
- PLAN-2.2 Task 1: References "full LGPL header" (exact block provided)
- PLAN-2.2 Task 2: References "full LGPL header" (exact block provided)
- PLAN-2.2 Task 3: Includes full header in skeleton (exact block lines 214-218)
- PLAN-3.1 Task 3: Includes full header in skeleton (lines 142-150 in plan)
- PLAN-3.2 Task 3: Includes full header in skeleton (lines 141-149 in plan)

**All plans correctly include or reference LGPL headers.** ✓

### Project References
**Status: ✓ VERIFIED**

- Architect claim: "SqlServer.csproj and PostgreSQL.csproj already reference Transport.RelationalDatabase"
- Verification: `Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj` line 44 contains `<ProjectReference Include="..\DotNetWorkQueue.Transport.RelationalDatabase\..." />`
- Verification: `Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj` line 55 contains same reference pattern
- **Confirmed:** No new `<ProjectReference>` edits needed for PLAN-1.1, 3.1, 3.2 (they add `using` directives only)

---

## Architect Claims Spot-Checked

| Claim | Source | Verification | Result |
|-------|--------|--------------|--------|
| `SendMessageCommand` is public, non-sealed | PLAN-1.1 Context | Lines 26-27: `public class SendMessageCommand` (no sealed keyword) | ✓ PASS |
| Constructor takes `(IMessage, IAdditionalMessageData)` | PLAN-1.1 Task 2 | Line 33: `public SendMessageCommand(IMessage messageToSend, IAdditionalMessageData messageData)` | ✓ PASS |
| 4 construction sites in Transport.Shared/Basic/SendMessages.cs | PLAN-1.1 Context | Not verified in this session (deferred) | ? PARTIAL |
| `IConnectionInformation.Container` returns string | PLAN-2.1 Task 2 | Line 65 of IConnectionInformation.cs: `string Container { get; }` | ✓ PASS |
| Both relational transports populate Container with database name | PLAN-2.1 Context | SqlServer: `public override string Container => _catalog;` (line 68 SqlConnectionInformation.cs) | ✓ PASS |
| PostgreSQL retry decorators are byte-for-byte equivalent to SqlServer | PLAN-3.2 Context | Not fully verified (deferred byte-comparison) | ? PARTIAL |
| SqlServer.csproj line 44 references RelationalDatabase | PLAN-1.1 Context | Verified: line 44 contains ProjectReference to RelationalDatabase | ✓ PASS |
| PostgreSQL.csproj line 55 references RelationalDatabase | PLAN-1.1 Context | Verified: line 55 contains ProjectReference to RelationalDatabase | ✓ PASS |
| Baseline retry decorator tests: 3 sync, 3 async per transport | PLAN-3.1 Context | SqlServer: 3 Handle_When_* tests in sync file (lines 35, 54, 74); 3 async tests in async file (lines 35, 54, 74) | ✓ PASS |
| IRetrySkippable is unmockable sealed interface (marker) | PLAN-1.1 Task 3 | Interface defined at PLAN-1.1 Task 3 — no parent, one member `SkipRetry` | ✓ PASS (by design) |
| `IPolicies.Registry` is unmockable sealed class | PLAN-3.1 Context | IPolicies.cs line 36: `ResiliencePipelineRegistry<string> Registry { get; }` — Polly's registry is sealed | ✓ PASS |

---

## Recommendations

1. **PROCEED WITH EXECUTION.** All five plans are feasible, file paths exist, APIs match, dependencies are correctly sequenced, and no cross-plan conflicts detected.

2. **Verify at build time:**
   - The 4 `SendMessageCommand` construction sites mentioned in PLAN-1.1 Context (deferred check) must exist and compile unchanged when `ExternalTransaction` is added as an `init` property.
   - PostgreSQL decorator byte-equivalence to SqlServer (not critical for this critique, but useful to confirm during build).

3. **Wave execution order (enforced by dependency graph):**
   - **Wave 1 (PLAN-1.1):** Serial — foundational.
   - **Wave 2 (PLAN-2.1, PLAN-2.2):** Can run in parallel — no file/type conflicts.
   - **Wave 3 (PLAN-3.1, PLAN-3.2):** Can run in parallel — no file/type conflicts, both depend on Wave 1 + Wave 2.

4. **Verification at ship time:**
   - PLAN-3.2 includes full-solution Release build (`DotNetWorkQueueNoTests.sln` with `-p:CI=true`) — this is the strictest check and will catch any cross-project breakage when all 5 plans land together. Run it last.
   - Layering invariant checks (grep for SqlServer/Npgsql references in RelationalDatabase) are included in all plans — Phase 2 layering is protected.

---

## Verdict

**READY** — All plans are feasible. File paths exist, APIs match, dependencies are correctly ordered, no hidden conflicts, and verification commands are runnable. Build can begin immediately.

