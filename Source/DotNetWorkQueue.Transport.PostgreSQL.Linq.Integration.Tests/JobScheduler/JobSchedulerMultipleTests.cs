using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.JobScheduler
{
    [Collection("PostgreSQL")]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(true, 10)]
        public void Run(
            bool interceptors,
            int producerCount)
        {
            var queueName = GenerateQueueName.Create();
            using (var queueContainer = new QueueContainer<PostgreSqlMessageQueueInit>(x => {
            }))
            {
                try
                {
                    var tests = new JobSchedulerTestsShared();
                    tests.RunTestMultipleProducers<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation>(queueName,
                        ConnectionInfo.ConnectionString, interceptors, producerCount, queueContainer.CreateTimeSync(ConnectionInfo.ConnectionString), LoggerShared.Create(queueName, GetType().Name));
                }
                finally
                {

                    using (var queueCreator =
                        new QueueCreationContainer<PostgreSqlMessageQueueInit>())
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
                                    ConnectionInfo.ConnectionString)
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
