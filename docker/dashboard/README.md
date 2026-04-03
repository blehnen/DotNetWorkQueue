# DotNetWorkQueue Dashboard — Docker Image

A self-contained Docker image that runs both the Dashboard UI and the Dashboard API in a single container. No separate API container is needed; the API is registered in-process when the `Dashboard:Connections` configuration section is present.

## Quick Start (Docker Hub)

```bash
docker pull blehnen74/dotnetworkqueue-dashboard:latest

docker run -d \
  --name dnwq-dashboard \
  -p 8080:8080 \
  -v "$(pwd)/appsettings.json:/app/appsettings.json:ro" \
  blehnen74/dotnetworkqueue-dashboard:latest
```

Open `http://localhost:8080` in your browser.

### Minimal Config Example

Create an `appsettings.json` with just the transport(s) you need:

```json
{
  "Dashboard": {
    "Connections": [
      {
        "Transport": "SqlServer",
        "ConnectionString": "Server=host.docker.internal;Database=mydb;User Id=sa;Password=YourPassword;TrustServerCertificate=true",
        "DisplayName": "SQL Server",
        "Queues": ["myqueue"]
      }
    ]
  },
  "DashboardApi": {
    "BaseUrl": "http://localhost:8080"
  }
}
```

See `appsettings.example.json` for a full config with all five transports.

## Build from Source

Run from the **repository root** (the build context must include `Source/`, `Lib/`, and the central build props):

```bash
docker build -t dotnetworkqueue-dashboard -f docker/dashboard/Dockerfile .
```

## Docker Compose

```yaml
services:
  dashboard:
    image: blehnen74/dotnetworkqueue-dashboard:latest
    ports:
      - "8080:8080"
    volumes:
      - ./appsettings.json:/app/appsettings.json:ro
      # Uncomment for SQLite/LiteDB:
      # - ./data/sqlite:/data/sqlite:ro
      # - ./data/litedb:/data/litedb:ro
```

## Configuration

All configuration is driven by `appsettings.json` (or environment variables using the standard ASP.NET Core `__` separator, e.g. `Dashboard__ApiKey`).

### Connections

The `Dashboard:Connections` array defines which queues are monitored. Each entry:

| Field | Description |
|---|---|
| `Transport` | One of: `SqlServer`, `PostgreSql`, `Redis`, `SQLite`, `LiteDb` |
| `ConnectionString` | Transport-specific connection string |
| `DisplayName` | Label shown in the UI |
| `Queues` | Array of queue names to monitor |

Use `host.docker.internal` to reach services on the Docker host from within the container.

### Authentication

Set `DashboardAuth:Username` and `DashboardAuth:PasswordHash` to enable login.

`PasswordHash` is a SHA-256 hex string of the password. Generate one:

```bash
# Linux / macOS
echo -n "MyPassword" | sha256sum | awk '{print $1}'

# PowerShell
[System.BitConverter]::ToString(
  [System.Security.Cryptography.SHA256]::Create().ComputeHash(
    [System.Text.Encoding]::UTF8.GetBytes("MyPassword")
  )
).Replace("-","").ToLower()
```

### API Key

Set `Dashboard:ApiKey` to a non-empty string to require an `X-Api-Key` header on all API requests. Set the matching `DashboardApi:ApiKey` so the UI can authenticate to the in-process API.

### Swagger

Swagger UI is available at `/swagger` when `Dashboard:EnableSwagger` is `true` (default).

## SQLite and LiteDB Volume Mounts

SQLite and LiteDB queue databases are file-based. Mount a host directory so the container can read existing database files:

```bash
docker run -d \
  --name dnwq-dashboard \
  -p 8080:8080 \
  -v "$(pwd)/appsettings.json:/app/appsettings.json:ro" \
  -v "/host/path/to/sqlite-data:/data/sqlite:ro" \
  -v "/host/path/to/litedb-data:/data/litedb:ro" \
  blehnen74/dotnetworkqueue-dashboard:latest
```

Match the mount paths to the `ConnectionString` values in `appsettings.json`:

```json
{ "Transport": "SQLite",  "ConnectionString": "Data Source=/data/sqlite/myqueue.db" }
{ "Transport": "LiteDb",  "ConnectionString": "Filename=/data/litedb/myqueue.litedb" }
```

## User Message Assemblies (POCO DLLs)

The dashboard deserializes message bodies for display. If your queues contain custom POCO types, the dashboard needs access to those assemblies to show typed message content instead of raw bytes.

### Option 1: Volume mount

Mount a directory containing your DLLs and tell the dashboard where to look:

```bash
docker run -d \
  --name dnwq-dashboard \
  -p 8080:8080 \
  -v "$(pwd)/appsettings.json:/app/appsettings.json:ro" \
  -v "/path/to/your/dlls:/app/plugins:ro" \
  blehnen74/dotnetworkqueue-dashboard:latest
```

Add the path to your `appsettings.json`:

```json
{
  "Dashboard": {
    "AssemblyPaths": ["/app/plugins"]
  }
}
```

### Option 2: Derived image

Build a custom image that includes your DLLs:

```dockerfile
FROM blehnen74/dotnetworkqueue-dashboard:latest
COPY MyMessages.dll /app/plugins/
```

Use the same `AssemblyPaths` configuration as above.

Multiple paths are supported — the dashboard searches them in order after checking the application's own bin directory.

## External API Mode

To point the UI at a separately hosted Dashboard API instead of running in-process, omit the `Dashboard:Connections` section and set:

```json
{
  "DashboardApi": {
    "BaseUrl": "https://my-api-host:5000",
    "ApiKey": "your-api-key"
  }
}
```

## Tags

- `latest` — latest build from master
- `x.y.z` — matches the DotNetWorkQueue NuGet package version
