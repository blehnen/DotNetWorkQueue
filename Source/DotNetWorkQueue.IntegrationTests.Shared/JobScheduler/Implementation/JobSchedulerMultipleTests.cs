namespace DotNetWorkQueue.IntegrationTests.Shared.JobScheduler.Implementation
{
    public class JobSchedulerMultipleTests
    {
        public void Run<TTransportInit, TJobQueueCreator, TTransportCreate>(
            string queueName,
            string connectionString,
            int producerCount)
            where TTransportInit : ITransportInit, new()
            where TJobQueueCreator : class, IJobQueueCreation
            where TTransportCreate : class, IQueueCreation
        {

            using (var queueCreator =
                new QueueCreationContainer<TTransportInit>())
            {
                var queueConnection =
                    new DotNetWorkQueue.Configuration.QueueConnection(queueName,
                        connectionString);
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
                            queueContainer.CreateTimeSync(connectionString),
                            LoggerShared.Create(queueName, GetType().Name), scope);
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
