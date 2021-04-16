using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.JobScheduler
{
    [CollectionDefinition("JobScheduler", DisableParallelization = true)]
    public class JobSchedulerTests
    {
        [Theory]
        [InlineData(false, false),
        InlineData(true, false),
        InlineData(false, true)]
        public void Run(
            bool dynamic,
            bool inMemoryDb)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerTests();
                consumer.Run<SqLiteMessageQueueInit, SqliteJobQueueCreation, SqLiteMessageQueueCreation>(
                    queueName,
                    connectionInfo.ConnectionString, false, dynamic, Helpers.Verify, Helpers.SetError);
            }
        }
    }
}
