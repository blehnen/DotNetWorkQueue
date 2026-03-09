using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.ConsumerMethodAsync
{
    [TestClass]
    public class ConsumerMethodAsyncPoisonMessage
    {
        [TestMethod]
        [DataRow(1, 60, 1, 1, 0, true, LinqMethodTypes.Dynamic, false),
         DataRow(10, 60, 5, 1, 0, false, LinqMethodTypes.Dynamic, false),
         DataRow(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, false),
         DataRow(10, 60, 5, 1, 0, true, LinqMethodTypes.Compiled, false),
         DataRow(1, 60, 5, 1, 0, false, LinqMethodTypes.Dynamic, true),
         DataRow(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int timeOut, int workerCount,
            int readerCount, int queueSize, bool inMemoryDb, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                        ConsumerMethodAsyncPoisonMessage();
                consumer.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, timeOut, workerCount, readerCount, queueSize, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                        false, true, false,
                        false, true, true, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
            }
        }

        private void ValidateErrorCounts(QueueConnection queueConnection, int messageCount, ICreationScope arg4)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueConnection.Queue, queueConnection.Connection).Verify(messageCount, 0);
        }
    }
}
