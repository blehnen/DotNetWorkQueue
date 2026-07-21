# Code Coverage Policy

DotNetWorkQueue targets a **90% line-coverage floor** across its product assemblies.
Both unit and integration tests count toward the number.

## Authoritative report

The source of truth is the **ReportGenerator-merged Cobertura report**,
`coverage/report/Cobertura.xml`, produced by the Jenkins pipeline from the 14
coverage-bearing stages. It contains product assemblies only — every test assembly is
stripped at collection time by the repo-wide Coverlet `Exclude` filter in
`Source/Directory.Build.props` (`[*.Tests]*`, `[*.IntegrationTests]*`, …).

**Codecov is a convenience mirror, not the authority.** It has a demonstrated
skipped-upload failure mode (see below), so when the two disagree, the merged Cobertura
report wins.

## The staged gate

Coverage is enforced by two Codecov status checks (`codecov.yml`), turned on in stages so
the remediation work that restores the floor is not failed by the gate it is fixing:

| Check | Rule | Status |
|---|---|---|
| `patch` | new/changed lines in a PR must be **≥90%** covered (hard target, no threshold) | **live now (Phase 0)** |
| `project` | whole-repo coverage must be **≥90%** (`threshold: 1%`) | hardens at **Phase 2c**; currently still `auto`/`2%` |

`patch` grades only the lines a PR touches, so it never punishes a PR for pre-existing
debt — it just stops new uncovered code from shipping. The ratcheting `auto` project
baseline is what allowed the floor to erode from 90% to ~87% unnoticed (each merge
rebaselines to the prior commit, and 2% of slack compounds downward); it is replaced by
a hard 90% once the remediation coverage is banked.

## Reconciliation finding (2026-07)

A ~3-point gap was observed: the merged Cobertura report read **89.9%** while Codecov
reported **87%**. It is **not** a counting/denominator difference — it is a **stale
Codecov snapshot**.

The Jenkins `Codecov Upload` stage was gated on `currentBuild.currentResult == 'SUCCESS'`,
while all parallel test stages wrap their steps in `catchError(buildResult: 'FAILURE')`.
So any single flaky test anywhere in the build (e.g. the Redis SNTP flake, #199) flipped
the build to FAILURE and **silently skipped the upload** — leaving Codecov displaying
whatever the last fully-green build reported, lagging master by an unknown number of
merged PRs.

**Fix:** the upload stage is decoupled from the build result — it now runs whenever a
merged `Cobertura.xml` exists (an `if [ ! -f … ]` guard makes it a no-op otherwise).
Codecov is informational, not a build gate, so a flaky stage no longer suppresses it.

## Excluded from coverage

- **Test assemblies** — stripped at collection time by the Coverlet `Exclude` filter in
  `Source/Directory.Build.props`, so they never reach the report or Codecov.
- **`Source/DotNetWorkQueue.Dashboard.Ui/Program.cs`** — Blazor host bootstrap (DI registration
  and middleware pipeline). It is exercised only by the E2E suite, which does not collect
  coverage, and is an explicit non-goal to unit test.

  It is excluded via Coverlet's `ExcludeByFile`, scoped to exactly that one file:

  ```xml
  <ExcludeByFile>**/DotNetWorkQueue.Dashboard.Ui/Program.cs</ExcludeByFile>
  ```

  **Why not `[ExcludeFromCodeCoverage]`:** `Program.cs` uses top-level statements, so the
  generated `Program` class lives in the **global namespace**; the partial class needed to
  attach the attribute therefore cannot be namespaced, which trips `csharpsquid:S3903`
  ("move into a named namespace" — reported as a *bug*) and `S1118`. Neither is fixable
  without breaking the exclusion, so the file-glob approach avoids the construct entirely.

  **Why the path is scoped, not `**/Program.cs`:** a bare filename glob would also match any
  `Program.cs` added to another project later — silently dropping real code from coverage,
  which is exactly the erosion this policy exists to prevent. Excluding anything further is a
  deliberate decision that belongs in this file, not a side effect of a filename.

## Maintainer verification checklist (Codecov UI only)

Codecov data cannot be exported; confirm health from the UI:

1. Compare Codecov's latest **processed commit SHA** against master `HEAD` — they should
   be at or very near the same commit.
2. Confirm **exactly one unflagged upload** per commit (the pipeline uploads a single
   merged report, no flags).
3. Scan recent master commits for any with **no coverage data** — those are skipped
   uploads and should stop appearing now that the gate is removed.
4. Spot-check the **file tree**: 12 product packages present, **no `*.Tests` packages**
   (they are Exclude-stripped before upload).
5. Confirm the **Flags** page is empty (uploads are unflagged by design).
