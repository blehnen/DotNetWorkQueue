using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.JobScheduler
{
    [CollectionDefinition("JobScheduler", DisableParallelization = true)]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(true, 2)]
        public void Run(
            bool interceptors,
            int producerCount)
        {
            var queueName = GenerateQueueName.Create();
            using (var queueContainer = new QueueContainer<PostgreSqlMessageQueueInit>(x => {
            }))
            {
                var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
                try
                {
                    var tests = new JobSchedulerTestsShared();
                    tests.RunTestMultipleProducers<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation>(queueConnection, interceptors, producerCount, queueContainer.CreateTimeSync(ConnectionInfo.ConnectionString), LoggerShared.Create(queueName, GetType().Name));
                }
                finally
                {

                    using (var queueCreator =
                        new QueueCreationContainer<PostgreSqlMessageQueueInit>())
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection)
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
