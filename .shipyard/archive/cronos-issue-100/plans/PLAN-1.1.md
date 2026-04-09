---
phase: transport-heartbeat-defaults
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - LiteDB heartbeat default changed from Schyntax to 6-field cron
  - Redis heartbeat default changed from Schyntax to 6-field cron
  - RelationalDatabase heartbeat default changed from Schyntax to 5-field cron
  - Solution builds cleanly
files_touched:
  - Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbMessageQueueInit.cs
  - Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalDatabaseMessageQueueInit.cs
tdd: false
---

# Plan 1.1: Transport Heartbeat Default Strings

Mechanical string replacements — change 3 Schyntax schedule strings to their cron equivalents. No logic changes.

## Context

Each transport's init class sets a default heartbeat schedule via `heartBeatConfiguration.UpdateTime`. These currently use Schyntax DSL format. After Phase 1 replaced Schyntax with Cronos in the core library, these strings must use cron format.

## Dependencies

None (Phase 1 already complete — Cronos is the parser now).

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbMessageQueueInit.cs, Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs, Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalDatabaseMessageQueueInit.cs" tdd="false">
  <action>
  Replace heartbeat default strings in 3 files:

  1. `Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbMessageQueueInit.cs` (line 330):
     - Change `"sec(*%10)"` to `"*/10 * * * * *"` (6-field cron: every 10 seconds)

  2. `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs` (line 321):
     - Change `"sec(*%10)"` to `"*/10 * * * * *"` (6-field cron: every 10 seconds)

  3. `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalDatabaseMessageQueueInit.cs` (line 144):
     - Change `"min(*%2)"` to `"*/2 * * * *"` (5-field cron: every 2 minutes)
  </action>
  <verify>grep -rn "sec(\*%\|min(\*%" Source/DotNetWorkQueue.Transport.LiteDB/ Source/DotNetWorkQueue.Transport.Redis/ Source/DotNetWorkQueue.Transport.RelationalDatabase/ --include="*.cs" | grep -v bin | grep -v obj; echo "Should be empty (0 matches)"</verify>
  <done>All 3 transport heartbeat defaults use cron format. No Schyntax schedule strings remain in transport init files.</done>
</task>

<task id="2" files="" tdd="false">
  <action>
  Build verification:
  1. `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug` — 0 errors
  2. `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release` — 0 errors, 0 warnings
  3. `grep -rn "sec(\*%\|min(\*%" Source/ --include="*.cs"` — should only match test files (Phase 3 scope), not transport init files
  </action>
  <verify>dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release --verbosity quiet 2>&1 | tail -3</verify>
  <done>Solution builds cleanly in both Debug and Release with cron heartbeat defaults.</done>
</task>

## Verification

```bash
# No Schyntax strings in transport init files
grep -rn 'sec(\*%\|min(\*%' Source/DotNetWorkQueue.Transport.*/Basic/*Init.cs
# Should return 0 matches

# Solution builds
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release
# 0 errors, 0 warnings
```
