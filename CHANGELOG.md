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