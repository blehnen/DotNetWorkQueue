using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.ConsumerAsync
{
    [Collection("consumerasyncmulti")]
    public class MultiConsumerAsync
    {
        [Theory]
        [InlineData(250, 1, 90, 10, 5, 5, false, false),
         InlineData(250, 1, 90, 10, 5, 5, true, false),
         InlineData(100, 0, 90, 10, 5, 0, false, false),
         InlineData(100, 0, 90, 10, 5, 0, true, false),
         InlineData(25, 1, 90, 10, 5, 5, true, true),
         InlineData(10, 0, 90, 10, 5, 0, false, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions, bool enableChaos)
        {
            var factory = SimpleConsumerAsync.CreateFactory(workerCount, queueSize, out var schedulerContainer);
            using (schedulerContainer)
            {
                using (factory.Scheduler)
                {
                    var task1 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, useTransactions, 1, factory, enableChaos));

                    var task2 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, useTransactions, 2, factory, enableChaos));

                    var task3 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, useTransactions, 3, factory, enableChaos));

                    Task.WaitAll(task1, task2, task3);
                }
            }
        }

        private void RunConsumer(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           bool useTransactions, int messageType, ITaskFactory factory, bool enableChaos)
        {
            var queue = new SimpleConsumerAsync();
            queue.RunWithFactory(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, useTransactions, messageType, factory, enableChaos);
        }
    }
}