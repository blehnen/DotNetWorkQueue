# DotNetWorkQueue.Transport.PostgreSQL

PostgreSQL transport for [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue).

## Features

- PostgreSQL message storage using Npgsql
- Supports delayed processing, message expiration, and priority queues
- Heartbeat monitoring for long-running messages
- Automatic table creation and schema management
- Job scheduling with deduplication
- Targets .NET 10.0, .NET 8.0, .NET Framework 4.8, .NET Standard 2.0

## Installation

```
dotnet add package DotNetWorkQueue.Transport.PostgreSQL
```

## Quick Start

```csharp
// Producer
using var queueContainer = new QueueContainer<PostgreSqlMessageQueueInit>();
using var producer = queueContainer.CreateProducer<MyMessage>(
    new QueueConnection("queueName", "Host=localhost;Database=MyDb;Username=user;Password=pass;"));
producer.Send(new MyMessage { Text = "Hello" });

// Consumer
using var consumer = queueContainer.CreateConsumer(
    new QueueConnection("queueName", "Host=localhost;Database=MyDb;Username=user;Password=pass;"));
consumer.Start<MyMessage>(HandleMessage);
```

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
