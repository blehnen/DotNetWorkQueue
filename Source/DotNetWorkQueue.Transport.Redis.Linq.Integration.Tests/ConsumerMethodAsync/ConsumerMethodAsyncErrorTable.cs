using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethodAsync;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ConsumerMethodAsync
{
    [Collection("ConsumerAsync")]
    public class ConsumerMethodAsyncErrorTable
    {
        [Theory]
#if NETFULL
         [InlineData(1, 30, 1, 1, 0, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(1, 30, 1, 1, 0, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled)]
#else
        [InlineData(1, 30, 1, 1, 0, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled)]
#endif
        public void Run(int messageCount, int timeOut, int workerCount, int readerCount, int queueSize, ConnectionInfoTypes type, LinqMethodTypes linqMethodTypes)
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
                            Helpers.Verify, false, id, GenerateMethod.CreateErrorCompiled, 0, null, false);
                    }
#if NETFULL
                    else
                    {
                        producer.RunTestDynamic<RedisQueueInit>(queueName,
                            connectionString, false, messageCount, logProvider, Helpers.GenerateData,
                            Helpers.Verify, false, id, GenerateMethod.CreateErrorDynamic, 0, null, false);
                    }
#endif
                    //process data
                    var consumer = new ConsumerMethodAsyncErrorShared();
                    consumer.RunConsumer<RedisQueueInit>(queueName, connectionString, false,
                        logProvider,
                        messageCount, workerCount, timeOut, queueSize, readerCount, TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(12), id, "second(*%3)", false);
                    ValidateErrorCounts(queueName, messageCount, connectionString);
                    using (
                        var count = new VerifyQueueRecordCount(queueName, connectionString))
                    {
                        count.Verify(messageCount, false, 2);
                    }

                    consumer.PurgeErrorMessages<RedisQueueInit>(queueName, connectionString,
                        false, logProvider, false);
                    ValidateErrorCounts(queueName, messageCount, connectionString);

                    //purge error messages and verify that count is 0
                    consumer.PurgeErrorMessages<RedisQueueInit>(queueName, connectionString,
                        false, logProvider, true);
                    ValidateErrorCounts(queueName, 0, connectionString);

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

        private void ValidateErrorCounts(string queueName, int messageCount, string connectionString)
        {
            using (var error = new VerifyErrorCounts(queueName, connectionString))
            {
                error.Verify(messageCount, 2);
            }
        }
    }
}
