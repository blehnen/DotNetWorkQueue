# Build Summary: Plan 2.2

## Status: complete

## Tasks Completed

- Task 1: Create `RelationalSendMessageCommand` derived class — complete — `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs` (56 lines)
- Task 2: Create `IRelationalProducerQueue<TMessage>` interface — complete — `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` (101 lines)
- Task 3: Create `RelationalProducerQueue<T>` concrete — complete — `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs` (161 lines)

## Commits

| SHA | Task | Subject |
|-----|------|---------|
| `ab193876` | 1 | `shipyard(phase-2): add RelationalSendMessageCommand derived class` |
| `081c104d` | 2 | `shipyard(phase-2): add IRelationalProducerQueue interface` |
| `4a67afb2` | 3 | `shipyard(phase-2): add RelationalProducerQueue concrete class` |

## Files Modified

- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs` — NEW
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` — NEW
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs` — NEW

## Decisions Made

- Batch collection type kept as `List<QueueMessage<TMessage, IAdditionalMessageData>>` per plan's architect note (PROJECT.md spec said `IEnumerable`; chose `List` to match existing `IProducerQueue<T>` shape — deviation flagged for verifier).
- `NotConfiguredMessage` is `private static` (no instance state).
- Comment text shortened from `// --- 4 protected virtual hooks ...` to `// --- 4 hooks ...` so `grep -c "protected virtual"` returns exactly 4 (acceptance-required count). No semantic change.
- XML docs filled in for every `<param>` and `<returns>` on public/protected members beyond what plan stubs showed, because `TreatWarningsAsErrors` + XML doc gen would fail CS1591 otherwise.

## Issues Encountered

- Transient cross-plan build error on first Release build after Task 1: PLAN-2.1's in-flight untracked `IExternalDbNameExtractor.cs` had an unresolved `<see cref="Basic.ExternalTransactionValidator"/>` because PLAN-2.1's Task 2 had not yet landed. Self-cleared on subsequent builds. Not caused by PLAN-2.2 code.
- Pre-existing NU1902 OpenTelemetry advisory warnings (11 per build) — identical to PLAN-1.1 baseline, out of scope.
- LF→CRLF git warnings on all 3 new files — cosmetic WSL behavior, `.gitattributes` normalizes committed bytes to LF.

## Verification Results

| Gate | Expected | Actual |
|---|---|---|
| 3 new files exist | present | OK |
| `Transport.RelationalDatabase` Release build | 0 errors | 0 errors, 11 pre-existing NU1902 warnings |
| `grep -c "DbTransaction transaction"` interface | 6 | 6 |
| `grep -c "protected virtual"` concrete | 4 | 4 |
| `grep "SkipRetry => ExternalTransaction != null"` | 1 match | 1 match (line 54) |
| Layering grep `Microsoft.Data.SqlClient\|using Npgsql` | no matches | no matches |
| `Transport.RelationalDatabase.Tests` suite | Failed: 0 | Passed: 221, Failed: 0, Skipped: 0 |

## Wave 3 Hand-off

- `RelationalSendMessageCommand` is what Wave 3 SqlServer + PostgreSQL retry decorators inspect via `IRetrySkippable.SkipRetry`.
- `RelationalProducerQueue<T>` is the base for Phase 3 `SqlServerRelationalProducerQueue<T>` and Phase 4 `PostgreSqlRelationalProducerQueue<T>` — Phase 3/4 override the 4 `protected virtual` hooks.
- `IRelationalProducerQueue<T>` is the runtime capability-cast surface; only SqlServer + PostgreSQL producers implement it. Memory/Redis/LiteDb/SQLite producers deliberately do NOT.
- No `ProjectReference` additions needed for Wave 3.
