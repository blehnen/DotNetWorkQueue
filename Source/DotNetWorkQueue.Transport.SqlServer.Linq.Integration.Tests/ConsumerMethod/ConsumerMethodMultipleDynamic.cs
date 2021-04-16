#if NETFULL
using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ConsumerMethod
{
    public class ConsumerMethodMultipleDynamic
    {
        [Collection("Consumer")]
        public class SimpleMethodConsumer
        {
           [Theory]
           [InlineData(20, 0, 240, 5, false, true),
           InlineData(2000, 0, 240, 25, false, false),
           InlineData(2000, 0, 240, 25, true, false),
           InlineData(20, 0, 240, 5, true, true)]
            public void Run(int messageCount, int runtime, int timeOut, int workerCount, bool useTransactions, bool enableChaos)
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                        ConsumerMethodMultipleDynamic();
                consumer.Run<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(queueName,
                    ConnectionInfo.ConnectionString,
                    messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                        true, !useTransactions, useTransactions,
                        false,
                        false, !useTransactions, true, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
#endif
