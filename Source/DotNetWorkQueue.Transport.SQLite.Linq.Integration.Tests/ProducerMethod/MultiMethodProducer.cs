using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class MultiMethodProducer
    {
        [Theory]
        [InlineData(100, true, LinqMethodTypes.Dynamic, false),
        InlineData(10, false, LinqMethodTypes.Dynamic, true),
        InlineData(10, true, LinqMethodTypes.Compiled, true),
        InlineData(100, false, LinqMethodTypes.Compiled, false)]
        public void Run(int messageCount, bool inMemoryDb, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(inMemoryDb))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<SqLiteMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection)
                            )
                        {
                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            RunTest(queueConnection, messageCount, 10, logProvider, linqMethodTypes, oCreation.Scope, enableChaos);
                            LoggerShared.CheckForErrors(queueName);
                            new VerifyQueueData(queueConnection, oCreation.Options).Verify(messageCount * 10, null);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection)
                            )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }

        private void RunTest(QueueConnection queueConnection, int messageCount, int queueCount, ILogProvider logProvider, LinqMethodTypes linqMethodTypes, ICreationScope scope, bool enableChaos)
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var id = Guid.NewGuid();
                var producer = new ProducerMethodShared();
                if (linqMethodTypes == LinqMethodTypes.Compiled)
                {
                    tasks.Add(new Task(() => producer.RunTestCompiled<SqLiteMessageQueueInit>(queueConnection, false, messageCount,
                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id, GenerateMethod.CreateCompiled, 0, scope, enableChaos)));
                }
                else
                {
                    tasks.Add(new Task(() => producer.RunTestDynamic<SqLiteMessageQueueInit>(queueConnection, false, messageCount,
                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id, GenerateMethod.CreateDynamic, 0, scope, enableChaos)));
                }
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
