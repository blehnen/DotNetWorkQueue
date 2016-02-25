###0.1.4 ????-??-??
* Minor refactor to poison message handling to allow for easier overriding of behavior
* Added redis hosted on Windows Integration tests. All test are passing. Tested using version 3.0.501 - https://github.com/MSOpenTech/redis


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