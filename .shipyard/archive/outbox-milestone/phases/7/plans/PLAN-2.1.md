---
phase: documentation
plan: 2.1
wave: 2
dependencies: [1.1, 1.2, 1.3]
must_haves:
  - Full-solution Release+CI=true build of DotNetWorkQueueNoTests.sln succeeds with zero XML-doc warnings
  - Source Link metadata verified on at least one packed outbox project (deterministic paths, ContinuousIntegrationBuild=true)
  - README → docs/outbox-pattern.md link target resolves
files_touched: []
tdd: false
risk: low
---

# Plan 2.1: Full-solution Release verification + Source Link spot-check

## Context

Wave 1 makes three independent changes:
- PLAN-1.1 adds the net8.0 XML-doc gate to `Transport.RelationalDatabase.csproj` and
  closes ISSUE-032 inline via `WarningsNotAsErrors NU1902` on `Transport.SQLite.csproj`.
- PLAN-1.2 creates `docs/outbox-pattern.md`.
- PLAN-1.3 inserts the README pointer bullet.

PLAN-2.1 is the final gate: run the **full-solution** Release build that ROADMAP §Phase 7
calls out as the success criterion, then spot-check Source Link / deterministic-path
embedding by packing one of the outbox csprojs and inspecting the resulting nuspec
metadata. PROJECT.md §Non-Functional Determinism requires this `-p:CI=true` flag.

Wave 2 depends on Wave 1 because the full-solution build only passes after both csproj
edits land (PLAN-1.1), and the README→doc link target only resolves after PLAN-1.2 ships
the file.

## Dependencies

- PLAN-1.1 (csproj fixes) — full-solution Release build will still fail without the
  SQLite NU1902 fix.
- PLAN-1.2 (`docs/outbox-pattern.md`) — README link target verification needs the file.
- PLAN-1.3 (README bullet) — link source verification needs the bullet.

## Tasks

### Task 1: Full-solution Release+CI=true build verification

**Files:** none modified; verification only
**Action:** verify
**Description:** Run the authoritative Phase 7 success-criterion build command:

```bash
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true
```

Capture the full build output. The build must:
- Exit 0 (`Build succeeded`).
- Report `0 Error(s)`.
- Report `0` CS1591 warnings across all projects.
- NU1902 may surface as a warning on `Transport.SQLite` and other OpenTelemetry-using
  projects (~14 expected per RESEARCH.md §2); that is pre-existing and out of scope.

If CS1591 appears, **do not edit the source files** — report the specific member(s) back
to the architect. RESEARCH.md §1 verified all known new public types carry XML docs; any
CS1591 means a member was missed, which is a finding to escalate, not silently patch.

**Acceptance Criteria:**
- `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true` exits 0.
- Output contains `0 Error(s)`.
- `grep -c "warning CS1591"` against the captured build output returns 0.
- NU1902 entries appear as `warning` not `error` in build output (confirms PLAN-1.1
  Task 2 took effect).

### Task 2: Source Link / determinism spot-check on a packed outbox csproj

**Files:** none modified; verification only
**Action:** verify
**Description:** Pack one of the four outbox-touching csprojs in Release+CI=true and
inspect the produced `.nuspec` (extracted from the `.nupkg`) for the Source Link and
deterministic-build metadata that `-p:CI=true` is supposed to inject. Pick
`DotNetWorkQueue.Transport.RelationalDatabase` (smallest of the four, most likely to
expose any nuspec-level regression from the new `Release|net8.0` block).

```bash
# Pack
dotnet pack "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" \
  -c Release -p:CI=true -o ./pack-verify

# Extract and inspect the nuspec (the .nupkg is a zip)
PKG=$(ls ./pack-verify/DotNetWorkQueue.Transport.RelationalDatabase.*.nupkg | head -1)
unzip -p "$PKG" "*.nuspec" > ./pack-verify/extracted.nuspec
cat ./pack-verify/extracted.nuspec
```

Inspect the nuspec for:
- `<repository ... type="git" ... commit="<sha>" />` element (Source Link's repo
  metadata).
- A `<requireLicenseAcceptance>` field (sanity check that pack succeeded with full
  metadata, not a stub).

Also inspect the compiled assembly with `ildasm` / `strings` / `readelf`-equivalent to
confirm deterministic source paths if available; this is a nice-to-have, not blocking.
The repository-element presence + nupkg packing successfully is sufficient evidence that
`ContinuousIntegrationBuild=true` was honored.

Clean up the `./pack-verify` directory after inspection.

**Acceptance Criteria:**
- `dotnet pack` exits 0 for `Transport.RelationalDatabase.csproj`.
- Extracted `.nuspec` contains a `<repository>` element with `type="git"` and a non-empty
  `commit` attribute.
- No CS1591 or other doc warnings during the pack step.
- The `./pack-verify` directory is removed before commit (it's a transient artifact).

### Task 3: README link target resolution check

**Files:** none modified; verification only
**Action:** verify
**Description:** Confirm the README bullet's link target exists and that no other
markdown link in the README broke. This is a 30-second sanity check using `grep` + `test`.

```bash
# Link target file exists
test -f docs/outbox-pattern.md && echo OK

# README bullet present with correct relative link
grep -F "[\`docs/outbox-pattern.md\`](docs/outbox-pattern.md)" README.md

# No accidental duplicate of the outbox bullet
grep -c "Transactional outbox pattern" README.md
# Expect: 1
```

**Acceptance Criteria:**
- `docs/outbox-pattern.md` exists.
- README contains exactly one outbox bullet with the exact link syntax above.
- No other markdown link in `README.md` references a path containing `outbox` (no leftover
  drafts).

## Verification

```bash
# Task 1 — full-solution Release build
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true 2>&1 | tee /tmp/phase7-release.log
grep -c "warning CS1591" /tmp/phase7-release.log   # expect 0
grep -c "error " /tmp/phase7-release.log           # expect 0
grep "Build succeeded" /tmp/phase7-release.log

# Task 2 — Source Link / pack spot-check
dotnet pack "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release -p:CI=true -o ./pack-verify
PKG=$(ls ./pack-verify/DotNetWorkQueue.Transport.RelationalDatabase.*.nupkg | head -1)
unzip -p "$PKG" "*.nuspec" | grep -E '<repository|commit='
rm -rf ./pack-verify

# Task 3 — README link target
test -f docs/outbox-pattern.md
grep -c "Transactional outbox pattern" README.md   # expect 1
```

## PROJECT.md Success Criteria coverage

| Plan element | §SC |
|---|---|
| Full-solution Release+CI=true build clean of XML-doc warnings | §SC #10 + ROADMAP §Phase 7 success criterion |
| Source Link / deterministic build verified | §Non-Functional Determinism (PROJECT.md §86–92) |
| README pointer link resolves | §SC #10 (per ROADMAP wording: "README points at the new page") |
