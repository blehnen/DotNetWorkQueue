using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodPoisonMessage
    {
        [Theory]
        [InlineData(1, 60, 1, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(10, 60, 5, LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Memory)]
        public void Run(int messageCount, int timeOut,
            int workerCount, LinqMethodTypes linqMethodTypes, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                        ConsumerMethodPoisonMessage();
                consumer.Run<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, timeOut, workerCount, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x, false, false, true),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
            }
        }

        private void ValidateErrorCounts(string queueName, string connectionString, int messageCount, ICreationScope scope)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueName, connectionString, scope).Verify(messageCount, 0);
        }
    }
}
