---
phase: phase-3-coverage
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - SqlServer SetJobLastKnownEventCommandHandler takes IDbConnectionFactory via constructor
  - Handler uses _dbConnectionFactory.Create() instead of new SqlConnection(...)
  - DI registration updated so the container resolves IDbConnectionFactory for the handler
  - Existing SqlServer build succeeds with the refactored constructor
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs
  - Source/DotNetWorkQueue.Transport.SqlServer/SqlServerMessageQueueInit.cs
tdd: false
risk: medium
---

# Plan 1.1 — Refactor SqlServer SetJobLastKnownEventCommandHandler to inject IDbConnectionFactory

## Context

`SetJobLastKnownEventCommandHandler` currently instantiates a concrete `SqlConnection` at line 56 using `_connectionInformation.ConnectionString`. This prevents unit testing. Refactor to inject `IDbConnectionFactory` (the relational transport abstraction already used elsewhere in this transport) and obtain the connection via `_dbConnectionFactory.Create()`.

DI registration for this handler lives in the SqlServer transport init (likely `SqlServerMessageQueueInit.cs` or a Job-specific init class). `IDbConnectionFactory` is already registered in the SqlServer transport because other handlers consume it; the refactor should be a constructor swap only.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs, Source/DotNetWorkQueue.Transport.SqlServer/SqlServerMessageQueueInit.cs" tdd="false">
  <action>
  1. Read `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs` in full.
  2. Replace the `IConnectionInformation` constructor parameter (or add alongside, whichever is used) with an `IDbConnectionFactory dbConnectionFactory` parameter. Add a null guard using `Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory)` matching project conventions. Store in a private readonly field.
  3. In `Handle`, replace `using (var connection = new SqlConnection(_connectionInformation.ConnectionString))` with `using (var connection = _dbConnectionFactory.Create())`. Remove the now-unused `_connectionInformation` field only if nothing else in the file consumes it.
  4. Locate the DI registration for `SetJobLastKnownEventCommandHandler` by grepping the SqlServer project for `SetJobLastKnownEventCommandHandler` and `ICommandHandlerWithOutput`. Update the registration so SimpleInjector can resolve `IDbConnectionFactory` automatically (if it already resolves other handlers with this dependency, no registration change is required — confirm by reading the init file).
  5. Build the SqlServer project and fix any compile errors.
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Debug</verify>
  <done>Build succeeds with zero errors. `SetJobLastKnownEventCommandHandler` no longer references `new SqlConnection(...)` anywhere. Grep for `new SqlConnection` in `SetJobLastKnownEventCommandHandler.cs` returns zero hits. The handler's constructor signature includes `IDbConnectionFactory`.</done>
</task>
