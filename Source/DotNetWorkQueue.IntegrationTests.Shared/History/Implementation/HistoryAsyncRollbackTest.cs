using System;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.History.Implementation
{
    /// <summary>
    /// Rollback history on the asynchronous consumer.
    ///
    /// Nothing exercised this combination — history is enabled only by the SimpleHistory tests, which
    /// commit, and the rollback tests run with history off. So `RecordRollbackAsync` reached no test on
    /// any transport.
    ///
    /// That gap hid a real defect, which is why this test exists rather than being written for the
    /// coverage number: the history decorator read `context.MessageId` *after* delegating, and Redis'
    /// rollback handler clears it on its way out, so rollback history was silently never recorded on
    /// that transport - synchronously or asynchronously.
    /// </summary>
    public class HistoryAsyncRollbackTest
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

                    var attempts = 0;
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

                        var seen = new ManualResetEventSlim(false);
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
                                    //OperationCanceledException is what puts a message down the
                                    //rollback path: MessageProcessingAsync catches it directly and
                                    //rolls back, where any other exception goes through the error
                                    //handler instead. Same mechanism HandleFakeMessagesRollback uses.
                                    //
                                    //Every message is refused. Redelivery is deliberately not required:
                                    //what a rolled-back message does next differs by transport - the
                                    //memory transport's rollback is a no-op - and the history write this
                                    //test exists for happens in the core decorator either way.
                                    consumer.Start<TMessage>((message, workerNotification) =>
                                    {
                                        if (Interlocked.Increment(ref attempts) >= messageCount)
                                            seen.Set();
                                        throw new OperationCanceledException("rolling this one back");
                                    }, null);

                                    seen.Wait(TimeSpan.FromSeconds(30));
                                    //let the rollback and its history write finish
                                    Thread.Sleep(3000);
                                }
                            }
                        }
                    }

                    Assert.IsGreaterThanOrEqualTo(1, attempts,
                        "The consumer never reached a message, so nothing was rolled back.");

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
                            var records = historyQuery.Get(0, 100, null);

                            Assert.IsNotNull(records);
                            Assert.IsGreaterThanOrEqualTo(messageCount, records.Count);

                            //A rolled-back message goes back to Enqueued with its retry count raised.
                            //RetryCount is the assertion that matters: it is only incremented by
                            //RecordRollback, so a zero here means the rollback history was not written -
                            //which is exactly what happened on Redis before the decorator was fixed.
                            Assert.IsTrue(records.Any(r => r.RetryCount > 0),
                                "No history record shows a retry, so the rollback was never recorded.");
                        }
                    }
                }
                finally
                {
                    oCreation ??= queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                    scope?.Dispose();
                }
            }
        }
    }
}
