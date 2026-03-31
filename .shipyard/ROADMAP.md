# Roadmap: Jenkins CI Migration

## Overview

Migrate CI from TeamCity to Jenkins to eliminate the agent bottleneck (3 agents, ~2 hour builds). Jenkins with 6 Docker agents on Linux will run integration tests in parallel, reducing wall-clock CI time to ~63 minutes. This requires multi-targeting the 21 net48-only test projects to also target net10.0 (so they can run on Linux), switching code coverage from dotCover to Coverlet, creating a Docker agent image, writing a Jenkinsfile pipeline, and setting up the Jenkins master.

**Current branch**: `bug_fixes`
**Target**: Full Jenkins pipeline operational with < 65 min wall-clock time

---

## Phase Summary

| Phase | Name | Plans | Dependencies | Risk | Estimated Scope |
|-------|------|-------|-------------|------|----------------|
| 1 | Multi-Target Test Projects | 3 | None | **High** -- touches 22 .csproj files; build breakage blocks everything | ~30% |
| 2 | Code Coverage Migration | 1 | Phase 1 | Low -- additive package references only | ~10% |
| 3 | Docker Agent Image + Jenkinsfile | 2 | Phase 1 | **High** -- untestable without Jenkins master; network/service access risks | ~30% |
| 4 | Jenkins Master Setup + GitHub Actions Update | 2 | Phase 3 | Medium -- manual setup; misconfiguration risk | ~20% |
| 5 | End-to-End Validation | 1 | Phase 4 | Medium -- first real pipeline run; timing/balancing adjustments | ~10% |

---

## Phase 1: Multi-Target Test Projects

**Goal**: All 22 test projects (21 net48-only + IntegrationTests.Shared library) build and pass on both `net10.0` and `net48`.

**Risk**: HIGH. This is the foundation for everything else. If the projects do not build on net10.0, nothing downstream works. The `System.Runtime.Serialization.Formatters.Soap` reference in `DotNetWorkQueue.Tests.csproj` is net48-only and will cause build failures on net10.0 if not conditioned. IntegrationTests.Shared must be multi-targeted first since all integration test projects depend on it.

**Why first**: Every subsequent phase depends on tests being runnable on Linux/net10.0. Fail fast here before investing in Docker/Jenkins infrastructure.

### Projects Requiring Changes

**Shared library (must go first -- all integration tests depend on it):**
1. `DotNetWorkQueue.IntegrationTests.Shared` -- currently `net48;` only, has `netstandard2.0` PropertyGroups already defined

**Unit test projects (10):**
2. `DotNetWorkQueue.Tests` -- has `System.Runtime.Serialization.Formatters.Soap` reference (net48-only)
3. `DotNetWorkQueue.Transport.SqlServer.Tests`
4. `DotNetWorkQueue.Transport.PostgreSQL.Tests`
5. `DotNetWorkQueue.Transport.Redis.Tests`
6. `DotNetWorkQueue.Transport.SQLite.Tests`
7. `DotNetWorkQueue.Transport.LiteDb.Tests`
8. `DotNetWorkQueue.Transport.RelationalDatabase.Tests`
9. `DotNetWorkQueue.Transport.Memory.Tests`
10. `DotNetWorkQueue.AppMetrics.Tests` (if it exists -- not found in Source; may have been removed)

**Integration test projects (12):**
11. `DotNetWorkQueue.Transport.SqlServer.IntegrationTests`
12. `DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests`
13. `DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests`
14. `DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests`
15. `DotNetWorkQueue.Transport.Redis.IntegrationTests`
16. `DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests`
17. `DotNetWorkQueue.Transport.SQLite.Integration.Tests`
18. `DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests`
19. `DotNetWorkQueue.Transport.LiteDB.IntegrationTests`
20. `DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests`
21. `DotNetWorkQueue.Transport.Memory.Integration.Tests`
22. `DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests`

**Already multi-targeted (no changes needed):**
- `DotNetWorkQueue.Dashboard.Api.Tests` -- `net10.0;net8.0`
- `DotNetWorkQueue.Dashboard.Api.Integration.Tests` -- `net10.0;net8.0`
- `DotNetWorkQueue.Dashboard.Client.Tests` -- `net10.0;net8.0`

### Known Obstacles

1. **`System.Runtime.Serialization.Formatters.Soap`** -- Referenced unconditionally in `DotNetWorkQueue.Tests.csproj`. This assembly does not exist on net10.0. Must be wrapped in `<ItemGroup Condition="'$(TargetFramework)' == 'net48'">`. Any test code that uses `SoapFormatter` must have `#if NETFULL` guards.

