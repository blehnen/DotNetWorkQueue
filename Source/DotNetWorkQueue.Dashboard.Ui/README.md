# DotNetWorkQueue.Dashboard.Ui

Blazor Server dashboard UI for monitoring and managing [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue) queues.

## Features

- Real-time queue status monitoring
- Message listing with filtering and sorting
- Message detail views (body, headers, errors)
- Error management: requeue individual or all error messages
- Stale message reset
- Message body editing
- Dark theme using MudBlazor
- Optional login with SHA256 hashed password
- Can run standalone or embedded in a host application
- Targets .NET 10.0, .NET 8.0

## Installation

```
dotnet add package DotNetWorkQueue.Dashboard.Ui
```

## Quick Start

The Dashboard UI connects to the Dashboard API via HTTP. Configure the API base URL in `appsettings.json`:

```json
{
  "DashboardApi": {
    "BaseUrl": "https://localhost:5001"
  }
}
```

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
