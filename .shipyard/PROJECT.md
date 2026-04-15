# Project: DNQ Open-Issue Cleanup Milestone (0.9.32)

**Captured:** 2026-04-15
**Branch:** `cleanup-all-open-issues` (off master @ `a222cae0`)
**Shipping target:** DNQ NuGet `0.9.32` (Phase 1) + polish PR (Phase 2)

## Description

Resolve 24 DNQ-local issues accumulated across prior milestones in `.shipyard/ISSUES.md`. The hero is a real correctness bug in `DotNetWorkQueue.Transport.RelationalDatabase.RecordComplete` (ISSUE-014) that silently mis-writes message-history completion metadata when `DurationMs = 0` and `StartedUtc IS NULL`. That alone justifies a point release. Alongside that, ship two performance wins — compile the `ValidateQueueName` regex (ISSUE-002) and eliminate a redundant Redis round-trip in `PurgeMessageHistoryHandler`'s orphan path (ISSUE-016) — as part of a tag-triggered DNQ NuGet `0.9.32` release.

After the release PR merges, a follow-up polish PR lands the remaining 16 Suggestion-level issues covering unused usings, stale XML doc, shipyard artifact backfills, dead local functions, empty NETFULL shell files, and an `OpenTelemetry.TracerProvider` leak in a shared test fixture.

The milestone is scoped to DNQ-local code only. 6 issues in the sibling `TaskScheduler` repo (025, 026, 027, 028, 029, 030) are explicitly out of scope and tracked separately. (ISSUE-027 test helper DRY and ISSUE-029 GH Actions Node.js 20 deprecation were initially thought DNQ-local but audit confirmed both are tagged `Repo: DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`.)

## Goals

1. Ship DNQ NuGet `0.9.32` to nuget.org containing the 8 Important-severity fixes via the same tag-triggered GH Actions flow that shipped `TaskScheduler 0.4.0`.
2. Burn down the remaining 16 Suggestion-level issues in one follow-up PR without requiring another release.
3. Preserve full test-suite green state throughout: 896/896 core unit tests, 57/57 Memory integration tests, plus all relational transport integration suites that Jenkins has live services for.
4. Confirm Jenkins full 14-stage parallel matrix + GH Actions `build-and-test` job stay green on every phase PR before merge.
5. Close 24 DNQ-local entries from `.shipyard/ISSUES.md` (Open → Resolved), leaving only the 6 sibling-repo entries and any new discoveries surfaced mid-milestone.

## Non-Goals

1. **Sibling `TaskScheduler` repo issues** — all 6 carry a `Repo: DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` tag in `ISSUES.md`:
   - **ISSUE-025** RunPoller start race on fast `Start() → Dispose()` cycles
   - **ISSUE-026** `NetMqQueueApiProbeTests.cs` design-time scaffolding to delete
   - **ISSUE-027** Test helper DRY (`XunitLogger` / `NextPort` / `BeaconInterface` copied across 4 test files)
   - **ISSUE-028** `Start()` `<remarks>` XML doc (apparently closed in Phase 2 release commit but not removed from Open section)
   - **ISSUE-029** GH Actions deprecated Node.js 20 actions in the sibling's `ci.yml`
   - **ISSUE-030** Sibling README uses wrong named arg `udpBroadcastPort:` instead of `broadCastPort:`

   Out of scope; tracked for a separate sibling-repo PR whenever there's a reason to ship `TaskScheduler 0.5.0`.
2. **Architectural refactors** — no API changes, no namespace moves, no new abstractions. Fix / perf / cleanup in place only.
3. **New features or API surface** — no new public types, no new methods, no new extension points. Behavioral corrections and test-side improvements only.
4. **Pre-existing `SYSLIB0012` warnings** in `LiteDB.IntegrationTests/ConnectionString.cs` and `SQLite.Integration.Tests/ConnectionString.cs` — flagged previously as a cleanup target, but explicitly NOT in the 30 tracked issues. Let them linger one more cycle.
5. **Major version bump** — `0.9.31 → 0.9.32` patch release only, not `0.10.0`.
6. **Phase 1 feature additions** — the release PR is strictly correctness + perf + test fixes. No scope creep into "while I'm here, let me add X."

