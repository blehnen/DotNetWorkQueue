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
    public class SimpleMethodConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [TestMethod]
        [DataRow(10, 45, 260, 7, 1, 1, 1, false, LinqMethodTypes.Dynamic, false),
         DataRow(10, 45, 260, 7, 1, 1, 1, true, LinqMethodTypes.Dynamic, true),
         DataRow(50, 1, 400, 10, 5, 5, 1, false, LinqMethodTypes.Compiled, true),
         DataRow(50, 5, 200, 10, 1, 2, 1, true, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int runtime, int timeOut,
            int workerCount, int readerCount, int queueSize,
           int messageType, bool inMemoryDb, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                        SimpleMethodConsumerAsync();
                consumer.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                        false, true, false,
                        false, true, true, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
