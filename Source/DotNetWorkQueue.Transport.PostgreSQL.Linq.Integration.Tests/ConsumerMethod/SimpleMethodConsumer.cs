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
    public class SimpleMethodConsumer
    {
        [TestMethod]
        [DataRow(10, 0, 240, 5, false, LinqMethodTypes.Compiled, true),
#if NETFULL
         DataRow(100, 0, 240, 5, false, LinqMethodTypes.Dynamic, false),
         DataRow(10, 15, 180, 7, true, LinqMethodTypes.Dynamic, false),
#endif
         DataRow(10, 15, 180, 7, true, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int runtime, int timeOut,
            int workerCount, bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                    SimpleMethodConsumer();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
