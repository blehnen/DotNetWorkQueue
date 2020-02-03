using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.JobScheduler
{
    [Collection("jobscheduler")]
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
                using (var queueCreator =
                    new QueueCreationContainer<MemoryMessageQueueInit>())
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                connectionInfo.ConnectionString)
                    )
                    {
                        using (var queueContainer = new QueueContainer<MemoryMessageQueueInit>(x => { }))
                        {
                            try
                            {
                                var tests = new JobSchedulerTestsShared();
                                if (!dynamic)
                                {
                                    tests.RunEnqueueTestCompiled<MemoryMessageQueueInit, JobQueueCreation>(
                                        queueName,
                                        connectionInfo.ConnectionString, true,
                                        Helpers.Verify, Helpers.SetError,
                                        queueContainer.CreateTimeSync(connectionInfo.ConnectionString),
                                            oCreation.Scope, LoggerShared.Create(queueName, GetType().Name));
                                }
#if NETFULL
                                else
                                {
                                    tests.RunEnqueueTestDynamic<MemoryMessageQueueInit, JobQueueCreation>(
                                        queueName,
                                        connectionInfo.ConnectionString, true,
                                        Helpers.Verify, Helpers.SetError,
                                        queueContainer.CreateTimeSync(connectionInfo.ConnectionString),
                                            oCreation.Scope, LoggerShared.Create(queueName, GetType().Name));
                                }
#endif
                            }
                            finally
                            {
                                oCreation.RemoveQueue();
                            }
                        }
                    }
                }
            }
        }
    }
}
