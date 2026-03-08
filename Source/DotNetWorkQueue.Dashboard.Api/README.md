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

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
