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
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Memory.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ConsumerMethodAsync
{
    public class SimpleMethodConsumerAsync
    {
        private ITaskFactory Factory { get; set; }

        [Theory]
        [InlineData(10, 15, 60, 7, 1, 1, 1, LinqMethodTypes.Dynamic),
         InlineData(10, 5, 60, 10, 1, 2, 1, LinqMethodTypes.Compiled)]
        public void Run(int messageCount, int runtime, int timeOut,
            int workerCount, int readerCount, int queueSize,
           int messageType, LinqMethodTypes linqMethodTypes)
        {
            if (Factory == null)
            {
                Factory = CreateFactory(workerCount, queueSize);
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
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<MessageQueueInit>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<MessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id);
                            }
                            else if (messageType == 2)
                            {
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<MessageQueueInit>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<MessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id);
                            }
                            else if (messageType == 3)
                            {
                                var id = Guid.NewGuid();
                                var producer = new ProducerMethodAsyncShared();
                                producer.RunTestAsync<MessageQueueInit>(queueName,
                                    connectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                                    Helpers.Verify, false, runtime, id, linqMethodTypes, oCreation.Scope).Wait(timeOut);

                                var consumer = new ConsumerMethodAsyncShared { Factory = Factory };
                                consumer.RunConsumer<MessageQueueInit>(queueName, connectionInfo.ConnectionString,
                                    false, logProvider,
                                    runtime, messageCount,
                                    timeOut, readerCount, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), id);
                            }

                            new VerifyQueueRecordCount().Verify(oCreation.Scope, 0, true);
                        }
                    }
                    finally
                    {
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

        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize,
            int messageType, ITaskFactory factory, LinqMethodTypes linqMethodTypes)
        {
            Factory = factory;
            Run(messageCount, runtime, timeOut, workerCount, readerCount, queueSize, messageType, linqMethodTypes);
        }


        public static ITaskFactory CreateFactory(int maxThreads, int maxQueueSize)
        {
            var schedulerCreator = new SchedulerContainer();
            var taskScheduler = schedulerCreator.CreateTaskScheduler();

            taskScheduler.Configuration.MaximumThreads = maxThreads;
            taskScheduler.Configuration.MaxQueueSize = maxQueueSize;

            taskScheduler.Start();
            return schedulerCreator.CreateTaskFactory(taskScheduler);
        }
    }
}
