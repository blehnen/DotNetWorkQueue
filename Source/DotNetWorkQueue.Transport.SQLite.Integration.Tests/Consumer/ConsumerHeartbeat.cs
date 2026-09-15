using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Consumer
{
    [TestClass]
    [Retry(2)]
    public class ConsumerHeartbeat
    {
        [TestMethod]
        [DataRow(7, 45, 180, 3, true, false),
        DataRow(7, 45, 180, 3, false, false),
        DataRow(7, 45, 280, 3, true, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerHeartbeat();
                consumer.Run<SqLiteMessageQueueInit, FakeMessage, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                        true, true, false,
                        false, true, true, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount,
                    //SQLite serialises writers, so a heartbeat write queues behind the de-queue and
                    //commit transactions of every other worker. One was measured at 15.45 seconds on
                    //an idle machine against the default 10 second claim, which reset a message that
                    //was still being worked and handed it to a second worker (GitHub #328). 30 seconds
                    //clears that and still sits below runtime, so a failing heartbeat would still lose
                    //the claim and fail this test.
                    TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(5));
            }
        }
    }
}
