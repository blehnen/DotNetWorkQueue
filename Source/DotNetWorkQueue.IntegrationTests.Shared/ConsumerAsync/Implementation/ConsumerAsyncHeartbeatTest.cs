using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation
{
    /// <summary>
    /// A heartbeat on the asynchronous consumer.
    ///
    /// The heartbeat tests only ever drove the synchronous consumer, so `ISendHeartBeat` reached no test
    /// on the asynchronous one - which is how it stayed synchronous unnoticed until #284. It is also the
    /// only remaining place where finishing a message could park the consumer's thread: the worker's
    /// `Stop` and `Dispose` wait on whatever lock the beat holds.
    ///
    /// The message handler blocks for longer than the beat interval and then reads the status the beat
    /// wrote, so a passing run means a beat completed while the message was still being processed.
    /// </summary>
    public class ConsumerAsyncHeartbeatTest
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

                    var processed = 0;
                    var beats = 0;
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

                        var done = new ManualResetEventSlim(false);
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
                                    consumer.Configuration.HeartBeat.UpdateTime = "*/2 * * * * *";
                                    consumer.Configuration.HeartBeat.Time = TimeSpan.FromSeconds(10);
                                    consumer.Configuration.HeartBeat.MonitorTime = TimeSpan.FromSeconds(12);
                                    consumer.Configuration.HeartBeat.ThreadPoolConfiguration.ThreadsMax = 2;

                                    consumer.Start<TMessage>((message, workerNotification) =>
                                    {
                                        //hold the message past two beat intervals, then read what the
                                        //beat recorded - the status is only set once a beat comes back
                                        Thread.Sleep(TimeSpan.FromSeconds(6));
                                        if (workerNotification.HeartBeat?.Status?.LastHeartBeatTime != null)
                                            Interlocked.Increment(ref beats);
                                        if (Interlocked.Increment(ref processed) >= messageCount)
                                            done.Set();
                                    }, null);

                                    done.Wait(TimeSpan.FromSeconds(90));
                                    //let the worker finish disposing its heartbeat
                                    Thread.Sleep(2000);
                                }
                            }
                        }
                    }

                    Assert.AreEqual(messageCount, processed, "Not every message was processed");
                    Assert.IsGreaterThanOrEqualTo(1, beats,
                        "No message saw a heartbeat, so the asynchronous consumer never recorded one.");
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
