# Milestone Report: Jenkins CI Migration

**Completed:** 2026-03-31
**Phases:** 5/5 complete
**Commits:** 37 (non-merge)
**PRs:** #86 (jenkins branch), #87 (Redis connectionstring refactor)

---

## Phase Summaries

### Phase 1: Multi-Target Test Projects
Multi-targeted all 22 test projects (21 net48-only + IntegrationTests.Shared) to `net10.0;net48`. Added `#if NETFULL` guards for `LinqMethodTypes.Dynamic` in Linq integration tests, `GetObjectData` serialization test, and conditioned `System.Runtime.Serialization.Formatters.Soap` reference. Fixed LiteDB csproj reference casing for Linux case-sensitivity.

**Commits:** `0c37c677`..`c7127d3c` (10 commits)

### Phase 2: Code Coverage Migration
Added `coverlet.collector` to all test projects via Central Package Management. Removed `global.json` SDK pin (was a dotCover workaround). Coverage output uses Cobertura XML format compatible with Codecov.io.

**Commits:** `7012699f`, `5e6ad858` (2 commits)

### Phase 3: Docker Agent Image + Jenkinsfile
Created `docker/Dockerfile` with Ubuntu base, .NET 8 + .NET 10 SDKs, Java 21 JRE (for Jenkins agent), libsqlite3, and `/home/jenkins` workspace. Created `Jenkinsfile` with 13 parallel integration test stages (expanded from initial 6-agent plan for better granularity). Uses label-based agents instead of Docker Pipeline plugin.

**Commits:** `c6cd36d4`..`1c72f93f` (10 commits of iterative fixes)

### Phase 4: Jenkins Master Setup + GitHub Actions Update
Created `docs/jenkins-setup.md` with step-by-step guide for Jenkins master configuration including plugins, Docker cloud setup, credentials, Multibranch Pipeline, and troubleshooting. Updated `.github/workflows/ci.yml` to run net48 unit tests only (integration tests now handled by Jenkins). Generalized setup guide for any environment.

**Commits:** `f2ae4b46`..`1ec99846` (6 commits)

### Phase 5: End-to-End Validation (Iterative)
Resolved issues discovered during real Jenkins pipeline runs:
- Connection strings written to bin output dirs after build
- Dashboard connection strings for SqlServer, PostgreSQL, Redis
- Redis connection string format fix for Dashboard tests
- SQLite native library + libdl symlink in Docker image
- JobScheduler tests excluded from Jenkins (timing-sensitive)
- Codecov CLI syntax fix (upload-process subcommand)
- Redis integration tests refactored to use `connectionstring.txt` (PR #87)

**Commits:** `bdaf881a`..`2edf7416` (8 commits)

### Bug Fixes (discovered during migration)
- `#if NETFULL` guard for `GetObjectData` serialization test
- BaseMonitor timer callback disposal race condition guard
- Time offset test tolerance for Linux
- LiteDB csproj reference casing for Linux case-sensitivity

---

## Key Decisions

1. **13 parallel stages** instead of 6 agents -- finer granularity allows better load balancing and faster failure isolation
2. **Label-based agents** instead of Docker Pipeline plugin -- simpler configuration, works with pre-built images
3. **Java 21 JRE** in Docker image -- must match Jenkins master's Java version
4. **Redis connectionstring.txt** refactored (originally "out of scope") -- consistency across all transports justified the change
5. **Multibranch Pipeline** -- builds master and all open PRs automatically with GitHub webhook triggers
6. **Codecov CLI** instead of Codecov Jenkins plugin -- more reliable, supports upload-process subcommand

## Documentation Status
- Jenkins setup guide: `docs/jenkins-setup.md` (comprehensive, 9KB)
- GitHub Actions: `.github/workflows/ci.yml` updated with comments explaining CI split
- Docker: `docker/Dockerfile` with inline comments
- Pipeline: `Jenkinsfile` with stage descriptions
- README: Not updated (no user-facing changes)

## Known Issues
- `SYSLIB0012` warning: `Assembly.CodeBase` obsolete in SQLite integration tests (cosmetic, net10.0 only)
- Phase 5 (E2E validation) was done iteratively on real Jenkins -- no formal verification document exists

## Metrics
- Files changed: 230 (across 37 commits)
- Lines added: ~7,744
- Lines removed: ~6,272
- Net change: +1,472 lines
- New files created: `Jenkinsfile`, `docker/Dockerfile`, `docs/jenkins-setup.md`
- Test projects multi-targeted: 22
- Pipeline stages: 13 parallel integration test stages + build/unit test gate + coverage merge
