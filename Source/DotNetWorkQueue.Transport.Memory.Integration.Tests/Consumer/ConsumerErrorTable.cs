using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Consumer
{
    [TestClass]
    [Retry(1)]
    public class ConsumerErrorTable
    {
        [TestMethod]
        [DataRow(1, 20, 1),
        DataRow(10, 30, 5)]
        public void Run(int messageCount, int timeOut, int workerCount)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerErrorTable();
                consumer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, timeOut, workerCount, false, x => { },
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount,
                    (_, _, scope) => new VerifyErrorCounts().Verify(scope, messageCount),
                    //memory does not retry: a failed message goes straight to the error queue, which keeps
                    //only a tally and so has nothing to purge
                    attemptsPerMessage: 1, errorRecordsCanBePurged: false);
            }
        }
    }
}
