DotNetWorkQueue
=========

[![License LGPLv2.1](https://img.shields.io/badge/license-LGPLv2.1-green.svg)](http://www.gnu.org/licenses/lgpl-2.1.html)
[![Build status](https://ci.appveyor.com/api/projects/status/vqqq9m0j9xodbfof/branch/master?svg=true)](https://ci.appveyor.com/project/blehnen/dotnetworkqueue/branch/master)
[![Coverity status](https://scan.coverity.com/projects/10126/badge.svg)](https://scan.coverity.com/projects/blehnen-dotnetworkqueue)
[![codecov](https://codecov.io/gh/blehnen/DotNetWorkQueue/branch/master/graph/badge.svg?token=E23UZ6U9CU)](https://codecov.io/gh/blehnen/DotNetWorkQueue)

A producer / distributed consumer library for dot net applications. Dot net 4.6.1, 4.7.2, 4.8, 6.0 and Dot net standard 2.0 are supported

High level features

* Queue / De-queue POCO for distributed processing
* Queue / Process LINQ statements; compiled or dyanamic (expressed as string)
* Re-occurring job scheduler

See the [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki) for more indepth documention

Installation
-------------

Base
* NuGet package [DotNetWorkQueue](https://www.nuget.org/packages/DotNetWorkQueue/)

Transports

* NuGet package [DotNetWorkQueue.Transport.Redis](https://www.nuget.org/packages/DotNetWorkQueue.Transport.Redis/)
* NuGet package [DotNetWorkQueue.Transport.SqlServer](https://www.nuget.org/packages/DotNetWorkQueue.Transport.SqlServer/)
* NuGet package [DotNetWorkQueue.Transport.SQLite](https://www.nuget.org/packages/DotNetWorkQueue.Transport.SQLite/)
* NuGet package [DotNetWorkQueue.Transport.PostgreSQL](https://www.nuget.org/packages/DotNetWorkQueue.Transport.PostgreSQL/)
* NuGet package [DotNetWorkQueue.Transport.LiteDb](https://www.nuget.org/packages/DotNetWorkQueue.Transport.LiteDb/)

Metrics

* NuGet package [DotNetWorkQueue.AppMetrics](https://www.nuget.org/packages/DotNetWorkQueue.AppMetrics/)

Differences between versions
------------

Dot net standard 2.0 / 6.0 are missing the following features from the full framework versions

- No support for aborting threads when stopping the consumer queues
- No support for dynamic linq statements

Usage - POCO
------

[**Producer - Sql server**]


https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/SQLServer/SQLServerProducer/Program.cs

[**Producer - SQLite**]

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/SQLite/SQLiteProducer/Program.cs

[**Producer - Redis**]

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/Redis/RedisProducer/Program.cs

[**Producer - PostgreSQL**]

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/PostgreSQL/PostgreSQLProducer/Program.cs


[**Producer - LiteDb**]

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/LiteDb/LiteDbProducer/Program.cs

[**Consumer - Sql server**]

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/SQLServer/SQLServerConsumer/Program.cs

[**Consumer - SQLite**]

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/SQLite/SQLiteConsumer/Program.cs

[**Consumer - Redis**]

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/Redis/RedisConsumer/Program.cs

[**Consumer - PostgreSQL**]

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/PostgreSQL/PostGreSQLConsumer/Program.cs

[**Consumer - LiteDb**]

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/LiteDb/LiteDbConsumer/Program.cs

Usage - Linq Expression
------

You can choose to send Linq expressions to be executed instead. This has some advantages, as the producers and consumers are generic; they no longer need to be message specific. The below examples are not transport specifc and assume that any queue creation steps have already been performed.

NOTE: It's possbile for a producer to queue up work that a consumer cannot process. In order for a consumer to execute the Linq statement, all types must be resolvable. For dynamic statements, it's also possible to queue up work that doesn't compile due to syntax errors. That won't be discovered until the consumer dequeues the work.

####Example#####

[**Producer**]
NOTE - if passing in the message or worker notifications as arguments to dynamic linq, you must cast them. The internal compiler treats those as objects. You can see this syntax in the examples below. That's not nessasry if using standard Linq expressions.

```csharp
Message
(IReceivedMessage<MessageExpression>)

WorkerNotification
(IWorkerNotification)
```

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/SQLite/SQLiteProducerLinq/Program.cs

If you are passing value types, you will need to parse them. Here is an example.
The Guid and the int are both inside string literials and parsed via the built in dot.net methods.

```csharp
var id = Guid.NewGuid();
var runTime = 200;
$"(message, workerNotification) => StandardTesting.Run(new Guid(\"{id}\"), int.Parse(\"{runTime}\"))"
```

This will produce a linq expression that can be compiled and executed by the consumer, assuming that it can resolve all of the types.


[**Consumer**]
The consumer is generic; it can process any linq expression. However, it must be able to resolve all types that the linq expression uses. You may need to wire up an assembly resolver if your DLL's cannot be located.

https://msdn.microsoft.com/en-us/library/system.appdomain.assemblyresolve(v=vs.110).aspx

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/SQLite/SQLiteConsumerLinq/Program.cs

The above queue will process all Linq statements sent to the specified connection / queue.

[**Considerations**]

No sandboxing or checking for risky commands is performed. For instance, the below statement will cause your consumer host to exit.

```csharp
"(message, workerNotification) => Environment.Exit(0)"
```

If you decide to allow configuration files to define dyanmic Linq statements (or if you cannot trust the producer), you should consider running the consumer in an application domain sandbox. Otherwise, the only thing stopping a command like the following from executing would be O/S user permissions.

```csharp
"(message, workerNotification) => System.IO.Directory.Delete(@"C:\Windows\, true)"
```

Usage - Job Scheduler
------

Jobs may be scheduled using [Schyntax ](https://github.com/schyntax/cs-schyntax) format. The scheduler and consumers are seperate; schedulers don't process any work, they queue it for processing by a consumer.  The standard LINQ consumers are used to process work enqueued by a scheduler / schedule.

Any LINQ statement that a linq producer supports can be scheduled using the scheduler.

Multiple schedulers with the same schedule may be ran if needed for redundancy. However, it's important that the clocks on the machines are in sync, or that the same time provider is injected into the schedulers and consumers. See the WIKI for more information on this.

Generally speaking, you may get funny results if you are using multiple machines and the clocks are not in sync. The server based transports tend to provide solutions for this if you can't sync the clocks of the local machines; see the WIKI.

See [Schyntax ](https://github.com/schyntax/schyntax) for event scheduling format.

[**Scheduler**]

The scheduler and container must be kept in scope until you are done scheduling work or shutting down. No work will be queued if the scheduler is disposed or falls out of scope.

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/SQLite/SQliteScheduler/Program.cs

To consume / process scheduled jobs, a [Linq Consumer ](https://github.com/blehnen/DotNetWorkQueue/wiki/ConsumerLinq) is used

https://github.com/blehnen/DotNetWorkQueue/blob/master/Source/Samples/SQLite/SQLiteSchedulerConsumer/Program.cs

---------------------
[**Samples**](https://github.com/blehnen/DotNetWorkQueue/tree/master/Source/Samples)

[**More examples**](https://github.com/blehnen/DotNetWorkQueue/tree/master/Source/Examples)
------

Building the source
---------------------

You'll need VS2022 (any version) and you'll also need to install the dot net core 2.0/6.0 SDKs

All references are either in NuGet or the \lib folder - building from Visual studio should restore any needed files.

License
--------
Copyright � 2015-2022 Brian Lehnen

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 2.1 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with this program.  If not, see [http://www.gnu.org/licenses/](http://www.gnu.org/licenses).

3rd party Libraries
--------

This library uses multiple 3rd party libaries, listed below.

[**DotNetWorkQueue**]

* [LibLog ](https://github.com/damianh/LibLog)

* [NetFX-Guard ](http://netfx.codeplex.com/)

* [SimpleInjector ](https://simpleinjector.org/index.html)

* [Microsoft.IO.RecyclableMemoryStream ](https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream)

* [Newtonsoft.Json ](http://www.newtonsoft.com/json)

* [JpLabs.DynamicCode ](http://jp-labs.blogspot.com/2008/11/dynamic-lambda-expressions-using.html)

* [JsonNet.PrivateSetterContractResolvers ](https://github.com/danielwertheim/jsonnet-privatesetterscontractresolvers)

* [Expression-JSON-Serializer ](https://github.com/blehnen/expression-json-serializer)

* [Schtick](https://github.com/schyntax/cs-schtick)

* [Schyntax ](https://github.com/blehnen/cs-schyntax)

* [Polly ](https://github.com/App-vNext/Polly)

[**DotNetWorkQueue.Transport.Redis**]

* [GuerrillaNTP ](https://github.com/blehnen/GuerrillaNtp)

* [MsgPack-CLI ](https://github.com/msgpack/msgpack-cli)

* [StackExchange.Redis ](https://github.com/StackExchange/StackExchange.Redis)


[**DotNetWorkQueue.Transport.SqlServer**]

* None

[**DotNetWorkQueue.Transport.SQLite**]

* [System.Data.SQLite ](https://www.sqlite.org/)

[**DotNetWorkQueue.Transport.SQLite.Microsoft**]

* [Microsoft.Data.Sqlite ](https://github.com/aspnet/Microsoft.Data.Sqlite)

[**DotNetWorkQueue.Transport.PostgreSQL**]

* [Npgsql ](http://www.npgsql.org/)

[**DotNetWorkQueue.Transport.LiteDb**]

* [LiteDb ](https://www.litedb.org/)

[**DotNetWorkQueue.AppMetrics**]

* [AppMetrics ](https://github.com/AppMetrics/AppMetrics)

[**Unit / Integration Tests**]

* [AutoFixture ](https://github.com/AutoFixture/AutoFixture)

* [CompareNetObjects ](http://comparenetobjects.codeplex.com/)

* [FluentAssertions ](http://www.fluentassertions.com/)

* [nsubstitute ](http://nsubstitute.github.io/)

* [ObjectFiller ](http://objectfiller.net/)

* [Xunit ](https://github.com/xunit/xunit)

##### Developed with:

<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/ReSharper_icon.png" width="48">

<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/dotCover_icon.png" width="48">

<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/dotTrace_icon.png" width="48">
