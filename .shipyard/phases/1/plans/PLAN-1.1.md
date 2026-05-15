# Plan 1.1: Polly Decorator Bypass Spike — Memo + Throwaway PoC

## Context

Phase 1 is a discovery spike that resolves Risk #1 from PROJECT.md: confirming a clean mechanism for the new caller-supplied-transaction code path to bypass the Polly retry decorator chain without losing trace observability or requiring DI surgery. Research (`.shipyard/phases/1/RESEARCH.md`) concluded that:

1. The decorator chain on both SqlServer and PostgreSQL is identical: `Trace(Retry(Handler))` for sync and async.
2. The existing `RetryCommandHandlerOutputDecorator` already has two fallthrough branches (no-pipeline + shutdown-race-disposed). Adding a third (caller-tx bypass via a marker interface) is consistent with its design.
3. The recommended mechanism is an `IRetrySkippable` marker interface in `Transport.Shared` that `SendMessageCommand` implements, with `SkipRetry => ExternalTransaction != null`.

This plan produces two artifacts:

- **Durable:** `.shipyard/notes/phase-1-polly-bypass-spike.md` — the memo documenting the chosen mechanism, decorator inventory, and per-transport mirror confirmation. Persists past Phase 2.
- **Throwaway:** A unit test in `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` that constructs a `RetryCommandHandlerOutputDecorator` against a mocked inner handler implementing the proposed marker check, hands it a `SendMessageCommand` simulating `ExternalTransaction != null`, and verifies the bare handler is called exactly once with no Polly retries. The PoC is named with a leading underscore + `_Spike` prefix so it is unambiguously throwaway. Phase 2's first task deletes it.

The PoC test does **not** modify any production code. It demonstrates the marker mechanism would work, without committing the production change. Phase 2 commits the production marker interface + decorator branch for real.

## Dependencies

None. Phase 1 is the first phase.

## Tasks

### Task 1: Write the spike memo

**Files:** `.shipyard/notes/phase-1-polly-bypass-spike.md` (create)
**Action:** create
**Description:** Author the durable spike memo. Content must include:

