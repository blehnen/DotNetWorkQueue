# DotNetWorkQueue

[![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.svg)](https://www.nuget.org/packages/DotNetWorkQueue)
[![Release](https://img.shields.io/github/v/release/blehnen/DotNetWorkQueue?sort=semver)](https://github.com/blehnen/DotNetWorkQueue/releases)
[![License LGPLv2.1](https://img.shields.io/badge/license-LGPLv2.1-green.svg)](http://www.gnu.org/licenses/lgpl-2.1.html)
[![CI](https://github.com/blehnen/DotNetWorkQueue/actions/workflows/ci.yml/badge.svg)](https://github.com/blehnen/DotNetWorkQueue/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/blehnen/DotNetWorkQueue/branch/master/graph/badge.svg?token=E23UZ6U9CU)](https://codecov.io/gh/blehnen/DotNetWorkQueue)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=blehnen_DotNetWorkQueue&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=blehnen_DotNetWorkQueue)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=blehnen_DotNetWorkQueue&metric=bugs)](https://sonarcloud.io/summary/new_code?id=blehnen_DotNetWorkQueue)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=blehnen_DotNetWorkQueue&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=blehnen_DotNetWorkQueue)

A producer / distributed consumer library for .NET applications. Targets .NET 8.0 and .NET 10.0.

**High-level features:**
- Queue / de-queue POCOs for distributed processing
- Queue / process compiled LINQ expressions
- Re-occurring job scheduler
- Transactional outbox pattern on SqlServer / PostgreSQL (see [`docs/outbox-pattern.md`](docs/outbox-pattern.md))

See the [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki) for in-depth documentation.

---

## Installation

| Package | Description | NuGet |
|---------|-------------|-------|
| DotNetWorkQueue | Core library with all abstractions, interfaces, and default implementations | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue)](https://www.nuget.org/packages/DotNetWorkQueue/) |

**Transports**

| Package | Description | NuGet |
|---------|-------------|-------|
| DotNetWorkQueue.Transport.SqlServer | SQL Server transport using Microsoft.Data.SqlClient | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.SqlServer)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.SqlServer/) |
| DotNetWorkQueue.Transport.PostgreSQL | PostgreSQL transport using Npgsql | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.PostgreSQL)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.PostgreSQL/) |
| DotNetWorkQueue.Transport.Redis | Redis transport using StackExchange.Redis | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.Redis)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.Redis/) |
| DotNetWorkQueue.Transport.SQLite | SQLite transport using System.Data.SQLite | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.SQLite)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.SQLite/) |
| DotNetWorkQueue.Transport.LiteDb | LiteDB embedded NoSQL transport | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.LiteDb)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.LiteDb/) |
| DotNetWorkQueue.Transport.Memory | In-memory transport for testing and lightweight scenarios | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Transport.Memory)](https://www.nuget.org/packages/DotNetWorkQueue.Transport.Memory/) |

**Dashboard**

| Package | Description | NuGet |
|---------|-------------|-------|
| DotNetWorkQueue.Dashboard.Api | REST API for monitoring and managing queues across all transports. Queue status, message counts, error tracking, consumer tracking, and admin actions. ASP.NET Core with Swagger/OpenAPI. | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Dashboard.Api)](https://www.nuget.org/packages/DotNetWorkQueue.Dashboard.Api/) |
| DotNetWorkQueue.Dashboard.Ui | Blazor Server web UI (MudBlazor) for browsing queues, viewing messages, managing errors, and monitoring consumers. Connects to the Dashboard API. | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Dashboard.Ui)](https://www.nuget.org/packages/DotNetWorkQueue.Dashboard.Ui/) |
| DotNetWorkQueue.Dashboard.Client | Typed API client and consumer registration client. Sends heartbeats with running metric counters (processed, errors, rollbacks, poison messages). No dependency on the core library — HttpClient and System.Text.Json only. | [![NuGet](https://img.shields.io/nuget/v/DotNetWorkQueue.Dashboard.Client)](https://www.nuget.org/packages/DotNetWorkQueue.Dashboard.Client/) |

**Docker**

A pre-built Docker image runs both the Dashboard UI and API in one container, so you don't have to run the API separately:

```bash
docker pull blehnen74/dotnetworkqueue-dashboard:latest
docker run -d -p 8080:8080 -v "$(pwd)/appsettings.json:/app/appsettings.json:ro" blehnen74/dotnetworkqueue-dashboard:latest
```

See [Docker Hub](https://hub.docker.com/r/blehnen74/dotnetworkqueue-dashboard) for tags and [docker/dashboard/README.md](docker/dashboard/README.md) for full configuration (authentication, API keys, SQLite/LiteDB volume mounts, Docker Compose).

---

> **Metrics:** Built-in metrics are provided via `System.Diagnostics.Metrics` in the core library. No additional package is needed. To collect and export metrics, configure [OpenTelemetry.Metrics](https://github.com/open-telemetry/opentelemetry-dotnet) in your host application. A sample [Grafana dashboard](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/main/grafana-dashboard.json) (Prometheus data source) is available — import it into Grafana to visualize queue metrics.

---

## Usage — POCO

| Transport | Producer | Consumer |
|-----------|----------|----------|
| SQL Server | [SQLServerProducer](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLServer/SQLServerProducer/Program.cs) | [SQLServerConsumer](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLServer/SQLServerConsumer/Program.cs) |
| PostgreSQL | [PostgreSQLProducer](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/PostgreSQL/PostgreSQLProducer/Program.cs) | [PostgreSQLConsumer](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/PostgreSQL/PostGreSQLConsumer/Program.cs) |
| Redis | [RedisProducer](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/Redis/RedisProducer/Program.cs) | [RedisConsumer](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/Redis/RedisConsumer/Program.cs) |
| SQLite | [SQLiteProducer](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLite/SQLiteProducer/Program.cs) | [SQLiteConsumer](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLite/SQLiteConsumer/Program.cs) |
| LiteDb | [LiteDbProducer](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/LiteDb/LiteDbProducer/Program.cs) | [LiteDbConsumer](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/LiteDb/LiteDbConsumer/Program.cs) |

---

## Usage — LINQ Expressions

You can queue LINQ expressions to be executed instead of POCOs. This makes producers and consumers generic — they no longer need to be message-specific. The examples below are not transport-specific and assume any queue creation steps have already been performed.

> **Note:** It is possible for a producer to queue work that a consumer cannot process. For a consumer to execute a LINQ expression, all referenced types must be resolvable.

[SQLiteProducerLinq/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLite/SQLiteProducerLinq/Program.cs)

### Consumer

The consumer is generic and can process any LINQ expression, but it must be able to resolve all types the expression references.

- [SQLiteConsumerLinq/Program.cs](https://github.com/blehnen/DotNetWorkQueue.Samples/blob/master/Source/Samples/SQLite/SQLiteConsumerLinq/Program.cs)

---

## Usage — Job Scheduler

Jobs are scheduled using standard cron expressions (5-field or 6-field with seconds), parsed by [Cronos](https://github.com/HangfireIO/Cronos). The scheduler and consumers are separate; schedulers only queue work, while standard LINQ consumers handle processing.

Any LINQ statement supported by a LINQ producer can be scheduled via the scheduler.

Multiple schedulers sharing the same schedule can be run for redundancy, but it is important that the clocks on all machines are in sync, or that the same time provider is injected into both schedulers and consumers. See the Wiki for more details.

> **Note:** Using multiple machines with out-of-sync clocks may produce unexpected results. Server-based transports often provide solutions for this. See the Wiki.

See [crontab.guru](https://crontab.guru/) for the cron expression format. 6-field expressions (with seconds) are also supported.

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

All references are on NuGet. Building from Visual Studio should restore all required packages automatically.

---

## License

Copyright © 2015–2026 Brian Lehnen

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 2.1 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with this program. If not, see [http://www.gnu.org/licenses/](http://www.gnu.org/licenses/).

---

## Third-Party Libraries

**Core (DotNetWorkQueue):** [SimpleInjector](https://simpleinjector.org/index.html), [Polly](https://github.com/App-vNext/Polly), [Newtonsoft.Json](http://www.newtonsoft.com/json), [OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet), [Microsoft.Extensions.Caching.Memory](https://github.com/dotnet/runtime), [Microsoft.IO.RecyclableMemoryStream](https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream), [GuerrillaNtp](https://guerrillantp.machinezoo.com/)

**Scheduling:** [Cronos](https://github.com/HangfireIO/Cronos) (cron parsing), [CronExpressionDescriptor](https://github.com/bradymholt/cron-expression-descriptor) (human-readable descriptions)

| Package | Dependencies |
|---------|-------------|
| Transport.SqlServer | [Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient) |
| Transport.PostgreSQL | [Npgsql](http://www.npgsql.org/) |
| Transport.Redis | [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis), [MsgPack-CLI](https://github.com/msgpack/msgpack-cli) |
| Transport.SQLite | [System.Data.SQLite](https://www.sqlite.org/) |
| Transport.LiteDb | [LiteDb](https://www.litedb.org/) |
| Dashboard.Api | [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) |

**Tests:** [MSTest](https://github.com/microsoft/testfx), [NSubstitute](http://nsubstitute.github.io/), [AutoFixture](https://github.com/AutoFixture/AutoFixture), [FluentAssertions](http://www.fluentassertions.com/), [Tynamix.ObjectFiller](http://objectfiller.net/)

---

##### Developed with:

<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/ReSharper_icon.png" width="48"> <img src="https://resources.jetbrains.com/storage/products/company/brand/logos/dotCover_icon.png" width="48"> <img src="https://resources.jetbrains.com/storage/products/company/brand/logos/dotTrace_icon.png" width="48">
