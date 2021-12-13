﻿using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ProducerMethod
{
    [Collection("producer")]
    public class SimpleProducerMethod
    {
        [Theory]
        [InlineData(100, true, true, false, false, false, false, false, LinqMethodTypes.Compiled, false),
#if NETFULL
        InlineData(100, true, true, false, false, false,  false, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, true, false, false, false,  false, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, false, false, false, false, false, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, true, false, false, false, false,  false, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, false, false, false, false,  true, false, LinqMethodTypes.Dynamic, false),
        InlineData(100, false, true, true, true, true, false,  false, LinqMethodTypes.Dynamic, false),
         InlineData(100, false, true,  false, true, true, true, false, LinqMethodTypes.Dynamic, false),
         InlineData(100, true, true,  false, false, false, true, true, LinqMethodTypes.Dynamic, false),
#endif
         InlineData(100, false, true,  false, false, false, true,  false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, false,  false, false, false, false,  false, LinqMethodTypes.Compiled, false),
         InlineData(100, true, false,  false, false, false, false, false, LinqMethodTypes.Compiled, false),
        InlineData(100, false, false, false, false, false, true, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, true,  true, true, true, false, false, LinqMethodTypes.Compiled, false),
         InlineData(100, false, true,  false, true, true, true,  false, LinqMethodTypes.Compiled, false),
         InlineData(100, true, true,  false, false, false, true,  true, LinqMethodTypes.Compiled, false),

         InlineData(10, false, true, false, false, false, true,  false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, false,  false, false, false, false, false, LinqMethodTypes.Compiled, true),
         InlineData(10, true, false,  false, false, false, false, false, LinqMethodTypes.Compiled, true),
        InlineData(10, false, false,  false, false, false, true,  false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, true,  true, true, true, false, false, LinqMethodTypes.Compiled, true),
         InlineData(10, false, true,  false, true, true, true, false, LinqMethodTypes.Compiled, true),
         InlineData(10, true, true,  false, false, false, true, true, LinqMethodTypes.Compiled, true)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableHoldTransactionUntilMessageCommitted,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatusTable,
            bool additionalColumn,
            LinqMethodTypes linqMethodTypes, 
            bool enableChaos)
        {

            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.SimpleMethodProducer();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString),
                messageCount, linqMethodTypes, interceptors, enableChaos, false, x => Helpers.SetOptions(x,
                    enableDelayedProcessing, !enableHoldTransactionUntilMessageCommitted, enableHoldTransactionUntilMessageCommitted, enableMessageExpiration,
                    enablePriority, !enableHoldTransactionUntilMessageCommitted, enableStatusTable, additionalColumn),
                Helpers.GenerateData, Helpers.Verify);
        }
    }
}
