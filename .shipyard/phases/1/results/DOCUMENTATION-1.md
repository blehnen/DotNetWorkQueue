# Phase 1 Documentation Review

## Overall Status: SUFFICIENT

## Phase 1 Documentation Deliverable
- The phase's primary deliverable IS documentation: `.shipyard/notes/phase-1-polly-bypass-spike.md`.
- Production code changes in this phase: zero (per `git diff --stat shipyard/pre-build-phase-1..HEAD` — only the memo, the PoC test, and a one-line PROJECT.md touch).
- `docs/outbox-pattern.md` is explicitly Phase 7's deliverable per ROADMAP.md line 172; this review does NOT generate it.

## Memo Quality
- Decorator inventory section: complete + file paths cited. Both SqlServer (`SQLServerMessageQueueInit.cs` init lines 154/182/186) and PostgreSQL (`PostgreSQLMessageQueueInit.cs` init lines 179/185/208/212) chains tabulated with type names + file paths + init-class line numbers. Inner-handler concrete locations cited (`SendMessageCommandHandler.cs:39`, `SendMessageCommandHandlerAsync.cs:39`).
- Per-transport divergence section: explicit. "**None found.**" with justification — both transports use identical open-generic-retry-then-closed-trace order, no per-transport conditional logic. Explicitly closes the CONTEXT-1 Decision 1 open question.
- Chosen mechanism section: complete with full C# snippet for the interface itself, the `SendMessageCommand` implementation hook (`SkipRetry => ExternalTransaction != null`), and the full proposed `Handle()` method body including the early-return branch placement relative to the `try`/`ObjectDisposedException` catch. Async variant explicitly called out as the same shape with `await … .ConfigureAwait(false)`.
- Files-to-touch list for Phase 2+: present + matches ROADMAP Phase 2 description. Six files enumerated: the new `IRetrySkippable.cs` in `Transport.Shared`, the `SendMessageCommand.cs` update, and the four decorator files (sync + async × SqlServer + PostgreSQL). Namespace-placement justification (Shared vs. RelationalDatabase) provided with the reference-graph reason.
- Risk #1 disposition: present + crisp. Explicitly downgraded to LOW with the "~3-line decorator branch reusing existing fallthrough pattern, no DI registration changes, no per-transport divergence" justification. Tied back to `.shipyard/PROJECT.md` Risk Inventory entry by name.
- PoC reference: present. Final section links to `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` and explicitly flags Phase 2's first task as deleting it.

## PoC Inline Documentation
- Purpose comment on `_SpikePatchedRetryDecorator`: clear. The XML doc on the class says "Near-copy of `RetryCommandHandlerOutputDecorator<TCommand, TOutput>` with the proposed marker-bypass branch added at the top of `Handle`. The production decorator is NOT modified by this spike — that change lands in Phase 2."
- Throwaway lifecycle clearly signaled: yes. File header comment lines 20–26 declare "THROWAWAY SPIKE FILE — Phase 1 Polly decorator bypass PoC" and "Phase 2's first task DELETES this file". The `_Spike` prefix on every type underscores throwaway status.
- Test method names self-documenting: yes. `SkipRetry_When_CommandImplementsMarker_With_SkipRetryTrue` (positive case) and `RetryPath_Still_Used_When_SkipRetryFalse` (negative case) describe their assertions in the name. Inline comments on lines 161–164 and 183–188 explain WHY the assertions take the shape they do (NSubstitute cannot intercept `TryGetPipeline` on the concrete registry, so the `Registry` getter call serves as the proxy assertion).
- The proposed-Phase-2-branch marker (`// ----- proposed Phase 2 branch -----` on lines 120–123) makes the diff-target unambiguous for a Phase 2 reader.

## Gaps for Future Phases
- Phase 2: Production `IRetrySkippable` interface and `SendMessageCommand.SkipRetry` / `SendMessageCommand.ExternalTransaction` property additions will need XML doc comments (the memo's C# snippet already shows the prose for `IRetrySkippable`'s `<summary>` — Phase 2 can lift it verbatim). Note for Phase 2 build agent — NOT blocking Phase 1.
- Phase 2: ROADMAP also requires XML doc comments on `IRelationalProducerQueue<TMessage>`, `RelationalProducerQueue<TMessage>`, `IExternalDbNameExtractor`, and `ExternalTransactionValidator` (Phase 2 §Description bullet 6). Independent of this spike's deliverables but worth flagging for trail continuity.
- Phase 7: User-facing `docs/outbox-pattern.md` page covering the caller-owned-transaction contract, retry-bypass semantics surfaced to callers, and the `NpgsqlBatch` fallback decision from Phase 4. Note for Phase 7 — NOT blocking Phase 1.

## New Documentation to Generate Now
- None. This phase IS the documentation phase for the spike. Phase 7 owns the user-facing docs; per ROADMAP Phase 7 is the locked location for `docs/outbox-pattern.md` and creating it now would parallel-document the as-yet-unbuilt API.

## Recommendation
- Memo is sufficient for Phase 1 — no further docs work this phase. The memo cleanly separates spike-decision documentation (its scope) from user-facing API documentation (Phase 7's scope). The PoC file's inline XML doc + "throwaway" header banner gives a Phase 2 reviewer everything needed to understand and then delete it. Proceed to phase completion.
