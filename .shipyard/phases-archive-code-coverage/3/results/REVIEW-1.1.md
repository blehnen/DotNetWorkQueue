# Review: Plan 1.1

## Verdict: PASS

## Stage 1 -- Correctness

### Task 1: Refactor handler to inject IDbConnectionFactory -- PASS

Evidence from `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs`:

- **Constructor (lines 41-49):** Takes `SqlServerCommandStringCache` + `IDbConnectionFactory dbConnectionFactory`. `IConnectionInformation` removed. Null guards present for both parameters via `Guard.NotNull`.
- **Handle signature (line 54):** Still `ICommandHandler<SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>>` -- generic type params unchanged as required.
- **Connection creation (line 56):** `using (var conn = _dbConnectionFactory.Create())` replaces `new SqlConnection(...)`. `var` correctly binds to `IDbConnection` (the factory's declared return type); no cast needed because `Open()`, `CreateCommand()`, and `Dispose()` are all on `IDbConnection`.
- **Parameter mapping (lines 63-79):** All three parameters use the generic `commandSql.CreateParameter()` + `Parameters.Add(param)` pattern:
  - `@JobName` -> `DbType.AnsiString` (correct semantic equivalent of `SqlDbType.VarChar` -- non-Unicode)
  - `@JobEventTime` -> `DbType.DateTimeOffset` (correct; the command field is a DateTimeOffset, no string conversion)
  - `@JobScheduledTime` -> `DbType.DateTimeOffset` (same)
  - Values assigned directly from `command.JobName`, `command.JobEventTime`, `command.JobScheduledTime` -- no semantic change.
- **ExecuteNonQuery() call (line 81):** Preserved inside the command `using` scope.

### Behavior Preservation

- `DbConnectionFactory.Create()` in `Source/DotNetWorkQueue.Transport.SqlServer/Basic/DbConnectionFactory.cs:46-49` returns `new SqlConnection(_connectionInformation.ConnectionString)` cast as `IDbConnection`. The runtime connection type is still `SqlConnection`, so SQL Server-specific provider behavior (TDS protocol, parameterization, DateTimeOffset binding) is unchanged.
- `DbType.AnsiString` maps to `SqlDbType.VarChar` under `Microsoft.Data.SqlClient`'s type inference -- confirmed equivalent.
- `DbType.DateTimeOffset` maps to `SqlDbType.DateTimeOffset` -- identical wire format.
- Command handler's `Handle()` previously ignored `command.Connection`/`command.Transaction` and created its own connection; that behavior is preserved, just routed through the factory.

## Stage 2 -- Integration

### DI Registration -- PASS
- `IDbConnectionFactory` is registered in `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs:67`:
  `container.Register<IDbConnectionFactory, DbConnectionFactory>(LifeStyles.Singleton);`
- `SetJobLastKnownEventCommandHandler` is not explicitly registered -- it is discovered by SimpleInjector's automatic `ICommandHandler<T>` scan, so no registration change is needed. The new constructor dependency resolves automatically.

### Build Verification
- Build commands not re-executed in the reviewer sandbox, but the builder reported clean `dotnet build` on both the SqlServer csproj and the full solution. The code inspection confirms:
  - All referenced types (`IDbConnectionFactory`, `DbType`, `Guard`, `SqlServerCommandStringCache`) have correct `using` directives.
  - `Microsoft.Data.SqlClient` using is retained for the generic type parameters.
  - No stray references to `_connectionInformation` or `new SqlConnection(`.

## Findings

### Critical
None.

### Minor
None.

### Positive
- Clean factory injection consistent with the SQLite handler's pattern.
- Null-guard hygiene preserved.
- Handle signature intentionally left unchanged -- no ripple effect on the command dispatcher or command type definition.
- The `Microsoft.Data.SqlClient` using is kept only for the generic type parameters, not for connection construction -- clear separation of concerns.
- `DbType.AnsiString` chosen over `DbType.String` correctly preserves the `VarChar` (non-Unicode) semantics of the original column type, avoiding a subtle collation/index performance regression.

## Summary
**Verdict:** APPROVE
The refactor is a textbook dependency-inversion improvement with zero behavioral delta. Connection-string plumbing is now testable via a mockable factory.
Critical: 0 | Minor: 0 | Suggestions: 0
