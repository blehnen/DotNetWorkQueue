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
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using System;
using Xunit;
namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.ConsumerAsync
{
    [Collection("Redis Consumer Tests")]
    public class ConsumerAsyncRollBack
    {
        [Theory]
        [InlineData(100, 1, 400, 5, 5, 5),
         InlineData(50, 5, 200, 5, 1, 3),
         InlineData(10, 5, 180, 7, 1, 1)]
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, int readerCount, int queueSize)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    //create data
                    var producer = new ProducerShared();
                    producer.RunTest<RedisQueueInit, FakeMessage>(queueName,
                        ConnectionInfo.ConnectionString, false, messageCount, logProvider, Helpers.GenerateData,
                        Helpers.Verify, false);

                    //process data
                    var consumer = new ConsumerAsyncRollBackShared<FakeMessage>();
                    consumer.RunConsumer<RedisQueueInit>(queueName, ConnectionInfo.ConnectionString, false,
                        workerCount, logProvider,
                        timeOut, readerCount, queueSize, runtime, messageCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12));
                    LoggerShared.CheckForErrors(queueName);
                    new VerifyQueueRecordCount(queueName).Verify(0, false);

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
