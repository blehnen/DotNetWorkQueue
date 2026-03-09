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
    public class ConsumerMethodErrorTable
    {
        [TestMethod]
        [DataRow(10, 60, 20, true, LinqMethodTypes.Compiled, true),
#if NETFULL
        DataRow(100, 60, 20, false, LinqMethodTypes.Dynamic, false),
         DataRow(100, 60, 20, true, LinqMethodTypes.Dynamic, false),
         DataRow(1, 60, 5, true, LinqMethodTypes.Dynamic, true),
#endif
         DataRow(10, 60, 5, true, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int timeOut, int workerCount,
            bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.ConsumerMethodErrorTable();
            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString),
                messageCount, timeOut, workerCount, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                   true, !useTransactions, useTransactions, false,
                   false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
        }

        private void ValidateErrorCounts(QueueConnection queueConnection, int arg3, ICreationScope arg4)
        {
            new VerifyErrorCounts(queueConnection.Queue).Verify(arg3, 2);
        }
    }
}
