# Shipyard Lessons Learned

## [2026-03-27] Milestone: Security & Stability Fixes

### What Went Well
- Deny-list/allow-list binder approach was non-breaking and straightforward to wire via DI
- Per-transport queue name validation with compiled regex caught SQL injection vectors cleanly
- Phase-level security audits caught real issues (deny-list expansion, schema name gap)
- Moving IntegrationTests.Metrics files (preserving namespace) avoided touching ~30 consumer files

### Surprises / Discoveries
- HeartBeatScheduler used hyphens in its internal queue name (`HeartBeatWorkers-{Guid}`), which our own validation caught in CI. Internal queue names need to comply with the same rules.
- IntegrationTests.Metrics types accumulate counter/meter values for test assertions. Core `MetricsNoOp` discards values, so it can't replace them. File-move was the correct strategy.
- AutoFixture `fixture.Create<string>()` generates GUID strings with hyphens, which broke queue name validation in 21 test locations across 3 QueueCreatorTests files.

### Pitfalls to Avoid
- When adding input validation to existing code, audit all internal callers too (not just user-facing paths). Internal code may violate the new rules.
- Don't assume "NoOp" replacements are equivalent without reading how the types are actually used in tests.
- Check Release build (`TreatWarningsAsErrors`) after removing fixture variables -- unused locals that were harmless in Debug become build-breaking in Release.

### Process Improvements
- Use the project's `Guard` class for validation instead of raw `if/throw` -- matches codebase conventions and was flagged in PR review.
- Mark queue name validation as a breaking change in CHANGELOG since existing names with special characters will now throw.

---
