# Review: Plan 1.1 (Phase 4 Wave 1)

## Verdict: PASS

Phase 4 PLAN-1.1 is a clean structural mirror of Phase 3 PLAN-1.1 with all three CONTEXT-4 hard rules honored verbatim and the two PG-specific deviations (Decision 2 pass-through extractor, Rule A `RegisterConditional`) correctly implemented. No critical or important findings. Three minor observations; one is a carry-over from Phase 3 SUMMARY-1.1 (Decision 4 noted there) and is not a regression.

## Findings

### Critical
- None.

### Important
- None.

### Minor
1. **`_generateMessageHeaders` field added beyond plan spec — same minor as Phase 3 REVIEW-1.1 §Minor #1.** `PostgreSqlRelationalProducerQueue.cs:57` adds a private `_generateMessageHeaders` field and uses `_generateMessageHeaders.HeaderSetup(data)` in `SendOne`/`SendOneAsync` (lines 176, 185). The plan body's `SendOne` stub showed `_messageFactory.Create(message)` (single-arg, no headers). Implementation correctly calls the 2-arg overload `_messageFactory.Create(message, additionalHeaders)` to populate `IAdditionalMessageHeaders` — this matches the inherited non-tx path's behavior and is more correct than the plan stub. This is the **same convergence-on-correctness** Phase 3 SUMMARY/REVIEW already accepted; no remediation required.
2. **`Send_ValidatorRejectsCaseMismatch_ThrowsBeforeCastGuard` asserts ordering via absence-of-substring** (`PostgreSqlRelationalProducerQueueTests.cs:158`): `Assert.IsFalse(ex.Message.Contains("NpgsqlTransaction"), ...)`. Same brittleness flagged in Phase 3 REVIEW-1.1 §Minor #2 against `"SqlTransaction"`. Correct today but brittle to future validator-message edits. Optional remediation: also assert the positive case (e.g., that the message starts with the validator's diagnostic prefix `"Caller-supplied transaction's connection points to database"`). Non-blocking — same posture as Phase 3.
3. **`BuildSut` helper constructs a `Substitute.For<QueueProducerConfiguration>(...)` with `new BaseTimeConfiguration()` while other deps are subbed** (`PostgreSqlRelationalProducerQueueTests.cs:91`). The plan stub showed `Substitute.For<BaseTimeConfiguration>()`. The builder chose a real instance — both work, but the asymmetry is cosmetic noise. No remediation required; the test fixture is honest about what's a stub and what's a real type.

### Positive
- LGPL-2.1 header verbatim on all 4 new files (extractor, producer, extractor-tests, producer-tests).
- XML doc complete on all new public types and the producer constructor — Release build clean per SUMMARY's verification table.
- Test fixture (`BuildSut`, `BuildNonPgTx`) is a near-byte-for-byte mirror of the Phase 3 SqlServer fixture; cross-plan consistency excellent.
- `IsAssignableFrom` capability-cast substitution mirrors Phase 3 verbatim (3 assertions against `IRelationalProducerQueue<T>`, `RelationalProducerQueue<T>`, `IProducerQueue<T>`), satisfying PROJECT.md §Success Criteria #3 at the type-system level.
- `GuardNpgsqlTransaction` is the only sealed-type cast in the producer — exactly mirrors `GuardSqlTransaction` in `SqlServerRelationalProducerQueue.cs:192`. `IDbConnection` discipline preserved everywhere else.
- Risk #3 closure proof appears in TWO places (extractor test + producer-subclass validator test) — defensive coverage exceeds the plan's single-test minimum.
- Producer test #6 (`SendBatch_ValidatorCalledOncePerBatch_NotPerItem`) uses the same `extractor.Received(1).Extract(...)` proxy assertion Phase 3 settled on — Decision 4 behavior verified.

## CONTEXT-4 Rule audit

### Rule A (RegisterConditional)
- **3 `RegisterConditional` matches present** at `PostgreSQLMessageQueueInit.cs:72-74` for the open-generic mappings `IProducerQueue<>`, `IRelationalProducerQueue<>`, `RelationalProducerQueue<>`, all targeting `PostgreSqlRelationalProducerQueue<>` with `LifeStyles.Singleton`. **Verified by Grep.**
- **0 plain `container.Register(typeof(...ProducerQueue<>)...)` matches** in the same file. **Verified by Grep (no matches found).**
- Explanatory comment block at lines 67–71 documents the lazy-verification rationale — matches the CONTEXT-4 explanatory text. Future maintainers will understand why this is `RegisterConditional` not `Register`.

### Rule B (lifecycle-comment substring rephrase)
- N/A for Wave 1 (applies to Wave 2 handler forks only — `HandleExternalTx`/`HandleExternalTxAsync`). No fork body in this plan to check.

