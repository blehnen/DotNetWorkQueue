using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.JobScheduler
{
    [CollectionDefinition("JobScheduler", DisableParallelization = true)]
    public class JobSchedulerMultipleTests
    {
        [Theory(Skip = "Cannot get the timing right on the CI server, its too slow. These work locally, remove the skip and run locally to test")]
        [InlineData(2)]
        public void Run(
            int producerCount)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerMultipleTests();
            consumer.Run<SqlServerMessageQueueInit, SqlServerJobQueueCreation, SqlServerMessageQueueCreation>(
                new QueueConnection(queueName, ConnectionInfo.ConnectionString), producerCount);
        }
    }
}
