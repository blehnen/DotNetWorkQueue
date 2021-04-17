using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Producer
{
    [Collection("Producer")]
    public class MultiProducer
    {
        [Theory]
        [InlineData(100, true, false),
        InlineData(100, false, false),
        InlineData(10, true, true),
        InlineData(10, false, true)]
        public void Run(int messageCount, bool inMemoryDb, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.MultiProducer();
                producer.Run<SqLiteMessageQueueInit, FakeMessage, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, enableChaos, 10, x => { }, Helpers.GenerateData, Helpers.Verify, VerifyQueueData);
            }
        }

        private void VerifyQueueData(QueueConnection arg1, IBaseTransportOptions arg2, ICreationScope arg3, long arg4, long arg5, string arg6)
        {
            new VerifyQueueData(arg1, (SqLiteMessageQueueTransportOptions)arg2).Verify(arg4 * arg5, arg6);
        }
    }
}