2. **`System.Net.Http` and `Microsoft.CSharp` framework references** -- Several test .csproj files have `<ItemGroup Condition="'$(TargetFramework)' == 'net48'">` blocks referencing these. These are already conditioned correctly and will not be an issue.

3. **68 files with `#if NETFULL`** -- These are already guarded and will compile correctly on net10.0. No code changes needed.

4. **IntegrationTests.Shared dependency chain** -- This project is referenced by all 12 integration test projects. It must be multi-targeted before any integration test project can be multi-targeted.

### Success Criteria

1. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds (builds all targets including net10.0)
2. `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" -f net10.0` passes all tests
3. `dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" -f net10.0` passes (verifiable without external services)
4. `dotnet test "Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" -f net10.0` passes
5. All existing net48 tests continue to pass on Windows (GitHub Actions CI green)

### Plans

- **Plan 1.1** (Wave 1): Multi-target IntegrationTests.Shared + core unit test projects (DotNetWorkQueue.Tests with Soap fix, RelationalDatabase.Tests, Memory.Tests)
- **Plan 1.2** (Wave 1): Multi-target transport-specific unit test projects (SqlServer.Tests, PostgreSQL.Tests, Redis.Tests, SQLite.Tests, LiteDb.Tests)
- **Plan 1.3** (Wave 2, depends on 1.1): Multi-target all 12 integration test projects

---

## Phase 2: Code Coverage Migration

**Goal**: Replace dotCover with Coverlet. All test projects produce Cobertura XML coverage output when run with `--collect:"XPlat Code Coverage"`.

**Risk**: LOW. Adding `coverlet.collector` is purely additive. No existing behavior changes. Central Package Management is already in place so the version goes in `Directory.Packages.props`.

**Why this ordering**: Coverlet must be in place before the Jenkinsfile can collect coverage. It is independent of Docker/Jenkins infrastructure and can be verified locally.

### Changes Required

1. Add `<PackageVersion Include="coverlet.collector" Version="6.0.4" />` to `Source/Directory.Packages.props`
2. Add `<PackageReference Include="coverlet.collector" />` to all test .csproj files (22 projects + 3 Dashboard test projects = 25 total, though the 3 Dashboard projects can be included for consistency)
3. Verify with: `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" -f net10.0 --collect:"XPlat Code Coverage" --results-directory ./coverage` produces a `coverage.cobertura.xml` file

### Success Criteria

1. `coverlet.collector` listed in `Directory.Packages.props`
2. All test .csproj files reference `coverlet.collector`
3. `dotnet test` with `--collect:"XPlat Code Coverage"` produces Cobertura XML for at least DotNetWorkQueue.Tests and one integration test project
4. Solution still builds and all tests pass

### Plans

- **Plan 2.1** (Wave 1): Add coverlet.collector to Directory.Packages.props and all test .csproj files; verify coverage output

---

## Phase 3: Docker Agent Image + Jenkinsfile

**Goal**: Create a Docker agent image that can build and test the solution on Linux, and a Jenkinsfile that orchestrates the full pipeline.

**Risk**: HIGH. The Docker image cannot be fully validated without running it against the test services at 192.168.0.2. The Jenkinsfile cannot be tested without the Jenkins master. However, both artifacts can be structurally validated (Dockerfile builds, Jenkinsfile parses).

**Why parallel with nothing**: Docker image and Jenkinsfile are independent of each other (the Jenkinsfile references the image by name, not by build). Both depend only on Phase 1 (knowing which test projects exist and their target frameworks).

### Docker Agent Image

**File**: `docker/Dockerfile`

Requirements:
- Base: Ubuntu 24.04 or mcr.microsoft.com/dotnet/sdk with both .NET 8 and .NET 10 SDKs
- Tools: git, curl, ssh client (for Jenkins agent connectivity)
- Network: must reach 192.168.0.2 (SQL Server 1433, PostgreSQL 5432, Redis 6379)
- No test services installed in the image -- they are external at 192.168.0.2

### Jenkinsfile Pipeline

**File**: `Jenkinsfile`

