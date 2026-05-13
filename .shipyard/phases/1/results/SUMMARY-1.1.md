# Build Summary: Plan 1.1

## Status: complete

## Tasks Completed
- Task 1: spike memo — complete — files: `.shipyard/notes/phase-1-polly-bypass-spike.md` (119 lines, 9 `##` sections)
- Task 2: throwaway PoC — complete — files: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` (191 lines, 2 passing tests)
- Task 3: PROJECT.md Risk #1 downgrade — complete — files: `.shipyard/PROJECT.md` (single-line edit)

## Commits
- `7e3e4779` — shipyard(phase-1): add Polly bypass spike memo
- `f4049fb8` — shipyard(phase-1): add throwaway PoC for IRetrySkippable bypass
- `3357a2b0` — shipyard(phase-1): downgrade Risk #1 in PROJECT.md

## Files Modified
- `.shipyard/notes/phase-1-polly-bypass-spike.md` (new) — durable spike memo. Per-transport decorator inventory (SqlServer + PostgreSQL, sync + async) with init-class line numbers; explicit "no divergence" finding; `IRetrySkippable` marker design with proposed C# snippets for sync and async branches; six-file Phase 2 files-to-touch list; Risk #1 downgrade justification; PoC pointer.
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` (new) — throwaway PoC. Local `_SpikeIRetrySkippable` marker; `_SpikeSendCommand : SendMessageCommand` implementing it; `_SpikeRecordingHandler` recording call counts; `_SpikePatchedRetryDecorator<TCommand, TOutput>` mirroring production decorator with the proposed bypass branch added. Two tests: positive (`SkipRetry_When_CommandImplementsMarker_With_SkipRetryTrue`) and negative (`RetryPath_Still_Used_When_SkipRetryFalse`).
- `.shipyard/PROJECT.md` (modify) — Risk #1 `(mid)` → `(low — closed by Phase 1 spike)` with pointer to memo. No other content modified.

## Decisions Made
- Memo committed via `git add -f` because `.shipyard/.gitignore` is `*` (matches the existing PROJECT.md / ROADMAP.md / plans pattern). Captured as a lesson candidate.
- Plan suggested `_policies.Registry.DidNotReceive().TryGetPipeline(...)` — not viable because `IPolicies.Registry` returns the sealed concrete `ResiliencePipelineRegistry<string>` (NSubstitute cannot proxy it). Pivoted to property-getter call assertions on the `IPolicies` substitute itself (`policies.DidNotReceiveWithAnyArgs().Registry` / `policies.Received().Registry`). Equivalent observability — if the bypass branch fires first, the `Registry` getter is never accessed.
- LGPL-2.1 header copied from sibling `RetryCommandHandlerOutputDecoratorTests.cs`. Added an explicit "THROWAWAY SPIKE FILE — Phase 2 Task 1 deletes this file" block-comment header below the LGPL to make the file's lifecycle unambiguous to reviewers.
- `_SpikeSendCommand` constructor forwards NSubstitute mocks of `IMessage` and `IAdditionalMessageData` (built in a `BuildCommand(bool skipRetry)` factory). The recording inner handler never inspects them.

## Issues Encountered
- `IPolicies.Registry` is a sealed concrete type — NSubstitute can't intercept its methods. Lesson candidate.
- `.shipyard/.gitignore` is `*`; required `git add -f` for every shipyard artifact. Lesson candidate.
- No other surprises. The production `RetryCommandHandlerOutputDecorator.cs` matched RESEARCH.md's description bit-for-bit.

## Verification Results
- Memo section count: 9 ≥ 6 — PASS
- PoC test results: 2 passed, 0 failed (~142 ms) — PASS
- No-production-change check vs `shipyard/pre-build-phase-1` (excluding `.shipyard/` and `_SpikePollyBypassPoC.cs`): empty diff — PASS
- Risk #1 downgrade: second line contains `(low — closed by Phase 1 spike)` — PASS
- Baseline `RetryCommandHandlerOutputDecoratorTests` (3 tests) confirmed passing — no regressions.

## Builder Context
- turns=12, compressed=no, task_complete=yes
- Agent id: a577c01fd0be1584d (still available via SendMessage if review feedback requires fixes)
