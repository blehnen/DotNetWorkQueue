# Phase 3 Plan Critique

## Verifier Output Status
The verifier agent inspected files but did not produce a written critique file. Verbal findings recovered from agent output are summarized below.

## Per-Plan Findings (orchestrator + partial verifier)

### Wave 1 - Refactors

#### PLAN-1.1 (SqlServer SetJobLastKnownEvent refactor)
- **File exists:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs` -- CONFIRMED (orchestrator read it earlier)
- **Hardcoded connection confirmed** at line 56: `new SqlConnection(_connectionInformation.ConnectionString)`
- **Risk: MEDIUM** -- DI registration update needed; builder must find and update the registration
- **Caution:** Builder should grep for `SetJobLastKnownEventCommandHandler` registrations to find all callsites

#### PLAN-1.2 (PostgreSQL SetJobLastKnownEvent refactor)
- **File exists:** `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs` -- CONFIRMED
- **Hardcoded connection confirmed** at line 51: `new NpgsqlConnection(_connectionInformation.ConnectionString)`
- **Risk: MEDIUM** -- same as Plan 1.1

### Wave 2 - Tests

#### PLAN-2.1 / 2.2 / 2.3 (JobSchema tests for SqlServer / PostgreSQL / SQLite)
- **SqlServerJobSchema confirmed:** Pure schema definition, takes `(TableNameHelper tableNameHelper, ISqlSchema schema)`. `GetSchema()` returns `List<ITable>`. Fully testable with NSubstitute.
- **PostgreSQLJobSchema and SqliteJobSchema:** Architect's plans say "READ THE SOURCE FIRST" -- builders will inspect before testing. Reasonable.
- **Risk: LOW** -- additive tests, no production code changes

#### PLAN-2.4 / 2.5 / 2.6 (SendJobToQueue tests)
- **SqlServerSendJobToQueue confirmed:** Inherits `ASendJobToQueue`, takes 6 injected deps, no hardcoded connections. Testable with NSubstitute.
- **PostgreSQLSendJobToQueue and SqliteSendToJobQueue:** Architect's plans say "READ THE SOURCE FIRST" -- builders will verify pattern.
- **Risk: LOW-MEDIUM** -- depends on whether these other transports follow the same pattern. If they hardcode connections like SetJobLastKnownEvent does, the plan will need to refactor first. Builders should fail-fast if they hit this.

#### PLAN-2.7 / 2.8 (SetJobLastKnownEvent tests, depend on Wave 1)
- **Wave dependency confirmed:** These tests target the refactored handlers (after IDbConnectionFactory injection)
- **AdoNetMockFixture in RelationalDatabase.Tests:** Architect's recommendation to NOT add cross-project reference is sound -- inline mocks are simpler. SQLite already has a similar test (`Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs`) that can be used as a template.
- **Risk: MEDIUM** -- success depends on Wave 1 refactor being correct

## Cross-Plan Concerns

1. **No file conflicts within Wave 2** -- each plan touches a different test file in a different transport project. Parallel-safe.

2. **Wave dependency 1.1 -> 2.7 and 1.2 -> 2.8** -- correctly captured in plan dependencies. Wave 2 plans 2.7/2.8 must wait for Wave 1.

3. **Plans 2.1-2.6 are independent of Wave 1** -- these don't touch the SetJobLastKnownEvent handlers. Could technically run in Wave 1 alongside refactors, but architect kept them in Wave 2 for simplicity.

## Verdict: **CAUTION**

Proceed with awareness:
- Builders for plans 2.4/2.5/2.6 must verify the SendJobToQueue handlers follow the testable pattern. If they don't (e.g., PostgreSQL hardcodes its own connection), the plan should fail-fast and report so we can refactor first or defer.
- Builders for plans 2.7/2.8 must use the SQLite SetJobLastKnownEventCommandHandlerTests as a structural reference.
- Phase 2 lockup pattern: builders may fail to commit/write summaries. Orchestrator should be prepared to commit and write summary files post-hoc.
