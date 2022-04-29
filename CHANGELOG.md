###0.6.6 2022-04-29
* Fix issue with custom default constraints in the SQL server transport

###0.6.5 2022-02-06
* relational database tranports now allow additional columns to be used as part of the de-queue

###0.6.4 2022-01-12
* ILogger will now be created using the queue name for the category

###0.6.3 2022-01-11
* Remove Polly Bulkhead; Does not correctly work with our task limited scheduler
* Remove 'MaxQueue' feature from async processing, as it depended on Polly Bulkheads
* Switch to ILogger from Microsoft.Extensions.Logging.Abstractions

###0.6.2 2021-12-19
* Add dot net 6.0 as a target

###0.6.1 2021-09-28
* The producer will throw an exception on a non-public class used as a message. This is a limitation due to how the queue internally handles the delegate; it cannot correctly create internal/private classes

###0.6.0 2021-09-07
* Switch from https://opentracing.io/ to https://opentelemetry.io/.  This is a breaking change, due to how an entry for opentracing was always added to the headers, even if not being used. OpenTelemetry will only add entires if enabled.  Any existing queues must be empty before updating to the new version.

###0.5.4 2021-05-19
* Fix error with adding items to a memory queue that has started the shutdown process
* Asking for the list of error messages should not throw an exception if the transport fails; A flag has been added to indicate if the errors are loaded or not.

###0.5.3 2021-05-18
* Fix performance issue with in-memory queues

###0.5.2 2021-04-18
* LiteDb transport now supports direct and memory connections; all connections must be made in the same process.

###0.5.1 2021-04-00
* Add LiteDb transport
* Add dot.net 5 as a target; however many of the references don't support 5.0 yet, so results may vary

###0.5.0 2020-12-08
* Change how connections are setup; this is a breaking change.  This was done to support generic connection settings that could not be expressed as part of the connection string
* Add dot net 4.8 as a target
* Add dot net standard 2.0 as a target for the SQLite transport. The microsoft SQLite transport will be deprecated as part of this release
* The SQL server transport now supports creating the queues in schemas other than 'dbo'

###0.4.6 2020-09-02
* Redis Transport - re-cache LUA scripts when they are no longer in the cache; fixes issue with server re-starts.

###0.4.5 2020-02-28
* Make the previous error types and count available to message processing
* Consumer queues will now remove errors by default; messages in an error status will be removed after 30 days. This can be configured on the consumer and disabled if needed.

###0.4.4 2019-12-23
* Fix issue with SQL server transport and heartbeat reset

###0.4.3 2019-10-29
* Fix issue with registration of message rollback

###0.4.2 2019-10-29
* Add target for 4.6.1
* Upgrade packages to latest versions

###0.4.1 2019-06-08
* Fix issue with retry policies using seconds instead of milliseconds

###0.4.0 2019-06-02
* Remove RPC
* Implement OpenTracing https://opentracing.io/
* Fix Message interception

###0.3.1 2019-04-26
* Correct versioning for nuget publish

###0.3.0 2019-04-26
* All modules are now targeting dot net 4.7.2 and dot net standard 2.0
* **Breaking Change** changes to metrics interface to swtich to AppMetrics
* Deperated Metrics.net - it's no longer updated and will not be getting support for dot.net core
* Added DotNetWorkQueue.AppMetrics as a replacement for DotNetWorkQueue.Metrics.Net

###0.2.1 2017-09-30
* Refactoring to share logic between transports better

* **Breaking Change** Various spelling mistakes have been fixed. This is a breaking change, as this changed the public signatures of a few methods / properties. There are no behavior changes.

* **Breaking Change** A Typo with an internal redis property was fixed. This might prevent new code from reading the correlation Id for items saved inbetween versions. The queues should be drained before upgrading.

* **Breaking Change** SmartThreadPool has been replaced with Task->StartNew and Polly Bulkheads. However, this invalided the following configuration properties; they have been removed
	* MinimumThreads - this was a specific SmartThreadPool feature.
	* ThreadIdleTimeout - this was a specific SmartThreadPool feature.

* The heart beat workers now use an internal job scheduler backed by the in-memory queue, instead of an instance of SmartThreadPool

* **Breaking Change** The hearbest configuration has been changed to use Schyntax format instead of a timespan. The interval has also been removed - you'll need to excplitly indicate how often you want to run the hearbeat - at least slightly less than 1/2 of your dead record time is a good rule of thumb.

* Added a new SQLite transport that uses the microsoft driver. This allows for dot net standard 2.0 support. Most of the logic lives in a module that is shared between the two implementations.

###0.1.10 2017-03-19
* Add route support to SQLServer, SQLite, Redis and PostgreSQL transports. Routes allow messages to be picked up for processing by specific consumer(s). A message can have at most 0 or 1 routes. A consumer can look for messages with 0 routes or N routes.

###0.1.9 2016-10-08
* Fix issue with deleting messages with errors for SQLServer, SQLite, PostGreSQL transports

###0.1.8 2016-09-24
* Refactor default task scheduler to allow easier extension

###0.1.7 2016-08-16

* Fix issue with PostGreSQL transport returning the wrong message body
* Update to msgpack.cli 8.0 for the Redis transport

###0.1.6 2016-08-12

* Add PostGre transport

###0.1.5 2016-08-04

* Add re-occurring job scheduler

* Add metrics for linq serialization, compiling and execution


###0.1.4 2016-06-22

* Minor refactor to poison message handling to allow for easier overriding of behavior

* Added redis hosted on Windows Integration tests. All test are passing. Tested using version 3.0.501 - https://github.com/MSOpenTech/redis

* Refactor IConnectionInformation interface so that it is immutable

* Add ability to send Linq expressions as queue items. This allows execution of remote code without explictly creating specific consumer. See readme / WIKI for usage.

* Fix scope issue with scheduler and multiple consumer queues



###0.1.3 2016-02-18

* Fix formatting issue with poison message exception

* Fix formatting issue with user/system exception

* Don't run monitor delegates if the queue is shutting down

* Add SQLite transport



###0.1.2 2015-11-22

* Fix issue with removing SQL server queues

* Fix issue with message expiration module being run, even if the transport did not support message expiration



###0.1.0 2015-11-03

* Inital release to github
