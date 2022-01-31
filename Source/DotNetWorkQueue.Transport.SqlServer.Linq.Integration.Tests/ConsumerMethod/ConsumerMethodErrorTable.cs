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
    public class ConsumerMethodErrorTable
    {
        [Theory]
        [InlineData(100, 60, 20, true, LinqMethodTypes.Compiled, false),
#if NETFULL
        InlineData(1, 10, 1, false, LinqMethodTypes.Dynamic, false),
        InlineData(100, 60, 20, true, LinqMethodTypes.Dynamic, false),
#endif
         InlineData(3, 40, 5, false, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int timeOut, int workerCount,
            bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.ConsumerMethodErrorTable();
            consumer.Run<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, timeOut, workerCount, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions,
                    false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
        }

        private void ValidateErrorCounts(QueueConnection queueConnection, int arg3, ICreationScope arg4)
        {
            new VerifyErrorCounts(queueConnection).Verify(arg3, 2);
        }
    }
}
