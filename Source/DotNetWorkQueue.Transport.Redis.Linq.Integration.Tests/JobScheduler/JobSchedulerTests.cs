using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.JobScheduler
{
    [CollectionDefinition("JobScheduler", DisableParallelization = true)]
    public class JobSchedulerTests
    {
        [Theory]
#if NETFULL
        [InlineData(true, ConnectionInfoTypes.Linux)]
#else
        [InlineData(false, ConnectionInfoTypes.Linux)]
#endif
        public void Run(
            bool dynamic,
            ConnectionInfoTypes type)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerTests();

            consumer.Run<RedisQueueInit, RedisJobQueueCreation, RedisQueueCreation>(
                new QueueConnection(queueName, connectionString), false, dynamic, Helpers.Verify, Helpers.SetError);
        }
    }
}
