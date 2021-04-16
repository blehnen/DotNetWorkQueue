using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ProducerMethod
{
    [Collection("producer")]
    public class MultiProducerMethod
    {
        [Theory]
#if NETFULL
        [InlineData(100, LinqMethodTypes.Dynamic, true),
         InlineData(1000, LinqMethodTypes.Compiled, false)]
#else
        [InlineData(1000, LinqMethodTypes.Compiled, false),
        InlineData(100, LinqMethodTypes.Compiled, true)]
#endif
        public void Run(int messageCount, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.MultiMethodProducer();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, 10, linqMethodTypes, enableChaos, Helpers.GenerateData, VerifyQueueCount);
        }

        private void VerifyQueueCount(QueueConnection arg1, IBaseTransportOptions arg2, ICreationScope arg3, int arg4, string arg5)
        {
            new VerifyQueueData(arg1.Queue, (PostgreSqlMessageQueueTransportOptions)arg2).Verify(arg4, null);
        }
    }
}
