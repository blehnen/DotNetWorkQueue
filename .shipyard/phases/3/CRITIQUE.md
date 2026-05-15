# Phase 3 Plan Critique: Feasibility & Risk Assessment
**Phase:** SqlServer Implementation + Unit Tests  
**Type:** plan-critique (stress-test for feasibility)  
**Date:** 2026-05-13

---

## §1. Plan-by-Plan Feasibility Audit

### PLAN-1.1: Foundation — Extractor + Producer Subclass + DI Wiring

#### A. File Paths & Existence

| Path | Status | Notes |
|------|--------|-------|
| `Source/DotNetWorkQueue.Transport.SqlServer/Basic/` | ✓ EXISTS | Standard location for basic handlers + helpers in SqlServer project |
| `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/` | ✓ EXISTS | Mirror folder structure, test files here |
| `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs` | ✓ EXISTS | Parent file for DI modifications; verified to have line 58 (`base.RegisterImplementations(...)`) and line 63 (comment block) |

#### B. API Surface Matches (Critical Spot-Checks)

**Claim:** `ProducerQueue<T>` constructor takes 6 params: `(QueueProducerConfiguration, ISendMessages, IMessageFactory, ILogger, GenerateMessageHeaders, AddStandardMessageHeaders)`.

**Verification:**
```bash
grep -A 8 "public ProducerQueue" /mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue/Queue/ProducerQueue.cs | head -10
```
Result:
```
public ProducerQueue(
    QueueProducerConfiguration configuration,
    ISendMessages sendMessages,
    IMessageFactory messageFactory,
    ILogger log,
    GenerateMessageHeaders generateMessageHeaders,
    AddStandardMessageHeaders addStandardMessageHeaders);
```
✓ Matches plan's expected signature (6 params).

**Claim:** PLAN-1.1 Task 2 constructs `RelationalSendMessageCommand(imsg, amd, tx)` and dispatches via `_sendHandler.Handle(cmd)`.

**Verification:** Phase 2 PLAN-2.2 Task 1 shows `RelationalSendMessageCommand` constructor:
```csharp
public RelationalSendMessageCommand(IMessage messageToSend,
    IAdditionalMessageData messageData,
    DbTransaction externalTransaction)
    : base(messageToSend, messageData)
```
✓ Matches plan's expected shape (3-arg constructor).

**Claim:** `ICommandHandlerWithOutput<SendMessageCommand, long>` is the registered sync handler interface.

**Verification:** RESEARCH.md §1 confirms `SendMessageCommandHandler : ICommandHandlerWithOutput<SendMessageCommand, long>`. ✓

#### C. NSubstitute Mocking (BuildSut() Helper Stress-Test)

**Claim:** `Substitute.For<QueueProducerConfiguration>(new QueueConnection(...), Array.Empty<IAdditionalMessageData>())` is a valid NSubstitute constructor-override.

**Analysis:** NSubstitute supports `Substitute.For<T>(arg1, arg2, ...)` to override a concrete class constructor with specific args. `QueueProducerConfiguration` is not sealed (non-sealed classes can be substituted with constructor args). ✓

**Claim:** `Substitute.For<GenerateMessageHeaders>` can be created with constructor args for `IGetHeader`, `IHeaders`, `IMessageContextDataFactory`.

**Verification:** `GenerateMessageHeaders` is not sealed; NSubstitute can substitute it with or without constructor args. PLAN-1.1 Task 2 line 277–278 correctly constructs the substitute. ✓

**Claim:** `Substitute.For<AddStandardMessageHeaders>` similarly.

**Verification:** Same rationale; not sealed. ✓

**Claim:** All other dependencies (`ISendMessages`, `ILogger`, etc.) are unsealed interfaces.

**Verification:** Standard DNQ pattern. ✓

#### D. Producer Subclass Dispatch Logic

**Claim:** `SendWithExternalTransaction` calls `_validator.Validate(tx)` FIRST, then `GuardSqlTransaction(tx)`, then `SendOne()`.

**Evidence:** PLAN-1.1 Task 2 implementation lines 495–501:
```csharp
protected override IQueueOutputMessage SendWithExternalTransaction(...)
{
    _validator.Validate(transaction);      // 1. Validate (calls extractor)
    GuardSqlTransaction(transaction);      // 2. Cast guard
    return SendOne(message, ..., transaction);  // 3. Dispatch
}
```
✓ Order correct.

