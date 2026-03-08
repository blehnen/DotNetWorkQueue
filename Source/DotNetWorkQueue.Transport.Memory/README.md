# DotNetWorkQueue.Transport.Memory

In-memory transport for [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue).

## Features

- In-memory message storage with no external dependencies
- Ideal for unit testing, integration testing, and lightweight scenarios
- Supports delayed processing and priority queues
- Dashboard support for monitoring
- Targets .NET 10.0, .NET 8.0, .NET Framework 4.8, .NET Standard 2.0

## Installation

```
dotnet add package DotNetWorkQueue.Transport.Memory
```

## Quick Start

```csharp
// Producer
using var queueContainer = new QueueContainer<MemoryMessageQueueInit>();
using var producer = queueContainer.CreateProducer<MyMessage>(
    new QueueConnection("queueName", "memory"));
producer.Send(new MyMessage { Text = "Hello" });

// Consumer
using var consumer = queueContainer.CreateConsumer(
    new QueueConnection("queueName", "memory"));
consumer.Start<MyMessage>(HandleMessage);
```

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
