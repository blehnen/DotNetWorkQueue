using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.JobScheduler
{
    [CollectionDefinition("JobScheduler", DisableParallelization = true)]
    public class JobSchedulerTests
    {
        [Theory]
#if NETFULL
        [InlineData(true, false),
         InlineData(true, true)]
#else
        [InlineData(true, false)]
#endif
        public void Run(
            bool interceptors,
            bool dynamic)
        {

            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerTests();
            consumer.Run<SqlServerMessageQueueInit, SqlServerJobQueueCreation, SqlServerMessageQueueCreation>(
                new QueueConnection(queueName, ConnectionInfo.ConnectionString), interceptors, dynamic, Helpers.Verify, Helpers.SetError);
        }
    }
}
