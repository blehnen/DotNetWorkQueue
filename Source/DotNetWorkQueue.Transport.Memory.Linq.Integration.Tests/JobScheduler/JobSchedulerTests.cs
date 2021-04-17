using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.JobScheduler
{
    [CollectionDefinition("JobScheduler", DisableParallelization = true)]
    public class JobSchedulerTests
    {
        [Theory]
#if NETFULL
        [InlineData(true)]
#else
        [InlineData(false)]
#endif
        public void Run(
            bool dynamic)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var consumer =
                    new DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation.JobSchedulerTests();

                consumer.Run<MemoryMessageQueueInit, JobQueueCreation, MessageQueueCreation>(new QueueConnection(queueName,
                    connectionInfo.ConnectionString), false, dynamic, Helpers.Verify, Helpers.SetError);
            }
        }
    }
}
