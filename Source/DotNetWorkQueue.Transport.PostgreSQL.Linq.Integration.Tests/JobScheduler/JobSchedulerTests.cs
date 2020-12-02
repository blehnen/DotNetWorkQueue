using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.JobScheduler;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.JobScheduler
{
    [Collection("jobscheduler")]
    public class JobSchedulerTests
    {
        [Theory]
#if NETFULL

#else

#endif
        [InlineData(true, false),
         InlineData(true, true)]
        public void Run(
            bool interceptors,
            bool dynamic)
        {
            var queueName = GenerateQueueName.Create();
            using (var queueCreator =
                new QueueCreationContainer<PostgreSqlMessageQueueInit>())
            {
                var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);

                using (
                    var oCreation =
                        queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection)
                )
                {

                    using (var queueContainer = new QueueContainer<PostgreSqlMessageQueueInit>(x =>
                    {
                    }))
                    {
                        try
                        {
                            var tests = new JobSchedulerTestsShared();
                            if (!dynamic)
                            {
                                tests.RunEnqueueTestCompiled<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation>(
                                    queueConnection, interceptors,
                                    Helpers.Verify, Helpers.SetError,
                                    queueContainer.CreateTimeSync(ConnectionInfo.ConnectionString), oCreation.Scope, LoggerShared.Create(queueName, GetType().Name));
                            }
#if NETFULL
                            else
                            {
                                tests.RunEnqueueTestDynamic<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation>(
                                    queueConnection, interceptors,
                                    Helpers.Verify, Helpers.SetError,
                                    queueContainer.CreateTimeSync(ConnectionInfo.ConnectionString), oCreation.Scope, LoggerShared.Create(queueName, GetType().Name));
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
