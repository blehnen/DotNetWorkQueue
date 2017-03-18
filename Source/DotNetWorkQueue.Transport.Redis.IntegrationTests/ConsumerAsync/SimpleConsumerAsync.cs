// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using System;
using Xunit;
namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.ConsumerAsync
{
    [Collection("Redis")]
    public class SimpleConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(500, 1, 400, 10, 5, 5, 1, ConnectionInfoTypes.Linux),
         InlineData(50, 5, 200, 10, 1, 2, 1, ConnectionInfoTypes.Linux),
         InlineData(10, 5, 180, 7, 1, 1, 1, ConnectionInfoTypes.Linux),
         InlineData(500, 0, 180, 10, 5, 0, 1, ConnectionInfoTypes.Linux),
            InlineData(500, 1, 400, 10, 5, 5, 1, ConnectionInfoTypes.Windows),
         InlineData(50, 5, 200, 10, 1, 2, 1, ConnectionInfoTypes.Windows),
         InlineData(10, 5, 180, 7, 1, 1, 1, ConnectionInfoTypes.Windows),
         InlineData(500, 0, 180, 10, 5, 0, 1, ConnectionInfoTypes.Windows)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            int messageType, ConnectionInfoTypes type)
        {
            SchedulerContainer schedulerContainer = null;
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize, out schedulerContainer);
            }

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (var queueCreator =
                new QueueCreationContainer<RedisQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    if (messageType == 1)
                    {
                        var producer = new ProducerAsyncShared();
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false).Wait(timeOut * 1000 / 2);

                        var consumer = new ConsumerAsyncShared<FakeMessage> {Factory = Factory};
                        consumer.RunConsumer<RedisQueueInit>(queueName, connectionString, false,
                            logProvider,
                            runtime, messageCount,
                            timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12));
                    }
                    else if (messageType == 2)
                    {
                        var producer = new ProducerAsyncShared();
                        producer.RunTest<RedisQueueInit, FakeMessageA>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false).Wait(timeOut * 1000 / 2);

                        var consumer = new ConsumerAsyncShared<FakeMessageA> {Factory = Factory};
                        consumer.RunConsumer<RedisQueueInit>(queueName, connectionString, false,
                            logProvider,
                            runtime, messageCount,
                            timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12));
                    }
                    else if (messageType == 3)
                    {
                        var producer = new ProducerAsyncShared();
                        producer.RunTest<RedisQueueInit, FakeMessageB>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false).Wait(timeOut * 1000 / 2);


                        var consumer = new ConsumerAsyncShared<FakeMessageB> {Factory = Factory};
                        consumer.RunConsumer<RedisQueueInit>(queueName, connectionString, false,
                            logProvider,
                            runtime, messageCount,
                            timeOut, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12));
                    }

                    using (var count = new VerifyQueueRecordCount(queueName, connectionString))
                    {
                        count.Verify(0, false, -1);
                    }
                }
                finally
                {
                    schedulerContainer?.Dispose();
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<RedisQueueCreation>(queueName,
                               connectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }

        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            int messageType, ITaskFactory factory, ConnectionInfoTypes type)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, type);
        }


        public static ITaskFactory CreateFactory(int maxThreads, int maxQueueSize, out SchedulerContainer schedulerCreator)
        {
            schedulerCreator = new SchedulerContainer();
            var taskScheduler = schedulerCreator.CreateTaskScheduler();

            taskScheduler.Configuration.MaximumThreads = maxThreads;
            taskScheduler.Configuration.MaxQueueSize = maxQueueSize;

            taskScheduler.Start();
            return schedulerCreator.CreateTaskFactory(taskScheduler);
        }
    }
}
