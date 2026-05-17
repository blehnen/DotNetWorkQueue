# Review: Plan 1.1 (Phase 3 Wave 1)

## Verdict: PASS

Both Wave 1 deviations are sound. Stage 1 spec compliance clean; Stage 2 findings minor and non-blocking.

## Findings

### Critical
- None.

### Important
- None.

### Minor
1. **`SqlServerRelationalProducerQueue.cs` adds `_generateMessageHeaders` field not explicitly in plan spec.** Implementation captures `generateMessageHeaders` for use in `SendOne`/`SendOneAsync` (`_generateMessageHeaders.HeaderSetup(data)`) — plan's `SendOne` stub called `_messageFactory.Create(message)` (single-arg) without header generation. Implementation calls the 2-arg `_messageFactory.Create(message, additionalHeaders)` which is more correct (matches the inherited non-tx path). Wave 2 plan reviewer should verify the handler fork consumes the populated headers on `RelationalSendMessageCommand.MessageToSend`.
2. **`Send_ValidatorRejectsDbMismatch_ThrowsBeforeCastGuard` asserts ordering via absence-of-substring.** Test asserts the exception message does NOT contain `"SqlTransaction"`. Correct now but brittle to future validator-message edits. Optional remediation: assert the prefix `"Caller-supplied transaction's connection points to database"`.
3. **`StringAssert.Contains(ex.Message, QueueDb)` matches because `QueueDb = "MYDB"`** and the validator's message includes both names. Correct.

### Positive
- LGPL-2.1 headers on all 4 new files.
- XML doc satisfies `TreatWarningsAsErrors` + XML doc gen (Release build 0 errors).
- Layering invariant intact: `Transport.RelationalDatabase` has no SqlServer/Npgsql refs.
- `IDbConnection`/`DbConnection` abstraction discipline preserved — the only sealed-type cast is the deliberate `transaction is not SqlTransaction` boundary check in `GuardSqlTransaction`.
- `RelationalProducerQueue<T>` base class's 4 `protected virtual` hooks are overridden cleanly.
- `ContainerWrapper.RegisterConditional` predicate `c => !c.Handled` matches `ComponentRegistration.RegisterFallbacks:385` — canonical SimpleInjector preemption pattern.

## Deviation Audit

### Register → RegisterConditional
- **Reasoning:** plain `Register` triggered SimpleInjector's `EnableAutoVerification` on first resolve, surfacing pre-existing repo-wide diagnostic warnings on transient `IDisposable` types (`IMessageContext`, `IWorker`, `IPrimaryWorker`) and breaking 6 pre-existing `QueueCreatorTests`. `RegisterConditional` preserves lazy verification semantics matching the existing fallback pattern.
- **Verification:** `ContainerWrapper.cs:179-183` applies predicate `c => !c.Handled` — Phase 3 registers first and claims the open-generic slot; the fallback in `ComponentRegistration.cs:385` runs later, finds `c.Handled = true`, and skips. Resolution still returns `SqlServerRelationalProducerQueue<>`.
- **Risk for Phase 4 (PostgreSQL):** must mirror this exact pattern.
- **Risk for Phase 5 (non-relational negative-path tests):** none — no transport-specific `RegisterConditional` claims `IProducerQueue<>` on Memory/Redis/LiteDb/SQLite, so the fallback fires.
- **Sound: YES.**

### CreateProducer<> runtime test → type-system check
- **Reasoning:** plan's `factory.CreateProducer<TestMessage>(...)` + `is IRelationalProducerQueue<TestMessage>` is blocked by the same `EnableAutoVerification` issue. Substituted with three `IsAssignableFrom` assertions covering `IRelationalProducerQueue<TestMessage>`, `RelationalProducerQueue<TestMessage>`, and `IProducerQueue<TestMessage>`.
- **What it proves:** the type implements the right capability chain; DI registrations resolve to it (verified via grep gate: 3 producer mappings + 1 extractor + 1 validator).
- **What it doesn't prove:** runtime SimpleInjector resolution returns the SqlServer-specific type. Deferred to Phase 6 integration tests against real SqlServer (CONTEXT-3 §Exit Criteria #3 explicitly accepts this Wave 1 + Phase 6 split).
- **Risk:** if SimpleInjector's preemption logic changes in a future major bump, the static check would pass while runtime would silently fall back. Phase 6 catches this; risk is bounded.
- **Sound: YES** (conditional on Phase 6 integration coverage landing as scheduled).

## Wave 2 Hand-off Notes

- Handler fork path (`HandleExternalTx`/`HandleExternalTxAsync`) must assume the incoming `RelationalSendMessageCommand.MessageToSend` already has `IAdditionalMessageHeaders` populated by the producer's `_messageFactory.Create(message, additionalHeaders)` call. The fork should NOT re-populate headers.
- Validator runs at the producer surface (Wave 1) — handler fork does NOT need to re-validate.
- `GuardSqlTransaction` runs AFTER validator in Wave 1 — handler fork can rely on `command.ExternalTransaction is SqlTransaction`.

## Tally

Critical: 0 | Important: 0 | Minor: 3 | Positive: 6+
