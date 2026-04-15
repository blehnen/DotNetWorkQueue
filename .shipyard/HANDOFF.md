## Current Task

DNQ Open-Issue Cleanup milestone (working name: 0.9.32). Brainstorm is complete; PROJECT.md and ROADMAP.md are committed on branch `cleanup-all-open-issues`. The next session should run `/shipyard:plan 1` to decompose Phase 1 into plan files under `.shipyard/phases/1/plans/`.

**State file:** `.shipyard/STATE.json` → `phase=1, status=ready, position="Project definition captured, ready for planning"`

**Branch:** `cleanup-all-open-issues` (in the main tree at `/mnt/f/git/dotnetworkqueue`, no worktree this time). Head commit: most recent shipyard commit is the project-definition capture. Base: `a222cae0` (the prior milestone's post-ship cleanup commit on master).

**Remote status:** Branch `cleanup-all-open-issues` is NOT pushed yet. Push happens with Phase 1's draft PR per the locked feature-branch-first workflow.

## Approach

**Scope locked during `/shipyard:brainstorm`:**

- **24 DNQ-local issues** (ISSUE-001 through ISSUE-024 — none have a `Repo:` tag, all default to DNQ).
- **6 sibling-repo issues out of scope** (ISSUE-025 through ISSUE-030 — all tagged `Repo: DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`). Tracked for a future `TaskScheduler 0.5.0` milestone. Do not touch in this milestone.
- **2 phases, severity-first grouping:**
  - **Phase 1** = 8 Important issues + release commit, ships DNQ NuGet `0.9.32` via manual `dotnet nuget push` from local deploy directory (DNQ's current release pattern — tag-triggered GH Actions publishing is NOT wired up for DNQ today and is explicitly deferred to a future milestone; do NOT confuse with the sibling TaskScheduler's tag-triggered publish workflow). Risk: MEDIUM.
  - **Phase 2** = 16 Suggestion-severity issues in 4 thematic clusters (A: unused using / dead code, B: test assertion strengthening, C: doc/log/clarity polish, D: async disposal / NETFULL artifacts / OpenTelemetry leak / SUMMARY backfill). Risk: LOW. No release. Depends on Phase 1 hard gate (merged PR + `v0.9.32` tag + green nuget.org Symbols/SourceLink).

**Phase 1 hero issue — ISSUE-014:** `DotNetWorkQueue.Transport.RelationalDatabase.Basic.WriteMessageHistoryHandler.RecordComplete` has a `StartedUtc IS NOT NULL AND ...` guard in its second UPDATE (~line 131 of `WriteMessageHistoryHandler.cs`) that silently drops the `DurationMs = 0` write when `StartedUtc` is never persisted. Fix = remove the guard. Researcher must verify the WHERE clause is the only fix site by tracing `RecordStart → RecordComplete` across SqlServer, PostgreSQL, and SQLite transports during `/shipyard:plan 1`. Regression test must capture actual `IDbCommand.CommandText` (not just parameters) per the silent-no-op-UPDATE lesson in CLAUDE.md.

**Phase 1 release mechanics (DNQ manual publish — NOT the sibling TaskScheduler tag-triggered flow):**
1. Each of the 8 Important issues lands as its own atomic commit on `cleanup-all-open-issues`.
2. Final Phase 1 commit is the release commit: bump `<Version>` in `Source/Directory.Build.props` from `0.9.31` to `0.9.32`, write a `## 0.9.32 (date)` section in `CHANGELOG.md` with ISSUE-* references, update `README.md` if it carries a "current version" mention.
3. Push branch + open a **draft PR** (not a ready PR — Jenkins is PR-triggered per the CLAUDE.md lesson, so the draft PR is what triggers CI).
4. Wait for both Jenkins (14 parallel stages) and GH Actions (`build-and-test`) to go green.
5. Convert PR to ready, merge to master.
6. **Local manual publish from master (user drives this — not automated):**
   - Local clean build: `dotnet build Source/DotNetWorkQueueNoTests.sln -c Release -p:CI=true`
   - Pack `.nupkg` + `.snupkg` into `deploy/` (exact build/pack commands are user-known from prior DNQ releases — architect should ask the user or read prior CHANGELOG references during `/shipyard:plan 1`)
   - Inspect a `.nupkg` to verify deterministic Source Link paths before publishing
   - Publish via `dotnet nuget push "deploy/*.nupkg" --api-key <KEY> --source https://api.nuget.org/v3/index.json` — the CLI auto-picks the matching `.snupkg` from the same directory. **Co-publish is mandatory — the `.snupkg` cannot be pushed separately after the `.nupkg` is live per the CLAUDE.md lesson.**
7. Tag `v0.9.32` locally, `git push origin v0.9.32` for release traceability. **DNQ has no tag-triggered publish workflow** — the tag is a label, not an automation trigger.
8. Verify on nuget.org: green Symbols badge, deterministic Source Link badge, `0.9.32` visible. Only then is Phase 1 done.

**Deferred follow-up (NOT in this milestone):** Add a tag-triggered GH Actions publish workflow for DNQ mirroring the sibling TaskScheduler pattern so future releases don't need manual push. Logged as a future-milestone enhancement; user preference is to ship 0.9.32 with the known-working manual flow first.

**Hard constraints (from PROJECT.md + CLAUDE.md lessons):**
- No architectural refactors, no new features, no API changes.
- No sibling repo work.
- Per-issue atomic commits; commit messages reference `ISSUE-NNN`.
- `-p:CI=true` required on the Release build for deterministic Source Link.
- Pre-release: local clean `dotnet pack -c Release -p:CI=true` + `.nupkg` inspection before tagging (mirrors TaskScheduler Phase 2 pre-flight pattern).
- Version-ordering: `0.9.31 < 0.9.32`. No going back to `0.9.4`.

## Tried

**This session's journey from "30 open issues" → "24 in scope":**

1. **First scoping attempt** (during `/shipyard:brainstorm`): thought there were 4 sibling issues (025, 026, 028, 030) and 26 DNQ-local. Locked severity-first grouping and `0.9.32` release shape based on this.
2. **Architect flagged "several issues are already Resolved in ISSUES.md"** on its first ROADMAP generation attempt. I verified via `awk` and found this was FALSE — all 30 issues are genuinely in the `## Open` section. Zero false positives from staleness.
3. **Architect correctly flagged ISSUE-027 and ISSUE-029 as sibling-repo.** I verified each issue body with `awk` Repo-tag extraction and confirmed 6 sibling issues exist (025, 026, 027, 028, 029, 030), not the 4 I originally claimed. ISSUE-027 (Medium) and ISSUE-029 (Low) are both tagged `Repo: DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`.
4. **PROJECT.md surgically patched** to reflect 24 DNQ-local / 6 sibling split, with Phase 2 dropping from 18 issues to 16 (pure Suggestion-severity cluster, no Medium, no Low).
5. **ROADMAP.md regenerated from scratch** via the architect (rather than surgical-patched — too many false-alarm paragraphs in the first draft to clean up individually). Second draft is clean: no "researcher must reconcile resolved status" language, ISSUE-027/029 listed only in the explicit out-of-scope section, Phase 2's 16 issues split into 4 clusters (A/B/C/D).
6. **Presented Phase 2 cluster shape to user for approval**, got "Looks good."
7. **Committed PROJECT.md + ROADMAP.md** on branch `cleanup-all-open-issues`. Updated STATE.json to `phase=1, status=ready`.

**No code changes attempted yet.** Brainstorm-and-roadmap only.

## Remaining

Ordered by priority for the next session:

1. **Run `/shipyard:plan 1`** to decompose Phase 1 into plan files under `.shipyard/phases/1/plans/`. Expect 3-4 plan files (architect decides grouping). Each plan has ≤3 tasks with `<action>`, `<verify>`, `<done>` blocks.
2. **Plan 1 research must:**
   - Trace `RecordStart → RecordComplete` across `Source/DotNetWorkQueue.Transport.SqlServer`, `Source/DotNetWorkQueue.Transport.PostgreSQL`, `Source/DotNetWorkQueue.Transport.SQLite` to confirm `WriteMessageHistoryHandler.cs:~131` is the only fix site for ISSUE-014.
   - Verify the `ValidateQueueName` regex files for ISSUE-002 (locations: `SQLConnectionInformation.cs` in each relational transport project).
   - Verify the Redis orphan-path file for ISSUE-016 (`Source/DotNetWorkQueue.Transport.Redis/Basic/PurgeMessageHistoryHandler.cs`).
   - Identify the exact file + line for each of the 5 supporting issues (001, 017, 019, 020, 022).
3. **Run `/shipyard:build 1`** after Plan 1 is verified READY.
4. **Push branch + open draft PR** once Phase 1 build is green locally.
5. **Tag `v0.9.32`** after the PR merges + CI goes green.
6. **Verify on nuget.org.**
7. **Run `/shipyard:plan 2`** after Phase 1 ships. Phase 2 architect splits the 16 Suggestion issues into waves by cluster (A/B/C/D as already documented in ROADMAP.md).
8. **Run `/shipyard:build 2`**, PR + merge, then `/shipyard:ship` to close the milestone.

## Open Questions

1. **ISSUE-019 content:** the issue is "Missing SUMMARY-1.1.md artifact for Plan 1.1 (LiteDb history tests)." This is a shipyard archive doc backfill, not a code change. It's classified as Important severity. Verify with the user whether this actually belongs in the `0.9.32` NuGet release PR (since it has no code impact and won't affect package contents) or whether it should move to Phase 2 as a Suggestion-level doc backfill.
2. **ISSUE-014 fix site verification:** the architect baked a "researcher traces RecordStart → RecordComplete in all 3 transports" task into Plan 1. If research finds the WHERE clause also exists in transport-specific handlers (not just the shared `Transport.RelationalDatabase` base class), Plan 1's scope expands from 1 file to 4 files. Not currently flagged as a risk but worth watching at plan time.
3. **CHANGELOG.md format:** verify the exact heading format used in past DNQ CHANGELOGs during Phase 1 Plan research. Prior releases may use `## [0.9.32] - 2026-XX-XX` vs `## 0.9.32 (2026-XX-XX)` vs some other shape.
4. **Version-bump file location:** PROJECT.md assumes `Source/Directory.Build.props` holds the `<Version>`. Research should confirm this — it may be in `Directory.Build.props` at the Source root, or in a per-project csproj, or in a separate `version.props`.
5. **Pre-existing stash to drop:** there is one prior-session stash (`stash@{0}: On master: pre-pull shipyard duplicates`) that is NOT from this session. Not relevant to the cleanup milestone but worth noting — it's been sitting there across multiple sessions and may eventually need cleanup.
