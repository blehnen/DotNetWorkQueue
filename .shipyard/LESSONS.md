# Shipyard Lessons Learned

## [2026-04-08] Milestone: Replace Schyntax with Cronos (issue #100)

### What Went Well
- 5-phase approach with clear dependency graph (core → parallel transports/tests/logging → cleanup) kept risk contained
- Phase 1 audit and reviews caught the one real issue (unused import) before it could cause Release build warnings
- Scoping Phase 4 down (logging only, not dashboard) avoided a rabbit hole once we discovered DashboardJob lacks a schedule expression field

### Surprises / Discoveries
- NuGet version ordering means 0.9.3 < 0.9.19, so you can't "go back" to a lower version number. Used 0.9.30 instead.
- Cronos 0.12.0 was released with 0 downloads during the work session. Pinning to 0.11.1 (3.8M downloads) was the right stability call.
- CronExpressionDescriptor 6-field handling was flagged as uncertain in research but worked correctly without any special configuration.

### Pitfalls to Avoid
- Builder agents will "helpfully" change version numbers if they think the specified one conflicts with existing entries. Always verify version in the commit, not just the summary.
- Don't assume dashboard APIs can display data that isn't in the stored model. Check the data layer before planning UI/API features.

### Process Improvements
- For milestones with a clear mechanical component (string replacements), doing Phases 2/3/4 directly instead of through builder agents saved significant time
- Planning all parallel phases in one pass (instead of one at a time) reduced round-trips

---

## [2026-04-07] Milestone: Drop net48/netstandard2.0 (issue #101)

### What Went Well
- 4-phase approach with clear dependency ordering prevented cascading failures
- Phase verification caught all issues before moving to next phase
- Memory Linq integration tests (no external dependencies) served as reliable regression gate throughout

### Surprises / Discoveries
- Perl regex `perl -0777 -pe 's/\n*#if NETFULL\b.*?#endif[^\n]*//gs'` fails on nested `#if NETFULL` blocks -- non-greedy match stops at inner `#endif`, leaving orphaned `#else`/`#endif` directives. Python nesting-depth-tracking script required.
- Builder agents exhaust context on bulk file edits -- confirmed across all 4 phases. Direct execution (Perl/Python + csproj edits) is faster and more reliable for mechanical changes.
- Plan file/caller counts can be wrong -- Phase 4 plan missed a 7th JobSchedulerTests caller (JobSchedulerInterceptorTests.cs). Build verification caught it before it could ship.
- Version numbers drift between planning and execution -- ROADMAP said 0.9.3 but actual version was 0.9.18. Always verify current state during research/planning, not just at roadmap creation time.

### Pitfalls to Avoid
- Don't use simple regex for nested preprocessor directives -- track nesting depth with a proper parser
- Don't trust ROADMAP version numbers if the roadmap was written in a different session -- re-check during planning
- When removing a parameter from a shared test method, grep for ALL callers across the entire solution, not just the ones listed in the roadmap

### Process Improvements
- For bulk mechanical edits (preprocessor removal, csproj target changes), skip builder agents and execute directly -- saves 10+ minutes per phase and avoids context exhaustion
- Research phase should always verify current file state, not rely on roadmap descriptions written weeks earlier

---

## [2026-04-07] Milestone: Publish Aq.ExpressionJsonSerializer as NuGet Package (issue #102)

### What Went Well
- Two-repo project with manual gate worked smoothly — Phase 1 (fork prep) and Phase 2 (reference swap) were cleanly separated
- Review gate caught real issues: SDK version pinning (10.0.100 → 10.0.x), missing fetch-depth for Source Link
- Security audit advisory (permissions: contents: read) was a one-liner improvement

### Surprises / Discoveries
- Upstream merges can introduce plain `Dictionary` where `ConcurrentDictionary` was the fork convention — always grep for `Dictionary<` after merging upstream
- Removing `DefineConstants` PropertyGroups can break `#if NETFULL` guards in tests — the net48 TypeAs test failed on GitHub Actions because `NETFULL` was no longer defined
- Test project TFMs can go stale without anyone noticing — `netcoreapp3.1` was EOL and incompatible with CI matrix

### Pitfalls to Avoid
- When updating test project TFMs, check for conditional `DefineConstants` that depend on the old TFM conditions — they may guard platform-specific test behavior
- Don't assume upstream code follows your fork's conventions — review the diff for thread safety, naming, and patterns

### Process Improvements
- CS1591 suppression is pragmatic for third-party forks where you won't write XML docs — enables `GenerateDocumentationFile` for consumer IntelliSense without requiring doc comments on every public member
- Two-repo projects need a manual gate between phases for NuGet indexing — plan for this in the roadmap rather than discovering it during build

---

## [2026-04-06] Phase 1: Dashboard API History Tests (Redis & LiteDb)

### What Went Well
- Pattern replication works well — following MemoryHistoryTests.cs exactly made both new test files straightforward
- Integration tests catch real bugs immediately — LiteDb history tests found a transport bug in QueryMessageHistoryHandler.Get on the first run

