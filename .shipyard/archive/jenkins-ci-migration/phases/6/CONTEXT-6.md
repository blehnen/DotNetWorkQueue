# Phase 6: Remove Thread.Abort — Design Decisions

## Decisions (from brainstorm)
- Remove Thread.Abort() entirely, not deprecate
- Remove AbortWorkerThreadsWhenStopping config property entirely (not mark Obsolete)
- Existing CancellationToken usage is sufficient for cooperative shutdown
- If a thread doesn't respond to cancellation, log and move on — don't abort
- Applies to all targets including net48
- Constraint: PR #82 must merge before code changes (planning can proceed now)
