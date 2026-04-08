using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.IntegrationTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.JobScheduler
{
    [TestClass]
    public class JobSchedulerTests
    {
        [TestMethod]
        [DataRow(false)]
        public void Run(
            bool dynamic)
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var consumer =
                new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerTests();

            consumer.Run<RedisQueueInit, RedisJobQueueCreation, RedisQueueCreation>(
                new QueueConnection(queueName, connectionString), false, dynamic, Helpers.Verify, Helpers.SetError);
        }
    }
}
