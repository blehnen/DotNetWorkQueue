using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.JobScheduler
{
    [TestClass]
    public class JobSchedulerTests
    {
        [TestMethod]
        [DataRow(false),
        DataRow(true)]
        public void Run(
            bool dynamic)
        {
            if (OsDetectionHelper.IsRunningOnServer(null))
            {
                Assert.Inconclusive("Test skipped on server");
                return;
            }
            using (var connectionInfo = new IntegrationConnectionInfo(IntegrationConnectionInfo.ConnectionTypes.Direct))
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerTests();

                consumer.Run<LiteDbMessageQueueInit, LiteDbJobQueueCreation, LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                    connectionInfo.ConnectionString), false, dynamic, Helpers.Verify, Helpers.SetError);
            }
        }
    }
}
