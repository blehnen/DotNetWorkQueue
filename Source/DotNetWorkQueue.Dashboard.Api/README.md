# DotNetWorkQueue.Dashboard.Api

REST API for monitoring and managing [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue) queues.

## Features

- Queue status and message listing with filtering
- Message detail views including body, headers, and error info
- Error message requeue (individual and bulk)
- Stale message reset (individual and bulk)
- Message body editing
- Swagger/OpenAPI documentation
- Optional API key authentication
- Supports all 6 transports: SQL Server, PostgreSQL, SQLite, Redis, LiteDB, Memory
- Targets .NET 10.0, .NET 8.0

## Installation

```
dotnet add package DotNetWorkQueue.Dashboard.Api
```

## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDashboardApi(builder.Configuration);
var app = builder.Build();
app.MapDashboardApi();
app.Run();
```

## Deployment

> **Important:** The Dashboard API is designed for internal use only. Deploy it behind a VPN, firewall, or reverse proxy that restricts access to authorized operators.

**Infrastructure Concerns (not handled by the API):**
- **HTTPS/TLS** -- Terminate TLS at your reverse proxy (nginx, HAProxy, AWS ALB)
- **Rate limiting** -- Configure at the infrastructure layer
- **Authentication** -- Use the built-in API key (`ApiKey` option) or configure an ASP.NET Core authorization policy (`AuthorizationPolicy` option)
- **CORS** -- Configure allowed origins via `EnableCors` and `CorsOrigins` options when the Blazor UI runs on a different origin

**Health Check:**
The API exposes a health check endpoint at `/api/v1/dashboard/health` for use with load balancers and monitoring systems. Returns HTTP 200 when healthy with uptime and connection count data.

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
