---
phase: phase-4
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - New integration test project runs on ubuntu-latest / net10.0 via GitHub Actions
  - Appended as a new step in the existing build-and-test job (no new job)
  - No coverage collection flags, no matrix changes, no connection string setup
files_touched:
  - .github/workflows/ci.yml
tdd: false
risk: low
---

# PLAN-1.1: GitHub Actions wiring for TaskScheduler Distributed integration tests

## Context

Phase 3 shipped `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests`, a net10.0-only integration test project that exercises the 0.4.0 `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` NuGet package with in-process NetMQ beacon discovery. Phase 4 wires it into both CI surfaces.

This plan handles the GitHub Actions surface only. It touches `.github/workflows/ci.yml` exclusively and is disjoint from PLAN-1.2 (Jenkinsfile), so the two plans run in parallel in Wave 1.

## Locked Decisions (see `.shipyard/phases/4/CONTEXT-4.md`)

- **Decision #1 — optimistic UDP.** No skip mechanism, no `[TestCategory]`, no special networking. `ubuntu-latest` has unrestricted UDP on loopback so beacon discovery should Just Work.
- **Decision #3 — no Codecov.** GitHub Actions does not collect coverage at all today. Do NOT add `--collect`, `--settings`, `--results-directory`, or any coverage reporting flags. The new step matches the existing bare `dotnet test ... --no-build -c Debug` shape.
- **Decision #5 — add to existing job.** No new job, no new matrix entry. Append one step to the existing `build-and-test` job. Per RESEARCH.md, this will be the FIRST integration test in `ci.yml` — there is no "Memory integration test" step to use as an analog; use the "Unit Tests - Memory" step on line 64–65 as the indentation/format template.
- **Feature branch.** Assume the working branch is not `master`. Commit message should be descriptive but not reference any specific branch name.

## Verified facts from research

- `ci.yml` has a single `build-and-test` job on `ubuntu-latest` with both net8 + net10 SDKs installed.
- Steps 1–3 are checkout / setup-dotnet / restore / build (solution-wide with `-c Debug`).
- Steps 4–14 are 11 `dotnet test` invocations for unit test projects, each `--no-build -c Debug`.
- The last step is "Unit Tests - Memory" at lines 64–65. New step inserts after line 65.
- No `-f` framework flag is used anywhere — the Phase 3 project is net10.0-only so it will only run that TFM.
- `--no-build` is safe because the earlier `dotnet build "Source/DotNetWorkQueue.sln"` step builds the whole solution.

<task id="1" files=".github/workflows/ci.yml" tdd="false">
  <action>
Append a new step to the `build-and-test` job immediately after the existing "Unit Tests - Memory" step (currently ends at line 65). Use 6-space indentation under `steps:` to match the surrounding steps. The new step must follow the exact format of the other test steps — a `- name:` line and a `run: dotnet test ...` line — with a blank line separating it from the previous step.

Insert the following verbatim after line 65 (and before any trailing newline or other content). There should be one blank line before `- name:` to match the separator pattern used between all other steps:

```yaml

      - name: Integration Tests - TaskScheduler Distributed
        run: dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" --no-build -c Debug
```

**DO NOT include any of the following flags on the `dotnet test` line** (Decision #3):
- `--collect "XPlat Code Coverage"` or `--collect:"XPlat Code Coverage"`
- `--settings` / `--settings Source/coverage.runsettings`
- `--results-directory ...`
- `--logger trx` / `-p:CollectCoverage=true`
- Any step that uploads a `.trx` or `.xml` artifact

**DO NOT add**:
- A `-f net10.0` flag (the project is net10.0-only; no flag is needed and none is used on other steps).
- A connection string setup step (Memory + Distributed TaskScheduler uses no connection string).
- A new job, matrix entry, or trigger. Append to the existing `build-and-test` job only.

Commit the change with message `shipyard(phase-4): add TaskScheduler distributed integration tests to GitHub Actions`.
  </action>
  <verify>
# 1. The new step appears exactly once in ci.yml
grep -c "Integration Tests - TaskScheduler Distributed" .github/workflows/ci.yml
# Expected: 1

# 2. The project reference appears exactly once in ci.yml
grep -c "TaskScheduling.Distributed.TaskScheduler.Integration.Tests" .github/workflows/ci.yml
# Expected: 1

# 3. No coverage/collect/trx flags were added anywhere in ci.yml (the file previously had zero such references)
grep -cE "(XPlat Code Coverage|coverage.runsettings|--collect|--results-directory|\.trx)" .github/workflows/ci.yml
# Expected: 0

# 4. YAML parses cleanly
python3 -c "import yaml, sys; yaml.safe_load(open('.github/workflows/ci.yml')); print('YAML OK')"
# Expected: YAML OK

# 5. The new step is positioned after "Unit Tests - Memory" (sanity check on ordering)
grep -n "Unit Tests - Memory\|Integration Tests - TaskScheduler Distributed" .github/workflows/ci.yml
# Expected: Unit Tests - Memory line number < Integration Tests - TaskScheduler Distributed line number
  </verify>
  <done>
- `.github/workflows/ci.yml` contains exactly one new `- name: Integration Tests - TaskScheduler Distributed` step with a `run: dotnet test ... --no-build -c Debug` line referencing the Phase 3 project csproj path.
- The new step is the last step in the `build-and-test` job and appears after "Unit Tests - Memory".
- YAML parses cleanly with `python3 -c "import yaml; yaml.safe_load(...)"`.
- No coverage-collection flags were introduced anywhere in the file.
- Single commit on the working branch (not master) with the message specified above.
  </done>
</task>
