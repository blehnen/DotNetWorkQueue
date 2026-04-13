# Build Summary: Plan 2.3 -- LiteDb GetJobIdQueryHandler Tests

## Status: complete

## Files Modified
- `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/QueryHandler/GetJobIdQueryHandlerTests.cs` (NEW)

## Tests Added (7)
1. `Create_Default`
2. `Create_Null_ConnectionInformation_Throws`
3. `Create_Null_TableNameHelper_Throws`
4. `Handle_JobExists_ReturnsJobId` (seeded QueueId=42)
5. `Handle_JobDoesNotExist_ReturnsZero` (default(int))
6. `Handle_MultipleJobs_ReturnsMatchingId` (3 rows, query middle)
7. `Handle_NonMatchingJobName_ReturnsZero`

## Decisions Made
- Constructor takes `LiteDbConnectionManager` + `TableNameHelper` (LiteDb-specific signature)
- Handle queries `StatusTable` by `JobName`, returns `QueueId` or `default(int)`
- Used `LiteDatabase("Filename=:memory:{guid};Mode=Memory")` -- per-test unique to avoid cross-test interference
- `LiteDbConnectionManager` constructed from substituted `IConnectionInformation` + `ICreationScope`
- Rows seeded directly via `db.Database.GetCollection<StatusTable>(tnh.StatusName)`

## Verification
- 7/7 tests pass (319 ms)

## Commit
`f36b6095 shipyard(phase-4): add LiteDb GetJobIdQueryHandler tests`
