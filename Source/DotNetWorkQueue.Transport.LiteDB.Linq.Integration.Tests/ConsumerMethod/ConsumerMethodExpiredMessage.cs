using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodExpiredMessage
    {
        [Theory]
        [InlineData(100, 0, 60, 5, LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(10, 0, 60, 5, LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory)]
        public void Run(int messageCount, int runtime,
            int timeOut, int workerCount, LinqMethodTypes linqMethodTypes, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                        ConsumerMethodExpiredMessage();
                consumer.Run<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        connectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x, true, true, true),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
