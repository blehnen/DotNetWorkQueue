using System;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation
{
    public class JobSchedulerTests
    {
        public void Run<TTransportInit, TJobQueueCreator, TTransportCreate>(
            QueueConnection queueConnection,
            bool interceptors,
            bool dynamic,
            Action<QueueConnection, long, ICreationScope> verify,
            Action<QueueConnection, ICreationScope> setErrorFlag)
            where TTransportInit : ITransportInit, new()
            where TJobQueueCreator : class, IJobQueueCreation
            where TTransportCreate : class, IQueueCreation
        {

            using (var trace = SharedSetup.CreateTrace("jobscheduler"))
            {
                using (var queueCreator =
                       new QueueCreationContainer<TTransportInit>((x) => x.RegisterNonScopedSingleton(trace.Source)))
                {
                    var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                    ICreationScope scope = oCreation.Scope;
                    using (var queueContainer =
                           new QueueContainer<TTransportInit>(x => x.RegisterNonScopedSingleton(scope).RegisterNonScopedSingleton(trace.Source)))
                    {
                        try
                        {
                            var tests = new JobSchedulerTestsShared();
                            if (!dynamic)
                            {
                                tests.RunEnqueueTestCompiled<TTransportInit, TJobQueueCreator>(
                                    queueConnection, interceptors,
                                    verify, setErrorFlag,
                                    queueContainer.CreateTimeSync(queueConnection.Connection),
                                    oCreation.Scope, LoggerShared.Create(queueConnection.Queue, GetType().Name));
                            }
                            else
                            {
                                tests.RunEnqueueTestDynamic<TTransportInit, TJobQueueCreator>(
                                    queueConnection, interceptors,
                                    verify, setErrorFlag,
                                    queueContainer.CreateTimeSync(queueConnection.Connection),
                                    oCreation.Scope, LoggerShared.Create(queueConnection.Queue, GetType().Name));
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
}
