# Review: Plan 2.2

## Verdict: PASS

## Stage 1: Spec Compliance — PASS

### Task 1: `RelationalSendMessageCommand` derived class — PASS
- File: `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs` (56 lines).
- Evidence:
  - LGPL header lines 1-18 verbatim.
  - Namespace `DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command` (line 22) — matches plan, file under `Basic/Command/` matches the existing 11 sibling command files in that folder.
  - Class declared `public class RelationalSendMessageCommand : SendMessageCommand, IRetrySkippable` (line 31).
  - Constructor `(IMessage messageToSend, IAdditionalMessageData messageData, DbTransaction externalTransaction)` (lines 42-44), forwards first two to base via `: base(messageToSend, messageData)` and assigns `ExternalTransaction = externalTransaction` in body (line 47).
  - `SkipRetry => ExternalTransaction != null` expression-bodied property (line 54). `grep -c "SkipRetry => ExternalTransaction != null"` returns 1.
  - Cross-assembly `init` setter assignment compiles cleanly — confirmed by SUMMARY Release build 0 errors, also per the architect note in PLAN-2.2 §Task 1.
  - XML `<summary>` on class + `<summary>` + `<param>` docs on the constructor + `<summary>` on the property — all 4 acceptance criteria met.

### Task 2: `IRelationalProducerQueue<TMessage>` interface — PASS
- File: `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` (101 lines).
- Evidence:
  - LGPL header lines 1-18 verbatim.
  - File placed at project root (line 24: `namespace DotNetWorkQueue.Transport.RelationalDatabase`), sibling to existing `IRetrySkippable.cs` and `IExternalDbNameExtractor.cs` (Wave 1 / PLAN-2.1) — matches plan and convention for top-level interfaces.
  - Interface declared `public interface IRelationalProducerQueue<TMessage> : IProducerQueue<TMessage>` (line 40) with `where TMessage : class` constraint on line 41 (matches base `IProducerQueue<T>` constraint).
  - Exactly 6 method overloads with `DbTransaction transaction` parameter — `grep -c "DbTransaction transaction"` returns 6 (lines 51, 61, 69, 78, 91, 99). 4 single-message (sync no-data, sync with-data, async no-data, async with-data) + 2 batch (sync, async).
  - Each method has `<summary>` XML doc; all six also have full `<param>` + `<returns>` coverage.
  - Batch parameter type is `List<QueueMessage<TMessage, IAdditionalMessageData>>` per the plan's documented deviation from PROJECT.md's `IEnumerable`. Deviation is explicitly called out in the inline `<remarks>` on the batch overload (lines 86-90) so the divergence is discoverable from the public surface, not just hidden in the plan. **See "Batch type deviation" assessment below.**

### Task 3: `RelationalProducerQueue<T>` concrete class — PASS
- File: `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs` (161 lines).
- Evidence:
  - LGPL header lines 1-18 verbatim.
  - Class declared `public class RelationalProducerQueue<T> : ProducerQueue<T>, IRelationalProducerQueue<T> where T : class` (lines 39-40) — public so Phase 3/4 can subclass, generic constraint matches the interface and the base.
  - Constructor (lines 54-63) has exactly 6 parameters mirroring `ProducerQueue<T>` (`QueueProducerConfiguration`, `ISendMessages`, `IMessageFactory`, `ILogger`, `GenerateMessageHeaders`, `AddStandardMessageHeaders`) and forwards all 6 to base via `: base(...)`. Confirmed against the canonical 6-param ctor at `Source/DotNetWorkQueue/Queue/ProducerQueue.cs:59`.
  - 6 `public` interface-implementation overloads (lines 68, 72, 76, 80, 84, 88) — each a one-line expression-bodied delegation to one of the 4 protected virtual hooks. `grep -c "DbTransaction transaction"` over the file returns 10 (6 public + 4 protected = correct).
  - 4 `protected virtual` hooks (lines 105, 121, 135, 149) — `grep -c "protected virtual"` returns 4 exactly, matching the verification expectation. Each hook throws `InvalidOperationException(NotConfiguredMessage())` (lines 108, 124, 138, 152).
  - `NotConfiguredMessage()` (lines 155-159) returns the exact wording specified in CONTEXT-2 Decision 3 / Plan §Task 3: "Caller-supplied-transaction send is not implemented for this transport. Override SendWithExternalTransaction (and the batch + async variants) in a transport-specific subclass, or resolve a SqlServer/PostgreSQL producer that already does."
  - Routing semantics correct: both single-message public overloads (with and without `IAdditionalMessageData`) route to `SendWithExternalTransaction`, passing `null` for `data` when invoked from the no-data overload. Same shape for async. Batch overloads route to `SendWithExternalTransactionBatch[Async]` unchanged. Matches plan §Task 3 routing description verbatim.
  - Existing non-tx `Send`/`SendAsync` overloads from base `ProducerQueue<T>` are NOT overridden — the existing self-managed path is unchanged, consistent with the CONTEXT-2 Hard Rule "no regressions on existing producers".
  - Class is in namespace `DotNetWorkQueue.Transport.RelationalDatabase.Basic` (line 28) — matches plan's `Basic/` file placement.

