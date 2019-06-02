DotNetWorkQueue.Samples
=========

[**Compile order**]

- \DotNetWorkQueue.sln | in debug mode
- \Samples\SampleShared\SampleShared.sln
- \\Samples\solutions


[**Samples**]

-Producer
-Producer LINQ
-Consumer with dedicated threads
-Consumer with dedicated reader and seperate processing threads
-Consumer for LINQ
-Scheduler
-Scheduler Consumer

[**Sample Transports**]

-Redis
-SQL Server
-SQLite
-PostGresSQL

[**Configuration**]

-Set connection strings and queue name in app.config
-Enable/disable GZIP and encryption as needed in app.config
-Enable/disable tracing and metrics in app.config

[**Trace**]

Jaeger is used for the sample. Configuration file for all samples is tracesettings.json. You will need to modify to point to your instance.

For testing, the all-in-one system works fine.  It can be found as a docker image or windows/linux executable here

https://www.jaegertracing.io/download/

Tracing can be disabled in app.config

[**Metrics**]

InfluxDB is used for the samples.  Configuration file for all samples is metricsettings.json. You will need to modify to point to your instance.

Metrics can be disabled in app.config

License
--------
Copyright (c) 2019 Brian Lehnen

All rights reserved.

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