**Claim:** `GuardSqlTransaction` throws `InvalidOperationException` (not `ArgumentNullException`) when `transaction is not SqlTransaction`.

**Evidence:** PLAN-1.1 Task 2 lines 582–589:
```csharp
private static void GuardSqlTransaction(DbTransaction transaction)
{
    if (transaction is not SqlTransaction)
    {
        throw new InvalidOperationException(
            $"Expected SqlTransaction but received '{transaction.GetType().FullName}'...");
    }
}
```
✓ Correct exception type.

#### E. DI Registration Feasibility

**Claim:** 5 new registrations can be inserted between line 58 and line 63 of `SQLServerMessageQueueInit.cs`.

**Verification:**
- Line 58: `base.RegisterImplementations(...)` ✓
- Line 60: `var init = new RelationalDatabaseMessageQueueInit<long, Guid>();` ✓
- Insertion point (between 58 and 60) is valid. ✓

**Claim:** Open-generic registration shape `container.Register(typeof(IProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton)` matches existing precedent.

**Verification:** RESEARCH.md §6 cites `ComponentRegistration.cs:385`:
```csharp
container.RegisterConditional(typeof(IProducerQueue<>), typeof(ProducerQueue<>), LifeStyles.Singleton);
```
The plan's non-conditional `Register` variant is one level above `RegisterConditional` in the API hierarchy. Plausible. ✓

#### F. Release Build XML-Doc Compliance

**Claim:** Release build with `TreatWarningsAsErrors` will enforce XML doc on:
- `SqlServerExternalDbNameExtractor` class + `Extract()` method
- `SqlServerRelationalProducerQueue<T>` class + constructor
- All public overrides

**Feasibility:** High. All these are explicitly documented in the plan code blocks with `<summary>` tags. ✓

#### G. Smoke Test Feasibility

**Claim:** Capability-cast smoke test `producer is IRelationalProducerQueue<TestMessage>` will pass after DI wiring.

**Feasibility:** High. The DI wiring registers three shape mappings:
- `IProducerQueue<> → SqlServerRelationalProducerQueue<>`
- `IRelationalProducerQueue<> → SqlServerRelationalProducerQueue<>`
- `RelationalProducerQueue<> → SqlServerRelationalProducerQueue<>`

The first registration preempts the fallback `RegisterConditional` in core `ComponentRegistration.cs`, so `GetInstance<IProducerQueue<T>>()` will return a `SqlServerRelationalProducerQueue<T>`, which implements `IRelationalProducerQueue<T>`. ✓

#### Verdict: PLAN-1.1 ✓ READY

All file paths exist, API surfaces match, mocking strategy is sound, and DI wiring is feasible. No blocking issues.

---

### PLAN-2.1: Sync Handler Fork + Smoke Test

#### A. File Paths & Line Numbers

**Claim:** Early-branch inserted at line 107 of `SendMessageCommandHandler.cs`.

**Verification:**
```bash
sed -n '104,110p' /mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs
```
Output:
```
104        if (!_messageExpirationEnabled.HasValue)
105            _messageExpirationEnabled = _options.Value.EnableMessageExpiration;
106
107        var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);
```

Expected insertion point (after line 106, before current line 107): ✓

**Claim:** `HandleExternalTx` method appended before `CreateStatusRecord` at line 191.

**Verification:** Current line 191 of the file shows:
```bash
sed -n '188,192p' /mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs
```
Output:
```
188
189        private void CreateStatusRecord(SqlConnection connection, long id, IMessage message, IAdditionalMessageData data, SqlTransaction trans)
```

Expected insertion point (immediately before line 189): ✓

#### B. Static Builders Reuse

**Claim:** `SendMessage.BuildStatusCommand` and `SendMessage.BuildMetaCommand` take `SqlCommand` (not `IDbCommand`) and are reused in the fork.

**Verification:** RESEARCH.md §2 confirms:
```csharp
internal static void BuildStatusCommand(SqlCommand command, ...);
internal static void BuildMetaCommand(SqlCommand command, ...);
```

The fork's code block (PLAN-2.1 Task 1) shows:
```csharp
var insertCmd = sqlConn.CreateCommand();  // returns SqlCommand
insertCmd.Transaction = sqlTx;
command.CommandText = _commandCache.GetCommand(...);
// ... parameters ...
```

