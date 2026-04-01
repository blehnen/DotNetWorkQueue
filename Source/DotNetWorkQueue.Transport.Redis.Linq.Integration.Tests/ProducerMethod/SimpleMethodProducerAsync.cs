using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ProducerMethod
{
    [TestClass]
    public class SimpleMethodProducerAsync
    {
        [TestMethod]
#if NETFULL
        [DataRow(100, true, false, LinqMethodTypes.Compiled),
#if NETFULL
        DataRow(100, true, false, LinqMethodTypes.Dynamic),
        DataRow(100, false, false, LinqMethodTypes.Dynamic),
        DataRow(100, false, true, LinqMethodTypes.Dynamic),
#endif
        DataRow(100, false, false, LinqMethodTypes.Compiled),
        DataRow(100, true, true, LinqMethodTypes.Compiled),
        DataRow(100, false, true, LinqMethodTypes.Compiled)]
#else
        [DataRow(100, true, false, LinqMethodTypes.Compiled),
         DataRow(100, false, false, LinqMethodTypes.Compiled),
         DataRow(100, true, true, LinqMethodTypes.Compiled),
         DataRow(100, false, true, LinqMethodTypes.Compiled)]
#endif
        public async Task Run(
           int messageCount,
           bool interceptors,
           bool batchSending,
           LinqMethodTypes linqMethodTypes)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod.Implementation.SimpleMethodProducerAsync();

            await consumer.Run<RedisQueueInit, RedisQueueCreation>(new QueueConnection(queueName, connectionString),
                messageCount, linqMethodTypes, interceptors, false, batchSending, x => { },
                Helpers.GenerateData, Helpers.Verify);
        }
    }
}
