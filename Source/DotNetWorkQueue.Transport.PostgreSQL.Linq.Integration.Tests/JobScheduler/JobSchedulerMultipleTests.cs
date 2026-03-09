using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.JobScheduler
{
    [TestClass]
    public class JobSchedulerMultipleTests
    {
        [TestMethod]
        [DataRow(2)]
        public void Run(
            int producerCount)
        {
            if (OsDetectionHelper.IsRunningOnServer(null))
            {
                Assert.Inconclusive("Test skipped on server");
                return;
            }
            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerMultipleTests();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation, PostgreSqlMessageQueueCreation>(new QueueConnection(queueName,
                ConnectionInfo.ConnectionString), producerCount);
        }
    }
}
