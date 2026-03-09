using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.JobScheduler
{
    [TestClass]
    public class JobSchedulerTests
    {
        [TestMethod]
#if NETFULL
        [DataRow(true, false),
         DataRow(true, true)]
#else
        [DataRow(true, false)]
#endif
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
            consumer.Run<SqlServerMessageQueueInit, SqlServerJobQueueCreation, SqlServerMessageQueueCreation>(
                new QueueConnection(queueName, ConnectionInfo.ConnectionString), interceptors, dynamic, Helpers.Verify, Helpers.SetError);
        }
    }
}
