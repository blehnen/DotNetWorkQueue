using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.SQLite.Basic;
using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.Producer
{
    [Collection("Producer")]
    public class SimpleProducerAsyncBatch
    {
        [Theory]
        [InlineData(1000, true, true, true, false, false, true, false, false, true, false),
         InlineData(100, false, true, true, false, false, true, false, false, true, false),
         InlineData(100, false, false, false, false, false, false, false, false, true, false),
         InlineData(100, true, false, false, false, false, false, false, false, true, false),
         InlineData(100, false, false, false, false, false, false, true, false, true, false),
         InlineData(100, false, false, false, false, false, true, true, false, true, false),
         InlineData(100, false, true, false, true, true, false, true, false, true, false),
         InlineData(100, false, true, true, true, true, true, true, false, true, false),
         InlineData(100, true, true, true, false, false, true, false, true, true, false),

         InlineData(100, true, true, true, false, false, true, false, false, false, false),
         InlineData(100, false, true, true, false, false, true, false, false, false, false),
         InlineData(100, false, false, false, false, false, false, false, false, false, false),
         InlineData(100, true, false, false, false, false, false, false, false, false, false),
         InlineData(100, false, false, false, false, false, false, true, false, false, false),
         InlineData(100, false, false, false, false, false, true, true, false, false, false),
         InlineData(100, false, true, false, true, true, false, true, false, false, false),
         InlineData(100, false, true, true, true, true, true, true, false, false, false),
         InlineData(1000, true, true, true, false, false, true, false, true, false, false),

         InlineData(10, true, true, true, false, false, true, false, false, true, true),
         InlineData(10, false, true, true, false, false, true, false, false, true, true),
         InlineData(10, false, false, false, false, false, false, false, false, true, true),
         InlineData(10, true, false, false, false, false, false, false, false, true, true),
         InlineData(10, false, false, false, false, false, false, true, false, true, true),
         InlineData(10, false, false, false, false, false, true, true, false, true, true),
         InlineData(10, false, true, false, true, true, false, true, false, true, true),
         InlineData(10, false, true, true, true, true, true, true, false, true, true),
         InlineData(10, true, true, true, false, false, true, false, true, true, true),

         InlineData(10, true, true, true, false, false, true, false, false, false, true),
         InlineData(10, false, true, true, false, false, true, false, false, false, true),
         InlineData(10, false, false, false, false, false, false, false, false, false, true),
         InlineData(10, true, false, false, false, false, false, false, false, false, true),
         InlineData(10, false, false, false, false, false, false, true, false, false, true),
         InlineData(10, false, false, false, false, false, true, true, false, false, true),
         InlineData(10, false, true, false, true, true, false, true, false, false, true),
         InlineData(10, false, true, true, true, true, true, true, false, false, true),
         InlineData(10, true, true, true, false, false, true, false, true, false, true)]
        public async Task Run(
            int messageCount,
            bool interceptors,
            bool enableDelayedProcessing,
            bool enableHeartBeat,
            bool enableMessageExpiration,
            bool enablePriority,
            bool enableStatus,
            bool enableStatusTable,
            bool additionalColumn,
            bool inMemoryDb,
            bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducerAsync();
                await producer.Run<SqLiteMessageQueueInit, FakeMessage, SqLiteMessageQueueCreation>(new QueueConnection(queueName, connectionInfo.ConnectionString),
                    messageCount, interceptors, enableChaos, true, x => Helpers.SetOptions(x,
                        enableDelayedProcessing, enableHeartBeat, enableMessageExpiration,
                        enablePriority, enableStatus, enableStatusTable, additionalColumn),
                    Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
