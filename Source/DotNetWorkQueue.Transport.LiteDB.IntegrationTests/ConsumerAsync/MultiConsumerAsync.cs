using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class MultiConsumerAsync
    {
        [Theory]
        [InlineData(100, 1, 400, 10, 1, 5, false),
         InlineData(50, 0, 180, 10, 1, 0,  false),
         InlineData(10, 0, 180, 10, 1, 0,  true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, 
            int readerCount, int queueSize, bool enableChaos)
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
                                    queueSize, 1, factory, enableChaos));

                    var task2 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, 2, factory, enableChaos));

                    var task3 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, 3, factory, enableChaos));

                    Task.WaitAll(task1, task2, task3);
                }
            }
        }

        private void RunConsumer(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           int messageType, ITaskFactory factory, bool enableChaos)
        {
            var queue = new SimpleConsumerAsync();
            queue.RunWithFactory(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, factory, enableChaos);
        }
    }
}