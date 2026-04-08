using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ConsumerMethod
{
    [TestClass]
    public class ConsumerMethodHeartbeat
    {
        [TestMethod]
        [DataRow(7, 15, 90, 3, false)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                    ConsumerMethodHeartbeat();
            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                    true, true, false, false,
                    false, true, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
