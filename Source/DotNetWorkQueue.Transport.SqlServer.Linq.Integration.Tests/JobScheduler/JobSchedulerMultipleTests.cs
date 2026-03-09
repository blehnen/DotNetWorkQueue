using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.JobScheduler
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
            consumer.Run<SqlServerMessageQueueInit, SqlServerJobQueueCreation, SqlServerMessageQueueCreation>(
                new QueueConnection(queueName, ConnectionInfo.ConnectionString), producerCount);
        }
    }
}
