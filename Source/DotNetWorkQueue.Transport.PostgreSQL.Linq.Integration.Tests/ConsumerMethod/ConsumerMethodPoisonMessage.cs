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
    public class ConsumerMethodPoisonMessage
    {
        [Theory]
#if NETFULL
        [InlineData(3, 60, 5, false, LinqMethodTypes.Dynamic, true),
         InlineData(3, 60, 5, true, LinqMethodTypes.Compiled, true)]
#else
        [InlineData(10, 60, 5, true, LinqMethodTypes.Compiled, false)]
#endif
        public void Run(int messageCount, int timeOut, int workerCount,
            bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                    ConsumerMethodPoisonMessage();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString),
                messageCount, timeOut, workerCount, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount, ValidateErrorCounts);

        }

        private void ValidateErrorCounts(QueueConnection queueConnection, int arg3, ICreationScope arg4)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            new VerifyErrorCounts(queueConnection.Queue).Verify(arg3, 0);
        }
    }
}
