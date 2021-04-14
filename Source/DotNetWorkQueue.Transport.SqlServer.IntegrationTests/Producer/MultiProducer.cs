using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Producer
{
    [Collection("Producer")]
    public class MultiProducer
    {
        [Theory]
        [InlineData(1000, false),
         InlineData(10, true)]
        public void Run(int messageCount, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.MultiProducer();
            producer.Run<SqlServerMessageQueueInit, FakeMessage, SqlServerMessageQueueCreation>(queueName,
                ConnectionInfo.ConnectionString,
                messageCount, enableChaos, 10, x => { }, Helpers.GenerateData, Helpers.Verify, VerifyQueueData);
        }

        private void VerifyQueueData(QueueConnection arg1, IBaseTransportOptions arg2, ICreationScope arg3, long arg4, long arg5, string arg6)
        {
            new VerifyQueueData(arg1, (SqlServerMessageQueueTransportOptions)arg2).Verify(arg4 * arg5);
        }
    }
}
