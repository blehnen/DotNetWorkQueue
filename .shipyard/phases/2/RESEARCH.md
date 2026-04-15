# Phase 2 Research — TaskScheduler 0.4.0 Release Mechanics

**Scope:** Confirm release mechanics for the sibling repo
`/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`. Phase 2
is NuGet release management, not new code. This report captures the concrete
mechanics the architect/builder will need to land 0.4.0.

> **Headline finding (BLOCKER to re-review with user):** 0.3.0 was published
> by a **tag-triggered GitHub Actions `publish` job**, not by a user-run
> `dotnet nuget push`. `CONTEXT-2.md` locks the release to "user-run push,
> manual verification". Phase 2 must pick one of two paths: (A) mirror 0.3.0's
> mechanism and let CI push 0.4.0 on tag, or (B) override the workflow for
> this release. See Section 6 for detail. Every other locked decision
> (merge-as-task-0, CHANGELOG content, XML-doc update) is unaffected.

---

## 1. Version Source of Truth

### 1.1 Primary version location

**File:** `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj`
**Line 9:** `<Version>0.3.0</Version>`

This is the only version-bump target for 0.4.0. The csproj is a clean
SDK-style project with all NuGet metadata inline (PackageId, Authors,
Description, License, etc.).

### 1.2 Directory.Build.props — does not exist

`find Source -name 'Directory.Build.props'` returns no results. **There is
no parent props file that could override or shadow the csproj `<Version>`.**
This is the opposite of the main DNQ repo, where `Source/Directory.Build.props`
drives `ContinuousIntegrationBuild` via `$(CI)`. In the sibling repo, the
`ContinuousIntegrationBuild Condition="'$(CI)' == 'true'"` property lives
directly on the main csproj (line 23) and is the same mechanism — passing
`-p:CI=true` enables deterministic Source Link.

### 1.3 Other packaging settings already on the csproj

From `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj`:
- Line 22: `<Deterministic>true</Deterministic>`
- Line 23: `<ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>`
- Line 24: `<IncludeSymbols>true</IncludeSymbols>`
- Line 25: `<SymbolPackageFormat>snupkg</SymbolPackageFormat>`
- Line 26: `<PublishRepositoryUrl>true</PublishRepositoryUrl>`
- Line 27: `<EmbedUntrackedSources>true</EmbedUntrackedSources>`
- Line 38: `Microsoft.SourceLink.GitHub 10.0.201` PackageReference

Confirmed: `dotnet pack -c Release -p:CI=true` will emit **both** `.nupkg`
and `.snupkg` for this project. No additional MSBuild flags needed.

### 1.4 Hardcoded `0.3.0` strings outside the csproj

Grep across the full repo (excluding `.shipyard/` metadata and `obj/`):

| File | Line | Context |
|---|---|---|
| `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj` | 9 | `<Version>0.3.0</Version>` — the authoritative bump target |
| `CHANGELOG.md` | 3 | `### 0.3.0 2026-04-10` — existing entry, new 0.4.0 entry goes **above** this |
| `README.md` | 30 | Example `<PackageReference Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler" Version="0.3.0" />` inside an installation snippet |

**README.md line 30 is a doc-drift risk.** The install snippet should be
updated to `0.4.0` in the same release commit that bumps the csproj, or the
first copy-paste user of the 0.4.0 docs will install 0.3.0. The builder must
Edit this line.

No hits in any `.cs` file — there are no hardcoded assembly-info constants,
no `AssemblyInfo.cs`, and no static `Version = "0.3.0"` fields. Clean bump.

Grep also surfaces occurrences in `.shipyard/*` (ROADMAP/PROJECT/HISTORY/
LESSONS/STATE/AUDIT/REVIEW/PLAN files) — **ignore these.** They document the
0.3.0 release itself and must remain as historical record.

---

## 2. CHANGELOG Format

**File:** `CHANGELOG.md` (repo root)

### 2.1 Exact format of the existing 0.3.0 heading

Line 3: `### 0.3.0 2026-04-10`

Three hashes, a space, version, a space, ISO-8601 date (no dashes around it,
no link wrapping, no `[0.3.0]` Keep-a-Changelog bracket syntax). **Bullet
style is a single `*` followed by a space** (not `-`).

### 2.2 Heading history mirrors the same format

