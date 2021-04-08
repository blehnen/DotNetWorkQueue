using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.JobScheduler
{
    [CollectionDefinition("JobScheduler", DisableParallelization = true)]
    public class JobSchedulerTests
    {
        [Theory]
#if NETFULL
        [InlineData(true, false),
         InlineData(true, true)]
#else
        [InlineData(true, false)]
#endif
        public void Run(
            bool interceptors,
            bool dynamic)
        {
            var queueName = GenerateQueueName.Create();
            using (var queueContainer = new QueueContainer<SqlServerMessageQueueInit>(x => {
            }))
            {
                var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, ConnectionInfo.ConnectionString);
                try
                {
                    var tests = new JobSchedulerTestsShared();
                    if (!dynamic)
                    {
                        tests.RunEnqueueTestCompiled<SqlServerMessageQueueInit, SqlServerJobQueueCreation>(queueConnection, interceptors,
                            Helpers.Verify, Helpers.SetError, queueContainer.CreateTimeSync(ConnectionInfo.ConnectionString), null, LoggerShared.Create(queueName, GetType().Name));
                    }
#if NETFULL
                    else
                    {
                        tests.RunEnqueueTestDynamic<SqlServerMessageQueueInit, SqlServerJobQueueCreation>(queueConnection, interceptors,
                            Helpers.Verify, Helpers.SetError, queueContainer.CreateTimeSync(ConnectionInfo.ConnectionString), null, LoggerShared.Create(queueName, GetType().Name));
                    }
#endif
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
