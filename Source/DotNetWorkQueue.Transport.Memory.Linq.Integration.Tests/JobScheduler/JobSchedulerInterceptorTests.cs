using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.JobScheduler
{
    [TestClass]
    public class JobSchedulerInterceptorTests
    {
        [TestMethod]
        public void Run()
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerTests();

                consumer.Run<MemoryMessageQueueInit, JobQueueCreation, MessageQueueCreation>(new QueueConnection(queueName,
                    connectionInfo.ConnectionString), true, Helpers.Verify, Helpers.SetError);
            }
        }
    }
}
