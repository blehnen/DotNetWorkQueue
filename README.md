DotNetWorkQueue
=========

[![License LGPLv2.1](https://img.shields.io/badge/license-LGPLv2.1-green.svg)](http://www.gnu.org/licenses/lgpl-2.1.html)

A producer / consumer library for dot net applications. Available transports are SQL server and Redis.

See the wiki for more indepth documention (https://github.com/blehnen/DotNetWorkQueue/wiki)

Installation
-------------

TODO - not yet published to nuget.

Usage
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
	notifications.Log.Debug($"Processing Message {message.Body.Message}");
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
	notifications.Log.Debug($"Processing Message {message.Body.Message}");
}

```
	
[**More examples**] (https://github.com/blehnen/DotNetWorkQueue/tree/master/Source/Examples)
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
Copyright © 2015 Brian Lehnen

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

ILMerge (http://research.microsoft.com/en-us/people/mbarnett/ILMerge.aspx)

LibLog (https://github.com/damianh/LibLog)

NetFX-Guard (http://netfx.codeplex.com/)

SimpleInjector (https://simpleinjector.org/index.html)

Microsoft.IO.RecyclableMemoryStream (https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream)

NetwtonSoft.JSON (http://www.newtonsoft.com/json)

SmartThreadPool (https://github.com/amibar/SmartThreadPool)


[**DotNetWorkQueue.Transport.Redis**]

GuerrillaNTP (https://bitbucket.org/robertvazan/guerrillantp)

MsgPack-CLI* (https://github.com/msgpack/msgpack-cli)

StackExchange.Redis (https://github.com/StackExchange/StackExchange.Redis)

*This module cannot be merged via ILMerge - causes serialization failures

[**DotNetWorkQueue.Transport.SqlServer**]

None

[**DotNetWorkQueue.Metrics.Net**]

Metrics.net (https://github.com/etishor/Metrics.NET)

[**Unit / Integration Tests**]

AutoFixture (https://github.com/AutoFixture/AutoFixture)

CompareNetObjects (http://comparenetobjects.codeplex.com/)

FluentAssertions (http://www.fluentassertions.com/)

nsubstitute (http://nsubstitute.github.io/)

ObjectFiler (http://objectfiller.net/)

Xunit (https://github.com/xunit/xunit)

