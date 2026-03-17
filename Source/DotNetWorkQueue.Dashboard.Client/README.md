# DotNetWorkQueue.Dashboard.Client

Typed API client and consumer registration client for the [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue) dashboard.

## Features

- Consumer registration with automatic heartbeat timer
- Thread-safe metric counters (processed, errors, rollbacks, poison messages) sent with each heartbeat
- Typed API client for all dashboard endpoints (queues, messages, errors, consumers)
- No dependency on the core library — HttpClient and System.Text.Json only
- Targets .NET 10.0, .NET 8.0

## Installation

```
dotnet add package DotNetWorkQueue.Dashboard.Client
```

## Quick Start

```csharp
// Register a consumer with the dashboard and send periodic heartbeats
var options = new DashboardClientOptions
{
    DashboardApiUrl = "http://localhost:5000",
    QueueName = "myQueue",
    FriendlyName = "MyConsumer"
};

using var client = new DashboardConsumerClient(options);
await client.StartAsync();

// Wire the client into the consumer pipeline for automatic metric counting.
// Register before creating the consumer queue:
container.Register<IConsumerMetricsNotification>(
    () => new ConsumerMetricsNotification(
        client.IncrementProcessed,
        client.IncrementErrored,
        client.IncrementRolledBack,
        client.IncrementPoisonMessage),
    LifeStyles.Singleton);

// Counters are now automatically incremented by the consumer pipeline
// and sent to the dashboard with each heartbeat.

// Stop and unregister when done
await client.StopAsync();
```

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
