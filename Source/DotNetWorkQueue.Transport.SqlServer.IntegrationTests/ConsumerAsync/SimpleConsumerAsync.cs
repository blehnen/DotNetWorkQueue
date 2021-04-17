using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class SimpleConsumerAsync
    {
        [Theory]
        [InlineData(500, 1, 400, 10, 5, 5, false, 1, false, "dbo", null),
         InlineData(500, 0, 180, 10, 5, 0, true, 1, false, "dbo", null),
         InlineData(10, 0, 180, 10, 5, 0, true, 1, true, "dbo", null)]
        public async Task Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, int messageType, bool enableChaos, string schema, string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
                queueName = GenerateQueueName.Create();

            var settings = new Dictionary<string, string>();
            settings.SetSchema(schema);
            var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString, settings);

            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.SimpleConsumerAsync();
            await consumer.Run<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(queueConnection,
                messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, enableChaos, x => Helpers.SetOptions(x,
                    false, !useTransactions, useTransactions,
                    false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount).ConfigureAwait(false);
        }
    }
}
