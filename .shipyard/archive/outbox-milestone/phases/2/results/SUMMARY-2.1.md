# Build Summary: Plan 2.1 (Wave 2 — Validator + Extractor)

## Status: complete

## Tasks Completed

- Task 1: Create `IExternalDbNameExtractor` interface — complete — `Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs` (NEW, 45 lines; LGPL header + interface with one `string Extract(DbConnection connection)` method; both type + method XML-doc'd).
- Task 2: Create `ExternalTransactionValidator` sealed class — complete — `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` (NEW, 98 lines; LGPL header; constructor `Guard.NotNull` on both injected dependencies; `Validate(DbTransaction)` runs the 4 checks in plan-documented order with the plan-documented exception types).
- Task 3: Add 5 unit tests for `ExternalTransactionValidator` — complete — `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/ExternalTransactionValidatorTests.cs` (NEW, 94 lines; LGPL header; 5 `[TestMethod]` per plan; MSTest 4.x `Assert.ThrowsExactly<T>`; NSubstitute on `DbTransaction` + `DbConnection` abstract bases per CLAUDE.md mocking lesson).

## Commits

| SHA | Task | Subject |
|-----|------|---------|
| `1e2d6913` | 1 | `shipyard(phase-2): add IExternalDbNameExtractor interface` |
| `a8f96ae5` | 2 | `shipyard(phase-2): add ExternalTransactionValidator standalone class` |
| `7f55500d` | 3 | `shipyard(phase-2): add ExternalTransactionValidator unit tests` |

Note: between Task 1 commit (`1e2d6913`) and Task 2 commit (`a8f96ae5`), the parallel Wave 2 plan PLAN-2.2 landed two commits (`ab193876` `RelationalSendMessageCommand`, `081c104d` `IRelationalProducerQueue`). My Task 2 commit retried after a transient HEAD-ref race and applied cleanly on the advanced master. No content overlap between the two plans — disjoint files as designed.

## Files Modified

- `Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs` — NEW
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` — NEW
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/ExternalTransactionValidatorTests.cs` — NEW

## Decisions Made

- All 3 file contents matched plan wording verbatim. No deviations.
- Task 1's commit was applied with Task 2's file present in working tree but unstaged. Rationale: Task 1's XML doc contains `<see cref="Basic.ExternalTransactionValidator"/>`, which requires Task 2's type to exist for the Release build to succeed (otherwise CS1574 with `TreatWarningsAsErrors` fails the build). Each commit remained atomic (one file each); the working-tree-carries-both pattern only affects the intermediate state, not the commit content. Final tree is consistent at HEAD.

## Issues Encountered

- **CS1574 on initial isolated Task 1 build**: Building only Task 1's interface (before Task 2's validator existed) fails the Release build because the interface's XML `<see cref="Basic.ExternalTransactionValidator"/>` cannot resolve. Resolved by creating Task 2's file before running the Release-build verify.
- **Transient HEAD-ref race on Task 2 commit**: First `git commit` for Task 2 failed with `fatal: cannot lock ref 'HEAD'`. Parallel Wave 2 plan PLAN-2.2 had advanced master mid-commit. Retried immediately and succeeded. No content conflict.
- Pre-existing NU1902 warnings on `OpenTelemetry.Api 1.15.2` (out of scope).
- WSL LF→CRLF git warnings (cosmetic).

## Verification Results

| Gate | Result |
|------|--------|
| `test -f` Task 1 file | exit 0 |
| `test -f` Task 2 file | exit 0 |
| `test -f` Task 3 file | exit 0 |
| `Transport.RelationalDatabase` Release build (`TreatWarningsAsErrors` + XML doc, net10.0 + net8.0) | 0 errors, 11 warnings (all NU1902 pre-existing OpenTelemetry advisory) |
| Filtered validator tests (`ExternalTransactionValidatorTests`) | 5 passed, 0 failed, 0 skipped |
| Full `RelationalDatabase.Tests` suite (regression gate) | 221 passed, 0 failed (baseline was 216; +5 new tests, no regressions) |
| Layering grep `Microsoft.Data.SqlClient\|using Npgsql` over `Source/DotNetWorkQueue.Transport.RelationalDatabase/` | no matches |

## Wave 3 Hand-off

- `ExternalTransactionValidator` is constructible and unit-tested. Phase 3/4 will register it in transport-specific DI once per-provider `IExternalDbNameExtractor` implementations land.
- `IExternalDbNameExtractor` contract is final: one method `string Extract(DbConnection)`. Phase 3 SqlServer extractor uses `OrdinalIgnoreCase` semantics; Phase 4 PostgreSQL passes through.
- Validator uses `_connectionInfo.Container` as configured-DB accessor (transport-agnostic).
- The validator does NOT consume `IRetrySkippable` (Wave 1's marker). The two surfaces remain orthogonal: marker is for retry-decorator bypass; validator is for caller-tx safety. Wave 3 retry-decorator branches will read the marker; producer overrides in Phase 3/4 will call the validator.
