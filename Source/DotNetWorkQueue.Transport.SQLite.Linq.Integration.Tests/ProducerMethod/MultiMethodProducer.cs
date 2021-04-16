using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class MultiMethodProducer
    {
        [Theory]
        [InlineData(100, true, LinqMethodTypes.Dynamic, false),
        InlineData(10, false, LinqMethodTypes.Dynamic, true),
        InlineData(10, true, LinqMethodTypes.Compiled, true),
        InlineData(100, false, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, bool inMemoryDb, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.MultiMethodProducer();
                consumer.Run<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(queueName,
                    connectionInfo.ConnectionString,
                    messageCount, 10, linqMethodTypes, enableChaos, Helpers.GenerateData, VerifyQueueCount);
            }
        }

        private void VerifyQueueCount(QueueConnection arg1, IBaseTransportOptions arg2, ICreationScope arg3, int arg4, string arg5)
        {
            //noop
        }
    }
}
