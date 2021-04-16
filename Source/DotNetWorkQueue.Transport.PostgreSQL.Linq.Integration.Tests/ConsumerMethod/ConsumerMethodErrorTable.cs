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
    public class ConsumerMethodErrorTable
    {
        [Theory]
        [InlineData(10, 60, 20, true, LinqMethodTypes.Compiled, true),
#if NETFULL
        InlineData(100, 60, 20, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, 60, 20, true, LinqMethodTypes.Dynamic, false),
         InlineData(1, 60, 5, true, LinqMethodTypes.Dynamic, true),
#endif
         InlineData(10, 60, 5, true, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, int timeOut, int workerCount, 
            bool useTransactions, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.ConsumerMethodErrorTable();
            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount,  timeOut, workerCount, linqMethodTypes, enableChaos, x => Helpers.SetOptions(x,
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
