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
    public class SimpleMethodConsumer
    {
        [Theory]
        [InlineData(10, 0, 240, 5, false, LinqMethodTypes.Compiled, true),
#if NETFULL
         InlineData(100, 0, 240, 5, false, LinqMethodTypes.Dynamic, false),
         InlineData(10, 15, 180, 7, true, LinqMethodTypes.Dynamic, false),
#endif
         InlineData(10, 15, 180, 7, true, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int runtime, int timeOut,
            int workerCount, bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                    SimpleMethodConsumer();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, runtime, timeOut, workerCount, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
