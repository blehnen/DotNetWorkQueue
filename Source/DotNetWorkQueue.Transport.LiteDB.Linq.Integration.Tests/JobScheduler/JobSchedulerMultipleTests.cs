using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.JobScheduler
{
    [CollectionDefinition("JobScheduler", DisableParallelization = true)]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(2)]
        public void Run(
            int producerCount)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(IntegrationConnectionInfo.ConnectionTypes.Direct))
            {
                using (var queueCreator =
                    new QueueCreationContainer<LiteDbMessageQueueInit>())
                {
                    var queueName = GenerateQueueName.Create();
                    var queueConnection =
                        new DotNetWorkQueue.Configuration.QueueConnection(queueName,
                            connectionInfo.ConnectionString);
                    var oCreation = queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection);
                    var scope = oCreation.Scope;
                    using (var queueContainer = new QueueContainer<LiteDbMessageQueueInit>(x => x.RegisterNonScopedSingleton(scope)))
                    {
                        try
                        {
                            var tests = new JobSchedulerTestsShared();
                            tests.RunTestMultipleProducers<LiteDbMessageQueueInit, LiteDbJobQueueCreation>(
                                queueConnection, true, producerCount,
                                queueContainer.CreateTimeSync(connectionInfo.ConnectionString),
                                LoggerShared.Create(queueName, GetType().Name), scope);
                        }
                        finally
                        {
                            oCreation.RemoveQueue();
                            oCreation.Dispose();
                            scope?.Dispose();
                        }
                    }
                }
            }
        }
    }
}
