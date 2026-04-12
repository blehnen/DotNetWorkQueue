# Combined Review: Plans 2.7, 2.8

## Verdict: PASS

Both plans are spec-compliant and the PostgreSQL re-refactor is semantically sound. No critical or important issues. A few minor suggestions noted.

---

## Plan 2.7 — SqlServer SetJobLastKnownEventCommandHandler Tests

### Stage 1: Spec Compliance — PASS

Test file: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs`

| Required coverage | Status | Evidence |
|---|---|---|
| Ctor null-guard: commandCache | PASS | `Constructor_NullCommandCache_Throws` uses `Assert.ThrowsExactly<ArgumentNullException>` (lines 37-43) |
| Ctor null-guard: dbConnectionFactory | PASS | `Constructor_NullDbConnectionFactory_Throws` (lines 45-51) |
| Ctor happy path | PASS | `Constructor_ValidArgs_Succeeds` (lines 53-60) |
| Connection lifecycle (Open + Dispose) | PASS | `Handle_HappyPath_OpensConnectionAndExecutes` asserts Open/ExecuteNonQuery/Dispose on both connection and command (lines 73-76) |
| Command text from cache | PASS | `Handle_SetsCommandText_FromCache` reads expected SQL from `commandCache.GetCommand(CommandStringTypes.SetJobLastKnownEvent)` and asserts CommandText setter received it (lines 79-92) |
| Three parameters added | PASS | `Handle_SetsParameters_AddsThreeToCollection` asserts `parameters.Received(3).Add` and `CreateParameter` called 3x (lines 94-107) |
| Parameter names/types/values | PASS | `Handle_SetsParameters_NamesTypesAndValues` asserts `@JobName`/`AnsiString`, `@JobEventTime`/`DateTimeOffset`, `@JobScheduledTime`/`DateTimeOffset` with exact values (lines 109-148) |
| MSTest 3.x `ThrowsExactly` | PASS | Both null-guard tests use `Assert.ThrowsExactly<ArgumentNullException>` |

7/7 tests pass per SUMMARY-2.7.md.

### Stage 2: Code Quality

No critical or important findings.

**Suggestions:**
- `Handle_SetsParameters_NamesTypesAndValues` duplicates the mock-wiring boilerplate that `CreateMockedFactory()` already encapsulates. Could be DRY'd by extending the helper to accept pre-sequenced parameter substitutes (minor — readability is still fine as-is).

---

## Plan 2.8 — PostgreSQL SetJobLastKnownEventCommandHandler Tests (incl. re-refactor)

### Re-Refactor Review (commit `9c77537d`)

Handler: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs`

Verified the handler:
- Removed the `(NpgsqlConnection)` cast. `_dbConnectionFactory.Create()` now returns `IDbConnection` directly (line 51).
- Uses `commandSql.CreateParameter()` + `IDbDataParameter` (lines 58, 64, 70) — fully interface-based, matches the SqlServer pattern.
- Uses `DbType.AnsiString` for `@JobName` and `DbType.Int64` for the two time params (lines 60, 66, 72).
- **Semantic equivalence check (critical concern): CONFIRMED SAFE.** The reviewer's concern was whether `DbType.Int64` is semantically equivalent to the prior `NpgsqlDbType.Bigint`. Verified via `PostgreSQLJobSchema.cs` lines 62-63: `JobEventTime` and `JobScheduledTime` are declared as `ColumnTypes.Bigint`, and the handler writes `command.JobEventTime.UtcDateTime.Ticks` (a `long`). `DbType.Int64` maps to Npgsql `bigint` — this is the canonical ADO.NET-to-Npgsql mapping and preserves the exact wire behavior of the prior `NpgsqlDbType.Bigint` binding. `DbType.AnsiString` similarly maps to Npgsql `varchar`/`text` which is correct for the `@JobName` column.
- **Note:** This handler previously (pre-Wave-1) likely wrote DateTimeOffset Ticks as `long` — unchanged by the re-refactor. The SqlServer handler differs (`DbType.DateTimeOffset`) because the SqlServer schema stores these as `DateTimeOffset`, while PostgreSQL stores them as `Bigint`. The divergence is pre-existing and correct.
- Guard clauses preserved (lines 42-43); IoC resolution unaffected since ctor signature matches prior Wave 1 shape.

### Stage 1: Spec Compliance — PASS

Test file: `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs`

| Required coverage | Status | Evidence |
|---|---|---|
| Ctor null-guard: commandCache | PASS | `Constructor_NullCommandCache_Throws` (lines 37-43), `Assert.ThrowsExactly<ArgumentNullException>` |
| Ctor null-guard: dbConnectionFactory | PASS | `Constructor_NullDbConnectionFactory_Throws` (lines 45-51) |
| Ctor happy path | PASS | `Constructor_ValidArgs_Succeeds` (lines 53-60) |
| Handle lifecycle (Create/Open/Execute) | PASS | `Handle_HappyPath_OpensConnectionAndExecutes` asserts factory Create, connection Open, command ExecuteNonQuery each received once (lines 62-72) |
| Command text from cache | PASS | `Handle_SetsCommandText_FromCache` reads expected SQL from cache and asserts on the command's CommandText (lines 74-83) |
| Parameter names/types/values | PASS | `Handle_SetsParameters_NamesTypesAndValues` asserts 3 params, names, DbType, and values including `UtcDateTime.Ticks` conversion for the time fields (lines 85-110) |
| MSTest 3.x `ThrowsExactly` | PASS | Used in both null-guard tests |
| Count tally matches summary | PASS | 6 tests in file, 6/6 pass per SUMMARY-2.8.md |

### Stage 2: Code Quality

No critical or important findings.

**Suggestions:**
- The test does not explicitly assert `connection.Dispose()` / `command.Dispose()` (Plan 2.7 does). Since `using` blocks in the handler guarantee disposal, the omission is low-risk, but adding `Received(1).Dispose()` would bring symmetry with the SqlServer tests.
- `HandleFixture` uses `System.Collections.Generic.List<...>` fully qualified — adding `using System.Collections.Generic;` would tidy the declaration.
- The `@JobName` value check at line 101 could additionally verify it is not being Unicode-upcasted — currently adequate since `DbType.AnsiString` enforcement is asserted directly.

---

## Summary

**Verdict:** APPROVE

Both plans meet their spec. The PostgreSQL re-refactor is well-justified (sealed-type testability) and semantically preserves prior behavior — verified against `PostgreSQLJobSchema` which declares the relevant columns as `Bigint`, confirming `DbType.Int64` is the correct ADO.NET equivalent of the prior `NpgsqlDbType.Bigint`. MSTest 3.x `Assert.ThrowsExactly` used correctly throughout. Mock wiring is clean and asserts meaningful behavior rather than implementation details.

Critical: 0 | Important: 0 | Suggestions: 4
