using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Consumer
{
    [Collection("consumer")]
    public class ConsumerRollBack
    {

        [Theory]
        [InlineData(500, 0, 60, 5, false, false),
        InlineData(50, 5, 90, 10, false, false),
        InlineData(500, 0, 60, 5, true, false),
        InlineData(50, 5, 90, 10, true, false),
        InlineData(5, 5, 90, 10, false, true),
        InlineData(50, 0, 60, 5, true, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerRollBack();
            consumer.Run<PostgreSqlMessageQueueInit, FakeMessage, PostgreSqlMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