Stage structure:
```
Stage 1: Build + Unit Tests (gates everything, ~7 min)
  - dotnet restore + build (net10.0 only)
  - Run all unit test projects on net10.0

Stage 2: Parallel Integration Tests (6 agents, ~63 min max)
  Agent 1: SqlServer Linq + Redis                          (~58 min)
  Agent 2: SqlServer + Dashboard                           (~54 min)
  Agent 3: SQLite Linq + Memory                            (~51 min)
  Agent 4: PostgreSQL + Memory Linq                        (~54 min)
  Agent 5: PostgreSQL Linq + LiteDB + Unit Tests coverage  (~56 min)
  Agent 6: SQLite + LiteDB Linq + Redis Linq               (~63 min)

Stage 3: Coverage Merge + Upload (~2 min)
  - Merge Cobertura XML files from all agents
  - Upload to Codecov.io
  - Generate ReportGenerator HTML report
  - Archive HTML report as Jenkins artifact
```

### Connection String Handling

The Jenkinsfile must inject connection strings before tests run. Current patterns by transport:

| Transport | Pattern | File/Location | Jenkins Credential Needed |
|-----------|---------|---------------|--------------------------|
| SQL Server | Reads `connectionstring.txt` at runtime | Each SqlServer integration test project dir | Yes -- Secret Text |
| PostgreSQL | Reads `connectionstring.txt` at runtime | Each PostgreSQL integration test project dir | Yes -- Secret Text |
| Redis | Hardcoded `192.168.0.2` in `ConnectionString.cs` | Source code (not a secret file) | No -- but should be refactored to env var or file for consistency |
| SQLite | File-based DB, path generated in code | No external connection | No |
| LiteDB | In-memory or file-based, path generated in code | No external connection | No |
| Memory | In-process | No external connection | No |

Connection string files that need to be written by Jenkins pipeline:
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/connectionstring.txt`
- `Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/connectionstring.txt`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/connectionstring.txt`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/connectionstring.txt`

`.gitignore` already covers `connectionstring.txt` and `connectionstring-*.txt` patterns.

### Success Criteria

1. `docker build -t dotnetworkqueue-ci docker/` succeeds
2. Container can run `dotnet --list-sdks` and shows both .NET 8 and .NET 10 SDKs
3. `Jenkinsfile` passes `jenkins-cli declarative-linter` validation (or manual syntax review)
4. Jenkinsfile references correct test project paths and target frameworks
5. Connection string injection steps are present for SQL Server and PostgreSQL agents

### Plans

- **Plan 3.1** (Wave 1): Create Docker agent image (`docker/Dockerfile`) with .NET 8 + .NET 10 SDKs
- **Plan 3.2** (Wave 1): Write Jenkinsfile with build/test/coverage pipeline and connection string injection

---

## Phase 4: Jenkins Master Setup + GitHub Actions Update

**Goal**: Configure the Jenkins master at 192.168.0.2 with required plugins, Docker cloud agents, and credentials. Update GitHub Actions to focus on net48 unit tests only.

**Risk**: MEDIUM. Jenkins master setup is manual and environment-specific. Misconfigured Docker cloud or missing credentials will cause silent agent failures. GitHub Actions change is low risk (removing steps, not adding).

**Why this ordering**: Requires the Dockerfile and Jenkinsfile from Phase 3 to exist. The Jenkins master must be configured before the end-to-end validation in Phase 5.

### Jenkins Master Setup (Documentation-Guided)

This phase produces a setup guide document, not automation. The user follows the guide to configure Jenkins manually.

Required Jenkins plugins:
- Docker Pipeline (for Docker agent provisioning)
- Pipeline (declarative pipeline support)
- Cobertura (coverage report visualization)
- Credentials Binding (secret injection)
- HTML Publisher (ReportGenerator HTML artifacts)
- Codecov (optional -- upload can be done via curl in pipeline)

Docker cloud configuration:
- Host 1: `tcp://192.168.0.75:2375` -- 4 agent slots
- Host 2: `tcp://192.168.0.2:2375` -- 2 agent slots
- Image: `dotnetworkqueue-ci:latest` (built from `docker/Dockerfile`)

Credentials to create:
- `codecov-token` -- Secret Text -- Codecov.io upload token
- `sqlserver-connstring` -- Secret Text -- SQL Server connection string for integration tests
- `postgresql-connstring` -- Secret Text -- PostgreSQL connection string for integration tests

### GitHub Actions Update

Update `.github/workflows/ci.yml`:
- Keep: build step, all unit test steps (net48 target)
- Remove: Memory integration test steps, Dashboard API integration test steps
- Add: comment explaining that integration tests run on Jenkins
- Keep running on `windows-latest` for net48 validation

### Success Criteria

