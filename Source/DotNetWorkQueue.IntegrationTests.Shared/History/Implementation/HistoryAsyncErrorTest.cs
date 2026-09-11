using System;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.History.Implementation
{
    /// <summary>
    /// Error history on the asynchronous consumer.
    ///
    /// The error path is the one place the asynchronous consumer still blocked after the commit and
    /// rollback work: <c>ProcessMessageAsync</c> called <c>MessageExceptionHandler.Handle</c>, which
    /// reaches the transport error write and the history write beneath it. Nothing covered that
    /// combination - the SimpleHistory tests commit, and the error tests run with history off - so
    /// <c>RecordErrorAsync</c> reached no test on any transport.
    ///
    /// Asserting <see cref="MessageHistoryStatus.Error"/> covers the processing-start row as well on the
    /// relational transports: their error update only matches a record already in Processing or
    /// Enqueued.
    /// </summary>
    public class HistoryAsyncErrorTest
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
                                    //No retry delay is configured for this exception type, so the first
                                    //failure sends the message straight to the error queue rather than
                                    //back to the queue - which is what writes the error history row.
                                    //An OperationCanceledException would take the rollback path instead.
                                    consumer.Start<TMessage>((message, workerNotification) =>
                                    {
                                        if (Interlocked.Increment(ref attempts) >= messageCount)
                                            seen.Set();
                                        throw new InvalidOperationException(ExceptionMarker);
                                    }, null);

                                    seen.Wait(TimeSpan.FromSeconds(30));
                                    //let the error move and its history write finish
                                    Thread.Sleep(3000);
                                }
                            }
                        }
                    }

                    Assert.IsGreaterThanOrEqualTo(messageCount, attempts,
                        "The consumer did not reach every message, so not all of them failed.");

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

                            var errorCount = historyQuery.GetCount(MessageHistoryStatus.Error);
                            Assert.IsGreaterThanOrEqualTo(messageCount, errorCount,
                                $"Expected at least {messageCount} history records in the error status, got {errorCount}. " +
                                "The asynchronous error path did not write history.");

                            var records = historyQuery.Get(0, 100, MessageHistoryStatus.Error);
                            Assert.IsNotNull(records);

                            //The exception the consumer threw has to be what was stored, not some
                            //wrapper raised by the error handling itself.
                            Assert.IsTrue(records.All(r => r.ExceptionText != null && r.ExceptionText.Contains(ExceptionMarker)),
                                "A history record in the error status does not carry the exception that caused it.");
                            Assert.IsTrue(records.All(r => r.CompletedUtc.HasValue),
                                "A history record in the error status has no completion time.");
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

        private const string ExceptionMarker = "sending this one to the error queue";
    }
}
