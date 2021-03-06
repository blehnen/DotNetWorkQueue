﻿using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Consumer
{
    [Collection("Consumer")]
    public class ConsumerExpiredMessage
    {
        [Theory]
        [InlineData(100, 0, 60, 5, false, false),
        InlineData(100, 5, 60, 5, true, false),
        InlineData(10, 5, 60, 5, true, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer = new DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.ConsumerExpiredMessage();
            consumer.Run<SqlServerMessageQueueInit, FakeMessage, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions,
                    true,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
