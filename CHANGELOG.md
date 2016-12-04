###0.1.10 ????-??-??
* Add route support to SQLServer, SQLite and PostgreSQL transports. Redis transport currently doesn't support routes. This might get added before publishing 0.1.10 to nuget. Routes allow messages to be picked up for processing by specific consumer(s). A message can have at most 0 or 1 routes. A consumer can look for messages with 0 routes or N routes.

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