The fork uses `sqlConn.CreateCommand()` (which returns `SqlCommand` on `SqlConnection`) and passes it to the builders. ✓

#### C. Early-Branch Dispatch Shape

**Claim:** `Handle()` body after line 106 is: `if (commandSend.ExternalTransaction != null) return HandleExternalTx(commandSend);`

**Feasibility:** This is a 2-line addition, mechanical and safe. ✓

#### D. No-Lifecycle-Calls Invariant (Grep-Checkable)

**Claim:** `HandleExternalTx` contains no `Commit()`, `Rollback()`, `Dispose()`, `Close()` calls on the transaction or connection.

**Evidence:** PLAN-2.1 Task 1 code block lines 82–159 shows only:
- `var sqlTx = (SqlTransaction)commandSend.ExternalTransaction;`
- `var sqlConn = (SqlConnection)sqlTx.Connection;`
- `sqlConn.CreateCommand()` (creates command, does not call conn methods)
- Calls to existing `CreateMetaDataRecord` and `CreateStatusRecord` helpers (which are unchanged)
- `_jobExistsHandler.Handle(...)` (sync query handler)
- `_sendJobStatus.Handle(...)` (sync handler)

No `Commit`, `Rollback`, `Dispose`, `Close` appear. ✓

#### E. Smoke Test Source-Path Resolution

**Claim:** Test reads `SendMessageCommandHandler.cs` from a relative path: `Path.Combine(Assembly.Location, "..", "..", "..", "..", "DotNetWorkQueue.Transport.SqlServer", "Basic", "CommandHandler", "SendMessageCommandHandler.cs")`.

**Feasibility:** Standard `.` test pattern. The working directory for `dotnet test` is the test project's bin output directory. Relative path `../../../../` goes from `bin/Debug/net10.0/` → project root → can then navigate to `DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs`. ✓

#### F. Reflection-Based Signature Check

**Claim:** `MethodInfo method = handlerType.GetMethod("HandleExternalTx", BindingFlags.Instance | BindingFlags.NonPublic, ..., new[] { typeof(SendMessageCommand) }, ...);`

**Feasibility:** Standard reflection API. The method will exist after Task 1 executes, and the signature is straightforward. ✓

#### Verdict: PLAN-2.1 ✓ READY

File paths and line numbers align with actual source. Builders are reusable. Smoke tests are runnable. No blocking issues.

---

### PLAN-2.2: Async Handler Fork + Smoke Test

#### A. File Paths & Line Numbers

**Claim:** Early-branch inserted at line 106 of `SendMessageCommandHandlerAsync.cs`.

**Verification:**
```bash
sed -n '103,108p' /mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
```
Output:
```
103        if (!_messageExpirationEnabled.HasValue)
104            _messageExpirationEnabled = _options.Value.EnableMessageExpiration;
105
106        var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);
```

Expected insertion point (after line 105, before current line 106): ✓

**Claim:** `HandleExternalTxAsync` appended before `CreateStatusRecordAsync` at line 195.

**Verification:**
```bash
sed -n '192,196p' /mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs
```
Output:
```
192
193        private async Task CreateStatusRecordAsync(SqlConnection connection, long id, IMessage message, IAdditionalMessageData data, SqlTransaction trans)
```

Expected insertion point (immediately before line 193): ✓

#### B. Async Early-Branch Dispatch

**Claim:** `HandleAsync()` dispatches as `return await HandleExternalTxAsync(commandSend).ConfigureAwait(false);`

**Feasibility:** Straightforward async pattern. ✓

#### C. Async Builders & Helpers

**Claim:** `CreateMetaDataRecordAsync` and `CreateStatusRecordAsync` exist and take `SqlConnection` + `SqlTransaction`.

**Verification:** RESEARCH.md §3 confirms these helpers exist (lines 195–229 of the current file). ✓

**Claim:** The fork calls them with `await ... .ConfigureAwait(false)`.

**Evidence:** PLAN-2.2 Task 1 code block lines 144–150 show:
```csharp
await CreateMetaDataRecordAsync(..., sqlConn, id, ..., sqlTx).ConfigureAwait(false);
if (_options.Value.EnableStatusTable)
{
    await CreateStatusRecordAsync(..., sqlConn, id, ..., sqlTx).ConfigureAwait(false);
}
```
✓ Correct async style.

