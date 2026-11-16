using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation
{
    /// <summary>
    /// Which retry rule a failed message matches, measured by how often the handler is called.
    /// </summary>
    /// <remarks>
    /// The consumer has a retry rule for <see cref="IndexOutOfRangeException"/> only
    /// (<see cref="SharedSetup.SetupDefaultErrorRetry"/>: two retries). A handler exception that matches
    /// it is attempted three times; one that does not goes straight to the error queue after one attempt.
    /// Counting attempts is what makes the matching observable - the error-count checks some transports
    /// use cannot tell the two apart.
    /// </remarks>
    public class ConsumerAsyncRetryMatching
    {
        public enum ConsumerKind
        {
            /// <summary><see cref="IConsumerQueueScheduler"/>: the handler runs on the task scheduler.</summary>
            Scheduler,
            /// <summary><see cref="IConsumerQueueAsync"/>: the handler is an async delegate.</summary>
            DirectAsync
        }

        public void Run<TTransportInit, TMessage, TTransportCreate>(
            QueueConnection queueConnection,
            int messageCount, int workerCount,
            ConsumerKind consumerKind,
            Func<Exception> handlerException,
            int expectedAttemptsPerMessage,
            Action<TTransportCreate> setOptions,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, ICreationScope> verify)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
            where TTransportCreate : class, IQueueCreation
        {
            var logProvider = LoggerShared.Create(queueConnection.Queue, GetType().Name);
            using (var queueCreator = new QueueCreationContainer<TTransportInit>(
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

                    new ProducerShared().RunTest<TTransportInit, TMessage>(queueConnection, false, messageCount,
                        logProvider, generateData, verify, false, scope, false);

                    var expected = (long)messageCount * expectedAttemptsPerMessage;
                    var attempts = Consume<TTransportInit, TMessage>(queueConnection, logProvider, workerCount,
                        consumerKind, handlerException, expected, scope);

                    Assert.AreEqual(expected, attempts,
                        $"{consumerKind}: the handler threw {handlerException().GetType().Name} and was called " +
                        $"{attempts} times for {messageCount} messages; expected {expectedAttemptsPerMessage} each.");
                }
                finally
                {
                    oCreation?.RemoveQueue();
                    oCreation?.Dispose();
                    scope?.Dispose();
                }
            }
        }

        private static long Consume<TTransportInit, TMessage>(QueueConnection queueConnection,
            Microsoft.Extensions.Logging.ILogger logProvider, int workerCount, ConsumerKind consumerKind,
            Func<Exception> handlerException, long expected, ICreationScope scope)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
        {
            long attempts = 0;
            using (var trace = SharedSetup.CreateTrace("consumer-retry-matching"))
            using (var metrics = new Metrics.Metrics(queueConnection.Queue))
            using (var creator = SharedSetup.CreateCreator<TTransportInit>(InterceptorAdding.No, logProvider,
                       metrics, false, false, scope, trace.Source))
            {
                if (consumerKind == ConsumerKind.Scheduler)
                {
                    using (var schedulerCreator = new SchedulerContainer(
                               // ReSharper disable once AccessToDisposedClosure
                               serviceRegister => serviceRegister.Register(() => metrics, LifeStyles.Singleton)
                                   .RegisterNonScopedSingleton(trace.Source),
                               options => SharedSetup.SetOptions(options, false)))
                    using (var taskScheduler = schedulerCreator.CreateTaskScheduler())
                    {
                        taskScheduler.Configuration.MaximumThreads = workerCount;
                        taskScheduler.Start();
                        var taskFactory = schedulerCreator.CreateTaskFactory(taskScheduler);
                        using (var queue = creator.CreateConsumerQueueScheduler(queueConnection, taskFactory))
                        {
                            Configure(queue.Configuration, workerCount);
                            queue.Start<TMessage>((message, notifications) =>
                            {
                                Interlocked.Increment(ref attempts);
                                throw handlerException();
                            }, CreateNotifications.Create(logProvider));
                            WaitThenSettle(ref attempts, expected);
                        }
                    }
                }
                else
                {
                    using (var queue = creator.CreateConsumerAsync(queueConnection))
                    {
                        Configure(queue.Configuration, workerCount);
                        queue.Start<TMessage>(async (message, notifications) =>
                        {
                            await Task.Yield();
                            Interlocked.Increment(ref attempts);
                            throw handlerException();
                        }, CreateNotifications.Create(logProvider));
                        WaitThenSettle(ref attempts, expected);
                    }
                }
            }
            return Interlocked.Read(ref attempts);
        }

        private static void Configure(QueueConsumerConfiguration configuration, int workerCount)
        {
            SharedSetup.SetupDefaultConsumerQueue(configuration, workerCount, TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(35), TimeSpan.FromSeconds(10), null);
            SharedSetup.SetupDefaultErrorRetry(configuration);
        }

        /// <summary>
        /// Waits for the expected number of attempts, then a little longer: an attempt beyond the
        /// expected count (a retry that should not have happened) has to have time to show up.
        /// </summary>
        private static void WaitThenSettle(ref long attempts, long expected)
        {
            var timer = Stopwatch.StartNew();
            while (Interlocked.Read(ref attempts) < expected && timer.Elapsed < TimeSpan.FromSeconds(60))
                Thread.Sleep(100);
            // The retry delays are 1s and 2s; give an unexpected retry well over that to land.
            Thread.Sleep(TimeSpan.FromSeconds(6));
        }
    }
}
