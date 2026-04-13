---
phase: phase-3-coverage
plan: 1.2
wave: 1
dependencies: []
must_haves:
  - PostgreSQL SetJobLastKnownEventCommandHandler takes IDbConnectionFactory via constructor
  - Handler uses _dbConnectionFactory.Create() instead of new NpgsqlConnection(...)
  - DI registration updated so the container resolves IDbConnectionFactory for the handler
  - Existing PostgreSQL build succeeds with the refactored constructor
files_touched:
  - Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL/PostgreSqlMessageQueueInit.cs
tdd: false
risk: medium
---

# Plan 1.2 — Refactor PostgreSQL SetJobLastKnownEventCommandHandler to inject IDbConnectionFactory

## Context

Mirror of Plan 1.1 for the PostgreSQL transport. Handler currently does `new NpgsqlConnection(_connectionInformation.ConnectionString)` at line 51. Refactor to use `IDbConnectionFactory.Create()`.

Plans 1.1 and 1.2 touch disjoint projects and are safe to run in parallel.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs, Source/DotNetWorkQueue.Transport.PostgreSQL/PostgreSqlMessageQueueInit.cs" tdd="false">
  <action>
  1. Read `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs` in full.
  2. Replace the `IConnectionInformation`-based connection instantiation with an injected `IDbConnectionFactory dbConnectionFactory`. Add a `Guard.NotNull` null check. Store in a private readonly field.
  3. In `Handle`, replace `using (var connection = new NpgsqlConnection(_connectionInformation.ConnectionString))` with `using (var connection = _dbConnectionFactory.Create())`. Remove the `_connectionInformation` field if it is no longer used elsewhere in the file.
  4. Grep the PostgreSQL project for `SetJobLastKnownEventCommandHandler` registration. `IDbConnectionFactory` is already used by other PostgreSQL handlers; SimpleInjector auto-resolution should handle the swap without an explicit registration change. Confirm by reading the init file.
  5. Build and fix any compile errors.
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Debug</verify>
  <done>Build succeeds with zero errors. Grep for `new NpgsqlConnection` in `SetJobLastKnownEventCommandHandler.cs` returns zero hits. The handler's constructor signature includes `IDbConnectionFactory`.</done>
</task>
