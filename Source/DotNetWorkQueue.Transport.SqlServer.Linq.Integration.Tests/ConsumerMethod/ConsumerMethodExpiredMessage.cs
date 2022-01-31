using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class ConsumerMethodExpiredMessage
    {
        [Theory]
        [InlineData(100, 0, 60, 5, false, LinqMethodTypes.Compiled, false),
#if NETFULL
        InlineData(100, 5, 60, 5, true, LinqMethodTypes.Dynamic, false),
        InlineData(100, 0, 60, 5, false, LinqMethodTypes.Dynamic, false),
#endif
        InlineData(100, 5, 120, 5, true, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount,
            bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                    ConsumerMethodExpiredMessage();
            consumer.Run<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, runtime, timeOut, workerCount, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions,
                    true,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
