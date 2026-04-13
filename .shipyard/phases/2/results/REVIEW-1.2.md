# Review: Plan 1.2

## Verdict: PASS

## Stage 1 — Spec Compliance

### Task 1: GetJobIdQueryHandlerTests.cs
- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetJobIdQueryHandlerTests.cs` contains 5 `[TestMethod]` entries.
  - `Handle_ReaderHasRow_ReturnsReadColumnValue` — stubs `Reader.Read()` → `true, false`, stubs `ReadColumn.ReadAsType<long>(GetJobId, 0, reader)` → `42L`, asserts result and verifies `PrepareQuery.Handle(query, command, CommandStringTypes.GetJobId)` received once. Matches source line 59/64.
  - `Handle_ReaderHasNoRows_ReturnsDefault` — `Reader.Read()` → `false`, asserts `default(long)` and `ReadColumn.DidNotReceive().ReadAsType<long>(...)`. Matches source line 69 `return default`.
  - Three constructor null-guard tests cover all 3 ctor params (`prepareQuery`, `dbConnectionFactory`, `readColumn`) via `Assert.ThrowsExactly<ArgumentNullException>`. Matches `Guard.NotNull` calls at lines 42–44.
- IDb mocks inline via NSubstitute (`IDbConnection`, `IDbCommand`, `IDataReader`) wired through `Create() → CreateCommand() → ExecuteReader()`.

### Task 2: GetJobLastKnownEventQueryHandlerTests.cs
- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/QueryHandler/GetJobLastKnownEventQueryHandlerTests.cs` contains 5 `[TestMethod]` entries.
  - `Handle_ReaderHasRow_ReturnsReadColumnValue` — uses `DateTimeOffset(2026,4,12,...)`, stubs `ReadColumn.ReadAsDateTimeOffset(GetJobLastKnownEvent, 0, reader)`, asserts `PrepareQuery.Handle(query, command, CommandStringTypes.GetJobLastKnownEvent)` received. Matches source line 59/62.
  - `Handle_ReaderHasNoRows_ReturnsDefault` — asserts `default(DateTimeOffset)` and `ReadColumn.DidNotReceive().ReadAsDateTimeOffset(...)`. Matches ternary `reader.Read() ? ... : default(DateTimeOffset)`.
  - Three ctor null-guard tests on all 3 params matching `Guard.NotNull` at lines 43–45.
- Uses `GetJobLastKnownEventQuery` (non-generic) and `IPrepareQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>` — correctly distinguishes from Task 1.

### Differentiation check
The two files are NOT duplicates. Task 1 exercises `GetJobIdQueryHandler<long>` with `GetJobIdQuery<long>` and `ReadAsType<long>`; Task 2 exercises non-generic `GetJobLastKnownEventQueryHandler` with `GetJobLastKnownEventQuery` and `ReadAsDateTimeOffset`. `CommandStringTypes` enum values differ (`GetJobId` vs `GetJobLastKnownEvent`).

### Test Execution
Per SUMMARY-1.2.md: 10/10 pass, 0 warnings, 0 errors. Both handlers are simple pass-through code with no hidden branches; the covered paths (happy, empty, 3 null-guards) give full coverage of each handler. Deferred live `dotnet test` to the verifier stage — static inspection shows the NSubstitute wiring and assertions are valid and consistent with existing patterns in the test project.

## Stage 2 — Code Quality

### Critical
(none)

### Minor
- Both fixtures duplicate a ~25-line `CreateFixture()` + `TestFixture` nested class. Since they exercise two different handlers with different generic signatures, extraction would add generics gymnastics. Acceptable duplication.
  - Remediation: none required; leave as-is.
- `Handle_ReaderHasRow_ReturnsReadColumnValue` in `GetJobIdQueryHandlerTests` stubs `Reader.Read().Returns(true, false)`. The source only calls `Read()` once (it is inside an `if`, not a loop), so the second `false` value is dead. Not a defect, just slightly misleading.
  - Remediation: change to `Returns(true)` for clarity. Low priority.

### Positive
- Clean NSubstitute wiring of the full `IDbConnection → IDbCommand → IDataReader` chain — mirrors the existing test conventions in this project.
- Null-guard tests use `Assert.ThrowsExactly<ArgumentNullException>` (MSTest 3.x idiom) consistent with recent Phase 2 patches.
- Both files carry the LGPL-2.1 license header per project convention.
- Non-generic vs generic handler distinction is handled correctly (`GetJobIdQueryHandler<long>` vs plain `GetJobLastKnownEventQueryHandler`).
- Happy-path assertions verify not only the return value but also the interaction with `PrepareQuery.Handle` using exact `CommandStringTypes` — catches regressions if someone swaps the enum value.
- Empty-reader test uses `DidNotReceive()` to prove the `ReadColumn` short-circuit, which is the important behavioural guarantee.

Critical: 0 | Minor: 2 | Positive: 5