- `### 0.2.1 2026-04-05` (line 15)
- `### 0.2.0 2026-03-05` (line 24)
- `### 0.1.1 2019-06-02` (line 31)
- `### 0.1.0 2015-10-23` (line 36)

There is no `## Unreleased` section. No `[Unreleased]` link-reference block
at the bottom. The file is a flat heading-per-release list under a single
`# Changelog` top-line. **The Phase 1 documenter's draft uses the right
format** (`### 0.4.0 2026-04-XX`) — the only transformation needed is
replacing `XX` with the actual release day-of-month.

### 2.3 Issue-link format in existing entries

0.3.0 references issue #6 as:
`[issue #6](https://github.com/blehnen/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/issues/6)`

This matches the format the Phase 1 documenter used in the 0.4.0 draft in
`DOCUMENTATION-1.md`. No change needed.

### 2.4 Insertion point

The 0.4.0 entry goes **immediately after line 2 (the blank line under
`# Changelog`)** and **immediately above line 3 (`### 0.3.0 2026-04-10`)**,
separated by one blank line on each side, matching the visual spacing of
the existing entries.

---

## 3. Git Tag Convention

### 3.1 Tag format for 0.3.0

From `.shipyard/STATE.json` (line 5): `"tagged v0.3.0"`
From `.shipyard/HISTORY.md` (line 36): `"tagged v0.3.0, pushed to origin/master + tag"`

**Format: `v`-prefixed (`v0.3.0`, not `0.3.0`).**

### 3.2 The tag IS the release trigger (critical)

`.shipyard/ROADMAP.md` line 77 and line 85 confirm that the v-tag is **both**
the release marker **and** the GitHub Actions publish trigger. The publish
workflow is `on: push tags: ['v*']` (confirmed — see Section 6).

Implication: pushing `v0.4.0` to origin will automatically trigger
`dotnet nuget push` inside GitHub Actions. **Do not push the tag until you
are ready for the package to go live.**

### 3.3 Annotated vs lightweight, signed vs unsigned

Not directly verifiable from the files I inspected — `git tag -l` would need
a Bash call to list with `--format` to know for certain. Uncertainty flagged
in Section 8. The HISTORY.md says "tagged v0.3.0, pushed to origin" without
specifying `-a` vs default. Reasonable mirror: `git tag -a v0.4.0 -m "Release
0.4.0 — lock contention fix"` (annotated, unsigned), but the architect should
confirm against the actual 0.3.0 tag metadata during plan creation.

---

## 4. 0.3.0 Release Commit Shape

### 4.1 Commit hash and message

From `.shipyard/HISTORY.md` (line 36): commit `a392e62` was the release
commit tagged as `v0.3.0`. I could not run `git show a392e62` without a Bash
shell (tool availability), so the exact commit-message prose and file-diff
list are not captured verbatim here. Architect should `git show a392e62
--stat` during plan creation to extract:

1. The exact subject line format (likely `Release 0.3.0` or similar)
2. Whether the version bump, CHANGELOG entry, and any doc updates were in
   one commit or split
3. Whether the tag was on the same commit as the bump, or a follow-up

### 4.2 Inferred minimal touch surface for 0.4.0 release commit

Based on repo inspection (no Directory.Build.props, single csproj, single
CHANGELOG, single README mention), the 0.4.0 release commit should touch
at most four files:

1. `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj`
   (line 9: `0.3.0` → `0.4.0`)
2. `CHANGELOG.md` (insert the `### 0.4.0 <date>` block from
   `DOCUMENTATION-1.md` above the existing 0.3.0 heading)
3. `README.md` (line 30: update the install example version string)
4. `Source/ITaskSchedulerJobCountSync.cs` + `Source/TaskSchedulerJobCountSync.cs`
   (the ISSUE-028 `<remarks>` addition on `Start()` — see Section 7)

Plus ISSUE-028 is two files — so total is **five files, one commit**. Very
tight diff, perfect for a release commit.

---

## 5. Build + Pack Commands That Will Work

### 5.1 Solution file

`Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln` exists.
Confirmed via Glob.

### 5.2 Command sequence

```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# Clean
rm -rf Source/bin Source/obj \
       Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/bin \
       Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/obj \
       deploy

# Release build (CI=true for deterministic Source Link)
dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" \
  -c Release -p:CI=true

# Tests on Release build
dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj" \
  -c Release --no-build

# Pack with output to ./deploy
dotnet pack "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj" \
  -c Release -p:CI=true -o deploy --no-build

ls -la deploy/
# Expect: DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg
#         DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.snupkg
```

