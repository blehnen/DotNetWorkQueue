# Security Audit: Phase 1

## Scope

**Branch:** `history_quick_processing_display`
**Tag range:** `pre-build-issue-94-phase-1`..`HEAD`
**Commits audited:** 8 (a2d2337e, 171c796f, 8cf57c0c, b538823a, 686117bc, 08ce80be, a79cec3c, 03a356db)

**Files audited (production code):**

| File | Change type |
|------|-------------|
| `Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs` | Modified |
| `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs` | Modified |
| `Source/DotNetWorkQueue.Transport.LiteDB/Basic/WriteMessageHistoryHandler.cs` | Modified |
| `Source/DotNetWorkQueue.Transport.LiteDB/Basic/QueryMessageHistoryHandler.cs` | Modified |
| `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` | Modified |
| `Source/DotNetWorkQueue.Transport.Redis/Basic/QueryMessageHistoryHandler.cs` | Modified |
| `Source/DotNetWorkQueue.Dashboard.Ui/Components/Shared/HistoryTab.razor` | Modified |

**Test files audited:** 6 (WriteMessageHistoryHandlerTests for Memory, RelationalDatabase, LiteDb, Redis; QueryMessageHistoryHandlerTests for LiteDb and Redis — both new and modified)

**Estimated LOC changed:** ~350 production lines, ~400 test lines.

---

## Verdict: CLEAN

**Risk Level: Low**

All changes in Phase 1 are tightly scoped to normalizing a display value (`DurationMs = 0` instead of `null`) when a message completes before its processing-start timestamp is persisted. No new attack surface was introduced. The SQL changes removed a restrictive WHERE clause guard (making an UPDATE correctly fire rather than silently skip), and all SQL parameters remain fully parameterized throughout. No secrets, no dependency changes, and no configuration changes are present in this diff.

---

## Findings

### Critical
_None._

### Important
_None._

### Suggestions

- **SQL: WHERE clause narrowing could mask future races** — `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs`, line 121 (RecordComplete second UPDATE). The guard was changed from `StartedUtc IS NOT NULL AND CompletedUtc IS NOT NULL AND DurationMs IS NULL` to `CompletedUtc IS NOT NULL AND DurationMs IS NULL`. This is correct for the stated bug. However, removing `StartedUtc IS NOT NULL` means a row whose `StartedUtc` is genuinely null (never started, only enqueued) could now have a duration written to it if `CompletedUtc` was somehow set and `DurationMs` is NULL. In the current write flow this is safe because `CompletedUtc` is only written by `RecordComplete` and `RecordError` after the row has been confirmed to exist. No action required, but a code comment documenting why `StartedUtc IS NOT NULL` was deliberately removed would prevent a future maintainer from re-adding it incorrectly.

- **`FormatDuration` Razor helper: negative-duration edge case not guarded** — `Source/DotNetWorkQueue.Dashboard.Ui/Components/Shared/HistoryTab.razor`. The function handles `null`, `0`, and positive values. A negative `DurationMs` (e.g., from a clock skew between nodes) would fall through to `{ms}ms` and display a negative number in the UI. This is a cosmetic display defect, not a security issue, but worth a defensive `if (ms < 0) return "< 1 ms";` guard.

- **Redis test subclass exposes production virtual seam** — `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` and `QueryMessageHistoryHandler.cs`. Both now declare `protected virtual IDatabase GetDb()` to allow test injection. This is a common and acceptable test seam pattern. The seam is not exploitable because `IDatabase` is not user-supplied at runtime — it comes from the `IRedisConnection` dependency injected at construction. No remediation needed, but the design note is worth recording: if `GetDb()` is ever made `public` virtual, it would become an extension point that bypasses the connection factory.

---

## Areas Examined

| Area | Checked | Notes |
|------|---------|-------|
| SQL security (injection, parameterization) | Yes | All SQL uses `AddParameter` with typed `DbType` bindings. No string concatenation of user input into query text. The `$@"UPDATE ..."` interpolations use only `_tableNameHelper.HistoryName` (set at construction from `ITableNameHelper`, not user input). |
| Data exposure | Yes | `FormatDuration` displays `< 1 ms`, `{ms}ms`, `{s}s`, or `{m}m`. No message content, queue names, connection strings, or internal identifiers are rendered. |
| OWASP A01 (Access Control) | Yes | No authorization logic changed. History write methods are called only from internal queue-worker code paths, not from API endpoints. |
| OWASP A03 (Injection) | Yes | No injection vectors introduced. Razor output is a static string or formatted numeric literal — no raw HTML from user-supplied data. |
| OWASP A05 (Security Misconfiguration) | Yes | No config files changed. |
| Secrets & Credentials | Yes | No API keys, connection strings, tokens, or credentials in any changed file. Test connection strings use `Filename=:memory:` (LiteDB in-memory) — not a real credential. |
| Dependencies | Yes | No NuGet packages added, removed, or version-bumped. Lock files not changed. |
| Infrastructure as Code | N/A | No Terraform, Ansible, or Dockerfile changes. |
| Docker / Container | N/A | No container configuration changed. |
| Configuration | Yes | No appsettings, environment files, or CI workflow files changed. |

---

## Cross-Component Analysis

**DurationMs=0 write/read symmetry is correctly maintained across all four transports.** Each transport that writes `0L` on the write side also reads it back correctly on the read side:

- **Memory:** In-memory dictionary; `DurationMs` is typed `long?` on the record — `0L` flows through unchanged. No read-side mapping exists that would suppress it.
- **LiteDB write side:** Ternary sets `0L` explicitly. **LiteDB read side** (`QueryMessageHistoryHandler.cs`) was also changed: `DurationMs = h.DurationMs > 0 ? h.DurationMs : (long?)null` became `DurationMs = h.CompletedUtc > 0 ? h.DurationMs : (long?)null`. This is the correct discriminator — a completed row with `DurationMs=0` now survives the mapping and reaches the UI as `0`, not `null`.
- **Redis write side:** `0L` is written into the hash. **Redis read side** was similarly corrected: `DurationMs = durationMs > 0 ? durationMs : (long?)null` became `DurationMs = completedTicks > 0 ? durationMs : (long?)null`. Same pattern, same correctness.
- **RelationalDatabase:** The SQL WHERE guard removal allows the `DurationMs=0` UPDATE to execute. The value is passed via a fully typed `DbType.Int64` parameter, not a nullable coalescing expression.

**The UI formatter correctly sits at the end of the pipeline.** `FormatDuration(long? ms)` checks `!ms.HasValue` before `ms == 0`, so a never-started row (null) still displays `-`, while a sub-millisecond completed row (0) displays `< 1 ms`. The three-way distinction (null/0/positive) is preserved end-to-end.

**No authorization boundary changes.** All modified code paths are write-side history recording (called from internal consumer worker threads) or read-side history projection (called from the Dashboard API layer, which has its own auth middleware not modified in this phase). The audit found no path by which a change in this diff could affect which rows a user can access or modify.

---

## Dependency Status

No dependencies were added or changed in this phase.

| Package | Version | Known CVEs | Status |
|---------|---------|-----------|--------|
| (none changed) | — | — | N/A |

---

## IaC Findings

No infrastructure-as-code files were changed in this phase.

---

## Conclusion

Phase 1 is a well-contained, low-risk change. The SQL modifications are safe: parameterization is unchanged, and the WHERE clause simplification is correct and necessary. The read-side fixes across LiteDB and Redis correctly mirror the write-side changes, maintaining a consistent `DurationMs=0` contract through the full data pipeline to the UI. No secrets, dependency changes, or authorization logic are touched. The phase may proceed to ship.
