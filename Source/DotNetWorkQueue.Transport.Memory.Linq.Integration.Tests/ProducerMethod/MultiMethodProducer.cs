using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.ProducerMethod
{
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
                    try
                    {

                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            var result = oCreation.CreateQueue();
                            Assert.True(result.Success, result.ErrorMessage);

                            RunTest(queueName, 100, 10, logProvider, connectionInfo.ConnectionString, linqMethodTypes, oCreation.Scope);
                            LoggerShared.CheckForErrors(queueName);
                        }
                    }
                    finally
                    {
                        using (
                            var oCreation =
                                queueCreator.GetQueueCreation<MessageQueueCreation>(queueName,
                                    connectionInfo.ConnectionString)
                            )
                        {
                            oCreation.RemoveQueue();
                        }
                    }
                }
            }
        }

        private void RunTest(string queueName, int messageCount, int queueCount, ILogProvider logProvider, string connectionString, LinqMethodTypes linqMethodTypes, ICreationScope scope)
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var id = Guid.NewGuid();
                var producer = new ProducerMethodShared();
                if (linqMethodTypes == LinqMethodTypes.Compiled)
                {
                    tasks.Add(new Task(() => producer.RunTestCompiled<MemoryMessageQueueInit>(queueName, connectionString, false, messageCount,
                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id, GenerateMethod.CreateCompiled, 0, scope)));
                }
#if NETFULL
                else
                {
                    tasks.Add(new Task(() => producer.RunTestDynamic<MemoryMessageQueueInit>(queueName, connectionString, false, messageCount,
                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id, GenerateMethod.CreateDynamic, 0, scope)));
                }
#endif
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
