# Phase 3: Docker Image and Example Configuration

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Create a standalone Docker image for the Dashboard that serves both the Blazor UI and REST API in a single container, configured via mounted `appsettings.json`.

**Architecture:** The Dashboard.Ui project currently talks to Dashboard.Api via HTTP (separate processes). For Docker, we add a ProjectReference from Dashboard.Ui to Dashboard.Api so one process serves both. When a `Dashboard:Connections` config section is present, `Program.cs` calls the `IConfiguration` overload from Phase 2 to register transports and starts the API middleware in-process. When absent, it falls back to the existing external-API-via-HTTP pattern.

**Tech Stack:** ASP.NET Core 10.0, Blazor Server, MudBlazor, Docker multi-stage build

---

<task id="1" name="Wire Dashboard.Api into Dashboard.Ui for self-contained mode">
  <description>
    Add a ProjectReference from Dashboard.Ui to Dashboard.Api. Update Program.cs to conditionally
    register the Dashboard API services and middleware when a "Dashboard:Connections" config section
    exists. This makes the UI self-contained when configured, while preserving the existing
    external-API-via-HTTP mode when the config section is absent.
  </description>
  <files>
    <modify>Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj</modify>
    <modify>Source/DotNetWorkQueue.Dashboard.Ui/Program.cs:28-121</modify>
  </files>
  <steps>
    <step>Add ProjectReference to Dashboard.Api in the .csproj</step>
    <step>Add conditional self-contained mode to Program.cs</step>
    <step>Build to verify compilation</step>
    <step>Run existing Dashboard API integration tests to verify no regression</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"</command>
    <expected>Build succeeded. 0 Warning(s). 0 Error(s).</expected>
  </verification>
  <verification>
    <command>dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory"</command>
    <expected>All tests passed</expected>
  </verification>
</task>

### Task 1: Wire Dashboard.Api into Dashboard.Ui for self-contained mode

**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj`
- Modify: `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs`

**Step 1: Add ProjectReference to Dashboard.Api in the .csproj**

In `DotNetWorkQueue.Dashboard.Ui.csproj`, add an `<ItemGroup>` with a ProjectReference to Dashboard.Api. This pulls in all 5 transport assemblies transitively (Dashboard.Api already references them all).

```xml
  <ItemGroup>
    <ProjectReference Include="..\DotNetWorkQueue.Dashboard.Api\DotNetWorkQueue.Dashboard.Api.csproj" />
  </ItemGroup>
```

Add this after the existing `<ItemGroup>` containing the `MudBlazor` PackageReference (after line 21).

**Step 2: Add conditional self-contained mode to Program.cs**

Add a `using` for the Dashboard.Api namespace and insert a conditional block after the `builder.Services.AddMudServices();` line (line 33). The block checks if a `Dashboard:Connections` config section exists; if so, it registers the API services in-process. The existing HTTP client registration for the external API mode stays — when in self-contained mode it just points to itself.

Add these usings at the top (after line 27, before `var builder`):

```csharp
using DotNetWorkQueue.Dashboard.Api;
```

After `builder.Services.AddMudServices();` (line 33), add:

```csharp
// --- Self-contained mode: embed Dashboard API in this process ---
var dashboardSection = builder.Configuration.GetSection("Dashboard");
var selfContained = dashboardSection.GetSection("Connections").GetChildren().Any();
if (selfContained)
{
    builder.Services.AddDotNetWorkQueueDashboard(dashboardSection);
}
```

Then, after `app.UseStaticFiles();` (line 87) and before the login/logout endpoints, add:

```csharp
if (selfContained)
{
    app.UseDotNetWorkQueueDashboard();
    app.UseRouting();
    app.MapControllers();
}
```

**Important:** The `selfContained` variable needs to be accessible to both the service registration and middleware sections. Declare it before `var app = builder.Build();`. Since it's already declared before that line (after `AddMudServices`), it's fine — it's a top-level statement local.

**Step 3: Build to verify compilation**

Run:
```bash
dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"
```
Expected: `Build succeeded` with 0 errors.

**Step 4: Run existing Dashboard API integration tests**

Run:
```bash
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory"
```
Expected: All tests pass (confirms API services still register correctly).

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj Source/DotNetWorkQueue.Dashboard.Ui/Program.cs
git commit -m "feat: add self-contained mode to Dashboard UI for Docker deployment"
```

---

