# Combined Review: Plans 2.4, 2.5, 2.6

## Verdict: PASS

All three plans implement focused unit tests for `SendJobToQueue` derivatives across SqlServer, PostgreSQL, and SQLite transports. Each file compiles, tests pass per the summaries (4/4, 7/7, 4/4), and the `DoesJobExist` / `DeleteJob` protected-method coverage is correctly exercised. Both exposure strategies (reflection vs. Testable subclass) are acceptable; subclassing is slightly preferred but reflection is fine for a single SUT.

---

## Plan 2.4 Findings (SqlServerSendJobToQueueTests.cs)

### Stage 1: Spec Compliance — PASS
- `DoesJobExist_DelegatesToQueryHandler_ReturnsResult` (lines 38-53): verifies return value pass-through and single call — PASS.
- `DoesJobExist_PassesCorrectQueryArguments` (lines 55-73): asserts `JobName` + `ScheduledTime` on the query via `Arg.Is` matcher — PASS.
- `DeleteJob_RetrievesJobIdAndRemovesMessageWithErrorReason` (lines 75-94): asserts `GetJobIdQuery.JobName`, the `MessageQueueId<long>` wrapping of the id, and `RemoveMessageReason.Error` — PASS.
- `Constructor_AssignsDependenciesWithoutThrowing` (lines 96-105): sanity check plus `ASendJobToQueue` base-type assertion — PASS.

### Stage 2: Code Quality
No critical or important findings.

**Suggestions:**
- Line 92: the inline cast `is MessageQueueId<long> && (long)((MessageQueueId<long>)id).Id.Value == expectedId` is readable but slightly dense; a pattern match `id is MessageQueueId<long> mq && (long)mq.Id.Value == expectedId` would be cleaner. Non-blocking.
- Reflection-based invocation (lines 126-138) couples tests to method names. A `TestableSqlServerSendJobToQueue` subclass (as used in 2.5/2.6) would be more refactor-safe, but either approach is acceptable.

---

## Plan 2.5 Findings (PostgreSqlSendJobToQueueTests.cs)

### Stage 1: Spec Compliance — PASS
- `Constructor_Creates_Instance` — PASS.
- `DoesJobExist_DelegatesToQueryHandler` (lines 114-129): return-value + call-count verified — PASS.
- `DoesJobExist_PassesCorrectQuery` (lines 131-149): captures query via `Arg.Do` and asserts `JobName`/`ScheduledTime` — PASS.
- `DeleteJob_RetrievesJobIdAndRemovesMessage` (lines 151-183): captures both `GetJobIdQuery` and the `IMessageId` passed to `RemoveMessage.Remove`, asserts `RemoveMessageReason.Error`, and verifies `capturedId.Id.Value == expectedJobId` — PASS.
- `JobAlreadyExistsError_True_For_Duplicate_Key_Jobname` (lines 185-193): exercises the `"duplicate key"` + `"jobname_idx"` branch — PASS.
- `JobAlreadyExistsError_True_For_Failed_To_Insert_Message` (lines 195-203): exercises the `"Failed to insert record"` branch — PASS.
- `JobAlreadyExistsError_False_For_Other_Error` (lines 205-213): negative branch — PASS.

### Stage 2: Code Quality
No critical or important findings.

**Suggestions:**
- The `JobAlreadyExistsError_*` tests rely on exact English substrings in `Exception.Message`. If the PostgreSQL handler's substring list ever changes, these tests will silently rot against the production strings. Acceptable given the handler's implementation is also hardcoded to those substrings.
- `Fixture` class is reasonable but `CreateFixture` creates a real `QueueProducerConfiguration` plus a real `Policies` and `ResiliencePipelineRegistry` that the tests don't actually exercise. That heavyweight setup is inert noise — could be stripped to just `Queue.Configuration.Returns(Substitute.For<QueueProducerConfiguration>())` if needed, or omitted entirely if the SUT doesn't read it in these paths. Non-blocking.
- `JobAlreadyExistsError_False_For_Other_Error` uses `"connection refused"` which happens to contain no trigger substrings. Consider adding a second negative case (empty message / null-message guard) for robustness.

---

## Plan 2.6 Findings (SqliteSendToJobQueueTests.cs)

### Stage 1: Spec Compliance — PASS
- `Create_Default` (lines 17-22): instance sanity — PASS.
- `DoesJobExist_DelegatesToQueryHandler` (lines 24-37): uses `QueueStatuses.Processed`, verifies return + call count — PASS.
- `DoesJobExist_PassesCorrectQuery` (lines 39-56): captures query, asserts name + scheduled time — PASS.
- `DeleteJob_RetrievesJobIdAndRemovesMessage` (lines 58-92): captures `GetJobIdQuery.JobName`, captures the `IMessageId`, asserts `RemoveMessageReason.Error` and `MessageQueueId<long>` with `HasValue == true` and correct ToString — PASS.

### Stage 2: Code Quality
No critical or important findings.

**Suggestions:**
- Line 90: `Assert.AreEqual(jobId.ToString(), typed.ToString())` works because `MessageQueueId<long>.ToString()` returns the id string. Slightly indirect — a direct assertion like `Assert.AreEqual(jobId, (long)typed.Id.Value)` would be more explicit. Non-blocking; the summary explains the `ISetting` vs `Setting<T>` typing rationale.
- `Deps`/`TestableSqliteSendToJobQueue` nested-class pattern is clean and preferred over reflection.

---

## Summary
**Verdict:** APPROVE
All 15 tests across the three plans are well-scoped, deterministic, mock-driven, and exercise the intended protected-method surface. No security, correctness, or coverage concerns. Suggestions listed are cosmetic.

Critical: 0 | Important: 0 | Suggestions: 6
