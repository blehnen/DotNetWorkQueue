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
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("Redis")]
    public class ConsumerMethodAsyncPoisonMessage
    {
        [Theory]
        [InlineData(1, 20, 1, 1, 0, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(10, 30, 5, 1, 0, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(50, 40, 20, 2, 2, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(1, 20, 1, 1, 0, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
        InlineData(10, 30, 5, 1, 0, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
        InlineData(50, 40, 20, 2, 2, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),

            InlineData(1, 20, 1, 1, 0, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(10, 30, 5, 1, 0, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(50, 40, 20, 2, 2, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
        InlineData(1, 20, 1, 1, 0, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
        InlineData(10, 30, 5, 1, 0, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
        InlineData(50, 40, 20, 2, 2, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled)]
        public void Run(int messageCount, int timeOut, int workerCount, 
            int readerCount, int queueSize, ConnectionInfoTypes type, LinqMethodTypes linqMethodTypes)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    //create data
                    var id = Guid.NewGuid();
                    var producer = new ProducerMethodShared();
                    if (linqMethodTypes == LinqMethodTypes.Compiled)
                    {
                        producer.RunTestCompiled<RedisQueueInit>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateNoOpCompiled, 0);
                    }
                    else
                    {
                        producer.RunTestDynamic<RedisQueueInit>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateNoOpDynamic, 0);
                    }

                    //process data
                    var consumer = new ConsumerMethodAsyncPoisonMessageShared();
                    consumer.RunConsumer<RedisQueueInit>(queueName, connectionString, false,
                        workerCount, logProvider,
                        timeOut, readerCount, queueSize, messageCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12));

                    ValidateErrorCounts(queueName, connectionString, messageCount);
                    using (
                        var count = new VerifyQueueRecordCount(queueName, connectionString))
                    {
                        count.Verify(messageCount, true, 2);
                    }

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
        private void ValidateErrorCounts(string queueName, string connectionString, long messageCount)
        {
            //poison messages are moved to the error queue right away
            //they don't update the tracking table, so specify 0 for the error count.
            //They still update the error table itself
            using (var error = new VerifyErrorCounts(queueName, connectionString))
            {
                error.Verify(messageCount, 0);
            }
        }
    }
}
