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
    public class SimpleConsumerMethodAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(5, 5, 200, 10, 1, 2, false, 1, LinqMethodTypes.Compiled, true),
#if NETFULL
        InlineData(5, 5, 200, 10, 1, 2, false, 1, LinqMethodTypes.Dynamic, true),
         InlineData(10, 5, 180, 7, 1, 2, true, 1, LinqMethodTypes.Dynamic, false),
#endif
         InlineData(10, 5, 180, 7, 1, 2, true, 1, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, int messageType, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync.Implementation.
                    SimpleMethodConsumerAsync();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
                    true, !useTransactions, useTransactions, false,
                    false, !useTransactions, true, false),
                Helpers.GenerateData, Helpers.Verify, Helpers.VerifyQueueCount);
        }
    }
}
