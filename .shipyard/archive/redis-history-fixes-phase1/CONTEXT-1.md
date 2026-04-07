# Phase 1 Context: Redis History Fixes

## Decisions

- **Purge guard:** Check Status field (terminal only) + CompletedUtc — belt-and-suspenders approach
- **Test seam:** Add `protected virtual GetDb()` to PurgeMessageHistoryHandler (matching WriteMessageHistoryHandler)
- **HasValue pattern:** Follow exact pattern from PR #105 RecordProcessingStart fix
- **Skip research:** Roadmap has all details, code was already read during brainstorming