### Cross-cutting Stage 1 checks — PASS
- All 3 file paths match plan §Files exactly. No stray or duplicate files.
- SUMMARY verification table confirms all 7 gates: files exist, Release build 0 errors, `DbTransaction transaction` count = 6 in interface, `protected virtual` count = 4 in concrete, `SkipRetry => ExternalTransaction != null` grep = 1, layering grep empty, 221/0/0 in `Transport.RelationalDatabase.Tests`.
- Cross-plan interleave with PLAN-2.1: re-verified that the PLAN-2.1 files (`IExternalDbNameExtractor.cs` at project root, `ExternalTransactionValidator.cs` under `Basic/`) and PLAN-2.2 files do not collide. The 3 new PLAN-2.2 files occupy distinct paths from the 3 PLAN-2.1 files. SUMMARY notes a transient Release-build error during the in-flight interleave that self-cleared once PLAN-2.1 Task 2 landed — final state is clean. No merge artifacts, stray files, or `.orig`/`.rej` patches found.
- Layering invariant holds: `grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase` returns no matches. None of the 3 new files reference sealed transport types.

## Stage 2: Code Quality

### Critical
- None.

### Important
- None.

### Suggestions
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs:42-48` — Constructor accepts a possibly-null `DbTransaction externalTransaction` with no `Guard.NotNull` check, and `SkipRetry` returns `false` for the null case. This is *intentional* (the XML doc explicitly says "may be null, in which case the command behaves identically to its base class") but it means the derived class is functionally equivalent to the base when callers pass null, which is a minor design smell — the null path doubles as a defensive fallback rather than being eliminated at construction time. Remediation (optional): if Wave 3 / Phase 3 producers will never construct this class with a null transaction (likely the case — they only construct it on the tx-aware send path), add `Guard.NotNull(() => externalTransaction, externalTransaction);` and update the XML doc accordingly. If null is genuinely permitted, no change. Defer this decision to the Wave 3 builder.
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs:68-89` — None of the 6 public Send overloads validate `transaction != null` before passing it down to the virtual hooks. Per CONTEXT-2 Decision 4 the validation contract lives in `ExternalTransactionValidator` (a separate type, Phase 3/4 wires it into the per-transport override). So the null-transaction check is deliberately deferred to the override, not the base. Acceptable design — but worth a comment on the class summary documenting the contract, so a future maintainer doesn't add a redundant `Guard.NotNull` here. Remediation (optional): add a one-line `<remarks>` on the class summary stating "Argument validation (transaction non-null, connection open, DB match) is enforced by transport-specific overrides via `ExternalTransactionValidator`; the base intentionally does not validate".
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs:99-100, 115-116` — The protected virtual `SendWithExternalTransaction` and `SendWithExternalTransactionAsync` `<param name="data">` doc says "`null` when invoked from the no-data overload". A future override implementer might miss that the base routes `null` here and substitute `new AdditionalMessageData()` only if they read the docstring carefully. Remediation (optional): mention `IAdditionalMessageData` null-substitution responsibility once on the class-level summary, not just on the param docs. Not blocking — the plan §Task 3 already documents this.
- Builder summary's "Decisions Made" notes shortening the comment text from `// --- 4 protected virtual hooks ...` to `// --- 4 hooks ...` solely to make `grep -c "protected virtual"` return exactly 4. This is a smell: the acceptance criterion shaped the code rather than the code shaping the criterion. The expected count of 4 is correct, but a more robust gate would be `grep -cE "^\s+protected virtual"` (matches only declaration lines, not comments). Remediation (optional): tighten the verification command in the next reviewer-author cycle. No code change needed.

