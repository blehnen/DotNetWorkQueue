# ROADMAP — DNQ Open-Issue Cleanup Milestone (0.9.32)

**Captured:** 2026-04-15
**Branch:** `cleanup-all-open-issues` (off master @ `a222cae0`)
**Source of truth:** `.shipyard/PROJECT.md`
**Scope:** 24 DNQ-local issues (ISSUE-001..024). 6 sibling-repo issues (025, 026, 027, 028, 029, 030) are tagged `Repo: DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` and are **out of scope** for this milestone.

## Phase Summary

| Phase | Repo | Name | Risk | Depends On | Status |
|-------|------|------|------|------------|--------|
| 1 | DotNetWorkQueue | 0.9.32 Release (8 Important issues + release) | Medium | -- | complete |
| 2 | DotNetWorkQueue | Polish & Cleanup (16 Suggestion issues) | Low | 1 (hard gate: `v0.9.32` tag pushed + nuget.org Symbols/SourceLink green) | complete |

**Milestone totals:** Phase 1 = 8 issues + release commit. Phase 2 = 16 issues. Total DNQ-local = 24. Sibling out of scope = 6.

---

## Phase 1 — 0.9.32 Release (Risk: Medium)

**Risk rationale:** This phase ships a NuGet package. ISSUE-014 is a real correctness bug in a WHERE clause across three relational transports; getting the fix site wrong or missing one transport is a user-visible regression. Mitigated by per-transport verification during `/shipyard:plan 1` research.

### Objective

Ship DNQ NuGet `0.9.32` via DNQ's current manual publishing flow: `dotnet nuget push "deploy/*.nupkg" --api-key <KEY> --source https://api.nuget.org/v3/index.json` from the local deploy directory. One PR against master on branch `cleanup-all-open-issues`. Per-issue atomic commits. Final commit is the release commit. After the PR merges, local clean build Release with `-p:CI=true`, pack to `deploy/`, manual push, then tag `v0.9.32` for release traceability. **DNQ does not currently have a tag-triggered GH Actions publish workflow** (that's a nice-to-have deferred to a future milestone); the sibling `TaskScheduler` repo is the one with tag-triggered publish, not this one.

### Scope (8 Important issues + release commit)

**Correctness / Release-critical trio**

1. **ISSUE-014** — `RelationalDatabase.RecordComplete` WHERE clause blocks `DurationMs = 0` write when `StartedUtc IS NULL`.
   - Fix site: `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs` (RecordComplete path).
   - Regression test: extend `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/WriteMessageHistoryHandlerTests.cs` (`RecordComplete_WithoutStartedUtc_PassesDurationZero`) so it asserts the captured `CommandText` writes the row, not just parameter values. This follows the CLAUDE.md lesson: "SQL UPDATE tests that only assert parameter values can pass while the UPDATE is a silent no-op... Capture and assert the actual `CommandText` to catch this."
   - Cross-transport verification: confirm the same WHERE clause exists and is corrected across SqlServer, PostgreSQL, SQLite transports. Researcher traces `RecordStart → RecordComplete` in all three to confirm fix site.

2. **ISSUE-002** — Compile `ValidateQueueName` regex.
   - Apply `RegexOptions.Compiled` (or `[GeneratedRegex]` source generator) to the regex instance in each relational transport's validator. Search pattern: `ValidateQueueName` across `Source/DotNetWorkQueue.Transport.*`.
   - Confirm no existing test asserts the regex is non-compiled.

3. **ISSUE-016** — Eliminate redundant Redis round-trip in `PurgeMessageHistoryHandler` orphan path.
   - Fix site: `Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs` (`Purge` method, loop body).
   - Existing test `Purge_Handles_Missing_Hash_Gracefully` in `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/PurgeMessageHistoryHandlerTests.cs` should still pass. ISSUE-017 hardens it.

**Test reliability**

4. **ISSUE-017** — Add `db.DidNotReceive().HashGet(..., "CompletedUtc", ...)` assertion to the orphan test in `PurgeMessageHistoryHandlerTests.cs` after ISSUE-016's fix lands. Without this, the orphan path is vulnerable to silent NSubstitute default-return collisions (see CLAUDE.md lesson on `RedisValue.Null` cast to int yielding 0).

5. **ISSUE-020** — Fix `LiteDbHistoryEnabledTests.CleanupAsync` double-dispose of `_scope` after `_creation.Dispose()`. Single-line fix in the cleanup block.