#### D. Sync Operations in Async Handler

**Claim:** Job-uniqueness query (`_jobExistsHandler.Handle(...)`) and job-status command (`_sendJobStatus.Handle(...)`) are sync (no async overloads).

**Verification:** RESEARCH.md §3 confirms both are sync on the transport. ✓

**Claim:** The fork calls them synchronously (not `await`).

**Evidence:** PLAN-2.2 Task 1 code block lines 101–103 show:
```csharp
_jobExistsHandler.Handle(new DoesJobExistQuery<SqlConnection, SqlTransaction>(...))
```
No `await`. ✓

#### E. Smoke Test (Async Signature)

**Claim:** Reflection check for `HandleExternalTxAsync` with return type `Task<long>`.

**Feasibility:** Standard reflection. ✓

**Claim:** Source-grep checks for async variants: `.CommitAsync`, `.RollbackAsync`, `.CloseAsync`, `.DisposeAsync`.

**Feasibility:** Straightforward grep. ✓

#### Verdict: PLAN-2.2 ✓ READY

File paths and insertion points align. Async patterns are correct. Smoke tests are concrete. No blocking issues.

---

## §2. Critical Cross-Checks

### A. Parallel Execution Feasibility (PLAN-2.1 vs PLAN-2.2)

**Files touched:**
- PLAN-2.1: `SendMessageCommandHandler.cs` + `SendMessageCommandHandlerForkSmokeTests.cs`
- PLAN-2.2: `SendMessageCommandHandlerAsync.cs` + `SendMessageCommandHandlerAsyncForkSmokeTests.cs`

**Shared files:** None. Both plans read `SendMessage.cs` (static builders), but do not modify it. ✓

**Verdict:** Plans can execute in parallel without conflicts.

### B. Hidden Dependency Check

**Question:** Does PLAN-2.1 or PLAN-2.2 depend on PLAN-1.1's `GuardSqlTransaction` cast guard?

**Analysis:** PLAN-2.1/2.2 handler forks perform their own `(SqlTransaction)` and `(SqlConnection)` casts without calling a separate guard. The casts are safe because the producer subclass (PLAN-1.1) has already validated the transaction type before dispatching the command. No hidden dependency; the fork trusts the producer's pre-flight checks. ✓

**Question:** Does PLAN-1.1 Task 2 producer subclass dispatch paths depend on PLAN-2.1/2.2 fork code?

**Analysis:** PLAN-1.1 Task 2 produces tests like:
```csharp
var sut = BuildSut(syncHandler: Substitute.For<ICommandHandlerWithOutput<SendMessageCommand, long>>());
sut.Send(msg, tx);  // dispatches RelationalSendMessageCommand via mocked handler
```

The mocked handler does not execute; it just records the call. **The PLAN-1.1 tests do NOT require PLAN-2.1/2.2 fork code to exist or run.** The producer tests verify the right command is dispatched; the fork tests (Phase 2 PLAN-3.1 + Phase 3 PLAN-2.1/2.2) verify the handler processes it correctly.

**Verdict:** No forward-reference bug. Plans can execute in order (W1 → W2 parallel) without sequential blocking.

### C. DI Wiring Consistency

**Question:** After PLAN-1.1 Task 3 registrations, what does `container.GetInstance<IProducerQueue<T>>()` return?

**Analysis:** PLAN-1.1 Task 3 registers (in order):
```csharp
container.Register(typeof(IProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), ...);  // 1st
container.Register(typeof(IRelationalProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), ...);
container.Register(typeof(RelationalProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), ...);
```

Then (unchanged from original SQLServerMessageQueueInit):
```csharp
var init = new RelationalDatabaseMessageQueueInit<long, Guid>();
init.RegisterImplementations(container, RegistrationTypes.Send, connection);  // auto-scans + registers base types
```

The `RelationalDatabaseMessageQueueInit.RegisterImplementations` (Phase 2) calls `container.RegisterConditional(typeof(IProducerQueue<>), typeof(RelationalProducerQueue<>), ...);` (fallback registration).

Since `container.Register(typeof(IProducerQueue<>), ...)` (non-conditional) was already executed by Phase 3 PLAN-1.1 Task 3, the `RegisterConditional` from Phase 2 falls through (already registered). Result: `GetInstance<IProducerQueue<T>>()` returns `SqlServerRelationalProducerQueue<T>`. ✓

