# Build Summary: Plan 1.1 (Phase 4 Wave 1 — PostgreSQL Foundation)

## Status: complete

## Tasks Completed

- Task 1: Create `PostgreSqlExternalDbNameExtractor` (pass-through; Decision 2) + 2 unit tests — complete — `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlExternalDbNameExtractor.cs` + `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlExternalDbNameExtractorTests.cs`. Extractor returns `connection.Database ?? string.Empty` verbatim — no normalization, case-sensitive per PG catalog semantics.
- Task 2: Create `PostgreSqlRelationalProducerQueue<T>` 11-param subclass (Rule C) + 6 producer unit tests including Risk #3 case-sensitive closure — complete — `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs` + `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalProducerQueueTests.cs`. Overrides 4 protected virtual hooks from Phase 2 base. Validator first, then `GuardNpgsqlTransaction` cast guard, then dispatch.
- Task 3: DI wiring in `PostgreSQLMessageQueueInit.cs` (Rule A: `RegisterConditional`) + capability-cast type-system test — complete — 5 registrations inserted between line 62 and line 64. Type-system check appended to producer test file.

## Commits

| SHA | Task | Subject |
|-----|------|---------|
| `4d04fc9a` | 1 | `shipyard(phase-4): add PostgreSqlExternalDbNameExtractor + tests` |
| `fd601045` | 2 | `shipyard(phase-4): add PostgreSqlRelationalProducerQueue<T> + producer tests` |
| `38d17f44` | 3 | `shipyard(phase-4): wire PostgreSQL outbox DI + capability-cast test` |

## Files Modified

- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlExternalDbNameExtractor.cs` — NEW
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs` — NEW
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` — MODIFIED (+12 lines: 5 registrations + RegisterConditional explanatory comment block + blank line)
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlExternalDbNameExtractorTests.cs` — NEW (2 [TestMethod])
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalProducerQueueTests.cs` — NEW (7 [TestMethod] — 6 producer + 1 capability-cast)

## Decisions Made

- **Followed all 3 CONTEXT-4 hard rules upfront, no rediscovery cycles:**
  - Rule A (`RegisterConditional`): all 3 open-generic producer mappings use `container.RegisterConditional(typeof(...), typeof(...), LifeStyles.Singleton)`. Avoided the SimpleInjector `EnableAutoVerification` issue that broke 6 SqlServer.Tests in Phase 3 Wave 1.
  - Rule C (11-param ctor): producer subclass constructor mirrors Phase 3 SqlServer's signature exactly.
- **Extractor returns pass-through verbatim** (Decision 2) — no `.ToUpperInvariant()`. The Risk #3 closure test verifies that `connection.Database = "mydb"` + `connInfo.Container = "MyDb"` produces an `InvalidOperationException` containing both names.
- **`GuardNpgsqlTransaction` cast** is the intentional fail-fast boundary. Phase 3's auditor confirmed this design pattern is safe given the producer subclass is the only public entry point for `RelationalSendMessageCommand` construction.
- Builder hit the turn budget after committing Tasks 1+2 and applying Task 3 DI changes but before committing Task 3. Orchestrator finished Task 3 directly: added the capability-cast type-system test, ran full PG.Tests regression suite (137/137 pass — 130 baseline + 7 new), Release build clean, then committed.

## Issues Encountered

- Builder turn-budget cutoff before Task 3 commit (same pattern as Phase 3 PLAN-1.1 builder). Orchestrator completed Task 3 directly without re-dispatching — net positive for elapsed time and context efficiency.
- Pre-existing 14 NU1902 OpenTelemetry advisory warnings (ISSUE-032) carried forward.

## Verification Results

| Gate | Expected | Actual |
|---|---|---|
| 4 source files exist (extractor + producer + extractor tests + producer tests) | present | OK |
| `PostgreSQLMessageQueueInit.cs` has 3 `PostgreSqlRelationalProducerQueue<>` registrations | 3 | 3 |
| `PostgreSQLMessageQueueInit.cs` has 1 `PostgreSqlExternalDbNameExtractor` registration | 1 | 1 |
| `PostgreSQLMessageQueueInit.cs` has 0 plain `container.Register(typeof(...ProducerQueue` matches (Rule A grep gate) | 0 | 0 |
| PostgreSQL main project Release build | 0 errors | 0 errors, 14 pre-existing NU1902 warnings |
| Full PG.Tests suite (regression gate) | Failed: 0 | 137 passed, 0 failed (baseline 130 + 7 new) |

## Wave 2 Hand-off

- `PostgreSqlRelationalProducerQueue<T>` constructs `RelationalSendMessageCommand` with caller's `NpgsqlTransaction` via the producer's overridden `SendOne`/`SendOneAsync` helpers. Wave 2 (PLAN-2.1 + PLAN-2.2) modifies the actual PG `SendMessageCommandHandler.cs` and `SendMessageCommandHandlerAsync.cs` to add the `HandleExternalTx`/`HandleExternalTxAsync` fork that branches when `command.ExternalTransaction != null`.
- Wave 2 forks MUST include `_getTime.GetCurrentUtcDate()` call before `CreateMetaDataRecord` (PG-specific 8th param — RESEARCH §1).
- Wave 2 forks MUST use `NpgsqlDbType.Bytea` for binary params (not `SqlDbType.VarBinary`).
- Wave 2 source-comment for lifecycle invariant MUST use exact wording from CONTEXT-4 Rule B: `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.`
- Wave 2 plans (PLAN-2.1 sync, PLAN-2.2 async) are file-disjoint and can run in parallel.
