using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.JobScheduler
{
    [Collection("JobScheduler")]
    public class JobSchedulerTests
    {
        [Theory]
        [InlineData(false),
        InlineData(true)]
        public void Run(
            bool dynamic)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                using (var queueCreator =
                    new QueueCreationContainer<LiteDbMessageQueueInit>())
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection)
                    )
                    {
                        using (var queueContainer = new QueueContainer<LiteDbMessageQueueInit>(x => { }))
                        {
                            try
                            {
                                var tests = new JobSchedulerTestsShared();
                                if (!dynamic)
                                {
                                    tests.RunEnqueueTestCompiled<LiteDbMessageQueueInit, LiteDbJobQueueCreation>(
                                        queueConnection, true,
                                        Helpers.Verify, Helpers.SetError,
                                        queueContainer.CreateTimeSync(connectionInfo.ConnectionString),
                                            oCreation.Scope, LoggerShared.Create(queueName, GetType().Name));
                                }
                                else
                                {
                                    tests.RunEnqueueTestDynamic<LiteDbMessageQueueInit, LiteDbJobQueueCreation>(
                                        queueConnection, true,
                                        Helpers.Verify, Helpers.SetError,
                                        queueContainer.CreateTimeSync(connectionInfo.ConnectionString),
                                            oCreation.Scope, LoggerShared.Create(queueName, GetType().Name));
                                }
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
