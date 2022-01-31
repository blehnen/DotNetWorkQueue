using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("Consumer")]
    public class ConsumerMethodAsyncErrorTable
    {
        [Theory]
        [InlineData(1, 60, 1, 1, 0, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
        InlineData(25, 200, 20, 1, 5, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Memory),
        InlineData(1, 60, 1, 1, 0, LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(int messageCount, int timeOut, int workerCount,
            int readerCount, int queueSize, LinqMethodTypes linqMethodTypes, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                        ConsumerMethodAsyncErrorTable();

                consumer.Run<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, timeOut, workerCount, readerCount, queueSize, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x, true, false, true),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
            }
        }
        private void ValidateErrorCounts(QueueConnection queueConnection, int messageCount, ICreationScope scope)
        {
            new VerifyErrorCounts(queueConnection.Queue, queueConnection.Connection, scope).Verify(messageCount, 2);
        }
    }
}
