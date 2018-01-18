using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.ProducerMethod
{
    [Collection("SQLite")]
    public class MultiMethodProducer
    {
        [Theory]
        [InlineData(true, LinqMethodTypes.Dynamic),
        InlineData(false, LinqMethodTypes.Dynamic),
            InlineData(true, LinqMethodTypes.Compiled),
        InlineData(false, LinqMethodTypes.Compiled)]
        public void Run(bool inMemoryDb, LinqMethodTypes linqMethodTypes)
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

                            RunTest(queueName, 100, 10, logProvider, connectionInfo.ConnectionString, linqMethodTypes, oCreation.Scope);
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

        private void RunTest(string queueName, int messageCount, int queueCount, ILogProvider logProvider, string connectionString, LinqMethodTypes linqMethodTypes, ICreationScope scope)
        {
            var tasks = new List<Task>(queueCount);
            for (var i = 0; i < queueCount; i++)
            {
                var id = Guid.NewGuid();
                var producer = new ProducerMethodShared();
                if (linqMethodTypes == LinqMethodTypes.Compiled)
                {
                    tasks.Add(new Task(() => producer.RunTestCompiled<SqLiteMessageQueueInit>(queueName, connectionString, false, messageCount,
                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id, GenerateMethod.CreateCompiled, 0, scope)));
                }
                else
                {
                    tasks.Add(new Task(() => producer.RunTestDynamic<SqLiteMessageQueueInit>(queueName, connectionString, false, messageCount,
                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id, GenerateMethod.CreateDynamic, 0, scope)));
                }
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
