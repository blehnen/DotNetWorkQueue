#if NETFULL
using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("consumer")]
    public class ConsumerMethodMultipleDynamic
    {
        [Collection("PostgreSQL")]
        public class SimpleMethodConsumer
        {
           [Theory]
           [InlineData(100, 0, 240, 5, false, false),
           InlineData(200, 0, 240, 25, false, false),
           InlineData(200, 0, 240, 25, true, false),
           InlineData(100, 0, 240, 5, true, false),
            InlineData(10, 0, 240, 5, true, true)]
            public void Run(int messageCount, int runtime, int timeOut, 
                int workerCount, bool useTransactions, bool enableChaos)
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                        ConsumerMethodMultipleDynamic();
                        consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                                ConnectionInfo.ConnectionString),
                    messageCount, runtime, timeOut, workerCount, enableChaos, x => Helpers.SetOptions(x,
                        true, !useTransactions, useTransactions, false,
                        false, !useTransactions, true, false),
                    Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
            }
        }
    }
}
#endif
