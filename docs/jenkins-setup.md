# Jenkins Master Setup Guide

This guide walks through setting up the Jenkins master to run the DotNetWorkQueue CI pipeline.

## Prerequisites

- Jenkins LTS installed on `192.168.0.2`
- Docker installed on both agent hosts (`192.168.0.75` and `192.168.0.2`)
- Docker daemon listening on TCP on both hosts (port 2375)
- Test services running on `192.168.0.2`:
  - SQL Server on port 1433
  - PostgreSQL on port 5432
  - Redis on port 6379

## 1. Build the Docker Agent Image

On each Docker host, build the CI agent image:

```bash
cd /path/to/dotnetworkqueue
docker build -t dotnetworkqueue-ci:latest docker/
```

Verify both SDKs are available:

```bash
docker run --rm dotnetworkqueue-ci:latest dotnet --list-sdks
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

Restart Jenkins after installing plugins.

## 3. Configure Docker Cloud

Go to **Manage Jenkins > Clouds > New cloud > Docker**.

### Docker Host 1 (192.168.0.75 - 4 agent slots)

- **Name**: `docker-host-75`
- **Docker Host URI**: `tcp://192.168.0.75:2375`
- **Container Cap**: `4`
- **Docker Agent Template**:
  - **Labels**: `docker`
  - **Docker Image**: `dotnetworkqueue-ci:latest`
  - **Remote Filing System Root**: `/home/jenkins`
  - **Connect method**: Attach Docker container

### Docker Host 2 (192.168.0.2 - 2 agent slots)

- **Name**: `docker-host-2`
- **Docker Host URI**: `tcp://192.168.0.2:2375`
- **Container Cap**: `2`
- **Docker Agent Template**:
  - **Labels**: `docker`
  - **Docker Image**: `dotnetworkqueue-ci:latest`
  - **Remote Filing System Root**: `/home/jenkins`
  - **Connect method**: Attach Docker container

Both hosts use the same `docker` label so the Jenkinsfile can request any available agent.

## 4. Configure Credentials

Go to **Manage Jenkins > Credentials > System > Global credentials > Add Credentials**.

Create three Secret Text credentials:

### SQL Server Connection String

- **Kind**: Secret text
- **ID**: `sqlserver-connstring`
- **Secret**: Your SQL Server connection string, e.g.:
  ```
  Server=192.168.0.2;Database=IntegrationTests;User Id=sa;Password=yourpassword;TrustServerCertificate=true;Encrypt=false
  ```

### PostgreSQL Connection String

- **Kind**: Secret text
- **ID**: `postgresql-connstring`
- **Secret**: Your PostgreSQL connection string, e.g.:
  ```
  Host=192.168.0.2;Database=integrationtests;Username=postgres;Password=yourpassword
  ```

### Codecov Token

- **Kind**: Secret text
- **ID**: `codecov-token`
- **Secret**: Your Codecov.io upload token (get this from codecov.io after adding the repository)

## 5. Create the Pipeline Job

1. Go to **New Item**
2. Enter name: `DotNetWorkQueue`
3. Select **Pipeline**, click OK
4. Under **Pipeline**:
   - **Definition**: Pipeline script from SCM
   - **SCM**: Git
   - **Repository URL**: Your repository URL
   - **Branches to build**: `*/master` (or `*/jenkins` for testing)
   - **Script Path**: `Jenkinsfile`
5. Click **Save**

## 6. Network Verification

Before running the pipeline, verify Docker containers on `192.168.0.75` can reach test services on `192.168.0.2`:

```bash
# Run from 192.168.0.75
docker run --rm dotnetworkqueue-ci:latest bash -c "
    curl -s --connect-timeout 5 192.168.0.2:1433 && echo 'SQL Server: OK' || echo 'SQL Server: FAIL'
    curl -s --connect-timeout 5 192.168.0.2:5432 && echo 'PostgreSQL: OK' || echo 'PostgreSQL: FAIL'
    curl -s --connect-timeout 5 192.168.0.2:6379 && echo 'Redis: OK' || echo 'Redis: FAIL'
"
```

Note: These services may not respond to curl properly, but the connection attempt should not time out. If connections time out, check firewall rules between the Docker hosts.

## 7. First Pipeline Run

1. Go to the `DotNetWorkQueue` job
2. Click **Build Now**
3. Monitor Stage 1 (Build & Unit Tests) — should complete in ~7 min
4. If Stage 1 passes, Stage 2 starts 6 parallel agents
5. Stage 3 merges coverage and uploads to Codecov

### Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| "No nodes with label docker" | Docker cloud not configured or hosts unreachable | Check cloud config, verify Docker TCP is open |
| Connection string errors | Credentials not created or wrong ID | Verify credential IDs match Jenkinsfile exactly |
| SQL Server/PostgreSQL test failures | Firewall blocking container-to-service traffic | Open ports 1433/5432 from Docker network to 192.168.0.2 |
| Coverage upload fails | codecov-token not set or Codecov not configured | Create token at codecov.io, add as Jenkins credential |
| Build takes too long | NuGet restore downloading on every run | Consider mounting a NuGet cache volume |

## Pipeline Timing Targets

| Stage | Expected Duration |
|-------|------------------|
| Build & Unit Tests | ~7 min |
| Integration Tests (parallel) | ~63 min max |
| Coverage Report | ~2 min |
| **Total** | **< 65 min** |

If actual timing differs significantly, adjust the agent balancing in the `Jenkinsfile` parallel stages.
