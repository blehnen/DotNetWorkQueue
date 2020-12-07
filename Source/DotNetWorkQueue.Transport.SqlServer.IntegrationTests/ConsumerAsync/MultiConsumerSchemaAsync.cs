using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var factory = SimpleConsumerAsync.CreateFactory(workerCount, queueSize, out var schedulerContainer);
            using (schedulerContainer)
            {
                using (factory.Scheduler)
                {
                    var queueName = GenerateQueueName.Create();
                    var task1 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, useTransactions, 1, factory, enableChaos, ConnectionInfo.Schema1, queueName));

                    var task2 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, useTransactions, 2, factory, enableChaos, ConnectionInfo.Schema2, queueName));

                    var task3 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, useTransactions, 3, factory, enableChaos, ConnectionInfo.SchemaDefault, queueName));

                    Task.WaitAll(task1, task2, task3);
                }
            }
        }

        private void RunConsumer(int messageCount, int runtime, int timeOut, int workerCount, int readerCount,
            int queueSize,
            bool useTransactions, int messageType, ITaskFactory factory, bool enableChaos, string schema, string queueName)
        {
            var queue = new SimpleConsumerAsync();
            queue.RunWithFactory(messageCount, runtime, timeOut, workerCount, readerCount, queueSize,
                useTransactions, messageType, factory, enableChaos, schema, queueName);
        }
    }
}
