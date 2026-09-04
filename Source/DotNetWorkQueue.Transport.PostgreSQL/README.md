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

## Performance tuning

### Npgsql automatic statement preparation

Off by default in Npgsql, and this transport does not turn it on. The send path issues the same
handful of statements repeatedly, which is the shape automatic preparation targets, so enabling it
is worth considering — it is a connection-string change, not a code one:

```text
Host=localhost;Database=MyDb;Username=user;Password=pass;Max Auto Prepare=20;Auto Prepare Min Usages=2;
```

Measured on this transport (net10, LAN PostgreSQL, 256-byte payload), across two runs:

| operation | effect |
|---|---|
| single send | **no measurable difference** |
| batch of 100 | **13-17% faster** |

Allocation is unchanged. The benefit is on the batch path and only there — a single send showed a
6% gain in one run and none in another, which is noise rather than a small win. If you send one
message at a time, this setting is not for you.

**Before enabling it, know the trade-off.** Prepared statements live on the physical connection and
the pool hands that connection back out, so dropping or recreating a table invalidates any prepared
statement referencing it. This library creates and drops queues as a normal operation, so that is
not a hypothetical. A create/drop/recreate cycle is covered by an integration test
(`AutoPrepareSurvivesDdl`) and does not fail — but Npgsql exposes no public counter for
auto-prepared statements, so that test shows the scenario working rather than proving the
invalidation path was exercised.

If your application creates and drops queues at runtime, measure your own workload before turning
this on. If your queues are long-lived and you send in batches, it is close to free.

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
