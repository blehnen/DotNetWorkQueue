# Milestone Report: Replace Schyntax with Cronos (issue #100)

**Completed:** 2026-04-08
**Version:** 0.9.30
**Phases:** 5/5 complete
**GitHub Issue:** #100

## Summary

Replaced the vendored Schyntax DLL (custom DSL, no NuGet package) with Cronos (MIT, standard cron expressions) and CronExpressionDescriptor (human-readable descriptions). Breaking change.

## Phase Summaries

### Phase 1: Core library
- Rewrote JobSchedule.cs from Schyntax.Schedule to Cronos.CronExpression
- IJobSchedule.Previous() now returns DateTimeOffset? (nullable)
- Added IJobSchedule.Description property
- Auto-detects 5-field vs 6-field cron by field count

### Phase 2: Transport heartbeat defaults
- 3 Schyntax strings converted to cron equivalents

### Phase 3: Test schedule strings
- 7 Schyntax strings converted across 2 test files, 878 tests pass

### Phase 4: CronExpressionDescriptor logging
- Structured log statements in JobScheduler.cs when jobs are added

### Phase 5: Cleanup, docs, version bump
- Deleted Lib/ directory, updated README/CLAUDE.md/CHANGELOG, version 0.9.30

## Key Decisions

1. Reuse ScheduledJob.Window for Previous() lookback (no new config)
2. Keep Func<DateTimeOffset> constructor param on JobSchedule
3. Auto-detect cron format by field count
4. Pin Cronos to 0.11.1 (0.12.0 had 0 downloads at time of work)
5. Dashboard API scoped to logging only (DashboardJob lacks schedule expression field)
6. Version 0.9.30 (0.9.3 < 0.9.19 in NuGet versioning)

## Quality Gates

| Gate | Result |
|------|--------|
| Build (Debug + Release) | PASS |
| Unit tests | 878/878 |
| Security Audit | PASS |
| Schyntax references | 0 in Source/ |

## Metrics

- Commits: 15
- Files modified: ~20
- Files deleted: 7 (Lib/ directory)
