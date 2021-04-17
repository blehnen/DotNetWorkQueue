using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class MultiProducerMethod
    {
        [Theory]
#if NETFULL
        [InlineData(1000, LinqMethodTypes.Dynamic, false),
         InlineData(1000, LinqMethodTypes.Compiled, false)]
#else
        [InlineData(10,LinqMethodTypes.Compiled, true)]
#endif
        public void Run(int messageCount, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.MultiMethodProducer();
            consumer.Run<SqlServerMessageQueueInit, SqlServerMessageQueueCreation>(new QueueConnection(queueName, ConnectionInfo.ConnectionString),
                messageCount, 10, linqMethodTypes, enableChaos, Helpers.GenerateData, VerifyQueueCount);
        }

        private void VerifyQueueCount(QueueConnection arg1, IBaseTransportOptions arg2, ICreationScope arg3, int arg4,
            string arg5)
        {
            //noop
        }
    }
}
