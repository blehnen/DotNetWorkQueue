using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.JobScheduler
{
    [CollectionDefinition("JobScheduler", DisableParallelization = true)]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(2, ConnectionInfoTypes.Linux)]
        public void RunMultiple(
           int producerCount,
           ConnectionInfoTypes type)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = new ConnectionInfo(type).ConnectionString;
            using (var queueContainer = new QueueContainer<RedisQueueInit>(x => {
            }))
            {
                var queueConnection = new QueueConnection(queueName, connectionString);
                try
                {
                    var tests = new JobSchedulerTestsShared();
                    tests.RunTestMultipleProducers<RedisQueueInit, RedisJobQueueCreation>(queueConnection, true, producerCount, queueContainer.CreateTimeSync(connectionString), LoggerShared.Create(queueName, GetType().Name));
                }
                finally
                {

                    using (var queueCreator =
                        new QueueCreationContainer<RedisQueueInit>())
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
        }
    }
}
