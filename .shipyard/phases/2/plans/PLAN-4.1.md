---
phase: taskscheduler-nuget-0.4.0
plan: 4.1
wave: 4
dependencies: [3.1]
must_haves:
  - Local clean pack produces deploy/*.0.4.0.nupkg and deploy/*.0.4.0.snupkg from the release commit
  - Packed .nupkg contains only the expected TFM assemblies (net8.0 + net10.0, NO stale net472/net48)
files_touched:
  - (none - read-only pack verification in sibling repo, no tracked files modified)
tdd: false
risk: high
---

# PLAN-4.1 — Local pre-tag pack verification

**Target repo:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Wave:** 4 (depends on the PLAN-3.1 release commit being on master)
**Risk:** HIGH — this is the LAST chance to catch a broken package before the `v0.4.0` tag push triggers CI's irreversible publish. NuGet version numbers are one-way: a bad 0.4.0 on nuget.org cannot be re-pushed or downgraded.

## Why a local pack before the tag?

CI will pack + push on tag, but by then it is too late to catch a problem. This plan reproduces the CI pack locally from a clean tree and inspects the resulting artifacts to catch:

1. Missing `.snupkg`.
2. Wrong version in the `.nupkg` filename or `.nuspec`.
3. Stale `net472` / `net48` assemblies leaking into the package (RESEARCH.md section 5.5: pre-Phase-1-era bin artifacts are present in the repo's `Source/bin/Debug/` on the local workstation).
4. Missing Source Link metadata inside the `.nupkg`.
5. TFM drift (the package should contain `lib/net8.0/` and `lib/net10.0/` only).

<task id="1" files="(local pack, no tracked files modified)" tdd="false">
  <action>
Run the complete clean-pack sequence against the PLAN-3.1 release commit on master. This is the same sequence CI will run in the publish job, reproduced locally so the builder can inspect the artifacts.

```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# Confirm we are on master at the release commit
git checkout master
git log -1 --format='%h %s'
# Expect: the release commit from PLAN-3.1

# Nuke every build artifact — especially the pre-Phase-1 net472/net48 stale DLLs
rm -rf Source/bin Source/obj deploy
rm -rf Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/bin
rm -rf Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/obj

# Confirm the stale net472/net48 directories are gone
ls Source/bin 2>/dev/null || echo "Source/bin absent (good)"

# Clean Release build
dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Release -p:CI=true

# Tests on Release build
dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj" -c Release --no-build

# Pack into deploy/
dotnet pack "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj" -c Release -p:CI=true -o deploy --no-build
```

Then list the artifacts:

```bash
ls -la deploy/
```

Expected: `deploy/` contains exactly two files, both matching version `0.4.0`:
  - `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg`
  - `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.snupkg`

If either file is missing, STOP. Do not proceed to Task 2 or PLAN-5.1. Investigate: is `IncludeSymbols=true` still set? Is `SymbolPackageFormat=snupkg` still set? Did the pack step emit errors? Fix the root cause on master as a follow-up commit BEFORE tagging.
  </action>
  <verify>
```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# 1. Exactly one 0.4.0 .nupkg and one 0.4.0 .snupkg exist in deploy/
ls deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg
ls deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.snupkg

# 2. No other-version packages leaked
ls deploy/ | grep -E '0\.[0-9]+\.[0-9]+' | grep -v '0\.4\.0' | wc -l
# Expect: 0

# 3. Clean build was warning-free (scroll up in the task output)
# Expect: "Build succeeded. 0 Error(s). 0 Warning(s)."
```
  </verify>
  <done>
`deploy/` contains exactly `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg` and `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.snupkg`. No stale-version packages. Release build was clean (0 errors, 0 warnings). Tests passed on Release.
  </done>
</task>

<task id="2" files="(package inspection, no tracked files modified)" tdd="false">
  <action>
Inspect the contents of the `.nupkg` to confirm it ships the expected files and NO stale assemblies. A `.nupkg` is a zip file — `unzip -l` lists its contents without extracting.

```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# 1. Full content listing of the nupkg
unzip -l deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg

# 2. TFM check: expect lib/net8.0/ and lib/net10.0/ present, net472/net48 ABSENT
unzip -l deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg | grep -E 'lib/net8\.0/'
unzip -l deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg | grep -E 'lib/net10\.0/'

# These must return ZERO lines — stale TFMs would burn 0.4.0
unzip -l deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg | grep -cE 'lib/net4[78]'
# Expect: 0
unzip -l deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg | grep -cE 'lib/net472|lib/netstandard'
# Expect: 0

# 3. Nuspec version check
unzip -p deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg '*.nuspec' | grep -E '<version>0\.4\.0</version>'
# Expect: one match

# 4. Source Link / repository URL present in nuspec
unzip -p deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg '*.nuspec' | grep -E '<repository '
# Expect: one match with url=... and commit=<release commit SHA>

# 5. Symbols package contains matching .pdb files
unzip -l deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.snupkg | grep -c '\.pdb'
# Expect: at least 2 (one per TFM)
```

If any of these checks fail, STOP. The plan halts. Investigate the root cause on master (file an ISSUE if needed) and do not tag v0.4.0.

Specific trap to watch for: if `grep -cE 'lib/net4[78]'` returns non-zero, the stale `Source/bin/Debug/net472` directories were not cleaned — the builder must re-run Task 1 with a more aggressive clean (`git clean -fdx Source/` if necessary, but be careful about untracked files the user may want to keep).
  </action>
  <verify>
```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# nupkg contents summary
unzip -l deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg > /tmp/nupkg-contents.txt
echo "--- nupkg contents ---"
cat /tmp/nupkg-contents.txt

# Must have both TFMs
grep -q 'lib/net8\.0/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler\.dll' /tmp/nupkg-contents.txt && echo "net8.0 OK"
grep -q 'lib/net10\.0/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler\.dll' /tmp/nupkg-contents.txt && echo "net10.0 OK"

# Must NOT have stale TFMs
grep -c 'lib/net472' /tmp/nupkg-contents.txt
grep -c 'lib/net48'  /tmp/nupkg-contents.txt
grep -c 'lib/netstandard' /tmp/nupkg-contents.txt
# All three: expect 0

# nuspec version
unzip -p deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg '*.nuspec' \
  | grep -E '<version>0\.4\.0</version>' \
  && echo "nuspec version OK"

# Repository commit in nuspec matches the release commit SHA
RELEASE_SHA=$(git rev-parse master)
unzip -p deploy/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.0.4.0.nupkg '*.nuspec' \
  | grep -E "commit=\"$RELEASE_SHA\"" \
  && echo "Source Link commit OK"
```
  </verify>
  <done>
`.nupkg` contains `lib/net8.0/` and `lib/net10.0/` assemblies. No `lib/net472`, `lib/net48`, or `lib/netstandard*` directories. `.nuspec` reports `<version>0.4.0</version>` and `<repository commit="<master SHA>">`. `.snupkg` has at least two `.pdb` files. All checks recorded in the plan's `## Task 2 Result` section. The package is ready for the tag-triggered CI push in PLAN-5.1. Local `deploy/` directory and its contents are disposable — it is git-ignored by `*.nupkg`/`*.snupkg` rules.
  </done>
</task>
