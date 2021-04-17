using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation
{
    public class JobSchedulerMultipleTests
    {
        public void Run<TTransportInit, TJobQueueCreator, TTransportCreate>(
            QueueConnection queueConnection,
            int producerCount)
            where TTransportInit : ITransportInit, new()
            where TJobQueueCreator : class, IJobQueueCreation
            where TTransportCreate : class, IQueueCreation
        {

            using (var queueCreator =
                new QueueCreationContainer<TTransportInit>())
            {
                var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
                var scope = oCreation.Scope;
                using (var queueContainer =
                    new QueueContainer<TTransportInit>(x => x.RegisterNonScopedSingleton(scope)))
                {
                    try
                    {
                        var tests = new JobSchedulerTestsShared();
                        tests.RunTestMultipleProducers<TTransportInit, TJobQueueCreator>(
                            queueConnection, true, producerCount,
                            queueContainer.CreateTimeSync(queueConnection.Connection),
                            LoggerShared.Create(queueConnection.Queue, GetType().Name), scope);
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
