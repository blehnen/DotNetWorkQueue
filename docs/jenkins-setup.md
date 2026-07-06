# Jenkins Master Setup Guide

This guide walks through setting up the Jenkins master to run the DotNetWorkQueue CI pipeline.

## Prerequisites

- Jenkins LTS installed
- One or more Docker hosts with the Docker daemon listening on TCP (port 2375)
- Test services accessible from the Docker hosts:
  - SQL Server on port 1433
  - PostgreSQL on port 5432
  - Redis on port 6379

## 1. Get the Docker Agent Image

The CI agent image is built and published to Docker Hub from the dedicated
[blehnen/dotnetworkqueue-ci](https://github.com/blehnen/dotnetworkqueue-ci)
repository (rebuilt weekly). On each Docker host, pull it:

```bash
docker pull blehnen74/dotnetworkqueue-ci:latest
```

Verify both SDKs are available:

```bash
docker run --rm blehnen74/dotnetworkqueue-ci:latest dotnet --list-sdks
```

You should see both .NET 8.x and .NET 10.x SDKs listed.

## 2. Install Jenkins Plugins

Go to **Manage Jenkins > Plugins > Available plugins** and install:

| Plugin | Purpose |
|--------|---------|
| **Docker Pipeline** | Provision Docker containers as build agents |
| **Docker plugin** | Docker cloud configuration for agent provisioning |
| **Pipeline** | Declarative pipeline support (usually pre-installed) |
| **HTML Publisher** | Publish ReportGenerator HTML coverage reports |
| **Credentials Binding** | Inject secrets into pipeline steps |
| **GitHub Branch Source** | Discover branches and PRs from GitHub repositories |
| **GitHub** | GitHub API integration and webhook support |

Restart Jenkins after installing plugins.

## 3. Configure Docker Cloud

Go to **Manage Jenkins > Clouds > New cloud > Docker**.

For each Docker host, create a cloud entry:

- **Name**: a descriptive name (e.g., `docker-host-1`)
- **Docker Host URI**: `tcp://<docker-host-ip>:2375`
- **Container Cap**: number of concurrent containers this host can run (depends on CPU/RAM)
- **Docker Agent Template**:
  - **Labels**: `docker`
  - **Docker Image**: `blehnen74/dotnetworkqueue-ci:latest`
  - **Pull strategy**: **Pull once** (or periodically, to pick up the weekly rebuild)
  - **Remote Filing System Root**: `/home/jenkins`
  - **Connect method**: Attach Docker container

All hosts should use the same `docker` label so the Jenkinsfile can request any available agent. If you have multiple hosts, list the preferred host first — Jenkins tries clouds top-to-bottom.

The pipeline runs 13 integration test stages in parallel, so you need at least 13 agent slots across all hosts for maximum parallelism. Fewer slots will work but stages will queue.

## 4. Configure Credentials

Go to **Manage Jenkins > Credentials > System > Global credentials > Add Credentials**.

Create four Secret Text credentials:

### SQL Server Connection String

- **Kind**: Secret text
- **ID**: `sqlserver-connstring`
- **Secret**: Your SQL Server connection string, e.g.:
  ```
  Server=<db-host>;Database=IntegrationTests;User Id=sa;Password=<password>;TrustServerCertificate=true;Encrypt=false
  ```

### PostgreSQL Connection String

- **Kind**: Secret text
- **ID**: `postgresql-connstring`
- **Secret**: Your PostgreSQL connection string, e.g.:
  ```
  Host=<db-host>;Database=integrationtests;Username=postgres;Password=<password>
  ```

### Redis Connection String

- **Kind**: Secret text
- **ID**: `redis-connstring`
- **Secret**: Your Redis connection string, e.g.:
  ```
  <redis-host>,defaultDatabase=1,syncTimeout=15000
  ```

### Codecov Token

- **Kind**: Secret text
- **ID**: `codecov-token`
- **Secret**: Your Codecov.io upload token (get this from codecov.io after adding the repository)

## 5. Create the Multibranch Pipeline Job

A Multibranch Pipeline automatically builds `master` and every open PR.

1. Go to **New Item**
2. Enter name: `DotNetWorkQueue`
3. Select **Multibranch Pipeline**, click OK
4. Under **Branch Sources** > click **Add source** > **GitHub**:
   - **Repository HTTPS URL**: `https://github.com/blehnen/DotNetWorkQueue`
   - **Credentials**: Add GitHub credentials if the repo is private (not needed for public repos)
   - **Behaviors** (click **Add**):
     - **Discover branches**: Strategy = "All branches" (or "Only branches that are also filed as PRs" to limit builds)
     - **Discover pull requests from origin**: Strategy = "Merging the pull request with the current target branch revision"
   - **Property strategy**: Default
5. Under **Build Configuration**:
   - **Mode**: by Jenkinsfile
   - **Script Path**: `Jenkinsfile`
6. Under **Scan Multibranch Pipeline Triggers**:
   - Check **Periodically if not otherwise run**
   - **Interval**: 5 minutes (or use a webhook for instant triggers — see below)
7. Click **Save**

Jenkins will immediately scan the repo and create jobs for `master` and any open PRs.

### Optional: GitHub Webhook for Instant Builds

Instead of polling every 5 minutes, set up a webhook for instant build triggers:

1. In GitHub, go to **Settings > Webhooks > Add webhook**
2. **Payload URL**: `http://<your-jenkins-url>:8080/github-webhook/`
3. **Content type**: `application/json`
4. **Events**: Select "Pull requests" and "Pushes"
5. Click **Add webhook**

Note: Your Jenkins master must be reachable from the internet for GitHub webhooks. If it's not (e.g., behind a firewall), the polling interval is fine.

### Branch Filtering

To build only `master` and PRs (not every feature branch), change the **Discover branches** behavior:
- Strategy: **Only branches that are also filed as PRs**

This means only `master` (as a PR target) and open PR branches get built.

## 6. Network Verification

Before running the pipeline, verify Docker containers can reach the test services:

```bash
docker run --rm blehnen74/dotnetworkqueue-ci:latest bash -c "
    curl -s --connect-timeout 5 <db-host>:1433 && echo 'SQL Server: OK' || echo 'SQL Server: FAIL'
    curl -s --connect-timeout 5 <db-host>:5432 && echo 'PostgreSQL: OK' || echo 'PostgreSQL: FAIL'
    curl -s --connect-timeout 5 <redis-host>:6379 && echo 'Redis: OK' || echo 'Redis: FAIL'
"
```

Note: These services may not respond to curl properly, but the connection attempt should not time out. If connections time out, check firewall rules between the Docker hosts and service hosts.

## 7. First Pipeline Run

1. Go to the `DotNetWorkQueue` job
2. Click **Build Now** (or **Scan Multibranch Pipeline Now** for the first scan)
3. Monitor Stage 1 (Build & Unit Tests) — should complete in ~2 min
4. If Stage 1 passes, Stage 2 starts up to 13 parallel integration test agents
5. Stage 3 merges coverage and uploads to Codecov

### Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| "No nodes with label docker" | Docker cloud not configured or hosts unreachable | Check cloud config, verify Docker TCP is open |
| "docker: not found" in pipeline | Using `docker { image }` agent syntax | Use `agent { label 'docker' }` — the cloud provisions the container |
| Java version error in agent | JRE in Docker image older than Jenkins master | Match the JRE version in the [dotnetworkqueue-ci](https://github.com/blehnen/dotnetworkqueue-ci) image to your Jenkins master |
| Connection string errors | Credentials not created or wrong ID | Verify credential IDs match Jenkinsfile: `sqlserver-connstring`, `postgresql-connstring`, `redis-connstring`, `codecov-token` |
| `connectionstring.txt` not found | File written to wrong path | Connection strings must be in `bin/Debug/net10.0/` (written after build) |
| SQLite `libdl.so` errors | Missing native library symlink | Pull the latest [dotnetworkqueue-ci](https://github.com/blehnen/dotnetworkqueue-ci) image — it includes the fix |
| Test host crash (ObjectDisposedException) | Timer callback race on Linux | Fixed in `BaseMonitor.cs` — ensure you have the latest code |
| Coverage upload fails | codecov-token not set or wrong CLI syntax | Verify credential exists; Jenkinsfile uses `codecov upload-process` subcommand |
| Build takes too long | NuGet restore downloading on every run | Consider mounting a NuGet cache volume |

## Pipeline Architecture

```
Stage 1: Build & Unit Tests (~2 min, 1 agent)
    |
Stage 2: Integration Tests (~30-40 min, up to 13 agents in parallel)
    |-- SqlServer          |-- PostgreSQL       |-- Redis          |-- SQLite
    |-- SqlServer Linq     |-- PostgreSQL Linq  |-- Redis Linq     |-- SQLite Linq
    |-- LiteDB             |-- LiteDB Linq      |-- Memory         |-- Memory Linq
    |-- Dashboard
    |
Stage 3: Coverage Report (~1 min, 1 agent)
    |-- Merge Cobertura XML via ReportGenerator
    |-- Upload to Codecov.io
    |-- Publish HTML report
```

## CI Split: Jenkins vs GitHub Actions

| Concern | Jenkins | GitHub Actions |
|---------|---------|----------------|
| **Target framework** | net10.0 | net48 |
| **OS** | Linux (Docker) | Windows |
| **Unit tests** | Yes | Yes |
| **Integration tests** | Yes (all transports) | No |
| **Code coverage** | Coverlet + Codecov | No |
| **Purpose** | Full CI with external services | .NET Framework compatibility check |
