using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Producer
{
    [Collection("producer")]
    public class MultiProducer
    {
        [Theory]
        [InlineData(1000,false),
         InlineData(10,true)]
        public void Run(int messageCount, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<PostgreSqlMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var queueConnection = new QueueConnection(queueName, ConnectionInfo.ConnectionString);
                ICreationScope scope = null;
                var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
                try
                {

                   
                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);
                        scope = oCreation.Scope;

                    RunTest(queueConnection, messageCount, 10, logProvider, oCreation.Scope, enableChaos);
                        LoggerShared.CheckForErrors(queueName);
                        new VerifyQueueData(queueName, oCreation.Options).Verify(messageCount * 10, null);
                    
                }
                finally
                {
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                    scope?.Dispose();
                }
            }
        }

        private void RunTest(QueueConnection queueConnection, int messageCount, int queueCount, ILogger logProvider, ICreationScope scope, bool enableChaos)
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var producer = new ProducerShared();
                var task = new Task(() => producer.RunTest<PostgreSqlMessageQueueInit, FakeMessage>(queueConnection, false, messageCount,
                    logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, scope, enableChaos));
                tasks.Add(task); 
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
