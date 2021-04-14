﻿using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.ConsumerAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerAsyncErrorTable
    {
        [Theory]
        [InlineData(1, 60, 1, 1, 0, ConnectionInfoTypes.Linux, false),
        InlineData(1, 60, 1, 1, 0, ConnectionInfoTypes.Linux, true)]
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize, ConnectionInfoTypes type, bool route)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new QueueConnection(queueName, connectionString);
                try
                {
                    //create data
                    if (route)
                    {
                        var producer = new ProducerShared();
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueConnection, false, messageCount, logProvider, Helpers.GenerateRouteData,
                            Helpers.Verify, false, new CreationScopeNoOp(), false);
                    }
                    else
                    {
                        var producer = new ProducerShared();
                        producer.RunTest<RedisQueueInit, FakeMessage>(queueConnection, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, new CreationScopeNoOp(), false);
                    }

                    //process data
                    var defaultRoute = route ? Helpers.DefaultRoute : null;
                    var consumer = new ConsumerAsyncErrorShared<FakeMessage>();
                    consumer.RunConsumer<RedisQueueInit>(queueConnection, false,
                        logProvider,
                        messageCount, workerCount, timeOut, queueSize, readerCount, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), "second(*%3)", defaultRoute, false, new CreationScopeNoOp());
                    ValidateErrorCounts(queueName, messageCount, connectionString);
                    using (
                        var count = new VerifyQueueRecordCount(queueName, connectionString))
                    {
                        count.Verify(messageCount, false, 2);
                    }

                    consumer.PurgeErrorMessages<RedisQueueInit>(queueConnection,
                        false, logProvider, false, new CreationScopeNoOp());
                    ValidateErrorCounts(queueName, messageCount, connectionString);

                    //purge error messages and verify that count is 0
                    consumer.PurgeErrorMessages<RedisQueueInit>(queueConnection,
                        false, logProvider, true, new CreationScopeNoOp());
                    ValidateErrorCounts(queueName, 0, connectionString);

                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<RedisQueueCreation>(queueConnection)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }

        private void ValidateErrorCounts(string queueName, int messageCount, string connectionString)
        {
            using (var error = new VerifyErrorCounts(queueName, connectionString))
            {
                error.Verify(messageCount, 2);
            }
        }
    }
}
