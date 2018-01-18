using System;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Redis")]
    public class SimpleMethodProducer
    {
        [Theory]
        [InlineData(100, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
#if NETFULL
        InlineData(100, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(500, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(500, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, true, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, false, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, true, false, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, false, false, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, true, false, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, true, true, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, false, true, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
         InlineData(100, true, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(100, false, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(500, true, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(500, false, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(100, true, true, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(100, false, true, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(100, true, false, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(100, false, false, false, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(100, true, false, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(100, true, true, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
         InlineData(100, false, true, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Dynamic),
#endif
         InlineData(100, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(500, true, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(500, false, false, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, true, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, false, true, false, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, true, false, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, false, false, false, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, true, false, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, true, true, true, false, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, false, true, true, true, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled),
         InlineData(100, true, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(100, false, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(500, true, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(500, false, false, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(100, true, true, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(100, false, true, false, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(100, true, false, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(100, false, false, false, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(100, true, false, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(100, true, true, true, false, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled),
         InlineData(100, false, true, true, true, ConnectionInfoTypes.Windows, LinqMethodTypes.Compiled)]
        public void Run(
            int messageCount,
            bool interceptors,
            bool batchSending,
            bool enableDelay,
            bool enableExpiration,
            ConnectionInfoTypes type,
             LinqMethodTypes linqMethodTypes)
        {

            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            var producer = new ProducerMethodShared();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (
                var queueCreator =
                    new QueueCreationContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {
                    var id = Guid.NewGuid();
                    if (enableExpiration && enableDelay)
                    {
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<RedisQueueInit>(queueName,
                                connectionString, interceptors, messageCount, logProvider,
                                Helpers.GenerateDelayExpiredData,
                                Helpers.Verify, batchSending, id, GenerateMethod.CreateCompiled, 0, null);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<RedisQueueInit>(queueName,
                               connectionString, interceptors, messageCount, logProvider,
                               Helpers.GenerateDelayExpiredData,
                               Helpers.Verify, batchSending, id, GenerateMethod.CreateDynamic, 0, null);
                        }
#endif
                    }
                    else if (enableDelay)
                    {
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<RedisQueueInit>(queueName,
                                connectionString, interceptors, messageCount, logProvider, Helpers.GenerateDelayData,
                                Helpers.Verify, batchSending, id, GenerateMethod.CreateCompiled, 0, null);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<RedisQueueInit>(queueName,
                               connectionString, interceptors, messageCount, logProvider, Helpers.GenerateDelayData,
                               Helpers.Verify, batchSending, id, GenerateMethod.CreateDynamic, 0, null);
                        }
#endif
                    }
                    else if (enableExpiration)
                    {
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<RedisQueueInit>(queueName,
                                connectionString, interceptors, messageCount, logProvider, Helpers.GenerateExpiredData,
                                Helpers.Verify, batchSending, id, GenerateMethod.CreateCompiled, 0, null);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<RedisQueueInit>(queueName,
                               connectionString, interceptors, messageCount, logProvider, Helpers.GenerateExpiredData,
                               Helpers.Verify, batchSending, id, GenerateMethod.CreateDynamic, 0, null);
                        }
#endif
                    }
                    else
                    {
                        if (linqMethodTypes == LinqMethodTypes.Compiled)
                        {
                            producer.RunTestCompiled<RedisQueueInit>(queueName,
                                connectionString, interceptors, messageCount, logProvider, Helpers.GenerateData,
                                Helpers.Verify, batchSending, id, GenerateMethod.CreateCompiled, 0, null);
                        }
#if NETFULL
                        else
                        {
                            producer.RunTestDynamic<RedisQueueInit>(queueName,
                               connectionString, interceptors, messageCount, logProvider, Helpers.GenerateData,
                               Helpers.Verify, batchSending, id, GenerateMethod.CreateDynamic, 0, null);
                        }
#endif
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
