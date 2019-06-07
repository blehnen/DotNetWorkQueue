using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Integration.Tests.ConsumerAsync
{
    [Collection("SQLite")]
    public class MultiConsumerAsync
    {
        [Theory]
        [InlineData(100, 1, 180, 10, 1, 5, true, false),
         InlineData(100, 0, 160, 10, 1, 0, false, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, 
            int readerCount, int queueSize, bool inMemoryDb, bool enableChaos)
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
                                    queueSize, 1, inMemoryDb, factory, enableChaos));

                    var task2 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, 2, inMemoryDb, factory, enableChaos));

                    var task3 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, 3, inMemoryDb, factory, enableChaos));

                    Task.WaitAll(task1, task2, task3);
                }
            }
        }

        private void RunConsumer(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           int messageType, bool inMemoryDb, ITaskFactory factory, bool enableChaos)
        {
            var queue = new SimpleConsumerAsync();
            queue.RunWithFactory(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, inMemoryDb, factory, enableChaos);
        }
    }
}