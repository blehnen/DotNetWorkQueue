using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.JobScheduler
{
    [TestClass]
    public class JobSchedulerTests
    {
        [TestMethod]
        [DataRow(false),
        DataRow(true)]
        public void Run(
            bool inMemoryDb)
        {
            if (OsDetectionHelper.IsRunningOnServer(null))
            {
                Assert.Inconclusive("Test skipped on server");
                return;
            }
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerTests();
                consumer.Run<SqLiteMessageQueueInit, SqliteJobQueueCreation, SqLiteMessageQueueCreation>(
                    new QueueConnection(queueName, connectionInfo.ConnectionString), false, Helpers.Verify, Helpers.SetError);
            }
        }
    }
}
