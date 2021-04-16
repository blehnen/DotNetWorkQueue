using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.ConsumerMethod
{
    [Collection("Consumer")]
    public class SimpleMethodConsumer
    {
        [Theory]
#if NETFULL
        [InlineData(100, 0, 240, 5, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic),
        InlineData(50, 5, 200, 10, ConnectionInfoTypes.Linux, LinqMethodTypes.Dynamic)]
#else
        [InlineData(10, 5, 180, 7, ConnectionInfoTypes.Linux, LinqMethodTypes.Compiled)]
#endif
        public void Run(int messageCount, int runtime, int timeOut, int workerCount, ConnectionInfoTypes type, LinqMethodTypes linqMethodTypes)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod.Implementation.
                    SimpleMethodConsumer();

            consumer.Run<RedisQueueInit, RedisQueueCreation>(queueName,
                connectionString,
                messageCount, runtime, timeOut, workerCount, linqMethodTypes, false, x => { },
                Helpers.GenerateData, Helpers.Verify, VerifyQueueCount);
        }

        private void VerifyQueueCount(string arg1, string arg2, IBaseTransportOptions arg3, ICreationScope arg4, int arg5, bool arg6, bool arg7)
        {
            using (var count = new VerifyQueueRecordCount(arg1, arg2))
            {
                count.Verify(0, false, -1);
            }
        }
    }
}
