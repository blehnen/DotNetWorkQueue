using System;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation
{
    public class JobSchedulerTests
    {
        public void Run<TTransportInit, TJobQueueCreator, TTransportCreate>(
            string queueName,
            string connectionString,
            bool interceptors,
            bool dynamic,
            Action<QueueConnection, long, ICreationScope> verify,
            Action<QueueConnection, ICreationScope> setErrorFlag)
            where TTransportInit : ITransportInit, new()
            where TJobQueueCreator : class, IJobQueueCreation
            where TTransportCreate : class, IQueueCreation
        {

            using (var queueCreator =
                    new QueueCreationContainer<TTransportInit>())
            {
                var queueConnection =
                    new DotNetWorkQueue.Configuration.QueueConnection(queueName, connectionString);
                var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                ICreationScope scope = oCreation.Scope;
                using (var queueContainer = new QueueContainer<TTransportInit>(x => x.RegisterNonScopedSingleton(scope)))
                {
                    try
                    {
                        var tests = new JobSchedulerTestsShared();
                        if (!dynamic)
                        {
                            tests.RunEnqueueTestCompiled<TTransportInit, TJobQueueCreator>(
                                queueConnection, interceptors,
                                verify, setErrorFlag,
                                queueContainer.CreateTimeSync(connectionString),
                                oCreation.Scope, LoggerShared.Create(queueName, GetType().Name));
                        }
                        else
                        {
                            tests.RunEnqueueTestDynamic<TTransportInit, TJobQueueCreator>(
                                queueConnection, interceptors,
                                verify, setErrorFlag,
                                queueContainer.CreateTimeSync(connectionString),
                                oCreation.Scope, LoggerShared.Create(queueName, GetType().Name));
                        }
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
    }
}
