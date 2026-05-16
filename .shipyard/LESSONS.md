# Shipyard Lessons Learned

## [2026-04-18] Milestone: Dependency Refresh (PR #118)

### What Went Well
- Aggressive one-pass strategy delivered: 8 major bumps + 15 low-risk bumps + 1 CVE fix on a single branch, zero reverts, zero aborts, Jenkins green on first full run.
- Per-bump atomic commits preserved revert granularity on the feature branch while the PR squashed cleanly on merge — best of both worlds.
- Pre-flight landmine enumeration from prior CLAUDE.md lessons (MudBlazor attr renames, sealed-transport mocking, `-p:CI=true` discipline) meant every predicted risk either hit where expected or was absorbed by existing abstractions.
- Direct-edit-over-agent-dispatch for bulk manifest edits (per prior session lesson) kept the Phase 2 + Phase 3 bump commits fast and deterministic.

### Surprises / Discoveries
- Swashbuckle 7→10 dragged in a Microsoft.OpenApi 1→2 namespace restructure — types flattened from `Microsoft.OpenApi.Models.*` to root `Microsoft.OpenApi`, `AddSecurityRequirement` became a lambda, `OpenApiSecurityRequirement` values changed type. 5 edits across Dashboard.Api + swagger tests. Non-obvious from the Swashbuckle release notes.
- Npgsql 8→10 and SqlClient 6→7 compiled clean on first try despite being classified as "high-risk" — the codebase's `IDbConnection` discipline absorbed both leaps with zero migration surface. Risk classification was precautionary; actual cost was zero.
- PLAN-5.1's initial draft target version for the CVE fix (`System.Security.Cryptography.Xml 8.0.2`) was itself the vulnerable version named in the CVE advisory. Caught in plan critique, not runtime — but if it had slipped past critique the build would have "fixed" the warning by silently re-introducing the vulnerability.

### Pitfalls to Avoid
- Do not author CVE-fix plans against "bump to latest" — always cite the advisory's explicit patched version. The vulnerable version often sits numerically close to the fix.
- Do not let `.shipyard/STATE.json` drift uncommitted across session boundaries. The next `git pull` blocks on "local changes would be overwritten" and is easy to misdiagnose as a tag/remote issue (happened twice this milestone).
- Do not assume a release-notes landmine list is exhaustive. Swashbuckle 10's OpenApi v2 dependency was buried; only hit when building.

### Process Improvements
- Consider a pre-pull git hook that auto-stashes `.shipyard/STATE.json` to eliminate the session-drift foot-gun at the workflow level rather than via discipline.
- Shipyard's `/shipyard:ship` ran on an already-merged milestone in this case — that worked but felt off. Future milestones: run ship BEFORE merge when possible, so the lessons-capture question is fresh in-context rather than a retrospective.

---

## [2026-04-09] Milestone: Dashboard UI — Support Multiple API Sources (issue #96)

### What Went Well
- TDD across all phases caught issues early — 48 UI tests + 11 integration tests provided strong safety net
- Parallel plan execution (Phase 3) saved time — zero shared files between UI and test plans
- 7 shared tab components required zero changes due to clean `IDashboardApiClient` abstraction via `[Parameter]`
- Clean wave/phase separation — each phase produced testable interfaces before downstream phases consumed them

### Surprises / Discoveries
- `DotNetWorkQueue.IConfiguration` shadows `Microsoft.Extensions.Configuration.IConfiguration` — C# resolves via namespace hierarchy BEFORE considering `using` directives. Requires `global::` fully-qualified types in all Dashboard.Ui code.
- NSubstitute indexer mocking fails on `IFeatureCollection` — use real `FeatureCollection` with `Set<T>()` instead
- MudBlazor 9.x uses `Expanded` not `IsInitiallyExpanded` on `MudExpansionPanel` — builder agents don't know current MudBlazor API
- `GetSettingsAsync()` is the lightest health probe endpoint — avoids data store queries

