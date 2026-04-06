# Session Handoff — 2026-04-06

## Current Task

All three milestones shipped this session. No active work in progress.

## Approach

Three milestones completed back-to-back:
1. **Issue #97** — Fix history status for errored messages (PR #105, merged)
2. **Issues #104/#103** — Redis history unchecked casts + broken purge (PR #106, merged)
3. **Dashboard API history tests** — Redis & LiteDb integration tests (PR #107, open)

## Tried

- PR #105: Decorator messageId capture + RecordProcessingStart guard in Redis/Memory. Review caught Redis null-cast collision (RedisValue.Null → int 0 = Enqueued). Fixed with HasValue check. Merged.
- PR #106: HasValue guard on StartedUtc + purge logic rewrite with terminal-status-only deletion. Discovery: RedisValue.Null cast to (long) silently returns 0L (doesn't throw). Merged.
- PR #107: LiteDb + Redis history integration tests (38 new tests). LiteDb tests immediately caught a real transport bug in QueryMessageHistoryHandler.Get (LiteDB query engine issue). Open, awaiting Jenkins CI.

## Remaining

- **PR #107** needs merge after Jenkins CI passes
- **Issue #104** filed during session — now resolved by PR #106
- **Branches to clean up after merge:** `fix_redis_history_bugs`, `fix_history_for_error_messages`, `dashboard-history-tests`
- Open GitHub issues: #100 (Schyntax replacement), #101 (JpLabs removal), #102 (Aq.ExpressionJsonSerializer), #96 (Dashboard multi-API), #91 (login dark theme)

## Open Questions

- None — all work is shipped or in open PRs awaiting CI
