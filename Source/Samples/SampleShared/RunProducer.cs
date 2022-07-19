using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue;
using DotNetWorkQueue.Messages;
using Serilog;

namespace SampleShared
{
    public static class RunProducer
    {
        public static IEnumerable<IQueueOutputMessage> Run(IProducerQueue<SimpleMessage> queue, IEnumerable<SimpleMessage> messages, Func<IAdditionalMessageData> expiredDataFuture)
        {
            Log.Logger.Information("Sending...");
            foreach (var message in messages)
            {
                yield return queue.Send(message, expiredDataFuture.Invoke());
            }
            Log.Logger.Information("Done");
        }

        public static async Task<List<IQueueOutputMessage>> RunAsync(IProducerQueue<SimpleMessage> queue, IEnumerable<SimpleMessage> messages, Func<IAdditionalMessageData> expiredDataFuture)
        {
            var data = new List<IQueueOutputMessage>();
            Log.Logger.Information("Sending...");
            foreach (var message in messages)
            {
                data.Add(await queue.SendAsync(message, expiredDataFuture.Invoke()));
            }
            Log.Logger.Information("Done");
            return data;
        }

        public static IEnumerable<IQueueOutputMessage> Run(IProducerQueue<SimpleMessage> queue, SimpleMessage message, IAdditionalMessageData data)
        {
            Log.Logger.Information("Sending...");
            yield return queue.Send(message, data);
            Log.Logger.Information("Done");
        }

        public static IEnumerable<IQueueOutputMessage> RunStatic(IProducerMethodQueue queue, int count, Func<IAdditionalMessageData> expiredDataFuture)
        {
            for (var i = 0; i < count; i++)
            {
                yield return queue.Send((message, workerNotification) => new TestClass().RunMe(
                    workerNotification,
                    "a string",
                    2,
                    new SomeInput(DateTime.UtcNow.ToString())), expiredDataFuture.Invoke());
            }
        }

#if net48
        public static IEnumerable<IQueueOutputMessage> RunDynamic(IProducerMethodQueue queue, int count, Func<IAdditionalMessageData> expiredDataFuture)
        {
            for (var i = 0; i < count; i++)
            {
                yield return queue.Send(new LinqExpressionToRun(
                    "(message, workerNotification) => new SampleShared.TestClass().RunMe((IWorkerNotification)workerNotification, \"dynamic\", 2, new SampleShared.SomeInput(DateTime.UtcNow.ToString()))",
                    new List<string> { "SampleShared.dll" }, //additional references
                    new List<string> { "SampleShared" }), expiredDataFuture.Invoke()); //additional using statements
            }
        }
#endif
        public static async Task<List<IQueueOutputMessage>> RunStaticAsync(IProducerMethodQueue queue, int count, Func<IAdditionalMessageData> expiredDataFuture)
        {
            var results = new List<IQueueOutputMessage>(count);
            for (var i = 0; i < count; i++)
            {
                var result = await queue.SendAsync((message, workerNotification) => new TestClass().RunMe(
                    workerNotification,
                    "a string",
                    2,
                    new SomeInput(DateTime.UtcNow.ToString())), expiredDataFuture.Invoke());
                results.Add(result);
            }

            return results;
        }

#if net48
        public static async Task<List<IQueueOutputMessage>> RunDynamicAsync(IProducerMethodQueue queue, int count, Func<IAdditionalMessageData> expiredDataFuture)
        {
            var results = new List<IQueueOutputMessage>(count);
            for (var i = 0; i < count; i++)
            {
                var result = await queue.SendAsync(new LinqExpressionToRun(
                    "(message, workerNotification) => new SampleShared.TestClass().RunMe((IWorkerNotification)workerNotification, \"dynamic\", 2, new SampleShared.SomeInput(DateTime.UtcNow.ToString()))",
                    new List<string> { "SampleShared.dll" }, //additional references
                    new List<string> { "SampleShared" }), expiredDataFuture.Invoke()); //additional using statements
                results.Add(result);
            }

            return results;
        }
#endif

        public static async Task<List<IQueueOutputMessage>> RunAsync(IProducerQueue<SimpleMessage> queue, SimpleMessage message, IAdditionalMessageData data)
        {
            var returnData = new List<IQueueOutputMessage>();
            Log.Logger.Information("Sending...");
            returnData.Add(await queue.SendAsync(message, data));
            Log.Logger.Information("Done");
            return returnData;
        }