### Surprises / Discoveries
- LiteDB `col.Find(x => x.Status == intValue)` does not reliably match recently-updated int fields. The same workaround (FindAll + LINQ Where) was already documented in GetCount but not applied to Get.
- Consumer waitHandle must signal from `onMessageCompleted` (after `CommitMessage.Commit`), not from inside the handler body. History records are still Processing when the handler returns — the status transition happens during commit.

### Pitfalls to Avoid
- When replicating test patterns, check ALL existing workarounds in the transport handlers. A known bug in one method may exist unremedied in sibling methods.

### Process Improvements
- Adding integration test coverage for each transport before shipping transport-level fixes would have caught #103 immediately

---

## [2026-04-06] Phase 1: Redis History Bug Fixes (#104, #103)

### What Went Well
- Small scope with parallel plans completed in one pass — no retries needed
- Purge fix handles orphaned sorted set entries (hash already deleted) — a scenario not in the original issue

### Surprises / Discoveries
- `(long)RedisValue.Null` silently returns `0L` in current StackExchange.Redis — it does NOT throw `InvalidOperationException` as issue #104 assumed. The HasValue guard is still correct for forward safety and making the zero-default intent explicit.

### Pitfalls to Avoid
- Don't assume Redis cast behavior from documentation alone — test it. The implicit conversion behavior may vary across StackExchange.Redis versions.

### Process Improvements
- For small, well-scoped fixes (2 plans, 4 files), the full pipeline runs fast enough that --light isn't needed

---

## [2026-04-06] Phase 1: Fix History Status for Errored Messages (issue #97)

### What Went Well
- Parallel Wave 1 worked cleanly: 3 plans with disjoint file sets completed without merge conflicts
- Review gate caught a real bug: Redis `RedisValue.Null` casts to `(int)0`, which equals `MessageHistoryStatus.Enqueued` — the builder's SUMMARY had the logic inverted

### Surprises / Discoveries
- `RedisValue.Null` cast to `(int)` yields `0`, not an exception. When `Enqueued = 0`, the null case collides with the valid case. Always check `.HasValue` before casting Redis values to integers.
- All 3 builders hit stale `obj/` artifacts when using `--no-restore`; full restore builds were needed. This is a recurring issue in this codebase.

### Pitfalls to Avoid
- When guarding Redis hash reads, never assume the default cast value is "safe" — check the actual enum values. `0` is a valid enum member in most C# enums.
- Don't trust builder summaries that claim null behavior is safe — verify against the actual enum definition.

### Process Improvements
- The review gate continues to prove its value: 1 real bug caught per milestone on average. The null-cast collision would have shipped as a subtle regression (writing phantom Processing entries for non-existent records).

---

## [2026-04-05] Phase 1: Fix History Duration for Fast-Completing Messages (issue #94)

### What Went Well
- TDD discipline caught real bugs post-implementation: the hardened regression test in commit `b538823a` detected a dead SQL block with the same guard pattern that a weaker assertion would have missed
- Scope expansion decision (fix `RecordError` alongside `RecordComplete`) prevented a follow-up PR — the code pattern was identical in both paths
- Architect's semantic improvement — using `CompletedUtc > 0` as the read-side discriminator instead of `DurationMs > 0` — correctly distinguishes "never completed" (null) from "sub-ms completion" (0)

### Surprises / Discoveries
- The SQL WHERE guard bug (`StartedUtc IS NOT NULL`) was subtle: C# computed `0L` correctly and the parameter was set properly, but the UPDATE was a silent no-op because the row didn't match the WHERE clause. Would have shipped a "fixed" display that wasn't actually fixed in the database
- StackExchange.Redis `ConnectionMultiplexer` can't be mocked with NSubstitute (sealed types + extension methods) — required adding a `protected virtual GetDb()` seam to the Redis handlers
- RelationalDatabase `RecordComplete` had been refactored to a two-UPDATE pattern since the roadmap was written, and contained dead code from the migration

### Pitfalls to Avoid
- Don't let tests only assert parameter values when the real bug is in the SQL. The original test would have passed while the bug persisted. Capture and assert the actual command text
- When adding a WHERE clause guard to an UPDATE, verify it doesn't accidentally exclude the valid case you're trying to fix

### Process Improvements
- The reviewer + verifier combination earned its keep: reviewer caught the WHERE guard bug, verifier caught the dead code via the hardened test. Neither would have caught it alone
- Using AskUserQuestion to capture user decisions upfront (CONTEXT-1) set the right scope and avoided mid-build scope creep

---

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

## [2026-03-31] Milestone: Jenkins CI Migration

### What Went Well
- Multi-targeting 22 test projects was mechanical and clean — existing `#if NETFULL` guards handled conditional compilation with zero code changes
- Coverlet integration was trivial (2 commits) — Central Package Management made it a single version entry + per-project references
- Iterative E2E validation caught real issues early — the 8 fix commits during Phase 5 would have been much harder to debug without a running pipeline

