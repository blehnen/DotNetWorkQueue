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

## [2026-03-29] Milestone: Thread Management Modernization

### What Went Well
- `Task.Factory.StartNew` with `TaskCreationOptions.LongRunning` is a clean drop-in for `new Thread()` on all targets including net48
- `ManualResetEventSlim` replaced `Thread.Sleep(20)` spin-wait in `BaseMonitor.Cancel()` with instant signaling and a 30s safety timeout
- Removing `IAbortWorkerThread` entirely (not gutting to no-op) was the right call — eliminated dead abstractions cleanly

### Surprises / Discoveries
- `Task.Run` does NOT support `TaskCreationOptions.LongRunning` — must use `Task.Factory.StartNew` instead
- Thread-to-Task migration must cover the entire dependency chain in one pass (WorkerBase field flows through PrimaryWorker, Worker, MultiWorkerBase, WorkerTerminate, StopThread, WaitForThreadToFinish — 6+ classes)
- Internal queue names (HeartBeatScheduler) must comply with validation rules added in earlier milestones — `HeartBeatWorkers-{Guid}` broke our own regex

### Pitfalls to Avoid
- Don't migrate Thread to Task piecemeal — the `Thread` type flows through method parameters across multiple classes, so partial migration won't compile
- JobScheduler integration tests are timing-sensitive on shared CI runners — the `WaitForRollover` / `WaitForEnQueue` pattern can miss the second enqueue on slow machines
- `Task` has no `.Name` property — need a separate `WorkerName` string field for diagnostics/logging

### Process Improvements
- Skip timing-sensitive integration tests on GitHub Actions using `--filter "FullyQualifiedName!~TestName"` in the workflow
- When removing a feature (Thread.Abort), grep for all references before planning — the roadmap's initial "gut to no-op" approach was changed to "delete entirely" after research showed no consumers

---
