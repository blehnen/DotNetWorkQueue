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
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;
namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.ConsumerAsync
{
    [Collection("Redis Consumer Tests")]
    public class ConsumerAsyncErrorTable
    {
        [Theory]
        [InlineData(1, 30, 1, 1, 0)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize)
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
                    var consumer = new ConsumerAsyncErrorShared<FakeMessage>();
                    consumer.RunConsumer<RedisQueueInit>(queueName, ConnectionInfo.ConnectionString, false,
                        logProvider,
                        messageCount, workerCount, timeOut, queueSize, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12));
                    ValidateErrorCounts(queueName, messageCount);
                    new VerifyQueueRecordCount(queueName).Verify(messageCount, false);

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

        private void ValidateErrorCounts(string queueName, int messageCount)
        {
            new VerifyErrorCounts(queueName).Verify(messageCount, 2);
        }
    }
}
