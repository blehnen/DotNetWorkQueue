using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.JobScheduler
{
    [CollectionDefinition("JobScheduler", DisableParallelization = true)]
    public class JobSchedulerTests
    {
        [Theory]
#if NETFULL

#else

#endif
        [InlineData(true, false),
         InlineData(true, true)]
        public void Run(
            bool interceptors,
            bool dynamic)
        {
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerTests();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation, PostgreSqlMessageQueueCreation>(
                new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString), interceptors, dynamic, Helpers.Verify, Helpers.SetError);
        }
    }
}
