# Project: DNQ Automated NuGet Publishing via GitHub Actions

**Captured:** 2026-04-16
**Branch:** `master` (feature branch TBD at `/shipyard:plan` time)
**Shipping target:** no runtime version bump for this milestone itself — first real exercise on the next DNQ release (likely `v0.9.33`)

## Description

Today, DNQ ships to NuGet.org via a manual local workflow: `dotnet build -c Release -p:CI=true`, `dotnet pack` (solution + explicit Dashboard.Ui), then 24 `dotnet nuget push` invocations against the `deploy/` directory (12 `.nupkg` + 12 `.snupkg`, with the `.snupkg` half done by hand because the CLI's auto-match logic is unreliable on Windows). Release discipline is entirely in the committer's head.

This milestone replaces that manual flow with a `v*` tag-triggered GitHub Actions workflow that builds, packs all 12 packages (including the Web-SDK-based Dashboard.Ui), publishes nupkg+snupkg to NuGet.org, and creates a GitHub Release with attached artifacts. A pre-publish "verify gate" job enforces three invariants: (1) the tag matches `v<SemVer>` regex, (2) the tag name equals `Source/Directory.Build.props`'s `<Version>`, and (3) the tagged commit carries a green Jenkins status (the "B2" gate — tag must land on a commit Jenkins has already blessed, which naturally forces PR-discipline since only PR'd commits get full Jenkins integration coverage). Reference workflows exist at `F:\Git\DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` and `F:\Git\expression-json-serializer`, but both are single-package repos; DNQ's 12-package multi-project layout plus Dashboard.Ui's Web-SDK quirk require a different packing strategy.

## Goals

1. **One action ships a release:** pushing tag `v0.9.33` on a green master commit results in all 12 packages on NuGet.org within ~60 seconds, with a GitHub Release auto-created containing the attached 24 artifact files, with no manual steps after the tag push.
2. **Prevent misfires:** a malformed tag (`vtest`, `v0.9`), a tag/Directory.Build.props version mismatch, or a tag on a Jenkins-red commit must fail the workflow *before* any publish step runs.
3. **Eliminate Windows snupkg pain:** all `.snupkg` pushes run on ubuntu-latest via explicit split `nuget push` commands (one per file glob), sidestepping the CLI's Windows auto-match bug entirely.
4. **Single version source of truth:** `Source/Directory.Build.props` carries `<Version>`; the 12 csprojs inherit. One edit per release instead of 12.
5. **Audit trail:** the workflow uploads the 24 packaged files as a 90-day retention artifact, so if NuGet.org rejects a package, the exact bits can be downloaded without a rebuild.

## Non-Goals

- **Version auto-derivation from the tag.** Rejected (flavor "C") because a typoed tag would silently ship a wrong version. Version lives in `Directory.Build.props`; the tag is just a trigger and its name is validated to match.
- **Jenkins status gating via Jenkinsfile changes.** Rejected (flavor "B1") — we use the cheaper B2 variant that just reads GitHub's commit-status API for the pre-existing Jenkins rollup status posted from the original PR build. No Jenkinsfile edits.
- **Keeping `deploy/Deploy.bat` as a local fallback.** Retired — CI becomes the only path. The workflow can be re-run manually via `workflow_dispatch` if ad-hoc re-publishing is needed.
- **Auto-publishing to TaskScheduler or expression-json-serializer repos.** Those already have their own workflows and are out of scope.
- **Release notes authoring.** GH Release notes use `--generate-notes` (auto-scraped from merged PRs since last tag). Hand-authored notes remain the committer's option via manual Release edits post-publish.
- **Shipping a runtime version bump as part of this milestone.** The workflow is infrastructure — first real release it exercises becomes the next version (likely `v0.9.33` in a future milestone).

## Requirements

### Workflow structure (`.github/workflows/publish.yml`, new file)

