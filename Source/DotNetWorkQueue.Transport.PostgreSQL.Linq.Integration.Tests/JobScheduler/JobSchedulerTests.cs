using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.JobScheduler
{
    [TestClass]
    public class JobSchedulerTests
    {
        [TestMethod]
#if NETFULL

#else

#endif
        [DataRow(true, false),
         DataRow(true, true)]
        public void Run(
            bool interceptors,
            bool dynamic)
        {
            if (OsDetectionHelper.IsRunningOnServer(null))
            {
                Assert.Inconclusive("Test skipped on server");
                return;
            }

            var queueName = GenerateQueueName.Create();
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerTests();

            consumer.Run<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation, PostgreSqlMessageQueueCreation>(
                new QueueConnection(queueName,
                    ConnectionInfo.ConnectionString), interceptors, dynamic, Helpers.Verify, Helpers.SetError);
        }
    }
}
