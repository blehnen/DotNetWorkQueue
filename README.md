# DotNetWorkQueue

[![License LGPLv2.1](https://img.shields.io/badge/license-LGPLv2.1-green.svg)](http://www.gnu.org/licenses/lgpl-2.1.html)
[![Build status](https://ci.appveyor.com/api/projects/status/vqqq9m0j9xodbfof/branch/master?svg=true)](https://ci.appveyor.com/project/blehnen/dotnetworkqueue/branch/master)
[![Coverity status](https://scan.coverity.com/projects/10126/badge.svg)](https://scan.coverity.com/projects/blehnen-dotnetworkqueue)
[![codecov](https://codecov.io/gh/blehnen/DotNetWorkQueue/branch/master/graph/badge.svg?token=E23UZ6U9CU)](https://codecov.io/gh/blehnen/DotNetWorkQueue)

A producer / distributed consumer library for .NET applications. Targets .NET 4.8, .NET 8.0, .NET 10.0, and .NET Standard 2.0.

**High-level features:**
- Queue / de-queue POCOs for distributed processing
- Queue / process LINQ statements (compiled or dynamic, expressed as a string)
- Re-occurring job scheduler

See the [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki) for in-depth documentation.

---

## Installation

**Base**

| Package | NuGet |
|---------|-------|
| DotNetWorkQueue | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue)](https://www.nuget.org/packages/DotNetWorkQueue/) |

**Transports**

| Package | NuGet |
|---------|-------|
| DotNetWorkQueue.Transport.Redis | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.Redis)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.Redis/) |
| DotNetWorkQueue.Transport.SqlServer | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.SqlServer)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.SqlServer/) |
| DotNetWorkQueue.Transport.SQLite | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.SQLite)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.SQLite/) |
| DotNetWorkQueue.Transport.PostgreSQL | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.PostgreSQL)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.PostgreSQL/) |
| DotNetWorkQueue.Transport.LiteDb | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.LiteDb)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.LiteDb/) |

**Transports (In-Memory)**

| Package | NuGet |
|---------|-------|
| DotNetWorkQueue.Transport.Memory | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.Memory)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.Memory/) |

**Dashboard**