6. **ISSUE-022** — Fix no-op `dynamic=true` case in PostgreSQL `JobSchedulerTests` so it actually exercises the dynamic code path (currently the assertion doesn't distinguish static vs dynamic).

**Housekeeping**

7. **ISSUE-001** — Remove unused `fixture` variable in `QueueCreatorTests` left over from Plan 1.1 refactor.

8. **ISSUE-019** — Backfill `SUMMARY-1.1.md` for the LiteDb history tests phase under `.shipyard/phases/`. Archive-only; no code impact.

**Release commit (final commit of Phase 1)**

- Bump `<Version>` in `Source/Directory.Build.props` from `0.9.31` → `0.9.32`.
- Add `## 0.9.32 (2026-04-XX)` section to `CHANGELOG.md` summarizing the 8 issues with ISSUE-* references.
- Update any `README.md` "current version" mention.
- After PR merges to master: local clean build Release with `-p:CI=true`, pack `.nupkg` + `.snupkg` to `deploy/`, inspect the nuspec for deterministic Source Link, then manually push via `dotnet nuget push "deploy/*.nupkg" --api-key <KEY> --source https://api.nuget.org/v3/index.json` (CLI auto-picks matching `.snupkg`). Tag `v0.9.32` (annotated, unsigned, matching prior release convention) after the manual push lands. This is DNQ's current release pattern — **no tag-triggered GH Actions workflow exists for DNQ today**; that automation is deferred to a future milestone.

### Success Criteria

- All 8 ISSUE-* entries moved from `## Open` to `## Resolved` in `.shipyard/ISSUES.md`.
- 896/896 core unit tests green; 57/57 Memory integration tests green.
- Jenkins 14-stage parallel matrix green on the PR. GH Actions `build-and-test` job green on the PR.
- Pre-tag local `dotnet pack -c Release -p:CI=true` with `.nupkg` inspection confirms deterministic Source Link paths before pushing the tag (mirrors TaskScheduler 0.4.0 pre-flight).
- `v0.9.32` tag pushed. nuget.org shows green Symbols badge + green Source Link badge for `DotNetWorkQueue 0.9.32` within 30 minutes of tag push.
- `CHANGELOG.md` contains the 0.9.32 section with all 8 ISSUE-* refs.
- Version ordering preserved: `0.9.31 < 0.9.32` (CLAUDE.md lesson).

### Hard Constraints

- `-p:CI=true` on every Release build (deterministic Source Link — CLAUDE.md lesson).
- Push from deploy directory so `.snupkg` co-publishes (CLAUDE.md lesson: `.snupkg` cannot be pushed separately after `.nupkg` is live).
- Per-issue atomic commits; commit messages reference `ISSUE-NNN`.
- API key stays in GH Secrets. No local `dotnet nuget push`.
- Draft PR first to trigger Jenkins; merge only after both CI surfaces are green.

---

## Phase 2 — Polish & Cleanup (Risk: Low)

**Risk rationale:** Pure polish. 16 small items, no release, no behavioral changes beyond one new Redis test (`Purge_Skips_Enqueued_Records` for ISSUE-018). No schema, no public API, no transport-specific wire format touched.

**Hard dependency gate:** Phase 1's `v0.9.32` tag must be pushed **and** nuget.org must show green Symbols + Source Link for `DotNetWorkQueue 0.9.32` before Phase 2's PR is opened. If the release verification fails, Phase 2 is blocked until Phase 1 re-ships.

### Objective

Single follow-up PR against master (post-merge, post-tag, post-publish) landing the 16 Suggestion-severity DNQ-local cleanups. No release. No version bump.

### Scope (16 Suggestion issues, grouped into 4 thematic clusters)

**Cluster A — Unused using / dead code sweep (4 issues)**

- **ISSUE-006** — Unused using directives in `RedisConnectionInfoTests`.
- **ISSUE-010** — Unused `using System.Threading;` in `WorkerTerminate.cs`.
- **ISSUE-011** — Unused `using System.Threading;` in `WaitForThreadToFinish.cs`.
- **ISSUE-015** — Dead local function `MakeTrackingParam` in `RecordComplete_WithoutStartedUtc_PassesDurationZero` test.

**Cluster B — Test assertion strengthening (4 issues)**

- **ISSUE-003** — `QueueName_Valid_Alphanumeric` tests do not assert the `QueueName` property value.
- **ISSUE-004** — `QueueName_Valid_Alphanumeric` tests only assert `IsNotNull` on the non-relational transports.
- **ISSUE-008** — `DisposeAsync_Owned_HttpClient_Is_Disposed` uses sync-over-async in the assertion.
- **ISSUE-018** — Add `Purge_Skips_Enqueued_Records` test to `PurgeMessageHistoryHandlerTests` covering the `Enqueued` status path. This is the **only new test** in Phase 2.

**Cluster C — Doc / log / clarity polish (3 issues)**

- **ISSUE-005** — Stale XML doc comment on Memory `ConnectionInformation` class.
- **ISSUE-009** — `PrimaryWorker.Stop()` / `Worker.Stop()` log messages still say "worker thread" (post thread→task migration).
- **ISSUE-013** — `MultiWorkerBase.Running` lacks explicit parentheses for operator precedence clarity.

**Cluster D — Async disposal / NETFULL artifacts / OpenTelemetry leak / SUMMARY backfill (5 issues)**

- **ISSUE-007** — `DisposeAsync` uses synchronous `Timer.Dispose()` instead of `Timer.DisposeAsync()`.
- **ISSUE-012** — Missing `SUMMARY` file for Phase 7 Plan 01 (BaseMonitor). Archive-only.
- **ISSUE-021** — Empty shell files left over from NETFULL removal.
- **ISSUE-023** — Stray blank line / double blank line artifacts from NETFULL removal.
- **ISSUE-024** — `SharedSetup.CreateTrace` leaks the OpenTelemetry `TracerProvider` when `TraceSettings.Enabled` is true.

### Verification

- Build clean: `dotnet build Source/DotNetWorkQueue.sln -c Debug` zero warnings, zero errors.
- 896+ core unit tests green: `dotnet test Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj`.
- 57+ Memory integration tests green: `dotnet test Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj`.
- Jenkins 14-stage parallel integration matrix green on the PR.
- GH Actions `build-and-test` green on the PR.
- No new tests other than ISSUE-018's `Purge_Skips_Enqueued_Records`.

### Success Criteria

- All 16 Suggestion ISSUE-* entries moved from `## Open` to `## Resolved` in `.shipyard/ISSUES.md`.
- PR merged to master.
- `.shipyard/ISSUES.md` Open section contains only the 6 sibling-repo entries (025–030) plus any new discoveries surfaced mid-milestone.
- No regressions in unit, Memory integration, or Jenkins transport suites.

### Hard Constraints

- No version bump. No tag. No NuGet publish.
- Per-issue atomic commits; commit messages reference `ISSUE-NNN`.
- Commits organized by cluster to make review tractable: ~3–5 commits per cluster, 4 clusters → ~16 commits in the PR, reviewable in cluster-sized chunks.
- No scope creep. New issues discovered during Phase 2 go into `.shipyard/ISSUES.md` as new entries, not into the current PR.
- Draft PR first to trigger Jenkins; merge only after both CI surfaces are green.

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| ISSUE-014 fix site wrong for one of SqlServer / PostgreSQL / SQLite (silent no-op UPDATE). | Researcher traces `RecordStart → RecordComplete` in all three transports during `/shipyard:plan 1`. Regression test captures `CommandText` via mock `IDbCommand` and asserts the UPDATE actually runs — not just parameter values. CLAUDE.md lesson on silent-no-op UPDATEs applies verbatim. |
| `0.9.32` release fails nuget.org verification (Symbols red, Source Link red). | Mirror TaskScheduler 0.4.0 pre-flight: local clean `dotnet pack -c Release -p:CI=true`, unzip `.nupkg`, inspect `<RepositoryUrl>` / `<RepositoryCommit>` / `<PublishRepositoryUrl>` in the `.nuspec` and Source Link JSON before tagging. If red locally, do not push the tag. |
| Version-ordering goof (accidentally tagging `v0.9.4` or similar). | Release plan task explicitly asserts the tag string is `v0.9.32` and that `Source/Directory.Build.props` reads `0.9.32` before the commit. CLAUDE.md lesson on NuGet version ordering is cited in the release plan. |
| Phase 2's 16-issue PR too large for reviewer to digest. | Organize commits into 4 thematic clusters (A–D above). Reviewer can review one cluster at a time; each cluster is self-contained and revertable in isolation. |
| External-service integration tests (SqlServer/PostgreSQL/Redis/LiteDb/SQLite) need services the dev machine doesn't run. | Phase 1 local verification runs only in-memory suites (core unit + Memory integration). Jenkins' 14-stage parallel matrix is the hard gate for external-service suites. Same pattern as Phase 3 TaskScheduler milestone. |

---

## Out of Scope (explicit)

The following **6 sibling-repo issues** are tagged `Repo: DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` in `.shipyard/ISSUES.md` and are **not** part of this milestone:

- **ISSUE-025** RunPoller start race on fast `Start() → Dispose()` cycles.
- **ISSUE-026** `NetMqQueueApiProbeTests.cs` design-time scaffolding to delete.
- **ISSUE-027** Test helper DRY (`XunitLogger` / `NextPort` / `BeaconInterface` duplicated across 4 test files).
- **ISSUE-028** `Start()` `<remarks>` XML doc cleanup.
- **ISSUE-029** GH Actions deprecated Node.js 20 actions in the sibling's `ci.yml`.
- **ISSUE-030** Sibling README uses wrong named arg `udpBroadcastPort:` instead of `broadCastPort:`.

These stay in the Open section of `.shipyard/ISSUES.md` and ship whenever a `TaskScheduler 0.5.0` milestone is spun up in the sibling repo.

Also out of scope:

- Architectural refactors, API changes, namespace moves.
- New features, new public types, new extension points.
- Pre-existing `SYSLIB0012` warnings in `LiteDB.IntegrationTests/ConnectionString.cs` and `SQLite.Integration.Tests/ConnectionString.cs`.
- Major version bump (this is `0.9.31 → 0.9.32`, not `0.10.0`).
