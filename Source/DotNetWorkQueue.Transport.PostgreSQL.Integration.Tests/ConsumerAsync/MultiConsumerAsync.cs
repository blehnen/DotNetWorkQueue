using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.ConsumerAsync
{
    [Collection("postgresql")]
    public class MultiConsumerAsync
    {
        [Theory]
        [InlineData(250, 1, 90, 10, 5, 5, false),
         InlineData(250, 1, 90, 10, 5, 5, true),
         InlineData(100, 0, 90, 10, 5, 0, false),
         InlineData(100, 0, 90, 10, 5, 0, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            bool useTransactions)
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
                                    queueSize, useTransactions, 1, factory));

                    var task2 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, useTransactions, 2, factory));

                    var task3 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, useTransactions, 3, factory));

                    Task.WaitAll(task1, task2, task3);
                }
            }
        }

        private void RunConsumer(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           bool useTransactions, int messageType, ITaskFactory factory)
        {
            var queue = new SimpleConsumerAsync();
            queue.RunWithFactory(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, useTransactions, messageType, factory);
        }
    }
}