| Package | NuGet |
|---------|-------|
| DotNetWorkQueue.Dashboard.Api | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Dashboard.Api)](https://www.nuget.org/packages/DotNetWorkQueue.Dashboard.Api/) |

The Dashboard API provides a REST API for monitoring and managing queues across all transports. It exposes endpoints for viewing queue status, message counts, error counts, and performing administrative actions such as deleting messages and purging error queues. Built on ASP.NET Core with Swagger/OpenAPI documentation.

**Metrics**

| Package | NuGet |
|---------|-------|
| DotNetWorkQueue.AppMetrics | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.AppMetrics)](https://www.nuget.org/packages/DotNetWorkQueue.AppMetrics/) |

---

## Differences Between Versions

.NET Standard 2.0, 8.0, and 10.0 are missing the following features compared to the full framework version:

- No support for aborting threads when stopping consumer queues
- No support for dynamic LINQ statements

---

## Usage — POCO

**Producers**

| Transport | Sample |
|-----------|--------|
| SQL Server | [SQLServerProducer/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLServer/SQLServerProducer/Program.cs) |
| SQLite | [SQLiteProducer/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLite/SQLiteProducer/Program.cs) |
| Redis | [RedisProducer/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/Redis/RedisProducer/Program.cs) |
| PostgreSQL | [PostgreSQLProducer/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/PostgreSQL/PostgreSQLProducer/Program.cs) |
| LiteDb | [LiteDbProducer/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/LiteDb/LiteDbProducer/Program.cs) |

**Consumers**

| Transport | Sample |
|-----------|--------|
| SQL Server | [SQLServerConsumer/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLServer/SQLServerConsumer/Program.cs) |
| SQLite | [SQLiteConsumer/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLite/SQLiteConsumer/Program.cs) |
| Redis | [RedisConsumer/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/Redis/RedisConsumer/Program.cs) |
| PostgreSQL | [PostGreSQLConsumer/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/PostgreSQL/PostGreSQLConsumer/Program.cs) |
| LiteDb | [LiteDbConsumer/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/LiteDb/LiteDbConsumer/Program.cs) |

---

## Usage — LINQ Expressions

You can queue LINQ expressions to be executed instead of POCOs. This makes producers and consumers generic — they no longer need to be message-specific. The examples below are not transport-specific and assume any queue creation steps have already been performed.

> **Note:** It is possible for a producer to queue work that a consumer cannot process. In order for a consumer to execute a LINQ statement, all types must be resolvable. For dynamic statements, it is also possible to queue work that doesn't compile due to syntax errors — this won't be discovered until the consumer dequeues the work.

### Producer

> **Note:** When passing `message` or `workerNotification` as arguments to dynamic LINQ, you must cast them, as the internal compiler treats them as `object`. This is not necessary when using standard LINQ expressions.

```csharp
// Cast types when using dynamic LINQ:
(IReceivedMessage<MessageExpression>) message
(IWorkerNotification) workerNotification
```

[SQLiteProducerLinq/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLite/SQLiteProducerLinq/Program.cs)

When passing value types, you will need to parse them inline. For example, a `Guid` and an `int` can be embedded in string literals and parsed using built-in .NET methods:

```csharp
var id = Guid.NewGuid();
var runTime = 200;
$"(message, workerNotification) => StandardTesting.Run(new Guid(\"{id}\"), int.Parse(\"{runTime}\"))"
```

This produces a LINQ expression that can be compiled and executed by the consumer, provided it can resolve all referenced types.

### Consumer

The consumer is generic and can process any LINQ expression, but it must be able to resolve all types the expression references. You may need to wire up an assembly resolver if your DLLs cannot be located automatically.

- [AppDomain.AssemblyResolve (MSDN)](https://msdn.microsoft.com/en-us/library/system.appdomain.assemblyresolve(v=vs.110).aspx)
- [SQLiteConsumerLinq/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLite/SQLiteConsumerLinq/Program.cs)

### Security Considerations

No sandboxing or checking for risky commands is performed. For example, the following statement will cause the consumer host to exit:

```csharp
"(message, workerNotification) => Environment.Exit(0)"
```

If configuration files define dynamic LINQ statements, or if you cannot fully trust the producer, consider running the consumer in an application domain sandbox. Without that, the only protection against destructive commands is O/S user permissions:

```csharp
"(message, workerNotification) => System.IO.Directory.Delete(@\"C:\\Windows\\\", true)"
```

---

## Usage — Job Scheduler

Jobs may be scheduled using [Schyntax](https://github.com/schyntax/cs-schyntax) format. The scheduler and consumers are separate — schedulers only queue work, while standard LINQ consumers handle processing.

Any LINQ statement supported by a LINQ producer can be scheduled via the scheduler.

Multiple schedulers sharing the same schedule can be run for redundancy, but it is important that the clocks on all machines are in sync, or that the same time provider is injected into both schedulers and consumers. See the Wiki for more details.

> **Note:** Using multiple machines with out-of-sync clocks may produce unexpected results. Server-based transports often provide solutions for this. See the Wiki.

See [Schyntax](https://github.com/schyntax/schyntax) for the event scheduling format.

### Scheduler

The scheduler and its container must remain in scope for as long as you are scheduling work. Disposing or allowing the scheduler to go out of scope will stop all queuing.

[SQliteScheduler/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLite/SQliteScheduler/Program.cs)

To consume and process scheduled jobs, use a [LINQ Consumer](https://github.com/blehnen/DotNetWorkQueue/wiki/ConsumerLinq):

[SQLiteSchedulerConsumer/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLite/SQLiteSchedulerConsumer/Program.cs)

---

## Samples & Examples

- [**Samples**](https://github.com/blehnen/DotNetWorkQueue.Samples)
- [**More examples**](https://github.com/blehnen/DotNetWorkQueue.Examples/tree/master/Source/Examples)

---

## Building the Source

You'll need Visual Studio 2022/2026 (any edition) and the .NET 8.0 / 10.0 SDKs installed.

All references are either on NuGet or in the `\lib` folder. Building from Visual Studio should restore all required files automatically.

---

## License

Copyright © 2015–2026 Brian Lehnen

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 2.1 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with this program. If not, see [http://www.gnu.org/licenses/](http://www.gnu.org/licenses/).

---

## Third-Party Libraries

### DotNetWorkQueue

- [SimpleInjector](https://simpleinjector.org/index.html)
- [Polly](https://github.com/App-vNext/Polly)
- [Newtonsoft.Json](http://www.newtonsoft.com/json)
- [OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet)
- [Microsoft.Extensions.Caching.Memory](https://github.com/dotnet/runtime)
- [Microsoft.IO.RecyclableMemoryStream](https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream)
- Custom libraries in `/Lib`: [Schyntax](https://github.com/blehnen/cs-schyntax), [Aq.ExpressionJsonSerializer](https://github.com/blehnen/expression-json-serializer), [JpLabs.DynamicCode](http://jp-labs.blogspot.com/2008/11/dynamic-lambda-expressions-using.html)

### DotNetWorkQueue.Transport.Redis

- [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)
- [MsgPack-CLI](https://github.com/msgpack/msgpack-cli)
- Custom library in `/Lib`: [GuerrillaNTP](https://github.com/blehnen/GuerrillaNtp)

### DotNetWorkQueue.Transport.SqlServer

- [Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient)

### DotNetWorkQueue.Transport.SQLite

- [System.Data.SQLite](https://www.sqlite.org/)

### DotNetWorkQueue.Transport.PostgreSQL

- [Npgsql](http://www.npgsql.org/)

### DotNetWorkQueue.Transport.LiteDb

- [LiteDb](https://www.litedb.org/)

### DotNetWorkQueue.AppMetrics

- [AppMetrics](https://github.com/AppMetrics/AppMetrics)

### DotNetWorkQueue.Dashboard.Api

- [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)

### Unit / Integration Tests

- [xUnit](https://github.com/xunit/xunit)
- [NSubstitute](http://nsubstitute.github.io/)
- [AutoFixture](https://github.com/AutoFixture/AutoFixture)
- [FluentAssertions](http://www.fluentassertions.com/)
- [Tynamix.ObjectFiller](http://objectfiller.net/)

---

##### Developed with:

<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/ReSharper_icon.png" width="48"> <img src="https://resources.jetbrains.com/storage/products/company/brand/logos/dotCover_icon.png" width="48"> <img src="https://resources.jetbrains.com/storage/products/company/brand/logos/dotTrace_icon.png" width="48">
