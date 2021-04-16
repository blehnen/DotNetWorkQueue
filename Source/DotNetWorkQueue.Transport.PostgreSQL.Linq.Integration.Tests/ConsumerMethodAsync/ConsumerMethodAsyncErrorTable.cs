using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("consumerasync")]
    public class ConsumerMethodAsyncErrorTable
    {
        [Theory]
#if NETFULL
        [InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Dynamic, true),
        InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, false)]
#else
        [InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, true)]
#endif
        public void Run(int messageCount, int timeOut, int workerCount, 
            int readerCount, int queueSize, bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                    ConsumerMethodAsyncErrorTable();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, timeOut, workerCount, readerCount, queueSize, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);

        }

        private void ValidateErrorCounts(string arg1, string arg2, int arg3, ICreationScope arg4)
        {
            new VerifyErrorCounts(arg1).Verify(arg3, 2);
        }
    }
}
