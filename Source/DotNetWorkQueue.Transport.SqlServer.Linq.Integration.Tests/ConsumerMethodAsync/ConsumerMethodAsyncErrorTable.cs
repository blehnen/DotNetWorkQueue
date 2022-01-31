using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using NSubstitute.Extensions;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerMethodAsyncErrorTable
    {
        [Theory]
#if NETFULL
        [InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Dynamic, false),
        InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, true)]
#else
        [InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, false)]
#endif
        public void Run(int messageCount, int timeOut, int workerCount,
            int readerCount, int queueSize, bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                    ConsumerMethodAsyncErrorTable();
            consumer.Run<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, timeOut, workerCount, readerCount, queueSize, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
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
