using System.Threading.Tasks;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.ConsumerAsync
{
    [Collection("consumerasync")]
    public class MultiConsumerAsync
    {
        [Theory]
        [InlineData(10, 5, 65, 10, 1, 2),
         InlineData(10, 8, 60, 7, 1, 1),
         InlineData(100, 0, 30, 10, 5, 0)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize)
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
                                    queueSize, 1, factory));

                    var task2 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, 2, factory));

                    var task3 =
                        Task.Factory.StartNew(
                            () =>
                                RunConsumer(messageCount, runtime, timeOut, workerCount, readerCount,
                                    queueSize, 3, factory));

                    Task.WaitAll(task1, task2, task3);
                }
            }
        }

        private void RunConsumer(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           int messageType, ITaskFactory factory)
        {
            var queue = new SimpleConsumerAsync();
            queue.RunWithFactory(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, factory);
        }
    }
}