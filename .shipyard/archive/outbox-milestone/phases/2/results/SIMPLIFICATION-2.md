# Simplification Report
**Phase:** 2 (Foundation + Marker + Decorator Branches)
**Date:** 2026-05-14
**Files analyzed:** 9 new, 3 modified C# files across 4 projects
**Findings:** 0 High / 0 Medium / 3 Low
**Verdict:** LOW_FINDINGS — ship as-is.

## High Priority
None.

## Medium Priority
None.

## Low Priority

- **Decorator bypass branch repeated 4x (SqlServer sync/async + PostgreSQL sync/async).** Files: `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs:54-55` + `RetryCommandHandlerOutputDecoratorAsync.cs:54-55`, `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs:54-55` + `RetryCommandHandlerOutputDecoratorAsync.cs:54-55`. Each is a 2-line `if (command is IRetrySkippable skippable && skippable.SkipRetry) return _decorated.Handle(command);`. Rule-of-Three triggers, but justified: decorators are per-transport by DNQ architecture, each lives in a different sealed class with its own generic constraints, and abstracting would force a shared base class in `Transport.RelationalDatabase` that complicates Polly imports. **Confirm — leave as-is.** Effort to extract: Significant. Value: minimal.

- **Bypass-tests near-duplication across SqlServer.Tests + PostgreSQL.Tests.** Files: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` (86 lines) and `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` (80 lines) are 95% identical (only the `Decorator` namespace import changes). Extraction would require either (a) a new shared `Transport.RelationalDatabase.TestHelpers` assembly or (b) generic test class using `<TDecorator>`. Cost: new project + InternalsVisibleTo + Jenkins stage update. Benefit: ~70 lines saved across 2 files. **Defer.** Adding Phase 4 LiteDb/SQLite negative-path tests in Phase 5 would push duplication to 3+, which would justify the helper assembly — revisit then.

- **`NotConfiguredMessage()` private static helper in `RelationalProducerQueue<T>`.** File: `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs:155-159`. Used 4x at lines 108, 124, 138, 152 — Rule-of-Three satisfied. Scope is correct (private static, no state, single class). **Keep as-is.** No action needed.

## Positives

- Zero AI-bloat XML doc remarks. All `<summary>` and `<remarks>` blocks describe non-obvious phase-handoff intent (Phase 3/4 override expectations, layering invariants), not self-evident restatements.
- `RelationalSendMessageCommand.SkipRetry => ExternalTransaction != null` is a clean one-liner — no over-defensive null checks.
- Layering invariant holds: zero `Microsoft.Data.SqlClient` / `Npgsql` refs in `Transport.RelationalDatabase`.
- Throwaway Phase 1 PoC (`_SpikePollyBypassPoC.cs`, 191 lines) deleted as the very first task — clean entry.
- 5 validator unit tests are crisp and each tests exactly one negative path + the happy path; no duplicate setup helpers.

## Summary
- **Duplication found:** 1 intra-project pattern (4-way decorator branch) + 1 cross-project pattern (2-way test class). Both justified or pre-Rule-of-Three.
- **Dead code:** none.
- **Complexity hotspots:** none (largest new method is `RelationalProducerQueue.Send` overloads at 2 lines each).
- **AI bloat patterns:** none observed.
- **Estimated cleanup impact:** 0 lines worth removing in Phase 2 scope.

## Recommendation
Phase 2 is a clean additive foundation. No simplification is recommended before shipping. Revisit the test-duplication finding in Phase 5 when negative-path tests for the other transports may push the helper-assembly tradeoff favorable.
