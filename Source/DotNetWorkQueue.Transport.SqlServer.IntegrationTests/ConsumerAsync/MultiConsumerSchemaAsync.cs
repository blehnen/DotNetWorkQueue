using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class MultiConsumerSchemaAsync
    {
        [Theory]
        [InlineData(2000, 0, 400, 10, 5, 5, false, false)]
        public async Task Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var task1 =
                Task.Factory.StartNew(
                     () =>
                        RunConsumer(queueName, messageCount, runtime, timeOut, workerCount, readerCount, queueSize,
                            useTransactions,
                            enableChaos, ConnectionInfo.Schema1, 1));

            var task2 =
                Task.Factory.StartNew(
                     () =>
                        RunConsumer(queueName, messageCount, runtime, timeOut, workerCount, readerCount, queueSize,
                            useTransactions,
                            enableChaos, ConnectionInfo.Schema2, 2));
            var task3 =
                Task.Factory.StartNew(
                     () =>
                        RunConsumer(queueName, messageCount, runtime, timeOut, workerCount, readerCount, queueSize,
                            useTransactions,
                            enableChaos, ConnectionInfo.SchemaDefault, 3));

            await Task.WhenAll(task1, task2, task3);
        }

        private async Task RunConsumer(string queueName, int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, bool enableChaos, string schema, int messageType)
        {
            var consumer = new SimpleConsumerAsync();
            await consumer.Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, useTransactions,
                messageType, enableChaos,
                schema, queueName).ConfigureAwait(false);
        }
    }
}
