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
    public class ConsumerMethodMultipleDynamic
    {
        [Theory]
        [InlineData(100, 0, 240, 5,  true, IntegrationConnectionInfo.ConnectionTypes.Direct),
         InlineData(1000, 0, 240, 10,  false, IntegrationConnectionInfo.ConnectionTypes.Shared)]
        public void Run(int messageCount, int runtime,
            int timeOut, int workerCount, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                        ConsumerMethodMultipleDynamic();
                consumer.Run<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x, false, false, true),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
