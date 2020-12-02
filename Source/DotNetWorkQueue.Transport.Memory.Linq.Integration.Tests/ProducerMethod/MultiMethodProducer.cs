using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
    [Collection("producer")]
    public class MultiMethodProducer
    {
        [Theory]
#if NETFULL
        [InlineData(LinqMethodTypes.Dynamic),
        InlineData(LinqMethodTypes.Compiled)]
#else
        [InlineData(LinqMethodTypes.Compiled)]
#endif
        public void Run(LinqMethodTypes linqMethodTypes)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var logProvider = LoggerShared.Create(queueName, GetType().Name);
                using (var queueCreator =
                    new QueueCreationContainer<MemoryMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                {
                    var queueConnection = new QueueConnection(queueName, connectionInfo.ConnectionString);
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueConnection)
                            )
                        {
                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            RunTest(queueConnection, 100, 10, logProvider, linqMethodTypes, oCreation.Scope);
                            LoggerShared.CheckForErrors(queueName);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueConnection)
                            )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }

        private void RunTest(QueueConnection queueConnection, int messageCount, int queueCount, ILogProvider logProvider, LinqMethodTypes linqMethodTypes, ICreationScope scope)
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var id = Guid.NewGuid();
                var producer = new ProducerMethodShared();
                if (linqMethodTypes == LinqMethodTypes.Compiled)
                {
                    tasks.Add(new Task(() => producer.RunTestCompiled<MemoryMessageQueueInit>(queueConnection, false, messageCount,
                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id, GenerateMethod.CreateCompiled, 0, scope, false)));
                }
#if NETFULL
                else
                {
                    tasks.Add(new Task(() => producer.RunTestDynamic<MemoryMessageQueueInit>(queueConnection, false, messageCount,
                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id, GenerateMethod.CreateDynamic, 0, scope, false)));
                }
#endif
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
