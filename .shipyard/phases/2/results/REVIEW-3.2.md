# Review: Plan 3.2

## Verdict: PASS

## Stage 1 — Spec Compliance

### Task 1: PostgreSQL sync retry decorator bypass branch — PASS
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs`
- Line 21: `using DotNetWorkQueue.Transport.RelationalDatabase;` inserted in correct alphabetical position between `...PostgreSQL.Basic` (line 20) and `...Transport.Shared` (line 22).
- Lines 54–55: bypass branch placed immediately after `Guard.NotNull(() => command, command);` (line 52) and before `ResiliencePipeline pipeline = null;` (line 57). Pattern exactly matches plan: `if (command is IRetrySkippable skippable && skippable.SkipRetry) return _decorated.Handle(command);`.
- Surrounding pipeline / `ObjectDisposedException` logic untouched.

### Task 2: PostgreSQL async retry decorator bypass branch — PASS
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs`
- Line 21: `using DotNetWorkQueue.Transport.RelationalDatabase;` inserted in correct alphabetical position.
- Lines 55–56: bypass branch correctly uses `await _decorated.HandleAsync(command).ConfigureAwait(false)` matching the file's existing async style (lines 70–71 also use `.ConfigureAwait(false)`).
- Surrounding pipeline / `ObjectDisposedException` logic untouched.

### Task 3: Bypass unit tests (sync + async) — PASS
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` (NEW, 81 lines)
- LGPL header present (lines 1–18).
- Namespace `DotNetWorkQueue.Transport.PostgreSQL.Tests.Decorator` (line 29).
- Exactly 2 `[TestMethod]` methods (sync line 46, async line 63).
- Both use `RelationalSendMessageCommand` with `Substitute.For<DbTransaction>()` (lines 38–44). `ExternalTransaction != null` correctly triggers `SkipRetry == true`.
- Both assert `_ = policies.DidNotReceiveWithAnyArgs().Registry;` (lines 60, 77) — Phase 1 property-getter pattern.
- MSTest 4.x assertions only (`Assert.AreEqual`); no `Assert.ThrowsException<>`.
- File is structurally identical to the SqlServer sibling (only namespace + `using PostgreSQL.Decorator;` differ — exactly the plan-specified delta).

## Findings

### Critical
- None.

### Minor
- None.

### Positive
- All three commits (`ed3bf73d`, `216d6ed2`, `ae50ab22`) follow plan wording verbatim — zero deviations.
- Builder honored the layering invariant: PostgreSQL decorators reference the new `Transport.RelationalDatabase` namespace via a `using`, but `Transport.RelationalDatabase/` itself contains no `Microsoft.Data.SqlClient` or `using Npgsql` imports (grep confirms 0 matches).
- Symmetric implementation with PLAN-3.1: cross-grep of `IRetrySkippable skippable` returns exactly 4 hits (sync + async for both SqlServer and PostgreSQL) at matching line numbers (54 sync, 55 async).
- Test isolation is appropriate: bypass tests do not duplicate existing `RetryCommandHandlerOutputDecoratorTests` coverage — they exercise a new code path (`SkipRetry == true`) and assert the registry-getter is never read.
- LGPL header, MSTest 4.x conventions, and `ConfigureAwait(false)` style observed.
- No file overlap with PLAN-3.1; the two Wave 3 plans were correctly parallelizable.

## NU1902 Pre-existing Confirmation

Agree it is pre-existing. Evidence:
1. **No Phase 2 commit touches `Source/DotNetWorkQueue.Transport.SQLite/`.** The PostgreSQL bypass changes are scoped to three files under `Transport.PostgreSQL/` and `Transport.PostgreSQL.Tests/`. Grep of `Transport.SQLite` across all PostgreSQL changes returns zero hits.
2. **No Phase 2 commit touches `Source/Directory.Packages.props`.** The OpenTelemetry `1.15.2` advisory `GHSA-g94r-2vxg-569j` predates Phase 1 completion (commit `99003720`) and is unrelated to the IRetrySkippable bypass work.
3. **ISSUE-032 is already documented in `.shipyard/ISSUES.md` (lines 260–269)** with the correct attribution (pre-existing; `git diff 99003720..HEAD Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj` shows no changes) and a remediation path (either bump OpenTelemetry or add `<NoWarn>$(NoWarn);NU1902</NoWarn>` to Transport.SQLite.csproj, deferred to a future dependency-refresh milestone).
4. **Per-project Release builds remain clean.** Transport.RelationalDatabase, Transport.SqlServer, Transport.PostgreSQL all build with 0 errors per the SUMMARY verification table.

No evidence the NU1902 was introduced or worsened by Phase 2. Not a CRITICAL flag.

## Summary
APPROVE. Plan 3.2 implementation is mechanical, faithful, and symmetric with PLAN-3.1. All three tasks pass with zero deviations. Pre-existing NU1902 escalation on Transport.SQLite is correctly attributed and tracked as ISSUE-032.

Critical: 0 | Minor: 0 | Positive: 6