### 5.3 Does `-o deploy` produce both `.nupkg` and `.snupkg`?

**Yes.** The csproj has `<IncludeSymbols>true</IncludeSymbols>` (line 24) and
`<SymbolPackageFormat>snupkg</SymbolPackageFormat>` (line 25). `dotnet pack -o <dir>`
honors both and writes the `.snupkg` alongside the `.nupkg` in the output
directory. Main DNQ CLAUDE.md lesson confirms same mechanism: *"push from
the deploy directory using `dotnet nuget push deploy/*.nupkg` — the CLI
automatically picks up matching `.snupkg` files from the same directory."*

### 5.4 `deploy/` in `.gitignore`?

**No, `deploy/` is not explicitly ignored.** However the `.gitignore` at
repo root has `*.nupkg` (line 204) and `*.snupkg` (line 206), so the
**contents** of `deploy/` will be git-ignored, but the directory itself could
get tracked if anything else ends up in it. Recommendation: the plan either
(a) adds `deploy/` to `.gitignore` before packing, or (b) packs into an
out-of-tree directory (`/tmp/tsd-deploy` or similar). Low-risk either way
because `*.nupkg`/`*.snupkg` are already ignored — this is belt-and-braces.

### 5.5 Stale build artifacts present in repo

`Glob` surfaced a `Source/bin/Debug/net472/` and `Source/bin/Debug/net48/`
directory containing pre-Phase-1-era `.dll`s (Schyntax, NaCl, AsyncIO,
JpLabs.DynamicCode, Aq.ExpressionJsonSerializer, System.ComponentModel.Annotations).
These are leftover from when the csproj still targeted `net48`/`net472`
before the 0.3.0 TFM drop. They are git-ignored (`Source/bin/` is under the
`[Bb]in/` gitignore rule line 33), but they will poison the Release pack
if not cleaned. **The clean step in Section 5.2 MUST `rm -rf Source/bin
Source/obj`** before the Release build — else `dotnet pack` may pick up
stale net472 assemblies. This is specifically the class of bug the CLAUDE.md
lessons warn about.

---

## 6. NuGet Publish Mechanism — CRITICAL CONTRADICTION WITH CONTEXT-2

### 6.1 What CONTEXT-2.md says (locked decision #2)

> The plan includes the exact `dotnet nuget push "deploy/*.nupkg"` command
> for the user to run locally. The builder does NOT execute the push... I
> don't have access to the user's NuGet API key.

### 6.2 What the sibling repo actually does

**File:** `.github/workflows/ci.yml`

The workflow has **two jobs**: `build` (runs on push/PR) and `publish`
(runs only on `v*` tag push). The `publish` job:

