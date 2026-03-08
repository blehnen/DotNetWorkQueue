# DotNetWorkQueue.Transport.SqlServer

SQL Server transport for [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue).

## Features

- SQL Server message storage using Microsoft.Data.SqlClient
- Supports delayed processing, message expiration, and priority queues
- Heartbeat monitoring for long-running messages
- Automatic table creation and schema management
- Job scheduling with deduplication
- Targets .NET 10.0, .NET 8.0, .NET Framework 4.8, .NET Standard 2.0

## Installation

```
dotnet add package DotNetWorkQueue.Transport.SqlServer
```

## Quick Start

```csharp
// Producer
using var queueContainer = new QueueContainer<SqlServerMessageQueueInit>();
using var producer = queueContainer.CreateProducer<MyMessage>(
    new QueueConnection("queueName", "Server=.;Database=MyDb;Trusted_Connection=true;"));
producer.Send(new MyMessage { Text = "Hello" });

// Consumer
using var consumer = queueContainer.CreateConsumer(
    new QueueConnection("queueName", "Server=.;Database=MyDb;Trusted_Connection=true;"));
consumer.Start<MyMessage>(HandleMessage);
```

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
