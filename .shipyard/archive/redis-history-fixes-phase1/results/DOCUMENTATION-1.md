# Documentation Report
**Phase:** 1 — Redis History Bug Fixes (issues #103, #104)
**Date:** 2026-04-06

## Summary

- API/Code docs: 0 files require documentation updates (all changed interfaces are internal)
- Architecture updates: 0 sections affected (no design changes, no new components)
- User-facing docs: CHANGELOG.md requires one new entry before this branch ships
- Test coverage notes: 1 implementation deviation worth preserving in code comments

## API Documentation

### WriteMessageHistoryHandler (`Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs`)

- **Public interfaces:** 0 (class is internal to the Redis transport)
- **Documentation status:** No action required

The change adds `.HasValue` guards before `(long)` casts on `RedisValue` results in `RecordComplete` and `RecordError`. These are internal methods with no public surface. The deviation note from SUMMARY-1.1 is worth preserving as a code comment: StackExchange.Redis silently casts `RedisValue.Null` to `0L` today, but the guard is explicit defensive code that does not rely on that undocumented behavior.

**Recommended inline comment** (non-blocking) in `WriteMessageHistoryHandler.cs` at the `HasValue` guard site:

```csharp
// HasValue guard: RedisValue.Null silently casts to 0L today, but the guard
// makes intent explicit and avoids reliance on undocumented implicit cast behavior.
var startedTicks = db.HashGet(...).HasValue ? (long)db.HashGet(...) : 0L;
```

### PurgeMessageHistoryHandler (`Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs`)

- **Public interfaces:** 0 (class is internal to the Redis transport)
- **Documentation status:** No action required

The `protected virtual GetDb()` seam added for testability has no public surface. The corrected purge logic (terminal-state guard + `HasValue` on `CompletedUtc`) has no impact on any documented API contract.

## Architecture Updates

No architecture changes. Both fixes are contained within the Redis transport's existing `Basic/` handler layer. The `protected virtual GetDb()` seam follows the established pattern already present in `WriteMessageHistoryHandler` — no new pattern is introduced.

## User-Facing Documentation

### CHANGELOG.md — entry required before ship

**Type:** Changelog
**Status:** Not yet written — must be added before merging to master

The CHANGELOG follows the established format (`0.9.X — date\n- Fix: ...`). The entry should cover both issues in one block since they ship together. Suggested wording aligned with existing changelog style:

```markdown
### 0.9.19 — TBD (Redis transport only)
- Fix: `WriteMessageHistoryHandler.RecordComplete` and `RecordError` guard Redis
  `HashGet` results with `.HasValue` before casting to `long`; prevents reliance on
  undocumented implicit cast behavior when the history hash is absent (GitHub #104)
- Fix: `PurgeMessageHistoryHandler.Purge()` no longer deletes Enqueued or Processing
  records; only terminal-state records (Complete, Error, Deleted, Expired) with a
  `CompletedUtc` older than the retention cutoff are removed; missing hashes are
  handled without throwing (GitHub #103)
```

Version number should be confirmed against the current package version at ship time.

## Gaps

None that require action before shipping. The only pre-ship requirement is the CHANGELOG entry above.

## Recommendations

1. **Add the CHANGELOG entry** (required, pre-ship). File: `/mnt/f/git/dotnetworkqueue/CHANGELOG.md`.

2. **Optional inline comment** in `WriteMessageHistoryHandler.cs` at the `HasValue` guard to document why the guard exists despite StackExchange.Redis's current implicit cast behavior. Low priority — the SUMMARY-1.1 deviation note preserves this reasoning in the shipyard record already.

3. No README updates, no migration guide, no architecture doc changes needed. This is a pure bug fix with no user-visible behavior change beyond correctness.