### Surprises / Discoveries
- Pipeline evolved from 6 agents to 13 parallel stages — finer granularity provides better load balancing and faster failure isolation than the original plan
- Docker Pipeline plugin was replaced with label-based agents — simpler configuration, works with pre-built images, avoids plugin complexity
- Jenkins agent JRE must exactly match the master's Java version (21) — class file version mismatch causes silent agent launch failures
- Connection strings need to be written to bin output dirs after build, not just source dirs — `dotnet test --no-build` runs from the bin directory
- Redis was refactored to use `connectionstring.txt` despite being marked "out of scope" — consistency across all transports justified the change (PR #87)
- LiteDB csproj reference casing (`LiteDB` vs `LiteDb`) breaks on Linux's case-sensitive filesystem — Windows hides this entirely
- `libsqlite3` + `libdl` symlink needed in Docker image for SQLite tests — the .NET SQLite library loads native libs by name
- BaseMonitor had a disposal race condition (timer callback vs dispose) only visible under Linux timing — never surfaced on Windows

### Pitfalls to Avoid
- `GetObjectData` serialization test needed `#if NETFULL` — not caught until Linux run because `SoapFormatter` doesn't exist on net10.0
- Time offset tests need tolerance on Linux — different clock resolution/behavior than Windows produces slightly different values
- Codecov CLI syntax changes between versions — the `upload-process` subcommand replaced the older syntax; always check current docs
- Don't assume Windows-developed code runs identically on Linux — case sensitivity, native library paths, and timer resolution all differ

### Process Improvements
- Run multi-target builds on Linux early in the process (even in a simple Docker container) to catch platform-specific issues before building the full CI pipeline
- When a change marked "out of scope" keeps causing friction (Redis hardcoded IP), just do it — the cost of the workaround exceeds the cost of the fix

---

## [2026-04-01] Post-Ship: Integration Test Stability

### What Went Well
- Root cause analysis of 99/100 metrics assertion was clean — the handler→commit→metric pipeline made the race obvious once examined
- Polling overload was a minimal, targeted fix (35 lines added, 13 callers updated mechanically)

### Surprises / Discoveries
- `--retry-failed-tests 1` was silently ignored in the Jenkinsfile because the test projects use VSTest, not Microsoft.Testing.Platform. TeamCity had its own retry mechanism built-in.
- Migrating to Microsoft.Testing.Platform (`EnableMSTestRunner`) on .NET 10 is an all-or-nothing change across the solution — partial migration breaks `--collect:"XPlat Code Coverage"` for non-migrated projects
- `TestingPlatformDotnetTestSupport` must be in `Directory.Build.props` (not per-csproj) due to multi-targeting MSBuild evaluation order

### Pitfalls to Avoid
- Don't assume CI test retry is working just because the config is present — verify with actual retry output in logs
- Don't set `TestingPlatformDotnetTestSupport=true` globally unless ALL test projects have `EnableMSTestRunner=true`
- Metrics assertions that compare a counter snapshot to a processed count have an inherent race — the counter increment happens after the handler returns, not during

### Process Improvements
- When tests pass on Windows but fail on Linux, investigate timing/latency differences first — network round-trips to remote services are the most common cause

---

## [2026-04-02] Milestone: Dashboard Improvements

### What Went Well
- Conditional self-contained mode (check config section, embed API if present) kept both deployment patterns working from one codebase
- Multi-stage Dockerfile with layer caching (csproj-first copy) produces a lean runtime image
- Security audit caught real issues: non-root container, auth placeholder UX

### Surprises / Discoveries
- Docker builds on Linux are case-sensitive — `LiteDb.csproj` vs `LiteDB/` directory, `Directory.*.props` lives in `Source/` not repo root. Windows hides all of this.
- `TreatWarningsAsErrors` in Release mode catches nullable warnings (CS8632) that Debug mode ignores — `string?` without `#nullable enable` compiles in Debug but fails Release
- `--no-restore` on `dotnet publish` fails when `COPY Source/` invalidates the restore cache layer — the restore output gets overwritten
- 13 parallel Jenkins stages cloning GitHub simultaneously causes "Maximum checkout retry attempts reached" — rate limiting from the same IP
- `UseRouting()` should come before `UseAuthentication()` in the ASP.NET Core middleware pipeline — reviewer caught incorrect ordering

### Pitfalls to Avoid
- Always verify Dockerfile COPY paths against the actual Linux filesystem with `ls` — don't trust csproj references or Windows conventions
- Don't use `--no-restore` in Docker multi-stage builds where a later COPY invalidates the restore cache
- When adding parallel CI stages, stagger the start times to avoid Git clone storms (5s intervals work for 13 stages)

### Process Improvements
- Test Docker builds early (before review gates) to catch path/casing issues that only surface on Linux
- For ASP.NET Core middleware, always check canonical ordering: UseRouting → UseAuthentication → UseAuthorization → UseEndpoints

---