        public static async Task<List<IQueueOutputMessage>> RunBatchAsync(IProducerQueue<SimpleMessage> queue,
            List<SimpleMessage> messages, Func<IAdditionalMessageData> expiredDataFuture)
        {
            var messagesWithData = new List<QueueMessage<SimpleMessage, IAdditionalMessageData>>(messages.Count);
            foreach (var message in messages)
            {
                messagesWithData.Add(new QueueMessage<SimpleMessage, IAdditionalMessageData>(message, expiredDataFuture.Invoke()));
            }

            var data = new List<IQueueOutputMessage>();
            Log.Logger.Information("Sending...");
            data.AddRange(await queue.SendAsync(messagesWithData));
            Log.Logger.Information("Done");
            return data;
        }

        public static List<IQueueOutputMessage> RunBatch(IProducerQueue<SimpleMessage> queue,
            List<SimpleMessage> messages, Func<IAdditionalMessageData> expiredDataFuture)
        {
            var messagesWithData = new List<QueueMessage<SimpleMessage, IAdditionalMessageData>>(messages.Count);
            foreach (var message in messages)
            {
                messagesWithData.Add(new QueueMessage<SimpleMessage, IAdditionalMessageData>(message, expiredDataFuture.Invoke()));
            }
            var data = new List<IQueueOutputMessage>();
            Log.Logger.Information("Sending...");
            data.AddRange(queue.Send(messagesWithData));
            Log.Logger.Information("Done");
            return data;
        }

        public static void RunLoop(IProducerMethodQueue queue, Func<IAdditionalMessageData> expiredDataInstant, Func<IAdditionalMessageData> expiredDataFuture, IAdminApi admin)
        {
            var keepRunning = true;
            while (keepRunning)
            {
                Console.WriteLine($"{admin.Count(admin.Connections.Keys.FirstOrDefault(), null)} items in queue");
                Console.WriteLine(@"To test heartbeat recovery, force kill your consumer after starting to process record(s)
To test rollbacks, cancel the consumer by pressing any button. Easier to test with longer running jobs.

Sync
a) Send 1 static job
b) Send 1 dynamic job (full framework only)

Async
c) Send 1 static job
d) Send 1 dynamic job (full framework only)

q) Quit");
                var key = char.ToLower(Console.ReadKey(true).KeyChar);
                switch (key)
                {
                    case 'a':
                        HandleResults.Handle(RunStatic(queue, 1, expiredDataFuture), Log.Logger);
                        break;
#if net48
                    case 'b':
                        HandleResults.Handle(RunDynamic(queue, 1, expiredDataFuture), Log.Logger);
                        break;
#endif
                    case 'c':
                        HandleResults.Handle(RunStaticAsync(queue, 1, expiredDataFuture).Result, Log.Logger);
                        break;
#if net48
                    case 'd':
                        HandleResults.Handle(RunDynamicAsync(queue, 1, expiredDataFuture).Result, Log.Logger);
                        break;
#endif
                    case 'q':
                        Console.WriteLine("Quitting");
                        keepRunning = false;
                        break;
                }
            }
        }

        public static void RunLoop(IProducerQueue<SimpleMessage> queue, Func<IAdditionalMessageData> expiredDataInstant, Func<IAdditionalMessageData> expiredDataFuture,
            Func<int, IAdditionalMessageData> delayProcessing, IAdminApi admin)
        {
            var keepRunning = true;
            while (keepRunning)
            {
                Console.WriteLine($"{admin.Count(admin.Connections.Keys.FirstOrDefault(), null)} items in queue");
                Console.WriteLine(@"To test heartbeat recovery, force kill your consumer after starting to process record(s)
To test rollbacks, cancel the consumer by pressing any button. Easier to test with longer running jobs.

Sync
a) Send 10 jobs
b) Send 500 jobs
c) Send 1000 jobs
d) Send 1 job with 20 second processing time
e) Send 1 job with 60 second processing time
f) Send 100 random jobs
g) Test error
h) Test retry-able error and finish after some retries
i) Test retry-able error and fail after some retries
j) Test expire 

Async
k) Send 10 jobs
l) Send 500 jobs
m) Send 1000 jobs
n) Send 1 job with 20 second processing time
o) Send 1 job with 60 second processing time
p) Send 100 random jobs
r) Test error
s) Test retry-able error and finish after some retries
t) Test retry-able error and fail after some retries
u) Test expire 

Batch
v) Send 100 random jobs
w) Send 1000 random jobs
x) Send Async 100 random jobs
y) Send Async 1000 random jobs

Long Running
z) Send 1 job with a 600 second processing time

Delayed Processing

1) Send 1 job with a 10 second processing delay
2) Send 10 jobs with a 30 second processing delay