<task id="2" name="Create Dockerfile, example config, and README">
  <description>
    Create a multi-stage Dockerfile that builds the Dashboard UI for net10.0 and produces a lean
    ASP.NET runtime image. Ship an appsettings.example.json showing all 5 transport configurations.
    Write a README with build and run instructions.
  </description>
  <files>
    <create>docker/dashboard/Dockerfile</create>
    <create>docker/dashboard/appsettings.example.json</create>
    <create>docker/dashboard/README.md</create>
  </files>
  <steps>
    <step>Create the multi-stage Dockerfile</step>
    <step>Create appsettings.example.json with all 5 transports</step>
    <step>Create README.md with build/run instructions</step>
    <step>Build the Docker image to verify</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>docker build -t dotnetworkqueue-dashboard -f docker/dashboard/Dockerfile .</command>
    <expected>Successfully built / Successfully tagged dotnetworkqueue-dashboard</expected>
  </verification>
</task>

### Task 2: Create Dockerfile, example config, and README

**Files:**
- Create: `docker/dashboard/Dockerfile`
- Create: `docker/dashboard/appsettings.example.json`
- Create: `docker/dashboard/README.md`

**Step 1: Create the multi-stage Dockerfile**

Create `docker/dashboard/Dockerfile`. This is a multi-stage build:
- **Stage 1 (build):** Uses .NET 10 SDK + .NET 8 SDK (needed because projects multi-target net8.0;net10.0). Restores and publishes the Dashboard.Ui project for net10.0 only.
- **Stage 2 (runtime):** Uses ASP.NET 10.0 runtime image. Installs `libsqlite3-0` for SQLite/LiteDB support. Copies published output. Exposes port 8080.

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install .NET 8 SDK (required for multi-targeted project references)
RUN dotnet_version=8.0.408 \
    && curl -fSL --output dotnet8.tar.gz \
       "https://dotnetcli.azureedge.net/dotnet/Sdk/${dotnet_version}/dotnet-sdk-${dotnet_version}-linux-x64.tar.gz" \
    && tar -oxzf dotnet8.tar.gz -C /usr/share/dotnet ./sdk ./shared \
    && rm dotnet8.tar.gz

WORKDIR /src

# Copy solution and project files first for layer caching
COPY Source/DotNetWorkQueue.sln Source/
COPY Source/DotNetWorkQueue/DotNetWorkQueue.csproj Source/DotNetWorkQueue/
COPY Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj Source/DotNetWorkQueue.Transport.Shared/
COPY Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj Source/DotNetWorkQueue.Transport.RelationalDatabase/
COPY Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj Source/DotNetWorkQueue.Transport.SqlServer/
COPY Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj Source/DotNetWorkQueue.Transport.PostgreSQL/
COPY Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj Source/DotNetWorkQueue.Transport.Redis/
COPY Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj Source/DotNetWorkQueue.Transport.SQLite/
COPY Source/DotNetWorkQueue.Transport.LiteDB/DotNetWorkQueue.Transport.LiteDB.csproj Source/DotNetWorkQueue.Transport.LiteDB/
COPY Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj Source/DotNetWorkQueue.Dashboard.Api/
COPY Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj Source/DotNetWorkQueue.Dashboard.Ui/
COPY Lib/ Lib/
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

# Restore only the Dashboard.Ui project (pulls transitive deps)
RUN dotnet restore "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"

# Copy all source
COPY Source/ Source/

# Publish for net10.0 (single TFM for the container)
RUN dotnet publish "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj" \
    -c Release \
    -f net10.0 \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Install native libraries for SQLite/LiteDB support
