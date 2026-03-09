using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ConsumerMethodAsync
{
    [TestClass]
    public class SimpleConsumerMethodAsync
    {
        [TestMethod]
        [DataRow(50, 5, 200, 10, 1, 1, true, 1, LinqMethodTypes.Compiled, false),
#if NETFULL
         DataRow(50, 5, 200, 10, 1, 2, false, 1, LinqMethodTypes.Dynamic, false),
         DataRow(10, 5, 180, 7, 1, 1, true, 1, LinqMethodTypes.Dynamic, false),
#endif
         DataRow(10, 5, 180, 7, 1, 2, false, 1, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, int messageType, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                    SimpleMethodConsumerAsync();
            consumer.Run<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, linqMethodTypes,
                enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions,
                    false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
