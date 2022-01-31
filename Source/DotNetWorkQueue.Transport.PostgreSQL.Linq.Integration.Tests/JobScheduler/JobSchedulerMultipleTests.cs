using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.JobScheduler
{
    [CollectionDefinition("JobScheduler", DisableParallelization = true)]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(2)]
        public void Run(
            int producerCount)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerMultipleTests();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                ConnectionInfo.ConnectionString), producerCount);
        }
    }
}
