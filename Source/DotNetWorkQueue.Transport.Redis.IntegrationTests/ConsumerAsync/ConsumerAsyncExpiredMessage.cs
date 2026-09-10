using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.ConsumerAsync
{
    /// <summary>
    /// Redis is the transport this test exists for.
    ///
    /// Every other transport expires messages from its own monitor, on its own thread. Redis notices
    /// expiry inside the de-queue itself and removes the message there, so this is the only place
    /// where an expired-message removal runs on the asynchronous receive path - the call #284 made
    /// awaitable. There was no expired-message test on the asynchronous consumer before, which is how
    /// that removal stayed synchronous without anything noticing.
    /// </summary>
    [TestClass]
    public class ConsumerAsyncExpiredMessage
    {
        [TestMethod]
        [DataRow(100, 60, 5, 2),
        DataRow(500, 120, 5, 2)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.ConsumerAsyncExpiredMessage();

            consumer.Run<RedisQueueInit, FakeMessage, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                messageCount, timeOut, workerCount, readerCount, false, x => { },
                Helpers.GenerateDelayExpiredData, Verify, VerifyQueueCount);
        }

        private void VerifyQueueCount(QueueConnection queueConnection, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            using (var count = new VerifyQueueRecordCount(queueConnection.Queue, queueConnection.Connection))
            {
                count.Verify(0, false, -1);
            }
        }

        private void Verify(QueueConnection arg1, QueueProducerConfiguration arg2, long arg3, ICreationScope arg4)
        {
            //only verify count in redis
        }
    }
}
