using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.ConsumerAsync
{
    [Collection("Redis")]
    public class MultiConsumerAsync
    {
        [Theory]
        [InlineData(250, 1, 400, 10, 5, 5, ConnectionInfoTypes.Linux),
         InlineData(35, 5, 200, 10, 1, 2, ConnectionInfoTypes.Linux),
         InlineData(10, 8, 180, 7, 1, 1, ConnectionInfoTypes.Windows),
         InlineData(100, 0, 180, 10, 5, 0, ConnectionInfoTypes.Windows)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize, ConnectionInfoTypes type)
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
                                    // ReSharper disable once AccessToDisposedClosure
                                    queueSize, 1, factory, type));

                    var task2 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    // ReSharper disable once AccessToDisposedClosure
                                    queueSize, 2, factory, type));

                    var task3 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    // ReSharper disable once AccessToDisposedClosure
                                    queueSize, 3, factory, type));

                    Task.WaitAll(task1, task2, task3);
                }
            }
        }

        private void RunConsumer(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           int messageType, ITaskFactory factory, ConnectionInfoTypes type)
        {
            var queue = new SimpleConsumerAsync();
            queue.RunWithFactory(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, factory, type);
        }
    }
}