using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class MultiMethodProducer
    {
        [Theory]
        [InlineData(100, LinqMethodTypes.Dynamic, false, IntegrationConnectionInfo.ConnectionTypes.Direct),
        InlineData(10, LinqMethodTypes.Dynamic, true, IntegrationConnectionInfo.ConnectionTypes.Memory),
        InlineData(10,  LinqMethodTypes.Compiled, true, IntegrationConnectionInfo.ConnectionTypes.Shared),
        InlineData(100,  LinqMethodTypes.Compiled, false, IntegrationConnectionInfo.ConnectionTypes.Direct)]
        public void Run(int messageCount, LinqMethodTypes linqMethodTypes, bool enableChaos, IntegrationConnectionInfo.ConnectionTypes connectionType)
        {
            using (var connectionInfo = new IntegrationConnectionInfo(connectionType))
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<LiteDbMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionInfo.ConnectionString);
                    ICreationScope scope = null;
                    var oCreation = queueCreator.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection);
                    try
                    {
                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);
                        scope = oCreation.Scope;

                        RunTest(queueConnection, messageCount, 10, logProvider, linqMethodTypes, oCreation.Scope,
                            enableChaos);
                        LoggerShared.CheckForErrors(queueName);
                        new VerifyQueueData(queueConnection, oCreation.Options, scope).Verify(messageCount * 10, null);

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

        private void RunTest(QueueConnection queueConnection, int messageCount, int queueCount, ILogger logProvider, LinqMethodTypes linqMethodTypes, ICreationScope scope, bool enableChaos)
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var id = Guid.NewGuid();
                var producer = new ProducerMethodShared();
                if (linqMethodTypes == LinqMethodTypes.Compiled)
                {
                    tasks.Add(new Task(() => producer.RunTestCompiled<LiteDbMessageQueueInit>(queueConnection, false, messageCount,
                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id, GenerateMethod.CreateCompiled, 0, scope, enableChaos)));
                }
                else
                {
                    tasks.Add(new Task(() => producer.RunTestDynamic<LiteDbMessageQueueInit>(queueConnection, false, messageCount,
                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id, GenerateMethod.CreateDynamic, 0, scope, enableChaos)));
                }
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
