DotNetWorkQueue
=========

[![License LGPLv2.1](https://img.shields.io/badge/license-LGPLv2.1-green.svg)](http://www.gnu.org/licenses/lgpl-2.1.html)

A producer / distributed consumer library for dot net applications. Available transports are:

* SQL Server
* SQLite
* Redis
* PostGre

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

* NuGet package [DotNetWorkQueue.Transports.Redis](https://www.nuget.org/packages/DotNetWorkQueue.Transport.Redis/)
* NuGet package [DotNetWorkQueue.Transports.SqlServer](https://www.nuget.org/packages/DotNetWorkQueue.Transport.SqlServer/)
* NuGet package [DotNetWorkQueue.Transports.SQLite](https://www.nuget.org/packages/DotNetWorkQueue.Transport.SQLite/)
* NuGet package [DotNetWorkQueue.Transports.PostgreSQL] - not yet published

Metrics

* NuGet package [DotNetWorkQueue.Metrics.Net](https://www.nuget.org/packages/DotNetWorkQueue.Metrics.Net/)

Usage - POCO
------

[**Message**]
```csharp
public class SimpleMessage
{
	public string Message { get; set; }
}
```

[**Producer - Sql server**]

```csharp
//Create the queue if it doesn't exist
var queueName = "testing";
var connectionString = "Server=V-SQL;Application Name=SQLProducer;Database=TestR;Trusted_Connection=True;";
using (var createQueueContainer = new QueueCreationContainer<SqlServerMessageQueueInit>())
{
	using (var createQueue = createQueueContainer.GetQueueCreation<SqlServerMessageQueueCreation>(queueName, connectionString))
	{
		if (!createQueue.QueueExists)
		{
			createQueue.CreateQueue();
		}
	}
}

using (var queueContainer = new QueueContainer<SqlServerMessageQueueInit>())
{
	using (var queue = queueContainer.CreateProducer<SimpleMessage>(queueName, connectionString))
    {
		queue.Send(new SimpleMessage {Message = "Hello World"});
    }
}
```

[**Producer - SQLite**]

```csharp
//Create the queue if it doesn't exist
var queueName = "testing";
var connectionString = @"Data Source=c:\queue.db;Version=3;";
using (var createQueueContainer = new QueueCreationContainer<SqLiteMessageQueueInit>())
{
	using (var createQueue = createQueueContainer.GetQueueCreation<SqLiteMessageQueueCreation>(queueName, connectionString))
    {
    	if (!createQueue.QueueExists)
        {
        	createQueue.CreateQueue();
        }
    }
 }

using (var queueContainer = new QueueContainer<SqLiteMessageQueueInit>())
{
	using (var queue = queueContainer.CreateProducer<SimpleMessage>(queueName, connectionString))
    {
    	queue.Send(new SimpleMessage { Message = "Hello World" });
    }
}
```

[**Producer - Redis**]
```csharp
var queueName = "example";
var connectionString = "192.168.0.212";
using (var queueContainer = new QueueContainer<RedisQueueInit>())
{
	using (var queue = queueContainer.CreateProducer<SimpleMessage.SimpleMessage>(queueName, connectionString))
	{
		queue.Send(new SimpleMessage.SimpleMessage{Message = "hello world"});
	}
}
```

[**Producer - PostGre**]

```csharp
//Create the queue if it doesn't exist
var queueName = "testing";
var connectionString = "Server=V-SQL;Port=5432;Database=IntegrationTesting;Integrated Security=true;";
using (var createQueueContainer = new QueueCreationContainer<PostgreSqlMessageQueueInit>())
{
	using (var createQueue = createQueueContainer.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName, connectionString))
	{
		if (!createQueue.QueueExists)
		{
			createQueue.CreateQueue();
		}
	}
}

using (var queueContainer = new QueueContainer<PostgreSqlMessageQueueInit>())
{
	using (var queue = queueContainer.CreateProducer<SimpleMessage>(queueName, connectionString))
    {
		queue.Send(new SimpleMessage {Message = "Hello World"});
    }
}
```

[**Consumer - Sql server**]

```csharp
using (var queueContainer = new QueueContainer<SqlServerMessageQueueInit>())
{
	using (var queue = queueContainer.CreateConsumer(queueName, connectionString))
    {
		queue.Start<SimpleMessage>(HandleMessages);
		Console.WriteLine("Processing messages - press any key to stop");
        Console.ReadKey((true));
    }
}

private void HandleMessages(IReceivedMessage<SimpleMessage> message, IWorkerNotification notifications)
{
	notifications.Log.Log(DotNetWorkQueue.Logging.LogLevel.Debug, () => $"Processing Message {message.Body.Message}");
}

```

[**Consumer - SQLite**]

```csharp
using (var queueContainer = new QueueContainer<SqLiteMessageQueueInit>())
{
	using (var queue = queueContainer.CreateConsumer(queueName, connectionString))
    {
    	queue.Start<SimpleMessage>(HandleMessages);
        Console.WriteLine("Processing messages - press any key to stop");
        Console.ReadKey((true));
    }
}

private void HandleMessages(IReceivedMessage<SimpleMessage> message, IWorkerNotification notifications)
{
	notifications.Log.Log(DotNetWorkQueue.Logging.LogLevel.Debug, () => $"Processing Message {message.Body.Message}");
}

```

[**Consumer - Redis**]
```csharp
using (var queueContainer = new QueueContainer<RedisQueueInit>())
{
	using (var queue = queueContainer.CreateConsumer(queueName, connectionString))
    {
		queue.Start<SimpleMessage.SimpleMessage>(HandleMessages);
        Console.WriteLine("Processing messages - press any key to stop");
        Console.ReadKey((true));
    }
}

private void HandleMessages(IReceivedMessage<SimpleMessage> message, IWorkerNotification notifications)
{
	notifications.Log.Log(DotNetWorkQueue.Logging.LogLevel.Debug, () => $"Processing Message {message.Body.Message}");
}
```

[**Consumer - PostGre**]

```csharp
using (var queueContainer = new QueueContainer<PostgreSqlMessageQueueInit>())
{
	using (var queue = queueContainer.CreateConsumer(queueName, connectionString))
    {
		queue.Start<SimpleMessage>(HandleMessages);
		Console.WriteLine("Processing messages - press any key to stop");
        Console.ReadKey((true));
    }
}

private void HandleMessages(IReceivedMessage<SimpleMessage> message, IWorkerNotification notifications)
{
	notifications.Log.Log(DotNetWorkQueue.Logging.LogLevel.Debug, () => $"Processing Message {message.Body.Message}");
}
```


Usage - Linq Expression
------

You can choose to send Linq expressions to be executed instead. This has some advantages, as the producers and consumers are generic; they no longer need to be message specific. The below examples are not transport specifc and assume that any queue creation steps have already been performed.

NOTE: It's possbile for a producer to queue up work that a consumer cannot process. In order for a consumer to execute the Linq statement, all types must be resolvable. For dynamic statements, it's also possible to queue up work that doesn't compile due to syntax errors. That won't be discovered until the consumer dequeues the work.

####Example#####

[**Shared Classes**]
Note that the producer needs references to shared resources when using standard Linq statements. However, if you are using dyanmic linq statements (expressed as a string), the producer does not need any references to the types being executed.

```csharp

Assembly: ProducerMethodTestingClasses.dll
NameSpace: ProducerMethodTestingClasses

public class TestCompile
{
	public void RunMe(IReceivedMessage<MessageExpression> message, IWorkerNotification workNotification)
    {
    	Console.WriteLine(message.MessageId.Id.Value);
    }
}

public class TestClass
{
	public void RunMe(IWorkerNotification workNotification, string input1, int input2, SomeInput moreInput)
	{
		var sb = new StringBuilder();
		sb.Append(input1);
        sb.Append(" ");
		sb.Append(input2);
        sb.Append(" ");
		sb.AppendLine(moreInput.Message);
		Console.Write(sb.ToString());
	}
}

public class SomeInput
{
	public SomeInput()
    {
    }

    public SomeInput(string message)
    {
    	Message = message;
    }

	public string Message { get; set; }
	public override string ToString()
    {
   	 	return Message;
    }
}
```

[**Producer**]
NOTE - if passing in the message or worker notifications as arguments to dynamic linq, you must cast them. The internal compiler treats those as objects. You can see this syntax in the examples below. That's not nessasry if using standard Linq expressions.

```csharp
Message
(IReceivedMessage<MessageExpression>)

WorkerNotification
(IWorkerNotification)
```

```csharp
using (var queueContainer = new QueueContainer<AnyTransport>())
{
	using (var queue = queueContainer.CreateMethodProducer(queueName, connectionString))
     {
		//send a standard linq expression
        queue.Send((message, workerNotification) => Console.WriteLine(message.MessageId.Id.Value));

		//send another standard linq expression
		queue.Send((message, workerNotification) => new TestClass().RunMe(
              workerNotification,
              "a string",
              2,
              new SomeInput(DateTime.UtcNow.ToString())));

       //send a dynamic expression. The consumer will attempt to compile and run this.
       //The producer will perform no checks on this expression.
       //note that you must indicate which additional assemblies and name spaces
       //are needed to compile your expression or dependent classes.
       queue.Send(new LinqExpressionToRun(
        "(message, workerNotification) => new TestClass().RunMe((IWorkerNotification)workerNotification, \"dynamic\", 2, new SomeInput(DateTime.UtcNow.ToString()))",
          new List<string> { "ProducerMethodTestingClasses.dll" }, //additional references
          new List<string> { "ProducerMethodTestingClasses" })); //additional using statements

       //send another dynamic expression
       queue.Send(new LinqExpressionToRun(
        "(message, workerNotification) => new TestCompile().RunMe((IReceivedMessage<MessageExpression>)message, (IWorkerNotification)workerNotification)",
          new List<string> { "ProducerMethodTestingClasses.dll" },
          new List<string> { "ProducerMethodTestingClasses" }));
	}
}

```

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

```csharp
//NOTE - assumed that scheduler factory has already been created
using (var queueContainer = new QueueContainer<AnyTransport>())
{
	using (var queue = queueContainer.CreateConsumerMethodQueueScheduler(queueName,
       connectionString, factory))
    {
        queue.Start();
        Console.WriteLine("Processing messages - press any key to stop");
        Console.ReadKey(true);
     }
}
```

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

```csharp
using (var jobContainer = new JobSchedulerContainer(QueueCreation))
{
	using (var scheduler = jobContainer.CreateJobScheduler())
    {
    	//events for job added, job add exception and job add failure
        scheduler.OnJobQueueException += SchedulerOnOnJobQueueException;
        scheduler.OnJobQueue += SchedulerOnOnJobEnQueue;
        scheduler.OnJobNonFatalFailureQueue += SchedulerOnOnJobNonFatalFailureEnQueue;

        scheduler.AddUpdateJob<SqlServerMessageQueueInit, SqlServerJobQueueCreation>("test job1",
        	"sampleSQL",
        	connectionString,
            "sec(0,5,10,15,20,25,30,35,40,45,50,55)",
    		(message, workerNotification) => Console.WriteLine(message.MessageId.Id.Value));

       scheduler.AddUpdateJob<RedisQueueInit, RedisJobQueueCreation>("test job2",
       "sampleRedis",
       "192.168.0.212",
       "second(0,15,30,45)",
     	new LinqExpressionToRun("(message, workerNotification) => System.Threading.Thread.Sleep(20000)"));

        scheduler.AddUpdateJob<SqLiteMessageQueueInit, SqliteJobQueueCreation>("test job3",
        "sampleSqlite",
        connectionStringSqlite,
        "sec(0,5,10,15,20,25,30,35,40,45,50,55)",
        (message, workerNotification) => Console.WriteLine(message.MessageId.Id.Value));

		//start may be called before or after adding jobs
        scheduler.Start();
        Console.WriteLine("Running - press any key to stop");
        Console.ReadKey(true);
	}
}

private static void SchedulerOnOnJobNonFatalFailureEnQueue(IScheduledJob scheduledJob, IJobQueueOutputMessage jobQueueOutputMessage)
{
}

private static void SchedulerOnOnJobEnQueue(IScheduledJob scheduledJob, IJobQueueOutputMessage jobQueueOutputMessage)
{
}

private static void SchedulerOnOnJobQueueException(IScheduledJob scheduledJob, Exception error)
{
}

```

To consume / process scheduled jobs, a [Linq Consumer ](https://github.com/blehnen/DotNetWorkQueue/wiki/ConsumerLinq) is used

[**More examples**](https://github.com/blehnen/DotNetWorkQueue/tree/master/Source/Examples)
------

Building the source
---------------------

To build the assembles run one of the following

```
build-debug
```

or

```
build-release
```

ILMerge is used to merge dependanices into a final assembly. These merged assemblies can be found in \MergedBuild

License
--------
Copyright � 2016 Brian Lehnen

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

* [ILMerge ](http://research.microsoft.com/en-us/people/mbarnett/ILMerge.aspx)

* [LibLog ](https://github.com/damianh/LibLog)

* [NetFX-Guard ](http://netfx.codeplex.com/)

* [SimpleInjector ](https://simpleinjector.org/index.html)

* [Microsoft.IO.RecyclableMemoryStream ](https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream)

* [Newtonsoft.Json ](http://www.newtonsoft.com/json)

* [SmartThreadPool ](https://github.com/amibar/SmartThreadPool)

* [JpLabs.DynamicCode ](http://jp-labs.blogspot.com/2008/11/dynamic-lambda-expressions-using.html)

* [JsonNet.PrivateSetterContractResolvers ](https://github.com/danielwertheim/jsonnet-privatesetterscontractresolvers)

* [Expression-JSON-Serializer ](https://github.com/aquilae/expression-json-serializer)*

* [Schtick](https://github.com/schyntax/cs-schtick)

* [Schyntax ](https://github.com/schyntax/cs-schyntax)

*A fork of this module was created and thread saftey changes made. This is the version that is being used. It can be found here [Expression-JSON-Serializer ](https://github.com/blehnen/expression-json-serializer)

[**DotNetWorkQueue.Transport.Redis**]

* [GuerrillaNTP ](https://bitbucket.org/robertvazan/guerrillantp)

* [MsgPack-CLI* ](https://github.com/msgpack/msgpack-cli)

* [StackExchange.Redis ](https://github.com/StackExchange/StackExchange.Redis)

*This module cannot be merged via ILMerge - causes serialization failures

[**DotNetWorkQueue.Transport.SqlServer**]

None

[**DotNetWorkQueue.Transport.SQLite**]

* [SQLite ](https://www.sqlite.org/)

[**DotNetWorkQueue.Transport.PostgreSQL**]

* [Npgsql ](http://www.npgsql.org/)

[**DotNetWorkQueue.Metrics.Net**]

* [Metrics.net ](https://github.com/Recognos/Metrics.NET)

[**Unit / Integration Tests**]

* [AutoFixture ](https://github.com/AutoFixture/AutoFixture)

* [CompareNetObjects ](http://comparenetobjects.codeplex.com/)

* [FluentAssertions ](http://www.fluentassertions.com/)

* [nsubstitute ](http://nsubstitute.github.io/)

* [ObjectFiller ](http://objectfiller.net/)

* [Xunit ](https://github.com/xunit/xunit)

##### Developed with:

[![Resharper](http://neventstore.org/images/logo_resharper_small.gif)](http://www.jetbrains.com/resharper/)
[![dotCover](http://neventstore.org/images/logo_dotcover_small.gif)](http://www.jetbrains.com/dotcover/)
[![dotTrace](http://neventstore.org/images/logo_dottrace_small.gif)](http://www.jetbrains.com/dottrace/)

