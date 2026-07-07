using System.Linq;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation
{
    /// <summary>
    /// Shared helper that sends a single message and asserts that trace activities
    /// were collected via the opt-in <see cref="ActivitySourceWrapper"/> listener.
    ///
    /// Transport-specific tests delegate to this helper rather than hand-rolling
    /// queue setup, so the same trace verification runs consistently across every
    /// transport without duplicating 40+ lines of nested using blocks per test.
    /// </summary>
    public class SimpleProducerWithTraceVerification
    {
        public void Run<TTransportInit, TMessage, TTransportCreate>(QueueConnection queueConnection)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
            where TTransportCreate : class, IQueueCreation
        {
            var logProvider = LoggerShared.Create(queueConnection.Queue, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<TTransportInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                using (var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection))
                {
                    var result = oCreation.CreateQueue();
                    Assert.IsTrue(result.Success, result.ErrorMessage);

                    try
                    {
                        // collectActivities: true flips the listener's ActivityStarted callback on
                        // so we can assert on the collected spans below.
                        using (var trace = SharedSetup.CreateTrace("producer", collectActivities: true))
                        {
                            using (var metrics = new Metrics.Metrics(queueConnection.Queue))
                            {
                                using (var creator = SharedSetup.CreateCreator<TTransportInit>(
                                           InterceptorAdding.No, logProvider, metrics, false, false,
                                           oCreation.Scope, trace.Source))
                                {
                                    using (var queue = creator.CreateProducer<TMessage>(queueConnection))
                                    {
                                        queue.Send(GenerateMessage.Create<TMessage>());
                                    }
                                }
                            }

                            // Verify trace activities were collected
                            Assert.IsNotEmpty(trace.CollectedActivities,
                                "Expected at least one trace activity to be collected, but none were recorded.");

                            var activityNames = trace.CollectedActivities.Select(a => a.OperationName).ToList();
                            CollectionAssert.Contains(activityNames, "SendMessage",
                                $"Expected a 'SendMessage' span. Found: [{string.Join(", ", activityNames)}]");
                        }
                    }
                    finally
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
