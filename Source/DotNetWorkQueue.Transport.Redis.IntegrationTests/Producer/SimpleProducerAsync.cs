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
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Producer
{
    [Collection("Redis")]
    public class SimpleProducerAsync
    {
        [Theory]
        [InlineData(1000, true, false, ConnectionInfoTypes.Linux),
        InlineData(1000, false, false, ConnectionInfoTypes.Linux),
        InlineData(10000, true, false, ConnectionInfoTypes.Linux),
        InlineData(10000, false, false, ConnectionInfoTypes.Linux),
        InlineData(1000, true, true, ConnectionInfoTypes.Linux),
        InlineData(1000, false, true, ConnectionInfoTypes.Linux),
            InlineData(1000, true, false, ConnectionInfoTypes.Windows),
        InlineData(1000, false, false, ConnectionInfoTypes.Windows),
        InlineData(10000, true, false, ConnectionInfoTypes.Windows),
        InlineData(10000, false, false, ConnectionInfoTypes.Windows),
        InlineData(1000, true, true, ConnectionInfoTypes.Windows),
        InlineData(1000, false, true, ConnectionInfoTypes.Windows)]
        public async void Run(
           int messageCount,
           bool interceptors,
           bool batchSending,
           ConnectionInfoTypes type)
        {

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var producer = new ProducerAsyncShared();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    await producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                       connectionString, interceptors, messageCount, logProvider, Helpers.GenerateData,
                        Helpers.Verify, batchSending);
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