1. Jenkins master accessible at `http://192.168.0.2:8080`
2. Docker cloud shows 6 agent slots across 2 hosts
3. All credentials configured (codecov-token, sqlserver-connstring, postgresql-connstring)
4. GitHub Actions workflow runs only unit tests (no integration test steps)
5. GitHub Actions CI passes on the updated workflow

### Plans

- **Plan 4.1** (Wave 1): Write Jenkins master setup guide (`docs/jenkins-setup.md`) with step-by-step instructions
- **Plan 4.2** (Wave 1): Update `.github/workflows/ci.yml` to remove integration tests and add explanatory comments

---

## Phase 5: End-to-End Validation

**Goal**: Run the full Jenkins pipeline end-to-end and verify all success criteria from the project requirements.

**Risk**: MEDIUM. First real pipeline run will likely surface timing issues, network connectivity problems, or credential injection bugs. This phase exists specifically to catch and resolve those.

**Why last**: Requires everything from Phases 1-4 to be in place. This is the integration test for the CI system itself.

### Validation Checklist

1. Trigger Jenkins pipeline manually on the current branch
2. Verify Stage 1 (Build + Unit Tests) completes in < 10 min
3. Verify Stage 2 (Parallel Integration Tests) starts 6 agents
4. Verify each agent can connect to test services at 192.168.0.2
5. Verify connection strings are injected correctly for SQL Server and PostgreSQL
6. Verify all integration tests pass on net10.0
7. Verify Stage 3 produces merged Cobertura XML
8. Verify Codecov.io receives the upload (check badge URL)
9. Verify ReportGenerator HTML report is archived as Jenkins artifact
10. Verify total pipeline wall-clock time is under 65 minutes
11. Verify GitHub Actions still passes on the same commit (net48 unit tests)

### Success Criteria

1. Full pipeline completes green (all stages pass)
2. Codecov.io badge displays valid coverage percentage
3. ReportGenerator HTML report downloadable from Jenkins build artifacts
4. Wall-clock time under 65 minutes
5. GitHub Actions CI green on same commit

### Plans

- **Plan 5.1** (Wave 1): Execute full pipeline, document issues, iterate on fixes

---

## Dependency Graph

```
Phase 1: Multi-Target Test Projects
    |
    +---> Phase 2: Code Coverage Migration
    |         |
    +---> Phase 3: Docker + Jenkinsfile (parallel with Phase 2)
              |
              v
         Phase 4: Jenkins Master Setup + GH Actions Update
              |
              v
         Phase 5: End-to-End Validation
```

Phase 2 and Phase 3 can proceed in parallel once Phase 1 is complete. Phase 4 depends on Phase 3 (needs the Dockerfile and Jenkinsfile). Phase 5 depends on everything.

---

## Risk Assessment (Ordered by Impact)

1. **Phase 1 -- Multi-targeting breaks the build** (HIGH). The `System.Runtime.Serialization.Formatters.Soap` reference will cause net10.0 build failure if not conditioned. IntegrationTests.Shared dependency chain means one mistake cascades to 12 projects. Mitigation: multi-target incrementally, verify build after each batch.

2. **Phase 3 -- Docker agent cannot reach test services** (HIGH). Network connectivity between Docker containers and 192.168.0.2 services depends on Docker network configuration. If the agent runs on 192.168.0.75, it needs to reach 192.168.0.2 across the network. Mitigation: test connectivity from a container before writing the full pipeline.

3. **Phase 3 -- Redis connection string is hardcoded** (MEDIUM). The Redis integration tests have `192.168.0.2` hardcoded in `ConnectionString.cs`, not read from a file or environment variable. This works for the current setup but is fragile. Mitigation: acceptable for now since the IP matches; consider refactoring to env var in a future PR.

4. **Phase 4 -- Jenkins plugin compatibility** (MEDIUM). Plugin versions may conflict or require specific Jenkins versions. Mitigation: use LTS Jenkins and document exact plugin versions.

5. **Phase 5 -- Pipeline timing exceeds 65 min target** (MEDIUM). Agent balancing is estimated from TeamCity historical data. Actual timing on Docker agents may differ. Mitigation: Phase 5 exists specifically to measure and rebalance.

---

## Out of Scope

- Running net48 tests on Jenkins (no Windows Docker available)
- Replacing GitHub Actions entirely (it validates net48 compatibility)
- Migrating TeamCity job history or build statistics
- Changing test code or test logic (only .csproj targeting and coverage tooling)
- Refactoring Redis hardcoded connection string (works as-is for 192.168.0.2)
