using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("Consumer")]
    public class ConsumerMethodAsyncErrorTable
    {
        [Theory]
        [InlineData(1, 60, 1, 1, 0, true, LinqMethodTypes.Dynamic, false),
        InlineData(25, 120, 20, 1, 5, false, LinqMethodTypes.Dynamic, false),
        InlineData(25, 120, 20, 1, 5, true, LinqMethodTypes.Compiled, false),
        InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int timeOut, int workerCount,
            int readerCount, int queueSize, bool inMemoryDb, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                        ConsumerMethodAsyncErrorTable();
                consumer.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, timeOut, workerCount, readerCount, queueSize, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                       true, true, false,
                       false, true, true, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
            }
        }

        private void ValidateErrorCounts(QueueConnection queueConnection, int arg3, ICreationScope arg4)
        {
            new VerifyErrorCounts(queueConnection.Queue, queueConnection.Connection).Verify(arg3, 2);
        }

    }
}
