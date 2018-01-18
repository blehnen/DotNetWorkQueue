using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Microsoft.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Integration.Tests.Producer
{
    [Collection("SQLite")]
    public class MultiProducer
    {
        [Theory]
        [InlineData(true),
        InlineData(false)]
        public void Run(bool inMemoryDb)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<SqLiteMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            RunTest(queueName, 100, 10, logProvider, connectionInfo.ConnectionString, oCreation.Scope);
                            LoggerShared.CheckForErrors(queueName);
                            new VerifyQueueData(queueName, connectionInfo.ConnectionString, oCreation.Options).Verify(100 * 10, null);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }

        private void RunTest(string queueName, int messageCount, int queueCount, ILogProvider logProvider, string connectionString, ICreationScope scope)
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var producer = new ProducerShared();
                var task = new Task(() => producer.RunTest<SqLiteMessageQueueInit, FakeMessage>(queueName, connectionString, false, messageCount,
                    logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, scope));
                tasks.Add(task); 
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
