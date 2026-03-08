# DotNetWorkQueue.AppMetrics

[App.Metrics](https://github.com/AppMetrics/AppMetrics) integration for [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue).

## Features

- Tracks message send and receive counts
- Measures message processing times
- Monitors error rates and retry counts
- Integrates with App.Metrics reporters (InfluxDB, Prometheus, etc.)
- Targets .NET 10.0, .NET 8.0, .NET Framework 4.8, .NET Standard 2.0

## Installation

```
dotnet add package DotNetWorkQueue.AppMetrics
```

## Quick Start

```csharp
var metrics = new MetricsBuilder().Build();
var container = new QueueContainer<SqlServerMessageQueueInit>(x =>
    x.RegisterMetrics(metrics));
```

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