## Requirements (Functional, Grouped by Phase)

### Phase 1 — 0.9.32 Release (8 Important issues + release commit)

One wave, one PR, one release. Each issue lands as its own atomic commit on branch `cleanup-all-open-issues`. Final commit in Phase 1 is the release commit.

**Correctness / Release-critical trio**
- **ISSUE-014** — Fix `RelationalDatabase.RecordComplete` WHERE clause so `DurationMs = 0` rows write correctly when `StartedUtc IS NULL`. Add a regression test that reproduces pre-fix failure and passes post-fix. Verify fix applies across SqlServer, PostgreSQL, SQLite transports.
- **ISSUE-002** — Compile the `ValidateQueueName` regex across all relational transports (use `RegexOptions.Compiled` or `[GeneratedRegex]`). Verify no existing test asserts the regex is non-compiled.
- **ISSUE-016** — Eliminate redundant Redis round-trip in orphan path of `PurgeMessageHistoryHandler`. Confirm via existing test that behavior is unchanged; add assertion if the test currently doesn't catch the round-trip.

**Test reliability**
- **ISSUE-017** — Add `CompletedUtc` non-read assertion in the orphan test so it's not fragile.
- **ISSUE-020** — Fix `LiteDbHistoryEnabledTests.CleanupAsync` double-dispose of `_scope` after `_creation.Dispose()`.
- **ISSUE-022** — Fix the no-op `dynamic=true` test case in PostgreSQL `JobSchedulerTests` so it actually asserts the dynamic code path.

**Housekeeping**
- **ISSUE-001** — Remove unused `fixture` variable in `QueueCreatorTests` post Plan 1.1 refactor.
- **ISSUE-019** — Backfill the missing `SUMMARY-1.1.md` shipyard artifact for the LiteDb history tests phase (archive-only, no code impact).

**Release commit (final commit of Phase 1)**
- Bump `<Version>` in `Source/Directory.Build.props` from `0.9.31` to `0.9.32`.
- Add `## 0.9.32 (2026-04-XX)` section to `CHANGELOG.md` summarizing the 8 issues with ISSUE-* references.
- Update `README.md` if it carries a "current version" mention.
- Tag `v0.9.32` after the PR merges to master. GH Actions tag-triggered publish workflow handles the actual NuGet push.

### Phase 2 — Polish & Cleanup (16 issues)

One PR against updated master. No release. Architect splits into waves by file cluster at plan time.

- **Suggestion (16):** ISSUE-003 through 013 (unused usings, stale XML doc, `DisposeAsync` patterns, log message text, parens clarity, missing SUMMARYs), 015 (dead local function in `RecordComplete_WithoutStartedUtc_PassesDurationZero` test), 018 (missing test for Enqueued status in `PurgeMessageHistoryHandler`), 021 (empty NETFULL shell files), 023 (blank line artifacts from NETFULL removal), 024 (`OpenTelemetry.TracerProvider` leak in `SharedSetup.CreateTrace`).

## Non-Functional Requirements

- **Test green:** no regression in 896/896 core unit tests + 57/57 Memory integration tests on every PR.
- **CI green:** Jenkins 14-stage parallel matrix + GH Actions `build-and-test` job green on every PR before merge.
- **Release integrity:** `0.9.32` ships with deterministic Source Link paths (`-p:CI=true`), Symbols visible on nuget.org, version ordering preserved (`0.9.31 < 0.9.32`).
- **Per-issue atomic commits:** each ISSUE-NNN resolution is a single commit so git blame / bisect can attribute individual fixes. Commit messages reference the ISSUE-NNN ID.
- **No scope creep:** if the planner / researcher discovers related issues mid-plan, they go into `.shipyard/ISSUES.md` as new entries, NOT into the current milestone's plan.