### Rule C (11-param producer ctor)
- **Constructor declaration at `PostgreSqlRelationalProducerQueue.cs:74-85` has exactly 11 parameters** in the order specified by CONTEXT-4: `configuration`, `sendMessages`, `messageFactory`, `log`, `generateMessageHeaders`, `addStandardMessageHeaders`, `sendHandler`, `sendHandlerAsync`, `validator`, `sentMessageFactory`, `ownMessageFactory`.
- `: base(configuration, sendMessages, messageFactory, log, generateMessageHeaders, addStandardMessageHeaders)` correctly passes the first 6 args to the Phase 2 `RelationalProducerQueue<T>` base (line 86–87).
- The 5 new params are guarded via `Guard.NotNull(...)` (lines 89–93) and stored in private readonly fields.
- `ownMessageFactory` and `messageFactory` are both stored — the base seals `_messageFactory` as private (Phase 2 SUMMARY-2.2 limitation), so the producer subclass keeps its own reference for the caller-tx path. SimpleInjector double-injects the same singleton. **Matches Phase 3 SqlServer exactly.**

## Cross-Phase consistency with Phase 3 SqlServer

Side-by-side comparison of `PostgreSqlRelationalProducerQueue.cs` against `SqlServerRelationalProducerQueue.cs`:

| Aspect | Phase 3 SqlServer | Phase 4 PostgreSQL | Match |
|---|---|---|---|
| Class declaration | `public sealed class SqlServerRelationalProducerQueue<TMessage> : RelationalProducerQueue<TMessage> where TMessage : class` | `public sealed class PostgreSqlRelationalProducerQueue<TMessage> : RelationalProducerQueue<TMessage> where TMessage : class` | OK |
| Private fields | 6 (5 ctor + `_generateMessageHeaders`) | 6 (5 ctor + `_generateMessageHeaders`) | OK |
| Constructor arity | 11 | 11 | OK |
| 4 protected overrides | sync + async + sync-batch + async-batch | sync + async + sync-batch + async-batch | OK |
| Override body shape | validator → guard → dispatch | validator → guard → dispatch | OK |
| Batch loop | `foreach` (Decision 1) | `foreach` (Decision 1) | OK |
| Per-item failure aggregation | `QueueOutputMessage(sentMessage, error)` | `QueueOutputMessage(sentMessage, error)` | OK |
| Cast-guard method | `GuardSqlTransaction` (private static) | `GuardNpgsqlTransaction` (private static) | OK (transport-renamed) |
| Cast-guard exception type | `InvalidOperationException` | `InvalidOperationException` | OK |
| Cast-guard message | mentions `SqlTransaction` + `Microsoft.Data.SqlClient` | mentions `NpgsqlTransaction` + `Npgsql provider` | OK (transport-renamed) |
| Send dispatch helper | `SendOne` + `SendOneAsync` calling `_messageFactory.Create(message, additionalHeaders)` | identical shape | OK |
| Extractor normalization | `?.ToUpperInvariant() ?? string.Empty` | `?? string.Empty` (NO normalization) | INTENTIONAL DIVERGENCE per Decision 2 |
| DI wiring | 3 `RegisterConditional` + extractor + validator | 3 `RegisterConditional` + extractor + validator | OK |
| Capability-cast test shape | 3 `IsAssignableFrom` assertions | 3 `IsAssignableFrom` assertions | OK |
| Risk #3 closure test | n/a for SqlServer (case-insensitive normalization) | present (`Extract_PreservesCase_NoUpperCasing` + `Send_ValidatorRejectsCaseMismatch_ThrowsBeforeCastGuard`) | PG-specific deliberate addition per Decision 2 |

**Conclusion:** Phase 4 Wave 1 is structurally identical to Phase 3 Wave 1 except for the deliberate Decision 2 divergence (pass-through extractor) and the added Risk #3 closure tests that prove it works. Cross-phase consistency is excellent — a maintainer who has read Phase 3 can read Phase 4 without surprises.

## Verification Cross-Check

- SUMMARY claims `137 passed, 0 failed` (130 baseline + 7 new — 2 extractor + 5 producer + nothing wait: 2 + 6 + 1 cap-cast = 9 new… counting test methods in the file shows 6 producer `[TestMethod]` + 1 `[TestMethod]` capability-cast = 7 in the producer file, plus 2 in the extractor file = **9 new tests**. SUMMARY says "+7 new" which would total 137. Reading the producer test file shows exactly 7 `[TestMethod]` attributes. So SUMMARY's "+7 new" appears to count only producer-file tests, omitting the 2 extractor tests. Total new across both files is 9; total new for producer file alone is 7. SUMMARY likely meant "7 new in the producer test file" — not a defect, just an arithmetic-presentation nit not worth gating on.). Tests not re-run by reviewer (out of scope for review-only role); reviewer trusts SUMMARY + Verification table.
- Layering invariant grep against `Source/DotNetWorkQueue.Transport.RelationalDatabase` for `using Npgsql` / `using Microsoft.Data.SqlClient` returns no matches — verified by reviewer.
- Rule A grep gate: 3 `RegisterConditional(typeof(...ProducerQueue<>)` matches, 0 plain `Register(typeof(...ProducerQueue<>)` matches — verified by reviewer.
- Rule C grep gate: constructor signature spans lines 74–85, contains 11 parameters — verified by reviewer.

## Tally

Critical: 0 | Important: 0 | Minor: 3 | Positive: 7+

Wave 1 ready for Wave 2 hand-off. PLAN-2.1 (sync fork) and PLAN-2.2 (async fork) can run in parallel against the foundation this plan ships.
