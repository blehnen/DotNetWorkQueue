## Current Task
Jenkins CI Migration — project definition and roadmap complete, ready for Phase 1 planning.

## Approach
Migrate CI from TeamCity (3 agents, ~2hr builds) to Jenkins with 6 Docker agents on Linux (~63 min target). Five phases:
1. Multi-target 22 test .csproj files (add net10.0 alongside net48)
2. Add Coverlet for code coverage (replacing dotCover)
3. Create Docker agent image + Jenkinsfile with 6 parallel integration test stages
4. Jenkins master setup guide + GitHub Actions update (net48 unit tests only)
5. End-to-end validation

Key decisions already made:
- Jenkins runs net10.0 only; GitHub Actions keeps net48 unit tests for framework compat
- Code coverage: Coverlet (Cobertura format) → Codecov.io + ReportGenerator HTML
- Connection strings injected via Jenkins Credentials (Secret Text), written to connectionstring.txt files in pipeline
- Redis connection string is hardcoded in source (192.168.0.2) — acceptable for now
- Docker hosts: 192.168.0.75 (4 agents) + 192.168.0.2 (2 agents)
- Test services (SQL Server, PostgreSQL, Redis) at 192.168.0.2
- Agent balancing: SqlServerLinq+Redis(58m), SqlServer+Dashboard(54m), SQLiteLinq+Memory(51m), PostgreSQL+MemoryLinq(54m), PostgreSQLLinq+LiteDB+UnitTests(56m), SQLite+LiteDBLinq+RedisLinq(63m)

## Tried
- Reviewed all 24 test projects: 21 target net48-only, 3 Dashboard projects already multi-target
- Confirmed only 5 test files have `#if NETFULL` — all already guarded, no code changes needed
- Identified `System.Runtime.Serialization.Formatters.Soap` in DotNetWorkQueue.Tests.csproj as a blocker (net48-only, needs conditional reference)
- `.gitignore` already covers connectionstring.txt patterns
- Also in this session: fixed .gitattributes line ending issue (PR #85 merged), converted 2 UTF-16 GlobalSuppressions.cs to UTF-8, updated CONCERNS.md (22/30 items now resolved)

## Remaining
1. `/shipyard:plan 1` — Plan Phase 1 (multi-target test projects)
2. Execute Phase 1: multi-target IntegrationTests.Shared first (all 12 integration test projects depend on it), then unit test projects, then integration test projects
3. Execute Phase 2: add coverlet.collector to all test projects
4. Execute Phase 3: create docker/Dockerfile and Jenkinsfile
5. Execute Phase 4: write Jenkins setup guide, update GitHub Actions
6. Execute Phase 5: end-to-end validation on real Jenkins

## Open Questions
- User is not familiar with Jenkins — will need guided setup assistance in Phase 4
- AppMetrics.Tests project may have been removed (architect couldn't find it) — verify during Phase 1
- Actual timing on Docker agents may differ from TeamCity estimates — Phase 5 handles rebalancing
