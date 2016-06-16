// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.ConsumerMethod
{
    [Collection("Redis")]
    public class ConsumerMethodCancelWork
    {
        [Theory]
        [InlineData(7, 5, 90, 3, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(7, 5, 90, 3, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
            InlineData(7, 5, 90, 3, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(7, 5, 90, 3, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic)]
        public void Run(int messageCount, int runtime, 
            int timeOut, int workerCount, ConnectionInfoTypes type, LinqMethodTypes linqMethodTypes)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (var queueCreator =
                new QueueCreationContainer<RedisQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    var id = Guid.NewGuid();
                    var producer = new ProducerMethodShared();
                    if (linqMethodTypes == LinqMethodTypes.Compiled)
                    {
                        producer.RunTestCompiled<RedisQueueInit>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateCancelCompiled, runtime);
                    }
                    else
                    {
                        producer.RunTestDynamic<RedisQueueInit>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateCancelDynamic, runtime);
                    }

                    var consumer = new ConsumerMethodCancelWorkShared<RedisQueueInit>();
                    consumer.RunConsumer(queueName, connectionString, false, logProvider,
                        runtime, messageCount,
                        workerCount, timeOut, serviceRegister => serviceRegister.Register<IMessageMethodHandling>(() => new MethodMessageProcessingCancel(id), LifeStyles.Singleton), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), id);

                    new VerifyQueueRecordCount(queueName, connectionString).Verify(0, false);
                    GenerateMethod.ClearCancel(id);
                }
                finally
                {
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
    }
}