```yaml
publish:
  name: Publish to NuGet
  needs: build
  runs-on: ubuntu-latest
  if: startsWith(github.ref, 'refs/tags/v')
  steps:
    - name: Checkout (actions/checkout@v4)
    - name: Setup .NET (8.0.x + 10.0.x)
    - name: Restore (Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln)
    - name: Pack
      run: dotnet pack Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj -c Release --no-restore
    - name: Push nupkg to NuGet
      run: dotnet nuget push "Source/bin/Release/*.nupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
    - name: Push snupkg to NuGet
      run: dotnet nuget push "Source/bin/Release/*.snupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

`.shipyard/HISTORY.md` line 36 confirms this is how 0.3.0 shipped:
*"Release 0.3.0 committed as a392e62, tagged v0.3.0, pushed to origin/master
+ tag. GitHub Actions ci.yml build + Publish to NuGet jobs both succeeded
(run 24266260265). Package...0.3.0 published to nuget.org."*

### 6.3 Specific discrepancies vs CONTEXT-2 assumptions

1. **Who pushes.** CONTEXT-2 assumes the user runs `dotnet nuget push` locally.
   0.3.0 shipped via CI on tag. The `NUGET_API_KEY` is stored as a GitHub
   repo secret, not a local env var.
2. **Source path.** CONTEXT-2 assumes `deploy/*.nupkg`. The workflow uses
   `Source/bin/Release/*.nupkg` (the default `dotnet pack` output path, no
   `-o` flag).
3. **Two separate push commands.** The workflow pushes `.nupkg` and
   `.snupkg` in **separate steps**. CLAUDE.md's main-repo lesson is
   *"NuGet.org does not allow pushing `.snupkg` separately after the `.nupkg`
   is already published"* — this **should** fail for the sibling repo for
   the same reason. Either (a) the `--skip-duplicate` flag saves it (it
   doesn't — `--skip-duplicate` handles "already pushed," not
   "symbols-after-main"), or (b) nuget.org's behavior differs from the
   main-repo lesson's reading, or (c) 0.3.0's symbols quietly didn't
   publish and no one noticed. **Uncertainty flag** — needs human
   verification against the live 0.3.0 package on nuget.org (check if
   symbols show green).
4. **No `-p:CI=true` on pack.** The workflow's `dotnet pack` step does not
   pass `-p:CI=true`. However, GitHub Actions sets `CI=true` as an
   environment variable automatically on all runners, and the csproj
   condition is `Condition="'$(CI)' == 'true'"`, so MSBuild still picks it
   up via the env var. **This is fragile** — if someone ever runs the
   workflow on a runner that doesn't auto-set `CI=true`, deterministic
   Source Link would silently regress. The Phase 2 plan should consider
   adding `-p:CI=true` explicitly.
5. **Pack doesn't use `-o`.** Artifacts land in `Source/bin/Release/`, not
   `deploy/`. The plan's verification step can't assume `deploy/` exists
   on the CI push path.

### 6.4 Decision required from the user (escalate to architect)

Phase 2 must pick one:

- **Path A — Mirror 0.3.0 (tag-triggered CI publish).** Merge phase-1-lock-fix
  → master, bump version + CHANGELOG + README + XML doc in one commit, push
  to master, tag `v0.4.0`, push tag, watch GitHub Actions publish job run.
  No local pack needed at all (though local pack for pre-flight validation
  is still wise). This matches 0.3.0's lessons. **Recommended.**
- **Path B — Local push from workstation.** As CONTEXT-2 currently describes.
  Requires either (a) temporarily disabling the publish job (so pushing the
  tag doesn't double-publish and error), or (b) timing the push so the
  user's `dotnet nuget push` wins the race with CI. Messy; not recommended.

Path A also gives us **one free pre-flight**: CI's build+test job runs on
every push to master, so after merging phase-1-lock-fix → master, GitHub
Actions will re-run the test suite on master before we tag. Path B skips
this safety net.

**Regardless of path**, the plan should include a **local pre-flight
validation** step (clean, Release build, tests, pack, inspect `.nuspec`
inside the `.nupkg`) before pushing the tag. The cost of catching a
broken package locally is ~5 minutes; the cost of a bad CI push is
burning 0.4.0.

### 6.5 `--skip-duplicate` behavior note

The workflow uses `--skip-duplicate` on both push steps. For a fresh
0.4.0 with no prior push, this is a no-op. Its effect is that re-running
the workflow after a partial failure won't error — useful recovery
behavior. Do not remove it.

---

## 7. ISSUE-028 — XML Doc Update Surface

Phase 1 deferred adding a `<remarks>` block to `Start()` on both the
interface and the implementation. Phase 2's release commit is where it
lands.

### 7.1 `Source/ITaskSchedulerJobCountSync.cs`

**Lines 52–55** — exact current state:

```csharp
        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start();
```

The `void Start();` is at **line 55** (matches the expected line from the
task brief). Insertion point for `<remarks>`: between the closing `</summary>`
line 54 and the `void Start();` line 55.

### 7.2 `Source/TaskSchedulerJobCountSync.cs`

**Lines 103–106** — exact current state:

```csharp
        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
```

The `public void Start()` is at **line 106** (also matches the expected
line). Insertion point: between `</summary>` line 105 and `public void
Start()` line 106. The `<remarks>` block should be byte-identical between
the two files so IDE tooltips match across the interface abstraction, per
the Phase 1 documenter's recommendation.

### 7.3 Proposed `<remarks>` content

From `DOCUMENTATION-1.md`, the behavior to document:
> `Start()` is now non-blocking. It still performs the host-address
> handshake, the ~1.1s beacon grace sleep, and the initial `BroadCast`
> synchronously on the caller thread, but socket-poll wiring is now spawned
> onto a dedicated background thread and `Start()` returns as soon as that
> thread is running.

A ~3-line `<remarks>` capturing this is the Phase 2 builder's output. The
existing `<summary>` ("Starts this instance.") should remain unchanged — the
remarks block is additive.

---

## 8. Uncertainty Flags

1. **DECISION REQUIRED — Phase 2 publish path.** CONTEXT-2 locks "user-run
   push" but the repo's 0.3.0 shipped via tag-triggered CI. Architect must
   resolve with the user before writing the plan. Recommendation: Path A
   (mirror 0.3.0, let CI publish on tag).
2. **Symbols health of 0.3.0 on nuget.org.** The workflow's separate
   `.snupkg` push step contradicts a documented lesson from the main DNQ
   repo. A human should visit the nuget.org page for
   `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler 0.3.0` and
   confirm the "Symbols" and "Source Link" badges are green. If they're
   red, Phase 2 should fix the workflow (push both files in one `deploy/*.nupkg`
   pattern form) before the 0.4.0 push.
3. **Exact 0.3.0 tag metadata.** Annotated vs lightweight, signed vs
   unsigned, and on which commit exactly — not verified. Architect should
   run `git show v0.3.0` + `git for-each-ref refs/tags/v0.3.0` during plan
   creation and instruct the builder to mirror the same convention.
4. **Exact commit-message prose of release commit `a392e62`.** Not read in
   this research pass. Architect should `git show a392e62 --stat` and lift
   the subject-line style for the 0.4.0 release commit.
5. **Whether `deploy/` exists locally on the user's workstation.** Not
   checked. If it does and holds stale artifacts, they must be cleaned.
   The plan's clean step must `rm -rf deploy` just like `rm -rf obj bin`.
6. **Jenkinsfile presence.** A `Jenkinsfile` exists at the repo root (per
   Glob), but the 0.3.0 release used GitHub Actions for the publish, and
   CONTEXT-2 does not mention Jenkins. The Jenkinsfile is probably only
   wired for CI (not publish). Not a Phase 2 blocker, but flag for the
   architect to confirm it doesn't also push packages on tag events.

---

## 9. Summary of Locked-In Mechanics for the Plan

Subject to Section 6.4 resolution, the following are confirmed and
load-bearing:

| Item | Value | Source |
|---|---|---|
| Version file | `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj` line 9 | direct read |
| Current value | `0.3.0` | direct read |
| Directory.Build.props | does not exist | Glob (no results) |
| Other version strings | `README.md` line 30 (install example), `CHANGELOG.md` line 3 (heading only — not to rewrite) | Grep |
| CHANGELOG heading format | `### <version> <YYYY-MM-DD>` | `CHANGELOG.md` lines 3/15/24/31/36 |
| CHANGELOG bullet style | `*` | `CHANGELOG.md` lines 5–13 |
| CHANGELOG insertion point | directly above line 3 (`### 0.3.0 2026-04-10`) | `CHANGELOG.md` |
| Issue link format | `[issue #6](https://github.com/blehnen/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/issues/6)` | `CHANGELOG.md` line 13 |
| Tag format | `v0.3.0` (v-prefixed) | `.shipyard/HISTORY.md` line 36, `.shipyard/STATE.json` line 5 |
| 0.3.0 release commit | `a392e62` | `.shipyard/HISTORY.md` line 36 |
| Solution file | `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln` | Glob |
| Release build cmd | `dotnet build "Source/...sln" -c Release -p:CI=true` | csproj `ContinuousIntegrationBuild` condition |
| Pack cmd | `dotnet pack "Source/...csproj" -c Release -p:CI=true -o deploy --no-build` | csproj settings, CLAUDE.md lessons |
| Symbols emitted? | yes, `.snupkg` via `IncludeSymbols`+`SymbolPackageFormat` | csproj lines 24–25 |
| Publish mechanism (0.3.0) | tag-triggered CI job, not user-run push — **CONTRADICTS CONTEXT-2** | `.github/workflows/ci.yml` lines 36–63 |
| `NUGET_API_KEY` location | GitHub repo secret (CI), not user's local env var | workflow line 60 |
| ISSUE-028 interface line | `Source/ITaskSchedulerJobCountSync.cs` line 55 (`void Start();`) | Grep |
| ISSUE-028 impl line | `Source/TaskSchedulerJobCountSync.cs` line 106 (`public void Start()`) | Grep |
| Stale bin artifacts present | yes, pre-Phase-1 net472/net48 DLLs in `Source/bin/Debug/` — must clean | Glob |