## Success Criteria

1. **Phase 1:** DNQ NuGet `0.9.32` live on nuget.org with green Symbols + deterministic Source Link badges. Tag `v0.9.32` on master. CHANGELOG contains the 8-issue release notes. All 8 Important ISSUE-* refs moved from Open to Resolved in `.shipyard/ISSUES.md`.
2. **Phase 1:** Jenkins + GH Actions green on the PR before merge; 896/896 core unit tests + 57/57 Memory integration tests + all Jenkins relational transport stages green.
3. **Phase 2:** 16 remaining DNQ-local issues resolved and moved from Open to Resolved. Jenkins + GH Actions green on the PR. No release. No regressions.
4. **Milestone:** `.shipyard/ISSUES.md` Open section contains only the 6 sibling-repo entries (025, 026, 027, 028, 029, 030) plus any entries opened mid-milestone from new discoveries. Everything DNQ-local that was open at milestone start is closed.

## Constraints

**Technical**
- net10.0 / net8.0 multi-target preserved for library projects; integration test projects remain `net10.0`-only (CLAUDE.md convention).
- Central Package Management (CPM) pattern preserved — no direct `Version=` attributes on `PackageReference`.
- `-p:CI=true` required on the Release build so Source Link paths are deterministic.
- Push `.nupkg` + `.snupkg` together from the deploy directory — `.snupkg` can't be pushed separately after the `.nupkg` is live.
- Phase 1 tag format: `v0.9.32` (annotated, unsigned, matching prior release convention).
- API key stays in GH Secrets. No local `dotnet nuget push`.

**Workflow**
- Jenkins is PR-triggered, not branch-triggered. Each phase MUST open a draft PR to trigger CI. Merge only after both CI surfaces go green.
- Per-issue atomic commits; per-phase single PR; per-milestone two PRs total (release PR + polish PR).
- Pre-release local pack + `.nupkg` inspection pre-tag to catch Source Link / Symbols red badges before they hit nuget.org.

**Scope**
- Sibling `TaskScheduler` repo is out of scope. Any fix that requires touching `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/` goes into a separate Shipyard instance or `/shipyard:quick` in that repo.
- No architectural refactors. Fix in place.

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| ISSUE-014 fix requires a schema assumption we can't hold. | Architect verifies during `/shipyard:plan 1` Research: trace `RecordStart → RecordComplete` in all 3 relational transports, confirm the WHERE clause is the right fix site. |
| `0.9.32` release fails verification on nuget.org (Symbols red, Source Link red). | Mirror the TaskScheduler Phase 2 pre-flight pattern: local clean pack + `.nupkg` inspection before tagging. |
| Phase 2's 16-issue PR too large to review. | Architect splits Phase 2 into waves by file cluster (unused-usings sweep, shipyard artifact backfills, OpenTelemetry leak fix). |
| Version-ordering goof (e.g., tagging `v0.9.4`). | CLAUDE.md lesson captured; release plan task explicitly asserts the tag is `v0.9.32`. |
| Integration tests for SqlServer/PostgreSQL/Redis/LiteDb/SQLite need external services the dev machine doesn't run. | Phase 1 local verification runs only in-memory suites (core unit + Memory integration). Jenkins handles external-service suites as the hard gate — matches the Phase 3 TaskScheduler milestone pattern. |

## Related Milestones

- **Prior milestone:** `TaskScheduler 0.4.0 + DNQ Integration Tests + CI Wiring` shipped via PR #115 as `190f1226` + ship commit `ddc8daf0` + cleanup commit `a222cae0`. 4 phases, 2 repos, resolved issues 028 and several others.
- **Next milestone (tentative):** sibling `TaskScheduler 0.5.0` if/when the 4 sibling-repo issues justify another release.