- Trigger: `push: tags: [ 'v*' ]` + `workflow_dispatch` (with a `dry_run: boolean` input that short-circuits the publish/release steps while still running gate + build-pack).
- Three sequential jobs on `ubuntu-latest`, each `needs:` the previous:
  1. **`verify-gate`** — runs a script (bash) that:
     - Asserts the tag matches regex `^v\d+\.\d+\.\d+(-[\w\.]+)?$`.
     - Extracts the tag's SHA, checks out `Source/Directory.Build.props` at that SHA, greps `<Version>...</Version>`, and asserts the stripped tag equals that value.
     - Queries `GET /repos/{owner}/{repo}/commits/{sha}/statuses` via `gh api` or the REST API directly, and asserts that the named Jenkins rollup status context (exact string TBD at implementation — verified via a recent PR's statuses, most likely `continuous-integration/jenkins/branch`) has state `success`.
     - Fails the job with a descriptive error message on any mismatch.
  2. **`build-pack`** — runs `dotnet restore`, then `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true --no-restore`, then two `dotnet pack` calls (solution-level `--no-build`, then explicit `Dashboard.Ui` `--no-build`) into `deploy/`. Asserts exactly 12 `.nupkg` + 12 `.snupkg` files exist in `deploy/` (fail on any other count). Uploads `deploy/*` as workflow artifact `nuget-packages-v<version>` with 90-day retention.
  3. **`publish`** — downloads the artifact, runs two independent `dotnet nuget push` commands (`.nupkg` glob then `.snupkg` glob) against `https://api.nuget.org/v3/index.json` with `--api-key ${{ secrets.NUGET_API_KEY }} --skip-duplicate`. On success, runs `gh release create v<version> deploy/*.nupkg deploy/*.snupkg --title "DNQ v<version>" --generate-notes`. Skipped entirely when `dry_run=true`.

### Source structure changes

- `Source/Directory.Build.props` gains a `<Version>0.9.33</Version>` line in the existing `<PropertyGroup>`. (Initial value matches the next planned release; bumped per-release going forward.)
- All 12 packable csprojs have their `<Version>0.9.32</Version>` lines removed:
  - `DotNetWorkQueue`
  - `DotNetWorkQueue.Dashboard.Api`
  - `DotNetWorkQueue.Dashboard.Client`
  - `DotNetWorkQueue.Dashboard.Ui`
  - `DotNetWorkQueue.Transport.LiteDB`
  - `DotNetWorkQueue.Transport.Memory`
  - `DotNetWorkQueue.Transport.PostgreSQL`
  - `DotNetWorkQueue.Transport.Redis`
  - `DotNetWorkQueue.Transport.RelationalDatabase`
  - `DotNetWorkQueue.Transport.SQLite`
  - `DotNetWorkQueue.Transport.Shared`
  - `DotNetWorkQueue.Transport.SqlServer`
- `.gitignore` confirms `deploy/` is ignored (or adds it if missing). The existing tracked `deploy/Deploy.bat` and `deploy/*.nupkg` / `deploy/*.snupkg` files are deleted from the repo.

### Documentation changes

- `CLAUDE.md` lesson correction: the current lesson claims `dotnet nuget push *.nupkg` auto-pushes matching `.snupkg` files. This is false on Windows (user has had to manually push all 12 snupkg files every release). Replace with guidance that the GH Actions workflow splits into two push commands for portability.
- `CLAUDE.md` new lesson: describe the `v<version>` tag → workflow release flow, the B2 Jenkins gate, and `Source/Directory.Build.props` as the single version source of truth.
- `CLAUDE.md` "Build Commands" section: add a short paragraph pointing at `.github/workflows/publish.yml` for release builds, keeping the existing `dotnet pack` commands as "this is what CI runs; don't invoke locally for a real release."

### One-time manual configuration (outside the PR)

- Add `NUGET_API_KEY` secret in GitHub repo Settings → Secrets and variables → Actions.
- No additional permissions changes required — `GITHUB_TOKEN` with default `contents: write` is sufficient for `gh release create`.

## Non-Functional Requirements

- **Idempotency:** Re-running the workflow on a partial-failure tag must safely complete the remaining pushes. Achieved via `--skip-duplicate` on both NuGet push commands and `gh release create` running last (failure before push leaves no Release to conflict with; failure after leaves the Release but `--skip-duplicate` prevents re-push conflicts).
- **Portability:** Workflow runs on `ubuntu-latest`. No Windows-specific steps. All path separators and glob patterns must work in bash.
- **Fail-fast:** Any gate mismatch or count assertion must abort the workflow before any network side effect (NuGet push, Release creation).
- **Determinism:** `-p:CI=true` on all build + pack calls enables `ContinuousIntegrationBuild` (per existing `Directory.Build.props`), ensuring deterministic Source Link paths so NuGet.org's Source Link validation stays green.
- **Secret hygiene:** `NUGET_API_KEY` is referenced only in the `publish` job. No echo, no log leakage.
- **Observability:** Each job's summary clearly reports: tag validated, version matched, Jenkins status observed, 24 files packed, N files pushed, Release URL.

## Success Criteria

1. **Dry-run passes:** `workflow_dispatch` with `dry_run=true` on the merged milestone runs `verify-gate` + `build-pack` to green with 24 artifacts uploaded, no push, no Release — confirming packaging works without spending a version.
2. **First real release ships:** the next DNQ release tag (e.g., `v0.9.33`) fires the workflow, 12 `.nupkg` + 12 `.snupkg` land on NuGet.org within ~60 seconds of the tag push, GH Release is auto-created with all 24 files attached, and the NuGet.org Source Link indicator shows green for each package.
3. **Bad-tag misfires blocked:** pushing `vtest` or `v0.9.34` when `Directory.Build.props` says `0.9.33` fails at `verify-gate` with no network side effect.
4. **Jenkins-red commit blocked:** tagging a commit whose Jenkins status is `failure` or `pending` fails at `verify-gate`.
5. **No snupkg manual steps:** the user never runs `dotnet nuget push` locally for `.snupkg` files again.
6. **Version bump is one commit:** the release PR for the next version touches `Source/Directory.Build.props` only (plus any feature-specific source), not 12 csprojs.

## Constraints

- **Technical:**
  - DNQ has 12 packable csprojs today, one of which (`Dashboard.Ui`) uses `Microsoft.NET.Sdk.Web` and is NOT picked up by solution-level `dotnet pack`. Workflow must explicit-pack it.
  - Jenkins is PR-triggered, not branch-triggered (per existing lessons). This is what makes the B2 gate pattern work: PR builds post commit statuses that later tag commits on master inherit.
  - The exact Jenkins status context name is unknown at design time — must be observed from a recent PR's commit statuses during implementation.
  - `Directory.Packages.props` exists (`ManagePackageVersionsCentrally=true`), so package-reference versions are already centralized — no interaction with this milestone's `<Version>` centralization, but worth noting both centralizations live in `Source/`.
- **Operational:**
  - User must add `NUGET_API_KEY` secret manually in GitHub repo settings before the first real release.
  - The `better-sqlite3` native rebuild warning in the WSL dev environment is non-blocking but worth fixing separately (`sudo apt install build-essential`).
- **Scope:**
  - No changes to `.github/workflows/ci.yml`. It keeps running unit tests on PRs and master pushes.
  - No changes to Jenkinsfile. The B2 gate reads Jenkins' existing status output.
- **Timeline:** no external deadline; this is developer-experience infrastructure. Ship when ready.