### Pitfalls to Avoid
- Builder agents struggle with C# namespace conflicts and MudBlazor API changes — do complex fixes directly instead of retrying the agent
- Agents often don't write result files (SUMMARY, REVIEW, RESEARCH) to disk — the orchestrator must create them from agent output
- `OnParametersSetAsync` must guard both route slug AND entity IDs (ConnectionId, QueueId) — slug-only guard causes stale data when navigating between entities in the same source

### Process Improvements
- Pass `global::` namespace conflict knowledge to all builder agents upfront (in CONTEXT files)
- Check for result files immediately after agent completion and write them if missing
- Run existing tests before AND after each plan to catch regressions early

---

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

## [2026-04-13] Code Coverage Milestone — Phase 5: Dashboard.Api DashboardExtensions

### What Went Well
- The balanced-budget plan shape (3 parallel unit-test plans in Wave 1 + 1 sequential integration-test plan in Wave 2) was the right structure for a DI/startup file where coverage gaps cluster by configuration surface (Swagger, CORS, auth, IConfiguration)
- All 4 plans passed reviewer on first attempt (4× PASS, 0 retries) — the research phase's upfront classification of "what clusters are unit-testable vs. must-be-integration-testable" paid off
- The Wave 2 scope extension (PLAN-2.1 absorbing PLAN-1.3's dropped branch guard) kept delivery on track without a new plan cycle
- Full milestone outcome: 5 phases, ~130 new tests, 2 production refactors, coverage 88.9% → projected ~90%, zero regressions

### Surprises / Discoveries
- `AddControllers(action)` in a bare `ServiceCollection` propagates filters but silently drops `MvcOptions.Conventions` — 4 debugging iterations before the pivot decision. Root cause is ASP.NET Core's internal `ConfigureMvcOptions` pipeline behaving differently without a real `IHostEnvironment`. Lesson captured in CLAUDE.md for future ASP.NET Core test authors.
- A mid-build session that was interrupted 9 times across multiple days still resumed cleanly via `/shipyard:resume` — the STATE.json + HISTORY.md + artifact inspection was sufficient to reconstruct intent without losing work
- Retroactive reviewer gates work: when a build session interrupts between `Step 4b: Collect Results` and `Step 4c: Review Gate`, the reviewer can run against the committed SUMMARY + git diff after the fact and produce the same verdict it would have during the original session

### Pitfalls to Avoid
- When a unit test for DI wiring is hitting the 4th debugging iteration, STOP and pivot to an integration test. The root cause is usually framework-internal behavior that won't be documented publicly and that fights back against "clever" workarounds. The direct `.Apply()` unit test + integration test combo is almost always cleaner than forcing the bare `ServiceCollection` path.
- Don't bundle multiple `[TestClass]` types into one .cs file in an integration test project when every existing file is one-class-per-file — it breaks navigation and diverges from convention even if it "works". Split up front; don't wait for the simplifier gate.
- Build session interruptions silently leave state stale: STATE.json said "Building wave 1" for ~24 hours across 9 interruption notes even though all 4 plans were committed. The phase-5 resume worked because artifacts were the source of truth, not STATE.json — but it's a reminder that STATE.json should not be trusted alone when a resume happens after interruptions.

### Process Improvements
- For phases where plans classify into unit-testable-cluster vs integration-testable-cluster, have the researcher produce a Wave-1-can-trigger-Wave-2-scope-change contract explicitly in RESEARCH.md — that's what made the PLAN-1.3 → PLAN-2.1 scope extension frictionless this phase
- After any `/shipyard:resume`, explicitly reconcile STATE.json against artifact inventory (plans/*, results/SUMMARY-*, results/REVIEW-*, git log) before deciding what to do next. The session's first action should be "what's real" vs "what does STATE say", and trust the artifacts
- The retroactive reviewer pattern (dispatch N parallel reviewers for already-committed plans, then proceed to verifier) is safe and efficient — document it as a supported resume path if a build session interrupts between waves and review gates

---

## [2026-04-15] Milestone: TaskScheduler 0.4.0 + DNQ Integration Tests + CI Wiring (issue #6)

**Shipped via PR #115. Cross-repo: Phases 1-2 in sibling `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`, Phases 3-4 in DNQ.**

### What Went Well

- **Optimistic UDP multicast on Jenkins Docker paid off.** CONTEXT-4 decision #1 (ship without pre-emptive `[TestCategory("BeaconRequired")]` skip) turned out correct — NetMQ beacon discovery worked on the Docker bridge network on the first Jenkins run of PR #115, and no follow-up skip mechanism was needed.
- **Phase 1's NetMQ poller refactor held under real concurrency load.** ConcurrencyRegressionTests (12 threads × 5000 iters, 30s deadlock detector) passed in 5/5 consecutive runs with ~2s wall clock per run. The fix is real and the regression guard catches it.
- **Phase 2's tag-triggered GH Actions publish path mirrored 0.3.0 cleanly.** No local `dotnet nuget push` needed; both `.nupkg` and `.snupkg` published with green badges on nuget.org via run 24423676631.
- **Phase 4's additive-only CI change** (15 lines across 2 files, 0 deletions, 0 modifications) made both Jenkins and GitHub Actions integration boringly predictable once the merge conflict was resolved.

### Surprises / Discoveries

- **Jenkins is PR-triggered, not branch-triggered.** Any "feature-branch-first validation" workflow MUST open a (draft) PR before Jenkins will pick it up. Pushing a feature branch alone is insufficient. **Implication:** the Shipyard worktree workflow's "push for validation" step for CI-sensitive phases must include `gh pr create --draft` as the actual trigger, not just `git push`.
- **Agent turn budget exhaustion is the dominant failure mode** for multi-task builder dispatches. 4/5 Phase 3 builder agents dropped their commits or SUMMARY files mid-response, forcing the main driver to take over. **For 1-task plans with exact-match anchors, direct edits from the main thread are faster and more reliable than dispatching an agent.** This became Phase 4's default execution strategy and worked well.
- **`git fetch origin` silently skipped at milestone start caused the ship-time merge conflict.** Local master worked the TaskScheduler milestone in isolation (9 commits) while origin was shipping Phase 5 dashboard-coverage via PR #114 (5 commits) in parallel. The two diverged until PR #115 couldn't merge. Recovery: merge origin/master into local master, resolve conflicts in `.shipyard/ROADMAP.md` (ours) + `.shipyard/HISTORY.md` (concatenate) + `.shipyard/STATE.json` (ours) + delete a stray archived phase file, then rebase `phase-4-ci-wiring` onto the unified master. **Always `git fetch origin master` before `/shipyard:plan 1` on a new milestone.**
- **Plan "mirror project X" directives can contradict literal specs** when project X has drifted from the plan author's mental model. PLAN-1.1 told the Phase 3 builder to mirror `Memory.Integration.Tests` AND spec'd `net10.0;net8.0`, but Memory is `net10.0`-only — the builder had to choose, and the plan's literal spec was wrong. Spot-check inheritance targets at plan time with an actual `cat` / `grep`, not from memory.
- **Shared test runners may not expose the seam the plan assumes.** PLAN-2.1 told the Phase 3 builder to "use `DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.SimpleConsumer.Run<>()`" to inject the distributed task scheduler — but that runner's signature exposes `Action<TTransportCreate> setOptions` (transport options), not `Action<IContainer> registerService` (container registration). Research must verify the exact seam exists before the architect commits to a plan pattern.
- **Memory transport storage is per-container.** Two `QueueContainer<MemoryMessageQueueInit>` instances don't share the underlying `IDataStorage` via `RegisterNonScopedSingleton(scope)` alone. A naive producer/consumer split across two containers never sees the produced messages. Phase 3 EndToEndSchedulingTests was scope-reduced to a SimpleInjector `Verify()` smoke test because of this constraint.
- **PLAN-1.2 had a plan data error** (expected "unstash count = 13" but pre-edit actual was 14). Research should spot-check grep counts with exact commands, not estimated counts.
- **The auditor agent wrote `AUDIT-4.md` to the WRONG directory** (`.shipyard/phases-archive-code-coverage/4/results/` instead of `.shipyard/phases/4/results/`). The agent's path resolution found an existing archive directory and wrote there by default. Had to move the file to the correct location before commit. **Watch for this in future ship flows where an archive directory with the same shape exists.**

### Pitfalls to Avoid

- **`SchedulerContainer.GetInstance<T>()` does not exist in `TaskScheduler 0.4.0`.** The only way to resolve `ITaskSchedulerJobCountSync` is the **IContainer closure pattern**: capture `IContainer` during the `SchedulerContainer(registerService)` callback, trigger build via `CreateTaskScheduler()`, then resolve from the captured container. An earlier NodeDiscoveryTests draft used the nonexistent API and produced 10 compile errors; rewritten during build recovery using the pattern discovered by the PLAN-2.2 builder.
- **`Start()` before spawning hammering threads is non-negotiable for `ConcurrencyRegressionTests`.** Without `Start()`, `_outbound` is null and the null-safe guard from Phase 1 short-circuits every `IncreaseCurrentTaskCount` / `DecreaseCurrentTaskCount` call — the test becomes a false positive that would pass even if Phase 1's lock fix were reverted. A comment in the test documents this invariant explicitly.
- **DNQ queue names must be alphanumeric/underscore/dot.** `Guid.NewGuid().ToString()` produces hyphenated strings that DNQ validation rejects with `Queue name contains invalid characters`. Use `Guid.NewGuid().ToString("N")` (no hyphens) or a sanitized format.
- **Cross-namespace walk-up gotcha for `IDataStorage`.** Cloning `SharedClasses.cs` from the Memory test project into a differently-named namespace breaks walk-up resolution of `DotNetWorkQueue.Transport.Memory.IDataStorage`. Same root cause as the existing `IConfiguration` and `Metrics.Metrics` shadowing lessons. **Always add `using DotNetWorkQueue.Transport.Memory;` explicitly when cloning from that project.**
- **`-p:CI=true` is a NuGet packaging flag, not a test-run flag.** It enables `ContinuousIntegrationBuild` in `Directory.Build.props` for deterministic Source Link during `dotnet build -c Release`. It has no effect on `dotnet test`. Don't copy-paste it into test invocations — the Phase 4 CONTEXT-4 draft made this mistake and had to be corrected mid-research.
- **Jenkinsfile stagger formula `(n-1) * 5` is at its ceiling with 14 stages** (worst-case startup delay 65s). A future 15th stage will push to 70s and may need the formula revisited or a different approach. Captured as a known issue for a future cleanup phase.

### Process Improvements

- **Before `/shipyard:plan 1` on any new milestone: `git fetch origin master && git log HEAD..origin/master --oneline`** to confirm local master is current. If origin has unpulled work, decide whether to pull or branch from origin/master BEFORE the milestone starts, not at ship time.
- **For 1-task plans with exact-match anchors, execute inline from the main thread** rather than dispatching a builder agent. Agent reliability for tiny plans is worse than direct edits because of turn budget exhaustion. Reserve builder agents for multi-task plans or plans that require iterative API discovery.
- **Always `gh pr create --draft` for CI-sensitive feature branches.** The Shipyard worktree protocol's "feature branch first" validation step needs a draft PR to trigger Jenkins, not just a push. Update `/shipyard:worktree create` docs / the CONTEXT capture template to mention this.
- **When cloning a file from one test project into another, do a namespace walk-up sanity check.** For every `using` the new file relies on implicitly, verify the new project's namespace hierarchy can reach it. When in doubt, add explicit `using` directives up front rather than debugging walk-up resolution later.
- **Auditor agent path-resolution hardening:** the auditor should verify the path it's writing to is under `.shipyard/phases/{N}/results/` and NOT under `.shipyard/phases-archive*/`. Captured as a Shipyard framework feedback item.

---

## [2026-04-17] Milestone: DNQ Automated NuGet Publishing via GitHub Actions

### What Went Well
- **Phase scope reality-check during research.** Phase 3's research step discovered in ~60 seconds that `deploy/*.{bat,nupkg,snupkg}` files were already untracked (`git ls-files deploy/` empty; `.gitignore` covered them via `*.nupkg`/`*.snupkg`/`Deploy.bat` patterns). The roadmap's "git rm 25 files" step was entirely moot. The plan was revised from 3 tasks to 2, a `.gitignore` intent-rule was added instead of a behaviorally-meaningful rule, and the cleanup became a plain `rm` instead of a `git rm`. Cost of the sanity check: nil. Cost of discovering the mistake at build time: a wasted task + embarrassing commit messages + possible retries. Pattern for future cleanup phases.
- **Orchestrator-direct execution across all 3 phases avoided the repo's builder-agent stall pattern.** Project memory already documented that builder/researcher agents have a history of stalling on this codebase (`feedback_agent_lockups.md`). This milestone wrote CONTEXT.md / RESEARCH.md / plan files / SUMMARY.md / REVIEW.md inline via the orchestrator, dispatched only read-only auditor + simplifier agents (both completed cleanly), and used atomic task-scoped Bash commits. Zero stalls across 14 commits.
- **Review-feedback applied in the same build cycle, not as a retry.** Phase 2's auditor (MINOR) and simplifier (LOW) findings were applied in a single follow-up commit (`940a9a68`) within the same `/shipyard:build 2` invocation. Full retry cycles exist for CRITICAL findings; MINOR / LOW findings are better handled by a feedback commit that keeps the build momentum.

### Surprises / Discoveries
- **GitHub's `/commits/{sha}/statuses` (plural) endpoint is a live landmine for naive jq filters.** The roadmap's original design used `.[] | select(.context=="...") | .state`, which against a commit with 15+ historical `pending` updates emits a multi-line blob. `[[ "$state" == "success" ]]` then silently fails because multi-line bash string comparison never matches a single-word target. Phase 1.5's manual API test against PR #116 exposed this before the workflow shipped. The `/status` (singular) rollup endpoint returns latest-per-context and was the correct fix.
- **Phase 1.5 "side task" as an explicit unblock gate was load-bearing.** Without the side-task discipline (discover the exact Jenkins context string + test against real commits), the workflow would have shipped with a silent-fail gate. Pattern: whenever a plan references a literal string that isn't in the codebase (`continuous-integration/jenkins/branch` was discovered, not known), carve out an XS discovery task and gate downstream planning on its completion.
- **Roadmap-to-reality drift is real even on recently-authored roadmaps.** The roadmap was authored 2026-04-16; Phase 3's reality-check ran 2026-04-17. In that single day, no drift occurred in the codebase, yet the roadmap's assumption about tracked `deploy/*` files was wrong from the start. Authorship-time correctness is not the same as run-time correctness.

### Pitfalls to Avoid
- **Don't use `\w` in bash `[[ =~ ]]` regex.** Bash's ERE does not support PCRE classes. POSIX bracket classes (`[A-Za-z0-9\.-]`) work everywhere bash runs.
- **Don't omit `2>/dev/null` on `ls | wc -l` under `set -euo pipefail`.** Without it, an empty-glob `ls` returns non-zero and `set -e` kills the script before `wc -l` can produce `0`. The `2>/dev/null` is load-bearing, not cosmetic.
- **Don't inline `${{ secrets.X }}` into `run:` when you can bind via `env:` block.** Inline is masked by GH but `env:` isolation is the documented best practice — survives `set -x`, survives CLI self-logging, and survives any future tool that echoes its command line.
- **Don't pass inline `${{ }}` values into bash strings without considering injection.** `GITHUB_REF_NAME` is user-controllable (a malicious actor can push a tag named ``"; rm -rf /"``). In `publish.yml`, the tag-regex gate runs BEFORE any bash interpolation of `GITHUB_REF_NAME`, and the regex's character class rejects shell metacharacters — fail-closed by construction.

### Process Improvements
- **Codify "reality-check" as a research-agent standing instruction for cleanup / migration / retirement phases.** The `git ls-files`, `git grep`, `.gitignore` test pattern takes under a minute and prevents planning against false premises. Low effort, high leverage.
- **Discovery-task gate pattern:** any plan that requires a literal string not in the codebase gets an XS-sized "discover and document" task marked as an explicit gate before downstream plans can be authored. The Phase 1.5 pattern worked; make it the default for similar situations.
- **Use `TaskOutput` with `block=true` for long-running verifications.** Background `dotnet build` + `TaskOutput` polling keeps the orchestrator alive for other setup work in parallel. Used during ship verification — cleaner than `sleep`-polling.

---


## [2026-05-15] Milestone: Outbox Pattern Support for Relational Transports (PR #138)

### What Went Well

- **Phased delivery with explicit CI-gating between waves.** CONTEXT-6 Decision 4 ("Wave 1 → draft PR → Jenkins SqlServer green → Wave 2") caught the Phase 3 extractor symmetry bug before PG work started in Phase 6. Reusable pattern for future split-CI phases on this repo: gate cross-transport waves behind targeted Jenkins green signals rather than waiting for the full matrix at the end.
- **Per-phase REVIEW agents catch real defects.** REVIEW-1.2 (Phase 7) found a missing `using DotNetWorkQueue.Configuration;` in the outbox tutorial that would have shipped as a broken copy-paste-able example. Without the review gate this would have surfaced as an early-user complaint. Worth the agent cost.
- **Multi-stage gates (verifier + critique + per-plan reviewer) caught different classes of issues.** Verifier validated requirement coverage; critique validated file paths / API signatures match current code; reviewers validated implementation against plan. Each gate is cheap individually and they don't substitute for each other.

### Surprises / Discoveries

- **Plan code shapes can drift from reality between authoring and build.** Phase 6 PG Wave 2 plans were finalized hours before commit `9858f04f` did the `Tx → Transaction` rename. Builder followed plan literally → 34 `tx` references landed in PG outbox tests inconsistent with the just-renamed SqlServer files. Simplifier caught it during Phase 6 close-out; resolved in commit `ef848165`.
- **Phase title can imply work that's already done.** ROADMAP Phase 7 said "XML doc comments on every public type/member added in phases 2-4." RESEARCH §1 surfaced that builders had already added docs as they went. The real Phase 7 work turned out to be CSPROJ GATE FIXES + a verification pass — much smaller than ROADMAP anticipated. Researcher's first job in each phase should be "validate the phase scope against current code state."
- **Pre-existing csproj asymmetry hidden by per-phase scoping.** `Transport.RelationalDatabase.csproj` had `<DocumentationFile>` only on the `Release|net10.0` block — the `Release|net8.0` block was missing it. This had been true since the multi-target work but no phase before Phase 7 ran a full-solution Release build to expose the gap.

### Pitfalls to Avoid

- **When introducing a comparator, normalize BOTH sides of the comparison or NEITHER.** Phase 3 introduced `SqlServerExternalDbNameExtractor` with `.ToUpperInvariant()` on the extractor side. The validator on the other side compared with `StringComparer.Ordinal` against the verbatim `IConnectionInformation.Container`. False mismatches surfaced only when Phase 6 ran integration tests with a mixed-case catalog. The pattern: any new "compare these two strings" code MUST verify both upstream sources apply identical normalization (or both apply none). Fix landed in commit `994e1404` (pass-through on both sides).
- **Mid-build phase-wide renames leave landmines in outstanding plan files.** ISSUE-036 (`Tx → Transaction`) landed during Phase 6 build. Plans 2.1 and 2.2 for the same phase were authored before the rename. The reviewer agent for PG Wave 2 didn't cross-check against the freshly-renamed SqlServer reference files — both reviewers passed both plans without flagging the `tx` divergence. **Pattern to adopt:** when a phase-wide rename lands mid-build, run `grep -nP "\bOldToken\b" .shipyard/phases/*/plans/*.md` and refresh outstanding plan files before kicking off subsequent waves.
- **Don't preempt later phases' scope in earlier phase builders.** Phase 2-4 builders added XML doc comments as they went — Phase 7 plans had originally allocated time for that work and would have wasted hours re-authoring. Acknowledge what each phase's builders ACTUALLY do (e.g., "Phase 2-4 builders write XML docs as they go") in ROADMAP so later phase scoping is accurate.

### Process Improvements

- **RESEARCH §1 should always validate phase scope against current code state.** Especially for phases that follow a docs/tests-only phase or a phase with cross-cutting builder discretion. The Phase 7 reframe (`authoring pass → verification pass + csproj gate fixes`) saved significant builder time. Future researcher prompts should explicitly include: "Confirm the work this phase is scoped for has not been incidentally done by earlier phases."
- **Cross-transport symmetry should be its own dedicated REVIEW dimension.** When two transports get parallel implementations (SqlServer + PostgreSQL), the reviewer for the second transport should explicitly diff against the first transport's freshly-shipped files, not just the plan code shape. The Phase 6 `tx` rename inconsistency would have been caught by the PG reviewer if cross-transport symmetry was a checklist item.
- **ISSUE-032-style "advisory carry-forward" closure is a viable pattern for ship-blocking advisories that are out of scope.** `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` demoted the SQLite escalation from error to warning without suppressing the advisory itself. The advisory still surfaces on every Release build (so the long-term remediation isn't forgotten). Pattern reusable for future advisory carry-forwards.
- **For Shipyard tracking artifacts: write file path + 1-line description in MILESTONE-REPORT.md, not the full content.** When the artifact is large (e.g., a per-phase VERIFICATION.md), the milestone report should point to it, not duplicate it. Reduces context bloat at ship time and keeps artifacts authoritative.

---

## [2026-05-16] Post-ship: 0.9.36 NuGet release flow + wiki update

### Surprises / Discoveries

- **`git tag <name>` without an explicit SHA tags HEAD, which is not always the merge commit.** Initial v0.9.36 tag landed on the release branch's tip commit (`bb44244`, the third commit in PR #140) instead of the master merge SHA (`4a332c56`). `publish.yml`'s verify-gate is the safety net here — it reads the singular `/status` rollup on the tag SHA and fails fast when the status is `missing` (Jenkins only posts `success` on the merge commit, not on individual feature-branch commits even though those commits are reachable from master post-merge). Recovery: `git tag -d`, `git push origin :refs/tags/<name>`, re-tag with explicit SHA, push. Zero NuGet pollution because publish.yml failed at verify-gate before the build-pack job ran. Reusable pattern: always pass the full merge SHA to `git tag` rather than relying on HEAD.

### Process Improvements

- **`<GenerateDocumentationFile>true</GenerateDocumentationFile>` is the modern .NET SDK replacement for hardcoded `<DocumentationFile>RelativePath.xml</DocumentationFile>`.** The legacy pattern wrote XML doc files to the project root (next to the .csproj), polluting source dirs and leading to at least one accidental commit (LiteDB.xml had been tracked since the multi-target work). 10 of 11 packable csprojs had this misconfig; only Transport.SqlServer had `bin\Release\<tfm>\` hardcoded (correct but verbose). PR #140 normalized all 11 to `<GenerateDocumentationFile>true</GenerateDocumentationFile>` — the SDK auto-emits to `bin/<Config>/<TFM>/<AssemblyName>.xml` and `dotnet pack` picks it up identically. Net effect on packaged DLLs unchanged. Pattern to enforce going forward: never hardcode the XML doc path in a packable csproj.
- **Wiki feature-on-multiple-transports follows the `UserDequeueColumns` precedent.** One wiki page, sidebar entries under EACH applicable transport. Avoids duplication while preserving discoverability from each transport's nav path. Used for outbox: one `OutboxPattern.md`, two sidebar entries (SQL Server + PostgreSQL). Cross-link sentences in each transport page sit next to the existing MessageHistory pointer.

---