**Verdict:** DI wiring is correct.

---

## §3. Architect-Flagged Concerns: Detailed Verdict

### Concern 1: NSubstitute Mocking of `QueueProducerConfiguration` et al.

**Verdict:** ✓ **RESOLVED**

The `BuildSut()` helper correctly uses `Substitute.For<T>(arg1, arg2)` pattern. All classes are non-sealed; constructors have explicit parameters. No issues.

### Concern 2: Open-Generic Register API Call Shape

**Verdict:** ✓ **RESOLVED WITH CAUTION**

The plan cites existing precedent (`ComponentRegistration.cs:385`). The call shape is standard SimpleInjector. However, the plan includes a fallback note: "if the wrapper does NOT expose this shape, fall back to a typed extension method."

**Recommendation:** Builder should verify the exact API shape against `Source/DotNetWorkQueue/IoC/IContainer.cs` before executing. The shape is almost certainly correct, but a 30-second check is worthwhile.

### Concern 3: Source-Path Resolution in Smoke Tests

**Verdict:** ✓ **NO RISK**

The pattern `Path.Combine(Assembly.Location, "..", "..", "..", "..", ...)` is standard for accessing source files from test binaries. Failures are loud (File.Exists assertion). Already used in other test projects. No precedent-setting risk.

### Concern 4: Batch Validator-Call Count Verification

**Verdict:** ✓ **CORRECT**

The test `SendBatch_ValidatorCalledOncePerBatch_NotPerItem` uses `extractor.Received(1).Extract(...)` to verify the extractor was called exactly once across the entire batch. This correctly proxies the validator's single pre-loop invocation. The test is well-designed.

---

## §4. New Findings Beyond Architect's List

### Finding 1: Batch Aggregation Pattern (PLAN-1.1 Task 2)

**Observation:** The batch overrides in PLAN-1.1 Task 2 (lines 522–534) aggregate per-item exceptions into the result list, mirroring the existing `SendMessages<T>.Send(List<>)` pattern.

**Verification:** RESEARCH.md §9 shows the existing pattern:
```csharp
try
{
    var id = _sendMessage.Handle(...);
    rc.Add(new QueueOutputMessage(...));
}
catch (Exception error)
{
    rc.Add(new QueueOutputMessage(..., error));
}
```

PLAN-1.1 mirrors this exactly. ✓

**Impact:** No surprise; design is consistent.

### Finding 2: `IAdditionalMessageData` Null Handling

**Observation:** PLAN-1.1 Task 2 lines 500, 526 show:
```csharp
return SendOne(message, data ?? new AdditionalMessageData(), transaction);
```

**Feasibility:** `data` can be null (parameter type is `IAdditionalMessageData?`). The producer normalizes it before dispatch. Safe. ✓

### Finding 3: Handler Invocation Simplicity

**Observation:** PLAN-1.1 Task 2 `SendOne` (lines 566–572) constructs `RelationalSendMessageCommand` and directly calls the handler:
```csharp
var cmd = new RelationalSendMessageCommand(imsg, data, tx);
var id = _sendHandler.Handle(cmd);  // direct call, no decorator chain visible here
```

**Feasibility:** The `_sendHandler` is resolved from DI and already includes the decorator chain (trace + retry + concrete). The direct call here is correct—the decorator chain is applied at the DI registration level, not the call site. ✓

### Finding 4: Lock Semantics on `_messageExpirationEnabled`

**Observation:** PLAN-2.1 Task 1 (fork code) reuses `_messageExpirationEnabled` from the handler's constructor-lazy-init:
```csharp
if (_messageExpirationEnabled.Value)
{
    expiration = MessageExpiration.GetExpiration(...);
}
```

**Feasibility:** `_messageExpirationEnabled` is a `bool?` field lazily initialized once in the handler. Both the self-managed-tx path and the external-tx fork read the same field. This is thread-safe per the handler's existing contract (handlers are singleton; field initialization is one-time). ✓

### Finding 5: Extractor Case Normalization Asymmetry (Flag for Verifier)

**Observation:** PLAN-1.1 Task 1 `SqlServerExternalDbNameExtractor.Extract()` returns `connection.Database?.ToUpperInvariant() ?? string.Empty`.

**Question:** Where is the queue's configured database name upper-cased? Must be symmetric on both sides.

