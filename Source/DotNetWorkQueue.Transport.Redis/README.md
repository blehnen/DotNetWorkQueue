# DotNetWorkQueue.Transport.Redis

Redis transport for [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue).

## Features

- Redis message storage using StackExchange.Redis
- Lua scripts for atomic queue operations
- Supports delayed processing, message expiration, and priority queues
- Heartbeat monitoring for long-running messages
- Job scheduling with deduplication
- Targets .NET 10.0, .NET 8.0, .NET Framework 4.8, .NET Standard 2.0

## Installation

```
dotnet add package DotNetWorkQueue.Transport.Redis
```

## Quick Start

```csharp
// Producer
using var queueContainer = new QueueContainer<RedisQueueInit>();
using var producer = queueContainer.CreateProducer<MyMessage>(
    new QueueConnection("queueName", "localhost:6379"));
producer.Send(new MyMessage { Text = "Hello" });

// Consumer
using var consumer = queueContainer.CreateConsumer(
    new QueueConnection("queueName", "localhost:6379"));
consumer.Start<MyMessage>(HandleMessage);
```

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