q) Quit");
                var key = char.ToLower(Console.ReadKey(true).KeyChar);

                switch (key)
                {
                    case 'a':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessage(10, 500, 1000), expiredDataFuture),
                            Log.Logger);
                        break;
                    case 'b':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessage(100, 500, 500), expiredDataFuture),
                            Log.Logger);
                        break;
                    case 'c':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessage(1000, 100, 50), expiredDataFuture),
                            Log.Logger);
                        break;
                    case 'd':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessage(1, 20000, 100000), expiredDataFuture),
                            Log.Logger);
                        break;
                    case 'e':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessage(1, 60000, 10), expiredDataFuture),
                            Log.Logger);
                        break;
                    case 'f':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessageRandom(100), expiredDataFuture),
                            Log.Logger);
                        break;
                    case 'g':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessageError(1000, 0), expiredDataFuture), Log.Logger);
                        break;
                    case 'h':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessageRetryError(1000, false), expiredDataFuture), Log.Logger);
                        break;
                    case 'i':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessageRetryError(1000, true), expiredDataFuture), Log.Logger);
                        break;
                    case 'j':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleExpiredMessage(), expiredDataInstant.Invoke()), Log.Logger);
                        break;
                    case 'z':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessage(1, 600000, 10), expiredDataFuture),
                            Log.Logger);
                        break;
                    //async

                    case 'k':
                        HandleResults.Handle(RunProducer.RunAsync(queue, Messages.CreateSimpleMessage(10, 500, 1000), expiredDataFuture).Result,
                            Log.Logger);
                        break;
                    case 'l':
                        HandleResults.Handle(RunProducer.RunAsync(queue, Messages.CreateSimpleMessage(100, 500, 500), expiredDataFuture).Result,
                            Log.Logger);
                        break;
                    case 'm':
                        HandleResults.Handle(RunProducer.RunAsync(queue, Messages.CreateSimpleMessage(1000, 100, 50), expiredDataFuture).Result,
                            Log.Logger);
                        break;
                    case 'n':
                        HandleResults.Handle(RunProducer.RunAsync(queue, Messages.CreateSimpleMessage(1, 20000, 100000), expiredDataFuture).Result,
                            Log.Logger);
                        break;
                    case 'o':
                        HandleResults.Handle(RunProducer.RunAsync(queue, Messages.CreateSimpleMessage(1, 60000, 10), expiredDataFuture).Result,
                            Log.Logger);
                        break;
                    case 'p':
                        HandleResults.Handle(RunProducer.RunAsync(queue, Messages.CreateSimpleMessageRandom(100), expiredDataFuture).Result,
                            Log.Logger);
                        break;
                    case 'r':
                        HandleResults.Handle(RunProducer.RunAsync(queue, Messages.CreateSimpleMessageError(1000, 0), expiredDataFuture).Result, Log.Logger);
                        break;
                    case 's':
                        HandleResults.Handle(RunProducer.RunAsync(queue, Messages.CreateSimpleMessageRetryError(1000, false), expiredDataFuture).Result, Log.Logger);
                        break;
                    case 't':
                        HandleResults.Handle(RunProducer.RunAsync(queue, Messages.CreateSimpleMessageRetryError(1000, true), expiredDataFuture).Result, Log.Logger);
                        break;
                    case 'u':
                        HandleResults.Handle(RunProducer.RunAsync(queue, Messages.CreateSimpleExpiredMessage(), expiredDataInstant.Invoke()).Result, Log.Logger);
                        break;

                    //batch
                    case 'v':
                        HandleResults.Handle(RunProducer.RunBatch(queue, Messages.CreateSimpleMessageRandomList(100), expiredDataFuture),
                            Log.Logger);
                        break;
                    case 'w':
                        HandleResults.Handle(RunProducer.RunBatch(queue, Messages.CreateSimpleMessageRandomList(1000), expiredDataFuture),
                            Log.Logger);
                        break;
                    case 'x':
                        HandleResults.Handle(RunProducer.RunBatchAsync(queue, Messages.CreateSimpleMessageRandomList(100), expiredDataFuture).Result,
                            Log.Logger);
                        break;
                    case 'y':
                        HandleResults.Handle(RunProducer.RunBatchAsync(queue, Messages.CreateSimpleMessageRandomList(1000), expiredDataFuture).Result,
                            Log.Logger);
                        break;

                    case '1':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessage(1, 500, 1000), () => delayProcessing(10)),
                            Log.Logger);
                        break;

                    case '2':
                        HandleResults.Handle(RunProducer.Run(queue, Messages.CreateSimpleMessage(10, 500, 1000), () => delayProcessing(30)),
                            Log.Logger);
                        break;

                    case 'q':
                        Console.WriteLine("Quitting");
                        keepRunning = false;
                        break;
                }
            }
        }
    }
}
