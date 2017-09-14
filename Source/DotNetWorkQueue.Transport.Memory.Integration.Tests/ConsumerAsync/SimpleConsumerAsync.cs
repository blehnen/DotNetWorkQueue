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

using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.ConsumerAsync
{
    public class SimpleConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(10, 5, 60, 7, 1, 1, 1),
         InlineData(100, 0, 30, 10, 5, 0, 1)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
           int messageType)
        {
            SchedulerContainer schedulerContainer = null;
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize, out schedulerContainer);
            }


            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<MessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            if (messageType == 1)
                            {
                                var producer = new ProducerAsyncShared();
                                producer.RunTestAsync<MessageQueueInit, FakeMessage>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, oCreation.Scope).Wait(timeOut / 2 * 1000);

                                var consumer = new ConsumerAsyncShared<FakeMessage> { Factory = Factory };
                                consumer.RunConsumer<MessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)");
                            }
                            else if (messageType == 2)
                            {
                                var producer = new ProducerAsyncShared();
                                producer.RunTestAsync<MessageQueueInit, FakeMessageA>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, oCreation.Scope).Wait(timeOut / 2 * 1000);

                                var consumer = new ConsumerAsyncShared<FakeMessageA> { Factory = Factory };
                                consumer.RunConsumer<MessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)");
                            }
                            else if (messageType == 3)
                            {
                                var producer = new ProducerAsyncShared();
                                producer.RunTestAsync<MessageQueueInit, FakeMessageB>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, oCreation.Scope).Wait(timeOut / 2 * 1000);

                                var consumer = new ConsumerAsyncShared<FakeMessageB> { Factory = Factory };
                                consumer.RunConsumer<MessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount,
                                    TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), "second(*%10)");
                            }

                            new VerifyQueueRecordCount().Verify(oCreation.Scope, 0, true);
                        }
                    }
                    finally
                    {
                        schedulerContainer?.Dispose();
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                   connectionInfo.ConnectionString)
                            )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }

        public void RunWithFactory(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            int messageType, ITaskFactory factory)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType);
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
