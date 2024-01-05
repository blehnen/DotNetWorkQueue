using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Memory.Basic;
using System;
using System.Threading;
using Xunit;

namespace DotNetWorkQueue.Tests.Transport
{
    public class CreateScheduleProducerAndConsumer
    {
        [Fact()]
        public void Create_Test()
        {
            using (var schedulerContainer = new SchedulerContainer())
            {
                using (var scheduler = schedulerContainer.CreateTaskScheduler())
                {
                    var factory = schedulerContainer.CreateTaskFactory(scheduler);
                    factory.Scheduler.Configuration.MaximumThreads = Environment.ProcessorCount;
                    factory.Scheduler.Start();
                    using (var queueContainer = new QueueContainer<MemoryMessageQueueInit>())
                    {
                        var queueConnection = new QueueConnection("test", "memory");
                        using (
                            var queue = queueContainer.CreateConsumerQueueScheduler(queueConnection, factory)
                        )
                        {
                            queue.Start<DummyMessage>(Handle, new ConsumerQueueNotifications());
                            Thread.Sleep(1000);
                        }
                    }
                }
            }
        }
        private void Handle(IReceivedMessage<DummyMessage> m, IWorkerNotification n)
        {
            //processing logic goes here
        }

        private class DummyMessage
        {

        }
    }
}
