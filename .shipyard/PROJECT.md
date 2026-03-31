# Project: Jenkins CI Migration

## Description

Migrate CI from TeamCity to Jenkins to eliminate the agent bottleneck (3 agents, ~2 hour builds). Jenkins with 6 Docker agents on Linux will run integration tests in parallel, reducing wall-clock CI time to ~63 minutes. This requires multi-targeting the 21 net48-only test projects to also target net10.0 (so they can run on Linux), switching code coverage from JetBrains dotCover to Coverlet, creating a Docker agent image, writing a Jenkinsfile pipeline, and setting up the Jenkins master.

Code coverage uploads to Codecov.io for badge display and PR-level coverage reporting on GitHub. GitHub Actions continues to run net48 unit tests for framework compatibility validation.

## Goals

1. Multi-target all 21 net48-only test projects to `net10.0;net48`
2. Switch code coverage tooling from dotCover to Coverlet (Cobertura format)
3. Create a Docker agent image with .NET 8 + .NET 10 SDKs
4. Write a Jenkinsfile with parallel integration test stages balanced across 6 agents
5. Set up Jenkins master with required plugins, Docker cloud agents, and credentials
6. Integrate Codecov.io upload into the Jenkins pipeline
7. Update GitHub Actions workflow to focus on net48 unit tests only
8. Generate ReportGenerator HTML reports as Jenkins artifacts

## Non-Goals

- Running net48 tests on Jenkins (no Windows Docker available)
- Replacing GitHub Actions entirely (it still validates net48 compatibility)
- Migrating TeamCity job history or build statistics
- Setting up Jenkins for other projects (this is DotNetWorkQueue-specific)
- Changing test code or test logic (only .csproj targeting and coverage tooling)

## Requirements

### Test Project Multi-Targeting
- Add `net10.0` to TargetFrameworks in all 21 test .csproj files that currently target only `net48`
- Keep `net48` in TargetFrameworks so GitHub Actions Windows runners can still build/test them
- 3 Dashboard test projects already target net10.0/net8.0 — no changes needed
- `#if NETFULL` blocks in 5 test files already handle conditional compilation — no code changes needed
- All test projects must build and pass on both net10.0 (Linux) and net48 (Windows)

### Code Coverage Migration
- Replace dotCover (JetBrains/TeamCity-specific) with Coverlet
- Add `coverlet.collector` NuGet package to all test projects
- Configure `dotnet test` to produce Cobertura XML output
- Each parallel agent produces its own coverage file
- Upload coverage files to Codecov.io (supports multiple uploads per commit SHA)
- Generate ReportGenerator HTML report as Jenkins build artifact

### Docker Agent Image
- Base image: Ubuntu or Debian with .NET 8 + .NET 10 SDKs
- Must be able to connect to test services at 192.168.0.2 (SQL Server, PostgreSQL, Redis)
- Include git, curl, and any other build tooling needed
- Dockerfile lives at `docker/Dockerfile`

### Jenkinsfile Pipeline
- Located at repo root: `Jenkinsfile`
- Stage 1: Build + Unit Tests (~7 min, gates everything)
- Stage 2: 6 parallel integration test branches:
  - Agent 1: SqlServer Linq + Redis (58 min)
  - Agent 2: SqlServer + Dashboard (54 min)
  - Agent 3: SQLite Linq + Memory (51 min)
  - Agent 4: PostgreSQL + Memory Linq (54 min)
  - Agent 5: PostgreSQL Linq + LiteDB + Unit Tests (56 min)
  - Agent 6: SQLite + LiteDB Linq + Redis Linq (63 min)
- Stage 3: Merge coverage + upload to Codecov + generate ReportGenerator HTML
- Pipeline targets net10.0 only on Jenkins

### Connection String Secret Management
- TeamCity currently injects connection strings via build parameters at build time, writing them to `connectionstring.txt` files before tests run
- These files contain secrets (database passwords, Redis auth) and must NOT be committed to source control
- Jenkins pipeline must replicate this pattern using Jenkins Credentials:
  - Store connection strings as Jenkins Secret Text or Secret File credentials
  - Pipeline step writes `connectionstring.txt` files into each integration test project directory before test execution
  - Files are created inside the Docker agent workspace (ephemeral — destroyed when container stops)
- Identify all `connectionstring.txt` file locations across integration test projects
- Ensure `.gitignore` covers `connectionstring.txt` patterns

### Jenkins Master Setup
- Jenkins master runs on 192.168.0.2
- Install required plugins: Docker Pipeline, Pipeline, Cobertura, Credentials
- Configure Docker cloud pointing to 192.168.0.75 (4 agents) and 192.168.0.2 (2 agents)
- Set up credentials: Codecov token, connection strings for each transport (SQL Server, PostgreSQL, Redis, SQLite, LiteDB), any Docker registry auth if needed
- Provide step-by-step setup guide as documentation

### GitHub Actions Update
- Update `.github/workflows/ci.yml` to run only net48 unit tests (no integration tests)
- Remove any steps that duplicate what Jenkins now handles
- Keep as a lightweight framework compatibility check

## Non-Functional Requirements

- CI wall-clock time target: under 65 minutes (down from ~2 hours)
- Coverage reports must be accessible via Codecov.io badges on GitHub
- Jenkins pipeline must fail fast — if build or unit tests fail, skip integration tests
- Docker agent image should be reasonably small (avoid unnecessary layers)
- Connection strings for test services must be configurable (not hardcoded)

## Success Criteria

1. All 21 test projects build on both net10.0 (Linux) and net48 (Windows)
2. `dotnet test` with Coverlet produces Cobertura coverage output for all test projects
3. Docker agent image builds and can run `dotnet test` against test services at 192.168.0.2
4. Jenkinsfile executes full pipeline: build → unit tests → 6 parallel integration stages → coverage upload
5. Codecov.io receives coverage data and badge displays on GitHub README
6. ReportGenerator HTML report available as Jenkins build artifact
7. GitHub Actions runs net48 unit tests only and passes
8. Total Jenkins pipeline wall-clock time is under 65 minutes

## Constraints

- No Windows Docker hosts available — net48 tests cannot run on Jenkins
- Test services (SQL Server, PostgreSQL, Redis) are at 192.168.0.2 — Docker agents must have network access
- Jenkins master is at 192.168.0.2 (same host as some test services and 2 Docker agents)
- Docker API already open on both hosts (192.168.0.2 and 192.168.0.75)
- Must preserve Codecov.io integration (badge + PR comments)
- TeamCity config at F:\Git\DotNetWorkQueue\TeamCity available for reference
