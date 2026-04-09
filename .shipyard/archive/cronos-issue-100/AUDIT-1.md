# Security Audit Report -- Phase 1

## Executive Summary

**Verdict:** PASS
**Risk Level:** Low

Phase 1 replaces the vendored Schyntax DLL with two well-maintained NuGet packages (Cronos and CronExpressionDescriptor) and rewrites the schedule parsing logic accordingly. No exploitable vulnerabilities were found. The cron expression input is parsed by Cronos, which uses a deterministic finite-state parser immune to ReDoS. The `Previous()` nullable return is correctly guarded at its single call site. Two minor design-level observations are noted below as advisory items -- neither blocks shipping.

### What to Do

| Priority | Finding | Location | Effort | Action |
|----------|---------|----------|--------|--------|
| 1 | Hardcoded 48h lookback in `PreviousInternal` | JobSchedule.cs:86 | Small | Consider using the caller's `Window` value instead of hardcoded `TimeSpan.FromHours(48)` |
| 2 | `GetOccurrences` iterates full 48h window | JobSchedule.cs:87-98 | Trivial | Use `.LastOrDefault()` via LINQ instead of manual foreach (clarity, no security impact) |

### Themes
- Clean dependency swap -- vendored DLL removed, replaced with pinned NuGet packages
- Null safety handled correctly at the single call site
- No secrets, no unsafe deserialization, no injection vectors in changed code

## Detailed Findings

### Critical

None.

### Important

None.

### Advisory

- **[A1] Hardcoded 48-hour lookback window in `PreviousInternal`** (`Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs:86`) -- The design context (CONTEXT-1.md) states that `ScheduledJob.Window` should be passed as the lookback bound, but the implementation hardcodes `TimeSpan.FromHours(48)`. This is not a security issue, but for schedules running less frequently than every 48 hours (e.g., weekly), the lookback will enumerate many unnecessary occurrences. For schedules more frequent than every 48 hours, it works correctly. Consider accepting a `TimeSpan` parameter or using the design's intended approach in a later phase.

- **[A2] `ExpressionDescriptor.GetDescription` exception handling** (`Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs:49`) -- The `Lazy<string>` wrapping `ExpressionDescriptor.GetDescription(schedule)` will cache any exception thrown on first access (via `LazyThreadSafetyMode.ExecutionAndPublication` default). If CronExpressionDescriptor fails to describe a valid Cronos expression, the `Description` property will throw on every subsequent access. This is unlikely in practice since both libraries understand standard cron, but wrapping in a try-catch with a fallback to `_originalText` would be more defensive.

- **[A3] `InvalidOperationException` includes cron expression text** (`Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs:61,69`) -- The exception message includes the original cron expression string. This is acceptable since cron expressions are not sensitive data (they are schedule patterns configured by the application developer, not user-supplied secrets). No remediation needed.

- **[A4] No NuGet lock file** -- The project does not use `RestorePackagesWithLockFile`. This means dependency resolution is not deterministic across builds. While not a vulnerability, enabling lock files would improve supply chain reproducibility. This is a pre-existing condition, not introduced by this phase.

## Cross-Component Analysis

**`Previous()` null safety is complete.** The `IJobSchedule.Previous()` return type changed from `DateTimeOffset` to `DateTimeOffset?`. There is exactly one call site (`ScheduledJob.cs:98`), and it correctly uses `prev.HasValue` before accessing `prev.Value`. The `HeartBeatScheduler` does not call `Previous()` -- it only uses `AddUpdateJob` which routes through `Next()`. No other consumers of `IJobSchedule.Previous()` exist in the codebase.

**UTC consistency is maintained.** All Cronos API calls pass `TimeZoneInfo.Utc` explicitly (`JobSchedule.cs:58,67,90`). The `_getCurrentOffset` function is sourced from `_getTime.GetCurrentUtcDate()` at all call sites (`ScheduledJob.cs:142`, `JobScheduler.cs:105,124`). No DST confusion is possible.

**Error propagation is safe.** `CronExpression.Parse` throws `CronFormatException` for invalid input, which propagates up from the `JobSchedule` constructor. This is fail-fast behavior during job registration, not at runtime during scheduling. The `Next()` method's `InvalidOperationException` for "no next occurrence" is a defensive guard that would only trigger for pathologically constrained cron expressions.

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | No injection, no auth changes, no user-facing output changes |
| Secrets & Credentials | Yes | No secrets in any changed file |
| Dependencies | Yes | Cronos 0.11.1 and CronExpressionDescriptor 2.45.0 audited (see below) |
| Infrastructure as Code | N/A | No IaC files changed |
| Docker/Container | N/A | No Docker files changed |
| Configuration | Yes | Only doc comment change in IHeartBeatConfiguration; no runtime config changes |

## Dependency Status

| Package | Version | License | Known CVEs | Maintenance | Status |
|---------|---------|---------|-----------|-------------|--------|
| Cronos | 0.11.1 | MIT | None known | Active (HangfireIO org, 1.2k+ GitHub stars, last release 2024) | OK |
| CronExpressionDescriptor | 2.45.0 | MIT | None known | Active (Brady Holt, 1k+ GitHub stars, regular releases) | OK |

**Cronos security notes:**
- Zero transitive dependencies (targets netstandard1.0+)
- Deterministic cron parser -- no regex, no backtracking, immune to ReDoS (CWE-1333)
- `CronExpression.Parse` validates input and throws `CronFormatException` for malformed expressions
- Used by Hangfire (widely deployed job scheduler), indicating production maturity

**CronExpressionDescriptor security notes:**
- Pure string-formatting library with no I/O or network calls
- Generates human-readable descriptions from cron expressions
- Used only in `Lazy<string>` for logging/display, not in any security-sensitive path

## IaC Findings

N/A -- no infrastructure-as-code files were modified in this phase.
