using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.JobScheduler
{
    [Collection("JobScheduler")]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(true, 2)]
        public void Run(
            bool interceptors,
            int producerCount)
        {
            var queueName = GenerateQueueName.Create();
            using (var queueContainer = new QueueContainer<SqlServerMessageQueueInit>(x => {
            }))
            {
                var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, ConnectionInfo.ConnectionString);
                try
                {
                    var tests = new JobSchedulerTestsShared();
                    tests.RunTestMultipleProducers<SqlServerMessageQueueInit, SqlServerJobQueueCreation>(queueConnection, interceptors, producerCount, queueContainer.CreateTimeSync(ConnectionInfo.ConnectionString), LoggerShared.Create(queueName, GetType().Name));
                }
                finally
                {

                    using (var queueCreator =
                        new QueueCreationContainer<SqlServerMessageQueueInit>())
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection)
                            )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }
    }
}
