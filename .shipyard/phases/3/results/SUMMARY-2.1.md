# Build Summary: Plan 2.1 -- SqlServerJobSchema Tests

## Status: complete

## Files Modified
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerJobSchemaTests.cs` (NEW)

## Test Cases Added (5)
1. `GetSchema_ReturnsExactlyOneTable`
2. `GetSchema_TableHasExpectedColumns` -- JobEventTime/JobScheduledTime (Datetimeoffset, NOT NULL), JobName (Varchar 255, NOT NULL)
3. `GetSchema_TableHasPrimaryKey` -- name `PK_{JobTableName}`, Clustered=true, Unique=true, on "JobName"
4. `GetSchema_TableNameMatchesHelper`
5. `GetSchema_TableOwnerMatchesSchema`

## Decisions Made
- `TableNameHelper` is concrete; constructed via `new TableNameHelper(connection)` with substituted `IConnectionInformation`
- Pattern matches existing `SqlServerMessageQueueSchemaTests` in the same test project
- No null-guard tests (constructor has no `Guard.NotNull` calls)

## Verification Results
- 5/5 tests pass (193 ms)

## Commit
`5481becc shipyard(phase-3): add SqlServerJobSchema tests`
