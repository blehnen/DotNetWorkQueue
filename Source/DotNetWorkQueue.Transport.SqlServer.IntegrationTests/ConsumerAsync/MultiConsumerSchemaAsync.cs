using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests.ConsumerAsync;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class MultiConsumerSchemaAsync
    {
        [Theory]
        [InlineData(2000, 0, 400, 10, 5, 5, false, false)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
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

            Task.WaitAll(task1.Result, task2.Result, task3.Result);
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
