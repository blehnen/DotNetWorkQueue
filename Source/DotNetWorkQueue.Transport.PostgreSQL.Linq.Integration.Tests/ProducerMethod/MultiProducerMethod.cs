using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ProducerMethod
{
    [Collection("producer")]
    public class MultiProducerMethod
    {
        [Theory]
#if NETFULL
        [InlineData(100, LinqMethodTypes.Dynamic, true),
         InlineData(1000, LinqMethodTypes.Compiled, false)]
#else
        [InlineData(1000, LinqMethodTypes.Compiled, false),
        InlineData(100, LinqMethodTypes.Compiled, true)]
#endif
        public void Run(int messageCount, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                        RunTest(queueConnection, messageCount, 10, logProvider, Guid.NewGuid(), 0, linqMethodTypes, null, enableChaos);
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

        private void RunTest(QueueConnection queueConnection, int messageCount, int queueCount, ILogger logProvider, Guid id, int runTime, LinqMethodTypes linqMethodTypes, ICreationScope scope, bool enableChaos)
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var producer = new ProducerMethodShared();
                switch (linqMethodTypes)
                {
#if NETFULL
                    case LinqMethodTypes.Dynamic:
                        tasks.Add(
                            new Task(
                                () =>
                                    producer.RunTestDynamic<PostgreSqlMessageQueueInit>(queueConnection, false, messageCount,
                                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id,
                                        GenerateMethod.CreateDynamic, runTime, scope, enableChaos)));
                        break;
#endif
                    case LinqMethodTypes.Compiled:
                        tasks.Add(
                            new Task(
                                () =>
                                    producer.RunTestCompiled<PostgreSqlMessageQueueInit>(queueConnection, false, messageCount,
                                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id,
                                        GenerateMethod.CreateCompiled, runTime, scope, enableChaos)));
                        break;
                }
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
