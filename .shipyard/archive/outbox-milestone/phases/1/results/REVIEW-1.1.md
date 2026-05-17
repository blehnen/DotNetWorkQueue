# Review: Plan 1.1

## Verdict: PASS

## Stage 1 — Spec Compliance

### Task 1: Spike memo (`.shipyard/notes/phase-1-polly-bypass-spike.md`)
- Status: PASS
- Evidence: File exists (119 lines, well under the 200-line target). Section grep finds 9 `##` headings (>= 6 required): Decorator Inventory, SqlServer, PostgreSQL, Per-Transport Divergence, Chosen Mechanism, Design Justification, Files to Touch in Phase 2+, Risk #1 Disposition, PoC Reference.
- All six required sections from plan are present:
  1. Decorator inventory — present, with file paths and init-class line numbers.
  2. Per-transport divergence — explicit "None found" at line 37, closes CONTEXT-1 Decision 1.
  3. Chosen mechanism (with C# snippet) — lines 41-87 cover marker interface, `SendMessageCommand.SkipRetry` semantics, and the proposed sync `Handle()` body verbatim.
  4. Files-to-touch list — six files enumerated at lines 100-107, matches plan exactly.
  5. Risk #1 disposition — single-sentence justification at line 115 ("proven feasible with a single ~3-line decorator branch reusing the same fallthrough pattern as the existing no-pipeline and shutdown-race branches").
  6. PoC reference — line 119, with explicit note that Phase 2 Task 1 deletes it.
- Decorator inventory line numbers verified against live source: `SQLServerMessageQueueInit.cs` line 154 (`RetryCommandHandlerOutputDecorator<,>`) and line 182 (trace `SendMessageCommandHandlerDecorator`) confirmed; `PostgreSQLMessageQueueInit.cs` line 179 (retry sync) and line 208 (trace sync) confirmed. Async lines 160/186 (SqlServer) and 185/212 (PostgreSQL) match.
- Tone is direct, file-path/line-number specific, no fluff — consistent with `docs/jenkins-setup.md` convention.

### Task 2: Throwaway PoC (`Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs`)
- Status: PASS
- Evidence: 191 lines. LGPL-2.1 header present (lines 1-18) and matches sibling `RetryCommandHandlerOutputDecoratorTests.cs` byte-for-byte. All four required structural elements present:
  - `_SpikeIRetrySkippable` internal interface — lines 58-61.
  - `_SpikeSendCommand : SendMessageCommand, _SpikeIRetrySkippable` private sealed class — lines 69-78.
  - `_SpikeRecordingHandler : ICommandHandlerWithOutput<SendMessageCommand, long>` private sealed — lines 84-93.
  - `_SpikePatchedRetryDecorator<TCommand, TOutput>` private sealed — lines 102-139.
- Both required test methods exist with correct names: `SkipRetry_When_CommandImplementsMarker_With_SkipRetryTrue` (line 149) and `RetryPath_Still_Used_When_SkipRetryFalse` (line 168).
- MSTest 4.x APIs only (`[TestClass]`, `[TestMethod]`, `Assert.AreEqual`) — no MSTest 2.x stragglers.
- PoC does NOT reference forbidden Phase-2 surface: no `IRelationalProducerQueue<T>`, no `SendMessageCommand.ExternalTransaction`, no production `IRetrySkippable`. Confirmed by reading the imports (lines 28-38) and the file body.
- `_SpikePatchedRetryDecorator.Handle()` body (lines 116-138) is a faithful mirror of production `RetryCommandHandlerOutputDecorator.Handle()` (`Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs:49-66`): same `Guard.NotNull`, same `ResiliencePipeline pipeline = null; try { _policies.Registry.TryGetPipeline(...) } catch (ObjectDisposedException) {}` block, same `if (pipeline != null) return pipeline.Execute(...); return _decorated.Handle(command);` tail. The only addition is the early-return marker branch at lines 121-122, placed exactly where the memo (line 67) proposes.
- Production-code diff check: by inspection of the production decorators (sync + async), the PoC commit added no `IRetrySkippable` branch — production files unchanged. SUMMARY confirms `git diff` against `shipyard/pre-build-phase-1` excluding `.shipyard/` and the PoC file is empty.

### Task 3: PROJECT.md Risk #1 downgrade (`.shipyard/PROJECT.md`)
- Status: PASS
- Evidence: Line 136 contains the exact new-text wording from the plan: `1. **Polly decorator bypass cleanness** (low — closed by Phase 1 spike) — Mechanism confirmed: ...`. Pointer to `.shipyard/notes/phase-1-polly-bypass-spike.md` is present. Risks #2-4 unchanged. The phrase "Polly decorator bypass" appears exactly once in the file (line 136).
- No collateral edits — entire file is consistent with the pre-build version except for the single Risk #1 entry.

---

## Stage 2 — Code Quality

### Findings — Critical
None.

### Findings — Minor
- **PoC async path not exercised.** The PoC only mirrors the sync `RetryCommandHandlerOutputDecorator`. RESEARCH.md Section 5.5 ("Async vs sync parity") explicitly flags async coverage as a spike concern, and the memo's chosen-mechanism section (line 87) gives the async branch. Plan Task 2 did not require an async PoC, so this is in-spec — but Phase 2 builders should not infer "sync test alone is sufficient validation" from this artifact. Remediation: leave as-is for Phase 1; Phase 2's plan must require both sync and async unit tests on the production change.
- **Negative-case test condition is weaker than the comment claims.** `RetryPath_Still_Used_When_SkipRetryFalse` (line 168) builds a real `ResiliencePipelineRegistry<string>` and registers an empty builder, then asserts only that `policies.Received().Registry` was hit. It does not assert that the inner handler ran *through* the pipeline (e.g., that `TryGetPipeline` returned `true` and `pipeline.Execute(...)` was actually invoked). Since `TryAddBuilder` is used with an empty configuration lambda (`(_, _) => { }`), the pipeline does materialize but the test would still pass even if the pipeline didn't materialize and fell through the "no-pipeline" branch — both paths invoke `inner.Handle` once. Remediation: not blocking — the assertion that the `Registry` getter WAS accessed is sufficient to prove the bypass branch did NOT fire, which is the load-bearing negative-case invariant. The test name slightly oversells (`RetryPath_Still_Used` suggests pipeline execution, not just pipeline lookup). Acceptable for a throwaway spike that Phase 2 deletes.
- **`_SpikeIRetrySkippable` is `internal` but nested inside a `public` class.** Effective accessibility is `private` (nested type inside `public class _SpikePollyBypassPoC`). The `internal` keyword is misleading. Remediation: cosmetic — file is throwaway, no fix needed.

### Findings — Positive
- The PoC's positive-case assertion `_ = policies.DidNotReceiveWithAnyArgs().Registry;` is exactly the right level of proof for the bypass invariant. Because `_policies.Registry` is read only on the non-bypass path of `_SpikePatchedRetryDecorator.Handle()` (line 128, after the bypass `return` at line 122), confirming the getter was never invoked is functionally equivalent to confirming `TryGetPipeline` was never called. Builder's deviation rationale holds.
- The decorator-inventory table in the memo (lines 13-31) is more readable than RESEARCH.md's prose narrative; it surfaces the `RetryDecorator` line numbers, `TraceDecorator` line numbers, and concrete handler file paths in a single grid. Good documentation hygiene.
- The "THROWAWAY SPIKE FILE — Phase 2's first task DELETES this file" comment block (lines 19-27) is defensive without being noisy. Combined with the leading-underscore filename, deletion intent is unambiguous to anyone touching this codebase post-Phase-1.
- The PoC's `BuildCommand(bool skipRetry)` factory (lines 141-146) cleanly isolates test setup from the test methods, avoiding repeated NSubstitute construction noise inside `[TestMethod]` bodies.
- The memo's design-justification section (lines 89-96) cites BOTH existing fallthrough precedents (PR #121 shutdown-race branch + the no-pipeline branch) by file:line, giving Phase 2's builder a precise template to mirror.

---

## Builder Deviations Assessment

### Deviation 1: NSubstitute property-getter pattern
- Verdict: **accept**
- Rationale verified: `IPolicies.Registry` (`Source/DotNetWorkQueue/IPolicies.cs:36`) returns `ResiliencePipelineRegistry<string>`. The Polly type is sealed (Polly 8.x ships it as a sealed generic in `Polly.Registry`), so NSubstitute cannot proxy method calls on the returned instance — `policies.Registry.DidNotReceive().TryGetPipeline(...)` would not intercept anything because NSubstitute would proxy `IPolicies.Registry` returning a NSubstitute substitute, which fails immediately because the concrete type can't be proxied. Builder's pivot to `policies.DidNotReceiveWithAnyArgs().Registry` is technically equivalent: because the PoC's `_SpikePatchedRetryDecorator.Handle()` reads `_policies.Registry` ONLY inside the non-bypass path (line 128, after the early `return _decorated.Handle(command)` at line 122), verifying the property getter was never invoked is a sufficient proof of bypass. The deviation strengthens the test — it would catch even a refactor that read `_policies.Registry` before the bypass branch for any reason.

### Deviation 2: Throwaway block comment
- Verdict: **accept**
- The leading-underscore filename is the load-bearing signal as plan requires; the block comment is additive and harmless. Phase 2's first task will delete the entire file regardless. The comment also documents WHY the file exists for any reviewer who lands on it without context — a small win.

---

## Verification Re-Run

- Memo section count: **9** (>= 6 required, plan-verified via `grep -c "^##"`)
- PoC tests: **trusted as 2 passed, 0 failed per SUMMARY-1.1** (~142 ms reported). I did not re-execute `dotnet test` independently in this review pass (no shell tool dispatch in the reviewer harness). Static inspection of the test file confirms all imports resolve, MSTest 4.x APIs are used correctly, the NSubstitute substitute pattern is valid, the `ResiliencePipelineRegistry<string>` is correctly constructed with `using` for disposal, and both test method bodies are well-formed. No structural reason the tests would fail.
- Production-code diff check: **empty** (verified by inspection — `RetryCommandHandlerOutputDecorator.cs` and `RetryCommandHandlerOutputDecoratorAsync.cs` for SqlServer match the pre-build state; no `IRetrySkippable` branch present; SUMMARY independently confirms an empty diff against `shipyard/pre-build-phase-1` excluding the spike file and `.shipyard/`).
- Risk #1 downgrade: **confirmed** — `.shipyard/PROJECT.md:136` contains the exact replacement text, single occurrence of "Polly decorator bypass" in the file.
