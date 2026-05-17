# Phase 2 Plan Verification

**Phase:** Foundation Layer (RelationalDatabase + Marker + Decorator Branches)
**Date:** 2026-05-13
**Type:** plan-review (pre-execution coverage & structure)
**Plans reviewed:** PLAN-1.1, PLAN-2.1, PLAN-2.2, PLAN-3.1, PLAN-3.2

## Verdict: PASS

## Requirements Coverage Matrix

| # | Requirement | Covered By | Status |
|---|-------------|------------|--------|
| 1 | `IRelationalProducerQueue<TMessage> : IProducerQueue<TMessage>` interface with six overloads | PLAN-2.2 Task 2 | ✓ |
| 2 | `RelationalProducerQueue<TMessage>` concrete with 4 virtual hooks | PLAN-2.2 Task 3 | ✓ |
| 3 | `SendMessageCommand.ExternalTransaction { get; }` property | PLAN-1.1 Task 2 | ✓ |
| 4 | `IExternalDbNameExtractor` interface | PLAN-2.1 Task 1 | ✓ |
| 5 | `ExternalTransactionValidator` standalone class | PLAN-2.1 Task 2 | ✓ |
| 6 | `IRetrySkippable` marker interface in `Transport.RelationalDatabase` | PLAN-1.1 Task 3 | ✓ |
| 7 | `RelationalSendMessageCommand : SendMessageCommand, IRetrySkippable` derived class | PLAN-2.2 Task 1 | ✓ |
| 8 | SqlServer retry-decorator sync branch | PLAN-3.1 Task 1 | ✓ |
| 9 | SqlServer retry-decorator async branch | PLAN-3.1 Task 2 | ✓ |
| 10 | PostgreSQL retry-decorator sync branch | PLAN-3.2 Task 1 | ✓ |
| 11 | PostgreSQL retry-decorator async branch | PLAN-3.2 Task 2 | ✓ |
| 12 | XML doc comments on every new public type | PLAN-1.1 (T2, T3), PLAN-2.1 (T1, T2), PLAN-2.2 (T1, T2, T3) — Release-config TreatWarningsAsErrors+XML gen acceptance criteria enforce it on each new type | ✓ |
| 13 | PoC deletion (`_SpikePollyBypassPoC.cs`) | PLAN-1.1 Task 1 | ✓ |
| 14 | Unit tests for the validator (5 cases) | PLAN-2.1 Task 3 (5 `[TestMethod]` names enumerated, exit-criterion match) | ✓ |
| 15 | Unit tests for the 4 retry-decorator branches (sync+async × SqlServer+PostgreSQL) | PLAN-3.1 Task 3 (2 tests, SqlServer sync+async) + PLAN-3.2 Task 3 (2 tests, PostgreSQL sync+async) = 4 total | ✓ |

All 15 requirements mapped to exactly one plan/task. No double-coverage, no gaps.

## Plan Structure Checks

- **Plan size invariant (≤3 tasks each):** PASS.
  - PLAN-1.1: 3 tasks (Delete PoC / Add property / Create marker).
  - PLAN-2.1: 3 tasks (Create extractor IF / Create validator / Add 5 tests).
  - PLAN-2.2: 3 tasks (Create RelationalSendMessageCommand / Create IRelationalProducerQueue / Create RelationalProducerQueue).
  - PLAN-3.1: 3 tasks (Sync branch / Async branch / 2 bypass tests).
  - PLAN-3.2: 3 tasks (Sync branch / Async branch / 2 bypass tests).

- **Wave 2 plans depend only on Wave 1:** PASS.
  - PLAN-2.1 frontmatter `dependencies: [1.1]`. Context body confirms only Wave 1 outputs needed (project must build; no Wave 2 type import).
  - PLAN-2.2 frontmatter `dependencies: [1.1]`. Context body confirms it consumes `IRetrySkippable` (PLAN-1.1 Task 3) and `SendMessageCommand.ExternalTransaction` (PLAN-1.1 Task 2) — both Wave 1.
  - Neither Wave 2 plan depends on the other.

- **Wave 3 plans depend only on Waves 1+2:** PASS.
  - PLAN-3.1 frontmatter `dependencies: [1.1, 2.2]`. Uses `IRetrySkippable` (1.1) and `RelationalSendMessageCommand` (2.2) as test input.
  - PLAN-3.2 frontmatter `dependencies: [1.1, 2.2]`. Mirror.
  - Neither Wave 3 plan depends on PLAN-2.1 (validator is unused by retry decorators — correct per CONTEXT-2 §Wave 2/3 separation).
  - Neither Wave 3 plan depends on the other.

- **No intra-wave file conflicts (Wave 2):** PASS.
  - PLAN-2.1 files: `IExternalDbNameExtractor.cs`, `Basic/ExternalTransactionValidator.cs`, `Tests/Basic/ExternalTransactionValidatorTests.cs`.
  - PLAN-2.2 files: `Basic/Command/RelationalSendMessageCommand.cs`, `IRelationalProducerQueue.cs`, `Basic/RelationalProducerQueue.cs`.
  - Zero path overlap. Both plans add NEW files only; neither modifies a shared file.

