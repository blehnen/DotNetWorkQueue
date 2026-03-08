# DotNetWorkQueue

Producer/distributed consumer library for .NET. Queue and dequeue POCOs, LINQ expressions (compiled or dynamic), and schedule recurring jobs.

## Features

- Producer/consumer and producer/method consumer patterns
- LINQ expression queuing (compiled and dynamic)
- Recurring job scheduling via Schyntax expressions
- Delayed processing, prioritized queues, message expiration
- Configurable retry policies with Polly
- OpenTelemetry distributed tracing
- Thread-safe with graceful shutdown support
- Targets .NET 10.0, .NET 8.0, .NET Framework 4.8, .NET Standard 2.0

## Installation

```
dotnet add package DotNetWorkQueue
```

## Quick Start

```csharp
// Producer
using var queueContainer = new QueueContainer<SqlServerMessageQueueInit>();
using var producer = queueContainer.CreateProducer<MyMessage>(
    new QueueConnection("queueName", "connectionString"));
producer.Send(new MyMessage { Text = "Hello" });

// Consumer
using var consumer = queueContainer.CreateConsumer(
    new QueueConnection("queueName", "connectionString"));
consumer.Start<MyMessage>(HandleMessage);
```

Complete examples are available in the [samples](https://github.com/blehnen/DotNetWorkQueue/tree/master/Source/Samples) directory.

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
