using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.JobScheduler
{
    [Collection("SqlServer")]
    public class JobSchedulerMultipleTests
    {
        [Theory]
        [InlineData(true, 3)]
        public void Run(
            bool interceptors,
            int producerCount)
        {
            var queueName = GenerateQueueName.Create();
            using (var queueContainer = new QueueContainer<SqlServerMessageQueueInit>(x => {
            }))
            {
                try
                {
                    var tests = new JobSchedulerTestsShared();
                    tests.RunTestMultipleProducers<SqlServerMessageQueueInit, SqlServerJobQueueCreation>(queueName,
                        ConnectionInfo.ConnectionString, interceptors, producerCount, queueContainer.CreateTimeSync(ConnectionInfo.ConnectionString), LoggerShared.Create(queueName, GetType().Name));
                }
                finally
                {

                    using (var queueCreator =
                        new QueueCreationContainer<SqlServerMessageQueueInit>())
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueName,
                                    ConnectionInfo.ConnectionString)
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
