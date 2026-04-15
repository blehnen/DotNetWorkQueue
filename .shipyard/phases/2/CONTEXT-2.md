# Phase 2 Context: Design Decisions

Captured during `/shipyard:plan 2` discussion. Phase 2 is the **NuGet 0.4.0 release** for the sibling `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` repository.

## Repository

**Target for Phase 2:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` (sibling repo).

Phase 1 is complete on branch `phase-1-lock-fix` with 12 commits ahead of master: `_lockSocket = 0`, 9/9 tests green, Debug + Release builds clean on net8.0 + net10.0.

## Phase 1 Artifacts to Consume in Phase 2

- `DOCUMENTATION-1.md` contains a ready-to-commit `CHANGELOG.md [0.4.0]` entry drafted during the Phase 1 documenter pass. Phase 2's release commit should drop it into the sibling's `CHANGELOG.md` verbatim, just above the existing `### 0.3.0 2026-04-10` heading.
- ISSUE-028 (deferred from Phase 1): adding `<remarks>` XML doc to `Start()` on both `ITaskSchedulerJobCountSync.cs` and `TaskSchedulerJobCountSync.cs`. Phase 1 deferred this to preserve the byte-identical interface-file invariant; Phase 2's release commit is the natural place to land the doc update alongside the CHANGELOG + version bump, since this is exactly the observable behavior change being released.

## Locked Decisions

### 1. Merge strategy: merge `phase-1-lock-fix` → master as Phase 2 Task 0

**Decision:** The first plan task of Phase 2 merges the feature branch into master in the sibling repo. Subsequent release steps (version bump, CHANGELOG, pack, push) all build from master.

**Why:** NuGet packages should be built from master, not a feature branch. Merging explicitly as a plan task gives an auditable handoff and keeps the merge in the release phase's commit log.

**How to apply:** Use `git merge --no-ff phase-1-lock-fix -m "Merge phase-1-lock-fix for 0.4.0 release"` to preserve the feature-branch topology, OR use `--ff-only` for a linear history — architect's call based on the sibling repo's existing branching convention. Tests must be re-run on master after the merge to confirm no merge-time regression.

### 2. NuGet push: GitHub Actions tag-triggered (REVISED)

**Decision:** Publishing to nuget.org is handled by the existing `.github/workflows/ci.yml` publish job, which fires on `v*` tag push. The user triggers it by pushing the `v0.4.0` tag to origin. `NUGET_API_KEY` is a GitHub repo secret, not a local env var.

**Why:** Discovered during Phase 2 research. 0.3.0 shipped via this exact flow. Mirroring the prior release is simpler and safer than rolling a new local-push mechanism. The API key never leaves GitHub secrets.

**How to apply:** Phase 2's publish task is a **tag-push**, not a `dotnet nuget push`. The sequence is:
1. Builder lands the release commit on master (version bump, CHANGELOG, XML doc updates).
2. Builder pushes master to origin.
3. Builder (or user) creates the annotated tag `v0.4.0` matching the 0.3.0 convention.
4. User runs `git push origin v0.4.0` (may require their credentials).
5. User watches the GitHub Actions workflow run complete green.
6. User runs the manual nuget.org verification checklist (decision #3).

**Prior decision superseded:** The original CONTEXT-2 #2 said "user runs `dotnet nuget push` locally." That is no longer correct. Builder should use this revised path.

### 4. Verify 0.3.0 symbols are green before trusting CI symbol-push

**Decision:** Before the `v0.4.0` tag push, confirm that the 0.3.0 package on nuget.org shows a green Symbols badge. If it's red, the existing CI workflow's separate `.snupkg` push step may be broken (CLAUDE.md lesson: nuget.org rejects separate `.snupkg` pushes after the `.nupkg` is already published).

**Why:** The 0.3.0 CI workflow pushes `.nupkg` and `.snupkg` in separate `dotnet nuget push` steps. If 0.3.0 actually validated green despite this, it means either (a) NuGet.org's behavior is more lenient than the DNQ lesson implies, (b) the two pushes were close enough in time to avoid the rejection window, or (c) the symbols just quietly failed and nobody noticed.

**How to apply:** Pre-Task 0, the plan includes a check: load `https://www.nuget.org/packages/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/0.3.0` and confirm:
- Package validation: green
- Source Link: green
- Deterministic Build: green
- Symbols: **specifically check this one**

If Symbols is red for 0.3.0, the plan pivots to an inline fix: edit `.github/workflows/ci.yml` to use the combined `dotnet nuget push "deploy/*.nupkg"` form (which auto-picks `.snupkg` from the same directory) and commit that fix as part of the release commit. If Symbols is green, proceed unchanged.

This is a gate BEFORE Task 0 (pre-flight), not a recovery after the fact — once 0.4.0 is pushed with a broken workflow, we can't re-push 0.4.0.

### 3. nuget.org verification: manual checklist

**Decision:** Phase 2's verification task is a human-readable checklist the user runs after pushing: load the package page on nuget.org, confirm 0.4.0 is listed, check green Source Link / Symbols / Deterministic Build badges, and run `dotnet add package DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler --version 0.4.0` from a throwaway console project.

**Why:** NuGet indexing takes 5–15 minutes and is flaky enough that automated polling loops become a mess of retries + timeouts. A human checklist is the right tool for this step; nothing downstream depends on the exact moment of visibility.

**How to apply:** The plan produces a markdown checklist in the verification block. The user checks each item off manually. The builder waits for user confirmation before marking Phase 2 complete.

## Out-of-Scope for Phase 2

- Changes to `ITaskSchedulerBus` or other internal APIs.
- Adding new features or fixes on top of Phase 1's work.
- Updating the DNQ repo (that's Phase 3+4 — the integration test project lives there).
- Marketing / announcement / release notes beyond the CHANGELOG entry.
- Backporting to 0.3.x (no 0.3.1 release).

## Release-Hard Constraints

- **NuGet versions are one-way.** Once 0.4.0 is pushed, it cannot be re-pushed or downgraded. Local pre-flight validation is critical before the push step.
- **`-p:CI=true` is mandatory** for deterministic Source Link paths. Without it, nuget.org shows red validation indicators.
- **`dotnet nuget push "deploy/*.nupkg"` form** is mandatory so `.snupkg` gets picked up automatically (CLAUDE.md lesson from prior NuGet incident).
- **Clean tree before pack.** `rm -rf Source/*/{obj,bin}` and a fresh Release build before `dotnet pack` to avoid shipping cached artifacts.
- **Version bump visible in exactly one place:** `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj` `<Version>` property. Verify there are no other version strings (e.g., hardcoded constants in source) that would drift.

## Verification Approach for Phase 2

- After merge (Task 0): full test suite on master, Release build clean.
- After version + CHANGELOG + XML doc commit: Release build clean, `dotnet pack` produces a 0.4.0 `.nupkg` in `deploy/`.
- After user pushes: manual checklist described above.
- After verification: tag `v0.4.0` on master, push the tag to origin.
- Phase 2 is NOT done until the NuGet package is visible on nuget.org with green validation indicators AND the tag is pushed.
