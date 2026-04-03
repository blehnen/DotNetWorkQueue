# DotNetWorkQueue Dashboard

Web dashboard for monitoring and managing [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue) message queues. Provides a Blazor Server UI and REST API in a single container.

## Supported Transports

- SQL Server
- PostgreSQL
- Redis
- SQLite (file-based, volume mount required)
- LiteDB (file-based, volume mount required)

## Quick Start

```bash
docker run -d \
  --name dnwq-dashboard \
  -p 8080:8080 \
  -v "$(pwd)/appsettings.json:/app/appsettings.json:ro" \
  blehnen74/dotnetworkqueue-dashboard:latest
```

Open http://localhost:8080

## Minimal Configuration

Create an `appsettings.json`:

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

Replace `SqlServer` with your transport: `PostgreSql`, `Redis`, `SQLite`, or `LiteDb`. Add multiple entries to monitor several transports at once.

A full example config with all five transports is available at [appsettings.example.json](https://github.com/blehnen/DotNetWorkQueue/blob/master/docker/dashboard/appsettings.example.json).

## Docker Compose

```yaml
services:
  dashboard:
    image: blehnen74/dotnetworkqueue-dashboard:latest
    ports:
      - "8080:8080"
    volumes:
      - ./appsettings.json:/app/appsettings.json:ro
```

## Features

- **Queue monitoring** — view messages, errors, stale records, and consumer status per queue
- **Scheduled jobs** — view recurring job schedules and their status
- **Configuration viewer** — inspect queue configuration at a glance
- **Message history** — browse message processing history
- **REST API** — all data available via `/api/v1/dashboard/*` endpoints
- **Swagger UI** — interactive API docs at `/swagger` (enabled by default)
- **Authentication** — optional username/password login with SHA-256 hashed passwords
- **API key** — optional `X-Api-Key` header for API access control

## Authentication

Add to your `appsettings.json`:

```json
{
  "DashboardAuth": {
    "Username": "admin",
    "PasswordHash": "<sha256-hex-of-your-password>"
  }
}
```

Generate a hash: `echo -n 'yourpassword' | sha256sum | awk '{print $1}'`

## SQLite / LiteDB

These transports use local database files. Mount the data directory into the container:

```bash
docker run -d \
  -p 8080:8080 \
  -v "$(pwd)/appsettings.json:/app/appsettings.json:ro" \
  -v "/path/to/data:/data/sqlite:ro" \
  blehnen74/dotnetworkqueue-dashboard:latest
```

Set the connection string to the mounted path: `"Data Source=/data/sqlite/myqueue.db"`

## User Message Assemblies (POCO DLLs)

If your queues contain custom POCO types, the dashboard needs those assemblies to display typed message bodies. Mount a directory with your DLLs and configure the path:

```bash
docker run -d \
  -p 8080:8080 \
  -v "$(pwd)/appsettings.json:/app/appsettings.json:ro" \
  -v "/path/to/your/dlls:/app/plugins:ro" \
  blehnen74/dotnetworkqueue-dashboard:latest
```

Add to `appsettings.json`:

```json
{
  "Dashboard": {
    "AssemblyPaths": ["/app/plugins"]
  }
}
```

You can also build a derived image instead: `FROM blehnen74/dotnetworkqueue-dashboard:latest` and `COPY` your DLLs into `/app/plugins/`.

## Tags

| Tag | Description |
|-----|-------------|
| `latest` | Latest build from master |
| `x.y.z` | Matches DotNetWorkQueue NuGet package version |

## Links

- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)
- [Full Documentation](https://github.com/blehnen/DotNetWorkQueue/blob/master/docker/dashboard/README.md)
- [Example Config](https://github.com/blehnen/DotNetWorkQueue/blob/master/docker/dashboard/appsettings.example.json)
- [Report Issues](https://github.com/blehnen/DotNetWorkQueue/issues)
