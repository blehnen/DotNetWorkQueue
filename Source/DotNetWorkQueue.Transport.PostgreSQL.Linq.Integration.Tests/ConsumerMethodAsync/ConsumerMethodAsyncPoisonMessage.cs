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
    public class ConsumerMethodAsyncPoisonMessage
    {
        [Theory]
        [InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Compiled, true),
#if NETFULL
        InlineData(1, 60, 1, 1, 0, false, LinqMethodTypes.Dynamic, true),
         InlineData(50, 60, 20, 2, 2, true, LinqMethodTypes.Dynamic, false),
#endif
         InlineData(5, 60, 20, 2, 2, true, LinqMethodTypes.Compiled, true)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {

            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                    ConsumerMethodAsyncPoisonMessage();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, timeOut, workerCount, readerCount, queueSize, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);
        }

        private void ValidateErrorCounts(string arg1, string arg2, int arg3, ICreationScope arg4)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(arg1).Verify(arg3, 0);
        }
    }
}
