using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Metrics;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Producer
{
    [TestClass]
    public class SimpleProducer
    {
        [TestMethod]
        [DataRow(1000, true),
         DataRow(1000, false)]
        public void Run(
            int messageCount,
            bool interceptors)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducer();
                producer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, interceptors, false, false, x => { },
                    Helpers.GenerateData, Helpers.Verify);
            }
        }

        [TestMethod]
        public void RunWithTraceVerification()
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<MemoryMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    using (var oCreation = queueCreator.GetQueueCreation<MessageQueueCreation>(new QueueConnection(queueName,
                               connectionInfo.ConnectionString)))
                    {
                        var result = oCreation.CreateQueue();
                        Assert.IsTrue(result.Success, result.ErrorMessage);

                        using (var trace = SharedSetup.CreateTrace("producer"))
                        {
                            using (var metrics = new DotNetWorkQueue.IntegrationTests.Metrics.Metrics(queueName))
                            {
                                using (var creator = SharedSetup.CreateCreator<MemoryMessageQueueInit>(
                                           InterceptorAdding.No, logProvider, metrics, false, false,
                                           oCreation.Scope, trace.Source))
                                {
                                    using (var queue = creator.CreateProducer<FakeMessage>(
                                               new QueueConnection(queueName, connectionInfo.ConnectionString)))
                                    {
                                        queue.Send(GenerateMessage.Create<FakeMessage>());
                                    }
                                }
                            }

                            // Verify trace activities were collected
                            Assert.IsTrue(trace.CollectedActivities.Count > 0,
                                "Expected at least one trace activity to be collected, but none were recorded.");

                            var activityNames = trace.CollectedActivities.Select(a => a.OperationName).ToList();
                            CollectionAssert.Contains(activityNames, "SendMessage",
                                $"Expected a 'SendMessage' span. Found: [{string.Join(", ", activityNames)}]");
                        }

                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
