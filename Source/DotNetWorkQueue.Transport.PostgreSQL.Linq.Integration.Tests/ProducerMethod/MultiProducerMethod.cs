using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.ProducerMethod;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests;
using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.ProducerMethod
{
    [Collection("PostgreSQL")]
    public class MultiProducerMethod
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

                        RunTest(queueName, 1000, 10, logProvider, Guid.NewGuid(), 0, linqMethodTypes, null);
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

        private void RunTest(string queueName, int messageCount, int queueCount, ILogProvider logProvider, Guid id, int runTime, LinqMethodTypes linqMethodTypes, ICreationScope scope)
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
                                    producer.RunTestDynamic<PostgreSqlMessageQueueInit>(queueName,
                                        ConnectionInfo.ConnectionString, false, messageCount,
                                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id,
                                        GenerateMethod.CreateDynamic, runTime, scope)));
                        break;
#endif
                    case LinqMethodTypes.Compiled:
                        tasks.Add(
                            new Task(
                                () =>
                                    producer.RunTestCompiled<PostgreSqlMessageQueueInit>(queueName,
                                        ConnectionInfo.ConnectionString, false, messageCount,
                                        logProvider, Helpers.GenerateData, Helpers.NoVerification, true, false, id,
                                        GenerateMethod.CreateCompiled, runTime, scope)));
                        break;
                }
            }
            tasks.AsParallel().ForAll(x => x.Start());
            Task.WaitAll(tasks.ToArray());
        }
    }
}
