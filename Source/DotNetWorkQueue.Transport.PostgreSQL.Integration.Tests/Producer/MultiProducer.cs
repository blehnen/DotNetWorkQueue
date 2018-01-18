using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Producer
{
    [Collection("postgresql")]
    public class MultiProducer
    {
        [Fact]
        public void Run()
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        RunTest(queueName, 1000, 10, logProvider, oCreation.Scope);
                        LoggerShared.CheckForErrors(queueName);
                        new VerifyQueueData(queueName, oCreation.Options).Verify(1000*10, null);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }

        private void RunTest(string queueName, int messageCount, int queueCount, ILogProvider logProvider, ICreationScope scope)
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var producer = new ProducerShared();
                var task = new Task(() => producer.RunTest<PostgreSqlMessageQueueInit, FakeMessage>(queueName, ConnectionInfo.ConnectionString, false, messageCount,
                    logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, scope));
                tasks.Add(task); 
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