1. **Decorator inventory section** — bit-for-bit accurate per-transport listing from RESEARCH.md Section 1: the four handler classes (SqlServer sync, SqlServer async, PostgreSQL sync, PostgreSQL async), the two decorators wrapping each (`Retry*` + `*Trace*`), and registration line numbers in `SQLServerMessageQueueInit.cs` (lines 154, 160, 182, 186) and `PostgreSQLMessageQueueInit.cs` (lines 179, 185, 208, 212).
2. **Per-transport divergence section** — explicitly state "none found"; both transports mirror the same registration pattern. This closes the open question from CONTEXT-1 Decision 1.
3. **Chosen mechanism section** — `IRetrySkippable` marker interface in `Transport.Shared`. Show the proposed interface and the proposed `SendMessageCommand.SkipRetry` property override. Show the proposed early-return branch inside `RetryCommandHandlerOutputDecorator.Handle()` (sync) and the async equivalent. Reference the two existing fallthrough precedents (no-pipeline branch + PR #121 `ObjectDisposedException` branch) as the design justification.
4. **Files-to-touch list for Phase 2+** — enumerate the 6 files Phase 2 onward will modify to commit the production change: `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs`, a new `Source/DotNetWorkQueue.Transport.Shared/IRetrySkippable.cs` interface file, and four retry decorator files (`Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs`, `RetryCommandHandlerOutputDecoratorAsync.cs`, and the PostgreSQL equivalents).
5. **Risk-#1 disposition** — explicit statement that Risk #1 from PROJECT.md is downgraded from MID to LOW: the mechanism is proven feasible with a single ~3-line decorator branch reusing an existing pattern.
6. **PoC reference** — a one-line pointer to the throwaway test file with a note that Phase 2's first task removes it.

Memo length target: under 200 lines. Tone matches existing repo `docs/jenkins-setup.md` style — direct, file-path-and-line-number specific, no fluff.

**Acceptance Criteria:**
- File exists at `.shipyard/notes/phase-1-polly-bypass-spike.md` with all six required sections.
- Decorator-inventory file paths and line numbers match RESEARCH.md Section 1 exactly.
- Memo includes the proposed C# snippet for the decorator early-return branch.
- Memo includes the list of files Phase 2 will modify (the 6 files enumerated above).
- Memo explicitly downgrades Risk #1 with one-sentence justification.

---

### Task 2: Add the throwaway proof-of-concept test

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` (create)
**Action:** create
**Description:** Create a single MSTest test class that demonstrates the `IRetrySkippable` mechanism works without modifying production code. The PoC defines its **own** marker interface (named `_SpikeIRetrySkippable` to avoid collision with the production interface Phase 2 will add) and a **local subclass** of `SendMessageCommand` that implements the marker — proving the design before Phase 2 commits the production version.

The PoC test class:

1. Defines `internal interface _SpikeIRetrySkippable { bool SkipRetry { get; } }` in the same file.
2. Defines `private sealed class _SpikeSendCommand : SendMessageCommand, _SpikeIRetrySkippable { public bool SkipRetry { get; init; } public _SpikeSendCommand(...) : base(...) { } }` — constructor forwards to `SendMessageCommand`'s base constructor with placeholder args (or test-supplied args).
3. Defines `private sealed class _SpikeRecordingHandler : ICommandHandlerWithOutput<SendMessageCommand, long> { public int CallCount; public long Handle(SendMessageCommand c) { CallCount++; return 42L; } }`.
4. Defines `private sealed class _SpikePatchedRetryDecorator<TCommand, TOutput> : ICommandHandlerWithOutput<TCommand, TOutput>` — a near-copy of the production retry decorator with the **proposed** `IRetrySkippable` early-return branch added. This subclass exists ONLY in this PoC file. The production `RetryCommandHandlerOutputDecorator` is **not** modified.
5. One test method `SkipRetry_When_CommandImplementsMarker_With_SkipRetryTrue`:
   - Arrange: `var inner = new _SpikeRecordingHandler();` + `var decorator = new _SpikePatchedRetryDecorator<SendMessageCommand, long>(inner, NSubstitute.Substitute.For<IPolicies>());`
   - Act: `decorator.Handle(new _SpikeSendCommand(...) { SkipRetry = true });`
   - Assert: `inner.CallCount` equals 1 (single invocation, no retry attempted). `_policies.Registry.TryGetPipeline` is **not** invoked (verified via NSubstitute call check). MSTest 4.x style: `Assert.AreEqual(1, inner.CallCount);` and NSubstitute `policies.Registry.DidNotReceive().TryGetPipeline(...)`.
6. A companion test method `RetryPath_Still_Used_When_SkipRetryFalse` confirming the negative case: `SkipRetry = false` triggers the pipeline lookup. This validates the early-return is properly conditional.

License header (LGPL-2.1) included per repo convention.

The file is named with a **leading underscore** to make its "throwaway" status visible in directory listings and to keep MSTest's `[TestClass]` discovery functional (underscore prefix doesn't affect discovery — confirmed by repo convention).

**Acceptance Criteria:**
- File `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` exists and compiles clean against net10.0 + net8.0.
- File contains the LGPL-2.1 header.
- Both test methods (`SkipRetry_When_CommandImplementsMarker_With_SkipRetryTrue` and `RetryPath_Still_Used_When_SkipRetryFalse`) pass when `dotnet test` is run against `Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj --filter "FullyQualifiedName~_SpikePollyBypassPoC"`.
- The production `RetryCommandHandlerOutputDecorator.cs` files in SqlServer and PostgreSQL are **not modified** by this plan (verified via `git diff --name-only` showing no production-code changes).
- The PoC does not reference any new public surface that doesn't exist yet (no references to `IRelationalProducerQueue<T>`, no references to `SendMessageCommand.ExternalTransaction` — those are Phase 2's responsibility).

---

### Task 3: Update PROJECT.md Risk Inventory to reflect Phase 1 outcome

**Files:** `.shipyard/PROJECT.md` (modify)
**Action:** modify
**Description:** Edit the Risk Inventory section of PROJECT.md to downgrade Risk #1 from "mid" to "low" and append a one-line note pointing to the spike memo.

Original:
```
1. **Polly decorator bypass cleanness** (mid) — verify the bare handler is reachable without retry wrapping; investigate current decorator chain before committing to full plan.
```

New:
```
1. **Polly decorator bypass cleanness** (low — closed by Phase 1 spike) — Mechanism confirmed: `IRetrySkippable` marker interface evaluated at the top of `RetryCommandHandlerOutputDecorator.Handle()`. Reuses the same fallthrough pattern as the existing no-pipeline + shutdown-race branches. See `.shipyard/notes/phase-1-polly-bypass-spike.md`.
```

**Acceptance Criteria:**
- The Risk Inventory entry #1 in `.shipyard/PROJECT.md` is updated to the new text exactly.
- No other PROJECT.md content is modified.
- `grep -c "Polly decorator bypass" .shipyard/PROJECT.md` returns exactly 1.

---

## Verification

After all three tasks complete, run from the repo root:

```bash
# 1. Memo exists and has all six required sections
test -f .shipyard/notes/phase-1-polly-bypass-spike.md && \
  grep -c "^##" .shipyard/notes/phase-1-polly-bypass-spike.md
# Expected: file exists and section count >= 6

# 2. PoC test compiles and passes
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" \
  --filter "FullyQualifiedName~_SpikePollyBypassPoC" \
  -c Debug --nologo
# Expected: 2 tests passed, 0 failed

# 3. No production code was modified
git diff --name-only master \
  | grep -vE "^(\.shipyard/|Source/DotNetWorkQueue\.Transport\.SqlServer\.Tests/Decorator/_SpikePollyBypassPoC\.cs)" \
  | tee /tmp/phase1-prod-changes.txt
# Expected: empty output (no production file touched)

# 4. PROJECT.md risk #1 downgrade
grep -A1 "Polly decorator bypass cleanness" .shipyard/PROJECT.md | head -2
# Expected: line contains "(low — closed by Phase 1 spike)"
```

All four verifications must pass before Phase 1 is considered complete. The PoC test pass is the key proof — it shows the proposed mechanism actually works in compiled code, not just on paper.

When Phase 2 starts, its first task removes `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs`. The memo and Risk Inventory update persist.
