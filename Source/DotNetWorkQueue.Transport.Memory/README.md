# DotNetWorkQueue.Transport.Memory

In-memory transport for [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue).

## Features

- In-memory message storage with no external dependencies
- Ideal for unit testing, integration testing, and lightweight scenarios
- Dashboard support for monitoring
- Targets .NET 10.0, .NET 8.0 and .NET Standard 2.0. The netstandard2.0 build is how .NET
  Framework uses this package; Microsoft supports it from 4.6.1 but recommends 4.7.2 or later

## Limitations

The memory transport is simpler than the others, so code tested against it can behave differently on them:

- **No retries.** A message whose handler throws goes straight to the error queue. Retry rules (`RetryDelayBehavior`) are ignored.
- **No stored errors.** The error queue is only a count of failed messages, so there is nothing in it to inspect, requeue or purge.
- **No rollback, delayed processing, priority or message expiration.** Messages are delivered in the order they were sent.
- **Nothing survives the process.** Every message is lost when it exits.

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
