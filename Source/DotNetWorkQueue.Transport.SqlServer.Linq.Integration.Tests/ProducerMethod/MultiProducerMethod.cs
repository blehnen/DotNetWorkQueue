using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.IntegrationTests;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.ProducerMethod
{
    [Collection("Producer")]
    public class MultiProducerMethod
    {
        [Theory]
#if NETFULL
        [InlineData(1000, LinqMethodTypes.Dynamic, false),
         InlineData(1000, LinqMethodTypes.Compiled, false)]
#else
        [InlineData(10,LinqMethodTypes.Compiled, true)]
#endif
        public void Run(int messageCount, LinqMethodTypes linqMethodTypes, bool enableChaos)
        {
            var queueName = GenerateQueueName.Create();
            var logProvider = LoggerShared.Create(queueName, GetType().Name);
            using (var queueCreator =
                new QueueCreationContainer<SqlServerMessageQueueInit>(
                    serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                try
                {

                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        var result = oCreation.CreateQueue();
                        Assert.True(result.Success, result.ErrorMessage);

                        RunTest(queueName, messageCount, 10, logProvider, Guid.NewGuid(), 0, linqMethodTypes, enableChaos);
                        LoggerShared.CheckForErrors(queueName);
                        new VerifyQueueData(queueName, oCreation.Options).Verify(messageCount * 10);
                    }
                }
                finally
                {
                    using (
                        var oCreation =
                            queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueName,
                                ConnectionInfo.ConnectionString)
                        )
                    {
                        oCreation.RemoveQueue();
                    }
                }
            }
        }

        private void RunTest(string queueName, int messageCount, int queueCount, ILogProvider logProvider, Guid id, int runTime, LinqMethodTypes linqMethodTypes, bool enableChaos)
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
                                    producer.RunTestDynamic<SqlServerMessageQueueInit>(queueName,
                                        ConnectionInfo.ConnectionString, false, messageCount,
                                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id,
                                        GenerateMethod.CreateDynamic, runTime, null, enableChaos)));
                        break;
#endif
                    case LinqMethodTypes.Compiled:
                        tasks.Add(
                            new Task(
                                () =>
                                    producer.RunTestCompiled<SqlServerMessageQueueInit>(queueName,
                                        ConnectionInfo.ConnectionString, false, messageCount,
                                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id,
                                        GenerateMethod.CreateCompiled, runTime, null, enableChaos)));
                        break;
                }
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
