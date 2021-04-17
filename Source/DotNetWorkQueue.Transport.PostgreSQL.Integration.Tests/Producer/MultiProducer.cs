using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Producer
{
    [Collection("producer")]
    public class MultiProducer
    {
        [Theory]
        [InlineData(1000, false),
         InlineData(10, true)]
        public void Run(int messageCount, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.MultiProducer();
            producer.Run<PostgreSqlMessageQueueInit, FakeMessage, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                ConnectionInfo.ConnectionString),
                messageCount, enableChaos, 10, x => { }, Helpers.GenerateData, Helpers.Verify, VerifyQueueData);
        }

        private void VerifyQueueData(QueueConnection arg1, IBaseTransportOptions arg2, ICreationScope arg3, long arg4, long arg5, string arg6)
        {
            new VerifyQueueData(arg1.Queue, (PostgreSqlMessageQueueTransportOptions)arg2).Verify(arg4 * arg5, arg6);
        }
    }
}