- **No intra-wave file conflicts (Wave 3):** PASS.
  - PLAN-3.1 files: all under `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/` + `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/`.
  - PLAN-3.2 files: all under `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/` + `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/`.
  - Zero path overlap (different transport projects). The shared "outer rim" file `Transport.RelationalDatabase/IRetrySkippable.cs` is read-only consumption only.

- **Acceptance criteria are testable:** PASS. Spot-checks:
  - PLAN-1.1 T1: `test ! -f <path>` + `dotnet test --filter` (concrete).
  - PLAN-1.1 T2: `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug` + filtered `dotnet test` + Release-config build (concrete).
  - PLAN-2.1 T3: `dotnet test ... --filter "FullyQualifiedName~ExternalTransactionValidatorTests"` expected 5 passed (concrete).
  - PLAN-2.2 T2: `grep -c "DbTransaction transaction" <file>` expected 6 (concrete count).
  - PLAN-2.2 T3: `grep -c "protected virtual" <file>` expected 4 (concrete count).
  - PLAN-3.1 / 3.2 T1+T2: `grep -n "IRetrySkippable skippable" <file>` expected 1 match (concrete).
  - PLAN-3.1 / 3.2 T3: `dotnet test ... --filter "...BypassTests"` expected 2 passed (concrete).
  - No vague "code looks correct" / "tests pass" prose detected.

- **Verification sections have runnable commands:** PASS. All 5 plans carry a `## Verification` fenced bash block with explicit command + `# expected: …` comment. Each section additionally includes the layering invariant grep from CONTEXT-2 Hard Rules where applicable. PLAN-3.2's verification block is also the de-facto phase-wide check: it ends with `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true` (the strictest Release/CI cross-project build) — this is the right place for it (last plan landing in dep order).

## Coverage Notes (informational, no action needed)

- **CONTEXT-2 Exit Criterion 1 (10-item public surface):** all 10 items mapped to one of the 5 plans (items 1, 2, 4, 5, 6, 7 ↔ requirements 1, 2, 4, 5, 6, 7 above; item 3 ↔ requirement 3; items 7+8 ↔ requirements 8–11; item 9 = XML docs cross-cutting ↔ requirement 12; item 10 = test coverage ↔ requirements 14–15).
- **CONTEXT-2 Exit Criterion 2 (layering grep):** appears in 4 of 5 plans' Verification sections (PLAN-1.1, PLAN-2.1, PLAN-2.2, PLAN-3.1, PLAN-3.2). Strong redundancy is correct: every wave can independently regress purity.
- **CONTEXT-2 Exit Criterion 7 (capability-cast SimpleInjector smoke test):** explicitly deferred to Phase 3 per CONTEXT-2 §Exit Criteria #7 paren ("Phase 2 likely cannot wire DI without the transport-specific init classes"). No plan attempts it. Correct deferral, not a gap.
- **CONTEXT-2 Decision 2 layering resolution:** Architect chose Option B (derived class) — `RelationalSendMessageCommand : SendMessageCommand, IRetrySkippable`. PLAN-1.1 Task 2 docstring explicitly notes the base class does NOT implement `IRetrySkippable` to keep `Transport.Shared` independent — this matches the layering invariant. Approved.
- **PROJECT.md spec deviation (List vs IEnumerable):** PLAN-2.2 Task 2 architect note flags PROJECT.md says `IEnumerable<QueueMessage<...>>` for batch overloads but plan uses `List<...>` to match existing `IProducerQueue<T>` shape. Plan documents the deviation explicitly and routes the decision back to verifier. This is a conscious, defensible spec deviation — accepting it (matches existing `IProducerQueue<T>` shape, avoids `IEnumerable→List` boundary inside the producer); the deviation is non-breaking and PROJECT.md spec is the looser type, so callers passing `List<>` still work. No revision needed.
- **PROJECT.md Validator extractor `StringComparer`:** PLAN-2.1 Task 2 architect note flags that the validator hard-codes `StringComparison.Ordinal` and pushes per-provider case semantics into each extractor's normalization. This is a defensible decomposition. The note also reminds the verifier to ensure Phase 3/4 plans encode the normalization convention symmetrically — that's a forward note, not a Phase 2 plan gap.

## Gaps

None blocking. All 15 phase requirements covered.

## Recommendations

No changes needed. The plan set is well-scoped, dependency-ordered, file-conflict-free, and every acceptance criterion is testable with a concrete command. The two architect-flagged deviations (List vs IEnumerable on the producer interface; `StringComparison.Ordinal` in the validator with provider-side normalization) are conscious decompositions documented inline — both are acceptable as-is and do not require revision.

Phase 2 plans are ready for build execution.
