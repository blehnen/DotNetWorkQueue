using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.JobScheduler
{
    [Collection("Redis")]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(10, ConnectionInfoTypes.Linux),
      InlineData(10, ConnectionInfoTypes.Windows)]
        public void RunMultiple(
           int producerCount,
           ConnectionInfoTypes type)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (var queueContainer = new QueueContainer<RedisQueueInit>(x => {
            }))
            {
                try
                {
                    var tests = new JobSchedulerTestsShared();
                    tests.RunTestMultipleProducers<RedisQueueInit, RedisJobQueueCreation>(queueName,
                        connectionString, true, producerCount, queueContainer.CreateTimeSync(connectionString), LoggerShared.Create(queueName, GetType().Name));
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
