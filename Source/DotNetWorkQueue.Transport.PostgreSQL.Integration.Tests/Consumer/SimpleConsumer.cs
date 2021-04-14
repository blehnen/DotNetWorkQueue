using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Consumer
{
    [Collection("consumer")]
    public class SimpleConsumer
    {
        [Theory]
        [InlineData(50, 5, 200, 10, false, false),
         InlineData(200, 0, 240, 25, false, false),
         InlineData(200, 0, 240, 25, true, false),
         InlineData(50, 5, 200, 10, true, false),
         InlineData(20, 0, 240, 15, false, true),
         InlineData(20, 0, 240, 15, true, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool useTransactions,
            bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.SimpleConsumer();
            consumer.Run<PostgreSqlMessageQueueInit, FakeMessage, PostgreSqlMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
