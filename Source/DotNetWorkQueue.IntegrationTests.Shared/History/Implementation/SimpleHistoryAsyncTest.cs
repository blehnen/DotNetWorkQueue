using System;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.History.Implementation
{
    /// <summary>
    /// The asynchronous twin of <see cref="SimpleHistoryTest"/>.
    ///
    /// History was only ever exercised through the synchronous consumer, on every transport. The
    /// asynchronous consumer records the processing-start row from its own receive path, so that write
    /// reached no test - which is how it stayed synchronous unnoticed until #284.
    ///
    /// Reaching Complete is the assertion that proves the start row was written at all: RecordComplete
    /// only matches a record whose status is already Processing, so a record that never started cannot
    /// finish. StartedUtc and DurationMs are asserted on top of that because nothing else asserts them
    /// anywhere, and DurationMs is derived from StartedUtc - it silently becomes zero when the start
    /// row is missing.
    /// </summary>
    public class SimpleHistoryAsyncTest
    {
        public void Run<TTransportInit, TMessage, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify)
            where TTransportInit : ITransportInit, new()
            where TMessage : class, new()
            where TTransportCreate : class, IQueueCreation
        {
            var logProvider = LoggerShared.Create(queueConnection.Queue, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<TTransportInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                ICreationScope scope = null;
                var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                try
                {
                    setOptions(oCreation);
                    var result = oCreation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);
                    scope = oCreation.Scope;

                    var processedCount = 0;
                    using (var queueContainer = new QueueContainer<TTransportInit>(serviceRegister =>
                    {
                        serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                        serviceRegister.RegisterNonScopedSingleton(scope);
                    }))
                    {
                        using (var producer = queueContainer.CreateProducer<TMessage>(queueConnection))
                        {
                            for (var i = 0; i < messageCount; i++)
                            {
                                var sendResult = producer.Send(new TMessage());
                                Assert.IsFalse(sendResult.HasError, $"Send failed: {sendResult.SendingException?.Message}");
                            }
                        }

                        var waitHandle = new ManualResetEventSlim(false);

                        //The asynchronous consumer, which is the whole point: it records the
                        //processing-start row from a continuation rather than from the worker's thread.
                        using (var schedulerCreator = new SchedulerContainer())
                        {
                            using (var taskScheduler = schedulerCreator.CreateTaskScheduler())
                            {
                                taskScheduler.Configuration.MaximumThreads = 1;
                                taskScheduler.Start();
                                var taskFactory = schedulerCreator.CreateTaskFactory(taskScheduler);

                                using (var consumer =
                                    queueContainer.CreateConsumerQueueScheduler(queueConnection, taskFactory))
                                {
                                    consumer.Start<TMessage>((message, workerNotification) =>
                                    {
                                        Interlocked.Increment(ref processedCount);
                                        if (processedCount >= messageCount)
                                            waitHandle.Set();
                                    }, null);

                                    waitHandle.Wait(TimeSpan.FromSeconds(30));
                                }
                            }
                        }
                    }

                    Assert.AreEqual(messageCount, processedCount, "Not all messages were processed");

                    oCreation?.Dispose();
                    oCreation = null;

                    using (var verifyContainer = new QueueContainer<TTransportInit>(serviceRegister =>
                    {
                        serviceRegister.Register(() => logProvider, LifeStyles.Singleton);
                        serviceRegister.RegisterNonScopedSingleton(scope);
                    }))
                    {
                        using (var adminContainer = verifyContainer.CreateAdminContainer(queueConnection))
                        {
                            var historyQuery = adminContainer.GetInstance<IQueryMessageHistory>();

                            var completeCount = historyQuery.GetCount(MessageHistoryStatus.Complete);
                            Assert.IsGreaterThanOrEqualTo(messageCount, completeCount,
                                $"Expected at least {messageCount} completed records, got {completeCount}. " +
                                "Complete is only reachable from Processing, so a shortfall here means the " +
                                "asynchronous consumer never recorded that processing started.");

                            var records = historyQuery.Get(0, 100, null);
                            Assert.IsNotNull(records);
                            Assert.IsGreaterThanOrEqualTo(messageCount, records.Count);

                            foreach (var record in records)
                            {
                                Assert.IsNotNull(record.QueueId);
                                Assert.AreEqual(MessageHistoryStatus.Complete, record.Status);
                                Assert.IsNotNull(record.StartedUtc,
                                    $"Record {record.QueueId} has no StartedUtc, so the processing-start row " +
                                    "was never written; DurationMs is meaningless without it.");
                                Assert.IsNotNull(record.CompletedUtc);
                            }

                            //Derived from StartedUtc, and silently zero when it is missing.
                            Assert.IsTrue(records.All(r => r.DurationMs.HasValue),
                                "A completed record carried no DurationMs.");
                        }
                    }
                }
                finally
                {
                    oCreation?.RemoveQueue();
                    oCreation?.Dispose();
                    scope?.Dispose();
                }
            }
        }
    }
}
