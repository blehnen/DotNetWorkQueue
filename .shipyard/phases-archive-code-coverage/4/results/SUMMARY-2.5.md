# Build Summary: Plan 2.5 -- Expand LiteDb DashboardUpdateMessageBodyCommandHandler Tests

## Status: complete

## Files Modified
- `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/DashboardUpdateMessageBodyCommandHandlerTests.cs` (EXPANDED)

## Existing Tests (unchanged)
- `Create_Default`
- `Create_NullConnectionManager_Throws`
- `Create_NullTableNameHelper_Throws`

## New Tests Added (6)
1. `Handle_MessageNotFound_ReturnsZero` -- id 999 against empty collection -> 0
2. `Handle_EmptyQueue_ReturnsZero` -- same branch, id 1
3. `Handle_ExistingMessage_UpdatesBodyAndHeadersAndReturnsOne` -- happy path with re-read assertion
4. `Handle_UpdatesOnlyTargetedMessage` -- two rows, only first updated, second untouched
5. `Handle_ExistingMessage_WithNullBodyAndHeaders_WritesNulls` -- null payload edge case
6. `Handle_NonNumericMessageId_Throws` -- `int.Parse` failure path (FormatException)

## Branches Now Covered
- `int.Parse` success and failure
- `FindById` returning null -> `return 0`
- `FindById` returning non-null -> `Update` -> `return 1`
- All executable lines of `Handle` exercised, lifting coverage from 40.9% baseline to ~100%

## Decisions Made
- Pattern mirrors `DashboardResetAllStaleMessagesCommandHandlerTests` (real `LiteDbConnectionManager` + `LiteDbConnectionInformation` + `TableNameHelper` backed by a temp-path LiteDB file)
- `ICreationScope` stubbed with NSubstitute
- Cleanup in `finally` blocks

## Verification
- 9/9 tests pass (3 original + 6 new, 261 ms)

## Commit
`9cbbc714 shipyard(phase-4): expand LiteDb DashboardUpdateMessageBodyCommandHandler test coverage`
