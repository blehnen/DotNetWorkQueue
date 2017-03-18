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
using DotNetWorkQueue.IntegrationTests.Shared.Consumer;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using System;
using Xunit;
namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Consumer
{
    [Collection("Redis")]
    public class ConsumerRollBack
    {

        [Theory]
        [InlineData(500, 0, 240, 5, ConnectionInfoTypes.Linux, false),
        InlineData(50, 5, 200, 10, ConnectionInfoTypes.Linux, false),
        InlineData(10, 15, 180, 7, ConnectionInfoTypes.Linux, false),
          InlineData(500, 0, 240, 5, ConnectionInfoTypes.Windows, false),
        InlineData(50, 5, 200, 10, ConnectionInfoTypes.Windows, false),
        InlineData(10, 15, 180, 7, ConnectionInfoTypes.Windows, false),
            InlineData(500, 0, 240, 5, ConnectionInfoTypes.Linux, true),
        InlineData(50, 5, 200, 10, ConnectionInfoTypes.Linux, true),
        InlineData(10, 15, 180, 7, ConnectionInfoTypes.Linux, true),
          InlineData(500, 0, 240, 5, ConnectionInfoTypes.Windows, true),
        InlineData(50, 5, 200, 10, ConnectionInfoTypes.Windows, true),
        InlineData(10, 15, 180, 7, ConnectionInfoTypes.Windows, true)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, ConnectionInfoTypes type, bool route)
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

                    if (route)
                    {
                        var producer = new ProducerShared();
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateRouteData,
                            Helpers.Verify, false);
                    }
                    else
                    {
                        var producer = new ProducerShared();
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false);
                    }

                    //process data
                    var defaultRoute = route ? Helpers.DefaultRoute : null;
                    var consumer = new ConsumerRollBackShared<FakeMessage>();
                    consumer.RunConsumer<RedisQueueInit>(queueName, connectionString, false,
                        workerCount, logProvider, timeOut, runtime, messageCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), defaultRoute);

                    using (var count = new VerifyQueueRecordCount(queueName, connectionString))
                    {
                        count.Verify(0, false, -1);
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
    }
}