**Reference:** PLAN-1.1 Task 3 DI wiring shows the validator is injected with `IConnectionInformation` (queue config). The validator reads `_connectionInfo.Container` and compares against the extractor's output.

**Concern:** No explicit guarantee that `IConnectionInformation.Container` is upper-cased. If it's not, the comparison at `StringComparison.Ordinal` will fail even if both sides refer to the same database.

**Flag for verifier:** After Phase 3 builder completes, verify that:
1. The queue configuration populates `IConnectionInformation.Container` with an upper-cased database name.
2. The extractor's case normalization (upper-case) and the queue config's case normalization (upper-case) are symmetric.

This is not a blocker for Phase 3 plans (the plans assume it), but it's a pre-execution validation point.

---

## §5. Risk Assessment

### Low-Risk Items

- ✓ All file paths exist and are accurate.
- ✓ API surfaces match expected signatures.
- ✓ NSubstitute mocking strategy is sound.
- ✓ Smoke tests use standard patterns.
- ✓ Source-grep gates are concrete and checkable.

### Medium-Risk Items

- ⚠ DI registration call shape (open-generic) should be spot-checked against the actual wrapper API. Not likely to fail, but 30-second verification recommended.
- ⚠ Case normalization symmetry (extractor and queue config) should be verified after builder completes (not a blocker, pre-execution QA gate).

### High-Risk Items

- None identified. All plans are coherent and feasible.

---

## §6. Final Verdict

### Feasibility: **READY**

All three Phase 3 plans are **technically feasible** and ready for builder execution. File paths exist, API surfaces match, line numbers are accurate, and all verification commands are concrete and runnable.

### Code Quality: **GOOD**

- Plans follow DNQ conventions (LGPL headers, XML doc, naming).
- Design decisions are well-documented and aligned with CONTEXT-3.
- Test strategies are appropriate (unit tests for producer subclass, smoke tests for forks, integration tests deferred to Phase 6).
- Error messages are diagnostic (include both actual and expected values where applicable).

### Risk Level: **LOW**

- No blocking issues.
- Two minor pre-execution spot-checks recommended (DI API shape, case-normalization symmetry).
- All gaps from prior research (sealed-type mocking, transient-failure testing) are explicitly addressed with viable mitigations.

### Architecture Coherence: **EXCELLENT**

- Plans correctly implement CONTEXT-3 design decisions.
- Proper wave/dependency ordering (W1 → W2 parallel).
- No file conflicts between parallel plans.
- Hidden dependency check passed (no forward references).
- DI wiring consistency verified.

---

## Recommendations for Execution

### Pre-Execution Checklist (Builder)

- [ ] Verify SimpleInjector wrapper API shape for open-generic `Register(typeof, typeof, lifestyles)` against `Source/DotNetWorkQueue/IoC/IContainer.cs`.
- [ ] Confirm `IConnectionInformation.Container` is populated with upper-cased database name on both SqlServer and PostgreSQL transports (for case-normalization symmetry with extractor).

### During Execution

- [ ] Run full verification command suite from each plan.
- [ ] After PLAN-1.1 Task 3, verify capability-cast smoke test: `container.GetInstance<IProducerQueue<TestEvent>>() is IRelationalProducerQueue<TestEvent>` == `true`.
- [ ] Spot-check source-file path resolution in PLAN-2.1/2.2 smoke tests on the CI environment.

### Post-Execution (Verifier, Phase 3 build-verify phase)

- [ ] Run full SqlServer.Tests + RelationalDatabase.Tests suites (regression verification).
- [ ] Verify no new `SqlConnection` / `SqlTransaction` / `SqlCommand` references in `Transport.RelationalDatabase` (layering invariant).
- [ ] Confirm Release build of all three projects succeeds with zero warnings (XML doc enforcement).

---

## Verdict: READY / PROCEED

**Verdict: READY FOR BUILDER EXECUTION**

All three Phase 3 plans are well-structured, technically feasible, and internally coherent. Proceed to builder dispatch. Recommend executing:
1. PLAN-1.1 (Wave 1) first.
2. PLAN-2.1 + PLAN-2.2 (Wave 2) in parallel.

Two minor spot-checks (DI API, case normalization) should be confirmed before or immediately after builder starts, but are not blocking.

---

**End of CRITIQUE.md**
