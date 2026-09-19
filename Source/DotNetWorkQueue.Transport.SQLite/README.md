# DotNetWorkQueue.Transport.SQLite

SQLite transport for [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue).

## Features

- SQLite message storage using System.Data.SQLite
- File-based or in-memory database support
- Supports delayed processing, message expiration, and priority queues
- Heartbeat monitoring for long-running messages
- Job scheduling with deduplication
- Ideal for single-process scenarios or development/testing
- Targets .NET 10.0, .NET 8.0 and .NET Standard 2.0. The netstandard2.0 build is how .NET
  Framework uses this package; Microsoft supports it from 4.6.1 but recommends 4.7.2 or later

## Installation

```
dotnet add package DotNetWorkQueue.Transport.SQLite
```

## Quick Start

```csharp
// Producer
using var queueContainer = new QueueContainer<SqLiteMessageQueueInit>();
using var producer = queueContainer.CreateProducer<MyMessage>(
    new QueueConnection("queueName", "Data Source=queue.db;"));
producer.Send(new MyMessage { Text = "Hello" });

// Consumer
using var consumer = queueContainer.CreateConsumer(
    new QueueConnection("queueName", "Data Source=queue.db;"));
consumer.Start<MyMessage>(HandleMessage);
```

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