### Positive
- Three atomic commits, one per task, with the `shipyard(phase-2):` prefix — clean bisect surface if a regression appears later. Matches Plan 1.1's commit cadence exactly.
- Plan's "exactly 6 overloads / exactly 4 virtuals / exactly 1 SkipRetry expression-body" contracts are all hit on the nose — no over- or under-builds.
- The batch-type deviation (`List` vs `IEnumerable` per PROJECT.md) is flagged in three places: the plan's architect note, the SUMMARY's "Decisions Made", and an inline `<remarks>` block on the interface (lines 86-90). That's the right place for a deviation to live — a future reader of the public API surface discovers it without having to dig into shipyard plans. **This is a verifier flag, not a critical issue:** the deviation is consistent with the existing `IProducerQueue<T>` (line 52 of `Source/DotNetWorkQueue/IProducerQueue.cs` declares the same batch overload using `List<QueueMessage<TMessage, IAdditionalMessageData>>`), so the interface inheritance chain is type-coherent. Forcing `IEnumerable` here would either require an `IEnumerable`-only batch overload (asymmetric with the base) or an `IEnumerable`→`List` conversion at the entry point (wasted allocation). The `List` choice is the strictly better design. Recommend PROJECT.md be amended in Wave 3 or Phase 3 docs work to reflect the actual signature.
- The four `protected virtual` hooks are correctly named with the `*WithExternalTransaction` suffix per CONTEXT-2 Decision 3, and the `Batch` / `Async` / `BatchAsync` suffix matrix is fully spelled out (no overload-by-arity guessing). Phase 3 SqlServer override implementers will get IntelliSense for all four hook names without ambiguity.
- The base `ProducerQueue<T>`'s existing non-tx `Send`/`SendAsync` overloads are not overridden — the existing self-managed-tx path is binary-compatible. Combined with the additive `ExternalTransaction` `init` property on `SendMessageCommand` (Wave 1), Phase 2's "no regressions on Memory/Redis/LiteDb/SQLite" exit criterion is structurally guaranteed for the producer surface, not just empirically observed.
- `Transport.RelationalDatabase.Tests` post-build run was 221/0/0 — same baseline as Wave 1 finished with. Wave 2's PLAN-2.2 additions did not perturb any existing test.
- Layering invariant `grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql"` independently re-verified — clean.
- The `NotConfiguredMessage()` is `private static` per SUMMARY decision — no instance state needed, and the format string is interned. Minor allocation/perf win over a `private` instance method.

## Summary
Verdict: APPROVE. PLAN-2.2's three atomic commits cleanly land the producer surface for the relational-outbox feature: the derived `RelationalSendMessageCommand` correctly implements `IRetrySkippable` for the Wave 3 retry-decorator branch, the `IRelationalProducerQueue<TMessage>` interface exposes the documented 6 tx-aware overloads, and `RelationalProducerQueue<T>` provides the 4 `protected virtual` hooks Phase 3/4 will override. The `List`-vs-`IEnumerable` batch type deviation is intentional, well-documented, structurally justified by `IProducerQueue<T>` symmetry, and is a verifier flag (recommend amending PROJECT.md spec wording) rather than a code defect. The cross-plan interleave with PLAN-2.1 produced no stray artifacts. Layering invariant holds.
Critical: 0 | Important: 0 | Suggestions: 4
