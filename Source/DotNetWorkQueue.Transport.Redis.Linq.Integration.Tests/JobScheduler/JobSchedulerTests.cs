using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.JobScheduler
{
    [Collection("Redis")]
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
            using (var queueContainer = new QueueContainer<RedisQueueInit>(x => { }))
            {
                try
                {
                    var tests = new JobSchedulerTestsShared();
                    if (!dynamic)
                    {
                        tests.RunEnqueueTestCompiled<RedisQueueInit, RedisJobQueueCreation>(queueName,
                            connectionString, true,
                            Helpers.Verify, Helpers.SetError, queueContainer.CreateTimeSync(connectionString), null, LoggerShared.Create(queueName, GetType().Name));
                    }
#if NETFULL
                    else
                    {
                        tests.RunEnqueueTestDynamic<RedisQueueInit, RedisJobQueueCreation>(queueName,
                            connectionString, true,
                            Helpers.Verify, Helpers.SetError, queueContainer.CreateTimeSync(connectionString), null, LoggerShared.Create(queueName, GetType().Name));
                    }
#endif
                }
                finally
                {

                    using (var queueCreator =
                        new QueueCreationContainer<RedisQueueInit>())
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
}
