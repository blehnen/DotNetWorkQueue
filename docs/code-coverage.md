# Code Coverage Policy

DotNetWorkQueue targets a **90% line-coverage floor** across its product assemblies.
Both unit and integration tests count toward the number.

## Authoritative number: Codecov

**Codecov is the source of truth**, because it is what gates pull requests. The Jenkins
pipeline merges the 13 coverage-bearing stages with ReportGenerator into
`coverage/report/Cobertura.xml` and uploads that single file; test assemblies are already
stripped at collection time by the Coverlet `Exclude` filter in
`Source/Directory.Build.props`.

### Codecov and the ReportGenerator badge will never agree, by design

They differ by ~2.5 points, permanently, because they treat **partially covered lines**
differently:

| | partial lines | master (2026-07-21) |
|---|---|---|
| **Codecov** | counted separately, **excluded** from hits | **87.45%** (29,373 / 33,585) |
| ReportGenerator / Cobertura badge | counted **as covered** | **90.04%** (30,240 / 33,585) |

A "partial" is a line that executed but whose branches were not all taken. There were
**867** of them on master, which is the entire gap. Both numbers are correct measurements
of different things; Codecov's is the stricter one.

**Do not treat a difference between the README badge and Codecov as a bug.** Compare
Codecov to Codecov.

## The gate

| Check | Rule | Status |
|---|---|---|
| `patch` | new/changed lines in a PR must be **≥90%** covered | live, **required** |
| `project` | `auto` + **0.5% threshold**: coverage must not fall below the parent commit by more than the measurement noise floor | live, **required** |

The tight threshold is the anti-erosion mechanism. The original `auto` + **2%** allowed 2
points of slack per merge, and because `auto` rebaselines to the prior commit every time,
that slack compounded downward silently. `patch` grades only the lines a PR touches, so it
never punishes a PR for pre-existing debt.

**Why 0.5% and not 0%:** the measurement is not deterministic. Commit `78bf5839` was
observed reporting 29,373 hits and later 29,512 (+0.41 points) with no code change. Repeat uploads are unioned, and the integration suite covers slightly different paths run
to run. A 0% threshold converts that noise into spurious failures on a *required* check.
Real regressions are far larger (the Dashboard.Ui shortfall is ~2.5 points), so 0.5% still
catches what matters.

`project` hardens to a fixed **`target: 90%`** once Dashboard.Ui is covered (see below).

## Where the missing coverage actually is (2026-07)

The repo did **not** erode through many careless changes. Excluding one project from
today's numbers reproduces the historical figure exactly:

```
hits  29,373 − 307   = 29,066
lines 33,585 − 1,303 = 32,282   →  90.04%
```

`DotNetWorkQueue.Dashboard.Ui` is **1,303 lines at 23.6%**. Adding that Blazor project
without tests is the entire difference between the ~90.25% Codecov reported in 2024 and
the ~87.5% it reports now. Every other project is still ≈90%.

**Consequence for planning:** covering the transports or core more heavily moves the
Codecov number very little, because that code is already exercised by the integration
suite. Reaching 90% requires Dashboard.Ui specifically, roughly **+755 hits**, which is
Dashboard.Ui going from 23.6% to ~90%. A cheaper secondary lever is the 867 partial
lines: those are already-executed lines missing branch coverage, so an extra assertion
often converts one to a full hit.

## Excluded from coverage

- **Test assemblies**: stripped at collection time by the Coverlet `Exclude` filter in
  `Source/Directory.Build.props`, so they never reach the report or Codecov.
- **`Source/DotNetWorkQueue.Dashboard.Ui/Program.cs`**: Blazor host bootstrap (DI registration
  and middleware pipeline). It is exercised only by the E2E suite, which does not collect
  coverage, and is an explicit non-goal to unit test.

  It is excluded via Coverlet's `ExcludeByFile`, scoped to exactly that one file:

  ```xml
  <ExcludeByFile>**/DotNetWorkQueue.Dashboard.Ui/Program.cs</ExcludeByFile>
  ```

  **Why not `[ExcludeFromCodeCoverage]`:** `Program.cs` uses top-level statements, so the
  generated `Program` class lives in the **global namespace**; the partial class needed to
  attach the attribute therefore cannot be namespaced, which trips `csharpsquid:S3903`
  ("move into a named namespace", reported as a *bug*) and `S1118`. Neither is fixable
  without breaking the exclusion, so the file-glob approach avoids the construct entirely.

  **Why the path is scoped, not `**/Program.cs`:** a bare filename glob would also match any
  `Program.cs` added to another project later, silently dropping real code from coverage,
  which is exactly the erosion this policy exists to prevent. Excluding anything further is a
  deliberate decision that belongs in this file, not a side effect of a filename.

## Reading the numbers without a Codecov login

Codecov's public API answers most questions directly, no token required:

```bash
# master totals: note `hits`, `partials` and `lines` separately
curl -s "https://api.codecov.io/api/v2/github/blehnen/repos/DotNetWorkQueue/branches/master/" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['head_commit']['totals'])"

# per-file report (aggregate by Source/<project>/ to find where coverage sits)
curl -s "https://api.codecov.io/api/v2/github/blehnen/repos/DotNetWorkQueue/report/?branch=master"

# coverage over time
curl -s "https://api.codecov.io/api/v2/github/blehnen/repos/DotNetWorkQueue/coverage/?branch=master&interval=1d&start_date=2026-01-01"
```

## Health checks when a number looks wrong

1. **Compare like with like.** Badge (ReportGenerator) vs Codecov will differ by ~2.5
   points because of partials. This is not a defect.
2. **Check the processed commit.** Codecov's `head_commit.commitid` should be at or near
   master `HEAD`. If it lags, an upload was missed.
3. **Look for commits with no coverage data.** A build whose stages flaked can still
   upload a *partial* merge: ReportGenerator silently skips any missing stash
   (`Jenkinsfile`, the `try { unstash s } catch` loop), producing the **same denominator
   with fewer hits**. Symptom: a sudden multi-point drop with `lines` unchanged.
   Historic examples exist in the trend data (65% spikes in 2026-03).
4. **Spot-check the file tree**: 12 product packages, **no `*.Tests` packages** (they are
   Exclude-stripped before upload).
