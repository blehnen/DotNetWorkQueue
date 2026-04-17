# ROADMAP — DNQ Automated NuGet Publishing via GitHub Actions

**Captured:** 2026-04-16
**Branch:** `master` (feature branch TBD at `/shipyard:plan` time — git strategy is `manual`)
**Source of truth:** `.shipyard/PROJECT.md`
**Scope:** Infrastructure-only milestone. Zero runtime behavior change. Replaces the 24-step manual `deploy/Deploy.bat` flow with a `v*` tag-triggered GitHub Actions workflow, centralizes `<Version>` in `Source/Directory.Build.props`, and retires the on-disk `deploy/` artifacts. The first real release this workflow exercises will be a future milestone (likely `v0.9.33`).

## Phase Summary

| Phase | Name | Risk | Sizing | Depends On | Trigger | Status |
|-------|------|------|--------|------------|---------|--------|
| 1 | Version centralization (`<Version>` → `Directory.Build.props`) | Low | S | -- | Code + PR | **complete** (branch `feature/nuget-publish-ci`, commits `bccb0d33..c42a8e40`) |
| 2 | Publish workflow (`publish.yml` with verify-gate / build-pack / publish) | Medium | M | 1 | Code + PR | **complete** (branch `feature/nuget-publish-ci`, commits `5955ce72..940a9a68`) |
| 3 | Cleanup & docs (retire `deploy/*`, update CLAUDE.md) | Low | S | 2 | Code + PR (may be squashed into Phase 2's PR) | **complete** (branch `feature/nuget-publish-ci`, commits `76cd5504..8f705e00`) |
| 4 | Dry-run validation (`workflow_dispatch` with `dry_run=true` on master) | Medium | S | 1 + 2 + 3 merged to master | Operational, not code | pending |

**Milestone totals:** 3 code phases (Phases 1–3) plus 1 operational phase (Phase 4). All three code phases may land in a single PR if the reviewer prefers, but sequencing below reflects the logical dependency order.

**Side task (blocker for Phase 2):** Discover the exact Jenkins status context name posted on a recent PR's commit statuses. See **Phase 1.5** below — this must complete before Phase 2 plan generation so the bash gate script has the exact string to match.

---

## Phase 1 — Version centralization (Risk: Low)

**Risk rationale:** Self-contained refactor. A `dotnet pack` sanity check immediately reveals drift (package filename carries `<Version>`). Only two ways to go wrong: (a) miss one of the 12 csprojs, (b) typo the new value in `Directory.Build.props`. Both are caught by the `build-pack` gate in Phase 2 (or a local `dotnet pack` dry-run in this phase).

### Objective

Make `Source/Directory.Build.props` the single source of truth for the NuGet `<Version>`. Add `<Version>0.9.33</Version>` (the next planned release number) to the existing `<PropertyGroup>` in `Directory.Build.props`. Remove the `<Version>0.9.32</Version>` line from all 12 packable csprojs (so they inherit from `Directory.Build.props`).

### Scope

**Files touched (13 total):**

- `Source/Directory.Build.props` — add `<Version>0.9.33</Version>` in the existing `<PropertyGroup>`.
- `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`
- `Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj`
- `Source/DotNetWorkQueue.Dashboard.Client/DotNetWorkQueue.Dashboard.Client.csproj`
- `Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj`
- `Source/DotNetWorkQueue.Transport.LiteDB/DotNetWorkQueue.Transport.LiteDb.csproj`
- `Source/DotNetWorkQueue.Transport.Memory/DotNetWorkQueue.Transport.Memory.csproj`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj`
- `Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj`
- `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj`
- `Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj`
- `Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj`

For each csproj: delete only the `<Version>0.9.32</Version>` line. Do not touch `<TargetFrameworks>`, `<PackageId>`, `<Authors>`, or any other metadata (those may stay per-project or migrate in a future milestone — not this one).

### Success Criteria

- `grep -r "<Version>" Source/` returns exactly one hit: `Source/Directory.Build.props` → `<Version>0.9.33</Version>`.
- `dotnet build Source/DotNetWorkQueueNoTests.sln -c Release -p:CI=true` succeeds locally with zero warnings, zero errors.
- Local `dotnet pack Source/DotNetWorkQueueNoTests.sln -c Release -p:CI=true --no-build -o deploy-test/` followed by `dotnet pack Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj -c Release -p:CI=true --no-build -o deploy-test/` produces 12 `.nupkg` and 12 `.snupkg` files, each named `*.0.9.33.nupkg` / `*.0.9.33.snupkg`. (`deploy-test/` is ephemeral — delete after verification. `deploy/` proper is retired in Phase 3.)
- Existing Jenkins 14-stage integration matrix green on the PR. GH Actions `build-and-test` green on the PR.

### Hard Constraints

- No runtime code changes. csproj edits only.
- The new version value is `0.9.33` (the next planned release). Do not use `0.9.32` — that's the version currently on NuGet.org, and a tag push with `Directory.Build.props` = `0.9.32` would fail Phase 2's verify-gate due to `--skip-duplicate` succeeding silently but producing no new publish (a false-positive release).
- Do not bump any transitive package references. `Directory.Packages.props` central management is orthogonal to this change.

### Risks / Watch-outs

- **Typo trap:** Only `<Version>` moves. `<VersionPrefix>`, `<AssemblyVersion>`, `<FileVersion>` (if any project uses them) stay untouched. Researcher/planner should grep for all four variants in the 12 csprojs before editing to avoid deleting the wrong line.
- **Dashboard.Ui is Web-SDK:** `Microsoft.NET.Sdk.Web`, not `Microsoft.NET.Sdk`. `Directory.Build.props` still applies, so the inheritance works — but the local verification must include the explicit `dotnet pack DotNetWorkQueue.Dashboard.Ui.csproj` step since solution-level pack skips it.

---

## Phase 1.5 — Side task: Jenkins status context name discovery (Risk: Low, Sizing: XS)

**Not a full phase — a single one-off operational task, treated as a blocker for Phase 2 plan authoring.**

### Objective

Determine the exact GitHub commit-status context string posted by Jenkins for a PR build. Document the string in the Phase 2 plan so the bash gate script matches it exactly.

### Procedure

Run against any recent merged PR (e.g., PR #116 or any commit on master with a known Jenkins run):

```bash
gh api repos/blehnen/DotNetWorkQueue/commits/<sha>/statuses --jq '.[].context'
```

Expected output contains one line per status. The Jenkins rollup most likely matches one of:
- `continuous-integration/jenkins/branch`
- `continuous-integration/jenkins/pr-merge`
- `continuous-integration/jenkins/pr-head`
- (or a custom name configured in the Jenkinsfile)

### Done

The exact context string is captured as a literal in the Phase 2 plan's bash gate script and recorded as a new CLAUDE.md lesson in Phase 3 ("Jenkins posts status context `<string>` — the B2 gate script matches this exact value").

### Risk

If the Jenkins status doesn't post on master commits (only PR commits), the B2 gate against a tag on master must read the status of the tag's SHA's *parent PR's head SHA* — not the tag commit itself. This is only a concern if master is a merge-commit target; DNQ currently uses squash-merge so the tag SHA *is* the PR's post-merge SHA and inherits the PR's statuses. Confirm squash-merge policy during this side task.

---

## Phase 2 — Publish workflow (Risk: Medium)

**Risk rationale:** Medium, not High, because the workflow has a hard-block dry-run path (Phase 4) before any real tag push. The real risks are: (a) the verify-gate regex too loose/strict, (b) the Jenkins status context string wrong (mitigated by Phase 1.5), (c) the `dotnet pack` step misses Dashboard.Ui (mitigated by the 12+12 count assertion — fails loud if the count is off), (d) secret leakage in logs. None are catastrophic because the dry-run catches them before a NuGet.org push.

### Objective

Author `.github/workflows/publish.yml` implementing the three-job pipeline described in `PROJECT.md`: `verify-gate` → `build-pack` → `publish`. Triggered on `push: tags: [ 'v*' ]` and on `workflow_dispatch` with a `dry_run: boolean` input.

### Scope

**New file:** `.github/workflows/publish.yml` (single file, ~100–150 lines of YAML).

**Job 1 — `verify-gate`** (all three assertions on `ubuntu-latest`):

1. Tag regex: `^v\d+\.\d+\.\d+(-[\w\.]+)?$`. Reject `vtest`, `v0.9`, `v0.9.33.1`.
2. Tag-version match: `grep -oP '(?<=<Version>)[^<]+' Source/Directory.Build.props` equals the tag with `v` stripped.
3. Jenkins B2 gate: `gh api repos/${{ github.repository }}/commits/${{ github.sha }}/statuses --jq '.[] | select(.context=="<EXACT_CONTEXT_FROM_PHASE_1_5>") | .state'` must return `success`. Fail on `failure`, `pending`, or empty.

On `workflow_dispatch` with `dry_run=true`, all three assertions still run (so we validate the gate works on the current master even without a real tag — see Phase 4).

**Job 2 — `build-pack`** (`needs: verify-gate`):

- `actions/setup-dotnet@v4` with `dotnet-version: '10.0.x'` (plus `8.0.x` if a net8.0 build is needed — confirm by grep of `TargetFrameworks`; if only net10.0 is needed at pack time, one SDK is enough).
- `dotnet restore Source/DotNetWorkQueueNoTests.sln`
- `dotnet build Source/DotNetWorkQueueNoTests.sln -c Release -p:CI=true --no-restore`
- `dotnet pack Source/DotNetWorkQueueNoTests.sln -c Release -p:CI=true --no-build -o deploy/`
- `dotnet pack Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj -c Release -p:CI=true --no-build -o deploy/`
- Count assertion: bash snippet that counts `ls deploy/*.nupkg | wc -l` and `ls deploy/*.snupkg | wc -l`, fails with descriptive message if either != 12.
- `actions/upload-artifact@v4` uploads `deploy/*` as `nuget-packages-v<version>` with `retention-days: 90`.

**Job 3 — `publish`** (`needs: build-pack`, `if: ${{ !inputs.dry_run }}` so it's skipped on dry-run):

- `actions/download-artifact@v4` restores `deploy/` from the Phase 2 output.
- `dotnet nuget push "deploy/*.nupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate`
- `dotnet nuget push "deploy/*.snupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate`
- `gh release create ${{ github.ref_name }} deploy/*.nupkg deploy/*.snupkg --title "DNQ ${{ github.ref_name }}" --generate-notes` (uses default `GITHUB_TOKEN` — `contents: write` permission required at workflow or job level).

**Permissions block at workflow level:**

```yaml
permissions:
  contents: write   # for gh release create
  statuses: read    # for the Jenkins B2 gate
```

**Job-level concurrency group** on tag name to prevent a double-run from a re-pushed tag.

### Success Criteria

- `.github/workflows/publish.yml` is syntactically valid (`gh workflow view publish.yml` shows it after merge).
- `act` or GitHub's UI linter shows no errors.
- Dry-run (Phase 4) completes both `verify-gate` and `build-pack` green, `publish` skipped.
- Job summaries log: tag string, version from `Directory.Build.props`, Jenkins status observed, 24 files packed (12+12), artifact URL.
- `NUGET_API_KEY` secret is referenced only in the `publish` job (grep the YAML).

### Hard Constraints

- `ubuntu-latest` only. No Windows matrix. (Per Non-Functional Requirements.)
- `--skip-duplicate` on both push commands for idempotency on re-run.
- No `.github/workflows/ci.yml` edits. CI and publish are separate files.
- No Jenkinsfile edits. The B2 gate reads Jenkins' existing status output.
- Secret reference in `verify-gate` and `build-pack` jobs is forbidden — only `publish` touches `NUGET_API_KEY`.
- `-p:CI=true` on every `dotnet build` and `dotnet pack` invocation (deterministic Source Link — CLAUDE.md lesson).

### Risks / Watch-outs

- **Wrong Jenkins context string:** Phase 1.5 must complete and its discovered string must be a literal in the gate bash, not a regex. If the string changes (Jenkins config update), the gate silently passes nothing and the workflow fails loud — acceptable.
- **Dashboard.Ui Web-SDK not packed by solution:** the explicit `dotnet pack DotNetWorkQueue.Dashboard.Ui.csproj` step is mandatory. Count assertion of 12+12 is the safety net.
- **`gh` CLI availability:** `gh` is preinstalled on `ubuntu-latest` runners, but the `GH_TOKEN` env var must be set to `${{ secrets.GITHUB_TOKEN }}` for `gh api` to auth.
- **`--no-restore` + `--no-build` brittleness:** if Phase 1's pack-locally sanity check worked, this will too. But Docker/cache differences have historically bitten us (CLAUDE.md lesson on `--no-restore` in Docker). If the workflow flakes, drop `--no-build` from the `dotnet pack` call — the cost is a small rebuild, the benefit is robustness.
- **Tag pushed without first merging Phase 1:** a tag on a commit where `Directory.Build.props` lacks `<Version>` will fail the tag-version match gate with a clear message — correct behavior.

---

## Phase 3 — Cleanup & docs (Risk: Low)

**Risk rationale:** Pure doc + file-deletion. Zero build impact. Only risk is leaving a stale instruction in CLAUDE.md that contradicts the new workflow.

### Objective

Retire `deploy/*` tracked files, ensure `.gitignore` covers future `deploy/` output, and update `CLAUDE.md` to reflect the new release flow.

### Scope

**Repository hygiene:**

- `git rm deploy/Deploy.bat`
- `git rm deploy/*.nupkg deploy/*.snupkg` (25 tracked files total: 1 bat + 12 nupkg + 12 snupkg for version 0.9.32)
- Confirm `.gitignore` contains `deploy/` (or add it). Place under an existing "build output" section if one exists; otherwise add a new "# Release artifacts" section with a one-line entry.

**CLAUDE.md edits:**

1. **Correct a lesson:** Replace the existing lesson:
   > "...push from the deploy directory using `dotnet nuget push "deploy/*.nupkg" --api-key KEY --source https://api.nuget.org/v3/index.json` — the CLI automatically picks up matching `.snupkg` files from the same directory."

   With:
   > "The CLI's auto-match of `.snupkg` alongside `.nupkg` is unreliable on Windows (requires 12 manual `.snupkg` pushes per release). The `publish.yml` GH Actions workflow splits the push into two explicit commands (`deploy/*.nupkg` then `deploy/*.snupkg`) on `ubuntu-latest`, which is portable. Do not run `dotnet nuget push` locally for real releases — push the `v<version>` tag and let the workflow do it."

2. **Add a new lesson:** Describe the `v<version>` tag → `publish.yml` → NuGet + GH Release flow:
   - `Source/Directory.Build.props` carries `<Version>`; the 12 csprojs inherit.
   - Tag regex `v\d+\.\d+\.\d+(-...)?$` is enforced by `verify-gate`.
   - Tag version must equal `Directory.Build.props` `<Version>` exactly.
   - Tag must land on a commit with Jenkins status `<exact context from Phase 1.5>` = `success` (the B2 gate).
   - Dry-run via `workflow_dispatch` with `dry_run=true` exercises gate + pack without publishing.

3. **Update "Build Commands" section:** add a short paragraph pointing at `.github/workflows/publish.yml` for release builds, and clarify that the existing `dotnet build -c Release -p:CI=true` and `dotnet pack` commands are "what CI runs; do not invoke locally for a real release — local packs are for inspection / dry-run only."

4. **Record the Phase 1.5 Jenkins context discovery** as a small lesson ("Jenkins posts status context `<string>`; grep for this in `publish.yml` if the workflow ever breaks after a Jenkins config change").

**Optional consolidation:** Phase 3's changes are small enough that they can land as the final commits of Phase 2's PR if the reviewer prefers a single milestone PR over three small PRs. The plan/builder can decide at execution time.

### Success Criteria

- `git ls-files deploy/` returns empty.
- `.gitignore` matches `deploy/` (verify with `git check-ignore deploy/anything.nupkg`).
- CLAUDE.md lesson about `.snupkg` auto-match is corrected (grep for the phrase "automatically picks up matching" — it should no longer appear with the positive claim).
- CLAUDE.md has a new lesson describing the `v<version>` tag → `publish.yml` flow.
- CLAUDE.md "Build Commands" section points at `.github/workflows/publish.yml`.

### Hard Constraints

- Do not delete the `deploy/` directory itself if any other tracked file lives in it (grep first). Expected: only the 25 files above are tracked there.
- Preserve `.gitignore` ordering and comments; insert minimally.
- No CLAUDE.md rewrite — surgical edits only, matching existing lesson style.

### Risks / Watch-outs

- **Accidental lesson deletion:** The existing snupkg lesson is long and intermixed with two correct lessons (about NuGet version ordering and `-p:CI=true`). Only replace the snupkg sentence; keep the neighbors intact.

---

## Phase 4 — Dry-run validation (Risk: Medium, Operational)

**Risk rationale:** Medium because this is the first end-to-end exercise of the whole pipeline against real GitHub infrastructure. Not High because `dry_run=true` skips the publish job, so a failure here costs nothing but time. This phase has no code output — its artifact is confidence.

### Objective

After Phases 1–3 are merged to master, manually trigger `publish.yml` via `workflow_dispatch` with `dry_run=true`. Observe: verify-gate passes (on master's HEAD SHA), build-pack produces 12+12 artifacts, publish is skipped. Download the artifact zip and sanity-check one of the `.nupkg` files has deterministic Source Link.

### Preconditions

- Phases 1 + 2 + 3 merged to master (or landed together in one PR merged to master).
- `NUGET_API_KEY` GitHub Actions secret configured in repo Settings → Secrets and variables → Actions (one-time manual step, documented in `PROJECT.md`). Not strictly required for dry-run since the `publish` job is skipped, but should be set before the first real release regardless.
- Phase 1.5 context string discovery complete and embedded in `publish.yml`.

### Procedure

1. Navigate to the Actions tab → `publish.yml` → "Run workflow".
2. Leave ref as `master`, set `dry_run` to `true`, click Run.
3. Watch job progression:
   - `verify-gate`: on `workflow_dispatch`, the tag-regex and tag-version-match gates should gracefully handle the "no tag" case (either skip those checks when the trigger isn't `push` to a tag ref, or synthesize a pseudo-tag from `Directory.Build.props`'s version). This behavior must be designed into Phase 2 — the plan should explicitly document how the gate handles `workflow_dispatch` vs `push:tags`.
   - `build-pack`: should produce 12+12 packages, 24 artifact files uploaded.
   - `publish`: should be skipped entirely (visible as `skipped` in the UI).
4. Download the `nuget-packages-v0.9.33` artifact from the run summary. Unzip one `.nupkg` (e.g., `DotNetWorkQueue.0.9.33.nupkg`) and inspect its `.nuspec` for `<repository url="..." commit="..."/>` with a real SHA, not `$(GitCommitId)` or similar placeholder. This confirms deterministic Source Link works in Actions.

### Success Criteria

- Actions run completes with `verify-gate` green, `build-pack` green, `publish` skipped.
- Artifact `nuget-packages-v0.9.33` contains 12 `.nupkg` + 12 `.snupkg` files.
- One unzipped `.nupkg` shows a deterministic `<repository commit="<real-sha>"/>` in its `.nuspec`.
- No secret leakage in job logs (visually scan, and confirm `NUGET_API_KEY` doesn't appear).

### Done

With the dry-run green, the milestone is complete. The first real release (a future milestone — likely the next DNQ feature bundle) can confidently push `v0.9.33` and let the workflow handle everything.

### Risks / Watch-outs

- **`workflow_dispatch` edge case:** The tag-regex gate needs a branch to test from. The plan for Phase 2 must handle `github.event_name == 'workflow_dispatch'` cleanly — likely by reading the version from `Directory.Build.props` and pretending it's `v<version>` for gate purposes, OR by declaring that on `workflow_dispatch` only the tag-version-match and Jenkins B2 gates run (tag-regex is skipped because there's no tag). Either is fine; the decision must be documented in the plan and reflected in the gate script.
- **Jenkins B2 gate on master HEAD:** master may not carry a Jenkins status if the last merge was squash-merged via a UI merge rather than via a PR Jenkins built. Phase 1.5 must confirm this; if problematic, the dry-run can be run against a recent PR's post-merge SHA manually via a separate `workflow_dispatch` input (not needed for real releases since real tags always land on merged-PR SHAs).

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Jenkins status context string wrong in `publish.yml` → silent gate misfire. | Phase 1.5 discovery step is a hard gate for Phase 2 plan authoring. The context string is a literal, not a regex. Gate script fails loud on missing/empty status — no silent pass. |
| Dashboard.Ui missed by solution-level `dotnet pack` → 11+11 instead of 12+12. | Explicit `dotnet pack Source/DotNetWorkQueue.Dashboard.Ui/...` step. Count assertion in `build-pack` job fails loud if either count != 12. |
| `<Version>` not picked up in one csproj (e.g., `<VersionPrefix>` shadow) → wrong package filename. | Phase 1's local `dotnet pack` dry-run verifies 12 files named `*.0.9.33.*`. Any outlier filename (e.g., `*.0.9.32.*`) surfaces immediately. |
| `workflow_dispatch` dry-run can't run the regex gate cleanly. | Phase 2 plan explicitly designs the gate's `github.event_name` branching. Documented in Phase 4 preconditions. |
| User forgets to add `NUGET_API_KEY` before first real release. | Documented in Phase 3's CLAUDE.md lesson. First real release will fail at `dotnet nuget push` with a clear auth error — recoverable by adding the secret and re-running via `workflow_dispatch` against the same tag (`--skip-duplicate` makes this safe). |
| CLAUDE.md snupkg lesson correction accidentally deletes neighboring lessons. | Phase 3 hard constraint: surgical edits only, match existing lesson style. Reviewer greps for the specific lesson markers to confirm neighbors intact. |
| Tag on Jenkins-red commit slips through because status context typo. | Bash gate uses `--jq '... | .state'` with `set -euo pipefail`. Empty output (no matching context) fails the `[[ "$state" == "success" ]]` check. |

---

## Out of Scope (explicit)

- **Runtime version bump to 0.9.33 and actual NuGet publish.** This milestone sets `<Version>0.9.33</Version>` in `Directory.Build.props` but does **not** push the tag. The first real release is a future milestone that bundles feature work + a tag push.
- **Changes to `.github/workflows/ci.yml`.** CI stays exactly as it is. Publish is a separate workflow.
- **Changes to `Jenkinsfile` or Jenkins configuration.** The B2 gate reads Jenkins' existing output.
- **Auto-version-from-tag (PROJECT.md flavor "C").** Rejected by the user during brainstorming — a typoed tag would silently ship a wrong version.
- **Keeping `deploy/Deploy.bat` as a local fallback.** Retired. `workflow_dispatch` on the published workflow is the re-run path.
- **TaskScheduler or expression-json-serializer publish workflows.** Those sibling repos already have their own workflows.
- **Release notes authoring.** `gh release create --generate-notes` auto-scrapes merged PRs. Hand-authored notes remain a post-publish manual edit option.
- **Migration of other csproj metadata (e.g., `<Authors>`, `<PackageId>`, `<PackageTags>`) to `Directory.Build.props`.** Out of scope — this milestone moves only `<Version>`. A broader metadata centralization is a separate refactor for a future milestone.
- **Fixing the WSL `better-sqlite3` native rebuild warning.** Non-blocking dev-env noise; fix separately with `sudo apt install build-essential`.
