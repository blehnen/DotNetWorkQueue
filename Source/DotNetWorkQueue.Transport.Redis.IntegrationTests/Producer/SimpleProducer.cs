// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using Xunit.Sdk;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Producer
{
    [Collection("RedisProducerTests")]
    public class SimpleProducer
    {
        [Theory]
        [InlineData(1000, true, false, false, false),
         InlineData(1000, false, false, false, false),
         InlineData(10000, true, false, false, false),
         InlineData(10000, false, false, false, false),
         InlineData(1000, true, true, false, false),
         InlineData(1000, false, true, false, false),
         InlineData(1000, true, false, true, false),
         InlineData(1000, false, false, false, true),
         InlineData(1000, true, false, true, true),
         InlineData(1000, false, false, false, true),
         InlineData(1000, true, true, true, false),
         InlineData(1000, false, true, true, true)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool batchSending,
            bool enableDelay,
            bool enableExpiration)
        {

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName);
            var producer = new ProducerShared();
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    if (enableExpiration && enableDelay)
                    {
                       producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                       ConnectionInfo.ConnectionString, interceptors, messageCount, logProvider, Helpers.GenerateDelayExpiredData,
                       Helpers.Verify, batchSending);
                    }
                    else if (enableDelay)
                    {
                       producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                       ConnectionInfo.ConnectionString, interceptors, messageCount, logProvider, Helpers.GenerateDelayData,
                       Helpers.Verify, batchSending);
                    }
                    else if (enableExpiration)
                    {
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                       ConnectionInfo.ConnectionString, interceptors, messageCount, logProvider, Helpers.GenerateExpiredData,
                       Helpers.Verify, batchSending);
                    }
                    else
                    {
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                        ConnectionInfo.ConnectionString, interceptors, messageCount, logProvider, Helpers.GenerateData,
                        Helpers.Verify, batchSending);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<RedisQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }
    }
}
