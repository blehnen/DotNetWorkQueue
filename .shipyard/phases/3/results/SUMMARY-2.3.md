# Build Summary: Plan 2.3 -- SqliteJobSchema Tests

## Status: complete

## Files Modified
- `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteJobSchemaTests.cs` (NEW)

## Test Cases Added (4)
1. `GetSchema_ReturnsExactlyOneTable`
2. `GetSchema_TableHasExpectedColumns` -- JobEventTime (Text 35, NOT NULL), JobScheduledTime (Text 35, NOT NULL), JobName (Text 255, NOT NULL)
3. `GetSchema_TableHasPrimaryKey` -- PK named `PK_{JobTableName}`, Unique=true, on "JobName"
4. `GetSchema_TableNameMatchesHelper`

## Key Findings
- SQLite job table uses `ColumnTypes.Text` (not SqlServer's `Datetimeoffset` / PostgreSQL's `Bigint`) -- stores ISO 8601 strings via the `o` format specifier
- SQLite primary key has no `Clustered` flag (unlike SqlServer)
- The schema casts `ITable` back to concrete `Table` to access `Columns.Items` and `PrimaryKey`

## Decisions Made
- `ITableNameHelper` mocked with NSubstitute (lives in `DotNetWorkQueue.Transport.RelationalDatabase.Basic` -- builder added the missing using on first compile failure)
- No null-guard tests (constructor has no `Guard.NotNull`)

## Verification Results
- 4/4 tests pass

## Commit
`341126f4 shipyard(phase-3): add SqliteJobSchema tests`