RUN apt-get update \
    && apt-get install -y --no-install-recommends libsqlite3-0 \
    && rm -rf /var/lib/apt/lists/*

# System.Data.SQLite expects libdl.so — create symlink for glibc 2.34+
RUN if [ ! -e /usr/lib/x86_64-linux-gnu/libdl.so ]; then \
        ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so 2>/dev/null || \
        ln -s /lib/x86_64-linux-gnu/libc.so.6 /usr/lib/x86_64-linux-gnu/libdl.so; \
    fi

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DotNetWorkQueue.Dashboard.Ui.dll"]
```

**Key decisions:**
- .NET 8 SDK is installed in the build stage because project references multi-target `net8.0;net10.0` — MSBuild needs both SDKs to resolve during restore/build even though we only publish `net10.0`.
- `libsqlite3-0` is installed in the runtime stage for SQLite/LiteDB transports.
- The `libdl.so` symlink matches the pattern from the Jenkins CI Dockerfile (`docker/Dockerfile:24-27`).
- `Directory.Build.props` and `Directory.Packages.props` are copied to the root for Central Package Management.
- `Lib/` is copied for the custom libraries (Schyntax, ExpressionJsonSerializer, DynamicCode).

**Step 2: Create appsettings.example.json**

Create `docker/dashboard/appsettings.example.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "DashboardApi": {
    "BaseUrl": "http://localhost:8080",
    "ApiKey": ""
  },
  "DashboardAuth": {
    "Username": "",
    "PasswordHash": ""
  },
  "Dashboard": {
    "EnableSwagger": true,
    "ApiKey": "",
    "Connections": [
      {
        "Transport": "SqlServer",
        "ConnectionString": "Server=host.docker.internal;Database=mydb;User Id=sa;Password=YourPassword;TrustServerCertificate=true",
        "DisplayName": "SQL Server",
        "Queues": ["queue1", "queue2"]
      },
      {
        "Transport": "PostgreSql",
        "ConnectionString": "Host=host.docker.internal;Database=mydb;Username=postgres;Password=YourPassword",
        "DisplayName": "PostgreSQL",
        "Queues": ["queue1"]
      },
      {
        "Transport": "Redis",
        "ConnectionString": "host.docker.internal:6379",
        "DisplayName": "Redis",
        "Queues": ["queue1"]
      },
      {
        "Transport": "SQLite",
        "ConnectionString": "Data Source=/data/sqlite/myqueue.db",
        "DisplayName": "SQLite (volume-mount /data/sqlite)",
        "Queues": ["queue1"]
      },
      {
        "Transport": "LiteDb",
        "ConnectionString": "Filename=/data/litedb/myqueue.litedb",
        "DisplayName": "LiteDB (volume-mount /data/litedb)",
        "Queues": ["queue1"]
      }
    ]
  }
}
```

**Notes encoded in the file:**
- `DashboardApi.BaseUrl` points to `localhost:8080` (self, since API is in-process)
- SQLite `ConnectionString` uses `/data/sqlite/` — a volume-mount path
- LiteDB `ConnectionString` uses `/data/litedb/` — a volume-mount path
- `host.docker.internal` for accessing host-network services from the container

**Step 3: Create README.md**

Create `docker/dashboard/README.md` with build and run instructions:

```markdown
# DotNetWorkQueue Dashboard — Docker Image

Standalone Docker image for the DotNetWorkQueue Dashboard. Serves both the Blazor UI and REST API in a single container, configured via a mounted `appsettings.json`.

## Build

From the repository root:

    docker build -t dotnetworkqueue-dashboard -f docker/dashboard/Dockerfile .

## Run

1. Copy `appsettings.example.json` and edit connection strings:

       cp docker/dashboard/appsettings.example.json appsettings.dashboard.json
       # Edit appsettings.dashboard.json with your connection strings

2. Run the container:

       docker run --rm \
         -v ./appsettings.dashboard.json:/app/appsettings.json \
         -p 8080:8080 \
         dotnetworkqueue-dashboard

3. Open http://localhost:8080

## Configuration

The `Dashboard:Connections` array configures which transports to monitor. Each entry requires:

| Field | Description |
|-------|-------------|
| `Transport` | One of: `SqlServer`, `PostgreSql`, `Redis`, `SQLite`, `LiteDb` |
| `ConnectionString` | Transport-specific connection string |
| `DisplayName` | Label shown in the UI |
| `Queues` | Array of queue names to monitor |

### Authentication

Set `DashboardAuth:Username` and `DashboardAuth:PasswordHash` to enable login.
Generate a SHA256 hash:

    echo -n 'yourpassword' | sha256sum | cut -d' ' -f1

### API Key

Set `Dashboard:ApiKey` to require an `X-Api-Key` header on REST API calls.

## SQLite and LiteDB

These transports store data on the local filesystem. Mount a volume for the database path:

    docker run --rm \
      -v ./appsettings.dashboard.json:/app/appsettings.json \
      -v ./data/sqlite:/data/sqlite \
      -v ./data/litedb:/data/litedb \
      -p 8080:8080 \
      dotnetworkqueue-dashboard

Ensure the connection strings in your config point to the mounted paths (e.g., `Data Source=/data/sqlite/myqueue.db`).

## Swagger

Swagger UI is enabled by default at http://localhost:8080/swagger when `Dashboard:EnableSwagger` is `true`.
```

**Step 4: Build the Docker image to verify**

Run from the repository root:
```bash
docker build -t dotnetworkqueue-dashboard -f docker/dashboard/Dockerfile .
```
Expected: Image builds successfully. Final image contains only the ASP.NET 10.0 runtime, not the SDK.

Optional smoke test:
```bash
docker run --rm -v ./docker/dashboard/appsettings.example.json:/app/appsettings.json -p 8080:8080 dotnetworkqueue-dashboard
```
Expected: App starts, serves UI at http://localhost:8080. Transport connections will fail without real services, but the app should not crash.

**Step 5: Commit**

```bash
git add docker/dashboard/Dockerfile docker/dashboard/appsettings.example.json docker/dashboard/README.md
git commit -m "feat: add Docker image for standalone Dashboard deployment"
```
