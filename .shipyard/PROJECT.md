# Project: CONCERNS.md Quick Wins & Accepted Risk Closures

## Description

Address all low-effort items from CONCERNS.md and ISSUES.md in a single PR. This includes marking 4 items as "Accepted Risk / Will Not Fix" with documented rationale, fixing 5 medium/low concerns that require small code changes, and resolving all 13 open issues from ISSUES.md (unused imports, test assertion improvements, log wording, minor code quality fixes).

This is the first of two planned PRs. The second PR (tier B) will handle moderate-effort items like centralized package version management, API exception disclosure, and TODO/HACK comment audit.

## Goals

1. Close 4 concerns as "Accepted Risk / Will Not Fix" with documented rationale in CONCERNS.md
2. Fix 5 small CONCERNS.md items (M-4, M-5, M-8, L-5, N-4)
3. Fix all 13 ISSUES.md items (ISSUE-001 through ISSUE-013)
4. Run unit tests to verify no regressions
5. Update CONCERNS.md and ISSUES.md to reflect resolved status

## Non-Goals

- Tier B items (H-3, H-4, H-6, M-3, M-9, N-3, and design-decision items)
- Architectural changes or new features
- Changing the security model for serialization (C-1/C-2 accepted as-is)
- Dropping .NET Framework 4.8 support (L-3 accepted as-is)

## Requirements

### Accepted Risk Closures
- C-1: Mark as "Accepted Risk" — DenyList binder provides defense-in-depth; transport security is user's responsibility; document AllowList binder option in README for untrusted messages
- C-2: Mark as "Accepted Risk" — Dynamic LINQ execution is by-design; transport security is user's responsibility
- H-1: Mark as "Accepted Risk (Partial)" — Source exists for Schyntax and ExpressionJsonSerializer; JpLabs.DynamicCode is net48-only and goes away when 4.8 is dropped
- L-3: Mark as "Will Not Fix" — Employer requires net48 until .NET 10 migration completes

### CONCERNS.md Fixes
- M-4: Remove xUnit artifact references remaining after MSTest migration
- M-5: Fix malformed `DocumentationFile` path in SQLite `.csproj`
- M-8: Add stale file patterns to `.gitignore`; remove working artifacts from working tree
- L-5: Replace `"TODO; not known"` with meaningful value in `LiteDbConnectionInformation.Server`
- N-4: Rebuild in Release mode to regenerate `DotNetWorkQueue.xml` with current types

### ISSUES.md Fixes
- ISSUE-001: Remove unused `fixture` variable in QueueCreatorTests
- ISSUE-002: Add `RegexOptions.Compiled` to `ValidateQueueName` regex in relational transports
- ISSUE-003: Add `Assert.AreEqual("MyQueue123", test.QueueName)` in relational transport tests
- ISSUE-004: Same assertion fix for Redis, LiteDb, Memory transport tests
- ISSUE-005: Fix stale XML doc comment on Memory `ConnectionInformation` class
- ISSUE-006: Remove 5 unused `using` directives in `RedisConnectionInfoTests`
- ISSUE-007: Use `Timer.DisposeAsync()` instead of `Timer.Dispose()` in `DisposeAsync`
- ISSUE-008: Fix sync-over-async pattern in `DisposeAsync` test assertion
- ISSUE-009: Change "Stopping worker thread" to "Stopping worker" in PrimaryWorker/Worker
- ISSUE-010: Remove unused `using System.Threading` from `WorkerTerminate.cs`
- ISSUE-011: Remove unused `using System.Threading` from `WaitForThreadToFinish.cs`
- ISSUE-012: Create missing SUMMARY file for Phase 7 Plan 01
- ISSUE-013: Add explicit parentheses in `MultiWorkerBase.Running` for operator precedence clarity

## Non-Functional Requirements

- All existing unit tests must pass after changes
- No behavioral changes — all fixes are cosmetic, documentation, or minor correctness improvements
- Changes should be safe across all target frameworks (net10.0, net8.0, net48, netstandard2.0)

## Success Criteria

1. CONCERNS.md updated: C-1, C-2, H-1, L-3 marked with resolution status and rationale
2. CONCERNS.md updated: M-4, M-5, M-8, L-5, N-4 marked as resolved
3. ISSUES.md updated: all 13 issues marked as closed
4. All unit tests pass (`dotnet test` on unit test projects)
5. No new warnings introduced

## Constraints

- Single PR for all tier A changes
- No changes to public API surface
- Dashboard API security items (H-3, M-9) deferred to tier B — document that internal-only deployment is strongly recommended
