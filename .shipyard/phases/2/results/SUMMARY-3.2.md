# Build Summary: Plan 3.2

## Status: complete

## Tasks Completed

- Task 1: Add `IRetrySkippable` bypass branch to PostgreSQL sync retry decorator — complete — `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs` (+1 using + 3-line early-return branch after `Guard.NotNull`).
- Task 2: Add `IRetrySkippable` bypass branch to PostgreSQL async retry decorator — complete — `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` (mirrored using `await ... .ConfigureAwait(false)` for the existing async style).
- Task 3: Add bypass-branch unit tests — complete — `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` (NEW; 1 `[TestClass]` + 2 `[TestMethod]`; uses `RelationalSendMessageCommand` + `Substitute.For<DbTransaction>()`; both assert `_ = policies.DidNotReceiveWithAnyArgs().Registry;`).

## Commits

| SHA | Task | Subject |
|-----|------|---------|
| `ed3bf73d` | 1 | `shipyard(phase-2): add IRetrySkippable bypass to PostgreSQL sync retry decorator` |
| `216d6ed2` | 2 | `shipyard(phase-2): add IRetrySkippable bypass to PostgreSQL async retry decorator` |
| `ae50ab22` | 3 | `shipyard(phase-2): add PostgreSQL retry-decorator bypass-branch unit tests` |

## Files Modified

- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs` — MODIFIED
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` — MODIFIED
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` — NEW

## Decisions Made

- None. All 3 edits followed plan wording verbatim.

## Issues Encountered

- **Phase-2 end-to-end verification gate (`dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true`) failed with one `NU1902` error on `Transport.SQLite` only.** This is **pre-existing** behavior: `git diff` of `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj` between Phase 2 HEAD (`86a16287`) and the Phase 1 complete commit (`99003720`) shows **no changes**. The OpenTelemetry `1.15.2` advisory `GHSA-g94r-2vxg-569j` was published before Phase 1 completed and trips the Transport.SQLite project's `TreatWarningsAsErrors` escalation pattern (other projects emit NU1902 as a warning; Transport.SQLite alone escalates). Not introduced by Phase 2; tracked separately as ISSUE-032 (see below).
- The Debug build of the full solution succeeds (0 errors). Per-project Release builds of `Transport.RelationalDatabase`, `Transport.SqlServer`, and `Transport.PostgreSQL` all succeed (0 errors each — all NU1902 emitted as warnings only).
- Pre-existing NU1902 warning noise on every build (consistent with Wave 1 / Wave 2 SUMMARYs).
- WSL LF→CRLF git warning on new test file (cosmetic).

## Verification Results

| Gate | Expected | Actual |
|---|---|---|
| grep `IRetrySkippable skippable` PostgreSQL sync decorator | 1 match | 1 match (line 54) |
| grep `IRetrySkippable skippable` PostgreSQL async decorator | 1 match | 1 match (line 55) |
| New test file exists | present | present |
| PostgreSQL main Release build (individual) | 0 errors | 0 errors, 14 NU1902 warnings (pre-existing) |
| Bypass tests (filter `~RetryCommandHandlerOutputDecoratorBypassTests`) | 2 passed | 2 passed, 0 failed |
| Debug build of `DotNetWorkQueueNoTests.sln` (no NU1902 escalation) | 0 errors | 0 errors, 53 NU1902 warnings (pre-existing) |
| `DotNetWorkQueueNoTests.sln` Release CI=true | 0 errors | 1 error (pre-existing NU1902 on Transport.SQLite csproj — identical to Phase 1 baseline) |
| Layering invariant on `Transport.RelationalDatabase/` | no matches | no matches |

## Phase 3 Hand-off

- PostgreSQL retry decorators (sync + async) now honor `IRetrySkippable.SkipRetry`. Together with PLAN-3.1's SqlServer counterpart, both relational transports bypass the Polly pipeline when `ExternalTransaction != null`.
- All Phase 2 vertical-slice deliverables now exist:
  1. `IRetrySkippable` marker (Wave 1)
  2. `SendMessageCommand.ExternalTransaction` property (Wave 1)
  3. `IExternalDbNameExtractor` interface (Wave 2)
  4. `ExternalTransactionValidator` sealed class + 5 unit tests (Wave 2)
  5. `RelationalSendMessageCommand` derived class (Wave 2)
  6. `IRelationalProducerQueue<T>` interface (Wave 2)
  7. `RelationalProducerQueue<T>` concrete with 4 protected virtual hooks (Wave 2)
  8. SqlServer retry decorator bypass branches + 2 unit tests (Wave 3 PLAN-3.1)
  9. PostgreSQL retry decorator bypass branches + 2 unit tests (Wave 3 PLAN-3.2)
- Phase 3 (SqlServer producer + handler fork) and Phase 4 (PostgreSQL producer + handler fork) have all the foundation types they need to derive from `RelationalProducerQueue<T>` and override the 4 virtual hooks.
